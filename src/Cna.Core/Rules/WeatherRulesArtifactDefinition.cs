using System.Collections.ObjectModel;

namespace Cna.Core.Rules;

internal sealed record WeatherRulesArtifactDefinition
{
    public WeatherRulesArtifactDefinition(
        int schemaVersion,
        WeatherArtifactProvenance provenance,
        IEnumerable<WeatherTableDefinition> seasons,
        IEnumerable<FoulWeatherLocationDefinition> foulWeatherLocations,
        IEnumerable<DeferredWeatherRuleDefinition> deferredRules,
        IEnumerable<RuleReference> sources)
    {
        ArgumentNullException.ThrowIfNull(provenance);

        SchemaVersion = schemaVersion;
        Provenance = provenance;
        Seasons = Copy(seasons, nameof(seasons));
        FoulWeatherLocations = Copy(foulWeatherLocations, nameof(foulWeatherLocations));
        DeferredRules = Copy(deferredRules, nameof(deferredRules));
        Sources = Copy(sources, nameof(sources));
    }

    public int SchemaVersion { get; }

    public WeatherArtifactProvenance Provenance { get; }

    public IReadOnlyList<WeatherTableDefinition> Seasons { get; }

    public IReadOnlyList<FoulWeatherLocationDefinition> FoulWeatherLocations { get; }

    public IReadOnlyList<DeferredWeatherRuleDefinition> DeferredRules { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(WeatherRulesArtifactDefinition? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && SchemaVersion == other.SchemaVersion
            && Provenance == other.Provenance
            && Seasons.SequenceEqual(other.Seasons)
            && FoulWeatherLocations.SequenceEqual(other.FoulWeatherLocations)
            && DeferredRules.SequenceEqual(other.DeferredRules)
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SchemaVersion);
        hash.Add(Provenance);
        AddValues(ref hash, Seasons);
        AddValues(ref hash, FoulWeatherLocations);
        AddValues(ref hash, DeferredRules);
        AddValues(ref hash, Sources);
        return hash.ToHashCode();
    }

    private static ReadOnlyCollection<T> Copy<T>(IEnumerable<T> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        return Array.AsReadOnly(values.ToArray());
    }

    private static void AddValues<T>(ref HashCode hash, IEnumerable<T> values)
    {
        foreach (var value in values)
        {
            hash.Add(value);
        }
    }
}

internal sealed record WeatherArtifactProvenance
{
    public WeatherArtifactProvenance(
        IEnumerable<RuleReference> gameTurnRanges,
        IEnumerable<RuleReference> outcomes,
        IEnumerable<RuleReference> foulWeatherLocations)
    {
        GameTurnRanges = Copy(gameTurnRanges, nameof(gameTurnRanges));
        Outcomes = Copy(outcomes, nameof(outcomes));
        FoulWeatherLocations = Copy(foulWeatherLocations, nameof(foulWeatherLocations));
    }

    public IReadOnlyList<RuleReference> GameTurnRanges { get; }

    public IReadOnlyList<RuleReference> Outcomes { get; }

    public IReadOnlyList<RuleReference> FoulWeatherLocations { get; }

    public bool Equals(WeatherArtifactProvenance? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && GameTurnRanges.SequenceEqual(other.GameTurnRanges)
            && Outcomes.SequenceEqual(other.Outcomes)
            && FoulWeatherLocations.SequenceEqual(other.FoulWeatherLocations));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        AddSources(ref hash, GameTurnRanges);
        AddSources(ref hash, Outcomes);
        AddSources(ref hash, FoulWeatherLocations);
        return hash.ToHashCode();
    }

    private static ReadOnlyCollection<RuleReference> Copy(
        IEnumerable<RuleReference> values,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        return Array.AsReadOnly(values.ToArray());
    }

    private static void AddSources(ref HashCode hash, IEnumerable<RuleReference> sources)
    {
        foreach (var source in sources)
        {
            hash.Add(source);
        }
    }
}

internal sealed record WeatherTableDefinition
{
    public WeatherTableDefinition(
        WeatherSeason season,
        IEnumerable<GameTurnRange> gameTurnRanges,
        IEnumerable<WeatherD66OutcomeDefinition> outcomes)
    {
        ArgumentNullException.ThrowIfNull(gameTurnRanges);
        ArgumentNullException.ThrowIfNull(outcomes);

        Season = season;
        GameTurnRanges = Array.AsReadOnly(gameTurnRanges.ToArray());
        Outcomes = Array.AsReadOnly(outcomes.ToArray());
    }

    public WeatherSeason Season { get; }

    public IReadOnlyList<GameTurnRange> GameTurnRanges { get; }

    public IReadOnlyList<WeatherD66OutcomeDefinition> Outcomes { get; }

    public bool Equals(WeatherTableDefinition? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && Season == other.Season
            && GameTurnRanges.SequenceEqual(other.GameTurnRanges)
            && Outcomes.SequenceEqual(other.Outcomes));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Season);
        foreach (var range in GameTurnRanges)
        {
            hash.Add(range);
        }
        foreach (var outcome in Outcomes)
        {
            hash.Add(outcome);
        }
        return hash.ToHashCode();
    }
}

internal sealed record WeatherD66OutcomeDefinition(
    WeatherKind Kind,
    int FirstD66,
    int LastD66);

internal sealed record FoulWeatherLocationDefinition
{
    public FoulWeatherLocationDefinition(int die, IEnumerable<WeatherArea> areas)
    {
        ArgumentNullException.ThrowIfNull(areas);

        Die = die;
        Areas = Array.AsReadOnly(areas.ToArray());
    }

    public int Die { get; }

    public IReadOnlyList<WeatherArea> Areas { get; }

    public bool Equals(FoulWeatherLocationDefinition? other) =>
        ReferenceEquals(this, other)
        || (other is not null && Die == other.Die && Areas.SequenceEqual(other.Areas));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Die);
        foreach (var area in Areas)
        {
            hash.Add(area);
        }
        return hash.ToHashCode();
    }
}

internal sealed record DeferredWeatherRuleDefinition
{
    public DeferredWeatherRuleDefinition(
        string ruleId,
        WeatherKind weatherKind,
        WeatherArea area,
        string status,
        IEnumerable<RuleReference> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        RuleId = ruleId;
        WeatherKind = weatherKind;
        Area = area;
        Status = status;
        Sources = Array.AsReadOnly(sources.ToArray());
    }

    public string RuleId { get; }

    public WeatherKind WeatherKind { get; }

    public WeatherArea Area { get; }

    public string Status { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(DeferredWeatherRuleDefinition? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(RuleId, other.RuleId, StringComparison.Ordinal)
            && WeatherKind == other.WeatherKind
            && Area == other.Area
            && string.Equals(Status, other.Status, StringComparison.Ordinal)
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(RuleId, StringComparer.Ordinal);
        hash.Add(WeatherKind);
        hash.Add(Area);
        hash.Add(Status, StringComparer.Ordinal);
        foreach (var source in Sources)
        {
            hash.Add(source);
        }
        return hash.ToHashCode();
    }
}
