using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class MovementRulesTests
{
    [Fact]
    public void MobilityVocabularyIsClosedAndStable()
    {
        Assert.Equal(
            [
                ("land.mobility.non-motorized", MovementMobilityClass.NonMotorized),
                ("land.mobility.motorized", MovementMobilityClass.Motorized),
            ],
            Cna1979Movement.Mobility
                .Select(value => (value.MobilityId, value.MobilityClass)));

        Assert.True(Cna1979Movement.IsSupportedMobilityId(
            Cna1979Movement.NonMotorizedMobilityId));
        Assert.True(Cna1979Movement.IsSupportedMobilityId(
            Cna1979Movement.MotorizedMobilityId));
        Assert.False(Cna1979Movement.IsSupportedMobilityId("land.mobility.unknown"));
        Assert.False(Cna1979Movement.IsSupportedMobilityId(null));
    }

    [Theory]
    [InlineData("land.terrain.clear", "land.mobility.non-motorized", 2, 1, 6)]
    [InlineData("land.terrain.clear", "land.mobility.motorized", 2, 1, 6)]
    [InlineData("land.terrain.desert", "land.mobility.non-motorized", 3, 1, 6)]
    [InlineData("land.terrain.desert", "land.mobility.motorized", 4, 1, 6)]
    public void TerrainLookupsReturnExactCostsAndStoppingLimits(
        string terrainId,
        string mobilityId,
        long numerator,
        int denominator,
        int stackingLimit)
    {
        var result = Cna1979Movement.LookupTerrain(terrainId, mobilityId);

        Assert.True(result.IsSupported);
        Assert.Null(result.UnsupportedKind);
        Assert.Equal(new CapabilityPointAmount(numerator, denominator), result.Value.Cost);
        Assert.Equal(stackingLimit, result.Value.StoppingStackingLimit);
        Assert.Contains(result.Sources, source =>
            source.SourceId == "spi-1979-map-a" && source.Locator == "8.37");
    }

    [Theory]
    [InlineData("land.edge.road", "land.mobility.non-motorized", "override", 1, 1, 5)]
    [InlineData("land.edge.road", "land.mobility.motorized", "override", 1, 2, 5)]
    [InlineData("land.edge.track", "land.mobility.non-motorized", "scale-underlying", 1, 2, 5)]
    [InlineData("land.edge.track", "land.mobility.motorized", "scale-underlying", 1, 2, 5)]
    public void RouteLookupsCarryRoadOverridesAndCorrectedTrackFactors(
        string routeId,
        string mobilityId,
        string costKind,
        long numerator,
        int denominator,
        int stackingLimit)
    {
        var result = Cna1979Movement.LookupRoute(routeId, mobilityId);

        Assert.True(result.IsSupported);
        var expectedCostKind = costKind switch
        {
            "override" => MovementRouteCostKind.Override,
            "scale-underlying" => MovementRouteCostKind.ScaleUnderlying,
            _ => throw new ArgumentOutOfRangeException(
                nameof(costKind),
                costKind,
                "Unsupported test cost kind."),
        };
        Assert.Equal(expectedCostKind, result.Value.CostKind);
        Assert.Equal(new CapabilityPointAmount(numerator, denominator), result.Value.Amount);
        Assert.Equal(stackingLimit, result.Value.TraversalStackingLimit);

        if (routeId == "land.edge.track")
        {
            Assert.Contains(result.Sources, source =>
                source.SourceId == "spi-1979-errata" && source.Locator == "8.37");
        }
    }

    [Theory]
    [InlineData("land.edge.ridge", MovementHexsideDirection.Either, "land.mobility.non-motorized", 2)]
    [InlineData("land.edge.ridge", MovementHexsideDirection.Either, "land.mobility.motorized", 4)]
    [InlineData("land.edge.slope", MovementHexsideDirection.Up, "land.mobility.non-motorized", 2)]
    [InlineData("land.edge.slope", MovementHexsideDirection.Up, "land.mobility.motorized", 4)]
    [InlineData("land.edge.slope", MovementHexsideDirection.Down, "land.mobility.non-motorized", 1)]
    [InlineData("land.edge.slope", MovementHexsideDirection.Down, "land.mobility.motorized", 2)]
    public void HexsideLookupsReturnExactAddedCosts(
        string hexsideId,
        MovementHexsideDirection direction,
        string mobilityId,
        long wholeCost)
    {
        var result = Cna1979Movement.LookupHexside(hexsideId, direction, mobilityId);

        Assert.True(result.IsSupported);
        Assert.Equal(new CapabilityPointAmount(wholeCost, 1), result.Value.AddedCost);
        Assert.Contains(result.Sources, source =>
            source.SourceId == "spi-1979-map-a" && source.Locator == "8.37");
    }

    [Fact]
    public void BattalionStackingValueIsDerivedFromRulesData()
    {
        var result = Cna1979Movement.LookupStackingValue("land.organization.battalion");

        Assert.True(result.IsSupported);
        Assert.Equal(1, result.Value.StackingValue);
        Assert.Contains(result.Sources, source =>
            source.SourceId == "spi-1979-common-charts"
                && source.Locator == "9.4.stacking-point-values");
    }

    [Fact]
    public void UnknownInputsReturnTypedUnsupportedResultsWithoutFallbacks()
    {
        AssertUnsupported(
            Cna1979Movement.LookupTerrain("land.terrain.clear", "land.mobility.unknown"),
            MovementRuleUnsupportedKind.Mobility);
        AssertUnsupported(
            Cna1979Movement.LookupTerrain(
                "land.terrain.mountain",
                Cna1979Movement.NonMotorizedMobilityId),
            MovementRuleUnsupportedKind.Terrain);
        AssertUnsupported(
            Cna1979Movement.LookupRoute(
                "land.edge.rail",
                Cna1979Movement.NonMotorizedMobilityId),
            MovementRuleUnsupportedKind.Route);
        AssertUnsupported(
            Cna1979Movement.LookupHexside(
                "land.edge.slope",
                MovementHexsideDirection.Either,
                Cna1979Movement.NonMotorizedMobilityId),
            MovementRuleUnsupportedKind.Hexside);
        AssertUnsupported(
            Cna1979Movement.LookupStackingValue("land.organization.regiment"),
            MovementRuleUnsupportedKind.Organization);
    }

    [Fact]
    public void CanonicalArtifactHasStrictGoldenBytesAndRulesetIdentity()
    {
        var canonical = MovementRulesArtifactCodec.SerializeCanonical(Cna1979Movement.Definition);
        var goldenPath = Path.Combine(
            AppContext.BaseDirectory,
            "Rules",
            "Fixtures",
            "cna-1979.1.movement-tables.v1.golden.json");
        var goldenFile = File.ReadAllBytes(goldenPath);
        var golden = goldenFile.AsSpan(0, goldenFile.Length - 1).ToArray();

        var parsed = MovementRulesArtifactCodec.Deserialize(canonical);
        Assert.Equal((byte)'\n', goldenFile[^1]);
        Assert.Equal(golden, canonical);
        Assert.Equal(canonical, MovementRulesArtifactCodec.SerializeCanonical(parsed));
        Assert.Throws<JsonException>(() => MovementRulesArtifactCodec.Deserialize([.. canonical, (byte)'\n']));

        var artifact = Cna1979Movement.CreateArtifact();
        Assert.Equal("cna-1979.1.movement-tables", artifact.ArtifactId);
        Assert.Equal(
            $"sha256:{Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant()}",
            artifact.ContentHash);
        Assert.Equal(
            "sha256:9d016292838fb9ad3397d699ecfab10e0d867eeee09ed3d7c4f78a26b3394ba5",
            artifact.ContentHash);
        var manifestArtifact = Assert.Single(
            Cna1979Ruleset.Manifest.Artifacts,
            value => value.ArtifactId == artifact.ArtifactId);
        Assert.Equal(artifact.ContentHash, manifestArtifact.ContentHash);
        Assert.Equal(artifact.Sources, manifestArtifact.Sources);
        Assert.Equal(8, Cna1979Ruleset.ContractVersion);
    }

    [Fact]
    public void ArtifactReaderRejectsChangedTrackAuthorityOrNoncanonicalBytes()
    {
        var canonical = Encoding.UTF8.GetString(
            MovementRulesArtifactCodec.SerializeCanonical(Cna1979Movement.Definition));

        var flatTrack = canonical.Replace(
            "\"costKind\":\"scale-underlying\",\"amount\":{\"numerator\":1,\"denominator\":2}",
            "\"costKind\":\"override\",\"amount\":{\"numerator\":1,\"denominator\":1}",
            StringComparison.Ordinal);
        var changedProvenance = canonical.Replace(
            "{\"sourceId\":\"spi-1979-errata\",\"locator\":\"8.37\"}",
            "{\"sourceId\":\"spi-1979-map-a\",\"locator\":\"8.37\"}",
            StringComparison.Ordinal);
        var reordered = canonical.Replace(
            "{\"schemaVersion\":1,\"mobility\"",
            "{\"mobility\":[],\"schemaVersion\":1,\"ignored\"",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => MovementRulesArtifactCodec.Deserialize(
            Encoding.UTF8.GetBytes(flatTrack)));
        Assert.Throws<JsonException>(() => MovementRulesArtifactCodec.Deserialize(
            Encoding.UTF8.GetBytes(changedProvenance)));
        Assert.Throws<JsonException>(() => MovementRulesArtifactCodec.Deserialize(
            Encoding.UTF8.GetBytes(reordered)));
    }

    private static void AssertUnsupported<T>(
        MovementRuleLookupResult<T> result,
        MovementRuleUnsupportedKind expectedKind)
    {
        Assert.False(result.IsSupported);
        Assert.Equal(expectedKind, result.UnsupportedKind);
        Assert.Empty(result.Sources);
        Assert.Throws<InvalidOperationException>(() => _ = result.Value);
    }
}
