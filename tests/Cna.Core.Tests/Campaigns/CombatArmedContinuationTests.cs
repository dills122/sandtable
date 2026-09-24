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

public sealed class CombatArmedContinuationTests
{
    [Theory]
    [InlineData("act-first", "axis")]
    [InlineData("act-last", "commonwealth")]
    public void AuthenticatedReleaseDerivesFrozenArmedProof(string choice, string actor)
    {
        var trace = Release(choice);
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-inherited-armed-continuation-v1.json")));
        var row = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(r => r.GetProperty("actor").GetString() == actor);
        var proof = CampaignCombatArmedContinuation.Derive(trace.Source, trace.Inputs, trace.Events);
        var bytes = CampaignCombatArmedContinuationCodec.Serialize(proof);
        Assert.Equal(row.GetProperty("golden").GetProperty("bytes").GetInt32(), bytes.Length);
        Assert.Equal(row.GetProperty("golden").GetProperty("sha256").GetString(), Hash(bytes));
        Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(row.GetProperty("proof")), bytes);
        Assert.Equal(bytes, CampaignCombatArmedContinuationCodec.Serialize(CampaignCombatArmedContinuationCodec.Read(bytes, trace.Source, trace.Inputs, trace.Events)));
    }

    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void ProofRetainsResourcesProgressAndCandidateWithoutAdvancingAuthority(string choice)
    {
        var t = Release(choice); var before = Bridge.Replay(t.Source, t.Inputs, t.Events);
        var world = JsonSerializer.SerializeToUtf8Bytes(before.World); var release = CampaignCombatReserveReleaseCodec.SerializeState(before.Release);
        var proof = CampaignCombatArmedContinuation.Derive(t.Source, t.Inputs, t.Events);
        var bytes = CampaignCombatArmedContinuationCodec.Serialize(proof); var node = JsonNode.Parse(bytes)!;
        Assert.Equal(world, JsonSerializer.SerializeToUtf8Bytes(proof.Source.World));
        Assert.Equal(release, CampaignCombatReserveReleaseCodec.SerializeState(proof.Source.Release));
        Assert.Equal(25, proof.Source.Release.StateVersion); Assert.Equal(1, proof.Source.Base.ReleaseBase.Cycle.Ordinal);
        Assert.Equal(before.Release.RandomState, proof.Source.Release.RandomState);
        Assert.Equal(before.Release.Members[0].Unit, proof.Candidate.Attacker.Unit);
        Assert.Equal(before.Release.Members[0].History.ReleaseReceiptId, node["progress"]!["receiptId"]!.GetValue<string>());
        Assert.Equal(Hash(t.Events[1]), node["progress"]!["eventHash"]!.GetValue<string>());
        Assert.Equal(Hash(Bridge.SerializeControl(before, t.Source.Request)), node["predecessorHash"]!.GetValue<string>());
        Assert.Equal(before.Base.Predecessor.MovementEnd!.CompletionReceiptId, node["movementCompletionReceiptId"]!.GetValue<string>());
        Assert.Empty(before.Release.AttackHistory);
        Assert.Equal("pending", proof.Source.Release.Members[0].History.NextMovement!.Status);
        foreach (var support in node["support"]!.AsArray())
        {
            var id = support!["contractId"]!.GetValue<string>();
            var fixture = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", id + ".json"));
            Assert.Equal(Hash(fixture), support["fixtureHash"]!.GetValue<string>());
            using var doc = JsonDocument.Parse(fixture);
            var count = id == "combat-snapshot-composition-v1" ? doc.RootElement.GetProperty("expected").EnumerateObject().Sum(v => v.Value.GetInt32())
                : doc.RootElement.GetProperty("cases").GetArrayLength();
            Assert.Equal(count, support["evidenceCount"]!.GetValue<int>());
        }
        Assert.Equal(Hash(Encoding.UTF8.GetBytes(node["support"]!.ToJsonString())), node["supportDigest"]!.GetValue<string>());
        bytes[0] ^= 1;
        Assert.NotEqual(bytes, CampaignCombatArmedContinuationCodec.Serialize(proof));
        Assert.Equal(release, CampaignCombatReserveReleaseCodec.SerializeState(Bridge.Replay(t.Source, t.Inputs, t.Events).Release));
    }

    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void EveryChangedProofLeafAndRehashedSupportRejectAgainstOriginalSource(string choice)
    {
        var t = Release(choice); var proof = CampaignCombatArmedContinuation.Derive(t.Source, t.Inputs, t.Events);
        var bytes = CampaignCombatArmedContinuationCodec.Serialize(proof);
        var rejected = 0;
        foreach (var changed in Mutations(bytes))
        { Assert.ThrowsAny<JsonException>(() => CampaignCombatArmedContinuationCodec.Read(changed, t.Source, t.Inputs, t.Events)); rejected++; }
        Assert.True(rejected > 75);
        var forged = JsonNode.Parse(bytes)!;
        forged["support"]![0]!["evidenceCount"] = 6;
        forged["supportDigest"] = Hash(Encoding.UTF8.GetBytes(forged["support"]!.ToJsonString()));
        Assert.Throws<JsonException>(() => CampaignCombatArmedContinuationCodec.Read(Encoding.UTF8.GetBytes(forged.ToJsonString()), t.Source, t.Inputs, t.Events));
        foreach (var raw in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)) })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatArmedContinuationCodec.Read(raw, t.Source, t.Inputs, t.Events));
        var foreign = Release(choice == "act-first" ? "act-last" : "act-first");
        Assert.Throws<JsonException>(() => CampaignCombatArmedContinuationCodec.Read(bytes, foreign.Source, foreign.Inputs, foreign.Events));
    }

    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void MissingFallbackForeignChangedAndResignedHistoryCannotProveContinuation(string choice)
    {
        var t = Release(choice);
        for (var cut = 0; cut < 3; cut++)
            Assert.Throws<JsonException>(() => CampaignCombatArmedContinuation.Derive(t.Source, t.Inputs[..cut], t.Events[..cut]));
        Assert.Throws<JsonException>(() => CampaignCombatArmedContinuation.Derive(t.Source, t.Inputs[..2], t.Events));
        Assert.Throws<JsonException>(() => CampaignCombatArmedContinuation.Derive(t.Source, [.. t.Inputs, t.Inputs[2]], [.. t.Events, t.Events[2]]));
        var wrong = t.Inputs.ToArray(); wrong[1] = wrong[1] with { Actor = CampaignOpeningPreambleActor.System };
        Assert.Throws<JsonException>(() => CampaignCombatArmedContinuation.Derive(t.Source, wrong, t.Events));
        var foreign = Source(choice == "act-first" ? "act-last" : "act-first");
        Assert.Throws<JsonException>(() => CampaignCombatArmedContinuation.Derive(foreign, t.Inputs, t.Events));
        var otherTime = Release(choice, 1001, 1101);
        Assert.True(Bridge.Replay(otherTime.Source, otherTime.Inputs, otherTime.Events).Closed);
        Assert.Throws<JsonException>(() => CampaignCombatArmedContinuation.Derive(otherTime.Source, otherTime.Inputs, otherTime.Events));
        var fallback = Release(choice, fallback: true);
        Assert.True(Bridge.Replay(fallback.Source, fallback.Inputs, fallback.Events).Closed);
        Assert.Throws<JsonException>(() => CampaignCombatArmedContinuation.Derive(fallback.Source, fallback.Inputs, fallback.Events));
        var eventNode = JsonNode.Parse(t.Events[1])!;
        eventNode["effect"]!["voluntaryCeiling"] = 15;
        eventNode.AsObject().Remove("receiptId");
        eventNode["receiptId"] = "rr." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.reserve-release-receipt.v1", Encoding.UTF8.GetBytes(eventNode.ToJsonString()))[7..];
        Assert.Throws<JsonException>(() => CampaignCombatArmedContinuation.Derive(t.Source, t.Inputs, [t.Events[0], Encoding.UTF8.GetBytes(eventNode.ToJsonString()), t.Events[2]]));
        var retained = Build(choice);
        foreach (var shortened in new[] { retained with { Cycle = [] }, retained with { Reserve = [] }, retained with { Stage = [] }, retained with { Preamble = [] } })
            Assert.Throws<JsonException>(() => CampaignCombatArmedContinuation.Derive(shortened.Source(), t.Inputs, t.Events));
        var captured = retained.Source();
        retained.Cycle[0][0] ^= 1;
        Assert.ThrowsAny<JsonException>(() => CampaignCombatArmedContinuation.Derive(retained.Source(), t.Inputs, t.Events));
        Assert.NotNull(CampaignCombatArmedContinuation.Derive(captured, t.Inputs, t.Events));
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
