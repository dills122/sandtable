using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatActualMovedCycleControlTests;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualMovedCycleControlContractTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var f = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-actual-moved-cycle-control-v1.json"));
        return f.RootElement.GetProperty("cases").EnumerateArray().Select((c, i) => new object[] { i, c.GetProperty("profile").GetString()!, c.GetProperty("parentName").GetString()!, c.GetProperty("choice").GetString()! }).ToArray();
    }
    [Theory, MemberData(nameof(Cases))]
    public void AllParentsBothDispositionsMatchEveryReferenceCutAndClaim(int index, string profile, string parentName, string choice)
    {
        var c = new Context(profile, parentName); using var f = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-actual-moved-cycle-control-v1.json")); var item = f.RootElement.GetProperty("cases")[index];
        Assert.Equal(c.Source, Encoding.ASCII.GetBytes(item.GetProperty("initialSourceCanonicalUtf8").GetString()!)); var current = c.Replay(); var inputs = Bytes(item.GetProperty("inputCanonicalUtf8"));
        for (var cut = 0; cut <= 2; cut++)
        {
            foreach (var (kind, b) in new[] { ("source", current.SourceBytes), ("control", current.ControlBytes), ("proof", current.ProofBytes) })
            { var expected = item.GetProperty("cuts")[cut].GetProperty(kind); Assert.Equal(expected.GetProperty("bytes").GetInt32(), b.Length); Assert.Equal(expected.GetProperty("sha256").GetString(), CampaignCombatActualMovedCycleControlCodec.Hash(b)); }
            Assert.Equal(current.SourceBytes, CampaignCombatActualMovedCycleControl.ReadSource(c.Source, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release, c.ControlInputs, Dependency));
            Assert.Equal(current.ControlBytes, ReadClaim(c, current.ControlBytes, false).ControlBytes); Assert.Equal(current.ProofBytes, ReadClaim(c, current.ProofBytes, true).ProofBytes);
            if (cut == 2) break; current = Apply(c, inputs[cut]); Assert.Equal(Encoding.ASCII.GetBytes(item.GetProperty("eventCanonicalUtf8")[cut].GetString()!), current.EventBytes); Retain(c, current, inputs[cut]);
        }
        Assert.Equal(choice == "finish" ? "finished" : "repeat-intent-pending", current.Control.GetProperty("phase").GetString());
    }
    [Theory]
    [InlineData("paid", "axis-player-decline-attacker-selected")]
    [InlineData("armed", "commonwealth-scripted-unavailable-attacker-finish")]
    public void TimingRecoveryNoopsAndFallbackPreserveAuthenticatedProvenance(string profile, string name)
    {
        foreach (var (now, clock) in new (long?, bool)[] { (null, true), (1000, false), (253402300799999, true) })
        {
            var c = new Context(profile, name); var open = Input(c.Replay().Control, "control-open", now, clock); var current = Apply(c, open); Retain(c, current, open); Assert.True(current.Control.GetProperty("openingClockFailure").GetBoolean()); Assert.Equal(JsonValueKind.Null, current.Control.GetProperty("controlWindow").ValueKind);
            var done = Apply(c, Input(current.Control, "fallback-step")); AssertFinish(done, "system", "opening-clock-unavailable");
        }
        foreach (var (kind, time, clock, reason) in new (string, long?, bool, string)[] { ("finish", 999, true, "clock-unavailable"), ("repeat-intent", null, true, "clock-unavailable"), ("finish", 1001, false, "clock-unavailable"), ("unavailable", 1001, true, "controller-unavailable"), ("expire", 31000, true, "deadline"), ("repeat-intent", 31000, true, "deadline") })
        {
            var c = new Context(profile, name); var open = Input(c.Replay().Control, "control-open"); var current = Apply(c, open); Retain(c, current, open); Assert.Equal(1000, current.Control.GetProperty("acceptedHighWater").GetInt64()); var deadline = current.Control.GetProperty("controlWindow").GetProperty("deadlineUnixMilliseconds").GetInt64(); var now = reason == "deadline" ? deadline : time;
            var early = Apply(c, Input(current.Control, "expire", deadline - 1)); Assert.Null(early.EventBytes); Assert.Equal(current.SourceBytes, early.SourceBytes); Assert.Equal(current.ControlBytes, early.ControlBytes);
            var bad = JsonNode.Parse(Input(current.Control, "expire", deadline - 1))!; bad["actor"] = "axis"; Assert.Throws<JsonException>(() => Apply(c, CanonicalInput(bad))); bad["actor"] = "system"; bad["command"]!["decisionId"] = "foreign"; Assert.Throws<JsonException>(() => Apply(c, CanonicalInput(bad)));
            var done = Apply(c, Input(current.Control, kind, now, clock)); AssertFinish(done, "system", reason); if (kind is "finish" or "repeat-intent") using (JsonDocument e = JsonDocument.Parse(done.EventBytes!)) Assert.Equal(current.Control.GetProperty("cycle").GetProperty("actingSide").GetString(), e.RootElement.GetProperty("input").GetProperty("actor").GetString());
        }
    }
    [Theory]
    [InlineData("paid", "axis-player-decline-attacker-selected")]
    [InlineData("armed", "commonwealth-scripted-unavailable-attacker-finish")]
    public void RetriesRecoverOldEventAndCurrentProofWithoutFreshAdmissionAndRejectBadContext(string profile, string name)
    {
        var c = new Context(profile, name); var initial = c.Replay(); var open = Input(initial.Control, "control-open"); Assert.Throws<JsonException>(() => Apply(c, open, fresh: false)); var opened = Apply(c, open); Retain(c, opened, open); var choice = Input(opened.Control, "repeat-intent"); var done = Apply(c, choice, fresh: false); Retain(c, done, choice);
        var retry = JsonNode.Parse(open)!; retry["admittedAt"] = 0; retry["clockAvailable"] = false; var recovered = Apply(c, CanonicalInput(retry), fresh: false); Assert.True(recovered.Duplicate); Assert.Equal(opened.EventBytes, recovered.EventBytes); Assert.Equal(done.SourceBytes, recovered.SourceBytes); Assert.Equal(done.ProofBytes, recovered.ProofBytes); Assert.Equal(done.EventBytes, Apply(c, choice, fresh: false).EventBytes);
        foreach (var key in new[] { "parentSourceHash", "cycleId", "configurationHash" }) { var bad = JsonNode.Parse(choice)!; bad["command"]![key] = "sha256:" + new string('0', 64); Assert.Throws<JsonException>(() => Apply(c, CanonicalInput(bad))); }
        var actor = JsonNode.Parse(choice)!; actor["actor"] = "system"; Assert.Throws<JsonException>(() => Apply(c, CanonicalInput(actor)));
    }
    [Fact]
    public void SourceCurrentInputAllEightLedgersAndCallbackBuffersAreOwned()
    {
        var c = new Context("paid", "axis-player-decline-attacker-selected"); var open = Input(c.Replay().Control, "control-open"); var opened = Apply(c, open); Retain(c, opened, open); var all = Ledgers(c); var original = all.Select(l => l.Select(b => b.ToArray()).ToArray()).ToArray(); var input = Input(opened.Control, "finish"); var first = true; byte[]? last = null;
        var result = Apply(c, input, p => { if (first) { first = false; c.Source[0] = 0; input[0] = 0; foreach (var l in all) foreach (var b in l) b[0] = 0; } if (last is not null) last[0] ^= 1; last = Dependency(p); return last; }); Assert.Equal("finished", result.Control.GetProperty("phase").GetString()); var returned = new[] { result.PrefixInputs, result.LifecycleInputs, result.BreakdownInputs, result.SelectionInputs, result.RoundInputs, result.PaidInputs, result.ReleaseInputs, result.ControlInputs };
        for (var i = 0; i < 8; i++) for (var j = 0; j < original[i].Length; j++) Assert.Equal(original[i][j], returned[i][j]);
        result.SourceBytes[0] = 0; result.ControlBytes[0] = 0; result.ProofBytes[0] = 0; result.EventBytes![0] = 0; returned[0][0][0] = 0; Assert.NotEqual((byte)0, result.SourceBytes[0]); Assert.NotEqual((byte)0, result.ProofBytes[0]); Assert.NotEqual((byte)0, result.EventBytes![0]); Assert.Equal(original[0][0], result.PrefixInputs[0]);
    }
    [Fact]
    public void EightLedgersAndClaimsAreIndependentlyAuthenticated()
    {
        var c = new Context("paid", "axis-player-decline-attacker-selected"); var open = Input(c.Replay().Control, "control-open"); var current = Apply(c, open); Retain(c, current, open);
        foreach (var l in Ledgers(c)) { var old = l[0][0]; l[0][0] = 0; Assert.ThrowsAny<JsonException>(() => c.Replay()); l[0][0] = old; }
        var claim = JsonNode.Parse(current.ControlBytes)!; claim["activeCycle"]!["ordinal"] = 2; Assert.Throws<JsonException>(() => ReadClaim(c, CampaignCombatActualMovedCycleControlCodec.Canonical(claim, "Control"), false)); var proof = JsonNode.Parse(current.ProofBytes)!; proof["terminalReceiptId"] = "foreign"; Assert.Throws<JsonException>(() => ReadClaim(c, CampaignCombatActualMovedCycleControlCodec.Canonical(proof, "Proof"), true)); c.ControlInputs.Clear(); Assert.Throws<JsonException>(() => c.Replay());
    }
    [Fact]
    public void WarmDependencyPinsAndEveryHandlerCapacityRemainMandatory()
    {
        var c = new Context("armed", "commonwealth-scripted-unavailable-attacker-finish"); c.Replay(); foreach (var target in new[] { "docs/specs/combat-actual-moved-cycle-control-v1.schema.json", "docs/specs/verify-combat-actual-moved-cycle-control-v1.py", "src/Cna.Core/Campaigns/CampaignCombatActualMovedContinuation.cs" }) Assert.Throws<JsonException>(() => CampaignCombatActualMovedCycleControl.ReplayTrustedSource(c.Source, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release, c.ControlInputs, p => { var b = Dependency(p); if (p == target) b[0] ^= 1; return b; }));
        using var schema = JsonDocument.Parse(Dependency("docs/specs/combat-actual-moved-cycle-control-v1.schema.json")); var h = schema.RootElement.GetProperty("supportHandlers").EnumerateArray().Select(x => x.GetString()!).ToHashSet(StringComparer.Ordinal); h.Remove(h.First()); var caps = schema.RootElement.GetProperty("limits").EnumerateObject().ToDictionary(x => x.Name, x => x.Value.GetInt32(), StringComparer.Ordinal);
        void Cert(IReadOnlySet<string>? handlers = null, IReadOnlyDictionary<string, int>? limits = null) => CampaignCombatActualMovedCycleControl.CertifyTrustedSource(c.Source, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release, c.ControlInputs, Dependency, handlers, limits);
        Assert.Throws<JsonException>(() => Cert(handlers: h)); foreach (var key in caps.Keys) { var smaller = new Dictionary<string, int>(caps, StringComparer.Ordinal); smaller[key]--; Assert.Throws<JsonException>(() => Cert(limits: smaller)); }
        Cert(limits: caps);
    }
    [Fact]
    public void NoncanonicalDuplicateExtraDeepAndOversizedPacketsFailBeforeCallback()
    {
        var c = new Context("armed", "commonwealth-scripted-unavailable-attacker-finish"); var text = Encoding.ASCII.GetString(c.Source); foreach (var b in new[] { Encoding.ASCII.GetBytes(" " + text), Encoding.ASCII.GetBytes(text.Insert(1, "\"extra\":1,")), Encoding.ASCII.GetBytes(text.Insert(1, "\"contractVersion\":1,")), Encoding.ASCII.GetBytes(new string('[', 42) + new string(']', 42)), new byte[33_554_433] }) { var called = false; Assert.ThrowsAny<JsonException>(() => CampaignCombatActualMovedCycleControl.ReplayTrustedSource(b, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release, c.ControlInputs, p => { called = true; return Dependency(p); })); Assert.False(called); }
        foreach (var (kind, cap) in new[] { ("Source", 33_554_432), ("ParentSource", 16_777_216), ("AncestorInput", 8_388_608), ("Control", 1_048_576), ("Proof", 2_097_152), ("Event", 65_536), ("Input", 4_096) }) Assert.Throws<JsonException>(() => CampaignCombatActualMovedCycleControlCodec.Copy(new byte[cap + 1], kind));
        var s = JsonNode.Parse(c.Source)!; foreach (var _ in new[] { 0, 1, 2 }) s["controlEventCanonicalUtf8"]!.AsArray().Add("{}"); Assert.Throws<JsonException>(() => CampaignCombatActualMovedCycleControlCodec.SourceBounds(s.AsObject()));
    }
    [Fact]
    public void MaximumQuotedWireCarrierDoesNotAuthenticateFabricatedAncestor()
    {
        var c = new Context("paid", "axis-player-decline-attacker-selected"); var parent = JsonNode.Parse(c.ParentSource)!; var assault = JsonNode.Parse(parent["movedAssaultSourceCanonicalUtf8"]!.GetValue<string>())!;
        assault["reactionEntrySourceCanonicalUtf8"] = new string('\\', 1_048_576);
        foreach (var (field, count) in new[] { ("selectionEventCanonicalUtf8", 16), ("roundEventCanonicalUtf8", 16), ("paidEventCanonicalUtf8", 11) }) assault[field] = new JsonArray(Enumerable.Range(0, count).Select(_ => (JsonNode?)JsonValue.Create(new string('\\', 65_536))).ToArray());
        parent["movedAssaultSourceCanonicalUtf8"] = Encoding.ASCII.GetString(CampaignCombatActualMovedAssaultCodec.Canonical(assault, "Source")); parent["releaseEventCanonicalUtf8"] = new JsonArray(new string('\\', 65_536), new string('\\', 65_536));
        var packet = JsonNode.Parse(c.Source)!; packet["continuationSourceCanonicalUtf8"] = Encoding.ASCII.GetString(CampaignCombatActualMovedContinuationCodec.Canonical(parent, "Source")); packet["controlEventCanonicalUtf8"] = new JsonArray(new string('\\', 65_536), new string('\\', 65_536)); var bytes = CampaignCombatActualMovedCycleControlCodec.Canonical(packet, "Source"); Assert.InRange(bytes.Length, 30_000_000, 33_554_432); CampaignCombatActualMovedCycleControlCodec.SourceBounds(packet.AsObject());
        Assert.ThrowsAny<JsonException>(() => CampaignCombatActualMovedCycleControl.ReplayTrustedSource(bytes, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release, [], Dependency));
        packet["continuationSourceCanonicalUtf8"] = new string('x', 16_777_217); Assert.Throws<JsonException>(() => CampaignCombatActualMovedCycleControlCodec.Canonical(packet, "Source")); packet["continuationSourceCanonicalUtf8"] = "{}"; packet["controlEventCanonicalUtf8"] = new JsonArray(new string('x', 65_537)); Assert.Throws<JsonException>(() => CampaignCombatActualMovedCycleControlCodec.Canonical(packet, "Source"));
    }
    [Theory]
    [InlineData("paid", "axis-player-decline-attacker-selected")]
    [InlineData("armed", "commonwealth-scripted-unavailable-attacker-finish")]
    public void WrongVersionNonemptyDecisionOnOpenAndUnsupportedCapabilityCannotGrantExecution(string profile, string name)
    {
        var c = new Context(profile, name); var initial = c.Replay(); var a = initial.Control.GetProperty("assessment"); Assert.False(a.GetProperty("executionAvailable").GetBoolean()); Assert.Equal("supported", a.GetProperty("witnessOutcome").GetString()); Assert.NotEmpty(a.GetProperty("witnesses").EnumerateArray());
        foreach (var key in new[] { "expectedPriorVersion", "decisionId" }) { var bad = JsonNode.Parse(Input(initial.Control, "control-open"))!; bad["command"]![key] = key == "decisionId" ? JsonValue.Create("foreign") : JsonValue.Create(initial.Control.GetProperty("stateVersion").GetInt64() + 1); Assert.Throws<JsonException>(() => Apply(c, CanonicalInput(bad))); }
        var open = Input(initial.Control, "control-open"); var opened = Apply(c, open); Retain(c, opened, open); Assert.Throws<JsonException>(() => Apply(c, Input(opened.Control, "fallback-step"))); var wrong = JsonNode.Parse(Input(opened.Control, "finish"))!; wrong["command"]!["expectedPriorVersion"] = initial.Control.GetProperty("stateVersion").GetInt64(); Assert.Throws<JsonException>(() => Apply(c, CanonicalInput(wrong)));
        Assert.Equal(opened.SourceBytes, c.Replay().SourceBytes);
    }
    private static byte[][][] Ledgers(Context c) => [c.Prefix, c.Lifecycle, c.Breakdown, c.Selection.ToArray(), c.Round.ToArray(), c.Paid.ToArray(), c.Release.ToArray(), c.ControlInputs.ToArray()];
    private static byte[] CanonicalInput(JsonNode input) => CampaignCombatActualMovedCycleControlCodec.Canonical(input, "Input");
    private static void Retain(Context c, CombatActualMovedCycleControlResult result, byte[] input) { c.Source = result.SourceBytes; c.ControlInputs.Add(input); }
    private static CombatActualMovedCycleControlResult ReadClaim(Context c, byte[] claim, bool proof) => proof ? CampaignCombatActualMovedCycleControl.ReadProof(claim, c.Source, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release, c.ControlInputs, Dependency) : CampaignCombatActualMovedCycleControl.ReadControl(claim, c.Source, c.Request, c.Prefix, c.Lifecycle, c.Breakdown, c.Selection, c.Round, c.Paid, c.Release, c.ControlInputs, Dependency);
    private static void AssertFinish(CombatActualMovedCycleControlResult result, string author, string reason) { Assert.Equal("finished", result.Control.GetProperty("phase").GetString()); using var e = JsonDocument.Parse(result.EventBytes!); Assert.Equal(author, e.RootElement.GetProperty("author").GetString()); Assert.Equal(reason, e.RootElement.GetProperty("effect").GetProperty("reason").GetString()); }
}
