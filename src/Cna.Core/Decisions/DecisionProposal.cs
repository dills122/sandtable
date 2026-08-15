namespace Cna.Core.Decisions;

public sealed record DecisionProposal(
    string DecisionId,
    long BasedOnStateVersion,
    string RulesetHash,
    string SelectedPlanId);
