using System.Text.Json;

namespace Cna.Core.Campaigns;

// This checkpoint represents only the isolated synthetic phase, never a campaign save.
internal static class CampaignSyntheticArrivalCheckpointCodec
{
    private sealed record WireCheckpoint(int Version, string CampaignId, string SetupHash, long StateVersion, string PositionId, byte[]? Event);

    public static byte[] Encode(CampaignSyntheticArrivalPhase state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var evidence = state.Receipt is null ? null : CampaignSyntheticArrivalEventCodec.Encode(state.Receipt);
        return JsonSerializer.SerializeToUtf8Bytes(new WireCheckpoint(1, state.CampaignId, CampaignSyntheticArrivalPhase.SetupHash,
            state.Version, state.Position.PositionId, evidence));
    }

    public static CampaignSyntheticArrivalPhase Decode(ReadOnlySpan<byte> bytes, string expectedCampaignId)
    {
        if (bytes.Length is 0 or > 2097152)
            throw new JsonException("Invalid synthetic arrival checkpoint size.");
        var wire = JsonSerializer.Deserialize<WireCheckpoint>(bytes);
        if (wire is null || wire.Version != 1 || wire.CampaignId != expectedCampaignId || wire.SetupHash != CampaignSyntheticArrivalPhase.SetupHash || wire.PositionId is null)
            throw new JsonException("Invalid synthetic arrival checkpoint.");
        try
        {
            var state = wire.Event is null
                ? CampaignSyntheticArrivalPhase.Replay(expectedCampaignId, [])
                : CampaignSyntheticArrivalPhase.Replay(expectedCampaignId, [CampaignSyntheticArrivalEventCodec.Decode(wire.Event)]);
            if (wire.StateVersion != state.Version || wire.PositionId != state.Position.PositionId
                || !bytes.SequenceEqual(Encode(state)))
                throw new JsonException("Synthetic arrival checkpoint does not match replay.");
            return state;
        }
        catch (InvalidOperationException exception)
        {
            throw new JsonException("Invalid synthetic arrival checkpoint history.", exception);
        }
    }
}
