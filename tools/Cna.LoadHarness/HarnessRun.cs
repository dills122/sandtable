using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Actions;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;
using Cna.OrleansHost.Api;
using Cna.OrleansHost.Campaigns;

namespace Cna.LoadHarness;

internal sealed record DecisionRow(int CampaignOrdinal, int StepOrdinal, string Audience, string Stage,
    string ActingElementId, int ChoiceCount, int ChosenIndex, long? ResultingRevision);
internal sealed record Diagnostic(int CampaignOrdinal, int StepOrdinal, string Audience, string Stage,
    string Operation, string Outcome, double ElapsedMs);
internal sealed record StateFingerprint(string ChainHeadHash, string ReconstructionHash, long EventCount,
    long CanonicalByteCount, string PositionId, int GameTurn);
internal sealed record CampaignResult(int CampaignOrdinal, string Status, int Steps, string DecisionFingerprint,
    double ElapsedMs, IReadOnlyList<Diagnostic> Diagnostics, ActionCoverage Coverage, StateFingerprint? StateFingerprint);

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
        var full = config.Policy == "random-legal-full";
        var coverageEnabled = full || config.RequireCoverage.Length > 0;
        if (full) Directory.CreateDirectory(Path.Combine(config.Output, "attempts"));
        var http = config.Driver == "http";
        var extended = http || config.Store == "postgres" || config.Until != "stage2" || config.HotPool != 16;
        var manifestData = new
        {
            contractVersion = 1,
            harnessVersion = typeof(HarnessRun).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion,
            hostVersion = config.BaseUrl is null ? typeof(CampaignSeatCredential).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion : null,
            driver = config.Driver,
            backing = config.Store,
            config.Campaigns,
            config.Policy,
            config.RootSeed,
            config.MaxStepsPerCampaign,
            scenario = config.Until == "stage2" ? InProcessDriver.SetupId : InProcessDriver.TurnBoundarySetupId,
            labSeed = InProcessDriver.LabSeed,
            decisionBudgetMs = InProcessDriver.BudgetMs,
            processorCount = Environment.ProcessorCount,
            serverGc = GCSettings.IsServerGC,
        };
        object manifest = config.RequireCoverage.Length == 0 ? manifestData
            : Extend(manifestData, new { requireCoverage = config.RequireCoverage });
        if (http)
            WriteJson(Path.Combine(config.Output, "run-manifest.json"), Extend(manifest, new
            {
                config.Until, config.HotPool, attached = config.BaseUrl is not null,
                config.RampSeconds, config.ThinkMsMin, config.ThinkMsMax, config.MaxInflight,
                config.StopSubmitP95Ms, config.StopErrorRate, config.VerifyState, checkpointEvidence = config.VerifyState ? "verified-read" : "owner-opening-only",
                decisionFingerprintFields = "v1-except-private-committed-state-version",
                metricsScope = config.BaseUrl is null ? "host-and-harness" : "harness-only",
            }));
        else if (extended)
            WriteJson(Path.Combine(config.Output, "run-manifest.json"), Extend(manifest, new { config.Until, config.HotPool }));
        else
            WriteJson(Path.Combine(config.Output, "run-manifest.json"), manifest);
        await using var driver = new InProcessDriver(config);
        await driver.StartAsync(cancellationToken).ConfigureAwait(false);
        using var runStop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var control = http ? new HttpRunControl(config, runStop) : null;
        var prefix = config.BaseUrl is null ? "" : "load-" + Guid.NewGuid().ToString("N") + "-";
        using var metrics = extended ? new RunMetrics(driver.Postgres) : null;
        using var sampleStop = new CancellationTokenSource();
        using var slots = new SemaphoreSlim(config.HotPool);
        if (metrics is not null) await metrics.SampleAsync(cancellationToken).ConfigureAwait(false);
        var timer = Stopwatch.StartNew();
        var polling = metrics?.PollAsync(sampleStop.Token) ?? Task.CompletedTask;
        CampaignResult[] results;
        try
        {
            results = await Task.WhenAll(Enumerable.Range(0, config.Campaigns).Select(ordinal =>
                Task.Run(async () =>
                {
                    if (!http) await slots.WaitAsync().ConfigureAwait(false);
                    try { return await DriveAsync(driver, config, ordinal, control, prefix, http ? runStop.Token : cancellationToken).ConfigureAwait(false); }
                    finally { if (!http) slots.Release(); }
                }, CancellationToken.None))).ConfigureAwait(false);
        }
        finally
        {
            timer.Stop();
            await sampleStop.CancelAsync().ConfigureAwait(false);
            await polling.ConfigureAwait(false);
        }
        if (metrics is not null) await metrics.SampleAsync(CancellationToken.None).ConfigureAwait(false);
        var fingerprints = results.Select(r => config.VerifyState
            ? Extend(new { r.CampaignOrdinal, r.Status, r.Steps, r.DecisionFingerprint }, new { r.StateFingerprint })
            : JsonSerializer.SerializeToNode(new { r.CampaignOrdinal, r.Status, r.Steps, r.DecisionFingerprint }, Json)!.AsObject()).ToArray();
        var fingerprintOutput = new
        {
            campaigns = fingerprints,
            aggregateDecisionFingerprint = Hash(Encoding.UTF8.GetBytes(string.Join('\n', results.Select(r => r.DecisionFingerprint)) + "\n")),
        };
        WriteJson(Path.Combine(config.Output, "fingerprints.json"), config.VerifyState
            ? Extend(fingerprintOutput, new
            {
                aggregateStateFingerprint = results.All(r => r.StateFingerprint is not null)
                    ? Hash(Encoding.UTF8.GetBytes(Lines(results.Select(r => r.StateFingerprint)))) : null,
            }) : (object)fingerprintOutput);
        var diagnostics = results.SelectMany(r => r.Diagnostics).ToArray();
        File.WriteAllText(Path.Combine(config.Output, "diagnostics.jsonl"), Lines(diagnostics));
        var completed = results.Count(r => r.Status == (config.Until == "stage2" ? "stage-2-boundary" : "turn2-reserve"));
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
                errors = g.Count(d => d.Outcome is not ("ok" or "unavailable" or "reaction-refused")),
                p50Ms = Percentile(g, 0.50),
                p95Ms = Percentile(g, 0.95),
                maxMs = g.Max(d => d.ElapsedMs),
            }, StringComparer.Ordinal),
            perCampaign = results.Select(r => config.VerifyState
                ? Extend(new { r.CampaignOrdinal, r.Status, r.Steps, r.ElapsedMs }, new { r.StateFingerprint })
                : JsonSerializer.SerializeToNode(new { r.CampaignOrdinal, r.Status, r.Steps, r.ElapsedMs }, Json)!.AsObject()),
        };
        object outputSummary = metrics is null ? summary : Extend(summary, metrics.Snapshot());
        if (control is not null)
        {
            outputSummary = Extend(outputSummary, control.Snapshot(diagnostics));
            outputSummary = Extend(outputSummary, new
            {
                checkpointEvidence = config.VerifyState ? "verified-read" : "owner-opening-only",
                metricsScope = config.BaseUrl is null ? "host-and-harness" : "harness-only",
            });
        }
        var coverage = new ActionCoverage();
        foreach (var result in results) coverage.Add(result.Coverage);
        var missing = config.RequireCoverage.Where(kind => kind == "moves" ? coverage.Moves == 0 : coverage.Attacks == 0).ToArray();
        if (coverageEnabled)
        {
            var node = JsonSerializer.SerializeToNode(outputSummary, Json)!.AsObject();
            node["coverage"] = JsonSerializer.SerializeToNode(coverage.Snapshot(), Json);
            node["missingCoverage"] = JsonSerializer.SerializeToNode(missing, Json);
            node["refusalWedgeCount"] = results.Count(r => r.Status == "wedge");
            node["discoveryWedgeCount"] = results.Count(r => r.Status == "unresolved-opening");
            node["perCampaign"] = JsonSerializer.SerializeToNode(results.Select(r =>
            {
                var campaign = new { r.CampaignOrdinal, r.Status, r.Steps, r.ElapsedMs, coverage = r.Coverage.Snapshot() };
                return config.VerifyState ? Extend(campaign, new { r.StateFingerprint })
                    : JsonSerializer.SerializeToNode(campaign, Json)!.AsObject();
            }), Json);
            outputSummary = node;
        }
        WriteJson(Path.Combine(config.Output, "summary.json"), outputSummary);
        Console.WriteLine(JsonSerializer.Serialize(outputSummary, Json));
        return completed == config.Campaigns && control?.StoppedOnCliff != true && missing.Length == 0 ? 0 : 1;
    }

    private static async Task<CampaignResult> DriveAsync(InProcessDriver driver, HarnessConfig config,
        int ordinal, HttpRunControl? control, string prefix, CancellationToken cancellationToken)
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
        using var worker = control is null ? null : new HttpDriver(config, driver.Address!, driver.OperatorSecret, control,
            (audience, stage, operation, outcome, elapsedMs) =>
                diagnostics.Add(new(ordinal, rows.Count, audience, stage, operation, outcome, elapsedMs)), prefix);
        ICampaignDriver transport = worker is null ? driver : worker;
        // Independent per-campaign random stream; pacing never consumes controller entropy.
        var think = worker is null ? null : new BotPolicy("random-legal", ledger, ExerciseSeedRole.System);
        StateFingerprint? stateFingerprint = null;
        var full = config.Policy == "random-legal-full";
        var coverageEnabled = full || config.RequireCoverage.Length > 0;
        var coverage = new ActionCoverage();
        var allActions = new BotPolicy(config.Policy, ledger, ExerciseSeedRole.System);
        var attemptsPath = Path.Combine(config.Output, "attempts", name + ".jsonl");
        var decisionPath = Path.Combine(config.Output, "campaigns", name + ".jsonl");
        if (full) { File.WriteAllText(attemptsPath, ""); File.WriteAllText(decisionPath, ""); }
        var status = "step-limit";
        var previous = "";
        var longRun = config.Until == "turn2-reserve";
        var declarations = 0;
        var endOfTurnVerified = false;
        var created = false;
        try
        {
            if (worker is not null)
            {
                if (config.Campaigns > 1 && config.RampSeconds > 0)
                    await Task.Delay(TimeSpan.FromSeconds((double)config.RampSeconds * ordinal / (config.Campaigns - 1)), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
            var seats = await transport.CreateAsync(id).ConfigureAwait(false);
            created = true;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var openings = new List<Opening>();
                var boundaryAdvertised = false;
                foreach (var seat in seats)
                {
                    var discovered = await Measure(seat, "", "discover", () => transport.DiscoverAsync(seat)).ConfigureAwait(false);
                    foreach (var entry in discovered.Open)
                    {
                        if (!longRun && entry.OpeningStage == BoundaryStage)
                        {
                            if (worker is null) boundaryAdvertised = true;
                            else foreach (var element in entry.ActingElementIds)
                            {
                                var boundary = await TryOpen(seat, entry.OpeningStage, element, "open").ConfigureAwait(false);
                                if (boundary is not null && Position(boundary, InProcessDriver.Boundary)) boundaryAdvertised = true;
                            }
                            continue;
                        }
                        if (entry.RuleNotImplemented)
                        {
                            if (longRun) throw new InvalidOperationException("Unsupported discovered opening.");
                            continue;
                        }
                        if (worker is null && longRun && declarations == 3 && entry.OpeningStage == BoundaryStage && !endOfTurnVerified)
                            endOfTurnVerified = await driver.AtBoundaryAsync(id, "land.position.end-of-turn").ConfigureAwait(false);
                        // B6 follows the synthetic checkpoint path without optional movement.
                        if (!full && longRun && entry.OpeningStage is "movement" or "second-movement" or "stage-entry-movement") continue;
                        foreach (var element in entry.ActingElementIds)
                        {
                            var cut = await TryOpen(seat, entry.OpeningStage, element, "open").ConfigureAwait(false);
                            if (cut is not null) openings.Add(new(seat, entry.ActionStage, element, cut));
                        }
                    }
                }
                // L1 can advertise and open Stage 2 prematurely. Inproc verifies replay;
                // HTTP additionally exhausts the Stage 1 discovery/fallback ladder before
                // treating an obtainable Stage 2 cut as public checkpoint evidence.
                if (worker is null && boundaryAdvertised && await driver.AtBoundaryAsync(id).ConfigureAwait(false))
                { status = "stage-2-boundary"; break; }
                if (!longRun && openings.Count == 0)
                {
                    foreach (var fallback in Fallbacks.Where(row => row.Previous == previous))
                    {
                        foreach (var seat in seats)
                        {
                            var element = fallback.Element.Replace("{seat}", Audience(seat), StringComparison.Ordinal);
                            var cut = await TryOpen(seat, fallback.Stage, element, "probe").ConfigureAwait(false);
                            if (cut is null) continue;
                            if (fallback.Stage == BoundaryStage) boundaryAdvertised |= worker is null || Position(cut, InProcessDriver.Boundary);
                            else openings.Add(new(seat, fallback.Stage, element, cut));
                        }
                        if (openings.Count > 0) break;
                    }
                    if (boundaryAdvertised && (worker is not null
                            ? openings.Count == 0
                            : await driver.AtBoundaryAsync(id).ConfigureAwait(false)))
                    { status = "stage-2-boundary"; break; }
                }
                if (openings.Count == 0) { status = "unresolved-opening"; break; }
                if (coverageEnabled)
                    foreach (var opening in openings)
                    {
                        using var visible = JsonDocument.Parse(opening.Cut.VisibleBytes);
                        if (visible.RootElement.TryGetProperty("positionId", out var position))
                            coverage.Positions.Add($"{(declarations >= 4 || declarations == 3 && opening.Stage == BoundaryStage ? 2 : 1)}:{position.GetString()}");
                    }
                // B6 uses discovery only and selects the ordinal-last element of the one open stage.
                var selected = longRun ? openings.OrderBy(o => o.Element, StringComparer.Ordinal).Last() : openings[^1];
                if (longRun)
                {
                    if (!full && openings.Select(o => (o.Seat.Audience, o.Stage)).Distinct().Count() != 1)
                    { status = "ambiguous-opening"; break; }
                    if (declarations == 4 && selected.Stage == "stage-entry-reserve")
                    {
                        status = (worker is not null || endOfTurnVerified) && (worker is not null
                            ? Position(selected.Cut, "land.position.operation-1.first-player.reserve-designation")
                            : await driver.AtBoundaryAsync(id, "land.position.operation-1.first-player.reserve-designation").ConfigureAwait(false))
                            ? "turn2-reserve" : "boundary-not-verified";
                        break;
                    }
                    if (selected.Stage == BoundaryStage && declarations > 0)
                    {
                        var position = declarations switch
                        {
                            1 => "land.position.operation-2.initiative-declaration",
                            2 => "land.position.operation-3.initiative-declaration",
                            3 => "land.position.operation-1.initiative-declaration",
                            _ => "",
                        };
                        if (worker is not null ? !Position(selected.Cut, position) : !await driver.AtBoundaryAsync(id, position).ConfigureAwait(false))
                        { status = "boundary-not-verified"; break; }
                    }
                }
                if (rows.Count == config.MaxStepsPerCampaign) break;
                if (full)
                {
                    // Sample without replacement across the whole discovered owner-action pool.
                    // Completion offers remain candidates alongside optional actions.
                    var candidates = openings.OrderBy(o => o.Seat.Audience).ThenBy(o => o.Stage, StringComparer.Ordinal)
                        .ThenBy(o => o.Element, StringComparer.Ordinal)
                        .SelectMany(o => Enumerable.Range(0, o.Cut.PublicActionRefs.Length).Select(index => (Opening: o, Index: index)))
                        .ToList();
                    var accepted = false;
                    while (candidates.Count > 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var pick = allActions.Choose(candidates.Count);
                        var (opening, choiceIndex) = candidates[pick];
                        candidates.RemoveAt(pick);
                        using var visible = JsonDocument.Parse(opening.Cut.VisibleBytes);
                        var choice = VisibleChoice.Read(visible.RootElement, opening.Stage, choiceIndex, opening.Cut.PublicActionRefs.Length);
                        File.AppendAllText(attemptsPath, Lines(new[] { new
                        {
                            campaignOrdinal = ordinal, stepOrdinal = rows.Count, audience = Audience(opening.Seat),
                            stage = opening.Stage, actingElementId = opening.Element, choiceCount = opening.Cut.PublicActionRefs.Length,
                            chosenIndex = choiceIndex, kind = choice.Kind, moveCp = choice.MoveCp,
                            positionId = visible.RootElement.TryGetProperty("positionId", out var position) ? position.GetString() : null,
                        } }));
                        var submission = await Submit(opening, choiceIndex).ConfigureAwait(false);
                        File.AppendAllText(attemptsPath, Lines(new[] { new
                        {
                            outcome = submission.IsAccepted ? "ok" : submission.RejectionCode == "rule-not-implemented"
                                ? "reaction-refused" : submission.RejectionCode ?? "rejected",
                        } }));
                        if (!submission.IsAccepted && submission.RejectionCode == "rule-not-implemented")
                        { coverage.ReactionRefused++; continue; }
                        if (!submission.IsAccepted) { status = "submission-rejected"; break; }
                        Accept(opening, choiceIndex, submission, choice);
                        File.AppendAllText(decisionPath, Lines(new[] { rows[^1] }));
                        accepted = true;
                        break;
                    }
                    if (!accepted)
                    {
                        if (status != "submission-rejected") status = "wedge";
                        break;
                    }
                    continue;
                }
                // L1's fixed opening-selection order, independent of the index-selection policy.
                int index;
                if (longRun && selected.Stage is "reserve-designation" or "second-reserve" or "stage-entry-reserve")
                {
                    // The B6 lab exercises reserve completion, not individual designations.
                    // Filter using the owner's visible choices, never hidden authoritative state.
                    using var visible = JsonDocument.Parse(selected.Cut.VisibleBytes);
                    var eligible = visible.RootElement.GetProperty("choices").EnumerateArray()
                        .Select((choice, offset) => (choice, offset))
                        .Where(row => row.choice.GetProperty("kind").GetString() == "complete")
                        .Select(row => row.offset).ToArray();
                    if (eligible.Length != 1) { status = "unresolved-reserve-completion"; break; }
                    index = eligible[bots[selected.Seat.Audience].Choose(eligible.Length)];
                }
                else index = bots[selected.Seat.Audience].Choose(selected.Cut.PublicActionRefs.Length);
                var result = await Submit(selected, index).ConfigureAwait(false);
                if (!result.IsAccepted) { status = "submission-rejected"; break; }
                VisibleChoice? acceptedChoice = null;
                if (coverageEnabled)
                {
                    using var visible = JsonDocument.Parse(selected.Cut.VisibleBytes);
                    acceptedChoice = VisibleChoice.Read(visible.RootElement, selected.Stage, index, selected.Cut.PublicActionRefs.Length);
                }
                Accept(selected, index, result, acceptedChoice);
            }
            if (config.VerifyState)
            {
                stateFingerprint = await worker!.VerifyAsync(id).ConfigureAwait(false);
                if (stateFingerprint is null) status = "verification-failed";
                else if (status == "turn2-reserve" && (stateFingerprint.GameTurn != 2
                    || stateFingerprint.PositionId != "land.position.operation-1.first-player.reserve-designation"))
                    status = "boundary-not-verified";
                else if (status == "stage-2-boundary" && stateFingerprint.PositionId != InProcessDriver.Boundary)
                    status = "boundary-not-verified";
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { status = "cancelled"; }
        catch (Exception exception)
        {
            // Never include credentials or opaque action references in artifacts.
            status = "error:" + exception.GetType().Name;
            Console.Error.WriteLine($"Campaign {ordinal}: {status} after {rows.Count} accepted owner submissions.");
        }
        if (full && config.BaseUrl is null && created)
        {
            try
            {
                if (!await driver.VerifyReplayAsync(id, coverage).ConfigureAwait(false)) status = "replay-mismatch";
            }
            catch (Exception exception)
            {
                if (!status.StartsWith("error:", StringComparison.Ordinal)) status = "replay-error:" + exception.GetType().Name;
            }
        }
        var lines = Lines(rows);
        File.WriteAllText(decisionPath, lines);
        return new(ordinal, status, rows.Count, Hash(Encoding.UTF8.GetBytes(lines)), timer.Elapsed.TotalMilliseconds, diagnostics, coverage, stateFingerprint);

        async Task<Submission> Submit(Opening opening, int index)
        {
            if (worker is not null && config.ThinkMsMax > 0)
                await Task.Delay(config.ThinkMsMin + think!.Choose(config.ThinkMsMax - config.ThinkMsMin + 1), cancellationToken).ConfigureAwait(false);
            return await Measure(opening.Seat, opening.Stage, "submit", () => transport.SubmitAsync(
                opening.Seat, opening.Stage, opening.Element, opening.Cut.Revision,
                opening.Cut.PublicActionRefs[index]), value => value.IsAccepted ? "ok"
                    : full && value.RejectionCode == "rule-not-implemented" ? "reaction-refused"
                    : value.RejectionCode ?? "rejected").ConfigureAwait(false);
        }

        void Accept(Opening opening, int index, Submission result, VisibleChoice? choice)
        {
            rows.Add(new(ordinal, rows.Count, Audience(opening.Seat), opening.Stage, opening.Element,
                opening.Cut.PublicActionRefs.Length, index, result.CommittedStateVersion));
            if (choice is not null) coverage.Accept(opening.Stage, choice);
            if (opening.Stage is "initiative-order" or BoundaryStage) declarations++;
            previous = opening.Stage;
        }


        async Task<CampaignGrainOpeningResult?> TryOpen(CampaignSeatCredential seat, string stage, string element, string operation)
        {
            try
            {
                var cut = await Measure(seat, stage, operation, () => transport.OpenAsync(seat, stage, element, operation),
                    value => value.PublicActionRefs.Length > 0 ? "ok" : "empty").ConfigureAwait(false);
                if (full && longRun && cut.PublicActionRefs.Length == 0)
                    throw new InvalidOperationException("Advertised owner opening has no references.");
                return cut.PublicActionRefs.Length > 0 ? cut : null;
            }
            catch (InvalidOperationException) when (worker is null && (!full || !longRun)) { return null; }
            catch (OpeningUnavailableException) { return null; }
        }

        async Task<T> Measure<T>(CampaignSeatCredential seat, string stage, string operation, Func<Task<T>> call,
            Func<T, string>? outcome = null)
        {
            if (worker is not null) return await call().ConfigureAwait(false);
            var watch = Stopwatch.StartNew();
            var code = "error";
            try
            {
                var value = await call().ConfigureAwait(false);
                code = outcome?.Invoke(value) ?? "ok";
                return value;
            }
            catch (InvalidOperationException) when ((!full || !longRun) && operation is "open" or "probe") { code = "unavailable"; throw; }
            finally { diagnostics.Add(new(ordinal, rows.Count, Audience(seat), stage, operation, code, watch.Elapsed.TotalMilliseconds)); }
        }
    }

    private static bool Position(CampaignGrainOpeningResult cut, string expected)
    {
        using var visible = JsonDocument.Parse(cut.VisibleBytes);
        return visible.RootElement.TryGetProperty("positionId", out var position) && position.GetString() == expected;
    }

    private static JsonObject Extend(object original, object additional)
    {
        var result = JsonSerializer.SerializeToNode(original, Json)!.AsObject();
        foreach (var property in JsonSerializer.SerializeToNode(additional, Json)!.AsObject())
            result.Add(property.Key, property.Value?.DeepClone());
        return result;
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
