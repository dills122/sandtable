using System.Collections.Immutable;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Fact]
    public async Task FreshHttpCampaignReachesTurnTwoCheckpoint()
    {
        var first = await DriveCheckpointHttp(1);
        var second = await DriveCheckpointHttp(1);
        Assert.Equal(first, second);
        await DriveCheckpointHttp(12);
    }

    private static async Task<(string? StreamHash, string CanonicalHash)> DriveCheckpointHttp(int initialTurn)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var request = TurnBoundaryRequest(initialTurn);
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.CreateCampaign(request, Timing(request), OperatorSecret);
        CampaignSeatCredential[] seats =
        [
            await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret),
            await outward.IssueAndRedeemSeat(CampaignActionAudience.Commonwealth, OperatorSecret),
        ];
        var declarations = 0;
        var stagesWithMovementOffers = new HashSet<int>();
        var trace = new List<string>();
        var duration = Stopwatch.StartNew();
        for (var step = 0; step < 112; step++)
        {
            var candidates = new List<(CampaignSeatCredential Seat, string Stage, string ActionStage, string Key)>();
            foreach (var seat in seats)
            {
                var (status, body) = await outward.OpenForSeatResponse(seat);
                Assert.True(status == HttpStatusCode.OK, $"{string.Join('\n', trace)}\nDiscovery {seat.Audience}: {status} {body}");
                foreach (var entry in ParseOpen(body))
                {
                    Assert.False(entry.RuleNotImplemented, $"Unsupported {entry.OpeningStage} after {string.Join('\n', trace)}");
                    // Optional movement is not required to complete the synthetic path.
                    if (entry.OpeningStage == "stage-entry-movement")
                    {
                        foreach (var element in entry.Elements)
                        {
                            var movement = await QueryOpeningForOpenSeatTest(outward, seat, element, entry.OpeningStage);
                            Assert.NotEmpty(movement.Refs);
                            var ledger = movement.Visible.GetProperty("ownElements")[0];
                            Assert.Equal(initialTurn, ledger.GetProperty("ledgerGameTurn").GetInt32());
                            Assert.Equal(declarations, ledger.GetProperty("ledgerOperationStage").GetInt32());
                            stagesWithMovementOffers.Add(declarations);
                        }
                    }
                    if (entry.OpeningStage is "movement" or "second-movement" or "stage-entry-movement") continue;
                    foreach (var key in entry.Elements)
                        candidates.Add((seat, entry.OpeningStage, entry.ActionStage, key));
                }
            }
            Assert.True(candidates.Count > 0, $"No opening after {string.Join('\n', trace)}");
            Assert.Single(candidates.Select(value => (value.Seat, value.Stage, value.ActionStage)).Distinct());
            // Per-element openings may advertise several actors for the same decision.
            var (owner, stage, actionStage, keyId) = candidates.OrderBy(value => value.Key, StringComparer.Ordinal).Last();
            trace.Add($"{step}: {owner.Audience}/{stage}/{keyId}");
            if (stage is "initiative-order" or "stage-entry-initiative-declaration")
            {
                if (declarations > 0)
                {
                    var (boundary, _) = VerifyReplay();
                    Assert.Equal(declarations == 3 ? "land.position.end-of-turn"
                        : $"land.position.operation-{declarations + 1}.initiative-declaration",
                        CampaignExercises.QueryCheckpoint(boundary).PositionId);
                }
                declarations++;
            }
            var opening = await QueryOpeningForOpenSeatTest(outward, owner, keyId, stage);
            Assert.NotEmpty(opening.Refs);
            if (declarations == 4 && stage == "stage-entry-initiative-declaration")
            {
                var (boundary, _) = VerifyReplay();
                Assert.Equal("land.position.operation-1.initiative-declaration",
                    CampaignExercises.QueryCheckpoint(boundary).PositionId);
            }
            if (declarations == 4 && stage == "stage-entry-reserve")
            {
                var (checkpoint, reconstruction) = VerifyReplay();
                Assert.Equal("land.position.operation-1.first-player.reserve-designation",
                    CampaignExercises.QueryCheckpoint(checkpoint).PositionId);
                var records = await fixture.ReadCanonicalCheckpointEvents(request.CampaignId);
                AssertCheckpointHistory(records, initialTurn, owner.Audience);
                Assert.Equal([2, 3], stagesWithMovementOffers.Order());
                AssertCheckpointTamper(checkpoint, records[^1]);
                var canonicalHash = Convert.ToHexStringLower(SHA256.HashData(records.SelectMany(value => value).ToArray()));
                TestContext.Current.TestOutputHelper?.WriteLine($"GT{initialTurn}: {step} owner submissions, {records.Length} events, {records.Sum(value => value.Length)} canonical bytes, {duration.Elapsed}; {canonicalHash}");
                return (reconstruction.EventStreamHash, canonicalHash);
            }
            var reference = opening.Refs[^1];
            if (stage is "reserve-designation" or "second-reserve" or "stage-entry-reserve")
            {
                var kinds = opening.Visible.GetProperty("choices").EnumerateArray()
                    .Select(value => value.GetProperty("kind").GetString()).ToArray();
                var complete = Array.IndexOf(kinds, "complete");
                Assert.True(complete >= 0, $"{stage} did not offer completion.");
                reference = opening.Refs[complete];
            }
            var (accepted, response) = await outward.SubmitResponse(owner, keyId, actionStage, opening.Revision, reference);
            Assert.True(accepted == HttpStatusCode.OK, $"{string.Join('\n', trace)}\n{accepted}: {response}");
        }
        throw new InvalidOperationException("Checkpoint not reached:\n" + string.Join('\n', trace));

        (ExerciseSession Session, ExerciseReconstructionResult Replay) VerifyReplay()
        {
            var liveSession = journal.ReadVerified(request.CampaignId);
            var live = CampaignExercises.Reconstruct(liveSession);
            var timer = Stopwatch.StartNew();
            var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
            var loadMs = timer.Elapsed.TotalMilliseconds;
            timer.Restart();
            var replay = CampaignExercises.Reconstruct(cold);
            Assert.True(replay.IsVerified, replay.FailureReason.ToString());
            Assert.True(live.IsVerified, live.FailureReason.ToString());
            Assert.Equal(live.EventStreamHash, replay.EventStreamHash);
            Assert.Equal(live.ReconstructedSnapshotBytes, replay.ReconstructedSnapshotBytes);
            TestContext.Current.TestOutputHelper?.WriteLine($"{CampaignExercises.QueryCheckpoint(cold).PositionId}: Load={loadMs:F1} ms, Reconstruct={timer.Elapsed.TotalMilliseconds:F1} ms");
            Assert.Equal(live.ExpectedSnapshotHash, replay.ExpectedSnapshotHash);
            var head = CampaignExercises.QueryCheckpoint(liveSession);
            var reconstructedHead = CampaignExercises.QueryCheckpoint(cold);
            Assert.Equal((head.PositionId, head.StateVersion, head.ContractVersion),
                (reconstructedHead.PositionId, reconstructedHead.StateVersion, reconstructedHead.ContractVersion));
            Assert.Equal(replay.ExpectedSnapshotHash, replay.ReconstructedSnapshotHash);
            return (cold, replay);
        }
    }

    // Validate every edge actually recorded by the HTTP host. The synthetic no-attack
    // record contains five explicit edges; empty Reserve Release retains its cursor
    // until Cycle Control finishes. Never fill a missing edge from the catalog.
    private static void AssertCheckpointHistory(byte[][] records, int initialTurn, CampaignActionAudience reserveOwner)
    {
        var firstTurn = Cna1979LandSequence.CreateTurn(initialTurn);
        Assert.Equal(112, firstTurn.Count);
        var laterTurn = Cna1979LandSequence.CreateTurn(initialTurn + 1);
        var expected = firstTurn.Concat(laterTurn.TakeWhile(position => position.PhaseId != LandPhaseIds.ReserveDesignation))
            .Append(laterTurn.First(position => position.PhaseId == LandPhaseIds.ReserveDesignation)).ToArray();
        var visited = 0;
        var noAttackCount = 0;
        var endCount = 0;
        var rolloverCount = 0;
        var weatherCount = 0;
        using (var genesis = JsonDocument.Parse(records[0]))
        {
            Assert.Equal("campaign-created", genesis.RootElement.GetProperty("eventType").GetString());
            var position = genesis.RootElement.GetProperty("sequencePosition");
            Assert.Equal(expected[0].PositionId, position.GetProperty("positionId").GetString());
            Assert.Equal(initialTurn, position.GetProperty("gameTurn").GetInt32());
        }
        foreach (var record in records.Skip(1))
        {
            using var document = JsonDocument.Parse(record);
            var root = document.RootElement;
            var kind = root.GetProperty("eventType").GetString();
            var turn = expected[visited].GameTurn;
            if (kind == "second-reserve-decided")
            {
                var position = expected[visited];
                var rollover = position.OperationStage > 1 && position.ActorRole == LandActorRole.FirstActingSide;
                Assert.Equal(rollover ? 58 : 43, root.GetProperty("contractVersion").GetInt32());
                if (rollover) rolloverCount++;
            }
            if (turn != initialTurn)
                Assert.Equal(turn, root.GetProperty("gameTurn").GetInt32());
            else if (root.TryGetProperty("gameTurn", out var stamped))
                Assert.Equal(turn, stamped.GetInt32());
            if (kind == "native-end-of-turn")
            {
                Assert.Equal(initialTurn, turn);
                Assert.Equal(initialTurn + 1, root.GetProperty("nextGameTurn").GetInt32());
                endCount++;
            }
            if (kind is "weather-determined" or "native-weather-determined")
            {
                Assert.Equal(turn == 13 ? "winter" : "fall", root.GetProperty("season").GetString(), ignoreCase: true);
                weatherCount++;
            }
            if (kind == "combat-no-attack-completed")
            {
                var steps = root.GetProperty("steps").EnumerateArray().ToArray();
                Assert.Equal(5, steps.Length);
                foreach (var edge in steps)
                    Advance(edge.GetProperty("positionId").GetString(), edge.GetProperty("nextPositionId").GetString());
                noAttackCount++;
                continue;
            }
            var next = root.TryGetProperty("nextPositionId", out var nextId) ? nextId.GetString()
                : root.GetProperty("sequencePosition").GetProperty("positionId").GetString();
            if (root.TryGetProperty("fromPositionId", out var from) || root.TryGetProperty("positionId", out from))
                Advance(from.GetString(), next);
            else
            {
                Assert.Equal(LandSegmentIds.ReserveRelease, expected[visited].SegmentId);
                if (kind == "reserve-release-completed") Assert.Equal(expected[visited].PositionId, next);
                else
                {
                    Assert.Equal("movement-combat-cycle-control", kind);
                    Advance(expected[visited].PositionId, next);
                }
            }
            if (kind == "native-no-obligation-fleet-repair-resolved" && turn != initialTurn)
                Assert.Equal(reserveOwner.ToString(), root.GetProperty("firstSide").GetString(), ignoreCase: true);
        }
        Assert.Equal(expected.Length - 1, visited);
        Assert.Equal(6, noAttackCount);
        Assert.Equal(1, endCount);
        Assert.Equal(2, rolloverCount);
        Assert.Equal(4, weatherCount);

        void Advance(string? from, string? next)
        {
            Assert.Equal(expected[visited].PositionId, from);
            Assert.True(visited + 1 < expected.Length, "HTTP advanced beyond the open Reserve checkpoint.");
            Assert.Equal(expected[++visited].PositionId, next);
        }
    }

    // Core's internal history is deliberately not a Host API. As in Core's SessionTamper
    // tests, alter only a detached replay session, using the exact persisted event shape.
    // A valid hex digit replacement changes one byte without making malformed JSON.
    private static void AssertCheckpointTamper(ExerciseSession session, byte[] lastRecord)
    {
        const BindingFlags instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var historyProperty = typeof(ExerciseSession).GetProperty("CurrentHistory", instance)!;
        var history = (ImmutableList<object>)historyProperty.GetValue(session)!;
        var altered = lastRecord.ToArray();
        ReadOnlySpan<byte> marker = "\"worldHash\":\"sha256:"u8;
        var index = altered.AsSpan().IndexOf(marker);
        Assert.True(index >= 0);
        index += marker.Length;
        altered[index] = altered[index] == (byte)'0' ? (byte)'1' : (byte)'0';
        var eventType = history[^1].GetType();
        var deserialize = eventType.GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!;
        var replacement = deserialize.Invoke(null, [new ReadOnlyMemory<byte>(altered)])!;
        Assert.NotEqual(history[^1], replacement);
        var copy = (ExerciseSession)typeof(object).GetMethod("MemberwiseClone", instance)!.Invoke(session, null)!;
        historyProperty.SetValue(copy, history.SetItem(history.Count - 1, replacement));
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, CampaignExercises.Reconstruct(copy).FailureReason);
    }
}
