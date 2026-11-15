using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Content;

// Candidate bytes only. Decode does not authenticate a journal or admit a campaign.
internal static class CampaignArrivalDraftEventCodec
{
    private sealed record WireEvent(int Version, string IdentityHash, WireCommand Command, long ResultVersion, string[] ElementIds);
    private sealed record WireCommand(int Version, string CommandId, long ExpectedVersion, string GroupId,
        LandSide Actor, int GameTurn, int OperationStage, string DestinationId);

    public static byte[] Encode(CampaignArrivalDraftEvent value)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(value.Command);
        var command = value.Command;
        return JsonSerializer.SerializeToUtf8Bytes(new WireEvent(value.ContractVersion, value.IdentityHash,
            new WireCommand(command.ContractVersion, command.CommandId, command.ExpectedVersion, command.GroupId,
                command.Actor, command.GameTurn, command.OperationStage, command.DestinationId),
            value.ResultVersion, value.ElementIds.ToArray()));
    }

    public static CampaignArrivalDraftEvent Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1048576)
            throw new JsonException("Invalid arrival event size.");
        var wire = JsonSerializer.Deserialize<WireEvent>(bytes);
        if (wire?.Command is null || wire.IdentityHash is null || wire.ElementIds is null
            || wire.Command.CommandId is null || wire.Command.GroupId is null || wire.Command.DestinationId is null)
            throw new JsonException("Invalid arrival event.");
        var command = wire.Command;
        var result = new CampaignArrivalDraftEvent(wire.Version, wire.IdentityHash,
            new CampaignArrivalDraftCommand(command.Version, command.CommandId, command.ExpectedVersion,
                command.GroupId, command.Actor, command.GameTurn, command.OperationStage, command.DestinationId),
            wire.ResultVersion, wire.ElementIds);
        if (!bytes.SequenceEqual(Encode(result)))
            throw new JsonException("Arrival event is not canonical.");
        return result;
    }
}
