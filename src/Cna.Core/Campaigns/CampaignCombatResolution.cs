using System.Text.Json;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CombatResolutionCommand(int ContractVersion, string RoundClockConfigurationHash,
    string ResultClockPolicyId, string Kind, string RoundId, string CommitmentId,
    long? ExpectedPriorVersion = null, string? DecisionId = null, string? Choice = null);
internal sealed record CombatResolutionInput(CombatResolutionCommand Command, CampaignOpeningPreambleActor Actor,
    long? AdmittedAt = null, bool ClockAvailable = true);
internal sealed record CombatResolutionDraw
{
    public CombatResolutionDraw(string purpose, ulong beforeCursor, ulong afterCursor, int die, IEnumerable<int> consumed)
    {
        Purpose = purpose; BeforeCursor = beforeCursor; AfterCursor = afterCursor; Die = die;
        Consumed = Array.AsReadOnly(consumed.ToArray());
    }
    public string Purpose { get; }
    public ulong BeforeCursor { get; }
    public ulong AfterCursor { get; }
    public int Die { get; }
    public IReadOnlyList<int> Consumed { get; }
}
internal sealed record CombatAssaultResult
{
    public CombatAssaultResult(string commitmentId, string rulesInputHash, string procedureId, RandomStreamState before,
        RandomStreamState after, IEnumerable<CombatResolutionDraw> draws, int attackerCoordinate, int defenderCoordinate,
        int attackerMorale, int defenderMorale, CampaignCombatResultFacts facts)
    {
        CommitmentId = commitmentId; RulesInputHash = rulesInputHash; ProcedureId = procedureId;
        BeforeRandomState = before; AfterRandomState = after; Draws = Array.AsReadOnly(draws.ToArray());
        AttackerMoraleCoordinate = attackerCoordinate; DefenderMoraleCoordinate = defenderCoordinate;
        AttackerMorale = attackerMorale; DefenderMorale = defenderMorale; Facts = facts;
        ResultId = "res." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.assault-result.v2",
            CampaignCombatResolutionCodec.SerializeResultPreimage(this))[7..];
    }
    public string ResultId { get; }
    public string CommitmentId { get; }
    public string RulesInputHash { get; }
    public string ProcedureId { get; }
    public RandomStreamState BeforeRandomState { get; }
    public RandomStreamState AfterRandomState { get; }
    public IReadOnlyList<CombatResolutionDraw> Draws { get; }
    public int AttackerMoraleCoordinate { get; }
    public int DefenderMoraleCoordinate { get; }
    public int AttackerMorale { get; }
    public int DefenderMorale { get; }
    public CampaignCombatResultFacts Facts { get; }
}
internal sealed class CombatResolutionContext
{
    private readonly byte[] worldBytes;
    internal CombatResolutionContext(CombatRoundState committed, CampaignCombatCreationContext creation)
    {
        Committed = committed; Creation = creation;
        var bytes = CampaignCombatSealedRoundCodec.SerializeState(committed);
        CommittedHash = CampaignOpeningPreambleCodec.Hash(bytes);
        using var document = JsonDocument.Parse(bytes);
        worldBytes = System.Text.Encoding.UTF8.GetBytes(document.RootElement.GetProperty("world").GetRawText());
    }
    public CombatRoundState Committed { get; }
    public CampaignCombatCreationContext Creation { get; }
    public string CommittedHash { get; }
    internal ReadOnlySpan<byte> WorldBytes => worldBytes;
}
internal sealed record CombatResolutionState
{
    internal CombatResolutionState(CombatResolutionContext context)
    {
        Context = context; World = context.Committed.World; RandomState = context.Committed.Base.Boundary.RandomState;
        StateVersion = context.Committed.StateVersion; Prefix = context.Committed.Prefix;
        AcceptedHighWater = context.Committed.Timing!.OpeningFloorUnixMilliseconds;
    }
    private IReadOnlyList<CampaignOpeningPreambleReceipt> receipts = Array.Empty<CampaignOpeningPreambleReceipt>();
    public CombatResolutionContext Context { get; }
    public CombatResolutionWindow? Window { get; init; }
    public long AcceptedHighWater { get; init; }
    public CampaignWorldSnapshotV7 World { get; init; }
    public RandomStreamState RandomState { get; init; }
    public CombatAssaultResult? Result { get; init; }
    public long StateVersion { get; init; }
    public string Prefix { get; init; }
    public string Status { get; init; } = "committed";
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get => receipts; init => receipts = Array.AsReadOnly(value.ToArray()); }
}
internal sealed record CombatResolutionWindow(string DecisionId, string Kind, string Owner, CombatStepsTiming Timing);
internal abstract record CombatResolutionEffect(string Kind)
{
    internal sealed record Resolve(CombatAssaultResult Result, string SettlementId) : CombatResolutionEffect("assault-resolved");
    internal sealed record Open(CombatResolutionWindow Window) : CombatResolutionEffect("choice-opened");
    internal sealed record Disposition(string Reason, CombatStepsTiming? Timing, CampaignCombatRetreatDisposition Payload) : CombatResolutionEffect("disposition-recorded");
    internal sealed record Loss(CampaignCombatLossReceipt Payload) : CombatResolutionEffect("losses-settled");
    internal sealed record Retreat(CampaignCombatRetreatReceipt Payload) : CombatResolutionEffect("retreat-settled");
}
internal sealed class CombatResolutionResult(CombatResolutionState state, CombatStepsDisposition disposition, byte[]? eventBytes, string? receiptId)
{
    private readonly byte[]? bytes = eventBytes?.ToArray();
    public CombatResolutionState State { get; } = state;
    public CombatStepsDisposition Disposition { get; } = disposition;
    public byte[]? EventBytes => bytes?.ToArray();
    public string? ReceiptId { get; } = receiptId;
}

