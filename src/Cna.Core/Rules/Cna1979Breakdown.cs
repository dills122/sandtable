using System.Numerics;
using System.Security.Cryptography;

namespace Cna.Core.Rules;

public static class Cna1979Breakdown
{
    public const int SchemaVersion = 1;
    public const string ArtifactId = "cna-1979.1.breakdown-tables";
    public const string ProfileTruckId = "land.breakdown.profile.truck";
    public const string VehicleTypeTruckId = "land.breakdown.vehicle-type.truck";
    public const string SequentialDiceRulingId =
        "cna-1979.1.ruling.breakdown-sequential-dice";
    public const string SandstormBasisRulingId =
        "cna-1979.1.ruling.breakdown-sandstorm-basis";

    private static readonly RuleReference LandVehicleSource =
        new("spi-1979-land-rules", "21.11-21.14");
    private static readonly RuleReference LandBandSource =
        new("spi-1979-land-rules", "21.31");
    private static readonly RuleReference ChartSource =
        new("spi-1979-common-charts", "21.38");
    private static readonly RuleReference MapSource =
        new("spi-1979-map-a", "8.37");
    private static readonly RuleReference TrackErrataSource =
        new("spi-1979-errata", "8.37");
    private static readonly RuleReference NormalWeatherSource =
        new("spi-1979-land-rules", "21.37a");
    private static readonly RuleReference HotWeatherSource =
        new("spi-1979-land-rules", "21.37b");
    private static readonly RuleReference RainstormSource =
        new("spi-1979-land-rules", "21.37c");
    private static readonly RuleReference SandstormSource =
        new("spi-1979-land-rules", "21.37d");
    private static readonly RuleReference DiceRuleSource =
        new("spi-1979-land-rules", "21.34");

    private static readonly System.Collections.ObjectModel.ReadOnlyCollection<
        BreakdownBandDefinition> BandAuthority =
        Array.AsReadOnly<BreakdownBandDefinition>(
        [
            Band("land.breakdown.band.0-3", 0, 3, false),
            Band("land.breakdown.band.4-10", 4, 10, true),
            Band("land.breakdown.band.11-20", 11, 20, true),
            Band("land.breakdown.band.21-30", 21, 30, true),
            Band("land.breakdown.band.31-40", 31, 40, true),
            Band("land.breakdown.band.41-50", 41, 50, true),
            Band("land.breakdown.band.51-60", 51, 60, true),
            Band("land.breakdown.band.61-70", 61, 70, true),
            Band("land.breakdown.band.71-plus", 71, null, true),
        ]);

    private static readonly IReadOnlyList<BreakdownProfileDefinition> ProfileAuthority =
        Array.AsReadOnly<BreakdownProfileDefinition>(
        [
            new(
                ProfileTruckId,
                -2,
                Sources(LandVehicleSource, ChartSource)),
        ]);

    private static readonly IReadOnlyList<BreakdownVehicleTypeDefinition> VehicleTypeAuthority =
        Array.AsReadOnly<BreakdownVehicleTypeDefinition>(
        [
            new(VehicleTypeTruckId, ProfileTruckId, Sources(LandVehicleSource)),
        ]);

    private static readonly IReadOnlyList<BreakdownWeatherShiftDefinition> WeatherShiftAuthority =
        Array.AsReadOnly<BreakdownWeatherShiftDefinition>(
        [
            new(BreakdownWeatherKind.Normal, 0,
                BreakdownWeatherShiftCondition.Always, Sources(NormalWeatherSource)),
            new(BreakdownWeatherKind.Hot, 1,
                BreakdownWeatherShiftCondition.Always, Sources(HotWeatherSource)),
            new(BreakdownWeatherKind.Sandstorm, 1,
                BreakdownWeatherShiftCondition.AtLeastHalfBreakdownPoints,
                Sources(ChartSource, SandstormSource)),
        ]);

    private static readonly IReadOnlyList<BreakdownWeatherInputTransformationDefinition>
        WeatherInputTransformationAuthority =
            Array.AsReadOnly<BreakdownWeatherInputTransformationDefinition>(
            [
                new(
                    BreakdownWeatherKind.Rainstorm,
                    "land.edge.road",
                    "land.edge.track",
                    Sources(RainstormSource)),
            ]);

    private static readonly IReadOnlyList<BreakdownTerrainDefinition> TerrainAuthority =
        Array.AsReadOnly<BreakdownTerrainDefinition>(
        [
            new("land.terrain.clear", new BreakdownPointAmount(4, 1), Sources(MapSource)),
            new("land.terrain.desert", new BreakdownPointAmount(24, 1), Sources(MapSource)),
        ]);

