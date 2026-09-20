using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Dormant authenticated Round2 choice and atomic commitment mechanism; no positive-history adapter or publication.</summary>
internal static class CampaignCombatSealedRound
{
    private sealed record Frame(CombatRoundState State, byte[][] Events);

    internal static CombatRoundBase AuthenticateBase(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        CombatStepsBoundary boundary, IReadOnlyList<CombatStepsInput> inputs, IReadOnlyList<byte[]> events)
    {
        Require(created.Length is > 0 and <= 1_048_576, "Missing or oversized Created11.");
        var ownedCreated = created.ToArray();
        var steps = CampaignCombatSelectionSteps.ReplayTrustedBoundary(request, ownedCreated, boundary, inputs, events);
        var context = CampaignCombatSelectionSteps.ValidateBoundary(request, ownedCreated, boundary);
        Require(steps.SelectionOutcome == "selected" && steps.DeclineReceiptId is not null && steps.StepIndex == 3 &&
            steps.StepReceipts.Count == 3 && !steps.Closed && steps.Selection is not null && context.Candidate is not null &&
            CampaignCombatIdentityCodec.SerializeCandidate(steps.Selection).AsSpan().SequenceEqual(CampaignCombatIdentityCodec.SerializeCandidate(context.Candidate)),
            "Base2 requires authenticated selected C3a decline at Force Assignment.");
        return new(boundary, context.BoundaryBytes, steps, new(2, context.Configuration.Hash,
            "sandtable.combat.public-opening-clock.v2", context.Configuration.Windows.Single(w => w.Kind == "force-assignment").DecisionBudgetMilliseconds));
    }

    public static CombatRoundState ReplayTrustedBoundary(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        CombatStepsBoundary boundary, IReadOnlyList<CombatStepsInput> predecessorInputs, IReadOnlyList<byte[]> predecessorEvents,
        IReadOnlyList<CombatRoundInput> inputs, IReadOnlyList<byte[]> events) =>
        Replay(request, created, boundary, predecessorInputs, predecessorEvents, inputs, events).State;

    public static CombatRoundResult ApplyTrustedBoundary(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        CombatStepsBoundary boundary, IReadOnlyList<CombatStepsInput> predecessorInputs, IReadOnlyList<byte[]> predecessorEvents,
        IReadOnlyList<CombatRoundInput> inputs, IReadOnlyList<byte[]> events, CombatRoundInput input, bool admissionEnabled = true)
    {
        var frame = Replay(request, created, boundary, predecessorInputs, predecessorEvents, inputs, events);
        var result = Transition(frame.State, input, admissionEnabled);
        if (result.Disposition != CombatStepsDisposition.Duplicate) return result;
        var index = frame.State.Receipts.ToList().FindIndex(r => r.ReceiptId == result.ReceiptId);
        return new(frame.State, CombatStepsDisposition.Duplicate, frame.Events[index], result.ReceiptId);
    }

    private static Frame Replay(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created, CombatStepsBoundary boundary,
        IReadOnlyList<CombatStepsInput> predecessorInputs, IReadOnlyList<byte[]> predecessorEvents,
        IReadOnlyList<CombatRoundInput> inputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(events);
        Require(created.Length is > 0 and <= 1_048_576, "Missing or oversized Created11.");
        var ownedCreated = created.ToArray();
        var count = events.Count;
        Require(count is >= 0 and <= 16 && inputs.Count == count, "Round2 accepted input/event capacity mismatch.");
        var owned = new byte[count][];
        var trusted = new CombatRoundInput[count];
        for (var index = 0; index < count; index++)
        {
            var bytes = events[index];
            Require(bytes is { Length: > 0 and <= 1_048_576 }, "Missing or oversized Round2 event.");
            owned[index] = bytes.ToArray();
            CampaignCombatSealedRoundCodec.ValidateSyntax(owned[index], "RoundEvent");
        }
        for (var index = 0; index < count; index++)
        {
            trusted[index] = inputs[index];
            _ = CampaignCombatSealedRoundCodec.SerializeInput(trusted[index]);
        }
        var state = new CombatRoundState(AuthenticateBase(request, ownedCreated, boundary, predecessorInputs, predecessorEvents));
        for (var index = 0; index < count; index++)
        {
            // Retained opening remains recoverable when fresh admission is disabled.
            var result = Transition(state, trusted[index], true);
            Require(result.Disposition == CombatStepsDisposition.Accepted && result.EventBytes is { } expected &&
                expected.AsSpan().SequenceEqual(owned[index]), "Round2 event differs from independently trusted input replay.");
            state = result.State;
        }
        return new(state, owned);
    }

