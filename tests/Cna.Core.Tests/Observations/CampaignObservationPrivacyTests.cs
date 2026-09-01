using System.Text;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Observations;

[Trait("Boundary", "UserSpace")]
public sealed class CampaignObservationPrivacyTests
{
    [Theory]
    [InlineData(LandSide.Axis)]
    [InlineData(LandSide.Commonwealth)]
    public void HiddenOpponentChangesRemainInvisibleAtEveryCheckpointThroughMovement(
        LandSide observer)
    {
        var pair = CampaignObservationTestData.CreateApparentEquivalentPair(observer);
        Assert.NotEqual(
            pair.BaselineContext.Artifact.Identity.Hash,
            pair.ChangedContext.Artifact.Identity.Hash);
        Assert.NotEqual(pair.BaselineSnapshot.Setup.SetupHash, pair.ChangedSnapshot.Setup.SetupHash);
        Assert.Equal(
            pair.BaselineSnapshot.World.Representations.Select(value => (
                value.RepresentationId,
                value.CurrentLocationId)),
            pair.ChangedSnapshot.World.Representations.Select(value => (
                value.RepresentationId,
                value.CurrentLocationId)));
        Assert.NotEqual(
            pair.BaselineSnapshot.World.Representations.SelectMany(value => value.BoundElementIds),
            pair.ChangedSnapshot.World.Representations.SelectMany(value => value.BoundElementIds));

        var baseline = CampaignObservationTestData.AdvanceThroughMovement(
            pair.BaselineSnapshot,
            pair.BaselineContext);
        var changed = CampaignObservationTestData.AdvanceThroughMovement(
            pair.ChangedSnapshot,
            pair.ChangedContext);

        Assert.Equal(11, baseline.Count);
        Assert.Equal(baseline.Count, changed.Count);

        for (var index = 0; index < baseline.Count; index++)
        {
            var baselineObservation = Project(
                baseline[index],
                pair.BaselineContext,
                observer);
            var changedObservation = Project(
                changed[index],
                pair.ChangedContext,
                observer);

            Assert.Equal(baselineObservation, changedObservation);
            Assert.Equal(baselineObservation.GetHashCode(),
                changedObservation.GetHashCode());
            Assert.Equal(
                CampaignObservationSerializer.SerializeCanonical(baselineObservation),
                CampaignObservationSerializer.SerializeCanonical(changedObservation));
            Assert.Equal(2, changedObservation.ApparentOpposingPresences.Count);
            Assert.All(
                changedObservation.ApparentOpposingPresences,
                presence => Assert.False(presence.ExertsZoc));
        }

        Assert.Equal(LandSegmentIds.Movement, baseline[^1].SegmentId);
    }

