using Cna.Core.Actions;

namespace Cna.OrleansHost.Api;

internal interface ITemporaryPrivateCutReader
{
    Task<TemporaryPrivateCutReply> ReadAsync(string campaignId,
        CampaignActionAudience audience, string sessionGeneration);
}

internal sealed class TemporaryPrivateCutReader(
    IGrainFactory grains, TemporaryCampaignAccess access) : ITemporaryPrivateCutReader
{
    public Task<TemporaryPrivateCutReply> ReadAsync(string campaignId,
        CampaignActionAudience audience, string sessionGeneration) =>
        grains.GetGrain<ITemporaryCampaignGrain>(campaignId)
            .QueryPrivateSideAsync(access.Capability, audience, sessionGeneration);
}
