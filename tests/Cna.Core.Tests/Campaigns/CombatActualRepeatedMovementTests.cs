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
