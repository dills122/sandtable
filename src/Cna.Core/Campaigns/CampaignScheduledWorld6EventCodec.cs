using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Internal successor-cut bytes; not yet a Chronicle or durable Host event.
internal static class CampaignScheduledWorld6EventCodec
{
    private sealed record WireCommand(int Version, string CampaignId, string SetupHash, string CreationHash, string CommandId,
        long ExpectedVersion, LandSide Actor, string DestinationId);
    private sealed record WireEvent(string Format, int Version, string SetupHash, WireCommand Command,
        long ResultVersion, string ElementId, string RepresentationId, string SuccessorPositionId);

    public static byte[] Encode(CampaignScheduledWorld6Event value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(value.Command);
        var command = value.Command;
        return JsonSerializer.SerializeToUtf8Bytes(new WireEvent("sandtable.scheduled-world6-event.v1",
            value.ContractVersion, value.SetupHash, new WireCommand(command.ContractVersion,
                command.CampaignId, command.SetupHash, command.CreationHash, command.CommandId, command.ExpectedVersion,
                command.Actor, command.DestinationId), value.ResultVersion, value.ElementId,
            value.RepresentationId, value.SuccessorPositionId));
    }

    public static CampaignScheduledWorld6Event Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1048576) throw new JsonException("Invalid scheduled World6 event size.");
        var wire = JsonSerializer.Deserialize<WireEvent>(bytes);
        if (wire?.Command is null || wire.Format != "sandtable.scheduled-world6-event.v1"
            || wire.SetupHash is null || wire.ElementId is null || wire.RepresentationId is null
            || wire.SuccessorPositionId is null || wire.Command.CampaignId is null
            || wire.Command.SetupHash is null || wire.Command.CreationHash is null || wire.Command.CommandId is null || wire.Command.DestinationId is null)
            throw new JsonException("Invalid scheduled World6 event.");
        var command = wire.Command;
        var value = new CampaignScheduledWorld6Event(wire.Version, wire.SetupHash,
            new CampaignScheduledWorld6Command(command.Version, command.CampaignId, command.SetupHash, command.CreationHash,
                command.CommandId, command.ExpectedVersion, command.Actor, command.DestinationId),
            wire.ResultVersion, wire.ElementId, wire.RepresentationId, wire.SuccessorPositionId);
        if (!bytes.SequenceEqual(Encode(value))) throw new JsonException("Scheduled World6 event is not canonical.");
        return value;
    }
}
