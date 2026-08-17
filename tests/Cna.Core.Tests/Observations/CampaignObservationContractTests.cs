using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignObservationContractTests
{
    [Fact]
    public void PositionCopiesOnlySourceFreeTurnFactsAndComparesStructurally()
    {
        var first = new CampaignObservationPosition(
            "land.position.initiative-determination",
            1,
            0,
            "land.stage.initiative-determination",
            "land.phase.initiative-determination",
            null,
            null,
            LandActorRole.None,
            null,
            null);
        var equivalent = new CampaignObservationPosition(
            "land.position.initiative-determination",
            1,
            0,
            "land.stage.initiative-determination",
            "land.phase.initiative-determination",
            null,
            null,
            LandActorRole.None,
            null,
            null);

        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
        Assert.DoesNotContain(
            first.GetType().GetProperties(),
            property => property.Name.Contains("Source", StringComparison.Ordinal));
    }

    [Fact]
    public void PositionRejectsInvalidStageAndActorSideCombinations()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePosition(operationStage: 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePosition(actorRole: (LandActorRole)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePosition(activeSide: (LandSide)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreatePosition(initiativeHolder: (LandSide)99));
        Assert.Throws<ArgumentException>(() => CreatePosition(
            actorRole: LandActorRole.None,
            activeSide: LandSide.Axis));
        Assert.Throws<ArgumentException>(() => CreatePosition(
            actorRole: LandActorRole.Commonwealth,
            activeSide: LandSide.Axis));
    }

    [Fact]
    public void EdgeCanonicalizesEndpointsAndDefensivelyCopiesSortedFeatures()
    {
        var features = new List<CampaignObservationEdgeFeature>
        {
            new("land.edge.slope", "west"),
            new("land.edge.road", null),
        };
        var first = new CampaignObservationEdge("west", "east", features);
        var equivalent = new CampaignObservationEdge(
            "east",
            "west",
            features.AsEnumerable().Reverse().ToArray());

        features.Clear();

        Assert.Equal("east", first.FirstLocationId);
        Assert.Equal("west", first.SecondLocationId);
        Assert.Equal(
            ["land.edge.road", "land.edge.slope"],
            first.Features.Select(feature => feature.FeatureId));
        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
    }

    [Fact]
    public void EdgeRejectsDuplicateFeaturesAndInvalidDirection()
    {
        Assert.Throws<ArgumentException>(() => new CampaignObservationEdge(
            "east",
            "east",
            []));
        Assert.Throws<ArgumentException>(() => new CampaignObservationEdge(
            "east",
            "west",
            [new CampaignObservationEdgeFeature("land.edge.slope", "north")]));
        Assert.Throws<ArgumentException>(() => new CampaignObservationEdge(
            "east",
            "west",
            [
                new CampaignObservationEdgeFeature("land.edge.road", null),
                new CampaignObservationEdgeFeature("land.edge.road", null),
            ]));
    }

    [Fact]
    public void ScalarValuesRejectUnstableIdsAndInvalidCapability()
    {
        Assert.Throws<ArgumentException>(() => new CampaignObservationLocation(
            "Invalid ID",
            "land.terrain.clear"));
        Assert.Throws<ArgumentException>(() => new CampaignObservationEdgeFeature(
            "land.edge.road",
            "Invalid ID"));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ObservedOwnElement(
            "axis-element-a",
            "axis-formation",
            "land.organization.battalion",
            0,
            "west"));
    }

    [Fact]
    public void AggregateCopiesCanonicalCollectionsAndComparesStructurally()
    {
        var locations = new List<CampaignObservationLocation>
        {
            new("west", "land.terrain.clear"),
            new("east", "land.terrain.clear"),
            new("north", "land.terrain.clear"),
        };
        var edges = new List<CampaignObservationEdge>
        {
            new(
                "west",
                "east",
                [
                    new CampaignObservationEdgeFeature("land.edge.slope", "west"),
                    new CampaignObservationEdgeFeature("land.edge.road", null),
                ]),
            new(
                "west",
                "north",
                [new CampaignObservationEdgeFeature("land.edge.track", null)]),
        };
        var ownElements = new List<ObservedOwnElement>
        {
            new(
                "axis-element-a",
                "axis-formation",
                "land.organization.battalion",
                20,
                "west"),
            new(
                "axis-element-b",
                "axis-formation",
                "land.organization.battalion",
                10,
                "north"),
        };
        var first = CreateObservation(locations, edges, ownElements);
        var equivalent = CreateObservation(
            locations.AsEnumerable().Reverse().ToArray(),
            [
                new CampaignObservationEdge(
                    "north",
                    "west",
                    [new CampaignObservationEdgeFeature("land.edge.track", null)]),
                new CampaignObservationEdge(
                    "east",
                    "west",
                    [
                        new CampaignObservationEdgeFeature("land.edge.road", null),
                        new CampaignObservationEdgeFeature("land.edge.slope", "west"),
                    ]),
            ],
            ownElements.AsEnumerable().Reverse().ToArray());

        locations.Clear();
        edges.Clear();
        ownElements.Clear();

        Assert.Equal(
            ["east", "north", "west"],
            first.Locations.Select(location => location.LocationId));
        Assert.Equal(2, first.Edges.Count);
        Assert.Equal(2, first.OwnElements.Count);
        Assert.Equal(first, equivalent);
        Assert.Equal(first.GetHashCode(), equivalent.GetHashCode());
        Assert.Equal(
            CampaignObservationSerializer.SerializeCanonical(first),
            CampaignObservationSerializer.SerializeCanonical(equivalent));
    }

    [Fact]
    public void AggregateRejectsNoncanonicalIdentityDuplicatesAndMissingReferences()
    {
        var locations = new[]
        {
            new CampaignObservationLocation("east", "land.terrain.clear"),
            new CampaignObservationLocation("west", "land.terrain.clear"),
        };

        Assert.Throws<ArgumentException>(() => CreateObservation(
            locations,
            [],
            [],
            rulesetHash: new string('0', 64)));
        Assert.Throws<ArgumentException>(() => CreateObservation(
            [locations[0], locations[0]],
            [],
            []));
        Assert.Throws<ArgumentException>(() => CreateObservation(
            locations,
            [new CampaignObservationEdge("east", "north", [])],
            []));
        Assert.Throws<ArgumentException>(() => CreateObservation(
            locations,
            [],
            [new ObservedOwnElement(
                "axis-element-a",
                "axis-formation",
                "land.organization.battalion",
                20,
                "north")]));
    }

    [Fact]
    public void ObservationValuesCannotBeConstructedByExternalCallers()
    {
        Type[] valueTypes =
        [
            typeof(CampaignObservation),
            typeof(CampaignObservationPosition),
            typeof(CampaignObservationLocation),
            typeof(CampaignObservationEdgeFeature),
            typeof(CampaignObservationEdge),
            typeof(ObservedOwnElement),
        ];

        Assert.All(valueTypes, valueType => Assert.Empty(valueType.GetConstructors()));
    }

    [Fact]
    public void RejectionResultRejectsNoneAndUndefinedReasons()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CampaignObservationProjectionResult.Rejected(
                CampaignObservationRejectionReason.None));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CampaignObservationProjectionResult.Rejected(
                (CampaignObservationRejectionReason)99));
    }

    private static CampaignObservationPosition CreatePosition(
        int operationStage = 0,
        LandActorRole actorRole = LandActorRole.None,
        LandSide? activeSide = null,
        LandSide? initiativeHolder = null) => new(
            "land.position.test",
            1,
            operationStage,
            "land.stage.test",
            "land.phase.test",
            null,
            null,
            actorRole,
            activeSide,
            initiativeHolder);

    private static CampaignObservation CreateObservation(
        IReadOnlyList<CampaignObservationLocation> locations,
        IReadOnlyList<CampaignObservationEdge> edges,
        IReadOnlyList<ObservedOwnElement> ownElements,
        string? rulesetHash = null) => new(
            CampaignObservation.CurrentContractVersion,
            CampaignObservation.CurrentPolicyId,
            "campaign-1",
            1,
            rulesetHash ?? Cna1979Ruleset.Manifest.Hash,
            "movement-contact-lab",
            LandSide.Axis,
            CreatePosition(),
            locations,
            edges,
            ownElements);
}
