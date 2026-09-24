using System.Text.Json;
using Cna.Core.Rules;
using Codec = Cna.Core.Campaigns.CampaignCombatInheritedCycleControlCodec;

namespace Cna.Core.Campaigns;

/// <summary>Owns retained Release inputs/bytes; admission always replays the complete predecessor.</summary>
internal sealed class CombatInheritedCycleControlSource
{
    private readonly CombatReleaseInput[] inputs;
    private readonly byte[][] events;
    public CombatInheritedCycleControlSource(CombatInheritedReserveReleaseSource source,
        IReadOnlyList<CombatReleaseInput> inputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(source); ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(events);
        if (inputs.Count != 3 || events.Count != 3) throw new JsonException("Control requires complete inherited Release.");
        Source = source; this.inputs = inputs.ToArray(); this.events = events.Select(Codec.Copy).ToArray();
        foreach (var input in this.inputs) _ = CampaignCombatReserveReleaseCodec.SerializeInput(input);
    }
    public CombatInheritedReserveReleaseSource Source { get; }
    internal CombatCycleControlBasis Derive() => new(CampaignCombatArmedContinuation.Derive(Source, inputs, events),
        inputs.ToArray(), events.Select(Codec.Copy).ToArray());
}
internal sealed record CombatCycleControlBasis(CampaignCombatArmedContinuation.Proof Proof,
    IReadOnlyList<CombatReleaseInput> ReleaseInputs, IReadOnlyList<byte[]> ReleaseEvents);
internal sealed record CombatCycleControlCommand(int ContractVersion, string Kind, string ControlId, string CycleId,
    long? ExpectedPriorVersion, string? DecisionId = null);
internal sealed record CombatCycleControlInput(CombatCycleControlCommand Command, CampaignOpeningPreambleActor Actor,
    long? AdmittedAt = null, bool ClockAvailable = true);
internal sealed record CombatCycleClosure(string CycleId, int Ordinal, string Outcome, string ReceiptId);
internal sealed record CombatCycleControlState(CombatCycleControlBasis Basis, string BaseHash, string ControlId,
    long StateVersion, string Prefix, string PositionId, CampaignCombatCycleAuthority? ActiveCycle)
{
    private IReadOnlyList<CombatReleaseMember> members = Array.Empty<CombatReleaseMember>();
    private IReadOnlyList<CampaignOpeningPreambleReceipt> receipts = Array.Empty<CampaignOpeningPreambleReceipt>();
    public string Status { get; init; } = "unopened";
    public CombatCycleClosure? Closure { get; init; }
    public string? DecisionId { get; init; }
    public CombatStepsTiming? Timing { get; init; }
    public bool OpeningClockFailure { get; init; }
    public long? AcceptedHighWater { get; init; }
    public IReadOnlyList<CombatReleaseMember> Members { get => members; init => members = Array.AsReadOnly(value.ToArray()); }
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get => receipts; init => receipts = Array.AsReadOnly(value.ToArray()); }
    public CampaignWorldSnapshotV7 World => Basis.Proof.Source.World;
}
internal sealed class CombatCycleControlResult(CombatCycleControlState state, CombatStepsDisposition disposition, byte[]? eventBytes, string? receiptId)
{
    private readonly byte[]? bytes = eventBytes?.ToArray();
    public CombatCycleControlState State { get; } = state;
    public CombatStepsDisposition Disposition { get; } = disposition;
    public byte[]? EventBytes => bytes?.ToArray();
    public string? ReceiptId { get; } = receiptId;
}

