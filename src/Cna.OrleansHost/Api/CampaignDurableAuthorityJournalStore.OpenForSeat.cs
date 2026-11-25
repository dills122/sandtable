namespace Cna.OrleansHost.Api;

internal sealed partial class CampaignDurableAuthorityJournalStore
{
    /// <summary>Read-only "what is open for my seat" derivation; see the journal method's remarks.</summary>
    internal Task<CampaignOpenSeatResult> QueryOpenForSeatAsync(CampaignSeatCredential credential) =>
        RunAsync(credential.CampaignId, false, journal => journal.QueryOpenForSeat(credential));
}
