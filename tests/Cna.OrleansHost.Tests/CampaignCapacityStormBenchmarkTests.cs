using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Campaigns;
using Cna.OrleansHost.Persistence;
using Cna.OrleansHost.Tests.Persistence;
using Cna.OrleansHost.Timeouts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Cna.OrleansHost.Tests;

/// <summary>A wall clock that can be jumped forward mid-run (never backward).</summary>
internal sealed class BenchmarkOffsetClock : TimeProvider
{
    private long offsetTicks;
    public override DateTimeOffset GetUtcNow() => TimeProvider.System.GetUtcNow().AddTicks(Interlocked.Read(ref offsetTicks));
    internal void Jump(TimeSpan by) => Interlocked.Add(ref offsetTicks, by.Ticks);
}

/// <summary>Wraps the production sink; records when each effective deadline tick started and
/// when its journal commit (store unit of work + timer re-derivation) returned.</summary>
internal sealed class TimingDeadlineSink(ICampaignRoundDeadlineSink inner, TimeProvider clock) : ICampaignRoundDeadlineSink
{
    internal ConcurrentDictionary<string, (DateTimeOffset Start, DateTimeOffset End)> Ticks { get; } = new();

    public Task<DateTimeOffset?> GetDeadlineAsync(string campaignId, CampaignRoundKind roundKind,
        CancellationToken cancellationToken) => inner.GetDeadlineAsync(campaignId, roundKind, cancellationToken);

    public async Task<bool> TickAsync(string campaignId, CampaignRoundKind roundKind, CancellationToken cancellationToken)
    {
        var start = clock.GetUtcNow();
        var ticked = await inner.TickAsync(campaignId, roundKind, cancellationToken);
        if (ticked) Ticks.TryAdd(campaignId + "/" + roundKind, (start, clock.GetUtcNow()));
        return ticked;
    }
}

public sealed partial class CampaignDurableAuthorityJournalTests
{
    /// <summary>
    /// Phase D capacity benchmark (docs/work/plans/2026-11-20-phase-d-capacity-latency-benchmark.md):
    /// K campaigns colocated in one silo, each with an open Selection round, against real Postgres
    /// (Marten hot store + AdoNet reminders), driven through the production grain path.
    /// </summary>
    [Fact]
    public async Task CapacityBenchmarkDeadlineStorm()
    {
        if (!CapacityBenchmarkEnabled) return;
        var counts = (Environment.GetEnvironmentVariable("SANDTABLE_BENCHMARK_CAMPAIGNS") ?? "16,32,64,128")
            .Split(',').Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToArray();
        var modes = (Environment.GetEnvironmentVariable("SANDTABLE_BENCHMARK_MODES") ?? "burst,staggered").Split(',');
        foreach (var mode in modes)
            foreach (var count in counts)
                await RunDeadlineStormAsync(count, mode);
    }

    private static async Task<string> SetupSelectionCampaignAsync(CampaignDurableAuthorityJournalStore store,
        string campaignId, bool withTiming, int budgetMs = 30_000)
    {
        var request = WithCampaignId(PositiveCombatRequest(6), campaignId);
        // Owner-load campaigns get a one-hour budget so the clock jump never expires them; the
        // staggered sweep widens the timed budget so setup always finishes before any deadline.
        var timing = withTiming && budgetMs == 30_000 ? Timing(request) : new CampaignDecisionTimingConfigurationV1(
            "rules-lab.campaign-timing.1", request.RulesetHash, request.ContentHash,
            [.. Timing(request).Windows.Select(window => window with
            {
                DecisionBudgetMilliseconds = withTiming ? budgetMs : 3_600_000,
            })]);
        var session = await store.CreateCampaignAsync(request, timing);
        for (var index = 0; index < 80; index++)
        {
            try
            {
                _ = CampaignHostAdapter.QueryContactCombatOwnerDraft(session, CampaignActionAudience.Axis, "axis-infantry");
                return campaignId;
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException) { }
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .First(value => value?.Candidates.Count > 0)!;
            var next = set.Candidates.FirstOrDefault(value => value is not MoveElementAction)
                ?? set.Candidates.FirstOrDefault(value => value is MoveElementAction move
                    && move.ElementId is not ("axis-infantry" or "commonwealth-infantry"))
                ?? set.Candidates[0];
            var accepted = await store.AppendAsync(campaignId, new CampaignActionSubmission(1, set.CampaignId,
                set.StateVersion, set.PositionId, set.Audience, next.ActionId));
            Assert.True(accepted.IsAccepted);
            session = accepted.SuccessorSession!;
        }
        throw new InvalidOperationException("Combat position was not reached.");
    }

    private static async Task ParallelAsync<T>(IEnumerable<T> items, int degree, Func<T, Task> body)
    {
        using var gate = new SemaphoreSlim(degree);
        await Task.WhenAll(items.Select(async item =>
        {
            await gate.WaitAsync();
            try { await body(item); } finally { gate.Release(); }
        }));
    }

