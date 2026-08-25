using Cna.Core.Content;

namespace Cna.Core.Observations;

public enum CampaignObservationReserveStatus
{
    None = 0,
    ReserveI = 1,
    ReserveII = 2,
}

public sealed record ObservedOwnElement
{
    internal ObservedOwnElement(
        string elementId,
        string parentFormationId,
        string organizationId,
        int baseCapabilityPointAllowance,
        string currentLocationId,
        CampaignObservationReserveStatus reserveStatus)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(baseCapabilityPointAllowance, 1);

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
    }

    public string ElementId { get; }

    public string ParentFormationId { get; }

    public string OrganizationId { get; }

    public int BaseCapabilityPointAllowance { get; }

    public string CurrentLocationId { get; }

    public CampaignObservationReserveStatus ReserveStatus { get; }
}