/// <summary>Authenticated dormant result/cursor projection; mandatory settlement remains pending.</summary>
internal static class CampaignCombatResolution
{
    internal const string Policy = "sandtable.combat.mandatory-window-clock.v2";
    private sealed record Frame(CombatResolutionState State, byte[][] Events);
    public static CombatResolutionState ReplayTrustedBoundary(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        CombatStepsBoundary boundary, IReadOnlyList<CombatStepsInput> predecessorInputs, IReadOnlyList<byte[]> predecessorEvents,
        IReadOnlyList<CombatRoundInput> roundInputs, IReadOnlyList<byte[]> roundEvents,
        IReadOnlyList<CombatResolutionInput> inputs, IReadOnlyList<byte[]> events) =>
        Replay(request, created, boundary, predecessorInputs, predecessorEvents, roundInputs, roundEvents, inputs, events).State;

    public static CombatResolutionResult ApplyTrustedBoundary(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        CombatStepsBoundary boundary, IReadOnlyList<CombatStepsInput> predecessorInputs, IReadOnlyList<byte[]> predecessorEvents,
        IReadOnlyList<CombatRoundInput> roundInputs, IReadOnlyList<byte[]> roundEvents,
        IReadOnlyList<CombatResolutionInput> inputs, IReadOnlyList<byte[]> events, CombatResolutionInput input)
    {
        var frame = Replay(request, created, boundary, predecessorInputs, predecessorEvents, roundInputs, roundEvents, inputs, events);
        var result = Transition(frame.State, input);
        if (result.Disposition != CombatStepsDisposition.Duplicate) return result;
        var index = frame.State.Receipts.ToList().FindIndex(r => r.ReceiptId == result.ReceiptId);
        return new(frame.State, result.Disposition, frame.Events[index], result.ReceiptId);
    }

