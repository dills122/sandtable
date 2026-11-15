using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

// Internal successor-cut evidence; normal campaign creation does not use this codec.
internal static class CampaignScheduledWorld6CreatedCodec
{
    public static string CalculateHash(CampaignScheduledWorld6Created value) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encode(value)));

    public static byte[] Encode(CampaignScheduledWorld6Created value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var request = new CampaignCreationRequest(CampaignCreationRequest.CurrentContractVersion,
            value.CampaignId, value.RulesetHash, value.Seed, value.SetupId, value.SetupHash,
            value.ContentPackId, value.ContentHash, value.ScenarioId);
        _ = CampaignScheduledWorld6Creation.Replay(request, value);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("format", "sandtable.scheduled-world6-created.v1");
            writer.WriteString("campaignId", value.CampaignId);
            writer.WriteNumber("stateVersion", value.StateVersion);
            writer.WriteString("rulesetHash", value.RulesetHash);
            writer.WriteNumber("seed", value.Seed);
            writer.WriteString("setupId", value.SetupId);
            writer.WriteString("setupHash", value.SetupHash);
            writer.WriteString("contentPackId", value.ContentPackId);
            writer.WriteString("contentHash", value.ContentHash);
            writer.WriteString("scenarioId", value.ScenarioId);
            writer.WriteString("positionId", value.Position.PositionId);
            writer.WriteString("randomAlgorithmId", value.RandomState.AlgorithmId);
            writer.WriteNumber("randomCursor", value.RandomState.NextByteCursor);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static CampaignScheduledWorld6Created Decode(ReadOnlySpan<byte> bytes, CampaignCreationRequest expectedRequest)
    {
        if (bytes.Length is 0 or > 65536) throw new JsonException("Invalid scheduled World6 creation size.");
        try
        {
            var expected = CampaignScheduledWorld6Creation.Create(expectedRequest);
            if (!bytes.SequenceEqual(Encode(expected))) throw new JsonException("Invalid scheduled World6 creation evidence.");
            return expected;
        }
        catch (ArgumentException exception)
        {
            throw new JsonException("Invalid scheduled World6 creation request.", exception);
        }
    }
}
