using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignCreationV9Tests
{
    [Fact]
    public void CreationTruthBindsExactV5ContentIdentityAndSeededInitialWorld()
    {
        var artifact = ContentPackV5Artifact.Create(
            ZocReactionContentTestData.CreatePositiveFixture());
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var setup = CreateSetup(artifact, scenario);

        var created = CampaignCreationV9Factory.Create(
            "campaign-1",
            Cna1979Ruleset.Manifest.Hash,
            setup,
            artifact,
            scenario,
            new RandomStreamState(1, SandtableRandom.AlgorithmId, 12345, 0),
            Cna1979LandSequence.CreateTurn(1)[0]);

        Assert.Equal(9, created.ContractVersion);
        Assert.Equal(1, created.StateVersion);
        Assert.Equal(setup.SetupId, created.Setup.SetupId);
        Assert.NotEqual(setup.SetupHash, created.Setup.SetupHash);
        Assert.Equal(artifact.Identity, created.Setup.Content.Pack);
        Assert.Equal(scenario.ScenarioId, created.Setup.Content.ScenarioId);
        Assert.Equal(
            typeof(CampaignContentV5Selection),
            typeof(CampaignSetupSnapshotV5).GetProperty(nameof(CampaignSetupSnapshotV5.Content))!
                .PropertyType);
        Assert.Equal(5, created.InitialWorld.ContractVersion);
        Assert.All(created.InitialWorld.Elements, element =>
        {
            var seed = artifact.Definition.InitialPlacementCombatFacts
                .Single(value => value.ElementId == element.ElementId)
                .InitialComponentToes.Single();
            var state = Assert.Single(element.Components);
            Assert.Equal(seed.CurrentToe, state.CurrentToe);
            Assert.Equal(seed.Origin, state.InitialToeOrigin);
        });
    }

    [Fact]
    public void CreationRejectsForeignScenarioOrSetupContentSelection()
    {
        var artifact = ContentPackV5Artifact.Create(
            ZocReactionContentTestData.CreatePositiveFixture());
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var setup = CreateSetup(artifact, scenario);
        var foreignScenario = ContentTestData.CreateMinimalPack().Scenarios.Single();
        var template = Cna1979SetupCatalog.Definitions[0];
        var mismatchedSetup = CampaignSetupSnapshot.FromDefinition(template);
        var wrongLegacyIdentity = new ContentPackIdentity(
            setup.Content.Pack.SchemaVersion,
            setup.Content.Pack.FormatId,
            setup.Content.Pack.PackId,
            setup.Content.Pack.RulesetId,
            setup.Content.Pack.Hash[..^1] + (setup.Content.Pack.Hash[^1] == '0' ? "1" : "0"));
        var rehashedMismatch = CopySetup(
            setup,
            new CampaignContentSelection(wrongLegacyIdentity, scenario.ScenarioId));
        var wrongStart = new CampaignSetupSnapshot(
            setup.SchemaVersion,
            setup.SetupId,
            setup.SetupHash,
            setup.IsSynthetic,
            checked(setup.InitialGameTurn + 1),
            setup.InitialInitiative,
            setup.OpeningPreamble,
            setup.Weather,
            setup.StageEntry,
            setup.Content,
            setup.Sources);

        Assert.Throws<ArgumentException>(() => CampaignCreationV9Factory.Create(
            "campaign-1",
            Cna1979Ruleset.Manifest.Hash,
            setup,
            artifact,
            foreignScenario,
            new RandomStreamState(1, SandtableRandom.AlgorithmId, 12345, 0),
            Cna1979LandSequence.CreateTurn(1)[0]));
        Assert.Throws<ArgumentException>(() => CampaignCreationV9Factory.Create(
            "campaign-1",
            Cna1979Ruleset.Manifest.Hash,
            wrongStart,
            artifact,
            scenario,
            new RandomStreamState(1, SandtableRandom.AlgorithmId, 12345, 0),
            Cna1979LandSequence.CreateTurn(1)[0]));
        Assert.Throws<ArgumentException>(() => CampaignCreationV9Factory.Create(
            "campaign-1",
            Cna1979Ruleset.Manifest.Hash,
            rehashedMismatch,
            artifact,
            scenario,
            new RandomStreamState(1, SandtableRandom.AlgorithmId, 12345, 0),
            Cna1979LandSequence.CreateTurn(1)[0]));
        Assert.Throws<ArgumentException>(() => CampaignCreationV9Factory.Create(
            "campaign-1",
            Cna1979Ruleset.Manifest.Hash,
            mismatchedSetup,
            artifact,
            scenario,
            new RandomStreamState(1, SandtableRandom.AlgorithmId, 12345, 0),
            Cna1979LandSequence.CreateTurn(1)[0]));
    }

    [Fact]
    public void SuccessorSetupHashBindsV5ContentIdentity()
    {
        var artifact = ContentPackV5Artifact.Create(
            ZocReactionContentTestData.CreatePositiveFixture());
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var setup = CreateSetup(artifact, scenario);
        var baseline = CampaignSetupSnapshotV5.FromPredecessor(
            setup,
            new CampaignContentV5Selection(artifact.Identity, scenario.ScenarioId));
        var changedIdentity = new ContentPackV5Identity(
            artifact.Identity.SchemaVersion,
            artifact.Identity.FormatId,
            artifact.Identity.PackId,
            artifact.Identity.RulesetId,
            artifact.Identity.Hash[..^1]
                + (artifact.Identity.Hash[^1] == '0' ? "1" : "0"));
        var changed = CampaignSetupSnapshotV5.FromPredecessor(
            setup,
            new CampaignContentV5Selection(changedIdentity, scenario.ScenarioId));

        Assert.Matches("^sha256:[0-9a-f]{64}$", baseline.SetupHash);
        Assert.NotEqual(baseline.SetupHash, changed.SetupHash);
    }

    [Fact]
    public void ActiveCreationContractRemainsEight()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var result = CampaignTestHarness.Decide(
            null,
            CampaignTestHarness.Create(
                "campaign-active",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setup.SetupId,
                setup.Hash));

        Assert.True(result.IsAccepted);
        Assert.Equal(8, Assert.IsType<CampaignCreated>(Assert.Single(result.Events)).ContractVersion);
    }

    private static CampaignSetupSnapshot CreateSetup(
        ContentPackV5Artifact artifact,
        ContentScenario scenario)
    {
        var template = Cna1979SetupCatalog.Definitions[0];
        var legacyArtifact = ContentPackArtifact.Create(artifact.Definition.LegacyDefinition);
        return CampaignSetupSnapshot.FromDefinition(new CampaignSetupDefinition(
            template.SchemaVersion,
            "zoc-reaction-setup",
            "ZOC reaction setup",
            true,
            scenario.Start.GameTurn,
            template.InitialInitiative,
            template.OpeningPreamble,
            template.Weather,
            template.StageEntry,
            new CampaignContentSelection(legacyArtifact.Identity, scenario.ScenarioId),
            template.Sources));
    }

    private static CampaignSetupSnapshot CopySetup(
        CampaignSetupSnapshot setup,
        CampaignContentSelection content) => new(
            setup.SchemaVersion,
            setup.SetupId,
            setup.SetupHash,
            setup.IsSynthetic,
            setup.InitialGameTurn,
            setup.InitialInitiative,
            setup.OpeningPreamble,
            setup.Weather,
            setup.StageEntry,
            content,
            setup.Sources);
}
