using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatReactionCompletionCodec
{
    public static byte[] SerializeInput(CampaignCombatReactionLifecycleInput input)
    {
        if (input?.Command is not (CampaignCombatReactionLifecycleCommand.Complete or CampaignCombatReactionLifecycleCommand.Resolve or CampaignCombatReactionLifecycleCommand.Close))
            throw new JsonException("Unsupported completion input.");
        return CampaignCombatReactionLifecycleCodec.SerializeInput(input);
    }
    public static CampaignCombatReactionLifecycleInput DeserializeInput(ReadOnlySpan<byte> bytes)
    {
        var input = CampaignCombatReactionLifecycleCodec.DeserializeInput(bytes);
        _ = SerializeInput(input); return input;
    }
    internal static CampaignCombatReactionLifecycleInput ReadEventInput(ReadOnlySpan<byte> bytes)
    {
        var input = CampaignCombatReactionLifecycleCodec.ReadEventInput(bytes);
        _ = SerializeInput(input); return input;
    }
    internal static string WindowCapability(CampaignCombatReactionCompletionState state) =>
        CampaignCombatReactionLifecycleCodec.WindowCapability(state.Predecessor.Predecessor);
    internal static string StopCapability(CampaignCombatReactionCompletionState state) => CampaignOpeningPreambleCodec.Hash(Bytes(writer =>
    {
        var cycle = state.Opening.Cycle!; writer.WriteStartObject(); writer.WriteString("domain", "sandtable.action.breakdown-stop.v1");
        writer.WriteString("campaignId", cycle.CampaignId); writer.WriteString("rulesetHash", cycle.RulesetHash);
        writer.WriteNumber("stateVersion", state.StateVersion); writer.WriteString("audience", "system"); writer.WriteEndObject();
    }));
    internal static byte[] SerializeEvent(CampaignCombatReactionCompletionEvent value, bool includeReceipt = true) => Bytes(writer =>
    {
        var state = value.Before; var opening = state.Opening; var cycle = opening.Cycle!; var command = value.Input.Command;
        var window = state.ReactionWindow!.Trigger; var opportunity = window.FrozenOpportunities.Single();
        var (version, type) = command switch
        {
            CampaignCombatReactionLifecycleCommand.Complete => (3, "reaction-participant-completed"),
            CampaignCombatReactionLifecycleCommand.Resolve => (2, "breakdown-stop-resolved"),
            CampaignCombatReactionLifecycleCommand.Close => (3, "reaction-window-closed"),
            _ => throw new JsonException("Unsupported completion event."),
        };
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", version); writer.WriteString("eventType", type);
        writer.WriteString("campaignId", cycle.CampaignId); writer.WriteNumber("stateVersion", value.StateVersion); writer.WriteNumber("priorStateVersion", state.StateVersion);
        if (command is CampaignCombatReactionLifecycleCommand.Resolve) writer.WriteString("rulesetHash", cycle.RulesetHash);
        writer.WriteString("fromPositionId", command is CampaignCombatReactionLifecycleCommand.Resolve ? "land.position.breakdown-stop" : state.SequencePosition.PositionId);
        if (command is not CampaignCombatReactionLifecycleCommand.Resolve)
            writer.WriteString("actingSide", command is CampaignCombatReactionLifecycleCommand.Close ? null : CampaignSnapshotSerializer.FormatSide(window.ReactingSide));
        writer.WriteString("actionId", command.Identity.ActionId);
        switch (command)
        {
            case CampaignCombatReactionLifecycleCommand.Complete complete:
                WriteHandles(writer, complete.WindowId, complete.OpportunityId, window, opportunity.OpportunityId);
                CampaignCombatReactionLifecycleCodec.WriteWindow(writer, "reactionWindowAfter", value.Window!); writer.WriteString("rulesetHash", cycle.RulesetHash); WriteFlow(writer, value.Flow); break;
            case CampaignCombatReactionLifecycleCommand.Resolve:
                writer.WritePropertyName("stop"); CampaignBreakdownCodec.WriteStop(writer, ((CampaignBreakdownFlow.ReactorStopOpen)state.BreakdownFlow).Stop);
                WriteRandom(writer, "randomStateBefore", state); WriteEmptyArray(writer, "checks"); WriteEmptyArray(writer, "createdLots"); WriteRandom(writer, "randomStateAfter", state);
                WriteFlow(writer, value.Flow); WriteReferences(writer, "sources", [new("spi-1979-land-rules", "21.24-21.26")]); break;
            case CampaignCombatReactionLifecycleCommand.Close close:
                writer.WriteString("submittedWindowId", close.WindowId); writer.WriteString("windowId", window.WindowId.Value);
                writer.WriteString("reason", "no-eligible-reactor"); WriteEmptyArray(writer, "closedOpportunityIds");
                CampaignV11CanonicalCodec.WritePosition(writer, "suspendedSequencePosition", state.SequencePosition);
                writer.WriteString("rulesetHash", cycle.RulesetHash); WriteFlow(writer, value.Flow); break;
        }
        var creation = opening.Predecessor.Stage.Weather.Opening.Creation;
        writer.WriteString("configurationHash", creation.Configuration.Hash); writer.WriteString("creationBinding", creation.CreationReceipt.CreationBinding);
        writer.WriteString("creationEventHash", creation.CreationReceipt.CreationEventHash); writer.WriteString("cycleId", opening.CycleId);
        writer.WriteString("openingBaseHash", opening.OpeningBaseHash); writer.WriteString("completionReceiptId", opening.CompletionReceiptId);
        writer.WriteString("priorPrefix", state.Prefix); writer.WritePropertyName("input"); writer.WriteRawValue(SerializeInput(value.Input));
        if (command is CampaignCombatReactionLifecycleCommand.Resolve)
        { CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", state.SequencePosition); writer.WriteNull("interruptContextAfter"); }
        if (includeReceipt) writer.WriteString("receiptId", value.ReceiptId);
        writer.WriteEndObject();
    });
    private static void WriteHandles(Utf8JsonWriter writer, string windowId, string opportunityId, CampaignCombatReactionWindow window, CampaignReactionOpportunityId opportunity)
    {
        writer.WriteString("submittedWindowId", windowId); writer.WriteString("submittedOpportunityId", opportunityId);
        writer.WriteString("windowId", window.WindowId.Value); writer.WriteString("opportunityId", opportunity.Value);
    }
    private static void WriteRandom(Utf8JsonWriter writer, string name, CampaignCombatReactionCompletionState state)
    {
        var random = state.Opening.Predecessor.Stage.Weather.RandomState;
        writer.WriteStartObject(name); writer.WriteNumber("contractVersion", random.ContractVersion); writer.WriteString("algorithmId", random.AlgorithmId);
        writer.WriteNumber("seed", random.Seed); writer.WriteNumber("nextByteCursor", random.NextByteCursor); writer.WriteEndObject();
    }
    private static void WriteFlow(Utf8JsonWriter writer, CampaignBreakdownFlow flow)
    { writer.WritePropertyName("breakdownFlowAfter"); CampaignBreakdownCodec.WriteFlow(writer, flow); }
    public static byte[] SerializeState(CampaignCombatReactionCompletionState state) => Bytes(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 1);
        WriteAuthority(writer, state.Opening.Predecessor);
        writer.WriteNumber("stateVersion", state.StateVersion);
        writer.WriteString("prefix", state.Prefix);
        CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", state.SequencePosition);
        writer.WriteStartObject("currentPosition");
        if (state.BreakdownFlow is CampaignBreakdownFlow.ReactorStopOpen)
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
        CampaignCombatReserveCodec.WriteMembers(writer, state.Predecessor.Predecessor.Trigger.Members);
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
        if (stream.Length > 1_048_576) throw new JsonException("Reaction completion record exceeds byte limit.");
        return stream.ToArray();
    }
    private static string Actor(CampaignOpeningPreambleActor actor) => actor switch
    {
        CampaignOpeningPreambleActor.System => "system",
        CampaignOpeningPreambleActor.Axis => "axis",
        CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
        _ => throw new JsonException("Unknown Reaction completion actor."),
    };
    internal static byte[] WriteWorld(CampaignCombatReactionCompletionState state)
    {
        // F6 never changes World. Preserve F5's complete typed guard before its bounded writer.
        var bytes = CampaignCombatReactionSecondMoveCodec.WriteWorld(state.Predecessor);
        if (state.World != state.Predecessor.World) throw new JsonException("Completion World differs from actual second move.");
        return bytes;
    }
    private static void WriteUnit(Utf8JsonWriter writer, CampaignCombatUnitKey unit)
    {
        writer.WriteStartObject("unit"); writer.WriteString("creationBinding", unit.CreationBinding);
        writer.WriteString("originalSide", unit.OriginalSide); writer.WriteString("elementId", unit.ElementId); writer.WriteEndObject();
    }
    private static void WriteReferences(Utf8JsonWriter writer, string name, IReadOnlyList<RuleReference> references)
    {
        writer.WriteStartArray(name);
        foreach (var reference in references)
        {
            writer.WriteStartObject(); writer.WriteString("sourceId", reference.SourceId); writer.WriteString("locator", reference.Locator); writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
    private static void WriteEmptyArray(Utf8JsonWriter writer, string name)
    { writer.WriteStartArray(name); writer.WriteEndArray(); }
}