    private static string Percentiles(IReadOnlyCollection<double> values)
    {
        if (values.Count == 0) return "{\"n\":0}";
        var sorted = values.Order().ToArray();
        double At(double q) => sorted[Math.Min(sorted.Length - 1, (int)Math.Ceiling(q * sorted.Length) - 1)];
        return string.Create(CultureInfo.InvariantCulture,
            $"{{\"n\":{sorted.Length},\"p50\":{At(0.50):F0},\"p95\":{At(0.95):F0},\"p99\":{At(0.99):F0},\"max\":{sorted[^1]:F0}}}");
    }

    private static async Task RunDeadlineStormAsync(int count, string mode)
    {
        const int OwnerCampaigns = 4;
        var clock = new BenchmarkOffsetClock();
        // The test container runs Postgres's default max_connections=100, while Npgsql's default
        // pool ceiling is also 100 per connection string -- so an uncapped hot pool plus the
        // reminder pool can exceed the server limit ("53300: too many clients") once enough units
        // of work are in flight. Cap both pools below the server limit, as a deployment must.
        var hotPool = int.Parse(Environment.GetEnvironmentVariable("SANDTABLE_BENCHMARK_HOT_POOL") ?? "60",
            CultureInfo.InvariantCulture);
        Npgsql.NpgsqlConnection.ClearAllPools();
        var hot = new Npgsql.NpgsqlConnectionStringBuilder(await SharedPostgres.CreateDatabaseAsync())
        { MaxPoolSize = hotPool }.ConnectionString;
        var orleansDb = new Npgsql.NpgsqlConnectionStringBuilder(await SharedPostgres.CreateDatabaseAsync())
        { MaxPoolSize = 20 }.ConnectionString;
        await OrleansAdoNetSchema.EnsureSchemaAsync(orleansDb, CancellationToken.None);
        using var documentStore = SharedPostgres.CreateCampaignStore(hot);
        var backend = await CampaignJournalMartenBackend.CreateAsync(documentStore, OperatorSecret, clock);
        var store = new CampaignDurableAuthorityJournalStore(backend);

        var setupWatch = Stopwatch.StartNew();
        var timed = Enumerable.Range(0, count).Select(index => $"storm-{count}-{index:D4}").ToArray();
        var owners = Enumerable.Range(0, OwnerCampaigns).Select(index => $"owner-{count}-{index}").ToArray();
        await ParallelAsync(timed, 8, id => SetupSelectionCampaignAsync(store, id, withTiming: true,
            budgetMs: mode == "staggered" ? 300_000 : 30_000));
        await ParallelAsync(owners, 4, id => SetupSelectionCampaignAsync(store, id, withTiming: false));
        var credentials = new Dictionary<string, CampaignSeatCredential>(StringComparer.Ordinal);
        foreach (var owner in owners)
            credentials[owner] = (await store.RedeemSeatGrantAsync(
                await store.IssueSeatGrantAsync(owner, CampaignActionAudience.Axis, OperatorSecret)))!;
        var deadlines = new Dictionary<string, DateTimeOffset>(StringComparer.Ordinal);
        foreach (var id in timed)
        {
            var (kind, ms) = Assert.Single(store.DrainPendingRoundTimerActions(id));
            Assert.Equal(CampaignRoundKind.Selection, kind);
            deadlines[id] = DateTimeOffset.FromUnixTimeMilliseconds(ms!.Value);
        }
        foreach (var owner in owners) _ = store.DrainPendingRoundTimerActions(owner);
        var setupMs = setupWatch.Elapsed.TotalMilliseconds;

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Services.AddSingleton(store);
        builder.Services.AddSingleton<TimeProvider>(clock);
        builder.Services.AddSingleton(provider => new TimingDeadlineSink(
            new MartenCampaignRoundDeadlineSink(store, provider.GetRequiredService<IGrainFactory>()), clock));
        builder.Services.AddSingleton<ICampaignRoundDeadlineSink>(provider => provider.GetRequiredService<TimingDeadlineSink>());
        builder.UseOrleans(silo =>
        {
            silo.UseLocalhostClustering(siloPort: FreeGrainTestPort(), gatewayPort: 0);
            silo.AddReminders();
            silo.UseAdoNetReminderService(options =>
            {
                options.Invariant = "Npgsql";
                options.ConnectionString = orleansDb;
            });
        });
        using var host = builder.Build();
        await host.StartAsync();
        try
        {
            var grains = host.Services.GetRequiredService<IGrainFactory>();
            var sink = host.Services.GetRequiredService<TimingDeadlineSink>();

            var ownerLatencies = new ConcurrentBag<(double AtMs, double LatencyMs)>();
            using var ownerStop = new CancellationTokenSource();
            var runWatch = Stopwatch.StartNew();
            var ownerLoops = owners.Select(owner => Task.Run(async () =>
            {
                var grain = grains.GetGrain<ICampaignGrain>(owner);
                while (!ownerStop.IsCancellationRequested)
                {
                    var started = runWatch.Elapsed.TotalMilliseconds;
                    _ = await grain.QueryOwnerOpeningAsync("combat-selection", credentials[owner], "axis-infantry");
                    ownerLatencies.Add((started, runWatch.Elapsed.TotalMilliseconds - started));
                    await Task.Delay(100);
                }
            })).ToArray();
            await Task.Delay(3000);
            var baselineEndMs = runWatch.Elapsed.TotalMilliseconds;

            // burst: every deadline already passed when armed (worst case: a silo restart or a
            // synchronized wave of round opens). staggered: deadlines keep the real spread their
            // rounds opened with; the earliest lands 2s after arming.
            var now = clock.GetUtcNow();
            if (mode == "burst") clock.Jump(deadlines.Values.Max() - now + TimeSpan.FromMilliseconds(1));
            else clock.Jump(deadlines.Values.Min() - now - TimeSpan.FromSeconds(2));
            var armedAt = clock.GetUtcNow();
            var armWatch = Stopwatch.StartNew();
            var armLatencies = new ConcurrentBag<double>();
            await Task.WhenAll(timed.Select(async id =>
            {
                var started = armWatch.Elapsed.TotalMilliseconds;
                await grains.GetGrain<ICampaignRoundTimeoutGrain>(CampaignRoundTimeoutGrainKey.For(id, CampaignRoundKind.Selection))
                    .ArmAsync(deadlines[id]);
                armLatencies.Add(armWatch.Elapsed.TotalMilliseconds - started);
            }));
            var stormStartMs = baselineEndMs;
            var limit = Stopwatch.StartNew();
            var peakConnections = 0L;
            await using (var probe = new Npgsql.NpgsqlConnection(
                new Npgsql.NpgsqlConnectionStringBuilder(orleansDb) { Pooling = false }.ConnectionString))
            {
                await probe.OpenAsync();
                while (timed.Any(id => !sink.Ticks.ContainsKey(id + "/" + CampaignRoundKind.Selection))
                    && limit.Elapsed < TimeSpan.FromMinutes(10))
                {
                    await using var sample = probe.CreateCommand();
                    sample.CommandText = "SELECT count(*) FROM pg_stat_activity WHERE backend_type='client backend'";
                    peakConnections = Math.Max(peakConnections, (long)(await sample.ExecuteScalarAsync())!);
                    await Task.Delay(50);
                }
            }
            var stormEndMs = runWatch.Elapsed.TotalMilliseconds;
            ownerStop.Cancel();
            await Task.WhenAll(ownerLoops);

            var lateness = new List<double>();
            var tickDurations = new List<double>();
            var queueDelays = new List<double>();
            foreach (var id in timed)
            {
                var (start, end) = sink.Ticks[id + "/" + CampaignRoundKind.Selection];
                var due = mode == "burst" ? armedAt : deadlines[id];
                lateness.Add((end - due).TotalMilliseconds);
                tickDurations.Add((end - start).TotalMilliseconds);
                queueDelays.Add((start - due).TotalMilliseconds);
            }
            var commits = timed.Select(id => sink.Ticks[id + "/" + CampaignRoundKind.Selection].End).ToArray();
            var spanMs = (commits.Max() - (mode == "burst" ? armedAt : deadlines.Values.Min())).TotalMilliseconds;

            // Invariants: every campaign timed out exactly once, replay-verified (REL-001/EVT-001).
            var verified = 0;
            await ParallelAsync(timed, 8, async id =>
            {
                var session = await store.ReadVerifiedAsync(id);
                Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
                Assert.Equal(27, CampaignExercises.QueryCheckpoint(session).ContractVersion);
                Interlocked.Increment(ref verified);
            });
            Assert.Equal(count, verified);

            var baseline = ownerLatencies.Where(value => value.AtMs < baselineEndMs).Select(value => value.LatencyMs).ToArray();
            var during = ownerLatencies.Where(value => value.AtMs >= stormStartMs && value.AtMs <= stormEndMs)
                .Select(value => value.LatencyMs).ToArray();
            BenchmarkRecord(string.Create(CultureInfo.InvariantCulture,
                $"{{\"kind\":\"storm\",\"mode\":\"{mode}\",\"campaigns\":{count},\"setupMs\":{setupMs:F0},\"spanMs\":{spanMs:F0},\"throughputPerS\":{count / (spanMs / 1000):F1},\"arm\":{Percentiles(armLatencies.ToArray())},\"lateness\":{Percentiles(lateness)},\"queueDelay\":{Percentiles(queueDelays)},\"tick\":{Percentiles(tickDurations)},\"ownerBaseline\":{Percentiles(baseline)},\"ownerDuring\":{Percentiles(during)},\"hotPool\":{hotPool},\"peakPgConnections\":{peakConnections},\"verified\":{verified}}}"));
        }
        finally
        {
            await host.StopAsync();
        }
    }
}
