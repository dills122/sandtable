using System.Collections.ObjectModel;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal abstract record StageEntryResolved : CampaignEvent
{
    protected StageEntryResolved(
        string campaignId,
        long stateVersion,
        string fromPositionId,
        int gameTurn,
        int operationStage,
        LandSequencePosition sequencePosition,
        IReadOnlyList<RuleReference> sources,
        long expectedStateVersion,
        string expectedFromPositionId,
        LandSequencePosition expectedSequencePosition,
        IReadOnlyList<RuleReference> expectedSources)
        : base(1, campaignId, stateVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(campaignId);
        ArgumentOutOfRangeException.ThrowIfNotEqual(stateVersion, expectedStateVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(fromPositionId);
        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);
        ArgumentOutOfRangeException.ThrowIfNotEqual(operationStage, 1);
        ArgumentNullException.ThrowIfNull(sequencePosition);
        ArgumentNullException.ThrowIfNull(sources);

        var sourceCopy = sources.ToArray();
        if (!string.Equals(fromPositionId, expectedFromPositionId, StringComparison.Ordinal)
            || sequencePosition != expectedSequencePosition
            || sourceCopy.Any(source => source is null)
            || !sourceCopy.SequenceEqual(expectedSources))
        {
            throw new ArgumentException("The Stage Entry event authority binding is invalid.");
        }

        FromPositionId = fromPositionId;
        GameTurn = gameTurn;
        OperationStage = operationStage;
        SequencePosition = sequencePosition;
        Sources = new ReadOnlyCollection<RuleReference>(sourceCopy);
    }

    public string FromPositionId { get; }

    public int GameTurn { get; }

    public int OperationStage { get; }

    public LandSequencePosition SequencePosition { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public virtual bool Equals(StageEntryResolved? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && EqualityContract == other.EqualityContract
            && ContractVersion == other.ContractVersion
            && string.Equals(CampaignId, other.CampaignId, StringComparison.Ordinal)
            && StateVersion == other.StateVersion
            && string.Equals(FromPositionId, other.FromPositionId, StringComparison.Ordinal)
            && GameTurn == other.GameTurn
            && OperationStage == other.OperationStage
            && SequencePosition == other.SequencePosition
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(EqualityContract);
        hash.Add(ContractVersion);
        hash.Add(CampaignId, StringComparer.Ordinal);
        hash.Add(StateVersion);
        hash.Add(FromPositionId, StringComparer.Ordinal);
        hash.Add(GameTurn);
        hash.Add(OperationStage);
        hash.Add(SequencePosition);
        foreach (var source in Sources) hash.Add(source);
        return hash.ToHashCode();
    }

    protected static LandSequencePosition GetPosition(
        int gameTurn,
        string phaseId,
        string? segmentId = null,
        LandActorRole? actorRole = null) => Cna1979LandSequence.CreateTurn(gameTurn).Single(value =>
            value.OperationStage == 1
            && value.PhaseId == phaseId
            && value.SegmentId == segmentId
            && (!actorRole.HasValue || value.ActorRole == actorRole.Value));
}

internal sealed record NoObligationOrganizationResolved : StageEntryResolved
{
    internal static IReadOnlyList<RuleReference> RequiredSources { get; } = CreateSources(
        "5.2.organization");

    public NoObligationOrganizationResolved(
        string campaignId,
        long stateVersion,
        string fromPositionId,
        int gameTurn,
        int operationStage,
        LandSequencePosition sequencePosition,
        IReadOnlyList<RuleReference> sources)
        : base(campaignId, stateVersion, fromPositionId, gameTurn, operationStage,
            sequencePosition, sources, 7, "land.position.operation-1.organization",
            GetPosition(gameTurn, LandPhaseIds.NavalConvoyArrival), RequiredSources)
    {
    }

    private static ReadOnlyCollection<RuleReference> CreateSources(string primaryLocator) =>
        Array.AsReadOnly<RuleReference>(
        [
            CampaignStageEntryPolicy.SourceReference,
            new("spi-1979-land-rules", primaryLocator),
        ]);
}

internal sealed record NoObligationNavalConvoyArrivalResolved : StageEntryResolved
{
    internal static IReadOnlyList<RuleReference> RequiredSources { get; } = Array.AsReadOnly<RuleReference>(
    [
        CampaignStageEntryPolicy.SourceReference,
        new("spi-1979-land-rules", "5.2.naval-convoy-arrival"),
    ]);

    public NoObligationNavalConvoyArrivalResolved(
        string campaignId,
        long stateVersion,
        string fromPositionId,
        int gameTurn,
        int operationStage,
        LandSequencePosition sequencePosition,
        IReadOnlyList<RuleReference> sources)
        : base(campaignId, stateVersion, fromPositionId, gameTurn, operationStage,
            sequencePosition, sources, 8, "land.position.operation-1.naval-convoy-arrival",
            GetPosition(gameTurn, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetAssignment),
            RequiredSources)
    {
    }
}

internal sealed record NoObligationFleetAssignmentResolved : StageEntryResolved
{
    internal static IReadOnlyList<RuleReference> RequiredSources { get; } = Array.AsReadOnly<RuleReference>(
    [
        CampaignStageEntryPolicy.SourceReference,
        new("spi-1979-land-rules", "5.2.commonwealth-fleet"),
    ]);

    public NoObligationFleetAssignmentResolved(
        string campaignId,
        long stateVersion,
        string fromPositionId,
        int gameTurn,
        int operationStage,
        LandSequencePosition sequencePosition,
        IReadOnlyList<RuleReference> sources)
        : base(campaignId, stateVersion, fromPositionId, gameTurn, operationStage,
            sequencePosition, sources, 9,
            "land.position.operation-1.commonwealth-fleet.assignment",
            GetPosition(gameTurn, LandPhaseIds.CommonwealthFleet, LandSegmentIds.FleetRepair),
            RequiredSources)
    {
    }
}

internal sealed record NoObligationFleetRepairResolved : StageEntryResolved
{
    internal static IReadOnlyList<RuleReference> RequiredSources { get; } =
        NoObligationFleetAssignmentResolved.RequiredSources;

    public NoObligationFleetRepairResolved(
        string campaignId,
        long stateVersion,
        string fromPositionId,
        int gameTurn,
        int operationStage,
        LandSequencePosition sequencePosition,
        IReadOnlyList<RuleReference> sources)
        : base(campaignId, stateVersion, fromPositionId, gameTurn, operationStage,
            sequencePosition, sources, 10,
            "land.position.operation-1.commonwealth-fleet.repair",
            GetPosition(gameTurn, LandPhaseIds.ReserveDesignation,
                actorRole: LandActorRole.FirstActingSide), RequiredSources)
    {
    }
}
