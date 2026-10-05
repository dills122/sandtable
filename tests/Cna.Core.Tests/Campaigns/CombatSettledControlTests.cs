using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatSettledControlTests
{
    [Fact]
    public void NativeSettledControlOpensOwnerDecisionAndRepeatPreservesLiteralWorld()
    {
        using var source = Fixture("combat-settled-continuation-v1.json");
        var row = source.RootElement.GetProperty("traces")[0];
        using var control = Fixture("combat-settled-control-v1.json");
        var trace = control.RootElement.GetProperty("traces")[0];
        var basis = Basis(row);
        var state = CampaignCombatSettledControl.Replay(basis, Context(), [Bytes(trace.GetProperty("inputs")[0])], [Utf8(trace.GetProperty("eventCanonicalUtf8")[0])]);
        using var opened = JsonDocument.Parse(state);
        Assert.True(opened.RootElement.TryGetProperty("status", out var status), "Native settled control has not opened the owner decision.");
        Assert.Equal("open", status.GetString());
        Assert.Equal(Utf8(trace.GetProperty("stateCanonicalUtf8")[1]), state);
    }
    [Fact]
    public void EightyFrozenTracesAllCutsOriginalRetriesAndPurePreservation()
    {
        using var f = Fixture("combat-settled-control-v1.json"); using var sf = Fixture("combat-settled-continuation-v1.json");
        var rows = sf.RootElement.GetProperty("traces").EnumerateArray().Concat(sf.RootElement.GetProperty("movementDescriptorProbes").EnumerateArray()).ToArray();
        var sources = rows.ToDictionary(r => Key(r), r => CampaignCombatSettledControlCodec.ReadBase(Basis(r), Context()), StringComparer.Ordinal);
        var cuts = 0; var retries = 0; var traces = 0;
        foreach (var trace in f.RootElement.GetProperty("traces").EnumerateArray())
        {
            var source = sources[Key(trace)]; var inputs = trace.GetProperty("inputs").EnumerateArray().Select(Bytes).ToArray();
            var events = trace.GetProperty("eventCanonicalUtf8").EnumerateArray().Select(Utf8).ToArray();
            var states = trace.GetProperty("stateCanonicalUtf8").EnumerateArray().Select(Utf8).ToArray();
            using var initial = JsonDocument.Parse(states[0]);
            for (var cut = 0; cut <= events.Length; cut++)
            {
                var state = CampaignCombatSettledControl.Replay(source, inputs[..cut], events[..cut]);
                Assert.Equal(states[cut], state.CanonicalBytes);
                Assert.Equal(states[cut], CampaignCombatSettledControlCodec.ReadState(states[cut], source, inputs[..cut], events[..cut]).CanonicalBytes);
                var projection = state.Projection;
                foreach (var field in new[] { "world", "randomState", "members", "attackHistory" })
                    Assert.Equal(initial.RootElement.GetProperty(field).GetRawText(), projection.GetProperty(field).GetRawText());
                if (cut < events.Length)
                {
                    var accepted = CampaignCombatSettledControl.Apply(source, inputs[..cut], events[..cut], inputs[cut], states[cut]);
                    Assert.Equal(CombatStepsDisposition.Accepted, accepted.Disposition); Assert.Equal(events[cut], accepted.EventBytes); Assert.Equal(states[cut + 1], accepted.State.CanonicalBytes);
                }
                for (var i = 0; i < cut; i++)
                {
                    var duplicate = CampaignCombatSettledControl.Apply(source, inputs[..cut], events[..cut], inputs[i]);
                    Assert.Equal(CombatStepsDisposition.Duplicate, duplicate.Disposition); Assert.Equal(events[i], duplicate.EventBytes); Assert.Equal(states[cut], duplicate.State.CanonicalBytes); retries++;
                    var alteredAdmission = JsonNode.Parse(inputs[i])!; alteredAdmission["admittedAt"] = CampaignCombatSelectionSteps.UtcMaximum; alteredAdmission["clockAvailable"] = false;
                    var originalReply = CampaignCombatSettledControl.Apply(source, inputs[..cut], events[..cut], Encode(alteredAdmission));
                    Assert.Equal(CombatStepsDisposition.Duplicate, originalReply.Disposition); Assert.Equal(events[i], originalReply.EventBytes); Assert.Equal(states[cut], originalReply.State.CanonicalBytes); retries++;
                }
                cuts++;
            }
            using var terminal = JsonDocument.Parse(states[^1]); var final = terminal.RootElement;
            if (trace.GetProperty("outcome").GetString() == "repeat" && trace.GetProperty("descriptor").GetString() == "boundary-locations")
            {
                Assert.Equal("repeated", final.GetProperty("status").GetString());
                Assert.Empty(final.GetProperty("targetUses").EnumerateArray()); Assert.Empty(final.GetProperty("nextCycleProgress").EnumerateArray());
                Assert.Equal(initial.RootElement.GetProperty("activeCycle").GetProperty("ordinal").GetInt32() + 1, final.GetProperty("activeCycle").GetProperty("ordinal").GetInt32());
                using var openedCut = JsonDocument.Parse(states[1]);
                Assert.Equal(openedCut.RootElement.GetProperty("prefix").GetString(), final.GetProperty("activeCycle").GetProperty("openingPrefix").GetString());
                Assert.EndsWith(".movement", final.GetProperty("positionId").GetString());
            }
            else { Assert.Equal("finished", final.GetProperty("status").GetString()); Assert.EndsWith("truck-convoy-movement", final.GetProperty("positionId").GetString()); }
            traces++;
        }
        Assert.Equal(36, sources.Count); Assert.Equal(80, traces); Assert.Equal(236, cuts); Assert.Equal(464, retries);
    }
    [Theory]
    [InlineData("ordinary", 1, 9, 2, 0)]
    [InlineData("zero-retreat", 2, 7, 0, 0)]
    [InlineData("refusal-loss-dp", 1, 9, 2, 0)]
    [InlineData("zero-engaged", 1, 11, 4, 1)]
    [InlineData("defender-capture-guard", 1, 9, 2, 0)]
    [InlineData("defender-capture-escape", 1, 9, 2, 0)]
    [InlineData("attacker-capture-guard-cp-limit", 2, 12, 0, 2)]
    [InlineData("attacker-capture-escape", 2, 7, 0, 0)]
    public void NativeControlAssessmentRetainsLiteralCostsAndCommitmentReceipt(string name, int count, int after, int breakOff, int dp)
    {
        using var sf = Fixture("combat-settled-continuation-v1.json"); var contexts = 0;
        foreach (var row in sf.RootElement.GetProperty("traces").EnumerateArray().Where(r => r.GetProperty("sourceId").GetString()!.StartsWith(name + ".", StringComparison.Ordinal)))
        {
            var source = CampaignCombatSettledControlCodec.ReadBase(Basis(row), Context()); var initial = CampaignCombatSettledControl.Replay(source, [], []);
            var input = CampaignCombatSettledControlCodec.SerializeInput(new(CampaignCombatSettledControl.Command(source, initial, "open"), CampaignOpeningPreambleActor.System, 100000));
            var opened = CampaignCombatSettledControl.Apply(source, [], [], input); var a = opened.State.Projection.GetProperty("assessment");
            Assert.Equal(count, a.GetProperty("witnesses").GetArrayLength());
            Assert.All(a.GetProperty("witnesses").EnumerateArray(), w => { Assert.Equal(after, w.GetProperty("afterCp").GetInt32()); Assert.Equal(breakOff, w.GetProperty("breakOffCost").GetInt32()); Assert.Equal(dp, w.GetProperty("excessCpDp").GetInt32()); Assert.Equal(2, w.GetProperty("terrainCost").GetInt32()); Assert.False(w.GetProperty("usesReleaseException").GetBoolean()); });
            using var packet = JsonDocument.Parse(row.GetProperty("packetCanonicalUtf8").GetString()!);
            var commitment = packet.RootElement.GetProperty("source").GetProperty("roundEventCanonicalUtf8").EnumerateArray().Select(Utf8).Single(IsCommitment);
            using var evt = JsonDocument.Parse(commitment); var progress = Assert.Single(a.GetProperty("progress").EnumerateArray());
            Assert.Equal(evt.RootElement.GetProperty("receiptId").GetString(), progress.GetProperty("receiptId").GetString()); Assert.Equal(CampaignOpeningPreambleCodec.Hash(commitment), progress.GetProperty("eventHash").GetString()); contexts++;
        }
        Assert.Equal(4, contexts);
    }
    [Fact]
    public void EveryControlStateAndEventLeafRejectsForgery()
    {
        using var sf = Fixture("combat-settled-continuation-v1.json"); using var f = Fixture("combat-settled-control-v1.json");
        var source = CampaignCombatSettledControlCodec.ReadBase(Basis(sf.RootElement.GetProperty("traces")[0]), Context()); var trace = f.RootElement.GetProperty("traces")[0];
        var inputs = trace.GetProperty("inputs").EnumerateArray().Select(Bytes).ToArray(); var events = trace.GetProperty("eventCanonicalUtf8").EnumerateArray().Select(Utf8).ToArray();
        var count = 0;
        for (var cut = 0; cut <= 2; cut++)
        {
            var state = Utf8(trace.GetProperty("stateCanonicalUtf8")[cut]);
            foreach (var path in Leaves(JsonNode.Parse(state)))
            {
                var node = JsonNode.Parse(state)!; Mutate(node, path);
                Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledControlCodec.ReadState(Encode(node), source, inputs[..cut], events[..cut])); count++;
            }
            if (cut == 0) continue;
            foreach (var path in Leaves(JsonNode.Parse(events[cut - 1])))
            {
                var node = JsonNode.Parse(events[cut - 1])!; Mutate(node, path); var changed = events[..cut]; changed[^1] = Encode(node);
                Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledControl.Replay(source, inputs[..cut], changed)); count++;
            }
        }
        Assert.True(count > 500);
    }
    [Fact]
    public void WrongOwnerForeignStaleDeadlineEarlyTimerAndClockLoss()
    {
        using var sf = Fixture("combat-settled-continuation-v1.json"); using var f = Fixture("combat-settled-control-v1.json");
        var source = CampaignCombatSettledControlCodec.ReadBase(Basis(sf.RootElement.GetProperty("traces")[0]), Context()); var trace = f.RootElement.GetProperty("traces")[0];
        var open = Bytes(trace.GetProperty("inputs")[0]); var choose = Bytes(trace.GetProperty("inputs")[1]); var eventOpen = Utf8(trace.GetProperty("eventCanonicalUtf8")[0]);
        var opened = CampaignCombatSettledControl.Replay(source, [open], [eventOpen]); var deadline = opened.Projection.GetProperty("timing").GetProperty("deadlineUnixMilliseconds").GetInt64();
        foreach (var field in new[] { "actor", "controlId", "cycleId", "expectedPriorVersion", "decisionId", "deadline" })
        {
            var input = JsonNode.Parse(choose)!;
            switch (field)
            {
                case "actor": input["actor"] = "commonwealth"; break;
                case "controlId": input["command"]![field] = "sctl.foreign"; break;
                case "cycleId": input["command"]![field] = "sha256:" + new string('0', 64); break;
                case "expectedPriorVersion": input["command"]![field] = 1; break;
                case "decisionId": input["command"]![field] = "foreign.decision"; break;
                default: input["admittedAt"] = deadline; break;
            }
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledControl.Apply(source, [open], [eventOpen], Encode(input)));
        }
        byte[] Timer(string kind, long now, string? decision = null) => CampaignCombatSettledControlCodec.SerializeInput(new(
            CampaignCombatSettledControl.Command(source, opened, kind) with { DecisionId = decision ?? opened.Projection.GetProperty("decisionId").GetString() }, CampaignOpeningPreambleActor.System, now));
        Assert.Equal(CombatStepsDisposition.NoOp, CampaignCombatSettledControl.Apply(source, [open], [eventOpen], Timer("expire", deadline - 1)).Disposition);
        Assert.Equal(CombatStepsDisposition.NoOp, CampaignCombatSettledControl.Apply(source, [open], [eventOpen], Timer("expire", deadline, "stale.timer")).Disposition);
        var expired = CampaignCombatSettledControl.Apply(source, [open], [eventOpen], Timer("expire", deadline)); Assert.Equal("finished", expired.State.Projection.GetProperty("status").GetString());
        var regressed = JsonNode.Parse(choose)!; regressed["admittedAt"] = 99999;
        var fallback = CampaignCombatSettledControl.Apply(source, [open], [eventOpen], Encode(regressed));
        using var evt = JsonDocument.Parse(fallback.EventBytes!); Assert.Equal("system", evt.RootElement.GetProperty("author").GetString()); Assert.Equal("clock-unavailable", evt.RootElement.GetProperty("effect").GetProperty("reason").GetString());
        var stale = JsonNode.Parse(choose)!; stale["command"]!["expectedPriorVersion"] = 0;
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledControl.Apply(source, [open, choose], [eventOpen, Utf8(trace.GetProperty("eventCanonicalUtf8")[1])], Encode(stale)));
    }
    [Fact]
    public void CanonicalShapeCapacitySourceAndOwnedBytesCannotBeBypassed()
    {
        using var sf = Fixture("combat-settled-continuation-v1.json"); using var f = Fixture("combat-settled-control-v1.json");
        var basis = Basis(sf.RootElement.GetProperty("traces")[0]); var source = CampaignCombatSettledControlCodec.ReadBase(basis, Context()); var trace = f.RootElement.GetProperty("traces")[0];
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1048577], Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(basis) + "\n"), Encoding.UTF8.GetBytes("{\"contractVersion\":1," + Encoding.UTF8.GetString(basis)[1..]) })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledControlCodec.ReadBase(bytes, Context()));
        foreach (var field in new[] { "packetCanonicalUtf8", "proofCanonicalUtf8", "extra", "contractVersion" })
        {
            var node = JsonNode.Parse(basis)!; node[field] = field == "contractVersion" ? JsonValue.Create(2) : JsonValue.Create("{}");
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledControlCodec.ReadBase(Encode(node), Context()));
        }
        var expected = source.CanonicalBytes; basis[0] = 0; var output = source.CanonicalBytes; output[0] = 0; Assert.Equal(expected, source.CanonicalBytes);
        var open = Bytes(trace.GetProperty("inputs")[0]); var accepted = CampaignCombatSettledControl.Apply(source, [], [], open); var eventExpected = accepted.EventBytes!; var stateExpected = accepted.State.CanonicalBytes;
        open[0] = 0; output = accepted.EventBytes!; output[0] = 0; output = accepted.State.CanonicalBytes; output[0] = 0;
        Assert.Equal(eventExpected, accepted.EventBytes); Assert.Equal(stateExpected, accepted.State.CanonicalBytes);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledControl.Replay(source, new byte[3][] { [], [], [] }, new byte[3][] { [], [], [] }));
        var nodeDepth = Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledControlCodec.Parse(nodeDepth, "SettledControlState"));
        foreach (var changed in new[] { stateExpected.Concat(new byte[] { 10 }).ToArray(), Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(stateExpected).Replace("\"contractVersion\":1", "\"contractVersion\":true", StringComparison.Ordinal)), Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(stateExpected).Replace("\"contractVersion\":1", "\"contractVersion\":1.0", StringComparison.Ordinal)) })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledControlCodec.ReadState(changed, source, [Bytes(trace.GetProperty("inputs")[0])], [eventExpected]));
        var forged = JsonNode.Parse(stateExpected)!; forged["members"] = new JsonArray(Enumerable.Range(0, 33).Select(_ => forged["members"]![0]!.DeepClone()).ToArray());
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledControlCodec.Parse(Encode(forged), "SettledControlState"));
    }
    [Fact]
    public void IndependentPolicyProbesCannotInventAdmittedHistory()
    {
        foreach (var (progress, witnesses, expected) in new[] { (0, 0, "no-continuation"), (1, 0, "no-continuation"), (0, 1, "no-material-progress"), (1, 1, "owner-choice") })
        {
            using var a = JsonDocument.Parse(Encode(new JsonObject { ["witnessOutcome"] = "supported", ["progress"] = new JsonArray(Enumerable.Range(0, progress).Select(_ => (JsonNode)JsonValue.Create(1)!).ToArray()), ["witnesses"] = new JsonArray(Enumerable.Range(0, witnesses).Select(_ => (JsonNode)JsonValue.Create(1)!).ToArray()) }));
            Assert.Equal(expected, CampaignCombatSettledControl.Mode(a.RootElement));
        }
        using var unsupported = JsonDocument.Parse("{\"witnessOutcome\":\"unsupported\"}"); Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledControl.Mode(unsupported.RootElement));
    }
    [Fact]
    public void ControlRejectsFullyResignedNativeResultClockAndRelease()
    {
        using var f = Fixture("combat-settled-continuation-v1.json"); var row = f.RootElement.GetProperty("traces").EnumerateArray().Single(r => r.GetProperty("sourceId").GetString()! == "zero-retreat.axis.attacker");
        using var results = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-result-settlement-v2.json")));
        var resultRow = results.RootElement.GetProperty("traces").EnumerateArray().Single(r => r.GetProperty("name").GetString()! == "zero-retreat.axis.attacker"); var test = CombatResolutionTests.Case(resultRow);
        var inputs = new List<CombatResolutionInput>(); var events = new List<byte[]>();
        foreach (var i in resultRow.GetProperty("resultInputs").EnumerateArray())
        {
            var input = CampaignCombatResolutionCodec.ReadInput(JsonSerializer.SerializeToUtf8Bytes(i));
            var state = CampaignCombatResolution.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events);
            input = input with { AdmittedAt = input.AdmittedAt is { } at ? at + 100 : null, Command = input.Command with { DecisionId = input.Command.Kind == "choose" ? state.Window!.DecisionId : null } };
            var accepted = CampaignCombatResolution.ApplyTrustedBoundary(test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events, input);
            Assert.Equal(CombatStepsDisposition.Accepted, accepted.Disposition); inputs.Add(input); events.Add(accepted.EventBytes!);
        }
        var source = new CombatResultReleaseSource(resultRow.GetProperty("name").GetString()!, test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events);
        var initial = CampaignCombatResultRelease.Replay(source, [], []); Assert.True(initial.Result.Closed);
        var releaseInputs = new List<CombatReleaseInput>(); var releaseEvents = new List<byte[]>(); var release = initial.Release;
        foreach (var kind in new[] { "open", "complete" })
        {
            var input = new CombatReleaseInput(new(1, kind, release.ReleaseId, release.StateVersion), CampaignOpeningPreambleActor.System);
            var accepted = CampaignCombatResultRelease.Apply(source, releaseInputs, releaseEvents, input); Assert.Equal(CombatStepsDisposition.Accepted, accepted.Disposition);
            releaseInputs.Add(input); releaseEvents.Add(accepted.EventBytes!); release = accepted.State;
        }
        Assert.Equal("completed", release.Status);
        var packet = JsonNode.Parse(row.GetProperty("packetCanonicalUtf8").GetString()!)!; var s = packet["source"]!;
        s["resultInputsCanonicalUtf8"] = Encoding.UTF8.GetString(Encode(new JsonArray(inputs.Select(i => JsonNode.Parse(CampaignCombatResolutionCodec.SerializeInput(i))).ToArray())));
        s["resultEventCanonicalUtf8"] = new JsonArray(events.Select(e => (JsonNode)JsonValue.Create(Encoding.UTF8.GetString(e))!).ToArray());
        s["resultStateCanonicalUtf8"] = Encoding.UTF8.GetString(CampaignCombatResolutionCodec.SerializeState(initial.Result));
        packet["releaseBaseCanonicalUtf8"] = Encoding.UTF8.GetString(CampaignCombatReserveReleaseCodec.SerializeBase(initial.Basis, test.Request));
        packet["releaseInputs"] = new JsonArray(releaseInputs.Select(i => JsonNode.Parse(CampaignCombatReserveReleaseCodec.SerializeInput(i))).ToArray());
        packet["releaseEventCanonicalUtf8"] = new JsonArray(releaseEvents.Select(e => (JsonNode)JsonValue.Create(Encoding.UTF8.GetString(e))!).ToArray());
        var basis = JsonNode.Parse(Basis(row))!; basis["packetCanonicalUtf8"] = Encoding.UTF8.GetString(Encode(packet));
        Assert.StartsWith("CMB-SCC-004", Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledControlCodec.ReadBase(Encode(basis), Context())).Message);
    }
    [Fact]
    public void FullSourceScopeOrdinalPrefixProgressAndOriginalDescriptorRejectTampering()
    {
        using var sf = Fixture("combat-settled-continuation-v1.json"); var row = sf.RootElement.GetProperty("traces")[0];
        var original = Basis(row);
        foreach (var field in new[] { "requestCanonicalUtf8", "createdCanonicalUtf8", "predecessorCanonicalUtf8", "baseCanonicalUtf8", "committedCanonicalUtf8", "resultStateCanonicalUtf8", "roundInputsCanonicalUtf8", "resultInputsCanonicalUtf8" })
        {
            var b = JsonNode.Parse(original)!; var packet = JsonNode.Parse(b["packetCanonicalUtf8"]!.GetValue<string>())!;
            packet["source"]![field] = "{}"; b["packetCanonicalUtf8"] = Encoding.UTF8.GetString(Encode(packet));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledControlCodec.ReadBase(Encode(b), Context()));
        }
        foreach (var path in Leaves(JsonNode.Parse(row.GetProperty("proofCanonicalUtf8").GetString()!)))
        {
            var b = JsonNode.Parse(original)!; var proof = JsonNode.Parse(b["proofCanonicalUtf8"]!.GetValue<string>())!; Mutate(proof, path);
            b["proofCanonicalUtf8"] = Encoding.UTF8.GetString(Encode(proof));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledControlCodec.ReadBase(Encode(b), Context()));
        }
    }
    private static bool IsCommitment(byte[] bytes)
    {
        using var doc = JsonDocument.Parse(bytes); return doc.RootElement.GetProperty("eventType").GetString() == "combat-attack-committed";
    }
    private static string Key(JsonElement row)
    {
        if (row.TryGetProperty("descriptor", out var descriptor)) return row.GetProperty("sourceId").GetString() + "/" + descriptor.GetString();
        using var proof = JsonDocument.Parse(row.GetProperty("proofCanonicalUtf8").GetString()!);
        return proof.RootElement.GetProperty("sourceIdentity").GetProperty("sourceId").GetString() + "/" + proof.RootElement.GetProperty("movementEnd").GetProperty("descriptorId").GetString();
    }
    private static byte[] Encode(JsonNode node) => CampaignCombatSettledContinuationCodec.Encode(node);
    private static IEnumerable<string[]> Leaves(JsonNode? node)
    {
        if (node is JsonObject obj) foreach (var pair in obj) foreach (var tail in Leaves(pair.Value)) yield return [pair.Key, .. tail];
        else if (node is JsonArray array) for (var i = 0; i < array.Count; i++) foreach (var tail in Leaves(array[i])) yield return [i.ToString(System.Globalization.CultureInfo.InvariantCulture), .. tail];
        else yield return [];
    }
    private static void Mutate(JsonNode root, string[] path)
    {
        var parent = root; foreach (var key in path[..^1]) parent = parent is JsonArray a ? a[int.Parse(key, System.Globalization.CultureInfo.InvariantCulture)]! : parent[key]!;
        var last = path[^1]; var value = parent is JsonArray values ? values[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] : parent[last];
        JsonNode? replacement = value?.GetValueKind() switch { JsonValueKind.Number => JsonValue.Create(value.GetValue<long>() + 1), JsonValueKind.True or JsonValueKind.False => JsonValue.Create(!value.GetValue<bool>()), JsonValueKind.String => JsonValue.Create(value.GetValue<string>() + "x"), _ => JsonValue.Create("forged") };
        if (parent is JsonArray items) items[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] = replacement; else parent[last] = replacement;
    }
    private static byte[] Basis(JsonElement row) => CampaignCombatSettledContinuationCodec.Encode(new JsonObject { ["contractVersion"] = 1, ["packetCanonicalUtf8"] = row.GetProperty("packetCanonicalUtf8").GetString(), ["proofCanonicalUtf8"] = row.GetProperty("proofCanonicalUtf8").GetString() });
    private static byte[] Bytes(JsonElement value) => CampaignCombatSettledControlCodec.Canonical(value, "SettledControlInput");
    private static byte[] Utf8(JsonElement value) => Encoding.UTF8.GetBytes(value.GetString()!);
    private static CampaignCombatCreationContext Context()
    {
        using var f = Fixture("combat-result-settlement-v2.json");
        return CombatResolutionTests.Case(f.RootElement.GetProperty("traces")[0]).Request.Context;
    }
    private static JsonDocument Fixture(string name) => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", name)));
}
