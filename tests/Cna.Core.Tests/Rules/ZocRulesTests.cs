using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class ZocRulesTests
{
    [Fact]
    public void CombatVocabularyIsClosedStableAndActive()
    {
        Assert.Equal(
            [
                ("land.combat-classification.combat-unit", ZocCombatClassificationKind.CombatUnit),
                ("land.combat-classification.headquarters", ZocCombatClassificationKind.Headquarters),
                ("land.combat-classification.truck-convoy", ZocCombatClassificationKind.TruckConvoy),
                ("land.combat-classification.aircraft", ZocCombatClassificationKind.Aircraft),
                ("land.combat-classification.squadron-ground-support", ZocCombatClassificationKind.SquadronGroundSupport),
                ("land.combat-classification.warship", ZocCombatClassificationKind.Warship),
                ("land.combat-classification.informational-marker", ZocCombatClassificationKind.InformationalMarker),
            ],
            Cna1979Combat.Classifications
                .Select(value => (value.ClassificationId, value.Kind)));

        Assert.Equal(
            [
                ("land.combat-component.infantry", ZocCombatComponentKind.Infantry),
            ],
            Cna1979Combat.ComponentClasses
                .Select(value => (value.ComponentClassId, value.Kind)));

        Assert.Equal(
            [
                ("land.edge.road", ZocTopologyFeatureKind.PassThrough),
                ("land.edge.track", ZocTopologyFeatureKind.PassThrough),
                ("land.edge.ridge", ZocTopologyFeatureKind.PassThrough),
                ("land.edge.slope", ZocTopologyFeatureKind.PassThrough),
                ("land.edge.all-sea", ZocTopologyFeatureKind.AllSea),
                ("land.edge.major-river", ZocTopologyFeatureKind.MajorRiver),
                ("land.edge.lake", ZocTopologyFeatureKind.Lake),
                ("land.edge.escarpment", ZocTopologyFeatureKind.Escarpment),
            ],
            Cna1979Zoc.TopologyFeatures
                .Select(value => (value.FeatureId, value.Kind)));

        Assert.True(Cna1979Combat.IsSupportedClassificationId(
            Cna1979Combat.CombatUnitClassificationId));
        Assert.True(Cna1979Combat.IsSupportedComponentClassId(
            Cna1979Combat.InfantryComponentClassId));
        Assert.False(Cna1979Combat.IsSupportedClassificationId(
            "land.combat-classification.unknown"));
        Assert.False(Cna1979Combat.IsSupportedClassificationId(null));
        Assert.False(Cna1979Combat.IsSupportedComponentClassId(
            "land.combat-component.unknown"));
        Assert.False(Cna1979Combat.IsSupportedComponentClassId(null));
        Assert.True(Cna1979Zoc.IsSupportedTopologyFeatureId("land.edge.road"));
        Assert.True(Cna1979Zoc.IsSupportedTopologyFeatureId(Cna1979Zoc.AllSeaHexsideId));
        Assert.False(Cna1979Zoc.IsSupportedTopologyFeatureId("land.edge.unknown"));
        Assert.False(Cna1979Zoc.IsSupportedTopologyFeatureId(null));

        Assert.Equal(8, Cna1979Ruleset.ContractVersion);
        Assert.Contains(
            Cna1979Ruleset.Manifest.Artifacts,
            artifact => artifact.ArtifactId == Cna1979Zoc.AuthorityId);
    }

    [Fact]
    public void QualifyingCombatForceRetainsCompleteRuleProvenance()
    {
        var result = Cna1979Zoc.EvaluateSource(new ZocSourceFacts(
            Cna1979Combat.CombatUnitClassificationId,
            aggregateStackingPoints: 2,
            cohesionLevel: 0,
            rawDefensiveCloseAssaultPoints: 10,
            hasAttachedCombatUnits: false));

        Assert.Equal(ZocRuleEvaluationStatus.Qualified, result.Status);
        Assert.True(result.IsSupported);
        Assert.True(result.IsQualified);
        Assert.Null(result.UnsupportedKind);
        Assert.Empty(result.Failures);
        Assert.Equal(
            ["10.11", "10.12", "10.13", "10.14", "10.15"],
            result.Sources.Select(source => source.Locator));
        Assert.All(result.Sources, source =>
            Assert.Equal("spi-1979-land-rules", source.SourceId));
    }

    public static TheoryData<string, int, int, long, bool, ZocSourceFailureKind>
        NonqualifyingSources => new()
        {
            {
                Cna1979Combat.CombatUnitClassificationId,
                1,
                0,
                10,
                false,
                ZocSourceFailureKind.InsufficientStackingPoints
            },
            {
                Cna1979Combat.CombatUnitClassificationId,
                2,
                0,
                9,
                false,
                ZocSourceFailureKind.InsufficientRawDefensiveCloseAssaultPoints
            },
            {
                Cna1979Combat.CombatUnitClassificationId,
                2,
                -26,
                10,
                false,
                ZocSourceFailureKind.CohesionTooLow
            },
            {
                Cna1979Combat.HeadquartersClassificationId,
                2,
                0,
                10,
                false,
                ZocSourceFailureKind.UnattachedHeadquarters
            },
            {
                Cna1979Combat.TruckConvoyClassificationId,
                2,
                0,
                10,
                false,
                ZocSourceFailureKind.ExcludedCombatClassification
            },
            {
                Cna1979Combat.AircraftClassificationId,
                2,
                0,
                10,
                false,
                ZocSourceFailureKind.ExcludedCombatClassification
            },
            {
                Cna1979Combat.SquadronGroundSupportClassificationId,
                2,
                0,
                10,
                false,
                ZocSourceFailureKind.ExcludedCombatClassification
            },
            {
                Cna1979Combat.WarshipClassificationId,
                2,
                0,
                10,
                false,
                ZocSourceFailureKind.ExcludedCombatClassification
            },
            {
                Cna1979Combat.InformationalMarkerClassificationId,
                2,
                0,
                10,
                false,
                ZocSourceFailureKind.ExcludedCombatClassification
            },
        };

    [Theory]
    [MemberData(nameof(NonqualifyingSources))]
    public void SourceQualificationRejectsEachIndependentRuleVector(
        string classificationId,
        int aggregateStackingPoints,
        int cohesionLevel,
        long rawDefensiveCloseAssaultPoints,
        bool hasAttachedCombatUnits,
        ZocSourceFailureKind expectedFailure)
    {
        var result = Cna1979Zoc.EvaluateSource(new ZocSourceFacts(
            classificationId,
            aggregateStackingPoints,
            cohesionLevel,
            rawDefensiveCloseAssaultPoints,
            hasAttachedCombatUnits));

        Assert.Equal(ZocRuleEvaluationStatus.NotQualified, result.Status);
        Assert.True(result.IsSupported);
        Assert.False(result.IsQualified);
        Assert.Null(result.UnsupportedKind);
        Assert.Contains(expectedFailure, result.Failures);
        Assert.NotEmpty(result.Sources);
    }

    [Fact]
    public void HeadquartersWithAttachedCombatUnitsMayQualify()
    {
        var result = Cna1979Zoc.EvaluateSource(new ZocSourceFacts(
            Cna1979Combat.HeadquartersClassificationId,
            aggregateStackingPoints: 2,
            cohesionLevel: 0,
            rawDefensiveCloseAssaultPoints: 10,
            hasAttachedCombatUnits: true));

        Assert.True(result.IsQualified);
        Assert.Empty(result.Failures);
    }

    [Fact]
    public void UnknownClassificationIsUnsupportedWithoutFallbackOrProvenance()
    {
        var result = Cna1979Zoc.EvaluateSource(new ZocSourceFacts(
            "land.combat-classification.unknown",
            aggregateStackingPoints: 2,
            cohesionLevel: 0,
            rawDefensiveCloseAssaultPoints: 10,
            hasAttachedCombatUnits: false));

        Assert.Equal(ZocRuleEvaluationStatus.Unsupported, result.Status);
        Assert.False(result.IsSupported);
        Assert.False(result.IsQualified);
        Assert.Equal(ZocRuleUnsupportedKind.CombatClassification, result.UnsupportedKind);
        Assert.Empty(result.Failures);
        Assert.Empty(result.Sources);
    }

    public static TheoryData<string, ZocProjectionFailureKind> ExcludedHexsides => new()
    {
        {
            Cna1979Zoc.AllSeaHexsideId,
            ZocProjectionFailureKind.ExcludedHexside
        },
        {
            Cna1979Zoc.MajorRiverHexsideId,
            ZocProjectionFailureKind.ExcludedHexside
        },
        {
            Cna1979Zoc.LakeHexsideId,
            ZocProjectionFailureKind.ExcludedHexside
        },
        {
            Cna1979Zoc.EscarpmentHexsideId,
            ZocProjectionFailureKind.ExcludedHexside
        },
    };

    [Theory]
    [MemberData(nameof(ExcludedHexsides))]
    public void ProjectionRejectsEveryNamedHexsideExclusion(
        string hexsideId,
        ZocProjectionFailureKind expectedFailure)
    {
        var result = Cna1979Zoc.EvaluateProjection(new ZocProjectionFacts(
            [hexsideId],
            canSourceForceEnterDestination: true));

        Assert.Equal(ZocRuleEvaluationStatus.NotQualified, result.Status);
        Assert.Contains(expectedFailure, result.Failures);
        Assert.Contains(result.Sources, source =>
            source.SourceId == "spi-1979-land-rules"
                && source.Locator.StartsWith("10.21", StringComparison.Ordinal));
    }

    [Fact]
    public void ProjectionRequiresEnterabilityAndSupportsFeaturelessEdges()
    {
        var projected = Cna1979Zoc.EvaluateProjection(new ZocProjectionFacts(
            [],
            canSourceForceEnterDestination: true));
        var blocked = Cna1979Zoc.EvaluateProjection(new ZocProjectionFacts(
            [],
            canSourceForceEnterDestination: false));

        Assert.True(projected.IsQualified);
        Assert.Equal(["10.21"], projected.Sources.Select(source => source.Locator));
        Assert.Equal(ZocRuleEvaluationStatus.NotQualified, blocked.Status);
        Assert.Equal(
            [ZocProjectionFailureKind.DestinationNotEnterable],
            blocked.Failures);
        Assert.Equal(["10.21c"], blocked.Sources.Select(source => source.Locator));
    }

    [Fact]
    public void UnknownOrDuplicateTopologyFailsClosed()
    {
        var unknown = Cna1979Zoc.EvaluateProjection(new ZocProjectionFacts(
            ["land.edge.unknown"],
            canSourceForceEnterDestination: true));

        Assert.Equal(ZocRuleEvaluationStatus.Unsupported, unknown.Status);
        Assert.Equal(ZocRuleUnsupportedKind.TopologyFeature, unknown.UnsupportedKind);
        Assert.Empty(unknown.Failures);
        Assert.Empty(unknown.Sources);
        Assert.Throws<ArgumentException>(() => new ZocProjectionFacts(
            [Cna1979Zoc.LakeHexsideId, Cna1979Zoc.LakeHexsideId],
            canSourceForceEnterDestination: true));
    }

    [Fact]
    public void DefensiveCloseAssaultArithmeticIsCheckedAndSourceCited()
    {
        var result = Cna1979Combat.CalculateRawDefensiveCloseAssaultPoints(
            [
                new ZocDefensiveCloseAssaultComponentFact(
                    Cna1979Combat.InfantryComponentClassId,
                    6,
                    1),
                new ZocDefensiveCloseAssaultComponentFact(
                    Cna1979Combat.InfantryComponentClassId,
                    2,
                    2),
            ]);

        Assert.Equal(10, result.RawDefensiveCloseAssaultPoints);
        Assert.Equal(
            ["11.15", "11.3"],
            result.Sources.Select(source => source.Locator));
        Assert.Throws<ArgumentException>(() =>
            Cna1979Combat.CalculateRawDefensiveCloseAssaultPoints([]));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ZocDefensiveCloseAssaultComponentFact(
                Cna1979Combat.InfantryComponentClassId,
                -1,
                1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ZocDefensiveCloseAssaultComponentFact(
                Cna1979Combat.InfantryComponentClassId,
                1,
                -1));
        Assert.Throws<ArgumentException>(() =>
            Cna1979Combat.CalculateRawDefensiveCloseAssaultPoints(
            [
                new ZocDefensiveCloseAssaultComponentFact(
                    "land.combat-component.unknown",
                    10,
                    1),
            ]));
        Assert.Throws<OverflowException>(() =>
            Cna1979Combat.CalculateRawDefensiveCloseAssaultPoints(
            [
                new ZocDefensiveCloseAssaultComponentFact(
                    Cna1979Combat.InfantryComponentClassId,
                    int.MaxValue,
                    int.MaxValue),
                new ZocDefensiveCloseAssaultComponentFact(
                    Cna1979Combat.InfantryComponentClassId,
                    int.MaxValue,
                    int.MaxValue),
                new ZocDefensiveCloseAssaultComponentFact(
                    Cna1979Combat.InfantryComponentClassId,
                    int.MaxValue,
                    int.MaxValue),
            ]));
    }

    [Fact]
    public void InvalidSourceFactsRejectBeforeEvaluation()
    {
        Assert.Throws<ArgumentException>(() => new ZocSourceFacts(
            " ",
            2,
            0,
            10,
            false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ZocSourceFacts(
            Cna1979Combat.CombatUnitClassificationId,
            -1,
            0,
            10,
            false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ZocSourceFacts(
            Cna1979Combat.CombatUnitClassificationId,
            2,
            11,
            10,
            false));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ZocSourceFacts(
            Cna1979Combat.CombatUnitClassificationId,
            2,
            0,
            -1,
            false));
    }

    [Fact]
    public void ClosedDefinitionVocabularyRejectsUndefinedEnumValues()
    {
        var sources = new[] { new RuleReference("test-source", "test-locator") };

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ZocCombatClassificationDefinition(
                "land.combat-classification.forged",
                (ZocCombatClassificationKind)int.MaxValue,
                sources));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ZocCombatComponentDefinition(
                "land.combat-component.forged",
                (ZocCombatComponentKind)int.MaxValue,
                sources));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ZocTopologyFeatureDefinition(
                "land.edge.forged",
                (ZocTopologyFeatureKind)int.MaxValue,
                sources));
    }
}
