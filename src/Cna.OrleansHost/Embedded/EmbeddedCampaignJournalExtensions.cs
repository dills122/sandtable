using Cna.Core.Campaigns;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Timeouts;
using Microsoft.Extensions.DependencyInjection;
using Orleans;
using Orleans.Runtime;

namespace Cna.OrleansHost.Embedded;

/// <summary>
/// DI wiring for the embedded (local-play) durable journal: one SQLite file per campaign under a
/// private directory, the exact journal code the Postgres + Marten backend was proven equivalent
/// to, plus the boot re-arm that replaces Postgres-backed reminders. Spike (phase 1): not yet called
/// from <c>Program.cs</c>; see the design doc for the wiring plan. Never changes the Postgres path.
/// </summary>
internal static class EmbeddedCampaignJournalExtensions
{
    /// <summary>Registers the file-per-campaign store, the deadline sink and the boot re-arm.</summary>
    internal static IServiceCollection AddEmbeddedCampaignJournal(this IServiceCollection services,
        string directory, string operatorSecret, TimeProvider? clock = null,
        IContentPackV6Resolver? contentResolver = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton(new CampaignDurableAuthorityJournalStore(directory, operatorSecret, clock,
            contentResolver));
        return services.AddEmbeddedDeadlineRearm();
    }

    /// <summary>
    /// Registers the sink and boot re-arm over an already-registered store. Split out so tests can
    /// supply a store over a specific journal.
    /// </summary>
    internal static IServiceCollection AddEmbeddedDeadlineRearm(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ICampaignRoundDeadlineSink, MartenCampaignRoundDeadlineSink>();
        services.AddSingleton<EmbeddedDeadlineRearm>();
        services.AddSingleton<ILifecycleParticipant<ISiloLifecycle>, EmbeddedDeadlineRearmParticipant>();
        return services;
    }
}
