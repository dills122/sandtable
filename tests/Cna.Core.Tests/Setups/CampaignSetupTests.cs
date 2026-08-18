using Cna.Core.Content;
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
                Assert.Equal(3, predetermined.SchemaVersion);
                Assert.Equal(
                    CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations,
                    predetermined.OpeningPreamble.Kind);
                Assert.Equal(
                    [Cna1979SetupCatalog.OpeningPreambleSourceReference],
                    predetermined.OpeningPreamble.Sources);
                Assert.Equal(
                    Cna1979SyntheticContentCatalog.Artifact.Identity,
                    predetermined.Content.Pack);
                Assert.Equal("movement-contact-lab", predetermined.Content.ScenarioId);
                Assert.Equal(
                    "sha256:ed20292efd3812382e6c371ea45dd96a0778732be14e865af144db97d3d7dfde",
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
                    CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations,
                    contested.OpeningPreamble.Kind);
                Assert.Equal(
                    Cna1979SyntheticContentCatalog.Artifact.Identity,
                    contested.Content.Pack);
                Assert.Equal("initiative-contested-lab", contested.Content.ScenarioId);
                Assert.Equal(
                    "sha256:a28c5f631853e9868c353774284326ff9fe4a70bc9acf89d8c55382c75fb85e3",
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
            baseline.OpeningPreamble,
            baseline.Content,
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
            baseline.OpeningPreamble,
            baseline.Content,
            baseline.Sources);
        var contentChanged = new CampaignSetupDefinition(
            baseline.SchemaVersion,
            baseline.SetupId,
            baseline.DisplayName,
            baseline.IsSynthetic,
            baseline.InitialGameTurn,
            baseline.InitialInitiative,
            baseline.OpeningPreamble,
            new CampaignContentSelection(
                new ContentPackIdentity(
                    baseline.Content.Pack.SchemaVersion,
                    baseline.Content.Pack.FormatId,
                    baseline.Content.Pack.PackId,
                    baseline.Content.Pack.RulesetId,
                    $"sha256:{new string('0', 64)}"),
                baseline.Content.ScenarioId),
            baseline.Sources);
        var scenarioChanged = new CampaignSetupDefinition(
            baseline.SchemaVersion,
            baseline.SetupId,
            baseline.DisplayName,
            baseline.IsSynthetic,
            baseline.InitialGameTurn,
            baseline.InitialInitiative,
            baseline.OpeningPreamble,
            new CampaignContentSelection(
                baseline.Content.Pack,
                "movement-contact-lab"),
            baseline.Sources);
        var sourceChanged = new CampaignSetupDefinition(
            baseline.SchemaVersion,
            baseline.SetupId,
            baseline.DisplayName,
            baseline.IsSynthetic,
            baseline.InitialGameTurn,
            baseline.InitialInitiative,
            baseline.OpeningPreamble,
            baseline.Content,
            [new RuleReference("sandtable-rules-lab", "different-source.v1")]);
        var openingPreambleChanged = new CampaignSetupDefinition(
            baseline.SchemaVersion,
            baseline.SetupId,
            baseline.DisplayName,
            baseline.IsSynthetic,
            baseline.InitialGameTurn,
            baseline.InitialInitiative,
            new CampaignOpeningPreamblePolicy(
                CampaignOpeningPreamblePolicy.CurrentContractVersion,
                CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations,
                [new RuleReference("sandtable-rules-lab", "different-opening-policy.v1")]),
            baseline.Content,
            baseline.Sources);

        Assert.Equal(baseline.Hash, displayChanged.Hash);
        Assert.NotEqual(baseline.Hash, policyChanged.Hash);
        Assert.NotEqual(baseline.Hash, contentChanged.Hash);
        Assert.NotEqual(baseline.Hash, scenarioChanged.Hash);
        Assert.NotEqual(baseline.Hash, sourceChanged.Hash);
        Assert.NotEqual(baseline.Hash, openingPreambleChanged.Hash);
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
            Cna1979SetupCatalog.SchemaVersion,
            "rules-lab.test",
            "Test setup",
            true,
            1,
            new PredeterminedInitiative(LandSide.Axis),
            Cna1979SetupCatalog.OpeningPreamblePolicy,
            Cna1979SetupCatalog.Definitions[0].Content,
            sources);
        var equivalent = new CampaignSetupDefinition(
            Cna1979SetupCatalog.SchemaVersion,
            "rules-lab.test",
            "Test setup",
            true,
            1,
            new PredeterminedInitiative(LandSide.Axis),
            Cna1979SetupCatalog.OpeningPreamblePolicy,
            Cna1979SetupCatalog.Definitions[0].Content,
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
            Cna1979SetupCatalog.OpeningPreamblePolicy,
            Cna1979SetupCatalog.Definitions[0].Content,
            [new RuleReference("sandtable-rules-lab", "test.v1")]));
        Assert.Throws<ArgumentException>(() => new CampaignSetupDefinition(
            1,
            "rules-lab.test",
            "Test setup",
            true,
            1,
            new PredeterminedInitiative(LandSide.Axis),
            Cna1979SetupCatalog.OpeningPreamblePolicy,
            Cna1979SetupCatalog.Definitions[0].Content,
            []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignOpeningPreamblePolicy(
            2,
            CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations,
            [Cna1979SetupCatalog.OpeningPreambleSourceReference]));
        Assert.Throws<ArgumentException>(() => new CampaignOpeningPreamblePolicy(
            CampaignOpeningPreamblePolicy.CurrentContractVersion,
            CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations,
            []));
    }
}
