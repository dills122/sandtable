using Cna.Core.Campaigns;
using Cna.OrleansHost;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Persistence;
using Cna.OrleansHost.Timeouts;
using Orleans.Configuration;

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
    // Phase C3a of issue #439 (docs/work/plans/2026-11-18-postgres-migration-design.md §4.2):
    // real round-deadline enforcement now runs through CampaignGrain-armed
    // CampaignRoundTimeoutGrain instances (one per campaign+round-kind), with this sink as the
    // bridge to the still-SQLite journal (see SqliteCampaignRoundDeadlineSink's own remarks).
    // CampaignCombatTimeoutService's cross-campaign sweep is retired (no longer registered) now
    // that equivalence with this grain-based path is proven for all seven round kinds -- see
    // tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.GrainEquivalence.cs and
    // this phase's PR description. The class itself stays (not deleted): the equivalence tests
    // still construct it directly as their sweep-path baseline, and
    // tests/Cna.OrleansHost.FoundationChild's own cross-process harness still calls SweepOnce
    // directly to drive its fixture.
    builder.Services.AddSingleton<ICampaignRoundDeadlineSink, SqliteCampaignRoundDeadlineSink>();
}
// Phase C4 of issue #439 (docs/work/plans/2026-11-18-postgres-migration-design.md §4.1):
// Microsoft.Orleans.Clustering.AdoNet / Microsoft.Orleans.Reminders.AdoNet against PostgreSQL
// via Npgsql, for every environment except Development. This is Orleans' own infrastructure
// bookkeeping (silo membership, reminder durability) -- a separate database from the hot/cold
// Marten stores (design doc §2's "N stores split by domain"), configured here, never mixed with
// campaign data. The connection string follows the same env-var-backed pattern as
// SANDTABLE_POSTGRES_HOT_CONNECTION_STRING/SANDTABLE_POSTGRES_COLD_CONNECTION_STRING (see
// .env.example).
string? orleansConnectionString = null;
if (!builder.Environment.IsDevelopment())
{
    orleansConnectionString = builder.Configuration["SANDTABLE_POSTGRES_ORLEANS_CONNECTION_STRING"];
    if (string.IsNullOrWhiteSpace(orleansConnectionString))
        throw new InvalidOperationException(
            "SANDTABLE_POSTGRES_ORLEANS_CONNECTION_STRING is required outside Development. " +
            "UseLocalhostClustering() is dev-only and cannot be used outside Development. " +
            "Configure Microsoft.Orleans.Clustering.AdoNet/Reminders.AdoNet before deploying this host.");
    // Orleans' ADO.NET providers assume the schema already exists (confirmed via each
    // package's own README: no auto-migration, unlike Marten). Applying it here, guarded by
    // OrleansAdoNetSchema's own existence check, makes repeat silo startups against an
    // already-provisioned database a no-op while still standing up a brand-new one correctly.
    await OrleansAdoNetSchema.EnsureSchemaAsync(orleansConnectionString, CancellationToken.None);
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
        // Explicit, non-"default" cluster/service identity rather than Orleans' own
        // implicit defaults -- required once membership is shared via a real database
        // rather than scoped to one dev process by UseLocalhostClustering().
        siloBuilder.Configure<ClusterOptions>(options =>
        {
            options.ClusterId = builder.Configuration["Orleans:ClusterId"] ?? "sandtable-cluster";
            options.ServiceId = builder.Configuration["Orleans:ServiceId"] ?? "sandtable-campaigns";
        });
        siloBuilder.UseAdoNetClustering(options =>
        {
            options.Invariant = "Npgsql";
            options.ConnectionString = orleansConnectionString;
        });
        // §4.2: reminders here are a durability backstop only (coarse, minutes-scale --
        // see CampaignRoundTimeoutGrain), never the primary deadline mechanism.
        siloBuilder.AddReminders();
        siloBuilder.UseAdoNetReminderService(options =>
        {
            options.Invariant = "Npgsql";
            options.ConnectionString = orleansConnectionString;
        });
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
