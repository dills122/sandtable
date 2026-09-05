using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class BreakdownWorldTests
{
    [Fact]
    public void InitialWorldCertifiesExactPointCountsAndEmptyLots()
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var world = CampaignWorldV6Factory.CreateInitial(artifact, scenario);
        Assert.True(CampaignWorldV6Validator.IsValidInitial(world, artifact, scenario));
        Assert.Equal(6, world.ContractVersion);
        Assert.Empty(world.BrokenVehicleLots);
        Assert.Equal(2, world.Elements.Count(element => element.OperationalState.VehicleBreakdownState is not null));
    }

    [Fact]
    public void LossConservesWorkingAndLotsAndSurvivorsLeaveLotsStationary()
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var initial = CampaignWorldV6Factory.CreateInitial(artifact, scenario);
        var truck = initial.Elements.First(element => element.OperationalState.VehicleBreakdownState is not null);
        var original = truck.OperationalState.VehicleBreakdownState!;
        var cohort = artifact.Definition.LegacyDefinition.Elements.Single(element => element.ElementId == truck.ElementId).BreakdownVehicleCohort!;
        var lot = CampaignBrokenVehicleLot.Create("breakdown-test", new string('a', 64), CampaignBreakdownCodec.EmptyHash,
            LandSide.Axis, cohort.CohortId, cohort.VehicleTypeId, 2, truck.CurrentLocationId, 5,
            [new RuleReference("spi-1979-land-rules", "21.41")]);
        var damaged = WithTruck(initial, truck, truck.CurrentLocationId, original.WorkingPointCount - 2, 2, [lot]);
        Assert.True(CampaignWorldV6Validator.IsValid(damaged, artifact, scenario));
        var destination = artifact.Definition.LegacyDefinition.Locations.First(location => location.LocationId != truck.CurrentLocationId).LocationId;
        var moved = WithTruck(damaged, damaged.Elements.Single(element => element.ElementId == truck.ElementId), destination,
            original.WorkingPointCount - 2, 2, [lot]);
        Assert.True(CampaignWorldV6Validator.IsValid(moved, artifact, scenario));
        Assert.Equal(truck.CurrentLocationId, Assert.Single(moved.BrokenVehicleLots).LocationId);
        Assert.False(CampaignWorldV6Validator.IsValid(WithTruck(initial, truck, truck.CurrentLocationId,
            original.WorkingPointCount - 1, 2, [lot]), artifact, scenario));
        Assert.False(CampaignWorldV6Validator.IsValid(WithTruck(initial, truck, truck.CurrentLocationId,
            original.WorkingPointCount - 2, 2, []), artifact, scenario));
        Assert.False(CampaignWorldV6Validator.IsValidInitial(damaged, artifact, scenario));
    }

    [Fact]
    public void DynamicCombatAggregateAndStageLedgerCannotEscapeProfile()
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var initial = CampaignWorldV6Factory.CreateInitial(artifact, scenario);
        var element = initial.Elements[0];
        var wrongStage = new CampaignElementStateV5(element.ElementId, element.CurrentLocationId, element.ReserveStatus,
            new CampaignElementOperationalStateV5(2, 1, CapabilityPointAmount.Zero, 0, element.OperationalState.VehicleBreakdownState, null), element.Components);
        var world = new CampaignWorldSnapshotV6(6, initial.Elements.Select(value => value == element ? wrongStage : value), initial.Representations, []);
        Assert.False(CampaignWorldV6Validator.IsValid(world, artifact, scenario));
    }

    internal static CampaignWorldSnapshotV6 WithTruck(CampaignWorldSnapshotV6 world, CampaignElementStateV5 truck,
        string location, int working, int broken, IEnumerable<CampaignBrokenVehicleLot> lots)
    {
        var ledger = truck.OperationalState.VehicleBreakdownState!;
        var updated = new CampaignElementStateV5(truck.ElementId, location, truck.ReserveStatus,
            new CampaignElementOperationalStateV5(truck.OperationalState.LedgerGameTurn, truck.OperationalState.LedgerOperationStage,
                truck.OperationalState.CapabilityPointsExpended, truck.OperationalState.CohesionLevel,
                new CampaignVehicleBreakdownState(ledger.CohortId, ledger.CumulativeBreakdownPoints,
                    ledger.SandstormAttributedBreakdownPoints, ledger.HighestEffectiveCheckedBandId, working, broken), null), truck.Components);
        return new CampaignWorldSnapshotV6(6, world.Elements.Select(value => value.ElementId == truck.ElementId ? updated : value),
            world.Representations.Select(value => value.BoundElementIds.Contains(truck.ElementId)
                ? new CampaignMapRepresentationState(value.RepresentationId, location, value.BindingKind, value.BoundElementIds) : value), lots);
    }
}
