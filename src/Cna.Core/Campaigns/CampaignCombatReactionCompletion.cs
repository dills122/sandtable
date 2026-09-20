using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Explicit completion, empty stop resolution, and closure after the actual second Reaction move.</summary>
internal static class CampaignCombatReactionCompletion
{
    public static CampaignCombatReactionCompletionState Replay(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> triggers,
        IReadOnlyList<byte[]> participants, IReadOnlyList<byte[]> secondMoves, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(secondMoves); ArgumentNullException.ThrowIfNull(events);
        if (secondMoves.Count != 1 || events.Count > 3) throw new JsonException("Completion requires one actual second move and at most three events.");
        var predecessor = CampaignCombatReactionSecondMove.Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, triggers, participants, secondMoves);
        if (predecessor.StateVersion != 15 || predecessor.Events.Count != 1 ||
            predecessor.ReactionWindow is not { ActiveOpportunityId: not null, ResolvedOpportunityIds.Count: 0, Trigger.FrozenOpportunities.Count: 1 } window ||
            window.ActiveOpportunityId != window.Trigger.FrozenOpportunities.Single().OpportunityId ||
            predecessor.BreakdownFlow is not CampaignBreakdownFlow.Reacting { ReactorRoute: { FirstMoveStateVersion: 14, CohortIds.Count: 0 }, PhasingContinuation: CampaignPhasingContinuation.ResumeRoute })
            throw new JsonException("Completion requires the actual active second move.");
        var state = new CampaignCombatReactionCompletionState(predecessor, predecessor.StateVersion, predecessor.Prefix, predecessor.World,
            window, predecessor.BreakdownFlow, predecessor.Receipts, predecessor.Tracks, predecessor.ActualProgressRefs, []);
        foreach (var retained in events)
        {
            if (retained is not { Length: > 0 and <= 1_048_576 }) throw new JsonException("Missing or oversized completion event.");
            var bytes = retained.ToArray();
            var result = Emit(state, CampaignCombatReactionCompletionCodec.ReadEventInput(bytes));
            if (!bytes.AsSpan().SequenceEqual(result.EventBytes)) throw new JsonException("Completion event differs from actual history-derived transition.");
            state = result.State;
        }
        return state;
    }
    public static CampaignCombatReactionCompletionResult Apply(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> triggers,
        IReadOnlyList<byte[]> participants, IReadOnlyList<byte[]> secondMoves, IReadOnlyList<byte[]> events, CampaignCombatReactionLifecycleInput input)
    {
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, triggers, participants, secondMoves, events);
        Authorize(state, input);
        foreach (var accepted in state.Events)
        {
            if (accepted.Input.Command.Identity.ExpectedPriorVersion != input.Command.Identity.ExpectedPriorVersion) continue;
            if (input != accepted.Input) throw new JsonException("Conflicting completion retry or sequential fork.");
            return new(state, CampaignCombatReactionCompletionCodec.SerializeEvent(accepted), true);
        }
        return Emit(state, input);
    }
    public static CampaignCombatReactionCompletionState ReadState(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents,
        IReadOnlyList<byte[]> stageEvents, IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves,
        IReadOnlyList<byte[]> triggers, IReadOnlyList<byte[]> participants, IReadOnlyList<byte[]> secondMoves, IReadOnlyList<byte[]> events)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized completion state.");
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, triggers, participants, secondMoves, events);
        if (!bytes.SequenceEqual(CampaignCombatReactionCompletionCodec.SerializeState(state))) throw new JsonException("Completion cache differs from retained history.");
        return state;
    }
    public static CampaignCombatReactionLifecycleInput Command(CampaignCombatReactionCompletionState state)
    {
        var index = state.Events.Count;
        if (index > 2 || state.StateVersion != 15 + index || state.ReactionWindow is null || state.Receipts.Count >= 512 ||
            state.Tracks.Count > 512 || state.Tracks.Any(t => t.Route.Count > 512) || state.ActualProgressRefs.Count > 512)
            throw new JsonException("Completion is terminal or exceeds authority capacity.");
        var window = state.ReactionWindow; var opportunity = window.Trigger.FrozenOpportunities.Single();
        if (index == 0)
        {
            if (window.ActiveOpportunityId != opportunity.OpportunityId || window.ResolvedOpportunityIds.Count != 0 ||
                state.BreakdownFlow is not CampaignBreakdownFlow.Reacting { ReactorRoute: { } route } ||
                route != ((CampaignBreakdownFlow.Reacting)state.Predecessor.BreakdownFlow).ReactorRoute)
                throw new JsonException("Completion requires the actual active second-move route.");
            var side = CampaignSnapshotSerializer.FormatSide(window.Trigger.ReactingSide);
            var element = state.World.Elements.Single(e => e.ElementId == route.ElementId);
            if (route.CurrentLocationId != side + "-supply" || element.CurrentLocationId != route.CurrentLocationId ||
                element.OperationalState.CapabilityPointsExpended != new CapabilityPointAmount(4, 1))
                throw new JsonException("Completion requires exhausted ordinary infantry at supply.");
        }
        else if (window.ActiveOpportunityId is not null || !window.ResolvedOpportunityIds.SequenceEqual([opportunity.OpportunityId]) ||
            (index == 1 && state.BreakdownFlow is not CampaignBreakdownFlow.ReactorStopOpen { Stop.CohortInputs.Count: 0 }) ||
            (index == 2 && state.BreakdownFlow is not CampaignBreakdownFlow.Reacting { ReactorRoute: null }))
            throw new JsonException("Completion successor requires the preceding explicit transition.");
        var handle = CampaignCombatReactionCompletionCodec.WindowCapability(state);
        var capability = index == 0 ? CampaignObservationV6DisclosureIdentity.CreateOpportunity(handle, state.StateVersion,
            CampaignObservationV6DisclosureIdentity.CreateCapabilityKey([])) : null;
        var stop = index == 1 ? CampaignCombatReactionCompletionCodec.StopCapability(state) : null;
        var actionId = index switch
        {
            0 => new CompleteReactionParticipantAction(handle, capability!).ActionId,
            1 => CampaignCombatMovementLifecycleCodec.Action(1, stop),
            _ => new CloseReactionWindowNoEligibleAction(handle).ActionId,
        };
        var receipt = state.Opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        var id = new CampaignCombatReactionLifecycleIdentity(2, actionId, receipt.CreationBinding, receipt.CreationEventHash,
            state.Opening.CycleId!, state.StateVersion, state.SequencePosition.PositionId);
        CampaignCombatReactionLifecycleCommand command = index switch
        {
            0 => new CampaignCombatReactionLifecycleCommand.Complete(id, handle, capability!),
            1 => new CampaignCombatReactionLifecycleCommand.Resolve(id, stop!),
            _ => new CampaignCombatReactionLifecycleCommand.Close(id, handle),
        };
        return new(command, index == 0 ? Owner(state) : CampaignOpeningPreambleActor.System);
    }
    private static CampaignOpeningPreambleActor Owner(CampaignCombatReactionCompletionState state) =>
        state.Predecessor.ReactionWindow.Trigger.ReactingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth;
    private static void Authorize(CampaignCombatReactionCompletionState state, CampaignCombatReactionLifecycleInput input)
    {
        _ = CampaignCombatReactionCompletionCodec.SerializeInput(input);
        var actor = input.Command is CampaignCombatReactionLifecycleCommand.Complete ? Owner(state) : CampaignOpeningPreambleActor.System;
        if (input.Actor != actor) throw new JsonException("Wrong authenticated completion actor.");
        var receipt = state.Opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        if (input.Command.Identity.CreationBinding != receipt.CreationBinding || input.Command.Identity.CreationEventHash != receipt.CreationEventHash ||
            input.Command.Identity.CycleId != state.Opening.CycleId) throw new JsonException("Completion creation or cycle differs from actual history.");
    }
    private static CampaignCombatReactionCompletionResult Emit(CampaignCombatReactionCompletionState state, CampaignCombatReactionLifecycleInput input)
    {
        Authorize(state, input);
        if (input != Command(state)) throw new JsonException("Completion command differs from current public capability or occurrence.");
        var index = state.Events.Count; var window = state.ReactionWindow!; var trigger = window.Trigger;
        var continuation = ((CampaignBreakdownFlow.Reacting)state.Predecessor.BreakdownFlow).PhasingContinuation;
        CampaignCombatReactionLifecycleWindow? afterWindow = window; CampaignBreakdownFlow flow;
        if (index == 0)
        {
            var route = ((CampaignBreakdownFlow.Reacting)state.BreakdownFlow).ReactorRoute!;
            var stop = CampaignBreakdownStop.Create(state.Opening.Cycle!.CampaignId, state.Opening.Cycle.RulesetHash,
                checked(state.StateVersion + 1), route, CampaignBreakdownStopReason.ReactionCompleted, BreakdownWeatherKind.Normal, []);
            flow = new CampaignBreakdownFlow.ReactorStopOpen(continuation, stop);
            afterWindow = new(trigger, [trigger.FrozenOpportunities.Single().OpportunityId], null);
        }
        else if (index == 1) flow = new CampaignBreakdownFlow.Reacting(continuation, null);
        else { flow = new CampaignBreakdownFlow.Moving(((CampaignPhasingContinuation.ResumeRoute)continuation).Route); afterWindow = null; }
        var emitted = new CampaignCombatReactionCompletionEvent(state, input, afterWindow, flow);
        var bytes = CampaignCombatReactionCompletionCodec.SerializeEvent(emitted);
        var receipt = new CampaignOpeningPreambleReceipt(CampaignOpeningPreambleCodec.Hash(CampaignCombatReactionCompletionCodec.SerializeInput(input)),
            CampaignOpeningPreambleCodec.Hash(bytes), emitted.ReceiptId, input.Actor, emitted.StateVersion);
        return new(new(state.Predecessor, emitted.StateVersion, CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, bytes), state.World,
            afterWindow, flow, state.Receipts.Append(receipt), state.Tracks, state.ActualProgressRefs, state.Events.Append(emitted)), bytes, false);
    }
}
