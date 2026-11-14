using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

// Internal experiment; not a Chronicle event or an authenticated Host record.
internal static class CampaignSyntheticArrivalEventCodec
{
    private sealed record WireCommand(int Version, string CampaignId, string SetupHash, string CommandId, long ExpectedVersion,
        LandSide Actor, string GroupId, string DestinationId);
    private sealed record WireEvent(int Version, string SetupHash, WireCommand Command, long ResultVersion,
        string[] ElementIds, string SuccessorPositionId);

    public static byte[] Encode(CampaignSyntheticArrivalEvent value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(value.Command);
        var command = value.Command;
        return JsonSerializer.SerializeToUtf8Bytes(new WireEvent(value.ContractVersion, value.SetupHash,
            new WireCommand(command.ContractVersion, command.CampaignId, command.SetupHash, command.CommandId, command.ExpectedVersion,
                command.Actor, command.GroupId, command.DestinationId), value.ResultVersion,
            value.ElementIds.ToArray(), value.SuccessorPositionId));
    }

    public static CampaignSyntheticArrivalEvent Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1048576)
            throw new JsonException("Invalid synthetic arrival event size.");
        var wire = JsonSerializer.Deserialize<WireEvent>(bytes);
        if (wire?.Command is null || wire.SetupHash is null || wire.ElementIds is null || wire.SuccessorPositionId is null
            || wire.Command.CampaignId is null || wire.Command.SetupHash is null || wire.Command.CommandId is null
            || wire.Command.GroupId is null || wire.Command.DestinationId is null)
            throw new JsonException("Invalid synthetic arrival event.");
        var command = wire.Command;
        var value = new CampaignSyntheticArrivalEvent(wire.Version, wire.SetupHash,
            new CampaignSyntheticArrivalCommand(command.Version, command.CampaignId, command.SetupHash, command.CommandId,
                command.ExpectedVersion, command.Actor, command.GroupId, command.DestinationId),
            wire.ResultVersion, wire.ElementIds, wire.SuccessorPositionId);
        if (!bytes.SequenceEqual(Encode(value)))
            throw new JsonException("Synthetic arrival event is not canonical.");
        return value;
    }
}
