using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Frozen C3a syntax and typed writers. Imported bytes never establish trusted provenance.</summary>
internal static class CampaignCombatSelectionStepsCodec
{
    private static readonly Dictionary<string, string> Shapes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Boundary"] = "contractVersion:int creationBinding:id cycle:Authority firstActingSide:side priorVersion:long priorPrefix:hash completedBreakdownReceipt:hash position:Position world:World randomState:Random weather:ApplicableWeather breakdownFlow:Idle reactionWindow:null",
        ["Participant"] = "unit:UnitKey representationId:id locationId:id componentIds:id[]",
        ["Candidate"] = "attacker:Participant defender:Participant targetLocationId:id basis:id",
        ["Window"] = "decisionId:id owner:side timing:Timing",
        ["Command"] = "contractVersion:int kind:id segmentId:id decisionId:id? fromPositionId:id? expectedPriorVersion:long? choice:id? candidate:Candidate? participant:UnitKey?",
        ["Input"] = "command:Command actor:actor admittedAt:utc? clockAvailable:bool",
        ["Event"] = "contractVersion:int eventType:id campaignId:id rulesetHash:rawHash configurationHash:hash cycleId:hash segmentId:id priorVersion:long stateVersion:long priorPrefix:hash input:Input effect:Effect receiptId:id",
        ["OpenEffect"] = "kind:id boundaryHash:hash window:Window?",
        ["SelectionEffect"] = "kind:id outcome:id candidate:Candidate? timing:Timing?",
        ["RbaOpenEffect"] = "kind:id selectionReceiptId:id window:Window",
        ["DeclineEffect"] = "kind:id selectionReceiptId:id participant:UnitKey timing:Timing",
        ["CancelEffect"] = "kind:id selectionReceiptId:id timing:Timing?",
        ["StepEffect"] = "kind:id fromPositionId:id toPositionId:id previousStepReceiptId:id dispositionReceiptId:id proofKind:id",
        ["Receipt"] = "commandHash:hash eventHash:hash receiptId:id actor:actor stateVersion:long",
        ["Control"] = "contractVersion:int boundaryHash:hash segmentId:id stateVersion:long prefix:hash stepIndex:int selectionOutcome:id selection:Candidate? selectionReceiptId:id? declineReceiptId:id? cancellationReceiptId:id? selectionWindow:Window? rbaWindow:Window? stepReceipts:id[] receipts:Receipt[] closed:bool",
        ["ApplicableWeather"] = "gameTurn:int operationStage:int attackerKind:id defenderKind:id weatherReceiptHash:hash",
        ["Timing"] = "contractVersion:int configHash:hash kind:id decisionBudgetMilliseconds:int openedAtUnixMilliseconds:utc deadlineUnixMilliseconds:utc highWaterUnixMilliseconds:utc",
        ["Idle"] = "kind:id",
    };

    public static byte[] SerializeInput(CombatStepsInput input) => Bytes("Input", writer => WriteInput(writer, input));
    public static byte[] SerializeCommand(CombatStepsCommand command) => Bytes("Command", writer => WriteCommand(writer, command));

    /// <summary>Decode only. Actor/time must be authenticated independently before becoming trusted input.</summary>
    public static CombatStepsInput ReadInput(ReadOnlySpan<byte> bytes)
    {
        ValidateSyntax(bytes, "Input");
        using var document = JsonDocument.Parse(bytes.ToArray());
        var input = document.RootElement;
        var command = input.GetProperty("command");
        return new(new(command.GetProperty("contractVersion").GetInt32(), Text(command, "kind"), Text(command, "segmentId"),
            OptionalText(command, "decisionId"), OptionalText(command, "fromPositionId"), OptionalLong(command, "expectedPriorVersion"),
            OptionalText(command, "choice"), command.GetProperty("candidate").ValueKind == JsonValueKind.Null ? null : ReadCandidate(command.GetProperty("candidate")),
            command.GetProperty("participant").ValueKind == JsonValueKind.Null ? null : ReadUnit(command.GetProperty("participant"))),
            Text(input, "actor") switch { "axis" => CampaignOpeningPreambleActor.Axis, "commonwealth" => CampaignOpeningPreambleActor.Commonwealth, _ => CampaignOpeningPreambleActor.System },
            OptionalLong(input, "admittedAt"), input.GetProperty("clockAvailable").GetBoolean());
    }

    public static byte[] SerializeBoundary(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created, CombatStepsBoundary boundary) =>
        CampaignCombatSelectionSteps.ValidateBoundary(request, created, boundary).BoundaryBytes.ToArray();

    public static CombatStepsBoundary ReadBoundary(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> created, CombatStepsBoundary independentlyTrustedBoundary)
    {
        ValidateSyntax(bytes, "Boundary");
        var expected = CampaignCombatSelectionSteps.ValidateBoundary(request, created, independentlyTrustedBoundary);
        if (!bytes.SequenceEqual(expected.BoundaryBytes)) throw new JsonException("C3a Boundary differs from independently trusted facts.");
        return expected.Boundary;
    }

    public static CombatStepsControl ReadControl(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> created, CombatStepsBoundary boundary, IReadOnlyList<CombatStepsInput> trustedInputs, IReadOnlyList<byte[]> events)
    {
        ValidateSyntax(bytes, "Control");
        var expected = CampaignCombatSelectionSteps.ReplayTrustedBoundary(request, created, boundary, trustedInputs, events);
        if (!bytes.SequenceEqual(SerializeControl(expected))) throw new JsonException("C3a Control differs from trusted replay.");
        return expected;
    }

    internal static byte[] WriteValidatedBoundary(CampaignCombatCreationRequest request, CampaignWorldSnapshotV7 initial, CombatStepsBoundary boundary)
    {
        var setup = request.Context.Setup;
        // Baseline comes from retained Created. Full typed equality except integral current CP
        // has already been established by009B; mapping changes only those validated CP fields.
        var world = JsonNode.Parse(CampaignWorldV7InitialCodec.Serialize(initial, setup.Artifact, setup.Scenario, setup.CombatInitialization))!;
        foreach (var element in world["elements"]!.AsArray())
        {
            var current = boundary.World.Elements.Single(value => value.ElementId == element!["elementId"]!.GetValue<string>());
            element!["operationalState"]!["capabilityPointsExpended"]!["numerator"] = current.OperationalState.CapabilityPointsExpended.Numerator;
            element["operationalState"]!["capabilityPointsExpended"]!["denominator"] = current.OperationalState.CapabilityPointsExpended.Denominator;
        }
        return Bytes("Boundary", writer =>
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", boundary.ContractVersion);
            writer.WriteString("creationBinding", boundary.CreationBinding);
            writer.WritePropertyName("cycle"); CampaignCombatReserveCompletionCodec.WriteCycle(writer, boundary.Cycle);
            writer.WriteString("firstActingSide", CampaignSnapshotSerializer.FormatSide(boundary.FirstActingSide));
            writer.WriteNumber("priorVersion", boundary.PriorVersion);
            writer.WriteString("priorPrefix", boundary.PriorPrefix);
            writer.WriteString("completedBreakdownReceipt", boundary.CompletedBreakdownReceipt);
            CampaignV11CanonicalCodec.WritePosition(writer, "position", boundary.Position);
            writer.WritePropertyName("world"); writer.WriteRawValue(world.ToJsonString());
            CampaignSnapshotSerializer.WriteRandomState(writer, boundary.RandomState);
            writer.WriteStartObject("weather");
            writer.WriteNumber("gameTurn", boundary.Weather.GameTurn);
            writer.WriteNumber("operationStage", boundary.Weather.OperationStage);
            writer.WriteString("attackerKind", Weather(boundary.Weather.AttackerKind));
            writer.WriteString("defenderKind", Weather(boundary.Weather.DefenderKind));
            writer.WriteString("weatherReceiptHash", boundary.Weather.WeatherReceiptHash);
            writer.WriteEndObject();
            writer.WriteStartObject("breakdownFlow"); writer.WriteString("kind", "idle"); writer.WriteEndObject();
            writer.WriteNull("reactionWindow"); writer.WriteEndObject();
        });
    }

    public static byte[] SerializeControl(CombatStepsControl state) => Bytes("Control", writer =>
    {
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
        writer.WriteString("boundaryHash", state.BoundaryHash); writer.WriteString("segmentId", state.SegmentId);
        writer.WriteNumber("stateVersion", state.StateVersion); writer.WriteString("prefix", state.Prefix);
        writer.WriteNumber("stepIndex", state.StepIndex); writer.WriteString("selectionOutcome", state.SelectionOutcome);
        writer.WritePropertyName("selection"); WriteCandidate(writer, state.Selection);
        writer.WriteString("selectionReceiptId", state.SelectionReceiptId); writer.WriteString("declineReceiptId", state.DeclineReceiptId);
        writer.WriteString("cancellationReceiptId", state.CancellationReceiptId);
        writer.WritePropertyName("selectionWindow"); WriteWindow(writer, state.SelectionWindow);
        writer.WritePropertyName("rbaWindow"); WriteWindow(writer, state.RbaWindow);
        writer.WriteStartArray("stepReceipts");
        foreach (var receipt in state.StepReceipts) writer.WriteStringValue(receipt);
        writer.WriteEndArray(); writer.WriteStartArray("receipts");
        foreach (var receipt in state.Receipts)
        {
            writer.WriteStartObject(); writer.WriteString("commandHash", receipt.CommandHash); writer.WriteString("eventHash", receipt.EventHash);
            writer.WriteString("receiptId", receipt.ReceiptId); writer.WriteString("actor", Actor(receipt.Actor));
            writer.WriteNumber("stateVersion", receipt.StateVersion); writer.WriteEndObject();
        }
        writer.WriteEndArray(); writer.WriteBoolean("closed", state.Closed); writer.WriteEndObject();
    });

    internal static byte[] SerializeEvent(CombatStepsBoundary boundary, CombatStepsControl prior, CombatStepsInput input,
        CombatStepsEffect effect, string? receiptId) => Bytes(receiptId is null ? null : "Event", writer =>
    {
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
        writer.WriteString("eventType", "combat-" + effect.Kind);
        writer.WriteString("campaignId", boundary.Cycle.CampaignId); writer.WriteString("rulesetHash", boundary.Cycle.RulesetHash);
        writer.WriteString("configurationHash", boundary.Cycle.AdmittedPolicyBundleDigest);
        writer.WriteString("cycleId", CampaignCombatReserveCompletionCodec.CycleId(boundary.Cycle));
        writer.WriteString("segmentId", prior.SegmentId); writer.WriteNumber("priorVersion", prior.StateVersion);
        writer.WriteNumber("stateVersion", checked(prior.StateVersion + 1)); writer.WriteString("priorPrefix", prior.Prefix);
        writer.WritePropertyName("input"); WriteInput(writer, input);
        writer.WriteStartObject("effect"); writer.WriteString("kind", effect.Kind);
        switch (effect)
        {
            case CombatStepsEffect.Open open:
                writer.WriteString("boundaryHash", open.BoundaryHash); writer.WritePropertyName("window"); WriteWindow(writer, open.Window); break;
            case CombatStepsEffect.Selection selection:
                writer.WriteString("outcome", selection.Outcome); writer.WritePropertyName("candidate"); WriteCandidate(writer, selection.Candidate);
                writer.WritePropertyName("timing"); WriteTiming(writer, selection.Timing); break;
            case CombatStepsEffect.RbaOpen rba:
                writer.WriteString("selectionReceiptId", rba.SelectionReceiptId); writer.WritePropertyName("window"); WriteWindow(writer, rba.Window); break;
            case CombatStepsEffect.Decline decline:
                writer.WriteString("selectionReceiptId", decline.SelectionReceiptId); writer.WritePropertyName("participant"); WriteUnit(writer, decline.Participant);
                writer.WritePropertyName("timing"); WriteTiming(writer, decline.Timing); break;
            case CombatStepsEffect.Cancel cancel:
                writer.WriteString("selectionReceiptId", cancel.SelectionReceiptId); writer.WritePropertyName("timing"); WriteTiming(writer, cancel.Timing); break;
            case CombatStepsEffect.Step step:
                writer.WriteString("fromPositionId", step.FromPositionId); writer.WriteString("toPositionId", step.ToPositionId);
                writer.WriteString("previousStepReceiptId", step.PreviousStepReceiptId); writer.WriteString("dispositionReceiptId", step.DispositionReceiptId);
                writer.WriteString("proofKind", step.ProofKind); break;
            default: throw new JsonException("Unsupported C3a effect.");
        }
        writer.WriteEndObject();
        if (receiptId is not null) writer.WriteString("receiptId", receiptId);
        writer.WriteEndObject();
    });

    private static void WriteInput(Utf8JsonWriter writer, CombatStepsInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        writer.WriteStartObject(); writer.WritePropertyName("command"); WriteCommand(writer, input.Command);
        writer.WriteString("actor", Actor(input.Actor)); writer.WritePropertyName("admittedAt");
        if (input.AdmittedAt is { } now) writer.WriteNumberValue(now); else writer.WriteNullValue();
        writer.WriteBoolean("clockAvailable", input.ClockAvailable); writer.WriteEndObject();
    }
    private static void WriteCommand(Utf8JsonWriter writer, CombatStepsCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", command.ContractVersion);
        writer.WriteString("kind", command.Kind); writer.WriteString("segmentId", command.SegmentId);
        writer.WriteString("decisionId", command.DecisionId); writer.WriteString("fromPositionId", command.FromPositionId);
        writer.WritePropertyName("expectedPriorVersion");
        if (command.ExpectedPriorVersion is { } version) writer.WriteNumberValue(version); else writer.WriteNullValue();
        writer.WriteString("choice", command.Choice); writer.WritePropertyName("candidate"); WriteCandidate(writer, command.Candidate);
        writer.WritePropertyName("participant"); WriteUnit(writer, command.Participant); writer.WriteEndObject();
    }
    private static void WriteCandidate(Utf8JsonWriter writer, CampaignCombatCandidate? value)
    {
        if (value is null) writer.WriteNullValue(); else writer.WriteRawValue(CampaignCombatIdentityCodec.SerializeCandidate(value));
    }
    private static void WriteUnit(Utf8JsonWriter writer, CampaignCombatUnitKey? unit)
    {
        if (unit is null) { writer.WriteNullValue(); return; }
        writer.WriteStartObject(); writer.WriteString("creationBinding", unit.CreationBinding);
        writer.WriteString("originalSide", unit.OriginalSide); writer.WriteString("elementId", unit.ElementId); writer.WriteEndObject();
    }
    private static void WriteWindow(Utf8JsonWriter writer, CombatStepsWindow? value)
    {
        if (value is null) { writer.WriteNullValue(); return; }
        writer.WriteStartObject(); writer.WriteString("decisionId", value.DecisionId);
        writer.WriteString("owner", CampaignSnapshotSerializer.FormatSide(value.Owner));
        writer.WritePropertyName("timing"); WriteTiming(writer, value.Timing); writer.WriteEndObject();
    }
    private static void WriteTiming(Utf8JsonWriter writer, CombatStepsTiming? value)
    {
        if (value is null) { writer.WriteNullValue(); return; }
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", value.ContractVersion); writer.WriteString("configHash", value.ConfigHash);
        writer.WriteString("kind", value.Kind); writer.WriteNumber("decisionBudgetMilliseconds", value.DecisionBudgetMilliseconds);
        writer.WriteNumber("openedAtUnixMilliseconds", value.OpenedAtUnixMilliseconds); writer.WriteNumber("deadlineUnixMilliseconds", value.DeadlineUnixMilliseconds);
        writer.WriteNumber("highWaterUnixMilliseconds", value.HighWaterUnixMilliseconds); writer.WriteEndObject();
    }
    private static CampaignCombatCandidate ReadCandidate(JsonElement value) => new(ReadParticipant(value.GetProperty("attacker")),
        ReadParticipant(value.GetProperty("defender")), Text(value, "targetLocationId"), Text(value, "basis"));
    private static CampaignCombatParticipant ReadParticipant(JsonElement value) => new(ReadUnit(value.GetProperty("unit")),
        Text(value, "representationId"), Text(value, "locationId"), value.GetProperty("componentIds").EnumerateArray().Select(v => v.GetString()!).ToArray());
    private static CampaignCombatUnitKey ReadUnit(JsonElement value) => new(Text(value, "creationBinding"), Text(value, "originalSide"), Text(value, "elementId"));
    private static string Text(JsonElement value, string name) => value.GetProperty(name).GetString()!;
    private static string? OptionalText(JsonElement value, string name) => value.GetProperty(name).ValueKind == JsonValueKind.Null ? null : Text(value, name);
    private static long? OptionalLong(JsonElement value, string name) => value.GetProperty(name).ValueKind == JsonValueKind.Null ? null : value.GetProperty(name).GetInt64();
    private static string Actor(CampaignOpeningPreambleActor actor) => actor switch
    {
        CampaignOpeningPreambleActor.Axis => "axis",
        CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
        CampaignOpeningPreambleActor.System => "system",
        _ => throw new JsonException("Invalid C3a actor."),
    };
    private static string Weather(WeatherKind kind) => kind switch
    {
        WeatherKind.Normal => "normal",
        WeatherKind.Hot => "hot",
        WeatherKind.Sandstorm => "sandstorm",
        WeatherKind.Rainstorm => "rainstorm",
        _ => throw new JsonException("Invalid C3a Weather."),
    };

    private static byte[] Bytes(string? kind, Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping })) write(writer);
        var bytes = stream.ToArray();
        if (bytes.Length > 1_048_576) throw new JsonException("C3a value exceeds byte bound.");
        if (kind is not null) ValidateSyntax(bytes, kind);
        return bytes;
    }
    internal static void ValidateSyntax(ReadOnlySpan<byte> bytes, string kind)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized C3a value.");
        using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 33 });
        CheckBounds(document.RootElement, 0);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            WriteSyntax(writer, document.RootElement, kind);
        if (!bytes.SequenceEqual(stream.ToArray())) throw new JsonException("Noncanonical C3a bytes.");
    }
    private static void CheckBounds(JsonElement value, int depth)
    {
        if (depth > 32) throw new JsonException("C3a depth bound exceeded.");
        if (value.ValueKind == JsonValueKind.Array)
        {
            if (value.GetArrayLength() > 512) throw new JsonException("C3a array bound exceeded.");
            foreach (var item in value.EnumerateArray()) CheckBounds(item, depth + 1);
        }
        else if (value.ValueKind == JsonValueKind.Object)
            foreach (var property in value.EnumerateObject()) CheckBounds(property.Value, depth + 1);
    }
    private static void WriteSyntax(Utf8JsonWriter writer, JsonElement value, string kind)
    {
        if (kind.EndsWith('?'))
        {
            if (value.ValueKind == JsonValueKind.Null) writer.WriteNullValue(); else WriteSyntax(writer, value, kind[..^1]);
            return;
        }
        if (kind.EndsWith("[]", StringComparison.Ordinal))
        {
            if (value.ValueKind != JsonValueKind.Array) throw new JsonException("C3a array required.");
            writer.WriteStartArray(); foreach (var item in value.EnumerateArray()) WriteSyntax(writer, item, kind[..^2]); writer.WriteEndArray(); return;
        }
        if (kind == "Effect")
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("kind", out var tag) || tag.ValueKind != JsonValueKind.String)
                throw new JsonException("C3a effect tag required.");
            kind = tag.GetString() switch
            {
                "segment-opened" => "OpenEffect",
                "selection-closed" => "SelectionEffect",
                "rba-opened" => "RbaOpenEffect",
                "rba-declined" => "DeclineEffect",
                "selection-cancelled" => "CancelEffect",
                "step-completed" => "StepEffect",
                _ => throw new JsonException("Unknown C3a effect tag."),
            };
        }
        if (Shapes.TryGetValue(kind, out var shape))
        {
            if (value.ValueKind != JsonValueKind.Object) throw new JsonException("C3a closed object required.");
            var fields = shape.Split(' ').Select(f => f.Split(':')).ToArray();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
                if (!names.Add(property.Name) || !fields.Any(f => f[0] == property.Name)) throw new JsonException("Duplicate or unknown C3a field.");
            if (names.Count != fields.Length) throw new JsonException("Missing C3a field.");
            writer.WriteStartObject();
            foreach (var field in fields) { writer.WritePropertyName(field[0]); WriteSyntax(writer, value.GetProperty(field[0]), field[1]); }
            writer.WriteEndObject(); return;
        }
        if (kind == "utc")
        {
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var time) || time is < 0 or > CampaignCombatSelectionSteps.UtcMaximum)
                throw new JsonException("C3a UTC bound exceeded.");
            writer.WriteNumberValue(time); return;
        }
        CampaignCombatIdentityCodec.WriteSelectionStepsExternalSyntax(writer, value, kind);
    }
}
