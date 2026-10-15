using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatActualMovedCycleControlTests;
using static Cna.Core.Tests.Campaigns.CombatActualMovedRepeatMovementCompletionTests;
using Codec = Cna.Core.Campaigns.CampaignCombatActualMovedRepeatMovementCompletionCodec;
using Domain = Cna.Core.Campaigns.CampaignCombatActualMovedRepeatMovementCompletion;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualMovedRepeatMovementCompletionContractTests
{
    internal static IEnumerable<object[]> RowsFor(string group)
    {
        using var fixture = JsonDocument.Parse(Dependency(FixturePath));
        var rows = fixture.RootElement.GetProperty("semanticRows").EnumerateArray().Where(r => r.GetProperty("group").GetString() == group).Select(r => new object[] { r.GetProperty("rootOrdinal").GetInt32(), r.GetProperty("movementCut").GetInt32(), r.GetProperty("profile").GetString()! }).ToArray();
        Assert.NotEmpty(rows); return rows;
    }
    internal static void VerifyTrace(int rootOrdinal, int movementCut, string profile)
    {
        var c = new CompletionContext(rootOrdinal, movementCut, profile); var current = c.Replay(); var initial = current.Control; var retries = 0;
        void CheckCut(int cut)
        {
            foreach (var (kind, bytes) in new[] { ("source", current.SourceBytes), ("control", current.ControlBytes), ("proof", current.ProofBytes) })
            {
                var expected = c.Case.GetProperty("cuts")[cut].GetProperty(kind);
                Assert.Equal(expected.GetProperty("bytes").GetInt32(), bytes.Length); Assert.Equal(expected.GetProperty("sha256").GetString(), Codec.Hash(bytes));
            }
        }
        CheckCut(0);
        for (var cut = 0; cut < c.Inputs.Length; cut++)
        {
            var input = c.Inputs[cut]; var kind = JsonNode.Parse(input)!["command"]!["kind"]!.GetValue<string>();
            Assert.Equal(input, Command(current.Control, kind)); current = Apply(c, input); Assert.False(current.Duplicate);
            Assert.Equal(Encoding.ASCII.GetBytes(c.Case.GetProperty("eventCanonicalUtf8")[cut].GetString()!), current.EventBytes);
            Retain(c, current, input); CheckCut(cut + 1);
            for (var prior = 0; prior <= cut; prior++)
            {
                var duplicate = Apply(c, c.Inputs[prior], admission: false); retries++;
                Assert.True(duplicate.Duplicate); Assert.Null(duplicate.EventBytes);
                Assert.Equal(JsonNode.Parse(c.Case.GetProperty("eventCanonicalUtf8")[prior].GetString()!)!["receiptId"]!.GetValue<string>(), duplicate.ReceiptId);
                Assert.Equal(current.SourceBytes, duplicate.SourceBytes); Assert.Equal(current.ControlBytes, duplicate.ControlBytes); Assert.Equal(current.ProofBytes, duplicate.ProofBytes);
                Assert.Equal(c.Completion.Count, duplicate.CompletionInputs.Count);
                for (var i = 0; i < c.Completion.Count; i++) Assert.Equal(c.Completion[i], duplicate.CompletionInputs[i]);
            }
        }
        Assert.Equal(c.Inputs.Length * (c.Inputs.Length + 1) / 2, retries);
        Assert.Equal(movementCut == 1 ? 1 : 3, c.Inputs.Length); Assert.True(current.Control.GetProperty("closed").GetBoolean());
        Assert.Equal("breakdown", current.Control.GetProperty("phase").GetString()); Assert.Equal(2, current.Control.GetProperty("movementEnd").GetProperty("ordinal").GetInt32());
        foreach (var field in RetainedFields) Assert.Equal(initial.GetProperty(field).GetRawText(), current.Control.GetProperty(field).GetRawText());
    }
    private static readonly string[] Kinds = ["stop-element-movement", "resolve-breakdown-stop", "complete-movement-segment"];
    private static readonly string[] RetainedFields = ["world", "randomState", "originalMovementEnd", "priorCycleProgress", "actualProgressRefs", "members", "attackHistory", "priorCycleTargetUses", "targetUses", "assessment", "pendingRepeatIntent", "pendingIntentReceiptId", "publicationReceiptId", "originalCycle", "activeCycle", "closure", "tracks", "movementReceipts"];
    [Theory]
    [InlineData(0, 1, "paid")]
    [InlineData(0, 3, "paid")]
    [InlineData(12, 1, "armed")]
    [InlineData(12, 6, "armed")]
    public void EveryCutContextActorCapabilityAndOrderReject(int rootOrdinal, int movementCut, string profile)
    {
        var checkedFaults = 0;
        for (var cut = 0; cut < (movementCut == 1 ? 1 : 3); cut++)
        {
            var c = new CompletionContext(rootOrdinal, movementCut, profile); AtCut(c, cut); var current = c.Replay(); var input = JsonNode.Parse(c.Inputs[cut])!;
            foreach (var (field, value, code) in new (string, JsonNode?, int)[] { ("baseHash", JsonValue.Create("sha256:" + new string('0', 64)), 4), ("cycleId", JsonValue.Create("sha256:" + new string('0', 64)), 4), ("expectedPositionId", JsonValue.Create("foreign"), 4), ("expectedPriorVersion", JsonValue.Create(0), 6), ("capabilityId", JsonValue.Create("sha256:" + new string('0', 64)), 5) })
            { var bad = input.DeepClone(); bad["command"]![field] = value; Code(code, () => Apply(c, Wire(bad))); checkedFaults++; }
            var actor = input.DeepClone(); actor["actor"] = input["actor"]!.GetValue<string>() == "system" ? current.Control.GetProperty("activeCycle").GetProperty("actingSide").GetString() : "system";
            Code(4, () => Apply(c, Wire(actor))); checkedFaults++; Code(7, () => Apply(c, c.Inputs[cut], admission: false)); checkedFaults++;
            foreach (var kind in Kinds.Where(k => k != input["command"]!["kind"]!.GetValue<string>()))
            { Assert.ThrowsAny<JsonException>(() => Apply(c, Command(current.Control, kind))); checkedFaults++; }
        }
        Assert.Equal((movementCut == 1 ? 1 : 3) * 9, checkedFaults);
    }
    [Theory]
    [InlineData(0, 1, "paid")]
    [InlineData(0, 3, "paid")]
    [InlineData(12, 1, "armed")]
    [InlineData(12, 6, "armed")]
    public void SourceControlProofReadersRejectForgedClaims(int rootOrdinal, int movementCut, string profile)
    {
        var c = new CompletionContext(rootOrdinal, movementCut, profile); AtCut(c, c.Inputs.Length); var current = c.Replay(); var l = Ledgers(c);
        Assert.Equal(current.SourceBytes, Domain.ReadSource(c.Source, c.Parent.Ancestor.Request, l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], l[9], Dependency));
        Assert.Equal(current.ControlBytes, Claim(c, current.ControlBytes, false).ControlBytes); Assert.Equal(current.ProofBytes, Claim(c, current.ProofBytes, true).ProofBytes);
        var faults = 0;
        foreach (var proof in new[] { false, true })
        foreach (var field in new[] { "baseHash", "movementEnd", "originalMovementEnd" })
        {
            var bad = JsonNode.Parse(proof ? current.ProofBytes : current.ControlBytes)!; var control = proof ? bad["control"]! : bad;
            if (field == "baseHash") control[field] = "sha256:" + new string('0', 64); else control[field]!["ordinal"] = field == "movementEnd" ? 1 : 2;
            Code(6, () => Claim(c, Codec.Canonical(bad, proof ? "Proof" : "Control"), proof)); faults++;
        }
        Assert.Equal(6, faults);
    }
    [Theory]
    [InlineData(0, 1, "paid")]
    [InlineData(0, 3, "paid")]
    [InlineData(12, 1, "armed")]
    [InlineData(12, 6, "armed")]
    public void CatalogFullPositionsAndBothProofsStayDistinct(int rootOrdinal, int movementCut, string profile)
    {
        var c = new CompletionContext(rootOrdinal, movementCut, profile); var initial = c.Replay().Control;
        using var fixture = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-cycle-sequence-v1.json"));
        var catalog = JsonNode.Parse(fixture.RootElement.GetProperty("predecessor").GetProperty("canonicalUtf8").GetString()!)!;
        JsonNode Position(string id) { var p = catalog["positions"]!.AsArray().Concat(catalog["interruptPositions"]!.AsArray()).Single(p => p!["positionId"]!.GetValue<string>() == id)!.DeepClone(); p["contractVersion"] = 5; return p; }
        if (movementCut > 1)
        {
            AtCut(c, 1); var stopped = c.Replay().Control; var stop = stopped.GetProperty("flow").GetProperty("stop"); var context = stopped.GetProperty("interruptContext");
            Assert.Equal(Encoding.ASCII.GetString(Codec.Canonical(Position("land.position.breakdown-stop"), "Position")), stopped.GetProperty("sequencePosition").GetRawText());
            Assert.Equal(initial.GetProperty("activeCycle").GetRawText(), context.GetProperty("cycle").GetRawText()); Assert.Equal(initial.GetProperty("sequencePosition").GetRawText(), context.GetProperty("suspendedPosition").GetRawText());
            Assert.Equal(initial.GetProperty("tracks")[0].GetRawText(), stop.GetProperty("track").GetRawText());
            Assert.Equal(initial.GetProperty("movementReceipts")[initial.GetProperty("movementReceipts").GetArrayLength() - 1].GetProperty("receiptId").GetString(), stop.GetProperty("lastMoveReceiptId").GetString());
            Assert.NotEqual(initial.GetProperty("publicationReceiptId").GetString(), stop.GetProperty("lastMoveReceiptId").GetString());
            Assert.Equal(stopped.GetProperty("stateVersion").GetInt64(), stop.GetProperty("recordedStateVersion").GetInt64());
            AtCut(c, 2); var resolved = c.Replay().Control; Assert.Equal(initial.GetProperty("sequencePosition").GetRawText(), resolved.GetProperty("sequencePosition").GetRawText()); Assert.Equal(JsonValueKind.Null, resolved.GetProperty("interruptContext").ValueKind);
            var effect = JsonNode.Parse(c.Case.GetProperty("eventCanonicalUtf8")[1].GetString()!)!["effect"]!; Assert.Empty(effect["checks"]!.AsArray()); Assert.Empty(effect["createdLots"]!.AsArray()); Assert.True(JsonNode.DeepEquals(effect["beforeRandomState"], effect["afterRandomState"]));
        }
        else { Assert.Empty(initial.GetProperty("tracks").EnumerateArray()); Assert.Empty(initial.GetProperty("actualProgressRefs").EnumerateArray()); Assert.Single(initial.GetProperty("movementReceipts").EnumerateArray()); }
        AtCut(c, c.Inputs.Length); var done = c.Replay().Control;
        Assert.Equal(Encoding.ASCII.GetString(Codec.Canonical(Position("land.position.operation-1.first-player.movement-and-combat.breakdown-determination"), "Position")), done.GetProperty("sequencePosition").GetRawText());
        Assert.Equal(initial.GetProperty("originalMovementEnd").GetRawText(), done.GetProperty("originalMovementEnd").GetRawText()); Assert.Equal(1, done.GetProperty("originalMovementEnd").GetProperty("ordinal").GetInt32()); Assert.Equal(2, done.GetProperty("movementEnd").GetProperty("ordinal").GetInt32());
        Assert.Equal(done.GetProperty("lifecycleReceipts")[done.GetProperty("lifecycleReceipts").GetArrayLength() - 1].GetProperty("receiptId").GetString(), done.GetProperty("movementEnd").GetProperty("completionReceiptId").GetString()); Assert.Equal(1, done.GetProperty("closure").GetProperty("ordinal").GetInt32()); Assert.Equal("repeated", done.GetProperty("closure").GetProperty("outcome").GetString()); Assert.Equal(2, done.GetProperty("activeCycle").GetProperty("ordinal").GetInt32());
        foreach (var field in RetainedFields) Assert.Equal(initial.GetProperty(field).GetRawText(), done.GetProperty(field).GetRawText());
    }
    [Theory]
    [InlineData(0, 3, "paid")]
    [InlineData(12, 6, "armed")]
    public void TenIndependentLedgersAndResignedEventsCannotAuthenticate(int rootOrdinal, int movementCut, string profile)
    {
        var c = new CompletionContext(rootOrdinal, movementCut, profile); AtCut(c, c.Inputs.Length); _ = c.Replay(); var original = Ledgers(c); var faults = 0;
        for (var index = 0; index < 10; index++)
        {
            var variants = original[index].Length == 0 ? new byte[][]?[] { null, [Encoding.ASCII.GetBytes("{}")], [Encoding.ASCII.GetBytes("[]")] } : [original[index][..^1], [.. original[index], original[index][0]], [Encoding.ASCII.GetBytes("{}"), .. original[index].Skip(1)]];
            foreach (var variant in variants)
            {
                var bad = original.Select(l => l.Select(b => b.ToArray()).ToArray()).ToArray(); bad[index] = variant!;
                Assert.ThrowsAny<JsonException>(() => ReplayLedgers(c, bad)); faults++;
            }
        }
        Assert.Equal(30, faults); var source = c.Source.ToArray();
        for (var index = 0; index < 3; index++)
        {
            var packet = JsonNode.Parse(source)!; var ev = JsonNode.Parse(packet["lifecycleEventCanonicalUtf8"]![index]!.GetValue<string>())!;
            ev["stateVersion"] = ev["stateVersion"]!.GetValue<long>() + 1; ev.AsObject().Remove("receiptId");
            ev["receiptId"] = "mrmc." + Codec.HashWithDomain("sandtable.combat.actual-moved-repeat-movement-completion.receipt.v1", Codec.Encode(ev))[7..];
            packet["lifecycleEventCanonicalUtf8"]![index] = Encoding.ASCII.GetString(Codec.Canonical(ev, "Event")); c.Source = Codec.Canonical(packet, "Source"); Code(6, () => c.Replay());
        }
    }
    [Theory]
    [InlineData(0, 3, "paid")]
    [InlineData(12, 6, "armed")]
    public void OwnedBeforeCallbackAndAllReturnedBuffers(int rootOrdinal, int movementCut, string profile)
    {
        var c = new CompletionContext(rootOrdinal, movementCut, profile); AtCut(c, 1); var input = c.Inputs[1].ToArray(); var expected = Apply(c, input);
        var first = true; byte[]? previousPin = null;
        var result = Apply(c, input, path =>
        {
            if (first)
            {
                first = false; c.Source[0] = 0; input[0] = 0;
                foreach (var ledger in Ledgers(c)) foreach (var bytes in ledger) bytes[0] = 0;
                c.Parent.Movement.Clear(); c.Completion.Clear();
            }
            if (previousPin is not null) previousPin[0] ^= 1; previousPin = Dependency(path); return previousPin;
        });
        Assert.Equal(expected.SourceBytes, result.SourceBytes); Assert.Equal(expected.ControlBytes, result.ControlBytes); Assert.Equal(expected.ProofBytes, result.ProofBytes); Assert.Equal(expected.EventBytes, result.EventBytes);
        IReadOnlyList<byte[]>[] Returned(CombatActualMovedRepeatMovementCompletionResult r) => [r.PrefixInputs, r.LifecycleInputs, r.BreakdownInputs, r.SelectionInputs, r.RoundInputs, r.PaidInputs, r.ReleaseInputs, r.ControlInputs, r.MovementInputs, r.CompletionInputs];
        var returned = Returned(result); var retained = Returned(expected);
        for (var i = 0; i < 10; i++) { Assert.Equal(retained[i].Count, returned[i].Count); for (var j = 0; j < returned[i].Count; j++) { Assert.Equal(retained[i][j], returned[i][j]); returned[i][j][0] = 0; } }
        result.SourceBytes[0] = 0; result.ControlBytes[0] = 0; result.ProofBytes[0] = 0; result.EventBytes![0] = 0;
        Assert.Equal(expected.SourceBytes, result.SourceBytes); Assert.Equal(expected.ControlBytes, result.ControlBytes); Assert.Equal(expected.ProofBytes, result.ProofBytes); Assert.Equal(expected.EventBytes, result.EventBytes);
        var again = Returned(result); for (var i = 0; i < 10; i++) for (var j = 0; j < again[i].Count; j++) Assert.Equal(retained[i][j], again[i][j]);
        foreach (var proof in new[] { false, true })
        {
            c = new CompletionContext(rootOrdinal, movementCut, profile); AtCut(c, c.Inputs.Length); var current = c.Replay(); var claim = proof ? current.ProofBytes : current.ControlBytes; var copy = claim.ToArray(); first = true;
            var read = Claim(c, claim, proof, path => { if (first) { first = false; claim[0] = 0; } return Dependency(path); });
            Assert.Equal(copy, proof ? read.ProofBytes : read.ControlBytes);
        }
    }
    [Fact]
    public void AllPhysicalPinsRemainRequiredAfterWarmReplay()
    {
        var c = new CompletionContext(0, 1, "paid"); _ = c.Replay();
        using var schema = JsonDocument.Parse(Dependency("docs/specs/combat-actual-moved-repeat-movement-completion-v1.schema.json"));
        var paths = schema.RootElement.GetProperty("runtimeSourceHashes").EnumerateObject().Select(p => p.Name).Concat(["docs/specs/combat-actual-moved-repeat-movement-completion-v1.schema.json", "docs/specs/verify-combat-actual-moved-repeat-movement-completion-v1.py"]).ToArray(); Assert.Equal(106, paths.Length); var faults = 0;
        foreach (var target in paths)
        foreach (var fault in new[] { "missing", "corrupt", "null", "oversize", "suffix" })
        {
            Code(9, () => c.Replay(path =>
            {
                if (path != target) return Dependency(path);
                return fault switch { "missing" => throw new IOException("missing retained pin"), "corrupt" => Encoding.ASCII.GetBytes("changed"), "null" => null!, "oversize" => new byte[8388609], _ => [.. Dependency(path), 10] };
            })); faults++;
        }
        Assert.Equal(530, faults);
    }
    [Fact]
    public void EveryHandlerAndCapacityRequired()
    {
        using var schema = JsonDocument.Parse(Dependency("docs/specs/combat-actual-moved-repeat-movement-completion-v1.schema.json")); var handlers = schema.RootElement.GetProperty("supportHandlers").EnumerateArray().Select(h => h.GetString()!).ToHashSet(StringComparer.Ordinal); var capacities = schema.RootElement.GetProperty("limits").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetInt32(), StringComparer.Ordinal);
        Assert.Equal(14, handlers.Count); Assert.Equal(25, capacities.Count); var faults = 0;
        foreach (var (ordinal, cut, profile) in FourRoots)
        {
            var c = new CompletionContext(ordinal, cut, profile);
            foreach (var handler in handlers) { var missing = handlers.Where(h => h != handler).ToHashSet(StringComparer.Ordinal); Code(7, () => Certify(c, missing, capacities)); faults++; }
            foreach (var key in capacities.Keys) { var smaller = new Dictionary<string, int>(capacities, StringComparer.Ordinal); smaller[key]--; Code(7, () => Certify(c, handlers, smaller)); faults++; }
        }
        Assert.Equal(156, faults);
    }
    private static readonly (int Ordinal, int Cut, string Profile)[] FourRoots = [(0, 1, "paid"), (0, 3, "paid"), (12, 1, "armed"), (12, 6, "armed")];
    [Fact]
    public void WholeTraceCapacityAndHistoricalExclusionAreBounded()
    {
        var boundaries = 0; var content = JsonNode.Parse(Dependency("docs/specs/fixtures/combat-content-v7.canonical.json"))!.AsObject();
        foreach (var (ordinal, cut, profile) in FourRoots)
        {
            var c = new CompletionContext(ordinal, cut, profile); var initial = c.Replay().Control; var count = cut - 1; var events = count == 0 ? 1 : 3;
            Domain.EnsureTraceCapacity(long.MaxValue - events, count); Code(7, () => Domain.EnsureTraceCapacity(long.MaxValue - events + 1, count)); boundaries++;
            // Isolated boundary probe only. This modified state never enters Source or a reader.
            var isolated = JsonNode.Parse(initial.GetRawText())!.AsObject(); var unit = isolated["members"]![0]!["unit"]!.DeepClone(); isolated["originalMovementEnd"]!["excludedBefore"] = new JsonArray(unit.DeepClone());
            var proof = Domain.EndProof(content, isolated, "isolated-union-probe"); Assert.Contains(proof["excludedBefore"]!.AsArray(), u => JsonNode.DeepEquals(u, unit)); Assert.Equal(2, proof["ordinal"]!.GetValue<int>()); Assert.Equal(1, isolated["originalMovementEnd"]!["ordinal"]!.GetValue<int>()); boundaries++;
        }
        Assert.Equal(8, boundaries);
    }
    [Fact]
    public void ClosedGrammarAndSizeFraming()
    {
        var c = new CompletionContext(12, 6, "armed"); AtCut(c, c.Inputs.Length); var current = c.Replay(); var checks = 0;
        foreach (var (kind, value) in new[] { ("Source", JsonNode.Parse(c.Source)!), ("Control", JsonNode.Parse(current.ControlBytes)!), ("Input", JsonNode.Parse(c.Inputs[0])!), ("Proof", JsonNode.Parse(current.ProofBytes)!) })
        {
            var good = Codec.Canonical(value, kind);
            foreach (var bad in new byte[][] { [.. good, 10], [32, .. good], [0xef, 0xbb, 0xbf, .. good], Encoding.ASCII.GetBytes("{\"unknown\":null," + Encoding.ASCII.GetString(good)[1..]) })
            { Assert.ThrowsAny<JsonException>(() => Codec.Object(bad, kind)); checks++; }
            foreach (var field in value.AsObject().Select(p => p.Key))
            { var bad = value.DeepClone().AsObject(); bad.Remove(field); Assert.ThrowsAny<JsonException>(() => Codec.Object(Codec.Encode(bad), kind)); checks++; }
            Assert.ThrowsAny<JsonException>(() => Codec.Object(new byte[Codec.ByteLimit(kind) + 1], kind)); checks++;
        }
        foreach (var bad in new[] { "{\"contractVersion\":1,\"contractVersion\":1}", new string('[', 42) + "0" + new string(']', 42), "[" + string.Join(",", Enumerable.Repeat("0", 513)) + "]", "{" + string.Join(",", Enumerable.Range(0, 129).Select(i => "\"k" + i + "\":0")) + "}" })
        { Assert.ThrowsAny<JsonException>(() => Codec.Object(Encoding.ASCII.GetBytes(bad), "Source")); checks++; }
        foreach (var (field, value) in new (string, JsonNode)[] { ("contractVersion", JsonValue.Create(true)), ("contractVersion", JsonValue.Create(2)), ("expectedPriorVersion", JsonValue.Create(true)), ("expectedPriorVersion", JsonValue.Create(-1)), ("expectedPriorVersion", JsonValue.Create(9223372036854775808UL)), ("kind", JsonValue.Create("foreign")) })
        { var bad = JsonNode.Parse(c.Inputs[0])!; bad["command"]![field] = value; Assert.ThrowsAny<JsonException>(() => Codec.Object(Codec.Encode(bad), "Input")); checks++; }
        var forged = new CompletionContext(12, 6, "armed"); var packet = JsonNode.Parse(forged.Source)!; packet["movementSourceCanonicalUtf8"] = "{}"; forged.Source = Codec.Canonical(packet, "Source");
        Assert.ThrowsAny<JsonException>(() => Claim(forged, Encoding.ASCII.GetBytes("{}"), false)); checks++;
        for (var index = 0; index < 9; index++) { var bad = Ledgers(c); bad[index] = null!; Assert.ThrowsAny<JsonException>(() => ReplayLedgers(c, bad)); checks++; }
        var returned = Claim(c, current.ProofBytes, true).ProofBytes; Array.Clear(returned); Assert.Equal(current.ProofBytes, Claim(c, current.ProofBytes, true).ProofBytes); checks++;
        var framedSource = SourceFraming(); forged.Source = framedSource; Assert.ThrowsAny<JsonException>(() => forged.Replay()); checks++;
        var world = JsonNode.Parse(current.Control.GetProperty("world").GetRawText())!; var e = world["elements"]![0]!;
        var references = new JsonArray(Enumerable.Range(0, 512).Select(_ => (JsonNode?)new JsonObject { ["sourceId"] = new string('x', 128), ["locator"] = new string('x', 128) }).ToArray());
        foreach (var origin in new[] { e["readiness"]!["initialReadinessOrigin"]!, e["ammunition"]!["initialAmmunitionOrigin"]!, e["operationalState"]!["initialLedgerOrigin"]!, e["components"]![0]!["initialToeOrigin"]! }) origin["references"] = references.DeepClone();
        var frame = Codec.Canonical(world, "World"); Assert.Equal(587212, frame.Length); Assert.InRange(frame.Length, 524289, 1048576); Assert.Equal(frame, Codec.Canonical(Codec.Object(frame, "World"), "World"));
        var claim = JsonNode.Parse(current.ControlBytes)!; claim["world"] = world; Code(6, () => Claim(c, Codec.Canonical(claim, "Control"), false)); checks++;
        Assert.Equal(88, checks); Assert.Equal(5, current.Control.GetProperty("actualProgressRefs").GetArrayLength()); Assert.Equal(current.SourceBytes, c.Replay().SourceBytes);
        foreach (var proof in new[] { false, true }) Code(9, () => Claim(c, Encoding.ASCII.GetBytes("{}"), proof, _ => throw new IOException("pins precede claim parse")));
        var tooLongTrack = JsonNode.Parse(current.Control.GetProperty("tracks")[0].GetRawText())!; tooLongTrack["route"]!.AsArray().Add("foreign"); Code(1, () => Codec.Canonical(tooLongTrack, "Track"));
    }
    private static byte[] SourceFraming()
    {
        // Maximal escaping is framing only, never an invented authenticated parent.
        var packet = new JsonObject { ["contractVersion"] = 1, ["movementSourceCanonicalUtf8"] = new string('"', 64231936), ["lifecycleEventCanonicalUtf8"] = new JsonArray(Enumerable.Range(0, 3).Select(_ => (JsonNode?)JsonValue.Create(new string('"', 65536))).ToArray()) };
        var bytes = Codec.Canonical(packet, "Source"); Codec.SourceBounds(packet); Assert.Equal(128857183, bytes.Length); Assert.InRange(bytes.Length, 1, 128857600); return bytes;
    }
    [Fact]
    public void FullSourceFramingCannotForgeParent()
    {
        var c = new CompletionContext(12, 6, "armed"); c.Source = SourceFraming(); Assert.ThrowsAny<JsonException>(() => c.Replay());
        var packet = new JsonObject { ["contractVersion"] = 1, ["movementSourceCanonicalUtf8"] = "{}", ["lifecycleEventCanonicalUtf8"] = new JsonArray("{}", "{}", "{}", "{}") }; Code(1, () => Codec.SourceBounds(packet));
        Code(1, () => Codec.Copy(new byte[134217729], "Source")); Code(1, () => Codec.Copy(new byte[4097], "Input")); Code(1, () => Codec.Copy(new byte[65537], "Event"));
    }
}

