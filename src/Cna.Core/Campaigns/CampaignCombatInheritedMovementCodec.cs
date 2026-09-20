using System.Text;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatInheritedMovementCodec
{
    public static byte[] SerializeInput(CampaignCombatInheritedMovementInput input)
    {
        try
        {
            if (input is null || input.Command is null || input.Command.Unit is null || input.Command.ContractVersion != 2 ||
                input.Command.Kind != "move-element" || input.Command.ExpectedPriorVersion < 1 || !Enum.IsDefined(input.Actor))
                throw new JsonException("Unsupported Move4 input identity.");
            foreach (var id in new[] { input.Command.CreationBinding, input.Command.ExpectedPositionId, input.Command.OriginLocationId,
                input.Command.DestinationLocationId, input.Command.Unit.CreationBinding, input.Command.Unit.ElementId })
                _ = ContentContractGuards.RequireSourceAtom(id, nameof(input));
            _ = ContentContractGuards.RequireSha256(input.Command.CreationEventHash, nameof(input));
            _ = ContentContractGuards.RequireSha256(input.Command.CycleId, nameof(input));
        }
        catch (ArgumentException error) { throw new JsonException("Invalid Move4 primitive.", error); }
        return Bytes(writer => WriteInput(writer, input));
    }
    public static CampaignCombatInheritedMovementInput DeserializeInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes);
            var root = document.RootElement; var cmd = root.GetProperty("command"); var unit = cmd.GetProperty("unit");
            var input = new CampaignCombatInheritedMovementInput(new(cmd.GetProperty("contractVersion").GetInt32(),
                cmd.GetProperty("kind").GetString()!, cmd.GetProperty("creationBinding").GetString()!, cmd.GetProperty("creationEventHash").GetString()!,
                cmd.GetProperty("cycleId").GetString()!, cmd.GetProperty("expectedPriorVersion").GetInt64(), cmd.GetProperty("expectedPositionId").GetString()!,
                new(unit.GetProperty("creationBinding").GetString()!, unit.GetProperty("originalSide").GetString()!, unit.GetProperty("elementId").GetString()!),
                cmd.GetProperty("originLocationId").GetString()!, cmd.GetProperty("destinationLocationId").GetString()!), root.GetProperty("actor").GetString() switch
                {
                    "system" => CampaignOpeningPreambleActor.System,
                    "axis" => CampaignOpeningPreambleActor.Axis,
                    "commonwealth" => CampaignOpeningPreambleActor.Commonwealth,
                    _ => throw new JsonException("Unknown movement actor."),
                });
            if (!bytes.SequenceEqual(SerializeInput(input))) throw new JsonException("Noncanonical Move4 input.");
            return input;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or OverflowException or FormatException)
        { throw new JsonException("Invalid Move4 input.", error); }
    }
    internal static CampaignCombatInheritedMovementInput ReadEventInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes); var root = document.RootElement;
            if (root.GetProperty("contractVersion").GetInt32() != 4 || root.GetProperty("eventType").GetString() != "element-moved")
                throw new JsonException("Unsupported inherited movement event.");
            return DeserializeInput(Encoding.UTF8.GetBytes(root.GetProperty("input").GetRawText()));
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or OverflowException or FormatException)
        { throw new JsonException("Invalid Move4 event.", error); }
    }
    internal static byte[] SerializeEvent(CampaignCombatInheritedMovementEvent value, bool includeReceipt = true) => Bytes(writer =>
    {
        var state = value.Before; var cmd = value.Input.Command; var creation = state.Opening.Predecessor.Stage.Weather.Opening.Creation;
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", 4); writer.WriteString("eventType", "element-moved");
        writer.WriteString("campaignId", creation.CampaignId); writer.WriteNumber("stateVersion", value.StateVersion);
        writer.WriteNumber("priorStateVersion", state.StateVersion); writer.WriteString("fromPositionId", state.SequencePosition.PositionId);
        writer.WriteNumber("gameTurn", 1); writer.WriteNumber("operationStage", 1);
        writer.WriteString("actingSide", cmd.Unit.OriginalSide); writer.WriteString("elementId", cmd.Unit.ElementId);
        writer.WriteString("representationId", value.RepresentationId); writer.WriteString("originLocationId", cmd.OriginLocationId);
        writer.WriteString("destinationLocationId", cmd.DestinationLocationId); writer.WriteString("mobilityId", Cna1979Movement.NonMotorizedMobilityId);
        WriteReferences(writer, "mobilitySources", value.Sources);
        writer.WriteStartObject("cost"); writer.WriteString("destinationTerrainId", "land.terrain.clear");
        Cp(writer, "destinationTerrainCost", value.Cost); WriteReferences(writer, "destinationTerrainSources", value.Sources);
        writer.WriteNull("routeAdjustment"); WriteEmptyArray(writer, "crossedHexsideCosts"); Cp(writer, "totalCost", value.Cost); writer.WriteEndObject();
        Cp(writer, "capabilityPointsExpendedBefore", value.Element.OperationalState.CapabilityPointsExpended);
        Cp(writer, "capabilityPointsExpendedAfter", value.AfterCp);
        writer.WriteNumber("cohesionBefore", value.Element.OperationalState.CohesionLevel); writer.WriteNumber("cohesionAfter", value.AfterCohesion);
        writer.WriteNull("movementEndedAfter"); CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", state.SequencePosition);
        writer.WriteNull("openedReactionWindow"); writer.WriteString("rulesetHash", creation.RulesetHash);
        WriteEmptyArray(writer, "breakdownAccounting"); writer.WritePropertyName("breakdownFlowAfter"); CampaignBreakdownCodec.WriteFlow(writer, value.Flow);
        writer.WriteString("configurationHash", creation.Configuration.Hash); writer.WriteString("creationBinding", creation.CreationReceipt.CreationBinding);
        writer.WriteString("creationEventHash", creation.CreationReceipt.CreationEventHash); writer.WriteString("cycleId", state.Opening.CycleId);
        writer.WriteString("openingBaseHash", state.Opening.OpeningBaseHash); writer.WriteString("completionReceiptId", state.Opening.CompletionReceiptId);
        writer.WriteString("priorPrefix", state.Prefix); writer.WritePropertyName("input"); WriteInput(writer, value.Input);
        if (includeReceipt) writer.WriteString("receiptId", value.ReceiptId);
        writer.WriteEndObject();
    });
    private static void WriteInput(Utf8JsonWriter writer, CampaignCombatInheritedMovementInput input)
    {
        var cmd = input.Command; writer.WriteStartObject(); writer.WriteStartObject("command");
        writer.WriteNumber("contractVersion", cmd.ContractVersion); writer.WriteString("kind", cmd.Kind);
        writer.WriteString("creationBinding", cmd.CreationBinding); writer.WriteString("creationEventHash", cmd.CreationEventHash);
        writer.WriteString("cycleId", cmd.CycleId); writer.WriteNumber("expectedPriorVersion", cmd.ExpectedPriorVersion);
        writer.WriteString("expectedPositionId", cmd.ExpectedPositionId); WriteUnit(writer, cmd.Unit);
        writer.WriteString("originLocationId", cmd.OriginLocationId); writer.WriteString("destinationLocationId", cmd.DestinationLocationId);
        writer.WriteEndObject(); writer.WriteString("actor", Actor(input.Actor)); writer.WriteEndObject();
    }
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
    private static JsonDocument Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized Move4 bytes.");
        return JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
    }
    public static byte[] SerializeState(CampaignCombatInheritedMovementState state) => Bytes(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 1);
        WriteAuthority(writer, state.Opening.Predecessor);
        writer.WriteNumber("stateVersion", state.StateVersion);
        writer.WriteString("prefix", state.Prefix);
        CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", state.SequencePosition);
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
        CampaignCombatReserveCodec.WriteMembers(writer, state.Members);
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
        writer.WritePropertyName("breakdownFlow");
        if (state.BreakdownFlow is null) writer.WriteNullValue();
        else CampaignBreakdownCodec.WriteFlow(writer, state.BreakdownFlow);
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
        if (stream.Length > 1_048_576) throw new JsonException("Inherited movement record exceeds byte limit.");
        return stream.ToArray();
    }
    private static string Actor(CampaignOpeningPreambleActor actor) => actor switch
    {
        CampaignOpeningPreambleActor.System => "system",
        CampaignOpeningPreambleActor.Axis => "axis",
        CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
        _ => throw new JsonException("Unknown Inherited movement actor."),
    };
    internal static byte[] WriteWorld(CampaignCombatInheritedMovementState state)
    {
        // This writer handles only the history-derived ordinary-movement World profile.
        // Equality protects hardcoded absent fields; it never admits an arbitrary World7.
        var world = CampaignCombatInheritedMovement.ExpectedWorld(state);
        if (state.World != world) throw new JsonException("Movement World differs from history-derived ordinary moves.");
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
