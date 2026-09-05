using System.Text.Json;
using Cna.Core.Randomness;

namespace Cna.Core.Campaigns;

internal static class CampaignBreakdownStopResolvedCodec
{
    public static byte[] Serialize(BreakdownStopResolved value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return CampaignBreakdownCodec.Bytes(writer =>
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", value.ContractVersion);
            writer.WriteString("eventType", "breakdown-stop-resolved");
            writer.WriteString("campaignId", value.CampaignId);
            writer.WriteNumber("stateVersion", value.StateVersion);
            writer.WriteNumber("priorStateVersion", value.PriorStateVersion);
            writer.WriteString("rulesetHash", value.RulesetHash);
            writer.WriteString("fromPositionId", value.FromPositionId);
            writer.WriteString("actionId", value.ActionId);
            writer.WritePropertyName("stop"); CampaignBreakdownCodec.WriteStop(writer, value.Stop);
            WriteRandom(writer, "randomStateBefore", value.RandomStateBefore);
            writer.WriteStartArray("checks");
            foreach (var check in value.Checks) CampaignBreakdownCheckCodec.Write(writer, check);
            writer.WriteEndArray();
            writer.WriteStartArray("createdLots");
            foreach (var lot in value.CreatedLots) CampaignBreakdownCodec.WriteLot(writer, lot);
            writer.WriteEndArray();
            WriteRandom(writer, "randomStateAfter", value.RandomStateAfter);
            writer.WritePropertyName("breakdownFlowAfter"); CampaignBreakdownCodec.WriteFlow(writer, value.BreakdownFlowAfter);
            CampaignSnapshotSerializer.WriteSources(writer, value.Sources);
            writer.WriteEndObject();
        });
    }
    public static BreakdownStopResolved Deserialize(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray());
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId", "stateVersion",
                "priorStateVersion", "rulesetHash", "fromPositionId", "actionId", "stop", "randomStateBefore", "checks",
                "createdLots", "randomStateAfter", "breakdownFlowAfter", "sources");
            if (root.GetProperty("contractVersion").GetInt32() != 1 || root.GetProperty("eventType").GetString() != "breakdown-stop-resolved")
                throw new JsonException("Unsupported Breakdown resolution event version.");
            var value = new BreakdownStopResolved(root.GetProperty("campaignId").GetString()!,
                root.GetProperty("stateVersion").GetInt64(), root.GetProperty("priorStateVersion").GetInt64(),
                root.GetProperty("rulesetHash").GetString()!, root.GetProperty("fromPositionId").GetString()!,
                root.GetProperty("actionId").GetString()!, CampaignBreakdownCodec.ParseStop(root.GetProperty("stop")),
                CampaignSnapshotSerializer.ParseRandomState(root.GetProperty("randomStateBefore")),
                root.GetProperty("checks").EnumerateArray().Select(CampaignBreakdownCheckCodec.Parse),
                root.GetProperty("createdLots").EnumerateArray().Select(CampaignBreakdownCodec.ParseLot),
                CampaignSnapshotSerializer.ParseRandomState(root.GetProperty("randomStateAfter")),
                CampaignBreakdownCodec.ParseFlow(root.GetProperty("breakdownFlowAfter")),
                CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));
            if (!bytes.SequenceEqual(Serialize(value))) throw new JsonException("Noncanonical Breakdown resolution event.");
            return value;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or ArithmeticException or KeyNotFoundException or FormatException)
        { throw new JsonException("Invalid Breakdown resolution event.", error); }
    }
    private static void WriteRandom(Utf8JsonWriter writer, string name, RandomStreamState state)
    {
        writer.WriteStartObject(name); writer.WriteNumber("contractVersion", state.ContractVersion);
        writer.WriteString("algorithmId", state.AlgorithmId); writer.WriteNumber("seed", state.Seed);
        writer.WriteNumber("nextByteCursor", state.NextByteCursor); writer.WriteEndObject();
    }
}
