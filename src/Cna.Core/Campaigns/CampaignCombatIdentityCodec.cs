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

    public static byte[] SerializeCandidate(CampaignCombatCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return Bytes(writer =>
        {
            writer.WriteStartObject();
            writer.WritePropertyName("attacker"); writer.WriteRawValue(SerializeParticipant(candidate.Attacker));
            writer.WritePropertyName("defender"); writer.WriteRawValue(SerializeParticipant(candidate.Defender));
            writer.WriteString("targetLocationId", candidate.TargetLocationId); writer.WriteString("basis", candidate.Basis);
            writer.WriteEndObject();
        });
    }

    public static CampaignCombatCandidate ReadCandidate(ReadOnlySpan<byte> bytes, CampaignCombatCandidate trustedExpected)
    {
        ValidateBounds(bytes);
        using var document = JsonDocument.Parse(bytes.ToArray()); var root = document.RootElement;
        string[] fields = ["attacker", "defender", "targetLocationId", "basis"];
        if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != fields.Length ||
            fields.Any(field => !root.TryGetProperty(field, out _))) throw new JsonException("Candidate requires its closed shape.");
        foreach (var field in fields[2..])
            if (root.GetProperty(field).ValueKind != JsonValueKind.String) throw new JsonException("Candidate requires string IDs.");
        var parsed = new CampaignCombatCandidate(
            ParseParticipant(System.Text.Encoding.UTF8.GetBytes(root.GetProperty("attacker").GetRawText())),
            ParseParticipant(System.Text.Encoding.UTF8.GetBytes(root.GetProperty("defender").GetRawText())),
            root.GetProperty("targetLocationId").GetString()!, root.GetProperty("basis").GetString()!);
        if (!bytes.SequenceEqual(SerializeCandidate(parsed))) throw new JsonException("Noncanonical Candidate bytes.");
        ArgumentNullException.ThrowIfNull(trustedExpected);
        if (!bytes.SequenceEqual(SerializeCandidate(trustedExpected))) throw new JsonException("Candidate differs from independently certified facts.");
        return trustedExpected;
    }

    /// <summary>Pure Round2 digest primitive; caller authenticates Base2, FA position and decline receipt.</summary>
    public static string CalculateOpportunityId(string baseHash, string cycleId, string positionId,
        CampaignCombatCandidate candidate, string declineReceiptId)
    {
        var bytes = Bytes(writer =>
        {
            writer.WriteStartObject();
            foreach (var (name, value, kind) in new[] { ("baseHash", baseHash, "hash"), ("cycleId", cycleId, "hash"), ("positionId", positionId, "id") })
            {
                writer.WritePropertyName(name); WriteBoundarySyntax(writer, JsonSerializer.SerializeToElement(value), kind);
            }
            writer.WritePropertyName("candidate"); writer.WriteRawValue(SerializeCandidate(candidate));
            writer.WritePropertyName("declineReceiptId"); WriteBoundarySyntax(writer, JsonSerializer.SerializeToElement(declineReceiptId), "id");
            writer.WriteEndObject();
        });
        return "opp." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.opportunity.v2", bytes)[7..];
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
        if (kind == "InheritedTraversalEffect" && (value.ValueKind != JsonValueKind.Object ||
            !value.TryGetProperty("kind", out var traversalTag) || traversalTag.ValueKind != JsonValueKind.String || traversalTag.GetString() != "step-completed"))
            throw new JsonException("Unknown inherited traversal effect.");
        if (kind == "InheritedSelectionEffect")
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty("kind", out var effectTag) || effectTag.ValueKind != JsonValueKind.String)
                throw new JsonException("Missing inherited selection effect tag.");
            kind = effectTag.GetString() switch { "segment-opened" => "InheritedSelectionOpen", "selection-closed" => "InheritedSelectionClose", _ => throw new JsonException("Unknown inherited selection effect.") };
        }
        if (BoundaryShapes.TryGetValue(kind, out var shape) || InheritedStepShapes.TryGetValue(kind, out shape))
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
                WriteBoundarySyntax(writer, value.GetProperty(field[0]), field[1], kind == "CandidateAssessment" || InheritedStepShapes.ContainsKey(kind));
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

    public static byte[] SerializeSelectionInput(CampaignCombatInheritedSelection.Input input)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentNullException.ThrowIfNull(input.Command);
        var command = input.Command;
        var bytes = Bytes(writer =>
        {
            writer.WriteStartObject(); writer.WriteStartObject("command");
            writer.WriteNumber("contractVersion", command.ContractVersion); writer.WriteString("kind", command.Kind); writer.WriteString("segmentId", command.SegmentId);
            writer.WriteNumber("expectedPriorVersion", command.ExpectedPriorVersion); writer.WriteString("expectedPositionId", command.ExpectedPositionId);
            writer.WriteString("openingReceiptId", command.OpeningReceiptId); writer.WriteEndObject(); writer.WriteString("actor", StepActor(input.Actor)); writer.WriteEndObject();
        });
        ValidateStepSyntax(bytes, "InheritedSelectionInput"); return bytes;
    }
    public static byte[] SerializeTraversalInput(CampaignCombatInheritedNoAttack.Input input)
    {
        ArgumentNullException.ThrowIfNull(input); ArgumentNullException.ThrowIfNull(input.Command);
        var command = input.Command;
        var bytes = Bytes(writer =>
        {
            writer.WriteStartObject(); writer.WriteStartObject("command");
            writer.WriteNumber("contractVersion", command.ContractVersion); writer.WriteString("kind", command.Kind); writer.WriteString("segmentId", command.SegmentId);
            writer.WriteNumber("expectedPriorVersion", command.ExpectedPriorVersion); writer.WriteString("fromPositionId", command.FromPositionId);
            writer.WriteString("dispositionReceiptId", command.DispositionReceiptId); writer.WriteEndObject(); writer.WriteString("actor", StepActor(input.Actor)); writer.WriteEndObject();
        });
        ValidateStepSyntax(bytes, "InheritedTraversalInput"); return bytes;
    }
    internal static CampaignCombatInheritedSelection.Input ReadSelectionEventInput(byte[] bytes)
    {
        ValidateStepSyntax(bytes, "InheritedSelectionEvent");
        using var doc = JsonDocument.Parse(bytes); var input = doc.RootElement.GetProperty("input"); var command = input.GetProperty("command");
        var opening = command.GetProperty("openingReceiptId");
        return new(new(command.GetProperty("contractVersion").GetInt32(), command.GetProperty("kind").GetString()!, command.GetProperty("segmentId").GetString()!,
            command.GetProperty("expectedPriorVersion").GetInt64(), command.GetProperty("expectedPositionId").GetString()!, opening.ValueKind == JsonValueKind.Null ? null : opening.GetString()), ReadStepActor(input));
    }
    internal static CampaignCombatInheritedNoAttack.Input ReadTraversalEventInput(byte[] bytes)
    {
        ValidateStepSyntax(bytes, "InheritedTraversalEvent");
        using var doc = JsonDocument.Parse(bytes); var input = doc.RootElement.GetProperty("input"); var command = input.GetProperty("command");
        return new(new(command.GetProperty("contractVersion").GetInt32(), command.GetProperty("kind").GetString()!, command.GetProperty("segmentId").GetString()!,
            command.GetProperty("expectedPriorVersion").GetInt64(), command.GetProperty("fromPositionId").GetString()!, command.GetProperty("dispositionReceiptId").GetString()!), ReadStepActor(input));
    }
    public static byte[] SerializeSelectionControl(CampaignCombatInheritedSelection.State state)
    {
        var bytes = Bytes(writer =>
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WritePropertyName("boundary"); writer.WriteRawValue(SerializeBoundary(state.Boundary));
            writer.WriteString("boundaryHash", state.BoundaryHash); writer.WriteString("segmentId", state.SegmentId);
            writer.WriteNumber("stateVersion", state.StateVersion); writer.WriteString("prefix", state.Prefix); writer.WriteNumber("stepIndex", 0);
            writer.WriteString("selectionOutcome", state.SelectionOutcome); writer.WriteStartArray("candidateIds"); writer.WriteEndArray();
            writer.WriteString("openingReceiptId", state.OpeningReceiptId); writer.WriteString("selectionReceiptId", state.SelectionReceiptId);
            WriteStepReceipts(writer, state.Receipts); writer.WriteBoolean("selectionClosed", state.SelectionClosed); writer.WriteBoolean("segmentClosed", false); writer.WriteEndObject();
        });
        ValidateSelectionControl(bytes); return bytes;
    }
    public static byte[] SerializeTraversalControl(CampaignCombatInheritedNoAttack.State state)
    {
        var bytes = Bytes(writer =>
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 1); writer.WritePropertyName("selection"); writer.WriteRawValue(SerializeSelectionControl(state.Selection));
            writer.WriteString("selectionHash", state.SelectionHash); writer.WriteNumber("stateVersion", state.StateVersion); writer.WriteString("prefix", state.Prefix);
            CampaignV11CanonicalCodec.WritePosition(writer, "position", state.Position); writer.WriteNumber("stepIndex", state.StepIndex);
            writer.WriteStartArray("stepReceipts"); foreach (var receipt in state.Receipts) writer.WriteStringValue(receipt.ReceiptId); writer.WriteEndArray();
            WriteStepReceipts(writer, state.Receipts); writer.WriteBoolean("closed", state.Closed); writer.WriteEndObject();
        });
        ValidateTraversalControl(bytes); return bytes;
    }
    internal static void ValidateSelectionControl(ReadOnlySpan<byte> bytes) => ValidateStepSyntax(bytes, "InheritedSelectionControl");
    internal static void ValidateTraversalControl(ReadOnlySpan<byte> bytes) => ValidateStepSyntax(bytes, "InheritedTraversalControl");
    internal static byte[] SerializeSelectionEvent(CampaignCombatInheritedSelection.State state, CampaignCombatInheritedSelection.Input input, string? receipt)
    {
        var opening = input.Command.Kind == "open-segment";
        return StepEvent(state.Boundary, state.SegmentId, state.StateVersion, state.Prefix, SerializeSelectionInput(input),
            opening ? "combat-segment-opened" : "combat-selection-closed", writer =>
            {
                writer.WriteStartObject(); writer.WriteString("kind", opening ? "segment-opened" : "selection-closed");
                if (opening)
                {
                    writer.WriteString("boundaryHash", state.BoundaryHash);
                    using var doc = JsonDocument.Parse(SerializeBoundary(state.Boundary));
                    writer.WriteString("assessmentHash", CampaignOpeningPreambleCodec.Hash(System.Text.Encoding.UTF8.GetBytes(doc.RootElement.GetProperty("assessment").GetRawText())));
                    writer.WriteNumber("candidateCount", 0); writer.WriteNull("decisionId");
                }
                else
                {
                    writer.WriteString("outcome", "no-selection"); writer.WriteNull("candidate"); writer.WriteString("openingReceiptId", state.OpeningReceiptId);
                }
                writer.WriteEndObject();
            }, receipt, "InheritedSelectionEvent");
    }
    internal static byte[] SerializeTraversalEvent(CampaignCombatInheritedNoAttack.State state, CampaignCombatInheritedNoAttack.Input input, string? receipt) =>
        StepEvent(state.Selection.Boundary, state.Selection.SegmentId, state.StateVersion, state.Prefix, SerializeTraversalInput(input), "combat-step-completed", writer =>
        {
            writer.WriteStartObject(); writer.WriteString("kind", "step-completed"); writer.WriteString("fromPositionId", state.Position.PositionId);
            writer.WriteString("toPositionId", CampaignCombatInheritedNoAttack.Route(state.Selection)[state.StepIndex + 1].PositionId);
            writer.WriteString("previousStepReceiptId", state.Receipts.Count == 0 ? state.Selection.OpeningReceiptId : state.Receipts[^1].ReceiptId);
            writer.WriteString("dispositionReceiptId", state.Selection.SelectionReceiptId); writer.WriteString("proofKind", "no-attack"); writer.WriteEndObject();
        }, receipt, "InheritedTraversalEvent");
    private static byte[] StepEvent(CampaignCombatAdmissionBoundary boundary, string segment, long version, string prefix, byte[] input,
        string eventType, Action<Utf8JsonWriter> effect, string? receipt, string kind)
    {
        var cycle = boundary.Entry.Lifecycle.Movement.Opening.Cycle!;
        var bytes = Bytes(writer =>
        {
            writer.WriteStartObject(); writer.WriteNumber("contractVersion", 2); writer.WriteString("eventType", eventType);
            writer.WriteString("campaignId", cycle.CampaignId); writer.WriteString("rulesetHash", cycle.RulesetHash);
            writer.WriteString("configurationHash", cycle.AdmittedPolicyBundleDigest); writer.WriteString("cycleId", boundary.Entry.Lifecycle.Movement.Opening.CycleId);
            writer.WriteString("segmentId", segment); writer.WriteNumber("priorVersion", version); writer.WriteNumber("stateVersion", checked(version + 1));
            writer.WriteString("priorPrefix", prefix); writer.WritePropertyName("input"); writer.WriteRawValue(input);
            writer.WritePropertyName("effect"); effect(writer); if (receipt is not null) writer.WriteString("receiptId", receipt); writer.WriteEndObject();
        });
        if (receipt is not null) ValidateStepSyntax(bytes, kind);
        return bytes;
    }
    private static void WriteStepReceipts(Utf8JsonWriter writer, IReadOnlyList<CampaignOpeningPreambleReceipt> receipts)
    {
        writer.WriteStartArray("receipts"); foreach (var receipt in receipts)
        {
            writer.WriteStartObject(); writer.WriteString("commandHash", receipt.CommandHash); writer.WriteString("eventHash", receipt.EventHash);
            writer.WriteString("receiptId", receipt.ReceiptId); writer.WriteString("actor", StepActor(receipt.Actor)); writer.WriteNumber("stateVersion", receipt.StateVersion); writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }
    private static string StepActor(CampaignOpeningPreambleActor actor) => actor switch
    {
        CampaignOpeningPreambleActor.System => "system",
        CampaignOpeningPreambleActor.Axis => "axis",
        CampaignOpeningPreambleActor.Commonwealth => "commonwealth",
        _ => throw new JsonException("Unsupported Combat actor."),
    };
    private static CampaignOpeningPreambleActor ReadStepActor(JsonElement input) => input.GetProperty("actor").GetString() switch
    {
        "system" => CampaignOpeningPreambleActor.System,
        "axis" => CampaignOpeningPreambleActor.Axis,
        "commonwealth" => CampaignOpeningPreambleActor.Commonwealth,
        _ => throw new JsonException("Unsupported Combat actor."),
    };
    private static readonly Dictionary<string, string> InheritedStepShapes = new(StringComparer.Ordinal)
    {
        ["InheritedSelectionCommand"] = "contractVersion:int kind:id segmentId:id expectedPriorVersion:long expectedPositionId:id openingReceiptId:id?",
        ["InheritedSelectionInput"] = "command:InheritedSelectionCommand actor:actor",
        ["InheritedSelectionEvent"] = "contractVersion:int eventType:id campaignId:id rulesetHash:rawHash configurationHash:hash cycleId:hash segmentId:id priorVersion:long stateVersion:long priorPrefix:hash input:InheritedSelectionInput effect:InheritedSelectionEffect receiptId:id",
        ["InheritedSelectionOpen"] = "kind:id boundaryHash:hash assessmentHash:hash candidateCount:int decisionId:null",
        ["InheritedSelectionClose"] = "kind:id outcome:id candidate:null openingReceiptId:id",
        ["InheritedSelectionControl"] = "contractVersion:int boundary:AdmissionBoundary boundaryHash:hash segmentId:id stateVersion:long prefix:hash stepIndex:int selectionOutcome:id candidateIds:id[] openingReceiptId:id? selectionReceiptId:id? receipts:PreambleReceipt[] selectionClosed:bool segmentClosed:bool",
        ["InheritedTraversalCommand"] = "contractVersion:int kind:id segmentId:id expectedPriorVersion:long fromPositionId:id dispositionReceiptId:id",
        ["InheritedTraversalInput"] = "command:InheritedTraversalCommand actor:actor",
        ["InheritedTraversalEvent"] = "contractVersion:int eventType:id campaignId:id rulesetHash:rawHash configurationHash:hash cycleId:hash segmentId:id priorVersion:long stateVersion:long priorPrefix:hash input:InheritedTraversalInput effect:InheritedTraversalEffect receiptId:id",
        ["InheritedTraversalEffect"] = "kind:id fromPositionId:id toPositionId:id previousStepReceiptId:id dispositionReceiptId:id proofKind:id",
        ["InheritedTraversalControl"] = "contractVersion:int selection:InheritedSelectionControl selectionHash:hash stateVersion:long prefix:hash position:Position stepIndex:int stepReceipts:id[] receipts:PreambleReceipt[] closed:bool",
    };
    private static void ValidateStepSyntax(ReadOnlySpan<byte> bytes, string kind)
    {
        ValidateBounds(bytes);
        using var document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 33 });
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
            WriteBoundarySyntax(writer, document.RootElement, kind);
        if (!bytes.SequenceEqual(stream.ToArray())) throw new JsonException("Noncanonical inherited Combat bytes.");
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
