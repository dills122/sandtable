using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Original Map D, sheet hash 8830ec78...fd030, and Land Terrain Effects
// Chart 8.37. This is one verified path, not a general map implementation.
internal static class CampaignSourceFourthIndianRoad
{
    internal const string Origin = "d3615";
    internal const string Destination = "d3715";
    internal const string SupplyLocation = "d3714";
    internal const int DestinationTerrainStackLimit = 6;
    internal const int RoadStackLimit = 5;
    internal const string SourceMapHash = "sha256:8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030";

    internal static void RequirePinnedSource(CampaignSourcePlayableWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        if (world.Source.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash)
            throw new InvalidOperationException("Opening road requires pinned Graziani source content.");
        var map = Cna1979HistoricalMap.Instance;
        var moveEdge = map.FeaturesBetween(Origin, Destination);
        var supplyEdge = map.FeaturesBetween(Destination, SupplyLocation);
        var destination = Cna1979Movement.LookupTerrain("land.terrain.clear", Cna1979Movement.NonMotorizedMobilityId);
        if (map.TerrainAt(Origin) != "rough" || map.TerrainAt(Destination) != "clear"
            || map.TerrainAt(SupplyLocation) != "clear"
            || !IsPlainRoad(moveEdge) || !IsPlainRoad(supplyEdge)
            || !destination.IsSupported || destination.Value.StoppingStackingLimit != DestinationTerrainStackLimit)
            throw new InvalidOperationException("Opening road differs from the pinned historical map.");
    }

    private static bool IsPlainRoad(IReadOnlyList<string>? features) => features is not null
        && features.Contains("road", StringComparer.Ordinal)
        && features.All(value => value is "road" or "railroad");

    // One road edge, with no unsupported hexside feature cost. The source
    // chart charges one CP for foot and one-half CP for motorized units.
    internal static CapabilityPointAmount Cost(bool motorized)
    {
        var mobility = motorized ? Cna1979Movement.MotorizedMobilityId : Cna1979Movement.NonMotorizedMobilityId;
        var lookup = Cna1979Movement.LookupRoute("land.edge.road", mobility);
        var expected = motorized ? new CapabilityPointAmount(1, 2) : new CapabilityPointAmount(1, 1);
        if (!lookup.IsSupported || lookup.Value.CostKind != MovementRouteCostKind.Override
            || lookup.Value.TraversalStackingLimit != RoadStackLimit || lookup.Value.Amount != expected)
            throw new InvalidOperationException("Opening road requires the pinned movement-table route.");
        return lookup.Value.Amount;
    }

    // The two source-verified road edges from D3615 via D3715 to D3714
    // cost a medium truck one CP under 8.37 and are within 32.16's half CPA.
    internal static CapabilityPointAmount SupplyTraceCost => Cost(motorized: true) + Cost(motorized: true);
}
