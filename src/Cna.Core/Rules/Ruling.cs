namespace Cna.Core.Rules;

public sealed record Ruling
{
    public Ruling(
        string rulingId,
        string decisionId,
        IEnumerable<RuleReference> sources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rulingId);
        ArgumentException.ThrowIfNullOrWhiteSpace(decisionId);
        ArgumentNullException.ThrowIfNull(sources);

        RulingId = rulingId;
        DecisionId = decisionId;
        Sources = Array.AsReadOnly(sources.ToArray());

        if (Sources.Count == 0)
        {
            throw new ArgumentException("At least one source reference is required.", nameof(sources));
        }
    }

    public string RulingId { get; }

    public string DecisionId { get; }

    public IReadOnlyList<RuleReference> Sources { get; }
}
