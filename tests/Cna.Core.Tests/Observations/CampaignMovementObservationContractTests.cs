using System.Text.Json;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignMovementObservationContractTests
{
    [Fact]
    public void MovementObservationFreezesContractAndPolicyIdentity()
    {
        Assert.Equal(5, CampaignObservation.CurrentContractVersion);
        Assert.Equal(
            "sandtable.observation.movement-side-safe.v1",
            CampaignObservation.CurrentPolicyId);
    }

    [Fact]
    public void VehicleBreakdownRiskCopiesExactSideSafeContinuityAndComparesStructurally()
    {
        var cumulative = new BreakdownPointAmount(42, 2);
        var sandstorm = new BreakdownPointAmount(2, 4);
        var first = new ObservedOwnVehicleBreakdownRisk(
            "axis-element-a.vehicle-cohort.trucks",
            Cna1979Breakdown.VehicleTypeTruckId,
            Cna1979Breakdown.ProfileTruckId,
            cumulative,
            sandstorm,
            "land.breakdown.band.4-10",
            9,
            1);
        var equivalent = new ObservedOwnVehicleBreakdownRisk(
            "axis-element-a.vehicle-cohort.trucks",
            Cna1979Breakdown.VehicleTypeTruckId,
            Cna1979Breakdown.ProfileTruckId,
            new BreakdownPointAmount(21, 1),
            new BreakdownPointAmount(1, 2),
            "land.breakdown.band.4-10",
            9,
            1);

        Assert.Equal("axis-element-a.vehicle-cohort.trucks", first.CohortId);
        Assert.Equal(Cna1979Breakdown.VehicleTypeTruckId, first.VehicleTypeId);
        Assert.Equal(Cna1979Breakdown.ProfileTruckId, first.ProfileId);
        Assert.Equal(new BreakdownPointAmount(21, 1), first.CumulativeBreakdownPoints);
        Assert.Equal(
            new BreakdownPointAmount(1, 2),
            first.SandstormAttributedBreakdownPoints);
        Assert.Equal("land.breakdown.band.4-10", first.HighestEffectiveCheckedBandId);
        Assert.Equal(9, first.WorkingPointCount);
        Assert.Equal(1, first.BrokenPointCount);
        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
    }

    [Fact]
    public void VehicleBreakdownRiskRejectsMalformedOrIncoherentContinuity()
    {
        Assert.Throws<ArgumentException>(() => CreateRisk(cohortId: "Invalid ID"));
        Assert.Throws<ArgumentException>(() => CreateRisk(vehicleTypeId: "Invalid ID"));
        Assert.Throws<ArgumentException>(() => CreateRisk(profileId: "Invalid ID"));
        Assert.Throws<ArgumentException>(() => CreateRisk(
            vehicleTypeId: "land.breakdown.vehicle-type.unsupported"));
        Assert.Throws<ArgumentException>(() => CreateRisk(
            profileId: "land.breakdown.profile.unsupported"));
        Assert.Throws<ArgumentNullException>(() => new ObservedOwnVehicleBreakdownRisk(
            "axis-element-a.vehicle-cohort.trucks",
            Cna1979Breakdown.VehicleTypeTruckId,
            Cna1979Breakdown.ProfileTruckId,
            null!,
            BreakdownPointAmount.Zero,
            null,
            1,
            0));
        Assert.Throws<ArgumentNullException>(() => new ObservedOwnVehicleBreakdownRisk(
            "axis-element-a.vehicle-cohort.trucks",
            Cna1979Breakdown.VehicleTypeTruckId,
            Cna1979Breakdown.ProfileTruckId,
            BreakdownPointAmount.Zero,
            null!,
            null,
            1,
            0));
        Assert.Throws<ArgumentException>(() => CreateRisk(
            cumulative: new BreakdownPointAmount(1, 2),
            sandstorm: new BreakdownPointAmount(1, 1)));
        Assert.Throws<ArgumentException>(() => CreateRisk(highestBandId: "unknown-band"));
        Assert.Throws<ArgumentException>(() => CreateRisk(
            highestBandId: "land.breakdown.band.0-3"));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateRisk(workingPointCount: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateRisk(brokenPointCount: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateRisk(
            workingPointCount: 0,
            brokenPointCount: 0));
        Assert.Throws<OverflowException>(() => CreateRisk(
            workingPointCount: int.MaxValue,
            brokenPointCount: 1));
    }

    [Fact]
    public void ApparentPresenceHasOnlyTheApprovedThreeFieldShape()
    {
        var first = new ObservedApparentPresence(
            "map-representation.0002",
            "east",
            false);
        var equivalent = new ObservedApparentPresence(
            "map-representation.0002",
            "east",
            false);

        Assert.Equal("map-representation.0002", first.RepresentationId);
        Assert.Equal("east", first.CurrentLocationId);
        Assert.False(first.ExertsZoc);
        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
        Assert.Equal(
            ["RepresentationId", "CurrentLocationId", "ExertsZoc"],
            typeof(ObservedApparentPresence)
                .GetProperties()
                .Select(property => property.Name));
    }

    [Fact]
    public void ApparentPresenceRejectsUnstableIdentityOrLocation()
    {
        Assert.Throws<ArgumentException>(() => new ObservedApparentPresence(
            "Invalid ID",
            "east",
            false));
        Assert.Throws<ArgumentException>(() => new ObservedApparentPresence(
            "map-representation.0002",
            "Invalid ID",
            false));
    }

    [Fact]
    public void OwnElementCarriesExactMovementLedgerAndOptionalVehicleRisk()
    {
        var risk = CreateRisk();
        var first = CreateOwnElement(risk);
        var equivalent = CreateOwnElement(CreateRisk());
        var withoutRisk = CreateOwnElement(null);

        Assert.Equal(Cna1979Movement.MotorizedMobilityId, first.MobilityId);
        Assert.Equal(1, first.LedgerGameTurn);
        Assert.Equal(1, first.LedgerOperationStage);
        Assert.Equal(new CapabilityPointAmount(1, 2), first.CapabilityPointsExpended);
        Assert.Equal(-1, first.CohesionLevel);
        Assert.Equal(risk, first.VehicleBreakdownRisk);
        Assert.Null(withoutRisk.VehicleBreakdownRisk);
        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
    }

    [Fact]
    public void OwnElementRejectsInvalidMovementLedgerFacts()
    {
        Assert.Throws<ArgumentException>(() => CreateOwnElement(
            mobilityId: "land.mobility.unsupported"));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateOwnElement(
            ledgerGameTurn: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateOwnElement(
            ledgerOperationStage: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateOwnElement(
            ledgerOperationStage: 4));
        Assert.Throws<ArgumentNullException>(() => new ObservedOwnElement(
            "axis-element-a",
            "axis-lab-formation",
            "land.organization.battalion",
            20,
            "west",
            CampaignObservationReserveStatus.None,
            Cna1979Movement.MotorizedMobilityId,
            1,
            1,
            null!,
            0,
            null));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateOwnElement(
            cohesionLevel: 11));
    }

    [Fact]
    public void AggregateCopiesOrdersAndComparesApparentPresencesStructurally()
    {
        var apparent = new List<ObservedApparentPresence>
        {
            new("map-representation.0003", "north", false),
            new("map-representation.0002", "east", false),
        };
        var first = CreateObservation(apparent);
        var equivalent = CreateObservation(apparent.AsEnumerable().Reverse().ToArray());

        apparent.Clear();

        Assert.Equal(
            ["map-representation.0002", "map-representation.0003"],
            first.ApparentOpposingPresences.Select(presence => presence.RepresentationId));
        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
        Assert.Equal(
            CampaignObservationSerializer.SerializeCanonical(first),
            CampaignObservationSerializer.SerializeCanonical(equivalent));
    }

    [Fact]
    public void AggregateRejectsDuplicateOrUnknownApparentPresenceReferences()
    {
        var presence = new ObservedApparentPresence(
            "map-representation.0002",
            "east",
            false);

        Assert.Throws<ArgumentException>(() => CreateObservation([presence, presence]));
        Assert.Throws<ArgumentException>(() => CreateObservation(
            [new ObservedApparentPresence(
                "map-representation.0002",
                "unknown-location",
                false)]));
    }

    [Fact]
    public void CanonicalMovementObservationUsesTheApprovedOrderedFieldAllowlists()
    {
        var observation = CreateObservation(
            [new ObservedApparentPresence(
                "map-representation.0002",
                "east",
                false)]);
        using var document = JsonDocument.Parse(
            CampaignObservationSerializer.SerializeCanonical(observation));
        var root = document.RootElement;
        var own = root.GetProperty("ownElements")[0];
        var risk = own.GetProperty("vehicleBreakdownRisk");
        var apparent = root.GetProperty("apparentOpposingPresences")[0];

        Assert.Equal(
            [
                "contractVersion",
                "policyId",
                "campaignId",
                "stateVersion",
                "rulesetHash",
                "scenarioId",
                "observer",
                "position",
                "weather",
                "locations",
                "edges",
                "ownElements",
                "apparentOpposingPresences",
            ],
            root.EnumerateObject().Select(property => property.Name));
        Assert.Equal(
            [
                "elementId",
                "parentFormationId",
                "organizationId",
                "baseCapabilityPointAllowance",
                "currentLocationId",
                "reserveStatus",
                "mobilityId",
                "ledgerGameTurn",
                "ledgerOperationStage",
                "capabilityPointsExpended",
                "cohesionLevel",
                "vehicleBreakdownRisk",
            ],
            own.EnumerateObject().Select(property => property.Name));
        Assert.Equal(
            [
                "cohortId",
                "vehicleTypeId",
                "profileId",
                "cumulativeBreakdownPoints",
                "sandstormAttributedBreakdownPoints",
                "highestEffectiveCheckedBandId",
                "workingPointCount",
                "brokenPointCount",
            ],
            risk.EnumerateObject().Select(property => property.Name));
        Assert.Equal(
            ["representationId", "currentLocationId", "exertsZoc"],
            apparent.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public void MovementObservationValuesCannotBeConstructedByExternalCallers()
    {
        Assert.Empty(typeof(ObservedOwnVehicleBreakdownRisk).GetConstructors());
        Assert.Empty(typeof(ObservedApparentPresence).GetConstructors());
    }

    private static ObservedOwnVehicleBreakdownRisk CreateRisk(
        string cohortId = "axis-element-a.vehicle-cohort.trucks",
        string vehicleTypeId = Cna1979Breakdown.VehicleTypeTruckId,
        string profileId = Cna1979Breakdown.ProfileTruckId,
        BreakdownPointAmount? cumulative = null,
        BreakdownPointAmount? sandstorm = null,
        string? highestBandId = "land.breakdown.band.4-10",
        int workingPointCount = 9,
        int brokenPointCount = 1) => new(
            cohortId,
            vehicleTypeId,
            profileId,
            cumulative ?? new BreakdownPointAmount(21, 1),
            sandstorm ?? new BreakdownPointAmount(1, 2),
            highestBandId,
            workingPointCount,
            brokenPointCount);

    private static ObservedOwnElement CreateOwnElement(
        ObservedOwnVehicleBreakdownRisk? risk = null,
        string mobilityId = Cna1979Movement.MotorizedMobilityId,
        int ledgerGameTurn = 1,
        int ledgerOperationStage = 1,
        CapabilityPointAmount? capabilityPointsExpended = null,
        int cohesionLevel = -1) => new(
            "axis-element-a",
            "axis-lab-formation",
            "land.organization.battalion",
            20,
            "west",
            CampaignObservationReserveStatus.None,
            mobilityId,
            ledgerGameTurn,
            ledgerOperationStage,
            capabilityPointsExpended ?? new CapabilityPointAmount(1, 2),
            cohesionLevel,
            risk);

    private static CampaignObservation CreateObservation(
        IReadOnlyList<ObservedApparentPresence> apparentOpposingPresences) => new(
            CampaignObservation.CurrentContractVersion,
            CampaignObservation.CurrentPolicyId,
            "campaign-1",
            1,
            Cna1979Ruleset.Manifest.Hash,
            "movement-contact-lab",
            LandSide.Axis,
            new CampaignObservationPosition(
                "land.position.movement",
                1,
                1,
                "land.stage.operation-stage-1",
                "land.phase.movement",
                null,
                null,
                LandActorRole.FirstActingSide,
                LandSide.Axis,
                LandSide.Axis),
            null,
            [
                new CampaignObservationLocation("west", "land.terrain.clear"),
                new CampaignObservationLocation("east", "land.terrain.clear"),
                new CampaignObservationLocation("north", "land.terrain.clear"),
            ],
            [],
            [CreateOwnElement(CreateRisk())],
            apparentOpposingPresences);
}
