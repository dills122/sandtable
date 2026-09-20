using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Creation-only codec; it is independent of fresh-admission policy and public dispatch.</summary>
internal static class CampaignCreationSnapshotV12Codec
{
    public static byte[] Serialize(CampaignCreationSnapshotV12 snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", snapshot.ContractVersion);
            writer.WriteString("campaignId", snapshot.CampaignId);
            writer.WriteNumber("stateVersion", snapshot.StateVersion);
            writer.WriteString("rulesetHash", snapshot.RulesetHash);
            writer.WritePropertyName("setup");
            writer.WriteRawValue(CampaignSetupV7Codec.Serialize(snapshot.Setup));
            writer.WritePropertyName("world");
            writer.WriteRawValue(CampaignWorldV7InitialCodec.Serialize(snapshot.World,
                snapshot.Setup.Artifact, snapshot.Setup.Scenario, snapshot.Setup.CombatInitialization));
            writer.WriteNull("initiativeHolder");
            WriteEmptyArray(writer, "operationStageOrders");
            WriteEmptyArray(writer, "operationStageWeather");
            CampaignSnapshotSerializer.WriteRandomState(writer, snapshot.RandomState);
            writer.WriteStartObject("currentPosition");
            writer.WriteString("kind", "sequence");
            CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", snapshot.SequencePosition);
            writer.WriteEndObject();
            writer.WriteNull("reactionWindow");
            writer.WriteStartObject("breakdownFlow");
            writer.WriteString("kind", "idle");
            writer.WriteEndObject();
            writer.WritePropertyName("configuration");
            writer.WriteRawValue(CombatDecisionConfigurationCodec.Serialize(snapshot.Configuration));
            writer.WriteStartObject("creationReceipt");
            writer.WriteNumber("contractVersion", 1);
            writer.WritePropertyName("creationRequest");
            CampaignCombatCreationRequestCodec.Write(writer, snapshot.CreationReceipt.Request);
            writer.WriteString("creationBinding", snapshot.CreationReceipt.CreationBinding);
            writer.WriteString("creationEventHash", snapshot.CreationReceipt.CreationEventHash);
            writer.WriteEndObject();
            writer.WriteString("chroniclePrefix", snapshot.ChroniclePrefix);
            writer.WriteNull("cycleState");
            writer.WriteNull("combatState");
            WriteEmptyArray(writer, "commandReceipts");
            writer.WriteEndObject();
        }
        var bytes = stream.ToArray();
        if (bytes.Length > 1_048_576) throw new JsonException("Creation Snapshot12 exceeds byte limit.");
        return bytes;
    }

    public static CampaignCreationSnapshotV12 Deserialize(ReadOnlySpan<byte> snapshotBytes,
        ReadOnlySpan<byte> createdBytes, CampaignCombatCreationRequest trustedRequest)
    {
        if (snapshotBytes.Length > 1_048_576) throw new JsonException("Creation Snapshot12 exceeds byte limit.");
        var expected = CampaignCreationSnapshotV12.Create(createdBytes, trustedRequest);
        // Every current field is derived at this cut, including mandatory empty/null slots.
        // This rejects noninitial state rather than projecting it back to a fresh campaign.
        if (!snapshotBytes.SequenceEqual(Serialize(expected)))
            throw new JsonException("Snapshot12 differs from canonical trusted creation evidence.");
        return expected;
    }

    private static void WriteEmptyArray(Utf8JsonWriter writer, string name)
    {
        writer.WriteStartArray(name);
        writer.WriteEndArray();
    }
}
