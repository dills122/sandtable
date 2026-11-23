using System.Security.Cryptography;
using Cna.Core.Campaigns;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

// MTS-TASK-005 (2026-11-22): proves the new stage's own first-acting-side
// Reserve Designation -- CampaignNativeSecondReserveV43.Create reused for a
// THIRD occurrence (Stage 1's second side and this one), with no new type.
// Same "dormant, contract-level" testing depth as
// CampaignNativeStageEntryPreambleTests (no real campaign can drive a full
// Combat cycle to Patrol completion yet in this suite, Core or Host), but
// unlike that file's own full-chain test, this one carries the full chain
// through to a genuinely SUCCESSFUL Create call by giving the synthetic
// predecessor the real initial World's own hash, rather than a placeholder.
public sealed class CampaignNativeStageEntryReserveTests
{
    private static string Hash(byte theme) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData([theme]));

    private static RandomStreamState GenesisRandomState() =>
        new(SandtableRandom.ContractVersion, SandtableRandom.AlgorithmId, 4242, 0);

    // Same synthetic predecessor shape as
    // CampaignNativeStageEntryPreambleTests.SecondSidePatrolComplete, except
    // its WorldHash is the real initial World's own hash (not a placeholder)
    // so the chain below can actually complete, not just reach the right
    // rejection message.
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

    [Fact]
    public void StageEntryReserveCompletesForTheNewStagesFirstActingSide()
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        var world = CampaignWorldV6Factory.CreateInitial(artifact, scenario);

        var predecessor = SecondSidePatrolComplete("test-campaign-stage-entry-reserve", 100, world);
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

        var (reserve, resultWorld) = CampaignNativeSecondReserveV43.Create(
            fleetRepair, world, context, orders, order.FirstSide, elementId: null, 1, fleetRepair.StateVersion);

        Assert.Equal(order.FirstSide, reserve.Side);
        Assert.Equal(2, Cna1979LandSequenceV4.CreateTurn(1)
            .Single(value => value.PositionId == reserve.PositionId).OperationStage);
        Assert.True(reserve.Completed);
        Assert.Equal(CampaignNativeCombatPaidWorldV22.Hash(resultWorld), reserve.ResultWorldHash);
        Assert.Same(world, resultWorld);

        var bytes = reserve.SerializeCanonical();
        Assert.Equal(reserve, CampaignNativeSecondReserveV43.Deserialize(bytes));
    }

    [Fact]
    public void StageEntryReserveRejectsTheOppositeStagesRole()
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        var world = CampaignWorldV6Factory.CreateInitial(artifact, scenario);

        var predecessor = SecondSidePatrolComplete("test-campaign-stage-entry-reserve-reject", 100, world);
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

        var error = Assert.Throws<InvalidOperationException>(() => CampaignNativeSecondReserveV43.Create(
            fleetRepair, world, context, orders, order.SecondSide, elementId: null, 1, fleetRepair.StateVersion));

        Assert.Equal("Second Reserve requires its owner and settled World.", error.Message);
    }
}
