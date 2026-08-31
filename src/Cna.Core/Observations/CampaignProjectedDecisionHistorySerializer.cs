using System.Text.Json;

namespace Cna.Core.Observations;

internal static class CampaignProjectedDecisionHistorySerializer
{
    public static byte[] SerializeCanonical(CampaignProjectedDecisionHistoryEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", entry.ContractVersion);
            writer.WriteString("campaignId", entry.CampaignId);
            writer.WriteNumber("stateVersion", entry.StateVersion);
            writer.WriteString(
                "observer",
                CampaignObservationSerializer.FormatSide(entry.Observer));
            CampaignObservationV6Serializer.WriteDecisionState(writer, entry.DecisionState);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static CampaignProjectedDecisionHistoryEntry DeserializeCanonical(
        ReadOnlySpan<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(
                canonicalJson.ToArray(),
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 16,
                });
            var root = document.RootElement;
            CampaignObservationSerializer.RequireProperties(
                root,
                "contractVersion",
                "campaignId",
                "stateVersion",
                "observer",
                "decisionState");
            var result = new CampaignProjectedDecisionHistoryEntry(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("stateVersion").GetInt64(),
                CampaignObservationSerializer.ParseSide(
                    root.GetProperty("observer").GetString()),
                CampaignObservationV6Serializer.ParseDecisionState(
                    root.GetProperty("decisionState")));
            if (!canonicalJson.SequenceEqual(SerializeCanonical(result)))
            {
                throw new JsonException(
                    "The projected Campaign decision history is not canonical JSON.");
            }

            return result;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or FormatException
            or KeyNotFoundException
            or OverflowException)
        {
            throw new JsonException(
                "The projected Campaign decision history JSON is invalid.",
                exception);
        }
    }
}
