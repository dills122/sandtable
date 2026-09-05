using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record ReactingElementMoved : CampaignSuccessorEvent
{
    public const int CurrentContractVersion = 1;

    public ReactingElementMoved(
        string campaignId,
        long stateVersion,
        long priorStateVersion,
        string fromPositionId,
        int gameTurn,
        int operationStage,
        LandSide actingSide,
        string actionId,
        string submittedWindowId,
        string submittedOpportunityId,
        CampaignReactionWindowId windowId,
        CampaignReactionOpportunityId opportunityId,
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
        CampaignReactionWindow reactionWindowAfter)
        : base(CurrentContractVersion, campaignId, stateVersion)
    {
        ArgumentNullException.ThrowIfNull(windowId);
        ArgumentNullException.ThrowIfNull(opportunityId);
        ArgumentNullException.ThrowIfNull(cost);
        ArgumentNullException.ThrowIfNull(capabilityPointsExpendedBefore);
        ArgumentNullException.ThrowIfNull(capabilityPointsExpendedAfter);
        ArgumentNullException.ThrowIfNull(reactionWindowAfter);
        PriorStateVersion = priorStateVersion;
        FromPositionId = ContentContractGuards.RequireStableId(
            fromPositionId,
            nameof(fromPositionId));
        GameTurn = gameTurn;
        OperationStage = operationStage;
        ActingSide = actingSide;
        ActionId = ContentContractGuards.RequireSha256(actionId, nameof(actionId));
        SubmittedWindowId = ContentContractGuards.RequireSha256(
            submittedWindowId,
            nameof(submittedWindowId));
        SubmittedOpportunityId = ContentContractGuards.RequireSha256(
            submittedOpportunityId,
            nameof(submittedOpportunityId));
        WindowId = windowId;
        OpportunityId = opportunityId;
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
        ReactionWindowAfter = reactionWindowAfter;
        ValidateContract();
    }

    public long PriorStateVersion { get; }
    public string FromPositionId { get; }
    public int GameTurn { get; }
    public int OperationStage { get; }
    public LandSide ActingSide { get; }
    public string ActionId { get; }
    public string SubmittedWindowId { get; }
    public string SubmittedOpportunityId { get; }
    public CampaignReactionWindowId WindowId { get; }
    public CampaignReactionOpportunityId OpportunityId { get; }
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
    public CampaignReactionWindow ReactionWindowAfter { get; }

    public ReactingElementMovedReplayInput ToReplayInput() => new(
        CampaignId,
        PriorStateVersion,
        FromPositionId,
        ActingSide,
        ActionId,
        SubmittedWindowId,
        SubmittedOpportunityId,
        WindowId,
        OpportunityId,
        OriginLocationId,
        DestinationLocationId);

    internal void ValidateContract()
    {
        var suspended = ReactionWindowAfter.ReactingPosition.SuspendedMovementPosition;
        CampaignSequenceV5Guards.RequireMaterializedMovement(suspended);
        var opportunity = ReactionWindowAfter.FrozenOpportunities.SingleOrDefault(value =>
            value.OpportunityId == OpportunityId);
        if (ContractVersion != CurrentContractVersion
            || StateVersion < 13
            || PriorStateVersion < 12
            || checked(PriorStateVersion + 1) != StateVersion
            || !Enum.IsDefined(ActingSide)
            || ReactionWindowAfter.WindowId != WindowId
            || ReactionWindowAfter.ReactingSide != ActingSide
            || ReactionWindowAfter.ActiveOpportunityId != OpportunityId
            || ReactionWindowAfter.ResolvedOpportunityIds.Contains(OpportunityId)
            || opportunity is null
            || !string.Equals(
                opportunity.ReactingRepresentation.RepresentationId,
                RepresentationId,
                StringComparison.Ordinal)
            || !opportunity.ReactingRepresentation.BoundElementIds.Contains(
                ElementId,
                StringComparer.Ordinal)
            || !string.Equals(FromPositionId, suspended.PositionId, StringComparison.Ordinal)
            || GameTurn != suspended.GameTurn
            || OperationStage != suspended.OperationStage
            || string.Equals(OriginLocationId, DestinationLocationId, StringComparison.Ordinal)
            || CohesionBefore is <= -26 or > 10
            || CohesionBefore != CohesionAfter
            || CapabilityPointsExpendedBefore + Cost.TotalCost
                != CapabilityPointsExpendedAfter)
        {
            throw new ArgumentException(
                "The ReactingElementMoved event contract is invalid.");
        }
    }

    public bool Equals(ReactingElementMoved? other) => ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && string.Equals(CampaignId, other.CampaignId, StringComparison.Ordinal)
            && StateVersion == other.StateVersion
            && PriorStateVersion == other.PriorStateVersion
            && string.Equals(FromPositionId, other.FromPositionId, StringComparison.Ordinal)
            && GameTurn == other.GameTurn
            && OperationStage == other.OperationStage
            && ActingSide == other.ActingSide
            && string.Equals(ActionId, other.ActionId, StringComparison.Ordinal)
            && string.Equals(SubmittedWindowId, other.SubmittedWindowId, StringComparison.Ordinal)
            && string.Equals(SubmittedOpportunityId, other.SubmittedOpportunityId,
                StringComparison.Ordinal)
            && WindowId == other.WindowId
            && OpportunityId == other.OpportunityId
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
            && ReactionWindowAfter == other.ReactionWindowAfter);

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
        hash.Add(ActionId, StringComparer.Ordinal);
        hash.Add(SubmittedWindowId, StringComparer.Ordinal);
        hash.Add(SubmittedOpportunityId, StringComparer.Ordinal);
        hash.Add(WindowId);
        hash.Add(OpportunityId);
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
        hash.Add(ReactionWindowAfter);
        return hash.ToHashCode();
    }
}

