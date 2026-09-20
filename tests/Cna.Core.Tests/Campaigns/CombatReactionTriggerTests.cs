using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatReactionTriggerTests
{
    [Theory]
    [InlineData("act-first", "axis-return-opens-reaction")]
    [InlineData("act-last", "commonwealth-return-opens-reaction")]
    public void FrozenTriggerDerivesWindowAndPreservesHistory(string choice, string name)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice);
        using var fixture = Fixture();
        var goldens = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(x => x.GetProperty("name").GetString() == name).GetProperty("goldens");
        CheckGolden(goldens, "movement-prefix", moves[0]);
        var before = Replay(p, moves, []);
        Assert.Equal(12, before.StateVersion);
        Assert.Equal(CampaignCombatInheritedMovementCodec.SerializeState(before.Predecessor), CampaignCombatReactionTriggerCodec.SerializeState(before));
        var input = CampaignCombatReactionTrigger.Command(before);
        CheckGolden(goldens, "input", CampaignCombatInheritedMovementCodec.SerializeInput(input));
        var result = Apply(p, moves, [], input);
        CheckGolden(goldens, "event", result.EventBytes);
        var state = result.State;
        var bytes = CampaignCombatReactionTriggerCodec.SerializeState(state);
        CheckGolden(goldens, "state", bytes);
        Assert.Equal(13, state.StateVersion); Assert.Equal(12, state.Receipts.Count);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionTrigger.Command(state));
        Assert.Equal(before.Receipts, state.Receipts.Take(11));
        Assert.Equal(before.Members[0].History, state.Members[0].History);
        Assert.Equal(new CapabilityPointAmount(4, 1), state.Members[0].SpentCp);
        Assert.Equal(0, state.World.Elements.Single(e => e.ElementId == input.Command.Unit.ElementId).OperationalState.CohesionLevel);
        Assert.Equal(before.World.Elements.Single(e => e.ElementId != input.Command.Unit.ElementId), state.World.Elements.Single(e => e.ElementId != input.Command.Unit.ElementId));
        Assert.Equal(before.Tracks[0].Route.Append(input.Command.DestinationLocationId), state.Tracks[0].Route);
        Assert.Equal(2, state.ActualProgressRefs.Count); Assert.Empty(state.World.CohesionCauses);
        var window = state.ReactionWindow!;
        Assert.Equal(4, window.TriggerAuthority.MoveContractVersion);
        Assert.Equal(13, window.TriggerCommittedStateVersion);
        Assert.Equal(before.SequencePosition, window.ReactingPosition.SuspendedMovementPosition);
        Assert.NotEqual(window.PhasingSide, window.ReactingSide);
        Assert.Empty(window.ResolvedOpportunityIds); Assert.Null(window.ActiveOpportunityId);
        var opportunity = Assert.Single(window.FrozenOpportunities);
        Assert.True(opportunity.AdjacencyEvidence.IsAdjacent);
        Assert.Equal("8.51", Assert.Single(opportunity.AdjacencyEvidence.Sources).Locator);
        var flow = Assert.IsType<CampaignBreakdownFlow.Reacting>(state.BreakdownFlow);
        Assert.Null(flow.ReactorRoute);
        var route = Assert.IsType<CampaignPhasingContinuation.ResumeRoute>(flow.PhasingContinuation).Route;
        Assert.Equal(before.Predecessor.BreakdownFlow!.Route.RouteId, route.RouteId);
        Assert.Equal(12, route.FirstMoveStateVersion); Assert.Equal(input.Command.DestinationLocationId, route.OriginLocationId);
        Assert.Equal(input.Command.DestinationLocationId, route.CurrentLocationId);
        Assert.Equal(Prefix(before.Prefix, result.EventBytes), state.Prefix);
        Assert.Equal(Receipt(JsonNode.Parse(result.EventBytes)!), state.Receipts[^1].ReceiptId);
        CheckIdentities(result.EventBytes);
        Assert.Equal(bytes, CampaignCombatReactionTriggerCodec.SerializeState(Read(bytes, p, moves, [result.EventBytes])));
        var retry = Apply(p, moves, [result.EventBytes], input);
        Assert.True(retry.Duplicate); Assert.Equal(result.EventBytes, retry.EventBytes);
        Assert.Equal(bytes, CampaignCombatReactionTriggerCodec.SerializeState(retry.State));
        Assert.ThrowsAny<JsonException>(() => CampaignEventSerializer.Deserialize(result.EventBytes));
        Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(bytes, p.Created, p.Request));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, [moves[0], result.EventBytes]));
        Assert.ThrowsAny<JsonException>(() => Read(CampaignCombatReactionTriggerCodec.SerializeState(before), p, moves, []));
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void ActorsConflictsAndFurtherOccurrencesReject(string choice)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var before = Replay(p, moves, []);
        var input = CampaignCombatReactionTrigger.Command(before); var result = Apply(p, moves, [], input);
        foreach (var changed in new[] {
            input with { Actor = CampaignOpeningPreambleActor.System },
            input with { Actor = input.Actor == CampaignOpeningPreambleActor.Axis ? CampaignOpeningPreambleActor.Commonwealth : CampaignOpeningPreambleActor.Axis },
            input with { Command = input.Command with { ContractVersion = 3 } },
            input with { Command = input.Command with { ExpectedPriorVersion = 13 } },
            input with { Command = input.Command with { OriginLocationId = input.Command.DestinationLocationId } },
            input with { Command = input.Command with { DestinationLocationId = "axis-supply" } },
            input with { Command = input.Command with { CycleId = "sha256:" + new string('0',64) } },
            input with { Command = input.Command with { CreationBinding = "foreign" } },
            input with { Command = input.Command with { ExpectedPositionId = "foreign" } },
            input with { Command = input.Command with { Unit = new(p.Request.CreationBinding, input.Command.Unit.OriginalSide, "foreign") } }
        }) foreach (var events in new[] { Array.Empty<byte[]>(), new[] { result.EventBytes } })
            Assert.ThrowsAny<JsonException>(() => Apply(p, moves, events, changed));
        Assert.ThrowsAny<JsonException>(() => Replay(p, moves, [result.EventBytes, result.EventBytes]));
    }
    [Fact]
    public void MalformedCanonicalAndResignedWindowEffectsReject()
    {
        var p = Ready(); var moves = FirstMove(p); var before = Replay(p, moves, []);
        var input = CampaignCombatReactionTrigger.Command(before); var result = Apply(p, moves, [], input);
        var stateBytes = CampaignCombatReactionTriggerCodec.SerializeState(result.State);
        foreach (var bytes in Mutations(CampaignCombatInheritedMovementCodec.SerializeInput(input)))
            Assert.ThrowsAny<JsonException>(() => Apply(p, moves, [], CampaignCombatInheritedMovementCodec.DeserializeInput(bytes)));
        foreach (var bytes in Mutations(result.EventBytes)) Assert.ThrowsAny<JsonException>(() => Replay(p, moves, [bytes]));
        foreach (var bytes in Mutations(stateBytes)) Assert.ThrowsAny<JsonException>(() => Read(bytes, p, moves, [result.EventBytes]));
        foreach (var edit in new Action<JsonNode>[] {
            n => n["openedReactionWindow"]!["triggerCommittedStateVersion"] = 14,
            n => n["openedReactionWindow"]!["frozenOpportunities"]![0]!["adjacencyEvidence"]!["triggerLocationId"] = "commonwealth-rear",
            n => n["openedReactionWindow"]!["triggerAuthority"]!["moveContractVersion"] = 3,
            n => n["openedReactionWindow"]!["reactionWindowId"] = "sha256:" + new string('0',64),
            n => n["capabilityPointsExpendedAfter"]!["numerator"] = 6,
            n => n["breakdownFlowAfter"]!["phasingContinuation"]!["route"]!["currentLocationId"] = "axis-rear"
        })
        {
            var forged = JsonNode.Parse(result.EventBytes)!; edit(forged); forged["receiptId"] = Receipt(forged);
            var bytes = Encoding.UTF8.GetBytes(forged.ToJsonString());
            var cache = JsonNode.Parse(stateBytes)!; cache["prefix"] = Prefix(before.Prefix, bytes);
            cache["receipts"]![11]!["eventHash"] = Digest(bytes); cache["receipts"]![11]!["receiptId"] = forged["receiptId"]!.DeepClone();
            Assert.ThrowsAny<JsonException>(() => Replay(p, moves, [bytes]));
            Assert.ThrowsAny<JsonException>(() => Read(Encoding.UTF8.GetBytes(cache.ToJsonString()), p, moves, [bytes]));
        }
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes("null") })
        {
            Assert.ThrowsAny<JsonException>(() => Replay(p, moves, [bytes]));
            Assert.ThrowsAny<JsonException>(() => Read(bytes, p, moves, [result.EventBytes]));
        }
        Assert.ThrowsAny<JsonException>(() => Replay(p, moves, [null!]));
        var retained = result.EventBytes.ToArray(); var originalMove = moves[0].ToArray();
        Array.Fill(result.EventBytes, (byte)0); Array.Fill(moves[0], (byte)0);
        Assert.Equal(stateBytes, CampaignCombatReactionTriggerCodec.SerializeState(result.State));
        Assert.Equal(stateBytes, CampaignCombatReactionTriggerCodec.SerializeState(Replay(p, [originalMove], [retained])));
    }
    [Fact]
    public void MissingForeignAndUnsupportedPredecessorsReject()
    {
        var p = Ready(); var moves = FirstMove(p); var other = Ready(1, "act-last");
        foreach (var history in new[] { Array.Empty<byte[]>(), new[] { moves[0], moves[0] }, FirstMove(other, "act-last") })
            Assert.ThrowsAny<JsonException>(() => Replay(p, history, []));
        Assert.ThrowsAny<JsonException>(() => Replay(p with { Reserve = [] }, moves, []));
        Assert.ThrowsAny<JsonException>(() => Replay(p with { Stage = p.Stage.Reverse().ToArray() }, moves, []));
        Assert.ThrowsAny<JsonException>(() => Replay(p with { Opening = p.Opening.Take(3).ToArray() }, moves, []));
        Assert.ThrowsAny<JsonException>(() => Replay(p with { Weather = other.Weather }, moves, []));
        foreach (var seed in new ulong[] { 0, 2, 3 }) Assert.ThrowsAny<JsonException>(() => Replay(Ready(seed), moves, []));
        Assert.ThrowsAny<JsonException>(() => Replay(Ready(1, "act-first", true), moves, []));
    }
    [Fact]
    public void BoundedWorldWriterRejectsTypedResourceForgery()
    {
        var p = Ready(); var moves = FirstMove(p); var before = Replay(p, moves, []);
        var state = Apply(p, moves, [], CampaignCombatReactionTrigger.Command(before)).State;
        var world = state.World;
        var original = world.Elements.Single(e => e.ElementId != state.Members[0].Unit.ElementId);
        var ledger = original.OperationalState;
        var changed = new CampaignElementStateV6(original.ElementId, original.CurrentLocationId, original.ReserveStatus,
            new CampaignElementOperationalStateV6(1, 1, new CapabilityPointAmount(2, 1), ledger.CohesionLevel,
                ledger.VehicleBreakdownState, ledger.MovementEnded, ledger.InitialLedgerOrigin), original.Components,
            original.SourceParentFormationId, original.CurrentParentFormationId, original.Ammunition, original.Readiness);
        var altered = new CampaignWorldSnapshotV7(7, world.CreationBinding, world.Elements.Select(e => e == original ? changed : e),
            world.Representations, world.BrokenVehicleLots, world.CohesionCauses, world.Relationships, world.CustodyLots,
            world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
        var forged = new CampaignCombatReactionTriggerState(state.Predecessor, altered, state.Members, state.Receipts,
            state.Tracks, state.ActualProgressRefs, state.Trigger, state.Prefix);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionTriggerCodec.SerializeState(forged));
        Assert.ThrowsAny<JsonException>(() => CampaignWorldV7InitialCodec.Serialize(state.World, p.Request.Context.Setup.Artifact,
            p.Request.Context.Setup.Scenario, p.Request.Context.Setup.CombatInitialization));
    }
    private static void CheckIdentities(byte[] bytes)
    {
        var e = JsonNode.Parse(bytes)!; var w = e["openedReactionWindow"]!; var a = w["triggerAuthority"]!;
        var preimage = new JsonObject
        {
            ["domain"] = "sandtable.campaign.reaction-window.v1",
            ["campaignId"] = e["campaignId"]!.DeepClone(),
            ["rulesetHash"] = e["rulesetHash"]!.DeepClone(),
            ["moveContractVersion"] = 4,
            ["committedStateVersion"] = 13,
            ["triggeringRepresentation"] = a["triggeringRepresentation"]!.DeepClone(),
            ["originLocationId"] = a["originLocationId"]!.DeepClone(),
            ["destinationLocationId"] = a["destinationLocationId"]!.DeepClone(),
            ["reactingSide"] = w["reactingSide"]!.DeepClone()
        };
        Assert.Equal(w["reactionWindowId"]!.GetValue<string>(), Digest(Encoding.UTF8.GetBytes(preimage.ToJsonString())));
        var o = w["frozenOpportunities"]![0]!;
        var opportunity = new JsonObject { ["domain"] = "sandtable.campaign.reaction-opportunity.v1", ["windowId"] = w["reactionWindowId"]!.DeepClone(), ["reactingRepresentation"] = o["reactingRepresentation"]!.DeepClone() };
        Assert.Equal(o["opportunityId"]!.GetValue<string>(), Digest(Encoding.UTF8.GetBytes(opportunity.ToJsonString())));
    }
    private sealed record Pack(CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather, byte[][] Stage, byte[][] Reserve);
    private static byte[][] FirstMove(Pack p, string choice = "act-first")
    {
        var state = CampaignCombatInheritedMovement.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, []);
        return [CampaignCombatInheritedMovement.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, [], CampaignCombatInheritedMovement.Command(state, choice == "act-first" ? "axis-rear" : "commonwealth-rear")).EventBytes];
    }
    private static CampaignCombatReactionTriggerState Replay(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> events) => CampaignCombatReactionTrigger.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, events);
    private static CampaignCombatReactionTriggerResult Apply(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> events, CampaignCombatInheritedMovementInput input) => CampaignCombatReactionTrigger.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, events, input);
    private static CampaignCombatReactionTriggerState Read(byte[] bytes, Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> events) => CampaignCombatReactionTrigger.ReadState(bytes, p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, events);
    private static string Prefix(string prior, byte[] bytes)
    {
        var length = BitConverter.GetBytes((ulong)bytes.Length);
        if (BitConverter.IsLittleEndian) Array.Reverse(length);
        return Digest(Encoding.ASCII.GetBytes("sandtable.cycle.prefix.event.v1\0").Concat(Convert.FromHexString(prior[7..])).Concat(length).Concat(bytes).ToArray());
    }
    private static Pack Ready(
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
        return new(request, created, opening, weather, stage, reserve.ToArray());
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
        "Campaigns", "Fixtures", "combat-inherited-reaction-trigger-v1.json")));
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
        return "irt." + Digest(Encoding.UTF8.GetBytes("sandtable.combat.inherited-reaction-trigger-receipt.v1\0" + unsigned.ToJsonString()))[7..];
    }
}
