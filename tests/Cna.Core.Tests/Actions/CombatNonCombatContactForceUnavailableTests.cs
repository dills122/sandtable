using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Actions;

/// <summary>
/// T020-Q8 (ruled 2026-11-20, option c): an assault on a contact bound to a unit with no combat
/// component stays offered on the Selection surface; the existing V19 <c>force-unavailable</c>
/// System cancellation carries it through Anti Armor and the cancelled-assault successor.
/// </summary>
public sealed class CombatNonCombatContactForceUnavailableTests
{
    private const string Attacker = "axis-infantry";

    [Fact]
    public void TruckBoundAssaultIsOfferedThenCancelledAsForceUnavailable()
    {
        var session = AdvanceToTruckCombat();
        // Offer presence does not inspect the hidden defender: the truck-bound contact is offered.
        var selection = CampaignCombatAudienceDecisions.QuerySelection(session, CampaignActionAudience.Axis, Attacker)!;
        Assert.Equal(2, selection.OfferRefs.Count); // assault + finish
        var selected = CampaignCombatAudienceDecisions.SubmitSelection(session, selection.Choose(0));
        Assert.True(selected.IsAccepted);
        session = selected.TrustedStep!.SuccessorSession!;
        Assert.Equal("select-contact-assault", session.CurrentCombatDecision!.Decision.Choice);

        session = Accept(CampaignHostAdapter.CompleteNativeCombatBarrage(session));
        var rba = CampaignExercises.QueryContactRbaCut(session);
        Assert.Equal("commonwealth-truck", rba.DefenderElementId);
        session = Accept(CampaignHostAdapter.SubmitContactDefenderRbaDecline(session,
            Command(session, CampaignActionAudience.Commonwealth, rba.DeclineOffer.ActionId), rba.DefenderElementId));

        // No payable role pair: the defender has no Force slot, so the round cannot complete.
        Assert.False(CampaignHostAdapter.CanCompleteNativeCombatForceAssignment(session));
        Assert.Null(CampaignCombatAudienceDecisions.QueryForceAssignment(session,
            CampaignActionAudience.Commonwealth, rba.DefenderElementId));

        // The same System step the Host appends before opening a Force round (no clock evidence).
        var cancelled = CampaignHostAdapter.CancelNativeCombatForceAssignment(session, null,
            -1, -1, -1, null, "force-unavailable");
        Assert.True(cancelled.IsAccepted, cancelled.RejectionReason.ToString());
        session = cancelled.SuccessorSession!;
        Assert.Equal("force-unavailable", session.CurrentCombatForceCancellation!.Reason);
        Assert.Empty(session.CurrentCombatForceSeals);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        Assert.Null(CampaignCombatAudienceDecisions.QueryForceAssignment(session,
            CampaignActionAudience.Axis, Attacker));
        Assert.Null(CampaignCombatAudienceDecisions.QueryForceAssignment(session,
            CampaignActionAudience.Commonwealth, rba.DefenderElementId));

        session = Accept(CampaignHostAdapter.CompleteNativeCombatAntiArmor(session));
        session = Accept(CampaignHostAdapter.CompleteNativeCombatCancelledAssault(session));
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        var release = CampaignExercises.QueryNativeReserveReleaseState(session);
        Assert.Equal(LandSegmentIds.ReserveRelease, release.Position.SegmentId);
    }

    [Fact]
    public void AttackerForceDecisionBeforeTheSystemCancellationIsRecordedBehavior()
    {
        // Sequencing note for Exercise/Runner adopters (022): the attacker's own Force cut still
        // projects before the System force-unavailable step is appended. The Host appends that
        // step before opening any Force round; an in-process driver must do the same.
        var session = AdvanceToTruckForce();
        Assert.False(CampaignHostAdapter.CanCompleteNativeCombatForceAssignment(session));
        var attacker = CampaignCombatAudienceDecisions.QueryForceAssignment(session,
            CampaignActionAudience.Axis, Attacker);
        Assert.NotNull(attacker);
    }

    private static ExerciseSession AdvanceToTruckForce()
    {
        var session = AdvanceToTruckCombat();
        var selection = CampaignCombatAudienceDecisions.QuerySelection(session, CampaignActionAudience.Axis, Attacker)!;
        session = CampaignCombatAudienceDecisions.SubmitSelection(session, selection.Choose(0))
            .TrustedStep!.SuccessorSession!;
        session = Accept(CampaignHostAdapter.CompleteNativeCombatBarrage(session));
        var rba = CampaignExercises.QueryContactRbaCut(session);
        return Accept(CampaignHostAdapter.SubmitContactDefenderRbaDecline(session,
            Command(session, CampaignActionAudience.Commonwealth, rba.DeclineOffer.ActionId), rba.DefenderElementId));
    }

    private static ExerciseSession AdvanceToTruckCombat()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions
            .Single(value => value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
        var request = new CampaignCreationRequest(1, "campaign-q8-force-unavailable",
            Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.SetupHash,
            setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId);
        var session = CampaignExercises.Begin(request).Session!;
        for (var index = 0; index < 80; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.PhaseId == LandPhaseIds.MovementAndCombat
                && position.SegmentId == LandSegmentIds.Combat
                && position.StepId == LandStepIds.PositionDetermination)
                return session;
            var actionSet = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .First(value => value?.Candidates.Count > 0)!;
            var action = position.SegmentId == LandSegmentIds.Movement
                ? actionSet.Candidates.OfType<MoveElementAction>().FirstOrDefault(value =>
                    value.ElementId == Attacker && value.DestinationLocationId == "north")
                    ?? actionSet.Candidates[0]
                : actionSet.Candidates[0];
            session = Accept(CampaignExercises.Submit(session, new CampaignActionSubmission(
                CampaignActionSubmission.CurrentContractVersion, actionSet.CampaignId,
                actionSet.StateVersion, actionSet.PositionId, actionSet.Audience, action.ActionId)));
        }
        throw new InvalidOperationException("Combat position was not reached.");
    }

    private static CampaignActionSubmission Command(ExerciseSession session, CampaignActionAudience audience, string action)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new(1, checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience, action);
    }

    private static ExerciseSession Accept(ExerciseStepResult result)
    {
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        return result.SuccessorSession!;
    }
}
