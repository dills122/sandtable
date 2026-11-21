using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Hosting;
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
//   - 022C: after a result with no required retreat, the System no-retreat completion, World
//     settlement, the captor's Custody choice or the System no-custody completion, and the
//     successor; then, after any result, cancelled assault or no-attack completion, each pending
//     Reserve Release member's owner choice and the System Reserve Release completion; then the
//     acting side's Cycle Control choice.
//   - the cut right after the last covered step offers nothing (the harness frontier): after Cycle
//     Control (Repeat leads to repeated Movement, a contract-v2 stage; Finish to the Truck Convoy
//     pass), and a result that requires a post-assault retreat (a contract-v2 stage).
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
        Custody,
        ReserveRelease,
        CycleControl,
        SecondPlayerPhase,
        SecondSelection,
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
            CombatExerciseOwnerChoiceAction { Stage: CombatExerciseOwnerChoiceAction.CustodyStage } custody =>
                WithResultingCheckpoint(CampaignHostAdapter.SubmitNativeCombatCustody(session, submission,
                    custody.SubjectId)),
            CombatExerciseOwnerChoiceAction { Stage: CombatExerciseOwnerChoiceAction.ReserveReleaseStage } release =>
                WithResultingCheckpoint(CampaignHostAdapter.SubmitNativeReserveReleaseChoice(session, submission,
                    release.SubjectId)),
            CombatExerciseOwnerChoiceAction { Stage: CombatExerciseOwnerChoiceAction.CycleControlStage } =>
                WithResultingCheckpoint(CampaignHostAdapter.SubmitNativeCycleControl(session, submission)),
            _ => WithResultingCheckpoint(offered.Step!),
        };
    }

    // T022-Q7 (open, author choice for owner review): some receipts report the cut the step was taken
    // at as ResultingPositionId, while the successor's checkpoint is the next position: the owner
    // decisions V12/V47 Selection, V15 RBA decline, V17 Force seal and V45 second-side Movement
    // completion, and the System V26 successor. The
    // other covered steps' receipts already name the successor checkpoint and pass through unchanged.
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
            || session.SourceTurn is not null
            // The second side's own assault runtime is classified by SecondPlayer below.
            || (session.CurrentRepeatAssault is not null && session.CurrentSecondAssault is null)
            || session.CurrentPostRepeat is not null
            || session.Context.ArtifactV6 is null)
            return new(Stage.None);
        // 022D: after the first side's Cycle Control Finish, the second player's phase.
        if (session.CurrentCycleControl is { Choice: CampaignNativeCycleControlChoiceV28.Finish })
            return SecondPlayer(session);
        if (session.CurrentSecondBreakdown is not null) return new(Stage.None);
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
            return session.CurrentNoAttackCompletion is null
                ? new(Stage.System, CampaignExercises.CompleteNativeCombatNoAttack,
                    CombatExerciseSystemAction.NoAttackKind)
                : AfterCombat(session, stateVersion);
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
            return session.CurrentCombatCancelledAssault is null
                ? new(Stage.System, CampaignExercises.CompleteNativeCombatCancelledAssault,
                    CombatExerciseSystemAction.CancelledAssaultKind)
                : AfterCombat(session, stateVersion);
        if (session.CurrentCombatAssaultResult is not { } result)
            return new(Stage.System, CampaignExercises.ResolveNativeCombatAssault,
                CombatExerciseSystemAction.AssaultResultKind);
        if (session.CurrentCombatPostResultRetreat is null)
            // A required post-assault retreat is a contract-v2 owner stage: not covered.
            return result.Facts.RequiredRetreat == 0
                ? new(Stage.System, CampaignExercises.CompleteNoPostResultRetreat,
                    CombatExerciseSystemAction.NoRetreatKind)
                : AtFrontier(result.StateVersion);
        if (session.CurrentCombatSettlement is not { } settlement)
            return new(Stage.System, CampaignExercises.SettleNativeCombatWorld,
                CombatExerciseSystemAction.SettlementKind);
        if (session.CurrentCombatCustodyDisposition is null)
            return settlement.PendingCaptives.Count == 0
                ? new(Stage.System, CampaignExercises.CompleteNoNativeCombatCustody,
                    CombatExerciseSystemAction.NoCustodyKind)
                : new(Stage.Custody);
        return session.CurrentCombatSuccessor is null
            ? new(Stage.System, CampaignExercises.CompleteNativeCombatSuccessor,
                CombatExerciseSystemAction.SuccessorKind)
            : AfterCombat(session, stateVersion);
    }

    // 022D: the second player's phase after the first side finishes, in the Host's order: Truck
    // Convoy pass, post-Combat pass, second-side Reserve designation, second-side Movement and its
    // completion, the System second-side Breakdown, second-side Combat Selection (V47), then the
    // System no-attack completion after a finish. A second-side assault has no Core continuation
    // (Barrage onward is out of scope there, as on the Host), and the second side's no-attack
    // completion does not open Reserve Release; both stop at an empty frontier.
    private static Cut SecondPlayer(ExerciseSession session)
    {
        if (session.CurrentSecondCombatDecision is not null)
            return CampaignHostAdapter.IsNativeSecondCombatVoluntaryFinish(session)
                && session.CurrentSecondNoAttackCompletion is null
                    ? new(Stage.System, CampaignExercises.CompleteSecondCombatNoAttack,
                        CombatExerciseSystemAction.SecondNoAttackKind)
                    : new(Stage.Frontier);
        if (session.CurrentSecondBreakdown is not null) return new(Stage.SecondSelection);
        if (session.CurrentSecondMovementCompletion is not null)
            return new(Stage.System, CampaignExercises.CompleteSecondBreakdown,
                CombatExerciseSystemAction.SecondBreakdownKind);
        return new(Stage.SecondPlayerPhase);
    }

    // Reserve Release, then Cycle Control, in the order the Host opens them
    // (AppendEmptyReserveRelease, then the Cycle Control round).
    private static Cut AfterCombat(ExerciseSession session, long stateVersion)
    {
        if (session.CurrentCycleControl is { } control)
            return control.StateVersion == stateVersion ? new(Stage.Frontier) : new(Stage.None);
        CampaignNativeReserveReleaseStateV27 release;
        try { release = CampaignExercises.QueryNativeReserveReleaseState(session); }
        catch (InvalidOperationException) { return new(Stage.None); }
        if (release.Completed) return new(Stage.CycleControl);
        return release.Pending(session.Context).Count > 0
            ? new(Stage.ReserveRelease)
            : new(Stage.System, CampaignExercises.CompleteNativeReserveRelease,
                CombatExerciseSystemAction.ReserveReleaseKind);
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
            Stage.Custody => CustodyOffers(session, side),
            Stage.ReserveRelease => ReserveReleaseOffers(session, side),
            Stage.CycleControl => CycleControlOffers(session, side),
            Stage.SecondPlayerPhase => SecondPlayerOffers(session, audience),
            Stage.SecondSelection => SelectionOffers(session, side),
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

    // The bound captor only (CampaignHostAdapter.QueryContactCustodyOwnerDraft's rule).
    private static List<(CampaignActionCandidate, ExerciseStepResult?)> CustodyOffers(
        ExerciseSession session, LandSide side)
    {
        if (CampaignExercises.QueryNativeCombatCustodyCut(session) is not { } cut) return [];
        var captorSide = session.Context.ArtifactV6!.Definition.LegacyDefinition.Elements
            .Single(value => value.ElementId == cut.CaptorElementId).SideId;
        if (captorSide != CampaignSnapshotSerializer.FormatSide(side)) return [];
        return cut.Choices.Select(choice => ((CampaignActionCandidate)new CombatExerciseOwnerChoiceAction(
                CombatExerciseOwnerChoiceAction.CustodyStage, choice.Kind, cut.CaptorElementId, choice.ActionId),
            (ExerciseStepResult?)null)).ToList();
    }

    // The next ordered pending member of the acting side, as the Host opens Reserve Release.
    private static List<(CampaignActionCandidate, ExerciseStepResult?)> ReserveReleaseOffers(
        ExerciseSession session, LandSide side)
    {
        var release = CampaignExercises.QueryNativeReserveReleaseState(session);
        if (release.ActingSide != side) return [];
        var member = release.Pending(session.Context)[0];
        var cut = CampaignReserveReleaseOwnerCutV27.Project(release, session.Context, side, member.ElementId);
        return member.Choices.Zip(cut.ActionIds, (choice, actionId) => ((CampaignActionCandidate)
                new CombatExerciseOwnerChoiceAction(CombatExerciseOwnerChoiceAction.ReserveReleaseStage,
                    Kebab(choice.ToString()), member.ElementId, actionId), (ExerciseStepResult?)null))
            .ToList();
    }

    private static List<(CampaignActionCandidate, ExerciseStepResult?)> CycleControlOffers(
        ExerciseSession session, LandSide side)
    {
        var release = CampaignExercises.QueryNativeReserveReleaseState(session);
        if (release.ActingSide != side) return [];
        var cut = CampaignCycleControlOwnerCutV28.Project(release, session.Context, session.CurrentContacts, side);
        return cut.Choices.Zip(cut.ActionIds, (choice, actionId) => ((CampaignActionCandidate)
                new CombatExerciseOwnerChoiceAction(CombatExerciseOwnerChoiceAction.CycleControlStage,
                    Kebab(choice.ToString()), CombatExerciseOwnerChoiceAction.CycleControlStage, actionId),
                (ExerciseStepResult?)null))
            .ToList();
    }

    // Second player's owner steps. Each draft's offer is kept only if the unchanged trusted step
    // accepts it for this audience (the step is pure over the immutable session), so the offer set
    // and its owning side are exactly what Core admits.
    private static List<(CampaignActionCandidate, ExerciseStepResult?)> SecondPlayerOffers(
        ExerciseSession session, CampaignActionAudience audience)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        CampaignActionSubmission Command(string actionId) => new(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience, actionId);
        var offers = new List<(CampaignActionCandidate, ExerciseStepResult?)>();
        void Offer(Func<IEnumerable<(CampaignActionCandidate Candidate, Func<CampaignActionSubmission, ExerciseStepResult> Submit)>> draft)
        {
            try
            {
                foreach (var (candidate, submit) in draft())
                {
                    var step = submit(Command(candidate.ActionId));
                    if (step.IsAccepted && step.Evidence!.EventRecords.Count == 1)
                        offers.Add((candidate, step));
                }
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                // That step is not open at this cut for this audience.
            }
        }
        Offer(() => CampaignHostAdapter.QueryTruckConvoyPassOwnerDraft(session, audience).PrivateActionIds
            .Select(id => ((CampaignActionCandidate)new CombatExerciseOwnerChoiceAction(
                CombatExerciseOwnerChoiceAction.TruckConvoyPassStage, "pass", CombatExerciseOwnerChoiceAction.TruckConvoyPassStage, id),
                (Func<CampaignActionSubmission, ExerciseStepResult>)(command =>
                    CampaignHostAdapter.SubmitTruckConvoyPass(session, command)))));
        Offer(() => CampaignHostAdapter.QueryPostCombatPassOwnerDraft(session, audience).PrivateActionIds
            .Select(id => ((CampaignActionCandidate)new CombatExerciseOwnerChoiceAction(
                CombatExerciseOwnerChoiceAction.PostCombatPassStage, "finish-without-activity",
                CombatExerciseOwnerChoiceAction.PostCombatPassStage, id),
                (Func<CampaignActionSubmission, ExerciseStepResult>)(command =>
                    CampaignHostAdapter.SubmitPostCombatPass(session, command)))));
        // Second-side Reserve offers are the ordinary typed Reserve designation candidates, so the
        // Runner's Reserve policies apply to them unchanged.
        var reserveCandidates = session.CurrentSnapshot.World.Elements
            .Select(value => (CampaignActionCandidate)new DesignateReserveAction(value.ElementId))
            .Append(new CompleteReserveDesignationAction())
            .ToDictionary(value => value.ActionId, StringComparer.Ordinal);
        Offer(() => CampaignHostAdapter.QuerySecondReserveOwnerDraft(session, audience).PrivateActionIds
            .Where(reserveCandidates.ContainsKey)
            .Select(id => (reserveCandidates[id],
                (Func<CampaignActionSubmission, ExerciseStepResult>)(command =>
                    CampaignHostAdapter.SubmitSecondReserve(session, command)))));
        foreach (var element in session.CurrentSnapshot.World.Elements.Select(value => value.ElementId)
            .Order(StringComparer.Ordinal))
            Offer(() => CampaignHostAdapter.QuerySecondMovementOwnerDraft(session, audience, element).PrivateActionIds
                .Select(id => ((CampaignActionCandidate)new CombatExerciseOwnerChoiceAction(
                    CombatExerciseOwnerChoiceAction.SecondMovementStage, "move", element, id),
                    (Func<CampaignActionSubmission, ExerciseStepResult>)(command =>
                        CampaignHostAdapter.SubmitSecondMovement(session, command, element)))));
        Offer(() => CampaignHostAdapter.QuerySecondMovementCompletionOwnerDraft(session, audience).PrivateActionIds
            .Select(id => ((CampaignActionCandidate)new CombatExerciseOwnerChoiceAction(
                CombatExerciseOwnerChoiceAction.SecondMovementCompletionStage, "complete-movement",
                CombatExerciseOwnerChoiceAction.SecondMovementCompletionStage, id),
                (Func<CampaignActionSubmission, ExerciseStepResult>)(command =>
                    CampaignHostAdapter.SubmitSecondMovementCompletion(session, command)))));
        return offers;
    }

    // "ConvertToII" -> "convert-to-ii", "Finish" -> "finish".
    private static string Kebab(string value)
    {
        var builder = new System.Text.StringBuilder();
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (index > 0 && char.IsUpper(current) && !char.IsUpper(value[index - 1])) builder.Append('-');
            builder.Append(char.ToLowerInvariant(current));
        }
        return builder.ToString();
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
