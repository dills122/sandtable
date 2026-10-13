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
    internal static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-actual-post-assault-continuation-v1.json")));
    internal static JsonElement PaidRow(JsonDocument paidFixture, JsonElement trace) => paidFixture.RootElement.GetProperty("cases").EnumerateArray().Single(row => row.GetProperty("owner").GetString() == trace.GetProperty("owner").GetString() && row.GetProperty("firstSeal").GetString() == trace.GetProperty("firstSeal").GetString());
}
