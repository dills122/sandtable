using System.Numerics;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignMovementRouteAdjustment
{
    public CampaignMovementRouteAdjustment(
        string routeId,
        MovementRouteCostKind costKind,
        CapabilityPointAmount amount,
        IEnumerable<RuleReference> sources)
    {
        if (!Enum.IsDefined(costKind))
        {
            throw new ArgumentOutOfRangeException(nameof(costKind));
        }

        ArgumentNullException.ThrowIfNull(amount);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            amount,
            CapabilityPointAmount.Zero);
        RouteId = ContentContractGuards.RequireStableId(routeId, nameof(routeId));
        CostKind = costKind;
        Amount = amount;
        Sources = RuleReferenceValidation.CopySources(sources, nameof(sources));
    }

    public string RouteId { get; }

    public MovementRouteCostKind CostKind { get; }

    public CapabilityPointAmount Amount { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(CampaignMovementRouteAdjustment? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(RouteId, other.RouteId, StringComparison.Ordinal)
            && CostKind == other.CostKind
            && Amount == other.Amount
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(RouteId, StringComparer.Ordinal);
        hash.Add(CostKind);
        hash.Add(Amount);
        foreach (var source in Sources) hash.Add(source);
        return hash.ToHashCode();
    }
}

internal sealed record CampaignMovementHexsideCost
{
    public CampaignMovementHexsideCost(
        string hexsideId,
        MovementHexsideDirection direction,
        CapabilityPointAmount addedCost,
        IEnumerable<RuleReference> sources)
    {
        if (!Enum.IsDefined(direction))
        {
            throw new ArgumentOutOfRangeException(nameof(direction));
        }

        ArgumentNullException.ThrowIfNull(addedCost);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            addedCost,
            CapabilityPointAmount.Zero);
        HexsideId = ContentContractGuards.RequireStableId(hexsideId, nameof(hexsideId));
        Direction = direction;
        AddedCost = addedCost;
        Sources = RuleReferenceValidation.CopySources(sources, nameof(sources));
    }

    public string HexsideId { get; }

    public MovementHexsideDirection Direction { get; }

    public CapabilityPointAmount AddedCost { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(CampaignMovementHexsideCost? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(HexsideId, other.HexsideId, StringComparison.Ordinal)
            && Direction == other.Direction
            && AddedCost == other.AddedCost
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(HexsideId, StringComparer.Ordinal);
        hash.Add(Direction);
        hash.Add(AddedCost);
        foreach (var source in Sources) hash.Add(source);
        return hash.ToHashCode();
    }
}

internal sealed record CampaignMovementCost
{
    public CampaignMovementCost(
        string destinationTerrainId,
        CapabilityPointAmount destinationTerrainCost,
        IEnumerable<RuleReference> destinationTerrainSources,
        CampaignMovementRouteAdjustment? routeAdjustment,
        IEnumerable<CampaignMovementHexsideCost> crossedHexsideCosts,
        CapabilityPointAmount totalCost)
    {
        ArgumentNullException.ThrowIfNull(destinationTerrainCost);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(
            destinationTerrainCost,
            CapabilityPointAmount.Zero);
        ArgumentNullException.ThrowIfNull(crossedHexsideCosts);
        ArgumentNullException.ThrowIfNull(totalCost);

        var crossed = crossedHexsideCosts.ToArray();
        if (crossed.Any(value => value is null)
            || crossed.Select(value => value.HexsideId)
                .Distinct(StringComparer.Ordinal).Count() != crossed.Length)
        {
            throw new ArgumentException(
                "Crossed hexside costs must be non-null and unique by feature ID.",
                nameof(crossedHexsideCosts));
        }

        var orderedCrossed = crossed
            .OrderBy(value => value.HexsideId, StringComparer.Ordinal)
            .ThenBy(value => value.Direction)
            .ToArray();
        var adjustedTerrain = routeAdjustment switch
        {
            null => destinationTerrainCost,
            { CostKind: MovementRouteCostKind.Override } => routeAdjustment.Amount,
            { CostKind: MovementRouteCostKind.ScaleUnderlying } => Multiply(
                destinationTerrainCost,
                routeAdjustment.Amount),
            _ => throw new ArgumentOutOfRangeException(nameof(routeAdjustment)),
        };
        var expectedTotal = orderedCrossed.Aggregate(
            adjustedTerrain,
            (current, value) => current + value.AddedCost);
        if (totalCost != expectedTotal)
        {
            throw new ArgumentException(
                "The Movement total cost does not match its authoritative breakdown.",
                nameof(totalCost));
        }

        DestinationTerrainId = ContentContractGuards.RequireStableId(
            destinationTerrainId,
            nameof(destinationTerrainId));
        DestinationTerrainCost = destinationTerrainCost;
        DestinationTerrainSources = RuleReferenceValidation.CopySources(
            destinationTerrainSources,
            nameof(destinationTerrainSources));
        RouteAdjustment = routeAdjustment;
        CrossedHexsideCosts = Array.AsReadOnly(orderedCrossed);
        TotalCost = totalCost;
    }