internal sealed record ReactionParticipantCompleted : CampaignSuccessorEvent
{
    public const int CurrentContractVersion = 1;

    public ReactionParticipantCompleted(
        string campaignId,
        long stateVersion,
        long priorStateVersion,
        string fromPositionId,
        LandSide actingSide,
        string actionId,
        string submittedWindowId,
        string submittedOpportunityId,
        CampaignReactionWindowId windowId,
        CampaignReactionOpportunityId opportunityId,
        CampaignReactionWindow reactionWindowAfter)
        : base(CurrentContractVersion, campaignId, stateVersion)
    {
        ArgumentNullException.ThrowIfNull(windowId);
        ArgumentNullException.ThrowIfNull(opportunityId);
        ArgumentNullException.ThrowIfNull(reactionWindowAfter);
        PriorStateVersion = priorStateVersion;
        FromPositionId = ContentContractGuards.RequireStableId(
            fromPositionId,
            nameof(fromPositionId));
        ActingSide = actingSide;
        ActionId = ContentContractGuards.RequireSha256(actionId, nameof(actionId));
        SubmittedWindowId = ContentContractGuards.RequireSha256(
            submittedWindowId,
            nameof(submittedWindowId));
        SubmittedOpportunityId = ContentContractGuards.RequireSha256(
            submittedOpportunityId,
            nameof(submittedOpportunityId));
        WindowId = windowId;
        OpportunityId = opportunityId;
        ReactionWindowAfter = reactionWindowAfter;
        ValidateContract();
    }

    public long PriorStateVersion { get; }
    public string FromPositionId { get; }
    public LandSide ActingSide { get; }
    public string ActionId { get; }
    public string SubmittedWindowId { get; }
    public string SubmittedOpportunityId { get; }
    public CampaignReactionWindowId WindowId { get; }
    public CampaignReactionOpportunityId OpportunityId { get; }
    public CampaignReactionWindow ReactionWindowAfter { get; }

    public ReactionParticipantCompletedReplayInput ToReplayInput() => new(
        CampaignId,
        PriorStateVersion,
        FromPositionId,
        ActingSide,
        ActionId,
        SubmittedWindowId,
        SubmittedOpportunityId,
        WindowId,
        OpportunityId);

    internal void ValidateContract()
    {
        var suspended = ReactionWindowAfter.ReactingPosition.SuspendedMovementPosition;
        CampaignSequenceV5Guards.RequireMaterializedMovement(suspended);
        if (ContractVersion != CurrentContractVersion
            || StateVersion < 14
            || PriorStateVersion < 13
            || checked(PriorStateVersion + 1) != StateVersion
            || !Enum.IsDefined(ActingSide)
            || ReactionWindowAfter.WindowId != WindowId
            || ReactionWindowAfter.ReactingSide != ActingSide
            || ReactionWindowAfter.ActiveOpportunityId is not null
            || !ReactionWindowAfter.ResolvedOpportunityIds.Contains(OpportunityId)
            || !ReactionWindowAfter.FrozenOpportunities.Any(value =>
                value.OpportunityId == OpportunityId)
            || !string.Equals(FromPositionId, suspended.PositionId, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The ReactionParticipantCompleted event contract is invalid.");
        }
    }
}

internal sealed record ReactingElementMovedReplayInput(
    string CampaignId,
    long PriorStateVersion,
    string FromPositionId,
    LandSide ActingSide,
    string ActionId,
    string SubmittedWindowId,
    string SubmittedOpportunityId,
    CampaignReactionWindowId WindowId,
    CampaignReactionOpportunityId OpportunityId,
    string OriginLocationId,
    string DestinationLocationId);

internal sealed record ReactionParticipantCompletedReplayInput(
    string CampaignId,
    long PriorStateVersion,
    string FromPositionId,
    LandSide ActingSide,
    string ActionId,
    string SubmittedWindowId,
    string SubmittedOpportunityId,
    CampaignReactionWindowId WindowId,
    CampaignReactionOpportunityId OpportunityId);

internal delegate ReactingElementMoved CampaignReactingElementMovedReconstructor(
    CampaignSnapshotV10 prior,
    ContentPackV5Artifact artifact,
    ContentScenario scenario,
    ReactingElementMovedReplayInput input);

internal delegate ReactionParticipantCompleted CampaignReactionParticipantCompletedReconstructor(
    CampaignSnapshotV10 prior,
    ContentPackV5Artifact artifact,
    ContentScenario scenario,
    ReactionParticipantCompletedReplayInput input);
