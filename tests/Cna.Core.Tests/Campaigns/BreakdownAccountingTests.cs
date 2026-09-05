using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class BreakdownAccountingTests
{
    [Theory]
    [InlineData("clear", null, BreakdownWeatherKind.Normal, 4, 1, null)]
    [InlineData("desert", null, BreakdownWeatherKind.Hot, 24, 1, null)]
    [InlineData("clear", "road", BreakdownWeatherKind.Normal, 1, 2, "road")]
    [InlineData("desert", "road", BreakdownWeatherKind.Normal, 1, 2, "road")]
    [InlineData("clear", "track", BreakdownWeatherKind.Normal, 2, 1, "track")]
    [InlineData("desert", "track", BreakdownWeatherKind.Normal, 12, 1, "track")]
    [InlineData("desert", "road", BreakdownWeatherKind.Rainstorm, 12, 1, "track")]
    [InlineData("clear", "road", BreakdownWeatherKind.Rainstorm, 2, 1, "track")]
    public void TerrainAndRouteOperationsUseExactNormalizedBp(string terrain, string? route,
        BreakdownWeatherKind weather, int numerator, int denominator, string? effectiveRoute)
    {
        var step = Calculate(terrain, route, weather);
        Assert.Equal(new BreakdownPointAmount(numerator, denominator), step.Delta);
        Assert.Equal(route is null ? null : "land.edge." + route, step.InputRouteId);
        Assert.Equal(effectiveRoute is null ? null : "land.edge." + effectiveRoute, step.EffectiveRouteId);
    }

    [Theory]
    [InlineData("a", BreakdownHexsideDirection.Up)]
    [InlineData("b", BreakdownHexsideDirection.Down)]
    public void RainstormTransformsRoadBeforeAllDirectionalAdditions(string directionFrom, BreakdownHexsideDirection direction)
    {
        var edge = Edge("road", new ContentEdgeFeature("land.edge.slope", directionFrom, Origin("slope")),
            new ContentEdgeFeature("land.edge.ridge", null, Origin("ridge")));
        var step = CampaignBreakdownAccounting.Calculate(Cohort(), Ledger(), edge, Destination("desert"), "a", BreakdownWeatherKind.Rainstorm);
        Assert.Equal(new BreakdownPointAmount(16, 1), step.Delta);
        Assert.Equal(["land.edge.ridge", "land.edge.slope"], step.Hexsides.Select(value => value.HexsideId));
        Assert.Equal(direction, step.Hexsides[1].Direction);
        Assert.All(step.Hexsides, value => Assert.Equal(new BreakdownPointAmount(2, 1), value.AddedPoints));
        Assert.Contains(new RuleReference("spi-1979-land-rules", "21.37c"), step.Sources);
        Assert.Contains(new RuleReference("spi-1979-errata", "8.37"), step.Sources);
        Assert.Contains(new RuleReference("breakdown-test", "slope"), step.Sources);
        Assert.Contains(new RuleReference("breakdown-test", "desert"), step.Sources);
        Assert.Equal(step.Sources.Distinct().OrderBy(value => value.SourceId, StringComparer.Ordinal).ThenBy(value => value.Locator, StringComparer.Ordinal), step.Sources);
        Assert.Equal(new RuleReference[] { new("breakdown-test", "cohort"), new("breakdown-test", "desert"),
            new("breakdown-test", "edge"), new("breakdown-test", "ridge"), new("breakdown-test", "road"),
            new("breakdown-test", "slope"), new("spi-1979-errata", "8.37"),
            new("spi-1979-land-rules", "21.37c"), new("spi-1979-map-a", "8.37") }, step.Sources);
    }

    [Fact]
    public void SandstormAttributesEntireStepWhileOtherWeatherPreservesSubtotalAndCheckedMemory()
    {
        var before = new CampaignVehicleBreakdownState("cohort", new(7, 2), new(1, 2), "land.breakdown.band.4-10", 7, 5);
        var first = CampaignBreakdownAccounting.Calculate(Cohort(), before, Edge("road"), Destination("desert"), "a", BreakdownWeatherKind.Sandstorm);
        var after = CampaignBreakdownAccounting.Apply(before, first);
        Assert.Equal(new BreakdownPointAmount(4, 1), after.CumulativeBreakdownPoints);
        Assert.Equal(new BreakdownPointAmount(1, 1), after.SandstormAttributedBreakdownPoints);
        var second = CampaignBreakdownAccounting.Calculate(Cohort(), after, Edge(null), Destination("clear"), "a", BreakdownWeatherKind.Hot);
        var final = CampaignBreakdownAccounting.Apply(after, second);
        Assert.Equal(new BreakdownPointAmount(8, 1), final.CumulativeBreakdownPoints);
        Assert.Equal(new BreakdownPointAmount(1, 1), final.SandstormAttributedBreakdownPoints);
        Assert.Equal(before.HighestEffectiveCheckedBandId, final.HighestEffectiveCheckedBandId);
        Assert.Equal(7, final.WorkingPointCount); Assert.Equal(5, final.BrokenPointCount);
        Assert.Equal(new BreakdownPointAmount(7, 2), before.CumulativeBreakdownPoints);
    }

    [Theory]
    [InlineData("wrong-cohort")]
    [InlineData("wrong-edge")]
    [InlineData("wrong-origin")]
    [InlineData("wrong-profile")]
    [InlineData("duplicate")]
    [InlineData("unknown-feature")]
    [InlineData("double-route")]
    [InlineData("directional-route")]
    [InlineData("wrong-direction")]
    [InlineData("unknown-weather")]
    [InlineData("unknown-terrain")]
    [InlineData("wrong-count")]
    public void InvalidBindingsAndUnsupportedInputsReject(string mutation)
    {
        var cohort = mutation == "wrong-profile" ? new ContentBreakdownVehicleCohort("cohort", Cna1979Breakdown.VehicleTypeTruckId, 12, "unsupported-profile", Origin("cohort")) : Cohort();
        var ledger = mutation switch
        {
            "wrong-cohort" => new CampaignVehicleBreakdownState("other", BreakdownPointAmount.Zero, BreakdownPointAmount.Zero, null, 12, 0),
            "wrong-count" => new CampaignVehicleBreakdownState("cohort", BreakdownPointAmount.Zero, BreakdownPointAmount.Zero, null, 13, 0),
            _ => Ledger(),
        };
        var edge = mutation switch
        {
            "wrong-edge" => new ContentHexEdge("a", "c", [], Origin("edge")),
            "duplicate" => Edge(null, new ContentEdgeFeature("land.edge.ridge", null, Origin("ridge")), new ContentEdgeFeature("land.edge.ridge", null, Origin("ridge"))),
            "unknown-feature" => Edge(null, new ContentEdgeFeature("unknown", null, Origin("edge"))),
            "double-route" => Edge("road", new ContentEdgeFeature("land.edge.track", null, Origin("track"))),
            "directional-route" => Edge(null, new ContentEdgeFeature("land.edge.road", "a", Origin("road"))),
            "wrong-direction" => Edge(null, new ContentEdgeFeature("land.edge.slope", "c", Origin("slope"))),
            _ => Edge(null),
        };
        Assert.Throws<ArgumentException>(() => CampaignBreakdownAccounting.Calculate(cohort, ledger, edge,
            Destination(mutation == "unknown-terrain" ? "unknown" : "clear"), mutation == "wrong-origin" ? "b" : "a",
            mutation == "unknown-weather" ? (BreakdownWeatherKind)99 : BreakdownWeatherKind.Normal));
    }

    [Fact]
    public void ApplyingStepTwiceOrToDifferentLedgerRejects()
    {
        var before = Ledger();
        var step = Calculate("clear", null, BreakdownWeatherKind.Normal);
        var after = CampaignBreakdownAccounting.Apply(before, step);
        Assert.Throws<ArgumentException>(() => CampaignBreakdownAccounting.Apply(after, step));
        Assert.Throws<ArgumentException>(() => CampaignBreakdownAccounting.Apply(new("other", BreakdownPointAmount.Zero, BreakdownPointAmount.Zero, null, 12, 0), step));
    }

    [Fact]
    public void CanonicalStepRoundTripsAndRejectsShapeOrderRationalAndForgedTotals()
    {
        var bytes = CampaignBreakdownStepCodec.Serialize(Calculate("clear", "road", BreakdownWeatherKind.Sandstorm));
        Assert.Equal(Calculate("clear", "road", BreakdownWeatherKind.Sandstorm), CampaignBreakdownStepCodec.Deserialize(bytes));
        Assert.Equal(bytes, CampaignBreakdownStepCodec.Serialize(CampaignBreakdownStepCodec.Deserialize(bytes)));
        var json = Encoding.UTF8.GetString(bytes);
        using var document = JsonDocument.Parse(bytes);
        Assert.Equal(["cohortId", "vehicleTypeId", "profileId", "destinationTerrainId", "inputRouteId", "effectiveRouteId", "hexsides", "weatherKind", "before", "delta", "after", "sandstormBefore", "sandstormDelta", "sandstormAfter", "sources"], document.RootElement.EnumerateObject().Select(value => value.Name));
        string[] mutations = [json + " ", json.Replace("\"cohortId\":\"cohort\"", "\"cohortId\":\"cohort\",\"cohortId\":\"cohort\"", StringComparison.Ordinal),
            json.Replace("\"numerator\":1,\"denominator\":2", "\"numerator\":2,\"denominator\":4", StringComparison.Ordinal),
            json.Replace("\"after\":{\"numerator\":1", "\"after\":{\"numerator\":3", StringComparison.Ordinal),
            json.Replace("\"sources\":[", "\"unknown\":null,\"sources\":[", StringComparison.Ordinal)];
        foreach (var mutation in mutations) Assert.Throws<JsonException>(() => CampaignBreakdownStepCodec.Deserialize(Encoding.UTF8.GetBytes(mutation)));
    }

    private static CampaignBreakdownStep Calculate(string terrain, string? route, BreakdownWeatherKind weather) =>
        CampaignBreakdownAccounting.Calculate(Cohort(), Ledger(), Edge(route), Destination(terrain), "a", weather);
    private static ContentOrigin Origin(string locator) => new(ContentOriginKind.Synthetic, [new("breakdown-test", locator)]);
    private static ContentHex Destination(string terrain) => new("b", "land.terrain." + terrain, null, Origin(terrain));
    private static ContentBreakdownVehicleCohort Cohort() => new("cohort", Cna1979Breakdown.VehicleTypeTruckId, 12, Cna1979Breakdown.ProfileTruckId, Origin("cohort"));
    private static CampaignVehicleBreakdownState Ledger() => new("cohort", BreakdownPointAmount.Zero, BreakdownPointAmount.Zero, null, 12, 0);
    private static ContentHexEdge Edge(string? route, params ContentEdgeFeature[] hexsides) =>
        new("a", "b", route is null ? hexsides : hexsides.Prepend(new("land.edge." + route, null, Origin(route))), Origin("edge"));
}
