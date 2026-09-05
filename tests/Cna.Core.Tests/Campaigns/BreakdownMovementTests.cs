using System.Text;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class BreakdownMovementTests
{
    [Fact]
    public void TruckStepCreatesRouteAndChargesBpWithoutReactionWindow()
    {
        var prior = ChangeElement(BreakdownSnapshotTests.Create(true), "commonwealth-infantry", "east");
        var moved = Move(prior, "axis-truck", "west", "center");
        var route = Assert.IsType<CampaignBreakdownFlow.Moving>(moved.BreakdownFlowAfter).Route;
        Assert.Equal("west", route.OriginLocationId);
        Assert.Equal("center", route.CurrentLocationId);
        Assert.Equal(12, route.FirstMoveStateVersion);
        Assert.Equal(new BreakdownPointAmount(26, 1), Assert.Single(moved.BreakdownAccounting).Delta);
        Assert.Null(moved.OpenedReactionWindow);
        Assert.Equal(new CapabilityPointAmount(8, 1), moved.Cost.TotalCost);
        Assert.Equal(Cna1979BreakdownRuleset.Manifest.Hash, moved.RulesetHash);
    }

    [Fact]
    public void CombatStepCreatesEmptyCohortRoute()
    {
        var moved = Move(BreakdownSnapshotTests.Create(true), "axis-infantry", "north-west", "north");
        Assert.Empty(moved.BreakdownAccounting);
        Assert.Empty(Assert.IsType<CampaignBreakdownFlow.Moving>(moved.BreakdownFlowAfter).Route.CohortIds);
    }

    [Fact]
    public void OpenRouteRejectsSwitchingElement()
    {
        var initial = BreakdownSnapshotTests.Create(true);
        var prior = BreakdownSnapshotTests.Copy(initial,
            new CampaignBreakdownFlow.Moving(BreakdownSnapshotTests.Route(initial, "axis-truck")));
        Assert.Throws<InvalidOperationException>(() => Move(prior, "axis-infantry", "north-west", "north"));
    }

    [Fact]
    public void ContinuingRoutePreservesIdentityAndOriginalOrigin()
    {
        var initial = BreakdownSnapshotTests.Create(true);
        var route = BreakdownSnapshotTests.Route(initial, "axis-truck");
        var prior = BreakdownSnapshotTests.Copy(initial, new CampaignBreakdownFlow.Moving(route));
        var moved = Move(prior, "axis-truck", "west", "center");
        var after = Assert.IsType<CampaignBreakdownFlow.Moving>(moved.BreakdownFlowAfter).Route;
        Assert.Equal(route.RouteId, after.RouteId);
        Assert.Equal(route.OriginLocationId, after.OriginLocationId);
        Assert.Equal(route.FirstMoveStateVersion, after.FirstMoveStateVersion);
    }

    [Fact]
    public void FinalTruckCpCreatesPhasingStopWithPostStepInputs()
    {
        var prior = ChangeElement(BreakdownSnapshotTests.Create(true), "axis-truck", "west", 12);
        var moved = Move(prior, "axis-truck", "west", "center");
        var stop = Assert.IsType<CampaignBreakdownFlow.PhasingStop>(moved.BreakdownFlowAfter).Stop;
        Assert.Equal(CampaignBreakdownStopReason.CpExhausted, stop.Reason);
        Assert.Equal(new CapabilityPointAmount(20, 1), moved.CapabilityPointsExpendedAfter);
        Assert.Equal("center", stop.Route.CurrentLocationId);
        Assert.Equal(12, Assert.Single(stop.CohortInputs).WorkingPointCount);
    }

    [Fact]
    public void FinalCombatCpDefersEmptyCohortStopBehindReactionWindow()
    {
        var prior = ChangeElement(BreakdownSnapshotTests.Create(true), "axis-infantry", "north-west", 9);
        prior = ChangeElement(prior, "commonwealth-infantry", "north-east");
        var moved = Move(prior, "axis-infantry", "north-west", "north");
        var reacting = Assert.IsType<CampaignBreakdownFlow.Reacting>(moved.BreakdownFlowAfter);
        var stop = Assert.IsType<CampaignPhasingContinuation.ResolveStop>(reacting.PhasingContinuation).Stop;
        Assert.Equal(CampaignBreakdownStopReason.CpExhausted, stop.Reason);
        Assert.Empty(stop.CohortInputs);
        Assert.Null(reacting.ReactorRoute);
        Assert.Single(moved.OpenedReactionWindow!.FrozenOpportunities);
        Assert.Equal(3, moved.OpenedReactionWindow.TriggerAuthority.MoveContractVersion);
    }

    [Fact]
    public void ZeroWorkingTruckCannotMoveEvenWithRemainingCp()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var truck = prior.World.Elements.Single(value => value.ElementId == "axis-truck");
        var cohort = BreakdownContentFixture.Artifact().Definition.LegacyDefinition.Elements
            .Single(value => value.ElementId == truck.ElementId).BreakdownVehicleCohort!;
        var lot = CampaignBrokenVehicleLot.Create(prior.CampaignId, prior.RulesetHash, CampaignBreakdownCodec.EmptyHash,
            LandSide.Axis, cohort.CohortId, cohort.VehicleTypeId, 12, "west", 10,
            [new RuleReference("spi-1979-land-rules", "21.41")]);
        var world = BreakdownWorldTests.WithTruck(prior.World, truck, "west", 0, 12, [lot]);
        prior = BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.Idle(), world: world);
        Assert.True(CampaignSnapshotV11Validator.IsValid(prior, BreakdownContentFixture.Artifact(), BreakdownSnapshotTests.Scenario()));
        Assert.Throws<InvalidOperationException>(() => Move(prior, "axis-truck", "west", "center"));
    }

    [Fact]
    public void HeadquartersMoveStillTriggersCombatAdjacencyWindow()
    {
        var root = JsonNode.Parse(BreakdownContentFixture.Bytes())!;
        root["elements"]!.AsArray().Single(value => value!["elementId"]!.GetValue<string>() == "axis-infantry")!
            ["combatClassificationId"] = Cna1979Combat.HeadquartersClassificationId;
        var parsed = ContentPackV6Serializer.Deserialize(Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(parsed.IsSuccess, parsed.Message);
        var artifact = ContentPackV6Artifact.Create(parsed.Definition!);
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var original = ChangeElement(BreakdownSnapshotTests.Create(true), "commonwealth-infantry", "north-east");
        var setup = CampaignSetupSnapshotV6.FromPredecessor(CampaignSetupSnapshot.FromDefinition(Cna1979SetupCatalog.Definitions[0]),
            new CampaignContentV6Selection(artifact.Identity, scenario.ScenarioId));
        var prior = new CampaignSnapshotV11(11, original.CampaignId, original.StateVersion, original.RulesetHash,
            setup, original.World, original.InitiativeHolder, original.OperationStageOrders, original.OperationStageWeather,
            original.RandomState, original.CurrentPosition, null, new CampaignBreakdownFlow.Idle());
        Assert.True(CampaignSnapshotV11Validator.IsValid(prior, artifact, scenario));
        var moved = CampaignElementMovedV3Factory.Create(prior, artifact, scenario,
            new ElementMovedV3ReplayInput(prior.CampaignId, prior.StateVersion, prior.CurrentPosition.SequenceContext.PositionId,
                LandSide.Axis, "axis-infantry", "north-west", "north"));
        Assert.Single(moved.OpenedReactionWindow!.FrozenOpportunities);
    }

    [Theory]
    [InlineData("legacy-ended")]
    [InlineData("idle-flow")]
    [InlineData("reacting-without-window")]
    [InlineData("window-without-reacting")]
    public void OrdinaryEventRejectsMixedSequenceOrImpossibleFlow(string mutation)
    {
        var moved = mutation == "window-without-reacting"
            ? Move(ChangeElement(BreakdownSnapshotTests.Create(true), "commonwealth-infantry", "north-east"),
                "axis-infantry", "north-west", "north")
            : Move(BreakdownSnapshotTests.Create(true), "axis-truck", "west", "center");
        CampaignMovementEndedState? ended = null;
        if (mutation == "legacy-ended")
        {
            var source = moved.SequencePosition;
            ended = new CampaignMovementEndedState(new LandSequencePosition(3, source.PositionId, source.GameTurn,
                source.OperationStage, source.StageId, source.PhaseId, source.SegmentId, source.StepId,
                source.ActorRole, source.ActiveSide, source.Sources));
        }
        CampaignBreakdownFlow flow = mutation switch
        {
            "idle-flow" or "window-without-reacting" => new CampaignBreakdownFlow.Idle(),
            "reacting-without-window" => new CampaignBreakdownFlow.Reacting(
                new CampaignPhasingContinuation.ResumeRoute(Assert.IsType<CampaignBreakdownFlow.Moving>(moved.BreakdownFlowAfter).Route), null),
            _ => moved.BreakdownFlowAfter,
        };
        Assert.Throws<ArgumentException>(() => new ElementMovedV3(moved.CampaignId, moved.StateVersion, moved.PriorStateVersion,
            moved.FromPositionId, moved.GameTurn, moved.OperationStage, moved.ActingSide, moved.ElementId,
            moved.RepresentationId, moved.OriginLocationId, moved.DestinationLocationId, moved.MobilityId,
            moved.MobilitySources, moved.Cost, moved.CapabilityPointsExpendedBefore, moved.CapabilityPointsExpendedAfter,
            moved.CohesionBefore, moved.CohesionAfter, ended, moved.SequencePosition, moved.OpenedReactionWindow,
            moved.RulesetHash, moved.BreakdownAccounting, flow));
    }

    internal static CampaignSnapshotV11 ChangeElement(CampaignSnapshotV11 prior, string elementId,
        string location, int expended = 0)
    {
        var world = new CampaignWorldSnapshotV6(6,
            prior.World.Elements.Select(value => value.ElementId != elementId ? value : new CampaignElementStateV5(
                value.ElementId, location, value.ReserveStatus, new CampaignElementOperationalStateV5(
                    value.OperationalState.LedgerGameTurn, value.OperationalState.LedgerOperationStage,
                    new CapabilityPointAmount(expended, 1), value.OperationalState.CohesionLevel,
                    value.OperationalState.VehicleBreakdownState, value.OperationalState.MovementEnded), value.Components)),
            prior.World.Representations.Select(value => !value.BoundElementIds.Contains(elementId) ? value :
                new CampaignMapRepresentationState(value.RepresentationId, location, value.BindingKind, value.BoundElementIds)),
            prior.World.BrokenVehicleLots);
        return BreakdownSnapshotTests.Copy(prior, prior.BreakdownFlow, world: world);
    }

    internal static ElementMovedV3 Move(CampaignSnapshotV11 prior, string elementId, string origin, string destination) =>
        CampaignElementMovedV3Factory.Create(prior, BreakdownContentFixture.Artifact(), BreakdownSnapshotTests.Scenario(),
            new ElementMovedV3ReplayInput(prior.CampaignId, prior.StateVersion, prior.CurrentPosition.SequenceContext.PositionId,
                LandSide.Axis, elementId, origin, destination));
}
