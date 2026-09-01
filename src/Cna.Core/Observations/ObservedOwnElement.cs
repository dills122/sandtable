using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

public enum CampaignObservationReserveStatus
{
    None = 0,
    ReserveI = 1,
    ReserveII = 2,
}

internal interface ICampaignObservedMovementSubject
{
    string OrganizationId { get; }
    int BaseCapabilityPointAllowance { get; }
    string CurrentLocationId { get; }
    CampaignObservationReserveStatus ReserveStatus { get; }
    string MobilityId { get; }
    int LedgerGameTurn { get; }
    int LedgerOperationStage { get; }
    CapabilityPointAmount CapabilityPointsExpended { get; }
    int CohesionLevel { get; }
}

public sealed record ObservedOwnElement : ICampaignObservedMovementSubject
{
    internal ObservedOwnElement(
        string elementId,
        string parentFormationId,
        string organizationId,
        int baseCapabilityPointAllowance,
        string currentLocationId,
        CampaignObservationReserveStatus reserveStatus,
        string mobilityId,
        int ledgerGameTurn,
        int ledgerOperationStage,
        CapabilityPointAmount capabilityPointsExpended,
        int cohesionLevel,
        ObservedOwnVehicleBreakdownRisk? vehicleBreakdownRisk)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(baseCapabilityPointAllowance, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(ledgerGameTurn, 1);

        if (ledgerOperationStage is < 1 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(ledgerOperationStage));
        }

        ArgumentNullException.ThrowIfNull(capabilityPointsExpended);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(cohesionLevel, 10);

        if (!Cna1979Movement.IsSupportedMobilityId(mobilityId))
        {
            throw new ArgumentException(
                "The observed mobility ID is not supported.",
                nameof(mobilityId));
        }

        if (!Enum.IsDefined(reserveStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(reserveStatus));
        }

        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        ParentFormationId = ContentContractGuards.RequireStableId(
            parentFormationId,
            nameof(parentFormationId));
        OrganizationId = ContentContractGuards.RequireStableId(
            organizationId,
            nameof(organizationId));
        BaseCapabilityPointAllowance = baseCapabilityPointAllowance;
        CurrentLocationId = ContentContractGuards.RequireStableId(
            currentLocationId,
            nameof(currentLocationId));
        ReserveStatus = reserveStatus;
        MobilityId = mobilityId;
        LedgerGameTurn = ledgerGameTurn;
        LedgerOperationStage = ledgerOperationStage;
        CapabilityPointsExpended = capabilityPointsExpended;
        CohesionLevel = cohesionLevel;
        VehicleBreakdownRisk = vehicleBreakdownRisk;
    }

    public string ElementId { get; }

    public string ParentFormationId { get; }

    public string OrganizationId { get; }

    public int BaseCapabilityPointAllowance { get; }

    public string CurrentLocationId { get; }

    public CampaignObservationReserveStatus ReserveStatus { get; }

    public string MobilityId { get; }

    public int LedgerGameTurn { get; }

    public int LedgerOperationStage { get; }

    public CapabilityPointAmount CapabilityPointsExpended { get; }

    public int CohesionLevel { get; }

    public ObservedOwnVehicleBreakdownRisk? VehicleBreakdownRisk { get; }
}
