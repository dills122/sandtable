using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class BreakdownRulesTests
{
    [Fact]
    public void VocabularyAndCompatibilityLookupsAreClosedAndStable()
    {
        Assert.Equal("cna-1979.1.breakdown-tables", Cna1979Breakdown.ArtifactId);
        Assert.Equal("land.breakdown.profile.truck", Cna1979Breakdown.ProfileTruckId);
        Assert.Equal("land.breakdown.vehicle-type.truck", Cna1979Breakdown.VehicleTypeTruckId);

        Assert.True(Cna1979Breakdown.IsSupportedProfileId(Cna1979Breakdown.ProfileTruckId));
        Assert.False(Cna1979Breakdown.IsSupportedProfileId(null));
        Assert.False(Cna1979Breakdown.IsSupportedProfileId("land.breakdown.profile.unknown"));
        Assert.True(Cna1979Breakdown.IsSupportedVehicleTypeId(
            Cna1979Breakdown.VehicleTypeTruckId));
        Assert.False(Cna1979Breakdown.IsSupportedVehicleTypeId(null));
        Assert.False(Cna1979Breakdown.IsSupportedVehicleTypeId(
            "land.breakdown.vehicle-type.unknown"));
        Assert.True(Cna1979Breakdown.IsSupportedVehicleProfile(
            Cna1979Breakdown.VehicleTypeTruckId,
            Cna1979Breakdown.ProfileTruckId));
        Assert.False(Cna1979Breakdown.IsSupportedVehicleProfile(
            Cna1979Breakdown.VehicleTypeTruckId,
            "land.breakdown.profile.unknown"));

        var profile = Cna1979Breakdown.LookupProfile(Cna1979Breakdown.ProfileTruckId);
        Assert.True(profile.IsSupported);
        Assert.Equal(-2, profile.Value.ColumnShift);
        Assert.Contains(profile.Sources, source =>
            source.SourceId == "spi-1979-land-rules" && source.Locator == "21.11-21.14");
        Assert.DoesNotContain(profile.Sources, source =>
            source.SourceId == "spi-1979-errata" && source.Locator == "21.12");

        var vehicleType = Cna1979Breakdown.LookupVehicleType(
            Cna1979Breakdown.VehicleTypeTruckId);
        Assert.True(vehicleType.IsSupported);
        Assert.Equal(Cna1979Breakdown.ProfileTruckId, vehicleType.Value.ProfileId);
        Assert.False(Cna1979Breakdown.LookupProfile("unknown").IsSupported);
        Assert.False(Cna1979Breakdown.LookupVehicleType("unknown").IsSupported);
    }

    [Theory]
    [InlineData(0, 1, "land.breakdown.band.0-3", false)]
    [InlineData(3, 1, "land.breakdown.band.0-3", false)]
    [InlineData(7, 2, "land.breakdown.band.4-10", true)]
    [InlineData(10, 1, "land.breakdown.band.4-10", true)]
    [InlineData(11, 1, "land.breakdown.band.11-20", true)]
    [InlineData(20, 1, "land.breakdown.band.11-20", true)]
    [InlineData(41, 2, "land.breakdown.band.21-30", true)]
    [InlineData(30, 1, "land.breakdown.band.21-30", true)]
    [InlineData(31, 1, "land.breakdown.band.31-40", true)]
    [InlineData(41, 1, "land.breakdown.band.41-50", true)]
    [InlineData(51, 1, "land.breakdown.band.51-60", true)]
    [InlineData(61, 1, "land.breakdown.band.61-70", true)]
    [InlineData(71, 1, "land.breakdown.band.71-plus", true)]
    [InlineData(900, 1, "land.breakdown.band.71-plus", true)]
    public void ExactCeilingSelectsThePublishedAccumulatedBand(
        long numerator,
        int denominator,
        string expectedBandId,
        bool expectedEligibility)
    {
        var band = Cna1979Breakdown.LookupAccumulatedBand(
            new BreakdownPointAmount(numerator, denominator));

        Assert.Equal(expectedBandId, band.BandId);
        Assert.Equal(expectedEligibility, band.IsCheckEligible);
        Assert.True(Cna1979Breakdown.IsSupportedBandId(band.BandId));
    }

    [Theory]
    [InlineData(BreakdownWeatherKind.Normal, 0, false)]
    [InlineData(BreakdownWeatherKind.Hot, 1, false)]
    [InlineData(BreakdownWeatherKind.Sandstorm, 0, false)]
    [InlineData(BreakdownWeatherKind.Sandstorm, 1, true)]
    public void WeatherColumnShiftsUseExactSandstormBreakdownPointShare(
        BreakdownWeatherKind weather,
        int expectedShift,
        bool halfOrMore)
    {
        var total = new BreakdownPointAmount(7, 2);
        var sandstorm = halfOrMore
            ? new BreakdownPointAmount(7, 4)
            : new BreakdownPointAmount(3, 2);

        Assert.Equal(expectedShift,
            Cna1979Breakdown.GetWeatherColumnShift(weather, total, sandstorm));
    }

    [Fact]
    public void WeatherShiftInputsFailClosedAndRainstormIsNotAColumnShift()
    {
        Assert.Equal(0, Cna1979Breakdown.GetWeatherColumnShift(
            BreakdownWeatherKind.Sandstorm,
            BreakdownPointAmount.Zero,
            BreakdownPointAmount.Zero));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Cna1979Breakdown.GetWeatherColumnShift(
                BreakdownWeatherKind.Sandstorm,
                new BreakdownPointAmount(1, 1),
                new BreakdownPointAmount(3, 2)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Cna1979Breakdown.GetWeatherColumnShift(
                (BreakdownWeatherKind)999,
                BreakdownPointAmount.Zero,
                BreakdownPointAmount.Zero));
        Assert.Throws<InvalidOperationException>(() =>
            Cna1979Breakdown.GetWeatherColumnShift(
                BreakdownWeatherKind.Rainstorm,
                BreakdownPointAmount.Zero,
                BreakdownPointAmount.Zero));

        var transformation = Cna1979Breakdown.LookupWeatherInputTransformation(
            BreakdownWeatherKind.Rainstorm);
        Assert.True(transformation.IsSupported);
        Assert.Equal("land.edge.road", transformation.Value.InputRouteId);
        Assert.Equal("land.edge.track", transformation.Value.TreatedAsRouteId);
        Assert.False(Cna1979Breakdown.LookupWeatherInputTransformation(
            BreakdownWeatherKind.Normal).IsSupported);
    }

    [Fact]
    public void SignedShiftsClampBelowEligibilityAndAtTheHighestBand()
    {
        Assert.Null(Cna1979Breakdown.SelectEffectiveCheckBand(
            new BreakdownPointAmount(4, 1),
            Cna1979Breakdown.ProfileTruckId,
            BreakdownWeatherKind.Normal,
            BreakdownPointAmount.Zero));

        var firstEligible = Cna1979Breakdown.SelectEffectiveCheckBand(
            new BreakdownPointAmount(21, 1),
            Cna1979Breakdown.ProfileTruckId,
            BreakdownWeatherKind.Normal,
            BreakdownPointAmount.Zero);
        Assert.Equal("land.breakdown.band.4-10", firstEligible?.BandId);

        var capped = Cna1979Breakdown.SelectEffectiveCheckBand(
            new BreakdownPointAmount(900, 1),
            Cna1979Breakdown.ProfileTruckId,
            BreakdownWeatherKind.Hot,
            BreakdownPointAmount.Zero);
        Assert.Equal("land.breakdown.band.61-70", capped?.BandId);

        var highest = Cna1979Breakdown.LookupAccumulatedBand(
            new BreakdownPointAmount(71, 1));
        Assert.Equal("land.breakdown.band.71-plus",
            Cna1979Breakdown.ApplyColumnShift(highest, 100)?.BandId);
    }

    [Theory]
    [InlineData("land.terrain.clear", 4, 1)]
    [InlineData("land.terrain.desert", 24, 1)]
    public void TerrainInputsReturnExactSourceBackedBreakdownPoints(
        string terrainId,
        long numerator,
        int denominator)
    {
        var lookup = Cna1979Breakdown.LookupTerrain(terrainId);

        Assert.True(lookup.IsSupported);
        Assert.Equal(new BreakdownPointAmount(numerator, denominator), lookup.Value.Points);
        Assert.Contains(lookup.Sources, source =>
            source.SourceId == "spi-1979-map-a" && source.Locator == "8.37");
    }

    [Theory]
    [InlineData("land.edge.road", BreakdownInputOperation.Override, 1, 2)]
    [InlineData("land.edge.track", BreakdownInputOperation.ScaleUnderlying, 1, 2)]
    public void RouteInputsRetainExactOverrideAndScalingSemantics(
        string routeId,
        BreakdownInputOperation operation,
        long numerator,
        int denominator)
    {
        var lookup = Cna1979Breakdown.LookupRoute(routeId);

        Assert.True(lookup.IsSupported);
        Assert.Equal(operation, lookup.Value.Operation);
        Assert.Equal(new BreakdownPointAmount(numerator, denominator), lookup.Value.Amount);
        if (routeId == "land.edge.track")
        {
            Assert.Contains(lookup.Sources, source =>
                source.SourceId == "spi-1979-errata" && source.Locator == "8.37");
        }
    }

    [Theory]
    [InlineData("land.edge.ridge", BreakdownHexsideDirection.Either)]
    [InlineData("land.edge.slope", BreakdownHexsideDirection.Up)]
    [InlineData("land.edge.slope", BreakdownHexsideDirection.Down)]
    public void HexsideInputsAddTwoExactBreakdownPoints(
        string hexsideId,
        BreakdownHexsideDirection direction)
    {
        var lookup = Cna1979Breakdown.LookupHexside(hexsideId, direction);

        Assert.True(lookup.IsSupported);
        Assert.Equal(new BreakdownPointAmount(2, 1), lookup.Value.AddedPoints);
    }

    [Fact]
    public void UnknownInputsReturnTypedUnsupportedResults()
    {
        Assert.Equal(BreakdownRuleUnsupportedKind.Terrain,
            Cna1979Breakdown.LookupTerrain("land.terrain.unknown").UnsupportedKind);
        Assert.Equal(BreakdownRuleUnsupportedKind.Route,
            Cna1979Breakdown.LookupRoute("land.edge.unknown").UnsupportedKind);
        Assert.Equal(BreakdownRuleUnsupportedKind.Hexside,
            Cna1979Breakdown.LookupHexside(
                "land.edge.slope", BreakdownHexsideDirection.Either).UnsupportedKind);
    }

    [Fact]
    public void SequentialDiceFormAValidatedCoordinateWithoutOwningRandomness()
    {
        Assert.Equal(11, Cna1979Breakdown.CreateSequentialDiceCoordinate(1, 1));
        Assert.Equal(33, Cna1979Breakdown.CreateSequentialDiceCoordinate(3, 3));
        Assert.Equal(66, Cna1979Breakdown.CreateSequentialDiceCoordinate(6, 6));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Cna1979Breakdown.CreateSequentialDiceCoordinate(0, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Cna1979Breakdown.CreateSequentialDiceCoordinate(3, 7));

        var domain = Enumerable.Range(1, 6)
            .SelectMany(first => Enumerable.Range(1, 6)
                .Select(second => Cna1979Breakdown.CreateSequentialDiceCoordinate(first, second)))
            .ToArray();
        Assert.Equal(36, domain.Distinct().Count());
        Assert.All(domain, coordinate =>
        {
            Assert.InRange(coordinate / 10, 1, 6);
            Assert.InRange(coordinate % 10, 1, 6);
        });
    }

    [Fact]
    public void ManifestReadyRulingsRetainBothApprovedConflicts()
    {
        var dice = Cna1979Breakdown.CreateSequentialDiceRuling();
        Assert.Equal(Cna1979Breakdown.SequentialDiceRulingId, dice.RulingId);
        Assert.Equal("form-sequential-d6-coordinate", dice.SelectedBehaviorId);
        Assert.Contains(dice.Sources, source =>
            source.SourceId == "spi-1979-common-charts" && source.Locator == "21.38");

        var sandstorm = Cna1979Breakdown.CreateSandstormBasisRuling();
        Assert.Equal(Cna1979Breakdown.SandstormBasisRulingId, sandstorm.RulingId);
        Assert.Equal("use-breakdown-point-share", sandstorm.SelectedBehaviorId);
        Assert.Contains(sandstorm.Sources, source =>
            source.SourceId == "spi-1979-land-rules" && source.Locator == "21.37d");
    }

    [Fact]
    public void CanonicalArtifactHasStrictGoldenBytesRoundtripAndMutationSensitiveHash()
    {
        var canonical = BreakdownRulesArtifactCodec.SerializeCanonical(
            Cna1979Breakdown.Definition);
        var goldenFile = File.ReadAllBytes(Path.Combine(
            AppContext.BaseDirectory,
            "Rules",
            "Fixtures",
            "cna-1979.1.breakdown-tables.v1.golden.json"));
        var golden = goldenFile.AsSpan(0, goldenFile.Length - 1).ToArray();
        var parsed = BreakdownRulesArtifactCodec.Deserialize(canonical);

        Assert.Equal((byte)'\n', goldenFile[^1]);
        Assert.Equal(golden, canonical);
        Assert.Equal(canonical, BreakdownRulesArtifactCodec.SerializeCanonical(parsed));
        Assert.Throws<JsonException>(() => BreakdownRulesArtifactCodec.Deserialize(goldenFile));

        var artifact = Cna1979Breakdown.CreateArtifact();
        Assert.Equal(Cna1979Breakdown.ArtifactId, artifact.ArtifactId);
        Assert.Equal(
            $"sha256:{Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant()}",
            artifact.ContentHash);
        Assert.Equal(
            "sha256:c7061325838dfcdd2f2388be3c6f6ec998bfa96df14b4cd6e733dd1c5d16c747",
            artifact.ContentHash);
        Assert.Equal(Cna1979Breakdown.Definition.Sources, artifact.Sources);

        var json = Encoding.UTF8.GetString(canonical);
        var changedBand = json.Replace(
            "\"minimumWholePoints\":4",
            "\"minimumWholePoints\":5",
            StringComparison.Ordinal);
        var changedShift = json.Replace(
            "\"columnShift\":-2",
            "\"columnShift\":-1",
            StringComparison.Ordinal);
        var changedSource = json.Replace(
            "\"locator\":\"21.38\"",
            "\"locator\":\"21.39\"",
            StringComparison.Ordinal);
        var changedInteriorCoordinate = json.Replace(
            "31,32,33,34,35,36",
            "31,32,37,34,35,36",
            StringComparison.Ordinal);

        Assert.Contains(
            "\"coordinates\":[11,12,13,14,15,16,21,22,23,24,25,26," +
            "31,32,33,34,35,36,41,42,43,44,45,46,51,52,53,54,55,56," +
            "61,62,63,64,65,66]",
            json,
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => BreakdownRulesArtifactCodec.Deserialize(
            Encoding.UTF8.GetBytes(changedBand)));
        Assert.Throws<JsonException>(() => BreakdownRulesArtifactCodec.Deserialize(
            Encoding.UTF8.GetBytes(changedShift)));
        Assert.Throws<JsonException>(() => BreakdownRulesArtifactCodec.Deserialize(
            Encoding.UTF8.GetBytes(changedSource)));
        Assert.Throws<JsonException>(() => BreakdownRulesArtifactCodec.Deserialize(
            Encoding.UTF8.GetBytes(changedInteriorCoordinate)));
        Assert.DoesNotContain("\"locator\":\"21.35\"", json, StringComparison.Ordinal);
        Assert.False(SHA256.HashData(canonical).SequenceEqual(
            SHA256.HashData(Encoding.UTF8.GetBytes(changedBand))));
        Assert.False(SHA256.HashData(canonical).SequenceEqual(
            SHA256.HashData(Encoding.UTF8.GetBytes(changedShift))));
        Assert.False(SHA256.HashData(canonical).SequenceEqual(
            SHA256.HashData(Encoding.UTF8.GetBytes(changedSource))));
        Assert.False(SHA256.HashData(canonical).SequenceEqual(
            SHA256.HashData(Encoding.UTF8.GetBytes(changedInteriorCoordinate))));
    }
}
