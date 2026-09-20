using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>One ordinary reacting participant, mandatory stop resolution, and exact phasing-route resumption.</summary>
internal static class CampaignCombatReactionLifecycle
{
    public static CampaignCombatReactionLifecycleState Replay(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> triggers, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(triggers); ArgumentNullException.ThrowIfNull(events);
        if (triggers.Count != 1 || events.Count > 4) throw new JsonException("Lifecycle requires one actual trigger and at most four events.");
        var trigger = CampaignCombatReactionTrigger.Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, triggers);
        var state = Initial(trigger);
        foreach (var retained in events)
        {
            if (retained is not { Length: > 0 and <= 1_048_576 }) throw new JsonException("Missing or oversized lifecycle event.");
            var bytes = retained.ToArray();
            var (after, emitted) = Emit(state, CampaignCombatReactionLifecycleCodec.ReadEventInput(bytes));
            if (!bytes.AsSpan().SequenceEqual(CampaignCombatReactionLifecycleCodec.SerializeEvent(emitted)))
                throw new JsonException("Lifecycle event differs from actual history-derived transition.");
            state = after;
        }
        return state;
    }
    public static CampaignCombatReactionLifecycleResult Apply(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> triggers,
        IReadOnlyList<byte[]> events, CampaignCombatReactionLifecycleInput input)
    {
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, triggers, events);
        Authorize(state, input);
        foreach (var accepted in state.Events)
        {
            if (accepted.Input.Command.Identity.ExpectedPriorVersion != input.Command.Identity.ExpectedPriorVersion) continue;
            if (accepted.Input != input) throw new JsonException("Conflicting lifecycle retry.");
            return new(state, CampaignCombatReactionLifecycleCodec.SerializeEvent(accepted), true);
        }
        var (after, emitted) = Emit(state, input);
        return new(after, CampaignCombatReactionLifecycleCodec.SerializeEvent(emitted), false);
    }
    public static CampaignCombatReactionLifecycleState ReadState(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents,
        IReadOnlyList<byte[]> stageEvents, IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves,
        IReadOnlyList<byte[]> triggers, IReadOnlyList<byte[]> events)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized lifecycle state.");
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, triggers, events);
        if (!bytes.SequenceEqual(CampaignCombatReactionLifecycleCodec.SerializeState(state)))
            throw new JsonException("Lifecycle cache differs from retained history.");
        return state;
    }
    private static CampaignCombatReactionLifecycleState Initial(CampaignCombatReactionTriggerState trigger)
    {
        if (trigger.StateVersion != 13 || trigger.ReactionWindow is not { FrozenOpportunities.Count: 1 } window ||
            trigger.BreakdownFlow is not CampaignBreakdownFlow.Reacting { ReactorRoute: null, PhasingContinuation: CampaignPhasingContinuation.ResumeRoute } flow)
            throw new JsonException("Lifecycle requires actual single-opportunity F1 trigger.");
        return new(trigger, trigger.StateVersion, trigger.Prefix, trigger.World, new(window, [], null), flow,
            trigger.Receipts, trigger.Tracks, trigger.ActualProgressRefs, []);
    }
    public static CampaignCombatReactionLifecycleInput Command(CampaignCombatReactionLifecycleState state)
    {
        var index = state.Events.Count;
        if (index > 3 || state.ReactionWindow is null) throw new JsonException("Lifecycle is already complete.");
        var creation = state.Opening.Predecessor.Stage.Weather.Opening.Creation;
        var window = CampaignCombatReactionLifecycleCodec.WindowCapability(state);
        var opportunity = index < 2 ? CampaignObservationV6DisclosureIdentity.CreateOpportunity(window, state.StateVersion,
            CampaignObservationV6DisclosureIdentity.CreateCapabilityKey(MoveOptions(state))) : null;
        var stop = index == 2 ? CampaignCombatReactionLifecycleCodec.StopCapability(state) : null;
        var option = index == 0 ? MoveOptions(state).Single() : null;
        var actionId = index switch
        {
            0 => new MoveReactingElementAction(window, opportunity!, option!.OriginLocationId, option.DestinationLocationId, option.CostBreakdown).ActionId,
            1 => new CompleteReactionParticipantAction(window, opportunity!).ActionId,
            2 => CampaignCombatMovementLifecycleCodec.Action(1, stop),
            _ => new CloseReactionWindowNoEligibleAction(window).ActionId,
        };
        var id = new CampaignCombatReactionLifecycleIdentity(2, actionId, creation.CreationReceipt.CreationBinding,
            creation.CreationReceipt.CreationEventHash, state.Opening.CycleId!, state.StateVersion, state.SequencePosition.PositionId);
        CampaignCombatReactionLifecycleCommand command = index switch
        {
            0 => new CampaignCombatReactionLifecycleCommand.Move(id, window, opportunity!, option!.OriginLocationId, option.DestinationLocationId),
            1 => new CampaignCombatReactionLifecycleCommand.Complete(id, window, opportunity!),
            2 => new CampaignCombatReactionLifecycleCommand.Resolve(id, stop!),
            _ => new CampaignCombatReactionLifecycleCommand.Close(id, window),
        };
        return new(command, index < 2 ? Owner(state) : CampaignOpeningPreambleActor.System);
    }
    private static CampaignOpeningPreambleActor Owner(CampaignCombatReactionLifecycleState state) =>
        state.Trigger.ReactionWindow!.ReactingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth;
    private static void Authorize(CampaignCombatReactionLifecycleState state, CampaignCombatReactionLifecycleInput input)
    {
        _ = CampaignCombatReactionLifecycleCodec.SerializeInput(input);
        var actor = input.Command is CampaignCombatReactionLifecycleCommand.Move or CampaignCombatReactionLifecycleCommand.Complete
            ? Owner(state) : CampaignOpeningPreambleActor.System;
        if (input.Actor != actor) throw new JsonException("Wrong authenticated lifecycle actor.");
        var receipt = state.Opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        if (input.Command.Identity.CreationBinding != receipt.CreationBinding || input.Command.Identity.CreationEventHash != receipt.CreationEventHash ||
            input.Command.Identity.CycleId != state.Opening.CycleId)
            throw new JsonException("Lifecycle creation or cycle identity differs from actual history.");
    }
    internal static IReadOnlyList<ObservedReactionMoveOption> MoveOptions(CampaignCombatReactionLifecycleState state)
    {
        if (state.ReactionWindow is null || state.ReactionWindow.ResolvedOpportunityIds.Count != 0) return [];
        var opportunity = state.ReactionWindow.Trigger.FrozenOpportunities.Single();
        var element = state.World.Elements.Single(e => e.ElementId == opportunity.ReactingRepresentation.BoundElementIds.Single());
        var side = CampaignSnapshotSerializer.FormatSide(state.ReactionWindow.Trigger.ReactingSide);
        var assault = side == "axis" ? "assault-west" : "assault-east";
        var destination = element.CurrentLocationId == assault ? side + "-rear" : element.CurrentLocationId == side + "-rear" ? side + "-supply" : null;
        var pack = state.Opening.Predecessor.Stage.Weather.Opening.Creation.Setup.Artifact.Definition;
        var facts = pack.Elements.Single(e => e.ElementId == element.ElementId);
        // Exact retained Content7 profile: return from rear to assault is controlled by the phasing
        // infantry. The remaining rear-to-supply edge stays in the complete public inventory.
        if (destination is null || facts.SideId != side || facts.MobilityId != Cna1979Movement.NonMotorizedMobilityId ||
            facts.PlacementMode != ContentPlacementMode.Independent || facts.BaseCapabilityPointAllowance != 10 ||
            facts.CombatClassificationId != "land.combat-classification.combat-unit" || facts.Components.Any(c => c.ComponentClassId != "land.combat-component.infantry") ||
            element.ReserveStatus != CampaignElementReserveStatus.None || element.OperationalState.VehicleBreakdownState is not null ||
            element.OperationalState.MovementEnded is not null || element.OperationalState.CohesionLevel != 0 ||
            element.OperationalState.CapabilityPointsExpended > new CapabilityPointAmount(2, 1) || element.Components.Any(c => c.CurrentToe <= 0))
            throw new JsonException("Unsupported reacting infantry profile.");
        var edge = pack.Edges.SingleOrDefault(e => e.FirstLocationId == element.CurrentLocationId && e.SecondLocationId == destination ||
            e.SecondLocationId == element.CurrentLocationId && e.FirstLocationId == destination);
        if (edge is null || edge.Features.Count != 0 || pack.Locations.Single(l => l.LocationId == destination).TerrainId != "land.terrain.clear")
            throw new JsonException("Unsupported reacting edge.");
        return Array.AsReadOnly<ObservedReactionMoveOption>([new(element.CurrentLocationId, destination,
            new("land.terrain.clear", new(2, 1), null, [], new(2, 1)))]);
    }
    private static (CampaignCombatReactionLifecycleState State, CampaignCombatReactionLifecycleEvent Event) Emit(
        CampaignCombatReactionLifecycleState state, CampaignCombatReactionLifecycleInput input)
    {
        Authorize(state, input);
        if (input != Command(state)) throw new JsonException("Lifecycle command differs from current public capability or occurrence.");
        var index = state.Events.Count; var window = state.ReactionWindow!; var trigger = window.Trigger;
        var cycle = state.Opening.Cycle!; var version = checked(state.StateVersion + 1);
        var opportunity = trigger.FrozenOpportunities.Single();
        var continuation = ((CampaignBreakdownFlow.Reacting)state.Trigger.BreakdownFlow!).PhasingContinuation;
        CampaignCombatReactionLifecycleWindow? afterWindow = window; CampaignBreakdownFlow flow;
        if (index == 0)
        {
            if (state.BreakdownFlow is not CampaignBreakdownFlow.Reacting { ReactorRoute: null } || window.ActiveOpportunityId is not null || window.ResolvedOpportunityIds.Count != 0)
                throw new JsonException("Move requires an inactive unresolved participant.");
            var move = (CampaignCombatReactionLifecycleCommand.Move)input.Command;
            var route = CampaignBreakdownRoute.Create(cycle.CampaignId, cycle.RulesetHash, version, opportunity.ReactingRepresentation.BoundElementIds.Single(),
                opportunity.ReactingRepresentation.RepresentationId, trigger.ReactingSide, move.OriginLocationId, move.DestinationLocationId, []);
            flow = new CampaignBreakdownFlow.Reacting(continuation, route);
            afterWindow = new(trigger, [], opportunity.OpportunityId);
        }
        else if (index == 1)
        {
            if (state.BreakdownFlow is not CampaignBreakdownFlow.Reacting { ReactorRoute: { } route } || window.ActiveOpportunityId != opportunity.OpportunityId)
                throw new JsonException("Completion requires the active reactor route.");
            var stop = CampaignBreakdownStop.Create(cycle.CampaignId, cycle.RulesetHash, version, route, CampaignBreakdownStopReason.ReactionCompleted, BreakdownWeatherKind.Normal, []);
            flow = new CampaignBreakdownFlow.ReactorStopOpen(continuation, stop); afterWindow = new(trigger, [opportunity.OpportunityId], null);
        }
        else if (index == 2)
        {
            if (state.BreakdownFlow is not CampaignBreakdownFlow.ReactorStopOpen { Stop.CohortInputs.Count: 0 })
                throw new JsonException("Resolution requires the explicit empty reactor stop.");
            flow = new CampaignBreakdownFlow.Reacting(continuation, null);
        }
        else
        {
            if (state.BreakdownFlow is not CampaignBreakdownFlow.Reacting { ReactorRoute: null } || window.ActiveOpportunityId is not null ||
                !window.ResolvedOpportunityIds.SequenceEqual([opportunity.OpportunityId]) || MoveOptions(state).Count != 0)
                throw new JsonException("Closure requires resolved stop and exhausted opportunities.");
            flow = new CampaignBreakdownFlow.Moving(((CampaignPhasingContinuation.ResumeRoute)continuation).Route); afterWindow = null;
        }
        var emitted = new CampaignCombatReactionLifecycleEvent(state, input, afterWindow, flow);
        var bytes = CampaignCombatReactionLifecycleCodec.SerializeEvent(emitted);
        var receipt = new CampaignOpeningPreambleReceipt(CampaignOpeningPreambleCodec.Hash(CampaignCombatReactionLifecycleCodec.SerializeInput(input)),
            CampaignOpeningPreambleCodec.Hash(bytes), emitted.ReceiptId, input.Actor, version);
        var world = state.World; var tracks = state.Tracks.AsEnumerable(); var progress = state.ActualProgressRefs.AsEnumerable();
        if (index == 0)
        {
            world = ProjectMove(state, emitted);
            var move = (CampaignCombatReactionLifecycleCommand.Move)input.Command;
            tracks = tracks.Append(new(new(world.CreationBinding, CampaignSnapshotSerializer.FormatSide(trigger.ReactingSide), opportunity.ReactingRepresentation.BoundElementIds.Single()),
                [move.OriginLocationId, move.DestinationLocationId]));
            progress = progress.Append(new("reacting-element-moved", receipt.ReceiptId, receipt.EventHash));
        }
        return (new(state.Trigger, version, CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, bytes), world, afterWindow, flow,
            state.Receipts.Append(receipt), tracks, progress, state.Events.Append(emitted)), emitted);
    }
    private static CampaignWorldSnapshotV7 ProjectMove(CampaignCombatReactionLifecycleState state, CampaignCombatReactionLifecycleEvent emitted)
    {
        var world = state.World; var move = (CampaignCombatReactionLifecycleCommand.Move)emitted.Input.Command;
        var rep = state.Trigger.ReactionWindow!.FrozenOpportunities.Single().ReactingRepresentation;
        var element = world.Elements.Single(e => e.ElementId == rep.BoundElementIds.Single());
        var spent = CampaignCombatSpending.ChargeOrdinary(element.OperationalState, new(2, 1), 10, CampaignCombatSpendCeiling.Ordinary,
            element.ElementId, emitted.ReceiptId, world.CohesionCauses);
        var moved = new CampaignElementStateV6(element.ElementId, move.DestinationLocationId, element.ReserveStatus, spent.State,
            element.Components, element.SourceParentFormationId, element.CurrentParentFormationId, element.Ammunition, element.Readiness);
        return new(7, world.CreationBinding, world.Elements.Select(e => e == element ? moved : e),
            world.Representations.Select(r => r.RepresentationId == rep.RepresentationId ? new CampaignMapRepresentationState(r.RepresentationId, move.DestinationLocationId, r.BindingKind, r.BoundElementIds) : r),
            world.BrokenVehicleLots, spent.Causes, world.Relationships, world.CustodyLots, world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
    }
    internal static CampaignWorldSnapshotV7 ExpectedWorld(CampaignCombatReactionLifecycleState state)
    {
        if (state.Trigger.World != CampaignCombatReactionTrigger.ExpectedWorld(state.Trigger)) throw new JsonException("Forged trigger World.");
        var expected = Initial(state.Trigger);
        foreach (var retained in state.Events) expected = Emit(expected, retained.Input).State;
        return expected.World;
    }
}
