using System.Text;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignBreakdownMoveEventSerializer
{
    public static byte[] Serialize(CampaignSuccessorEvent value)
    {
        ArgumentNullException.ThrowIfNull(value);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            switch (value)
            {
                case ElementMovedV3 moved: WriteMoved(writer, moved); break;
                case ReactingElementMovedV2 moved: WriteReactingElementMovedV2(writer, moved); break;
                default: throw new JsonException("Unsupported dormant Breakdown move event.");
            }
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static CampaignSuccessorEvent Deserialize(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray());
            var root = document.RootElement;
            CampaignSuccessorEvent result = (root.GetProperty("eventType").GetString(), root.GetProperty("contractVersion").GetInt32()) switch
            {
                ("element-moved", 3) => ParseMoved(root),
                ("reacting-element-moved", 2) => ParseReactingElementMovedV2(root),
                _ => throw new JsonException("Unsupported event type/version pair."),
            };
            if (!bytes.SequenceEqual(Serialize(result))) throw new JsonException("Noncanonical Breakdown move event.");
            return result;
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or ArithmeticException or KeyNotFoundException)
        { throw new JsonException("Invalid Breakdown move event.", exception); }
    }
    private static void WriteMoved(Utf8JsonWriter writer, ElementMovedV3 moved)
    {
        moved.ValidateContract();
        writer.WriteNumber("contractVersion", moved.ContractVersion);
        writer.WriteString("eventType", "element-moved");
        writer.WriteString("campaignId", moved.CampaignId);
        writer.WriteNumber("stateVersion", moved.StateVersion);
        writer.WriteNumber("priorStateVersion", moved.PriorStateVersion);
        writer.WriteString("fromPositionId", moved.FromPositionId);
        writer.WriteNumber("gameTurn", moved.GameTurn);
        writer.WriteNumber("operationStage", moved.OperationStage);
        writer.WriteString(
            "actingSide",
            CampaignSnapshotSerializer.FormatSide(moved.ActingSide));
        writer.WriteString("elementId", moved.ElementId);
        writer.WriteString("representationId", moved.RepresentationId);
        writer.WriteString("originLocationId", moved.OriginLocationId);
        writer.WriteString("destinationLocationId", moved.DestinationLocationId);
        writer.WriteString("mobilityId", moved.MobilityId);
        writer.WriteStartArray("mobilitySources");
        foreach (var source in moved.MobilitySources)
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", source.SourceId);
            writer.WriteString("locator", source.Locator);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        CampaignEventSerializer.WriteMovementCost(writer, moved.Cost);
        writer.WritePropertyName("capabilityPointsExpendedBefore");
        CapabilityPointAmountCodec.WriteCanonical(
            writer,
            moved.CapabilityPointsExpendedBefore);
        writer.WritePropertyName("capabilityPointsExpendedAfter");
        CapabilityPointAmountCodec.WriteCanonical(
            writer,
            moved.CapabilityPointsExpendedAfter);
        writer.WriteNumber("cohesionBefore", moved.CohesionBefore);
        writer.WriteNumber("cohesionAfter", moved.CohesionAfter);
        CampaignV11CanonicalCodec.WriteMovementEnded(
            writer,
            moved.MovementEndedAfter,
            "movementEndedAfter");
        CampaignV11CanonicalCodec.WritePosition(
            writer,
            "sequencePosition",
            moved.SequencePosition);
        CampaignV11CanonicalCodec.WriteReactionWindow(
            writer,
            "openedReactionWindow",
            moved.OpenedReactionWindow);
        writer.WriteString("rulesetHash", moved.RulesetHash);
        writer.WriteStartArray("breakdownAccounting");
        foreach (var step in moved.BreakdownAccounting) CampaignBreakdownStepCodec.Write(writer, step);
        writer.WriteEndArray();
        writer.WritePropertyName("breakdownFlowAfter");
        CampaignBreakdownCodec.WriteFlow(writer, moved.BreakdownFlowAfter);
    }

    private static ElementMovedV3 ParseMoved(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "priorStateVersion",
            "fromPositionId",
            "gameTurn",
            "operationStage",
            "actingSide",
            "elementId",
            "representationId",
            "originLocationId",
            "destinationLocationId",
            "mobilityId",
            "mobilitySources",
            "cost",
            "capabilityPointsExpendedBefore",
            "capabilityPointsExpendedAfter",
            "cohesionBefore",
            "cohesionAfter",
            "movementEndedAfter",
            "sequencePosition",
            "openedReactionWindow", "rulesetHash", "breakdownAccounting", "breakdownFlowAfter");
        var result = new ElementMovedV3(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("priorStateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            root.GetProperty("gameTurn").GetInt32(),
            root.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParseSide(root.GetProperty("actingSide").GetString()),
            root.GetProperty("elementId").GetString()!,
            root.GetProperty("representationId").GetString()!,
            root.GetProperty("originLocationId").GetString()!,
            root.GetProperty("destinationLocationId").GetString()!,
            root.GetProperty("mobilityId").GetString()!,
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("mobilitySources")),
            CampaignEventSerializer.ParseMovementCost(root.GetProperty("cost")),
            ParseCapabilityPoints(root.GetProperty("capabilityPointsExpendedBefore")),
            ParseCapabilityPoints(root.GetProperty("capabilityPointsExpendedAfter")),
            root.GetProperty("cohesionBefore").GetInt32(),
            root.GetProperty("cohesionAfter").GetInt32(),
            CampaignV11CanonicalCodec.ParseMovementEnded(
                root.GetProperty("movementEndedAfter")),
            CampaignV11CanonicalCodec.ParsePosition(root.GetProperty("sequencePosition")),
            CampaignV11CanonicalCodec.ParseReactionWindow(
                root.GetProperty("openedReactionWindow")),
            root.GetProperty("rulesetHash").GetString()!,
            root.GetProperty("breakdownAccounting").EnumerateArray().Select(CampaignBreakdownStepCodec.Parse),
            CampaignBreakdownCodec.ParseFlow(root.GetProperty("breakdownFlowAfter")));
        if (root.GetProperty("contractVersion").GetInt32() != result.ContractVersion)
        {
            throw new JsonException("The ElementMoved v2 version is invalid.");
        }

        result.ValidateContract();
        return result;
    }

    private static void WriteReactingElementMovedV2(
        Utf8JsonWriter writer,
        ReactingElementMovedV2 moved)
    {
        moved.ValidateContract();
        writer.WriteNumber("contractVersion", moved.ContractVersion);
        writer.WriteString("eventType", "reacting-element-moved");
        writer.WriteString("campaignId", moved.CampaignId);
        writer.WriteNumber("stateVersion", moved.StateVersion);
        writer.WriteNumber("priorStateVersion", moved.PriorStateVersion);
        writer.WriteString("fromPositionId", moved.FromPositionId);
        writer.WriteNumber("gameTurn", moved.GameTurn);
        writer.WriteNumber("operationStage", moved.OperationStage);
        writer.WriteString(
            "actingSide",
            CampaignSnapshotSerializer.FormatSide(moved.ActingSide));
        writer.WriteString("actionId", moved.ActionId);
        writer.WriteString("submittedWindowId", moved.SubmittedWindowId);
        writer.WriteString("submittedOpportunityId", moved.SubmittedOpportunityId);
        writer.WriteString("windowId", moved.WindowId.Value);
        writer.WriteString("opportunityId", moved.OpportunityId.Value);
        writer.WriteString("elementId", moved.ElementId);
        writer.WriteString("representationId", moved.RepresentationId);
        writer.WriteString("originLocationId", moved.OriginLocationId);
        writer.WriteString("destinationLocationId", moved.DestinationLocationId);
        writer.WriteString("mobilityId", moved.MobilityId);
        writer.WriteStartArray("mobilitySources");
        foreach (var source in moved.MobilitySources)
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", source.SourceId);
            writer.WriteString("locator", source.Locator);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        CampaignEventSerializer.WriteMovementCost(writer, moved.Cost);
        writer.WritePropertyName("capabilityPointsExpendedBefore");
        CapabilityPointAmountCodec.WriteCanonical(
            writer,
            moved.CapabilityPointsExpendedBefore);
        writer.WritePropertyName("capabilityPointsExpendedAfter");
        CapabilityPointAmountCodec.WriteCanonical(
            writer,
            moved.CapabilityPointsExpendedAfter);
        writer.WriteNumber("cohesionBefore", moved.CohesionBefore);
        writer.WriteNumber("cohesionAfter", moved.CohesionAfter);
        CampaignV11CanonicalCodec.WriteReactionWindow(
            writer,
            "reactionWindowAfter",
            moved.ReactionWindowAfter);
        writer.WriteString("rulesetHash", moved.RulesetHash);
        writer.WriteStartArray("breakdownAccounting");
        foreach (var step in moved.BreakdownAccounting) CampaignBreakdownStepCodec.Write(writer, step);
        writer.WriteEndArray();
        writer.WritePropertyName("breakdownFlowAfter");
        CampaignBreakdownCodec.WriteFlow(writer, moved.BreakdownFlowAfter);
    }

    private static ReactingElementMovedV2 ParseReactingElementMovedV2(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "priorStateVersion",
            "fromPositionId",
            "gameTurn",
            "operationStage",
            "actingSide",
            "actionId",
            "submittedWindowId",
            "submittedOpportunityId",
            "windowId",
            "opportunityId",
            "elementId",
            "representationId",
            "originLocationId",
            "destinationLocationId",
            "mobilityId",
            "mobilitySources",
            "cost",
            "capabilityPointsExpendedBefore",
            "capabilityPointsExpendedAfter",
            "cohesionBefore",
            "cohesionAfter",
            "reactionWindowAfter", "rulesetHash", "breakdownAccounting", "breakdownFlowAfter");
        var result = new ReactingElementMovedV2(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("priorStateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            root.GetProperty("gameTurn").GetInt32(),
            root.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParseSide(root.GetProperty("actingSide").GetString()),
            root.GetProperty("actionId").GetString()!,
            root.GetProperty("submittedWindowId").GetString()!,
            root.GetProperty("submittedOpportunityId").GetString()!,
            new CampaignReactionWindowId(root.GetProperty("windowId").GetString()!),
            new CampaignReactionOpportunityId(root.GetProperty("opportunityId").GetString()!),
            root.GetProperty("elementId").GetString()!,
            root.GetProperty("representationId").GetString()!,
            root.GetProperty("originLocationId").GetString()!,
            root.GetProperty("destinationLocationId").GetString()!,
            root.GetProperty("mobilityId").GetString()!,
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("mobilitySources")),
            CampaignEventSerializer.ParseMovementCost(root.GetProperty("cost")),
            ParseCapabilityPoints(root.GetProperty("capabilityPointsExpendedBefore")),
            ParseCapabilityPoints(root.GetProperty("capabilityPointsExpendedAfter")),
            root.GetProperty("cohesionBefore").GetInt32(),
            root.GetProperty("cohesionAfter").GetInt32(),
            CampaignV11CanonicalCodec.ParseReactionWindow(root.GetProperty("reactionWindowAfter"))
                ?? throw new JsonException("The resulting Reaction window is required."),
            root.GetProperty("rulesetHash").GetString()!,
            root.GetProperty("breakdownAccounting").EnumerateArray().Select(CampaignBreakdownStepCodec.Parse),
            CampaignBreakdownCodec.ParseFlow(root.GetProperty("breakdownFlowAfter")));
        if (root.GetProperty("contractVersion").GetInt32() != result.ContractVersion)
        {
            throw new JsonException("The ReactingElementMovedV2 version is invalid.");
        }

        result.ValidateContract();
        return result;
    }
    private static CapabilityPointAmount ParseCapabilityPoints(JsonElement value) =>
        CapabilityPointAmountCodec.Deserialize(Encoding.UTF8.GetBytes(value.GetRawText()));
}
