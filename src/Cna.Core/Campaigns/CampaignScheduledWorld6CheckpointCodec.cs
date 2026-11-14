using System.Text.Json;

namespace Cna.Core.Campaigns;

// A replay-derived checkpoint for the synthetic cut; not a campaign snapshot.
internal static class CampaignScheduledWorld6CheckpointCodec
{
    private sealed record WireCheckpoint(string Format, string CampaignId, long BaseVersion, string SetupHash, string CreationHash,
        long StateVersion, string PositionId, byte[]? Event);

    public static byte[] Encode(CampaignScheduledWorld6Phase state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var evidence = state.Receipt is null ? null : CampaignScheduledWorld6EventCodec.Encode(state.Receipt);
        return JsonSerializer.SerializeToUtf8Bytes(new WireCheckpoint("sandtable.scheduled-world6-checkpoint.v1",
            state.CampaignId, state.BaseVersion, CampaignScheduledWorld6Phase.SetupHash, state.CreationHash,
            state.Version, state.Position.PositionId, evidence));
    }

    public static CampaignScheduledWorld6Phase Decode(ReadOnlySpan<byte> bytes, CampaignScheduledWorld6PreambleEvents.VerifiedCut proof)
    {
        ArgumentNullException.ThrowIfNull(proof);
        var cut = proof.Cut;
        if (bytes.Length is 0 or > 2097152) throw new JsonException("Invalid scheduled World6 checkpoint size.");
        var wire = JsonSerializer.Deserialize<WireCheckpoint>(bytes);
        if (wire is null || wire.Format != "sandtable.scheduled-world6-checkpoint.v1"
            || wire.CampaignId != cut.Request.CampaignId || wire.BaseVersion != cut.Version
            || wire.SetupHash != CampaignScheduledWorld6Phase.SetupHash || wire.PositionId is null)
            throw new JsonException("Invalid scheduled World6 checkpoint.");
        try
        {
            var phase = wire.Event is null
                ? CampaignScheduledWorld6Phase.Replay(proof, [])
                : CampaignScheduledWorld6Phase.Replay(proof,
                    [CampaignScheduledWorld6EventCodec.Decode(wire.Event)]);
            if (phase.CreationHash != wire.CreationHash || phase.Version != wire.StateVersion || phase.Position.PositionId != wire.PositionId
                || !bytes.SequenceEqual(Encode(phase)))
                throw new JsonException("Scheduled World6 checkpoint does not match replay.");
            return phase;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            throw new JsonException("Invalid scheduled World6 checkpoint history.", exception);
        }
    }
}
