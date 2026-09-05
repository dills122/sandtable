using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal static class CampaignCurrentEventSerializer
{
    public static byte[] Serialize(object campaignEvent) => campaignEvent switch
    {
        CampaignCreatedV10 created => SerializeCreated(created),
        CampaignSuccessorEvent successor => CampaignBreakdownEventSerializer.Serialize(successor),
        CampaignEvent unchanged => CampaignV11PreambleCodec.Serialize(unchanged),
        _ => throw new JsonException("Unsupported current Campaign event."),
    };
    public static object Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        using var document = JsonDocument.Parse(canonicalJson);
        var eventType = document.RootElement.GetProperty("eventType").GetString();
        if (eventType == "campaign-created")
        {
            var setup = CampaignSetupV6Codec.ParseSetup(document.RootElement.GetProperty("setup"));
            var context = CampaignCurrentSnapshotSerializer.ResolveContext(setup);
            var created = CampaignCreatedV10Serializer.Deserialize(canonicalJson.Span, context.ArtifactV6!, context.Scenario);
            _ = SerializeCreated(created);
            return created;
        }
        return eventType switch
        {
            "element-moved" or "reacting-element-moved" or "reaction-participant-completed" or "reaction-window-closed"
                or "movement-segment-completed" or "element-movement-stopped" or "breakdown-stop-resolved"
                or "breakdown-segment-completed" => CampaignBreakdownEventSerializer.Deserialize(canonicalJson.Span),
            _ => CampaignV11PreambleCodec.Deserialize(canonicalJson),
        };
    }
    private static byte[] SerializeCreated(CampaignCreatedV10 created)
    {
        var context = CampaignCurrentSnapshotSerializer.ResolveContext(created.Setup);
        var snapshot = CampaignCreationV10Factory.CreateSnapshot(created, context.ArtifactV6!, context.Scenario);
        if (!CampaignSnapshotV11Admission.IsValid(snapshot, context.ArtifactV6!, context.Scenario))
            throw new JsonException("Creation is not current certified authority.");
        return CampaignCreatedV10Serializer.Serialize(created, context.ArtifactV6!, context.Scenario);
    }
}

internal static class CampaignCurrentSnapshotSerializer
{
    public static byte[] Serialize(CampaignSnapshotV11 snapshot)
    {
        var context = ResolveContext(snapshot.Setup);
        if (!CampaignSnapshotV11Admission.IsValid(snapshot, context.ArtifactV6!, context.Scenario))
            throw new JsonException("invalid-breakdown-authority");
        return CampaignSnapshotV11Serializer.Serialize(snapshot, context.ArtifactV6!, context.Scenario);
    }
    public static CampaignSnapshotV11 Deserialize(ReadOnlyMemory<byte> bytes)
    {
        var snapshot = CampaignSnapshotV11Serializer.Deserialize(bytes);
        _ = Serialize(snapshot);
        return snapshot;
    }
    internal static CampaignContentContext ResolveContext(CampaignSetupSnapshotV6 setup)
    {
        var resolution = Cna1979SyntheticContentCatalog.ResolveV6(setup.Content.Pack.PackId, setup.Content.Pack.Hash);
        if (!resolution.IsResolved || resolution.Artifact is null || resolution.Artifact.Identity != setup.Content.Pack)
            throw new JsonException("Current checkpoint content is not registered.");
        return CampaignContentContext.Create(resolution.Artifact, setup.Content.ScenarioId);
    }
}

internal static class CampaignCurrentProjector
{
    public static CampaignSnapshotV11 Apply(CampaignSnapshotV11? snapshot, object campaignEvent, CampaignContentContext context)
    {
        var artifact = context.ArtifactV6 ?? throw new InvalidCampaignHistoryException("Current replay requires Content 6.");
        if (snapshot is not null && !CampaignSnapshotV11Admission.IsValid(snapshot, artifact, context.Scenario))
            throw new InvalidCampaignHistoryException("invalid-breakdown-authority");
        CampaignSnapshotV11 after = campaignEvent switch
        {
            CampaignCreatedV10 created when snapshot is null => CampaignCreationV10Factory.CreateSnapshot(created, artifact, context.Scenario),
            CampaignSuccessorEvent successor when snapshot is not null => CampaignV11BreakdownProjector.Apply(snapshot, successor, artifact, context.Scenario),
            CampaignEvent preamble when snapshot is not null => CampaignV11Preamble.Apply(snapshot, preamble, artifact, context.Scenario),
            _ => throw new InvalidCampaignHistoryException("Unsupported current event or event order."),
        };
        if (!CampaignSnapshotV11Admission.IsValid(after, artifact, context.Scenario))
            throw new InvalidCampaignHistoryException("invalid-breakdown-authority");
        return after;
    }
    public static CampaignSnapshotV11 Replay(IEnumerable<object> events, CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(events);
        CampaignSnapshotV11? snapshot = null;
        foreach (var value in events) snapshot = Apply(snapshot, value, context);
        return snapshot ?? throw new InvalidCampaignHistoryException("Campaign history must contain CampaignCreated 10.");
    }
}
