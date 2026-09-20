using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatInheritedStepsTests
{
    private sealed record History(CampaignCombatCreationRequest Request, byte[] Created, byte[][] Events);
    private sealed record Trace(History History, CampaignCombatInheritedSelection.State[] Selection,
        CampaignCombatInheritedSelection.Input[] SelectionInputs, byte[][] SelectionEvents,
        CampaignCombatInheritedNoAttack.State[] Steps, CampaignCombatInheritedNoAttack.Input[] StepInputs, byte[][] StepEvents);

    [Fact]
    public void FourActualHistoriesMatchAllEightyEightFrozenCommitmentsAndRestoreEveryCut()
    {
        using var selectionFixture = Fixture("combat-inherited-selection-v1.json");
        using var traversalFixture = Fixture("combat-inherited-no-attack-v1.json");
        var count = 0;
        foreach (var h in Histories())
        {
            var trace = Build(h); var boundary = trace.Selection[0].Boundary;
            var side = boundary.Assessment.Acting.Unit.OriginalSide;
            var moves = boundary.Entry.Lifecycle.Movement.ActualProgressRefs.Count;
            var selectedRow = Row(selectionFixture, side, moves); var stepRow = Row(traversalFixture, side, moves);
            var recordHashes = new[] { CampaignCombatCreationRequestCodec.Serialize(h.Request), h.Created }.Concat(h.Events).Select(Hash).ToArray();
            byte[][] selectionValues = [JsonSerializer.SerializeToUtf8Bytes(recordHashes), CampaignCombatIdentityCodec.SerializeBoundary(boundary),
                .. trace.SelectionEvents, .. trace.Selection.Select(CampaignCombatIdentityCodec.SerializeSelectionControl)];
            CheckGoldens(selectedRow, selectionValues);
            // Reconstruct bytes from typed runtime values, then check independent frozen commitments.
            // Fixtures contain hashes/lengths, not literal serialized Control JSON.
            var predecessorCommitment = JsonSerializer.SerializeToUtf8Bytes(new { bytes = selectionValues.Select(b => b.Length).ToArray(), sha256 = selectionValues.Select(Hash).ToArray() });
            byte[][] traversalValues = [predecessorCommitment, selectionValues[^1], .. trace.StepEvents,
                .. trace.Steps.Select(CampaignCombatIdentityCodec.SerializeTraversalControl)];
            CheckGoldens(stepRow, traversalValues);
            for (var cut = 0; cut <= 2; cut++)
            {
                var bytes = CampaignCombatIdentityCodec.SerializeSelectionControl(trace.Selection[cut]);
                var restored = CampaignCombatInheritedSelection.ReadControl(bytes, h.Request, h.Created, h.Events, trace.SelectionEvents.Take(cut).ToArray());
                Assert.Equal(bytes, CampaignCombatIdentityCodec.SerializeSelectionControl(restored));
                Assert.Equal(boundary.Entry.StateVersion + cut, restored.StateVersion);
                Assert.Equal(selectionValues[1], CampaignCombatIdentityCodec.SerializeBoundary(restored.Boundary));
                for (var retry = 0; retry < cut; retry++)
                {
                    var result = CampaignCombatInheritedSelection.Apply(h.Request, h.Created, h.Events, trace.SelectionEvents.Take(cut).ToArray(), trace.SelectionInputs[retry]);
                    Assert.True(result.IsDuplicate); Assert.Equal(trace.SelectionEvents[retry], result.EventBytes);
                    Assert.Equal(bytes, CampaignCombatIdentityCodec.SerializeSelectionControl(result.State));
                }
            }
            for (var cut = 0; cut <= 6; cut++)
            {
                var bytes = CampaignCombatIdentityCodec.SerializeTraversalControl(trace.Steps[cut]);
                var events = trace.StepEvents.Take(cut).ToArray();
                var restored = CampaignCombatInheritedNoAttack.ReadControl(bytes, h.Request, h.Created, h.Events, trace.SelectionEvents, events);
                Assert.Equal(bytes, CampaignCombatIdentityCodec.SerializeTraversalControl(restored));
                Assert.Equal(selectionValues[^1], CampaignCombatIdentityCodec.SerializeSelectionControl(restored.Selection));
                Assert.Equal(cut, restored.StepIndex); Assert.Equal(cut == 6, restored.Closed);
                for (var retry = 0; retry < cut; retry++)
                {
                    var result = CampaignCombatInheritedNoAttack.Apply(h.Request, h.Created, h.Events, trace.SelectionEvents, events, trace.StepInputs[retry], bytes);
                    Assert.True(result.IsDuplicate); Assert.Equal(trace.StepEvents[retry], result.EventBytes);
                    Assert.Equal(bytes, CampaignCombatIdentityCodec.SerializeTraversalControl(result.State));
                }
            }
            Assert.Equal(moves + 22, h.Events.Length + 8); Assert.Equal(moves + 23, trace.Steps[^1].StateVersion);
            Assert.EndsWith("reserve-release", trace.Steps[^1].Position.PositionId);
            using var frozen = JsonDocument.Parse(selectionValues[^1]);
            Assert.False(frozen.RootElement.GetProperty("segmentClosed").GetBoolean());
            Assert.Equal(0, frozen.RootElement.GetProperty("stepIndex").GetInt32());
            // Entire Boundary includes World, RNG, CP, TOE, ammunition, movement and completion proofs.
            Assert.Equal(selectionValues[1], CampaignCombatIdentityCodec.SerializeBoundary(trace.Steps[^1].Selection.Boundary));
            count++;
        }
        Assert.Equal(4, count);
    }

    [Fact]
    public void CreationAndCompleteG2AreMandatoryAndLocalTailsCannotReplaceTheirAuthority()
    {
        var histories = Histories().ToArray(); var h = histories[0]; var trace = Build(h);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSelection.Replay(h.Request, [], h.Events, []));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSelection.Replay(h.Request, h.Created, h.Events[..^1], []));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSelection.Replay(h.Request, h.Created, [], trace.SelectionEvents));
        foreach (var other in histories.Skip(1))
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSelection.Replay(other.Request, other.Created, other.Events, trace.SelectionEvents));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedNoAttack.Replay(h.Request, h.Created, h.Events, [], []));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedNoAttack.Replay(h.Request, h.Created, h.Events, trace.SelectionEvents[..1], []));
        foreach (var bad in new[] { new[] { trace.SelectionEvents[1] }, trace.SelectionEvents.Reverse().ToArray(), new[] { trace.SelectionEvents[0], trace.SelectionEvents[0] } })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSelection.Replay(h.Request, h.Created, h.Events, bad));
        foreach (var bad in new[] { trace.StepEvents.Skip(1).ToArray(), trace.StepEvents.Reverse().ToArray(), new[] { trace.StepEvents[0], trace.StepEvents[0] },
            trace.StepEvents.Take(2).Append(trace.StepEvents[3]).ToArray() })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedNoAttack.Replay(h.Request, h.Created, h.Events, trace.SelectionEvents, bad));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedNoAttack.Apply(h.Request, h.Created, h.Events, trace.SelectionEvents, trace.StepEvents,
            CampaignCombatInheritedNoAttack.CreateInput(trace.Steps[^1])));
    }

    [Fact]
    public void RehashedCanonicalEventForgeriesAndControlCachesCannotOverrideCausality()
    {
        var h = Histories().First(); var trace = Build(h);
        foreach (var (path, value) in new (string, JsonNode?)[] { ("priorVersion", JsonValue.Create(1)), ("priorPrefix", JsonValue.Create(ForeignHash)),
            ("stateVersion", JsonValue.Create(55)), ("cycleId", JsonValue.Create(ForeignHash)), ("input.actor", JsonValue.Create("axis")),
            ("effect.assessmentHash", JsonValue.Create(ForeignHash)), ("effect.boundaryHash", JsonValue.Create(ForeignHash)), ("effect.candidateCount", JsonValue.Create(1)) })
        {
            var changed = Change(trace.SelectionEvents[0], path, value);
            Rehash(changed, "cis.", "sandtable.combat.inherited-selection-receipt.v2");
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSelection.Replay(h.Request, h.Created, h.Events, [Bytes(changed)]));
        }
        foreach (var (path, value) in new (string, JsonNode?)[] { ("priorVersion", JsonValue.Create(1)), ("priorPrefix", JsonValue.Create(ForeignHash)),
            ("input.actor", JsonValue.Create("commonwealth")), ("effect.fromPositionId", JsonValue.Create("foreign.position")),
            ("effect.toPositionId", JsonValue.Create("foreign.position")), ("effect.previousStepReceiptId", JsonValue.Create("foreign.receipt")),
            ("effect.dispositionReceiptId", JsonValue.Create("foreign.receipt")), ("effect.proofKind", JsonValue.Create("foreign.proof")) })
        {
            var changed = Change(trace.StepEvents[0], path, value);
            Rehash(changed, "cin.", "sandtable.combat.inherited-no-attack-receipt.v2");
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedNoAttack.Replay(h.Request, h.Created, h.Events, trace.SelectionEvents, [Bytes(changed)]));
        }
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSelection.Replay(h.Request, h.Created, h.Events,
            [Bytes(Change(trace.SelectionEvents[0], "receiptId", JsonValue.Create("foreign.receipt")))]));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedNoAttack.Replay(h.Request, h.Created, h.Events, trace.SelectionEvents,
            [Bytes(Change(trace.StepEvents[0], "receiptId", JsonValue.Create("foreign.receipt")))]));
        var selected = CampaignCombatIdentityCodec.SerializeSelectionControl(trace.Selection[^1]);
        foreach (var (path, value) in new (string, JsonNode?)[] { ("stateVersion", JsonValue.Create(1)), ("prefix", JsonValue.Create(ForeignHash)),
            ("boundaryHash", JsonValue.Create(ForeignHash)), ("boundary.assessment.adjacent", JsonValue.Create(true)),
            ("openingReceiptId", JsonValue.Create("foreign.receipt")), ("selectionReceiptId", JsonValue.Create("foreign.receipt")),
            ("stepIndex", JsonValue.Create(1)), ("segmentClosed", JsonValue.Create(true)) })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSelection.ReadControl(Bytes(Change(selected, path, value)), h.Request, h.Created, h.Events, trace.SelectionEvents));
        var current = CampaignCombatIdentityCodec.SerializeTraversalControl(trace.Steps[1]);
        foreach (var (path, value) in new (string, JsonNode?)[] { ("stateVersion", JsonValue.Create(1)), ("prefix", JsonValue.Create(ForeignHash)),
            ("selectionHash", JsonValue.Create(ForeignHash)), ("stepIndex", JsonValue.Create(2)), ("closed", JsonValue.Create(true)),
            ("position.positionId", JsonValue.Create("foreign.position")), ("selection.stepIndex", JsonValue.Create(1)), ("selection.segmentClosed", JsonValue.Create(true)) })
        {
            var changed = Bytes(Change(current, path, value));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedNoAttack.ReadControl(changed, h.Request, h.Created, h.Events, trace.SelectionEvents, trace.StepEvents[..1]));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedNoAttack.Apply(h.Request, h.Created, h.Events, trace.SelectionEvents, trace.StepEvents[..1], trace.StepInputs[1], changed));
        }
    }

    [Fact]
    public void ChangedStaleForeignOrUnauthorizedCommandsNeverBecomeRetries()
    {
        var h = Histories().First(); var trace = Build(h); var input = trace.SelectionInputs[0];
        foreach (var bad in new[] { input with { Actor = CampaignOpeningPreambleActor.Axis }, input with { Command = input.Command with { ExpectedPriorVersion = 1 } },
            input with { Command = input.Command with { SegmentId = "foreign.segment" } }, input with { Command = input.Command with { ExpectedPositionId = "foreign.position" } },
            input with { Command = input.Command with { OpeningReceiptId = "foreign.receipt" } }, input with { Command = input.Command with { Kind = "complete-step" } },
            input with { Command = input.Command with { ContractVersion = 1 } } })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSelection.Apply(h.Request, h.Created, h.Events, trace.SelectionEvents, bad));
        var step = trace.StepInputs[0];
        foreach (var bad in new[] { step with { Actor = CampaignOpeningPreambleActor.Commonwealth }, step with { Command = step.Command with { ExpectedPriorVersion = 1 } },
            step with { Command = step.Command with { SegmentId = "foreign.segment" } }, step with { Command = step.Command with { FromPositionId = "foreign.position" } },
            step with { Command = step.Command with { DispositionReceiptId = trace.Selection[^1].OpeningReceiptId! } }, step with { Command = step.Command with { ContractVersion = 1 } } })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedNoAttack.Apply(h.Request, h.Created, h.Events, trace.SelectionEvents, trace.StepEvents, bad));
    }

    [Fact]
    public void MalformedControlsEventsAndCapacityFailBeforeTrustedHistoryAccess()
    {
        var h = Histories().First(); var trace = Build(h); var sentry = new Sentry(1);
        foreach (var bytes in new[] { CampaignCombatIdentityCodec.SerializeSelectionControl(trace.Selection[^1]), CampaignCombatIdentityCodec.SerializeTraversalControl(trace.Steps[^1]) })
        {
            var text = Encoding.UTF8.GetString(bytes); var root = JsonNode.Parse(bytes)!;
            var missing = root.DeepClone(); missing.AsObject().Remove("contractVersion");
            var unknown = root.DeepClone(); unknown["unknown"] = 0;
            var nested = root.DeepClone(); var boundary = nested["boundary"] ?? nested["selection"]!["boundary"]!; boundary["entry"]!["world"]!["elements"]![0]!["operationalState"] = null;
            var reverse = new JsonObject(); foreach (var pair in root.AsObject().Reverse()) reverse.Add(pair.Key, pair.Value?.DeepClone());
            var hostile = new[] { Encoding.UTF8.GetBytes("null"), Encoding.UTF8.GetBytes("[]"), Encoding.UTF8.GetBytes("{}"), Encoding.UTF8.GetBytes(text + " "),
                Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"contractVersion\":1,\"contractVersion\":1", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(text.Replace("\"prefix\"", "\"pre\\u0066ix\"", StringComparison.Ordinal)),
                Bytes(missing), Bytes(unknown), Bytes(nested), Bytes(reverse), new byte[1_048_577],
                Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)),
                Encoding.UTF8.GetBytes("{\"entry.world.cohesionCauses\":[" + string.Join(',', Enumerable.Repeat("null", 513)) + "]}") };
            foreach (var bad in hostile)
            {
                Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSelection.ReadControl(bad, null!, h.Created, sentry, []));
                Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedNoAttack.ReadControl(bad, null!, h.Created, sentry, [], []));
                Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedNoAttack.Apply(null!, h.Created, sentry, [], [], trace.StepInputs[0], bad));
            }
        }
        Assert.Throws<InvalidOperationException>(() => CampaignCombatInheritedSelection.ReadControl(
            CampaignCombatIdentityCodec.SerializeSelectionControl(trace.Selection[^1]), h.Request, h.Created, sentry, []));
        Assert.Throws<InvalidOperationException>(() => CampaignCombatInheritedNoAttack.ReadControl(
            CampaignCombatIdentityCodec.SerializeTraversalControl(trace.Steps[^1]), h.Request, h.Created, sentry, [], []));
        foreach (var bad in new[] { Encoding.UTF8.GetBytes("{}"), [.. trace.SelectionEvents[0], (byte)' '] })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSelection.Replay(null!, h.Created, sentry, [bad]));
        foreach (var bad in new[] { Encoding.UTF8.GetBytes("{}"), Bytes(Change(trace.StepEvents[0], "effect.kind", JsonValue.Create("foreign"))) })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedNoAttack.Replay(null!, h.Created, sentry, [], [bad]));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSelection.Replay(null!, h.Created, sentry, new Sentry(3)));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedNoAttack.Replay(null!, h.Created, sentry, [], new Sentry(7)));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSelection.Replay(h.Request, h.Created, h.Events, [new byte[1_048_577]]));
    }

    [Fact]
    public void ReturnedAndRetainedBuffersRemainOwnedAcrossCallerMutation()
    {
        var h = Histories().First(); var trace = Build(h);
        var result = CampaignCombatInheritedNoAttack.Apply(h.Request, h.Created, h.Events, trace.SelectionEvents, trace.StepEvents[..1], trace.StepInputs[0]);
        var expectedEvent = result.EventBytes; var expected = CampaignCombatIdentityCodec.SerializeTraversalControl(result.State);
        Array.Fill(result.EventBytes, (byte)0); Array.Fill(result.State.Events[0], (byte)0);
        foreach (var bytes in h.Events.Concat(trace.SelectionEvents).Concat(trace.StepEvents).Append(h.Created)) Array.Fill(bytes, (byte)0);
        Assert.Equal(expectedEvent, result.EventBytes); Assert.Equal(expected, CampaignCombatIdentityCodec.SerializeTraversalControl(result.State));
        Array.Fill(expected, (byte)0); Assert.NotEqual(expected, CampaignCombatIdentityCodec.SerializeTraversalControl(result.State));
    }

    [Fact]
    public void CreatedIsCapturedBeforeAdversarialLocalTailIndexing()
    {
        var h = Histories().First(); var trace = Build(h); var created = h.Created.ToArray();
        var selected = CampaignCombatInheritedSelection.Replay(h.Request, created, h.Events,
            new MutatingList(trace.SelectionEvents, () => Array.Fill(created, (byte)0)));
        Assert.Equal(CampaignCombatIdentityCodec.SerializeSelectionControl(trace.Selection[^1]), CampaignCombatIdentityCodec.SerializeSelectionControl(selected));
        created = h.Created.ToArray();
        var traversal = CampaignCombatInheritedNoAttack.Replay(h.Request, created, h.Events, trace.SelectionEvents,
            new MutatingList(trace.StepEvents, () => Array.Fill(created, (byte)0)));
        Assert.Equal(CampaignCombatIdentityCodec.SerializeTraversalControl(trace.Steps[^1]), CampaignCombatIdentityCodec.SerializeTraversalControl(traversal));
    }
    private sealed class MutatingList(byte[][] values, Action mutation) : IReadOnlyList<byte[]>
    {
        public int Count { get { mutation(); return values.Length; } }
        public byte[] this[int index] { get { mutation(); return values[index]; } }
        public IEnumerator<byte[]> GetEnumerator() => throw new InvalidOperationException("Use bounded indexed capture.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static IEnumerable<History> Histories()
    {
        foreach (var h in CombatMovementHistoryReplayTests.SnapshotHistories().Where(h => h.Events.Length >= 20))
        {
            using var tail = JsonDocument.Parse(h.Events[^1]);
            if (tail.RootElement.GetProperty("eventType").GetString() == "breakdown-segment-completed") yield return new(h.Request, h.Created, h.Events);
        }
    }
    private static Trace Build(History h)
    {
        var selections = new List<CampaignCombatInheritedSelection.State> { CampaignCombatInheritedSelection.Replay(h.Request, h.Created, h.Events, []) };
        var selectionInputs = new List<CampaignCombatInheritedSelection.Input>(); var selectionEvents = new List<byte[]>();
        foreach (var kind in new[] { "open-segment", "close-empty-selection" })
        {
            var input = CampaignCombatInheritedSelection.CreateInput(selections[^1], kind);
            var result = CampaignCombatInheritedSelection.Apply(h.Request, h.Created, h.Events, selectionEvents, input);
            Assert.False(result.IsDuplicate); selectionInputs.Add(input); selections.Add(result.State); selectionEvents.Add(result.EventBytes);
        }
        var steps = new List<CampaignCombatInheritedNoAttack.State> { CampaignCombatInheritedNoAttack.Replay(h.Request, h.Created, h.Events, selectionEvents, []) };
        var inputs = new List<CampaignCombatInheritedNoAttack.Input>(); var events = new List<byte[]>();
        for (var index = 0; index < 6; index++)
        {
            var input = CampaignCombatInheritedNoAttack.CreateInput(steps[^1]);
            var result = CampaignCombatInheritedNoAttack.Apply(h.Request, h.Created, h.Events, selectionEvents, events, input);
            Assert.False(result.IsDuplicate); inputs.Add(input); steps.Add(result.State); events.Add(result.EventBytes);
        }
        return new(h, selections.ToArray(), selectionInputs.ToArray(), selectionEvents.ToArray(), steps.ToArray(), inputs.ToArray(), events.ToArray());
    }
    private const string ForeignHash = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private static string Hash(byte[] bytes) => CampaignOpeningPreambleCodec.Hash(bytes);
    private static JsonDocument Fixture(string name) => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", name)));
    private static JsonElement Row(JsonDocument fixture, string actor, int moves) => fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("actor").GetString() == actor && c.GetProperty("moves").GetInt32() == moves);
    private static void CheckGoldens(JsonElement row, byte[][] values)
    {
        var gold = row.GetProperty("goldens"); Assert.Equal(values.Length, gold.GetProperty("bytes").GetArrayLength());
        for (var index = 0; index < values.Length; index++)
        { Assert.Equal(gold.GetProperty("bytes")[index].GetInt32(), values[index].Length); Assert.Equal(gold.GetProperty("sha256")[index].GetString(), Hash(values[index])); }
    }
    private static JsonNode Change(byte[] bytes, string path, JsonNode? value)
    {
        var root = JsonNode.Parse(bytes)!; var parts = path.Split('.'); var current = root;
        foreach (var part in parts[..^1]) current = current[part]!;
        current[parts[^1]] = value?.DeepClone(); return root;
    }
    private static byte[] Bytes(JsonNode value) => Encoding.UTF8.GetBytes(value.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
    private static void Rehash(JsonNode root, string prefix, string domain)
    {
        root.AsObject().Remove("receiptId"); root["receiptId"] = prefix + CampaignOpeningPreambleCodec.HashWithDomain(domain, Bytes(root))[7..];
    }
    private sealed class Sentry(int count) : IReadOnlyList<byte[]>
    {
        public int Count => count;
        public byte[] this[int index] => throw new InvalidOperationException("Trusted history was accessed before hostile bytes were rejected.");
        public IEnumerator<byte[]> GetEnumerator() => throw new InvalidOperationException("Trusted history was enumerated.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
