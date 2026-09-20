using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatIdentityTests
{
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
