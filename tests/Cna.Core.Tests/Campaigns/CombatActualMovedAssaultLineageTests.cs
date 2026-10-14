using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualMovedAssaultLineageTests
{
    [Fact]
    public void ActualParentAuthenticatesBeforeN1AndIndependentAncestorLedgersCannotBeSubstituted()
    {
        var context = new Context("axis-timeout-defender-selected");
        using var source = JsonDocument.Parse(context.Source);
        var parent = Encoding.UTF8.GetBytes(source.RootElement.GetProperty("reactionEntrySourceCanonicalUtf8").GetString()!);
        var actual = CampaignCombatActualReactionCombatEntry.ReplayTrustedSource(parent, context.Request, context.Prefix, context.Lifecycle, context.Breakdown, Dependency);
        Assert.Equal(18, actual.Control.GetProperty("stateVersion").GetInt64());
        Assert.NotNull(actual.Candidate);
        context.Replay();
        foreach (var ledger in new[] { context.Prefix, context.Lifecycle, context.Breakdown })
        {
            for (var i = 0; i < ledger.Length; i++)
            {
                var retained = ledger[i];
                var forged = JsonNode.Parse(retained)!;
                forged["actor"] = forged["actor"]!.GetValue<string>() == "system" ? "axis" : "system";
                ledger[i] = JsonSerializer.SerializeToUtf8Bytes(forged, Options);
                Assert.ThrowsAny<JsonException>(() => context.Replay());
                ledger[i] = retained;
            }
        }
        Assert.Equal(parent, actual.SourceBytes);
    }

    [Fact]
    public void EachStagedLedgerIsAuthenticatedAndCompactCutHashesRequireActualReplay()
    {
        var context = new Context("commonwealth-scripted-unavailable-defender-selected");
        for (var i = 0; i < context.Inputs.Length; i++)
        {
            var result = context.Step(i);
            var expected = context.Item.GetProperty("cuts")[i + 1];
            Assert.Equal(expected.GetProperty("source").GetProperty("sha256").GetString(), Digest(result.SourceBytes));
            Assert.Equal(expected.GetProperty("control").GetProperty("sha256").GetString(), Digest(Encoding.UTF8.GetBytes(result.Control.GetRawText())));
        }
        foreach (var ledger in new[] { context.Selection, context.Round, context.Paid })
        {
            var original = ledger[0];
            var forged = JsonNode.Parse(original)!;
            forged["input"]!["admittedAt"] = 9000;
            ledger[0] = JsonSerializer.SerializeToUtf8Bytes(forged, Options);
            Assert.ThrowsAny<JsonException>(() => context.Replay());
            ledger[0] = original;
            ledger.RemoveAt(0);
            Assert.ThrowsAny<JsonException>(() => context.Replay());
            ledger.Insert(0, original);
        }
    }

    [Fact]
    public void StagedCommandActorRetryAtTerminalAllowsValidChangedTimeWithoutRewritingHistory()
    {
        var context = new Context("axis-player-decline-attacker-selected");
        var events = new List<byte[]>();
        for (var i = 0; i < context.Inputs.Length; i++) events.Add(context.Step(i).EventBytes!);
        var terminal = context.Replay();
        foreach (var index in new[] { 0, 1, 7, 8, 12 })
        {
            var retryInput = JsonNode.Parse(context.Inputs[index])!;
            retryInput["input"]!["admittedAt"] = 45000;
            retryInput["input"]!["clockAvailable"] = false;
            var retry = context.Apply(JsonSerializer.SerializeToUtf8Bytes(retryInput, Options));
            Assert.Equal(events[index], retry.EventBytes);
            Assert.Equal(terminal.SourceBytes, retry.SourceBytes);
            Assert.Equal(terminal.Control.GetRawText(), retry.Control.GetRawText());
            Assert.Equal(context.Inputs[index], index < 7 ? context.Selection[index] : index < 12 ? context.Round[index - 7] : context.Paid[index - 12]);
        }
        var malformed = JsonNode.Parse(context.Inputs[12])!;
        malformed["input"]!["admittedAt"] = true;
        Assert.ThrowsAny<JsonException>(() => context.Apply(JsonSerializer.SerializeToUtf8Bytes(malformed, Options)));
        var foreign = JsonNode.Parse(context.Inputs[12])!;
        foreign["input"]!["actor"] = "axis";
        Assert.ThrowsAny<JsonException>(() => context.Apply(JsonSerializer.SerializeToUtf8Bytes(foreign, Options)));
    }

    [Fact]
    public void CallerBuffersAreSnapshottedBeforeDependencyCallbackAndReturnedBuffersAreOwned()
    {
        var context = new Context("axis-player-decline-attacker-selected");
        var originalSource = context.Source.ToArray();
        var originalInput = context.Prefix[0].ToArray();
        var changed = false;
        byte[] Acquire(string path)
        {
            if (!changed) { context.Source[0] = 0; context.Prefix[0][0] = 0; changed = true; }
            return Dependency(path);
        }
        var result = context.Replay(Acquire);
        Assert.True(changed);
        Assert.Equal(originalSource, result.SourceBytes);
        var exported = result.SourceBytes;
        exported[0] = 0;
        Assert.Equal(originalSource, result.SourceBytes);
        Assert.ThrowsAny<JsonException>(() => context.Replay());
        context.Source[0] = originalSource[0]; context.Prefix[0][0] = originalInput[0];
        Assert.Equal(originalSource, context.Replay().SourceBytes);
    }

    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

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
