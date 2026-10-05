using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatSettledContinuationTests
{
    [Fact]
    public void ExhaustedAmmoEngagedElevenAndContactNineAreLiteralNativeWitnesses()
    {
        using var fixture = Fixture();
        foreach (var side in new[] { "axis", "commonwealth" })
            foreach (var (name, after, dp) in new[] { ("zero-engaged", 11, 1), ("ordinary", 9, 0) })
            {
                var row = fixture.RootElement.GetProperty("traces").EnumerateArray().Single(r => Text(r, "sourceId") == $"{name}.{side}.attacker");
                using var proof = JsonDocument.Parse(Bridge(row));
                Assert.True(proof.RootElement.TryGetProperty("witnesses", out var witnesses), "Native settled witnesses are missing.");
                Assert.Contains(witnesses.EnumerateArray(), w => w.GetProperty("afterCp").GetInt64() == after && w.GetProperty("excessCpDp").GetInt32() == dp);
            }
    }
    [Fact]
    public void ProgressUsesActualCommitmentReceiptAndEventHash()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("traces")[0];
        using var packet = JsonDocument.Parse(Text(row, "packetCanonicalUtf8")); var source = packet.RootElement.GetProperty("source");
        var committedEvent = source.GetProperty("roundEventCanonicalUtf8").EnumerateArray().Single(e => e.GetString()!.Contains("\"eventType\":\"combat-attack-committed\"", StringComparison.Ordinal));
        using var e = JsonDocument.Parse(committedEvent.GetString()!); using var proof = JsonDocument.Parse(Bridge(row));
        Assert.True(proof.RootElement.TryGetProperty("progress", out var progress), "Native settled progress is missing.");
        Assert.Equal(e.RootElement.GetProperty("receiptId").GetString(), progress[0].GetProperty("receiptId").GetString());
        Assert.Equal(CampaignOpeningPreambleCodec.Hash(Encoding.UTF8.GetBytes(committedEvent.GetString()!)), progress[0].GetProperty("eventHash").GetString());
    }
    [Fact]
    public void AllThirtyTwoOwnerSealContextsAndFourDescriptorsMatchExactCanonicalProofs()
    {
        using var fixture = Fixture(); var count = 0;
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray().Concat(fixture.RootElement.GetProperty("movementDescriptorProbes").EnumerateArray()))
        {
            var bytes = Bridge(row); Assert.Equal(Encoding.UTF8.GetBytes(Text(row, "proofCanonicalUtf8")), bytes);
            Assert.Equal(Text(row, "proofHash"), CampaignOpeningPreambleCodec.Hash(bytes));
            var proof = CampaignCombatSettledContinuationCodec.ReadProof(bytes, Encoding.UTF8.GetBytes(Text(row, "packetCanonicalUtf8")), Context());
            Assert.Equal(bytes, proof.CanonicalBytes);
            if (row.TryGetProperty("proofId", out var id)) Assert.Equal(id.GetString(), CampaignCombatSettledContinuationCodec.ProofId(proof));
            count++;
        }
        Assert.Equal(36, count);
    }
    [Fact]
    public void WorldRngHistoriesMembersAndObligationsRemainSourceSpecific()
    {
        using var fixture = Fixture(); var guards = 0; var escapes = 0;
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray())
        {
            using var packet = JsonDocument.Parse(Text(row, "packetCanonicalUtf8")); var source = packet.RootElement.GetProperty("source");
            using var result = JsonDocument.Parse(Text(source, "resultStateCanonicalUtf8")); using var committed = JsonDocument.Parse(Text(source, "committedCanonicalUtf8"));
            using var proof = JsonDocument.Parse(Bridge(row)); var p = proof.RootElement; var w = p.GetProperty("world");
            Assert.Equal(result.RootElement.GetProperty("world").GetRawText(), w.GetRawText());
            Assert.Equal(result.RootElement.GetProperty("randomState").GetRawText(), p.GetProperty("randomState").GetRawText());
            foreach (var name in new[] { "attackHistory", "targetUses" }) Assert.Equal(committed.RootElement.GetProperty(name).GetRawText(), p.GetProperty(name).GetRawText());
            Assert.Equal(result.RootElement.GetProperty("stateVersion").GetInt64() + 2, p.GetProperty("stateVersion").GetInt64());
            Assert.Equal("supported", Text(p, "witnessOutcome")); Assert.Equal(JsonValueKind.Null, p.GetProperty("unsupportedReason").ValueKind);
            Assert.Equal("synthetic-pre-combat", Text(p, "trustLabel"));
            if (w.GetProperty("guards").GetArrayLength() > 0) { guards++; Assert.Single(w.GetProperty("futureObligations").EnumerateArray()); }
            if (w.GetProperty("replacementEntitlements").GetArrayLength() > 0) { escapes++; Assert.Single(w.GetProperty("futureObligations").EnumerateArray()); }
            using var basis = JsonDocument.Parse(Text(packet.RootElement, "releaseBaseCanonicalUtf8"));
            Assert.Equal(JsonValueKind.Null, basis.RootElement.GetProperty("acceptedHighWater").ValueKind);
            Assert.Equal(basis.RootElement.GetProperty("members").GetRawText(), p.GetProperty("members").GetRawText());
            Assert.Equal("none", Text(p.GetProperty("members")[0], "status"));
            Assert.All(p.GetProperty("witnesses").EnumerateArray(), witness => Assert.False(witness.GetProperty("usesReleaseException").GetBoolean()));
        }
        Assert.Equal((8, 8), (guards, escapes));
    }
    [Fact]
    public void OriginalDistanceAndPriorExclusionsRemainAuthenticatedSyntheticEvidence()
    {
        using var fixture = Fixture(); var count = 0;
        foreach (var row in fixture.RootElement.GetProperty("movementDescriptorProbes").EnumerateArray())
        {
            using var p = JsonDocument.Parse(Bridge(row)); var descriptor = p.RootElement.GetProperty("movementEnd");
            Assert.Empty(p.RootElement.GetProperty("witnesses").EnumerateArray());
            var packet = JsonNode.Parse(Text(row, "packetCanonicalUtf8"))!;
            packet["source"]!["movementEnd"]!["proof"]!["completionReceiptId"] = "synthetic.other";
            Reject(Encode(packet), "004"); count++;
            Assert.True(Text(descriptor, "descriptorId") is "distant-original" or "prior-exclusion");
        }
        Assert.Equal(4, count);
    }
    [Fact]
    public void CallerInputOutputAndParsedProjectionsCannotMutateEvidence()
    {
        using var f = Fixture(); var row = f.RootElement.GetProperty("traces")[0]; var packet = Encoding.UTF8.GetBytes(Text(row, "packetCanonicalUtf8"));
        var proof = CampaignCombatSettledContinuation.Bridge(packet, Context()); var expected = proof.CanonicalBytes;
        var restored = CampaignCombatSettledContinuationCodec.ReadProof(expected, packet, Context());
        packet[0] = 0; var projection = JsonNode.Parse(expected)!; projection["world"]!["elements"]![0]!["currentLocationId"] = "forged";
        var output = proof.CanonicalBytes; output[0] = 0; var restoredBytes = restored.CanonicalBytes; restoredBytes[0] = 0;
        Assert.Equal(expected, proof.CanonicalBytes); Assert.Equal(expected, restored.CanonicalBytes); Assert.Equal(expected, Bridge(row));
        var id = CampaignCombatSettledContinuationCodec.ProofId(proof); expected[0] = 0;
        Assert.Equal(id, CampaignCombatSettledContinuationCodec.ProofId(proof));
    }
    [Fact]
    public void ForgedPacketSourceFieldsAndEveryProofLeafReject()
    {
        using var f = Fixture(); var row = f.RootElement.GetProperty("traces")[0]; var packetBytes = Encoding.UTF8.GetBytes(Text(row, "packetCanonicalUtf8"));
        var sourceFields = new[] { "requestCanonicalUtf8", "createdCanonicalUtf8", "baseCanonicalUtf8", "predecessorCanonicalUtf8", "roundInputsCanonicalUtf8", "committedCanonicalUtf8", "resultInputsCanonicalUtf8", "resultStateCanonicalUtf8" };
        foreach (var field in sourceFields)
        {
            var packet = JsonNode.Parse(packetBytes)!; packet["source"]![field] = "{}"; Reject(Encode(packet), "004");
        }
        foreach (var field in new[] { "roundEventCanonicalUtf8", "resultEventCanonicalUtf8" })
        {
            var packet = JsonNode.Parse(packetBytes)!; packet["source"]![field]![0] = "{}"; Reject(Encode(packet), "004");
        }
        var proof = Bridge(row); var mutations = 0;
        foreach (var path in Leaves(JsonNode.Parse(proof)))
        {
            var node = JsonNode.Parse(proof)!; MutateLeaf(node, path);
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledContinuationCodec.ReadProof(Encode(node), packetBytes, Context())); mutations++;
        }
        Assert.True(mutations > 200);
    }
    [Fact]
    public void EmbeddedWorldInputAndPredecessorMetadataCannotHideBehindProjection()
    {
        using var f = Fixture(); var row = f.RootElement.GetProperty("traces")[0];
        foreach (var field in new[] { "baseCanonicalUtf8", "predecessorCanonicalUtf8", "committedCanonicalUtf8", "resultStateCanonicalUtf8" })
        {
            var packet = JsonNode.Parse(Text(row, "packetCanonicalUtf8"))!;
            var nested = JsonNode.Parse(packet["source"]![field]!.GetValue<string>())!;
            nested["extra"] = "ignored-metadata"; packet["source"]![field] = Encoding.UTF8.GetString(Encode(nested)); Reject(Encode(packet), "004");
        }
        foreach (var field in new[] { "roundInputsCanonicalUtf8", "resultInputsCanonicalUtf8" })
        {
            var packet = JsonNode.Parse(Text(row, "packetCanonicalUtf8"))!;
            var nested = JsonNode.Parse(packet["source"]![field]!.GetValue<string>())!;
            nested[0]!["actor"] = "axis"; packet["source"]![field] = Encoding.UTF8.GetString(Encode(nested)); Reject(Encode(packet), "004");
        }
    }
    [Fact]
    public void EmptyReleaseRequiresExactUntimedSystemOpenCompleteAndPreservesNullAudit()
    {
        using var f = Fixture(); var row = f.RootElement.GetProperty("traces")[0];
        foreach (var index in new[] { 0, 1 })
            foreach (var field in new[] { "actor", "admittedAt", "clockAvailable" })
            {
                var packet = JsonNode.Parse(Text(row, "packetCanonicalUtf8"))!;
                packet["releaseInputs"]![index]![field] = field switch { "actor" => JsonValue.Create("axis"), "admittedAt" => JsonValue.Create(0), _ => JsonValue.Create(false) };
                Reject(Encode(packet), "005");
            }
        foreach (var mode in new[] { "missing", "extra", "reordered", "forged" })
        {
            var packet = JsonNode.Parse(Text(row, "packetCanonicalUtf8"))!;
            var inputs = packet["releaseInputs"]!.AsArray(); var events = packet["releaseEventCanonicalUtf8"]!.AsArray();
            if (mode == "missing") { inputs.RemoveAt(1); events.RemoveAt(1); }
            if (mode == "extra") { inputs.Add(inputs[1]!.DeepClone()); events.Add(events[1]!.DeepClone()); }
            if (mode == "reordered") { var first = events[0]!.DeepClone(); events[0] = events[1]!.DeepClone(); events[1] = first; }
            if (mode == "forged") events[0] = "{}";
            Reject(Encode(packet), "005");
        }
    }
    [Fact]
    public void VersionProfileCanonicalCapacityDepthAndArrayLimitsReject()
    {
        using var f = Fixture(); var row = f.RootElement.GetProperty("traces")[0]; var text = Text(row, "packetCanonicalUtf8");
        Reject([], "001"); Reject(new byte[1_048_577], "001"); Reject(Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)), "001");
        Reject(Encoding.UTF8.GetBytes(text + "\n"), "008");
        Reject(Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"contractVersion\":true", StringComparison.Ordinal)), "001");
        Reject(Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"contractVersion\":1.0", StringComparison.Ordinal)), "001");
        Reject(Encoding.UTF8.GetBytes("{\"contractVersion\":1," + text[1..]), "001");
        var packet = JsonNode.Parse(text)!; packet["extra"] = 1; Reject(Encode(packet), "001");
        packet = JsonNode.Parse(text)!; packet["contractVersion"] = 2; Reject(Encode(packet), "003");
        packet = JsonNode.Parse(text)!; packet["source"]!["trustLabel"] = "actual-history"; Reject(Encode(packet), "003");
        packet = JsonNode.Parse(text)!; packet["source"]!["sourceId"] = "unknown.axis.attacker"; Reject(Encode(packet), "004");
        packet = JsonNode.Parse(text)!; packet["source"]!["roundEventCanonicalUtf8"] = new JsonArray(Enumerable.Range(0, 17).Select(_ => (JsonNode)JsonValue.Create("{}")!).ToArray()); Reject(Encode(packet), "001");
        packet = JsonNode.Parse(text)!; packet["source"]!["movementEnd"]!["proof"]!["excludedBefore"] = new JsonArray(Enumerable.Range(0, 513).Select(_ => packet["source"]!["movementEnd"]!["proof"]!["endLocations"]![0]!["unit"]!.DeepClone()).ToArray()); Reject(Encode(packet), "001");
    }
    [Fact]
    public void FullyResignedNativeResultClockAndReleaseCannotSelfAdmit()
    {
        using var f = Fixture(); var row = f.RootElement.GetProperty("traces").EnumerateArray().Single(r => Text(r, "sourceId") == "zero-retreat.axis.attacker");
        using var results = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-result-settlement-v2.json")));
        var resultRow = results.RootElement.GetProperty("traces").EnumerateArray().Single(r => Text(r, "name") == "zero-retreat.axis.attacker"); var test = CombatResolutionTests.Case(resultRow);
        var inputs = new List<CombatResolutionInput>(); var events = new List<byte[]>();
        foreach (var i in resultRow.GetProperty("resultInputs").EnumerateArray())
        {
            var input = CampaignCombatResolutionCodec.ReadInput(JsonSerializer.SerializeToUtf8Bytes(i));
            var state = CampaignCombatResolution.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events);
            input = input with { AdmittedAt = input.AdmittedAt is { } at ? at + 100 : null, Command = input.Command with { DecisionId = input.Command.Kind == "choose" ? state.Window!.DecisionId : null } };
            var accepted = CampaignCombatResolution.ApplyTrustedBoundary(test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events, input);
            Assert.Equal(CombatStepsDisposition.Accepted, accepted.Disposition); inputs.Add(input); events.Add(accepted.EventBytes!);
        }
        var source = new CombatResultReleaseSource(Text(resultRow, "name"), test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events);
        var initial = CampaignCombatResultRelease.Replay(source, [], []); Assert.True(initial.Result.Closed);
        var releaseInputs = new List<CombatReleaseInput>(); var releaseEvents = new List<byte[]>(); var release = initial.Release;
        foreach (var kind in new[] { "open", "complete" })
        {
            var input = new CombatReleaseInput(new(1, kind, release.ReleaseId, release.StateVersion), CampaignOpeningPreambleActor.System);
            var accepted = CampaignCombatResultRelease.Apply(source, releaseInputs, releaseEvents, input); Assert.Equal(CombatStepsDisposition.Accepted, accepted.Disposition);
            releaseInputs.Add(input); releaseEvents.Add(accepted.EventBytes!); release = accepted.State;
        }
        Assert.Equal("completed", release.Status);
        var packet = JsonNode.Parse(Text(row, "packetCanonicalUtf8"))!; var s = packet["source"]!;
        s["resultInputsCanonicalUtf8"] = Encoding.UTF8.GetString(Encode(new JsonArray(inputs.Select(i => JsonNode.Parse(CampaignCombatResolutionCodec.SerializeInput(i))).ToArray())));
        s["resultEventCanonicalUtf8"] = new JsonArray(events.Select(e => (JsonNode)JsonValue.Create(Encoding.UTF8.GetString(e))!).ToArray());
        s["resultStateCanonicalUtf8"] = Encoding.UTF8.GetString(CampaignCombatResolutionCodec.SerializeState(initial.Result));
        packet["releaseBaseCanonicalUtf8"] = Encoding.UTF8.GetString(CampaignCombatReserveReleaseCodec.SerializeBase(initial.Basis, test.Request));
        packet["releaseInputs"] = new JsonArray(releaseInputs.Select(i => JsonNode.Parse(CampaignCombatReserveReleaseCodec.SerializeInput(i))).ToArray());
        packet["releaseEventCanonicalUtf8"] = new JsonArray(releaseEvents.Select(e => (JsonNode)JsonValue.Create(Encoding.UTF8.GetString(e))!).ToArray());
        Reject(Encode(packet), "004");
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
    public void EveryAdmittedRowRetainsIndependentLiteralMovementArithmetic(string name, int count, int after, int breakOff, int dp)
    {
        using var fixture = Fixture(); var contexts = 0;
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray().Where(r => Text(r, "sourceId").StartsWith(name + ".", StringComparison.Ordinal)))
        {
            using var proof = JsonDocument.Parse(Bridge(row)); var p = proof.RootElement; var witnesses = p.GetProperty("witnesses");
            Assert.Equal(count, witnesses.GetArrayLength());
            var ownId = Text(p.GetProperty("members")[0].GetProperty("unit"), "elementId");
            var own = p.GetProperty("world").GetProperty("elements").EnumerateArray().Single(e => Text(e, "elementId") == ownId);
            Assert.Equal(0, own.GetProperty("ammunition").GetProperty("points").GetInt32());
            var spent = p.GetProperty("members")[0].GetProperty("spentCp").GetProperty("numerator").GetInt64();
            Assert.Equal(name == "attacker-capture-guard-cp-limit" ? 10 : 5, spent);
            Assert.All(witnesses.EnumerateArray(), w =>
            {
                Assert.Equal(2, w.GetProperty("terrainCost").GetInt32()); Assert.Equal(breakOff, w.GetProperty("breakOffCost").GetInt32());
                Assert.Equal(after, w.GetProperty("afterCp").GetInt64()); Assert.Equal(dp, w.GetProperty("excessCpDp").GetInt32());
                Assert.Equal(spent + 2 + breakOff, after); Assert.False(w.GetProperty("usesReleaseException").GetBoolean());
            });
            contexts++;
        }
        Assert.Equal(4, contexts);
    }
    [Fact]
    public void IndependentArithmeticProbesDoNotAdmitCampaignHistory()
    {
        using var results = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-result-settlement-v2.json")));
        var test = CombatResolutionTests.Case(results.RootElement.GetProperty("traces")[0]);
        var op = test.Boundary.World.Elements[0].OperationalState;
        CampaignElementOperationalStateV6 State(long spent, int cohesion = 10) => new(op.LedgerGameTurn, op.LedgerOperationStage,
            new(spent, 1), cohesion, op.VehicleBreakdownState, op.MovementEnded, op.InitialLedgerOrigin);
        CampaignCombatSpendResult Charge(long spent, long cost, CampaignCombatSpendCeiling ceiling = CampaignCombatSpendCeiling.Ordinary) =>
            CampaignCombatSpending.ChargeOrdinary(State(spent), new(cost, 1), 10, ceiling, "probe.element", "probe.receipt", []);
        var spent15 = Charge(9, 6); Assert.Equal(15, spent15.State.CapabilityPointsExpended.Numerator); Assert.Equal(5, spent15.Cause!.Points);
        Assert.Throws<ArgumentOutOfRangeException>(() => Charge(10, 6));
        Assert.Equal(2, Charge(11, 2).Cause!.Points);
        Assert.Equal(10, Charge(8, 2, CampaignCombatSpendCeiling.ReleasedReserveI).State.CapabilityPointsExpended.Numerator);
        Assert.Throws<ArgumentOutOfRangeException>(() => Charge(9, 2, CampaignCombatSpendCeiling.ReleasedReserveI));
        Assert.Equal(5, Charge(3, 2, CampaignCombatSpendCeiling.ReleasedReserveII).State.CapabilityPointsExpended.Numerator);
        Assert.Throws<ArgumentOutOfRangeException>(() => Charge(4, 2, CampaignCombatSpendCeiling.ReleasedReserveII));
        Assert.Throws<OverflowException>(() => Charge(long.MaxValue, 1));
        Assert.Throws<OverflowException>(() => CampaignCombatSpending.ChargeOrdinary(State(9, int.MinValue), new(6, 1), 10,
            CampaignCombatSpendCeiling.Ordinary, "probe.element", "probe.receipt", []));
    }
    [Fact]
    public void OuterUtf8StringsUseAsciiCanonicalEscapes()
    {
        using var f = Fixture(); var row = f.RootElement.GetProperty("traces")[0];
        var packet = JsonNode.Parse(Text(row, "packetCanonicalUtf8"))!; packet["source"]!["requestCanonicalUtf8"] = "é";
        var literal = Encode(packet); var ascii = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(literal).Replace("é", "\\u00e9", StringComparison.Ordinal));
        Assert.Equal(ascii, CampaignCombatSettledContinuationCodec.Bytes(CampaignCombatSettledContinuationCodec.Parse(ascii, "SettledPacket")));
        Reject(literal, "008"); Reject(ascii, "004");
    }
    private static void Reject(byte[] bytes, string code) => Assert.StartsWith("CMB-SCT-" + code, Assert.ThrowsAny<JsonException>(() => CampaignCombatSettledContinuation.Bridge(bytes, Context())).Message);
    private static byte[] Encode(JsonNode node) => CampaignCombatSettledContinuationCodec.Encode(node);
    private static IEnumerable<string[]> Leaves(JsonNode? node)
    {
        if (node is JsonObject obj) foreach (var pair in obj) foreach (var tail in Leaves(pair.Value)) yield return [pair.Key, .. tail];
        else if (node is JsonArray array) for (var i = 0; i < array.Count; i++) foreach (var tail in Leaves(array[i])) yield return [i.ToString(System.Globalization.CultureInfo.InvariantCulture), .. tail];
        else yield return [];
    }
    private static void MutateLeaf(JsonNode root, string[] path)
    {
        var parent = root; foreach (var key in path[..^1]) parent = parent is JsonArray a ? a[int.Parse(key, System.Globalization.CultureInfo.InvariantCulture)]! : parent[key]!;
        var last = path[^1]; var value = parent is JsonArray values ? values[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] : parent[last];
        JsonNode? replacement = value?.GetValueKind() switch
        {
            JsonValueKind.Number => JsonValue.Create(value.GetValue<long>() + 1),
            JsonValueKind.True or JsonValueKind.False => JsonValue.Create(!value.GetValue<bool>()),
            JsonValueKind.String => JsonValue.Create(value.GetValue<string>() + "x"),
            _ => JsonValue.Create("forged")
        };
        if (parent is JsonArray items) items[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] = replacement; else parent[last] = replacement;
    }
    private static byte[] Bridge(JsonElement row) => CampaignCombatSettledContinuationCodec.Serialize(CampaignCombatSettledContinuation.Bridge(Encoding.UTF8.GetBytes(Text(row, "packetCanonicalUtf8")), Context()));
    private static CampaignCombatCreationContext Context()
    {
        using var results = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-result-settlement-v2.json")));
        return CombatResolutionTests.Case(results.RootElement.GetProperty("traces")[0]).Request.Context;
    }
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-settled-continuation-v1.json")));
    private static string Text(JsonElement node, string name) => node.GetProperty(name).GetString()!;
}
