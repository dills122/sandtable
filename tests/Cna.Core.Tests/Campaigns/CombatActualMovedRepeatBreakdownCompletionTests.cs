using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatActualMovedCycleControlTests;
using static Cna.Core.Tests.Campaigns.CombatActualMovedRepeatMovementCompletionTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualMovedRepeatBreakdownCompletionTests
{
    [Theory]
    [InlineData(0, 1, "paid")]
    [InlineData(0, 3, "paid")]
    [InlineData(12, 1, "armed")]
    [InlineData(12, 6, "armed")]
    public void AuthenticTerminalMatchesFrozenReference(int root, int cut, string profile)
    {
        using var fixture = JsonDocument.Parse(Dependency("docs/specs/fixtures/combat-actual-moved-repeat-breakdown-completion-v1.json"));
        var expected = fixture.RootElement.GetProperty("cases").EnumerateArray().Single(r => r.GetProperty("rootOrdinal").GetInt32() == root && r.GetProperty("movementCut").GetInt32() == cut);
        Assert.Equal(profile, expected.GetProperty("profile").GetString());
        VerifyReference(expected);
    }

    internal static void VerifyReference(JsonElement expected)
    {
        var root = expected.GetProperty("rootOrdinal").GetInt32(); var cut = expected.GetProperty("movementCut").GetInt32(); var profile = expected.GetProperty("profile").GetString()!;
        var c = new CompletionContext(root, cut, profile);
        foreach (var input in c.Inputs) { var next = Apply(c, input); Retain(c, next, input); }
        var parent = c.Replay(); Assert.True(parent.Control.GetProperty("closed").GetBoolean());
        var l = Ledgers(c); var source = Codec.Canonical(new JsonObject
        {
            ["contractVersion"] = 1,
            ["movementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(c.Source),
            ["breakdownEventCanonicalUtf8"] = new JsonArray()
        }, "Source");
        var before = Replay(source, c, l, []);
        Assert.Equal(expected.GetProperty("beforeControlHash").GetString(), Codec.Hash(before.ControlBytes));
        Assert.Equal(expected.GetProperty("beforeProofHash").GetString(), Codec.Hash(before.ProofBytes));
        var command = Codec.Canonical(CampaignCombatActualMovedRepeatBreakdownCompletion.Command(JsonNode.Parse(before.ControlBytes)!.AsObject()), "Input");
        var after = CampaignCombatActualMovedRepeatBreakdownCompletion.ApplyTrustedSource(source, c.Parent.Ancestor.Request,
            l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], l[9], [], command, Dependency);
        Assert.False(after.Duplicate); Assert.Equal(expected.GetProperty("receiptId").GetString(), after.ReceiptId);
        Assert.Equal(expected.GetProperty("eventHash").GetString(), Codec.Hash(after.EventBytes!));
        Assert.Equal(expected.GetProperty("sourceHash").GetString(), Codec.Hash(after.SourceBytes));
        Assert.Equal(expected.GetProperty("afterControlHash").GetString(), Codec.Hash(after.ControlBytes));
        Assert.Equal(expected.GetProperty("afterProofHash").GetString(), Codec.Hash(after.ProofBytes));
        var replayed = Replay(after.SourceBytes, c, l, [command]); Assert.Equal(after.ProofBytes, replayed.ProofBytes);
        var retry = CampaignCombatActualMovedRepeatBreakdownCompletion.ApplyTrustedSource(after.SourceBytes, c.Parent.Ancestor.Request,
            l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], l[9], [command], command, Dependency, false);
        Assert.True(retry.Duplicate); Assert.Equal(after.SourceBytes, retry.SourceBytes); Assert.Equal(after.ControlBytes, retry.ControlBytes);
        Assert.Equal(2, after.Control.GetProperty("activeCycle").GetProperty("ordinal").GetInt32());
        Assert.Equal(2, after.Control.GetProperty("movementEnd").GetProperty("ordinal").GetInt32());
        Assert.Equal(after.Control.GetProperty("receipts").EnumerateArray().Last().GetRawText(), after.Control.GetProperty("breakdownReceipts")[0].GetRawText());
    }

    [Fact]
    public void NonterminalPredecessorAndWrongActorReject()
    {
        var c = new CompletionContext(0, 1, "paid"); var l = Ledgers(c);
        var source = Codec.Canonical(new JsonObject
        {
            ["contractVersion"] = 1,
            ["movementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(c.Source),
            ["breakdownEventCanonicalUtf8"] = new JsonArray()
        }, "Source");
        Assert.Throws<JsonException>(() => Replay(source, c, l, []));
        foreach (var input in c.Inputs) { var next = Apply(c, input); Retain(c, next, input); }
        l = Ledgers(c); source = Codec.Canonical(new JsonObject
        {
            ["contractVersion"] = 1,
            ["movementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(c.Source),
            ["breakdownEventCanonicalUtf8"] = new JsonArray()
        }, "Source");
        var before = Replay(source, c, l, []); var bad = CampaignCombatActualMovedRepeatBreakdownCompletion.Command(JsonNode.Parse(before.ControlBytes)!.AsObject()); bad["actor"] = "axis";
        var inputBytes = Codec.Canonical(bad, "Input");
        Assert.Equal("CMB-MRB-004", Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletion.ApplyTrustedSource(source, c.Parent.Ancestor.Request,
            l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], l[9], [], inputBytes, Dependency)).Message);
    }

    [Fact]
    public void ClosedSourceFramingAndPinnedDependencyFailClosed()
    {
        var wrong = Encoding.ASCII.GetBytes("{\"contractVersion\":1,\"movementCompletionSourceCanonicalUtf8\":\"x\",\"breakdownEventCanonicalUtf8\":[],\"extra\":0}");
        Assert.Equal("CMB-MRB-001", Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletionCodec.Object(wrong, "Source")).Message);
        var duplicate = Encoding.ASCII.GetBytes("{\"contractVersion\":1,\"contractVersion\":1,\"movementCompletionSourceCanonicalUtf8\":\"x\",\"breakdownEventCanonicalUtf8\":[]}");
        Assert.Equal("CMB-MRB-001", Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletionCodec.Object(duplicate, "Source")).Message);
        var overflow = Encoding.ASCII.GetBytes("{\"contractVersion\":2147483648,\"movementCompletionSourceCanonicalUtf8\":\"x\",\"breakdownEventCanonicalUtf8\":[]}");
        Assert.Equal("CMB-MRB-002", Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletionCodec.Object(overflow, "Source")).Message);
        foreach (var (kind, literal) in new[]
        {
            ("int", "2147483648"), ("long", "9223372036854775808"),
            ("signedInt", "-2147483649"), ("futureTurn", "116"),
            ("differential", "3"), ("ulong", "18446744073709551616")
        })
            Assert.Equal("CMB-MRB-002", Assert.Throws<JsonException>(() => Codec.Canonical(JsonNode.Parse(literal)!, kind)).Message);
        Assert.Equal("CMB-MRB-001", Assert.Throws<JsonException>(() => Codec.Canonical(JsonNode.Parse("1.0")!, "futureTurn")).Message);
        Assert.Equal("0", Encoding.ASCII.GetString(Codec.Canonical(JsonNode.Parse("-0")!, "ulong")));
        var c = new CompletionContext(0, 1, "paid");
        foreach (var input in c.Inputs) { var next = Apply(c, input); Retain(c, next, input); }
        var l = Ledgers(c); var source = Codec.Canonical(new JsonObject
        {
            ["contractVersion"] = 1,
            ["movementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(c.Source),
            ["breakdownEventCanonicalUtf8"] = new JsonArray()
        }, "Source");
        byte[] Changed(string path) => path == "docs/specs/verify-combat-actual-moved-repeat-breakdown-completion-v1.py" ? [1, 2, 3] : Dependency(path);
        Assert.Equal("CMB-MRB-009", Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletion.ReplayTrustedSource(source, c.Parent.Ancestor.Request,
            l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], l[9], [], Changed)).Message);
    }

    [Fact]
    public void ForgedCapabilityEventAndClaimsRejectAfterAuthenticReplay()
    {
        var c = new CompletionContext(0, 1, "paid");
        foreach (var input in c.Inputs) { var next = Apply(c, input); Retain(c, next, input); }
        var l = Ledgers(c); var source = Codec.Canonical(new JsonObject
        {
            ["contractVersion"] = 1,
            ["movementCompletionSourceCanonicalUtf8"] = Encoding.ASCII.GetString(c.Source),
            ["breakdownEventCanonicalUtf8"] = new JsonArray()
        }, "Source");
        var before = Replay(source, c, l, []); var command = CampaignCombatActualMovedRepeatBreakdownCompletion.Command(JsonNode.Parse(before.ControlBytes)!.AsObject());
        var bad = command.DeepClone(); bad["command"]!["capabilityId"] = "sha256:" + new string('0', 64);
        Assert.Equal("CMB-MRB-005", Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletion.ApplyTrustedSource(source, c.Parent.Ancestor.Request,
            l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], l[9], [], Codec.Canonical(bad, "Input"), Dependency)).Message);
        var inputBytes = Codec.Canonical(command, "Input");
        var after = CampaignCombatActualMovedRepeatBreakdownCompletion.ApplyTrustedSource(source, c.Parent.Ancestor.Request,
            l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], l[9], [], inputBytes, Dependency);
        var forged = JsonNode.Parse(after.SourceBytes)!; var ev = JsonNode.Parse(forged["breakdownEventCanonicalUtf8"]![0]!.GetValue<string>())!;
        ev["receiptId"] = "mrbc.forged"; forged["breakdownEventCanonicalUtf8"]![0] = Encoding.ASCII.GetString(Codec.Canonical(ev, "Event"));
        var tampered = Codec.Canonical(forged, "Source");
        Assert.Equal("CMB-MRB-006", Assert.Throws<JsonException>(() => Replay(tampered, c, l, [inputBytes])).Message);
        var claim = JsonNode.Parse(after.ControlBytes)!; claim["stateVersion"] = claim["stateVersion"]!.GetValue<long>() + 1;
        var parents = l.Select(x => (IReadOnlyList<byte[]>)x).ToArray();
        Assert.Equal("CMB-MRB-006", Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletion.ReadControl(Codec.Canonical(claim, "Control"), after.SourceBytes, c.Parent.Ancestor.Request, parents, [inputBytes], Dependency)).Message);
        var proof = JsonNode.Parse(after.ProofBytes)!; proof["control"]!["stateVersion"] = proof["control"]!["stateVersion"]!.GetValue<long>() + 1;
        Assert.Equal("CMB-MRB-006", Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletion.ReadProof(Codec.Canonical(proof, "Proof"), after.SourceBytes, c.Parent.Ancestor.Request, parents, [inputBytes], Dependency)).Message);
        var outOfRangeControl = JsonNode.Parse(after.ControlBytes)!;
        outOfRangeControl["world"]!["futureObligations"]!.AsArray().Add(JsonNode.Parse("""
            {"obligationId":"o1","receiptId":"r1","kind":"test","subjectId":"u1","earnedScope":{"gameTurn":1,"operationStage":1},"eligibleScope":{"gameTurn":116,"operationStage":1},"activationGate":"test","status":"pending"}
            """));
        Assert.Equal("CMB-MRB-002", Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletion.ReadControl(
            JsonSerializer.SerializeToUtf8Bytes(outOfRangeControl), after.SourceBytes, c.Parent.Ancestor.Request, parents, [inputBytes], Dependency)).Message);
        var outOfRangeBorrowedProof = JsonNode.Parse(after.ProofBytes)!;
        outOfRangeBorrowedProof["base"]!["movedRepeatMovementCompletionProof"]!["control"]!["sequencePosition"]!["gameTurn"] = 2_147_483_648L;
        Assert.Equal("CMB-MRB-002", Assert.Throws<JsonException>(() => CampaignCombatActualMovedRepeatBreakdownCompletion.ReadProof(
            JsonSerializer.SerializeToUtf8Bytes(outOfRangeBorrowedProof), after.SourceBytes, c.Parent.Ancestor.Request, parents, [inputBytes], Dependency)).Message);
    }

    private static CombatActualMovedRepeatBreakdownCompletionResult Replay(byte[] source, CompletionContext c, byte[][][] l, IReadOnlyList<byte[]> own)
        => CampaignCombatActualMovedRepeatBreakdownCompletion.ReplayTrustedSource(source, c.Parent.Ancestor.Request,
            l[0], l[1], l[2], l[3], l[4], l[5], l[6], l[7], l[8], l[9], own, Dependency);
    private static class Codec
    {
        internal static byte[] Canonical(JsonNode value, string kind) => CampaignCombatActualMovedRepeatBreakdownCompletionCodec.Canonical(value, kind);
        internal static string Hash(ReadOnlySpan<byte> value) => CampaignCombatActualMovedRepeatBreakdownCompletionCodec.Hash(value);
    }
}

