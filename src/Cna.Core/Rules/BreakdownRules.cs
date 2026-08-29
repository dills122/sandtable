namespace Cna.Core.Rules;

public enum BreakdownWeatherKind
{
    Normal,
    Hot,
    Sandstorm,
    Rainstorm,
}

public enum BreakdownInputOperation
{
    Override,
    ScaleUnderlying,
}

public enum BreakdownHexsideDirection
{
    Either,
    Up,
    Down,
}

public enum BreakdownRuleUnsupportedKind
{
    Profile,
    VehicleType,
    Terrain,
    Route,
    Hexside,
    WeatherInputTransformation,
}

public sealed class BreakdownRuleLookupResult<T>
{
    private readonly T? value;

    private BreakdownRuleLookupResult(
        bool isSupported,
        T? value,
        BreakdownRuleUnsupportedKind? unsupportedKind,
        IReadOnlyList<RuleReference> sources)
    {
        IsSupported = isSupported;
        this.value = value;
        UnsupportedKind = unsupportedKind;
        Sources = sources;
    }

    public bool IsSupported { get; }

    public T Value => IsSupported
        ? value!
        : throw new InvalidOperationException("An unsupported Breakdown lookup has no value.");

    public BreakdownRuleUnsupportedKind? UnsupportedKind { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    internal static BreakdownRuleLookupResult<T> Supported(
        T value,
        IReadOnlyList<RuleReference> sources) => new(true, value, null, sources);

    internal static BreakdownRuleLookupResult<T> Unsupported(
        BreakdownRuleUnsupportedKind kind) => new(
            false,
            default,
            kind,
            Array.Empty<RuleReference>());
}

public sealed record BreakdownBandRule(
    string BandId,
    int MinimumWholePoints,
    int? MaximumWholePoints,
    bool IsCheckEligible);

public sealed record BreakdownProfileRule(string ProfileId, int ColumnShift);

public sealed record BreakdownVehicleTypeRule(string VehicleTypeId, string ProfileId);

public sealed record BreakdownTerrainRule(BreakdownPointAmount Points);

public sealed record BreakdownRouteRule(
    BreakdownInputOperation Operation,
    BreakdownPointAmount Amount);

public sealed record BreakdownHexsideRule(BreakdownPointAmount AddedPoints);

public sealed record BreakdownWeatherInputTransformationRule(
    string InputRouteId,
    string TreatedAsRouteId);
