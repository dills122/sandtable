namespace Cna.Core.Campaigns;

public enum CampaignCommandRejectionReason
{
    None,
    CampaignAlreadyCreated,
    CampaignNotCreated,
    InvalidCommand,
    InvalidState,
    StaleState,
    UnexpectedSequenceStep,
    UnsupportedTransition,
}
