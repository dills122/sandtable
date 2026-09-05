using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class BreakdownFixtureMigrationTests
{
    public static IEnumerable<object[]> MigrationFiles()
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(
            RepositoryPath("docs/specs/breakdown-fixture-migration.v1.json")));
        return document.RootElement.GetProperty("scenarioFiles").EnumerateArray()
            .Select(value => new object[]
            {
                value.GetProperty("path").GetString()!,
                value.GetProperty("sha256").GetString()!,
                value.GetProperty("successorPath").GetString()!,
            }).ToArray();
    }

    [Theory]
    [MemberData(nameof(MigrationFiles))]
    public void HistoricalBytesRemainFrozenAndNamedSuccessorAdmitsCurrentIdentity(
        string historicalPath, string historicalHash, string successorPath)
    {
        Assert.Equal(historicalHash, Convert.ToHexStringLower(
            SHA256.HashData(File.ReadAllBytes(RepositoryPath(historicalPath)))));
        Assert.True(File.Exists(RepositoryPath(successorPath)), $"Required successor is missing: {successorPath}");
        foreach (var (manifest, identity) in ReadCases(successorPath))
        {
            Assert.Equal(Cna1979Ruleset.Manifest.Hash, manifest.RulesetHash);
            var result = ExerciseExecutor.Execute(manifest, identity, TestContext.Current.CancellationToken);
            Assert.True(result.IsSucceeded,
                $"{manifest.ExerciseId}: {System.Text.Encoding.UTF8.GetString(ExerciseRunResultCodec.Serialize(result.RunResult))}");
            Assert.Equal(manifest.TerminalBoundary, result.BoundaryPositionId);
            Assert.True(result.Reconstruction!.IsVerified);
            Assert.True(ReadjudicationVerifier.Verify(manifest, result).IsVerified);
            AssertMatrixEvidence(manifest, result);
            AssertReactionEvidence(manifest, result);
        }
    }

    [Fact]
    public void ReactionSuccessorContainsExactlyThirteenNamedChildrenAndNoDeferredZocClaims()
    {
        using var inventory = JsonDocument.Parse(File.ReadAllBytes(
            RepositoryPath("docs/specs/breakdown-fixture-migration.v1.json")));
        var expected = inventory.RootElement.GetProperty("reactionChildren").EnumerateArray()
            .Where(value => value.GetProperty("capabilityDisposition").GetString() == "successor")
            .Select(value => value.GetProperty("successorExerciseId").GetString()).ToArray();
        var actual = ReadExercises("scenarios/maneuvers/rules-lab.reaction.serial.breakdown.v1.json");
        Assert.Equal(13, actual.Count);
        Assert.Equal(expected, actual.Select(value => value.ExerciseId));
        Assert.DoesNotContain(actual, value => value.ExerciseId.Contains("positive-zoc", StringComparison.Ordinal)
            || value.ExerciseId.Contains("remote-zoc", StringComparison.Ordinal));
    }

    [Fact]
    public void LastCpTriggerKeepsPhasingStopDeferredUntilReactionCloses()
    {
        var (manifest, identity) = ReadCases("scenarios/maneuvers/rules-lab.breakdown-truck.serial.v1.json")
            .Single(value => value.Manifest.ExerciseId == "reaction.last-cp");
        var result = ExerciseExecutor.Execute(manifest, identity, TestContext.Current.CancellationToken);
        Assert.True(result.IsSucceeded, $"{manifest.ExerciseId}: {result.FailureCategory}");
        Assert.True(result.Reconstruction!.IsVerified);
        Assert.True(ReadjudicationVerifier.Verify(manifest, result).IsVerified);
        var events = result.Steps.SelectMany(value => value.EventRecords).Select(CloneEvent).ToArray();
        var trigger = Assert.Single(events, value => EventType(value) == "element-moved");
        Assert.Equal("reacting", trigger.GetProperty("breakdownFlowAfter").GetProperty("kind").GetString());
        Assert.Equal("resolve-stop", trigger.GetProperty("breakdownFlowAfter")
            .GetProperty("phasingContinuation").GetProperty("kind").GetString());
        var closure = Array.FindIndex(events, value => EventType(value) == "reaction-window-closed");
        Assert.True(closure > 0);
        Assert.Equal("phasing-stop", events[closure].GetProperty("breakdownFlowAfter")
            .GetProperty("kind").GetString());
        Assert.Equal("breakdown-stop-resolved", EventType(events[closure + 1]));
        Assert.Equal("cp-exhausted", events[closure + 1].GetProperty("stop").GetProperty("reason").GetString());
        Assert.DoesNotContain(events.Take(closure), value => EventType(value) == "breakdown-stop-resolved"
            && value.GetProperty("stop").GetProperty("route").GetProperty("owner").GetString() == "axis");
    }

    [Fact]
    public void TruckCostPairRetainsIdenticalInitialAuthorityAndExactPublishedCpDifference()
    {
        var cases = ReadCases("scenarios/maneuvers/rules-lab.movement-cost.paired.breakdown.v1.json");
        Assert.Equal(2, cases.Count);
        Assert.Equal(cases[0].Identity, cases[1].Identity);
        var results = cases.Select(value => ExerciseExecutor.Execute(value.Manifest, value.Identity,
            TestContext.Current.CancellationToken)).ToArray();
        Assert.All(results, value => Assert.True(value.IsSucceeded));
        Assert.Equal(results[0].InitialSnapshot, results[1].InitialSnapshot);
        var moves = results.Select(value => Assert.Single(value.Steps.SelectMany(step => step.EventRecords)
            .Select(CloneEvent), record => EventType(record) == "element-moved")).ToArray();
        Assert.All(moves, value => Assert.Equal("axis-truck", value.GetProperty("elementId").GetString()));
        Assert.Equal(new CapabilityPointAmount(8, 1), Cost(moves[0]));
        Assert.Equal(new CapabilityPointAmount(1, 2), Cost(moves[1]));
        Assert.All(moves, value => Assert.Equal(value.GetProperty("cost").GetProperty("totalCost").GetRawText(),
            value.GetProperty("capabilityPointsExpendedAfter").GetRawText()));

        static CapabilityPointAmount Cost(JsonElement movement)
        {
            var value = movement.GetProperty("cost").GetProperty("totalCost");
            return new(value.GetProperty("numerator").GetInt64(), value.GetProperty("denominator").GetInt32());
        }
    }

    internal static IReadOnlyList<ExerciseManifest> ReadExercises(string path) =>
        ReadCases(path).Select(value => value.Manifest).ToArray();

    internal static IReadOnlyList<(ExerciseManifest Manifest, ExerciseRunIdentity Identity)> ReadCases(string path)
    {
        var bytes = File.ReadAllBytes(RepositoryPath(path));
        using var document = JsonDocument.Parse(bytes);
        if (!document.RootElement.TryGetProperty("mode", out var mode))
        {
            var exercise = ExerciseManifestCodec.Deserialize(bytes);
            return [(exercise, ExerciseRunIdentity.Standalone(exercise.ExerciseId, exercise.RootSeed))];
        }
        if (mode.GetString() == "serial-paired")
        {
            var paired = PairedManeuverManifestCodec.Deserialize(bytes);
            return paired.Pairs.SelectMany(pair => new[] { pair.Baseline, pair.Candidate }
                .Select(value => (value.Materialize(paired.RootSeed), new ExerciseRunIdentity(
                    paired.RootSeed, paired.ManeuverId, pair.Repetition, pair.PairKey)))).ToArray();
        }
        var manifest = ManeuverManifestCodec.Deserialize(bytes);
        return manifest.Exercises.Select((value, ordinal) => (value.Materialize(manifest.RootSeed),
            new ExerciseRunIdentity(manifest.RootSeed, manifest.ManeuverId, ordinal, null))).ToArray();
    }

    private static void AssertMatrixEvidence(ExerciseManifest manifest, ExerciseExecutionResult result)
    {
        if (!manifest.ExerciseId.StartsWith("movement-entry.", StringComparison.Ordinal)
            && !manifest.ExerciseId.StartsWith("movement-execution.", StringComparison.Ordinal)) return;
        var reserves = manifest.ExerciseId.Contains("reserve-none", StringComparison.Ordinal) ? 0
            : manifest.ExerciseId.Contains("reserve-one", StringComparison.Ordinal) ? 1 : 2;
        var movement = manifest.ExerciseId.StartsWith("movement-execution.", StringComparison.Ordinal);
        var moves = movement ? 2 - reserves : 0;
        var events = result.Steps.SelectMany(value => value.EventRecords).Select(CloneEvent).ToArray();
        Assert.Equal(reserves, events.Count(value => EventType(value) == "reserve-element-designated"));
        Assert.Equal(moves, events.Count(value => EventType(value) == "element-moved"));
        Assert.Equal(moves, events.Count(value => EventType(value) == "element-movement-stopped"));
        var resolutions = events.Where(value => EventType(value) == "breakdown-stop-resolved").ToArray();
        Assert.Equal(moves, resolutions.Length);
        Assert.All(resolutions, value =>
        {
            Assert.Empty(value.GetProperty("checks").EnumerateArray());
            Assert.Equal(value.GetProperty("randomStateBefore").GetRawText(),
                value.GetProperty("randomStateAfter").GetRawText());
        });
        Assert.Equal(10 + reserves + (movement ? 3 * moves + 1 : 0), result.Steps.Count);
        using var snapshot = JsonDocument.Parse(result.FinalSnapshot);
        var firstSide = snapshot.RootElement.GetProperty("operationStageOrders")[0]
            .GetProperty("firstSide").GetString();
        Assert.Equal(manifest.ExerciseId.Contains("act-first", StringComparison.Ordinal)
            ? "axis" : "commonwealth", firstSide);
    }

    private static JsonElement CloneEvent(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        return document.RootElement.Clone();
    }

    private static string EventType(JsonElement value) => value.GetProperty("eventType").GetString()!;

    private static void AssertReactionEvidence(ExerciseManifest manifest, ExerciseExecutionResult result)
    {
        if (!manifest.ExerciseId.StartsWith("reaction.", StringComparison.Ordinal)) return;
        var id = manifest.ExerciseId.Replace(".breakdown-v1", "", StringComparison.Ordinal);
        var (ordinaryMoves, reactorMoves, completions, closures, resolutions, steps, reason) = id switch
        {
            "reaction.adjacent.all-by-action-id" or "reaction.adjacent.all-by-descending-action-id"
                or "reaction.low-defense.all-by-action-id" => (1, 2, 2, 1, 3, 21, "no-eligible-reactor"),
            "reaction.adjacent.two-steps" => (1, 4, 2, 1, 3, 23, "no-eligible-reactor"),
            "reaction.adjacent.decline" => (1, 0, 0, 1, 1, 15, "player-decline"),
            "reaction.adjacent.one-then-decline" => (1, 1, 1, 1, 2, 18, "player-decline"),
            "reaction.adjacent.unavailable" => (1, 0, 0, 1, 1, 15, "scripted-unavailable"),
            "reaction.adjacent.timeout" => (1, 0, 0, 1, 1, 15, "timeout"),
            "reaction.adjacent.active-unavailable" => (1, 1, 0, 1, 2, 17, "scripted-unavailable"),
            "reaction.adjacent.active-timeout" => (1, 1, 0, 1, 2, 17, "timeout"),
            "reaction.headquarters.all-by-action-id" => (1, 0, 0, 1, 1, 15, "no-eligible-reactor"),
            "reaction.noncombat.all-by-action-id" => (1, 0, 0, 0, 1, 14, ""),
            "reaction.recurrence.all-by-action-id" => (2, 4, 4, 2, 6, 31, "no-eligible-reactor"),
            _ => throw new InvalidOperationException($"Missing exact Reaction expectation: {id}"),
        };
        var events = result.Steps.SelectMany(value => value.EventRecords).Select(CloneEvent).ToArray();
        Assert.Equal(ordinaryMoves, events.Count(value => EventType(value) == "element-moved"));
        Assert.Equal(reactorMoves, events.Count(value => EventType(value) == "reacting-element-moved"));
        Assert.Equal(completions, events.Count(value => EventType(value) == "reaction-participant-completed"));
        var closed = events.Where(value => EventType(value) == "reaction-window-closed").ToArray();
        Assert.Equal(closures, closed.Length);
        Assert.All(closed, value => Assert.Equal(reason, value.GetProperty("reason").GetString()));
        var resolved = events.Where(value => EventType(value) == "breakdown-stop-resolved").ToArray();
        Assert.Equal(resolutions, resolved.Length);
        Assert.Equal(steps, result.Steps.Count);
        Assert.All(resolved, value =>
        {
            Assert.Empty(value.GetProperty("checks").EnumerateArray());
            Assert.Empty(value.GetProperty("createdLots").EnumerateArray());
            Assert.Equal(value.GetProperty("randomStateBefore").GetRawText(), value.GetProperty("randomStateAfter").GetRawText());
        });
        for (var index = 0; index < events.Length; index++)
        {
            if (EventType(events[index]) != "reaction-participant-completed"
                && !(EventType(events[index]) == "reaction-window-closed"
                    && id.Contains(".active-", StringComparison.Ordinal))) continue;
            var flow = events[index].GetProperty("breakdownFlowAfter");
            Assert.Equal(EventType(events[index]) == "reaction-participant-completed"
                ? "reactor-stop-open" : "reactor-stop-closed", flow.GetProperty("kind").GetString());
            var next = events[index + 1];
            Assert.Equal("breakdown-stop-resolved", EventType(next));
            Assert.Equal(flow.GetProperty("stop").GetRawText(), next.GetProperty("stop").GetRawText());
        }
        if (id.Contains("recurrence", StringComparison.Ordinal))
        {
            var episodes = events.Where(value => EventType(value) == "reacting-element-moved")
                .GroupBy(value => value.GetProperty("elementId").GetString()).ToArray();
            Assert.Equal(2, episodes.Length);
            Assert.All(episodes, episode =>
            {
                var moves = episode.ToArray();
                Assert.Equal(2, moves.Length);
                Assert.Equal(moves[0].GetProperty("capabilityPointsExpendedAfter").GetRawText(),
                    moves[1].GetProperty("capabilityPointsExpendedBefore").GetRawText());
                Assert.NotEqual(moves[0].GetProperty("submittedOpportunityId").GetString(),
                    moves[1].GetProperty("submittedOpportunityId").GetString());
            });
        }
    }

    private static string RepositoryPath(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Sandtable.slnx")))
            directory = directory.Parent;
        return Path.Combine(directory?.FullName
            ?? throw new InvalidOperationException("Repository root was not found."), relativePath);
    }
}
