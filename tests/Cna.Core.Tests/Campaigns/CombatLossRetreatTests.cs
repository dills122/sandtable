using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using static Cna.Core.Tests.Campaigns.CombatSealsTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatLossRetreatTests
{
    [Fact]
    public void AllRetreatPrefixesMatch144LiteralEventsAnd176StateHashCuts()
    {
        using var fixture = Fixture(); var events = 0; var cuts = 0;
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
            var final = Replay(test, inputs[..end], literals[..end]); Assert.Equal("retreat", final.Status); Assert.Null(final.Window);
            Assert.Null(final.World.Settlements.Single().Custody); Assert.Null(final.World.Settlements.Single().Relationships);
            for (var i = end; i < inputs.Length; i++) Assert.ThrowsAny<JsonException>(() => Apply(test, inputs[..end], literals[..end], inputs[i]));
        }
        Assert.Equal(144, events); Assert.Equal(176, cuts);
    }
    [Fact]
    public void IndependentRetreatOpeningAndLocalClockMatrixPreserveAcceptedIntent()
    {
        using var fixture = Fixture(); var openings = 0; var choices = 0;
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray().Where(r => r.GetProperty("name").GetString()!.StartsWith("zero-retreat.", StringComparison.Ordinal)))
        {
            var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var events = Events(row);
            foreach (var (time, available) in new (long?, bool)[] { (null, true), (1000, false), (CampaignCombatSelectionSteps.UtcMaximum, true), (0, true), (1000, true) })
            {
                var input = inputs[1] with { AdmittedAt = time, ClockAvailable = available };
                var result = Apply(test, inputs[..1], events[..1], input); openings++;
                if (!available || time is null || time == CampaignCombatSelectionSteps.UtcMaximum)
                {
                    Assert.Equal("disposition", result.State.Status); Assert.Null(result.State.Window);
                    Assert.Equal("refuse-retreat", result.State.World.Settlements.Single().Disposition!.Kind);
                    using var effect = JsonDocument.Parse(result.EventBytes!); Assert.Equal(JsonValueKind.Null, effect.RootElement.GetProperty("effect").GetProperty("timing").ValueKind);
                    Assert.Equal("opening-clock-unavailable", effect.RootElement.GetProperty("effect").GetProperty("reason").GetString());
                }
                else
                {
                    Assert.Equal("waiting-retreat", result.State.Status); Assert.Equal(time, result.State.Window!.Timing.OpenedAtUnixMilliseconds);
                    Assert.Equal(3000, result.State.AcceptedHighWater); Assert.Equal(time, result.State.Window.Timing.HighWaterUnixMilliseconds);
                    Assert.Equal(test.Request.Context.Configuration.Hash, result.State.Window.Timing.ConfigHash);
                }
            }
            var opening = inputs[1] with { AdmittedAt = 1000 }; var opened = Apply(test, inputs[..1], events[..1], opening);
            var prefixInputs = new[] { inputs[0], opening }; var prefixEvents = new[] { events[0], opened.EventBytes! };
            var window = opened.State.Window!; var deadline = window.Timing.DeadlineUnixMilliseconds;
            Assert.Equal(1000 + test.Request.Context.Configuration.Windows.Single(w => w.Kind == "retreat").DecisionBudgetMilliseconds, deadline);
            foreach (var time in new long?[] { null, 0, 999, 1000, deadline - 1, deadline, deadline + 1 })
                foreach (var available in new[] { true, false })
                {
                    var input = inputs[2] with { AdmittedAt = time, ClockAvailable = available, Command = inputs[2].Command with { Choice = "retreat" } };
                    if (available && time >= deadline) Assert.ThrowsAny<JsonException>(() => Apply(test, prefixInputs, prefixEvents, input));
                    else
                    {
                        var result = Apply(test, prefixInputs, prefixEvents, input);
                        var lost = !available || time is null || time < 1000;
                        Assert.Equal(lost ? "refuse-retreat" : "retreat", result.State.World.Settlements.Single().Disposition!.Kind);
                        using var e = JsonDocument.Parse(result.EventBytes!);
                        Assert.Equal(lost ? "system" : CampaignCombatSealedRoundCodec.Actor(input.Actor), e.RootElement.GetProperty("author").GetString());
                        var retry = Apply(test, [.. prefixInputs, input], [.. prefixEvents, result.EventBytes!], input with { AdmittedAt = null, ClockAvailable = false });
                        Assert.Equal(CombatStepsDisposition.Duplicate, retry.Disposition); Assert.Equal(result.EventBytes, retry.EventBytes);
                        foreach (var kind in new[] { "expire", "unavailable" })
                            Assert.Equal(CombatStepsDisposition.NoOp, Apply(test, [.. prefixInputs, input], [.. prefixEvents, result.EventBytes!],
                                new(input.Command with { Kind = kind, Choice = null }, CampaignOpeningPreambleActor.System, deadline)).Disposition);
                    }
                    choices++;
                }
            var wrongOwner = inputs[2] with { Actor = inputs[2].Actor == CampaignOpeningPreambleActor.Axis ? CampaignOpeningPreambleActor.Commonwealth : CampaignOpeningPreambleActor.Axis, AdmittedAt = null, ClockAvailable = false };
            Assert.ThrowsAny<JsonException>(() => Apply(test, prefixInputs, prefixEvents, wrongOwner));
            Assert.ThrowsAny<JsonException>(() => Apply(test, prefixInputs, prefixEvents, inputs[2] with { Command = inputs[2].Command with { Choice = "foreign" }, AdmittedAt = null }));
            var expiry = new CombatResolutionInput(inputs[2].Command with { Kind = "expire", Choice = null }, CampaignOpeningPreambleActor.System, deadline - 1);
            Assert.Equal(CombatStepsDisposition.NoOp, Apply(test, prefixInputs, prefixEvents, expiry).Disposition);
            Assert.Equal("refuse-retreat", Apply(test, prefixInputs, prefixEvents, expiry with { AdmittedAt = deadline }).State.World.Settlements.Single().Disposition!.Kind);
            var unavailable = expiry with { Command = expiry.Command with { Kind = "unavailable" } };
            using var unavailableEvent = JsonDocument.Parse(Apply(test, prefixInputs, prefixEvents, unavailable).EventBytes!);
            Assert.Equal("controller-unavailable", unavailableEvent.RootElement.GetProperty("effect").GetProperty("reason").GetString());
        }
        Assert.Equal(20, openings); Assert.Equal(56, choices);
    }

    [Fact]
    public void JointLossCapturesAndActualRetreatConserveOriginalPaidParticipants()
    {
        using var fixture = Fixture(); var positiveCaptures = 0; var cpLimit = 0; var zeroRetreat = 0; var refusals = 0;
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray())
        {
            var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var events = Events(row); var end = End(events);
            var final = Replay(test, inputs[..end], events[..end]); var settlement = final.World.Settlements.Single(); var paid = final.Context.Committed.World;
            foreach (var loss in settlement.Losses!.Roles)
            {
                Assert.Equal(10, loss.RemainingToe + loss.LossToe); Assert.Equal(loss.LossToe, loss.CapturedToe + loss.OtherLossToe);
                Assert.Equal(loss.LossToe >= 3 ? 3 : 0, loss.LossDp);
                Assert.Equal(loss.RemainingToe, final.World.Elements.Single(e => e.ElementId == loss.Component.Unit.ElementId).Components.Single().CurrentToe);
            }
            var retreat = settlement.Retreat!; var defender = final.World.Elements.Single(e => e.ElementId == settlement.Defender.ElementId);
            Assert.Equal(retreat.AfterCp, defender.OperationalState.CapabilityPointsExpended); Assert.Equal(retreat.Route[^1], defender.CurrentLocationId);
            Assert.Equal(defender.CurrentLocationId, final.World.Representations.Single(r => r.BoundElementIds.Contains(defender.ElementId)).CurrentLocationId);
            Assert.All(final.World.Elements, e => Assert.Equal(0, e.Ammunition.Points));
            Assert.Equal(final.Result!.AfterRandomState, final.RandomState); Assert.Equal(paid.Elements, settlement.PreLossElements);
            if (settlement.Losses.Roles.Any(r => r.CapturedToe > 0))
            {
                var captured = settlement.Losses.Roles.Single(r => r.CapturedToe > 0); var lot = Assert.Single(final.World.CustodyLots);
                Assert.Equal(captured.CapturedToe, lot.Quantity); Assert.Equal(captured.Component, lot.OriginalComponent); Assert.Equal("pending", lot.Status);
                Assert.Equal(paid.Elements.Single(e => e.ElementId == captured.Component.Unit.ElementId).CurrentLocationId, lot.OriginLocationId);
                Assert.Equal(lot.OriginLocationId, lot.CurrentLocationId); positiveCaptures++;
            }
            var name = row.GetProperty("name").GetString()!;
            if (name.StartsWith("attacker-capture-guard-cp-limit.", StringComparison.Ordinal))
            {
                Assert.Equal(10, retreat.BeforeCp.Numerator); Assert.Equal(11, retreat.AfterCp.Numerator); Assert.Equal(1, retreat.ExcessCpDp); cpLimit++;
            }
            if (name.StartsWith("zero-retreat.", StringComparison.Ordinal))
            {
                Assert.All(settlement.Losses.Roles, loss => Assert.Equal(0, loss.LossToe)); Assert.Equal(1, retreat.CompletedDistance);
                Assert.Equal(3, retreat.AttackerVictoryRp); Assert.Equal(3, final.World.Elements.Single(e => e.ElementId == settlement.Attacker.ElementId).OperationalState.CohesionLevel); zeroRetreat++;
            }
            if (name.StartsWith("refusal-loss-dp.", StringComparison.Ordinal))
            {
                Assert.Equal(10, settlement.Losses.Roles.Single(l => l.Role == "defender").RefusalPercent); Assert.Equal(0, retreat.AttackerVictoryRp);
                Assert.Equal(0, retreat.CompletedDistance); Assert.Equal(retreat.BeforeCp, retreat.AfterCp); refusals++;
            }
        }
        Assert.Equal((16, 4, 4, 4), (positiveCaptures, cpLimit, zeroRetreat, refusals));
    }

    [Fact]
    public void EveryRetainedCutRecoversRetriesAndDiscardedCandidatesWithoutDoubleEffects()
    {
        using var fixture = Fixture();
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray())
        {
            var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var events = Events(row); var end = End(events);
            for (var cut = 1; cut <= end; cut++)
            {
                var prior = Replay(test, inputs[..cut], events[..cut]); var bytes = CampaignCombatResolutionCodec.SerializeState(prior);
                for (var i = 0; i < cut; i++)
                {
                    var retry = Apply(test, inputs[..cut], events[..cut], inputs[i] with { AdmittedAt = null, ClockAvailable = false });
                    Assert.Equal(CombatStepsDisposition.Duplicate, retry.Disposition); Assert.Equal(events[i], retry.EventBytes);
                    Assert.Equal(bytes, CampaignCombatResolutionCodec.SerializeState(retry.State));
                }
                if (cut < end)
                {
                    var discarded = Apply(test, inputs[..cut], events[..cut], inputs[cut]);
                    Assert.Equal(bytes, CampaignCombatResolutionCodec.SerializeState(prior)); Assert.Equal(bytes, CampaignCombatResolutionCodec.SerializeState(Replay(test, inputs[..cut], events[..cut])));
                    Assert.Equal(discarded.EventBytes, Apply(test, inputs[..cut], events[..cut], inputs[cut]).EventBytes);
                }
            }
            Assert.ThrowsAny<JsonException>(() => Replay(test, [.. inputs[..end], inputs[end - 1]], [.. events[..end], events[end - 1]]));
        }
    }

    [Fact]
    public void RehashedReceiptsRoutesAndWorldCannotReplaceCausalSettlement()
    {
        using var fixture = Fixture();
        foreach (var row in fixture.RootElement.GetProperty("traces").EnumerateArray().Where((_, index) => index % 4 == 0))
        {
            var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var events = Events(row); var end = End(events);
            for (var index = 1; index < end; index++)
            {
                var node = JsonNode.Parse(events[index])!; var effect = node["effect"]!; var kind = effect["kind"]!.GetValue<string>();
                if (kind == "choice-opened") effect["window"]!["owner"] = effect["window"]!["owner"]!.GetValue<string>() == "axis" ? "commonwealth" : "axis";
                else if (kind == "disposition-recorded") effect["payload"]!["route"] = new JsonArray("foreign.a", "foreign.b");
                else if (kind == "losses-settled") effect["payload"]!["roles"]![0]!["remainingToe"] = 1;
                else effect["payload"]!["attackerVictoryRp"] = 9;
                Assert.ThrowsAny<JsonException>(() => Replay(test, inputs[..(index + 1)], [.. events[..index], Rehash(node)]));
                var predecessor = JsonNode.Parse(events[index])!;
                if (predecessor["effect"]!["payload"] is { } payload)
                {
                    payload["predecessorReceiptId"] = "foreign";
                    Assert.ThrowsAny<JsonException>(() => Replay(test, inputs[..(index + 1)], [.. events[..index], Rehash(predecessor)]));
                }
            }
            var state = Replay(test, inputs[..end], events[..end]); var bytes = CampaignCombatResolutionCodec.SerializeState(state);
            foreach (var field in new[] { "commitmentId", "resultId", "settlementId" })
            {
                var node = JsonNode.Parse(bytes)!; node["world"]!["settlements"]![0]![field] = "foreign";
                Assert.ThrowsAny<JsonException>(() => Read(test, Bytes(node), inputs[..end], events[..end]));
            }
            var root = JsonNode.Parse(bytes)!; root["world"]!["elements"]![0]!["operationalState"]!["cohesionLevel"] = 10;
            Assert.ThrowsAny<JsonException>(() => Read(test, Bytes(root), inputs[..end], events[..end]));
            root = JsonNode.Parse(bytes)!; root["randomState"]!["nextByteCursor"] = 0;
            Assert.ThrowsAny<JsonException>(() => Read(test, Bytes(root), inputs[..end], events[..end]));
            if (state.World.CustodyLots.Count > 0)
            {
                root = JsonNode.Parse(bytes)!; root["world"]!["custodyLots"]![0]!["quantity"] = 9;
                Assert.ThrowsAny<JsonException>(() => Read(test, Bytes(root), inputs[..end], events[..end]));
            }
            Assert.ThrowsAny<JsonException>(() => Replay(test, inputs[..end].Select((i, n) => n == end - 1 ? i with { AdmittedAt = 1 } : i).ToArray(), events[..end]));
        }
    }

    [Fact]
    public void HashedAppendMethodsRetainIdentityStageAndTypedWorldAuthority()
    {
        using var fixture = Fixture();
        var row = fixture.RootElement.GetProperty("traces").EnumerateArray().First(r => r.GetProperty("name").GetString()!.StartsWith("zero-retreat.", StringComparison.Ordinal));
        var test = CombatResolutionTests.Case(row); var inputs = Inputs(row); var events = Events(row); var end = End(events);
        var resolved = Replay(test, inputs[..1], events[..1]); var final = Replay(test, inputs[..end], events[..end]);
        var basis = resolved.World.Settlements.Single(); var settled = final.World.Settlements.Single();
        Assert.Throws<ArgumentException>(() => basis.WithResultV2Losses(settled.Losses!));
        Assert.Throws<ArgumentException>(() => basis.WithResultV2Retreat(settled.Retreat!));
        var disposition = basis.WithResultV2Disposition(settled.Disposition!);
        Assert.Throws<ArgumentException>(() => disposition.WithResultV2Disposition(settled.Disposition!));
        var losses = disposition.WithResultV2Losses(settled.Losses!); var retreat = losses.WithResultV2Retreat(settled.Retreat!);
        Assert.Equal(settled, retreat); Assert.Throws<ArgumentException>(() => retreat.WithResultV2Retreat(settled.Retreat!));
        var legacy = new CampaignCombatSettlementState("legacy", "legacy.commit", "legacy.result", basis.GameTurn, basis.OperationStage,
            basis.Attacker, basis.Defender, basis.PreLossElements, basis.Result, null, null, null, null, null);
        var legacyDisposition = new CampaignCombatRetreatDisposition("legacy.disposition", "legacy.result", "retreat", 1, 1, 0, settled.Disposition!.Route);
        Assert.Throws<ArgumentException>(() => legacy.WithResultV2Disposition(legacyDisposition));
        Assert.Throws<ArgumentException>(() => basis.WithResultV2Disposition(new("foreign", basis.ResultId, "retreat", 1, 1, 0, settled.Disposition.Route)));
        var forged = basis.WithResultV2Disposition(new(basis.SettlementId + ".disposition", basis.ResultId, "retreat", 1, 1, 0, ["foreign.a", "foreign.b"]));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatLossRetreat.Project(resolved.Context, resolved.Result!, forged));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(final with { World = resolved.World }));
        for (var cut = 3; cut < end; cut++)
        {
            var intermediate = Replay(test, inputs[..cut], events[..cut]);
            Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(intermediate with { World = resolved.World }));
        }
        var waiting = Replay(test, inputs[..2], events[..2]);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(waiting with { Window = null }));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatResolutionCodec.SerializeState(resolved with { Window = waiting.Window }));
        var wrongStatus = JsonNode.Parse(CampaignCombatResolutionCodec.SerializeState(final))!; wrongStatus["status"] = "resolved";
        Assert.ThrowsAny<JsonException>(() => Read(test, Bytes(wrongStatus), inputs[..end], events[..end]));
        Assert.Equal(resolved.Context.CommittedHash, final.Context.CommittedHash);
        Assert.Equal(CampaignCombatSealedRoundCodec.SerializeState(resolved.Context.Committed), CampaignCombatSealedRoundCodec.SerializeState(final.Context.Committed));
    }

    [Fact]
    public void RawNewPayloadsRejectBeforeContextAndReturnedSettlementCollectionsStayOwned()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("traces")[0]; var test = CombatResolutionTests.Case(row);
        var inputs = Inputs(row); var events = Events(row); var end = End(events); var state = Replay(test, inputs[..end], events[..end]);
        var bytes = CampaignCombatResolutionCodec.SerializeState(state);
        CombatResolutionState SentryRead(byte[] raw) => CampaignCombatResolutionCodec.ReadState(raw, null!, test.Created, test.Boundary,
            test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, new Sentry<CombatResolutionInput>(), new Sentry<byte[]>());
        Assert.Throws<InvalidOperationException>(() => SentryRead(bytes));
        foreach (var field in new[] { "completedDistance", "beforeCp", "afterCp", "excessCpDp", "attackerVictoryRp", "route" })
        {
            var bad = JsonNode.Parse(bytes)!; bad["world"]!["settlements"]![0]!["retreat"]![field] = false;
            Assert.ThrowsAny<JsonException>(() => SentryRead(Bytes(bad)));
        }
        var badLoss = JsonNode.Parse(bytes)!; badLoss["world"]!["settlements"]![0]!["losses"]!["roles"]![0]!["committedToe"] = false;
        Assert.ThrowsAny<JsonException>(() => SentryRead(Bytes(badLoss)));
        var oversized = JsonNode.Parse(bytes)!; oversized["world"]!["settlements"]![0]!["retreat"]!["route"] = new JsonArray(Enumerable.Range(0, 513).Select(_ => (JsonNode?)JsonValue.Create("x")).ToArray());
        Assert.ThrowsAny<JsonException>(() => SentryRead(Bytes(oversized)));
        Assert.ThrowsAny<JsonException>(() => SentryRead(Encoding.UTF8.GetBytes(" " + Encoding.UTF8.GetString(bytes))));
        var result = Apply(test, inputs[..(end - 1)], events[..(end - 1)], inputs[end - 1]); var returned = result.EventBytes!; returned[0] = 0;
        Assert.Equal(events[end - 1], result.EventBytes);
        var settlement = result.State.World.Settlements.Single();
        Assert.Throws<NotSupportedException>(() => ((IList<string>)settlement.Disposition!.Route).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<CampaignCombatRoleLoss>)settlement.Losses!.Roles).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<CampaignCombatCohesionCause>)result.State.World.CohesionCauses).Clear());
        Assert.Equal(bytes, CampaignCombatResolutionCodec.SerializeState(result.State));
    }
    private sealed class Sentry<T> : IReadOnlyList<T>
    {
        public int Count => throw new InvalidOperationException("Trusted history touched.");
        public T this[int index] => throw new InvalidOperationException();
        public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());
    private static byte[] Rehash(JsonNode root)
    {
        root.AsObject().Remove("receiptId"); root["receiptId"] = "cmb." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.result-receipt.v2", Bytes(root))[7..]; return Bytes(root);
    }

    private static int End(byte[][] events) => Array.FindIndex(events, e => JsonNode.Parse(e)!["effect"]!["kind"]!.GetValue<string>() == "retreat-settled") + 1;
    private static CombatResolutionState Replay(TestCase test, CombatResolutionInput[] inputs, byte[][] events) => CampaignCombatResolution.ReplayTrustedBoundary(
        test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events);
    private static CombatResolutionResult Apply(TestCase test, CombatResolutionInput[] inputs, byte[][] events, CombatResolutionInput input) => CampaignCombatResolution.ApplyTrustedBoundary(
        test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events, input);
    private static CombatResolutionState Read(TestCase test, byte[] bytes, CombatResolutionInput[] inputs, byte[][] events) => CampaignCombatResolutionCodec.ReadState(
        bytes, test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, test.Events, inputs, events);
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-result-settlement-v2.json")));
    private static CombatResolutionInput[] Inputs(JsonElement row) => row.GetProperty("resultInputs").EnumerateArray().Select(i => CampaignCombatResolutionCodec.ReadInput(JsonSerializer.SerializeToUtf8Bytes(i))).ToArray();
    private static byte[][] Events(JsonElement row) => row.GetProperty("resultEventCanonicalUtf8").EnumerateArray().Select(e => Encoding.UTF8.GetBytes(e.GetString()!)).ToArray();
}
