using Cna.Core.Campaigns;
using Cna.OrleansHost;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Embedded;
using Cna.OrleansHost.Persistence;
using Cna.OrleansHost.Timeouts;
using Orleans.Configuration;

var builder = WebApplication.CreateBuilder(args);
using var localPlay = LocalPlayPilot.Configure(builder);

builder.AddServiceDefaults();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IContentPackV6Resolver>(Cna1979SyntheticContentResolver.Instance);
builder.Services.AddSingleton<LocalSessionStore>();
builder.Services.AddSingleton<LocalCampaignCreationStore>();
builder.Services.AddSingleton<ITemporaryPrivateCutReader, TemporaryPrivateCutReader>();
builder.Services.AddSingleton<ITemporaryCampaignCreator, TemporaryCampaignCreator>();
builder.Services.AddSingleton<TemporaryOutwardActionIngress>();
builder.Services.AddSingleton<TemporaryPlayerViewProjection>();
// Phase C3b of issue #439 (docs/work/plans/2026-11-19-phase-c3b-durable-state-design-spike.md):
// the durable campaign journal's store is PostgreSQL + Marten -- the hot-tier database named by
// SANDTABLE_POSTGRES_HOT_CONNECTION_STRING (see .env.example), one Marten stream per campaign keyed
// by its campaign id, with host operational state (seats, openings, reference keys, open-round
// high-water marks, operator capability) as optimistic-concurrency Marten documents. The former
// file-per-campaign SQLite store (CampaignJournal:Path) is retired as a host configuration: its
// class survives as the test-side equivalence baseline the Marten store was proven against
// (tests/Cna.OrleansHost.Tests/CampaignDurableAuthorityJournalTests.MartenEquivalence.cs) and
// is used by the explicitly opted-in, Development-only LocalPlayPilot above.
var campaignJournalOperatorSecret = builder.Configuration["CampaignJournal:OperatorSecret"];
var hotConnectionString = builder.Configuration["SANDTABLE_POSTGRES_HOT_CONNECTION_STRING"];
if (!string.IsNullOrWhiteSpace(builder.Configuration["CampaignJournal:Path"]))
    throw new InvalidOperationException(
        "CampaignJournal:Path (the SQLite file store) is retired; configure " +
        "SANDTABLE_POSTGRES_HOT_CONNECTION_STRING and CampaignJournal:OperatorSecret instead.");
if (localPlay is null && !string.IsNullOrWhiteSpace(hotConnectionString))
{
    if (string.IsNullOrWhiteSpace(campaignJournalOperatorSecret))
        throw new InvalidOperationException(
            "CampaignJournal:OperatorSecret is required when the durable journal is configured.");
    // Marten's default AutoCreate (CreateOrUpdate) provisions/extends the campaign schema on first
    // use. Design doc §3.5's generated-migration-script workflow for production is Phase D's job.
    var campaignDocumentStore = MartenStoreFactory.CreateStore(hotConnectionString, CampaignEventStreamSchema.Configure);
    var campaignJournalBackend = await CampaignJournalMartenBackend.CreateAsync(campaignDocumentStore,
        campaignJournalOperatorSecret, TimeProvider.System, Cna1979SyntheticContentResolver.Instance);
    builder.Services.AddSingleton<Marten.IDocumentStore>(campaignDocumentStore);
    builder.Services.AddSingleton(new CampaignDurableAuthorityJournalStore(campaignJournalBackend));
    // Phase C3a's grain-local deadline enforcement (CampaignGrain-armed CampaignRoundTimeoutGrain
    // instances, one per campaign+round-kind) now reads its durable deadlines from Marten through
    // this sink (renamed from C3a's SqliteCampaignRoundDeadlineSink; contract unchanged).
    // CampaignCombatTimeoutService's cross-campaign sweep stays retired (C3a).
    builder.Services.AddSingleton<ICampaignRoundDeadlineSink, MartenCampaignRoundDeadlineSink>();
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
        siloBuilder.UseTemporaryCampaignDevelopmentSilo(localPlay?.SiloPort);
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
if (localPlay is not null || builder.Configuration.GetValue<bool>("CampaignJournal:EnableOutwardApi"))
    app.MapDurableCampaignApi();
app.MapGet("/", () => Results.Ok(new
{
    service = "Cna.OrleansHost",
    authoritative = true,
}));

app.Run();

public partial class Program;
