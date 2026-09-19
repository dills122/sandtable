using System.Text;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CombatCreationInputsCodecTests
{
    [Fact]
    public void Setup7MatchesGoldenAndReadsBackOnlyWithTrustedContent()
    {
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var setup = CreateSetup(artifact, scenario);
        var golden = SetupGolden();

        Assert.Equal(1_655, golden.Length);
        Assert.Equal(golden, CampaignSetupV7Codec.Serialize(setup));
        Assert.Equal(setup, CampaignSetupV7Codec.Deserialize(golden, artifact, scenario));
        Assert.Equal("sha256:22495e26528c2f4db8335d3b4041194aef26295f30aab0e4e6f384ff17069848",
            setup.SetupHash);
    }

    [Fact]
    public void Setup7RejectsAlteredBytesAndForeignContent()
    {
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var golden = Encoding.UTF8.GetString(SetupGolden());
        foreach (var changed in new[]
        {
            " " + golden,
            golden.Replace("\"cohesionLevel\":0", "\"cohesionLevel\":1", StringComparison.Ordinal),
            golden.Replace("\"initialGameTurn\":1", "\"initialGameTurn\":2", StringComparison.Ordinal),
            golden.Replace("\"schemaVersion\":7", "\"schemaVersion\":6", StringComparison.Ordinal),
            golden.Replace("\"setupId\":", "\"setupId\":\"duplicate\",\"setupId\":", StringComparison.Ordinal),
            golden.Replace("rules-lab.combat.close-assault.v1", "rules-lab.combat.close-\\u0061ssault.v1",
                StringComparison.Ordinal),
            golden[..^1] + ",\"unknown\":0}",
        })
            Assert.ThrowsAny<Exception>(() => CampaignSetupV7Codec.Deserialize(
                Encoding.UTF8.GetBytes(changed), artifact, scenario));
        Assert.ThrowsAny<Exception>(() => CampaignSetupV7Codec.Deserialize(SetupGolden(), artifact,
            new ContentCombatScenario("foreign", scenario.Start, scenario.End, scenario.InitialPlacements,
                scenario.RetreatSupplyAnchors, scenario.Origin)));
    }

    [Fact]
    public void AlternatePredeterminedHolderChangesSetupIdentityWithoutChangingContent()
    {
        var artifact = Cna1979CombatContentCatalog.Artifact;
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        var axis = CreateSetup(artifact, scenario);
        var commonwealth = CreateSetup(artifact, scenario, LandSide.Commonwealth);
        Assert.NotEqual(axis.SetupHash, commonwealth.SetupHash);
        Assert.Equal(commonwealth.SetupHash, CampaignSetupV7Codec.Deserialize(
            CampaignSetupV7Codec.Serialize(commonwealth), artifact, scenario).SetupHash);
    }

    [Fact]
    public void ConfigurationMatchesGoldenAndRejectsAlteredArtifactOrBudget()
    {
        var golden = ConfigGolden();
        var config = CombatDecisionConfigurationCodec.Deserialize(golden, Cna1979CombatRuleset.Manifest);
        Assert.Equal(955, golden.Length);
        Assert.Equal(golden, CombatDecisionConfigurationCodec.Serialize(config));
        Assert.Equal("sha256:3de30c451ba7e81d4fdde6493e07d4d06110209a89035d9e59bba738f0aeba34",
            config.Hash);
        var changed = Encoding.UTF8.GetString(golden).Replace(
            "\"decisionBudgetMilliseconds\":30000", "\"decisionBudgetMilliseconds\":0",
            StringComparison.Ordinal);
        Assert.ThrowsAny<Exception>(() => CombatDecisionConfigurationCodec.Deserialize(
            Encoding.UTF8.GetBytes(changed), Cna1979CombatRuleset.Manifest));
        Assert.ThrowsAny<Exception>(() => CombatDecisionConfigurationCodec.Deserialize(golden,
            Cna1979Ruleset.Manifest));
    }

    [Fact]
    public void ConfigurationWriterRejectsForeignRuleInputsButAllowsExplicitBudgetVariation()
    {
        var golden = ConfigGolden();
        var config = CombatDecisionConfigurationCodec.Deserialize(golden, Cna1979CombatRuleset.Manifest);
        Assert.Throws<ArgumentException>(() => new CombatDecisionConfiguration(config.ConfigId,
            "sha256:" + new string('0', 64), config.Windows));
        var changed = new CombatDecisionConfiguration(config.ConfigId, config.RulesInputHash,
            config.Windows.Select(value => value.Kind == "selection"
                ? value with { DecisionBudgetMilliseconds = 30_001 }
                : value));
        Assert.NotEqual(config.Hash, changed.Hash);
        Assert.Equal(changed.Hash, CombatDecisionConfigurationCodec.Deserialize(
            CombatDecisionConfigurationCodec.Serialize(changed), Cna1979CombatRuleset.Manifest).Hash);
    }

    private static CampaignSetupSnapshotV7 CreateSetup(ContentPackV7Artifact artifact,
        ContentCombatScenario scenario, LandSide holder = LandSide.Axis) => CampaignSetupSnapshotV7.Create(
        "rules-lab.combat.close-assault.v1", 1, new PredeterminedInitiative(holder),
        new CampaignOpeningPreamblePolicy(1, CampaignOpeningPreambleKind.NoOpeningNavalConvoyObligations,
            [new RuleReference("sandtable-rules-lab", "opening-preamble.no-naval-convoy-obligations.v1")]),
        new CampaignWeatherPolicy(1, CampaignWeatherPolicyKind.NoImmediateWeatherEffectSubjects,
            [new RuleReference("sandtable-rules-lab", "weather.no-immediate-effect-subjects.v1")]),
        new CampaignStageEntryPolicy(1, 1, 1, StageEntryObligationKind.ExplicitNone,
            StageEntryObligationKind.ExplicitNone, StageEntryObligationKind.ExplicitNone,
            StageEntryObligationKind.ExplicitNone,
            [new RuleReference("sandtable-rules-lab", "stage-entry.no-obligations.v1")]),
        new CampaignCombatInitializationPolicy(1, 1, 1, CapabilityPointAmount.Zero, 0,
            CampaignElementReserveStatus.None, new ContentOrigin(ContentOriginKind.Synthetic,
                [new RuleReference("sandtable-rules-lab", "combat.close-assault-positive.v1:initial-ledger")])),
        artifact, scenario,
        [new RuleReference("sandtable-rules-lab", "combat.close-assault-positive.v1:setup")]);

    private static byte[] SetupGolden()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Campaigns", "Fixtures", "combat-creation-ledger-v1.json")));
        return Encoding.UTF8.GetBytes(fixture.RootElement.GetProperty("setup").GetProperty("canonicalUtf8")
            .GetString()!);
    }

    private static byte[] ConfigGolden()
    {
        using var fixture = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,
            "Rules", "Fixtures", "combat-rules-inputs-v1.json")));
        return Encoding.UTF8.GetBytes(fixture.RootElement.GetProperty("goldens").EnumerateArray()
            .Single(value => value.GetProperty("kind").GetString() == "Config")
            .GetProperty("canonicalUtf8").GetString()!);
    }
}
