using System.Text;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatBreakdownCompletionCodec
{
    internal static readonly string ActionId = CampaignOpeningPreambleCodec.Hash("{\"contractVersion\":1,\"kind\":\"complete-breakdown-segment\",\"operationStage\":1}"u8);
    public static byte[] SerializeInput(CampaignCombatBreakdownCompletionInput input)
    {
        try
        {
            if (input?.Command is not { } command || command.ContractVersion != 2 || command.Kind != "complete-breakdown-segment" ||
                command.OperationStage != 1 || command.ExpectedPriorVersion < 1 || !Enum.IsDefined(input.Actor))
                throw new JsonException("Unsupported Breakdown completion input identity.");
            _ = ContentContractGuards.RequireSourceAtom(command.CreationBinding, nameof(input));
            _ = ContentContractGuards.RequireSourceAtom(command.ExpectedPositionId, nameof(input));
            foreach (var hash in new[] { command.ActionId, command.CreationEventHash, command.CycleId })
                _ = ContentContractGuards.RequireSha256(hash, nameof(input));
        }
        catch (ArgumentException error) { throw new JsonException("Invalid Breakdown completion primitive.", error); }
        return Bytes(writer => WriteInput(writer, input));
    }
    public static CampaignCombatBreakdownCompletionInput DeserializeInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes); var root = document.RootElement; var cmd = root.GetProperty("command");
            var input = new CampaignCombatBreakdownCompletionInput(new(cmd.GetProperty("contractVersion").GetInt32(), cmd.GetProperty("kind").GetString()!,
                cmd.GetProperty("operationStage").GetInt32(), cmd.GetProperty("actionId").GetString()!, cmd.GetProperty("creationBinding").GetString()!,
                cmd.GetProperty("creationEventHash").GetString()!, cmd.GetProperty("cycleId").GetString()!, cmd.GetProperty("expectedPriorVersion").GetInt64(),
                cmd.GetProperty("expectedPositionId").GetString()!), root.GetProperty("actor").GetString() switch
                {
                    "system" => CampaignOpeningPreambleActor.System,
                    "axis" => CampaignOpeningPreambleActor.Axis,
                    "commonwealth" => CampaignOpeningPreambleActor.Commonwealth,
                    _ => throw new JsonException("Unknown Breakdown completion actor."),
                });
            if (!bytes.SequenceEqual(SerializeInput(input))) throw new JsonException("Noncanonical Breakdown completion input.");
            return input;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { throw new JsonException("Invalid Breakdown completion input.", error); }
    }
    internal static CampaignCombatBreakdownCompletionInput ReadEventInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes); var root = document.RootElement;
            if (root.GetProperty("contractVersion").GetInt32() != 2 || root.GetProperty("eventType").GetString() != "breakdown-segment-completed")
                throw new JsonException("Unsupported Breakdown completion event.");
            return DeserializeInput(Encoding.UTF8.GetBytes(root.GetProperty("input").GetRawText()));
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { throw new JsonException("Invalid Breakdown completion event.", error); }
    }
    internal static byte[] SerializeEvent(CampaignCombatBreakdownCompletionEvent value, bool includeReceipt = true) => Bytes(writer =>
    {
        var state = value.Before; var opening = state.Movement.Opening; var cycle = opening.Cycle!;
        var creation = opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", 2); writer.WriteString("eventType", "breakdown-segment-completed");
        writer.WriteString("campaignId", cycle.CampaignId); writer.WriteNumber("stateVersion", value.StateVersion); writer.WriteNumber("priorStateVersion", state.StateVersion);
        writer.WriteString("rulesetHash", cycle.RulesetHash); writer.WriteString("fromPositionId", state.SequencePosition.PositionId);
        writer.WriteString("actionId", value.Input.Command.ActionId); CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", value.SequencePosition);
        writer.WritePropertyName("breakdownFlowAfter"); CampaignBreakdownCodec.WriteFlow(writer, state.BreakdownFlow);
        // Sources belong to the completed Breakdown position, not its Combat successor.
        CampaignSnapshotSerializer.WriteSources(writer, state.SequencePosition.Sources);
        writer.WriteString("configurationHash", cycle.AdmittedPolicyBundleDigest); writer.WriteString("creationBinding", creation.CreationBinding);
        writer.WriteString("creationEventHash", creation.CreationEventHash); writer.WriteString("cycleId", opening.CycleId);
        writer.WriteString("openingBaseHash", opening.OpeningBaseHash); writer.WriteString("completionReceiptId", opening.CompletionReceiptId);
        writer.WriteString("movementCompletionReceiptId", state.MovementEnd!.CompletionReceiptId); writer.WriteString("priorPrefix", state.Prefix);
        writer.WritePropertyName("input"); WriteInput(writer, value.Input);
        if (includeReceipt) writer.WriteString("receiptId", value.ReceiptId);
        writer.WriteEndObject();
    });
    private static void WriteInput(Utf8JsonWriter writer, CampaignCombatBreakdownCompletionInput input)
    {
        var cmd = input.Command;
        writer.WriteStartObject(); writer.WriteStartObject("command"); writer.WriteNumber("contractVersion", cmd.ContractVersion); writer.WriteString("kind", cmd.Kind);
        writer.WriteNumber("operationStage", cmd.OperationStage); writer.WriteString("actionId", cmd.ActionId); writer.WriteString("creationBinding", cmd.CreationBinding);
        writer.WriteString("creationEventHash", cmd.CreationEventHash); writer.WriteString("cycleId", cmd.CycleId);
        writer.WriteNumber("expectedPriorVersion", cmd.ExpectedPriorVersion); writer.WriteString("expectedPositionId", cmd.ExpectedPositionId);
        writer.WriteEndObject(); writer.WriteString("actor", Actor(input.Actor)); writer.WriteEndObject();
    }
    private static JsonDocument Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized Breakdown completion bytes.");
        return JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
    }
    public static byte[] SerializeState(CampaignCombatBreakdownCompletionState state) => Bytes(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 1);
        WriteAuthority(writer, state.Lifecycle.Movement.Opening.Predecessor);
        writer.WriteNumber("stateVersion", state.StateVersion);
        writer.WriteString("prefix", state.Prefix);
        CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", state.SequencePosition);
        writer.WriteString("initiativeHolder", CampaignSnapshotSerializer.FormatSide(state.Lifecycle.Movement.Opening.Predecessor.Stage.Weather.Opening.InitiativeHolder!.Value));
        writer.WriteStartArray("operationStageOrders");
        foreach (var order in state.Lifecycle.Movement.Opening.Predecessor.Stage.Weather.Opening.Orders)
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
        CampaignOperationStageWeatherCodec.Write(writer, state.Lifecycle.Movement.Opening.Predecessor.Stage.Weather.Weather);
        writer.WritePropertyName("world");
        writer.WriteRawValue(CampaignCombatInheritedMovementCodec.WriteWorld(state.Lifecycle.Movement));
        CampaignSnapshotSerializer.WriteRandomState(writer, state.Lifecycle.Movement.Opening.Predecessor.Stage.Weather.RandomState);
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
        writer.WriteString("firstActingSide", CampaignSnapshotSerializer.FormatSide(state.Lifecycle.Movement.Opening.Predecessor.FirstActingSide));
        CampaignCombatReserveCodec.WriteMembers(writer, state.Lifecycle.Movement.Members);
        if (state.Lifecycle.Movement.Opening.Cycle is null) writer.WriteNull("cycle");
        else
        {
            writer.WritePropertyName("cycle");
            CampaignCombatReserveCompletionCodec.WriteCycle(writer, state.Lifecycle.Movement.Opening.Cycle);
        }
        writer.WriteString("cycleId", state.Lifecycle.Movement.Opening.CycleId);
        writer.WriteString("openingBaseHash", state.Lifecycle.Movement.Opening.OpeningBaseHash);
        writer.WriteString("completionReceiptId", state.Lifecycle.Movement.Opening.CompletionReceiptId);
        writer.WriteStartArray("tracks");
        foreach (var track in state.Lifecycle.Movement.Tracks)
        {
            writer.WriteStartObject(); WriteUnit(writer, track.Unit);
            writer.WriteStartArray("route");
            foreach (var location in track.Route) writer.WriteStringValue(location);
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteStartArray("actualProgressRefs");
        foreach (var reference in state.Lifecycle.Movement.ActualProgressRefs)
        {
            writer.WriteStartObject(); writer.WriteString("eventType", reference.EventType);
            writer.WriteString("receiptId", reference.ReceiptId); writer.WriteString("eventHash", reference.EventHash);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WritePropertyName("breakdownFlow");
        if (state.Lifecycle.BreakdownFlow is null) writer.WriteNullValue();
        else CampaignBreakdownCodec.WriteFlow(writer, state.Lifecycle.BreakdownFlow);
        WriteInterrupt(writer, "interruptContext", state.Lifecycle.InterruptContext);
        writer.WritePropertyName("movementEnd");
        if (state.Lifecycle.MovementEnd is null) writer.WriteNullValue();
        else WriteProof(writer, state.Lifecycle.MovementEnd);
        writer.WriteString("breakdownCompletionReceiptId", state.BreakdownCompletionReceiptId);
        writer.WriteEndObject();
    });

    private static void WriteInterrupt(Utf8JsonWriter writer, string name, CampaignCombatMovementInterrupt? value)
    {
        writer.WritePropertyName(name);
        if (value is null) { writer.WriteNullValue(); return; }
        writer.WriteStartObject(); writer.WritePropertyName("cycle"); CampaignCombatReserveCompletionCodec.WriteCycle(writer, value.Cycle);
        writer.WriteString("cycleId", value.CycleId); CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", value.SequencePosition); writer.WriteEndObject();
    }
    private static void WriteProof(Utf8JsonWriter writer, CampaignCombatMovementEndProof proof)
    {
        writer.WriteStartObject(); writer.WriteStartObject("scope"); writer.WriteNumber("gameTurn", proof.Scope.GameTurn);
        writer.WriteNumber("operationStage", proof.Scope.OperationStage); writer.WriteString("playerPhaseSlot", proof.Scope.PlayerPhaseSlot);
        writer.WriteString("actingSide", CampaignSnapshotSerializer.FormatSide(proof.Scope.ActingSide)); writer.WriteEndObject();
        writer.WriteNumber("ordinal", proof.Ordinal); writer.WriteString("completionReceiptId", proof.CompletionReceiptId);
        WriteLocations(writer, proof.EndLocations); WriteUnits(writer, "excludedBefore", proof.ExcludedBefore); writer.WriteEndObject();
    }
    private static void WriteLocations(Utf8JsonWriter writer, IReadOnlyList<CampaignCombatEndLocation> locations)
    {
        writer.WriteStartArray("endLocations");
        foreach (var location in locations) { writer.WriteStartObject(); WriteUnit(writer, location.Unit); writer.WriteString("locationId", location.LocationId); writer.WriteEndObject(); }
        writer.WriteEndArray();
    }
    private static void WriteUnits(Utf8JsonWriter writer, string name, IReadOnlyList<CampaignCombatUnitKey> units)
    { writer.WriteStartArray(name); foreach (var unit in units) WriteUnit(writer, unit, false); writer.WriteEndArray(); }
    private static void WriteUnit(Utf8JsonWriter writer, CampaignCombatUnitKey unit, bool property = true)
    {
        if (property) writer.WritePropertyName("unit"); writer.WriteStartObject(); writer.WriteString("creationBinding", unit.CreationBinding);
        writer.WriteString("originalSide", unit.OriginalSide); writer.WriteString("elementId", unit.ElementId); writer.WriteEndObject();
    }
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
        if (stream.Length > 1_048_576) throw new JsonException("Breakdown completion record exceeds byte limit.");
        return stream.ToArray();
    }
    private static string Actor(CampaignOpeningPreambleActor actor) => actor switch
    {
        CampaignOpeningPreambleActor.System => "system",
        CampaignOpeningPreambleActor.Axis => "axis",
        CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
        _ => throw new JsonException("Unknown Breakdown completion actor."),
    };
}
