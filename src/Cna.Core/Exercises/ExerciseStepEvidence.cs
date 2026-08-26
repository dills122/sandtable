using Cna.Core.Actions;

namespace Cna.Core.Exercises;

public sealed class ExerciseStepEvidence
{
    public const int CurrentContractVersion = 1;

    private readonly byte[] eventRecord;
    private readonly byte[] snapshotCheckpoint;

    internal ExerciseStepEvidence(
        CampaignActionAcceptanceReceipt receipt,
        byte[] eventRecord,
        byte[] snapshotCheckpoint)
    {
        Receipt = receipt ?? throw new ArgumentNullException(nameof(receipt));
        this.eventRecord = eventRecord?.ToArray()
            ?? throw new ArgumentNullException(nameof(eventRecord));
        this.snapshotCheckpoint = snapshotCheckpoint?.ToArray()
            ?? throw new ArgumentNullException(nameof(snapshotCheckpoint));
        ContractVersion = CurrentContractVersion;
    }

    public int ContractVersion { get; }
    public CampaignActionAcceptanceReceipt Receipt { get; }
    public IReadOnlyList<byte[]> EventRecords => Array.AsReadOnly(new[] { eventRecord.ToArray() });
    public byte[] SnapshotCheckpoint => snapshotCheckpoint.ToArray();
}
