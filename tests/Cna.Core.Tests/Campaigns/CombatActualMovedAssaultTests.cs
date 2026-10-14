using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Security.Cryptography;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
namespace Cna.Core.Tests.Campaigns;
public sealed class CombatActualMovedAssaultTests
{
    [Theory]
    [InlineData("act-first", "axis-player-decline")]
    [InlineData("act-last", "commonwealth-player-decline")]
    public void AuthenticMovedSourceAdmitsInitialSelectionWithoutResettingCp(string choice, string name)
    {
        var p = Ready(1, choice);
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-actual-reaction-combat-entry-v1.json")));
        var item = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("name").GetString() == name);
        var parent = Encoding.ASCII.GetBytes(item.GetProperty("cuts")[4].GetProperty("sourceCanonicalUtf8").GetString()!);
        static byte[][] Inputs(JsonElement array) => array.EnumerateArray().Select(e => Encoding.ASCII.GetBytes(e.GetString()!)).ToArray();
        var prefix = Inputs(item.GetProperty("prefixInputCanonicalUtf8")); var suffix = Inputs(item.GetProperty("inputCanonicalUtf8"));
        var authenticated = CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(parent, p.Request, prefix, suffix[..3], suffix[3..], Dependency);
        Assert.NotNull(authenticated.Candidate);
        var source = JsonSerializer.SerializeToUtf8Bytes(new JsonObject { ["contractVersion"] = 1,
            ["reactionEntrySourceCanonicalUtf8"] = Encoding.ASCII.GetString(authenticated.SourceBytes),
            ["selectionEventCanonicalUtf8"] = new JsonArray(), ["roundEventCanonicalUtf8"] = new JsonArray(), ["paidEventCanonicalUtf8"] = new JsonArray() }, Options);
        var result = CampaignCombatActualMovedAssault.ReplayTrustedSource(source, p.Request, prefix, suffix[..3], suffix[3..], [], [], [], Dependency);
        Assert.Equal(18, result.Control.GetProperty("stateVersion").GetInt64());
        Assert.Equal("selection", result.Control.GetProperty("phase").GetString());
        var owner = choice == "act-first" ? "axis-assault-battalion" : "commonwealth-assault-battalion";
        Assert.Equal(4, result.Control.GetProperty("world").GetProperty("elements").EnumerateArray().Single(e => e.GetProperty("elementId").GetString() == owner)
            .GetProperty("operationalState").GetProperty("capabilityPointsExpended").GetProperty("numerator").GetInt32());
    }
    private static readonly JsonSerializerOptions Options = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
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
        ulong seed = 1, string choice = "act-first", bool designated = false, string campaignId = "rules-lab.combat-creation.1")
    {
        var (request, created, opening, weather, stage) = Chain(seed, choice, campaignId);
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
    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather, byte[][] Stage) Chain(ulong seed = 1, string choice = "act-first", string campaignId = "rules-lab.combat-creation.1")
    {
        var (request, created, opening, weather) = Predecessor(seed, choice, campaignId);
        var stage = new List<byte[]>();
        for (var i = 0; i < 4; i++)
        {
            var state = CampaignCombatStageEntry.Replay(request, created, opening, [weather], stage);
            stage.Add(CampaignCombatStageEntry.Apply(request, created, opening, [weather], stage, CampaignCombatStageEntry.Command(state)).EventBytes);
        }
        return (request, created, opening, weather, stage.ToArray());
    }
    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Opening, byte[] Weather) Predecessor(
        ulong seed = 1, string choice = "act-first", string campaignId = "rules-lab.combat-creation.1")
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
        using var golden = JsonDocument.Parse(fixture.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
        var root = golden.RootElement;
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("setup").GetRawText()), artifact, scenario);
        var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(root.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
        var request = CampaignCombatCreationRequest.Create(campaignId, seed,
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

    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();    private static byte[] Dependency(string path)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Sandtable.slnx"))) root = root.Parent;
        return File.ReadAllBytes(Path.Combine(root!.FullName, path));
    }
}
