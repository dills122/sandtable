namespace Cna.Core.Rules;

internal sealed record MovementTerrainDefinition(
    string TerrainId,
    string MobilityId,
    CapabilityPointAmount Cost,
    int StoppingStackingLimit,
    IReadOnlyList<RuleReference> Sources);

internal sealed record MovementRouteDefinition(
    string RouteId,
    string MobilityId,
    MovementRouteCostKind CostKind,
    CapabilityPointAmount Amount,
    int TraversalStackingLimit,
    IReadOnlyList<RuleReference> Sources);

internal sealed record MovementHexsideDefinition(
    string HexsideId,
    MovementHexsideDirection Direction,
    string MobilityId,
    CapabilityPointAmount AddedCost,
    IReadOnlyList<RuleReference> Sources);

internal sealed record MovementStackingDefinition(
    string OrganizationId,
    int StackingValue,
    IReadOnlyList<RuleReference> Sources);

internal sealed record MovementRulesArtifactDefinition(
    int SchemaVersion,
    IReadOnlyList<MovementMobilityDefinition> Mobility,
    IReadOnlyList<MovementTerrainDefinition> Terrain,
    IReadOnlyList<MovementRouteDefinition> Routes,
    IReadOnlyList<MovementHexsideDefinition> Hexsides,
    IReadOnlyList<MovementStackingDefinition> Stacking,
    IReadOnlyList<RuleReference> Sources);
