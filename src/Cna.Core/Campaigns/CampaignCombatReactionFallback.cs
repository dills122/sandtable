using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Two System fallback forks from the active first participant move, followed by mandatory stop resolution.</summary>
internal static class CampaignCombatReactionFallback
{
    public static CampaignCombatReactionFallbackState Replay(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> triggers, IReadOnlyList<byte[]> participants, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(participants);
        if (participants.Count != 1 || events.Count > 2) throw new JsonException("Fallback requires one actual participant move and at most two events.");
        var predecessor = CampaignCombatReactionLifecycle.Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, triggers, participants);
        if (predecessor.StateVersion != 14 || predecessor.Events.Count != 1 ||
            predecessor.ReactionWindow is not { ActiveOpportunityId: not null, ResolvedOpportunityIds.Count: 0, Trigger.FrozenOpportunities.Count: 1 } window ||
            window.ActiveOpportunityId != window.Trigger.FrozenOpportunities.Single().OpportunityId ||
            predecessor.BreakdownFlow is not CampaignBreakdownFlow.Reacting { ReactorRoute: not null, PhasingContinuation: CampaignPhasingContinuation.ResumeRoute } ||
            CampaignCombatReactionLifecycle.MoveOptions(predecessor).Count != 1)
            throw new JsonException("Fallback requires the actual active first participant move.");
        var state = new CampaignCombatReactionFallbackState(predecessor, predecessor.StateVersion, predecessor.Prefix,
            predecessor.BreakdownFlow, predecessor.Receipts, []);
        foreach (var retained in events)
        {
            if (retained is not { Length: > 0 and <= 1_048_576 }) throw new JsonException("Missing or oversized fallback event.");
            var bytes = retained.ToArray();
            var result = Emit(state, CampaignCombatReactionFallbackCodec.ReadEventInput(bytes));
            if (!bytes.AsSpan().SequenceEqual(result.EventBytes)) throw new JsonException("Fallback event differs from actual history-derived transition.");
            state = result.State;
        }
        return state;
    }
    public static CampaignCombatReactionFallbackResult Apply(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> triggers,
        IReadOnlyList<byte[]> participants, IReadOnlyList<byte[]> events, CampaignCombatReactionFallbackInput input)
    {
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, triggers, participants, events);
        Authorize(state, input);
        foreach (var accepted in state.Events)
        {
            if (accepted.Input.Command.Identity.ExpectedPriorVersion != input.Command.Identity.ExpectedPriorVersion) continue;
            if (input != accepted.Input) throw new JsonException("Conflicting active fallback retry or sequential fork.");
            return new(state, CampaignCombatReactionFallbackCodec.SerializeEvent(accepted), true);
        }
        return Emit(state, input);
    }
    public static CampaignCombatReactionFallbackState ReadState(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents,
        IReadOnlyList<byte[]> stageEvents, IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves,
        IReadOnlyList<byte[]> triggers, IReadOnlyList<byte[]> participants, IReadOnlyList<byte[]> events)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized fallback state.");
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, triggers, participants, events);
        if (!bytes.SequenceEqual(CampaignCombatReactionFallbackCodec.SerializeState(state))) throw new JsonException("Fallback cache differs from retained history.");
        return state;
    }
    public static CampaignCombatReactionFallbackInput Command(CampaignCombatReactionFallbackState state, string kind)
    {
        var resolve = kind == "resolve-breakdown-stop";
        if (state.StateVersion == long.MaxValue || state.Receipts.Count >= 512)
            throw new JsonException("Fallback version or receipt capacity exceeded.");
        if (resolve)
        {
            if (state.Events.Count != 1 || state.StateVersion != 15 || state.ReactionWindow is not null ||
                state.BreakdownFlow is not CampaignBreakdownFlow.ReactorStopClosed { Stop.CohortInputs.Count: 0, PhasingContinuation: CampaignPhasingContinuation.ResumeRoute })
                throw new JsonException("Resolution requires the recorded closed reactor stop.");
        }
        else if (state.Events.Count != 0 || state.StateVersion != 14 ||
            state.ReactionWindow is not { ActiveOpportunityId: not null, ResolvedOpportunityIds.Count: 0, Trigger.FrozenOpportunities.Count: 1 } ||
            state.BreakdownFlow is not CampaignBreakdownFlow.Reacting { ReactorRoute: not null, PhasingContinuation: CampaignPhasingContinuation.ResumeRoute })
            throw new JsonException("Fallback close requires the active first participant route.");
        var creation = state.Opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        var handle = resolve ? CampaignCombatReactionFallbackCodec.StopCapability(state) : CampaignCombatReactionLifecycleCodec.WindowCapability(state.Predecessor);
        var action = kind switch
        {
            "close-reaction-window-scripted-unavailable" => new CloseReactionWindowUnavailableAction(handle).ActionId,
            "close-reaction-window-timeout" => new CloseReactionWindowTimeoutAction(handle).ActionId,
            "resolve-breakdown-stop" => CampaignCombatMovementLifecycleCodec.Action(1, handle),
            _ => throw new JsonException("Unknown active fallback kind."),
        };
        return new(new(new(2, action, creation.CreationBinding, creation.CreationEventHash, state.Opening.CycleId!,
            state.StateVersion, state.SequencePosition.PositionId), kind, handle), CampaignOpeningPreambleActor.System);
    }
    private static void Authorize(CampaignCombatReactionFallbackState state, CampaignCombatReactionFallbackInput input)
    {
        _ = CampaignCombatReactionFallbackCodec.SerializeInput(input);
        if (input.Actor != CampaignOpeningPreambleActor.System) throw new JsonException("Wrong authenticated active fallback actor.");
        var receipt = state.Opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        if (input.Command.Identity.CreationBinding != receipt.CreationBinding || input.Command.Identity.CreationEventHash != receipt.CreationEventHash ||
            input.Command.Identity.CycleId != state.Opening.CycleId) throw new JsonException("Fallback creation or cycle differs from actual history.");
    }
    private static CampaignCombatReactionFallbackResult Emit(CampaignCombatReactionFallbackState state, CampaignCombatReactionFallbackInput input)
    {
        Authorize(state, input);
        if (input != Command(state, input.Command.Kind)) throw new JsonException("Fallback command differs from current public capability or occurrence.");
        if (state.StateVersion == long.MaxValue || state.Receipts.Count >= 512) throw new JsonException("Fallback version or receipt capacity exceeded.");
        CampaignBreakdownFlow flow;
        if (input.Command.Kind == "resolve-breakdown-stop")
        {
            var stopped = (CampaignBreakdownFlow.ReactorStopClosed)state.BreakdownFlow;
            flow = new CampaignBreakdownFlow.Moving(((CampaignPhasingContinuation.ResumeRoute)stopped.PhasingContinuation).Route);
        }
        else
        {
            var reacting = (CampaignBreakdownFlow.Reacting)state.BreakdownFlow;
            var cycle = state.Opening.Cycle!;
            var stop = CampaignBreakdownStop.Create(cycle.CampaignId, cycle.RulesetHash, checked(state.StateVersion + 1), reacting.ReactorRoute!,
                input.Command.Kind == "close-reaction-window-timeout" ? CampaignBreakdownStopReason.ReactionTimeout : CampaignBreakdownStopReason.ReactionUnavailable,
                BreakdownWeatherKind.Normal, []);
            flow = new CampaignBreakdownFlow.ReactorStopClosed(reacting.PhasingContinuation, stop);
        }
        var emitted = new CampaignCombatReactionFallbackEvent(state, input, flow);
        var bytes = CampaignCombatReactionFallbackCodec.SerializeEvent(emitted);
        var receipt = new CampaignOpeningPreambleReceipt(CampaignOpeningPreambleCodec.Hash(CampaignCombatReactionFallbackCodec.SerializeInput(input)),
            CampaignOpeningPreambleCodec.Hash(bytes), emitted.ReceiptId, input.Actor, emitted.StateVersion);
        return new(new(state.Predecessor, emitted.StateVersion, CampaignOpeningPreambleCodec.EventPrefix(state.Prefix, bytes),
            emitted.Flow, state.Receipts.Append(receipt), state.Events.Append(emitted)), bytes, false);
    }
}