    private static CombatRoundResult Transition(CombatRoundState prior, CombatRoundInput input, bool admissionEnabled)
    {
        _ = CampaignCombatSealedRoundCodec.SerializeInput(input);
        var command = input.Command;
        var kind = command.Kind;
        var basis = prior.Base;
        Require(command.ContractVersion == 2 && kind is "open-round" or "seal-choice" or "expire-round" or "controller-unavailable" or "complete-step" or "commit-attack",
            "Unsupported Round2 command/version.");
        Require(command.SegmentId == basis.Steps.SegmentId && command.ClockConfigurationHash == basis.ConfigurationHash,
            "Round2 command context mismatch.");
        Require(kind == "seal-choice" ? input.Actor is CampaignOpeningPreambleActor.Axis or CampaignOpeningPreambleActor.Commonwealth : input.Actor == CampaignOpeningPreambleActor.System,
            "Round2 command actor mismatch.");
        Require(kind == "seal-choice" ? command.Allocation is not null && command.SlotId is not null : command.Allocation is null && command.SlotId is null,
            "Round2 allocation fields mismatch.");
        Require((command.ExpectedPriorVersion is not null) == (kind is "open-round" or "complete-step" or "commit-attack") &&
            (command.RoundId is null) == (kind == "open-round"), "Round2 command field combination mismatch.");
        var commandHash = CampaignOpeningPreambleCodec.Hash(CampaignCombatSealedRoundCodec.SerializeCommand(command));
        var duplicate = prior.Receipts.SingleOrDefault(r => r.CommandHash == commandHash);
        if (duplicate is not null)
        {
            Require(duplicate.Actor == input.Actor, "Round2 retry owner mismatch.");
            return new(prior, CombatStepsDisposition.Duplicate, null, duplicate.ReceiptId);
        }
        if (kind is "expire-round" or "controller-unavailable" && (command.RoundId != prior.RoundId || prior.Status != "collecting"))
            return new(prior, CombatStepsDisposition.NoOp, null, null);
        Require(!prior.Closed && prior.Status != "committed", "Round2 is closed or already committed.");
        Require(prior.World == basis.Boundary.World && prior.AttackHistory.Count == 0 && prior.TargetUses.Count == 0,
            "Round2 precommit facts differ from original authenticated eligibility.");
        if (kind != "open-round") Require(command.RoundId == prior.RoundId, "Round2 round identity mismatch.");
        if (command.ExpectedPriorVersion is { } version) Require(version == prior.StateVersion, "Stale Round2 version.");
        Require(prior.StateVersion < long.MaxValue && prior.Receipts.Count < 16, "Round2 authority capacity exceeded.");
        var next = prior;
        var author = input.Actor;
        CombatRoundEffect effect;
        switch (kind)
        {
            case "open-round":
                Require(admissionEnabled, "Fresh Round2 opening is disabled.");
                Require(prior.Status == "unopened" && prior.StepIndex == 3 && prior.Receipts.Count == 0, "Round2 already opened.");
                Require(input.ClockAvailable && input.AdmittedAt is not null, "Round2 opening requires trusted time.");
                var opened = input.AdmittedAt!.Value;
                var deadline = checked(opened + basis.Configuration.DecisionBudgetMilliseconds);
                Require(deadline <= CampaignCombatSelectionSteps.UtcMaximum, "Round2 deadline overflow.");
                var timing = new CombatRoundTiming(2, basis.ConfigurationHash, "force-assignment", basis.Configuration.DecisionBudgetMilliseconds, opened, deadline, opened);
                var candidate = basis.Steps.Selection!;
                var positions = Route(basis);
                var opportunity = CampaignCombatIdentityCodec.CalculateOpportunityId(basis.BaseHash,
                    CampaignCombatReserveCompletionCodec.CycleId(basis.Boundary.Cycle), positions[3], candidate, basis.Steps.DeclineReceiptId!);
                var round = CampaignCombatSealedRoundCodec.RoundId(prior, opportunity, timing);
                CombatRoundSlot Slot(string role, CampaignCombatParticipant participant) => new(role, participant.Unit.OriginalSide,
                    CampaignCombatSealedRoundCodec.SlotId(round, role), new("full-close-assault", participant.Unit, participant.ComponentIds[0], 10));
                var slots = Array.AsReadOnly(new[] { Slot("attacker", candidate.Attacker), Slot("defender", candidate.Defender) });
                next = prior with { OpportunityId = opportunity, RoundId = round, Timing = timing, Slots = slots, Status = "collecting" };
                effect = new CombatRoundEffect.Open(basis.BaseHash, opportunity, timing, slots);
                break;
            case "seal-choice":
                Require(prior.Status == "collecting" && prior.StepIndex == 3, "Round2 is not collecting.");
                var slot = prior.Slots.SingleOrDefault(s => s.SlotId == command.SlotId);
                Require(slot is not null && slot.Owner == CampaignCombatSealedRoundCodec.Actor(input.Actor) && slot.SealedReceiptId is null && slot.Allocation == command.Allocation,
                    "Round2 live own-slot/allocation mismatch.");
                var gate = Gate(prior.Timing!, input);
                Require(gate != "expired", "Round2 player deadline expired.");
                if (gate == "unavailable")
                {
                    effect = new CombatRoundEffect.Cancel("clock-unavailable", prior.Timing!);
                    author = CampaignOpeningPreambleActor.System;
                }
                else effect = new CombatRoundEffect.Seal(slot!.SlotId, slot.Allocation, prior.Timing!, prior.Slots.Any(s => s.SealedReceiptId is not null));
                break;
            case "expire-round":
            case "controller-unavailable":
                var clock = Gate(prior.Timing!, input);
                if (kind == "expire-round" && clock == "before") return new(prior, CombatStepsDisposition.NoOp, null, null);
                effect = new CombatRoundEffect.Cancel(kind == "controller-unavailable" ? "controller-unavailable" : clock == "expired" ? "deadline" : "clock-unavailable", prior.Timing!);
                break;
            case "complete-step":
                Require(input.AdmittedAt is null && input.ClockAvailable, "Round2 structural input must have no time and available confidence.");
                Require(prior.Status is "prepared" or "cancelled" && prior.StepIndex is >= 3 and <= 5 &&
                    (prior.StepIndex < 5 || prior.Status == "cancelled"), "Round2 structural continuation is unsupported.");
                var route = Route(basis);
                var proofs = prior.Status == "cancelled" ? new[] { prior.CancellationReceiptId! } : prior.Slots.Select(s => s.SealedReceiptId!).ToArray();
                Require(proofs.All(p => p is not null), "Round2 disposition proof missing.");
                effect = new CombatRoundEffect.Step(route[prior.StepIndex], route[prior.StepIndex + 1], prior.StepReceipts[^1], Array.AsReadOnly(proofs));
                break;
            case "commit-attack":
                Require(input.AdmittedAt is null && input.ClockAvailable, "Round2 commitment requires structural input.");
                Require(prior.Status == "prepared" && prior.StepIndex == 5 && prior.StepReceipts.Count == 5 &&
                    prior.Slots.Count == 2 && prior.Slots.All(s => s.SealedReceiptId is not null), "Round2 commitment requires complete Prepared proof.");
                var rules = Cna1979CombatAdjudication.Definition.Costs;
                Require(rules.AttackerCapabilityPoints == 5 && rules.DefenderCapabilityPoints == 3 &&
                    rules.BaseCapabilityPointAllowance == 10 && rules.CommittedToePerRole == 10 && rules.AmmunitionPointsPerToe == 1,
                    "Unsupported Round2 cost profile.");
                var allocations = Array.AsReadOnly(prior.Slots.Select(s => s.Allocation).ToArray());
                var commitment = CampaignCombatSealedRoundCodec.CommitmentId(prior, allocations);
                var costs = new List<CombatRoundCost>();
                for (var index = 0; index < prior.Slots.Count; index++)
                {
                    var unit = prior.Slots[index].Allocation.Unit;
                    var element = prior.World.Elements.Single(e => e.ElementId == unit.ElementId);
                    var cp = element.OperationalState.CapabilityPointsExpended;
                    var cost = index == 0 ? rules.AttackerCapabilityPoints : rules.DefenderCapabilityPoints;
                    var after = checked(cp.Numerator + cost);
                    Require(cp.Denominator == 1 && after <= rules.BaseCapabilityPointAllowance && element.Ammunition.Points == 10,
                        "Round2 selected CP/ammunition eligibility changed.");
                    costs.Add(new(unit, checked((int)cp.Numerator), checked((int)after), 10, 0));
                }
                effect = new CombatRoundEffect.Commit(commitment, allocations, Array.AsReadOnly(costs.ToArray()), basis.Boundary.RandomState);
                break;
            default: throw new JsonException("Unsupported Round2 continuation.");
        }
        var unsigned = CampaignCombatSealedRoundCodec.SerializeEvent(prior, next.RoundId!, input, author, effect, null);
        var receipt = "cmb." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.round-receipt.v2", unsigned)[7..];
        var bytes = CampaignCombatSealedRoundCodec.SerializeEvent(prior, next.RoundId!, input, author, effect, receipt);
        next = effect switch
        {
            CombatRoundEffect.Open => next with { OpeningReceiptId = receipt },
            CombatRoundEffect.Seal seal => next with
            {
                Slots = next.Slots.Select(s => s.SlotId == seal.SlotId ? s with { SealedReceiptId = receipt, SealedAt = input.AdmittedAt } : s).ToArray(),
                Status = seal.Prepared ? "prepared" : "collecting",
            },
            CombatRoundEffect.Commit commit => Commit(next, commit, receipt),
            CombatRoundEffect.Cancel => next with { Status = "cancelled", CancellationReceiptId = receipt },
            CombatRoundEffect.Step => next with { StepIndex = next.StepIndex + 1, StepReceipts = [.. next.StepReceipts, receipt], Closed = next.StepIndex == 5 },
            _ => throw new JsonException("Unsupported Round2 fold."),
        };
        next = next with
        {
            StateVersion = prior.StateVersion + 1,
            Prefix = CampaignOpeningPreambleCodec.EventPrefix(prior.Prefix, bytes),
            Receipts = [.. prior.Receipts, new(commandHash, CampaignOpeningPreambleCodec.Hash(bytes), receipt, input.Actor, prior.StateVersion + 1)],
        };
        _ = CampaignCombatSealedRoundCodec.SerializeState(next);
        return new(next, CombatStepsDisposition.Accepted, bytes, receipt);
    }

