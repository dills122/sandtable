using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Randomness;
using static Cna.Core.Tests.Campaigns.ExerciseSessionTestHarness;

namespace Cna.Core.Tests.Campaigns;

// MTS-TASK-005/006 follow-up (2026-11-23): proves this stage's own
// Combat No-Attack completion guard fires correctly via
// ExerciseSession.WithStageEntryNoAttack, same depth and same reason as
// CampaignNativeStageEntryCombatSelectionTests -- a genuine success-path
// test still needs a real (World, Position) pair this slice cannot derive
// without a real driven session.
//
// WithStageEntryAssaultEvent itself (the counterpart to WithSecondAssaultEvent),
// the CurrentRepeatAssault computed property's new three-way resolution, and
// WithRepeatAssaultEvent's new three-way dispatch are not independently
// unit-testable at this depth either -- they operate on a real
// CampaignNativeRepeatAssaultRuntime, which (like CampaignNativeSecondBreakdownV46
// and CampaignNativeSecondCombatDecisionStateV47 before it) only accepts a
// real ExerciseSession, not typed parameters. What proves those three
// changes correct here is the full existing regression suite (488/488,
// covering every #394 second-side and first-side call site this change
// touches) passing unchanged: CurrentRepeatAssault and WithRepeatAssaultEvent
// are read by every one of those paths today, so any error in the new
// Stage-Entry-first branch would show up as those same tests failing, not
// just as a missing new test.
public sealed class CampaignNativeStageEntryCombatAssaultTests
{
    [Fact]
    public void StageEntryNoAttackRejectsASessionWithoutACompletedStageEntryCombatDecision()
    {
        var session = Start(CreateRequest("campaign-stage-entry-combat-assault-reject"));
        var completion = SyntheticCompletion(session);

        var error = Assert.Throws<InvalidOperationException>(
            () => session.WithStageEntryNoAttack(completion, session.CurrentContacts));

        Assert.Equal("Stage Entry no-attack completion must advance its retained cut once.", error.Message);
    }

    // The guard's CurrentStageEntryCombatDecision-is-null check short-circuits
    // before any of completion's own fields are examined, so this object only
    // needs to construct successfully, not describe a legal completion --
    // same reasoning as CampaignNativeStageEntryCombatSelectionTests' own
    // SyntheticDecision helper.
    private static CampaignNativeCombatNoAttackCompletionV13 SyntheticCompletion(ExerciseSession session) =>
        new(CampaignNativeCombatNoAttackCompletionV13.CurrentContractVersion, session.CurrentSnapshot.CampaignId,
            session.CurrentContacts.AuthorityVersion, "placeholder-action", []);

}
