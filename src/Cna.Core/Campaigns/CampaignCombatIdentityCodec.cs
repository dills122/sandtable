using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignCombatIdentityCodec
{
    public static byte[] SerializeParticipant(CampaignCombatParticipant participant)
    {
        ArgumentNullException.ThrowIfNull(participant);
        return Bytes(writer =>
        {
            writer.WriteStartObject(); writer.WriteStartObject("unit");
            writer.WriteString("creationBinding", participant.Unit.CreationBinding);
            writer.WriteString("originalSide", participant.Unit.OriginalSide);
            writer.WriteString("elementId", participant.Unit.ElementId); writer.WriteEndObject();
            writer.WriteString("representationId", participant.RepresentationId);
            writer.WriteString("locationId", participant.LocationId);
            writer.WriteStartArray("componentIds");
            foreach (var component in participant.ComponentIds) writer.WriteStringValue(component);
            writer.WriteEndArray(); writer.WriteEndObject();
        });
    }

    public static CampaignCombatParticipant ReadParticipant(ReadOnlySpan<byte> bytes,
        CampaignCombatCreationRequest request, CampaignWorldSnapshotV7 world, CampaignCombatUnitKey unit)
    {
        ValidateBounds(bytes);
        var supplied = ParseParticipant(bytes);
        if (!bytes.SequenceEqual(SerializeParticipant(supplied))) throw new JsonException("Noncanonical Participant bytes.");
        // Selection-v1 requires shape/bounds, then canonical spelling, then trusted-state semantics.
        // The parsed value establishes syntax only; independently trusted context still owns binding.
        var expected = CampaignCombatCertification.BindParticipant(request, world, unit);
        if (!bytes.SequenceEqual(SerializeParticipant(expected))) throw new JsonException("Participant differs from canonical current unit binding.");
        return expected;
    }

    private static CampaignCombatParticipant ParseParticipant(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 33 });
            var root = document.RootElement;
            Fields(root, ["unit", "representationId", "locationId", "componentIds"]);
            var unit = root.GetProperty("unit");
            Fields(unit, ["creationBinding", "originalSide", "elementId"]);
            var components = root.GetProperty("componentIds");
            if (components.ValueKind != JsonValueKind.Array) throw new JsonException("Participant components must be an array.");
            return new(new CampaignCombatUnitKey(Text(unit.GetProperty("creationBinding")), Text(unit.GetProperty("originalSide")), Text(unit.GetProperty("elementId"))),
                Text(root.GetProperty("representationId")), Text(root.GetProperty("locationId")), components.EnumerateArray().Select(Text).ToArray());
        }
        catch (ArgumentException error) { throw new JsonException("Invalid Participant primitive.", error); }

        static string Text(JsonElement value)
        {
            if (value.ValueKind != JsonValueKind.String) throw new JsonException("Participant IDs must be strings.");
            return value.GetString()!;
        }
        static void Fields(JsonElement value, string[] expected)
        {
            if (value.ValueKind != JsonValueKind.Object) throw new JsonException("Participant requires closed objects.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
                if (!expected.Contains(property.Name, StringComparer.Ordinal) || !seen.Add(property.Name))
                    throw new JsonException("Unknown or duplicate Participant field.");
            if (seen.Count != expected.Length) throw new JsonException("Missing Participant field.");
        }
    }

    public static byte[] SerializeBoundary(CampaignCombatAdmissionBoundary boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        return Bytes(writer =>
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1);
            writer.WritePropertyName("entry"); writer.WriteRawValue(CampaignCombatBreakdownCompletionCodec.SerializeState(boundary.Entry));
            writer.WriteStartObject("assessment");
            var value = boundary.Assessment;
            writer.WriteString("actingSide", value.Acting.Unit.OriginalSide);
            writer.WriteString("actingElementId", value.Acting.Unit.ElementId);
            writer.WriteString("defendingSide", value.Defending.Unit.OriginalSide);
            writer.WriteString("defendingElementId", value.Defending.Unit.ElementId);
            writer.WriteString("actingLocationId", value.Acting.LocationId);
            writer.WriteString("defendingLocationId", value.Defending.LocationId);
            writer.WriteBoolean("normalWeather", value.NormalWeather); writer.WriteBoolean("adjacent", value.Adjacent);
            writer.WritePropertyName("actingCapabilityPointsExpended"); CapabilityPointAmountCodec.WriteCanonical(writer, value.ActingCapabilityPointsExpended);
            writer.WritePropertyName("defendingCapabilityPointsExpended"); CapabilityPointAmountCodec.WriteCanonical(writer, value.DefendingCapabilityPointsExpended);
            writer.WriteBoolean("actingWithinVoluntaryCeiling", value.ActingWithinVoluntaryCeiling);
            writer.WriteBoolean("defendingWithinVoluntaryCeiling", value.DefendingWithinVoluntaryCeiling);
            writer.WriteStartArray("candidateIds"); writer.WriteEndArray();
            writer.WriteEndObject(); writer.WriteEndObject();
        });
    }

    public static CampaignCombatAdmissionBoundary ReadBoundary(ReadOnlySpan<byte> bytes,
        CampaignCombatCreationRequest request, ReadOnlySpan<byte> retainedCreated, IReadOnlyList<byte[]> events)
    {
        ValidateBounds(bytes);
        ValidateBoundarySyntax(bytes);
        var expected = CampaignCombatCertification.Admit(request, retainedCreated, events);
        if (!bytes.SequenceEqual(SerializeBoundary(expected))) throw new JsonException("AdmissionBoundary differs from canonical retained G2 history.");
        return expected;
    }

    // Frozen transitive grammar of inherited-selection-v1 AdmissionBoundary and G2 entry.
    // These descriptors establish syntax only. No parsed value becomes trusted campaign state.
    // Sources: inherited-breakdown-completion, inherited-movement-lifecycle/movement, reserve-
    // designation, inherited-successors, stage-entry and their World/authority dependencies.
    private static readonly Dictionary<string, string> BoundaryShapes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["AdmissionBoundary"] = "contractVersion:int entry:CombatEntryState assessment:CandidateAssessment",
        ["CombatEntryState"] = "contractVersion:int campaignId:id rulesetHash:rawHash configurationHash:hash creationBinding:id creationEventHash:hash stateVersion:long prefix:hash sequencePosition:Position initiativeHolder:side operationStageOrders:PreambleOrder[] operationStageWeather:WeatherValue[] world:World randomState:Random receipts:PreambleReceipt[] firstActingSide:side members:ReserveMember[] cycle:Authority? cycleId:hash? openingBaseHash:hash? completionReceiptId:id? tracks:InheritedTrack[] actualProgressRefs:ActualProgressRef[] breakdownFlow:LifecycleFlow interruptContext:InterruptContext? movementEnd:MovementEndProof? breakdownCompletionReceiptId:id?",
        ["Position"] = "contractVersion:int positionId:id gameTurn:int operationStage:int stageId:id phaseId:id segmentId:id? stepId:id? actorRole:id activeSide:id? sources:Reference[]",
        ["Reference"] = "sourceId:id locator:locator",
        ["PreambleOrder"] = "contractVersion:int gameTurn:int operationStage:int firstSide:side secondSide:side",
        ["WeatherValue"] = "contractVersion:int gameTurn:int operationStage:int determiningSide:side season:id firstDie:int secondDie:int kind:id scope:id locationDie:int? affectedAreas:id[] fuelWaterReductionSubjectCount:int restoredWellCount:int damagedGroundedAircraftCount:int",
        ["World"] = "contractVersion:int creationBinding:id elements:Element[] representations:Representation[] brokenVehicleLots:LegacyBrokenVehicleLot[] cohesionCauses:Cause[] relationships:Relationship[] custodyLots:Lot[] guards:Guard[] replacementEntitlements:Entitlement[] futureObligations:Obligation[] settlements:Settlement[]",
        ["Element"] = "elementId:id currentLocationId:id reserveStatus:string operationalState:Operational components:Component[] sourceParentFormationId:id currentParentFormationId:id ammunition:Ammunition readiness:Readiness",
        ["Operational"] = "ledgerGameTurn:int ledgerOperationStage:int capabilityPointsExpended:Cp cohesionLevel:int vehicleBreakdownState:null movementEnded:null initialLedgerOrigin:Origin",
        ["Cp"] = "numerator:long denominator:int",
        ["Origin"] = "kind:string references:Reference[]",
        ["Component"] = "componentId:id currentToe:int initialToeOrigin:Origin",
        ["Ammunition"] = "points:int initialAmmunitionOrigin:Origin",
        ["Readiness"] = "gameTurn:int operationStage:int waterStatus:string storesStatus:string pinned:bool initialReadinessOrigin:Origin",
        ["Representation"] = "representationId:id currentLocationId:id bindingKind:string boundElementIds:id[]",
        ["Cause"] = "causeId:id ordinal:int receiptId:id elementId:id gameTurn:int operationStage:int kind:string points:int before:int after:int",
        ["Relationship"] = "relationId:id creationReceiptId:id kind:string attacker:UnitKey defender:UnitKey gameTurn:int operationStage:int active:bool endedByReceiptId:id? endingCause:string?",
        ["UnitKey"] = "creationBinding:id originalSide:side elementId:id",
        ["Lot"] = "lotId:id lossReceiptId:id originalComponent:ComponentKey captor:UnitKey quantity:int originLocationId:id currentLocationId:id? status:string guardId:id? escapeReceiptId:id?",
        ["ComponentKey"] = "unit:UnitKey componentId:id",
        ["Guard"] = "guardId:id formationReceiptId:id lotId:id originComponent:ComponentKey currentLocationId:id toe:int baseCapabilityPointAllowance:int offensiveCloseAssaultRating:int defensiveCloseAssaultRating:int operationalState:Operational ammunition:Ammunition readiness:Readiness",
        ["Entitlement"] = "entitlementId:id escapeReceiptId:id lotId:id originalComponent:ComponentKey quantity:int reunionLocationId:id earnedScope:Scope delayOperationStages:int eligibleScope:FutureScope status:string",
        ["Scope"] = "gameTurn:int operationStage:int",
        ["FutureScope"] = "gameTurn:futureTurn operationStage:int",
        ["Obligation"] = "obligationId:id receiptId:id kind:string subjectId:id earnedScope:Scope eligibleScope:FutureScope? activationGate:string status:string",
        ["Settlement"] = "settlementId:id commitmentId:id resultId:id gameTurn:int operationStage:int attacker:UnitKey defender:UnitKey preLossElements:Element[] result:Result disposition:Disposition? losses:Losses? retreat:Retreat? custody:Custody? relationships:RelationsReceipt?",
        ["Result"] = "differential:int attackerCoordinate:int defenderCoordinate:int captureDie:int? attackerPercent:int defenderPercent:int rawEngaged:bool requiredRetreat:int capturedRole:string? captureShare:int",
        ["Disposition"] = "receiptId:id predecessorReceiptId:id kind:string requiredDistance:int plannedDistance:int unfulfilledDistance:int route:Route",
        ["Losses"] = "receiptId:id predecessorReceiptId:id roles:RoleLoss[]",
        ["RoleLoss"] = "role:string component:ComponentKey committedToe:int tablePercent:int refusalPercent:int lossToe:int capturedToe:int otherLossToe:int remainingToe:int lossDp:int",
        ["Retreat"] = "receiptId:id predecessorReceiptId:id kind:string route:Route completedDistance:int beforeCp:Cp afterCp:Cp excessCpDp:int attackerVictoryRp:int",
        ["Custody"] = "receiptId:id predecessorReceiptId:id lotId:id kind:string route:Route guardId:id? entitlementId:id? donorToeBefore:int donorToeAfter:int",
        ["RelationsReceipt"] = "receiptId:id predecessorReceiptId:id relationshipId:id? kind:string?",
        ["Random"] = "contractVersion:int algorithmId:id seed:ulong nextByteCursor:ulong",
        ["PreambleReceipt"] = "commandHash:hash eventHash:hash receiptId:id actor:actor stateVersion:long",
        ["ReserveMember"] = "unit:UnitKey status:id baseCpa:int spentCp:Cp history:ReleaseHistory",
        ["ReleaseHistory"] = "scope:ReleaseScope designationReceiptId:id? conversionReceiptId:id? releasedType:id? releaseReceiptId:id? releaseCycle:int? cpaBasis:int? voluntaryCeiling:int? offensiveCommitmentId:id? nextMovement:MovementException?",
        ["ReleaseScope"] = "gameTurn:int operationStage:int playerPhaseSlot:id actingSide:side",
        ["MovementException"] = "scope:ReleaseScope ordinal:int status:id completionReceiptId:id?",
        ["Authority"] = "contractVersion:int campaignId:id rulesetHash:rawHash setupId:id setupHash:hash contentPackId:id contentHash:hash scenarioId:id gameTurn:int operationStage:int playerPhaseSlot:id actingSide:id ordinal:int openedAuthorityVersion:long openingPrefix:hash admittedPolicyBundleDigest:hash",
        ["InheritedTrack"] = "unit:UnitKey route:OrderedLocations",
        ["ActualProgressRef"] = "eventType:id receiptId:id eventHash:hash",
        ["MovingFlow"] = "kind:id route:MovingRoute",
        ["MovingRoute"] = "routeId:hash firstMoveStateVersion:long elementId:id representationId:id owner:side originLocationId:id currentLocationId:id cohortIds:empty",
        ["PhasingStopFlow"] = "kind:id stop:LifecycleStop",
        ["LifecycleStop"] = "stopId:hash recordedStateVersion:long route:MovingRoute reason:id weatherKind:id cohortInputs:empty",
        ["IdleFlow"] = "kind:id",
        ["InterruptContext"] = "cycle:Authority cycleId:hash sequencePosition:Position",
        ["MovementEndProof"] = "scope:ReleaseScope ordinal:int completionReceiptId:id endLocations:EndLocation[] excludedBefore:UnitKey[]",
        ["EndLocation"] = "unit:UnitKey locationId:id",
        ["CandidateAssessment"] = "actingSide:side actingElementId:id defendingSide:side defendingElementId:id actingLocationId:id defendingLocationId:id normalWeather:bool adjacent:bool actingCapabilityPointsExpended:Cp defendingCapabilityPointsExpended:Cp actingWithinVoluntaryCeiling:bool defendingWithinVoluntaryCeiling:bool candidateIds:id[]",
    };

    private static void ValidateBoundarySyntax(ReadOnlySpan<byte> bytes)
    {
        using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 33 });
        using var stream = new MemoryStream();
        // Frozen primitives are ASCII. Relaxed encoding matches the oracle's literal printable
        // ASCII spelling; escaping, numeric spelling and property ordering remain byte-exact.
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            WriteBoundarySyntax(writer, document.RootElement, "AdmissionBoundary");
        if (!bytes.SequenceEqual(stream.ToArray())) throw new JsonException("Noncanonical AdmissionBoundary bytes.");
    }

    private static void WriteBoundarySyntax(Utf8JsonWriter writer, JsonElement value, string kind, bool assessmentArray = false)
    {
        if (kind.EndsWith('?'))
        {
            if (value.ValueKind == JsonValueKind.Null) writer.WriteNullValue();
            else WriteBoundarySyntax(writer, value, kind[..^1]);
            return;
        }
        if (kind == "LifecycleFlow")
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("kind", out var tag) || tag.ValueKind != JsonValueKind.String)
                throw new JsonException("Missing lifecycle flow tag.");
            kind = tag.GetString() switch
            {
                "moving" => "MovingFlow",
                "phasing-stop" => "PhasingStopFlow",
                "idle" => "IdleFlow",
                _ => throw new JsonException("Unknown lifecycle flow tag."),
            };
        }
        if (BoundaryShapes.TryGetValue(kind, out var shape))
        {
            if (value.ValueKind != JsonValueKind.Object) throw new JsonException("Boundary requires closed objects.");
            var fields = shape.Split(' ').Select(field => field.Split(':')).ToArray();
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in value.EnumerateObject())
                if (!names.Add(property.Name) || !fields.Any(field => field[0] == property.Name)) throw new JsonException("Unknown or duplicate Boundary field.");
            if (names.Count != fields.Length) throw new JsonException("Missing Boundary field.");
            writer.WriteStartObject();
            foreach (var field in fields)
            {
                writer.WritePropertyName(field[0]);
                WriteBoundarySyntax(writer, value.GetProperty(field[0]), field[1], kind == "CandidateAssessment");
            }
            writer.WriteEndObject(); return;
        }
        if (kind.EndsWith("[]", StringComparison.Ordinal) || kind is "Route" or "OrderedLocations" or "empty")
        {
            if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() > 512 ||
                kind == "empty" && value.GetArrayLength() != 0 || kind == "OrderedLocations" && value.GetArrayLength() is < 2 or > 33)
                throw new JsonException("Invalid Boundary array.");
            var child = kind.EndsWith("[]", StringComparison.Ordinal) ? kind[..^2] : "id";
            var items = value.EnumerateArray().ToArray();
            writer.WriteStartArray();
            foreach (var item in items) WriteBoundarySyntax(writer, item, child);
            writer.WriteEndArray();
            // Canonical external arrays sort by frozen keys; route order and assessment IDs
            // remain semantic. Validate order after children so malformed keys cannot escape.
            if (!assessmentArray && kind is not ("Route" or "OrderedLocations" or "empty"))
                for (var i = 1; i < items.Length; i++)
                    if (CompareBoundaryItems(items[i - 1], items[i], child) > 0) throw new JsonException("Noncanonical Boundary array order.");
            return;
        }
        switch (kind)
        {
            case "null":
                if (value.ValueKind != JsonValueKind.Null) throw new JsonException("Expected null Boundary value.");
                writer.WriteNullValue(); return;
            case "bool":
                if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw new JsonException("Expected Boundary Boolean.");
                writer.WriteBooleanValue(value.GetBoolean()); return;
            case "int":
            case "long":
            case "futureTurn":
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var number) ||
                    kind == "int" && number is < int.MinValue or > int.MaxValue || kind == "futureTurn" && number is < 1 or > 115)
                    throw new JsonException("Invalid Boundary signed integer.");
                writer.WriteNumberValue(number); return;
            case "ulong":
                if (value.ValueKind != JsonValueKind.Number || !value.TryGetUInt64(out var unsigned)) throw new JsonException("Invalid Boundary UInt64.");
                writer.WriteNumberValue(unsigned); return;
        }
        if (value.ValueKind != JsonValueKind.String) throw new JsonException("Invalid Boundary string primitive.");
        var text = value.GetString()!;
        var valid = kind switch
        {
            "id" => text.Length is > 0 and <= 128 && AsciiAlphaNumeric(text[0]) && text.All(c => AsciiAlphaNumeric(c) || c is '.' or '_' or ':' or '-'),
            "hash" => text.StartsWith("sha256:", StringComparison.Ordinal) && LowerHex(text[7..]),
            "rawHash" => LowerHex(text),
            "string" or "locator" => text.Length is > 0 and <= 512 && text.All(c => c is >= ' ' and <= '~'),
            "side" => text is "axis" or "commonwealth",
            "actor" => text is "axis" or "commonwealth" or "system",
            _ => false,
        };
        if (!valid) throw new JsonException("Invalid Boundary string grammar.");
        writer.WriteStringValue(text);
        static bool AsciiAlphaNumeric(char c) => c is >= 'a' and <= 'z' or >= 'A' and <= 'Z' or >= '0' and <= '9';
        static bool LowerHex(string text) => text.Length == 64 && text.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    }

    private static int CompareBoundaryItems(JsonElement left, JsonElement right, string kind)
    {
        if (kind == "id") return StringComparer.Ordinal.Compare(left.GetString(), right.GetString());
        if (kind == "Cause") return left.GetProperty("ordinal").GetInt32().CompareTo(right.GetProperty("ordinal").GetInt32());
        if (kind == "EndLocation") return CompareBoundaryItems(left.GetProperty("unit"), right.GetProperty("unit"), "UnitKey");
        string[] fields = kind switch
        {
            "UnitKey" => ["creationBinding", "originalSide", "elementId"],
            "Reference" => ["sourceId", "locator"],
            "Element" => ["elementId"],
            "Component" => ["componentId"],
            "Representation" => ["representationId"],
            "Relationship" => ["relationId"],
            "Lot" => ["lotId"],
            "Guard" => ["guardId"],
            "Entitlement" => ["entitlementId"],
            "Obligation" => ["obligationId"],
            "Settlement" => ["settlementId"],
            "RoleLoss" => ["role"],
            _ => [],
        };
        foreach (var field in fields)
        {
            var compared = StringComparer.Ordinal.Compare(left.GetProperty(field).GetString(), right.GetProperty(field).GetString());
            if (compared != 0) return compared;
        }
        return 0;
    }

    private static byte[] Bytes(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) write(writer);
        var bytes = stream.ToArray(); ValidateBounds(bytes); return bytes;
    }

    private static void ValidateBounds(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length is 0 or > 1_048_576) throw new JsonException("Missing or oversized Combat identity value.");
        using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 33 });
        Check(document.RootElement, 0);
        static void Check(JsonElement value, int depth)
        {
            if (depth > 32) throw new JsonException("Combat identity exceeds depth limit.");
            if (value.ValueKind == JsonValueKind.Array)
            {
                if (value.GetArrayLength() > 512) throw new JsonException("Combat identity exceeds array limit.");
                foreach (var child in value.EnumerateArray()) Check(child, depth + 1);
            }
            else if (value.ValueKind == JsonValueKind.Object)
                foreach (var property in value.EnumerateObject()) Check(property.Value, depth + 1);
        }
    }
}
