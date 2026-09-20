using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatSealsTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatClosureTests
{
    [Fact]
    public void AcceptedCustodyFrontierAdvancesToLiteralRelationships()
    {
        using var fixture = Fixture();
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray())
        {
            var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var events = Events(row); var end = events.Length - 3;
            var result = Apply(test, inputs[..end], events[..end], inputs[end]);
            Assert.Equal(CombatStepsDisposition.Accepted, result.Disposition);
            Assert.Equal(events[end], result.EventBytes);
        }
    }
    [Fact]
    public void AllClosurePrefixesMatch272LiteralEvents304HashCutsAnd32CompleteFinalStates()
    {
        using var fixture = Fixture(); var eventCount = 0; var cuts = 0; var finals = 0; var contact = 0; var engaged = 0; var absent = 0; var four = 0; var five = 0;
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray())
        {
            var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var events = Events(row);
            for (var cut = 0; cut <= events.Length; cut++)
            {
                var state = Replay(test, inputs[..cut], events[..cut]); var bytes = CampaignCombatResolutionCodec.SerializeState(state);
                Assert.Equal(row.GetProperty("stateHashes")[cut].GetString(), CampaignOpeningPreambleCodec.Hash(bytes));
                Assert.Equal(bytes, CampaignCombatResolutionCodec.SerializeState(Read(test, bytes, inputs[..cut], events[..cut]))); cuts++;
                if (cut < events.Length) { Assert.Equal(events[cut], Apply(test, inputs[..cut], events[..cut], inputs[cut]).EventBytes); eventCount++; }
            }
            var final = Replay(test, inputs, events); var frontier = Replay(test, inputs[..^3], events[..^3]); var settlement = final.World.Settlements.Single();
            Assert.Equal(row.GetProperty("stateCanonicalUtf8").GetString(), Encoding.UTF8.GetString(CampaignCombatResolutionCodec.SerializeState(final))); finals++;
            Assert.True(final.Closed); Assert.Equal("closed", final.Status); Assert.Null(final.Window);
            Assert.Equal(frontier.RandomState, final.RandomState); Assert.Equal(CampaignCombatResolutionCodec.SerializeResultPreimage(frontier.Result!), CampaignCombatResolutionCodec.SerializeResultPreimage(final.Result!));
            Assert.Equal(CampaignCombatSealedRoundCodec.SerializeState(frontier.Context.Committed), CampaignCombatSealedRoundCodec.SerializeState(final.Context.Committed));
            Assert.Equal(frontier.World.Elements, final.World.Elements); Assert.Equal(frontier.World.Representations, final.World.Representations);
            Assert.Equal(frontier.World.BrokenVehicleLots, final.World.BrokenVehicleLots); Assert.Equal(frontier.World.CohesionCauses, final.World.CohesionCauses);
            Assert.Equal(frontier.World.CustodyLots, final.World.CustodyLots); Assert.Equal(frontier.World.Guards, final.World.Guards);
            Assert.Equal(frontier.World.ReplacementEntitlements, final.World.ReplacementEntitlements); Assert.Equal(frontier.World.FutureObligations, final.World.FutureObligations);
            var receipt = settlement.Relationships!;
            Assert.Equal(settlement.Custody?.ReceiptId ?? settlement.Retreat!.ReceiptId, receipt.PredecessorReceiptId);
            if (receipt.Kind is null) { absent++; Assert.Empty(final.World.Relationships); }
            else
            {
                if (receipt.Kind == "contact") contact++; else engaged++;
                var relation = Assert.Single(final.World.Relationships);
                Assert.Equal(settlement.Attacker, relation.Attacker); Assert.Equal(settlement.Defender, relation.Defender);
                Assert.Equal(settlement.GameTurn, relation.GameTurn); Assert.Equal(settlement.OperationStage, relation.OperationStage);
                Assert.True(relation.Active); Assert.Null(relation.EndedByReceiptId); Assert.Null(relation.EndingCause);
                if (settlement.Result.RequiredRetreat > 0) Assert.Equal("contact", relation.Kind);
                Assert.DoesNotContain(final.World.Guards, guard => guard.GuardId == relation.Attacker.ElementId || guard.GuardId == relation.Defender.ElementId);
            }
            var round = JsonNode.Parse(events[^2])!; var ca = JsonNode.Parse(events[^1])!;
            var proof = round["effect"]!["proofReceipts"]!.AsArray().Select(x => x!.GetValue<string>()).ToArray();
            Assert.Equal(new[] { settlement.Disposition!.ReceiptId, settlement.Losses!.ReceiptId, settlement.Retreat!.ReceiptId, settlement.Custody?.ReceiptId, receipt.ReceiptId }.OfType<string>(), proof);
            if (proof.Length == 4) four++; else { Assert.Equal(5, proof.Length); five++; }
            Assert.Equal(round["receiptId"]!.GetValue<string>(), final.RoundClosureReceiptId);
            Assert.Equal(ca["receiptId"]!.GetValue<string>(), final.CaCompletionReceiptId);
            Assert.Equal(final.RoundClosureReceiptId, ca["effect"]!["roundClosureReceiptId"]!.GetValue<string>());
            Assert.Equal(final.Context.Committed.StepReceipts[^1], ca["effect"]!["previousStepReceiptId"]!.GetValue<string>());
        }
        Assert.Equal((272, 304, 32, 16, 4, 12, 16, 16), (eventCount, cuts, finals, contact, engaged, absent, four, five));
    }

    [Fact]
    public void EveryCutRetriesAndDiscardRemainStableIncludingClosedAndFreshCommandsReject()
    {
        using var fixture = Fixture();
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray())
        {
            var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var events = Events(row);
            for (var cut = 0; cut <= events.Length; cut++)
            {
                var state = Replay(test, inputs[..cut], events[..cut]); var bytes = CampaignCombatResolutionCodec.SerializeState(state);
                for (var i = 0; i < cut; i++)
                {
                    var retry = Apply(test, inputs[..cut], events[..cut], inputs[i] with { AdmittedAt = null, ClockAvailable = false });
                    Assert.Equal(CombatStepsDisposition.Duplicate, retry.Disposition); Assert.Equal(events[i], retry.EventBytes);
                    Assert.Equal(bytes, CampaignCombatResolutionCodec.SerializeState(retry.State));
                }
                if (cut < events.Length)
                {
                    var discarded = Apply(test, inputs[..cut], events[..cut], inputs[cut]);
                    Assert.Equal(bytes, CampaignCombatResolutionCodec.SerializeState(Replay(test, inputs[..cut], events[..cut])));
                    Assert.Equal(discarded.EventBytes, Apply(test, inputs[..cut], events[..cut], inputs[cut]).EventBytes);
                }
            }
            var final = Replay(test, inputs, events);
            foreach (var kind in new[] { "advance", "resolve", "choose" })
            {
                var command = inputs[^1].Command with
                {
                    Kind = kind,
                    ExpectedPriorVersion = kind == "choose" ? null : final.StateVersion,
                    DecisionId = kind == "choose" ? "fresh" : null,
                    Choice = kind == "choose" ? "retreat" : null
                };
                Assert.ThrowsAny<JsonException>(() => Apply(test, inputs, events, new(command, kind == "choose" ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.System)));
            }
            foreach (var kind in new[] { "expire", "unavailable" })
            {
                var result = Apply(test, inputs, events, new(inputs[^1].Command with { Kind = kind, ExpectedPriorVersion = null, DecisionId = "stale" }, CampaignOpeningPreambleActor.System));
                Assert.Equal(CombatStepsDisposition.NoOp, result.Disposition); Assert.Null(result.EventBytes);
            }
            Assert.ThrowsAny<JsonException>(() => Replay(test, [.. inputs, inputs[^1]], [.. events, events[^1]]));
            for (var i = events.Length - 3; i < events.Length; i++)
            {
                foreach (var input in new[] { inputs[i] with { AdmittedAt = 0 }, inputs[i] with { ClockAvailable = false }, inputs[i] with { Command = inputs[i].Command with { ExpectedPriorVersion = 0 } } })
                    Assert.ThrowsAny<JsonException>(() => Apply(test, inputs[..i], events[..i], input));
            }
        }
    }

    [Fact]
    public void RehashedRelationshipsProofOrderTopologyAndClosureForgeriesReject()
    {
        using var fixture = Fixture();
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray().Where((_, i) => i % 4 == 0))
        {
            var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var events = Events(row);
            void RejectEvent(int index, Action<JsonNode> change)
            {
                var node = JsonNode.Parse(events[index])!; change(node);
                Assert.ThrowsAny<JsonException>(() => Replay(test, inputs[..(index + 1)], [.. events[..index], Rehash(node)]));
            }
            foreach (var field in new[] { "receiptId", "predecessorReceiptId", "relationshipId", "kind" })
                RejectEvent(events.Length - 3, node => node["effect"]!["payload"]![field] = field == "kind" ? (node["effect"]!["payload"]!["kind"]?.GetValue<string>() == "engaged" ? "contact" : "engaged") : "foreign");
            foreach (var mutation in new[] { "missing", "reordered", "duplicate", "foreign" })
                RejectEvent(events.Length - 2, node =>
                {
                    var proof = node["effect"]!["proofReceipts"]!.AsArray();
                    if (mutation == "missing") proof.RemoveAt(0);
                    else if (mutation == "reordered") { var first = proof[0]!.GetValue<string>(); proof[0] = proof[1]!.GetValue<string>(); proof[1] = first; }
                    else proof[0] = mutation == "duplicate" ? proof[1]!.GetValue<string>() : "foreign";
                });
            RejectEvent(events.Length - 2, node => node["effect"]!["settlementId"] = "foreign");
            foreach (var field in new[] { "fromPositionId", "toPositionId", "previousStepReceiptId", "roundClosureReceiptId" })
                RejectEvent(events.Length - 1, node => node["effect"]![field] = "foreign");
            var bytes = CampaignCombatResolutionCodec.SerializeState(Replay(test, inputs, events));
            void RejectState(Action<JsonNode> change)
            {
                var node = JsonNode.Parse(bytes)!; change(node); Assert.ThrowsAny<JsonException>(() => Read(test, Bytes(node), inputs, events));
            }
            foreach (var field in new[] { "roundClosureReceiptId", "caCompletionReceiptId", "prefix" }) RejectState(node => node[field] = field == "prefix" ? "sha256:" + new string('0', 64) : "foreign");
            RejectState(node => node["randomState"]!["nextByteCursor"] = 0);
            RejectState(node => node["world"]!["elements"]![0]!["operationalState"]!["capabilityPointsExpended"]!["numerator"] = 99);
            var final = Replay(test, inputs, events);
            if (final.World.Relationships.Count > 0)
                foreach (var field in new[] { "attacker", "defender", "gameTurn", "operationStage", "creationReceiptId" })
                    RejectState(node => { var relation = node["world"]!["relationships"]![0]!; if (field is "attacker" or "defender") relation[field]!["elementId"] = "foreign"; else relation[field] = field is "gameTurn" or "operationStage" ? JsonValue.Create(2) : JsonValue.Create("foreign"); });
            if (final.World.FutureObligations.Count > 0) RejectState(node => node["world"]!["futureObligations"] = new JsonArray());
        }
    }

    [Fact]
    public void TypedPostRelationshipResourceForgeryAndStageMetadataReject()
    {
        using var fixture = Fixture();
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray())
        {
            var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var events = Events(row);
            var frontier = Replay(test, inputs[..^3], events[..^3]); var settlement = frontier.World.Settlements.Single();
            var receipt = CampaignCombatLossRetreat.Relationships(frontier.Context, frontier.World);
            var appended = settlement.WithResultV2Relationships(receipt);
            Assert.Null(settlement.Relationships); Assert.Equal(receipt, appended.Relationships);
            Assert.Throws<ArgumentException>(() => appended.WithResultV2Relationships(receipt));
            Assert.Throws<ArgumentException>(() => Replay(test, inputs[..1], events[..1]).World.Settlements.Single().WithResultV2Relationships(receipt));
            for (var cut = events.Length - 2; cut <= events.Length; cut++)
            {
                var state = Replay(test, inputs[..cut], events[..cut]); var world = state.World; var element = world.Elements[0]; var ledger = element.OperationalState;
                var changed = new CampaignElementStateV6(element.ElementId, element.CurrentLocationId, element.ReserveStatus,
                    new(ledger.LedgerGameTurn, ledger.LedgerOperationStage, new(99, 1), ledger.CohesionLevel, ledger.VehicleBreakdownState, ledger.MovementEnded, ledger.InitialLedgerOrigin),
                    element.Components, element.SourceParentFormationId, element.CurrentParentFormationId, element.Ammunition, element.Readiness);
                var forged = new CampaignWorldSnapshotV7(7, world.CreationBinding, world.Elements.Select(e => e.ElementId == changed.ElementId ? changed : e), world.Representations,
                    world.BrokenVehicleLots, world.CohesionCauses, world.Relationships, world.CustodyLots, world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
                Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(state with { World = forged }));
                foreach (var altered in new[] { state with { Closed = !state.Closed }, state with { RoundClosureReceiptId = "foreign" },
                    state with { CaCompletionReceiptId = "foreign" }, state with { Status = "custody" }, state with { World = frontier.World } })
                    Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(altered));
                var timing = new CombatStepsTiming(1, state.Context.Creation.Configuration.Hash, "custody", 30000, 0, 30000, 0);
                Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(state with { Window = new("foreign", "custody", "axis", timing) }));
            }
        }
    }

    [Fact]
    public void BorrowedReceiptCannotInventTerminalStage()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("traces")[0]; var test = CombatResolutionTests.Case(row);
        var inputs = Inputs(row); var events = Events(row);
        var relationships = Replay(test, inputs[..^2], events[..^2]);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(relationships with
        { Status = "round-closed", RoundClosureReceiptId = relationships.Receipts[^1].ReceiptId }));
        var round = Replay(test, inputs[..^1], events[..^1]);
        foreach (var changed in new[] { round with { RoundClosureOrigin = null },
            round with { RoundClosureOrigin = round.RoundClosureOrigin! with { PriorVersion = 0 } },
            round with { RoundClosureOrigin = round.RoundClosureOrigin! with { PriorPrefix = "sha256:" + new string('0', 64) } } })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(changed));
        var closed = Replay(test, inputs, events);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(closed with { CaCompletionOrigin = null }));
        foreach (var index in new[] { closed.Receipts.Count - 2, closed.Receipts.Count - 1 })
        {
            var original = closed.Receipts[index];
            foreach (var changed in new[] { original with { EventHash = "sha256:" + new string('0', 64) },
                original with { Actor = CampaignOpeningPreambleActor.Axis }, original with { StateVersion = 0 } })
            {
                var receipts = closed.Receipts.ToArray(); receipts[index] = changed;
                Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(closed with { Receipts = receipts }));
            }
        }
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(round with
        { Status = "closed", Closed = true, RoundClosureReceiptId = round.Receipts[^2].ReceiptId, CaCompletionReceiptId = round.Receipts[^1].ReceiptId }));
    }

    [Fact]
    public void ClosureProofAndBytesAreOwnedAndRawPayloadRejectsBeforeTrustedContext()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("traces")[0]; var test = CombatResolutionTests.Case(row);
        var inputs = Inputs(row); var events = Events(row); var proof = new[] { "first", "second" }; var effect = new CombatResolutionEffect.RoundClosed("settlement", proof);
        proof[0] = "mutated"; Assert.Equal("first", effect.ProofReceipts[0]); Assert.Throws<NotSupportedException>(() => ((IList<string>)effect.ProofReceipts).Clear());
        CombatResolutionState SentryEvent(byte[] raw) => CampaignCombatResolution.ReplayTrustedBoundary(null!, test.Created, test.Boundary,
            test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, new Sentry<CombatResolutionInput>(), [raw]);
        Assert.Throws<InvalidOperationException>(() => SentryEvent(events[^2]));
        foreach (var index in new[] { events.Length - 3, events.Length - 2, events.Length - 1 })
        {
            var node = JsonNode.Parse(events[index])!;
            var field = index == events.Length - 3 ? "payload" : index == events.Length - 2 ? "proofReceipts" : "roundClosureReceiptId";
            node["effect"]![field] = false; Assert.ThrowsAny<JsonException>(() => SentryEvent(Bytes(node)));
            var result = Apply(test, inputs[..index], events[..index], inputs[index]); var copy = result.EventBytes!; copy[0] = 0; Assert.Equal(events[index], result.EventBytes);
        }
        var over = JsonNode.Parse(events[^2])!; over["effect"]!["proofReceipts"] = new JsonArray(Enumerable.Range(0, 513).Select(_ => (JsonNode?)JsonValue.Create("proof")).ToArray());
        Assert.ThrowsAny<JsonException>(() => SentryEvent(Bytes(over)));
        Assert.ThrowsAny<JsonException>(() => SentryEvent(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(events[^2]) + "\n")));
        Assert.ThrowsAny<JsonException>(() => SentryEvent(new byte[1_048_577]));
    }
    private sealed class Sentry<T> : IReadOnlyList<T>
    {
        public int Count => 1;
        public T this[int index] => throw new InvalidOperationException("Trusted context accessed.");
        public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());
    private static byte[] Rehash(JsonNode root)
    {
        root.AsObject().Remove("receiptId"); root["receiptId"] = "cmb." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.result-receipt.v2", Bytes(root))[7..]; return Bytes(root);
    }
    private static CombatResolutionState Read(TestCase test, byte[] bytes, CombatResolutionInput[] inputs, byte[][] events) => CampaignCombatResolutionCodec.ReadState(
        bytes, test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events);
    private static CombatResolutionState Replay(TestCase test, CombatResolutionInput[] inputs, byte[][] events) => CampaignCombatResolution.ReplayTrustedBoundary(
        test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events);
    private static CombatResolutionResult Apply(TestCase test, CombatResolutionInput[] inputs, byte[][] events, CombatResolutionInput input) => CampaignCombatResolution.ApplyTrustedBoundary(
        test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events, input);
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-result-settlement-v2.json")));
    private static CombatResolutionInput[] Inputs(JsonElement row) => row.GetProperty("resultInputs").EnumerateArray().Select(i => CampaignCombatResolutionCodec.ReadInput(JsonSerializer.SerializeToUtf8Bytes(i))).ToArray();
    private static byte[][] Events(JsonElement row) => row.GetProperty("resultEventCanonicalUtf8").EnumerateArray().Select(e => Encoding.UTF8.GetBytes(e.GetString()!)).ToArray();
}
