#if CAPACITY_PROBE
using System.Diagnostics;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Campaigns;

namespace Cna.LoadHarness;

// Opt-in, sequential measurement: no parallel campaigns, no credentials or event contents in output.
internal static class CapacityProbe
{
    private sealed record Opening(CampaignSeatCredential Seat, string Stage, string Element, CampaignGrainOpeningResult Cut);
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length is < 3 or > 6 || !int.TryParse(args[0], out var size) || !CapacityProbeCatalog.Sizes.Contains(size)
            || !int.TryParse(args[1], out var limit) || limit < 0 || limit > 512)
            throw new ArgumentException("Usage: --capacity-probe 60|250|1000 <accepted-step-limit> <new-jsonl-file> [warm-samples=20] [creation-replicates=1] [full|sample]");
        var samples = args.Length >= 4 ? ParseCount(args[3], 0, 100) : 20;
        var creations = args.Length >= 5 ? ParseCount(args[4], 1, 20) : 1;
        var mode = args.Length == 6 ? args[5] : "full";
        if (mode is not ("full" or "sample")) throw new ArgumentException("Unknown capacity probe mode.");
        var samplePath = mode == "sample";
        static int ParseCount(string value, int min, int max) => int.TryParse(value, out var count)
            && count >= min && count <= max ? count : throw new ArgumentException("Invalid sample or creation count.");
        using var cancellation = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellation.Cancel();
        };
        using var output = new StreamWriter(new FileStream(args[2], FileMode.CreateNew)) { AutoFlush = true };
        using var process = Process.GetCurrentProcess();
        Console.CancelKeyPress += cancel;
        var prefix = 0;
        var stage = "";
        void Write(object row) => output.WriteLine(JsonSerializer.Serialize(row, Json));
        async Task<T> Measure<T>(string operation, Func<Task<T>> call, Func<T, object>? detail = null)
        {
            cancellation.Token.ThrowIfCancellationRequested();
            Write(new { size, prefix, stage, operation, outcome = "started", utc = DateTimeOffset.UtcNow });
            var allocated = GC.GetTotalAllocatedBytes(precise: true);
            var start = Stopwatch.GetTimestamp();
            try
            {
                var value = await call().ConfigureAwait(false);
                var elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                var allocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - allocated;
                process.Refresh();
                Write(new { size, prefix, stage, operation, elapsedMs, allocatedBytes,
                    rssBytes = process.WorkingSet64, peakRssBytes = process.PeakWorkingSet64, detail = detail?.Invoke(value), outcome = "ok" });
                return value;
            }
            catch (Exception ex)
            {
                Write(new { size, prefix, stage, operation, elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds,
                    allocatedBytes = GC.GetTotalAllocatedBytes(precise: true) - allocated, outcome = ex.GetType().Name });
                throw;
            }
        }
        try
        {
            Write(new { size, operation = "manifest", limit, samples, creations, mode,
                runtime = Environment.Version.ToString(), serverGc = System.Runtime.GCSettings.IsServerGC,
                processorCount = Environment.ProcessorCount });
            var pack = await Measure("generate", () => Task.FromResult(CapacityProbeCatalog.Get(size)),
                value => new { packBytes = value.CanonicalByteCount, hexes = value.Definition.LegacyDefinition.Locations.Count,
                    edges = value.Definition.LegacyDefinition.Edges.Count, elements = value.Definition.LegacyDefinition.Elements.Count,
                    hash = value.Identity.Hash }).ConfigureAwait(false);
            if (CapacityProbeCatalog.Generate(size).Identity != pack.Identity)
                throw new InvalidOperationException("Non-deterministic generated content.");
            var config = HarnessConfig.Parse(["--output", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                "--policy", "random-legal-full", "--store", "sqlite", "--hot-pool", "1"]);
            await using var driver = new InProcessDriver(config);
            await driver.StartAsync(cancellation.Token).ConfigureAwait(false);
            const string id = "capacity-probe";
            var request = CapacityProbeCatalog.Request(size, id, InProcessDriver.LabSeed);
            var (_, template) = InProcessDriver.Creation(config, id);
            var timing = new CampaignDecisionTimingConfigurationV1("capacity-probe.timing.v1", request.RulesetHash,
                request.ContentHash, template.Windows);
            await Measure("create-campaign", () => driver.CreateProbeCampaignAsync(request, timing)).ConfigureAwait(false);
            await Journal().ConfigureAwait(false);
            var seats = await Measure("issue-redeem-seats", () => driver.IssueSeatsAsync(id)).ConfigureAwait(false);
            for (var replica = 1; replica < creations; replica++)
            {
                var replicaId = $"capacity-probe-replica-{replica}";
                await Measure("create-campaign-warm", () => driver.CreateProbeCampaignAsync(
                    CapacityProbeCatalog.Request(size, replicaId, InProcessDriver.LabSeed), timing)).ConfigureAwait(false);
                var retained = await driver.ProbeEvidenceLengthsAsync(replicaId).ConfigureAwait(false);
                Write(new { size, prefix, operation = "replica-journal", replica, creationBytes = retained[0].EventBytes,
                    evidence = retained });
            }
            var ledger = ExerciseSeedLedger.Create(ExerciseRunIdentity.Standalone(id, config.RootSeed));
            var bot = new BotPolicy("random-legal-full", ledger, ExerciseSeedRole.System);
            // Same prefix, warm sequential discovery and verified disk replay; first sample is separate.
            await Replay().ConfigureAwait(false);
            for (var sample = 0; sample <= samples; sample++)
            {
                await Discover(seats[0], sample == 0 ? "discover-first" : "discover-warm").ConfigureAwait(false);
                if (sample != samples) await Replay().ConfigureAwait(false);
            }
            var moveSampled = false;
            var attackSampled = false;
            for (prefix = 0; prefix < limit; prefix++)
            {
                var openings = new List<Opening>();
                foreach (var seat in seats)
                {
                    var result = await Discover(seat, "discover-drive").ConfigureAwait(false);
                    foreach (var entry in result.Open.Where(entry => !entry.RuleNotImplemented))
                    foreach (var element in samplePath ? entry.ActingElementIds.Take(1) : entry.ActingElementIds)
                    {
                        stage = entry.ActionStage;
                        var cut = await Measure("open", () => driver.OpenAsync(seat, entry.OpeningStage, element, "open"),
                            value => new { visibleBytes = value.VisibleBytes.Length, refs = value.PublicActionRefs.Length }).ConfigureAwait(false);
                        if (cut.PublicActionRefs.Length > 0) openings.Add(new(seat, entry.ActionStage, element, cut));
                    }
                }
                var candidates = openings.OrderBy(o => o.Seat.Audience).ThenBy(o => o.Stage, StringComparer.Ordinal)
                    .ThenBy(o => o.Element, StringComparer.Ordinal)
                    .SelectMany(o => Enumerable.Range(0, o.Cut.PublicActionRefs.Length).Select(index => (Opening: o, Index: index))).ToList();
                var accepted = false;
                while (candidates.Count > 0)
                {
                    var pick = samplePath ? Enumerable.Range(0, candidates.Count).MinBy(index =>
                    {
                        var candidate = candidates[index];
                        using var cut = JsonDocument.Parse(candidate.Opening.Cut.VisibleBytes);
                        var kind = VisibleChoice.Read(cut.RootElement, candidate.Opening.Stage, candidate.Index,
                            candidate.Opening.Cut.PublicActionRefs.Length);
                        return (!moveSampled && kind.IsMove) || (moveSampled && kind.IsAttack) ? 0 : kind.IsFinish ? 1 : 2;
                    }) : bot.Choose(candidates.Count);
                    var (opening, index) = candidates[pick];
                    candidates.RemoveAt(pick);
                    stage = opening.Stage;
                    using var visible = JsonDocument.Parse(opening.Cut.VisibleBytes);
                    var choice = VisibleChoice.Read(visible.RootElement, opening.Stage, index, opening.Cut.PublicActionRefs.Length);
                    var result = await Measure("submit", () => driver.SubmitAsync(opening.Seat, opening.Stage,
                        opening.Element, opening.Cut.Revision, opening.Cut.PublicActionRefs[index]),
                        value => new { value.IsAccepted, value.RejectionCode, choice.Kind }).ConfigureAwait(false);
                    if (result.IsAccepted)
                    {
                        accepted = true;
                        moveSampled |= choice.IsMove;
                        attackSampled |= choice.IsAttack;
                        break;
                    }
                    if (result.RejectionCode != "rule-not-implemented") break;
                }
                if (!accepted) { Write(new { size, prefix, stage, operation = "stop", outcome = "no-accepted-action" }); break; }
                await Journal().ConfigureAwait(false);
                await Replay().ConfigureAwait(false);
                if (samplePath && moveSampled && attackSampled)
                {
                    prefix++;
                    break;
                }
            }
            var sampled = samplePath && moveSampled && attackSampled;
            Write(new { size, prefix, operation = "finish", moveSampled, attackSampled,
                outcome = sampled ? "sampled" : prefix == limit ? "step-limit" : "stopped" });
            return sampled || prefix == limit ? 0 : 1;

            Task<CampaignOpenSeatResult> Discover(CampaignSeatCredential seat, string operation) =>
                Measure(operation, () => driver.DiscoverAsync(seat), value => new
                {
                    audience = value.Audience.ToString(),
                    serializedBytes = JsonSerializer.SerializeToUtf8Bytes(value, Json).Length,
                    advertisedByStage = value.Open.Select(entry => new { entry.OpeningStage,
                        entries = entry.ActingElementIds.Count, entry.RuleNotImplemented }).ToArray(),
                    advertisedDrafts = value.Open.Sum(entry => entry.ActingElementIds.Count),
                    unsupportedDrafts = value.Open.Where(entry => entry.RuleNotImplemented).Sum(entry => entry.ActingElementIds.Count),
                });
            async Task Journal()
            {
                var rows = await driver.ProbeEvidenceLengthsAsync(id).ConfigureAwait(false);
                Write(new { size, prefix, stage, operation = "journal", rows = rows.Count, creationBytes = rows[0].EventBytes,
                    totalBytes = rows.Sum(row => row.EventBytes),
                    maxStepBytes = rows.Skip(1).Select(row => row.EventBytes).DefaultIfEmpty().Max(),
                    lastBytes = rows[^1].EventBytes, evidence = rows });
            }
            async Task Replay()
            {
                var session = await Measure("cold-open-load", () => Task.FromResult(driver.ColdLoad(id))).ConfigureAwait(false);
                var replay = await Measure("reconstruct", () => Task.FromResult(CampaignExercises.Reconstruct(session)),
                    value => new { value.IsVerified }).ConfigureAwait(false);
                if (!replay.IsVerified) throw new InvalidOperationException("Cold replay did not verify.");
            }
        }
        catch (Exception ex)
        {
            Write(new { size, prefix, stage, operation = "stop", outcome = ex.GetType().Name });
            Console.Error.WriteLine($"Capacity probe stopped at '{stage}': {ex.GetType().Name}.");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= cancel;
        }
    }
}
#endif
