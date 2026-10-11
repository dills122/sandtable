using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace Cna.Core.Campaigns;

/// <summary>Closed syntax only; source readbacks require both separately trusted ledgers.</summary>
internal static class CampaignCombatActualRoundEntryCodec
{
    internal static readonly JsonSerializerOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    internal static byte[] Encode(JsonNode value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);
    internal static void Require(bool condition, int code)
    { if (!condition) throw new JsonException($"CMB-ARE-{code:000}"); }

    internal static JsonElement Parse(ReadOnlySpan<byte> bytes, string kind)
    {
        Require(bytes.Length is > 0 and <= 1_048_576, 1);
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 33 });
            Bounds(document.RootElement, 0);
            Require(bytes.SequenceEqual(Canonical(document.RootElement, kind)), 8);
            return document.RootElement.Clone();
        }
        catch (JsonException e) when (!e.Message.StartsWith("CMB-ARE-", StringComparison.Ordinal))
        { throw new JsonException("CMB-ARE-001", e); }
        catch (Exception e) when (e is InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentException)
        { throw new JsonException("CMB-ARE-001", e); }
    }

    internal static byte[] Canonical(JsonElement value, string kind)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping })) Write(writer, value, kind);
        var bytes = CanonicalEscapes(stream.ToArray()); Require(bytes.Length <= 1_048_576, 1); return bytes;
    }
    internal static byte[] Canonical(JsonNode value, string kind) => Canonical(JsonSerializer.SerializeToElement(value), kind);
    internal static JsonObject Object(ReadOnlySpan<byte> bytes, string kind) => JsonNode.Parse(Parse(bytes, kind).GetRawText())!.AsObject();
    public static byte[] ReadInput(ReadOnlySpan<byte> bytes) { _ = Parse(bytes, "RoundInput"); return bytes.ToArray(); }
    public static byte[] ReadSource(ReadOnlySpan<byte> bytes, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> selectionInputs, IReadOnlyList<byte[]> roundInputs, Func<string, byte[]> dependencyBytes)
    {
        CampaignCombatActualRoundEntry.VerifyDependencies(dependencyBytes);
        var owned = bytes.ToArray(); _ = CampaignCombatActualRoundEntry.ReplayTrustedSource(owned, context, selectionInputs, roundInputs, dependencyBytes); return owned;
    }
    public static CombatActualRoundEntryResult ReadControl(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> selectionInputs, IReadOnlyList<byte[]> roundInputs, Func<string, byte[]> dependencyBytes)
    {
        CampaignCombatActualRoundEntry.VerifyDependencies(dependencyBytes); _ = Parse(bytes, "RoundControl");
        var result = CampaignCombatActualRoundEntry.ReplayTrustedSource(source, context, selectionInputs, roundInputs, dependencyBytes);
        Require(bytes.SequenceEqual(result.ControlBytes), 6); return result;
    }
    public static CombatActualRoundEntryResult ReadProof(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> source, CampaignCombatCreationContext context,
        IReadOnlyList<byte[]> selectionInputs, IReadOnlyList<byte[]> roundInputs, Func<string, byte[]> dependencyBytes)
    {
        CampaignCombatActualRoundEntry.VerifyDependencies(dependencyBytes); _ = Parse(bytes, "ActualRoundProof");
        var result = CampaignCombatActualRoundEntry.ReplayTrustedSource(source, context, selectionInputs, roundInputs, dependencyBytes);
        Require(bytes.SequenceEqual(result.ProofBytes), 6); return result;
    }
    // Match the reference ASCII JSON spellings, leaving escaped literal backslash-u text untouched.
    private static byte[] CanonicalEscapes(byte[] encoded)
    {
        using var stream = new MemoryStream(encoded.Length);
        for (var i = 0; i < encoded.Length; i++)
        {
            if (encoded[i] != 92 || i + 1 >= encoded.Length) { stream.WriteByte(encoded[i]); continue; }
            if (encoded[i + 1] != 'u') { stream.WriteByte(encoded[i]); stream.WriteByte(encoded[++i]); continue; }
            var code = int.Parse(Encoding.ASCII.GetString(encoded, i + 2, 4), System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture);
            if (code is >= 32 and <= 127)
            {
                if (code is 34 or 92) stream.WriteByte((byte)92);
                stream.WriteByte((byte)code);
            }
            else
            {
                stream.WriteByte((byte)92); stream.WriteByte((byte)'u');
                foreach (var c in code.ToString("x4", System.Globalization.CultureInfo.InvariantCulture)) stream.WriteByte((byte)c);
            }
            i += 5;
        }
        return stream.ToArray();
    }

    private static void Bounds(JsonElement value, int depth)
    {
        Require(depth <= 32, 1);
        if (value.ValueKind == JsonValueKind.Array)
        { Require(value.GetArrayLength() <= 512, 1); foreach (var item in value.EnumerateArray()) Bounds(item, depth + 1); }
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in value.EnumerateObject()) { Require(names.Add(field.Name), 1); Bounds(field.Value, depth + 1); }
        }
    }
    private static void Write(Utf8JsonWriter writer, JsonElement value, string kind)
    {
        if (kind.EndsWith('?'))
        { if (value.ValueKind == JsonValueKind.Null) writer.WriteNullValue(); else Write(writer, value, kind[..^1]); return; }
        if (kind == "RoundEffect")
        {
            Require(value.ValueKind == JsonValueKind.Object && value.TryGetProperty("kind", out var tag) && tag.ValueKind == JsonValueKind.String, 1);
            kind = value.GetProperty("kind").GetString() switch
            {
                "round-opened" => "RoundOpen",
                "choice-sealed" => "RoundSeal",
                "round-cancelled" => "RoundCancel",
                "step-completed" => "RoundStep",
                _ => throw new JsonException("CMB-ARE-003"),
            };
        }
        if (Shapes.TryGetValue(kind, out var shape))
        {
            Require(value.ValueKind == JsonValueKind.Object, 1);
            var fields = shape.Split(' ').Select(x => x.Split(':')).ToArray();
            Require(value.EnumerateObject().Select(x => x.Name).ToHashSet(StringComparer.Ordinal).SetEquals(fields.Select(x => x[0])), 1);
            writer.WriteStartObject();
            foreach (var field in fields) { writer.WritePropertyName(field[0]); Write(writer, value.GetProperty(field[0]), field[1]); }
            writer.WriteEndObject(); return;
        }
        if (kind.EndsWith("[]", StringComparison.Ordinal))
        {
            Require(value.ValueKind == JsonValueKind.Array && value.GetArrayLength() <= 512, 1);
            writer.WriteStartArray(); foreach (var item in value.EnumerateArray()) Write(writer, item, kind[..^2]); writer.WriteEndArray(); return;
        }
        if (kind is "int" or "long" or "utc")
        {
            Require(value.ValueKind == JsonValueKind.Number && value.GetRawText().All(c => c is >= '0' and <= '9' or '-'), 1);
            Require(value.TryGetInt64(out var integer) && integer >= 0, 2);
            Require(kind != "int" || integer <= int.MaxValue, 2);
            Require(kind != "utc" || integer <= 253402300799999, 2);
            writer.WriteNumberValue(integer); return;
        }
        if (kind == "role")
        { Require(value.ValueKind == JsonValueKind.String, 1); Require(value.GetString() is "attacker" or "defender", 2); writer.WriteStringValue(value.GetString()); return; }
        if (kind == "utf8")
        {
            Require(value.ValueKind == JsonValueKind.String, 1); var text = value.GetString()!;
            Require(text.Length is > 0 and <= 1_048_576 && text.All(c => c <= 127), 1);
            writer.WriteStringValue(text); return;
        }
        // Entire inherited shapes, Effect and primitive mappings retain predecessor grammar.
        try { writer.WriteRawValue(CampaignCombatActualSelectionCodec.Canonical(value, kind)); }
        catch (JsonException e)
        {
            var code = e.Message.StartsWith("CMB-ASE-", StringComparison.Ordinal) && int.TryParse(e.Message.AsSpan(8, 3), out var inherited) ? inherited : 1;
            throw new JsonException($"CMB-ARE-{code:000}", e);
        }
    }
    private static readonly Dictionary<string, string> Shapes = new(StringComparer.Ordinal)
    {
        ["ActualRoundSource"] = "contractVersion:int actualSelectionSourceCanonicalUtf8:utf8 clockConfiguration:ClockConfiguration roundEventCanonicalUtf8:utf8[]",
        ["Base"] = "contractVersion:int actualSelectionProof:ActualSelectionProof clockConfiguration:ClockConfiguration",
        ["ClockConfiguration"] = "contractVersion:int parentConfigurationHash:hash timingPolicyId:id decisionBudgetMilliseconds:int",
        ["ClockTiming"] = "contractVersion:int clockConfigurationHash:hash kind:id decisionBudgetMilliseconds:int openedAtUnixMilliseconds:utc deadlineUnixMilliseconds:utc openingFloorUnixMilliseconds:utc",
        ["Allocation"] = "kind:id unit:UnitKey componentId:id committedToe:int",
        ["Slot"] = "role:role owner:side slotId:id allocation:Allocation sealedReceiptId:id? sealedAt:utc?",
        ["RoundCommand"] = "contractVersion:int kind:id clockConfigurationHash:hash segmentId:id roundId:id? slotId:id? expectedPriorVersion:long? fromPositionId:id? allocation:Allocation?",
        ["RoundInput"] = "command:RoundCommand actor:actor admittedAt:utc? clockAvailable:bool",
        ["RoundEvent"] = "contractVersion:int eventType:id author:actor campaignId:id rulesetHash:rawHash configurationHash:hash predecessorConfigurationHash:hash actualSelectionSourceHash:hash baseHash:hash cycleId:hash segmentId:id roundId:id priorVersion:long stateVersion:long priorPrefix:hash input:RoundInput effect:RoundEffect receiptId:id",
        ["RoundOpen"] = "kind:id opportunityId:id timing:ClockTiming slots:Slot[]",
        ["RoundSeal"] = "kind:id slotId:id allocation:Allocation timing:ClockTiming prepared:bool",
        ["RoundCancel"] = "kind:id cause:id timing:ClockTiming",
        ["RoundStep"] = "kind:id fromPositionId:id toPositionId:id previousStepReceiptId:id proofKind:id proofReceipts:id[] certificateHash:hash?",
        ["RoundReceipt"] = "commandHash:hash eventHash:hash receiptId:id actor:actor stateVersion:long",
        ["RoundControl"] = "contractVersion:int baseHash:hash clockConfigurationHash:hash segmentId:id opportunityId:id? roundId:id? stateVersion:long prefix:hash stepIndex:int status:id openingReceiptId:id? timing:ClockTiming? slots:Slot[] stepReceipts:id[] world:World randomState:Random attackHistory:null[] targetUses:null[] commitmentId:null cancellationReceiptId:id? receipts:RoundReceipt[] closed:bool",
        ["ActualRoundProof"] = "contractVersion:int sourceId:id sourceHash:hash base:Base control:RoundControl",
        ["EmptyAaCertificate"] = "contractVersion:int contentHash:hash worldHash:hash candidate:Candidate allocations:Allocation[] assignmentReceiptId:id componentIds:id[]",
    };
}
