using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatActualMovedCycleControlTests;
using static Cna.Core.Tests.Campaigns.CombatActualMovedRepeatMovementTests;
namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualMovedRepeatMovementContractTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var f = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-actual-moved-repeat-movement-v1.json"));
        return f.RootElement.GetProperty("cases").EnumerateArray().Select((c, ordinal) => new object[] { ordinal, c.GetProperty("profile").GetString()! }).ToArray();
    }
    [Theory, MemberData(nameof(Cases))]
    public void AllParentsEveryCutMatchesCanonicalEventsControlAndProof(int ordinal, string profile)
    {
        var c = new RepeatContext(ordinal); Assert.Equal(profile, c.Case.GetProperty("profile").GetString()); var current = c.Replay(); var initial = current.Control;
        for (var cut = 0; cut <= c.Inputs.Length; cut++)
        {
            foreach (var (kind, bytes) in new[] { ("source", current.SourceBytes), ("control", current.ControlBytes), ("proof", current.ProofBytes) })
            {
                var expected = c.Case.GetProperty("cuts")[cut].GetProperty(kind);
                Assert.Equal(expected.GetProperty("bytes").GetInt32(), bytes.Length);
                Assert.Equal(expected.GetProperty("sha256").GetString(), CampaignCombatActualMovedRepeatMovementCodec.Hash(bytes));
            }
            if (cut == c.Inputs.Length) break;
            current = Apply(c, c.Inputs[cut]); Assert.Equal(Encoding.ASCII.GetBytes(c.Case.GetProperty("eventCanonicalUtf8")[cut].GetString()!), current.EventBytes); Retain(c, current, c.Inputs[cut]);
            if (cut == 0)
            {
                var s = current.Control; Assert.Equal(2, s.GetProperty("activeCycle").GetProperty("ordinal").GetInt32()); Assert.Equal(s.GetProperty("stateVersion").GetInt64(), s.GetProperty("activeCycle").GetProperty("openedAuthorityVersion").GetInt64()); Assert.Equal(initial.GetProperty("prefix").GetString(), s.GetProperty("activeCycle").GetProperty("openingPrefix").GetString());
                Assert.Empty(s.GetProperty("actualProgressRefs").EnumerateArray()); Assert.Empty(s.GetProperty("targetUses").EnumerateArray()); Assert.Equal(initial.GetProperty("world").GetRawText(), s.GetProperty("world").GetRawText()); Assert.Equal("repeated", s.GetProperty("closure").GetProperty("outcome").GetString()); Assert.Equal(1, s.GetProperty("closure").GetProperty("ordinal").GetInt32()); Assert.False(s.GetProperty("pendingRepeatIntent").GetProperty("executionAvailable").GetBoolean());
                foreach (var field in initial.GetProperty("sequencePosition").EnumerateObject().Where(p => p.Name is not ("positionId" or "segmentId"))) Assert.Equal(field.Value.GetRawText(), s.GetProperty("sequencePosition").GetProperty(field.Name).GetRawText());
            }
        }
        Assert.Equal(profile == "paid" ? 3 : 6, c.Movement.Count); Assert.Equal(c.Movement.Count - 1, current.Control.GetProperty("actualProgressRefs").GetArrayLength());
        foreach (var field in new[] { "randomState", "originalMovementEnd", "priorCycleProgress", "priorCycleTargetUses", "attackHistory", "assessment", "pendingRepeatIntent", "pendingIntentReceiptId" }) Assert.Equal(initial.GetProperty(field).GetRawText(), current.Control.GetProperty(field).GetRawText());
        foreach (var field in new[] { "settlements", "custodyLots", "guards", "replacementEntitlements", "futureObligations", "brokenVehicleLots" }) Assert.Equal(initial.GetProperty("world").GetProperty(field).GetRawText(), current.Control.GetProperty("world").GetProperty(field).GetRawText());
        var elementId = current.Control.GetProperty("members")[0].GetProperty("unit").GetProperty("elementId").GetString(); var element = current.Control.GetProperty("world").GetProperty("elements").EnumerateArray().Single(e => e.GetProperty("elementId").GetString() == elementId); Assert.Equal(profile == "paid" ? 15 : 14, element.GetProperty("operationalState").GetProperty("capabilityPointsExpended").GetProperty("numerator").GetInt32());
        var beforeElement = initial.GetProperty("world").GetProperty("elements").EnumerateArray().Single(e => e.GetProperty("elementId").GetString() == elementId); var expectedDp = profile == "paid" ? 5 : 4; Assert.Equal(beforeElement.GetProperty("operationalState").GetProperty("cohesionLevel").GetInt32() - expectedDp, element.GetProperty("operationalState").GetProperty("cohesionLevel").GetInt32()); Assert.Equal(expectedDp, current.Control.GetProperty("world").GetProperty("cohesionCauses").EnumerateArray().Where(x => x.GetProperty("kind").GetString() == "ordinary-movement-excess-cp-dp").Sum(x => x.GetProperty("points").GetInt32())); Assert.Equal(element.GetProperty("operationalState").GetProperty("capabilityPointsExpended").GetRawText(), current.Control.GetProperty("members")[0].GetProperty("spentCp").GetRawText());
        var origin = JsonNode.Parse(c.Inputs[^1])!["command"]!["originLocationId"]!.GetValue<string>(); AssertCode("CMB-MRM-005", () => Apply(c, Command(current.Control, "move", origin)));
        foreach (var prior in new[] { 0, c.Inputs.Length - 1 }) { var retry = Apply(c, c.Inputs[prior], fresh: false); Assert.True(retry.Duplicate); Assert.Equal(Encoding.ASCII.GetBytes(c.Case.GetProperty("eventCanonicalUtf8")[prior].GetString()!), retry.EventBytes); Assert.Equal(current.SourceBytes, retry.SourceBytes); Assert.Equal(current.ProofBytes, retry.ProofBytes); }
    }
    [Theory]
    [InlineData(0, "paid")]
    [InlineData(12, "armed")]
    public void EveryCutRetryWorksWithoutFreshAdmissionButFreshCommandsAndBadContextReject(int ordinal, string profile)
    {
        var c = new RepeatContext(ordinal); Assert.Equal(profile, c.Case.GetProperty("profile").GetString()); AssertCode("CMB-MRM-007", () => Apply(c, c.Inputs[0], fresh: false));
        for (var cut = 1; cut <= c.Inputs.Length; cut++)
        {
            c = new RepeatContext(ordinal); AtCut(c, cut); var current = c.Replay();
            for (var prior = 0; prior < cut; prior++) { var retry = Apply(c, c.Inputs[prior], fresh: false); Assert.True(retry.Duplicate); Assert.Equal(current.SourceBytes, retry.SourceBytes); Assert.Equal(current.ProofBytes, retry.ProofBytes); Assert.Equal(c.Movement.Count, retry.MovementInputs.Count); }
            if (cut < c.Inputs.Length) AssertCode("CMB-MRM-007", () => Apply(c, c.Inputs[cut], fresh: false));
        }
        c = new RepeatContext(ordinal); AtCut(c, 1); var opened = c.Replay();
        foreach (var field in new[] { "parentSourceHash", "baseHash", "sourceOccurrence", "configurationHash" }) { var bad = JsonNode.Parse(c.Inputs[1])!; bad["command"]![field] = "sha256:" + new string('0', 64); AssertCode("CMB-MRM-004", () => Apply(c, Wire(bad))); }
        foreach (var field in new[] { "intentReceiptId", "decisionId", "positionId" }) { var bad = JsonNode.Parse(c.Inputs[1])!; bad["command"]![field] = "foreign"; AssertCode("CMB-MRM-004", () => Apply(c, Wire(bad))); }
        var stale = JsonNode.Parse(c.Inputs[1])!; stale["command"]!["expectedPriorVersion"] = 0; AssertCode("CMB-MRM-006", () => Apply(c, Wire(stale)));
        var actor = JsonNode.Parse(c.Inputs[1])!; actor["actor"] = "system"; AssertCode("CMB-MRM-004", () => Apply(c, Wire(actor)));
        var occupied = JsonNode.Parse(c.Inputs[1])!; occupied["command"]!["destinationLocationId"] = occupied["command"]!["originLocationId"]!.DeepClone(); AssertCode("CMB-MRM-005", () => Apply(c, Wire(occupied)));
        c = new RepeatContext(ordinal); AtCut(c, 2); var moved = c.Replay(); var origin = JsonNode.Parse(c.Inputs[1])!["command"]!["originLocationId"]!.GetValue<string>(); AssertCode("CMB-MRM-003", () => Apply(c, Command(moved.Control, "move", origin))); Assert.Equal(moved.SourceBytes, c.Replay().SourceBytes);
    }
    [Theory]
    [InlineData(0, "paid")]
    [InlineData(12, "armed")]
    public void NineIndependentLedgersAndClaimsCannotBeReplacedBySourceHashes(int ordinal, string profile)
    {
        var c = new RepeatContext(ordinal); AtCut(c, 2); var current = c.Replay(); Assert.Equal(profile, current.Control.GetProperty("profile").GetString());
        var ledgers = Ledgers(c);
        for (var index = 0; index < ledgers.Length; index++)
        {
            var bad = ledgers.Select(l => l.Select(b => b.ToArray()).ToArray()).ToArray(); bad[index] = bad[index].Length == 0 ? [Encoding.ASCII.GetBytes("{}")] : bad[index][..^1];
            Assert.ThrowsAny<JsonException>(() => ReplayLedgers(c, bad));
        }
        Assert.Equal(current.SourceBytes, CampaignCombatActualMovedRepeatMovement.ReadSource(c.Source, c.Ancestor.Request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4], ledgers[5], ledgers[6], ledgers[7], ledgers[8], Dependency));
        Assert.Equal(current.ControlBytes, Claim(c, current.ControlBytes, false).ControlBytes); Assert.Equal(current.ProofBytes, Claim(c, current.ProofBytes, true).ProofBytes);
        var claim = JsonNode.Parse(current.ControlBytes)!; claim["activeCycle"]!["ordinal"] = 3; AssertCode("CMB-MRM-006", () => Claim(c, CampaignCombatActualMovedRepeatMovementCodec.Canonical(claim, "Control"), false));
        var proof = JsonNode.Parse(current.ProofBytes)!; proof["publicationReceiptId"] = "foreign"; AssertCode("CMB-MRM-006", () => Claim(c, CampaignCombatActualMovedRepeatMovementCodec.Canonical(proof, "Proof"), true));
        var packet = JsonNode.Parse(c.Source)!; var ev = JsonNode.Parse(packet["movementEventCanonicalUtf8"]!.AsArray()[^1]!.GetValue<string>())!;
        ev["effect"]!["costs"]!["afterCp"] = ev["effect"]!["costs"]!["afterCp"]!.GetValue<int>() + 1; ev.AsObject().Remove("receiptId"); ev["receiptId"] = "mrm." + CampaignCombatActualMovedRepeatMovementCodec.HashWithDomain("sandtable.combat.actual-moved-repeat-movement.receipt.v1", CampaignCombatActualMovedRepeatMovementCodec.Encode(ev))[7..];
        packet["movementEventCanonicalUtf8"]!.AsArray()[^1] = Encoding.ASCII.GetString(CampaignCombatActualMovedRepeatMovementCodec.Canonical(ev, "Event")); c.Source = CampaignCombatActualMovedRepeatMovementCodec.Canonical(packet, "Source"); AssertCode("CMB-MRM-006", () => c.Replay());
    }
    [Theory]
    [InlineData(0, "paid")]
    [InlineData(12, "armed")]
    public void CallbackMutationCannotChangeSourceInputNineLedgersOrReturnedBuffers(int ordinal, string profile)
    {
        var c = new RepeatContext(ordinal); AtCut(c, 1); var before = c.Replay(); Assert.Equal(profile, before.Control.GetProperty("profile").GetString()); var input = c.Inputs[1].ToArray(); var original = Ledgers(c).Select(l => l.Select(b => b.ToArray()).ToArray()).ToArray(); var first = true; byte[]? last = null;
        var result = Apply(c, input, path =>
        {
            if (first) { first = false; c.Source[0] = 0; input[0] = 0; foreach (var ledger in Ledgers(c)) foreach (var b in ledger) b[0] = 0; }
            if (last is not null) last[0] ^= 1; last = Dependency(path); return last;
        });
        var returned = new[] { result.PrefixInputs, result.LifecycleInputs, result.BreakdownInputs, result.SelectionInputs, result.RoundInputs, result.PaidInputs, result.ReleaseInputs, result.ControlInputs, result.MovementInputs };
        for (var i = 0; i < original.Length; i++) for (var j = 0; j < original[i].Length; j++) Assert.Equal(original[i][j], returned[i][j]);
        Assert.Equal(Encoding.ASCII.GetBytes(c.Case.GetProperty("eventCanonicalUtf8")[1].GetString()!), result.EventBytes);
        result.SourceBytes[0] = 0; result.ControlBytes[0] = 0; result.ProofBytes[0] = 0; result.EventBytes![0] = 0; returned[0][0][0] = 0; Assert.NotEqual((byte)0, result.SourceBytes[0]); Assert.NotEqual((byte)0, result.ControlBytes[0]); Assert.NotEqual((byte)0, result.ProofBytes[0]); Assert.NotEqual((byte)0, result.EventBytes![0]); Assert.Equal(original[0][0], result.PrefixInputs[0]);
        c = new RepeatContext(ordinal); AtCut(c, 2); var claim = c.Replay().ProofBytes; var expected = claim.ToArray(); first = true;
        var recovered = Claim(c, claim, true, path => { if (first) { first = false; claim[0] = 0; } return Dependency(path); }); Assert.Equal(expected, recovered.ProofBytes);
    }
    [Fact]
    public void EveryPhysicalPinRemainsMandatoryAfterWarmReplay()
    {
        var c = new RepeatContext(12); c.Replay(); using var schema = JsonDocument.Parse(Dependency("docs/specs/combat-actual-moved-repeat-movement-v1.schema.json"));
        var paths = schema.RootElement.GetProperty("runtimeSourceHashes").EnumerateObject().Select(p => p.Name).Concat(["docs/specs/combat-actual-moved-repeat-movement-v1.schema.json", "docs/specs/verify-combat-actual-moved-repeat-movement-v1.py"]).ToArray(); Assert.Equal(103, paths.Length);
        foreach (var target in paths)
        {
            AssertCode("CMB-MRM-009", () => c.Replay(path => { var b = Dependency(path); if (path == target) b[0] ^= 1; return b; }));
            AssertCode("CMB-MRM-009", () => c.Replay(path => path == target ? throw new IOException("missing retained dependency") : Dependency(path)));
        }
        var first = paths[0]; AssertCode("CMB-MRM-009", () => c.Replay(path => path == first ? null! : Dependency(path))); AssertCode("CMB-MRM-009", () => c.Replay(path => path == first ? new byte[8388609] : Dependency(path))); AssertCode("CMB-MRM-009", () => c.Replay(path => path == first ? [.. Dependency(path), 10] : Dependency(path)));
    }
    [Fact]
    public void EveryPublishedHandlerAndCapacityIsRequired()
    {
        var c = new RepeatContext(12); using var schema = JsonDocument.Parse(Dependency("docs/specs/combat-actual-moved-repeat-movement-v1.schema.json")); var handlers = schema.RootElement.GetProperty("supportHandlers").EnumerateArray().Select(h => h.GetString()!).ToHashSet(StringComparer.Ordinal); var capacities = schema.RootElement.GetProperty("limits").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetInt32(), StringComparer.Ordinal);
        foreach (var handler in handlers) { var missing = handlers.Where(h => h != handler).ToHashSet(StringComparer.Ordinal); AssertCode("CMB-MRM-007", () => Certify(c, missing, capacities)); }
        foreach (var key in capacities.Keys) { var smaller = new Dictionary<string, int>(capacities, StringComparer.Ordinal); smaller[key]--; AssertCode("CMB-MRM-007", () => Certify(c, handlers, smaller)); }
    }
    [Theory]
    [InlineData(0, "paid")]
    [InlineData(12, "armed")]
    public void GenuineFinishedAncestorAndForeignCreationRequestCannotPublish(int ordinal, string profile)
    {
        var c = new RepeatContext(ordinal); Assert.Equal(profile, c.Case.GetProperty("profile").GetString()); var packet = JsonNode.Parse(c.Source)!; var parent = JsonNode.Parse(packet["controlSourceCanonicalUtf8"]!.GetValue<string>())!;
        using var fixture = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-actual-moved-cycle-control-v1.json")); var finish = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(x => x.GetProperty("parentName").GetString() == c.ParentCase.GetProperty("parentName").GetString() && x.GetProperty("choice").GetString() == "finish");
        parent["controlEventCanonicalUtf8"] = new JsonArray(finish.GetProperty("eventCanonicalUtf8").EnumerateArray().Select(e => (JsonNode?)JsonValue.Create(e.GetString())).ToArray()); var parentBytes = CampaignCombatActualMovedCycleControlCodec.Canonical(parent, "Source"); var ledgers = Ledgers(c); ledgers[7] = Bytes(finish.GetProperty("inputCanonicalUtf8"));
        var authentic = CampaignCombatActualMovedCycleControl.ReplayTrustedSource(parentBytes, c.Ancestor.Request, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4], ledgers[5], ledgers[6], ledgers[7], Dependency); Assert.Equal("finished", authentic.Control.GetProperty("phase").GetString()); packet["controlSourceCanonicalUtf8"] = Encoding.ASCII.GetString(parentBytes); c.Source = CampaignCombatActualMovedRepeatMovementCodec.Canonical(packet, "Source"); AssertCode("CMB-MRM-004", () => ReplayLedgers(c, ledgers));
        c = new RepeatContext(ordinal); ledgers = Ledgers(c); var foreign = CampaignCombatCreationRequest.Create("foreign-campaign", 1, c.Ancestor.Request.Context); Assert.ThrowsAny<JsonException>(() => CampaignCombatActualMovedRepeatMovement.ReplayTrustedSource(c.Source, foreign, ledgers[0], ledgers[1], ledgers[2], ledgers[3], ledgers[4], ledgers[5], ledgers[6], ledgers[7], ledgers[8], Dependency));
    }
    [Fact]
    public void OwnedWorldGrammarExceedsOldCapWithoutClaimingReachableAuthority()
    {
        var c = new RepeatContext(0); AtCut(c, c.Inputs.Length); var world = JsonNode.Parse(c.Replay().Control.GetProperty("world").GetRawText())!;
        var element = world["elements"]![0]!; var refs = new JsonArray(Enumerable.Range(0, 512).Select(_ => (JsonNode?)new JsonObject { ["sourceId"] = new string('s', 128), ["locator"] = new string('a', 128) }).ToArray());
        foreach (var origin in new[] { element["readiness"]!["initialReadinessOrigin"]!, element["ammunition"]!["initialAmmunitionOrigin"]!, element["operationalState"]!["initialLedgerOrigin"]!, element["components"]![0]!["initialToeOrigin"]! }) origin["references"] = refs.DeepClone();
        var framing = CampaignCombatActualMovedRepeatMovementCodec.Canonical(world, "World"); Assert.Equal(593548, framing.Length); Assert.InRange(framing.Length, 524289, 1048576); Assert.Equal(framing, CampaignCombatActualMovedRepeatMovementCodec.Canonical(CampaignCombatActualMovedRepeatMovementCodec.Object(framing, "World"), "World"));
        Assert.Equal(c.Source, c.Replay().SourceBytes); // Modified origins are framing only, never submitted as a source claim.
    }
    [Fact]
    public void MaximumSourceFramingRemainsBoundedAndCannotAuthenticateInventedParent()
    {
        var packet = new JsonObject { ["contractVersion"] = 1, ["controlSourceCanonicalUtf8"] = new string('"', 31722496), ["movementEventCanonicalUtf8"] = new JsonArray(Enumerable.Range(0, 6).Select(_ => (JsonNode?)JsonValue.Create(new string('"', 65536))).ToArray()) };
        var framing = CampaignCombatActualMovedRepeatMovementCodec.Canonical(packet, "Source"); Assert.Equal(64231526, framing.Length); Assert.InRange(framing.Length, 1, 64231936); CampaignCombatActualMovedRepeatMovementCodec.SourceBounds(packet);
        var c = new RepeatContext(12); c.Source = framing; Assert.ThrowsAny<JsonException>(() => c.Replay());
        packet["controlSourceCanonicalUtf8"] = "{}"; packet["movementEventCanonicalUtf8"]!.AsArray().Add("{}"); Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatMovementCodec.SourceBounds(packet));
        AssertCode("CMB-MRM-001", () => CampaignCombatActualMovedRepeatMovementCodec.Copy(new byte[67108865], "Source")); AssertCode("CMB-MRM-001", () => CampaignCombatActualMovedRepeatMovementCodec.Copy(new byte[4097], "Input"));
    }
    [Fact]
    public void ClosedGrammarRejectsDuplicateBooleanIntegerAliasesAndNoncanonicalBytes()
    {
        var c = new RepeatContext(12); var input = c.Inputs[0]; var node = JsonNode.Parse(input)!; var command = node["command"]!.ToJsonString();
        var duplicate = Encoding.ASCII.GetBytes("{\"command\":" + command + ",\"command\":" + command + ",\"actor\":\"system\"}");
        AssertCode("CMB-MRM-001", () => CampaignCombatActualMovedRepeatMovementCodec.Object(duplicate, "Input"));
        AssertCode("CMB-MRM-008", () => CampaignCombatActualMovedRepeatMovementCodec.Object([.. input, 32], "Input"));
        node["command"]!["contractVersion"] = true; AssertCode("CMB-MRM-001", () => CampaignCombatActualMovedRepeatMovementCodec.Object(CampaignCombatActualMovedRepeatMovementCodec.Encode(node), "Input"));
        AssertCode("CMB-MRM-001", () => CampaignCombatActualMovedRepeatMovementCodec.Canonical(new JsonObject(), "LegacyBrokenVehicleLot")); AssertCode("CMB-MRM-001", () => CampaignCombatActualMovedRepeatMovementCodec.Canonical(new JsonArray(Enumerable.Range(0, 10).Select(_ => (JsonNode?)JsonValue.Create("a")).ToArray()), "Route"));
        var tooDeep = Encoding.ASCII.GetBytes(new string('[', 42) + "0" + new string(']', 42)); AssertCode("CMB-MRM-001", () => CampaignCombatActualMovedRepeatMovementCodec.Object(tooDeep, "Input"));
    }
    private static byte[][][] Ledgers(RepeatContext c) => [c.Ancestor.Prefix, c.Ancestor.Lifecycle, c.Ancestor.Breakdown, c.Ancestor.Selection.ToArray(), c.Ancestor.Round.ToArray(), c.Ancestor.Paid.ToArray(), c.Ancestor.Release.ToArray(), c.Control, c.Movement.ToArray()];
    private static CombatActualMovedRepeatMovementResult ReplayLedgers(RepeatContext c, byte[][][] l) => CampaignCombatActualMovedRepeatMovement.ReplayTrustedSource(c.Source, c.Ancestor.Request, l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], Dependency);
    private static CombatActualMovedRepeatMovementResult Claim(RepeatContext c, byte[] claim, bool proof, Func<string, byte[]>? dependency = null)
    {
        var l = Ledgers(c); return proof ? CampaignCombatActualMovedRepeatMovement.ReadProof(claim, c.Source, c.Ancestor.Request, l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], dependency ?? Dependency) : CampaignCombatActualMovedRepeatMovement.ReadControl(claim, c.Source, c.Ancestor.Request, l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], dependency ?? Dependency);
    }
    private static void Certify(RepeatContext c, IReadOnlySet<string> handlers, IReadOnlyDictionary<string, int> capacities)
    { var l = Ledgers(c); CampaignCombatActualMovedRepeatMovement.CertifyTrustedSource(c.Source, c.Ancestor.Request, l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], Dependency, handlers, capacities); }
    internal static void AtCut(RepeatContext c, int cut)
    {
        var packet = JsonNode.Parse(c.Source)!; packet["movementEventCanonicalUtf8"] = new JsonArray(c.Case.GetProperty("eventCanonicalUtf8").EnumerateArray().Take(cut).Select(e => (JsonNode?)JsonValue.Create(e.GetString())).ToArray()); c.Source = CampaignCombatActualMovedRepeatMovementCodec.Canonical(packet, "Source"); c.Movement.AddRange(c.Inputs.Take(cut));
    }
}
