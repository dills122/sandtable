using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Exercises;

// Task 022A (issue #448): first-cycle Combat steps for the trusted in-process Exercise harness.
//
// Test-harness capability only: not Combat activation or certification, and independent of the
// Task 021 production activation gate. The Host never calls this path; it keeps its own owner
// openings and System orchestration, and CampaignExercises.Query/Submit are unchanged.
//
// Every step delegates to the existing trusted Core step (the same ones CampaignHostAdapter wraps
// and the frozen combat-audience-decision-v1 surface delegates to), so adjudication, events,
// snapshots and receipts are unchanged. Slice 022A covers:
//   - first-side Selection at Combat Position Determination: the union of every acting-side
//     element's assault and finish offers (an owner may open Selection for any own element; the
//     first accepted one closes the cut);
//   - the System step the Host appends right after Selection: no-attack completion after a finish,
//     Barrage after an assault;
//   - the cut immediately after that System step, which offers nothing (the harness frontier).
// Anything else returns null, and the caller keeps the ordinary behavior.
internal static class CampaignCombatExerciseActions
{
    private enum Range
    {
        None,
        Selection,
        NoAttack,
        Barrage,
        Frontier,
    }

    internal static CampaignLegalActionQueryResult? Query(ExerciseSession session,
        CampaignActionAudience audience)
    {
        var range = Classify(session);
        if (range == Range.None || !Enum.IsDefined(audience)) return null;
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        var candidates = Candidates(session, range, audience).Select(value => value.Candidate).ToArray();
        return CampaignLegalActionQueryResult.Success(new CampaignLegalActionSet(checkpoint.CampaignId,
            checkpoint.StateVersion, checkpoint.RulesetHash, checkpoint.PositionId, audience, candidates,
            CampaignLegalActionSet.CombatExercisePolicyId));
    }

    internal static ExerciseStepResult? Submit(ExerciseSession session, CampaignActionSubmission submission)
    {
        var range = Classify(session);
        if (range == Range.None) return null;
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var offered = Candidates(session, range, submission.Audience)
            .SingleOrDefault(value => value.Candidate.ActionId == submission.ActionId);
        if (offered.Candidate is null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        return offered.Candidate switch
        {
            SelectContactAssaultAction assault => WithResultingCheckpoint(
                CampaignExercises.SubmitContactOwnerCombatSelection(session, submission, assault.ActingElementId)),
            FinishCombatWithoutAttackAction finish => WithResultingCheckpoint(
                CampaignExercises.SubmitContactOwnerCombatSelection(session, submission, finish.ActingElementId)),
            _ => offered.Step!,
        };
    }

    // T022-Q7 (open, author choice for owner review): the existing V12 Selection receipt reports the
    // decision's own position (Position Determination) as ResultingPositionId, while the successor's
    // checkpoint is the decision's NextPositionId (Barrage). Every other Combat step's receipt names
    // its next position. The Host persists and returns the V12 receipt, so it is left unchanged there;
    // this harness path alone re-issues the receipt with the successor checkpoint position so the
    // Runner's checkpoint-continuity invariant holds. Event, snapshot checkpoint, successor session,
    // versions, audience and action ID are the unchanged trusted step's.
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

    private static Range Classify(ExerciseSession session)
    {
        if (session is null || session.Pregame is not null || session.SourceAuthority is not null
            || session.SourceTurn is not null || session.CurrentSecondBreakdown is not null
            || session.Context.ArtifactV6 is null)
            return Range.None;
        if (session.CurrentCombatDecision is not { } selected)
            return IsFirstSideCombatEntry(session.CurrentSnapshot)
                // Only where the ordinary legal-action query offers nothing to anyone.
                && Enum.GetValues<CampaignActionAudience>().All(audience =>
                    CampaignExercises.Query(session, audience) is { IsSuccessful: true } ordinary
                    && ordinary.ActionSet!.Candidates.Count == 0)
                ? Range.Selection : Range.None;
        var stateVersion = CampaignExercises.QueryCheckpoint(session).StateVersion;
        return selected.Decision.Choice switch
        {
            "finish-combat-without-attack" when session.CurrentNoAttackCompletion is null => Range.NoAttack,
            "finish-combat-without-attack" when session.CurrentNoAttackCompletion?.StateVersion == stateVersion
                => Range.Frontier,
            "select-contact-assault" when session.CurrentNoAttackCompletion is null
                && session.CurrentRepeatAssault is null
                && session.CurrentCombatBarrage is null => Range.Barrage,
            "select-contact-assault" when session.CurrentCombatBarrage?.StateVersion == stateVersion
                => Range.Frontier,
            _ => Range.None,
        };
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
        ExerciseSession session, Range range, CampaignActionAudience audience)
    {
        switch (range)
        {
            case Range.Selection when audience is CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth:
                return SelectionOffers(session, audience == CampaignActionAudience.Axis
                    ? LandSide.Axis : LandSide.Commonwealth);
            case Range.NoAttack when audience == CampaignActionAudience.System:
                return SystemStep(CampaignExercises.CompleteNativeCombatNoAttack(session),
                    bytes => new CompleteCombatNoAttackAction(bytes));
            case Range.Barrage when audience == CampaignActionAudience.System:
                return SystemStep(CampaignExercises.CompleteNativeCombatBarrage(session),
                    bytes => new CompleteCombatBarrageAction(bytes));
            default:
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
        ExerciseStepResult step, Func<byte[], CampaignActionCandidate> candidate)
    {
        // The step is pure over the immutable session; the offered action ID is its receipt ID.
        if (!step.IsAccepted || step.Evidence!.EventRecords.Count != 1) return [];
        var offer = candidate(step.Evidence.EventRecords[0]);
        return offer.ActionId == step.Evidence.Receipt.ActionId ? [(offer, step)] : [];
    }
}
