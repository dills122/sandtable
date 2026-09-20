using System.Text;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatStageEntryCodec
{
    public static byte[] SerializeInput(CampaignCombatStageEntryInput input)
    {
        ValidateInput(input);
        return Bytes(writer => WriteInput(writer, input));
    }

    public static CampaignCombatStageEntryInput DeserializeInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes);
            var root = document.RootElement;
            var command = root.GetProperty("command");
            var result = new CampaignCombatStageEntryInput(new CampaignCombatStageEntryCommand(
                command.GetProperty("contractVersion").GetInt32(), command.GetProperty("kind").GetString()!,
                command.GetProperty("creationBinding").GetString()!, command.GetProperty("creationEventHash").GetString()!,
                command.GetProperty("expectedPriorVersion").GetInt64(), command.GetProperty("expectedPositionId").GetString()!),
                ReadActor(root.GetProperty("actor").GetString()));
            if (!bytes.SequenceEqual(SerializeInput(result))) throw new JsonException("Noncanonical stage entry input.");
            return result;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException
            or FormatException or OverflowException)
        { throw new JsonException("Invalid stage entry input.", error); }
    }

    internal static CampaignCombatStageEntryInput ReadEventInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes);
            var root = document.RootElement;
            if (root.GetProperty("contractVersion").GetInt32() != 2)
                throw new JsonException("Unsupported stage entry event version.");
            var input = DeserializeInput(Encoding.UTF8.GetBytes(root.GetProperty("input").GetRawText()));
            if (root.GetProperty("eventType").GetString() != CampaignCombatStageEntry.Edge(input.Command.Kind).EventType)
                throw new JsonException("Stage entry input/event kind mismatch.");
            return input;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException
            or FormatException or OverflowException)
        { throw new JsonException("Invalid stage entry event.", error); }
    }

    internal static byte[] SerializeEvent(CampaignCombatStageEntryEvent value, bool includeReceipt = true) => Bytes(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 2);
        writer.WriteString("eventType", CampaignCombatStageEntry.Edge(value.Input.Command.Kind).EventType);
        WriteAuthority(writer, value.Before);
        writer.WriteNumber("priorVersion", value.Before.StateVersion);
        writer.WriteNumber("stateVersion", value.StateVersion);
        writer.WriteString("priorPrefix", value.Before.Prefix);
        writer.WriteString("fromPositionId", value.Before.SequencePosition.PositionId);
        writer.WritePropertyName("input");
        WriteInput(writer, value.Input);
        writer.WriteNumber("gameTurn", 1);
        writer.WriteNumber("operationStage", 1);
        CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", value.SequencePosition);
        CampaignSnapshotSerializer.WriteSources(writer, value.Sources);
        if (includeReceipt) writer.WriteString("receiptId", value.ReceiptId);
        writer.WriteEndObject();
    });

    public static byte[] SerializeState(CampaignCombatStageEntryState state) => Bytes(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 1);
        WriteAuthority(writer, state);
        writer.WriteNumber("stateVersion", state.StateVersion);
        writer.WriteString("prefix", state.Prefix);
        CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", state.SequencePosition);
        writer.WriteString("initiativeHolder", CampaignSnapshotSerializer.FormatSide(state.Weather.Opening.InitiativeHolder!.Value));
        writer.WriteStartArray("operationStageOrders");
        foreach (var order in state.Weather.Opening.Orders)
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
        CampaignOperationStageWeatherCodec.Write(writer, state.Weather.Weather);
        var creation = state.Weather.Opening.Creation;
        writer.WritePropertyName("world");
        writer.WriteRawValue(CampaignWorldV7InitialCodec.Serialize(creation.World,
            creation.Setup.Artifact, creation.Setup.Scenario, creation.Setup.CombatInitialization));
        CampaignSnapshotSerializer.WriteRandomState(writer, state.Weather.RandomState);
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
        writer.WriteEndObject();
    });

    private static void WriteAuthority(Utf8JsonWriter writer, CampaignCombatStageEntryState state)
    {
        var creation = state.Weather.Opening.Creation;
        writer.WriteString("campaignId", creation.CampaignId);
        writer.WriteString("rulesetHash", creation.RulesetHash);
        writer.WriteString("configurationHash", creation.Configuration.Hash);
        writer.WriteString("creationBinding", creation.CreationReceipt.CreationBinding);
        writer.WriteString("creationEventHash", creation.CreationReceipt.CreationEventHash);
    }
    private static void WriteInput(Utf8JsonWriter writer, CampaignCombatStageEntryInput input)
    {
        writer.WriteStartObject();
        writer.WriteStartObject("command");
        writer.WriteNumber("contractVersion", input.Command.ContractVersion);
        writer.WriteString("kind", input.Command.Kind);
        writer.WriteString("creationBinding", input.Command.CreationBinding);
        writer.WriteString("creationEventHash", input.Command.CreationEventHash);
        writer.WriteNumber("expectedPriorVersion", input.Command.ExpectedPriorVersion);
        writer.WriteString("expectedPositionId", input.Command.ExpectedPositionId);
        writer.WriteEndObject();
        writer.WriteString("actor", Actor(input.Actor));
        writer.WriteEndObject();
    }
    private static void ValidateInput(CampaignCombatStageEntryInput input)
    {
        try
        {
            if (input is null || input.Command is null || input.Command.ContractVersion != 2 ||
                !Enum.IsDefined(input.Actor) ||
                input.Command.ExpectedPriorVersion < 1)
                throw new JsonException("Unsupported stage entry input identity.");
            _ = CampaignCombatStageEntry.Edge(input.Command.Kind);
            _ = ContentContractGuards.RequireSourceAtom(input.Command.CreationBinding, nameof(input.Command.CreationBinding));
            _ = ContentContractGuards.RequireSha256(input.Command.CreationEventHash, nameof(input.Command.CreationEventHash));
            _ = ContentContractGuards.RequireSourceAtom(input.Command.ExpectedPositionId, nameof(input.Command.ExpectedPositionId));
        }
        catch (ArgumentException error) { throw new JsonException("Invalid stage entry input primitive.", error); }
    }
    private static byte[] Bytes(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) write(writer);
        if (stream.Length > 1_048_576) throw new JsonException("Stage entry record exceeds byte limit.");
        return stream.ToArray();
    }
    private static JsonDocument Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Stage entry record missing or exceeds byte limit.");
        return JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
    }
    private static string Actor(CampaignOpeningPreambleActor actor) => actor switch
    {
        CampaignOpeningPreambleActor.System => "system",
        CampaignOpeningPreambleActor.Axis => "axis",
        CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
        _ => throw new JsonException("Unknown stage entry actor."),
    };
    private static CampaignOpeningPreambleActor ReadActor(string? actor) => actor switch
    {
        "system" => CampaignOpeningPreambleActor.System,
        "axis" => CampaignOpeningPreambleActor.Axis,
        "commonwealth" => CampaignOpeningPreambleActor.Commonwealth,
        _ => throw new JsonException("Unknown stage entry actor."),
    };
}
