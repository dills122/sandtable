using System.Security.Cryptography;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

// MTS-TASK-006 (2026-11-23): proves this stage's own SECOND acting side's
// Movement + Movement Completion -- CampaignNativeSecondMoveV44/
// CampaignNativeSecondMovementCompletedV45 reused unchanged for a SIXTH
// occurrence (Stage 1's second side, this stage's first acting side, and
// now this), entered from this stage's own completed second-side Reserve
// Designation. Same "dormant, contract-level" testing depth as
// CampaignNativeStageEntryMovementTests: this file proves the
// zero-elements-moved Movement Completion path genuinely completes for the
// SecondSide role, plus that the predecessor switch correctly rejects the
// opposite side -- the same depth and the same limitation that file already
// documents.
public sealed class CampaignNativeStageEntrySecondMovementTests
{
    private static string Hash(byte theme) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData([theme]));

    // This stage's own FIRST acting side's completed terminal Patrol pass
    // -- the direct predecessor CampaignExercises.QueryStageEntrySecondReserveSource
    // itself reads from CurrentStageEntryAssault.PostCombatPasses[^1] in
    // production. Unlike CampaignNativeStageEntryMovementTests' own
    // SecondSidePatrolComplete (Stage 1, SecondActingSide, used to reach
    // the SIX-EVENT PREAMBLE), this is OperationStage 2's FirstActingSide
    // Patrol, used to reach this stage's own second-side Reserve DIRECTLY
    // -- no preamble in between, confirmed by this slice's own design
    // investigation (see docs/work/plans/2026-11-23-mts-task-006-stage-entry-second-reserve.md).
    private static CampaignNativePostCombatPassV42 FirstActingSidePatrolComplete(
        string campaignId, long stateVersion, CampaignWorldSnapshotV6 world)
    {
        var catalog = Cna1979LandSequenceV4.CreateTurn(1);
        var patrol = catalog.Single(value => value.PhaseId == LandPhaseIds.Patrol
            && value.ActorRole == LandActorRole.FirstActingSide && value.OperationStage == 2);
        var next = Cna1979LandSequenceV4.GetNext(patrol);
        return new CampaignNativePostCombatPassV42(CampaignNativePostCombatPassV42.CurrentContractVersion,
            campaignId, stateVersion, Hash(1), LandSide.Axis, patrol.PositionId, next.PositionId,
            CampaignNativeCombatPaidWorldV22.Hash(world));
    }

    // Completes this stage's own SECOND acting side's Reserve Designation,
    // entered directly from this stage's own first-acting-side Patrol
    // completion -- the direct analogue of
    // CampaignNativeStageEntryMovementTests.CompletedStageEntryReserve,
    // substituted to this stage's second-side entry point and role
    // (order.SecondSide, not FirstSide) rather than the preamble's.
    private static (CampaignNativeSecondReserveV43 Reserve, CampaignOperationStageOrder Order)
        CompletedStageEntrySecondReserve(string campaignId, CampaignWorldSnapshotV6 world, CampaignContentContext context)
    {
        var predecessor = FirstActingSidePatrolComplete(campaignId, 100, world);
        var order = new CampaignOperationStageOrder(CampaignOperationStageOrder.CurrentContractVersion,
            1, 2, LandSide.Axis, LandSide.Commonwealth);
        IReadOnlyList<CampaignOperationStageOrder> orders = [order];
        var (reserve, _) = CampaignNativeSecondReserveV43.Create(
            predecessor, world, context, orders, order.SecondSide, elementId: null, 1, predecessor.StateVersion);
        return (reserve, order);
    }

    [Fact]
    public void StageEntrySecondMovementCompletesForTheNewStagesSecondActingSideWithNoElementsMoved()
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        var world = CampaignWorldV6Factory.CreateInitial(artifact, scenario);

        var (reserve, order) = CompletedStageEntrySecondReserve("test-campaign-stage-entry-second-movement", world, context);
        Assert.True(reserve.Completed);
        IReadOnlyList<CampaignOperationStageOrder> orders = [order];

        var completed = CampaignNativeSecondMovementCompletedV45.Create(
            reserve, world, context, orders, order.SecondSide, 1, reserve.StateVersion);

        Assert.Equal(order.SecondSide, completed.Side);
        Assert.Equal(2, Cna1979LandSequenceV4.CreateTurn(1)
            .Single(value => value.PositionId == completed.PositionId).OperationStage);
        var next = Cna1979LandSequenceV4.CreateTurn(1)
            .Single(value => value.PositionId == completed.NextPositionId);
        Assert.Equal(LandSegmentIds.BreakdownDetermination, next.SegmentId);

        var bytes = completed.SerializeCanonical();
        Assert.Equal(completed, CampaignNativeSecondMovementCompletedV45.Deserialize(bytes));
    }

    [Fact]
    public void StageEntrySecondMovementRejectsTheOppositeStagesRole()
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        var world = CampaignWorldV6Factory.CreateInitial(artifact, scenario);

        var (reserve, order) = CompletedStageEntrySecondReserve("test-campaign-stage-entry-second-movement-reject", world, context);
        IReadOnlyList<CampaignOperationStageOrder> orders = [order];

        var error = Assert.Throws<InvalidOperationException>(() => CampaignNativeSecondMovementCompletedV45.Create(
            reserve, world, context, orders, order.FirstSide, 1, reserve.StateVersion));

        Assert.Equal("Second Movement does not bind the owner and World.", error.Message);
    }
}
