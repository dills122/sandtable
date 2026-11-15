using System.Text.Json;

namespace Cna.Core.Campaigns;

// Internal replay-derived prefix evidence; not a normal campaign checkpoint.
internal static class CampaignScheduledWorld6PreambleCheckpointCodec
{
    public static byte[] Encode(CampaignCreationRequest request, CampaignScheduledWorld6Created creation,
        IEnumerable<CampaignScheduledWorld6PreambleEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var copy = events.ToArray();
        var verified = CampaignScheduledWorld6PreambleEvents.Verify(request, creation, copy);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("format", "sandtable.scheduled-world6-preamble-checkpoint.v1");
            writer.WriteString("campaignId", request.CampaignId);
            writer.WriteString("setupHash", CampaignScheduledWorld6Phase.SetupHash);
            writer.WriteString("creationHash", CampaignScheduledWorld6CreatedCodec.CalculateHash(creation));
            writer.WriteNumber("stateVersion", verified.Checkpoint.StateVersion);
            writer.WriteString("positionId", verified.Checkpoint.Position.PositionId);
            writer.WriteStartArray("events");
            foreach (var value in copy)
                writer.WriteBase64StringValue(CampaignScheduledWorld6PreambleEventCodec.Encode(value));
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static CampaignScheduledWorld6PreambleVerification Decode(ReadOnlySpan<byte> bytes,
        CampaignCreationRequest expectedRequest, CampaignScheduledWorld6Created expectedCreation)
    {
        if (bytes.Length is 0 or > 1048576) throw new JsonException("Invalid World6 preamble checkpoint size.");
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 64 });
            var root = document.RootElement;
            if (root.GetProperty("format").GetString() != "sandtable.scheduled-world6-preamble-checkpoint.v1")
                throw new JsonException("Unsupported World6 preamble checkpoint.");
            var encodedEvents = root.GetProperty("events").EnumerateArray().ToArray();
            if (encodedEvents.Length > 6) throw new JsonException("Too many World6 preamble events.");
            var events = encodedEvents.Select(value => CampaignScheduledWorld6PreambleEventCodec.Decode(value.GetBytesFromBase64())).ToArray();
            var verified = CampaignScheduledWorld6PreambleEvents.Verify(expectedRequest, expectedCreation, events);
            if (!bytes.SequenceEqual(Encode(expectedRequest, expectedCreation, events)))
                throw new JsonException("World6 preamble checkpoint is not canonical or does not match replay.");
            return verified;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or OverflowException)
        {
            throw new JsonException("Invalid World6 preamble checkpoint.", exception);
        }
    }
}
