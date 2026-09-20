using System.Text;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatReactionFallbackCodec
{
    public static byte[] SerializeInput(CampaignCombatReactionFallbackInput input)
    {
        try
        {
            if (input?.Command?.Identity is not { } id || id.ContractVersion != 2 || id.ExpectedPriorVersion < 1 || !Enum.IsDefined(input.Actor))
                throw new JsonException("Unsupported fallback input identity.");
            if (input.Command.Kind != "resolve-breakdown-stop") _ = input.Command.Reason;
            _ = ContentContractGuards.RequireSourceAtom(id.CreationBinding, nameof(input));
            _ = ContentContractGuards.RequireSourceAtom(id.ExpectedPositionId, nameof(input));
            foreach (var hash in new[] { id.ActionId, id.CreationEventHash, id.CycleId, input.Command.Handle })
                _ = ContentContractGuards.RequireSha256(hash, nameof(input));
        }
        catch (ArgumentException error) { throw new JsonException("Invalid fallback primitive.", error); }
        return Bytes(writer => WriteInput(writer, input));
    }
    public static CampaignCombatReactionFallbackInput DeserializeInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes); var root = document.RootElement; var cmd = root.GetProperty("command");
            var id = new CampaignCombatReactionFallbackIdentity(cmd.GetProperty("contractVersion").GetInt32(), cmd.GetProperty("actionId").GetString()!,
                cmd.GetProperty("creationBinding").GetString()!, cmd.GetProperty("creationEventHash").GetString()!, cmd.GetProperty("cycleId").GetString()!,
                cmd.GetProperty("expectedPriorVersion").GetInt64(), cmd.GetProperty("expectedPositionId").GetString()!);
            var command = new CampaignCombatReactionFallbackCommand(id, cmd.GetProperty("kind").GetString()!, cmd.GetProperty(cmd.GetProperty("kind").GetString() == "resolve-breakdown-stop" ? "stopId" : "windowId").GetString()!);
            var input = new CampaignCombatReactionFallbackInput(command, root.GetProperty("actor").GetString() switch
            {
                "system" => CampaignOpeningPreambleActor.System,
                "axis" => CampaignOpeningPreambleActor.Axis,
                "commonwealth" => CampaignOpeningPreambleActor.Commonwealth,
                _ => throw new JsonException("Unknown fallback actor."),
            });
            if (!bytes.SequenceEqual(SerializeInput(input))) throw new JsonException("Noncanonical fallback input.");
            return input;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { throw new JsonException("Invalid fallback input.", error); }
    }
    internal static CampaignCombatReactionFallbackInput ReadEventInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes); var root = document.RootElement;
            if (!((root.GetProperty("contractVersion").GetInt32() == 3 && root.GetProperty("eventType").GetString() == "reaction-window-closed") ||
                (root.GetProperty("contractVersion").GetInt32() == 2 && root.GetProperty("eventType").GetString() == "breakdown-stop-resolved")))
                throw new JsonException("Unsupported active fallback event.");
            return DeserializeInput(Encoding.UTF8.GetBytes(root.GetProperty("input").GetRawText()));
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { throw new JsonException("Invalid fallback event.", error); }
    }
    private static JsonDocument Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized fallback bytes.");
        var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
        try { CheckArrays(document.RootElement); return document; }
        catch { document.Dispose(); throw; }
    }
    private static void CheckArrays(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            if (value.GetArrayLength() > 512) throw new JsonException("Fallback array exceeds item bound.");
            foreach (var item in value.EnumerateArray()) CheckArrays(item);
        }
        else if (value.ValueKind == JsonValueKind.Object)
            foreach (var item in value.EnumerateObject()) CheckArrays(item.Value);
    }
    private static void WriteInput(Utf8JsonWriter writer, CampaignCombatReactionFallbackInput input)
    {
        var cmd = input.Command; var id = cmd.Identity;
        writer.WriteStartObject(); writer.WriteStartObject("command"); writer.WriteNumber("contractVersion", id.ContractVersion); writer.WriteString("kind", cmd.Kind);
        writer.WriteString(cmd.Kind == "resolve-breakdown-stop" ? "stopId" : "windowId", cmd.Handle); writer.WriteString("actionId", id.ActionId);
        writer.WriteString("creationBinding", id.CreationBinding); writer.WriteString("creationEventHash", id.CreationEventHash);
        writer.WriteString("cycleId", id.CycleId); writer.WriteNumber("expectedPriorVersion", id.ExpectedPriorVersion); writer.WriteString("expectedPositionId", id.ExpectedPositionId);
        writer.WriteEndObject(); writer.WriteString("actor", Actor(input.Actor)); writer.WriteEndObject();
    }
    internal static byte[] SerializeEvent(CampaignCombatReactionFallbackEvent value, bool includeReceipt = true) => Bytes(writer =>
    {
        var state = value.Before; var opening = state.Opening; var cycle = opening.Cycle!; var command = value.Input.Command;
        var resolve = command.Kind == "resolve-breakdown-stop";
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", resolve ? 2 : 3); writer.WriteString("eventType", resolve ? "breakdown-stop-resolved" : "reaction-window-closed");
        writer.WriteString("campaignId", cycle.CampaignId); writer.WriteNumber("stateVersion", value.StateVersion); writer.WriteNumber("priorStateVersion", state.StateVersion);
        if (resolve) writer.WriteString("rulesetHash", cycle.RulesetHash);
        writer.WriteString("fromPositionId", resolve ? "land.position.breakdown-stop" : state.SequencePosition.PositionId);
        if (!resolve) writer.WriteNull("actingSide");
        writer.WriteString("actionId", command.Identity.ActionId);
        if (resolve)
        {
            writer.WritePropertyName("stop"); CampaignBreakdownCodec.WriteStop(writer, ((CampaignBreakdownFlow.ReactorStopClosed)state.BreakdownFlow).Stop);
            WriteRandom(writer, "randomStateBefore", state);
            writer.WriteStartArray("checks"); writer.WriteEndArray(); writer.WriteStartArray("createdLots"); writer.WriteEndArray();
            WriteRandom(writer, "randomStateAfter", state);
            writer.WritePropertyName("breakdownFlowAfter"); CampaignBreakdownCodec.WriteFlow(writer, value.Flow);
            writer.WriteStartArray("sources"); writer.WriteStartObject(); writer.WriteString("sourceId", "spi-1979-land-rules");
            writer.WriteString("locator", "21.24-21.26"); writer.WriteEndObject(); writer.WriteEndArray();
        }
        else
        {
            var window = state.ReactionWindow!.Trigger;
            writer.WriteString("submittedWindowId", command.Handle); writer.WriteString("windowId", window.WindowId.Value); writer.WriteString("reason", command.Reason);
            writer.WriteStartArray("closedOpportunityIds"); writer.WriteStringValue(state.ReactionWindow.ActiveOpportunityId!.Value); writer.WriteEndArray();
            CampaignV11CanonicalCodec.WritePosition(writer, "suspendedSequencePosition", state.SequencePosition);
            writer.WriteString("rulesetHash", cycle.RulesetHash); writer.WritePropertyName("breakdownFlowAfter"); CampaignBreakdownCodec.WriteFlow(writer, value.Flow);
        }
        var creation = opening.Predecessor.Stage.Weather.Opening.Creation;
        writer.WriteString("configurationHash", creation.Configuration.Hash); writer.WriteString("creationBinding", creation.CreationReceipt.CreationBinding);
        writer.WriteString("creationEventHash", creation.CreationReceipt.CreationEventHash); writer.WriteString("cycleId", opening.CycleId);
        writer.WriteString("openingBaseHash", opening.OpeningBaseHash); writer.WriteString("completionReceiptId", opening.CompletionReceiptId);
        writer.WriteString("priorPrefix", state.Prefix); writer.WritePropertyName("input"); WriteInput(writer, value.Input);
        if (resolve) { CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", state.SequencePosition); writer.WriteNull("interruptContextAfter"); }
        if (includeReceipt) writer.WriteString("receiptId", value.ReceiptId);
        writer.WriteEndObject();
    });
    public static byte[] SerializeState(CampaignCombatReactionFallbackState state) => Bytes(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 1);
        WriteAuthority(writer, state.Opening.Predecessor);
        writer.WriteNumber("stateVersion", state.StateVersion);
        writer.WriteString("prefix", state.Prefix);
        CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", state.SequencePosition);
        writer.WriteStartObject("currentPosition");
        if (state.BreakdownFlow is CampaignBreakdownFlow.ReactorStopClosed)
        { writer.WriteString("kind", "breakdown-stop"); CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", state.SequencePosition); }
        else if (state.ReactionWindow is null)
        { writer.WriteString("kind", "sequence"); CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", state.SequencePosition); }
        else
        { writer.WriteString("kind", "reaction"); CampaignCombatReactionLifecycleCodec.WriteReactingPosition(writer, "reactingPosition", state.ReactionWindow.Trigger.ReactingPosition); }
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
        writer.WriteRawValue(CampaignCombatReactionLifecycleCodec.WriteWorld(state.Predecessor));
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
        if (state.ReactionWindow is null) writer.WriteNull("reactionWindow");
        else CampaignCombatReactionLifecycleCodec.WriteWindow(writer, "reactionWindow", state.ReactionWindow);
        writer.WritePropertyName("breakdownFlow");
        if (state.BreakdownFlow is null) writer.WriteNullValue();
        else CampaignBreakdownCodec.WriteFlow(writer, state.BreakdownFlow);
        writer.WriteEndObject();
    });

    internal static string StopCapability(CampaignCombatReactionFallbackState state) => CampaignOpeningPreambleCodec.Hash(Bytes(writer =>
    {
        var cycle = state.Opening.Cycle!; writer.WriteStartObject(); writer.WriteString("domain", "sandtable.action.breakdown-stop.v1");
        writer.WriteString("campaignId", cycle.CampaignId); writer.WriteString("rulesetHash", cycle.RulesetHash);
        writer.WriteNumber("stateVersion", state.StateVersion); writer.WriteString("audience", "system"); writer.WriteEndObject();
    }));
    private static void WriteRandom(Utf8JsonWriter writer, string name, CampaignCombatReactionFallbackState state)
    {
        var random = state.Opening.Predecessor.Stage.Weather.RandomState;
        writer.WriteStartObject(name); writer.WriteNumber("contractVersion", random.ContractVersion); writer.WriteString("algorithmId", random.AlgorithmId);
        writer.WriteNumber("seed", random.Seed); writer.WriteNumber("nextByteCursor", random.NextByteCursor); writer.WriteEndObject();
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
        if (stream.Length > 1_048_576) throw new JsonException("Reaction fallback record exceeds byte limit.");
        return stream.ToArray();
    }
    private static string Actor(CampaignOpeningPreambleActor actor) => actor switch
    {
        CampaignOpeningPreambleActor.System => "system",
        CampaignOpeningPreambleActor.Axis => "axis",
        CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
        _ => throw new JsonException("Unknown Reaction fallback actor."),
    };
    private static void WriteUnit(Utf8JsonWriter writer, CampaignCombatUnitKey unit)
    {
        writer.WriteStartObject("unit"); writer.WriteString("creationBinding", unit.CreationBinding);
        writer.WriteString("originalSide", unit.OriginalSide); writer.WriteString("elementId", unit.ElementId); writer.WriteEndObject();
    }
}
