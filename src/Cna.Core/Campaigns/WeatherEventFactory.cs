using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal static class WeatherEventFactory
{
    private static readonly RuleReference[] BaseSources =
    [
        new(Cna1979Weather.SeasonBoundaryRulingId, "selected-behavior"),
        Cna1979SetupCatalog.WeatherPolicySourceReference,
        new("spi-1979-common-charts", "29.61"),
        new("spi-1979-errata", "29.1"),
        new("spi-1979-errata", "29.61"),
        new("spi-1979-land-rules", "29.0"),
        new("spi-1979-land-rules", "29.1"),
    ];

    public static WeatherDetermined Create(CampaignSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.SequencePosition.PhaseId != LandPhaseIds.WeatherDetermination
            || !Cna1979SetupCatalog.IsAdmittedWeatherPolicy(snapshot.Setup.Weather))
        {
            throw new InvalidOperationException("Weather authority is not admitted.");
        }
        var holder = snapshot.InitiativeHolder
            ?? throw new InvalidOperationException("Weather requires an initiative holder.");
        var pair = (snapshot.GameTurn, snapshot.OperationStage);
        if (snapshot.OperationStageOrders.Count(order => (order.GameTurn, order.OperationStage) == pair) != 1
            || snapshot.OperationStageWeather.Any(value => (value.GameTurn, value.OperationStage) == pair))
        {
            throw new InvalidOperationException("Weather requires one unresolved actor-order pair.");
        }
        var resolution = Cna1979Weather.Resolve(snapshot.GameTurn, snapshot.RandomState);
        var successor = Cna1979LandSequence.GetNext(snapshot.SequencePosition);
        if (successor.GameTurn != snapshot.GameTurn || successor.OperationStage != snapshot.OperationStage
            || successor.PhaseId != LandPhaseIds.Organization)
        {
            throw new InvalidOperationException("Weather must advance to Organization in the same pair.");
        }
        return new WeatherDetermined(snapshot.CampaignId, checked(snapshot.StateVersion + 1),
            snapshot.SequencePosition.PositionId, snapshot.GameTurn, snapshot.OperationStage,
            holder, resolution.Season, resolution.FirstDie,
            resolution.SecondDie, resolution.Kind, resolution.Scope, resolution.LocationDie,
            resolution.AffectedAreas, 0, 0, 0, resolution.RandomState.NextByteCursor,
            successor, GetSources(resolution.Kind));
    }

    internal static IReadOnlyList<RuleReference> GetSources(WeatherKind kind) => BaseSources
        .Concat(kind switch
        {
            WeatherKind.Normal => [new RuleReference("spi-1979-land-rules", "29.2")],
            WeatherKind.Hot =>
            [
                new RuleReference("spi-1979-land-rules", "29.31"),
                new RuleReference("spi-1979-land-rules", "29.34"),
            ],
            WeatherKind.Sandstorm =>
            [
                new RuleReference("spi-1979-common-charts", "29.7"),
                new RuleReference("spi-1979-land-rules", "29.41"),
                new RuleReference("spi-1979-land-rules", "29.47"),
                new RuleReference("spi-1979-land-rules", "38.5"),
            ],
            WeatherKind.Rainstorm =>
            [
                new RuleReference("spi-1979-common-charts", "29.7"),
                new RuleReference("spi-1979-land-rules", "29.53"),
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        })
        .OrderBy(source => source.SourceId, StringComparer.Ordinal)
        .ThenBy(source => source.Locator, StringComparer.Ordinal)
        .ToArray();
}
