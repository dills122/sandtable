using System.Security.Cryptography;

namespace Cna.Core.Rules;

public static class Cna1979Movement
{
    public const int SchemaVersion = 1;
    public const string ArtifactId = "cna-1979.1.movement-tables";
    public const string NonMotorizedMobilityId = "land.mobility.non-motorized";
    public const string MotorizedMobilityId = "land.mobility.motorized";

    private static readonly RuleReference MapChartSource = new("spi-1979-map-a", "8.37");
    private static readonly RuleReference TrackErrataSource = new("spi-1979-errata", "8.37");
    private static readonly RuleReference StackingSource = new(
        "spi-1979-common-charts",
        "9.4.stacking-point-values");

    private static readonly IReadOnlyList<MovementMobilityDefinition> MobilityAuthority =
        Array.AsReadOnly<MovementMobilityDefinition>(
        [
            new(
                NonMotorizedMobilityId,
                MovementMobilityClass.NonMotorized,
                Sources(MapChartSource)),
            new(
                MotorizedMobilityId,
                MovementMobilityClass.Motorized,
                Sources(MapChartSource)),
        ]);

    private static readonly IReadOnlyList<MovementTerrainDefinition> TerrainAuthority =
        Array.AsReadOnly<MovementTerrainDefinition>(
        [
            Terrain("land.terrain.clear", NonMotorizedMobilityId, 2, 6),
            Terrain("land.terrain.clear", MotorizedMobilityId, 2, 6),
            Terrain("land.terrain.desert", NonMotorizedMobilityId, 3, 6),
            Terrain("land.terrain.desert", MotorizedMobilityId, 4, 6),
        ]);

    private static readonly IReadOnlyList<MovementRouteDefinition> RouteAuthority =
        Array.AsReadOnly<MovementRouteDefinition>(
        [
            Route(
                "land.edge.road",
                NonMotorizedMobilityId,
                MovementRouteCostKind.Override,
                new CapabilityPointAmount(1, 1),
                5,
                MapChartSource),
            Route(
                "land.edge.road",
                MotorizedMobilityId,
                MovementRouteCostKind.Override,
                new CapabilityPointAmount(1, 2),
                5,
                MapChartSource),
            Route(
                "land.edge.track",
                NonMotorizedMobilityId,
                MovementRouteCostKind.ScaleUnderlying,
                new CapabilityPointAmount(1, 2),
                5,
                MapChartSource,
                TrackErrataSource),
            Route(
                "land.edge.track",
                MotorizedMobilityId,
                MovementRouteCostKind.ScaleUnderlying,
                new CapabilityPointAmount(1, 2),
                5,
                MapChartSource,
                TrackErrataSource),
        ]);

    private static readonly IReadOnlyList<MovementHexsideDefinition> HexsideAuthority =
        Array.AsReadOnly<MovementHexsideDefinition>(
        [
            Hexside("land.edge.ridge", MovementHexsideDirection.Either,
                NonMotorizedMobilityId, 2),
            Hexside("land.edge.ridge", MovementHexsideDirection.Either,
                MotorizedMobilityId, 4),
            Hexside("land.edge.slope", MovementHexsideDirection.Up,
                NonMotorizedMobilityId, 2),
            Hexside("land.edge.slope", MovementHexsideDirection.Up,
                MotorizedMobilityId, 4),
            Hexside("land.edge.slope", MovementHexsideDirection.Down,
                NonMotorizedMobilityId, 1),
            Hexside("land.edge.slope", MovementHexsideDirection.Down,
                MotorizedMobilityId, 2),
        ]);

    private static readonly IReadOnlyList<MovementStackingDefinition> StackingAuthority =
        Array.AsReadOnly<MovementStackingDefinition>(
        [
            new(
                "land.organization.battalion",
                1,
                Sources(StackingSource)),
        ]);

    public static IReadOnlyList<MovementMobilityDefinition> Mobility => MobilityAuthority;

    internal static MovementRulesArtifactDefinition Definition { get; } = new(
        SchemaVersion,
        MobilityAuthority,
        TerrainAuthority,
        RouteAuthority,
        HexsideAuthority,
        StackingAuthority,
        Array.AsReadOnly(
            MobilityAuthority.SelectMany(value => value.Sources)
                .Concat(TerrainAuthority.SelectMany(value => value.Sources))
                .Concat(RouteAuthority.SelectMany(value => value.Sources))
                .Concat(HexsideAuthority.SelectMany(value => value.Sources))
                .Concat(StackingAuthority.SelectMany(value => value.Sources))
                .Distinct()
                .OrderBy(value => value.SourceId, StringComparer.Ordinal)
                .ThenBy(value => value.Locator, StringComparer.Ordinal)
                .ToArray()));

    static Cna1979Movement()
    {
        MovementRulesArtifactCodec.Validate(Definition);
    }

    public static bool IsSupportedMobilityId(string? mobilityId) =>
        MobilityAuthority.Any(value => string.Equals(
            value.MobilityId,
            mobilityId,
            StringComparison.Ordinal));

