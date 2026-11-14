using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Exercises;

internal static class CampaignScheduledWorld6EvidenceCodec
{
    public static byte[] PreambleCommand(CampaignScheduledWorld6PreambleEvent value) => Write(writer =>
    {
        writer.WriteString("format", "sandtable.scheduled-world6-preamble-command.v1");
        writer.WriteString("campaignId", value.CampaignId);
        writer.WriteString("creationHash", value.CreationHash);
        writer.WriteNumber("expectedVersion", value.PreviousVersion);
        writer.WriteNumber("ordinal", value.Ordinal);
    });

    public static byte[] ArrivalCommand(CampaignScheduledWorld6Command value) => Write(writer =>
    {
        writer.WriteString("format", "sandtable.scheduled-world6-arrival-command.v1");
        writer.WriteString("campaignId", value.CampaignId);
        writer.WriteString("creationHash", value.CreationHash);
        writer.WriteString("commandId", value.CommandId);
        writer.WriteNumber("expectedVersion", value.ExpectedVersion);
        writer.WriteString("actor", value.Actor == LandSide.Axis ? "axis" : "commonwealth");
        writer.WriteString("destinationId", value.DestinationId);
    });

    public static byte[] Receipt(string campaignId, long version, byte[] eventBytes) => Write(writer =>
    {
        writer.WriteString("format", "sandtable.scheduled-world6-receipt.v1");
        writer.WriteString("campaignId", campaignId);
        writer.WriteNumber("resultVersion", version);
        writer.WriteString("eventHash", "sha256:" + Convert.ToHexStringLower(SHA256.HashData(eventBytes)));
    });

    private static byte[] Write(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            write(writer);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
