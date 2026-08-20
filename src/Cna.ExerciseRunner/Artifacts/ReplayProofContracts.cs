using System.Buffers.Binary;
using System.Security.Cryptography;
using Cna.Core.Exercises;

namespace Cna.ExerciseRunner.Artifacts;

public sealed record ReconstructionProof
{
    public const int CurrentContractVersion = 1;
    public const string SchemeId = "sandtable.exercise-reconstruction-proof.v1";

    internal ReconstructionProof(
        ExerciseReconstructionFailureReason failureReason,
        string eventStreamHash,
        string expectedSnapshotHash,
        string? reconstructedSnapshotHash)
    {
        if (!Enum.IsDefined(failureReason))
            throw new ArgumentOutOfRangeException(nameof(failureReason));
        ReplayProofValidation.RequireSha256(eventStreamHash, nameof(eventStreamHash));
        ReplayProofValidation.RequireSha256(expectedSnapshotHash, nameof(expectedSnapshotHash));
        if (reconstructedSnapshotHash is not null)
            ReplayProofValidation.RequireSha256(
                reconstructedSnapshotHash,
                nameof(reconstructedSnapshotHash));

        var historyAccepted = failureReason != ExerciseReconstructionFailureReason.InvalidHistory;
        var snapshotsMatch = string.Equals(
            expectedSnapshotHash,
            reconstructedSnapshotHash,
            StringComparison.Ordinal);
        if (failureReason switch
        {
            ExerciseReconstructionFailureReason.None => !historyAccepted || !snapshotsMatch,
            ExerciseReconstructionFailureReason.InvalidHistory =>
                historyAccepted || reconstructedSnapshotHash is not null,
            ExerciseReconstructionFailureReason.SnapshotMismatch =>
                !historyAccepted || reconstructedSnapshotHash is null || snapshotsMatch,
            _ => true,
        })
            throw new ArgumentException("The reconstruction proof is contradictory.");

        ContractVersion = CurrentContractVersion;
        ContractSchemeId = SchemeId;
        FailureReason = failureReason;
        EventStreamHash = eventStreamHash;
        ExpectedSnapshotHash = expectedSnapshotHash;
        ReconstructedSnapshotHash = reconstructedSnapshotHash;
        HistoryAccepted = historyAccepted;
        FinalSnapshotMatches = snapshotsMatch;
    }

    public int ContractVersion { get; }
    public string ContractSchemeId { get; }
    public ExerciseReconstructionFailureReason FailureReason { get; }
    public string EventStreamHash { get; }
    public string ExpectedSnapshotHash { get; }
    public string? ReconstructedSnapshotHash { get; }
    public bool HistoryAccepted { get; }
    public bool FinalSnapshotMatches { get; }
    public bool IsVerified => HistoryAccepted && FinalSnapshotMatches;

    public static ReconstructionProof From(ExerciseReconstructionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new ReconstructionProof(
            result.FailureReason,
            result.EventStreamHash,
            result.ExpectedSnapshotHash,
            result.ReconstructedSnapshotHash);
    }
}

public sealed record ReadjudicationProof
{
    public const int CurrentContractVersion = 1;
    public const string SchemeId = "sandtable.exercise-readjudication-proof.v1";

    internal ReadjudicationProof(
        string expectedTranscriptHash,
        string readjudicatedTranscriptHash,
        string expectedEventsHash,
        string readjudicatedEventsHash,
        string expectedFinalSnapshotHash,
        string readjudicatedFinalSnapshotHash)
    {
        ReplayProofValidation.RequireSha256(
            expectedTranscriptHash,
            nameof(expectedTranscriptHash));
        ReplayProofValidation.RequireSha256(
            readjudicatedTranscriptHash,
            nameof(readjudicatedTranscriptHash));
        ReplayProofValidation.RequireSha256(expectedEventsHash, nameof(expectedEventsHash));
        ReplayProofValidation.RequireSha256(
            readjudicatedEventsHash,
            nameof(readjudicatedEventsHash));
        ReplayProofValidation.RequireSha256(
            expectedFinalSnapshotHash,
            nameof(expectedFinalSnapshotHash));
        ReplayProofValidation.RequireSha256(
            readjudicatedFinalSnapshotHash,
            nameof(readjudicatedFinalSnapshotHash));

        ContractVersion = CurrentContractVersion;
        ContractSchemeId = SchemeId;
        ExpectedTranscriptHash = expectedTranscriptHash;
        ReadjudicatedTranscriptHash = readjudicatedTranscriptHash;
        ExpectedEventsHash = expectedEventsHash;
        ReadjudicatedEventsHash = readjudicatedEventsHash;
        ExpectedFinalSnapshotHash = expectedFinalSnapshotHash;
        ReadjudicatedFinalSnapshotHash = readjudicatedFinalSnapshotHash;
    }

    public int ContractVersion { get; }
    public string ContractSchemeId { get; }
    public string ExpectedTranscriptHash { get; }
    public string ReadjudicatedTranscriptHash { get; }
    public string ExpectedEventsHash { get; }
    public string ReadjudicatedEventsHash { get; }
    public string ExpectedFinalSnapshotHash { get; }
    public string ReadjudicatedFinalSnapshotHash { get; }
    public bool TranscriptMatches => string.Equals(
        ExpectedTranscriptHash,
        ReadjudicatedTranscriptHash,
        StringComparison.Ordinal);
    public bool EventsMatch => string.Equals(
        ExpectedEventsHash,
        ReadjudicatedEventsHash,
        StringComparison.Ordinal);
    public bool FinalSnapshotMatches => string.Equals(
        ExpectedFinalSnapshotHash,
        ReadjudicatedFinalSnapshotHash,
        StringComparison.Ordinal);
    public bool IsVerified => TranscriptMatches && EventsMatch && FinalSnapshotMatches;
}

internal static class ReplayEvidenceHasher
{
    internal static string HashRecords(IEnumerable<byte[]> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[sizeof(int)];
        foreach (var record in records)
        {
            ArgumentNullException.ThrowIfNull(record);
            BinaryPrimitives.WriteInt32BigEndian(length, record.Length);
            hash.AppendData(length);
            hash.AppendData(record);
        }
        return FormatHash(hash.GetHashAndReset());
    }

    internal static string HashBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return FormatHash(SHA256.HashData(bytes));
    }

    private static string FormatHash(byte[] digest) =>
        $"sha256:{Convert.ToHexStringLower(digest)}";
}

internal static class ReplayProofValidation
{
    internal static void RequireSha256(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (value.Length != 71 || !value.StartsWith("sha256:", StringComparison.Ordinal))
            throw new ArgumentException("A SHA-256 value is required.", parameterName);
        foreach (var character in value.AsSpan(7))
        {
            if (character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f'))
                throw new ArgumentException("A SHA-256 value is required.", parameterName);
        }
    }
}
