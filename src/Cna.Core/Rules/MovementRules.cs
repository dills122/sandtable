namespace Cna.Core.Rules;

public enum MovementMobilityClass
{
    NonMotorized,
    Motorized,
}

public enum MovementRouteCostKind
{
    Override,
    ScaleUnderlying,
}

public enum MovementHexsideDirection
{
    Either,
    Up,
    Down,
}

public enum MovementRuleUnsupportedKind
{
    Mobility,
    Terrain,
    Route,
    Hexside,
    Organization,
}

public sealed class MovementRuleLookupResult<T>
{
    private readonly T? value;

    private MovementRuleLookupResult(
        bool isSupported,
        T? value,
        MovementRuleUnsupportedKind? unsupportedKind,
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
        : throw new InvalidOperationException("An unsupported Movement lookup has no value.");

    public MovementRuleUnsupportedKind? UnsupportedKind { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    internal static MovementRuleLookupResult<T> Supported(
        T value,
        IReadOnlyList<RuleReference> sources) => new(
            true,
            value,
            null,
            sources);

    internal static MovementRuleLookupResult<T> Unsupported(
        MovementRuleUnsupportedKind kind) => new(
            false,
            default,
            kind,
            Array.Empty<RuleReference>());
}

public sealed record MovementMobilityDefinition(
    string MobilityId,
    MovementMobilityClass MobilityClass,
    IReadOnlyList<RuleReference> Sources);

public sealed record MovementTerrainRule(
    CapabilityPointAmount Cost,
    int StoppingStackingLimit);

public sealed record MovementRouteRule(
    MovementRouteCostKind CostKind,
    CapabilityPointAmount Amount,
    int TraversalStackingLimit);

public sealed record MovementHexsideRule(CapabilityPointAmount AddedCost);

public sealed record MovementStackingRule(int StackingValue);
