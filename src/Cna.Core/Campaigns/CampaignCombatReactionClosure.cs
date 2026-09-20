using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Three mutually exclusive direct exits from the single inactive inherited Reaction window.</summary>
internal static class CampaignCombatReactionClosure
{
    public static CampaignCombatReactionClosureState Replay(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> triggers, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count > 1) throw new JsonException("Direct closure admits at most one event.");
        var predecessor = CampaignCombatReactionLifecycle.Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, triggers, []);
        var state = new CampaignCombatReactionClosureState(predecessor, predecessor.StateVersion, predecessor.Prefix,
            predecessor.BreakdownFlow, predecessor.Receipts, null);
        foreach (var retained in events)
        {
            if (retained is not { Length: > 0 and <= 1_048_576 }) throw new JsonException("Missing or oversized closure event.");
            var bytes = retained.ToArray();
            var result = Emit(state, CampaignCombatReactionClosureCodec.ReadEventInput(bytes));
            if (!bytes.AsSpan().SequenceEqual(result.EventBytes)) throw new JsonException("Closure event differs from actual history-derived transition.");
            state = result.State;
        }
        return state;
    }
    public static CampaignCombatReactionClosureResult Apply(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> triggers,
        IReadOnlyList<byte[]> events, CampaignCombatReactionClosureInput input)
    {
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, triggers, events);
        Authorize(state, input);
        if (state.Accepted is { } accepted)
        {
            if (input != accepted.Input) throw new JsonException("Conflicting direct closure retry or sequential fork.");
            return new(state, CampaignCombatReactionClosureCodec.SerializeEvent(accepted), true);
        }
        return Emit(state, input);
    }
    public static CampaignCombatReactionClosureState ReadState(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents,
        IReadOnlyList<byte[]> stageEvents, IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves,
        IReadOnlyList<byte[]> triggers, IReadOnlyList<byte[]> events)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized closure state.");
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, triggers, events);
        if (!bytes.SequenceEqual(CampaignCombatReactionClosureCodec.SerializeState(state))) throw new JsonException("Closure cache differs from retained history.");
        return state;
    }
    public static CampaignCombatReactionClosureInput Command(CampaignCombatReactionClosureState state, string kind)
    {
        if (state.Accepted is not null || state.StateVersion != 13 ||
            state.ReactionWindow is not { ActiveOpportunityId: null, ResolvedOpportunityIds.Count: 0, Trigger.FrozenOpportunities.Count: 1 } ||
            state.BreakdownFlow is not CampaignBreakdownFlow.Reacting { ReactorRoute: null, PhasingContinuation: CampaignPhasingContinuation.ResumeRoute })
            throw new JsonException("Direct closure requires the inactive unresolved F1 window.");
        var creation = state.Opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        var window = CampaignCombatReactionLifecycleCodec.WindowCapability(state.Predecessor);
        var action = kind switch
        {
            "decline-reaction-window" => new DeclineReactionWindowAction(window).ActionId,
            "close-reaction-window-scripted-unavailable" => new CloseReactionWindowUnavailableAction(window).ActionId,
            "close-reaction-window-timeout" => new CloseReactionWindowTimeoutAction(window).ActionId,
            _ => throw new JsonException("Unknown direct closure kind."),
        };
        return new(new(new(2, action, creation.CreationBinding, creation.CreationEventHash, state.Opening.CycleId!,
            state.StateVersion, state.SequencePosition.PositionId), kind, window), Actor(state, kind));
    }
    private static CampaignOpeningPreambleActor Actor(CampaignCombatReactionClosureState state, string kind) => kind == "decline-reaction-window"
        ? state.Predecessor.Trigger.ReactionWindow!.ReactingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth
        : CampaignOpeningPreambleActor.System;
    private static void Authorize(CampaignCombatReactionClosureState state, CampaignCombatReactionClosureInput input)
    {
        _ = CampaignCombatReactionClosureCodec.SerializeInput(input);
        if (input.Actor != Actor(state, input.Command.Kind)) throw new JsonException("Wrong authenticated direct closure actor.");
        var receipt = state.Opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        if (input.Command.Identity.CreationBinding != receipt.CreationBinding || input.Command.Identity.CreationEventHash != receipt.CreationEventHash ||
            input.Command.Identity.CycleId != state.Opening.CycleId) throw new JsonException("Closure creation or cycle differs from actual history.");
    }
    private static CampaignCombatReactionClosureResult Emit(CampaignCombatReactionClosureState state, CampaignCombatReactionClosureInput input)
    {
        Authorize(state, input);
        if (input != Command(state, input.Command.Kind)) throw new JsonException("Closure command differs from current public capability or occurrence.");
        if (state.StateVersion == long.MaxValue || state.Receipts.Count >= 512) throw new JsonException("Closure version or receipt capacity exceeded.");
        var route = ((CampaignPhasingContinuation.ResumeRoute)((CampaignBreakdownFlow.Reacting)state.BreakdownFlow).PhasingContinuation).Route;
        var emitted = new CampaignCombatReactionClosureEvent(state, input, new(route));
        var bytes = CampaignCombatReactionClosureCodec.SerializeEvent(emitted);
        var receipt = new CampaignOpeningPreambleReceipt(CampaignOpeningPreambleCodec.Hash(CampaignCombatReactionClosureCodec.SerializeInput(input)),
            CampaignOpeningPreambleCodec.Hash(bytes), emitted.ReceiptId, input.Actor, emitted.StateVersion);
        return new(new(state.Predecessor, emitted.StateVersion, CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, bytes),
            emitted.Flow, state.Receipts.Append(receipt), emitted), bytes, false);
    }
}
