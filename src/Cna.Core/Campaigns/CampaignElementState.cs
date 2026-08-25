using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal enum CampaignElementReserveStatus
{
    None = 0,
    ReserveI = 1,
    ReserveII = 2,
}

internal sealed record CampaignElementState
{
    public CampaignElementState(string elementId, string currentLocationId)
        : this(elementId, currentLocationId, CampaignElementReserveStatus.None)
    {
    }

    public CampaignElementState(
        string elementId,
        string currentLocationId,
        CampaignElementReserveStatus reserveStatus)
    {
        if (!Enum.IsDefined(reserveStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(reserveStatus));
        }

        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        CurrentLocationId = ContentContractGuards.RequireStableId(
            currentLocationId,
            nameof(currentLocationId));
        ReserveStatus = reserveStatus;
    }

    public string ElementId { get; }

    public string CurrentLocationId { get; }

    public CampaignElementReserveStatus ReserveStatus { get; }
}
