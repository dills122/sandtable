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
        if (session.CurrentCombatSelection is not null)
            return CampaignLegalActionQueryResult.Rejected(CampaignLegalActionQueryRejectionReason.InvalidState);
        return CampaignLegalActions.Query(
            new CampaignAuthorityHandle(session.CurrentSnapshot, session.Context),
            audience);
    }

    internal static CampaignContactOwnerMovementCut QueryContactOwnerMovementCut(
        ExerciseSession session, Cna.Core.Rules.LandSide side, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatSelection is not null)
            throw new InvalidOperationException("Combat selection has already advanced this authority cut.");
        return CampaignContactOwnerMovementCut.Project(session.CurrentSnapshot, session.Context,
            session.CurrentContacts, side, actingElementId);
    }

    internal static CampaignContactCombatSelectionCut QueryContactOwnerCombatCut(
        ExerciseSession session, LandSide side, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.CurrentCombatSelection is not null)
            throw new InvalidOperationException("Combat selection has already advanced this authority cut.");
        return CampaignContactCombatSelectionCut.Project(session.CurrentSnapshot, session.Context,
            session.CurrentContacts, side, actingElementId);
    }

    public static ExerciseCheckpoint QueryCheckpoint(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.CurrentCombatSelection is { } selected
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
        if (session.CurrentCombatSelection is not null)
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
        if (session.CurrentCombatSelection is not null)
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
        if (session.CurrentCombatSelection is not null
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
            if (!cut.AssaultOffers.Any(offer => offer.ActionId == submission.ActionId))
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var selected = CampaignNativeCombatSelectionStateV12.Select(session.CurrentSnapshot,
                session.Context, session.CurrentContacts, side.Value, actingElementId,
                submission.ActionId);
            var contacts = session.CurrentContacts.AdvanceNativeCombatSelection(selected);
            var history = session.CurrentHistory.Add(selected.Selection);
            var successor = new ExerciseSession(session.CurrentSnapshot, session.Context,
                history, contacts, selected);
            var receipt = new CampaignActionAcceptanceReceipt(session.CurrentSnapshot.CampaignId,
                session.CurrentSnapshot.StateVersion, selected.StateVersion,
                selected.Selection.PositionId, submission.Audience, submission.ActionId);
            var evidence = new ExerciseStepEvidence(receipt,
                selected.Selection.SerializeCanonical(), selected.SerializeCanonical());
            return ExerciseStepResult.Accepted(successor, evidence);
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
        var expectedBytes = completedSession.CurrentCombatSelection is { } combat
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
            if (completedSession.CurrentCombatSelection is not null && replayEvents.Length < 2)
                throw new InvalidCampaignHistoryException("Native Combat selection requires prior authority history.");
            var priorEvents = completedSession.CurrentCombatSelection is null
                ? replayEvents
                : replayEvents[..^1];
            var replayed = CampaignCurrentProjector.Replay(priorEvents, completedSession.Context);
            var replayedContacts = CampaignContactJournal.FromEvents(
                completedSession.Context, priorEvents);
            byte[] replayedBytes;
            if (completedSession.CurrentCombatSelection is not null)
            {
                if (replayEvents[^1] is not CampaignNativeCombatSelectedV12 selected)
                    throw new InvalidCampaignHistoryException("Native Combat selection is not terminal.");
                var verified = CampaignNativeCombatSelectionStateV12.Replay(replayed,
                    completedSession.Context, replayedContacts, selected.SerializeCanonical());
                replayedBytes = verified.SerializeCanonical();
                replayedContacts = replayedContacts.AdvanceNativeCombatSelection(verified);
            }
            else
            {
                replayedBytes = CampaignCurrentSnapshotSerializer.Serialize(replayed);
            }
            var replayedHash = Hash(replayedBytes);
            var contactRecords = completedSession.CurrentContacts.Records;
            var contactsMatch = completedSession.CurrentContacts.AuthorityVersion
                == (completedSession.CurrentCombatSelection?.StateVersion
                    ?? completedSession.CurrentSnapshot.StateVersion)
                && completedSession.CurrentContacts.AuthorityVersion == replayedContacts.AuthorityVersion
                && contactRecords.Count == replayedContacts.Records.Count
                && contactRecords.Zip(replayedContacts.Records).All(pair =>
                    pair.First.AsSpan().SequenceEqual(pair.Second));
            var failureReason = expectedBytes.AsSpan().SequenceEqual(replayedBytes) && contactsMatch
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
