using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Observations;

[Trait("Boundary", "UserSpace")]
public sealed class CampaignObservationV7TranscriptTests
{
    [Theory]
    [InlineData(false, "complete")]
    [InlineData(false, "unavailable")]
    [InlineData(false, "timeout")]
    [InlineData(true, "complete")]
    [InlineData(true, "unavailable")]
    [InlineData(true, "timeout")]
    public void HiddenRouteAndStopIdentitiesPreserveBothRetainedAudienceTranscripts(bool lastCp, string closure)
    {
        var fixture = ReachReaction(lastCp ? "reaction.last-cp" : "reaction.adjacent");
        var snapshot = fixture.Handle.CurrentSnapshot!;
        var reacting = Assert.IsType<CampaignBreakdownFlow.Reacting>(snapshot.BreakdownFlow);
        var original = reacting.PhasingContinuation switch
        {
            CampaignPhasingContinuation.ResumeRoute resume => resume.Route,
            CampaignPhasingContinuation.ResolveStop resolve => resolve.Stop.Route,
            _ => throw new InvalidOperationException(),
        };
        var changed = CampaignBreakdownRoute.Create(snapshot.CampaignId, snapshot.RulesetHash,
            original.FirstMoveStateVersion - 1, original.ElementId, original.RepresentationId,
            original.Owner, original.OriginLocationId, original.CurrentLocationId, original.CohortIds);
        Assert.NotEqual(original.RouteId, changed.RouteId);
        CampaignPhasingContinuation continuation = reacting.PhasingContinuation is CampaignPhasingContinuation.ResolveStop stopped
            ? new CampaignPhasingContinuation.ResolveStop(CampaignBreakdownStop.Create(snapshot.CampaignId,
                snapshot.RulesetHash, stopped.Stop.RecordedStateVersion, changed, stopped.Stop.Reason,
                stopped.Stop.WeatherKind, stopped.Stop.CohortInputs))
            : new CampaignPhasingContinuation.ResumeRoute(changed);
        var alternate = Replace(fixture.Handle, flow: new CampaignBreakdownFlow.Reacting(continuation, null));

        var left = Run(fixture.Handle, closure);
        var right = Run(alternate, closure);
        AssertTraces(fixture.Prefix, left, right, LandSide.Axis, LandSide.Commonwealth);
        Assert.Contains(left, handle => handle.CurrentSnapshot!.BreakdownFlow is
            CampaignBreakdownFlow.ReactorStopOpen or CampaignBreakdownFlow.ReactorStopClosed);
        Assert.Contains(left, handle => handle.CurrentSnapshot!.BreakdownFlow is CampaignBreakdownFlow.PhasingStop);
        Assert.Equal(closure == "complete", left.Any(handle => handle.CurrentSnapshot!.BreakdownFlow is CampaignBreakdownFlow.ReactorStopOpen));
        Assert.Equal(closure != "complete", left.Any(handle => handle.CurrentSnapshot!.BreakdownFlow is CampaignBreakdownFlow.ReactorStopClosed));
        Assert.Equal(!lastCp, left.Any(handle => handle.CurrentSnapshot!.BreakdownFlow is CampaignBreakdownFlow.Moving));
    }

    [Theory]
    [InlineData("absent")]
    [InlineData("frozen-location")]
    public void SuppressedOpportunityIdentityEligibilityAndFrozenLocationPreserveWholeContinuation(string variation)
    {
        var fixture = ReachReaction("reaction.adjacent");
        // A spent inactive reactor has no public capability. Its frozen membership/location is hidden;
        // its current location remains unchanged and visible in both worlds.
        var baseline = ChangeElement(fixture.Handle, "commonwealth-reactor-b", spent: new CapabilityPointAmount(10, 1));
        var snapshot = baseline.CurrentSnapshot!;
        var window = snapshot.ReactionWindow!;
        var hidden = window.FrozenOpportunities.Single(value => value.ReactingRepresentation.BoundElementIds.Contains("commonwealth-reactor-b"));
        var other = window.FrozenOpportunities.Single(value => value != hidden);
        var representation = new CampaignMapRepresentationState(hidden.ReactingRepresentation.RepresentationId,
            other.ReactingRepresentation.CurrentLocationId, hidden.ReactingRepresentation.BindingKind,
            hidden.ReactingRepresentation.BoundElementIds);
        var relocated = new CampaignFrozenReactionOpportunity(CampaignReactionIdentity.CreateOpportunity(window.WindowId, representation),
            representation, new CampaignReactionAdjacencyEvidence(representation.CurrentLocationId,
                window.TriggerAuthority.DestinationLocationId, true, hidden.AdjacencyEvidence.Sources));
        var changedWindow = new CampaignReactionWindow(window.WindowId, window.TriggerCommittedStateVersion,
            window.PhasingSide, window.ReactingSide, window.ReactingPosition, window.TriggerAuthority,
            window.ApparentTrigger, variation == "absent" ? [other] : [other, relocated], [], null);
        var alternate = Replace(baseline, window: changedWindow);
        Assert.NotEqual(window, alternate.CurrentSnapshot!.ReactionWindow);
        Assert.Equal(baseline.CurrentSnapshot!.World, alternate.CurrentSnapshot.World);
        var left = Run(baseline, "complete");
        var right = Run(alternate, "complete");
        AssertTraces(fixture.Prefix, left, right, LandSide.Axis, LandSide.Commonwealth);
        Assert.Single(left, handle => handle.CurrentSnapshot!.BreakdownFlow is CampaignBreakdownFlow.ReactorStopOpen);
    }

