using System.Text.Json;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Commands;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class ReactionManeuverTests : IDisposable
{
    private static readonly string[] AdjacentElements = ["commonwealth-element-a", "commonwealth-element-b"];
    private static readonly string[] DescendingOrder =
        ["commonwealth-element-a", "commonwealth-element-b", "commonwealth-element-b", "commonwealth-element-a"];
    private readonly string root = Path.Combine(Path.GetTempPath(), $"sandtable-reaction-{Guid.NewGuid():N}");

    [Fact]
    public void CheckedMatrixCompletesEveryChildWithStrictReplayAndReadjudicationEvidence()
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var exit = ManeuverRunCommand.Execute(
            ["maneuver", "run", "--manifest", "scenarios/maneuvers/rules-lab.reaction.serial.v2.json",
                "--artifact-root", root], output, error, TestContext.Current.CancellationToken);
        Assert.True(exit == ManeuverProcessExitCode.Succeeded, $"{exit}: {error}\n{output}");
        var lines = output.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var report = ManeuverReportReader.Read(lines.Single(value => value.StartsWith("report=", StringComparison.Ordinal))[7..]);
        Assert.Equal(ManeuverReportStatus.Succeeded, report.Report.Deterministic.Status);
        var bundles = lines.Where(value => value.StartsWith("exerciseBundle[", StringComparison.Ordinal))
            .Select(value => ExerciseBundleReader.Read(value[(value.IndexOf('=', StringComparison.Ordinal) + 1)..])).ToArray();
        Assert.Equal(15, bundles.Length);
        foreach (var bundle in bundles)
        {
            Assert.True(bundle.ReconstructionProof!.IsVerified);
            Assert.True(bundle.ReadjudicationProof!.IsVerified);
            using var final = JsonDocument.Parse(bundle.FinalSnapshotBytes!);
            Assert.Equal(JsonValueKind.Null, final.RootElement.GetProperty("reactionWindow").ValueKind);
            Assert.Equal("land.segment.breakdown-determination", final.RootElement.GetProperty("currentPosition")
                .GetProperty("sequencePosition").GetProperty("segmentId").GetString());
            Assert.Equal("movement-segment-completed", EventKind(bundle.CanonicalEvents[^1]));
            AssertReactionEvidence(bundle, final.RootElement);
        }
    }

    private static void AssertReactionEvidence(ExerciseBundle bundle, JsonElement final)
    {
        var documents = bundle.CanonicalEvents.Select(value => JsonDocument.Parse(value)).ToArray();
        try
        {
            var events = documents.Select(value => value.RootElement).ToArray();
            var moves = events.Where(value => Kind(value) == "reacting-element-moved").ToArray();
            var triggers = events.Where(value => Kind(value) == "element-moved"
                && value.GetProperty("openedReactionWindow").ValueKind != JsonValueKind.Null).ToArray();
            var closes = events.Where(value => Kind(value) == "reaction-window-closed").ToArray();
            var id = bundle.NormalizedManifest!.ExerciseId;
            var expected = id switch
            {
                "reaction.adjacent.all-by-action-id" => (21, 3, 2),
                "reaction.adjacent.all-by-descending-action-id" => (23, 4, 2),
                "reaction.adjacent.two-steps" => (27, 8, 2),
                "reaction.adjacent.one-then-decline" => (19, 2, 2),
                "reaction.adjacent.active-unavailable" or "reaction.adjacent.active-timeout" => (17, 2, 2),
                "reaction.positive-zoc.all-by-action-id" or "reaction.low-defense.all-by-action-id" => (23, 4, 2),
                "reaction.remote-zoc.all-by-action-id" or "reaction.recurrence.all-by-action-id" => (21, 3, 2),
                "reaction.noncombat.all-by-action-id" => (13, 0, 0),
                _ => (15, 0, 2),
            };
            Assert.Equal(expected.Item1, events.Length);
            Assert.Equal(expected.Item2, moves.Length);
            Assert.Equal(expected.Item3, triggers.Length);
            Assert.Equal(triggers.Length, closes.Length);
            foreach (var trigger in triggers)
            {
                var opportunities = trigger.GetProperty("openedReactionWindow").GetProperty("frozenOpportunities");
                Assert.All(opportunities.EnumerateArray(), opportunity => Assert.All(
                    opportunity.GetProperty("reactingRepresentation").GetProperty("boundElementIds").EnumerateArray(),
                    element => Assert.Contains(element.GetString(), AdjacentElements)));
            }
            if (triggers.Length > 0)
                Assert.Equal(id.Contains("headquarters", StringComparison.Ordinal) ? 0 : 2,
                    triggers[0].GetProperty("openedReactionWindow").GetProperty("frozenOpportunities").GetArrayLength());

            if (id.Contains("headquarters", StringComparison.Ordinal))
            {
                for (var index = 0; index < triggers.Length; index++)
                {
                    Assert.Equal(triggers[index].GetProperty("stateVersion").GetInt64() + 1,
                        closes[index].GetProperty("stateVersion").GetInt64());
                    Assert.Equal("no-eligible-reactor", closes[index].GetProperty("reason").GetString());
                }
            }
            if (id.Contains("active-", StringComparison.Ordinal))
            {
                Assert.DoesNotContain(events, value => Kind(value) == "reaction-participant-completed");
                Assert.All(closes, close =>
                {
                    var prior = events.Single(value => value.GetProperty("stateVersion").GetInt64()
                        == close.GetProperty("priorStateVersion").GetInt64());
                    Assert.Equal("reacting-element-moved", Kind(prior));
                    Assert.Equal(2, close.GetProperty("closedOpportunityIds").GetArrayLength());
                });
            }
            if (id.Contains("unavailable", StringComparison.Ordinal) || id.Contains("timeout", StringComparison.Ordinal))
                Assert.All(closes, close => Assert.Equal(id.Contains("timeout", StringComparison.Ordinal)
                    ? "timeout" : "scripted-unavailable", close.GetProperty("reason").GetString()));
            if (id == "reaction.adjacent.decline")
                Assert.All(closes, close => Assert.Equal("player-decline", close.GetProperty("reason").GetString()));
            if (id == "reaction.adjacent.one-then-decline")
                Assert.Equal(1, closes.First(value => value.GetProperty("reason").GetString() == "player-decline")
                    .GetProperty("closedOpportunityIds").GetArrayLength());
            if (id == "reaction.adjacent.all-by-descending-action-id")
                Assert.Equal(DescendingOrder,
                    moves.Select(value => value.GetProperty("elementId").GetString()));
            if (id == "reaction.recurrence.all-by-action-id")
                Assert.Equal(2, moves.Where(value => value.GetProperty("elementId").GetString() == "commonwealth-element-a")
                    .Select(value => value.GetProperty("windowId").GetString()).Distinct(StringComparer.Ordinal).Count());
            if (id == "reaction.adjacent.two-steps")
            {
                Assert.All(moves.GroupBy(value => value.GetProperty("opportunityId").GetString()),
                    episode => Assert.Equal(2, episode.Count()));
            }

            var world = final.GetProperty("world").GetProperty("elements").EnumerateArray().ToArray();
            foreach (var group in events.Where(value => Kind(value) is "element-moved" or "reacting-element-moved")
                .GroupBy(value => value.GetProperty("elementId").GetString()))
            {
                var cost = group.Select(value => Amount(value.GetProperty("cost").GetProperty("totalCost")))
                    .Aggregate(CapabilityPointAmount.Zero, (total, value) => total + value);
                var element = world.Single(value => value.GetProperty("elementId").GetString() == group.Key);
                Assert.Equal(cost, Amount(element.GetProperty("operationalState").GetProperty("capabilityPointsExpended")));
            }
            using var initial = JsonDocument.Parse(bundle.InitialSnapshotBytes!);
            foreach (var element in world)
            {
                var seed = initial.RootElement.GetProperty("world").GetProperty("elements").EnumerateArray()
                    .Single(value => value.GetProperty("elementId").GetString() == element.GetProperty("elementId").GetString());
                Assert.Equal(seed.GetProperty("operationalState").GetProperty("vehicleBreakdownState").GetRawText(),
                    element.GetProperty("operationalState").GetProperty("vehicleBreakdownState").GetRawText());
            }
            Assert.Equal(events.Last(value => value.TryGetProperty("randomCursorAfter", out _))
                .GetProperty("randomCursorAfter").GetInt64(), final.GetProperty("randomState").GetProperty("nextByteCursor").GetInt64());
            if (id.Contains("positive-zoc", StringComparison.Ordinal))
                Assert.Equal(2, world.Count(value => value.GetProperty("operationalState").GetProperty("movementEnded").ValueKind != JsonValueKind.Null));
            if (id.Contains("low-defense", StringComparison.Ordinal) || id.Contains("remote-zoc", StringComparison.Ordinal))
                Assert.All(world, value => Assert.Equal(JsonValueKind.Null,
                    value.GetProperty("operationalState").GetProperty("movementEnded").ValueKind));
        }
        finally
        {
            foreach (var document in documents) document.Dispose();
        }
    }

    private static string? Kind(JsonElement value) => value.GetProperty("eventType").GetString();

    private static CapabilityPointAmount Amount(JsonElement value) => new(
        value.GetProperty("numerator").GetInt64(), value.GetProperty("denominator").GetInt32());

    private static string EventKind(byte[] bytes)
    {
        using var document = JsonDocument.Parse(bytes);
        return document.RootElement.GetProperty("eventType").GetString()!;
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
    }
}
