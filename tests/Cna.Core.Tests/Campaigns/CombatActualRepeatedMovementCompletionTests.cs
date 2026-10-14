using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatActualPaidAssaultTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatActualRepeatedMovementCompletionTests
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
    public void AuthenticZeroCompletionOrPositiveStopPublishesAfterSixthLedgerChecks(int index)
    {
        var trace = Trace(index); var args = Arguments(trace); var source = EmptySource(trace); var movement = Parent(source);
        var prior = CampaignCombatActualRepeatedMovement.ReplayTrustedSource(movement, Context(), args.Selection, args.Rounds, args.Paid, args.Continuation, args.Movement, Dependency);
        var count = trace.GetProperty("moves").GetInt32(); Assert.Equal(37 + count, prior.Control.GetProperty("stateVersion").GetInt64());
        Assert.Equal(2, prior.Control.GetProperty("activeCycle").GetProperty("ordinal").GetInt32()); Assert.Equal(1, prior.Control.GetProperty("movementEnd").GetProperty("ordinal").GetInt32());
        Assert.Equal(new long[] { 5, 9, 11, 13, 15 }[count], prior.Control.GetProperty("members")[0].GetProperty("spentCp").GetProperty("numerator").GetInt64());
        Assert.Equal(10UL, prior.Control.GetProperty("randomState").GetProperty("nextByteCursor").GetUInt64()); Assert.Equal(count, prior.Control.GetProperty("receipts").GetArrayLength());
        var input = Ledger(trace, "lifecycleLedger")[0]; Reject(1, () => Apply(source, args, null!, input)); Reject(1, () => Apply(source, args, [null!], input)); Reject(4, () => Apply(source, args, [input], input));
        var broken = Packet(source); broken["movementSourceCanonicalUtf8"] = "{}"; Reject(1, () => CampaignCombatActualRepeatedMovementCompletion.ReadControl([], Encode(broken), Context(), args.Selection, args.Rounds, args.Paid, args.Continuation, args.Movement, null!, Dependency));
        var result = Apply(source, args, [], input);
        Assert.Equal(38 + count, result.Control.GetProperty("stateVersion").GetInt64()); Assert.Single(result.LifecycleInputs); Assert.StartsWith("armc.", result.ReceiptId);
        Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[1]), result.ControlBytes);
        using var ev = JsonDocument.Parse(result.EventBytes!); var effect = ev.RootElement.GetProperty("effect"); Assert.Equal(count == 0 ? "movement-completed" : "movement-stopped", effect.GetProperty("kind").GetString());
        if (count == 0) { Assert.True(result.Control.GetProperty("closed").GetBoolean()); Assert.Equal(2, result.Control.GetProperty("movementEnd").GetProperty("ordinal").GetInt32()); }
        else { Assert.Equal("phasing-stop", result.Control.GetProperty("flow").GetProperty("kind").GetString()); Assert.Equal(2, result.Control.GetProperty("interruptContext").GetProperty("cycle").GetProperty("ordinal").GetInt32()); }
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
    public void EveryNativeLifecycleLiteralAndStateIsPreserved(int index)
    {
        var trace = Trace(index); var a = Arguments(trace); var all = Ledger(trace, "lifecycleLedger"); var source = EmptySource(trace); byte[][] ledger = [];
        var prior = CampaignCombatActualRepeatedMovement.ReplayTrustedSource(Parent(source), Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, Dependency).Control;
        var result = Replay(source, a, ledger); Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[0]), result.ControlBytes); Preserve(prior, result.Control, 0);
        var events = Packet(Bytes(trace.GetProperty("sourceCanonicalUtf8")))["lifecycleEventCanonicalUtf8"]!.AsArray();
        for (var i = 0; i < all.Length; i++)
        {
            result = Apply(source, a, ledger, all[i]); Assert.Equal(Encoding.ASCII.GetBytes(events[i]!.GetValue<string>()), result.EventBytes); Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[i + 1]), result.ControlBytes);
            source = result.SourceBytes; ledger = result.LifecycleInputs.Select(x => x.ToArray()).ToArray(); Assert.Equal(Cut(trace, i + 1), source); Preserve(prior, result.Control, i + 1);
            using var ev = JsonDocument.Parse(result.EventBytes!); Assert.Equal(ev.RootElement.GetProperty("receiptId").GetString(), result.ReceiptId);
            var effect = ev.RootElement.GetProperty("effect");
            if (effect.GetProperty("kind").GetString() == "stop-resolved")
            {
                Assert.Equal("system", ev.RootElement.GetProperty("author").GetString()); Assert.Empty(effect.GetProperty("checks").EnumerateArray()); Assert.Empty(effect.GetProperty("createdLots").EnumerateArray()); Assert.Equal(prior.GetProperty("randomState").GetRawText(), effect.GetProperty("beforeRandomState").GetRawText()); Assert.Equal(effect.GetProperty("beforeRandomState").GetRawText(), effect.GetProperty("afterRandomState").GetRawText()); Assert.Equal("spi-1979-land-rules:21.24-21.26", effect.GetProperty("sources")[0].GetString());
            }
        }
        Assert.Equal(Bytes(trace.GetProperty("sourceCanonicalUtf8")), source); Assert.Equal(Bytes(trace.GetProperty("proofCanonicalUtf8")), result.ProofBytes); Assert.True(result.Control.GetProperty("closed").GetBoolean());
        Assert.Equal(2, result.Control.GetProperty("movementEnd").GetProperty("ordinal").GetInt32()); Assert.Equal(result.ReceiptId, result.Control.GetProperty("movementEnd").GetProperty("completionReceiptId").GetString());
        Assert.Equal("land.position.operation-1.first-player.movement-and-combat.breakdown-determination", result.Control.GetProperty("positionId").GetString());
        var moves = trace.GetProperty("moves").GetInt32(); Assert.Equal(moves == 0 ? 38 : 40 + moves, result.Control.GetProperty("stateVersion").GetInt64()); Assert.Equal(moves == 0, result.Control.GetProperty("world").GetProperty("relationships")[0].GetProperty("active").GetBoolean());
        var end = result.Control.GetProperty("movementEnd"); Assert.Equal(moves is 2 or 4 ? 1 : 0, end.GetProperty("excludedBefore").GetArrayLength());
        var world = result.Control.GetProperty("world");
        foreach (var location in end.GetProperty("endLocations").EnumerateArray()) Assert.Equal(world.GetProperty("elements").EnumerateArray().Single(e => e.GetProperty("elementId").GetString() == location.GetProperty("unit").GetProperty("elementId").GetString()).GetProperty("currentLocationId").GetString(), location.GetProperty("locationId").GetString());
        using var last = JsonDocument.Parse(result.EventBytes!); Assert.Equal(prior.GetProperty("nextCycleProgress").GetRawText(), last.RootElement.GetProperty("effect").GetProperty("progress").GetRawText());
    }
    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(15)]
    public void ZeroEveryCutAndAllPriorCommandsRecoverOwnedReadback(int index) => Recover(index);
    [Theory]
    [InlineData(1)]
    [InlineData(6)]
    [InlineData(11)]
    [InlineData(16)]
    public void OneMoveEveryCutAndAllPriorCommandsRecoverOwnedReadback(int index) => Recover(index);
    [Theory]
    [InlineData(2)]
    [InlineData(7)]
    [InlineData(12)]
    [InlineData(17)]
    public void TwoMovesEveryCutAndAllPriorCommandsRecoverOwnedReadback(int index) => Recover(index);
    [Theory]
    [InlineData(3)]
    [InlineData(8)]
    [InlineData(13)]
    [InlineData(18)]
    public void ThreeMovesEveryCutAndAllPriorCommandsRecoverOwnedReadback(int index) => Recover(index);
    [Theory]
    [InlineData(4)]
    [InlineData(9)]
    [InlineData(14)]
    [InlineData(19)]
    public void FourMovesEveryCutAndAllPriorCommandsRecoverOwnedReadback(int index) => Recover(index);
    private static void Recover(int index)
    {
        var trace = Trace(index); var a = Arguments(trace); var all = Ledger(trace, "lifecycleLedger"); var finalProof = Packet(Bytes(trace.GetProperty("proofCanonicalUtf8")));
        for (var cut = 0; cut <= all.Length; cut++)
        {
            var source = Cut(trace, cut); var ledger = all.Take(cut).ToArray(); var result = Replay(source, a, ledger); Assert.Equal(Bytes(trace.GetProperty("controlCanonicalUtf8")[cut]), result.ControlBytes);
            var expected = finalProof.DeepClone(); var hash = CampaignCombatActualPaidAssaultSupport.Domain("sandtable.combat.actual-repeated-movement-completion.source.v1", source); expected["sourceId"] = "armcomp." + hash; expected["sourceHash"] = "sha256:" + hash; expected["control"] = Packet(Bytes(trace.GetProperty("controlCanonicalUtf8")[cut])); Assert.Equal(Encode(expected), result.ProofBytes);
            Assert.Equal(source, CampaignCombatActualRepeatedMovementCompletion.ReadSource(source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, ledger, Dependency));
            Assert.Equal(result.ControlBytes, CampaignCombatActualRepeatedMovementCompletion.ReadControl(result.ControlBytes, source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, ledger, Dependency).ControlBytes);
            Assert.Equal(result.ProofBytes, CampaignCombatActualRepeatedMovementCompletion.ReadProof(result.ProofBytes, source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, ledger, Dependency).ProofBytes);
            var exposed = result.SourceBytes; exposed[0] ^= 1; exposed = result.ControlBytes; exposed[0] ^= 1; exposed = result.ProofBytes; exposed[0] ^= 1; Assert.Equal(source, result.SourceBytes); Assert.Equal(Encode(expected), result.ProofBytes);
            for (var earlier = 0; earlier < cut; earlier++)
            {
                var retry = Apply(source, a, ledger, all[earlier]); Assert.True(retry.Duplicate); Assert.Null(retry.EventBytes); Assert.Equal(source, retry.SourceBytes); Assert.Equal(result.ControlBytes, retry.ControlBytes); Assert.Equal(result.ProofBytes, retry.ProofBytes); Assert.Equal(cut, retry.LifecycleInputs.Count);
                using var ev = JsonDocument.Parse(Packet(source)["lifecycleEventCanonicalUtf8"]![earlier]!.GetValue<string>()); Assert.Equal(ev.RootElement.GetProperty("receiptId").GetString(), retry.ReceiptId);
                for (var j = 0; j < cut; j++) Assert.Equal(ledger[j], retry.LifecycleInputs[j]); var altered = retry.LifecycleInputs[0]; altered[0] ^= 1; Assert.Equal(ledger[0], retry.LifecycleInputs[0]);
            }
        }
    }
    private static void Preserve(JsonElement prior, JsonElement state, int receipts)
    {
        foreach (var field in new[] { "world", "members", "randomState", "originalCycle", "activeCycle", "closure", "attackHistory", "targetUses", "nextCycleProgress", "tracks" }) Assert.Equal(prior.GetProperty(field).GetRawText(), state.GetProperty(field).GetRawText());
        Assert.Equal(prior.GetProperty("movementEnd").GetRawText(), state.GetProperty("priorMovementEnd").GetRawText()); Assert.Equal(prior.GetProperty("receipts").GetRawText(), state.GetProperty("movementReceipts").GetRawText()); Assert.Equal(receipts, state.GetProperty("receipts").GetArrayLength());
    }
    internal static byte[] Cut(JsonElement trace, int count) { var p = Packet(Bytes(trace.GetProperty("sourceCanonicalUtf8"))); var events = p["lifecycleEventCanonicalUtf8"]!.AsArray(); while (events.Count > count) events.RemoveAt(events.Count - 1); return Encode(p); }
    private static readonly Lazy<JsonElement[]> Traces = new(() =>
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-actual-repeated-movement-completion-v1.json")));
        return fixture.RootElement.GetProperty("traces").EnumerateArray().Select(x => x.Clone()).ToArray();
    });
    internal sealed record Inputs(byte[][] Selection, byte[][] Rounds, byte[][] Paid, byte[][] Continuation, byte[][] Movement);
    internal static JsonElement Trace(int index) => Traces.Value[index];
    internal static Inputs Arguments(JsonElement trace)
    {
        var parent = Enumerable.Range(0, 4).Select(CombatActualRepeatedMovementTests.Trace).Single(x => x.GetProperty("owner").GetString() == trace.GetProperty("owner").GetString() && x.GetProperty("firstSeal").GetString() == trace.GetProperty("firstSeal").GetString());
        var args = CombatActualRepeatedMovementTests.Arguments(parent); return new(args.Selection, args.Rounds, args.Paid, args.Continuation, Ledger(parent, "movementLedger").Take(trace.GetProperty("moves").GetInt32()).ToArray());
    }
    internal static byte[] Bytes(JsonElement value) => Encoding.ASCII.GetBytes(value.GetString()!);
    internal static JsonObject Packet(byte[] bytes) => JsonNode.Parse(bytes)!.AsObject();
    internal static byte[] Encode(JsonNode value) => CampaignCombatActualRepeatedMovementCompletionCodec.Encode(value);
    internal static byte[] Parent(byte[] source) => Encoding.ASCII.GetBytes(Packet(source)["movementSourceCanonicalUtf8"]!.GetValue<string>());
    internal static byte[] EmptySource(JsonElement trace) { var p = Packet(Bytes(trace.GetProperty("sourceCanonicalUtf8"))); p["lifecycleEventCanonicalUtf8"] = new JsonArray(); return Encode(p); }
    internal static CombatActualRepeatedMovementCompletionResult Apply(byte[] source, Inputs a, IReadOnlyList<byte[]> lifecycle, byte[] input)
        => CampaignCombatActualRepeatedMovementCompletion.ApplyTrustedSource(source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, lifecycle, input, Dependency);
    internal static CombatActualRepeatedMovementCompletionResult Replay(byte[] source, Inputs a, IReadOnlyList<byte[]> lifecycle)
        => CampaignCombatActualRepeatedMovementCompletion.ReplayTrustedSource(source, Context(), a.Selection, a.Rounds, a.Paid, a.Continuation, a.Movement, lifecycle, Dependency);
    internal static void Reject(int code, Action action) => Assert.StartsWith($"CMB-RMC-{code:000}", Assert.Throws<JsonException>(action).Message);
}
