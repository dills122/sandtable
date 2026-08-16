using Cna.Core.Campaigns;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class InitiativeResolverTests
{
    [Fact]
    public void PredeterminedPolicyConsumesNoRandomBytes()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var initial = SandtableRandom.Create(12345);

        var resolution = InitiativeResolver.Resolve(
            setup.InitialGameTurn,
            setup.InitialInitiative,
            initial,
            setup.Sources);

        var outcome = Assert.IsType<PredeterminedInitiativeOutcome>(resolution.Outcome);
        Assert.Equal(LandSide.Axis, outcome.Holder);
        Assert.Equal(initial, resolution.RandomState);
        Assert.Equal(
            [
                Cna1979SetupCatalog.PredeterminedSourceReference,
                InitiativeResolver.TimingSourceReference,
                InitiativeResolver.PredeterminedSourceReference,
            ],
            resolution.Sources);
    }

    [Fact]
    public void ContestedPolicyDerivesRatingsAndDrawsAxisThenCommonwealth()
    {
        var setup = Cna1979SetupCatalog.Definitions[1];

        var resolution = InitiativeResolver.Resolve(
            setup.InitialGameTurn,
            setup.InitialInitiative,
            SandtableRandom.Create(0),
            setup.Sources);

        var outcome = Assert.IsType<ContestedInitiativeOutcome>(resolution.Outcome);
        var round = Assert.Single(outcome.Rounds);
        Assert.Equal(AxisInitiativePresence.GermanLandCombatUnitOnQualifyingGameMap, outcome.AxisPresence);
        Assert.Equal(1, round.Round);
        Assert.Equal(6, round.AxisDie);
        Assert.Equal(3, round.AxisRating);
        Assert.Equal(9, round.AxisTotal);
        Assert.Equal(6, round.CommonwealthDie);
        Assert.Equal(4, round.CommonwealthRating);
        Assert.Equal(10, round.CommonwealthTotal);
        Assert.Equal(LandSide.Commonwealth, outcome.Holder);
        Assert.Equal(2UL, resolution.RandomState.NextByteCursor);
        Assert.Equal(
            [
                Cna1979SetupCatalog.ContestedSourceReference,
                Cna1979InitiativeRatings.RatingChartSourceReference,
                InitiativeResolver.TimingSourceReference,
                Cna1979InitiativeRatings.RatingConceptSourceReference,
                Cna1979RandomProcedure.OpposedDiceSourceReference,
            ],
            resolution.Sources);
    }

    [Fact]
    public void ContestedPolicyRetainsCompleteTieRoundsAndRejectedCandidates()
    {
        var setup = Cna1979SetupCatalog.Definitions[1];

        var resolution = InitiativeResolver.Resolve(
            setup.InitialGameTurn,
            setup.InitialInitiative,
            SandtableRandom.Create(7),
            setup.Sources);

        var outcome = Assert.IsType<ContestedInitiativeOutcome>(resolution.Outcome);
        Assert.Collection(
            outcome.Rounds,
            first =>
            {
                Assert.Equal((1, 5, 8, 4, 8), (
                    first.Round,
                    first.AxisDie,
                    first.AxisTotal,
                    first.CommonwealthDie,
                    first.CommonwealthTotal));
            },
            second =>
            {
                Assert.Equal((2, 5, 8, 6, 10), (
                    second.Round,
                    second.AxisDie,
                    second.AxisTotal,
                    second.CommonwealthDie,
                    second.CommonwealthTotal));
            });
        Assert.Equal(LandSide.Commonwealth, outcome.Holder);
        Assert.Equal(5UL, resolution.RandomState.NextByteCursor);
    }

    [Fact]
    public void HoldingBoxFactsAddTheExactExclusionProvenance()
    {
        var policy = new ContestedInitiative(new AxisInitiativeSourceFacts(
            AxisInitiativeLocation.TripoliTunisiaHoldingBox,
            [
                AxisInitiativeLocation.QualifyingGameMap,
                AxisInitiativeLocation.TripoliTunisiaHoldingBox,
            ]));

        var resolution = InitiativeResolver.Resolve(
            43,
            policy,
            SandtableRandom.Create(0),
            [new RuleReference("sandtable-rules-lab", "holding-box-test.v1")]);

        Assert.Contains(
            Cna1979InitiativeRatings.HoldingBoxExclusionSourceReference,
            resolution.Sources);
    }

    [Fact]
    public void ContestedOutcomeCopiesRoundsAndComparesThemStructurally()
    {
        var facts = new AxisInitiativeSourceFacts(
            AxisInitiativeLocation.OffMapOrUnavailable,
            [AxisInitiativeLocation.QualifyingGameMap]);
        var rounds = new List<InitiativeRollRound>
        {
            new(1, 6, 3, 9, 6, 4, 10),
        };
        var first = new ContestedInitiativeOutcome(
            facts,
            AxisInitiativePresence.GermanLandCombatUnitOnQualifyingGameMap,
            rounds,
            LandSide.Commonwealth);
        var equivalent = new ContestedInitiativeOutcome(
            facts,
            AxisInitiativePresence.GermanLandCombatUnitOnQualifyingGameMap,
            rounds.ToArray(),
            LandSide.Commonwealth);

        rounds.Clear();

        Assert.Single(first.Rounds);
        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
    }

    [Fact]
    public void InitiativeEventCopiesAndComparesSourcesStructurally()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var resolution = InitiativeResolver.Resolve(
            setup.InitialGameTurn,
            setup.InitialInitiative,
            SandtableRandom.Create(1),
            setup.Sources);
        var sources = resolution.Sources.Reverse().ToList();
        var position = Cna1979LandSequence.CreateTurn(1)[1];
        var first = new InitiativeDetermined(
            "campaign-1",
            2,
            "land.position.initiative-determination",
            resolution.Outcome,
            SandtableRandom.AlgorithmId,
            0,
            0,
            position,
            sources);
        var equivalent = new InitiativeDetermined(
            "campaign-1",
            2,
            "land.position.initiative-determination",
            resolution.Outcome,
            SandtableRandom.AlgorithmId,
            0,
            0,
            position,
            resolution.Sources);

        sources.Clear();

        Assert.Equal(resolution.Sources, first.Sources);
        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
    }
}
