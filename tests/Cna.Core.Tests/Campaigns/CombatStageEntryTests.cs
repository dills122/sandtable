using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatStageEntryTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var fixture = Fixture();
        foreach (var value in fixture.RootElement.GetProperty("cases").EnumerateArray())
            foreach (var choice in new[] { "act-first", "act-last" })
                yield return [value.GetProperty("seed").GetUInt64(), choice];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void FrozenTracesMatchEveryCutAndPreserveWeatherAuthority(ulong seed, string choice)
    {
        using var fixture = Fixture();
        var test = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("seed").GetUInt64() == seed);
        var goldens = test.GetProperty("goldens").GetProperty(choice);
        var (request, created, opening, weather) = Predecessor(seed, choice);
        CheckGolden(goldens, "creation", created);
        for (var index = 0; index < 4; index++) CheckGolden(goldens, $"preamble-{index + 1}", opening[index]);
        CheckGolden(goldens, "weather", weather);
        var events = new List<byte[]>();
        var inputs = new List<CampaignCombatStageEntryInput>();
        var before = CampaignCombatStageEntry.Replay(request, created, opening, [weather], events);
        var immutable = ImmutableFields(CampaignCombatStageEntryCodec.SerializeState(before));
        Assert.Equal(test.GetProperty("weatherKind").GetString(), CampaignOperationStageWeatherCodec.FormatKind(Assert.Single(before.Weather.Weather).Kind));
        Assert.Equal(test.GetProperty("weatherCursor").GetUInt64(), before.Weather.RandomState.NextByteCursor);
        for (var cut = 0; cut <= 4; cut++)
        {
            var state = CampaignCombatStageEntry.Replay(request, created, opening, [weather], events);
            var bytes = CampaignCombatStageEntryCodec.SerializeState(state);
            CheckGolden(goldens, $"state-{cut + 6}", bytes);
            Assert.Equal(bytes, CampaignCombatStageEntryCodec.SerializeState(
                CampaignCombatStageEntry.ReadState(bytes, request, created, opening, [weather], events)));
            Assert.Equal(immutable, ImmutableFields(bytes));
            Assert.Equal(6 + cut, state.StateVersion);
            Assert.Equal(5 + cut, state.Receipts.Count);
            Assert.Equal(before.Receipts, state.Receipts.Take(5));
            using var encoded = JsonDocument.Parse(bytes);
            Assert.True(JsonElement.DeepEquals(fixture.RootElement.GetProperty("positions")[cut],
                encoded.RootElement.GetProperty("sequencePosition")));
            Assert.Throws<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(bytes, created, request));
            foreach (var accepted in Enumerable.Range(0, inputs.Count))
            {
                var retry = CampaignCombatStageEntry.Apply(request, created, opening, [weather], events, inputs[accepted]);
                Assert.True(retry.Duplicate);
                Assert.Equal(events[accepted], retry.EventBytes);
                Assert.Equal(bytes, CampaignCombatStageEntryCodec.SerializeState(retry.State));
                foreach (var actor in new[] { CampaignOpeningPreambleActor.Axis, CampaignOpeningPreambleActor.Commonwealth })
                    Assert.Throws<JsonException>(() => CampaignCombatStageEntry.Apply(request, created, opening, [weather], events,
                        inputs[accepted] with { Actor = actor }));
            }
            if (cut == 4) break;
            var input = CampaignCombatStageEntry.Command(state);
            var command = CampaignCombatStageEntryCodec.SerializeInput(input);
            CheckGolden(goldens, $"input-{cut + 1}", command);
            Assert.Equal(input, CampaignCombatStageEntryCodec.DeserializeInput(command));
            foreach (var actor in new[] { CampaignOpeningPreambleActor.Axis, CampaignOpeningPreambleActor.Commonwealth })
                Assert.Throws<JsonException>(() => CampaignCombatStageEntry.Apply(request, created, opening, [weather], events,
                    input with { Actor = actor }));
            var result = CampaignCombatStageEntry.Apply(request, created, opening, [weather], events, input);
            Assert.False(result.Duplicate);
            CheckGolden(goldens, $"event-{cut + 1}", result.EventBytes);
            using var eventDocument = JsonDocument.Parse(result.EventBytes);
            Assert.True(JsonElement.DeepEquals(fixture.RootElement.GetProperty("eventSources")[cut],
                eventDocument.RootElement.GetProperty("sources")));
            Assert.Equal(state.Prefix, eventDocument.RootElement.GetProperty("priorPrefix").GetString());
            Assert.Equal(Prefix(state.Prefix, result.EventBytes), result.State.Prefix);
            Assert.Equal(Receipt(JsonNode.Parse(result.EventBytes)!), result.State.Receipts[^1].ReceiptId);
            Assert.Equal(Digest(command), result.State.Receipts[^1].CommandHash);
            Assert.Equal(Digest(result.EventBytes), result.State.Receipts[^1].EventHash);
            Assert.ThrowsAny<JsonException>(() => CampaignV11PreambleCodec.Deserialize(result.EventBytes));
            inputs.Add(input);
            events.Add(result.EventBytes);
        }
        var final = CampaignCombatStageEntry.Replay(request, created, opening, [weather], events);
        Assert.Equal(LandActorRole.FirstActingSide, final.SequencePosition.ActorRole);
        Assert.Null(final.SequencePosition.ActiveSide);
        Assert.Equal(LandSide.Axis, final.Weather.Opening.InitiativeHolder);
        Assert.Equal(choice == "act-first" ? LandSide.Axis : LandSide.Commonwealth, final.Weather.Opening.Orders[0].FirstSide);
        Assert.Throws<JsonException>(() => CampaignCombatStageEntry.Command(final));
    }

    [Fact]
    public void AllFourExplicitNonePoliciesAreRequiredWithoutInferringFromEmptyWorld()
    {
        var (request, _, _, _) = Predecessor();
        var accepted = request.Context.Setup.StageEntry;
        CampaignCombatStageEntry.RequirePolicy(accepted);
        Assert.Throws<JsonException>(() => CampaignCombatStageEntry.RequirePolicy(null));
        for (var changed = 0; changed < 4; changed++)
        {
            var gates = Enumerable.Repeat(StageEntryObligationKind.ExplicitNone, 4).ToArray();
            gates[changed] = StageEntryObligationKind.HasObligations;
            var policy = new CampaignStageEntryPolicy(1, 1, 1, gates[0], gates[1], gates[2], gates[3], accepted.Sources);
            Assert.Throws<JsonException>(() => CampaignCombatStageEntry.RequirePolicy(policy));
        }
        Assert.Throws<JsonException>(() => CampaignCombatStageEntry.RequirePolicy(new CampaignStageEntryPolicy(1, 2, 1,
            StageEntryObligationKind.ExplicitNone, StageEntryObligationKind.ExplicitNone,
            StageEntryObligationKind.ExplicitNone, StageEntryObligationKind.ExplicitNone, accepted.Sources)));
        var setupBytes = CampaignSetupV7Codec.Serialize(request.Context.Setup);
        foreach (var field in new[] { "organization", "navalConvoyArrival", "fleetAssignment", "fleetRepair" })
            foreach (var value in new[] { "has-obligations", "unknown", null })
            {
                var node = JsonNode.Parse(setupBytes)!;
                node["stageEntry"]![field] = value;
                Assert.ThrowsAny<JsonException>(() => CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(node.ToJsonString()),
                    request.Context.Setup.Artifact, request.Context.Setup.Scenario));
            }
    }

    [Fact]
    public void WrongPredecessorsOrderAndChangedRetryInputsReject()
    {
        var (request, created, opening, weather, inputs, events) = Trace();
        foreach (var suffix in new[] { events.Skip(1).ToArray(), events.AsEnumerable().Reverse().ToArray(),
            events.Concat([events[^1]]).ToArray(), events.Take(2).Concat(events.Skip(3)).ToArray(),
            Enumerable.Repeat(events[0], 4).ToArray() })
            Assert.Throws<JsonException>(() => CampaignCombatStageEntry.Replay(request, created, opening, [weather], suffix));
        foreach (var suffix in new[] { Array.Empty<byte[]>(), new[] { weather, weather }, new[] { opening[^1] }, new[] { weather.Concat(new byte[] { 32 }).ToArray() } })
            Assert.Throws<JsonException>(() => CampaignCombatStageEntry.Replay(request, created, opening, suffix, events));
        foreach (var prefix in new[] { opening.Take(3).ToArray(), opening.Skip(1).ToArray(), opening.Reverse().ToArray(), opening.Append(opening[^1]).ToArray() })
            Assert.Throws<JsonException>(() => CampaignCombatStageEntry.Replay(request, created, prefix, [weather], events));
        var foreign = Predecessor(1);
        Assert.Throws<JsonException>(() => CampaignCombatStageEntry.Replay(request, created, foreign.Opening, [foreign.Weather], events));
        var opposite = Trace(0, "act-last");
        Assert.Throws<JsonException>(() => CampaignCombatStageEntry.Replay(request, created, opposite.Opening, [opposite.Weather], events));
        var finalBytes = CampaignCombatStageEntryCodec.SerializeState(CampaignCombatStageEntry.Replay(request, created, opening, [weather], events));
        Assert.Throws<JsonException>(() => CampaignCombatStageEntry.ReadState(finalBytes, request, created,
            opposite.Opening, [opposite.Weather], opposite.Events));
        foreach (var original in inputs)
            foreach (var changed in new[]
            {
            original with { Command = original.Command with { ContractVersion = 1 } },
            original with { Command = original.Command with { Kind = "resolve-weather" } },
            original with { Command = original.Command with { ExpectedPriorVersion = 5 } },
            original with { Command = original.Command with { ExpectedPriorVersion = 10 } },
            original with { Command = original.Command with { ExpectedPositionId = "land.position.foreign" } },
            original with { Command = original.Command with { CreationBinding = "creation.foreign" } },
            original with { Command = original.Command with { CreationEventHash = "sha256:" + new string('0', 64) } },
        }) Assert.Throws<JsonException>(() => CampaignCombatStageEntry.Apply(request, created, opening, [weather], events, changed));
    }

    [Fact]
    public void ResignedInventedTransitionAndCacheCannotReplaceSourceReplay()
    {
        var (request, created, opening, weather, _, events) = Trace();
        var forged = JsonNode.Parse(events[0])!;
        using var fixture = Fixture();
        forged["sequencePosition"] = JsonNode.Parse(fixture.RootElement.GetProperty("positions")[2].GetRawText());
        forged["receiptId"] = Receipt(forged);
        var bytes = Encoding.UTF8.GetBytes(forged.ToJsonString());
        var before = CampaignCombatStageEntry.Replay(request, created, opening, [weather], []);
        var after = CampaignCombatStageEntry.Replay(request, created, opening, [weather], [events[0]]);
        var cache = JsonNode.Parse(CampaignCombatStageEntryCodec.SerializeState(after))!;
        cache["sequencePosition"] = forged["sequencePosition"]!.DeepClone();
        cache["prefix"] = Prefix(before.Prefix, bytes);
        cache["receipts"]![5]!["receiptId"] = forged["receiptId"]!.DeepClone();
        cache["receipts"]![5]!["eventHash"] = Digest(bytes);
        Assert.Equal(Receipt(forged), forged["receiptId"]!.GetValue<string>());
        Assert.Throws<JsonException>(() => CampaignCombatStageEntry.Replay(request, created, opening, [weather], [bytes]));
        Assert.Throws<JsonException>(() => CampaignCombatStageEntry.ReadState(Encoding.UTF8.GetBytes(cache.ToJsonString()),
            request, created, opening, [weather], [bytes]));
    }

    [Fact]
    public void ScalarRawAndWellFormedSemanticForgeriesRejectAtEveryCut()
    {
        var (request, created, opening, weather, inputs, events) = Trace();
        for (var cut = 0; cut <= 4; cut++)
        {
            var history = events.Take(cut).ToArray();
            var state = CampaignCombatStageEntry.Replay(request, created, opening, [weather], history);
            var stateBytes = CampaignCombatStageEntryCodec.SerializeState(state);
            foreach (var changed in Mutations(stateBytes))
                Assert.Throws<JsonException>(() => CampaignCombatStageEntry.ReadState(changed, request, created, opening, [weather], history));
            foreach (var field in new[] { "receipts", "operationStageOrders", "operationStageWeather" })
            {
                var missing = JsonNode.Parse(stateBytes)!;
                missing[field] = new JsonArray();
                Assert.Throws<JsonException>(() => CampaignCombatStageEntry.ReadState(Encoding.UTF8.GetBytes(missing.ToJsonString()),
                    request, created, opening, [weather], history));
            }
            if (cut == 4) continue;
            foreach (var changed in Mutations(events[cut]))
                Assert.ThrowsAny<JsonException>(() => CampaignCombatStageEntry.Replay(request, created, opening, [weather], history.Append(changed).ToArray()));
            foreach (var changed in Mutations(CampaignCombatStageEntryCodec.SerializeInput(inputs[cut])))
                Assert.ThrowsAny<JsonException>(() => CampaignCombatStageEntry.Apply(request, created, opening, [weather], history,
                    CampaignCombatStageEntryCodec.DeserializeInput(changed)));
            foreach (var (field, value) in new (string, JsonNode)[]
            {
                ("gameTurn", JsonValue.Create(2)!), ("operationStage", JsonValue.Create(2)!),
                ("priorPrefix", JsonValue.Create("sha256:" + new string('0', 64))!),
                ("receiptId", JsonValue.Create("ste." + new string('0', 64))!),
                ("sources", new JsonArray()),
            })
            {
                var forged = JsonNode.Parse(events[cut])!;
                forged[field] = value;
                if (field != "receiptId") forged["receiptId"] = Receipt(forged);
                var forgedBytes = Encoding.UTF8.GetBytes(forged.ToJsonString());
                Assert.NotEqual(events[cut], forgedBytes);
                Assert.Throws<JsonException>(() => CampaignCombatStageEntry.Replay(request, created, opening, [weather],
                    history.Append(forgedBytes).ToArray()));
            }
        }
    }

    [Fact]
    public void InputLimitsAndBufferIsolation()
    {
        var (request, created, opening, weather, inputs, events) = Trace();
        foreach (var invalid in new[] { Array.Empty<byte>(), "null"u8.ToArray(), "[]"u8.ToArray(), new byte[1_048_577] })
        {
            Assert.ThrowsAny<JsonException>(() => CampaignCombatStageEntryCodec.DeserializeInput(invalid));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatStageEntry.Replay(request, created, opening, [weather], [invalid]));
            Assert.Throws<JsonException>(() => CampaignCombatStageEntry.ReadState(invalid, request, created, opening, [weather], []));
        }
        var result = CampaignCombatStageEntry.Apply(request, created, opening, [weather], events, inputs[0]);
        var expected = CampaignCombatStageEntryCodec.SerializeState(result.State);
        Array.Fill(result.EventBytes, (byte)0);
        Array.Fill(created, (byte)0);
        Array.Fill(weather, (byte)0);
        foreach (var bytes in opening.Concat(events)) Array.Fill(bytes, (byte)0);
        Assert.Equal(expected, CampaignCombatStageEntryCodec.SerializeState(result.State));
    }

    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather,
        List<CampaignCombatStageEntryInput> Inputs, List<byte[]> Events) Trace(ulong seed = 0, string choice = "act-first")
    {
        var (request, created, opening, weather) = Predecessor(seed, choice);
        var events = new List<byte[]>();
        var inputs = new List<CampaignCombatStageEntryInput>();
        for (var index = 0; index < 4; index++)
        {
            var state = CampaignCombatStageEntry.Replay(request, created, opening, [weather], events);
            var input = CampaignCombatStageEntry.Command(state);
            inputs.Add(input);
            events.Add(CampaignCombatStageEntry.Apply(request, created, opening, [weather], events, input).EventBytes);
        }
        return (request, created, opening, weather, inputs, events);
    }

    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather) Predecessor(
        ulong seed = 0, string choice = "act-first")
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
        using var golden = JsonDocument.Parse(fixture.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
        var root = golden.RootElement;
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("setup").GetRawText()), artifact, scenario);
        var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
        var request = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", seed,
            new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, config));
        var created = CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(request));
        var opening = new List<byte[]>();
        for (var index = 0; index < 4; index++)
        {
            var state = CampaignOpeningPreamble.Replay(request, created, opening);
            var input = CampaignOpeningPreamble.Command(state, index == 3
                ? choice == "act-first" ? InitiativeOrderChoice.ActFirst : InitiativeOrderChoice.ActLast : null);
            opening.Add(CampaignOpeningPreamble.Apply(request, created, opening, input).EventBytes);
        }
        var weatherState = CampaignCombatWeather.Replay(request, created, opening, []);
        var weather = CampaignCombatWeather.Apply(request, created, opening, [], CampaignCombatWeather.Command(weatherState)).EventBytes;
        return (request, created, opening.ToArray(), weather);
    }

    private static string ImmutableFields(byte[] bytes)
    {
        var node = JsonNode.Parse(bytes)!;
        foreach (var key in new[] { "stateVersion", "prefix", "sequencePosition", "receipts" }) node.AsObject().Remove(key);
        return node.ToJsonString();
    }
    private static IEnumerable<byte[]> Mutations(byte[] bytes)
    {
        var root = JsonNode.Parse(bytes)!;
        foreach (var path in Leaves(root, []))
        {
            var copy = root.DeepClone();
            var parent = copy;
            foreach (var part in path[..^1]) parent = parent is JsonArray array ? array[int.Parse(part,
                System.Globalization.CultureInfo.InvariantCulture)]! : parent[part]!;
            if (parent is JsonArray list) list[int.Parse(path[^1], System.Globalization.CultureInfo.InvariantCulture)] = "invalid";
            else parent[path[^1]] = "invalid";
            yield return Encoding.UTF8.GetBytes(copy.ToJsonString());
        }
        var reversed = new JsonObject();
        foreach (var property in root.AsObject().Reverse()) reversed.Add(property.Key, property.Value?.DeepClone());
        yield return Encoding.UTF8.GetBytes(reversed.ToJsonString());
        var text = Encoding.UTF8.GetString(bytes);
        foreach (var raw in new[] { " " + text, text + "\n", "\uFEFF" + text,
            text.Replace("\"contractVersion\":", "\"contractVersion\":1,\"contractVersion\":", StringComparison.Ordinal),
            text.Replace("\"contractVersion\":2,", "\"contractVersion\":2.0,", StringComparison.Ordinal)
                .Replace("\"contractVersion\":1,", "\"contractVersion\":1.0,", StringComparison.Ordinal),
            text.Replace("creation.", "cre\\u0061tion.", StringComparison.Ordinal),
            text[..^1] + ",\"unknown\":null}" }) yield return Encoding.UTF8.GetBytes(raw);
    }
    private static IEnumerable<string[]> Leaves(JsonNode? node, string[] path)
    {
        if (node is JsonObject obj)
            foreach (var property in obj)
                foreach (var leaf in Leaves(property.Value, [.. path, property.Key])) yield return leaf;
        else if (node is JsonArray array)
            for (var i = 0; i < array.Count; i++)
                foreach (var leaf in Leaves(array[i], [.. path, i.ToString(System.Globalization.CultureInfo.InvariantCulture)])) yield return leaf;
        else yield return path;
    }
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
        "Campaigns", "Fixtures", "combat-stage-entry-v1.json")));
    private static void CheckGolden(JsonElement goldens, string kind, byte[] bytes)
    {
        Assert.Equal(goldens.GetProperty(kind).GetProperty("bytes").GetInt32(), bytes.Length);
        Assert.Equal(goldens.GetProperty(kind).GetProperty("sha256").GetString(), Digest(bytes));
    }
    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static string Receipt(JsonNode value)
    {
        var unsigned = value.DeepClone();
        unsigned.AsObject().Remove("receiptId");
        return "ste." + Digest(Encoding.UTF8.GetBytes("sandtable.combat.stage-entry-receipt.v1\0" + unsigned.ToJsonString()))[7..];
    }
    private static string Prefix(string prior, byte[] bytes)
    {
        var length = BitConverter.GetBytes((ulong)bytes.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(length);
        return Digest(Encoding.ASCII.GetBytes("sandtable.cycle.prefix.event.v1\0")
            .Concat(Convert.FromHexString(prior[7..])).Concat(length).Concat(bytes).ToArray());
    }
}
