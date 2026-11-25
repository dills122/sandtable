using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Campaigns;

namespace Cna.LoadHarness;

internal sealed record DecisionRow(int CampaignOrdinal, int StepOrdinal, string Audience, string Stage,
    string ActingElementId, int ChoiceCount, int ChosenIndex, long ResultingRevision);
internal sealed record Diagnostic(int CampaignOrdinal, int StepOrdinal, string Audience, string Stage,
    string Operation, string Outcome, double ElapsedMs);
internal sealed record CampaignResult(int CampaignOrdinal, string Status, int Steps, string DecisionFingerprint,
    double ElapsedMs, IReadOnlyList<Diagnostic> Diagnostics);

internal static class HarnessRun
{
    private const string BoundaryStage = "stage-entry-initiative-declaration";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly (string Previous, string Stage, string Element)[] Fallbacks =
    [
        ("truck-convoy-pass", "post-combat-pass", "land.post-combat-pass"),
        ("post-combat-pass", "post-combat-pass", "land.post-combat-pass"),
        ("post-combat-pass", "second-reserve", "land.second-reserve"),
        ("post-combat-pass", BoundaryStage, "land.stage-entry-initiative-declaration"),
        ("second-reserve", "second-reserve", "land.second-reserve"),
        ("second-reserve", "second-movement-completion", "land.second-movement-completion"),
        ("second-movement", "second-movement-completion", "land.second-movement-completion"),
        ("second-movement-completion", "combat-selection", "{seat}-infantry"),
        ("combat-selection", "cycle-control", "land.cycle-control"),
        ("cycle-control", "truck-convoy-pass", "land.truck-convoy-pass"),
    ];

    private sealed record Opening(CampaignSeatCredential Seat, string Stage, string Element, CampaignGrainOpeningResult Cut);

