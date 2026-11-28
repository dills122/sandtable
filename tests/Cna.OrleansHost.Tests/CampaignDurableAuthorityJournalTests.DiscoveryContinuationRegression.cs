using System.Net;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

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

        var retained = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignHostAdapter.IsNativeReactionPending(retained));
        var system = CampaignHostAdapter.QuerySystem(retained);
        Assert.True(system.IsSuccessful);
        Assert.Equal("close-reaction-window-no-eligible-reactor", Assert.Single(system.ActionSet!.Candidates).Kind);
        Assert.True(CampaignExercises.Reconstruct(retained).IsVerified);

        // Seed 6 has a real retained window but no eligible player choice. Discovery
        // previews the closure without persisting it; the next owner query commits V60.
        var before = journal.VerifyReadOnly(request.CampaignId);
        var (axisStatus, axisBody) = await outward.OpenForSeatResponse(axis);
        var (commonwealthStatus, commonwealthBody) = await outward.OpenForSeatResponse(commonwealth);
        Assert.True(axisStatus == HttpStatusCode.OK, axisBody);
        Assert.True(commonwealthStatus == HttpStatusCode.OK, commonwealthBody);
        Assert.Empty(ParseOpen(axisBody));
        var entryAfterClose = Assert.Single(ParseOpen(commonwealthBody), entry => entry.OpeningStage == "second-movement-completion");
        Assert.False(entryAfterClose.RuleNotImplemented);
        Assert.Equal(before, journal.VerifyReadOnly(request.CampaignId));
        var (completionRevision, completionRefs, _) = await QueryOpeningForOpenSeatTest(outward, commonwealth,
            "land.second-movement-completion", "second-movement-completion");
        var events = await fixture.ReadCanonicalCheckpointEvents(request.CampaignId);
        using var closure = JsonDocument.Parse(events[^1]);
        Assert.Equal(60, closure.RootElement.GetProperty("contractVersion").GetInt32());
        Assert.Equal("noEligibleReactor", closure.RootElement.GetProperty("closeKind").GetString());
        Assert.True((await outward.SubmitResponse(commonwealth, "land.second-movement-completion",
            "second-movement-completion", completionRevision, Assert.Single(completionRefs))).Status == HttpStatusCode.OK);
        var verification = journal.VerifyReadOnly(request.CampaignId);
        Assert.NotNull(verification);
        Assert.True(verification.IsVerified);
        Assert.Equal(verification, fixture.Open(clock).VerifyReadOnly(request.CampaignId));
    }
}
