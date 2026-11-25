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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Hosting;

namespace Cna.LoadHarness;

internal sealed class InProcessDriver : IAsyncDisposable
{
    internal const string SetupId = "rules-lab.breakdown.reaction.combat-infantry.v1";
    internal const ulong LabSeed = 12345;
    internal const int BudgetMs = 3_600_000;
    internal const string Boundary = "land.position.operation-2.initiative-declaration";
    private readonly string directory = Path.Combine(Path.GetTempPath(), "sandtable-load-" + Guid.NewGuid().ToString("N"));
    private readonly string operatorSecret = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
    private IHost? silo;
    private CampaignDurableAuthorityJournalStore? store;
    private CampaignDurableAuthorityJournalStore Store => store ?? throw new InvalidOperationException("Silo not started.");
    private IGrainFactory Grains => silo!.Services.GetRequiredService<IGrainFactory>();

    // LocalCampaignSimulator bootstrap, but with the multi-campaign journal directory and no web listener.
    internal async Task StartAsync(CancellationToken cancellationToken)
    {
        store = new(directory, operatorSecret, contentResolver: Cna1979SyntheticContentResolver.Instance);
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(Store);
        builder.Services.AddSingleton<ICampaignRoundDeadlineSink, MartenCampaignRoundDeadlineSink>();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.UseOrleans(s =>
        {
            s.UseLocalhostClustering(siloPort: FreePort(), gatewayPort: 0);
            s.AddReminders();
            s.UseInMemoryReminderService();
        });
        silo = builder.Build();
        await silo.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    internal async Task<CampaignSeatCredential[]> CreateAsync(string id)
    {
        var request = new CampaignCreationRequest(1, id, Cna1979Ruleset.Manifest.Hash, LabSeed, SetupId,
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

    internal Task<CampaignOpenSeatResult> DiscoverAsync(CampaignSeatCredential seat) => Store.QueryOpenForSeatAsync(seat);
    internal Task<CampaignGrainOpeningResult> OpenAsync(CampaignSeatCredential seat, string stage, string element) =>
        Grains.GetGrain<ICampaignGrain>(seat.CampaignId).QueryOwnerOpeningAsync(stage, seat, element);
    internal Task<CampaignGrainSubmissionResult> SubmitAsync(CampaignSeatCredential seat, string stage,
        string element, long revision, string reference) => Grains.GetGrain<ICampaignGrain>(seat.CampaignId)
        .SubmitOwnerActionAsync(stage, seat, element, revision, reference);

    // Authoritative read is used exclusively for terminal verification, never to select bot actions.
    internal async Task<bool> AtBoundaryAsync(string id)
    {
        var session = await Store.ReadVerifiedAsync(id).ConfigureAwait(false);
        return CampaignExercises.QueryCheckpoint(session).PositionId == Boundary
            && CampaignExercises.Reconstruct(session).IsVerified;
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
                finally { silo.Dispose(); }
            }
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}
