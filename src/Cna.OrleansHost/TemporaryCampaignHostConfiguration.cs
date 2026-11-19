using Microsoft.Extensions.DependencyInjection;
using Orleans.Hosting;

namespace Cna.OrleansHost;

internal static class TemporaryCampaignHostConfiguration
{
    internal static ISiloBuilder UseTemporaryCampaignDevelopmentSilo(
        this ISiloBuilder silo, int? siloPort = null)
    {
        // A zero gateway port disables remote Orleans client ingress. This trusted-only
        // grain must never be reachable by a browser or another process.
        if (siloPort is { } port)
            silo.UseLocalhostClustering(siloPort: port, gatewayPort: 0);
        else
            silo.UseLocalhostClustering(gatewayPort: 0);
        silo.ConfigureServices(services =>
        {
            services.AddSingleton<TemporaryCampaignMemoryStore>();
            services.AddSingleton<TemporaryCampaignAccess>();
        });
        // Phase C3a of issue #439: CampaignRoundTimeoutGrain's reminder-backstop
        // (design doc §4.2) needs a reminder service to activate at all, in every environment --
        // including this dev-only silo, once CampaignJournal:EnableOutwardApi actually arms one
        // for the first time. Development has no real reminder storage, so the in-memory
        // provider (dev/test-only, per its own documentation) stands in, same as every test in
        // this codebase that already exercises reminder-backed grains.
        silo.AddReminders();
        silo.UseInMemoryReminderService();
        return silo;
    }
}
