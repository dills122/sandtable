using System.Text;
using System.Text.Json;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignSuccessorEventSerializer
{
    public static byte[] Serialize(CampaignSuccessorEvent campaignEvent)
    {
        ArgumentNullException.ThrowIfNull(campaignEvent);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            switch (campaignEvent)
            {
                case CampaignCreatedV9 created:
                    WriteCreated(writer, created);
                    break;
                case ElementMovedV2 moved:
                    WriteMoved(writer, moved);
                    break;
                case ReactionWindowClosed closed:
                    WriteReactionWindowClosed(writer, closed);
                    break;
                case ReactingElementMoved reactingMoved:
                    WriteReactingElementMoved(writer, reactingMoved);
                    break;
                case ReactionParticipantCompleted participantCompleted:
                    WriteReactionParticipantCompleted(writer, participantCompleted);
                    break;
                default:
                    throw new JsonException("The Campaign successor event type is unsupported.");
            }

            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static CampaignSuccessorEvent Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            CampaignSuccessorEvent result = root.GetProperty("eventType").GetString() switch
            {
                "campaign-created" => ParseCreated(root),
                "element-moved" => ParseMoved(root),
                "reaction-window-closed" => ParseReactionWindowClosed(root),
                "reacting-element-moved" => ParseReactingElementMoved(root),
                "reaction-participant-completed" => ParseReactionParticipantCompleted(root),
                var value => throw new JsonException(
                    $"Unknown Campaign successor event type '{value}'."),
            };
            if (!canonicalJson.Span.SequenceEqual(Serialize(result)))
            {
                throw new JsonException("The Campaign successor event is not canonical JSON.");
            }

            return result;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or FormatException
            or InvalidOperationException
            or KeyNotFoundException)
        {
            throw new JsonException("The Campaign successor event JSON is invalid.", exception);
        }
    }

    private static void WriteCreated(Utf8JsonWriter writer, CampaignCreatedV9 created)
    {
        ValidateCreated(created);
        writer.WriteNumber("contractVersion", created.ContractVersion);
        writer.WriteString("eventType", "campaign-created");
        writer.WriteString("campaignId", created.CampaignId);
        writer.WriteNumber("stateVersion", created.StateVersion);
        writer.WriteString("rulesetHash", created.RulesetHash);
        CampaignV10CanonicalCodec.WriteSetup(writer, created.Setup);
        CampaignV10CanonicalCodec.WriteWorld(writer, "initialWorld", created.InitialWorld);
        CampaignSnapshotSerializer.WriteRandomState(writer, created.RandomState);
        CampaignV10CanonicalCodec.WritePosition(
            writer,
            "sequencePosition",
            created.SequencePosition);
    }

    private static CampaignCreatedV9 ParseCreated(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "rulesetHash",
            "setup",
            "initialWorld",
            "randomState",
            "sequencePosition");
        var result = new CampaignCreatedV9(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("rulesetHash").GetString()!,
            CampaignV10CanonicalCodec.ParseSetup(root.GetProperty("setup")),
            CampaignV10CanonicalCodec.ParseWorld(root.GetProperty("initialWorld")),
            CampaignSnapshotSerializer.ParseRandomState(root.GetProperty("randomState")),
            CampaignV10CanonicalCodec.ParsePosition(root.GetProperty("sequencePosition")));
        if (root.GetProperty("contractVersion").GetInt32() != result.ContractVersion)
        {
            throw new JsonException("The CampaignCreated v9 version is invalid.");
        }

        ValidateCreated(result);
        return result;
    }

    private static void WriteMoved(Utf8JsonWriter writer, ElementMovedV2 moved)
    {
        ValidateMoved(moved);
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
        CampaignV10CanonicalCodec.WriteMovementEnded(
            writer,
            moved.MovementEndedAfter,
            "movementEndedAfter");
        CampaignV10CanonicalCodec.WritePosition(
            writer,
            "sequencePosition",
            moved.SequencePosition);
        CampaignV10CanonicalCodec.WriteReactionWindow(
            writer,
            "openedReactionWindow",
            moved.OpenedReactionWindow);
    }

    private static ElementMovedV2 ParseMoved(JsonElement root)
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
            "openedReactionWindow");
        var result = new ElementMovedV2(
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
            CampaignV10CanonicalCodec.ParseMovementEnded(
                root.GetProperty("movementEndedAfter")),
            CampaignV10CanonicalCodec.ParsePosition(root.GetProperty("sequencePosition")),
            CampaignV10CanonicalCodec.ParseReactionWindow(
                root.GetProperty("openedReactionWindow")));
        if (root.GetProperty("contractVersion").GetInt32() != result.ContractVersion)
        {
            throw new JsonException("The ElementMoved v2 version is invalid.");
        }

        ValidateMoved(result);
        return result;
    }

    private static void ValidateCreated(CampaignCreatedV9 created)
    {
        var expectedPosition = Cna1979LandSequence.CreateTurn(
            created.Setup.InitialGameTurn)[0];
        if (created.ContractVersion != CampaignCreatedV9.CurrentContractVersion
            || created.StateVersion != 1
            || !Cna1979Ruleset.IsCanonicalHash(created.RulesetHash)
            || created.Setup.Content.Pack.SchemaVersion != 5
            || !string.Equals(created.Setup.Content.Pack.FormatId,
                "sandtable.content-json.v4", StringComparison.Ordinal)
            || created.InitialWorld.ContractVersion
                != CampaignWorldSnapshotV5.CurrentContractVersion
            || created.RandomState.ContractVersion != SandtableRandom.ContractVersion
            || !string.Equals(created.RandomState.AlgorithmId,
                SandtableRandom.AlgorithmId, StringComparison.Ordinal)
            || created.RandomState.NextByteCursor != 0
            || created.SequencePosition != expectedPosition)
        {
            throw new JsonException("The CampaignCreated v9 contract is invalid.");
        }
    }

    private static void WriteReactionWindowClosed(
        Utf8JsonWriter writer,
        ReactionWindowClosed closed)
    {
        ValidateReactionWindowClosed(closed);
        writer.WriteNumber("contractVersion", closed.ContractVersion);
        writer.WriteString("eventType", "reaction-window-closed");
        writer.WriteString("campaignId", closed.CampaignId);
        writer.WriteNumber("stateVersion", closed.StateVersion);
        writer.WriteNumber("priorStateVersion", closed.PriorStateVersion);
        writer.WriteString("fromPositionId", closed.FromPositionId);
        if (closed.ActingSide is null)
        {
            writer.WriteNull("actingSide");
        }
        else
        {
            writer.WriteString(
                "actingSide",
                CampaignSnapshotSerializer.FormatSide(closed.ActingSide.Value));
        }

        writer.WriteString("actionId", closed.ActionId);
        writer.WriteString("submittedWindowId", closed.SubmittedWindowId);
        writer.WriteString("windowId", closed.WindowId.Value);
        writer.WriteString("reason", FormatCloseReason(closed.Reason));
        writer.WriteStartArray("closedOpportunityIds");
        foreach (var opportunityId in closed.ClosedOpportunityIds)
        {
            writer.WriteStringValue(opportunityId.Value);
        }

        writer.WriteEndArray();
        CampaignV10CanonicalCodec.WritePosition(
            writer,
            "resumedSequencePosition",
            closed.ResumedSequencePosition);
    }

    private static ReactionWindowClosed ParseReactionWindowClosed(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "priorStateVersion",
            "fromPositionId",
            "actingSide",
            "actionId",
            "submittedWindowId",
            "windowId",
            "reason",
            "closedOpportunityIds",
            "resumedSequencePosition");
        var actingSide = root.GetProperty("actingSide") is { ValueKind: not JsonValueKind.Null } side
            ? CampaignSnapshotSerializer.ParseSide(side.GetString())
            : (LandSide?)null;
        var result = new ReactionWindowClosed(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("priorStateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            actingSide,
            root.GetProperty("actionId").GetString()!,
            root.GetProperty("submittedWindowId").GetString()!,
            new CampaignReactionWindowId(root.GetProperty("windowId").GetString()!),
            ParseCloseReason(root.GetProperty("reason").GetString()),
            root.GetProperty("closedOpportunityIds").EnumerateArray()
                .Select(value => new CampaignReactionOpportunityId(value.GetString()!)),
            CampaignV10CanonicalCodec.ParsePosition(
                root.GetProperty("resumedSequencePosition")));
        if (root.GetProperty("contractVersion").GetInt32() != result.ContractVersion)
        {
            throw new JsonException("The ReactionWindowClosed version is invalid.");
        }

        ValidateReactionWindowClosed(result);
        return result;
    }

    private static void WriteReactingElementMoved(
        Utf8JsonWriter writer,
        ReactingElementMoved moved)
    {
        ValidateReactingElementMoved(moved);
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
        CampaignV10CanonicalCodec.WriteReactionWindow(
            writer,
            "reactionWindowAfter",
            moved.ReactionWindowAfter);
    }

    private static ReactingElementMoved ParseReactingElementMoved(JsonElement root)
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
            "reactionWindowAfter");
        var result = new ReactingElementMoved(
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
            CampaignV10CanonicalCodec.ParseReactionWindow(root.GetProperty("reactionWindowAfter"))
                ?? throw new JsonException("The resulting Reaction window is required."));
        if (root.GetProperty("contractVersion").GetInt32() != result.ContractVersion)
        {
            throw new JsonException("The ReactingElementMoved version is invalid.");
        }

        ValidateReactingElementMoved(result);
        return result;
    }

    private static void WriteReactionParticipantCompleted(
        Utf8JsonWriter writer,
        ReactionParticipantCompleted completed)
    {
        ValidateReactionParticipantCompleted(completed);
        writer.WriteNumber("contractVersion", completed.ContractVersion);
        writer.WriteString("eventType", "reaction-participant-completed");
        writer.WriteString("campaignId", completed.CampaignId);
        writer.WriteNumber("stateVersion", completed.StateVersion);
        writer.WriteNumber("priorStateVersion", completed.PriorStateVersion);
        writer.WriteString("fromPositionId", completed.FromPositionId);
        writer.WriteString(
            "actingSide",
            CampaignSnapshotSerializer.FormatSide(completed.ActingSide));
        writer.WriteString("actionId", completed.ActionId);
        writer.WriteString("submittedWindowId", completed.SubmittedWindowId);
        writer.WriteString("submittedOpportunityId", completed.SubmittedOpportunityId);
        writer.WriteString("windowId", completed.WindowId.Value);
        writer.WriteString("opportunityId", completed.OpportunityId.Value);
        CampaignV10CanonicalCodec.WriteReactionWindow(
            writer,
            "reactionWindowAfter",
            completed.ReactionWindowAfter);
    }

    private static ReactionParticipantCompleted ParseReactionParticipantCompleted(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "priorStateVersion",
            "fromPositionId",
            "actingSide",
            "actionId",
            "submittedWindowId",
            "submittedOpportunityId",
            "windowId",
            "opportunityId",
            "reactionWindowAfter");
        var result = new ReactionParticipantCompleted(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("priorStateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            CampaignSnapshotSerializer.ParseSide(root.GetProperty("actingSide").GetString()),
            root.GetProperty("actionId").GetString()!,
            root.GetProperty("submittedWindowId").GetString()!,
            root.GetProperty("submittedOpportunityId").GetString()!,
            new CampaignReactionWindowId(root.GetProperty("windowId").GetString()!),
            new CampaignReactionOpportunityId(root.GetProperty("opportunityId").GetString()!),
            CampaignV10CanonicalCodec.ParseReactionWindow(root.GetProperty("reactionWindowAfter"))
                ?? throw new JsonException("The resulting Reaction window is required."));
        if (root.GetProperty("contractVersion").GetInt32() != result.ContractVersion)
        {
            throw new JsonException("The ReactionParticipantCompleted version is invalid.");
        }

        ValidateReactionParticipantCompleted(result);
        return result;
    }

    private static string FormatCloseReason(
        CampaignReactionWindowCloseReason reason) => reason switch
        {
            CampaignReactionWindowCloseReason.PlayerDecline => "player-decline",
            CampaignReactionWindowCloseReason.ScriptedUnavailable => "scripted-unavailable",
            CampaignReactionWindowCloseReason.Timeout => "timeout",
            CampaignReactionWindowCloseReason.NoEligibleReactor => "no-eligible-reactor",
            _ => throw new JsonException("The Reaction close reason is unsupported."),
        };

    private static CampaignReactionWindowCloseReason ParseCloseReason(string? reason) =>
        reason switch
        {
            "player-decline" => CampaignReactionWindowCloseReason.PlayerDecline,
            "scripted-unavailable" => CampaignReactionWindowCloseReason.ScriptedUnavailable,
            "timeout" => CampaignReactionWindowCloseReason.Timeout,
            "no-eligible-reactor" => CampaignReactionWindowCloseReason.NoEligibleReactor,
            _ => throw new JsonException("The Reaction close reason is unsupported."),
        };

    private static void ValidateMoved(ElementMovedV2 moved)
    {
        try
        {
            moved.ValidateContract();
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidOperationException)
        {
            throw new JsonException("The ElementMoved v2 contract is invalid.", exception);
        }
    }

    private static void ValidateReactionWindowClosed(ReactionWindowClosed closed)
    {
        try
        {
            closed.ValidateContract();
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidOperationException)
        {
            throw new JsonException(
                "The ReactionWindowClosed contract is invalid.",
                exception);
        }
    }

    private static void ValidateReactingElementMoved(ReactingElementMoved moved)
    {
        try
        {
            moved.ValidateContract();
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidOperationException)
        {
            throw new JsonException("The ReactingElementMoved contract is invalid.", exception);
        }
    }

    private static void ValidateReactionParticipantCompleted(
        ReactionParticipantCompleted completed)
    {
        try
        {
            completed.ValidateContract();
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidOperationException)
        {
            throw new JsonException(
                "The ReactionParticipantCompleted contract is invalid.",
                exception);
        }
    }

    private static CapabilityPointAmount ParseCapabilityPoints(JsonElement value) =>
        CapabilityPointAmountCodec.Deserialize(Encoding.UTF8.GetBytes(value.GetRawText()));
}
