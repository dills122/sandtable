namespace Cna.Core.Exercises;

public enum ExerciseReconstructionFailureReason
{
    None,
    InvalidHistory,
    SnapshotMismatch,
}

public sealed class ExerciseReconstructionResult
{
    public const int CurrentContractVersion = 1;

    private readonly byte[]? reconstructedSnapshotBytes;

    internal ExerciseReconstructionResult(
        ExerciseReconstructionFailureReason failureReason,
        byte[]? reconstructedSnapshotBytes,
        string eventStreamHash,
        string expectedSnapshotHash,
        string? reconstructedSnapshotHash)
    {
        ContractVersion = CurrentContractVersion;
        FailureReason = failureReason;
        this.reconstructedSnapshotBytes = reconstructedSnapshotBytes?.ToArray();
        EventStreamHash = eventStreamHash;
        ExpectedSnapshotHash = expectedSnapshotHash;
        ReconstructedSnapshotHash = reconstructedSnapshotHash;
    }

    public int ContractVersion { get; }
    public bool IsVerified => FailureReason == ExerciseReconstructionFailureReason.None;
    public ExerciseReconstructionFailureReason FailureReason { get; }
    public byte[]? ReconstructedSnapshotBytes => reconstructedSnapshotBytes?.ToArray();
    public string EventStreamHash { get; }
    public string ExpectedSnapshotHash { get; }
    public string? ReconstructedSnapshotHash { get; }
}