    [Theory]
    [InlineData("bp", "complete")]
    [InlineData("bp", "unavailable")]
    [InlineData("bp", "timeout")]
    [InlineData("eligibility", "complete")]
    public void OpposingPrivateLedgersPreservePhasingTranscriptThroughCombat(string variation, string closure)
    {
        var fixture = ReachReaction(variation == "bp" ? "reaction" : "reaction.adjacent");
        var baseline = variation == "bp" ? fixture.Handle
            : ChangeElement(fixture.Handle, "commonwealth-reactor-b", spent: new CapabilityPointAmount(10, 1));
        var alternate = variation == "bp"
            ? ChangeElement(baseline, "commonwealth-truck", bp: new BreakdownPointAmount(26, 1))
            : ChangeElement(baseline, "commonwealth-reactor-b", spent: CapabilityPointAmount.Zero, cohesion: -26);
        Assert.NotEqual(baseline.CurrentSnapshot!.World, alternate.CurrentSnapshot!.World);
        var left = Run(baseline, closure);
        var right = Run(alternate, closure);
        AssertTraces(fixture.Prefix, left, right, LandSide.Axis);
        // Own ledgers are intentionally released again on resumption. Equality for this audience
        // after release would incorrectly turn an approved fact into a secrecy requirement.
        Assert.NotEqual(Frame(left[^1], LandSide.Commonwealth), Frame(right[^1], LandSide.Commonwealth));
    }

    [Theory]
    [InlineData("complete")]
    [InlineData("unavailable")]
    [InlineData("timeout")]
    public void HiddenOwnLocationStaysSuppressedUntilApprovedRowsReturn(string closure)
    {
        var fixture = ReachReaction("reaction");
        var snapshot = fixture.Handle.CurrentSnapshot!;
        var world = snapshot.World;
        var changedWorld = new CampaignWorldSnapshotV6(6,
            world.Elements.Select(element => element.ElementId == "commonwealth-truck"
                ? new CampaignElementStateV5(element.ElementId, "south-east", element.ReserveStatus,
                    element.OperationalState, element.Components) : element),
            world.Representations.Select(representation => representation.BoundElementIds.Contains("commonwealth-truck")
                ? new CampaignMapRepresentationState(representation.RepresentationId, "south-east",
                    representation.BindingKind, representation.BoundElementIds) : representation), world.BrokenVehicleLots);
        var alternate = Replace(fixture.Handle, world: changedWorld);
        var left = Run(fixture.Handle, closure);
        var right = Run(alternate, closure);
        Assert.Equal(left.Count, right.Count);
        var suppressed = 0;
        for (var index = 0; index < left.Count; index++)
        {
            var before = CampaignObservations.Query(left[index], LandSide.Commonwealth).Observation!;
            var after = CampaignObservations.Query(right[index], LandSide.Commonwealth).Observation!;
            Assert.Equal(before.Position, after.Position);
            if (before.DecisionState is CampaignObservationV7ReactingDecisionState or CampaignObservationV7BreakdownWaitingDecisionState)
            {
                Assert.Empty(before.OwnElements);
                Assert.Empty(after.OwnElements);
                Assert.Equal(Frame(left[index], LandSide.Commonwealth), Frame(right[index], LandSide.Commonwealth));
                suppressed++;
            }
            else
            {
                Assert.NotEmpty(before.OwnElements);
                Assert.NotEqual(Frame(left[index], LandSide.Commonwealth), Frame(right[index], LandSide.Commonwealth));
            }
        }
        Assert.True(suppressed >= 3); // Idle Reaction, active reactor, pending reactor stop.
        // Opposing locations are already apparent to Axis, so these worlds are deliberately
        // outside that audience's equivalence class (including occupancy/blocker locations).
        Assert.NotEqual(Frame(fixture.Handle, LandSide.Axis), Frame(alternate, LandSide.Axis));
    }

