using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Campaigns;
using Cna.OrleansHost.Timeouts;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;

namespace Cna.LoadHarness;

internal sealed class InProcessDriver(HarnessConfig config) : IAsyncDisposable, ICampaignDriver
{
    internal const string SetupId = "rules-lab.breakdown.reaction.combat-infantry.v1";
    internal const string TurnBoundarySetupId = "rules-lab.breakdown.turn-boundary.gt1.v1";
    internal const ulong LabSeed = 12345;
    internal const int BudgetMs = 3_600_000;
    internal const string Boundary = "land.position.operation-2.initiative-declaration";
    private readonly string directory = Path.Combine(Path.GetTempPath(), "sandtable-load-" + Guid.NewGuid().ToString("N"));
    private readonly string operatorSecret = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
    private IHost? silo;
    internal string OperatorSecret => config.BaseUrl is null ? operatorSecret
        : Environment.GetEnvironmentVariable("SANDTABLE_LOAD_OPERATOR_SECRET")!;
    internal Uri? Address { get; private set; }
    private PostgresBacking? postgres;
    internal PostgresBacking? Postgres => postgres;
    private CampaignDurableAuthorityJournalStore? store;
    private CampaignDurableAuthorityJournalStore Store => store ?? throw new InvalidOperationException("Silo not started.");
    private IGrainFactory Grains => silo!.Services.GetRequiredService<IGrainFactory>();

