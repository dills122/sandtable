using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignOpeningPreambleCodec
{
    public static byte[] SerializeInput(CampaignOpeningPreambleInput input)
    {
        ValidateInput(input);
        return Bytes(writer => WriteInput(writer, input));
    }

    public static CampaignOpeningPreambleInput DeserializeInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes);
            var root = document.RootElement;
            var command = root.GetProperty("command");
            var value = new CampaignOpeningPreambleInput(new CampaignOpeningPreambleCommand(
                command.GetProperty("contractVersion").GetInt32(), ParseKind(command.GetProperty("kind").GetString()),
                command.GetProperty("creationBinding").GetString()!, command.GetProperty("creationEventHash").GetString()!,
                command.GetProperty("expectedPriorVersion").GetInt64(), command.GetProperty("expectedPositionId").GetString()!,
                command.GetProperty("operationStage").ValueKind == JsonValueKind.Null ? null : command.GetProperty("operationStage").GetInt32(),
                ReadSide(command.GetProperty("declaringSide")), ReadChoice(command.GetProperty("choice"))),
                root.GetProperty("actor").GetString() switch
                {
                    "system" => CampaignOpeningPreambleActor.System,
                    "axis" => CampaignOpeningPreambleActor.Axis,
                    "commonwealth" => CampaignOpeningPreambleActor.Commonwealth,
                    _ => throw new JsonException("Unknown opening actor."),
                });
            if (!bytes.SequenceEqual(SerializeInput(value))) throw new JsonException("Noncanonical opening input.");
            return value;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException
            or FormatException or OverflowException)
        { throw new JsonException("Invalid opening input.", error); }
    }

    internal static CampaignOpeningPreambleInput ReadEventInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes);
            var root = document.RootElement;
            var kind = root.GetProperty("eventType").GetString() switch
            {
                "initiative-determined" => CampaignOpeningCommandKind.Initiative,
                "no-obligation-naval-convoy-schedule-resolved" => CampaignOpeningCommandKind.ConvoySchedule,
                "no-obligation-tactical-shipping-resolved" => CampaignOpeningCommandKind.TacticalShipping,
                "initiative-order-declared" => CampaignOpeningCommandKind.InitiativeOrder,
                _ => throw new JsonException("Unsupported opening event."),
            };
            if (root.GetProperty("contractVersion").GetInt32() != Version(kind))
                throw new JsonException("Unsupported opening event version.");
            var input = DeserializeInput(Encoding.UTF8.GetBytes(root.GetProperty("input").GetRawText()));
            if (input.Command.Kind != kind) throw new JsonException("Opening input/event kind mismatch.");
            return input;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException
            or FormatException or OverflowException)
        { throw new JsonException("Invalid opening event.", error); }
    }

    internal static byte[] SerializeEvent(CampaignOpeningPreambleEvent value, bool includeReceipt = true) => Bytes(writer =>
    {
        var state = value.Before;
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", Version(value.Input.Command.Kind));
        writer.WriteString("eventType", EventType(value.Input.Command.Kind));
        WriteAuthority(writer, state);
        writer.WriteNumber("priorVersion", state.StateVersion);
        writer.WriteNumber("stateVersion", value.StateVersion);
        writer.WriteString("priorPrefix", state.Prefix);
        writer.WriteString("fromPositionId", state.SequencePosition.PositionId);
        writer.WritePropertyName("input");
        WriteInput(writer, value.Input);
        switch (value)
        {
            case CampaignOpeningInitiativeEvent initiative:
                writer.WriteStartObject("outcome");
                writer.WriteString("kind", "predetermined");
                writer.WriteString("holder", Side(initiative.Holder));
                writer.WriteEndObject();
                writer.WriteString("randomAlgorithmId", state.Creation.RandomState.AlgorithmId);
                writer.WriteNumber("randomCursorBefore", state.Creation.RandomState.NextByteCursor);
                writer.WriteNumber("randomCursorAfter", state.Creation.RandomState.NextByteCursor);
                break;
            case CampaignOpeningOrderEvent order:
                writer.WriteNumber("operationStage", 1);
                writer.WriteString("declaringHolder", Side(order.DeclaringHolder));
                writer.WriteString("firstSide", Side(order.FirstSide));
                writer.WriteString("secondSide", Side(order.SecondSide));
                break;
            case CampaignOpeningAdvanceEvent:
                break;
            default: throw new JsonException("Unknown opening payload arm.");
        }
        CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", value.SequencePosition);
        CampaignSnapshotSerializer.WriteSources(writer, value.Sources);
        if (includeReceipt) writer.WriteString("receiptId", value.ReceiptId);
        writer.WriteEndObject();
    });

    public static byte[] SerializeState(CampaignOpeningPreambleState state) => Bytes(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 1);
        WriteAuthority(writer, state);
        writer.WriteNumber("stateVersion", state.StateVersion);
        writer.WriteString("prefix", state.Prefix);
        CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", state.SequencePosition);
        if (state.InitiativeHolder is { } holder) writer.WriteString("initiativeHolder", Side(holder));
        else writer.WriteNull("initiativeHolder");
        writer.WriteStartArray("operationStageOrders");
        foreach (var order in state.Orders)
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteNumber("gameTurn", order.GameTurn);
            writer.WriteNumber("operationStage", order.OperationStage);
            writer.WriteString("firstSide", Side(order.FirstSide));
            writer.WriteString("secondSide", Side(order.SecondSide));
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WritePropertyName("world");
        writer.WriteRawValue(CampaignWorldV7InitialCodec.Serialize(state.Creation.World,
            state.Creation.Setup.Artifact, state.Creation.Setup.Scenario, state.Creation.Setup.CombatInitialization));
        CampaignSnapshotSerializer.WriteRandomState(writer, state.Creation.RandomState);
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

    private static void WriteAuthority(Utf8JsonWriter writer, CampaignOpeningPreambleState state)
    {
        writer.WriteString("campaignId", state.Creation.CampaignId);
        writer.WriteString("rulesetHash", state.Creation.RulesetHash);
        writer.WriteString("configurationHash", state.Creation.Configuration.Hash);
        writer.WriteString("creationBinding", state.Creation.CreationReceipt.CreationBinding);
        writer.WriteString("creationEventHash", state.Creation.CreationReceipt.CreationEventHash);
    }

    private static void WriteInput(Utf8JsonWriter writer, CampaignOpeningPreambleInput input)
    {
        var command = input.Command;
        writer.WriteStartObject();
        writer.WriteStartObject("command");
        writer.WriteNumber("contractVersion", command.ContractVersion);
        writer.WriteString("kind", Kind(command.Kind));
        writer.WriteString("creationBinding", command.CreationBinding);
        writer.WriteString("creationEventHash", command.CreationEventHash);
        writer.WriteNumber("expectedPriorVersion", command.ExpectedPriorVersion);
        writer.WriteString("expectedPositionId", command.ExpectedPositionId);
        if (command.OperationStage is { } stage) writer.WriteNumber("operationStage", stage);
        else writer.WriteNull("operationStage");
        if (command.DeclaringSide is { } side) writer.WriteString("declaringSide", Side(side));
        else writer.WriteNull("declaringSide");
        if (command.Choice is { } choice) writer.WriteString("choice", choice == InitiativeOrderChoice.ActFirst ? "act-first" : "act-last");
        else writer.WriteNull("choice");
        writer.WriteEndObject();
        writer.WriteString("actor", Actor(input.Actor));
        writer.WriteEndObject();
    }

    private static void ValidateInput(CampaignOpeningPreambleInput input)
    {
        try
        {
            if (input is null || input.Command is null) throw new JsonException("Missing opening input.");
            var command = input.Command;
            if (!Enum.IsDefined(command.Kind) || !Enum.IsDefined(input.Actor) || command.ContractVersion != Version(command.Kind) ||
                command.ExpectedPriorVersion is < 1 or > 4 ||
                (command.Choice is { } choice && !Enum.IsDefined(choice)) ||
                (command.DeclaringSide is { } side && !Enum.IsDefined(side)))
                throw new JsonException("Unsupported opening command identity or value.");
            _ = ContentContractGuards.RequireSourceAtom(command.CreationBinding, nameof(command.CreationBinding));
            _ = ContentContractGuards.RequireSha256(command.CreationEventHash, nameof(command.CreationEventHash));
            _ = ContentContractGuards.RequireSourceAtom(command.ExpectedPositionId, nameof(command.ExpectedPositionId));
        }
        catch (ArgumentException error) { throw new JsonException("Invalid opening command primitive.", error); }
    }

    internal static string Hash(ReadOnlySpan<byte> bytes) => "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    internal static string HashWithDomain(string domain, ReadOnlySpan<byte> bytes)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(Encoding.ASCII.GetBytes(domain));
        hash.AppendData([0]);
        hash.AppendData(bytes);
        return "sha256:" + Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }
    internal static string EventPrefix(string prior, ReadOnlySpan<byte> bytes)
    {
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData("sandtable.cycle.prefix.event.v1\0"u8);
        hash.AppendData(Convert.FromHexString(prior[7..]));
        Span<byte> length = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(length, checked((ulong)bytes.Length));
        hash.AppendData(length);
        hash.AppendData(bytes);
        return "sha256:" + Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();
    }
    private static byte[] Bytes(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) write(writer);
        if (stream.Length > 1_048_576) throw new JsonException("Opening record exceeds byte limit.");
        return stream.ToArray();
    }
    private static JsonDocument Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Opening record is missing or exceeds byte limit.");
        return JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
    }
    private static int Version(CampaignOpeningCommandKind kind) => kind == CampaignOpeningCommandKind.Initiative ? 3 : 2;
    private static string Kind(CampaignOpeningCommandKind kind) => kind switch
    {
        CampaignOpeningCommandKind.Initiative => "resolve-initiative",
        CampaignOpeningCommandKind.ConvoySchedule => "resolve-no-obligation-naval-convoy-schedule",
        CampaignOpeningCommandKind.TacticalShipping => "resolve-no-obligation-tactical-shipping",
        CampaignOpeningCommandKind.InitiativeOrder => "declare-initiative-order",
        _ => throw new JsonException("Unknown opening command."),
    };
    private static CampaignOpeningCommandKind ParseKind(string? kind) => kind switch
    {
        "resolve-initiative" => CampaignOpeningCommandKind.Initiative,
        "resolve-no-obligation-naval-convoy-schedule" => CampaignOpeningCommandKind.ConvoySchedule,
        "resolve-no-obligation-tactical-shipping" => CampaignOpeningCommandKind.TacticalShipping,
        "declare-initiative-order" => CampaignOpeningCommandKind.InitiativeOrder,
        _ => throw new JsonException("Unknown opening command."),
    };
    private static string EventType(CampaignOpeningCommandKind kind) => kind switch
    {
        CampaignOpeningCommandKind.Initiative => "initiative-determined",
        CampaignOpeningCommandKind.ConvoySchedule => "no-obligation-naval-convoy-schedule-resolved",
        CampaignOpeningCommandKind.TacticalShipping => "no-obligation-tactical-shipping-resolved",
        CampaignOpeningCommandKind.InitiativeOrder => "initiative-order-declared",
        _ => throw new JsonException("Unknown opening event."),
    };
    private static string Actor(CampaignOpeningPreambleActor actor) => actor switch
    {
        CampaignOpeningPreambleActor.System => "system",
        CampaignOpeningPreambleActor.Axis => "axis",
        CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
        _ => throw new JsonException("Unknown opening actor."),
    };
    private static string Side(LandSide side) => CampaignSnapshotSerializer.FormatSide(side);
    private static LandSide? ReadSide(JsonElement value) => value.ValueKind == JsonValueKind.Null ? null : value.GetString() switch
    {
        "axis" => LandSide.Axis,
        "commonwealth" => LandSide.Commonwealth,
        _ => throw new JsonException("Unknown opening side."),
    };
    private static InitiativeOrderChoice? ReadChoice(JsonElement value) => value.ValueKind == JsonValueKind.Null ? null : value.GetString() switch
    {
        "act-first" => InitiativeOrderChoice.ActFirst,
        "act-last" => InitiativeOrderChoice.ActLast,
        _ => throw new JsonException("Unknown opening choice."),
    };
}
