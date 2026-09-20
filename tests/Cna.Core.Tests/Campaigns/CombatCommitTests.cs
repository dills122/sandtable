using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using static Cna.Core.Tests.Campaigns.CombatSealsTests;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatCommitTests
{
    [Fact]
    public void FourFinalCommitEventsAndPaidStatesMatchFrozenLiterals()
    {
        using var fixture = Fixture(); var count = 0;
        foreach (var row in fixture.RootElement.GetProperty("cases").EnumerateArray().Where(r => r.GetProperty("name").GetString()!.EndsWith(".committed", StringComparison.Ordinal)))
        {
            var test = Case(row);
            var result = Apply(test, 5, test.Inputs[5], false);
            Assert.Equal(CombatStepsDisposition.Accepted, result.Disposition);
            Assert.Equal(test.Events[5], result.EventBytes);
            Assert.Equal(Encoding.UTF8.GetBytes(row.GetProperty("stateCanonicalUtf8")[6].GetString()!), CampaignCombatSealedRoundCodec.SerializeState(result.State));
            var paid = result.State;
            Assert.Equal("committed", paid.Status); Assert.Equal(5, paid.StepIndex); Assert.False(paid.Closed);
            Assert.Equal(Replay(test, 5).Base.BaseHash, paid.Base.BaseHash);
            Assert.Equal(CampaignCombatSealedRoundCodec.SerializeBase(Replay(test, 5).Base), CampaignCombatSealedRoundCodec.SerializeBase(paid.Base));
            Assert.Equal(paid.Base.Boundary.RandomState, Replay(test, 6).Base.Boundary.RandomState);
            var edge = Assert.Single(paid.AttackHistory); var target = Assert.Single(paid.TargetUses);
            Assert.Equal(paid.Base.Steps.Selection!.Attacker.Unit, edge.Attacker);
            Assert.Equal(paid.Base.Steps.Selection.Defender.Unit, edge.Defender);
            Assert.Equal(edge.TargetLocationId, target.TargetLocationId); Assert.Equal(edge.CommitmentId, target.CommitmentId);
            foreach (var element in paid.World.Elements)
            {
                var original = paid.Base.Boundary.World.Elements.Single(e => e.ElementId == element.ElementId);
                Assert.Equal(0, element.Ammunition.Points); Assert.Equal(10, original.Ammunition.Points);
                Assert.Equal(original.Ammunition.InitialAmmunitionOrigin, element.Ammunition.InitialAmmunitionOrigin);
                Assert.Equal(original.Components, element.Components); Assert.Equal(original.CurrentLocationId, element.CurrentLocationId);
                Assert.Equal(original.OperationalState.CohesionLevel, element.OperationalState.CohesionLevel);
                Assert.Equal(original.OperationalState.MovementEnded, element.OperationalState.MovementEnded);
                Assert.Equal(element.ElementId == edge.Attacker.ElementId ? 5 : 3, element.OperationalState.CapabilityPointsExpended.Numerator);
            }
            count++;
        }
        Assert.Equal(4, count);
    }
    [Fact]
    public void AllTenCompleteTracesRestoreFiftyEightEventsAndSixtyEightStateCuts()
    {
        using var fixture = Fixture(); var events = 0; var states = 0;
        foreach (var row in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var test = Case(row);
            for (var cut = 0; cut <= test.Events.Length; cut++)
            {
                var literal = Encoding.UTF8.GetBytes(row.GetProperty("stateCanonicalUtf8")[cut].GetString()!);
                var restored = Read(test, cut, literal);
                Assert.Equal(literal, CampaignCombatSealedRoundCodec.SerializeState(restored)); states++;
                if (cut < test.Events.Length) { Assert.Equal(test.Events[cut], Apply(test, cut, test.Inputs[cut], cut == 0).EventBytes); events++; }
            }
        }
        Assert.Equal(58, events); Assert.Equal(68, states);
    }

    [Fact]
    public void DiscardedCandidateAndLostResponseRecoverWithoutSecondChargeOrDraw()
    {
        using var fixture = Fixture();
        foreach (var row in CommittedRows(fixture))
        {
            var test = Case(row); var prior = Replay(test, 5);
            var before = CampaignCombatSealedRoundCodec.SerializeState(prior);
            var candidate = Apply(test, 5, test.Inputs[5], false);
            // Simulated failure to publish: discard candidate without appending its event.
            Assert.Equal(before, CampaignCombatSealedRoundCodec.SerializeState(prior));
            Assert.Equal(before, CampaignCombatSealedRoundCodec.SerializeState(Replay(test, 5)));
            Assert.Equal(candidate.EventBytes, Apply(test, 5, test.Inputs[5], false).EventBytes);
            var accepted = Replay(test, 6); var paidBytes = CampaignCombatSealedRoundCodec.SerializeState(accepted);
            foreach (var retry in test.Inputs)
            {
                var duplicate = Apply(test, 6, retry with { AdmittedAt = null, ClockAvailable = false }, false);
                Assert.Equal(CombatStepsDisposition.Duplicate, duplicate.Disposition);
                Assert.Equal(paidBytes, CampaignCombatSealedRoundCodec.SerializeState(duplicate.State));
                Assert.Single(duplicate.State.AttackHistory); Assert.Single(duplicate.State.TargetUses);
            }
            Assert.Equal(test.Events[5], Apply(test, 6, test.Inputs[5] with { AdmittedAt = 99999, ClockAvailable = false }, false).EventBytes);
            Assert.Equal(paidBytes, CampaignCombatSealedRoundCodec.SerializeState(Read(test, 6, paidBytes)));
            Assert.ThrowsAny<JsonException>(() => Apply(test, 6, test.Inputs[5] with { Actor = CampaignOpeningPreambleActor.Axis }));
            Assert.ThrowsAny<JsonException>(() => Apply(test, 6, test.Inputs[5] with { AdmittedAt = -1 }));
            Assert.ThrowsAny<JsonException>(() => Apply(test, 6, test.Inputs[5] with { Command = test.Inputs[5].Command with { ExpectedPriorVersion = accepted.StateVersion } }));
            Assert.ThrowsAny<JsonException>(() => Apply(test, 6, test.Inputs[5] with { Command = test.Inputs[5].Command with { Kind = "complete-step", ExpectedPriorVersion = accepted.StateVersion } }));
            foreach (var kind in new[] { "expire-round", "controller-unavailable" })
                Assert.Equal(CombatStepsDisposition.NoOp, Apply(test, 6, test.Inputs[5] with { Command = test.Inputs[5].Command with { Kind = kind, ExpectedPriorVersion = null } }).Disposition);
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRound.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary,
                test.PredecessorInputs, test.PredecessorEvents, [.. test.Inputs, test.Inputs[5]], [.. test.Events, test.Events[5]]));
        }
    }

    [Fact]
    public void ExactCpFiveSevenBoundaryReplaysRealSyntheticPredecessorAndCommitsToTen()
    {
        using var fixture = Fixture();
        foreach (var row in CommittedRows(fixture))
        {
            var original = Case(row);
            var attackerId = original.PredecessorInputs.Single(i => i.Command.Kind == "choose-selection").Command.Candidate!.Attacker.Unit.ElementId;
            var amounts = original.Boundary.World.Elements.Select(e => e.ElementId == attackerId ? 5L : 7L).ToArray();
            var test = Rebuild(original, original.Boundary with { World = WithCp(original.Boundary.World, amounts) });
            var paid = Apply(test, 5, test.Inputs[5], false).State;
            Assert.All(paid.World.Elements, e => Assert.Equal(10, e.OperationalState.CapabilityPointsExpended.Numerator));
            Assert.All(paid.World.Elements, e => Assert.Equal(0, e.Ammunition.Points));
            Assert.Empty(paid.World.CohesionCauses);
            Assert.All(paid.World.Elements, e => Assert.Equal(0, e.OperationalState.CohesionLevel));
            var before = Replay(test, 5); Assert.Equal(amounts, before.World.Elements.Select(e => e.OperationalState.CapabilityPointsExpended.Numerator));
            var literal = CampaignCombatSealedRoundCodec.SerializeState(paid);
            Assert.Equal(literal, CampaignCombatSealedRoundCodec.SerializeState(Read(test, 6, literal)));
            foreach (var invalid in new[] { amounts.Select(n => n + 1).ToArray(), new long[] { 11, 11 } })
                Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRound.AuthenticateBase(test.Request, test.Created,
                    test.Boundary with { World = WithCp(test.Boundary.World, invalid) }, test.PredecessorInputs, test.PredecessorEvents));
        }
    }

    [Fact]
    public void EarlyCancelledStaleOrMalformedCommitCannotChangeRetainedAuthority()
    {
        using var fixture = Fixture(); var test = Case(CommittedRows(fixture).First()); var command = test.Inputs[5];
        for (var cut = 0; cut < 5; cut++)
        {
            var state = Replay(test, cut); var before = CampaignCombatSealedRoundCodec.SerializeState(state);
            Assert.ThrowsAny<JsonException>(() => Apply(test, cut, command with { Command = command.Command with { ExpectedPriorVersion = state.StateVersion } }));
            Assert.Equal(before, CampaignCombatSealedRoundCodec.SerializeState(state));
        }
        foreach (var bad in new[] { command with { AdmittedAt = 1 }, command with { ClockAvailable = false }, command with { Actor = CampaignOpeningPreambleActor.Axis },
            command with { Command = command.Command with { ExpectedPriorVersion = 1 } }, command with { Command = command.Command with { RoundId = "foreign.round" } },
            command with { Command = command.Command with { ClockConfigurationHash = ForeignHash } }, command with { Command = command.Command with { SlotId = "foreign.slot" } },
            command with { Command = command.Command with { Allocation = test.Inputs[1].Command.Allocation } } })
            Assert.ThrowsAny<JsonException>(() => Apply(test, 5, bad));
        foreach (var row in fixture.RootElement.GetProperty("cases").EnumerateArray().Where(r => !r.GetProperty("name").GetString()!.EndsWith(".committed", StringComparison.Ordinal)))
        {
            var cancelled = Case(row); var state = Replay(cancelled, cancelled.Count);
            Assert.ThrowsAny<JsonException>(() => Apply(cancelled, cancelled.Count, command with
            {
                Command = command.Command with
                { RoundId = state.RoundId, SegmentId = state.Base.Steps.SegmentId, ExpectedPriorVersion = state.StateVersion }
            }));
        }
    }

    [Fact]
    public void RehashedCommitAndPaidRootForgeriesCannotReplaceCausalPayment()
    {
        using var fixture = Fixture(); var test = Case(CommittedRows(fixture).First());
        foreach (var path in new[] { "effect.commitmentId", "priorPrefix", "configurationHash", "effect.preResultRandomState.nextByteCursor", "stateVersion" })
        {
            var root = JsonNode.Parse(test.Events[5])!;
            Set(root, path, path.EndsWith("nextByteCursor", StringComparison.Ordinal) || path == "stateVersion" ? JsonValue.Create(99) : JsonValue.Create(path.EndsWith("Id", StringComparison.Ordinal) ? "foreign.commitment" : ForeignHash));
            RejectEvent(test, Rehash(root));
        }
        foreach (var field in new[] { "beforeCp", "afterCp", "beforeAmmo", "afterAmmo" })
        {
            var root = JsonNode.Parse(test.Events[5])!; root["effect"]!["costs"]![0]![field] = 9;
            RejectEvent(test, Rehash(root));
        }
        foreach (var field in new[] { "allocations", "costs" })
        {
            var root = JsonNode.Parse(test.Events[5])!;
            root["effect"]![field] = new JsonArray(root["effect"]![field]!.AsArray().Reverse().Select(n => n!.DeepClone()).ToArray());
            RejectEvent(test, Rehash(root));
        }
        var paid = CampaignCombatSealedRoundCodec.SerializeState(Replay(test, 6));
        foreach (var path in new[] { "commitmentId", "status", "prefix", "baseHash" })
        {
            var root = JsonNode.Parse(paid)!; root[path] = path.EndsWith("Hash", StringComparison.Ordinal) || path == "prefix" ? ForeignHash : "foreign.value";
            Assert.ThrowsAny<JsonException>(() => Read(test, 6, Bytes(root)));
        }
        foreach (var collection in new[] { "attackHistory", "targetUses" })
        {
            var root = JsonNode.Parse(paid)!; root[collection] = new JsonArray();
            Assert.ThrowsAny<JsonException>(() => Read(test, 6, Bytes(root)));
            root = JsonNode.Parse(paid)!; root[collection]!.AsArray().Add(root[collection]![0]!.DeepClone());
            Assert.ThrowsAny<JsonException>(() => Read(test, 6, Bytes(root)));
        }
        var reversed = JsonNode.Parse(paid)!;
        (reversed["attackHistory"]![0]!["attacker"], reversed["attackHistory"]![0]!["defender"]) =
            (reversed["attackHistory"]![0]!["defender"]!.DeepClone(), reversed["attackHistory"]![0]!["attacker"]!.DeepClone());
        Assert.ThrowsAny<JsonException>(() => Read(test, 6, Bytes(reversed)));
        var target = JsonNode.Parse(paid)!; target["targetUses"]![0]!["targetLocationId"] = "axis-supply";
        Assert.ThrowsAny<JsonException>(() => Read(test, 6, Bytes(target)));
        var ammo = JsonNode.Parse(paid)!; ammo["world"]!["elements"]![0]!["ammunition"]!["points"] = 10;
        Assert.ThrowsAny<JsonException>(() => Read(test, 6, Bytes(ammo)));
        var steps = test.Events.ToArray(); steps[4] = steps[3];
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRound.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary,
            test.PredecessorInputs, test.PredecessorEvents, test.Inputs, steps));
    }

    [Fact]
    public void CommitGrammarRejectsBeforeTrustedInputAndPaidWorldHasNoStaleByteCache()
    {
        using var fixture = Fixture(); var test = Case(CommittedRows(fixture).First());
        foreach (var field in new[] { "beforeCp", "afterCp", "beforeAmmo", "afterAmmo", "unit" })
        {
            var root = JsonNode.Parse(test.Events[5])!; root["effect"]!["costs"]![0]![field] = false;
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRound.ReplayTrustedBoundary(null!, test.Created, test.Boundary,
                test.PredecessorInputs, test.PredecessorEvents, new Sentry<CombatRoundInput>(1), [Bytes(root)]));
        }
        var paid = Replay(test, 6); var bytes = CampaignCombatSealedRoundCodec.SerializeState(paid);
        foreach (var field in new[] { "attacker", "gameTurn", "commitmentId" })
        {
            var root = JsonNode.Parse(bytes)!; root["attackHistory"]![0]![field] = false;
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRoundCodec.ReadState(Bytes(root), null!, test.Created, test.Boundary,
                test.PredecessorInputs, test.PredecessorEvents, new Sentry<CombatRoundInput>(1), new Sentry<byte[]>(1)));
        }
        var changedAmmo = WithElements(paid.World, paid.World.Elements.Select(e => Copy(e, ammunition: new(9, e.Ammunition.InitialAmmunitionOrigin))));
        var altered = CampaignCombatSealedRoundCodec.SerializeState(paid with { World = changedAmmo });
        Assert.NotEqual(bytes, altered);
        using var changed = JsonDocument.Parse(altered);
        Assert.Equal(9, changed.RootElement.GetProperty("world").GetProperty("elements")[0].GetProperty("ammunition").GetProperty("points").GetInt32());
        Assert.ThrowsAny<JsonException>(() => Read(test, 6, altered));
        var foreignReadiness = WithElements(paid.World, paid.World.Elements.Select(e => Copy(e, readiness: new(e.Readiness.GameTurn,
            e.Readiness.OperationStage, e.Readiness.WaterStatus, e.Readiness.StoresStatus, true, e.Readiness.InitialReadinessOrigin))));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRoundCodec.SerializeState(paid with { World = foreignReadiness }));
        Assert.Equal(bytes, CampaignCombatSealedRoundCodec.SerializeState(paid));
        Assert.All(paid.Base.Boundary.World.Elements, e => Assert.Equal(10, e.Ammunition.Points));
    }

    [Fact]
    public void InvalidPreUseResourcesAndForeignPaidWorldCannotBeReintroducedAsInitialEligibility()
    {
        using var fixture = Fixture(); var test = Case(CommittedRows(fixture).First());
        var initial = test.Boundary.World;
        var lowAmmo = WithElements(initial, initial.Elements.Select(e => Copy(e, ammunition: new(9, e.Ammunition.InitialAmmunitionOrigin))));
        var cohesion = WithElements(initial, initial.Elements.Select(e => Copy(e, operational: new(e.OperationalState.LedgerGameTurn,
            e.OperationalState.LedgerOperationStage, e.OperationalState.CapabilityPointsExpended, -1, e.OperationalState.VehicleBreakdownState,
            e.OperationalState.MovementEnded, e.OperationalState.InitialLedgerOrigin))));
        var fractional = WithElements(initial, initial.Elements.Select(e => Copy(e, operational: new(e.OperationalState.LedgerGameTurn,
            e.OperationalState.LedgerOperationStage, new(1, 2), e.OperationalState.CohesionLevel, e.OperationalState.VehicleBreakdownState,
            e.OperationalState.MovementEnded, e.OperationalState.InitialLedgerOrigin))));
        foreach (var world in new[] { lowAmmo, cohesion, fractional, Replay(test, 6).World })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRound.AuthenticateBase(test.Request, test.Created, test.Boundary with { World = world },
                test.PredecessorInputs, test.PredecessorEvents));
    }

    [Fact]
    public void CommittedHistoryWorldAndReturnedEventBuffersStayOwned()
    {
        using var fixture = Fixture(); var test = Case(CommittedRows(fixture).First());
        var paid = Apply(test, 5, test.Inputs[5]); var expected = CampaignCombatSealedRoundCodec.SerializeState(paid.State);
        var edge = Assert.IsAssignableFrom<IList<CombatRoundAttackHistory>>(paid.State.AttackHistory);
        Assert.Throws<NotSupportedException>(() => edge[0] = edge[0] with { TargetLocationId = "foreign.target" });
        var target = Assert.IsAssignableFrom<IList<CombatRoundTargetUse>>(paid.State.TargetUses);
        Assert.Throws<NotSupportedException>(() => target.Clear());
        var elements = Assert.IsAssignableFrom<IList<CampaignElementStateV6>>(paid.State.World.Elements);
        Assert.Throws<NotSupportedException>(() => elements[0] = elements[1]);
        var eventBytes = paid.EventBytes!; eventBytes[0] ^= 1;
        Assert.Equal(test.Events[5], paid.EventBytes); Assert.Equal(expected, CampaignCombatSealedRoundCodec.SerializeState(paid.State));
        var source = new List<CombatRoundAttackHistory>(paid.State.AttackHistory);
        var copy = paid.State with { AttackHistory = source }; source.Clear();
        Assert.Single(copy.AttackHistory);
    }

    private sealed class Sentry<T>(int count) : IReadOnlyList<T>
    {
        public int Count => count;
        public T this[int index] => throw new InvalidOperationException("Trusted inputs accessed before hostile commit bytes rejected.");
        public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private static CampaignElementStateV6 Copy(CampaignElementStateV6 e, CampaignElementOperationalStateV6? operational = null,
        CampaignElementAmmunitionState? ammunition = null, CampaignElementCombatReadinessState? readiness = null) => new(e.ElementId,
        e.CurrentLocationId, e.ReserveStatus, operational ?? e.OperationalState, e.Components, e.SourceParentFormationId,
        e.CurrentParentFormationId, ammunition ?? e.Ammunition, readiness ?? e.Readiness);
    private static CampaignWorldSnapshotV7 WithElements(CampaignWorldSnapshotV7 world, IEnumerable<CampaignElementStateV6> elements) => new(7,
        world.CreationBinding, elements, world.Representations, world.BrokenVehicleLots, world.CohesionCauses, world.Relationships,
        world.CustodyLots, world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
    private static byte[] Bytes(JsonNode node) => Encoding.UTF8.GetBytes(node.ToJsonString());
    private static void Set(JsonNode root, string path, JsonNode? value)
    {
        var parts = path.Split('.'); var current = root;
        foreach (var part in parts[..^1]) current = current[part]!;
        current[parts[^1]] = value;
    }
    private static byte[] Rehash(JsonNode root)
    {
        root.AsObject().Remove("receiptId");
        root["receiptId"] = "cmb." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.round-receipt.v2", Bytes(root))[7..];
        return Bytes(root);
    }
    private static void RejectEvent(TestCase test, byte[] bytes) => Assert.ThrowsAny<JsonException>(() => CampaignCombatSealedRound.ReplayTrustedBoundary(
        test.Request, test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs, [.. test.Events[..5], bytes]));

    private static IEnumerable<JsonElement> CommittedRows(JsonDocument fixture) => fixture.RootElement.GetProperty("cases").EnumerateArray()
        .Where(r => r.GetProperty("name").GetString()!.EndsWith(".committed", StringComparison.Ordinal));
    private static CombatRoundState Read(TestCase test, int cut, byte[] bytes) => CampaignCombatSealedRoundCodec.ReadState(bytes, test.Request,
        test.Created, test.Boundary, test.PredecessorInputs, test.PredecessorEvents, test.Inputs[..cut], test.Events[..cut]);
    private const string ForeignHash = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private static TestCase Rebuild(TestCase original, CombatStepsBoundary boundary)
    {
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
