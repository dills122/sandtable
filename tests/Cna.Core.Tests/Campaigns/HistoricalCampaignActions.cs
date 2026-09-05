using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

// Historical evidence driver; never called by production current entrypoints.
internal static class HistoricalCampaignActions
{
    public static CampaignAuthorityHandle Create(CampaignSetupDefinition setup, string campaignId, ulong seed)
    {
        var decision = CampaignTestHarness.Decide(null, CampaignTestHarness.Create(campaignId,
            Cna1979Ruleset.HistoricalManifestV8.Hash, seed, setup.SetupId, setup.Hash));
        Assert.True(decision.IsAccepted);
        var snapshot = CampaignTestHarness.Replay(decision.Events);
        return new CampaignAuthorityHandle(snapshot, CampaignTestHarness.ContextFor(snapshot));
    }

    public static CampaignLegalActionQueryResult Query(CampaignAuthorityHandle handle,
        CampaignActionAudience audience) => CampaignLegalActions.QueryLegacy(handle.Snapshot, handle.Context, audience);

    public static CampaignActionSubmissionResult Submit(CampaignAuthorityHandle handle,
        CampaignActionSubmission submission)
    {
        var execution = CampaignActionExecution.Execute(handle.Snapshot, handle.Context, submission);
        return execution.IsAccepted
            ? CampaignActionSubmissionResult.Accepted(
                new CampaignAuthorityHandle(execution.SuccessorSnapshot!, handle.Context), execution.Receipt!)
            : CampaignActionSubmissionResult.Rejected(execution.RejectionReason);
    }
}
