using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record ElementMovedV2 : CampaignSuccessorEvent
{
    public const int CurrentContractVersion = 2;

    public ElementMovedV2(
        string campaignId,
        long stateVersion,
        long priorStateVersion,
        string fromPositionId,
        int gameTurn,
        int operationStage,
        LandSide actingSide,
        string elementId,
        string representationId,
        string originLocationId,
        string destinationLocationId,
        string mobilityId,
        IEnumerable<RuleReference> mobilitySources,
        CampaignMovementCost cost,
        CapabilityPointAmount capabilityPointsExpendedBefore,
        CapabilityPointAmount capabilityPointsExpendedAfter,
        int cohesionBefore,
        int cohesionAfter,
        CampaignMovementEndedState? movementEndedAfter,
        LandSequencePosition sequencePosition,
        CampaignReactionWindow? openedReactionWindow)
        : base(CurrentContractVersion, campaignId, stateVersion)
    {
        ArgumentNullException.ThrowIfNull(cost);
        ArgumentNullException.ThrowIfNull(capabilityPointsExpendedBefore);
        ArgumentNullException.ThrowIfNull(capabilityPointsExpendedAfter);
        ArgumentNullException.ThrowIfNull(sequencePosition);
        PriorStateVersion = priorStateVersion;
        FromPositionId = ContentContractGuards.RequireStableId(
            fromPositionId,
            nameof(fromPositionId));
        GameTurn = gameTurn;
        OperationStage = operationStage;
        ActingSide = actingSide;
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        RepresentationId = ContentContractGuards.RequireStableId(
            representationId,
            nameof(representationId));
        OriginLocationId = ContentContractGuards.RequireStableId(
            originLocationId,
            nameof(originLocationId));
        DestinationLocationId = ContentContractGuards.RequireStableId(
            destinationLocationId,
            nameof(destinationLocationId));
        MobilityId = ContentContractGuards.RequireStableId(mobilityId, nameof(mobilityId));
        MobilitySources = RuleReferenceValidation.CopySources(
            mobilitySources,
            nameof(mobilitySources));
        Cost = cost;
        CapabilityPointsExpendedBefore = capabilityPointsExpendedBefore;
        CapabilityPointsExpendedAfter = capabilityPointsExpendedAfter;
        CohesionBefore = cohesionBefore;
        CohesionAfter = cohesionAfter;
        MovementEndedAfter = movementEndedAfter;
        SequencePosition = sequencePosition;
        OpenedReactionWindow = openedReactionWindow;
        ValidateContract();
    }

    public long PriorStateVersion { get; }

    public string FromPositionId { get; }

    public int GameTurn { get; }

    public int OperationStage { get; }

    public LandSide ActingSide { get; }

    public string ElementId { get; }

    public string RepresentationId { get; }

    public string OriginLocationId { get; }

    public string DestinationLocationId { get; }

    public string MobilityId { get; }

    public IReadOnlyList<RuleReference> MobilitySources { get; }

    public CampaignMovementCost Cost { get; }

    public CapabilityPointAmount CapabilityPointsExpendedBefore { get; }

    public CapabilityPointAmount CapabilityPointsExpendedAfter { get; }

    public int CohesionBefore { get; }

    public int CohesionAfter { get; }

    public CampaignMovementEndedState? MovementEndedAfter { get; }

    public LandSequencePosition SequencePosition { get; }

    public CampaignReactionWindow? OpenedReactionWindow { get; }

    public ElementMovedV2ReplayInput ToReplayInput() => new(
        CampaignId,
        PriorStateVersion,
        FromPositionId,
        ActingSide,
        ElementId,
        OriginLocationId,
        DestinationLocationId);

    internal void ValidateContract()
    {
        CampaignSequenceV5Guards.RequireMaterializedMovement(SequencePosition);
        if (ContractVersion != CurrentContractVersion
            || StateVersion < 12
            || PriorStateVersion < 11
            || checked(PriorStateVersion + 1) != StateVersion
            || OperationStage != SequencePosition.OperationStage
            || GameTurn != SequencePosition.GameTurn
            || !Enum.IsDefined(ActingSide)
            || SequencePosition.ActiveSide != ActingSide
            || OriginLocationId == DestinationLocationId
            || CohesionBefore is <= -26 or > 10
            || CohesionBefore != CohesionAfter
            || CapabilityPointsExpendedBefore + Cost.TotalCost
                != CapabilityPointsExpendedAfter
            || (MovementEndedAfter is not null
                && (!string.Equals(MovementEndedAfter.PositionId,
                        SequencePosition.PositionId, StringComparison.Ordinal)
                    || MovementEndedAfter.GameTurn != SequencePosition.GameTurn
                    || MovementEndedAfter.OperationStage
                        != SequencePosition.OperationStage
                    || MovementEndedAfter.PhasingSide != ActingSide))
            || !string.Equals(FromPositionId, SequencePosition.PositionId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException("The ElementMoved v2 event contract is invalid.");
        }

        if (OpenedReactionWindow is null)
        {
            return;
        }

        var trigger = OpenedReactionWindow.TriggerAuthority;
        if (OpenedReactionWindow.TriggerCommittedStateVersion != StateVersion
            || OpenedReactionWindow.PhasingSide != ActingSide
            || OpenedReactionWindow.ReactingPosition.SuspendedMovementPosition != SequencePosition
            || trigger.MoveContractVersion != CurrentContractVersion
            || !string.Equals(trigger.ElementId, ElementId, StringComparison.Ordinal)
            || !string.Equals(trigger.TriggeringRepresentation.RepresentationId,
                RepresentationId, StringComparison.Ordinal)
            || !string.Equals(trigger.OriginLocationId, OriginLocationId,
                StringComparison.Ordinal)
            || !string.Equals(trigger.DestinationLocationId, DestinationLocationId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The ElementMoved v2 opened window is inconsistent with the triggering move.");
        }
    }

    public bool Equals(ElementMovedV2? other) => ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && string.Equals(CampaignId, other.CampaignId, StringComparison.Ordinal)
            && StateVersion == other.StateVersion
            && PriorStateVersion == other.PriorStateVersion
            && string.Equals(FromPositionId, other.FromPositionId, StringComparison.Ordinal)
            && GameTurn == other.GameTurn
            && OperationStage == other.OperationStage
            && ActingSide == other.ActingSide
            && string.Equals(ElementId, other.ElementId, StringComparison.Ordinal)
            && string.Equals(RepresentationId, other.RepresentationId, StringComparison.Ordinal)
            && string.Equals(OriginLocationId, other.OriginLocationId, StringComparison.Ordinal)
            && string.Equals(DestinationLocationId, other.DestinationLocationId,
                StringComparison.Ordinal)
            && string.Equals(MobilityId, other.MobilityId, StringComparison.Ordinal)
            && MobilitySources.SequenceEqual(other.MobilitySources)
            && Cost == other.Cost
            && CapabilityPointsExpendedBefore == other.CapabilityPointsExpendedBefore
            && CapabilityPointsExpendedAfter == other.CapabilityPointsExpendedAfter
            && CohesionBefore == other.CohesionBefore
            && CohesionAfter == other.CohesionAfter
            && MovementEndedAfter == other.MovementEndedAfter
            && SequencePosition == other.SequencePosition
            && OpenedReactionWindow == other.OpenedReactionWindow);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion);
        hash.Add(CampaignId, StringComparer.Ordinal);
        hash.Add(StateVersion);
        hash.Add(PriorStateVersion);
        hash.Add(FromPositionId, StringComparer.Ordinal);
        hash.Add(GameTurn);
        hash.Add(OperationStage);
        hash.Add(ActingSide);
        hash.Add(ElementId, StringComparer.Ordinal);
        hash.Add(RepresentationId, StringComparer.Ordinal);
        hash.Add(OriginLocationId, StringComparer.Ordinal);
        hash.Add(DestinationLocationId, StringComparer.Ordinal);
        hash.Add(MobilityId, StringComparer.Ordinal);
        foreach (var source in MobilitySources) hash.Add(source);
        hash.Add(Cost);
        hash.Add(CapabilityPointsExpendedBefore);
        hash.Add(CapabilityPointsExpendedAfter);
        hash.Add(CohesionBefore);
        hash.Add(CohesionAfter);
        hash.Add(MovementEndedAfter);
        hash.Add(SequencePosition);
        hash.Add(OpenedReactionWindow);
        return hash.ToHashCode();
    }
}

internal sealed record ElementMovedV2ReplayInput(
    string CampaignId,
    long PriorStateVersion,
    string FromPositionId,
    LandSide ActingSide,
    string ElementId,
    string OriginLocationId,
    string DestinationLocationId);

internal delegate ElementMovedV2 CampaignElementMovedV2Reconstructor(
    CampaignSnapshotV10 prior,
    ElementMovedV2ReplayInput input);
