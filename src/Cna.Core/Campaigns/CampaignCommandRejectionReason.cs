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
    UnknownSetup,
    SetupHashMismatch,
    UnknownContent,
    ContentHashMismatch,
    UnsupportedRuleset,
    UnknownScenario,
    SetupContentMismatch,
    ScenarioStartMismatch,
}
