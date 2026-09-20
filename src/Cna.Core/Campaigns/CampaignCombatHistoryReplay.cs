using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>Dormant creation-rooted routing through Movement, Reaction forks and actual no-attack steps.</summary>
internal static class CampaignCombatHistoryReplay
{
    public static CampaignCombatHistoryResult Replay(CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (createdBytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized independently retained Created11.");
        // Caller list access must not change the creation evidence used by this replay.
        var ownedCreated = createdBytes.ToArray();
        var history = CampaignCombatRetainedHistory.Capture(ownedCreated, events);
        var created = history.CreatedSpan;
        // This profile has fixed causal gates. Each partition is admitted in order by the
        // actual reader before its successor is reachable; count is never proof of validity.
        var preamble = history.CopyRange(0, Math.Min(history.Count, 4));
        var opening = CampaignOpeningPreamble.Replay(request, created, preamble);
        if (history.Count <= 4) return new(history, new CampaignCombatHistoryProjection.Preamble(opening));

        var weatherEvents = history.CopyRange(4, 1);
        var weather = CampaignCombatWeather.Replay(request, created, preamble, weatherEvents);
        if (history.Count == 5) return new(history, new CampaignCombatHistoryProjection.Weather(weather));

        var stageEvents = history.CopyRange(5, Math.Min(history.Count - 5, 4));
        var stage = CampaignCombatStageEntry.Replay(request, created, preamble, weatherEvents, stageEvents);
        if (history.Count < 9) return new(history, new CampaignCombatHistoryProjection.StageEntry(stage));

        // A designation and a completion can both be the tenth retained event. Only
        // the accepted typed completion proves that the Movement partition may start.
        var cursor = 9;
        var reserveEvents = history.CopyRange(9, 0);
        var reserve = CampaignCombatReserveOpening.Replay(request, created, preamble, weatherEvents, stageEvents, reserveEvents);
        while (cursor < history.Count && reserve.Completion is null)
        {
            reserveEvents = history.CopyRange(9, ++cursor - 9);
            reserve = CampaignCombatReserveOpening.Replay(request, created, preamble, weatherEvents, stageEvents, reserveEvents);
        }
        if (cursor == history.Count) return new(history, new CampaignCombatHistoryProjection.ReserveOpening(reserve));

        // Family tags select a candidate reader, never authority. In particular the
        // same-tag Reaction Move4 must pass its strict trigger reader's exact effects.
        var moveStart = cursor;
        var moves = history.CopyRange(moveStart, 0);
        var movement = CampaignCombatInheritedMovement.Replay(request, created, preamble, weatherEvents, stageEvents, reserveEvents, moves);
        while (cursor < history.Count && HasType(history.CopyRange(cursor, 1)[0], "element-moved"))
        {
            if (HasReactionHint(history.CopyRange(cursor, 1)[0]))
                return ReplayReaction(request, history, preamble, weatherEvents, stageEvents, reserveEvents, moves, cursor);
            moves = history.CopyRange(moveStart, ++cursor - moveStart);
            movement = CampaignCombatInheritedMovement.Replay(request, created, preamble, weatherEvents, stageEvents, reserveEvents, moves);
        }
        if (cursor == history.Count) return new(history, new CampaignCombatHistoryProjection.Movement(movement));

        var lifecycleEvents = history.CopyRange(cursor, Math.Min(3, history.Count - cursor));
        var lifecycle = CampaignCombatMovementLifecycle.Replay(request, created, preamble, weatherEvents,
            stageEvents, reserveEvents, moves, lifecycleEvents);
        cursor += lifecycleEvents.Length;
        if (cursor == history.Count) return new(history, new CampaignCombatHistoryProjection.MovementLifecycle(lifecycle));

        var breakdown = CampaignCombatBreakdownCompletion.Replay(request, created, preamble, weatherEvents,
            stageEvents, reserveEvents, moves, lifecycleEvents, history.CopyRange(cursor++, 1));
        if (cursor == history.Count) return new(history, new CampaignCombatHistoryProjection.BreakdownCompletion(breakdown));
        if (breakdown.Completion is null) throw new JsonException("Selection requires actual Breakdown completion.");

        // Successor admission replays only this exact G2 prefix. Passing the outer tail
        // would recursively admit the same successor instead of proving its predecessor.
        var predecessorEvents = history.CopyRange(0, cursor);
        var selectionEvents = history.CopyRange(cursor, Math.Min(2, history.Count - cursor));
        var selection = CampaignCombatInheritedSelection.Replay(request, created, predecessorEvents, selectionEvents);
        cursor += selectionEvents.Length;
        if (cursor == history.Count) return new(history, new CampaignCombatHistoryProjection.Selection(selection));
        if (!selection.SelectionClosed) throw new JsonException("Traversal requires closed actual selection.");

        // Pass every remaining record to the bounded strict reader; never truncate a tail.
        var traversal = CampaignCombatInheritedNoAttack.Replay(request, created, predecessorEvents, selectionEvents,
            history.CopyRange(cursor, history.Count - cursor));
        return new(history, new CampaignCombatHistoryProjection.NoAttack(traversal));
    }

    private static CampaignCombatHistoryResult ReplayReaction(CampaignCombatCreationRequest request,
        CampaignCombatRetainedHistory history, byte[][] preamble, byte[][] weather, byte[][] stage,
        byte[][] reserve, byte[][] moves, int cursor)
    {
        var created = history.CreatedSpan;
        var triggers = history.CopyRange(cursor++, 1);
        var trigger = CampaignCombatReactionTrigger.Replay(request, created, preamble, weather, stage, reserve, moves, triggers);
        if (cursor == history.Count) return new(history, new CampaignCombatHistoryProjection.ReactionTrigger(trigger));

        // The actual trigger now proves the inactive window. A tag only chooses which
        // strict reader must validate the entire remaining fork, including excess tails.
        if (HasType(history.CopyRange(cursor, 1)[0], "reaction-window-closed"))
            return new(history, new CampaignCombatHistoryProjection.ReactionClosure(
                CampaignCombatReactionClosure.Replay(request, created, preamble, weather, stage, reserve, moves, triggers,
                    history.CopyRange(cursor, history.Count - cursor))));

        var participantStart = cursor;
        var participants = history.CopyRange(cursor++, 1);
        var participant = CampaignCombatReactionLifecycle.Replay(request, created, preamble, weather, stage, reserve, moves, triggers, participants);
        if (cursor == history.Count) return new(history, new CampaignCombatHistoryProjection.ReactionLifecycle(participant));

        if (HasType(history.CopyRange(cursor, 1)[0], "reaction-window-closed"))
            return new(history, new CampaignCombatHistoryProjection.ReactionFallback(
                CampaignCombatReactionFallback.Replay(request, created, preamble, weather, stage, reserve, moves, triggers, participants,
                    history.CopyRange(cursor, history.Count - cursor))));

        if (!HasType(history.CopyRange(cursor, 1)[0], "reacting-element-moved"))
            return new(history, new CampaignCombatHistoryProjection.ReactionLifecycle(
                CampaignCombatReactionLifecycle.Replay(request, created, preamble, weather, stage, reserve, moves, triggers,
                    history.CopyRange(participantStart, history.Count - participantStart))));

        var secondMoves = history.CopyRange(cursor++, 1);
        var second = CampaignCombatReactionSecondMove.Replay(request, created, preamble, weather, stage, reserve, moves, triggers, participants, secondMoves);
        if (cursor == history.Count) return new(history, new CampaignCombatHistoryProjection.ReactionSecondMove(second));
        return new(history, new CampaignCombatHistoryProjection.ReactionCompletion(
            CampaignCombatReactionCompletion.Replay(request, created, preamble, weather, stage, reserve, moves, triggers, participants, secondMoves,
                history.CopyRange(cursor, history.Count - cursor))));
    }

    private static bool HasReactionHint(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 32 });
        return document.RootElement.TryGetProperty("openedReactionWindow", out var window) && window.ValueKind != JsonValueKind.Null;
    }

    private static bool HasType(byte[] bytes, string eventType)
    {
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 32 });
        return document.RootElement.ValueKind == JsonValueKind.Object &&
            document.RootElement.TryGetProperty("eventType", out var kind) &&
            kind.ValueKind == JsonValueKind.String && kind.GetString() == eventType;
    }
}