    internal static async Task<int> RunAsync(HarnessConfig config, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(config.Output);
        Directory.CreateDirectory(Path.Combine(config.Output, "campaigns"));
        Directory.CreateDirectory(Path.Combine(config.Output, "seed-ledgers"));
        WriteJson(Path.Combine(config.Output, "run-manifest.json"), new
        {
            contractVersion = 1,
            harnessVersion = typeof(HarnessRun).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,
            hostVersion = typeof(CampaignSeatCredential).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,
            driver = "inproc",
            backing = "sqlite",
            config.Campaigns,
            config.Policy,
            config.RootSeed,
            config.MaxStepsPerCampaign,
            scenario = InProcessDriver.SetupId,
            labSeed = InProcessDriver.LabSeed,
            decisionBudgetMs = InProcessDriver.BudgetMs,
            processorCount = Environment.ProcessorCount,
            serverGc = GCSettings.IsServerGC,
        });
        await using var driver = new InProcessDriver();
        await driver.StartAsync(cancellationToken).ConfigureAwait(false);
        var timer = Stopwatch.StartNew();
        var results = await Task.WhenAll(Enumerable.Range(0, config.Campaigns).Select(ordinal =>
            Task.Run(() => DriveAsync(driver, config, ordinal, cancellationToken), CancellationToken.None))).ConfigureAwait(false);
        timer.Stop();
        var fingerprints = results.Select(r => new { r.CampaignOrdinal, r.Status, r.Steps, r.DecisionFingerprint }).ToArray();
        WriteJson(Path.Combine(config.Output, "fingerprints.json"), new
        {
            campaigns = fingerprints,
            aggregateDecisionFingerprint = Hash(Encoding.UTF8.GetBytes(string.Join('\n', results.Select(r => r.DecisionFingerprint)) + "\n")),
        });
        var diagnostics = results.SelectMany(r => r.Diagnostics).ToArray();
        File.WriteAllText(Path.Combine(config.Output, "diagnostics.jsonl"), Lines(diagnostics));
        var completed = results.Count(r => r.Status == "stage-2-boundary");
        var steps = results.Sum(r => r.Steps);
        var summary = new
        {
            config.Campaigns,
            completed,
            failed = config.Campaigns - completed,
            steps,
            elapsedSeconds = timer.Elapsed.TotalSeconds,
            stepsPerSecond = steps / timer.Elapsed.TotalSeconds,
            serverGc = GCSettings.IsServerGC,
            outcomes = results.GroupBy(r => r.Status).ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal),
            calls = diagnostics.GroupBy(d => d.Operation).ToDictionary(g => g.Key, g => new
            {
                count = g.Count(),
                conflicts = g.Count(d => d.Outcome == "unavailable"),
                errors = g.Count(d => d.Outcome != "ok" && d.Outcome != "unavailable"),
                p50Ms = Percentile(g, 0.50),
                p95Ms = Percentile(g, 0.95),
                maxMs = g.Max(d => d.ElapsedMs),
            }, StringComparer.Ordinal),
            perCampaign = results.Select(r => new { r.CampaignOrdinal, r.Status, r.Steps, r.ElapsedMs }),
        };
        WriteJson(Path.Combine(config.Output, "summary.json"), summary);
        Console.WriteLine(JsonSerializer.Serialize(summary, Json));
        return completed == config.Campaigns ? 0 : 1;
    }

    private static async Task<CampaignResult> DriveAsync(InProcessDriver driver, HarnessConfig config,
        int ordinal, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var rows = new List<DecisionRow>();
        var diagnostics = new List<Diagnostic>();
        var id = "load-campaign-" + ordinal.ToString(CultureInfo.InvariantCulture);
        var name = ordinal.ToString(CultureInfo.InvariantCulture);
        var ledger = ExerciseSeedLedger.Create(ExerciseRunIdentity.Standalone(id, config.RootSeed));
        File.WriteAllBytes(Path.Combine(config.Output, "seed-ledgers", name + ".json"), SeedLedgerCodec.Serialize(ledger));
        var bots = new Dictionary<CampaignActionAudience, BotPolicy>
        {
            [CampaignActionAudience.Axis] = new(config.Policy, ledger, ExerciseSeedRole.Axis),
            [CampaignActionAudience.Commonwealth] = new(config.Policy, ledger, ExerciseSeedRole.Commonwealth),
        };
        var status = "step-limit";
        var previous = "";
        try
        {
            var seats = await driver.CreateAsync(id).ConfigureAwait(false);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var openings = new List<Opening>();
                var boundaryAdvertised = false;
                foreach (var seat in seats)
                {
                    var discovered = await Measure(seat, "", "discover", () => driver.DiscoverAsync(seat)).ConfigureAwait(false);
                    foreach (var entry in discovered.Open)
                    {
                        if (entry.OpeningStage == BoundaryStage) { boundaryAdvertised = true; continue; }
                        if (entry.RuleNotImplemented) continue;
                        foreach (var element in entry.ActingElementIds)
                        {
                            var cut = await TryOpen(seat, entry.OpeningStage, element, "open").ConfigureAwait(false);
                            if (cut is not null) openings.Add(new(seat, entry.ActionStage, element, cut));
                        }
                    }
                }
                // A Stage 2 opening alone is not proof: L1 observed it before Stage 1 finished.
                if (boundaryAdvertised && await driver.AtBoundaryAsync(id).ConfigureAwait(false))
                { status = "stage-2-boundary"; break; }
                if (openings.Count == 0)
                {
                    foreach (var fallback in Fallbacks.Where(row => row.Previous == previous))
                    {
                        foreach (var seat in seats)
                        {
                            var element = fallback.Element.Replace("{seat}", Audience(seat), StringComparison.Ordinal);
                            var cut = await TryOpen(seat, fallback.Stage, element, "probe").ConfigureAwait(false);
                            if (cut is null) continue;
                            if (fallback.Stage == BoundaryStage) boundaryAdvertised = true;
                            else openings.Add(new(seat, fallback.Stage, element, cut));
                        }
                        if (openings.Count > 0) break;
                    }
                    if (boundaryAdvertised && await driver.AtBoundaryAsync(id).ConfigureAwait(false))
                    { status = "stage-2-boundary"; break; }
                }
                if (openings.Count == 0) { status = "unresolved-opening"; break; }
                if (rows.Count == config.MaxStepsPerCampaign) break;
                // L1's fixed opening-selection order, independent of the index-selection policy.
                var selected = openings[^1];
                var index = bots[selected.Seat.Audience].Choose(selected.Cut.PublicActionRefs.Length);
                var result = await Measure(selected.Seat, selected.Stage, "submit", () => driver.SubmitAsync(
                    selected.Seat, selected.Stage, selected.Element, selected.Cut.Revision,
                    selected.Cut.PublicActionRefs[index]), value => value.IsAccepted ? "ok" : value.RejectionCode ?? "rejected").ConfigureAwait(false);
                if (!result.IsAccepted) { status = "submission-rejected"; break; }
                var receipt = CampaignActionAcceptanceReceiptSerializer.DeserializeCanonical(result.CanonicalReceipt!);
                rows.Add(new(ordinal, rows.Count, Audience(selected.Seat), selected.Stage, selected.Element,
                    selected.Cut.PublicActionRefs.Length, index, receipt.CommittedStateVersion));
                previous = selected.Stage;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { status = "cancelled"; }
        catch (Exception exception)
        {
            // Never include credentials or opaque action references in artifacts.
            status = "error:" + exception.GetType().Name;
            Console.Error.WriteLine($"Campaign {ordinal}: {status} after {rows.Count} accepted owner submissions.");
        }
        var lines = Lines(rows);
        File.WriteAllText(Path.Combine(config.Output, "campaigns", name + ".jsonl"), lines);
        return new(ordinal, status, rows.Count, Hash(Encoding.UTF8.GetBytes(lines)), timer.Elapsed.TotalMilliseconds, diagnostics);

        async Task<CampaignGrainOpeningResult?> TryOpen(CampaignSeatCredential seat, string stage, string element, string operation)
        {
            try
            {
                var cut = await Measure(seat, stage, operation, () => driver.OpenAsync(seat, stage, element),
                    value => value.PublicActionRefs.Length > 0 ? "ok" : "empty").ConfigureAwait(false);
                return cut.PublicActionRefs.Length > 0 ? cut : null;
            }
            catch (InvalidOperationException) { return null; }
        }

        async Task<T> Measure<T>(CampaignSeatCredential seat, string stage, string operation, Func<Task<T>> call,
            Func<T, string>? outcome = null)
        {
            var watch = Stopwatch.StartNew();
            var code = "error";
            try
            {
                var value = await call().ConfigureAwait(false);
                code = outcome?.Invoke(value) ?? "ok";
                return value;
            }
            catch (InvalidOperationException) when (operation is "open" or "probe") { code = "unavailable"; throw; }
            finally { diagnostics.Add(new(ordinal, rows.Count, Audience(seat), stage, operation, code, watch.Elapsed.TotalMilliseconds)); }
        }
    }

    private static double Percentile(IEnumerable<Diagnostic> values, double fraction)
    {
        var sorted = values.Select(d => d.ElapsedMs).Order().ToArray();
        return sorted[(int)Math.Ceiling(sorted.Length * fraction) - 1];
    }
    private static string Audience(CampaignSeatCredential seat) => seat.Audience.ToString().ToLowerInvariant();
    private static string Lines<T>(IEnumerable<T> rows) => string.Concat(rows.Select(row => JsonSerializer.Serialize(row, Json) + "\n"));
    private static string Hash(byte[] bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static void WriteJson<T>(string path, T value) => File.WriteAllText(path, JsonSerializer.Serialize(value, Json) + "\n");
}
