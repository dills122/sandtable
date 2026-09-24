using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed class CombatCycleMovementAssessment
{
    internal CombatCycleMovementAssessment(int terrainCost, int breakOffCost,
        IEnumerable<CampaignCombatRelationship> affected)
    {
        TerrainCost = terrainCost;
        BreakOffCost = breakOffCost;
        TotalCost = new CapabilityPointAmount(checked(terrainCost + breakOffCost), 1);
        Affected = Array.AsReadOnly(affected.ToArray());
    }

    public int TerrainCost { get; }
    public int BreakOffCost { get; }
    public CapabilityPointAmount TotalCost { get; }
    public IReadOnlyList<CampaignCombatRelationship> Affected { get; }
}

internal sealed record CombatCycleMovementCharge(CombatCycleMovementAssessment Assessment, CampaignCombatSpendResult Spending);

/// <summary>
/// Pure assessment over an independently admitted current-stage relationship ledger.
/// Callers own terrain/topology, World, original-unit and Reserve-history admission.
/// Neither assessment nor provisional charging emits a move or ends a relationship.
/// </summary>
internal static class CampaignCombatCycleMovementRules
{
    public static CombatCycleMovementAssessment Assess(CampaignCombatUnitKey unit, int gameTurn,
        int operationStage, int terrainCost, IReadOnlyList<CampaignCombatRelationship> relationships)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(relationships);
        _ = new CampaignCombatScope(gameTurn, operationStage);
        // Frozen integer infantry probes admit terrain1/2; actual Clear movement costs2.
        if (terrainCost is not (1 or 2)) throw new ArgumentOutOfRangeException(nameof(terrainCost));
        if (relationships.Count > 512) throw new ArgumentOutOfRangeException(nameof(relationships));
        var retained = relationships.ToArray();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var affected = new List<CampaignCombatRelationship>();
        var breakOff = 0;
        foreach (var relationship in retained)
        {
            if (relationship is null || !ids.Add(relationship.RelationId) ||
                relationship.Attacker.CreationBinding != unit.CreationBinding ||
                relationship.Defender.CreationBinding != unit.CreationBinding ||
                relationship.GameTurn != gameTurn || relationship.OperationStage != operationStage)
                throw new ArgumentException("Relationship ledger differs from admitted creation/scope or has duplicate identities.", nameof(relationships));
            if (!relationship.Active || (relationship.Attacker != unit && relationship.Defender != unit)) continue;
            affected.Add(relationship);
            breakOff = Math.Max(breakOff, relationship.Kind == "engaged" ? 4 : 2);
        }
        return new(terrainCost, breakOff, affected.OrderBy(r => r.RelationId, StringComparer.Ordinal));
    }

    public static CombatCycleMovementCharge AssessAndCharge(CampaignCombatUnitKey unit,
        CampaignElementOperationalStateV6 state, int cpa, CampaignCombatSpendCeiling ceiling,
        int terrainCost, IReadOnlyList<CampaignCombatRelationship> relationships, string receiptId,
        IReadOnlyList<CampaignCombatCohesionCause> priorCauses)
    {
        ArgumentNullException.ThrowIfNull(state);
        var assessment = Assess(unit, state.LedgerGameTurn, state.LedgerOperationStage, terrainCost, relationships);
        var spending = CampaignCombatSpending.ChargeOrdinary(state, assessment.TotalCost, cpa,
            ceiling, unit.ElementId, receiptId, priorCauses);
        return new(assessment, spending);
    }
}
