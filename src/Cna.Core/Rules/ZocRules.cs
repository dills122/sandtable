namespace Cna.Core.Rules;

public enum ZocRuleEvaluationStatus
{
    Qualified,
    NotQualified,
    Unsupported,
}

public enum ZocRuleUnsupportedKind
{
    CombatClassification,
    TopologyFeature,
}

public enum ZocSourceFailureKind
{
    ExcludedCombatClassification,
    UnattachedHeadquarters,
    InsufficientStackingPoints,
    CohesionTooLow,
    InsufficientRawDefensiveCloseAssaultPoints,
}

public enum ZocProjectionFailureKind
{
    ExcludedHexside,
    DestinationNotEnterable,
}

public enum ZocTopologyFeatureKind
{
    PassThrough,
    AllSea,
    MajorRiver,
    Lake,
    Escarpment,
}

public sealed record ZocSourceFacts
{
    public ZocSourceFacts(
        string combatClassificationId,
        int aggregateStackingPoints,
        int cohesionLevel,
        long rawDefensiveCloseAssaultPoints,
        bool hasAttachedCombatUnits)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(combatClassificationId);
        ArgumentOutOfRangeException.ThrowIfNegative(aggregateStackingPoints);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cohesionLevel, 10);
        ArgumentOutOfRangeException.ThrowIfNegative(rawDefensiveCloseAssaultPoints);

        CombatClassificationId = combatClassificationId;
        AggregateStackingPoints = aggregateStackingPoints;
        CohesionLevel = cohesionLevel;
        RawDefensiveCloseAssaultPoints = rawDefensiveCloseAssaultPoints;
        HasAttachedCombatUnits = hasAttachedCombatUnits;
    }

    public string CombatClassificationId { get; }

    public int AggregateStackingPoints { get; }

    public int CohesionLevel { get; }

    public long RawDefensiveCloseAssaultPoints { get; }

    public bool HasAttachedCombatUnits { get; }
}

public sealed record ZocProjectionFacts
{
    public ZocProjectionFacts(
        IEnumerable<string> hexsideFeatureIds,
        bool canSourceForceEnterDestination)
    {
        ArgumentNullException.ThrowIfNull(hexsideFeatureIds);
        var copy = hexsideFeatureIds.ToArray();
        if (copy.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Hexside feature IDs cannot contain a null or whitespace value.",
                nameof(hexsideFeatureIds));
        }

        if (copy.Distinct(StringComparer.Ordinal).Count() != copy.Length)
        {
            throw new ArgumentException(
                "Duplicate hexside feature IDs are not allowed.",
                nameof(hexsideFeatureIds));
        }

        HexsideFeatureIds = Array.AsReadOnly(copy
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray());
        CanSourceForceEnterDestination = canSourceForceEnterDestination;
    }

    public IReadOnlyList<string> HexsideFeatureIds { get; }

    public bool CanSourceForceEnterDestination { get; }
}

public sealed record ZocControlCandidate
{
    public ZocControlCandidate(
        string destinationLocationId,
        ZocSourceFacts source,
        ZocProjectionFacts projection)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationLocationId);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(projection);
        DestinationLocationId = destinationLocationId;
        Source = source;
        Projection = projection;
    }

    public string DestinationLocationId { get; }

    public ZocSourceFacts Source { get; }

    public ZocProjectionFacts Projection { get; }
}

public sealed record ZocTopologyFeatureDefinition
{
    public ZocTopologyFeatureDefinition(
        string featureId,
        ZocTopologyFeatureKind kind,
        IEnumerable<RuleReference> sources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(featureId);
        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        FeatureId = featureId;
        Kind = kind;
        Sources = RuleReferenceValidation.CopySources(sources, nameof(sources));
    }

    public string FeatureId { get; }

    public ZocTopologyFeatureKind Kind { get; }

    public IReadOnlyList<RuleReference> Sources { get; }
}

public sealed class ZocSourceQualificationResult
{
    private ZocSourceQualificationResult(
        ZocRuleEvaluationStatus status,
        ZocRuleUnsupportedKind? unsupportedKind,
        IReadOnlyList<ZocSourceFailureKind> failures,
        IReadOnlyList<RuleReference> sources)
    {
        Status = status;
        UnsupportedKind = unsupportedKind;
        Failures = failures;
        Sources = sources;
    }

    public ZocRuleEvaluationStatus Status { get; }

    public bool IsSupported => Status != ZocRuleEvaluationStatus.Unsupported;

    public bool IsQualified => Status == ZocRuleEvaluationStatus.Qualified;

    public ZocRuleUnsupportedKind? UnsupportedKind { get; }

    public IReadOnlyList<ZocSourceFailureKind> Failures { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    internal static ZocSourceQualificationResult Qualified(
        IReadOnlyList<RuleReference> sources) => new(
            ZocRuleEvaluationStatus.Qualified,
            null,
            Array.Empty<ZocSourceFailureKind>(),
            sources);

    internal static ZocSourceQualificationResult NotQualified(
        IReadOnlyList<ZocSourceFailureKind> failures,
        IReadOnlyList<RuleReference> sources) => new(
            ZocRuleEvaluationStatus.NotQualified,
            null,
            failures,
            sources);

    internal static ZocSourceQualificationResult Unsupported(
        ZocRuleUnsupportedKind unsupportedKind) => new(
            ZocRuleEvaluationStatus.Unsupported,
            unsupportedKind,
            Array.Empty<ZocSourceFailureKind>(),
            Array.Empty<RuleReference>());
}

public sealed class ZocProjectionResult
{
    private ZocProjectionResult(
        ZocRuleEvaluationStatus status,
        ZocRuleUnsupportedKind? unsupportedKind,
        IReadOnlyList<ZocProjectionFailureKind> failures,
        IReadOnlyList<RuleReference> sources)
    {
        Status = status;
        UnsupportedKind = unsupportedKind;
        Failures = failures;
        Sources = sources;
    }

    public ZocRuleEvaluationStatus Status { get; }

    public bool IsSupported => Status != ZocRuleEvaluationStatus.Unsupported;

    public bool IsQualified => Status == ZocRuleEvaluationStatus.Qualified;

    public ZocRuleUnsupportedKind? UnsupportedKind { get; }

    public IReadOnlyList<ZocProjectionFailureKind> Failures { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    internal static ZocProjectionResult Qualified(
        IReadOnlyList<RuleReference> sources) => new(
            ZocRuleEvaluationStatus.Qualified,
            null,
            Array.Empty<ZocProjectionFailureKind>(),
            sources);

    internal static ZocProjectionResult NotQualified(
        IReadOnlyList<ZocProjectionFailureKind> failures,
        IReadOnlyList<RuleReference> sources) => new(
            ZocRuleEvaluationStatus.NotQualified,
            null,
            failures,
            sources);

    internal static ZocProjectionResult Unsupported(
        ZocRuleUnsupportedKind unsupportedKind) => new(
            ZocRuleEvaluationStatus.Unsupported,
            unsupportedKind,
            Array.Empty<ZocProjectionFailureKind>(),
            Array.Empty<RuleReference>());
}
