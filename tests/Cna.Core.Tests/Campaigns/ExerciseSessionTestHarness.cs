using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

// Shared fixture helper for the family of CampaignNativeStageEntry*/
// CampaignNativeSecondBreakdown*/CampaignExercisesTests test files that each
// previously carried an identical private Start(CampaignCreationRequest)/
// CreateRequest() pair (maintainability-refactor-direction.md Slice 2b,
// Finding C minor). CreateRequest takes the campaign id explicitly because
// at least one caller (CampaignExercisesTests) asserts on the exact literal
// it passed in, so the per-test id is preserved rather than collapsed.
internal static class ExerciseSessionTestHarness
{
    public static ExerciseSession Start(CampaignCreationRequest request)
    {
        var result = CampaignExercises.Begin(request);
        Assert.True(result.IsStarted);
        return result.Session!;
    }

    public static CampaignCreationRequest CreateRequest(string campaignId)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(
            Cna1979BreakdownSetupCatalog.Definitions.Single(value => value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
        return new CampaignCreationRequest(1, campaignId, Cna1979Ruleset.Manifest.Hash,
            12345, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId,
            setup.Content.Pack.Hash, setup.Content.ScenarioId);
    }
}
