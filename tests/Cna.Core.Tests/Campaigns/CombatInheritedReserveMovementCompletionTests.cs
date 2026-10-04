using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Bridge = Cna.Core.Campaigns.CampaignCombatInheritedReserveRelease;
using Codec = Cna.Core.Campaigns.CampaignCombatInheritedReserveMovementCompletionCodec;
using Completion = Cna.Core.Campaigns.CampaignCombatInheritedReserveMovementCompletion;
using Control = Cna.Core.Campaigns.CampaignCombatInheritedCycleControl;
using MoveCodec = Cna.Core.Campaigns.CampaignCombatInheritedReserveMovementCodec;
using Movement = Cna.Core.Campaigns.CampaignCombatInheritedReserveMovement;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatInheritedReserveMovementCompletionTests
{
    private static readonly string[] CompletionKinds = ["stop-element-movement", "resolve-breakdown-stop", "complete-movement-segment"];
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void BothOwnerCompletionTracesMatchFrozenContract(string choice)
    {
        var movement = Repeat(choice); var initialMove = Movement.Replay(movement, [], []);
        var move = Movement.Command(initialMove, (choice == "act-first" ? "axis" : "commonwealth") + "-rear");
        var moved = Movement.Apply(movement, [], [], move);
        var source = new CombatInheritedReserveMovementCompletionSource(movement, [move], [moved.EventBytes]);
        var inputs = new List<ReserveMovementCompletionInput>(); var events = new List<byte[]>();
        var state = Completion.Replay(source, inputs, events);
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Campaigns", "Fixtures", "combat-inherited-reserve-movement-completion-v1.json")));
        var golden = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("actor").GetString() ==
            (choice == "act-first" ? "axis" : "commonwealth")).GetProperty("golden");
        var basis = Codec.SerializeBase(state.Basis);
        Assert.Equal(golden.GetProperty("baseBytes").GetInt32(), basis.Length);
        Assert.Equal(golden.GetProperty("baseHash").GetString(), Hash(basis));
        Assert.Equal(basis, Codec.SerializeBase(Codec.ReadBase(basis, source)));
        for (var cut = 0; cut <= 3; cut++)
        {
            var bytes = Codec.SerializeState(state); var frame = golden.GetProperty("frames")[cut];
            Assert.Equal(frame.GetProperty("bytes").GetInt32(), bytes.Length);
            Assert.Equal(frame.GetProperty("sha256").GetString(), Hash(bytes));
            Assert.Equal(bytes, Codec.SerializeState(Codec.ReadState(bytes, source, inputs, events)));
            Assert.Equal(28 + cut, state.StateVersion);
            if (cut == 3) break;
            var input = Completion.Command(state); var result = Completion.Apply(source, inputs, events, input, bytes);
            Assert.False(result.Duplicate);
            var expected = golden.GetProperty("events")[cut];
            Assert.Equal(expected.GetProperty("bytes").GetInt32(), result.EventBytes.Length);
            Assert.Equal(expected.GetProperty("sha256").GetString(), Hash(result.EventBytes));
            inputs.Add(input); events.Add(result.EventBytes); state = result.State;
            var retry = Completion.Apply(source, inputs, events, input);
            Assert.True(retry.Duplicate); Assert.Equal(result.EventBytes, retry.EventBytes);
        }
        Assert.Equal(golden.GetProperty("terminalPrefix").GetString(), state.Prefix);
        Assert.Equal(golden.GetProperty("completionReceiptId").GetString(), state.MovementEnd!.CompletionReceiptId);
        Assert.Equal("expired", state.ReleaseMember.History.NextMovement!.Status);
        Assert.Equal(state.MovementEnd.CompletionReceiptId, state.ReleaseMember.History.NextMovement.CompletionReceiptId);
    }

    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void EveryCutPreservesWorldAndBindsStopProofAndRetries(string choice)
    {
        var source = CompletionSource(choice); var inputs = new List<ReserveMovementCompletionInput>(); var events = new List<byte[]>();
        var first = Completion.Replay(source, inputs, events); var before = JsonNode.Parse(Codec.SerializeState(first))!;
        var state = first;
        for (var cut = 0; cut < 3; cut++)
        {
            var input = Completion.Command(state);
            var result = Completion.Apply(source, inputs, events, input);
            inputs.Add(input); events.Add(result.EventBytes); state = result.State;
            var after = JsonNode.Parse(Codec.SerializeState(state))!;
            foreach (var key in new[] { "cycle", "world", "randomState", "attackHistory", "tracks" })
                Assert.True(JsonNode.DeepEquals(before[key], after[key]), key);
            Assert.Equal(new CapabilityPointAmount(2, 1), state.ReleaseMember.SpentCp);
            var e = state.Basis.Movement.World.Elements.Single(e => e.ElementId == state.ReleaseMember.Unit.ElementId);
            Assert.Equal(10, e.Ammunition.Points); Assert.Equal(10, e.Components.Sum(c => c.CurrentToe)); Assert.Equal(0, e.OperationalState.CohesionLevel);
            if (cut == 0)
            {
                Assert.Equal("land.position.breakdown-stop", state.Position.PositionId);
                Assert.Equal(first.Position, state.InterruptContext!.SequencePosition);
                Assert.Equal(first.Basis.Movement.Tracks.Single().Unit, state.Stop!.Track.Unit);
                Assert.Equal(first.Basis.Movement.Tracks.Single().Route, state.Stop.Track.Route);
                Assert.Equal(first.Basis.Movement.Receipts.Single().ReceiptId, state.Stop.MovementReceiptId);
            }
            else if (cut == 1)
            {
                Assert.Equal(first.Position, state.Position); Assert.Null(state.Stop); Assert.Null(state.InterruptContext);
                var resolved = JsonNode.Parse(result.EventBytes)!;
                Assert.Empty(resolved["checks"]!.AsArray()); Assert.Empty(resolved["createdLots"]!.AsArray());
                Assert.Equal("21.24-21.26", resolved["sources"]![0]!["locator"]!.GetValue<string>());
            }
            if (cut < 2) { Assert.Null(state.MovementEnd); Assert.Equal("pending", state.ReleaseMember.History.NextMovement!.Status); }
            foreach (var (accepted, bytes) in inputs.Zip(events))
            {
                var retry = Completion.Apply(source, inputs, events, accepted);
                Assert.True(retry.Duplicate); Assert.Equal(bytes, retry.EventBytes);
                Assert.Equal(Codec.SerializeState(state), Codec.SerializeState(retry.State));
                retry.EventBytes[0] ^= 1;
                Assert.Equal(bytes, Completion.Apply(source, inputs, events, accepted).EventBytes);
            }
        }
        Assert.Equal(first.Basis.BreakdownPosition, state.Position); Assert.Null(state.Stop); Assert.Null(state.InterruptContext);
        Assert.Equal(2, state.MovementEnd!.Ordinal);
        var retained = first.Basis.Movement.Basis.Control.Basis.Proof.Source.Base.Predecessor.MovementEnd!;
        Assert.Equal(retained.EndLocations.Select(l => l.Unit), state.MovementEnd.Locations.Select(l => l.Unit));
        foreach (var location in state.MovementEnd.Locations)
            Assert.Equal(first.Basis.Movement.World.Elements.Single(e => e.ElementId == location.Unit.ElementId).CurrentLocationId, location.LocationId);
        Assert.Equal(events[2], Completion.Apply(source, inputs, events, inputs[2]).EventBytes);
        Assert.Throws<JsonException>(() => Completion.Command(state));
        Assert.Throws<JsonException>(() => Completion.Replay(source, [.. inputs, inputs[2]], [.. events, events[2]]));
    }

    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void StaleForeignMalformedAndReorderedRequestsReject(string choice)
    {
        var source = CompletionSource(choice); var inputs = new List<ReserveMovementCompletionInput>(); var events = new List<byte[]>();
        for (var cut = 0; cut < 3; cut++)
        {
            var state = Completion.Replay(source, inputs, events); var input = Completion.Command(state); var cmd = input.Command;
            var wrongActor = input.Actor == CampaignOpeningPreambleActor.Axis ? CampaignOpeningPreambleActor.Commonwealth : CampaignOpeningPreambleActor.Axis;
            var invalid = new[] { input with { Actor = wrongActor }, input with { Command = cmd with { ExpectedPriorVersion = cmd.ExpectedPriorVersion - 1 } },
                input with { Command = cmd with { ExpectedPriorVersion = long.MaxValue } }, input with { Command = cmd with { ContractVersion = 2 } },
                input with { Command = cmd with { Kind = "fourth-event" } }, input with { Command = cmd with { BaseHash = "sha256:" + new string('0', 64) } },
                input with { Command = cmd with { CycleId = "sha256:" + new string('0', 64) } }, input with { Command = cmd with { PositionId = "land.position.foreign" } },
                input with { Command = cmd with { ActionId = "sha256:" + new string('0', 64) } }, input with { Command = cmd with { CapabilityId = "sha256:" + new string('0', 64) } } };
            foreach (var bad in invalid) Assert.Throws<JsonException>(() => Completion.Apply(source, inputs, events, bad));
            foreach (var kind in CompletionKinds.Where(k => k != cmd.Kind))
                Assert.Throws<JsonException>(() => Completion.Apply(source, inputs, events, input with { Command = cmd with { Kind = kind } }));
            var cache = JsonNode.Parse(Codec.SerializeState(state))!; cache["stateVersion"] = long.MaxValue;
            Assert.Throws<JsonException>(() => Completion.Apply(source, inputs, events, input, Encoding.UTF8.GetBytes(cache.ToJsonString())));
            var result = Completion.Apply(source, inputs, events, input); inputs.Add(input); events.Add(result.EventBytes);
        }
        Assert.Throws<JsonException>(() => Completion.Replay(source, inputs, events.AsEnumerable().Reverse().ToArray()));
        Assert.Throws<JsonException>(() => Completion.Replay(source, [], events));
        var foreign = CompletionSource(choice == "act-first" ? "act-last" : "act-first");
        Assert.Throws<JsonException>(() => Completion.Replay(foreign, inputs, events));
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)) })
            Assert.ThrowsAny<JsonException>(() => Codec.ReadBase(bytes, source));
        var oversized = new JsonObject { ["values"] = new JsonArray(Enumerable.Range(0, 513).Select(_ => (JsonNode?)JsonValue.Create(1)).ToArray()) };
        Assert.Throws<JsonException>(() => Codec.ReadBase(Encoding.UTF8.GetBytes(oversized.ToJsonString()), source));
    }

    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void DeepReadbackAndResignedHistoryForgeriesReject(string choice)
    {
        var source = CompletionSource(choice); var inputs = new List<ReserveMovementCompletionInput>(); var events = new List<byte[]>();
        var first = Completion.Replay(source, inputs, events); var basis = Codec.SerializeBase(first.Basis);
        // Sample the deep predecessor plus every top-level canonical envelope mutation; predecessor families already have exhaustive leaf coverage.
        var mutations = Mutations(basis).ToArray();
        foreach (var bytes in mutations.Where((_, index) => index % 37 == 0).Concat(mutations.TakeLast(8)))
            Assert.ThrowsAny<JsonException>(() => Codec.ReadBase(bytes, source));
        for (var cut = 0; cut <= 3; cut++)
        {
            var state = Completion.Replay(source, inputs, events);
            foreach (var bytes in Mutations(Codec.SerializeState(state)))
                Assert.ThrowsAny<JsonException>(() => Codec.ReadState(bytes, source, inputs, events));
            if (cut == 3) break;
            var input = Completion.Command(state); var result = Completion.Apply(source, inputs, events, input);
            foreach (var bytes in Mutations(result.EventBytes))
                Assert.ThrowsAny<JsonException>(() => Completion.Replay(source, [.. inputs, input], [.. events, bytes]));
            var forged = JsonNode.Parse(result.EventBytes)!;
            if (cut == 0) forged["stop"]!["movementReceiptId"] = "mov.forged";
            else if (cut == 1) forged["randomStateAfter"]!["drawCount"] = 99;
            else forged["endLocations"]![0]!["locationId"] = "axis-home";
            forged.AsObject().Remove("receiptId");
            var domain = new[] { "sandtable.combat.inherited-reserve-movement-stop-receipt.v1",
                "sandtable.combat.inherited-reserve-breakdown-stop-resolved-receipt.v1", "sandtable.combat.inherited-reserve-movement-completion-receipt.v1" }[cut];
            forged["receiptId"] = "irmc." + CampaignOpeningPreambleCodec.HashWithDomain(domain, Encoding.UTF8.GetBytes(forged.ToJsonString()))[7..];
            Assert.Throws<JsonException>(() => Completion.Replay(source, [.. inputs, input], [.. events, Encoding.UTF8.GetBytes(forged.ToJsonString())]));
            inputs.Add(input); events.Add(result.EventBytes);
        }
        var forgedBase = JsonNode.Parse(basis)!; forgedBase["predecessorEvents"]![0]!["effect"]!["destinationLocationId"] = "axis-home";
        forgedBase["predecessorHash"] = "sha256:" + new string('0', 64);
        Assert.Throws<JsonException>(() => Codec.ReadBase(Encoding.UTF8.GetBytes(forgedBase.ToJsonString()), source));
        var terminal = JsonNode.Parse(Codec.SerializeState(Completion.Replay(source, inputs, events)))!;
        terminal["movementEnd"]!["completionReceiptId"] = "irmc.forged";
        terminal["releaseMember"]!["history"]!["nextMovement"]!["completionReceiptId"] = "irmc.forged";
        Assert.Throws<JsonException>(() => Codec.ReadState(Encoding.UTF8.GetBytes(terminal.ToJsonString()), source, inputs, events));
    }

    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void SourceOwnsBytesAndRejectsForeignOrIncompleteMoves(string choice)
    {
        var movement = Repeat(choice); var initial = Movement.Replay(movement, [], []);
        var move = Movement.Command(initial, (choice == "act-first" ? "axis" : "commonwealth") + "-rear");
        var moved = Movement.Apply(movement, [], [], move); var bytes = moved.EventBytes;
        var inputs = new[] { move }; var events = new[] { bytes };
        var source = new CombatInheritedReserveMovementCompletionSource(movement, inputs, events);
        var expected = Codec.SerializeState(Completion.Replay(source, [], []));
        inputs[0] = move with { Actor = CampaignOpeningPreambleActor.System }; bytes[0] ^= 1;
        Assert.Equal(expected, Codec.SerializeState(Completion.Replay(source, [], [])));
        Assert.Throws<JsonException>(() => new CombatInheritedReserveMovementCompletionSource(movement, [], []));
        Assert.Throws<JsonException>(() => new CombatInheritedReserveMovementCompletionSource(movement, [move, move], [moved.EventBytes, moved.EventBytes]));
        Assert.Throws<JsonException>(() => Completion.Replay(new(movement, inputs, [moved.EventBytes]), [], []));
        var foreign = Repeat(choice == "act-first" ? "act-last" : "act-first");
        Assert.Throws<JsonException>(() => Completion.Replay(new(foreign, [move], [moved.EventBytes]), [], []));
        var forged = JsonNode.Parse(moved.EventBytes)!; forged["effect"]!["costs"]!["afterCp"] = 0;
        forged.AsObject().Remove("receiptId");
        forged["receiptId"] = "mov." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.ordinary-movement-receipt.v1", Encoding.UTF8.GetBytes(forged.ToJsonString()))[7..];
        Assert.Throws<JsonException>(() => Completion.Replay(new(movement, [move], [Encoding.UTF8.GetBytes(forged.ToJsonString())]), [], []));
        var state = Completion.Replay(source, [], []); state.Basis.Events[0][0] ^= 1;
        Assert.Equal(expected, Codec.SerializeState(Completion.Replay(source, [], [])));
    }
    private static CombatInheritedReserveMovementCompletionSource CompletionSource(string choice)
    {
        var movement = Repeat(choice); var initial = Movement.Replay(movement, [], []);
        var input = Movement.Command(initial, (choice == "act-first" ? "axis" : "commonwealth") + "-rear");
        return new(movement, [input], [Movement.Apply(movement, [], [], input).EventBytes]);
    }

    private sealed record ControlHistory(CombatInheritedCycleControlSource Source, CombatCycleControlInput[] Inputs, byte[][] Events)
    { public CombatInheritedReserveMovementSource Capture() => new(Source, Inputs, Events); }
    private static CombatInheritedReserveMovementSource Repeat(string choice) => ControlTrace(choice).Capture();
    private static ControlHistory ControlTrace(string choice, string action = "repeat", long openedAt = 2100)
    {
        var r = Release(choice); var source = new CombatInheritedCycleControlSource(r.Source, r.Inputs, r.Events);
        var inputs = new List<CombatCycleControlInput>(); var events = new List<byte[]>();
        for (var i = 0; i < 2; i++)
        {
            var state = Control.Replay(source, inputs, events);
            var input = new CombatCycleControlInput(Control.Command(state, i == 0 ? "open" : action),
                i == 0 ? CampaignOpeningPreambleActor.System : choice == "act-first" ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth, openedAt + i);
            var result = Control.Apply(source, inputs, events, input);
            inputs.Add(input); events.Add(result.EventBytes!);
        }
        return new(source, inputs.ToArray(), events.ToArray());
    }
    private sealed record Trace(CombatInheritedReserveReleaseSource Source, CombatReleaseInput[] Inputs, byte[][] Events);
    private static Trace Release(string choice, long openedAt = 1000, long chosenAt = 1100, bool fallback = false)
    {
        var source = Source(choice); var inputs = new List<CombatReleaseInput>(); var events = new List<byte[]>();
        for (var cut = 0; cut < 3; cut++)
        {
            var p = Bridge.Replay(source, inputs, events); var s = p.Release;
            var kind = cut == 0 ? "open" : cut == 1 ? fallback ? "fallback-step" : "choose" : "complete";
            var actor = kind == "choose" ? p.Base.ReleaseBase.Cycle.ActingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth : CampaignOpeningPreambleActor.System;
            var input = new CombatReleaseInput(new(1, kind, s.ReleaseId, s.StateVersion, cut == 0 ? null : s.DecisionId,
                kind == "choose" ? s.Members[0].Unit : null, kind == "choose" ? "release-I" : null), actor,
                cut == 0 ? fallback ? null : openedAt : cut == 1 && !fallback ? chosenAt : null, !(fallback && cut == 0));
            var result = Bridge.Apply(source, inputs, events, input);
            inputs.Add(input); events.Add(result.EventBytes!);
        }
        return new(source, inputs.ToArray(), events.ToArray());
    }
    private sealed record Retained(CampaignCombatCreationRequest Request, byte[] Created, byte[][] Preamble, byte[] Weather, byte[][] Stage, byte[][] Reserve, byte[][] Cycle)
    {
        public CombatInheritedReserveReleaseSource Source() => new(Request, Created, Preamble, [Weather], Stage, Reserve, Cycle);
    }
    private static CombatInheritedReserveReleaseSource Source(string choice) => Build(choice).Source();
    private static Retained Build(string choice)
    {
        var (request, created, opening, weather, stage) = Chain(1, choice); var reserve = new List<byte[]>();
        var state = CampaignCombatReserveDesignation.Replay(request, created, opening, [weather], stage, reserve);
        reserve.Add(CampaignCombatReserveDesignation.Apply(request, created, opening, [weather], stage, reserve, CampaignCombatReserveDesignation.Command(state)).EventBytes);
        reserve.Add(CampaignCombatReserveOpening.Apply(request, created, opening, [weather], stage, reserve,
            CampaignCombatReserveCompletion.CreateCommand(request, created, opening, [weather], stage, reserve)).EventBytes);
        var cycle = new List<byte[]>();
        for (var i = 0; i < 10; i++)
        {
            var current = CampaignCombatInheritedReserveCycle.Replay(request, created, opening, [weather], stage, reserve, cycle);
            cycle.Add(CampaignCombatInheritedReserveCycle.Apply(request, created, opening, [weather], stage, reserve, cycle,
                CampaignCombatInheritedReserveCycle.CreateInput(current)).EventBytes);
        }
        return new(request, created, opening, weather, stage, reserve.ToArray(), cycle.ToArray());
    }
    private static string Hash(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather, byte[][] Stage) Chain(ulong seed = 0, string choice = "act-first")
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
        ulong seed = 0, string choice = "act-first")
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