/// <summary>Frozen3k control only. Does not execute the next Movement or activate public gameplay.</summary>
internal static class CampaignCombatInheritedCycleControl
{
    public static CombatCycleControlState Replay(CombatInheritedCycleControlSource source,
        IReadOnlyList<CombatCycleControlInput> inputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(source);
        var (trusted, retained) = Capture(inputs, events);
        var basis = source.Derive(); var release = basis.Proof.Source.Release;
        var nested = Codec.SerializeNestedBase(basis); var hash = CampaignOpeningPreambleCodec.Hash(nested);
        var state = new CombatCycleControlState(basis, hash,
            "ctl." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.cycle-control.v1", nested)[7..],
            release.StateVersion, release.Prefix, basis.Proof.Source.Base.ReleaseBase.PositionId,
            basis.Proof.Source.Base.ReleaseBase.Cycle)
        { Members = release.Members, AcceptedHighWater = release.AcceptedHighWater };
        _ = Codec.SerializeState(state);
        for (var i = 0; i < trusted.Length; i++)
        {
            var result = Transition(state, trusted[i]);
            Require(result.Disposition == CombatStepsDisposition.Accepted && retained[i].AsSpan().SequenceEqual(result.EventBytes),
                "Control event differs from independently admitted replay.");
            state = result.State;
        }
        return state;
    }
    public static CombatCycleControlResult Apply(CombatInheritedCycleControlSource source,
        IReadOnlyList<CombatCycleControlInput> inputs, IReadOnlyList<byte[]> events, CombatCycleControlInput input,
        byte[]? cachedState = null)
    {
        var cache = cachedState is null ? null : Codec.Copy(cachedState);
        var (trusted, retained) = Capture(inputs, events);
        var state = Replay(source, trusted, retained);
        if (cache is not null) Require(cache.AsSpan().SequenceEqual(Codec.SerializeState(state)), "Control cache differs from replay.");
        var result = Transition(state, input);
        if (result.Disposition != CombatStepsDisposition.Duplicate) return result;
        var index = state.Receipts.ToList().FindIndex(r => r.ReceiptId == result.ReceiptId);
        return new(state, result.Disposition, retained[index], result.ReceiptId);
    }
    public static CombatCycleControlCommand Command(CombatCycleControlState state, string kind) => new(1, kind, state.ControlId,
        CampaignCombatReserveCompletionCodec.CycleId(state.Basis.Proof.Source.Base.ReleaseBase.Cycle),
        kind is "expire" or "unavailable" ? null : state.StateVersion, kind == "open" ? null : state.DecisionId);

