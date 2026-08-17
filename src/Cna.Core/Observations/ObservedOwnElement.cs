using Cna.Core.Content;

namespace Cna.Core.Observations;

public sealed record ObservedOwnElement
{
    internal ObservedOwnElement(
        string elementId,
        string parentFormationId,
        string organizationId,
        int baseCapabilityPointAllowance,
        string currentLocationId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(baseCapabilityPointAllowance, 1);

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
    }

    public string ElementId { get; }

    public string ParentFormationId { get; }

    public string OrganizationId { get; }

    public int BaseCapabilityPointAllowance { get; }

    public string CurrentLocationId { get; }
}
