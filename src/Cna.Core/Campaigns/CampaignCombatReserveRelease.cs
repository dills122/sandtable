using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>Dormant isolated mechanism; does not authenticate campaign lineage or project World changes.</summary>
internal static class CampaignCombatReserveRelease
{
    public static CombatReleaseState Replay(CombatReleaseBase basis, CampaignCombatCreationRequest request,
        IReadOnlyList<CombatReleaseInput> trustedInputs, IReadOnlyList<byte[]> events)
    {
        ArgumentNullException.ThrowIfNull(trustedInputs); ArgumentNullException.ThrowIfNull(events);
        Require(events.Count <= 34 && trustedInputs.Count == events.Count, "Release replay capacity/count mismatch.");
        var ownedEvents = new byte[events.Count][];
        for (var index = 0; index < events.Count; index++)
        {
            var bytes = events[index];
            Require(bytes is { Length: > 0 and <= 1_048_576 }, "Missing or oversized Release event.");
            ownedEvents[index] = bytes!.ToArray();
        }
        foreach (var bytes in ownedEvents) CampaignCombatReserveReleaseCodec.ValidateSyntax(bytes, "event");
        var inputs = trustedInputs.ToArray();
        foreach (var input in inputs) _ = CampaignCombatReserveReleaseCodec.SerializeInput(input);
        var state = Initial(basis, request);
        for (var index = 0; index < inputs.Length; index++)
        {
            var result = Transition(basis, request, state, inputs[index]);
            Require(result.Disposition == CombatStepsDisposition.Accepted && ownedEvents[index].AsSpan().SequenceEqual(result.EventBytes),
                "Release event differs from independently trusted replay.");
            state = result.State;
        }
        return state;
    }

    public static CombatReleaseResult Apply(CombatReleaseBase basis, CampaignCombatCreationRequest request,
        IReadOnlyList<CombatReleaseInput> trustedInputs, IReadOnlyList<byte[]> events, CombatReleaseInput input) =>
        Transition(basis, request, Replay(basis, request, trustedInputs, events), input);

    private static CombatReleaseState Initial(CombatReleaseBase basis, CampaignCombatCreationRequest request)
    {
        var hash = CampaignOpeningPreambleCodec.Hash(CampaignCombatReserveReleaseCodec.SerializeBase(basis, request));
        var state = new CombatReleaseState(hash, CampaignCombatReserveReleaseCodec.ReleaseId(hash, basis), basis.PriorVersion,
            basis.PriorPrefix, basis.RetainedWorldHash, basis.RandomState)
        { Members = basis.Members, AttackHistory = basis.AttackHistory, AcceptedHighWater = basis.AcceptedHighWater };
        _ = CampaignCombatReserveReleaseCodec.SerializeState(state);
        return state;
    }

