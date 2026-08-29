namespace Cna.Core.Rules;

internal enum BreakdownWeatherShiftCondition
{
    Always,
    AtLeastHalfBreakdownPoints,
}

internal sealed record BreakdownBandDefinition(
    string BandId,
    int MinimumWholePoints,
    int? MaximumWholePoints,
    bool IsCheckEligible,
    IReadOnlyList<RuleReference> Sources);

internal sealed record BreakdownProfileDefinition(
    string ProfileId,
    int ColumnShift,
    IReadOnlyList<RuleReference> Sources);

internal sealed record BreakdownVehicleTypeDefinition(
    string VehicleTypeId,
    string ProfileId,
    IReadOnlyList<RuleReference> Sources);

internal sealed record BreakdownWeatherShiftDefinition(
    BreakdownWeatherKind WeatherKind,
    int ColumnShift,
    BreakdownWeatherShiftCondition Condition,
    IReadOnlyList<RuleReference> Sources);

internal sealed record BreakdownWeatherInputTransformationDefinition(
    BreakdownWeatherKind WeatherKind,
    string InputRouteId,
    string TreatedAsRouteId,
    IReadOnlyList<RuleReference> Sources);

internal sealed record BreakdownTerrainDefinition(
    string TerrainId,
    BreakdownPointAmount Points,
    IReadOnlyList<RuleReference> Sources);

internal sealed record BreakdownRouteDefinition(
    string RouteId,
    BreakdownInputOperation Operation,
    BreakdownPointAmount Amount,
    IReadOnlyList<RuleReference> Sources);

internal sealed record BreakdownHexsideDefinition(
    string HexsideId,
    BreakdownHexsideDirection Direction,
    BreakdownPointAmount AddedPoints,
    IReadOnlyList<RuleReference> Sources);

internal sealed record BreakdownDiceCoordinateDefinition(
    string Formation,
    IReadOnlyList<int> Coordinates,
    IReadOnlyList<RuleReference> Sources);

internal sealed record BreakdownRulesArtifactDefinition(
    int SchemaVersion,
    IReadOnlyList<BreakdownBandDefinition> Bands,
    IReadOnlyList<BreakdownProfileDefinition> Profiles,
    IReadOnlyList<BreakdownVehicleTypeDefinition> VehicleTypes,
    IReadOnlyList<BreakdownWeatherShiftDefinition> WeatherShifts,
    IReadOnlyList<BreakdownWeatherInputTransformationDefinition> WeatherInputTransformations,
    IReadOnlyList<BreakdownTerrainDefinition> Terrain,
    IReadOnlyList<BreakdownRouteDefinition> Routes,
    IReadOnlyList<BreakdownHexsideDefinition> Hexsides,
    BreakdownDiceCoordinateDefinition DiceCoordinate,
    IReadOnlyList<RuleReference> Sources);
