using System.Collections.ObjectModel;

namespace Cna.Core.Rules;

public sealed record GameTurnRange
{
    public GameTurnRange(int first, int last)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(first, 1);

        ArgumentOutOfRangeException.ThrowIfLessThan(last, first);

        First = first;
        Last = last;
    }

    public int First { get; }

    public int Last { get; }

    public bool Contains(int gameTurn) => gameTurn >= First && gameTurn <= Last;
}

public sealed record CommonwealthInitiativeRating
{
    public CommonwealthInitiativeRating(
        int schemaVersion,
        GameTurnRange turns,
        int rating,
        IReadOnlyList<RuleReference> sources)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(schemaVersion, 1);
        ArgumentNullException.ThrowIfNull(turns);
        ArgumentOutOfRangeException.ThrowIfLessThan(rating, 1);

        SchemaVersion = schemaVersion;
        Turns = turns;
        Rating = rating;
        Sources = CopySources(sources);
    }

    public int SchemaVersion { get; }

    public GameTurnRange Turns { get; }

    public int Rating { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(CommonwealthInitiativeRating? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && SchemaVersion == other.SchemaVersion
            && Turns == other.Turns
            && Rating == other.Rating
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SchemaVersion);
        hash.Add(Turns);
        hash.Add(Rating);

        foreach (var source in Sources)
        {
            hash.Add(source);
        }

        return hash.ToHashCode();
    }

    private static ReadOnlyCollection<RuleReference> CopySources(
        IReadOnlyList<RuleReference> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var copy = sources.ToArray();

        if (copy.Length == 0 || copy.Any(source => source is null))
        {
            throw new ArgumentException(
                "At least one non-null source reference is required.",
                nameof(sources));
        }

        if (copy.Distinct().Count() != copy.Length)
        {
            throw new ArgumentException(
                "Duplicate source references are not allowed.",
                nameof(sources));
        }

        return Array.AsReadOnly(copy
            .OrderBy(source => source.SourceId, StringComparer.Ordinal)
            .ThenBy(source => source.Locator, StringComparer.Ordinal)
            .ToArray());
    }
}

public enum AxisInitiativePresence
{
    RommelOnQualifyingGameMap,
    GermanLandCombatUnitOnQualifyingGameMap,
    NeitherOnQualifyingGameMap,
}

public sealed record AxisInitiativeRating
{
    public AxisInitiativeRating(
        int schemaVersion,
        AxisInitiativePresence presence,
        int rating,
        IReadOnlyList<RuleReference> sources)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(schemaVersion, 1);

        if (!Enum.IsDefined(presence))
        {
            throw new ArgumentOutOfRangeException(nameof(presence));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(rating, 1);

        SchemaVersion = schemaVersion;
        Presence = presence;
        Rating = rating;
        Sources = CopySources(sources);
    }

    public int SchemaVersion { get; }

    public AxisInitiativePresence Presence { get; }

    public int Rating { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(AxisInitiativeRating? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && SchemaVersion == other.SchemaVersion
            && Presence == other.Presence
            && Rating == other.Rating
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SchemaVersion);
        hash.Add(Presence);
        hash.Add(Rating);

        foreach (var source in Sources)
        {
            hash.Add(source);
        }

        return hash.ToHashCode();
    }

    private static ReadOnlyCollection<RuleReference> CopySources(
        IReadOnlyList<RuleReference> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var copy = sources.ToArray();

        if (copy.Length == 0 || copy.Any(source => source is null))
        {
            throw new ArgumentException(
                "At least one non-null source reference is required.",
                nameof(sources));
        }

        if (copy.Distinct().Count() != copy.Length)
        {
            throw new ArgumentException(
                "Duplicate source references are not allowed.",
                nameof(sources));
        }

        return Array.AsReadOnly(copy
            .OrderBy(source => source.SourceId, StringComparer.Ordinal)
            .ThenBy(source => source.Locator, StringComparer.Ordinal)
            .ToArray());
    }
}

public enum AxisInitiativeLocation
{
    QualifyingGameMap,
    TripoliTunisiaHoldingBox,
    OffMapOrUnavailable,
}

public sealed record AxisInitiativeSourceFacts
{
    public AxisInitiativeSourceFacts(
        AxisInitiativeLocation rommelLocation,
        IReadOnlyList<AxisInitiativeLocation> germanLandCombatUnitLocations)
    {
        if (!Enum.IsDefined(rommelLocation))
        {
            throw new ArgumentOutOfRangeException(nameof(rommelLocation));
        }

        ArgumentNullException.ThrowIfNull(germanLandCombatUnitLocations);
        var locationCopy = germanLandCombatUnitLocations.ToArray();

        if (locationCopy.Any(location => !Enum.IsDefined(location)))
        {
            throw new ArgumentOutOfRangeException(nameof(germanLandCombatUnitLocations));
        }

        if (locationCopy.Distinct().Count() != locationCopy.Length)
        {
            throw new ArgumentException(
                "Duplicate German land combat unit location categories are not allowed.",
                nameof(germanLandCombatUnitLocations));
        }

        RommelLocation = rommelLocation;
        GermanLandCombatUnitLocations = Array.AsReadOnly(locationCopy.Order().ToArray());
    }

    public AxisInitiativeLocation RommelLocation { get; }

    public IReadOnlyList<AxisInitiativeLocation> GermanLandCombatUnitLocations { get; }

    public bool Equals(AxisInitiativeSourceFacts? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && RommelLocation == other.RommelLocation
            && GermanLandCombatUnitLocations.SequenceEqual(
                other.GermanLandCombatUnitLocations));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(RommelLocation);

        foreach (var location in GermanLandCombatUnitLocations)
        {
            hash.Add(location);
        }

        return hash.ToHashCode();
    }
}
