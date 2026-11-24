using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using static Cna.Core.Tests.Campaigns.ExerciseSessionTestHarness;

namespace Cna.Core.Tests.Campaigns;

// MTS-TASK-006 (2026-11-23): proves this stage's own SECOND acting side's
// Combat Decision guard fires correctly via
// ExerciseSession.WithStageEntrySecondCombatSelection. Same depth and same
// reasoning as CampaignNativeStageEntryCombatSelectionTests --
// CampaignNativeSecondCombatDecisionStateV47.Select is already fully
// parameterized (confirmed when MTS-TASK-005's own Combat Decision slice
// built it; no reason to expect a deviation here), so it needed no new
// entry point, but a genuine success-path test still needs a real
// (World, Position) pair this slice cannot derive without a real driven
// session, for the same reason that file's own doc explains.
public sealed class CampaignNativeStageEntrySecondCombatSelectionTests
{
    [Fact]
    public void StageEntrySecondCombatSelectionRejectsASessionWithoutACompletedSecondSideBreakdown()
    {
        var session = Start(CreateRequest("campaign-stage-entry-second-combat-selection-reject"));
        var selected = SyntheticDecision(session);

        var error = Assert.Throws<InvalidOperationException>(
            () => session.WithStageEntrySecondCombatSelection(selected, session.CurrentContacts));

        Assert.Equal("Stage Entry second-side Combat selection must advance its retained cut once.", error.Message);
    }

    // The guard's CurrentStageEntrySecondBreakdown-is-null check
    // short-circuits before any of selected's own fields are examined, so
    // this object only needs to construct successfully -- same reasoning
    // as CampaignNativeStageEntryCombatSelectionTests' own SyntheticDecision
    // helper.
    private static CampaignNativeSecondCombatDecisionStateV47 SyntheticDecision(ExerciseSession session)
    {
        var artifact = Cna.Core.Tests.Content.BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var world = CampaignWorldV6Factory.CreateInitial(artifact, scenario);
        var decision = new CampaignNativeSecondCombatDecisionV47(
            CampaignNativeSecondCombatDecisionV47.CurrentContractVersion, session.CurrentSnapshot.CampaignId,
            session.CurrentContacts.AuthorityVersion, "placeholder-position", "placeholder-next-position",
            "finish-combat-without-attack", LandSide.Axis, "placeholder-element", null, null, null, "placeholder-action");
        return new CampaignNativeSecondCombatDecisionStateV47(session.CurrentSnapshot, world,
            session.CurrentSnapshot.RandomState, decision);
    }

}
