namespace Cna.Core.Decisions;

public sealed record PendingDecision(
    string DecisionId,
    long StateVersion,
    string RulesetHash,
    IReadOnlySet<string> ValidPlanIds);
