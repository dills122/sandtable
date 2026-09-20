using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatMovementHistoryReplayTests
{
    public static IEnumerable<object[]> Cases()
    {
        foreach (var side in new[] { "axis", "commonwealth" })
            foreach (var count in new[] { 1, 5, 6, 7 }) yield return [side, count];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryOrdinaryLifecycleAndCombatEntryPrefixMatchesActualReadersAndFrozenEvidence(string side, int count)
    {
        var trace = Trace(side, count);
        using var fixture = Fixture("combat-inherited-snapshot-v1.json");
        using var moveFixture = Fixture("combat-inherited-movement-v1.json");
        using var lifecycleFixture = Fixture("combat-inherited-movement-lifecycle-v1.json");
        using var breakdownFixture = Fixture("combat-inherited-breakdown-completion-v1.json");
        var movementGold = moveFixture.RootElement.GetProperty("cases").EnumerateArray().Single(r => r.GetProperty("expected").GetProperty("actor").GetString() == side).GetProperty("goldens");
        var lifecycleGold = lifecycleFixture.RootElement.GetProperty("cases").EnumerateArray().Single(r => r.GetProperty("actor").GetString() == side && r.GetProperty("moves").GetInt32() == count).GetProperty("goldens");
        var breakdownGold = breakdownFixture.RootElement.GetProperty("cases").EnumerateArray().Single(r => r.GetProperty("actor").GetString() == side && r.GetProperty("moves").GetInt32() == count).GetProperty("goldens");
        for (var cut = 10; cut <= trace.Events.Length; cut++)
        {
            var events = trace.Events.Take(cut).ToArray();
            var result = CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, events);
            var bytes = Serialize(result.Projection);
            Assert.Equal(trace.States[cut - 10], bytes);
            Assert.Equal(cut + 1, result.Projection.StateVersion);
            Assert.Equal(cut, result.Projection.Receipts.Count);
            Assert.Equal(trace.Created, result.History.Created);
            Assert.Equal(events.Select(Digest), result.History.Events.Select(Digest));
            var rows = fixture.RootElement.GetProperty("roots").EnumerateArray().Where(r => Identity(r) == Identity(trace.Created, events)).ToArray();
            Assert.NotEmpty(rows);
            foreach (var row in rows) CheckH0(row, bytes, result.Projection);
            if (cut > 10 && cut <= 10 + count)
            {
                CheckGolden(movementGold.GetProperty($"state-{cut + 1}"), bytes);
                CheckGolden(movementGold.GetProperty($"event-{cut - 10}"), events[^1]);
                Assert.IsType<CampaignCombatHistoryProjection.Movement>(result.Projection);
            }
            else if (cut > 10 + count && cut <= 13 + count)
            {
                CheckGolden(lifecycleGold, 7 + cut - 10 - count, bytes);
                CheckGolden(lifecycleGold, 3 + cut - 10 - count, events[^1]);
                Assert.IsType<CampaignCombatHistoryProjection.MovementLifecycle>(result.Projection);
            }
            else if (cut == 14 + count)
            {
                CheckGolden(breakdownGold, 4, bytes);
                CheckGolden(breakdownGold, 2, events[^1]);
                var state = Assert.IsType<CampaignCombatHistoryProjection.BreakdownCompletion>(result.Projection).State;
                Assert.NotNull(state.Lifecycle.MovementEnd);
                Assert.IsType<CampaignBreakdownFlow.Idle>(state.Lifecycle.BreakdownFlow);
                Assert.Null(state.Lifecycle.InterruptContext);
                Assert.Equal("land.position.operation-1.first-player.movement-and-combat.combat.position-determination", state.SequencePosition.PositionId);
            }
            Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(bytes, trace.Created, trace.Request));
            if (cut > 10) Assert.ThrowsAny<JsonException>(() => CampaignEventSerializer.Deserialize(events[^1]));
        }
    }

    [Fact]
    public void AssignedFrozenFamiliesHaveExactExhaustiveHistoryIdentityCoverage()
    {
        using var fixture = Fixture("combat-inherited-snapshot-v1.json");
        var expected = fixture.RootElement.GetProperty("roots").EnumerateArray().Where(r =>
            r.GetProperty("family").GetString() is "E1" or "E2G1" or "G2").Select(Identity).ToHashSet(StringComparer.Ordinal);
        var actual = new HashSet<string>(StringComparer.Ordinal);
        foreach (var values in Cases())
        {
            var trace = Trace((string)values[0], (int)values[1]);
            for (var cut = 10; cut <= trace.Events.Length; cut++) actual.Add(Identity(trace.Created, trace.Events.Take(cut)));
        }
        Assert.Equal(48, expected.Count);
        Assert.Equal(expected.Order(StringComparer.Ordinal), actual.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void SameTagActualReactionTailAdmitsThroughStrictReactionEffects(string side)
    {
        var (request, created, opening, weather, stage, reserve, moves) = Moved(side);
        var state = CampaignCombatReactionTrigger.Replay(request, created, opening, [weather], stage, reserve, moves, []);
        var reaction = CampaignCombatReactionTrigger.Apply(request, created, opening, [weather], stage, reserve, moves, [], CampaignCombatReactionTrigger.Command(state));
        var history = opening.Append(weather).Concat(stage).Concat(reserve).Concat(moves).Append(reaction.EventBytes).ToArray();
        using var node = JsonDocument.Parse(reaction.EventBytes);
        Assert.Equal("element-moved", node.RootElement.GetProperty("eventType").GetString());
        Assert.Equal(4, node.RootElement.GetProperty("contractVersion").GetInt32());
        Assert.ThrowsAny<JsonException>(() => CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, reserve, moves.Append(reaction.EventBytes).ToArray()));
        Assert.Equal(13, CampaignCombatHistoryReplay.Replay(request, created, history).Projection.StateVersion);
    }

    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void EveryNewOccurrenceRejectsMissingReorderedDuplicateForgedAndNoncanonicalEvents(string side)
    {
        var trace = Trace(side, 2);
        for (var index = 10; index < trace.Events.Length; index++)
        {
            if (index < trace.Events.Length - 1)
            {
                Reject(trace, trace.Events.Where((_, i) => i != index));
                var reordered = trace.Events.ToArray();
                (reordered[index], reordered[index + 1]) = (reordered[index + 1], reordered[index]);
                Reject(trace, reordered);
            }
            Reject(trace, trace.Events.Take(index).Concat([trace.Events[index], trace.Events[index]]));
            foreach (var bytes in Mutations(trace.Events[index])) Reject(trace, trace.Events.Take(index).Append(bytes));
            if (index < trace.Events.Length - 1) Reject(trace, trace.Events.Take(index).Append(trace.Events[^1]));
        }
        Reject(trace, trace.Events.Append(Encoding.UTF8.GetBytes("{}")));
        Reject(trace, trace.Events.Append(trace.Events[10]));
        var foreign = Trace(side == "axis" ? "commonwealth" : "axis", 2);
        for (var index = 10; index < trace.Events.Length; index++)
            Reject(trace, trace.Events.Take(index).Append(foreign.Events[index]));
        // Valid events from another route length cannot replace the captured stop/end authority.
        var later = Trace(side, 6);
        Reject(trace, trace.Events.Take(12).Concat(later.Events.Skip(16)));
        // Stop/resolve remain mandatory for empty cohorts; G2 cannot replace any lifecycle gate.
        for (var cut = 10; cut < trace.Events.Length - 1; cut++) Reject(trace, trace.Events.Take(cut).Append(trace.Events[^1]));
    }

    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void CanonicallyResignedTypedEffectsAndAuthorityRejectAgainstActualPredecessors(string side)
    {
        var (request, created, opening, weather, stage, reserve, moves) = Moved(side);
        var trace = Trace(side, 1);
        var movement = CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, reserve, moves);
        var acceptedMove = Assert.Single(movement.Events);
        foreach (var forged in new[] {
            acceptedMove with { Cost = new CapabilityPointAmount(3, 1) },
            acceptedMove with { Input = acceptedMove.Input with { Actor = CampaignOpeningPreambleActor.System } },
        })
        {
            var bytes = CampaignCombatInheritedMovementCodec.SerializeEvent(forged);
            Assert.NotEqual(moves[0], bytes);
            Assert.NotEqual(acceptedMove.ReceiptId, forged.ReceiptId);
            Assert.Equal(forged.Input, CampaignCombatInheritedMovementCodec.ReadEventInput(bytes));
            Reject(trace, trace.Events.Take(10).Append(bytes));
        }
        var lifecycle = Lifecycle(request, created, opening, weather, stage, reserve, moves);
        var state = CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves, lifecycle);
        for (var index = 0; index < state.Events.Count; index++)
        {
            var accepted = state.Events[index];
            var forgedActor = accepted.Input.Actor == CampaignOpeningPreambleActor.System ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.System;
            var effects = new CampaignCombatMovementLifecycleEvent(accepted.Before, accepted.Input,
                accepted.SequencePosition, accepted.BreakdownFlow,
                index == 0 ? accepted.InterruptContext! with { CycleId = "sha256:" + new string('0', 64) } : accepted.InterruptContext,
                index == 2 ? accepted.EndLocations.Select(location => location with { LocationId = side + "-supply" }) : accepted.EndLocations,
                accepted.ExcludedUnits);
            // Resolution has no end/interrupt payload, so forge its restored position instead.
            if (index == 1) effects = new(accepted.Before, accepted.Input, accepted.Before.SequencePosition,
                accepted.BreakdownFlow, accepted.InterruptContext, accepted.EndLocations, accepted.ExcludedUnits);
            var authority = new CampaignCombatMovementLifecycleEvent(accepted.Before, accepted.Input with { Actor = forgedActor },
                accepted.SequencePosition, accepted.BreakdownFlow, accepted.InterruptContext, accepted.EndLocations, accepted.ExcludedUnits);
            foreach (var forged in new[] { effects, authority })
            {
                var bytes = CampaignCombatMovementLifecycleCodec.SerializeEvent(forged);
                Assert.NotEqual(lifecycle[index], bytes);
                Assert.NotEqual(accepted.ReceiptId, forged.ReceiptId);
                Assert.Equal(forged.Input, CampaignCombatMovementLifecycleCodec.ReadEventInput(bytes));
                Reject(trace, trace.Events.Take(11 + index).Append(bytes));
            }
        }
        var before = CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, reserve, moves, lifecycle, []);
        var completion = CampaignCombatBreakdownCompletion.Apply(request, created, opening, [weather], stage, reserve, moves, lifecycle, [], CampaignCombatBreakdownCompletion.Command(before)).State.Completion!;
        foreach (var forged in new[] {
            completion with { SequencePosition = before.SequencePosition },
            completion with { Input = completion.Input with { Actor = CampaignOpeningPreambleActor.Axis } },
        })
        {
            var bytes = CampaignCombatBreakdownCompletionCodec.SerializeEvent(forged);
            Assert.NotEqual(completion.ReceiptId, forged.ReceiptId);
            Assert.Equal(forged.Input, CampaignCombatBreakdownCompletionCodec.ReadEventInput(bytes));
            Reject(trace, trace.Events.Take(14).Append(bytes));
        }
    }

    [Fact]
    public void ReserveDesignationAtCountTenDoesNotAuthorizeMovementAndLegacyReaderRemainsStrict()
    {
        var ordinary = Moved();
        var designated = Ready(designated: true);
        var baseEvents = designated.Opening.Append(designated.Weather).Concat(designated.Stage).ToArray();
        var prefix = baseEvents.Append(designated.Reserve[0]).ToArray();
        var result = CampaignCombatHistoryReplay.Replay(designated.Request, designated.Created, prefix);
        Assert.Null(Assert.IsType<CampaignCombatHistoryProjection.ReserveOpening>(result.Projection).State.Completion);
        foreach (var reserve in new[] { designated.Reserve.Take(1), designated.Reserve.AsEnumerable() })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatHistoryReplay.Replay(designated.Request, designated.Created,
                baseEvents.Concat(reserve).Concat(ordinary.Moves).ToArray()));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatReserveOpening.Replay(ordinary.Request, ordinary.Created, ordinary.Opening,
            [ordinary.Weather], ordinary.Stage, ordinary.Reserve.Concat(ordinary.Moves).ToArray()));
    }

    [Fact]
    public void CallerAndResultBuffersCannotMutateAnyNewTypedProjection()
    {
        var trace = Trace("axis", 1);
        for (var cut = 11; cut <= trace.Events.Length; cut++)
        {
            var created = trace.Created.ToArray();
            var events = trace.Events.Take(cut).Select(bytes => bytes.ToArray()).ToArray();
            var result = CampaignCombatHistoryReplay.Replay(trace.Request, created, events);
            var expected = Serialize(result.Projection);
            foreach (var bytes in events.Append(created).Concat(result.History.Events).Append(result.History.Created)) Array.Fill(bytes, (byte)0);
            Assert.Equal(expected, Serialize(result.Projection));
            Assert.Equal(expected, Serialize(CampaignCombatHistoryReplay.Replay(trace.Request, result.History.Created, result.History.Events).Projection));
        }
    }

    private static void CheckH0(JsonElement row, byte[] bytes, CampaignCombatHistoryProjection projection)
    {
        var root = JsonNode.Parse(row.GetProperty("canonicalJson").GetString()!)!;
        var actual = JsonNode.Parse(bytes)!;
        Assert.Equal(root["chroniclePrefix"]!.GetValue<string>(), projection.Prefix);
        Assert.True(JsonNode.DeepEquals(root["commandReceipts"], actual["receipts"]));
        Assert.True(JsonNode.DeepEquals(root["currentPosition"]!["sequencePosition"], actual["sequencePosition"]));
        foreach (var field in new[] { "campaignId", "rulesetHash", "stateVersion", "initiativeHolder", "operationStageOrders", "operationStageWeather", "randomState", "world" })
            Assert.True(JsonNode.DeepEquals(root[field], actual[field]), field);
        var cycle = root["cycleState"]!;
        foreach (var field in new[] { "firstActingSide", "members", "cycle", "cycleId", "openingBaseHash", "completionReceiptId", "sequencePosition", "tracks", "actualProgressRefs", "interruptContext", "movementEnd", "breakdownCompletionReceiptId" })
        {
            if (!actual.AsObject().ContainsKey(field) && field is "tracks" or "actualProgressRefs") Assert.Empty(cycle[field]!.AsArray());
            else Assert.True(JsonNode.DeepEquals(cycle[field], actual[field]), field);
        }
        if (projection is CampaignCombatHistoryProjection.ReserveOpening) Assert.Equal("idle", root["breakdownFlow"]!["kind"]!.GetValue<string>());
        else Assert.True(JsonNode.DeepEquals(root["breakdownFlow"], actual["breakdownFlow"]));
        Assert.Null(root["reactionWindow"]);
        Assert.Null(root["combatState"]);
    }

    private sealed record TraceData(CampaignCombatCreationRequest Request, byte[] Created, byte[][] Events, byte[][] States);
    private static TraceData Trace(string side, int count)
    {
        var (request, created, opening, weather, stage, reserve, moves) = Moved(side, count);
        var events = opening.Append(weather).Concat(stage).Concat(reserve).ToList();
        var states = new List<byte[]> { CampaignCombatReserveOpeningCodec.SerializeState(CampaignCombatReserveOpening.Replay(request, created, opening, [weather], stage, reserve)) };
        for (var cut = 1; cut <= moves.Length; cut++)
        {
            events.Add(moves[cut - 1]);
            states.Add(CampaignCombatInheritedMovementCodec.SerializeState(CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, reserve, moves.Take(cut).ToArray())));
        }
        var lifecycle = Lifecycle(request, created, opening, weather, stage, reserve, moves);
        for (var cut = 1; cut <= lifecycle.Length; cut++)
        {
            events.Add(lifecycle[cut - 1]);
            states.Add(CampaignCombatMovementLifecycleCodec.SerializeState(CampaignCombatMovementLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves, lifecycle.Take(cut).ToArray())));
        }
        var before = CampaignCombatBreakdownCompletion.Replay(request, created, opening, [weather], stage, reserve, moves, lifecycle, []);
        var end = CampaignCombatBreakdownCompletion.Apply(request, created, opening, [weather], stage, reserve, moves, lifecycle, [], CampaignCombatBreakdownCompletion.Command(before));
        events.Add(end.EventBytes); states.Add(CampaignCombatBreakdownCompletionCodec.SerializeState(end.State));
        return new(request, created, events.ToArray(), states.ToArray());
    }
    private static byte[] Serialize(CampaignCombatHistoryProjection projection) => projection switch
    {
        CampaignCombatHistoryProjection.ReserveOpening p => CampaignCombatReserveOpeningCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.Movement p => CampaignCombatInheritedMovementCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.MovementLifecycle p => CampaignCombatMovementLifecycleCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.BreakdownCompletion p => CampaignCombatBreakdownCompletionCodec.SerializeState(p.State),
        _ => throw new InvalidOperationException(),
    };
    private static string Identity(JsonElement row) => row.GetProperty("creationHash").GetString() + ":" + string.Join(',', row.GetProperty("eventHashes").EnumerateArray().Select(v => v.GetString()));
    private static string Identity(byte[] created, IEnumerable<byte[]> events) => Digest(created) + ":" + string.Join(',', events.Select(Digest));
    private static void Reject(TraceData trace, IEnumerable<byte[]> events) => Assert.ThrowsAny<JsonException>(() => CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, events.ToArray()));
    private static JsonDocument Fixture(string name) => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", name)));
    private static void CheckGolden(JsonElement golden, byte[] bytes)
    {
        Assert.Equal(golden.GetProperty("bytes").GetInt32(), bytes.Length);
        Assert.Equal(golden.GetProperty("sha256").GetString(), Digest(bytes));
    }
    private static void CheckGolden(JsonElement golden, int index, byte[] bytes)
    {
        Assert.Equal(golden.GetProperty("bytes")[index].GetInt32(), bytes.Length);
        Assert.Equal(golden.GetProperty("sha256")[index].GetString(), Digest(bytes));
    }
    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
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
}
