using System.Security.Cryptography;
using Cna.Core.Campaigns;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

// MTS-TASK-005/006 (2026-11-22): proves this stage's own Movement +
// Movement Completion -- CampaignNativeSecondMoveV44/
// CampaignNativeSecondMovementCompletedV45 reused unchanged, entered from
// this stage's own completed Reserve Designation
// (CampaignNativeStageEntryReserveTests). Same "dormant, contract-level"
// testing depth as that file and CampaignNativeStageEntryPreambleTests: no
// real campaign in this suite can drive a full Combat cycle to a legal
// element move yet (that needs a World whose element positions actually
// match this stage's Movement catalog step, which only a real driven
// campaign produces), so this file proves the zero-elements-moved
// Movement Completion path genuinely completes, plus that the predecessor
// switch correctly rejects the opposite side -- the same depth and the
// same limitation the Reserve file already documents.
public sealed class CampaignNativeStageEntryMovementTests
{
    private static string Hash(byte theme) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData([theme]));

    private static RandomStreamState GenesisRandomState() =>
        new(SandtableRandom.ContractVersion, SandtableRandom.AlgorithmId, 4242, 0);

    // Same synthetic predecessor shape as CampaignNativeStageEntryReserveTests.
    private static CampaignNativePostCombatPassV42 SecondSidePatrolComplete(
        string campaignId, long stateVersion, CampaignWorldSnapshotV6 world)
    {
        var catalog = Cna1979LandSequenceV4.CreateTurn(1);
        var patrol = catalog.Single(value => value.PhaseId == LandPhaseIds.Patrol
            && value.ActorRole == LandActorRole.SecondActingSide && value.OperationStage == 1);
        var next = Cna1979LandSequenceV4.GetNext(patrol);
        return new CampaignNativePostCombatPassV42(CampaignNativePostCombatPassV42.CurrentContractVersion,
            campaignId, stateVersion, Hash(1), LandSide.Commonwealth, patrol.PositionId, next.PositionId,
            CampaignNativeCombatPaidWorldV22.Hash(world));
    }

    // Carries the six-event preamble chain through to a completed Reserve
    // Designation for the new stage's first acting side -- identical setup
    // to CampaignNativeStageEntryReserveTests.StageEntryReserveCompletesForTheNewStagesFirstActingSide.
    private static (CampaignNativeSecondReserveV43 Reserve, CampaignOperationStageOrder Order)
        CompletedStageEntryReserve(string campaignId, CampaignWorldSnapshotV6 world, CampaignContentContext context)
    {
        var predecessor = SecondSidePatrolComplete(campaignId, 100, world);
        var declared = CampaignNativeInitiativeDeclarationV49.Create(
            predecessor, LandSide.Axis, actFirst: true, 1, predecessor.StateVersion);
        var weather = CampaignNativeWeatherDeterminationV50.Create(declared, GenesisRandomState(), 1, declared.StateVersion);
        var organization = CampaignNativeNoObligationOrganizationV51.Create(weather, weather.StateVersion);
        var convoyArrival = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(
            organization, context, organization.StateVersion);
        var fleetAssignment = CampaignNativeNoObligationFleetAssignmentV53.Create(
            convoyArrival, convoyArrival.StateVersion);
        var fleetRepair = CampaignNativeNoObligationFleetRepairV54.Create(fleetAssignment, fleetAssignment.StateVersion);
        var order = fleetRepair.ToOrder();
        IReadOnlyList<CampaignOperationStageOrder> orders = [order];
        var (reserve, _) = CampaignNativeSecondReserveV43.Create(
            fleetRepair, world, context, orders, order.FirstSide, elementId: null, 1, fleetRepair.StateVersion);
        return (reserve, order);
    }

    [Fact]
    public void StageEntryMovementCompletesForTheNewStagesFirstActingSideWithNoElementsMoved()
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        var world = CampaignWorldV6Factory.CreateInitial(artifact, scenario);

        var (reserve, order) = CompletedStageEntryReserve("test-campaign-stage-entry-movement", world, context);
        Assert.True(reserve.Completed);
        IReadOnlyList<CampaignOperationStageOrder> orders = [order];

        var completed = CampaignNativeSecondMovementCompletedV45.Create(
            reserve, world, context, orders, order.FirstSide, 1, reserve.StateVersion);

        Assert.Equal(order.FirstSide, completed.Side);
        Assert.Equal(2, Cna1979LandSequenceV4.CreateTurn(1)
            .Single(value => value.PositionId == completed.PositionId).OperationStage);
        var next = Cna1979LandSequenceV4.CreateTurn(1)
            .Single(value => value.PositionId == completed.NextPositionId);
        Assert.Equal(LandSegmentIds.BreakdownDetermination, next.SegmentId);

        var bytes = completed.SerializeCanonical();
        Assert.Equal(completed, CampaignNativeSecondMovementCompletedV45.Deserialize(bytes));
    }

    [Fact]
    public void StageEntryMovementRejectsTheOppositeStagesRole()
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        var world = CampaignWorldV6Factory.CreateInitial(artifact, scenario);

        var (reserve, order) = CompletedStageEntryReserve("test-campaign-stage-entry-movement-reject", world, context);
        IReadOnlyList<CampaignOperationStageOrder> orders = [order];

        var error = Assert.Throws<InvalidOperationException>(() => CampaignNativeSecondMovementCompletedV45.Create(
            reserve, world, context, orders, order.SecondSide, 1, reserve.StateVersion));

        Assert.Equal("Second Movement does not bind the owner and World.", error.Message);
    }
}