    private static readonly IReadOnlyList<BreakdownRouteDefinition> RouteAuthority =
        Array.AsReadOnly<BreakdownRouteDefinition>(
        [
            new(
                "land.edge.road",
                BreakdownInputOperation.Override,
                new BreakdownPointAmount(1, 2),
                Sources(MapSource)),
            new(
                "land.edge.track",
                BreakdownInputOperation.ScaleUnderlying,
                new BreakdownPointAmount(1, 2),
                Sources(MapSource, TrackErrataSource)),
        ]);

    private static readonly IReadOnlyList<BreakdownHexsideDefinition> HexsideAuthority =
        Array.AsReadOnly<BreakdownHexsideDefinition>(
        [
            new("land.edge.ridge", BreakdownHexsideDirection.Either,
                new BreakdownPointAmount(2, 1), Sources(MapSource)),
            new("land.edge.slope", BreakdownHexsideDirection.Up,
                new BreakdownPointAmount(2, 1), Sources(MapSource)),
            new("land.edge.slope", BreakdownHexsideDirection.Down,
                new BreakdownPointAmount(2, 1), Sources(MapSource)),
        ]);

    private static readonly BreakdownDiceCoordinateDefinition DiceCoordinateAuthority = new(
        "sequential-d6",
        Array.AsReadOnly(
            Enumerable.Range(1, 6)
                .SelectMany(first => Enumerable.Range(1, 6)
                    .Select(second => (first * 10) + second))
                .ToArray()),
        Sources(ChartSource, DiceRuleSource));

    internal static BreakdownRulesArtifactDefinition Definition { get; } = new(
        SchemaVersion,
        BandAuthority,
        ProfileAuthority,
        VehicleTypeAuthority,
        WeatherShiftAuthority,
        WeatherInputTransformationAuthority,
        TerrainAuthority,
        RouteAuthority,
        HexsideAuthority,
        DiceCoordinateAuthority,
        Array.AsReadOnly(
            BandAuthority.SelectMany(value => value.Sources)
                .Concat(ProfileAuthority.SelectMany(value => value.Sources))
                .Concat(VehicleTypeAuthority.SelectMany(value => value.Sources))
                .Concat(WeatherShiftAuthority.SelectMany(value => value.Sources))
                .Concat(WeatherInputTransformationAuthority.SelectMany(value => value.Sources))
                .Concat(TerrainAuthority.SelectMany(value => value.Sources))
                .Concat(RouteAuthority.SelectMany(value => value.Sources))
                .Concat(HexsideAuthority.SelectMany(value => value.Sources))
                .Concat(DiceCoordinateAuthority.Sources)
                .Distinct()
                .OrderBy(value => value.SourceId, StringComparer.Ordinal)
                .ThenBy(value => value.Locator, StringComparer.Ordinal)
                .ToArray()));

    static Cna1979Breakdown()
    {
        BreakdownRulesArtifactCodec.Validate(Definition);
    }

    public static bool IsSupportedBandId(string? bandId) =>
        BandAuthority.Any(value => string.Equals(value.BandId, bandId, StringComparison.Ordinal));

    public static bool IsCheckEligibleBandId(string? bandId) =>
        BandAuthority.Any(value =>
            value.IsCheckEligible
            && string.Equals(value.BandId, bandId, StringComparison.Ordinal));

    public static bool IsSupportedProfileId(string? profileId) =>
        ProfileAuthority.Any(value => string.Equals(
            value.ProfileId,
            profileId,
            StringComparison.Ordinal));

    public static bool IsSupportedVehicleTypeId(string? vehicleTypeId) =>
        VehicleTypeAuthority.Any(value => string.Equals(
            value.VehicleTypeId,
            vehicleTypeId,
            StringComparison.Ordinal));

    public static bool IsSupportedVehicleProfile(string? vehicleTypeId, string? profileId) =>
        VehicleTypeAuthority.Any(value =>
            string.Equals(value.VehicleTypeId, vehicleTypeId, StringComparison.Ordinal)
            && string.Equals(value.ProfileId, profileId, StringComparison.Ordinal));

    public static BreakdownRuleLookupResult<BreakdownProfileRule> LookupProfile(
        string? profileId)
    {
        var definition = ProfileAuthority.SingleOrDefault(value => string.Equals(
            value.ProfileId,
            profileId,
            StringComparison.Ordinal));
        return definition is null
            ? BreakdownRuleLookupResult<BreakdownProfileRule>.Unsupported(
                BreakdownRuleUnsupportedKind.Profile)
            : BreakdownRuleLookupResult<BreakdownProfileRule>.Supported(
                new BreakdownProfileRule(definition.ProfileId, definition.ColumnShift),
                definition.Sources);
    }

