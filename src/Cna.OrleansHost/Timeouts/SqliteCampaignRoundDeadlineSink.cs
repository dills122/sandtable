using Cna.OrleansHost.Api;
using Cna.OrleansHost.Campaigns;
using Orleans;

namespace Cna.OrleansHost.Timeouts;

/// <summary>
/// The <see cref="ICampaignRoundDeadlineSink"/> implementation against the still-SQLite journal
/// for Phase C3a of issue #439 -- the storage engine stays SQLite this phase (the Marten swap is
/// C3b); only the activation boundary and deadline enforcement change. No implementation shipped
/// in Phase C4 (see docs/work/plans/2026-11-19-phase-c4-grain-wiring-fork.md); this is it.
/// </summary>
/// <remarks>
/// <see cref="GetDeadlineAsync"/> is a plain read and goes straight to
/// <see cref="CampaignDurableAuthorityJournalStore"/> -- it does not need this campaign's
/// single-writer boundary, and routing it through <c>CampaignGrain</c> as well would add a
/// pointless extra grain hop on every activation/reminder tick for no correctness benefit.
/// <see cref="TickAsync"/> is a write that can itself open or close a different round (e.g. a
/// Selection timeout cascading into a fresh Reserve Release round), so it goes through
/// <c>CampaignGrain</c> -- the same single-writer boundary and round-timer-arming logic every
/// other mutating entry point uses.
/// </remarks>
internal sealed class SqliteCampaignRoundDeadlineSink(CampaignDurableAuthorityJournalStore store,
    IGrainFactory grainFactory) : ICampaignRoundDeadlineSink
{
    public Task<DateTimeOffset?> GetDeadlineAsync(string campaignId, CampaignRoundKind roundKind,
        CancellationToken cancellationToken)
    {
        var deadlineMs = store.GetRoundDeadlineMs(campaignId, roundKind);
        return Task.FromResult(deadlineMs is { } value
            ? DateTimeOffset.FromUnixTimeMilliseconds(value)
            : (DateTimeOffset?)null);
    }

    public async Task<bool> TickAsync(string campaignId, CampaignRoundKind roundKind,
        CancellationToken cancellationToken) =>
        await grainFactory.GetGrain<ICampaignGrain>(campaignId).TickRoundAsync(roundKind);
}
