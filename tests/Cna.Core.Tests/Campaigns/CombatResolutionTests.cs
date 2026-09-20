using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Randomness;
using static Cna.Core.Tests.Campaigns.CombatSealsTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatResolutionTests
{
    [Fact]
    public void ThirtyTwoResolveEventsAndSixtyFourStateHashesMatchFrozenAuthority()
    {
        using var fixture = Fixture(); var events = 0; var cuts = 0;
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray())
        {
            var test = Case(row);
            var paid = CombatSealsTests.Replay(test, test.Events.Length);
            Assert.Equal(Encoding.UTF8.GetBytes(row.GetProperty("committedCanonicalUtf8").GetString()!), CampaignCombatSealedRoundCodec.SerializeState(paid));
            var inputs = row.GetProperty("resultInputs").EnumerateArray().Select(i => CampaignCombatResolutionCodec.ReadInput(JsonSerializer.SerializeToUtf8Bytes(i))).ToArray();
            var literal = Encoding.UTF8.GetBytes(row.GetProperty("resultEventCanonicalUtf8")[0].GetString()!);
            for (var cut = 0; cut <= 1; cut++)
            {
                var state = CampaignCombatResolution.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary, test.PredecessorInputs,
                    test.PredecessorEvents, test.Inputs, test.Events, inputs[..cut], cut == 0 ? [] : [literal]);
                var bytes = CampaignCombatResolutionCodec.SerializeState(state);
                Assert.Equal(row.GetProperty("stateHashes")[cut].GetString(), CampaignOpeningPreambleCodec.Hash(bytes)); cuts++;
                var restored = CampaignCombatResolutionCodec.ReadState(bytes, test.Request, test.Created, test.Boundary, test.PredecessorInputs,
                    test.PredecessorEvents, test.Inputs, test.Events, inputs[..cut], cut == 0 ? [] : [literal]);
                Assert.Equal(bytes, CampaignCombatResolutionCodec.SerializeState(restored));
            }
            var result = CampaignCombatResolution.ApplyTrustedBoundary(test.Request, test.Created, test.Boundary, test.PredecessorInputs,
                test.PredecessorEvents, test.Inputs, test.Events, [], [], inputs[0]);
            Assert.Equal(literal, result.EventBytes); events++;
        }
        Assert.Equal(32, events); Assert.Equal(64, cuts);
    }

    [Fact]
    public void LiteralDrawsPreserveRejectionsRolesPaidWorldAndPendingConsequences()
    {
        using var fixture = Fixture(); var eight = 0; var nine = 0; var rejected = 0; var crossings = 0;
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray())
        {
            var test = Case(row); var result = Apply(test, Input(row)); var state = result.State; var assault = state.Result!;
            if (assault.Draws.Count == 8) eight++; else { Assert.Equal(9, assault.Draws.Count); nine++; }
            if (assault.Draws.Any(d => d.Consumed.Count > 1)) rejected++;
            if (assault.BeforeRandomState.NextByteCursor / 32 != (assault.AfterRandomState.NextByteCursor - 1) / 32) crossings++;
            Assert.Equal(assault.BeforeRandomState.NextByteCursor, assault.Draws[0].BeforeCursor);
            for (var i = 0; i < assault.Draws.Count; i++)
            {
                var d = assault.Draws[i]; Assert.Equal(d.BeforeCursor + (ulong)d.Consumed.Count, d.AfterCursor);
                Assert.All(d.Consumed.SkipLast(1), b => Assert.InRange(b, 252, 255)); Assert.InRange(d.Consumed[^1], 0, 251);
                Assert.Equal(d.Consumed[^1] % 6 + 1, d.Die);
                if (i > 0) Assert.Equal(assault.Draws[i - 1].AfterCursor, d.BeforeCursor);
            }
            Assert.Equal(assault.AfterRandomState, state.RandomState);
            var pending = Assert.Single(state.World.Settlements); var paid = state.Context.Committed;
            Assert.Equal(paid.World.Elements, state.World.Elements); Assert.Equal(paid.World.Representations, state.World.Representations);
            Assert.Equal(paid.World.CohesionCauses, state.World.CohesionCauses); Assert.Empty(state.World.CustodyLots); Assert.Empty(state.World.Relationships);
            Assert.Null(pending.Disposition); Assert.Null(pending.Losses); Assert.Null(pending.Retreat); Assert.Null(pending.Custody); Assert.Null(pending.Relationships);
            Assert.Equal(paid.World.Elements, pending.PreLossElements); Assert.Equal(paid.CommitmentId, pending.CommitmentId);
            Assert.Equal(assault.ResultId, pending.ResultId); Assert.Single(paid.AttackHistory); Assert.Single(paid.TargetUses);
            Assert.Equal(3000, paid.Timing!.OpeningFloorUnixMilliseconds);
        }
        Assert.Equal((16, 16, 12, 0), (eight, nine, rejected, crossings));
    }

    [Fact]
    public void ExistingCrossBlockVectorAndUInt64OverflowUseFullyRebuiltPredecessors()
    {
        using var fixture = Fixture(); var original = Case(fixture.RootElement.GetProperty("traces")[0]);
        var test = RebuildCursor(original, 30); var input = ResolveInput(test); var result = Apply(test, input);
        var assault = result.State.Result!;
        Assert.Equal("4dd79fabb6070dc7", Convert.ToHexStringLower(assault.Draws.SelectMany(d => d.Consumed).Select(b => (byte)b).ToArray()));
        int[] expectedDice = [6, 6, 4, 4, 3, 2, 2, 2];
        Assert.Equal(expectedDice, assault.Draws.Select(d => d.Die));
        Assert.Equal(38UL, result.State.RandomState.NextByteCursor); Assert.Equal(-1, assault.Facts.Differential);
        Assert.Equal(10, assault.Facts.AttackerPercent); Assert.Equal(10, assault.Facts.DefenderPercent); Assert.Null(assault.Facts.CaptureDie);
        foreach (var cursor in new[] { ulong.MaxValue, ulong.MaxValue - 3 })
        {
            var overflow = RebuildCursor(original, cursor); var prior = Replay(overflow); var bytes = CampaignCombatResolutionCodec.SerializeState(prior);
            Assert.ThrowsAny<JsonException>(() => Apply(overflow, ResolveInput(overflow)));
            Assert.Equal(bytes, CampaignCombatResolutionCodec.SerializeState(Replay(overflow)));
            Assert.Equal(cursor, prior.RandomState.NextByteCursor); Assert.Null(prior.Result); Assert.Empty(prior.World.Settlements);
        }
    }

    [Fact]
    public void DiscardRetryIndependentInputsAndLaterFamiliesCannotRerollOrSettle()
    {
        using var fixture = Fixture();
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray())
        {
            var test = Case(row); var input = Input(row); var before = Replay(test); var beforeBytes = CampaignCombatResolutionCodec.SerializeState(before);
            var candidate = Apply(test, input); var bytes = candidate.EventBytes!;
            Assert.Equal(beforeBytes, CampaignCombatResolutionCodec.SerializeState(before)); Assert.Equal(beforeBytes, CampaignCombatResolutionCodec.SerializeState(Replay(test)));
            Assert.Equal(bytes, Apply(test, input).EventBytes);
            foreach (var changed in new[] { input, input with { AdmittedAt = null, ClockAvailable = false }, input with { AdmittedAt = 9000 } })
            {
                var retry = Apply(test, changed, [input], [bytes]); Assert.Equal(CombatStepsDisposition.Duplicate, retry.Disposition);
                Assert.Equal(bytes, retry.EventBytes); Assert.Equal(CampaignCombatResolutionCodec.SerializeState(candidate.State), CampaignCombatResolutionCodec.SerializeState(retry.State));
            }
            foreach (var kind in new[] { "expire", "unavailable" })
                Assert.Equal(CombatStepsDisposition.NoOp, Apply(test, input with { Command = input.Command with { Kind = kind, ExpectedPriorVersion = null, DecisionId = "stale" } }, [input], [bytes]).Disposition);
            Assert.ThrowsAny<JsonException>(() => Apply(test, input with { Actor = CampaignOpeningPreambleActor.Axis }, [input], [bytes]));
            Assert.ThrowsAny<JsonException>(() => Apply(test, input with { AdmittedAt = -1 }, [input], [bytes]));
            Assert.ThrowsAny<JsonException>(() => Apply(test, input with { Command = input.Command with { ExpectedPriorVersion = candidate.State.StateVersion } }, [input], [bytes]));
            Assert.ThrowsAny<JsonException>(() => Replay(test, [input, input], [bytes, bytes]));
            var laterInputs = row.GetProperty("resultInputs").EnumerateArray().Select(i => CampaignCombatResolutionCodec.ReadInput(JsonSerializer.SerializeToUtf8Bytes(i))).ToArray();
            var laterEvents = row.GetProperty("resultEventCanonicalUtf8").EnumerateArray().Select(e => Encoding.UTF8.GetBytes(e.GetString()!)).ToArray();
            // Task015 supports custody; relationships and closure remain future work.
            var laterStart = Array.FindIndex(laterEvents, e => JsonNode.Parse(e)!["effect"]!["kind"]!.GetValue<string>() == "relationships-settled");
            for (var i = laterStart; i < laterInputs.Length; i++)
            {
                Assert.ThrowsAny<JsonException>(() => Apply(test, laterInputs[i], [input], [bytes]));
                Assert.ThrowsAny<JsonException>(() => Replay(test, [input, laterInputs[i]], [bytes, laterEvents[i]]));
            }
            Assert.ThrowsAny<JsonException>(() => Replay(test, [input with { Actor = CampaignOpeningPreambleActor.Axis }], [bytes]));
        }
    }

    [Fact]
    public void RehashedDrawResultWorldAndProvenanceForgeriesReject()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("traces")[0]; var test = Case(row); var input = Input(row);
        var result = Apply(test, input); var eventBytes = result.EventBytes!;
        foreach (var path in new[] { "effect.result.rulesInputHash", "effect.result.commitmentId", "effect.result.resultId", "effect.settlementId", "priorPrefix", "roundClockConfigurationHash", "configurationHash" })
        {
            var node = JsonNode.Parse(eventBytes)!; Set(node, path, path.EndsWith("Id", StringComparison.Ordinal) ? "foreign" : ForeignHash);
            Assert.ThrowsAny<JsonException>(() => Replay(test, [input], [Rehash(node)]));
        }
        foreach (var field in new[] { "purpose", "beforeCursor", "afterCursor", "die", "consumed" })
        {
            var node = JsonNode.Parse(eventBytes)!; var draw = node["effect"]!["result"]!["draws"]![0]!;
            draw[field] = field == "purpose" ? JsonValue.Create("defender.capture.share") : field == "consumed" ? new JsonArray(0) : JsonValue.Create(99);
            Assert.ThrowsAny<JsonException>(() => Replay(test, [input], [Rehash(node)]));
        }
        foreach (var field in new[] { "attackerMoraleCoordinate", "defenderMoraleCoordinate", "attackerMorale", "defenderMorale", "basicDifferential" })
        {
            var node = JsonNode.Parse(eventBytes)!; node["effect"]!["result"]![field] = 1;
            Assert.ThrowsAny<JsonException>(() => Replay(test, [input], [Rehash(node)]));
        }
        var stateBytes = CampaignCombatResolutionCodec.SerializeState(result.State);
        foreach (var path in new[] { "committedHash", "prefix", "status", "settlementId", "randomState.nextByteCursor", "result.afterRandomState.nextByteCursor" })
        {
            var node = JsonNode.Parse(stateBytes)!;
            Set(node, path, path.EndsWith("Cursor", StringComparison.Ordinal) ? JsonValue.Create(999) : JsonValue.Create(path is "committedHash" or "prefix" ? ForeignHash : "foreign"));
            Assert.ThrowsAny<JsonException>(() => Read(test, Bytes(node), [input], [eventBytes]));
        }
        var world = JsonNode.Parse(stateBytes)!; world["world"]!["settlements"]![0]!["preLossElements"]![0]!["ammunition"]!["points"] = 10;
        Assert.ThrowsAny<JsonException>(() => Read(test, Bytes(world), [input], [eventBytes]));
        Assert.ThrowsAny<JsonException>(() => Replay(test with { Events = test.Events[..^1] }));
        Assert.ThrowsAny<JsonException>(() => Replay(test with { Inputs = test.Inputs.Select((i, n) => n == 1 ? i with { Actor = CampaignOpeningPreambleActor.System } : i).ToArray() }));
        Assert.ThrowsAny<JsonException>(() => Replay(test with { Boundary = test.Boundary with { RandomState = new(1, SandtableRandom.AlgorithmId, 0, 30) } }));
    }

    [Fact]
    public void RawShapeCanonicalAndBoundsFailuresPrecedeTrustedHistory()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("traces")[0]; var test = Case(row);
        var input = Input(row); var result = Apply(test, input); var bytes = CampaignCombatResolutionCodec.SerializeState(result.State);
        CombatResolutionState SentryRead(byte[] raw) => CampaignCombatResolutionCodec.ReadState(raw, null!, test.Created, test.Boundary,
            test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, new Sentry<CombatResolutionInput>(), new Sentry<byte[]>());
        Assert.Throws<InvalidOperationException>(() => SentryRead(bytes));
        foreach (var raw in new[] { Encoding.UTF8.GetBytes("null"), Encoding.UTF8.GetBytes("{}"), Encoding.UTF8.GetBytes(" " + Encoding.UTF8.GetString(bytes)),
            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes) + "\n"), new byte[1_048_577],
            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace("\"contractVersion\":2", "\"contractVersion\":2,\"contractVersion\":2", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes).Replace("committedHash", "\\u0063ommittedHash", StringComparison.Ordinal)) })
            Assert.ThrowsAny<JsonException>(() => SentryRead(raw));
        foreach (var path in new[] { "result.draws", "result.beforeRandomState.nextByteCursor", "world.settlements", "roundClockConfigurationHash", "acceptedHighWater" })
        {
            var node = JsonNode.Parse(bytes)!; Set(node, path, false); Assert.ThrowsAny<JsonException>(() => SentryRead(Bytes(node)));
        }
        var array = JsonNode.Parse(bytes)!; array["result"]!["draws"] = new JsonArray(Enumerable.Range(0, 513).Select(_ => (JsonNode?)null).ToArray());
        Assert.ThrowsAny<JsonException>(() => SentryRead(Bytes(array)));
        var missing = JsonNode.Parse(bytes)!; missing.AsObject().Remove("world"); Assert.ThrowsAny<JsonException>(() => SentryRead(Bytes(missing)));
        var unordered = JsonNode.Parse(bytes)!.AsObject(); var first = unordered["contractVersion"]!.DeepClone(); unordered.Remove("contractVersion"); unordered.Add("contractVersion", first);
        Assert.ThrowsAny<JsonException>(() => SentryRead(Bytes(unordered)));
        var badEvent = JsonNode.Parse(result.EventBytes!)!; badEvent["effect"]!["result"]!["draws"]![0]!["die"] = false;
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResolution.ReplayTrustedBoundary(null!, test.Created, test.Boundary,
            test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, new Sentry<CombatResolutionInput>(1), [Bytes(badEvent)]));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResolution.ReplayTrustedBoundary(null!, test.Created, test.Boundary,
            test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, new Sentry<CombatResolutionInput>(33), new Sentry<byte[]>(33)));
    }

    [Fact]
    public void ResultProfileAllowsSemanticRoutesAndFutureTurnsWithoutWeakeningC3a()
    {
        using var fixture = Fixture();
        var row = fixture.RootElement.GetProperty("traces").EnumerateArray().First(r => r.GetProperty("name").GetString()!.Contains("capture-escape", StringComparison.Ordinal));
        var test = Case(row); var final = Encoding.UTF8.GetBytes(row.GetProperty("stateCanonicalUtf8").GetString()!);
        CombatResolutionState SentryRead(byte[] raw) => CampaignCombatResolutionCodec.ReadState(raw, null!, test.Created, test.Boundary,
            test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, new Sentry<CombatResolutionInput>(), new Sentry<byte[]>());
        Assert.Throws<InvalidOperationException>(() => SentryRead(final));
        using var document = JsonDocument.Parse(final); var world = document.RootElement.GetProperty("world");
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatIdentityCodec.WriteSelectionStepsExternalSyntax(writer, world, "World"));
        foreach (var turn in new[] { 1, 115 })
        {
            var node = JsonNode.Parse(final)!; node["world"]!["replacementEntitlements"]![0]!["eligibleScope"]!["gameTurn"] = turn;
            Assert.Throws<InvalidOperationException>(() => SentryRead(Bytes(node)));
        }
        foreach (var turn in new[] { 0, 116 })
        {
            var node = JsonNode.Parse(final)!; node["world"]!["replacementEntitlements"]![0]!["eligibleScope"]!["gameTurn"] = turn;
            Assert.ThrowsAny<JsonException>(() => SentryRead(Bytes(node)));
        }
        var reversed = JsonNode.Parse(final)!; var elements = reversed["world"]!["elements"]!.AsArray(); var copy = elements.Select(e => e!.DeepClone()).Reverse().ToArray();
        reversed["world"]!["elements"] = new JsonArray(copy); Assert.Throws<InvalidOperationException>(() => SentryRead(Bytes(reversed)));
        var broken = JsonNode.Parse(final)!; broken["world"]!["brokenVehicleLots"] = new JsonArray(new JsonObject());
        Assert.ThrowsAny<JsonException>(() => SentryRead(Bytes(broken)));
        var emptyRoute = JsonNode.Parse(final)!; emptyRoute["world"]!["settlements"]![0]!["disposition"]!["route"] = new JsonArray();
        Assert.Throws<InvalidOperationException>(() => SentryRead(Bytes(emptyRoute)));
    }

    [Fact]
    public void ResolvedFactoryPreservesLegacyPaidStateChecksAndWorldSerializationAuthority()
    {
        using var fixture = Fixture(); var test = Case(fixture.RootElement.GetProperty("traces")[0]); var state = Apply(test, ResolveInput(test)).State;
        var settlement = Assert.Single(state.World.Settlements); var paid = state.Context.Committed.World;
        var created = CampaignCombatSettlementState.CreateResolvedResultV2(settlement.CommitmentId, settlement.ResultId, settlement.GameTurn,
            settlement.OperationStage, settlement.Attacker, settlement.Defender, paid.Elements, settlement.Result);
        Assert.Equal(settlement, created);
        Assert.Throws<ArgumentException>(() => new CampaignCombatSettlementState(settlement.SettlementId, settlement.CommitmentId, settlement.ResultId,
            1, 1, settlement.Attacker, settlement.Defender, paid.Elements, settlement.Result, null, null, null, null, null));
        var legacy = new CampaignCombatSettlementState("legacy", "legacy.commit", "legacy.result", 1, 1, settlement.Attacker,
            settlement.Defender, paid.Elements, settlement.Result, null, null, null, null, null); Assert.Equal("legacy", legacy.SettlementId);
        foreach (var commitment in new[] { "cmt.x", "cmt.sha256:" + new string('a', 64), "cmt." + new string('A', 64) })
            Assert.Throws<ArgumentException>(() => CampaignCombatSettlementState.CreateResolvedResultV2(commitment, settlement.ResultId, 1, 1,
                settlement.Attacker, settlement.Defender, paid.Elements, settlement.Result));
        Assert.Throws<ArgumentException>(() => CampaignCombatSettlementState.CreateResolvedResultV2(settlement.CommitmentId, "res.x", 1, 1,
            settlement.Attacker, settlement.Defender, paid.Elements, settlement.Result));
        Assert.Throws<ArgumentException>(() => CampaignCombatSettlementState.CreateResolvedResultV2(settlement.CommitmentId, settlement.ResultId, 1, 1,
            settlement.Attacker, settlement.Attacker, paid.Elements, settlement.Result));
        Assert.Throws<ArgumentException>(() => CampaignCombatSettlementState.CreateResolvedResultV2(settlement.CommitmentId, settlement.ResultId, 1, 1,
            settlement.Attacker, settlement.Defender, test.Boundary.World.Elements, settlement.Result));
        Assert.Throws<ArgumentException>(() => CampaignCombatSettlementState.CreateResolvedResultV2(settlement.CommitmentId, settlement.ResultId, 1, 2,
            settlement.Attacker, settlement.Defender, paid.Elements, settlement.Result));
        var original = CampaignCombatResolutionCodec.SerializeState(state);
        var changed = new CampaignWorldSnapshotV7(7, paid.CreationBinding, paid.Elements, paid.Representations, paid.BrokenVehicleLots,
            paid.CohesionCauses, paid.Relationships, paid.CustodyLots, paid.Guards, paid.ReplacementEntitlements, paid.FutureObligations, []);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(state with { World = changed }));
        // A paid non-settlement field cannot be normalized away, even when its own model is valid.
        var element = paid.Elements[0]; var readiness = element.Readiness;
        var altered = new CampaignElementStateV6(element.ElementId, element.CurrentLocationId, element.ReserveStatus, element.OperationalState,
            element.Components, element.SourceParentFormationId, element.CurrentParentFormationId, element.Ammunition,
            new(readiness.GameTurn, readiness.OperationStage, readiness.WaterStatus, readiness.StoresStatus, true, readiness.InitialReadinessOrigin));
        var forgedPaid = new CampaignWorldSnapshotV7(7, paid.CreationBinding, [altered, paid.Elements[1]], paid.Representations, paid.BrokenVehicleLots,
            paid.CohesionCauses, paid.Relationships, paid.CustodyLots, paid.Guards, paid.ReplacementEntitlements, paid.FutureObligations, []);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(Replay(test) with { World = forgedPaid }));
        Assert.Equal(original, CampaignCombatResolutionCodec.SerializeState(state));
    }

    [Fact]
    public void CreatedBuffersDrawsAndCollectionsAreOwnedBeforeCallerHistoryAccess()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("traces")[0]; var test = Case(row); var input = Input(row);
        var expected = Apply(test, input); var ownedCreated = test.Created.ToArray();
        var events = new MutatingList<byte[]>([], () => ownedCreated[0] = 0);
        var actual = CampaignCombatResolution.ApplyTrustedBoundary(test.Request, ownedCreated, test.Boundary, test.PredecessorInputs,
            test.PredecessorEvents, test.Inputs, test.Events, [], events, input);
        Assert.Equal(expected.EventBytes, actual.EventBytes); Assert.Equal(0, ownedCreated[0]);
        var returned = actual.EventBytes!; returned[0] = 0; Assert.Equal(expected.EventBytes, actual.EventBytes);
        Assert.Throws<NotSupportedException>(() => ((IList<CombatResolutionDraw>)actual.State.Result!.Draws).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<int>)actual.State.Result!.Draws[0].Consumed).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<CampaignCombatSettlementState>)actual.State.World.Settlements).Clear());
        var source = actual.State.Receipts.ToList(); var copied = actual.State with { Receipts = source }; source.Clear(); Assert.Single(copied.Receipts);
        var consumed = new List<int> { 1 }; var draw = new CombatResolutionDraw("x", 0, 1, 2, consumed); consumed[0] = 3; Assert.Equal(1, draw.Consumed[0]);
    }

    private sealed class Sentry<T>(int? count = null) : IReadOnlyList<T>
    {
        public int Count => count ?? throw new InvalidOperationException("Trusted context accessed.");
        public T this[int index] => throw new InvalidOperationException("Trusted context indexed.");
        public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private sealed class MutatingList<T>(T[] values, Action mutate) : IReadOnlyList<T>
    {
        public int Count { get { mutate(); return values.Length; } }
        public T this[int index] => values[index];
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void ConditionalCaptureAndInheritedTimingGrammarRemainExact()
    {
        using var fixture = Fixture();
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray().Where((_, index) => index % 4 == 0))
        {
            var test = Case(row); var input = Input(row); var result = Apply(test, input); var node = JsonNode.Parse(result.EventBytes!)!;
            var draws = node["effect"]!["result"]!["draws"]!.AsArray();
            if (draws.Count == 9) draws.RemoveAt(8); else draws.Add(draws[0]!.DeepClone());
            Assert.ThrowsAny<JsonException>(() => Replay(test, [input], [Rehash(node)]));
            var swapped = JsonNode.Parse(result.EventBytes!)!; var other = swapped["effect"]!["result"]!["draws"]!.AsArray();
            var first = other[0]!.DeepClone(); other[0] = other[4]!.DeepClone(); other[4] = first;
            Assert.ThrowsAny<JsonException>(() => Replay(test, [input], [Rehash(swapped)]));
        }
        var timedRow = fixture.RootElement.GetProperty("traces").EnumerateArray().First(r => r.GetProperty("name").GetString()!.StartsWith("zero-retreat.", StringComparison.Ordinal));
        var timedTest = Case(timedRow); var timed = Encoding.UTF8.GetBytes(timedRow.GetProperty("resultEventCanonicalUtf8")[1].GetString()!);
        CombatResolutionState SentryEvent(byte[] bytes) => CampaignCombatResolution.ReplayTrustedBoundary(null!, timedTest.Created, timedTest.Boundary,
            timedTest.PredecessorInputs, timedTest.PredecessorEvents, timedTest.Inputs, timedTest.Events, new Sentry<CombatResolutionInput>(1), [bytes]);
        Assert.Throws<InvalidOperationException>(() => SentryEvent(timed));
        var bad = JsonNode.Parse(timed)!; var timing = bad["effect"]!["window"]!["timing"]!.AsObject();
        timing["openingFloorUnixMilliseconds"] = timing["highWaterUnixMilliseconds"]!.DeepClone(); timing.Remove("highWaterUnixMilliseconds");
        Assert.ThrowsAny<JsonException>(() => SentryEvent(Bytes(bad)));
        var deep = JsonNode.Parse(timed)!; JsonNode nested = JsonValue.Create(0)!;
        for (var i = 0; i < 34; i++) nested = new JsonArray(nested);
        deep["effect"] = nested; Assert.ThrowsAny<JsonException>(() => SentryEvent(Bytes(deep)));
    }

    private static CombatResolutionInput Input(JsonElement row) => CampaignCombatResolutionCodec.ReadInput(JsonSerializer.SerializeToUtf8Bytes(row.GetProperty("resultInputs")[0]));
    private static CombatResolutionInput ResolveInput(TestCase test)
    {
        var paid = CombatSealsTests.Replay(test, test.Events.Length);
        return new(new(2, paid.Base.ConfigurationHash, CampaignCombatResolution.Policy, "resolve", paid.RoundId!, paid.CommitmentId!, paid.StateVersion), CampaignOpeningPreambleActor.System);
    }
    private static CombatResolutionState Replay(TestCase test, CombatResolutionInput[]? inputs = null, byte[][]? events = null) =>
        CampaignCombatResolution.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents,
            test.Inputs, test.Events, inputs ?? [], events ?? []);
    private static CombatResolutionResult Apply(TestCase test, CombatResolutionInput input, CombatResolutionInput[]? inputs = null, byte[][]? events = null) =>
        CampaignCombatResolution.ApplyTrustedBoundary(test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents,
            test.Inputs, test.Events, inputs ?? [], events ?? [], input);
    private static CombatResolutionState Read(TestCase test, byte[] bytes, CombatResolutionInput[] inputs, byte[][] events) =>
        CampaignCombatResolutionCodec.ReadState(bytes, test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents,
            test.Inputs, test.Events, inputs, events);
    private const string ForeignHash = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());
    private static void Set(JsonNode root, string path, JsonNode? value)
    {
        var parts = path.Split('.'); var current = root; foreach (var part in parts[..^1]) current = current[part]!; current[parts[^1]] = value;
    }
    private static byte[] Rehash(JsonNode root)
    {
        root.AsObject().Remove("receiptId"); root["receiptId"] = "cmb." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.result-receipt.v2", Bytes(root))[7..]; return Bytes(root);
    }

    internal static CombatSealsTests.TestCase Case(JsonElement row)
    {
        var node = JsonNode.Parse(row.GetRawText())!;
        node["inputs"] = node["roundInputs"]!.DeepClone();
        node["eventCanonicalUtf8"] = node["roundEventCanonicalUtf8"]!.DeepClone();
        using var mapped = JsonDocument.Parse(node.ToJsonString());
        return CombatSealsTests.Case(mapped.RootElement);
    }
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-result-settlement-v2.json")));
    private static TestCase RebuildCursor(TestCase original, ulong cursor)
    {
        var boundary = original.Boundary with { RandomState = new(1, SandtableRandom.AlgorithmId, 0, cursor) };
        var predecessorInputs = new List<CombatStepsInput>(); var predecessorEvents = new List<byte[]>();
        var candidate = CampaignCombatSelectionSteps.ValidateBoundary(original.Request, original.Created, boundary).Candidate!;
        foreach (var input in original.PredecessorInputs)
        {
            var state = CampaignCombatSelectionSteps.ReplayTrustedBoundary(original.Request, original.Created, boundary, predecessorInputs, predecessorEvents);
            var command = input.Command with
            {
                SegmentId = state.SegmentId,
                ExpectedPriorVersion = input.Command.ExpectedPriorVersion is null ? null : state.StateVersion,
                DecisionId = input.Command.DecisionId is null ? null : state.SegmentId + (input.Command.Kind == "choose-selection" ? ".selection" : ".rba"),
                Candidate = input.Command.Candidate is null ? null : candidate,
                Participant = input.Command.Participant is null ? null : candidate.Defender.Unit,
            };
            var trusted = input with { Command = command };
            var result = CampaignCombatSelectionSteps.ApplyTrustedBoundary(original.Request, original.Created, boundary, predecessorInputs, predecessorEvents, trusted);
            predecessorInputs.Add(trusted); predecessorEvents.Add(result.EventBytes!);
        }
        var inputs = new List<CombatRoundInput>(); var events = new List<byte[]>();
        foreach (var input in original.Inputs)
        {
            var state = CampaignCombatSealedRound.ReplayTrustedBoundary(original.Request, original.Created, boundary, predecessorInputs, predecessorEvents, inputs, events);
            var slot = input.Command.SlotId is null ? null : state.Slots.Single(s => s.Owner == CampaignCombatSealedRoundCodec.Actor(input.Actor));
            var trusted = input with
            {
                Command = input.Command with
                {
                    SegmentId = state.Base.Steps.SegmentId,
                    RoundId = state.RoundId,
                    ExpectedPriorVersion = input.Command.ExpectedPriorVersion is null ? null : state.StateVersion,
                    SlotId = slot?.SlotId,
                    Allocation = slot?.Allocation
                }
            };
            var result = CampaignCombatSealedRound.ApplyTrustedBoundary(original.Request, original.Created, boundary, predecessorInputs, predecessorEvents, inputs, events, trusted);
            inputs.Add(trusted); events.Add(result.EventBytes!);
        }
        return new(original.Request, original.Created, boundary, predecessorInputs.ToArray(), predecessorEvents.ToArray(), inputs.ToArray(), events.ToArray(), 5);
    }
}