    public static MovementRuleLookupResult<MovementTerrainRule> LookupTerrain(
        string? terrainId,
        string? mobilityId)
    {
        if (!IsSupportedMobilityId(mobilityId))
        {
            return MovementRuleLookupResult<MovementTerrainRule>.Unsupported(
                MovementRuleUnsupportedKind.Mobility);
        }

        var definition = TerrainAuthority.SingleOrDefault(value =>
            string.Equals(value.TerrainId, terrainId, StringComparison.Ordinal)
            && string.Equals(value.MobilityId, mobilityId, StringComparison.Ordinal));
        return definition is null
            ? MovementRuleLookupResult<MovementTerrainRule>.Unsupported(
                MovementRuleUnsupportedKind.Terrain)
            : MovementRuleLookupResult<MovementTerrainRule>.Supported(
                new MovementTerrainRule(
                    definition.Cost,
                    definition.StoppingStackingLimit),
                definition.Sources);
    }

    public static MovementRuleLookupResult<MovementRouteRule> LookupRoute(
        string? routeId,
        string? mobilityId)
    {
        if (!IsSupportedMobilityId(mobilityId))
        {
            return MovementRuleLookupResult<MovementRouteRule>.Unsupported(
                MovementRuleUnsupportedKind.Mobility);
        }

        var definition = RouteAuthority.SingleOrDefault(value =>
            string.Equals(value.RouteId, routeId, StringComparison.Ordinal)
            && string.Equals(value.MobilityId, mobilityId, StringComparison.Ordinal));
        return definition is null
            ? MovementRuleLookupResult<MovementRouteRule>.Unsupported(
                MovementRuleUnsupportedKind.Route)
            : MovementRuleLookupResult<MovementRouteRule>.Supported(
                new MovementRouteRule(
                    definition.CostKind,
                    definition.Amount,
                    definition.TraversalStackingLimit),
                definition.Sources);
    }

    public static MovementRuleLookupResult<MovementHexsideRule> LookupHexside(
        string? hexsideId,
        MovementHexsideDirection direction,
        string? mobilityId)
    {
        if (!IsSupportedMobilityId(mobilityId))
        {
            return MovementRuleLookupResult<MovementHexsideRule>.Unsupported(
                MovementRuleUnsupportedKind.Mobility);
        }

        var definition = HexsideAuthority.SingleOrDefault(value =>
            string.Equals(value.HexsideId, hexsideId, StringComparison.Ordinal)
            && value.Direction == direction
            && string.Equals(value.MobilityId, mobilityId, StringComparison.Ordinal));
        return definition is null
            ? MovementRuleLookupResult<MovementHexsideRule>.Unsupported(
                MovementRuleUnsupportedKind.Hexside)
            : MovementRuleLookupResult<MovementHexsideRule>.Supported(
                new MovementHexsideRule(definition.AddedCost),
                definition.Sources);
    }

    public static MovementRuleLookupResult<MovementStackingRule> LookupStackingValue(
        string? organizationId)
    {
        var definition = StackingAuthority.SingleOrDefault(value => string.Equals(
            value.OrganizationId,
            organizationId,
            StringComparison.Ordinal));
        return definition is null
            ? MovementRuleLookupResult<MovementStackingRule>.Unsupported(
                MovementRuleUnsupportedKind.Organization)
            : MovementRuleLookupResult<MovementStackingRule>.Supported(
                new MovementStackingRule(definition.StackingValue),
                definition.Sources);
    }

    public static RulesetArtifact CreateArtifact()
    {
        var canonical = MovementRulesArtifactCodec.SerializeCanonical(Definition);
        return new RulesetArtifact(
            ArtifactId,
            $"sha256:{Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant()}",
            Definition.Sources);
    }

    private static MovementTerrainDefinition Terrain(
        string terrainId,
        string mobilityId,
        long cost,
        int stoppingStackingLimit) => new(
            terrainId,
            mobilityId,
            new CapabilityPointAmount(cost, 1),
            stoppingStackingLimit,
            Sources(MapChartSource));

    private static MovementRouteDefinition Route(
        string routeId,
        string mobilityId,
        MovementRouteCostKind costKind,
        CapabilityPointAmount amount,
        int traversalStackingLimit,
        params RuleReference[] sources) => new(
            routeId,
            mobilityId,
            costKind,
            amount,
            traversalStackingLimit,
            Sources(sources));

    private static MovementHexsideDefinition Hexside(
        string hexsideId,
        MovementHexsideDirection direction,
        string mobilityId,
        long cost) => new(
            hexsideId,
            direction,
            mobilityId,
            new CapabilityPointAmount(cost, 1),
            Sources(MapChartSource));

    private static System.Collections.ObjectModel.ReadOnlyCollection<RuleReference> Sources(
        params RuleReference[] sources) => Array.AsReadOnly(sources);
}
