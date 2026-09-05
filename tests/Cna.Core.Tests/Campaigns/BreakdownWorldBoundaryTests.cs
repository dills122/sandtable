using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class BreakdownWorldBoundaryTests
{
    [Theory]
    [InlineData("missing-ledger")]
    [InlineData("foreign-cohort")]
    [InlineData("extra-working-point")]
    [InlineData("unbacked-broken-point")]
    [InlineData("noncohort-ledger")]
    public void LedgerMustDescribeOnlyImmutableAdmittedCohort(string mutation)
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var initial = CampaignWorldV6Factory.CreateInitial(artifact, scenario);
        var truck = initial.Elements.Single(element => element.ElementId == "axis-truck");
        var original = truck.OperationalState.VehicleBreakdownState!;
        var element = mutation == "noncohort-ledger"
            ? initial.Elements.Single(value => value.ElementId == "axis-infantry")
            : truck;
        var ledger = mutation == "missing-ledger" ? null : new CampaignVehicleBreakdownState(
            mutation == "foreign-cohort" ? "foreign-cohort" : original.CohortId,
            original.CumulativeBreakdownPoints, original.SandstormAttributedBreakdownPoints,
            original.HighestEffectiveCheckedBandId,
            original.WorkingPointCount + (mutation == "extra-working-point" ? 1 : 0),
            mutation == "unbacked-broken-point" ? 1 : 0);
        var changed = new CampaignElementStateV5(element.ElementId, element.CurrentLocationId, element.ReserveStatus,
            new CampaignElementOperationalStateV5(1, 1, CapabilityPointAmount.Zero, 0, ledger, null), element.Components);
        var invalid = new CampaignWorldSnapshotV6(6,
            initial.Elements.Select(value => value.ElementId == changed.ElementId ? changed : value),
            initial.Representations, []);

        Assert.True(CampaignWorldV6Validator.IsValidInitial(initial, artifact, scenario));
        Assert.False(CampaignWorldV6Validator.IsValid(invalid, artifact, scenario));
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("vehicle-type")]
    [InlineData("location")]
    public void RehashedConservedLotStillRequiresAdmittedOwnerTypeAndLocation(string mutation)
    {
        var snapshot = BreakdownSnapshotTests.Create(true);
        var artifact = BreakdownContentFixture.Artifact();
        var truck = snapshot.World.Elements.Single(element => element.ElementId == "axis-truck");
        var cohort = artifact.Definition.LegacyDefinition.Elements.Single(element => element.ElementId == truck.ElementId).BreakdownVehicleCohort!;
        var lot = CampaignBrokenVehicleLot.Create(snapshot.CampaignId, snapshot.RulesetHash,
            CampaignBreakdownCodec.EmptyHash, mutation == "owner" ? LandSide.Commonwealth : LandSide.Axis,
            cohort.CohortId, mutation == "vehicle-type" ? "land.vehicle.unadmitted" : cohort.VehicleTypeId,
            2, mutation == "location" ? "outside-map" : truck.CurrentLocationId, 10,
            [new RuleReference("spi-1979-land-rules", "21.41")]);
        lot.ValidateIdentity(snapshot.CampaignId, snapshot.RulesetHash);
        var invalid = BreakdownWorldTests.WithTruck(snapshot.World, truck, truck.CurrentLocationId,
            truck.OperationalState.VehicleBreakdownState!.WorkingPointCount - 2, 2, [lot]);

        Assert.False(CampaignWorldV6Validator.IsValid(invalid, artifact,
            artifact.Definition.LegacyDefinition.Scenarios.Single()));
    }

    [Fact]
    public void SeparatelyCertifiedFriendlyBattalionsCannotMergeIntoOversizedCombatStack()
    {
        var root = JsonNode.Parse(BreakdownContentFixture.Bytes())!;
        root["elements"]!.AsArray().Single(value => value!["elementId"]!.GetValue<string>() == "commonwealth-infantry")!["sideId"] = "axis";
        root["formations"]!.AsArray().Single(value => value!["formationId"]!.GetValue<string>() == "commonwealth-infantry.formation")!["sideId"] = "axis";
        var parsed = ContentPackV6Serializer.Deserialize(Encoding.UTF8.GetBytes(root.ToJsonString()));
        Assert.True(parsed.IsSuccess, parsed.Message);
        var artifact = ContentPackV6Artifact.Create(parsed.Definition!);
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();
        var initial = CampaignWorldV6Factory.CreateInitial(artifact, scenario);
        Assert.True(CampaignWorldV6Validator.IsValidInitial(initial, artifact, scenario));
        var destination = initial.Elements.Single(element => element.ElementId == "axis-infantry").CurrentLocationId;
        var mover = initial.Elements.Single(element => element.ElementId == "commonwealth-infantry");
        Assert.NotEqual(destination, mover.CurrentLocationId);
        var updated = new CampaignElementStateV5(mover.ElementId, destination, mover.ReserveStatus,
            mover.OperationalState, mover.Components);
        var combined = new CampaignWorldSnapshotV6(6,
            initial.Elements.Select(value => value.ElementId == mover.ElementId ? updated : value),
            initial.Representations.Select(value => value.BoundElementIds.Contains(mover.ElementId)
                ? new CampaignMapRepresentationState(value.RepresentationId, destination, value.BindingKind, value.BoundElementIds)
                : value), []);

        Assert.False(CampaignWorldV6Validator.IsValid(combined, artifact, scenario));
    }

    [Fact]
    public void SurvivorRelocationPreservesEveryCanonicalLotField()
    {
        var snapshot = BreakdownSnapshotTests.Create(true);
        var truck = snapshot.World.Elements.Single(element => element.ElementId == "axis-truck");
        var lot = Lot(snapshot);
        var working = truck.OperationalState.VehicleBreakdownState!.WorkingPointCount - lot.PointCount;
        var damaged = BreakdownWorldTests.WithTruck(snapshot.World, truck, truck.CurrentLocationId, working, 2, [lot]);
        var before = BreakdownSnapshotTests.Copy(snapshot, new CampaignBreakdownFlow.Idle(), world: damaged);
        var moved = BreakdownWorldTests.WithTruck(damaged,
            damaged.Elements.Single(element => element.ElementId == truck.ElementId), "center", working, 2, damaged.BrokenVehicleLots);
        var after = BreakdownSnapshotTests.Copy(snapshot, new CampaignBreakdownFlow.Idle(), world: moved);
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.Single();

        Assert.True(CampaignSnapshotV11Validator.IsValid(before, artifact, scenario));
        Assert.True(CampaignSnapshotV11Validator.IsValid(after, artifact, scenario));
        Assert.Equal("center", after.World.Elements.Single(element => element.ElementId == truck.ElementId).CurrentLocationId);
        Assert.Equal("west", Assert.Single(after.World.BrokenVehicleLots).LocationId);
        using var beforeJson = JsonDocument.Parse(CampaignSnapshotV11Serializer.Serialize(before));
        using var afterJson = JsonDocument.Parse(CampaignSnapshotV11Serializer.Serialize(after));
        Assert.Equal(beforeJson.RootElement.GetProperty("world").GetProperty("brokenVehicleLots").GetRawText(),
            afterJson.RootElement.GetProperty("world").GetProperty("brokenVehicleLots").GetRawText());
        Assert.Equal(after, CampaignSnapshotV11Serializer.Deserialize(CampaignSnapshotV11Serializer.Serialize(after)));
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("sources")]
    [InlineData("future-version")]
    public void SnapshotRejectsLotForgeryEvenWhenConservationStillBalances(string mutation)
    {
        var snapshot = BreakdownSnapshotTests.Create(true);
        var truck = snapshot.World.Elements.Single(element => element.ElementId == "axis-truck");
        var valid = Lot(snapshot);
        var forged = mutation == "identity"
            ? new CampaignBrokenVehicleLot(CampaignBreakdownCodec.EmptyHash, valid.StopId, valid.CheckId,
                valid.Owner, valid.CohortId, valid.VehicleTypeId, valid.PointCount, valid.LocationId,
                valid.CreatedStateVersion, valid.Sources)
            : CampaignBrokenVehicleLot.Create(snapshot.CampaignId, snapshot.RulesetHash, valid.StopId,
                valid.Owner, valid.CohortId, valid.VehicleTypeId, valid.PointCount, valid.LocationId,
                mutation == "future-version" ? snapshot.StateVersion + 1 : valid.CreatedStateVersion,
                mutation == "sources" ? [new RuleReference("spi-1979-land-rules", "21.99")] : valid.Sources);
        if (mutation != "identity") forged.ValidateIdentity(snapshot.CampaignId, snapshot.RulesetHash);
        var invalid = BreakdownWorldTests.WithTruck(snapshot.World, truck, truck.CurrentLocationId,
            truck.OperationalState.VehicleBreakdownState!.WorkingPointCount - forged.PointCount, forged.PointCount, [forged]);

        Assert.Throws<ArgumentException>(() => BreakdownSnapshotTests.Copy(snapshot, new CampaignBreakdownFlow.Idle(), world: invalid));
    }

    private static CampaignBrokenVehicleLot Lot(CampaignSnapshotV11 snapshot)
    {
        var truck = snapshot.World.Elements.Single(element => element.ElementId == "axis-truck");
        var cohort = BreakdownContentFixture.Artifact().Definition.LegacyDefinition.Elements
            .Single(element => element.ElementId == truck.ElementId).BreakdownVehicleCohort!;
        return CampaignBrokenVehicleLot.Create(snapshot.CampaignId, snapshot.RulesetHash,
            CampaignBreakdownCodec.EmptyHash, LandSide.Axis, cohort.CohortId, cohort.VehicleTypeId,
            2, truck.CurrentLocationId, 10, [new RuleReference("spi-1979-land-rules", "21.41")]);
    }
}