    public static BreakdownRuleLookupResult<BreakdownVehicleTypeRule> LookupVehicleType(
        string? vehicleTypeId)
    {
        var definition = VehicleTypeAuthority.SingleOrDefault(value => string.Equals(
            value.VehicleTypeId,
            vehicleTypeId,
            StringComparison.Ordinal));
        return definition is null
            ? BreakdownRuleLookupResult<BreakdownVehicleTypeRule>.Unsupported(
                BreakdownRuleUnsupportedKind.VehicleType)
            : BreakdownRuleLookupResult<BreakdownVehicleTypeRule>.Supported(
                new BreakdownVehicleTypeRule(definition.VehicleTypeId, definition.ProfileId),
                definition.Sources);
    }

    public static BreakdownBandRule LookupAccumulatedBand(BreakdownPointAmount points)
    {
        ArgumentNullException.ThrowIfNull(points);
        var wholePoints = points.CeilingToWhole();
        var definition = BandAuthority.Single(value =>
            wholePoints >= value.MinimumWholePoints
            && (value.MaximumWholePoints is null || wholePoints <= value.MaximumWholePoints));
        return ToRule(definition);
    }

    public static int GetWeatherColumnShift(
        BreakdownWeatherKind weatherKind,
        BreakdownPointAmount totalPoints,
        BreakdownPointAmount sandstormAttributedPoints)
    {
        ValidatePointShare(totalPoints, sandstormAttributedPoints);
        var definition = WeatherShiftAuthority.SingleOrDefault(value =>
            value.WeatherKind == weatherKind);
        if (definition is null)
        {
            if (weatherKind == BreakdownWeatherKind.Rainstorm)
            {
                throw new InvalidOperationException(
                    "Rainstorm transforms Road input into Track input; it is not a column shift.");
            }

            throw new ArgumentOutOfRangeException(nameof(weatherKind), weatherKind, null);
        }

        return definition.Condition == BreakdownWeatherShiftCondition.AtLeastHalfBreakdownPoints
            && !IsAtLeastHalf(sandstormAttributedPoints, totalPoints)
                ? 0
                : definition.ColumnShift;
    }

    public static BreakdownBandRule? SelectEffectiveCheckBand(
        BreakdownPointAmount totalPoints,
        string? profileId,
        BreakdownWeatherKind weatherKind,
        BreakdownPointAmount sandstormAttributedPoints)
    {
        var profile = LookupProfile(profileId);
        if (!profile.IsSupported)
        {
            throw new ArgumentException("Unsupported Breakdown profile.", nameof(profileId));
        }

        var rawBand = LookupAccumulatedBand(totalPoints);
        return ApplyColumnShift(
            rawBand,
            profile.Value.ColumnShift
                + GetWeatherColumnShift(weatherKind, totalPoints, sandstormAttributedPoints));
    }

    public static BreakdownBandRule? ApplyColumnShift(
        BreakdownBandRule band,
        int columnShift)
    {
        ArgumentNullException.ThrowIfNull(band);
        var rawIndex = BandAuthority
            .Select((value, index) => (value.BandId, Index: index))
            .SingleOrDefault(value => value.BandId == band.BandId).Index;
        if (!IsSupportedBandId(band.BandId))
        {
            throw new ArgumentException("Unsupported Breakdown band.", nameof(band));
        }

        var shiftedIndex = (long)rawIndex + columnShift;
        if (shiftedIndex < 1)
        {
            return null;
        }

        return ToRule(BandAuthority[(int)Math.Min(shiftedIndex, BandAuthority.Count - 1)]);
    }

    public static BreakdownRuleLookupResult<BreakdownWeatherInputTransformationRule>
        LookupWeatherInputTransformation(BreakdownWeatherKind weatherKind)
    {
        var definition = WeatherInputTransformationAuthority.SingleOrDefault(value =>
            value.WeatherKind == weatherKind);
        return definition is null
            ? BreakdownRuleLookupResult<BreakdownWeatherInputTransformationRule>.Unsupported(
                BreakdownRuleUnsupportedKind.WeatherInputTransformation)
            : BreakdownRuleLookupResult<BreakdownWeatherInputTransformationRule>.Supported(
                new BreakdownWeatherInputTransformationRule(
                    definition.InputRouteId,
                    definition.TreatedAsRouteId),
                definition.Sources);
    }

