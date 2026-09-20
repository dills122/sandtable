using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatOpeningPreambleTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var fixture = Fixture();
        foreach (var value in fixture.RootElement.GetProperty("cases").EnumerateArray())
            yield return [value.GetProperty("name").GetString()!, value.GetProperty("seed").GetUInt64(),
                value.GetProperty("choice").GetString()!];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void SixFrozenTracesMatchEveryCreationEventAndStateCut(string name, ulong seed, string choiceText)
    {
        var choice = choiceText == "act-first" ? InitiativeOrderChoice.ActFirst : InitiativeOrderChoice.ActLast;
        using var fixture = Fixture();
        var expected = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("name").GetString() == name);
        var request = Request(seed);
        var created = CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(request));
        CheckGolden(expected, "creation", created);
        var events = new List<byte[]>();
        var inputs = new List<CampaignOpeningPreambleInput>();
        var initial = CampaignOpeningPreamble.Replay(request, created, events);
        var world = WorldBytes(initial);
        for (var cut = 0; cut <= 4; cut++)
        {
            var state = CampaignOpeningPreamble.Replay(request, created, events);
            var bytes = CampaignOpeningPreambleCodec.SerializeState(state);
            CheckGolden(expected, $"state-{cut}", bytes);
            Assert.Equal(bytes, CampaignOpeningPreambleCodec.SerializeState(
                CampaignOpeningPreamble.ReadState(bytes, request, created, events)));
            Assert.Equal(cut + 1, state.StateVersion);
            Assert.Equal(expected.GetProperty("positions")[cut].GetString(), state.SequencePosition.PositionId);
            Assert.Equal(world, WorldBytes(state));
            Assert.Equal(seed, state.Creation.RandomState.Seed);
            Assert.Equal(0UL, state.Creation.RandomState.NextByteCursor);
            Assert.Null(state.SequencePosition.ActiveSide);
            Assert.Equal(cut, state.Receipts.Count);
            if (cut > 0) Assert.Equal(LandSide.Axis, state.InitiativeHolder);
            for (var accepted = 0; accepted < inputs.Count; accepted++)
            {
                var retry = CampaignOpeningPreamble.Apply(request, created, events, inputs[accepted]);
                Assert.True(retry.Duplicate);
                Assert.Equal(events[accepted], retry.EventBytes);
                Assert.Equal(bytes, CampaignOpeningPreambleCodec.SerializeState(retry.State));
                Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Apply(request, created, events,
                    inputs[accepted] with { Actor = CampaignOpeningPreambleActor.Commonwealth }));
            }
            if (cut == 4) break;
            var input = CampaignOpeningPreamble.Command(state, cut == 3 ? choice : null);
            var commandBytes = CampaignOpeningPreambleCodec.SerializeInput(input);
            Assert.Equal(input, CampaignOpeningPreambleCodec.DeserializeInput(commandBytes));
            var result = CampaignOpeningPreamble.Apply(request, created, events, input);
            Assert.False(result.Duplicate);
            CheckGolden(expected, $"event-{cut + 1}", result.EventBytes);
            using var value = JsonDocument.Parse(result.EventBytes);
            Assert.Equal(state.Prefix, value.RootElement.GetProperty("priorPrefix").GetString());
            Assert.Equal(EventPrefix(state.Prefix, result.EventBytes), result.State.Prefix);
            Assert.Equal(Digest(commandBytes), result.State.Receipts[^1].CommandHash);
            Assert.Equal(Digest(result.EventBytes), result.State.Receipts[^1].EventHash);
            Assert.ThrowsAny<JsonException>(() => CampaignV11PreambleCodec.Deserialize(result.EventBytes));
            inputs.Add(input);
            events.Add(result.EventBytes);
        }
        var final = CampaignOpeningPreamble.Replay(request, created, events);
        var order = Assert.Single(final.Orders);
        Assert.Equal(expected.GetProperty("expectedFirst").GetString(), Side(order.FirstSide));
        Assert.Equal(expected.GetProperty("expectedSecond").GetString(), Side(order.SecondSide));
        Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Command(final));
        var opposite = inputs[^1] with
        {
            Command = inputs[^1].Command with
            { Choice = choice == InitiativeOrderChoice.ActFirst ? InitiativeOrderChoice.ActLast : InitiativeOrderChoice.ActFirst }
        };
        Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Apply(request, created, events, opposite));
    }

    [Fact]
    public void InvalidChainsAndForeignCreationRejectWithoutChangingInputs()
    {
        var (request, created, inputs, events) = Trace();
        var before = events.Select(Convert.ToHexString).ToArray();
        foreach (var suffix in new[] { events.Skip(1).ToArray(), events.AsEnumerable().Reverse().ToArray(),
            events.Concat([events[^1]]).ToArray(), new[] { events[0], events[1], events[1] } })
            Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Replay(request, created, suffix));
        foreach (var bad in new[] { Array.Empty<byte>(), "{}"u8.ToArray(), created.Concat(new byte[] { 32 }).ToArray() })
            Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Replay(request, bad, events));
        Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Replay(Request(1), created, events));
        var other = Request(1);
        var otherCreated = CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(other));
        Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Replay(other, otherCreated, events));
        var last = inputs[^1];
        foreach (var changed in new[]
        {
            last with { Command = last.Command with { ExpectedPriorVersion = 3 } },
            last with { Command = last.Command with { CreationBinding = "creation.forged" } },
            last with { Command = last.Command with { DeclaringSide = LandSide.Commonwealth } },
            last with { Command = last.Command with { OperationStage = 2 } },
            last with { Command = last.Command with { ContractVersion = 1 } },
        }) Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Apply(request, created, events.Take(3).ToArray(), changed));
        Assert.Equal(before, events.Select(Convert.ToHexString).ToArray());
    }

    [Fact]
    public void AlteredEventStateAndRawBytesRejectAtEveryCut()
    {
        var (request, created, _, events) = Trace();
        for (var cut = 0; cut <= 4; cut++)
        {
            var prefix = events.Take(cut).ToArray();
            var state = CampaignOpeningPreamble.Replay(request, created, prefix);
            var bytes = CampaignOpeningPreambleCodec.SerializeState(state);
            foreach (var changed in ScalarMutations(bytes))
                Assert.Throws<JsonException>(() => CampaignOpeningPreamble.ReadState(changed, request, created, prefix));
            foreach (var changed in RawMutations(bytes))
                Assert.Throws<JsonException>(() => CampaignOpeningPreamble.ReadState(changed, request, created, prefix));
            if (cut == 4) continue;
            foreach (var changed in ScalarMutations(events[cut]).Concat(RawMutations(events[cut])))
                Assert.ThrowsAny<JsonException>(() => CampaignOpeningPreamble.Replay(request, created, prefix.Append(changed).ToArray()));
        }
    }

    [Fact]
    public void UnsupportedValidCreationContextsAndFutureEventsReject()
    {
        var request = Request();
        var context = request.Context;
        var setup = context.Setup;
        var holder = CampaignSetupSnapshotV7.Create(setup.SetupId, 1, new PredeterminedInitiative(LandSide.Commonwealth),
            setup.OpeningPreamble, setup.Weather, setup.StageEntry, setup.CombatInitialization,
            setup.Artifact, setup.Scenario, setup.Sources);
        var config = new CombatDecisionConfiguration(context.Configuration.ConfigId, context.Configuration.RulesInputHash,
            context.Configuration.Windows.Select(w => w with { DecisionBudgetMilliseconds = w.DecisionBudgetMilliseconds + 1 }));
        foreach (var changed in new[]
        {
            new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, holder, holder.Artifact, holder.Scenario, context.Configuration),
            new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, setup, setup.Artifact, setup.Scenario, config),
        })
        {
            var foreign = CampaignCombatCreationRequest.Create(request.CampaignId, 0, changed);
            Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Replay(foreign,
                CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(foreign)), []));
        }
        var (_, created, _, events) = Trace();
        var future = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(events[^1]).Replace(
            "initiative-order-declared", "weather-determined", StringComparison.Ordinal));
        Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Replay(request, created, events.Take(3).Append(future).ToArray()));
    }

    [Fact]
    public void WellFormedForgedAuthorityAndOppositeOrderCacheReject()
    {
        var (request, created, inputs, events) = Trace();
        foreach (var property in new[] { "priorPrefix", "creationEventHash", "configurationHash" })
        {
            var forged = JsonNode.Parse(events[2])!;
            forged[property] = "sha256:" + new string('0', 64);
            Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Replay(request, created,
                events.Take(2).Append(Encoding.UTF8.GetBytes(forged.ToJsonString())).ToArray()));
        }
        var receipt = JsonNode.Parse(events[2])!;
        receipt["receiptId"] = "pre." + new string('0', 64);
        Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Replay(request, created,
            events.Take(2).Append(Encoding.UTF8.GetBytes(receipt.ToJsonString())).ToArray()));
        var source = JsonNode.Parse(events[2])!;
        source["sources"]![0]!["locator"] = "opening-preamble.other";
        Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Replay(request, created,
            events.Take(2).Append(Encoding.UTF8.GetBytes(source.ToJsonString())).ToArray()));
        var beforeOrder = events.Take(3).ToArray();
        var oppositeInput = inputs[^1] with { Command = inputs[^1].Command with { Choice = InitiativeOrderChoice.ActLast } };
        var opposite = CampaignOpeningPreamble.Apply(request, created, beforeOrder, oppositeInput);
        var first = CampaignOpeningPreamble.Replay(request, created, events);
        Assert.NotEqual(first.Prefix, opposite.State.Prefix);
        Assert.Throws<JsonException>(() => CampaignOpeningPreamble.ReadState(
            CampaignOpeningPreambleCodec.SerializeState(first), request, created, beforeOrder.Append(opposite.EventBytes).ToArray()));
    }

    [Fact]
    public void CommandRawBytesLimitsAndForgedInputsReject()
    {
        var (request, created, inputs, events) = Trace();
        for (var index = 0; index < inputs.Count; index++)
        {
            var bytes = CampaignOpeningPreambleCodec.SerializeInput(inputs[index]);
            foreach (var malformed in RawMutations(bytes))
                Assert.ThrowsAny<JsonException>(() => CampaignOpeningPreambleCodec.DeserializeInput(malformed));
            foreach (var mutatedInput in ScalarMutations(bytes))
                Assert.ThrowsAny<JsonException>(() => CampaignOpeningPreamble.Apply(request, created,
                    events.Take(index).ToArray(), CampaignOpeningPreambleCodec.DeserializeInput(mutatedInput)));
        }
        foreach (var malformed in new[] { Array.Empty<byte>(), "null"u8.ToArray(), "[]"u8.ToArray(), new byte[1_048_577] })
        {
            Assert.ThrowsAny<JsonException>(() => CampaignOpeningPreambleCodec.DeserializeInput(malformed));
            Assert.ThrowsAny<JsonException>(() => CampaignOpeningPreamble.Replay(request, created, [malformed]));
            Assert.Throws<JsonException>(() => CampaignOpeningPreamble.ReadState(malformed, request, created, []));
        }
        var first = inputs[0];
        Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Apply(request, created, events,
            first with { Command = first.Command with { CreationEventHash = "sha256:" + new string('0', 64) } }));
        Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Apply(request, created, [],
            first with { Command = first.Command with { Choice = InitiativeOrderChoice.ActFirst } }));
        Assert.Throws<JsonException>(() => CampaignOpeningPreamble.Apply(request, created, [],
            first with { Command = first.Command with { OperationStage = 1 } }));
        var altered = JsonNode.Parse(CampaignOpeningPreambleCodec.SerializeInput(first))!;
        altered["command"]!["kind"] = "resolve-weather";
        Assert.Throws<JsonException>(() => CampaignOpeningPreambleCodec.DeserializeInput(Encoding.UTF8.GetBytes(altered.ToJsonString())));
    }

    [Fact]
    public void RetainedCallerBytesAndReturnedBytesCannotMutateReplayedState()
    {
        var (request, created, inputs, events) = Trace();
        var retry = CampaignOpeningPreamble.Apply(request, created, events, inputs[0]);
        var expected = CampaignOpeningPreambleCodec.SerializeState(retry.State);
        Array.Fill(retry.EventBytes, (byte)0);
        Array.Fill(created, (byte)0);
        foreach (var value in events) Array.Fill(value, (byte)0);
        Assert.Equal(expected, CampaignOpeningPreambleCodec.SerializeState(retry.State));
    }

    private static (CampaignCombatCreationRequest Request, byte[] Created,
        List<CampaignOpeningPreambleInput> Inputs, List<byte[]> Events) Trace()
    {
        var request = Request();
        var created = CampaignCreatedV11Serializer.Serialize(CampaignCreatedV11.Create(request));
        var events = new List<byte[]>();
        var inputs = new List<CampaignOpeningPreambleInput>();
        for (var index = 0; index < 4; index++)
        {
            var state = CampaignOpeningPreamble.Replay(request, created, events);
            var input = CampaignOpeningPreamble.Command(state, index == 3 ? InitiativeOrderChoice.ActFirst : null);
            inputs.Add(input);
            events.Add(CampaignOpeningPreamble.Apply(request, created, events, input).EventBytes);
        }
        return (request, created, inputs, events);
    }

    private static IEnumerable<byte[]> ScalarMutations(byte[] bytes)
    {
        var original = JsonNode.Parse(bytes)!;
        foreach (var path in Leaves(original, []))
        {
            var copy = original.DeepClone();
            var node = copy;
            foreach (var segment in path[..^1]) node = node is JsonArray array ? array[int.Parse(segment,
                System.Globalization.CultureInfo.InvariantCulture)]! : node[segment]!;
            var last = path[^1];
            if (node is JsonArray list) list[int.Parse(last, System.Globalization.CultureInfo.InvariantCulture)] = "invalid";
            else node[last] = "invalid";
            yield return Encoding.UTF8.GetBytes(copy.ToJsonString());
        }
    }

    private static IEnumerable<string[]> Leaves(JsonNode? node, string[] path)
    {
        if (node is JsonObject obj)
            foreach (var property in obj)
                foreach (var leaf in Leaves(property.Value, [.. path, property.Key])) yield return leaf;
        else if (node is JsonArray array)
            for (var index = 0; index < array.Count; index++)
                foreach (var leaf in Leaves(array[index], [.. path, index.ToString(System.Globalization.CultureInfo.InvariantCulture)])) yield return leaf;
        else yield return path;
    }

    private static IEnumerable<byte[]> RawMutations(byte[] bytes)
    {
        var text = Encoding.UTF8.GetString(bytes);
        foreach (var value in new[] { " " + text, text + "\n", "\uFEFF" + text,
            text.Replace("\"contractVersion\":", "\"contractVersion\":1,\"contractVersion\":", StringComparison.Ordinal),
            text[..^1] + ",\"unknown\":null}" }) yield return Encoding.UTF8.GetBytes(value);
    }

    private static CampaignCombatCreationRequest Request(ulong seed = 0)
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
        using var created = JsonDocument.Parse(fixture.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
        var root = created.RootElement;
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("setup").GetRawText()), artifact, scenario);
        var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
        return CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", seed,
            new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, config));
    }

    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
        "Campaigns", "Fixtures", "combat-opening-preamble-v1.json")));
    private static void CheckGolden(JsonElement test, string kind, byte[] bytes)
    {
        var expected = test.GetProperty("goldens").GetProperty(kind);
        Assert.Equal(expected.GetProperty("bytes").GetInt32(), bytes.Length);
        Assert.Equal(expected.GetProperty("sha256").GetString(), Digest(bytes));
    }
    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static string EventPrefix(string prior, byte[] bytes)
    {
        var length = BitConverter.GetBytes((ulong)bytes.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(length);
        return Digest(Encoding.ASCII.GetBytes("sandtable.cycle.prefix.event.v1\0")
            .Concat(Convert.FromHexString(prior[7..])).Concat(length).Concat(bytes).ToArray());
    }
    private static string Side(LandSide side) => side == LandSide.Axis ? "axis" : "commonwealth";
    private static byte[] WorldBytes(CampaignOpeningPreambleState state) => CampaignWorldV7InitialCodec.Serialize(
        state.Creation.World, state.Creation.Setup.Artifact, state.Creation.Setup.Scenario, state.Creation.Setup.CombatInitialization);
}
