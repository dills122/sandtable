using Cna.Core.Campaigns;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Exercises;

// These helpers consume accepted predecessor history, never the event being
// reconstructed. The frozen V11 setup and random stream remain unchanged.
internal static class CampaignGameTurn
{
    internal static int CurrentGameTurn(ExerciseSession session) =>
        CurrentGameTurn(session.CurrentSnapshot.Setup.InitialGameTurn, session.CurrentHistory);

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
    internal static void RequireCurrentGameTurn(int initialGameTurn, IEnumerable<object> predecessorHistory, int gameTurn)
    {
        if (gameTurn != CurrentGameTurn(initialGameTurn, predecessorHistory))
            throw new InvalidOperationException("Event Game Turn does not match accepted predecessor history.");
    }

    internal static LandSide LatestInitiativeHolder(IEnumerable<object> history, LandSide snapshotHolder) =>
        history.OfType<CampaignNativeTurnInitiativeV56>().LastOrDefault()?.Holder ?? snapshotHolder;

    // B4 will delegate ExerciseSession.LatestCursor to this helper (or add
    // V56 to its existing last-drawing-event filter and corresponding arm).
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
}
