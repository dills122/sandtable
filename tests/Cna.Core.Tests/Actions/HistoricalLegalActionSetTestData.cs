using Cna.Core.Actions;

namespace Cna.Core.Tests.Actions;

internal static class HistoricalLegalActionSetTestData
{
    public static CampaignLegalActionSet Create(string campaignId, long stateVersion, string rulesetHash,
        string positionId, CampaignActionAudience audience, IReadOnlyList<CampaignActionCandidate> candidates) =>
        new(campaignId, stateVersion, rulesetHash, positionId, audience, candidates, CampaignLegalActionSet.HistoricalPolicyIdV2);
}
