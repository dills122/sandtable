using Cna.OrleansHost;
using Cna.OrleansHost.Api;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<LocalSessionStore>();
builder.Services.AddSingleton<LocalCampaignCreationStore>();
builder.Services.AddSingleton<ITemporaryPrivateCutReader, TemporaryPrivateCutReader>();
builder.Services.AddSingleton<ITemporaryCampaignCreator, TemporaryCampaignCreator>();
builder.Services.AddSingleton<TemporaryPlayerViewProjection>();
var campaignJournalPath = builder.Configuration["CampaignJournal:Path"];
if (!string.IsNullOrWhiteSpace(campaignJournalPath))
{
    var operatorSecret = builder.Configuration["CampaignJournal:OperatorSecret"];
    if (string.IsNullOrWhiteSpace(operatorSecret))
        throw new InvalidOperationException(
            "CampaignJournal:OperatorSecret is required when the durable journal is configured.");
    builder.Services.AddSingleton(services => File.Exists(campaignJournalPath)
        ? CampaignDurableAuthorityJournal.OpenExisting(campaignJournalPath,
            services.GetRequiredService<TimeProvider>())
        : CampaignDurableAuthorityJournal.CreateNew(campaignJournalPath,
            operatorSecret, services.GetRequiredService<TimeProvider>()));
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
