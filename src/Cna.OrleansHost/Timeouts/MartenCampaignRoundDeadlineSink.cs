using Cna.OrleansHost.Api;
using Cna.OrleansHost.Campaigns;
using Orleans;

namespace Cna.OrleansHost.Timeouts;

/// <summary>
/// The <see cref="ICampaignRoundDeadlineSink"/> implementation over the campaign journal store.
/// Introduced in Phase C3a of issue #439 as <c>SqliteCampaignRoundDeadlineSink</c>; renamed in
/// Phase C3b, when the production store became Postgres + Marten
/// (<see cref="CampaignDurableAuthorityJournalStore.IsMartenBacked"/>). The contract is unchanged.
/// </summary>
/// <remarks>
/// <see cref="GetDeadlineAsync"/> is a plain read and goes straight to the store -- against Marten
/// an O(1) load of that round kind's C2 projection document, whose retained rows now carry the
/// write-once <c>deadline_ms</c> (design spike &#167;3.3); no stream fetch, no replay, no grain hop
/// on every timeout-grain activation/reminder tick. <see cref="TickAsync"/> is a write that can
/// itself open or close a different round (e.g. a Selection timeout cascading into a fresh Reserve
/// Release round), so it goes through <c>CampaignGrain</c> -- the same single-writer boundary and
/// round-timer-arming logic every other mutating entry point uses.
/// </remarks>
internal sealed class MartenCampaignRoundDeadlineSink(CampaignDurableAuthorityJournalStore store,
    IGrainFactory grainFactory) : ICampaignRoundDeadlineSink
{
    public async Task<DateTimeOffset?> GetDeadlineAsync(string campaignId, CampaignRoundKind roundKind,
        CancellationToken cancellationToken)
    {
        long? deadlineMs;
        try
        {
            deadlineMs = await store.GetRoundDeadlineMsAsync(campaignId, roundKind, cancellationToken);
        }
        catch (ArgumentException)
        {
            // Not a campaign id this store could ever hold (C3a's file-store read returned null for
            // a campaign it has no journal for; an unusable id is the same "nothing to arm" answer).
            return null;
        }
        return deadlineMs is { } value ? DateTimeOffset.FromUnixTimeMilliseconds(value) : null;
    }

    public async Task<bool> TickAsync(string campaignId, CampaignRoundKind roundKind,
        CancellationToken cancellationToken) =>
        await grainFactory.GetGrain<ICampaignGrain>(campaignId).TickRoundAsync(roundKind);
}