    private static void AssertTraces(IReadOnlyList<CampaignAuthorityHandle> prefix,
        List<CampaignAuthorityHandle> left, List<CampaignAuthorityHandle> right, params LandSide[] observers)
    {
        Assert.Equal(left.Count, right.Count);
        foreach (var observer in observers)
        {
            var retained = prefix.Select(handle => Frame(handle, observer)).ToArray();
            Assert.Equal(retained.Concat(left.Select(handle => Frame(handle, observer))),
                retained.Concat(right.Select(handle => Frame(handle, observer))));
        }
    }

    private static AudienceFrame Frame(CampaignAuthorityHandle handle, LandSide observer)
    {
        var projection = CampaignObservations.Query(handle, observer);
        Assert.True(projection.IsProjected, projection.RejectionReason.ToString());
        var observation = projection.Observation!;
        var set = Query(handle, observer == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth);
        return new AudienceFrame(
            Encoding.UTF8.GetString(CampaignObservationV7Serializer.SerializeCanonical(observation)),
            Encoding.UTF8.GetString(CampaignProjectedDecisionHistoryV2Serializer.SerializeCanonical(CampaignProjectedDecisionHistoryV2.Project(observation))),
            Encoding.UTF8.GetString(CampaignLegalActionSerializer.Serialize(set)),
            $"{observation.StateVersion}:{observation.Position.PositionId}:{observation.Position.ActorRole}:{observation.Position.ActiveSide}");
    }

    private static List<CampaignAuthorityHandle> Run(CampaignAuthorityHandle handle, string closure)
    {
        List<CampaignAuthorityHandle> trace = [handle];
        for (var step = 0; step < 40; step++)
        {
            var snapshot = handle.CurrentSnapshot!;
            if (snapshot.CurrentPosition.SequenceContext.SegmentId == LandSegmentIds.Combat)
            {
                Assert.All(Enum.GetValues<CampaignActionAudience>(), audience => Assert.Empty(Query(handle, audience).Candidates));
                return trace;
            }
            var axis = Query(handle, CampaignActionAudience.Axis);
            var reacting = Query(handle, CampaignActionAudience.Commonwealth);
            var system = Query(handle, CampaignActionAudience.System);
            CampaignLegalActionSet set;
            CampaignActionCandidate action;
            if (snapshot.BreakdownFlow is CampaignBreakdownFlow.Reacting { ReactorRoute: not null })
            {
                set = closure == "complete" ? reacting : system;
                action = set.Candidates.Single(value => closure switch
                {
                    "complete" => value is CompleteReactionParticipantAction,
                    "timeout" => value is CloseReactionWindowTimeoutAction,
                    _ => value is CloseReactionWindowUnavailableAction,
                });
            }
            else if (reacting.Candidates.OfType<MoveReactingElementAction>().FirstOrDefault() is { } move)
            { set = reacting; action = move; }
            else if (system.Candidates.FirstOrDefault(value => value is ResolveBreakdownStopAction
                or CloseReactionWindowNoEligibleAction or CompleteBreakdownSegmentAction) is { } automatic)
            { set = system; action = automatic; }
            else
            {
                set = axis;
                action = axis.Candidates.FirstOrDefault(value => value is StopElementMovementAction)
                    ?? Assert.Single(axis.Candidates.OfType<CompleteMovementSegmentAction>());
            }
            handle = Submit(handle, set, action);
            Assert.True(handle.CurrentSnapshot!.StateVersion > snapshot.StateVersion);
            trace.Add(handle);
        }
        throw new InvalidOperationException("Current continuation failed to reach Combat within 40 accepted actions.");
    }

