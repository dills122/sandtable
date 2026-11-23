using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

// MTS-TASK-005/006 (2026-11-23): proves this stage's own Breakdown guard
// fires correctly via CampaignNativeSecondBreakdownV46.CreateForStageEntry.
//
// Different depth than CampaignNativeStageEntryReserveTests/
// CampaignNativeStageEntryMovementTests: those files test their native
// classes' Create/Source methods directly with synthetic predecessors,
// because V43/V44/V45 take their predecessor as an explicit, type-matched
// parameter. CampaignNativeSecondBreakdownV46 (both Create and the new
// CreateForStageEntry) instead takes the whole ExerciseSession and reads
// named fields off it directly -- so testing it at all requires a real
// ExerciseSession instance, not just constructed event objects.
//
// Reaching a session with CurrentStageEntryMovementCompletion actually
// populated needs the full six-event preamble chain plus this stage's own
// Reserve/Move/MovementCompletion driven through the real ExerciseSession
// With* chain -- which itself needs a real driven campaign to reach
// legitimately, the same pre-existing gap CampaignNativeStageEntryMovementTests
// and the MTS-TASK-002 wiring doc both document ("the ExerciseSession/
// CampaignExercises wiring is not integration tested"). That gap is not
// this slice's to close. What's genuinely testable at today's depth is the
// guard itself: CreateForStageEntry must reject a real, legitimately
// constructed session (a fresh campaign genesis) that has not reached a
// completed Stage Entry Movement, which is exactly what a caller would hit
// if the wiring above it had a bug and invoked this too early.
public sealed class CampaignNativeStageEntryBreakdownTests
{
    [Fact]
    public void StageEntryBreakdownRejectsASessionWithoutACompletedStageEntryMovement()
    {
        var session = Start(CreateRequest());

        var error = Assert.Throws<InvalidOperationException>(
            () => CampaignNativeSecondBreakdownV46.CreateForStageEntry(session));

        Assert.Equal("Stage Entry Movement has not completed.", error.Message);
    }

    [Fact]
    public void StageEntryBreakdownCompletionRejectsTheSameSession()
    {
        var session = Start(CreateRequest());

        var result = CampaignExercises.CompleteStageEntryBreakdown(session);

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
        return new CampaignCreationRequest(1, "campaign-stage-entry-breakdown-reject", Cna1979Ruleset.Manifest.Hash,
            12345, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId,
            setup.Content.Pack.Hash, setup.Content.ScenarioId);
    }
}
