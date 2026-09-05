using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

internal static class CampaignCurrentRequestTestData
{
    public static CampaignCreationRequest Create(
        CampaignSetupDefinition setup,
        string campaignId,
        ulong seed)
    {
        var artifact = Cna1979SyntheticContentCatalog.ArtifactV5;
        var successorSetup = CampaignSetupSnapshotV5.FromPredecessor(
            CampaignSetupSnapshot.FromDefinition(setup),
            new CampaignContentV5Selection(artifact.Identity, setup.Content.ScenarioId));
        return new CampaignCreationRequest(
            CampaignCreationRequest.CurrentContractVersion,
            campaignId,
            Cna1979Ruleset.Manifest.Hash,
            seed,
            setup.SetupId,
            successorSetup.SetupHash,
            artifact.Identity.PackId,
            artifact.Identity.Hash,
            setup.Content.ScenarioId);
    }
}
