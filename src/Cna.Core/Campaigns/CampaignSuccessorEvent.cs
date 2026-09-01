using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal abstract record CampaignSuccessorEvent
{
    protected CampaignSuccessorEvent(
        int contractVersion,
        string campaignId,
        long stateVersion)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(contractVersion, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        ContractVersion = contractVersion;
        CampaignId = ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        StateVersion = stateVersion;
    }

    public int ContractVersion { get; }

    public string CampaignId { get; }

    public long StateVersion { get; }
}
