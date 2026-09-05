using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Observations;

[Trait("Boundary", "UserSpace")]
public sealed class CampaignObservationV7ContractTests
{
    [Fact]
    public void NormalWirePinsProfilePositionAndExplicitNullRoute()
    {
        var observation = Project(BreakdownSnapshotTests.Create(true), LandSide.Axis);
        var bytes = CampaignObservationV7Serializer.SerializeCanonical(observation);
        var root = JsonNode.Parse(bytes)!;
        Assert.Equal(7, root["contractVersion"]!.GetValue<int>());
        Assert.Equal("sandtable.observation.breakdown-side-safe.v1", root["policyId"]!.GetValue<string>());
        Assert.Equal("sandtable.capability.breakdown-truck-battalion.v1", root["capabilityProfileId"]!.GetValue<string>());
        Assert.Equal("{\"kind\":\"normal\",\"activeMovement\":null}", root["decisionState"]!.ToJsonString());
        var properties = root.AsObject().Select(value => value.Key).ToArray();
        Assert.Equal("capabilityProfileId", properties[Array.IndexOf(properties, "scenarioId") + 1]);
        Assert.Equal("ownBrokenVehicleLots", properties[^1]);
        Assert.Equal(observation, CampaignObservationV7Serializer.DeserializeCanonical(bytes));
        Assert.Throws<JsonException>(() => CampaignObservationV6Serializer.DeserializeCanonical(bytes));
    }