    private static Frame Replay(CampaignCombatCreationRequest request, ReadOnlySpan<byte> created,
        CombatStepsBoundary boundary, IReadOnlyList<CombatStepsInput> predecessorInputs, IReadOnlyList<byte[]> predecessorEvents,
        IReadOnlyList<CombatRoundInput> roundInputs, IReadOnlyList<byte[]> roundEvents,
        IReadOnlyList<CombatResolutionInput> inputs, IReadOnlyList<byte[]> events)
    {
        Require(created.Length is > 0 and <= 1_048_576, "Missing or oversized Created11.");
        var ownedCreated = created.ToArray();
        ArgumentNullException.ThrowIfNull(inputs); ArgumentNullException.ThrowIfNull(events);
        var count = events.Count;
        Require(count is >= 0 and <= 32 && inputs.Count == count, "Result2 input/event capacity mismatch.");
        var owned = new byte[count][];
        for (var i = 0; i < count; i++)
        {
            var bytes = events[i];
            Require(bytes is { Length: > 0 and <= 1_048_576 }, "Missing or oversized Result2 event.");
            owned[i] = bytes.ToArray(); CampaignCombatResolutionCodec.ValidateSyntax(owned[i], "ResultEvent");
        }
        var trusted = new CombatResolutionInput[count];
        for (var i = 0; i < count; i++) { trusted[i] = inputs[i]; _ = CampaignCombatResolutionCodec.SerializeInput(trusted[i]); }
        var committed = CampaignCombatSealedRound.ReplayTrustedBoundary(request, ownedCreated, boundary,
            predecessorInputs, predecessorEvents, roundInputs, roundEvents);
        Require(committed.Status == "committed" && committed.StepIndex == 5 && committed.StepReceipts.Count == 5 &&
            committed.Slots.Count == 2 && committed.Slots.All(s => s.SealedReceiptId is not null) && committed.CommitmentId is not null &&
            committed.RoundId is not null && committed.AttackHistory.Count == 1 && committed.TargetUses.Count == 1 && !committed.Closed,
            "Result2 requires complete authenticated paid Round2.");
        var state = new CombatResolutionState(new(committed, request.Context));
        for (var i = 0; i < count; i++)
        {
            var result = Transition(state, trusted[i]);
            Require(result.Disposition == CombatStepsDisposition.Accepted && result.EventBytes is { } expected && expected.AsSpan().SequenceEqual(owned[i]),
                "Result2 event differs from independently trusted causal replay.");
            state = result.State;
        }
        return new(state, owned);
    }

