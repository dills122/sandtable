namespace Cna.Core.Rules;

/// <summary>
/// Shared validation/normalization for <see cref="RuleReference"/> source collections:
/// reject a null sequence, reject an empty or null-containing sequence, and — for call
/// sites that require a canonical order — reject duplicate entries and return the
/// sources sorted by <see cref="RuleReference.SourceId"/> then
/// <see cref="RuleReference.Locator"/> (both ordinal).
/// </summary>
internal static class RuleReferenceValidation
{
    /// <param name="sortAndDeduplicate">
    /// When <see langword="true"/> (the default), duplicate sources are rejected and the
    /// result is sorted into canonical <see cref="RuleReference.SourceId"/>/
    /// <see cref="RuleReference.Locator"/> order. Some call sites intentionally preserve
    /// the caller-supplied order and allow duplicates; pass <see langword="false"/> for
    /// those.
    /// </param>
    public static IReadOnlyList<RuleReference> CopySources(
        IEnumerable<RuleReference> sources,
        string paramName,
        bool sortAndDeduplicate = true)
    {
        ArgumentNullException.ThrowIfNull(sources, paramName);
        var copy = sources.ToArray();

        if (copy.Length == 0 || copy.Any(source => source is null))
        {
            throw new ArgumentException(
                "At least one non-null source reference is required.",
                paramName);
        }

        if (!sortAndDeduplicate)
        {
            return Array.AsReadOnly(copy);
        }

        if (copy.Distinct().Count() != copy.Length)
        {
            throw new ArgumentException(
                "Duplicate source references are not allowed.",
                paramName);
        }

        return Array.AsReadOnly(copy
            .OrderBy(source => source.SourceId, StringComparer.Ordinal)
            .ThenBy(source => source.Locator, StringComparer.Ordinal)
            .ToArray());
    }
}
