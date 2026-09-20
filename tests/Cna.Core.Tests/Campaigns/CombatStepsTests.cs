using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatStepsTests
{
    [Fact]
    public void EveryLiteralTrustedInputPreservesFrozenBytes()
    {
        using var fixture = Fixture();
        foreach (var row in fixture.RootElement.GetProperty("cases").EnumerateArray())
            foreach (var input in row.GetProperty("inputs").EnumerateArray())
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(input);
                var parsed = CampaignCombatSelectionStepsCodec.ReadInput(bytes);
                Assert.Equal(bytes, CampaignCombatSelectionStepsCodec.SerializeInput(parsed));
            }
    }
    [Fact]
    public void FiveSyntheticLiteralTracesMatchEveryEventFinalControlAndRestoreCut()
    {
        using var fixture = Fixture();
        var eventsChecked = 0;
        foreach (var row in fixture.RootElement.GetProperty("cases").EnumerateArray())
        {
            var test = Case(fixture, row);
            Assert.Equal(test.BoundaryBytes, CampaignCombatSelectionStepsCodec.SerializeBoundary(test.Request, test.Created, test.Boundary));
            Assert.Equal(test.Boundary, CampaignCombatSelectionStepsCodec.ReadBoundary(test.BoundaryBytes, test.Request, test.Created, test.Boundary));
            var prior = Replay(test, 0);
            var initialBytes = CampaignCombatSelectionStepsCodec.SerializeControl(prior);
            Assert.Equal(initialBytes, CampaignCombatSelectionStepsCodec.SerializeControl(CampaignCombatSelectionStepsCodec.ReadControl(initialBytes,
                test.Request, test.Created, test.Boundary, [], [])));
            for (var index = 0; index < test.Events.Length; index++)
            {
                var result = Apply(test, index, test.Inputs[index]);
                Assert.Equal(CombatStepsDisposition.Accepted, result.Disposition);
                Assert.Equal(test.Events[index], result.EventBytes);
                var golden = row.GetProperty("events")[index];
                Assert.Equal(golden.GetProperty("byteCount").GetInt32(), result.EventBytes!.Length);
                Assert.Equal(golden.GetProperty("sha256").GetString(), Hash(result.EventBytes));
                var bytes = CampaignCombatSelectionStepsCodec.SerializeControl(result.Control);
                Assert.Equal(bytes, CampaignCombatSelectionStepsCodec.SerializeControl(Replay(test, index + 1)));
                var restored = CampaignCombatSelectionStepsCodec.ReadControl(bytes, test.Request, test.Created, test.Boundary,
                    test.Inputs[..(index + 1)], test.Events[..(index + 1)]);
                Assert.Equal(bytes, CampaignCombatSelectionStepsCodec.SerializeControl(restored));
                Assert.Equal(prior.StateVersion + 1, restored.StateVersion);
                Assert.Equal(CampaignOpeningPreambleCodec.EventPrefix(prior.Prefix, test.Events[index]), restored.Prefix);
                for (var retry = 0; retry <= index; retry++)
                {
                    // C3a hashes Command only and checks actor separately; different valid
                    // admission metadata cannot turn a retained retry into a new event.
                    var duplicate = Apply(test, index + 1, test.Inputs[retry] with { AdmittedAt = 999999, ClockAvailable = false });
                    Assert.Equal(CombatStepsDisposition.Duplicate, duplicate.Disposition);
                    Assert.Equal(test.Events[retry], duplicate.EventBytes);
                    Assert.Equal(bytes, CampaignCombatSelectionStepsCodec.SerializeControl(duplicate.Control));
                }
                prior = restored; eventsChecked++;
            }
            var final = Encoding.UTF8.GetBytes(row.GetProperty("controlGolden").GetProperty("canonicalUtf8").GetString()!);
            Assert.Equal(final, CampaignCombatSelectionStepsCodec.SerializeControl(prior));
            Assert.Equal(row.GetProperty("controlGolden").GetProperty("sha256").GetString(), Hash(final));
            Assert.Equal(test.BoundaryBytes, CampaignCombatSelectionStepsCodec.SerializeBoundary(test.Request, test.Created, test.Boundary));
            Assert.Equal(row.GetProperty("expected").GetProperty("stepCount").GetInt32(), prior.StepIndex);
            if (!prior.Closed) Assert.ThrowsAny<JsonException>(() => Apply(test, test.Events.Length,
                Structural(prior, "complete-step", "land.position.operation-1.first-player.movement-and-combat.combat.force-assignment")));
        }
        Assert.Equal(41, eventsChecked);
    }

    [Fact]
    public void BothRoleMirrorsMatchIndependentSupplementalOracleCommitments()
    {
        // Derived with frozen side-projection-v1 mirror_steps, not additional literal private fixtures.
        string[] commonwealthHashes = ["780315d6f887fa459a224d62951c7fff0da5b88a1a57da6c6db8bd96f7ecb7cb", "8aec812644de589712b5e8645098bc1a35ed6a66706c0c258b135ccccd173247",
            "4650da6740bea859ba06f0aa1a9255da2b5eff356a2b5c460252ff3621532381", "ffd20b2d4eaea749e161dcb0a8b98931b9c5baea9f2f19360d0eb6fa1fd68b6c", "1e61e714841ae9a05b6412ddbe2d7d173b287e5672ef92a0f6231ccef6ab58a9"];
        using var fixture = Fixture(); var eventCount = 0; var cutCount = 0;
        foreach (var side in new[] { LandSide.Axis, LandSide.Commonwealth })
        {
            var index = 0;
            foreach (var row in fixture.RootElement.GetProperty("cases").EnumerateArray())
            {
                var original = Case(fixture, row); var boundary = Mirror(original.Boundary, side);
                var inputs = new List<CombatStepsInput>(); var events = new List<byte[]>();
                var state = CampaignCombatSelectionSteps.ReplayTrustedBoundary(original.Request, original.Created, boundary, inputs, events);
                var boundaryBytes = CampaignCombatSelectionStepsCodec.SerializeBoundary(original.Request, original.Created, boundary);
                var initialBytes = CampaignCombatSelectionStepsCodec.SerializeControl(state);
                Assert.Equal(initialBytes, CampaignCombatSelectionStepsCodec.SerializeControl(CampaignCombatSelectionStepsCodec.ReadControl(initialBytes,
                    original.Request, original.Created, boundary, inputs, events)));
                cutCount++;
                foreach (var input in original.Inputs)
                {
                    var command = input.Command with
                    {
                        SegmentId = state.SegmentId,
                        ExpectedPriorVersion = input.Command.ExpectedPriorVersion is null ? null : state.StateVersion,
                        DecisionId = input.Command.DecisionId is null ? null : state.SegmentId + (input.Command.DecisionId.EndsWith(".selection", StringComparison.Ordinal) ? ".selection" : ".rba"),
                    };
                    var actor = input.Actor;
                    if (command.Kind == "choose-selection")
                    {
                        actor = side == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth;
                        if (side == LandSide.Commonwealth && command.Candidate is { } candidate)
                            command = command with { Candidate = new(candidate.Defender, candidate.Attacker, candidate.Attacker.LocationId, candidate.Basis) };
                    }
                    if (command.Kind == "decline-rba")
                    {
                        actor = side == LandSide.Axis ? CampaignOpeningPreambleActor.Commonwealth : CampaignOpeningPreambleActor.Axis;
                        if (side == LandSide.Commonwealth) command = command with { Participant = original.Inputs[1].Command.Candidate!.Attacker.Unit };
                    }
                    var trusted = input with { Command = command, Actor = actor };
                    var result = CampaignCombatSelectionSteps.ApplyTrustedBoundary(original.Request, original.Created, boundary, inputs, events, trusted);
                    Assert.Equal(CombatStepsDisposition.Accepted, result.Disposition);
                    inputs.Add(trusted); events.Add(result.EventBytes!); state = result.Control;
                    var bytes = CampaignCombatSelectionStepsCodec.SerializeControl(state);
                    Assert.Equal(bytes, CampaignCombatSelectionStepsCodec.SerializeControl(CampaignCombatSelectionStepsCodec.ReadControl(bytes,
                        original.Request, original.Created, boundary, inputs, events)));
                    Assert.Equal(boundaryBytes, CampaignCombatSelectionStepsCodec.SerializeBoundary(original.Request, original.Created, boundary));
                    eventCount++; cutCount++;
                }
                Assert.Equal(side == LandSide.Axis ? row.GetProperty("controlGolden").GetProperty("sha256").GetString() : "sha256:" + commonwealthHashes[index],
                    Hash(CampaignCombatSelectionStepsCodec.SerializeControl(state)));
                if (state.Selection is { } selected)
                {
                    Assert.Equal(side == LandSide.Axis ? "axis" : "commonwealth", selected.Attacker.Unit.OriginalSide);
                    Assert.Equal(selected.Defender.LocationId, selected.TargetLocationId);
                }
                Assert.Equal(row.GetProperty("expected").GetProperty("stepCount").GetInt32(), state.StepIndex);
                index++;
            }
        }
        Assert.Equal(82, eventCount); Assert.Equal(92, cutCount);
    }

    [Fact]
    public void DeadlinesRegressionUnavailabilityAndStaleTimersFollowFrozenHighWaterRules()
    {
        using var fixture = Fixture(); var test = Case(fixture, fixture.RootElement.GetProperty("cases")[3]);
        var selection = Replay(test, 1); var choose = test.Inputs[1];
        Assert.Equal(CombatStepsDisposition.Accepted, Apply(test, 1, choose with { AdmittedAt = 30999 }).Disposition);
        foreach (var time in new long?[] { 31000, 31001, 999, null }) Reject(test, 1, choose with { AdmittedAt = time });
        Reject(test, 1, choose with { ClockAvailable = false });
        foreach (var invalid in new long[] { -1, CampaignCombatSelectionSteps.UtcMaximum + 1 }) Reject(test, 1, choose with { AdmittedAt = invalid });
        var timer = new CombatStepsInput(new(1, "expire-window", selection.SegmentId, DecisionId: selection.SelectionWindow!.DecisionId), CampaignOpeningPreambleActor.System, 30999);
        var early = Apply(test, 1, timer);
        Assert.Equal(CombatStepsDisposition.NoOp, early.Disposition); Assert.Null(early.EventBytes); Assert.Null(early.ReceiptId);
        Assert.Equal(CampaignCombatSelectionStepsCodec.SerializeControl(selection), CampaignCombatSelectionStepsCodec.SerializeControl(early.Control));
        foreach (var (now, available) in new (long?, bool)[] { (31000, true), (31001, true), (999, true), (null, true), (null, false) })
        {
            var expired = Apply(test, 1, timer with { AdmittedAt = now, ClockAvailable = available });
            Assert.Equal("no-selection", expired.Control.SelectionOutcome);
            Assert.Null(expired.Control.DeclineReceiptId);
            Assert.Equal(31000, expired.Control.SelectionWindow!.Timing.DeadlineUnixMilliseconds);
            Assert.Equal(now is null ? 1000 : Math.Max(1000, now.Value), expired.Control.SelectionWindow.Timing.HighWaterUnixMilliseconds);
        }
        foreach (var time in new long?[] { null, CampaignCombatSelectionSteps.UtcMaximum, -1 }) Reject(test, 0, test.Inputs[0] with { AdmittedAt = time });
        var unavailableOpen = Apply(test, 0, test.Inputs[0] with { AdmittedAt = null, ClockAvailable = false });
        Assert.Equal("system-no-selection", unavailableOpen.Control.SelectionOutcome); Assert.Null(unavailableOpen.Control.SelectionWindow);
        var noClock = test with { Inputs = [test.Inputs[0] with { AdmittedAt = null, ClockAvailable = false }], Events = [unavailableOpen.EventBytes!] };
        Assert.Equal("no-selection", Apply(noClock, 1, Structural(unavailableOpen.Control, "close-empty-selection")).Control.SelectionOutcome);

        var beforeRba = Replay(test, 4);
        foreach (var input in new[] { test.Inputs[4] with { AdmittedAt = 999 }, test.Inputs[4] with { AdmittedAt = null }, test.Inputs[4] with { ClockAvailable = false } }) Reject(test, 4, input);
        var unavailable = new CombatStepsInput(new(1, "controller-unavailable", beforeRba.SegmentId, DecisionId: beforeRba.SegmentId + ".rba"), CampaignOpeningPreambleActor.System, null, false);
        var cancelledBefore = Apply(test, 4, unavailable);
        Assert.Equal("cancelled", cancelledBefore.Control.SelectionOutcome); Assert.Null(cancelledBefore.Control.RbaWindow); Assert.Null(cancelledBefore.Control.DeclineReceiptId);
        var rba = Replay(test, 5);
        foreach (var (now, available) in new (long?, bool)[] { (1999, true), (null, false), (2001, true), (32000, true) })
        {
            var cancelled = Apply(test, 5, unavailable with { AdmittedAt = now, ClockAvailable = available });
            Assert.Equal("cancelled", cancelled.Control.SelectionOutcome); Assert.Null(cancelled.Control.DeclineReceiptId);
            Assert.Equal(32000, cancelled.Control.RbaWindow!.Timing.DeadlineUnixMilliseconds);
            Assert.Equal(now is null ? 2000 : Math.Max(2000, now.Value), cancelled.Control.RbaWindow.Timing.HighWaterUnixMilliseconds);
            Assert.Equal(CampaignCombatIdentityCodec.SerializeCandidate(rba.Selection!), CampaignCombatIdentityCodec.SerializeCandidate(cancelled.Control.Selection!));
        }
        Assert.Equal(CombatStepsDisposition.Accepted, Apply(test, 5, test.Inputs[5] with { AdmittedAt = 31999 }).Disposition);
        foreach (var time in new long?[] { 32000, 32001, 1999, null }) Reject(test, 5, test.Inputs[5] with { AdmittedAt = time });
        foreach (var cut in new[] { 5, 6, 7 }) Assert.Equal(CombatStepsDisposition.NoOp, Apply(test, cut, timer with { AdmittedAt = 999999 }).Disposition);
        var rbaTimer = timer with { Command = timer.Command with { DecisionId = rba.RbaWindow!.DecisionId }, AdmittedAt = 32000 };
        Assert.Equal(CombatStepsDisposition.NoOp, Apply(test, 6, rbaTimer).Disposition);
        Assert.Equal("cancelled", Apply(test, 5, rbaTimer).Control.SelectionOutcome);
    }

    [Fact]
    public void ForeignCompatibleRequestsAndUnsupportedFactsNeverBroadenSyntheticBoundaryAdmission()
    {
        using var fixture = Fixture(); var test = Case(fixture, fixture.RootElement.GetProperty("cases")[3]);
        var context = test.Request.Context;
        var changedConfig = new CombatDecisionConfiguration(context.Configuration.ConfigId, context.Configuration.RulesInputHash,
            context.Configuration.Windows.Select(w => w.Kind == "selection" ? w with { DecisionBudgetMilliseconds = w.DecisionBudgetMilliseconds + 1 } : w));
        var changedContext = new CampaignCombatCreationContext(Cna1979CombatRuleset.Manifest, context.Setup, context.Setup.Artifact, context.Setup.Scenario, changedConfig);
        foreach (var request in new[] { CampaignCombatCreationRequest.Create("foreign.campaign", 0, context), CampaignCombatCreationRequest.Create(test.Request.CampaignId, 1, context),
            CampaignCombatCreationRequest.Create(test.Request.CampaignId, 0, changedContext) })
        {
            var created = CampaignCreatedV11.Create(request);
            var boundary = test.Boundary with
            {
                CreationBinding = request.CreationBinding,
                World = created.InitialWorld,
                RandomState = request.RandomState,
                Cycle = test.Boundary.Cycle with { CampaignId = request.CampaignId, AdmittedPolicyBundleDigest = request.Context.Configuration.Hash }
            };
            var error = Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionSteps.ReplayTrustedBoundary(request, CampaignCreatedV11Serializer.Serialize(created), boundary, [], []));
            Assert.Contains("exact retained compatible C2", error.Message);
        }
        foreach (var boundary in new[] { test.Boundary with { ContractVersion = 2 }, test.Boundary with { CreationBinding = "foreign.creation" },
            test.Boundary with { PriorVersion = 1 }, test.Boundary with { PriorPrefix = "bad" }, test.Boundary with { CompletedBreakdownReceipt = "bad" },
            test.Boundary with { FirstActingSide = LandSide.Commonwealth }, test.Boundary with { Cycle = test.Boundary.Cycle with { GameTurn = 2 } },
            test.Boundary with { Cycle = test.Boundary.Cycle with { OpeningPrefix = "bad" } },
            test.Boundary with { RandomState = new(1, SandtableRandom.AlgorithmId, 1, 0) }, test.Boundary with { RandomState = new(2, SandtableRandom.AlgorithmId, 0, 0) },
            test.Boundary with { RandomState = new(1, "foreign.algorithm", 0, 0) }, test.Boundary with { Weather = test.Boundary.Weather with { GameTurn = 2 } },
            test.Boundary with { Weather = test.Boundary.Weather with { AttackerKind = (WeatherKind)99 } },
            test.Boundary with { World = WithCp(test.Boundary.World, [11, 0]) },
            test.Boundary with { Position = WithSide(test.Boundary.Position, LandSide.Commonwealth) } })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionSteps.ReplayTrustedBoundary(test.Request, test.Created, boundary, [], []));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionSteps.ReplayTrustedBoundary(test.Request, [], test.Boundary, [], []));
        var moved = CombatMovementHistoryReplayTests.SnapshotHistories().First(h => h.Events.Length == 20);
        var actual = CampaignCombatCertification.Admit(moved.Request, moved.Created, moved.Events);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionSteps.ReplayTrustedBoundary(test.Request, test.Created,
            test.Boundary with { World = actual.Entry.Lifecycle.Movement.World }, [], []));
        foreach (var (a, d, empty) in new[] { (5L, 7L, false), (6L, 7L, true), (5L, 8L, true) })
        {
            var boundary = test.Boundary with { World = WithCp(test.Boundary.World, [a, d]), RandomState = new(1, SandtableRandom.AlgorithmId, 0, ulong.MaxValue) };
            var state = CampaignCombatSelectionSteps.ReplayTrustedBoundary(test.Request, test.Created, boundary, [], []);
            var opened = CampaignCombatSelectionSteps.ApplyTrustedBoundary(test.Request, test.Created, boundary, [], [], Structural(state, "open-segment") with { AdmittedAt = empty ? null : 1000 });
            Assert.Equal(empty ? "system-no-selection" : "pending", opened.Control.SelectionOutcome);
            Assert.Equal(ulong.MaxValue, boundary.RandomState.NextByteCursor);
        }
        foreach (var kind in new[] { WeatherKind.Hot, WeatherKind.Sandstorm, WeatherKind.Rainstorm })
            foreach (var attacker in new[] { true, false })
            {
                var boundary = test.Boundary with { Weather = attacker ? test.Boundary.Weather with { AttackerKind = kind } : test.Boundary.Weather with { DefenderKind = kind } };
                var state = CampaignCombatSelectionSteps.ReplayTrustedBoundary(test.Request, test.Created, boundary, [], []);
                Assert.Equal("system-no-selection", CampaignCombatSelectionSteps.ApplyTrustedBoundary(test.Request, test.Created, boundary, [], [], Structural(state, "open-segment")).Control.SelectionOutcome);
            }
    }

    [Fact]
    public void RehashedForgeriesWrongTrustedInputsAndLifecycleConflictsReject()
    {
        using var fixture = Fixture(); var test = Case(fixture, fixture.RootElement.GetProperty("cases")[3]);
        for (var index = 0; index < test.Events.Length; index++)
        {
            foreach (var (path, value) in new (string, JsonNode?)[] { ("priorVersion", JsonValue.Create(1)), ("stateVersion", JsonValue.Create(999)),
                ("priorPrefix", JsonValue.Create(ForeignHash)), ("segmentId", JsonValue.Create("foreign.segment")), ("configurationHash", JsonValue.Create(ForeignHash)),
                ("input.actor", JsonValue.Create("system")), ("input.admittedAt", JsonValue.Create(999999)) })
            {
                var changed = Change(test.Events[index], path, value);
                changed.AsObject().Remove("receiptId"); changed["receiptId"] = "cmb." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.step-receipt.v1", Bytes(changed))[7..];
                // Actor System is unchanged on structural calls; admittedAt999999 is always a change.
                if (Bytes(changed).AsSpan().SequenceEqual(test.Events[index])) continue;
                Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionSteps.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary,
                    test.Inputs[..(index + 1)], [.. test.Events.Take(index), Bytes(changed)]));
            }
        }
        var chosen = test.Inputs[1];
        foreach (var input in new[] { chosen with { Actor = CampaignOpeningPreambleActor.Commonwealth }, chosen with { Command = chosen.Command with { Choice = "finish-without-attack", Candidate = null } },
            chosen with { Command = chosen.Command with { Participant = chosen.Command.Candidate!.Defender.Unit } }, chosen with { Command = chosen.Command with { ContractVersion = 2 } } }) Reject(test, 2, input);
        Reject(test, 5, test.Inputs[5] with { Actor = CampaignOpeningPreambleActor.Axis });
        Reject(test, 5, test.Inputs[5] with { Command = test.Inputs[5].Command with { Participant = chosen.Command.Candidate!.Attacker.Unit } });
        Reject(test, 4, Structural(Replay(test, 4), "complete-step", test.Inputs[4].Command.FromPositionId));
        Reject(test, 7, Structural(Replay(test, 7), "complete-step", "land.position.operation-1.first-player.movement-and-combat.combat.force-assignment"));
        foreach (var cut in new[] { 2, 4, 6 })
        {
            var wrongInputs = test.Inputs[..cut]; wrongInputs[^1] = wrongInputs[^1] with { AdmittedAt = 2222 };
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionSteps.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary, wrongInputs, test.Events[..cut]));
        }
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionSteps.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary, [test.Inputs[0], test.Inputs[0]], [test.Events[0], test.Events[0]]));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionSteps.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary, test.Inputs.Reverse().ToArray(), test.Events.Reverse().ToArray()));
        var state = Replay(test, 1);
        var earlyTimer = new CombatStepsInput(new(1, "expire-window", state.SegmentId, DecisionId: state.SelectionWindow!.DecisionId), CampaignOpeningPreambleActor.System, 1001);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionSteps.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary, [test.Inputs[0], earlyTimer], test.Events[..2]));
        var atMax = test.Boundary with { PriorVersion = long.MaxValue };
        var maxState = CampaignCombatSelectionSteps.ReplayTrustedBoundary(test.Request, test.Created, atMax, [], []);
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionSteps.ApplyTrustedBoundary(test.Request, test.Created, atMax, [], [], Structural(maxState, "open-segment") with { AdmittedAt = 1000 }));
        Assert.Equal(CombatStepsDisposition.NoOp, CampaignCombatSelectionSteps.ApplyTrustedBoundary(test.Request, test.Created, atMax, [], [],
            new(new(1, "expire-window", maxState.SegmentId, DecisionId: "stale.decision"), CampaignOpeningPreambleActor.System)).Disposition);
    }

    [Fact]
    public void HostileBytesPrecedeTrustedContextAndLocalHistoryCapacityPrecedesIndexing()
    {
        using var fixture = Fixture(); var test = Case(fixture, fixture.RootElement.GetProperty("cases")[3]);
        var control = CampaignCombatSelectionStepsCodec.SerializeControl(Replay(test, 5));
        foreach (var source in new[] { test.BoundaryBytes, control })
        {
            var root = JsonNode.Parse(source)!; var text = Encoding.UTF8.GetString(source);
            var unknown = root.DeepClone(); unknown["unknown"] = 0;
            var missing = root.DeepClone(); missing.AsObject().Remove("contractVersion");
            var reverse = new JsonObject(); foreach (var field in root.AsObject().Reverse()) reverse.Add(field.Key, field.Value?.DeepClone());
            foreach (var bad in new[] { Encoding.UTF8.GetBytes("null"), Encoding.UTF8.GetBytes("[]"), Encoding.UTF8.GetBytes("{}"), [.. source, (byte)' '],
                Encoding.UTF8.GetBytes("\uFEFF" + text), Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"contractVersion\":1.0", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\":1", "\"contractVersion\":1,\"contractVersion\":1", StringComparison.Ordinal)),
                Encoding.UTF8.GetBytes(text.Replace("\"contractVersion\"", "\"contract\\u0056ersion\"", StringComparison.Ordinal)), Bytes(unknown), Bytes(missing), Bytes(reverse),
                new byte[1_048_577], Encoding.UTF8.GetBytes(new string('[', 33) + "0" + new string(']', 33)) })
            {
                Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionStepsCodec.ReadBoundary(bad, null!, [], null!));
                Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionStepsCodec.ReadControl(bad, null!, [], null!, new Sentry<CombatStepsInput>(1), new Sentry<byte[]>(1)));
            }
        }
        foreach (var (path, value) in new (string, JsonNode?)[] { ("world.elements", new JsonArray(Enumerable.Repeat<JsonNode?>(null, 513).ToArray())),
            ("cycle.ordinal", JsonValue.Create("one")), ("randomState.seed", JsonValue.Create(-1)), ("reactionWindow", new JsonObject()),
            ("position.sources", null), ("world.elements", null) })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionStepsCodec.ReadBoundary(Bytes(Change(test.BoundaryBytes, path, value)), null!, [], null!));
        foreach (var (path, value) in new (string, JsonNode?)[] { ("rbaWindow.timing.highWaterUnixMilliseconds", JsonValue.Create(-1)),
            ("selection.attacker.unit.elementId", JsonValue.Create(false)), ("selectionWindow", new JsonObject()) })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionStepsCodec.ReadControl(Bytes(Change(control, path, value)), null!, [], null!, new Sentry<CombatStepsInput>(1), new Sentry<byte[]>(1)));
        Assert.Throws<ArgumentNullException>(() => CampaignCombatSelectionStepsCodec.ReadBoundary(test.BoundaryBytes, null!, [], null!));
        Assert.Throws<InvalidOperationException>(() => CampaignCombatSelectionStepsCodec.ReadControl(control, test.Request, test.Created, test.Boundary, new Sentry<CombatStepsInput>(1), new Sentry<byte[]>(1)));
        foreach (var (inputCount, eventCount) in new[] { (17, 17), (0, 1), (1, 0), (-1, -1) })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionSteps.ReplayTrustedBoundary(null!, test.Created, null!, new Sentry<CombatStepsInput>(inputCount), new Sentry<byte[]>(eventCount)));
        foreach (var bad in new[] { Encoding.UTF8.GetBytes("{}"), Bytes(Change(test.Events[0], "effect.kind", JsonValue.Create("unknown"))), [.. test.Events[0], (byte)' '] })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionSteps.ReplayTrustedBoundary(null!, test.Created, null!, new Sentry<CombatStepsInput>(1), [bad]));
        foreach (var (path, value) in new (string, JsonNode?)[] { ("prefix", JsonValue.Create(ForeignHash)), ("selectionReceiptId", JsonValue.Create("forged.receipt")),
            ("stepIndex", JsonValue.Create(5)), ("rbaWindow.timing.deadlineUnixMilliseconds", JsonValue.Create(999999)), ("selection.targetLocationId", JsonValue.Create("axis-supply")) })
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionStepsCodec.ReadControl(Bytes(Change(control, path, value)), test.Request, test.Created, test.Boundary, test.Inputs[..5], test.Events[..5]));
    }

    [Fact]
    public void ImmutableControlsAndOwnedResultsSurviveSourceBufferMutation()
    {
        using var fixture = Fixture(); var test = Case(fixture, fixture.RootElement.GetProperty("cases")[3]);
        var created = test.Created.ToArray(); var inputs = test.Inputs[..5]; var events = test.Events.Take(5).Select(b => b.ToArray()).ToArray();
        var state = CampaignCombatSelectionSteps.ReplayTrustedBoundary(test.Request, created, test.Boundary,
            new MutatingInputs(inputs, () => Array.Fill(created, (byte)0)), events);
        Assert.Equal(CampaignCombatSelectionStepsCodec.SerializeControl(Replay(test, 5)), CampaignCombatSelectionStepsCodec.SerializeControl(state));
        var result = Apply(test, 7, test.Inputs[5]); var before = CampaignCombatSelectionStepsCodec.SerializeControl(result.Control); var expectedEvent = result.EventBytes;
        Array.Fill(result.EventBytes!, (byte)0);
        foreach (var bytes in test.Events.Append(test.Created).Append(test.BoundaryBytes)) Array.Fill(bytes, (byte)0);
        Assert.Equal(expectedEvent, result.EventBytes); Assert.Equal(before, CampaignCombatSelectionStepsCodec.SerializeControl(result.Control));
        Assert.Throws<NotSupportedException>(() => ((IList<string>)result.Control.StepReceipts)[0] = "changed");
        Assert.Throws<NotSupportedException>(() => ((IList<CampaignOpeningPreambleReceipt>)result.Control.Receipts).Clear());
    }

    private static CombatStepsBoundary Mirror(CombatStepsBoundary boundary, LandSide side) => boundary with
    {
        FirstActingSide = side,
        Cycle = boundary.Cycle with { ActingSide = side },
        Position = WithSide(boundary.Position, side),
        World = side == LandSide.Axis ? boundary.World : WithCp(boundary.World, boundary.World.Elements.Reverse().Select(e => e.OperationalState.CapabilityPointsExpended.Numerator).ToArray()),
    };
    private static LandSequencePosition WithSide(LandSequencePosition p, LandSide side) => new(p.ContractVersion, p.PositionId, p.GameTurn,
        p.OperationStage, p.StageId, p.PhaseId, p.SegmentId, p.StepId, p.ActorRole, side, p.Sources);
    private static void Reject(TestCase test, int count, CombatStepsInput input) => Assert.ThrowsAny<JsonException>(() => Apply(test, count, input));
    private const string ForeignHash = "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private static JsonNode Change(byte[] bytes, string path, JsonNode? value)
    {
        var root = JsonNode.Parse(bytes)!; var current = root; var parts = path.Split('.');
        foreach (var part in parts[..^1]) current = current[part]!;
        current[parts[^1]] = value?.DeepClone(); return root;
    }
    private static byte[] Bytes(JsonNode value) => Encoding.UTF8.GetBytes(value.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
    private sealed class Sentry<T>(int count) : IReadOnlyList<T>
    {
        public int Count => count;
        public T this[int index] => throw new InvalidOperationException("Trusted list accessed before hostile bytes/capacity rejected.");
        public IEnumerator<T> GetEnumerator() => throw new InvalidOperationException("Unexpected enumeration.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    private sealed class MutatingInputs(CombatStepsInput[] values, Action mutation) : IReadOnlyList<CombatStepsInput>
    {
        public int Count { get { mutation(); return values.Length; } }
        public CombatStepsInput this[int index] { get { mutation(); return values[index]; } }
        public IEnumerator<CombatStepsInput> GetEnumerator() => throw new InvalidOperationException("Use bounded indexed capture.");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    [Fact]
    public void C3aForbiddenRoutesRejectBeforeTrustedBoundaryContext()
    {
        using var fixture = Fixture(); var test = Case(fixture, fixture.RootElement.GetProperty("cases")[3]);
        var root = JsonNode.Parse(test.BoundaryBytes)!;
        var candidate = JsonNode.Parse(CampaignCombatIdentityCodec.SerializeCandidate(test.Inputs[1].Command.Candidate!))!;
        var settlement = new JsonObject
        {
            ["settlementId"] = "test.settlement",
            ["commitmentId"] = "test.commitment",
            ["resultId"] = "test.result",
            ["gameTurn"] = 1,
            ["operationStage"] = 1,
            ["attacker"] = candidate["attacker"]!["unit"]!.DeepClone(),
            ["defender"] = candidate["defender"]!["unit"]!.DeepClone(),
            ["preLossElements"] = root["world"]!["elements"]!.DeepClone(),
            ["result"] = new JsonObject
            {
                ["differential"] = 0,
                ["attackerCoordinate"] = 11,
                ["defenderCoordinate"] = 11,
                ["captureDie"] = null,
                ["attackerPercent"] = 0,
                ["defenderPercent"] = 0,
                ["rawEngaged"] = false,
                ["requiredRetreat"] = 0,
                ["capturedRole"] = null,
                ["captureShare"] = 0
            },
            ["disposition"] = new JsonObject
            {
                ["receiptId"] = "test.receipt",
                ["predecessorReceiptId"] = "test.predecessor",
                ["kind"] = "test.kind",
                ["requiredDistance"] = 0,
                ["plannedDistance"] = 0,
                ["unfulfilledDistance"] = 0,
                ["route"] = new JsonArray()
            },
            ["losses"] = null,
            ["retreat"] = null,
            ["custody"] = null,
            ["relationships"] = null,
        };
        root["world"]!["settlements"] = new JsonArray(settlement);
        // Frozen C3a env.typed explicitly rejects Route, even the empty route. An
        // ArgumentNullException here means syntax wrongly reached trusted context.
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionStepsCodec.ReadBoundary(Bytes(root), null!, [], null!));
        root = JsonNode.Parse(test.BoundaryBytes)!;
        root["world"]!["brokenVehicleLots"] = new JsonArray(new JsonObject());
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionStepsCodec.ReadBoundary(Bytes(root), null!, [], null!));
    }

    [Fact]
    public void C3aSemanticArrayOrderReachesTrustedContextBeforeSemanticRejection()
    {
        using var fixture = Fixture(); var test = Case(fixture, fixture.RootElement.GetProperty("cases")[3]);
        foreach (var path in new[] { "world.elements", "position.sources" })
        {
            var root = JsonNode.Parse(test.BoundaryBytes)!; var parts = path.Split('.');
            var items = root[parts[0]]![parts[1]]!.AsArray().Reverse().Select(n => n!.DeepClone()).ToArray();
            root[parts[0]]![parts[1]] = new JsonArray(items);
            Assert.Throws<ArgumentNullException>(() => CampaignCombatSelectionStepsCodec.ReadBoundary(Bytes(root), null!, [], null!));
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionStepsCodec.ReadBoundary(Bytes(root), test.Request, test.Created, test.Boundary));
        }
        var control = JsonNode.Parse(CampaignCombatSelectionStepsCodec.SerializeControl(Replay(test, 5)))!;
        control["selection"]!["attacker"]!["componentIds"] = new JsonArray("z.component", "a.component");
        Assert.Throws<InvalidOperationException>(() => CampaignCombatSelectionStepsCodec.ReadControl(Bytes(control), test.Request, test.Created,
            test.Boundary, new Sentry<CombatStepsInput>(1), new Sentry<byte[]>(1)));
        Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionStepsCodec.ReadControl(Bytes(control), test.Request, test.Created, test.Boundary,
            test.Inputs[..5], test.Events[..5]));
    }

    [Fact]
    public void RehashedEffectsCannotInventStepProofsOrDefenderDisposition()
    {
        using var fixture = Fixture(); var test = Case(fixture, fixture.RootElement.GetProperty("cases")[3]);
        foreach (var (index, path, value) in new[] { (2, "effect.previousStepReceiptId", "foreign.receipt"), (2, "effect.dispositionReceiptId", "foreign.receipt"),
            (2, "effect.toPositionId", "foreign.position"), (2, "effect.proofKind", "no-attack"), (5, "effect.participant.elementId", "foreign.element"),
            (5, "effect.selectionReceiptId", "foreign.receipt"), (1, "effect.candidate.targetLocationId", "axis-supply") })
        {
            var changed = Change(test.Events[index], path, JsonValue.Create(value));
            changed.AsObject().Remove("receiptId");
            changed["receiptId"] = "cmb." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.step-receipt.v1", Bytes(changed))[7..];
            Assert.ThrowsAny<JsonException>(() => CampaignCombatSelectionSteps.ReplayTrustedBoundary(test.Request, test.Created, test.Boundary,
                test.Inputs[..(index + 1)], [.. test.Events.Take(index), Bytes(changed)]));
        }
        Reject(test, 2, test.Inputs[2] with { Command = test.Inputs[2].Command with { FromPositionId = "foreign.position" } });
        Reject(test, 2, test.Inputs[2] with { Command = test.Inputs[2].Command with { ExpectedPriorVersion = 1 } });
        Reject(test, 2, test.Inputs[2] with { AdmittedAt = 1001 });
        Reject(test, 2, test.Inputs[2] with { ClockAvailable = false });
        var closed = Case(fixture, fixture.RootElement.GetProperty("cases")[1]); var state = Replay(closed, closed.Events.Length);
        Assert.True(state.Closed);
        Reject(closed, closed.Events.Length, Structural(state, "complete-step", "land.position.operation-1.first-player.reserve-release"));
        Reject(closed, closed.Events.Length, closed.Inputs[2] with { Command = closed.Inputs[2].Command with { ExpectedPriorVersion = state.StateVersion } });
        var timer = new CombatStepsInput(new(1, "expire-window", state.SegmentId, DecisionId: state.SelectionWindow!.DecisionId), CampaignOpeningPreambleActor.System, 999999);
        foreach (var kind in new[] { "expire-window", "controller-unavailable" })
        {
            var result = Apply(closed, closed.Events.Length, timer with { Command = timer.Command with { Kind = kind } });
            Assert.Equal(CombatStepsDisposition.NoOp, result.Disposition);
            Assert.Equal(CampaignCombatSelectionStepsCodec.SerializeControl(state), CampaignCombatSelectionStepsCodec.SerializeControl(result.Control));
        }
    }

    [Fact]
    public void UnavailableBeforeWindowOpeningCompletesNoAttackWithoutSyntheticDecline()
    {
        using var fixture = Fixture(); var original = Case(fixture, fixture.RootElement.GetProperty("cases")[3]);
        foreach (var beforeRba in new[] { false, true })
        {
            var inputs = original.Inputs.Take(beforeRba ? 4 : 0).ToList(); var events = original.Events.Take(inputs.Count).ToList();
            var state = CampaignCombatSelectionSteps.ReplayTrustedBoundary(original.Request, original.Created, original.Boundary, inputs, events);
            var boundaryBytes = original.BoundaryBytes.ToArray();
            if (beforeRba)
                Accept(new(new(1, "controller-unavailable", state.SegmentId, DecisionId: state.SegmentId + ".rba"), CampaignOpeningPreambleActor.System, null, false));
            else
            {
                Accept(Structural(state, "open-segment") with { ClockAvailable = false });
                Accept(Structural(state, "close-empty-selection"));
            }
            var positions = Cna1979LandSequence.CreateTurn(1);
            var start = positions.ToList().FindIndex(p => p.PositionId == original.Boundary.Position.PositionId);
            while (!state.Closed)
            {
                var previous = state.StepReceipts.Count == 0 ? state.Receipts[0].ReceiptId : state.StepReceipts[^1];
                var disposition = state.CancellationReceiptId ?? state.SelectionReceiptId;
                Accept(Structural(state, "complete-step", positions[start + state.StepIndex].PositionId));
                using var step = JsonDocument.Parse(events[^1]); var effect = step.RootElement.GetProperty("effect");
                Assert.Equal("no-attack", effect.GetProperty("proofKind").GetString());
                Assert.Equal(previous, effect.GetProperty("previousStepReceiptId").GetString());
                Assert.Equal(disposition, effect.GetProperty("dispositionReceiptId").GetString());
            }
            Assert.Equal(6, state.StepIndex); Assert.Null(state.DeclineReceiptId); Assert.Null(state.RbaWindow);
            Assert.Equal(boundaryBytes, CampaignCombatSelectionStepsCodec.SerializeBoundary(original.Request, original.Created, original.Boundary));
            void Accept(CombatStepsInput input)
            {
                var result = CampaignCombatSelectionSteps.ApplyTrustedBoundary(original.Request, original.Created, original.Boundary, inputs, events, input);
                Assert.Equal(CombatStepsDisposition.Accepted, result.Disposition);
                inputs.Add(input); events.Add(result.EventBytes!); state = result.Control;
                var bytes = CampaignCombatSelectionStepsCodec.SerializeControl(state);
                Assert.Equal(bytes, CampaignCombatSelectionStepsCodec.SerializeControl(CampaignCombatSelectionStepsCodec.ReadControl(bytes,
                    original.Request, original.Created, original.Boundary, inputs, events)));
            }
        }
    }

    private sealed record TestCase(CampaignCombatCreationRequest Request, byte[] Created, CombatStepsBoundary Boundary,
        byte[] BoundaryBytes, CombatStepsInput[] Inputs, byte[][] Events);
    private static TestCase Case(JsonDocument fixture, JsonElement row)
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
        var boundaryBytes = Encoding.UTF8.GetBytes(fixture.RootElement.GetProperty("boundaries")[row.GetProperty("boundaryIndex").GetInt32()].GetProperty("canonicalUtf8").GetString()!);
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
        return new(request, created, boundary, boundaryBytes,
            row.GetProperty("inputs").EnumerateArray().Select(i => CampaignCombatSelectionStepsCodec.ReadInput(JsonSerializer.SerializeToUtf8Bytes(i))).ToArray(),
            row.GetProperty("events").EnumerateArray().Select(e => Encoding.UTF8.GetBytes(e.GetProperty("canonicalUtf8").GetString()!)).ToArray());
    }
    private static CampaignWorldSnapshotV7 WithCp(CampaignWorldSnapshotV7 world, long[] amounts) => new(7, world.CreationBinding,
        world.Elements.Select((e, index) => new CampaignElementStateV6(e.ElementId, e.CurrentLocationId, e.ReserveStatus,
            new(e.OperationalState.LedgerGameTurn, e.OperationalState.LedgerOperationStage, new(amounts[index], 1), e.OperationalState.CohesionLevel,
                e.OperationalState.VehicleBreakdownState, e.OperationalState.MovementEnded, e.OperationalState.InitialLedgerOrigin), e.Components,
            e.SourceParentFormationId, e.CurrentParentFormationId, e.Ammunition, e.Readiness)), world.Representations, world.BrokenVehicleLots,
        world.CohesionCauses, world.Relationships, world.CustodyLots, world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
    private static CombatStepsControl Replay(TestCase test, int count) => CampaignCombatSelectionSteps.ReplayTrustedBoundary(test.Request, test.Created,
        test.Boundary, test.Inputs[..count], test.Events[..count]);
    private static CombatStepsResult Apply(TestCase test, int count, CombatStepsInput input) => CampaignCombatSelectionSteps.ApplyTrustedBoundary(test.Request,
        test.Created, test.Boundary, test.Inputs[..count], test.Events[..count], input);
    private static CombatStepsInput Structural(CombatStepsControl state, string kind, string? position = null) => new(new(1, kind, state.SegmentId,
        FromPositionId: position, ExpectedPriorVersion: state.StateVersion), CampaignOpeningPreambleActor.System);
    private static LandSide Side(string side) => side == "axis" ? LandSide.Axis : LandSide.Commonwealth;
    private static WeatherKind Weather(string kind) => kind switch { "normal" => WeatherKind.Normal, "hot" => WeatherKind.Hot, "sandstorm" => WeatherKind.Sandstorm, _ => WeatherKind.Rainstorm };
    private static string Hash(byte[] bytes) => CampaignOpeningPreambleCodec.Hash(bytes);
    private static JsonDocument Fixture() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
        "Campaigns", "Fixtures", "combat-selection-steps-v1.json")));
}
