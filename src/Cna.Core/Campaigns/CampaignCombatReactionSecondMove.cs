using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>One owner-authored rear-to-supply move from the actual first participant move.</summary>
internal static class CampaignCombatReactionSecondMove
{
    public static CampaignCombatReactionSecondMoveState Replay(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> triggers, IReadOnlyList<byte[]> participants, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(participants);
        if (participants.Count != 1 || events.Count > 1) throw new JsonException("Second move requires one actual participant move and at most one event.");
        var predecessor = CampaignCombatReactionLifecycle.Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, triggers, participants);
        if (predecessor.StateVersion != 14 || predecessor.Events.Count != 1 ||
            predecessor.ReactionWindow is not { ActiveOpportunityId: not null, ResolvedOpportunityIds.Count: 0, Trigger.FrozenOpportunities.Count: 1 } window ||
            window.ActiveOpportunityId != window.Trigger.FrozenOpportunities.Single().OpportunityId ||
            predecessor.BreakdownFlow is not CampaignBreakdownFlow.Reacting { ReactorRoute: not null, PhasingContinuation: CampaignPhasingContinuation.ResumeRoute } ||
            CampaignCombatReactionLifecycle.MoveOptions(predecessor).Count != 1)
            throw new JsonException("Second move requires the actual active first participant move.");
        var state = new CampaignCombatReactionSecondMoveState(predecessor, predecessor.StateVersion, predecessor.Prefix,
            predecessor.World, predecessor.BreakdownFlow, predecessor.Receipts, predecessor.Tracks, predecessor.ActualProgressRefs, []);
        foreach (var retained in events)
        {
            if (retained is not { Length: > 0 and <= 1_048_576 }) throw new JsonException("Missing or oversized second move event.");
            var bytes = retained.ToArray();
            var result = Emit(state, CampaignCombatReactionSecondMoveCodec.ReadEventInput(bytes));
            if (!bytes.AsSpan().SequenceEqual(result.EventBytes)) throw new JsonException("Second move event differs from actual history-derived transition.");
            state = result.State;
        }
        return state;
    }
    public static CampaignCombatReactionSecondMoveResult Apply(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> triggers,
        IReadOnlyList<byte[]> participants, IReadOnlyList<byte[]> events, CampaignCombatReactionSecondMoveInput input)
    {
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, triggers, participants, events);
        Authorize(state, input);
        foreach (var accepted in state.Events)
        {
            if (accepted.Input.Command.Identity.ExpectedPriorVersion != input.Command.Identity.ExpectedPriorVersion) continue;
            if (input != accepted.Input) throw new JsonException("Conflicting active second move retry or sequential fork.");
            return new(state, CampaignCombatReactionSecondMoveCodec.SerializeEvent(accepted), true);
        }
        return Emit(state, input);
    }
    public static CampaignCombatReactionSecondMoveState ReadState(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents,
        IReadOnlyList<byte[]> stageEvents, IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves,
        IReadOnlyList<byte[]> triggers, IReadOnlyList<byte[]> participants, IReadOnlyList<byte[]> events)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized second move state.");
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, triggers, participants, events);
        if (!bytes.SequenceEqual(CampaignCombatReactionSecondMoveCodec.SerializeState(state))) throw new JsonException("Second move cache differs from retained history.");
        return state;
    }
    public static CampaignCombatReactionSecondMoveInput Command(CampaignCombatReactionSecondMoveState state)
    {
        if (state.StateVersion != 14 || state.Events.Count != 0 || state.Receipts.Count >= 512 || state.ActualProgressRefs.Count >= 512 ||
            state.Tracks.Count > 512 || state.Tracks.Any(t => t.Route.Count >= 512) ||
            state.BreakdownFlow is not CampaignBreakdownFlow.Reacting { ReactorRoute: { } route, PhasingContinuation: CampaignPhasingContinuation.ResumeRoute } ||
            state.ReactionWindow is not { ActiveOpportunityId: not null, ResolvedOpportunityIds.Count: 0, Trigger.FrozenOpportunities.Count: 1 } window ||
            window.ActiveOpportunityId != window.Trigger.FrozenOpportunities.Single().OpportunityId)
            throw new JsonException("Second move requires the active first participant route within capacity.");
        var option = CampaignCombatReactionLifecycle.MoveOptions(state.Predecessor).Single();
        var element = state.World.Elements.Single(e => e.ElementId == route.ElementId);
        var representation = state.World.Representations.Single(r => r.RepresentationId == route.RepresentationId);
        var track = state.Tracks.SingleOrDefault(t => t.Unit.ElementId == route.ElementId);
        if (route != ((CampaignBreakdownFlow.Reacting)state.Predecessor.BreakdownFlow).ReactorRoute ||
            route.FirstMoveStateVersion != 14 || route.CohortIds.Count != 0 || route.Owner != window.Trigger.ReactingSide ||
            route.CurrentLocationId != option.OriginLocationId || element.CurrentLocationId != option.OriginLocationId ||
            representation.CurrentLocationId != option.OriginLocationId || element.OperationalState.CapabilityPointsExpended != new CapabilityPointAmount(2, 1) ||
            track is null || !track.Route.SequenceEqual([route.OriginLocationId, option.OriginLocationId]))
            throw new JsonException("Second move route, track or participant differs from first move.");
        var handle = CampaignCombatReactionLifecycleCodec.WindowCapability(state.Predecessor);
        var opportunity = CampaignObservationV6DisclosureIdentity.CreateOpportunity(handle, state.StateVersion,
            CampaignObservationV6DisclosureIdentity.CreateCapabilityKey([option]));
        var action = new MoveReactingElementAction(handle, opportunity, option.OriginLocationId, option.DestinationLocationId, option.CostBreakdown);
        var creation = state.Opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        return new(new(new(2, action.ActionId, creation.CreationBinding, creation.CreationEventHash, state.Opening.CycleId!,
            state.StateVersion, state.SequencePosition.PositionId), handle, opportunity, option.OriginLocationId, option.DestinationLocationId),
            window.Trigger.ReactingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth);
    }
    private static void Authorize(CampaignCombatReactionSecondMoveState state, CampaignCombatReactionSecondMoveInput input)
    {
        _ = CampaignCombatReactionSecondMoveCodec.SerializeInput(input);
        var actor = state.ReactionWindow.Trigger.ReactingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth;
        if (input.Actor != actor) throw new JsonException("Wrong authenticated second move actor.");
        var receipt = state.Opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        if (input.Command.Identity.CreationBinding != receipt.CreationBinding || input.Command.Identity.CreationEventHash != receipt.CreationEventHash ||
            input.Command.Identity.CycleId != state.Opening.CycleId) throw new JsonException("Second move creation or cycle differs from actual history.");
    }
    private static CampaignCombatReactionSecondMoveResult Emit(CampaignCombatReactionSecondMoveState state, CampaignCombatReactionSecondMoveInput input)
    {
        Authorize(state, input);
        if (input != Command(state)) throw new JsonException("Second move command differs from current public capability or occurrence.");
        var flow = (CampaignBreakdownFlow.Reacting)state.BreakdownFlow;
        var emitted = new CampaignCombatReactionSecondMoveEvent(state, input, new(flow.PhasingContinuation, flow.ReactorRoute!.AtLocation(input.Command.DestinationLocationId)));
        var bytes = CampaignCombatReactionSecondMoveCodec.SerializeEvent(emitted);
        var receipt = new CampaignOpeningPreambleReceipt(CampaignOpeningPreambleCodec.Hash(CampaignCombatReactionSecondMoveCodec.SerializeInput(input)),
            CampaignOpeningPreambleCodec.Hash(bytes), emitted.ReceiptId, input.Actor, emitted.StateVersion);
        var world = ProjectWorld(state.World, flow.ReactorRoute, input.Command.DestinationLocationId, receipt.ReceiptId);
        var tracks = state.Tracks.Select(t => t.Unit.ElementId == flow.ReactorRoute.ElementId
            ? new CampaignCombatInheritedTrack(t.Unit, t.Route.Append(input.Command.DestinationLocationId)) : t);
        return new(new(state.Predecessor, emitted.StateVersion, CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, bytes), world,
            emitted.Flow, state.Receipts.Append(receipt), tracks,
            state.ActualProgressRefs.Append(new("reacting-element-moved", receipt.ReceiptId, receipt.EventHash)), state.Events.Append(emitted)), bytes, false);
    }
    private static CampaignWorldSnapshotV7 ProjectWorld(CampaignWorldSnapshotV7 world, CampaignBreakdownRoute route, string destination, string receipt)
    {
        var element = world.Elements.Single(e => e.ElementId == route.ElementId);
        var spent = CampaignCombatSpending.ChargeOrdinary(element.OperationalState, new(2, 1), 10, CampaignCombatSpendCeiling.Ordinary,
            element.ElementId, receipt, world.CohesionCauses);
        var moved = new CampaignElementStateV6(element.ElementId, destination, element.ReserveStatus, spent.State,
            element.Components, element.SourceParentFormationId, element.CurrentParentFormationId, element.Ammunition, element.Readiness);
        return new(7, world.CreationBinding, world.Elements.Select(e => e == element ? moved : e),
            world.Representations.Select(r => r.RepresentationId == route.RepresentationId ? new CampaignMapRepresentationState(r.RepresentationId, destination, r.BindingKind, r.BoundElementIds) : r),
            world.BrokenVehicleLots, spent.Causes, world.Relationships, world.CustodyLots, world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
    }
    internal static CampaignWorldSnapshotV7 ExpectedWorld(CampaignCombatReactionSecondMoveState state)
    {
        var predecessor = state.Predecessor;
        // Validate all predecessor World fields before this bounded writer can omit unsupported fields.
        _ = CampaignCombatReactionLifecycleCodec.WriteWorld(predecessor);
        var expected = new CampaignCombatReactionSecondMoveState(predecessor, predecessor.StateVersion, predecessor.Prefix,
            predecessor.World, predecessor.BreakdownFlow, predecessor.Receipts, predecessor.Tracks, predecessor.ActualProgressRefs, []);
        foreach (var retained in state.Events) expected = Emit(expected, retained.Input).State;
        return expected.World;
    }
}
