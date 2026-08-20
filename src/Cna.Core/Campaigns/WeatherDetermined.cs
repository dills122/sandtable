using System.Collections.ObjectModel;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record WeatherDetermined : CampaignEvent
{
    public WeatherDetermined(
        string campaignId, long stateVersion, string fromPositionId, int gameTurn,
        int operationStage, LandSide determiningSide, WeatherSeason season, int firstDie,
        int secondDie, WeatherKind kind, WeatherScope scope, int? locationDie,
        IReadOnlyList<WeatherArea> affectedAreas, int fuelWaterReductionSubjectCount,
        int restoredWellCount, int damagedGroundedAircraftCount, ulong randomCursorAfter,
        LandSequencePosition sequencePosition, IReadOnlyList<RuleReference> sources)
        : base(1, campaignId, stateVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromPositionId);
        ArgumentNullException.ThrowIfNull(sequencePosition);
        _ = new CampaignOperationStageWeather(1, gameTurn, operationStage, determiningSide,
            season, firstDie, secondDie, kind, scope, locationDie, affectedAreas,
            fuelWaterReductionSubjectCount, restoredWellCount, damagedGroundedAircraftCount);
        ArgumentNullException.ThrowIfNull(sources);
        var sourceCopy = sources.ToArray();
        if (sourceCopy.Length == 0 || sourceCopy.Any(source => source is null)
            || sourceCopy.Distinct().Count() != sourceCopy.Length
            || !sourceCopy.SequenceEqual(sourceCopy.OrderBy(source => source.SourceId, StringComparer.Ordinal)
                .ThenBy(source => source.Locator, StringComparer.Ordinal)))
        {
            throw new ArgumentException("Weather sources must be unique and canonical.", nameof(sources));
        }
        FromPositionId = fromPositionId;
        GameTurn = gameTurn;
        OperationStage = operationStage;
        DeterminingSide = determiningSide;
        Season = season;
        FirstDie = firstDie;
        SecondDie = secondDie;
        Kind = kind;
        Scope = scope;
        LocationDie = locationDie;
        AffectedAreas = new ReadOnlyCollection<WeatherArea>(affectedAreas.ToArray());
        FuelWaterReductionSubjectCount = fuelWaterReductionSubjectCount;
        RestoredWellCount = restoredWellCount;
        DamagedGroundedAircraftCount = damagedGroundedAircraftCount;
        RandomCursorAfter = randomCursorAfter;
        SequencePosition = sequencePosition;
        Sources = new ReadOnlyCollection<RuleReference>(sourceCopy);
    }

    public string FromPositionId { get; }
    public int GameTurn { get; }
    public int OperationStage { get; }
    public LandSide DeterminingSide { get; }
    public WeatherSeason Season { get; }
    public int FirstDie { get; }
    public int SecondDie { get; }
    public WeatherKind Kind { get; }
    public WeatherScope Scope { get; }
    public int? LocationDie { get; }
    public IReadOnlyList<WeatherArea> AffectedAreas { get; }
    public int FuelWaterReductionSubjectCount { get; }
    public int RestoredWellCount { get; }
    public int DamagedGroundedAircraftCount { get; }
    public ulong RandomCursorAfter { get; }
    public LandSequencePosition SequencePosition { get; }
    public IReadOnlyList<RuleReference> Sources { get; }

    public CampaignOperationStageWeather ToState() => new(1, GameTurn, OperationStage,
        DeterminingSide, Season, FirstDie, SecondDie, Kind, Scope, LocationDie, AffectedAreas,
        FuelWaterReductionSubjectCount, RestoredWellCount, DamagedGroundedAircraftCount);

    public bool Equals(WeatherDetermined? other) => ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && CampaignId == other.CampaignId
            && StateVersion == other.StateVersion
            && FromPositionId == other.FromPositionId
            && ToState() == other.ToState()
            && RandomCursorAfter == other.RandomCursorAfter
            && SequencePosition == other.SequencePosition
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion);
        hash.Add(CampaignId, StringComparer.Ordinal);
        hash.Add(StateVersion);
        hash.Add(FromPositionId, StringComparer.Ordinal);
        hash.Add(ToState());
        hash.Add(RandomCursorAfter);
        hash.Add(SequencePosition);
        foreach (var source in Sources) hash.Add(source);
        return hash.ToHashCode();
    }
}
