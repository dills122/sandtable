using System.Text;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignObservationPrivacyTests
{
    [Fact]
    public void OpponentOnlyChangesCannotAffectTheCompleteObservationOrCanonicalPayload()
    {
        var pair = CampaignObservationTestData.CreateOpponentOnlyPair();
        Assert.NotEqual(
            pair.BaselineContext.Artifact.Identity.Hash,
            pair.ChangedContext.Artifact.Identity.Hash);
        Assert.NotEqual(pair.BaselineSnapshot.Setup.SetupHash, pair.ChangedSnapshot.Setup.SetupHash);

        var baseline = Project(pair.BaselineSnapshot, pair.BaselineContext, LandSide.Axis);
        var changed = Project(pair.ChangedSnapshot, pair.ChangedContext, LandSide.Axis);
        var baselineBytes = CampaignObservationSerializer.SerializeCanonical(baseline);
        var changedBytes = CampaignObservationSerializer.SerializeCanonical(changed);

        Assert.Equal(baseline, changed);
        Assert.Equal(baseline.GetHashCode(), changed.GetHashCode());
        Assert.Equal(baselineBytes, changedBytes);
    }

    [Fact]
    public void OpponentSentinelsAndAuthoritativeMetadataAreAbsentFromObjectAndBytes()
    {
        var pair = CampaignObservationTestData.CreateOpponentOnlyPair();
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
