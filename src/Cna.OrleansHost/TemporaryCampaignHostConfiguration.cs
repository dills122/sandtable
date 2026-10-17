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
        return silo;
    }
}
