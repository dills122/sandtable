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
    [Theory]
    [InlineData("paid", "axis-player-decline-attacker-selected", "finish")]
    [InlineData("paid", "axis-player-decline-attacker-selected", "repeat-intent")]
    [InlineData("armed", "commonwealth-scripted-unavailable-attacker-finish", "finish")]
    [InlineData("armed", "commonwealth-scripted-unavailable-attacker-finish", "repeat-intent")]
    public void FinishClosesRealCycleWhilePendingRepeatRetainsOccurrenceAndAllHistory(string profile,string name,string choice)
    {
        var c=new Context(profile,name);var parent=c.Parent();var current=c.Replay();
        foreach(var kind in new[]{"control-open",choice})
        {
            var input=Input(current.Control,kind);
            current=Apply(c,input);Assert.NotNull(current.EventBytes);c.Source=current.SourceBytes;c.ControlInputs.Add(input);
        }
        Assert.Equal(parent.Control.GetProperty("stateVersion").GetInt64()+2,current.Control.GetProperty("stateVersion").GetInt64());
        foreach(var field in new[]{"world","randomState","movementEnd","actualProgressRefs","members","attackHistory","targetUses"})
            Assert.Equal(parent.Control.GetProperty(field).GetRawText(),current.Control.GetProperty(field).GetRawText());
        if(choice=="finish")
        {
            Assert.Equal("finished",current.Control.GetProperty("phase").GetString());
            Assert.Equal(JsonValueKind.Null,current.Control.GetProperty("activeCycle").ValueKind);
            Assert.Equal(1,current.Control.GetProperty("closure").GetProperty("ordinal").GetInt32());
            Assert.Equal("land.position.operation-1.first-player.truck-convoy-movement",current.Control.GetProperty("sequencePosition").GetProperty("positionId").GetString());
        }
        else
        {
            Assert.Equal("repeat-intent-pending",current.Control.GetProperty("phase").GetString());
            Assert.Equal(parent.Control.GetProperty("cycle").GetRawText(),current.Control.GetProperty("activeCycle").GetRawText());
            Assert.Equal(parent.Control.GetProperty("sequencePosition").GetRawText(),current.Control.GetProperty("sequencePosition").GetRawText());
            Assert.False(current.Control.GetProperty("pendingRepeatIntent").GetProperty("executionAvailable").GetBoolean());
            Assert.Equal(JsonValueKind.Null,current.Control.GetProperty("closure").ValueKind);
        }
    }
    internal static byte[] Input(JsonElement s,string kind,long? now=1000,bool clock=true)
    {
        var timer=kind is "expire" or "unavailable";
        return JsonSerializer.SerializeToUtf8Bytes(new JsonObject
        {
            ["command"]=new JsonObject { ["contractVersion"]=1,["kind"]=kind,["parentSourceHash"]=s.GetProperty("parentSourceHash").GetString(),
                ["controlId"]=s.GetProperty("controlId").GetString(),["cycleId"]=s.GetProperty("cycleId").GetString(),["configurationHash"]=s.GetProperty("cycle").GetProperty("admittedPolicyBundleDigest").GetString(),
                ["expectedPriorVersion"]=timer ? null : s.GetProperty("stateVersion").GetInt64(),["decisionId"]=kind=="control-open" ? null : s.GetProperty("decisionId").GetString() },
            ["actor"]=kind is "finish" or "repeat-intent" ? s.GetProperty("cycle").GetProperty("actingSide").GetString() : "system",["admittedAt"]=now,["clockAvailable"]=clock
        },Options);
    }
    internal static CombatActualMovedCycleControlResult Apply(Context c,byte[] input,Func<string,byte[]>? dependency=null,bool fresh=true)
        => CampaignCombatActualMovedCycleControl.ApplyTrustedSource(c.Source,c.Request,c.Prefix,c.Lifecycle,c.Breakdown,c.Selection,c.Round,c.Paid,c.Release,c.ControlInputs,input,dependency??Dependency,fresh);
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
