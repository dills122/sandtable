using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Bridge = Cna.Core.Campaigns.CampaignCombatInheritedReserveRelease;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatInheritedReserveReleaseTests
{
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void ActualHistoryMatchesEveryFrozenBaseEventControlAndRetry(string choice)
    {
        var source = Source(choice); using var fixture = Fixture();
        var golden = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("choice").GetString() == choice).GetProperty("goldens");
        var inputs = new List<CombatReleaseInput>(); var events = new List<byte[]>();
        var predecessor = source.Replay(); var before = CampaignCombatInheritedReserveCycleCodec.SerializeControl(predecessor);
        for (var cut = 0; cut <= 3; cut++)
        {
            var projection = Bridge.Replay(source, inputs, events); var state = projection.Release;
            var basis = Bridge.SerializeBase(projection.Base, source.Request); var control = Bridge.SerializeControl(projection, source.Request);
            Assert.Equal(golden.GetProperty("predecessorHash").GetString(), projection.Base.PredecessorHash);
            Assert.Equal(golden.GetProperty("baseHash").GetString(), Hash(basis));
            Assert.Equal(golden.GetProperty("frames")[cut].GetProperty("bytes").GetInt32(), control.Length);
            Assert.Equal(golden.GetProperty("frames")[cut].GetProperty("sha256").GetString(), Hash(control));
            Assert.Equal(basis, Bridge.SerializeBase(Bridge.ReadBase(basis, source), source.Request));
            Assert.Equal(control, Bridge.SerializeControl(Bridge.ReadControl(control, source, inputs, events), source.Request));
            Assert.Equal(before, CampaignCombatInheritedReserveCycleCodec.SerializeControl(projection.Base.Predecessor));
            Assert.Equal(22 + cut, state.StateVersion); Assert.Equal(cut == 3, projection.Closed);
            Assert.Equal(1, projection.Base.ReleaseBase.Cycle.Ordinal);
            Assert.Null(projection.Base.ReleaseBase.AcceptedHighWater);
            AssertWorld(projection, cut < 2 ? CampaignElementReserveStatus.ReserveI : CampaignElementReserveStatus.None);
            Assert.Equal(projection.Base.ReleaseBase.RandomState, state.RandomState);
            Assert.Equal(projection.Base.ReleaseBase.RetainedWorldHash, state.RetainedWorldHash);
            Assert.Empty(state.AttackHistory);
            if (cut < 2) Assert.Empty(projection.Progress);
            else
            {
                var progress = Assert.Single(projection.Progress); Assert.Equal(Hash(events[1]), progress.EventHash);
                Assert.Equal(state.Dispositions[0].ReceiptId, progress.ReceiptId);
                var member = Assert.Single(state.Members); Assert.Equal("I", member.History.ReleasedType);
                Assert.Equal(1, member.History.ReleaseCycle); Assert.Equal(10, member.History.VoluntaryCeiling);
                Assert.Equal(new CapabilityPointAmount(0, 1), member.SpentCp);
                Assert.Equal(2, member.History.NextMovement!.Ordinal); Assert.Equal("pending", member.History.NextMovement.Status);
                Assert.Null(member.History.NextMovement.CompletionReceiptId);
                Assert.Equal(projection.Base.ReleaseBase.Members[0].History.DesignationReceiptId, member.History.DesignationReceiptId);
            }
            for (var i = 0; i < cut; i++)
            {
                var retry = Bridge.Apply(source, inputs, events, inputs[i], control);
                Assert.Equal(CombatStepsDisposition.Duplicate, retry.Disposition); Assert.Equal(events[i], retry.EventBytes);
                Assert.Equal(CampaignCombatReserveReleaseCodec.SerializeState(state), CampaignCombatReserveReleaseCodec.SerializeState(retry.State));
                retry.EventBytes![0] ^= 1; Assert.Equal(events[i], retry.EventBytes);
            }
            if (cut == 3)
            {
                Assert.Equal(golden.GetProperty("terminalEvent").GetString(), Encoding.UTF8.GetString(events[^1]));
                Assert.Equal(1100, state.AcceptedHighWater);
                break;
            }
            var input = Input(projection, cut == 0 ? "open" : cut == 1 ? "choose" : "complete", cut == 0 ? 1000 : cut == 1 ? 1100 : null);
            var result = Bridge.Apply(source, inputs, events, input, control);
            Assert.Equal(CombatStepsDisposition.Accepted, result.Disposition);
            Assert.Equal(golden.GetProperty("eventHashes")[cut].GetString(), Hash(result.EventBytes!));
            inputs.Add(input); events.Add(result.EventBytes!);
        }
    }

    [Theory]
    [InlineData("act-first", "deadline")]
    [InlineData("act-last", "deadline")]
    [InlineData("act-first", "unavailable")]
    [InlineData("act-last", "unavailable")]
    [InlineData("act-first", "regression")]
    [InlineData("act-last", "regression")]
    [InlineData("act-first", "clock-lost")]
    [InlineData("act-last", "clock-lost")]
    [InlineData("act-first", "opening-lost")]
    [InlineData("act-last", "opening-lost")]
    public void RecoveryConvertsHeldIWithoutReleasingOrChangingResources(string choice, string mode)
    {
        var source = Source(choice); var inputs = new List<CombatReleaseInput>(); var events = new List<byte[]>();
        var initial = Bridge.Replay(source, inputs, events);
        Send(Input(initial, "open", mode == "opening-lost" ? null : 1000) with { ClockAvailable = mode != "opening-lost" });
        var opened = Bridge.Replay(source, inputs, events);
        var kind = mode == "deadline" ? "expire" : mode == "unavailable" ? "unavailable" : mode == "opening-lost" ? "fallback-step" : "choose";
        Send(Input(opened, kind, mode == "deadline" ? 31000 : mode == "regression" ? 900 : mode == "opening-lost" ? null : 1100)
            with
        { ClockAvailable = mode != "clock-lost" });
        var converted = Bridge.Replay(source, inputs, events);
        AssertWorld(converted, CampaignElementReserveStatus.ReserveII);
        Assert.True(converted.Release.FallbackLocked); Assert.Null(converted.Release.Members[0].History.NextMovement);
        Assert.Null(converted.Release.Members[0].History.ReleasedType);
        Assert.Equal(converted.Release.Dispositions[0].ReceiptId, converted.Release.Members[0].History.ConversionReceiptId);
        using var disposition = JsonDocument.Parse(events[1]); Assert.Equal("system", disposition.RootElement.GetProperty("author").GetString());
        Send(Input(converted, "complete"));
        var complete = Bridge.Replay(source, inputs, events); Assert.True(complete.Closed); Assert.Equal(25, complete.Release.StateVersion);
        AssertWorld(complete, CampaignElementReserveStatus.ReserveII);
        for (var cut = 0; cut <= events.Count; cut++)
        {
            var recovered = Bridge.Replay(source, inputs.Take(cut).ToArray(), events.Take(cut).ToArray());
            var bytes = Bridge.SerializeControl(recovered, source.Request);
            Assert.Equal(bytes, Bridge.SerializeControl(Bridge.ReadControl(bytes, source, inputs.Take(cut).ToArray(), events.Take(cut).ToArray()), source.Request));
        }
        var retry = Bridge.Apply(source, inputs, events, Input(complete, "expire", 32000));
        Assert.Equal(mode == "deadline" ? CombatStepsDisposition.Duplicate : CombatStepsDisposition.NoOp, retry.Disposition);
        if (mode == "deadline") Assert.Equal(events[1], retry.EventBytes);
        void Send(CombatReleaseInput input)
        {
            var result = Bridge.Apply(source, inputs, events, input); Assert.Equal(CombatStepsDisposition.Accepted, result.Disposition);
            inputs.Add(input); events.Add(result.EventBytes!);
        }
    }

    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void RejectsForeignStaleLateUnsupportedAndResignedEvents(string choice)
    {
        var source = Source(choice); var foreign = Source(choice == "act-first" ? "act-last" : "act-first");
        var initial = Bridge.Replay(source, [], []); var open = Input(initial, "open", 1000);
        var opened = Bridge.Apply(source, [], [], open); var events = new[] { opened.EventBytes! }; var inputs = new[] { open };
        var projection = Bridge.Replay(source, inputs, events); var valid = Input(projection, "choose", 1100);
        foreach (var bad in new[] {
            valid with { Actor = valid.Actor == CampaignOpeningPreambleActor.Axis ? CampaignOpeningPreambleActor.Commonwealth : CampaignOpeningPreambleActor.Axis },
            valid with { Command = valid.Command with { ExpectedPriorVersion = 22 } },
            valid with { Command = valid.Command with { DecisionId = "foreign" } },
            valid with { Command = valid.Command with { ReleaseId = "foreign" } },
            valid with { Command = valid.Command with { Choice = "convert-to-II" } },
            valid with { Command = valid.Command with { Choice = "release-II" } },
            valid with { AdmittedAt = 31000 },
            Input(projection, "complete"), Input(projection, "complete-release", 1100), Input(projection, "fallback-step") })
            Assert.ThrowsAny<JsonException>(() => Bridge.Apply(source, inputs, events, bad));
        Assert.Equal(CombatStepsDisposition.NoOp, Bridge.Apply(source, inputs, events, Input(projection, "expire", 30999)).Disposition);
        Assert.Equal(CombatStepsDisposition.NoOp, Bridge.Apply(source, inputs, events, Input(projection, "expire", 31000) with { Command = Input(projection, "expire", 31000).Command with { DecisionId = "foreign" } }).Disposition);
        Assert.ThrowsAny<JsonException>(() => Bridge.Replay(foreign, inputs, events));
        foreach (var bad in Mutations(events[0])) Assert.ThrowsAny<JsonException>(() => Bridge.Replay(source, inputs, [bad]));
        var chosen = Bridge.Apply(source, inputs, events, valid); var finalInputs = inputs.Append(valid).ToArray(); var finalEvents = events.Append(chosen.EventBytes!).ToArray();
        foreach (var bad in Mutations(chosen.EventBytes!)) Assert.ThrowsAny<JsonException>(() => Bridge.Replay(source, finalInputs, [events[0], bad]));
        var resigned = JsonNode.Parse(chosen.EventBytes!)!; resigned["effect"]!["afterStatus"] = "I";
        resigned.AsObject().Remove("receiptId");
        resigned["receiptId"] = "rr." + Hash(Encoding.UTF8.GetBytes("sandtable.combat.reserve-release-receipt.v1\0" + resigned.ToJsonString()))[7..];
        Assert.ThrowsAny<JsonException>(() => Bridge.Replay(source, finalInputs, [events[0], Encoding.UTF8.GetBytes(resigned.ToJsonString())]));
        Assert.ThrowsAny<JsonException>(() => Bridge.Replay(source, finalInputs.Reverse().ToArray(), finalEvents.Reverse().ToArray()));
        Assert.ThrowsAny<JsonException>(() => Bridge.Replay(source, [open, open], [events[0], events[0]]));
        Assert.ThrowsAny<JsonException>(() => Bridge.Replay(source, [], events));
        Assert.ThrowsAny<JsonException>(() => Bridge.Replay(source, [open, open, open, open], [events[0], events[0], events[0], events[0]]));
    }

    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void BaseAndControlReadbackRejectTamperingAndRequireActualSource(string choice)
    {
        var source = Source(choice); var initial = Bridge.Replay(source, [], []);
        var basis = Bridge.SerializeBase(initial.Base, source.Request); var control = Bridge.SerializeControl(initial, source.Request);
        foreach (var bad in Mutations(basis)) Assert.ThrowsAny<JsonException>(() => Bridge.ReadBase(bad, source));
        foreach (var bad in Mutations(control)) Assert.ThrowsAny<JsonException>(() => Bridge.ReadControl(bad, source, [], []));
        Assert.ThrowsAny<JsonException>(() => Bridge.Apply(source, [], [], Input(initial, "open", 1000), control.Append((byte)' ').ToArray()));
        Assert.ThrowsAny<JsonException>(() => Bridge.ReadBase([], source));
        Assert.ThrowsAny<JsonException>(() => Bridge.ReadControl(new byte[1_048_577], source, [], []));
        var history = Build(choice);
        foreach (var corrupt in new[] { history with { Cycle = history.Cycle.Take(9).ToArray() }, history with { Cycle = [] },
            history with { Cycle = history.Cycle.Reverse().ToArray() }, history with { Reserve = [] },
            history with { Preamble = history.Preamble.Skip(1).ToArray() }, history with { Cycle = Build(choice == "act-first" ? "act-last" : "act-first").Cycle } })
            Assert.ThrowsAny<JsonException>(() => Bridge.Replay(corrupt.Source(), [], []));
        var captured = history.Source(); history.Created[0] ^= 1; history.Cycle[0][0] ^= 1;
        Assert.Equal(basis, Bridge.SerializeBase(Bridge.Replay(captured, [], []).Base, captured.Request));
    }

    private static CombatReleaseInput Input(Bridge.Projection projection, string kind, long? now = null)
    {
        var state = projection.Release;
        return new(new(1, kind, state.ReleaseId, kind is "expire" or "unavailable" ? null : state.StateVersion,
            kind == "open" ? null : state.DecisionId, kind == "choose" ? state.Members[0].Unit : null, kind == "choose" ? "release-I" : null),
            kind is "choose" or "complete-release" ? projection.Base.ReleaseBase.Cycle.ActingSide == LandSide.Axis
                ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth : CampaignOpeningPreambleActor.System, now);
    }
    private static void AssertWorld(Bridge.Projection projection, CampaignElementReserveStatus expected)
    {
        var prior = projection.Base.Predecessor.Base.Predecessor.World;
        var before = JsonSerializer.SerializeToNode(prior)!;
        var own = before["Elements"]!.AsArray().Single(e => e!["ElementId"]!.GetValue<string>() == projection.Release.Members[0].Unit.ElementId)!;
        own["ReserveStatus"] = (int)expected;
        Assert.True(JsonNode.DeepEquals(before, JsonSerializer.SerializeToNode(projection.World)), "Only own Reserve status may change.");
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
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
        "Campaigns", "Fixtures", "combat-inherited-reserve-release-v1.json")));
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
