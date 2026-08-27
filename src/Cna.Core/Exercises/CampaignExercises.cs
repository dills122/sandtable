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
            execution.Snapshot!,
            execution.Context!,
            [execution.CreatedEvent!]);
        return ExerciseStartResult.Started(
            session,
            CampaignEventSerializer.Serialize(execution.CreatedEvent!),
            CampaignSnapshotSerializer.Serialize(execution.Snapshot!));
    }

    public static CampaignLegalActionQueryResult Query(
        ExerciseSession session,
        CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        return CampaignLegalActions.Query(
            new CampaignAuthorityHandle(session.Snapshot, session.Context),
            audience);
    }

    public static ExerciseCheckpoint QueryCheckpoint(ExerciseSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return new ExerciseCheckpoint(session.Snapshot);
    }

    public static ExerciseCheckpoint ReadCheckpoint(ReadOnlyMemory<byte> canonicalSnapshot) =>
        new(CampaignSnapshotSerializer.Deserialize(canonicalSnapshot));

    public static ExerciseStepResult Submit(
        ExerciseSession session,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        var execution = CampaignActionExecution.Execute(
            session.Snapshot,
            session.Context,
            submission);
        if (!execution.IsAccepted)
            return ExerciseStepResult.Rejected(execution.RejectionReason);

        var history = session.History.Add(execution.AcceptedEvent!);
        var successor = new ExerciseSession(
            execution.SuccessorSnapshot!,
            session.Context,
            history);
        var evidence = new ExerciseStepEvidence(
            execution.Receipt!,
            CampaignEventSerializer.Serialize(execution.AcceptedEvent!),
            CampaignSnapshotSerializer.Serialize(execution.SuccessorSnapshot!));
        return ExerciseStepResult.Accepted(successor, evidence);
    }

    public static ExerciseReconstructionResult Reconstruct(ExerciseSession completedSession)
    {
        ArgumentNullException.ThrowIfNull(completedSession);
        var expectedBytes = CampaignSnapshotSerializer.Serialize(completedSession.Snapshot);
        var expectedHash = Hash(expectedBytes);
        string? eventStreamHash = null;

        try
        {
            var canonicalEvents = completedSession.History
                .Select(CampaignEventSerializer.Serialize)
                .ToArray();
            eventStreamHash = HashFramed(canonicalEvents);
            var replayEvents = canonicalEvents
                .Select(value => CampaignEventSerializer.Deserialize(value))
                .ToArray();
            var replayed = CampaignProjector.Replay(replayEvents, completedSession.Context);
            var replayedBytes = CampaignSnapshotSerializer.Serialize(replayed);
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
