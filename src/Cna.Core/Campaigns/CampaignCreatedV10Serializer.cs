using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal static class CampaignCreatedV10Serializer
{
    public static byte[] Serialize(CampaignCreatedV10 created, ContentPackV6Artifact artifact,
        ContentScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(created);
        var expected = CampaignCreationV10Factory.Create(created.CampaignId, created.RulesetHash,
            created.Setup, artifact, scenario, created.RandomState, created.SequencePosition);
        if (created != expected)
            throw new JsonException("CampaignCreated 10 must exactly equal certified initial truth.");
        return WriteCanonical(created);
    }

    public static CampaignCreatedV10 Deserialize(ReadOnlySpan<byte> bytes, ContentPackV6Artifact artifact,
        ContentScenario scenario)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray());
            var root = document.RootElement;
            CampaignSnapshotSerializer.RequireProperties(root,
                "contractVersion", "eventType", "campaignId", "stateVersion", "rulesetHash", "setup",
                "initialWorld", "randomState", "sequencePosition", "breakdownFlow");
            if (root.GetProperty("contractVersion").GetInt32() != CampaignCreatedV10.CurrentContractVersion
                || root.GetProperty("eventType").GetString() != "campaign-created"
                || root.GetProperty("stateVersion").GetInt64() != 1)
                throw new JsonException("CampaignCreated 10 identity is invalid.");

            var expected = CampaignCreationV10Factory.Create(
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("rulesetHash").GetString()!,
                CampaignSetupV6Codec.ParseSetup(root.GetProperty("setup")), artifact, scenario,
                CampaignSnapshotSerializer.ParseRandomState(root.GetProperty("randomState")),
                CampaignV11CanonicalCodec.ParsePosition(root.GetProperty("sequencePosition")));
            // Initial World and idle flow are derived authority. Comparing complete bytes also
            // rejects all unknown, duplicate, reordered and forged nested evidence.
            if (!bytes.SequenceEqual(WriteCanonical(expected)))
                throw new JsonException("CampaignCreated 10 requires exact canonical certified initial truth.");
            return expected;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException
            or FormatException or InvalidOperationException or KeyNotFoundException)
        {
            throw new JsonException("CampaignCreated 10 JSON is invalid.", exception);
        }
    }

    private static byte[] WriteCanonical(CampaignCreatedV10 created)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", created.ContractVersion);
            writer.WriteString("eventType", "campaign-created");
            writer.WriteString("campaignId", created.CampaignId);
            writer.WriteNumber("stateVersion", created.StateVersion);
            writer.WriteString("rulesetHash", created.RulesetHash);
            CampaignSetupV6Codec.WriteSetup(writer, created.Setup);
            CampaignV11CanonicalCodec.WriteWorld(writer, "initialWorld", created.InitialWorld);
            CampaignSnapshotSerializer.WriteRandomState(writer, created.RandomState);
            CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", created.SequencePosition);
            writer.WritePropertyName("breakdownFlow");
            CampaignBreakdownCodec.WriteFlow(writer, created.BreakdownFlow);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }
}
