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
        if (after != checked(before - points)) throw new ArgumentException("Cohesion cause does not conserve points.", nameof(after));
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
    CampaignCombatCohesionCause? Cause);

internal static class CampaignCombatSpending
{
    public static CampaignCombatSpendResult ChargeOrdinary(CampaignElementOperationalStateV6 state,
        CapabilityPointAmount cost, int cpa, CampaignCombatSpendCeiling ceiling, string elementId, string receiptId)
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
        return Charge(state, cost, cpa, limit, elementId, receiptId, "excess-cp-dp");
    }

    public static CampaignCombatSpendResult ChargeMandatoryRetreat(CampaignElementOperationalStateV6 state,
        int cpa, string elementId, string receiptId)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentOutOfRangeException.ThrowIfLessThan(cpa, 1);
        return Charge(state, new CapabilityPointAmount(1, 1), cpa, long.MaxValue,
            elementId, receiptId, "retreat-excess-cp-dp");
    }

    private static CampaignCombatSpendResult Charge(CampaignElementOperationalStateV6 state,
        CapabilityPointAmount cost, int cpa, long limit, string elementId, string receiptId, string causeKind)
    {
        ContentContractGuards.RequireStableId(elementId, nameof(elementId));
        ContentContractGuards.RequireStableId(receiptId, nameof(receiptId));
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
        var cause = points == 0 ? null : new CampaignCombatCohesionCause(
            $"{receiptId}.excess-cp", 1, receiptId, elementId,
            state.LedgerGameTurn, state.LedgerOperationStage, causeKind, points,
            state.CohesionLevel, cohesion);
        return new CampaignCombatSpendResult(next, cause);
    }
}