    private static CombatResolutionResult Transition(CombatResolutionState prior, CombatResolutionInput input)
    {
        _ = CampaignCombatResolutionCodec.SerializeInput(input);
        var command = input.Command; var committed = prior.Context.Committed;
        Require(command.ContractVersion == 2 && command.RoundClockConfigurationHash == committed.Base.ConfigurationHash &&
            command.ResultClockPolicyId == Policy && command.RoundId == committed.RoundId && command.CommitmentId == committed.CommitmentId,
            "Wrong Result2 authority binding.");
        Require(command.Kind is "resolve" or "advance" or "choose" or "expire" or "unavailable", "Unknown Result2 command.");
        Require(command.Kind == "choose" ? input.Actor is CampaignOpeningPreambleActor.Axis or CampaignOpeningPreambleActor.Commonwealth
            : input.Actor == CampaignOpeningPreambleActor.System, "Wrong Result2 actor.");
        Require((command.Choice is not null) == (command.Kind == "choose") &&
            (command.ExpectedPriorVersion is not null) == (command.Kind is "resolve" or "advance"), "Invalid Result2 command fields.");
        var hash = CampaignOpeningPreambleCodec.Hash(CampaignCombatResolutionCodec.SerializeCommand(command));
        var retained = prior.Receipts.SingleOrDefault(r => r.CommandHash == hash && r.Actor == input.Actor);
        if (retained is not null) return new(prior, CombatStepsDisposition.Duplicate, null, retained.ReceiptId);
        var window = prior.Window;
        if (command.Kind is "expire" or "unavailable" && (window is null || command.DecisionId != window.DecisionId))
            return new(prior, CombatStepsDisposition.NoOp, null, null);
        Require(prior.Receipts.Count < 32 && prior.StateVersion < long.MaxValue, "Result2 receipt/version capacity exceeded.");
        Require(command.Kind is not ("resolve" or "advance") || command.ExpectedPriorVersion == prior.StateVersion, "Stale Result2 version.");
        Require(command.Kind is "resolve" or "advance" ? command.DecisionId is null : window is not null && command.DecisionId == window.DecisionId,
            "Wrong Result2 decision.");
        var next = prior; var author = input.Actor; CombatResolutionEffect effect;
        void RecordDisposition(string choice)
        {
            var settlement = prior.World.Settlements.Single();
            var disposition = CampaignCombatLossRetreat.Disposition(prior.Context, settlement, choice);
            next = next with
            {
                World = CampaignCombatLossRetreat.Project(prior.Context, prior.Result!, settlement.WithResultV2Disposition(disposition)),
                Status = "disposition",
                Window = null
            };
        }
        if (command.Kind is "choose" or "expire" or "unavailable")
        {
            Require(window is { Kind: "retreat" }, "Only retreat choice is implemented.");
            if (command.Kind == "choose") Require(CampaignCombatSealedRoundCodec.Actor(input.Actor) == window!.Owner &&
                command.Choice is "retreat" or "refuse-retreat", "Foreign or invalid retreat choice.");
            var timing = window!.Timing;
            var lost = !input.ClockAvailable || input.AdmittedAt is null || input.AdmittedAt < timing.HighWaterUnixMilliseconds;
            var late = input.AdmittedAt is { } now && now >= timing.DeadlineUnixMilliseconds;
            if (command.Kind == "expire" && !lost && !late) return new(prior, CombatStepsDisposition.NoOp, null, null);
            if (command.Kind == "choose" && !lost) Require(!late, "Retreat choice reached exclusive deadline.");
            var fallback = command.Kind != "choose" || lost;
            if (!lost)
            {
                timing = timing with { HighWaterUnixMilliseconds = Math.Max(timing.HighWaterUnixMilliseconds, input.AdmittedAt!.Value) };
                next = next with { AcceptedHighWater = Math.Max(prior.AcceptedHighWater, input.AdmittedAt.Value) };
            }
            var reason = fallback ? lost ? "clock-unavailable" : command.Kind == "unavailable" ? "controller-unavailable" : "deadline" : "owner-choice";
            if (fallback) author = CampaignOpeningPreambleActor.System;
            RecordDisposition(fallback ? "refuse-retreat" : command.Choice!);
            effect = new CombatResolutionEffect.Disposition(reason, timing, next.World.Settlements.Single().Disposition!);
        }
        else if (command.Kind == "resolve")
        {
            Require(prior.Status == "committed" && input.AdmittedAt is null && input.ClockAvailable, "Invalid fresh resolution.");
            var assault = Resolve(committed); var selection = committed.Base.Steps.Selection!;
            var settlement = CampaignCombatSettlementState.CreateResolvedResultV2(committed.CommitmentId!, assault.ResultId,
                committed.Base.Boundary.Cycle.GameTurn, committed.Base.Boundary.Cycle.OperationStage,
                selection.Attacker.Unit, selection.Defender.Unit, committed.World.Elements, assault.Facts);
            next = prior with { Result = assault, World = PendingWorld(committed.World, settlement), RandomState = assault.AfterRandomState, Status = "resolved" };
            effect = new CombatResolutionEffect.Resolve(assault, settlement.SettlementId);
        }
        else
        {
            Require(window is null, "Structural work cannot bypass a retreat window.");
            if (prior.Status == "resolved" && prior.Result!.Facts.RequiredRetreat > 0)
            {
                var budget = prior.Context.Creation.Configuration.Windows.Single(w => w.Kind == "retreat").DecisionBudgetMilliseconds;
                var now = input.AdmittedAt;
                if (!input.ClockAvailable || now is null || now > CampaignCombatSelectionSteps.UtcMaximum - budget)
                {
                    RecordDisposition("refuse-retreat");
                    effect = new CombatResolutionEffect.Disposition("opening-clock-unavailable", null, next.World.Settlements.Single().Disposition!);
                }
                else
                {
                    var timing = new CombatStepsTiming(1, prior.Context.Creation.Configuration.Hash, "retreat", budget, now.Value, now.Value + budget, now.Value);
                    var opened = new CombatResolutionWindow(prior.World.Settlements.Single().SettlementId + ".choice.retreat", "retreat",
                        committed.Base.Steps.Selection!.Defender.Unit.OriginalSide, timing);
                    next = prior with { Window = opened, Status = "waiting-retreat", AcceptedHighWater = Math.Max(prior.AcceptedHighWater, now.Value) };
                    effect = new CombatResolutionEffect.Open(opened);
                }
            }
            else
            {
                Require(input.AdmittedAt is null && input.ClockAvailable, "Structural settlement requires null time and available confidence.");
                if (prior.Status == "resolved")
                {
                    RecordDisposition("not-required");
                    effect = new CombatResolutionEffect.Disposition("not-required", null, next.World.Settlements.Single().Disposition!);
                }
                else if (prior.Status == "disposition")
                {
                    var settlement = prior.World.Settlements.Single(); var losses = CampaignCombatLossRetreat.Losses(settlement);
                    next = prior with { World = CampaignCombatLossRetreat.Project(prior.Context, prior.Result!, settlement.WithResultV2Losses(losses)), Status = "losses" };
                    effect = new CombatResolutionEffect.Loss(losses);
                }
                else if (prior.Status == "losses")
                {
                    var settlement = prior.World.Settlements.Single(); var retreat = CampaignCombatLossRetreat.Retreat(settlement);
                    next = prior with { World = CampaignCombatLossRetreat.Project(prior.Context, prior.Result!, settlement.WithResultV2Retreat(retreat)), Status = "retreat" };
                    effect = new CombatResolutionEffect.Retreat(retreat);
                }
                else throw new JsonException("Custody, relationships and closure require later tasks.");
            }
        }
        var unsigned = CampaignCombatResolutionCodec.SerializeEvent(prior, input, effect, author, null);
        var receiptId = "cmb." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.result-receipt.v2", unsigned)[7..];
        var bytes = CampaignCombatResolutionCodec.SerializeEvent(prior, input, effect, author, receiptId);
        next = next with
        {
            StateVersion = prior.StateVersion + 1,
            Prefix = CampaignOpeningPreambleCodec.EventPrefix(prior.Prefix, bytes),
            Receipts = [.. prior.Receipts, new(hash, CampaignOpeningPreambleCodec.Hash(bytes), receiptId, input.Actor, prior.StateVersion + 1)]
        };
        _ = CampaignCombatResolutionCodec.SerializeState(next);
        return new(next, CombatStepsDisposition.Accepted, bytes, receiptId);
    }

