using System.Diagnostics;
using System.Globalization;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Tests.Persistence;

namespace Cna.OrleansHost.Tests;

public sealed partial class CampaignDurableAuthorityJournalTests
{
    private static bool CapacityBenchmarkEnabled =>
        Environment.GetEnvironmentVariable("SANDTABLE_CAPACITY_BENCHMARK") == "1";

    private static readonly string BenchmarkOut = Environment.GetEnvironmentVariable("SANDTABLE_BENCHMARK_OUT")
        ?? Path.Combine(Path.GetTempPath(), "sandtable-capacity-benchmark.jsonl");

    private static void BenchmarkRecord(string line)
    {
        lock (BenchmarkOut) File.AppendAllText(BenchmarkOut, line + "\n");
    }

    private sealed record PhaseTiming(double OpenMs, double BodyMs, double FlushMs)
    {
        internal double TotalMs => OpenMs + BodyMs + FlushMs;
    }

    /// <summary>Exactly CampaignDurableAuthorityJournalStore.RunOnceAsync's production sequence,
    /// with a stopwatch between its three phases.</summary>
    private static async Task<(T Result, PhaseTiming Timing)> TimedUnitOfWork<T>(
        CampaignJournalMartenBackend backend, string campaignId, Func<CampaignDurableAuthorityJournal, T> body)
    {
        var watch = Stopwatch.StartNew();
        using var workingSet = await backend.OpenAsync([campaignId], () => backend.RandomBytes(32),
            CancellationToken.None);
        var opened = watch.Elapsed.TotalMilliseconds;
        var journal = CampaignDurableAuthorityJournal.ForWorkingSet(workingSet.Connect, backend.AuthenticateOperator,
            backend.Clock, backend.ContentResolver, backend.RandomBytes);
        var result = body(journal);
        var bodied = watch.Elapsed.TotalMilliseconds;
        await workingSet.FlushAsync(CancellationToken.None);
        var flushed = watch.Elapsed.TotalMilliseconds;
        return (result, new PhaseTiming(opened, bodied - opened, flushed - bodied));
    }

    [Fact]
    public async Task CapacityBenchmarkHistoryGrowthExploration()
    {
        if (!CapacityBenchmarkEnabled) return;
        using var database = MartenJournalDatabase.Create(OperatorSecret);
        var backend = database.Backend;
        foreach (var (label, baseRequest) in new[] { ("truck", Request()), ("combat", PositiveCombatRequest(6)) })
        {
            var request = WithCampaignId(baseRequest, "bench-history-" + label);
            var (session, created) = await TimedUnitOfWork(backend, request.CampaignId,
                journal => journal.CreateCampaign(request, label == "combat" ? Timing(request) : null));
            BenchmarkRecord($"{{\"kind\":\"history\",\"label\":\"{label}\",\"steps\":0,\"total\":{created.TotalMs:F1}}}");
            for (var step = 1; step <= 400; step++)
            {
                var set = Enum.GetValues<CampaignActionAudience>()
                    .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                    .FirstOrDefault(value => value?.Candidates.Count > 0);
                if (set is null) break;
                var submission = new CampaignActionSubmission(1, set.CampaignId, set.StateVersion, set.PositionId,
                    set.Audience, set.Candidates[0].ActionId);
                ExerciseStepResult accepted;
                PhaseTiming timing;
                try
                {
                    (accepted, timing) = await TimedUnitOfWork(backend, request.CampaignId,
                        journal => journal.Append(request.CampaignId, submission));
                }
                catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
                {
                    BenchmarkRecord($"{{\"kind\":\"history-stop\",\"label\":\"{label}\",\"steps\":{step - 1},\"reason\":\"{exception.GetType().Name}\"}}");
                    break;
                }
                if (!accepted.IsAccepted) break;
                session = accepted.SuccessorSession!;
                var (_, read) = await TimedUnitOfWork(backend, request.CampaignId,
                    journal => journal.ReadVerified(request.CampaignId));
                BenchmarkRecord(string.Create(CultureInfo.InvariantCulture,
                    $"{{\"kind\":\"history\",\"label\":\"{label}\",\"steps\":{step},\"appendOpen\":{timing.OpenMs:F1},\"appendBody\":{timing.BodyMs:F1},\"appendFlush\":{timing.FlushMs:F1},\"readOpen\":{read.OpenMs:F1},\"readBody\":{read.BodyMs:F1},\"bytes\":{StreamBytes(backend, request.CampaignId)}}}"));
            }
        }
    }

