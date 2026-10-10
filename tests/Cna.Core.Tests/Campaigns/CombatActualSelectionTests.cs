using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualSelectionTests
{
    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void ActualOpeningCreatesPendingSelectionAtFourteen(string owner)
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-actual-selection-v1.json")));
        var row = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("owner").GetString() == owner && c.GetProperty("variant").GetString() == "selected");
        var packet = JsonNode.Parse(row.GetProperty("source").GetProperty("canonicalUtf8").GetString()!)!;
        packet["selectionEventCanonicalUtf8"] = new JsonArray();
        var source = CampaignCombatPositiveEntryCodec.Encode(packet);
        var input = Encoding.ASCII.GetBytes(row.GetProperty("trustedInputs")[0].GetProperty("canonicalUtf8").GetString()!);
        var result = CampaignCombatActualSelection.ApplyTrustedSource(source, Context(), [], input, Dependency);
        Assert.Equal(14, result.Control.GetProperty("stateVersion").GetInt64());
        Assert.Equal("pending", result.Control.GetProperty("selectionOutcome").GetString());
    }
    public static IEnumerable<object[]> Cases()
    {
        foreach (var owner in new[] { "axis", "commonwealth" })
            foreach (var variant in new[] { "selected", "opening-unavailable", "finish", "selection-expired", "selection-unavailable", "rba-before-open-unavailable", "rba-expired", "rba-unavailable" }) yield return [owner, variant];
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryFrozenCutSuffixRetryAndProofMatchesOriginalBytes(string owner, string variant)
    {
        using var fixture = Fixture(); var row = Row(fixture, owner, variant); var context = Context();
        var packet = JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject();
        var inputs = row.GetProperty("trustedInputs").EnumerateArray().Select(Artifact).ToArray();
        var events = row.GetProperty("events").EnumerateArray().Select(Artifact).ToArray();
        string? world = null, random = null;
        for (var cut = 0; cut <= events.Length; cut++)
        {
            var bytes = Cut(packet, cut); var ledger = inputs.Take(cut).ToArray();
            Assert.Equal(bytes, CampaignCombatActualSelectionCodec.ReadSource(bytes, context, ledger, Dependency));
            var result = CampaignCombatActualSelection.ReplayTrustedSource(bytes, context, ledger, Dependency);
            Assert.Equal(Artifact(row.GetProperty("controls")[cut]), result.ControlBytes);
            Assert.Equal(Artifact(row.GetProperty("proofs")[cut]), result.ProofBytes);
            Assert.Equal(result.ProofBytes, CampaignCombatActualSelectionCodec.ReadProof(result.ProofBytes, bytes, context, ledger, Dependency).ProofBytes);
            Assert.Equal(result.ControlBytes, CampaignCombatActualSelectionCodec.ReadControl(result.ControlBytes, bytes, context, ledger, Dependency).ControlBytes);
            var boundary = result.Proof.GetProperty("boundary"); var entry = result.Proof.GetProperty("positiveEntryProof").GetProperty("entry");
            world ??= boundary.GetProperty("world").GetRawText(); random ??= boundary.GetProperty("randomState").GetRawText();
            Assert.Equal(world, boundary.GetProperty("world").GetRawText()); Assert.Equal(random, boundary.GetProperty("randomState").GetRawText());
            Assert.Equal(12, entry.GetProperty("receipts").GetArrayLength()); Assert.Equal(JsonValueKind.Null, boundary.GetProperty("position").GetProperty("activeSide").ValueKind);
            Assert.Equal(13 + cut, result.Control.GetProperty("stateVersion").GetInt64());
            if (cut < events.Length)
            {
                var next = CampaignCombatActualSelection.ApplyTrustedSource(bytes, context, ledger, inputs[cut], Dependency);
                Assert.Equal(events[cut], next.EventBytes); Assert.False(next.Duplicate);
                Assert.Equal(Artifact(row.GetProperty("controls")[cut + 1]), next.ControlBytes);
                Assert.Equal(Artifact(row.GetProperty("proofs")[cut + 1]), next.ProofBytes);
            }
            for (var index = 0; index < cut; index++)
                foreach (var retryMode in Enumerable.Range(0, 4))
                {
                    var retryInput = JsonNode.Parse(inputs[index])!;
                    if (retryMode == 1) { retryInput["admittedAt"] = null; retryInput["clockAvailable"] = false; }
                    if (retryMode == 2) { retryInput["admittedAt"] = 999999; retryInput["clockAvailable"] = true; }
                    if (retryMode == 3) retryInput["clockAvailable"] = !retryInput["clockAvailable"]!.GetValue<bool>();
                    var retry = CampaignCombatActualSelection.ApplyTrustedSource(bytes, context, ledger, Encode(retryInput), Dependency);
                    Assert.True(retry.Duplicate); Assert.Equal(events[index], retry.EventBytes); Assert.Equal(result.ControlBytes, retry.ControlBytes); Assert.Equal(result.ProofBytes, retry.ProofBytes);
                }
        }
        var terminal = CampaignCombatActualSelection.ReplayTrustedSource(Cut(packet, events.Length), context, inputs, Dependency).Control;
        Assert.Equal(variant == "selected" ? 3 : 6, terminal.GetProperty("stepIndex").GetInt32()); Assert.Equal(variant != "selected", terminal.GetProperty("closed").GetBoolean());
        if (variant == "selected") { Assert.Equal(20, terminal.GetProperty("stateVersion").GetInt64()); Assert.NotEqual(JsonValueKind.Null, terminal.GetProperty("declineReceiptId").ValueKind); }
        else { Assert.Equal(JsonValueKind.Null, terminal.GetProperty("declineReceiptId").ValueKind); Assert.Equal(6, terminal.GetProperty("stepReceipts").GetArrayLength()); }
    }
    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void TrustedLedgerAndOwnedBuffersCannotBeReplacedByEventClaims(string owner)
    {
        using var fixture = Fixture(); var row = Row(fixture, owner, "selected"); var context = Context(); var packet = JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject();
        var ledger = row.GetProperty("trustedInputs").EnumerateArray().Select(Artifact).ToArray(); var original = Encode(packet);
        var result = CampaignCombatActualSelection.ReplayTrustedSource(original, context, ledger, Dependency); var expected = result.ProofBytes;
        Array.Fill(original, (byte)0); var returned = result.ProofBytes; Array.Fill(returned, (byte)0); var control = result.ControlBytes; Array.Fill(control, (byte)0); Assert.Equal(expected, result.ProofBytes);
        foreach (var index in Enumerable.Range(0, ledger.Length))
        {
            var input = JsonNode.Parse(ledger[index])!; input["admittedAt"] = index == 1 ? 1101 : 999;
            var changed = ledger.Select(b => b.ToArray()).ToArray(); changed[index] = Encode(input);
            Assert.Throws<JsonException>(() => CampaignCombatActualSelection.ReplayTrustedSource(Encode(packet), context, changed, Dependency));
            var forged = packet.DeepClone(); var e = JsonNode.Parse(forged["selectionEventCanonicalUtf8"]![index]!.GetValue<string>())!;
            e["input"]!["actor"] = "system"; e["input"]!["admittedAt"] = 999; Resign(e); forged["selectionEventCanonicalUtf8"]![index] = Encoding.ASCII.GetString(Encode(e));
            Reject(() => CampaignCombatActualSelection.ReplayTrustedSource(Encode(forged), context, ledger, Dependency), 6);
        }
        Reject(() => CampaignCombatActualSelection.ReplayTrustedSource(Encode(packet), context, ledger[..^1], Dependency), 1);
        var backwards = packet.DeepClone(); backwards["selectionEventCanonicalUtf8"] = new JsonArray(backwards["selectionEventCanonicalUtf8"]!.AsArray().Reverse().Select(n => n!.DeepClone()).ToArray());
        Reject(() => CampaignCombatActualSelection.ReplayTrustedSource(Encode(backwards), context, ledger, Dependency), 6);
        var reversed = JsonNode.Parse(result.ControlBytes)!; reversed["stepReceipts"] = new JsonArray(reversed["stepReceipts"]!.AsArray().Reverse().Select(n => n!.DeepClone()).ToArray());
        Reject(() => CampaignCombatActualSelectionCodec.ReadControl(Encode(reversed), Encode(packet), context, ledger, Dependency), 6);
    }
    [Fact]
    public void EveryDependencyIsCheckedBeforeReplayRetryAndReadback()
    {
        using var fixture = Fixture(); var row = Row(fixture, "axis", "selected"); var context = Context(); var source = Artifact(row.GetProperty("source"));
        var ledger = row.GetProperty("trustedInputs").EnumerateArray().Select(Artifact).ToArray(); var result = CampaignCombatActualSelection.ReplayTrustedSource(source, context, ledger, Dependency);
        foreach (var pin in fixture.RootElement.GetProperty("sourceHashes").EnumerateObject())
        {
            byte[] Tamper(string path) { var bytes = Dependency(path); if (path == pin.Name) bytes[0] ^= 1; return bytes; }
            Reject(() => CampaignCombatActualSelection.ReplayTrustedSource(source, context, ledger, Tamper), 9);
            Reject(() => CampaignCombatActualSelection.ApplyTrustedSource(source, context, ledger, ledger[0], Tamper), 9);
            Reject(() => CampaignCombatActualSelectionCodec.ReadProof(result.ProofBytes, source, context, ledger, Tamper), 9);
            Reject(() => CampaignCombatActualSelectionCodec.ReadControl(result.ControlBytes, source, context, ledger, Tamper), 9);
        }
    }
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-actual-selection-v1.json")));
    private static JsonElement Row(JsonDocument fixture, string owner, string variant) => fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("owner").GetString() == owner && c.GetProperty("variant").GetString() == variant);
    private static byte[] Artifact(JsonElement artifact) => Encoding.ASCII.GetBytes(artifact.GetProperty("canonicalUtf8").GetString()!);
    private static byte[] Encode(JsonNode node) => CampaignCombatPositiveEntryCodec.Encode(node);
    private static byte[] Cut(JsonObject packet, int count)
    {
        var copy = packet.DeepClone(); copy["selectionEventCanonicalUtf8"] = new JsonArray(copy["selectionEventCanonicalUtf8"]!.AsArray().Take(count).Select(n => n!.DeepClone()).ToArray()); return Encode(copy);
    }
    private static void Resign(JsonNode e)
    {
        var unsigned = e.DeepClone().AsObject(); unsigned.Remove("receiptId"); e["receiptId"] = "asc." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.actual-selection.receipt.v1", Encode(unsigned))[7..];
    }
    private static void Reject(Action action, int code) => Assert.StartsWith($"CMB-ASE-{code:000}", Assert.Throws<JsonException>(action).Message);
    [Theory]
    [MemberData(nameof(Cases))]
    public void CombinedErrorPrecedenceMatchesIndependentDocumentedGateTable(string owner, string variant)
    {
        using var fixture = Fixture(); var row = Row(fixture, owner, variant); var context = Context(); var packet = JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject();
        var ledger = row.GetProperty("trustedInputs").EnumerateArray().Select(Artifact).ToArray();
        var entry = CampaignCombatActualSelection.ReplayTrustedSource(Cut(packet, 0), context, [], Dependency);
        var boundary = JsonNode.Parse(entry.Proof.GetProperty("boundary").GetRawText())!.AsObject();
        foreach (var cut in Enumerable.Range(0, ledger.Length + 1))
        {
            var state = JsonNode.Parse(Artifact(row.GetProperty("controls")[cut]))!.AsObject();
            foreach (var original in MatrixCommands(boundary, state))
            {
                var kind = original["command"]!["kind"]!.GetValue<string>();
                foreach (var (now, available) in new (long?, bool)[] { (null, true), (null, false), (2300, true), (2300, false), (253402300799999, true) })
                {
                    var input = original.DeepClone().AsObject(); input["admittedAt"] = now; input["clockAvailable"] = available;
                    var expected = ExpectedGate(state, input);
                    Assert.Equal(expected, MechanicsOutcome(boundary, state, input, context.Configuration));
                }
                var bad = original.DeepClone().AsObject(); bad["admittedAt"] = null; bad["clockAvailable"] = false;
                if (!state["closed"]!.GetValue<bool>() && kind is "complete-step" or "close-empty-selection")
                {
                    var expected = ExpectedGate(state, bad);
                    var actual = PublicOutcome(Cut(packet, cut), context, ledger.Take(cut).ToArray(), Encode(bad));
                    Assert.Equal(expected, actual);
                }
                var segment = bad.DeepClone().AsObject(); segment["command"]!["segmentId"] = "foreign";
                Assert.Equal("error4", MechanicsOutcome(boundary, state, segment, context.Configuration));
                var arm = segment.DeepClone().AsObject();
                if (kind == "choose-selection") arm["command"]!["participant"] = boundary["candidate"]!["defender"]!["unit"]!.DeepClone();
                else arm["command"]!["choice"] = "finish-without-attack";
                Assert.Equal("error3", MechanicsOutcome(boundary, state, arm, context.Configuration));
                foreach (var primitive in new JsonNode[] { JsonValue.Create(true)!, JsonValue.Create(-1)! })
                {
                    var input = arm.DeepClone().AsObject(); input["admittedAt"] = primitive.DeepClone();
                    Assert.Equal(primitive.GetValueKind() == JsonValueKind.True ? "error1" : "error2", MechanicsOutcome(boundary, state, input, context.Configuration));
                }
                var version = segment.DeepClone().AsObject(); version["command"]!["contractVersion"] = 2;
                Assert.Equal("error3", MechanicsOutcome(boundary, state, version, context.Configuration));
                var actor = bad.DeepClone().AsObject(); actor["actor"] = kind is "choose-selection" or "decline-rba" ? "system" : owner;
                Assert.Equal("error4", MechanicsOutcome(boundary, state, actor, context.Configuration));
                if (kind is "open-segment" or "close-empty-selection" or "complete-step" or "open-rba")
                {
                    var input = bad.DeepClone().AsObject(); input["command"]!["expectedPriorVersion"] = -1;
                    Assert.Equal("error6", MechanicsOutcome(boundary, state, input, context.Configuration));
                }
                if (kind is "complete-step" or "open-rba")
                {
                    var input = bad.DeepClone().AsObject(); input["command"]!["fromPositionId"] = "foreign.position";
                    Assert.Equal("error6", MechanicsOutcome(boundary, state, input, context.Configuration));
                }
                if (kind is "choose-selection" or "decline-rba")
                {
                    var input = bad.DeepClone().AsObject(); input["command"]!["decisionId"] = "foreign.decision";
                    Assert.Equal("error6", MechanicsOutcome(boundary, state, input, context.Configuration));
                    input = bad.DeepClone().AsObject(); input["actor"] = owner == "axis" ? "commonwealth" : "axis";
                    if (kind == "decline-rba") input["actor"] = owner;
                    var gate = ExpectedGate(state, bad);
                    Assert.Equal(gate is "duplicate" or "error5" ? "error4" : "error6", MechanicsOutcome(boundary, state, input, context.Configuration));
                }
                if (kind is "expire-window" or "controller-unavailable")
                {
                    var input = bad.DeepClone().AsObject(); input["command"]!["decisionId"] = "foreign.decision";
                    Assert.Equal("no-op", MechanicsOutcome(boundary, state, input, context.Configuration));
                }
                if (kind == "choose-selection" && state["selectionOutcome"]!.GetValue<string>() == "pending")
                {
                    var input = bad.DeepClone().AsObject(); input["command"]!["candidate"]!["targetLocationId"] = "foreign.location";
                    Assert.Equal("error4", MechanicsOutcome(boundary, state, input, context.Configuration)); input["command"]!["choice"] = "foreign-choice";
                    Assert.Equal("error3", MechanicsOutcome(boundary, state, input, context.Configuration));
                }
                if (kind == "decline-rba" && Active(state) is not null && state["selectionOutcome"]!.GetValue<string>() == "selected")
                {
                    var input = bad.DeepClone().AsObject(); input["command"]!["participant"]!["elementId"] = "foreign.element";
                    Assert.Equal("error4", MechanicsOutcome(boundary, state, input, context.Configuration));
                }
            }
        }
    }
    private static string PublicOutcome(byte[] source, CampaignCombatCreationContext context, byte[][] ledger, byte[] input)
    {
        try { var result = CampaignCombatActualSelection.ApplyTrustedSource(source, context, ledger, input, Dependency); return result.Duplicate ? "duplicate" : result.EventBytes is null ? "no-op" : "accepted"; }
        catch (JsonException e) { return ErrorCode(e); }
    }
    private static string MechanicsOutcome(JsonObject boundary, JsonObject state, JsonObject input, CombatDecisionConfiguration configuration)
    {
        var before = Encode(state); var boundaryBefore = Encode(boundary); var inputBefore = Encode(input);
        try
        {
            var (after, e, receipt, duplicate) = CampaignCombatActualSelection.Transition(boundary, state, input, configuration);
            Assert.Equal(before, Encode(state)); Assert.Equal(boundaryBefore, Encode(boundary)); Assert.Equal(inputBefore, Encode(input));
            if (duplicate || e is null) Assert.Equal(before, Encode(after));
            else { Assert.NotNull(receipt); Assert.Equal(N(state, "stateVersion") + 1, N(after, "stateVersion")); }
            return duplicate ? "duplicate" : e is null ? "no-op" : "accepted";
        }
        catch (JsonException e) { return ErrorCode(e); }
    }
    private static string ErrorCode(JsonException error)
    { Assert.StartsWith("CMB-ASE-", error.Message); return "error" + int.Parse(error.Message.AsSpan(8, 3), System.Globalization.CultureInfo.InvariantCulture); }
    private static JsonNode? Active(JsonNode state) => T(state, "selectionOutcome") == "pending" ? state["selectionWindow"] : T(state, "selectionOutcome") == "selected" && state["declineReceiptId"] is null ? state["rbaWindow"] : null;
    // Independent state/clock table from the frozen contract, deliberately separate from the transition switch.
    private static string ExpectedGate(JsonNode state, JsonNode input)
    {
        var cmd = input["command"]!; var kind = T(cmd, "kind"); var now = input["admittedAt"]?.GetValue<long>(); var available = input["clockAvailable"]!.GetValue<bool>();
        var hash = CampaignOpeningPreambleCodec.Hash(Encode(cmd));
        if (state["receipts"]!.AsArray().Any(r => T(r!, "commandHash") == hash)) return "duplicate";
        var active = Active(state); var outcome = T(state, "selectionOutcome"); var step = N(state, "stepIndex"); var selected = outcome == "selected";
        var beforeRba = kind == "controller-unavailable" && selected && step == 2 && state["rbaWindow"] is null && T(cmd, "decisionId") == T(state, "segmentId") + ".rba";
        if (kind is "expire-window" or "controller-unavailable" && !beforeRba && (active is null || T(cmd, "decisionId") != T(active, "decisionId"))) return "no-op";
        if (state["closed"]!.GetValue<bool>() || state["receipts"]!.AsArray().Count >= 16 || N(state, "stateVersion") == long.MaxValue) return "error6";
        var live = kind switch
        {
            "open-segment" => outcome == "unopened" && state["receipts"]!.AsArray().Count == 0,
            "close-empty-selection" => outcome == "system-no-selection" && state["selectionWindow"] is null,
            "choose-selection" => outcome == "pending",
            "complete-step" => outcome is "selected" or "no-selection" or "cancelled" && (!selected || step != 2 || state["declineReceiptId"] is not null),
            "open-rba" => selected && step == 2 && state["rbaWindow"] is null,
            "decline-rba" => selected && step == 2 && active is not null,
            _ => beforeRba || active is not null,
        };
        if (!live) return "error6";
        if (kind is "complete-step" or "close-empty-selection") { if (now is not null || !available) return "error5"; if (kind == "complete-step" && selected && step >= 3) return "error7"; }
        else if (kind == "open-segment") { if (available && (now is null || now > 253402300769999)) return "error5"; }
        else if (kind == "open-rba") { if (!available || now is null || now < N(state["selectionWindow"]!["timing"]!, "highWaterUnixMilliseconds") || now > 253402300769999) return "error5"; }
        else if (kind is "choose-selection" or "decline-rba" or "expire-window")
        {
            var timing = active!["timing"]!; var before = available && now is not null && now >= N(timing, "highWaterUnixMilliseconds") && now < N(timing, "deadlineUnixMilliseconds");
            if (kind == "expire-window" && before) return "no-op"; if (kind != "expire-window" && !before) return "error5";
        }
        return "accepted";
    }
    private static IEnumerable<JsonObject> MatrixCommands(JsonObject boundary, JsonObject state)
    {
        var segment = T(state, "segmentId"); var active = Active(state); var outcome = T(state, "selectionOutcome");
        var route = Cna1979LandSequence.CreateTurn(1).SkipWhile(p => p.PositionId != T(boundary["position"]!, "positionId")).Take(7).ToArray();
        foreach (var kind in new[] { "open-segment", "close-empty-selection", "choose-selection", "complete-step", "open-rba", "decline-rba", "expire-window", "controller-unavailable" })
        {
            var cmd = new JsonObject { ["contractVersion"] = 1, ["kind"] = kind, ["segmentId"] = segment, ["decisionId"] = null, ["fromPositionId"] = null, ["expectedPriorVersion"] = null, ["choice"] = null, ["candidate"] = null, ["participant"] = null };
            if (kind is "open-segment" or "close-empty-selection" or "complete-step" or "open-rba") cmd["expectedPriorVersion"] = N(state, "stateVersion");
            if (kind is "complete-step" or "open-rba") cmd["fromPositionId"] = route[Math.Min((int)N(state, "stepIndex"), 5)].PositionId;
            if (kind == "choose-selection") { cmd["decisionId"] = segment + ".selection"; cmd["choice"] = "select-close-assault"; cmd["candidate"] = boundary["candidate"]!.DeepClone(); }
            if (kind == "decline-rba") { cmd["decisionId"] = segment + ".rba"; cmd["participant"] = boundary["candidate"]!["defender"]!["unit"]!.DeepClone(); }
            if (kind is "expire-window" or "controller-unavailable") cmd["decisionId"] = kind == "controller-unavailable" && outcome == "selected" && N(state, "stepIndex") == 2 && state["rbaWindow"] is null ? segment + ".rba" : active is null ? segment + ".selection" : T(active, "decisionId");
            var actor = kind == "choose-selection" ? T(boundary["cycle"]!, "actingSide") : kind == "decline-rba" ? T(boundary["candidate"]!["defender"]!["unit"]!, "originalSide") : "system";
            yield return new() { ["command"] = cmd, ["actor"] = actor, ["admittedAt"] = null, ["clockAvailable"] = true };
        }
    }
    private static long N(JsonNode node, string field) => JsonSerializer.SerializeToElement(node[field]).GetInt64();
    private static string T(JsonNode node, string field) => node[field]!.GetValue<string>();
    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryResignedEventAndProofLeafRejectsAgainstIndependentLedger(string owner, string variant)
    {
        using var fixture = Fixture(); var row = Row(fixture, owner, variant); var context = Context(); var packet = JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject();
        var ledger = row.GetProperty("trustedInputs").EnumerateArray().Select(Artifact).ToArray();
        foreach (var index in Enumerable.Range(0, ledger.Length))
        {
            var original = JsonNode.Parse(Artifact(row.GetProperty("events")[index]))!;
            foreach (var path in Leaves(original))
            {
                var forged = Changed(original, path); if (path.Length != 1 || path[0] != "receiptId") Resign(forged);
                var source = JsonNode.Parse(Cut(packet, index + 1))!; source["selectionEventCanonicalUtf8"]![index] = Encoding.ASCII.GetString(Encode(forged));
                Assert.Throws<JsonException>(() => CampaignCombatActualSelection.ReplayTrustedSource(Encode(source), context, ledger.Take(index + 1).ToArray(), Dependency));
            }
            foreach (var malformed in Malformed(Artifact(row.GetProperty("events")[index]))) Assert.Throws<JsonException>(() => CampaignCombatActualSelectionCodec.Parse(malformed, "Event"));
        }
        var full = Encode(packet); var proof = JsonNode.Parse(Artifact(row.GetProperty("proofs")[ledger.Length]))!;
        foreach (var path in Leaves(proof)) Assert.Throws<JsonException>(() => CampaignCombatActualSelectionCodec.ReadProof(Encode(Changed(proof, path)), full, context, ledger, Dependency));
        foreach (var (kind, bytes) in new[] { ("ActualSelectionSource", full), ("ActualSelectionProof", Encode(proof)), ("Control", Artifact(row.GetProperty("controls")[ledger.Length])), ("Input", ledger[0]) })
            foreach (var malformed in Malformed(bytes)) Assert.Throws<JsonException>(() => CampaignCombatActualSelectionCodec.Parse(malformed, kind));
        foreach (var field in new[] { "publicAction", "observation", "provider", "snapshot", "hiddenSeal", "transport" })
            foreach (var (kind, bytes) in new[] { ("ActualSelectionSource", full), ("ActualSelectionProof", Encode(proof)), ("Control", Artifact(row.GetProperty("controls")[ledger.Length])), ("Event", Artifact(row.GetProperty("events")[0])), ("Input", ledger[0]) })
            { var smuggled = JsonNode.Parse(bytes)!; smuggled[field] = null; Reject(() => CampaignCombatActualSelectionCodec.Parse(Encode(smuggled), kind), 1); }
    }
    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void CompleteOriginalHistoryRejectsEveryMutatedLeafAndEarlierCut(string owner)
    {
        using var fixture = Fixture(); var row = Row(fixture, owner, "selected"); var context = Context(); var source = JsonNode.Parse(Cut(JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject(), 0))!;
        var original = JsonNode.Parse(source["positiveEntrySourceCanonicalUtf8"]!.GetValue<string>())!;
        foreach (var count in new[] { 0, 1 })
        {
            var earlier = original.DeepClone(); earlier["entryEventCanonicalUtf8"] = new JsonArray(earlier["entryEventCanonicalUtf8"]!.AsArray().Take(count).Select(v => v!.DeepClone()).ToArray());
            var candidate = source.DeepClone(); candidate["positiveEntrySourceCanonicalUtf8"] = Encoding.ASCII.GetString(Encode(earlier));
            Reject(() => CampaignCombatActualSelection.ReplayTrustedSource(Encode(candidate), context, [], Dependency), 4);
        }
        foreach (var field in new[] { "requestCanonicalUtf8", "createdCanonicalUtf8", "preambleEventCanonicalUtf8", "weatherEventCanonicalUtf8", "stageEventCanonicalUtf8", "reserveEventCanonicalUtf8", "entryEventCanonicalUtf8" })
        {
            var array = original[field] is JsonArray; var records = array ? original[field]!.AsArray().Select(n => n!.GetValue<string>()).ToArray() : [original[field]!.GetValue<string>()];
            foreach (var index in Enumerable.Range(0, records.Length))
            {
                var record = JsonNode.Parse(records[index])!;
                foreach (var path in Leaves(record))
                {
                    var altered = original.DeepClone(); var replacement = Encoding.ASCII.GetString(Encode(Changed(record, path)));
                    if (array) altered[field]![index] = replacement; else altered[field] = replacement;
                    var candidate = source.DeepClone(); candidate["positiveEntrySourceCanonicalUtf8"] = Encoding.ASCII.GetString(Encode(altered));
                    Reject(() => CampaignCombatActualSelection.ReplayTrustedSource(Encode(candidate), context, [], Dependency), 4);
                }
            }
            if (array)
                foreach (var mode in new[] { "missing", "duplicate", "reverse" })
                {
                    if (mode == "reverse" && records.Length < 2) continue; var altered = original.DeepClone(); var history = altered[field]!.AsArray();
                    if (mode == "missing") history.RemoveAt(0); else if (mode == "duplicate") history.Add(history[0]!.DeepClone()); else altered[field] = new JsonArray(history.Reverse().Select(n => n!.DeepClone()).ToArray());
                    var candidate = source.DeepClone(); candidate["positiveEntrySourceCanonicalUtf8"] = Encoding.ASCII.GetString(Encode(altered)); Reject(() => CampaignCombatActualSelection.ReplayTrustedSource(Encode(candidate), context, [], Dependency), 4);
                }
        }
    }
    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void ExactClocksCapacityAndFamilyBoundariesRemainClosed(string owner)
    {
        using var fixture = Fixture(); var row = Row(fixture, owner, "selected"); var context = Context(); var packet = JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject(); var ledger = row.GetProperty("trustedInputs").EnumerateArray().Select(Artifact).ToArray();
        foreach (var index in new[] { 1, 5 })
        {
            var active = Active(JsonNode.Parse(Artifact(row.GetProperty("controls")[index]))!)!; var timing = active["timing"]!; var floor = N(timing, "highWaterUnixMilliseconds"); var deadline = N(timing, "deadlineUnixMilliseconds");
            foreach (var (now, available) in new (long?, bool)[] { (null, true), (null, false), (floor - 1, true), (deadline, true), (deadline + 1, true), (floor, false) })
            {
                var input = JsonNode.Parse(ledger[index])!; input["admittedAt"] = now; input["clockAvailable"] = available; Reject(() => CampaignCombatActualSelection.ApplyTrustedSource(Cut(packet, index), context, ledger.Take(index).ToArray(), Encode(input), Dependency), 5);
                input["actor"] = index == 1 ? (owner == "axis" ? "commonwealth" : "axis") : owner; Reject(() => CampaignCombatActualSelection.ApplyTrustedSource(Cut(packet, index), context, ledger.Take(index).ToArray(), Encode(input), Dependency), 4);
            }
            foreach (var now in new[] { floor, deadline - 1 }) { var input = JsonNode.Parse(ledger[index])!; input["admittedAt"] = now; Assert.NotNull(CampaignCombatActualSelection.ApplyTrustedSource(Cut(packet, index), context, ledger.Take(index).ToArray(), Encode(input), Dependency).EventBytes); }
        }
        foreach (var index in new[] { 0, 4 })
            foreach (var now in new[] { 253402300769999L, 253402300770000L })
            {
                var input = JsonNode.Parse(ledger[index])!; input["admittedAt"] = now;
                if (now == 253402300769999) Assert.NotNull(CampaignCombatActualSelection.ApplyTrustedSource(Cut(packet, index), context, ledger.Take(index).ToArray(), Encode(input), Dependency).EventBytes);
                else Reject(() => CampaignCombatActualSelection.ApplyTrustedSource(Cut(packet, index), context, ledger.Take(index).ToArray(), Encode(input), Dependency), 5);
            }
        var earlyRba = JsonNode.Parse(ledger[4])!; earlyRba["admittedAt"] = 1099; Reject(() => CampaignCombatActualSelection.ApplyTrustedSource(Cut(packet, 4), context, ledger.Take(4).ToArray(), Encode(earlyRba), Dependency), 5);
        var boundary = JsonNode.Parse(Artifact(row.GetProperty("proofs")[0]))!["boundary"]!.AsObject(); var pending = JsonNode.Parse(Artifact(row.GetProperty("controls")[1]))!.AsObject(); var choice = JsonNode.Parse(ledger[1])!.AsObject();
        var full = pending.DeepClone().AsObject(); full["receipts"] = new JsonArray(Enumerable.Range(0, 16).Select(_ => full["receipts"]![0]!.DeepClone()).ToArray()); Assert.Equal("error6", MechanicsOutcome(boundary, full, choice, context.Configuration));
        var overflow = pending.DeepClone().AsObject(); overflow["stateVersion"] = long.MaxValue; Assert.Equal("error6", MechanicsOutcome(boundary, overflow, choice, context.Configuration));
        foreach (var input in ledger) { var bad = JsonNode.Parse(input)!; bad["admittedAt"] = true; Reject(() => CampaignCombatActualSelection.ApplyTrustedSource(Encode(packet), context, ledger, Encode(bad), Dependency), 1); }
        var longOverflow = JsonNode.Parse(ledger[0])!; longOverflow["command"]!["expectedPriorVersion"] = JsonNode.Parse("9223372036854775808"); Reject(() => CampaignCombatActualSelection.ApplyTrustedSource(Cut(packet, 0), context, [], Encode(longOverflow), Dependency), 2);
        var tooMany = packet.DeepClone(); tooMany["selectionEventCanonicalUtf8"] = new JsonArray(Enumerable.Range(0, 17).Select(_ => packet["selectionEventCanonicalUtf8"]![0]!.DeepClone()).ToArray()); Reject(() => CampaignCombatActualSelection.ReplayTrustedSource(Encode(tooMany), context, Enumerable.Range(0, 17).Select(_ => ledger[0]).ToArray(), Dependency), 1);
        Reject(() => CampaignCombatActualSelectionCodec.Parse(new byte[1_048_577], "Input"), 1);
        Reject(() => CampaignCombatActualSelectionCodec.Parse(Encode(new JsonArray(Enumerable.Range(0, 513).Select(_ => JsonValue.Create("x") as JsonNode).ToArray())), "id[]"), 1);
        var exact = Encoding.ASCII.GetBytes("\"" + new string('x', 1_048_574) + "\""); Assert.Equal(1_048_576, exact.Length); _ = CampaignCombatActualSelectionCodec.Parse(exact, "utf8");
        foreach (var index in Enumerable.Range(0, ledger.Length))
        {
            var family = packet.DeepClone(); var e = JsonNode.Parse(family["selectionEventCanonicalUtf8"]![index]!.GetValue<string>())!; e["eventType"] = "combat-" + T(e["effect"]!, "kind"); Resign(e); family["selectionEventCanonicalUtf8"]![index] = Encoding.ASCII.GetString(Encode(e)); Reject(() => CampaignCombatActualSelection.ReplayTrustedSource(Encode(family), context, ledger, Dependency), 6);
        }
    }
    private static IEnumerable<string[]> Leaves(JsonNode? node, string[]? path = null)
    {
        path ??= [];
        if (node is JsonObject obj) { foreach (var field in obj) foreach (var leaf in Leaves(field.Value, [.. path, field.Key])) yield return leaf; }
        else if (node is JsonArray array && array.Count > 0) { for (var i = 0; i < array.Count; i++) foreach (var leaf in Leaves(array[i], [.. path, i.ToString(System.Globalization.CultureInfo.InvariantCulture)])) yield return leaf; }
        else yield return path;
    }
    private static JsonNode Changed(JsonNode original, string[] path)
    {
        var copy = original.DeepClone(); var target = copy;
        foreach (var part in path[..^1]) target = target is JsonArray a ? a[int.Parse(part, System.Globalization.CultureInfo.InvariantCulture)]! : target[part]!;
        var last = path[^1]; var value = target is JsonArray array ? array[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] : target[last]; JsonNode? replacement;
        if (value is null) replacement = JsonValue.Create(1);
        else if (value is JsonArray) replacement = new JsonArray((JsonNode?)null);
        else if (value.GetValueKind() is JsonValueKind.True or JsonValueKind.False) replacement = JsonValue.Create(!value.GetValue<bool>());
        else if (value.GetValueKind() == JsonValueKind.Number) replacement = JsonValue.Create(JsonSerializer.SerializeToElement(value).GetInt64() + 1);
        else
        {
            var text = value.GetValue<string>(); var hex = text.Length == 64 && text.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
            replacement = JsonValue.Create(text.StartsWith("sha256:", StringComparison.Ordinal) ? "sha256:" + new string(text == "sha256:" + new string('0', 64) ? '1' : '0', 64) : hex ? new string(text == new string('0', 64) ? '1' : '0', 64) : "tampered");
        }
        if (target is JsonArray list) list[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] = replacement; else target[last] = replacement; return copy;
    }
    private static IEnumerable<byte[]> Malformed(byte[] bytes)
    {
        var text = Encoding.ASCII.GetString(bytes); var obj = JsonNode.Parse(bytes)!.AsObject(); var first = obj.First().Key;
        foreach (var raw in new[] { text + " ", text + "\n", text[..^1], text + "{}", "{\"" + first + "\":1," + text[1..], "{\"provider\":null," + text[1..], text.Replace("\"contractVersion\":1", "\"contractVersion\":1.0", StringComparison.Ordinal), text.Replace("\"contractVersion\":1", "\"contractVersion\":1e0", StringComparison.Ordinal), text.Replace("\"contractVersion\":1", "\"contractVersion\":true", StringComparison.Ordinal), text.Replace("contractVersion", "contract\\u0056ersion", StringComparison.Ordinal), new string('[', 34) + "0" + new string(']', 34) }) yield return Encoding.ASCII.GetBytes(raw);
        yield return [0xff]; yield return [0xef, 0xbb, 0xbf, .. bytes];
        var missing = obj.DeepClone().AsObject(); missing.Remove(first); yield return Encode(missing);
        var backwards = new JsonObject(); foreach (var field in obj.Reverse()) backwards[field.Key] = field.Value?.DeepClone(); yield return Encode(backwards);
    }
    [Theory]
    [InlineData("9223372036854775808", 2)]
    [InlineData("true", 1)]
    public void IntegerRangeRejectsBeforeSegmentAndClock(string value, int code)
    {
        using var fixture = Fixture(); var row = Row(fixture, "axis", "selected"); var context = Context(); var packet = JsonNode.Parse(Artifact(row.GetProperty("source")))!.AsObject();
        var input = JsonNode.Parse(Artifact(row.GetProperty("trustedInputs")[0]))!; input["command"]!["segmentId"] = "foreign"; input["admittedAt"] = JsonNode.Parse(value);
        Reject(() => CampaignCombatActualSelection.ApplyTrustedSource(Cut(packet, 0), context, [], Encode(input), Dependency), code);
    }
    [Fact]
    public void MissingDependencyRejectsBeforeSourceSyntax()
    {
        Reject(() => CampaignCombatActualSelection.ReplayTrustedSource([], Context(), [], _ => throw new KeyNotFoundException()), 9);
    }
    private static byte[] Dependency(string path) => File.ReadAllBytes(Path.Combine(Repository(), path));
    private static string Repository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Sandtable.slnx"))) directory = directory.Parent;
        return directory!.FullName;
    }
    private static CampaignCombatCreationContext Context()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
        using var golden = JsonDocument.Parse(fixture.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
        var root = golden.RootElement;
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("setup").GetRawText()), artifact, scenario);
        var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
        return new(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, config);
    }
}
