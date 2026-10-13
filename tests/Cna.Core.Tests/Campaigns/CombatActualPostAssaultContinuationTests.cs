using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatActualPaidAssaultTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualPostAssaultContinuationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void GenuineActualPaidTerminalPublishesExplicitReleaseOpening(int index)
    {
        using var fixture = Fixture(); var trace = fixture.RootElement.GetProperty("traces")[index];
        using var paidFixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-actual-paid-assault-v1.json")));
        var row = PaidRow(paidFixture, trace); var selection = Ledger(row, "selectionLedger"); var rounds = Ledger(row, "roundLedger"); var paid = Ledger(row, "paidLedger"); var context = Context();
        var predecessor = Encoding.ASCII.GetBytes(row.GetProperty("sourceCanonicalUtf8").GetString()!);
        var terminal = CampaignCombatActualPaidAssault.ReplayTrustedSource(predecessor, context, selection, rounds, paid, Dependency);
        Assert.Equal(33, terminal.Control.GetProperty("stateVersion").GetInt64());
        Assert.True(terminal.Control.GetProperty("closed").GetBoolean());
        Assert.Equal(1UL, terminal.Control.GetProperty("randomState").GetProperty("seed").GetUInt64());
        Assert.Equal(10UL, terminal.Control.GetProperty("randomState").GetProperty("nextByteCursor").GetUInt64());
        var selectionProof = terminal.Proof.GetProperty("base").GetProperty("admissionProof").GetProperty("actualRoundProof").GetProperty("base").GetProperty("actualSelectionProof");
        Assert.StartsWith("iml.", selectionProof.GetProperty("boundary").GetProperty("movementEnd").GetProperty("completionReceiptId").GetString());
        var ownId = selectionProof.GetProperty("positiveEntryProof").GetProperty("entry").GetProperty("members")[0].GetProperty("unit").GetProperty("elementId").GetString();
        var own = terminal.Control.GetProperty("world").GetProperty("elements").EnumerateArray().Single(e => e.GetProperty("elementId").GetString() == ownId);
        Assert.Equal(5, own.GetProperty("operationalState").GetProperty("capabilityPointsExpended").GetProperty("numerator").GetInt64());
        Assert.Equal(0, own.GetProperty("ammunition").GetProperty("points").GetInt32());
        var source = CampaignCombatActualPaidAssaultCodec.Encode(new JsonObject { ["contractVersion"] = 1, ["paidSourceCanonicalUtf8"] = Encoding.ASCII.GetString(predecessor), ["continuationEventCanonicalUtf8"] = new JsonArray() });
        var input = Encode(trace.GetProperty("continuationLedger")[0]);

        var result = CampaignCombatActualPostAssaultContinuation.ApplyTrustedSource(source, context, selection, rounds, paid, [], input, Dependency);

        Assert.Equal(34, result.Control.GetProperty("stateVersion").GetInt64());
        Assert.Single(result.ContinuationInputs);
        Assert.Equal(input, result.ContinuationInputs[0]);
        Assert.StartsWith("apc.", result.ReceiptId);
        Assert.Equal(terminal.Control.GetProperty("world").GetRawText(), result.Control.GetProperty("world").GetRawText());
        var expected = JsonNode.Parse(trace.GetProperty("sourceCanonicalUtf8").GetString()!)!.AsObject()["continuationEventCanonicalUtf8"]![0]!.GetValue<string>();
        Assert.Equal(Encoding.ASCII.GetBytes(expected), result.EventBytes);
        Assert.Equal(Encoding.ASCII.GetBytes(trace.GetProperty("controlCanonicalUtf8")[1].GetString()!), result.ControlBytes);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void AllGenuineEventsFramesAndFinalProofMatchFrozenContract(int index)
    {
        var trace = Trace(index); var args = Arguments(trace); var source = EmptySource(trace); byte[][] ledger = [];
        var initial = Replay(source, args, ledger); Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[0]), initial.ControlBytes);
        var expected = JsonNode.Parse(trace.GetProperty("sourceCanonicalUtf8").GetString()!)!.AsObject();
        for (var step = 0; step < 4; step++)
        {
            var input = Encode(trace.GetProperty("continuationLedger")[step]); var result = CampaignCombatActualPostAssaultContinuation.ApplyTrustedSource(source, Context(), args.Selection, args.Rounds, args.Paid, ledger, input, Dependency);
            Assert.Equal(Encoding.ASCII.GetBytes(expected["continuationEventCanonicalUtf8"]![step]!.GetValue<string>()), result.EventBytes); Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[step + 1]), result.ControlBytes);
            foreach (var field in PreservedFields) Assert.Equal(initial.Control.GetProperty(field).GetRawText(), result.Control.GetProperty(field).GetRawText());
            Assert.Equal(34 + step, result.Control.GetProperty("stateVersion").GetInt64()); source = result.SourceBytes; ledger = result.ContinuationInputs.Select(x => x.ToArray()).ToArray();
        }
        var final = Replay(source, args, ledger); Assert.Equal(Bytes(trace.GetProperty("sourceCanonicalUtf8")), source); Assert.Equal(Bytes(trace.GetProperty("proofCanonicalUtf8")), final.ProofBytes);
        var assessment = final.Control.GetProperty("assessment"); var witness = Assert.Single(assessment.GetProperty("witnesses").EnumerateArray()); Assert.Equal(9, witness.GetProperty("afterCp").GetInt64()); Assert.Equal(0, witness.GetProperty("excessCpDp").GetInt64());
        Assert.Equal("combat-actual-paid-attack-committed", Assert.Single(assessment.GetProperty("progress").EnumerateArray()).GetProperty("eventType").GetString());
        var ownId = witness.GetProperty("unit").GetProperty("elementId").GetString(); var element = final.Control.GetProperty("world").GetProperty("elements").EnumerateArray().Single(e => e.GetProperty("elementId").GetString() == ownId); Assert.Equal(5, element.GetProperty("operationalState").GetProperty("capabilityPointsExpended").GetProperty("numerator").GetInt64());
        if (trace.GetProperty("outcome").GetString() == "repeat")
        {
            Assert.Equal(2, final.Control.GetProperty("activeCycle").GetProperty("ordinal").GetInt32()); Assert.Equal(37, final.Control.GetProperty("activeCycle").GetProperty("openedAuthorityVersion").GetInt64());
            using var ev = JsonDocument.Parse(expected["continuationEventCanonicalUtf8"]![3]!.GetValue<string>()); Assert.Equal(ev.RootElement.GetProperty("priorPrefix").GetString(), final.Control.GetProperty("activeCycle").GetProperty("openingPrefix").GetString()); Assert.Empty(final.Control.GetProperty("targetUses").EnumerateArray()); Assert.Empty(final.Control.GetProperty("nextCycleProgress").EnumerateArray());
        }
        else { Assert.Equal(JsonValueKind.Null, final.Control.GetProperty("activeCycle").ValueKind); Assert.EndsWith("truck-convoy-movement", final.Control.GetProperty("positionId").GetString()); }
        Assert.Equal(final.ControlBytes, CampaignCombatActualPostAssaultContinuation.ReadControl(final.ControlBytes, source, Context(), args.Selection, args.Rounds, args.Paid, ledger, Dependency).ControlBytes);
        Assert.Equal(final.ProofBytes, CampaignCombatActualPostAssaultContinuation.ReadProof(final.ProofBytes, source, Context(), args.Selection, args.Rounds, args.Paid, ledger, Dependency).ProofBytes);
    }
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void EveryCutAuthenticatesReadbackOwnedOutputsAndLostReplyRetry(int index)
    {
        var trace = Trace(index); var args = Arguments(trace); var full = JsonNode.Parse(trace.GetProperty("sourceCanonicalUtf8").GetString()!)!.AsObject(); var all = Ledger(trace, "continuationLedger");
        for (var cut = 0; cut <= 4; cut++)
        {
            var packet = full.DeepClone().AsObject(); var events = packet["continuationEventCanonicalUtf8"]!.AsArray(); while (events.Count > cut) events.RemoveAt(events.Count - 1); var source = CampaignCombatActualPostAssaultContinuationCodec.Encode(packet); var ledger = all.Take(cut).Select(x => x.ToArray()).ToArray(); var result = Replay(source, args, ledger);
            Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[cut]), result.ControlBytes); var exposedSource = result.SourceBytes; exposedSource[0] ^= 1; var exposedControl = result.ControlBytes; exposedControl[0] ^= 1; Assert.Equal(source, result.SourceBytes); Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[cut]), result.ControlBytes);
            if (cut == 0) continue; var retry = JsonNode.Parse(all[cut - 1])!.AsObject(); if (cut > 2) { retry["admittedAt"] = null; retry["clockAvailable"] = false; }
            var duplicate = CampaignCombatActualPostAssaultContinuation.ApplyTrustedSource(source, Context(), args.Selection, args.Rounds, args.Paid, ledger, CampaignCombatActualPostAssaultContinuationCodec.Encode(retry), Dependency); Assert.True(duplicate.Duplicate); Assert.Null(duplicate.EventBytes); Assert.Equal(source, duplicate.SourceBytes); Assert.Equal(result.ControlBytes, duplicate.ControlBytes);
            using var expected = JsonDocument.Parse(packet["continuationEventCanonicalUtf8"]![cut - 1]!.GetValue<string>()); Assert.Equal(expected.RootElement.GetProperty("receiptId").GetString(), duplicate.ReceiptId); var exposedInput = duplicate.ContinuationInputs[0]; exposedInput[0] ^= 1; Assert.Equal(ledger[0], duplicate.ContinuationInputs[0]);
        }
    }
    private static readonly string[] PreservedFields = ["world", "randomState", "members", "attackHistory", "movementEnd"];
    internal sealed record Inputs(byte[][] Selection, byte[][] Rounds, byte[][] Paid);
    internal static JsonElement Trace(int index) { using var fixture = Fixture(); return fixture.RootElement.GetProperty("traces")[index].Clone(); }
    internal static Inputs Arguments(JsonElement trace) { using var paid = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-actual-paid-assault-v1.json"))); var row = PaidRow(paid, trace); return new(Ledger(row, "selectionLedger"), Ledger(row, "roundLedger"), Ledger(row, "paidLedger")); }
    internal static byte[] Bytes(JsonElement value) => Encoding.ASCII.GetBytes(value.GetString()!);
    internal static byte[] EmptySource(JsonElement trace) { var source = JsonNode.Parse(trace.GetProperty("sourceCanonicalUtf8").GetString()!)!.AsObject(); source["continuationEventCanonicalUtf8"] = new JsonArray(); return CampaignCombatActualPostAssaultContinuationCodec.Encode(source); }
    internal static CombatActualPostAssaultContinuationResult Replay(byte[] source, Inputs args, IReadOnlyList<byte[]> ledger) => CampaignCombatActualPostAssaultContinuation.ReplayTrustedSource(source, Context(), args.Selection, args.Rounds, args.Paid, ledger, Dependency);
    internal static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-actual-post-assault-continuation-v1.json")));
    internal static JsonElement PaidRow(JsonDocument paidFixture, JsonElement trace) => paidFixture.RootElement.GetProperty("cases").EnumerateArray().Single(row => row.GetProperty("owner").GetString() == trace.GetProperty("owner").GetString() && row.GetProperty("firstSeal").GetString() == trace.GetProperty("firstSeal").GetString());
}
