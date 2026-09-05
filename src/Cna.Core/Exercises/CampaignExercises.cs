using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;

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
            [execution.CreatedEvent!]);
        return ExerciseStartResult.Started(
            session,
            CampaignCurrentEventSerializer.Serialize(execution.CurrentCreatedEvent!),
            CampaignSnapshotV10Serializer.Serialize(execution.CurrentSnapshot!));
    }

    public static CampaignLegalActionQueryResult Query(
        ExerciseSession session,
        CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        return CampaignLegalActions.Query(
            new CampaignAuthorityHandle(session.CurrentSnapshot, session.Context),
            audience);
    }

    public static ExerciseCheckpoint QueryCheckpoint(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new ExerciseCheckpoint(session.CurrentSnapshot);
    }

    public static ExerciseCheckpoint ReadCheckpoint(ReadOnlyMemory<byte> canonicalSnapshot) =>
        new(CampaignSnapshotV10Serializer.Deserialize(canonicalSnapshot));

    public static ExerciseStepResult Submit(
        ExerciseSession session,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        var execution = CampaignCurrentActionExecution.Execute(
            session.CurrentSnapshot,
            session.Context,
            submission);
        if (!execution.IsAccepted)
            return ExerciseStepResult.Rejected(execution.RejectionReason);

        var history = session.CurrentHistory.Add(execution.AcceptedEvent!);
        var legacyHistory = execution.AcceptedEvent is CampaignEvent legacyEvent
            ? session.History.Add(legacyEvent)
            : session.History;
        var successor = new ExerciseSession(
            execution.SuccessorSnapshot!,
            session.Context,
            history,
            legacyHistory,
            execution.SuccessorSnapshot!.ReactionWindow is null
                ? CampaignV10LegacyBridge.ToLegacy(
                    execution.SuccessorSnapshot,
                    session.Context)
                : session.Snapshot);
        var evidence = new ExerciseStepEvidence(
            execution.Receipt!,
            CampaignCurrentEventSerializer.Serialize(execution.AcceptedEvent!),
            CampaignSnapshotV10Serializer.Serialize(execution.SuccessorSnapshot!));
        return ExerciseStepResult.Accepted(successor, evidence);
    }

    public static ExerciseReconstructionResult Reconstruct(ExerciseSession completedSession)
    {
        ArgumentNullException.ThrowIfNull(completedSession);
        var expectedBytes = CampaignSnapshotV10Serializer.Serialize(completedSession.CurrentSnapshot);
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
            var replayed = CampaignCurrentProjector.Replay(replayEvents, completedSession.Context);
            var replayedBytes = CampaignSnapshotV10Serializer.Serialize(replayed);
            var replayedHash = Hash(replayedBytes);
            var failureReason = expectedBytes.AsSpan().SequenceEqual(replayedBytes)
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
