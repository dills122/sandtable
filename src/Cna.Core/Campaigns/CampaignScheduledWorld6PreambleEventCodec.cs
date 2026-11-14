using System.Text.Json;

namespace Cna.Core.Campaigns;

// Internal successor-cut bytes; not yet a Chronicle or durable Host event.
internal static class CampaignScheduledWorld6PreambleEventCodec
{
    private sealed record WireEvent(string Format, int Version, string CampaignId, string SetupHash,
        string CreationHash, int Ordinal, long PreviousVersion, long ResultVersion,
        string PreviousPositionId, string SuccessorPositionId, string ReferenceEventHash);

    public static byte[] Encode(CampaignScheduledWorld6PreambleEvent value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.SerializeToUtf8Bytes(new WireEvent("sandtable.scheduled-world6-preamble.v1",
            value.ContractVersion, value.CampaignId, value.SetupHash, value.CreationHash, value.Ordinal,
            value.PreviousVersion, value.ResultVersion, value.PreviousPositionId, value.SuccessorPositionId,
            value.ReferenceEventHash));
    }

    public static CampaignScheduledWorld6PreambleEvent Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 65536) throw new JsonException("Invalid World6 preamble event size.");
        var wire = JsonSerializer.Deserialize<WireEvent>(bytes);
        if (wire is null || wire.Format != "sandtable.scheduled-world6-preamble.v1"
            || wire.CampaignId is null || wire.SetupHash is null || wire.CreationHash is null
            || wire.PreviousPositionId is null || wire.SuccessorPositionId is null || wire.ReferenceEventHash is null)
            throw new JsonException("Invalid World6 preamble event.");
        var value = new CampaignScheduledWorld6PreambleEvent(wire.Version, wire.CampaignId, wire.SetupHash,
            wire.CreationHash, wire.Ordinal, wire.PreviousVersion, wire.ResultVersion,
            wire.PreviousPositionId, wire.SuccessorPositionId, wire.ReferenceEventHash);
        if (!bytes.SequenceEqual(Encode(value))) throw new JsonException("World6 preamble event is not canonical.");
        return value;
    }
}
