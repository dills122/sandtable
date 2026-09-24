using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Bridge = Cna.Core.Campaigns.CampaignCombatInheritedReserveRelease;
using Codec = Cna.Core.Campaigns.CampaignCombatInheritedReserveMovementCodec;
using Control = Cna.Core.Campaigns.CampaignCombatInheritedCycleControl;
using Movement = Cna.Core.Campaigns.CampaignCombatInheritedReserveMovement;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatInheritedReserveMovementTests
{
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void BothOwnerMovementTracesMatchFrozenContract(string choice)
    {
        var source = Repeat(choice); var state = Movement.Replay(source, [], []);
        var before = Codec.SerializeState(state); var basis = Codec.SerializeBase(state.Basis);
        var side = choice == "act-first" ? "axis" : "commonwealth";
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Campaigns", "Fixtures", "combat-inherited-reserve-movement-v1.json")));
        var golden = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("actor").GetString() == side).GetProperty("golden");
        Assert.Equal(golden.GetProperty("baseBytes").GetInt32(), basis.Length);
        Assert.Equal(golden.GetProperty("baseHash").GetString(), Hash(basis));
        Assert.Equal(basis, Codec.SerializeBase(Codec.ReadBase(basis, source)));
        CheckFrame(before, golden.GetProperty("frames")[0]);
        Assert.Equal(before, Codec.SerializeState(Codec.ReadState(before, source, [], [])));
        var input = Movement.Command(state, side + "-rear");
        var moved = Movement.Apply(source, [], [], input, before);
        Assert.False(moved.Duplicate); var bytes = moved.EventBytes;
        Assert.Equal(golden.GetProperty("eventBytes").GetInt32(), bytes.Length);
        Assert.Equal(golden.GetProperty("eventHash").GetString(), Hash(bytes));
        var after = Codec.SerializeState(moved.State); CheckFrame(after, golden.GetProperty("frames")[1]);
        Assert.Equal(after, Codec.SerializeState(Codec.ReadState(after, source, [input], [bytes])));
        Assert.Equal(golden.GetProperty("terminalPrefix").GetString(), moved.State.Prefix);
        Assert.Equal(golden.GetProperty("receiptId").GetString(), Assert.Single(moved.State.Receipts).ReceiptId);
        Assert.Equal(27, state.StateVersion); Assert.Equal(28, moved.State.StateVersion);
        var expectedWorld = JsonNode.Parse(before)!["world"]!.DeepClone();
        var element = expectedWorld["elements"]!.AsArray().Single(e => e!["elementId"]!.GetValue<string>() == input.Command.Unit.ElementId)!;
        element["currentLocationId"] = side + "-rear";
        element["operationalState"]!["capabilityPointsExpended"]!["numerator"] = 2;
        var rep = expectedWorld["representations"]!.AsArray().Single(r => r!["boundElementIds"]![0]!.GetValue<string>() == input.Command.Unit.ElementId)!;
        rep["currentLocationId"] = side + "-rear";
        var final = JsonNode.Parse(after)!;
        Assert.True(JsonNode.DeepEquals(expectedWorld, final["world"]));
        var old = state.World.Elements.Single(e => e.ElementId == input.Command.Unit.ElementId);
        var typedMoved = moved.State.World.Elements.Single(e => e.ElementId == old.ElementId);
        Assert.Equal(new CampaignElementStateV6(old.ElementId, side + "-rear", old.ReserveStatus,
            CampaignCombatSpending.ChargeOrdinary(old.OperationalState, new(2, 1), 10, CampaignCombatSpendCeiling.ReleasedReserveI,
                old.ElementId, "mov.expected", []).State, old.Components, old.SourceParentFormationId, old.CurrentParentFormationId,
            old.Ammunition, old.Readiness), typedMoved);
        Assert.Equal(state.World.Elements.Single(e => e.ElementId != old.ElementId), moved.State.World.Elements.Single(e => e.ElementId != old.ElementId));
        Assert.Equal(state.World.BrokenVehicleLots, moved.State.World.BrokenVehicleLots);
        Assert.Equal(state.World.CohesionCauses, moved.State.World.CohesionCauses);
        Assert.Equal(state.World.Relationships, moved.State.World.Relationships);
        Assert.Equal(state.World.CustodyLots, moved.State.World.CustodyLots);
        Assert.Equal(state.World.Guards, moved.State.World.Guards);
        Assert.Equal(state.World.ReplacementEntitlements, moved.State.World.ReplacementEntitlements);
        Assert.Equal(state.World.FutureObligations, moved.State.World.FutureObligations);
        Assert.Equal(state.World.Settlements, moved.State.World.Settlements);
        Assert.Equal(state.ReleaseMember with { SpentCp = new(2, 1) }, moved.State.ReleaseMember);
        Assert.Equal("pending", moved.State.ReleaseMember.History.NextMovement!.Status);
        Assert.Null(moved.State.ReleaseMember.History.NextMovement.CompletionReceiptId);
        Assert.Equal(state.ReleaseMember.History, moved.State.ReleaseMember.History);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(before)!["randomState"], final["randomState"]));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(before)!["attackHistory"], final["attackHistory"]));
        Assert.Equal(new[] { input.Command.OriginLocationId, input.Command.DestinationLocationId }, Assert.Single(moved.State.Tracks).Route);
        var costs = JsonNode.Parse(bytes)!["effect"]!["costs"]!;
        Assert.Equal(10, costs["voluntaryCeiling"]!.GetValue<int>()); Assert.Equal(0, costs["excessCpDp"]!.GetValue<int>());
        Assert.Empty(JsonNode.Parse(bytes)!["effect"]!["endedMemberships"]!.AsArray());
        var retry = Movement.Apply(source, [input], [bytes], input);
        Assert.True(retry.Duplicate); Assert.Equal(bytes, retry.EventBytes); Assert.Equal(after, Codec.SerializeState(retry.State));
        moved.EventBytes[0] ^= 1; Assert.Equal(bytes, moved.EventBytes);
        Assert.Equal(before, Codec.SerializeState(state));
    }
    private static void CheckFrame(byte[] bytes, JsonElement golden)
    { Assert.Equal(golden.GetProperty("bytes").GetInt32(), bytes.Length); Assert.Equal(golden.GetProperty("sha256").GetString(), Hash(bytes)); }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void InvalidCommandsAndSecondMoveCannotChangeAuthority(string choice)
    {
        var source = Repeat(choice); var state = Movement.Replay(source, [], []);
        var side = choice == "act-first" ? "axis" : "commonwealth";
        var valid = Movement.Command(state, side + "-rear"); var c = valid.Command;
        var other = state.World.Elements.Single(e => e.ElementId != c.Unit.ElementId);
        foreach (var command in new[]
        {
            c with { ContractVersion = 2 }, c with { Kind = "complete" },
            c with { CycleId = "sha256:" + new string('0', 64) }, c with { PositionId = "foreign" },
            c with { ExpectedPriorVersion = 26 }, c with { ExpectedPriorVersion = 28 }, c with { ExpectedPriorVersion = long.MaxValue },
            c with { Unit = new(c.Unit.CreationBinding, c.Unit.OriginalSide, other.ElementId) },
            c with { Unit = new("foreign", c.Unit.OriginalSide, c.Unit.ElementId) },
            c with { OriginLocationId = c.DestinationLocationId },
            c with { DestinationLocationId = c.OriginLocationId }, c with { DestinationLocationId = "unknown" },
            c with { DestinationLocationId = other.CurrentLocationId },
            c with { DestinationLocationId = side == "axis" ? "commonwealth-rear" : "axis-rear" }
        }) Assert.Throws<JsonException>(() => Movement.Apply(source, [], [], valid with { Command = command }));
        foreach (var actor in new[] { CampaignOpeningPreambleActor.System, side == "axis" ? CampaignOpeningPreambleActor.Commonwealth : CampaignOpeningPreambleActor.Axis })
            Assert.Throws<JsonException>(() => Movement.Apply(source, [], [], valid with { Actor = actor }));
        var accepted = Movement.Apply(source, [], [], valid);
        Assert.Throws<JsonException>(() => Movement.Apply(source, [valid], [accepted.EventBytes],
            Movement.Command(accepted.State, c.OriginLocationId)));
        Assert.Throws<JsonException>(() => Movement.Apply(source, [valid], [accepted.EventBytes], valid with { Actor = CampaignOpeningPreambleActor.System }));
        Assert.Throws<JsonException>(() => Movement.Apply(source, [valid], [accepted.EventBytes], valid with { Command = c with { DestinationLocationId = other.CurrentLocationId } }));
        Assert.Throws<JsonException>(() => Movement.Replay(source, [valid], []));
        Assert.Throws<JsonException>(() => Movement.Replay(source, [valid, valid], [accepted.EventBytes, accepted.EventBytes]));
        Assert.Throws<JsonException>(() => Movement.Apply(source, [], [], valid, Codec.SerializeState(accepted.State)));
        Assert.Equal(27, Movement.Replay(source, [], []).StateVersion);
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void DeepReadbackMutationsAndResignedForgeriesReject(string choice)
    {
        var source = Repeat(choice); var first = Movement.Replay(source, [], []);
        var input = Movement.Command(first, choice == "act-first" ? "axis-rear" : "commonwealth-rear");
        var moved = Movement.Apply(source, [], [], input); var basis = Codec.SerializeBase(first.Basis);
        var count = 0;
        foreach (var bytes in Mutations(basis)) { Assert.ThrowsAny<JsonException>(() => Codec.ReadBase(bytes, source)); count++; }
        Assert.True(count > 600);
        foreach (var (state, inputs, events) in new (InheritedReserveMoveState, CycleMoveInput[], byte[][])[]
            { (first, [], []), (moved.State, [input], [moved.EventBytes]) })
            foreach (var bytes in Mutations(Codec.SerializeState(state)))
                Assert.ThrowsAny<JsonException>(() => Codec.ReadState(bytes, source, inputs, events));
        foreach (var bytes in Mutations(moved.EventBytes))
            Assert.ThrowsAny<JsonException>(() => Movement.Replay(source, [input], [bytes]));
        var forged = JsonNode.Parse(moved.EventBytes)!;
        forged["effect"]!["costs"]!["voluntaryCeiling"] = 15;
        forged.AsObject().Remove("receiptId");
        forged["receiptId"] = "mov." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.ordinary-movement-receipt.v1", Encoding.UTF8.GetBytes(forged.ToJsonString()))[7..];
        Assert.Throws<JsonException>(() => Movement.Replay(source, [input], [Encoding.UTF8.GetBytes(forged.ToJsonString())]));
        var forgedBase = JsonNode.Parse(basis)!;
        forgedBase["predecessorBase"]!["armedProof"]!["assessment"]!["candidateIds"]![0] = "cand.forged";
        var proofHash = Hash(Encoding.UTF8.GetBytes(forgedBase["predecessorBase"]!["armedProof"]!.ToJsonString()));
        forgedBase["predecessorBase"]!["armedProofHash"] = proofHash;
        forgedBase["predecessorBase"]!["controlBase"]!["profile"] = "inherited-armed-" + proofHash[7..];
        Assert.Throws<JsonException>(() => Codec.ReadBase(Encoding.UTF8.GetBytes(forgedBase.ToJsonString()), source));
        var forgedState = JsonNode.Parse(Codec.SerializeState(moved.State))!;
        forgedState["releaseMember"]!["spentCp"]!["numerator"] = 0;
        Assert.Throws<JsonException>(() => Codec.ReadState(Encoding.UTF8.GetBytes(forgedState.ToJsonString()), source, [input], [moved.EventBytes]));
        var overflow = JsonNode.Parse(Codec.SerializeState(first))!; overflow["stateVersion"] = long.MaxValue;
        Assert.Throws<JsonException>(() => Movement.Apply(source, [], [], input, Encoding.UTF8.GetBytes(overflow.ToJsonString())));
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)) })
            Assert.ThrowsAny<JsonException>(() => Codec.ReadBase(bytes, source));
        forgedBase["predecessorInputs"] = new JsonArray(Enumerable.Range(0, 513).Select(_ => (JsonNode?)JsonValue.Create(1)).ToArray());
        Assert.Throws<JsonException>(() => Codec.ReadBase(Encoding.UTF8.GetBytes(forgedBase.ToJsonString()), source));
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void SourceRequiresExactRepeatHistoryAndOwnsCallerBytes(string choice)
    {
        var retained = ControlTrace(choice); var source = retained.Capture();
        var initial = Movement.Replay(source, [], []); var initialBytes = Codec.SerializeState(initial);
        var original = retained.Events[1].ToArray(); var originalInput = retained.Inputs[1];
        retained.Events[1][0] ^= 1; retained.Inputs[1] = originalInput with { Actor = CampaignOpeningPreambleActor.System };
        Assert.Equal(initialBytes, Codec.SerializeState(Movement.Replay(source, [], [])));
        retained.Events[1] = original;
        Assert.Throws<JsonException>(() => Movement.Replay(retained.Capture(), [], []));
        retained.Inputs[1] = originalInput;
        Assert.Throws<JsonException>(() => new CombatInheritedReserveMovementSource(retained.Source, [], []));
        foreach (var unsupported in new[] { ControlTrace(choice, "finish"), ControlTrace(choice, "repeat", 2101) })
            Assert.Throws<JsonException>(() => Movement.Replay(unsupported.Capture(), [], []));
        var foreign = ControlTrace(choice == "act-first" ? "act-last" : "act-first");
        Assert.Throws<JsonException>(() => Movement.Replay(new(retained.Source, foreign.Inputs, foreign.Events), [], []));
        Assert.Throws<JsonException>(() => Codec.ReadBase(Codec.SerializeBase(initial.Basis), foreign.Capture()));
        var reordered = new[] { retained.Events[1], retained.Events[0] };
        Assert.Throws<JsonException>(() => Movement.Replay(new(retained.Source, retained.Inputs, reordered), [], []));
        var controlEvent = JsonNode.Parse(original)!;
        controlEvent["effect"]!["nextCycle"]!["ordinal"] = 3;
        controlEvent.AsObject().Remove("receiptId");
        controlEvent["receiptId"] = "cc." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.cycle-control-receipt.v1", Encoding.UTF8.GetBytes(controlEvent.ToJsonString()))[7..];
        Assert.Throws<JsonException>(() => Movement.Replay(new(retained.Source, retained.Inputs, [retained.Events[0], Encoding.UTF8.GetBytes(controlEvent.ToJsonString())]), [], []));
        var incomplete = Build(choice) with { Cycle = [] }; var release = Release(choice);
        var badSource = new CombatInheritedCycleControlSource(incomplete.Source(), release.Inputs, release.Events);
        Assert.Throws<JsonException>(() => Movement.Replay(new(badSource, retained.Inputs, retained.Events), [], []));
        initial.Basis.Events[0][0] ^= 1;
        Assert.Equal(initialBytes, Codec.SerializeState(Movement.Replay(source, [], [])));
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
