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
