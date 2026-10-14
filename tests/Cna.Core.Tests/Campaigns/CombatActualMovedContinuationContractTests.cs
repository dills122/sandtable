using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualMovedContinuationContractTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var fixture = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-actual-moved-continuation-v1.json"));
        return fixture.RootElement.GetProperty("cases").EnumerateArray().Select((c, index) => new object[] { index, c.GetProperty("profile").GetString()!, c.GetProperty("name").GetString()! }).ToArray();
    }
    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryGenuineParentHasExactThreeCutSourceControlProofAndEventParity(int _, string profile, string name)
    {
        var c = new Context(profile, name); using var fixture = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-actual-moved-continuation-v1.json"));
        var item = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(x => x.GetProperty("name").GetString() == name);
        var inputs = Bytes(item.GetProperty("inputCanonicalUtf8")); var current = c.Replay();
        for (var index = 0; index <= 2; index++)
        {
            Check(item.GetProperty("cuts")[index], "source", current.SourceBytes); Check(item.GetProperty("cuts")[index], "control", current.ControlBytes); Check(item.GetProperty("cuts")[index], "proof", current.ProofBytes);
            if (index == 2) break;
            current = Apply(c, inputs[index]); Assert.Equal(Encoding.ASCII.GetBytes(item.GetProperty("eventCanonicalUtf8")[index].GetString()!), current.EventBytes);
            c.Source = current.SourceBytes; c.Release.Add(inputs[index]);
        }
        Assert.Equal("assessed", current.Control.GetProperty("phase").GetString()); Assert.Equal(current.ProofBytes, c.Replay().ProofBytes);
        Assert.Equal(current.SourceBytes, CampaignCombatActualMovedContinuation.ReadSource(c.Source, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release, Dependency));
        Assert.Equal(current.ProofBytes, CampaignCombatActualMovedContinuation.ReadProof(current.ProofBytes, c.Source, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release, Dependency).ProofBytes);
    }
    private static CombatActualMovedContinuationResult Apply(Context c, byte[] input, bool enabled = true, Func<string, byte[]>? callback = null)
        => CampaignCombatActualMovedContinuation.ApplyTrustedSource(c.Source, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release, input, callback ?? Dependency, enabled);
    private static byte[] Input(Context c, string kind)
    {
        var s = c.Replay().Control;
        return CampaignCombatActualMovedContinuationCodec.Canonical(new JsonObject
        {
            ["command"] = new JsonObject
            {
                ["contractVersion"] = 1,
                ["kind"] = kind,
                ["parentSourceHash"] = s.GetProperty("parentSourceHash").GetString(),
                ["releaseId"] = s.GetProperty("releaseId").GetString(),
                ["expectedPriorVersion"] = s.GetProperty("stateVersion").GetInt64()
            },
            ["actor"] = "system"
        }, "Input");
    }
    private static void Check(JsonElement cut, string kind, byte[] bytes)
    { Assert.Equal(cut.GetProperty(kind).GetProperty("bytes").GetInt32(), bytes.Length); Assert.Equal(cut.GetProperty(kind).GetProperty("sha256").GetString(), "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes))); }
    [Theory]
    [InlineData("paid", "axis-player-decline-attacker-selected")]
    [InlineData("armed", "commonwealth-scripted-unavailable-attacker-finish")]
    public void RetryAndAlreadyOpenedCompletionRecoverWithFreshAdmissionDisabled(string profile, string name)
    {
        var c = new Context(profile, name); var open = Input(c, "open-empty-release"); Assert.Throws<JsonException>(() => Apply(c, open, false));
        var first = Apply(c, open); c.Source = first.SourceBytes; c.Release.Add(open);
        var retry = Apply(c, open, false); Assert.True(retry.Duplicate); Assert.Equal(first.EventBytes, retry.EventBytes); Assert.Equal(first.ProofBytes, retry.ProofBytes);
        var complete = Input(c, "complete-empty-release"); var last = Apply(c, complete, false); c.Source = last.SourceBytes; c.Release.Add(complete);
        retry = Apply(c, open, false); Assert.True(retry.Duplicate); Assert.Equal(first.EventBytes, retry.EventBytes); Assert.Equal(last.SourceBytes, retry.SourceBytes); Assert.Equal(last.ProofBytes, retry.ProofBytes);
        Assert.Equal(last.ProofBytes, c.Replay().ProofBytes);
    }
    [Fact]
    public void ChangedActorCommandContextOrForbiddenControlInputCannotRetryOrPublish()
    {
        var c = new Context("paid", "axis-player-decline-attacker-selected"); var open = Input(c, "open-empty-release"); var first = Apply(c, open); c.Source = first.SourceBytes; c.Release.Add(open);
        foreach (var field in new[] { "actor", "parentSourceHash", "releaseId", "kind" })
        {
            var forged = JsonNode.Parse(open)!;
            if (field == "actor") forged[field] = "axis";
            else forged["command"]![field] = field == "parentSourceHash" ? "sha256:" + new string('0', 64) : field == "kind" ? "finish" : "foreign.release";
            Assert.ThrowsAny<JsonException>(() => Apply(c, Encoding.UTF8.GetBytes(forged.ToJsonString())));
        }
        Assert.Equal(first.SourceBytes, c.Source);
    }
    [Fact]
    public void ClaimsAndSourceProfileNeverSubstituteForAuthenticatedParent()
    {
        var c = new Context("paid", "axis-player-decline-attacker-selected"); var result = c.Replay();
        var forged = JsonNode.Parse(c.Source)!; forged["profile"] = "armed";
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedContinuation.ReplayTrustedSource(CampaignCombatActualMovedContinuationCodec.Canonical(forged, "Source"), c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release, Dependency));
        var claim = JsonNode.Parse(result.ControlBytes)!; claim["stateVersion"] = 99;
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedContinuation.ReadControl(CampaignCombatActualMovedContinuationCodec.Canonical(claim, "Control"), c.Source, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release, Dependency));
        claim = JsonNode.Parse(result.ProofBytes)!; claim["sourceHash"] = "sha256:" + new string('0', 64);
        Assert.Throws<JsonException>(() => CampaignCombatActualMovedContinuation.ReadProof(CampaignCombatActualMovedContinuationCodec.Canonical(claim, "Proof"), c.Source, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release, Dependency));
    }
    [Fact]
    public void AllSevenLedgersAndCurrentInputAreOwnedBeforeDependencyCallbacks()
    {
        var c = new Context("paid", "axis-player-decline-attacker-selected"); var open = Input(c, "open-empty-release"); var first = Apply(c, open); c.Source = first.SourceBytes; c.Release.Add(open);
        var complete = Input(c, "complete-empty-release"); var lists = new[] { c.Prefix, c.Lifecycle, c.Breakdown, c.Selection.ToArray(), c.Round.ToArray(), c.Paid.ToArray(), c.Release.ToArray() };
        var originals = lists.Select(l => l.Select(b => b.ToArray()).ToArray()).ToArray(); var called = false;
        var result = Apply(c, complete, callback: path => { if (!called) { called = true; foreach (var l in lists) l[0][0] = 0; c.Source[0] = 0; complete[0] = 0; } return Dependency(path); });
        Assert.Equal("assessed", result.Control.GetProperty("phase").GetString());
        var returned = new[] { result.PrefixInputs, result.LifecycleInputs, result.BreakdownInputs, result.SelectionInputs, result.RoundInputs, result.PaidInputs, result.ReleaseInputs };
        for (var i = 0; i < 7; i++) for (var j = 0; j < originals[i].Length; j++) Assert.Equal(originals[i][j], returned[i][j]);
        var source = result.SourceBytes; source[0] = 0; var proof = result.ProofBytes; proof[0] = 0; returned[0][0][0] = 0;
        Assert.NotEqual((byte)0, result.SourceBytes[0]); Assert.NotEqual((byte)0, result.ProofBytes[0]); Assert.Equal(originals[0][0], result.PrefixInputs[0]);
    }
    [Fact]
    public void EachAncestorAndReleaseLedgerCannotBeChangedOrOmitted()
    {
        var c = new Context("paid", "axis-player-decline-attacker-selected"); var open = Input(c, "open-empty-release"); var result = Apply(c, open); c.Source = result.SourceBytes; c.Release.Add(open);
        foreach (var l in new[] { c.Prefix, c.Lifecycle, c.Breakdown, c.Selection.ToArray(), c.Round.ToArray(), c.Paid.ToArray(), c.Release.ToArray() })
        {
            var old = l[0][0]; l[0][0] = 0; Assert.ThrowsAny<JsonException>(() => c.Replay()); l[0][0] = old;
        }
        c.Release.Clear(); Assert.Throws<JsonException>(() => c.Replay());
    }
    [Fact]
    public void OwnedSchemaVerifierAndRuntimeDependenciesRevalidateOnWarmReads()
    {
        var c = new Context("armed", "commonwealth-scripted-unavailable-attacker-finish"); c.Replay();
        foreach (var path in new[] { "docs/specs/combat-actual-moved-continuation-v1.schema.json", "docs/specs/verify-combat-actual-moved-continuation-v1.py", "src/Cna.Core/Campaigns/CampaignCombatActualMovedAssault.cs" })
            Assert.Throws<JsonException>(() => CampaignCombatActualMovedContinuation.ReplayTrustedSource(c.Source, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release, p => { var b = Dependency(p); if (p == path) b[0] ^= 1; return b; }));
        c.Replay();
    }
    [Fact]
    public void MissingHandlerCapacityAndCatalogueFailBeforeReleaseWithoutChangingSource()
    {
        var c = new Context("armed", "commonwealth-scripted-unavailable-attacker-finish"); var before = c.Replay();
        using var schema = JsonDocument.Parse(Dependency("docs/specs/combat-actual-moved-continuation-v1.schema.json"));
        var handlers = schema.RootElement.GetProperty("supportHandlers").EnumerateArray().Select(x => x.GetString()!).ToHashSet(StringComparer.Ordinal); handlers.Remove(handlers.First());
        void Cert(IReadOnlySet<string>? h = null, IReadOnlyDictionary<string, int>? caps = null, IEnumerable<JsonObject>? rows = null) => CampaignCombatActualMovedContinuation.CertifyTrustedSource(c.Source, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release, Dependency, h, caps, rows);
        Assert.Throws<JsonException>(() => Cert(h: handlers)); Assert.Throws<JsonException>(() => Cert(rows: []));
        var rows = CampaignCombatActualPaidAssaultSupport.Catalogue().Select(x => x.DeepClone().AsObject()).ToArray(); rows[0]["ordinal"] = 1; Assert.Throws<JsonException>(() => Cert(rows: rows));
        var limits = schema.RootElement.GetProperty("limits").EnumerateObject().ToDictionary(x => x.Name, x => x.Value.GetInt32(), StringComparer.Ordinal);
        foreach (var key in new[] { "sourceBytes", "parentSourceBytes", "controlBytes", "proofBytes", "eventBytes", "inputBytes", "depth", "members", "progress", "witnesses", "unsupportedWitnesses", "releaseEvents", "releaseReceipts" })
        { var shortByOne = new Dictionary<string, int>(limits, StringComparer.Ordinal); shortByOne[key]--; Assert.Throws<JsonException>(() => Cert(caps: shortByOne)); }
        Cert(caps: limits); Assert.Equal(before.SourceBytes, c.Source); Assert.Equal(before.ControlBytes, c.Replay().ControlBytes);
    }
    [Fact]
    public void MaximumQuotedStructuralSourceAndEachJustOverLimitDoNotWeakenAncestorAdmission()
    {
        var c = new Context("paid", "axis-player-decline-attacker-selected"); var parent = JsonNode.Parse(c.ParentSource)!;
        parent["reactionEntrySourceCanonicalUtf8"] = new string('\\', 1_048_576);
        foreach (var (field, count) in new[] { ("selectionEventCanonicalUtf8", 16), ("roundEventCanonicalUtf8", 16), ("paidEventCanonicalUtf8", 11) })
            parent[field] = new JsonArray(Enumerable.Range(0, count).Select(_ => (JsonNode?)JsonValue.Create(new string('\\', 65_536))).ToArray());
        var parentBytes = CampaignCombatActualMovedAssaultCodec.Canonical(parent, "Source");
        var packet = JsonNode.Parse(c.Source)!; packet["movedAssaultSourceCanonicalUtf8"] = Encoding.ASCII.GetString(parentBytes);
        packet["releaseEventCanonicalUtf8"] = new JsonArray(new string('\\', 65_536), new string('\\', 65_536));
        var bytes = CampaignCombatActualMovedContinuationCodec.Canonical(packet, "Source"); Assert.InRange(bytes.Length, 15_000_000, 16_777_216);
        CampaignCombatActualMovedContinuationCodec.SourceBounds(CampaignCombatActualMovedContinuationCodec.Object(bytes, "Source"));
        // Valid framing only: the fabricated opaque ancestor is deliberately not authentic.
        Assert.ThrowsAny<JsonException>(() => CampaignCombatActualMovedContinuation.ReplayTrustedSource(bytes, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, [], Dependency));
        packet["releaseEventCanonicalUtf8"]!.AsArray().Add("{}"); Assert.Throws<JsonException>(() => CampaignCombatActualMovedContinuationCodec.SourceBounds(packet.AsObject()));
        foreach (var (kind, size) in new[] { ("Source", 16_777_216), ("ParentSource", 8_388_608), ("Control", 1_048_576), ("Proof", 2_097_152), ("Event", 65_536), ("Input", 4_096) })
            Assert.Throws<JsonException>(() => CampaignCombatActualMovedContinuationCodec.Copy(new byte[size + 1], kind));
    }
    [Fact]
    public void DuplicateExtraNoncanonicalAndDeepMalformedInputsRejectBeforeDependencyAcquisition()
    {
        var c = new Context("armed", "commonwealth-scripted-unavailable-attacker-finish"); var text = Encoding.ASCII.GetString(c.Source);
        foreach (var bytes in new[] { Encoding.ASCII.GetBytes(text.Insert(1, "\"extra\":1,")), Encoding.ASCII.GetBytes(text.Insert(1, "\"contractVersion\":1,")),
            Encoding.ASCII.GetBytes(" " + text), Encoding.ASCII.GetBytes(new string('[', 42) + new string(']', 42)), new byte[16_777_217] })
        {
            var calls = 0; Assert.Throws<JsonException>(() => CampaignCombatActualMovedContinuation.ReplayTrustedSource(bytes, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release,
                path => { calls++; return Dependency(path); })); Assert.Equal(0, calls);
        }
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
