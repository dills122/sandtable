using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

internal static class CampaignObservationWeatherSelector
{
    public static CampaignObservationWeather? Select(int gameTurn, int operationStage,
        IReadOnlyList<CampaignOperationStageWeather> history)
    {
        ArgumentNullException.ThrowIfNull(history);
        var matches = history.Where(value => value.GameTurn == gameTurn
            && value.OperationStage == operationStage).ToArray();
        if (matches.Length > 1)
            throw new ArgumentException("Weather history contains a duplicate pair.", nameof(history));
        if (matches.Length == 0) return null;
        var value = matches[0];
        return new CampaignObservationWeather(1, value.GameTurn, value.OperationStage,
            FormatSeason(value.Season),
            FormatKind(value.Kind),
            FormatScope(value.Scope),
            value.AffectedAreas.Select(FormatArea).ToArray());
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
