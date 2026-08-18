using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal static class InitiativeResolver
{
    public static RuleReference TimingSourceReference { get; } = new(
        "spi-1979-land-rules",
        "7.12");

    public static RuleReference PredeterminedSourceReference { get; } = new(
        "spi-1979-land-rules",
        "7.15");

    public static InitiativeResolution Resolve(
        int gameTurn,
        InitiativePolicy policy,
        RandomStreamState randomState,
        IReadOnlyList<RuleReference> setupSources)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(randomState);
        ArgumentNullException.ThrowIfNull(setupSources);

        if (randomState.ContractVersion != SandtableRandom.ContractVersion
            || !string.Equals(
                randomState.AlgorithmId,
                SandtableRandom.AlgorithmId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException("The random state is not supported.", nameof(randomState));
        }

        return policy switch
        {
            PredeterminedInitiative predetermined => ResolvePredetermined(
                predetermined,
                randomState,
                setupSources),
            ContestedInitiative contested => ResolveContested(
                gameTurn,
                contested,
                randomState,
                setupSources),
            _ => throw new ArgumentException("The Initiative policy is not supported.", nameof(policy)),
        };
    }

    private static InitiativeResolution ResolvePredetermined(
        PredeterminedInitiative policy,
        RandomStreamState randomState,
        IReadOnlyList<RuleReference> setupSources) => new(
            new PredeterminedInitiativeOutcome(policy.Holder),
            randomState,
            BuildSources(
                setupSources,
                [TimingSourceReference, PredeterminedSourceReference]));

    private static InitiativeResolution ResolveContested(
        int gameTurn,
        ContestedInitiative policy,
        RandomStreamState randomState,
        IReadOnlyList<RuleReference> setupSources)
    {
        var axisPresence = Cna1979InitiativeRatings.ClassifyAxisPresence(policy.AxisFacts);
        var axisRating = Cna1979InitiativeRatings.GetAxis(axisPresence).Rating;
        var commonwealthRating = Cna1979InitiativeRatings.GetCommonwealth(gameTurn).Rating;
        var rounds = new List<InitiativeRollRound>();
        var current = randomState;
        LandSide holder;

        while (true)
        {
            var axisRoll = SandtableRandom.RollD6(current);
            var commonwealthRoll = SandtableRandom.RollD6(axisRoll.State);
            current = commonwealthRoll.State;
            var axisTotal = checked(axisRoll.Value + axisRating);
            var commonwealthTotal = checked(commonwealthRoll.Value + commonwealthRating);
            rounds.Add(new InitiativeRollRound(
                rounds.Count + 1,
                axisRoll.Value,
                axisRating,
                axisTotal,
                commonwealthRoll.Value,
                commonwealthRating,
                commonwealthTotal));

            if (axisTotal == commonwealthTotal)
            {
                continue;
            }

            holder = axisTotal > commonwealthTotal ? LandSide.Axis : LandSide.Commonwealth;
            break;
        }

        var procedureSources = new List<RuleReference>
        {
            TimingSourceReference,
            Cna1979InitiativeRatings.RatingConceptSourceReference,
            Cna1979RandomProcedure.OpposedDiceSourceReference,
            Cna1979InitiativeRatings.RatingChartSourceReference,
        };

        if (policy.AxisFacts.RommelLocation == AxisInitiativeLocation.TripoliTunisiaHoldingBox
            || policy.AxisFacts.GermanLandCombatUnitLocations.Contains(
                AxisInitiativeLocation.TripoliTunisiaHoldingBox))
        {
            procedureSources.Add(Cna1979InitiativeRatings.HoldingBoxExclusionSourceReference);
        }

        return new InitiativeResolution(
            new ContestedInitiativeOutcome(
                policy.AxisFacts,
                axisPresence,
                rounds,
                holder),
            current,
            BuildSources(setupSources, procedureSources));
    }

    private static RuleReference[] BuildSources(
        IEnumerable<RuleReference> setupSources,
        IEnumerable<RuleReference> procedureSources) => setupSources
            .Concat(procedureSources)
            .Distinct()
            .OrderBy(source => source.SourceId, StringComparer.Ordinal)
            .ThenBy(source => source.Locator, StringComparer.Ordinal)
            .ToArray();
}
