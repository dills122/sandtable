using System.Reflection;
using Cna.Core.Actions;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Tests.Observations;

namespace Cna.Core.Tests.Actions;

public sealed class CampaignMovementActionDerivationTests
{
    [Fact]
    public void ValidMovementObservationDerivesCanonicalCompletionAndSupportedMoves()
    {
        var observation = CreateActiveMovementObservation();

        var candidates = CampaignMovementActionDerivation.Derive(observation);
        var moves = candidates.OfType<MoveElementAction>().ToArray();

        Assert.Equal(6, candidates.Count);
        Assert.Single(candidates.OfType<CompleteMovementSegmentAction>());
        Assert.Equal(5, moves.Length);
        Assert.Equal(
            candidates
                .OrderBy(candidate => candidate.Kind, StringComparer.Ordinal)
                .ThenBy(candidate => candidate.ActionId, StringComparer.Ordinal),
            candidates);

        var ridge = Assert.Single(moves, move =>
            move.ElementId == "commonwealth-element-a"
            && move.OriginLocationId == "east"
            && move.DestinationLocationId == "center");
        Assert.Equal("land.terrain.desert", ridge.CostBreakdown.DestinationTerrainId);
        Assert.Equal(new CapabilityPointAmount(4, 1),
            ridge.CostBreakdown.DestinationTerrainCost);
        Assert.Null(ridge.CostBreakdown.RouteAdjustment);
        var ridgeAddition = Assert.Single(ridge.CostBreakdown.CrossedHexsideCosts);
        Assert.Equal("land.edge.ridge", ridgeAddition.HexsideId);
        Assert.Equal(MovementHexsideDirection.Either, ridgeAddition.Direction);
        Assert.Equal(new CapabilityPointAmount(4, 1), ridgeAddition.AddedCost);
        Assert.Equal(new CapabilityPointAmount(8, 1), ridge.CostBreakdown.TotalCost);

        var road = Assert.Single(moves, move =>
            move.ElementId == "commonwealth-element-a"
            && move.OriginLocationId == "east"
            && move.DestinationLocationId == "north-east");
        Assert.Equal("land.terrain.clear", road.CostBreakdown.DestinationTerrainId);
        Assert.Equal(new CapabilityPointAmount(2, 1),
            road.CostBreakdown.DestinationTerrainCost);
        Assert.Equal("land.edge.road", road.CostBreakdown.RouteAdjustment!.RouteId);
        Assert.Equal(MovementRouteCostKind.Override,
            road.CostBreakdown.RouteAdjustment.CostKind);
        Assert.Equal(new CapabilityPointAmount(1, 2),
            road.CostBreakdown.RouteAdjustment.Amount);
        Assert.Empty(road.CostBreakdown.CrossedHexsideCosts);
        Assert.Equal(new CapabilityPointAmount(1, 2), road.CostBreakdown.TotalCost);

        var track = Assert.Single(moves, move =>
            move.ElementId == "commonwealth-element-b"
            && move.OriginLocationId == "south-east"
            && move.DestinationLocationId == "south");
        Assert.Equal(MovementRouteCostKind.ScaleUnderlying,
            track.CostBreakdown.RouteAdjustment!.CostKind);
        Assert.Equal(new CapabilityPointAmount(1, 2),
            track.CostBreakdown.RouteAdjustment.Amount);
        Assert.Equal(new CapabilityPointAmount(1, 1), track.CostBreakdown.TotalCost);
    }

