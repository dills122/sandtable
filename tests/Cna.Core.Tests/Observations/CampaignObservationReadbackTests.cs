using System.Text;
using System.Text.Json;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignObservationReadbackTests
{
    [Fact]
    public void StrictReaderRoundTripsTheCompleteCanonicalMovementObservation()
    {
        var expected = CreateObservation();
        var canonical = CampaignObservationSerializer.SerializeCanonical(expected);

        var actual = CampaignObservationSerializer.DeserializeCanonical(canonical);

        Assert.Equal(expected, actual);
        Assert.Equal(canonical, CampaignObservationSerializer.SerializeCanonical(actual));
        Assert.Equal(5, actual.ContractVersion);
        Assert.Equal(
            "sandtable.observation.movement-side-safe.v1",
            actual.PolicyId);
        Assert.Equal(new CapabilityPointAmount(1, 2),
            Assert.Single(actual.OwnElements).CapabilityPointsExpended);
        Assert.Equal(new BreakdownPointAmount(21, 2),
            Assert.Single(actual.OwnElements).VehicleBreakdownRisk!
                .CumulativeBreakdownPoints);
        Assert.False(Assert.Single(actual.ApparentOpposingPresences).ExertsZoc);
    }

    [Fact]
    public void StrictReaderRejectsLegacyMissingExtraReorderedAndHiddenFields()
    {
        var canonical = SerializeText();
        var prefix =
            "{\"contractVersion\":5," +
            "\"policyId\":\"sandtable.observation.movement-side-safe.v1\"," +
            "\"campaignId\":\"campaign-1\",";
        var reorderedPrefix =
            "{\"policyId\":\"sandtable.observation.movement-side-safe.v1\"," +
            "\"contractVersion\":5," +
            "\"campaignId\":\"campaign-1\",";

        AssertRejects(canonical.Replace(
            "\"contractVersion\":5",
            "\"contractVersion\":4",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"contractVersion\":5,",
            "\"contractVersion\":5,\"contractVersion\":5,",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"policyId\":\"sandtable.observation.movement-side-safe.v1\",",
            string.Empty,
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"campaignId\":",
            "\"hiddenAuthority\":true,\"campaignId\":",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(prefix, reorderedPrefix, StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"representationId\":",
            "\"boundElementIds\":[\"commonwealth-element-a\"],\"representationId\":",
            StringComparison.Ordinal));
    }

    [Fact]
    public void StrictReaderRejectsMalformedValuesAndNoncanonicalExactAmounts()
    {
        var canonical = SerializeText();

        AssertRejects(canonical.Replace(
            "\"observer\":\"axis\"",
            "\"observer\":\"unknown\"",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"ledgerOperationStage\":1",
            "\"ledgerOperationStage\":4",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"capabilityPointsExpended\":{\"numerator\":1,\"denominator\":2}",
            "\"capabilityPointsExpended\":{\"numerator\":2,\"denominator\":4}",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"cumulativeBreakdownPoints\":{\"numerator\":21,\"denominator\":2}",
            "\"cumulativeBreakdownPoints\":{\"numerator\":42,\"denominator\":4}",
            StringComparison.Ordinal));
        AssertRejects(canonical.Replace(
            "\"sandstormAttributedBreakdownPoints\":{\"numerator\":1,\"denominator\":2}",
            "\"sandstormAttributedBreakdownPoints\":{\"numerator\":22,\"denominator\":1}",
            StringComparison.Ordinal));
        AssertRejects(canonical + "\n");
    }

    [Fact]
    public void StrictReaderRejectsNoncanonicalCollectionOrder()
    {
        var canonical = SerializeText();
        var first =
            "{\"representationId\":\"map-representation.0002\"," +
            "\"currentLocationId\":\"east\",\"exertsZoc\":false}";
        var second =
            "{\"representationId\":\"map-representation.0003\"," +
            "\"currentLocationId\":\"north\",\"exertsZoc\":false}";
        var withSecond = canonical.Replace(
            $"\"apparentOpposingPresences\":[{first}]",
            $"\"apparentOpposingPresences\":[{second},{first}]",
            StringComparison.Ordinal);
        AssertRejects(withSecond);
    }

    private static void AssertRejects(string value) =>
        Assert.Throws<JsonException>(() => CampaignObservationSerializer.DeserializeCanonical(
            Encoding.UTF8.GetBytes(value)));

    private static string SerializeText() => Encoding.UTF8.GetString(
        CampaignObservationSerializer.SerializeCanonical(CreateObservation()));

    private static CampaignObservation CreateObservation()
    {
        var risk = new ObservedOwnVehicleBreakdownRisk(
            "axis-element-a.vehicle-cohort.trucks",
            Cna1979Breakdown.VehicleTypeTruckId,
            Cna1979Breakdown.ProfileTruckId,
            new BreakdownPointAmount(21, 2),
            new BreakdownPointAmount(1, 2),
            "land.breakdown.band.4-10",
            9,
            1);
        var own = new ObservedOwnElement(
            "axis-element-a",
            "axis-lab-formation",
            "land.organization.battalion",
            20,
            "west",
            CampaignObservationReserveStatus.None,
            Cna1979Movement.MotorizedMobilityId,
            1,
            1,
            new CapabilityPointAmount(1, 2),
            -1,
            risk);

        return new CampaignObservation(
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
                new CampaignObservationLocation("east", "land.terrain.clear"),
                new CampaignObservationLocation("north", "land.terrain.clear"),
                new CampaignObservationLocation("west", "land.terrain.clear"),
            ],
            [],
            [own],
            [new ObservedApparentPresence(
                "map-representation.0002",
                "east",
                false)]);
    }
}
