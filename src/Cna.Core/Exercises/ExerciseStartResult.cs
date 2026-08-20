using Cna.Core.Campaigns;

namespace Cna.Core.Exercises;

public sealed class ExerciseStartResult
{
    private readonly byte[]? creationEventBytes;
    private readonly byte[]? initialSnapshotBytes;

    private ExerciseStartResult(
        ExerciseSession? session,
        byte[]? creationEventBytes,
        byte[]? initialSnapshotBytes,
        CampaignCreationRejectionReason rejectionReason)
    {
        Session = session;
        this.creationEventBytes = creationEventBytes?.ToArray();
        this.initialSnapshotBytes = initialSnapshotBytes?.ToArray();
        RejectionReason = rejectionReason;
    }

    public bool IsStarted => Session is not null;
    public ExerciseSession? Session { get; }
    public byte[]? CreationEventBytes => creationEventBytes?.ToArray();
    public byte[]? InitialSnapshotBytes => initialSnapshotBytes?.ToArray();
    public CampaignCreationRejectionReason RejectionReason { get; }

    internal static ExerciseStartResult Started(
        ExerciseSession session,
        byte[] creationEventBytes,
        byte[] initialSnapshotBytes) =>
        new(
            session ?? throw new ArgumentNullException(nameof(session)),
            creationEventBytes ?? throw new ArgumentNullException(nameof(creationEventBytes)),
            initialSnapshotBytes ?? throw new ArgumentNullException(nameof(initialSnapshotBytes)),
            CampaignCreationRejectionReason.None);

    internal static ExerciseStartResult Rejected(
        CampaignCreationRejectionReason rejectionReason)
    {
        if (rejectionReason == CampaignCreationRejectionReason.None)
            throw new ArgumentOutOfRangeException(nameof(rejectionReason));
        return new ExerciseStartResult(null, null, null, rejectionReason);
    }
}