    private static CombatRoundState Commit(CombatRoundState state, CombatRoundEffect.Commit effect, string receipt)
    {
        var world = state.World;
        var elements = world.Elements.Select(element =>
        {
            var cost = effect.Costs.Single(c => c.Unit.ElementId == element.ElementId);
            // General ordinary spending permits excess CPA; selected Combat must not.
            Require(cost.AfterCp <= 10, "Round2 selected CP ceiling exceeded.");
            var paid = CampaignCombatSpending.ChargeOrdinary(element.OperationalState, new(cost.AfterCp - cost.BeforeCp, 1),
                10, CampaignCombatSpendCeiling.Ordinary, element.ElementId, receipt, world.CohesionCauses);
            Require(paid.Cause is null && paid.State.CohesionLevel == element.OperationalState.CohesionLevel &&
                paid.State.CapabilityPointsExpended.Numerator == cost.AfterCp, "Round2 cost produced an unsupported Cohesion change.");
            return new CampaignElementStateV6(element.ElementId, element.CurrentLocationId, element.ReserveStatus, paid.State,
                element.Components, element.SourceParentFormationId, element.CurrentParentFormationId,
                new(cost.AfterAmmo, element.Ammunition.InitialAmmunitionOrigin), element.Readiness);
        }).ToArray();
        var paidWorld = new CampaignWorldSnapshotV7(7, world.CreationBinding, elements, world.Representations, world.BrokenVehicleLots,
            world.CohesionCauses, world.Relationships, world.CustodyLots, world.Guards, world.ReplacementEntitlements, world.FutureObligations, world.Settlements);
        var basis = state.Base;
        var candidate = basis.Steps.Selection!;
        return state with
        {
            World = paidWorld,
            Status = "committed",
            CommitmentId = effect.CommitmentId,
            AttackHistory = [new(effect.CommitmentId, CampaignCombatReserveCompletionCodec.CycleId(basis.Boundary.Cycle), basis.Steps.SegmentId,
                candidate.Attacker.Unit, candidate.Defender.Unit, candidate.TargetLocationId, basis.Boundary.Cycle.GameTurn, basis.Boundary.Cycle.OperationStage)],
            TargetUses = [new(effect.CommitmentId, basis.Steps.SegmentId, candidate.TargetLocationId)],
        };
    }

    private static string Gate(CombatRoundTiming timing, CombatRoundInput input) =>
        !input.ClockAvailable || input.AdmittedAt is null || input.AdmittedAt < timing.OpeningFloorUnixMilliseconds ? "unavailable" :
        input.AdmittedAt >= timing.DeadlineUnixMilliseconds ? "expired" : "before";
    private static string[] Route(CombatRoundBase basis)
    {
        var positions = Cna.Core.Rules.Cna1979LandSequence.CreateTurn(1).ToArray();
        var start = Array.FindIndex(positions, p => p.PositionId == basis.Boundary.Position.PositionId);
        return positions.Skip(start).Take(7).Select(p => p.PositionId).ToArray();
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new JsonException(message);
    }
}
