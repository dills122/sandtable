using System.Text.Json;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>
/// Dormant C3a mechanism over independently trusted Boundary and authenticated Input values.
/// Does not authenticate positive campaign history or expose a live admission/publication adapter.
/// </summary>
internal static class CampaignCombatSelectionSteps
{
    internal const long UtcMaximum = 253402300799999;
    // Frozen C3a oracle Context().request(), committed in authority-envelope-v1 fixture.
    // Request derives this from the full canonical request; no runtime fixture is loaded.
    private const string CompatibleCreation = "creation.dc1c1bff9db6122758ab2b06360ba2131231fe2b63871780e416c8fd6ba4490b";

    internal sealed record ValidatedBoundary(CombatStepsBoundary Boundary, byte[] BoundaryBytes,
        CampaignCombatCandidate? Candidate, CombatDecisionConfiguration Configuration, LandSequencePosition[] Route);
    private sealed record ReplayFrame(ValidatedBoundary Context, CombatStepsControl Control, byte[][] Events);

    internal static ValidatedBoundary ValidateBoundary(CampaignCombatCreationRequest request,
        ReadOnlySpan<byte> created, CombatStepsBoundary boundary)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(boundary);
        ArgumentNullException.ThrowIfNull(boundary.Cycle);
        ArgumentNullException.ThrowIfNull(boundary.RandomState);
        ArgumentNullException.ThrowIfNull(boundary.Weather);
        var initial = CampaignCreatedV11Serializer.Deserialize(created, request).InitialWorld;
        Require(request.CreationBinding == CompatibleCreation && boundary.CreationBinding == request.CreationBinding,
            "C3a requires its exact retained compatible C2 request.");
        Require(boundary.ContractVersion == 1 && boundary.Cycle.OpenedAuthorityVersion <= boundary.PriorVersion,
            "C3a Boundary version or opening authority mismatch.");
        var candidate = CampaignCombatCertification.CertifyInitialProfileFacts(request, created, boundary.World, boundary.Cycle,
            boundary.FirstActingSide, boundary.Weather.AttackerKind, boundary.Weather.DefenderKind);
        Require(boundary.RandomState.ContractVersion == 1 && boundary.RandomState.AlgorithmId == SandtableRandom.AlgorithmId &&
            boundary.RandomState.Seed == request.RandomState.Seed, "C3a random provenance mismatch.");
        Require(boundary.Weather.GameTurn == boundary.Cycle.GameTurn && boundary.Weather.OperationStage == boundary.Cycle.OperationStage,
            "C3a Weather scope mismatch.");
        var catalog = Cna1979LandSequence.CreateTurn(1).ToArray();
        var start = Array.FindIndex(catalog, p => p.PositionId == "land.position.operation-1.first-player.movement-and-combat.combat.position-determination");
        var route = catalog.Skip(start).Take(7).Select(p => new LandSequencePosition(5, p.PositionId, p.GameTurn, p.OperationStage,
            p.StageId, p.PhaseId, p.SegmentId, p.StepId, p.ActorRole, boundary.Cycle.ActingSide, p.Sources)).ToArray();
        Require(PositionBytes(boundary.Position).AsSpan().SequenceEqual(PositionBytes(route[0])), "C3a Position Determination mismatch.");
        var bytes = CampaignCombatSelectionStepsCodec.WriteValidatedBoundary(request, initial, boundary);
        return new(boundary, bytes, candidate, request.Context.Configuration, route);
    }

    public static CombatStepsControl ReplayTrustedBoundary(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        CombatStepsBoundary boundary, IReadOnlyList<CombatStepsInput> trustedAcceptedInputs, IReadOnlyList<byte[]> acceptedEvents) =>
        Replay(request, created, boundary, trustedAcceptedInputs, acceptedEvents).Control;

    public static CombatStepsResult ApplyTrustedBoundary(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        CombatStepsBoundary boundary, IReadOnlyList<CombatStepsInput> trustedAcceptedInputs, IReadOnlyList<byte[]> acceptedEvents,
        CombatStepsInput trustedInput)
    {
        var replay = Replay(request, created, boundary, trustedAcceptedInputs, acceptedEvents);
        var result = Transition(replay.Context, replay.Control, trustedInput);
        if (result.Disposition != CombatStepsDisposition.Duplicate) return result;
        var index = replay.Control.Receipts.ToList().FindIndex(r => r.ReceiptId == result.ReceiptId);
        return new(replay.Control, CombatStepsDisposition.Duplicate, replay.Events[index], result.ReceiptId);
    }

    private static ReplayFrame Replay(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        CombatStepsBoundary boundary, IReadOnlyList<CombatStepsInput> trustedInputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(trustedInputs);
        ArgumentNullException.ThrowIfNull(events);
        Require(created.Length is > 0 and <= 1_048_576, "Missing or oversized retained Created11.");
        var ownedCreated = created.ToArray();
        var eventCount = events.Count;
        var inputCount = trustedInputs.Count;
        Require(eventCount is >= 0 and <= 16 && inputCount == eventCount, "C3a accepted input/event capacity or count mismatch.");
        var ownedEvents = new byte[eventCount][];
        var inputs = new CombatStepsInput[inputCount];
        for (var index = 0; index < eventCount; index++)
        {
            var bytes = events[index];
            Require(bytes is { Length: > 0 and <= 1_048_576 }, "Missing or oversized accepted C3a event.");
            ownedEvents[index] = bytes.ToArray();
            CampaignCombatSelectionStepsCodec.ValidateSyntax(ownedEvents[index], "Event");
        }
        for (var index = 0; index < inputCount; index++)
        {
            inputs[index] = trustedInputs[index];
            _ = CampaignCombatSelectionStepsCodec.SerializeInput(inputs[index]);
        }
        var context = ValidateBoundary(request, ownedCreated, boundary);
        var state = new CombatStepsControl(Hash(context.BoundaryBytes),
            "seg." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.segment.v1", context.BoundaryBytes)[7..], boundary.PriorVersion, boundary.PriorPrefix);
        for (var index = 0; index < eventCount; index++)
        {
            var result = Transition(context, state, inputs[index]);
            Require(result.Disposition == CombatStepsDisposition.Accepted && ownedEvents[index].AsSpan().SequenceEqual(result.EventBytes),
                "C3a event differs from independently trusted replay.");
            state = result.Control;
        }
        return new(context, state, ownedEvents);
    }

    private static CombatStepsResult Transition(ValidatedBoundary context, CombatStepsControl prior, CombatStepsInput input)
    {
        _ = CampaignCombatSelectionStepsCodec.SerializeInput(input);
        var command = input.Command;
        AuthorizeFields(prior, input);
        var commandHash = Hash(CampaignCombatSelectionStepsCodec.SerializeCommand(command));
        var duplicate = prior.Receipts.FirstOrDefault(r => r.CommandHash == commandHash);
        if (duplicate is not null)
        {
            Require(duplicate.Actor == input.Actor, "C3a retry actor mismatch.");
            return new(prior, CombatStepsDisposition.Duplicate, null, duplicate.ReceiptId);
        }
        var window = ActiveWindow(prior);
        var beforeRbaUnavailable = command.Kind == "controller-unavailable" && prior.SelectionOutcome == "selected" &&
            prior.StepIndex == 2 && prior.RbaWindow is null && command.DecisionId == prior.SegmentId + ".rba";
        if (!beforeRbaUnavailable && command.Kind is "expire-window" or "controller-unavailable" &&
            (window is null || command.DecisionId != window.DecisionId)) return NoOp(prior);
        Require(!prior.Closed && prior.Receipts.Count < 16 && prior.StateVersion < long.MaxValue, "C3a lifecycle or capacity exhausted.");
        if (command.Kind is "open-segment" or "close-empty-selection" or "complete-step" or "open-rba")
            Require(command.ExpectedPriorVersion == prior.StateVersion, "C3a stale structural version.");
        if (command.Kind is "complete-step" or "open-rba")
            Require(command.FromPositionId == context.Route[prior.StepIndex].PositionId, "C3a structural position mismatch.");
        if (command.Kind is "complete-step" or "close-empty-selection")
            Require(input.AdmittedAt is null && input.ClockAvailable, "Structural C3a input requires no time and available clock.");

        CombatStepsEffect effect;
        switch (command.Kind)
        {
            case "open-segment":
                Require(prior.SelectionOutcome == "unopened" && prior.Receipts.Count == 0, "C3a segment already opened.");
                Require(context.Candidate is null || !input.ClockAvailable || input.AdmittedAt is not null, "Reliable opening requires UTC.");
                Require(context.Candidate is not null || input.AdmittedAt is null, "No-candidate opening takes no time.");
                var opened = context.Candidate is not null && input.ClockAvailable ? OpenWindow(context, prior, "selection", input.AdmittedAt!.Value) : null;
                effect = new CombatStepsEffect.Open(prior.BoundaryHash, opened);
                break;
            case "close-empty-selection":
                Require(prior.SelectionOutcome == "system-no-selection" && prior.SelectionWindow is null, "C3a empty closure has no proof.");
                effect = new CombatStepsEffect.Selection("no-selection", null, null);
                break;
            case "choose-selection":
                Require(prior.SelectionOutcome == "pending" && window is not null && command.DecisionId == window.DecisionId, "C3a selection is not live.");
                Require(input.Actor == Actor(window!.Owner), "C3a selection owner mismatch.");
                Require(ClockGate(window.Timing, input) == Clock.Before, "C3a selection clock rejected.");
                Require(command.Choice is "select-close-assault" or "finish-without-attack", "Unsupported C3a selection choice.");
                var selected = command.Choice == "select-close-assault";
                Require(selected ? SameCandidate(command.Candidate, context.Candidate) : command.Candidate is null, "C3a candidate mismatch.");
                effect = new CombatStepsEffect.Selection(selected ? "selected" : "no-selection", selected ? context.Candidate : null, AdmittedTiming(window, input));
                break;
            case "open-rba":
                Require(prior.StepIndex == 2 && prior.SelectionOutcome == "selected" && prior.RbaWindow is null, "C3a RBA is not ready.");
                Require(input.ClockAvailable && input.AdmittedAt is not null && input.AdmittedAt >= prior.SelectionWindow!.Timing.HighWaterUnixMilliseconds,
                    "C3a RBA opening clock rejected.");
                effect = new CombatStepsEffect.RbaOpen(prior.SelectionReceiptId!, OpenWindow(context, prior, "rba", input.AdmittedAt!.Value));
                break;
            case "decline-rba":
                Require(prior.StepIndex == 2 && prior.SelectionOutcome == "selected" && window is not null && command.DecisionId == window.DecisionId,
                    "C3a RBA decline is not live.");
                Require(input.Actor == Actor(window!.Owner) && command.Participant == prior.Selection!.Defender.Unit, "C3a decline participant/owner mismatch.");
                Require(ClockGate(window.Timing, input) == Clock.Before, "C3a decline clock rejected.");
                effect = new CombatStepsEffect.Decline(prior.SelectionReceiptId!, command.Participant!, AdmittedTiming(window, input));
                break;
            case "expire-window":
            case "controller-unavailable":
                if (beforeRbaUnavailable) effect = new CombatStepsEffect.Cancel(prior.SelectionReceiptId!, null);
                else
                {
                    Require(window is not null, "C3a fallback has no live window.");
                    if (command.Kind == "expire-window" && ClockGate(window!.Timing, input) == Clock.Before) return NoOp(prior);
                    effect = prior.SelectionOutcome == "pending"
                        ? new CombatStepsEffect.Selection("no-selection", null, AdmittedTiming(window!, input))
                        : new CombatStepsEffect.Cancel(prior.SelectionReceiptId!, AdmittedTiming(window!, input));
                }
                break;
            default:
                Require(prior.SelectionOutcome is "selected" or "no-selection" or "cancelled", "C3a disposition is unresolved.");
                var noAttack = prior.SelectionOutcome != "selected";
                Require(noAttack || prior.StepIndex < 3, "Positive Force Assignment requires later Prepared contract.");
                Require(noAttack || prior.StepIndex != 2 || prior.DeclineReceiptId is not null, "C3a RBA requires accepted decline.");
                var disposition = prior.CancellationReceiptId ?? (prior.StepIndex >= 2 ? prior.DeclineReceiptId : null) ?? prior.SelectionReceiptId;
                effect = new CombatStepsEffect.Step(context.Route[prior.StepIndex].PositionId, context.Route[prior.StepIndex + 1].PositionId,
                    prior.StepReceipts.Count == 0 ? prior.Receipts[0].ReceiptId : prior.StepReceipts[^1], disposition!,
                    noAttack ? "no-attack" : prior.StepIndex switch { 0 => "no-gun-positions", 1 => "no-barrage-work", _ => "accepted-decline" });
                break;
        }
        var unsigned = CampaignCombatSelectionStepsCodec.SerializeEvent(context.Boundary, prior, input, effect, null);
        var receiptId = "cmb." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.step-receipt.v1", unsigned)[7..];
        var bytes = CampaignCombatSelectionStepsCodec.SerializeEvent(context.Boundary, prior, input, effect, receiptId);
        var state = Fold(prior, effect, receiptId) with
        {
            StateVersion = prior.StateVersion + 1,
            Prefix = CampaignOpeningPreambleCodec.EventPrefix(prior.Prefix, bytes),
            Receipts = prior.Receipts.Append(new(commandHash, Hash(bytes), receiptId, input.Actor, prior.StateVersion + 1)).ToArray(),
        };
        _ = CampaignCombatSelectionStepsCodec.SerializeControl(state);
        return new(state, CombatStepsDisposition.Accepted, bytes, receiptId);
    }

    private static CombatStepsControl Fold(CombatStepsControl prior, CombatStepsEffect effect, string receipt) => effect switch
    {
        CombatStepsEffect.Open open => prior with { SelectionOutcome = open.Window is null ? "system-no-selection" : "pending", SelectionWindow = open.Window },
        CombatStepsEffect.Selection selection => prior with
        {
            SelectionOutcome = selection.Outcome,
            Selection = selection.Candidate,
            SelectionReceiptId = receipt,
            SelectionWindow = prior.SelectionWindow is null ? null : prior.SelectionWindow with { Timing = selection.Timing! },
        },
        CombatStepsEffect.RbaOpen rba => prior with { RbaWindow = rba.Window },
        CombatStepsEffect.Decline decline => prior with { DeclineReceiptId = receipt, RbaWindow = prior.RbaWindow! with { Timing = decline.Timing } },
        CombatStepsEffect.Cancel cancel => prior with
        {
            SelectionOutcome = "cancelled",
            CancellationReceiptId = receipt,
            RbaWindow = prior.RbaWindow is null ? null : prior.RbaWindow with { Timing = cancel.Timing! },
        },
        CombatStepsEffect.Step => prior with { StepIndex = prior.StepIndex + 1, StepReceipts = prior.StepReceipts.Append(receipt).ToArray(), Closed = prior.StepIndex == 5 },
        _ => throw new JsonException("Unknown C3a effect."),
    };

    private static void AuthorizeFields(CombatStepsControl state, CombatStepsInput input)
    {
        var command = input.Command;
        string[] allowed = command.Kind switch
        {
            "open-segment" or "close-empty-selection" => ["version"],
            "choose-selection" => ["decision", "choice", "candidate"],
            "decline-rba" => ["decision", "participant"],
            "complete-step" or "open-rba" => ["position", "version"],
            "expire-window" or "controller-unavailable" => ["decision"],
            _ => throw new JsonException("Unsupported C3a command kind."),
        };
        Require(command.ContractVersion == 1 && command.SegmentId == state.SegmentId, "C3a command identity mismatch.");
        foreach (var (name, present) in new[] { ("decision", command.DecisionId is not null), ("position", command.FromPositionId is not null),
            ("version", command.ExpectedPriorVersion is not null), ("choice", command.Choice is not null), ("candidate", command.Candidate is not null), ("participant", command.Participant is not null) })
            Require(!present || allowed.Contains(name, StringComparer.Ordinal), "Unused C3a command field must be null.");
        var player = command.Kind is "choose-selection" or "decline-rba";
        Require(player ? input.Actor is CampaignOpeningPreambleActor.Axis or CampaignOpeningPreambleActor.Commonwealth : input.Actor == CampaignOpeningPreambleActor.System,
            "C3a command actor category mismatch.");
    }
    private static CombatStepsWindow? ActiveWindow(CombatStepsControl state) => state.SelectionOutcome == "pending" ? state.SelectionWindow :
        state.SelectionOutcome == "selected" && state.RbaWindow is not null && state.DeclineReceiptId is null ? state.RbaWindow : null;
    private enum Clock { Before, Expired, Unavailable }
    private static Clock ClockGate(CombatStepsTiming timing, CombatStepsInput input) => !input.ClockAvailable || input.AdmittedAt is null ||
        input.AdmittedAt < timing.HighWaterUnixMilliseconds ? Clock.Unavailable : input.AdmittedAt >= timing.DeadlineUnixMilliseconds ? Clock.Expired : Clock.Before;
    private static CombatStepsTiming AdmittedTiming(CombatStepsWindow window, CombatStepsInput input) => window.Timing with
    {
        HighWaterUnixMilliseconds = input.AdmittedAt is { } time ? Math.Max(time, window.Timing.HighWaterUnixMilliseconds) : window.Timing.HighWaterUnixMilliseconds,
    };
    private static CombatStepsWindow OpenWindow(ValidatedBoundary context, CombatStepsControl state, string kind, long now)
    {
        var budget = context.Configuration.Windows.Single(w => w.Kind == kind).DecisionBudgetMilliseconds;
        Require(now >= 0 && now <= UtcMaximum - budget, "C3a deadline exceeds UTC bound.");
        var owner = kind == "selection" ? context.Boundary.Cycle.ActingSide :
            context.Boundary.Cycle.ActingSide == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;
        return new(state.SegmentId + "." + kind, owner, new(1, context.Configuration.Hash, kind, budget, now, now + budget, now));
    }
    private static bool SameCandidate(CampaignCombatCandidate? left, CampaignCombatCandidate? right) => left is not null && right is not null &&
        CampaignCombatIdentityCodec.SerializeCandidate(left).AsSpan().SequenceEqual(CampaignCombatIdentityCodec.SerializeCandidate(right));
    private static CampaignOpeningPreambleActor Actor(LandSide side) => side == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth;
    private static CombatStepsResult NoOp(CombatStepsControl state) => new(state, CombatStepsDisposition.NoOp, null, null);
    private static string Hash(byte[] bytes) => CampaignOpeningPreambleCodec.Hash(bytes);
    private static void Require(bool condition, string message) { if (!condition) throw new JsonException(message); }
    private static byte[] PositionBytes(LandSequencePosition position)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            CampaignV11CanonicalCodec.WritePosition(writer, "position", position);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
}