    public string DestinationTerrainId { get; }

    public CapabilityPointAmount DestinationTerrainCost { get; }

    public IReadOnlyList<RuleReference> DestinationTerrainSources { get; }

    public CampaignMovementRouteAdjustment? RouteAdjustment { get; }

    public IReadOnlyList<CampaignMovementHexsideCost> CrossedHexsideCosts { get; }

    public CapabilityPointAmount TotalCost { get; }

    public bool Equals(CampaignMovementCost? other) => ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(
                DestinationTerrainId,
                other.DestinationTerrainId,
                StringComparison.Ordinal)
            && DestinationTerrainCost == other.DestinationTerrainCost
            && DestinationTerrainSources.SequenceEqual(other.DestinationTerrainSources)
            && RouteAdjustment == other.RouteAdjustment
            && CrossedHexsideCosts.SequenceEqual(other.CrossedHexsideCosts)
            && TotalCost == other.TotalCost);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(DestinationTerrainId, StringComparer.Ordinal);
        hash.Add(DestinationTerrainCost);
        foreach (var source in DestinationTerrainSources) hash.Add(source);
        hash.Add(RouteAdjustment);
        foreach (var crossed in CrossedHexsideCosts) hash.Add(crossed);
        hash.Add(TotalCost);
        return hash.ToHashCode();
    }

    private static CapabilityPointAmount Multiply(
        CapabilityPointAmount left,
        CapabilityPointAmount right)
    {
        var numerator = (BigInteger)left.Numerator * right.Numerator;
        var denominator = (BigInteger)left.Denominator * right.Denominator;
        var divisor = BigInteger.GreatestCommonDivisor(numerator, denominator);
        numerator /= divisor;
        denominator /= divisor;
        if (numerator > long.MaxValue || denominator > int.MaxValue)
        {
            throw new OverflowException(
                "The exact Movement cost is outside the supported representation.");
        }

        return new CapabilityPointAmount((long)numerator, (int)denominator);
    }
}