public sealed class CombatActualMovedRepeatBreakdownParityTests
{
    // Current-revision parity is distinct from the immutable original P01 catalogue.
    private const string Catalogue = "docs/specs/fixtures/combat-actual-moved-repeat-breakdown-refreshed-native-hashes-v1.json";
    public static IEnumerable<object[]> Rows()
    {
        using var fixture = JsonDocument.Parse(CombatActualMovedCycleControlTests.Dependency(Catalogue));
        return fixture.RootElement.GetProperty("rows").EnumerateArray().Select(row => new object[] { row.GetProperty("rootOrdinal").GetInt32(), row.GetProperty("movementCut").GetInt32(), row.GetProperty("profile").GetString()! }).ToArray();
    }
    [Theory, MemberData(nameof(Rows))]
    public void EveryAuthenticTerminalMatchesFrozenHashes(int root, int cut, string profile)
    {
        using var fixture = JsonDocument.Parse(CombatActualMovedCycleControlTests.Dependency(Catalogue));
        var row = fixture.RootElement.GetProperty("rows").EnumerateArray().Single(item => item.GetProperty("rootOrdinal").GetInt32() == root && item.GetProperty("movementCut").GetInt32() == cut && item.GetProperty("profile").GetString() == profile);
        CombatActualMovedRepeatBreakdownCompletionTests.VerifyReference(row);
    }
}
