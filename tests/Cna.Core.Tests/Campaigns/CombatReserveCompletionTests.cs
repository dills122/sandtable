using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatReserveCompletionTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var fixture = Fixture();
        foreach (var value in fixture.RootElement.GetProperty("cases").EnumerateArray())
            yield return [value.GetProperty("name").GetString()!];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void AllCompletionBytesAndCycleOneIdentitiesMatchHistoryDerivedGoldens(string name)
    {
        using var fixture = Fixture();
        var row = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("name").GetString() == name);
        var (request, created, opening, weather, stage) = Chain(row.GetProperty("seed").GetUInt64(), row.GetProperty("choice").GetString()!);
        var designation = Designation(request, created, opening, weather, stage, row.GetProperty("reserve").GetString() == "I");
        var before = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, designation);
        var unchanged = CampaignCombatReserveCodec.SerializeState(before);
        var input = CampaignCombatReserveCompletion.CreateCommand(request, created, opening, [weather], stage, designation);
        var evidence = CampaignCombatReserveCompletion.Create(request, created, opening, [weather], stage, designation, input);
        var basis = CampaignCombatReserveCompletionCodec.SerializeBase(evidence.OpeningBase);
        var inputBytes = CampaignCombatReserveCompletionCodec.SerializeInput(input);
        var bytes = evidence.EventBytes;
        var goldens = row.GetProperty("goldens");
        var index = designation.Length == 0 ? 1 : 2;
        CheckGolden(goldens, "opening-base", basis);
        CheckGolden(goldens, $"input-{index}", inputBytes);
        CheckGolden(goldens, $"event-{index}", bytes);
        Assert.Equal(input, CampaignCombatReserveCompletionCodec.DeserializeInput(inputBytes));
        Assert.Equal(Digest(basis), input.Command.BaseHash);
        Assert.Equal(unchanged, CampaignCombatReserveCodec.SerializeState(evidence.Predecessor));
        Assert.Equal(unchanged, CampaignCombatReserveCodec.SerializeState(before));
        var value = JsonNode.Parse(bytes)!;
        var cycle = value["cycle"]!;
        Assert.Equal(designation.Length == 0 ? 11 : 12, value["stateVersion"]!.GetValue<int>());
        Assert.Equal(1, cycle["ordinal"]!.GetValue<int>());
        Assert.Equal(value["stateVersion"]!.GetValue<int>(), cycle["openedAuthorityVersion"]!.GetValue<int>());
        Assert.Equal(before.Prefix, cycle["openingPrefix"]!.GetValue<string>());
        Assert.Equal(row.GetProperty("expected").GetProperty("actor").GetString(), cycle["actingSide"]!.GetValue<string>());
        Assert.Equal(request.Context.Configuration.Hash, cycle["admittedPolicyBundleDigest"]!.GetValue<string>());
        Assert.Equal(Identity(cycle), value["cycleId"]!.GetValue<string>());
        Assert.NotEqual(Identity(cycle, true), value["cycleId"]!.GetValue<string>());
        Assert.Equal(Receipt(value), value["receiptId"]!.GetValue<string>());
        Assert.Equal(5, value["sequencePosition"]!["contractVersion"]!.GetValue<int>());
        Assert.Null(value["sequencePosition"]!["activeSide"]);
        Assert.Equal("land.position.operation-1.first-player.movement-and-combat.movement", value["sequencePosition"]!["positionId"]!.GetValue<string>());
        var baseNode = JsonNode.Parse(basis)!;
        Assert.Equal("isolated-first-opening", baseNode["profile"]!.GetValue<string>());
        Assert.Equal(before.Stage.Weather.RandomState.NextByteCursor, baseNode["randomState"]!["nextByteCursor"]!.GetValue<ulong>());
        Assert.True(before.Stage.Weather.RandomState.NextByteCursor > request.RandomState.NextByteCursor);
        Assert.Equal(bytes, CampaignCombatReserveCompletion.ReadEvent(bytes, request, created, opening, [weather], stage, designation).EventBytes);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, designation.Append(bytes).ToArray()));
        Assert.ThrowsAny<JsonException>(() => CampaignV11PreambleCodec.Deserialize(bytes));
        Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(bytes, created, request));
    }

    [Theory]
    [InlineData("act-first", false)]
    [InlineData("act-last", true)]
    public void ActorBindingAndOccurrenceCannotBeReplacedBySelfConsistentHashes(string choice, bool reserve)
    {
        var (request, created, opening, weather, stage) = Chain(0, choice);
        var designation = Designation(request, created, opening, weather, stage, reserve);
        var input = CampaignCombatReserveCompletion.CreateCommand(request, created, opening, [weather], stage, designation);
        foreach (var changed in new[] {
            input with { Actor = CampaignOpeningPreambleActor.System },
            input with { Actor = input.Actor == CampaignOpeningPreambleActor.Axis ? CampaignOpeningPreambleActor.Commonwealth : CampaignOpeningPreambleActor.Axis },
            input with { Command = input.Command with { ContractVersion = 1 } },
            input with { Command = input.Command with { Kind = "designate-reserve-element" } },
            input with { Command = input.Command with { BaseHash = "sha256:" + new string('0', 64) } },
            input with { Command = input.Command with { ExpectedPriorVersion = 9 } },
            input with { Command = input.Command with { ExpectedPositionId = "land.position.foreign" } },
        }) Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCompletion.Create(request, created, opening, [weather], stage, designation, changed));
        var bytes = CampaignCombatReserveCompletion.Create(request, created, opening, [weather], stage, designation, input).EventBytes;
        foreach (var field in new[] { "ordinal", "openedAuthorityVersion", "actingSide", "openingPrefix", "setupHash", "admittedPolicyBundleDigest" })
        {
            var forged = JsonNode.Parse(bytes)!;
            var cycle = forged["cycle"]!;
            if (field is "ordinal" or "openedAuthorityVersion") cycle[field] = 42;
            else if (field == "actingSide") cycle[field] = choice == "act-first" ? "commonwealth" : "axis";
            else cycle[field] = "sha256:" + new string('0', 64);
            forged["cycleId"] = Identity(cycle);
            forged["receiptId"] = Receipt(forged);
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCompletion.ReadEvent(Encoding.UTF8.GetBytes(forged.ToJsonString()),
                request, created, opening, [weather], stage, designation));
        }
    }

    [Fact]
    public void PartialForeignReorderedOrRepeatedHistoriesCannotSupplyOpeningAuthority()
    {
        var (request, created, opening, weather, stage) = Chain();
        foreach (var records in new[] { stage.Take(3).ToArray(), stage.Reverse().ToArray(), stage.Append(stage[^1]).ToArray() })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCompletion.CreateCommand(request, created, opening, [weather], records, []));
        foreach (var records in new[] { Array.Empty<byte[]>(), new[] { weather, weather }, new[] { opening[0] } })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCompletion.CreateCommand(request, created, opening, records, stage, []));
        foreach (var records in new[] { opening.Skip(1).ToArray(), opening.Reverse().ToArray(), opening.Append(opening[^1]).ToArray() })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCompletion.CreateCommand(request, created, records, [weather], stage, []));
        var foreign = Chain(1);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCompletion.CreateCommand(request, created, foreign.Opening, [foreign.Weather], foreign.Stage, []));
        var designation = Designation(request, created, opening, weather, stage, true);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCompletion.CreateCommand(request, created, opening, [weather], stage, designation.Concat(designation).ToArray()));
        var input = CampaignCombatReserveCompletion.CreateCommand(request, created, opening, [weather], stage, []);
        var bytes = CampaignCombatReserveCompletion.Create(request, created, opening, [weather], stage, [], input).EventBytes;
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCompletion.ReadEvent(bytes, request, created, opening, [weather], stage, designation));
        var opposite = Chain(0, "act-last");
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCompletion.ReadEvent(bytes, request, created, opposite.Opening, [opposite.Weather], opposite.Stage, []));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCompletion.ReadEvent(bytes, request, [], opening, [weather], stage, []));
    }

    [Fact]
    public void ScalarAndRawForgeriesAndBoundsReject()
    {
        var (request, created, opening, weather, stage) = Chain();
        var input = CampaignCombatReserveCompletion.CreateCommand(request, created, opening, [weather], stage, []);
        var bytes = CampaignCombatReserveCompletion.Create(request, created, opening, [weather], stage, [], input).EventBytes;
        var inputBytes = CampaignCombatReserveCompletionCodec.SerializeInput(input);
        foreach (var forged in Mutations(inputBytes))
        {
            Assert.False(forged.AsSpan().SequenceEqual(inputBytes));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCompletion.Create(request, created, opening, [weather], stage, [],
                CampaignCombatReserveCompletionCodec.DeserializeInput(forged)));
        }
        foreach (var forged in Mutations(bytes))
        {
            Assert.False(forged.AsSpan().SequenceEqual(bytes));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCompletion.ReadEvent(forged, request, created, opening, [weather], stage, []));
        }
        foreach (var missing in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes("null") })
        {
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCompletionCodec.DeserializeInput(missing));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCompletion.ReadEvent(missing, request, created, opening, [weather], stage, []));
        }
    }

    [Fact]
    public void EvidenceDoesNotRetainMutableCallerStorage()
    {
        var (request, created, opening, weather, stage) = Chain();
        var designation = Designation(request, created, opening, weather, stage, true);
        var input = CampaignCombatReserveCompletion.CreateCommand(request, created, opening, [weather], stage, designation);
        var evidence = CampaignCombatReserveCompletion.Create(request, created, opening, [weather], stage, designation, input);
        var expected = evidence.EventBytes;
        var callerBytes = evidence.EventBytes;
        var restored = CampaignCombatReserveCompletion.ReadEvent(callerBytes, request, created, opening, [weather], stage, designation);
        foreach (var buffer in new[] { created, weather, callerBytes }.Concat(opening).Concat(stage).Concat(designation)) Array.Fill(buffer, (byte)0);
        Assert.Equal(expected, evidence.EventBytes);
        Assert.Equal(expected, restored.EventBytes);
    }

    private static byte[][] Designation(CampaignCombatCreationRequest request, byte[] created, byte[][] opening, byte[] weather, byte[][] stage, bool reserve)
    {
        if (!reserve) return [];
        var state = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, []);
        return [CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, [], CampaignCombatReserveDesignation.Command(state)).EventBytes];
    }
    private static string Identity(JsonNode cycle, bool incorrectlyDecodeStringHashes = false)
    {
        using var stream = new MemoryStream();
        stream.Write(Encoding.ASCII.GetBytes("sandtable.cycle.authority.v1\0"));
        foreach (var field in new[] { "contractVersion", "campaignId", "rulesetHash", "setupId", "setupHash", "contentPackId", "contentHash", "scenarioId", "gameTurn", "operationStage", "playerPhaseSlot", "actingSide", "ordinal", "openedAuthorityVersion", "openingPrefix", "admittedPolicyBundleDigest" })
        {
            if (field == "contractVersion") WriteNumber(stream, (ulong)cycle[field]!.GetValue<int>(), 4);
            else if (field is "gameTurn" or "operationStage" or "ordinal" or "openedAuthorityVersion") WriteNumber(stream, ulong.Parse(cycle[field]!.ToJsonString(), System.Globalization.CultureInfo.InvariantCulture), 8);
            else
            {
                var value = cycle[field]!.GetValue<string>();
                if (field is "openingPrefix" or "admittedPolicyBundleDigest" || incorrectlyDecodeStringHashes && field is "rulesetHash" or "setupHash" or "contentHash")
                    stream.Write(Convert.FromHexString(value.StartsWith("sha256:", StringComparison.Ordinal) ? value[7..] : value));
                else
                {
                    var text = Encoding.UTF8.GetBytes(value);
                    WriteNumber(stream, (ulong)text.Length, 4);
                    stream.Write(text);
                }
            }
        }
        return Digest(stream.ToArray());
    }
    private static void WriteNumber(Stream stream, ulong number, int width)
    {
        for (var shift = (width - 1) * 8; shift >= 0; shift -= 8) stream.WriteByte((byte)(number >> shift));
    }
    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather, byte[][] Stage) Chain(ulong seed = 0, string choice = "act-first")
    {
        var (request, created, opening, weather) = Predecessor(seed, choice);
        var stage = new List<byte[]>();
        for (var i = 0; i < 4; i++)
        {
            var state = CampaignCombatStageEntry.Replay(request, created, opening, [weather], stage);
            stage.Add(CampaignCombatStageEntry.Apply(request, created, opening, [weather], stage, CampaignCombatStageEntry.Command(state)).EventBytes);
        }
        return (request, created, opening, weather, stage.ToArray());
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
            text.Replace("contractVersion", "contract\\u0056ersion", StringComparison.Ordinal),
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
        "Campaigns", "Fixtures", "combat-reserve-designation-v1.json")));
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
        return "rc." + Digest(Encoding.UTF8.GetBytes("sandtable.combat.reserve-completion-receipt.v2\0" + unsigned.ToJsonString()))[7..];
    }
}
