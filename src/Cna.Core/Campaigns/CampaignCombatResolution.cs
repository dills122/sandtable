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
    internal CombatResolutionContext(CombatRoundState committed)
    {
        Committed = committed;
        var bytes = CampaignCombatSealedRoundCodec.SerializeState(committed);
        CommittedHash = CampaignOpeningPreambleCodec.Hash(bytes);
        using var document = JsonDocument.Parse(bytes);
        worldBytes = System.Text.Encoding.UTF8.GetBytes(document.RootElement.GetProperty("world").GetRawText());
    }
    public CombatRoundState Committed { get; }
    public string CommittedHash { get; }
    internal ReadOnlySpan<byte> WorldBytes => worldBytes;
}
internal sealed record CombatResolutionState
{
    internal CombatResolutionState(CombatResolutionContext context)
    {
        Context = context; World = context.Committed.World; RandomState = context.Committed.Base.Boundary.RandomState;
        StateVersion = context.Committed.StateVersion; Prefix = context.Committed.Prefix;
    }
    private IReadOnlyList<CampaignOpeningPreambleReceipt> receipts = Array.Empty<CampaignOpeningPreambleReceipt>();
    public CombatResolutionContext Context { get; }
    public CampaignWorldSnapshotV7 World { get; init; }
    public RandomStreamState RandomState { get; init; }
    public CombatAssaultResult? Result { get; init; }
    public long StateVersion { get; init; }
    public string Prefix { get; init; }
    public string Status { get; init; } = "committed";
    public IReadOnlyList<CampaignOpeningPreambleReceipt> Receipts { get => receipts; init => receipts = Array.AsReadOnly(value.ToArray()); }
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
        var state = new CombatResolutionState(new(committed));
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
        if (command.Kind is "expire" or "unavailable") return new(prior, CombatStepsDisposition.NoOp, null, null);
        Require(prior.Receipts.Count < 32 && prior.StateVersion < long.MaxValue && command.ExpectedPriorVersion == prior.StateVersion &&
            command.DecisionId is null && command.Kind == "resolve" && prior.Status == "committed" &&
            input.AdmittedAt is null && input.ClockAvailable, "Result2 only admits fresh resolution; settlement is pending.");
        var assault = Resolve(committed);
        var selection = committed.Base.Steps.Selection!;
        var settlement = CampaignCombatSettlementState.CreateResolvedResultV2(committed.CommitmentId!, assault.ResultId,
            committed.Base.Boundary.Cycle.GameTurn, committed.Base.Boundary.Cycle.OperationStage,
            selection.Attacker.Unit, selection.Defender.Unit, committed.World.Elements, assault.Facts);
        var world = PendingWorld(committed.World, settlement);
        var unsigned = CampaignCombatResolutionCodec.SerializeEvent(prior, input, assault, settlement.SettlementId, null);
        var receiptId = "cmb." + CampaignOpeningPreambleCodec.HashWithDomain("sandtable.combat.result-receipt.v2", unsigned)[7..];
        var bytes = CampaignCombatResolutionCodec.SerializeEvent(prior, input, assault, settlement.SettlementId, receiptId);
        var next = prior with
        {
            Result = assault,
            World = world,
            RandomState = assault.AfterRandomState,
            Status = "resolved",
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
