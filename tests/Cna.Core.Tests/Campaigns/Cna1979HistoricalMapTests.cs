using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Campaigns;

public sealed class Cna1979HistoricalMapTests
{
    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(3, false)]
    [InlineData(99, false)]
    public void HostReplaySelectsLegacySourceTurnOnlyForVersionOne(int version, bool legacy)
    {
        var record = System.Text.Encoding.UTF8.GetBytes($"{{\"contractVersion\":{version}}}");
        var step = new CampaignHostAcceptedStep([1], [record], [1], [1], CampaignHostAcceptedStep.SourceTurnStartKind);
        Assert.Equal(legacy, CampaignHostAdapter.IsLegacySourceTurnStart(step));
        Assert.Equal(version, CampaignHostAdapter.SourceTurnStartVersion(step));
    }

    [Fact]
    public void ApprovedCdCrossingUsesVerifiedTrackAndKeepsOtherSeamsUnknown()
    {
        var map = Cna1979HistoricalMap.Instance;
        Assert.Null(map.EdgeBetween("c3533", "d3501"));
        var edge = map.EdgeBetween("c3533", "d3501", useCdSeam: true)!;
        Assert.Equal("track", Assert.Single(edge.Features));
        Assert.Equal(edge, map.EdgeBetween("d3501", "c3533", useCdSeam: true));
        var diagonal = map.EdgeBetween("c4033", "d3901", useCdSeam: true)!;
        Assert.Equal("track", Assert.Single(diagonal.Features));
        Assert.Equal(diagonal, map.EdgeBetween("d3901", "c4033", useCdSeam: true));
        Assert.Null(map.EdgeBetween("c4033", "d3901"));
        var unfinishedRail = map.EdgeBetween("c3633", "d3601", useCdSeam: true)!;
        Assert.Equal("unfinished-railroad", Assert.Single(unfinishedRail.Features));
        Assert.False(Cna1979Movement.LookupRoute("land.edge.unfinished-railroad", Cna1979Movement.MotorizedMobilityId).IsSupported);
        Assert.Equal(unfinishedRail, map.EdgeBetween("d3601", "c3633", useCdSeam: true));
        Assert.Null(map.EdgeBetween("c3733", "d3701", useCdSeam: true));
        Assert.Null(map.TerrainAt("c3534"));
        Assert.False(CampaignSourceInitialContacts.TryAdjacent("c3533", "d3501", out _, useCdSeam: false));
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c3533", "d3501", out var adjacent, useCdSeam: true));
        Assert.True(adjacent);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c4033", "d3901", out adjacent, useCdSeam: true));
        Assert.True(adjacent);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("c3633", "d3601", out adjacent, useCdSeam: true));
        Assert.True(adjacent);
        Assert.False(CampaignSourceInitialContacts.TryAdjacent("c3733", "d3701", out _, useCdSeam: true));
        Assert.False(CampaignSourceInitialContacts.TryAdjacent("c3533", "d3501", out _, useHistoricalMap: false, useCdSeam: true));
        Cna1979HistoricalMap.RequireCdSeamPolicy();
        CampaignSourceContactCounter[] counters =
        [
            new("axis-cd", "axis", "axis-cd", "c3533"),
            new("cw-cd", "commonwealth", "cw-cd", "d3501"),
        ];
        var knowledge = new CampaignContactKnowledge("graziani-cd", LandSide.Axis);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceInitialContacts.DeriveCounters(counters, 2, knowledge, true, false));
        var contact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(counters, 2, knowledge, true, true));
        Assert.Equal("cw-cd", contact.BoundRepresentationId);
        var opposing = new CampaignContactKnowledge("graziani-cd", LandSide.Commonwealth);
        var opposingContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(counters, 2, opposing, true, true));
        Assert.Equal("axis-cd", opposingContact.BoundRepresentationId);
    }

    [Fact]
    public void PinnedMapContainsSourceCheckedMatruhRoadAndTerrain()
    {
        var map = Cna1979HistoricalMap.Instance;
        Assert.Equal(7073, map.HexCount);
        Assert.Equal("rough", map.TerrainAt("d3615"));
        Assert.Equal("clear", map.TerrainAt("d3715"));
        Assert.Equal("village", map.HexAt("d3714")!.Settlement);
        Assert.True(map.HexAt("d3714")!.Coastal);
        Assert.Contains("road", map.FeaturesBetween("d3615", "d3715")!);
        Assert.Contains("road", map.FeaturesBetween("d3714", "d3715")!);
        Assert.Equal(map.FeaturesBetween("d3715", "d3615"), map.FeaturesBetween("d3615", "d3715"));
    }

    [Fact]
    public void UnknownHexesAndUncertifiedSeamsDoNotBecomeClearEdges()
    {
        var map = Cna1979HistoricalMap.Instance;
        Assert.Null(map.TerrainAt("c9999"));
        // The original C sheet prints this overlap label, but alias identity
        // with D3501 is not yet admitted by the versioned map policy.
        Assert.Null(map.TerrainAt("c3534"));
        Assert.NotNull(map.TerrainAt("d3501"));
        Assert.Null(map.FeaturesBetween("c9999", "c1014"));
        Assert.Null(map.FeaturesBetween("c1014", "c1016"));
        Assert.Null(map.FeaturesBetween("c4033", "d4001"));
        Assert.Empty(map.FeaturesBetween("c1014", "c1015")!);
        CampaignSourcePlayableWorld.RequireKnownMapLocations(["d3615", "cairo", null]);
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePlayableWorld.RequireKnownMapLocations(["c9999"]));
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePlayableWorld.RequireKnownMapLocations(["c3534"]));
    }

    [Fact]
    public void AlteredMapBytesAreRejectedBeforeParsing()
    {
        var altered = System.Text.Encoding.UTF8.GetBytes("{}");
        Assert.Throws<InvalidOperationException>(() => Cna1979HistoricalMap.ParseVerified(altered, Cna1979HistoricalMap.HexHash));
    }

    [Fact]
    public void PolishArrivalEntryAreaMatchesThePinnedCairoCityTerrain()
    {
        var map = Cna1979HistoricalMap.Instance;
        Assert.Equal(5, Cna1979GrazianiSourceFacts.CairoEntryArea.LocationIds.Count);
        Assert.All(Cna1979GrazianiSourceFacts.CairoEntryArea.LocationIds,
            location => Assert.Equal("major-city", map.TerrainAt(location)));
        Assert.Equal("delta", map.TerrainAt("e1929"));
    }

    [Fact]
    public void OpeningMoveAndSupplyTraceUseNormalRoadTable()
    {
        Assert.Equal(new CapabilityPointAmount(1, 1), CampaignSourceFourthIndianRoad.Cost(false));
        Assert.Equal(new CapabilityPointAmount(1, 2), CampaignSourceFourthIndianRoad.Cost(true));
        Assert.Equal(new CapabilityPointAmount(1, 1), CampaignSourceFourthIndianRoad.SupplyTraceCost);
    }

    [Fact]
    public void SlopeRetainsHigherHexInBothDirections()
    {
        var map = Cna1979HistoricalMap.Instance;
        var edge = map.EdgeBetween("d0107", "d0207")!;
        Assert.Contains("slope", edge.Features);
        Assert.Equal("D0107", edge.HigherHex);
        Assert.Equal(edge, map.EdgeBetween("d0207", "d0107"));
        var unresolved = map.EdgeBetween("b1705", "b1706")!;
        Assert.Contains("slope", unresolved.Features);
        Assert.Null(unresolved.HigherHex);
    }

    [Fact]
    public void SourceContactsResolveSameSheetGridWithoutAssumingCrossSheetSeams()
    {
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("d3615", "d3715", out var adjacent));
        Assert.True(adjacent);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("d3615", "d3716", out adjacent));
        Assert.True(adjacent);
        Assert.True(CampaignSourceInitialContacts.TryAdjacent("d3715", "d3616", out adjacent));
        Assert.False(adjacent);
        Assert.False(CampaignSourceInitialContacts.TryAdjacent("c1716", "d3612", out _));
    }

    [Fact]
    public void SourceContactProjectionObservesNewlyKnownSameSheetNeighbor()
    {
        CampaignSourceContactCounter[] counters =
        [
            new("axis-counter", "axis", "axis-formation", "e1520"),
            new("cw-counter", "commonwealth", "cw-formation", "e1521"),
        ];
        var knowledge = new CampaignContactKnowledge("campaign-contact-test", LandSide.Axis);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceInitialContacts.DeriveCounters(counters, 2, knowledge,
            useHistoricalMap: false));
        var contact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(counters, 2, knowledge));
        Assert.Equal("cw-counter", contact.BoundRepresentationId);
        var opposing = new CampaignContactKnowledge("campaign-contact-test", LandSide.Commonwealth);
        var otherContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(counters, 2, opposing));
        Assert.Equal("axis-counter", otherContact.BoundRepresentationId);
    }

    [Fact]
    public void SourceMovementContactUsesSameSheetGeometry()
    {
        CampaignSourceContactCounter[] before =
        [
            new("axis-counter", "axis", "axis-formation", "e1520"),
            new("cw-counter", "commonwealth", "cw-formation", "e1522"),
        ];
        var after = before.Select(value => value.RepresentationId == "cw-counter"
            ? value with { LocationId = "e1521" } : value).ToArray();
        var knowledge = new CampaignContactKnowledge("campaign-move-test", LandSide.Axis);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceMovementContacts.DeriveCounters(before, after,
            "cw-counter", 2, knowledge, useHistoricalMap: false));
        var contact = Assert.Single(CampaignSourceMovementContacts.DeriveCounters(before, after, "cw-counter", 2, knowledge));
        Assert.Equal(CampaignContactTransitionKind.Observed, contact.Kind);
        Assert.Equal("cw-counter", contact.BoundRepresentationId);
    }

    [Fact]
    public void GridMatchesPreviouslyScannedMapCNeighborhoods()
    {
        var map = Cna1979HistoricalMap.Instance;
        foreach (var anchor in new[] { "c3320", "c3617" })
        {
            var expected = ContentSourceLocalMapPackArtifact.Neighbors(anchor).ToHashSet(StringComparer.Ordinal);
            var row = int.Parse(anchor.AsSpan(1, 2), System.Globalization.CultureInfo.InvariantCulture);
            var column = int.Parse(anchor.AsSpan(3, 2), System.Globalization.CultureInfo.InvariantCulture);
            for (var r = row - 1; r <= row + 1; r++)
                for (var c = column - 1; c <= column + 1; c++)
                {
                    var candidate = $"c{r:00}{c:00}";
                    Assert.Equal(expected.Contains(candidate), map.EdgeBetween(anchor, candidate) is not null);
                }
        }
    }
}
