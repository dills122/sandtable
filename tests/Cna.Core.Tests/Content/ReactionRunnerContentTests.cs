using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Content;

public sealed class ReactionRunnerContentTests
{
    [Theory]
    [InlineData("adjacent")]
    [InlineData("positive-zoc")]
    [InlineData("low-defense")]
    [InlineData("remote-zoc")]
    [InlineData("headquarters")]
    [InlineData("noncombat")]
    [InlineData("recurrence")]
    public void CheckedReactionSetupRetainsStrictHistoricalContentAndRejectsCurrentCreation(string variant)
    {
        Assert.True(Cna1979SetupCatalog.TryGet($"rules-lab.reaction.{variant}", out var definition));
        var artifact = Cna1979SyntheticContentResolver.Instance.ResolveV5(
            definition!.Content.Pack.PackId,
            Cna1979ReactionContentCatalog.Artifacts.Single(value =>
                value.Identity.PackId == definition.Content.Pack.PackId).Identity.Hash).Artifact!;
        Assert.Equal(artifact.GetCanonicalBytes(), ContentPackV5Artifact.Create(
            ContentPackV5Serializer.Deserialize(artifact.GetCanonicalBytes()).Definition!).GetCanonicalBytes());
        var setup = CampaignSetupSnapshotV5.FromPredecessor(
            CampaignSetupSnapshot.FromDefinition(definition),
            new CampaignContentV5Selection(artifact.Identity, definition.Content.ScenarioId));
        var request = new CampaignCreationRequest(CampaignCreationRequest.CurrentContractVersion,
            "reaction-content-check", Cna.Core.Rules.Cna1979Ruleset.HistoricalManifestV8.Hash, 123,
            definition.SetupId, setup.SetupHash,
            artifact.Identity.PackId, artifact.Identity.Hash, definition.Content.ScenarioId);
        Assert.False(CampaignAuthority.Create(request).IsCreated);
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var created = CampaignCreationV9Factory.Create(request.CampaignId, request.RulesetHash,
            CampaignSetupSnapshot.FromDefinition(definition), artifact, scenario, SandtableRandom.Create(request.Seed),
            Cna1979LandSequence.CreateTurn(scenario.Start.GameTurn)[0]);
        Assert.Equal(10, CampaignV10Projector.ApplyCreation(created, artifact, scenario).ContractVersion);
        var controlled = CampaignElementMovedV2Factory.DeriveControlledLocationIds(
            CampaignWorldV5Factory.CreateInitial(artifact, scenario), artifact, scenario,
            Cna.Core.Rules.LandSide.Commonwealth);
        string[] expected = variant switch
        {
            "positive-zoc" => ["center", "north-east", "south-east"],
            "remote-zoc" => ["remote-neighbor"],
            _ => [],
        };
        Assert.Equal(expected, controlled);

    }
}
