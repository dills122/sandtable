namespace Cna.Core.Decisions;

public enum DecisionProposalRejectionReason
{
    None = 0,
    DecisionIdMismatch = 1,
    StaleState = 2,
    RulesetMismatch = 3,
    UnknownPlan = 4,
}
