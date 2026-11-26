using System.Net;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static CampaignCreationRequest CombatTurnBoundaryRequest(ulong seed) => new(1,
        "campaign-turn-boundary-combat-gt1", Cna1979Ruleset.Manifest.Hash, seed,
        "rules-lab.breakdown.turn-boundary.combat.gt1.v1", "sha256:f5e5b4a48ca9da4f7bbfa48e56183a9ca3f22b1acb735a8500a78a0b241083d4",
        "rules-lab.content.breakdown-turn-boundary.combat.gt1.v1",
        "sha256:37724a4ae5e69acdd71e30e458029a9d74bb93df90caca7d922c259014cdde04",
        "breakdown-turn-boundary-combat-gt1");

    [Theory]
    [InlineData(66ul, "custody", false)]
    [InlineData(6ul, "post-assault-retreat", false)]
    [InlineData(6ul, "post-assault-retreat", true)]
    public async Task CombatTurnBoundaryOwnerRoundsReplay(ulong seed, string expectedBranch, bool refuseRetreat)
    {
        var result = await DriveCombatTurnBoundary(seed, stopAfterCombat: true, refuseRetreat: refuseRetreat);
        Assert.Contains("force-assignment:Axis", result.Visited);
        Assert.Contains("force-assignment:Commonwealth", result.Visited);
        Assert.Contains(expectedBranch, result.Visited);
    }

    [Fact]
    public async Task CombatTurnBoundaryOwnerRoundsReplayAndReachTurnTwo()
    {
        var first = await DriveCombatTurnBoundary(5);
        var repeat = await DriveCombatTurnBoundary(5);
        Assert.Equal(first.Bytes, repeat.Bytes);
        Assert.Equal(first.Visited, repeat.Visited);
        Assert.Contains("force-assignment:Axis", first.Visited);
        Assert.Contains("force-assignment:Commonwealth", first.Visited);
    }

    private static async Task<(byte[] Bytes, string[] Visited)> DriveCombatTurnBoundary(ulong seed, bool stopAfterCombat = false, bool refuseRetreat = false)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var request = CombatTurnBoundaryRequest(seed);
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.CreateCampaign(request, Timing(request), OperatorSecret);
        CampaignSeatCredential[] seats =
        [
            await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret),
            await outward.IssueAndRedeemSeat(CampaignActionAudience.Commonwealth, OperatorSecret),
        ];
        var selected = false;
        var declarations = 0;
        var visited = new HashSet<string>();
        var trace = new List<string>();
        for (var step = 0; step < 160; step++)
        {
            var candidates = new List<(CampaignSeatCredential Seat, string Opening, string Action, string Element)>();
            foreach (var seat in seats)
            {
                var (status, body) = await outward.OpenForSeatResponse(seat);
                Assert.True(status == HttpStatusCode.OK, $"{string.Join(',', trace)}: {status} {body}");
                foreach (var entry in ParseOpen(body))
                {
                    Assert.False(entry.RuleNotImplemented, $"{string.Join(',', trace)}: {entry.OpeningStage}");
                    if (entry.OpeningStage is "movement" or "second-movement" or "stage-entry-movement") continue;
                    foreach (var element in entry.Elements)
                        candidates.Add((seat, entry.OpeningStage, entry.ActionStage, element));
                }
            }
            Assert.True(candidates.Count > 0, $"No opening: {string.Join(',', trace)}");
            var candidate = candidates.OrderBy(value => value.Element, StringComparer.Ordinal).First();
            var stage = candidate.Opening;
            trace.Add(stage);
            if (stage is "initiative-order" or "stage-entry-initiative-declaration") declarations++;
            var opening = await QueryOpeningForOpenSeatTest(outward, candidate.Seat, candidate.Element, stage);
            Assert.NotEmpty(opening.Refs);
            if (stopAfterCombat && selected && stage == "cycle-control")
            {
                VerifyReplay(journal.ReadVerified(request.CampaignId));
                var records = await fixture.ReadCanonicalCheckpointEvents(request.CampaignId);
                return (records.SelectMany(value => value).ToArray(), visited.Order().ToArray());
            }
            if (declarations == 4 && stage == "stage-entry-reserve")
            {
                var checkpoint = journal.ReadVerified(request.CampaignId);
                Assert.Equal("land.position.operation-1.first-player.reserve-designation", CampaignExercises.QueryCheckpoint(checkpoint).PositionId);
                VerifyReplay(checkpoint);
                TestContext.Current.TestOutputHelper?.WriteLine($"Seed {seed}: {string.Join(',', visited.Order())}");
                var records = await fixture.ReadCanonicalCheckpointEvents(request.CampaignId);
                return (records.SelectMany(value => value).ToArray(), visited.Order().ToArray());
            }
            var reference = stage is "force-assignment" or "post-assault-retreat" or "custody"
                ? opening.Refs[0] : opening.Refs[^1];
            if (stage == "post-assault-retreat" && refuseRetreat)
            {
                Assert.True(opening.Refs.Length > 1, "Retreat refusal must be distinct from the first offer.");
                reference = opening.Refs[^1];
            }
            if (stage is "reserve-designation" or "second-reserve" or "stage-entry-reserve")
            {
                var choices = opening.Visible.GetProperty("choices").EnumerateArray()
                    .Select(choice => choice.GetProperty("kind").GetString()).ToArray();
                reference = opening.Refs[Array.IndexOf(choices, "complete")];
            }
            if (stage == "combat-selection" && !selected && opening.Refs.Length > 1)
            {
                reference = opening.Refs[0];
                selected = true;
            }
            if (stage is "force-assignment" or "post-assault-retreat" or "custody") VerifyReplay(journal.ReadVerified(request.CampaignId));
            var (accepted, response) = await outward.SubmitResponse(candidate.Seat, candidate.Element,
                candidate.Action, opening.Revision, reference);
            Assert.True(accepted == HttpStatusCode.OK, $"{string.Join(',', trace)}/{stage}: {accepted} {response}");
            visited.Add(stage);
            if (stage == "force-assignment") visited.Add($"{stage}:{candidate.Seat.Audience}");
            if (stage is "force-assignment" or "post-assault-retreat" or "custody")
                VerifyReplay(journal.ReadVerified(request.CampaignId));
        }
        throw new InvalidOperationException("Turn 2 Reserve not reached: " + string.Join(',', trace));

        void VerifyReplay(ExerciseSession live)
        {
            var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
            var replay = CampaignExercises.Reconstruct(cold);
            var authoritative = CampaignExercises.Reconstruct(live);
            Assert.True(replay.IsVerified, replay.FailureReason.ToString());
            Assert.True(authoritative.IsVerified, authoritative.FailureReason.ToString());
            Assert.Equal(authoritative.EventStreamHash, replay.EventStreamHash);
            Assert.Equal(authoritative.ReconstructedSnapshotBytes, replay.ReconstructedSnapshotBytes);
            Assert.Equal(CampaignExercises.QueryCheckpoint(live).PositionId, CampaignExercises.QueryCheckpoint(cold).PositionId);
        }
    }
}
