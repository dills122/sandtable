namespace Cna.Core.Campaigns;

public enum CampaignCommandRejectionReason
{
    None,
    CampaignAlreadyCreated,
    CampaignNotCreated,
    InvalidCommand,
    StaleState,
    UnexpectedSequenceStep,
    UnsupportedTransition,
}
