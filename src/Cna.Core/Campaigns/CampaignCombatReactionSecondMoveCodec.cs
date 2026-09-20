using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatReactionSecondMoveCodec
{
    public static byte[] SerializeInput(CampaignCombatReactionSecondMoveInput input)
    {
        if (input?.Command is null) throw new JsonException("Missing second move input.");
        return CampaignCombatReactionLifecycleCodec.SerializeInput(new(input.Command, input.Actor));
    }
    public static CampaignCombatReactionSecondMoveInput DeserializeInput(ReadOnlySpan<byte> bytes)
    {
        var input = CampaignCombatReactionLifecycleCodec.DeserializeInput(bytes);
        if (input.Command is not CampaignCombatReactionLifecycleCommand.Move move) throw new JsonException("Second move requires movement command.");
        return new(move, input.Actor);
    }
    internal static CampaignCombatReactionSecondMoveInput ReadEventInput(ReadOnlySpan<byte> bytes)
    {
        var input = CampaignCombatReactionLifecycleCodec.ReadEventInput(bytes);
        if (input.Command is not CampaignCombatReactionLifecycleCommand.Move move) throw new JsonException("Second move requires movement event.");
        return new(move, input.Actor);
    }
    internal static byte[] SerializeEvent(CampaignCombatReactionSecondMoveEvent value, bool includeReceipt = true) => Bytes(writer =>
    {
        var state = value.Before; var opening = state.Opening; var cycle = opening.Cycle!; var move = value.Input.Command;
        var window = state.ReactionWindow.Trigger; var opportunity = window.FrozenOpportunities.Single();
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", 3); writer.WriteString("eventType", "reacting-element-moved");
        writer.WriteString("campaignId", cycle.CampaignId); writer.WriteNumber("stateVersion", value.StateVersion); writer.WriteNumber("priorStateVersion", state.StateVersion);
        writer.WriteString("fromPositionId", state.SequencePosition.PositionId); writer.WriteNumber("gameTurn", 1); writer.WriteNumber("operationStage", 1);
        writer.WriteString("actingSide", CampaignSnapshotSerializer.FormatSide(window.ReactingSide)); writer.WriteString("actionId", move.Identity.ActionId);
        writer.WriteString("submittedWindowId", move.WindowId); writer.WriteString("submittedOpportunityId", move.OpportunityId);
        writer.WriteString("windowId", window.WindowId.Value); writer.WriteString("opportunityId", opportunity.OpportunityId.Value);
        var element = state.World.Elements.Single(e => e.ElementId == opportunity.ReactingRepresentation.BoundElementIds.Single());
        writer.WriteString("elementId", element.ElementId); writer.WriteString("representationId", opportunity.ReactingRepresentation.RepresentationId);
        writer.WriteString("originLocationId", move.OriginLocationId); writer.WriteString("destinationLocationId", move.DestinationLocationId);
        writer.WriteString("mobilityId", Cna1979Movement.NonMotorizedMobilityId); WriteReferences(writer, "mobilitySources", [new("spi-1979-map-a", "8.37")]);
        writer.WriteStartObject("cost"); writer.WriteString("destinationTerrainId", "land.terrain.clear"); Cp(writer, "destinationTerrainCost", new(2, 1));
        WriteReferences(writer, "destinationTerrainSources", [new("spi-1979-map-a", "8.37")]); writer.WriteNull("routeAdjustment");
        WriteEmptyArray(writer, "crossedHexsideCosts"); Cp(writer, "totalCost", new(2, 1)); writer.WriteEndObject();
        Cp(writer, "capabilityPointsExpendedBefore", element.OperationalState.CapabilityPointsExpended); Cp(writer, "capabilityPointsExpendedAfter", new(4, 1));
        writer.WriteNumber("cohesionBefore", element.OperationalState.CohesionLevel); writer.WriteNumber("cohesionAfter", element.OperationalState.CohesionLevel);
        CampaignCombatReactionLifecycleCodec.WriteWindow(writer, "reactionWindowAfter", state.ReactionWindow); writer.WriteString("rulesetHash", cycle.RulesetHash);
        WriteEmptyArray(writer, "breakdownAccounting"); writer.WritePropertyName("breakdownFlowAfter"); CampaignBreakdownCodec.WriteFlow(writer, value.Flow);
        var creation = opening.Predecessor.Stage.Weather.Opening.Creation;
        writer.WriteString("configurationHash", creation.Configuration.Hash); writer.WriteString("creationBinding", creation.CreationReceipt.CreationBinding);
        writer.WriteString("creationEventHash", creation.CreationReceipt.CreationEventHash); writer.WriteString("cycleId", opening.CycleId);
        writer.WriteString("openingBaseHash", opening.OpeningBaseHash); writer.WriteString("completionReceiptId", opening.CompletionReceiptId);
        writer.WriteString("priorPrefix", state.Prefix); writer.WritePropertyName("input"); writer.WriteRawValue(SerializeInput(value.Input));
        if (includeReceipt) writer.WriteString("receiptId", value.ReceiptId);
        writer.WriteEndObject();
    });
    private static void WriteUnit(Utf8JsonWriter writer, CampaignCombatUnitKey unit)
    {
        writer.WriteStartObject("unit"); writer.WriteString("creationBinding", unit.CreationBinding);
        writer.WriteString("originalSide", unit.OriginalSide); writer.WriteString("elementId", unit.ElementId); writer.WriteEndObject();
    }
    private static void Cp(Utf8JsonWriter writer, string name, CapabilityPointAmount value)
    { writer.WritePropertyName(name); CapabilityPointAmountCodec.WriteCanonical(writer, value); }
    private static void WriteReferences(Utf8JsonWriter writer, string name, IReadOnlyList<RuleReference> sources)
    {
        writer.WriteStartArray(name);
        foreach (var source in sources) { writer.WriteStartObject(); writer.WriteString("sourceId", source.SourceId); writer.WriteString("locator", source.Locator); writer.WriteEndObject(); }
        writer.WriteEndArray();
    }
    public static byte[] SerializeState(CampaignCombatReactionSecondMoveState state) => Bytes(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 1);
        WriteAuthority(writer, state.Opening.Predecessor);
        writer.WriteNumber("stateVersion", state.StateVersion);
        writer.WriteString("prefix", state.Prefix);
        CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", state.SequencePosition);
        writer.WriteStartObject("currentPosition");
        writer.WriteString("kind", "reaction");
        CampaignCombatReactionLifecycleCodec.WriteReactingPosition(writer, "reactingPosition", state.ReactionWindow.Trigger.ReactingPosition);
        writer.WriteEndObject();
        writer.WriteString("initiativeHolder", CampaignSnapshotSerializer.FormatSide(state.Opening.Predecessor.Stage.Weather.Opening.InitiativeHolder!.Value));
        writer.WriteStartArray("operationStageOrders");
        foreach (var order in state.Opening.Predecessor.Stage.Weather.Opening.Orders)
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteNumber("gameTurn", order.GameTurn);
            writer.WriteNumber("operationStage", order.OperationStage);
            writer.WriteString("firstSide", CampaignSnapshotSerializer.FormatSide(order.FirstSide));
            writer.WriteString("secondSide", CampaignSnapshotSerializer.FormatSide(order.SecondSide));
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        CampaignOperationStageWeatherCodec.Write(writer, state.Opening.Predecessor.Stage.Weather.Weather);
        writer.WritePropertyName("world");
        writer.WriteRawValue(WriteWorld(state));
        CampaignSnapshotSerializer.WriteRandomState(writer, state.Opening.Predecessor.Stage.Weather.RandomState);
        writer.WriteStartArray("receipts");
        foreach (var receipt in state.Receipts)
        {
            writer.WriteStartObject();
            writer.WriteString("commandHash", receipt.CommandHash);
            writer.WriteString("eventHash", receipt.EventHash);
            writer.WriteString("receiptId", receipt.ReceiptId);
            writer.WriteString("actor", Actor(receipt.Actor));
            writer.WriteNumber("stateVersion", receipt.StateVersion);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteString("firstActingSide", CampaignSnapshotSerializer.FormatSide(state.Opening.Predecessor.FirstActingSide));
        CampaignCombatReserveCodec.WriteMembers(writer, state.Predecessor.Trigger.Members);
        if (state.Opening.Cycle is null) writer.WriteNull("cycle");
        else
        {
            writer.WritePropertyName("cycle");
            CampaignCombatReserveCompletionCodec.WriteCycle(writer, state.Opening.Cycle);
        }
        writer.WriteString("cycleId", state.Opening.CycleId);
        writer.WriteString("openingBaseHash", state.Opening.OpeningBaseHash);
        writer.WriteString("completionReceiptId", state.Opening.CompletionReceiptId);
        writer.WriteStartArray("tracks");
        foreach (var track in state.Tracks)
        {
            writer.WriteStartObject(); WriteUnit(writer, track.Unit);
            writer.WriteStartArray("route");
            foreach (var location in track.Route) writer.WriteStringValue(location);
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteStartArray("actualProgressRefs");
        foreach (var reference in state.ActualProgressRefs)
        {
            writer.WriteStartObject(); writer.WriteString("eventType", reference.EventType);
            writer.WriteString("receiptId", reference.ReceiptId); writer.WriteString("eventHash", reference.EventHash);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        CampaignCombatReactionLifecycleCodec.WriteWindow(writer, "reactionWindow", state.ReactionWindow);
        writer.WritePropertyName("breakdownFlow");
        CampaignBreakdownCodec.WriteFlow(writer, state.BreakdownFlow);
        writer.WriteEndObject();
    });

    private static void WriteAuthority(Utf8JsonWriter writer, CampaignCombatReserveState state)
    {
        var creation = state.Stage.Weather.Opening.Creation;
        writer.WriteString("campaignId", creation.CampaignId);
        writer.WriteString("rulesetHash", creation.RulesetHash);
        writer.WriteString("configurationHash", creation.Configuration.Hash);
        writer.WriteString("creationBinding", creation.CreationReceipt.CreationBinding);
        writer.WriteString("creationEventHash", creation.CreationReceipt.CreationEventHash);
    }
    private static byte[] Bytes(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) write(writer);
        if (stream.Length > 1_048_576) throw new JsonException("Reaction second move record exceeds byte limit.");
        return stream.ToArray();
    }
    private static string Actor(CampaignOpeningPreambleActor actor) => actor switch
    {
        CampaignOpeningPreambleActor.System => "system",
        CampaignOpeningPreambleActor.Axis => "axis",
        CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
        _ => throw new JsonException("Unknown Reaction second move actor."),
    };
    internal static byte[] WriteWorld(CampaignCombatReactionSecondMoveState state)
    {
        // This writer handles only the history-derived reaction second move World profile.
        // Equality protects hardcoded absent fields; it never admits an arbitrary World7.
        var world = CampaignCombatReactionSecondMove.ExpectedWorld(state);
        if (state.World != world) throw new JsonException("Movement World differs from history-derived Reaction second move.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", world.ContractVersion);
            writer.WriteString("creationBinding", world.CreationBinding);
            writer.WriteStartArray("elements");
            foreach (var element in world.Elements)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", element.ElementId);
                writer.WriteString("currentLocationId", element.CurrentLocationId);
                writer.WriteString("reserveStatus", CampaignSnapshotSerializer.FormatReserveStatus(
                    element.ReserveStatus));
                writer.WriteStartObject("operationalState");
                writer.WriteNumber("ledgerGameTurn", element.OperationalState.LedgerGameTurn);
                writer.WriteNumber("ledgerOperationStage", element.OperationalState.LedgerOperationStage);
                writer.WritePropertyName("capabilityPointsExpended");
                CapabilityPointAmountCodec.WriteCanonical(writer,
                    element.OperationalState.CapabilityPointsExpended);
                writer.WriteNumber("cohesionLevel", element.OperationalState.CohesionLevel);
                writer.WriteNull("vehicleBreakdownState");
                writer.WriteNull("movementEnded");
                WriteOrigin(writer, "initialLedgerOrigin", element.OperationalState.InitialLedgerOrigin);
                writer.WriteEndObject();
                writer.WriteStartArray("components");
                foreach (var component in element.Components)
                {
                    writer.WriteStartObject();
                    writer.WriteString("componentId", component.ComponentId);
                    writer.WriteNumber("currentToe", component.CurrentToe);
                    WriteOrigin(writer, "initialToeOrigin", component.InitialToeOrigin);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteString("sourceParentFormationId", element.SourceParentFormationId);
                writer.WriteString("currentParentFormationId", element.CurrentParentFormationId);
                writer.WriteStartObject("ammunition");
                writer.WriteNumber("points", element.Ammunition.Points);
                WriteOrigin(writer, "initialAmmunitionOrigin", element.Ammunition.InitialAmmunitionOrigin);
                writer.WriteEndObject();
                writer.WriteStartObject("readiness");
                writer.WriteNumber("gameTurn", element.Readiness.GameTurn);
                writer.WriteNumber("operationStage", element.Readiness.OperationStage);
                writer.WriteString("waterStatus", element.Readiness.WaterStatus);
                writer.WriteString("storesStatus", element.Readiness.StoresStatus);
                writer.WriteBoolean("pinned", element.Readiness.Pinned);
                WriteOrigin(writer, "initialReadinessOrigin", element.Readiness.InitialReadinessOrigin);
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("representations");
            foreach (var representation in world.Representations)
            {
                writer.WriteStartObject();
                writer.WriteString("representationId", representation.RepresentationId);
                writer.WriteString("currentLocationId", representation.CurrentLocationId);
                writer.WriteString("bindingKind", "independent-element");
                writer.WriteStartArray("boundElementIds");
                foreach (var elementId in representation.BoundElementIds)
                    writer.WriteStringValue(elementId);
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            WriteEmptyArray(writer, "brokenVehicleLots");
            writer.WriteStartArray("cohesionCauses");
            foreach (var cause in world.CohesionCauses)
            {
                writer.WriteStartObject();
                writer.WriteString("causeId", cause.CauseId); writer.WriteNumber("ordinal", cause.Ordinal);
                writer.WriteString("receiptId", cause.ReceiptId); writer.WriteString("elementId", cause.ElementId);
                writer.WriteNumber("gameTurn", cause.GameTurn); writer.WriteNumber("operationStage", cause.OperationStage);
                writer.WriteString("kind", cause.Kind); writer.WriteNumber("points", cause.Points);
                writer.WriteNumber("before", cause.Before); writer.WriteNumber("after", cause.After);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            WriteEmptyArray(writer, "relationships");
            WriteEmptyArray(writer, "custodyLots");
            WriteEmptyArray(writer, "guards");
            WriteEmptyArray(writer, "replacementEntitlements");
            WriteEmptyArray(writer, "futureObligations");
            WriteEmptyArray(writer, "settlements");
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static void WriteOrigin(Utf8JsonWriter writer, string propertyName, ContentOrigin origin)
    {
        writer.WriteStartObject(propertyName);
        writer.WriteString("kind", origin.Kind switch
        {
            ContentOriginKind.Synthetic => "synthetic",
            ContentOriginKind.SourceDerived => "source-derived",
            _ => throw new JsonException("Unsupported Content origin kind."),
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

    private static void WriteEmptyArray(Utf8JsonWriter writer, string name)
    {
        writer.WriteStartArray(name);
        writer.WriteEndArray();
    }
}
