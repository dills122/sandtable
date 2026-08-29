using System.Collections.Frozen;

namespace Cna.Core.Decisions;

public sealed record PendingDecision
{
    private string decisionId = string.Empty;
    private string rulesetHash = string.Empty;
    private IReadOnlySet<string> validPlanIds = FrozenSet<string>.Empty;

    // PascalCase parameter names preserve named-argument compatibility with the former
    // positional record declaration.
    public PendingDecision(
        string DecisionId,
        long StateVersion,
        string RulesetHash,
        IReadOnlySet<string> ValidPlanIds)
    {
        this.DecisionId = DecisionId;
        this.StateVersion = StateVersion;
        this.RulesetHash = RulesetHash;
        this.ValidPlanIds = ValidPlanIds;
    }

    public string DecisionId
    {
        get => decisionId;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            decisionId = value;
        }
    }

    public long StateVersion
    {
        get;
        init
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
        }
    }

    public string RulesetHash
    {
        get => rulesetHash;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            rulesetHash = value;
        }
    }

    public IReadOnlySet<string> ValidPlanIds
    {
        get => validPlanIds;
        init => validPlanIds = FreezePlanIds(value);
    }

    public void Deconstruct(
        out string decisionId,
        out long stateVersion,
        out string rulesetHash,
        out IReadOnlySet<string> validPlanIds)
    {
        decisionId = DecisionId;
        stateVersion = StateVersion;
        rulesetHash = RulesetHash;
        validPlanIds = ValidPlanIds;
    }

    private static FrozenSet<string> FreezePlanIds(IReadOnlySet<string> value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var planIds = value.ToArray();
        if (planIds.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Plan identifiers cannot be null or whitespace.",
                nameof(value));
        }

        return planIds.ToFrozenSet(StringComparer.Ordinal);
    }
}
