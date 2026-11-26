using System.Net;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    // #662: seed-7 ordinal-0 (attacker-only) and root-1 ordinal-12 (defender-only).
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdvertisedTruckSelectionContinuationIsAccepted(bool truckDefender)
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
            (CampaignActionAudience.Axis, "reserve-designation", "land.reserve-designation", 2, 1),
            (CampaignActionAudience.Axis, "reserve-designation", "land.reserve-designation", 1, 0),
            (CampaignActionAudience.Axis, "movement", "axis-truck", 3, 2),
            (CampaignActionAudience.Axis, "movement", "axis-truck", 2, 0),
            (CampaignActionAudience.Axis, "movement-segment-completion", "land.movement-segment-completion", 1, 0),
            (CampaignActionAudience.Axis, "movement-segment-completion", "land.movement-segment-completion", 1, 0),
            (CampaignActionAudience.Axis, "combat-selection", "axis-truck", 2, 0),
            (CampaignActionAudience.Commonwealth, "rba-decline", "commonwealth-infantry", 1, 0),
            (CampaignActionAudience.Axis, "reserve-release", "axis-infantry", 2, 0),
            (CampaignActionAudience.Axis, "cycle-control", "land.cycle-control", 1, 0),
            (CampaignActionAudience.Axis, "truck-convoy-pass", "land.truck-convoy-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "second-reserve", "land.second-reserve", 2, 1),
            (CampaignActionAudience.Commonwealth, "second-movement", "commonwealth-infantry", 1, 0),
            (CampaignActionAudience.Commonwealth, "second-movement-completion", "land.second-movement-completion", 1, 0),
            (CampaignActionAudience.Commonwealth, "combat-selection", "commonwealth-infantry", 1, 0),
            (CampaignActionAudience.Commonwealth, "cycle-control", "land.cycle-control", 1, 0),
            (CampaignActionAudience.Commonwealth, "truck-convoy-pass", "land.truck-convoy-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "stage-entry-initiative-declaration", "land.stage-entry-initiative-declaration", 2, 0),
            (CampaignActionAudience.Commonwealth, "stage-entry-reserve", "land.stage-entry-reserve", 2, 1),
            (CampaignActionAudience.Commonwealth, "stage-entry-movement", "commonwealth-infantry", 3, 2),
            (CampaignActionAudience.Commonwealth, "stage-entry-movement-completion", "land.stage-entry-movement-completion", 1, 0),
            (CampaignActionAudience.Commonwealth, "combat-selection", "commonwealth-truck", 1, 0),
            (CampaignActionAudience.Commonwealth, "cycle-control", "land.cycle-control", 1, 0),
            (CampaignActionAudience.Commonwealth, "truck-convoy-pass", "land.truck-convoy-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "stage-entry-reserve", "land.stage-entry-reserve", 2, 1),
            (CampaignActionAudience.Axis, "stage-entry-movement-completion", "land.stage-entry-movement-completion", 1, 0),
            (CampaignActionAudience.Axis, "combat-selection", "axis-truck", 2, 0),
        ];
        if (truckDefender) prefix = TruckDefenderPrefix;
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

        var defender = truckDefender ? axis : commonwealth;
        var (openStatus, openBody) = await outward.OpenForSeatResponse(defender);
        Assert.True(openStatus == HttpStatusCode.OK, openBody);
        var opening = Assert.Single(ParseOpen(openBody), item => item.ActionStage == "rba-decline");
        var actor = Assert.Single(opening.Elements);
        Assert.Equal(truckDefender ? "axis-truck" : "commonwealth-infantry", actor);
        var (currentRevision, actionRefs, _) = await QueryOpeningForOpenSeatTest(outward, defender,
            actor, opening.OpeningStage);
        var choice = Assert.Single(actionRefs);
        var (submitStatus, submitBody) = await outward.SubmitResponse(defender, actor,
            opening.ActionStage, currentRevision, choice);
        Assert.True(submitStatus == HttpStatusCode.OK, submitBody);
        var reconstructed = CampaignExercises.Reconstruct(fixture.Open(clock).ReadVerified(request.CampaignId));
        Assert.True(reconstructed.IsVerified, reconstructed.FailureReason.ToString());
        var (axisStatus, axisBody) = await outward.OpenForSeatResponse(axis);
        var (commonwealthStatus, commonwealthBody) = await outward.OpenForSeatResponse(commonwealth);
        Assert.True(axisStatus == HttpStatusCode.OK, axisBody);
        Assert.True(commonwealthStatus == HttpStatusCode.OK, commonwealthBody);
        Assert.NotEmpty(ParseOpen(axisBody).Concat(ParseOpen(commonwealthBody)));
    }

    private static readonly (CampaignActionAudience Audience, string Stage, string Actor, int Count, int Index)[]
        TruckDefenderPrefix =
        [
            (CampaignActionAudience.Commonwealth, "initiative-order", "land.initiative-order", 2, 1),
            (CampaignActionAudience.Axis, "reserve-designation", "land.reserve-designation", 2, 1),
            (CampaignActionAudience.Axis, "reserve-designation", "land.reserve-designation", 1, 0),
            (CampaignActionAudience.Axis, "movement", "axis-truck", 3, 0),
            (CampaignActionAudience.Axis, "movement", "axis-truck", 1, 0),
            (CampaignActionAudience.Axis, "movement-segment-completion", "land.movement-segment-completion", 1, 0),
            (CampaignActionAudience.Axis, "movement", "axis-truck", 3, 2),
            (CampaignActionAudience.Axis, "movement", "axis-truck", 2, 0),
            (CampaignActionAudience.Axis, "movement-segment-completion", "land.movement-segment-completion", 1, 0),
            (CampaignActionAudience.Axis, "movement-segment-completion", "land.movement-segment-completion", 1, 0),
            (CampaignActionAudience.Axis, "combat-selection", "axis-infantry", 1, 0),
            (CampaignActionAudience.Axis, "reserve-release", "axis-infantry", 2, 1),
            (CampaignActionAudience.Axis, "cycle-control", "land.cycle-control", 1, 0),
            (CampaignActionAudience.Axis, "truck-convoy-pass", "land.truck-convoy-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "second-reserve", "land.second-reserve", 2, 0),
            (CampaignActionAudience.Commonwealth, "second-reserve", "land.second-reserve", 1, 0),
            (CampaignActionAudience.Commonwealth, "second-movement-completion", "land.second-movement-completion", 1, 0),
            (CampaignActionAudience.Commonwealth, "combat-selection", "commonwealth-infantry", 2, 1),
            (CampaignActionAudience.Commonwealth, "reserve-release", "commonwealth-infantry", 2, 1),
            (CampaignActionAudience.Commonwealth, "cycle-control", "land.cycle-control", 1, 0),
            (CampaignActionAudience.Commonwealth, "truck-convoy-pass", "land.truck-convoy-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "stage-entry-initiative-declaration", "land.stage-entry-initiative-declaration", 2, 1),
            (CampaignActionAudience.Axis, "stage-entry-reserve", "land.stage-entry-reserve", 1, 0),
            (CampaignActionAudience.Axis, "stage-entry-movement-completion", "land.stage-entry-movement-completion", 1, 0),
            (CampaignActionAudience.Axis, "combat-selection", "axis-truck", 2, 1),
            (CampaignActionAudience.Axis, "cycle-control", "land.cycle-control", 1, 0),
            (CampaignActionAudience.Axis, "truck-convoy-pass", "land.truck-convoy-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "stage-entry-reserve", "land.stage-entry-reserve", 1, 0),
            (CampaignActionAudience.Commonwealth, "stage-entry-movement-completion", "land.stage-entry-movement-completion", 1, 0),
            (CampaignActionAudience.Commonwealth, "combat-selection", "commonwealth-infantry", 2, 1),
            (CampaignActionAudience.Commonwealth, "cycle-control", "land.cycle-control", 1, 0),
            (CampaignActionAudience.Commonwealth, "truck-convoy-pass", "land.truck-convoy-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "stage-entry-initiative-declaration", "land.stage-entry-initiative-declaration", 2, 1),
            (CampaignActionAudience.Axis, "stage-entry-reserve", "land.stage-entry-reserve", 1, 0),
            (CampaignActionAudience.Axis, "stage-entry-movement-completion", "land.stage-entry-movement-completion", 1, 0),
            (CampaignActionAudience.Axis, "combat-selection", "axis-infantry", 1, 0),
            (CampaignActionAudience.Axis, "cycle-control", "land.cycle-control", 1, 0),
            (CampaignActionAudience.Axis, "truck-convoy-pass", "land.truck-convoy-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Axis, "post-combat-pass", "land.post-combat-pass", 1, 0),
            (CampaignActionAudience.Commonwealth, "stage-entry-reserve", "land.stage-entry-reserve", 1, 0),
            (CampaignActionAudience.Commonwealth, "stage-entry-movement-completion", "land.stage-entry-movement-completion", 1, 0),
            (CampaignActionAudience.Commonwealth, "combat-selection", "commonwealth-infantry", 2, 0),
        ];
}
