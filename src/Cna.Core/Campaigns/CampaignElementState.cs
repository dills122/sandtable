using Cna.Core.Content;

namespace Cna.Core.Campaigns;

public sealed record CampaignElementState
{
    public CampaignElementState(string elementId, string currentLocationId)
    {
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        CurrentLocationId = ContentContractGuards.RequireStableId(
            currentLocationId,
            nameof(currentLocationId));
    }

    public string ElementId { get; }

    public string CurrentLocationId { get; }
}
