using System.Net;
using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Theory]
    [InlineData(2, "first-player")]
    [InlineData(2, "second-player")]
    [InlineData(3, "first-player")]
    [InlineData(3, "second-player")]
    public async Task LaterStageCombatSelectionRunsThroughHttp(int stage, string role)
    {
        var custodySeed = stage == 2 ? 66ul : 83ul;
        var retreatSeed = stage == 2 ? 5ul : 12345ul;
        var first = await DriveLaterStageCombatHttp(stage, role, custodySeed);
        Assert.Contains("custody", first.Visited);
        var repeat = await DriveLaterStageCombatHttp(stage, role, custodySeed);
        Assert.Equal(first.CanonicalBytes, repeat.CanonicalBytes);
        Assert.Equal(first.Visited, repeat.Visited);
        var alternate = await DriveLaterStageCombatHttp(stage, role, retreatSeed);
        Assert.Contains("post-assault-retreat", alternate.Visited);
    }

    private static async Task<(byte[] CanonicalBytes, string[] Visited)> DriveLaterStageCombatHttp(int stage, string role, ulong seed)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var request = PositiveCombatRequest(seed);
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.CreateCampaign(request, Timing(request), OperatorSecret);
        CampaignSeatCredential[] seats =
        [
            await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret),
            await outward.IssueAndRedeemSeat(CampaignActionAudience.Commonwealth, OperatorSecret),
        ];
        var target = $"land.position.operation-{stage}.{role}.";
        var selected = false;
        var visited = new HashSet<string>();
        for (var step = 0; step < 112; step++)
        {
            var live = journal.ReadVerified(request.CampaignId);
            var checkpoint = CampaignExercises.QueryCheckpoint(live);
            var position = checkpoint.PositionId;
            var inTarget = position.StartsWith(target, StringComparison.Ordinal);
            if (selected && !inTarget)
            {
                Assert.Contains("retreat-before-assault", visited);
                Assert.Contains("force-assignment", visited);
                Assert.Contains("post-combat-pass", visited);
                VerifyReplay(live, position);
                var canonical = await fixture.ReadCanonicalCheckpointEvents(request.CampaignId);
                return (canonical.SelectMany(value => value).ToArray(), visited.Order().ToArray());
            }

            var candidates = new List<(CampaignSeatCredential Seat, string Opening, string Action, string Element)>();
            foreach (var seat in seats)
            {
                var (status, body) = await outward.OpenForSeatResponse(seat);
                Assert.True(status == HttpStatusCode.OK, $"{position}: {status} {body}");
                foreach (var entry in ParseOpen(body))
                {
                    Assert.False(entry.RuleNotImplemented, $"{position}: {entry.OpeningStage}");
                    if (entry.OpeningStage is "movement" or "second-movement" or "stage-entry-movement") continue;
                    foreach (var element in entry.Elements)
                        candidates.Add((seat, entry.OpeningStage, entry.ActionStage, element));
                }
            }
            Assert.True(candidates.Count > 0, $"No owner opening at {position}.");
            var candidate = candidates.OrderBy(value => value.Element, StringComparer.Ordinal).First();
            var opening = await QueryOpeningForOpenSeatTest(outward, candidate.Seat, candidate.Element, candidate.Opening);
            Assert.NotEmpty(opening.Refs);
            var reference = candidate.Opening is "force-assignment" or "post-assault-retreat" or "custody"
                ? opening.Refs[0] : opening.Refs[^1];
            if (candidate.Opening is "reserve-designation" or "second-reserve" or "stage-entry-reserve")
            {
                var choices = opening.Visible.GetProperty("choices").EnumerateArray()
                    .Select(choice => choice.GetProperty("kind").GetString()).ToArray();
                reference = opening.Refs[Array.IndexOf(choices, "complete")];
            }
            if (candidate.Opening == "combat-selection" && inTarget && !selected)
            {
                Assert.True(opening.Refs.Length > 1, $"No attack offer at {position} for {candidate.Element}.");
                reference = opening.Refs[0];
                selected = true;
            }
            if (selected)
            {
                visited.Add(candidate.Opening);
                VerifyReplay(live, position);
                foreach (var listed in candidates)
                {
                    var advertised = await QueryOpeningForOpenSeatTest(outward, listed.Seat, listed.Element, listed.Opening);
                    Assert.NotEmpty(advertised.Refs);
                    var other = seats.Single(seat => seat != listed.Seat);
                    Assert.Equal(HttpStatusCode.Conflict, (await outward.OpeningResponse(other, listed.Element, listed.Opening)).Status);
                }
            }
            var (accepted, response) = await outward.SubmitResponse(candidate.Seat, candidate.Element,
                candidate.Action, opening.Revision, reference);
            Assert.True(accepted == HttpStatusCode.OK, $"{position}/{candidate.Opening}: {accepted} {response}");
        }
        throw new InvalidOperationException($"Did not complete target occurrence {target}.");

        void VerifyReplay(ExerciseSession live, string position)
        {
            var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
            var replay = CampaignExercises.Reconstruct(cold);
            var authoritative = CampaignExercises.Reconstruct(live);
            Assert.True(replay.IsVerified, $"{position}: {replay.FailureReason}");
            Assert.True(authoritative.IsVerified, $"{position}: {authoritative.FailureReason}");
            Assert.Equal(authoritative.EventStreamHash, replay.EventStreamHash);
            Assert.Equal(authoritative.ReconstructedSnapshotBytes, replay.ReconstructedSnapshotBytes);
            Assert.Equal(position, CampaignExercises.QueryCheckpoint(cold).PositionId);
        }
    }
}
