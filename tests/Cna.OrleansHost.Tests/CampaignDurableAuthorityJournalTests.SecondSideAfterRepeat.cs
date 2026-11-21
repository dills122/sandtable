using System.Text.Json;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;

namespace Cna.OrleansHost.Tests;

// The first side's later cycle leaves its own repeated-cycle state behind
// (CurrentPostRepeat / repeated assault runtime) when it finally finishes.
// The second side's Combat must read only its own state from then on.
public sealed partial class CampaignDurableAuthorityJournalTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SecondSideCombatFollowsAFirstSideThatRepeatedItsCycle(bool secondSideAssaults)
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, axis) = PrepareReserveRound(journal, two: true);
        // Keep axis-infantry in place (Convert to II) and release only the
        // second infantry, so the repeated Movement moves it and the original
        // contact stays adjacent for the second side.
        ResolveReserveMembers(fixture, clock, axis, ("axis-infantry", false), ("axis-infantry-second", true));

        // First side: Repeat, a repeated Movement and a repeated voluntary finish.
        var cycle = fixture.Open(clock).QueryCycleControlOwnerOpening(axis);
        using (var visible = JsonDocument.Parse(cycle.VisibleBytes))
            Assert.Equal("Repeat", visible.RootElement.GetProperty("choices")[0].GetString());
        Assert.True(fixture.Open(clock).SubmitOwnerCycleControl(axis, cycle.Revision,
            cycle.PublicActionRefs[0]).IsAccepted);
        var movement = fixture.Open(clock).QueryRepeatMovementOwnerOpening(axis);
        Assert.True(fixture.Open(clock).SubmitOwnerRepeatMovement(axis, movement.Revision,
            movement.PublicActionRefs[0]).IsAccepted);
        var combat = fixture.Open(clock).QueryRepeatCombatOwnerOpening(axis, "axis-infantry");
        Assert.True(fixture.Open(clock).SubmitOwnerRepeatCombat(axis, "axis-infantry", combat.Revision,
            combat.PublicActionRefs[^1]).IsAccepted);
        ResolveReserveMembers(fixture, clock, axis, ("axis-infantry", false), ("axis-infantry-second", false));
        var finish = fixture.Open(clock).QueryCycleControlOwnerOpening(axis);
        Assert.True(fixture.Open(clock).SubmitOwnerCycleControl(axis, finish.Revision,
            finish.PublicActionRefs[^1]).IsAccepted);
        var convoy = fixture.Open(clock).QueryTruckConvoyPassOwnerOpening(axis);
        Assert.True(fixture.Open(clock).SubmitOwnerTruckConvoyPass(axis, convoy.Revision,
            convoy.PublicActionRefs[0]).IsAccepted);
        for (var index = 0; index < 4; index++)
        {
            var phase = fixture.Open(clock).QueryPostCombatPassOwnerOpening(axis);
            Assert.True(fixture.Open(clock).SubmitOwnerPostCombatPass(axis, phase.Revision,
                Assert.Single(phase.PublicActionRefs)).IsAccepted);
        }
        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.Equal("land.position.operation-1.second-player.reserve-designation",
            CampaignExercises.QueryCheckpoint(cold).PositionId);

        // Second side: Reserve, Movement completion, its own Combat, then its block.
        var commonwealth = fixture.Open(clock).RedeemSeatGrant(fixture.Open(clock).IssueSeatGrant(
            request.CampaignId, Cna.Core.Actions.CampaignActionAudience.Commonwealth, OperatorSecret))!;
        var reserve = fixture.Open(clock).QuerySecondReserveOwnerOpening(commonwealth);
        Assert.True(fixture.Open(clock).SubmitOwnerSecondReserve(commonwealth, reserve.Revision,
            reserve.PublicActionRefs[^1]).IsAccepted);
        var completion = fixture.Open(clock).QuerySecondMovementCompletionOwnerOpening(commonwealth);
        Assert.True(fixture.Open(clock).SubmitOwnerSecondMovementCompletion(commonwealth,
            completion.Revision, Assert.Single(completion.PublicActionRefs)).IsAccepted);
        var selection = fixture.Open(clock).QueryCombatOwnerOpening(commonwealth, "commonwealth-infantry");
        var choice = selection.PublicActionRefs[^1];
        if (secondSideAssaults)
        {
            using var offers = JsonDocument.Parse(selection.VisibleBytes);
            Assert.True(offers.RootElement.GetProperty("assaultOffers").GetArrayLength() > 0,
                System.Text.Encoding.UTF8.GetString(selection.VisibleBytes));
            choice = selection.PublicActionRefs[0];
        }
        Assert.True(fixture.Open(clock).SubmitOwnerCombatSelection(commonwealth, "commonwealth-infantry",
            selection.Revision, choice).IsAccepted);
        cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        if (secondSideAssaults)
        {
            var defenderId = Cna.Core.Hosting.CampaignHostAdapter.QueryContactRbaDefenderId(cold,
                Cna.Core.Actions.CampaignActionAudience.Axis);
            var rba = fixture.Open(clock).QueryRbaOwnerOpening(axis, defenderId);
            Assert.True(fixture.Open(clock).SubmitOwnerRbaDecline(axis, defenderId, rba.Revision,
                rba.PublicActionRefs[^1]).IsAccepted);
            if (CountAcceptedStep(fixture.Path, request.CampaignId, "native-combat-force-cancellation") == 0)
            {
                var attackerForce = fixture.Open(clock).QueryForceOwnerOpening(commonwealth, "commonwealth-infantry");
                var defenderForce = fixture.Open(clock).QueryForceOwnerOpening(axis, defenderId);
                clock.Advance(TimeSpan.FromSeconds(1));
                Assert.True(fixture.Open(clock).SubmitOwnerForceSeal(commonwealth, "commonwealth-infantry",
                    attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
                clock.Advance(TimeSpan.FromSeconds(1));
                Assert.True(fixture.Open(clock).SubmitOwnerForceSeal(axis, defenderId,
                    defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
            }
            cold = fixture.Open(clock).ReadVerified(request.CampaignId);
            Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        }
        var released = CampaignExercises.QueryCheckpoint(cold);
        Assert.Equal(SecondCombatPrefix + "reserve-release", released.PositionId);
        FinishSecondSideOperationStage(fixture, clock, request, commonwealth);
    }

    // Resolves whichever listed Reserve Release members are still pending,
    // with the opening's first (release) or last offer, leaving cycle control pending.
    private static void ResolveReserveMembers(JournalFixture fixture, ManualClock clock,
        CampaignSeatCredential owner, params (string ElementId, bool Release)[] members)
    {
        foreach (var (elementId, release) in members)
        {
            CampaignOwnerOpening opening;
            try { opening = fixture.Open(clock).QueryReserveReleaseOwnerOpening(owner, elementId); }
            catch (InvalidOperationException) { continue; }
            Assert.True(fixture.Open(clock).SubmitOwnerReserveRelease(owner, elementId, opening.Revision,
                release ? opening.PublicActionRefs[0] : opening.PublicActionRefs[^1]).IsAccepted);
        }
    }
}
