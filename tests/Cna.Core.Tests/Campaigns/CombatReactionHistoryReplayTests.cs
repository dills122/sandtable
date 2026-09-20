using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatReactionHistoryReplayTests
{
    internal static IEnumerable<(CampaignCombatCreationRequest Request, byte[] Created, byte[][] Events)> SnapshotHistories()
    {
        foreach (var values in Cases())
        {
            var trace = Trace((string)values[0], (string)values[1]);
            for (var cut = 11; cut <= trace.Events.Length; cut++)
                yield return (trace.Request, trace.Created.ToArray(), trace.Events.Take(cut).Select(bytes => bytes.ToArray()).ToArray());
        }
    }

    public static IEnumerable<object[]> Cases()
    {
        foreach (var side in new[] { "axis", "commonwealth" })
            foreach (var fork in new[] { "lifecycle", "decline", "unavailable", "timeout", "active-unavailable", "active-timeout", "second" })
                yield return [side, fork];
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryReactionForkAndIntermediateCutMatchesStrictReadersAndFrozenRoots(string side, string fork)
    {
        var trace = Trace(side, fork);
        using var fixture = Fixture("combat-inherited-snapshot-v1.json");
        for (var cut = 11; cut <= trace.Events.Length; cut++)
        {
            var events = trace.Events.Take(cut).ToArray();
            var result = CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, events);
            var bytes = Serialize(result.Projection);
            Assert.Equal(trace.States[cut - 11], bytes);
            Assert.Equal(cut + 1, result.Projection.StateVersion);
            Assert.Equal(cut, result.Projection.Receipts.Count);
            Assert.Equal(events.Select(Digest), result.History.Events.Select(Digest));
            var rows = fixture.RootElement.GetProperty("roots").EnumerateArray().Where(r => Identity(r) == Identity(trace.Created, events)).ToArray();
            Assert.NotEmpty(rows);
            foreach (var row in rows) CheckH0(row, bytes, result.Projection);
            Assert.ThrowsAny<JsonException>(() => CampaignCreationSnapshotV12Codec.Deserialize(bytes, trace.Created, trace.Request));
        }
    }

    [Fact]
    public void FrozenReactionInventoryContainsExactlyFiftyVectorsAndThirtyFourHistories()
    {
        using var fixture = Fixture("combat-inherited-snapshot-v1.json");
        var rows = fixture.RootElement.GetProperty("roots").EnumerateArray().Where(r => r.GetProperty("family").GetString() is "F1" or "F2" or "F3" or "F4" or "F5" or "F6").ToArray();
        var expected = rows.Select(Identity).ToHashSet(StringComparer.Ordinal);
        var actual = new HashSet<string>(StringComparer.Ordinal);
        foreach (var values in Cases())
        {
            var trace = Trace((string)values[0], (string)values[1]);
            for (var cut = 11; cut <= trace.Events.Length; cut++) actual.Add(Identity(trace.Created, trace.Events.Take(cut)));
        }
        Assert.Equal(50, rows.Length);
        Assert.Equal(34, expected.Count);
        var ordinary = fixture.RootElement.GetProperty("roots").EnumerateArray().Where(r => r.GetProperty("family").GetString() is "E1" or "E2G1" or "G2").Select(Identity).ToHashSet(StringComparer.Ordinal);
        Assert.Equal(2, actual.Intersect(ordinary).Count());
        Assert.Equal(32, actual.Except(ordinary).Count());
        Assert.Equal(expected.Order(StringComparer.Ordinal), actual.Order(StringComparer.Ordinal));
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryOccurrenceRejectsMissingDuplicateReorderedForeignAndUnsupportedTails(string side, string fork)
    {
        var trace = Trace(side, fork);
        var foreign = Trace(side == "axis" ? "commonwealth" : "axis", fork);
        for (var index = 11; index < trace.Events.Length; index++)
        {
            Reject(trace, trace.Events.Take(index).Append(trace.Events[index]).Concat(trace.Events.Skip(index)));
            Reject(trace, trace.Events.Take(index).Concat(foreign.Events.Skip(index)));
            foreach (var bytes in (fork == "lifecycle" || index >= (fork.StartsWith("active-", StringComparison.Ordinal) || fork == "second" ? 13 : 12) ? Mutations(trace.Events[index]) : []).Where(b => !b.SequenceEqual(trace.Events[index])))
                Reject(trace, trace.Events.Take(index).Append(bytes));
            if (index + 1 < trace.Events.Length)
            {
                Reject(trace, trace.Events.Take(index).Concat(trace.Events.Skip(index + 1)).Append(trace.Events[index]));
                Reject(trace, trace.Events.Take(index).Concat(trace.Events.Skip(index + 1)));
                Reject(trace, trace.Events.Take(index).Append(trace.Events[index + 1]).Append(trace.Events[index]));
            }
        }
        Reject(trace, trace.Events.Append(Encoding.UTF8.GetBytes("{}")));
        Reject(trace, trace.Events.Append(trace.Events[10]));
        // Neither explicit closure nor stop resolution can be skipped, even for empty cohorts.
        for (var cut = 12; cut < trace.Events.Length - 1; cut++)
            Reject(trace, trace.Events.Take(cut).Append(trace.Events[^1]));
        foreach (var other in new[] { "lifecycle", "decline", "unavailable", "timeout", "active-unavailable", "active-timeout", "second" })
        {
            var sibling = Trace(side, other);
            var common = trace.Events.Zip(sibling.Events).TakeWhile(pair => pair.First.SequenceEqual(pair.Second)).Count();
            if (common == Math.Min(trace.Events.Length, sibling.Events.Length)) continue;
            Reject(trace, trace.Events.Take(common + 1).Concat(sibling.Events.Skip(common)));
            if (common + 1 < sibling.Events.Length)
                Reject(trace, trace.Events.Take(common + 1).Concat(sibling.Events.Skip(common + 1)));
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void RetainedAndReturnedBuffersCannotChangeAnyReactionCut(string side, string fork)
    {
        var trace = Trace(side, fork);
        for (var cut = 12; cut <= trace.Events.Length; cut++)
        {
            var created = trace.Created.ToArray();
            var events = trace.Events.Take(cut).Select(b => b.ToArray()).ToArray();
            var result = CampaignCombatHistoryReplay.Replay(trace.Request, created, events);
            var expected = Serialize(result.Projection);
            foreach (var bytes in events.Append(created).Concat(result.History.Events).Append(result.History.Created)) Array.Fill(bytes, (byte)0);
            Assert.Equal(expected, Serialize(result.Projection));
            Assert.Equal(expected, Serialize(CampaignCombatHistoryReplay.Replay(trace.Request, result.History.Created, result.History.Events).Projection));
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void CanonicallyResignedEffectsAndActorsRejectAtEveryReactionTransition(string side, string fork)
    {
        var trace = Trace(side, fork);
        for (var cut = 12; cut <= trace.Events.Length; cut++)
        {
            var projection = CampaignCombatHistoryReplay.Replay(trace.Request, trace.Created, trace.Events.Take(cut).ToArray()).Projection;
            foreach (var bytes in Forgeries(projection))
            {
                Assert.NotEqual(trace.Events[cut - 1], bytes);
                using var accepted = JsonDocument.Parse(trace.Events[cut - 1]);
                using var forged = JsonDocument.Parse(bytes);
                Assert.NotEqual(accepted.RootElement.GetProperty("receiptId").GetString(), forged.RootElement.GetProperty("receiptId").GetString());
                Reject(trace, trace.Events.Take(cut - 1).Append(bytes));
            }
        }
    }

    [Theory]
    [InlineData("axis")]
    [InlineData("commonwealth")]
    public void WindowHintsAndEventTagsCannotAuthorizeOrHideEffects(string side)
    {
        var trace = Trace(side, "second");
        foreach (var index in new[] { 10, 11 })
        {
            var original = JsonNode.Parse(trace.Events[index])!.AsObject();
            var changed = original.DeepClone().AsObject();
            changed["openedReactionWindow"] = index == 10 ? new JsonObject() : null;
            Reject(trace, trace.Events.Take(index).Append(Encoding.UTF8.GetBytes(changed.ToJsonString())));
            changed.Remove("openedReactionWindow");
            Reject(trace, trace.Events.Take(index).Append(Encoding.UTF8.GetBytes(changed.ToJsonString())));
            var raw = Encoding.UTF8.GetString(trace.Events[index]);
            var hint = original["openedReactionWindow"]?.ToJsonString() ?? "null";
            foreach (var value in new[] { "null", "{}", hint })
            {
                Reject(trace, trace.Events.Take(index).Append(Encoding.UTF8.GetBytes(raw[..^1] + ",\"openedReactionWindow\":" + value + "}")));
                Reject(trace, trace.Events.Take(index).Append(Encoding.UTF8.GetBytes("{\"openedReactionWindow\":" + value + "," + raw[1..])));
            }
        }
        foreach (var fork in new[] { "lifecycle", "decline", "active-timeout", "second" })
        {
            var current = Trace(side, fork);
            for (var index = 12; index < current.Events.Length; index++)
                foreach (var tag in new[] { "element-moved", "reaction-window-closed", "reacting-element-moved", "reaction-participant-completed" })
                {
                    var node = JsonNode.Parse(current.Events[index])!;
                    if (node["eventType"]!.GetValue<string>() == tag) continue;
                    node["eventType"] = tag;
                    Reject(current, current.Events.Take(index).Append(Encoding.UTF8.GetBytes(node.ToJsonString())));
                }
        }
    }

    private static CampaignOpeningPreambleActor Other(CampaignOpeningPreambleActor actor) => actor == CampaignOpeningPreambleActor.System
        ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.System;

    private static IEnumerable<byte[]> Forgeries(CampaignCombatHistoryProjection projection)
    {
        if (projection is CampaignCombatHistoryProjection.ReactionTrigger trigger)
        {
            var accepted = trigger.State.Trigger!;
            foreach (var forged in new[] { accepted with { Cost = new CapabilityPointAmount(3, 1) }, accepted with { Input = accepted.Input with { Actor = Other(accepted.Input.Actor) } } })
            {
                var bytes = CampaignCombatReactionTriggerCodec.SerializeEvent(forged);
                Assert.Equal(forged.Input, CampaignCombatInheritedMovementCodec.ReadEventInput(bytes));
                yield return bytes;
            }
        }
        if (projection is CampaignCombatHistoryProjection.ReactionLifecycle lifecycle)
        {
            var accepted = lifecycle.State.Events[^1];
            foreach (var forged in new[] { accepted with { Flow = new CampaignBreakdownFlow.Idle() }, accepted with { Input = accepted.Input with { Actor = Other(accepted.Input.Actor) } } })
            {
                var bytes = CampaignCombatReactionLifecycleCodec.SerializeEvent(forged);
                Assert.Equal(forged.Input, CampaignCombatReactionLifecycleCodec.ReadEventInput(bytes));
                yield return bytes;
            }
        }
        if (projection is CampaignCombatHistoryProjection.ReactionClosure closure)
        {
            var accepted = closure.State.Accepted!;
            foreach (var forged in new[] { accepted with { Flow = accepted.Flow with { Route = accepted.Flow.Route.AtLocation("axis-supply") } }, accepted with { Input = accepted.Input with { Actor = Other(accepted.Input.Actor) } } })
            {
                var bytes = CampaignCombatReactionClosureCodec.SerializeEvent(forged);
                Assert.Equal(forged.Input, CampaignCombatReactionClosureCodec.ReadEventInput(bytes));
                yield return bytes;
            }
        }
        if (projection is CampaignCombatHistoryProjection.ReactionFallback fallback)
        {
            var accepted = fallback.State.Events[^1];
            foreach (var forged in new[] { accepted with { Flow = new CampaignBreakdownFlow.Idle() }, accepted with { Input = accepted.Input with { Actor = Other(accepted.Input.Actor) } } })
            {
                var bytes = CampaignCombatReactionFallbackCodec.SerializeEvent(forged);
                Assert.Equal(forged.Input, CampaignCombatReactionFallbackCodec.ReadEventInput(bytes));
                yield return bytes;
            }
        }
        if (projection is CampaignCombatHistoryProjection.ReactionSecondMove secondmove)
        {
            var accepted = secondmove.State.Events[^1];
            foreach (var forged in new[] { accepted with { Flow = accepted.Flow with { ReactorRoute = null } }, accepted with { Input = accepted.Input with { Actor = Other(accepted.Input.Actor) } } })
            {
                var bytes = CampaignCombatReactionSecondMoveCodec.SerializeEvent(forged);
                Assert.Equal(forged.Input, CampaignCombatReactionSecondMoveCodec.ReadEventInput(bytes));
                yield return bytes;
            }
        }
        if (projection is CampaignCombatHistoryProjection.ReactionCompletion completion)
        {
            var accepted = completion.State.Events[^1];
            foreach (var forged in new[] { accepted with { Flow = new CampaignBreakdownFlow.Idle() }, accepted with { Input = accepted.Input with { Actor = Other(accepted.Input.Actor) } } })
            {
                var bytes = CampaignCombatReactionCompletionCodec.SerializeEvent(forged);
                Assert.Equal(forged.Input, CampaignCombatReactionCompletionCodec.ReadEventInput(bytes));
                yield return bytes;
            }
        }
    }

    private static void CheckH0(JsonElement row, byte[] bytes, CampaignCombatHistoryProjection projection)
    {
        var root = JsonNode.Parse(row.GetProperty("canonicalJson").GetString()!)!;
        var actual = JsonNode.Parse(bytes)!;
        Assert.Equal(root["chroniclePrefix"]!.GetValue<string>(), projection.Prefix);
        Assert.True(JsonNode.DeepEquals(root["commandReceipts"], actual["receipts"]));
        foreach (var field in new[] { "campaignId", "rulesetHash", "stateVersion", "initiativeHolder", "operationStageOrders", "operationStageWeather", "randomState", "world", "breakdownFlow", "reactionWindow" })
            Assert.True(JsonNode.DeepEquals(root[field], actual[field]), field);
        if (projection is CampaignCombatHistoryProjection.Movement)
            Assert.True(JsonNode.DeepEquals(root["currentPosition"]!["sequencePosition"], actual["sequencePosition"]));
        else Assert.True(JsonNode.DeepEquals(root["currentPosition"], actual["currentPosition"]));
        foreach (var field in new[] { "firstActingSide", "members", "cycle", "cycleId", "openingBaseHash", "completionReceiptId", "sequencePosition", "tracks", "actualProgressRefs", "interruptContext", "movementEnd", "breakdownCompletionReceiptId" })
            Assert.True(JsonNode.DeepEquals(root["cycleState"]![field], actual[field]), field);
        Assert.Null(root["combatState"]);
    }

    private sealed record TraceData(CampaignCombatCreationRequest Request, byte[] Created, byte[][] Events, byte[][] States);
    private static TraceData Trace(string side, string fork)
    {
        var (request, created, opening, weather, stage, reserve, moves) = Moved(side);
        var events = opening.Append(weather).Concat(stage).Concat(reserve).Concat(moves).ToList();
        var states = new List<byte[]> { CampaignCombatInheritedMovementCodec.SerializeState(CampaignCombatInheritedMovement.Replay(request, created, opening, [weather], stage, reserve, moves)) };
        var trigger = CampaignCombatReactionTrigger.Replay(request, created, opening, [weather], stage, reserve, moves, []);
        var triggered = CampaignCombatReactionTrigger.Apply(request, created, opening, [weather], stage, reserve, moves, [], CampaignCombatReactionTrigger.Command(trigger));
        byte[][] triggers = [triggered.EventBytes];
        Add(triggered.EventBytes, CampaignCombatReactionTriggerCodec.SerializeState(triggered.State), "trigger", "event", "state");
        if (fork is "decline" or "unavailable" or "timeout")
        {
            var before = CampaignCombatReactionClosure.Replay(request, created, opening, [weather], stage, reserve, moves, triggers, []);
            var after = CampaignCombatReactionClosure.Apply(request, created, opening, [weather], stage, reserve, moves, triggers, [], CampaignCombatReactionClosure.Command(before, Kind(fork)));
            Add(after.EventBytes, CampaignCombatReactionClosureCodec.SerializeState(after.State), "closure", "event", "state-after", Kind(fork));
        }
        else
        {
            var participants = new List<byte[]>();
            var before = CampaignCombatReactionLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves, triggers, participants);
            var after = CampaignCombatReactionLifecycle.Apply(request, created, opening, [weather], stage, reserve, moves, triggers, participants, CampaignCombatReactionLifecycle.Command(before));
            participants.Add(after.EventBytes);
            Add(after.EventBytes, CampaignCombatReactionLifecycleCodec.SerializeState(after.State), "lifecycle", "event-1", "state-1");
            if (fork == "lifecycle")
            {
                for (var i = 2; i <= 4; i++)
                {
                    before = CampaignCombatReactionLifecycle.Replay(request, created, opening, [weather], stage, reserve, moves, triggers, participants);
                    after = CampaignCombatReactionLifecycle.Apply(request, created, opening, [weather], stage, reserve, moves, triggers, participants, CampaignCombatReactionLifecycle.Command(before));
                    participants.Add(after.EventBytes);
                    Add(after.EventBytes, CampaignCombatReactionLifecycleCodec.SerializeState(after.State), "lifecycle", $"event-{i}", $"state-{i}");
                }
            }
            else if (fork.StartsWith("active-", StringComparison.Ordinal))
            {
                var fallback = new List<byte[]>();
                for (var i = 1; i <= 2; i++)
                {
                    var state = CampaignCombatReactionFallback.Replay(request, created, opening, [weather], stage, reserve, moves, triggers, participants, fallback);
                    var result = CampaignCombatReactionFallback.Apply(request, created, opening, [weather], stage, reserve, moves, triggers, participants, fallback,
                        CampaignCombatReactionFallback.Command(state, i == 1 ? Kind(fork[7..]) : "resolve-breakdown-stop"));
                    fallback.Add(result.EventBytes);
                    Add(result.EventBytes, CampaignCombatReactionFallbackCodec.SerializeState(result.State), "active-fallback", $"event-{i}", $"state-{i}", Kind(fork[7..]));
                }
            }
            else
            {
                var second = CampaignCombatReactionSecondMove.Replay(request, created, opening, [weather], stage, reserve, moves, triggers, participants, []);
                var result = CampaignCombatReactionSecondMove.Apply(request, created, opening, [weather], stage, reserve, moves, triggers, participants, [], CampaignCombatReactionSecondMove.Command(second));
                byte[][] secondMoves = [result.EventBytes];
                Add(result.EventBytes, CampaignCombatReactionSecondMoveCodec.SerializeState(result.State), "second-move", "event", "state-1");
                var completion = new List<byte[]>();
                for (var i = 1; i <= 3; i++)
                {
                    var state = CampaignCombatReactionCompletion.Replay(request, created, opening, [weather], stage, reserve, moves, triggers, participants, secondMoves, completion);
                    var completed = CampaignCombatReactionCompletion.Apply(request, created, opening, [weather], stage, reserve, moves, triggers, participants, secondMoves, completion, CampaignCombatReactionCompletion.Command(state));
                    completion.Add(completed.EventBytes);
                    Add(completed.EventBytes, CampaignCombatReactionCompletionCodec.SerializeState(completed.State), "movement-completion", $"event-{i}", $"state-{i}");
                }
            }
        }
        return new(request, created, events.ToArray(), states.ToArray());
        void Add(byte[] item, byte[] state, string family, string eventKey, string stateKey, string? kind = null)
        {
            using var fixture = Fixture($"combat-inherited-reaction-{family}-v1.json");
            var golden = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("name").GetString()!.StartsWith(side + "-", StringComparison.Ordinal) &&
                (kind is null || c.GetProperty("kind").GetString() == kind)).GetProperty("goldens");
            CheckGolden(golden.GetProperty(eventKey), item); CheckGolden(golden.GetProperty(stateKey), state);
            events.Add(item); states.Add(state);
        }
    }
    private static string Kind(string fork) => fork == "decline" ? "decline-reaction-window" : "close-reaction-window-" + (fork == "unavailable" ? "scripted-unavailable" : "timeout");
    private static byte[] Serialize(CampaignCombatHistoryProjection projection) => projection switch
    {
        CampaignCombatHistoryProjection.Movement p => CampaignCombatInheritedMovementCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.ReactionTrigger p => CampaignCombatReactionTriggerCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.ReactionLifecycle p => CampaignCombatReactionLifecycleCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.ReactionClosure p => CampaignCombatReactionClosureCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.ReactionFallback p => CampaignCombatReactionFallbackCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.ReactionSecondMove p => CampaignCombatReactionSecondMoveCodec.SerializeState(p.State),
        CampaignCombatHistoryProjection.ReactionCompletion p => CampaignCombatReactionCompletionCodec.SerializeState(p.State),
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
    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
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