    /// <summary>Do P independent campaigns' units of work actually run in parallel? Plain
    /// thread-pool tasks, no Orleans, so any serialization seen here is below the grain layer.</summary>
    [Fact]
    public async Task CapacityBenchmarkUnitOfWorkParallelism()
    {
        if (!CapacityBenchmarkEnabled) return;
        using var database = MartenJournalDatabase.Create(OperatorSecret);
        var store = new CampaignDurableAuthorityJournalStore(database.Backend);
        var ids = Enumerable.Range(0, 16).Select(index => $"parallel-{index:D2}").ToArray();
        await ParallelAsync(ids, 8, id => SetupSelectionCampaignAsync(store, id, withTiming: true));
        foreach (var parallel in new[] { 1, 2, 4, 8, 16 })
        {
            for (var round = 0; round < 3; round++)
            {
                var watch = Stopwatch.StartNew();
                var timings = await Task.WhenAll(ids.Take(parallel).Select(id => Task.Run(async () =>
                    (await TimedUnitOfWork(database.Backend, id, journal => journal.ReadVerified(id))).Timing)));
                var wall = watch.Elapsed.TotalMilliseconds;
                BenchmarkRecord(string.Create(CultureInfo.InvariantCulture,
                    $"{{\"kind\":\"parallel\",\"parallel\":{parallel},\"round\":{round},\"wallMs\":{wall:F0},\"perCallOpen\":{timings.Average(t => t.OpenMs):F1},\"perCallBody\":{timings.Average(t => t.BodyMs):F1},\"perCallFlush\":{timings.Average(t => t.FlushMs):F1},\"callsPerS\":{parallel / (wall / 1000):F1}}}"));
            }
        }
        // Core-only control: the same replay verification with no storage layer underneath.
        var sessions = new List<ExerciseSession>();
        foreach (var id in ids) sessions.Add(await store.ReadVerifiedAsync(id));
        foreach (var parallel in new[] { 1, 2, 4, 8, 16 })
        {
            var watch = Stopwatch.StartNew();
            await Task.WhenAll(sessions.Take(parallel).Select(session => Task.Run(() =>
                Assert.True(CampaignExercises.Reconstruct(session).IsVerified))));
            var wall = watch.Elapsed.TotalMilliseconds;
            BenchmarkRecord(string.Create(CultureInfo.InvariantCulture,
                $"{{\"kind\":\"core-only\",\"parallel\":{parallel},\"wallMs\":{wall:F0},\"callsPerS\":{parallel / (wall / 1000):F1}}}"));
        }
    }

    private static CampaignCreationRequest WithCampaignId(CampaignCreationRequest request, string campaignId) =>
        new(request.ContractVersion, campaignId, request.RulesetHash, request.Seed, request.SetupId,
            request.SetupHash, request.ContentPackId, request.ContentHash, request.ScenarioId);

    private static long StreamBytes(CampaignJournalMartenBackend backend, string campaignId)
    {
        using var workingSet = backend.OpenAsync([campaignId], () => backend.RandomBytes(32), CancellationToken.None)
            .GetAwaiter().GetResult();
        using var connection = workingSet.Connect();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COALESCE(SUM(length(canonical_command)+length(canonical_event)+length(canonical_receipt)
              +length(snapshot_checkpoint)+length(canonical_contacts)),0) FROM accepted_steps
            """;
        return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }
}
