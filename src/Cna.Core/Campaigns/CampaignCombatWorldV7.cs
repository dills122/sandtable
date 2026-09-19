using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCombatInitializationPolicy
{
    public CampaignCombatInitializationPolicy(int contractVersion, int gameTurn, int operationStage,
        CapabilityPointAmount capabilityPointsExpended, int cohesionLevel,
        CampaignElementReserveStatus reserveStatus, ContentOrigin origin)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(contractVersion, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);
        if (operationStage is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(operationStage));
        ArgumentNullException.ThrowIfNull(capabilityPointsExpended);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cohesionLevel, 10);
        if (!Enum.IsDefined(reserveStatus)) throw new ArgumentOutOfRangeException(nameof(reserveStatus));
        ArgumentNullException.ThrowIfNull(origin);
        ContractVersion = contractVersion;
        GameTurn = gameTurn;
        OperationStage = operationStage;
        CapabilityPointsExpended = capabilityPointsExpended;
        CohesionLevel = cohesionLevel;
        ReserveStatus = reserveStatus;
        Origin = origin;
    }

    public int ContractVersion { get; }
    public int GameTurn { get; }
    public int OperationStage { get; }
    public CapabilityPointAmount CapabilityPointsExpended { get; }
    public int CohesionLevel { get; }
    public CampaignElementReserveStatus ReserveStatus { get; }
    public ContentOrigin Origin { get; }
}

internal sealed record CampaignElementOperationalStateV6
{
    public CampaignElementOperationalStateV6(int ledgerGameTurn, int ledgerOperationStage,
        CapabilityPointAmount capabilityPointsExpended, int cohesionLevel,
        CampaignVehicleBreakdownState? vehicleBreakdownState,
        CampaignMovementEndedState? movementEnded, ContentOrigin initialLedgerOrigin)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ledgerGameTurn, 1);
        if (ledgerOperationStage is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(ledgerOperationStage));
        ArgumentNullException.ThrowIfNull(capabilityPointsExpended);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cohesionLevel, 10);
        ArgumentNullException.ThrowIfNull(initialLedgerOrigin);
        if (movementEnded is not null &&
            (movementEnded.GameTurn != ledgerGameTurn || movementEnded.OperationStage != ledgerOperationStage))
            throw new ArgumentException("Movement-ended scope must equal ledger scope.", nameof(movementEnded));
        LedgerGameTurn = ledgerGameTurn;
        LedgerOperationStage = ledgerOperationStage;
        CapabilityPointsExpended = capabilityPointsExpended;
        CohesionLevel = cohesionLevel;
        VehicleBreakdownState = vehicleBreakdownState;
        MovementEnded = movementEnded;
        InitialLedgerOrigin = initialLedgerOrigin;
    }

    public int LedgerGameTurn { get; }
    public int LedgerOperationStage { get; }
    public CapabilityPointAmount CapabilityPointsExpended { get; }
    public int CohesionLevel { get; }
    public CampaignVehicleBreakdownState? VehicleBreakdownState { get; }
    public CampaignMovementEndedState? MovementEnded { get; }
    public ContentOrigin InitialLedgerOrigin { get; }
}

internal sealed record CampaignElementAmmunitionState
{
    public CampaignElementAmmunitionState(int points, ContentOrigin initialAmmunitionOrigin)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(points);
        ArgumentNullException.ThrowIfNull(initialAmmunitionOrigin);
        Points = points;
        InitialAmmunitionOrigin = initialAmmunitionOrigin;
    }

    public int Points { get; }
    public ContentOrigin InitialAmmunitionOrigin { get; }
}

internal sealed record CampaignElementCombatReadinessState
{
    public CampaignElementCombatReadinessState(int gameTurn, int operationStage, string waterStatus,
        string storesStatus, bool pinned, ContentOrigin initialReadinessOrigin)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);
        if (operationStage is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(operationStage));
        ArgumentNullException.ThrowIfNull(initialReadinessOrigin);
        GameTurn = gameTurn;
        OperationStage = operationStage;
        WaterStatus = ContentContractGuards.RequireStableId(waterStatus, nameof(waterStatus));
        StoresStatus = ContentContractGuards.RequireStableId(storesStatus, nameof(storesStatus));
        Pinned = pinned;
        InitialReadinessOrigin = initialReadinessOrigin;
    }

    public int GameTurn { get; }
    public int OperationStage { get; }
    public string WaterStatus { get; }
    public string StoresStatus { get; }
    public bool Pinned { get; }
    public ContentOrigin InitialReadinessOrigin { get; }
}

internal sealed record CampaignElementStateV6
{
    public CampaignElementStateV6(string elementId, string currentLocationId,
        CampaignElementReserveStatus reserveStatus, CampaignElementOperationalStateV6 operationalState,
        IEnumerable<CampaignComponentToeState> components, string sourceParentFormationId,
        string currentParentFormationId, CampaignElementAmmunitionState ammunition,
        CampaignElementCombatReadinessState readiness)
    {
        if (!Enum.IsDefined(reserveStatus)) throw new ArgumentOutOfRangeException(nameof(reserveStatus));
        ArgumentNullException.ThrowIfNull(operationalState);
        ArgumentNullException.ThrowIfNull(ammunition);
        ArgumentNullException.ThrowIfNull(readiness);
        var componentCopy = ContentContractGuards.CopyValues(components, nameof(components));
        if (componentCopy.Select(value => value.ComponentId).Distinct(StringComparer.Ordinal).Count() != componentCopy.Length)
            throw new ArgumentException("Component IDs must be unique.", nameof(components));
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        CurrentLocationId = ContentContractGuards.RequireStableId(currentLocationId, nameof(currentLocationId));
        ReserveStatus = reserveStatus;
        OperationalState = operationalState;
        Components = Array.AsReadOnly(componentCopy.OrderBy(value => value.ComponentId, StringComparer.Ordinal).ToArray());
        SourceParentFormationId = ContentContractGuards.RequireStableId(sourceParentFormationId, nameof(sourceParentFormationId));
        CurrentParentFormationId = ContentContractGuards.RequireStableId(currentParentFormationId, nameof(currentParentFormationId));
        Ammunition = ammunition;
        Readiness = readiness;
    }

    public string ElementId { get; }
    public string CurrentLocationId { get; }
    public CampaignElementReserveStatus ReserveStatus { get; }
    public CampaignElementOperationalStateV6 OperationalState { get; }
    public IReadOnlyList<CampaignComponentToeState> Components { get; }
    public string SourceParentFormationId { get; }
    public string CurrentParentFormationId { get; }
    public CampaignElementAmmunitionState Ammunition { get; }
    public CampaignElementCombatReadinessState Readiness { get; }

    public bool Equals(CampaignElementStateV6? other) => ReferenceEquals(this, other) ||
        (other is not null && ElementId == other.ElementId && CurrentLocationId == other.CurrentLocationId &&
         ReserveStatus == other.ReserveStatus && OperationalState == other.OperationalState &&
         Components.SequenceEqual(other.Components) && SourceParentFormationId == other.SourceParentFormationId &&
         CurrentParentFormationId == other.CurrentParentFormationId && Ammunition == other.Ammunition &&
         Readiness == other.Readiness);

    public override int GetHashCode() => HashCode.Combine(ElementId, CurrentLocationId, ReserveStatus,
        OperationalState, SourceParentFormationId, CurrentParentFormationId, Ammunition, Readiness);
}
