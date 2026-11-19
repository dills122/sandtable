using System.Net;
using System.Net.Sockets;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Campaigns;
using Cna.OrleansHost.Timeouts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Orleans.Hosting;

namespace Cna.OrleansHost.Tests;

/// <summary>
/// Phase C3a of issue #439, task item 4: proves the new grain-based path
/// (<c>CampaignGrain</c>/<c>MartenCampaignRoundDeadlineSink</c>/<c>CampaignRoundTimeoutGrain</c>)
/// produces the identical timeout outcome <c>CampaignCombatTimeoutService.SweepOnce</c> would, for
/// a round that times out -- against a real, in-process Orleans silo (the same "real silo, no
/// mocks" precedent <c>LocalGameApiTests.cs</c> and
/// <c>Timeouts/CampaignRoundTimeoutGrainTests.cs</c> already established), for all seven round
/// kinds. Each test builds two independent, identically-scripted campaign fixtures -- one driven
/// by the old sweep, one by the new grain path -- and asserts the resulting durable state
/// (contract version and full hash-chain reconstruction) is the same either way.
/// </summary>
public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static async Task<IHost> BuildCampaignGrainHostAsync(
        CampaignDurableAuthorityJournalStore store, TimeProvider clock)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(store);
        builder.Services.AddSingleton(clock);
        builder.Services.AddSingleton<ICampaignRoundDeadlineSink, MartenCampaignRoundDeadlineSink>();
        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering(siloPort: FreeGrainTestPort(), gatewayPort: 0);
            silo.AddReminders();
            silo.UseInMemoryReminderService();
        });
        var host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);
        return host;
    }

    private static async Task StopCampaignGrainHostAsync(IHost host)
    {
        await host.StopAsync(TestContext.Current.CancellationToken);
        host.Dispose();
    }

    private static int FreeGrainTestPort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static long SweepContractVersion(JournalFixture fixture, ManualClock clock, string campaignId)
    {
        var cold = fixture.Open(clock);
        new CampaignCombatTimeoutService(CampaignDurableAuthorityJournalStore.ForSingleCampaign(cold),
            clock, NullLogger<CampaignCombatTimeoutService>.Instance).SweepOnce(CancellationToken.None);
        var session = fixture.Open(clock).ReadVerified(campaignId);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        return CampaignExercises.QueryCheckpoint(session).ContractVersion;
    }

    private static async Task<long> GrainTickContractVersionAsync(JournalFixture fixture, ManualClock clock,
        string campaignId, CampaignRoundKind roundKind)
    {
        var store = CampaignDurableAuthorityJournalStore.ForSingleCampaign(fixture.Open(clock));
        var host = await BuildCampaignGrainHostAsync(store, clock);
        try
        {
            var grain = host.Services.GetRequiredService<IGrainFactory>()
                .GetGrain<ICampaignGrain>(campaignId);
            _ = await grain.TickRoundAsync(roundKind);
        }
        finally { await StopCampaignGrainHostAsync(host); }
        var session = fixture.Open(clock).ReadVerified(campaignId);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        return CampaignExercises.QueryCheckpoint(session).ContractVersion;
    }

    [Fact]
    public async Task SelectionRoundTimeoutIsIdenticalViaGrainPathAndSweepPath()
    {
        using var fixtureA = new JournalFixture();
        var clockA = new ManualClock();
        var (requestA, _) = PrepareSelectionRound(fixtureA.Create(clockA));
        clockA.Advance(TimeSpan.FromSeconds(30));
        var sweptVersion = SweepContractVersion(fixtureA, clockA, requestA.CampaignId);

        using var fixtureB = new JournalFixture();
        var clockB = new ManualClock();
        var (requestB, _) = PrepareSelectionRound(fixtureB.Create(clockB));
        clockB.Advance(TimeSpan.FromSeconds(30));
        var grainVersion = await GrainTickContractVersionAsync(fixtureB, clockB, requestB.CampaignId,
            CampaignRoundKind.Selection);

        Assert.Equal(sweptVersion, grainVersion);
    }

    [Fact]
    public async Task RbaRoundTimeoutIsIdenticalViaGrainPathAndSweepPath()
    {
        using var fixtureA = new JournalFixture();
        var clockA = new ManualClock();
        var (requestA, _, _) = PrepareRbaRound(fixtureA.Create(clockA));
        clockA.Advance(TimeSpan.FromSeconds(30));
        var sweptVersion = SweepContractVersion(fixtureA, clockA, requestA.CampaignId);

        using var fixtureB = new JournalFixture();
        var clockB = new ManualClock();
        var (requestB, _, _) = PrepareRbaRound(fixtureB.Create(clockB));
        clockB.Advance(TimeSpan.FromSeconds(30));
        var grainVersion = await GrainTickContractVersionAsync(fixtureB, clockB, requestB.CampaignId,
            CampaignRoundKind.Rba);

        Assert.Equal(sweptVersion, grainVersion);
    }

    [Fact]
    public async Task ForceRoundTimeoutIsIdenticalViaGrainPathAndSweepPath()
    {
        using var fixtureA = new JournalFixture();
        var clockA = new ManualClock();
        var (requestA, _, _, _, _, _) = PrepareInfantryForceRound(fixtureA.Create(clockA), positive: true, seed: 8);
        clockA.Advance(TimeSpan.FromSeconds(30));
        var sweptVersion = SweepContractVersion(fixtureA, clockA, requestA.CampaignId);

        using var fixtureB = new JournalFixture();
        var clockB = new ManualClock();
        var (requestB, _, _, _, _, _) = PrepareInfantryForceRound(fixtureB.Create(clockB), positive: true, seed: 8);
        clockB.Advance(TimeSpan.FromSeconds(30));
        var grainVersion = await GrainTickContractVersionAsync(fixtureB, clockB, requestB.CampaignId,
            CampaignRoundKind.Force);

        Assert.Equal(sweptVersion, grainVersion);
    }

    [Fact]
    public async Task RetreatRoundTimeoutIsIdenticalViaGrainPathAndSweepPath()
    {
        using var fixtureA = new JournalFixture();
        var clockA = new ManualClock();
        var (requestA, _, _, _) = PrepareRetreatRound(fixtureA.Create(clockA));
        clockA.Advance(TimeSpan.FromSeconds(30));
        var sweptVersion = SweepContractVersion(fixtureA, clockA, requestA.CampaignId);

        using var fixtureB = new JournalFixture();
        var clockB = new ManualClock();
        var (requestB, _, _, _) = PrepareRetreatRound(fixtureB.Create(clockB));
        clockB.Advance(TimeSpan.FromSeconds(30));
        var grainVersion = await GrainTickContractVersionAsync(fixtureB, clockB, requestB.CampaignId,
            CampaignRoundKind.Retreat);

        Assert.Equal(sweptVersion, grainVersion);
    }

    [Fact]
    public async Task CustodyRoundTimeoutIsIdenticalViaGrainPathAndSweepPath()
    {
        using var fixtureA = new JournalFixture();
        var clockA = new ManualClock();
        var (requestA, _, _, _) = PrepareCustodyRound(fixtureA.Create(clockA));
        clockA.Advance(TimeSpan.FromSeconds(30));
        var sweptVersion = SweepContractVersion(fixtureA, clockA, requestA.CampaignId);

        using var fixtureB = new JournalFixture();
        var clockB = new ManualClock();
        var (requestB, _, _, _) = PrepareCustodyRound(fixtureB.Create(clockB));
        clockB.Advance(TimeSpan.FromSeconds(30));
        var grainVersion = await GrainTickContractVersionAsync(fixtureB, clockB, requestB.CampaignId,
            CampaignRoundKind.Custody);

        Assert.Equal(sweptVersion, grainVersion);
    }

    [Fact]
    public async Task ReserveRoundTimeoutIsIdenticalViaGrainPathAndSweepPath()
    {
        using var fixtureA = new JournalFixture();
        var clockA = new ManualClock();
        var (requestA, _) = PrepareReserveRound(fixtureA.Create(clockA), two: true);
        clockA.Advance(TimeSpan.FromSeconds(30));
        var sweptVersion = SweepContractVersion(fixtureA, clockA, requestA.CampaignId);

        using var fixtureB = new JournalFixture();
        var clockB = new ManualClock();
        var (requestB, _) = PrepareReserveRound(fixtureB.Create(clockB), two: true);
        clockB.Advance(TimeSpan.FromSeconds(30));
        var grainVersion = await GrainTickContractVersionAsync(fixtureB, clockB, requestB.CampaignId,
            CampaignRoundKind.Reserve);

        Assert.Equal(sweptVersion, grainVersion);
    }

    [Fact]
    public async Task CycleRoundTimeoutIsIdenticalViaGrainPathAndSweepPath()
    {
        using var fixtureA = new JournalFixture();
        var clockA = new ManualClock();
        var (requestA, _) = PrepareCycleRound(fixtureA.Create(clockA));
        clockA.Advance(TimeSpan.FromSeconds(30));
        var sweptVersion = SweepContractVersion(fixtureA, clockA, requestA.CampaignId);

        using var fixtureB = new JournalFixture();
        var clockB = new ManualClock();
        var (requestB, _) = PrepareCycleRound(fixtureB.Create(clockB));
        clockB.Advance(TimeSpan.FromSeconds(30));
        var grainVersion = await GrainTickContractVersionAsync(fixtureB, clockB, requestB.CampaignId,
            CampaignRoundKind.Cycle);

        Assert.Equal(sweptVersion, grainVersion);
    }

    /// <summary>
    /// Proves the full, real pipeline -- not just a direct <c>TickRoundAsync</c> call -- by
    /// actually arming a real <c>CampaignRoundTimeoutGrain</c> (as <c>CampaignGrain</c> would at
    /// round-open time) with a deadline already in the past, so its one-shot
    /// <c>RegisterGrainTimer</c> fires on the next scheduler turn with no real-time wait, and
    /// confirming it independently reaches the same contract version a direct sweep produces.
    /// </summary>
    [Fact]
    public async Task SelectionRoundArmedTimeoutGrainFiresAndTicksThroughTheRealPipeline()
    {
        using var fixtureA = new JournalFixture();
        var clockA = new ManualClock();
        var (requestA, _) = PrepareSelectionRound(fixtureA.Create(clockA));
        clockA.Advance(TimeSpan.FromSeconds(30));
        var sweptVersion = SweepContractVersion(fixtureA, clockA, requestA.CampaignId);

        using var fixtureB = new JournalFixture();
        var clockB = new ManualClock();
        var journalB = fixtureB.Create(clockB);
        var (requestB, _) = PrepareSelectionRound(journalB);
        clockB.Advance(TimeSpan.FromSeconds(30));
        var store = CampaignDurableAuthorityJournalStore.ForSingleCampaign(fixtureB.Open(clockB));
        var host = await BuildCampaignGrainHostAsync(store, clockB);
        try
        {
            var grainFactory = host.Services.GetRequiredService<IGrainFactory>();
            var sink = host.Services.GetRequiredService<ICampaignRoundDeadlineSink>();
            var deadline = await sink.GetDeadlineAsync(requestB.CampaignId, CampaignRoundKind.Selection,
                TestContext.Current.CancellationToken);
            Assert.NotNull(deadline);
            var timeoutGrain = grainFactory.GetGrain<ICampaignRoundTimeoutGrain>(
                CampaignRoundTimeoutGrainKey.For(requestB.CampaignId, CampaignRoundKind.Selection));
            // Already in the past relative to clockB (advanced 30s above) -- ArmInternalAsync
            // clamps the timer's due time to zero, so it fires on the next scheduler turn.
            await timeoutGrain.ArmAsync(deadline.Value);

            for (var attempt = 0; attempt < 100; attempt++)
            {
                if (await timeoutGrain.ArmedDeadlineForTestAsync() is null) break;
                await Task.Delay(50, TestContext.Current.CancellationToken);
            }
            Assert.Null(await timeoutGrain.ArmedDeadlineForTestAsync());
        }
        finally { await StopCampaignGrainHostAsync(host); }

        var session = fixtureB.Open(clockB).ReadVerified(requestB.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        Assert.Equal(sweptVersion, CampaignExercises.QueryCheckpoint(session).ContractVersion);
    }

    /// <summary>
    /// Proves the arm/disarm side through the real production entry point --
    /// <see cref="ICampaignGrain.SubmitOwnerActionAsync"/>, exactly as
    /// <c>CampaignDurableOutwardApi</c>'s <c>/{campaignId}/actions</c> endpoint now calls it --
    /// rather than only through direct <c>TickRoundAsync</c> calls. Arms the round-timeout grain
    /// first (mirroring what would have happened for real at round-open time via this same
    /// mechanism), then submits the owner action that completes the round before its deadline and
    /// confirms the real grain is disarmed as a consequence of that one call.
    /// </summary>
    [Fact]
    public async Task SubmitOwnerActionAsyncDisarmsTheRealTimeoutGrainOnEarlyCompletion()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var journal = fixture.Create(clock);
        var (request, owner) = PrepareSelectionRound(journal);
        var store = CampaignDurableAuthorityJournalStore.ForSingleCampaign(fixture.Open(clock));
        var host = await BuildCampaignGrainHostAsync(store, clock);
        try
        {
            var grainFactory = host.Services.GetRequiredService<IGrainFactory>();
            var timeoutGrain = grainFactory.GetGrain<ICampaignRoundTimeoutGrain>(
                CampaignRoundTimeoutGrainKey.For(request.CampaignId, CampaignRoundKind.Selection));
            await timeoutGrain.ArmAsync(DateTimeOffset.FromUnixTimeMilliseconds(
                journal.GetSelectionRoundDeadlineMs(request.CampaignId)!.Value));
            Assert.NotNull(await timeoutGrain.ArmedDeadlineForTestAsync());

            var opening = journal.QueryCombatOwnerOpening(owner, "axis-infantry");
            var campaignGrain = grainFactory.GetGrain<ICampaignGrain>(request.CampaignId);
            var result = await campaignGrain.SubmitOwnerActionAsync("combat-selection", owner,
                "axis-infantry", opening.Revision, opening.PublicActionRefs[^1]);
            Assert.True(result.IsAccepted);

            Assert.Null(await timeoutGrain.ArmedDeadlineForTestAsync());
        }
        finally { await StopCampaignGrainHostAsync(host); }
    }
}
