using System.Text;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal static class CampaignV11CanonicalCodec
{
    public static void WriteSetup(Utf8JsonWriter writer, CampaignSetupSnapshotV6 setup) =>
        CampaignSetupV6Codec.WriteSetup(writer, setup);
    public static CampaignSetupSnapshotV6 ParseSetup(JsonElement value) => CampaignSetupV6Codec.ParseSetup(value);

    public static void WriteWorld(
        Utf8JsonWriter writer,
        string propertyName,
        CampaignWorldSnapshotV6 world)
    {
        writer.WriteStartObject(propertyName);
        writer.WriteNumber("contractVersion", world.ContractVersion);
        writer.WriteStartArray("elements");
        foreach (var element in world.Elements)
        {
            writer.WriteStartObject();
            writer.WriteString("elementId", element.ElementId);
            writer.WriteString("currentLocationId", element.CurrentLocationId);
            writer.WriteString(
                "reserveStatus",
                CampaignSnapshotSerializer.FormatReserveStatus(element.ReserveStatus));
            writer.WriteStartObject("operationalState");
            writer.WriteNumber("ledgerGameTurn", element.OperationalState.LedgerGameTurn);
            writer.WriteNumber(
                "ledgerOperationStage",
                element.OperationalState.LedgerOperationStage);
            writer.WritePropertyName("capabilityPointsExpended");
            CapabilityPointAmountCodec.WriteCanonical(
                writer,
                element.OperationalState.CapabilityPointsExpended);
            writer.WriteNumber("cohesionLevel", element.OperationalState.CohesionLevel);
            CampaignSnapshotSerializer.WriteVehicleBreakdownState(
                writer,
                element.OperationalState.VehicleBreakdownState);
            WriteMovementEnded(writer, element.OperationalState.MovementEnded);
            writer.WriteEndObject();
            writer.WriteStartArray("components");
            foreach (var component in element.Components)
            {
                writer.WriteStartObject();
                writer.WriteString("componentId", component.ComponentId);
                writer.WriteNumber("currentToe", component.CurrentToe);
                WriteOrigin(writer, component.InitialToeOrigin);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("representations");
        foreach (var representation in world.Representations)
        {
            WriteRepresentationValue(writer, representation);
        }

        writer.WriteEndArray();
        writer.WriteStartArray("brokenVehicleLots");
        foreach (var lot in world.BrokenVehicleLots) CampaignBreakdownCodec.WriteLot(writer, lot);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    public static CampaignWorldSnapshotV6 ParseWorld(JsonElement world)
    {
        CampaignSnapshotSerializer.RequireProperties(
            world,
            "contractVersion",
            "elements",
            "representations", "brokenVehicleLots");
        return new CampaignWorldSnapshotV6(
            world.GetProperty("contractVersion").GetInt32(),
            world.GetProperty("elements").EnumerateArray().Select(ParseElement).ToArray(),
            world.GetProperty("representations")
                .EnumerateArray().Select(ParseRepresentation).ToArray(),
            world.GetProperty("brokenVehicleLots").EnumerateArray().Select(CampaignBreakdownCodec.ParseLot));
    }

    public static void WriteCurrentPosition(
        Utf8JsonWriter writer,
        CampaignPositionV11 position)
    {
        writer.WriteStartObject("currentPosition");
        switch (position.Kind)
        {
            case CampaignPositionV11Kind.Sequence:
                writer.WriteString("kind", "sequence");
                WritePosition(writer, "sequencePosition", position.SequencePosition!);
                break;
            case CampaignPositionV11Kind.Reaction:
                writer.WriteString("kind", "reaction");
                WriteReactingPosition(writer, "reactingPosition", position.ReactingPosition!);
                break;
            case CampaignPositionV11Kind.BreakdownStop:
                writer.WriteString("kind", "breakdown-stop");
                WritePosition(writer, "suspendedSequencePosition", position.SuspendedSequencePosition!);
                break;
            default:
                throw new JsonException("Unknown Campaign v11 current position kind.");
        }

        writer.WriteEndObject();
    }

    public static CampaignPositionV11 ParseCurrentPosition(JsonElement position)
    {
        var kind = position.GetProperty("kind").GetString();
        return kind switch
        {
            "sequence" => ParseSequencePosition(position),
            "reaction" => ParseReactionPosition(position),
            "breakdown-stop" => ParseBreakdownPosition(position),
            _ => throw new JsonException($"Unknown Campaign v11 position kind '{kind}'."),
        };
    }

    public static void WriteReactionWindow(
        Utf8JsonWriter writer,
        string propertyName,
        CampaignReactionWindow? window)
    {
        if (window is null)
        {
            writer.WriteNull(propertyName);
            return;
        }

        writer.WriteStartObject(propertyName);
        writer.WriteString("reactionWindowId", window.WindowId.Value);
        writer.WriteNumber(
            "triggerCommittedStateVersion",
            window.TriggerCommittedStateVersion);
        writer.WriteString(
            "phasingSide",
            CampaignSnapshotSerializer.FormatSide(window.PhasingSide));
        writer.WriteString(
            "reactingSide",
            CampaignSnapshotSerializer.FormatSide(window.ReactingSide));
        WriteReactingPosition(writer, "reactingPosition", window.ReactingPosition);
        WriteTriggerAuthority(writer, window.TriggerAuthority);
        WriteApparentTrigger(writer, window.ApparentTrigger);
        writer.WriteStartArray("frozenOpportunities");
        foreach (var opportunity in window.FrozenOpportunities)
        {
            writer.WriteStartObject();
            writer.WriteString("opportunityId", opportunity.OpportunityId.Value);
            writer.WritePropertyName("reactingRepresentation");
            WriteRepresentationValue(writer, opportunity.ReactingRepresentation);
            writer.WriteStartObject("adjacencyEvidence");
            writer.WriteString(
                "triggerLocationId",
                opportunity.AdjacencyEvidence.TriggerLocationId);
            writer.WriteString(
                "committedDestinationLocationId",
                opportunity.AdjacencyEvidence.CommittedDestinationLocationId);
            writer.WriteBoolean("isAdjacent", opportunity.AdjacencyEvidence.IsAdjacent);
            CampaignSnapshotSerializer.WriteSources(
                writer,
                opportunity.AdjacencyEvidence.Sources);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("resolvedOpportunityIds");
        foreach (var id in window.ResolvedOpportunityIds)
        {
            writer.WriteStringValue(id.Value);
        }

        writer.WriteEndArray();
        if (window.ActiveOpportunityId is null)
        {
            writer.WriteNull("activeOpportunityId");
        }
        else
        {
            writer.WriteString("activeOpportunityId", window.ActiveOpportunityId.Value);
        }

        writer.WriteEndObject();
    }

    public static CampaignReactionWindow? ParseReactionWindow(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        CampaignSnapshotSerializer.RequireProperties(
            element,
            "reactionWindowId",
            "triggerCommittedStateVersion",
            "phasingSide",
            "reactingSide",
            "reactingPosition",
            "triggerAuthority",
            "apparentTrigger",
            "frozenOpportunities",
            "resolvedOpportunityIds",
            "activeOpportunityId");
        var active = element.GetProperty("activeOpportunityId");
        return new CampaignReactionWindow(
            new CampaignReactionWindowId(
                element.GetProperty("reactionWindowId").GetString()!),
            element.GetProperty("triggerCommittedStateVersion").GetInt64(),
            CampaignSnapshotSerializer.ParseSide(
                element.GetProperty("phasingSide").GetString()),
            CampaignSnapshotSerializer.ParseSide(
                element.GetProperty("reactingSide").GetString()),
            ParseReactingPosition(element.GetProperty("reactingPosition")),
            ParseTriggerAuthority(element.GetProperty("triggerAuthority")),
            ParseApparentTrigger(element.GetProperty("apparentTrigger")),
            element.GetProperty("frozenOpportunities")
                .EnumerateArray().Select(ParseOpportunity).ToArray(),
            element.GetProperty("resolvedOpportunityIds")
                .EnumerateArray()
                .Select(value => new CampaignReactionOpportunityId(value.GetString()!))
                .ToArray(),
            active.ValueKind == JsonValueKind.Null
                ? null
                : new CampaignReactionOpportunityId(active.GetString()!));
    }

    public static void WritePosition(
        Utf8JsonWriter writer,
        string propertyName,
        LandSequencePosition position)
    {
        writer.WriteStartObject(propertyName);
        writer.WriteNumber("contractVersion", position.ContractVersion);
        writer.WriteString("positionId", position.PositionId);
        writer.WriteNumber("gameTurn", position.GameTurn);
        writer.WriteNumber("operationStage", position.OperationStage);
        writer.WriteString("stageId", position.StageId);
        writer.WriteString("phaseId", position.PhaseId);
        WriteNullableString(writer, "segmentId", position.SegmentId);
        WriteNullableString(writer, "stepId", position.StepId);
        writer.WriteString("actorRole", FormatActorRole(position.ActorRole));
        if (position.ActiveSide is null)
        {
            writer.WriteNull("activeSide");
        }
        else
        {
            writer.WriteString(
                "activeSide",
                CampaignSnapshotSerializer.FormatSide(position.ActiveSide.Value));
        }

        CampaignSnapshotSerializer.WriteSources(writer, position.Sources);
        writer.WriteEndObject();
    }

    public static LandSequencePosition ParsePosition(JsonElement position)
    {
        CampaignSnapshotSerializer.RequireProperties(
            position,
            "contractVersion",
            "positionId",
            "gameTurn",
            "operationStage",
            "stageId",
            "phaseId",
            "segmentId",
            "stepId",
            "actorRole",
            "activeSide",
            "sources");
        var active = position.GetProperty("activeSide");
        return new LandSequencePosition(
            position.GetProperty("contractVersion").GetInt32(),
            position.GetProperty("positionId").GetString()!,
            position.GetProperty("gameTurn").GetInt32(),
            position.GetProperty("operationStage").GetInt32(),
            position.GetProperty("stageId").GetString()!,
            position.GetProperty("phaseId").GetString()!,
            ReadNullableString(position.GetProperty("segmentId")),
            ReadNullableString(position.GetProperty("stepId")),
            ParseActorRole(position.GetProperty("actorRole").GetString()),
            active.ValueKind == JsonValueKind.Null
                ? null
                : CampaignSnapshotSerializer.ParseSide(active.GetString()),
            CampaignSnapshotSerializer.ParseSources(position.GetProperty("sources")));
    }

    private static CampaignElementStateV5 ParseElement(JsonElement element)
    {
        CampaignSnapshotSerializer.RequireProperties(
            element,
            "elementId",
            "currentLocationId",
            "reserveStatus",
            "operationalState",
            "components");
        var operational = element.GetProperty("operationalState");
        CampaignSnapshotSerializer.RequireProperties(
            operational,
            "ledgerGameTurn",
            "ledgerOperationStage",
            "capabilityPointsExpended",
            "cohesionLevel",
            "vehicleBreakdownState",
            "movementEnded");
        return new CampaignElementStateV5(
            element.GetProperty("elementId").GetString()!,
            element.GetProperty("currentLocationId").GetString()!,
            CampaignSnapshotSerializer.ParseReserveStatus(
                element.GetProperty("reserveStatus").GetString()),
            new CampaignElementOperationalStateV5(
                operational.GetProperty("ledgerGameTurn").GetInt32(),
                operational.GetProperty("ledgerOperationStage").GetInt32(),
                ParseCapabilityPoints(
                    operational.GetProperty("capabilityPointsExpended")),
                operational.GetProperty("cohesionLevel").GetInt32(),
                CampaignSnapshotSerializer.ParseVehicleBreakdownState(
                    operational.GetProperty("vehicleBreakdownState")),
                ParseMovementEnded(operational.GetProperty("movementEnded"))),
            element.GetProperty("components").EnumerateArray().Select(component =>
            {
                CampaignSnapshotSerializer.RequireProperties(
                    component,
                    "componentId",
                    "currentToe",
                    "initialToeOrigin");
                return new CampaignComponentToeState(
                    component.GetProperty("componentId").GetString()!,
                    component.GetProperty("currentToe").GetInt32(),
                    ParseOrigin(component.GetProperty("initialToeOrigin")));
            }).ToArray());
    }

    internal static void WriteMovementEnded(
        Utf8JsonWriter writer,
        CampaignMovementEndedState? state,
        string propertyName = "movementEnded")
    {
        if (state is null)
        {
            writer.WriteNull(propertyName);
            return;
        }

        writer.WriteStartObject(propertyName);
        writer.WriteNumber("sequenceContractVersion", state.SequenceContractVersion);
        writer.WriteString("positionId", state.PositionId);
        writer.WriteNumber("gameTurn", state.GameTurn);
        writer.WriteNumber("operationStage", state.OperationStage);
        writer.WriteString("stageId", state.StageId);
        writer.WriteString("phaseId", state.PhaseId);
        writer.WriteString("segmentId", state.SegmentId);
        writer.WriteString(
            "phasingSide",
            CampaignSnapshotSerializer.FormatSide(state.PhasingSide));
        writer.WriteEndObject();
    }

    internal static CampaignMovementEndedState? ParseMovementEnded(JsonElement state)
    {
        if (state.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        CampaignSnapshotSerializer.RequireProperties(
            state,
            "sequenceContractVersion",
            "positionId",
            "gameTurn",
            "operationStage",
            "stageId",
            "phaseId",
            "segmentId",
            "phasingSide");
        var source = Cna1979LandSequence.CreateTurn(state.GetProperty("gameTurn").GetInt32())
            .Single(value => string.Equals(
                value.PositionId,
                state.GetProperty("positionId").GetString(),
                StringComparison.Ordinal));
        var position = new LandSequencePosition(
            state.GetProperty("sequenceContractVersion").GetInt32(),
            state.GetProperty("positionId").GetString()!,
            state.GetProperty("gameTurn").GetInt32(),
            state.GetProperty("operationStage").GetInt32(),
            state.GetProperty("stageId").GetString()!,
            state.GetProperty("phaseId").GetString()!,
            state.GetProperty("segmentId").GetString(),
            null,
            source.ActorRole,
            CampaignSnapshotSerializer.ParseSide(
                state.GetProperty("phasingSide").GetString()),
            source.Sources);
        return CampaignMovementEndedState.CreateForBreakdown(position);
    }

    private static void WriteOrigin(Utf8JsonWriter writer, ContentOrigin origin)
    {
        writer.WriteStartObject("initialToeOrigin");
        writer.WriteString("kind", origin.Kind switch
        {
            ContentOriginKind.SourceDerived => "source-derived",
            ContentOriginKind.Synthetic => "synthetic",
            _ => throw new JsonException("Unknown Content origin kind."),
        });
        writer.WriteStartArray("references");
        foreach (var reference in origin.References)
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", reference.SourceId);
            writer.WriteString("locator", reference.Locator);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static ContentOrigin ParseOrigin(JsonElement origin)
    {
        CampaignSnapshotSerializer.RequireProperties(origin, "kind", "references");
        var kind = origin.GetProperty("kind").GetString() switch
        {
            "source-derived" => ContentOriginKind.SourceDerived,
            "synthetic" => ContentOriginKind.Synthetic,
            var value => throw new JsonException($"Unknown Content origin kind '{value}'."),
        };
        return new ContentOrigin(
            kind,
            CampaignSnapshotSerializer.ParseSources(origin.GetProperty("references")));
    }

    private static void WriteReactingPosition(
        Utf8JsonWriter writer,
        string propertyName,
        CampaignReactingPosition position)
    {
        writer.WriteStartObject(propertyName);
        WritePosition(
            writer,
            "suspendedMovementPosition",
            position.SuspendedMovementPosition);
        writer.WriteString(
            "phasingSide",
            CampaignSnapshotSerializer.FormatSide(position.PhasingSide));
        writer.WriteString(
            "reactingSide",
            CampaignSnapshotSerializer.FormatSide(position.ReactingSide));
        writer.WriteEndObject();
    }

    private static CampaignReactingPosition ParseReactingPosition(JsonElement position)
    {
        CampaignSnapshotSerializer.RequireProperties(
            position,
            "suspendedMovementPosition",
            "phasingSide",
            "reactingSide");
        var result = CampaignReactingPosition.CreateForBreakdown(
            ParsePosition(position.GetProperty("suspendedMovementPosition")));
        if (result.PhasingSide != CampaignSnapshotSerializer.ParseSide(
                position.GetProperty("phasingSide").GetString())
            || result.ReactingSide != CampaignSnapshotSerializer.ParseSide(
                position.GetProperty("reactingSide").GetString()))
        {
            throw new JsonException("The reacting position side projection is invalid.");
        }

        return result;
    }

    private static void WriteTriggerAuthority(
        Utf8JsonWriter writer,
        CampaignReactionTriggerAuthority trigger)
    {
        writer.WriteStartObject("triggerAuthority");
        writer.WriteNumber("moveContractVersion", trigger.MoveContractVersion);
        writer.WriteString("elementId", trigger.ElementId);
        writer.WritePropertyName("triggeringRepresentation");
        WriteRepresentationValue(writer, trigger.TriggeringRepresentation);
        writer.WriteString("originLocationId", trigger.OriginLocationId);
        writer.WriteString("destinationLocationId", trigger.DestinationLocationId);
        writer.WriteEndObject();
    }

    private static CampaignReactionTriggerAuthority ParseTriggerAuthority(JsonElement trigger)
    {
        CampaignSnapshotSerializer.RequireProperties(
            trigger,
            "moveContractVersion",
            "elementId",
            "triggeringRepresentation",
            "originLocationId",
            "destinationLocationId");
        if (trigger.GetProperty("moveContractVersion").GetInt32() != 3)
            throw new JsonException("Expected Breakdown move contract 3.");
        return CampaignReactionTriggerAuthority.CreateForBreakdown(
            trigger.GetProperty("elementId").GetString()!,
            ParseRepresentation(trigger.GetProperty("triggeringRepresentation")),
            trigger.GetProperty("originLocationId").GetString()!,
            trigger.GetProperty("destinationLocationId").GetString()!);
    }

    private static void WriteApparentTrigger(
        Utf8JsonWriter writer,
        CampaignApparentReactionTrigger trigger)
    {
        writer.WriteStartObject("apparentTrigger");
        writer.WriteString("apparentRepresentationId", trigger.ApparentRepresentationId);
        writer.WriteString("originLocationId", trigger.OriginLocationId);
        writer.WriteString("destinationLocationId", trigger.DestinationLocationId);
        writer.WriteEndObject();
    }

    private static CampaignApparentReactionTrigger ParseApparentTrigger(JsonElement trigger)
    {
        CampaignSnapshotSerializer.RequireProperties(
            trigger,
            "apparentRepresentationId",
            "originLocationId",
            "destinationLocationId");
        return new CampaignApparentReactionTrigger(
            trigger.GetProperty("apparentRepresentationId").GetString()!,
            trigger.GetProperty("originLocationId").GetString()!,
            trigger.GetProperty("destinationLocationId").GetString()!);
    }

    private static CampaignFrozenReactionOpportunity ParseOpportunity(JsonElement opportunity)
    {
        CampaignSnapshotSerializer.RequireProperties(
            opportunity,
            "opportunityId",
            "reactingRepresentation",
            "adjacencyEvidence");
        var evidence = opportunity.GetProperty("adjacencyEvidence");
        CampaignSnapshotSerializer.RequireProperties(
            evidence,
            "triggerLocationId",
            "committedDestinationLocationId",
            "isAdjacent",
            "sources");
        return new CampaignFrozenReactionOpportunity(
            new CampaignReactionOpportunityId(
                opportunity.GetProperty("opportunityId").GetString()!),
            ParseRepresentation(opportunity.GetProperty("reactingRepresentation")),
            new CampaignReactionAdjacencyEvidence(
                evidence.GetProperty("triggerLocationId").GetString()!,
                evidence.GetProperty("committedDestinationLocationId").GetString()!,
                evidence.GetProperty("isAdjacent").GetBoolean(),
                CampaignSnapshotSerializer.ParseSources(
                    evidence.GetProperty("sources"))));
    }

    private static void WriteRepresentationValue(
        Utf8JsonWriter writer,
        CampaignMapRepresentationState representation)
    {
        writer.WriteStartObject();
        writer.WriteString("representationId", representation.RepresentationId);
        writer.WriteString("currentLocationId", representation.CurrentLocationId);
        writer.WriteString(
            "bindingKind",
            CampaignSnapshotSerializer.FormatRepresentationBindingKind(
                representation.BindingKind));
        writer.WriteStartArray("boundElementIds");
        foreach (var id in representation.BoundElementIds)
        {
            writer.WriteStringValue(id);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static CampaignMapRepresentationState ParseRepresentation(JsonElement value)
    {
        CampaignSnapshotSerializer.RequireProperties(
            value,
            "representationId",
            "currentLocationId",
            "bindingKind",
            "boundElementIds");
        return new CampaignMapRepresentationState(
            value.GetProperty("representationId").GetString()!,
            value.GetProperty("currentLocationId").GetString()!,
            CampaignSnapshotSerializer.ParseRepresentationBindingKind(
                value.GetProperty("bindingKind").GetString()),
            value.GetProperty("boundElementIds").EnumerateArray()
                .Select(id => id.GetString()!).ToArray());
    }

    private static CampaignPositionV11 ParseBreakdownPosition(JsonElement position)
    {
        CampaignSnapshotSerializer.RequireProperties(position, "kind", "suspendedSequencePosition");
        return CampaignPositionV11.FromBreakdownStop(ParsePosition(position.GetProperty("suspendedSequencePosition")));
    }

    private static CampaignPositionV11 ParseSequencePosition(JsonElement position)
    {
        CampaignSnapshotSerializer.RequireProperties(position, "kind", "sequencePosition");
        return CampaignPositionV11.FromSequence(
            ParsePosition(position.GetProperty("sequencePosition")));
    }

    private static CampaignPositionV11 ParseReactionPosition(JsonElement position)
    {
        CampaignSnapshotSerializer.RequireProperties(position, "kind", "reactingPosition");
        return CampaignPositionV11.FromReaction(
            ParseReactingPosition(position.GetProperty("reactingPosition")));
    }

    private static CapabilityPointAmount ParseCapabilityPoints(JsonElement value) =>
        CapabilityPointAmountCodec.Deserialize(Encoding.UTF8.GetBytes(value.GetRawText()));

    private static string FormatActorRole(LandActorRole role) => role switch
    {
        LandActorRole.None => "none",
        LandActorRole.Commonwealth => "commonwealth",
        LandActorRole.InitiativeHolder => "initiative-holder",
        LandActorRole.FirstActingSide => "first-acting-side",
        LandActorRole.SecondActingSide => "second-acting-side",
        _ => throw new JsonException("Unknown land actor role."),
    };

    private static LandActorRole ParseActorRole(string? role) => role switch
    {
        "none" => LandActorRole.None,
        "commonwealth" => LandActorRole.Commonwealth,
        "initiative-holder" => LandActorRole.InitiativeHolder,
        "first-acting-side" => LandActorRole.FirstActingSide,
        "second-acting-side" => LandActorRole.SecondActingSide,
        _ => throw new JsonException($"Unknown land actor role '{role}'."),
    };

    private static void WriteNullableString(
        Utf8JsonWriter writer,
        string propertyName,
        string? value)
    {
        if (value is null) writer.WriteNull(propertyName);
        else writer.WriteString(propertyName, value);
    }

    private static string? ReadNullableString(JsonElement value) =>
        value.ValueKind == JsonValueKind.Null ? null : value.GetString();
}
