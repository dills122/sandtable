using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

// Pre-refactor-gap regression test (2026-11-24 maintainability direction doc,
// Section 7): CreateForStageEntry and CreateForStageEntrySecondSide already
// have dedicated coverage (CampaignNativeStageEntryBreakdownTests,
// CampaignNativeStageEntrySecondBreakdownTests), but plain Create had no
// dedicated unit test file -- it was only reached indirectly via
// CompleteSecondBreakdown/CampaignExercises integration tests. This proves
// all 3 entry points reject their own missing-predecessor case with the
// right error message, in one parameterized test, so a future collapse of
// their shared control flow (CreateCore + Slot) can't silently swap which
// entry point reads which ExerciseSession field.
public sealed class CampaignNativeSecondBreakdownTests
{
    [Theory]
    [InlineData("Create", "Second Movement has not completed.")]
    [InlineData("CreateForStageEntry", "Stage Entry Movement has not completed.")]
    [InlineData("CreateForStageEntrySecondSide", "Stage Entry second-side Movement has not completed.")]
    public void BreakdownRejectsASessionWithoutItsOwnCompletedMovement(string entryPoint, string expectedMessage)
    {
        var session = Start(CreateRequest());

        var error = Assert.Throws<InvalidOperationException>(() => Invoke(entryPoint, session));

        Assert.Equal(expectedMessage, error.Message);
    }

    private static CampaignNativeSecondBreakdownV46 Invoke(string entryPoint, ExerciseSession session) => entryPoint switch
    {
        "Create" => CampaignNativeSecondBreakdownV46.Create(session),
        "CreateForStageEntry" => CampaignNativeSecondBreakdownV46.CreateForStageEntry(session),
        "CreateForStageEntrySecondSide" => CampaignNativeSecondBreakdownV46.CreateForStageEntrySecondSide(session),
        _ => throw new ArgumentOutOfRangeException(nameof(entryPoint)),
    };

    private static ExerciseSession Start(CampaignCreationRequest request)
    {
        var result = CampaignExercises.Begin(request);
        Assert.True(result.IsStarted);
        return result.Session!;
    }

    private static CampaignCreationRequest CreateRequest()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(
            Cna1979BreakdownSetupCatalog.Definitions.Single(value => value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
        return new CampaignCreationRequest(1, "campaign-second-breakdown-entry-points-reject", Cna1979Ruleset.Manifest.Hash,
            12345, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId,
            setup.Content.Pack.Hash, setup.Content.ScenarioId);
    }
}
