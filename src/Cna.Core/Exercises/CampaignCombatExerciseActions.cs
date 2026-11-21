using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Exercises;

// Tasks 022A/022B (issue #448): first-cycle Combat steps for the trusted in-process Exercise harness.
//
// Test-harness capability only: not Combat activation or certification, and independent of the
// Task 021 production activation gate. The Host never calls this path; it keeps its own owner
// openings and System orchestration, and CampaignExercises.Query/Submit are unchanged.
//
// Every step delegates to the existing trusted Core step (the same ones CampaignHostAdapter wraps
// and the frozen combat-audience-decision-v1 surface delegates to), so adjudication, events and
// snapshots are unchanged. System steps follow the Host's own orchestration order
// (CampaignDurableAuthorityJournal.Submissions/ForceRounds). Covered, first side, first cycle:
//   - 022A: Selection at Combat Position Determination: the union of every acting-side element's
//     assault and finish offers (an owner may open Selection for any own element; the first
//     accepted one closes the cut); then the System no-attack completion or Barrage.
//   - 022B: decline-only RBA (bound defender); Force Assignment: the V19 force-unavailable System
//     cancellation when the round cannot be paid (the Host's pre-opening check), otherwise both
//     owners' seals, offered together because they are hidden and order-independent (020A1), then
//     the System Force completion; System Anti Armor; then the V21 cancelled assault or the V22
//     assault result.
//   - the cut right after the last covered step offers nothing (the harness frontier): Reserve
//     Release after a no-attack or cancelled assault, and the post-result retreat after a result.
// Anything else (repeat cycles, second side, timeouts, RBA movement) returns null, and the caller
// keeps the ordinary behavior.
internal static class CampaignCombatExerciseActions
{
    private enum Stage
    {
        None,
        Selection,
        RetreatBeforeAssault,
        ForceAssignment,
        System,
        Frontier,
    }

    private readonly record struct Cut(Stage Stage, Func<ExerciseSession, ExerciseStepResult>? Step = null,
        string? StepKind = null);

    internal static CampaignLegalActionQueryResult? Query(ExerciseSession session,
        CampaignActionAudience audience)
    {
        var cut = Classify(session);
        if (cut.Stage == Stage.None || !Enum.IsDefined(audience)) return null;
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        var candidates = Candidates(session, cut, audience).Select(value => value.Candidate).ToArray();
        return CampaignLegalActionQueryResult.Success(new CampaignLegalActionSet(checkpoint.CampaignId,
            checkpoint.StateVersion, checkpoint.RulesetHash, checkpoint.PositionId, audience, candidates,
            CampaignLegalActionSet.CombatExercisePolicyId));
    }

