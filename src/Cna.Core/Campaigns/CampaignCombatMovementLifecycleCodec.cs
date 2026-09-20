using System.Text;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatMovementLifecycleCodec
{
    private static readonly System.Collections.ObjectModel.ReadOnlyCollection<RuleReference> ResolveSources = Array.AsReadOnly<RuleReference>([new("spi-1979-land-rules", "21.24-21.26")]);
    public static byte[] SerializeInput(CampaignCombatMovementLifecycleInput input)
    {
        try
        {
            if (input?.Command?.Identity is not { } id || id.ContractVersion != 2 || id.ExpectedPriorVersion < 1 || !Enum.IsDefined(input.Actor))
                throw new JsonException("Unsupported lifecycle input identity.");
            _ = ContentContractGuards.RequireSourceAtom(id.CreationBinding, nameof(input));
            _ = ContentContractGuards.RequireSourceAtom(id.ExpectedPositionId, nameof(input));
            foreach (var hash in new[] { id.ActionId, id.CreationEventHash, id.CycleId }) _ = ContentContractGuards.RequireSha256(hash, nameof(input));
            if (input.Command is CampaignCombatMovementLifecycleCommand.Stop stop) _ = ContentContractGuards.RequireSha256(stop.RouteId, nameof(input));
            else if (input.Command is CampaignCombatMovementLifecycleCommand.Resolve resolve) _ = ContentContractGuards.RequireSha256(resolve.StopId, nameof(input));
            else if (input.Command is not CampaignCombatMovementLifecycleCommand.Complete) throw new JsonException("Unknown lifecycle command.");
        }
        catch (ArgumentException error) { throw new JsonException("Invalid lifecycle primitive.", error); }
        return Bytes(writer => WriteInput(writer, input));
    }
    public static CampaignCombatMovementLifecycleInput DeserializeInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes); var root = document.RootElement; var cmd = root.GetProperty("command");
            var id = new CampaignCombatMovementLifecycleIdentity(cmd.GetProperty("contractVersion").GetInt32(), cmd.GetProperty("actionId").GetString()!,
                cmd.GetProperty("creationBinding").GetString()!, cmd.GetProperty("creationEventHash").GetString()!, cmd.GetProperty("cycleId").GetString()!,
                cmd.GetProperty("expectedPriorVersion").GetInt64(), cmd.GetProperty("expectedPositionId").GetString()!);
            CampaignCombatMovementLifecycleCommand command = cmd.GetProperty("kind").GetString() switch
            {
                "stop-element-movement" => new CampaignCombatMovementLifecycleCommand.Stop(id, cmd.GetProperty("routeId").GetString()!),
                "resolve-breakdown-stop" => new CampaignCombatMovementLifecycleCommand.Resolve(id, cmd.GetProperty("stopId").GetString()!),
                "complete-movement-segment" => new CampaignCombatMovementLifecycleCommand.Complete(id),
                _ => throw new JsonException("Unsupported lifecycle command kind."),
            };
            var input = new CampaignCombatMovementLifecycleInput(command, root.GetProperty("actor").GetString() switch
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
    internal static CampaignCombatMovementLifecycleInput ReadEventInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes); var root = document.RootElement;
            var input = DeserializeInput(Encoding.UTF8.GetBytes(root.GetProperty("input").GetRawText()));
            var (version, type) = Tag(input.Command);
            if (root.GetProperty("contractVersion").GetInt32() != version || root.GetProperty("eventType").GetString() != type)
                throw new JsonException("Lifecycle command and event variant differ.");
            return input;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { throw new JsonException("Invalid lifecycle event.", error); }
    }
    internal static string ReceiptDomain(CampaignCombatMovementLifecycleCommand command) => command switch
    {
        CampaignCombatMovementLifecycleCommand.Stop => "sandtable.combat.inherited-movement-stop-receipt.v2",
        CampaignCombatMovementLifecycleCommand.Resolve => "sandtable.combat.inherited-breakdown-stop-resolved-receipt.v2",
        CampaignCombatMovementLifecycleCommand.Complete => "sandtable.combat.inherited-movement-completion-receipt.v3",
        _ => throw new JsonException("Unknown lifecycle receipt variant."),
    };
    private static (int Version, string Type) Tag(CampaignCombatMovementLifecycleCommand command) => command switch
    {
        CampaignCombatMovementLifecycleCommand.Stop => (2, "element-movement-stopped"),
        CampaignCombatMovementLifecycleCommand.Resolve => (2, "breakdown-stop-resolved"),
        CampaignCombatMovementLifecycleCommand.Complete => (3, "movement-segment-completed"),
        _ => throw new JsonException("Unknown lifecycle event variant."),
    };
    internal static string Capability(CampaignCombatMovementLifecycleState state, int index) => CampaignOpeningPreambleCodec.Hash(Bytes(writer =>
    {
        var cycle = state.Movement.Opening.Cycle!;
        writer.WriteStartObject(); writer.WriteString("domain", index == 0 ? "sandtable.observation.movement-route.v1" : "sandtable.action.breakdown-stop.v1");
        writer.WriteString("campaignId", cycle.CampaignId); writer.WriteString("rulesetHash", cycle.RulesetHash);
        writer.WriteNumber("stateVersion", state.StateVersion); writer.WriteString("audience", index == 0 ? CampaignSnapshotSerializer.FormatSide(cycle.ActingSide) : "system");
        if (index == 0)
        {
            var route = ((CampaignBreakdownFlow.Moving)state.BreakdownFlow).Route;
            writer.WriteString("elementId", route.ElementId); writer.WriteString("originLocationId", route.OriginLocationId); writer.WriteString("currentLocationId", route.CurrentLocationId);
        }
        writer.WriteEndObject();
    }));
    internal static string Action(int index, string? capability) => CampaignOpeningPreambleCodec.Hash(Bytes(writer =>
    {
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
        writer.WriteString("kind", index switch { 0 => "stop-element-movement", 1 => "resolve-breakdown-stop", _ => "complete-movement-segment" });
        if (index < 2) writer.WriteString(index == 0 ? "routeId" : "stopId", capability);
        writer.WriteEndObject();
    }));
    internal static byte[] SerializeEvent(CampaignCombatMovementLifecycleEvent value, bool includeReceipt = true) => Bytes(writer =>
    {
        var state = value.Before; var opening = state.Movement.Opening; var cycle = opening.Cycle!;
        var (version, type) = Tag(value.Input.Command);
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", version); writer.WriteString("eventType", type);
        writer.WriteString("campaignId", cycle.CampaignId); writer.WriteNumber("stateVersion", value.StateVersion); writer.WriteNumber("priorStateVersion", state.StateVersion);
        if (value.Input.Command is not CampaignCombatMovementLifecycleCommand.Complete) writer.WriteString("rulesetHash", cycle.RulesetHash);
        writer.WriteString("fromPositionId", state.SequencePosition.PositionId);
        switch (value.Input.Command)
        {
            case CampaignCombatMovementLifecycleCommand.Stop stop:
                writer.WriteString("actionId", stop.Identity.ActionId); writer.WriteString("actingSide", CampaignSnapshotSerializer.FormatSide(cycle.ActingSide));
                writer.WriteString("submittedRouteId", stop.RouteId); WriteFlow(writer, value.BreakdownFlow);
                break;
            case CampaignCombatMovementLifecycleCommand.Resolve resolve:
                writer.WriteString("actionId", resolve.Identity.ActionId); writer.WritePropertyName("stop");
                CampaignBreakdownCodec.WriteStop(writer, ((CampaignBreakdownFlow.PhasingStop)state.BreakdownFlow).Stop);
                WriteRandom(writer, "randomStateBefore", state); Empty(writer, "checks"); Empty(writer, "createdLots"); WriteRandom(writer, "randomStateAfter", state);
                WriteFlow(writer, value.BreakdownFlow); CampaignSnapshotSerializer.WriteSources(writer, ResolveSources);
                break;
            case CampaignCombatMovementLifecycleCommand.Complete:
                writer.WriteNumber("gameTurn", 1); writer.WriteNumber("operationStage", 1); writer.WriteString("actingSide", CampaignSnapshotSerializer.FormatSide(cycle.ActingSide));
                CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", value.SequencePosition);
                writer.WriteString("rulesetHash", cycle.RulesetHash); WriteFlow(writer, value.BreakdownFlow);
                break;
        }
        var receipt = opening.Predecessor.Stage.Weather.Opening.Creation.CreationReceipt;
        writer.WriteString("configurationHash", cycle.AdmittedPolicyBundleDigest); writer.WriteString("creationBinding", receipt.CreationBinding);
        writer.WriteString("creationEventHash", receipt.CreationEventHash); writer.WriteString("cycleId", opening.CycleId);
        writer.WriteString("openingBaseHash", opening.OpeningBaseHash); writer.WriteString("completionReceiptId", opening.CompletionReceiptId);
        writer.WriteString("priorPrefix", state.Prefix); writer.WritePropertyName("input"); WriteInput(writer, value.Input);
        if (value.Input.Command is not CampaignCombatMovementLifecycleCommand.Complete)
            CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", value.SequencePosition);
        WriteInterrupt(writer, "interruptContextAfter", value.InterruptContext);
        if (value.Input.Command is CampaignCombatMovementLifecycleCommand.Complete)
        {
            WriteLocations(writer, value.EndLocations); WriteUnits(writer, "excludedUnits", value.ExcludedUnits);
            writer.WriteStartArray("progress");
            foreach (var reference in state.Movement.ActualProgressRefs)
            {
                writer.WriteStartObject(); writer.WriteString("eventType", reference.EventType); writer.WriteString("receiptId", reference.ReceiptId);
                writer.WriteString("eventHash", reference.EventHash); writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }
        if (includeReceipt) writer.WriteString("receiptId", value.ReceiptId);
        writer.WriteEndObject();
    });
    private static void WriteInput(Utf8JsonWriter writer, CampaignCombatMovementLifecycleInput input)
    {
        var command = input.Command; var id = command.Identity;
        writer.WriteStartObject(); writer.WriteStartObject("command"); writer.WriteNumber("contractVersion", id.ContractVersion); writer.WriteString("kind", command.Kind);
        if (command is CampaignCombatMovementLifecycleCommand.Stop stop) writer.WriteString("routeId", stop.RouteId);
        if (command is CampaignCombatMovementLifecycleCommand.Resolve resolve) writer.WriteString("stopId", resolve.StopId);
        writer.WriteString("actionId", id.ActionId); writer.WriteString("creationBinding", id.CreationBinding); writer.WriteString("creationEventHash", id.CreationEventHash);
        writer.WriteString("cycleId", id.CycleId); writer.WriteNumber("expectedPriorVersion", id.ExpectedPriorVersion); writer.WriteString("expectedPositionId", id.ExpectedPositionId);
        writer.WriteEndObject(); writer.WriteString("actor", Actor(input.Actor)); writer.WriteEndObject();
    }
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
    private static void WriteRandom(Utf8JsonWriter writer, string name, CampaignCombatMovementLifecycleState state)
    {
        var random = state.Movement.Opening.Predecessor.Stage.Weather.RandomState;
        writer.WriteStartObject(name); writer.WriteNumber("contractVersion", random.ContractVersion); writer.WriteString("algorithmId", random.AlgorithmId);
        writer.WriteNumber("seed", random.Seed); writer.WriteNumber("nextByteCursor", random.NextByteCursor); writer.WriteEndObject();
    }
    private static void WriteFlow(Utf8JsonWriter writer, CampaignBreakdownFlow flow)
    { writer.WritePropertyName("breakdownFlowAfter"); CampaignBreakdownCodec.WriteFlow(writer, flow); }
    private static void Empty(Utf8JsonWriter writer, string name) { writer.WriteStartArray(name); writer.WriteEndArray(); }
    private static JsonDocument Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized lifecycle bytes.");
        return JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
    }
    public static byte[] SerializeState(CampaignCombatMovementLifecycleState state) => Bytes(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 1);
        WriteAuthority(writer, state.Movement.Opening.Predecessor);
        writer.WriteNumber("stateVersion", state.StateVersion);
        writer.WriteString("prefix", state.Prefix);
        CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", state.SequencePosition);
        writer.WriteString("initiativeHolder", CampaignSnapshotSerializer.FormatSide(state.Movement.Opening.Predecessor.Stage.Weather.Opening.InitiativeHolder!.Value));
        writer.WriteStartArray("operationStageOrders");
        foreach (var order in state.Movement.Opening.Predecessor.Stage.Weather.Opening.Orders)
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
        CampaignOperationStageWeatherCodec.Write(writer, state.Movement.Opening.Predecessor.Stage.Weather.Weather);
        writer.WritePropertyName("world");
        writer.WriteRawValue(CampaignCombatInheritedMovementCodec.WriteWorld(state.Movement));
        CampaignSnapshotSerializer.WriteRandomState(writer, state.Movement.Opening.Predecessor.Stage.Weather.RandomState);
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
        writer.WriteString("firstActingSide", CampaignSnapshotSerializer.FormatSide(state.Movement.Opening.Predecessor.FirstActingSide));
        CampaignCombatReserveCodec.WriteMembers(writer, state.Movement.Members);
        if (state.Movement.Opening.Cycle is null) writer.WriteNull("cycle");
        else
        {
            writer.WritePropertyName("cycle");
            CampaignCombatReserveCompletionCodec.WriteCycle(writer, state.Movement.Opening.Cycle);
        }
        writer.WriteString("cycleId", state.Movement.Opening.CycleId);
        writer.WriteString("openingBaseHash", state.Movement.Opening.OpeningBaseHash);
        writer.WriteString("completionReceiptId", state.Movement.Opening.CompletionReceiptId);
        writer.WriteStartArray("tracks");
        foreach (var track in state.Movement.Tracks)
        {
            writer.WriteStartObject(); WriteUnit(writer, track.Unit);
            writer.WriteStartArray("route");
            foreach (var location in track.Route) writer.WriteStringValue(location);
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteStartArray("actualProgressRefs");
        foreach (var reference in state.Movement.ActualProgressRefs)
        {
            writer.WriteStartObject(); writer.WriteString("eventType", reference.EventType);
            writer.WriteString("receiptId", reference.ReceiptId); writer.WriteString("eventHash", reference.EventHash);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WritePropertyName("breakdownFlow");
        if (state.BreakdownFlow is null) writer.WriteNullValue();
        else CampaignBreakdownCodec.WriteFlow(writer, state.BreakdownFlow);
        WriteInterrupt(writer, "interruptContext", state.InterruptContext);
        writer.WritePropertyName("movementEnd");
        if (state.MovementEnd is null) writer.WriteNullValue();
        else WriteProof(writer, state.MovementEnd);
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
        if (stream.Length > 1_048_576) throw new JsonException("Movement lifecycle record exceeds byte limit.");
        return stream.ToArray();
    }
    private static string Actor(CampaignOpeningPreambleActor actor) => actor switch
    {
        CampaignOpeningPreambleActor.System => "system",
        CampaignOpeningPreambleActor.Axis => "axis",
        CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
        _ => throw new JsonException("Unknown Movement lifecycle actor."),
    };
}