    [Fact]
    public void ObservationVisibleIneligibilityExcludesTheAffectedDormantMove()
    {
        var baseline = CreateActiveMovementObservation();
        const string elementId = "commonwealth-element-a";
        const string originId = "east";
        const string destinationId = "north-east";
        var target = Assert.Single(
            CampaignMovementActionDerivation.Derive(baseline).OfType<MoveElementAction>(),
            move => IsTarget(move, elementId, originId, destinationId));
        Assert.NotNull(target);

        var targetElement = baseline.OwnElements.Single(element =>
            element.ElementId == elementId);
        var targetEdge = baseline.Edges.Single(edge => Connects(edge, originId, destinationId));
        var destination = baseline.Locations.Single(location =>
            location.LocationId == destinationId);
        var cases = new (string Name, CampaignObservation Observation)[]
        {
            ("wrong observer", Copy(baseline, observer: LandSide.Axis)),
            ("wrong position", Copy(baseline, position: CopyPosition(
                baseline.Position,
                segmentId: LandSegmentIds.BreakdownDetermination))),
            ("wrong actor", Copy(baseline, position: CopyPosition(
                baseline.Position,
                actorRole: LandActorRole.None,
                activeSide: null))),
            ("Reserve", CopyWithOwnElement(baseline, targetElement,
                CopyElement(targetElement, reserveStatus: CampaignObservationReserveStatus.ReserveI))),
            ("depleted Cohesion", CopyWithOwnElement(baseline, targetElement,
                CopyElement(targetElement, cohesionLevel: -26))),
            ("ledger mismatch", CopyWithOwnElement(baseline, targetElement,
                CopyElement(targetElement,
                    ledgerOperationStage: baseline.Position.OperationStage + 1))),
            ("nonadjacency", Copy(baseline, edges: baseline.Edges.Where(edge =>
                !ReferenceEquals(edge, targetEdge)).ToArray())),
            ("apparent destination occupancy", Copy(baseline,
                apparentOpposingPresences: baseline.ApparentOpposingPresences.Append(
                    new ObservedApparentPresence(
                        "map-representation.destination-canary",
                        destinationId,
                        false)).ToArray())),
            ("apparent origin occupancy", Copy(baseline,
                apparentOpposingPresences: baseline.ApparentOpposingPresences.Append(
                    new ObservedApparentPresence(
                        "map-representation.origin-canary",
                        originId,
                        false)).ToArray())),
            ("positive apparent ZOC at origin", Copy(baseline,
                apparentOpposingPresences: baseline.ApparentOpposingPresences.Append(
                    new ObservedApparentPresence(
                        "map-representation.origin-zoc-canary",
                        originId,
                        true)).ToArray())),
            ("positive apparent ZOC at destination", Copy(baseline,
                apparentOpposingPresences: baseline.ApparentOpposingPresences.Append(
                    new ObservedApparentPresence(
                        "map-representation.destination-zoc-canary",
                        destinationId,
                        true)).ToArray())),
            ("unsupported terrain", Copy(baseline, locations: baseline.Locations.Select(location =>
                ReferenceEquals(location, destination)
                    ? new CampaignObservationLocation(location.LocationId, "land.terrain.unsupported")
                    : location).ToArray())),
            ("unsupported route", Copy(baseline, edges: baseline.Edges.Select(edge =>
                ReferenceEquals(edge, targetEdge)
                    ? new CampaignObservationEdge(
                        edge.FirstLocationId,
                        edge.SecondLocationId,
                        [new CampaignObservationEdgeFeature("land.edge.route-unsupported", null)])
                    : edge).ToArray())),
            ("unsupported hexside", Copy(baseline, edges: baseline.Edges.Select(edge =>
                ReferenceEquals(edge, targetEdge)
                    ? new CampaignObservationEdge(
                        edge.FirstLocationId,
                        edge.SecondLocationId,
                        [new CampaignObservationEdgeFeature("land.edge.hexside-unsupported", null)])
                    : edge).ToArray())),
            ("duplicate hexside feature directions", Copy(baseline,
                edges: baseline.Edges.Select(edge =>
                    ReferenceEquals(edge, targetEdge)
                        ? new CampaignObservationEdge(
                            edge.FirstLocationId,
                            edge.SecondLocationId,
                            [
                                new CampaignObservationEdgeFeature(
                                    "land.edge.slope",
                                    originId),
                                new CampaignObservationEdgeFeature(
                                    "land.edge.slope",
                                    destinationId),
                            ])
                        : edge).ToArray())),
            ("unsupported organization", CopyWithOwnElement(baseline, targetElement,
                CopyElement(targetElement, organizationId: "land.organization.unsupported"))),
            ("over CPA", CopyWithOwnElement(baseline, targetElement,
                CopyElement(targetElement,
                    capabilityPointsExpended: new CapabilityPointAmount(
                        targetElement.BaseCapabilityPointAllowance,
                        1)))),
            ("traversal stacking", Copy(baseline, ownElements: baseline.OwnElements.Concat(
                Enumerable.Range(1, 5).Select(index => CopyElement(
                    targetElement,
                    elementId: $"commonwealth-road-stack-{index}",
                    currentLocationId: destinationId))).ToArray())),
        };

        foreach (var testCase in cases)
        {
            var derivedMoves = CampaignMovementActionDerivation.Derive(testCase.Observation)
                .OfType<MoveElementAction>();

            Assert.DoesNotContain(
                derivedMoves,
                move => IsTarget(move, elementId, originId, destinationId));
        }

        Assert.Empty(CampaignMovementActionDerivation.Derive(cases[0].Observation));
        Assert.Empty(CampaignMovementActionDerivation.Derive(cases[1].Observation));
        Assert.Empty(CampaignMovementActionDerivation.Derive(cases[2].Observation));

        var stoppingStacked = Copy(baseline, ownElements: baseline.OwnElements.Concat(
            Enumerable.Range(1, 6).Select(index => CopyElement(
                targetElement,
                elementId: $"commonwealth-terrain-stack-{index}",
                currentLocationId: "center"))).ToArray());
        Assert.DoesNotContain(
            CampaignMovementActionDerivation.Derive(stoppingStacked)
                .OfType<MoveElementAction>(),
            move => IsTarget(move, elementId, originId, "center"));
    }

