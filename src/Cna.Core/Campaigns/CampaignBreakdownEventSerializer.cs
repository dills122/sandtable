using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>Strict dormant event dispatch from first-side Movement through its Combat checkpoint.</summary>
internal static class CampaignBreakdownEventSerializer
{
    public static byte[] Serialize(CampaignSuccessorEvent value) => value switch
    {
        ElementMovedV3 or ReactingElementMovedV2 => CampaignBreakdownMoveEventSerializer.Serialize(value),
        BreakdownStopResolved resolved => CampaignBreakdownStopResolvedCodec.Serialize(resolved),
        ElementMovementStopped or ReactionParticipantCompletedV2 or ReactionWindowClosedV2 or MovementSegmentCompletedV2 or BreakdownSegmentCompleted
            => CampaignBreakdownLifecycleCodec.Serialize(value),
        _ => throw new JsonException("Unsupported dormant Breakdown event."),
    };
    public static CampaignSuccessorEvent Deserialize(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray());
            var root = document.RootElement;
            return (root.GetProperty("eventType").GetString(), root.GetProperty("contractVersion").GetInt32()) switch
            {
                ("element-moved", 3) or ("reacting-element-moved", 2) => CampaignBreakdownMoveEventSerializer.Deserialize(bytes),
                ("breakdown-stop-resolved", 1) => CampaignBreakdownStopResolvedCodec.Deserialize(bytes),
                ("element-movement-stopped", 1) or ("reaction-participant-completed", 2) or ("reaction-window-closed", 2)
                    or ("movement-segment-completed", 2) or ("breakdown-segment-completed", 1) => CampaignBreakdownLifecycleCodec.Deserialize(bytes),
                _ => throw new JsonException("Unsupported dormant Breakdown event/version pair."),
            };
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException)
        { throw new JsonException("Invalid dormant Breakdown event.", error); }
    }
}
