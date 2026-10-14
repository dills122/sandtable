using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualReactionCombatEntryTests
{
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void AuthenticRebuiltWorldCertifiesPositiveCp4Geometry(string choice)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        foreach (var kind in new[] { "decline-reaction-window", "close-reaction-window-scripted-unavailable", "close-reaction-window-timeout" })
        {
            var before = Replay(p, moves, trigger, []);
            var after = Apply(p, moves, trigger, [], CampaignCombatReactionClosure.Command(before, kind)).State;
            var cycle = after.Opening.Cycle!;
            var candidate = CampaignCombatCertification.CertifyInitialProfileFacts(p.Request, p.Created, after.World, cycle, cycle.ActingSide, WeatherKind.Normal, WeatherKind.Normal);
            Assert.NotNull(candidate);
            Assert.Equal(new CapabilityPointAmount(4, 1), after.World.Elements.Single(e => e.ElementId == candidate.Attacker.Unit.ElementId).OperationalState.CapabilityPointsExpended);
            Assert.Equal(candidate.Defender.LocationId, candidate.TargetLocationId);
            Assert.Equal(14, after.StateVersion);
            using var bytes = JsonDocument.Parse(CampaignCombatReactionClosureCodec.SerializeState(after));
            Assert.Equal((ulong)1, bytes.RootElement.GetProperty("randomState").GetProperty("seed").GetUInt64());
            Assert.Equal((ulong)2, bytes.RootElement.GetProperty("randomState").GetProperty("nextByteCursor").GetUInt64());
            Assert.Equal(13, after.Receipts.Count);
        }
    }
    [Theory]
    [InlineData("act-first")]
    [InlineData("act-last")]
    public void AuthenticatedReactionClosureReachesCertifiedCombatEntryWithoutResettingCp(string choice)
    {
        var p = Ready(1, choice); var moves = FirstMove(p, choice); var trigger = Trigger(p, moves);
        var before = Replay(p, moves, trigger, []);
        var closure = Apply(p, moves, trigger, [], CampaignCombatReactionClosure.Command(before, "decline-reaction-window"));
        var prefix = p.Opening.Concat(new[] { p.Weather }).Concat(p.Stage).Concat(p.Reserve).Concat(moves).Concat(trigger).Append(closure.EventBytes).ToArray();
        var trusted = prefix.Select(InputBytes).ToArray();
        var source = Source(p.Created, prefix); var suffix = new List<byte[]>();
        var current = CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(source, p.Request, trusted, suffix);
        Assert.Equal(14, current.Control.GetProperty("stateVersion").GetInt64());
        for (var i = 0; i < 4; i++)
        {
            var input = current.CommandBytes;
            current = CampaignCombatActualReactionCombatEntry.ApplyTrustedSource(current.SourceBytes, p.Request, trusted, suffix, input);
            suffix.Add(input);
        }
        Assert.Equal(18, current.Control.GetProperty("stateVersion").GetInt64());
        Assert.Equal("land.position.operation-1.first-player.movement-and-combat.combat.position-determination", current.Control.GetProperty("sequencePosition").GetProperty("positionId").GetString());
        Assert.Equal(17, current.Control.GetProperty("receipts").GetArrayLength());
        Assert.Equal("idle", current.Control.GetProperty("breakdownFlow").GetProperty("kind").GetString());
        Assert.NotNull(current.Candidate);
        Assert.Equal(new CapabilityPointAmount(4, 1), current.World.Elements.Single(e => e.ElementId == current.Candidate!.Attacker.Unit.ElementId).OperationalState.CapabilityPointsExpended);
    }
    private static byte[] InputBytes(byte[] ev)
    { using var d = JsonDocument.Parse(ev); return Encoding.UTF8.GetBytes(d.RootElement.GetProperty("input").GetRawText()); }
    private static readonly JsonSerializerOptions SourceOptions = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static byte[] Source(byte[] created, byte[][] events)
    {
        var obj = new System.Text.Json.Nodes.JsonObject { ["contractVersion"] = 1, ["createdCanonicalUtf8"] = Encoding.ASCII.GetString(created),
            ["prefixEventCanonicalUtf8"] = new System.Text.Json.Nodes.JsonArray(events.Select(e => (System.Text.Json.Nodes.JsonNode?)System.Text.Json.Nodes.JsonValue.Create(Encoding.ASCII.GetString(e))).ToArray()),
            ["entryEventCanonicalUtf8"] = new System.Text.Json.Nodes.JsonArray() };
        return System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(obj, SourceOptions);
    }
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
    private static CampaignCombatReactionClosureState Replay(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events) => CampaignCombatReactionClosure.Replay(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, events);
    private static CampaignCombatReactionClosureResult Apply(Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events, CampaignCombatReactionClosureInput input) => CampaignCombatReactionClosure.Apply(p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, events, input);
    private static CampaignCombatReactionClosureState Read(byte[] bytes, Pack p, IReadOnlyList<byte[]> moves, IReadOnlyList<byte[]> trigger, IReadOnlyList<byte[]> events) => CampaignCombatReactionClosure.ReadState(bytes, p.Request, p.Created, p.Opening, [p.Weather], p.Stage, p.Reserve, moves, trigger, events);
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

    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
}
