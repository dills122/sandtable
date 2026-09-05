using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal enum CampaignPositionV11Kind { Sequence, Reaction, BreakdownStop }

internal sealed record CampaignPositionV11
{
    private CampaignPositionV11(CampaignPositionV11Kind kind, LandSequencePosition? sequence,
        CampaignReactingPosition? reacting, LandSequencePosition? suspended)
    {
        Kind = kind;
        SequencePosition = sequence;
        ReactingPosition = reacting;
        SuspendedSequencePosition = suspended;
    }
    public CampaignPositionV11Kind Kind { get; }
    public LandSequencePosition? SequencePosition { get; }
    public CampaignReactingPosition? ReactingPosition { get; }
    public LandSequencePosition? SuspendedSequencePosition { get; }
    public LandSequencePosition SequenceContext => SequencePosition ?? ReactingPosition?.SuspendedMovementPosition ?? SuspendedSequencePosition!;
    public static CampaignPositionV11 FromSequence(LandSequencePosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        return new(CampaignPositionV11Kind.Sequence, position, null, null);
    }
    public static CampaignPositionV11 FromReaction(CampaignReactingPosition position)
    {
        ArgumentNullException.ThrowIfNull(position);
        Cna1979LandSequenceV4.RequireMaterializedMovement(position.SuspendedMovementPosition);
        return new(CampaignPositionV11Kind.Reaction, null, position, null);
    }
    public static CampaignPositionV11 FromBreakdownStop(LandSequencePosition suspended)
    {
        Cna1979LandSequenceV4.RequireMaterializedMovement(suspended);
        return new(CampaignPositionV11Kind.BreakdownStop, null, null, suspended);
    }
}

internal sealed record CampaignSnapshotV11
{
    public const int CurrentContractVersion = 11;
    public CampaignSnapshotV11(int contractVersion, string campaignId, long stateVersion, string rulesetHash,
        CampaignSetupSnapshotV6 setup, CampaignWorldSnapshotV6 world, LandSide? initiativeHolder,
        IEnumerable<CampaignOperationStageOrder> operationStageOrders,
        IEnumerable<CampaignOperationStageWeather> operationStageWeather, RandomStreamState randomState,
        CampaignPositionV11 currentPosition, CampaignReactionWindow? reactionWindow, CampaignBreakdownFlow breakdownFlow)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(contractVersion, CurrentContractVersion);
        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        CampaignId = ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        if (!Cna1979BreakdownRuleset.IsCanonicalHash(rulesetHash)) throw new ArgumentException("Expected dormant Breakdown ruleset 9.");
        ArgumentNullException.ThrowIfNull(setup); ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(randomState); ArgumentNullException.ThrowIfNull(currentPosition);
        ArgumentNullException.ThrowIfNull(breakdownFlow);
        if (initiativeHolder is not null && !Enum.IsDefined(initiativeHolder.Value)) throw new ArgumentOutOfRangeException(nameof(initiativeHolder));
        var orders = ContentContractGuards.CopyValues(operationStageOrders, nameof(operationStageOrders));
        var weather = ContentContractGuards.CopyValues(operationStageWeather, nameof(operationStageWeather));
        if (orders.Select(value => (value.GameTurn, value.OperationStage)).Distinct().Count() != orders.Length
            || weather.Select(value => (value.GameTurn, value.OperationStage)).Distinct().Count() != weather.Length)
            throw new ArgumentException("Duplicate operation-stage state.");
        ContractVersion = contractVersion; StateVersion = stateVersion; RulesetHash = rulesetHash;
        Setup = setup; World = world; InitiativeHolder = initiativeHolder;
        OperationStageOrders = Array.AsReadOnly(orders.OrderBy(value => value.GameTurn).ThenBy(value => value.OperationStage).ToArray());
        OperationStageWeather = Array.AsReadOnly(weather.OrderBy(value => value.GameTurn).ThenBy(value => value.OperationStage).ToArray());
        RandomState = randomState; CurrentPosition = currentPosition; ReactionWindow = reactionWindow; BreakdownFlow = breakdownFlow;
        CampaignSnapshotV11Validator.RequireShape(this);
    }
    public int ContractVersion { get; }
    public string CampaignId { get; }
    public long StateVersion { get; }
    public string RulesetHash { get; }
    public CampaignSetupSnapshotV6 Setup { get; }
    public CampaignWorldSnapshotV6 World { get; }
    public LandSide? InitiativeHolder { get; }
    public IReadOnlyList<CampaignOperationStageOrder> OperationStageOrders { get; }
    public IReadOnlyList<CampaignOperationStageWeather> OperationStageWeather { get; }
    public RandomStreamState RandomState { get; }
    public CampaignPositionV11 CurrentPosition { get; }
    public CampaignReactionWindow? ReactionWindow { get; }
    public CampaignBreakdownFlow BreakdownFlow { get; }

    public bool Equals(CampaignSnapshotV11? other) => ReferenceEquals(this, other) || (other is not null
        && ContractVersion == other.ContractVersion && CampaignId == other.CampaignId && StateVersion == other.StateVersion
        && RulesetHash == other.RulesetHash && Setup == other.Setup && World == other.World && InitiativeHolder == other.InitiativeHolder
        && OperationStageOrders.SequenceEqual(other.OperationStageOrders) && OperationStageWeather.SequenceEqual(other.OperationStageWeather)
        && RandomState == other.RandomState && CurrentPosition == other.CurrentPosition && ReactionWindow == other.ReactionWindow
        && BreakdownFlow == other.BreakdownFlow);
    public override int GetHashCode() => HashCode.Combine(CampaignId, StateVersion, RulesetHash, World, BreakdownFlow);
}
