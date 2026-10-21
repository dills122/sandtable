using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Exercises;

public static class CampaignExercises
{
    public static ExerciseStartResult Begin(CampaignCreationRequest request)
    {
        var execution = CampaignCreationExecution.Execute(request);
        if (!execution.IsCreated)
            return ExerciseStartResult.Rejected(execution.RejectionReason);

        var session = new ExerciseSession(
            execution.CurrentSnapshot!,
            execution.Context!,
            [execution.CurrentCreatedEvent!],
            CampaignContactJournal.Initialize(execution.CurrentSnapshot!, execution.Context!));
        return ExerciseStartResult.Started(
            session,
            CampaignCurrentEventSerializer.Serialize(execution.CurrentCreatedEvent!),
            CampaignCurrentSnapshotSerializer.Serialize(execution.CurrentSnapshot!));
    }

    public static CampaignLegalActionQueryResult Query(
        ExerciseSession session,
        CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not null)
            return CampaignLegalActionQueryResult.Rejected(CampaignLegalActionQueryRejectionReason.InvalidState);
        return CampaignLegalActions.Query(
            new CampaignAuthorityHandle(session.CurrentSnapshot, session.Context),
            audience);
    }

    internal static CampaignContactOwnerMovementCut QueryContactOwnerMovementCut(
        ExerciseSession session, Cna.Core.Rules.LandSide side, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not null)
            throw new InvalidOperationException("Combat selection has already advanced this authority cut.");
        return CampaignContactOwnerMovementCut.Project(session.CurrentSnapshot, session.Context,
            session.CurrentContacts, side, actingElementId);
    }

    internal static CampaignContactCombatSelectionCut QueryContactOwnerCombatCut(
        ExerciseSession session, LandSide side, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not null)
            throw new InvalidOperationException("Combat selection has already advanced this authority cut.");
        return CampaignContactCombatSelectionCut.Project(session.CurrentSnapshot, session.Context,
            session.CurrentContacts, side, actingElementId);
    }

    internal static CampaignContactRetreatBeforeAssaultCut QueryContactRbaCut(
        ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatBarrage is not { } barrage
            || session.CurrentCombatRetreatDecline is not null)
            throw new InvalidOperationException("Defender retreat requires completed Barrage authority.");
        if (session.CurrentCombatRetreatMove is { } move)
            return CampaignContactRetreatBeforeAssaultCut.ProjectAfterMove(
                selected, barrage, move, session.CurrentCombatRetreatWorld!,
                session.CurrentCombatRetreatMoves[0].OriginLocationId,
                session.Context, session.CurrentContacts);
        return CampaignContactRetreatBeforeAssaultCut.Project(selected, barrage,
            session.Context, session.CurrentContacts);
    }

    internal static CampaignContactForceAssignmentCut QueryContactForceAssignmentCut(
        ExerciseSession session, LandSide ownerSide)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatRetreatDecline is not { } decline
            || session.CurrentCombatForceCompletion is not null
            || session.CurrentCombatForceSeals.Count == 2
            || session.CurrentCombatForceSeals.Any(value => value.OwnerSide == ownerSide))
            throw new InvalidOperationException("Owner Force Assignment seal is unavailable.");
        return CampaignContactForceAssignmentCut.Project(selected, decline,
            session.CurrentCombatRetreatWorld ?? selected.PriorSnapshot.World,
            session.Context, session.CurrentContacts, ownerSide);
    }

    public static ExerciseCheckpoint QueryCheckpoint(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatAssaultResult is { } result)
            return new ExerciseCheckpoint(result, session.CurrentCombatDecision!);
        if (session.CurrentCombatCancelledAssault is { } cancelledAssault)
            return new ExerciseCheckpoint(cancelledAssault, session.CurrentCombatDecision!);
        if (session.CurrentCombatAntiArmor is { } antiArmor)
            return new ExerciseCheckpoint(antiArmor, session.CurrentCombatDecision!);
        if (session.CurrentCombatForceCancellation is { } forceCancellation)
            return new ExerciseCheckpoint(forceCancellation, session.CurrentCombatDecision!);
        if (session.CurrentCombatForceCompletion is { } forceCompletion)
            return new ExerciseCheckpoint(forceCompletion, session.CurrentCombatDecision!);
        if (session.CurrentCombatForceSeals.Count > 0)
            return new ExerciseCheckpoint(session.CurrentCombatForceSeals[^1],
                session.CurrentCombatDecision!);
        if (session.CurrentCombatRetreatDecline is { } decline)
            return new ExerciseCheckpoint(decline, session.CurrentCombatDecision!);
        if (session.CurrentCombatRetreatMove is { } retreatMove)
            return new ExerciseCheckpoint(retreatMove, session.CurrentCombatDecision!);
        if (session.CurrentNoAttackCompletion is { } completion)
            return new ExerciseCheckpoint(completion, session.CurrentCombatDecision!);
        if (session.CurrentCombatBarrage is { } barrage)
            return new ExerciseCheckpoint(barrage, session.CurrentCombatDecision!);
        return session.CurrentCombatDecision is { } selected
            ? new ExerciseCheckpoint(selected)
            : new ExerciseCheckpoint(session.CurrentSnapshot);
    }

    public static ExerciseCheckpoint ReadCheckpoint(ReadOnlyMemory<byte> canonicalSnapshot) =>
        new(CampaignCurrentSnapshotSerializer.Deserialize(canonicalSnapshot));

    public static void ValidateCanonicalEvent(ReadOnlyMemory<byte> canonicalEvent) =>
        _ = CampaignCurrentEventSerializer.Deserialize(canonicalEvent);

    public static ExerciseStepResult Submit(
        ExerciseSession session,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.CurrentCombatDecision is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var execution = CampaignCurrentActionExecution.Execute(
            session.CurrentSnapshot,
            session.Context,
            submission);
        return Complete(session, execution);
    }

    internal static ExerciseStepResult SubmitContactOwnerMovement(
        ExerciseSession session, CampaignActionSubmission submission, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (session.CurrentCombatDecision is not null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var execution = CampaignCurrentActionExecution.ExecuteContactMovement(
            session.CurrentSnapshot, session.Context, session.CurrentContacts,
            submission, actingElementId);
        return Complete(session, execution);
    }

    internal static ExerciseStepResult SubmitContactOwnerCombatSelection(
        ExerciseSession session, CampaignActionSubmission submission, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (string.IsNullOrWhiteSpace(actingElementId)
            || !CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (session.CurrentCombatDecision is not null
            || session.Context.ArtifactV6 is not { } artifact
            || !CampaignSnapshotV11Admission.IsValid(session.CurrentSnapshot, artifact,
                session.Context.Scenario)
            || session.CurrentContacts.AuthorityVersion != session.CurrentSnapshot.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.CampaignId != session.CurrentSnapshot.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != session.CurrentSnapshot.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignCurrentActionExecution.CurrentPositionId(session.CurrentSnapshot))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var side = submission.Audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => (LandSide?)null,
        };
        if (side is null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        try
        {
            var cut = QueryContactOwnerCombatCut(session, side.Value, actingElementId);
            if (!cut.AssaultOffers.Any(offer => offer.ActionId == submission.ActionId)
                && cut.FinishOffer?.ActionId != submission.ActionId)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var selected = CampaignNativeCombatDecisionStateV12.Select(session.CurrentSnapshot,
                session.Context, session.CurrentContacts, side.Value, actingElementId,
                submission.ActionId);
            var contacts = session.CurrentContacts.AdvanceNativeCombatSelection(selected);
            var history = session.CurrentHistory.Add(selected.Decision);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                history, contacts, selected);
            var receipt = new CampaignActionAcceptanceReceipt(session.CurrentSnapshot.CampaignId,
                session.CurrentSnapshot.StateVersion, selected.StateVersion,
                selected.Decision.PositionId, submission.Audience, submission.ActionId);
            var evidence = new ExerciseStepEvidence(receipt,
                selected.Decision.SerializeCanonical(), selected.SerializeCanonical());
            return ExerciseStepResult.Accepted(successor, evidence);
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CompleteNativeCombatNoAttack(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentNoAttackCompletion is not null
            || session.CurrentContacts.AuthorityVersion != selected.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var completion = CampaignNativeCombatNoAttackCompletionV13.Create(selected, session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatNoAttackCompletion(completion);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(completion), contacts, selected, completion);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                selected.StateVersion, completion.StateVersion,
                completion.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(
                    completion.SerializeCanonical()))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                completion.SerializeCanonical(), completion.SerializeStateCanonical(selected)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CompleteNativeCombatBarrage(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentNoAttackCompletion is not null
            || session.CurrentCombatBarrage is not null
            || session.CurrentContacts.AuthorityVersion != selected.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var barrage = CampaignNativeCombatBarrageV14.Create(selected, session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatBarrage(barrage);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(barrage), contacts, selected, combatBarrage: barrage);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                selected.StateVersion, barrage.StateVersion, barrage.NextPositionId,
                CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(barrage.SerializeCanonical()))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                barrage.SerializeCanonical(), barrage.SerializeStateCanonical(selected)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult SubmitContactDefenderRbaDecline(
        ExerciseSession session, CampaignActionSubmission submission, string defenderElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (string.IsNullOrWhiteSpace(defenderElementId)
            || !CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatBarrage is not { } barrage
            || session.CurrentCombatRetreatDecline is not null
            || session.CurrentContacts.AuthorityVersion
                != (session.CurrentCombatRetreatMove?.StateVersion ?? barrage.StateVersion))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.CampaignId != selected.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion
            != (session.CurrentCombatRetreatMove?.StateVersion ?? barrage.StateVersion))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != barrage.NextPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        try
        {
            var cut = QueryContactRbaCut(session);
            var audience = cut.DefenderSide == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            if (submission.Audience != audience || defenderElementId != cut.DefenderElementId
                || submission.ActionId != cut.DeclineOffer.ActionId)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var decline = session.CurrentCombatRetreatMove is { } previousMove
                ? CampaignNativeCombatRetreatDeclineV15.CreateAfterMove(
                    selected, barrage, previousMove, session.CurrentCombatRetreatWorld!,
                    session.CurrentCombatRetreatMoves[0].OriginLocationId,
                    session.Context, session.CurrentContacts, submission.ActionId)
                : CampaignNativeCombatRetreatDeclineV15.Create(selected, barrage,
                    session.Context, session.CurrentContacts, submission.ActionId);
            var contacts = session.CurrentContacts.AdvanceNativeCombatRetreatDecline(decline);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(decline), contacts, selected,
                combatBarrage: barrage, combatRetreatDecline: decline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                decline.PriorStateVersion, decline.StateVersion, decline.PositionId,
                submission.Audience, submission.ActionId);
            var checkpoint = session.CurrentCombatRetreatMove is { } move
                ? decline.SerializeAfterMoveStateCanonical(
                    SerializeRetreatMoveState(session))
                : decline.SerializeStateCanonical(selected, barrage);
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                decline.SerializeCanonical(), checkpoint));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult SubmitContactOwnerForceSeal(
        ExerciseSession session, CampaignActionSubmission submission, string elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (string.IsNullOrWhiteSpace(elementId)
            || !CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatRetreatDecline is not { } decline
            || session.CurrentCombatForceCancellation is not null
            || session.CurrentCombatForceCompletion is not null
            || session.CurrentCombatForceSeals.Count == 2)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.CampaignId != selected.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != session.CurrentContacts.AuthorityVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != decline.NextPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var side = submission.Audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => (LandSide?)null,
        };
        if (side is null)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        try
        {
            var cut = QueryContactForceAssignmentCut(session, side.Value);
            if (cut.SealOffer.ElementId != elementId
                || cut.SealOffer.ActionId != submission.ActionId)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var first = session.CurrentCombatForceSeals.Count == 0
                ? null : session.CurrentCombatForceSeals[0];
            var seal = CampaignNativeCombatForceSealV17.Create(selected, decline,
                session.CurrentCombatRetreatWorld ?? selected.PriorSnapshot.World,
                session.Context, session.CurrentContacts, side.Value,
                submission.ActionId, first);
            var contacts = session.CurrentContacts.AdvanceNativeCombatForceSeal(seal);
            var seals = session.CurrentCombatForceSeals.Append(seal).ToArray();
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(seal), contacts, selected,
                combatBarrage: session.CurrentCombatBarrage,
                combatRetreatDecline: decline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves,
                combatForceSeals: seals);
            var priorState = SerializeRetreatDeclineState(session);
            foreach (var previous in session.CurrentCombatForceSeals)
                priorState = previous.SerializeStateCanonical(priorState);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                seal.PriorStateVersion, seal.StateVersion, seal.PositionId,
                submission.Audience, submission.ActionId);
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                seal.SerializeCanonical(), seal.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CompleteNativeCombatForceAssignment(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatRetreatDecline is not { } decline
            || session.CurrentCombatForceCancellation is not null
            || session.CurrentCombatForceSeals.Count != 2
            || session.CurrentCombatForceCompletion is not null
            || session.CurrentContacts.AuthorityVersion
                != session.CurrentCombatForceSeals[^1].StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var completion = CampaignNativeCombatForceCompletionV18.Create(selected,
                decline, session.CurrentCombatForceSeals);
            var contacts = session.CurrentContacts.AdvanceNativeCombatForceCompletion(completion);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(completion), contacts, selected,
                combatBarrage: session.CurrentCombatBarrage,
                combatRetreatDecline: decline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves,
                combatForceSeals: session.CurrentCombatForceSeals,
                combatForceCompletion: completion);
            var priorState = SerializeRetreatDeclineState(session);
            foreach (var seal in session.CurrentCombatForceSeals)
                priorState = seal.SerializeStateCanonical(priorState);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                completion.PriorStateVersion, completion.StateVersion,
                completion.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(completion.SerializeCanonical()))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                completion.SerializeCanonical(), completion.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CancelNativeCombatForceAssignment(
        ExerciseSession session, CampaignDecisionTimingConfigurationV1 configuration,
        long openedAtUnixMilliseconds, long deadlineUnixMilliseconds,
        long highWaterUnixMilliseconds, long? cancelledAtUnixMilliseconds,
        string reason)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(configuration);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatRetreatDecline is not { } decline
            || session.CurrentCombatForceSeals.Count > 1
            || session.CurrentCombatForceCompletion is not null
            || session.CurrentCombatForceCancellation is not null
            || configuration.RulesetHash != selected.PriorSnapshot.RulesetHash
            || openedAtUnixMilliseconds < 0
            || deadlineUnixMilliseconds - openedAtUnixMilliseconds
                != configuration.Budget("force-assignment"))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var cancellation = CampaignNativeCombatForceCancellationV19.Create(
                selected, decline, session.CurrentCombatForceSeals,
                configuration.Hash, openedAtUnixMilliseconds,
                deadlineUnixMilliseconds, highWaterUnixMilliseconds,
                cancelledAtUnixMilliseconds, reason);
            var contacts = session.CurrentContacts.AdvanceNativeCombatForceCancellation(cancellation);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(cancellation), contacts, selected,
                combatBarrage: session.CurrentCombatBarrage,
                combatRetreatDecline: decline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves,
                combatForceSeals: session.CurrentCombatForceSeals,
                combatForceCancellation: cancellation);
            var priorState = SerializeRetreatDeclineState(session);
            foreach (var seal in session.CurrentCombatForceSeals)
                priorState = seal.SerializeStateCanonical(priorState);
            var bytes = cancellation.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                cancellation.PriorStateVersion, cancellation.StateVersion,
                cancellation.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                bytes, cancellation.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CompleteNativeCombatAntiArmor(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not { } selected
            || (session.CurrentCombatForceCompletion is null)
                == (session.CurrentCombatForceCancellation is null)
            || session.CurrentCombatAntiArmor is not null
            || session.CurrentContacts.AuthorityVersion
                != (session.CurrentCombatForceCompletion?.StateVersion
                    ?? session.CurrentCombatForceCancellation!.StateVersion))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var antiArmor = CampaignNativeCombatAntiArmorV20.Create(selected,
                session.CurrentCombatForceCompletion,
                session.CurrentCombatForceCancellation, session.Context);
            var contacts = session.CurrentContacts.AdvanceNativeCombatAntiArmor(antiArmor);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(antiArmor), contacts, selected,
                combatBarrage: session.CurrentCombatBarrage,
                combatRetreatDecline: session.CurrentCombatRetreatDecline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves,
                combatForceSeals: session.CurrentCombatForceSeals,
                combatForceCompletion: session.CurrentCombatForceCompletion,
                combatForceCancellation: session.CurrentCombatForceCancellation,
                combatAntiArmor: antiArmor);
            var priorState = SerializeRetreatDeclineState(session);
            foreach (var seal in session.CurrentCombatForceSeals)
                priorState = seal.SerializeStateCanonical(priorState);
            if (session.CurrentCombatForceCompletion is { } completion)
                priorState = completion.SerializeStateCanonical(priorState);
            if (session.CurrentCombatForceCancellation is { } cancellation)
                priorState = cancellation.SerializeStateCanonical(priorState);
            var bytes = antiArmor.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                antiArmor.PriorStateVersion, antiArmor.StateVersion,
                antiArmor.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                bytes, antiArmor.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult CompleteNativeCombatCancelledAssault(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatAntiArmor is not { } antiArmor
            || session.CurrentCombatForceCancellation is null
            || session.CurrentCombatCancelledAssault is not null
            || session.CurrentContacts.AuthorityVersion != antiArmor.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var cancelledAssault = CampaignNativeCombatCancelledAssaultV21.Create(
                selected, antiArmor);
            var contacts = session.CurrentContacts.AdvanceNativeCombatCancelledAssault(
                cancelledAssault);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(cancelledAssault), contacts, selected,
                combatBarrage: session.CurrentCombatBarrage,
                combatRetreatDecline: session.CurrentCombatRetreatDecline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves,
                combatForceSeals: session.CurrentCombatForceSeals,
                combatForceCancellation: session.CurrentCombatForceCancellation,
                combatAntiArmor: antiArmor,
                combatCancelledAssault: cancelledAssault);
            var priorState = SerializeRetreatDeclineState(session);
            foreach (var seal in session.CurrentCombatForceSeals)
                priorState = seal.SerializeStateCanonical(priorState);
            priorState = session.CurrentCombatForceCancellation.SerializeStateCanonical(priorState);
            priorState = antiArmor.SerializeStateCanonical(priorState);
            var bytes = cancelledAssault.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                cancelledAssault.PriorStateVersion, cancelledAssault.StateVersion,
                cancelledAssault.NextPositionId, CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                bytes, cancelledAssault.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult ResolveNativeCombatAssault(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatForceCompletion is not { } completion
            || session.CurrentCombatAntiArmor is not { } antiArmor
            || session.CurrentCombatAssaultResult is not null
            || session.CurrentCombatCancelledAssault is not null
            || session.CurrentCombatForceCancellation is not null
            || session.CurrentContacts.AuthorityVersion != antiArmor.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        try
        {
            var ammunition = CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(
                session.Context, session.CurrentHistory);
            var inputs = CampaignNativeCombatAssaultInputs.Create(selected, completion,
                antiArmor, session.CurrentCombatForceSeals,
                session.CurrentCombatRetreatWorld ?? selected.PriorSnapshot.World,
                session.Context, ammunition);
            var result = CampaignNativeCombatAssaultResultV22.Create(selected,
                antiArmor, inputs, selected.PriorSnapshot.RandomState);
            var contacts = session.CurrentContacts.AdvanceNativeCombatAssaultResult(result);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(result), contacts, selected,
                combatBarrage: session.CurrentCombatBarrage,
                combatRetreatDecline: session.CurrentCombatRetreatDecline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves,
                combatForceSeals: session.CurrentCombatForceSeals,
                combatForceCompletion: completion,
                combatAntiArmor: antiArmor,
                combatAssaultResult: result);
            var priorState = SerializeRetreatDeclineState(session);
            foreach (var seal in session.CurrentCombatForceSeals)
                priorState = seal.SerializeStateCanonical(priorState);
            priorState = completion.SerializeStateCanonical(priorState);
            priorState = antiArmor.SerializeStateCanonical(priorState);
            var bytes = result.SerializeCanonical();
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                result.PriorStateVersion, result.StateVersion, result.NextPositionId,
                CampaignActionAudience.System,
                $"sha256:{Convert.ToHexStringLower(SHA256.HashData(bytes))}");
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(
                receipt, bytes, result.SerializeStateCanonical(priorState)));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    internal static ExerciseStepResult SubmitContactDefenderRbaMove(
        ExerciseSession session, CampaignActionSubmission submission, string defenderElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        if (string.IsNullOrWhiteSpace(defenderElementId)
            || !CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (session.CurrentCombatDecision is not { } selected
            || session.CurrentCombatBarrage is not { } barrage
            || session.CurrentCombatRetreatDecline is not null
            || session.CurrentContacts.AuthorityVersion
                != (session.CurrentCombatRetreatMove?.StateVersion ?? barrage.StateVersion))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.CampaignId != selected.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion
            != (session.CurrentCombatRetreatMove?.StateVersion ?? barrage.StateVersion))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != barrage.NextPositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        try
        {
            var cut = QueryContactRbaCut(session);
            var audience = cut.DefenderSide == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            if (submission.Audience != audience || defenderElementId != cut.DefenderElementId
                || !cut.MoveOffers.Any(value => value.ActionId == submission.ActionId))
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var previous = session.CurrentCombatRetreatMove;
            var initialLocationId = session.CurrentCombatRetreatMoves.Count == 0
                ? cut.InitialLocationId : session.CurrentCombatRetreatMoves[0].OriginLocationId;
            var movement = previous is null
                ? CampaignNativeCombatRetreatMoveV16.CreateFirst(selected, barrage,
                    session.Context, session.CurrentContacts, submission.ActionId)
                : CampaignNativeCombatRetreatMoveV16.CreateNext(selected, barrage,
                    previous, session.CurrentCombatRetreatWorld!, initialLocationId,
                    session.Context, session.CurrentContacts, submission.ActionId);
            var world = previous is null
                ? movement.ProjectWorld(selected, barrage, session.Context,
                    session.CurrentContacts)
                : movement.ProjectWorldAfter(selected, barrage, previous,
                    session.CurrentCombatRetreatWorld!, initialLocationId,
                    session.Context, session.CurrentContacts);
            var beforeWorld = session.CurrentCombatRetreatWorld ?? session.CurrentSnapshot.World;
            var contacts = session.CurrentContacts.AdvanceNativeCombatRetreatMove(
                beforeWorld, world, movement,
                selected.Decision.BoundRepresentationId!, session.Context);
            var checkpoint = previous is null
                ? movement.SerializeStateCanonical(selected, barrage,
                    session.Context, session.CurrentContacts)
                : movement.SerializeNextStateCanonical(
                    SerializeRetreatMoveState(session), world);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(movement), contacts, selected,
                combatBarrage: barrage, combatRetreatMove: movement, combatRetreatWorld: world,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts
                    ?? session.CurrentContacts,
                combatRetreatMoves: session.CurrentCombatRetreatMoves.Append(movement));
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                movement.PriorStateVersion, movement.StateVersion, movement.PositionId,
                submission.Audience, submission.ActionId);
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                movement.SerializeCanonical(), checkpoint));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or InvalidCampaignHistoryException or InvalidOperationException or JsonException)
        {
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static ExerciseStepResult Complete(ExerciseSession session,
        CampaignCurrentActionExecutionResult execution)
    {
        if (!execution.IsAccepted)
            return ExerciseStepResult.Rejected(execution.RejectionReason);

        var history = session.CurrentHistory.Add(execution.AcceptedEvent!);
        var contacts = execution.AcceptedEvent switch
        {
            ElementMovedV3 movement => session.CurrentContacts.AppendMovement(
                session.CurrentSnapshot, execution.SuccessorSnapshot!, movement, session.Context),
            ReactingElementMovedV2 movement => session.CurrentContacts.AppendMovement(
                session.CurrentSnapshot, execution.SuccessorSnapshot!, movement, session.Context),
            _ => session.CurrentContacts.AdvanceNonMovement(
                session.CurrentSnapshot, execution.SuccessorSnapshot!),
        };
        var successor = new ExerciseSession(execution.SuccessorSnapshot!, session.Context, history, contacts);
        var evidence = new ExerciseStepEvidence(
            execution.Receipt!,
            CampaignCurrentEventSerializer.Serialize(execution.AcceptedEvent!),
            CampaignCurrentSnapshotSerializer.Serialize(execution.SuccessorSnapshot!));
        return ExerciseStepResult.Accepted(successor, evidence);
    }

    private static byte[] SerializeRetreatDeclineState(ExerciseSession session)
    {
        var decline = session.CurrentCombatRetreatDecline
            ?? throw new InvalidOperationException("Force Assignment lacks RBA completion.");
        return session.CurrentCombatRetreatMove is not null
            ? decline.SerializeAfterMoveStateCanonical(SerializeRetreatMoveState(session))
            : decline.SerializeStateCanonical(session.CurrentCombatDecision!,
                session.CurrentCombatBarrage!);
    }

    private static byte[] SerializeRetreatMoveState(ExerciseSession session)
    {
        var selected = session.CurrentCombatDecision
            ?? throw new InvalidOperationException("Retreat move lacks selected assault.");
        var barrage = session.CurrentCombatBarrage
            ?? throw new InvalidOperationException("Retreat move lacks Barrage.");
        var contacts = session.CurrentCombatRetreatPriorContacts
            ?? throw new InvalidOperationException("Retreat move lacks its first contact cut.");
        var world = selected.PriorSnapshot.World;
        CampaignNativeCombatRetreatMoveV16? previous = null;
        byte[] state = [];
        foreach (var move in session.CurrentCombatRetreatMoves)
        {
            var nextWorld = previous is null
                ? move.ProjectWorld(selected, barrage, session.Context, contacts)
                : move.ProjectWorldAfter(selected, barrage, previous, world,
                    session.CurrentCombatRetreatMoves[0].OriginLocationId,
                    session.Context, contacts);
            state = previous is null
                ? move.SerializeStateCanonical(selected, barrage, session.Context, contacts)
                : move.SerializeNextStateCanonical(state, nextWorld);
            contacts = contacts.AdvanceNativeCombatRetreatMove(world, nextWorld, move,
                selected.Decision.BoundRepresentationId!, session.Context);
            world = nextWorld;
            previous = move;
        }
        if (previous is null)
            throw new InvalidOperationException("Retreat movement state is empty.");
        return state;
    }

    public static ExerciseReconstructionResult Reconstruct(ExerciseSession completedSession)
    {
        ArgumentNullException.ThrowIfNull(completedSession);
        var expectedBytes = completedSession.CurrentNoAttackCompletion is { } completion
            ? completion.SerializeStateCanonical(completedSession.CurrentCombatDecision!)
            : completedSession.CurrentCombatRetreatDecline is { } decline
                ? completedSession.CurrentCombatRetreatMove is not null
                    ? decline.SerializeAfterMoveStateCanonical(
                        SerializeRetreatMoveState(completedSession))
                    : decline.SerializeStateCanonical(completedSession.CurrentCombatDecision!,
                        completedSession.CurrentCombatBarrage!)
            : completedSession.CurrentCombatRetreatMove is not null
                ? SerializeRetreatMoveState(completedSession)
            : completedSession.CurrentCombatBarrage is { } barrage
                ? barrage.SerializeStateCanonical(completedSession.CurrentCombatDecision!)
            : completedSession.CurrentCombatDecision is { } combat
                ? combat.SerializeCanonical()
                : CampaignCurrentSnapshotSerializer.Serialize(completedSession.CurrentSnapshot);
        if (completedSession.CurrentCombatForceSeals.Count > 0
            || completedSession.CurrentCombatForceCancellation is not null)
        {
            expectedBytes = SerializeRetreatDeclineState(completedSession);
            foreach (var seal in completedSession.CurrentCombatForceSeals)
                expectedBytes = seal.SerializeStateCanonical(expectedBytes);
            if (completedSession.CurrentCombatForceCompletion is { } forceCompletion)
                expectedBytes = forceCompletion.SerializeStateCanonical(expectedBytes);
            if (completedSession.CurrentCombatForceCancellation is { } forceCancellation)
                expectedBytes = forceCancellation.SerializeStateCanonical(expectedBytes);
            if (completedSession.CurrentCombatAntiArmor is { } antiArmor)
                expectedBytes = antiArmor.SerializeStateCanonical(expectedBytes);
            if (completedSession.CurrentCombatCancelledAssault is { } cancelledAssault)
                expectedBytes = cancelledAssault.SerializeStateCanonical(expectedBytes);
            if (completedSession.CurrentCombatAssaultResult is { } assaultResult)
                expectedBytes = assaultResult.SerializeStateCanonical(expectedBytes);
        }
        var expectedHash = Hash(expectedBytes);
        string? eventStreamHash = null;

        try
        {
            var canonicalEvents = completedSession.CurrentHistory
                .Select(CampaignCurrentEventSerializer.Serialize)
                .ToArray();
            eventStreamHash = HashFramed(canonicalEvents);
            var replayEvents = canonicalEvents
                .Select(value => CampaignCurrentEventSerializer.Deserialize(value))
                .ToArray();
            var combatTailCount = completedSession.CurrentCombatDecision is null ? 0
                : completedSession.CurrentCombatBarrage is not null
                    ? 2 + completedSession.CurrentCombatRetreatMoves.Count
                        + (completedSession.CurrentCombatRetreatDecline is null ? 0 : 1)
                        + completedSession.CurrentCombatForceSeals.Count
                        + (completedSession.CurrentCombatForceCompletion is null ? 0 : 1)
                        + (completedSession.CurrentCombatForceCancellation is null ? 0 : 1)
                        + (completedSession.CurrentCombatAntiArmor is null ? 0 : 1)
                        + (completedSession.CurrentCombatCancelledAssault is null ? 0 : 1)
                        + (completedSession.CurrentCombatAssaultResult is null ? 0 : 1)
                : completedSession.CurrentNoAttackCompletion is not null ? 2 : 1;
            if (combatTailCount > 0 && replayEvents.Length < combatTailCount + 1)
                throw new InvalidCampaignHistoryException("Native Combat selection requires prior authority history.");
            var priorEvents = combatTailCount == 0 ? replayEvents
                : replayEvents[..^combatTailCount];
            var replayed = CampaignCurrentProjector.Replay(priorEvents, completedSession.Context);
            var replayedContacts = CampaignContactJournal.FromEvents(
                completedSession.Context, priorEvents);
            byte[] replayedBytes;
            var worldMatch = completedSession.CurrentCombatRetreatWorld is null;
            if (completedSession.CurrentCombatDecision is not null)
            {
                var decisionIndex = replayEvents.Length - combatTailCount;
                if (replayEvents[decisionIndex] is not CampaignNativeCombatDecisionV12 selected)
                    throw new InvalidCampaignHistoryException("Native Combat selection is missing.");
                var verified = CampaignNativeCombatDecisionStateV12.Replay(replayed,
                    completedSession.Context, replayedContacts, selected.SerializeCanonical());
                replayedContacts = replayedContacts.AdvanceNativeCombatSelection(verified);
                if (completedSession.CurrentNoAttackCompletion is not null)
                {
                    if (replayEvents[^1] is not CampaignNativeCombatNoAttackCompletionV13 noAttack)
                        throw new InvalidCampaignHistoryException("Native no-attack completion is missing.");
                    var verifiedCompletion = CampaignNativeCombatNoAttackCompletionV13.Replay(
                        verified, completedSession.Context, noAttack.SerializeCanonical());
                    replayedBytes = verifiedCompletion.SerializeStateCanonical(verified);
                    replayedContacts = replayedContacts.AdvanceNativeCombatNoAttackCompletion(
                        verifiedCompletion);
                }
                else if (completedSession.CurrentCombatBarrage is not null)
                {
                    if (replayEvents[decisionIndex + 1] is not CampaignNativeCombatBarrageV14 barrageEvent)
                        throw new InvalidCampaignHistoryException("Native Barrage completion is missing.");
                    var verifiedBarrage = CampaignNativeCombatBarrageV14.Replay(
                        verified, completedSession.Context, barrageEvent.SerializeCanonical());
                    replayedContacts = replayedContacts.AdvanceNativeCombatBarrage(verifiedBarrage);
                    var world = verified.PriorSnapshot.World;
                    CampaignNativeCombatRetreatMoveV16? previousMove = null;
                    string? initialLocationId = null;
                    byte[] moveState = [];
                    for (var moveIndex = 0;
                         moveIndex < completedSession.CurrentCombatRetreatMoves.Count;
                         moveIndex++)
                    {
                        if (replayEvents[decisionIndex + 2 + moveIndex]
                            is not CampaignNativeCombatRetreatMoveV16 moveEvent)
                            throw new InvalidCampaignHistoryException("Native defender move is missing.");
                        var beforeContacts = replayedContacts;
                        var verifiedMove = previousMove is null
                            ? CampaignNativeCombatRetreatMoveV16.ReplayFirst(
                                verified, verifiedBarrage, completedSession.Context,
                                beforeContacts, moveEvent.SerializeCanonical())
                            : CampaignNativeCombatRetreatMoveV16.ReplayNext(
                                verified, verifiedBarrage, previousMove, world,
                                initialLocationId!, completedSession.Context,
                                beforeContacts, moveEvent.SerializeCanonical());
                        var nextWorld = previousMove is null
                            ? verifiedMove.ProjectWorld(verified, verifiedBarrage,
                                completedSession.Context, beforeContacts)
                            : verifiedMove.ProjectWorldAfter(verified, verifiedBarrage,
                                previousMove, world, initialLocationId!,
                                completedSession.Context, beforeContacts);
                        moveState = previousMove is null
                            ? verifiedMove.SerializeStateCanonical(verified, verifiedBarrage,
                                completedSession.Context, beforeContacts)
                            : verifiedMove.SerializeNextStateCanonical(moveState, nextWorld);
                        replayedContacts = replayedContacts.AdvanceNativeCombatRetreatMove(
                            world, nextWorld, verifiedMove,
                            verified.Decision.BoundRepresentationId!, completedSession.Context);
                        initialLocationId ??= verifiedMove.OriginLocationId;
                        world = nextWorld;
                        previousMove = verifiedMove;
                    }
                    if (previousMove is not null)
                        worldMatch = world.Equals(completedSession.CurrentCombatRetreatWorld);
                    if (completedSession.CurrentCombatRetreatDecline is not null)
                    {
                        if (replayEvents[decisionIndex + 2 + completedSession.CurrentCombatRetreatMoves.Count]
                            is not CampaignNativeCombatRetreatDeclineV15 declineEvent)
                            throw new InvalidCampaignHistoryException("Native defender decline is missing.");
                        var verifiedDecline = previousMove is null
                            ? CampaignNativeCombatRetreatDeclineV15.Replay(
                                verified, verifiedBarrage, completedSession.Context,
                                replayedContacts, declineEvent.SerializeCanonical())
                            : CampaignNativeCombatRetreatDeclineV15.ReplayAfterMove(
                                verified, verifiedBarrage, previousMove, world,
                                initialLocationId!, completedSession.Context,
                                replayedContacts, declineEvent.SerializeCanonical());
                        replayedBytes = previousMove is null
                            ? verifiedDecline.SerializeStateCanonical(verified, verifiedBarrage)
                            : verifiedDecline.SerializeAfterMoveStateCanonical(moveState);
                        replayedContacts = replayedContacts.AdvanceNativeCombatRetreatDecline(
                            verifiedDecline);
                        CampaignNativeCombatForceSealV17? firstSeal = null;
                        for (var sealIndex = 0;
                             sealIndex < completedSession.CurrentCombatForceSeals.Count;
                             sealIndex++)
                        {
                            var eventIndex = decisionIndex + 3
                                + completedSession.CurrentCombatRetreatMoves.Count + sealIndex;
                            if (replayEvents[eventIndex] is not CampaignNativeCombatForceSealV17 sealEvent)
                                throw new InvalidCampaignHistoryException("Native Force Assignment seal is missing.");
                            var verifiedSeal = CampaignNativeCombatForceSealV17.Replay(
                                verified, verifiedDecline, world, completedSession.Context,
                                replayedContacts, sealEvent.SerializeCanonical(), firstSeal);
                            replayedBytes = verifiedSeal.SerializeStateCanonical(replayedBytes);
                            replayedContacts = replayedContacts.AdvanceNativeCombatForceSeal(verifiedSeal);
                            firstSeal ??= verifiedSeal;
                        }
                        CampaignNativeCombatForceCompletionV18? verifiedForceCompletion = null;
                        if (completedSession.CurrentCombatForceCompletion is not null)
                        {
                            var completionIndex = decisionIndex + 3
                                + completedSession.CurrentCombatRetreatMoves.Count
                                + completedSession.CurrentCombatForceSeals.Count;
                            if (replayEvents[completionIndex]
                                is not CampaignNativeCombatForceCompletionV18 completionEvent)
                                throw new InvalidCampaignHistoryException("Native Force completion is missing.");
                            var verifiedCompletion = CampaignNativeCombatForceCompletionV18.Replay(
                                verified, verifiedDecline, completedSession.CurrentCombatForceSeals,
                                completionEvent.SerializeCanonical());
                            verifiedForceCompletion = verifiedCompletion;
                            replayedBytes = verifiedCompletion.SerializeStateCanonical(replayedBytes);
                            replayedContacts = replayedContacts.AdvanceNativeCombatForceCompletion(
                                verifiedCompletion);
                        }
                        if (completedSession.CurrentCombatForceCancellation is not null)
                        {
                            var cancellationIndex = decisionIndex + 3
                                + completedSession.CurrentCombatRetreatMoves.Count
                                + completedSession.CurrentCombatForceSeals.Count;
                            if (replayEvents[cancellationIndex]
                                is not CampaignNativeCombatForceCancellationV19 cancellationEvent)
                                throw new InvalidCampaignHistoryException("Native Force cancellation is missing.");
                            var verifiedCancellation = CampaignNativeCombatForceCancellationV19.Replay(
                                verified, verifiedDecline, completedSession.CurrentCombatForceSeals,
                                cancellationEvent.SerializeCanonical());
                            replayedBytes = verifiedCancellation.SerializeStateCanonical(replayedBytes);
                            replayedContacts = replayedContacts.AdvanceNativeCombatForceCancellation(
                                verifiedCancellation);
                        }
                        CampaignNativeCombatAntiArmorV20? verifiedAntiArmor = null;
                        if (completedSession.CurrentCombatAntiArmor is not null)
                        {
                            var antiArmorIndex = decisionIndex + 3
                                + completedSession.CurrentCombatRetreatMoves.Count
                                + completedSession.CurrentCombatForceSeals.Count + 1;
                            if (replayEvents[antiArmorIndex]
                                is not CampaignNativeCombatAntiArmorV20 antiArmorEvent)
                                throw new InvalidCampaignHistoryException("Native Anti Armor completion is missing.");
                            var replayedAntiArmor = CampaignNativeCombatAntiArmorV20.Replay(
                                verified, completedSession.CurrentCombatForceCompletion,
                                completedSession.CurrentCombatForceCancellation,
                                completedSession.Context, antiArmorEvent.SerializeCanonical());
                            verifiedAntiArmor = replayedAntiArmor;
                            replayedBytes = replayedAntiArmor.SerializeStateCanonical(replayedBytes);
                            replayedContacts = replayedContacts.AdvanceNativeCombatAntiArmor(
                                replayedAntiArmor);
                        }
                        if (completedSession.CurrentCombatCancelledAssault is not null)
                        {
                            var cancelledIndex = decisionIndex + 3
                                + completedSession.CurrentCombatRetreatMoves.Count
                                + completedSession.CurrentCombatForceSeals.Count + 2;
                            if (replayEvents[cancelledIndex]
                                is not CampaignNativeCombatCancelledAssaultV21 cancelledEvent)
                                throw new InvalidCampaignHistoryException("Cancelled Close Assault completion is missing.");
                            var verifiedCancelled = CampaignNativeCombatCancelledAssaultV21.Replay(
                                verified, completedSession.CurrentCombatAntiArmor!,
                                cancelledEvent.SerializeCanonical());
                            replayedBytes = verifiedCancelled.SerializeStateCanonical(replayedBytes);
                            replayedContacts = replayedContacts.AdvanceNativeCombatCancelledAssault(
                                verifiedCancelled);
                        }
                        if (completedSession.CurrentCombatAssaultResult is not null)
                        {
                            var resultIndex = decisionIndex + 3
                                + completedSession.CurrentCombatRetreatMoves.Count
                                + completedSession.CurrentCombatForceSeals.Count + 2;
                            if (replayEvents[resultIndex]
                                is not CampaignNativeCombatAssaultResultV22 resultEvent
                                || verifiedForceCompletion is null
                                || verifiedAntiArmor is null)
                                throw new InvalidCampaignHistoryException(
                                    "Native Combat result or its prior authority is missing.");
                            var ammunition = CampaignCombatAmmunitionLedgerV1.FromAcceptedEvents(
                                completedSession.Context, priorEvents);
                            var inputs = CampaignNativeCombatAssaultInputs.Create(verified,
                                verifiedForceCompletion, verifiedAntiArmor,
                                completedSession.CurrentCombatForceSeals, world,
                                completedSession.Context, ammunition);
                            var verifiedResult = CampaignNativeCombatAssaultResultV22.Replay(
                                verified, verifiedAntiArmor, inputs,
                                verified.PriorSnapshot.RandomState,
                                resultEvent.SerializeCanonical());
                            replayedBytes = verifiedResult.SerializeStateCanonical(replayedBytes);
                            replayedContacts = replayedContacts.AdvanceNativeCombatAssaultResult(
                                verifiedResult);
                        }
                    }
                    else if (previousMove is not null) replayedBytes = moveState;
                    else replayedBytes = verifiedBarrage.SerializeStateCanonical(verified);
                }
                else replayedBytes = verified.SerializeCanonical();
            }
            else
            {
                replayedBytes = CampaignCurrentSnapshotSerializer.Serialize(replayed);
            }
            var replayedHash = Hash(replayedBytes);
            var contactRecords = completedSession.CurrentContacts.Records;
            var contactsMatch = completedSession.CurrentContacts.AuthorityVersion
                == (completedSession.CurrentCombatAssaultResult?.StateVersion
                    ?? completedSession.CurrentCombatCancelledAssault?.StateVersion
                    ?? completedSession.CurrentCombatAntiArmor?.StateVersion
                    ?? completedSession.CurrentCombatForceCancellation?.StateVersion
                    ?? completedSession.CurrentCombatForceCompletion?.StateVersion
                    ?? (completedSession.CurrentCombatForceSeals.Count > 0
                    ? completedSession.CurrentCombatForceSeals[^1].StateVersion
                    : completedSession.CurrentNoAttackCompletion?.StateVersion
                    ?? completedSession.CurrentCombatRetreatDecline?.StateVersion
                    ?? completedSession.CurrentCombatRetreatMove?.StateVersion
                    ?? completedSession.CurrentCombatBarrage?.StateVersion
                    ?? completedSession.CurrentCombatDecision?.StateVersion
                    ?? completedSession.CurrentSnapshot.StateVersion))
                && completedSession.CurrentContacts.AuthorityVersion == replayedContacts.AuthorityVersion
                && contactRecords.Count == replayedContacts.Records.Count
                && contactRecords.Zip(replayedContacts.Records).All(pair =>
                    pair.First.AsSpan().SequenceEqual(pair.Second));
            var failureReason = expectedBytes.AsSpan().SequenceEqual(replayedBytes)
                && contactsMatch && worldMatch
                ? ExerciseReconstructionFailureReason.None
                : ExerciseReconstructionFailureReason.SnapshotMismatch;
            return new ExerciseReconstructionResult(
                failureReason,
                replayedBytes,
                eventStreamHash,
                expectedHash,
                replayedHash);
        }
        catch (Exception exception) when (exception is JsonException
            or InvalidCampaignHistoryException
            or ArgumentException
            or InvalidOperationException)
        {
            return new ExerciseReconstructionResult(
                ExerciseReconstructionFailureReason.InvalidHistory,
                null,
                eventStreamHash,
                expectedHash,
                null);
        }
    }

    private static string Hash(ReadOnlySpan<byte> value) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(value))}";

    private static string HashFramed(IEnumerable<byte[]> records)
    {
        using var stream = new MemoryStream();
        foreach (var record in records)
        {
            stream.Write(record);
            stream.WriteByte((byte)'\n');
        }
        return Hash(stream.ToArray());
    }
}
