namespace Cna.Core.Campaigns;

/// <summary>Dormant creation-rooted routing through atomic first-cycle opening only.</summary>
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

        // Reserve membership exists already at stage-entry's terminal cut. Its strict reader
        // also rejects every unsupported suffix, including otherwise valid future movement.
        var reserve = CampaignCombatReserveOpening.Replay(request, created, preamble, weatherEvents,
            stageEvents, history.CopyRange(9, history.Count - 9));
        return new(history, new CampaignCombatHistoryProjection.ReserveOpening(reserve));
    }
}
