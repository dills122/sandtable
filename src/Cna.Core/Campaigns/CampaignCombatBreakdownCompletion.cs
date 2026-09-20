using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>System Breakdown completion after actual Movement stop, resolution and end proof.</summary>
internal static class CampaignCombatBreakdownCompletion
{
    private const string Boundary = "land.position.operation-1.first-player.movement-and-combat.breakdown-determination";
    public static CampaignCombatBreakdownCompletionState Replay(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> lifecycleEvents, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(lifecycleEvents); ArgumentNullException.ThrowIfNull(events);
        if (lifecycleEvents.Count != 3 || events.Count > 1)
            throw new JsonException("Breakdown completion requires three actual lifecycle records and at most one completion.");
        var lifecycle = CampaignCombatMovementLifecycle.Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, lifecycleEvents);
        if (lifecycle.SequencePosition.PositionId != Boundary || lifecycle.BreakdownFlow is not CampaignBreakdownFlow.Idle ||
            lifecycle.InterruptContext is not null || lifecycle.MovementEnd is null)
            throw new JsonException("Breakdown completion requires actual idle boundary with completed Movement proof.");
        var state = new CampaignCombatBreakdownCompletionState(lifecycle, null);
        foreach (var retained in events)
        {
            if (retained is not { Length: > 0 and <= 1_048_576 }) throw new JsonException("Missing or oversized Breakdown completion event.");
            var bytes = retained.ToArray();
            var emitted = Emit(state, CampaignCombatBreakdownCompletionCodec.ReadEventInput(bytes));
            if (!bytes.AsSpan().SequenceEqual(CampaignCombatBreakdownCompletionCodec.SerializeEvent(emitted)))
                throw new JsonException("Breakdown event differs from canonical actual predecessor authority.");
            state = new(lifecycle, emitted);
        }
        return state;
    }
    public static CampaignCombatBreakdownCompletionResult Apply(CampaignCombatCreationRequest request, ReadOnlySpan<byte> createdBytes,
        IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents, IReadOnlyList<byte[]> stageEvents,
        IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> lifecycleEvents,
        IReadOnlyList<byte[]> events, CampaignCombatBreakdownCompletionInput input)
    {
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, lifecycleEvents, events);
        Authorize(state, input);
        if (state.Completion is { } accepted)
        {
            if (accepted.Input != input) throw new JsonException("Conflicting Breakdown completion retry.");
            return new(state, CampaignCombatBreakdownCompletionCodec.SerializeEvent(accepted), true);
        }
        var emitted = Emit(state, input);
        return new(new(state.Lifecycle, emitted), CampaignCombatBreakdownCompletionCodec.SerializeEvent(emitted), false);
    }
    public static CampaignCombatBreakdownCompletionState ReadState(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> preamble, IReadOnlyList<byte[]> weatherEvents,
        IReadOnlyList<byte[]> stageEvents, IReadOnlyList<byte[]> reserveEvents, IReadOnlyList<byte[]> moves,
        IReadOnlyList<byte[]> lifecycleEvents, IReadOnlyList<byte[]> events)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized Combat entry projection.");
        var state = Replay(request, createdBytes, preamble, weatherEvents, stageEvents, reserveEvents, moves, lifecycleEvents, events);
        if (!bytes.SequenceEqual(CampaignCombatBreakdownCompletionCodec.SerializeState(state)))
            throw new JsonException("Combat entry projection differs from full actual retained history.");
        return state;
    }
    public static CampaignCombatBreakdownCompletionInput Command(CampaignCombatBreakdownCompletionState state)
    {
        if (state.Completion is not null) throw new JsonException("Breakdown completion already consumed.");
        var creation = state.Lifecycle.Movement.Opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        return new(new(2, "complete-breakdown-segment", 1, CampaignCombatBreakdownCompletionCodec.ActionId, creation.CreationBinding,
            creation.CreationEventHash, state.Lifecycle.Movement.Opening.CycleId!, state.StateVersion, state.SequencePosition.PositionId), CampaignOpeningPreambleActor.System);
    }
    private static void Authorize(CampaignCombatBreakdownCompletionState state, CampaignCombatBreakdownCompletionInput input)
    {
        _ = CampaignCombatBreakdownCompletionCodec.SerializeInput(input);
        if (input.Actor != CampaignOpeningPreambleActor.System || input.Command.ActionId != CampaignCombatBreakdownCompletionCodec.ActionId)
            throw new JsonException("Breakdown completion requires trusted System actor and exact action identity.");
        var creation = state.Lifecycle.Movement.Opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        if (input.Command.CreationBinding != creation.CreationBinding || input.Command.CreationEventHash != creation.CreationEventHash ||
            input.Command.CycleId != state.Lifecycle.Movement.Opening.CycleId)
            throw new JsonException("Breakdown command differs from actual creation and cycle identity.");
    }
    private static CampaignCombatBreakdownCompletionEvent Emit(CampaignCombatBreakdownCompletionState state, CampaignCombatBreakdownCompletionInput input)
    {
        Authorize(state, input);
        if (input != Command(state) || state.SequencePosition.PositionId != Boundary || state.Lifecycle.MovementEnd is null ||
            state.Lifecycle.BreakdownFlow is not CampaignBreakdownFlow.Idle || state.Lifecycle.InterruptContext is not null)
            throw new JsonException("Breakdown completion does not match actual current boundary.");
        if (state.StateVersion == long.MaxValue || state.Receipts.Count >= 512)
            throw new JsonException("Breakdown completion authority capacity exhausted.");
        var next = Cna1979LandSequence.CreateTurn(1).Single(p =>
            p.PositionId == "land.position.operation-1.first-player.movement-and-combat.combat.position-determination");
        var position = new LandSequencePosition(5, next.PositionId, next.GameTurn, next.OperationStage, next.StageId,
            next.PhaseId, next.SegmentId, next.StepId, next.ActorRole, next.ActiveSide, next.Sources);
        return new(state.Lifecycle, input, position);
    }
}
