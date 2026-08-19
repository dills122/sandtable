using System.Collections.ObjectModel;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignOperationStageWeather
{
    public const int CurrentContractVersion = 1;

    public CampaignOperationStageWeather(
        int contractVersion,
        int gameTurn,
        int operationStage,
        LandSide determiningSide,
        WeatherSeason season,
        int firstDie,
        int secondDie,
        WeatherKind kind,
        WeatherScope scope,
        int? locationDie,
        IReadOnlyList<WeatherArea> affectedAreas,
        int fuelWaterReductionSubjectCount,
        int restoredWellCount,
        int damagedGroundedAircraftCount)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(contractVersion, CurrentContractVersion);
        if (gameTurn is < 1 or > 110) throw new ArgumentOutOfRangeException(nameof(gameTurn));
        if (operationStage is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(operationStage));
        if (!Enum.IsDefined(determiningSide)) throw new ArgumentOutOfRangeException(nameof(determiningSide));
        if (!Enum.IsDefined(season)) throw new ArgumentOutOfRangeException(nameof(season));
        if (firstDie is < 1 or > 6) throw new ArgumentOutOfRangeException(nameof(firstDie));
        if (secondDie is < 1 or > 6) throw new ArgumentOutOfRangeException(nameof(secondDie));
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (!Enum.IsDefined(scope)) throw new ArgumentOutOfRangeException(nameof(scope));
        ArgumentNullException.ThrowIfNull(affectedAreas);
        var areas = affectedAreas.ToArray();
        if (!areas.SequenceEqual(areas.Distinct().Order())
            || areas.Any(area => !Enum.IsDefined(area)))
        {
            throw new ArgumentException("Affected Weather areas must be unique and canonical.", nameof(affectedAreas));
        }
        var foul = kind is WeatherKind.Sandstorm or WeatherKind.Rainstorm;
        if (Cna1979Weather.GetSeason(gameTurn) != season
            || Cna1979Weather.GetKind(season, (firstDie * 10) + secondDie) != kind
            || foul != locationDie.HasValue
            || (locationDie.HasValue && locationDie.Value is < 1 or > 6)
            || (foul && (scope != WeatherScope.ListedAreas
                || !areas.SequenceEqual(Cna1979Weather.GetAffectedAreas(locationDie!.Value))))
            || (kind == WeatherKind.Normal && (scope != WeatherScope.None || areas.Length != 0))
            || (kind == WeatherKind.Hot && (scope != WeatherScope.Global || areas.Length != 0))
            || fuelWaterReductionSubjectCount != 0
            || restoredWellCount != 0
            || damagedGroundedAircraftCount != 0)
        {
            throw new ArgumentException("The Weather state combination is invalid.");
        }

        ContractVersion = contractVersion;
        GameTurn = gameTurn;
        OperationStage = operationStage;
        DeterminingSide = determiningSide;
        Season = season;
        FirstDie = firstDie;
        SecondDie = secondDie;
        Kind = kind;
        Scope = scope;
        LocationDie = locationDie;
        AffectedAreas = new ReadOnlyCollection<WeatherArea>(areas);
        FuelWaterReductionSubjectCount = fuelWaterReductionSubjectCount;
        RestoredWellCount = restoredWellCount;
        DamagedGroundedAircraftCount = damagedGroundedAircraftCount;
    }

    public int ContractVersion { get; }
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

    public bool Equals(CampaignOperationStageWeather? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && GameTurn == other.GameTurn
            && OperationStage == other.OperationStage
            && DeterminingSide == other.DeterminingSide
            && Season == other.Season
            && FirstDie == other.FirstDie
            && SecondDie == other.SecondDie
            && Kind == other.Kind
            && Scope == other.Scope
            && LocationDie == other.LocationDie
            && AffectedAreas.SequenceEqual(other.AffectedAreas)
            && FuelWaterReductionSubjectCount == other.FuelWaterReductionSubjectCount
            && RestoredWellCount == other.RestoredWellCount
            && DamagedGroundedAircraftCount == other.DamagedGroundedAircraftCount);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion);
        hash.Add(GameTurn);
        hash.Add(OperationStage);
        hash.Add(DeterminingSide);
        hash.Add(Season);
        hash.Add(FirstDie);
        hash.Add(SecondDie);
        hash.Add(Kind);
        hash.Add(Scope);
        hash.Add(LocationDie);
        foreach (var area in AffectedAreas) hash.Add(area);
        hash.Add(FuelWaterReductionSubjectCount);
        hash.Add(RestoredWellCount);
        hash.Add(DamagedGroundedAircraftCount);
        return hash.ToHashCode();
    }
}
