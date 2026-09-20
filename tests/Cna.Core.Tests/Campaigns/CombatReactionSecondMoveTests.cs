using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatReactionSecondMoveTests
{
    [Theory]
    [InlineData("act-first", "axis")]
    [InlineData("act-last", "commonwealth")]
    public void BothOwnersReproduceTenFrozenArtifactsAndFourCuts(string choice, string side)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        using var fixture = Fixture(); Assert.Equal(2, fixture.RootElement.GetProperty("cases").GetArrayLength());
        var item = Assert.Single(fixture.RootElement.GetProperty("cases").EnumerateArray(), c => c.GetProperty("name").GetString() == side + "-active-second-move");
        var goldens = item.GetProperty("goldens"); Assert.Equal(5, goldens.EnumerateObject().Count());
        var state = Replay(p, moves, trigger, []);
        CheckGolden(goldens, "participant", Participant(p, moves, trigger)[0]);
        var initial = CampaignCombatReactionSecondMoveCodec.SerializeState(state); CheckGolden(goldens, "state-0", initial);
        Assert.Equal(CampaignCombatReactionLifecycleCodec.SerializeState(state.Predecessor), initial);
        Assert.Equal(initial, CampaignCombatReactionSecondMoveCodec.SerializeState(Read(initial, p, moves, trigger, [])));
        var input = CampaignCombatReactionSecondMove.Command(state);
        CheckGolden(goldens, "input", CampaignCombatReactionSecondMoveCodec.SerializeInput(input));
        var result = Apply(p, moves, trigger, [], input); Assert.False(result.Duplicate);
        CheckGolden(goldens, "event", result.EventBytes);
        var terminal = CampaignCombatReactionSecondMoveCodec.SerializeState(result.State); CheckGolden(goldens, "state-1", terminal);
        Assert.Equal(terminal, CampaignCombatReactionSecondMoveCodec.SerializeState(Read(terminal, p, moves, trigger, [result.EventBytes])));
        Assert.Equal(terminal, CampaignCombatReactionSecondMoveCodec.SerializeState(Replay(p, moves, trigger, [result.EventBytes])));
        var retry = Apply(p, moves, trigger, [result.EventBytes], input); Assert.True(retry.Duplicate);
        Assert.Equal(result.EventBytes, retry.EventBytes); Assert.Equal(terminal, CampaignCombatReactionSecondMoveCodec.SerializeState(retry.State));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionSecondMove.Command(result.State));
    }
    [Theory]
    [InlineData("act-first", "commonwealth", "assault-east")]
    [InlineData("act-last", "axis", "assault-west")]
    public void MovePreservesOpenRouteAndAllUnchangedAuthority(string choice, string reactorSide, string assault)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        var before = Replay(p, moves, trigger, []); var input = CampaignCombatReactionSecondMove.Command(before);
        var result = Apply(p, moves, trigger, [], input); var after = result.State;
        var b = JsonNode.Parse(CampaignCombatReactionSecondMoveCodec.SerializeState(before))!;
        var a = JsonNode.Parse(CampaignCombatReactionSecondMoveCodec.SerializeState(after))!;
        var ev = JsonNode.Parse(result.EventBytes)!;
        Assert.Equal(14, before.StateVersion); Assert.Equal(15, after.StateVersion);
        foreach (var field in b.AsObject().Select(x => x.Key).Except(["stateVersion", "prefix", "receipts", "world", "tracks", "actualProgressRefs", "breakdownFlow"]))
            Assert.True(JsonNode.DeepEquals(b[field], a[field]), field);
        var flow = Assert.IsType<CampaignBreakdownFlow.Reacting>(after.BreakdownFlow);
        var priorFlow = Assert.IsType<CampaignBreakdownFlow.Reacting>(before.BreakdownFlow);
        Assert.Equal(priorFlow.PhasingContinuation, flow.PhasingContinuation);
        Assert.Equal(priorFlow.ReactorRoute!.AtLocation(reactorSide + "-supply"), flow.ReactorRoute);
        Assert.Equal(14, flow.ReactorRoute!.FirstMoveStateVersion); Assert.Equal(assault, flow.ReactorRoute.OriginLocationId); Assert.Empty(flow.ReactorRoute.CohortIds);
        var reactor = after.World.Elements.Single(e => e.ElementId == flow.ReactorRoute.ElementId);
        Assert.Equal(reactorSide + "-supply", reactor.CurrentLocationId); Assert.Equal(new CapabilityPointAmount(4, 1), reactor.OperationalState.CapabilityPointsExpended);
        Assert.Equal(0, reactor.OperationalState.CohesionLevel);
        Assert.Equal(reactor.CurrentLocationId, after.World.Representations.Single(r => r.RepresentationId == flow.ReactorRoute.RepresentationId).CurrentLocationId);
        Assert.Equal(before.World.Elements.Single(e => e.ElementId != reactor.ElementId), after.World.Elements.Single(e => e.ElementId != reactor.ElementId));
        Assert.Equal(before.Tracks.Count, after.Tracks.Count);
        Assert.Equal(new[] { assault, reactorSide + "-rear", reactorSide + "-supply" }, after.Tracks.Single(t => t.Unit.ElementId == reactor.ElementId).Route);
        Assert.Equal(before.ActualProgressRefs, after.ActualProgressRefs.Take(before.ActualProgressRefs.Count)); Assert.Equal(before.ActualProgressRefs.Count + 1, after.ActualProgressRefs.Count);
        Assert.Equal(before.Receipts, after.Receipts.Take(before.Receipts.Count)); Assert.Equal(before.Receipts.Count + 1, after.Receipts.Count);
        Assert.Equal(Receipt(ev), ev["receiptId"]!.GetValue<string>()); Assert.Equal(Prefix(before.Prefix, result.EventBytes), after.Prefix);
        Assert.Equal(Digest(result.EventBytes), after.Receipts[^1].EventHash);
        Assert.Equal(Digest(CampaignCombatReactionSecondMoveCodec.SerializeInput(input)), after.Receipts[^1].CommandHash);
        Assert.ThrowsAny<JsonException>(() => CampaignEventSerializer.Deserialize(result.EventBytes));
        Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(CampaignCombatReactionSecondMoveCodec.SerializeState(after), p.Created, p.Request));
    }
    [Theory]
    [InlineData("act-first", "commonwealth")]
    [InlineData("act-last", "axis")]
    public void PublicWindowOpportunityAndActionUseIndependentOrderedPreimages(string choice, string side)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        var state = Replay(p, moves, trigger, []); var input = CampaignCombatReactionSecondMove.Command(state);
        var b = JsonNode.Parse(CampaignCombatReactionSecondMoveCodec.SerializeState(state))!;
        var window = Digest(Utf8(new JsonObject
        {
            ["domain"] = "sandtable.observation.reaction-window.v1",
            ["campaignId"] = b["campaignId"]!.DeepClone(),
            ["rulesetHash"] = b["rulesetHash"]!.DeepClone(),
            ["committedStateVersion"] = 13,
            ["reactingSide"] = side
        }));
        var cost = JsonNode.Parse("""{"destinationTerrainId":"land.terrain.clear","destinationTerrainCost":{"numerator":2,"denominator":1},"routeAdjustment":null,"crossedHexsideCosts":[],"totalCost":{"numerator":2,"denominator":1}}""")!;
        var option = new JsonObject { ["originLocationId"] = side + "-rear", ["destinationLocationId"] = side + "-supply", ["costBreakdown"] = cost.DeepClone() };
        var capability = Digest(Utf8(new JsonObject { ["domain"] = "sandtable.observation.reaction-capability.v1", ["moveOptions"] = new JsonArray(option) }));
        var opportunity = Digest(Utf8(new JsonObject { ["domain"] = "sandtable.observation.reaction-opportunity.v2", ["windowId"] = window, ["stateVersion"] = 14, ["capabilityKey"] = capability }));
        var action = new JsonObject
        {
            ["contractVersion"] = 1,
            ["kind"] = "move-reacting-element",
            ["windowId"] = window,
            ["opportunityId"] = opportunity,
            ["originLocationId"] = side + "-rear",
            ["destinationLocationId"] = side + "-supply",
            ["costBreakdown"] = cost.DeepClone()
        };
        Assert.Equal(window, input.Command.WindowId); Assert.Equal(opportunity, input.Command.OpportunityId); Assert.Equal(Digest(Utf8(action)), input.Command.Identity.ActionId);
        Assert.NotEqual(state.ReactionWindow.Trigger.WindowId.Value, window); Assert.NotEqual(state.ReactionWindow.ActiveOpportunityId!.Value, opportunity);
        var first = Assert.IsType<CampaignCombatReactionLifecycleCommand.Move>(state.Predecessor.Events[0].Input.Command);
        Assert.Equal(first.WindowId, window); Assert.NotEqual(first.OpportunityId, opportunity);
        action["costBreakdown"]!["totalCost"]!["numerator"] = 3;
        Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, [], input with { Command = input.Command with { Identity = input.Command.Identity with { ActionId = Digest(Utf8(action)) } } }));
        var ev = JsonNode.Parse(Apply(p, moves, trigger, [], input).EventBytes)!;
        Assert.Equal(38, ev.AsObject().Count); Assert.Equal("reacting-element-moved", ev["eventType"]!.GetValue<string>());
        Assert.Equal(2, ev["capabilityPointsExpendedBefore"]!["numerator"]!.GetValue<int>()); Assert.Equal(4, ev["capabilityPointsExpendedAfter"]!["numerator"]!.GetValue<int>());
        Assert.Empty(ev["breakdownAccounting"]!.AsArray()); Assert.Equal("spi-1979-map-a", ev["mobilitySources"]![0]!["sourceId"]!.GetValue<string>());
        Assert.Equal("8.37", ev["mobilitySources"]![0]!["locator"]!.GetValue<string>());
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void EveryCommandFieldActorRetryAndWrongStepRejects(string choice)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves); var state = Replay(p, moves, trigger, []);
        var input = CampaignCombatReactionSecondMove.Command(state); var result = Apply(p, moves, trigger, [], input);
        var id = input.Command.Identity;
        foreach (var history in new byte[][][] { [], [result.EventBytes] })
        {
            foreach (var actor in Enum.GetValues<CampaignOpeningPreambleActor>().Where(a => a != input.Actor).Append((CampaignOpeningPreambleActor)99))
                Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, history, input with { Actor = actor }));
            foreach (var changed in new[] { id with { ContractVersion = 1 }, id with { ContractVersion = 3 }, id with { ActionId = FakeHash },
                id with { CreationBinding = id.CreationBinding + "x" }, id with { CreationEventHash = FakeHash }, id with { CycleId = FakeHash },
                id with { ExpectedPriorVersion = 0 }, id with { ExpectedPriorVersion = 13 }, id with { ExpectedPriorVersion = 15 },
                id with { ExpectedPriorVersion = long.MaxValue }, id with { ExpectedPositionId = "wrong-position" } })
                Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, history, input with { Command = input.Command with { Identity = changed } }));
            var first = Assert.IsType<CampaignCombatReactionLifecycleCommand.Move>(state.Predecessor.Events[0].Input.Command);
            foreach (var changed in new[] { input.Command with { WindowId = state.ReactionWindow.Trigger.WindowId.Value },
                input.Command with { OpportunityId = state.ReactionWindow.ActiveOpportunityId!.Value }, input.Command with { OpportunityId = first.OpportunityId },
                input.Command with { WindowId = FakeHash }, input.Command with { OpportunityId = FakeHash },
                input.Command with { OriginLocationId = first.OriginLocationId }, input.Command with { DestinationLocationId = input.Command.OriginLocationId }, first })
                Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, history, input with { Command = changed }));
            foreach (var bytes in Mutations(CampaignCombatReactionSecondMoveCodec.SerializeInput(input)))
                Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, history, CampaignCombatReactionSecondMoveCodec.DeserializeInput(bytes)));
        }
        var complete = CampaignCombatReactionLifecycle.Command(state.Predecessor);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionSecondMoveCodec.DeserializeInput(CampaignCombatReactionLifecycleCodec.SerializeInput(complete)));
        foreach (var history in new byte[][][] { [result.EventBytes, result.EventBytes], [Participant(p, moves, trigger)[0]], [trigger[0]], [moves[0]] })
            Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, history));
        Assert.ThrowsAny<JsonException>(() => Read(CampaignCombatReactionSecondMoveCodec.SerializeState(result.State), p, moves, trigger, []));
        Assert.ThrowsAny<JsonException>(() => Read(CampaignCombatReactionSecondMoveCodec.SerializeState(state), p, moves, trigger, [result.EventBytes]));
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void EveryEventAndCacheLeafRejectsRawAndResigned(string choice)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves); var state = Replay(p, moves, trigger, []);
        var input = CampaignCombatReactionSecondMove.Command(state); var result = Apply(p, moves, trigger, [], input);
        foreach (var bytes in Mutations(result.EventBytes)) Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [bytes]));
        var original = JsonNode.Parse(result.EventBytes)!;
        foreach (var path in Leaves(original, []))
        {
            var changed = original.DeepClone(); ChangeLeaf(changed, path);
            if (path[0] != "receiptId") changed["receiptId"] = Receipt(changed);
            var bytes = Utf8(changed); Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [bytes]));
            var cache = JsonNode.Parse(CampaignCombatReactionSecondMoveCodec.SerializeState(result.State))!;
            cache["prefix"] = Prefix(state.Prefix, bytes); cache["receipts"]!.AsArray()[^1]!["eventHash"] = Digest(bytes);
            cache["receipts"]!.AsArray()[^1]!["receiptId"] = changed["receiptId"]!.DeepClone();
            cache["actualProgressRefs"]!.AsArray()[^1]!["eventHash"] = Digest(bytes);
            cache["actualProgressRefs"]!.AsArray()[^1]!["receiptId"] = changed["receiptId"]!.DeepClone();
            Assert.ThrowsAny<JsonException>(() => Read(Utf8(cache), p, moves, trigger, [bytes]));
        }
        foreach (var cut in new[] { state, result.State })
            foreach (var bytes in Mutations(CampaignCombatReactionSecondMoveCodec.SerializeState(cut)))
                Assert.ThrowsAny<JsonException>(() => Read(bytes, p, moves, trigger, cut == state ? [] : [result.EventBytes]));
    }
    [Fact]
    public void WholeWorldForgeryRejectsBeforeAndAfterMoveAndAllBuffersDetach()
    {
        var p = Ready(); var moves = FirstMove(p); var trigger = Trigger(p, moves); var state = Replay(p, moves, trigger, []);
        var input = CampaignCombatReactionSecondMove.Command(state); var result = Apply(p, moves, trigger, [], input);
        foreach (var cut in new[] { state, result.State })
        {
            var world = cut.World; var element = world.Elements[0]; var ledger = element.OperationalState;
            foreach (var forgedLedger in new[] {
                new CampaignElementOperationalStateV6(1, 1, new CapabilityPointAmount(7, 1), ledger.CohesionLevel,
                    ledger.VehicleBreakdownState, ledger.MovementEnded, ledger.InitialLedgerOrigin),
                new CampaignElementOperationalStateV6(1, 1, ledger.CapabilityPointsExpended, ledger.CohesionLevel,
                    new CampaignVehicleBreakdownState("forged-cohort", BreakdownPointAmount.Zero, BreakdownPointAmount.Zero, null, 1, 0),
                    ledger.MovementEnded, ledger.InitialLedgerOrigin) })
            {
                var changed = new CampaignElementStateV6(element.ElementId, element.CurrentLocationId, element.ReserveStatus,
                    forgedLedger, element.Components,
                    element.SourceParentFormationId, element.CurrentParentFormationId, element.Ammunition, element.Readiness);
                var altered = new CampaignWorldSnapshotV7(7, world.CreationBinding, world.Elements.Select(e => e == element ? changed : e),
                    world.Representations, world.BrokenVehicleLots, world.CohesionCauses, world.Relationships, world.CustodyLots,
                    world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
                Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionSecondMoveCodec.SerializeState(Copy(cut, world: altered)));
                var predecessor = cut.Predecessor;
                var forged = new CampaignCombatReactionLifecycleState(predecessor.Trigger, predecessor.StateVersion, predecessor.Prefix, altered,
                    predecessor.ReactionWindow, predecessor.BreakdownFlow, predecessor.Receipts, predecessor.Tracks, predecessor.ActualProgressRefs, predecessor.Events);
                Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionSecondMoveCodec.SerializeState(Copy(cut, predecessor: forged)));
            }
        }
        var expected = CampaignCombatReactionSecondMoveCodec.SerializeState(result.State); var eventCopy = result.EventBytes.ToArray(); var participants = Participant(p, moves, trigger);
        var replay = CampaignCombatReactionSecondMove.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, participants, [result.EventBytes]);
        var inputBytes = CampaignCombatReactionSecondMoveCodec.SerializeInput(input); var detached = CampaignCombatReactionSecondMoveCodec.DeserializeInput(inputBytes);
        Array.Fill(inputBytes, (byte)0); Assert.Equal(input, detached);
        foreach (var buffer in p.Opening.Concat(p.Stage).Concat(p.Reserve).Concat(moves).Concat(trigger).Concat(participants).Append(result.EventBytes).Append(p.Created).Append(p.Weather)) Array.Fill(buffer, (byte)0);
        Assert.Equal(expected, CampaignCombatReactionSecondMoveCodec.SerializeState(replay)); Assert.Equal(expected, CampaignCombatReactionSecondMoveCodec.SerializeState(result.State));
        Assert.Equal(eventCopy, CampaignCombatReactionSecondMoveCodec.SerializeEvent(Assert.Single(replay.Events)));
    }
    [Fact]
    public void MalformedBytesDepthItemsAndAuthorityCapacityReject()
    {
        var p = Ready(); var moves = FirstMove(p); var trigger = Trigger(p, moves); var state = Replay(p, moves, trigger, []);
        var input = CampaignCombatReactionSecondMove.Command(state); var result = Apply(p, moves, trigger, [], input);
        var excessive = JsonNode.Parse(result.EventBytes)!; excessive["breakdownAccounting"] = new JsonArray(Enumerable.Range(0, 513).Select(_ => (JsonNode?)JsonValue.Create(0)).ToArray());
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes("null"), Encoding.UTF8.GetBytes("{}"), new byte[] { 0xff },
            Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)), Utf8(excessive) })
        {
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionSecondMoveCodec.DeserializeInput(bytes));
            Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [bytes])); Assert.ThrowsAny<JsonException>(() => Read(bytes, p, moves, trigger, []));
        }
        Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [null!])); Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionSecondMoveCodec.SerializeInput(null!));
        foreach (var invalid in new[] { Copy(state, version: long.MaxValue), Copy(state, receipts: Enumerable.Repeat(state.Receipts[0], 512)),
            Copy(state, progress: Enumerable.Repeat(state.ActualProgressRefs[0], 512)), Copy(state, tracks: Enumerable.Repeat(state.Tracks[0], 513)),
            Copy(state, tracks: [new(state.Tracks[0].Unit, Enumerable.Repeat("axis-rear", 512))]) })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionSecondMove.Command(invalid));
        foreach (var edit in new Action<JsonNode>[] { n => n["reactionWindow"]!["frozenOpportunities"] = new JsonArray(),
            n => n["reactionWindow"]!["frozenOpportunities"]!.AsArray().Add(n["reactionWindow"]!["frozenOpportunities"]![0]!.DeepClone()),
            n => n["reactionWindow"]!["activeOpportunityId"] = null, n => n["breakdownFlow"]!["reactorRoute"] = null })
        { var cache = JsonNode.Parse(CampaignCombatReactionSecondMoveCodec.SerializeState(state))!; edit(cache); Assert.ThrowsAny<JsonException>(() => Read(Utf8(cache), p, moves, trigger, [])); }
        var node = JsonNode.Parse(CampaignCombatReactionSecondMoveCodec.SerializeInput(input))!; node["command"]!["expectedPriorVersion"] = true;
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionSecondMoveCodec.DeserializeInput(Utf8(node)));
    }
    private static string FakeHash => "sha256:" + new string('f', 64);
    private static CampaignCombatReactionSecondMoveState Copy(CampaignCombatReactionSecondMoveState s, long? version = null,
        CampaignWorldSnapshotV7? world = null, CampaignCombatReactionLifecycleState? predecessor = null,
        IEnumerable<CampaignOpeningPreambleReceipt>? receipts = null, IEnumerable<CampaignCombatInheritedTrack>? tracks = null,
        IEnumerable<CampaignCombatInheritedProgress>? progress = null) =>
        new(predecessor ?? s.Predecessor, version ?? s.StateVersion, s.Prefix, world ?? s.World, s.BreakdownFlow, receipts ?? s.Receipts, tracks ?? s.Tracks, progress ?? s.ActualProgressRefs, s.Events);
    [Fact]
    public void MissingCompletedForeignAndAlteredPredecessorHistoryRejects()
    {
        var p = Ready(); var moves = FirstMove(p); var trigger = Trigger(p, moves); var participants = Participant(p, moves, trigger);
        var state = Replay(p, moves, trigger, []); var input = CampaignCombatReactionSecondMove.Command(state); var result = Apply(p, moves, trigger, [], input);
        var completed = CampaignCombatReactionLifecycle.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, participants, CampaignCombatReactionLifecycle.Command(state.Predecessor));
        foreach (var invalid in new byte[][][] { [], [participants[0], participants[0]], [completed.EventBytes], [participants[0], completed.EventBytes], [result.EventBytes] })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionSecondMove.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, invalid, []));
        var foreign = Ready(1, "act-last"); var foreignMoves = FirstMove(foreign, "act-last"); var foreignTrigger = Trigger(foreign, foreignMoves);
        Assert.ThrowsAny<JsonException>(() => Apply(foreign, foreignMoves, foreignTrigger, [], input));
        Assert.ThrowsAny<JsonException>(() => Replay(foreign, foreignMoves, foreignTrigger, [result.EventBytes]));
        Assert.ThrowsAny<JsonException>(() => Replay(p, [], trigger, [])); Assert.ThrowsAny<JsonException>(() => Replay(p, moves, [], []));
        Assert.ThrowsAny<JsonException>(() => Replay(Ready(1, "act-first", true), moves, trigger, []));
        foreach (var buffer in p.Opening.Concat(p.Stage).Concat(p.Reserve).Concat(moves).Concat(trigger).Concat(participants).Append(p.Created).Append(p.Weather))
        {
            var saved = buffer[0]; buffer[0] = (byte)' ';
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionSecondMove.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, participants, [result.EventBytes]));
            buffer[0] = saved;
        }
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionSecondMove.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, Participant(foreign, foreignMoves, foreignTrigger), []));
    }
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
    private static CampaignCombatReactionSecondMoveState Replay(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events) => CampaignCombatReactionSecondMove.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, Participant(p, moves, trigger), events);
    private static CampaignCombatReactionSecondMoveResult Apply(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events, CampaignCombatReactionSecondMoveInput input) => CampaignCombatReactionSecondMove.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, Participant(p, moves, trigger), events, input);
    private static CampaignCombatReactionSecondMoveState Read(byte[] bytes, Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events) => CampaignCombatReactionSecondMove.ReadState(bytes, p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, Participant(p, moves, trigger), events);
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
        "Campaigns", "Fixtures", "combat-inherited-reaction-second-move-v1.json")));
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
        return "irl." + Digest(Encoding.UTF8.GetBytes("sandtable.combat.inherited-reacting-element-moved-receipt.v3\0" + unsigned.ToJsonString()))[7..];
    }
}
