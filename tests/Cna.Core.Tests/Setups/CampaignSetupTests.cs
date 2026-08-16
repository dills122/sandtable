using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Setups;

public sealed class CampaignSetupTests
{
    [Fact]
    public void CatalogContainsOnlyTheTwoSyntheticInitiativeLabSetups()
    {
        Assert.Collection(
            Cna1979SetupCatalog.Definitions,
            predetermined =>
            {
                Assert.Equal("rules-lab.initiative.predetermined", predetermined.SetupId);
                Assert.True(predetermined.IsSynthetic);
                Assert.Equal(1, predetermined.InitialGameTurn);
                var policy = Assert.IsType<PredeterminedInitiative>(
                    predetermined.InitialInitiative);
                Assert.Equal(LandSide.Axis, policy.Holder);
                Assert.Equal(
                    [Cna1979SetupCatalog.PredeterminedSourceReference],
                    predetermined.Sources);
                Assert.Equal(
                    "sha256:ef7dd9cf4cf78616f5b8e2c95408c7fbf03eae46c934238be541565390e2520f",
                    predetermined.Hash);
            },
            contested =>
            {
                Assert.Equal("rules-lab.initiative.contested", contested.SetupId);
                Assert.True(contested.IsSynthetic);
                Assert.Equal(43, contested.InitialGameTurn);
                var policy = Assert.IsType<ContestedInitiative>(contested.InitialInitiative);
                Assert.Equal(
                    AxisInitiativeLocation.OffMapOrUnavailable,
                    policy.AxisFacts.RommelLocation);
                Assert.Equal(
                    [AxisInitiativeLocation.QualifyingGameMap],
                    policy.AxisFacts.GermanLandCombatUnitLocations);
                Assert.Equal(
                    [Cna1979SetupCatalog.ContestedSourceReference],
                    contested.Sources);
                Assert.Equal(
                    "sha256:7f979c371c0c773aac87af3119011a9b37f0633dc75733ec95b3ac055015aa43",
                    contested.Hash);
            });
    }

    [Fact]
    public void CatalogLookupAcceptsKnownIdsAndRejectsUnknownIds()
    {
        Assert.True(Cna1979SetupCatalog.TryGet(
            "rules-lab.initiative.predetermined",
            out var known));
        Assert.Equal(Cna1979SetupCatalog.Definitions[0], known);
        Assert.False(Cna1979SetupCatalog.TryGet("rules-lab.unknown", out var unknown));
        Assert.Null(unknown);
        Assert.False(Cna1979SetupCatalog.TryGet(" ", out _));
        Assert.False(Cna1979SetupCatalog.TryGet(null, out _));
    }

    [Fact]
    public void SetupHashCoversPolicyFactsAndSourcesButNotDisplayName()
    {
        var rulesetHash = Cna1979Ruleset.Manifest.Hash;
        var baseline = Cna1979SetupCatalog.Definitions[1];
        var displayChanged = new CampaignSetupDefinition(
            baseline.SchemaVersion,
            baseline.SetupId,
            "Different presentation text",
            baseline.IsSynthetic,
            baseline.InitialGameTurn,
            baseline.InitialInitiative,
            baseline.Sources);
        var policyChanged = new CampaignSetupDefinition(
            baseline.SchemaVersion,
            baseline.SetupId,
            baseline.DisplayName,
            baseline.IsSynthetic,
            baseline.InitialGameTurn,
            new ContestedInitiative(new AxisInitiativeSourceFacts(
                AxisInitiativeLocation.QualifyingGameMap,
                [])),
            baseline.Sources);
        var sourceChanged = new CampaignSetupDefinition(
            baseline.SchemaVersion,
            baseline.SetupId,
            baseline.DisplayName,
            baseline.IsSynthetic,
            baseline.InitialGameTurn,
            baseline.InitialInitiative,
            [new RuleReference("sandtable-rules-lab", "different-source.v1")]);

        Assert.Equal(baseline.Hash, displayChanged.Hash);
        Assert.NotEqual(baseline.Hash, policyChanged.Hash);
        Assert.NotEqual(baseline.Hash, sourceChanged.Hash);
        Assert.Matches("^sha256:[0-9a-f]{64}$", baseline.Hash);
        Assert.Equal(rulesetHash, Cna1979Ruleset.Manifest.Hash);
        Assert.DoesNotContain(
            Cna1979Ruleset.Manifest.Artifacts,
            artifact => artifact.ArtifactId.Contains("setup", StringComparison.Ordinal));
    }

    [Fact]
    public void SetupDefinitionDefensivelyCopiesAndComparesSourcesStructurally()
    {
        var sources = new List<RuleReference>
        {
            new("sandtable-rules-lab", "source-b"),
            new("sandtable-rules-lab", "source-a"),
        };
        var first = new CampaignSetupDefinition(
            1,
            "rules-lab.test",
            "Test setup",
            true,
            1,
            new PredeterminedInitiative(LandSide.Axis),
            sources);
        var equivalent = new CampaignSetupDefinition(
            1,
            "rules-lab.test",
            "Test setup",
            true,
            1,
            new PredeterminedInitiative(LandSide.Axis),
            sources.AsEnumerable().Reverse().ToArray());

        sources.Clear();

        Assert.Equal(
            [
                new RuleReference("sandtable-rules-lab", "source-a"),
                new RuleReference("sandtable-rules-lab", "source-b"),
            ],
            first.Sources);
        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
    }

    [Fact]
    public void SetupPoliciesRejectInvalidValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PredeterminedInitiative((LandSide)99));
        Assert.Throws<ArgumentNullException>(() => new ContestedInitiative(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignSetupDefinition(
            1,
            "rules-lab.test",
            "Test setup",
            true,
            112,
            new PredeterminedInitiative(LandSide.Axis),
            [new RuleReference("sandtable-rules-lab", "test.v1")]));
        Assert.Throws<ArgumentException>(() => new CampaignSetupDefinition(
            1,
            "rules-lab.test",
            "Test setup",
            true,
            1,
            new PredeterminedInitiative(LandSide.Axis),
            []));
    }
}