    public static BreakdownRuleLookupResult<BreakdownTerrainRule> LookupTerrain(
        string? terrainId)
    {
        var definition = TerrainAuthority.SingleOrDefault(value => string.Equals(
            value.TerrainId,
            terrainId,
            StringComparison.Ordinal));
        return definition is null
            ? BreakdownRuleLookupResult<BreakdownTerrainRule>.Unsupported(
                BreakdownRuleUnsupportedKind.Terrain)
            : BreakdownRuleLookupResult<BreakdownTerrainRule>.Supported(
                new BreakdownTerrainRule(definition.Points),
                definition.Sources);
    }

    public static BreakdownRuleLookupResult<BreakdownRouteRule> LookupRoute(string? routeId)
    {
        var definition = RouteAuthority.SingleOrDefault(value => string.Equals(
            value.RouteId,
            routeId,
            StringComparison.Ordinal));
        return definition is null
            ? BreakdownRuleLookupResult<BreakdownRouteRule>.Unsupported(
                BreakdownRuleUnsupportedKind.Route)
            : BreakdownRuleLookupResult<BreakdownRouteRule>.Supported(
                new BreakdownRouteRule(definition.Operation, definition.Amount),
                definition.Sources);
    }

    public static BreakdownRuleLookupResult<BreakdownHexsideRule> LookupHexside(
        string? hexsideId,
        BreakdownHexsideDirection direction)
    {
        var definition = HexsideAuthority.SingleOrDefault(value =>
            string.Equals(value.HexsideId, hexsideId, StringComparison.Ordinal)
            && value.Direction == direction);
        return definition is null
            ? BreakdownRuleLookupResult<BreakdownHexsideRule>.Unsupported(
                BreakdownRuleUnsupportedKind.Hexside)
            : BreakdownRuleLookupResult<BreakdownHexsideRule>.Supported(
                new BreakdownHexsideRule(definition.AddedPoints),
                definition.Sources);
    }

    public static int CreateSequentialDiceCoordinate(int firstDie, int secondDie)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(firstDie, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(firstDie, 6);
        ArgumentOutOfRangeException.ThrowIfLessThan(secondDie, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(secondDie, 6);
        return (firstDie * 10) + secondDie;
    }

    public static RulesetArtifact CreateArtifact()
    {
        var canonical = BreakdownRulesArtifactCodec.SerializeCanonical(Definition);
        return new RulesetArtifact(
            ArtifactId,
            $"sha256:{Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant()}",
            Definition.Sources);
    }

    public static Ruling CreateSequentialDiceRuling() => new(
        SequentialDiceRulingId,
        "cna-1979.1.conflict.breakdown-dice-coordinate",
        ["add-two-d6", "form-sequential-d6-coordinate"],
        "form-sequential-d6-coordinate",
        ["MOV-AC-014"],
        [ChartSource, DiceRuleSource]);

    public static Ruling CreateSandstormBasisRuling() => new(
        SandstormBasisRulingId,
        "cna-1979.1.conflict.breakdown-sandstorm-threshold-basis",
        ["use-breakdown-point-share", "use-capability-point-share"],
        "use-breakdown-point-share",
        ["MOV-AC-014"],
        [ChartSource, SandstormSource]);

    private static BreakdownBandDefinition Band(
        string bandId,
        int minimumWholePoints,
        int? maximumWholePoints,
        bool isCheckEligible) => new(
            bandId,
            minimumWholePoints,
            maximumWholePoints,
            isCheckEligible,
            Sources(ChartSource, LandBandSource));

    private static BreakdownBandRule ToRule(BreakdownBandDefinition definition) => new(
        definition.BandId,
        definition.MinimumWholePoints,
        definition.MaximumWholePoints,
        definition.IsCheckEligible);

    private static void ValidatePointShare(
        BreakdownPointAmount totalPoints,
        BreakdownPointAmount sandstormAttributedPoints)
    {
        ArgumentNullException.ThrowIfNull(totalPoints);
        ArgumentNullException.ThrowIfNull(sandstormAttributedPoints);
        if (sandstormAttributedPoints > totalPoints)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sandstormAttributedPoints),
                "Sandstorm-attributed Breakdown Points cannot exceed total Breakdown Points.");
        }
    }

    private static bool IsAtLeastHalf(
        BreakdownPointAmount attributed,
        BreakdownPointAmount total) =>
        total != BreakdownPointAmount.Zero
        && ((BigInteger)attributed.Numerator * 2 * total.Denominator)
            >= ((BigInteger)total.Numerator * attributed.Denominator);

    private static System.Collections.ObjectModel.ReadOnlyCollection<RuleReference> Sources(
        params RuleReference[] sources) => Array.AsReadOnly(sources);
}
