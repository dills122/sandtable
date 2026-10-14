using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualMovedContinuationTests
{
    [Theory]
    [InlineData("paid", "axis-player-decline-attacker-selected", "axis", 9, 0)]
    [InlineData("armed", "commonwealth-scripted-unavailable-attacker-finish", "commonwealth", 4, 10)]
    public void AuthenticTerminalAdmitsWithoutResettingResourcesOrProgress(string profile, string name, string owner, int cp, int ammo)
    {
        var context = new Context(profile, name);
        var parent = context.Parent();
        Assert.True(parent.Control.GetProperty("closed").GetBoolean());
        var current = context.Replay();
        Assert.Equal(parent.Control.GetProperty("stateVersion").GetInt64(), current.Control.GetProperty("stateVersion").GetInt64());
        Assert.Equal("release-unopened", current.Control.GetProperty("phase").GetString());
        AssertPreserved(parent.Control, current.Control, owner, cp, ammo);
    }
    [Theory]
    [InlineData("paid", "axis-player-decline-attacker-selected", "axis", 9, 0)]
    [InlineData("armed", "commonwealth-scripted-unavailable-attacker-finish", "commonwealth", 4, 10)]
    public void ExactlyTwoSystemReleaseEventsProduceImmutableAssessmentWithoutCostOrControlWindow(string profile, string name, string owner, int cp, int ammo)
    {
        var context = new Context(profile, name); var parent = context.Parent(); var current = context.Replay();
        foreach (var kind in new[] { "open-empty-release", "complete-empty-release" })
        {
            var input = JsonSerializer.SerializeToUtf8Bytes(new JsonObject
            {
                ["command"] = new JsonObject
                {
                    ["contractVersion"] = 1,
                    ["kind"] = kind,
                    ["parentSourceHash"] = "sha256:" + Convert.ToHexStringLower(SHA256.HashData(context.ParentSource)),
                    ["releaseId"] = current.Control.GetProperty("releaseId").GetString(),
                    ["expectedPriorVersion"] = current.Control.GetProperty("stateVersion").GetInt64()
                },
                ["actor"] = "system"
            }, Options);
            current = CampaignCombatActualMovedContinuation.ApplyTrustedSource(context.Source, context.Request, context.Prefix, context.Lifecycle,
                context.Breakdown, context.Selection, context.Round, context.Paid, context.Release, input, Dependency);
            Assert.NotNull(current.EventBytes); context.Source = current.SourceBytes; context.Release.Add(input);
            AssertPreserved(parent.Control, current.Control, owner, cp, ammo);
        }
        Assert.Equal(parent.Control.GetProperty("stateVersion").GetInt64() + 2, current.Control.GetProperty("stateVersion").GetInt64());
        Assert.Equal("assessed", current.Control.GetProperty("phase").GetString());
        Assert.Equal(JsonValueKind.Null, current.Control.GetProperty("controlWindow").ValueKind);
        var assessment = current.Control.GetProperty("assessment");
        Assert.Equal(profile, assessment.GetProperty("mode").GetString());
        Assert.Equal("supported", assessment.GetProperty("witnessOutcome").GetString());
        Assert.False(assessment.GetProperty("executionAvailable").GetBoolean());
    }
    private static void AssertPreserved(JsonElement before, JsonElement after, string owner, int cp, int ammo)
    {
        foreach (var field in new[] { "world", "randomState", "movementEnd", "actualProgressRefs" })
            Assert.Equal(before.GetProperty(field).GetRawText(), after.GetProperty(field).GetRawText());
        var element = after.GetProperty("world").GetProperty("elements").EnumerateArray().Single(e => e.GetProperty("elementId").GetString() == owner + "-assault-battalion");
        Assert.Equal(cp, element.GetProperty("operationalState").GetProperty("capabilityPointsExpended").GetProperty("numerator").GetInt32());
        Assert.Equal(ammo, element.GetProperty("ammunition").GetProperty("points").GetInt32());
        Assert.Equal(ammo == 0 ? 3 : 2, after.GetProperty("actualProgressRefs").GetArrayLength());
    }
    private sealed class Context
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
            using var fixture = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-actual-moved-assault-v1.json"));
            var item = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("name").GetString() == name);
            using var authority = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
            using var created = JsonDocument.Parse(authority.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
            var artifact = Cna1979CombatContentCatalog.Artifact; var scenario = Assert.Single(artifact.Definition.Scenarios);
            var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(created.RootElement.GetProperty("setup").GetRawText()), artifact, scenario);
            var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(created.RootElement.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
            Request = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", 1, new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, config));
            Prefix = Bytes(item.GetProperty("prefixInputCanonicalUtf8")); Lifecycle = Bytes(item.GetProperty("lifecycleInputCanonicalUtf8")); Breakdown = Bytes(item.GetProperty("breakdownInputCanonicalUtf8"));
            var packet = JsonNode.Parse(item.GetProperty("initialSourceCanonicalUtf8").GetString()!)!;
            var inputs = Bytes(item.GetProperty("inputCanonicalUtf8"));
            for (var i = 0; i < inputs.Length; i++)
            {
                using var input = JsonDocument.Parse(inputs[i]); var phase = input.RootElement.GetProperty("phase").GetString()!;
                (phase == "selection" ? Selection : phase == "round" ? Round : Paid).Add(inputs[i]);
                packet[phase + "EventCanonicalUtf8"]!.AsArray().Add(item.GetProperty("eventCanonicalUtf8")[i].GetString());
            }
            ParentSource = CampaignCombatActualMovedAssaultCodec.Canonical(packet, "Source");
            Assert.Equal(item.GetProperty("cuts")[inputs.Length].GetProperty("source").GetProperty("sha256").GetString(), "sha256:" + Convert.ToHexStringLower(SHA256.HashData(ParentSource)));
            Source = JsonSerializer.SerializeToUtf8Bytes(new JsonObject
            {
                ["contractVersion"] = 1,
                ["profile"] = profile,
                ["movedAssaultSourceCanonicalUtf8"] = Encoding.ASCII.GetString(ParentSource),
                ["releaseEventCanonicalUtf8"] = new JsonArray()
            }, Options);
        }
        public CombatActualMovedAssaultResult Parent() => CampaignCombatActualMovedAssault.ReplayTrustedSource(ParentSource, Request, Prefix, Lifecycle, Breakdown, Selection, Round, Paid, Dependency);
        public CombatActualMovedContinuationResult Replay() => CampaignCombatActualMovedContinuation.ReplayTrustedSource(Source, Request, Prefix, Lifecycle, Breakdown, Selection, Round, Paid, Release, Dependency);
    }
    private static byte[][] Bytes(JsonElement values) => values.EnumerateArray().Select(v => Encoding.UTF8.GetBytes(v.GetString()!)).ToArray();
    private static byte[] Dependency(string path)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "Sandtable.slnx"))) root = root.Parent;
        return File.ReadAllBytes(Path.Combine(root!.FullName, path));
    }
    private static readonly JsonSerializerOptions Options = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
}
