using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatBreakdownCompletionTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var fixture = Fixture();
        foreach (var row in fixture.RootElement.GetProperty("cases").EnumerateArray()) yield return [row.GetProperty("actor").GetString()!, row.GetProperty("moves").GetInt32()];
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void FrozenSystemCompletionEntersCombatAndPreservesActualProof(string side, int count)
    {
        using var fixture = Fixture();
        var row = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("actor").GetString() == side && c.GetProperty("moves").GetInt32() == count);
        var golden = row.GetProperty("goldens");
        var (request, created, opening, weather, stage, reserve, moves) = Moved(side, count);
        var lifecycle = Lifecycle(request, created, opening, weather, stage, reserve, moves);
        CheckGolden(golden, 0, JsonSerializer.SerializeToUtf8Bytes(new[] { CampaignCombatCreationRequestCodec.Serialize(request), created }
            .Concat(opening).Append(weather).Concat(stage).Concat(reserve).Concat(moves).Concat(lifecycle).Select(Digest)));
        var before = CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, reserve, moves, lifecycle, []);
        var beforeBytes = CampaignCombatBreakdownCompletionCodec.SerializeState(before);
        CheckGolden(golden, 3, beforeBytes);
        Assert.Null(before.BreakdownCompletionReceiptId);
        var input = CampaignCombatBreakdownCompletion.Command(before);
        var inputBytes = CampaignCombatBreakdownCompletionCodec.SerializeInput(input);
        CheckGolden(golden, 1, inputBytes);
        Assert.Equal(input, CampaignCombatBreakdownCompletionCodec.DeserializeInput(inputBytes));
        Assert.Equal(CampaignOpeningPreambleActor.System, input.Actor);
        Assert.Equal(Digest(Encoding.UTF8.GetBytes("{\"contractVersion\":1,\"kind\":\"complete-breakdown-segment\",\"operationStage\":1}")), input.Command.ActionId);
        var result = CampaignCombatBreakdownCompletion.Apply(request, created, opening, [weather], stage, reserve, moves, lifecycle, [], input);
        CheckGolden(golden, 2, result.EventBytes);
        var afterBytes = CampaignCombatBreakdownCompletionCodec.SerializeState(result.State);
        CheckGolden(golden, 4, afterBytes);
        Assert.False(result.Duplicate);
        Assert.Equal(15 + count, result.State.StateVersion);
        Assert.Equal(before.Receipts, result.State.Receipts.Take(before.Receipts.Count));
        Assert.Equal(Prefix(before.Prefix, result.EventBytes), result.State.Prefix);
        Assert.Equal(Receipt(JsonNode.Parse(result.EventBytes)!), result.State.BreakdownCompletionReceiptId);
        Assert.Equal(Digest(inputBytes), result.State.Receipts[^1].CommandHash);
        Assert.Equal(Digest(result.EventBytes), result.State.Receipts[^1].EventHash);
        Assert.Equal("land.position.operation-1.first-player.movement-and-combat.combat.position-determination", result.State.SequencePosition.PositionId);
        Assert.Equal(LandActorRole.FirstActingSide, result.State.SequencePosition.ActorRole);
        Assert.Null(result.State.SequencePosition.ActiveSide); Assert.Equal(5, result.State.SequencePosition.ContractVersion);
        Assert.Equal(ImmutableFields(beforeBytes), ImmutableFields(afterBytes));
        var proof = result.State.Lifecycle.MovementEnd!;
        Assert.Equal(row.GetProperty("expectedExclusions").GetInt32(), proof.ExcludedBefore.Count);
        var owner = result.State.Lifecycle.Movement.Members[0];
        var element = result.State.Lifecycle.Movement.World.Elements.Single(e => e.ElementId == owner.Unit.ElementId);
        Assert.Equal(new CapabilityPointAmount(row.GetProperty("expectedCp").GetInt64(), 1), element.OperationalState.CapabilityPointsExpended);
        Assert.Equal(row.GetProperty("expectedCohesion").GetInt32(), element.OperationalState.CohesionLevel);
        Assert.NotEqual(proof.CompletionReceiptId, result.State.BreakdownCompletionReceiptId);
        Assert.NotEqual(result.State.Lifecycle.Movement.Opening.CompletionReceiptId, result.State.BreakdownCompletionReceiptId);
        var node = JsonNode.Parse(result.EventBytes)!;
        Assert.Equal(proof.CompletionReceiptId, node["movementCompletionReceiptId"]!.GetValue<string>());
        Assert.Equal("5.2,7.11,7.14", string.Join(',', node["sources"]!.AsArray().Select(x => x!["locator"]!.GetValue<string>())));
        Assert.Equal(before.SequencePosition.Sources, before.Lifecycle.SequencePosition.Sources);
        var retry = CampaignCombatBreakdownCompletion.Apply(request, created, opening, [weather], stage, reserve, moves, lifecycle, [result.EventBytes], input);
        Assert.True(retry.Duplicate); Assert.Equal(result.EventBytes, retry.EventBytes);
        Assert.Equal(afterBytes, CampaignCombatBreakdownCompletionCodec.SerializeState(retry.State));
        foreach (var (bytes, history) in new[] { (beforeBytes, Array.Empty<byte[]>()), (afterBytes, new[] { result.EventBytes }) })
            Assert.Equal(bytes, CampaignCombatBreakdownCompletionCodec.SerializeState(CampaignCombatBreakdownCompletion.ReadState(bytes, request, created, opening, [weather], stage, reserve, moves, lifecycle, history)));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.Command(result.State));
        Assert.ThrowsAny<JsonException>(() => CampaignEventSerializer.Deserialize(result.EventBytes));
        Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(afterBytes, created, request));
    }

    [Fact]
    public void ActorActionIdentityAndRetryConflictsRejectBeforeAdmission()
    {
        var (request, created, opening, weather, stage, reserve, moves) = Moved();
        var lifecycle = Lifecycle(request, created, opening, weather, stage, reserve, moves);
        var state = CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, reserve, moves, lifecycle, []);
        var input = CampaignCombatBreakdownCompletion.Command(state);
        var result = CampaignCombatBreakdownCompletion.Apply(request, created, opening, [weather], stage, reserve, moves, lifecycle, [], input);
        foreach (var changed in new[] {
            input with { Actor = CampaignOpeningPreambleActor.Axis }, input with { Actor = CampaignOpeningPreambleActor.Commonwealth },
            input with { Command = input.Command with { ActionId = "sha256:" + new string('0', 64) } },
            input with { Command = input.Command with { CreationBinding = "creation.foreign" } },
            input with { Command = input.Command with { CreationEventHash = "sha256:" + new string('0', 64) } },
            input with { Command = input.Command with { CycleId = "sha256:" + new string('0', 64) } },
            input with { Command = input.Command with { ExpectedPriorVersion = result.State.StateVersion } },
            input with { Command = input.Command with { ExpectedPositionId = result.State.SequencePosition.PositionId } },
            input with { Command = input.Command with { OperationStage = 2 } },
            input with { Command = input.Command with { ContractVersion = 1 } },
            input with { Command = input.Command with { Kind = "complete-movement-segment" } },
        }) foreach (var history in new[] { Array.Empty<byte[]>(), new[] { result.EventBytes } })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.Apply(request, created, opening, [weather], stage, reserve, moves, lifecycle, history, changed));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, reserve, moves, lifecycle, [result.EventBytes, result.EventBytes]));
    }

    [Fact]
    public void ActualCompleteLifecycleCannotBeReplacedWithPartialReorderedForeignOrForgedProof()
    {
        var (request, created, opening, weather, stage, reserve, moves) = Moved();
        var lifecycle = Lifecycle(request, created, opening, weather, stage, reserve, moves);
        foreach (var history in new[] { Array.Empty<byte[]>(), lifecycle.Take(2).ToArray(), lifecycle.Reverse().ToArray(), lifecycle.Skip(1).ToArray(), new[] { lifecycle[0], lifecycle[0], lifecycle[2] } })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, reserve, moves, history, []));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, reserve, [], lifecycle, []));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, [], moves, lifecycle, []));
        var foreign = Moved("commonwealth");
        var foreignLifecycle = Lifecycle(foreign.Request, foreign.Created, foreign.Opening, foreign.Weather, foreign.Stage, foreign.Reserve, foreign.Moves);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, reserve, moves, foreignLifecycle, []));
        var later = Moved("axis", 6);
        var laterLifecycle = Lifecycle(later.Request, later.Created, later.Opening, later.Weather, later.Stage, later.Reserve, later.Moves);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, reserve, moves, laterLifecycle, []));
        var proof = JsonNode.Parse(lifecycle[2])!;
        proof["endLocations"]![0]!["locationId"] = "axis-supply";
        proof.AsObject().Remove("receiptId");
        proof["receiptId"] = "iml." + Digest(Encoding.UTF8.GetBytes("sandtable.combat.inherited-movement-completion-receipt.v3\0" + proof.ToJsonString()))[7..];
        Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, reserve, moves,
            [lifecycle[0], lifecycle[1], Encoding.UTF8.GetBytes(proof.ToJsonString())], []));
    }

    [Fact]
    public void CanonicalRawScalarAndResignedSourceReceiptForgeriesReject()
    {
        var (request, created, opening, weather, stage, reserve, moves) = Moved();
        var lifecycle = Lifecycle(request, created, opening, weather, stage, reserve, moves);
        var before = CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, reserve, moves, lifecycle, []);
        var input = CampaignCombatBreakdownCompletion.Command(before);
        var result = CampaignCombatBreakdownCompletion.Apply(request, created, opening, [weather], stage, reserve, moves, lifecycle, [], input);
        foreach (var bytes in Mutations(CampaignCombatBreakdownCompletionCodec.SerializeInput(input)))
            Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.Apply(request, created, opening, [weather], stage, reserve, moves, lifecycle, [], CampaignCombatBreakdownCompletionCodec.DeserializeInput(bytes)));
        foreach (var bytes in Mutations(result.EventBytes))
            Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, reserve, moves, lifecycle, [bytes]));
        foreach (var bytes in Mutations(CampaignCombatBreakdownCompletionCodec.SerializeState(result.State)))
            Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.ReadState(bytes, request, created, opening, [weather], stage, reserve, moves, lifecycle, [result.EventBytes]));
        foreach (var field in new[] { "sources", "movementCompletionReceiptId" })
        {
            var node = JsonNode.Parse(result.EventBytes)!;
            if (field == "sources") node["sources"]![0]!["locator"] = "21.24-21.26";
            else node[field] = before.Lifecycle.Movement.Opening.CompletionReceiptId;
            node["receiptId"] = Receipt(node);
            var bytes = Encoding.UTF8.GetBytes(node.ToJsonString());
            var cache = JsonNode.Parse(CampaignCombatBreakdownCompletionCodec.SerializeState(result.State))!;
            cache["breakdownCompletionReceiptId"] = node["receiptId"]!.DeepClone();
            cache["prefix"] = Prefix(before.Prefix, bytes);
            cache["receipts"]!.AsArray()[^1]!["receiptId"] = node["receiptId"]!.DeepClone();
            cache["receipts"]!.AsArray()[^1]!["eventHash"] = Digest(bytes);
            Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.ReadState(Encoding.UTF8.GetBytes(cache.ToJsonString()), request, created, opening, [weather], stage, reserve, moves, lifecycle, [bytes]));
        }
    }

    [Fact]
    public void BoundsAndCallerBuffersCannotChangeTerminalState()
    {
        var (request, created, opening, weather, stage, reserve, moves) = Moved();
        var lifecycle = Lifecycle(request, created, opening, weather, stage, reserve, moves);
        var before = CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, reserve, moves, lifecycle, []);
        var result = CampaignCombatBreakdownCompletion.Apply(request, created, opening, [weather], stage, reserve, moves, lifecycle, [], CampaignCombatBreakdownCompletion.Command(before));
        var expected = CampaignCombatBreakdownCompletionCodec.SerializeState(result.State);
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes("null") })
        {
            Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletionCodec.DeserializeInput(bytes));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, reserve, moves, lifecycle, [bytes]));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.ReadState(bytes, request, created, opening, [weather], stage, reserve, moves, lifecycle, []));
        }
        Assert.ThrowsAny<JsonException>(() => CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, reserve, moves, lifecycle, [null!]));
        foreach (var bytes in new[] { created, weather, result.EventBytes }.Concat(opening).Concat(stage).Concat(reserve).Concat(moves).Concat(lifecycle)) Array.Fill(bytes, (byte)0);
        Assert.Equal(expected, CampaignCombatBreakdownCompletionCodec.SerializeState(result.State));
    }
    private static string ImmutableFields(byte[] bytes)
    {
        var node = JsonNode.Parse(bytes)!;
        foreach (var field in new[] { "stateVersion", "prefix", "receipts", "sequencePosition", "breakdownCompletionReceiptId" }) node.AsObject().Remove(field);
        return node.ToJsonString();
    }
    private static byte[][] Lifecycle(CampaignCombatCreationRequest request, byte[] created, byte[][] opening, byte[] weather, byte[][] stage, byte[][] reserve, byte[][] moves)
    {
        var events = new List<byte[]>();
        for (var index = 0; index < 3; index++)
        {
            var state = CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves, events);
            events.Add(CampaignCombatMovementLifecycle.Apply(request, created, opening, [weather], stage, reserve, moves, events, CampaignCombatMovementLifecycle.Command(state)).EventBytes);
        }
        return events.ToArray();
    }
    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather, byte[][] Stage, byte[][] Reserve, byte[][] Moves) Moved(string side = "axis", int count = 1)
    {
        var (request, created, opening, weather, stage, reserve) = Ready(1, side == "axis" ? "act-first" : "act-last");
        var moves = new List<byte[]>();
        for (var index = 0; index < count; index++)
        {
            var state = CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, reserve, moves);
            moves.Add(CampaignCombatInheritedMovement.Apply(request, created, opening, [weather], stage, reserve, moves,
                CampaignCombatInheritedMovement.Command(state, side + (index % 2 == 0 ? "-rear" : "-supply"))).EventBytes);
        }
        return (request, created, opening, weather, stage, reserve, moves.ToArray());
    }
    private static string Prefix(string prior, byte[] bytes)
    {
        var length = BitConverter.GetBytes((ulong)bytes.Length); if (BitConverter.IsLittleEndian) Array.Reverse(length);
        return Digest(Encoding.ASCII.GetBytes("sandtable.cycle.prefix.event.v1\0").Concat(Convert.FromHexString(prior[7..])).Concat(length).Concat(bytes).ToArray());
    }
    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather, byte[][] Stage, byte[][] Reserve) Ready(
        ulong seed = 1, string choice = "act-first", bool designated = false)
    {
        var (request, created, opening, weather, stage) = Chain(seed, choice);
        var reserve = new List<byte[]>();
        if (designated)
        {
            var state = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, []);
            reserve.Add(CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, [], CampaignCombatReserveDesignation.Command(state)).EventBytes);
        }
        var input = CampaignCombatReserveCompletion.CreateCommand(request, created, opening, [weather], stage, reserve);
        reserve.Add(CampaignCombatReserveCompletion.Create(request, created, opening, [weather], stage, reserve, input).EventBytes);
        return (request, created, opening, weather, stage, reserve.ToArray());
    }
    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather, byte[][] Stage) Chain(ulong seed = 1, string choice = "act-first")
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
        ulong seed = 1, string choice = "act-first")
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
        "Campaigns", "Fixtures", "combat-inherited-breakdown-completion-v1.json")));
    private static void CheckGolden(JsonElement golden, int index, byte[] bytes)
    {
        Assert.Equal(golden.GetProperty("bytes")[index].GetInt32(), bytes.Length);
        Assert.Equal(golden.GetProperty("sha256")[index].GetString(), Digest(bytes));
    }
    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static string Receipt(JsonNode node)
    {
        var copy = node.DeepClone(); copy.AsObject().Remove("receiptId");
        return "ibc." + Digest(Encoding.UTF8.GetBytes("sandtable.combat.inherited-breakdown-completion-receipt.v2\0" + copy.ToJsonString()))[7..];
    }
}
