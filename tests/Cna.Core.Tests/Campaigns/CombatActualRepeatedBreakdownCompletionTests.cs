using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatActualPaidAssaultTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualRepeatedBreakdownCompletionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(19)]
    public void AuthenticTerminalRmcPublishesOneSystemBreakdownAfterSeventhLedgerChecks(int index)
    {
        var trace = Trace(index); var args = Arguments(trace); var source = EmptySource(trace);
        var prior = CampaignCombatActualRepeatedMovementCompletion.ReplayTrustedSource(Parent(source), Context(), args.Selection, args.Rounds, args.Paid, args.Continuation, args.Movement, args.Lifecycle, Dependency);
        var rmc = CombatActualRepeatedMovementCompletionTests.Trace(index);
        Assert.Equal(Bytes(rmc.GetProperty("controlCanonicalUtf8")[args.Lifecycle.Length]), prior.ControlBytes);
        var moves = trace.GetProperty("moves").GetInt32(); Assert.Equal(moves == 0 ? 38 : 40 + moves, prior.Control.GetProperty("stateVersion").GetInt64());
        Assert.Equal(2, prior.Control.GetProperty("activeCycle").GetProperty("ordinal").GetInt32()); Assert.Equal(2, prior.Control.GetProperty("movementEnd").GetProperty("ordinal").GetInt32());
        Assert.StartsWith("armc.", prior.Control.GetProperty("movementEnd").GetProperty("completionReceiptId").GetString()); Assert.True(prior.Control.GetProperty("closed").GetBoolean());
        Assert.Equal(10UL, prior.Control.GetProperty("randomState").GetProperty("nextByteCursor").GetUInt64()); Assert.Equal(moves == 0 ? 1 : 3, args.Lifecycle.Length);
        var input = Ledger(trace, "breakdownLedger")[0]; Reject(1, () => Apply(source, args, null!, input)); Reject(1, () => Apply(source, args, [null!], input)); Reject(4, () => Apply(source, args, [input], input));
        var broken = Packet(source); broken["repeatedMovementCompletionSourceCanonicalUtf8"] = "{}"; Reject(1, () => CampaignCombatActualRepeatedBreakdownCompletion.ReadControl([], Encode(broken), Context(), args.Selection, args.Rounds, args.Paid, args.Continuation, args.Movement, args.Lifecycle, null!, Dependency));
        var result = Apply(source, args, [], input);
        Assert.Equal(prior.Control.GetProperty("stateVersion").GetInt64() + 1, result.Control.GetProperty("stateVersion").GetInt64()); Assert.Single(result.BreakdownInputs); Assert.StartsWith("arbc.", result.ReceiptId);
        Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[1]), result.ControlBytes); Assert.True(result.Control.GetProperty("closed").GetBoolean());
        Assert.Equal("land.position.operation-1.first-player.movement-and-combat.combat.position-determination", result.Control.GetProperty("positionId").GetString());
        using var ev = JsonDocument.Parse(result.EventBytes!); Assert.Equal("system", ev.RootElement.GetProperty("author").GetString()); Assert.Equal("breakdown-completed", ev.RootElement.GetProperty("effect").GetProperty("kind").GetString());
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
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(10)]
    [InlineData(11)]
    [InlineData(12)]
    [InlineData(13)]
    [InlineData(14)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(18)]
    [InlineData(19)]
    public void EveryNativeBreakdownLiteralAndInheritedStateIsPreserved(int index)
    {
        var trace = Trace(index); var a = Arguments(trace); var input = Ledger(trace, "breakdownLedger")[0]; var initialSource = EmptySource(trace);
        var parent = CampaignCombatActualRepeatedMovementCompletion.ReplayTrustedSource(Parent(initialSource), Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, Dependency);
        var initial = Replay(initialSource, a, []); Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[0]), initial.ControlBytes); Assert.Equal(ExpectedControl(trace, parent, 0), initial.ControlBytes);
        var frame = CampaignCombatActualRepeatedBreakdownCompletion.Derive(Parent(initialSource), Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, Dependency);
        Assert.Equal(input, CampaignCombatActualRepeatedBreakdownCompletionCodec.Canonical(CampaignCombatActualRepeatedBreakdownCompletion.Command(frame, Packet(initial.ControlBytes)), "Input"));
        var result = Apply(initialSource, a, [], input); var finalSource = Bytes(trace.GetProperty("sourceCanonicalUtf8")); var expectedEvent = Encoding.ASCII.GetBytes(Packet(finalSource)["breakdownEventCanonicalUtf8"]![0]!.GetValue<string>());
        Assert.Equal(expectedEvent, result.EventBytes); Assert.Equal(finalSource, result.SourceBytes); Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[1]), result.ControlBytes); Assert.Equal(ExpectedControl(trace, parent, 1), result.ControlBytes);
        Assert.Equal(Bytes(trace.GetProperty("proofCanonicalUtf8")), result.ProofBytes); Assert.Equal(ExpectedProof(trace, parent, finalSource, 1), result.ProofBytes); Assert.Equal(input, Assert.Single(result.BreakdownInputs));
        var before = parent.Control; var after = result.Control;
        foreach (var field in new[] { "originalCycle", "activeCycle", "closure", "priorMovementEnd", "movementEnd", "members", "world", "randomState", "attackHistory", "targetUses", "nextCycleProgress", "tracks", "movementReceipts", "flow", "interruptContext", "movementSourceHash" }) Assert.Equal(before.GetProperty(field).GetRawText(), after.GetProperty(field).GetRawText());
        Assert.Equal(before.GetProperty("receipts").GetRawText(), after.GetProperty("priorLifecycleReceipts").GetRawText()); Assert.Equal(2, after.GetProperty("activeCycle").GetProperty("ordinal").GetInt32()); Assert.True(after.GetProperty("closed").GetBoolean());
        Assert.Equal(result.ReceiptId, after.GetProperty("breakdownCompletionReceiptId").GetString()); Assert.NotEqual(result.ReceiptId, after.GetProperty("movementEnd").GetProperty("completionReceiptId").GetString()); Assert.NotEqual(result.ReceiptId, after.GetProperty("priorMovementEnd").GetProperty("completionReceiptId").GetString());
        using var ev = JsonDocument.Parse(result.EventBytes!); var e = ev.RootElement; Assert.Equal(before.GetProperty("stateVersion").GetInt64() + 1, e.GetProperty("stateVersion").GetInt64()); Assert.Equal(before.GetProperty("positionId").GetString(), e.GetProperty("positionId").GetString());
        var boundary = Cna.Core.Rules.Cna1979LandSequence.CreateTurn(1).Single(p => p.PositionId == before.GetProperty("positionId").GetString()); Assert.Equal(Cna.Core.Rules.Cna1979LandSequence.GetNext(boundary).PositionId, e.GetProperty("effect").GetProperty("successorPositionId").GetString());
        Assert.Equal(boundary.Sources.Select(r => r.SourceId + ":" + r.Locator), e.GetProperty("effect").GetProperty("sources").EnumerateArray().Select(r => r.GetProperty("sourceId").GetString() + ":" + r.GetProperty("locator").GetString())); Assert.Equal(before.GetProperty("movementEnd").GetProperty("completionReceiptId").GetString(), e.GetProperty("effect").GetProperty("movementCompletionReceiptId").GetString());
    }
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    public void ZeroMovesBothCutsAndExactRetryRecoverOwnedReadback(int index) => Recover(index);
    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(11)]
    [InlineData(16)]
    public void OneMoveBothCutsAndExactRetryRecoverOwnedReadback(int index) => Recover(index);
    [Theory]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(12)]
    [InlineData(17)]
    public void TwoMovesBothCutsAndExactRetryRecoverOwnedReadback(int index) => Recover(index);
    [Theory]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(13)]
    [InlineData(18)]
    public void ThreeMovesBothCutsAndExactRetryRecoverOwnedReadback(int index) => Recover(index);
    [Theory]
    [InlineData(4)]
    [InlineData(9)]
    [InlineData(14)]
    [InlineData(19)]
    public void FourMovesBothCutsAndExactRetryRecoverOwnedReadback(int index) => Recover(index);
    private static void Recover(int index)
    {
        var trace = Trace(index); var a = Arguments(trace); var all = Ledger(trace, "breakdownLedger"); var parent = CampaignCombatActualRepeatedMovementCompletion.ReplayTrustedSource(Parent(EmptySource(trace)), Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, Dependency);
        for (var cut = 0; cut <= 1; cut++)
        {
            var source = cut == 0 ? EmptySource(trace) : Bytes(trace.GetProperty("sourceCanonicalUtf8")); var ledger = all.Take(cut).ToArray(); var result = Replay(source, a, ledger); var expectedControl = ExpectedControl(trace, parent, cut); var expectedProof = ExpectedProof(trace, parent, source, cut);
            Assert.Equal(expectedControl, result.ControlBytes); Assert.Equal(expectedProof, result.ProofBytes);
            Assert.Equal(source, CampaignCombatActualRepeatedBreakdownCompletion.ReadSource(source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, ledger, Dependency));
            Assert.Equal(expectedControl, CampaignCombatActualRepeatedBreakdownCompletion.ReadControl(expectedControl, source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, ledger, Dependency).ControlBytes);
            Assert.Equal(expectedProof, CampaignCombatActualRepeatedBreakdownCompletion.ReadProof(expectedProof, source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, ledger, Dependency).ProofBytes);
            var exposed = result.SourceBytes; exposed[0] ^= 1; exposed = result.ControlBytes; exposed[0] ^= 1; exposed = result.ProofBytes; exposed[0] ^= 1; Assert.Equal(source, result.SourceBytes); Assert.Equal(expectedControl, result.ControlBytes); Assert.Equal(expectedProof, result.ProofBytes);
            var accepted = Apply(source, a, ledger, all[0]); Assert.Equal(Bytes(trace.GetProperty("sourceCanonicalUtf8")), accepted.SourceBytes); Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[1]), accepted.ControlBytes); Assert.Equal(Bytes(trace.GetProperty("proofCanonicalUtf8")), accepted.ProofBytes); Assert.Single(accepted.BreakdownInputs);
            if (cut == 1) { Assert.True(accepted.Duplicate); Assert.Null(accepted.EventBytes); } else { Assert.False(accepted.Duplicate); Assert.NotNull(accepted.EventBytes); }
            Assert.Equal(all[0], accepted.BreakdownInputs[0]); exposed = accepted.BreakdownInputs[0]; exposed[0] ^= 1; Assert.Equal(all[0], accepted.BreakdownInputs[0]); if (accepted.EventBytes is { } bytes) { bytes[0] ^= 1; Assert.NotEqual(bytes, accepted.EventBytes); }
        }
    }
    internal static JsonObject ExpectedBase(CombatActualRepeatedMovementCompletionResult parent)
    {
        using var schema = JsonDocument.Parse(Dependency("docs/specs/combat-actual-repeated-breakdown-completion-v1.schema.json")); var manifest = new JsonObject(); foreach (var field in schema.RootElement.GetProperty("runtimeSourceHashes").EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal)) manifest[field.Name] = field.Value.GetString();
        return new JsonObject { ["contractVersion"] = 1, ["dependencyManifestHash"] = "sha256:" + CampaignCombatActualPaidAssaultSupport.Domain("sandtable.combat.actual-repeated-breakdown-completion.dependency-manifest.v1", Encode(manifest)), ["repeatedMovementCompletionProof"] = Packet(parent.ProofBytes) };
    }
    internal static byte[] ExpectedControl(JsonElement trace, CombatActualRepeatedMovementCompletionResult parent, int cut)
    {
        var state = Packet(parent.ControlBytes); var basis = ExpectedBase(parent); state["baseHash"] = "sha256:" + CampaignCombatActualPaidAssaultSupport.Domain("sandtable.combat.actual-repeated-breakdown-completion.base.v1", Encode(basis)); state["repeatedMovementCompletionSourceHash"] = basis["repeatedMovementCompletionProof"]!["sourceHash"]!.DeepClone(); state["priorLifecycleReceipts"] = state["receipts"]!.DeepClone(); state["receipts"] = new JsonArray(); state["breakdownCompletionReceiptId"] = null; state["closed"] = false;
        if (cut == 1)
        {
            var evBytes = Encoding.ASCII.GetBytes(Packet(Bytes(trace.GetProperty("sourceCanonicalUtf8")))["breakdownEventCanonicalUtf8"]![0]!.GetValue<string>()); var unsigned = Packet(evBytes); unsigned.Remove("receiptId"); var rid = "arbc." + CampaignCombatActualPaidAssaultSupport.Domain("sandtable.combat.actual-repeated-breakdown-completion.receipt.v1", Encode(unsigned));
            state["stateVersion"] = parent.Control.GetProperty("stateVersion").GetInt64() + 1; state["prefix"] = CampaignOpeningPreambleCodec.EventPrefix(parent.Control.GetProperty("prefix").GetString()!, evBytes); state["positionId"] = "land.position.operation-1.first-player.movement-and-combat.combat.position-determination"; state["breakdownCompletionReceiptId"] = rid; state["closed"] = true;
            var command = Packet(Ledger(trace, "breakdownLedger")[0])["command"]!; state["receipts"]!.AsArray().Add(new JsonObject { ["commandHash"] = CampaignCombatActualPaidAssaultSupport.Hash(Encode(command)), ["eventHash"] = CampaignCombatActualPaidAssaultSupport.Hash(evBytes), ["receiptId"] = rid, ["actor"] = "system", ["stateVersion"] = state["stateVersion"]!.DeepClone() });
        }
        return CampaignCombatActualRepeatedBreakdownCompletionCodec.Canonical(state, "Control");
    }
    internal static byte[] ExpectedProof(JsonElement trace, CombatActualRepeatedMovementCompletionResult parent, byte[] source, int cut)
    {
        var hash = CampaignCombatActualPaidAssaultSupport.Domain("sandtable.combat.actual-repeated-breakdown-completion.source.v1", source);
        return Encode(new JsonObject { ["contractVersion"] = 1, ["sourceId"] = "arbcomp." + hash, ["sourceHash"] = "sha256:" + hash, ["base"] = ExpectedBase(parent), ["control"] = Packet(ExpectedControl(trace, parent, cut)) });
    }
    private static readonly Lazy<JsonElement[]> Traces = new(() =>
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-actual-repeated-breakdown-completion-v1.json")));
        return fixture.RootElement.GetProperty("traces").EnumerateArray().Select(x => x.Clone()).ToArray();
    });
    internal sealed record Inputs(byte[][] Selection, byte[][] Rounds, byte[][] Paid, byte[][] Continuation, byte[][] Movement, byte[][] Lifecycle);
    internal static JsonElement Trace(int index) => Traces.Value[index];
    internal static Inputs Arguments(JsonElement trace)
    {
        var row = Enumerable.Range(0, 20).Select(CombatActualRepeatedMovementCompletionTests.Trace).Single(x => x.GetProperty("owner").GetString() == trace.GetProperty("owner").GetString() && x.GetProperty("firstSeal").GetString() == trace.GetProperty("firstSeal").GetString() && x.GetProperty("moves").GetInt32() == trace.GetProperty("moves").GetInt32());
        var a = CombatActualRepeatedMovementCompletionTests.Arguments(row); return new(a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, Ledger(row, "lifecycleLedger"));
    }
    internal static byte[] Bytes(JsonElement value) => Encoding.ASCII.GetBytes(value.GetString()!);
    internal static JsonObject Packet(byte[] bytes) => JsonNode.Parse(bytes)!.AsObject();
    internal static byte[] Encode(JsonNode value) => CampaignCombatActualRepeatedBreakdownCompletionCodec.Encode(value);
    internal static byte[] Parent(byte[] source) => Encoding.ASCII.GetBytes(Packet(source)["repeatedMovementCompletionSourceCanonicalUtf8"]!.GetValue<string>());
    internal static byte[] EmptySource(JsonElement trace) { var p = Packet(Bytes(trace.GetProperty("sourceCanonicalUtf8"))); p["breakdownEventCanonicalUtf8"] = new JsonArray(); return Encode(p); }
    internal static CombatActualRepeatedBreakdownCompletionResult Apply(byte[] source, Inputs a, IReadOnlyList<byte[]> breakdown, byte[] input)
        => CampaignCombatActualRepeatedBreakdownCompletion.ApplyTrustedSource(source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, breakdown, input, Dependency);
    internal static CombatActualRepeatedBreakdownCompletionResult Replay(byte[] source, Inputs a, IReadOnlyList<byte[]> breakdown)
        => CampaignCombatActualRepeatedBreakdownCompletion.ReplayTrustedSource(source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, a.Lifecycle, breakdown, Dependency);
    internal static void Reject(int code, Action action) => Assert.StartsWith($"CMB-ABC-{code:000}", Assert.Throws<JsonException>(action).Message);
}
