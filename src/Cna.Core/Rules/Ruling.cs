namespace Cna.Core.Rules;

public sealed record Ruling
{
    public Ruling(
        string rulingId,
        string conflictId,
        IEnumerable<string> alternativeIds,
        string selectedBehaviorId,
        IEnumerable<string> protectingTestIds,
        IEnumerable<RuleReference> sources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rulingId);
        ArgumentException.ThrowIfNullOrWhiteSpace(conflictId);
        ArgumentNullException.ThrowIfNull(alternativeIds);
        ArgumentException.ThrowIfNullOrWhiteSpace(selectedBehaviorId);
        ArgumentNullException.ThrowIfNull(protectingTestIds);

        RulingId = rulingId;
        ConflictId = conflictId;
        AlternativeIds = CopyRequiredUniqueValues(alternativeIds, nameof(alternativeIds));
        SelectedBehaviorId = selectedBehaviorId;
        ProtectingTestIds = CopyRequiredUniqueValues(
            protectingTestIds,
            nameof(protectingTestIds));
        Sources = RuleReferenceValidation.CopySources(
            sources,
            nameof(sources),
            sortAndDeduplicate: false);

        if (!AlternativeIds.Contains(SelectedBehaviorId, StringComparer.Ordinal))
        {
            throw new ArgumentException(
                "The selected behavior must be one of the considered alternatives.",
                nameof(selectedBehaviorId));
        }
    }

    public string RulingId { get; }

    public string ConflictId { get; }

    public IReadOnlyList<string> AlternativeIds { get; }

    public string SelectedBehaviorId { get; }

    public IReadOnlyList<string> ProtectingTestIds { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    private static System.Collections.ObjectModel.ReadOnlyCollection<string> CopyRequiredUniqueValues(
        IEnumerable<string> values,
        string parameterName)
    {
        var copy = values.ToArray();

        if (copy.Length == 0 || copy.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "At least one non-empty identifier is required.",
                parameterName);
        }

        if (copy.Distinct(StringComparer.Ordinal).Count() != copy.Length)
        {
            throw new ArgumentException("Duplicate identifiers are not allowed.", parameterName);
        }

        return Array.AsReadOnly(copy);
    }
}
