using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatInheritedMovementTests
{
    private static readonly JsonSerializerOptions PredecessorJson = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    [Theory]
    [InlineData("axis-ordinary-clear-route", "act-first")]
    [InlineData("commonwealth-ordinary-clear-route", "act-last")]
    public void FrozenMovesPreserveResourcesAndProveCumulativeOrdinaryCost(string name, string choice)
    {
        using var fixture = Fixture();
        var row = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("name").GetString() == name);
        var goldens = row.GetProperty("goldens");
        var route = row.GetProperty("expected").GetProperty("route").EnumerateArray().Select(x => x.GetString()!).ToArray();
        var (request, created, opening, weather, stage, reserve) = Ready(1, choice);
        CheckGolden(goldens, "request", CampaignCombatCreationRequestCodec.Serialize(request));
        CheckGolden(goldens, "predecessor-records", JsonSerializer.SerializeToUtf8Bytes(new[] { created }.Concat(opening).Append(weather).Concat(stage).Concat(reserve)
            .Select(bytes => Encoding.UTF8.GetString(bytes)), PredecessorJson));
        var history = new List<byte[]>();
        var inputs = new List<CampaignCombatInheritedMovementInput>();
        var initial = CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, reserve, history);
        var immutable = ImmutableFields(CampaignCombatInheritedMovementCodec.SerializeState(initial));
        for (var cut = 0; cut <= 7; cut++)
        {
            var state = CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, reserve, history);
            var bytes = CampaignCombatInheritedMovementCodec.SerializeState(state);
            CheckGolden(goldens, $"state-{11 + cut}", bytes);
            Assert.Equal(immutable, ImmutableFields(bytes));
            Assert.Equal(11 + cut, state.StateVersion);
            Assert.Equal(10 + cut, state.Receipts.Count);
            Assert.Equal(initial.Receipts, state.Receipts.Take(10));
            Assert.Equal(bytes, CampaignCombatInheritedMovementCodec.SerializeState(CampaignCombatInheritedMovement.ReadState(bytes,
                request, created, opening, [weather], stage, reserve, history)));
            var member = Assert.Single(state.Members);
            var moved = state.World.Elements.Single(e => e.ElementId == member.Unit.ElementId);
            Assert.Equal(route[cut], moved.CurrentLocationId);
            Assert.Equal(new CapabilityPointAmount(cut * 2, 1), moved.OperationalState.CapabilityPointsExpended);
            Assert.Equal(moved.OperationalState.CapabilityPointsExpended, member.SpentCp);
            Assert.Equal(-Math.Max(0, cut * 2 - 10), moved.OperationalState.CohesionLevel);
            Assert.Equal(initial.Members[0].History, member.History);
            Assert.Equal(cut, state.ActualProgressRefs.Count);
            Assert.Equal(Math.Max(0, cut - 5), state.World.CohesionCauses.Count);
            if (cut == 0) { Assert.Empty(state.Tracks); Assert.Null(state.BreakdownFlow); }
            else
            {
                Assert.Equal(route.Take(cut + 1), Assert.Single(state.Tracks).Route);
                Assert.Equal(route[cut], state.BreakdownFlow!.Route.CurrentLocationId);
                Assert.Equal(12, state.BreakdownFlow.Route.FirstMoveStateVersion);
                Assert.Equal(route[0], state.BreakdownFlow.Route.OriginLocationId);
                Assert.Equal(RouteIdentity(state), state.BreakdownFlow.Route.RouteId);
                Assert.Empty(state.BreakdownFlow.Route.CohortIds);
            }
            foreach (var cause in state.World.CohesionCauses)
            {
                Assert.Equal(2, cause.Points);
                Assert.Equal("ordinary-movement-excess-cp-dp", cause.Kind);
                Assert.Equal(state.Receipts[14 + cause.Ordinal].ReceiptId, cause.ReceiptId);
                Assert.Equal(cause.ReceiptId + ".dp", cause.CauseId);
            }
            for (var prior = 0; prior < inputs.Count; prior++)
            {
                var retry = CampaignCombatInheritedMovement.Apply(request, created, opening, [weather], stage, reserve, history, inputs[prior]);
                Assert.True(retry.Duplicate);
                Assert.Equal(history[prior], retry.EventBytes);
                Assert.Equal(bytes, CampaignCombatInheritedMovementCodec.SerializeState(retry.State));
            }
            Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(bytes, created, request));
            if (cut == 7)
            {
                var eighth = CampaignCombatInheritedMovement.Command(state, route[6]);
                Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Apply(request, created, opening, [weather], stage, reserve, history, eighth));
                break;
            }
            var input = CampaignCombatInheritedMovement.Command(state, route[cut + 1]);
            var inputBytes = CampaignCombatInheritedMovementCodec.SerializeInput(input);
            CheckGolden(goldens, $"input-{cut + 1}", inputBytes);
            Assert.Equal(input, CampaignCombatInheritedMovementCodec.DeserializeInput(inputBytes));
            var result = CampaignCombatInheritedMovement.Apply(request, created, opening, [weather], stage, reserve, history, input);
            CheckGolden(goldens, $"event-{cut + 1}", result.EventBytes);
            Assert.False(result.Duplicate);
            Assert.Equal(Prefix(state.Prefix, result.EventBytes), result.State.Prefix);
            Assert.Equal(Receipt(JsonNode.Parse(result.EventBytes)!), result.State.Receipts[^1].ReceiptId);
            Assert.Equal(Digest(inputBytes), result.State.Receipts[^1].CommandHash);
            Assert.Equal(Digest(result.EventBytes), result.State.ActualProgressRefs[^1].EventHash);
            Assert.ThrowsAny<JsonException>(() => CampaignEventSerializer.Deserialize(result.EventBytes));
            inputs.Add(input); history.Add(result.EventBytes);
        }
    }

    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void WrongActorsUnitsOriginsDestinationsAndRetryConflictsReject(string choice)
    {
        var (request, created, opening, weather, stage, reserve) = Ready(1, choice);
        var state = CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, reserve, []);
        var side = choice == "act-first" ? "axis" : "commonwealth";
        var input = CampaignCombatInheritedMovement.Command(state, side + "-rear");
        var result = CampaignCombatInheritedMovement.Apply(request, created, opening, [weather], stage, reserve, [], input);
        var opposite = state.World.Elements.Single(e => e.ElementId != input.Command.Unit.ElementId);
        foreach (var changed in new[] {
            input with { Actor = CampaignOpeningPreambleActor.System },
            input with { Actor = input.Actor == CampaignOpeningPreambleActor.Axis ? CampaignOpeningPreambleActor.Commonwealth : CampaignOpeningPreambleActor.Axis },
            input with { Command = input.Command with { Unit = new CampaignCombatUnitKey(request.CreationBinding, side, opposite.ElementId) } },
            input with { Command = input.Command with { OriginLocationId = side + "-supply" } },
            input with { Command = input.Command with { DestinationLocationId = side + "-supply" } },
            input with { Command = input.Command with { DestinationLocationId = opposite.CurrentLocationId } },
            input with { Command = input.Command with { DestinationLocationId = input.Command.OriginLocationId } },
            input with { Command = input.Command with { CycleId = "sha256:" + new string('0', 64) } },
            input with { Command = input.Command with { CreationEventHash = "sha256:" + new string('0', 64) } },
            input with { Command = input.Command with { CreationBinding = "creation.foreign" } },
            input with { Command = input.Command with { ExpectedPriorVersion = 12 } },
            input with { Command = input.Command with { ExpectedPositionId = "land.position.foreign" } },
            input with { Command = input.Command with { ContractVersion = 1 } },
        }) foreach (var history in new[] { Array.Empty<byte[]>(), new[] { result.EventBytes } })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Apply(request, created, opening, [weather], stage, reserve, history, changed));
        var adjacentEnemy = CampaignCombatInheritedMovement.Command(result.State, input.Command.OriginLocationId);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Apply(request, created, opening, [weather], stage, reserve, [result.EventBytes], adjacentEnemy));
    }

    [Fact]
    public void UnsupportedProfilesAndIncompleteOrForeignHistoryReject()
    {
        foreach (var seed in new ulong[] { 0, 2, 3 })
        {
            var p = Ready(seed);
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, []));
        }
        var positiveReserve = Ready(1, "act-first", true);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Replay(positiveReserve.Request, positiveReserve.Created, positiveReserve.Opening,
            [positiveReserve.Weather], positiveReserve.Stage, positiveReserve.Reserve, []));
        var (request, created, opening, weather, stage, reserve) = Ready();
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, [], []));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, reserve.Concat(reserve).ToArray(), []));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Replay(request, created, opening, [], stage, reserve, []));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage.Take(3).ToArray(), reserve, []));
        var foreign = Ready(1, "act-last");
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, foreign.Reserve, []));
    }

    [Fact]
    public void RawScalarAndResignedEffectsRejectDespiteConsistentReceiptAndCache()
    {
        var (request, created, opening, weather, stage, reserve) = Ready();
        var state = CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, reserve, []);
        var input = CampaignCombatInheritedMovement.Command(state, "axis-rear");
        var result = CampaignCombatInheritedMovement.Apply(request, created, opening, [weather], stage, reserve, [], input);
        foreach (var forged in Mutations(CampaignCombatInheritedMovementCodec.SerializeInput(input)))
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Apply(request, created, opening, [weather], stage, reserve, [], CampaignCombatInheritedMovementCodec.DeserializeInput(forged)));
        foreach (var forged in Mutations(result.EventBytes))
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, reserve, [forged]));
        foreach (var forged in Mutations(CampaignCombatInheritedMovementCodec.SerializeState(result.State)))
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.ReadState(forged, request, created, opening, [weather], stage, reserve, [result.EventBytes]));
        var effect = JsonNode.Parse(result.EventBytes)!;
        effect["capabilityPointsExpendedAfter"]!["numerator"] = 1;
        effect["receiptId"] = Receipt(effect);
        var bytes = Encoding.UTF8.GetBytes(effect.ToJsonString());
        var cache = JsonNode.Parse(CampaignCombatInheritedMovementCodec.SerializeState(result.State))!;
        cache["prefix"] = Prefix(state.Prefix, bytes);
        cache["receipts"]![10]!["receiptId"] = effect["receiptId"]!.DeepClone();
        cache["receipts"]![10]!["eventHash"] = Digest(bytes);
        cache["actualProgressRefs"]![0]!["receiptId"] = effect["receiptId"]!.DeepClone();
        cache["actualProgressRefs"]![0]!["eventHash"] = Digest(bytes);
        cache["members"]![0]!["spentCp"]!["numerator"] = 1;
        cache["world"]!["elements"]![0]!["operationalState"]!["capabilityPointsExpended"]!["numerator"] = 1;
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.ReadState(Encoding.UTF8.GetBytes(cache.ToJsonString()), request, created, opening, [weather], stage, reserve, [bytes]));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, reserve, [result.EventBytes, result.EventBytes]));
        foreach (var missing in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes("null") })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, reserve, [missing]));
    }

    [Fact]
    public void ProjectionDoesNotRetainCallerBuffersOrEraseMovedWorldWithInitialWriter()
    {
        var (request, created, opening, weather, stage, reserve) = Ready();
        var initial = CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, reserve, []);
        var result = CampaignCombatInheritedMovement.Apply(request, created, opening, [weather], stage, reserve, [], CampaignCombatInheritedMovement.Command(initial, "axis-rear"));
        var expected = CampaignCombatInheritedMovementCodec.SerializeState(result.State);
        foreach (var buffer in new[] { created, weather, result.EventBytes }.Concat(opening).Concat(stage).Concat(reserve)) Array.Fill(buffer, (byte)0);
        Assert.Equal(expected, CampaignCombatInheritedMovementCodec.SerializeState(result.State));
        Assert.ThrowsAny<JsonException>(() => CampaignWorldV7InitialCodec.Serialize(result.State.World, request.Context.Setup.Artifact,
            request.Context.Setup.Scenario, request.Context.Setup.CombatInitialization));
    }

    [Fact]
    public void BoundedMovedWorldWriterRejectsTypedForgedResourcesAndMissingCausalDp()
    {
        var (request, created, opening, weather, stage, reserve) = Ready();
        var history = new List<byte[]>();
        var state = CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, reserve, history);
        for (var move = 0; move < 7; move++)
        {
            var destination = move % 2 == 0 ? "axis-rear" : "axis-supply";
            var result = CampaignCombatInheritedMovement.Apply(request, created, opening, [weather], stage, reserve, history,
                CampaignCombatInheritedMovement.Command(state, destination));
            state = result.State; history.Add(result.EventBytes);
        }
        foreach (var change in new[] { "opponent-cp", "opponent-location", "own-ammo", "missing-dp" })
        {
            var world = state.World;
            var original = world.Elements.Single(e => change == "own-ammo" ? e.ElementId == state.Members[0].Unit.ElementId : e.ElementId != state.Members[0].Unit.ElementId);
            var ledger = original.OperationalState;
            var operational = change == "opponent-cp" ? new CampaignElementOperationalStateV6(1, 1, new CapabilityPointAmount(2, 1),
                ledger.CohesionLevel, ledger.VehicleBreakdownState, ledger.MovementEnded, ledger.InitialLedgerOrigin) : ledger;
            var location = change == "opponent-location" ? "commonwealth-rear" : original.CurrentLocationId;
            var replacement = new CampaignElementStateV6(original.ElementId, location, original.ReserveStatus,
                operational, original.Components, original.SourceParentFormationId, original.CurrentParentFormationId,
                change == "own-ammo" ? new CampaignElementAmmunitionState(9, original.Ammunition.InitialAmmunitionOrigin) : original.Ammunition, original.Readiness);
            var altered = new CampaignWorldSnapshotV7(7, world.CreationBinding, world.Elements.Select(e => e == original ? replacement : e),
                world.Representations.Select(r => r.BoundElementIds.Contains(original.ElementId)
                    ? new CampaignMapRepresentationState(r.RepresentationId, location, r.BindingKind, r.BoundElementIds) : r),
                world.BrokenVehicleLots, change == "missing-dp" ? [] : world.CohesionCauses, world.Relationships, world.CustodyLots,
                world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
            Assert.NotEqual(world, altered);
            var forged = new CampaignCombatInheritedMovementState(state.Opening, state.StateVersion, state.Prefix, altered,
                state.Members, state.Receipts, state.Tracks, state.ActualProgressRefs, state.BreakdownFlow, state.Events);
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovementCodec.SerializeState(forged));
        }
        var finalBytes = CampaignCombatInheritedMovementCodec.SerializeState(state);
        var cache = JsonNode.Parse(finalBytes)!;
        cache["world"]!["cohesionCauses"]![0]!["receiptId"] = state.Receipts[^1].ReceiptId;
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.ReadState(Encoding.UTF8.GetBytes(cache.ToJsonString()),
            request, created, opening, [weather], stage, reserve, history));
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes("null") })
        {
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovementCodec.DeserializeInput(bytes));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.ReadState(bytes, request, created, opening, [weather], stage, reserve, history));
        }
    }

    private static string ImmutableFields(byte[] bytes)
    {
        var root = JsonNode.Parse(bytes)!;
        foreach (var field in new[] { "stateVersion", "prefix", "receipts", "tracks", "actualProgressRefs", "breakdownFlow" }) root.AsObject().Remove(field);
        foreach (var member in root["members"]!.AsArray()) member!["spentCp"] = null;
        foreach (var element in root["world"]!["elements"]!.AsArray())
        {
            element!["currentLocationId"] = null;
            element["operationalState"]!["capabilityPointsExpended"] = null;
            element["operationalState"]!["cohesionLevel"] = null;
        }
        foreach (var representation in root["world"]!["representations"]!.AsArray()) representation!["currentLocationId"] = null;
        root["world"]!["cohesionCauses"] = null;
        return root.ToJsonString();
    }
    private static string RouteIdentity(CampaignCombatInheritedMovementState state)
    {
        var route = state.BreakdownFlow!.Route;
        var node = new JsonObject
        {
            ["domain"] = "sandtable.breakdown.route.v1",
            ["campaignId"] = state.Opening.Predecessor.Stage.Weather.Opening.Creation.CampaignId,
            ["rulesetHash"] = state.Opening.Predecessor.Stage.Weather.Opening.Creation.RulesetHash,
            ["firstMoveStateVersion"] = route.FirstMoveStateVersion,
            ["elementId"] = route.ElementId,
            ["representationId"] = route.RepresentationId,
            ["owner"] = CampaignSnapshotSerializer.FormatSide(route.Owner),
            ["originLocationId"] = route.OriginLocationId,
            ["cohortIds"] = new JsonArray()
        };
        return Digest(Encoding.UTF8.GetBytes(node.ToJsonString()));
    }
    private static string Prefix(string prior, byte[] bytes)
    {
        var length = BitConverter.GetBytes((ulong)bytes.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(length);
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
        "Campaigns", "Fixtures", "combat-inherited-movement-v1.json")));
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
        return "imv." + Digest(Encoding.UTF8.GetBytes("sandtable.combat.inherited-movement-receipt.v4\0" + unsigned.ToJsonString()))[7..];
    }
}
