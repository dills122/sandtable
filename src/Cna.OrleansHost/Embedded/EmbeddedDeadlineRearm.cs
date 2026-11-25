using Cna.OrleansHost.Api;
using Cna.OrleansHost.Timeouts;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;

namespace Cna.OrleansHost.Embedded;

/// <summary>
/// Embedded-mode boot re-arm (design: <c>docs/work/plans/2026-11-25-embedded-local-journal-design.md</c>).
/// </summary>
/// <remarks>
/// <para><b>Why this exists.</b> Postgres mode survives a process restart because the Orleans
/// reminder rows live in Postgres: after a restart the reminder service re-activates every
/// <see cref="CampaignRoundTimeoutGrain"/> that had one, and that activation re-derives its timer from
/// the durable deadline. Embedded mode uses Orleans' in-memory reminder service
/// (<c>UseInMemoryReminderService</c>), whose registrations die with the process, so after a restart
/// nothing activates the timeout grains and an open round's deadline would never fire until someone
/// happened to touch that campaign. This class closes that gap: on silo start it enumerates every
/// collecting round across the embedded journal directory (the same seven
/// <c>CollectingXCampaigns</c> lanes the retired sweep used) and arms the matching timeout grain
/// from the durable deadline. An already-passed deadline fires immediately, so a campaign whose
/// deadline elapsed while the process was down times out at boot, identically to the sweep.</para>
/// <para>Idempotent: arming an already-armed grain just re-derives its timer. Per-campaign failures
/// are logged and skipped so one corrupt journal file cannot block the rest.</para>
/// </remarks>
internal sealed class EmbeddedDeadlineRearm(CampaignDurableAuthorityJournalStore store,
    IGrainFactory grainFactory, ILogger<EmbeddedDeadlineRearm> logger)
{
    internal const int BatchSize = 64;

    private static readonly Action<ILogger, string, CampaignRoundKind, Exception?> LogRearmFailed =
        LoggerMessage.Define<string, CampaignRoundKind>(LogLevel.Error, new EventId(1, "EmbeddedRearmFailed"),
            "Embedded boot re-arm failed for campaign {CampaignId} round {RoundKind}.");
    private static readonly Action<ILogger, string, Exception?> LogEnumerationFailed =
        LoggerMessage.Define<string>(LogLevel.Error, new EventId(2, "EmbeddedRearmEnumerationFailed"),
            "Embedded boot re-arm could not enumerate collecting {Lane} rounds.");

    /// <summary>Arms a timeout grain for every currently collecting round; returns how many were armed.</summary>
    internal async Task<int> RearmAsync(CancellationToken cancellationToken)
    {
        var armed = 0;
        (CampaignRoundKind Kind, Func<string, int, IReadOnlyList<string>> Enumerate)[] lanes =
        [
            (CampaignRoundKind.Selection, store.CollectingSelectionCampaigns),
            (CampaignRoundKind.Rba, store.CollectingRbaCampaigns),
            (CampaignRoundKind.Force, store.CollectingForceCampaigns),
            (CampaignRoundKind.Retreat, store.CollectingRetreatCampaigns),
            (CampaignRoundKind.Custody, store.CollectingCustodyCampaigns),
            (CampaignRoundKind.Reserve, store.CollectingReserveCampaigns),
            (CampaignRoundKind.Cycle, store.CollectingCycleCampaigns),
        ];
        foreach (var (kind, enumerate) in lanes)
        {
            var cursor = "";
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IReadOnlyList<string> page;
                try { page = enumerate(cursor, BatchSize); }
                catch (Exception exception) when (exception is InvalidOperationException or ArgumentException
                    or Microsoft.Data.Sqlite.SqliteException)
                {
                    LogEnumerationFailed(logger, kind.ToString(), exception);
                    break;
                }
                if (page.Count == 0) break;
                foreach (var campaignId in page)
                {
                    cursor = campaignId;
                    try
                    {
                        var deadlineMs = await store.GetRoundDeadlineMsAsync(campaignId, kind, cancellationToken)
                            .ConfigureAwait(false);
                        if (deadlineMs is not { } ms) continue;
                        await grainFactory.GetGrain<ICampaignRoundTimeoutGrain>(
                                CampaignRoundTimeoutGrainKey.For(campaignId, kind))
                            .ArmAsync(DateTimeOffset.FromUnixTimeMilliseconds(ms)).ConfigureAwait(false);
                        armed++;
                    }
                    catch (Exception exception) when (exception is InvalidOperationException or ArgumentException
                        or Microsoft.Data.Sqlite.SqliteException)
                    {
                        LogRearmFailed(logger, campaignId, kind, exception);
                    }
                }
            }
        }
        return armed;
    }
}

/// <summary>Runs <see cref="EmbeddedDeadlineRearm"/> once when the silo reaches the Active stage.</summary>
internal sealed class EmbeddedDeadlineRearmParticipant(EmbeddedDeadlineRearm rearm)
    : ILifecycleParticipant<ISiloLifecycle>
{
    public void Participate(ISiloLifecycle lifecycle) =>
        lifecycle.Subscribe(nameof(EmbeddedDeadlineRearmParticipant), ServiceLifecycleStage.Active,
            async cancellationToken => { _ = await rearm.RearmAsync(cancellationToken).ConfigureAwait(false); },
            _ => Task.CompletedTask);
}
