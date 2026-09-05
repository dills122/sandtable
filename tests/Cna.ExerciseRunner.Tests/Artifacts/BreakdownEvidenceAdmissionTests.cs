using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class BreakdownEvidenceAdmissionTests
{
    private const string MovementPosition = "land.position.operation-1.first-player.movement-and-combat.movement";
    private const string StopPosition = "land.position.breakdown-stop";

    [Fact]
    public void CurrentSnapshotAdmitsCertifiedElevenAndBreakdownFlow()
    {
        var start = Begin();
        var snapshot = ExerciseEvidenceCodec.DeserializeSnapshot(start.InitialSnapshotBytes!);
        Assert.Equal(Cna1979Ruleset.Manifest.Hash, snapshot.RulesetHash);
        Assert.Equal(CampaignExercises.QueryCheckpoint(start.Session!).PositionId, snapshot.PositionId);
    }

    [Theory]
    [InlineData("snapshot")]
    [InlineData("world")]
    [InlineData("setup")]
    [InlineData("content")]
    [InlineData("profile")]
    [InlineData("rules")]
    [InlineData("missing-flow")]
    [InlineData("unknown-flow-field")]
    public void CurrentSnapshotRejectsEveryLegacyOrMixedRoot(string mutation)
    {
        var root = JsonNode.Parse(Begin().InitialSnapshotBytes!)!;
        switch (mutation)
        {
            case "snapshot": root["contractVersion"] = 10; break;
            case "world": root["world"]!["contractVersion"] = 5; break;
            case "setup": root["setup"]!["schemaVersion"] = 5; break;
            case "content": root["setup"]!["content"]!["schemaVersion"] = 5; break;
            case "profile": root["setup"]!["capabilityProfileId"] = "unknown-profile"; break;
            case "rules": root["rulesetHash"] = Cna1979Ruleset.HistoricalManifestV8.Hash; break;
            case "missing-flow": root.AsObject().Remove("breakdownFlow"); break;
            case "unknown-flow-field": root["breakdownFlow"]!["stopId"] = "forged"; break;
        }
        Assert.Throws<JsonException>(() => ExerciseEvidenceCodec.DeserializeSnapshot(JsonBytes(root)));
    }

    [Fact]
    public void CurrentAcceptedStepRegistryAdmitsTruckStopResolutionAndBothCompletions()
    {
        var trace = ReachMovement();
        Accept(trace, (_, action) => action is MoveElementAction { ElementId: "axis-truck", DestinationLocationId: "center" });
        Accept(trace, (_, action) => action.Kind == "stop-element-movement");
        Accept(trace, (_, action) => action.Kind == "resolve-breakdown-stop");
        Accept(trace, (_, action) => action.Kind == "complete-movement-segment");
        Accept(trace, (_, action) => action.Kind == "complete-breakdown-segment");

        AssertTraceReadback(trace);
        Assert.Equal(["element-moved", "element-movement-stopped", "breakdown-stop-resolved",
            "movement-segment-completed", "breakdown-segment-completed"], trace.Steps.TakeLast(5).Select(EventKind));
        Assert.Equal(StopPosition, trace.Steps[^4].Receipt.ResultingPositionId);
        Assert.Equal(MovementPosition, trace.Steps[^3].Receipt.ResultingPositionId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CurrentReactionRegistryPreservesPendingStopBeforePhasingResumption(bool activeSystemClose)
    {
        var trace = ReachMovement();
        foreach (var destination in new[] { "west", "south-west", "south" })
            Accept(trace, (_, action) => action is MoveElementAction move
                && move.ElementId == "axis-infantry" && move.DestinationLocationId == destination);
        Accept(trace, (_, action) => action.Kind == "move-reacting-element");
        Accept(trace, (_, action) => action.Kind == (activeSystemClose
            ? "close-reaction-window-scripted-unavailable" : "complete-reaction-participant"));
        Assert.Equal(StopPosition, trace.Steps[^1].Receipt.ResultingPositionId);
        Accept(trace, (_, action) => action.Kind == "resolve-breakdown-stop");
        if (!activeSystemClose)
            Accept(trace, (set, action) => set.Audience == CampaignActionAudience.System
                && action.Kind.StartsWith("close-reaction-window-", StringComparison.Ordinal));
        AssertTraceReadback(trace);
        Assert.Equal(MovementPosition, trace.Steps[^1].Receipt.ResultingPositionId);
    }

    [Theory]
    [InlineData("event-version")]
    [InlineData("rules")]
    [InlineData("sequence")]
    [InlineData("missing-flow")]
    [InlineData("extra-root")]
    [InlineData("extra-flow")]
    [InlineData("duplicate")]
    public void CurrentEventReaderRejectsMixedOrNoncontractEvidence(string mutation)
    {
        var trace = ReachMovement();
        Accept(trace, (_, action) => action is MoveElementAction { ElementId: "axis-truck", DestinationLocationId: "center" });
        var root = JsonNode.Parse(trace.Steps[^1].EventRecords.Single())!;
        switch (mutation)
        {
            case "event-version": root["contractVersion"] = 2; break;
            case "rules": root["rulesetHash"] = Cna1979Ruleset.HistoricalManifestV8.Hash; break;
            case "sequence": root["sequencePosition"]!["contractVersion"] = 3; break;
            case "missing-flow": root.AsObject().Remove("breakdownFlowAfter"); break;
            case "extra-root": root["selectedLoss"] = 0; break;
            case "extra-flow": root["breakdownFlowAfter"]!["selectedLoss"] = 0; break;
        }
        var json = root.ToJsonString();
        if (mutation == "duplicate") json = json.Replace("\"contractVersion\":3,",
            "\"contractVersion\":3,\"contractVersion\":3,", StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => ExerciseEvidenceCodec.DeserializeCanonicalEvents(Encoding.UTF8.GetBytes(json + "\n")));
    }

    [Fact]
    public void CurrentEventReaderRejectsLegacySequenceInUnchangedPreambleSchema()
    {
        var trace = ReachMovement();
        var root = JsonNode.Parse(trace.Steps[0].EventRecords.Single())!;
        root["sequencePosition"]!["contractVersion"] = 3;
        Assert.Throws<JsonException>(() => ExerciseEvidenceCodec.DeserializeCanonicalEvents(Frame(JsonBytes(root))));
    }

    private static void AssertTraceReadback(Trace trace)
    {
        foreach (var step in trace.Steps)
        {
            var bytes = step.EventRecords.Single();
            var record = Assert.Single(ExerciseEvidenceCodec.DeserializeCanonicalEvents(Frame(bytes)));
            Assert.Equal(bytes, record.CanonicalBytes);
            Assert.Equal(step.Receipt.CommittedStateVersion, record.StateVersion);
            Assert.Equal(step.Receipt.ResultingPositionId, record.PositionId);
            Assert.Equal(record.PositionId, ExerciseEvidenceCodec.DeserializeSnapshot(step.SnapshotCheckpoint).PositionId);
        }
    }

    private static ExerciseStartResult Begin()
    {
        var content = Cna1979SyntheticContentCatalog.ArtifactV6;
        var start = CampaignExercises.Begin(new CampaignCreationRequest(1, "breakdown-runner-admission",
            Cna1979Ruleset.Manifest.Hash, 12345, "rules-lab.breakdown.truck.v1",
            "sha256:e6631e81ad8f97e39fd9d7eec93bad7fe2b39db4d2d3059ed94a02dd4093e7a3",
            content.Identity.PackId, content.Identity.Hash, "breakdown-truck-lab"));
        Assert.True(start.IsStarted, start.RejectionReason.ToString());
        return start;
    }

    private static Trace ReachMovement()
    {
        var trace = new Trace(Begin().Session!);
        for (var count = 0; count < 20; count++)
        {
            if (CampaignExercises.QueryCheckpoint(trace.Session).PositionId == MovementPosition) return trace;
            var choices = Choices(trace);
            var choice = choices.FirstOrDefault(value => value.Action.Kind == "complete-reserve-designation")
                ?? choices.FirstOrDefault(value => value.Action.Kind == "act-first") ?? choices[0];
            Submit(trace, choice);
        }
        throw new InvalidOperationException("Certified preamble did not reach Movement.");
    }

    private static void Accept(Trace trace, Func<CampaignLegalActionSet, CampaignActionCandidate, bool> predicate) =>
        Submit(trace, Choices(trace).First(value => predicate(value.Set, value.Action)));

    private static List<Choice> Choices(Trace trace) => Enum.GetValues<CampaignActionAudience>()
        .Select(audience => CampaignExercises.Query(trace.Session, audience))
        .Where(query => query.IsSuccessful)
        .SelectMany(query => query.ActionSet!.Candidates.Select(action => new Choice(query.ActionSet, action)))
        .ToList();

    private static void Submit(Trace trace, Choice choice)
    {
        var set = choice.Set;
        var accepted = CampaignExercises.Submit(trace.Session, new CampaignActionSubmission(1,
            set.CampaignId, set.StateVersion, set.PositionId, set.Audience, choice.Action.ActionId));
        Assert.True(accepted.IsAccepted, $"{choice.Action.Kind}: {accepted.RejectionReason}");
        trace.Session = accepted.SuccessorSession!;
        trace.Steps.Add(accepted.Evidence!);
    }

    private static string EventKind(ExerciseStepEvidence step) =>
        JsonNode.Parse(step.EventRecords.Single())!["eventType"]!.GetValue<string>();
    private static byte[] JsonBytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());
    private static byte[] Frame(byte[] bytes) => [.. bytes, (byte)'\n'];
    private sealed record Choice(CampaignLegalActionSet Set, CampaignActionCandidate Action);
    private sealed class Trace(ExerciseSession session)
    {
        public ExerciseSession Session { get; set; } = session;
        public List<ExerciseStepEvidence> Steps { get; } = [];
    }
}
