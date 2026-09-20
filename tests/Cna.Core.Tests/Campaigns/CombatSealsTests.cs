using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatSealsTests
{
    [Fact]
    public void AllTenBasesFiftyFourEventsAndSixtyFourStatesMatchFrozenPrecommitLiterals()
    {
        using var fixture = Fixture();
        var eventCount = 0; var cuts = 0; var excluded = 0; var bases = 0;
        foreach (var row in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var test = Case(row);
            var basisBytes = Encoding.UTF8.GetBytes(row.GetProperty("baseCanonicalUtf8").GetString()!);
            var basis = CampaignCombatSealedRoundCodec.ReadBase(basisBytes, test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents);
            bases++;
            Assert.Equal(basisBytes, CampaignCombatSealedRoundCodec.SerializeBase(basis));
            Assert.Equal(row.GetProperty("clockConfigurationHash").GetString(), basis.ConfigurationHash);
            for (var cut = 0; cut <= test.Count; cut++)
            {
                var state = Replay(test, cut);
                var literal = Encoding.UTF8.GetBytes(row.GetProperty("stateCanonicalUtf8")[cut].GetString()!);
                Assert.Equal(literal, CampaignCombatSealedRoundCodec.SerializeState(state));
                Assert.Equal(literal, CampaignCombatSealedRoundCodec.SerializeState(CampaignCombatSealedRoundCodec.ReadState(literal,
                    test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs[..cut], test.Events[..cut])));
                if (cut < test.Count)
                {
                    var result = Apply(test, cut, test.Inputs[cut]);
                    Assert.Equal(CombatStepsDisposition.Accepted, result.Disposition);
                    Assert.Equal(test.Events[cut], result.EventBytes);
                    eventCount++;
                }
                cuts++;
            }
            if (test.Count < test.Events.Length)
            {
                Assert.ThrowsAny<JsonException>(() => Apply(test, test.Count, test.Inputs[test.Count]));
                Assert.ThrowsAny<JsonException>(() => ReadState(test, test.Events.Length,
                    Encoding.UTF8.GetBytes(row.GetProperty("stateCanonicalUtf8")[test.Events.Length].GetString()!)));
                excluded++;
            }
        }
        Assert.Equal(10, bases); Assert.Equal(54, eventCount); Assert.Equal(64, cuts); Assert.Equal(4, excluded);
    }

    [Fact]
    public void BothSidesAndSealOrdersMatch192FreshClockOutcomesAnd96Retries()
    {
        using var fixture = Fixture();
        var outcomes = 0; var retries = 0;
        foreach (var row in fixture.RootElement.GetProperty("cases").EnumerateArray().Where(r => r.GetProperty("name").GetString()!.EndsWith(".committed", StringComparison.Ordinal)))
        {
            var test = Case(row); var empty = Replay(test, 1); var one = Replay(test, 2);
            var waiting = test.Inputs[2];
            Assert.Equal(Witness(empty, waiting.Command.SlotId!), Witness(one, waiting.Command.SlotId!));
            foreach (var clock in fixture.RootElement.GetProperty("clockOutcomes").EnumerateArray())
            {
                var proposal = waiting with
                {
                    AdmittedAt = clock.GetProperty("admittedAt").ValueKind == JsonValueKind.Null ? null : clock.GetProperty("admittedAt").GetInt64(),
                    ClockAvailable = clock.GetProperty("clockAvailable").GetBoolean()
                };
                foreach (var cut in new[] { 1, 2 })
                {
                    var expected = clock.GetProperty("outcome").GetString();
                    if (expected == "rejected") Assert.ThrowsAny<JsonException>(() => Apply(test, cut, proposal));
                    else
                    {
                        var result = Apply(test, cut, proposal);
                        Assert.Equal(CombatStepsDisposition.Accepted, result.Disposition);
                        Assert.Equal(expected == "cancelled", result.State.Status == "cancelled");
                        Assert.Equal(empty.Timing, result.State.Timing);
                        Assert.Equal(proposal.Actor, result.State.Receipts[^1].Actor);
                        using var ev = JsonDocument.Parse(result.EventBytes!);
                        Assert.Equal(expected == "cancelled" ? "system" : CampaignCombatSealedRoundCodec.Actor(proposal.Actor), ev.RootElement.GetProperty("author").GetString());
                        if (expected != "cancelled") Assert.Equal(proposal.AdmittedAt, result.State.Slots.Single(slot => slot.SlotId == proposal.Command.SlotId).SealedAt);
                    }
                    outcomes++;
                }
                var duplicate = Apply(test, 2, test.Inputs[1] with { AdmittedAt = proposal.AdmittedAt, ClockAvailable = proposal.ClockAvailable }, false);
                Assert.Equal(CombatStepsDisposition.Duplicate, duplicate.Disposition);
                Assert.Equal(test.Events[1], duplicate.EventBytes);
                Assert.Equal(CampaignCombatSealedRoundCodec.SerializeState(one), CampaignCombatSealedRoundCodec.SerializeState(duplicate.State));
                retries++;
            }
        }
        Assert.Equal(192, outcomes); Assert.Equal(96, retries);
    }

    [Fact]
    public void EveryRetainedCutRecoversExactCommandsAndStructuralSuffixWithAdmissionDisabled()
    {
        using var fixture = Fixture();
        foreach (var row in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var test = Case(row);
            Assert.ThrowsAny<JsonException>(() => Apply(test, 0, test.Inputs[0], false));
            for (var cut = 1; cut <= test.Count; cut++)
            {
                var prior = Replay(test, cut); var before = CampaignCombatSealedRoundCodec.SerializeState(prior);
                for (var index = 0; index < cut; index++)
                {
                    var duplicate = Apply(test, cut, test.Inputs[index] with { AdmittedAt = null, ClockAvailable = false }, false);
                    Assert.Equal(CombatStepsDisposition.Duplicate, duplicate.Disposition);
                    Assert.Equal(test.Events[index], duplicate.EventBytes);
                    Assert.Equal(before, CampaignCombatSealedRoundCodec.SerializeState(duplicate.State));
                }
                for (var next = cut; next < test.Count; next++) Assert.Equal(test.Events[next], Apply(test, next, test.Inputs[next], false).EventBytes);
                if (prior.Status is "prepared" or "cancelled")
                    foreach (var kind in new[] { "expire-round", "controller-unavailable" })
                    {
                        var callback = System(prior, kind, 33000);
                        var hash = CampaignOpeningPreambleCodec.Hash(CampaignCombatSealedRoundCodec.SerializeCommand(callback.Command));
                        var expected = prior.Receipts.Any(receipt => receipt.CommandHash == hash) ? CombatStepsDisposition.Duplicate : CombatStepsDisposition.NoOp;
                        Assert.Equal(expected, Apply(test, cut, callback, false).Disposition);
                        Assert.Equal(CombatStepsDisposition.NoOp, Apply(test, cut, callback with { Command = callback.Command with { RoundId = "foreign.round" } }, false).Disposition);
                    }
            }
            var end = Replay(test, test.Count);
            Assert.Equal(test.Count < test.Events.Length ? "prepared" : "cancelled", end.Status);
            Assert.Equal(test.Count < test.Events.Length ? 5 : 6, end.StepIndex);
            Assert.ThrowsAny<JsonException>(() => Apply(test, test.Count, System(end, "complete-step")));
            var changedChoice = test.Inputs.FirstOrDefault(input => input.Command.Kind == "seal-choice");
            if (changedChoice is not null)
                Assert.ThrowsAny<JsonException>(() => Apply(test, test.Count, changedChoice with
                {
                    Command = changedChoice.Command with { Allocation = changedChoice.Command.Allocation! with { CommittedToe = 9 } },
                    AdmittedAt = null,
                    ClockAvailable = false,
                }));
            using var bytes = JsonDocument.Parse(CampaignCombatSealedRoundCodec.SerializeState(end));
            Assert.Empty(bytes.RootElement.GetProperty("attackHistory").EnumerateArray());
            Assert.Empty(bytes.RootElement.GetProperty("targetUses").EnumerateArray());
            Assert.Equal(JsonValueKind.Null, bytes.RootElement.GetProperty("commitmentId").ValueKind);
            Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(row.GetProperty("predecessor").GetProperty("boundary").GetProperty("world")),
                JsonSerializer.SerializeToUtf8Bytes(bytes.RootElement.GetProperty("world")));
            Assert.Equal(JsonSerializer.SerializeToUtf8Bytes(row.GetProperty("predecessor").GetProperty("boundary").GetProperty("randomState")),
                JsonSerializer.SerializeToUtf8Bytes(bytes.RootElement.GetProperty("randomState")));
        }
    }

    [Fact]
    public void OpeningClockIsIndependentAndInvalidSlotCannotTriggerCancellation()
    {
        using var fixture = Fixture(); var test = Case(fixture.RootElement.GetProperty("cases")[0]);
        var opened = Apply(test, 0, test.Inputs[0] with { AdmittedAt = 1999 });
        Assert.Equal(1999, opened.State.Timing!.OpeningFloorUnixMilliseconds);
        Assert.Equal(31999, opened.State.Timing.DeadlineUnixMilliseconds);
        foreach (var bad in new[] { test.Inputs[0] with { AdmittedAt = null }, test.Inputs[0] with { ClockAvailable = false },
            test.Inputs[0] with { AdmittedAt = CampaignCombatSelectionSteps.UtcMaximum } })
            Assert.ThrowsAny<JsonException>(() => Apply(test, 0, bad));
        var prepared = Replay(test, 3);
        Assert.ThrowsAny<JsonException>(() => Apply(test, 3, System(prepared, "complete-step", 4000)));
        Assert.ThrowsAny<JsonException>(() => Apply(test, 3, System(prepared, "complete-step") with { ClockAvailable = false }));
        Assert.ThrowsAny<JsonException>(() => Apply(test, 3, System(prepared, "complete-step") with { Command = System(prepared, "complete-step").Command with { ExpectedPriorVersion = 1 } }));
        foreach (var metadata in new[] { (Time: (long?)null, Available: true), (Time: (long?)2999, Available: true), (Time: (long?)3000, Available: false) })
            Assert.Equal("cancelled", Apply(test, 1, System(Replay(test, 1), "expire-round", metadata.Time) with { ClockAvailable = metadata.Available }).State.Status);
        var input = test.Inputs[2]; var command = input.Command;
        foreach (var bad in new[]
        {
            input with { Actor = test.Inputs[1].Actor }, input with { Actor = CampaignOpeningPreambleActor.System },
            input with { Command = command with { RoundId = "foreign.round" } }, input with { Command = command with { SlotId = "foreign.slot" } },
            input with { Command = command with { SegmentId = "foreign.segment" } }, input with { Command = command with { ClockConfigurationHash = ForeignHash } },
            input with { Command = command with { ContractVersion = 1 } }, input with { Command = command with { ExpectedPriorVersion = 29 } },
            input with { Command = command with { Allocation = command.Allocation! with { CommittedToe = 9 } } }
        })
            foreach (var cut in new[] { 1, 2 })
                Assert.ThrowsAny<JsonException>(() => Apply(test, cut, bad with { AdmittedAt = null, ClockAvailable = false }));
        Assert.ThrowsAny<JsonException>(() => Apply(test, 2, test.Inputs[1] with { Actor = test.Inputs[2].Actor, AdmittedAt = null, ClockAvailable = false }));
        Assert.ThrowsAny<JsonException>(() => Apply(test, 2, test.Inputs[1] with { Command = test.Inputs[1].Command with { Allocation = test.Inputs[1].Command.Allocation! with { CommittedToe = 9 } }, AdmittedAt = null, ClockAvailable = false }));
        Assert.Equal(CombatStepsDisposition.NoOp, Apply(test, 1, System(Replay(test, 1), "expire-round", 32999)).Disposition);
        Assert.Equal("cancelled", Apply(test, 1, System(Replay(test, 1), "expire-round", 33000)).State.Status);
        Assert.Equal(CombatStepsDisposition.NoOp, Apply(test, 1, System(Replay(test, 1), "expire-round", 33000) with { Command = System(Replay(test, 1), "expire-round", 33000).Command with { RoundId = "foreign.round" } }).Disposition);
    }

    [Fact]
    public void SeparatePredecessorAndRoundInputsRemainRequiredAuthority()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0]; var test = Case(row);
        var baseBytes = Encoding.UTF8.GetBytes(row.GetProperty("baseCanonicalUtf8").GetString()!);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRoundCodec.ReadBase(baseBytes, test.Request, test.Created, test.Boundary,
            test.PredecessorInputs[..^1], test.PredecessorEvents[..^1]));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRoundCodec.ReadBase(baseBytes, test.Request, [], test.Boundary, test.PredecessorInputs, test.PredecessorEvents));
        var foreign = Case(fixture.RootElement.GetProperty("cases")[5]);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRoundCodec.ReadBase(baseBytes, foreign.Request, foreign.Created, foreign.Boundary, foreign.PredecessorInputs, foreign.PredecessorEvents));
        var inputs = test.Inputs[..2]; inputs[1] = inputs[1] with { Actor = test.Inputs[2].Actor };
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRound.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary,
            test.PredecessorInputs, test.PredecessorEvents, inputs, test.Events[..2]));
        inputs = test.Inputs[..2]; inputs[1] = inputs[1] with { AdmittedAt = 4001 };
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRound.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary,
            test.PredecessorInputs, test.PredecessorEvents, inputs, test.Events[..2]));
        foreach (var (path, value) in new (string, JsonNode?)[] { ("contractVersion", JsonValue.Create(1)), ("clockConfiguration.contractVersion", JsonValue.Create(1)),
            ("clockConfiguration.parentConfigurationHash", JsonValue.Create(ForeignHash)), ("clockConfiguration.decisionBudgetMilliseconds", JsonValue.Create(30001)),
            ("clockConfiguration.timingPolicyId", JsonValue.Create("sandtable.combat.fixed-deadline.v1")), ("steps.stepIndex", JsonValue.Create(4)),
            ("steps.declineReceiptId", null), ("boundary.priorPrefix", JsonValue.Create(ForeignHash)) })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRoundCodec.ReadBase(Change(baseBytes, path, value), test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents));
    }

    [Fact]
    public void CanonicalRehashedEffectsAndStatesCannotReplaceReplay()
    {
        using var fixture = Fixture(); var test = Case(fixture.RootElement.GetProperty("cases")[0]);
        foreach (var (index, path, value) in new (int, string, JsonNode?)[]
        {
            (0,"effect.timing.openingFloorUnixMilliseconds", JsonValue.Create(2999)), (0,"effect.timing.deadlineUnixMilliseconds", JsonValue.Create(33001)),
            (0,"effect.timing.openedAtUnixMilliseconds", JsonValue.Create(2999)), (0,"configurationHash", JsonValue.Create(ForeignHash)),
            (0,"predecessorConfigurationHash", JsonValue.Create(ForeignHash)), (0,"priorPrefix", JsonValue.Create(ForeignHash)),
            (1,"author", JsonValue.Create("system")), (1,"effect.prepared", JsonValue.Create(true)),
            (1,"effect.allocation.committedToe", JsonValue.Create(9)), (3,"effect.previousStepReceiptId", JsonValue.Create("foreign.receipt")),
            (3,"effect.toPositionId", JsonValue.Create("foreign.position")), (3,"effect.fromPositionId", JsonValue.Create("foreign.position"))
        })
        {
            var changed = JsonNode.Parse(Change(test.Events[index], path, value))!;
            changed.AsObject().Remove("receiptId");
            changed["receiptId"] = "cmb." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.round-receipt.v2", Bytes(changed))[7..];
            var events = test.Events[..(index + 1)]; events[index] = Bytes(changed);
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRound.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary,
                test.PredecessorInputs, test.PredecessorEvents, test.Inputs[..(index + 1)], events));
        }
        var state = CampaignCombatSealedRoundCodec.SerializeState(Replay(test, 3));
        foreach (var (path, value) in new (string, JsonNode?)[] { ("stateVersion", JsonValue.Create(31)), ("prefix", JsonValue.Create(ForeignHash)),
            ("baseHash", JsonValue.Create(ForeignHash)), ("roundId", JsonValue.Create("foreign.round")), ("status", JsonValue.Create("cancelled")),
            ("closed", JsonValue.Create(true)), ("stepIndex", JsonValue.Create(5)), ("timing.openingFloorUnixMilliseconds", JsonValue.Create(3500)),
            ("clockConfigurationHash", JsonValue.Create(ForeignHash)), ("commitmentId", JsonValue.Create("foreign.commitment")) })
            Assert.ThrowsAny<JsonException>(() => ReadState(test, 3, Change(state, path, value)));
        var reverse = JsonNode.Parse(state)!;
        reverse["slots"] = new JsonArray(reverse["slots"]!.AsArray().Reverse().Select(n => n!.DeepClone()).ToArray());
        Assert.ThrowsAny<JsonException>(() => ReadState(test, 3, Bytes(reverse)));
        var proof = JsonNode.Parse(test.Events[3])!;
        proof["effect"]!["proofReceipts"] = new JsonArray(proof["effect"]!["proofReceipts"]!.AsArray().Reverse().Select(n => n!.DeepClone()).ToArray());
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRound.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary,
            test.PredecessorInputs, test.PredecessorEvents, test.Inputs[..4], [.. test.Events[..3], Bytes(proof)]));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRound.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary,
            test.PredecessorInputs, test.PredecessorEvents, [.. test.Inputs[..3], test.Inputs[1]], [.. test.Events[..3], test.Events[1]]));
    }

    [Fact]
    public void ClosedGrammarAndBoundsRejectBeforeTrustedContext()
    {
        using var fixture = Fixture(); var row = fixture.RootElement.GetProperty("cases")[0]; var test = Case(row);
        var basis = Encoding.UTF8.GetBytes(row.GetProperty("baseCanonicalUtf8").GetString()!);
        var state = CampaignCombatSealedRoundCodec.SerializeState(Replay(test, 2));
        foreach (var (bytes, kind) in new[] { (basis, "Base"), (state, "RoundState") })
        {
            void Read(byte[] raw)
            {
                if (kind == "Base") CampaignCombatSealedRoundCodec.ReadBase(raw, null!, test.Created, test.Boundary, new Sentry<CombatStepsInput>(1), new Sentry<byte[]>(1));
                else CampaignCombatSealedRoundCodec.ReadState(raw, null!, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents,
                    new Sentry<CombatRoundInput>(1), new Sentry<byte[]>(1));
            }
            var text = Encoding.UTF8.GetString(bytes);
            var root = JsonNode.Parse(bytes)!.AsObject();
            foreach (var field in root.Select(p => p.Key))
            {
                var missing = root.DeepClone().AsObject(); missing.Remove(field);
                Assert.ThrowsAny<JsonException>(() => Read(Bytes(missing)));
            }
            var reverse = new JsonObject(); foreach (var pair in root.Reverse()) reverse.Add(pair.Key, pair.Value?.DeepClone());
            var extra = root.DeepClone(); extra["unknown"] = 0;
            foreach (var raw in new[] { "null"u8.ToArray(), "[]"u8.ToArray(), "{}"u8.ToArray(), Encoding.UTF8.GetBytes(text + " "),
                Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":2", "\"contractVersion\":2,\"contractVersion\":2", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(text.Replace("contractVersion", "contract\\u0056ersion", StringComparison.Ordinal)),
                Bytes(extra), Bytes(reverse), new byte[1_048_577], Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)),
                Encoding.UTF8.GetBytes("{\"world.cohesionCauses\":[" + string.Join(',', Enumerable.Repeat("null", 513)) + "]}") })
                Assert.ThrowsAny<JsonException>(() => Read(raw));
            var nested = root.DeepClone(); var world = kind == "Base" ? nested["boundary"]!["world"]! : nested["world"]!;
            world["elements"]![0]!["operationalState"] = null;
            Assert.ThrowsAny<JsonException>(() => Read(Bytes(nested)));
            Assert.Throws<InvalidOperationException>(() => Read(bytes));
        }
        foreach (var field in new[] { "role", "owner", "slotId", "sealedAt", "allocation" })
        {
            var malformed = JsonNode.Parse(state)!;
            malformed["slots"]![0]![field] = field == "role" ? JsonValue.Create("third-role") : JsonValue.Create(false);
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRoundCodec.ReadState(Bytes(malformed), null!, test.Created, test.Boundary,
                test.PredecessorInputs, test.PredecessorEvents, new Sentry<CombatRoundInput>(1), new Sentry<byte[]>(1)));
        }
        var input = CampaignCombatSealedRoundCodec.SerializeInput(test.Inputs[1]);
        foreach (var value in new JsonNode?[] { JsonValue.Create(-1), JsonValue.Create(true), JsonValue.Create(3.5), JsonValue.Create("3500"), JsonValue.Create(253402300800000L) })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRoundCodec.ReadInput(Change(input, "admittedAt", value)));
        foreach (var value in new JsonNode?[] { null, JsonValue.Create(0), JsonValue.Create("false") })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRoundCodec.ReadInput(Change(input, "clockAvailable", value)));
        Assert.ThrowsAny<JsonException>(() => Apply(test, 2, test.Inputs[1] with { AdmittedAt = -1 }));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRound.ReplayTrustedBoundary(null!, test.Created, test.Boundary,
            test.PredecessorInputs, test.PredecessorEvents, new Sentry<CombatRoundInput>(17), new Sentry<byte[]>(17)));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRound.ReplayTrustedBoundary(null!, test.Created, test.Boundary,
            test.PredecessorInputs, test.PredecessorEvents, new Sentry<CombatRoundInput>(1), ["{}"u8.ToArray()]));
        var unsignedCursor = Change(state, "randomState.nextByteCursor", JsonValue.Create(ulong.MaxValue));
        CampaignCombatSealedRoundCodec.ValidateSyntax(unsignedCursor, "RoundState");
        Assert.ThrowsAny<JsonException>(() => ReadState(test, 2, unsignedCursor));
        var reordered = JsonNode.Parse(basis)!;
        reordered["boundary"]!["world"]!["elements"] = new JsonArray(reordered["boundary"]!["world"]!["elements"]!.AsArray().Reverse().Select(n => n!.DeepClone()).ToArray());
        CampaignCombatSealedRoundCodec.ValidateSyntax(Bytes(reordered), "Base");
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRoundCodec.ReadBase(Bytes(reordered), test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents));
    }

    [Fact]
    public void CapturedCreationInputAndResultCollectionsCannotAliasCallerState()
    {
        using var fixture = Fixture(); var test = Case(fixture.RootElement.GetProperty("cases")[0]);
        var original = test.Created.ToArray(); var events = test.Events[..3].Select(e => e.ToArray()).ToArray();
        var inputs = test.Inputs[..3];
        var state = CampaignCombatSealedRound.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents,
            new MutatingList<CombatRoundInput>(inputs, () => test.Created[0] ^= 1), events);
        test = test with { Created = original };
        var expected = CampaignCombatSealedRoundCodec.SerializeState(state);
        inputs[1] = inputs[1] with { Actor = CampaignOpeningPreambleActor.System }; events[0][0] ^= 1;
        Assert.Equal(expected, CampaignCombatSealedRoundCodec.SerializeState(state));
        var slots = Assert.IsAssignableFrom<IList<CombatRoundSlot>>(state.Slots);
        Assert.Throws<NotSupportedException>(() => slots[0] = slots[1]);
        var steps = Assert.IsAssignableFrom<IList<string>>(state.StepReceipts);
        Assert.Throws<NotSupportedException>(() => steps[0] = "foreign.receipt");
        var receipts = Assert.IsAssignableFrom<IList<CampaignOpeningPreambleReceipt>>(state.Receipts);
        Assert.Throws<NotSupportedException>(() => receipts[0] = receipts[1]);
        var result = Apply(test, 0, test.Inputs[0]); var first = result.EventBytes!; first[0] ^= 1;
        Assert.Equal(test.Events[0], result.EventBytes);
        var serialized = CampaignCombatSealedRoundCodec.SerializeBase(state.Base); serialized[0] ^= 1;
        Assert.Equal(expected, CampaignCombatSealedRoundCodec.SerializeState(state));
    }

    private sealed class Sentry<T>(int count) : IReadOnlyList<T>
    {
        public int Count => count;
        public T this[int index] => throw new InvalidOperationException("Trusted evidence accessed before hostile grammar rejection.");
        public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private sealed class MutatingList<T>(T[] values, Action mutation) : IReadOnlyList<T>
    {
        private bool mutated;
        public int Count { get { if (!mutated) { mutation(); mutated = true; } return values.Length; } }
        public T this[int index] => values[index];
        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)values).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private static CombatRoundInput System(CombatRoundState state, string kind, long? time = null) => new(new(2, kind, state.Base.ConfigurationHash,
        state.Base.Steps.SegmentId, state.RoundId, ExpectedPriorVersion: kind is "complete-step" or "commit-attack" ? state.StateVersion : null),
        CampaignOpeningPreambleActor.System, time);
    private static object Witness(CombatRoundState state, string slotId)
    {
        var slot = state.Slots.Single(s => s.SlotId == slotId);
        return (slot.Owner, slot.Role, slot.Allocation, Sealed: slot.SealedReceiptId is not null, state.Timing!.OpenedAtUnixMilliseconds, state.Timing.DeadlineUnixMilliseconds);
    }
    private const string ForeignHash = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());
    private static byte[] Change(byte[] bytes, string path, JsonNode? value)
    {
        var node = JsonNode.Parse(bytes)!; var current = node; var parts = path.Split('.');
        foreach (var part in parts[..^1]) current = current[part]!;
        current[parts[^1]] = value?.DeepClone(); return Bytes(node);
    }
    private static CombatRoundState ReadState(TestCase test, int count, byte[] bytes) => CampaignCombatSealedRoundCodec.ReadState(bytes, test.Request,
        test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs[..count], test.Events[..count]);

    private sealed record TestCase(CampaignCombatCreationRequest Request, byte[] Created, CombatStepsBoundary Boundary,
        CombatStepsInput[] PredecessorInputs, byte[][] PredecessorEvents, CombatRoundInput[] Inputs, byte[][] Events, int Count);
    private static TestCase Case(JsonElement row)
    {
        using var authority = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Rules", "Fixtures", "combat-authority-envelope-v1.json")));
        var created = Encoding.UTF8.GetBytes(authority.RootElement.GetProperty("goldens").GetProperty("created").GetProperty("canonicalUtf8").GetString()!);
        using var root = JsonDocument.Parse(created);
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var setup = CampaignSetupV7Codec.Deserialize(Encoding.UTF8.GetBytes(root.RootElement.GetProperty("setup").GetRawText()), artifact, scenario);
        var config = CombatDecisionConfigurationCodec.Deserialize(Encoding.UTF8.GetBytes(root.RootElement.GetProperty("configuration").GetRawText()), Cna1979CombatRuleset.Manifest);
        var request = CampaignCombatCreationRequest.Create("rules-lab.combat-creation.1", 0,
            new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, setup, artifact, scenario, config));
        var initial = CampaignCreatedV11Serializer.Deserialize(created, request).InitialWorld;
        var predecessor = row.GetProperty("predecessor");
        var boundaryBytes = JsonSerializer.SerializeToUtf8Bytes(predecessor.GetProperty("boundary"));
        using var boundaryJson = JsonDocument.Parse(boundaryBytes);
        var b = boundaryJson.RootElement; var cycle = b.GetProperty("cycle"); var weather = b.GetProperty("weather"); var random = b.GetProperty("randomState");
        string Text(JsonElement value, string field) => value.GetProperty(field).GetString()!;
        var world = WithCp(initial, b.GetProperty("world").GetProperty("elements").EnumerateArray().Select(e => e.GetProperty("operationalState").GetProperty("capabilityPointsExpended").GetProperty("numerator").GetInt64()).ToArray());
        var boundary = new CombatStepsBoundary(b.GetProperty("contractVersion").GetInt32(), Text(b, "creationBinding"),
            new(cycle.GetProperty("contractVersion").GetInt32(), Text(cycle, "campaignId"), Text(cycle, "rulesetHash"), Text(cycle, "setupId"), Text(cycle, "setupHash"),
                Text(cycle, "contentPackId"), Text(cycle, "contentHash"), Text(cycle, "scenarioId"), cycle.GetProperty("gameTurn").GetInt32(), cycle.GetProperty("operationStage").GetInt32(),
                Text(cycle, "playerPhaseSlot"), Side(Text(cycle, "actingSide")), cycle.GetProperty("ordinal").GetInt32(), cycle.GetProperty("openedAuthorityVersion").GetInt64(),
                Text(cycle, "openingPrefix"), Text(cycle, "admittedPolicyBundleDigest")), Side(Text(b, "firstActingSide")), b.GetProperty("priorVersion").GetInt64(),
            Text(b, "priorPrefix"), Text(b, "completedBreakdownReceipt"), CampaignV11CanonicalCodec.ParsePosition(b.GetProperty("position")), world,
            new(random.GetProperty("contractVersion").GetInt32(), Text(random, "algorithmId"), random.GetProperty("seed").GetUInt64(), random.GetProperty("nextByteCursor").GetUInt64()),
            new(weather.GetProperty("gameTurn").GetInt32(), weather.GetProperty("operationStage").GetInt32(), Weather(Text(weather, "attackerKind")), Weather(Text(weather, "defenderKind")), Text(weather, "weatherReceiptHash")));
        var events = row.GetProperty("eventCanonicalUtf8").EnumerateArray().Select(e => Encoding.UTF8.GetBytes(e.GetString()!)).ToArray();
        var count = Array.FindIndex(events, e => JsonNode.Parse(e)!["effect"]!["kind"]!.GetValue<string>() == "attack-committed");
        return new(request, created, boundary,
            predecessor.GetProperty("inputs").EnumerateArray().Select(i => CampaignCombatSelectionStepsCodec.ReadInput(JsonSerializer.SerializeToUtf8Bytes(i))).ToArray(),
            predecessor.GetProperty("events").EnumerateArray().Select(e => Encoding.UTF8.GetBytes(e.GetProperty("canonicalUtf8").GetString()!)).ToArray(),
            row.GetProperty("inputs").EnumerateArray().Select(i => CampaignCombatSealedRoundCodec.ReadInput(JsonSerializer.SerializeToUtf8Bytes(i))).ToArray(),
            events, count < 0 ? events.Length : count);
    }

    private static CampaignWorldSnapshotV7 WithCp(CampaignWorldSnapshotV7 world, long[] amounts) => new(7, world.CreationBinding,
        world.Elements.Select((e, index) => new CampaignElementStateV6(e.ElementId, e.CurrentLocationId, e.ReserveStatus,
            new(e.OperationalState.LedgerGameTurn, e.OperationalState.LedgerOperationStage, new(amounts[index], 1), e.OperationalState.CohesionLevel,
                e.OperationalState.VehicleBreakdownState, e.OperationalState.MovementEnded, e.OperationalState.InitialLedgerOrigin), e.Components,
            e.SourceParentFormationId, e.CurrentParentFormationId, e.Ammunition, e.Readiness)), world.Representations, world.BrokenVehicleLots,
        world.CohesionCauses, world.Relationships, world.CustodyLots, world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Campaigns", "Fixtures", "combat-sealed-round-v2.json")));
    private static LandSide Side(string side) => side == "axis" ? LandSide.Axis : LandSide.Commonwealth;
    private static WeatherKind Weather(string weather) => weather switch { "normal" => WeatherKind.Normal, "hot" => WeatherKind.Hot, "sandstorm" => WeatherKind.Sandstorm, _ => WeatherKind.Rainstorm };
    private static CombatRoundState Replay(TestCase test, int count) => CampaignCombatSealedRound.ReplayTrustedBoundary(test.Request, test.Created,
        test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs[..count], test.Events[..count]);
    private static CombatRoundResult Apply(TestCase test, int count, CombatRoundInput input, bool enabled = true) => CampaignCombatSealedRound.ApplyTrustedBoundary(test.Request,
        test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs[..count], test.Events[..count], input, enabled);
}
