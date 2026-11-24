using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using static Cna.Core.Tests.Campaigns.ExerciseSessionTestHarness;

namespace Cna.Core.Tests.Campaigns;

// MTS-TASK-005/006 (2026-11-23): proves this stage's own Combat Decision
// guard fires correctly via ExerciseSession.WithStageEntryCombatSelection.
//
// Unlike CampaignNativeStageEntryMovementTests, CampaignNativeSecondCombatDecisionStateV47.Select
// is already fully parameterized (world/position/turnFacts/randomState/
// context/contacts/side/actingElementId/actionId, confirmed by reading it
// directly) -- it needed no CreateForStageEntry sibling the way V46
// (Breakdown) did. A genuine success-path test would still need a real
// (World, Position) pair at the Combat/Barrage catalog step, which this
// slice cannot derive safely: QueryStageEntryCombatWorldAndPosition's own
// position comes from a completed CampaignNativeSecondBreakdownV46's
// NextPositionId, and that native class (like its sibling CreateForStageEntry)
// only takes a real ExerciseSession, not typed parameters -- so there is no
// way to reach that position from pure native-class calls the way
// CampaignNativeStageEntryMovementTests reaches its own success path.
// Hand-deriving the catalog position independently, without the actual
// Breakdown event's own internal computation to check it against, would
// risk testing an invented position rather than the real one. Same honest
// depth as CampaignNativeStageEntryBreakdownTests instead: what's genuinely
// testable without that missing piece is the ExerciseSession-layer guard
// itself, on a real, legitimately-constructed session (a fresh campaign
// genesis) that has not reached a completed Stage Entry Breakdown.
public sealed class CampaignNativeStageEntryCombatSelectionTests
{
    [Fact]
    public void StageEntryCombatSelectionRejectsASessionWithoutACompletedStageEntryBreakdown()
    {
        var session = Start(CreateRequest("campaign-stage-entry-combat-selection-reject"));
        var selected = SyntheticDecision(session);

        var error = Assert.Throws<InvalidOperationException>(
            () => session.WithStageEntryCombatSelection(selected, session.CurrentContacts));

        Assert.Equal("Stage Entry Combat selection must advance its retained cut once.", error.Message);
    }

    // The guard's CurrentStageEntryBreakdown-is-null check short-circuits
    // before any of selected's own fields are examined, so this object only
    // needs to construct successfully, not describe a legal decision --
    // mirrors how CampaignNativeStageEntryBreakdownTests needs no real
    // predecessor chain to prove its own guard either.
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
