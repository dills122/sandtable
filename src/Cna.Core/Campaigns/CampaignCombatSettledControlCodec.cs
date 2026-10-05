using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cna.Core.Campaigns;

/// <summary>Closed019D2 byte grammar. Reading evidence always replays its complete source.</summary>
internal static class CampaignCombatSettledControlCodec
{
    private static readonly JsonSerializerOptions CommandOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static readonly JsonSerializerOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    public static byte[] SerializeState(CombatSettledControlState state) => state.CanonicalBytes;
    public static CombatSettledControlSource ReadBase(ReadOnlySpan<byte> bytes, CampaignCombatCreationContext context) =>
        CombatSettledControlSource.Admit(bytes, context);
    public static CombatSettledControlState ReadState(ReadOnlySpan<byte> bytes, CombatSettledControlSource source,
        IReadOnlyList<byte[]> inputs, IReadOnlyList<byte[]> events)
    {
        _ = Parse(bytes, "SettledControlState"); var state = CampaignCombatSettledControl.Replay(source, inputs, events);
        Require(bytes.SequenceEqual(state.CanonicalBytes), 6); return state;
    }
    public static byte[] SerializeInput(CombatCycleControlInput input)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentNullException.ThrowIfNull(input.Command);
        var node = new JsonObject
        {
            ["command"] = JsonSerializer.SerializeToNode(input.Command, CommandOptions),
            ["actor"] = CampaignCombatSealedRoundCodec.Actor(input.Actor),
            ["admittedAt"] = input.AdmittedAt,
            ["clockAvailable"] = input.ClockAvailable
        };
        return Checked(node, "SettledControlInput");
    }
    internal static byte[] Checked(JsonNode node, string kind) { var bytes = Encode(node); _ = Parse(bytes, kind); return bytes; }
    internal static byte[] Encode(JsonNode node) => JsonSerializer.SerializeToUtf8Bytes(node, Options);
    internal static byte[] Bytes(JsonElement node) => Encoding.UTF8.GetBytes(node.GetRawText());
    internal static string Text(JsonElement node, string field) => node.GetProperty(field).GetString()!;
    internal static void Require(bool condition, int code) { if (!condition) throw new JsonException($"CMB-SCC-{code:000}"); }
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
        catch (JsonException e) when (!e.Message.StartsWith("CMB-SCC-", StringComparison.Ordinal)) { throw new JsonException("CMB-SCC-001", e); }
        catch (Exception e) when (e is InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { throw new JsonException("CMB-SCC-001", e); }
    }
    internal static byte[] Canonical(JsonElement node, string kind)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping })) Write(writer, node, kind);
        var bytes = stream.ToArray(); Require(bytes.Length <= 1_048_576, 1); return bytes;
    }
    private static void Bounds(JsonElement node, int depth)
    {
        Require(depth <= 32, 1);
        if (node.ValueKind == JsonValueKind.Array)
        {
            Require(node.GetArrayLength() <= 512, 1);
            foreach (var item in node.EnumerateArray()) Bounds(item, depth + 1);
        }
        if (node.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in node.EnumerateObject()) { Require(names.Add(field.Name), 1); Bounds(field.Value, depth + 1); }
        }
    }
    private static void Write(Utf8JsonWriter w, JsonElement value, string kind)
    {
        if (kind.EndsWith('?'))
        { if (value.ValueKind == JsonValueKind.Null) w.WriteNullValue(); else Write(w, value, kind[..^1]); return; }
        if (kind.EndsWith("[]", StringComparison.Ordinal))
        {
            Require(value.ValueKind == JsonValueKind.Array, 1); w.WriteStartArray();
            foreach (var item in value.EnumerateArray()) Write(w, item, kind[..^2]); w.WriteEndArray(); return;
        }
        if (Shapes.TryGetValue(kind, out var shape))
        {
            Require(value.ValueKind == JsonValueKind.Object, 1);
            if (kind == "SettledControlState") Require(value.GetProperty("members").GetArrayLength() <= 32, 1);
            var fields = shape.Split(' ').Select(f => f.Split(':')).ToArray();
            Require(value.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal).SetEquals(fields.Select(f => f[0])), 1);
            w.WriteStartObject(); foreach (var f in fields) { w.WritePropertyName(f[0]); Write(w, value.GetProperty(f[0]), f[1]); }
            w.WriteEndObject(); return;
        }
        if (kind == "utf8")
        {
            Require(value.ValueKind == JsonValueKind.String && value.GetString()!.Length is > 0 and <= 1_048_576, 1); var escaped = JsonSerializer.Serialize(value.GetString(), Options);
            var ascii = new StringBuilder();
            foreach (var ch in escaped)
                if (ch > 127) ascii.Append("\\u").Append(((int)ch).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
                else ascii.Append(ch);
            w.WriteRawValue(ascii.ToString()); return;
        }
        if (kind == "utc")
        { Require(value.TryGetInt64(out var time) && time is >= 0 and <= CampaignCombatSelectionSteps.UtcMaximum, 1); w.WriteNumberValue(time); return; }
        if (kind == "Authority") CampaignCombatIdentityCodec.WriteSelectionStepsExternalSyntax(w, value, kind);
        else CampaignCombatIdentityCodec.WriteResultExternalSyntax(w, value, kind);
    }
    private static readonly Dictionary<string, string> Shapes = new(StringComparer.Ordinal)
    {
        ["SettledControlBase"] = "contractVersion:int packetCanonicalUtf8:utf8 proofCanonicalUtf8:utf8",
        ["SettledAssessment"] = "releaseCompletionReceiptId:id movementCompletionReceiptId:id progress:ProgressRef[] witnesses:ContinuationWitness[] combatAssessment:id witnessOutcome:id unsupportedReason:id? sourceProofId:id",
        ["SettledControlCommand"] = "contractVersion:int kind:id controlId:id cycleId:hash expectedPriorVersion:long? decisionId:id?",
        ["SettledControlInput"] = "command:SettledControlCommand actor:actor admittedAt:utc? clockAvailable:bool",
        ["SettledControlEffect"] = "kind:id reason:id assessment:SettledAssessment timing:Timing? openingClockFailure:bool successorPositionId:id nextCycle:Authority? expiredUnits:UnitKey[]",
        ["SettledControlEvent"] = "contractVersion:int sourceProofId:id eventType:id author:actor campaignId:id rulesetHash:rawHash configurationHash:hash cycleId:hash positionId:id baseHash:hash controlId:id priorVersion:long stateVersion:long priorPrefix:hash input:SettledControlInput effect:SettledControlEffect receiptId:id",
        ["SettledClosure"] = "cycleId:hash ordinal:int outcome:id receiptId:id",
        ["SettledControlState"] = "contractVersion:int sourceProofId:id baseHash:hash controlId:id stateVersion:long prefix:hash status:id positionId:id activeCycle:Authority? closure:SettledClosure? decisionId:id? timing:Timing? openingClockFailure:bool acceptedHighWater:utc? assessment:SettledAssessment? members:ReserveMember[] world:World randomState:Random attackHistory:AttackHistory[] targetUses:TargetUse[] nextCycleProgress:ProgressRef[] receipts:Receipt[]",
        ["Timing"] = "contractVersion:int configHash:hash kind:id decisionBudgetMilliseconds:int openedAtUnixMilliseconds:utc deadlineUnixMilliseconds:utc highWaterUnixMilliseconds:utc",
        ["Receipt"] = "commandHash:hash eventHash:hash receiptId:id actor:actor stateVersion:long",
        ["ReleaseScope"] = "gameTurn:int operationStage:int playerPhaseSlot:id actingSide:side",
        ["ProgressRef"] = "eventType:id receiptId:id eventHash:hash",
        ["ContinuationWitness"] = "kind:id unit:UnitKey destinationLocationId:id terrainCost:int breakOffCost:int afterCp:int excessCpDp:int usesReleaseException:bool",
        ["ReserveMember"] = "unit:UnitKey status:id baseCpa:int spentCp:CP history:ReserveHistory",
        ["ReserveHistory"] = "scope:ReleaseScope designationReceiptId:id? conversionReceiptId:id? releasedType:id? releaseReceiptId:id? releaseCycle:int? cpaBasis:int? voluntaryCeiling:int? offensiveCommitmentId:id? nextMovement:MovementException?",
        ["MovementException"] = "scope:ReleaseScope ordinal:int status:id completionReceiptId:id?",
        ["AttackHistory"] = "commitmentId:id cycleId:hash segmentId:id attacker:UnitKey defender:UnitKey targetLocationId:id gameTurn:int operationStage:int",
        ["TargetUse"] = "commitmentId:id segmentId:id targetLocationId:id",
        ["CP"] = "numerator:long denominator:int",

    };
}