    // LocalCampaignSimulator bootstrap. HTTP mode runs the actual outward route map on a loopback Kestrel listener.
    internal async Task StartAsync(CancellationToken cancellationToken)
    {
        if (config.BaseUrl is not null)
        {
            Address = config.BaseUrl;
            return;
        }
        if (config.Store == "postgres")
        {
            postgres = new PostgresBacking(config.HotPool);
            await postgres.StartAsync(cancellationToken).ConfigureAwait(false);
            store = await postgres.CreateStoreAsync(operatorSecret, cancellationToken).ConfigureAwait(false);
        }
        else
            store = new(directory, operatorSecret, contentResolver: Cna1979SyntheticContentResolver.Instance);
        if (config.Driver == "http")
        {
            var web = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
            web.Configuration["CampaignJournal:EnableOutwardApi"] = "true";
            web.Configuration["CampaignJournal:OperatorSecret"] = operatorSecret;
            web.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
            web.Services.AddSingleton(Store);
            web.Services.AddSingleton<ICampaignRoundDeadlineSink, MartenCampaignRoundDeadlineSink>();
            web.Logging.SetMinimumLevel(LogLevel.Warning);
            web.Host.UseOrleans(ConfigureSilo);
            var app = web.Build();
            silo = app;
            if (web.Configuration.GetValue<bool>("CampaignJournal:EnableOutwardApi"))
                app.MapDurableCampaignApi();
            await app.StartAsync(cancellationToken).ConfigureAwait(false);
            Address = new Uri(app.Services.GetRequiredService<IServer>().Features
                .Get<IServerAddressesFeature>()!.Addresses.Single());
            return;
        }

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(Store);
        builder.Services.AddSingleton<ICampaignRoundDeadlineSink, MartenCampaignRoundDeadlineSink>();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.UseOrleans(ConfigureSilo);
        silo = builder.Build();
        await silo.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void ConfigureSilo(ISiloBuilder s)
    {
        s.UseLocalhostClustering(siloPort: FreePort(), gatewayPort: 0);
        s.AddReminders();
        s.UseInMemoryReminderService();
    }

    internal static (CampaignCreationRequest Request, CampaignDecisionTimingConfigurationV1 Timing) Creation(HarnessConfig config, string id)
    {
        var request = config.Until == "turn2-reserve"
            ? new CampaignCreationRequest(1, id, Cna1979Ruleset.Manifest.Hash, LabSeed, TurnBoundarySetupId,
                "sha256:e6f9d8de65a7f01fec9089d3f58e8f94871214a7d628c52508c9aecadca68dc6",
                "rules-lab.content.breakdown-turn-boundary.gt1.v1",
                "sha256:b0ac6f247766a4fbf45f11dae2d69c2893af0128f834a3c5683e6642522563aa",
                "breakdown-turn-boundary-gt1")
            : new CampaignCreationRequest(1, id, Cna1979Ruleset.Manifest.Hash, LabSeed, SetupId,
            "sha256:18734d3693747a35d855205f869917dc53dfdbdb9333ee32cb7764cbddaff3ac",
            "rules-lab.content.breakdown-reaction.combat-infantry.v1",
            "sha256:f6c8e7cbed61be1e99c877323471857b2d6b43a5bcfbe9475a59df9599e3498e",
            "breakdown-reaction-combat-infantry");
        var timing = new CampaignDecisionTimingConfigurationV1("rules-lab.campaign-timing.1", request.RulesetHash,
            request.ContentHash, [new("custody", BudgetMs, "leave-unguarded"),
                new("cycle-control", BudgetMs, "finish-phase"),
                new("force-assignment", BudgetMs, "cancel-incomplete-voluntary-round"),
                new("rba", BudgetMs, "cancel-without-decline"),
                new("reserve-release", BudgetMs, "convert-unresolved-i-retain-ii"),
                new("retreat", BudgetMs, "refuse-retreat"), new("selection", BudgetMs, "no-selection")]);
        return (request, timing);
    }

    public async Task<CampaignSeatCredential[]> CreateAsync(string id)
    {
        var (request, timing) = Creation(config, id);
        await Store.CreateCampaignAsync(request, timing).ConfigureAwait(false);
        var seats = new List<CampaignSeatCredential>();
        foreach (var audience in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
        {
            var grant = await Store.IssueSeatGrantAsync(id, audience, operatorSecret).ConfigureAwait(false);
            seats.Add(await Store.RedeemSeatGrantAsync(grant).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Seat grant was not redeemed."));
        }
        return [.. seats];
    }

    public Task<CampaignOpenSeatResult> DiscoverAsync(CampaignSeatCredential seat) => Store.QueryOpenForSeatAsync(seat);
    public Task<CampaignGrainOpeningResult> OpenAsync(CampaignSeatCredential seat, string stage, string element, string operation) =>
        Grains.GetGrain<ICampaignGrain>(seat.CampaignId).QueryOwnerOpeningAsync(stage, seat, element);
    public async Task<Submission> SubmitAsync(CampaignSeatCredential seat, string stage,
        string element, long revision, string reference)
    {
        var result = await Grains.GetGrain<ICampaignGrain>(seat.CampaignId)
            .SubmitOwnerActionAsync(stage, seat, element, revision, reference).ConfigureAwait(false);
        return new(result.IsAccepted, result.RejectionCode, result.IsAccepted
            ? CampaignActionAcceptanceReceiptSerializer.DeserializeCanonical(result.CanonicalReceipt!).CommittedStateVersion
            : null);
    }

    // Authoritative read is used exclusively for terminal verification, never to select bot actions.
    internal async Task<bool> AtBoundaryAsync(string id, string position = Boundary)
    {
        var session = await Store.ReadVerifiedAsync(id).ConfigureAwait(false);
        return CampaignExercises.QueryCheckpoint(session).PositionId == position
            && CampaignExercises.Reconstruct(session).IsVerified;
    }

    internal async Task<bool> VerifyReplayAsync(string id, ActionCoverage coverage)
    {
        var session = await Store.ReadVerifiedAsync(id).ConfigureAwait(false);
        var verified = CampaignExercises.Reconstruct(session).IsVerified;
        if (verified) coverage.RecordJournal(await ReadCanonicalEventsAsync(id).ConfigureAwait(false));
        return verified;
    }

    private async Task<byte[][]> ReadCanonicalEventsAsync(string id)
    {
        if (postgres is not null) return await postgres.ReadCanonicalEventsAsync(id).ConfigureAwait(false);
        var path = Path.Combine(directory, id + CampaignDurableAuthorityJournal.CampaignFileExtension);
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = path, Mode = SqliteOpenMode.ReadOnly, Pooling = false,
        }.ConnectionString);
        await connection.OpenAsync().ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT canonical FROM (
                SELECT 0 AS sequence, creation_event AS canonical FROM campaigns WHERE campaign_id=$id
                UNION ALL
                SELECT sequence, canonical_event FROM accepted_steps WHERE campaign_id=$id
            ) ORDER BY sequence
            """;
        command.Parameters.AddWithValue("$id", id);
        await using var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
        var records = new List<byte[]>();
        while (await reader.ReadAsync().ConfigureAwait(false)) records.Add((byte[])reader.GetValue(0));
        return records.ToArray();
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (silo is not null)
            {
                try { await silo.StopAsync().ConfigureAwait(false); }
                finally
                {
                    if (silo is WebApplication app) await app.DisposeAsync().ConfigureAwait(false);
                    else silo.Dispose();
                }
            }
        }
        finally
        {
            try
            {
                if (postgres is not null) await postgres.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
            }
        }
    }
}
