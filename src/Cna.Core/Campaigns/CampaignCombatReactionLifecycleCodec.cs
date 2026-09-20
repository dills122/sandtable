using System.Text;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatReactionLifecycleCodec
{
    public static byte[] SerializeInput(CampaignCombatReactionLifecycleInput input)
    {
        try
        {
            if (input?.Command?.Identity is not { } id || id.ContractVersion != 2 || id.ExpectedPriorVersion < 1 || !Enum.IsDefined(input.Actor))
                throw new JsonException("Unsupported lifecycle input identity.");
            _ = ContentContractGuards.RequireSourceAtom(id.CreationBinding, nameof(input));
            _ = ContentContractGuards.RequireSourceAtom(id.ExpectedPositionId, nameof(input));
            foreach (var hash in new[] { id.ActionId, id.CreationEventHash, id.CycleId }) _ = ContentContractGuards.RequireSha256(hash, nameof(input));
            switch (input.Command)
            {
                case CampaignCombatReactionLifecycleCommand.Move move:
                    _ = ContentContractGuards.RequireSha256(move.WindowId, nameof(input));
                    _ = ContentContractGuards.RequireSha256(move.OpportunityId, nameof(input));
                    _ = ContentContractGuards.RequireStableId(move.OriginLocationId, nameof(input));
                    _ = ContentContractGuards.RequireStableId(move.DestinationLocationId, nameof(input)); break;
                case CampaignCombatReactionLifecycleCommand.Complete complete:
                    _ = ContentContractGuards.RequireSha256(complete.WindowId, nameof(input));
                    _ = ContentContractGuards.RequireSha256(complete.OpportunityId, nameof(input)); break;
                case CampaignCombatReactionLifecycleCommand.Resolve resolve:
                    _ = ContentContractGuards.RequireSha256(resolve.StopId, nameof(input)); break;
                case CampaignCombatReactionLifecycleCommand.Close close:
                    _ = ContentContractGuards.RequireSha256(close.WindowId, nameof(input)); break;
                default: throw new JsonException("Unknown lifecycle command.");
            }
        }
        catch (ArgumentException error) { throw new JsonException("Invalid lifecycle primitive.", error); }
        return Bytes(writer => WriteInput(writer, input));
    }
    public static CampaignCombatReactionLifecycleInput DeserializeInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes); var root = document.RootElement; var cmd = root.GetProperty("command");
            var id = new CampaignCombatReactionLifecycleIdentity(cmd.GetProperty("contractVersion").GetInt32(), cmd.GetProperty("actionId").GetString()!,
                cmd.GetProperty("creationBinding").GetString()!, cmd.GetProperty("creationEventHash").GetString()!, cmd.GetProperty("cycleId").GetString()!,
                cmd.GetProperty("expectedPriorVersion").GetInt64(), cmd.GetProperty("expectedPositionId").GetString()!);
            CampaignCombatReactionLifecycleCommand command = cmd.GetProperty("kind").GetString() switch
            {
                "move-reacting-element" => new CampaignCombatReactionLifecycleCommand.Move(id, cmd.GetProperty("windowId").GetString()!, cmd.GetProperty("opportunityId").GetString()!, cmd.GetProperty("originLocationId").GetString()!, cmd.GetProperty("destinationLocationId").GetString()!),
                "complete-reaction-participant" => new CampaignCombatReactionLifecycleCommand.Complete(id, cmd.GetProperty("windowId").GetString()!, cmd.GetProperty("opportunityId").GetString()!),
                "resolve-breakdown-stop" => new CampaignCombatReactionLifecycleCommand.Resolve(id, cmd.GetProperty("stopId").GetString()!),
                "close-reaction-window-no-eligible-reactor" => new CampaignCombatReactionLifecycleCommand.Close(id, cmd.GetProperty("windowId").GetString()!),
                _ => throw new JsonException("Unsupported lifecycle command kind."),
            };
            var input = new CampaignCombatReactionLifecycleInput(command, root.GetProperty("actor").GetString() switch
            {
                "system" => CampaignOpeningPreambleActor.System,
                "axis" => CampaignOpeningPreambleActor.Axis,
                "commonwealth" => CampaignOpeningPreambleActor.Commonwealth,
                _ => throw new JsonException("Unknown lifecycle actor."),
            });
            if (!bytes.SequenceEqual(SerializeInput(input))) throw new JsonException("Noncanonical lifecycle input.");
            return input;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { throw new JsonException("Invalid lifecycle input.", error); }
    }
    internal static CampaignCombatReactionLifecycleInput ReadEventInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes); var root = document.RootElement;
            var input = DeserializeInput(Encoding.UTF8.GetBytes(root.GetProperty("input").GetRawText()));
            var (version, type) = Tag(input.Command);
            if (root.GetProperty("contractVersion").GetInt32() != version || root.GetProperty("eventType").GetString() != type)
                throw new JsonException("Lifecycle event and command variants differ.");
            return input;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { throw new JsonException("Invalid lifecycle event.", error); }
    }
    private static JsonDocument Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized lifecycle bytes.");
        var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
        try { CheckArrays(document.RootElement); return document; }
        catch { document.Dispose(); throw; }
    }
    private static void CheckArrays(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            if (value.GetArrayLength() > 512) throw new JsonException("Lifecycle array exceeds item bound.");
            foreach (var item in value.EnumerateArray()) CheckArrays(item);
        }
        else if (value.ValueKind == JsonValueKind.Object)
            foreach (var item in value.EnumerateObject()) CheckArrays(item.Value);
    }
    private static (int Version, string Type) Tag(CampaignCombatReactionLifecycleCommand command) => command switch
    {
        CampaignCombatReactionLifecycleCommand.Move => (3, "reacting-element-moved"),
        CampaignCombatReactionLifecycleCommand.Complete => (3, "reaction-participant-completed"),
        CampaignCombatReactionLifecycleCommand.Resolve => (2, "breakdown-stop-resolved"),
        CampaignCombatReactionLifecycleCommand.Close => (3, "reaction-window-closed"),
        _ => throw new JsonException("Unknown lifecycle event."),
    };
    internal static string ReceiptDomain(CampaignCombatReactionLifecycleCommand command) => command switch
    {
        CampaignCombatReactionLifecycleCommand.Move => "sandtable.combat.inherited-reacting-element-moved-receipt.v3",
        CampaignCombatReactionLifecycleCommand.Complete => "sandtable.combat.inherited-reaction-participant-completed-receipt.v3",
        CampaignCombatReactionLifecycleCommand.Resolve => "sandtable.combat.inherited-breakdown-stop-resolved-receipt.v2",
        CampaignCombatReactionLifecycleCommand.Close => "sandtable.combat.inherited-reaction-window-closed-receipt.v3",
        _ => throw new JsonException("Unknown lifecycle receipt."),
    };
    internal static string WindowCapability(CampaignCombatReactionLifecycleState state) => CampaignOpeningPreambleCodec.Hash(Bytes(writer =>
    {
        var cycle = state.Opening.Cycle!; var window = state.ReactionWindow?.Trigger ?? throw new JsonException("No current window.");
        // Pure ordered public identity; the historical Snapshot11 helper deliberately rejects Rules10.
        writer.WriteStartObject(); writer.WriteString("domain", "sandtable.observation.reaction-window.v1");
        writer.WriteString("campaignId", cycle.CampaignId); writer.WriteString("rulesetHash", cycle.RulesetHash);
        writer.WriteNumber("committedStateVersion", window.TriggerCommittedStateVersion);
        writer.WriteString("reactingSide", CampaignSnapshotSerializer.FormatSide(window.ReactingSide)); writer.WriteEndObject();
    }));
    internal static string StopCapability(CampaignCombatReactionLifecycleState state) => CampaignOpeningPreambleCodec.Hash(Bytes(writer =>
    {
        var cycle = state.Opening.Cycle!; writer.WriteStartObject(); writer.WriteString("domain", "sandtable.action.breakdown-stop.v1");
        writer.WriteString("campaignId", cycle.CampaignId); writer.WriteString("rulesetHash", cycle.RulesetHash);
        writer.WriteNumber("stateVersion", state.StateVersion); writer.WriteString("audience", "system"); writer.WriteEndObject();
    }));
    private static void WriteInput(Utf8JsonWriter writer, CampaignCombatReactionLifecycleInput input)
    {
        var cmd = input.Command; var id = cmd.Identity;
        writer.WriteStartObject(); writer.WriteStartObject("command"); writer.WriteNumber("contractVersion", id.ContractVersion); writer.WriteString("kind", cmd.Kind);
        switch (cmd)
        {
            case CampaignCombatReactionLifecycleCommand.Move move:
                writer.WriteString("windowId", move.WindowId); writer.WriteString("opportunityId", move.OpportunityId);
                writer.WriteString("originLocationId", move.OriginLocationId); writer.WriteString("destinationLocationId", move.DestinationLocationId); break;
            case CampaignCombatReactionLifecycleCommand.Complete complete:
                writer.WriteString("windowId", complete.WindowId); writer.WriteString("opportunityId", complete.OpportunityId); break;
            case CampaignCombatReactionLifecycleCommand.Resolve resolve: writer.WriteString("stopId", resolve.StopId); break;
            case CampaignCombatReactionLifecycleCommand.Close close: writer.WriteString("windowId", close.WindowId); break;
        }
        writer.WriteString("actionId", id.ActionId); writer.WriteString("creationBinding", id.CreationBinding); writer.WriteString("creationEventHash", id.CreationEventHash);
        writer.WriteString("cycleId", id.CycleId); writer.WriteNumber("expectedPriorVersion", id.ExpectedPriorVersion); writer.WriteString("expectedPositionId", id.ExpectedPositionId);
        writer.WriteEndObject(); writer.WriteString("actor", Actor(input.Actor)); writer.WriteEndObject();
    }
    internal static byte[] SerializeEvent(CampaignCombatReactionLifecycleEvent value, bool includeReceipt = true) => Bytes(writer =>
    {
        var state = value.Before; var opening = state.Opening; var cycle = opening.Cycle!; var command = value.Input.Command;
        var window = state.ReactionWindow!.Trigger; var opportunity = window.FrozenOpportunities.Single();
        var (version, type) = Tag(command);
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", version); writer.WriteString("eventType", type);
        writer.WriteString("campaignId", cycle.CampaignId); writer.WriteNumber("stateVersion", value.StateVersion); writer.WriteNumber("priorStateVersion", state.StateVersion);
        if (command is CampaignCombatReactionLifecycleCommand.Resolve) writer.WriteString("rulesetHash", cycle.RulesetHash);
        writer.WriteString("fromPositionId", command is CampaignCombatReactionLifecycleCommand.Resolve ? "land.position.breakdown-stop" : state.SequencePosition.PositionId);
        if (command is CampaignCombatReactionLifecycleCommand.Move) { writer.WriteNumber("gameTurn", 1); writer.WriteNumber("operationStage", 1); }
        if (command is not CampaignCombatReactionLifecycleCommand.Resolve)
            writer.WriteString("actingSide", command is CampaignCombatReactionLifecycleCommand.Close ? null : CampaignSnapshotSerializer.FormatSide(window.ReactingSide));
        writer.WriteString("actionId", command.Identity.ActionId);
        switch (command)
        {
            case CampaignCombatReactionLifecycleCommand.Move move:
                WriteHandles(writer, move.WindowId, move.OpportunityId, window, opportunity.OpportunityId);
                var element = state.World.Elements.Single(e => e.ElementId == opportunity.ReactingRepresentation.BoundElementIds.Single());
                writer.WriteString("elementId", element.ElementId); writer.WriteString("representationId", opportunity.ReactingRepresentation.RepresentationId);
                writer.WriteString("originLocationId", move.OriginLocationId); writer.WriteString("destinationLocationId", move.DestinationLocationId);
                writer.WriteString("mobilityId", Cna1979Movement.NonMotorizedMobilityId); WriteReferences(writer, "mobilitySources", [new("spi-1979-map-a", "8.37")]);
                writer.WriteStartObject("cost"); writer.WriteString("destinationTerrainId", "land.terrain.clear"); Cp(writer, "destinationTerrainCost", new(2, 1));
                WriteReferences(writer, "destinationTerrainSources", [new("spi-1979-map-a", "8.37")]); writer.WriteNull("routeAdjustment");
                WriteEmptyArray(writer, "crossedHexsideCosts"); Cp(writer, "totalCost", new(2, 1)); writer.WriteEndObject();
                Cp(writer, "capabilityPointsExpendedBefore", element.OperationalState.CapabilityPointsExpended); Cp(writer, "capabilityPointsExpendedAfter", new(2, 1));
                writer.WriteNumber("cohesionBefore", element.OperationalState.CohesionLevel); writer.WriteNumber("cohesionAfter", element.OperationalState.CohesionLevel);
                WriteWindow(writer, "reactionWindowAfter", value.Window!); writer.WriteString("rulesetHash", cycle.RulesetHash);
                WriteEmptyArray(writer, "breakdownAccounting"); WriteFlow(writer, value.Flow); break;
            case CampaignCombatReactionLifecycleCommand.Complete complete:
                WriteHandles(writer, complete.WindowId, complete.OpportunityId, window, opportunity.OpportunityId);
                WriteWindow(writer, "reactionWindowAfter", value.Window!); writer.WriteString("rulesetHash", cycle.RulesetHash); WriteFlow(writer, value.Flow); break;
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
        writer.WriteString("priorPrefix", state.Prefix); writer.WritePropertyName("input"); WriteInput(writer, value.Input);
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
    private static void WriteRandom(Utf8JsonWriter writer, string name, CampaignCombatReactionLifecycleState state)
    {
        var random = state.Opening.Predecessor.Stage.Weather.RandomState;
        writer.WriteStartObject(name); writer.WriteNumber("contractVersion", random.ContractVersion); writer.WriteString("algorithmId", random.AlgorithmId);
        writer.WriteNumber("seed", random.Seed); writer.WriteNumber("nextByteCursor", random.NextByteCursor); writer.WriteEndObject();
    }
    private static void WriteFlow(Utf8JsonWriter writer, CampaignBreakdownFlow flow)
    { writer.WritePropertyName("breakdownFlowAfter"); CampaignBreakdownCodec.WriteFlow(writer, flow); }
    internal static void WriteReactingPosition(Utf8JsonWriter writer, string name, CampaignCombatReactingPosition value)
    {
        writer.WriteStartObject(name);
        CampaignV11CanonicalCodec.WritePosition(writer, "suspendedMovementPosition", value.SuspendedMovementPosition);
        writer.WriteString("phasingSide", CampaignSnapshotSerializer.FormatSide(value.PhasingSide));
        writer.WriteString("reactingSide", CampaignSnapshotSerializer.FormatSide(value.ReactingSide)); writer.WriteEndObject();
    }
    internal static void WriteWindow(Utf8JsonWriter writer, string name, CampaignCombatReactionLifecycleWindow lifecycle)
    {
        var value = lifecycle.Trigger;
        writer.WriteStartObject(name); writer.WriteString("reactionWindowId", value.WindowId.Value);
        writer.WriteNumber("triggerCommittedStateVersion", value.TriggerCommittedStateVersion);
        writer.WriteString("phasingSide", CampaignSnapshotSerializer.FormatSide(value.PhasingSide));
        writer.WriteString("reactingSide", CampaignSnapshotSerializer.FormatSide(value.ReactingSide));
        WriteReactingPosition(writer, "reactingPosition", value.ReactingPosition);
        var authority = value.TriggerAuthority; writer.WriteStartObject("triggerAuthority");
        writer.WriteNumber("moveContractVersion", authority.MoveContractVersion); writer.WriteString("elementId", authority.ElementId);
        WriteRepresentation(writer, "triggeringRepresentation", authority.TriggeringRepresentation);
        writer.WriteString("originLocationId", authority.OriginLocationId); writer.WriteString("destinationLocationId", authority.DestinationLocationId); writer.WriteEndObject();
        writer.WriteStartObject("apparentTrigger"); writer.WriteString("apparentRepresentationId", value.ApparentTrigger.ApparentRepresentationId);
        writer.WriteString("originLocationId", value.ApparentTrigger.OriginLocationId); writer.WriteString("destinationLocationId", value.ApparentTrigger.DestinationLocationId); writer.WriteEndObject();
        writer.WriteStartArray("frozenOpportunities");
        foreach (var opportunity in value.FrozenOpportunities)
        {
            writer.WriteStartObject(); writer.WriteString("opportunityId", opportunity.OpportunityId.Value);
            WriteRepresentation(writer, "reactingRepresentation", opportunity.ReactingRepresentation);
            var evidence = opportunity.AdjacencyEvidence; writer.WriteStartObject("adjacencyEvidence");
            writer.WriteString("triggerLocationId", evidence.TriggerLocationId); writer.WriteString("committedDestinationLocationId", evidence.CommittedDestinationLocationId);
            writer.WriteBoolean("isAdjacent", evidence.IsAdjacent); WriteReferences(writer, "sources", evidence.Sources); writer.WriteEndObject(); writer.WriteEndObject();
        }
        writer.WriteEndArray(); writer.WriteStartArray("resolvedOpportunityIds");
        foreach (var id in lifecycle.ResolvedOpportunityIds) writer.WriteStringValue(id.Value);
        writer.WriteEndArray(); writer.WriteString("activeOpportunityId", lifecycle.ActiveOpportunityId?.Value); writer.WriteEndObject();
    }
    private static void WriteRepresentation(Utf8JsonWriter writer, string name, CampaignMapRepresentationState value)
    {
        writer.WriteStartObject(name); writer.WriteString("representationId", value.RepresentationId); writer.WriteString("currentLocationId", value.CurrentLocationId);
        writer.WriteString("bindingKind", "independent-element"); writer.WriteStartArray("boundElementIds");
        foreach (var id in value.BoundElementIds) writer.WriteStringValue(id);
        writer.WriteEndArray(); writer.WriteEndObject();
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
    public static byte[] SerializeState(CampaignCombatReactionLifecycleState state) => Bytes(writer =>
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
        { writer.WriteString("kind", "reaction"); WriteReactingPosition(writer, "reactingPosition", state.ReactionWindow.Trigger.ReactingPosition); }
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
        CampaignCombatReserveCodec.WriteMembers(writer, state.Trigger.Members);
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
        else WriteWindow(writer, "reactionWindow", state.ReactionWindow);
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
        if (stream.Length > 1_048_576) throw new JsonException("Reaction lifecycle record exceeds byte limit.");
        return stream.ToArray();
    }
    private static string Actor(CampaignOpeningPreambleActor actor) => actor switch
    {
        CampaignOpeningPreambleActor.System => "system",
        CampaignOpeningPreambleActor.Axis => "axis",
        CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
        _ => throw new JsonException("Unknown Reaction lifecycle actor."),
    };
    internal static byte[] WriteWorld(CampaignCombatReactionLifecycleState state)
    {
        // This writer handles only the history-derived reaction-lifecycle World profile.
        // Equality protects hardcoded absent fields; it never admits an arbitrary World7.
        var world = CampaignCombatReactionLifecycle.ExpectedWorld(state);
        if (state.World != world) throw new JsonException("Movement World differs from history-derived Reaction lifecycle.");
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
