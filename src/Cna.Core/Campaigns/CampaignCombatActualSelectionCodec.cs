using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
namespace Cna.Core.Campaigns;

/// <summary>Private closed grammar. Decoding actor/time never authenticates the caller ledger.</summary>
internal static class CampaignCombatActualSelectionCodec
{
    internal static readonly JsonSerializerOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    internal static byte[] Encode(JsonNode value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);
    internal static void Require(bool condition, int code)
    { if (!condition) throw new JsonException($"CMB-ASE-{code:000}"); }
    internal static JsonElement Parse(ReadOnlySpan<byte> bytes, string kind)
    {
        Require(bytes.Length is > 0 and <= 1_048_576, 1);
        try
        {
            using var doc = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 33 });
            Bounds(doc.RootElement, 0);
            Require(bytes.SequenceEqual(Canonical(doc.RootElement, kind)), 8);
            return doc.RootElement.Clone();
        }
        catch (JsonException e) when (!e.Message.StartsWith("CMB-ASE-", StringComparison.Ordinal))
        { throw new JsonException("CMB-ASE-001", e); }
        catch (Exception e) when (e is InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentException)
        { throw new JsonException("CMB-ASE-001", e); }
    }
    internal static byte[] Canonical(JsonElement value, string kind)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping })) Write(writer, value, kind);
        var bytes = stream.ToArray(); Require(bytes.Length <= 1_048_576, 1); return bytes;
    }
    internal static byte[] Canonical(JsonNode value, string kind) => Canonical(JsonSerializer.SerializeToElement(value), kind);
    internal static JsonObject Object(ReadOnlySpan<byte> bytes, string kind) => JsonNode.Parse(Parse(bytes, kind).GetRawText())!.AsObject();
    public static byte[] ReadInput(ReadOnlySpan<byte> bytes) { _ = Parse(bytes, "Input"); return bytes.ToArray(); }
    public static byte[] ReadSource(ReadOnlySpan<byte> bytes, CampaignCombatCreationContext context, IReadOnlyList<byte[]> trustedInputs, Func<string, byte[]> dependencyBytes)
    { CampaignCombatActualSelection.VerifyDependencies(dependencyBytes); Require(bytes.Length is > 0 and <= 1_048_576, 1); var owned = bytes.ToArray(); _ = CampaignCombatActualSelection.ReplayTrustedSource(owned, context, trustedInputs, dependencyBytes); return owned; }
    public static CombatActualSelectionResult ReadProof(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> source, CampaignCombatCreationContext context, IReadOnlyList<byte[]> trustedInputs, Func<string, byte[]> dependencyBytes)
    {
        CampaignCombatActualSelection.VerifyDependencies(dependencyBytes);
        _ = Parse(bytes, "ActualSelectionProof");
        var result = CampaignCombatActualSelection.ReplayTrustedSource(source, context, trustedInputs, dependencyBytes);
        Require(bytes.SequenceEqual(result.ProofBytes), 6); return result;
    }
    public static CombatActualSelectionResult ReadControl(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> source, CampaignCombatCreationContext context, IReadOnlyList<byte[]> trustedInputs, Func<string, byte[]> dependencyBytes)
    {
        CampaignCombatActualSelection.VerifyDependencies(dependencyBytes);
        _ = Parse(bytes, "Control");
        var result = CampaignCombatActualSelection.ReplayTrustedSource(source, context, trustedInputs, dependencyBytes);
        Require(bytes.SequenceEqual(result.ControlBytes), 6); return result;
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
        if (kind == "Effect")
        {
            Require(value.ValueKind == JsonValueKind.Object && value.TryGetProperty("kind", out var tag) && tag.ValueKind == JsonValueKind.String, 3);
            kind = value.GetProperty("kind").GetString() switch
            {
                "segment-opened" => "OpenEffect",
                "selection-closed" => "SelectionEffect",
                "rba-opened" => "RbaOpenEffect",
                "rba-declined" => "DeclineEffect",
                "selection-cancelled" => "CancelEffect",
                "step-completed" => "StepEffect",
                _ => throw new JsonException("CMB-ASE-003"),
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
            // Selection histories, step receipts and candidate component lists preserve their declared order.
            writer.WriteStartArray(); foreach (var item in value.EnumerateArray()) Write(writer, item, kind[..^2]); writer.WriteEndArray(); return;
        }
        if (kind is "int" or "long")
        {
            Require(value.ValueKind == JsonValueKind.Number && value.GetRawText().All(c => c is >= '0' and <= '9' or '-'), 1);
            Require(value.TryGetInt64(out var integer), 2); Require(kind != "int" || integer is >= int.MinValue and <= int.MaxValue, 2);
            writer.WriteNumberValue(integer); return;
        }
        if (kind == "utc")
        { Require(value.ValueKind == JsonValueKind.Number && value.GetRawText().All(c => c is >= '0' and <= '9' or '-'), 1); Require(value.TryGetInt64(out var time) && time is >= 0 and <= 253402300799999, 2); writer.WriteNumberValue(time); return; }
        if (kind == "actor")
        { Require(value.ValueKind == JsonValueKind.String, 1); Require(value.GetString() is "axis" or "commonwealth" or "system", 2); writer.WriteStringValue(value.GetString()); return; }
        if (kind is "id" or "hash" or "rawHash" or "side")
        {
            Require(value.ValueKind == JsonValueKind.String, 1); var text = value.GetString()!;
            var valid = kind switch
            {
                "side" => text is "axis" or "commonwealth",
                "hash" => text.StartsWith("sha256:", StringComparison.Ordinal) && Hex(text[7..]),
                "rawHash" => Hex(text),
                _ => text.Length is > 0 and <= 128 && Alpha(text[0]) && text.All(c => Alpha(c) || c is '.' or '_' or ':' or '-'),
            };
            Require(valid, 2); writer.WriteStringValue(text); return;
        }
        try { writer.WriteRawValue(CampaignCombatPositiveEntryCodec.Canonical(value, kind)); }
        catch (JsonException e) { throw new JsonException("CMB-ASE-001", e); }
    }
    private static bool Alpha(char c) => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9';
    private static bool Hex(string s) => s.Length == 64 && s.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static readonly Dictionary<string, string> Shapes = new(StringComparer.Ordinal)
    {
        ["Participant"] = "unit:UnitKey representationId:id locationId:id componentIds:id[]",
        ["Candidate"] = "attacker:Participant defender:Participant targetLocationId:id basis:id",
        ["Window"] = "decisionId:id owner:side timing:Timing",
        ["Command"] = "contractVersion:int kind:id segmentId:id decisionId:id? fromPositionId:id? expectedPriorVersion:long? choice:id? candidate:Candidate? participant:UnitKey?",
        ["Input"] = "command:Command actor:actor admittedAt:utc? clockAvailable:bool",
        ["Event"] = "contractVersion:int eventType:id campaignId:id rulesetHash:rawHash configurationHash:hash positiveEntrySourceHash:hash cycleId:hash segmentId:id priorVersion:long stateVersion:long priorPrefix:hash input:Input effect:Effect receiptId:id",
        ["OpenEffect"] = "kind:id boundaryHash:hash window:Window?",
        ["SelectionEffect"] = "kind:id outcome:id candidate:Candidate? timing:Timing?",
        ["RbaOpenEffect"] = "kind:id selectionReceiptId:id window:Window",
        ["DeclineEffect"] = "kind:id selectionReceiptId:id participant:UnitKey timing:Timing",
        ["CancelEffect"] = "kind:id selectionReceiptId:id timing:Timing?",
        ["StepEffect"] = "kind:id fromPositionId:id toPositionId:id previousStepReceiptId:id dispositionReceiptId:id proofKind:id",
        ["Receipt"] = "commandHash:hash eventHash:hash receiptId:id actor:actor stateVersion:long",
        ["Control"] = "contractVersion:int boundaryHash:hash segmentId:id stateVersion:long prefix:hash stepIndex:int selectionOutcome:id selection:Candidate? selectionReceiptId:id? declineReceiptId:id? cancellationReceiptId:id? selectionWindow:Window? rbaWindow:Window? stepReceipts:id[] receipts:Receipt[] closed:bool",
        ["ActualSelectionSource"] = "contractVersion:int positiveEntrySourceCanonicalUtf8:utf8 selectionEventCanonicalUtf8:utf8[]",
        ["ActualSelectionBoundary"] = "contractVersion:int positiveEntrySourceId:id positiveEntrySourceHash:hash creationBinding:id creationEventHash:hash cycle:Authority cycleId:hash firstActingSide:side priorVersion:long priorPrefix:hash reserveCompletionReceiptId:id movementCompletionReceiptId:id breakdownCompletionReceiptId:id breakdownCompletionEventHash:hash position:Position world:World randomState:Random weather:ActualApplicableWeather breakdownFlow:Idle reactionWindow:null movementEnd:MovementEndProof candidate:Candidate",
        ["ActualApplicableWeather"] = "gameTurn:int operationStage:int attackerKind:id defenderKind:id weatherReceiptId:id weatherEventHash:hash",
        ["ActualSelectionProof"] = "contractVersion:int sourceId:id sourceHash:hash positiveEntryProof:PositiveEntryProof boundary:ActualSelectionBoundary control:Control",
        ["Timing"] = "contractVersion:int configHash:hash kind:id decisionBudgetMilliseconds:int openedAtUnixMilliseconds:utc deadlineUnixMilliseconds:utc highWaterUnixMilliseconds:utc",
        ["Idle"] = "kind:id",
    };
}
