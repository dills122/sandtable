using Cna.Core.Campaigns;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Exercises;

// These helpers consume accepted predecessor history, never the event being
// reconstructed. The frozen V11 setup and random stream remain unchanged.
internal static class CampaignGameTurn
{
    internal static bool IsInitialTurn(ExerciseSession session) =>
        !session.CurrentHistory.Any(value => value is CampaignNativeEndOfTurnV55);

    internal static int CurrentGameTurn(ExerciseSession session) =>
        CurrentGameTurn(session.CurrentSnapshot, session.CurrentHistory);

    internal static int CurrentGameTurn(CampaignSnapshotV11 initial, IEnumerable<object> history) =>
        CurrentGameTurn(initial.Setup.InitialGameTurn, history);

    internal static int CurrentGameTurn(int initialGameTurn, IEnumerable<object> history)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(initialGameTurn, 1);
        ArgumentNullException.ThrowIfNull(history);
        var current = initialGameTurn;
        foreach (var rollover in history.OfType<CampaignNativeEndOfTurnV55>())
        {
            if (rollover.GameTurn != current || rollover.NextGameTurn != checked(current + 1))
                throw new InvalidOperationException("End of Turn history is not contiguous.");
            current++;
        }
        return current;
    }

    // Replay must apply this check to every turn-stamped candidate before
    // rebuilding it; catalog position IDs alone do not distinguish turns.
    internal static void RequireCurrentGameTurn(ExerciseSession predecessor, int gameTurn) =>
        RequireCurrentGameTurn(predecessor.CurrentSnapshot.Setup.InitialGameTurn, predecessor.CurrentHistory, gameTurn);

    internal static void RequireCurrentGameTurn(int initialGameTurn, IEnumerable<object> predecessorHistory, int gameTurn)
    {
        if (gameTurn != CurrentGameTurn(initialGameTurn, predecessorHistory))
            throw new InvalidOperationException("Event Game Turn does not match accepted predecessor history.");
    }

    internal static LandSide LatestInitiativeHolder(IEnumerable<object> history, LandSide snapshotHolder) =>
        history.OfType<CampaignNativeTurnInitiativeV56>().LastOrDefault()?.Holder ?? snapshotHolder;

    // ExerciseSession delegates to this chronological last-drawing-event helper.
    internal static RandomStreamState LatestCursor(RandomStreamState initial, IEnumerable<object> history) =>
        history.LastOrDefault(value => value is CampaignNativeCombatAssaultResultV22
            or CampaignNativeWeatherDeterminationV50 or CampaignNativeTurnInitiativeV56) switch
        {
            CampaignNativeCombatAssaultResultV22 assault => assault.AfterRandomState,
            CampaignNativeWeatherDeterminationV50 weather => new RandomStreamState(initial.ContractVersion,
                initial.AlgorithmId, initial.Seed, weather.ResultRandomCursor),
            CampaignNativeTurnInitiativeV56 initiative => new RandomStreamState(initial.ContractVersion,
                initial.AlgorithmId, initial.Seed, initiative.ResultRandomCursor),
            _ => initial,
        };
    internal readonly record struct EventInfo(int ContractVersion, string CampaignId, long PriorStateVersion,
        string NextPositionId, int GameTurn, string WorldHash);

    internal static EventInfo BoundaryInfo(object value) => value switch
    {
        CampaignNativeEndOfTurnV55 e => new(e.ContractVersion, e.CampaignId, e.PriorStateVersion, e.NextPositionId, e.GameTurn, e.WorldHash),
        CampaignNativeTurnInitiativeV56 e => new(e.ContractVersion, e.CampaignId, e.PriorStateVersion, e.NextPositionId, e.GameTurn, e.WorldHash),
        CampaignNativeNoObligationTurnPhaseV57 e => new(e.ContractVersion, e.CampaignId, e.PriorStateVersion, e.NextPositionId, e.GameTurn, e.WorldHash),
        CampaignNativeInitiativeDeclarationV49 e => new(e.ContractVersion, e.CampaignId, e.PriorStateVersion, e.NextPositionId, e.GameTurn, e.WorldHash),
        CampaignNativeWeatherDeterminationV50 e => new(e.ContractVersion, e.CampaignId, e.PriorStateVersion, e.NextPositionId, e.GameTurn, e.WorldHash),
        CampaignNativeNoObligationOrganizationV51 e => new(e.ContractVersion, e.CampaignId, e.PriorStateVersion, e.NextPositionId, e.GameTurn, e.WorldHash),
        CampaignNativeNoObligationNavalConvoyArrivalV52 e => new(e.ContractVersion, e.CampaignId, e.PriorStateVersion, e.NextPositionId, e.GameTurn, e.WorldHash),
        CampaignNativeNoObligationFleetAssignmentV53 e => new(e.ContractVersion, e.CampaignId, e.PriorStateVersion, e.NextPositionId, e.GameTurn, e.WorldHash),
        CampaignNativeNoObligationFleetRepairV54 e => new(e.ContractVersion, e.CampaignId, e.PriorStateVersion, e.NextPositionId, e.GameTurn, e.WorldHash),
        _ => throw new InvalidOperationException("Not a turn-boundary event."),
    };
}
