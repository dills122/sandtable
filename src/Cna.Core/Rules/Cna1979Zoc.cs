namespace Cna.Core.Rules;

public static class Cna1979Zoc
{
    public const string AuthorityId = "cna-1979.1.zoc-rules";
    public const string AllSeaHexsideId = "land.edge.all-sea";
    public const string MajorRiverHexsideId = "land.edge.major-river";
    public const string LakeHexsideId = "land.edge.lake";
    public const string EscarpmentHexsideId = "land.edge.escarpment";

    private static readonly RuleReference QualificationSource = new(
        "spi-1979-land-rules",
        "10.11");
    private static readonly RuleReference NonCombatSource = new(
        "spi-1979-land-rules",
        "10.12");
    private static readonly RuleReference MarkerSource = new(
        "spi-1979-land-rules",
        "10.13");
    private static readonly RuleReference CohesionSource = new(
        "spi-1979-land-rules",
        "10.14");
    private static readonly RuleReference RawDefenseSource = new(
        "spi-1979-land-rules",
        "10.15");
    private static readonly RuleReference ProjectionSource = new(
        "spi-1979-land-rules",
        "10.21");
    private static readonly RuleReference WaterHexsideSource = new(
        "spi-1979-land-rules",
        "10.21a");
    private static readonly RuleReference EscarpmentSource = new(
        "spi-1979-land-rules",
        "10.21b");
    private static readonly RuleReference EnterabilitySource = new(
        "spi-1979-land-rules",
        "10.21c");

    private static readonly IReadOnlyList<ZocTopologyFeatureDefinition> TopologyAuthority =
        Array.AsReadOnly<ZocTopologyFeatureDefinition>(
        [
            Topology("land.edge.road", ZocTopologyFeatureKind.PassThrough, ProjectionSource),
            Topology("land.edge.track", ZocTopologyFeatureKind.PassThrough, ProjectionSource),
            Topology("land.edge.ridge", ZocTopologyFeatureKind.PassThrough, ProjectionSource),
            Topology("land.edge.slope", ZocTopologyFeatureKind.PassThrough, ProjectionSource),
            Topology(AllSeaHexsideId, ZocTopologyFeatureKind.AllSea, WaterHexsideSource),
            Topology(MajorRiverHexsideId, ZocTopologyFeatureKind.MajorRiver, WaterHexsideSource),
            Topology(LakeHexsideId, ZocTopologyFeatureKind.Lake, WaterHexsideSource),
            Topology(EscarpmentHexsideId, ZocTopologyFeatureKind.Escarpment, EscarpmentSource),
        ]);

    private static readonly IReadOnlyList<RuleReference> CompleteQualificationSources =
        RuleReferenceValidation.CopySources(
            [
                QualificationSource,
                NonCombatSource,
                MarkerSource,
                CohesionSource,
                RawDefenseSource,
            ],
            "sources");

    public static IReadOnlyList<ZocTopologyFeatureDefinition> TopologyFeatures =>
        TopologyAuthority;

    public static bool IsSupportedTopologyFeatureId(string? featureId) =>
        TopologyAuthority.Any(value => string.Equals(
            value.FeatureId,
            featureId,
            StringComparison.Ordinal));

    public static ZocSourceQualificationResult EvaluateSource(ZocSourceFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var classification = Cna1979Combat.FindClassification(
            facts.CombatClassificationId);
        if (classification is null)
        {
            return ZocSourceQualificationResult.Unsupported(
                ZocRuleUnsupportedKind.CombatClassification);
        }

        var failures = new List<ZocSourceFailureKind>();
        var sources = new List<RuleReference>();

        if (classification.Kind is not (
            ZocCombatClassificationKind.CombatUnit
            or ZocCombatClassificationKind.Headquarters))
        {
            failures.Add(ZocSourceFailureKind.ExcludedCombatClassification);
            sources.AddRange(classification.Sources);
        }

        if (classification.Kind == ZocCombatClassificationKind.Headquarters
            && !facts.HasAttachedCombatUnits)
        {
            failures.Add(ZocSourceFailureKind.UnattachedHeadquarters);
            sources.Add(QualificationSource);
        }

        if (facts.AggregateStackingPoints <= 1)
        {
            failures.Add(ZocSourceFailureKind.InsufficientStackingPoints);
            sources.Add(QualificationSource);
        }

        if (facts.CohesionLevel <= -26)
        {
            failures.Add(ZocSourceFailureKind.CohesionTooLow);
            sources.Add(CohesionSource);
        }

        if (facts.RawDefensiveCloseAssaultPoints < 10)
        {
            failures.Add(ZocSourceFailureKind.InsufficientRawDefensiveCloseAssaultPoints);
            sources.Add(RawDefenseSource);
        }

        if (failures.Count == 0)
        {
            return ZocSourceQualificationResult.Qualified(
                CompleteQualificationSources);
        }

        return ZocSourceQualificationResult.NotQualified(
            Array.AsReadOnly(failures
                .Distinct()
                .OrderBy(value => value)
                .ToArray()),
            CanonicalSources(sources));
    }

    public static ZocProjectionResult EvaluateProjection(ZocProjectionFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        var definitions = new List<ZocTopologyFeatureDefinition>();
        foreach (var featureId in facts.HexsideFeatureIds)
        {
            var definition = TopologyAuthority.SingleOrDefault(value => string.Equals(
                value.FeatureId,
                featureId,
                StringComparison.Ordinal));
            if (definition is null)
            {
                return ZocProjectionResult.Unsupported(
                    ZocRuleUnsupportedKind.TopologyFeature);
            }

            definitions.Add(definition);
        }

        var failures = new List<ZocProjectionFailureKind>();
        var sources = new List<RuleReference>();
        foreach (var definition in definitions.Where(value =>
            value.Kind != ZocTopologyFeatureKind.PassThrough))
        {
            failures.Add(ZocProjectionFailureKind.ExcludedHexside);
            sources.AddRange(definition.Sources);
        }

        if (!facts.CanSourceForceEnterDestination)
        {
            failures.Add(ZocProjectionFailureKind.DestinationNotEnterable);
            sources.Add(EnterabilitySource);
        }

        if (failures.Count == 0)
        {
            return ZocProjectionResult.Qualified(
                Array.AsReadOnly([ProjectionSource]));
        }

        return ZocProjectionResult.NotQualified(
            Array.AsReadOnly(failures
                .Distinct()
                .OrderBy(value => value)
                .ToArray()),
            CanonicalSources(sources));
    }

    private static ZocTopologyFeatureDefinition Topology(
        string featureId,
        ZocTopologyFeatureKind kind,
        params RuleReference[] sources) => new(featureId, kind, sources);

    private static System.Collections.ObjectModel.ReadOnlyCollection<RuleReference> CanonicalSources(
        IEnumerable<RuleReference> sources) => Array.AsReadOnly(sources
            .Distinct()
            .OrderBy(source => source.SourceId, StringComparer.Ordinal)
            .ThenBy(source => source.Locator, StringComparer.Ordinal)
            .ToArray());
}
