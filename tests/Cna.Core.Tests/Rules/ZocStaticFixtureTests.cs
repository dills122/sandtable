using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class ZocStaticFixtureTests
{
    [Fact]
    public void PositiveFixtureControlsExactlyPermittedNeighbors()
    {
        var source = QualifyingSource();

        var controlled = Cna1979Zoc.DeriveControlledLocationIds(
        [
            Candidate("permitted-clear", source, []),
            Candidate("permitted-road", source, ["land.edge.road"]),
            Candidate("blocked-sea", source, [Cna1979Zoc.AllSeaHexsideId]),
            Candidate("blocked-river", source, [Cna1979Zoc.MajorRiverHexsideId]),
            Candidate("blocked-lake", source, [Cna1979Zoc.LakeHexsideId]),
            Candidate("blocked-escarpment", source, [Cna1979Zoc.EscarpmentHexsideId]),
            Candidate("blocked-enterability", source, [], canEnter: false),
        ]);

        Assert.Equal(["permitted-clear", "permitted-road"], controlled);
    }

    [Fact]
    public void OverlappingQualifiedSourcesRemainNonAdditive()
    {
        var source = QualifyingSource();

        var controlled = Cna1979Zoc.DeriveControlledLocationIds(
        [
            Candidate("shared-destination", source, []),
            Candidate("shared-destination", source, ["land.edge.road"]),
        ]);

        Assert.Equal(["shared-destination"], controlled);
    }

    public static TheoryData<string, ZocSourceFacts, ZocSourceFailureKind> NamedSourceNegatives =>
        new()
        {
            {
                "lone battalion",
                Source(stacking: 1, rawDefense: 10),
                ZocSourceFailureKind.InsufficientStackingPoints
            },
            {
                "aggregate stacking no greater than one",
                Source(stacking: 0, rawDefense: 10),
                ZocSourceFailureKind.InsufficientStackingPoints
            },
            {
                "raw defense below ten",
                Source(stacking: 2, rawDefense: 9),
                ZocSourceFailureKind.InsufficientRawDefensiveCloseAssaultPoints
            },
            {
                "cohesion minus twenty-six",
                Source(stacking: 2, rawDefense: 10, cohesion: -26),
                ZocSourceFailureKind.CohesionTooLow
            },
            {
                "excluded noncombat category",
                Source(
                    stacking: 2,
                    rawDefense: 10,
                    classificationId: Cna1979Combat.TruckConvoyClassificationId),
                ZocSourceFailureKind.ExcludedCombatClassification
            },
            {
                "unattached headquarters",
                Source(
                    stacking: 2,
                    rawDefense: 10,
                    classificationId: Cna1979Combat.HeadquartersClassificationId),
                ZocSourceFailureKind.UnattachedHeadquarters
            },
        };

    [Theory]
    [MemberData(nameof(NamedSourceNegatives))]
    public void EveryNamedSourceNegativeFailsIndependently(
        string name,
        ZocSourceFacts source,
        ZocSourceFailureKind expectedFailure)
    {
        var result = Cna1979Zoc.EvaluateSource(source);

        Assert.Equal(ZocRuleEvaluationStatus.NotQualified, result.Status);
        Assert.Equal([expectedFailure], result.Failures);
        Assert.NotEmpty(result.Sources);
        Assert.False(result.IsQualified, name);
    }

    [Fact]
    public void UnsupportedAggregateInputRejectsInsteadOfDefaulting()
    {
        var unsupported = Source(
            stacking: 2,
            rawDefense: 10,
            classificationId: "land.combat-classification.future");

        Assert.Throws<InvalidOperationException>(() =>
            Cna1979Zoc.DeriveControlledLocationIds(
            [
                Candidate("destination", unsupported, []),
            ]));
    }

    private static ZocControlCandidate Candidate(
        string destination,
        ZocSourceFacts source,
        IEnumerable<string> features,
        bool canEnter = true) => new(
            destination,
            source,
            new ZocProjectionFacts(features, canEnter));

    private static ZocSourceFacts QualifyingSource() => Source(stacking: 2, rawDefense: 10);

    private static ZocSourceFacts Source(
        int stacking,
        long rawDefense,
        int cohesion = 0,
        string? classificationId = null) => new(
            classificationId ?? Cna1979Combat.CombatUnitClassificationId,
            stacking,
            cohesion,
            rawDefense,
            hasAttachedCombatUnits: false);
}
