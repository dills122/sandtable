using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Rules;
using static Cna.Core.Campaigns.CampaignCombatSettledContinuationCodec;

namespace Cna.Core.Campaigns;

/// <summary>Owned immutable canonical evidence. No gameplay permission or mutable projection is retained.</summary>
internal sealed class CombatSettledContinuationProof
{
    private readonly byte[] bytes;
    internal CombatSettledContinuationProof(ReadOnlySpan<byte> bytes) => this.bytes = bytes.ToArray();
    public byte[] CanonicalBytes => bytes.ToArray();
}

/// <summary>Native017B replay followed by closed catalogue admission and pure019A assessment.</summary>
internal static class CampaignCombatSettledContinuation
{
    private const string Trust = "synthetic-pre-combat";
    public static CombatSettledContinuationProof Bridge(ReadOnlySpan<byte> packetBytes, CampaignCombatCreationContext context)
    {
        // Parse owns its tree. No caller span/object or output/cache alias survives this entry point.
        var packet = Parse(packetBytes, "SettledPacket"); var s = packet.GetProperty("source");
        Require(packet.GetProperty("contractVersion").GetInt32() == 1 && s.GetProperty("contractVersion").GetInt32() == 1 &&
            Text(s, "trustLabel") == Trust && Text(s.GetProperty("movementEnd"), "trustLabel") == Trust, 3);
        Require(s.GetProperty("roundEventCanonicalUtf8").GetArrayLength() <= 16 && s.GetProperty("resultEventCanonicalUtf8").GetArrayLength() <= 16, 1);
        CombatResultReleaseSource source; CombatResultReleaseProjection initial;
        try
        {
            source = DecodeSource(s, context);
            // Full semantic replay precedes catalogue admission/extraction.017B binds the exact
            // committed Base2/predecessor/seals and selected owner-choice sequence, not just a digest.
            initial = CampaignCombatResultRelease.Replay(source, [], []);
            CompareUpstream(s, source, initial);
            AdmitResultInputs(source, initial.Result.Context.Committed.StateVersion);
        }
        catch (Exception e) when (SourceFailure(e)) { throw new JsonException("CMB-SCT-004", e); }
        var inputsJson = packet.GetProperty("releaseInputs"); var events = Strings(packet.GetProperty("releaseEventCanonicalUtf8"));
        Require(inputsJson.GetArrayLength() == 2 && events.Length == 2, 5);
        CombatResultReleaseProjection terminal;
        try
        {
            var inputs = inputsJson.EnumerateArray().Select(ReleaseInput).ToArray();
            Require(inputs[0].Command.Kind == "open" && inputs[1].Command.Kind == "complete", 5);
            Compare(Utf8(packet, "releaseBaseCanonicalUtf8"), CampaignCombatReserveReleaseCodec.SerializeBase(initial.Basis, source.Request), 4);
            terminal = CampaignCombatResultRelease.Replay(source, inputs, events);
            Require(terminal.Release.Status == "completed" && terminal.Release.CompletionReceiptId is not null && terminal.Release.Pending.Count == 0 &&
                terminal.Release.AcceptedHighWater is null && terminal.Release.Timing is null && terminal.Release.DecisionId is null, 5);
        }
        catch (Exception e) when (SourceFailure(e) && !e.Message.StartsWith("CMB-SCT-004", StringComparison.Ordinal))
        { throw new JsonException("CMB-SCT-005", e); }
        var descriptor = s.GetProperty("movementEnd"); CombatContinuationMovementEnd movement;
        try { movement = AdmitDescriptor(descriptor, source); }
        catch (Exception e) when (SourceFailure(e)) { throw new JsonException("CMB-SCT-004", e); }
        CombatContinuationAssessment assessment;
        try
        {
            assessment = CampaignCombatContinuation.AssessTrustedBoundary(context.Setup.Artifact.Definition, terminal.Basis.Cycle,
                terminal.Result.World, terminal.Release, movement, WeatherKind.Normal);
        }
        catch (Exception e) when (SourceFailure(e)) { throw new JsonException("CMB-SCT-003", e); }
        return new(Proof(s, source, terminal, descriptor, assessment));
    }
    private static bool SourceFailure(Exception e) => e is JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException or
        IndexOutOfRangeException or OverflowException or FormatException;
    private static byte[] Utf8(JsonElement node, string key) => Encoding.UTF8.GetBytes(Text(node, key));
    private static byte[][] Strings(JsonElement values) => values.EnumerateArray().Select(e => Encoding.UTF8.GetBytes(e.GetString()!)).ToArray();
    private static void Compare(byte[] actual, byte[] expected, int code = 4) => Require(actual.AsSpan().SequenceEqual(expected), code);
    private static CombatResultReleaseSource DecodeSource(JsonElement s, CampaignCombatCreationContext context)
    {
        var request = CampaignCombatCreationRequestCodec.Deserialize(Utf8(s, "requestCanonicalUtf8"), context);
        var created = Utf8(s, "createdCanonicalUtf8"); var initial = CampaignCreatedV11Serializer.Deserialize(created, request).InitialWorld;
        using var predecessor = JsonDocument.Parse(Utf8(s, "predecessorCanonicalUtf8"), new JsonDocumentOptions { MaxDepth = 32 });
        var p = predecessor.RootElement; var boundary = Boundary(p.GetProperty("boundary"), initial);
        var predInputs = p.GetProperty("inputs").EnumerateArray().Select(i => CampaignCombatSelectionStepsCodec.ReadInput(Bytes(i))).ToArray();
        var predEvents = p.GetProperty("events").EnumerateArray().Select(e => Utf8(e, "canonicalUtf8")).ToArray();
        Require(predInputs.Length <= 16 && predEvents.Length <= 16, 1);
        using var rounds = JsonDocument.Parse(Utf8(s, "roundInputsCanonicalUtf8"), new JsonDocumentOptions { MaxDepth = 32 });
        using var results = JsonDocument.Parse(Utf8(s, "resultInputsCanonicalUtf8"), new JsonDocumentOptions { MaxDepth = 32 });
        Require(rounds.RootElement.GetArrayLength() <= 16 && results.RootElement.GetArrayLength() <= 16, 1);
        return new(Text(s, "sourceId"), request, created, boundary, predInputs, predEvents,
            rounds.RootElement.EnumerateArray().Select(i => CampaignCombatSealedRoundCodec.ReadInput(Bytes(i))).ToArray(), Strings(s.GetProperty("roundEventCanonicalUtf8")),
            results.RootElement.EnumerateArray().Select(i => CampaignCombatResolutionCodec.ReadInput(Bytes(i))).ToArray(), Strings(s.GetProperty("resultEventCanonicalUtf8")));
    }
    private static CombatStepsBoundary Boundary(JsonElement b, CampaignWorldSnapshotV7 initial)
    {
        var c = b.GetProperty("cycle"); var r = b.GetProperty("randomState"); var weather = b.GetProperty("weather");
        var cp = b.GetProperty("world").GetProperty("elements").EnumerateArray().ToDictionary(e => Text(e, "elementId"),
            e => e.GetProperty("operationalState").GetProperty("capabilityPointsExpended").GetProperty("numerator").GetInt64(), StringComparer.Ordinal);
        //009B's supported synthetic boundary changes only integral current CP. Full canonical
        //boundary comparison below rejects any ignored field, location, relationship or ledger change.
        var world = new CampaignWorldSnapshotV7(7, initial.CreationBinding, initial.Elements.Select(e =>
            new CampaignElementStateV6(e.ElementId, e.CurrentLocationId, e.ReserveStatus,
                new(e.OperationalState.LedgerGameTurn, e.OperationalState.LedgerOperationStage, new(cp[e.ElementId], 1),
                    e.OperationalState.CohesionLevel, e.OperationalState.VehicleBreakdownState, e.OperationalState.MovementEnded, e.OperationalState.InitialLedgerOrigin),
                e.Components, e.SourceParentFormationId, e.CurrentParentFormationId, e.Ammunition, e.Readiness)),
            initial.Representations, initial.BrokenVehicleLots, initial.CohesionCauses, initial.Relationships, initial.CustodyLots, initial.Guards,
            initial.ReplacementEntitlements, initial.FutureObligations, initial.Settlements);
        return new(b.GetProperty("contractVersion").GetInt32(), Text(b, "creationBinding"),
            new(c.GetProperty("contractVersion").GetInt32(), Text(c, "campaignId"), Text(c, "rulesetHash"), Text(c, "setupId"), Text(c, "setupHash"),
                Text(c, "contentPackId"), Text(c, "contentHash"), Text(c, "scenarioId"), c.GetProperty("gameTurn").GetInt32(), c.GetProperty("operationStage").GetInt32(),
                Text(c, "playerPhaseSlot"), Side(Text(c, "actingSide")), c.GetProperty("ordinal").GetInt32(), c.GetProperty("openedAuthorityVersion").GetInt64(),
                Text(c, "openingPrefix"), Text(c, "admittedPolicyBundleDigest")), Side(Text(b, "firstActingSide")), b.GetProperty("priorVersion").GetInt64(),
            Text(b, "priorPrefix"), Text(b, "completedBreakdownReceipt"), CampaignV11CanonicalCodec.ParsePosition(b.GetProperty("position")), world,
            new(r.GetProperty("contractVersion").GetInt32(), Text(r, "algorithmId"), r.GetProperty("seed").GetUInt64(), r.GetProperty("nextByteCursor").GetUInt64()),
            new(weather.GetProperty("gameTurn").GetInt32(), weather.GetProperty("operationStage").GetInt32(),
                Weather(Text(weather, "attackerKind")), Weather(Text(weather, "defenderKind")), Text(weather, "weatherReceiptHash")));
    }
    private static LandSide Side(string text) => text switch { "axis" => LandSide.Axis, "commonwealth" => LandSide.Commonwealth, _ => throw new JsonException() };
    private static WeatherKind Weather(string text) => text switch
    { "normal" => WeatherKind.Normal, "hot" => WeatherKind.Hot, "sandstorm" => WeatherKind.Sandstorm, "rainstorm" => WeatherKind.Rainstorm, _ => throw new JsonException() };
    private static JsonNode Node(byte[] bytes) => JsonNode.Parse(bytes)!;
    private static JsonArray Array(IEnumerable<byte[]> bytes) => new(bytes.Select(Node).ToArray());
    private static void CompareUpstream(JsonElement s, CombatResultReleaseSource source, CombatResultReleaseProjection p)
    {
        var committed = p.Result.Context.Committed;
        Compare(Utf8(s, "baseCanonicalUtf8"), CampaignCombatSealedRoundCodec.SerializeBase(committed.Base));
        Compare(Utf8(s, "committedCanonicalUtf8"), CampaignCombatSealedRoundCodec.SerializeState(committed));
        Compare(Utf8(s, "resultStateCanonicalUtf8"), CampaignCombatResolutionCodec.SerializeState(p.Result));
        var predecessor = new JsonObject
        {
            ["boundary"] = Node(CampaignCombatSelectionStepsCodec.SerializeBoundary(source.Request, source.Created, source.Boundary)),
            ["inputs"] = Array(source.PredecessorInputs.Select(CampaignCombatSelectionStepsCodec.SerializeInput)),
            ["events"] = new JsonArray(source.PredecessorEvents.Select(e => (JsonNode)new JsonObject { ["canonicalUtf8"] = Encoding.UTF8.GetString(e) }).ToArray())
        };
        Compare(Utf8(s, "predecessorCanonicalUtf8"), Encode(predecessor));
        Compare(Utf8(s, "roundInputsCanonicalUtf8"), Encode(Array(source.RoundInputs.Select(CampaignCombatSealedRoundCodec.SerializeInput))));
        Compare(Utf8(s, "resultInputsCanonicalUtf8"), Encode(Array(source.ResultInputs.Select(CampaignCombatResolutionCodec.SerializeInput))));
    }
    private static void AdmitResultInputs(CombatResultReleaseSource source, long committedVersion)
    {
        // Independently frozen Result2 catalogue actor/choice sequence is checked by017B after
        //native replay. These literal admitted times and explicit versions close its intentionally
        //retimeable adapter seam. No terminal hash or new settled fixture supplies admission.
        var row = source.CaseId[..source.CaseId.LastIndexOf('.', source.CaseId.LastIndexOf('.') - 1)];
        long?[] times = row switch
        {
            "ordinary" or "zero-engaged" => [null, null, null, null, null, null, null],
            "zero-retreat" or "refusal-loss-dp" => [null, 10000, 10001, null, null, null, null, null],
            "defender-capture-guard" or "defender-capture-escape" => [null, null, null, null, 10000, 10001, null, null, null],
            "attacker-capture-guard-cp-limit" or "attacker-capture-escape" => [null, 10000, 10001, null, null, 11001, 11002, null, null, null],
            _ => throw new JsonException("Unsupported catalogue row.")
        };
        Require(source.ResultInputs.Count == times.Length, 4);
        for (var index = 0; index < times.Length; index++)
        {
            var input = source.ResultInputs[index];
            Require(input.ClockAvailable && input.AdmittedAt == times[index] && input.Command.ExpectedPriorVersion == (input.Command.Kind == "choose" ? null : checked(committedVersion + index)), 4);
        }
    }
    private static CombatReleaseInput ReleaseInput(JsonElement i)
    {
        var c = i.GetProperty("command");
        Require(Text(i, "actor") == "system" && i.GetProperty("admittedAt").ValueKind == JsonValueKind.Null && i.GetProperty("clockAvailable").GetBoolean() &&
            c.GetProperty("decisionId").ValueKind == JsonValueKind.Null && c.GetProperty("unit").ValueKind == JsonValueKind.Null && c.GetProperty("choice").ValueKind == JsonValueKind.Null, 5);
        return new(new(c.GetProperty("contractVersion").GetInt32(), Text(c, "kind"), Text(c, "releaseId"),
            c.GetProperty("expectedPriorVersion").ValueKind == JsonValueKind.Null ? null : c.GetProperty("expectedPriorVersion").GetInt64()), CampaignOpeningPreambleActor.System);
    }
    private static CombatContinuationMovementEnd AdmitDescriptor(JsonElement descriptor, CombatResultReleaseSource source)
    {
        Require(Text(descriptor, "receiptSource") == "independently-pinned-synthetic-descriptor", 4);
        var kind = Text(descriptor, "descriptorId"); Require(kind is "boundary-locations" or "distant-original" or "prior-exclusion", 3);
        var b = source.Boundary; var c = b.Cycle; var pack = source.Request.Context.Setup.Artifact.Definition;
        var locations = b.World.Elements.Select(e => new CombatMovementEndLocation(new(b.World.CreationBinding,
            pack.Elements.Single(f => f.ElementId == e.ElementId).SideId, e.ElementId), e.CurrentLocationId))
            .OrderBy(l => $"{l.Unit.CreationBinding}/{l.Unit.OriginalSide}/{l.Unit.ElementId}", StringComparer.Ordinal).ToArray();
        if (kind == "distant-original") locations = locations.Select(l => l with
        { LocationId = source.Request.Context.Setup.Scenario.RetreatSupplyAnchors.Single(a => a.SideId == l.Unit.OriginalSide).LocationId }).ToArray();
        var exclusions = kind == "prior-exclusion" ? locations.Where(l => l.Unit.OriginalSide == CampaignSnapshotSerializer.FormatSide(c.ActingSide)).Select(l => l.Unit).ToArray() : [];
        var scope = new CombatReleaseScope(c.GameTurn, c.OperationStage, c.PlayerPhaseSlot, c.ActingSide);
        var proof = new JsonObject
        {
            ["scope"] = Scope(scope),
            ["ordinal"] = c.Ordinal,
            ["completionReceiptId"] = "synthetic.movement-completed",
            ["endLocations"] = new JsonArray(locations.Select(l => (JsonNode)new JsonObject { ["unit"] = Unit(l.Unit), ["locationId"] = l.LocationId }).ToArray()),
            ["excludedBefore"] = new JsonArray(exclusions.Select(Unit).ToArray())
        };
        Compare(Bytes(descriptor.GetProperty("proof")), Encode(proof));
        return new(scope, c.Ordinal, "synthetic.movement-completed", locations, exclusions);
    }
    private static JsonObject Unit(CampaignCombatUnitKey u) => new() { ["creationBinding"] = u.CreationBinding, ["originalSide"] = u.OriginalSide, ["elementId"] = u.ElementId };
    private static JsonObject Scope(CombatReleaseScope s) => new()
    { ["gameTurn"] = s.GameTurn, ["operationStage"] = s.OperationStage, ["playerPhaseSlot"] = s.PlayerPhaseSlot, ["actingSide"] = CampaignSnapshotSerializer.FormatSide(s.ActingSide) };
    private static byte[] Proof(JsonElement s, CombatResultReleaseSource source, CombatResultReleaseProjection p,
        JsonElement descriptor, CombatContinuationAssessment assessment)
    {
        var committed = p.Result.Context.Committed; var cycle = p.Basis.Cycle; var selected = committed.Base.Steps.Selection!.Attacker.Unit;
        var matches = source.RoundEvents.Where(e => { using var d = JsonDocument.Parse(e); return Text(d.RootElement, "eventType") == "combat-attack-committed"; }).ToArray();
        Require(matches.Length == 1, 4); using var eventDoc = JsonDocument.Parse(matches[0]); var evt = eventDoc.RootElement;
        var receipt = Text(evt, "receiptId"); var eventHash = CampaignOpeningPreambleCodec.Hash(matches[0]);
        Require(Text(evt.GetProperty("effect"), "kind") == "attack-committed" && Text(evt.GetProperty("effect"), "commitmentId") == committed.CommitmentId &&
            receipt != committed.CommitmentId && selected.OriginalSide == CampaignSnapshotSerializer.FormatSide(cycle.ActingSide) &&
            committed.AttackHistory.Count(h => h.CommitmentId == committed.CommitmentId && h.Attacker == selected && h.GameTurn == cycle.GameTurn && h.OperationStage == cycle.OperationStage) == 1 &&
            committed.Receipts.Count(r => r.ReceiptId == receipt && r.EventHash == eventHash) == 1, 4);
        var result = Node(CampaignCombatResolutionCodec.SerializeState(p.Result));
        var stateBytes = CampaignCombatReserveReleaseCodec.SerializeState(p.Release); var release = Node(stateBytes);
        var baseBytes = CampaignCombatReserveReleaseCodec.SerializeBase(p.Basis, source.Request); var basis = Node(baseBytes);
        var round = Node(CampaignCombatSealedRoundCodec.SerializeState(committed));
        Require(p.Release.RandomState == p.Result.RandomState && p.Release.Members.SequenceEqual(p.Basis.Members) &&
            p.Release.AttackHistory.SequenceEqual(committed.AttackHistory) && p.Release.RetainedWorldHash == p.Basis.RetainedWorldHash, 4);
        var identity = new JsonObject { ["sourceId"] = source.CaseId, ["sourceHash"] = CampaignOpeningPreambleCodec.Hash(Bytes(s)) };
        foreach (var (name, field) in new[] { ("requestHash", "requestCanonicalUtf8"), ("createdHash", "createdCanonicalUtf8"),
            ("selectionHash", "predecessorCanonicalUtf8"), ("baseHash", "baseCanonicalUtf8"), ("committedHash", "committedCanonicalUtf8"), ("resultHash", "resultStateCanonicalUtf8") })
            identity[name] = CampaignOpeningPreambleCodec.Hash(Utf8(s, field));
        identity["boundaryHash"] = CampaignOpeningPreambleCodec.Hash(CampaignCombatSelectionStepsCodec.SerializeBoundary(source.Request, source.Created, source.Boundary));
        identity["releaseBaseHash"] = CampaignOpeningPreambleCodec.Hash(baseBytes); identity["releaseStateHash"] = CampaignOpeningPreambleCodec.Hash(stateBytes);
        identity["movementDescriptorHash"] = CampaignOpeningPreambleCodec.Hash(Bytes(descriptor));
        identity["roundClockConfigurationHash"] = committed.Base.ConfigurationHash; identity["resultClockPolicyId"] = CampaignCombatResolution.Policy;
        var proof = new JsonObject
        {
            ["contractVersion"] = 1,
            ["profile"] = "result2-settled-continuation",
            ["trustLabel"] = Trust,
            ["sourceIdentity"] = identity,
            ["cycle"] = basis["cycle"]!.DeepClone(),
            ["stateVersion"] = p.Release.StateVersion,
            ["prefix"] = p.Release.Prefix,
            ["positionId"] = p.Basis.PositionId,
            ["releaseCompletionReceiptId"] = p.Release.CompletionReceiptId,
            ["movementEnd"] = JsonNode.Parse(Bytes(descriptor)),
            ["world"] = result["world"]!.DeepClone(),
            ["randomState"] = result["randomState"]!.DeepClone(),
            ["attackHistory"] = release["attackHistory"]!.DeepClone(),
            ["targetUses"] = round["targetUses"]!.DeepClone(),
            ["members"] = release["members"]!.DeepClone(),
            ["progress"] = new JsonArray(new JsonObject { ["eventType"] = "combat-attack-committed", ["receiptId"] = receipt, ["eventHash"] = eventHash }),
            ["witnessOutcome"] = "supported",
            ["unsupportedReason"] = null,
            ["witnesses"] = new JsonArray(assessment.Witnesses.Select(w => (JsonNode)new JsonObject
            {
                ["kind"] = "movement",
                ["unit"] = Unit(w.Unit),
                ["destinationLocationId"] = w.DestinationLocationId,
                ["terrainCost"] = w.TerrainCost,
                ["breakOffCost"] = w.BreakOffCost,
                ["afterCp"] = w.AfterCp,
                ["excessCpDp"] = w.ExcessCpDp,
                ["usesReleaseException"] = w.UsesReleaseException
            }).ToArray())
        };
        using var doc = JsonDocument.Parse(Encode(proof)); return Canonical(doc.RootElement, "SettledContinuationProof");
    }
}