    [Fact]
    public void AnyPositiveApparentZocFailsAllDormantMovesClosed()
    {
        var baseline = CreateActiveMovementObservation();
        var remoteZoc = Copy(
            baseline,
            apparentOpposingPresences: baseline.ApparentOpposingPresences.Append(
                new ObservedApparentPresence(
                    "map-representation.remote-zoc-canary",
                    "south-west",
                    true)).ToArray());

        var candidates = CampaignMovementActionDerivation.Derive(remoteZoc);

        Assert.Empty(candidates.OfType<MoveElementAction>());
        Assert.Single(candidates.OfType<CompleteMovementSegmentAction>());
    }

    [Theory]
    [InlineData(LandSide.Axis)]
    [InlineData(LandSide.Commonwealth)]
    public void ByteIdenticalObservationsFromDifferentHiddenAuthorityDeriveByteIdenticalVectors(
        LandSide observer)
    {
        var pair = CampaignObservationTestData.CreateApparentEquivalentPair(
            observer);
        var baselineSnapshot = CampaignObservationTestData.AdvanceThroughMovement(
            pair.BaselineSnapshot,
            pair.BaselineContext)[^1];
        var changedSnapshot = CampaignObservationTestData.AdvanceThroughMovement(
            pair.ChangedSnapshot,
            pair.ChangedContext)[^1];
        var baseline = Project(baselineSnapshot, pair.BaselineContext, observer);
        var changed = Project(changedSnapshot, pair.ChangedContext, observer);

        Assert.NotEqual(pair.BaselineContext.Artifact.Identity.Hash,
            pair.ChangedContext.Artifact.Identity.Hash);
        Assert.Equal(
            CampaignObservationSerializer.SerializeCanonical(baseline),
            CampaignObservationSerializer.SerializeCanonical(changed));

        Assert.Equal(SerializeVector(baseline), SerializeVector(changed));
    }

    [Fact]
    public void DerivationSurfaceDependsOnlyOnCampaignObservation()
    {
        var type = typeof(CampaignMovementActionDerivation);
        var derive = Assert.Single(
            type.GetMethods(BindingFlags.Static | BindingFlags.Public
                | BindingFlags.NonPublic | BindingFlags.DeclaredOnly),
            method => method.Name == "Derive");

        Assert.True(type.IsAbstract && type.IsSealed);
        Assert.Equal("Derive", derive.Name);
        Assert.True(derive.IsAssembly);
        Assert.Equal([typeof(CampaignObservation)], derive.GetParameters().Select(
            parameter => parameter.ParameterType));
        Assert.Equal(typeof(IReadOnlyList<CampaignActionCandidate>), derive.ReturnType);
    }

    private static CampaignObservation CreateActiveMovementObservation()
    {
        var pair = CampaignObservationTestData.CreateApparentEquivalentPair(
            LandSide.Commonwealth);
        var movement = CampaignObservationTestData.AdvanceThroughMovement(
            pair.BaselineSnapshot,
            pair.BaselineContext)[^1];
        return Project(movement, pair.BaselineContext, LandSide.Commonwealth);
    }

