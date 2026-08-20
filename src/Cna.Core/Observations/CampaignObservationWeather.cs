namespace Cna.Core.Observations;

public enum CampaignObservationWeatherSeason { Fall = 1, Winter = 2, Spring = 3, Summer = 4 }
public enum CampaignObservationWeatherKind { Normal = 1, Hot = 2, Sandstorm = 3, Rainstorm = 4 }
public enum CampaignObservationWeatherScope { None = 1, Global = 2, ListedAreas = 3 }
public enum CampaignObservationWeatherArea { A = 1, B = 2, C = 3, D = 4, E = 5 }

public sealed record CampaignObservationWeather
{
    public const int CurrentContractVersion = 1;

    internal CampaignObservationWeather(int contractVersion, int gameTurn, int operationStage,
        CampaignObservationWeatherSeason season, CampaignObservationWeatherKind kind,
        CampaignObservationWeatherScope scope, IReadOnlyList<CampaignObservationWeatherArea> affectedAreas)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(contractVersion, CurrentContractVersion);
        if (gameTurn is < 1 or > 110) throw new ArgumentOutOfRangeException(nameof(gameTurn));
        if (operationStage is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(operationStage));
        if (!Enum.IsDefined(season)) throw new ArgumentOutOfRangeException(nameof(season));
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (!Enum.IsDefined(scope)) throw new ArgumentOutOfRangeException(nameof(scope));
        ArgumentNullException.ThrowIfNull(affectedAreas);
        var areas = affectedAreas.ToArray();
        if (areas.Any(area => !Enum.IsDefined(area)) || !areas.SequenceEqual(areas.Distinct().Order())
            || (kind == CampaignObservationWeatherKind.Normal
                && (scope != CampaignObservationWeatherScope.None || areas.Length != 0))
            || (kind == CampaignObservationWeatherKind.Hot
                && (scope != CampaignObservationWeatherScope.Global || areas.Length != 0))
            || (kind is CampaignObservationWeatherKind.Sandstorm or CampaignObservationWeatherKind.Rainstorm
                && (scope != CampaignObservationWeatherScope.ListedAreas || areas.Length == 0)))
            throw new ArgumentException("The observed Weather combination is invalid.");
        ContractVersion = contractVersion; GameTurn = gameTurn; OperationStage = operationStage;
        Season = season; Kind = kind; Scope = scope; AffectedAreas = Array.AsReadOnly(areas);
    }

    public int ContractVersion { get; }
    public int GameTurn { get; }
    public int OperationStage { get; }
    public CampaignObservationWeatherSeason Season { get; }
    public CampaignObservationWeatherKind Kind { get; }
    public CampaignObservationWeatherScope Scope { get; }
    public IReadOnlyList<CampaignObservationWeatherArea> AffectedAreas { get; }

    public bool Equals(CampaignObservationWeather? other) => ReferenceEquals(this, other)
        || (other is not null && ContractVersion == other.ContractVersion
            && GameTurn == other.GameTurn && OperationStage == other.OperationStage
            && Season == other.Season && Kind == other.Kind && Scope == other.Scope
            && AffectedAreas.SequenceEqual(other.AffectedAreas));
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion); hash.Add(GameTurn); hash.Add(OperationStage); hash.Add(Season);
        hash.Add(Kind); hash.Add(Scope); foreach (var area in AffectedAreas) hash.Add(area);
        return hash.ToHashCode();
    }
}
