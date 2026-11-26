using System.Net;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    // #661: exact retained seed-6 ordinal-0 accepted prefix, followed by both seats' discovery.
    [Fact]
    public async Task DiscoveryContinuesAfterSecondMovementStop()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var request = TurnBoundaryRequest(1);
        var timing = Timing(request);
        timing = new CampaignDecisionTimingConfigurationV1(timing.ConfigId, timing.RulesetHash,
            timing.ContentHash, timing.Windows.Select(window => window with { DecisionBudgetMilliseconds = 3_600_000 }));
        await using var outward = await OwnerHttpHarness.Start(journal, request.CampaignId);
        await outward.CreateCampaign(request, timing, OperatorSecret);
        var axis = await outward.IssueAndRedeemSeat(CampaignActionAudience.Axis, OperatorSecret);
        var commonwealth = await outward.IssueAndRedeemSeat(CampaignActionAudience.Commonwealth, OperatorSecret);
        (CampaignActionAudience Audience, string Stage, string Actor, int Count, int Index)[] prefix =
        [
            (CampaignActionAudience.Commonwealth, "initiative-order", "land.initiative-order", 2, 1),
            (CampaignActionAudience.Axis, "reserve-designation", "land.reserve-designation", 2, 0),
            (CampaignActionAudience.Axis, "movement", "axis-truck", 3, 1),
            (CampaignActionAudience.Axis, "movement", "axis-truck", 2, 0),
            (CampaignActionAudience.Axis, "movement", "axis-truck", 2, 0),
            (CampaignActionAudience.Axis, "movement-segment-completion", "land.movement-segment-completion", 1, 0),
            (CampaignActionAudience.Axis, "movement", "axis-infantry", 2, 1),
            (CampaignActionAudience.Axis, "movement", "axis-infantry", 3, 0),
            (CampaignActionAudience.Axis, "movement", "axis-infantry", 1, 0),
            (CampaignActionAudience.Axis, "movement", "axis-infantry", 2, 1),
            (CampaignActionAudience.Axis, "movement", "axis-truck", 1, 0),
            (CampaignActionAudience.Axis, "movement", "axis-truck", 2, 1),
            (CampaignActionAudience.Axis, "movement", "axis-truck", 2, 1),
            (CampaignActionAudience.Axis, "movement-segment-completion", "land.movement-segment-completion", 1, 0),
            (CampaignActionAudience.Axis, "movement", "axis-truck", 3, 1),
            (CampaignActionAudience.Axis, "movement-segment-completion", "land.movement-segment-completion", 1, 0),
            (CampaignActionAudience.Axis, "movement", "axis-truck", 2, 0),
            (CampaignActionAudience.Axis, "movement", "axis-truck", 2, 1),
            (CampaignActionAudience.Axis, "movement-segment-completion", "land.movement-segment-completion", 1, 0),
            (CampaignActionAudience.Axis, "movement-segment-completion", "land.movement-segment-completion", 1, 0),
            (CampaignActionAudience.Axis, "combat-selection", "axis-truck", 1, 0),
            (CampaignActionAudience.Axis, "cycle-control", "land.cycle-control", 1, 0),
            (CampaignActionAudience.Axis, "truck-convoy-pass", "land.truck-convoy-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "second-reserve", "land.second-reserve", 2, 1),
            (CampaignActionAudience.Commonwealth, "second-movement", "commonwealth-infantry", 2, 1),
        ];
        foreach (var step in prefix)
        {
            var seat = step.Audience == CampaignActionAudience.Axis ? axis : commonwealth;
            var (status, body) = await outward.OpenForSeatResponse(seat);
            Assert.True(status == HttpStatusCode.OK, body);
            var entry = Assert.Single(ParseOpen(body), item => item.ActionStage == step.Stage);
            Assert.Contains(step.Actor, entry.Elements);
            var (revision, refs, _) = await QueryOpeningForOpenSeatTest(outward, seat, step.Actor, entry.OpeningStage);
            Assert.Equal(step.Count, refs.Length);
            var (submitted, receipt) = await outward.SubmitResponse(seat, step.Actor, entry.ActionStage,
                revision, refs[step.Index]);
            Assert.True(submitted == HttpStatusCode.OK, $"{step.Stage}: {receipt}");
        }

        var reconstructed = CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId));
        Assert.True(reconstructed.IsVerified, reconstructed.FailureReason.ToString());
        var (axisStatus, axisBody) = await outward.OpenForSeatResponse(axis);
        var (commonwealthStatus, commonwealthBody) = await outward.OpenForSeatResponse(commonwealth);
        Assert.True(axisStatus == HttpStatusCode.OK, axisBody);
        Assert.True(commonwealthStatus == HttpStatusCode.OK, commonwealthBody);
        Assert.NotEmpty(ParseOpen(axisBody).Concat(ParseOpen(commonwealthBody)));

        // #394 gap 3 deliberately refuses the retained window in this occurrence. Discovery
        // must report that explicit marker, not pretend there is no owner opening or legal ref.
        foreach (var (seat, body) in new[] { (axis, axisBody), (commonwealth, commonwealthBody) })
        {
            var entry = Assert.Single(ParseOpen(body));
            Assert.Equal("reaction", entry.OpeningStage);
            Assert.Equal("reaction", entry.ActionStage);
            Assert.True(entry.RuleNotImplemented);
            var actor = Assert.Single(entry.Elements);
            Assert.Equal("land.reaction", actor);
            var (openStatus, openBody) = await outward.OpeningResponse(seat, actor, entry.OpeningStage);
            Assert.Equal(HttpStatusCode.Conflict, openStatus);
            using var json = JsonDocument.Parse(openBody);
            Assert.Equal("rule-not-implemented", json.RootElement.GetProperty("code").GetString());
            Assert.False(json.RootElement.TryGetProperty("publicActionRefs", out _));
        }
    }
}
