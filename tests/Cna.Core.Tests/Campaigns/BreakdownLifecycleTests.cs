using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class BreakdownLifecycleTests
{
    [Fact]
    public void DeliberateStopRecordsCurrentRouteAndRejectsAuthorityIdentityAsCapability()
    {
        var prior = Moving();
        var stopped = CampaignBreakdownLifecycleFactory.CreateStop(prior, Artifact, Scenario);
        var stop = Assert.IsType<CampaignBreakdownFlow.PhasingStop>(stopped.BreakdownFlowAfter).Stop;
        Assert.Equal(CampaignBreakdownStopReason.Deliberate, stop.Reason);
        Assert.Equal(prior.StateVersion + 1, stop.RecordedStateVersion);
        Assert.Equal("center", stop.Route.CurrentLocationId);
        Assert.Equal(new BreakdownPointAmount(26, 1), Assert.Single(stop.CohortInputs).CumulativeBreakdownPoints);
        Assert.NotEqual(stop.Route.RouteId, stopped.SubmittedRouteId);
        Assert.Throws<InvalidOperationException>(() => CampaignBreakdownLifecycleFactory.CreateStop(prior, Artifact, Scenario,
            LandSide.Axis, stop.Route.RouteId, stopped.ActionId));
        RoundTrip(stopped);
    }

    [Fact]
    public void MovementCompletionRequiresIdleAndAdvancesOnlyToBreakdown()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var completed = CampaignBreakdownLifecycleFactory.CreateMovementCompletion(prior, Artifact, Scenario);
        Assert.Equal(LandSegmentIds.BreakdownDetermination, completed.SequencePosition.SegmentId);
        Assert.IsType<CampaignBreakdownFlow.Idle>(completed.BreakdownFlowAfter);
        Assert.Throws<InvalidOperationException>(() => CampaignBreakdownLifecycleFactory.CreateMovementCompletion(Moving(), Artifact, Scenario));
        RoundTrip(completed);
    }

    [Fact]
    public void ActiveReactionCompletionRecordsOpenStopAndClearsParticipant()
    {
        var prior = ActiveReaction();
        var input = CampaignBreakdownLifecycleFactory.CreateReactionCompletionInput(prior, Artifact, Scenario);
        var completed = CampaignBreakdownLifecycleFactory.CreateReactionCompletion(prior, Artifact, Scenario, input);
        var flow = Assert.IsType<CampaignBreakdownFlow.ReactorStopOpen>(completed.BreakdownFlowAfter);
        Assert.Empty(flow.Stop.CohortInputs);
        Assert.Equal(CampaignBreakdownStopReason.ReactionCompleted, flow.Stop.Reason);
        Assert.Null(completed.ReactionWindowAfter.ActiveOpportunityId);
        Assert.Contains(input.OpportunityId, completed.ReactionWindowAfter.ResolvedOpportunityIds);
        Assert.Throws<InvalidOperationException>(() => CampaignBreakdownLifecycleFactory.CreateReactionCompletion(prior, Artifact, Scenario,
            input with { SubmittedOpportunityId = CampaignBreakdownCodec.EmptyHash }));
        RoundTrip(completed);
    }

    [Theory]
    [InlineData(3, 5)]
    [InlineData(2, 4)]
    public void ActiveFallbackRecordsClosedStop(int reasonValue, int stopReasonValue)
    {
        var reason = (CampaignReactionWindowCloseReason)reasonValue;
        var stopReason = (CampaignBreakdownStopReason)stopReasonValue;
        var prior = ActiveReaction();
        var input = CampaignBreakdownLifecycleFactory.CreateReactionCloseInput(prior, Artifact, Scenario, reason);
        var closed = CampaignBreakdownLifecycleFactory.CreateReactionClose(prior, Artifact, Scenario, input);
        var stop = Assert.IsType<CampaignBreakdownFlow.ReactorStopClosed>(closed.BreakdownFlowAfter).Stop;
        Assert.Equal(stopReason, stop.Reason);
        Assert.Contains(prior.ReactionWindow!.ActiveOpportunityId!, closed.ClosedOpportunityIds);
        Assert.Null(closed.ActingSide);
        RoundTrip(closed);
    }

    [Fact]
    public void ActiveReactionCannotDeclineAndInactiveCompletionIsRejected()
    {
        Assert.Throws<InvalidOperationException>(() => CampaignBreakdownLifecycleFactory.CreateReactionCloseInput(ActiveReaction(), Artifact, Scenario,
            CampaignReactionWindowCloseReason.PlayerDecline));
        var (prior, _, _) = BreakdownMoveReplayTests.Reaction();
        Assert.Throws<InvalidOperationException>(() => CampaignBreakdownLifecycleFactory.CreateReactionCompletionInput(prior, Artifact, Scenario));
    }

    [Fact]
    public void InactiveCloseResumesPhasingRouteWithoutInventedStop()
    {
        var (prior, _, _) = BreakdownMoveReplayTests.Reaction();
        var input = CampaignBreakdownLifecycleFactory.CreateReactionCloseInput(prior, Artifact, Scenario, CampaignReactionWindowCloseReason.PlayerDecline);
        var closed = CampaignBreakdownLifecycleFactory.CreateReactionClose(prior, Artifact, Scenario, input);
        Assert.IsType<CampaignBreakdownFlow.Moving>(closed.BreakdownFlowAfter);
        Assert.Throws<InvalidOperationException>(() => CampaignBreakdownLifecycleFactory.CreateReactionCloseInput(prior, Artifact, Scenario,
            CampaignReactionWindowCloseReason.NoEligibleReactor));
        RoundTrip(closed);
    }

    [Fact]
    public void BreakdownCompletionAdvancesToFirstCombatAndRejectsForgedAction()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        var movement = CampaignBreakdownLifecycleFactory.CreateMovementCompletion(prior, Artifact, Scenario);
        var atBreakdown = new CampaignSnapshotV11(11, prior.CampaignId, movement.StateVersion, prior.RulesetHash,
            prior.Setup, prior.World, prior.InitiativeHolder, prior.OperationStageOrders, prior.OperationStageWeather,
            prior.RandomState, CampaignPositionV11.FromSequence(movement.SequencePosition), null, new CampaignBreakdownFlow.Idle());
        var completed = CampaignBreakdownLifecycleFactory.CreateBreakdownCompletion(atBreakdown, Artifact, Scenario,
            CampaignBreakdownLifecycleFactory.CreateBreakdownCompletionActionId());
        Assert.Equal(LandStepIds.PositionDetermination, completed.SequencePosition.StepId);
        Assert.Equal(LandActorRole.FirstActingSide, completed.SequencePosition.ActorRole);
        Assert.Equal(1, completed.SequencePosition.OperationStage);
        Assert.Throws<InvalidOperationException>(() => CampaignBreakdownLifecycleFactory.CreateBreakdownCompletion(atBreakdown, Artifact, Scenario,
            CampaignBreakdownCodec.EmptyHash));
        RoundTrip(completed);
    }

    [Fact]
    public void RouteAndSystemHandlesAreStateScopedAndExcludeTrustedStopIdentity()
    {
        var prior = Moving();
        var route = CampaignBreakdownLifecycleFactory.CreateRouteCapability(prior);
        var next = new CampaignSnapshotV11(11, prior.CampaignId, prior.StateVersion + 1, prior.RulesetHash, prior.Setup,
            prior.World, prior.InitiativeHolder, prior.OperationStageOrders, prior.OperationStageWeather,
            prior.RandomState, prior.CurrentPosition, prior.ReactionWindow, prior.BreakdownFlow);
        Assert.NotEqual(route, CampaignBreakdownLifecycleFactory.CreateRouteCapability(next));
        Assert.NotEqual(CampaignBreakdownLifecycleFactory.CreateSystemStopCapability(prior),
            CampaignBreakdownLifecycleFactory.CreateSystemStopCapability(next));
    }

    [Fact]
    public void ExhaustedActiveReactorCanCompleteOrTimeoutWithoutAnotherMove()
    {
        var prior = ActiveReaction();
        var id = Assert.IsType<CampaignBreakdownFlow.Reacting>(prior.BreakdownFlow).ReactorRoute!.ElementId;
        var allowance = Artifact.Definition.LegacyDefinition.Elements.Single(x => x.ElementId == id).BaseCapabilityPointAllowance;
        var world = new CampaignWorldSnapshotV6(6, prior.World.Elements.Select(x => x.ElementId != id ? x :
            new CampaignElementStateV5(x.ElementId, x.CurrentLocationId, x.ReserveStatus,
                new CampaignElementOperationalStateV5(x.OperationalState.LedgerGameTurn, x.OperationalState.LedgerOperationStage,
                    new CapabilityPointAmount(allowance, 1), x.OperationalState.CohesionLevel,
                    x.OperationalState.VehicleBreakdownState, x.OperationalState.MovementEnded), x.Components)),
            prior.World.Representations, prior.World.BrokenVehicleLots);
        prior = BreakdownSnapshotTests.Copy(prior, prior.BreakdownFlow, window: prior.ReactionWindow, world: world);
        var complete = CampaignBreakdownLifecycleFactory.CreateReactionCompletionInput(prior, Artifact, Scenario);
        Assert.IsType<CampaignBreakdownFlow.ReactorStopOpen>(
            CampaignBreakdownLifecycleFactory.CreateReactionCompletion(prior, Artifact, Scenario, complete).BreakdownFlowAfter);
        var timeout = CampaignBreakdownLifecycleFactory.CreateReactionCloseInput(prior, Artifact, Scenario, CampaignReactionWindowCloseReason.Timeout);
        Assert.IsType<CampaignBreakdownFlow.ReactorStopClosed>(
            CampaignBreakdownLifecycleFactory.CreateReactionClose(prior, Artifact, Scenario, timeout).BreakdownFlowAfter);
        Assert.Throws<InvalidOperationException>(() => CampaignBreakdownLifecycleFactory.CreateReactionCloseInput(prior, Artifact, Scenario,
            CampaignReactionWindowCloseReason.NoEligibleReactor));
    }

    [Fact]
    public void PendingOpenStopPreventsCompletionAndWindowClosure()
    {
        var prior = ActiveReaction();
        var completed = CampaignBreakdownLifecycleFactory.CreateReactionCompletion(prior, Artifact, Scenario,
            CampaignBreakdownLifecycleFactory.CreateReactionCompletionInput(prior, Artifact, Scenario));
        var stopped = new CampaignSnapshotV11(11, prior.CampaignId, completed.StateVersion, prior.RulesetHash,
            prior.Setup, prior.World, prior.InitiativeHolder, prior.OperationStageOrders, prior.OperationStageWeather,
            prior.RandomState, CampaignPositionV11.FromBreakdownStop(prior.CurrentPosition.SequenceContext),
            completed.ReactionWindowAfter, completed.BreakdownFlowAfter);
        Assert.Throws<InvalidOperationException>(() => CampaignBreakdownLifecycleFactory.CreateReactionCompletionInput(stopped, Artifact, Scenario));
        Assert.Throws<InvalidOperationException>(() => CampaignBreakdownLifecycleFactory.CreateReactionCloseInput(stopped, Artifact, Scenario,
            CampaignReactionWindowCloseReason.Timeout));
    }

    [Fact]
    public void ReplayOpenStopResolvesBeforeNoEligibleClosureResumesPhasingRoute()
    {
        var prior = ReplayActiveReaction();
        var phasing = Assert.IsType<CampaignPhasingContinuation.ResumeRoute>(
            Assert.IsType<CampaignBreakdownFlow.Reacting>(prior.BreakdownFlow).PhasingContinuation).Route;
        var complete = CampaignBreakdownLifecycleFactory.CreateReactionCompletion(prior, Artifact, Scenario,
            CampaignBreakdownLifecycleFactory.CreateReactionCompletionInput(prior, Artifact, Scenario));
        var stopped = ApplyCanonical(prior, complete);
        Assert.Equal(CampaignPositionV11Kind.BreakdownStop, stopped.CurrentPosition.Kind);
        Assert.IsType<CampaignBreakdownFlow.ReactorStopOpen>(stopped.BreakdownFlow);
        Assert.Null(stopped.ReactionWindow!.ActiveOpportunityId);
        var resolvedEvent = CampaignBreakdownStopResolvedFactory.Create(stopped, Artifact, Scenario,
            CampaignBreakdownLifecycleFactory.CreateResolveActionId(stopped));
        var resolved = ApplyCanonical(stopped, resolvedEvent);
        Assert.IsType<CampaignBreakdownFlow.Reacting>(resolved.BreakdownFlow);
        Assert.Null(resolved.ReactionWindow!.ActiveOpportunityId);
        Assert.Equal(stopped.World, resolved.World);
        Assert.Equal(stopped.RandomState, resolved.RandomState);
        var close = CampaignBreakdownLifecycleFactory.CreateReactionClose(resolved, Artifact, Scenario,
            CampaignBreakdownLifecycleFactory.CreateReactionCloseInput(resolved, Artifact, Scenario, CampaignReactionWindowCloseReason.NoEligibleReactor));
        Assert.Empty(close.ClosedOpportunityIds);
        var resumed = ApplyCanonical(resolved, close);
        Assert.Null(resumed.ReactionWindow);
        Assert.Equal(phasing, Assert.IsType<CampaignBreakdownFlow.Moving>(resumed.BreakdownFlow).Route);
        Assert.Equal(prior.World, resumed.World);
        Assert.Equal(prior.RandomState, resumed.RandomState);
        Assert.Throws<InvalidCampaignHistoryException>(() => CampaignV11BreakdownProjector.Apply(resolved, resolvedEvent, Artifact, Scenario));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public void ReplayActiveSystemClosureWaitsForStopBeforeResumingPhasingRoute(int reasonValue)
    {
        var prior = ReplayActiveReaction();
        var phasing = Assert.IsType<CampaignPhasingContinuation.ResumeRoute>(
            Assert.IsType<CampaignBreakdownFlow.Reacting>(prior.BreakdownFlow).PhasingContinuation).Route;
        var close = CampaignBreakdownLifecycleFactory.CreateReactionClose(prior, Artifact, Scenario,
            CampaignBreakdownLifecycleFactory.CreateReactionCloseInput(prior, Artifact, Scenario, (CampaignReactionWindowCloseReason)reasonValue));
        var stopped = ApplyCanonical(prior, close);
        Assert.Null(stopped.ReactionWindow);
        Assert.IsType<CampaignBreakdownFlow.ReactorStopClosed>(stopped.BreakdownFlow);
        Assert.Equal(CampaignPositionV11Kind.BreakdownStop, stopped.CurrentPosition.Kind);
        var resolved = ApplyCanonical(stopped, CampaignBreakdownStopResolvedFactory.Create(stopped, Artifact, Scenario,
            CampaignBreakdownLifecycleFactory.CreateResolveActionId(stopped)));
        Assert.Equal(phasing, Assert.IsType<CampaignBreakdownFlow.Moving>(resolved.BreakdownFlow).Route);
        Assert.Null(resolved.ReactionWindow);
        Assert.Equal(prior.World, resolved.World);
        Assert.Equal(prior.RandomState, resolved.RandomState);
    }

    [Fact]
    public void ReplayForcedPhasingStopRemainsDeferredUntilReactorStopResolves()
    {
        var prior = ReplayActiveReaction(forcePhasingStop: true);
        var deferred = Assert.IsType<CampaignPhasingContinuation.ResolveStop>(
            Assert.IsType<CampaignBreakdownFlow.Reacting>(prior.BreakdownFlow).PhasingContinuation).Stop;
        Assert.Equal(CampaignBreakdownStopReason.CpExhausted, deferred.Reason);
        var closed = ApplyCanonical(prior, CampaignBreakdownLifecycleFactory.CreateReactionClose(prior, Artifact, Scenario,
            CampaignBreakdownLifecycleFactory.CreateReactionCloseInput(prior, Artifact, Scenario, CampaignReactionWindowCloseReason.Timeout)));
        var reactorStop = Assert.IsType<CampaignBreakdownFlow.ReactorStopClosed>(closed.BreakdownFlow).Stop;
        Assert.Equal(LandSide.Commonwealth, reactorStop.Route.Owner);
        var phasingStop = ApplyCanonical(closed, CampaignBreakdownStopResolvedFactory.Create(closed, Artifact, Scenario,
            CampaignBreakdownLifecycleFactory.CreateResolveActionId(closed)));
        Assert.Equal(deferred, Assert.IsType<CampaignBreakdownFlow.PhasingStop>(phasingStop.BreakdownFlow).Stop);
        Assert.Equal(CampaignPositionV11Kind.BreakdownStop, phasingStop.CurrentPosition.Kind);
        var idle = ApplyCanonical(phasingStop, CampaignBreakdownStopResolvedFactory.Create(phasingStop, Artifact, Scenario,
            CampaignBreakdownLifecycleFactory.CreateResolveActionId(phasingStop)));
        Assert.IsType<CampaignBreakdownFlow.Idle>(idle.BreakdownFlow);
        Assert.Equal(CampaignPositionV11Kind.Sequence, idle.CurrentPosition.Kind);
        Assert.Equal(prior.World, idle.World);
        Assert.Equal(prior.RandomState, idle.RandomState);
    }

    [Theory]
    [InlineData("action")]
    [InlineData("state")]
    [InlineData("side")]
    [InlineData("opportunity")]
    public void ReplayRejectsForgedOrStaleParticipantAuthority(string mutation)
    {
        var prior = ReplayActiveReaction();
        var original = CampaignBreakdownLifecycleFactory.CreateReactionCompletion(prior, Artifact, Scenario,
            CampaignBreakdownLifecycleFactory.CreateReactionCompletionInput(prior, Artifact, Scenario));
        var forged = mutation switch
        {
            "action" => original with { ActionId = CampaignBreakdownCodec.EmptyHash },
            "state" => original with { PriorStateVersion = original.PriorStateVersion - 1 },
            "side" => original with { ActingSide = LandSide.Axis },
            _ => original with { SubmittedOpportunityId = CampaignBreakdownCodec.EmptyHash },
        };
        Assert.Throws<InvalidCampaignHistoryException>(() => CampaignV11BreakdownProjector.Apply(prior, forged, Artifact, Scenario));
    }

    private static CampaignSnapshotV11 ReplayActiveReaction(bool forcePhasingStop = false)
    {
        var prior = BreakdownSnapshotTests.Create(true);
        prior = BreakdownSnapshotTests.Copy(prior, prior.BreakdownFlow,
            world: BreakdownMoveReplayTests.Relocate(prior.World, "commonwealth-infantry", "south-west"));
        var input = BreakdownMoveReplayTests.Ordinary(prior, "axis-infantry", "north-west", "west");
        if (forcePhasingStop)
        {
            var cost = CampaignElementMovedV3Factory.Create(prior, Artifact, Scenario, input).Cost.TotalCost;
            var allowance = Artifact.Definition.LegacyDefinition.Elements.Single(x => x.ElementId == "axis-infantry").BaseCapabilityPointAllowance;
            var world = new CampaignWorldSnapshotV6(6, prior.World.Elements.Select(x => x.ElementId != "axis-infantry" ? x :
                new CampaignElementStateV5(x.ElementId, x.CurrentLocationId, x.ReserveStatus,
                    new CampaignElementOperationalStateV5(x.OperationalState.LedgerGameTurn, x.OperationalState.LedgerOperationStage,
                        new CapabilityPointAmount(allowance * cost.Denominator - cost.Numerator, cost.Denominator),
                        x.OperationalState.CohesionLevel, x.OperationalState.VehicleBreakdownState, x.OperationalState.MovementEnded), x.Components)),
                prior.World.Representations, prior.World.BrokenVehicleLots);
            prior = BreakdownSnapshotTests.Copy(prior, prior.BreakdownFlow, world: world);
        }
        var reacting = ApplyCanonical(prior, CampaignElementMovedV3Factory.Create(prior, Artifact, Scenario, input));
        var reactionInput = CampaignReactingElementMovedV2Factory.CreateReplayInput(reacting, Artifact, Scenario,
            reacting.ReactionWindow!.FrozenOpportunities.Single().OpportunityId, "south");
        return ApplyCanonical(reacting, CampaignReactingElementMovedV2Factory.Create(reacting, Artifact, Scenario, reactionInput));
    }

    private static CampaignSnapshotV11 ApplyCanonical(CampaignSnapshotV11 prior, CampaignSuccessorEvent value)
    {
        var bytes = CampaignBreakdownEventSerializer.Serialize(value);
        var restored = CampaignBreakdownEventSerializer.Deserialize(bytes);
        Assert.Equal(bytes, CampaignBreakdownEventSerializer.Serialize(restored));
        var after = CampaignV11BreakdownProjector.Apply(prior, restored, Artifact, Scenario);
        Assert.Equal(prior.StateVersion + 1, after.StateVersion);
        Assert.True(CampaignSnapshotV11Validator.IsValid(after, Artifact, Scenario));
        return after;
    }

    private static void RoundTrip(CampaignSuccessorEvent value)
    {
        var bytes = CampaignBreakdownLifecycleCodec.Serialize(value);
        Assert.Equal(bytes, CampaignBreakdownLifecycleCodec.Serialize(CampaignBreakdownLifecycleCodec.Deserialize(bytes)));
        Assert.Throws<JsonException>(() => CampaignBreakdownLifecycleCodec.Deserialize(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes) + " ")));
    }
    internal static CampaignSnapshotV11 Moving()
    {
        var prior = BreakdownSnapshotTests.Create(true);
        return CampaignV11MoveProjector.ApplyMovement(prior, BreakdownMovementTests.Move(prior, "axis-truck", "west", "center"), Artifact, Scenario);
    }
    internal static CampaignSnapshotV11 ActiveReaction()
    {
        var (prior, artifact, scenario) = BreakdownMoveReplayTests.Reaction();
        var input = CampaignReactingElementMovedV2Factory.CreateReplayInput(prior, artifact, scenario,
            prior.ReactionWindow!.FrozenOpportunities.Single().OpportunityId, "south");
        return CampaignV11MoveProjector.ApplyReactionMove(prior, CampaignReactingElementMovedV2Factory.Create(prior, artifact, scenario, input), artifact, scenario);
    }
    private static ContentPackV6Artifact Artifact => BreakdownContentFixture.Artifact();
    private static ContentScenario Scenario => BreakdownSnapshotTests.Scenario();
}
