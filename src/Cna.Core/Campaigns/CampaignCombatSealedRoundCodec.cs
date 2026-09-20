using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cna.Core.Campaigns;

/// <summary>Frozen Round2 syntax followed by independently authenticated causal replay.</summary>
internal static class CampaignCombatSealedRoundCodec
{
    public static byte[] SerializeInput(CombatRoundInput input) => Bytes("RoundInput", writer => WriteInput(writer, input));
    public static byte[] SerializeCommand(CombatRoundCommand command) => Bytes("RoundCommand", writer => WriteCommand(writer, command));
    public static CombatRoundInput ReadInput(ReadOnlySpan<byte> bytes)
    {
        ValidateSyntax(bytes, "RoundInput");
        using var document = JsonDocument.Parse(bytes.ToArray());
        var input = document.RootElement;
        var command = input.GetProperty("command");
        var allocation = command.GetProperty("allocation");
        return new(new(command.GetProperty("contractVersion").GetInt32(), Text(command, "kind"), Text(command, "clockConfigurationHash"),
            Text(command, "segmentId"), OptionalText(command, "roundId"), OptionalText(command, "slotId"), OptionalLong(command, "expectedPriorVersion"),
            allocation.ValueKind == JsonValueKind.Null ? null : ReadAllocation(allocation)), Text(input, "actor") switch
            {
                "axis" => CampaignOpeningPreambleActor.Axis,
                "commonwealth" => CampaignOpeningPreambleActor.Commonwealth,
                _ => CampaignOpeningPreambleActor.System,
            }, OptionalLong(input, "admittedAt"), input.GetProperty("clockAvailable").GetBoolean());
    }

    public static CombatRoundBase ReadBase(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        CombatStepsBoundary boundary, IReadOnlyList<CombatStepsInput> predecessorInputs, IReadOnlyList<byte[]> predecessorEvents)
    {
        ValidateSyntax(bytes, "Base");
        var basis = CampaignCombatSealedRound.AuthenticateBase(request, created, boundary, predecessorInputs, predecessorEvents);
        if (!bytes.SequenceEqual(SerializeBase(basis))) throw new JsonException("Base2 differs from authenticated C3a predecessor.");
        return basis;
    }

    public static CombatRoundState ReadState(ReadOnlySpan<byte> bytes, CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        CombatStepsBoundary boundary, IReadOnlyList<CombatStepsInput> predecessorInputs, IReadOnlyList<byte[]> predecessorEvents,
        IReadOnlyList<CombatRoundInput> inputs, IReadOnlyList<byte[]> events)
    {
        ValidateSyntax(bytes, "RoundState");
        var state = CampaignCombatSealedRound.ReplayTrustedBoundary(request, created, boundary, predecessorInputs, predecessorEvents, inputs, events);
        if (!bytes.SequenceEqual(SerializeState(state))) throw new JsonException("Round2 state differs from authenticated history.");
        return state;
    }

