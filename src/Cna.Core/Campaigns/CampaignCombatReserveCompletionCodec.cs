using System.Buffers.Binary;
using System.Text;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatReserveCompletionCodec
{
    public static byte[] SerializeBase(CampaignCombatOpeningBase basis) => Bytes(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 1);
        // Literal frozen compatibility tag; full history supplies authority, not an isolated seed.
        writer.WriteString("profile", "isolated-first-opening");
        writer.WritePropertyName("creationRequest");
        CampaignCombatCreationRequestCodec.Write(writer, basis.Request);
        writer.WriteString("firstActingSide", CampaignSnapshotSerializer.FormatSide(basis.Predecessor.FirstActingSide));
        CampaignV11CanonicalCodec.WritePosition(writer, "position", basis.Predecessor.SequencePosition);
        writer.WriteNumber("priorVersion", basis.Predecessor.StateVersion);
        writer.WriteString("priorPrefix", basis.Predecessor.Prefix);
        writer.WritePropertyName("world");
        writer.WriteRawValue(CampaignCombatReserveCodec.WriteWorld(basis.Predecessor));
        CampaignSnapshotSerializer.WriteRandomState(writer, basis.Predecessor.Stage.Weather.RandomState);
        CampaignCombatReserveCodec.WriteMembers(writer, basis.Predecessor.Members);
        writer.WriteEndObject();
    });

    public static byte[] SerializeInput(CampaignCombatReserveCompletionInput input)
    {
        try
        {
            if (input is null || input.Command is null || input.Command.ContractVersion != 2 ||
                input.Command.Kind != "complete-reserve-designation" || input.Command.ExpectedPriorVersion < 1 || !Enum.IsDefined(input.Actor))
                throw new JsonException("Unsupported Reserve completion input identity.");
            _ = ContentContractGuards.RequireSha256(input.Command.BaseHash, nameof(input.Command.BaseHash));
            _ = ContentContractGuards.RequireSourceAtom(input.Command.ExpectedPositionId, nameof(input.Command.ExpectedPositionId));
        }
        catch (ArgumentException error) { throw new JsonException("Invalid Reserve completion primitive.", error); }
        return Bytes(writer => WriteInput(writer, input));
    }

    public static CampaignCombatReserveCompletionInput DeserializeInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes);
            var root = document.RootElement;
            var command = root.GetProperty("command");
            var input = new CampaignCombatReserveCompletionInput(new(command.GetProperty("contractVersion").GetInt32(),
                command.GetProperty("kind").GetString()!, command.GetProperty("baseHash").GetString()!,
                command.GetProperty("expectedPriorVersion").GetInt64(), command.GetProperty("expectedPositionId").GetString()!),
                root.GetProperty("actor").GetString() switch
                {
                    "system" => CampaignOpeningPreambleActor.System,
                    "axis" => CampaignOpeningPreambleActor.Axis,
                    "commonwealth" => CampaignOpeningPreambleActor.Commonwealth,
                    _ => throw new JsonException("Unknown completion actor."),
                });
            if (!bytes.SequenceEqual(SerializeInput(input))) throw new JsonException("Noncanonical completion input.");
            return input;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException
            or FormatException or OverflowException)
        { throw new JsonException("Invalid completion input.", error); }
    }

    internal static CampaignCombatReserveCompletionInput ReadEventInput(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = Parse(bytes);
            var root = document.RootElement;
            if (root.GetProperty("contractVersion").GetInt32() != 2 ||
                root.GetProperty("eventType").GetString() != "reserve-designation-completed")
                throw new JsonException("Unsupported Reserve completion event.");
            return DeserializeInput(Encoding.UTF8.GetBytes(root.GetProperty("input").GetRawText()));
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException
            or FormatException or OverflowException)
        { throw new JsonException("Invalid Reserve completion event.", error); }
    }

    internal static byte[] SerializeEvent(CampaignCombatReserveCompletionEvent value, bool includeReceipt = true) => Bytes(writer =>
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", 2);
        writer.WriteString("eventType", "reserve-designation-completed");
        writer.WriteString("campaignId", value.Cycle.CampaignId);
        writer.WriteString("rulesetHash", value.Cycle.RulesetHash);
        writer.WriteString("configurationHash", value.Cycle.AdmittedPolicyBundleDigest);
        writer.WriteString("baseHash", value.OpeningBase.Hash);
        writer.WriteNumber("priorVersion", value.OpeningBase.Predecessor.StateVersion);
        writer.WriteNumber("stateVersion", value.StateVersion);
        writer.WriteString("priorPrefix", value.OpeningBase.Predecessor.Prefix);
        writer.WriteString("fromPositionId", value.OpeningBase.Predecessor.SequencePosition.PositionId);
        CampaignSnapshotSerializer.WriteSources(writer, CampaignCombatReserveCompletion.Sources);
        writer.WritePropertyName("input");
        WriteInput(writer, value.Input);
        CampaignV11CanonicalCodec.WritePosition(writer, "sequencePosition", value.SequencePosition);
        writer.WritePropertyName("cycle");
        WriteCycle(writer, value.Cycle);
        writer.WriteString("cycleId", value.CycleId);
        if (includeReceipt) writer.WriteString("receiptId", value.ReceiptId);
        writer.WriteEndObject();
    });

    internal static string CycleId(CampaignCombatCycleAuthority cycle)
    {
        using var stream = new MemoryStream();
        stream.Write("sandtable.cycle.authority.v1\0"u8);
        U32(stream, checked((uint)cycle.ContractVersion));
        String(stream, cycle.CampaignId);
        String(stream, cycle.RulesetHash);
        String(stream, cycle.SetupId);
        String(stream, cycle.SetupHash);
        String(stream, cycle.ContentPackId);
        String(stream, cycle.ContentHash);
        String(stream, cycle.ScenarioId);
        U64(stream, checked((ulong)cycle.GameTurn));
        U64(stream, checked((ulong)cycle.OperationStage));
        String(stream, cycle.PlayerPhaseSlot);
        String(stream, CampaignSnapshotSerializer.FormatSide(cycle.ActingSide));
        U64(stream, checked((ulong)cycle.Ordinal));
        U64(stream, checked((ulong)cycle.OpenedAuthorityVersion));
        // Only these two fields are raw digests. Rules/setup/content hashes above are length-framed strings.
        stream.Write(Convert.FromHexString(cycle.OpeningPrefix[7..]));
        stream.Write(Convert.FromHexString(cycle.AdmittedPolicyBundleDigest[7..]));
        return CampaignOpeningPreambleCodec.Hash(stream.ToArray());
    }

    private static void WriteCycle(Utf8JsonWriter writer, CampaignCombatCycleAuthority cycle)
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", cycle.ContractVersion);
        writer.WriteString("campaignId", cycle.CampaignId);
        writer.WriteString("rulesetHash", cycle.RulesetHash);
        writer.WriteString("setupId", cycle.SetupId);
        writer.WriteString("setupHash", cycle.SetupHash);
        writer.WriteString("contentPackId", cycle.ContentPackId);
        writer.WriteString("contentHash", cycle.ContentHash);
        writer.WriteString("scenarioId", cycle.ScenarioId);
        writer.WriteNumber("gameTurn", cycle.GameTurn);
        writer.WriteNumber("operationStage", cycle.OperationStage);
        writer.WriteString("playerPhaseSlot", cycle.PlayerPhaseSlot);
        writer.WriteString("actingSide", CampaignSnapshotSerializer.FormatSide(cycle.ActingSide));
        writer.WriteNumber("ordinal", cycle.Ordinal);
        writer.WriteNumber("openedAuthorityVersion", cycle.OpenedAuthorityVersion);
        writer.WriteString("openingPrefix", cycle.OpeningPrefix);
        writer.WriteString("admittedPolicyBundleDigest", cycle.AdmittedPolicyBundleDigest);
        writer.WriteEndObject();
    }
    private static void WriteInput(Utf8JsonWriter writer, CampaignCombatReserveCompletionInput input)
    {
        writer.WriteStartObject();
        writer.WriteStartObject("command");
        writer.WriteNumber("contractVersion", input.Command.ContractVersion);
        writer.WriteString("kind", input.Command.Kind);
        writer.WriteString("baseHash", input.Command.BaseHash);
        writer.WriteNumber("expectedPriorVersion", input.Command.ExpectedPriorVersion);
        writer.WriteString("expectedPositionId", input.Command.ExpectedPositionId);
        writer.WriteEndObject();
        writer.WriteString("actor", input.Actor switch
        {
            CampaignOpeningPreambleActor.System => "system",
            CampaignOpeningPreambleActor.Axis => "axis",
            CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
            _ => throw new JsonException("Unknown completion actor."),
        });
        writer.WriteEndObject();
    }
    private static byte[] Bytes(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) write(writer);
        if (stream.Length > 1_048_576) throw new JsonException("Completion record exceeds byte limit.");
        return stream.ToArray();
    }
    private static JsonDocument Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized completion record.");
        return JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 32 });
    }
    private static void String(Stream stream, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        U32(stream, checked((uint)bytes.Length));
        stream.Write(bytes);
    }
    private static void U32(Stream stream, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(bytes, value);
        stream.Write(bytes);
    }
    private static void U64(Stream stream, ulong value)
    {
        Span<byte> bytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(bytes, value);
        stream.Write(bytes);
    }
}
