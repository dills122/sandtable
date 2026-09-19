using System.Text;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatReserveCodec
{
    public static byte[] SerializeInput(CampaignCombatReserveInput input)
    {
        ValidateInput(input);
        return Bytes(writer => WriteInput(writer, input));
    }

    public static CampaignCombatReserveInput DeserializeInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes);
            var root = document.RootElement;
            var command = root.GetProperty("command");
            var result = new CampaignCombatReserveInput(new CampaignCombatReserveCommand(
                command.GetProperty("contractVersion").GetInt32(), command.GetProperty("kind").GetString()!,
                command.GetProperty("creationBinding").GetString()!, command.GetProperty("creationEventHash").GetString()!,
                command.GetProperty("expectedPriorVersion").GetInt64(), command.GetProperty("expectedPositionId").GetString()!, command.GetProperty("elementId").GetString()!),
                ReadActor(root.GetProperty("actor").GetString()));
            if (!bytes.SequenceEqual(SerializeInput(result))) throw new JsonException("Noncanonical Reserve designation input.");
            return result;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException
            or FormatException or OverflowException)
        { throw new JsonException("Invalid Reserve designation input.", error); }
    }

    internal static CampaignCombatReserveInput ReadEventInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes);
            var root = document.RootElement;
            if (root.GetProperty("contractVersion").GetInt32() != 2)
                throw new JsonException("Unsupported Reserve designation event version.");
            var input = DeserializeInput(Encoding.UTF8.GetBytes(root.GetProperty("input").GetRawText()));
            if (root.GetProperty("eventType").GetString() != "reserve-element-designated")
                throw new JsonException("Reserve designation input/event kind mismatch.");
            return input;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException
            or FormatException or OverflowException)
        { throw new JsonException("Invalid Reserve designation event.", error); }
    }

    internal static byte[] SerializeEvent(CampaignCombatReserveEvent value, bool includeReceipt = true) => Bytes(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 2);
        writer.WriteString("eventType", "reserve-element-designated");
        WriteAuthority(writer, value.Before);
        writer.WriteNumber("priorVersion", value.Before.StateVersion);
        writer.WriteNumber("stateVersion", value.StateVersion);
        writer.WriteString("priorPrefix", value.Before.Prefix);
        writer.WriteString("fromPositionId", value.Before.SequencePosition.PositionId);
        writer.WritePropertyName("input");
        WriteInput(writer, value.Input);
        writer.WriteNumber("gameTurn", 1);
        writer.WriteNumber("operationStage", 1);
        writer.WriteString("actingSide", CampaignSnapshotSerializer.FormatSide(value.Before.FirstActingSide));
        writer.WriteString("elementId", value.Input.Command.ElementId);
        writer.WriteString("priorStatus", "none");
        writer.WriteString("resultingStatus", "I");
        CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", value.Before.SequencePosition);
        CampaignSnapshotSerializer.WriteSources(writer, CampaignCombatReserveDesignation.Sources);
        if (includeReceipt) writer.WriteString("receiptId", value.ReceiptId);
        writer.WriteEndObject();
    });

    public static byte[] SerializeState(CampaignCombatReserveState state) => Bytes(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 1);
        WriteAuthority(writer, state);
        writer.WriteNumber("stateVersion", state.StateVersion);
        writer.WriteString("prefix", state.Prefix);
        CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", state.SequencePosition);
        writer.WriteString("initiativeHolder", CampaignSnapshotSerializer.FormatSide(state.Stage.Weather.Opening.InitiativeHolder!.Value));
        writer.WriteStartArray("operationStageOrders");
        foreach (var order in state.Stage.Weather.Opening.Orders)
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
        CampaignOperationStageWeatherCodec.Write(writer, state.Stage.Weather.Weather);
        writer.WritePropertyName("world");
        writer.WriteRawValue(WriteWorld(state));
        CampaignSnapshotSerializer.WriteRandomState(writer, state.Stage.Weather.RandomState);
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
        writer.WriteString("firstActingSide", CampaignSnapshotSerializer.FormatSide(state.FirstActingSide));
        writer.WriteStartArray("members");
        foreach (var member in state.Members)
        {
            writer.WriteStartObject();
            writer.WriteStartObject("unit");
            writer.WriteString("creationBinding", member.Unit.CreationBinding);
            writer.WriteString("originalSide", member.Unit.OriginalSide);
            writer.WriteString("elementId", member.Unit.ElementId);
            writer.WriteEndObject();
            writer.WriteString("status", ReserveStatus(member.Status));
            writer.WriteNumber("baseCpa", member.BaseCpa);
            writer.WritePropertyName("spentCp");
            Cna.Core.Rules.CapabilityPointAmountCodec.WriteCanonical(writer, member.SpentCp);
            writer.WriteStartObject("history");
            writer.WriteStartObject("scope");
            writer.WriteNumber("gameTurn", member.History.Scope.GameTurn);
            writer.WriteNumber("operationStage", member.History.Scope.OperationStage);
            writer.WriteString("playerPhaseSlot", member.History.Scope.PlayerPhaseSlot);
            writer.WriteString("actingSide", CampaignSnapshotSerializer.FormatSide(member.History.Scope.ActingSide));
            writer.WriteEndObject();
            writer.WriteString("designationReceiptId", member.History.DesignationReceiptId);
            writer.WriteNull("conversionReceiptId");
            writer.WriteNull("releasedType");
            writer.WriteNull("releaseReceiptId");
            writer.WriteNull("releaseCycle");
            writer.WriteNull("cpaBasis");
            writer.WriteNull("voluntaryCeiling");
            writer.WriteNull("offensiveCommitmentId");
            writer.WriteNull("nextMovement");
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteNull("cycle");
        writer.WriteNull("cycleId");
        writer.WriteNull("openingBaseHash");
        writer.WriteNull("completionReceiptId");
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
    private static void WriteInput(Utf8JsonWriter writer, CampaignCombatReserveInput input)
    {
        writer.WriteStartObject();
        writer.WriteStartObject("command");
        writer.WriteNumber("contractVersion", input.Command.ContractVersion);
        writer.WriteString("kind", input.Command.Kind);
        writer.WriteString("creationBinding", input.Command.CreationBinding);
        writer.WriteString("creationEventHash", input.Command.CreationEventHash);
        writer.WriteNumber("expectedPriorVersion", input.Command.ExpectedPriorVersion);
        writer.WriteString("expectedPositionId", input.Command.ExpectedPositionId);
        writer.WriteString("elementId", input.Command.ElementId);
        writer.WriteEndObject();
        writer.WriteString("actor", Actor(input.Actor));
        writer.WriteEndObject();
    }
    private static void ValidateInput(CampaignCombatReserveInput input)
    {
        try
        {
            if (input is null || input.Command is null || input.Command.ContractVersion != 2 ||
                !Enum.IsDefined(input.Actor) ||
                input.Command.ExpectedPriorVersion < 1 || input.Command.Kind != "designate-reserve-element")
                throw new JsonException("Unsupported Reserve designation input identity.");
            _ = ContentContractGuards.RequireSourceAtom(input.Command.ElementId, nameof(input.Command.ElementId));
            _ = ContentContractGuards.RequireSourceAtom(input.Command.CreationBinding, nameof(input.Command.CreationBinding));
            _ = ContentContractGuards.RequireSha256(input.Command.CreationEventHash, nameof(input.Command.CreationEventHash));
            _ = ContentContractGuards.RequireSourceAtom(input.Command.ExpectedPositionId, nameof(input.Command.ExpectedPositionId));
        }
        catch (ArgumentException error) { throw new JsonException("Invalid Reserve designation input primitive.", error); }
    }
    private static byte[] Bytes(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) write(writer);
        if (stream.Length > 1_048_576) throw new JsonException("Reserve designation record exceeds byte limit.");
        return stream.ToArray();
    }
    private static JsonDocument Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Reserve designation record missing or exceeds byte limit.");
        return JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
    }
    private static string Actor(CampaignOpeningPreambleActor actor) => actor switch
    {
        CampaignOpeningPreambleActor.System => "system",
        CampaignOpeningPreambleActor.Axis => "axis",
        CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
        _ => throw new JsonException("Unknown Reserve designation actor."),
    };
    private static CampaignOpeningPreambleActor ReadActor(string? actor) => actor switch
    {
        "system" => CampaignOpeningPreambleActor.System,
        "axis" => CampaignOpeningPreambleActor.Axis,
        "commonwealth" => CampaignOpeningPreambleActor.Commonwealth,
        _ => throw new JsonException("Unknown Reserve designation actor."),
    };
    private static byte[] WriteWorld(CampaignCombatReserveState state)
    {
        // This writer handles only the history-derived initial/none-to-I World profile.
        // Equality protects hardcoded absent fields; it never admits an arbitrary World7.
        var world = CampaignCombatReserveDesignation.ExpectedWorld(state);
        if (state.World != world) throw new JsonException("Reserve World differs from history-derived designation.");
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
                writer.WriteString("reserveStatus", ReserveStatus(
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
            WriteEmptyArray(writer, "cohesionCauses");
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
    private static string ReserveStatus(CampaignElementReserveStatus status) => status switch
    {
        CampaignElementReserveStatus.None => "none",
        CampaignElementReserveStatus.ReserveI => "I",
        _ => throw new JsonException("Status outside precompletion Reserve profile."),
    };
}
