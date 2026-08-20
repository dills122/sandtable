using Cna.Core.Actions;

namespace Cna.Core.Exercises;

public sealed class ExerciseStepEvidence
{
    public const int CurrentContractVersion = 1;

    private readonly byte[][] eventRecords;
    private readonly byte[] snapshotCheckpoint;

    internal ExerciseStepEvidence(
        CampaignActionAcceptanceReceipt receipt,
        IEnumerable<byte[]> eventRecords,
        byte[] snapshotCheckpoint)
    {
        Receipt = receipt ?? throw new ArgumentNullException(nameof(receipt));
        ArgumentNullException.ThrowIfNull(eventRecords);
        this.eventRecords = eventRecords
            .Select(value => value?.ToArray()
                ?? throw new ArgumentException("Event records cannot contain null.", nameof(eventRecords)))
            .ToArray();
        if (this.eventRecords.Length != 1)
            throw new ArgumentException(
                "Exercise evidence version 1 requires exactly one event record.",
                nameof(eventRecords));
        this.snapshotCheckpoint = snapshotCheckpoint?.ToArray()
            ?? throw new ArgumentNullException(nameof(snapshotCheckpoint));
        ContractVersion = CurrentContractVersion;
    }

    public int ContractVersion { get; }
    public CampaignActionAcceptanceReceipt Receipt { get; }
    public IReadOnlyList<byte[]> EventRecords => Array.AsReadOnly(
        eventRecords.Select(value => value.ToArray()).ToArray());
    public byte[] SnapshotCheckpoint => snapshotCheckpoint.ToArray();
}
