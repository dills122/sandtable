using System.Text;
using System.Text.Json;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatInheritedReserveCycle;

namespace Cna.Core.Campaigns;

/// <summary>Frozen inherited-reserve-cycle-v1 canonical bytes; parsed records never supply authority.</summary>
internal static class CampaignCombatInheritedReserveCycleCodec
{
    public static byte[] SerializeInput(Input input) => Bytes(w => WriteInput(w, input));
    private static void WriteInput(Utf8JsonWriter w, Input input)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentNullException.ThrowIfNull(input.Command);
        var c = input.Command;
        w.WriteStartObject(); w.WriteStartObject("command"); w.WriteNumber("contractVersion", c.ContractVersion);
        w.WriteString("kind", c.Kind); w.WriteString("creationBinding", c.CreationBinding); w.WriteString("creationEventHash", c.CreationEventHash);
        w.WriteString("cycleId", c.CycleId); w.WriteNumber("expectedPriorVersion", c.ExpectedPriorVersion);
        w.WriteString("expectedPositionId", c.ExpectedPositionId); w.WriteString("priorReceiptId", c.PriorReceiptId);
        w.WriteEndObject(); w.WriteString("actor", Actor(input.Actor)); w.WriteEndObject();
    }
    public static Input ReadEventInput(byte[] bytes)
    {
        var retained = CopyRecord(bytes);
        try
        {
            using var document = JsonDocument.Parse(retained, new JsonDocumentOptions { MaxDepth = 32 });
            var input = document.RootElement.GetProperty("input"); var c = input.GetProperty("command");
            var value = new Input(new(c.GetProperty("contractVersion").GetInt32(), c.GetProperty("kind").GetString()!,
                c.GetProperty("creationBinding").GetString()!, c.GetProperty("creationEventHash").GetString()!, c.GetProperty("cycleId").GetString()!,
                c.GetProperty("expectedPriorVersion").GetInt64(), c.GetProperty("expectedPositionId").GetString()!, c.GetProperty("priorReceiptId").GetString()!),
                input.GetProperty("actor").GetString() switch
                {
                    "system" => CampaignOpeningPreambleActor.System,
                    "axis" => CampaignOpeningPreambleActor.Axis,
                    "commonwealth" => CampaignOpeningPreambleActor.Commonwealth,
                    _ => throw new JsonException("Unknown held-I actor."),
                });
            if (!Encoding.UTF8.GetBytes(input.GetRawText()).AsSpan().SequenceEqual(SerializeInput(value)))
                throw new JsonException("Noncanonical held-I input.");
            return value;
        }
        catch (Exception e) when (e is ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { throw new JsonException("Malformed held-I event input.", e); }
    }
    internal static byte[] SerializeEvent(State state, Input input, string? receipt) => Bytes(w =>
    {
        var basis = state.Base; var creation = basis.Predecessor.Stage.Weather.Opening.Creation; var index = state.Receipts.Count;
        var type = index switch
        {
            0 => "movement-segment-completed",
            1 => "breakdown-segment-completed",
            2 => "combat-selection-opened",
            3 => "combat-selection-closed",
            _ => "combat-step-completed"
        };
        w.WriteStartObject(); w.WriteNumber("contractVersion", index == 0 ? 3 : 2); w.WriteString("eventType", type);
        w.WriteString("campaignId", creation.CampaignId); w.WriteString("rulesetHash", creation.RulesetHash);
        w.WriteString("configurationHash", creation.Configuration.Hash); w.WriteString("creationBinding", creation.CreationReceipt.CreationBinding);
        w.WriteString("creationEventHash", creation.CreationReceipt.CreationEventHash); w.WriteString("cycleId", basis.CycleId);
        w.WriteString("openingBaseHash", basis.OpeningBaseHash); w.WriteString("openingCompletionReceiptId", basis.CompletionReceiptId);
        w.WriteNumber("gameTurn", basis.Cycle!.GameTurn); w.WriteNumber("operationStage", basis.Cycle.OperationStage);
        w.WriteString("actingSide", CampaignSnapshotSerializer.FormatSide(basis.Cycle.ActingSide));
        w.WriteNumber("priorVersion", state.StateVersion); w.WriteNumber("stateVersion", state.StateVersion + 1);
        w.WriteString("fromPositionId", state.Position.PositionId); CampaignV11CanonicalCodec.WritePosition(w, "sequencePosition", PositionAfter(index + 1));
        w.WriteStartArray("sources");
        foreach (var source in state.Position.Sources)
        { w.WriteStartObject(); w.WriteString("sourceId", source.SourceId); w.WriteString("locator", source.Locator); w.WriteEndObject(); }
        w.WriteEndArray(); w.WriteString("priorPrefix", state.Prefix); w.WritePropertyName("input"); WriteInput(w, input);
        w.WriteStartObject("effect");
        switch (index)
        {
            case 0:
                w.WriteString("kind", "movement-completed"); Idle(w, "breakdownFlowAfter"); w.WriteNull("interruptContextAfter");
                Locations(w, state.EndLocations); Empty(w, "excludedUnits"); Empty(w, "progress"); break;
            case 1:
                w.WriteString("kind", "breakdown-completed"); Idle(w, "breakdownFlowAfter");
                w.WriteString("movementCompletionReceiptId", state.Receipts[0].ReceiptId); break;
            case 2:
                w.WriteString("kind", "selection-opened"); w.WriteNumber("candidateCount", 0); w.WriteNull("decisionId"); break;
            case 3:
                w.WriteString("kind", "selection-closed"); w.WriteString("outcome", "no-selection");
                w.WriteString("openingReceiptId", state.Receipts[2].ReceiptId); break;
            default:
                w.WriteString("kind", "step-completed"); w.WriteString("fromPositionId", state.Position.PositionId);
                w.WriteString("toPositionId", PositionAfter(index + 1).PositionId);
                w.WriteString("previousStepReceiptId", state.Receipts[index == 4 ? 2 : index - 1].ReceiptId);
                w.WriteString("dispositionReceiptId", state.Receipts[3].ReceiptId); w.WriteString("proofKind", "no-attack"); break;
        }
        w.WriteEndObject(); if (receipt is not null) w.WriteString("receiptId", receipt); w.WriteEndObject();
    });
    public static byte[] SerializeControl(State state) => Bytes(w =>
    {
        w.WriteStartObject(); w.WriteNumber("contractVersion", 1); w.WritePropertyName("base");
        w.WriteRawValue(CampaignCombatReserveOpeningCodec.SerializeState(state.Base)); w.WriteString("baseHash", state.BaseHash);
        w.WriteNumber("stateVersion", state.StateVersion); w.WriteString("prefix", state.Prefix);
        CampaignV11CanonicalCodec.WritePosition(w, "position", state.Position); Idle(w, "breakdownFlow");
        w.WritePropertyName("movementEnd");
        if (state.MovementEnd is not { } proof) w.WriteNullValue();
        else
        {
            w.WriteStartObject(); w.WriteStartObject("scope"); w.WriteNumber("gameTurn", proof.Scope.GameTurn);
            w.WriteNumber("operationStage", proof.Scope.OperationStage); w.WriteString("playerPhaseSlot", proof.Scope.PlayerPhaseSlot);
            w.WriteString("actingSide", CampaignSnapshotSerializer.FormatSide(proof.Scope.ActingSide)); w.WriteEndObject();
            w.WriteNumber("ordinal", proof.Ordinal); w.WriteString("completionReceiptId", proof.CompletionReceiptId);
            Locations(w, proof.EndLocations); Empty(w, "excludedBefore"); w.WriteEndObject();
        }
        var count = state.Receipts.Count;
        w.WriteStartObject("selection"); w.WriteString("segmentId", state.SegmentId); Empty(w, "candidateIds");
        w.WriteString("openingReceiptId", count >= 3 ? state.Receipts[2].ReceiptId : null);
        w.WriteString("selectionReceiptId", count >= 4 ? state.Receipts[3].ReceiptId : null);
        w.WriteString("outcome", count < 3 ? "unopened" : count == 3 ? "system-no-selection" : "no-selection"); w.WriteEndObject();
        w.WriteNumber("stepIndex", state.StepIndex); w.WriteStartArray("stepReceipts");
        foreach (var r in state.Receipts.Skip(4)) w.WriteStringValue(r.ReceiptId);
        w.WriteEndArray(); w.WriteStartArray("receipts");
        foreach (var r in state.Receipts)
        {
            w.WriteStartObject(); w.WriteString("commandHash", r.CommandHash); w.WriteString("eventHash", r.EventHash);
            w.WriteString("receiptId", r.ReceiptId); w.WriteString("actor", Actor(r.Actor)); w.WriteNumber("stateVersion", r.StateVersion); w.WriteEndObject();
        }
        w.WriteEndArray(); w.WriteBoolean("closed", state.Closed); w.WriteEndObject();
    });
    private static void Locations(Utf8JsonWriter w, IReadOnlyList<CampaignCombatEndLocation> locations)
    {
        w.WriteStartArray("endLocations");
        foreach (var location in locations)
        {
            w.WriteStartObject(); w.WriteStartObject("unit"); w.WriteString("creationBinding", location.Unit.CreationBinding);
            w.WriteString("originalSide", location.Unit.OriginalSide); w.WriteString("elementId", location.Unit.ElementId); w.WriteEndObject();
            w.WriteString("locationId", location.LocationId); w.WriteEndObject();
        }
        w.WriteEndArray();
    }
    private static void Idle(Utf8JsonWriter w, string name) { w.WriteStartObject(name); w.WriteString("kind", "idle"); w.WriteEndObject(); }
    private static void Empty(Utf8JsonWriter w, string name) { w.WriteStartArray(name); w.WriteEndArray(); }
    private static string Actor(CampaignOpeningPreambleActor actor) => actor switch
    {
        CampaignOpeningPreambleActor.Axis => "axis",
        CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
        CampaignOpeningPreambleActor.System => "system",
        _ => throw new JsonException("Unknown held-I actor."),
    };
    internal static byte[] CopyRecord(byte[] bytes) => bytes is { Length: > 0 and <= 1_048_576 }
        ? bytes.ToArray() : throw new JsonException("Held-I cycle record exceeds byte bounds.");
    private static byte[] Bytes(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream(); using (var writer = new Utf8JsonWriter(stream)) write(writer);
        if (stream.Length > 1_048_576) throw new JsonException("Held-I cycle record exceeds byte bounds.");
        return stream.ToArray();
    }
}