    internal static ExerciseStepResult? Submit(ExerciseSession session, CampaignActionSubmission submission)
    {
        var cut = Classify(session);
        if (cut.Stage == Stage.None) return null;
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var offered = Candidates(session, cut, submission.Audience)
            .SingleOrDefault(value => value.Candidate.ActionId == submission.ActionId);
        if (offered.Candidate is null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return offered.Candidate switch
        {
            SelectContactAssaultAction assault => WithResultingCheckpoint(
                CampaignExercises.SubmitContactOwnerCombatSelection(session, submission, assault.ActingElementId)),
            FinishCombatWithoutAttackAction finish => WithResultingCheckpoint(
                CampaignExercises.SubmitContactOwnerCombatSelection(session, submission, finish.ActingElementId)),
            DeclineRetreatBeforeAssaultAction decline => WithResultingCheckpoint(
                CampaignExercises.SubmitContactDefenderRbaDecline(session, submission, decline.ElementId)),
            SealCombatForceAssignmentAction seal => WithResultingCheckpoint(
                CampaignExercises.SubmitContactOwnerForceSeal(session, submission, seal.ElementId)),
            _ => offered.Step!,
        };
    }

    // T022-Q7 (open, author choice for owner review): owner-decision receipts (V12 Selection, V15 RBA
    // decline, V17 Force seal) report the cut the decision was taken at as ResultingPositionId, while
    // the successor's checkpoint is the next position. System-step receipts name the next position.
    // The Host persists and returns these receipts, so they are left unchanged there; this harness
    // path alone re-issues an owner receipt with the successor checkpoint position so the Runner's
    // checkpoint-continuity invariant holds. Event, snapshot checkpoint, successor session, versions,
    // audience and action ID are the unchanged trusted step's.
    private static ExerciseStepResult WithResultingCheckpoint(ExerciseStepResult step)
    {
        if (!step.IsAccepted) return step;
        var evidence = step.Evidence!;
        var receipt = evidence.Receipt;
        var resulting = CampaignExercises.QueryCheckpoint(step.SuccessorSession!).PositionId;
        if (receipt.ResultingPositionId == resulting) return step;
        return ExerciseStepResult.Accepted(step.SuccessorSession!, new ExerciseStepEvidence(
            new CampaignActionAcceptanceReceipt(receipt.CampaignId, receipt.PriorStateVersion,
                receipt.CommittedStateVersion, resulting, receipt.Audience, receipt.ActionId),
            evidence.EventRecords[0], evidence.SnapshotCheckpoint));
    }

    private static Cut Classify(ExerciseSession session)
    {
        if (session is null || session.Pregame is not null || session.SourceAuthority is not null
            || session.SourceTurn is not null || session.CurrentSecondBreakdown is not null
            || session.CurrentRepeatAssault is not null || session.CurrentPostRepeat is not null
            || session.Context.ArtifactV6 is null)
            return new(Stage.None);
        if (session.CurrentCombatDecision is not { } selected)
            return IsFirstSideCombatEntry(session.CurrentSnapshot)
                // Only where the ordinary legal-action query offers nothing to anyone.
                && Enum.GetValues<CampaignActionAudience>().All(audience =>
                    CampaignExercises.Query(session, audience) is { IsSuccessful: true } ordinary
                    && ordinary.ActionSet!.Candidates.Count == 0)
                ? new(Stage.Selection) : new(Stage.None);
        var stateVersion = CampaignExercises.QueryCheckpoint(session).StateVersion;
        Cut AtFrontier(long lastVersion) =>
            lastVersion == stateVersion ? new(Stage.Frontier) : new(Stage.None);
        if (selected.Decision.Choice == "finish-combat-without-attack")
            return session.CurrentNoAttackCompletion is { } completion
                ? AtFrontier(completion.StateVersion)
                : new(Stage.System, CampaignExercises.CompleteNativeCombatNoAttack,
                    CombatExerciseSystemAction.NoAttackKind);
        if (selected.Decision.Choice != "select-contact-assault" || session.CurrentNoAttackCompletion is not null)
            return new(Stage.None);
        if (session.CurrentCombatBarrage is null)
            return new(Stage.System, CampaignExercises.CompleteNativeCombatBarrage,
                CombatExerciseSystemAction.BarrageKind);
        // RBA movement (excluded, CMB-POL-001) and RBA/Force timeouts need a clock; not covered.
        if (session.CurrentCombatRetreatMove is not null || session.CurrentCombatRbaCancellation is not null)
            return new(Stage.None);
        if (session.CurrentCombatRetreatDecline is null)
            return new(Stage.RetreatBeforeAssault);
        if (session.CurrentCombatForceCompletion is null && session.CurrentCombatForceCancellation is null)
        {
            var seals = session.CurrentCombatForceSeals.Count;
            // The Host's pre-opening check (InitializeForceRound): an unpayable round is cancelled
            // force-unavailable before any seal, with no clock evidence.
            if (seals == 0 && !CampaignExercises.CanCompleteNativeCombatForceAssignment(session))
                return new(Stage.System, value => CampaignExercises.CancelNativeCombatForceAssignment(value, null,
                    -1, -1, -1, null, "force-unavailable"), CombatExerciseSystemAction.ForceUnavailableKind);
            return seals < 2
                ? new(Stage.ForceAssignment)
                : new(Stage.System, CampaignExercises.CompleteNativeCombatForceAssignment,
                    CombatExerciseSystemAction.ForceCompletionKind);
        }
        if (session.CurrentCombatAntiArmor is null)
            return new(Stage.System, CampaignExercises.CompleteNativeCombatAntiArmor,
                CombatExerciseSystemAction.AntiArmorKind);
        if (session.CurrentCombatForceCancellation is not null)
            return session.CurrentCombatCancelledAssault is { } cancelled
                ? AtFrontier(cancelled.StateVersion)
                : new(Stage.System, CampaignExercises.CompleteNativeCombatCancelledAssault,
                    CombatExerciseSystemAction.CancelledAssaultKind);
        return session.CurrentCombatAssaultResult is { } result
            ? AtFrontier(result.StateVersion)
            : new(Stage.System, CampaignExercises.ResolveNativeCombatAssault,
                CombatExerciseSystemAction.AssaultResultKind);
    }

    private static bool IsFirstSideCombatEntry(CampaignSnapshotV11 snapshot)
    {
        var position = snapshot.CurrentPosition.SequencePosition;
        return snapshot.CurrentPosition.Kind == CampaignPositionV11Kind.Sequence
            && position is not null
            && position.PhaseId == LandPhaseIds.MovementAndCombat
            && position.SegmentId == LandSegmentIds.Combat
            && position.StepId == LandStepIds.PositionDetermination
            && position.ActorRole == LandActorRole.FirstActingSide
            && snapshot.ReactionWindow is null
            && snapshot.BreakdownFlow is CampaignBreakdownFlow.Idle;
    }

    private static IEnumerable<(CampaignActionCandidate Candidate, ExerciseStepResult? Step)> Candidates(
        ExerciseSession session, Cut cut, CampaignActionAudience audience)
    {
        if (cut.Stage == Stage.System)
            return audience == CampaignActionAudience.System
                ? SystemStep(cut.Step!(session), cut.StepKind!) : [];
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth)) return [];
        var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        return cut.Stage switch
        {
            Stage.Selection => SelectionOffers(session, side),
            Stage.RetreatBeforeAssault => RbaDeclineOffer(session, side),
            Stage.ForceAssignment => ForceSealOffer(session, side),
            _ => [],
        };
    }

    // Decline only: Core's RBA move offers are never offered (CMB-POL-001, T020-Q7).
    private static List<(CampaignActionCandidate, ExerciseStepResult?)> RbaDeclineOffer(
        ExerciseSession session, LandSide side)
    {
        try
        {
            var cut = CampaignExercises.QueryContactRbaCut(session);
            return cut.DefenderSide == side ? [(cut.DeclineOffer, null)] : [];
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return [];
        }
    }

    // Each owner without a seal gets its own seal offer; the other owner's seal never changes it.
    private static List<(CampaignActionCandidate, ExerciseStepResult?)> ForceSealOffer(
        ExerciseSession session, LandSide side)
    {
        if (session.CurrentCombatForceSeals.Any(value => value.OwnerSide == side)) return [];
        try
        {
            var cut = CampaignExercises.QueryContactForceAssignmentCut(session, side);
            return cut.OwnerSide == side ? [(cut.SealOffer, null)] : [];
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return [];
        }
    }

    private static List<(CampaignActionCandidate, ExerciseStepResult?)> SelectionOffers(
        ExerciseSession session, LandSide side)
    {
        var offers = new List<(CampaignActionCandidate, ExerciseStepResult?)>();
        foreach (var element in session.CurrentSnapshot.World.Elements
            .Select(value => value.ElementId).Order(StringComparer.Ordinal))
        {
            try
            {
                var cut = CampaignExercises.QueryContactOwnerCombatCut(session, side, element);
                offers.AddRange(cut.AssaultOffers.Select(value => ((CampaignActionCandidate)value, (ExerciseStepResult?)null)));
                if (cut.FinishOffer is { } finish) offers.Add((finish, null));
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                // Not an acting-side element at this cut (wrong side, no contact cut): no offers.
            }
        }
        return offers;
    }

    private static IEnumerable<(CampaignActionCandidate, ExerciseStepResult?)> SystemStep(
        ExerciseStepResult step, string kind)
    {
        // The step is pure over the immutable session; the offered action ID is its receipt ID.
        if (!step.IsAccepted || step.Evidence!.EventRecords.Count != 1) return [];
        var offer = new CombatExerciseSystemAction(kind, step.Evidence.EventRecords[0]);
        return offer.ActionId == step.Evidence.Receipt.ActionId ? [(offer, step)] : [];
    }
}
