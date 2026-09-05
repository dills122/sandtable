using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class BreakdownMoveReplayTests
{
    [Fact]
    public void TruckMoveStrictReadbackAndReplayPreserveRngAndLots()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var moved = CampaignElementMovedV3Factory.Create(prior, artifact, scenario, Ordinary(prior, "axis-truck", "west", "center"));
        var bytes = CampaignBreakdownMoveEventSerializer.Serialize(moved);
        var parsed = Assert.IsType<ElementMovedV3>(CampaignBreakdownMoveEventSerializer.Deserialize(bytes));
        Assert.Equal(moved, parsed);
        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(bytes));
        var after = CampaignV11MoveProjector.ApplyMovement(prior, parsed, artifact, scenario);
        Assert.Equal(prior.StateVersion + 1, after.StateVersion);
        Assert.Equal(prior.RandomState, after.RandomState);
        Assert.Equal(prior.World.BrokenVehicleLots, after.World.BrokenVehicleLots);
        Assert.Equal(moved.BreakdownAccounting.Single().After,
            after.World.Elements.Single(x => x.ElementId == "axis-truck").OperationalState.VehicleBreakdownState!.CumulativeBreakdownPoints);
        Assert.Equal(after, CampaignSnapshotV11Serializer.Deserialize(CampaignSnapshotV11Serializer.Serialize(after, artifact, scenario), artifact, scenario));
    }

    [Theory]
    [InlineData("delta")]
    [InlineData("consistent-delta")]
    [InlineData("sources")]
    [InlineData("weather")]
    [InlineData("missing-accounting")]
    [InlineData("route")]
    [InlineData("rules")]
    [InlineData("version")]
    [InlineData("unknown")]
    public void ForgedMovementCannotPatchAuthority(string mutation)
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var before = CampaignSnapshotV11Serializer.Serialize(prior);
        var moved = CampaignElementMovedV3Factory.Create(prior, artifact, scenario, Ordinary(prior, "axis-truck", "west", "center"));
        var node = JsonNode.Parse(CampaignBreakdownMoveEventSerializer.Serialize(moved))!;
        switch (mutation)
        {
            case "delta": node["breakdownAccounting"]![0]!["delta"]!["numerator"] = 9; break;
            case "consistent-delta": node["breakdownAccounting"]![0]!["delta"]!["numerator"] = 9; node["breakdownAccounting"]![0]!["after"]!["numerator"] = 9; break;
            case "sources": node["breakdownAccounting"]![0]!["sources"]!.AsArray().Last()!["locator"] = "zzzz-counterfeit"; break;
            case "weather": node["breakdownAccounting"]![0]!["weatherKind"] = "hot"; break;
            case "missing-accounting": node["breakdownAccounting"] = new JsonArray(); break;
            case "route": node["breakdownFlowAfter"]!["route"]!["originLocationId"] = "east"; break;
            case "rules": node["rulesetHash"] = Cna1979Ruleset.Manifest.Hash; break;
            case "version": node["contractVersion"] = 2; break;
            case "unknown": node["counterfeit"] = true; break;
        }
        if (mutation is "consistent-delta" or "sources" or "weather" or "missing-accounting")
        {
            var forged = Assert.IsType<ElementMovedV3>(CampaignBreakdownMoveEventSerializer.Deserialize(
                Encoding.UTF8.GetBytes(node.ToJsonString(new JsonSerializerOptions { WriteIndented = false }))));
            Assert.Throws<InvalidCampaignHistoryException>(() => CampaignV11MoveProjector.ApplyMovement(prior, forged, artifact, scenario));
            Assert.Equal(before, CampaignSnapshotV11Serializer.Serialize(prior));
            return;
        }
        Assert.ThrowsAny<Exception>(() => CampaignV11MoveProjector.ApplyMovement(prior,
            Assert.IsType<ElementMovedV3>(CampaignBreakdownMoveEventSerializer.Deserialize(Encoding.UTF8.GetBytes(node.ToJsonString(new JsonSerializerOptions { WriteIndented = false })))), artifact, scenario));
        Assert.Equal(before, CampaignSnapshotV11Serializer.Serialize(prior));
    }

    [Fact]
    public void ReactionMovesRetainPhasingRouteAndActiveEpisodeWithoutNestedWindow()
    {
        var (prior, artifact, scenario) = Reaction();
        var window = prior.ReactionWindow!;
        var input = CampaignReactingElementMovedV2Factory.CreateReplayInput(prior, artifact, scenario, window.FrozenOpportunities.Single().OpportunityId, "south");
        var moved = CampaignReactingElementMovedV2Factory.Create(prior, artifact, scenario, input);
        var after = CampaignV11MoveProjector.ApplyReactionMove(prior, moved, artifact, scenario);
        var parsed = Assert.IsType<ReactingElementMovedV2>(CampaignBreakdownMoveEventSerializer.Deserialize(CampaignBreakdownMoveEventSerializer.Serialize(moved)));
        Assert.Equal(moved, parsed);
        Assert.Equal(window.WindowId, after.ReactionWindow!.WindowId);
        Assert.Equal(input.OpportunityId, after.ReactionWindow.ActiveOpportunityId);
        Assert.Equal(((CampaignBreakdownFlow.Reacting)prior.BreakdownFlow).PhasingContinuation,
            ((CampaignBreakdownFlow.Reacting)after.BreakdownFlow).PhasingContinuation);
        var route = ((CampaignBreakdownFlow.Reacting)after.BreakdownFlow).ReactorRoute!;
        Assert.Equal("south-west", route.OriginLocationId);
        Assert.Equal("south", route.CurrentLocationId);
        Assert.Empty(moved.BreakdownAccounting);
        Assert.Equal(prior.RandomState, after.RandomState);
        Assert.ThrowsAny<Exception>(() => CampaignReactingElementMovedV2Factory.Create(after, artifact, scenario, input));
    }

    [Fact]
    public void OrdinaryAndDormantReactionAccountingSeamsReturnIdenticalCohortEvidence()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var artifact = BreakdownContentFixture.Artifact();
        var definition = artifact.Definition.LegacyDefinition;
        var moved = CampaignElementMovedV3Factory.Create(prior, artifact, BreakdownSnapshotTests.Scenario(), Ordinary(prior, "axis-truck", "west", "center"));
        var edge = CampaignElementMovedV2Factory.FindEdge(definition, "west", "center")!;
        var accounting = CampaignReactingElementMovedV2Factory.CalculateAccounting(prior, artifact,
            definition.Elements.Single(x => x.ElementId == "axis-truck"), prior.World.Elements.Single(x => x.ElementId == "axis-truck"),
            edge, definition.Locations.Single(x => x.LocationId == "center"));
        Assert.Equal(moved.BreakdownAccounting, accounting);
        Assert.Equal(new BreakdownPointAmount(26, 1), accounting.Single().Delta);
    }

    [Fact]
    public void FinalCpReactionMoveKeepsActiveRouteAndCannotMoveAgain()
    {
        var (prior, artifact, scenario) = Reaction();
        var opportunity = prior.ReactionWindow!.FrozenOpportunities.Single().OpportunityId;
        var initialInput = CampaignReactingElementMovedV2Factory.CreateReplayInput(prior, artifact, scenario, opportunity, "south");
        var cost = CampaignReactingElementMovedV2Factory.Create(prior, artifact, scenario, initialInput).Cost.TotalCost;
        var allowance = artifact.Definition.LegacyDefinition.Elements.Single(x => x.ElementId == "commonwealth-infantry").BaseCapabilityPointAllowance;
        var world = new CampaignWorldSnapshotV6(6, prior.World.Elements.Select(x => x.ElementId != "commonwealth-infantry" ? x :
            new CampaignElementStateV5(x.ElementId, x.CurrentLocationId, x.ReserveStatus,
                new CampaignElementOperationalStateV5(x.OperationalState.LedgerGameTurn, x.OperationalState.LedgerOperationStage,
                    new CapabilityPointAmount(allowance * cost.Denominator - cost.Numerator, cost.Denominator),
                    x.OperationalState.CohesionLevel, x.OperationalState.VehicleBreakdownState, x.OperationalState.MovementEnded), x.Components)),
            prior.World.Representations, prior.World.BrokenVehicleLots);
        prior = BreakdownSnapshotTests.Copy(prior, prior.BreakdownFlow, window: prior.ReactionWindow, world: world);
        var input = CampaignReactingElementMovedV2Factory.CreateReplayInput(prior, artifact, scenario, opportunity, "south");
        var moved = CampaignReactingElementMovedV2Factory.Create(prior, artifact, scenario, input);
        var after = CampaignV11MoveProjector.ApplyReactionMove(prior, moved, artifact, scenario);
        Assert.Equal(new CapabilityPointAmount(allowance, 1), moved.CapabilityPointsExpendedAfter);
        Assert.IsType<CampaignBreakdownFlow.Reacting>(after.BreakdownFlow);
        Assert.Equal(opportunity, after.ReactionWindow!.ActiveOpportunityId);
        Assert.Throws<InvalidOperationException>(() => CampaignReactingElementMovedV2Factory.CreateReplayInput(after, artifact, scenario, opportunity, "south-east"));
        Assert.Equal(prior.RandomState, after.RandomState);
    }

    [Theory]
    [InlineData("action")]
    [InlineData("alias")]
    [InlineData("state")]
    [InlineData("side")]
    [InlineData("origin")]
    public void ReactionRejectsForgedOrStaleCapability(string field)
    {
        var (prior, artifact, scenario) = Reaction();
        var input = CampaignReactingElementMovedV2Factory.CreateReplayInput(prior, artifact, scenario,
            prior.ReactionWindow!.FrozenOpportunities.Single().OpportunityId, "south");
        input = field switch
        {
            "action" => input with { ActionId = CampaignBreakdownCodec.EmptyHash },
            "alias" => input with { SubmittedOpportunityId = CampaignBreakdownCodec.EmptyHash },
            "state" => input with { PriorStateVersion = input.PriorStateVersion - 1 },
            "side" => input with { ActingSide = LandSide.Axis },
            _ => input with { OriginLocationId = "east" },
        };
        Assert.Throws<InvalidOperationException>(() => CampaignReactingElementMovedV2Factory.Create(prior, artifact, scenario, input));
    }

    [Fact]
    public void SurvivorMovementPreservesLotsCountsAndCheckedBandMemory()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var truck = prior.World.Elements.Single(x => x.ElementId == "axis-truck");
        var cohort = artifact.Definition.LegacyDefinition.Elements.Single(x => x.ElementId == truck.ElementId).BreakdownVehicleCohort!;
        var lot = CampaignBrokenVehicleLot.Create(prior.CampaignId, prior.RulesetHash, CampaignBreakdownCodec.EmptyHash,
            LandSide.Axis, cohort.CohortId, cohort.VehicleTypeId, 2, "west", 10, [new RuleReference("spi-1979-land-rules", "21.41")]);
        var world = BreakdownWorldTests.WithTruck(prior.World, truck, "west", 10, 2, [lot]);
        prior = BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.Idle(), world: world);
        var moved = CampaignElementMovedV3Factory.Create(prior, artifact, scenario, Ordinary(prior, "axis-truck", "west", "center"));
        var after = CampaignV11MoveProjector.ApplyMovement(prior, moved, artifact, scenario);
        var ledger = after.World.Elements.Single(x => x.ElementId == truck.ElementId).OperationalState.VehicleBreakdownState!;
        var before = prior.World.Elements.Single(x => x.ElementId == truck.ElementId).OperationalState.VehicleBreakdownState!;
        Assert.Equal(before.WorkingPointCount, ledger.WorkingPointCount);
        Assert.Equal(before.BrokenPointCount, ledger.BrokenPointCount);
        Assert.Equal(before.HighestEffectiveCheckedBandId, ledger.HighestEffectiveCheckedBandId);
        Assert.Equal(lot, Assert.Single(after.World.BrokenVehicleLots));
        Assert.Equal("west", after.World.BrokenVehicleLots[0].LocationId);
        Assert.Equal("center", after.World.Elements.Single(x => x.ElementId == truck.ElementId).CurrentLocationId);
        Assert.Equal(prior.RandomState, after.RandomState);
    }

    internal static ElementMovedV3ReplayInput Ordinary(CampaignSnapshotV11 prior, string element, string origin, string destination) =>
        new(prior.CampaignId, prior.StateVersion, prior.CurrentPosition.SequenceContext.PositionId, LandSide.Axis, element, origin, destination);

    internal static (CampaignSnapshotV11 Prior, ContentPackV6Artifact Artifact, ContentScenario Scenario) Reaction()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var world = Relocate(prior.World, "commonwealth-infantry", "south-west");
        prior = BreakdownSnapshotTests.Copy(prior, prior.BreakdownFlow, world: world);
        var moved = CampaignElementMovedV3Factory.Create(prior, artifact, scenario, Ordinary(prior, "axis-infantry", "north-west", "west"));
        return (CampaignV11MoveProjector.ApplyMovement(prior, moved, artifact, scenario), artifact, scenario);
    }

    internal static CampaignWorldSnapshotV6 Relocate(CampaignWorldSnapshotV6 world, string elementId, string location) => new(6,
        world.Elements.Select(x => x.ElementId != elementId ? x : new CampaignElementStateV5(x.ElementId, location, x.ReserveStatus, x.OperationalState, x.Components)),
        world.Representations.Select(x => !x.BoundElementIds.Contains(elementId) ? x : new CampaignMapRepresentationState(x.RepresentationId, location, x.BindingKind, x.BoundElementIds)), world.BrokenVehicleLots);
}
