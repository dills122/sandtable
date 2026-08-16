using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class InitiativeRatingTests
{
    public static TheoryData<int, int> CommonwealthBoundaryRatings => new()
    {
        { 1, 3 },
        { 42, 3 },
        { 43, 4 },
        { 90, 4 },
        { 91, 5 },
        { 111, 5 },
    };

    [Theory]
    [MemberData(nameof(CommonwealthBoundaryRatings))]
    public void CommonwealthRatingUsesPublishedGameTurnBands(int gameTurn, int expectedRating)
    {
        var row = Cna1979InitiativeRatings.GetCommonwealth(gameTurn);

        Assert.Equal(expectedRating, row.Rating);
        Assert.True(row.Turns.Contains(gameTurn));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(112)]
    public void CommonwealthRatingRejectsTurnsOutsideThePublishedTable(int gameTurn)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Cna1979InitiativeRatings.GetCommonwealth(gameTurn));
    }

    [Theory]
    [InlineData(AxisInitiativePresence.RommelOnQualifyingGameMap, 6)]
    [InlineData(AxisInitiativePresence.GermanLandCombatUnitOnQualifyingGameMap, 3)]
    [InlineData(AxisInitiativePresence.NeitherOnQualifyingGameMap, 1)]
    public void AxisRatingUsesPublishedPresenceCases(
        AxisInitiativePresence presence,
        int expectedRating)
    {
        var row = Cna1979InitiativeRatings.GetAxis(presence);

        Assert.Equal(expectedRating, row.Rating);
        Assert.Equal(presence, row.Presence);
    }

    [Fact]
    public void AxisRatingRejectsUndefinedPresence()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Cna1979InitiativeRatings.GetAxis((AxisInitiativePresence)99));
    }

    [Theory]
    [MemberData(nameof(AxisClassificationCases))]
    public void AxisPresenceIsDerivedFromTypedLocationFacts(
        AxisInitiativeLocation rommelLocation,
        IReadOnlyList<AxisInitiativeLocation> germanLocations,
        AxisInitiativePresence expectedPresence)
    {
        var facts = new AxisInitiativeSourceFacts(rommelLocation, germanLocations);

        var actual = Cna1979InitiativeRatings.ClassifyAxisPresence(facts);

        Assert.Equal(expectedPresence, actual);
    }

    public static TheoryData<
        AxisInitiativeLocation,
        IReadOnlyList<AxisInitiativeLocation>,
        AxisInitiativePresence> AxisClassificationCases => new()
    {
        {
            AxisInitiativeLocation.QualifyingGameMap,
            [AxisInitiativeLocation.OffMapOrUnavailable],
            AxisInitiativePresence.RommelOnQualifyingGameMap
        },
        {
            AxisInitiativeLocation.TripoliTunisiaHoldingBox,
            [AxisInitiativeLocation.QualifyingGameMap],
            AxisInitiativePresence.GermanLandCombatUnitOnQualifyingGameMap
        },
        {
            AxisInitiativeLocation.OffMapOrUnavailable,
            [AxisInitiativeLocation.QualifyingGameMap],
            AxisInitiativePresence.GermanLandCombatUnitOnQualifyingGameMap
        },
        {
            AxisInitiativeLocation.TripoliTunisiaHoldingBox,
            [AxisInitiativeLocation.TripoliTunisiaHoldingBox],
            AxisInitiativePresence.NeitherOnQualifyingGameMap
        },
        {
            AxisInitiativeLocation.OffMapOrUnavailable,
            [],
            AxisInitiativePresence.NeitherOnQualifyingGameMap
        },
    };

    [Fact]
    public void AxisFactsRejectUndefinedAndDuplicateLocationCategories()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new AxisInitiativeSourceFacts(
            (AxisInitiativeLocation)99,
            []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AxisInitiativeSourceFacts(
            AxisInitiativeLocation.OffMapOrUnavailable,
            [(AxisInitiativeLocation)99]));
        Assert.Throws<ArgumentException>(() => new AxisInitiativeSourceFacts(
            AxisInitiativeLocation.OffMapOrUnavailable,
            [
                AxisInitiativeLocation.QualifyingGameMap,
                AxisInitiativeLocation.QualifyingGameMap,
            ]));
    }

    [Fact]
    public void AxisFactsDefensivelyCopyAndCompareLocationCategoriesStructurally()
    {
        var locations = new List<AxisInitiativeLocation>
        {
            AxisInitiativeLocation.OffMapOrUnavailable,
            AxisInitiativeLocation.QualifyingGameMap,
        };
        var facts = new AxisInitiativeSourceFacts(
            AxisInitiativeLocation.TripoliTunisiaHoldingBox,
            locations);
        var reorderedEquivalent = new AxisInitiativeSourceFacts(
            AxisInitiativeLocation.TripoliTunisiaHoldingBox,
            locations.AsEnumerable().Reverse().ToArray());

        locations.Clear();

        Assert.Equal(
            [
                AxisInitiativeLocation.QualifyingGameMap,
                AxisInitiativeLocation.OffMapOrUnavailable,
            ],
            facts.GermanLandCombatUnitLocations);
        Assert.Equal(facts, reorderedEquivalent);
        Assert.Equal(facts.GetHashCode(), reorderedEquivalent.GetHashCode());
    }

    [Fact]
    public void CanonicalRowsExposeExactProvenanceAndStructuralCollectionSemantics()
    {
        var commonwealth = Cna1979InitiativeRatings.CommonwealthRows[0];
        var axis = Cna1979InitiativeRatings.AxisRows[2];
        var copiedSources = commonwealth.Sources.ToList();
        var equivalent = new CommonwealthInitiativeRating(
            commonwealth.SchemaVersion,
            commonwealth.Turns,
            commonwealth.Rating,
            copiedSources.AsEnumerable().Reverse().ToArray());
        var equivalentAxis = new AxisInitiativeRating(
            axis.SchemaVersion,
            axis.Presence,
            axis.Rating,
            axis.Sources.Reverse().ToArray());

        copiedSources.Clear();

        Assert.Equal(
            [
                Cna1979InitiativeRatings.RatingChartSourceReference,
                Cna1979InitiativeRatings.RatingConceptSourceReference,
            ],
            commonwealth.Sources);
        Assert.Contains(
            Cna1979InitiativeRatings.HoldingBoxExclusionSourceReference,
            axis.Sources);
        Assert.Equal(commonwealth, equivalent);
        Assert.Equal(commonwealth.GetHashCode(), equivalent.GetHashCode());
        Assert.Equal(axis, equivalentAxis);
        Assert.Equal(axis.GetHashCode(), equivalentAxis.GetHashCode());
    }

    [Fact]
    public void InitiativeRatingsArtifactIsCanonicalAndHashSensitive()
    {
        var artifact = Cna1979InitiativeRatings.CreateArtifact();
        var baseline = Cna1979InitiativeRatings.CalculateContentHash(
            Cna1979InitiativeRatings.CommonwealthRows,
            Cna1979InitiativeRatings.AxisRows);
        var reordered = Cna1979InitiativeRatings.CalculateContentHash(
            Cna1979InitiativeRatings.CommonwealthRows.Reverse(),
            Cna1979InitiativeRatings.AxisRows.Reverse());
        var changedRows = Cna1979InitiativeRatings.CommonwealthRows.ToArray();
        var changed = changedRows[0];
        changedRows[0] = new CommonwealthInitiativeRating(
            changed.SchemaVersion,
            changed.Turns,
            changed.Rating + 1,
            changed.Sources);
        var changedHash = Cna1979InitiativeRatings.CalculateContentHash(
            changedRows,
            Cna1979InitiativeRatings.AxisRows);

        Assert.Equal("cna-1979.1.initiative-ratings", artifact.ArtifactId);
        Assert.Equal(baseline, artifact.ContentHash);
        Assert.Equal(
            "sha256:d9219cffbba133c7531f00471e9619b27602cf9dfd87ee105f103cb646122c96",
            baseline);
        Assert.Equal(baseline, reordered);
        Assert.NotEqual(baseline, changedHash);
        Assert.Matches("^sha256:[0-9a-f]{64}$", baseline);
        Assert.Equal(
            [
                Cna1979InitiativeRatings.RatingChartSourceReference,
                Cna1979InitiativeRatings.HoldingBoxExclusionSourceReference,
                Cna1979InitiativeRatings.RatingConceptSourceReference,
            ],
            artifact.Sources);
    }
}