public sealed class CombatActualMovedRepeatMovementCompletionArmedAxisMove0ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("armed-axis-m0");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionArmedAxisMove1ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("armed-axis-m1");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionArmedAxisMove2ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("armed-axis-m2");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionArmedAxisMove3ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("armed-axis-m3");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionArmedAxisMove4ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("armed-axis-m4");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionArmedAxisMove5ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("armed-axis-m5");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionArmedCommonwealthMove0ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("armed-commonwealth-m0");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionArmedCommonwealthMove1ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("armed-commonwealth-m1");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionArmedCommonwealthMove2ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("armed-commonwealth-m2");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionArmedCommonwealthMove3ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("armed-commonwealth-m3");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionArmedCommonwealthMove4ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("armed-commonwealth-m4");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionArmedCommonwealthMove5ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("armed-commonwealth-m5");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionPaidAxisMove0ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("paid-axis-m0");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionPaidAxisMove1ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("paid-axis-m1");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionPaidAxisMove2ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("paid-axis-m2");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionPaidCommonwealthMove0ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("paid-commonwealth-m0");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionPaidCommonwealthMove1ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("paid-commonwealth-m1");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}

public sealed class CombatActualMovedRepeatMovementCompletionPaidCommonwealthMove2ContractTests
{
    public static IEnumerable<object[]> Rows() => CombatActualMovedRepeatMovementCompletionContractTests.RowsFor("paid-commonwealth-m2");
    [Theory, MemberData(nameof(Rows))]
    public void CompleteTraceEveryCutAndAllPriorRetry(int rootOrdinal, int movementCut, string profile) => CombatActualMovedRepeatMovementCompletionContractTests.VerifyTrace(rootOrdinal, movementCut, profile);
}
