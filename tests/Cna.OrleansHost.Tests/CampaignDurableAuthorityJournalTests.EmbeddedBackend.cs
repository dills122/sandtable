using Cna.Core.Exercises;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Embedded;
using Cna.OrleansHost.Timeouts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;

namespace Cna.OrleansHost.Tests;

/// <summary>
/// Embedded-mode spike (D7, design: docs/work/plans/2026-11-25-embedded-local-journal-design.md).
/// The embedded backend is the SQLite file store, so the existing journal suite already covers its
/// storage semantics; these tests cover only what embedded mode adds: re-arming durable deadlines
/// after a restart, where Orleans' in-memory reminders have lost every registration.
/// </summary>
public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static async Task<IHost> BuildEmbeddedHostAsync(CampaignDurableAuthorityJournalStore store,
        TimeProvider clock, bool withRearm)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(store);
        builder.Services.AddSingleton(clock);
        if (withRearm) builder.Services.AddEmbeddedDeadlineRearm();
        else builder.Services.AddSingleton<ICampaignRoundDeadlineSink, MartenCampaignRoundDeadlineSink>();
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

    private static long ContractVersionOf(JournalFixture fixture, ManualClock clock, string campaignId) =>
        CampaignExercises.QueryCheckpoint(fixture.Open(clock).ReadVerified(campaignId)).ContractVersion;

    [Fact]
    public async Task EmbeddedRestartWithoutRearmLeavesAPassedDeadlineUnfired()
    {
        // Negative control: documents the gap EmbeddedDeadlineRearm closes. In-memory reminders are
        // gone after a restart, so with only the sink registered nothing activates the timeout grain.
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var (request, _) = PrepareSelectionRound(fixture.Create(clock));
        var before = ContractVersionOf(fixture, clock, request.CampaignId);
        clock.Advance(TimeSpan.FromSeconds(30));
        var host = await BuildEmbeddedHostAsync(
            CampaignDurableAuthorityJournalStore.ForSingleCampaign(fixture.Open(clock)), clock, withRearm: false);
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
            Assert.Equal(before, ContractVersionOf(fixture, clock, request.CampaignId));
        }
        finally { await StopCampaignGrainHostAsync(host); }
    }

    [Fact]
    public async Task EmbeddedBootRearmFiresADeadlinePassedWhileTheProcessWasDown()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var (request, _) = PrepareSelectionRound(fixture.Create(clock));
        var before = ContractVersionOf(fixture, clock, request.CampaignId);
        clock.Advance(TimeSpan.FromSeconds(30));
        var host = await BuildEmbeddedHostAsync(
            CampaignDurableAuthorityJournalStore.ForSingleCampaign(fixture.Open(clock)), clock, withRearm: true);
        try
        {
            var deadline = TimeProvider.System.GetUtcNow().AddSeconds(15);
            while (ContractVersionOf(fixture, clock, request.CampaignId) == before
                && TimeProvider.System.GetUtcNow() < deadline)
                await Task.Delay(50, TestContext.Current.CancellationToken);
        }
        finally { await StopCampaignGrainHostAsync(host); }
        var session = fixture.Open(clock).ReadVerified(request.CampaignId);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(session).ContractVersion);
    }

    [Fact]
    public async Task EmbeddedBootRearmArmsOnlyCollectingRoundsWithTheirDurableDeadline()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var (request, _) = PrepareSelectionRound(fixture.Create(clock));
        var store = CampaignDurableAuthorityJournalStore.ForSingleCampaign(fixture.Open(clock));
        var expected = store.GetRoundDeadlineMs(request.CampaignId, CampaignRoundKind.Selection);
        Assert.NotNull(expected);
        var host = await BuildEmbeddedHostAsync(store, clock, withRearm: true);
        try
        {
            var grain = host.Services.GetRequiredService<IGrainFactory>().GetGrain<ICampaignRoundTimeoutGrain>(
                CampaignRoundTimeoutGrainKey.For(request.CampaignId, CampaignRoundKind.Selection));
            Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(expected!.Value), await grain.ArmedDeadlineForTestAsync());
            // Only the one collecting Selection round exists, so a second explicit pass arms exactly one.
            Assert.Equal(1, await host.Services.GetRequiredService<EmbeddedDeadlineRearm>()
                .RearmAsync(TestContext.Current.CancellationToken));
        }
        finally { await StopCampaignGrainHostAsync(host); }
    }

    [Fact]
    public void EmbeddedEnumerationSurvivesACorruptJournalFileInTheDirectory()
    {
        using var fixture = new JournalFixture();
        var clock = new ManualClock();
        var (request, _) = PrepareSelectionRound(fixture.Create(clock));
        var directory = System.IO.Path.GetDirectoryName(fixture.Path)!;
        // Sorts before campaign.sqlite; not a SQLite database at all.
        File.WriteAllText(System.IO.Path.Combine(directory, "0000-corrupt.sqlite"), "not a sqlite database");
        File.WriteAllText(System.IO.Path.Combine(directory, "1111-corrupt.sqlite"), "also not sqlite");
        var store = new CampaignDurableAuthorityJournalStore(directory, OperatorSecret, clock);
        Assert.Contains(request.CampaignId, store.CollectingSelectionCampaigns("", 16));
    }
}