    internal static CampaignWorldSnapshotV7 PendingWorld(CampaignWorldSnapshotV7 paid, CampaignCombatSettlementState settlement) =>
        new(7, paid.CreationBinding, paid.Elements, paid.Representations, paid.BrokenVehicleLots, paid.CohesionCauses,
            paid.Relationships, paid.CustodyLots, paid.Guards, paid.ReplacementEntitlements, paid.FutureObligations, [settlement]);

    private static CombatAssaultResult Resolve(CombatRoundState committed)
    {
        var before = committed.Base.Boundary.RandomState; var current = before;
        var draws = new List<CombatResolutionDraw>(); var dice = new List<int>();
        void Draw(string purpose)
        {
            var start = current.NextByteCursor; var consumed = new List<int>();
            while (true)
            {
                Require(consumed.Count < 512, "Result2 rejection trace capacity exceeded.");
                var next = SandtableRandom.NextByte(current); current = next.State; consumed.Add(next.Value);
                if (next.Value >= SandtableRandom.D6AcceptBelow) continue;
                var die = next.Value % 6 + 1; dice.Add(die);
                draws.Add(new(purpose, start, current.NextByteCursor, die, consumed)); return;
            }
        }
        try
        {
            var definition = Cna1979CombatAdjudication.Definition;
            foreach (var purpose in definition.Procedure.OrderedPurposes) Draw(purpose);
            var attackerMorale = 10 * dice[0] + dice[1]; var defenderMorale = 10 * dice[2] + dice[3];
            var attacker = 10 * dice[4] + dice[5]; var defender = 10 * dice[6] + dice[7];
            var differential = Cna1979CombatAdjudication.CalculateDifferential(attackerMorale, defenderMorale);
            var effects = definition.Effects[differential + 2];
            var captured = effects.AttackerCaptureSums.Contains(dice[4] + dice[5]) ? "attacker"
                : effects.DefenderCaptureSums.Contains(dice[6] + dice[7]) ? "defender" : null;
            if (captured is not null) Draw(captured + ".capture.share");
            int? captureDie = captured is null ? null : dice[^1];
            var selected = Cna1979CombatAdjudication.Resolve(attackerMorale, defenderMorale, attacker, defender, captureDie: captureDie);
            var facts = new CampaignCombatResultFacts(selected.Differential, attacker, defender, captureDie,
                selected.AttackerLossPercent, selected.DefenderBaseLossPercent, selected.RawEngaged, selected.DefenderRetreatHexes,
                captured, selected.CaptureSharePercent ?? 0);
            return new(committed.CommitmentId!, Cna1979CombatAdjudication.ContentHash,
                definition.Procedure.ProcedureId, before, current, draws, attackerMorale, defenderMorale,
                Cna1979CombatAdjudication.LookupMoraleAdjustment(attackerMorale), Cna1979CombatAdjudication.LookupMoraleAdjustment(defenderMorale), facts);
        }
        catch (OverflowException exception) { throw new JsonException("Result2 cursor overflow.", exception); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new JsonException(message); }
}
