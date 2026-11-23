using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

// MTS-TASK-006 (2026-11-23): proves this stage's own SECOND acting side's
// Breakdown guard fires correctly via
// CampaignNativeSecondBreakdownV46.CreateForStageEntrySecondSide -- a third
// entry point, needed for the same reason CreateForStageEntry needed a
// second one (confirmed by reading Create/CreateForStageEntry directly
// before writing this slice): each occurrence reads its own four named
// ExerciseSession fields rather than taking its predecessor by type.
//
// Same depth and same limitation as CampaignNativeStageEntryBreakdownTests:
// reaching a session with CurrentStageEntrySecondMovementCompletion
// genuinely populated needs this stage's own second-side Reserve/Move/
// MovementCompletion driven through the real ExerciseSession With* chain,
// which needs the Host/HTTP wiring that does not exist yet. What's
// genuinely testable at today's depth is the guard itself: CreateForStageEntrySecondSide
// must reject a real, legitimately constructed session (a fresh campaign
// genesis) that has not reached a completed second-side Movement
// Completion.
public sealed class CampaignNativeStageEntrySecondBreakdownTests
{
    [Fact]
    public void StageEntrySecondBreakdownRejectsASessionWithoutACompletedSecondSideMovement()
    {
        var session = Start(CreateRequest());

        var error = Assert.Throws<InvalidOperationException>(
            () => CampaignNativeSecondBreakdownV46.CreateForStageEntrySecondSide(session));

        Assert.Equal("Stage Entry second-side Movement has not completed.", error.Message);
    }

    [Fact]
    public void StageEntrySecondBreakdownCompletionRejectsTheSameSession()
    {
        var session = Start(CreateRequest());

        var result = CampaignExercises.CompleteStageEntrySecondBreakdown(session);

        Assert.False(result.IsAccepted);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, result.RejectionReason);
    }

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
        return new CampaignCreationRequest(1, "campaign-stage-entry-second-breakdown-reject", Cna1979Ruleset.Manifest.Hash,
            12345, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId,
            setup.Content.Pack.Hash, setup.Content.ScenarioId);
    }
}
