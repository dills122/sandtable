using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatReserveDesignationTests
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
    public void FrozenPrecompletionCutsPreserveResourcesAndDeriveOwnerFromOrder(string name)
    {
        using var fixture = Fixture();
        var row = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("name").GetString() == name);
        var goldens = row.GetProperty("goldens");
        var trace = Chain(row.GetProperty("seed").GetUInt64(), row.GetProperty("choice").GetString()!);
        var (request, created, opening, weather, stage) = trace;
        CheckGolden(goldens, "request", CampaignCombatCreationRequestCodec.Serialize(request));
        CheckGolden(goldens, "creation", created);
        var predecessor = JsonSerializer.SerializeToUtf8Bytes(new[] { created }.Concat(opening).Append(weather).Concat(stage)
            .Select(bytes => Encoding.UTF8.GetString(bytes)), PredecessorJson);
        CheckGolden(goldens, "predecessor-records", predecessor);
        var before = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, []);
        var beforeBytes = CampaignCombatReserveCodec.SerializeState(before);
        CheckGolden(goldens, "state-10", beforeBytes);
        Assert.Equal(10, before.StateVersion);
        Assert.Equal(9, before.Receipts.Count);
        Assert.Equal(row.GetProperty("expected").GetProperty("actor").GetString(), CampaignSnapshotSerializer.FormatSide(before.FirstActingSide));
        Assert.Equal(LandSide.Axis, before.Stage.Weather.Opening.InitiativeHolder);
        Assert.Null(before.SequencePosition.ActiveSide);
        Assert.Equal(LandActorRole.FirstActingSide, before.SequencePosition.ActorRole);
        Assert.Equal(CampaignElementReserveStatus.None, Assert.Single(before.Members).Status);
        Assert.Equal(beforeBytes, CampaignCombatReserveCodec.SerializeState(CampaignCombatReserveDesignation.ReadState(beforeBytes, request, created, opening, [weather], stage, [])));
        Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(beforeBytes, created, request));
        if (row.GetProperty("reserve").GetString() == "none") return;
        var input = CampaignCombatReserveDesignation.Command(before);
        var inputBytes = CampaignCombatReserveCodec.SerializeInput(input);
        CheckGolden(goldens, "input-1", inputBytes);
        Assert.Equal(input, CampaignCombatReserveCodec.DeserializeInput(inputBytes));
        var result = CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, [], input);
        var afterBytes = CampaignCombatReserveCodec.SerializeState(result.State);
        CheckGolden(goldens, "event-1", result.EventBytes);
        CheckGolden(goldens, "state-11", afterBytes);
        Assert.False(result.Duplicate);
        Assert.Equal(before.SequencePosition, result.State.SequencePosition);
        Assert.Equal(before.Receipts, result.State.Receipts.Take(9));
        Assert.Equal(Prefix(before.Prefix, result.EventBytes), result.State.Prefix);
        Assert.Equal(Receipt(JsonNode.Parse(result.EventBytes)!), result.State.Receipts[^1].ReceiptId);
        Assert.Equal(Digest(inputBytes), result.State.Receipts[^1].CommandHash);
        Assert.Equal(Digest(result.EventBytes), result.State.Receipts[^1].EventHash);
        var member = Assert.Single(result.State.Members);
        Assert.Equal(CampaignElementReserveStatus.ReserveI, member.Status);
        Assert.Equal(result.State.Receipts[^1].ReceiptId, member.History.DesignationReceiptId);
        Assert.Equal(before.Members[0].Unit, member.Unit);
        Assert.Equal(before.Members[0].SpentCp, member.SpentCp);
        Assert.Equal(10, member.BaseCpa);
        var beforeNode = JsonNode.Parse(beforeBytes)!;
        var afterNode = JsonNode.Parse(afterBytes)!;
        var changed = afterNode["world"]!["elements"]!.AsArray().Single(e => e!["elementId"]!.GetValue<string>() == input.Command.ElementId)!;
        changed["reserveStatus"] = "none";
        foreach (var key in new[] { "world", "initiativeHolder", "operationStageOrders", "operationStageWeather", "randomState", "sequencePosition" })
            Assert.True(JsonNode.DeepEquals(beforeNode[key], afterNode[key]), key);
        foreach (var key in new[] { "cycle", "cycleId", "openingBaseHash", "completionReceiptId" }) Assert.Null(afterNode[key]);
        var retry = CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, [result.EventBytes], input);
        Assert.True(retry.Duplicate);
        Assert.Equal(result.EventBytes, retry.EventBytes);
        Assert.Equal(afterBytes, CampaignCombatReserveCodec.SerializeState(retry.State));
        Assert.Equal(afterBytes, CampaignCombatReserveCodec.SerializeState(CampaignCombatReserveDesignation.ReadState(afterBytes, request, created, opening, [weather], stage, [result.EventBytes])));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.Command(result.State));
        Assert.ThrowsAny<JsonException>(() => CampaignV11PreambleCodec.Deserialize(result.EventBytes));
        Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(afterBytes, created, request));
    }

    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void ActorAndIdentityChecksPrecedeRetryAndCompletionRemainsClosed(string choice)
    {
        var (request, created, opening, weather, stage) = Chain(0, choice);
        var before = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, []);
        var input = CampaignCombatReserveDesignation.Command(before);
        var accepted = CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, [], input);
        var changed = new[] {
            input with { Actor = CampaignOpeningPreambleActor.System },
            input with { Actor = input.Actor == CampaignOpeningPreambleActor.Axis ? CampaignOpeningPreambleActor.Commonwealth : CampaignOpeningPreambleActor.Axis },
            input with { Command = input.Command with { ContractVersion = 1 } },
            input with { Command = input.Command with { Kind = "complete-reserve-designation" } },
            input with { Command = input.Command with { ElementId = "element.unknown" } },
            input with { Command = input.Command with { ElementId = before.World.Elements.Single(e => e.ElementId != input.Command.ElementId).ElementId } },
            input with { Command = input.Command with { CreationBinding = "creation.foreign" } },
            input with { Command = input.Command with { CreationEventHash = "sha256:" + new string('0', 64) } },
            input with { Command = input.Command with { ExpectedPriorVersion = 11 } },
            input with { Command = input.Command with { ExpectedPositionId = "land.position.foreign" } },
        };
        foreach (var value in changed)
            foreach (var history in new[] { Array.Empty<byte[]>(), new[] { accepted.EventBytes } })
                Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, history, value));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, [accepted.EventBytes, accepted.EventBytes]));
    }

    [Fact]
    public void CompleteCreationRootedPredecessorsRequiredAndForeignCachesRejected()
    {
        var (request, created, opening, weather, stage) = Chain();
        foreach (var records in new[] { stage.Take(3).ToArray(), stage.Reverse().ToArray(), stage.Append(stage[^1]).ToArray(), Enumerable.Repeat(stage[0], 4).ToArray() })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], records, []));
        foreach (var records in new[] { Array.Empty<byte[]>(), new[] { weather, weather }, new[] { stage[0] } })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.Replay(request, created, opening, records, stage, []));
        foreach (var records in new[] { opening.Skip(1).ToArray(), opening.Reverse().ToArray(), opening.Append(opening[^1]).ToArray() })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.Replay(request, created, records, [weather], stage, []));
        var opposite = Chain(0, "act-last");
        var state = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, []);
        var bytes = CampaignCombatReserveCodec.SerializeState(state);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.ReadState(bytes, request, created, opposite.Opening, [opposite.Weather], opposite.Stage, []));
        var foreign = Chain(1);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.Replay(request, created, foreign.Opening, [foreign.Weather], foreign.Stage, []));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.Replay(request, [], opening, [weather], stage, []));
    }

    [Fact]
    public void ScalarRawAndResignedForgeriesRejectAgainstIndependentHistory()
    {
        var (request, created, opening, weather, stage) = Chain();
        var state = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, []);
        var input = CampaignCombatReserveDesignation.Command(state);
        var result = CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, [], input);
        foreach (var bytes in Mutations(CampaignCombatReserveCodec.SerializeInput(input)))
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, [], CampaignCombatReserveCodec.DeserializeInput(bytes)));
        foreach (var bytes in Mutations(result.EventBytes))
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, [bytes]));
        foreach (var current in new[] { state, result.State })
        {
            var bytes = CampaignCombatReserveCodec.SerializeState(current);
            foreach (var forged in Mutations(bytes))
                Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.ReadState(forged, request, created, opening, [weather], stage,
                    current.StateVersion == 10 ? [] : [result.EventBytes]));
        }
        foreach (var (field, value) in new[] { ("elementId", state.World.Elements.Single(e => e.ElementId != input.Command.ElementId).ElementId),
            ("resultingStatus", "II"), ("priorStatus", "I"), ("actingSide", "commonwealth"),
            ("priorPrefix", "sha256:" + new string('0', 64)), ("receiptId", "rd." + new string('0', 64)) })
        {
            var node = JsonNode.Parse(result.EventBytes)!;
            node[field] = value;
            if (field == "elementId") node["input"]!["command"]!["elementId"] = value;
            if (field != "receiptId") node["receiptId"] = Receipt(node);
            var bytes = Encoding.UTF8.GetBytes(node.ToJsonString());
            Assert.False(bytes.AsSpan().SequenceEqual(result.EventBytes));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, [bytes]));
            var cache = JsonNode.Parse(CampaignCombatReserveCodec.SerializeState(result.State))!;
            cache["prefix"] = Prefix(state.Prefix, bytes);
            cache["receipts"]![9]!["commandHash"] = Digest(Encoding.UTF8.GetBytes(node["input"]!.ToJsonString()));
            cache["receipts"]![9]!["eventHash"] = Digest(bytes);
            cache["receipts"]![9]!["receiptId"] = node["receiptId"]!.GetValue<string>();
            cache["members"]![0]!["history"]!["designationReceiptId"] = node["receiptId"]!.GetValue<string>();
            var cacheBytes = Encoding.UTF8.GetBytes(cache.ToJsonString());
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.ReadState(cacheBytes, request, created, opening, [weather], stage, [bytes]));
        }
    }

    [Fact]
    public void ReturnedProjectionDoesNotRetainCallerBuffersAndInitialWorldReaderStaysStrict()
    {
        var (request, created, opening, weather, stage) = Chain();
        var state = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, []);
        var result = CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, [], CampaignCombatReserveDesignation.Command(state));
        var restored = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, [result.EventBytes]);
        var expected = CampaignCombatReserveCodec.SerializeState(restored);
        Array.Fill(created, (byte)0); Array.Fill(weather, (byte)0); Array.Fill(result.EventBytes, (byte)0);
        foreach (var bytes in opening.Concat(stage)) Array.Fill(bytes, (byte)0);
        Assert.Equal(expected, CampaignCombatReserveCodec.SerializeState(restored));
        Assert.ThrowsAny<JsonException>(() => CampaignWorldV7InitialCodec.Serialize(restored.World, request.Context.Setup.Artifact,
            request.Context.Setup.Scenario, request.Context.Setup.CombatInitialization));
    }

    [Fact]
    public void MissingOversizedAndCompletionRecordsReject()
    {
        var (request, created, opening, weather, stage) = Chain();
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes("null") })
        {
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCodec.DeserializeInput(bytes));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, [bytes]));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.ReadState(bytes, request, created, opening, [weather], stage, []));
        }
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, [null!]));
        var state = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, []);
        var result = CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, [], CampaignCombatReserveDesignation.Command(state));
        var completion = JsonNode.Parse(result.EventBytes)!;
        completion["eventType"] = "reserve-designation-completed";
        completion["receiptId"] = Receipt(completion);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage,
            [Encoding.UTF8.GetBytes(completion.ToJsonString())]));
    }

    [Fact]
    public void BoundedWorldWriterRejectsTypedResourceLocationAndOpponentChanges()
    {
        var (request, created, opening, weather, stage) = Chain();
        var state = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, []);
        var result = CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, [], CampaignCombatReserveDesignation.Command(state));
        foreach (var current in new[] { state, result.State })
            foreach (var change in new[] { "cp", "ammo", "location", "opponent-reserve" })
            {
                var world = current.World;
                var original = world.Elements.Single(e => e.ElementId != current.Members[0].Unit.ElementId);
                var ledger = original.OperationalState;
                var operational = change == "cp" ? new CampaignElementOperationalStateV6(1, 1, new CapabilityPointAmount(1, 1),
                    ledger.CohesionLevel, ledger.VehicleBreakdownState, ledger.MovementEnded, ledger.InitialLedgerOrigin) : ledger;
                var location = change == "location" ? world.Elements.Single(e => e != original).CurrentLocationId : original.CurrentLocationId;
                var replacement = new CampaignElementStateV6(original.ElementId, location,
                    change == "opponent-reserve" ? CampaignElementReserveStatus.ReserveI : original.ReserveStatus,
                    operational, original.Components, original.SourceParentFormationId, original.CurrentParentFormationId,
                    change == "ammo" ? new CampaignElementAmmunitionState(1, original.Ammunition.InitialAmmunitionOrigin) : original.Ammunition, original.Readiness);
                var altered = new CampaignWorldSnapshotV7(7, world.CreationBinding,
                    world.Elements.Select(e => e == original ? replacement : e), world.Representations.Select(r => r.BoundElementIds.Contains(original.ElementId)
                        ? new CampaignMapRepresentationState(r.RepresentationId, location, r.BindingKind, r.BoundElementIds) : r),
                    world.BrokenVehicleLots, world.CohesionCauses, world.Relationships, world.CustodyLots, world.Guards,
                    world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
                Assert.NotEqual(world, altered);
                var forged = new CampaignCombatReserveState(current.Stage, current.StateVersion, current.Prefix, current.FirstActingSide,
                    altered, current.Members, current.Receipts, current.Events);
                Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveCodec.SerializeState(forged));
            }
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
        return "rd." + Digest(Encoding.UTF8.GetBytes("sandtable.combat.reserve-designation-receipt.v2\0" + unsigned.ToJsonString()))[7..];
    }
    private static string Prefix(string prior, byte[] bytes)
    {
        var length = BitConverter.GetBytes((ulong)bytes.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(length);
        return Digest(Encoding.ASCII.GetBytes("sandtable.cycle.prefix.event.v1\0")
            .Concat(Convert.FromHexString(prior[7..])).Concat(length).Concat(bytes).ToArray());
    }
}
