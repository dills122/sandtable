using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Cna.OrleansHost.Timeouts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans;
using Orleans.Hosting;
using Orleans.Runtime;

namespace Cna.OrleansHost.Tests.Timeouts;

// Proves Phase C4's timer+reminder hybrid (design doc §4.2,
// docs/work/plans/2026-11-18-postgres-migration-design.md) against a real, in-process Orleans
// silo -- the same "real silo, no mocks" precedent LocalGameApiTests.cs already established for
// grain-backed testing in this repo (UseLocalhostClustering + a free port), extended here with
// an in-memory reminder service (Orleans' own dev/test-only provider -- see
// SiloBuilderReminderMemoryExtensions.UseInMemoryReminderService's own remarks) since this is the
// first grain in this codebase to use reminders at all.
public sealed class CampaignRoundTimeoutGrainTests
{
    [Fact]
    public async Task ArmedDeadlineFiresPreciselyViaTheGrainTimer()
    {
        var sink = new FakeDeadlineSink();
        var host = await BuildHostAsync(sink);
        try
        {
            var grain = GetGrain(host, "selection-fires", CampaignRoundKind.Selection);
            var deadline = DateTimeOffset.UtcNow.AddMilliseconds(250);
            sink.SetDeadline("selection-fires", CampaignRoundKind.Selection, deadline);

            await grain.ArmAsync(deadline);
            var (campaignId, roundKind) = await sink.Ticked.Task.WaitAsync(
                TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

            Assert.Equal("selection-fires", campaignId);
            Assert.Equal(CampaignRoundKind.Selection, roundKind);
        }
        finally { await StopAsync(host); }
    }

    [Fact]
    public async Task ReminderBackstopRearmsTheTimerAfterASimulatedDeactivation()
    {
        var sink = new FakeDeadlineSink();
        var host = await BuildHostAsync(sink);
        try
        {
            var grain = GetGrain(host, "force-crash-recovery", CampaignRoundKind.Force);
            var deadline = DateTimeOffset.UtcNow.AddSeconds(1);
            sink.SetDeadline("force-crash-recovery", CampaignRoundKind.Force, deadline);
            await grain.ArmAsync(deadline);

            var priorActivation = await grain.ActivationIdForTestAsync();
            await grain.DeactivateForTestAsync();

            // Simulate Orleans' reminder subsystem delivering the coarse backstop tick to a
            // deactivated grain -- exactly what re-activates it in production, since nothing
            // else in this phase calls this grain (see the fork doc: no production call site
            // arms it yet). Calling ReceiveReminder directly, rather than waiting out Orleans'
            // real minutes-scale reminder schedule, keeps this deterministic and fast; the
            // schedule itself (RegisterOrUpdateReminder's period/dueTime) is exercised by
            // ArmAsync above without error, which is what proves a real, valid reminder was
            // registered.
            await grain.ReceiveReminder(CampaignRoundTimeoutGrain.ReminderName, default,
                TestContext.Current.CancellationToken);

            Assert.NotEqual(priorActivation, await grain.ActivationIdForTestAsync());
            Assert.Equal(deadline, await grain.ArmedDeadlineForTestAsync());

            var (campaignId, roundKind) = await sink.Ticked.Task.WaitAsync(
                TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            Assert.Equal("force-crash-recovery", campaignId);
            Assert.Equal(CampaignRoundKind.Force, roundKind);
        }
        finally { await StopAsync(host); }
    }

    [Fact]
    public async Task DisarmClearsTheTimerSoNoLateTickArrives()
    {
        var sink = new FakeDeadlineSink();
        var host = await BuildHostAsync(sink);
        try
        {
            var grain = GetGrain(host, "custody-completes-first", CampaignRoundKind.Custody);
            var deadline = DateTimeOffset.UtcNow.AddMilliseconds(300);
            sink.SetDeadline("custody-completes-first", CampaignRoundKind.Custody, deadline);
            await grain.ArmAsync(deadline);

            // The round completed normally before its deadline: the caller clears the durable
            // deadline and disarms the grain in the same breath.
            sink.SetDeadline("custody-completes-first", CampaignRoundKind.Custody, null);
            await grain.DisarmAsync();

            Assert.Null(await grain.ArmedDeadlineForTestAsync());
            await Task.Delay(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken);
            Assert.False(sink.Ticked.Task.IsCompleted);
        }
        finally { await StopAsync(host); }
    }

    private static ICampaignRoundTimeoutGrain GetGrain(IHost host, string campaignId, CampaignRoundKind roundKind) =>
        host.Services.GetRequiredService<IGrainFactory>()
            .GetGrain<ICampaignRoundTimeoutGrain>(CampaignRoundTimeoutGrainKey.For(campaignId, roundKind));

    private static async Task<IHost> BuildHostAsync(ICampaignRoundDeadlineSink sink)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(sink);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering(siloPort: FreePort(), gatewayPort: 0);
            silo.AddReminders();
            silo.UseInMemoryReminderService();
        });
        var host = builder.Build();
        await host.StartAsync(TestContext.Current.CancellationToken);
        return host;
    }

    private static async Task StopAsync(IHost host)
    {
        await host.StopAsync(TestContext.Current.CancellationToken);
        host.Dispose();
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private sealed class FakeDeadlineSink : ICampaignRoundDeadlineSink
    {
        private readonly ConcurrentDictionary<string, DateTimeOffset?> deadlines = new(StringComparer.Ordinal);

        public TaskCompletionSource<(string CampaignId, CampaignRoundKind RoundKind)> Ticked { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void SetDeadline(string campaignId, CampaignRoundKind roundKind, DateTimeOffset? deadline) =>
            deadlines[Key(campaignId, roundKind)] = deadline;

        public Task<DateTimeOffset?> GetDeadlineAsync(string campaignId, CampaignRoundKind roundKind,
            CancellationToken cancellationToken) => Task.FromResult(
                deadlines.TryGetValue(Key(campaignId, roundKind), out var deadline) ? deadline : null);

        public Task<bool> TickAsync(string campaignId, CampaignRoundKind roundKind,
            CancellationToken cancellationToken)
        {
            Ticked.TrySetResult((campaignId, roundKind));
            return Task.FromResult(true);
        }

        private static string Key(string campaignId, CampaignRoundKind roundKind) =>
            $"{campaignId}|{roundKind}";
    }
}
