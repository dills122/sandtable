using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualMovedAssaultAccountingTests
{
    public static IEnumerable<object[]> GenuineRoots()
    {
        foreach (var owner in new[] { "axis", "commonwealth" })
            foreach (var closure in new[] { "player-decline", "scripted-unavailable", "timeout" })
                foreach (var firstSeal in new[] { "attacker", "defender" })
                    yield return [owner + "-" + closure + "-" + firstSeal + "-selected", owner];
    }

    [Theory]
    [MemberData(nameof(GenuineRoots))]
    public void GenuineCommitChargesCumulativeCostsOnceAndPreservesMovement(string name, string owner)
    {
        var context = new Context(name);
        var initial = context.Replay().Control;
        var progress = initial.GetProperty("actualProgressRefs").EnumerateArray().Select(x => x.GetRawText()).ToArray();
        Assert.Equal(2, progress.Length);
        var movement = initial.GetProperty("movementEnd").GetRawText();
        var cycle = initial.GetProperty("cycle").GetRawText();
        AssertResources(initial, owner, 4, 0, 10);
        for (var i = 0; i < 12; i++)
        {
            var beforeCommit = context.Step(i).Control;
            AssertResources(beforeCommit, owner, 4, 0, 10);
            Assert.Equal((ulong)2, beforeCommit.GetProperty("randomState").GetProperty("nextByteCursor").GetUInt64());
            Assert.Equal(progress, beforeCommit.GetProperty("actualProgressRefs").EnumerateArray().Select(x => x.GetRawText()).ToArray());
        }
        var preparedWorld = context.Replay().Control.GetProperty("world").Clone();
        var commit = context.Step(12);
        Assert.Equal(31, commit.Control.GetProperty("stateVersion").GetInt64());
        AssertResources(commit.Control, owner, 9, 3, 0);
        Assert.Equal((ulong)1, commit.Control.GetProperty("randomState").GetProperty("seed").GetUInt64());
        Assert.Equal((ulong)2, commit.Control.GetProperty("randomState").GetProperty("nextByteCursor").GetUInt64());
        foreach (var element in commit.Control.GetProperty("world").GetProperty("elements").EnumerateArray())
        {
            Assert.Equal(10, Assert.Single(element.GetProperty("components").EnumerateArray()).GetProperty("currentToe").GetInt32());
            var prior = preparedWorld.GetProperty("elements").EnumerateArray().Single(e => e.GetProperty("elementId").GetString() == element.GetProperty("elementId").GetString());
            var beforeOperational = JsonNode.Parse(prior.GetProperty("operationalState").GetRawText())!.AsObject();
            var afterOperational = JsonNode.Parse(element.GetProperty("operationalState").GetRawText())!.AsObject();
            beforeOperational.Remove("capabilityPointsExpended"); afterOperational.Remove("capabilityPointsExpended");
            Assert.Equal(beforeOperational.ToJsonString(), afterOperational.ToJsonString());
        }
        var afterProgress = commit.Control.GetProperty("actualProgressRefs").EnumerateArray().ToArray();
        Assert.Equal(3, afterProgress.Length);
        Assert.Equal(progress, afterProgress.Take(2).Select(x => x.GetRawText()).ToArray());
        Assert.Contains("moved-paid-attack-committed", afterProgress[2].GetRawText(), StringComparison.Ordinal);
        using var eventDocument = JsonDocument.Parse(commit.EventBytes!);
        var costs = eventDocument.RootElement.GetProperty("effect").GetProperty("costs").EnumerateArray().ToArray();
        Assert.Equal(2, costs.Length);
        AssertCost(costs[0], owner + "-assault-battalion", 4, 9);
        AssertCost(costs[1], (owner == "axis" ? "commonwealth" : "axis") + "-assault-battalion", 0, 3);
        var retry = context.Apply(context.Inputs[12]);
        Assert.Equal(commit.EventBytes, retry.EventBytes);
        Assert.Equal(commit.Control.GetRawText(), retry.Control.GetRawText());
        Assert.Equal(commit.SourceBytes, retry.SourceBytes);
        for (var i = 13; i < context.Inputs.Length; i++) context.Step(i);
        var terminal = context.Replay().Control;
        AssertResources(terminal, owner, 9, 3, 0);
        Assert.Equal(movement, terminal.GetProperty("movementEnd").GetRawText());
        Assert.Equal(cycle, terminal.GetProperty("cycle").GetRawText());
        Assert.Equal(38, terminal.GetProperty("stateVersion").GetInt64());
        Assert.True(terminal.GetProperty("closed").GetBoolean());
        Assert.Equal("land.position.operation-1.first-player.movement-and-combat.reserve-release", terminal.GetProperty("sequencePosition").GetProperty("positionId").GetString());
    }

    [Theory]
    [InlineData("axis-player-decline-attacker-opening-unavailable", "axis")]
    [InlineData("commonwealth-scripted-unavailable-attacker-finish", "commonwealth")]
    [InlineData("axis-player-decline-attacker-selected-empty-deadline", "axis")]
    [InlineData("commonwealth-timeout-attacker-selected-attacker-null", "commonwealth")]
    public void ArmedFallbackKeepsMovementCostsAndNeverInventsCommitment(string name, string owner)
    {
        var context = new Context(name);
        var initial = context.Replay().Control;
        for (var i = 0; i < context.Inputs.Length; i++)
        {
            var result = context.Step(i);
            AssertResources(result.Control, owner, 4, 0, 10);
            Assert.Equal((ulong)2, result.Control.GetProperty("randomState").GetProperty("nextByteCursor").GetUInt64());
            Assert.Equal(initial.GetProperty("actualProgressRefs").GetRawText(), result.Control.GetProperty("actualProgressRefs").GetRawText());
            Assert.DoesNotContain("moved-paid-attack-committed", Encoding.UTF8.GetString(result.EventBytes!), StringComparison.Ordinal);
        }
        Assert.True(context.Replay().Control.GetProperty("closed").GetBoolean());
    }

    private static void AssertResources(JsonElement control, string owner, int attackerCp, int defenderCp, int ammo)
    {
        foreach (var element in control.GetProperty("world").GetProperty("elements").EnumerateArray())
        {
            var attacker = element.GetProperty("elementId").GetString() == owner + "-assault-battalion";
            var cp = element.GetProperty("operationalState").GetProperty("capabilityPointsExpended");
            Assert.Equal(attacker ? attackerCp : defenderCp, cp.GetProperty("numerator").GetInt32());
            Assert.Equal(1, cp.GetProperty("denominator").GetInt32());
            Assert.Equal(ammo, element.GetProperty("ammunition").GetProperty("points").GetInt32());
        }
    }
    private static void AssertCost(JsonElement cost, string element, int before, int after)
    {
        Assert.Equal(element, cost.GetProperty("unit").GetProperty("elementId").GetString());
        Assert.Equal(before, cost.GetProperty("beforeCp").GetInt32());
        Assert.Equal(after, cost.GetProperty("afterCp").GetInt32());
        Assert.Equal(10, cost.GetProperty("beforeAmmo").GetInt32());
        Assert.Equal(0, cost.GetProperty("afterAmmo").GetInt32());
    }

    // Trusted ledgers come from the independently retained fixture inputs, never from event payloads.
    private sealed class Context
    {
        public JsonElement Item { get; }
        public CampaignCombatCreationRequest Request { get; }
        public byte[][] Prefix { get; }
        public byte[][] Lifecycle { get; }
        public byte[][] Breakdown { get; }
        public List<byte[]> Selection { get; } = [];
        public List<byte[]> Round { get; } = [];
        public List<byte[]> Paid { get; } = [];
        public byte[] Source { get; private set; }
        public byte[][] Inputs { get; }

        public Context(string name)
        {
            using var fixture = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-actual-moved-assault-v1.json"));
            Item = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(c => c.GetProperty("name").GetString() == name).Clone();
            using var authority = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
            using var created = JsonDocument.Parse(authority.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
            var artifact = Cna1979CombatContentCatalog.Artifact;
            var scenario = Assert.Single(artifact.Definition.Scenarios);
            var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(created.RootElement.GetProperty("setup").GetRawText()), artifact, scenario);
            var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(created.RootElement.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
            Request = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", 1,
                new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, config));
            Prefix = Bytes(Item.GetProperty("prefixInputCanonicalUtf8"));
            Lifecycle = Bytes(Item.GetProperty("lifecycleInputCanonicalUtf8"));
            Breakdown = Bytes(Item.GetProperty("breakdownInputCanonicalUtf8"));
            Inputs = Bytes(Item.GetProperty("inputCanonicalUtf8"));
            Source = Encoding.UTF8.GetBytes(Item.GetProperty("initialSourceCanonicalUtf8").GetString()!);
        }

        public CombatActualMovedAssaultResult Replay(Func<string, byte[]>? dependency = null) =>
            CampaignCombatActualMovedAssault.ReplayTrustedSource(Source, Request, Prefix, Lifecycle, Breakdown, Selection, Round, Paid, dependency ?? Dependency);

        public CombatActualMovedAssaultResult Apply(byte[] input) =>
            CampaignCombatActualMovedAssault.ApplyTrustedSource(Source, Request, Prefix, Lifecycle, Breakdown, Selection, Round, Paid, input, Dependency);

        public CombatActualMovedAssaultResult Step(int index)
        {
            var result = Apply(Inputs[index]);
            using var envelope = JsonDocument.Parse(Inputs[index]);
            var phase = envelope.RootElement.GetProperty("phase").GetString();
            (phase == "selection" ? Selection : phase == "round" ? Round : Paid).Add(Inputs[index].ToArray());
            Source = result.SourceBytes;
            return result;
        }
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
