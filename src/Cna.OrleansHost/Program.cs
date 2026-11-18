using Cna.Core.Campaigns;
using Cna.OrleansHost;
using Cna.OrleansHost.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IContentPackV6Resolver>(Cna1979SyntheticContentResolver.Instance);
builder.Services.AddSingleton<LocalSessionStore>();
builder.Services.AddSingleton<LocalCampaignCreationStore>();
builder.Services.AddSingleton<ITemporaryPrivateCutReader, TemporaryPrivateCutReader>();
builder.Services.AddSingleton<ITemporaryCampaignCreator, TemporaryCampaignCreator>();
builder.Services.AddSingleton<TemporaryOutwardActionIngress>();
builder.Services.AddSingleton<TemporaryPlayerViewProjection>();
// CampaignJournal:Path is now a directory, not a single file: the design doc's §3
// (docs/work/plans/2026-11-18-data-layer-design-1000-campaign-ceiling.md) moves the hot tier from
// one SQLite file shared by every campaign to one file per campaign under this directory, to
// remove the artificial cross-campaign lock contention §1.3 identifies (each campaign's grain is
// already single-writer by Orleans's own model; only the shared file forced them to contend).
// CampaignDurableAuthorityJournalStore resolves/creates each campaign's own file lazily, on first
// use, and caches it for the life of the host -- see its own remarks for why
// CampaignDurableAuthorityJournal itself did not need to change shape to support this.
var campaignJournalDirectory = builder.Configuration["CampaignJournal:Path"];
if (!string.IsNullOrWhiteSpace(campaignJournalDirectory))
{
    var operatorSecret = builder.Configuration["CampaignJournal:OperatorSecret"];
    if (string.IsNullOrWhiteSpace(operatorSecret))
        throw new InvalidOperationException(
            "CampaignJournal:OperatorSecret is required when the durable journal is configured.");
    builder.Services.AddSingleton(services => new CampaignDurableAuthorityJournalStore(
        campaignJournalDirectory, operatorSecret,
        services.GetRequiredService<TimeProvider>(), services.GetRequiredService<IContentPackV6Resolver>()));
    builder.Services.AddHostedService<CampaignCombatTimeoutService>();
}
// https://learn.microsoft.com/en-us/dotnet/orleans/host/configuration-guide/server-configuration
builder.Host.UseOrleans(siloBuilder =>
{
    if (builder.Environment.IsDevelopment())
    {
        // Single-machine, dev-only clustering provider. Fine for local development,
        // but silently using it in any other environment would produce a silo that
        // can never form a multi-instance cluster.
        siloBuilder.UseTemporaryCampaignDevelopmentSilo();
    }
    else
    {
        // TODO: wire up a persistent clustering provider (e.g. Azure Table Storage,
        // ADO.NET, or Kubernetes) before deploying outside local development. There is
        // no production clustering provider configured yet, so fail loudly here instead
        // of silently falling back to localhost clustering, which would misbehave (or
        // simply not cluster) outside a single dev machine.
        throw new InvalidOperationException(
            "No production Orleans clustering provider is configured. " +
            "UseLocalhostClustering() is dev-only and cannot be used outside Development. " +
            "Configure a persistent clustering provider before deploying this host.");
    }
});

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapLocalGameApi();
if (builder.Configuration.GetValue<bool>("CampaignJournal:EnableOutwardApi"))
    app.MapDurableCampaignApi();
app.MapGet("/", () => Results.Ok(new
{
    service = "Cna.OrleansHost",
    authoritative = true,
}));

app.Run();

public partial class Program;
