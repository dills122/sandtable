namespace Cna.Core.Rules;

public sealed record RuleReference
{
    public RuleReference(string sourceId, string locator)
    {
        SourceId = RequireValue(sourceId, nameof(sourceId));
        Locator = RequireValue(locator, nameof(locator));
    }

    public string SourceId { get; }

    public string Locator { get; }

    private static string RequireValue(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        return value;
    }
}
