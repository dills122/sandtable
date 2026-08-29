using System.Text;
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
                Assert.Equal(5, predetermined.SchemaVersion);
                Assert.Equal(
                    CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations,
                    predetermined.OpeningPreamble.Kind);
                Assert.Equal(
                    [Cna1979SetupCatalog.OpeningPreambleSourceReference],
                    predetermined.OpeningPreamble.Sources);
                Assert.Equal(
                    CampaignWeatherPolicyKind.NoImmediateWeatherEffectSubjects,
                    predetermined.Weather.Kind);
                Assert.Equal(
                    [Cna1979SetupCatalog.WeatherPolicySourceReference],
                    predetermined.Weather.Sources);
                Assert.Equal(1, predetermined.StageEntry.GameTurn);
                Assert.Equal(1, predetermined.StageEntry.OperationStage);
                Assert.All(
                    new[]
                    {
                        predetermined.StageEntry.Organization,
                        predetermined.StageEntry.NavalConvoyArrival,
                        predetermined.StageEntry.FleetAssignment,
                        predetermined.StageEntry.FleetRepair,
                    },
                    value => Assert.Equal(StageEntryObligationKind.ExplicitNone, value));
                Assert.Equal(
                    [CampaignStageEntryPolicy.SourceReference],
                    predetermined.StageEntry.Sources);
                Assert.Equal(
                    Cna1979SyntheticContentCatalog.Artifact.Identity,
                    predetermined.Content.Pack);
                Assert.Equal("movement-contact-lab", predetermined.Content.ScenarioId);
                Assert.Equal(
                    "sha256:9e55e3de11338ba6432768ccb6740a6fed83b37503f69cc7ff8ecd58e205634f",
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
                    CampaignWeatherPolicyKind.NoImmediateWeatherEffectSubjects,
                    contested.Weather.Kind);
                Assert.Equal(
                    [Cna1979SetupCatalog.WeatherPolicySourceReference],
                    contested.Weather.Sources);
                Assert.Equal(43, contested.StageEntry.GameTurn);
                Assert.Equal(1, contested.StageEntry.OperationStage);
                Assert.All(
                    new[]
                    {
                        contested.StageEntry.Organization,
                        contested.StageEntry.NavalConvoyArrival,
                        contested.StageEntry.FleetAssignment,
                        contested.StageEntry.FleetRepair,
                    },
                    value => Assert.Equal(StageEntryObligationKind.ExplicitNone, value));
                Assert.Equal(
                    [CampaignStageEntryPolicy.SourceReference],
                    contested.StageEntry.Sources);
                Assert.Equal(
                    Cna1979SyntheticContentCatalog.Artifact.Identity,
                    contested.Content.Pack);
                Assert.Equal("initiative-contested-lab", contested.Content.ScenarioId);
                Assert.Equal(
                    "sha256:8ae5d9d7c922c45376690bd8fd1de93f9937ff757dca0084ca9372dd954c014a",
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
            baseline.Weather,
            baseline.StageEntry,
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
            baseline.Weather,
            baseline.StageEntry,
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
            baseline.Weather,
            baseline.StageEntry,
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
            baseline.Weather,
            baseline.StageEntry,
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
            baseline.Weather,
            baseline.StageEntry,
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
            baseline.Weather,
            baseline.StageEntry,
            baseline.Content,
            baseline.Sources);
        var weatherChanged = new CampaignSetupDefinition(
            baseline.SchemaVersion,
            baseline.SetupId,
            baseline.DisplayName,
            baseline.IsSynthetic,
            baseline.InitialGameTurn,
            baseline.InitialInitiative,
            baseline.OpeningPreamble,
            new CampaignWeatherPolicy(
                CampaignWeatherPolicy.CurrentContractVersion,
                CampaignWeatherPolicyKind.NoImmediateWeatherEffectSubjects,
                [new RuleReference("sandtable-rules-lab", "different-weather-policy.v1")]),
            baseline.StageEntry,
            baseline.Content,
            baseline.Sources);
        var stageEntryChanged = new CampaignSetupDefinition(
            baseline.SchemaVersion,
            baseline.SetupId,
            baseline.DisplayName,
            baseline.IsSynthetic,
            baseline.InitialGameTurn,
            baseline.InitialInitiative,
            baseline.OpeningPreamble,
            baseline.Weather,
            CreateStageEntryPolicy(
                baseline.InitialGameTurn,
                organization: StageEntryObligationKind.HasObligations),
            baseline.Content,
            baseline.Sources);

        Assert.Equal(baseline.Hash, displayChanged.Hash);
        Assert.NotEqual(baseline.Hash, policyChanged.Hash);
        Assert.NotEqual(baseline.Hash, contentChanged.Hash);
        Assert.NotEqual(baseline.Hash, scenarioChanged.Hash);
        Assert.NotEqual(baseline.Hash, sourceChanged.Hash);
        Assert.NotEqual(baseline.Hash, openingPreambleChanged.Hash);
        Assert.NotEqual(baseline.Hash, weatherChanged.Hash);
        Assert.NotEqual(baseline.Hash, stageEntryChanged.Hash);
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
            Cna1979SetupCatalog.WeatherPolicy,
            CreateStageEntryPolicy(1),
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
            Cna1979SetupCatalog.WeatherPolicy,
            CreateStageEntryPolicy(1),
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
            Cna1979SetupCatalog.WeatherPolicy,
            CreateStageEntryPolicy(1),
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
            Cna1979SetupCatalog.WeatherPolicy,
            CreateStageEntryPolicy(1),
            Cna1979SetupCatalog.Definitions[0].Content,
            []));
        Assert.Throws<ArgumentNullException>(() => new CampaignSetupDefinition(
            1,
            "rules-lab.test",
            "Test setup",
            true,
            1,
            new PredeterminedInitiative(LandSide.Axis),
            Cna1979SetupCatalog.OpeningPreamblePolicy,
            Cna1979SetupCatalog.WeatherPolicy,
            null!,
            Cna1979SetupCatalog.Definitions[0].Content,
            [new RuleReference("sandtable-rules-lab", "test.v1")]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignOpeningPreamblePolicy(
            2,
            CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations,
            [Cna1979SetupCatalog.OpeningPreambleSourceReference]));
        Assert.Throws<ArgumentException>(() => new CampaignOpeningPreamblePolicy(
            CampaignOpeningPreamblePolicy.CurrentContractVersion,
            CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations,
            []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CampaignWeatherPolicy(
            2,
            CampaignWeatherPolicyKind.NoImmediateWeatherEffectSubjects,
            [Cna1979SetupCatalog.WeatherPolicySourceReference]));
        Assert.Throws<ArgumentException>(() => new CampaignWeatherPolicy(
            CampaignWeatherPolicy.CurrentContractVersion,
            CampaignWeatherPolicyKind.NoImmediateWeatherEffectSubjects,
            []));
    }

    [Fact]
    public void CatalogAdmitsOnlyExactExplicitNonePolicyForTheSetupPair()
    {
        Assert.All(Cna1979SetupCatalog.Definitions, definition =>
            Assert.True(Cna1979SetupCatalog.IsAdmittedStageEntryPolicy(
                definition.StageEntry,
                definition.InitialGameTurn)));
        Assert.False(Cna1979SetupCatalog.IsAdmittedStageEntryPolicy(null, 1));
        Assert.False(Cna1979SetupCatalog.IsAdmittedStageEntryPolicy(
            CreateStageEntryPolicy(2),
            1));

        StageEntryObligationKind[][] unsupportedSubjects =
        [
            [StageEntryObligationKind.HasObligations, StageEntryObligationKind.ExplicitNone,
                StageEntryObligationKind.ExplicitNone, StageEntryObligationKind.ExplicitNone],
            [StageEntryObligationKind.ExplicitNone, StageEntryObligationKind.HasObligations,
                StageEntryObligationKind.ExplicitNone, StageEntryObligationKind.ExplicitNone],
            [StageEntryObligationKind.ExplicitNone, StageEntryObligationKind.ExplicitNone,
                StageEntryObligationKind.HasObligations, StageEntryObligationKind.ExplicitNone],
            [StageEntryObligationKind.ExplicitNone, StageEntryObligationKind.ExplicitNone,
                StageEntryObligationKind.ExplicitNone, StageEntryObligationKind.HasObligations],
        ];

        Assert.All(unsupportedSubjects, subjects =>
            Assert.False(Cna1979SetupCatalog.IsAdmittedStageEntryPolicy(
                CreateStageEntryPolicy(
                    1,
                    subjects[0],
                    subjects[1],
                    subjects[2],
                    subjects[3]),
                1)));
    }

    [Fact]
    public void SetupHashEmbedsStageEntryPolicyInFrozenCanonicalOrder()
    {
        var canonical = Encoding.UTF8.GetString(CampaignSetupHash.SerializeCanonical(
            Cna1979SetupCatalog.Definitions[0]));

        Assert.Equal(
            "{\"schemaVersion\":5,\"setupId\":\"rules-lab.initiative.predetermined\"," +
            "\"isSynthetic\":true,\"initialGameTurn\":1," +
            "\"initialInitiative\":{\"kind\":\"predetermined\",\"holder\":\"axis\"}," +
            "\"openingPreamble\":{\"contractVersion\":1," +
            "\"kind\":\"no-opening-naval-convoy-obligations\",\"sources\":[{" +
            "\"sourceId\":\"sandtable-rules-lab\"," +
            "\"locator\":\"opening-preamble.no-naval-convoy-obligations.v1\"}]}," +
            "\"weather\":{\"contractVersion\":1," +
            "\"kind\":\"no-immediate-weather-effect-subjects\",\"sources\":[{" +
            "\"sourceId\":\"sandtable-rules-lab\"," +
            "\"locator\":\"weather.no-immediate-effect-subjects.v1\"}]}," +
            "\"stageEntry\":{\"contractVersion\":1,\"gameTurn\":1," +
            "\"operationStage\":1,\"organization\":\"explicit-none\"," +
            "\"navalConvoyArrival\":\"explicit-none\"," +
            "\"fleetAssignment\":\"explicit-none\"," +
            "\"fleetRepair\":\"explicit-none\",\"sources\":[{" +
            "\"sourceId\":\"sandtable-rules-lab\"," +
            "\"locator\":\"stage-entry.no-obligations.v1\"}]}," +
            "\"content\":{\"schemaVersion\":4," +
            "\"formatId\":\"sandtable.content-json.v3\"," +
            "\"packId\":\"rules-lab.content.movement-contact.v1\"," +
            "\"rulesetId\":\"cna-1979.1\"," +
            "\"hash\":\"sha256:40f0e7a0a8876e4fefc4f06c1d752253cf338da614e587b9ff017e04541e7d79\"," +
            "\"scenarioId\":\"movement-contact-lab\"},\"sources\":[{" +
            "\"sourceId\":\"sandtable-rules-lab\"," +
            "\"locator\":\"initiative.predetermined-axis.v1\"}]}",
            canonical);
    }

    private static CampaignStageEntryPolicy CreateStageEntryPolicy(
        int gameTurn,
        StageEntryObligationKind organization = StageEntryObligationKind.ExplicitNone,
        StageEntryObligationKind navalConvoyArrival = StageEntryObligationKind.ExplicitNone,
        StageEntryObligationKind fleetAssignment = StageEntryObligationKind.ExplicitNone,
        StageEntryObligationKind fleetRepair = StageEntryObligationKind.ExplicitNone) => new(
            CampaignStageEntryPolicy.CurrentContractVersion,
            gameTurn,
            1,
            organization,
            navalConvoyArrival,
            fleetAssignment,
            fleetRepair,
            [CampaignStageEntryPolicy.SourceReference]);
}
