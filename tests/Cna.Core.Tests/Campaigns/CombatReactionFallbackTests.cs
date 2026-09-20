using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatReactionFallbackTests
{
    [Theory]
    [InlineData("act-first", "axis")]
    [InlineData("act-last", "commonwealth")]
    public void FourFrozenForksRetainThirtyTwoArtifactsAndTwelveCuts(string choice, string side)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        using var fixture = Fixture();
        Assert.Equal(4, fixture.RootElement.GetProperty("cases").GetArrayLength());
        var cases = fixture.RootElement.GetProperty("cases").EnumerateArray().Where(c => c.GetProperty("name").GetString()!.StartsWith(side + "-", StringComparison.Ordinal)).ToArray();
        Assert.Equal(2, cases.Length);
        foreach (var item in cases)
        {
            var goldens = item.GetProperty("goldens"); var events = new List<byte[]>();
            var state = Replay(p, moves, trigger, events);
            CheckGolden(goldens, "participant", Participant(p, moves, trigger)[0]);
            var initialBytes = CampaignCombatReactionFallbackCodec.SerializeState(state);
            CheckGolden(goldens, "state-0", initialBytes);
            Assert.Equal(initialBytes, CampaignCombatReactionFallbackCodec.SerializeState(Read(initialBytes, p, moves, trigger, [])));
            for (var index = 1; index <= 2; index++)
            {
                var input = CampaignCombatReactionFallback.Command(state, index == 1 ? item.GetProperty("kind").GetString()! : "resolve-breakdown-stop");
                CheckGolden(goldens, "input-" + index, CampaignCombatReactionFallbackCodec.SerializeInput(input));
                var result = Apply(p, moves, trigger, events, input);
                Assert.False(result.Duplicate); CheckGolden(goldens, "event-" + index, result.EventBytes);
                events.Add(result.EventBytes); state = result.State;
                var bytes = CampaignCombatReactionFallbackCodec.SerializeState(state);
                CheckGolden(goldens, "state-" + index, bytes);
                Assert.Equal(bytes, CampaignCombatReactionFallbackCodec.SerializeState(Read(bytes, p, moves, trigger, events)));
            }
        }
    }
    [Theory]
    [InlineData("act-first", "axis")]
    [InlineData("act-last", "commonwealth")]
    public void StopsPreserveMaterialAndResumeOnlyAfterMandatoryResolution(string choice, string side)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        var before = Replay(p, moves, trigger, []); var b = JsonNode.Parse(CampaignCombatReactionFallbackCodec.SerializeState(before))!;
        Assert.Equal(14, before.StateVersion); Assert.Single(CampaignCombatReactionLifecycle.MoveOptions(before.Predecessor));
        Assert.Equal(CampaignCombatReactionLifecycleCodec.SerializeState(before.Predecessor), CampaignCombatReactionFallbackCodec.SerializeState(before));
        var publicWindow = Digest(Utf8(new JsonObject
        {
            ["domain"] = "sandtable.observation.reaction-window.v1",
            ["campaignId"] = b["campaignId"]!.DeepClone(),
            ["rulesetHash"] = b["rulesetHash"]!.DeepClone(),
            ["committedStateVersion"] = 13,
            ["reactingSide"] = b["reactionWindow"]!["reactingSide"]!.DeepClone()
        }));
        foreach (var kind in Kinds)
        {
            var close = CampaignCombatReactionFallback.Command(before, kind);
            Assert.Equal(publicWindow, close.Command.Handle);
            Assert.Equal(Digest(Utf8(new JsonObject { ["contractVersion"] = 1, ["kind"] = kind, ["windowId"] = publicWindow })), close.Command.Identity.ActionId);
            var first = Apply(p, moves, trigger, [], close); var stopped = first.State;
            var ev = JsonNode.Parse(first.EventBytes)!;
            Assert.Equal(24, ev.AsObject().Count); Assert.Equal(15, stopped.StateVersion); Assert.Null(stopped.ReactionWindow);
            Assert.Null(ev["actingSide"]); Assert.Equal(kind == Kinds[0] ? "scripted-unavailable" : "timeout", ev["reason"]!.GetValue<string>());
            Assert.Equal(b["reactionWindow"]!["activeOpportunityId"]!.GetValue<string>(), Assert.Single(ev["closedOpportunityIds"]!.AsArray())!.GetValue<string>());
            Assert.NotEqual(close.Command.Handle, ev["windowId"]!.GetValue<string>());
            var flow = Assert.IsType<CampaignBreakdownFlow.ReactorStopClosed>(stopped.BreakdownFlow);
            Assert.Equal(15, flow.Stop.RecordedStateVersion); Assert.Empty(flow.Stop.CohortInputs);
            Assert.Equal(kind == Kinds[0] ? CampaignBreakdownStopReason.ReactionUnavailable : CampaignBreakdownStopReason.ReactionTimeout, flow.Stop.Reason);
            Assert.Equal(BreakdownWeatherKind.Normal, flow.Stop.WeatherKind);
            Assert.Equal(((CampaignBreakdownFlow.Reacting)before.BreakdownFlow).ReactorRoute, flow.Stop.Route);
            var stop = ev["breakdownFlowAfter"]!["stop"]!;
            var identity = new JsonObject { ["domain"] = "sandtable.breakdown.stop.v1", ["campaignId"] = b["campaignId"]!.DeepClone(), ["rulesetHash"] = b["rulesetHash"]!.DeepClone() };
            foreach (var field in stop.AsObject().Where(x => x.Key != "stopId")) identity.Add(field.Key, field.Value?.DeepClone());
            Assert.Equal(Digest(Utf8(identity)), stop["stopId"]!.GetValue<string>());
            var resolve = CampaignCombatReactionFallback.Command(stopped, "resolve-breakdown-stop");
            var capability = Digest(Utf8(new JsonObject
            {
                ["domain"] = "sandtable.action.breakdown-stop.v1",
                ["campaignId"] = b["campaignId"]!.DeepClone(),
                ["rulesetHash"] = b["rulesetHash"]!.DeepClone(),
                ["stateVersion"] = 15,
                ["audience"] = "system"
            }));
            Assert.Equal(capability, resolve.Command.Handle); Assert.NotEqual(stop["stopId"]!.GetValue<string>(), capability);
            Assert.Equal(Digest(Utf8(new JsonObject { ["contractVersion"] = 1, ["kind"] = "resolve-breakdown-stop", ["stopId"] = capability })), resolve.Command.Identity.ActionId);
            var second = Apply(p, moves, trigger, [first.EventBytes], resolve); var resolved = JsonNode.Parse(second.EventBytes)!;
            Assert.Equal(26, resolved.AsObject().Count); Assert.Equal(16, second.State.StateVersion); Assert.Null(second.State.ReactionWindow);
            Assert.True(JsonNode.DeepEquals(stop, resolved["stop"])); Assert.Empty(resolved["checks"]!.AsArray()); Assert.Empty(resolved["createdLots"]!.AsArray());
            Assert.Null(resolved["interruptContextAfter"]); Assert.True(JsonNode.DeepEquals(b["randomState"], resolved["randomStateBefore"]));
            Assert.True(JsonNode.DeepEquals(b["randomState"], resolved["randomStateAfter"]));
            Assert.Equal("spi-1979-land-rules", resolved["sources"]![0]!["sourceId"]!.GetValue<string>());
            Assert.Equal("21.24-21.26", resolved["sources"]![0]!["locator"]!.GetValue<string>());
            Assert.Equal(((CampaignPhasingContinuation.ResumeRoute)flow.PhasingContinuation).Route, Assert.IsType<CampaignBreakdownFlow.Moving>(second.State.BreakdownFlow).Route);
            foreach (var result in new[] { first, second })
            {
                var state = result.State; var bytes = CampaignCombatReactionFallbackCodec.SerializeState(state); var n = JsonNode.Parse(bytes)!;
                foreach (var field in b.AsObject().Select(x => x.Key).Except(["stateVersion", "prefix", "receipts", "currentPosition", "reactionWindow", "breakdownFlow"]))
                    Assert.True(JsonNode.DeepEquals(b[field], n[field]), field);
                Assert.Equal(result == first ? "breakdown-stop" : "sequence", n["currentPosition"]!["kind"]!.GetValue<string>());
                Assert.Equal(before.Receipts, state.Receipts.Take(before.Receipts.Count)); Assert.Equal(before.Receipts.Count + (result == first ? 1 : 2), state.Receipts.Count);
                var phasing = state.World.Elements.Single(e => e.ElementId == before.Predecessor.Trigger.Members[0].Unit.ElementId);
                var reactor = state.World.Elements.Single(e => e != phasing);
                Assert.Equal(new CapabilityPointAmount(4, 1), phasing.OperationalState.CapabilityPointsExpended);
                Assert.Equal(new CapabilityPointAmount(2, 1), reactor.OperationalState.CapabilityPointsExpended);
                Assert.Equal(side == "axis" ? "assault-west" : "assault-east", phasing.CurrentLocationId);
                Assert.Equal(side == "axis" ? "commonwealth-rear" : "axis-rear", reactor.CurrentLocationId);
                var emitted = JsonNode.Parse(result.EventBytes)!; Assert.Equal(Receipt(emitted), emitted["receiptId"]!.GetValue<string>());
                Assert.Equal(Digest(result.EventBytes), state.Receipts[^1].EventHash);
                Assert.Equal(Digest(CampaignCombatReactionFallbackCodec.SerializeInput(result == first ? close : resolve)), state.Receipts[^1].CommandHash);
                Assert.Equal(Prefix(result == first ? before.Prefix : stopped.Prefix, result.EventBytes), state.Prefix);
                Assert.ThrowsAny<JsonException>(() => CampaignEventSerializer.Deserialize(result.EventBytes));
                Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(bytes, p.Created, p.Request));
            }
            foreach (var input in new[] { close, resolve })
            {
                var retry = Apply(p, moves, trigger, [first.EventBytes, second.EventBytes], input);
                Assert.True(retry.Duplicate); Assert.Equal(input == close ? first.EventBytes : second.EventBytes, retry.EventBytes);
                Assert.Equal(CampaignCombatReactionFallbackCodec.SerializeState(second.State), CampaignCombatReactionFallbackCodec.SerializeState(retry.State));
            }
            Assert.True(Apply(p, moves, trigger, [first.EventBytes], close).Duplicate);
            Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, [], resolve));
            foreach (var invalidKind in Kinds.Append("resolve-breakdown-stop").Append("decline-reaction-window").Append("close-reaction-window-no-eligible-reactor"))
                Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionFallback.Command(second.State, invalidKind));
        }
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void EveryCommandFieldActorRetryAndCompetingForkRejects(string choice)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves); var state = Replay(p, moves, trigger, []);
        var inputs = Kinds.Select(k => CampaignCombatReactionFallback.Command(state, k)).ToArray();
        Assert.Equal(2, inputs.Select(i => i.Command.Identity.ActionId).Distinct().Count());
        foreach (var close in inputs)
        {
            var first = Apply(p, moves, trigger, [], close); var resolve = CampaignCombatReactionFallback.Command(first.State, "resolve-breakdown-stop");
            var second = Apply(p, moves, trigger, [first.EventBytes], resolve); byte[][] terminal = [first.EventBytes, second.EventBytes];
            Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, terminal, inputs.Single(i => i != close)));
            Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, [first.EventBytes], inputs.Single(i => i != close)));
            foreach (var input in new[] { close, resolve })
            {
                byte[][] prior = input == close ? [] : [first.EventBytes];
                foreach (var actor in Enum.GetValues<CampaignOpeningPreambleActor>().Where(a => a != CampaignOpeningPreambleActor.System).Append((CampaignOpeningPreambleActor)99))
                    foreach (var history in new[] { prior, terminal })
                        Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, history, input with { Actor = actor }));
                var id = input.Command.Identity;
                foreach (var changed in new[] { id with { ContractVersion = 1 }, id with { ContractVersion = 3 }, id with { ActionId = "sha256:" + new string('f', 64) },
                    id with { CreationBinding = id.CreationBinding + "x" }, id with { CreationEventHash = "sha256:" + new string('f', 64) },
                    id with { CycleId = "sha256:" + new string('f', 64) }, id with { ExpectedPriorVersion = 0 }, id with { ExpectedPriorVersion = id.ExpectedPriorVersion - 1 },
                    id with { ExpectedPriorVersion = id.ExpectedPriorVersion + 1 }, id with { ExpectedPriorVersion = long.MaxValue }, id with { ExpectedPositionId = "wrong-position" } })
                    foreach (var history in new[] { prior, terminal })
                        Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, history, input with { Command = input.Command with { Identity = changed } }));
                foreach (var kind in new[] { "decline-reaction-window", "close-reaction-window-no-eligible-reactor", "complete-reaction-participant", "unknown", input == close ? "resolve-breakdown-stop" : close.Command.Kind })
                    Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, terminal, input with { Command = input.Command with { Kind = kind } }));
                var authoritative = input == close ? state.ReactionWindow!.Trigger.WindowId.Value : ((CampaignBreakdownFlow.ReactorStopClosed)first.State.BreakdownFlow).Stop.StopId;
                foreach (var history in new[] { prior, terminal })
                    Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, history, input with { Command = input.Command with { Handle = authoritative } }));
                foreach (var bytes in Mutations(CampaignCombatReactionFallbackCodec.SerializeInput(input)))
                    Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, terminal, CampaignCombatReactionFallbackCodec.DeserializeInput(bytes)));
            }
            foreach (var history in new byte[][][] { [second.EventBytes], [second.EventBytes, first.EventBytes], [first.EventBytes, first.EventBytes], [.. terminal, second.EventBytes] })
                Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, history));
            var other = Apply(p, moves, trigger, [], inputs.Single(i => i != close));
            Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [other.EventBytes, second.EventBytes]));
            Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [first.EventBytes, other.EventBytes]));
        }
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void EveryEventAndCacheLeafRejectsRawAndResigned(string choice)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves); var state = Replay(p, moves, trigger, []);
        foreach (var kind in Kinds)
        {
            var events = new List<byte[]>(); var current = state;
            foreach (var step in new[] { kind, "resolve-breakdown-stop" })
            {
                var result = Apply(p, moves, trigger, events, CampaignCombatReactionFallback.Command(current, step));
                foreach (var bytes in Mutations(result.EventBytes)) Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [.. events, bytes]));
                var original = JsonNode.Parse(result.EventBytes)!;
                foreach (var path in Leaves(original, []))
                {
                    var changed = original.DeepClone(); ChangeLeaf(changed, path);
                    if (path[0] != "receiptId") changed["receiptId"] = Receipt(changed);
                    var bytes = Utf8(changed); Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [.. events, bytes]));
                    var cache = JsonNode.Parse(CampaignCombatReactionFallbackCodec.SerializeState(result.State))!;
                    cache["prefix"] = Prefix(current.Prefix, bytes); cache["receipts"]!.AsArray()[^1]!["eventHash"] = Digest(bytes);
                    cache["receipts"]!.AsArray()[^1]!["receiptId"] = changed["receiptId"]!.DeepClone();
                    Assert.ThrowsAny<JsonException>(() => Read(Utf8(cache), p, moves, trigger, [.. events, bytes]));
                }
                foreach (var bytes in Mutations(CampaignCombatReactionFallbackCodec.SerializeState(current)))
                    Assert.ThrowsAny<JsonException>(() => Read(bytes, p, moves, trigger, events));
                events.Add(result.EventBytes); current = result.State;
            }
            foreach (var bytes in Mutations(CampaignCombatReactionFallbackCodec.SerializeState(current)))
                Assert.ThrowsAny<JsonException>(() => Read(bytes, p, moves, trigger, events));
        }
    }
    [Fact]
    public void MissingCompletedForeignAndAlteredPredecessorHistoryRejects()
    {
        var p = Ready(); var moves = FirstMove(p); var trigger = Trigger(p, moves); var participants = Participant(p, moves, trigger);
        var state = Replay(p, moves, trigger, []); var input = CampaignCombatReactionFallback.Command(state, Kinds[0]); var result = Apply(p, moves, trigger, [], input);
        var completed = CampaignCombatReactionLifecycle.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, participants, CampaignCombatReactionLifecycle.Command(state.Predecessor));
        foreach (var invalid in new byte[][][] { [], [participants[0], participants[0]], [completed.EventBytes], [participants[0], completed.EventBytes], [result.EventBytes] })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionFallback.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, invalid, []));
        var foreign = Ready(1, "act-last"); var foreignMoves = FirstMove(foreign, "act-last"); var foreignTrigger = Trigger(foreign, foreignMoves);
        Assert.ThrowsAny<JsonException>(() => Apply(foreign, foreignMoves, foreignTrigger, [], input));
        Assert.ThrowsAny<JsonException>(() => Replay(foreign, foreignMoves, foreignTrigger, [result.EventBytes]));
        Assert.ThrowsAny<JsonException>(() => Replay(p, [], trigger, [])); Assert.ThrowsAny<JsonException>(() => Replay(p, moves, [], []));
        Assert.ThrowsAny<JsonException>(() => Replay(Ready(1, "act-first", true), moves, trigger, []));
        foreach (var buffer in p.Opening.Concat(p.Stage).Concat(p.Reserve).Concat(moves).Concat(trigger).Concat(participants).Append(p.Created).Append(p.Weather))
        {
            var saved = buffer[0]; buffer[0] = (byte)' ';
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionFallback.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, participants, [result.EventBytes]));
            buffer[0] = saved;
        }
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionFallback.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, Participant(foreign, foreignMoves, foreignTrigger), []));
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
        var forged = new CampaignCombatReactionFallbackState(forgedPredecessor, state.StateVersion, state.Prefix, state.BreakdownFlow, state.Receipts, []);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionFallbackCodec.SerializeState(forged));
        var input = CampaignCombatReactionFallback.Command(state, Kinds[0]); var first = Apply(p, moves, trigger, [], input);
        var second = Apply(p, moves, trigger, [first.EventBytes], CampaignCombatReactionFallback.Command(first.State, "resolve-breakdown-stop"));
        var expected = CampaignCombatReactionFallbackCodec.SerializeState(second.State); var events = new[] { first.EventBytes, second.EventBytes };
        var eventCopies = events.Select(e => e.ToArray()).ToArray(); var participants = Participant(p, moves, trigger);
        var replay = CampaignCombatReactionFallback.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, participants, events);
        var inputBytes = CampaignCombatReactionFallbackCodec.SerializeInput(input); var detached = CampaignCombatReactionFallbackCodec.DeserializeInput(inputBytes);
        Array.Fill(inputBytes, (byte)0); Assert.Equal(input, detached);
        foreach (var buffer in p.Opening.Concat(p.Stage).Concat(p.Reserve).Concat(moves).Concat(trigger).Concat(participants).Concat(events).Append(p.Created).Append(p.Weather)) Array.Fill(buffer, (byte)0);
        Assert.Equal(expected, CampaignCombatReactionFallbackCodec.SerializeState(replay)); Assert.Equal(expected, CampaignCombatReactionFallbackCodec.SerializeState(second.State));
        for (var i = 0; i < 2; i++) Assert.Equal(eventCopies[i], CampaignCombatReactionFallbackCodec.SerializeEvent(replay.Events[i]));
    }
    [Fact]
    public void MalformedBytesDepthItemsAndAuthorityCapacityReject()
    {
        var p = Ready(); var moves = FirstMove(p); var trigger = Trigger(p, moves); var state = Replay(p, moves, trigger, []);
        var input = CampaignCombatReactionFallback.Command(state, Kinds[0]); var result = Apply(p, moves, trigger, [], input);
        var excessive = JsonNode.Parse(result.EventBytes)!;
        excessive["closedOpportunityIds"] = new JsonArray(Enumerable.Range(0, 513).Select(_ => (JsonNode?)JsonValue.Create("sha256:" + new string('f', 64))).ToArray());
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes("null"), Encoding.UTF8.GetBytes("{}"), new byte[] { 0xff },
            Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)), Utf8(excessive) })
        {
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionFallbackCodec.DeserializeInput(bytes));
            Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [bytes]));
            Assert.ThrowsAny<JsonException>(() => Read(bytes, p, moves, trigger, []));
        }
        Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [null!])); Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionFallbackCodec.SerializeInput(null!));
        foreach (var invalid in new[] { new CampaignCombatReactionFallbackState(state.Predecessor, long.MaxValue, state.Prefix, state.BreakdownFlow, state.Receipts, []),
            new CampaignCombatReactionFallbackState(state.Predecessor, state.StateVersion, state.Prefix, state.BreakdownFlow, Enumerable.Repeat(state.Receipts[0], 512), []) })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionFallback.Command(invalid, Kinds[0]));
        var node = JsonNode.Parse(CampaignCombatReactionFallbackCodec.SerializeInput(input))!; node["command"]!["reason"] = "timeout";
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionFallbackCodec.DeserializeInput(Utf8(node)));
        foreach (var edit in new Action<JsonNode>[] { n => n["reactionWindow"]!["frozenOpportunities"] = new JsonArray(),
            n => n["reactionWindow"]!["frozenOpportunities"]!.AsArray().Add(n["reactionWindow"]!["frozenOpportunities"]![0]!.DeepClone()),
            n => n["reactionWindow"]!["activeOpportunityId"] = null, n => n["breakdownFlow"]!["reactorRoute"] = null })
        { var cache = JsonNode.Parse(CampaignCombatReactionFallbackCodec.SerializeState(state))!; edit(cache); Assert.ThrowsAny<JsonException>(() => Read(Utf8(cache), p, moves, trigger, [])); }
    }
    private static readonly string[] Kinds = ["close-reaction-window-scripted-unavailable", "close-reaction-window-timeout"];
    private static byte[][] Participant(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger)
    {
        var state = CampaignCombatReactionLifecycle.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, []);
        return [CampaignCombatReactionLifecycle.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, [], CampaignCombatReactionLifecycle.Command(state)).EventBytes];
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
    private static CampaignCombatReactionFallbackState Replay(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events) => CampaignCombatReactionFallback.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, Participant(p, moves, trigger), events);
    private static CampaignCombatReactionFallbackResult Apply(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events, CampaignCombatReactionFallbackInput input) => CampaignCombatReactionFallback.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, Participant(p, moves, trigger), events, input);
    private static CampaignCombatReactionFallbackState Read(byte[] bytes, Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events) => CampaignCombatReactionFallback.ReadState(bytes, p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, Participant(p, moves, trigger), events);
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
        "Campaigns", "Fixtures", "combat-inherited-reaction-active-fallback-v1.json")));
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
        var resolve = value["eventType"]!.GetValue<string>() == "breakdown-stop-resolved";
        return (resolve ? "iml." : "irl.") + Digest(Encoding.UTF8.GetBytes((resolve ? "sandtable.combat.inherited-breakdown-stop-resolved-receipt.v2\0" : "sandtable.combat.inherited-reaction-window-closed-receipt.v3\0") + unsigned.ToJsonString()))[7..];
    }
}
