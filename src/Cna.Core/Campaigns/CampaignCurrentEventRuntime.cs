using System.Text.Json;

namespace Cna.Core.Campaigns;

internal static class CampaignCurrentEventSerializer
{
    public static byte[] Serialize(object campaignEvent) => campaignEvent switch
    {
        CampaignSuccessorEvent successor => CampaignSuccessorEventSerializer.Serialize(successor),
        CampaignCreated => throw UnsupportedLegacyRoot("CampaignCreated v8"),
        ElementMoved => throw UnsupportedLegacyRoot("ElementMoved v1"),
        CampaignEvent unchanged => CampaignEventSerializer.Serialize(unchanged),
        _ => throw new JsonException("The current Campaign event type is unsupported."),
    };

    public static object Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        using var document = JsonDocument.Parse(canonicalJson);
        var eventType = document.RootElement.GetProperty("eventType").GetString();
        return eventType switch
        {
            "campaign-created" or "element-moved" or "reaction-window-closed"
                or "reacting-element-moved" or "reaction-participant-completed" =>
                CampaignSuccessorEventSerializer.Deserialize(canonicalJson),
            _ => CampaignEventSerializer.Deserialize(canonicalJson),
        };
    }

    private static JsonException UnsupportedLegacyRoot(string identity) => new(
        $"Legacy {identity} is not admitted by current Campaign event authority.");
}

internal static class CampaignCurrentProjector
{
    public static CampaignSnapshotV10 Apply(
        CampaignSnapshotV10? snapshot,
        object campaignEvent,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(campaignEvent);
        ArgumentNullException.ThrowIfNull(context);
        var artifact = context.ArtifactV5
            ?? throw new InvalidCampaignHistoryException(
                "Current Campaign replay requires Content Pack v5.");
        return campaignEvent switch
        {
            CampaignCreatedV9 created when snapshot is null =>
                CampaignV10Projector.ApplyCreation(created, artifact, context.Scenario),
            CampaignCreatedV9 => throw new InvalidCampaignHistoryException(
                "CampaignCreated v9 must be the first event."),
            ElementMovedV2 moved when snapshot is not null =>
                CampaignV10Projector.ApplyMovement(
                    snapshot,
                    moved,
                    artifact,
                    context.Scenario),
            ReactionWindowClosed closed when snapshot is not null =>
                CampaignV10Projector.ApplyReactionClose(
                    snapshot,
                    closed,
                    artifact,
                    context.Scenario),
            ReactingElementMoved moved when snapshot is not null =>
                CampaignV10Projector.ApplyReactionMove(
                    snapshot,
                    moved,
                    artifact,
                    context.Scenario),
            ReactionParticipantCompleted completed when snapshot is not null =>
                CampaignV10Projector.ApplyReactionCompletion(
                    snapshot,
                    completed,
                    artifact,
                    context.Scenario),
            CampaignCreated or ElementMoved => throw new InvalidCampaignHistoryException(
                "Legacy creation and Movement events are not current authority."),
            CampaignEvent unchanged when snapshot is not null => ApplyUnchanged(
                snapshot,
                unchanged,
                context),
            _ => throw new InvalidCampaignHistoryException(
                "The Campaign event is unsupported or out of order."),
        };
    }

    public static CampaignSnapshotV10 Replay(
        IEnumerable<object> events,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(events);
        CampaignSnapshotV10? snapshot = null;
        foreach (var campaignEvent in events)
        {
            snapshot = Apply(snapshot, campaignEvent, context);
        }

        return snapshot ?? throw new InvalidCampaignHistoryException(
            "Campaign history must contain CampaignCreated v9.");
    }

    private static CampaignSnapshotV10 ApplyUnchanged(
        CampaignSnapshotV10 prior,
        CampaignEvent campaignEvent,
        CampaignContentContext context)
    {
        var legacy = CampaignV10LegacyBridge.ToLegacy(prior, context);
        var successor = CampaignProjector.Apply(legacy, campaignEvent, context);
        return CampaignV10LegacyBridge.FromLegacy(prior, successor, context);
    }
}