    private static (CombatCycleControlInput[], byte[][]) Capture(IReadOnlyList<CombatCycleControlInput> inputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(events);
        Require(inputs.Count == events.Count && events.Count <= 2, "Control suffix capacity/count mismatch.");
        var trusted = inputs.ToArray(); var retained = events.Select(Codec.Copy).ToArray();
        foreach (var input in trusted) _ = Codec.SerializeInput(input);
        return (trusted, retained);
    }
    private static CombatCycleControlResult Transition(CombatCycleControlState prior, CombatCycleControlInput input)
    {
        _ = Codec.SerializeInput(input);
        var command = input.Command; var kind = command.Kind; var actor = input.Actor;
        var basis = prior.Basis.Proof.Source.Base.ReleaseBase; var cycle = basis.Cycle;
        var owner = cycle.ActingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth;
        var timer = kind is "expire" or "unavailable";
        Require(command.ContractVersion == 1 && kind is "open" or "repeat" or "finish" or "expire" or "unavailable" or "fallback-step", "Unsupported control command.");
        Require(command.ControlId == prior.ControlId && command.CycleId == CampaignCombatReserveCompletionCodec.CycleId(cycle), "Control/cycle binding mismatch.");
        Require(actor == (kind is "repeat" or "finish" ? owner : CampaignOpeningPreambleActor.System), "Control actor mismatch.");
        Require((command.ExpectedPriorVersion is null) == timer && (kind != "open" || command.DecisionId is null), "Control command field mismatch.");
        var hash = CampaignOpeningPreambleCodec.Hash(Codec.SerializeCommand(command));
        var duplicate = prior.Receipts.FirstOrDefault(r => r.CommandHash == hash && r.Actor == actor);
        if (duplicate is not null) return new(prior, CombatStepsDisposition.Duplicate, null, duplicate.ReceiptId);
        if (timer && (prior.Status != "open" || command.DecisionId != prior.DecisionId)) return new(prior, CombatStepsDisposition.NoOp, null, null);
        Require(prior.Status is "unopened" or "open" && prior.Receipts.Count < 2 && prior.StateVersion < long.MaxValue, "Control terminal/capacity fault.");
        Require(timer || command.ExpectedPriorVersion == prior.StateVersion, "Stale control version.");
        Require(kind == "open" || command.DecisionId == prior.DecisionId, "Stale control decision.");
        var next = prior; var now = input.AdmittedAt; var author = actor; var reason = "owner-choice"; string effect;
        if (kind == "open")
        {
            Require(prior.Status == "unopened", "Control already opened.");
            Require(cycle.Ordinal < int.MaxValue && prior.StateVersion <= long.MaxValue - 2, "Control open/closure capacity exceeded.");
            var lost = !input.ClockAvailable || now is null || now < prior.AcceptedHighWater;
            var config = prior.Basis.Proof.Request.Context.Configuration;
            var budget = config.Windows.Single(w => w.Kind == "cycle-control").DecisionBudgetMilliseconds;
            CombatStepsTiming? timing = !lost && now <= CampaignCombatSelectionSteps.UtcMaximum - budget
                ? new(1, config.Hash, "cycle-control", budget, now!.Value, checked(now.Value + budget), now.Value) : null;
            next = next with
            {
                Status = "open",
                DecisionId = prior.ControlId + ".decision",
                Timing = timing,
                OpeningClockFailure = timing is null,
                AcceptedHighWater = timing is null ? prior.AcceptedHighWater : now
            };
            effect = "control-opened";
        }
        else
        {
            Require(prior.Status == "open", "Control choice window closed.");
            var lost = prior.OpeningClockFailure || !input.ClockAvailable || now is null || now < prior.AcceptedHighWater;
            var late = prior.Timing is not null && now >= prior.Timing.DeadlineUnixMilliseconds;
            if (kind == "fallback-step") Require(prior.OpeningClockFailure, "Control fallback not active.");
            if (kind == "expire" && !lost && !late) return new(prior, CombatStepsDisposition.NoOp, null, null);
            if (kind is "repeat" or "finish" && !lost) Require(!late, "Control owner input at or after deadline.");
            if (!lost) next = next with { AcceptedHighWater = now, Timing = prior.Timing! with { HighWaterUnixMilliseconds = now!.Value } };
            if (lost || timer || kind == "fallback-step")
            {
                author = CampaignOpeningPreambleActor.System; effect = "phase-finished";
                reason = prior.OpeningClockFailure ? "opening-clock-unavailable" : lost ? "clock-unavailable" : kind == "expire" ? "deadline" : "controller-unavailable";
            }
            else effect = kind == "repeat" ? "cycle-repeated" : "phase-finished";
        }
        CampaignCombatCycleAuthority? successor = null;
        if (effect == "cycle-repeated")
        {
            Require(cycle.Ordinal < int.MaxValue, "Control ordinal overflow.");
            successor = cycle with { Ordinal = checked(cycle.Ordinal + 1), OpenedAuthorityVersion = checked(prior.StateVersion + 1), OpeningPrefix = prior.Prefix };
            _ = CampaignCombatReserveCompletionCodec.CycleId(successor);
            next = next with { Status = "repeated", ActiveCycle = successor, PositionId = SuccessorPosition(cycle, true) };
        }
        else if (effect == "phase-finished") next = next with { Status = "finished", ActiveCycle = null, PositionId = SuccessorPosition(cycle, false) };
        var expired = effect == "phase-finished" ? next.Members.Where(m => m.History.NextMovement?.Status == "pending").Select(m => m.Unit).ToArray() : [];
        var unsigned = Codec.Event(prior, next, input, author, effect, reason, successor, expired, null);
        var receipt = "cc." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.cycle-control-receipt.v1", unsigned)[7..];
        var bytes = Codec.Event(prior, next, input, author, effect, reason, successor, expired, receipt);
        next = next with
        {
            StateVersion = checked(prior.StateVersion + 1),
            Prefix = CampaignOpeningPreambleCodec.EventPrefix(prior.Prefix, bytes),
            Closure = effect == "control-opened" ? null : new(command.CycleId, cycle.Ordinal, next.Status, receipt),
            Members = next.Members.Select(m => expired.Contains(m.Unit) ? m with
            {
                History = m.History with
                { NextMovement = m.History.NextMovement! with { Status = "expired", CompletionReceiptId = receipt } }
            } : m).ToArray(),
            Receipts = [.. prior.Receipts, new(hash, CampaignOpeningPreambleCodec.Hash(bytes), receipt, actor, checked(prior.StateVersion + 1))]
        };
        _ = Codec.SerializeState(next);
        return new(next, CombatStepsDisposition.Accepted, bytes, receipt);
    }
    private static string SuccessorPosition(CampaignCombatCycleAuthority cycle, bool repeat)
    {
        // Admission is exactly frozen3k: first slot of operation stage1, either actual owner.
        Require(cycle.GameTurn == 1 && cycle.OperationStage == 1 && cycle.PlayerPhaseSlot == "first-acting-side", "Unsupported control successor scope.");
        var id = repeat ? "land.position.operation-1.first-player.movement-and-combat.movement" : "land.position.operation-1.first-player.truck-convoy-movement";
        return Cna1979LandSequence.CreateTurn(cycle.GameTurn).Single(p => p.PositionId == id).PositionId;
    }
    private static void Require(bool condition, string message) { if (!condition) throw new JsonException(message); }
}