    [Fact]
    public void OpenRouteExposesExactStateScopedPublicHandleOnlyToPhasingOwner()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var moved = BreakdownMovementTests.Move(prior, "axis-truck", "west", "center");
        var snapshot = CampaignV11MoveProjector.ApplyMovement(prior, moved,
            BreakdownContentFixture.Artifact(), BreakdownSnapshotTests.Scenario());
        var owner = Project(snapshot, LandSide.Axis);
        var route = Assert.IsType<CampaignObservationV7NormalDecisionState>(owner.DecisionState).ActiveMovement;
        Assert.NotNull(route);
        Assert.Equal(CampaignBreakdownLifecycleFactory.CreateRouteCapability(snapshot), route.RouteId);
        Assert.NotEqual(((CampaignBreakdownFlow.Moving)snapshot.BreakdownFlow).Route.RouteId, route.RouteId);
        Assert.Equal("axis-truck", route.ElementId);
        Assert.Equal("west", route.OriginLocationId);
        Assert.Equal("center", route.CurrentLocationId);
        Assert.Null(Assert.IsType<CampaignObservationV7NormalDecisionState>(Project(snapshot, LandSide.Commonwealth).DecisionState).ActiveMovement);
        Assert.Equal(owner, CampaignObservationV7Serializer.DeserializeCanonical(CampaignObservationV7Serializer.SerializeCanonical(owner)));
    }

    [Fact]
    public void PendingStopPublishesGenericSystemWaitingAndSuppressesReactorOwnerRows()
    {
        var snapshot = BreakdownResolutionReplayTests.PendingTruck();
        foreach (var side in new[] { LandSide.Axis, LandSide.Commonwealth })
        {
            var observation = Project(snapshot, side);
            Assert.IsType<CampaignObservationV7BreakdownWaitingDecisionState>(observation.DecisionState);
            Assert.Equal("land.position.breakdown-stop", observation.Position.PositionId);
            Assert.Equal(LandActorRole.None, observation.Position.ActorRole);
            Assert.Null(observation.Position.ActiveSide);
            var wire = Encoding.UTF8.GetString(CampaignObservationV7Serializer.SerializeCanonical(observation));
            Assert.Contains("\"decisionState\":{\"kind\":\"breakdown-waiting\"}", wire, StringComparison.Ordinal);
            Assert.DoesNotContain("stopId", wire, StringComparison.Ordinal);
            Assert.DoesNotContain("routeId", wire, StringComparison.Ordinal);
            Assert.DoesNotContain("random", wire, StringComparison.Ordinal);
            if (side == LandSide.Axis) Assert.NotEmpty(observation.OwnElements);
            else { Assert.Empty(observation.OwnElements); Assert.Empty(observation.OwnBrokenVehicleLots); Assert.Empty(observation.MovementEndedElementIds); }
        }
    }

    [Fact]
    public void ResolvedLotsExposeOnlyOwnCohortLocationAndAggregateCount()
    {
        var pending = BreakdownResolutionReplayTests.PendingTruck();
        var resolved = BreakdownResolutionReplayTests.Resolve(pending);
        var snapshot = CampaignV11BreakdownProjector.Apply(pending, resolved,
            BreakdownContentFixture.Artifact(), BreakdownSnapshotTests.Scenario());
        var observation = Project(snapshot, LandSide.Axis);
        var lot = Assert.Single(observation.OwnBrokenVehicleLots);
        Assert.Equal("center", lot.LocationId);
        Assert.Equal(Assert.Single(snapshot.World.BrokenVehicleLots).PointCount, lot.PointCount);
        Assert.Empty(Project(snapshot, LandSide.Commonwealth).OwnBrokenVehicleLots);
        var wire = Encoding.UTF8.GetString(CampaignObservationV7Serializer.SerializeCanonical(observation));
        Assert.DoesNotContain("lotId", wire, StringComparison.Ordinal);
        Assert.DoesNotContain("stopId", wire, StringComparison.Ordinal);
        Assert.DoesNotContain("checkId", wire, StringComparison.Ordinal);
        Assert.Equal(observation, CampaignObservationV7Serializer.DeserializeCanonical(Encoding.UTF8.GetBytes(wire)));
    }

    [Fact]
    public void CanonicalReaderRejectsLotAggregateThatDisagreesWithOwnBrokenCount()
    {
        var pending = BreakdownResolutionReplayTests.PendingTruck();
        var snapshot = CampaignV11BreakdownProjector.Apply(pending, BreakdownResolutionReplayTests.Resolve(pending),
            BreakdownContentFixture.Artifact(), BreakdownSnapshotTests.Scenario());
        var root = JsonNode.Parse(CampaignObservationV7Serializer.SerializeCanonical(Project(snapshot, LandSide.Axis)))!;
        root["ownBrokenVehicleLots"]![0]!["pointCount"] = root["ownBrokenVehicleLots"]![0]!["pointCount"]!.GetValue<int>() + 1;
        Assert.Throws<JsonException>(() => CampaignObservationV7Serializer.DeserializeCanonical(Encoding.UTF8.GetBytes(root.ToJsonString())));
    }

    [Fact]
    public void History2UsesSameDecisionClosureAndRejectsPredecessorReader()
    {
        var observation = Project(BreakdownResolutionReplayTests.PendingTruck(), LandSide.Commonwealth);
        var entry = CampaignProjectedDecisionHistoryV2.Project(observation);
        var bytes = CampaignProjectedDecisionHistoryV2Serializer.SerializeCanonical(entry);
        Assert.Equal(2, entry.ContractVersion);
        Assert.Equal(entry, CampaignProjectedDecisionHistoryV2Serializer.DeserializeCanonical(bytes));
        Assert.Same(observation.DecisionState, entry.DecisionState);
        Assert.Throws<JsonException>(() => CampaignProjectedDecisionHistorySerializer.DeserializeCanonical(bytes));
        Assert.Equal(["contractVersion", "campaignId", "stateVersion", "observer", "decisionState"],
            JsonNode.Parse(bytes)!.AsObject().Select(value => value.Key));
    }

    [Theory]
    [InlineData("profile")]
    [InlineData("missing-route")]
    [InlineData("unknown-root")]
    [InlineData("waiting-data")]
    [InlineData("history-version")]
    public void StrictReaderRejectsMixedOrExpandedOutwardWire(string mutation)
    {
        var root = JsonNode.Parse(CampaignObservationV7Serializer.SerializeCanonical(
            Project(BreakdownSnapshotTests.Create(true), LandSide.Axis)))!;
        switch (mutation)
        {
            case "profile": root["capabilityProfileId"] = "unapproved"; break;
            case "missing-route": root["decisionState"]!.AsObject().Remove("activeMovement"); break;
            case "unknown-root": root["breakdownFlow"] = new JsonObject(); break;
            case "waiting-data": root["decisionState"] = new JsonObject { ["kind"] = "breakdown-waiting", ["stopId"] = CampaignBreakdownCodec.EmptyHash }; break;
            case "history-version": root["contractVersion"] = 6; break;
        }
        Assert.Throws<JsonException>(() => CampaignObservationV7Serializer.DeserializeCanonical(Encoding.UTF8.GetBytes(root.ToJsonString())));
    }

    [Fact]
    public void ReactionStatesPreservePredecessorShapeAndSuppressRawRowsThroughClosedStop()
    {
        var (prior, window, phasing, reactor) = ReactionFixture();
        var reacting = BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.Reacting(phasing, reactor),
            CampaignPositionV11.FromReaction(window.ReactingPosition), window);
        Assert.IsType<CampaignObservationV7PhasingWaitingDecisionState>(Project(reacting, LandSide.Axis).DecisionState);
        var owner = Project(reacting, LandSide.Commonwealth);
        var decision = Assert.IsType<CampaignObservationV7ReactingDecisionState>(owner.DecisionState);
        Assert.NotNull(decision.ActiveParticipant);
        Assert.Empty(owner.OwnElements);
        Assert.Empty(owner.OwnBrokenVehicleLots);
        Assert.Equal(owner, CampaignObservationV7Serializer.DeserializeCanonical(CampaignObservationV7Serializer.SerializeCanonical(owner)));
        var stop = CampaignBreakdownStop.Create(prior.CampaignId, prior.RulesetHash, prior.StateVersion,
            reactor, CampaignBreakdownStopReason.ReactionCompleted, BreakdownWeatherKind.Normal, []);
        var completedWindow = new CampaignReactionWindow(window.WindowId, window.TriggerCommittedStateVersion,
            window.PhasingSide, window.ReactingSide, window.ReactingPosition, window.TriggerAuthority,
            window.ApparentTrigger, window.FrozenOpportunities, [window.ActiveOpportunityId!], null);
        var open = BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.ReactorStopOpen(phasing, stop),
            CampaignPositionV11.FromBreakdownStop(prior.CurrentPosition.SequenceContext), completedWindow);
        byte[]? reference = null;
        foreach (var reason in new[] { CampaignBreakdownStopReason.ReactionTimeout, CampaignBreakdownStopReason.ReactionUnavailable })
        {
            var forced = CampaignBreakdownStop.Create(prior.CampaignId, prior.RulesetHash, prior.StateVersion,
                reactor, reason, BreakdownWeatherKind.Normal, []);
            var closed = BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.ReactorStopClosed(phasing, forced),
                CampaignPositionV11.FromBreakdownStop(prior.CurrentPosition.SequenceContext));
            foreach (var pending in new[] { open, closed })
            {
                var observation = Project(pending, LandSide.Commonwealth);
                Assert.IsType<CampaignObservationV7BreakdownWaitingDecisionState>(observation.DecisionState);
                Assert.Empty(observation.OwnElements); Assert.Empty(observation.OwnBrokenVehicleLots);
                Assert.Empty(observation.MovementEndedElementIds);
                Assert.NotEmpty(Project(pending, LandSide.Axis).OwnElements);
                var bytes = CampaignObservationV7Serializer.SerializeCanonical(observation);
                if (reference is not null) Assert.Equal(reference, bytes);
                reference = bytes;
            }
        }
    }

    [Fact]
    public void HiddenRouteAgeDoesNotChangePublicObservationOrHistory()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var moved = BreakdownMovementTests.Move(prior, "axis-truck", "west", "center");
        var snapshot = CampaignV11MoveProjector.ApplyMovement(prior, moved,
            BreakdownContentFixture.Artifact(), BreakdownSnapshotTests.Scenario());
        var route = Assert.IsType<CampaignBreakdownFlow.Moving>(snapshot.BreakdownFlow).Route;
        var alternate = CampaignBreakdownRoute.Create(snapshot.CampaignId, snapshot.RulesetHash, route.FirstMoveStateVersion - 1,
            route.ElementId, route.RepresentationId, route.Owner, route.OriginLocationId, route.CurrentLocationId, route.CohortIds);
        var permuted = BreakdownSnapshotTests.Copy(snapshot, new CampaignBreakdownFlow.Moving(alternate));
        Assert.NotEqual(route.RouteId, alternate.RouteId);
        foreach (var side in new[] { LandSide.Axis, LandSide.Commonwealth })
        {
            Assert.Equal(CampaignObservationV7Serializer.SerializeCanonical(Project(snapshot, side)),
                CampaignObservationV7Serializer.SerializeCanonical(Project(permuted, side)));
            Assert.Equal(CampaignProjectedDecisionHistoryV2Serializer.SerializeCanonical(CampaignProjectedDecisionHistoryV2.Project(Project(snapshot, side))),
                CampaignProjectedDecisionHistoryV2Serializer.SerializeCanonical(CampaignProjectedDecisionHistoryV2.Project(Project(permuted, side))));
        }
    }

    [Fact]
    public void LotIdentityPartitionDoesNotChangeAggregatedPublicOutput()
    {
        var pending = BreakdownResolutionReplayTests.PendingTruck();
        var snapshot = CampaignV11BreakdownProjector.Apply(pending, BreakdownResolutionReplayTests.Resolve(pending),
            BreakdownContentFixture.Artifact(), BreakdownSnapshotTests.Scenario());
        var original = Assert.Single(snapshot.World.BrokenVehicleLots);
        Assert.True(original.PointCount > 1);
        CampaignBrokenVehicleLot Lot(string stopId, int count) => CampaignBrokenVehicleLot.Create(snapshot.CampaignId,
            snapshot.RulesetHash, stopId, original.Owner, original.CohortId, original.VehicleTypeId, count,
            original.LocationId, original.CreatedStateVersion, original.Sources);
        var world = new CampaignWorldSnapshotV6(6, snapshot.World.Elements, snapshot.World.Representations,
            [Lot(original.StopId, 1), Lot(CampaignBreakdownCodec.EmptyHash, original.PointCount - 1)]);
        var permuted = BreakdownSnapshotTests.Copy(snapshot, snapshot.BreakdownFlow, world: world);
        foreach (var side in new[] { LandSide.Axis, LandSide.Commonwealth })
            Assert.Equal(CampaignObservationV7Serializer.SerializeCanonical(Project(snapshot, side)),
                CampaignObservationV7Serializer.SerializeCanonical(Project(permuted, side)));
    }

    internal static (CampaignSnapshotV11 Prior, CampaignReactionWindow Window, CampaignPhasingContinuation Phasing,
        CampaignBreakdownRoute Reactor) ReactionFixture()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var positionedWorld = ChangeOperational(ChangeOperational(prior.World, "axis-infantry", location: "west"),
            "commonwealth-infantry", location: "south-west");
        prior = BreakdownSnapshotTests.Copy(prior, prior.BreakdownFlow, world: positionedWorld);
        var phasingRepresentationBefore = prior.World.Representations.Single(value => value.BoundElementIds.Contains("axis-infantry"));
        var phasingRoute = CampaignBreakdownRoute.Create(prior.CampaignId, prior.RulesetHash, 10, "axis-infantry",
            phasingRepresentationBefore.RepresentationId, LandSide.Axis, "north-west", "west", []);
        var phasingRepresentation = prior.World.Representations.Single(value => value.BoundElementIds.Contains("axis-infantry"));
        var reactorRepresentation = prior.World.Representations.Single(value => value.BoundElementIds.Contains("commonwealth-infantry"));
        var windowId = CampaignReactionIdentity.CreateWindow(prior.CampaignId, prior.RulesetHash, 3, 10,
            phasingRepresentation, phasingRoute.OriginLocationId, phasingRoute.CurrentLocationId, LandSide.Commonwealth);
        var opportunity = new CampaignFrozenReactionOpportunity(CampaignReactionIdentity.CreateOpportunity(windowId, reactorRepresentation),
            reactorRepresentation, new CampaignReactionAdjacencyEvidence(reactorRepresentation.CurrentLocationId,
                phasingRoute.CurrentLocationId, true, [new RuleReference("spi-1979-land-rules", "8.51")]));
        var window = new CampaignReactionWindow(windowId, 10, LandSide.Axis, LandSide.Commonwealth,
            CampaignReactingPosition.CreateForBreakdown(prior.CurrentPosition.SequenceContext),
            CampaignReactionTriggerAuthority.CreateForBreakdown(phasingRoute.ElementId, phasingRepresentation,
                phasingRoute.OriginLocationId, phasingRoute.CurrentLocationId),
            new CampaignApparentReactionTrigger("apparent-trigger", phasingRoute.OriginLocationId, phasingRoute.CurrentLocationId),
            [opportunity], [], opportunity.OpportunityId);
        var reactor = CampaignBreakdownRoute.Create(prior.CampaignId, prior.RulesetHash, 11, "commonwealth-infantry",
            reactorRepresentation.RepresentationId, LandSide.Commonwealth, reactorRepresentation.CurrentLocationId,
            "south", []);
        var world = ChangeOperational(prior.World, "commonwealth-infantry", spent: new CapabilityPointAmount(BreakdownContentFixture.Artifact().Definition.LegacyDefinition.Elements
            .Single(value => value.ElementId == "commonwealth-infantry").BaseCapabilityPointAllowance, 1), location: "south");
        prior = new CampaignSnapshotV11(11, prior.CampaignId, prior.StateVersion, prior.RulesetHash, prior.Setup, world,
            prior.InitiativeHolder, prior.OperationStageOrders, prior.OperationStageWeather, prior.RandomState, prior.CurrentPosition, null,
            new CampaignBreakdownFlow.Idle());
        return (prior, window, new CampaignPhasingContinuation.ResumeRoute(phasingRoute), reactor);
    }
    private static CampaignWorldSnapshotV6 ChangeOperational(CampaignWorldSnapshotV6 world, string elementId,
        CampaignMovementEndedState? ended = null, CapabilityPointAmount? spent = null, string? location = null) => new(6,
        world.Elements.Select(value => value.ElementId != elementId ? value : new CampaignElementStateV5(value.ElementId,
            location ?? value.CurrentLocationId, value.ReserveStatus, new CampaignElementOperationalStateV5(value.OperationalState.LedgerGameTurn,
                value.OperationalState.LedgerOperationStage, spent ?? value.OperationalState.CapabilityPointsExpended,
                value.OperationalState.CohesionLevel, value.OperationalState.VehicleBreakdownState, ended), value.Components)),
        world.Representations.Select(value => location is not null && value.BoundElementIds.Contains(elementId)
            ? new CampaignMapRepresentationState(value.RepresentationId, location, value.BindingKind, value.BoundElementIds) : value), world.BrokenVehicleLots);

    internal static CampaignObservationV7 Project(CampaignSnapshotV11 snapshot, LandSide observer) =>
        CampaignObservationV7Projector.Project(snapshot, BreakdownContentFixture.Artifact(),
            BreakdownSnapshotTests.Scenario(), observer, new CampaignObservationV6AuthorityFacts([], []));
}
