using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignMovementCalculatorTests
{
    [Fact]
    public void RoadOverrideIsRecalculatedWithAuthoritativeProvenance()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var candidate = CampaignMovementTestData.FindMove(
            evidence.Snapshot,
            evidence.Context,
            evidence.ActingSide,
            "commonwealth-element-a",
            "north-east");

        var moved = CampaignMovementEventFactory.Create(
            evidence.Snapshot,
            evidence.Context,
            CampaignMovementTestData.CommandFor(
                evidence.Snapshot,
                evidence.ActingSide,
                candidate));

        Assert.Equal(evidence.Snapshot.StateVersion, moved.PriorStateVersion);
        Assert.Equal(evidence.Snapshot.StateVersion + 1, moved.StateVersion);
        Assert.Equal(Cna1979Movement.MotorizedMobilityId, moved.MobilityId);
        Assert.NotEmpty(moved.MobilitySources);
        Assert.Equal("land.terrain.clear", moved.Cost.DestinationTerrainId);
        Assert.Equal(new CapabilityPointAmount(2, 1), moved.Cost.DestinationTerrainCost);
        Assert.NotEmpty(moved.Cost.DestinationTerrainSources);
        Assert.Equal("land.edge.road", moved.Cost.RouteAdjustment!.RouteId);
        Assert.Equal(MovementRouteCostKind.Override, moved.Cost.RouteAdjustment.CostKind);
        Assert.Equal(new CapabilityPointAmount(1, 2), moved.Cost.TotalCost);
        Assert.NotEmpty(moved.Cost.RouteAdjustment.Sources);
        Assert.Empty(moved.Cost.CrossedHexsideCosts);
    }

    [Fact]
    public void HexsideAdditionIsRecalculatedWithoutACommandCost()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var candidate = CampaignMovementTestData.FindMove(
            evidence.Snapshot,
            evidence.Context,
            evidence.ActingSide,
            "commonwealth-element-a",
            "center");

        var moved = CampaignMovementEventFactory.Create(
            evidence.Snapshot,
            evidence.Context,
            CampaignMovementTestData.CommandFor(
                evidence.Snapshot,
                evidence.ActingSide,
                candidate));

        Assert.Null(moved.Cost.RouteAdjustment);
        var crossed = Assert.Single(moved.Cost.CrossedHexsideCosts);
        Assert.Equal("land.edge.ridge", crossed.HexsideId);
        Assert.Equal(MovementHexsideDirection.Either, crossed.Direction);
        Assert.Equal(new CapabilityPointAmount(4, 1), crossed.AddedCost);
        Assert.NotEmpty(crossed.Sources);
        Assert.Equal(new CapabilityPointAmount(8, 1), moved.Cost.TotalCost);
    }

    [Fact]
    public void ForgedCandidateIdentityIsRejectedAfterAuthoritativeRecalculation()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var candidate = CampaignMovementTestData.FindMove(
            evidence.Snapshot,
            evidence.Context,
            evidence.ActingSide,
            "commonwealth-element-a",
            "north-east");
        var command = CampaignMovementTestData.CommandFor(
            evidence.Snapshot,
            evidence.ActingSide,
            candidate) with
        {
            CandidateId = $"sha256:{new string('0', 64)}",
        };

        Assert.Throws<InvalidOperationException>(() => CampaignMovementEventFactory.Create(
            evidence.Snapshot,
            evidence.Context,
            command));
    }
}
