using System.Runtime.CompilerServices;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

internal static class CampaignObservationWeatherSelector
{
    // history grows by one entry per operation stage as a campaign progresses, and this
    // selector is called once per observer per observation (the per-decision-point hot
    // path). Indexing it once per distinct history-list instance instead of linearly
    // rescanning it on every call avoids the resulting O(turns^2) cost across a replay.
    private static readonly ConditionalWeakTable<
        IReadOnlyList<CampaignOperationStageWeather>,
        Dictionary<(int GameTurn, int OperationStage), CampaignOperationStageWeather>> IndexCache = new();

    public static CampaignObservationWeather? Select(int gameTurn, int operationStage,
        IReadOnlyList<CampaignOperationStageWeather> history)
    {
        ArgumentNullException.ThrowIfNull(history);
        var index = IndexCache.GetValue(history, BuildIndex);
        if (!index.TryGetValue((gameTurn, operationStage), out var value))
        {
            return null;
        }

        return new CampaignObservationWeather(1, value.GameTurn, value.OperationStage,
            FormatSeason(value.Season),
            FormatKind(value.Kind),
            FormatScope(value.Scope),
            value.AffectedAreas.Select(FormatArea).ToArray());
    }

    private static Dictionary<(int GameTurn, int OperationStage), CampaignOperationStageWeather>
        BuildIndex(IReadOnlyList<CampaignOperationStageWeather> history)
    {
        var index = new Dictionary<(int GameTurn, int OperationStage), CampaignOperationStageWeather>(
            history.Count);
        foreach (var value in history)
        {
            if (!index.TryAdd((value.GameTurn, value.OperationStage), value))
            {
                throw new ArgumentException(
                    "Weather history contains a duplicate pair.", nameof(history));
            }
        }

        return index;
    }

    private static CampaignObservationWeatherSeason FormatSeason(WeatherSeason season) => season switch
    {
        WeatherSeason.Fall => CampaignObservationWeatherSeason.Fall,
        WeatherSeason.Winter => CampaignObservationWeatherSeason.Winter,
        WeatherSeason.Spring => CampaignObservationWeatherSeason.Spring,
        WeatherSeason.Summer => CampaignObservationWeatherSeason.Summer,
        _ => throw new ArgumentOutOfRangeException(nameof(season)),
    };

    private static CampaignObservationWeatherKind FormatKind(WeatherKind kind) => kind switch
    {
        WeatherKind.Normal => CampaignObservationWeatherKind.Normal,
        WeatherKind.Hot => CampaignObservationWeatherKind.Hot,
        WeatherKind.Sandstorm => CampaignObservationWeatherKind.Sandstorm,
        WeatherKind.Rainstorm => CampaignObservationWeatherKind.Rainstorm,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static CampaignObservationWeatherScope FormatScope(WeatherScope scope) => scope switch
    {
        WeatherScope.None => CampaignObservationWeatherScope.None,
        WeatherScope.Global => CampaignObservationWeatherScope.Global,
        WeatherScope.ListedAreas => CampaignObservationWeatherScope.ListedAreas,
        _ => throw new ArgumentOutOfRangeException(nameof(scope)),
    };

    private static CampaignObservationWeatherArea FormatArea(WeatherArea area) => area switch
    {
        WeatherArea.A => CampaignObservationWeatherArea.A,
        WeatherArea.B => CampaignObservationWeatherArea.B,
        WeatherArea.C => CampaignObservationWeatherArea.C,
        WeatherArea.D => CampaignObservationWeatherArea.D,
        WeatherArea.E => CampaignObservationWeatherArea.E,
        _ => throw new ArgumentOutOfRangeException(nameof(area)),
    };
}
