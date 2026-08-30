using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignMovementCheckpointValidationTests
{
    [Fact]
    public void MovementCheckpointAdmitsRepeatedFirstSideMovementWithinBaseAllowance()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var mover = FindFirstSideElement(evidence, reserveStatus: CampaignElementReserveStatus.None);
        var destination = evidence.Context.Artifact.Definition.Locations
            .Select(location => location.LocationId)
            .First(locationId => !string.Equals(
                locationId,
                mover.CurrentLocationId,
                StringComparison.Ordinal));
        var moved = ReplaceElementAndRepresentation(
            evidence.Snapshot,
            mover.ElementId,
            destination,
            new CapabilityPointAmount(3, 2)) with
        {
            StateVersion = evidence.Snapshot.StateVersion + 3,
        };
        var belowMovementEntry = moved with
        {
            StateVersion = evidence.Snapshot.StateVersion - 1,
        };
        var reserveCheckpoint = CampaignProjector.Replay(
            evidence.Events.Take(evidence.Events.Count - 1),
            evidence.Context);
        var chargedBeforeMovement = ReplaceElementAndRepresentation(
            reserveCheckpoint,
            mover.ElementId,
            mover.CurrentLocationId,
            new CapabilityPointAmount(1, 2));

        Assert.True(CampaignSnapshotValidator.IsLocallyValid(moved));
        Assert.True(CampaignSnapshotValidator.IsValid(moved, evidence.Context));
        Assert.False(CampaignSnapshotValidator.IsLocallyValid(belowMovementEntry));
        Assert.False(CampaignSnapshotValidator.IsLocallyValid(chargedBeforeMovement));
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(moved),
            CampaignSnapshotSerializer.Serialize(
                CampaignSnapshotSerializer.Deserialize(
                    CampaignSnapshotSerializer.Serialize(moved))));
    }

    [Fact]
    public void MovementCheckpointRejectsUnknownLocationOverAllowanceAndAtomicityFailures()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var snapshot = evidence.Snapshot;
        var mover = FindFirstSideElement(evidence, reserveStatus: CampaignElementReserveStatus.None);
        var content = evidence.Context.Artifact.Definition.Elements.Single(element =>
            element.ElementId == mover.ElementId);
        var knownDestination = evidence.Context.Artifact.Definition.Locations
            .Select(location => location.LocationId)
            .First(locationId => !string.Equals(
                locationId,
                mover.CurrentLocationId,
                StringComparison.Ordinal));
        var coherent = ReplaceElementAndRepresentation(
            snapshot,
            mover.ElementId,
            knownDestination,
            new CapabilityPointAmount(1, 2)) with
        {
            StateVersion = snapshot.StateVersion + 1,
        };
        var unknownLocation = ReplaceElementAndRepresentation(
            coherent,
            mover.ElementId,
            "unknown-location",
            new CapabilityPointAmount(1, 2));
        var overAllowance = ReplaceElementAndRepresentation(
            coherent,
            mover.ElementId,
            knownDestination,
            new CapabilityPointAmount(content.BaseCapabilityPointAllowance + 1, 1));
        var zeroCostRelocation = ReplaceElementAndRepresentation(
            coherent,
            mover.ElementId,
            knownDestination,
            CapabilityPointAmount.Zero);
        var phantomMoveVersion = snapshot with
        {
            StateVersion = snapshot.StateVersion + 1,
        };
        var mismatchedRepresentation = new CampaignSnapshot(
            coherent.ContractVersion,
            coherent.CampaignId,
            coherent.StateVersion,
            coherent.RulesetHash,
            coherent.Setup,
            new CampaignWorldSnapshot(
                CampaignWorldSnapshot.CurrentContractVersion,
                coherent.World.Elements,
                coherent.World.Representations.Select(representation =>
                    representation.BoundElementIds.Contains(mover.ElementId)
                        ? new CampaignMapRepresentationState(
                            representation.RepresentationId,
                            mover.CurrentLocationId,
                            representation.BindingKind,
                            representation.BoundElementIds)
                        : representation).ToArray()),
            coherent.InitiativeHolder,
            coherent.OperationStageOrders,
            coherent.OperationStageWeather,
            coherent.RandomState,
            coherent.SequencePosition);
        var changedCohesion = ReplaceOperationalState(
            coherent,
            mover.ElementId,
            new CampaignElementOperationalState(
                mover.OperationalState.LedgerGameTurn,
                mover.OperationalState.LedgerOperationStage,
                new CapabilityPointAmount(1, 2),
                -1,
                mover.OperationalState.VehicleBreakdownState));
        var breakdown = Assert.IsType<CampaignVehicleBreakdownState>(
            mover.OperationalState.VehicleBreakdownState);
        var changedBreakdown = ReplaceOperationalState(
            coherent,
            mover.ElementId,
            new CampaignElementOperationalState(
                mover.OperationalState.LedgerGameTurn,
                mover.OperationalState.LedgerOperationStage,
                new CapabilityPointAmount(1, 2),
                mover.OperationalState.CohesionLevel,
                new CampaignVehicleBreakdownState(
                    breakdown.CohortId,
                    new BreakdownPointAmount(1, 1),
                    BreakdownPointAmount.Zero,
                    null,
                    breakdown.WorkingPointCount,
                    breakdown.BrokenPointCount)));

        Assert.False(CampaignSnapshotValidator.IsValid(unknownLocation, evidence.Context));
        Assert.False(CampaignSnapshotValidator.IsValid(overAllowance, evidence.Context));
        Assert.False(CampaignSnapshotValidator.IsValid(zeroCostRelocation, evidence.Context));
        Assert.False(CampaignSnapshotValidator.IsValid(phantomMoveVersion, evidence.Context));
        Assert.False(CampaignSnapshotValidator.IsLocallyValid(mismatchedRepresentation));
        Assert.False(CampaignSnapshotValidator.IsValid(changedCohesion, evidence.Context));
        Assert.False(CampaignSnapshotValidator.IsValid(changedBreakdown, evidence.Context));
    }

    [Fact]
    public void MovementCheckpointKeepsOpponentsAndReserveElementsAtInitialZeroState()
    {
        var evidence = CampaignMovementTestData.ReachMovement(reserveCount: 1);
        var reserve = FindFirstSideElement(
            evidence,
            reserveStatus: CampaignElementReserveStatus.ReserveI);
        var opponent = evidence.Snapshot.World.Elements.First(element =>
            !IsOnSide(element.ElementId, evidence.Context, evidence.ActingSide));
        var destination = evidence.Context.Artifact.Definition.Locations
            .Select(location => location.LocationId)
            .First(locationId => !string.Equals(
                locationId,
                reserve.CurrentLocationId,
                StringComparison.Ordinal));
        var movedReserve = ReplaceElementAndRepresentation(
            evidence.Snapshot,
            reserve.ElementId,
            destination,
            CapabilityPointAmount.Zero);
        var chargedReserve = ReplaceElementAndRepresentation(
            evidence.Snapshot,
            reserve.ElementId,
            reserve.CurrentLocationId,
            new CapabilityPointAmount(1, 2));
        var opponentDestination = evidence.Context.Artifact.Definition.Locations
            .Select(location => location.LocationId)
            .First(locationId => !string.Equals(
                locationId,
                opponent.CurrentLocationId,
                StringComparison.Ordinal));
        var movedOpponent = ReplaceElementAndRepresentation(
            evidence.Snapshot,
            opponent.ElementId,
            opponentDestination,
            CapabilityPointAmount.Zero);
        var chargedOpponent = ReplaceElementAndRepresentation(
            evidence.Snapshot,
            opponent.ElementId,
            opponent.CurrentLocationId,
            new CapabilityPointAmount(1, 2));

        Assert.False(CampaignSnapshotValidator.IsValid(movedReserve, evidence.Context));
        Assert.False(CampaignSnapshotValidator.IsValid(chargedReserve, evidence.Context));
        Assert.False(CampaignSnapshotValidator.IsValid(movedOpponent, evidence.Context));
        Assert.False(CampaignSnapshotValidator.IsValid(chargedOpponent, evidence.Context));
    }

    private static CampaignElementState FindFirstSideElement(
        CampaignMovementEvidence evidence,
        CampaignElementReserveStatus reserveStatus) => evidence.Snapshot.World.Elements.First(
            element => element.ReserveStatus == reserveStatus
                && IsOnSide(element.ElementId, evidence.Context, evidence.ActingSide));

    private static bool IsOnSide(
        string elementId,
        CampaignContentContext context,
        LandSide side)
    {
        var sideId = side == LandSide.Axis ? "axis" : "commonwealth";
        return string.Equals(
            context.Artifact.Definition.Elements.Single(element =>
                element.ElementId == elementId).SideId,
            sideId,
            StringComparison.Ordinal);
    }

    private static CampaignSnapshot ReplaceElementAndRepresentation(
        CampaignSnapshot snapshot,
        string elementId,
        string locationId,
        CapabilityPointAmount expenditure)
    {
        var element = snapshot.World.Elements.Single(value => value.ElementId == elementId);
        var operational = new CampaignElementOperationalState(
            element.OperationalState.LedgerGameTurn,
            element.OperationalState.LedgerOperationStage,
            expenditure,
            element.OperationalState.CohesionLevel,
            element.OperationalState.VehicleBreakdownState);
        var world = new CampaignWorldSnapshot(
            CampaignWorldSnapshot.CurrentContractVersion,
            snapshot.World.Elements.Select(value => value.ElementId == elementId
                ? new CampaignElementState(
                    value.ElementId,
                    locationId,
                    value.ReserveStatus,
                    operational)
                : value).ToArray(),
            snapshot.World.Representations.Select(value =>
                value.BoundElementIds.Contains(elementId)
                    ? new CampaignMapRepresentationState(
                        value.RepresentationId,
                        locationId,
                        value.BindingKind,
                        value.BoundElementIds)
                    : value).ToArray());
        return snapshot with { World = world };
    }

    private static CampaignSnapshot ReplaceOperationalState(
        CampaignSnapshot snapshot,
        string elementId,
        CampaignElementOperationalState operationalState)
    {
        var world = new CampaignWorldSnapshot(
            CampaignWorldSnapshot.CurrentContractVersion,
            snapshot.World.Elements.Select(value => value.ElementId == elementId
                ? new CampaignElementState(
                    value.ElementId,
                    value.CurrentLocationId,
                    value.ReserveStatus,
                    operationalState)
                : value).ToArray(),
            snapshot.World.Representations);
        return snapshot with { World = world };
    }
}
