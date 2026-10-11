using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cna.Core.Campaigns;

/// <summary>Closed paid syntax; claim readers authenticate full ingress before comparison.</summary>
internal static class CampaignCombatActualPaidAssaultCodec
{
    private static readonly JsonSerializerOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    internal static void Require([DoesNotReturnIf(false)] bool condition, int code)
    { if (!condition) throw new JsonException($"CMB-APA-{code:000}"); }
    internal static int ByteLimit(string kind) => kind switch
    {
        "PaidSource" => 4_194_304,
        "PaidControl" or "World" => 262_144,
        "PaidEvent" or "AssaultResult" => 32_768,
        "PaidInput" or "PaidCommand" => 1_024,
        "Input" or "RoundInput" or "AdmissionSource" or "AdmissionProof" => 1_048_576,
        _ => 2_097_152,
    };
    private static int DepthLimit(string kind) => kind is "Input" or "RoundInput" ? 32 : 34;
    internal static JsonObject Object(ReadOnlySpan<byte> bytes, string kind)
        => JsonNode.Parse(Parse(bytes, kind).GetRawText())!.AsObject();
    internal static JsonElement Parse(ReadOnlySpan<byte> bytes, string kind)
    {
        Require(bytes.Length > 0 && bytes.Length <= ByteLimit(kind) && bytes.IndexOfAnyInRange((byte)128, byte.MaxValue) < 0, 1);
        Preparse(bytes, DepthLimit(kind));
        try
        {
            using var doc = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = DepthLimit(kind) + 1 });
            Bounds(doc.RootElement, 0, DepthLimit(kind));
            Require(bytes.SequenceEqual(Canonical(doc.RootElement, kind)), 8);
            return doc.RootElement.Clone();
        }
        catch (JsonException e) when (!e.Message.StartsWith("CMB-APA-", StringComparison.Ordinal))
        { throw new JsonException("CMB-APA-001", e); }
        catch (Exception e) when (e is InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentException)
        { throw new JsonException("CMB-APA-001", e); }
    }
    internal static byte[] Canonical(JsonNode value, string kind) => Canonical(JsonSerializer.SerializeToElement(value), kind);
    internal static byte[] Canonical(JsonElement value, string kind)
    {
        Bounds(value, 0, DepthLimit(kind));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping })) Write(writer, value, kind);
        var bytes = AsciiEscapes(stream.ToArray());
        Require(bytes.Length <= ByteLimit(kind), 1); return bytes;
    }
    // Python ensure_ascii: lowercase escapes, literal printable ASCII, and surrogate pairs.
    internal static byte[] Encode(JsonNode value)
        => AsciiEscapes(JsonSerializer.SerializeToUtf8Bytes(value, Options));
    private static byte[] AsciiEscapes(byte[] bytes)
    {
        var text = Encoding.UTF8.GetString(bytes);
        var output = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\\' && i + 1 < text.Length)
            {
                if (text[i + 1] == 'u')
                {
                    var n = int.Parse(text.AsSpan(i + 2, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    if (n is >= 32 and <= 126 && n is not 34 and not 92) output.Append((char)n);
                    else output.Append("\\u").Append(n.ToString("x4", CultureInfo.InvariantCulture));
                    i += 5;
                }
                else { output.Append(c).Append(text[++i]); }
            }
            else if (c > 127) output.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
            else output.Append(c);
        }
        return Encoding.ASCII.GetBytes(output.ToString());
    }
    // Enforce allocation bounds before JsonDocument allocates a nested document.
    private static void Preparse(ReadOnlySpan<byte> data, int maxDepth)
    {
        Span<byte> starts = stackalloc byte[35]; Span<int> commas = stackalloc int[35]; Span<bool> occupied = stackalloc bool[35];
        var depth = 0; var quoted = false; var escaped = false;
        foreach (var c in data)
        {
            if (quoted) { if (escaped) escaped = false; else if (c == 92) escaped = true; else if (c == 34) quoted = false; continue; }
            if (c is 9 or 10 or 13 or 32) continue;
            if (c == 34) { if (depth > 0) occupied[depth - 1] = true; quoted = true; continue; }
            if (c is 123 or 91)
            {
                if (depth > 0) occupied[depth - 1] = true; Require(depth < maxDepth + 1, 1);
                starts[depth] = c; commas[depth] = 0; occupied[depth] = false; depth++; continue;
            }
            if (c is 125 or 93)
            {
                Require(depth > 0 && (starts[depth - 1] == 123 && c == 125 || starts[depth - 1] == 91 && c == 93), 1);
                depth--; Require(commas[depth] + (occupied[depth] ? 1 : 0) <= (starts[depth] == 91 ? 512 : 128), 1); continue;
            }
            if (c == 44 && depth > 0) { commas[depth - 1]++; Require(commas[depth - 1] < (starts[depth - 1] == 91 ? 512 : 128), 1); }
            else if (depth > 0) occupied[depth - 1] = true;
        }
        Require(!quoted && depth == 0, 1);
    }
    private static void Bounds(JsonElement value, int depth, int maxDepth)
    {
        Require(depth <= maxDepth, 1);
        if (value.ValueKind == JsonValueKind.Array)
        { Require(value.GetArrayLength() <= 512, 1); foreach (var x in value.EnumerateArray()) Bounds(x, depth + 1, maxDepth); }
        if (value.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var x in value.EnumerateObject())
            {
                Require(names.Add(x.Name) && names.Count <= 128, 1);
                if (x.Value.ValueKind == JsonValueKind.Array && x.Name is "components" or "boundElementIds" or "references")
                    Require(x.Value.GetArrayLength() <= (x.Name == "references" ? 16 : 1), 1);
                Bounds(x.Value, depth + 1, maxDepth);
            }
        }
    }
    private static void Write(Utf8JsonWriter writer, JsonElement value, string kind, string parent = "", string field = "")
    {
        if (kind.EndsWith('?'))
        { if (value.ValueKind == JsonValueKind.Null) writer.WriteNullValue(); else Write(writer, value, kind[..^1], parent, field); return; }
        if (kind == "PaidEffect")
        {
            Require(value.ValueKind == JsonValueKind.Object && value.TryGetProperty("kind", out var tag) && tag.ValueKind == JsonValueKind.String, 1);
            Require(Tags.TryGetValue(value.GetProperty("kind").GetString()!, out var arm), 3); kind = arm!;
        }
        if (Shapes.TryGetValue(kind, out var shape))
        {
            Require(value.ValueKind == JsonValueKind.Object, 1);
            var fields = shape.Split(' ').Select(x => x.Split(':')).ToArray();
            Require(value.EnumerateObject().Select(x => x.Name).ToHashSet(StringComparer.Ordinal).SetEquals(fields.Select(x => x[0])), 1);
            if (kind == "ArithmeticAssessment") Require(value.GetProperty("scopeKind").GetString() == "pure-selected-arithmetic-v1" &&
                !value.GetProperty("paidAdmissionEnabled").GetBoolean() && !value.GetProperty("nativeSettlementCertified").GetBoolean() && !value.GetProperty("authenticSettlementCertified").GetBoolean(), 2);
            writer.WriteStartObject();
            foreach (var f in fields) { writer.WritePropertyName(f[0]); Write(writer, value.GetProperty(f[0]), f[1], kind, f[0]); }
            if (Versions.TryGetValue(kind, out var version)) Require(value.GetProperty(version.Field).GetInt64() == version.Value, 3);
            writer.WriteEndObject(); return;
        }
        if (kind.EndsWith("[]", StringComparison.Ordinal) || kind == "Route")
        {
            var cap = kind == "Route" ? 9 : ArrayBounds.GetValueOrDefault(parent + "." + field, 512);
            Require(value.ValueKind == JsonValueKind.Array && value.GetArrayLength() <= cap, 1);
            writer.WriteStartArray(); foreach (var x in value.EnumerateArray()) Write(writer, x, kind == "Route" ? "id" : kind[..^2]); writer.WriteEndArray(); return;
        }
        if (kind is "die" or "adjustment" or "differential" or "byte" or "signedInt" or "int" or "long" or "utc")
        {
            Require(value.ValueKind == JsonValueKind.Number && value.GetRawText().All(c => c is >= '0' and <= '9' or '-'), 1);
            Require(value.TryGetInt64(out var n), 2);
            var (lo, hi) = kind switch { "die" => (1L, 6L), "adjustment" => (-1L, 1L), "differential" => (-2L, 2L), "byte" => (0L, 255L), "signedInt" => ((long)int.MinValue, int.MaxValue), "int" => (0L, int.MaxValue), "utc" => (0L, 253402300799999L), _ => (0L, long.MaxValue) };
            Require(n >= lo && n <= hi, 2); writer.WriteNumberValue(n); return;
        }
        if (kind is "string" or "text" or "utf8")
        {
            Require(value.ValueKind == JsonValueKind.String, 1); var text = DecodeString(value.GetRawText());
            if (kind == "utf8") Require(text.Length is > 0 and <= 4_194_304 && text.All(c => c <= 127), 1);
            else Require(CharacterCount(text) <= 256, 2);
            writer.WriteRawValue(Quote(text)); return;
        }
        if (kind == "ulong")
        {
            Require(value.ValueKind == JsonValueKind.Number && value.GetRawText().All(c => c is >= '0' and <= '9' or '-'), 1);
            Require(value.TryGetUInt64(out var n), 2); writer.WriteNumberValue(n); return;
        }
        try
        {
            var inherited = CampaignCombatActualRoundEntryCodec.Canonical(value, kind);
            InheritedVersions(value); writer.WriteRawValue(inherited);
        }
        catch (JsonException e)
        { var code = e.Message.StartsWith("CMB-ARE-", StringComparison.Ordinal) && int.TryParse(e.Message.AsSpan(8, 3), out var c) ? c : 1; throw new JsonException($"CMB-APA-{code:000}", e); }
    }
    private static int CharacterCount(string text)
    {
        var count = 0;
        for (var i = 0; i < text.Length; i++, count++) if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) i++;
        return count;
    }
    private static string DecodeString(string raw)
    {
        var value = new StringBuilder(raw.Length);
        for (var i = 1; i < raw.Length - 1; i++)
        {
            if (raw[i] != '\\') { value.Append(raw[i]); continue; }
            var c = raw[++i];
            if (c == 'u') { value.Append((char)int.Parse(raw.AsSpan(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture)); i += 4; }
            else value.Append(c switch { 'b' => '\b', 'f' => '\f', 'n' => '\n', 'r' => '\r', 't' => '\t', _ => c });
        }
        return value.ToString();
    }
    private static string Quote(string text)
    {
        var value = new StringBuilder(text.Length + 2); value.Append('"');
        foreach (var c in text)
        {
            switch (c)
            {
                case '"': value.Append("\\\""); break;
                case '\\': value.Append("\\\\"); break;
                case '\b': value.Append("\\b"); break;
                case '\f': value.Append("\\f"); break;
                case '\n': value.Append("\\n"); break;
                case '\r': value.Append("\\r"); break;
                case '\t': value.Append("\\t"); break;
                default: if (c < 32 || c >= 127) value.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture)); else value.Append(c); break;
            }
        }
        return value.Append('"').ToString();
    }
    private static void InheritedVersions(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) { foreach (var child in value.EnumerateArray()) InheritedVersions(child); return; }
        if (value.ValueKind != JsonValueKind.Object) return;
        // All closed predecessor objects use version1 except Position5 and World7.
        // Run only after predecessor syntax validation; quoted carrier JSON remains atomic.
        foreach (var field in value.EnumerateObject()) InheritedVersions(field.Value);
        if (value.TryGetProperty("contractVersion", out var version))
        {
            var expected = value.TryGetProperty("elements", out _) && value.TryGetProperty("creationBinding", out _) ? 7
                : value.TryGetProperty("positionId", out _) && value.TryGetProperty("actorRole", out _) ? 5 : 1;
            Require(version.ValueKind == JsonValueKind.Number && version.TryGetInt64(out var n), 1);
            Require(version.GetInt64() == expected, 3);
        }
    }
    private static readonly Dictionary<string, string> Shapes = new(StringComparer.Ordinal)
    {
        ["Input"] = "command:Command actor:actor admittedAt:utc? clockAvailable:bool",
        ["Command"] = "contractVersion:int kind:id segmentId:id decisionId:id? fromPositionId:id? expectedPriorVersion:long? choice:id? candidate:Candidate? participant:UnitKey?",
        ["RoundInput"] = "command:RoundCommand actor:actor admittedAt:utc? clockAvailable:bool",
        ["RoundCommand"] = "contractVersion:int kind:id clockConfigurationHash:hash segmentId:id roundId:id? slotId:id? expectedPriorVersion:long? fromPositionId:id? allocation:Allocation?",
        ["PaidSource"] = "contractVersion:int admissionSourceCanonicalUtf8:utf8 paidEventCanonicalUtf8:utf8[]",
        ["PaidBase"] = "contractVersion:int dependencyManifestHash:hash admissionProof:AdmissionProof resultClockPolicyId:id",
        ["PaidCommand"] = "contractVersion:int kind:id baseHash:hash roundId:id expectedPriorVersion:long? commitmentId:id? decisionId:id? choice:id?",
        ["PaidInput"] = "command:PaidCommand actor:actor admittedAt:utc? clockAvailable:bool",
        ["PaidEvent"] = "contractVersion:int eventType:id author:actor campaignId:id rulesetHash:rawHash configurationHash:hash roundClockConfigurationHash:hash resultClockPolicyId:id admissionSourceHash:hash baseHash:hash cycleId:hash segmentId:id roundId:id priorVersion:long stateVersion:long priorPrefix:hash input:PaidInput effect:PaidEffect receiptId:id",
        ["PaidControl"] = "contractVersion:int baseHash:hash roundClockConfigurationHash:hash resultClockPolicyId:id stateVersion:long prefix:hash status:id commitmentId:id? result:AssaultResult? settlementId:id? window:ChoiceWindow? acceptedHighWater:utc world:World randomState:Random attackHistory:AttackHistory[] targetUses:TargetUse[] roundClosureReceiptId:id? caCompletionReceiptId:id? receipts:PaidReceipt[] closed:bool",
        ["PaidReceipt"] = "commandHash:hash eventHash:hash receiptId:id actor:actor stateVersion:long",
        ["PaidProof"] = "contractVersion:int sourceId:id sourceHash:hash base:PaidBase control:PaidControl",
        ["CommitEffect"] = "kind:id commitmentId:id allocations:Allocation[] costs:Cost[] preResultRandomState:Random",
        ["Cost"] = "unit:UnitKey beforeCp:int afterCp:int beforeAmmo:int afterAmmo:int",
        ["AttackHistory"] = "commitmentId:id cycleId:hash segmentId:id attacker:UnitKey defender:UnitKey targetLocationId:id gameTurn:int operationStage:int",
        ["TargetUse"] = "commitmentId:id segmentId:id targetLocationId:id",
        ["Draw"] = "purpose:id beforeCursor:ulong afterCursor:ulong die:int consumed:byte[]",
        ["AssaultResult"] = "resultId:id commitmentId:id rulesInputHash:hash procedureId:id beforeRandomState:Random afterRandomState:Random draws:Draw[] attackerMoraleCoordinate:int defenderMoraleCoordinate:int attackerMorale:adjustment defenderMorale:adjustment basicDifferential:int facts:Result",
        ["ChoiceWindow"] = "decisionId:id kind:id owner:side timing:Timing",
        ["ResolveEffect"] = "kind:id result:AssaultResult settlementId:id",
        ["WindowEffect"] = "kind:id window:ChoiceWindow",
        ["DispositionEffect"] = "kind:id reason:id timing:Timing? payload:Disposition",
        ["LossEffect"] = "kind:id payload:Losses",
        ["RetreatEffect"] = "kind:id payload:Retreat",
        ["CustodyEffect"] = "kind:id reason:id timing:Timing? payload:Custody",
        ["RelationsEffect"] = "kind:id payload:RelationsReceipt",
        ["CloseEffect"] = "kind:id settlementId:id proofReceipts:id[]",
        ["CaEffect"] = "kind:id fromPositionId:id toPositionId:id previousStepReceiptId:id roundClosureReceiptId:id",
        ["World"] = "contractVersion:int creationBinding:id elements:Element[] representations:Representation[] brokenVehicleLots:LegacyBrokenVehicleLot[] cohesionCauses:Cause[] relationships:Relationship[] custodyLots:Lot[] guards:Guard[] replacementEntitlements:Entitlement[] futureObligations:Obligation[] settlements:Settlement[]",
        ["Cause"] = "causeId:id ordinal:int receiptId:id elementId:id gameTurn:int operationStage:int kind:string points:int before:signedInt after:signedInt",
        ["Relationship"] = "relationId:id creationReceiptId:id kind:string attacker:UnitKey defender:UnitKey gameTurn:int operationStage:int active:bool endedByReceiptId:id? endingCause:string?",
        ["Lot"] = "lotId:id lossReceiptId:id originalComponent:ComponentKey captor:UnitKey quantity:int originLocationId:id currentLocationId:id? status:string guardId:id? escapeReceiptId:id?",
        ["Guard"] = "guardId:id formationReceiptId:id lotId:id originComponent:ComponentKey currentLocationId:id toe:int baseCapabilityPointAllowance:int offensiveCloseAssaultRating:int defensiveCloseAssaultRating:int operationalState:Operational ammunition:Ammunition readiness:Readiness",
        ["Entitlement"] = "entitlementId:id escapeReceiptId:id lotId:id originalComponent:ComponentKey quantity:int reunionLocationId:id earnedScope:Scope delayOperationStages:int eligibleScope:FutureScope status:string",
        ["Obligation"] = "obligationId:id receiptId:id kind:string subjectId:id earnedScope:Scope eligibleScope:FutureScope? activationGate:string status:string",
        ["Result"] = "differential:differential attackerCoordinate:int defenderCoordinate:int captureDie:int? attackerPercent:int defenderPercent:int rawEngaged:bool requiredRetreat:int capturedRole:string? captureShare:int",
        ["Disposition"] = "receiptId:id predecessorReceiptId:id kind:string requiredDistance:int plannedDistance:int unfulfilledDistance:int route:Route",
        ["RoleLoss"] = "role:string component:ComponentKey committedToe:int tablePercent:int refusalPercent:int lossToe:int capturedToe:int otherLossToe:int remainingToe:int lossDp:int",
        ["Losses"] = "receiptId:id predecessorReceiptId:id roles:RoleLoss[]",
        ["Retreat"] = "receiptId:id predecessorReceiptId:id kind:string route:Route completedDistance:int beforeCp:Cp afterCp:Cp excessCpDp:int attackerVictoryRp:int",
        ["Custody"] = "receiptId:id predecessorReceiptId:id lotId:id kind:string route:Route guardId:id? entitlementId:id? donorToeBefore:int donorToeAfter:int",
        ["RelationsReceipt"] = "receiptId:id predecessorReceiptId:id relationshipId:id? kind:string?",
        ["Settlement"] = "settlementId:id commitmentId:id resultId:id gameTurn:int operationStage:int attacker:UnitKey defender:UnitKey preLossElements:Element[] result:Result disposition:Disposition? losses:Losses? retreat:Retreat? custody:Custody? relationships:RelationsReceipt?",
        ["CommitmentPreimage"] = "baseHash:hash priorVersion:long priorPrefix:hash roundId:id allocations:Allocation[] costs:Cost[] preResultRandomState:Random",
        ["AdmissionSource"] = "contractVersion:int actualRoundSourceCanonicalUtf8:utf8",
        ["ArithmeticAssessment"] = "contractVersion:int scopeKind:id actualRoundSourceHash:hash dependencyManifestHash:hash rulesInputHash:hash moralePairs:int assaultPairs:int captureRefusalVariants:int arithmeticCatalogueHash:hash paidAdmissionEnabled:bool nativeSettlementCertified:bool authenticSettlementCertified:bool",
        ["AdmissionProof"] = "contractVersion:int sourceId:id sourceHash:hash actualRoundProof:ActualRoundProof arithmeticAssessment:ArithmeticAssessment",
        ["header"] = "kind:id catalogueVersion:int rulesInputHash:hash dependencyManifestHash:hash moralePairs:int assaultPairs:int captureRefusalVariants:int totalRecords:int",
        ["MoraleRow"] = "kind:id ordinal:int attackerMoraleCoordinate:int defenderMoraleCoordinate:int attackerAdjustment:adjustment defenderAdjustment:adjustment differential:differential",
        ["AssaultCell"] = "kind:id ordinal:int differential:differential attackerCoordinate:int defenderCoordinate:int attackerPercent:int defenderPercent:int rawEngaged:bool requiredRetreat:int capturedRole:role? captureTriggered:bool",
        ["VariantRow"] = "kind:id ordinal:int differential:differential attackerCoordinate:int defenderCoordinate:int captureDie:die? refusalHexes:int facts:Result attackerLossToe:int defenderLossToe:int attackerCapturedToe:int defenderCapturedToe:int attackerOtherLossToe:int defenderOtherLossToe:int attackerRemainingToe:int defenderRemainingToe:int attackerLossDp:int defenderLossDp:int",
    };
    private static readonly Dictionary<string, string> Tags = new(StringComparer.Ordinal)
    {
        ["attack-committed"] = "CommitEffect",
        ["assault-resolved"] = "ResolveEffect",
        ["choice-opened"] = "WindowEffect",
        ["disposition-recorded"] = "DispositionEffect",
        ["losses-settled"] = "LossEffect",
        ["retreat-settled"] = "RetreatEffect",
        ["custody-settled"] = "CustodyEffect",
        ["relationships-settled"] = "RelationsEffect",
        ["round-closed"] = "CloseEffect",
        ["ca-completed"] = "CaEffect",
    };
    private static readonly Dictionary<string, int> ArrayBounds = new(StringComparer.Ordinal)
    {
        ["CommitEffect.allocations"] = 2,
        ["CommitEffect.costs"] = 2,
        ["AssaultResult.draws"] = 9,
        ["Draw.consumed"] = 512,
        ["CloseEffect.proofReceipts"] = 5,
        ["PaidControl.attackHistory"] = 1,
        ["PaidControl.targetUses"] = 1,
        ["PaidControl.receipts"] = 11,
        ["World.elements"] = 2,
        ["World.representations"] = 2,
        ["World.brokenVehicleLots"] = 0,
        ["World.cohesionCauses"] = 4,
        ["World.relationships"] = 1,
        ["World.custodyLots"] = 1,
        ["World.guards"] = 1,
        ["World.replacementEntitlements"] = 1,
        ["World.futureObligations"] = 1,
        ["World.settlements"] = 1,
        ["Settlement.preLossElements"] = 2,
        ["Losses.roles"] = 2,
        ["Element.components"] = 1,
        ["Origin.references"] = 16,
        ["Representation.boundElementIds"] = 1,
        ["CommitmentPreimage.allocations"] = 2,
        ["CommitmentPreimage.costs"] = 2,
    };
    private static readonly Dictionary<string, (string Field, int Value)> Versions = new(StringComparer.Ordinal)
    {
        ["Command"] = ("contractVersion", 1),
        ["RoundCommand"] = ("contractVersion", 1),
        ["PaidSource"] = ("contractVersion", 1),
        ["PaidBase"] = ("contractVersion", 1),
        ["PaidCommand"] = ("contractVersion", 1),
        ["PaidEvent"] = ("contractVersion", 1),
        ["PaidControl"] = ("contractVersion", 1),
        ["PaidProof"] = ("contractVersion", 1),
        ["World"] = ("contractVersion", 7),
        ["AdmissionSource"] = ("contractVersion", 1),
        ["AdmissionProof"] = ("contractVersion", 1),
        ["ArithmeticAssessment"] = ("contractVersion", 1),
        ["header"] = ("catalogueVersion", 1),
    };
}