    private static CombatReleaseResult Transition(CombatReleaseBase basis, CampaignCombatCreationRequest request,
        CombatReleaseState prior, CombatReleaseInput input)
    {
        _ = CampaignCombatReserveReleaseCodec.SerializeInput(input);
        var command = input.Command; var kind = command.Kind; var actor = input.Actor; var cycle = basis.Cycle;
        var owner = cycle.ActingSide == LandSide.Axis ? CampaignOpeningPreambleActor.Axis : CampaignOpeningPreambleActor.Commonwealth;
        var timer = kind is "expire" or "unavailable";
        Require(command.ContractVersion == 1 && kind is "open" or "choose" or "complete-release" or "complete" or "expire" or "unavailable" or "fallback-step", "Unsupported Release command.");
        Require(command.ReleaseId == prior.ReleaseId, "Release identity mismatch.");
        Require(actor == (kind is "choose" or "complete-release" ? owner : CampaignOpeningPreambleActor.System), "Release actor mismatch.");
        Require((command.Unit is not null) == (kind == "choose") && (command.Choice is not null) == (kind == "choose") &&
            (command.ExpectedPriorVersion is null) == timer && (kind != "open" || command.DecisionId is null), "Release command field mismatch.");
        var commandHash = CampaignOpeningPreambleCodec.Hash(CampaignCombatReserveReleaseCodec.SerializeCommand(command));
        var duplicate = prior.Receipts.FirstOrDefault(r => r.CommandHash == commandHash && r.Actor == actor);
        if (duplicate is not null) return new(prior, CombatStepsDisposition.Duplicate, null, duplicate.ReceiptId);
        if (timer && (prior.Status != "open" || command.DecisionId != prior.DecisionId || prior.DecisionId is null))
            return new(prior, CombatStepsDisposition.NoOp, null, null);
        Require(prior.Status != "completed" && prior.Receipts.Count < 34 && prior.StateVersion < long.MaxValue, "Release terminal/capacity fault.");
        Require(timer || command.ExpectedPriorVersion == prior.StateVersion, "Stale Release version.");
        Require(kind == "open" || command.DecisionId == prior.DecisionId, "Stale Release decision.");
        var next = prior; CombatReleaseEffect? effect = null; var author = actor; var now = input.AdmittedAt;

        void Complete(string reason)
        {
            Require(next.Pending.Count == 0 || cycle.Ordinal > 1, "First Release cannot skip unresolved I.");
            effect = new CombatReleaseEffect.Complete(reason, next.Pending, next.Dispositions.Select(d => d.ReceiptId).ToArray(), next.Timing, next.FallbackLocked);
            next = next with { Pending = [], Status = "completed" }; author = CampaignOpeningPreambleActor.System;
        }
        void Dispose(string choice, string reason)
        {
            Require(next.Pending.Count > 0, "No pending Release member.");
            var unit = next.Pending[0]; var member = next.Members.Single(m => m.Unit == unit);
            Require(LegalChoice(member.Status, cycle.Ordinal, choice), "Illegal Release choice.");
            var released = choice is "release-I" or "release-II";
            Require(!released || cycle.Ordinal < int.MaxValue, "Release next ordinal overflow.");
            var after = released ? CampaignElementReserveStatus.None : CampaignElementReserveStatus.ReserveII;
            var exception = released ? new CombatReleaseMovementException(new(cycle.GameTurn, cycle.OperationStage, cycle.PlayerPhaseSlot, cycle.ActingSide), checked(cycle.Ordinal + 1), "pending", null) : null;
            effect = new CombatReleaseEffect.UnitDisposition(unit, choice, member.Status, after, reason, member.BaseCpa,
                released ? member.Status == CampaignElementReserveStatus.ReserveI ? member.BaseCpa : member.BaseCpa / 2 : null,
                exception, next.Timing, next.FallbackLocked);
            next = next with { Members = next.Members.Select(m => m.Unit == unit ? m with { Status = after } : m).ToArray(), Pending = next.Pending.Skip(1).ToArray() };
        }
        void Fallback(string reason)
        {
            next = next with { FallbackLocked = true, FallbackReason = next.FallbackReason ?? reason }; author = CampaignOpeningPreambleActor.System;
            if (cycle.Ordinal == 1 && next.Pending.Count > 0) Dispose("convert-to-II", next.FallbackReason!);
            else Complete(next.FallbackReason!);
        }

        if (kind == "open")
        {
            Require(next.Status == "unopened", "Release already opened.");
            var queue = next.Members.Where(m => m.Status != CampaignElementReserveStatus.None).Select(m => m.Unit).ToArray();
            Require(queue.Length == 0 || cycle.Ordinal < int.MaxValue, "Release next ordinal overflow.");
            next = next with { Pending = queue, Status = "open" };
            if (queue.Length > 0)
            {
                var lost = !input.ClockAvailable || now is null || now < next.AcceptedHighWater;
                var budget = request.Context.Configuration.Windows.Single(w => w.Kind == "reserve-release").DecisionBudgetMilliseconds;
                CombatStepsTiming? timing = !lost && now <= CampaignCombatSelectionSteps.UtcMaximum - budget
                    ? new(1, request.Context.Configuration.Hash, "reserve-release", budget, now!.Value, checked(now.Value + budget), now.Value) : null;
                next = next with
                {
                    DecisionId = next.ReleaseId + ".decision",
                    Timing = timing,
                    OpeningClockFailure = timing is null,
                    AcceptedHighWater = timing is null ? next.AcceptedHighWater : now
                };
            }
            effect = new CombatReleaseEffect.Open(next.DecisionId, next.Timing, next.OpeningClockFailure, next.Pending);
        }
        else if (kind == "complete")
        {
            Require(next.Status == "open" && next.Pending.Count == 0, "Release completion still has pending work.");
            Complete(next.FallbackLocked ? next.FallbackReason! : next.Members.Count > 0 ? "units-resolved" : "empty-membership");
        }
        else if (kind == "fallback-step")
        {
            Require(next.Status == "open" && (next.FallbackLocked || next.OpeningClockFailure), "Release fallback not active.");
            Fallback(next.FallbackReason ?? "opening-clock-unavailable");
        }
        else
        {
            Require(next.Status == "open" && !next.FallbackLocked, "Release choice window closed.");
            if (kind is "choose" or "complete-release") Require(next.Pending.Count > 0, "No pending Release choice.");
            if (kind == "choose")
            {
                Require(command.Unit == next.Pending[0], "Release choice is not canonical pending unit.");
                Require(LegalChoice(next.Members.Single(m => m.Unit == command.Unit).Status, cycle.Ordinal, command.Choice!), "Illegal Release choice.");
            }
            if (kind == "complete-release") Require(cycle.Ordinal > 1, "First Release cannot bulk retain I.");
            var lost = next.OpeningClockFailure || !input.ClockAvailable || now is null || now < next.AcceptedHighWater;
            var late = next.Timing is not null && now >= next.Timing.DeadlineUnixMilliseconds;
            if (kind == "expire" && !lost && !late) return new(prior, CombatStepsDisposition.NoOp, null, null);
            if (kind is "choose" or "complete-release" && !lost) Require(!late, "Release owner input at or after deadline.");
            if (!lost)
            {
                var highWater = Math.Max(next.AcceptedHighWater ?? 0, now!.Value);
                next = next with { AcceptedHighWater = highWater, Timing = next.Timing! with { HighWaterUnixMilliseconds = highWater } };
            }
            if (timer || lost) Fallback(next.OpeningClockFailure ? "opening-clock-unavailable" : lost ? "clock-unavailable" : kind == "unavailable" ? "controller-unavailable" : "deadline");
            else if (kind == "complete-release") Complete("owner-complete-release");
            else Dispose(command.Choice!, "owner-choice");
        }
        var unsigned = CampaignCombatReserveReleaseCodec.Event(basis, prior, input, author, effect!, null);
        var receiptId = "rr." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.reserve-release-receipt.v1", unsigned)[7..];
        var bytes = CampaignCombatReserveReleaseCodec.Event(basis, prior, input, author, effect!, receiptId);
        if (effect is CombatReleaseEffect.Open) next = next with { OpeningReceiptId = receiptId };
        else if (effect is CombatReleaseEffect.Complete) next = next with { CompletionReceiptId = receiptId };
        else if (effect is CombatReleaseEffect.UnitDisposition disposition)
        {
            var member = next.Members.Single(m => m.Unit == disposition.Unit); var history = member.History;
            if (disposition.Choice == "convert-to-II") history = history with { ConversionReceiptId = receiptId };
            else if (disposition.Choice is "release-I" or "release-II") history = history with
            {
                ReleasedType = disposition.BeforeStatus == CampaignElementReserveStatus.ReserveI ? "I" : "II",
                ReleaseReceiptId = receiptId,
                ReleaseCycle = cycle.Ordinal,
                CpaBasis = disposition.CpaBasis,
                VoluntaryCeiling = disposition.VoluntaryCeiling,
                NextMovement = disposition.NextMovement
            };
            next = next with
            {
                Members = next.Members.Select(m => m.Unit == member.Unit ? m with { History = history } : m).ToArray(),
                Dispositions = [.. next.Dispositions, new(receiptId, disposition.Unit, disposition.Choice, disposition.BeforeStatus, disposition.AfterStatus)]
            };
        }
        next = next with
        {
            StateVersion = checked(prior.StateVersion + 1),
            Prefix = CampaignOpeningPreambleCodec.EventPrefix(prior.Prefix, bytes),
            Receipts = [.. prior.Receipts, new(commandHash, CampaignOpeningPreambleCodec.Hash(bytes), receiptId, actor, checked(prior.StateVersion + 1))]
        };
        _ = CampaignCombatReserveReleaseCodec.SerializeState(next); // Capacity must succeed before publishing any immutable result.
        return new(next, CombatStepsDisposition.Accepted, bytes, receiptId);
    }

    private static bool LegalChoice(CampaignElementReserveStatus status, int ordinal, string choice) =>
        status == CampaignElementReserveStatus.ReserveI && ordinal == 1 && choice is "release-I" or "convert-to-II" ||
        status == CampaignElementReserveStatus.ReserveII && ordinal > 1 && choice is "release-II" or "retain-II";
    private static void Require(bool condition, string message) { if (!condition) throw new JsonException(message); }
}
