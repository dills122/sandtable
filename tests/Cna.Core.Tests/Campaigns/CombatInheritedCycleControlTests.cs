using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Bridge = Cna.Core.Campaigns.CampaignCombatInheritedReserveRelease;
using Codec = Cna.Core.Campaigns.CampaignCombatInheritedCycleControlCodec;
using Control = Cna.Core.Campaigns.CampaignCombatInheritedCycleControl;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatInheritedCycleControlTests
{
    [Theory]
    [InlineData("act-first", "repeat")]
    [InlineData("act-first", "finish")]
    [InlineData("act-last", "repeat")]
    [InlineData("act-last", "finish")]
    public void BothOwnerControlTracesMatchFrozenContract(string choice, string action)
    {
        var release = Release(choice);
        var source = new CombatInheritedCycleControlSource(release.Source, release.Inputs, release.Events);
        var state = Control.Replay(source, [], []);
        var initial = state;
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Campaigns", "Fixtures", "combat-inherited-cycle-control-v1.json")));
        var golden = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c =>
            c.GetProperty("actor").GetString() == (choice == "act-first" ? "axis" : "commonwealth") &&
            c.GetProperty("action").GetString() == action).GetProperty("golden");
        var basis = Codec.SerializeBase(state.Basis);
        Assert.Equal(golden.GetProperty("baseBytes").GetInt32(), basis.Length);
        Assert.Equal(golden.GetProperty("baseHash").GetString(), Hash(basis));
        Assert.Equal(golden.GetProperty("nestedBaseHash").GetString(), state.BaseHash);
        Assert.Equal(golden.GetProperty("controlId").GetString(), state.ControlId);
        Assert.Equal(basis, Codec.SerializeBase(Codec.ReadBase(basis, source)));
        var inputs = new List<CombatCycleControlInput>(); var events = new List<byte[]>();
        var openedAt = state.AcceptedHighWater!.Value + 1000;
        for (var cut = 0; cut <= 2; cut++)
        {
            var bytes = Codec.SerializeState(state); var frame = golden.GetProperty("frames")[cut];
            Assert.Equal(frame.GetProperty("bytes").GetInt32(), bytes.Length);
            Assert.Equal(frame.GetProperty("sha256").GetString(), Hash(bytes));
            Assert.Equal(bytes, Codec.SerializeState(Codec.ReadState(bytes, source, inputs, events)));
            if (cut == 2) break;
            var input = new CombatCycleControlInput(Control.Command(state, cut == 0 ? "open" : action),
                cut == 0 ? CampaignOpeningPreambleActor.System : Owner(choice), openedAt + cut);
            var result = Control.Apply(source, inputs, events, input, bytes);
            var eventBytes = result.EventBytes!; var eg = golden.GetProperty("events")[cut];
            Assert.Equal(eg.GetProperty("bytes").GetInt32(), eventBytes.Length);
            Assert.Equal(eg.GetProperty("sha256").GetString(), Hash(eventBytes));
            inputs.Add(input); events.Add(eventBytes); state = result.State;
            var duplicate = Control.Apply(source, inputs, events, input with { AdmittedAt = null, ClockAvailable = false });
            Assert.Equal(CombatStepsDisposition.Duplicate, duplicate.Disposition);
            Assert.Equal(eventBytes, duplicate.EventBytes);
            Assert.Equal(result.ReceiptId, duplicate.ReceiptId);
        }
        Assert.Equal(27, state.StateVersion);
        Assert.Equal(golden.GetProperty("terminalPrefix").GetString(), state.Prefix);
        Assert.Equal(golden.GetProperty("terminalPositionId").GetString(), state.PositionId);
        Assert.Equal(golden.GetProperty("terminalReceiptId").GetString(), state.Closure!.ReceiptId);
        var before = JsonNode.Parse(Codec.SerializeState(initial))!;
        var after = JsonNode.Parse(Codec.SerializeState(state))!;
        foreach (var field in new[] { "world", "randomState", "attackHistory", "targetUses", "nextCycleProgress" })
            Assert.True(JsonNode.DeepEquals(before[field], after[field]), field);
        Assert.Equal(initial.Basis.Proof.Source.Base.ReleaseBase.Cycle.ActingSide, state.Basis.Proof.Source.Base.ReleaseBase.Cycle.ActingSide);
        var member = Assert.Single(state.Members);
        if (action == "repeat")
        {
            Assert.Equal("repeated", state.Status); Assert.Equal(2, state.ActiveCycle!.Ordinal);
            Assert.Equal(27, state.ActiveCycle.OpenedAuthorityVersion);
            Assert.Equal(JsonNode.Parse(events[1])!["priorPrefix"]!.GetValue<string>(), state.ActiveCycle.OpeningPrefix);
            Assert.Equal(initial.Members, state.Members); Assert.Equal("pending", member.History.NextMovement!.Status);
        }
        else
        {
            Assert.Equal("finished", state.Status); Assert.Null(state.ActiveCycle);
            Assert.Equal("expired", member.History.NextMovement!.Status);
            Assert.Equal(state.Closure.ReceiptId, member.History.NextMovement.CompletionReceiptId);
            Assert.Equal(initial.Members[0].History with { NextMovement = member.History.NextMovement }, member.History);
        }
        var oldRetry = Control.Apply(source, inputs, events, inputs[0]);
        Assert.Equal(events[0], oldRetry.EventBytes);
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void TimingFallbacksRetainBudgetAndCloseDeterministically(string choice)
    {
        var t = Open(choice); var deadline = t.State.Timing!.DeadlineUnixMilliseconds;
        foreach (var kind in new[] { "repeat", "finish" })
            Assert.Throws<JsonException>(() => Control.Apply(t.Source, t.Inputs, t.Events,
                new(Control.Command(t.State, kind), Owner(choice), deadline)));
        var early = new CombatCycleControlInput(Control.Command(t.State, "expire"), CampaignOpeningPreambleActor.System, deadline - 1);
        var noOp = Control.Apply(t.Source, t.Inputs, t.Events, early);
        Assert.Equal(CombatStepsDisposition.NoOp, noOp.Disposition); Assert.Null(noOp.EventBytes);
        Assert.Equal(Codec.SerializeState(t.State), Codec.SerializeState(noOp.State));
        foreach (var (kind, now, available, reason) in new (string, long?, bool, string)[]
        {
            ("expire", deadline, true, "deadline"),
            ("unavailable", 2101, true, "controller-unavailable"),
            ("repeat", 2099, true, "clock-unavailable"),
            ("finish", null, true, "clock-unavailable"),
            ("repeat", 2101, false, "clock-unavailable")
        })
        {
            var input = new CombatCycleControlInput(Control.Command(t.State, kind), kind is "repeat" or "finish" ? Owner(choice) : CampaignOpeningPreambleActor.System, now, available);
            var result = Control.Apply(t.Source, t.Inputs, t.Events, input);
            AssertFinished(result, reason);
            Assert.Equal(t.State.Timing.OpenedAtUnixMilliseconds, result.State.Timing!.OpenedAtUnixMilliseconds);
            Assert.Equal(deadline, result.State.Timing.DeadlineUnixMilliseconds);
            Assert.Equal(reason == "clock-unavailable" ? 2100 : now, result.State.AcceptedHighWater);
            var replayed = Control.Replay(t.Source, [.. t.Inputs, input], [.. t.Events, result.EventBytes!]);
            Assert.Equal(Codec.SerializeState(result.State), Codec.SerializeState(replayed));
            var retry = Control.Apply(t.Source, [.. t.Inputs, input], [.. t.Events, result.EventBytes!], input);
            Assert.Equal(CombatStepsDisposition.Duplicate, retry.Disposition); Assert.Equal(result.EventBytes, retry.EventBytes);
        }
        Assert.Throws<JsonException>(() => Control.Apply(t.Source, t.Inputs, t.Events,
            new(Control.Command(t.State, "fallback-step"), CampaignOpeningPreambleActor.System)));
        foreach (var (now, available) in new (long?, bool)[] { (null, true), (2100, false), (1099, true), (CampaignCombatSelectionSteps.UtcMaximum, true) })
        {
            var initial = Control.Replay(t.Source, [], []);
            var input = new CombatCycleControlInput(Control.Command(initial, "open"), CampaignOpeningPreambleActor.System, now, available);
            var opened = Control.Apply(t.Source, [], [], input);
            Assert.True(opened.State.OpeningClockFailure); Assert.Null(opened.State.Timing);
            Assert.Equal(initial.AcceptedHighWater, opened.State.AcceptedHighWater);
            var fallback = new CombatCycleControlInput(Control.Command(opened.State, "fallback-step"), CampaignOpeningPreambleActor.System);
            var finished = Control.Apply(t.Source, [input], [opened.EventBytes!], fallback);
            AssertFinished(finished, "opening-clock-unavailable");
            Assert.Null(finished.State.Timing);
        }
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void BindingAndStaleInputsRejectBeforeRetryAndTimersDoNotAdvance(string choice)
    {
        var t = Open(choice); var owner = Owner(choice);
        var valid = new CombatCycleControlInput(Control.Command(t.State, "repeat"), owner, 2101);
        var badCommands = new[]
        {
            valid.Command with { ContractVersion = 2 }, valid.Command with { Kind = "unknown" },
            valid.Command with { ControlId = valid.Command.ControlId + ".foreign" },
            valid.Command with { CycleId = "sha256:" + new string('0', 64) },
            valid.Command with { ExpectedPriorVersion = null }, valid.Command with { ExpectedPriorVersion = 25 },
            valid.Command with { DecisionId = null }, valid.Command with { DecisionId = "foreign" }
        };
        foreach (var command in badCommands) Assert.Throws<JsonException>(() => Control.Apply(t.Source, t.Inputs, t.Events, valid with { Command = command }));
        foreach (var actor in new[] { CampaignOpeningPreambleActor.System, Owner(choice == "act-first" ? "act-last" : "act-first") })
            Assert.Throws<JsonException>(() => Control.Apply(t.Source, t.Inputs, t.Events, valid with { Actor = actor }));
        Assert.Throws<JsonException>(() => Control.Apply(t.Source, t.Inputs, t.Events, t.Inputs[0] with { Actor = owner }));
        var lateOpen = t.Inputs[0] with { Command = Control.Command(t.State, "open") };
        Assert.Throws<JsonException>(() => Control.Apply(t.Source, t.Inputs, t.Events, lateOpen));
        var oldTimer = new CombatCycleControlInput(Control.Command(t.State, "expire") with { DecisionId = "old" }, CampaignOpeningPreambleActor.System, 999999);
        Assert.Equal(CombatStepsDisposition.NoOp, Control.Apply(t.Source, t.Inputs, t.Events, oldTimer).Disposition);
        var terminal = Control.Apply(t.Source, t.Inputs, t.Events, valid);
        var inputs = new[] { t.Inputs[0], valid }; var events = new[] { t.Events[0], terminal.EventBytes! };
        Assert.Throws<JsonException>(() => Control.Apply(t.Source, inputs, events, valid with { Command = valid.Command with { Kind = "finish" } }));
        Assert.Equal(CombatStepsDisposition.NoOp, Control.Apply(t.Source, inputs, events,
            new(Control.Command(terminal.State, "expire"), CampaignOpeningPreambleActor.System, 999999)).Disposition);
        Assert.Throws<JsonException>(() => Control.Replay(t.Source, [.. inputs, valid], [.. events, terminal.EventBytes!]));
        Assert.Throws<JsonException>(() => Control.Replay(t.Source, inputs, [events[0]]));
        Assert.Throws<JsonException>(() => Control.Replay(t.Source, [valid, t.Inputs[0]], [events[1], events[0]]));
        Assert.Throws<JsonException>(() => Control.Replay(t.Source, [t.Inputs[0], t.Inputs[0]], [events[0], events[0]]));
        foreach (var time in new long[] { -1, CampaignCombatSelectionSteps.UtcMaximum + 1 })
            Assert.Throws<JsonException>(() => Control.Apply(t.Source, t.Inputs, t.Events, valid with { AdmittedAt = time }));
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void ReadbackRejectsDeepMutationAndResignedEvents(string choice)
    {
        var t = Open(choice);
        var closing = new CombatCycleControlInput(Control.Command(t.State, "repeat"), Owner(choice), 2101);
        var end = Control.Apply(t.Source, t.Inputs, t.Events, closing);
        var inputs = new[] { t.Inputs[0], closing }; var events = new[] { t.Events[0], end.EventBytes! };
        var basis = Codec.SerializeBase(t.State.Basis);
        var baseCount = 0;
        foreach (var bytes in Mutations(basis)) { Assert.ThrowsAny<JsonException>(() => Codec.ReadBase(bytes, t.Source)); baseCount++; }
        Assert.True(baseCount > 350);
        foreach (var cut in Enumerable.Range(0, 3))
        {
            var prefixInputs = inputs[..cut]; var prefixEvents = events[..cut];
            var state = Control.Replay(t.Source, prefixInputs, prefixEvents);
            var stateCount = 0;
            foreach (var bytes in Mutations(Codec.SerializeState(state)))
            { Assert.ThrowsAny<JsonException>(() => Codec.ReadState(bytes, t.Source, prefixInputs, prefixEvents)); stateCount++; }
            Assert.True(stateCount > 90);
        }
        for (var index = 0; index < 2; index++)
        {
            foreach (var bytes in Mutations(events[index]))
            {
                var altered = events.ToArray(); altered[index] = bytes;
                Assert.ThrowsAny<JsonException>(() => Control.Replay(t.Source, inputs, altered));
            }
            var forged = JsonNode.Parse(events[index])!;
            forged["effect"]!["assessment"]!["witnesses"]![0]!["afterCp"] = 1;
            forged.AsObject().Remove("receiptId");
            forged["receiptId"] = "cc." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.cycle-control-receipt.v1", Encoding.UTF8.GetBytes(forged.ToJsonString()))[7..];
            var alteredEvents = events.ToArray(); alteredEvents[index] = Encoding.UTF8.GetBytes(forged.ToJsonString());
            Assert.Throws<JsonException>(() => Control.Replay(t.Source, inputs, alteredEvents));
        }
        var forgedBase = JsonNode.Parse(basis)!;
        forgedBase["armedProof"]!["assessment"]!["candidateIds"]![0] = "cand.forged";
        var proofHash = Hash(Encoding.UTF8.GetBytes(forgedBase["armedProof"]!.ToJsonString()));
        forgedBase["armedProofHash"] = proofHash; forgedBase["controlBase"]!["profile"] = "inherited-armed-" + proofHash[7..];
        Assert.Throws<JsonException>(() => Codec.ReadBase(Encoding.UTF8.GetBytes(forgedBase.ToJsonString()), t.Source));
        var cache = JsonNode.Parse(Codec.SerializeState(t.State))!; cache["stateVersion"] = long.MaxValue;
        Assert.Throws<JsonException>(() => Control.Apply(t.Source, t.Inputs, t.Events, closing, Encoding.UTF8.GetBytes(cache.ToJsonString())));
        foreach (var bytes in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)) })
            Assert.ThrowsAny<JsonException>(() => Codec.ReadBase(bytes, t.Source));
        var tooMany = JsonNode.Parse(basis)!; tooMany["armedProof"]!["support"] = new JsonArray(Enumerable.Range(0, 513).Select(_ => (JsonNode?)JsonValue.Create(1)).ToArray());
        Assert.Throws<JsonException>(() => Codec.ReadBase(Encoding.UTF8.GetBytes(tooMany.ToJsonString()), t.Source));
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void SourceAdmissionAndOwnershipRemainBoundToActualRelease(string choice)
    {
        var release = Release(choice); var source = new CombatInheritedCycleControlSource(release.Source, release.Inputs, release.Events);
        var initial = Control.Replay(source, [], []); var bytes = Codec.SerializeState(initial);
        var capturedInput = release.Inputs[1]; var capturedEvent = release.Events[1].ToArray();
        release.Inputs[1] = capturedInput with { Actor = CampaignOpeningPreambleActor.System }; release.Events[1][0] ^= 1;
        Assert.Equal(bytes, Codec.SerializeState(Control.Replay(source, [], [])));
        Assert.Throws<JsonException>(() => new CombatInheritedCycleControlSource(release.Source, [], []));
        release.Events[1] = capturedEvent;
        Assert.Throws<JsonException>(() => Control.Replay(new(release.Source, release.Inputs, release.Events), [], []));
        foreach (var other in new[] { Release(choice, 1001, 1101), Release(choice, fallback: true) })
            Assert.Throws<JsonException>(() => Control.Replay(new(other.Source, other.Inputs, other.Events), [], []));
        var foreign = Release(choice == "act-first" ? "act-last" : "act-first");
        Assert.Throws<JsonException>(() => Codec.ReadState(bytes, new(foreign.Source, foreign.Inputs, foreign.Events), [], []));
        var retained = Build(choice);
        Assert.Throws<JsonException>(() => Control.Replay(new((retained with { Cycle = [] }).Source(), [release.Inputs[0], capturedInput, release.Inputs[2]], release.Events), [], []));
        var opening = new CombatCycleControlInput(Control.Command(initial, "open"), CampaignOpeningPreambleActor.System, 2100);
        var result = Control.Apply(source, [], [], opening); var original = result.EventBytes!; result.EventBytes![0] ^= 1;
        Assert.Equal(original, result.EventBytes);
        Codec.SerializeState(result.State)[0] ^= 1;
        Assert.Equal(bytes, Codec.SerializeState(initial));
        var exported = initial.Basis.ReleaseEvents[0]; exported[0] ^= 1;
        Assert.Equal(bytes, Codec.SerializeState(Control.Replay(source, [], [])));
    }
    private sealed record Opened(CombatInheritedCycleControlSource Source, CombatCycleControlInput[] Inputs, byte[][] Events, CombatCycleControlState State);
    private static Opened Open(string choice)
    {
        var r = Release(choice); var source = new CombatInheritedCycleControlSource(r.Source, r.Inputs, r.Events);
        var state = Control.Replay(source, [], []);
        var input = new CombatCycleControlInput(Control.Command(state, "open"), CampaignOpeningPreambleActor.System, 2100);
        var result = Control.Apply(source, [], [], input);
        return new(source, [input], [result.EventBytes!], result.State);
    }
    private static void AssertFinished(CombatCycleControlResult result, string reason)
    {
        Assert.Equal("finished", result.State.Status); Assert.Null(result.State.ActiveCycle);
        var node = JsonNode.Parse(result.EventBytes!)!;
        Assert.Equal("system", node["author"]!.GetValue<string>());
        Assert.Equal(reason, node["effect"]!["reason"]!.GetValue<string>());
        Assert.Equal("expired", result.State.Members[0].History.NextMovement!.Status);
        Assert.Equal(result.ReceiptId, result.State.Members[0].History.NextMovement!.CompletionReceiptId);
    }

    private static CampaignOpeningPreambleActor Owner(string choice) => choice == "act-first" ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth;

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