    [Fact]
    public void ApparentPresenceUsesTheExactAllowlistAndHidesAuthoritativeCanaries()
    {
        var pair = CampaignObservationTestData.CreateApparentEquivalentPair(LandSide.Axis);
        var observation = Project(pair.ChangedSnapshot, pair.ChangedContext, LandSide.Axis);
        var payload = Encoding.UTF8.GetString(
            CampaignObservationSerializer.SerializeCanonical(observation));
        string[] prohibitedCanaries =
        [
            "commonwealth-hidden",
            "enemy-sentinel-formation",
            "commonwealth-element",
            "privacy.element",
            "privacy.placement",
            "\"baseCapabilityPointAllowance\":31",
            "\"baseCapabilityPointAllowance\":32",
            pair.ChangedContext.Artifact.Identity.Hash,
            pair.ChangedSnapshot.Setup.SetupHash,
            SandtableRandom.AlgorithmId,
            "nextByteCursor",
            "sourceCoordinate",
            "origin",
            "boundElementIds",
            "bindingKind",
            "operationalState",
            "campaignEvent",
        ];

        Assert.All(prohibitedCanaries, canary =>
            Assert.DoesNotContain(canary, payload, StringComparison.Ordinal));
        Assert.Equal(2, observation.OwnElements.Count);
        Assert.Equal(
            ["map-representation.0003", "map-representation.0004"],
            observation.ApparentOpposingPresences.Select(value => value.RepresentationId));
        Assert.All(
            observation.ApparentOpposingPresences,
            presence => Assert.False(presence.ExertsZoc));
        Assert.Equal(
            ["CurrentLocationId", "ExertsZoc", "RepresentationId"],
            typeof(ObservedApparentPresence)
                .GetProperties()
                .Select(property => property.Name)
                .Order(StringComparer.Ordinal));
        Assert.Contains("\"apparentOpposingPresences\"", payload, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(LandSide.Axis)]
    [InlineData(LandSide.Commonwealth)]
    public void ChangedOpponentApparentLocationProducesOnlyTheApprovedVisibleDelta(
        LandSide observer)
    {
        var pair = CampaignObservationTestData.CreateApparentLocationDeltaPair(observer);
        var baseline = Project(pair.BaselineSnapshot, pair.BaselineContext, observer);
        var changed = Project(pair.ChangedSnapshot, pair.ChangedContext, observer);

        Assert.Equal(baseline.ContractVersion, changed.ContractVersion);
        Assert.Equal(baseline.PolicyId, changed.PolicyId);
        Assert.Equal(baseline.CampaignId, changed.CampaignId);
        Assert.Equal(baseline.StateVersion, changed.StateVersion);
        Assert.Equal(baseline.RulesetHash, changed.RulesetHash);
        Assert.Equal(baseline.ScenarioId, changed.ScenarioId);
        Assert.Equal(baseline.Observer, changed.Observer);
        Assert.Equal(baseline.Position, changed.Position);
        Assert.Equal(baseline.Weather, changed.Weather);
        Assert.Equal(baseline.Locations, changed.Locations);
        Assert.Equal(baseline.Edges, changed.Edges);
        Assert.Equal(baseline.OwnElements, changed.OwnElements);
        Assert.Equal(
            baseline.ApparentOpposingPresences.Select(value => (
                value.RepresentationId,
                value.ExertsZoc)),
            changed.ApparentOpposingPresences.Select(value => (
                value.RepresentationId,
                value.ExertsZoc)));
        Assert.Single(
            baseline.ApparentOpposingPresences.Zip(
                changed.ApparentOpposingPresences),
            pairValues => !string.Equals(
                pairValues.First.CurrentLocationId,
                pairValues.Second.CurrentLocationId,
                StringComparison.Ordinal));
        Assert.All(changed.ApparentOpposingPresences, value => Assert.False(value.ExertsZoc));
        Assert.NotEqual(
            CampaignObservationSerializer.SerializeCanonical(baseline),
            CampaignObservationSerializer.SerializeCanonical(changed));
    }

    [Fact]
    public void PublicObservationGraphDoesNotRetainAuthoritativeTypes()
    {
        Type[] prohibitedTypes =
        [
            typeof(CampaignSnapshot),
            typeof(CampaignSetupSnapshot),
            typeof(CampaignWorldSnapshot),
            typeof(CampaignElementState),
            typeof(CampaignElementOperationalState),
            typeof(CampaignMapRepresentationState),
            typeof(CampaignVehicleBreakdownState),
            typeof(ContentPackArtifact),
            typeof(ContentPackDefinition),
            typeof(ContentScenario),
            typeof(ContentCombatElement),
            typeof(ContentBreakdownVehicleCohort),
            typeof(ContentOrigin),
            typeof(ContentSourceCoordinate),
            typeof(RuleReference),
            typeof(RandomStreamState),
        ];
        Type[] observationTypes =
        [
            typeof(CampaignObservation),
            typeof(CampaignObservationPosition),
            typeof(CampaignObservationLocation),
            typeof(CampaignObservationEdgeFeature),
            typeof(CampaignObservationEdge),
            typeof(ObservedOwnElement),
            typeof(ObservedOwnVehicleBreakdownRisk),
            typeof(ObservedApparentPresence),
        ];

        Assert.All(observationTypes, observationType => Assert.All(
            observationType.GetProperties(),
            property => Assert.DoesNotContain(
                prohibitedTypes,
                prohibited => ContainsType(property.PropertyType, prohibited))));
    }

    private static CampaignObservation Project(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        LandSide observer)
    {
        var result = CampaignObservationProjector.Project(snapshot, context, observer);
        Assert.True(result.IsProjected);
        return Assert.IsType<CampaignObservation>(result.Observation);
    }

    private static bool ContainsType(Type candidate, Type prohibited) =>
        candidate == prohibited
        || candidate.IsAssignableTo(prohibited)
        || (candidate.IsGenericType
            && candidate.GetGenericArguments().Any(argument => ContainsType(argument, prohibited)));
}
