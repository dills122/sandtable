using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatReserveOpeningTests
{
    private static readonly JsonSerializerOptions PredecessorJson = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    public static IEnumerable<object[]> Cases()
    {
        using var fixture = Fixture();
        foreach (var value in fixture.RootElement.GetProperty("cases").EnumerateArray())
            yield return [value.GetProperty("name").GetString()!];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void CompleteFrozenTracesAtomicallyOpenCycleAndRetryAtEveryLaterCut(string name)
    {
        using var fixture = Fixture();
        var row = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("name").GetString() == name);
        var (request, created, opening, weather, stage) = Chain(row.GetProperty("seed").GetUInt64(), row.GetProperty("choice").GetString()!);
        var reserve = row.GetProperty("reserve").GetString() == "I";
        var goldens = row.GetProperty("goldens");
        CheckGolden(goldens, "request", CampaignCombatCreationRequestCodec.Serialize(request));
        CheckGolden(goldens, "creation", created);
        CheckGolden(goldens, "predecessor-records", JsonSerializer.SerializeToUtf8Bytes(new[] { created }.Concat(opening).Append(weather).Concat(stage)
            .Select(bytes => Encoding.UTF8.GetString(bytes)), PredecessorJson));
        var history = new List<byte[]>();
        CampaignCombatReserveInput? designated = null;
        CampaignCombatReserveCompletionInput? completed = null;
        var initial = CampaignCombatReserveOpening.Replay(request, created, opening, [weather], stage, history);
        var initialBytes = CampaignCombatReserveOpeningCodec.SerializeState(initial);
        for (var cut = 0; cut <= (reserve ? 2 : 1); cut++)
        {
            var state = CampaignCombatReserveOpening.Replay(request, created, opening, [weather], stage, history);
            var bytes = CampaignCombatReserveOpeningCodec.SerializeState(state);
            CheckGolden(goldens, $"state-{cut + 10}", bytes);
            Assert.Equal(10 + cut, state.StateVersion);
            Assert.Equal(9 + cut, state.Receipts.Count);
            Assert.Equal(initial.Receipts, state.Receipts.Take(9));
            Assert.Equal(bytes, CampaignCombatReserveOpeningCodec.SerializeState(CampaignCombatReserveOpening.ReadState(bytes,
                request, created, opening, [weather], stage, history)));
            var node = JsonNode.Parse(bytes)!;
            var original = JsonNode.Parse(initialBytes)!;
            foreach (var key in new[] { "initiativeHolder", "operationStageOrders", "operationStageWeather", "randomState" })
                Assert.True(JsonNode.DeepEquals(original[key], node[key]), key);
            Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(bytes, created, request));
            if (designated is not null)
            {
                var retry = CampaignCombatReserveOpening.Apply(request, created, opening, [weather], stage, history, designated);
                Assert.True(retry.Duplicate);
                Assert.Equal(history[0], retry.EventBytes);
                Assert.Equal(bytes, CampaignCombatReserveOpeningCodec.SerializeState(retry.State));
                foreach (var actor in Enum.GetValues<CampaignOpeningPreambleActor>().Where(a => a != designated.Actor))
                    Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Apply(request, created, opening, [weather], stage, history, designated with { Actor = actor }));
            }
            if (completed is not null)
            {
                var retry = CampaignCombatReserveOpening.Apply(request, created, opening, [weather], stage, history, completed);
                Assert.True(retry.Duplicate);
                Assert.Equal(history[^1], retry.EventBytes);
                Assert.Equal(bytes, CampaignCombatReserveOpeningCodec.SerializeState(retry.State));
                foreach (var actor in Enum.GetValues<CampaignOpeningPreambleActor>().Where(a => a != completed.Actor))
                    Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Apply(request, created, opening, [weather], stage, history, completed with { Actor = actor }));
                Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.ReadState(bytes, request, created, opening, [weather], stage, history.Take(history.Count - 1).ToArray()));
                Assert.Equal(1, state.Cycle!.Ordinal);
                Assert.Equal(state.StateVersion, state.Cycle.OpenedAuthorityVersion);
                Assert.Equal(state.Predecessor.Prefix, state.Cycle.OpeningPrefix);
                Assert.Equal(row.GetProperty("expected").GetProperty("actor").GetString(), CampaignSnapshotSerializer.FormatSide(state.Cycle.ActingSide));
                Assert.Null(state.SequencePosition.ActiveSide);
                Assert.Equal("land.position.operation-1.first-player.movement-and-combat.movement", state.SequencePosition.PositionId);
                var predecessor = JsonNode.Parse(CampaignCombatReserveCodec.SerializeState(state.Predecessor))!;
                foreach (var key in new[] { "world", "members", "firstActingSide" }) Assert.True(JsonNode.DeepEquals(predecessor[key], node[key]), key);
                Assert.Equal(state.CompletionReceiptId, state.Receipts[^1].ReceiptId);
                Assert.Equal(Prefix(state.Predecessor.Prefix, history[^1]), state.Prefix);
                Assert.Equal(Receipt(JsonNode.Parse(history[^1])!), state.CompletionReceiptId);
                Assert.Equal(Identity(node["cycle"]!), state.CycleId);
                Assert.Equal(completed.Command.BaseHash, state.OpeningBaseHash);
                Assert.Equal(11 + (reserve ? 1 : 0), state.StateVersion);
                break;
            }
            Assert.Null(state.Cycle);
            foreach (var key in new[] { "cycle", "cycleId", "openingBaseHash", "completionReceiptId" }) Assert.Null(node[key]);
            CampaignCombatReserveOpeningResult result;
            byte[] inputBytes;
            if (reserve && cut == 0)
            {
                designated = CampaignCombatReserveDesignation.Command(state.Predecessor);
                inputBytes = CampaignCombatReserveCodec.SerializeInput(designated);
                result = CampaignCombatReserveOpening.Apply(request, created, opening, [weather], stage, history, designated);
            }
            else
            {
                completed = CampaignCombatReserveCompletion.CreateCommand(request, created, opening, [weather], stage, history);
                inputBytes = CampaignCombatReserveCompletionCodec.SerializeInput(completed);
                var evidence = CampaignCombatReserveCompletion.Create(request, created, opening, [weather], stage, history, completed);
                CheckGolden(goldens, "opening-base", CampaignCombatReserveCompletionCodec.SerializeBase(evidence.OpeningBase));
                result = CampaignCombatReserveOpening.Apply(request, created, opening, [weather], stage, history, completed);
            }
            Assert.False(result.Duplicate);
            CheckGolden(goldens, $"input-{cut + 1}", inputBytes);
            CheckGolden(goldens, $"event-{cut + 1}", result.EventBytes);
            Assert.Equal(Prefix(state.Prefix, result.EventBytes), result.State.Prefix);
            Assert.Equal(Digest(inputBytes), result.State.Receipts[^1].CommandHash);
            Assert.Equal(Digest(result.EventBytes), result.State.Receipts[^1].EventHash);
            Assert.ThrowsAny<JsonException>(() => CampaignV11PreambleCodec.Deserialize(result.EventBytes));
            history.Add(result.EventBytes);
        }
    }

    [Theory]
    [InlineData("act-first", false)]
    [InlineData("act-first", true)]
    [InlineData("act-last", false)]
    [InlineData("act-last", true)]
    public void ClosedTerminalStateRejectsChangedRetriesAndCrossFamilyOccurrenceReuse(string choice, bool reserve)
    {
        var (request, created, opening, weather, stage, history, designation, completion) = Trace(choice, reserve);
        var state = CampaignCombatReserveOpening.Replay(request, created, opening, [weather], stage, history);
        foreach (var input in new[] {
            completion with { Command = completion.Command with { BaseHash = "sha256:" + new string('0', 64) } },
            completion with { Command = completion.Command with { ExpectedPriorVersion = state.StateVersion } },
            completion with { Command = completion.Command with { ExpectedPriorVersion = 10 } },
            completion with { Command = completion.Command with { ExpectedPositionId = state.SequencePosition.PositionId } },
            completion with { Command = completion.Command with { ContractVersion = 1 } },
        }.Where(input => input != completion))
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Apply(request, created, opening, [weather], stage, history, input));
        foreach (var input in new[] {
            designation with { Command = designation.Command with { ElementId = "element.foreign" } },
            designation with { Command = designation.Command with { ExpectedPriorVersion = state.StateVersion } },
            designation with { Command = designation.Command with { ExpectedPriorVersion = completion.Command.ExpectedPriorVersion } },
            designation with { Command = designation.Command with { CreationBinding = "creation.foreign" } },
            designation with { Command = designation.Command with { ContractVersion = 1 } },
        }) Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Apply(request, created, opening, [weather], stage, history, input));
        if (!reserve) Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Apply(request, created, opening, [weather], stage, history, designation));
    }

    [Fact]
    public void MissingForeignReorderedRepeatedAndPostCompletionEvidenceReject()
    {
        var (request, created, opening, weather, stage, history, _, _) = Trace();
        foreach (var records in new[] { history.Reverse().ToArray(), history.Append(history[^1]).ToArray(), new[] { history[1], history[1] }, new[] { history[0], history[0] }, new[] { history[1] }, new[] { weather } })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Replay(request, created, opening, [weather], stage, records));
        foreach (var records in new[] { stage.Take(3).ToArray(), stage.Reverse().ToArray(), stage.Append(stage[^1]).ToArray() })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Replay(request, created, opening, [weather], records, history));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Replay(request, created, opening, [], stage, history));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Replay(request, created, opening, [weather, weather], stage, history));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Replay(request, created, opening.Take(3).ToArray(), [weather], stage, history));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Replay(request, [], opening, [weather], stage, history));
        var opposite = Trace("act-last");
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Replay(request, created, opening, [weather], stage, opposite.History));
        var stateBytes = CampaignCombatReserveOpeningCodec.SerializeState(CampaignCombatReserveOpening.Replay(request, created, opening, [weather], stage, history));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.ReadState(stateBytes, request, created, opposite.Opening, [opposite.Weather], opposite.Stage, opposite.History));
    }

    [Fact]
    public void ScalarRawAndCoherentlyResignedCycleCacheForgeriesReject()
    {
        var (request, created, opening, weather, stage, history, _, _) = Trace();
        for (var cut = 0; cut <= 2; cut++)
        {
            var retained = history.Take(cut).ToArray();
            var state = CampaignCombatReserveOpening.Replay(request, created, opening, [weather], stage, retained);
            var bytes = CampaignCombatReserveOpeningCodec.SerializeState(state);
            foreach (var forged in Mutations(bytes))
            {
                Assert.False(bytes.AsSpan().SequenceEqual(forged));
                Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.ReadState(forged, request, created, opening, [weather], stage, retained));
            }
            if (cut == 2) continue;
            foreach (var forged in Mutations(history[cut]))
            {
                Assert.False(history[cut].AsSpan().SequenceEqual(forged));
                Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Replay(request, created, opening, [weather], stage, retained.Append(forged).ToArray()));
            }
        }
        var terminal = CampaignCombatReserveOpening.Replay(request, created, opening, [weather], stage, history);
        var forgedEvent = JsonNode.Parse(history[^1])!;
        forgedEvent["cycle"]!["ordinal"] = 2;
        forgedEvent["cycleId"] = Identity(forgedEvent["cycle"]!);
        forgedEvent["receiptId"] = Receipt(forgedEvent);
        var forgedBytes = Encoding.UTF8.GetBytes(forgedEvent.ToJsonString());
        var cache = JsonNode.Parse(CampaignCombatReserveOpeningCodec.SerializeState(terminal))!;
        cache["cycle"] = forgedEvent["cycle"]!.DeepClone();
        cache["cycleId"] = forgedEvent["cycleId"]!.DeepClone();
        cache["completionReceiptId"] = forgedEvent["receiptId"]!.DeepClone();
        cache["prefix"] = Prefix(terminal.Predecessor.Prefix, forgedBytes);
        cache["receipts"]![10]!["eventHash"] = Digest(forgedBytes);
        cache["receipts"]![10]!["receiptId"] = forgedEvent["receiptId"]!.DeepClone();
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.ReadState(Encoding.UTF8.GetBytes(cache.ToJsonString()),
            request, created, opening, [weather], stage, [history[0], forgedBytes]));
    }

    [Fact]
    public void BoundsAndCallerBuffersCannotAlterAcceptedProjection()
    {
        var (request, created, opening, weather, stage, history, _, completion) = Trace();
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes("null") })
        {
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Replay(request, created, opening, [weather], stage, [bytes]));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.ReadState(bytes, request, created, opening, [weather], stage, history));
        }
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Replay(request, created, opening, [weather], stage, [null!]));
        var state = CampaignCombatReserveOpening.Replay(request, created, opening, [weather], stage, history);
        var expected = CampaignCombatReserveOpeningCodec.SerializeState(state);
        var result = CampaignCombatReserveOpening.Apply(request, created, opening, [weather], stage, history, completion);
        Array.Fill(result.EventBytes, (byte)0);
        Assert.Equal(expected, CampaignCombatReserveOpeningCodec.SerializeState(result.State));
        foreach (var bytes in new[] { created, weather }.Concat(opening).Concat(stage).Concat(history)) Array.Fill(bytes, (byte)0);
        Assert.Equal(expected, CampaignCombatReserveOpeningCodec.SerializeState(state));
    }

    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather, byte[][] Stage, byte[][] History,
        CampaignCombatReserveInput Designation, CampaignCombatReserveCompletionInput Completion) Trace(string choice = "act-first", bool reserve = true)
    {
        var (request, created, opening, weather, stage) = Chain(0, choice);
        var initial = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, []);
        var designation = CampaignCombatReserveDesignation.Command(initial);
        var history = new List<byte[]>();
        if (reserve) history.Add(CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, [], designation).EventBytes);
        var completion = CampaignCombatReserveCompletion.CreateCommand(request, created, opening, [weather], stage, history);
        history.Add(CampaignCombatReserveCompletion.Create(request, created, opening, [weather], stage, history, completion).EventBytes);
        return (request, created, opening, weather, stage, history.ToArray(), designation, completion);
    }
    private static string Prefix(string prior, byte[] bytes)
    {
        var length = BitConverter.GetBytes((ulong)bytes.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(length);
        return Digest(Encoding.ASCII.GetBytes("sandtable.cycle.prefix.event.v1\0")
            .Concat(Convert.FromHexString(prior[7..])).Concat(length).Concat(bytes).ToArray());
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
