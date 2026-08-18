namespace Cna.Core.Campaigns;

internal sealed record CampaignReplayResult
{
    internal CampaignReplayResult(
        IEnumerable<CampaignEvent> events,
        CampaignSnapshot? snapshot,
        CampaignCommandRejectionReason rejectionReason,
        int? rejectedCommandIndex)
    {
        ArgumentNullException.ThrowIfNull(events);

        Events = Array.AsReadOnly(events.ToArray());
        Snapshot = snapshot;
        RejectionReason = rejectionReason;
        RejectedCommandIndex = rejectedCommandIndex;
    }

    public bool IsAccepted => RejectionReason == CampaignCommandRejectionReason.None;

    public IReadOnlyList<CampaignEvent> Events { get; }

    public CampaignSnapshot? Snapshot { get; }

    public CampaignCommandRejectionReason RejectionReason { get; }

    public int? RejectedCommandIndex { get; }
}
