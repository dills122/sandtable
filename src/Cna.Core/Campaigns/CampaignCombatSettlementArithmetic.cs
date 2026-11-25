using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>
/// Single reader of the selected <see cref="CombatSettlementRules"/> and <see cref="CombatCosts"/> for the
/// Result2 typed and JSON settlement paths, so a rules revision cannot leave them on stale numbers. The
/// native V22-V26 path reads the same rules inside <c>Cna1979CombatAdjudication</c>.
/// </summary>
internal static class CampaignCombatSettlementArithmetic
{
    // No selected-rules field carries this limit; adding one would change the rules-artifact hash.
    internal const int EscapePathMaximumHexes = 4;

    internal static CombatSettlementRules Rules => Cna1979CombatAdjudication.Definition.Settlement;
    internal static CombatCosts Costs => Cna1979CombatAdjudication.Definition.Costs;

    internal static int RefusalPercent(int unfulfilledDistance) => Rules.RefusalPercentPerHex * unfulfilledDistance;

    /// <summary>Attacker loss rounds up, defender loss rounds down (see <c>Cna1979CombatAdjudication.Loss</c>).</summary>
    internal static int LossToe(bool attacker, int toe, int totalPercent)
    {
        var numerator = checked(toe * totalPercent);
        return attacker ? (numerator + 99) / 100 : numerator / 100;
    }
    internal static int CapturedToe(int lossToe, int sharePercent) => (lossToe * sharePercent + 99) / 100;
    internal static int LossDp(int lossToe) => lossToe >= Rules.LossDpThresholdToe ? Rules.LossDpPoints : 0;
    internal static int VictoryRp(int completedRetreatDistance) => completedRetreatDistance > 0 ? Rules.VictoryRpPoints : 0;
    internal static int RetreatCapabilityPoints(int completedRetreatDistance) =>
        checked(completedRetreatDistance * Rules.ClearRetreatCapabilityPointsPerHex);
    internal static int CohesionAfterVictory(int prior, int points) => Math.Min(Rules.CohesionUpperCap, checked(prior + points));
    internal static long ExcessCapabilityPoints(long before, long after) =>
        Math.Max(0, after - Costs.BaseCapabilityPointAllowance) - Math.Max(0, before - Costs.BaseCapabilityPointAllowance);

    internal static bool GuardLegal(int routeHexes, int quantity, int donorToe) =>
        routeHexes <= Rules.GuardedPathMaximumHexes && quantity <= 5 * Rules.GuardToe && donorToe > Rules.GuardToe;
    internal static bool EscapeLegal(long capabilityPoints, int routeHexes) =>
        capabilityPoints <= Rules.EscapePathMaximumCapabilityPoints && routeHexes <= EscapePathMaximumHexes;

    /// <summary>Earliest scope at which an escaped captive's replacement becomes eligible.</summary>
    internal static (int GameTurn, int OperationStage) ReplacementEligibleScope(int earnedGameTurn, int earnedOperationStage)
    {
        var stages = Rules.OperationStagesPerGameTurn;
        var ordinal = checked((earnedGameTurn - 1) * stages + earnedOperationStage - 1 + Rules.ReplacementDelayOperationStages);
        return (ordinal / stages + 1, ordinal % stages + 1);
    }

    /// <summary>
    /// Shortest route by BFS expanding neighbours in ordinal order: the canonical custody/retreat tie-break
    /// shared by native V25 and Result2. Null when no route exists.
    /// </summary>
    internal static string[]? ShortestRoute(IEnumerable<(string First, string Second)> edges, string start, string end)
    {
        var graph = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var (first, second) in edges) { Add(first, second); Add(second, first); }
        var queue = new Queue<string[]>();
        var seen = new HashSet<string>(StringComparer.Ordinal) { start };
        queue.Enqueue([start]);
        while (queue.TryDequeue(out var path))
        {
            if (path[^1] == end) return path;
            if (!graph.TryGetValue(path[^1], out var neighbors)) continue;
            foreach (var next in neighbors.Order(StringComparer.Ordinal))
                if (seen.Add(next)) queue.Enqueue([.. path, next]);
        }
        return null;

        void Add(string from, string to)
        {
            if (!graph.TryGetValue(from, out var neighbors)) graph[from] = neighbors = [];
            neighbors.Add(to);
        }
    }
}
