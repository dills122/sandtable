using Cna.OrleansHost.Api;
using Cna.OrleansHost.Campaigns;

namespace Cna.LoadHarness;

internal sealed record Submission(bool IsAccepted, string? RejectionCode, long? CommittedStateVersion);

internal interface ICampaignDriver
{
    Task<CampaignSeatCredential[]> CreateAsync(string id);
    Task<CampaignOpenSeatResult> DiscoverAsync(CampaignSeatCredential seat);
    Task<CampaignGrainOpeningResult> OpenAsync(CampaignSeatCredential seat, string stage, string element, string operation);
    Task<Submission> SubmitAsync(CampaignSeatCredential seat, string stage, string element, long revision, string reference);
}
