using Cna.Core.Campaigns;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class BreakdownFlowTests
{
    [Fact]
    public void FinalCpReactionRetainsActiveRouteThenCompletionAndClosureUseDistinctStopShapes()
    {
        var (prior, window, phasing, reactor) = Reaction();
        var active = BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.Reacting(phasing, reactor),
            CampaignPositionV11.FromReaction(window.ReactingPosition), window);
        Assert.True(Certified(active));
        Assert.Equal(active, CampaignSnapshotV11Serializer.Deserialize(CampaignSnapshotV11Serializer.Serialize(active)));
        var stopped = CampaignBreakdownStop.Create(prior.CampaignId, prior.RulesetHash, prior.StateVersion,
            reactor, CampaignBreakdownStopReason.ReactionCompleted, BreakdownWeatherKind.Normal, []);
        var completedWindow = Window(window, [window.ActiveOpportunityId!], null);
        var open = BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.ReactorStopOpen(phasing, stopped),
            CampaignPositionV11.FromBreakdownStop(prior.CurrentPosition.SequenceContext), completedWindow);
        Assert.True(Certified(open));
        Assert.Equal(open, CampaignSnapshotV11Serializer.Deserialize(CampaignSnapshotV11Serializer.Serialize(open)));
        Assert.Throws<ArgumentException>(() => BreakdownSnapshotTests.Copy(prior, open.BreakdownFlow,
            open.CurrentPosition, window));
        foreach (var reason in new[] { CampaignBreakdownStopReason.ReactionTimeout, CampaignBreakdownStopReason.ReactionUnavailable })
        {
            var forced = CampaignBreakdownStop.Create(prior.CampaignId, prior.RulesetHash, prior.StateVersion,
                reactor, reason, BreakdownWeatherKind.Normal, []);
            var closed = BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.ReactorStopClosed(phasing, forced),
                CampaignPositionV11.FromBreakdownStop(prior.CurrentPosition.SequenceContext));
            Assert.True(Certified(closed));
            Assert.Equal(closed, CampaignSnapshotV11Serializer.Deserialize(CampaignSnapshotV11Serializer.Serialize(closed)));
        }
    }

    [Fact]
    public void ForcedPhasingStopCannotResumeAnEndedRoute()
    {
        var (prior, window, phasing, reactor) = Reaction();
        var route = ((CampaignPhasingContinuation.ResumeRoute)phasing).Route;
        var endedWorld = ChangeOperational(prior.World, route.ElementId, ended: CampaignMovementEndedState.CreateForBreakdown(prior.CurrentPosition.SequenceContext));
        Assert.Throws<ArgumentException>(() => BreakdownSnapshotTests.Copy(prior,
            new CampaignBreakdownFlow.Reacting(phasing, reactor), CampaignPositionV11.FromReaction(window.ReactingPosition), window, endedWorld));
        var stop = CampaignBreakdownStop.Create(prior.CampaignId, prior.RulesetHash, window.TriggerCommittedStateVersion,
            route, CampaignBreakdownStopReason.MovementEnded, BreakdownWeatherKind.Normal, []);
        var deferred = BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.Reacting(new CampaignPhasingContinuation.ResolveStop(stop), reactor),
            CampaignPositionV11.FromReaction(window.ReactingPosition), window, endedWorld);
        Assert.True(Certified(deferred));
    }

    [Fact]
    public void RouteDatesMustBracketTriggerCommit()
    {
        var (prior, window, phasing, reactor) = Reaction();
        var route = ((CampaignPhasingContinuation.ResumeRoute)phasing).Route;
        var late = CampaignBreakdownRoute.Create(prior.CampaignId, prior.RulesetHash, 11, route.ElementId,
            route.RepresentationId, route.Owner, route.OriginLocationId, route.CurrentLocationId, route.CohortIds);
        Assert.Throws<ArgumentException>(() => BreakdownSnapshotTests.Copy(prior,
            new CampaignBreakdownFlow.Reacting(new CampaignPhasingContinuation.ResumeRoute(late), reactor),
            CampaignPositionV11.FromReaction(window.ReactingPosition), window));
        var early = CampaignBreakdownRoute.Create(prior.CampaignId, prior.RulesetHash, 9, reactor.ElementId,
            reactor.RepresentationId, reactor.Owner, reactor.OriginLocationId, reactor.CurrentLocationId, reactor.CohortIds);
        Assert.Throws<ArgumentException>(() => BreakdownSnapshotTests.Copy(prior,
            new CampaignBreakdownFlow.Reacting(phasing, early), CampaignPositionV11.FromReaction(window.ReactingPosition), window));
    }

    [Fact]
    public void MovementRequiresRetainedStageWeatherAndStopWeatherMustMatch()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        Assert.Throws<ArgumentException>(() => new CampaignSnapshotV11(11, prior.CampaignId, prior.StateVersion,
            prior.RulesetHash, prior.Setup, prior.World, prior.InitiativeHolder, prior.OperationStageOrders, [],
            prior.RandomState, prior.CurrentPosition, null, prior.BreakdownFlow));
        var route = BreakdownSnapshotTests.Route(prior, "axis-infantry");
        var hot = CampaignBreakdownStop.Create(prior.CampaignId, prior.RulesetHash, 11, route,
            CampaignBreakdownStopReason.Deliberate, BreakdownWeatherKind.Hot, []);
        var forged = BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.PhasingStop(hot),
            CampaignPositionV11.FromBreakdownStop(prior.CurrentPosition.SequenceContext));
        Assert.False(Certified(forged));
    }

    [Fact]
    public void RehashedNonadjacentFrozenOpportunityFailsCertification()
    {
        var (prior, original, phasing, reactor) = Reaction();
        var frozen = original.FrozenOpportunities.Single().ReactingRepresentation;
        var forgedRepresentation = new CampaignMapRepresentationState(frozen.RepresentationId, "north-east", frozen.BindingKind, frozen.BoundElementIds);
        var opportunity = new CampaignFrozenReactionOpportunity(CampaignReactionIdentity.CreateOpportunity(original.WindowId, forgedRepresentation),
            forgedRepresentation, new CampaignReactionAdjacencyEvidence("north-east", original.TriggerAuthority.DestinationLocationId,
                true, [new RuleReference("spi-1979-land-rules", "8.51")]));
        var window = new CampaignReactionWindow(original.WindowId, original.TriggerCommittedStateVersion, original.PhasingSide,
            original.ReactingSide, original.ReactingPosition, original.TriggerAuthority, original.ApparentTrigger, [opportunity], [], opportunity.OpportunityId);
        var route = CampaignBreakdownRoute.Create(prior.CampaignId, prior.RulesetHash, 11, reactor.ElementId, reactor.RepresentationId,
            reactor.Owner, "north-east", reactor.CurrentLocationId, []);
        var snapshot = BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.Reacting(phasing, route),
            CampaignPositionV11.FromReaction(window.ReactingPosition), window);
        Assert.False(Certified(snapshot));
    }

    [Fact]
    public void ExhaustedPhasingMovementRequiresCpStopAndCannotChooseDeliberateReason()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var elementId = "axis-infantry";
        var allowance = BreakdownContentFixture.Artifact().Definition.LegacyDefinition.Elements.Single(value => value.ElementId == elementId).BaseCapabilityPointAllowance;
        var route = BreakdownSnapshotTests.Route(prior, elementId);
        var exhausted = ChangeOperational(prior.World, elementId, spent: new CapabilityPointAmount(allowance, 1));
        var overspent = ChangeOperational(prior.World, elementId, spent: new CapabilityPointAmount(allowance + 1, 1));
        Assert.False(Certified(BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.Idle(), world: overspent)));
        Assert.False(Certified(BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.Moving(route), world: exhausted)));
        var stop = CampaignBreakdownStop.Create(prior.CampaignId, prior.RulesetHash, 11, route,
            CampaignBreakdownStopReason.CpExhausted, BreakdownWeatherKind.Normal, []);
        Assert.True(Certified(BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.PhasingStop(stop),
            CampaignPositionV11.FromBreakdownStop(prior.CurrentPosition.SequenceContext), world: exhausted)));
        Assert.False(Certified(BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.PhasingStop(stop),
            CampaignPositionV11.FromBreakdownStop(prior.CurrentPosition.SequenceContext))));
        var deliberate = CampaignBreakdownStop.Create(prior.CampaignId, prior.RulesetHash, 11, route,
            CampaignBreakdownStopReason.Deliberate, BreakdownWeatherKind.Normal, []);
        Assert.False(Certified(BreakdownSnapshotTests.Copy(prior, new CampaignBreakdownFlow.PhasingStop(deliberate),
            CampaignPositionV11.FromBreakdownStop(prior.CurrentPosition.SequenceContext), world: exhausted)));
    }

    [Theory]
    [InlineData(55, BreakdownWeatherKind.Sandstorm)]
    [InlineData(62, BreakdownWeatherKind.Rainstorm)]
    public void FoulStopWeatherAppliesOnlyInAffectedContentArea(int coordinate, BreakdownWeatherKind expected)
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var artifact = BreakdownContentFixture.Artifact();
        var weather = new CampaignOperationStageWeather(1, 1, 1, LandSide.Axis, Cna1979Weather.GetSeason(1),
            coordinate / 10, coordinate % 10, coordinate == 55 ? WeatherKind.Sandstorm : WeatherKind.Rainstorm,
            WeatherScope.ListedAreas, 1, Cna1979Weather.GetAffectedAreas(1), 0, 0, 0);
        var snapshot = new CampaignSnapshotV11(11, prior.CampaignId, prior.StateVersion, prior.RulesetHash, prior.Setup,
            prior.World, prior.InitiativeHolder, prior.OperationStageOrders, [weather], prior.RandomState, prior.CurrentPosition, null, prior.BreakdownFlow);
        // Printed location die 1 affects areas A/B; fixture center=A and north=C.
        Assert.Equal(expected, CampaignSnapshotV11Validator.ApplicableWeather(snapshot, artifact, "center"));
        Assert.Equal(BreakdownWeatherKind.Normal, CampaignSnapshotV11Validator.ApplicableWeather(snapshot, artifact, "north"));
    }

    private static bool Certified(CampaignSnapshotV11 value) => CampaignSnapshotV11Validator.IsValid(value,
        BreakdownContentFixture.Artifact(), BreakdownSnapshotTests.Scenario());

    private static (CampaignSnapshotV11 Prior, CampaignReactionWindow Window, CampaignPhasingContinuation Phasing,
        CampaignBreakdownRoute Reactor) Reaction()
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
    private static CampaignReactionWindow Window(CampaignReactionWindow prior, IEnumerable<CampaignReactionOpportunityId> resolved,
        CampaignReactionOpportunityId? active) => new(prior.WindowId, prior.TriggerCommittedStateVersion, prior.PhasingSide, prior.ReactingSide, prior.ReactingPosition,
            prior.TriggerAuthority, prior.ApparentTrigger, prior.FrozenOpportunities, resolved, active);
    private static CampaignWorldSnapshotV6 ChangeOperational(CampaignWorldSnapshotV6 world, string elementId,
        CampaignMovementEndedState? ended = null, CapabilityPointAmount? spent = null, string? location = null) => new(6,
        world.Elements.Select(value => value.ElementId != elementId ? value : new CampaignElementStateV5(value.ElementId,
            location ?? value.CurrentLocationId, value.ReserveStatus, new CampaignElementOperationalStateV5(value.OperationalState.LedgerGameTurn,
                value.OperationalState.LedgerOperationStage, spent ?? value.OperationalState.CapabilityPointsExpended,
                value.OperationalState.CohesionLevel, value.OperationalState.VehicleBreakdownState, ended), value.Components)),
        world.Representations.Select(value => location is not null && value.BoundElementIds.Contains(elementId)
            ? new CampaignMapRepresentationState(value.RepresentationId, location, value.BindingKind, value.BoundElementIds) : value), world.BrokenVehicleLots);
}
