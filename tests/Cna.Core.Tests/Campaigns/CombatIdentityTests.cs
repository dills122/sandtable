using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatIdentityTests
{
    [Fact]
    public void SyntheticInitialFactsCertifyEveryResultForBothRolesWithoutClaimingHistoryAdmission()
    {
        var history = Supported();
        var inherited = CampaignCombatCertification.Admit(history.Request, history.Created, history.Events);
        var initial = CampaignCreatedV11Serializer.Deserialize(history.Created, history.Request).InitialWorld;
        foreach (var side in new[] { LandSide.Axis, LandSide.Commonwealth })
        {
            var cycle = inherited.Entry.Lifecycle.Movement.Opening.Cycle! with { ActingSide = side };
            var candidate = CampaignCombatCertification.CertifyInitialProfileFacts(history.Request, history.Created, initial,
                cycle, side, WeatherKind.Normal, WeatherKind.Normal);
            Assert.NotNull(candidate);
            Assert.Equal(side == LandSide.Axis ? "axis" : "commonwealth", candidate.Attacker.Unit.OriginalSide);
            Assert.Equal(candidate.Defender.LocationId, candidate.TargetLocationId);
            Assert.Equal("voluntary-adjacent", candidate.Basis);
            var bytes = CampaignCombatIdentityCodec.SerializeCandidate(candidate);
            Assert.Equal(bytes, CampaignCombatIdentityCodec.SerializeCandidate(CampaignCombatIdentityCodec.ReadCandidate(bytes, candidate)));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatIdentityCodec.ReadCandidate([.. bytes, (byte)' '], null!));
            Assert.Throws<ArgumentNullException>(() => CampaignCombatIdentityCodec.ReadCandidate(bytes, null!));
            Assert.Null(CampaignCombatCertification.CertifyInitialProfileFacts(history.Request, history.Created, initial,
                cycle, side, WeatherKind.Hot, WeatherKind.Normal));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatCertification.CertifyInitialProfileFacts(history.Request, history.Created,
                inherited.Entry.Lifecycle.Movement.World, cycle, side, WeatherKind.Normal, WeatherKind.Normal));
        }
        Assert.Equal((1296, 6480, 8840), CampaignCombatCertification.SupportCoverage);
        Assert.Empty(inherited.Assessment.CandidateIds);
        Assert.Equal(initial, CampaignCreatedV11Serializer.Deserialize(history.Created, history.Request).InitialWorld);
    }

    [Fact]
    public void PositiveFactsKeepCurrentCpAndRejectForeignOrUnsupportedInventoryAndScope()
    {
        var history = Supported(); var initial = CampaignCreatedV11Serializer.Deserialize(history.Created, history.Request).InitialWorld;
        var actual = CampaignCombatCertification.Admit(history.Request, history.Created, history.Events);
        foreach (var side in new[] { LandSide.Axis, LandSide.Commonwealth })
        {
            var cycle = actual.Entry.Lifecycle.Movement.Opening.Cycle! with { ActingSide = side };
            var sideName = side == LandSide.Axis ? "axis" : "commonwealth";
            foreach (var (a, d, eligible) in new[] { (0, 0, true), (5, 7, true), (6, 7, false), (5, 8, false), (10, 10, false) })
            {
                var current = CopyWorld(initial, initial.Elements.Select(e => ProfileElement(e, new(e.ElementId.StartsWith(sideName, StringComparison.Ordinal) ? a : d, 1))));
                var snapshot = current.Elements.Select(e => e.OperationalState.CapabilityPointsExpended).ToArray();
                Assert.Equal(eligible, Certify(current) is not null);
                Assert.Equal(snapshot, current.Elements.Select(e => e.OperationalState.CapabilityPointsExpended));
                foreach (var weather in new[] { WeatherKind.Hot, WeatherKind.Sandstorm, WeatherKind.Rainstorm })
                {
                    Assert.Null(CampaignCombatCertification.CertifyInitialProfileFacts(history.Request, history.Created, current, cycle, side, weather, WeatherKind.Normal));
                    Assert.Null(CampaignCombatCertification.CertifyInitialProfileFacts(history.Request, history.Created, current, cycle, side, WeatherKind.Normal, weather));
                }
            }
            foreach (var cp in new[] { new CapabilityPointAmount(1, 2), new CapabilityPointAmount(11, 1), new CapabilityPointAmount(long.MaxValue, 1) })
                Reject(CopyWorld(initial, initial.Elements.Select(e => ProfileElement(e, cp))));
            foreach (var element in initial.Elements)
            {
                foreach (var changed in new[] { ProfileElement(element, cohesion: -1), ProfileElement(element, ammo: 9),
                    ProfileElement(element, toe: 9), ProfileElement(element, location: "axis-rear"), ProfileElement(element, parent: "foreign.parent"),
                    ProfileElement(element, pinned: true), ProfileElement(element, stage: 2), ProfileElement(element, reserve: CampaignElementReserveStatus.ReserveI) })
                {
                    var world = CopyWorld(initial, initial.Elements.Select(e => e.ElementId == element.ElementId ? changed : e),
                        initial.Representations.Select(r => r.BoundElementIds.Contains(element.ElementId) ? new CampaignMapRepresentationState(r.RepresentationId, changed.CurrentLocationId, r.BindingKind, r.BoundElementIds) : r));
                    Reject(world);
                }
            }
            Reject(CopyWorld(initial, creationBinding: "foreign.creation"));
            foreach (var count in new[] { 1, 512, 4096 })
            {
                var causes = Enumerable.Range(1, count).Select(i => new CampaignCombatCohesionCause($"cause.{i}", i, $"receipt.{i}",
                    initial.Elements[0].ElementId, 1, 1, "loss-dp", 1, 0, -1));
                Reject(new CampaignWorldSnapshotV7(7, initial.CreationBinding, initial.Elements, initial.Representations, [], causes, [], [], [], [], [], []));
            }
            foreach (var changed in new[] { cycle with { CampaignId = "foreign.campaign" }, cycle with { SetupHash = "sha256:" + new string('a', 64) },
                cycle with { ContentHash = "sha256:" + new string('a', 64) }, cycle with { GameTurn = 2 }, cycle with { Ordinal = 2 },
                cycle with { PlayerPhaseSlot = "second-acting-side" }, cycle with { OpeningPrefix = "bad" }, cycle with { OpenedAuthorityVersion = 0 } })
                Assert.ThrowsAny<JsonException>(() => CampaignCombatCertification.CertifyInitialProfileFacts(history.Request, history.Created, initial, changed, side, WeatherKind.Normal, WeatherKind.Normal));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatCertification.CertifyInitialProfileFacts(history.Request, [], initial, cycle, side, WeatherKind.Normal, WeatherKind.Normal));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatCertification.CertifyInitialProfileFacts(history.Request, history.Created, initial, cycle, side, (WeatherKind)99, WeatherKind.Normal));
            CampaignCombatCandidate? Certify(CampaignWorldSnapshotV7 world) => CampaignCombatCertification.CertifyInitialProfileFacts(history.Request, history.Created, world, cycle, side, WeatherKind.Normal, WeatherKind.Normal);
            void Reject(CampaignWorldSnapshotV7 world) => Assert.ThrowsAny<JsonException>(() => Certify(world));
        }
        // Independent frozen source-oracle reduction: 637 complete result tuples, including
        // 141 accepted retreats,141 refusals,45 attacker captures and125 defender captures.
        // Each successful CP5/7 certification above executes every tuple, including defender
        // paidCP10→mandatoryCP11. Existing CombatWorldTests proves that helper's exact DP.
        Assert.Equal(637, CampaignCombatCertification.DistinctSupportResultCount);
    }

    [Fact]
    public void CandidateGrammarAndCanonicalSpellingPrecedeTrustedExpectedAndOwnTheirBytes()
    {
        using var fixture = Fixture("combat-sealed-round-v2.json");
        using var baseValue = JsonDocument.Parse(fixture.RootElement.GetProperty("cases")[0].GetProperty("baseCanonicalUtf8").GetString()!);
        var selection = baseValue.RootElement.GetProperty("steps").GetProperty("selection");
        var expected = Candidate(selection); var bytes = Encoding.UTF8.GetBytes(selection.GetRawText());
        Assert.Equal(bytes, CampaignCombatIdentityCodec.SerializeCandidate(expected));
        var root = JsonNode.Parse(bytes)!; var text = Encoding.UTF8.GetString(bytes);
        foreach (var bad in new[] { "null", "{}", "[]", "[", text + " ", "\uFEFF" + text,
            text.Replace("\"attacker\"", "\"a\\u0074tacker\"", StringComparison.Ordinal),
            text.Replace("\"basis\":", "\"basis\":\"voluntary-adjacent\",\"basis\":", StringComparison.Ordinal) }) Reject(Encoding.UTF8.GetBytes(bad));
        foreach (var field in new[] { "attacker", "defender", "targetLocationId", "basis" })
        {
            var missing = root.DeepClone(); missing.AsObject().Remove(field); Reject(Bytes(missing));
            var wrong = root.DeepClone(); wrong[field] = false; Reject(Bytes(wrong));
            var nulled = root.DeepClone(); nulled[field] = null; Reject(Bytes(nulled));
        }
        foreach (var role in new[] { "attacker", "defender" })
        {
            var unknown = root.DeepClone(); unknown[role]!["unit"]!["unknown"] = "value"; Reject(Bytes(unknown));
            var missing = root.DeepClone(); missing[role]!.AsObject().Remove("componentIds"); Reject(Bytes(missing));
            var oversized = root.DeepClone(); oversized[role]!["locationId"] = new string('a', 129); Reject(Bytes(oversized));
            var components = root.DeepClone(); components[role]!["componentIds"] = new JsonArray(Enumerable.Range(0, 513).Select(i => (JsonNode?)JsonValue.Create($"component.{i:D3}")).ToArray()); Reject(Bytes(components));
            var duplicate = root.DeepClone(); duplicate[role]!["componentIds"]!.AsArray().Add(duplicate[role]!["componentIds"]![0]!.DeepClone()); Reject(Bytes(duplicate));
        }
        Reject(new byte[1_048_577]);
        Reject(Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)));
        var reversed = new JsonObject(); foreach (var pair in root.AsObject().Reverse()) reversed.Add(pair.Key, pair.Value?.DeepClone()); Reject(Bytes(reversed));
        Assert.Throws<ArgumentNullException>(() => CampaignCombatIdentityCodec.ReadCandidate(bytes, null!));
        var foreign = new CampaignCombatCandidate(expected.Attacker, expected.Defender, "axis-supply", expected.Basis);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatIdentityCodec.ReadCandidate(bytes, foreign));
        var returned = CampaignCombatIdentityCodec.ReadCandidate(bytes, expected); Array.Fill(bytes, (byte)0);
        Assert.Equal(selection.GetRawText(), Encoding.UTF8.GetString(CampaignCombatIdentityCodec.SerializeCandidate(returned)));
        void Reject(byte[] value) => Assert.ThrowsAny<JsonException>(() => CampaignCombatIdentityCodec.ReadCandidate(value, null!));
    }

    [Fact]
    public void CurrentRoundTwoLiteralOpportunitiesBindEveryPreimageField()
    {
        using var fixture = Fixture("combat-sealed-round-v2.json"); var checkedCases = 0;
        const string position = "land.position.operation-1.first-player.movement-and-combat.combat.force-assignment";
        foreach (var row in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            Assert.StartsWith("synthetic-C3a", row.GetProperty("provenance").GetString());
            using var baseValue = JsonDocument.Parse(row.GetProperty("baseCanonicalUtf8").GetString()!);
            using var opened = JsonDocument.Parse(row.GetProperty("eventCanonicalUtf8")[0].GetString()!);
            var effect = opened.RootElement.GetProperty("effect");
            var baseHash = effect.GetProperty("baseHash").GetString()!;
            Assert.Equal(baseHash, "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes("sandtable.combat.base-fragment.v2\0" + row.GetProperty("baseCanonicalUtf8").GetString()))));
            var cycleId = opened.RootElement.GetProperty("cycleId").GetString()!;
            var steps = baseValue.RootElement.GetProperty("steps"); var candidate = Candidate(steps.GetProperty("selection"));
            var decline = steps.GetProperty("declineReceiptId").GetString()!;
            var expected = effect.GetProperty("opportunityId").GetString()!;
            Assert.Equal(expected, CampaignCombatIdentityCodec.CalculateOpportunityId(baseHash, cycleId, position, candidate, decline));
            Assert.NotEqual(expected, CampaignCombatIdentityCodec.CalculateOpportunityId("sha256:" + new string('a', 64), cycleId, position, candidate, decline));
            Assert.NotEqual(expected, CampaignCombatIdentityCodec.CalculateOpportunityId(baseHash, "sha256:" + new string('a', 64), position, candidate, decline));
            Assert.NotEqual(expected, CampaignCombatIdentityCodec.CalculateOpportunityId(baseHash, cycleId, "other.position", candidate, decline));
            Assert.NotEqual(expected, CampaignCombatIdentityCodec.CalculateOpportunityId(baseHash, cycleId, position, candidate, "other.decline"));
            Assert.NotEqual(expected, CampaignCombatIdentityCodec.CalculateOpportunityId(baseHash, cycleId, position, new(candidate.Attacker, candidate.Defender, "other.target", candidate.Basis), decline));
            checkedCases++;
        }
        Assert.Equal(10, checkedCases);
    }

    private static CampaignCombatCandidate Candidate(JsonElement value) => new(Participant(value.GetProperty("attacker")), Participant(value.GetProperty("defender")),
        value.GetProperty("targetLocationId").GetString()!, value.GetProperty("basis").GetString()!);
    private static CampaignCombatParticipant Participant(JsonElement value)
    {
        var unit = value.GetProperty("unit");
        return new(new(unit.GetProperty("creationBinding").GetString()!, unit.GetProperty("originalSide").GetString()!, unit.GetProperty("elementId").GetString()!),
            value.GetProperty("representationId").GetString()!, value.GetProperty("locationId").GetString()!, value.GetProperty("componentIds").EnumerateArray().Select(v => v.GetString()!).ToArray());
    }
    private static CampaignElementStateV6 ProfileElement(CampaignElementStateV6 source, CapabilityPointAmount? cp = null, int? cohesion = null,
        int? ammo = null, int? toe = null, string? location = null, string? parent = null, bool? pinned = null, int? stage = null, CampaignElementReserveStatus? reserve = null) =>
        new(source.ElementId, location ?? source.CurrentLocationId, reserve ?? source.ReserveStatus,
            new(source.OperationalState.LedgerGameTurn, stage ?? source.OperationalState.LedgerOperationStage, cp ?? source.OperationalState.CapabilityPointsExpended,
                cohesion ?? source.OperationalState.CohesionLevel, source.OperationalState.VehicleBreakdownState, source.OperationalState.MovementEnded, source.OperationalState.InitialLedgerOrigin),
            source.Components.Select(c => new CampaignComponentToeState(c.ComponentId, toe ?? c.CurrentToe, c.InitialToeOrigin)), source.SourceParentFormationId,
            parent ?? source.CurrentParentFormationId, new(ammo ?? source.Ammunition.Points, source.Ammunition.InitialAmmunitionOrigin),
            new(source.Readiness.GameTurn, source.Readiness.OperationStage, source.Readiness.WaterStatus, source.Readiness.StoresStatus, pinned ?? source.Readiness.Pinned, source.Readiness.InitialReadinessOrigin));

    [Fact]
    public void FourActualCompletedHistoriesMatchFrozenAdmissionBoundaryAndLiteralAssessment()
    {
        using var fixture = Fixture("combat-inherited-selection-v1.json");
        var cases = fixture.RootElement.GetProperty("cases").EnumerateArray().ToArray();
        var histories = CombatMovementHistoryReplayTests.SnapshotHistories().Where(h => h.Events.Length >= 20).ToArray();
        var admitted = 0;
        foreach (var history in histories)
        {
            using var tail = JsonDocument.Parse(history.Events[^1]);
            if (tail.RootElement.GetProperty("eventType").GetString() != "breakdown-segment-completed") continue;
            var boundary = CampaignCombatCertification.Admit(history.Request, history.Created, history.Events);
            var moves = boundary.Entry.Lifecycle.Movement.ActualProgressRefs.Count;
            var side = boundary.Assessment.Acting.Unit.OriginalSide;
            var other = side == "axis" ? "commonwealth" : "axis";
            var row = cases.Single(c => c.GetProperty("actor").GetString() == side && c.GetProperty("moves").GetInt32() == moves);
            var bytes = CampaignCombatIdentityCodec.SerializeBoundary(boundary);
            Assert.Equal(row.GetProperty("goldens").GetProperty("bytes")[1].GetInt32(), bytes.Length);
            Assert.Equal(row.GetProperty("goldens").GetProperty("sha256")[1].GetString(), Digest(bytes));
            var actingLocation = side + (moves == 6 ? "-supply" : "-rear");
            var defendingLocation = side == "axis" ? "assault-east" : "assault-west";
            var literal = "{\"actingSide\":\"" + side + "\",\"actingElementId\":\"" + side + "-assault-battalion\",\"defendingSide\":\"" + other +
                "\",\"defendingElementId\":\"" + other + "-assault-battalion\",\"actingLocationId\":\"" + actingLocation + "\",\"defendingLocationId\":\"" + defendingLocation +
                "\",\"normalWeather\":true,\"adjacent\":false,\"actingCapabilityPointsExpended\":{\"numerator\":" + (moves * 2) +
                ",\"denominator\":1},\"defendingCapabilityPointsExpended\":{\"numerator\":0,\"denominator\":1},\"actingWithinVoluntaryCeiling\":false,\"defendingWithinVoluntaryCeiling\":true,\"candidateIds\":[]}";
            var entry = CampaignCombatBreakdownCompletionCodec.SerializeState(boundary.Entry);
            Assert.Equal(Encoding.UTF8.GetBytes("{\"contractVersion\":1,\"entry\":" + Encoding.UTF8.GetString(entry) + ",\"assessment\":" + literal + "}"), bytes);
            var read = CampaignCombatIdentityCodec.ReadBoundary(bytes, history.Request, history.Created, history.Events);
            Assert.Equal(bytes, CampaignCombatIdentityCodec.SerializeBoundary(read));
            Assert.Equal(history.Events.Length + 1, boundary.Entry.StateVersion);
            Assert.Equal(history.Events.Length, boundary.Entry.Receipts.Count);
            Assert.Equal(history.Events.Select(Digest), boundary.History.Events.Select(Digest));
            Assert.Empty(boundary.Assessment.CandidateIds);
            Assert.Equal(10 - moves * 2, boundary.Entry.Lifecycle.Movement.World.Elements.Single(e => e.ElementId == boundary.Assessment.Acting.Unit.ElementId).OperationalState.CohesionLevel);
            admitted++;
        }
        Assert.Equal(4, admitted);
    }

    [Fact]
    public void UntrustedUnsupportedOrIncompleteHistoriesCannotBecomeEmptyAssessment()
    {
        var history = Supported();
        var boundary = CampaignCombatCertification.Admit(history.Request, history.Created, history.Events);
        var bytes = CampaignCombatIdentityCodec.SerializeBoundary(boundary);
        foreach (var missing in new byte[][] { [], new byte[1_048_577], Encoding.UTF8.GetBytes("null") })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatCertification.Admit(history.Request, missing, history.Events));
        var foreign = CombatHistoryReplayTests.SnapshotHistories().First(h => !h.Created.SequenceEqual(history.Created));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatCertification.Admit(foreign.Request, history.Created, history.Events));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatCertification.Admit(history.Request, foreign.Created, history.Events));
        foreach (var cut in new[] { 0, 4, 5, 9, 10, 16, 17, 18, 19 }) Reject(history.Events.Take(cut).ToArray());
        foreach (var h in CombatMovementHistoryReplayTests.SnapshotHistories().Where(h => h.Events.Length is 15 or 19))
            Assert.ThrowsAny<JsonException>(() => CampaignCombatCertification.Admit(h.Request, h.Created, h.Events));
        var reaction = CombatReactionHistoryReplayTests.SnapshotHistories().First(h => h.Events.Length == 13);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatCertification.Admit(reaction.Request, reaction.Created, reaction.Events));
        Reject(history.Events.Where((_, i) => i != 11).ToArray());
        var reordered = history.Events.ToArray();
        (reordered[11], reordered[12]) = (reordered[12], reordered[11]);
        Reject(reordered);
        Reject(history.Events.Append(history.Events[^1]).ToArray());
        var tampered = history.Events.ToArray(); tampered[^1] = [.. tampered[^1], (byte)' ']; Reject(tampered);
        var otherHead = CombatMovementHistoryReplayTests.SnapshotHistories().First(h => h.Events.Length == 21);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatIdentityCodec.ReadBoundary(bytes, otherHead.Request, otherHead.Created, otherHead.Events));
        using var synthetic = Fixture("combat-selection-steps-v1.json");
        var syntheticBytes = Encoding.UTF8.GetBytes(synthetic.RootElement.GetProperty("boundaries")[0].GetProperty("canonicalUtf8").GetString()!);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatIdentityCodec.ReadBoundary(syntheticBytes, history.Request, history.Created, history.Events));
        void Reject(byte[][] events) => Assert.ThrowsAny<JsonException>(() => CampaignCombatCertification.Admit(history.Request, history.Created, events));
    }

    [Fact]
    public void EveryAssessmentFieldAndInheritedAuthorityRemainReplayDerived()
    {
        var history = Supported();
        var bytes = CampaignCombatIdentityCodec.SerializeBoundary(CampaignCombatCertification.Admit(history.Request, history.Created, history.Events));
        var root = JsonNode.Parse(bytes)!.AsObject();
        foreach (var field in root["assessment"]!.AsObject().Select(pair => pair.Key).ToArray())
        {
            var changed = root.DeepClone(); var value = changed["assessment"]![field]!;
            changed["assessment"]![field] = value.GetValueKind() switch
            {
                JsonValueKind.String => JsonValue.Create(value.GetValue<string>() + ".foreign"),
                JsonValueKind.True or JsonValueKind.False => JsonValue.Create(!value.GetValue<bool>()),
                JsonValueKind.Array => new JsonArray("candidate.forged"),
                _ => new JsonObject { ["numerator"] = 0, ["denominator"] = 2 },
            };
            Reject(Bytes(changed));
            changed = root.DeepClone(); changed["assessment"]!.AsObject().Remove(field); Reject(Bytes(changed));
        }
        foreach (var mutation in new Action<JsonNode>[] {
            node => node["entry"]!["stateVersion"] = 1,
            node => node["entry"]!["prefix"] = Digest(bytes),
            node => node["entry"]!["randomState"]!["nextByteCursor"] = 0,
            node => node["entry"]!["receipts"]!.AsArray().RemoveAt(0),
            node => node["entry"]!["movementEnd"] = null,
            node => node["entry"]!["breakdownCompletionReceiptId"] = null,
            node => node["entry"]!["world"]!["elements"]![0]!["operationalState"]!["capabilityPointsExpended"]!["numerator"] = 0,
        })
        {
            var changed = root.DeepClone(); mutation(changed);
            Assert.NotEqual(bytes, Bytes(changed)); Reject(Bytes(changed));
        }
        var text = Encoding.UTF8.GetString(bytes);
        foreach (var value in new[] { "null", "{}", "[", text + " ", "\uFEFF" + text,
            text.Replace("\"contractVersion\":1,\"entry\"", "\"contractVersion\":1,\"contractVersion\":1,\"entry\"", StringComparison.Ordinal),
            text.Replace("\"assessment\"", "\"assessm\\u0065nt\"", StringComparison.Ordinal),
            text.Replace("\"contractVersion\":1,\"entry\"", "\"contractVersion\":1.0,\"entry\"", StringComparison.Ordinal) }) Reject(Encoding.UTF8.GetBytes(value));
        var reverse = new JsonObject(); foreach (var pair in root.Reverse()) reverse.Add(pair.Key, pair.Value?.DeepClone()); Reject(Bytes(reverse));
        var unknown = root.DeepClone(); unknown["unknown"] = true; Reject(Bytes(unknown));
        void Reject(byte[] candidate) => Assert.ThrowsAny<JsonException>(() => CampaignCombatIdentityCodec.ReadBoundary(candidate, history.Request, history.Created, history.Events));
    }

    [Fact]
    public void CurrentParticipantBindingPreservesUnitComponentIdentityAcrossMovementAndRepresentationChange()
    {
        var history = Supported();
        var boundary = CampaignCombatCertification.Admit(history.Request, history.Created, history.Events);
        var world = boundary.Entry.Lifecycle.Movement.World;
        var current = boundary.Assessment.Acting;
        var initialWorld = CampaignCreatedV11Serializer.Deserialize(history.Created, history.Request).InitialWorld;
        var initial = CampaignCombatCertification.BindParticipant(history.Request, initialWorld, current.Unit);
        Assert.Equal(initial.Unit, current.Unit);
        Assert.Equal(initial.Components, current.Components);
        Assert.NotEqual(initial.LocationId, current.LocationId);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatIdentityCodec.ReadParticipant(CampaignCombatIdentityCodec.SerializeParticipant(initial), history.Request, world, current.Unit));
        var renamed = CopyWorld(world, representations: world.Representations.Select(r => r.BoundElementIds.Contains(current.Unit.ElementId)
            ? new CampaignMapRepresentationState("renamed.current", r.CurrentLocationId, r.BindingKind, r.BoundElementIds) : r));
        var rebound = CampaignCombatCertification.BindParticipant(history.Request, renamed, current.Unit);
        Assert.Equal(current.Unit, rebound.Unit); Assert.Equal(current.Components, rebound.Components);
        Assert.Equal("renamed.current", rebound.RepresentationId);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatIdentityCodec.ReadParticipant(CampaignCombatIdentityCodec.SerializeParticipant(current), history.Request, renamed, current.Unit));
        var literal = "{\"unit\":{\"creationBinding\":\"" + history.Request.CreationBinding + "\",\"originalSide\":\"axis\",\"elementId\":\"axis-assault-battalion\"}," +
            "\"representationId\":\"renamed.current\",\"locationId\":\"axis-supply\",\"componentIds\":[\"axis-assault-battalion.toe.infantry\"]}";
        Assert.Equal(Encoding.UTF8.GetBytes(literal), CampaignCombatIdentityCodec.SerializeParticipant(rebound));
        Assert.Equal(rebound.Unit, CampaignCombatIdentityCodec.ReadParticipant(Encoding.UTF8.GetBytes(literal), history.Request, renamed, current.Unit).Unit);
        var component = current.Components[0];
        Assert.NotEqual(component, new CampaignCombatComponentKey(boundary.Assessment.Defending.Unit, component.ComponentId));
        Assert.NotEqual(component, new CampaignCombatComponentKey(new CampaignCombatUnitKey("another.creation", "axis", component.Unit.ElementId), component.ComponentId));
        var element = initialWorld.Elements.Single(e => e.ElementId == current.Unit.ElementId);
        var depleted = Element(element, element.CurrentLocationId, [new CampaignComponentToeState(element.Components[0].ComponentId, 0, element.Components[0].InitialToeOrigin)]);
        var depletedWorld = CopyWorld(initialWorld, initialWorld.Elements.Select(e => e.ElementId == element.ElementId ? depleted : e));
        var historicalIdentity = CampaignCombatCertification.BindParticipant(history.Request, depletedWorld, current.Unit);
        Assert.Equal(current.Unit, historicalIdentity.Unit);
        Assert.Equal(current.Components, historicalIdentity.Components);
        // Identity continuity is not a positive eligibility assertion for this hypothetical depletion.
    }

    [Theory]
    [InlineData("contact")]
    [InlineData("engaged")]
    public void NewOccupantNeverReplacesOriginalRelationshipParticipant(string kind)
    {
        var history = Supported();
        var boundary = CampaignCombatCertification.Admit(history.Request, history.Created, history.Events);
        var world = CampaignCreatedV11Serializer.Deserialize(history.Created, history.Request).InitialWorld;
        var actor = boundary.Assessment.Acting.Unit; var defender = boundary.Assessment.Defending.Unit;
        var relation = new CampaignCombatRelationship("relation.original", "receipt.original", kind, actor, defender, 1, 1, true, null, null);
        var original = CampaignCombatCertification.BindParticipant(history.Request, world, relation.Attacker);
        var locations = world.Elements.ToDictionary(e => e.ElementId, e => world.Elements.Single(other => other.ElementId != e.ElementId).CurrentLocationId, StringComparer.Ordinal);
        var swapped = CopyWorld(world, world.Elements.Select(e => Element(e, locations[e.ElementId])), world.Representations.Select(r =>
            new CampaignMapRepresentationState(r.RepresentationId, locations[r.BoundElementIds[0]], r.BindingKind, r.BoundElementIds)));
        // Hypothetical identity probe only: no admitted movement, relationship mutation or Combat offer.
        var movedOriginal = CampaignCombatCertification.BindParticipant(history.Request, swapped, relation.Attacker);
        var arrival = CampaignCombatCertification.BindParticipant(history.Request, swapped, relation.Defender);
        Assert.Equal(original.LocationId, arrival.LocationId);
        Assert.Equal(actor, movedOriginal.Unit); Assert.NotEqual(actor, arrival.Unit);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatIdentityCodec.ReadParticipant(CampaignCombatIdentityCodec.SerializeParticipant(arrival), history.Request, swapped, relation.Attacker));
        Assert.Equal(actor, relation.Attacker); Assert.Equal(defender, relation.Defender);
    }

    [Fact]
    public void ForeignMissingDuplicateOrNoncanonicalParticipantBindingsReject()
    {
        var history = Supported();
        var boundary = CampaignCombatCertification.Admit(history.Request, history.Created, history.Events);
        var current = boundary.Assessment.Acting; var world = boundary.Entry.Lifecycle.Movement.World;
        foreach (var unit in new[] { new CampaignCombatUnitKey("foreign.creation", "axis", current.Unit.ElementId),
            new CampaignCombatUnitKey(current.Unit.CreationBinding, "commonwealth", current.Unit.ElementId),
            new CampaignCombatUnitKey(current.Unit.CreationBinding, "axis", "missing.element") })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatCertification.BindParticipant(history.Request, world, unit));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatCertification.BindParticipant(history.Request, CopyWorld(world, creationBinding: "foreign.creation"), current.Unit));
        var source = world.Elements.Single(e => e.ElementId == current.Unit.ElementId);
        var forged = Element(source, source.CurrentLocationId, [new CampaignComponentToeState("foreign.component", 10, source.Components[0].InitialToeOrigin)]);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatCertification.BindParticipant(history.Request,
            CopyWorld(world, world.Elements.Select(e => e.ElementId == source.ElementId ? forged : e)), current.Unit));
        Assert.ThrowsAny<JsonException>(() => new CampaignCombatParticipant(current.Unit, current.RepresentationId, current.LocationId, [current.ComponentIds[0], current.ComponentIds[0]]));
        foreach (var count in new[] { -1, 0, 513 })
            Assert.ThrowsAny<JsonException>(() => new CampaignCombatParticipant(current.Unit, current.RepresentationId, current.LocationId, new CountOnlyComponents(count)));
        Assert.ThrowsAny<JsonException>(() => new CampaignCombatParticipant(current.Unit, new string('x', 129), current.LocationId, current.ComponentIds));
        var bytes = CampaignCombatIdentityCodec.SerializeParticipant(current); var root = JsonNode.Parse(bytes)!;
        foreach (var field in new[] { "unit", "representationId", "locationId", "componentIds" })
        {
            var changed = root.DeepClone(); changed.AsObject().Remove(field); Reject(Bytes(changed));
        }
        var duplicate = root.DeepClone(); duplicate["componentIds"]!.AsArray().Add(current.ComponentIds[0]); Reject(Bytes(duplicate));
        var foreignComponent = root.DeepClone(); foreignComponent["componentIds"]![0] = "foreign.component"; Reject(Bytes(foreignComponent));
        Reject([.. bytes, (byte)' ']); Reject(Encoding.UTF8.GetBytes("null"));
        var text = Encoding.UTF8.GetString(bytes);
        Reject(Encoding.UTF8.GetBytes(text.Replace("\"unit\"", "\"u\\u006eit\"", StringComparison.Ordinal)));
        void Reject(byte[] candidate) => Assert.ThrowsAny<JsonException>(() => CampaignCombatIdentityCodec.ReadParticipant(candidate, history.Request, world, current.Unit));
    }

    [Fact]
    public void ParticipantShapeAndCanonicalBytesRejectBeforeTrustedBindingContextIsRead()
    {
        const string canonical = "{\"unit\":{\"creationBinding\":\"creation.fixture\",\"originalSide\":\"axis\",\"elementId\":\"axis-assault-battalion\"},\"representationId\":\"map-representation.0001\",\"locationId\":\"assault-west\",\"componentIds\":[\"axis-assault-battalion.toe.infantry\"]}";
        var root = JsonNode.Parse(canonical)!;
        var invalid = new List<string> { "null", "{}", "[]", "[", canonical + " ", "\uFEFF" + canonical,
            canonical.Replace("\"unit\"", "\"u\\u006eit\"", StringComparison.Ordinal),
            canonical.Replace("\"locationId\":\"assault-west\"", "\"locationId\":\"assault-west\",\"locationId\":\"assault-west\"", StringComparison.Ordinal),
            canonical.Replace("\"originalSide\":\"axis\"", "\"originalSide\":\"axis\",\"originalSide\":\"axis\"", StringComparison.Ordinal),
        };
        foreach (var field in new[] { "unit", "representationId", "locationId", "componentIds" })
        {
            var missing = root.DeepClone(); missing.AsObject().Remove(field); invalid.Add(missing.ToJsonString());
            var wrongType = root.DeepClone(); wrongType[field] = 1; invalid.Add(wrongType.ToJsonString());
            var nullValue = root.DeepClone(); nullValue[field] = null; invalid.Add(nullValue.ToJsonString());
        }
        foreach (var field in new[] { "creationBinding", "originalSide", "elementId" })
        {
            var missing = root.DeepClone(); missing["unit"]!.AsObject().Remove(field); invalid.Add(missing.ToJsonString());
            var wrongType = root.DeepClone(); wrongType["unit"]![field] = false; invalid.Add(wrongType.ToJsonString());
        }
        var unknown = root.DeepClone(); unknown["unexpected"] = false; invalid.Add(unknown.ToJsonString());
        var unknownUnit = root.DeepClone(); unknownUnit["unit"]!["unexpected"] = false; invalid.Add(unknownUnit.ToJsonString());
        var badItem = root.DeepClone(); badItem["componentIds"]![0] = true; invalid.Add(badItem.ToJsonString());
        var reordered = new JsonObject(); foreach (var pair in root.AsObject().Reverse()) reordered.Add(pair.Key, pair.Value?.DeepClone()); invalid.Add(reordered.ToJsonString());
        foreach (var candidate in invalid)
            Assert.ThrowsAny<JsonException>(() => CampaignCombatIdentityCodec.ReadParticipant(Encoding.UTF8.GetBytes(candidate), null!, null!, null!));
        // Well-formed canonical syntax does not authenticate anything: it proceeds to the binder,
        // whose first null-context guard proves the parsing stage completed in the required order.
        Assert.Throws<ArgumentNullException>(() => CampaignCombatIdentityCodec.ReadParticipant(Encoding.UTF8.GetBytes(canonical), null!, null!, null!));
    }

    [Fact]
    public void BoundaryShapeAndCanonicalBytesRejectBeforeTrustedHistoryIsRead()
    {
        var history = Supported();
        var bytes = CampaignCombatIdentityCodec.SerializeBoundary(CampaignCombatCertification.Admit(history.Request, history.Created, history.Events));
        var text = Encoding.UTF8.GetString(bytes); var root = JsonNode.Parse(bytes)!;
        foreach (var invalid in new[] { "null", "{}", "[]", "[", text + " ", "\uFEFF" + text,
            text.Replace("\"entry\"", "\"e\\u006etry\"", StringComparison.Ordinal),
            text.Replace("\"contractVersion\":1", "\"contractVersion\":1,\"contractVersion\":1", StringComparison.Ordinal),
            text.Replace("\"stateVersion\":", "\"stateVersion\":1,\"stateVersion\":", StringComparison.Ordinal),
            text.Replace("\"normalWeather\":true", "\"normalWeather\":1", StringComparison.Ordinal),
            text.Replace("\"numerator\":12", "\"numerator\":12.0", StringComparison.Ordinal) }) Reject(Encoding.UTF8.GetBytes(invalid));
        // Every populated nested object must have its own closed shape; checking only the root,
        // assessment or entry would allow malformed external World/authority values into replay.
        foreach (var path in ObjectPaths(root, []))
        {
            var value = At(root, path).AsObject();
            foreach (var name in value.Select(pair => pair.Key))
            {
                var missing = root.DeepClone(); At(missing, path).AsObject().Remove(name); Reject(Bytes(missing));
                var wrongType = root.DeepClone(); At(wrongType, path)[name] = value[name] is JsonArray ? JsonValue.Create(false) : new JsonArray(); Reject(Bytes(wrongType));
            }
            var unknown = root.DeepClone(); At(unknown, path)["unexpected"] = false; Reject(Bytes(unknown));
            if (value.Count > 1)
            {
                var changed = root.DeepClone(); var target = At(changed, path).AsObject();
                var fields = target.Reverse().Select(pair => (pair.Key, Value: pair.Value?.DeepClone())).ToArray();
                target.Clear(); foreach (var pair in fields) target.Add(pair.Key, pair.Value); Reject(Bytes(changed));
            }
        }
        foreach (var count in new[] { 512, 513 })
        {
            var changed = root.DeepClone(); var candidates = changed["assessment"]!["candidateIds"]!.AsArray();
            for (var i = 0; i < count; i++) candidates.Add($"candidate.{i:D3}");
            if (count == 512) ReachesHistory(Bytes(changed)); else Reject(Bytes(changed));
        }
        var largeCursor = root.DeepClone(); largeCursor["entry"]!["randomState"]!["nextByteCursor"] = ulong.MaxValue;
        ReachesHistory(Bytes(largeCursor)); // UInt64 grammar must not be narrowed to Int64.
        foreach (var badCursor in new[] { "-1", "18446744073709551616", "1.0" })
        {
            var changed = root.DeepClone(); changed["entry"]!["randomState"]!["nextByteCursor"] = JsonNode.Parse(badCursor); Reject(Bytes(changed));
        }
        var reorderedElements = root.DeepClone(); var elements = reorderedElements["entry"]!["world"]!["elements"]!.AsArray();
        var first = elements[0]!.DeepClone(); var second = elements[1]!.DeepClone(); elements[0] = second; elements[1] = first;
        Reject(Bytes(reorderedElements));
        var nullableCycle = root.DeepClone(); nullableCycle["entry"]!["cycle"] = null; ReachesHistory(Bytes(nullableCycle));
        var signedLimit = root.DeepClone(); signedLimit["entry"]!["stateVersion"] = long.MinValue; ReachesHistory(Bytes(signedLimit));
        var integerLimit = root.DeepClone(); integerLimit["contractVersion"] = int.MinValue; ReachesHistory(Bytes(integerLimit));
        foreach (var (path, invalid) in new (string[] Path, JsonNode? Value)[]
        {
            (["entry", "randomState", "algorithmId"], JsonValue.Create("invalid id")),
            (["entry", "prefix"], JsonValue.Create("sha256:bad")),
            (["entry", "rulesetHash"], JsonValue.Create(new string('A', 64))),
            (["entry", "initiativeHolder"], JsonValue.Create("system")),
            (["entry", "breakdownFlow", "kind"], JsonValue.Create("unknown")),
            (["entry", "stateVersion"], JsonNode.Parse("9223372036854775808")),
            (["contractVersion"], JsonNode.Parse("2147483648")),
            (["assessment", "normalWeather"], null),
        })
        {
            var changed = root.DeepClone(); At(changed, path[..^1])[path[^1]] = invalid; Reject(Bytes(changed));
        }
        var moving = root.DeepClone();
        moving["entry"]!["breakdownFlow"] = new JsonObject
        {
            ["kind"] = "moving",
            ["route"] = new JsonObject
            {
                ["routeId"] = root["entry"]!["prefix"]!.DeepClone(),
                ["firstMoveStateVersion"] = 1,
                ["elementId"] = "axis-assault-battalion",
                ["representationId"] = "map-representation.0001",
                ["owner"] = "axis",
                ["originLocationId"] = "assault-west",
                ["currentLocationId"] = "axis-rear",
                ["cohortIds"] = new JsonArray(),
            },
        };
        ReachesHistory(Bytes(moving)); // Syntax recognizes nullable/union arms without admitting them.
        moving["entry"]!["breakdownFlow"]!["route"]!["cohortIds"]!.AsArray().Add("unexpected"); Reject(Bytes(moving));
        ReachesHistory(bytes);
        void Reject(byte[] candidate) => Assert.ThrowsAny<JsonException>(() => CampaignCombatIdentityCodec.ReadBoundary(candidate, history.Request, history.Created, new InaccessibleHistory()));
        void ReachesHistory(byte[] candidate) => Assert.Throws<InvalidOperationException>(() => CampaignCombatIdentityCodec.ReadBoundary(candidate, history.Request, history.Created, new InaccessibleHistory()));
        static JsonNode At(JsonNode node, string[] path)
        {
            foreach (var part in path) node = node is JsonArray array ? array[int.Parse(part, System.Globalization.CultureInfo.InvariantCulture)]! : node[part]!;
            return node;
        }
        static IEnumerable<string[]> ObjectPaths(JsonNode? node, string[] path)
        {
            if (node is JsonObject obj)
            {
                yield return path;
                foreach (var pair in obj) foreach (var child in ObjectPaths(pair.Value, [.. path, pair.Key])) yield return child;
            }
            else if (node is JsonArray array)
                for (var i = 0; i < array.Count; i++) foreach (var child in ObjectPaths(array[i], [.. path, i.ToString(System.Globalization.CultureInfo.InvariantCulture)])) yield return child;
        }
    }

    [Fact]
    public void BoundsPrecedeHistoryAccessAndAllReturnedValuesOwnTheirBuffers()
    {
        var history = Supported();
        foreach (var invalid in new[] { Array.Empty<byte>(), new byte[1_048_577], Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)),
            Encoding.UTF8.GetBytes("{\"entry\":{\"world\":{\"cohesionCauses\":[" + string.Join(',', Enumerable.Repeat("null", 513)) + "]}}}") })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatIdentityCodec.ReadBoundary(invalid, history.Request, history.Created, new InaccessibleHistory()));
        var boundary = CampaignCombatCertification.Admit(history.Request, history.Created, history.Events);
        var bytes = CampaignCombatIdentityCodec.SerializeBoundary(boundary); var expected = bytes.ToArray();
        var read = CampaignCombatIdentityCodec.ReadBoundary(bytes, history.Request, history.Created, history.Events);
        foreach (var buffer in history.Events.Append(history.Created).Append(bytes).Concat(read.History.Events).Append(read.History.Created)) Array.Fill(buffer, (byte)0);
        Assert.Equal(expected, CampaignCombatIdentityCodec.SerializeBoundary(boundary));
        Assert.Equal(expected, CampaignCombatIdentityCodec.SerializeBoundary(read));
        Assert.Equal(expected, CampaignCombatIdentityCodec.SerializeBoundary(CampaignCombatCertification.Admit(history.Request, read.History.Created, read.History.Events)));
        var ids = boundary.Assessment.Acting.ComponentIds.ToArray();
        var participant = new CampaignCombatParticipant(boundary.Assessment.Acting.Unit, "test.representation", "axis-supply", ids);
        var before = CampaignCombatIdentityCodec.SerializeParticipant(participant); ids[0] = "changed";
        Assert.Equal(before, CampaignCombatIdentityCodec.SerializeParticipant(participant));
        Assert.Throws<NotSupportedException>(() => ((IList<string>)participant.ComponentIds)[0] = "changed");
    }

    private static (CampaignCombatCreationRequest Request, byte[] Created, byte[][] Events) Supported() =>
        CombatMovementHistoryReplayTests.SnapshotHistories().First(h => h.Events.Length == 20);
    private static CampaignWorldSnapshotV7 CopyWorld(CampaignWorldSnapshotV7 world, IEnumerable<CampaignElementStateV6>? elements = null,
        IEnumerable<CampaignMapRepresentationState>? representations = null, string? creationBinding = null) => new(7, creationBinding ?? world.CreationBinding,
        elements ?? world.Elements, representations ?? world.Representations, world.BrokenVehicleLots, world.CohesionCauses, world.Relationships,
        world.CustodyLots, world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
    private static CampaignElementStateV6 Element(CampaignElementStateV6 source, string location, IEnumerable<CampaignComponentToeState>? components = null) =>
        new(source.ElementId, location, source.ReserveStatus, source.OperationalState, components ?? source.Components,
            source.SourceParentFormationId, source.CurrentParentFormationId, source.Ammunition, source.Readiness);
    private sealed class InaccessibleHistory : IReadOnlyList<byte[]>
    {
        public int Count => throw new InvalidOperationException("History read before value bounds.");
        public byte[] this[int index] => throw new InvalidOperationException("History indexed.");
        public IEnumerator<byte[]> GetEnumerator() => throw new InvalidOperationException("History enumerated.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private sealed class CountOnlyComponents(int count) : IReadOnlyList<string>
    {
        public int Count => count;
        public string this[int index] => throw new InvalidOperationException("Component indexed before count rejection.");
        public IEnumerator<string> GetEnumerator() => throw new InvalidOperationException("Component enumerated.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());
    private static JsonDocument Fixture(string name) => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", name)));
    private static string Digest(byte[] bytes) => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
}
