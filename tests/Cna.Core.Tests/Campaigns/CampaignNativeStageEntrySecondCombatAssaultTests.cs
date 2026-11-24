using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Randomness;
using static Cna.Core.Tests.Campaigns.ExerciseSessionTestHarness;

namespace Cna.Core.Tests.Campaigns;

// MTS-TASK-006 (2026-11-23): proves this stage's own SECOND acting side's
// Combat No-Attack completion guard fires correctly via
// ExerciseSession.WithStageEntrySecondNoAttack, same depth and same reason
// as CampaignNativeStageEntryCombatAssaultTests -- a genuine success-path
// test still needs a real (World, Position) pair this slice cannot derive
// without a real driven session.
//
// WithStageEntrySecondAssaultEvent itself, the CurrentRepeatAssault
// computed property's new four-way resolution, and WithRepeatAssaultEvent's
// new four-way dispatch are not independently unit-testable at this depth
// either -- they operate on a real CampaignNativeRepeatAssaultRuntime,
// which only accepts a real ExerciseSession, not typed parameters. What
// proves those changes correct here is the full existing regression suite
// passing unchanged: CurrentRepeatAssault and WithRepeatAssaultEvent are
// read by every one of the Tier 1/Tier 2 call sites today, so any error in
// the new branch would show up as those same tests failing, not just as a
// missing new test.
public sealed class CampaignNativeStageEntrySecondCombatAssaultTests
{
    [Fact]
    public void StageEntrySecondNoAttackRejectsASessionWithoutACompletedSecondSideCombatDecision()
    {
        var session = Start(CreateRequest("campaign-stage-entry-second-combat-assault-reject"));
        var completion = SyntheticCompletion(session);

        var error = Assert.Throws<InvalidOperationException>(
            () => session.WithStageEntrySecondNoAttack(completion, session.CurrentContacts));

        Assert.Equal("Stage Entry second-side no-attack completion must advance its retained cut once.", error.Message);
    }

    // The guard's CurrentStageEntrySecondCombatDecision-is-null check
    // short-circuits before any of completion's own fields are examined --
    // same reasoning as CampaignNativeStageEntryCombatAssaultTests' own
    // SyntheticCompletion helper.
    private static CampaignNativeCombatNoAttackCompletionV13 SyntheticCompletion(ExerciseSession session) =>
        new(CampaignNativeCombatNoAttackCompletionV13.CurrentContractVersion, session.CurrentSnapshot.CampaignId,
            session.CurrentContacts.AuthorityVersion, "placeholder-action", []);
}
