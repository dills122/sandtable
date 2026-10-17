namespace Cna.OrleansHost.Api;

internal interface ITemporaryCampaignCreator
{
    Task<TemporaryCampaignReply> CreateAsync(string campaignId,
        string operatorId, string requestId);
}

internal sealed class TemporaryCampaignCreator(
    IGrainFactory grains, TemporaryCampaignAccess access) : ITemporaryCampaignCreator
{
    public Task<TemporaryCampaignReply> CreateAsync(string campaignId,
        string operatorId, string requestId) =>
        grains.GetGrain<ITemporaryCampaignGrain>(campaignId)
            .CreateAsync(access.Capability, operatorId, requestId);
}
