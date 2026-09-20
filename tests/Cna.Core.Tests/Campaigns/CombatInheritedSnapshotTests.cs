using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatInheritedSnapshotTests
{
    [Fact]
    public void EverySelectedRootMatchesExactBytesAndRestoresWithFreshAdmissionDisabled()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Campaigns", "Fixtures", "combat-inherited-snapshot-v1.json")));
        var rows = fixture.RootElement.GetProperty("roots").EnumerateArray().ToArray();
        var expected = rows.GroupBy(Identity).ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.Ordinal);
        var histories = CombatHistoryReplayTests.SnapshotHistories()
            .Concat(CombatMovementHistoryReplayTests.SnapshotHistories())
            .Concat(CombatReactionHistoryReplayTests.SnapshotHistories())
            .DistinctBy(history => Identity(history.Created, history.Events)).ToArray();
        Assert.Equal(368, rows.Length);
        Assert.Equal(286, histories.Length);
        Assert.Equal(62, expected.Values.Count(group => group.Length > 1));
        Assert.Equal(expected.Keys.Order(StringComparer.Ordinal), histories.Select(h => Identity(h.Created, h.Events)).Order(StringComparer.Ordinal));
        var checkedRows = 0;
        foreach (var history in histories)
        {
            var bytes = CampaignCombatInheritedSnapshotV12Codec.Serialize(history.Request, history.Created, history.Events);
            foreach (var row in expected[Identity(history.Created, history.Events)])
            {
                Assert.Equal(Encoding.UTF8.GetBytes(row.GetProperty("canonicalJson").GetString()!), bytes);
                Assert.Equal(row.GetProperty("bytes").GetInt32(), bytes.Length);
                Assert.Equal(row.GetProperty("sha256").GetString(), Digest(bytes));
                checkedRows++;
            }
            var restored = CampaignCombatInheritedSnapshotV12Codec.Restore(bytes, history.Request, history.Created, history.Events, admissionEnabled: false);
            Assert.Equal(history.Created, restored.History.Created);
            Assert.Equal(history.Events.Select(Digest), restored.History.Events.Select(Digest));
            Assert.Equal(history.Events.Length + 1, restored.Projection.StateVersion);
            Assert.Equal(history.Events.Length, restored.Projection.Receipts.Count);
            if (history.Events.Length == 0)
                Assert.Equal(CampaignCreationSnapshotV12Codec.Serialize(CampaignCreationSnapshotV12.Create(history.Created, history.Request)), bytes);
            else
                Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(bytes, history.Created, history.Request));
        }
        Assert.Equal(368, checkedRows);
    }

    [Fact]
    public void RestoreRequiresRetainedCreationEvenWhenFreshAdmissionIsEnabled()
    {
        var history = CombatHistoryReplayTests.SnapshotHistories().First();
        var bytes = CampaignCombatInheritedSnapshotV12Codec.Serialize(history.Request, history.Created, history.Events);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatCreationCut.Decide(null, history.Request, admissionEnabled: false));
        var retained = CampaignCombatCreationCut.Decide(history.Created, history.Request, admissionEnabled: false);
        Assert.False(retained.RequiresPublication);
        Assert.Equal(history.Created, retained.CreatedBytes);
        foreach (var admission in new[] { false, true })
        {
            foreach (var missing in new byte[]?[] { null, [] })
                Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Restore(bytes, history.Request, missing, new InaccessibleHistory(), admission));
            Assert.Equal(1, CampaignCombatInheritedSnapshotV12Codec.Restore(bytes, history.Request, history.Created, [], admission).Projection.StateVersion);
        }
    }

    [Fact]
    public void NoncanonicalMalformedAndChangedRootsRejectInsteadOfBecomingAuthority()
    {
        var history = CombatReactionHistoryReplayTests.SnapshotHistories().First(h => h.Events.Length == 14);
        var bytes = CampaignCombatInheritedSnapshotV12Codec.Serialize(history.Request, history.Created, history.Events);
        var text = Encoding.UTF8.GetString(bytes);
        var raw = new[] { text + " ", "\uFEFF" + text, text.Replace("\"contractVersion\":12", "\"contractVersion\":12.0", StringComparison.Ordinal),
            text.Replace("\"campaignId\"", "\"camp\\u0061ignId\"", StringComparison.Ordinal),
            text.Replace("\"contractVersion\":12", "\"contractVersion\":12,\"contractVersion\":12", StringComparison.Ordinal), "null", "{}", "[" };
        foreach (var value in raw) Reject(Encoding.UTF8.GetBytes(value));
        var root = JsonNode.Parse(bytes)!.AsObject();
        // Every required root field must survive, including causal nulls and complete receipts.
        foreach (var field in root.Select(pair => pair.Key).ToArray())
        {
            var changed = root.DeepClone().AsObject();
            changed.Remove(field);
            Reject(Bytes(changed));
        }
        var reordered = new JsonObject();
        foreach (var pair in root.Reverse()) reordered.Add(pair.Key, pair.Value?.DeepClone());
        Reject(Bytes(reordered));
        foreach (var mutate in new Action<JsonObject>[] {
            node => node["unknown"] = true,
            node => node["stateVersion"] = 1,
            node => node["currentPosition"] = new JsonObject { ["kind"] = "sequence", ["sequencePosition"] = node["cycleState"]!["sequencePosition"]!.DeepClone() },
            node => node["reactionWindow"] = null,
            node => node["breakdownFlow"] = new JsonObject { ["kind"] = "idle" },
            node => node["cycleState"]!["tracks"] = new JsonArray(),
            node => node["cycleState"]!["actualProgressRefs"] = new JsonArray(),
            node => node["cycleState"]!["members"] = new JsonArray(),
            node => node["cycleState"]!["cycleId"] = "sha256:" + new string('0', 64),
            node => node["creationReceipt"]!["creationEventHash"] = Digest(bytes),
            node => node["commandReceipts"]!.AsArray().RemoveAt(0),
            node => node["combatState"] = new JsonObject(),
        })
        {
            var changed = root.DeepClone().AsObject();
            mutate(changed);
            Assert.NotEqual(bytes, Bytes(changed));
            Reject(Bytes(changed));
        }
        void Reject(byte[] candidate) => Assert.ThrowsAny<JsonException>(() =>
            CampaignCombatInheritedSnapshotV12Codec.Restore(candidate, history.Request, history.Created, history.Events, false));
    }

    [Fact]
    public void LawfulForeignForksAndShorterHeadsCannotAuthenticateAnotherRetainedHistory()
    {
        var cuts = CombatReactionHistoryReplayTests.SnapshotHistories().Where(h => h.Events.Length == 13).ToArray();
        var history = cuts[0];
        var foreign = cuts.First(h => h.Created.SequenceEqual(history.Created) && !h.Events[^1].SequenceEqual(history.Events[^1]));
        var bytes = CampaignCombatInheritedSnapshotV12Codec.Serialize(history.Request, history.Created, history.Events);
        var foreignBytes = CampaignCombatInheritedSnapshotV12Codec.Serialize(foreign.Request, foreign.Created, foreign.Events);
        Assert.Equal(history.Events.Take(12).Select(Digest), foreign.Events.Take(12).Select(Digest));
        Assert.NotEqual(Digest(bytes), Digest(foreignBytes));
        Reject(foreignBytes, history.Events);
        Reject(bytes, foreign.Events);
        var shorter = history.Events.Take(12).ToArray();
        Reject(CampaignCombatInheritedSnapshotV12Codec.Serialize(history.Request, history.Created, shorter), history.Events);
        Reject(bytes, shorter);
        Reject(bytes, history.Events.Where((_, i) => i != 10).ToArray());
        var reordered = history.Events.ToArray();
        (reordered[10], reordered[11]) = (reordered[11], reordered[10]);
        Reject(bytes, reordered);
        Reject(bytes, history.Events.Append(history.Events[^1]).ToArray());
        var tampered = history.Events.ToArray();
        tampered[^1] = [.. tampered[^1], (byte)' '];
        Reject(bytes, tampered);
        var other = CombatHistoryReplayTests.SnapshotHistories().First(h => !h.Created.SequenceEqual(history.Created));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Restore(bytes, other.Request, history.Created, history.Events, false));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Restore(bytes, history.Request, other.Created, history.Events, false));
        void Reject(byte[] candidate, byte[][] events) => Assert.ThrowsAny<JsonException>(() =>
            CampaignCombatInheritedSnapshotV12Codec.Restore(candidate, history.Request, history.Created, events, false));
    }

    [Fact]
    public void RehashedWeatherAndConsistentRootLedgerStillRejectForgedCausalRng()
    {
        var history = CombatHistoryReplayTests.SnapshotHistories().First(h => h.Events.Length == 5);
        var opening = history.Events.Take(4).ToArray();
        var weather = CampaignCombatWeather.Replay(history.Request, history.Created, opening, [history.Events[4]]);
        var accepted = weather.AcceptedEvent!;
        var forged = accepted with { RandomState = accepted.Before.RandomState };
        var forgedEvent = CampaignCombatWeatherCodec.SerializeEvent(forged);
        Assert.NotEqual(accepted.ReceiptId, forged.ReceiptId);
        Assert.NotEqual(Digest(history.Events[4]), Digest(forgedEvent));
        var root = JsonNode.Parse(CampaignCombatInheritedSnapshotV12Codec.Serialize(history.Request, history.Created, history.Events))!;
        var predecessor = JsonNode.Parse(CampaignCombatInheritedSnapshotV12Codec.Serialize(history.Request, history.Created, opening))!;
        root["randomState"] = predecessor["randomState"]!.DeepClone();
        root["chroniclePrefix"] = CampaignOpeningPreambleCodec.EventPrefix(predecessor["chroniclePrefix"]!.GetValue<string>(), forgedEvent);
        root["commandReceipts"]![4]!["eventHash"] = Digest(forgedEvent);
        root["commandReceipts"]![4]!["receiptId"] = forged.ReceiptId;
        var forgedHistory = opening.Append(forgedEvent).ToArray();
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Serialize(history.Request, history.Created, forgedHistory));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Restore(Bytes(root), history.Request, history.Created, forgedHistory, false));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Restore(Bytes(root), history.Request, history.Created, history.Events, false));
    }

    [Theory]
    [InlineData(12, "interruptContext")]
    [InlineData(14, "movementEnd")]
    [InlineData(15, "breakdownCompletionReceiptId")]
    public void ClosedCausalEvidenceCannotBeNormalizedAway(int cut, string field)
    {
        var history = CombatMovementHistoryReplayTests.SnapshotHistories().First(h => h.Events.Length == cut);
        var root = JsonNode.Parse(CampaignCombatInheritedSnapshotV12Codec.Serialize(history.Request, history.Created, history.Events))!;
        Assert.NotNull(root["cycleState"]![field]);
        root["cycleState"]![field] = null;
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Restore(Bytes(root), history.Request, history.Created, history.Events, false));
    }

    [Fact]
    public void ResolvedInactiveReactionWindowSurvivesUntilActualClosure()
    {
        var history = CombatReactionHistoryReplayTests.SnapshotHistories().First(h => h.Events.Length == 15);
        var root = JsonNode.Parse(CampaignCombatInheritedSnapshotV12Codec.Serialize(history.Request, history.Created, history.Events))!;
        Assert.NotNull(root["reactionWindow"]);
        Assert.Null(root["reactionWindow"]!["activeOpportunityId"]);
        Assert.NotEmpty(root["reactionWindow"]!["resolvedOpportunityIds"]!.AsArray());
        root["reactionWindow"] = null;
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Restore(Bytes(root), history.Request, history.Created, history.Events, false));
    }

    [Fact]
    public void RootBoundsRejectBeforeReadingRetainedHistoryAndHistoryBoundsRemainEnforced()
    {
        var history = CombatHistoryReplayTests.SnapshotHistories().First();
        var bytes = CampaignCombatInheritedSnapshotV12Codec.Serialize(history.Request, history.Created, history.Events);
        foreach (var invalid in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes(new string('[', 34) + "0" + new string(']', 34)),
            Encoding.UTF8.GetBytes("{\"other\":[" + string.Join(',', Enumerable.Repeat("0", 513)) + "]}"),
            Encoding.UTF8.GetBytes("{\"world.cohesionCauses\":[" + string.Join(',', Enumerable.Repeat("null", 513)) + "]}"),
            Encoding.UTF8.GetBytes("{\"\":{\"world\":{\"cohesionCauses\":[" + string.Join(',', Enumerable.Repeat("null", 513)) + "]}}}"),
            Encoding.UTF8.GetBytes("{\"world\":{\"cohesionCauses\":[" + string.Join(',', Enumerable.Repeat("0", 4097)) + "]}}") })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Restore(invalid, history.Request, history.Created, new InaccessibleHistory(), false));
        // The larger array allowance belongs only to World.cohesionCauses.
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Restore(
            Encoding.UTF8.GetBytes("{\"cohesionCauses\":[" + string.Join(',', Enumerable.Repeat("0", 513)) + "]}"),
            history.Request, history.Created, new InaccessibleHistory(), false));
        foreach (var count in new[] { -1, 513 })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Restore(bytes, history.Request, history.Created, new InaccessibleHistory(count), false));
        foreach (var invalid in new byte[]?[] { null, [], new byte[1_048_577] })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Restore(bytes, history.Request, history.Created, [invalid!], false));
        var max = new byte[1_048_576];
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Restore(bytes, history.Request, history.Created,
            Enumerable.Repeat(max, 16).Append(new byte[] { 1 }).ToArray(), false));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedSnapshotV12Codec.Restore(bytes, history.Request, new byte[1_048_577], [], false));
        // Exact structural limits reach the history sentry; bounds must not be narrowed accidentally.
        foreach (var withinBounds in new[] {
            Encoding.UTF8.GetBytes(new string('[', 32) + "0" + new string(']', 32)),
            Encoding.UTF8.GetBytes("{\"other\":[" + string.Join(',', Enumerable.Repeat("0", 512)) + "]}"),
            Encoding.UTF8.GetBytes("{\"world\":{\"cohesionCauses\":[" + string.Join(',', Enumerable.Repeat("0", 4096)) + "]}}"),
            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).PadRight(1_048_576)),
        })
            Assert.Throws<InvalidOperationException>(() => CampaignCombatInheritedSnapshotV12Codec.Restore(withinBounds, history.Request, history.Created, new InaccessibleHistory(), false));
    }

    [Fact]
    public void ExportedAndCallerBuffersCannotChangeRestoredHistoryOrLaterSerializedRoots()
    {
        var history = CombatReactionHistoryReplayTests.SnapshotHistories().First(h => h.Events.Length == 16);
        var bytes = CampaignCombatInheritedSnapshotV12Codec.Serialize(history.Request, history.Created, history.Events);
        var expected = bytes.ToArray();
        var restored = CampaignCombatInheritedSnapshotV12Codec.Restore(bytes, history.Request, history.Created, history.Events, false);
        foreach (var buffer in history.Events.Append(history.Created).Append(bytes).Concat(restored.History.Events).Append(restored.History.Created))
            Array.Fill(buffer, (byte)0);
        Assert.Equal(17, restored.Projection.StateVersion);
        Assert.Equal(16, restored.Projection.Receipts.Count);
        var output = CampaignCombatInheritedSnapshotV12Codec.Serialize(history.Request, restored.History.Created, restored.History.Events);
        Assert.Equal(expected, output);
        Array.Fill(output, (byte)0);
        Assert.Equal(expected, CampaignCombatInheritedSnapshotV12Codec.Serialize(history.Request, restored.History.Created, restored.History.Events));
    }

    private sealed class InaccessibleHistory(int? count = null) : IReadOnlyList<byte[]>
    {
        public int Count => count ?? throw new InvalidOperationException("History accessed before root or creation rejection.");
        public byte[] this[int index] => throw new InvalidOperationException("History indexed before count rejection.");
        public IEnumerator<byte[]> GetEnumerator() => throw new InvalidOperationException("History enumerated.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private static byte[] Bytes(JsonNode root) => Encoding.UTF8.GetBytes(root.ToJsonString());
    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
    private static string Identity(byte[] created, IEnumerable<byte[]> events) => Digest(created) + ":" + string.Join(',', events.Select(Digest));
    private static string Identity(JsonElement row) => row.GetProperty("creationHash").GetString() + ":" +
        string.Join(',', row.GetProperty("eventHashes").EnumerateArray().Select(value => value.GetString()));
}