    private static (CampaignAuthorityHandle Handle, List<CampaignAuthorityHandle> Prefix) ReachReaction(string variant)
    {
        var setup = Cna1979BreakdownSetupCatalog.Definitions.Single(value => value.SetupId == $"rules-lab.breakdown.{variant}.v1");
        var creation = CampaignAuthority.Create(new CampaignCreationRequest(1, "breakdown-private-transcript",
            Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.Hash, setup.Content.Pack.PackId,
            setup.Content.Pack.Hash, setup.Content.ScenarioId));
        Assert.True(creation.IsCreated, creation.RejectionReason.ToString());
        var handle = creation.Handle!;
        List<CampaignAuthorityHandle> prefix = [];
        for (var step = 0; step < 20; step++)
        {
            prefix.Add(handle);
            var snapshot = handle.CurrentSnapshot!;
            if (snapshot.CurrentPosition.SequenceContext.SegmentId == LandSegmentIds.Movement)
            {
                var set = Query(handle, CampaignActionAudience.Axis);
                var move = set.Candidates.OfType<MoveElementAction>().Single(value => variant == "reaction"
                    ? value.ElementId == "axis-infantry" && value.DestinationLocationId == "north"
                    : value.DestinationLocationId == "trigger-a");
                handle = Submit(handle, set, move);
                Assert.NotNull(handle.CurrentSnapshot!.ReactionWindow);
                return (handle, prefix);
            }
            var actions = Enum.GetValues<CampaignActionAudience>().Select(audience => Query(handle, audience))
                .First(value => value.Candidates.Count > 0);
            var candidate = actions.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? actions.Candidates.FirstOrDefault(value => value is ActFirstAction) ?? actions.Candidates[0];
            handle = Submit(handle, actions, candidate);
        }
        throw new InvalidOperationException("Preamble failed to reach Movement.");
    }

    private static CampaignAuthorityHandle ChangeElement(CampaignAuthorityHandle handle, string id,
        CapabilityPointAmount? spent = null, int? cohesion = null, BreakdownPointAmount? bp = null)
    {
        var world = handle.CurrentSnapshot!.World;
        return Replace(handle, world: new CampaignWorldSnapshotV6(6, world.Elements.Select(element =>
        {
            if (element.ElementId != id) return element;
            var state = element.OperationalState;
            var breakdown = state.VehicleBreakdownState;
            if (bp is not null)
                breakdown = new CampaignVehicleBreakdownState(breakdown!.CohortId, bp,
                    breakdown.SandstormAttributedBreakdownPoints, breakdown.HighestEffectiveCheckedBandId,
                    breakdown.WorkingPointCount, breakdown.BrokenPointCount);
            return new CampaignElementStateV5(id, element.CurrentLocationId, element.ReserveStatus,
                new CampaignElementOperationalStateV5(state.LedgerGameTurn, state.LedgerOperationStage,
                    spent ?? state.CapabilityPointsExpended, cohesion ?? state.CohesionLevel, breakdown, state.MovementEnded), element.Components);
        }), world.Representations, world.BrokenVehicleLots));
    }

    private static CampaignAuthorityHandle Replace(CampaignAuthorityHandle handle, CampaignBreakdownFlow? flow = null,
        CampaignReactionWindow? window = null, CampaignWorldSnapshotV6? world = null)
    {
        var s = handle.CurrentSnapshot!;
        var alternate = new CampaignSnapshotV11(11, s.CampaignId, s.StateVersion, s.RulesetHash, s.Setup,
            world ?? s.World, s.InitiativeHolder, s.OperationStageOrders, s.OperationStageWeather,
            s.RandomState, s.CurrentPosition, window ?? s.ReactionWindow, flow ?? s.BreakdownFlow);
        Assert.True(CampaignSnapshotV11Admission.IsValid(alternate, handle.Context.ArtifactV6!, handle.Context.Scenario));
        Assert.NotEqual(CampaignSnapshotV11Serializer.Serialize(s), CampaignSnapshotV11Serializer.Serialize(alternate));
        return new CampaignAuthorityHandle(alternate, handle.Context);
    }

    private static CampaignLegalActionSet Query(CampaignAuthorityHandle handle, CampaignActionAudience audience)
    {
        var result = CampaignLegalActions.Query(handle, audience);
        Assert.True(result.IsSuccessful, result.RejectionReason.ToString());
        return result.ActionSet!;
    }

    private static CampaignAuthorityHandle Submit(CampaignAuthorityHandle handle, CampaignLegalActionSet set, CampaignActionCandidate action)
    {
        var result = CampaignLegalActions.Submit(handle, new CampaignActionSubmission(1, set.CampaignId,
            set.StateVersion, set.PositionId, set.Audience, action.ActionId));
        Assert.True(result.IsAccepted, $"{action.Kind}: {result.RejectionReason}");
        return result.SuccessorHandle!;
    }

    private sealed record AudienceFrame(string Observation, string History, string Actions, string Progress);
}
