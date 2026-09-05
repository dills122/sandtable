using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class BreakdownRecordTests
{
    private const string Campaign = "breakdown-test";
    private static readonly string Rules = new('a', 64);

    [Fact]
    public void RouteIdentityExcludesOnlyMutableLocation()
    {
        var route = Route();
        var moved = route.AtLocation("location-b");
        Assert.Equal(route.RouteId, moved.RouteId);
        moved.ValidateIdentity(Campaign, Rules);
        Assert.Throws<ArgumentException>(() => moved.ValidateIdentity("another-campaign", Rules));
        Assert.Throws<ArgumentException>(() => moved.ValidateIdentity(Campaign, new string('b', 64)));
    }

    [Fact]
    public void AllSixFiniteFlowVariantsRoundTripWithExactCanonicalBytes()
    {
        var route = Route();
        var phasing = new CampaignPhasingContinuation.ResumeRoute(route);
        var stop = CampaignBreakdownStop.Create(Campaign, Rules, 4, route,
            CampaignBreakdownStopReason.ReactionCompleted, BreakdownWeatherKind.Normal, []);
        CampaignBreakdownFlow[] flows = [new CampaignBreakdownFlow.Idle(), new CampaignBreakdownFlow.Moving(route),
            new CampaignBreakdownFlow.Reacting(phasing, null), new CampaignBreakdownFlow.Reacting(phasing, route),
            new CampaignBreakdownFlow.ReactorStopOpen(phasing, stop),
            new CampaignBreakdownFlow.ReactorStopClosed(new CampaignPhasingContinuation.ResolveStop(stop), stop),
            new CampaignBreakdownFlow.PhasingStop(stop)];
        foreach (var flow in flows)
        {
            var bytes = CampaignBreakdownCodec.Serialize(flow);
            Assert.Equal(flow, CampaignBreakdownCodec.Deserialize(bytes));
            Assert.Equal(bytes, CampaignBreakdownCodec.Serialize(CampaignBreakdownCodec.Deserialize(bytes)));
        }
    }

    [Theory]
    [InlineData("{\"kind\":\"idle\",\"kind\":\"idle\"}")]
    [InlineData("{\"kind\":\"idle\",\"route\":null}")]
    [InlineData("{\"kind\":\"idle\"} ")]
    [InlineData("{\"kind\":\"unknown\"}")]
    [InlineData("{\"kind\":\"reacting\",\"phasingContinuation\":{\"kind\":\"reacting\"},\"reactorRoute\":null}")]
    public void InvalidFiniteShapesAndNoncanonicalBytesReject(string json) =>
        Assert.Throws<JsonException>(() => CampaignBreakdownCodec.Deserialize(Encoding.UTF8.GetBytes(json)));

    [Fact]
    public void StopAndLotIdentityBindAuthorityAndCounts()
    {
        var route = Route();
        var stop = CampaignBreakdownStop.Create(Campaign, Rules, 4, route,
            CampaignBreakdownStopReason.Deliberate, BreakdownWeatherKind.Normal, []);
        stop.ValidateIdentity(Campaign, Rules);
        var lot = CampaignBrokenVehicleLot.Create(Campaign, Rules, stop.StopId, LandSide.Axis,
            "truck-cohort", "truck", 2, "location-a", 5, [new RuleReference("spi-1979-land-rules", "21.41")]);
        lot.ValidateIdentity(Campaign, Rules);
        var changed = new CampaignBrokenVehicleLot(lot.LotId, lot.StopId, lot.CheckId, lot.Owner,
            lot.CohortId, lot.VehicleTypeId, 3, lot.LocationId, lot.CreatedStateVersion, lot.Sources);
        Assert.Throws<ArgumentException>(() => changed.ValidateIdentity(Campaign, Rules));
    }

    private static CampaignBreakdownRoute Route() => CampaignBreakdownRoute.Create(Campaign, Rules,
        2, "element-a", "representation-a", LandSide.Axis, "location-a", "location-a", []);
}
