using System.Net;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    // #659: exact seed-4, ordinal-0 accepted prefix, independent of the load harness.
    [Fact]
    public async Task AdvertisedStageEntryReserveCompletionIsAcceptedAfterDesignation()
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
            (CampaignActionAudience.Commonwealth, "initiative-order", "land.initiative-order", 2, 0),
            (CampaignActionAudience.Commonwealth, "reserve-designation", "land.reserve-designation", 2, 1),
            (CampaignActionAudience.Commonwealth, "reserve-designation", "land.reserve-designation", 1, 0),
            (CampaignActionAudience.Commonwealth, "movement-segment-completion", "land.movement-segment-completion", 1, 0),
            (CampaignActionAudience.Commonwealth, "combat-selection", "commonwealth-truck", 1, 0),
            (CampaignActionAudience.Commonwealth, "reserve-release", "commonwealth-infantry", 2, 0),
            (CampaignActionAudience.Commonwealth, "cycle-control", "land.cycle-control", 1, 0),
            (CampaignActionAudience.Commonwealth, "truck-convoy-pass", "land.truck-convoy-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "second-reserve", "land.second-reserve", 2, 1),
            (CampaignActionAudience.Axis, "second-movement", "axis-infantry", 2, 0),
            (CampaignActionAudience.Axis, "second-movement-completion", "land.second-movement-completion", 1, 0),
            (CampaignActionAudience.Axis, "combat-selection", "axis-infantry", 1, 0),
            (CampaignActionAudience.Axis, "cycle-control", "land.cycle-control", 1, 0),
            (CampaignActionAudience.Axis, "truck-convoy-pass", "land.truck-convoy-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "stage-entry-initiative-declaration", "land.stage-entry-initiative-declaration", 2, 0),
            (CampaignActionAudience.Commonwealth, "stage-entry-reserve", "land.stage-entry-reserve", 2, 0),
        ];
        foreach (var step in prefix)
        {
            var seat = step.Audience == CampaignActionAudience.Axis ? axis : commonwealth;
            var (status, body) = await outward.OpenForSeatResponse(seat);
            Assert.True(status == HttpStatusCode.OK, body);
            var entry = Assert.Single(ParseOpen(body), item => item.OpeningStage == step.Stage);
            Assert.Contains(step.Actor, entry.Elements);
            var (revision, refs, _) = await QueryOpeningForOpenSeatTest(outward, seat, step.Actor, step.Stage);
            Assert.Equal(step.Count, refs.Length);
            var (submitted, receipt) = await outward.SubmitResponse(seat, step.Actor, entry.ActionStage,
                revision, refs[step.Index]);
            Assert.True(submitted == HttpStatusCode.OK, $"{step.Stage}: {receipt}");
        }

        var (openStatus, openBody) = await outward.OpenForSeatResponse(commonwealth);
        Assert.True(openStatus == HttpStatusCode.OK, openBody);
        var opening = Assert.Single(ParseOpen(openBody));
        Assert.Equal("stage-entry-reserve", opening.OpeningStage);
        var actor = Assert.Single(opening.Elements);
        Assert.Equal("land.stage-entry-reserve", actor);
        var (currentRevision, actionRefs, _) = await QueryOpeningForOpenSeatTest(outward, commonwealth,
            actor, opening.OpeningStage);
        var completion = Assert.Single(actionRefs);
        var (submitStatus, submitBody) = await outward.SubmitResponse(commonwealth, actor,
            opening.ActionStage, currentRevision, completion);
        Assert.True(submitStatus == HttpStatusCode.OK, submitBody);

        var reconstructed = CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId));
        Assert.True(reconstructed.IsVerified, reconstructed.FailureReason.ToString());
        var (axisStatus, axisBody) = await outward.OpenForSeatResponse(axis);
        var (commonwealthStatus, commonwealthBody) = await outward.OpenForSeatResponse(commonwealth);
        Assert.True(axisStatus == HttpStatusCode.OK, axisBody);
        Assert.True(commonwealthStatus == HttpStatusCode.OK, commonwealthBody);
        Assert.NotEmpty(ParseOpen(axisBody).Concat(ParseOpen(commonwealthBody)));
    }
}
