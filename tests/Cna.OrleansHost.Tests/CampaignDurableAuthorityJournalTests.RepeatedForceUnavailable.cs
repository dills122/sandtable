using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    // #476: the repeated assault on the spent first pair used to open a Force round whose seals were
    // accepted but whose result and cancellation both refused, leaving the campaign stalled. With
    // the repeated pre-opening check, the Host appends the V19 force-unavailable cancellation, then
    // Anti Armor, the V21 cancelled assault and Reserve Release, exactly as for an initial round.
    [Fact]
    public void UnpayableRepeatedAssaultTakesForceUnavailableAndColdReplays()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, attacker, defender, defenderId, attackerForce, defenderForce) =
            PrepareInfantryForceRound(journal, positive: true,
                reserveElementId: "axis-infantry-second", doubleContact: true);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(attacker, "axis-infantry",
            attackerForce.Revision, attackerForce.PublicActionRefs[0]).IsAccepted);
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.True(journal.SubmitOwnerForceSeal(defender, defenderId,
            defenderForce.Revision, defenderForce.PublicActionRefs[0]).IsAccepted);
        var release = journal.QueryReserveReleaseOwnerOpening(attacker, "axis-infantry-second");
        Assert.True(journal.SubmitOwnerReserveRelease(attacker, "axis-infantry-second",
            release.Revision, release.PublicActionRefs[0]).IsAccepted);
        var control = journal.QueryCycleControlOwnerOpening(attacker);
        Assert.True(journal.SubmitOwnerCycleControl(attacker,
            control.Revision, control.PublicActionRefs[0]).IsAccepted);
        var movement = journal.QueryRepeatMovementOwnerOpening(attacker);
        Assert.True(journal.SubmitOwnerRepeatMovement(attacker,
            movement.Revision, movement.PublicActionRefs[1]).IsAccepted);

        // The spent first pair again: axis-infantry's only repeated assault.
        var selection = journal.QueryRepeatCombatOwnerOpening(attacker, "axis-infantry");
        Assert.Equal(2, selection.PublicActionRefs.Count); // one assault plus finish
        Assert.True(journal.SubmitOwnerRepeatCombat(attacker, "axis-infantry",
            selection.Revision, selection.PublicActionRefs[0]).IsAccepted);
        var repeatDefender = CampaignHostAdapter.QueryContactRbaDefenderId(
            journal.ReadVerified(request.CampaignId), CampaignActionAudience.Commonwealth);
        var rba = journal.QueryRbaOwnerOpening(defender, repeatDefender);
        Assert.True(journal.SubmitOwnerRbaDecline(defender, repeatDefender,
            rba.Revision, rba.PublicActionRefs[^1]).IsAccepted);

        // No Force round opened: neither owner gets a Force opening.
        Assert.Empty(journal.CollectingForceCampaigns("", 16));
        Assert.Throws<InvalidOperationException>(() =>
            journal.QueryForceOwnerOpening(attacker, "axis-infantry"));
        Assert.Throws<InvalidOperationException>(() =>
            journal.QueryForceOwnerOpening(defender, repeatDefender));

        var cold = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(cold).IsVerified);
        Assert.EndsWith(".reserve-release", CampaignExercises.QueryCheckpoint(cold).PositionId,
            StringComparison.Ordinal);
    }
}
