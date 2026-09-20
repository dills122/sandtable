using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatStepsHistoryReplayTests
{
    private sealed record Trace(CampaignCombatCreationRequest Request, byte[] Created, byte[][] Predecessor,
        byte[][] Selection, byte[][] Steps)
    {
        public byte[][] Events => [.. Predecessor, .. Selection, .. Steps];
    }

    [Fact]
    public void CompleteActualSuffixIsConsumedByGenericReplay()
    {
        var trace = Build(Histories().First());
        var result = CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, trace.Events);
        Assert.Equal(trace.Events.Length + 1, result.Projection.StateVersion);
        Assert.Equal(trace.Events.Length, result.Projection.Receipts.Count);
        Assert.EndsWith("reserve-release", result.Projection.SequencePosition.PositionId);
    }

    [Fact]
    public void FourHistoriesPreserveAllPrefixesLedgersAndFrozenLocalControls()
    {
        using var selectionFixture = Fixture("combat-inherited-selection-v1.json");
        using var stepFixture = Fixture("combat-inherited-no-attack-v1.json");
        var visits = 0;
        var newCuts = 0;
        var withG2 = 0;
        foreach (var history in Histories())
        {
            var trace = Build(history);
            var events = trace.Events;
            var boundary = CampaignCombatCertification.Admit(trace.Request, trace.Created, trace.Predecessor);
            var side = boundary.Assessment.Acting.Unit.OriginalSide;
            var moves = boundary.Entry.Lifecycle.Movement.ActualProgressRefs.Count;
            var selectionGold = Row(selectionFixture, side, moves);
            var stepGold = Row(stepFixture, side, moves);
            var prefix = CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, []).Projection.Prefix;
            for (var cut = 0; cut <= events.Length; cut++)
            {
                var result = CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, events[..cut]);
                var projection = result.Projection;
                Assert.Equal(cut + 1, projection.StateVersion);
                Assert.Equal(prefix, projection.Prefix);
                Assert.Equal(cut, projection.Receipts.Count);
                for (var index = 0; index < cut; index++)
                {
                    Assert.Equal(index + 2, projection.Receipts[index].StateVersion);
                    Assert.Equal(Hash(events[index]), projection.Receipts[index].EventHash);
                }
                visits++;
                if (cut >= trace.Predecessor.Length) withG2++;
                var local = cut - trace.Predecessor.Length;
                if (local == 0) Assert.IsType<CampaignCombatHistoryProjection.BreakdownCompletion>(projection);
                if (local is 1 or 2)
                {
                    var state = Assert.IsType<CampaignCombatHistoryProjection.Selection>(projection).State;
                    var bytes = CampaignCombatIdentityCodec.SerializeSelectionControl(state);
                    Golden(selectionGold, 4 + local, bytes);
                    Golden(selectionGold, 1 + local, trace.Selection[local - 1]);
                    var restored = CampaignCombatInheritedSelection.ReadControl(bytes, trace.Request, trace.Created,
                        trace.Predecessor, trace.Selection[..local]);
                    Assert.Equal(bytes, CampaignCombatIdentityCodec.SerializeSelectionControl(restored));
                    Assert.Equal(boundary.Entry.Receipts.Concat(state.Receipts), projection.Receipts);
                    Assert.Equal(boundary.Entry.SequencePosition, projection.SequencePosition);
                    Assert.Equal(local, state.Receipts.Count);
                    Assert.Equal(CampaignCombatIdentityCodec.SerializeBoundary(boundary), CampaignCombatIdentityCodec.SerializeBoundary(state.Boundary));
                    newCuts++;
                }
                else if (local > 2)
                {
                    var step = local - 2;
                    var state = Assert.IsType<CampaignCombatHistoryProjection.NoAttack>(projection).State;
                    var bytes = CampaignCombatIdentityCodec.SerializeTraversalControl(state);
                    Golden(stepGold, 8 + step, bytes);
                    Golden(stepGold, 1 + step, trace.Steps[step - 1]);
                    var restored = CampaignCombatInheritedNoAttack.ReadControl(bytes, trace.Request, trace.Created,
                        trace.Predecessor, trace.Selection, trace.Steps[..step]);
                    Assert.Equal(bytes, CampaignCombatIdentityCodec.SerializeTraversalControl(restored));
                    Assert.Equal(boundary.Entry.Receipts.Concat(state.Selection.Receipts).Concat(state.Receipts), projection.Receipts);
                    Assert.Equal(step, state.StepIndex);
                    Assert.Equal(step == 6, state.Closed);
                    Assert.Equal(state.Position, projection.SequencePosition);
                    var nested = CampaignCombatIdentityCodec.SerializeSelectionControl(state.Selection);
                    Golden(selectionGold, 6, nested);
                    using var control = JsonDocument.Parse(nested);
                    Assert.Equal(0, control.RootElement.GetProperty("stepIndex").GetInt32());
                    Assert.False(control.RootElement.GetProperty("segmentClosed").GetBoolean());
                    Assert.Equal(CampaignCombatIdentityCodec.SerializeBoundary(boundary), CampaignCombatIdentityCodec.SerializeBoundary(state.Selection.Boundary));
                    newCuts++;
                }
                if (cut < events.Length) prefix = CampaignOpeningPreambleCodec.EventPrefix(prefix, events[cut]);
            }
        }
        Assert.Equal(118, visits);
        Assert.Equal(36, withG2);
        Assert.Equal(32, newCuts);
    }

    [Fact]
    public void MissingReorderedForgedAndExcessTailsNeverBecomeAcceptedPrefixes()
    {
        var traces = Histories().Select(Build).ToArray();
        var trace = traces[0];
        var suffix = trace.Selection.Concat(trace.Steps).ToArray();
        foreach (var tail in new byte[][][]
        {
            suffix[1..], suffix.Reverse().ToArray(), [suffix[0], suffix[0]],
            [.. suffix, suffix[^1]], [.. suffix, "{}"u8.ToArray()],
            [suffix[0], suffix[2]], [.. suffix[..4], suffix[5]],
            [trace.Predecessor[^1], .. suffix]
        }) Reject(trace, [.. trace.Predecessor, .. tail]);
        Reject(trace, [.. trace.Predecessor[..^1], .. suffix]);
        foreach (var other in traces.Skip(1)) Reject(trace, [.. trace.Predecessor, .. other.Selection, .. other.Steps]);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatHistoryReplay.Replay(trace.Request, [], trace.Events));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatHistoryReplay.Replay(trace.Request, [.. trace.Created, (byte)' '], trace.Events));
        var foreignCreated = JsonNode.Parse(trace.Created)!;
        foreignCreated["campaignId"] = "foreign.campaign";
        Assert.ThrowsAny<JsonException>(() => CampaignCombatHistoryReplay.Replay(trace.Request, Bytes(foreignCreated), trace.Events));
        foreach (var (index, field, value) in new (int, string, JsonNode?)[]
        {
            (0, "contractVersion", JsonValue.Create(1)), (0, "priorVersion", JsonValue.Create(1)),
            (0, "priorPrefix", JsonValue.Create(ForeignHash)), (0, "input.actor", JsonValue.Create("axis")),
            (0, "effect.candidateCount", JsonValue.Create(1)), (1, "effect.kind", JsonValue.Create("foreign")),
            (2, "effect.fromPositionId", JsonValue.Create("foreign.position")),
            (2, "effect.toPositionId", JsonValue.Create("foreign.position")),
            (2, "effect.dispositionReceiptId", JsonValue.Create("foreign.receipt")),
            (2, "effect.previousStepReceiptId", JsonValue.Create("foreign.receipt"))
        })
        {
            var changed = JsonNode.Parse(suffix[index])!;
            var parts = field.Split('.'); var target = changed;
            foreach (var part in parts[..^1]) target = target[part]!;
            target[parts[^1]] = value;
            changed.AsObject().Remove("receiptId");
            var domain = index < 2 ? "sandtable.combat.inherited-selection-receipt.v2" : "sandtable.combat.inherited-no-attack-receipt.v2";
            changed["receiptId"] = (index < 2 ? "cis." : "cin.") + CampaignOpeningPreambleCodec.HashWithDomain(domain, Bytes(changed))[7..];
            var forged = suffix.ToArray(); forged[index] = Bytes(changed);
            Reject(trace, [.. trace.Predecessor, .. forged]);
        }
        Assert.NotNull(CampaignCombatCertification.Admit(trace.Request, trace.Created, trace.Predecessor));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatCertification.Admit(trace.Request, trace.Created, trace.Events));
    }

    [Fact]
    public void SyntheticC3aAndReactionForksCannotSupplyActualSelectionProvenance()
    {
        var trace = Build(Histories().First());
        using var fixture = Fixture("combat-selection-steps-v1.json");
        foreach (var row in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var events = row.GetProperty("events").EnumerateArray()
                .Select(e => Encoding.UTF8.GetBytes(e.GetProperty("canonicalUtf8").GetString()!)).ToArray();
            Reject(trace, [.. trace.Predecessor, .. events]);
            Reject(trace, events);
        }
        foreach (var history in CombatMovementHistoryReplayTests.SnapshotHistories())
        {
            using var tail = JsonDocument.Parse(history.Events[^1]);
            if (history.Events.Length >= 20 || tail.RootElement.GetProperty("eventType").GetString() != "breakdown-segment-completed") continue;
            Assert.ThrowsAny<JsonException>(() => CampaignCombatHistoryReplay.Replay(history.Request, history.Created, [.. history.Events, .. trace.Selection]));
        }
        var reaction = CombatReactionHistoryReplayTests.SnapshotHistories().Last();
        Assert.ThrowsAny<JsonException>(() => CampaignCombatHistoryReplay.Replay(reaction.Request, reaction.Created, [.. reaction.Events, .. trace.Selection]));
    }

    [Fact]
    public void NewFamiliesFailClosedForSnapshot12WhileExactOldG2StillRestores()
    {
        var trace = Build(Histories().First());
        var old = CampaignCombatInheritedSnapshotV12Codec.Serialize(trace.Request, trace.Created, trace.Predecessor);
        var restored = CampaignCombatInheritedSnapshotV12Codec.Restore(old, trace.Request, trace.Created, trace.Predecessor, false);
        Assert.IsType<CampaignCombatHistoryProjection.BreakdownCompletion>(restored.Projection);
        foreach (var length in new[] { 1, 2, 3, 8 })
        {
            var events = trace.Events[..(trace.Predecessor.Length + length)];
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Serialize(trace.Request, trace.Created, events));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Restore(old, trace.Request, trace.Created, events, false));
        }
    }

    [Fact]
    public void ReturnedHistoryAndCumulativeLedgerAreOwnedAndStateCannotBeReplaced()
    {
        var trace = Build(Histories().First());
        var source = trace.Events;
        var result = CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, source);
        var hash = result.Projection.Prefix;
        var created = result.History.Created;
        var copies = result.History.Events;
        created[0] ^= 1; copies[0][0] ^= 1; source[0][0] ^= 1; trace.Created[0] ^= 1;
        Assert.Equal(hash, CampaignCombatHistoryReplay.Replay(trace.Request, result.History.Created, result.History.Events).Projection.Prefix);
        var ledger = Assert.IsAssignableFrom<IList<CampaignOpeningPreambleReceipt>>(result.Projection.Receipts);
        Assert.Throws<NotSupportedException>(() => ledger[0] = ledger[^1]);
        Assert.Null(typeof(CampaignCombatHistoryProjection.Selection).GetProperty("State")!.SetMethod);
        Assert.Null(typeof(CampaignCombatHistoryProjection.NoAttack).GetProperty("State")!.SetMethod);
    }

    [Fact]
    public void RetainedBoundsRejectBeforeIndexingAndCreatedIsOwnedBeforeListAccess()
    {
        var trace = Build(Histories().First());
        Assert.ThrowsAny<JsonException>(() => CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, new Sentry(513)));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatHistoryReplay.Replay(trace.Request, new byte[1_048_577], new Sentry(1)));
        Reject(trace, [new byte[1_048_577]]);
        Reject(trace, Enumerable.Repeat(new byte[1_048_576], 17).ToArray());
        var created = trace.Created.ToArray();
        var events = trace.Events;
        var result = CampaignCombatHistoryReplay.Replay(trace.Request, created, new MutatingList(events, () => created[0] ^= 1));
        Assert.Equal(trace.Created, result.History.Created);
        Assert.Equal(events.Length + 1, result.Projection.StateVersion);
    }

    private sealed class Sentry(int count) : IReadOnlyList<byte[]>
    {
        public int Count => count;
        public byte[] this[int index] => throw new InvalidOperationException("History was indexed before bounds rejection.");
        public IEnumerator<byte[]> GetEnumerator() => throw new InvalidOperationException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class MutatingList(byte[][] values, Action mutate) : IReadOnlyList<byte[]>
    {
        private bool mutated;
        public int Count
        {
            get
            {
                if (!mutated) { mutate(); mutated = true; }
                return values.Length;
            }
        }
        public byte[] this[int index] => values[index];
        public IEnumerator<byte[]> GetEnumerator() => ((IEnumerable<byte[]>)values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private const string ForeignHash = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private static void Reject(Trace trace, byte[][] events) => Assert.ThrowsAny<JsonException>(() => CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, events));
    private static string Hash(byte[] bytes) => CampaignOpeningPreambleCodec.Hash(bytes);
    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());
    private static JsonDocument Fixture(string name) => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", name)));
    private static JsonElement Row(JsonDocument fixture, string side, int moves) => fixture.RootElement.GetProperty("cases").EnumerateArray()
        .Single(row => row.GetProperty("actor").GetString() == side && row.GetProperty("moves").GetInt32() == moves).GetProperty("goldens");
    private static void Golden(JsonElement gold, int index, byte[] bytes)
    {
        Assert.Equal(gold.GetProperty("bytes")[index].GetInt32(), bytes.Length);
        Assert.Equal(gold.GetProperty("sha256")[index].GetString(), Hash(bytes));
    }

    private static IEnumerable<(CampaignCombatCreationRequest Request, byte[] Created, byte[][] Events)> Histories()
    {
        foreach (var history in CombatMovementHistoryReplayTests.SnapshotHistories())
        {
            if (history.Events.Length is not (20 or 21)) continue;
            using var tail = JsonDocument.Parse(history.Events[^1]);
            if (tail.RootElement.GetProperty("eventType").GetString() == "breakdown-segment-completed") yield return history;
        }
    }

    private static Trace Build((CampaignCombatCreationRequest Request, byte[] Created, byte[][] Events) history)
    {
        var selection = new List<byte[]>();
        foreach (var kind in new[] { "open-segment", "close-empty-selection" })
        {
            var state = CampaignCombatInheritedSelection.Replay(history.Request, history.Created, history.Events, selection);
            var result = CampaignCombatInheritedSelection.Apply(history.Request, history.Created, history.Events, selection,
                CampaignCombatInheritedSelection.CreateInput(state, kind));
            selection.Add(result.EventBytes);
        }
        var steps = new List<byte[]>();
        for (var index = 0; index < 6; index++)
        {
            var state = CampaignCombatInheritedNoAttack.Replay(history.Request, history.Created, history.Events, selection, steps);
            var result = CampaignCombatInheritedNoAttack.Apply(history.Request, history.Created, history.Events, selection, steps,
                CampaignCombatInheritedNoAttack.CreateInput(state));
            steps.Add(result.EventBytes);
        }
        return new(history.Request, history.Created, history.Events, selection.ToArray(), steps.ToArray());
    }
}
