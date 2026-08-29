namespace Cna.Core.Content;

public sealed record ContentFormation
{
    public ContentFormation(
        string formationId,
        string sideId,
        string? parentFormationId,
        string organizationId,
        ContentOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(origin);
        FormationId = ContentContractGuards.RequireStableId(formationId, nameof(formationId));
        SideId = ContentContractGuards.RequireStableId(sideId, nameof(sideId));
        ParentFormationId = parentFormationId is null
            ? null
            : ContentContractGuards.RequireStableId(parentFormationId, nameof(parentFormationId));
        OrganizationId = ContentContractGuards.RequireStableId(
            organizationId,
            nameof(organizationId));
        Origin = origin;
    }

    public string FormationId { get; }

    public string SideId { get; }

    public string? ParentFormationId { get; }

    public string OrganizationId { get; }

    public ContentOrigin Origin { get; }
}

public enum ContentPlacementMode
{
    Independent,
    AttachmentOnly,
}

public sealed record ContentBreakdownVehicleCohort
{
    public ContentBreakdownVehicleCohort(
        string cohortId,
        string vehicleTypeId,
        int workingPointCount,
        string profileId,
        ContentOrigin origin)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(workingPointCount, 0);
        ArgumentNullException.ThrowIfNull(origin);
        CohortId = ContentContractGuards.RequireStableId(cohortId, nameof(cohortId));
        VehicleTypeId = ContentContractGuards.RequireStableId(
            vehicleTypeId,
            nameof(vehicleTypeId));
        WorkingPointCount = workingPointCount;
        ProfileId = ContentContractGuards.RequireStableId(profileId, nameof(profileId));
        Origin = origin;
    }

    public string CohortId { get; }

    public string VehicleTypeId { get; }

    public int WorkingPointCount { get; }

    public string ProfileId { get; }

    public ContentOrigin Origin { get; }
}

public sealed record ContentCombatElement
{
    public ContentCombatElement(
        string elementId,
        string sideId,
        string parentFormationId,
        string organizationId,
        string mobilityId,
        int baseCapabilityPointAllowance,
        ContentPlacementMode placementMode,
        ContentOrigin origin,
        ContentBreakdownVehicleCohort? breakdownVehicleCohort = null)
    {
        if (!Enum.IsDefined(placementMode))
        {
            throw new ArgumentOutOfRangeException(nameof(placementMode));
        }

        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(baseCapabilityPointAllowance, 0);
        ArgumentNullException.ThrowIfNull(origin);
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        SideId = ContentContractGuards.RequireStableId(sideId, nameof(sideId));
        ParentFormationId = ContentContractGuards.RequireStableId(
            parentFormationId,
            nameof(parentFormationId));
        OrganizationId = ContentContractGuards.RequireStableId(
            organizationId,
            nameof(organizationId));
        MobilityId = ContentContractGuards.RequireStableId(mobilityId, nameof(mobilityId));
        BaseCapabilityPointAllowance = baseCapabilityPointAllowance;
        PlacementMode = placementMode;
        Origin = origin;
        BreakdownVehicleCohort = breakdownVehicleCohort;
    }

    public string ElementId { get; }

    public string SideId { get; }

    public string ParentFormationId { get; }

    public string OrganizationId { get; }

    public string MobilityId { get; }

    public int BaseCapabilityPointAllowance { get; }

    public ContentPlacementMode PlacementMode { get; }

    public ContentOrigin Origin { get; }

    public ContentBreakdownVehicleCohort? BreakdownVehicleCohort { get; }
}
