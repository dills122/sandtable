using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignScheduledWorld6Created(string CampaignId, long StateVersion, string RulesetHash,
    string SetupId, string SetupHash, string ContentPackId, string ContentHash, string ScenarioId,
    ulong Seed, CampaignWorldSnapshotV6 World, RandomStreamState RandomState, LandSequencePosition Position);

// Synthetic successor creation cut; not yet routed by CampaignCreationExecution.
internal static class CampaignScheduledWorld6Creation
{
    public static CampaignScheduledWorld6Created Create(CampaignCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ContentContractGuards.RequireStableId(request.CampaignId, nameof(request));
        var artifact = Cna1979SyntheticContentCatalog.ArtifactV6;
        if (request.ContractVersion != CampaignCreationRequest.CurrentContractVersion
            || !Cna1979Ruleset.IsCanonicalHash(request.RulesetHash)
            || request.SetupId != CampaignScheduledWorld6Phase.SetupId
            || request.SetupHash != CampaignScheduledWorld6Phase.SetupHash
            || request.ContentPackId != artifact.Identity.PackId || request.ContentHash != artifact.Identity.Hash
            || request.ScenarioId != "breakdown-truck-lab")
            throw new ArgumentException("Unsupported scheduled World6 creation request.", nameof(request));
        return new CampaignScheduledWorld6Created(request.CampaignId, 1, request.RulesetHash,
            request.SetupId, request.SetupHash, request.ContentPackId, request.ContentHash, request.ScenarioId,
            request.Seed, CampaignScheduledWorld6.CreateInitial(), SandtableRandom.Create(request.Seed),
            Cna1979LandSequenceV4.CreateTurn(1)[0]);
    }

    public static CampaignScheduledWorld6Created Replay(CampaignCreationRequest request, CampaignScheduledWorld6Created recorded)
    {
        ArgumentNullException.ThrowIfNull(recorded);
        var expected = Create(request);
        if (expected != recorded) throw new InvalidOperationException("Invalid scheduled World6 creation evidence.");
        return expected;
    }
}
