using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using static Cna.Core.Tests.Campaigns.ExerciseSessionTestHarness;

namespace Cna.Core.Tests.Campaigns;

// MTS-TASK-006 (2026-11-23): proves this stage's own SECOND acting side's
// Reserve Designation guard fires correctly via
// CampaignExercises.QueryStageEntrySecondReserveSource/SubmitStageEntrySecondReserve.
//
// Same depth as CampaignNativeStageEntryBreakdownTests/
// CampaignNativeStageEntryCombatSelectionTests/
// CampaignNativeStageEntryCombatAssaultTests: reaching a session with
// CurrentStageEntryAssault's own PostCombatPasses populated needs the full
// six-event preamble chain plus this stage's own first-acting-side
// Reserve/Move/Breakdown/CombatDecision/Assault chain driven through the
// real ExerciseSession With* chain, which itself needs a real driven
// campaign and the Host/HTTP wiring that does not exist yet (same
// pre-existing gap every prior Stage Entry slice has documented). What's
// genuinely testable at today's depth is the guard itself: this stage's own
// second-acting-side Reserve must reject a real, legitimately constructed
// session (a fresh campaign genesis) that has not reached a completed
// first-acting-side chain, which is exactly what a caller would hit if the
// wiring above it had a bug and invoked this too early.
public sealed class CampaignNativeStageEntrySecondReserveTests
{
    [Fact]
    public void StageEntrySecondReserveSourceRejectsASessionWithoutACompletedFirstActingSideChain()
    {
        var session = Start(CreateRequest("campaign-stage-entry-second-reserve-reject"));

        var error = Assert.Throws<InvalidOperationException>(
            () => CampaignExercises.QueryStageEntrySecondReserveSource(session));

        Assert.Equal("Stage Entry second-side Reserve requires this stage's completed first-acting-side chain.",
            error.Message);
    }

    [Fact]
    public void StageEntrySecondReserveSubmissionRejectsTheSameSession()
    {
        var session = Start(CreateRequest("campaign-stage-entry-second-reserve-reject"));

        var result = CampaignExercises.SubmitStageEntrySecondReserve(
            session, CampaignActionAudience.Axis, "action-stage-entry-second-reserve", elementId: null);

        Assert.False(result.IsAccepted);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, result.RejectionReason);
    }
}
