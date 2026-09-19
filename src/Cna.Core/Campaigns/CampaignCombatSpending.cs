using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal enum CampaignCombatSpendCeiling
{
    Ordinary,
    ReleasedReserveI,
    ReleasedReserveII,
}

internal sealed record CampaignCombatCohesionCause
{
    public CampaignCombatCohesionCause(string causeId, int ordinal, string receiptId, string elementId,
        int gameTurn, int operationStage, string kind, int points, int before, int after)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(ordinal, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(gameTurn, 1);
        if (operationStage is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(operationStage));
        ArgumentOutOfRangeException.ThrowIfLessThan(points, 1);
        if (before > 10 || after > 10) throw new ArgumentOutOfRangeException(nameof(after));
        var expectedAfter = kind switch
        {
            "assault-victory-rp" => Math.Min(10L, (long)before + points),
            "loss-dp" or "retreat-excess-dp" or "ordinary-movement-excess-cp-dp" => (long)before - points,
            _ => throw new ArgumentException("Unsupported Cohesion cause kind.", nameof(kind)),
        };
        if (expectedAfter < int.MinValue || after != expectedAfter)
            throw new ArgumentException("Cohesion cause does not conserve points.", nameof(after));
        CauseId = ContentContractGuards.RequireStableId(causeId, nameof(causeId));
        Ordinal = ordinal;
        ReceiptId = ContentContractGuards.RequireStableId(receiptId, nameof(receiptId));
        ElementId = ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        GameTurn = gameTurn;
        OperationStage = operationStage;
        Kind = ContentContractGuards.RequireStableId(kind, nameof(kind));
        Points = points;
        Before = before;
        After = after;
    }

    public string CauseId { get; }
    public int Ordinal { get; }
    public string ReceiptId { get; }
    public string ElementId { get; }
    public int GameTurn { get; }
    public int OperationStage { get; }
    public string Kind { get; }
    public int Points { get; }
    public int Before { get; }
    public int After { get; }
}

internal sealed record CampaignCombatSpendResult(
    CampaignElementOperationalStateV6 State,
    CampaignCombatCohesionCause? Cause,
    IReadOnlyList<CampaignCombatCohesionCause> Causes);

internal static class CampaignCombatSpending
{
    public static CampaignCombatSpendResult ChargeOrdinary(CampaignElementOperationalStateV6 state,
        CapabilityPointAmount cost, int cpa, CampaignCombatSpendCeiling ceiling, string elementId,
        string receiptId, IReadOnlyList<CampaignCombatCohesionCause> priorCauses)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(cost);
        if (!Enum.IsDefined(ceiling)) throw new ArgumentOutOfRangeException(nameof(ceiling));
        ArgumentOutOfRangeException.ThrowIfLessThan(cpa, 1);
        if (cost.Denominator != 1 || cost.Numerator == 0)
            throw new ArgumentOutOfRangeException(nameof(cost), "Ordinary infantry spending requires a positive integer cost.");
        var limit = ceiling switch
        {
            CampaignCombatSpendCeiling.Ordinary => checked(3L * cpa / 2),
            CampaignCombatSpendCeiling.ReleasedReserveI => cpa,
            CampaignCombatSpendCeiling.ReleasedReserveII => cpa / 2,
            _ => throw new ArgumentOutOfRangeException(nameof(ceiling)),
        };
        return Charge(state, cost, cpa, limit, elementId, receiptId, $"{receiptId}.dp",
            "ordinary-movement-excess-cp-dp", priorCauses);
    }

    public static CampaignCombatSpendResult ChargeMandatoryRetreat(CampaignElementOperationalStateV6 state,
        int cpa, string elementId, string receiptId, string causeId,
        IReadOnlyList<CampaignCombatCohesionCause> priorCauses)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentOutOfRangeException.ThrowIfLessThan(cpa, 1);
        return Charge(state, new CapabilityPointAmount(1, 1), cpa, long.MaxValue,
            elementId, receiptId, causeId, "retreat-excess-dp", priorCauses);
    }

    private static CampaignCombatSpendResult Charge(CampaignElementOperationalStateV6 state,
        CapabilityPointAmount cost, int cpa, long limit, string elementId, string receiptId,
        string causeId, string causeKind, IReadOnlyList<CampaignCombatCohesionCause> priorCauses)
    {
        ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        ContentContractGuards.RequireStableId(receiptId, nameof(receiptId));
        ContentContractGuards.RequireStableId(causeId, nameof(causeId));
        var history = ContentContractGuards.CopyValues(priorCauses, nameof(priorCauses));
        if (history.Length > 4096 || history.Where((cause, index) => cause.Ordinal != index + 1).Any() ||
            history.Select(cause => cause.CauseId).Distinct(StringComparer.Ordinal).Count() != history.Length)
            throw new ArgumentException("Prior Cohesion causes must be a contiguous unique history.", nameof(priorCauses));
        if (state.CapabilityPointsExpended.Denominator != 1)
            throw new ArgumentException("Selected infantry ledger requires integer CP.", nameof(state));
        var before = state.CapabilityPointsExpended.Numerator;
        var after = checked(before + cost.Numerator);
        if (after > limit) throw new ArgumentOutOfRangeException(nameof(cost), "CP ceiling exceeded.");
        var points = checked((int)(Math.Max(0L, after - cpa) - Math.Max(0L, before - cpa)));
        var cohesion = checked(state.CohesionLevel - points);
        var next = new CampaignElementOperationalStateV6(state.LedgerGameTurn, state.LedgerOperationStage,
            new CapabilityPointAmount(after, 1), cohesion, state.VehicleBreakdownState,
            state.MovementEnded, state.InitialLedgerOrigin);
        if (points > 0 && history.Length == 4096)
            throw new ArgumentOutOfRangeException(nameof(priorCauses), "Cohesion cause capacity is exhausted.");
        if (points > 0 && history.Any(cause => cause.CauseId == causeId))
            throw new ArgumentException("Cohesion cause ID already exists.", nameof(causeId));
        var cause = points == 0 ? null : new CampaignCombatCohesionCause(
            causeId, history.Length + 1, receiptId, elementId,
            state.LedgerGameTurn, state.LedgerOperationStage, causeKind, points,
            state.CohesionLevel, cohesion);
        var causes = cause is null ? history : [.. history, cause];
        return new CampaignCombatSpendResult(next, cause, Array.AsReadOnly(causes));
    }
}
