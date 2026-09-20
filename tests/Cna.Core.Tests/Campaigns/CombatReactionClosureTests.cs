using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatReactionClosureTests
{
    [Theory]
    [InlineData("act-first", "axis")]
    [InlineData("act-last", "commonwealth")]
    public void SixFrozenForksRetainThirtyArtifactsAndTwelveCuts(string choice, string side)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        using var fixture = Fixture();
        Assert.Equal(6, fixture.RootElement.GetProperty("cases").GetArrayLength());
        var cases = fixture.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("name").GetString()!.StartsWith(side + "-", StringComparison.Ordinal)).ToArray();
        Assert.Equal(3, cases.Length);
        foreach (var item in cases)
        {
            var goldens = item.GetProperty("goldens");
            var before = Replay(p, moves, trigger, []);
            var input = CampaignCombatReactionClosure.Command(before, item.GetProperty("kind").GetString()!);
            var inputBytes = CampaignCombatReactionClosureCodec.SerializeInput(input);
            Assert.Equal(input, CampaignCombatReactionClosureCodec.DeserializeInput(inputBytes));
            var beforeBytes = CampaignCombatReactionClosureCodec.SerializeState(before);
            CheckGolden(goldens, "trigger", trigger[0]); CheckGolden(goldens, "input", inputBytes);
            CheckGolden(goldens, "state-before", beforeBytes);
            Assert.Equal(CampaignCombatReactionTriggerCodec.SerializeState(before.Predecessor.Trigger), beforeBytes);
            Assert.Equal(beforeBytes, CampaignCombatReactionClosureCodec.SerializeState(Read(beforeBytes, p, moves, trigger, [])));
            var result = Apply(p, moves, trigger, [], input);
            Assert.False(result.Duplicate); CheckGolden(goldens, "event", result.EventBytes);
            var afterBytes = CampaignCombatReactionClosureCodec.SerializeState(result.State);
            CheckGolden(goldens, "state-after", afterBytes);
            Assert.Equal(afterBytes, CampaignCombatReactionClosureCodec.SerializeState(Replay(p, moves, trigger, [result.EventBytes])));
            Assert.Equal(afterBytes, CampaignCombatReactionClosureCodec.SerializeState(Read(afterBytes, p, moves, trigger, [result.EventBytes])));
            Assert.Equal(14, result.State.StateVersion); Assert.Null(result.State.ReactionWindow);
            Assert.Equal(before.Receipts.Count + 1, result.State.Receipts.Count);
            Assert.Equal(before.Receipts, result.State.Receipts.Take(before.Receipts.Count));
            var phasing = result.State.World.Elements.Single(e => e.ElementId == before.Predecessor.Trigger.Members[0].Unit.ElementId);
            var reacting = result.State.World.Elements.Single(e => e.ElementId != phasing.ElementId);
            Assert.Equal(new CapabilityPointAmount(4, 1), phasing.OperationalState.CapabilityPointsExpended);
            Assert.Equal(new CapabilityPointAmount(0, 1), reacting.OperationalState.CapabilityPointsExpended);
            Assert.Equal(side == "axis" ? "assault-west" : "assault-east", phasing.CurrentLocationId);
            Assert.Equal(side == "axis" ? "assault-east" : "assault-west", reacting.CurrentLocationId);
            Assert.ThrowsAny<JsonException>(() => CampaignEventSerializer.Deserialize(result.EventBytes));
            Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(afterBytes, p.Created, p.Request));
            Assert.Same(before.World, before.Predecessor.World);
            Assert.Equal(Prefix(before.Prefix, result.EventBytes), result.State.Prefix);
            var b = JsonNode.Parse(beforeBytes)!; var a = JsonNode.Parse(afterBytes)!;
            foreach (var field in b.AsObject().Select(x => x.Key).Except(["stateVersion", "prefix", "receipts", "currentPosition", "reactionWindow", "breakdownFlow"]))
                Assert.True(JsonNode.DeepEquals(b[field], a[field]), field);
            Assert.True(JsonNode.DeepEquals(b["breakdownFlow"]!["phasingContinuation"]!["route"], a["breakdownFlow"]!["route"]));
            var ev = JsonNode.Parse(result.EventBytes)!;
            Assert.Equal(24, ev.AsObject().Count); Assert.Equal(item.GetProperty("reason").GetString(), ev["reason"]!.GetValue<string>());
            Assert.Equal(input.Actor == CampaignOpeningPreambleActor.System ? null : input.Actor.ToString().ToLowerInvariant(), ev["actingSide"]?.GetValue<string>());
            Assert.Equal(b["reactionWindow"]!["frozenOpportunities"]![0]!["opportunityId"]!.GetValue<string>(), ev["closedOpportunityIds"]![0]!.GetValue<string>());
            Assert.NotEqual(input.Command.WindowId, ev["windowId"]!.GetValue<string>());
            Assert.Equal(Receipt(ev), ev["receiptId"]!.GetValue<string>());
            Assert.Equal(Digest(inputBytes), result.State.Receipts[^1].CommandHash);
            Assert.Equal(Digest(result.EventBytes), result.State.Receipts[^1].EventHash);
            var retry = Apply(p, moves, trigger, [result.EventBytes], input);
            Assert.True(retry.Duplicate); Assert.Equal(result.EventBytes, retry.EventBytes);
            Assert.Equal(afterBytes, CampaignCombatReactionClosureCodec.SerializeState(retry.State));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionClosure.Command(result.State, input.Command.Kind));
        }
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void ReasonForksPublicAuthorityAndActorsRejectBeforeAndAfterRetry(string choice)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        var state = Replay(p, moves, trigger, []);
        var inputs = Kinds.Select(kind => CampaignCombatReactionClosure.Command(state, kind)).ToArray();
        Assert.Equal(3, inputs.Select(x => x.Command.Identity.ActionId).Distinct().Count());
        var node = JsonNode.Parse(CampaignCombatReactionClosureCodec.SerializeState(state))!;
        var publicWindow = Digest(Utf8(new JsonObject
        {
            ["domain"] = "sandtable.observation.reaction-window.v1",
            ["campaignId"] = node["campaignId"]!.DeepClone(),
            ["rulesetHash"] = node["rulesetHash"]!.DeepClone(),
            ["committedStateVersion"] = 13,
            ["reactingSide"] = node["reactionWindow"]!["reactingSide"]!.DeepClone()
        }));
        foreach (var input in inputs)
        {
            Assert.Equal(publicWindow, input.Command.WindowId);
            Assert.Equal(Digest(Utf8(new JsonObject { ["contractVersion"] = 1, ["kind"] = input.Command.Kind, ["windowId"] = publicWindow })), input.Command.Identity.ActionId);
            var result = Apply(p, moves, trigger, [], input);
            foreach (var actor in Enum.GetValues<CampaignOpeningPreambleActor>().Where(x => x != input.Actor).Append((CampaignOpeningPreambleActor)99))
            {
                Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, [], input with { Actor = actor }));
                Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, [result.EventBytes], input with { Actor = actor }));
            }
            foreach (var alternate in inputs.Where(x => x != input))
            {
                Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, [result.EventBytes], alternate));
                var crossed = input with { Command = input.Command with { Identity = input.Command.Identity with { ActionId = alternate.Command.Identity.ActionId } } };
                Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, [], crossed));
            }
            var command = input.Command; var id = command.Identity;
            foreach (var bad in new[] {
                command with { WindowId = state.ReactionWindow!.Trigger.WindowId.Value },
                command with { WindowId = "sha256:" + new string('f', 64) },
                command with { Kind = "close-reaction-window-no-eligible-reactor" },
                command with { Identity = id with { ContractVersion = 1 } },
                command with { Identity = id with { ExpectedPriorVersion = 12 } },
                command with { Identity = id with { ExpectedPriorVersion = 14 } },
                command with { Identity = id with { ExpectedPriorVersion = long.MaxValue } },
                command with { Identity = id with { ExpectedPositionId = "land.position.breakdown-stop" } },
                command with { Identity = id with { CreationBinding = "different-creation" } },
                command with { Identity = id with { CreationEventHash = "sha256:" + new string('f', 64) } },
                command with { Identity = id with { CycleId = "sha256:" + new string('f', 64) } }
            })
            {
                Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, [], input with { Command = bad }));
                Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, [result.EventBytes], input with { Command = bad }));
            }
            Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [result.EventBytes, result.EventBytes]));
        }
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void CanonicalAndResignedEffectsAndCachesReject(string choice)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        var state = Replay(p, moves, trigger, []);
        foreach (var kind in Kinds)
        {
            var input = CampaignCombatReactionClosure.Command(state, kind); var result = Apply(p, moves, trigger, [], input);
            foreach (var bytes in Mutations(CampaignCombatReactionClosureCodec.SerializeInput(input)))
                Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, [], CampaignCombatReactionClosureCodec.DeserializeInput(bytes)));
            foreach (var bytes in Mutations(result.EventBytes)) Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [bytes]));
            var original = JsonNode.Parse(result.EventBytes)!;
            foreach (var path in Leaves(original, []))
            {
                var changed = original.DeepClone(); ChangeLeaf(changed, path);
                if (path[0] != "receiptId") changed["receiptId"] = Receipt(changed);
                var bytes = Utf8(changed);
                Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [bytes]));
                var cache = JsonNode.Parse(CampaignCombatReactionClosureCodec.SerializeState(result.State))!;
                cache["prefix"] = Prefix(state.Prefix, bytes);
                cache["receipts"]!.AsArray()[^1]!["eventHash"] = Digest(bytes);
                cache["receipts"]!.AsArray()[^1]!["receiptId"] = changed["receiptId"]!.DeepClone();
                Assert.ThrowsAny<JsonException>(() => Read(Utf8(cache), p, moves, trigger, [bytes]));
            }
            foreach (var cut in new[] { state, result.State })
                foreach (var bytes in Mutations(CampaignCombatReactionClosureCodec.SerializeState(cut)))
                    Assert.ThrowsAny<JsonException>(() => Read(bytes, p, moves, trigger, cut == state ? [] : [result.EventBytes]));
            foreach (var ids in new[] { new JsonArray(), new JsonArray("sha256:" + new string('f', 64)), new JsonArray(original["closedOpportunityIds"]![0]!.DeepClone(), original["closedOpportunityIds"]![0]!.DeepClone()) })
            {
                var changed = original.DeepClone(); changed["closedOpportunityIds"] = ids; changed["receiptId"] = Receipt(changed);
                Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [Utf8(changed)]));
            }
        }
    }
    [Fact]
    public void ActiveLifecycleForeignAndAlteredActualHistoryReject()
    {
        var p = Ready(); var moves = FirstMove(p); var trigger = Trigger(p, moves); var state = Replay(p, moves, trigger, []);
        var input = CampaignCombatReactionClosure.Command(state, Kinds[0]);
        var result = Apply(p, moves, trigger, [], input);
        var lifecycle = new List<byte[]>();
        for (var i = 0; i < 4; i++)
        {
            var active = CampaignCombatReactionLifecycle.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, lifecycle);
            var step = CampaignCombatReactionLifecycle.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, lifecycle, CampaignCombatReactionLifecycle.Command(active));
            lifecycle.Add(step.EventBytes);
            Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, lifecycle, input));
            Assert.ThrowsAny<JsonException>(() => Apply(p, moves, [step.EventBytes], [], input));
            Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger.Concat(lifecycle).ToArray(), [], input));
        }
        var foreign = Ready(1, "act-last"); var foreignMoves = FirstMove(foreign, "act-last"); var foreignTrigger = Trigger(foreign, foreignMoves);
        Assert.ThrowsAny<JsonException>(() => Apply(foreign, foreignMoves, foreignTrigger, [], input));
        Assert.ThrowsAny<JsonException>(() => Replay(foreign, foreignMoves, foreignTrigger, [result.EventBytes]));
        Assert.ThrowsAny<JsonException>(() => Replay(p, moves, [], []));
        Assert.ThrowsAny<JsonException>(() => Replay(p, [], trigger, []));
        Assert.ThrowsAny<JsonException>(() => Replay(Ready(1, "act-first", true), moves, trigger, []));
        foreach (var buffer in p.Opening.Concat(p.Stage).Concat(p.Reserve).Concat(moves).Concat(trigger).Append(p.Created).Append(p.Weather))
        {
            var saved = buffer[0]; buffer[0] = (byte)' ';
            Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [result.EventBytes])); buffer[0] = saved;
        }
        foreach (var pair in new[] { (foreignMoves, trigger), (moves, foreignTrigger) })
            Assert.ThrowsAny<JsonException>(() => Replay(p, pair.Item1, pair.Item2, []));
    }
    [Fact]
    public void WholeWorldForgeryRejectsAndAllBuffersDetach()
    {
        var p = Ready(); var moves = FirstMove(p); var trigger = Trigger(p, moves); var state = Replay(p, moves, trigger, []);
        var before = state.Predecessor; var world = before.World; var element = world.Elements[0]; var ledger = element.OperationalState;
        var changed = new CampaignElementStateV6(element.ElementId, element.CurrentLocationId, element.ReserveStatus,
            new CampaignElementOperationalStateV6(1, 1, new CapabilityPointAmount(7, 1), ledger.CohesionLevel,
                ledger.VehicleBreakdownState, ledger.MovementEnded, ledger.InitialLedgerOrigin), element.Components,
            element.SourceParentFormationId, element.CurrentParentFormationId, element.Ammunition, element.Readiness);
        var altered = new CampaignWorldSnapshotV7(7, world.CreationBinding, world.Elements.Select(e => e == element ? changed : e),
            world.Representations, world.BrokenVehicleLots, world.CohesionCauses, world.Relationships, world.CustodyLots,
            world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
        var forgedPredecessor = new CampaignCombatReactionLifecycleState(before.Trigger, before.StateVersion, before.Prefix, altered,
            before.ReactionWindow, before.BreakdownFlow, before.Receipts, before.Tracks, before.ActualProgressRefs, before.Events);
        var forged = new CampaignCombatReactionClosureState(forgedPredecessor, state.StateVersion, state.Prefix, state.BreakdownFlow, state.Receipts, null);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionClosureCodec.SerializeState(forged));
        var input = CampaignCombatReactionClosure.Command(state, Kinds[0]); var result = Apply(p, moves, trigger, [], input);
        var eventCopy = result.EventBytes.ToArray(); var bytes = CampaignCombatReactionClosureCodec.SerializeState(result.State);
        var inputBytes = CampaignCombatReactionClosureCodec.SerializeInput(input); var detached = CampaignCombatReactionClosureCodec.DeserializeInput(inputBytes);
        Array.Fill(inputBytes, (byte)0); Assert.Equal(input, detached);
        var replay = Replay(p, moves, trigger, [result.EventBytes]);
        foreach (var buffer in p.Opening.Concat(p.Stage).Concat(p.Reserve).Concat(moves).Concat(trigger).Append(p.Created).Append(p.Weather).Append(result.EventBytes)) Array.Fill(buffer, (byte)0);
        Assert.Equal(bytes, CampaignCombatReactionClosureCodec.SerializeState(result.State));
        Assert.Equal(bytes, CampaignCombatReactionClosureCodec.SerializeState(replay));
        Assert.Equal(eventCopy, CampaignCombatReactionClosureCodec.SerializeEvent(replay.Accepted!));
    }
    [Fact]
    public void NullMalformedDepthByteAndItemBoundsReject()
    {
        var p = Ready(); var moves = FirstMove(p); var trigger = Trigger(p, moves);
        var input = CampaignCombatReactionClosure.Command(Replay(p, moves, trigger, []), Kinds[0]);
        var result = Apply(p, moves, trigger, [], input);
        var excessive = JsonNode.Parse(result.EventBytes)!;
        excessive["closedOpportunityIds"] = new JsonArray(Enumerable.Range(0, 513).Select(_ => (JsonNode?)JsonValue.Create("sha256:" + new string('f', 64))).ToArray());
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes("null"), Encoding.UTF8.GetBytes("{}"), new byte[] { 0xff },
            Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)), Utf8(excessive) })
        {
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionClosureCodec.DeserializeInput(bytes));
            Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [bytes]));
            Assert.ThrowsAny<JsonException>(() => Read(bytes, p, moves, trigger, []));
        }
        Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [null!]));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionClosureCodec.SerializeInput(null!));
        var withReason = JsonNode.Parse(CampaignCombatReactionClosureCodec.SerializeInput(input))!;
        withReason["command"]!["reason"] = "player-decline";
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionClosureCodec.DeserializeInput(Utf8(withReason)));
        var empty = JsonNode.Parse(CampaignCombatReactionClosureCodec.SerializeState(Replay(p, moves, trigger, [])))!;
        foreach (var edit in new Action<JsonNode>[] {
            n => n["reactionWindow"]!["frozenOpportunities"] = new JsonArray(),
            n => n["reactionWindow"]!["frozenOpportunities"]!.AsArray().Add(n["reactionWindow"]!["frozenOpportunities"]![0]!.DeepClone()),
            n => n["reactionWindow"]!["activeOpportunityId"] = n["reactionWindow"]!["frozenOpportunities"]![0]!["opportunityId"]!.DeepClone(),
            n => n["reactionWindow"]!["resolvedOpportunityIds"]!.AsArray().Add(n["reactionWindow"]!["frozenOpportunities"]![0]!["opportunityId"]!.DeepClone()) })
        { var cache = empty.DeepClone(); edit(cache); Assert.ThrowsAny<JsonException>(() => Read(Utf8(cache), p, moves, trigger, [])); }
    }
    private static readonly string[] Kinds = ["decline-reaction-window", "close-reaction-window-scripted-unavailable", "close-reaction-window-timeout"];
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
            text.Replace("\"contractVersion\":3,", "\"contractVersion\":3.0,", StringComparison.Ordinal)
                .Replace("\"contractVersion\":2,", "\"contractVersion\":2.0,", StringComparison.Ordinal)
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
        {
            if (array.Count == 0) yield return path;
            for (var i = 0; i < array.Count; i++)
                foreach (var leaf in Leaves(array[i], [.. path, i.ToString(System.Globalization.CultureInfo.InvariantCulture)])) yield return leaf;
        }
        else yield return path;
    }
    private static void ChangeLeaf(JsonNode node, string[] path)
    {
        foreach (var part in path[..^1]) node = node is JsonArray a ? a[int.Parse(part, System.Globalization.CultureInfo.InvariantCulture)]! : node[part]!;
        var key = path[^1]; var old = node is JsonArray list ? list[int.Parse(key, System.Globalization.CultureInfo.InvariantCulture)] : node[key];
        JsonNode? replacement = old is null ? JsonValue.Create("unexpected") : old is JsonArray ? new JsonArray("unexpected") : old is JsonValue value && value.TryGetValue<bool>(out var boolean) ? JsonValue.Create(!boolean) : old is JsonValue number && number.TryGetValue<long>(out var integer) ? JsonValue.Create(integer + 1) : JsonValue.Create((old.GetValue<string>().StartsWith("sha256:", StringComparison.Ordinal) ? "sha256:" + new string('f', 64) : old.GetValue<string>() + "x"));
        if (node is JsonArray array) array[int.Parse(key, System.Globalization.CultureInfo.InvariantCulture)] = replacement; else node[key] = replacement;
    }
    private static byte[] Utf8(JsonNode value) => Encoding.UTF8.GetBytes(value.ToJsonString());
    private sealed record Pack(CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather, byte[][] Stage, byte[][] Reserve);
    private static byte[][] FirstMove(Pack p, string choice = "act-first")
    {
        var state = CampaignCombatInheritedMovement.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, []);
        return [CampaignCombatInheritedMovement.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, [], CampaignCombatInheritedMovement.Command(state, choice == "act-first" ? "axis-rear" : "commonwealth-rear")).EventBytes];
    }
    private static byte[][] Trigger(Pack p, byte[][] moves)
    {
        var state = CampaignCombatReactionTrigger.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, []);
        return [CampaignCombatReactionTrigger.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, [], CampaignCombatReactionTrigger.Command(state)).EventBytes];
    }
    private static CampaignCombatReactionClosureState Replay(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events) => CampaignCombatReactionClosure.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, events);
    private static CampaignCombatReactionClosureResult Apply(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events, CampaignCombatReactionClosureInput input) => CampaignCombatReactionClosure.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, events, input);
    private static CampaignCombatReactionClosureState Read(byte[] bytes, Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events) => CampaignCombatReactionClosure.ReadState(bytes, p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, events);
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

    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
        "Campaigns", "Fixtures", "combat-inherited-reaction-closure-v1.json")));
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
        return "irl." + Digest(Encoding.UTF8.GetBytes("sandtable.combat.inherited-reaction-window-closed-receipt.v3\0" + unsigned.ToJsonString()))[7..];
    }
}
