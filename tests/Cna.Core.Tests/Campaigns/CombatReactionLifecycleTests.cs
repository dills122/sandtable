using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatReactionLifecycleTests
{
    [Theory]
    [InlineData("act-first", "axis-one-participant-reaction-lifecycle")]
    [InlineData("act-last", "commonwealth-one-participant-reaction-lifecycle")]
    public void FrozenLifecycleRetainsEveryCutAndAuthenticatedTerminalRetry(string choice, string name)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        using var fixture = Fixture();
        var goldens = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(x => x.GetProperty("name").GetString() == name).GetProperty("goldens");
        CheckGolden(goldens, "trigger", trigger[0]);
        var state = Replay(p, moves, trigger, []);
        CheckGolden(goldens, "state-0", CampaignCombatReactionLifecycleCodec.SerializeState(state));
        var initial = state;
        Assert.Equal(CampaignCombatReactionTriggerCodec.SerializeState(state.Trigger), CampaignCombatReactionLifecycleCodec.SerializeState(state));
        var initialRoute = Assert.IsType<CampaignPhasingContinuation.ResumeRoute>(Assert.IsType<CampaignBreakdownFlow.Reacting>(state.BreakdownFlow).PhasingContinuation).Route;
        var events = new List<byte[]>(); var inputs = new List<CampaignCombatReactionLifecycleInput>();
        for (var i = 0; i < 4; i++)
        {
            var input = CampaignCombatReactionLifecycle.Command(state);
            CheckGolden(goldens, $"input-{i + 1}", CampaignCombatReactionLifecycleCodec.SerializeInput(input));
            var prior = state;
            CheckPublicIdentity(prior, input);
            var result = Apply(p, moves, trigger, events, input);
            Assert.False(result.Duplicate); inputs.Add(input); events.Add(result.EventBytes); state = result.State;
            CheckGolden(goldens, $"event-{i + 1}", result.EventBytes);
            CheckEffects(initial, prior, state, input, result.EventBytes, i);
            Assert.Equal(Prefix(prior.Prefix, result.EventBytes), state.Prefix);
            Assert.Equal(Receipt(JsonNode.Parse(result.EventBytes)!), state.Receipts[^1].ReceiptId);
            Assert.Equal(Digest(CampaignCombatReactionLifecycleCodec.SerializeInput(input)), state.Receipts[^1].CommandHash);
            Assert.Equal(Digest(result.EventBytes), state.Receipts[^1].EventHash);
            Assert.ThrowsAny<JsonException>(() => CampaignEventSerializer.Deserialize(result.EventBytes));
            var bytes = CampaignCombatReactionLifecycleCodec.SerializeState(state);
            CheckGolden(goldens, $"state-{i + 1}", bytes);
            Assert.Equal(bytes, CampaignCombatReactionLifecycleCodec.SerializeState(Replay(p, moves, trigger, events)));
            Assert.Equal(bytes, CampaignCombatReactionLifecycleCodec.SerializeState(Read(bytes, p, moves, trigger, events)));
        }
        Assert.Equal(17, state.StateVersion);
        Assert.Null(state.ReactionWindow);
        Assert.Equal(initialRoute, Assert.IsType<CampaignBreakdownFlow.Moving>(state.BreakdownFlow).Route);
        Assert.Equal(initial.ActualProgressRefs.Count + 1, state.ActualProgressRefs.Count);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeState(initial))!["members"],
            JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeState(state))!["members"]));
        var phasing = state.World.Elements.Single(e => e.ElementId == state.Trigger.Members[0].Unit.ElementId);
        var reacting = state.World.Elements.Single(e => e.ElementId != phasing.ElementId);
        Assert.Equal(new CapabilityPointAmount(4, 1), phasing.OperationalState.CapabilityPointsExpended);
        Assert.Equal(new CapabilityPointAmount(2, 1), reacting.OperationalState.CapabilityPointsExpended);
        Assert.Equal(choice == "act-first" ? "assault-west" : "assault-east", phasing.CurrentLocationId);
        Assert.Equal(choice == "act-first" ? "commonwealth-rear" : "axis-rear", reacting.CurrentLocationId);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionLifecycle.Command(state));
        Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(CampaignCombatReactionLifecycleCodec.SerializeState(state), p.Created, p.Request));
        foreach (var (input, i) in inputs.Select((input, i) => (input, i)))
        {
            var retry = Apply(p, moves, trigger, events, input);
            Assert.True(retry.Duplicate); Assert.Equal(events[i], retry.EventBytes);
            Assert.Equal(CampaignCombatReactionLifecycleCodec.SerializeState(state), CampaignCombatReactionLifecycleCodec.SerializeState(retry.State));
        }
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void PublicAuthoritySubstitutionAndActorsRejectBeforeAndAfterTerminal(string choice)
    {
        var t = Trace(choice);
        for (var i = 0; i < 4; i++)
        {
            foreach (var actor in Enum.GetValues<CampaignOpeningPreambleActor>().Where(actor => actor != t.Inputs[i].Actor))
            {
                var wrongActor = t.Inputs[i] with { Actor = actor };
                Assert.ThrowsAny<JsonException>(() => Apply(t.Pack, t.Moves, t.Trigger, t.Events.Take(i).ToArray(), wrongActor));
                Assert.ThrowsAny<JsonException>(() => Apply(t.Pack, t.Moves, t.Trigger, t.Events, wrongActor));
            }
            var foreignKind = t.Inputs[i == 3 ? 2 : 3];
            var foreignNode = JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeInput(foreignKind))!;
            foreignNode["command"]!["expectedPriorVersion"] = t.Inputs[i].Command.Identity.ExpectedPriorVersion;
            var sameOccurrence = CampaignCombatReactionLifecycleCodec.DeserializeInput(Utf8(foreignNode));
            Assert.ThrowsAny<JsonException>(() => Apply(t.Pack, t.Moves, t.Trigger, t.Events, sameOccurrence));
            var original = JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeInput(t.Inputs[i]))!;
            var state = JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeState(t.States[i]))!;
            foreach (var edit in new Action<JsonNode>[] {
                n => n["actor"] = t.Inputs[i].Actor == CampaignOpeningPreambleActor.System ? "axis" : "system",
                n => n["command"]!["cycleId"] = "sha256:" + new string('f', 64),
                n => n["command"]!["creationEventHash"] = "sha256:" + new string('f', 64),
                n => n["command"]!["creationBinding"] = "foreign.creation",
                n => n["command"]!["expectedPriorVersion"] = 1,
                n => n["command"]!["expectedPositionId"] = "foreign.position",
                n => n["command"]!["actionId"] = "sha256:" + new string('f', 64)
            })
            {
                var altered = original.DeepClone(); edit(altered);
                var input = CampaignCombatReactionLifecycleCodec.DeserializeInput(Utf8(altered));
                Assert.ThrowsAny<JsonException>(() => Apply(t.Pack, t.Moves, t.Trigger, t.Events.Take(i).ToArray(), input));
                Assert.ThrowsAny<JsonException>(() => Apply(t.Pack, t.Moves, t.Trigger, t.Events, input));
            }
            var field = i == 2 ? "stopId" : i == 1 ? "opportunityId" : "windowId";
            var authority = i == 2 ? state["breakdownFlow"]!["stop"]!["stopId"] : i == 1 ? state["reactionWindow"]!["activeOpportunityId"] : state["reactionWindow"]!["reactionWindowId"];
            var forged = original.DeepClone(); forged["command"]![field] = authority!.DeepClone();
            forged["command"]!["actionId"] = ActionHash(forged["command"]!, i);
            var substitution = CampaignCombatReactionLifecycleCodec.DeserializeInput(Utf8(forged));
            Assert.ThrowsAny<JsonException>(() => Apply(t.Pack, t.Moves, t.Trigger, t.Events.Take(i).ToArray(), substitution));
            Assert.ThrowsAny<JsonException>(() => Apply(t.Pack, t.Moves, t.Trigger, t.Events, substitution));
        }
        var first = JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeInput(t.Inputs[0]))!;
        var window = JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeState(t.States[0]))!["reactionWindow"]!;
        first["command"]!["opportunityId"] = window["frozenOpportunities"]![0]!["opportunityId"]!.DeepClone();
        first["command"]!["actionId"] = ActionHash(first["command"]!, 0);
        Assert.ThrowsAny<JsonException>(() => Apply(t.Pack, t.Moves, t.Trigger, [], CampaignCombatReactionLifecycleCodec.DeserializeInput(Utf8(first))));
    }
    [Fact]
    public void MandatoryResolutionHistoryOrderAndUnsupportedBranchesReject()
    {
        var t = Trace();
        foreach (var events in new[] {
            new[] { t.Events[1] }, new[] { t.Events[2] }, new[] { t.Events[3] },
            t.Events.Skip(1).ToArray(), new[] { t.Events[0], t.Events[2] }, new[] { t.Events[0], t.Events[1], t.Events[3] },
            t.Events.Reverse().ToArray(), new[] { t.Events[0], t.Events[0] }, t.Events.Append(t.Events[^1]).ToArray()
        }) Assert.ThrowsAny<JsonException>(() => Replay(t.Pack, t.Moves, t.Trigger, events));
        foreach (var input in t.Inputs.Skip(1)) Assert.ThrowsAny<JsonException>(() => Apply(t.Pack, t.Moves, t.Trigger, [], input));
        var secondMove = JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeInput(t.Inputs[0]))!;
        var completion = JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeInput(t.Inputs[1]))!;
        var option = Assert.Single(CampaignCombatReactionLifecycle.MoveOptions(t.States[1]));
        secondMove["command"]!["expectedPriorVersion"] = 14;
        secondMove["command"]!["opportunityId"] = completion["command"]!["opportunityId"]!.DeepClone();
        secondMove["command"]!["originLocationId"] = option.OriginLocationId;
        secondMove["command"]!["destinationLocationId"] = option.DestinationLocationId;
        secondMove["command"]!["actionId"] = ActionHash(secondMove["command"]!, 0);
        Assert.ThrowsAny<JsonException>(() => Apply(t.Pack, t.Moves, t.Trigger, [t.Events[0]], CampaignCombatReactionLifecycleCodec.DeserializeInput(Utf8(secondMove))));
        foreach (var kind in new[] { "decline-reaction-window", "close-reaction-window-timeout", "close-reaction-window-scripted-unavailable", "complete-movement-segment" })
        {
            var input = JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeInput(t.Inputs[3]))!;
            input["command"]!["kind"] = kind;
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionLifecycleCodec.DeserializeInput(Utf8(input)));
        }
        var alteredTrigger = JsonNode.Parse(t.Trigger[0])!;
        alteredTrigger["openedReactionWindow"]!["reactingSide"] = "axis";
        Assert.ThrowsAny<JsonException>(() => Replay(t.Pack, t.Moves, [Utf8(alteredTrigger)], []));
        var alteredCreation = t.Pack.Created.ToArray(); alteredCreation[^2] ^= 1;
        Assert.ThrowsAny<JsonException>(() => Replay(t.Pack with { Created = alteredCreation }, t.Moves, t.Trigger, []));
        Assert.ThrowsAny<JsonException>(() => Replay(t.Pack, t.Moves, [], []));
        Assert.ThrowsAny<JsonException>(() => Replay(t.Pack, t.Moves, [t.Trigger[0], t.Trigger[0]], []));
        Assert.ThrowsAny<JsonException>(() => Replay(t.Pack, [], t.Trigger, []));
        Assert.ThrowsAny<JsonException>(() => Replay(t.Pack with { Reserve = [] }, t.Moves, t.Trigger, []));
        Assert.ThrowsAny<JsonException>(() => Replay(t.Pack with { Stage = t.Pack.Stage.Reverse().ToArray() }, t.Moves, t.Trigger, []));
        Assert.ThrowsAny<JsonException>(() => Replay(t.Pack with { Opening = t.Pack.Opening.Take(3).ToArray() }, t.Moves, t.Trigger, []));
        var foreign = Trace("act-last");
        Assert.ThrowsAny<JsonException>(() => Replay(foreign.Pack, foreign.Moves, foreign.Trigger, t.Events));
        Assert.ThrowsAny<JsonException>(() => Replay(t.Pack, foreign.Moves, foreign.Trigger, []));
        foreach (var seed in new ulong[] { 0, 2, 3 }) Assert.ThrowsAny<JsonException>(() => Replay(Ready(seed), t.Moves, t.Trigger, []));
        Assert.ThrowsAny<JsonException>(() => Replay(Ready(1, "act-first", true), t.Moves, t.Trigger, []));
    }
    [Fact]
    public void CanonicalAndResignedEventEffectsRejectAtEveryCut()
    {
        var t = Trace();
        for (var i = 0; i < 4; i++)
        {
            var prefix = t.Events.Take(i).ToArray();
            foreach (var mutated in Mutations(CampaignCombatReactionLifecycleCodec.SerializeInput(t.Inputs[i])))
                Assert.ThrowsAny<JsonException>(() => Apply(t.Pack, t.Moves, t.Trigger, prefix, CampaignCombatReactionLifecycleCodec.DeserializeInput(mutated)));
            foreach (var mutated in Mutations(t.Events[i]))
                Assert.ThrowsAny<JsonException>(() => Replay(t.Pack, t.Moves, t.Trigger, prefix.Append(mutated).ToArray()));
            var original = JsonNode.Parse(t.Events[i])!;
            foreach (var path in Leaves(original, []))
            {
                var changed = original.DeepClone(); ChangeLeaf(changed, path);
                if (path[0] != "receiptId") changed["receiptId"] = Receipt(changed);
                var bytes = Utf8(changed);
                Assert.ThrowsAny<JsonException>(() => Replay(t.Pack, t.Moves, t.Trigger, prefix.Append(bytes).ToArray()));
                // Coherently re-sign the cache receipt and prefix too: the history-derived event still wins.
                var cache = JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeState(t.States[i + 1]))!;
                cache["prefix"] = Prefix(t.States[i].Prefix, bytes);
                cache["receipts"]![12 + i]!["eventHash"] = Digest(bytes);
                cache["receipts"]![12 + i]!["receiptId"] = changed["receiptId"]!.DeepClone();
                Assert.ThrowsAny<JsonException>(() => Read(Utf8(cache), t.Pack, t.Moves, t.Trigger, prefix.Append(bytes).ToArray()));
            }
        }
    }
    [Fact]
    public void CachedWorldRngRouteProgressAndWindowForgeriesReject()
    {
        var t = Trace();
        for (var i = 0; i <= 4; i++)
        {
            var bytes = CampaignCombatReactionLifecycleCodec.SerializeState(t.States[i]);
            foreach (var altered in Mutations(bytes))
                Assert.ThrowsAny<JsonException>(() => Read(altered, t.Pack, t.Moves, t.Trigger, t.Events.Take(i).ToArray()));
        }
        foreach (var edit in new Action<JsonNode>[] {
            n => n["reactionWindow"]!["activeOpportunityId"] = null,
            n => n["breakdownFlow"]!["reactorRoute"]!["originLocationId"] = "axis-supply",
            n => n["breakdownFlow"]!["phasingContinuation"]!["route"]!["currentLocationId"] = "axis-rear",
            n => n["randomState"]!["nextByteCursor"] = 100,
            n => n["tracks"]![1]!["route"]![1] = "axis-supply",
            n => n["actualProgressRefs"] = new JsonArray(),
            n => n["world"]!["elements"]![0]!["ammunition"]!["points"] = 9,
            n => n["world"]!["elements"]![0]!["operationalState"]!["capabilityPointsExpended"]!["numerator"] = 7,
            n => n["world"]!["elements"]![0]!["readiness"]!["pinned"] = true
        })
        {
            var cache = JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeState(t.States[1]))!; edit(cache);
            Assert.ThrowsAny<JsonException>(() => Read(Utf8(cache), t.Pack, t.Moves, t.Trigger, [t.Events[0]]));
        }
    }
    [Fact]
    public void BoundedWriterRejectsTypedWorldForgeryAndBuffersAreDetached()
    {
        var t = Trace(); var state = t.States[1]; var world = state.World;
        var element = world.Elements[0]; var ledger = element.OperationalState;
        var changed = new CampaignElementStateV6(element.ElementId, element.CurrentLocationId, element.ReserveStatus,
            new CampaignElementOperationalStateV6(1, 1, new CapabilityPointAmount(7, 1), ledger.CohesionLevel,
                ledger.VehicleBreakdownState, ledger.MovementEnded, ledger.InitialLedgerOrigin), element.Components,
            element.SourceParentFormationId, element.CurrentParentFormationId, element.Ammunition, element.Readiness);
        var altered = new CampaignWorldSnapshotV7(7, world.CreationBinding, world.Elements.Select(e => e == element ? changed : e),
            world.Representations, world.BrokenVehicleLots, world.CohesionCauses, world.Relationships, world.CustodyLots,
            world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
        var forged = new CampaignCombatReactionLifecycleState(state.Trigger, state.StateVersion, state.Prefix, altered,
            state.ReactionWindow, state.BreakdownFlow, state.Receipts, state.Tracks, state.ActualProgressRefs, state.Events);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionLifecycleCodec.SerializeState(forged));
        var bytes = CampaignCombatReactionLifecycleCodec.SerializeState(t.States[4]);
        var inputBytes = CampaignCombatReactionLifecycleCodec.SerializeInput(t.Inputs[0]);
        var detached = CampaignCombatReactionLifecycleCodec.DeserializeInput(inputBytes); Array.Fill(inputBytes, (byte)0);
        Assert.Equal(t.Inputs[0], detached);
        foreach (var buffer in t.Events.Concat(t.Moves).Concat(t.Trigger).Concat(t.Pack.Opening).Concat(t.Pack.Stage).Concat(t.Pack.Reserve).Append(t.Pack.Created).Append(t.Pack.Weather)) Array.Fill(buffer, (byte)0);
        Assert.Equal(bytes, CampaignCombatReactionLifecycleCodec.SerializeState(t.States[4]));
    }
    [Fact]
    public void BoundsNullAndMalformedBytesReject()
    {
        var t = Trace();
        var excessive = JsonNode.Parse(t.Events[0])!;
        excessive["breakdownAccounting"] = new JsonArray(Enumerable.Range(0, 513).Select(_ => (JsonNode?)JsonValue.Create(0)).ToArray());
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes("null"), Encoding.UTF8.GetBytes("{}"),
            new byte[] { 0xff }, Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)), Utf8(excessive) })
        {
            Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionLifecycleCodec.DeserializeInput(bytes));
            Assert.ThrowsAny<JsonException>(() => Replay(t.Pack, t.Moves, t.Trigger, [bytes]));
            Assert.ThrowsAny<JsonException>(() => Read(bytes, t.Pack, t.Moves, t.Trigger, []));
        }
        Assert.ThrowsAny<JsonException>(() => Replay(t.Pack, t.Moves, t.Trigger, [null!]));
        var input = t.Inputs[0];
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReactionLifecycleCodec.SerializeInput(input with { Actor = (CampaignOpeningPreambleActor)99 }));
    }
    private static void CheckEffects(CampaignCombatReactionLifecycleState initial, CampaignCombatReactionLifecycleState prior,
        CampaignCombatReactionLifecycleState state, CampaignCombatReactionLifecycleInput input, byte[] bytes, int index)
    {
        var e = JsonNode.Parse(bytes)!;
        var before = JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeState(prior))!;
        var after = JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeState(state))!;
        foreach (var field in new[] { "randomState", "members", "operationStageWeather", "operationStageOrders", "initiativeHolder", "cycle", "sequencePosition" })
            Assert.True(JsonNode.DeepEquals(before[field], after[field]), field);
        Assert.Equal(index == 0 ? prior.ActualProgressRefs.Count + 1 : prior.ActualProgressRefs.Count, state.ActualProgressRefs.Count);
        var authority = initial.ReactionWindow!.Trigger.FrozenOpportunities.Single().OpportunityId;
        if (index == 0)
        {
            Assert.Equal(authority, state.ReactionWindow!.ActiveOpportunityId); Assert.Empty(state.ReactionWindow.ResolvedOpportunityIds);
            var route = Assert.IsType<CampaignBreakdownFlow.Reacting>(state.BreakdownFlow).ReactorRoute!;
            Assert.Equal(14, route.FirstMoveStateVersion); Assert.Empty(route.CohortIds);
            Assert.Equal(0, e["capabilityPointsExpendedBefore"]!["numerator"]!.GetValue<int>());
            Assert.Equal(2, e["capabilityPointsExpendedAfter"]!["numerator"]!.GetValue<int>());
            Assert.Equal(0, e["cohesionAfter"]!.GetValue<int>());
            CheckRouteIdentity(e, e["breakdownFlowAfter"]!["reactorRoute"]!);
            Assert.Equal("reaction", after["currentPosition"]!["kind"]!.GetValue<string>());
        }
        else
        {
            Assert.Equal(prior.World, state.World); Assert.True(JsonNode.DeepEquals(before["tracks"], after["tracks"]));
            if (index == 1)
            {
                Assert.Null(state.ReactionWindow!.ActiveOpportunityId); Assert.Equal(authority, Assert.Single(state.ReactionWindow.ResolvedOpportunityIds));
                var stop = Assert.IsType<CampaignBreakdownFlow.ReactorStopOpen>(state.BreakdownFlow).Stop;
                Assert.Equal(15, stop.RecordedStateVersion); Assert.Equal(CampaignBreakdownStopReason.ReactionCompleted, stop.Reason);
                Assert.Equal(BreakdownWeatherKind.Normal, stop.WeatherKind); Assert.Empty(stop.CohortInputs);
                Assert.Equal("breakdown-stop", after["currentPosition"]!["kind"]!.GetValue<string>());
                var stopNode = e["breakdownFlowAfter"]!["stop"]!;
                var preimage = new JsonObject { ["domain"] = "sandtable.breakdown.stop.v1", ["campaignId"] = e["campaignId"]!.DeepClone(), ["rulesetHash"] = e["rulesetHash"]!.DeepClone() };
                foreach (var pair in stopNode.AsObject().Where(p => p.Key != "stopId")) preimage.Add(pair.Key, pair.Value?.DeepClone());
                Assert.Equal(stop.StopId, Digest(Utf8(preimage)));
            }
            else if (index == 2)
            {
                Assert.True(JsonNode.DeepEquals(before["reactionWindow"], after["reactionWindow"]));
                Assert.True(JsonNode.DeepEquals(before["breakdownFlow"]!["stop"], e["stop"]));
                Assert.True(JsonNode.DeepEquals(e["randomStateBefore"], e["randomStateAfter"]));
                Assert.Empty(e["checks"]!.AsArray()); Assert.Empty(e["createdLots"]!.AsArray());
                Assert.Equal("21.24-21.26", e["sources"]![0]!["locator"]!.GetValue<string>());
                Assert.Null(Assert.IsType<CampaignBreakdownFlow.Reacting>(state.BreakdownFlow).ReactorRoute);
                Assert.Equal("reaction", after["currentPosition"]!["kind"]!.GetValue<string>());
            }
            else
            {
                Assert.Null(e["actingSide"]); Assert.Equal(CampaignOpeningPreambleActor.System, input.Actor);
                Assert.Empty(e["closedOpportunityIds"]!.AsArray()); Assert.Equal("no-eligible-reactor", e["reason"]!.GetValue<string>());
                Assert.Equal("sequence", after["currentPosition"]!["kind"]!.GetValue<string>());
                Assert.True(JsonNode.DeepEquals(e["suspendedSequencePosition"], before["sequencePosition"]));
            }
        }
    }
    private static void CheckPublicIdentity(CampaignCombatReactionLifecycleState state, CampaignCombatReactionLifecycleInput input)
    {
        var stateNode = JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeState(state))!;
        var cmd = JsonNode.Parse(CampaignCombatReactionLifecycleCodec.SerializeInput(input))!["command"]!;
        var w = stateNode["reactionWindow"]!;
        var publicWindow = Digest(Utf8(new JsonObject
        {
            ["domain"] = "sandtable.observation.reaction-window.v1",
            ["campaignId"] = stateNode["campaignId"]!.DeepClone(),
            ["rulesetHash"] = stateNode["rulesetHash"]!.DeepClone(),
            ["committedStateVersion"] = 13,
            ["reactingSide"] = w["reactingSide"]!.DeepClone()
        }));
        if (input.Command is not CampaignCombatReactionLifecycleCommand.Resolve)
        {
            Assert.Equal(publicWindow, cmd["windowId"]!.GetValue<string>());
            Assert.NotEqual(w["reactionWindowId"]!.GetValue<string>(), publicWindow);
        }
        if (input.Command is CampaignCombatReactionLifecycleCommand.Move or CampaignCombatReactionLifecycleCommand.Complete)
        {
            var side = w["reactingSide"]!.GetValue<string>(); var completion = input.Command is CampaignCombatReactionLifecycleCommand.Complete;
            var origin = completion ? side + "-rear" : side == "axis" ? "assault-west" : "assault-east";
            var destination = side + (completion ? "-supply" : "-rear");
            var option = Assert.Single(CampaignCombatReactionLifecycle.MoveOptions(state));
            Assert.Equal(origin, option.OriginLocationId); Assert.Equal(destination, option.DestinationLocationId);
            var options = new JsonArray(new JsonObject { ["originLocationId"] = origin, ["destinationLocationId"] = destination, ["costBreakdown"] = PublicCost() });
            var capability = Digest(Utf8(new JsonObject { ["domain"] = "sandtable.observation.reaction-capability.v1", ["moveOptions"] = options }));
            var preimage = new JsonObject { ["domain"] = "sandtable.observation.reaction-opportunity.v2", ["windowId"] = publicWindow, ["stateVersion"] = state.StateVersion, ["capabilityKey"] = capability };
            Assert.Equal(Digest(Utf8(preimage)), cmd["opportunityId"]!.GetValue<string>());
            Assert.NotEqual(w["frozenOpportunities"]![0]!["opportunityId"]!.GetValue<string>(), cmd["opportunityId"]!.GetValue<string>());
            preimage["capabilityKey"] = Digest(Utf8(new JsonObject { ["domain"] = "sandtable.observation.reaction-capability.v1", ["moveOptions"] = new JsonArray() }));
            Assert.NotEqual(Digest(Utf8(preimage)), cmd["opportunityId"]!.GetValue<string>());
            preimage["capabilityKey"] = capability; preimage["stateVersion"] = state.StateVersion - 1;
            Assert.NotEqual(Digest(Utf8(preimage)), cmd["opportunityId"]!.GetValue<string>());
        }
        else if (input.Command is CampaignCombatReactionLifecycleCommand.Resolve)
        {
            var stop = Digest(Utf8(new JsonObject
            {
                ["domain"] = "sandtable.action.breakdown-stop.v1",
                ["campaignId"] = stateNode["campaignId"]!.DeepClone(),
                ["rulesetHash"] = stateNode["rulesetHash"]!.DeepClone(),
                ["stateVersion"] = state.StateVersion,
                ["audience"] = "system"
            }));
            Assert.Equal(stop, cmd["stopId"]!.GetValue<string>());
            Assert.NotEqual(stateNode["breakdownFlow"]!["stop"]!["stopId"]!.GetValue<string>(), stop);
        }
        Assert.Equal(ActionHash(cmd, state.Events.Count), input.Command.Identity.ActionId);
        Assert.Throws<ArgumentException>(() => CampaignObservationV6DisclosureIdentity.CreateWindow(state.Opening.Cycle!.CampaignId,
            state.Opening.Cycle.RulesetHash, 13, state.Trigger.ReactionWindow!.ReactingSide));
    }
    private static void CheckRouteIdentity(JsonNode e, JsonNode route)
    {
        var preimage = new JsonObject { ["domain"] = "sandtable.breakdown.route.v1", ["campaignId"] = e["campaignId"]!.DeepClone(), ["rulesetHash"] = e["rulesetHash"]!.DeepClone() };
        foreach (var pair in route.AsObject().Where(p => p.Key is not "routeId" and not "currentLocationId")) preimage.Add(pair.Key, pair.Value?.DeepClone());
        Assert.Equal(route["routeId"]!.GetValue<string>(), Digest(Utf8(preimage)));
    }
    private static JsonNode PublicCost() => JsonNode.Parse("{\"destinationTerrainId\":\"land.terrain.clear\",\"destinationTerrainCost\":{\"numerator\":2,\"denominator\":1},\"routeAdjustment\":null,\"crossedHexsideCosts\":[],\"totalCost\":{\"numerator\":2,\"denominator\":1}}")!;
    private static string ActionHash(JsonNode command, int index)
    {
        var action = new JsonObject { ["contractVersion"] = 1 };
        foreach (var pair in command.AsObject().Where(p => p.Key is "kind" or "windowId" or "opportunityId" or "originLocationId" or "destinationLocationId" or "stopId")) action.Add(pair.Key, pair.Value?.DeepClone());
        if (index == 0) action.Add("costBreakdown", PublicCost());
        return Digest(Utf8(action));
    }
    private static void ChangeLeaf(JsonNode node, string[] path)
    {
        foreach (var part in path[..^1]) node = node is JsonArray a ? a[int.Parse(part, System.Globalization.CultureInfo.InvariantCulture)]! : node[part]!;
        var key = path[^1]; var old = node is JsonArray list ? list[int.Parse(key, System.Globalization.CultureInfo.InvariantCulture)] : node[key];
        JsonNode? replacement = old is null ? JsonValue.Create("unexpected") : old is JsonArray ? new JsonArray("unexpected") : old is JsonValue value && value.TryGetValue<bool>(out var boolean) ? JsonValue.Create(!boolean) : old is JsonValue number && number.TryGetValue<long>(out var integer) ? JsonValue.Create(integer + 1) : JsonValue.Create((old.GetValue<string>().StartsWith("sha256:", StringComparison.Ordinal) ? "sha256:" + new string('f', 64) : old.GetValue<string>() + "x"));
        if (node is JsonArray array) array[int.Parse(key, System.Globalization.CultureInfo.InvariantCulture)] = replacement; else node[key] = replacement;
    }
    private sealed record LifecycleTrace(Pack Pack, byte[][] Moves, byte[][] Trigger, byte[][] Events, CampaignCombatReactionLifecycleInput[] Inputs, CampaignCombatReactionLifecycleState[] States);
    private static LifecycleTrace Trace(string choice = "act-first")
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        var states = new List<CampaignCombatReactionLifecycleState> { Replay(p, moves, trigger, []) };
        var events = new List<byte[]>(); var inputs = new List<CampaignCombatReactionLifecycleInput>();
        for (var i = 0; i < 4; i++)
        {
            var input = CampaignCombatReactionLifecycle.Command(states[^1]); var result = Apply(p, moves, trigger, events, input);
            states.Add(result.State); events.Add(result.EventBytes); inputs.Add(input);
        }
        return new(p, moves, trigger, events.ToArray(), inputs.ToArray(), states.ToArray());
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
    private static CampaignCombatReactionLifecycleState Replay(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events) => CampaignCombatReactionLifecycle.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, events);
    private static CampaignCombatReactionLifecycleResult Apply(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events, CampaignCombatReactionLifecycleInput input) => CampaignCombatReactionLifecycle.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, events, input);
    private static CampaignCombatReactionLifecycleState Read(byte[] bytes, Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events) => CampaignCombatReactionLifecycle.ReadState(bytes, p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, events);
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
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
        "Campaigns", "Fixtures", "combat-inherited-reaction-lifecycle-v1.json")));
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
        var type = value["eventType"]!.GetValue<string>();
        var domain = type switch
        {
            "reacting-element-moved" => "sandtable.combat.inherited-reacting-element-moved-receipt.v3",
            "reaction-participant-completed" => "sandtable.combat.inherited-reaction-participant-completed-receipt.v3",
            "breakdown-stop-resolved" => "sandtable.combat.inherited-breakdown-stop-resolved-receipt.v2",
            _ => "sandtable.combat.inherited-reaction-window-closed-receipt.v3"
        };
        return (type == "breakdown-stop-resolved" ? "iml." : "irl.") + Digest(Encoding.UTF8.GetBytes(domain + "\0" + unsigned.ToJsonString()))[7..];
    }
}
