using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatReactionCompletionTests
{
    [Theory]
    [InlineData("act-first", "axis")]
    [InlineData("act-last", "commonwealth")]
    public void BothOwnersReproduceTwentyTwoFrozenArtifactsAndEightCuts(string choice, string side)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        using var fixture = Fixture(); Assert.Equal(2, fixture.RootElement.GetProperty("cases").GetArrayLength());
        var item = Assert.Single(fixture.RootElement.GetProperty("cases").EnumerateArray(), c => c.GetProperty("name").GetString() == side + "-reaction-movement-completion");
        var goldens = item.GetProperty("goldens"); Assert.Equal(11, goldens.EnumerateObject().Count());
        CheckGolden(goldens, "second-move", Second(p, moves, trigger)[0]);
        var state = Replay(p, moves, trigger, []); var initial = state; var events = new List<byte[]>(); var inputs = new List<CampaignCombatReactionLifecycleInput>();
        Assert.Equal(CampaignCombatReactionSecondMoveCodec.SerializeState(state.Predecessor), CampaignCombatReactionCompletionCodec.SerializeState(state));
        for (var cut = 0; cut <= 3; cut++)
        {
            var bytes = CampaignCombatReactionCompletionCodec.SerializeState(state); CheckGolden(goldens, $"state-{cut}", bytes);
            Assert.Equal(bytes, CampaignCombatReactionCompletionCodec.SerializeState(Read(bytes, p, moves, trigger, events)));
            Assert.Equal(bytes, CampaignCombatReactionCompletionCodec.SerializeState(Replay(p, moves, trigger, events)));
            var original = JsonNode.Parse(CampaignCombatReactionCompletionCodec.SerializeState(initial))!; var current = JsonNode.Parse(bytes)!;
            foreach (var property in new[] { "campaignId", "rulesetHash", "configurationHash", "creationBinding", "creationEventHash", "sequencePosition", "initiativeHolder", "operationStageOrders", "operationStageWeather", "world", "randomState", "firstActingSide", "members", "cycle", "cycleId", "openingBaseHash", "completionReceiptId", "tracks", "actualProgressRefs" })
                Assert.True(JsonNode.DeepEquals(original[property], current[property]), property);
            Assert.Equal(15 + cut, state.StateVersion); Assert.Equal(initial.World, state.World);
            Assert.Equal(initial.ActualProgressRefs, state.ActualProgressRefs); Assert.Equal(initial.Tracks.Count, state.Tracks.Count);
            foreach (var track in initial.Tracks) Assert.Equal(track.Route, state.Tracks.Single(t => t.Unit == track.Unit).Route);
            Assert.Equal(initial.Receipts, state.Receipts.Take(initial.Receipts.Count)); Assert.Equal(initial.Receipts.Count + cut, state.Receipts.Count);
            if (cut == 3) break;
            var input = CampaignCombatReactionCompletion.Command(state); inputs.Add(input);
            var inputBytes = CampaignCombatReactionCompletionCodec.SerializeInput(input); CheckGolden(goldens, $"input-{cut + 1}", inputBytes);
            Assert.Equal(input, CampaignCombatReactionCompletionCodec.DeserializeInput(inputBytes));
            var result = Apply(p, moves, trigger, events, input); Assert.False(result.Duplicate);
            CheckGolden(goldens, $"event-{cut + 1}", result.EventBytes);
            Assert.ThrowsAny<JsonException>(() => CampaignEventSerializer.Deserialize(result.EventBytes));
            var ev = JsonNode.Parse(result.EventBytes)!;
            Assert.Equal(cut switch { 0 => "reaction-participant-completed", 1 => "breakdown-stop-resolved", _ => "reaction-window-closed" }, ev["eventType"]!.GetValue<string>());
            Assert.Equal(cut == 1 ? 2 : 3, ev["contractVersion"]!.GetValue<int>());
            Assert.Equal(Receipt(ev), ev["receiptId"]!.GetValue<string>()); Assert.Equal(Prefix(state.Prefix, result.EventBytes), result.State.Prefix);
            Assert.Equal(Digest(inputBytes), result.State.Receipts[^1].CommandHash); Assert.Equal(Digest(result.EventBytes), result.State.Receipts[^1].EventHash);
            if (cut == 0)
            {
                var route = Assert.IsType<CampaignBreakdownFlow.Reacting>(state.BreakdownFlow).ReactorRoute!;
                var stop = Assert.IsType<CampaignBreakdownFlow.ReactorStopOpen>(result.State.BreakdownFlow).Stop;
                Assert.Equal(route, stop.Route); Assert.Equal(14, route.FirstMoveStateVersion); Assert.Empty(route.CohortIds);
                Assert.Equal(16, stop.RecordedStateVersion); Assert.Equal(CampaignBreakdownStopReason.ReactionCompleted, stop.Reason);
                Assert.Equal(BreakdownWeatherKind.Normal, stop.WeatherKind); Assert.Empty(stop.CohortInputs);
                Assert.Null(result.State.ReactionWindow!.ActiveOpportunityId);
                Assert.Equal(state.ReactionWindow!.ActiveOpportunityId, Assert.Single(result.State.ReactionWindow.ResolvedOpportunityIds));
            }
            else if (cut == 1)
            {
                Assert.NotNull(result.State.ReactionWindow); Assert.Null(Assert.IsType<CampaignBreakdownFlow.Reacting>(result.State.BreakdownFlow).ReactorRoute);
                Assert.Empty(ev["checks"]!.AsArray()); Assert.Empty(ev["createdLots"]!.AsArray()); Assert.Null(ev["interruptContextAfter"]);
                Assert.True(JsonNode.DeepEquals(ev["randomStateBefore"], ev["randomStateAfter"]));
            }
            else
            {
                Assert.Null(result.State.ReactionWindow); Assert.Null(ev["actingSide"]); Assert.Empty(ev["closedOpportunityIds"]!.AsArray());
                var originalRoute = Assert.IsType<CampaignPhasingContinuation.ResumeRoute>(Assert.IsType<CampaignBreakdownFlow.Reacting>(initial.BreakdownFlow).PhasingContinuation).Route;
                Assert.Equal(originalRoute, Assert.IsType<CampaignBreakdownFlow.Moving>(result.State.BreakdownFlow).Route);
            }
            events.Add(result.EventBytes); state = result.State;
        }
        for (var i = 0; i < 3; i++)
        {
            var retry = Apply(p, moves, trigger, events, inputs[i]); Assert.True(retry.Duplicate); Assert.Equal(events[i], retry.EventBytes);
            Assert.Equal(CampaignCombatReactionCompletionCodec.SerializeState(state), CampaignCombatReactionCompletionCodec.SerializeState(retry.State));
        }
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionCompletion.Command(state));
        Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(CampaignCombatReactionCompletionCodec.SerializeState(state), p.Created, p.Request));
        var reactor = initial.World.Elements.Single(e => e.ElementId == Assert.IsType<CampaignBreakdownFlow.Reacting>(initial.BreakdownFlow).ReactorRoute!.ElementId);
        var reactingSide = choice == "act-first" ? "commonwealth" : "axis";
        Assert.Equal(reactingSide + "-supply", reactor.CurrentLocationId); Assert.Equal(new CapabilityPointAmount(4, 1), reactor.OperationalState.CapabilityPointsExpended);
        Assert.All(state.World.Elements, e => Assert.Equal(new CapabilityPointAmount(4, 1), e.OperationalState.CapabilityPointsExpended));
        Assert.Equal(new[] { reactingSide == "axis" ? "assault-west" : "assault-east", reactingSide + "-rear", reactingSide + "-supply" }, initial.Tracks.Single(t => t.Unit.ElementId == reactor.ElementId).Route);
    }
    [Theory]
    [InlineData("act-first", "commonwealth")]
    [InlineData("act-last", "axis")]
    public void EmptyOpportunityStopAndActionsUseIndependentPreimages(string choice, string side)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves); var state = Replay(p, moves, trigger, []);
        var b = JsonNode.Parse(CampaignCombatReactionCompletionCodec.SerializeState(state))!;
        var window = Digest(Utf8(new JsonObject
        {
            ["domain"] = "sandtable.observation.reaction-window.v1",
            ["campaignId"] = b["campaignId"]!.DeepClone(),
            ["rulesetHash"] = b["rulesetHash"]!.DeepClone(),
            ["committedStateVersion"] = 13,
            ["reactingSide"] = side
        }));
        var capability = Digest(Utf8(new JsonObject { ["domain"] = "sandtable.observation.reaction-capability.v1", ["moveOptions"] = new JsonArray() }));
        var opportunity = Digest(Utf8(new JsonObject { ["domain"] = "sandtable.observation.reaction-opportunity.v2", ["windowId"] = window, ["stateVersion"] = 15, ["capabilityKey"] = capability }));
        var complete = CampaignCombatReactionCompletion.Command(state); var command = Assert.IsType<CampaignCombatReactionLifecycleCommand.Complete>(complete.Command);
        Assert.Equal(window, command.WindowId); Assert.Equal(opportunity, command.OpportunityId);
        Assert.Equal(Digest(Utf8(new JsonObject { ["contractVersion"] = 1, ["kind"] = "complete-reaction-participant", ["windowId"] = window, ["opportunityId"] = opportunity })), command.Identity.ActionId);
        Assert.NotEqual(window, state.ReactionWindow!.Trigger.WindowId.Value); Assert.NotEqual(opportunity, state.ReactionWindow.ActiveOpportunityId!.Value);
        var first = Assert.IsType<CampaignCombatReactionLifecycleCommand.Move>(state.Predecessor.Predecessor.Events[0].Input.Command);
        var second = state.Predecessor.Events[0].Input.Command;
        Assert.Equal(window, first.WindowId); Assert.Equal(window, second.WindowId);
        Assert.NotEqual(opportunity, first.OpportunityId); Assert.NotEqual(opportunity, second.OpportunityId);
        var completed = Apply(p, moves, trigger, [], complete);
        var stopPreimage = new JsonObject
        {
            ["domain"] = "sandtable.breakdown.stop.v1",
            ["campaignId"] = b["campaignId"]!.DeepClone(),
            ["rulesetHash"] = b["rulesetHash"]!.DeepClone(),
            ["recordedStateVersion"] = 16,
            ["route"] = b["breakdownFlow"]!["reactorRoute"]!.DeepClone(),
            ["reason"] = "reaction-completed",
            ["weatherKind"] = "normal",
            ["cohortInputs"] = new JsonArray()
        };
        Assert.Equal(Digest(Utf8(stopPreimage)), Assert.IsType<CampaignBreakdownFlow.ReactorStopOpen>(completed.State.BreakdownFlow).Stop.StopId);
        var stop = Digest(Utf8(new JsonObject
        {
            ["domain"] = "sandtable.action.breakdown-stop.v1",
            ["campaignId"] = b["campaignId"]!.DeepClone(),
            ["rulesetHash"] = b["rulesetHash"]!.DeepClone(),
            ["stateVersion"] = 16,
            ["audience"] = "system"
        }));
        var resolve = CampaignCombatReactionCompletion.Command(completed.State); var resolution = Assert.IsType<CampaignCombatReactionLifecycleCommand.Resolve>(resolve.Command);
        Assert.Equal(stop, resolution.StopId); Assert.NotEqual(stop, Assert.IsType<CampaignBreakdownFlow.ReactorStopOpen>(completed.State.BreakdownFlow).Stop.StopId);
        Assert.Equal(Digest(Utf8(new JsonObject { ["contractVersion"] = 1, ["kind"] = "resolve-breakdown-stop", ["stopId"] = stop })), resolution.Identity.ActionId);
        var resolved = Apply(p, moves, trigger, [completed.EventBytes], resolve); var close = CampaignCombatReactionCompletion.Command(resolved.State);
        Assert.Equal(window, Assert.IsType<CampaignCombatReactionLifecycleCommand.Close>(close.Command).WindowId);
        Assert.Equal(Digest(Utf8(new JsonObject { ["contractVersion"] = 1, ["kind"] = "close-reaction-window-no-eligible-reactor", ["windowId"] = window })), close.Command.Identity.ActionId);
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void EveryCommandFieldActorRetryAndSequenceForkRejects(string choice)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        var events = new List<byte[]>(); var inputs = new List<CampaignCombatReactionLifecycleInput>();
        for (var i = 0; i < 3; i++)
        {
            var state = Replay(p, moves, trigger, events); var input = CampaignCombatReactionCompletion.Command(state); inputs.Add(input);
            events.Add(Apply(p, moves, trigger, events, input).EventBytes);
        }
        for (var i = 0; i < 3; i++)
        {
            var input = inputs[i];
            foreach (var history in new[] { events.Take(i).ToArray(), events.ToArray() })
            {
                foreach (var actor in Enum.GetValues<CampaignOpeningPreambleActor>().Where(a => a != input.Actor).Append((CampaignOpeningPreambleActor)99))
                    Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, history, input with { Actor = actor }));
                foreach (var bytes in Mutations(CampaignCombatReactionCompletionCodec.SerializeInput(input)))
                    Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, history, CampaignCombatReactionCompletionCodec.DeserializeInput(bytes)));
                var original = JsonNode.Parse(CampaignCombatReactionCompletionCodec.SerializeInput(input))!;
                foreach (var path in Leaves(original, []))
                {
                    var changed = original.DeepClone(); ChangeLeaf(changed, path);
                    Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, history, CampaignCombatReactionCompletionCodec.DeserializeInput(Utf8(changed))));
                }
                foreach (var version in new long[] { 0, 13, 14, 15, 16, 17, 18, long.MaxValue }.Where(v => v != input.Command.Identity.ExpectedPriorVersion))
                {
                    var changed = original.DeepClone(); changed["command"]!["expectedPriorVersion"] = version;
                    Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, history, CampaignCombatReactionCompletionCodec.DeserializeInput(Utf8(changed))));
                }
            }
            for (var j = 0; j < i; j++) Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, events.Take(j).ToArray(), input));
        }
        var initial = Replay(p, moves, trigger, []); var first = Assert.IsType<CampaignCombatReactionLifecycleCommand.Move>(initial.Predecessor.Predecessor.Events[0].Input.Command);
        var complete = Assert.IsType<CampaignCombatReactionLifecycleCommand.Complete>(inputs[0].Command);
        foreach (var changed in new[] { complete with { OpportunityId = first.OpportunityId }, complete with { OpportunityId = initial.Predecessor.Events[0].Input.Command.OpportunityId },
            complete with { OpportunityId = initial.ReactionWindow!.ActiveOpportunityId!.Value }, complete with { WindowId = initial.ReactionWindow!.Trigger.WindowId.Value } })
            Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, [], inputs[0] with { Command = changed }));
        var stopState = Replay(p, moves, trigger, [events[0]]);
        var resolve = Assert.IsType<CampaignCombatReactionLifecycleCommand.Resolve>(inputs[1].Command);
        Assert.ThrowsAny<JsonException>(() => Apply(p, moves, trigger, [events[0]], inputs[1] with { Command = resolve with { StopId = Assert.IsType<CampaignBreakdownFlow.ReactorStopOpen>(stopState.BreakdownFlow).Stop.StopId } }));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionCompletionCodec.DeserializeInput(CampaignCombatReactionLifecycleCodec.SerializeInput(initial.Predecessor.Predecessor.Events[0].Input)));
        foreach (var history in new byte[][][] { [events[1]], [events[2]], [events[0], events[2]], [events[0], events[0]], [events[1], events[0]],
            [events[0], events[1], events[1]], [events[0], events[1], events[2], events[2]], [Second(p, moves, trigger)[0]], [Participant(p, moves, trigger)[0]] })
            Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, history));
        for (var i = 0; i <= 3; i++)
        {
            var cache = CampaignCombatReactionCompletionCodec.SerializeState(Replay(p, moves, trigger, events.Take(i).ToArray()));
            for (var j = 0; j <= 3; j++) if (i != j) Assert.ThrowsAny<JsonException>(() => Read(cache, p, moves, trigger, events.Take(j).ToArray()));
        }
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void EveryEventAndEveryCutCacheLeafRejectsRawAndResigned(string choice)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves); var events = new List<byte[]>();
        for (var cut = 0; cut <= 3; cut++)
        {
            var state = Replay(p, moves, trigger, events); var cacheBytes = CampaignCombatReactionCompletionCodec.SerializeState(state);
            foreach (var bytes in Mutations(cacheBytes)) Assert.ThrowsAny<JsonException>(() => Read(bytes, p, moves, trigger, events));
            if (cut == 3) break;
            var result = Apply(p, moves, trigger, events, CampaignCombatReactionCompletion.Command(state));
            foreach (var bytes in Mutations(result.EventBytes)) Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [.. events, bytes]));
            var original = JsonNode.Parse(result.EventBytes)!;
            foreach (var path in Leaves(original, []))
            {
                var changed = original.DeepClone(); ChangeLeaf(changed, path);
                if (path[0] != "receiptId") changed["receiptId"] = Receipt(changed);
                var bytes = Utf8(changed); Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [.. events, bytes]));
                var cache = JsonNode.Parse(CampaignCombatReactionCompletionCodec.SerializeState(result.State))!;
                cache["prefix"] = Prefix(state.Prefix, bytes); cache["receipts"]!.AsArray()[^1]!["eventHash"] = Digest(bytes);
                cache["receipts"]!.AsArray()[^1]!["receiptId"] = changed["receiptId"]!.DeepClone();
                Assert.ThrowsAny<JsonException>(() => Read(Utf8(cache), p, moves, trigger, [.. events, bytes]));
            }
            events.Add(result.EventBytes);
        }
    }
    [Fact]
    public void MissingForeignCompletedAndAlteredActualHistoryRejects()
    {
        var p = Ready(); var moves = FirstMove(p); var trigger = Trigger(p, moves); var participants = Participant(p, moves, trigger); var second = Second(p, moves, trigger);
        var state = Replay(p, moves, trigger, []); var input = CampaignCombatReactionCompletion.Command(state); var result = Apply(p, moves, trigger, [], input);
        foreach (var invalid in new byte[][][] { [], [second[0], second[0]], participants, [result.EventBytes] })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionCompletion.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, participants, invalid, []));
        var foreign = Ready(1, "act-last"); var foreignMoves = FirstMove(foreign, "act-last"); var foreignTrigger = Trigger(foreign, foreignMoves);
        Assert.ThrowsAny<JsonException>(() => Apply(foreign, foreignMoves, foreignTrigger, [], input));
        Assert.ThrowsAny<JsonException>(() => Replay(foreign, foreignMoves, foreignTrigger, [result.EventBytes]));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionCompletion.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, participants, Second(foreign, foreignMoves, foreignTrigger), []));
        Assert.ThrowsAny<JsonException>(() => Replay(p, [], trigger, [])); Assert.ThrowsAny<JsonException>(() => Replay(p, moves, [], []));
        Assert.ThrowsAny<JsonException>(() => Replay(Ready(1, "act-first", true), moves, trigger, []));
        foreach (var buffer in p.Opening.Concat(p.Stage).Concat(p.Reserve).Concat(moves).Concat(trigger).Concat(participants).Concat(second).Append(p.Created).Append(p.Weather))
        {
            var saved = buffer[0]; buffer[0] = (byte)' ';
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionCompletion.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, participants, second, [result.EventBytes]));
            buffer[0] = saved;
        }
        var first = state.Predecessor.Predecessor;
        var completed = CampaignCombatReactionLifecycle.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, participants, CampaignCombatReactionLifecycle.Command(first));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionCompletion.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, [.. participants, completed.EventBytes], second, []));
    }
    [Fact]
    public void MalformedBytesDepthItemsAndAuthorityCapacityReject()
    {
        var p = Ready(); var moves = FirstMove(p); var trigger = Trigger(p, moves); var state = Replay(p, moves, trigger, []);
        var result = Apply(p, moves, trigger, [], CampaignCombatReactionCompletion.Command(state));
        var excessive = JsonNode.Parse(result.EventBytes)!; excessive["unknown"] = new JsonArray(Enumerable.Range(0, 513).Select(_ => (JsonNode?)JsonValue.Create(0)).ToArray());
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes("null"), Encoding.UTF8.GetBytes("{}"), new byte[] { 0xff },
            Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)), Utf8(excessive) })
        {
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionCompletionCodec.DeserializeInput(bytes));
            Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [bytes])); Assert.ThrowsAny<JsonException>(() => Read(bytes, p, moves, trigger, []));
        }
        Assert.ThrowsAny<JsonException>(() => Replay(p, moves, trigger, [null!])); Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionCompletionCodec.SerializeInput(null!));
        var events = new List<byte[]>();
        for (var cut = 0; cut < 3; cut++)
        {
            state = Replay(p, moves, trigger, events);
            foreach (var invalid in new[] { Copy(state, version: long.MaxValue), Copy(state, receipts: Enumerable.Repeat(state.Receipts[0], 512)),
                Copy(state, progress: Enumerable.Repeat(state.ActualProgressRefs[0], 513)), Copy(state, tracks: Enumerable.Repeat(state.Tracks[0], 513)),
                Copy(state, tracks: [new(state.Tracks[0].Unit, Enumerable.Repeat("axis-rear", 513))]) })
                Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionCompletion.Command(invalid));
            var node = JsonNode.Parse(CampaignCombatReactionCompletionCodec.SerializeInput(CampaignCombatReactionCompletion.Command(state)))!;
            node["command"]!["expectedPriorVersion"] = true;
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionCompletionCodec.DeserializeInput(Utf8(node)));
            events.Add(Apply(p, moves, trigger, events, CampaignCombatReactionCompletion.Command(state)).EventBytes);
        }
    }
    [Fact]
    public void WholeTypedWorldGuardsAndOwnershipBuffersHoldAtEveryCut()
    {
        var p = Ready(); var moves = FirstMove(p); var trigger = Trigger(p, moves); var participants = Participant(p, moves, trigger); var second = Second(p, moves, trigger);
        var events = new List<byte[]>(); var cuts = new List<CampaignCombatReactionCompletionState>();
        for (var i = 0; i <= 3; i++)
        {
            var cut = CampaignCombatReactionCompletion.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, participants, second, events); cuts.Add(cut);
            var world = cut.World; var element = world.Elements[0]; var ledger = element.OperationalState;
            foreach (var forgedLedger in new[] {
                new CampaignElementOperationalStateV6(1, 1, new CapabilityPointAmount(7, 1), ledger.CohesionLevel, ledger.VehicleBreakdownState, ledger.MovementEnded, ledger.InitialLedgerOrigin),
                new CampaignElementOperationalStateV6(1, 1, ledger.CapabilityPointsExpended, ledger.CohesionLevel,
                    new CampaignVehicleBreakdownState("forged-cohort", BreakdownPointAmount.Zero, BreakdownPointAmount.Zero, null, 1, 0), ledger.MovementEnded, ledger.InitialLedgerOrigin) })
            {
                var changed = new CampaignElementStateV6(element.ElementId, element.CurrentLocationId, element.ReserveStatus, forgedLedger, element.Components,
                    element.SourceParentFormationId, element.CurrentParentFormationId, element.Ammunition, element.Readiness);
                var altered = new CampaignWorldSnapshotV7(7, world.CreationBinding, world.Elements.Select(e => e == element ? changed : e), world.Representations,
                    world.BrokenVehicleLots, world.CohesionCauses, world.Relationships, world.CustodyLots, world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
                Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionCompletionCodec.SerializeState(Copy(cut, world: altered)));
                var old = cut.Predecessor;
                var predecessor = new CampaignCombatReactionSecondMoveState(old.Predecessor, old.StateVersion, old.Prefix, altered, old.BreakdownFlow,
                    old.Receipts, old.Tracks, old.ActualProgressRefs, old.Events);
                Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionCompletionCodec.SerializeState(Copy(cut, world: altered, predecessor: predecessor)));
            }
            if (i < 3)
            {
                var input = CampaignCombatReactionCompletion.Command(cut); var bytes = CampaignCombatReactionCompletionCodec.SerializeInput(input);
                var detached = CampaignCombatReactionCompletionCodec.DeserializeInput(bytes); Array.Fill(bytes, (byte)0); Assert.Equal(input, detached);
                events.Add(Apply(p, moves, trigger, events, input).EventBytes);
            }
        }
        var expected = cuts.Select(CampaignCombatReactionCompletionCodec.SerializeState).ToArray(); var eventCopies = events.Select(e => e.ToArray()).ToArray();
        foreach (var buffer in p.Opening.Concat(p.Stage).Concat(p.Reserve).Concat(moves).Concat(trigger).Concat(participants).Concat(second).Concat(events).Append(p.Created).Append(p.Weather)) Array.Fill(buffer, (byte)0);
        for (var i = 0; i < 4; i++) Assert.Equal(expected[i], CampaignCombatReactionCompletionCodec.SerializeState(cuts[i]));
        for (var i = 0; i < 3; i++) Assert.Equal(eventCopies[i], CampaignCombatReactionCompletionCodec.SerializeEvent(cuts[3].Events[i]));
    }
    private static CampaignCombatReactionCompletionState Copy(CampaignCombatReactionCompletionState s, long? version = null,
        CampaignWorldSnapshotV7? world = null, CampaignCombatReactionSecondMoveState? predecessor = null,
        IEnumerable<CampaignOpeningPreambleReceipt>? receipts = null, IEnumerable<CampaignCombatInheritedTrack>? tracks = null,
        IEnumerable<CampaignCombatInheritedProgress>? progress = null) =>
        new(predecessor ?? s.Predecessor, version ?? s.StateVersion, s.Prefix, world ?? s.World, s.ReactionWindow, s.BreakdownFlow, receipts ?? s.Receipts, tracks ?? s.Tracks, progress ?? s.ActualProgressRefs, s.Events);
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
    private static byte[][] Participant(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger)
    {
        var state = CampaignCombatReactionLifecycle.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, []);
        return [CampaignCombatReactionLifecycle.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, [], CampaignCombatReactionLifecycle.Command(state)).EventBytes];
    }
    private static byte[][] Second(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger)
    {
        var participants = Participant(p, moves, trigger);
        var state = CampaignCombatReactionSecondMove.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, participants, []);
        return [CampaignCombatReactionSecondMove.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, participants, [], CampaignCombatReactionSecondMove.Command(state)).EventBytes];
    }
    private static CampaignCombatReactionCompletionState Replay(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events) => CampaignCombatReactionCompletion.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, Participant(p, moves, trigger), Second(p, moves, trigger), events);
    private static CampaignCombatReactionCompletionResult Apply(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events, CampaignCombatReactionLifecycleInput input) => CampaignCombatReactionCompletion.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, Participant(p, moves, trigger), Second(p, moves, trigger), events, input);
    private static CampaignCombatReactionCompletionState Read(byte[] bytes, Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events) => CampaignCombatReactionCompletion.ReadState(bytes, p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, Participant(p, moves, trigger), Second(p, moves, trigger), events);
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
        "Campaigns", "Fixtures", "combat-inherited-reaction-movement-completion-v1.json")));
    private static void CheckGolden(JsonElement goldens, string kind, byte[] bytes)
    {
        Assert.Equal(goldens.GetProperty(kind).GetProperty("bytes").GetInt32(), bytes.Length);
        Assert.Equal(goldens.GetProperty(kind).GetProperty("sha256").GetString(), Digest(bytes));
    }
    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static string Receipt(JsonNode value)
    {
        var unsigned = value.DeepClone(); unsigned.AsObject().Remove("receiptId");
        var type = value["eventType"]!.GetValue<string>();
        var version = type == "breakdown-stop-resolved" ? 2 : 3;
        return (version == 2 ? "iml." : "irl.") + Digest(Encoding.UTF8.GetBytes($"sandtable.combat.inherited-{type}-receipt.v{version}\0" + unsigned.ToJsonString()))[7..];
    }
}