    private static CampaignObservation Project(
        Cna.Core.Campaigns.CampaignSnapshot snapshot,
        Cna.Core.Campaigns.CampaignContentContext context,
        LandSide observer)
    {
        var result = CampaignObservationProjector.Project(snapshot, context, observer);
        Assert.True(result.IsProjected);
        return Assert.IsType<CampaignObservation>(result.Observation);
    }

    private static byte[] SerializeVector(CampaignObservation observation)
    {
        var audience = observation.Observer switch
        {
            LandSide.Axis => CampaignActionAudience.Axis,
            LandSide.Commonwealth => CampaignActionAudience.Commonwealth,
            _ => throw new ArgumentOutOfRangeException(nameof(observation)),
        };
        var set = new CampaignLegalActionSet(
            observation.CampaignId,
            observation.StateVersion,
            observation.RulesetHash,
            observation.Position.PositionId,
            audience,
            CampaignMovementActionDerivation.Derive(observation));
        return CampaignLegalActionSerializer.Serialize(set);
    }

    private static bool IsTarget(
        MoveElementAction move,
        string elementId,
        string originId,
        string destinationId) =>
        move.ElementId == elementId
        && move.OriginLocationId == originId
        && move.DestinationLocationId == destinationId;

    private static bool Connects(
        CampaignObservationEdge edge,
        string first,
        string second) =>
        (edge.FirstLocationId == first && edge.SecondLocationId == second)
        || (edge.FirstLocationId == second && edge.SecondLocationId == first);

    private static CampaignObservation Copy(
        CampaignObservation source,
        LandSide? observer = null,
        CampaignObservationPosition? position = null,
        IReadOnlyList<CampaignObservationLocation>? locations = null,
        IReadOnlyList<CampaignObservationEdge>? edges = null,
        IReadOnlyList<ObservedOwnElement>? ownElements = null,
        IReadOnlyList<ObservedApparentPresence>? apparentOpposingPresences = null) => new(
            source.ContractVersion,
            source.PolicyId,
            source.CampaignId,
            source.StateVersion,
            source.RulesetHash,
            source.ScenarioId,
            observer ?? source.Observer,
            position ?? source.Position,
            source.Weather,
            locations ?? source.Locations,
            edges ?? source.Edges,
            ownElements ?? source.OwnElements,
            apparentOpposingPresences ?? source.ApparentOpposingPresences);

    private static CampaignObservation CopyWithOwnElement(
        CampaignObservation source,
        ObservedOwnElement replaced,
        ObservedOwnElement replacement) => Copy(
            source,
            ownElements: source.OwnElements.Select(element =>
                ReferenceEquals(element, replaced) ? replacement : element).ToArray());

    private static CampaignObservationPosition CopyPosition(
        CampaignObservationPosition source,
        string? segmentId = null,
        LandActorRole? actorRole = null,
        LandSide? activeSide = null) => new(
            source.PositionId,
            source.GameTurn,
            source.OperationStage,
            source.StageId,
            source.PhaseId,
            segmentId ?? source.SegmentId,
            source.StepId,
            actorRole ?? source.ActorRole,
            actorRole == LandActorRole.None ? activeSide : activeSide ?? source.ActiveSide,
            source.InitiativeHolder);

    private static ObservedOwnElement CopyElement(
        ObservedOwnElement source,
        string? elementId = null,
        string? organizationId = null,
        string? currentLocationId = null,
        CampaignObservationReserveStatus? reserveStatus = null,
        int? ledgerOperationStage = null,
        CapabilityPointAmount? capabilityPointsExpended = null,
        int? cohesionLevel = null) => new(
            elementId ?? source.ElementId,
            source.ParentFormationId,
            organizationId ?? source.OrganizationId,
            source.BaseCapabilityPointAllowance,
            currentLocationId ?? source.CurrentLocationId,
            reserveStatus ?? source.ReserveStatus,
            source.MobilityId,
            source.LedgerGameTurn,
            ledgerOperationStage ?? source.LedgerOperationStage,
            capabilityPointsExpended ?? source.CapabilityPointsExpended,
            cohesionLevel ?? source.CohesionLevel,
            source.VehicleBreakdownRisk);
}
