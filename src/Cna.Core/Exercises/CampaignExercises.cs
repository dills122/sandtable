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
            return CampaignContactRetreatBeforeAssaultCut.ProjectAfterFirstMove(
                selected, barrage, move, session.CurrentCombatRetreatWorld!,
                session.Context, session.CurrentContacts);
        return CampaignContactRetreatBeforeAssaultCut.Project(selected, barrage,
            session.Context, session.CurrentContacts);
    }

    public static ExerciseCheckpoint QueryCheckpoint(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
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
                ? CampaignNativeCombatRetreatDeclineV15.CreateAfterFirstMove(
                    selected, barrage, previousMove, session.CurrentCombatRetreatWorld!,
                    session.Context, session.CurrentCombatRetreatPriorContacts!,
                    session.CurrentContacts, submission.ActionId)
                : CampaignNativeCombatRetreatDeclineV15.Create(selected, barrage,
                    session.Context, session.CurrentContacts, submission.ActionId);
            var contacts = session.CurrentContacts.AdvanceNativeCombatRetreatDecline(decline);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(decline), contacts, selected,
                combatBarrage: barrage, combatRetreatDecline: decline,
                combatRetreatMove: session.CurrentCombatRetreatMove,
                combatRetreatWorld: session.CurrentCombatRetreatWorld,
                combatRetreatPriorContacts: session.CurrentCombatRetreatPriorContacts);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                decline.PriorStateVersion, decline.StateVersion, decline.PositionId,
                submission.Audience, submission.ActionId);
            var checkpoint = session.CurrentCombatRetreatMove is { } move
                ? decline.SerializeAfterFirstMoveStateCanonical(selected, barrage, move,
                    session.Context, session.CurrentCombatRetreatPriorContacts!)
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

    internal static ExerciseStepResult SubmitContactDefenderRbaFirstMove(
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
            || session.CurrentCombatRetreatMove is not null
            || session.CurrentContacts.AuthorityVersion != barrage.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (submission.CampaignId != selected.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != barrage.StateVersion)
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
            var movement = CampaignNativeCombatRetreatMoveV16.CreateFirst(selected, barrage,
                session.Context, session.CurrentContacts, submission.ActionId);
            var world = movement.ProjectWorld(selected, barrage, session.Context,
                session.CurrentContacts);
            var contacts = session.CurrentContacts.AdvanceNativeCombatRetreatMove(
                session.CurrentSnapshot.World, world, movement,
                selected.Decision.BoundRepresentationId!, session.Context);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                session.CurrentHistory.Add(movement), contacts, selected,
                combatBarrage: barrage, combatRetreatMove: movement, combatRetreatWorld: world,
                combatRetreatPriorContacts: session.CurrentContacts);
            var receipt = new CampaignActionAcceptanceReceipt(selected.CampaignId,
                barrage.StateVersion, movement.StateVersion, movement.PositionId,
                submission.Audience, submission.ActionId);
            return ExerciseStepResult.Accepted(successor, new ExerciseStepEvidence(receipt,
                movement.SerializeCanonical(), movement.SerializeStateCanonical(selected,
                    barrage, session.Context, session.CurrentContacts)));
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

    public static ExerciseReconstructionResult Reconstruct(ExerciseSession completedSession)
    {
        ArgumentNullException.ThrowIfNull(completedSession);
        var expectedBytes = completedSession.CurrentNoAttackCompletion is { } completion
            ? completion.SerializeStateCanonical(completedSession.CurrentCombatDecision!)
            : completedSession.CurrentCombatRetreatDecline is { } decline
                ? completedSession.CurrentCombatRetreatMove is { } beforeDeclineMove
                    ? decline.SerializeAfterFirstMoveStateCanonical(
                        completedSession.CurrentCombatDecision!,
                        completedSession.CurrentCombatBarrage!, beforeDeclineMove,
                        completedSession.Context,
                        completedSession.CurrentCombatRetreatPriorContacts!)
                    : decline.SerializeStateCanonical(completedSession.CurrentCombatDecision!,
                        completedSession.CurrentCombatBarrage!)
            : completedSession.CurrentCombatRetreatMove is { } retreatMove
                ? retreatMove.SerializeStateCanonical(completedSession.CurrentCombatDecision!,
                    completedSession.CurrentCombatBarrage!, completedSession.Context,
                    completedSession.CurrentCombatRetreatPriorContacts!)
            : completedSession.CurrentCombatBarrage is { } barrage
                ? barrage.SerializeStateCanonical(completedSession.CurrentCombatDecision!)
            : completedSession.CurrentCombatDecision is { } combat
                ? combat.SerializeCanonical()
                : CampaignCurrentSnapshotSerializer.Serialize(completedSession.CurrentSnapshot);
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
                : completedSession.CurrentCombatRetreatDecline is not null
                    && completedSession.CurrentCombatRetreatMove is not null ? 4
                : completedSession.CurrentCombatRetreatDecline is not null
                    || completedSession.CurrentCombatRetreatMove is not null ? 3
                : completedSession.CurrentNoAttackCompletion is not null
                    || completedSession.CurrentCombatBarrage is not null ? 2 : 1;
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
                    if (completedSession.CurrentCombatRetreatDecline is not null)
                    {
                        if (replayEvents[^1] is not CampaignNativeCombatRetreatDeclineV15 declineEvent)
                            throw new InvalidCampaignHistoryException("Native defender decline is missing.");
                        CampaignNativeCombatRetreatDeclineV15 verifiedDecline;
                        if (completedSession.CurrentCombatRetreatMove is not null)
                        {
                            if (replayEvents[^2] is not CampaignNativeCombatRetreatMoveV16 moveEvent)
                                throw new InvalidCampaignHistoryException("Prior defender move is missing.");
                            var priorContacts = replayedContacts;
                            var verifiedMove = CampaignNativeCombatRetreatMoveV16.ReplayFirst(
                                verified, verifiedBarrage, completedSession.Context,
                                priorContacts, moveEvent.SerializeCanonical());
                            var world = verifiedMove.ProjectWorld(verified, verifiedBarrage,
                                completedSession.Context, priorContacts);
                            worldMatch = world.Equals(completedSession.CurrentCombatRetreatWorld);
                            replayedContacts = replayedContacts.AdvanceNativeCombatRetreatMove(
                                verified.PriorSnapshot.World, world, verifiedMove,
                                verified.Decision.BoundRepresentationId!, completedSession.Context);
                            verifiedDecline = CampaignNativeCombatRetreatDeclineV15.ReplayAfterFirstMove(
                                verified, verifiedBarrage, verifiedMove, world,
                                completedSession.Context, priorContacts, replayedContacts,
                                declineEvent.SerializeCanonical());
                            replayedBytes = verifiedDecline.SerializeAfterFirstMoveStateCanonical(
                                verified, verifiedBarrage, verifiedMove,
                                completedSession.Context, priorContacts);
                        }
                        else
                        {
                            verifiedDecline = CampaignNativeCombatRetreatDeclineV15.Replay(
                                verified, verifiedBarrage, completedSession.Context,
                                replayedContacts, declineEvent.SerializeCanonical());
                            replayedBytes = verifiedDecline.SerializeStateCanonical(verified,
                                verifiedBarrage);
                        }
                        replayedContacts = replayedContacts.AdvanceNativeCombatRetreatDecline(
                            verifiedDecline);
                    }
                    else if (completedSession.CurrentCombatRetreatMove is not null)
                    {
                        if (replayEvents[^1] is not CampaignNativeCombatRetreatMoveV16 moveEvent)
                            throw new InvalidCampaignHistoryException("Native defender move is missing.");
                        var verifiedMove = CampaignNativeCombatRetreatMoveV16.ReplayFirst(
                            verified, verifiedBarrage, completedSession.Context,
                            replayedContacts, moveEvent.SerializeCanonical());
                        var world = verifiedMove.ProjectWorld(verified, verifiedBarrage,
                            completedSession.Context, replayedContacts);
                        worldMatch = world.Equals(completedSession.CurrentCombatRetreatWorld);
                        replayedBytes = verifiedMove.SerializeStateCanonical(verified,
                            verifiedBarrage, completedSession.Context, replayedContacts);
                        replayedContacts = replayedContacts.AdvanceNativeCombatRetreatMove(
                            verified.PriorSnapshot.World, world, verifiedMove,
                            verified.Decision.BoundRepresentationId!, completedSession.Context);
                    }
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
                == (completedSession.CurrentNoAttackCompletion?.StateVersion
                    ?? completedSession.CurrentCombatRetreatDecline?.StateVersion
                    ?? completedSession.CurrentCombatRetreatMove?.StateVersion
                    ?? completedSession.CurrentCombatBarrage?.StateVersion
                    ?? completedSession.CurrentCombatDecision?.StateVersion
                    ?? completedSession.CurrentSnapshot.StateVersion)
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
