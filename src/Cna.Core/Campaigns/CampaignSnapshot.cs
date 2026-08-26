using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignSnapshot
{
    public const int CurrentContractVersion = 8;

    public CampaignSnapshot(
        int contractVersion,
        string campaignId,
        long stateVersion,
        string rulesetHash,
        CampaignSetupSnapshot setup,
        CampaignWorldSnapshot world,
        LandSide? initiativeHolder,
        IReadOnlyList<CampaignOperationStageOrder> operationStageOrders,
        RandomStreamState randomState,
        LandSequencePosition sequencePosition)
        : this(contractVersion, campaignId, stateVersion, rulesetHash, setup, world,
            initiativeHolder, operationStageOrders, [], randomState, sequencePosition)
    {
    }

    public CampaignSnapshot(
        int contractVersion,
        string campaignId,
        long stateVersion,
        string rulesetHash,
        CampaignSetupSnapshot setup,
        CampaignWorldSnapshot world,
        LandSide? initiativeHolder,
        IReadOnlyList<CampaignOperationStageOrder> operationStageOrders,
        IReadOnlyList<CampaignOperationStageWeather> operationStageWeather,
        RandomStreamState randomState,
        LandSequencePosition sequencePosition)
    {
        ContractVersion = contractVersion;
        CampaignId = campaignId;
        StateVersion = stateVersion;
        RulesetHash = rulesetHash;
        Setup = setup;
        World = world;
        InitiativeHolder = initiativeHolder;
        ArgumentNullException.ThrowIfNull(operationStageOrders);
        OperationStageOrders = Array.AsReadOnly(operationStageOrders
            .OrderBy(order => order.GameTurn)
            .ThenBy(order => order.OperationStage)
            .ToArray());
        ArgumentNullException.ThrowIfNull(operationStageWeather);
        OperationStageWeather = Array.AsReadOnly(operationStageWeather
            .OrderBy(value => value.GameTurn)
            .ThenBy(value => value.OperationStage)
            .ToArray());
        RandomState = randomState;
        SequencePosition = sequencePosition;
    }

    public int ContractVersion { get; init; }
    public string CampaignId { get; init; }
    public long StateVersion { get; init; }
    public string RulesetHash { get; init; }
    public CampaignSetupSnapshot Setup { get; init; }
    public CampaignWorldSnapshot World { get; init; }
    public LandSide? InitiativeHolder { get; init; }
    public IReadOnlyList<CampaignOperationStageOrder> OperationStageOrders { get; init; }
    public IReadOnlyList<CampaignOperationStageWeather> OperationStageWeather { get; init; }
    public RandomStreamState RandomState { get; init; }
    public LandSequencePosition SequencePosition { get; init; }

    public int GameTurn => SequencePosition.GameTurn;
    public int OperationStage => SequencePosition.OperationStage;
    public LandSide? ActiveSide => SequencePosition.ActiveSide;
    public string PhaseId => SequencePosition.PhaseId;
    public string? SegmentId => SequencePosition.SegmentId;

    public bool Equals(CampaignSnapshot? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && CampaignId == other.CampaignId
            && StateVersion == other.StateVersion
            && RulesetHash == other.RulesetHash
            && Setup == other.Setup
            && World == other.World
            && InitiativeHolder == other.InitiativeHolder
            && OperationStageOrders.SequenceEqual(other.OperationStageOrders)
            && OperationStageWeather.SequenceEqual(other.OperationStageWeather)
            && RandomState == other.RandomState
            && SequencePosition == other.SequencePosition);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion);
        hash.Add(CampaignId, StringComparer.Ordinal);
        hash.Add(StateVersion);
        hash.Add(RulesetHash, StringComparer.Ordinal);
        hash.Add(Setup);
        hash.Add(World);
        hash.Add(InitiativeHolder);
        foreach (var order in OperationStageOrders) hash.Add(order);
        foreach (var weather in OperationStageWeather) hash.Add(weather);
        hash.Add(RandomState);
        hash.Add(SequencePosition);
        return hash.ToHashCode();
    }
}