internal sealed record ElementMoved : CampaignEvent
{
    public ElementMoved(
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
        LandSequencePosition sequencePosition)
        : base(1, campaignId, stateVersion)
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
        SequencePosition = sequencePosition;
        ValidateContract();
    }

    public string FromPositionId { get; }

    public long PriorStateVersion { get; }

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

    public LandSequencePosition SequencePosition { get; }

    internal void ValidateContract()
    {
        _ = ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId));
        var expectedPosition = Cna1979LandSequence.CreateTurn(GameTurn).Single(value =>
            value.OperationStage == 1
            && value.PhaseId == LandPhaseIds.MovementAndCombat
            && value.SegmentId == LandSegmentIds.Movement
            && value.ActorRole == LandActorRole.FirstActingSide);
        if (ContractVersion != 1
            || StateVersion < 12
            || PriorStateVersion < 11
            || checked(PriorStateVersion + 1) != StateVersion
            || OperationStage != 1
            || !Enum.IsDefined(ActingSide)
            || OriginLocationId == DestinationLocationId
            || CohesionBefore is <= -26 or > 10
            || CohesionBefore != CohesionAfter
            || CapabilityPointsExpendedBefore + Cost.TotalCost
                != CapabilityPointsExpendedAfter
            || SequencePosition != expectedPosition
            || !string.Equals(
                FromPositionId,
                SequencePosition.PositionId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException("The ElementMoved event contract is invalid.");
        }
    }

    public bool Equals(ElementMoved? other) => ReferenceEquals(this, other)
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
            && string.Equals(
                DestinationLocationId,
                other.DestinationLocationId,
                StringComparison.Ordinal)
            && string.Equals(MobilityId, other.MobilityId, StringComparison.Ordinal)
            && MobilitySources.SequenceEqual(other.MobilitySources)
            && Cost == other.Cost
            && CapabilityPointsExpendedBefore == other.CapabilityPointsExpendedBefore
            && CapabilityPointsExpendedAfter == other.CapabilityPointsExpendedAfter
            && CohesionBefore == other.CohesionBefore
            && CohesionAfter == other.CohesionAfter
            && SequencePosition == other.SequencePosition);

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
        hash.Add(SequencePosition);
        return hash.ToHashCode();
    }
}

internal sealed record MovementSegmentCompleted : CampaignEvent
{
    public MovementSegmentCompleted(
        string campaignId,
        long stateVersion,
        long priorStateVersion,
        string fromPositionId,
        int gameTurn,
        int operationStage,
        LandSide actingSide,
        LandSequencePosition sequencePosition)
        : base(1, campaignId, stateVersion)
    {
        ArgumentNullException.ThrowIfNull(sequencePosition);
        PriorStateVersion = priorStateVersion;
        FromPositionId = ContentContractGuards.RequireStableId(
            fromPositionId,
            nameof(fromPositionId));
        GameTurn = gameTurn;
        OperationStage = operationStage;
        ActingSide = actingSide;
        SequencePosition = sequencePosition;
        ValidateContract();
    }

    public long PriorStateVersion { get; }

    public string FromPositionId { get; }

    public int GameTurn { get; }

    public int OperationStage { get; }

    public LandSide ActingSide { get; }

    public LandSequencePosition SequencePosition { get; }

    internal void ValidateContract()
    {
        _ = ContentContractGuards.RequireStableId(CampaignId, nameof(CampaignId));
        var movement = Cna1979LandSequence.CreateTurn(GameTurn).Single(value =>
            value.OperationStage == 1
            && value.PhaseId == LandPhaseIds.MovementAndCombat
            && value.SegmentId == LandSegmentIds.Movement
            && value.ActorRole == LandActorRole.FirstActingSide);
        var breakdown = Cna1979LandSequence.GetNext(movement);
        if (ContractVersion != 1
            || StateVersion < 12
            || PriorStateVersion < 11
            || checked(PriorStateVersion + 1) != StateVersion
            || OperationStage != 1
            || !Enum.IsDefined(ActingSide)
            || !string.Equals(FromPositionId, movement.PositionId, StringComparison.Ordinal)
            || SequencePosition != breakdown
            || breakdown.GameTurn != GameTurn
            || breakdown.OperationStage != OperationStage
            || breakdown.PhaseId != LandPhaseIds.MovementAndCombat
            || breakdown.SegmentId != LandSegmentIds.BreakdownDetermination
            || breakdown.ActorRole != LandActorRole.FirstActingSide
            || breakdown.ActiveSide is not null)
        {
            throw new ArgumentException(
                "The MovementSegmentCompleted event contract is invalid.");
        }
    }
}
