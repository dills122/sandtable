using System.Text;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatWeatherCodec
{
    public static byte[] SerializeInput(CampaignCombatWeatherInput input)
    {
        ValidateInput(input);
        return Bytes(writer => WriteInput(writer, input));
    }

    public static CampaignCombatWeatherInput DeserializeInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes);
            var root = document.RootElement;
            var command = root.GetProperty("command");
            var result = new CampaignCombatWeatherInput(new CampaignCombatWeatherCommand(
                command.GetProperty("contractVersion").GetInt32(), command.GetProperty("kind").GetString()!,
                command.GetProperty("creationBinding").GetString()!, command.GetProperty("creationEventHash").GetString()!,
                command.GetProperty("expectedPriorVersion").GetInt64(), command.GetProperty("expectedPositionId").GetString()!),
                ReadActor(root.GetProperty("actor").GetString()));
            if (!bytes.SequenceEqual(SerializeInput(result))) throw new JsonException("Noncanonical Weather input.");
            return result;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException
            or FormatException or OverflowException)
        { throw new JsonException("Invalid Weather input.", error); }
    }

    internal static CampaignCombatWeatherInput ReadEventInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes);
            var root = document.RootElement;
            if (root.GetProperty("contractVersion").GetInt32() != 2 ||
                root.GetProperty("eventType").GetString() != "weather-determined")
                throw new JsonException("Unsupported Weather event kind/version.");
            return DeserializeInput(Encoding.UTF8.GetBytes(root.GetProperty("input").GetRawText()));
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException
            or FormatException or OverflowException)
        { throw new JsonException("Invalid Weather event.", error); }
    }

    internal static byte[] SerializeEvent(CampaignCombatWeatherEvent value, bool includeReceipt = true) => Bytes(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 2);
        writer.WriteString("eventType", "weather-determined");
        WriteAuthority(writer, value.Before);
        writer.WriteNumber("priorVersion", 5);
        writer.WriteNumber("stateVersion", 6);
        writer.WriteString("priorPrefix", value.Before.Prefix);
        writer.WriteString("fromPositionId", value.Before.SequencePosition.PositionId);
        writer.WritePropertyName("input");
        WriteInput(writer, value.Input);
        WriteWeatherFields(writer, value.Weather);
        writer.WriteString("randomAlgorithmId", value.RandomState.AlgorithmId);
        writer.WriteNumber("randomCursorBefore", value.Before.RandomState.NextByteCursor);
        writer.WriteNumber("randomCursorAfter", value.RandomState.NextByteCursor);
        CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", value.SequencePosition);
        CampaignSnapshotSerializer.WriteSources(writer, value.Sources);
        if (includeReceipt) writer.WriteString("receiptId", value.ReceiptId);
        writer.WriteEndObject();
    });

    public static byte[] SerializeState(CampaignCombatWeatherState state) => Bytes(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 1);
        WriteAuthority(writer, state);
        writer.WriteNumber("stateVersion", state.StateVersion);
        writer.WriteString("prefix", state.Prefix);
        CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", state.SequencePosition);
        writer.WriteString("initiativeHolder", CampaignSnapshotSerializer.FormatSide(state.Opening.InitiativeHolder!.Value));
        writer.WriteStartArray("operationStageOrders");
        foreach (var order in state.Opening.Orders)
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
        CampaignOperationStageWeatherCodec.Write(writer, state.Weather);
        var creation = state.Opening.Creation;
        writer.WritePropertyName("world");
        writer.WriteRawValue(CampaignWorldV7InitialCodec.Serialize(creation.World,
            creation.Setup.Artifact, creation.Setup.Scenario, creation.Setup.CombatInitialization));
        CampaignSnapshotSerializer.WriteRandomState(writer, state.RandomState);
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

    private static void WriteWeatherFields(Utf8JsonWriter writer, CampaignOperationStageWeather value)
    {
        writer.WriteNumber("gameTurn", value.GameTurn);
        writer.WriteNumber("operationStage", value.OperationStage);
        writer.WriteString("determiningSide", CampaignSnapshotSerializer.FormatSide(value.DeterminingSide));
        writer.WriteString("season", CampaignOperationStageWeatherCodec.FormatSeason(value.Season));
        writer.WriteNumber("firstDie", value.FirstDie);
        writer.WriteNumber("secondDie", value.SecondDie);
        writer.WriteString("kind", CampaignOperationStageWeatherCodec.FormatKind(value.Kind));
        writer.WriteString("scope", CampaignOperationStageWeatherCodec.FormatScope(value.Scope));
        if (value.LocationDie is { } location) writer.WriteNumber("locationDie", location);
        else writer.WriteNull("locationDie");
        writer.WriteStartArray("affectedAreas");
        foreach (var area in value.AffectedAreas) writer.WriteStringValue(CampaignOperationStageWeatherCodec.FormatArea(area));
        writer.WriteEndArray();
        writer.WriteNumber("fuelWaterReductionSubjectCount", value.FuelWaterReductionSubjectCount);
        writer.WriteNumber("restoredWellCount", value.RestoredWellCount);
        writer.WriteNumber("damagedGroundedAircraftCount", value.DamagedGroundedAircraftCount);
    }

    private static void WriteAuthority(Utf8JsonWriter writer, CampaignCombatWeatherState state)
    {
        var creation = state.Opening.Creation;
        writer.WriteString("campaignId", creation.CampaignId);
        writer.WriteString("rulesetHash", creation.RulesetHash);
        writer.WriteString("configurationHash", creation.Configuration.Hash);
        writer.WriteString("creationBinding", creation.CreationReceipt.CreationBinding);
        writer.WriteString("creationEventHash", creation.CreationReceipt.CreationEventHash);
    }
    private static void WriteInput(Utf8JsonWriter writer, CampaignCombatWeatherInput input)
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
    private static void ValidateInput(CampaignCombatWeatherInput input)
    {
        try
        {
            if (input is null || input.Command is null || input.Command.ContractVersion != 2 ||
                input.Command.Kind != "resolve-weather" || !Enum.IsDefined(input.Actor) ||
                input.Command.ExpectedPriorVersion < 1)
                throw new JsonException("Unsupported Weather input identity.");
            _ = ContentContractGuards.RequireSourceAtom(input.Command.CreationBinding, nameof(input.Command.CreationBinding));
            _ = ContentContractGuards.RequireSha256(input.Command.CreationEventHash, nameof(input.Command.CreationEventHash));
            _ = ContentContractGuards.RequireSourceAtom(input.Command.ExpectedPositionId, nameof(input.Command.ExpectedPositionId));
        }
        catch (ArgumentException error) { throw new JsonException("Invalid Weather input primitive.", error); }
    }
    private static byte[] Bytes(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) write(writer);
        if (stream.Length > 1_048_576) throw new JsonException("Weather record exceeds byte limit.");
        return stream.ToArray();
    }
    private static JsonDocument Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Weather record missing or exceeds byte limit.");
        return JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
    }
    private static string Actor(CampaignOpeningPreambleActor actor) => actor switch
    {
        CampaignOpeningPreambleActor.System => "system",
        CampaignOpeningPreambleActor.Axis => "axis",
        CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
        _ => throw new JsonException("Unknown Weather actor."),
    };
    private static CampaignOpeningPreambleActor ReadActor(string? actor) => actor switch
    {
        "system" => CampaignOpeningPreambleActor.System,
        "axis" => CampaignOpeningPreambleActor.Axis,
        "commonwealth" => CampaignOpeningPreambleActor.Commonwealth,
        _ => throw new JsonException("Unknown Weather actor."),
    };
}
