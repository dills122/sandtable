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
            (CampaignObservationWeatherSeason)(int)value.Season,
            (CampaignObservationWeatherKind)(int)value.Kind,
            (CampaignObservationWeatherScope)(int)value.Scope,
            value.AffectedAreas.Select(area => (CampaignObservationWeatherArea)(int)area).ToArray());
    }
}
