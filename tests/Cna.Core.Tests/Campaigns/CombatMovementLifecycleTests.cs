using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatMovementLifecycleTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var fixture = Fixture();
        foreach (var row in fixture.RootElement.GetProperty("cases").EnumerateArray())
            yield return [row.GetProperty("name").GetString()!, row.GetProperty("actor").GetString()!, row.GetProperty("moves").GetInt32()];
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void FrozenLifecycleCapturesScopeResolvesAndDerivesActualEndProof(string name, string side, int count)
    {
        using var fixture = Fixture();
        var row = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("name").GetString() == name);
        var golden = row.GetProperty("goldens");
        var (request, created, opening, weather, stage, reserve, moves) = Moved(side, count);
        CheckGolden(golden, 0, JsonSerializer.SerializeToUtf8Bytes(new[] { CampaignCombatCreationRequestCodec.Serialize(request), created }
            .Concat(opening).Append(weather).Concat(stage).Concat(reserve).Concat(moves).Select(Digest)));
        var events = new List<byte[]>(); var inputs = new List<CampaignCombatMovementLifecycleInput>();
        var initial = CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves, events);
        var immutable = ImmutableFields(CampaignCombatMovementLifecycleCodec.SerializeState(initial));
        for (var cut = 0; cut <= 3; cut++)
        {
            var state = CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves, events);
            var bytes = CampaignCombatMovementLifecycleCodec.SerializeState(state);
            CheckGolden(golden, 7 + cut, bytes);
            Assert.Equal(immutable, ImmutableFields(bytes));
            Assert.Equal(initial.StateVersion + cut, state.StateVersion);
            Assert.Equal(initial.Receipts, state.Receipts.Take(initial.Receipts.Count));
            Assert.Equal(bytes, CampaignCombatMovementLifecycleCodec.SerializeState(CampaignCombatMovementLifecycle.ReadState(bytes,
                request, created, opening, [weather], stage, reserve, moves, events)));
            Assert.Null(state.SequencePosition.ActiveSide);
            Assert.Equal(5, state.SequencePosition.ContractVersion);
            Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(bytes, created, request));
            for (var accepted = 0; accepted < inputs.Count; accepted++)
            {
                var retry = CampaignCombatMovementLifecycle.Apply(request, created, opening, [weather], stage, reserve, moves, events, inputs[accepted]);
                Assert.True(retry.Duplicate); Assert.Equal(events[accepted], retry.EventBytes);
                Assert.Equal(bytes, CampaignCombatMovementLifecycleCodec.SerializeState(retry.State));
                foreach (var actor in Enum.GetValues<CampaignOpeningPreambleActor>().Where(a => a != inputs[accepted].Actor))
                    Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Apply(request, created, opening, [weather], stage, reserve, moves, events, inputs[accepted] with { Actor = actor }));
            }
            if (cut == 1)
            {
                Assert.IsType<CampaignBreakdownFlow.PhasingStop>(state.BreakdownFlow);
                Assert.Equal(initial.SequencePosition, state.InterruptContext!.SequencePosition);
                Assert.Equal(state.Movement.Opening.Cycle, state.InterruptContext.Cycle);
                Assert.Equal("land.position.breakdown-stop", state.SequencePosition.PositionId);
            }
            if (cut == 2)
            {
                Assert.IsType<CampaignBreakdownFlow.Idle>(state.BreakdownFlow);
                Assert.Null(state.InterruptContext); Assert.Equal(initial.SequencePosition, state.SequencePosition);
            }
            if (cut == 3)
            {
                Assert.IsType<CampaignBreakdownFlow.Idle>(state.BreakdownFlow);
                Assert.Null(state.InterruptContext);
                Assert.Equal("land.position.operation-1.first-player.movement-and-combat.breakdown-determination", state.SequencePosition.PositionId);
                var proof = Assert.IsType<CampaignCombatMovementEndProof>(state.MovementEnd);
                Assert.Equal(1, proof.Ordinal); Assert.Equal(state.Receipts[^1].ReceiptId, proof.CompletionReceiptId);
                Assert.NotEqual(state.Movement.Opening.CompletionReceiptId, proof.CompletionReceiptId);
                Assert.Equal(2, proof.EndLocations.Count);
                Assert.Collection(proof.EndLocations, location => Assert.Equal("axis", location.Unit.OriginalSide),
                    location => Assert.Equal("commonwealth", location.Unit.OriginalSide));
                foreach (var location in proof.EndLocations)
                    Assert.Equal(state.Movement.World.Elements.Single(e => e.ElementId == location.Unit.ElementId).CurrentLocationId, location.LocationId);
                Assert.Equal(row.GetProperty("expected").GetProperty("excluded").GetBoolean() ? 1 : 0, proof.ExcludedBefore.Count);
                if (proof.ExcludedBefore.Count != 0) Assert.Equal(state.Movement.Members[0].Unit, proof.ExcludedBefore[0]);
                Assert.Equal(state.Movement.Opening.Cycle!.ActingSide, proof.Scope.ActingSide);
                Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Command(state));
                break;
            }
            Assert.Null(state.MovementEnd);
            var input = CampaignCombatMovementLifecycle.Command(state);
            var inputBytes = CampaignCombatMovementLifecycleCodec.SerializeInput(input);
            CheckGolden(golden, 1 + cut, inputBytes);
            Assert.Equal(input, CampaignCombatMovementLifecycleCodec.DeserializeInput(inputBytes));
            CheckCapabilityAndAction(state, input, inputBytes, cut);
            var result = CampaignCombatMovementLifecycle.Apply(request, created, opening, [weather], stage, reserve, moves, events, input);
            Assert.False(result.Duplicate); CheckGolden(golden, 4 + cut, result.EventBytes);
            Assert.Equal(Prefix(state.Prefix, result.EventBytes), result.State.Prefix);
            Assert.Equal(Receipt(JsonNode.Parse(result.EventBytes)!, cut), result.State.Receipts[^1].ReceiptId);
            Assert.Equal(Digest(inputBytes), result.State.Receipts[^1].CommandHash);
            Assert.Equal(Digest(result.EventBytes), result.State.Receipts[^1].EventHash);
            Assert.ThrowsAny<JsonException>(() => CampaignEventSerializer.Deserialize(result.EventBytes));
            inputs.Add(input); events.Add(result.EventBytes);
        }
    }

    [Fact]
    public void IdentityCapabilitiesAndConsumedOccurrencesRejectBeforeRetry()
    {
        var (request, created, opening, weather, stage, reserve, moves) = Moved();
        var events = new List<byte[]>(); var inputs = new List<CampaignCombatMovementLifecycleInput>();
        for (var step = 0; step < 3; step++)
        {
            var state = CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves, events);
            var input = CampaignCombatMovementLifecycle.Command(state); inputs.Add(input);
            events.Add(CampaignCombatMovementLifecycle.Apply(request, created, opening, [weather], stage, reserve, moves, events, input).EventBytes);
        }
        foreach (var input in inputs)
            foreach (var identity in new[] {
                input.Command.Identity with { CreationBinding = "creation.foreign" },
                input.Command.Identity with { CreationEventHash = "sha256:" + new string('0', 64) },
                input.Command.Identity with { CycleId = "sha256:" + new string('0', 64) },
                input.Command.Identity with { ActionId = "sha256:" + new string('0', 64) },
                input.Command.Identity with { ExpectedPriorVersion = input.Command.Identity.ExpectedPriorVersion + 1 },
                input.Command.Identity with { ExpectedPositionId = "land.position.foreign" },
                input.Command.Identity with { ContractVersion = 1 },
            }) Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Apply(request, created, opening, [weather], stage, reserve, moves, events,
                input with { Command = input.Command with { Identity = identity } }));
        foreach (var original in inputs.Take(2))
        {
            var node = JsonNode.Parse(CampaignCombatMovementLifecycleCodec.SerializeInput(original))!;
            node["command"]![original.Command.Kind == "stop-element-movement" ? "routeId" : "stopId"] = "sha256:" + new string('0', 64);
            var changed = CampaignCombatMovementLifecycleCodec.DeserializeInput(Encoding.UTF8.GetBytes(node.ToJsonString()));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Apply(request, created, opening, [weather], stage, reserve, moves, events, changed));
        }
    }

    [Fact]
    public void MissingReorderedInterleavedAndPostCompletionHistoriesReject()
    {
        var (request, created, opening, weather, stage, reserve, moves) = Moved();
        var events = Lifecycle(request, created, opening, weather, stage, reserve, moves);
        foreach (var records in new[] { events.Reverse().ToArray(), events.Skip(1).ToArray(), new[] { events[0], events[0] }, events.Append(events[^1]).ToArray(), new[] { events[0], moves[0] }, new[] { events[2] } })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves, records));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, [], []));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Replay(request, created, opening, [], stage, reserve, moves, events));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage.Take(3).ToArray(), reserve, moves, events));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, [], moves, events));
        var foreign = Moved("commonwealth");
        Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, foreign.Moves, events));
    }

    [Fact]
    public void RawScalarAndCoherentlyResignedProofsRejectAgainstCausalHistory()
    {
        var (request, created, opening, weather, stage, reserve, moves) = Moved("axis", 6);
        var events = Lifecycle(request, created, opening, weather, stage, reserve, moves);
        for (var cut = 0; cut <= 3; cut++)
        {
            var retained = events.Take(cut).ToArray();
            var state = CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves, retained);
            foreach (var forged in Mutations(CampaignCombatMovementLifecycleCodec.SerializeState(state)))
                Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.ReadState(forged, request, created, opening, [weather], stage, reserve, moves, retained));
            if (cut == 3) continue;
            foreach (var forged in Mutations(events[cut]))
                Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves, retained.Append(forged).ToArray()));
            foreach (var forged in Mutations(CampaignCombatMovementLifecycleCodec.SerializeInput(CampaignCombatMovementLifecycle.Command(state))))
                Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Apply(request, created, opening, [weather], stage, reserve, moves, retained,
                    CampaignCombatMovementLifecycleCodec.DeserializeInput(forged)));
        }
        var stopFork = JsonNode.Parse(events[0])!;
        stopFork["interruptContextAfter"]!["cycle"]!["ordinal"] = 2;
        stopFork["receiptId"] = Receipt(stopFork, 0);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves,
            [Encoding.UTF8.GetBytes(stopFork.ToJsonString())]));
        var progressFork = JsonNode.Parse(events[2])!;
        progressFork["progress"]![0] = progressFork["progress"]![1]!.DeepClone();
        progressFork["receiptId"] = Receipt(progressFork, 2);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves,
            [events[0], events[1], Encoding.UTF8.GetBytes(progressFork.ToJsonString())]));
        var completion = JsonNode.Parse(events[2])!;
        completion["excludedUnits"] = new JsonArray(); completion["receiptId"] = Receipt(completion, 2);
        var forgedBytes = Encoding.UTF8.GetBytes(completion.ToJsonString());
        Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves, [events[0], events[1], forgedBytes]));
    }

    [Fact]
    public void LimitsAndCallerBufferIsolationHold()
    {
        var (request, created, opening, weather, stage, reserve, moves) = Moved();
        var events = Lifecycle(request, created, opening, weather, stage, reserve, moves);
        var state = CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves, events);
        var expected = CampaignCombatMovementLifecycleCodec.SerializeState(state);
        var result = CampaignCombatMovementLifecycle.Apply(request, created, opening, [weather], stage, reserve, moves, events, state.Events[^1].Input);
        Array.Fill(result.EventBytes, (byte)0);
        Assert.Equal(expected, CampaignCombatMovementLifecycleCodec.SerializeState(result.State));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves, [null!]));
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes("null") })
        {
            Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycleCodec.DeserializeInput(bytes));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.ReadState(bytes, request, created, opening, [weather], stage, reserve, moves, events));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves, [bytes]));
        }
        foreach (var buffer in new[] { created, weather }.Concat(opening).Concat(stage).Concat(reserve).Concat(moves).Concat(events)) Array.Fill(buffer, (byte)0);
        Assert.Equal(expected, CampaignCombatMovementLifecycleCodec.SerializeState(state));
    }

    private static void CheckCapabilityAndAction(CampaignCombatMovementLifecycleState state, CampaignCombatMovementLifecycleInput input, byte[] bytes, int index)
    {
        var command = JsonNode.Parse(bytes)!["command"]!;
        var action = new JsonObject { ["contractVersion"] = 1, ["kind"] = input.Command.Kind };
        if (index < 2)
        {
            var cycle = state.Movement.Opening.Cycle!;
            var capability = new JsonObject
            {
                ["domain"] = index == 0 ? "sandtable.observation.movement-route.v1" : "sandtable.action.breakdown-stop.v1",
                ["campaignId"] = cycle.CampaignId,
                ["rulesetHash"] = cycle.RulesetHash,
                ["stateVersion"] = state.StateVersion,
                ["audience"] = index == 0 ? CampaignSnapshotSerializer.FormatSide(cycle.ActingSide) : "system"
            };
            if (index == 0)
            {
                var route = Assert.IsType<CampaignBreakdownFlow.Moving>(state.BreakdownFlow).Route;
                capability["elementId"] = route.ElementId; capability["originLocationId"] = route.OriginLocationId;
                capability["currentLocationId"] = route.CurrentLocationId;
                Assert.NotEqual(route.RouteId, command["routeId"]!.GetValue<string>());
            }
            else Assert.NotEqual(Assert.IsType<CampaignBreakdownFlow.PhasingStop>(state.BreakdownFlow).Stop.StopId, command["stopId"]!.GetValue<string>());
            var field = index == 0 ? "routeId" : "stopId";
            Assert.Equal(Digest(Encoding.UTF8.GetBytes(capability.ToJsonString())), command[field]!.GetValue<string>());
            action[field] = command[field]!.DeepClone();
        }
        Assert.Equal(Digest(Encoding.UTF8.GetBytes(action.ToJsonString())), input.Command.Identity.ActionId);
    }
    private static string ImmutableFields(byte[] bytes)
    {
        var node = JsonNode.Parse(bytes)!;
        foreach (var field in new[] { "stateVersion", "prefix", "receipts", "sequencePosition", "breakdownFlow", "interruptContext", "movementEnd" }) node.AsObject().Remove(field);
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
        "Campaigns", "Fixtures", "combat-inherited-movement-lifecycle-v1.json")));
    private static void CheckGolden(JsonElement golden, int index, byte[] bytes)
    {
        Assert.Equal(golden.GetProperty("bytes")[index].GetInt32(), bytes.Length);
        Assert.Equal(golden.GetProperty("sha256")[index].GetString(), Digest(bytes));
    }
    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static string Receipt(JsonNode node, int index)
    {
        var copy = node.DeepClone(); copy.AsObject().Remove("receiptId");
        var domain = new[] { "sandtable.combat.inherited-movement-stop-receipt.v2", "sandtable.combat.inherited-breakdown-stop-resolved-receipt.v2", "sandtable.combat.inherited-movement-completion-receipt.v3" }[index];
        return "iml." + Digest(Encoding.UTF8.GetBytes(domain + "\0" + copy.ToJsonString()))[7..];
    }
}
