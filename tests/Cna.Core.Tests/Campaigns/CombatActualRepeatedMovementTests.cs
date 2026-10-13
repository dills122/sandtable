using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatActualPaidAssaultTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualRepeatedMovementTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void GenuineRepeatedSourcePublishesActualOrdinaryMove(int index)
    {
        var trace = Trace(index); var args = Arguments(trace); var source = EmptySource(trace);
        var ancestor = CampaignCombatActualPostAssaultContinuation.ReplayTrustedSource(Ancestor(trace), Context(), args.Selection, args.Rounds, args.Paid, args.Continuation, Dependency);
        Assert.Equal(37, ancestor.Control.GetProperty("stateVersion").GetInt64()); Assert.Equal("repeated", ancestor.Control.GetProperty("status").GetString()); Assert.True(ancestor.Control.GetProperty("closed").GetBoolean());
        Assert.Equal(2, ancestor.Control.GetProperty("activeCycle").GetProperty("ordinal").GetInt32()); Assert.Equal(5, ancestor.Control.GetProperty("members")[0].GetProperty("spentCp").GetProperty("numerator").GetInt64());
        Assert.Empty(ancestor.Control.GetProperty("nextCycleProgress").EnumerateArray()); Assert.Equal(10UL, ancestor.Control.GetProperty("randomState").GetProperty("nextByteCursor").GetUInt64()); Assert.StartsWith("iml.", ancestor.Control.GetProperty("movementEnd").GetProperty("completionReceiptId").GetString());
        Assert.Equal(1, ancestor.Control.GetProperty("movementEnd").GetProperty("ordinal").GetInt32()); Assert.Empty(Packet(source)["movementEventCanonicalUtf8"]!.AsArray());
        byte[][] movement = []; Assert.Empty(movement); var input = Ledger(trace, "movementLedger")[0];
        var malformed = Assert.Throws<JsonException>(() => Apply(source, args, null!, input)); Assert.StartsWith("CMB-RMV-001", malformed.Message);
        var mismatched = Assert.Throws<JsonException>(() => Apply(source, args, [input], input)); Assert.StartsWith("CMB-RMV-004", mismatched.Message);
        var result = Apply(source, args, movement, input);
        Assert.Equal(38, result.Control.GetProperty("stateVersion").GetInt64()); Assert.Equal(9, result.Control.GetProperty("members")[0].GetProperty("spentCp").GetProperty("numerator").GetInt64());
        Assert.Single(result.MovementInputs); Assert.Single(result.Control.GetProperty("nextCycleProgress").EnumerateArray()); Assert.StartsWith("armv.", result.ReceiptId);
        using var ev = JsonDocument.Parse(result.EventBytes!); Assert.Equal(9, ev.RootElement.GetProperty("effect").GetProperty("costs").GetProperty("afterCp").GetInt64()); Assert.Equal(0, ev.RootElement.GetProperty("effect").GetProperty("costs").GetProperty("excessCpDp").GetInt64()); Assert.Single(ev.RootElement.GetProperty("effect").GetProperty("endedMemberships").EnumerateArray());
        Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[1]), result.ControlBytes);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EveryNativeEventControlAndFinalProofSourceMatchesLiteral(int index)
    {
        var trace = Trace(index); var args = Arguments(trace); var source = EmptySource(trace); byte[][] ledger = []; var expected = Packet(Bytes(trace.GetProperty("sourceCanonicalUtf8"))); var all = Ledger(trace, "movementLedger");
        Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[0]), Replay(source, args, ledger).ControlBytes);
        for (var n = 0; n < 4; n++)
        {
            var result = Apply(source, args, ledger, all[n]); Assert.Equal(Encoding.ASCII.GetBytes(expected["movementEventCanonicalUtf8"]![n]!.GetValue<string>()), result.EventBytes); Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[n + 1]), result.ControlBytes); Assert.Equal(all[n], result.MovementInputs[n]); source = result.SourceBytes; ledger = result.MovementInputs.Select(x => x.ToArray()).ToArray();
        }
        var final = Replay(source, args, ledger); Assert.Equal(Bytes(trace.GetProperty("sourceCanonicalUtf8")), source); Assert.Equal(Bytes(trace.GetProperty("proofCanonicalUtf8")), final.ProofBytes);
        Assert.Equal(final.ControlBytes, CampaignCombatActualRepeatedMovement.ReadControl(final.ControlBytes, source, Context(), args.Selection, args.Rounds, args.Paid, args.Continuation, ledger, Dependency).ControlBytes);
        Assert.Equal(final.ProofBytes, CampaignCombatActualRepeatedMovement.ReadProof(final.ProofBytes, source, Context(), args.Selection, args.Rounds, args.Paid, args.Continuation, ledger, Dependency).ProofBytes);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EveryCutAndAllPriorCommandsRecoverOwnedReadback(int index)
    {
        var trace = Trace(index); var args = Arguments(trace); var all = Ledger(trace, "movementLedger");
        for (var cut = 0; cut <= 4; cut++)
        {
            var source = Cut(trace, cut); var ledger = all.Take(cut).ToArray(); var result = Replay(source, args, ledger); Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[cut]), result.ControlBytes);
            var expectedProof = Packet(Bytes(trace.GetProperty("proofCanonicalUtf8"))); var digest = CampaignCombatActualPaidAssaultSupport.Domain("sandtable.combat.actual-repeated-movement.source.v1", source); expectedProof["sourceId"] = "armove." + digest; expectedProof["sourceHash"] = "sha256:" + digest; expectedProof["control"] = Packet(Bytes(trace.GetProperty("controlCanonicalUtf8")[cut])); Assert.Equal(CampaignCombatActualRepeatedMovementCodec.Encode(expectedProof), result.ProofBytes);
            var exposedSource = result.SourceBytes; exposedSource[0] ^= 1; var exposedControl = result.ControlBytes; exposedControl[0] ^= 1; var exposedProof = result.ProofBytes; exposedProof[0] ^= 1; Assert.Equal(source, result.SourceBytes); Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[cut]), result.ControlBytes);
            Assert.Equal(result.ProofBytes, CampaignCombatActualRepeatedMovement.ReadProof(result.ProofBytes, source, Context(), args.Selection, args.Rounds, args.Paid, args.Continuation, ledger, Dependency).ProofBytes);
            for (var prior = 0; prior < cut; prior++)
            {
                var duplicate = Apply(source, args, ledger, all[prior]); Assert.True(duplicate.Duplicate); Assert.Null(duplicate.EventBytes); Assert.Equal(source, duplicate.SourceBytes); Assert.Equal(result.ControlBytes, duplicate.ControlBytes); Assert.Equal(result.ProofBytes, duplicate.ProofBytes); Assert.Equal(ledger.Length, duplicate.MovementInputs.Count);
                for (var j = 0; j < ledger.Length; j++) Assert.Equal(ledger[j], duplicate.MovementInputs[j]);
                using var ev = JsonDocument.Parse(Packet(source)["movementEventCanonicalUtf8"]![prior]!.GetValue<string>()); Assert.Equal(ev.RootElement.GetProperty("receiptId").GetString(), duplicate.ReceiptId); var exposed = duplicate.MovementInputs[0]; exposed[0] ^= 1; Assert.Equal(ledger[0], duplicate.MovementInputs[0]);
            }
        }
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void CumulativeCostsContactProgressAndOriginalFieldsArePreserved(int index)
    {
        var trace = Trace(index); var args = Arguments(trace); var all = Ledger(trace, "movementLedger"); var initial = Packet(Replay(EmptySource(trace), args, []).ControlBytes); var id = initial["members"]![0]!["unit"]!["elementId"]!.GetValue<string>();
        var firstReceipt = Packet(Encoding.ASCII.GetBytes(Packet(Bytes(trace.GetProperty("sourceCanonicalUtf8")))["movementEventCanonicalUtf8"]![0]!.GetValue<string>()))["receiptId"]!.GetValue<string>();
        for (var cut = 0; cut <= 4; cut++)
        {
            var source = Cut(trace, cut); var s = Packet(Replay(source, args, all.Take(cut).ToArray()).ControlBytes); var w = s["world"]!; var own = w["elements"]!.AsArray().Single(x => x!["elementId"]!.GetValue<string>() == id)!;
            Assert.Equal(new long[] { 5, 9, 11, 13, 15 }[cut], CampaignCombatActualPaidAssaultSupport.N(s["members"]![0]!["spentCp"]!, "numerator")); Assert.True(JsonNode.DeepEquals(s["members"]![0]!["spentCp"], own["operationalState"]!["capabilityPointsExpended"])); Assert.Equal(new long[] { 0, 0, -1, -3, -5 }[cut], CampaignCombatActualPaidAssaultSupport.N(own["operationalState"]!, "cohesionLevel"));
            foreach (var key in Preserved) Assert.True(JsonNode.DeepEquals(initial[key], s[key]), key); Assert.True(JsonNode.DeepEquals(initial["members"]![0]!["history"], s["members"]![0]!["history"])); Assert.Empty(s["targetUses"]!.AsArray()); Assert.Equal(cut, s["nextCycleProgress"]!.AsArray().Count); Assert.Equal(Math.Max(0, cut - 1), w["cohesionCauses"]!.AsArray().Count);
            for (var n = 0; n < cut; n++) { var bytes = Encoding.ASCII.GetBytes(Packet(source)["movementEventCanonicalUtf8"]![n]!.GetValue<string>()); var ev = Packet(bytes); Assert.Equal(ev["receiptId"]!.GetValue<string>(), s["nextCycleProgress"]![n]!["receiptId"]!.GetValue<string>()); Assert.Equal(CampaignCombatActualPaidAssaultSupport.Hash(bytes), s["nextCycleProgress"]![n]!["eventHash"]!.GetValue<string>()); var cost = ev["effect"]!["costs"]!; Assert.Equal(new long[] { 0, 1, 2, 2 }[n], CampaignCombatActualPaidAssaultSupport.N(cost, "excessCpDp")); Assert.Equal(n == 0 ? 2 : 0, CampaignCombatActualPaidAssaultSupport.N(cost, "breakOffCost")); Assert.Equal(n == 0 ? 1 : 0, ev["effect"]!["endedMemberships"]!.AsArray().Count); }
            foreach (var field in w.AsObject().Where(x => x.Key is not ("elements" or "representations" or "relationships" or "cohesionCauses"))) Assert.True(JsonNode.DeepEquals(initial["world"]![field.Key], field.Value), field.Key);
            foreach (var element in w["elements"]!.AsArray()) { var old = initial["world"]!["elements"]!.AsArray().Single(x => x!["elementId"]!.GetValue<string>() == element!["elementId"]!.GetValue<string>())!; var restored = element!.DeepClone(); if (element["elementId"]!.GetValue<string>() == id) { restored["currentLocationId"] = old["currentLocationId"]!.DeepClone(); restored["operationalState"]!["capabilityPointsExpended"] = old["operationalState"]!["capabilityPointsExpended"]!.DeepClone(); restored["operationalState"]!["cohesionLevel"] = old["operationalState"]!["cohesionLevel"]!.DeepClone(); } Assert.True(JsonNode.DeepEquals(old, restored)); }
            if (cut > 0) { Assert.False(w["relationships"]![0]!["active"]!.GetValue<bool>()); Assert.Equal(firstReceipt, w["relationships"]![0]!["endedByReceiptId"]!.GetValue<string>()); Assert.Equal(cut + 1, s["tracks"]![0]!["route"]!.AsArray().Count); }
        }
    }
    private static readonly string[] Preserved = ["originalCycle", "activeCycle", "closure", "movementEnd", "randomState", "attackHistory"];
    internal static byte[] Cut(JsonElement trace, int count) { var p = Packet(Bytes(trace.GetProperty("sourceCanonicalUtf8"))); var events = p["movementEventCanonicalUtf8"]!.AsArray(); while (events.Count > count) events.RemoveAt(events.Count - 1); return CampaignCombatActualRepeatedMovementCodec.Encode(p); }
    internal static CombatActualRepeatedMovementResult Replay(byte[] source, Inputs a, IReadOnlyList<byte[]> movement) => CampaignCombatActualRepeatedMovement.ReplayTrustedSource(source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, movement, Dependency);

    internal sealed record Inputs(byte[][] Selection, byte[][] Rounds, byte[][] Paid, byte[][] Continuation);
    internal static JsonElement Trace(int index) { using var f = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-actual-repeated-movement-v1.json"))); return f.RootElement.GetProperty("traces")[index].Clone(); }
    internal static Inputs Arguments(JsonElement trace)
    {
        using var f = CombatActualPostAssaultContinuationTests.Fixture(); var pac = f.RootElement.GetProperty("traces").EnumerateArray().Single(x => x.GetProperty("owner").GetString() == trace.GetProperty("owner").GetString() && x.GetProperty("firstSeal").GetString() == trace.GetProperty("firstSeal").GetString() && x.GetProperty("outcome").GetString() == "repeat");
        var a = CombatActualPostAssaultContinuationTests.Arguments(pac); return new(a.Selection, a.Rounds, a.Paid, Ledger(pac, "continuationLedger"));
    }
    internal static byte[] Bytes(JsonElement value) => Encoding.ASCII.GetBytes(value.GetString()!);
    internal static JsonObject Packet(byte[] bytes) => JsonNode.Parse(bytes)!.AsObject();
    internal static byte[] Ancestor(JsonElement trace) => Encoding.ASCII.GetBytes(Packet(Bytes(trace.GetProperty("sourceCanonicalUtf8")))["continuationSourceCanonicalUtf8"]!.GetValue<string>());
    internal static byte[] EmptySource(JsonElement trace) { var p = Packet(Bytes(trace.GetProperty("sourceCanonicalUtf8"))); p["movementEventCanonicalUtf8"] = new JsonArray(); return CampaignCombatActualRepeatedMovementCodec.Encode(p); }
    internal static CombatActualRepeatedMovementResult Apply(byte[] source, Inputs a, IReadOnlyList<byte[]> movement, byte[] input) => CampaignCombatActualRepeatedMovement.ApplyTrustedSource(source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, movement, input, Dependency);
}
