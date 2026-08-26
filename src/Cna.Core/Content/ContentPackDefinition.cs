using System.Collections.ObjectModel;

namespace Cna.Core.Content;

public sealed record ContentPackDefinition
{
    public const int CurrentSchemaVersion = 3;
    public const string CanonicalFormatId = "sandtable.content-json.v2";

    public ContentPackDefinition(
        int schemaVersion,
        string formatId,
        string packId,
        string rulesetId,
        IEnumerable<string> capabilities,
        IEnumerable<ContentSourceIndexEntry> sourceIndex,
        IEnumerable<ContentHex> locations,
        IEnumerable<ContentHexEdge> edges,
        IEnumerable<ContentFormation> formations,
        IEnumerable<ContentCombatElement> elements,
        IEnumerable<ContentScenario> scenarios)
        : this(
            schemaVersion,
            formatId,
            packId,
            rulesetId,
            capabilities,
            sourceIndex,
            locations,
            [],
            edges,
            formations,
            elements,
            scenarios)
    {
    }

    public ContentPackDefinition(
        int schemaVersion,
        string formatId,
        string packId,
        string rulesetId,
        IEnumerable<string> capabilities,
        IEnumerable<ContentSourceIndexEntry> sourceIndex,
        IEnumerable<ContentHex> locations,
        IEnumerable<ContentWeatherAreaAssignment> weatherAreaAssignments,
        IEnumerable<ContentHexEdge> edges,
        IEnumerable<ContentFormation> formations,
        IEnumerable<ContentCombatElement> elements,
        IEnumerable<ContentScenario> scenarios)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(schemaVersion, CurrentSchemaVersion);

        if (!string.Equals(formatId, CanonicalFormatId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                $"The only supported format is '{CanonicalFormatId}'.",
                nameof(formatId));
        }

        ArgumentNullException.ThrowIfNull(capabilities);
        var capabilityCopy = capabilities.ToArray();

        if (capabilityCopy.Any(capability => capability is null))
        {
            throw new ArgumentException("Null capabilities are not allowed.", nameof(capabilities));
        }

        foreach (var capability in capabilityCopy)
        {
            ContentContractGuards.RequireStableId(capability, nameof(capabilities));
        }

        if (capabilityCopy.Distinct(StringComparer.Ordinal).Count() != capabilityCopy.Length)
        {
            throw new ArgumentException("Duplicate capabilities are not allowed.", nameof(capabilities));
        }

        SchemaVersion = schemaVersion;
        FormatId = formatId;
        PackId = ContentContractGuards.RequireStableId(packId, nameof(packId));
        RulesetId = ContentContractGuards.RequireStableId(rulesetId, nameof(rulesetId));
        Capabilities = Array.AsReadOnly(capabilityCopy.Order(StringComparer.Ordinal).ToArray());
        SourceIndex = CopyAndOrder(sourceIndex, value => value.SourceId, nameof(sourceIndex));
        Locations = CopyAndOrder(locations, value => value.LocationId, nameof(locations));
        WeatherAreaAssignments = CopyAndOrder(
            weatherAreaAssignments,
            value => value.LocationId,
            nameof(weatherAreaAssignments));
        Edges = CopyAndOrder(
            edges,
            value => $"{value.FirstLocationId}\0{value.SecondLocationId}",
            nameof(edges));
        Formations = CopyAndOrder(formations, value => value.FormationId, nameof(formations));
        Elements = CopyAndOrder(elements, value => value.ElementId, nameof(elements));
        Scenarios = CopyAndOrder(scenarios, value => value.ScenarioId, nameof(scenarios));
    }

    public int SchemaVersion { get; }

    public string FormatId { get; }

    public string PackId { get; }

    public string RulesetId { get; }

    public IReadOnlyList<string> Capabilities { get; }

    public IReadOnlyList<ContentSourceIndexEntry> SourceIndex { get; }

    public IReadOnlyList<ContentHex> Locations { get; }

    public IReadOnlyList<ContentWeatherAreaAssignment> WeatherAreaAssignments { get; }

    public IReadOnlyList<ContentHexEdge> Edges { get; }

    public IReadOnlyList<ContentFormation> Formations { get; }

    public IReadOnlyList<ContentCombatElement> Elements { get; }

    public IReadOnlyList<ContentScenario> Scenarios { get; }

    public bool Equals(ContentPackDefinition? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && SchemaVersion == other.SchemaVersion
            && string.Equals(FormatId, other.FormatId, StringComparison.Ordinal)
            && string.Equals(PackId, other.PackId, StringComparison.Ordinal)
            && string.Equals(RulesetId, other.RulesetId, StringComparison.Ordinal)
            && Capabilities.SequenceEqual(other.Capabilities, StringComparer.Ordinal)
            && SourceIndex.SequenceEqual(other.SourceIndex)
            && Locations.SequenceEqual(other.Locations)
            && WeatherAreaAssignments.SequenceEqual(other.WeatherAreaAssignments)
            && Edges.SequenceEqual(other.Edges)
            && Formations.SequenceEqual(other.Formations)
            && Elements.SequenceEqual(other.Elements)
            && Scenarios.SequenceEqual(other.Scenarios));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SchemaVersion);
        hash.Add(FormatId, StringComparer.Ordinal);
        hash.Add(PackId, StringComparer.Ordinal);
        hash.Add(RulesetId, StringComparer.Ordinal);
        AddValues(ref hash, Capabilities);
        AddValues(ref hash, SourceIndex);
        AddValues(ref hash, Locations);
        AddValues(ref hash, WeatherAreaAssignments);
        AddValues(ref hash, Edges);
        AddValues(ref hash, Formations);
        AddValues(ref hash, Elements);
        AddValues(ref hash, Scenarios);
        return hash.ToHashCode();
    }

    private static ReadOnlyCollection<T> CopyAndOrder<T>(
        IEnumerable<T> values,
        Func<T, string> keySelector,
        string parameterName)
        where T : class
    {
        var copy = ContentContractGuards.CopyValues(values, parameterName);
        return Array.AsReadOnly(copy.OrderBy(keySelector, StringComparer.Ordinal).ToArray());
    }

    private static void AddValues<T>(ref HashCode hash, IEnumerable<T> values)
    {
        foreach (var value in values)
        {
            hash.Add(value);
        }
    }
}
