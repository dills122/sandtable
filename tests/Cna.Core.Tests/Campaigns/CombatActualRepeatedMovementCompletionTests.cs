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
