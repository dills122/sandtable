using System.Text;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignObservationPrivacyTests
{
    [Theory]
    [InlineData(LandSide.Axis)]
    [InlineData(LandSide.Commonwealth)]
    public void OpponentOnlyChangesRemainInvisibleAtEveryCheckpointThroughReserve(
        LandSide observer)
    {
        var pair = CampaignObservationTestData.CreateOpponentOnlyPair(observer);
        Assert.NotEqual(
            pair.BaselineContext.Artifact.Identity.Hash,
            pair.ChangedContext.Artifact.Identity.Hash);
        Assert.NotEqual(pair.BaselineSnapshot.Setup.SetupHash, pair.ChangedSnapshot.Setup.SetupHash);

        var baseline = StageEntryCampaignTestData.Advance(
            pair.BaselineSnapshot,
            pair.BaselineContext,
            InitiativeOrderChoice.ActLast);
        var changed = StageEntryCampaignTestData.Advance(
            pair.ChangedSnapshot,
            pair.ChangedContext,
            InitiativeOrderChoice.ActLast);

        Assert.Equal(10, baseline.Snapshots.Count);
        Assert.Equal(baseline.Snapshots.Count, changed.Snapshots.Count);

        for (var index = 0; index < baseline.Snapshots.Count; index++)
        {
            var baselineObservation = Project(
                baseline.Snapshots[index],
                pair.BaselineContext,
                observer);
            var changedObservation = Project(
                changed.Snapshots[index],
                pair.ChangedContext,
                observer);

            Assert.Equal(baselineObservation, changedObservation);
            Assert.Equal(baselineObservation.GetHashCode(),
                changedObservation.GetHashCode());
            Assert.Equal(
                CampaignObservationSerializer.SerializeCanonical(baselineObservation),
                CampaignObservationSerializer.SerializeCanonical(changedObservation));
        }
    }

    [Fact]
    public void OpponentSentinelsAndAuthoritativeMetadataAreAbsentFromObjectAndBytes()
    {
        var pair = CampaignObservationTestData.CreateOpponentOnlyPair(LandSide.Axis);
        var observation = Project(pair.ChangedSnapshot, pair.ChangedContext, LandSide.Axis);
        var payload = Encoding.UTF8.GetString(
            CampaignObservationSerializer.SerializeCanonical(observation));
        string[] prohibitedCanaries =
        [
            "enemy-sentinel",
            "commonwealth-element",
            "privacy.element",
            "privacy.placement",
            "\"baseCapabilityPointAllowance\":31",
            pair.ChangedContext.Artifact.Identity.Hash,
            pair.ChangedSnapshot.Setup.SetupHash,
            SandtableRandom.AlgorithmId,
            "nextByteCursor",
            "sourceCoordinate",
            "origin",
            "map-representation",
            "boundElementIds",
            "operationalState",
            "capabilityPointsExpended",
            "cohesionLevel",
        ];

        Assert.All(prohibitedCanaries, canary =>
            Assert.DoesNotContain(canary, payload, StringComparison.Ordinal));
        Assert.Equal(2, observation.OwnElements.Count);
        Assert.DoesNotContain(
            observation.OwnElements,
            element => element.ElementId.Contains("enemy", StringComparison.Ordinal));
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
            typeof(ContentPackArtifact),
            typeof(ContentPackDefinition),
            typeof(ContentScenario),
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
