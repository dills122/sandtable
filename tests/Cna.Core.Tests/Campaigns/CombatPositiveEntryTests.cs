using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatPositiveEntryTests
{
    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void ActualIdleSourceReachesPositionDeterminationWithRealMovementProof(string side)
    {
        using var fixture = Fixture();
        var row = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("actor").GetString() == side);
        var source = new CombatPositiveEntrySource(Encoding.ASCII.GetBytes(row.GetProperty("sourceCanonicalUtf8").GetString()!), Context());
        var state = CampaignCombatPositiveEntry.Replay(source);
        Assert.Equal(13, state.Projection.GetProperty("stateVersion").GetInt64());
        Assert.Equal(12, state.Projection.GetProperty("receipts").GetArrayLength());
        Assert.Equal(JsonValueKind.Object, state.Projection.GetProperty("movementEnd").ValueKind);
        Assert.Equal(JsonValueKind.String, state.Projection.GetProperty("breakdownCompletionReceiptId").ValueKind);
        Assert.Equal("land.position.operation-1.first-player.movement-and-combat.combat.position-determination", state.Projection.GetProperty("sequencePosition").GetProperty("positionId").GetString());
    }
    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void FrozenCutsCommandsEventsAndFullProofsMatchWithOriginalByteRetries(string side)
    {
        using var fixture = Fixture();
        var row = Row(fixture, side);
        var context = Context();
        var terminal = JsonNode.Parse(row.GetProperty("sourceCanonicalUtf8").GetString()!)!.AsObject();
        var originalWorld = "";
        var stable = "";
        for (var cut = 0; cut <= 2; cut++)
        {
            var packet = Cut(terminal, cut);
            var bytes = Encode(packet);
            var source = CampaignCombatPositiveEntryCodec.ReadSource(bytes, context);
            var state = CampaignCombatPositiveEntry.Replay(source);
            Assert.Equal(bytes, source.CanonicalBytes);
            Assert.Equal(11 + cut, state.Projection.GetProperty("stateVersion").GetInt64());
            Assert.Equal(10 + cut, state.Projection.GetProperty("receipts").GetArrayLength());
            Assert.Equal(27, state.Projection.EnumerateObject().Count());
            Assert.Equal(Ascii(row.GetProperty("proofCanonicalUtf8")[cut]), state.ProofBytes);
            Golden(row, $"proof-{cut}", state.ProofBytes);
            Golden(row, $"state-{11 + cut}", state.CanonicalBytes);
            Assert.Equal(state.ProofBytes, CampaignCombatPositiveEntryCodec.ReadProof(state.ProofBytes, source).ProofBytes);
            Assert.Equal(cut == 2 ? JsonValueKind.Object : JsonValueKind.Null, state.Proof.GetProperty("candidate").ValueKind);
            Assert.Equal(JsonValueKind.Null, state.Projection.GetProperty("sequencePosition").GetProperty("activeSide").ValueKind);
            Assert.Equal("idle", state.Projection.GetProperty("breakdownFlow").GetProperty("kind").GetString());
            Assert.Empty(state.Projection.GetProperty("tracks").EnumerateArray());
            Assert.Empty(state.Projection.GetProperty("actualProgressRefs").EnumerateArray());
            var world = state.Projection.GetProperty("world").GetRawText();
            if (cut == 0) { originalWorld = world; stable = Stable(state.Projection); }
            Assert.Equal(originalWorld, world); Assert.Equal(stable, Stable(state.Projection));
            Assert.Equal(1UL, state.Projection.GetProperty("randomState").GetProperty("seed").GetUInt64());
            Assert.Equal(2, state.Projection.GetProperty("randomState").GetProperty("nextByteCursor").GetInt64());
            foreach (var element in state.Projection.GetProperty("world").GetProperty("elements").EnumerateArray())
            {
                Assert.Equal(0, element.GetProperty("operationalState").GetProperty("capabilityPointsExpended").GetProperty("numerator").GetInt64());
                Assert.Equal(0, element.GetProperty("operationalState").GetProperty("cohesionLevel").GetInt32());
                Assert.Equal(10, element.GetProperty("ammunition").GetProperty("points").GetInt32());
                Assert.Equal(10, Assert.Single(element.GetProperty("components").EnumerateArray()).GetProperty("currentToe").GetInt32());
            }
            for (var prior = 0; prior < cut; prior++)
            {
                var retry = CampaignCombatPositiveEntry.Apply(source, Ascii(row.GetProperty("inputCanonicalUtf8")[prior]));
                Assert.True(retry.Duplicate); Assert.Equal(Ascii(row.GetProperty("eventCanonicalUtf8")[prior]), retry.EventBytes);
                Assert.Equal(state.ProofBytes, retry.State.ProofBytes);
            }
            if (cut < 2)
            {
                var kind = cut == 0 ? "complete-movement-segment" : "complete-breakdown-segment";
                var input = CampaignCombatPositiveEntry.Command(source, kind);
                Assert.Equal(Ascii(row.GetProperty("inputCanonicalUtf8")[cut]), input); Golden(row, $"input-{cut + 1}", input);
                var result = CampaignCombatPositiveEntry.Apply(source, input);
                Assert.False(result.Duplicate); Assert.Equal(Ascii(row.GetProperty("eventCanonicalUtf8")[cut]), result.EventBytes);
                Golden(row, $"event-{cut + 1}", result.EventBytes);
                Assert.Equal(Ascii(row.GetProperty("proofCanonicalUtf8")[cut + 1]), result.State.ProofBytes);
                var e = JsonNode.Parse(result.EventBytes)!;
                Assert.Equal(cut == 0 ? side : "system", e["input"]!["actor"]!.GetValue<string>());
                Assert.Equal(11 + cut, e["priorStateVersion"]!.GetValue<long>());
                Assert.Equal(12 + cut, e["stateVersion"]!.GetValue<long>());
                Assert.Equal(Receipt(e, cut), e["receiptId"]!.GetValue<string>());
                Assert.Equal(CampaignOpeningPreambleCodec.EventPrefix(state.Projection.GetProperty("prefix").GetString()!, result.EventBytes), result.State.Projection.GetProperty("prefix").GetString());
            }
            else
            {
                var end = state.Projection.GetProperty("movementEnd");
                var moveEvent = JsonNode.Parse(row.GetProperty("eventCanonicalUtf8")[0].GetString()!)!;
                Assert.Equal(moveEvent["receiptId"]!.GetValue<string>(), end.GetProperty("completionReceiptId").GetString());
                Assert.Equal(moveEvent["endLocations"]!.ToJsonString(), JsonNode.Parse(end.GetProperty("endLocations").GetRawText())!.ToJsonString());
                Assert.Empty(end.GetProperty("excludedBefore").EnumerateArray());
                Assert.Equal(1, end.GetProperty("ordinal").GetInt32());
                Assert.NotEqual(end.GetProperty("completionReceiptId").GetString(), state.Projection.GetProperty("completionReceiptId").GetString());
                Assert.NotEqual(end.GetProperty("completionReceiptId").GetString(), state.Projection.GetProperty("breakdownCompletionReceiptId").GetString());
                Assert.Equal(side, state.Proof.GetProperty("candidate").GetProperty("attacker").GetProperty("unit").GetProperty("originalSide").GetString());
                Assert.Equal("voluntary-adjacent", state.Proof.GetProperty("candidate").GetProperty("basis").GetString());
                foreach (var kind in new[] { "complete-movement-segment", "complete-breakdown-segment" })
                    Reject(() => CampaignCombatPositiveEntry.Apply(source, CampaignCombatPositiveEntry.Command(source, kind)), 6);
            }
        }
        Golden(row, "source", Encode(terminal));
    }

    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void EveryInputLeafClockForeignScopeAndConsumedOccurrenceReject(string side)
    {
        using var fixture = Fixture(); var row = Row(fixture, side); var terminal = Packet(row); var context = Context();
        for (var cut = 0; cut <= 2; cut++)
        {
            var source = CampaignCombatPositiveEntryCodec.ReadSource(Encode(Cut(terminal, cut)), context);
            for (var index = 0; index < 2; index++)
            {
                var input = JsonNode.Parse(row.GetProperty("inputCanonicalUtf8")[index].GetString()!)!;
                foreach (var bad in Mutations(input)) Reject(() => CampaignCombatPositiveEntry.Apply(source, Encode(bad)));
                foreach (var actor in Actors.Where(x => x != input["actor"]!.GetValue<string>()))
                { var bad = input.DeepClone(); bad["actor"] = actor; Reject(() => CampaignCombatPositiveEntry.Apply(source, Encode(bad)), 5); }
                foreach (var clock in new[] { "admittedAt", "clockAvailable", "deadline" })
                { var bad = input.DeepClone(); bad[clock] = clock == "clockAvailable" ? JsonValue.Create(true) : JsonValue.Create("2000-01-01T00:00:00.000Z"); Reject(() => CampaignCombatPositiveEntry.Apply(source, Encode(bad)), 1); }
                foreach (var field in new[] { "creationBinding", "creationEventHash", "cycleId" })
                { var bad = input.DeepClone(); bad["command"]![field] = field == "creationBinding" ? "creation.foreign" : "sha256:" + new string('0', 64); Reject(() => CampaignCombatPositiveEntry.Apply(source, Encode(bad)), 4); }
                foreach (var version in new long[] { -1, 0, 9, 10, 13, 14, long.MaxValue })
                { var bad = input.DeepClone(); bad["command"]!["expectedPriorVersion"] = version; Reject(() => CampaignCombatPositiveEntry.Apply(source, Encode(bad)), 6); }
                foreach (var v in new JsonNode[] { JsonValue.Create(true)!, JsonValue.Create(11.0)! })
                { var bad = input.DeepClone(); bad["command"]!["expectedPriorVersion"] = v.DeepClone(); var data = Encode(bad); if (v.GetValueKind() == JsonValueKind.Number) data = Encoding.ASCII.GetBytes(Encoding.ASCII.GetString(data).Replace("\"expectedPriorVersion\":11", "\"expectedPriorVersion\":11.0", StringComparison.Ordinal)); Reject(() => CampaignCombatPositiveEntry.Apply(source, data), 1); }
            }
        }
    }

    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void EveryOriginalSourceRecordLeafAndHistoryCutRejects(string side)
    {
        using var fixture = Fixture(); var row = Row(fixture, side); var terminal = Packet(row); var context = Context();
        foreach (var key in SourceKeys)
        {
            var records = terminal[key] is JsonArray array ? array.Select(x => x!.GetValue<string>()).ToArray() : new[] { terminal[key]!.GetValue<string>() };
            for (var index = 0; index < records.Length; index++)
            {
                var original = JsonNode.Parse(records[index])!;
                foreach (var bad in Mutations(original).Concat(new[] { WithExtra(original, "admittedAt"), WithExtra(original, "completedHistory") }))
                {
                    var packet = terminal.DeepClone();
                    if (packet[key] is JsonArray list) list[index] = Encoding.ASCII.GetString(Encode(bad)); else packet[key] = Encoding.ASCII.GetString(Encode(bad));
                    Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(packet), context));
                }
            }
            if (terminal[key] is not JsonArray history) continue;
            for (var cut = 0; cut < history.Count; cut++)
            { var bad = terminal.DeepClone(); bad[key] = new JsonArray(history.Take(cut).Select(x => x!.DeepClone()).ToArray()); Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(bad), context), 7); }
            if (history.Count > 1)
            { var bad = terminal.DeepClone(); bad[key] = new JsonArray(history.Reverse().Select(x => x!.DeepClone()).ToArray()); Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(bad), context), 4); }
        }
        var other = Packet(Row(fixture, side == "axis" ? "commonwealth" : "axis"));
        foreach (var key in SourceKeys.Append("entryEventCanonicalUtf8"))
        {
            if (terminal[key]!.ToJsonString() == other[key]!.ToJsonString()) continue;
            var bad = terminal.DeepClone(); bad[key] = other[key]!.DeepClone(); Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(bad), context));
        }
        foreach (var key in new[] { "world", "entry", "candidate", "movementEnd", "clockAvailable" })
            Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(WithExtra(terminal, key)), context), 1);
    }

    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void EveryEventLeafAndResignedForgeryRejects(string side)
    {
        using var fixture = Fixture(); var row = Row(fixture, side); var terminal = Packet(row); var context = Context();
        for (var index = 0; index < 2; index++)
        {
            var original = JsonNode.Parse(row.GetProperty("eventCanonicalUtf8")[index].GetString()!)!;
            var mutation = 0;
            foreach (var bad in Mutations(original).Concat(new[] { WithExtra(original, "completedHistory") }))
            {
                var packet = Cut(terminal, index + 1); packet["entryEventCanonicalUtf8"]![index] = Encoding.ASCII.GetString(Encode(bad));
                Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(packet), context), EventLeafCodes[index][mutation] - '0');
                var signedCode = SignedEventLeafCodes[index][mutation++] - '0';
                bad["receiptId"] = Receipt(bad, index);
                if (Encode(bad).AsSpan().SequenceEqual(Encode(original))) continue; // Receipt-only mutation restored genuine event.
                packet["entryEventCanonicalUtf8"]![index] = Encoding.ASCII.GetString(Encode(bad));
                Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(packet), context), signedCode);
            }
            Assert.Equal(EventLeafCodes[index].Length, mutation);
            var clock = original.DeepClone(); clock["input"]!["admittedAt"] = "2000-01-01T00:00:00.000Z"; clock["receiptId"] = Receipt(clock, index);
            var clockPacket = Cut(terminal, index + 1); clockPacket["entryEventCanonicalUtf8"]![index] = Encoding.ASCII.GetString(Encode(clock));
            Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(clockPacket), context), 4);
        }
        foreach (var records in new[] { new[] { 1 }, new[] { 1, 0 }, new[] { 0, 0 }, new[] { 0, 1, 1 } })
        {
            var bad = terminal.DeepClone(); bad["entryEventCanonicalUtf8"] = new JsonArray(records.Select(i => terminal["entryEventCanonicalUtf8"]![i]!.DeepClone()).ToArray());
            Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(bad), context), records.Length > 2 ? 7 : 6);
        }
    }

    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void EveryProofLeafAndCacheOnlyAdmissionRejects(string side)
    {
        using var fixture = Fixture(); var row = Row(fixture, side); var terminal = Packet(row); var context = Context();
        for (var cut = 0; cut <= 2; cut++)
        {
            var source = CampaignCombatPositiveEntryCodec.ReadSource(Encode(Cut(terminal, cut)), context);
            var proof = JsonNode.Parse(row.GetProperty("proofCanonicalUtf8")[cut].GetString()!)!;
            foreach (var bad in Mutations(proof)) Reject(() => CampaignCombatPositiveEntryCodec.ReadProof(Encode(bad), source));
            foreach (var extra in new[] { "completedHistory", "selection", "result", "admittedAt", "clockAvailable" })
                Reject(() => CampaignCombatPositiveEntryCodec.ReadProof(Encode(WithExtra(proof, extra)), source), 1);
            var other = Row(fixture, side == "axis" ? "commonwealth" : "axis");
            Reject(() => CampaignCombatPositiveEntryCodec.ReadProof(Ascii(other.GetProperty("proofCanonicalUtf8")[cut]), source), 6);
            Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(proof), context), 1);
        }
    }

    [Fact]
    public void ClosedCanonicalSyntaxSizeArrayDepthAndErrorOrderingMatchContract()
    {
        using var fixture = Fixture(); var row = Row(fixture, "axis"); var terminal = Packet(row); var context = Context();
        var values = new[] { ("PositiveSource", Encode(terminal)), ("PositiveEntryProof", Ascii(row.GetProperty("proofCanonicalUtf8")[2])),
            ("LifecycleInput", Ascii(row.GetProperty("inputCanonicalUtf8")[0])), ("EntryInput", Ascii(row.GetProperty("inputCanonicalUtf8")[1])),
            ("CompleteEvent", Ascii(row.GetProperty("eventCanonicalUtf8")[0])), ("EntryEvent", Ascii(row.GetProperty("eventCanonicalUtf8")[1])) };
        foreach (var (kind, bytes) in values)
        {
            var text = Encoding.ASCII.GetString(bytes);
            var reversed = new JsonObject(JsonNode.Parse(bytes)!.AsObject().Reverse().Select(x => new KeyValuePair<string, JsonNode?>(x.Key, x.Value?.DeepClone())));
            foreach (var data in new[] { Encoding.ASCII.GetBytes(text + " "), Encoding.ASCII.GetBytes(text + "\n"), Encode(reversed), Encoding.ASCII.GetBytes(text.Replace("\"contractVersion\"", "\"\\u0063ontractVersion\"", StringComparison.Ordinal)) })
                Reject(() => CampaignCombatPositiveEntryCodec.Parse(data, kind), 8);
            foreach (var data in new[] { new byte[] { 0xff }, Encoding.ASCII.GetBytes("null"), Encoding.ASCII.GetBytes("[]"), Encoding.ASCII.GetBytes("{"),
                Encoding.ASCII.GetBytes("{\"contractVersion\":1,\"contractVersion\":1," + text[1..]), Encoding.ASCII.GetBytes(text[..^1] + ",\"extra\":null}"),
                Encoding.ASCII.GetBytes(new string('[', 40) + "0" + new string(']', 40)), new byte[] { 0xef, 0xbb, 0xbf }.Concat(bytes).ToArray() })
                Reject(() => CampaignCombatPositiveEntryCodec.Parse(data, kind), 1);
        }
        foreach (var key in SourceKeys.Skip(2).Append("entryEventCanonicalUtf8"))
        {
            var bad = terminal.DeepClone(); bad[key] = new JsonArray(Enumerable.Range(0, 513).Select(_ => (JsonNode?)JsonValue.Create("{}")).ToArray());
            Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(bad), context), 1);
            bad[key] = new JsonArray(Enumerable.Range(0, 512).Select(_ => (JsonNode?)JsonValue.Create("{}")).ToArray());
            _ = CampaignCombatPositiveEntryCodec.Parse(Encode(bad), "PositiveSource");
            Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(bad), context), 7);
        }
        foreach (var carrier in new[] { "", "é", "a" + new string('a', 1_048_576) })
        { var bad = terminal.DeepClone(); bad["createdCanonicalUtf8"] = carrier; Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(bad), context), 1); }
        Reject(() => CampaignCombatPositiveEntryCodec.ReadSource([], context), 1);
        Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(new byte[1_048_577], context), 1);
        _ = CampaignCombatPositiveEntryCodec.Parse(Encoding.ASCII.GetBytes("\"" + new string('a', 1_048_574) + "\""), "utf8");
        Reject(() => CampaignCombatPositiveEntryCodec.Parse(Encoding.ASCII.GetBytes("\"" + new string('a', 1_048_575) + "\""), "utf8"), 1);
        var wrongVersion = terminal.DeepClone(); wrongVersion["contractVersion"] = 2; wrongVersion["reserveEventCanonicalUtf8"] = new JsonArray();
        Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(wrongVersion), context), 3);
        var source = CampaignCombatPositiveEntryCodec.ReadSource(Encode(terminal), context);
        var input = JsonNode.Parse(row.GetProperty("inputCanonicalUtf8")[0].GetString()!)!;
        input["actor"] = "system"; input["command"]!["creationBinding"] = "creation.foreign";
        Reject(() => CampaignCombatPositiveEntry.Apply(source, Encode(input)), 5);
        input["command"]!["contractVersion"] = 3; Reject(() => CampaignCombatPositiveEntry.Apply(source, Encode(input)), 3);
    }

    [Fact]
    public void CallerCannotMutateRetainedSourceEventStateOrCandidateBytes()
    {
        using var fixture = Fixture(); var row = Row(fixture, "axis"); var context = Context(); var original = Ascii(row.GetProperty("sourceCanonicalUtf8"));
        var supplied = original.ToArray(); var source = CampaignCombatPositiveEntryCodec.ReadSource(supplied, context); Array.Clear(supplied);
        var read = source.CanonicalBytes; Array.Clear(read); Assert.Equal(original, source.CanonicalBytes);
        var state = CampaignCombatPositiveEntry.Replay(source); var expected = state.ProofBytes; Array.Clear(state.ProofBytes); Array.Clear(state.CanonicalBytes);
        var proof = JsonNode.Parse(state.Proof.GetRawText())!; proof["entry"]!["world"]!["elements"]!.AsArray().Clear(); proof["candidate"]!["attacker"]!["componentIds"]!.AsArray().Clear();
        Assert.Equal(expected, CampaignCombatPositiveEntry.Replay(source).ProofBytes); Assert.Equal(expected, state.ProofBytes);
        var retry = CampaignCombatPositiveEntry.Apply(source, Ascii(row.GetProperty("inputCanonicalUtf8")[0])); var eventBytes = retry.EventBytes; Array.Clear(eventBytes);
        Assert.Equal(Ascii(row.GetProperty("eventCanonicalUtf8")[0]), retry.EventBytes);
        var sourceNode = JsonNode.Parse(source.CanonicalBytes)!; sourceNode["entryEventCanonicalUtf8"]!.AsArray().Clear(); Assert.Equal(original, source.CanonicalBytes);
    }

    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void UnsupportedActualOpeningsAndOldRouteAdmissionStayClosed(string side)
    {
        using var fixture = Fixture(); var row = Row(fixture, side); var context = Context();
        var choice = side == "axis" ? InitiativeOrderChoice.ActFirst : InitiativeOrderChoice.ActLast;
        foreach (var (seed, designated) in new[] { (0UL, false), (1UL, true), (11UL, false), (12UL, false) })
            Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(ActualOpening(context, seed, choice, designated)), context));
        var packet = ActualOpening(context, 1, choice, false);
        Assert.Equal(Encode(Cut(Packet(row), 0)), Encode(packet));
        var source = CampaignCombatPositiveEntryCodec.ReadSource(Encode(packet), context);
        var request = CampaignCombatCreationRequestCodec.Deserialize(Encoding.ASCII.GetBytes(packet["requestCanonicalUtf8"]!.GetValue<string>()), context);
        byte[][] History(string key) => packet[key]!.AsArray().Select(x => Encoding.ASCII.GetBytes(x!.GetValue<string>())).ToArray();
        var created = Encoding.ASCII.GetBytes(packet["createdCanonicalUtf8"]!.GetValue<string>());
        Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Replay(request, created, History("preambleEventCanonicalUtf8"), History("weatherEventCanonicalUtf8"), History("stageEventCanonicalUtf8"), History("reserveEventCanonicalUtf8"), [], []));
        Assert.ThrowsAny<JsonException>(() => CampaignEventSerializer.Deserialize(Ascii(row.GetProperty("eventCanonicalUtf8")[0])));
        Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(CampaignCombatPositiveEntry.Replay(source).CanonicalBytes, created, request));
    }
    private static JsonObject ActualOpening(CampaignCombatCreationContext context, ulong seed, InitiativeOrderChoice choice, bool designated)
    {
        var request = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", seed, context);
        var created = CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(request));
        var preamble = new List<byte[]>();
        for (var i = 0; i < 4; i++)
        {
            var state = CampaignOpeningPreamble.Replay(request, created, preamble);
            preamble.Add(CampaignOpeningPreamble.Apply(request, created, preamble, CampaignOpeningPreamble.Command(state, i == 3 ? choice : null)).EventBytes);
        }
        var weatherState = CampaignCombatWeather.Replay(request, created, preamble, []);
        var weather = CampaignCombatWeather.Apply(request, created, preamble, [], CampaignCombatWeather.Command(weatherState)).EventBytes;
        var stage = new List<byte[]>();
        for (var i = 0; i < 4; i++)
        { var state = CampaignCombatStageEntry.Replay(request, created, preamble, [weather], stage); stage.Add(CampaignCombatStageEntry.Apply(request, created, preamble, [weather], stage, CampaignCombatStageEntry.Command(state)).EventBytes); }
        var reserve = new List<byte[]>();
        if (designated)
        { var state = CampaignCombatReserveDesignation.Replay(request, created, preamble, [weather], stage, []); reserve.Add(CampaignCombatReserveDesignation.Apply(request, created, preamble, [weather], stage, [], CampaignCombatReserveDesignation.Command(state)).EventBytes); }
        var input = CampaignCombatReserveCompletion.CreateCommand(request, created, preamble, [weather], stage, reserve);
        reserve.Add(CampaignCombatReserveCompletion.Create(request, created, preamble, [weather], stage, reserve, input).EventBytes);
        static JsonArray Texts(IEnumerable<byte[]> records) => new(records.Select(x => (JsonNode?)JsonValue.Create(Encoding.ASCII.GetString(x))).ToArray());
        return new JsonObject
        {
            ["contractVersion"] = 1,
            ["requestCanonicalUtf8"] = Encoding.ASCII.GetString(CampaignCombatCreationRequestCodec.Serialize(request)),
            ["createdCanonicalUtf8"] = Encoding.ASCII.GetString(created),
            ["preambleEventCanonicalUtf8"] = Texts(preamble),
            ["weatherEventCanonicalUtf8"] = Texts([weather]),
            ["stageEventCanonicalUtf8"] = Texts(stage),
            ["reserveEventCanonicalUtf8"] = Texts(reserve),
            ["entryEventCanonicalUtf8"] = new JsonArray()
        };
    }

    [Fact]
    public void EventTagAndMalformedCarrierErrorsMatchOracleOrder()
    {
        using var fixture = Fixture(); var row = Row(fixture, "axis"); var context = Context();
        foreach (var (text, code) in new[] { ("[]", 4), ("null", 4), ("{}", 3), ("{", 4), ("{\"eventType\":0}", 3) })
        { var packet = Cut(Packet(row), 0); packet["entryEventCanonicalUtf8"]!.AsArray().Add(text); Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(packet), context), code); }
    }

    [Fact]
    public void Utf8CarrierCanonicalControlEscapesUseOracleLowercaseSpelling()
    {
        Assert.Equal("\x1f", CampaignCombatPositiveEntryCodec.Parse("\"\\u001f\""u8, "utf8").GetString());
        Assert.Equal("\x7f", CampaignCombatPositiveEntryCodec.Parse("\"\\u007f\""u8, "utf8").GetString());
        Reject(() => CampaignCombatPositiveEntryCodec.Parse("\"\\u007F\""u8, "utf8"), 8);
    }

    [Fact]
    public void RetainedReceiptValidationPrecedesActorAndOccurrenceAuthorization()
    {
        using var fixture = Fixture(); var row = Row(fixture, "axis"); var context = Context();
        foreach (var field in new[] { "receiptId", "actor", "expectedPriorVersion" })
        {
            var packet = Cut(Packet(row), 1); var e = JsonNode.Parse(row.GetProperty("eventCanonicalUtf8")[0].GetString()!)!;
            if (field == "receiptId") e[field] = "forged";
            else if (field == "actor") e["input"]![field] = "system";
            else e["input"]!["command"]![field] = 12;
            packet["entryEventCanonicalUtf8"]![0] = Encoding.ASCII.GetString(Encode(e));
            Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(packet), context), 4);
        }
        var crossed = Cut(Packet(row), 2); var bd = JsonNode.Parse(row.GetProperty("eventCanonicalUtf8")[1].GetString()!)!;
        bd["input"]!["command"]!["kind"] = "complete-movement-segment"; bd["receiptId"] = Receipt(bd, 1);
        crossed["entryEventCanonicalUtf8"]![1] = Encoding.ASCII.GetString(Encode(bd));
        Reject(() => CampaignCombatPositiveEntryCodec.ReadSource(Encode(crossed), context), 1);
    }

    // Literal error codes independently computed from unchanged 019E0 oracle.
    private static readonly string[] EventLeafCodes = ["43444444444444444444444444444444444444444444444444444444", "434444444444444444444444444444444444444444444444444"];
    private static readonly string[] SignedEventLeafCodes = ["43666666466666666666444464666666663464446644446664664404", "436664666666666666644446664444666666666336644466404"];
    private static readonly string[] Actors = ["axis", "commonwealth", "system"];
    private static readonly string[] SourceKeys = ["requestCanonicalUtf8", "createdCanonicalUtf8", "preambleEventCanonicalUtf8", "weatherEventCanonicalUtf8", "stageEventCanonicalUtf8", "reserveEventCanonicalUtf8"];
    private static readonly JsonSerializerOptions Options = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static JsonElement Row(JsonDocument fixture, string side) => fixture.RootElement.GetProperty("cases").EnumerateArray().Single(x => x.GetProperty("actor").GetString() == side);
    private static JsonObject Packet(JsonElement row) => JsonNode.Parse(row.GetProperty("sourceCanonicalUtf8").GetString()!)!.AsObject();
    private static JsonObject Cut(JsonObject terminal, int count)
    { var packet = terminal.DeepClone().AsObject(); packet["entryEventCanonicalUtf8"] = new JsonArray(terminal["entryEventCanonicalUtf8"]!.AsArray().Take(count).Select(x => x!.DeepClone()).ToArray()); return packet; }
    private static byte[] Encode(JsonNode value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);
    private static byte[] Ascii(JsonElement value) => Encoding.ASCII.GetBytes(value.GetString()!);
    private static string Stable(JsonElement value)
    {
        var node = JsonNode.Parse(value.GetRawText())!.AsObject();
        foreach (var key in new[] { "stateVersion", "prefix", "receipts", "sequencePosition", "movementEnd", "breakdownCompletionReceiptId" }) node.Remove(key);
        return node.ToJsonString();
    }
    private static string Receipt(JsonNode value, int index)
    {
        var unsigned = value.DeepClone().AsObject(); unsigned.Remove("receiptId");
        return (index == 0 ? "iml." : "ibc.") + CampaignOpeningPreambleCodec.HashWithDomain(index == 0
            ? "sandtable.combat.inherited-movement-completion-receipt.v3" : "sandtable.combat.inherited-breakdown-completion-receipt.v2", Encode(unsigned))[7..];
    }
    private static JsonNode WithExtra(JsonNode value, string key) { var node = value.DeepClone(); node[key] = null; return node; }
    private static void Reject(Action action, int? code = null)
    {
        var error = Assert.ThrowsAny<JsonException>(action);
        Assert.StartsWith("CMB-PEN-", error.Message, StringComparison.Ordinal);
        if (code.HasValue) Assert.Equal($"CMB-PEN-{code.Value:000}", error.Message);
    }
    private static void Golden(JsonElement row, string key, byte[] bytes)
    {
        var golden = row.GetProperty("goldens").GetProperty(key);
        Assert.Equal(golden.GetProperty("bytes").GetInt32(), bytes.Length);
        Assert.Equal(golden.GetProperty("sha256").GetString(), CampaignOpeningPreambleCodec.Hash(bytes));
    }
    private static IEnumerable<JsonNode> Mutations(JsonNode value)
    {
        foreach (var path in Leaves(value, []))
        {
            var changed = value.DeepClone(); var target = changed;
            foreach (var part in path[..^1]) target = target is JsonArray a ? a[int.Parse(part, System.Globalization.CultureInfo.InvariantCulture)]! : target[part]!;
            var key = path[^1]; var old = target is JsonArray list ? list[int.Parse(key, System.Globalization.CultureInfo.InvariantCulture)] : target[key];
            JsonNode replacement = old switch
            {
                null => JsonValue.Create("forged")!,
                JsonArray => new JsonArray("forged"),
                _ when old.GetValueKind() is JsonValueKind.True or JsonValueKind.False => JsonValue.Create(!old.GetValue<bool>())!,
                _ when old.GetValueKind() == JsonValueKind.Number => JsonValue.Create(old.GetValue<long>() + 1)!,
                _ => JsonValue.Create(old.GetValue<string>().StartsWith("sha256:", StringComparison.Ordinal) ? "sha256:" + new string('0', 64) : "forged")!
            };
            if (target is JsonArray array) array[int.Parse(key, System.Globalization.CultureInfo.InvariantCulture)] = replacement; else target[key] = replacement;
            yield return changed;
        }
    }
    private static IEnumerable<string[]> Leaves(JsonNode? value, string[] path)
    {
        if (value is JsonObject obj)
        { foreach (var field in obj) foreach (var child in Leaves(field.Value, [.. path, field.Key])) yield return child; }
        else if (value is JsonArray array && array.Count != 0)
        { for (var i = 0; i < array.Count; i++) foreach (var child in Leaves(array[i], [.. path, i.ToString(System.Globalization.CultureInfo.InvariantCulture)])) yield return child; }
        else yield return path;
    }
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
        "Campaigns", "Fixtures", "combat-positive-entry-v1.json")));
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
