using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class BreakdownResolutionReplayTests
{
    [Fact]
    public void RolledResolutionReplaysExactRngAndCreatesConservedStationaryLot()
    {
        var prior = PendingTruck();
        var artifact = BreakdownContentFixture.Artifact(); var scenario = BreakdownSnapshotTests.Scenario();
        var resolved = Resolve(prior);
        var bytes = CampaignBreakdownStopResolvedCodec.Serialize(resolved);
        Assert.Equal(resolved, CampaignBreakdownStopResolvedCodec.Deserialize(bytes));
        var after = CampaignV11BreakdownProjector.Apply(prior, resolved, artifact, scenario);
        Assert.IsType<CampaignBreakdownFlow.Idle>(after.BreakdownFlow);
        var check = Assert.Single(resolved.Checks);
        Assert.NotNull(check.Roll);
        Assert.True(check.Roll.LossCount > 0);
        var lot = Assert.Single(after.World.BrokenVehicleLots);
        Assert.Equal("center", lot.LocationId);
        Assert.Equal(check.Roll.LossCount, lot.PointCount);
        var truck = after.World.Elements.Single(x => x.ElementId == "axis-truck");
        Assert.Equal(12, truck.OperationalState.VehicleBreakdownState!.WorkingPointCount + lot.PointCount);
        Assert.Equal(resolved.RandomStateAfter, after.RandomState);
        Assert.Equal(prior.World.Elements.Single(x => x.ElementId == truck.ElementId).OperationalState.CapabilityPointsExpended,
            truck.OperationalState.CapabilityPointsExpended);
        Assert.Equal(after, CampaignSnapshotV11Serializer.Deserialize(CampaignSnapshotV11Serializer.Serialize(after, artifact, scenario), artifact, scenario));
        Assert.Throws<InvalidCampaignHistoryException>(() => CampaignV11BreakdownProjector.Apply(after, resolved, artifact, scenario));
    }

    [Theory]
    [InlineData("face")]
    [InlineData("cursor")]
    [InlineData("fraction")]
    [InlineData("lot-location")]
    [InlineData("memory")]
    [InlineData("sources")]
    [InlineData("continuation")]
    [InlineData("action")]
    public void ForgedResolutionCannotChangeAuthority(string mutation)
    {
        var prior = PendingTruck(); var before = CampaignSnapshotV11Serializer.Serialize(prior);
        var resolved = Resolve(prior);
        var node = JsonNode.Parse(CampaignBreakdownStopResolvedCodec.Serialize(resolved))!;
        switch (mutation)
        {
            case "face": node["checks"]![0]!["roll"]!["firstDie"] = 1; break;
            case "cursor": node["randomStateAfter"]!["nextByteCursor"] = 99; break;
            case "fraction": node["checks"]![0]!["roll"]!["fraction"]!["numerator"] = 0; break;
            case "lot-location": node["createdLots"]![0]!["locationId"] = "west"; break;
            case "memory": node["checks"]![0]!["highestEffectiveCheckedBandIdAfter"] = "land.breakdown.band.71-plus"; break;
            case "sources": node["sources"]!.AsArray().Last()!["locator"] = "zzzz-forged"; break;
            case "continuation": node["breakdownFlowAfter"] = JsonNode.Parse(CampaignBreakdownCodec.Serialize(prior.BreakdownFlow)); break;
            case "action": node["actionId"] = CampaignBreakdownCodec.EmptyHash; break;
        }
        Assert.ThrowsAny<Exception>(() => CampaignV11BreakdownProjector.Apply(prior,
            CampaignBreakdownStopResolvedCodec.Deserialize(Encoding.UTF8.GetBytes(node.ToJsonString())),
            BreakdownContentFixture.Artifact(), BreakdownSnapshotTests.Scenario()));
        Assert.Equal(before, CampaignSnapshotV11Serializer.Serialize(prior));
    }

    [Fact]
    public void ZeroCohortStopStillResolvesWithoutRngOrWorldChanges()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var moved = BreakdownMovementTests.Move(prior, "axis-infantry", "north-west", "north");
        var artifact = BreakdownContentFixture.Artifact(); var scenario = BreakdownSnapshotTests.Scenario();
        prior = CampaignV11MoveProjector.ApplyMovement(prior, moved, artifact, scenario);
        prior = RecordStop(prior);
        var resolved = Resolve(prior);
        var after = CampaignV11BreakdownProjector.Apply(prior, resolved, artifact, scenario);
        Assert.Empty(resolved.Checks); Assert.Empty(resolved.CreatedLots);
        Assert.Equal(prior.World, after.World); Assert.Equal(prior.RandomState, after.RandomState);
        Assert.IsType<CampaignBreakdownFlow.Idle>(after.BreakdownFlow);
    }

    [Fact]
    public void CanonicalButWrongDiceAreRejectedByAuthoritativeReplay()
    {
        var prior = PendingTruck(2);
        var resolved = Resolve(prior);
        Assert.Equal(51, Assert.Single(resolved.Checks).Roll!.Coordinate);
        var node = JsonNode.Parse(CampaignBreakdownStopResolvedCodec.Serialize(resolved))!;
        node["checks"]![0]!["roll"]!["secondDie"] = 2;
        node["checks"]![0]!["roll"]!["coordinate"] = 52;
        // Both coordinates have the same table outcome, so this is structurally valid evidence.
        var forged = CampaignBreakdownStopResolvedCodec.Deserialize(Encoding.UTF8.GetBytes(node.ToJsonString()));
        Assert.Throws<InvalidCampaignHistoryException>(() => CampaignV11BreakdownProjector.Apply(prior,
            forged, BreakdownContentFixture.Artifact(), BreakdownSnapshotTests.Scenario()));
    }

    [Fact]
    public void CheckpointHistoryReplaysThroughStopsAndBothSegmentCompletions()
    {
        var artifact = BreakdownContentFixture.Artifact(); var scenario = BreakdownSnapshotTests.Scenario();
        var original = BreakdownSnapshotTests.Create(true);
        var initial = new CampaignSnapshotV11(11, original.CampaignId, original.StateVersion, original.RulesetHash,
            original.Setup, original.World, original.InitiativeHolder, original.OperationStageOrders, original.OperationStageWeather,
            SandtableRandom.Create(0), original.CurrentPosition, null, original.BreakdownFlow);
        var live = initial; var history = new List<byte[]>();
        void Apply(CampaignSuccessorEvent value)
        {
            history.Add(CampaignBreakdownEventSerializer.Serialize(value));
            live = CampaignV11BreakdownProjector.Apply(live, value, artifact, scenario);
        }
        Apply(BreakdownMovementTests.Move(live, "axis-truck", "west", "center"));
        Apply(CampaignBreakdownLifecycleFactory.CreateStop(live, artifact, scenario));
        Apply(Resolve(live));
        var firstLot = Assert.Single(live.World.BrokenVehicleLots);
        Apply(BreakdownMovementTests.Move(live, "axis-truck", "center", "west"));
        if (live.BreakdownFlow is CampaignBreakdownFlow.Moving)
            Apply(CampaignBreakdownLifecycleFactory.CreateStop(live, artifact, scenario));
        Apply(Resolve(live));
        Assert.Contains(firstLot, live.World.BrokenVehicleLots);
        Assert.Equal("center", firstLot.LocationId);
        var beforeCompletion = live;
        Apply(CampaignBreakdownLifecycleFactory.CreateMovementCompletion(live, artifact, scenario));
        Apply(CampaignBreakdownLifecycleFactory.CreateBreakdownCompletion(live, artifact, scenario,
            CampaignBreakdownLifecycleFactory.CreateBreakdownCompletionActionId()));
        Assert.Equal(LandStepIds.PositionDetermination, live.CurrentPosition.SequenceContext.StepId);
        Assert.Equal(beforeCompletion.World, live.World);
        Assert.Equal(beforeCompletion.RandomState, live.RandomState);
        Assert.Throws<InvalidOperationException>(() => CampaignBreakdownLifecycleFactory.CreateBreakdownCompletion(
            live, artifact, scenario, CampaignBreakdownLifecycleFactory.CreateBreakdownCompletionActionId()));
        for (var pass = 0; pass < 2; pass++)
        {
            var replay = initial;
            foreach (var bytes in history)
            {
                var decoded = CampaignBreakdownEventSerializer.Deserialize(bytes);
                Assert.Equal(bytes, CampaignBreakdownEventSerializer.Serialize(decoded));
                replay = CampaignV11BreakdownProjector.Apply(replay, decoded, artifact, scenario);
            }
            Assert.Equal(CampaignSnapshotV11Serializer.Serialize(live), CampaignSnapshotV11Serializer.Serialize(replay));
        }
    }

    internal static BreakdownStopResolved Resolve(CampaignSnapshotV11 prior) => CampaignBreakdownStopResolvedFactory.Create(
        prior, BreakdownContentFixture.Artifact(), BreakdownSnapshotTests.Scenario(), CampaignBreakdownLifecycleFactory.CreateResolveActionId(prior));

    internal static CampaignSnapshotV11 PendingTruck(ulong seed = 0)
    {
        var original = BreakdownSnapshotTests.Create(true);
        var prior = new CampaignSnapshotV11(11, original.CampaignId, original.StateVersion, original.RulesetHash,
            original.Setup, original.World, original.InitiativeHolder, original.OperationStageOrders, original.OperationStageWeather,
            SandtableRandom.Create(seed), original.CurrentPosition, null, original.BreakdownFlow);
        var moved = BreakdownMovementTests.Move(prior, "axis-truck", "west", "center");
        prior = CampaignV11MoveProjector.ApplyMovement(prior, moved, BreakdownContentFixture.Artifact(), BreakdownSnapshotTests.Scenario());
        return RecordStop(prior);
    }
    private static CampaignSnapshotV11 RecordStop(CampaignSnapshotV11 prior)
    {
        var route = ((CampaignBreakdownFlow.Moving)prior.BreakdownFlow).Route;
        var state = prior.World.Elements.Single(x => x.ElementId == route.ElementId).OperationalState.VehicleBreakdownState;
        var cohort = BreakdownContentFixture.Artifact().Definition.LegacyDefinition.Elements.Single(x => x.ElementId == route.ElementId).BreakdownVehicleCohort;
        CampaignBreakdownCheckInput[] inputs = state is null ? [] : [new(state.CohortId, cohort!.VehicleTypeId,
            cohort.ProfileId, state.WorkingPointCount, state.CumulativeBreakdownPoints, state.SandstormAttributedBreakdownPoints, state.HighestEffectiveCheckedBandId)];
        var stop = CampaignBreakdownStop.Create(prior.CampaignId, prior.RulesetHash, prior.StateVersion + 1, route,
            CampaignBreakdownStopReason.Deliberate, BreakdownWeatherKind.Normal, inputs);
        return new CampaignSnapshotV11(11, prior.CampaignId, prior.StateVersion + 1, prior.RulesetHash, prior.Setup,
            prior.World, prior.InitiativeHolder, prior.OperationStageOrders, prior.OperationStageWeather, prior.RandomState,
            CampaignPositionV11.FromBreakdownStop(prior.CurrentPosition.SequenceContext), null, new CampaignBreakdownFlow.PhasingStop(stop));
    }
}
