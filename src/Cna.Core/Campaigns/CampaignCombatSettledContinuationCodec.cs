using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cna.Core.Campaigns;

/// <summary>Closed019D0 byte grammar. Reading evidence always replays its complete source.</summary>
internal static class CampaignCombatSettledContinuationCodec
{
    private static readonly JsonSerializerOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    public static byte[] Serialize(CombatSettledContinuationProof proof) => proof.CanonicalBytes;
    public static CombatSettledContinuationProof ReadProof(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> packet,
        CampaignCombatCreationContext context)
    {
        _ = Parse(bytes, "SettledContinuationProof");
        var expected = CampaignCombatSettledContinuation.Bridge(packet, context);
        Require(bytes.SequenceEqual(expected.CanonicalBytes), 6);
        return expected;
    }
    public static string ProofId(CombatSettledContinuationProof proof) => "sct." +
        CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.settled-continuation.v1", Serialize(proof))[7..];
    internal static byte[] Encode(JsonNode node) => JsonSerializer.SerializeToUtf8Bytes(node, Options);
    internal static byte[] Bytes(JsonElement node) => Encoding.UTF8.GetBytes(node.GetRawText());
    internal static string Text(JsonElement node, string field) => node.GetProperty(field).GetString()!;
    internal static void Require(bool condition, int code) { if (!condition) throw new JsonException($"CMB-SCT-{code:000}"); }
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
        catch (JsonException e) when (!e.Message.StartsWith("CMB-SCT-", StringComparison.Ordinal)) { throw new JsonException("CMB-SCT-001", e); }
        catch (Exception e) when (e is InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { throw new JsonException("CMB-SCT-001", e); }
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
            Require(value.ValueKind == JsonValueKind.Object, 1); var fields = shape.Split(' ').Select(f => f.Split(':')).ToArray();
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
        ["SyntheticMovementDescriptor"] = "trustLabel:id descriptorId:id receiptSource:id proof:MovementEndProof",
        ["SettledSource"] = "contractVersion:int sourceId:id trustLabel:id requestCanonicalUtf8:utf8 createdCanonicalUtf8:utf8 baseCanonicalUtf8:utf8 predecessorCanonicalUtf8:utf8 roundInputsCanonicalUtf8:utf8 roundEventCanonicalUtf8:utf8[] committedCanonicalUtf8:utf8 resultInputsCanonicalUtf8:utf8 resultEventCanonicalUtf8:utf8[] resultStateCanonicalUtf8:utf8 movementEnd:SyntheticMovementDescriptor",
        ["SettledPacket"] = "contractVersion:int source:SettledSource releaseBaseCanonicalUtf8:utf8 releaseInputs:ReleaseInput[] releaseEventCanonicalUtf8:utf8[]",
        ["SourceIdentity"] = "sourceId:id sourceHash:hash requestHash:hash createdHash:hash boundaryHash:hash selectionHash:hash baseHash:hash committedHash:hash resultHash:hash releaseBaseHash:hash releaseStateHash:hash movementDescriptorHash:hash roundClockConfigurationHash:hash resultClockPolicyId:id",
        ["SettledContinuationProof"] = "contractVersion:int profile:id trustLabel:id sourceIdentity:SourceIdentity cycle:Authority stateVersion:long prefix:hash positionId:id releaseCompletionReceiptId:id movementEnd:SyntheticMovementDescriptor world:World randomState:Random attackHistory:AttackHistory[] targetUses:TargetUse[] members:ReserveMember[] progress:ProgressRef[] witnessOutcome:id unsupportedReason:id? witnesses:ContinuationWitness[]",
        ["MovementEndProof"] = "scope:ReleaseScope ordinal:int completionReceiptId:id endLocations:EndLocation[] excludedBefore:UnitKey[]",
        ["EndLocation"] = "unit:UnitKey locationId:id",
        ["ReleaseScope"] = "gameTurn:int operationStage:int playerPhaseSlot:id actingSide:side",
        ["ProgressRef"] = "eventType:id receiptId:id eventHash:hash",
        ["ContinuationWitness"] = "kind:id unit:UnitKey destinationLocationId:id terrainCost:int breakOffCost:int afterCp:int excessCpDp:int usesReleaseException:bool",
        ["ReleaseInput"] = "command:ReleaseCommand actor:actor admittedAt:utc? clockAvailable:bool",
        ["ReleaseCommand"] = "contractVersion:int kind:id releaseId:id expectedPriorVersion:long? decisionId:id? unit:UnitKey? choice:id?",
        ["ReserveMember"] = "unit:UnitKey status:id baseCpa:int spentCp:CP history:ReserveHistory",
        ["ReserveHistory"] = "scope:ReleaseScope designationReceiptId:id? conversionReceiptId:id? releasedType:id? releaseReceiptId:id? releaseCycle:int? cpaBasis:int? voluntaryCeiling:int? offensiveCommitmentId:id? nextMovement:MovementException?",
        ["MovementException"] = "scope:ReleaseScope ordinal:int status:id completionReceiptId:id?",
        ["AttackHistory"] = "commitmentId:id cycleId:hash segmentId:id attacker:UnitKey defender:UnitKey targetLocationId:id gameTurn:int operationStage:int",
        ["TargetUse"] = "commitmentId:id segmentId:id targetLocationId:id",
        ["CP"] = "numerator:long denominator:int",

    };
}
