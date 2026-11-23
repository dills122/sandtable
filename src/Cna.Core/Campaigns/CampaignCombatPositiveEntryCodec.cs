using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cna.Core.Campaigns;

/// <summary>Closed canonical syntax; full replay supplies authority for every source and cache.</summary>
internal static class CampaignCombatPositiveEntryCodec
{
    private static readonly JsonSerializerOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    public static CombatPositiveEntrySource ReadSource(ReadOnlySpan<byte> bytes, CampaignCombatCreationContext context)
    {
        _ = Parse(bytes, "PositiveSource");
        var source = new CombatPositiveEntrySource(bytes, context);
        _ = CampaignCombatPositiveEntry.Replay(source);
        return source;
    }
    public static CombatPositiveEntryState ReadProof(ReadOnlySpan<byte> bytes, CombatPositiveEntrySource source)
    {
        _ = Parse(bytes, "PositiveEntryProof");
        var state = CampaignCombatPositiveEntry.Replay(source);
        Require(bytes.SequenceEqual(state.ProofBytes), 6);
        return state;
    }
    internal static byte[] Encode(JsonNode value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);
    internal static byte[] Bytes(JsonElement value) => Encoding.UTF8.GetBytes(value.GetRawText());
    internal static void Require(bool condition, int code)
    { if (!condition) throw new JsonException($"CMB-PEN-{code:000}"); }
    internal static JsonElement Parse(ReadOnlySpan<byte> bytes, string kind)
    {
        Require(bytes.Length is > 0 and <= 1_048_576, 1);
        try
        {
            using var doc = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 33 });
            Bounds(doc.RootElement, 0);
            var canonical = Canonical(doc.RootElement, kind);
            Require(bytes.SequenceEqual(canonical), 8);
            return doc.RootElement.Clone();
        }
        catch (JsonException e) when (!e.Message.StartsWith("CMB-PEN-", StringComparison.Ordinal))
        { throw new JsonException("CMB-PEN-001", e); }
        catch (Exception e) when (e is InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentException)
        { throw new JsonException("CMB-PEN-001", e); }
    }
    internal static byte[] Canonical(JsonElement value, string kind)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            Write(writer, value, kind);
        var bytes = stream.ToArray(); Require(bytes.Length <= 1_048_576, 1); return bytes;
    }
    private static void Bounds(JsonElement value, int depth)
    {
        Require(depth <= 32, 1);
        if (value.ValueKind == JsonValueKind.Array)
        {
            Require(value.GetArrayLength() <= 512, 1);
            foreach (var child in value.EnumerateArray()) Bounds(child, depth + 1);
        }
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
        if (kind is "LifecycleCommand" or "LifecycleFlow")
        {
            Require(value.ValueKind == JsonValueKind.Object && value.TryGetProperty("kind", out var tag) && tag.ValueKind == JsonValueKind.String, 1);
            kind = value.GetProperty("kind").GetString() switch
            {
                "stop-element-movement" when kind == "LifecycleCommand" => "StopCommand",
                "resolve-breakdown-stop" when kind == "LifecycleCommand" => "ResolveCommand",
                "complete-movement-segment" when kind == "LifecycleCommand" => "CompleteCommand",
                "moving" when kind == "LifecycleFlow" => "MovingFlow",
                "phasing-stop" when kind == "LifecycleFlow" => "PhasingStopFlow",
                "idle" when kind == "LifecycleFlow" => "IdleFlow",
                _ => throw new JsonException("CMB-PEN-001"),
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
        if (kind.EndsWith("[]", StringComparison.Ordinal) || kind is "OrderedLocations" or "empty")
        {
            Require(value.ValueKind == JsonValueKind.Array && value.GetArrayLength() <= 512, 1);
            Require(kind != "empty" || value.GetArrayLength() == 0, 1);
            Require(kind != "OrderedLocations" || value.GetArrayLength() is >= 2 and <= 33, 1);
            var child = kind.EndsWith("[]", StringComparison.Ordinal) ? kind[..^2] : "id";
            var items = value.EnumerateArray().ToArray();
            // Validate children before comparing keys, preserving malformed-before-canonical errors.
            foreach (var item in items) _ = Canonical(item, child);
            // Routes preserve traversal order and revisits; identity collections remain sorted.
            if (kind != "OrderedLocations" && (child is "UnitKey" or "EndLocation" || child == "id" || Keys.ContainsKey(child)))
                Array.Sort(items, (a, b) => Compare(a, b, child));
            writer.WriteStartArray(); foreach (var item in items) Write(writer, item, child); writer.WriteEndArray(); return;
        }
        if (kind is "LegacyBrokenVehicleLot" or "Route") throw new JsonException("CMB-PEN-001");
        if (kind == "null") { Require(value.ValueKind == JsonValueKind.Null, 1); writer.WriteNullValue(); return; }
        if (kind == "bool") { Require(value.ValueKind is JsonValueKind.True or JsonValueKind.False, 1); writer.WriteBooleanValue(value.GetBoolean()); return; }
        if (kind is "int" or "long" or "ulong" or "futureTurn")
        {
            Require(value.ValueKind == JsonValueKind.Number, 1);
            if (kind == "ulong") { Require(value.TryGetUInt64(out var n), 1); writer.WriteNumberValue(n); return; }
            Require(value.TryGetInt64(out var number), 1);
            Require(kind != "int" || number is >= int.MinValue and <= int.MaxValue, 1);
            Require(kind != "futureTurn" || number is >= 1 and <= 115, 1);
            writer.WriteNumberValue(number); return;
        }
        Require(value.ValueKind == JsonValueKind.String, 1); var text = value.GetString()!;
        var valid = kind switch
        {
            "utf8" => text.Length is > 0 and <= 1_048_576 && text.All(c => c <= 127),
            "id" => text.Length is > 0 and <= 128 && AlphaNumeric(text[0]) && text.All(c => AlphaNumeric(c) || c is '.' or '_' or ':' or '-'),
            "hash" => text.StartsWith("sha256:", StringComparison.Ordinal) && Hex(text[7..]),
            "rawHash" => Hex(text),
            "string" or "locator" => text.Length is > 0 and <= 512 && text.All(c => c is >= ' ' and <= '~'),
            "side" => text is "axis" or "commonwealth",
            "actor" => text is "axis" or "commonwealth" or "system",
            _ => false,
        };
        Require(valid, 1);
        if (kind == "utf8") writer.WriteRawValue(Carrier(text)); else writer.WriteStringValue(text);
    }
    private static string Carrier(string text)
    {
        var escaped = new StringBuilder("\"");
        foreach (var c in text)
        {
            escaped.Append(c switch
            {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\b' => "\\b",
                '\f' => "\\f",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                < ' ' or '\x7f' => "\\u" + ((int)c).ToString("x4", System.Globalization.CultureInfo.InvariantCulture),
                _ => c.ToString()
            });
        }
        return escaped.Append('"').ToString();
    }
    private static bool AlphaNumeric(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';
    private static bool Hex(string text) => text.Length == 64 && text.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static int Compare(JsonElement a, JsonElement b, string child)
    {
        if (child == "id") return StringComparer.Ordinal.Compare(a.GetString(), b.GetString());
        if (child == "EndLocation") { a = a.GetProperty("unit"); b = b.GetProperty("unit"); child = "UnitKey"; }
        var keys = child == "UnitKey" ? new[] { "creationBinding", "originalSide", "elementId" } : Keys[child];
        foreach (var key in keys)
        {
            var left = a.GetProperty(key); var right = b.GetProperty(key);
            var comparison = left.ValueKind == JsonValueKind.Number ? left.GetInt64().CompareTo(right.GetInt64()) : StringComparer.Ordinal.Compare(left.GetString(), right.GetString());
            if (comparison != 0) return comparison;
        }
        return 0;
    }
    // Frozen transitive inventory of 019E0 and unchanged predecessor grammars; no runtime schema/fixture I/O.
    private static readonly Dictionary<string, string> Shapes = new(StringComparer.Ordinal)
    {
        ["ActualProgressRef"] = CampaignCodecSchemaFragments.ActualProgressRef,
        ["Ammunition"] = CampaignCodecSchemaFragments.Ammunition,
        ["Authority"] = CampaignCodecSchemaFragments.Authority,
        ["Candidate"] = CampaignCodecSchemaFragments.Candidate,
        ["Cause"] = CampaignCodecSchemaFragments.Cause,
        ["CombatEntryState"] = CampaignCodecSchemaFragments.CombatEntryState,
        ["CompleteCommand"] = "contractVersion:int kind:id actionId:hash creationBinding:id creationEventHash:hash cycleId:hash expectedPriorVersion:long expectedPositionId:id",
        ["CompleteEvent"] = "contractVersion:int eventType:id campaignId:id stateVersion:long priorStateVersion:long fromPositionId:id gameTurn:int operationStage:int actingSide:side sequencePosition:Position rulesetHash:rawHash breakdownFlowAfter:IdleFlow configurationHash:hash creationBinding:id creationEventHash:hash cycleId:hash openingBaseHash:hash completionReceiptId:id priorPrefix:hash input:LifecycleInput interruptContextAfter:InterruptContext? endLocations:EndLocation[] excludedUnits:UnitKey[] progress:ActualProgressRef[] receiptId:id",
        ["Component"] = CampaignCodecSchemaFragments.Component,
        ["ComponentKey"] = CampaignCodecSchemaFragments.ComponentKey,
        ["Cp"] = CampaignCodecSchemaFragments.Cp,
        ["Custody"] = CampaignCodecSchemaFragments.Custody,
        ["Disposition"] = CampaignCodecSchemaFragments.Disposition,
        ["Element"] = CampaignCodecSchemaFragments.Element,
        ["EndLocation"] = CampaignCodecSchemaFragments.EndLocation,
        ["Entitlement"] = CampaignCodecSchemaFragments.Entitlement,
        ["EntryCommand"] = "contractVersion:int kind:id operationStage:int actionId:hash creationBinding:id creationEventHash:hash cycleId:hash expectedPriorVersion:long expectedPositionId:id",
        ["EntryEvent"] = "contractVersion:int eventType:id campaignId:id stateVersion:long priorStateVersion:long rulesetHash:rawHash fromPositionId:id actionId:hash sequencePosition:Position breakdownFlowAfter:IdleFlow sources:Reference[] configurationHash:hash creationBinding:id creationEventHash:hash cycleId:hash openingBaseHash:hash completionReceiptId:id movementCompletionReceiptId:id priorPrefix:hash input:EntryInput receiptId:id",
        ["EntryInput"] = "command:EntryCommand actor:actor",
        ["FutureScope"] = CampaignCodecSchemaFragments.FutureScope,
        ["Guard"] = CampaignCodecSchemaFragments.Guard,
        ["IdleFlow"] = CampaignCodecSchemaFragments.IdleFlow,
        ["InheritedTrack"] = CampaignCodecSchemaFragments.InheritedTrack,
        ["InterruptContext"] = CampaignCodecSchemaFragments.InterruptContext,
        ["LifecycleInput"] = "command:LifecycleCommand actor:actor",
        ["LifecycleStop"] = CampaignCodecSchemaFragments.LifecycleStop,
        ["Losses"] = CampaignCodecSchemaFragments.Losses,
        ["Lot"] = CampaignCodecSchemaFragments.Lot,
        ["MovementEndProof"] = CampaignCodecSchemaFragments.MovementEndProof,
        ["MovementException"] = CampaignCodecSchemaFragments.MovementException,
        ["MovingFlow"] = CampaignCodecSchemaFragments.MovingFlow,
        ["MovingRoute"] = CampaignCodecSchemaFragments.MovingRoute,
        ["Obligation"] = CampaignCodecSchemaFragments.Obligation,
        ["Operational"] = CampaignCodecSchemaFragments.Operational,
        ["Origin"] = CampaignCodecSchemaFragments.Origin,
        ["Participant"] = CampaignCodecSchemaFragments.Participant,
        ["PhasingStopFlow"] = CampaignCodecSchemaFragments.PhasingStopFlow,
        ["Position"] = CampaignCodecSchemaFragments.Position,
        ["PositiveEntryProof"] = "contractVersion:int sourceId:id sourceHash:hash entry:CombatEntryState candidate:Candidate?",
        ["PositiveSource"] = "contractVersion:int requestCanonicalUtf8:utf8 createdCanonicalUtf8:utf8 preambleEventCanonicalUtf8:utf8[] weatherEventCanonicalUtf8:utf8[] stageEventCanonicalUtf8:utf8[] reserveEventCanonicalUtf8:utf8[] entryEventCanonicalUtf8:utf8[]",
        ["PreambleOrder"] = CampaignCodecSchemaFragments.PreambleOrder,
        ["PreambleReceipt"] = CampaignCodecSchemaFragments.Receipt,
        ["Random"] = CampaignCodecSchemaFragments.Random,
        ["Readiness"] = CampaignCodecSchemaFragments.Readiness,
        ["Reference"] = CampaignCodecSchemaFragments.Reference,
        ["RelationsReceipt"] = CampaignCodecSchemaFragments.RelationsReceipt,
        ["Relationship"] = CampaignCodecSchemaFragments.Relationship,
        ["ReleaseHistory"] = CampaignCodecSchemaFragments.ReleaseHistory,
        ["ReleaseScope"] = CampaignCodecSchemaFragments.ReleaseScope,
        ["Representation"] = CampaignCodecSchemaFragments.Representation,
        ["ReserveMember"] = CampaignCodecSchemaFragments.ReserveMember,
        ["ResolveCommand"] = "contractVersion:int kind:id stopId:hash actionId:hash creationBinding:id creationEventHash:hash cycleId:hash expectedPriorVersion:long expectedPositionId:id",
        ["Result"] = CampaignCodecSchemaFragments.Result,
        ["Retreat"] = CampaignCodecSchemaFragments.Retreat,
        ["RoleLoss"] = CampaignCodecSchemaFragments.RoleLoss,
        ["Scope"] = CampaignCodecSchemaFragments.Scope,
        ["Settlement"] = CampaignCodecSchemaFragments.Settlement,
        ["StopCommand"] = "contractVersion:int kind:id routeId:hash actionId:hash creationBinding:id creationEventHash:hash cycleId:hash expectedPriorVersion:long expectedPositionId:id",
        ["UnitKey"] = CampaignCodecSchemaFragments.UnitKey,
        ["WeatherValue"] = CampaignCodecSchemaFragments.WeatherValue,
        ["World"] = CampaignCodecSchemaFragments.World,
    };
    private static readonly Dictionary<string, string[]> Keys = new(StringComparer.Ordinal)
    {
        ["Element"] = ["elementId"],
        ["Component"] = ["componentId"],
        ["Reference"] = ["sourceId", "locator"],
        ["Representation"] = ["representationId"],
        ["Cause"] = ["ordinal"],
        ["Relationship"] = ["relationId"],
        ["Lot"] = ["lotId"],
        ["Guard"] = ["guardId"],
        ["Entitlement"] = ["entitlementId"],
        ["Obligation"] = ["obligationId"],
        ["Settlement"] = ["settlementId"],
        ["RoleLoss"] = ["role"],
    };
}
