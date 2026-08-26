using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

internal static class CampaignReserveActionTestData
{
    public static StageEntryCampaignEvidence ExecuteToReserve(
        int setupIndex,
        InitiativeOrderChoice choice) =>
        StageEntryCampaignTestData.Execute(
            Cna1979SetupCatalog.Definitions[setupIndex],
            setupIndex == 0 ? 12345UL : 7UL,
            choice);

    public static CampaignAuthorityHandle ReachReserve(
        int setupIndex,
        InitiativeOrderChoice choice)
    {
        var evidence = ExecuteToReserve(setupIndex, choice);
        return new CampaignAuthorityHandle(evidence.Snapshot, evidence.Context);
    }

    public static CampaignLegalActionSet Query(
        CampaignAuthorityHandle handle,
        CampaignActionAudience audience)
    {
        var result = CampaignLegalActions.Query(handle, audience);
        Assert.True(result.IsSuccessful);
        return result.ActionSet!;
    }

    public static CampaignActionAudience ToAudience(LandSide side) => side switch
    {
        LandSide.Axis => CampaignActionAudience.Axis,
        LandSide.Commonwealth => CampaignActionAudience.Commonwealth,
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };

    public static CampaignActionSubmission Bind(
        CampaignLegalActionSet set,
        CampaignActionCandidate candidate) => new(
            CampaignActionSubmission.CurrentContractVersion,
            set.CampaignId,
            set.StateVersion,
            set.PositionId,
            set.Audience,
            candidate.ActionId);
}
