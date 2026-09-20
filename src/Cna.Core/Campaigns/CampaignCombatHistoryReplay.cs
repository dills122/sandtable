using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>Dormant creation-rooted routing through ordinary Movement and Breakdown completion.</summary>
internal static class CampaignCombatHistoryReplay
{
    public static CampaignCombatHistoryResult Replay(CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> createdBytes, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(request);
        var history = CampaignCombatRetainedHistory.Capture(createdBytes, events);
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
        // same-tag Reaction Move4 must still pass the ordinary reader's exact effects.
        var moveStart = cursor;
        var moves = history.CopyRange(moveStart, 0);
        var movement = CampaignCombatInheritedMovement.Replay(request, created, preamble, weatherEvents, stageEvents, reserveEvents, moves);
        while (cursor < history.Count && IsMovement(history.CopyRange(cursor, 1)[0]))
        {
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
            stageEvents, reserveEvents, moves, lifecycleEvents, history.CopyRange(cursor, history.Count - cursor));
        return new(history, new CampaignCombatHistoryProjection.BreakdownCompletion(breakdown));
    }

    private static bool IsMovement(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 32 });
        return document.RootElement.ValueKind == JsonValueKind.Object &&
            document.RootElement.TryGetProperty("eventType", out var kind) &&
            kind.ValueKind == JsonValueKind.String && kind.GetString() == "element-moved";
    }
}