    public static byte[] SerializeBase(CombatRoundBase basis) => Bytes("Base", writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 2);
        writer.WritePropertyName("boundary"); writer.WriteRawValue(basis.BoundaryBytes);
        writer.WritePropertyName("steps"); writer.WriteRawValue(CampaignCombatSelectionStepsCodec.SerializeControl(basis.Steps));
        writer.WritePropertyName("clockConfiguration"); WriteConfiguration(writer, basis.Configuration);
        writer.WriteEndObject();
    });
    internal static byte[] SerializeConfiguration(CombatRoundClockConfiguration value) => Bytes("ClockConfiguration", writer => WriteConfiguration(writer, value));
    private static void WriteConfiguration(Utf8JsonWriter writer, CombatRoundClockConfiguration value)
    {
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", value.ContractVersion);
        writer.WriteString("parentConfigurationHash", value.ParentConfigurationHash); writer.WriteString("timingPolicyId", value.TimingPolicyId);
        writer.WriteNumber("decisionBudgetMilliseconds", value.DecisionBudgetMilliseconds); writer.WriteEndObject();
    }

    public static byte[] SerializeState(CombatRoundState state) => Bytes("RoundState", writer =>
    {
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", 2); writer.WriteString("baseHash", state.Base.BaseHash);
        writer.WriteString("clockConfigurationHash", state.Base.ConfigurationHash); writer.WriteString("opportunityId", state.OpportunityId);
        writer.WriteString("roundId", state.RoundId); writer.WriteNumber("stateVersion", state.StateVersion); writer.WriteString("prefix", state.Prefix);
        writer.WriteNumber("stepIndex", state.StepIndex); writer.WriteString("status", state.Status); writer.WriteString("openingReceiptId", state.OpeningReceiptId);
        writer.WritePropertyName("timing"); WriteTiming(writer, state.Timing);
        writer.WritePropertyName("slots"); WriteSlots(writer, state.Slots);
        WriteStrings(writer, "stepReceipts", state.StepReceipts);
        writer.WritePropertyName("world"); WriteWorld(writer, state);
        CampaignSnapshotSerializer.WriteRandomState(writer, state.Base.Boundary.RandomState);
        writer.WriteStartArray("attackHistory");
        foreach (var attack in state.AttackHistory)
        {
            writer.WriteStartObject(); writer.WriteString("commitmentId", attack.CommitmentId); writer.WriteString("cycleId", attack.CycleId);
            writer.WriteString("segmentId", attack.SegmentId); writer.WritePropertyName("attacker"); WriteUnit(writer, attack.Attacker);
            writer.WritePropertyName("defender"); WriteUnit(writer, attack.Defender); writer.WriteString("targetLocationId", attack.TargetLocationId);
            writer.WriteNumber("gameTurn", attack.GameTurn); writer.WriteNumber("operationStage", attack.OperationStage); writer.WriteEndObject();
        }
        writer.WriteEndArray(); writer.WriteStartArray("targetUses");
        foreach (var target in state.TargetUses)
        {
            writer.WriteStartObject(); writer.WriteString("commitmentId", target.CommitmentId); writer.WriteString("segmentId", target.SegmentId);
            writer.WriteString("targetLocationId", target.TargetLocationId); writer.WriteEndObject();
        }
        writer.WriteEndArray(); writer.WriteString("commitmentId", state.CommitmentId); writer.WriteString("cancellationReceiptId", state.CancellationReceiptId);
        writer.WriteStartArray("receipts");
        foreach (var receipt in state.Receipts)
        {
            writer.WriteStartObject(); writer.WriteString("commandHash", receipt.CommandHash); writer.WriteString("eventHash", receipt.EventHash);
            writer.WriteString("receiptId", receipt.ReceiptId); writer.WriteString("actor", Actor(receipt.Actor));
            writer.WriteNumber("stateVersion", receipt.StateVersion); writer.WriteEndObject();
        }
        writer.WriteEndArray(); writer.WriteBoolean("closed", state.Closed); writer.WriteEndObject();
    });

    internal static byte[] SerializeEvent(CombatRoundState prior, string roundId, CombatRoundInput input,
        CampaignOpeningPreambleActor author, CombatRoundEffect effect, string? receiptId) => Bytes(receiptId is null ? null : "RoundEvent", writer =>
    {
        var basis = prior.Base;
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", 2);
        writer.WriteString("eventType", effect.Kind switch
        {
            "round-opened" => "combat-round-opened",
            "choice-sealed" => "combat-choice-sealed",
            "round-cancelled" => "combat-round-cancelled",
            "step-completed" => "combat-round-step-completed",
            "attack-committed" => "combat-attack-committed",
            _ => throw new JsonException("Unsupported Round2 effect."),
        });
        writer.WriteString("author", Actor(author)); writer.WriteString("campaignId", basis.Boundary.Cycle.CampaignId);
        writer.WriteString("rulesetHash", basis.Boundary.Cycle.RulesetHash); writer.WriteString("configurationHash", basis.ConfigurationHash);
        writer.WriteString("predecessorConfigurationHash", basis.Configuration.ParentConfigurationHash);
        writer.WriteString("cycleId", CampaignCombatReserveCompletionCodec.CycleId(basis.Boundary.Cycle));
        writer.WriteString("segmentId", basis.Steps.SegmentId); writer.WriteString("roundId", roundId);
        writer.WriteNumber("priorVersion", prior.StateVersion); writer.WriteNumber("stateVersion", checked(prior.StateVersion + 1));
        writer.WriteString("priorPrefix", prior.Prefix); writer.WritePropertyName("input"); WriteInput(writer, input);
        writer.WritePropertyName("effect"); WriteEffect(writer, effect);
        if (receiptId is not null) writer.WriteString("receiptId", receiptId);
        writer.WriteEndObject();
    });

    // Only the typed CP/ammunition fields can differ from authenticated pre-use World.
    // Derive on every serialization; no cached bytes can drift from record-with state.
    private static void WriteWorld(Utf8JsonWriter writer, CombatRoundState state)
    {
        var original = state.Base.Boundary.World;
        var allowedElements = original.Elements.Select(element =>
        {
            var current = state.World.Elements.Single(e => e.ElementId == element.ElementId);
            var prior = element.OperationalState;
            return new CampaignElementStateV6(element.ElementId, element.CurrentLocationId, element.ReserveStatus,
                new(prior.LedgerGameTurn, prior.LedgerOperationStage, current.OperationalState.CapabilityPointsExpended,
                    prior.CohesionLevel, prior.VehicleBreakdownState, prior.MovementEnded, prior.InitialLedgerOrigin),
                element.Components, element.SourceParentFormationId, element.CurrentParentFormationId,
                new(current.Ammunition.Points, element.Ammunition.InitialAmmunitionOrigin), element.Readiness);
        });
        var allowed = new CampaignWorldSnapshotV7(7, original.CreationBinding, allowedElements, original.Representations, original.BrokenVehicleLots,
            original.CohesionCauses, original.Relationships, original.CustodyLots, original.Guards, original.ReplacementEntitlements, original.FutureObligations, original.Settlements);
        if (state.World != allowed) throw new JsonException("Round2 World changed beyond derived cost fields.");
        var root = JsonNode.Parse(state.Base.WorldBytes)!;
        foreach (var element in root["elements"]!.AsArray())
        {
            var current = state.World.Elements.Single(e => e.ElementId == element!["elementId"]!.GetValue<string>());
            element!["operationalState"]!["capabilityPointsExpended"]!["numerator"] = current.OperationalState.CapabilityPointsExpended.Numerator;
            element["operationalState"]!["capabilityPointsExpended"]!["denominator"] = current.OperationalState.CapabilityPointsExpended.Denominator;
            element["ammunition"]!["points"] = current.Ammunition.Points;
        }
        writer.WriteRawValue(root.ToJsonString());
    }

    internal static string CommitmentId(CombatRoundState state, IReadOnlyList<CombatRoundAllocation> allocations) => "cmt." +
        CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.commitment.v2", Bytes(null, writer =>
        {
            writer.WriteStartObject(); writer.WriteString("roundId", state.RoundId); writer.WriteNumber("priorVersion", state.StateVersion);
            writer.WriteString("priorPrefix", state.Prefix); writer.WritePropertyName("allocations"); WriteAllocations(writer, allocations); writer.WriteEndObject();
        }))[7..];
    private static void WriteAllocations(Utf8JsonWriter writer, IReadOnlyList<CombatRoundAllocation> allocations)
    {
        writer.WriteStartArray(); foreach (var allocation in allocations) WriteAllocation(writer, allocation); writer.WriteEndArray();
    }
    private static void WriteUnit(Utf8JsonWriter writer, CampaignCombatUnitKey unit)
    {
        writer.WriteStartObject(); writer.WriteString("creationBinding", unit.CreationBinding); writer.WriteString("originalSide", unit.OriginalSide);
        writer.WriteString("elementId", unit.ElementId); writer.WriteEndObject();
    }

    internal static string RoundId(CombatRoundState state, string opportunity, CombatRoundTiming timing) => "rnd." +
        CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.round.v2", Bytes(null, writer =>
        {
            writer.WriteStartObject(); writer.WriteString("baseHash", state.Base.BaseHash); writer.WriteString("opportunityId", opportunity);
            writer.WriteNumber("openingAuthorityVersion", state.StateVersion); writer.WriteString("openingHistoryPrefix", state.Prefix);
            writer.WritePropertyName("timing"); WriteTiming(writer, timing); writer.WriteEndObject();
        }))[7..];
    internal static string SlotId(string roundId, string role) => "slt." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.slot.v2", Bytes(null, writer =>
    {
        writer.WriteStartObject(); writer.WriteString("roundId", roundId); writer.WriteString("role", role); writer.WriteEndObject();
    }))[7..];

    private static void WriteInput(Utf8JsonWriter writer, CombatRoundInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        writer.WriteStartObject(); writer.WritePropertyName("command"); WriteCommand(writer, input.Command);
        writer.WriteString("actor", Actor(input.Actor)); WriteLong(writer, "admittedAt", input.AdmittedAt);
        writer.WriteBoolean("clockAvailable", input.ClockAvailable); writer.WriteEndObject();
    }
    private static void WriteCommand(Utf8JsonWriter writer, CombatRoundCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", command.ContractVersion); writer.WriteString("kind", command.Kind);
        writer.WriteString("clockConfigurationHash", command.ClockConfigurationHash); writer.WriteString("segmentId", command.SegmentId);
        writer.WriteString("roundId", command.RoundId); writer.WriteString("slotId", command.SlotId);
        WriteLong(writer, "expectedPriorVersion", command.ExpectedPriorVersion); writer.WritePropertyName("allocation"); WriteAllocation(writer, command.Allocation);
        writer.WriteEndObject();
    }
    private static void WriteAllocation(Utf8JsonWriter writer, CombatRoundAllocation? value)
    {
        if (value is null) { writer.WriteNullValue(); return; }
        writer.WriteStartObject(); writer.WriteString("kind", value.Kind); writer.WriteStartObject("unit");
        writer.WriteString("creationBinding", value.Unit.CreationBinding); writer.WriteString("originalSide", value.Unit.OriginalSide);
        writer.WriteString("elementId", value.Unit.ElementId); writer.WriteEndObject(); writer.WriteString("componentId", value.ComponentId);
        writer.WriteNumber("committedToe", value.CommittedToe); writer.WriteEndObject();
    }
    private static void WriteSlots(Utf8JsonWriter writer, IReadOnlyList<CombatRoundSlot> values)
    {
        writer.WriteStartArray();
        foreach (var slot in values)
        {
            writer.WriteStartObject(); writer.WriteString("role", slot.Role); writer.WriteString("owner", slot.Owner); writer.WriteString("slotId", slot.SlotId);
            writer.WritePropertyName("allocation"); WriteAllocation(writer, slot.Allocation); writer.WriteString("sealedReceiptId", slot.SealedReceiptId);
            WriteLong(writer, "sealedAt", slot.SealedAt); writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
    private static void WriteTiming(Utf8JsonWriter writer, CombatRoundTiming? value)
    {
        if (value is null) { writer.WriteNullValue(); return; }
        writer.WriteStartObject(); writer.WriteNumber("contractVersion", value.ContractVersion); writer.WriteString("clockConfigurationHash", value.ClockConfigurationHash);
        writer.WriteString("kind", value.Kind); writer.WriteNumber("decisionBudgetMilliseconds", value.DecisionBudgetMilliseconds);
        writer.WriteNumber("openedAtUnixMilliseconds", value.OpenedAtUnixMilliseconds); writer.WriteNumber("deadlineUnixMilliseconds", value.DeadlineUnixMilliseconds);
        writer.WriteNumber("openingFloorUnixMilliseconds", value.OpeningFloorUnixMilliseconds); writer.WriteEndObject();
    }
    private static void WriteEffect(Utf8JsonWriter writer, CombatRoundEffect effect)
    {
        writer.WriteStartObject(); writer.WriteString("kind", effect.Kind);
        switch (effect)
        {
            case CombatRoundEffect.Open open:
                writer.WriteString("baseHash", open.BaseHash); writer.WriteString("opportunityId", open.OpportunityId);
                writer.WritePropertyName("timing"); WriteTiming(writer, open.Timing); writer.WritePropertyName("slots"); WriteSlots(writer, open.Slots); break;
            case CombatRoundEffect.Seal seal:
                writer.WriteString("slotId", seal.SlotId); writer.WritePropertyName("allocation"); WriteAllocation(writer, seal.Allocation);
                writer.WritePropertyName("timing"); WriteTiming(writer, seal.Timing); writer.WriteBoolean("prepared", seal.Prepared); break;
            case CombatRoundEffect.Cancel cancel:
                writer.WriteString("cause", cancel.Cause); writer.WritePropertyName("timing"); WriteTiming(writer, cancel.Timing); break;
            case CombatRoundEffect.Commit commit:
                writer.WriteString("commitmentId", commit.CommitmentId); writer.WritePropertyName("allocations"); WriteAllocations(writer, commit.Allocations);
                writer.WriteStartArray("costs");
                foreach (var cost in commit.Costs)
                {
                    writer.WriteStartObject(); writer.WritePropertyName("unit"); WriteUnit(writer, cost.Unit);
                    writer.WriteNumber("beforeCp", cost.BeforeCp); writer.WriteNumber("afterCp", cost.AfterCp);
                    writer.WriteNumber("beforeAmmo", cost.BeforeAmmo); writer.WriteNumber("afterAmmo", cost.AfterAmmo); writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WritePropertyName("preResultRandomState");
                var random = commit.PreResultRandomState;
                writer.WriteStartObject(); writer.WriteNumber("contractVersion", random.ContractVersion); writer.WriteString("algorithmId", random.AlgorithmId);
                writer.WriteNumber("seed", random.Seed); writer.WriteNumber("nextByteCursor", random.NextByteCursor); writer.WriteEndObject();
                break;
            case CombatRoundEffect.Step step:
                writer.WriteString("fromPositionId", step.FromPositionId); writer.WriteString("toPositionId", step.ToPositionId);
                writer.WriteString("previousStepReceiptId", step.PreviousStepReceiptId); WriteStrings(writer, "proofReceipts", step.ProofReceipts); break;
            default: throw new JsonException("Unsupported Round2 effect.");
        }
        writer.WriteEndObject();
    }
    private static void WriteStrings(Utf8JsonWriter writer, string name, IReadOnlyList<string> values)
    {
        writer.WriteStartArray(name); foreach (var value in values) writer.WriteStringValue(value); writer.WriteEndArray();
    }
    private static void WriteLong(Utf8JsonWriter writer, string name, long? value)
    {
        writer.WritePropertyName(name); if (value is { } number) writer.WriteNumberValue(number); else writer.WriteNullValue();
    }
    private static CombatRoundAllocation ReadAllocation(JsonElement value)
    {
        var unit = value.GetProperty("unit");
        return new(Text(value, "kind"), new(Text(unit, "creationBinding"), Text(unit, "originalSide"), Text(unit, "elementId")),
            Text(value, "componentId"), value.GetProperty("committedToe").GetInt32());
    }
    internal static string Actor(CampaignOpeningPreambleActor actor) => actor switch
    {
        CampaignOpeningPreambleActor.Axis => "axis",
        CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
        CampaignOpeningPreambleActor.System => "system",
        _ => throw new JsonException("Invalid Round2 actor."),
    };
    private static string Text(JsonElement value, string name) => value.GetProperty(name).GetString()!;
    private static string? OptionalText(JsonElement value, string name) => value.GetProperty(name).ValueKind == JsonValueKind.Null ? null : Text(value, name);
    private static long? OptionalLong(JsonElement value, string name) => value.GetProperty(name).ValueKind == JsonValueKind.Null ? null : value.GetProperty(name).GetInt64();
    private static byte[] Bytes(string? kind, Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping })) write(writer);
        var bytes = stream.ToArray();
        if (bytes.Length > 1_048_576) throw new JsonException("Oversized Round2 value.");
        if (kind is not null) ValidateSyntax(bytes, kind);
        return bytes;
    }
    private static readonly Dictionary<string, string> Shapes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["Base"] = "contractVersion:int boundary:Boundary steps:Control clockConfiguration:ClockConfiguration",
        ["Allocation"] = "kind:id unit:UnitKey componentId:id committedToe:int",
        ["Slot"] = "role:role owner:side slotId:id allocation:Allocation sealedReceiptId:id? sealedAt:utc?",
        ["RoundCommand"] = "contractVersion:int kind:id clockConfigurationHash:hash segmentId:id roundId:id? slotId:id? expectedPriorVersion:long? allocation:Allocation?",
        ["RoundInput"] = "command:RoundCommand actor:actor admittedAt:utc? clockAvailable:bool",
        ["RoundEvent"] = "contractVersion:int eventType:id author:actor campaignId:id rulesetHash:rawHash configurationHash:hash predecessorConfigurationHash:hash cycleId:hash segmentId:id roundId:id priorVersion:long stateVersion:long priorPrefix:hash input:RoundInput effect:RoundEffect receiptId:id",
        ["RoundOpen"] = "kind:id baseHash:hash opportunityId:id timing:ClockTiming slots:Slot[]",
        ["RoundSeal"] = "kind:id slotId:id allocation:Allocation timing:ClockTiming prepared:bool",
        ["RoundCancel"] = "kind:id cause:id timing:ClockTiming",
        ["RoundStep"] = "kind:id fromPositionId:id toPositionId:id previousStepReceiptId:id proofReceipts:id[]",
        ["RoundCommit"] = "kind:id commitmentId:id allocations:Allocation[] costs:Cost[] preResultRandomState:Random",
        ["Cost"] = "unit:UnitKey beforeCp:int afterCp:int beforeAmmo:int afterAmmo:int",
        ["AttackHistory"] = "commitmentId:id cycleId:hash segmentId:id attacker:UnitKey defender:UnitKey targetLocationId:id gameTurn:int operationStage:int",
        ["TargetUse"] = "commitmentId:id segmentId:id targetLocationId:id",
        ["RoundState"] = "contractVersion:int baseHash:hash clockConfigurationHash:hash opportunityId:id? roundId:id? stateVersion:long prefix:hash stepIndex:int status:id openingReceiptId:id? timing:ClockTiming? slots:Slot[] stepReceipts:id[] world:World randomState:Random attackHistory:AttackHistory[] targetUses:TargetUse[] commitmentId:id? cancellationReceiptId:id? receipts:Receipt[] closed:bool",
        ["ClockConfiguration"] = "contractVersion:int parentConfigurationHash:hash timingPolicyId:id decisionBudgetMilliseconds:int",
        ["ClockTiming"] = "contractVersion:int clockConfigurationHash:hash kind:id decisionBudgetMilliseconds:int openedAtUnixMilliseconds:utc deadlineUnixMilliseconds:utc openingFloorUnixMilliseconds:utc",
    };
    internal static void ValidateSyntax(ReadOnlySpan<byte> bytes, string kind)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized Round2 value.");
        using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 33 });
        CheckBounds(document.RootElement, 0);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            WriteSyntax(writer, document.RootElement, kind);
        if (!bytes.SequenceEqual(stream.ToArray())) throw new JsonException("Noncanonical Round2 bytes.");
    }
    private static void CheckBounds(JsonElement value, int depth)
    {
        if (depth > 32) throw new JsonException("Round2 depth bound exceeded.");
        if (value.ValueKind == JsonValueKind.Array)
        {
            if (value.GetArrayLength() > 512) throw new JsonException("Round2 array bound exceeded.");
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
            if (value.ValueKind != JsonValueKind.Array) throw new JsonException("Round2 array required.");
            writer.WriteStartArray(); foreach (var item in value.EnumerateArray()) WriteSyntax(writer, item, kind[..^2]); writer.WriteEndArray(); return;
        }
        if (kind == "RoundEffect")
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("kind", out var tag) || tag.ValueKind != JsonValueKind.String)
                throw new JsonException("Round2 effect tag required.");
            kind = tag.GetString() switch
            {
                "round-opened" => "RoundOpen",
                "choice-sealed" => "RoundSeal",
                "round-cancelled" => "RoundCancel",
                "step-completed" => "RoundStep",
                "attack-committed" => "RoundCommit",
                _ => throw new JsonException("Unknown Round2 effect tag."),
            };
        }
        if (kind == "role")
        {
            if (value.ValueKind != JsonValueKind.String || value.GetString() is not ("attacker" or "defender"))
                throw new JsonException("Invalid Round2 role.");
            writer.WriteStringValue(value.GetString()); return;
        }
        if (Shapes.TryGetValue(kind, out var shape))
        {
            if (value.ValueKind != JsonValueKind.Object) throw new JsonException("Round2 closed object required.");
            var fields = shape.Split(' ').Select(f => f.Split(':')).ToArray();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
                if (!names.Add(property.Name) || !fields.Any(f => f[0] == property.Name)) throw new JsonException("Duplicate or unknown Round2 field.");
            if (names.Count != fields.Length) throw new JsonException("Missing Round2 field.");
            writer.WriteStartObject();
            foreach (var field in fields) { writer.WritePropertyName(field[0]); WriteSyntax(writer, value.GetProperty(field[0]), field[1]); }
            writer.WriteEndObject(); return;
        }
        if (kind == "utc")
        {
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var time) || time is < 0 or > CampaignCombatSelectionSteps.UtcMaximum)
                throw new JsonException("Round2 UTC bound exceeded.");
            writer.WriteNumberValue(time); return;
        }
        CampaignCombatSelectionStepsCodec.WriteSealedRoundExternalSyntax(writer, value, kind);
    }
}
