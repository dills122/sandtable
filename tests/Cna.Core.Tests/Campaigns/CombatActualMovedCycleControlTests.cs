using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualMovedCycleControlTests
{
    [Theory]
    [InlineData("paid", "axis-player-decline-attacker-selected")]
    [InlineData("armed", "commonwealth-scripted-unavailable-attacker-finish")]
    public void AuthenticAssessedTerminalRequiresActualControlReader(string profile, string name)
    {
        var c = new Context(profile,name); var parent=c.Parent();
        Assert.Equal("assessed",parent.Control.GetProperty("phase").GetString());
        var result=c.Replay();
        Assert.Equal("control-unopened",result.Control.GetProperty("phase").GetString());
        foreach(var field in new[]{"world","randomState","movementEnd","actualProgressRefs","targetUses","attackHistory"})
            Assert.Equal(parent.Control.GetProperty(field).GetRawText(),result.Control.GetProperty(field).GetRawText());
        Assert.Equal(parent.Control.GetProperty("cycle").GetRawText(),result.Control.GetProperty("activeCycle").GetRawText());
    }
    internal sealed class Context
    {
        public CampaignCombatCreationRequest Request { get; }
        public byte[][] Prefix { get; }
        public byte[][] Lifecycle { get; }
        public byte[][] Breakdown { get; }
        public List<byte[]> Selection { get; } = [];
        public List<byte[]> Round { get; } = [];
        public List<byte[]> Paid { get; } = [];
        public List<byte[]> Release { get; } = [];
        public byte[] ParentSource { get; }
        public byte[] Source { get; set; }
        public Context(string profile, string name)
        {
            using var fixture = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-actual-moved-continuation-v1.json"));
            var item = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("name").GetString() == name);
            using var authority = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
            using var created = JsonDocument.Parse(authority.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
            var artifact = Cna1979CombatContentCatalog.Artifact; var scenario = Assert.Single(artifact.Definition.Scenarios);
            var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(created.RootElement.GetProperty("setup").GetRawText()), artifact, scenario);
            var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(created.RootElement.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
            Request = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", 1, new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, config));
            Prefix = Bytes(item.GetProperty("prefixInputCanonicalUtf8")); Lifecycle = Bytes(item.GetProperty("lifecycleInputCanonicalUtf8")); Breakdown = Bytes(item.GetProperty("breakdownInputCanonicalUtf8"));
            Selection.AddRange(Bytes(item.GetProperty("selectionInputCanonicalUtf8"))); Round.AddRange(Bytes(item.GetProperty("roundInputCanonicalUtf8"))); Paid.AddRange(Bytes(item.GetProperty("paidInputCanonicalUtf8"))); Release.AddRange(Bytes(item.GetProperty("inputCanonicalUtf8")));
            var packet = JsonNode.Parse(item.GetProperty("initialSourceCanonicalUtf8").GetString()!)!;
            foreach (var ev in item.GetProperty("eventCanonicalUtf8").EnumerateArray()) packet["releaseEventCanonicalUtf8"]!.AsArray().Add(ev.GetString());
            ParentSource = CampaignCombatActualMovedContinuationCodec.Canonical(packet, "Source");
            Source = JsonSerializer.SerializeToUtf8Bytes(new JsonObject { ["contractVersion"] = 1, ["continuationSourceCanonicalUtf8"] = Encoding.ASCII.GetString(ParentSource), ["controlEventCanonicalUtf8"] = new JsonArray() }, Options);
        }
        public CombatActualMovedContinuationResult Parent() => CampaignCombatActualMovedContinuation.ReplayTrustedSource(ParentSource, Request, Prefix, Lifecycle, Breakdown, Selection, Round, Paid, Release, Dependency);
        public CombatActualMovedCycleControlResult Replay() => CampaignCombatActualMovedCycleControl.ReplayTrustedSource(Source, Request, Prefix, Lifecycle, Breakdown, Selection, Round, Paid, Release, ControlInputs, Dependency);
        public List<byte[]> ControlInputs { get; } = [];
    }
    internal static byte[][] Bytes(JsonElement values) => values.EnumerateArray().Select(v => Encoding.UTF8.GetBytes(v.GetString()!)).ToArray();
    internal static byte[] Dependency(string path)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Sandtable.slnx"))) root = root.Parent;
        return File.ReadAllBytes(Path.Combine(root!.FullName, path));
    }
    private static readonly JsonSerializerOptions Options = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
}
