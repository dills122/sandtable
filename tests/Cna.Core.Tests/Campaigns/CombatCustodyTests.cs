using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatSealsTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatCustodyTests
{
    [Fact]
    public void AllCustodyPrefixesMatch176LiteralEventsAnd208StateHashCuts()
    {
        using var fixture = Fixture(); var events = 0; var cuts = 0; var guards = 0; var escapes = 0;
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray())
        {
            var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var literals = Events(row); var end = End(literals);
            for (var cut = 0; cut <= end; cut++)
            {
                var state = Replay(test, inputs[..cut], literals[..cut]); var bytes = CampaignCombatResolutionCodec.SerializeState(state);
                Assert.Equal(row.GetProperty("stateHashes")[cut].GetString(), CampaignOpeningPreambleCodec.Hash(bytes));
                Assert.Equal(bytes, CampaignCombatResolutionCodec.SerializeState(Read(test, bytes, inputs[..cut], literals[..cut]))); cuts++;
                if (cut < end) { Assert.Equal(literals[cut], Apply(test, inputs[..cut], literals[..cut], inputs[cut]).EventBytes); events++; }
            }
            var final = Replay(test, inputs[..end], literals[..end]); Assert.Null(final.Window); Assert.Null(final.World.Settlements.Single().Relationships);
            if (final.World.Guards.Count > 0) guards++; else if (final.World.ReplacementEntitlements.Count > 0) escapes++;
            Assert.Equal(final.World.CustodyLots.Count > 0 ? "custody" : "retreat", final.Status);
            Assert.Equal(literals[end], Apply(test, inputs[..end], literals[..end], inputs[end]).EventBytes);
            for (var i = end + 1; i < inputs.Length; i++) Assert.ThrowsAny<JsonException>(() => Apply(test, inputs[..end], literals[..end], inputs[i]));
        }
        Assert.Equal((176, 208, 8, 8), (events, cuts, guards, escapes));
    }
    [Fact]
    public void TwoHundredSameOwnerClockComparisonsIgnoreEarlierPrivateAcceptance()
    {
        using var fixture = Fixture(); var comparisons = 0;
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray().Where(r => r.GetProperty("name").GetString()!.StartsWith("attacker-capture-escape.", StringComparison.Ordinal)))
        {
            var test = CombatResolutionTests.Case(row); var original = Inputs(row);
            var pair = new[] { BeforeCustody(test, original, 10001), BeforeCustody(test, original, 11000) };
            Assert.Equal(pair[0].State.World, pair[1].State.World);
            foreach (var (now, available) in new (long?, bool)[] { (0, true), (3500, true), (10500, true), (null, true), (null, false), (10500, false), (CampaignCombatSelectionSteps.UtcMaximum, true) })
            {
                var after = pair.Select(p => Apply(test, p.Inputs, p.Events, original[5] with { AdmittedAt = now, ClockAvailable = available })).ToArray();
                Assert.Equal(after[0].State.Window, after[1].State.Window); Assert.Equal(after[0].State.World, after[1].State.World); comparisons += 2;
            }
            var opening = original[5] with { AdmittedAt = 10500 };
            var opened = pair.Select(p => Apply(test, p.Inputs, p.Events, opening)).ToArray();
            Assert.Equal(11000, opened[1].State.AcceptedHighWater); Assert.Equal(10500, opened[1].State.Window!.Timing.HighWaterUnixMilliseconds);
            Assert.Equal(40500, opened[1].State.Window!.Timing.DeadlineUnixMilliseconds);
            foreach (var time in new long?[] { null, 0, 10499, 10500, 10600, 11000, 40499, 40500, 40501 })
                foreach (var available in new[] { true, false })
                {
                    var input = original[6] with { Command = original[6].Command with { Choice = "relocate-and-guard" }, AdmittedAt = time, ClockAvailable = available };
                    var outcomes = pair.Select((prior, index) => Outcome(test, [.. prior.Inputs, opening], [.. prior.Events, opened[index].EventBytes!], input)).ToArray();
                    Assert.Equal(outcomes[0], outcomes[1]);
                    Assert.Equal(available && time >= 40500 ? "rejected" : "custody-settled", outcomes[0].Kind);
                    if (available && time == 10600) Assert.Single(outcomes[0].World!.Guards);
                    comparisons += 2;
                }
        }
        Assert.Equal(200, comparisons);
    }

    [Fact]
    public void GuardAndEscapePreserveCurrentResourcesOriginalSourceAndFutureObligations()
    {
        using var fixture = Fixture(); var guards = 0; var escapes = 0; var cpEleven = 0;
        foreach (var row in CaptureRows(fixture))
        {
            var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var events = Events(row); var end = End(events);
            var prior = Replay(test, inputs[..(end - 2)], events[..(end - 2)]); var final = Replay(test, inputs[..end], events[..end]);
            var originalLot = prior.World.CustodyLots.Single(); var lot = final.World.CustodyLots.Single(); var settlement = final.World.Settlements.Single();
            var donor = prior.World.Elements.Single(e => e.ElementId == lot.Captor.ElementId); var current = final.World.Elements.Single(e => e.ElementId == donor.ElementId);
            var custody = settlement.Custody!; var obligation = Assert.Single(final.World.FutureObligations);
            Assert.Equal(originalLot.OriginalComponent, lot.OriginalComponent); Assert.Equal(originalLot.Quantity, lot.Quantity); Assert.Equal(originalLot.OriginLocationId, lot.OriginLocationId);
            Assert.Equal(prior.Result!.ResultId, final.Result!.ResultId);
            Assert.Equal(CampaignCombatResolutionCodec.SerializeResultPreimage(prior.Result), CampaignCombatResolutionCodec.SerializeResultPreimage(final.Result)); Assert.Equal(prior.RandomState, final.RandomState); Assert.Equal(prior.Context.CommittedHash, final.Context.CommittedHash);
            Assert.Equal(donor.OperationalState, current.OperationalState); Assert.Equal(donor.Ammunition, current.Ammunition); Assert.Equal(donor.Readiness, current.Readiness);
            Assert.Equal("retained-unimplemented", obligation.Status); Assert.Equal(custody.ReceiptId, obligation.ReceiptId); Assert.Empty(final.World.Relationships);
            if (custody.Kind == "relocate-and-guard")
            {
                var guard = Assert.Single(final.World.Guards); guards++;
                Assert.Equal(donor.Components.Single().CurrentToe, current.Components.Single().CurrentToe + guard.Toe);
                Assert.Equal(donor.OperationalState, guard.OperationalState); Assert.Equal(donor.Ammunition, guard.Ammunition); Assert.Equal(donor.Readiness, guard.Readiness);
                Assert.Equal(donor.ElementId, guard.OriginComponent.Unit.ElementId); Assert.Equal(donor.CurrentLocationId, guard.CurrentLocationId);
                Assert.Equal("guarded", lot.Status); Assert.Equal(guard.GuardId, lot.GuardId); Assert.Equal(guard.CurrentLocationId, lot.CurrentLocationId);
                Assert.Null(lot.EscapeReceiptId); Assert.Empty(final.World.ReplacementEntitlements); Assert.Equal("guard-priority-upkeep", obligation.Kind); Assert.Null(obligation.EligibleScope);
                if (guard.OperationalState.CapabilityPointsExpended.Numerator == 11) cpEleven++;
            }
            else
            {
                var entitlement = Assert.Single(final.World.ReplacementEntitlements); escapes++;
                Assert.Equal(donor.Components, current.Components); Assert.Empty(final.World.Guards); Assert.Equal("escaped", lot.Status); Assert.Null(lot.CurrentLocationId);
                Assert.Equal(lot.OriginalComponent, entitlement.OriginalComponent); Assert.Equal(lot.Quantity, entitlement.Quantity);
                Assert.Equal(12, entitlement.DelayOperationStages); Assert.Equal(new CampaignCombatScope(5, 1, true), entitlement.EligibleScope);
                Assert.Equal("awaiting-eligibility-and-training", entitlement.Status); Assert.Equal("replacement-training-gate", obligation.Kind);
                Assert.Equal(entitlement.EligibleScope, obligation.EligibleScope);
                var victim = prior.World.Elements.Single(e => e.ElementId == lot.OriginalComponent.Unit.ElementId);
                Assert.Equal(victim.Components, final.World.Elements.Single(e => e.ElementId == victim.ElementId).Components);
            }
        }
        Assert.Equal((8, 8, 4), (guards, escapes, cpEleven));
    }

    [Fact]
    public void EveryCutRetryDiscardAndClockFallbackCannotDoubleTransferOrOverrideCustody()
    {
        using var fixture = Fixture();
        foreach (var row in CaptureRows(fixture))
        {
            var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var events = Events(row); var end = End(events);
            for (var cut = 1; cut <= end; cut++)
            {
                var state = Replay(test, inputs[..cut], events[..cut]); var bytes = CampaignCombatResolutionCodec.SerializeState(state);
                for (var i = 0; i < cut; i++)
                {
                    var duplicate = Apply(test, inputs[..cut], events[..cut], inputs[i] with { AdmittedAt = null, ClockAvailable = false });
                    Assert.Equal(CombatStepsDisposition.Duplicate, duplicate.Disposition); Assert.Equal(events[i], duplicate.EventBytes);
                    Assert.Equal(bytes, CampaignCombatResolutionCodec.SerializeState(duplicate.State));
                }
                if (cut < end)
                {
                    var candidate = Apply(test, inputs[..cut], events[..cut], inputs[cut]);
                    Assert.Equal(bytes, CampaignCombatResolutionCodec.SerializeState(Replay(test, inputs[..cut], events[..cut])));
                    Assert.Equal(candidate.EventBytes, Apply(test, inputs[..cut], events[..cut], inputs[cut]).EventBytes);
                }
            }
            var waiting = Replay(test, inputs[..(end - 1)], events[..(end - 1)]); var choice = inputs[end - 1]; var deadline = waiting.Window!.Timing.DeadlineUnixMilliseconds;
            Assert.ThrowsAny<JsonException>(() => Apply(test, inputs[..(end - 1)], events[..(end - 1)], choice with
            {
                Actor = choice.Actor == CampaignOpeningPreambleActor.Axis ? CampaignOpeningPreambleActor.Commonwealth : CampaignOpeningPreambleActor.Axis,
                AdmittedAt = null,
                ClockAvailable = false
            }));
            var expiry = new CombatResolutionInput(choice.Command with { Kind = "expire", Choice = null }, CampaignOpeningPreambleActor.System, deadline - 1);
            Assert.Equal(CombatStepsDisposition.NoOp, Apply(test, inputs[..(end - 1)], events[..(end - 1)], expiry).Disposition);
            Assert.Equal("leave-unguarded", Apply(test, inputs[..(end - 1)], events[..(end - 1)], expiry with { AdmittedAt = deadline }).State.World.Settlements.Single().Custody!.Kind);
            Assert.Equal(CombatStepsDisposition.NoOp, Apply(test, inputs[..end], events[..end], expiry with { AdmittedAt = deadline }).Disposition);
            Assert.ThrowsAny<JsonException>(() => Replay(test, [.. inputs[..end], choice], [.. events[..end], events[end - 1]]));
        }
    }

    [Fact]
    public void RehashedCustodyAndAssetForgeriesCannotReplaceReplayOrTypedProjection()
    {
        using var fixture = Fixture();
        foreach (var row in CaptureRows(fixture).Where((_, index) => index % 4 == 0))
        {
            var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var events = Events(row); var end = End(events);
            var prior = Replay(test, inputs[..(end - 2)], events[..(end - 2)]); var final = Replay(test, inputs[..end], events[..end]);
            foreach (var field in new[] { "predecessorReceiptId", "lotId", "route", "donorToeBefore", "donorToeAfter" })
            {
                var node = JsonNode.Parse(events[end - 1])!; var payload = node["effect"]!["payload"]!;
                payload[field] = field == "route" ? new JsonArray("foreign") : field.StartsWith("donor", StringComparison.Ordinal) ? JsonValue.Create(99) : JsonValue.Create("foreign");
                Assert.ThrowsAny<JsonException>(() => Replay(test, inputs[..end], [.. events[..(end - 1)], Rehash(node)]));
            }
            var bytes = CampaignCombatResolutionCodec.SerializeState(final);
            foreach (var field in new[] { "futureObligations", "guards", "replacementEntitlements" })
            {
                if (JsonNode.Parse(bytes)!["world"]![field]!.AsArray().Count == 0) continue;
                var node = JsonNode.Parse(bytes)!; node["world"]![field] = new JsonArray();
                Assert.ThrowsAny<JsonException>(() => Read(test, Bytes(node), inputs[..end], events[..end]));
            }
            foreach (var field in new[] { "quantity", "originLocationId", "originalComponent" })
            {
                var node = JsonNode.Parse(bytes)!;
                var lot = node["world"]!["custodyLots"]![0]!;
                if (field == "quantity") lot[field] = 1 + lot[field]!.GetValue<int>();
                else if (field == "originalComponent") lot[field]!["unit"]!["elementId"] = "foreign";
                else lot[field] = "foreign";
                Assert.ThrowsAny<JsonException>(() => Read(test, Bytes(node), inputs[..end], events[..end]));
            }
            if (final.World.Guards.Count > 0)
            {
                var node = JsonNode.Parse(bytes)!; node["world"]!["guards"]![0]!["operationalState"]!["capabilityPointsExpended"]!["numerator"] = 0;
                Assert.ThrowsAny<JsonException>(() => Read(test, Bytes(node), inputs[..end], events[..end]));
            }
            else
            {
                var node = JsonNode.Parse(bytes)!; node["world"]!["replacementEntitlements"]![0]!["eligibleScope"]!["gameTurn"] = 6;
                Assert.ThrowsAny<JsonException>(() => Read(test, Bytes(node), inputs[..end], events[..end]));
            }
            var settlement = final.World.Settlements.Single(); var custody = settlement.Custody!;
            Assert.Throws<ArgumentException>(() => settlement.WithResultV2Custody(custody));
            var malformed = new CampaignCombatCustodyReceipt(custody.ReceiptId, custody.PredecessorReceiptId, custody.LotId,
                custody.Kind, ["foreign"], custody.GuardId, custody.EntitlementId, custody.DonorToeBefore, custody.DonorToeAfter);
            var forged = prior.World.Settlements.Single().WithResultV2Custody(malformed);
            Assert.ThrowsAny<JsonException>(() => CampaignCombatLossRetreat.Project(prior.Context, prior.Result!, forged));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(final with { World = prior.World }));
            var waiting = Replay(test, inputs[..(end - 1)], events[..(end - 1)]);
            Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(waiting with { Window = waiting.Window! with { Kind = "retreat" } }));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(final with { Window = waiting.Window }));
        }
    }

    [Fact]
    public void CustodyRawBoundsTypesAndOwnershipRemainStrict()
    {
        using var fixture = Fixture();
        foreach (var row in CaptureRows(fixture).Where((_, index) => index % 4 == 0))
        {
            var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var events = Events(row); var end = End(events);
            var final = Replay(test, inputs[..end], events[..end]); var bytes = CampaignCombatResolutionCodec.SerializeState(final);
            CombatResolutionState SentryRead(byte[] raw) => CampaignCombatResolutionCodec.ReadState(raw, null!, test.Created, test.Boundary,
                test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, new Sentry<CombatResolutionInput>(), new Sentry<byte[]>());
            Assert.Throws<InvalidOperationException>(() => SentryRead(bytes));
            foreach (var field in new[] { "route", "donorToeBefore", "donorToeAfter", "guardId", "entitlementId" })
            {
                var node = JsonNode.Parse(bytes)!; node["world"]!["settlements"]![0]!["custody"]![field] = false;
                Assert.ThrowsAny<JsonException>(() => SentryRead(Bytes(node)));
            }
            var over = JsonNode.Parse(bytes)!; over["world"]!["futureObligations"] = new JsonArray(Enumerable.Range(0, 513).Select(_ => (JsonNode?)null).ToArray());
            Assert.ThrowsAny<JsonException>(() => SentryRead(Bytes(over)));
            Assert.ThrowsAny<JsonException>(() => SentryRead(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes) + "\n")));
            if (final.World.Guards.Count > 0)
            {
                var node = JsonNode.Parse(bytes)!; node["world"]!["guards"]![0]!["ammunition"]!["initialAmmunitionOrigin"]!["kind"] = 0;
                Assert.ThrowsAny<JsonException>(() => SentryRead(Bytes(node)));
            }
            else
            {
                var node = JsonNode.Parse(bytes)!; node["world"]!["replacementEntitlements"]![0]!["eligibleScope"]!["gameTurn"] = 116;
                Assert.ThrowsAny<JsonException>(() => SentryRead(Bytes(node)));
            }
            Assert.Throws<NotSupportedException>(() => ((IList<CampaignCombatFutureObligation>)final.World.FutureObligations).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<CampaignCombatGuardAsset>)final.World.Guards).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<CampaignCombatReplacementEntitlement>)final.World.ReplacementEntitlements).Clear());
            var result = Apply(test, inputs[..(end - 1)], events[..(end - 1)], inputs[end - 1]); var copy = result.EventBytes!; copy[0] = 0;
            Assert.Equal(events[end - 1], result.EventBytes); Assert.Equal(bytes, CampaignCombatResolutionCodec.SerializeState(result.State));
        }
    }
    private sealed class Sentry<T> : IReadOnlyList<T>
    {
        public int Count => throw new InvalidOperationException("Trusted context accessed.");
        public T this[int index] => throw new InvalidOperationException();
        public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());
    private static byte[] Rehash(JsonNode root)
    {
        root.AsObject().Remove("receiptId"); root["receiptId"] = "cmb." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.result-receipt.v2", Bytes(root))[7..]; return Bytes(root);
    }
    private sealed record Prefix(CombatResolutionState State, CombatResolutionInput[] Inputs, byte[][] Events);
    private static Prefix BeforeCustody(TestCase test, CombatResolutionInput[] original, long acceptedAt)
    {
        var inputs = new List<CombatResolutionInput>(); var events = new List<byte[]>(); CombatResolutionState? state = null;
        for (var i = 0; i < 5; i++)
        {
            var input = i == 2 ? original[i] with { AdmittedAt = acceptedAt } : original[i];
            var result = Apply(test, inputs.ToArray(), events.ToArray(), input); inputs.Add(input); events.Add(result.EventBytes!); state = result.State;
        }
        return new(state!, inputs.ToArray(), events.ToArray());
    }
    private static (string Kind, string Reason, CampaignWorldSnapshotV7? World, ulong Cursor) Outcome(TestCase test,
        CombatResolutionInput[] inputs, byte[][] events, CombatResolutionInput input)
    {
        try
        {
            var result = Apply(test, inputs, events, input); using var parsed = JsonDocument.Parse(result.EventBytes!);
            var effect = parsed.RootElement.GetProperty("effect");
            return (effect.GetProperty("kind").GetString()!, effect.GetProperty("reason").GetString()!, result.State.World, result.State.RandomState.NextByteCursor);
        }
        catch (JsonException) { return ("rejected", "", null, 0); }
    }
    private static IEnumerable<JsonElement> CaptureRows(JsonDocument fixture) => fixture.RootElement.GetProperty("traces").EnumerateArray()
        .Where(r => r.GetProperty("name").GetString()!.Contains("capture", StringComparison.Ordinal));
    private static int End(byte[][] events) => Array.FindIndex(events, e => JsonNode.Parse(e)!["effect"]!["kind"]!.GetValue<string>() == "relationships-settled");
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
