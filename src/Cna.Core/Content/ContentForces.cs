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
        ContentOrigin origin)
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
    }

    public string ElementId { get; }

    public string SideId { get; }

    public string ParentFormationId { get; }

    public string OrganizationId { get; }

    public string MobilityId { get; }

    public int BaseCapabilityPointAllowance { get; }

    public ContentPlacementMode PlacementMode { get; }

    public ContentOrigin Origin { get; }
}
