namespace Cna.Core.Campaigns;

public sealed record CampaignCommandResult
{
    private CampaignCommandResult(
        IReadOnlyList<CampaignEvent> events,
        CampaignCommandRejectionReason rejectionReason)
    {
        Events = events;
        RejectionReason = rejectionReason;
    }

    public bool IsAccepted => RejectionReason == CampaignCommandRejectionReason.None;

    public IReadOnlyList<CampaignEvent> Events { get; }

    public CampaignCommandRejectionReason RejectionReason { get; }

    public static CampaignCommandResult Accept(CampaignEvent campaignEvent)
    {
        ArgumentNullException.ThrowIfNull(campaignEvent);
        return new CampaignCommandResult(
            Array.AsReadOnly<CampaignEvent>([campaignEvent]),
            CampaignCommandRejectionReason.None);
    }

    public static CampaignCommandResult Reject(CampaignCommandRejectionReason reason)
    {
        if (reason == CampaignCommandRejectionReason.None)
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        return new CampaignCommandResult(
            Array.Empty<CampaignEvent>(),
            reason);
    }
}
