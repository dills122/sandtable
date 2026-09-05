using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record CampaignZocAuthorityProjection(
    IReadOnlyList<string> ControlledLocationIds,
    IReadOnlyList<string> SourceRepresentationIds);

internal static class CampaignElementMovedV2Factory
{
    private static readonly RuleReference ReactionAdjacencySource = new(
        "spi-1979-land-rules",
        "8.51");

    public static ElementMovedV2 Create(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        ElementMovedV2ReplayInput input)
    {
        ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(input);
        ValidateAuthority(prior, artifact, scenario, input);

        var definition = artifact.Definition.LegacyDefinition;
        var sideId = CampaignSnapshotSerializer.FormatSide(input.ActingSide);
        var contentElement = definition.Elements.SingleOrDefault(value =>
            string.Equals(value.ElementId, input.ElementId, StringComparison.Ordinal)
            && string.Equals(value.SideId, sideId, StringComparison.Ordinal)
            && value.PlacementMode == ContentPlacementMode.Independent)
            ?? throw Unsupported("The moving element is not owned and independently placed.");
        var combatFacts = artifact.Definition.ElementCombatFacts.Single(value =>
            string.Equals(value.ElementId, input.ElementId, StringComparison.Ordinal));
        if (!IsCombatElement(combatFacts))
        {
            throw Unsupported("Only a combat element can create an ElementMoved v2 event.");
        }

        var element = prior.World.Elements.SingleOrDefault(value => string.Equals(
            value.ElementId,
            input.ElementId,
            StringComparison.Ordinal))
            ?? throw Unsupported("The moving element is absent from the world.");
        var representation = prior.World.Representations.SingleOrDefault(value =>
            value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
            && value.BoundElementIds.Count == 1
            && string.Equals(value.BoundElementIds[0], input.ElementId, StringComparison.Ordinal))
            ?? throw Unsupported("The moving element has no unique independent representation.");
        ValidateMovingElement(prior, artifact, input, element, representation);

        var edge = FindEdge(definition, input.OriginLocationId, input.DestinationLocationId)
            ?? throw Unsupported("The requested destination is not adjacent to the origin.");
        var destination = definition.Locations.SingleOrDefault(value => string.Equals(
            value.LocationId,
            input.DestinationLocationId,
            StringComparison.Ordinal))
            ?? throw Unsupported("The requested destination is absent from the map.");
        var movement = CalculateMovement(
            prior,
            definition,
            contentElement,
            element,
            edge,
            destination,
            input.OriginLocationId,
            input.DestinationLocationId);
        var triggerAfter = new CampaignMapRepresentationState(
            representation.RepresentationId,
            input.DestinationLocationId,
            representation.BindingKind,
            representation.BoundElementIds);
        var postMoveWorld = ProjectMoveForAuthority(
            prior.World,
            element,
            triggerAfter,
            movement.ExpendedAfter);
        var reactingSide = Opposite(input.ActingSide);
        var enemyControlled = DeriveControlledLocationIds(
            postMoveWorld,
            artifact,
            scenario,
            reactingSide);
        if (enemyControlled.Contains(input.OriginLocationId, StringComparer.Ordinal)
            && enemyControlled.Contains(input.DestinationLocationId, StringComparer.Ordinal))
        {
            throw Unsupported("Movement cannot leave one enemy-controlled location for another.");
        }

        var movementEnded = enemyControlled.Contains(
            input.DestinationLocationId,
            StringComparer.Ordinal)
            ? new CampaignMovementEndedState(prior.CurrentPosition.SequencePosition!)
            : null;
        var window = CreateWindow(
            prior,
            artifact,
            scenario,
            input,
            triggerAfter,
            postMoveWorld,
            reactingSide);

        return new ElementMovedV2(
            prior.CampaignId,
            checked(prior.StateVersion + 1),
            prior.StateVersion,
            prior.CurrentPosition.SequencePosition!.PositionId,
            prior.CurrentPosition.SequencePosition.GameTurn,
            prior.CurrentPosition.SequencePosition.OperationStage,
            input.ActingSide,
            input.ElementId,
            representation.RepresentationId,
            input.OriginLocationId,
            input.DestinationLocationId,
            movement.MobilityId,
            movement.MobilitySources,
            movement.Cost,
            movement.ExpendedBefore,
            movement.ExpendedAfter,
            element.OperationalState.CohesionLevel,
            element.OperationalState.CohesionLevel,
            movementEnded,
            prior.CurrentPosition.SequencePosition,
            window);
    }

    private static CampaignReactionWindow? CreateWindow(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        ElementMovedV2ReplayInput input,
        CampaignMapRepresentationState triggerAfter,
        CampaignWorldSnapshotV5 postMoveWorld,
        LandSide reactingSide)
    {
        var adjacent = FindAdjacentCombatRepresentations(
            postMoveWorld,
            artifact,
            reactingSide,
            input.DestinationLocationId);
        if (adjacent.Length == 0)
        {
            return null;
        }

        var committedVersion = checked(prior.StateVersion + 1);
        var windowId = CampaignReactionIdentity.CreateWindow(
            prior.CampaignId,
            prior.RulesetHash,
            ElementMovedV2.CurrentContractVersion,
            committedVersion,
            triggerAfter,
            input.OriginLocationId,
            input.DestinationLocationId,
            reactingSide);
        var phasingControlled = DeriveControlledLocationIds(
            postMoveWorld,
            artifact,
            scenario,
            input.ActingSide);
        var elements = postMoveWorld.Elements.ToDictionary(
            value => value.ElementId,
            StringComparer.Ordinal);
        var facts = artifact.Definition.ElementCombatFacts.ToDictionary(
            value => value.ElementId,
            StringComparer.Ordinal);
        var opportunities = adjacent
            .Where(value => IsEligible(
                value,
                elements,
                facts,
                prior.CurrentPosition.SequencePosition!,
                phasingControlled))
            .Select(value => new CampaignFrozenReactionOpportunity(
                CampaignReactionIdentity.CreateOpportunity(windowId, value),
                value,
                new CampaignReactionAdjacencyEvidence(
                    value.CurrentLocationId,
                    input.DestinationLocationId,
                    true,
                    [ReactionAdjacencySource])))
            .ToArray();

        return new CampaignReactionWindow(
            windowId,
            committedVersion,
            input.ActingSide,
            reactingSide,
            new CampaignReactingPosition(prior.CurrentPosition.SequencePosition!),
            new CampaignReactionTriggerAuthority(
                ElementMovedV2.CurrentContractVersion,
                input.ElementId,
                triggerAfter,
                input.OriginLocationId,
                input.DestinationLocationId),
            new CampaignApparentReactionTrigger(
                triggerAfter.RepresentationId,
                input.OriginLocationId,
                input.DestinationLocationId),
            opportunities,
            [],
            null);
    }

    private static CampaignMapRepresentationState[]
        FindAdjacentCombatRepresentations(
            CampaignWorldSnapshotV5 world,
            ContentPackV5Artifact artifact,
            LandSide side,
            string destinationLocationId)
    {
        var sideId = CampaignSnapshotSerializer.FormatSide(side);
        var definition = artifact.Definition.LegacyDefinition;
        var content = definition.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var facts = artifact.Definition.ElementCombatFacts.ToDictionary(
            value => value.ElementId,
            StringComparer.Ordinal);
        return world.Representations
            .Where(representation => representation.BoundElementIds.Count > 0
                && representation.BoundElementIds.All(elementId =>
                    string.Equals(content[elementId].SideId, sideId, StringComparison.Ordinal))
                && representation.BoundElementIds.Any(elementId => IsCombatElement(facts[elementId]))
                && FindEdge(
                    definition,
                    representation.CurrentLocationId,
                    destinationLocationId) is not null)
            .OrderBy(value => value.RepresentationId, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsEligible(
        CampaignMapRepresentationState representation,
        Dictionary<string, CampaignElementStateV5> elements,
        Dictionary<string, ContentElementCombatFacts> facts,
        LandSequencePosition movement,
        IReadOnlyList<string> phasingControlled)
    {
        if (phasingControlled.Contains(representation.CurrentLocationId, StringComparer.Ordinal))
        {
            return false;
        }

        return representation.BoundElementIds.Count > 0
            && representation.BoundElementIds.All(elementId =>
        {
            var element = elements[elementId];
            return IsIndependentCombatUnit(facts[elementId])
                && element.ReserveStatus == CampaignElementReserveStatus.None
                && element.OperationalState.CohesionLevel > -26
                && element.OperationalState.LedgerGameTurn == movement.GameTurn
                && element.OperationalState.LedgerOperationStage == movement.OperationStage
                && (element.OperationalState.MovementEnded is null
                    || element.OperationalState.MovementEnded != new CampaignMovementEndedState(movement));
        });
    }

    internal static IReadOnlyList<string> DeriveControlledLocationIds(
        CampaignWorldSnapshotV5 world,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        LandSide side) => DeriveZocAuthority(world, artifact, scenario, side)
        .ControlledLocationIds;

    internal static CampaignZocAuthorityProjection DeriveZocAuthority(
        CampaignWorldSnapshotV5 world,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        LandSide side)
    {
        var sideId = CampaignSnapshotSerializer.FormatSide(side);
        var definition = artifact.Definition.LegacyDefinition;
        var content = definition.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var facts = artifact.Definition.ElementCombatFacts.ToDictionary(
            value => value.ElementId,
            StringComparer.Ordinal);
        var states = world.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        var candidates = new List<ZocControlCandidate>();
        var sourceRepresentationIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var locationGroup in world.Representations
                     .Where(value => value.BoundElementIds.Count > 0
                         && value.BoundElementIds.All(elementId => string.Equals(
                             content[elementId].SideId,
                             sideId,
                             StringComparison.Ordinal))
                         && value.BoundElementIds.Any(elementId =>
                             IsIndependentCombatUnit(facts[elementId])))
                     .GroupBy(value => value.CurrentLocationId, StringComparer.Ordinal))
        {
            var representations = locationGroup.ToArray();
            var representedElementIds = representations
                .SelectMany(value => value.BoundElementIds)
                .Where(elementId => IsIndependentCombatUnit(facts[elementId]))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            var aggregateStacking = representedElementIds.Aggregate(0, (sum, elementId) =>
            {
                var lookup = Cna1979Movement.LookupStackingValue(
                    content[elementId].OrganizationId);
                if (!lookup.IsSupported)
                {
                    throw Unsupported("A ZOC source has unsupported stacking authority.");
                }

                return checked(sum + lookup.Value.StackingValue);
            });
            var rawDefense = CampaignWorldV5CombatDerivation
                .CalculateRawDefensiveCloseAssaultPoints(
                    world,
                    artifact,
                    scenario,
                    representations.Select(value => value.RepresentationId));
            foreach (var representation in representations)
            {
                var representativeId = representation.BoundElementIds.First(elementId =>
                    IsIndependentCombatUnit(facts[elementId]));
                var representative = content[representativeId];
                var sourceFacts = new ZocSourceFacts(
                    facts[representativeId].CombatClassificationId,
                    aggregateStacking,
                    states[representativeId].OperationalState.CohesionLevel,
                    rawDefense.RawDefensiveCloseAssaultPoints,
                    false);
                var source = Cna1979Zoc.EvaluateSource(sourceFacts);
                if (!source.IsSupported)
                {
                    throw Unsupported("A ZOC source has unsupported qualification authority.");
                }

                if (source.IsQualified)
                {
                    sourceRepresentationIds.Add(representation.RepresentationId);
                }

                foreach (var edge in definition.Edges.Where(value =>
                             string.Equals(value.FirstLocationId, locationGroup.Key, StringComparison.Ordinal)
                             || string.Equals(value.SecondLocationId, locationGroup.Key, StringComparison.Ordinal)))
                {
                    var destinationId = string.Equals(
                        edge.FirstLocationId,
                        locationGroup.Key,
                        StringComparison.Ordinal)
                        ? edge.SecondLocationId
                        : edge.FirstLocationId;
                    var destination = definition.Locations.Single(value => string.Equals(
                        value.LocationId,
                        destinationId,
                        StringComparison.Ordinal));
                    var enterable = Cna1979Movement.LookupTerrain(
                        destination.TerrainId,
                        representative.MobilityId).IsSupported;
                    candidates.Add(new ZocControlCandidate(
                        destinationId,
                        sourceFacts,
                        new ZocProjectionFacts(
                            edge.Features.Select(value => value.FeatureId),
                            enterable)));
                }
            }
        }

        var controlled = Cna1979Zoc.DeriveControlledLocationIds(candidates);
        return new CampaignZocAuthorityProjection(
            controlled,
            Array.AsReadOnly(sourceRepresentationIds.Order(StringComparer.Ordinal).ToArray()));
    }

    internal static MovementResult CalculateMovement(
        CampaignSnapshotV10 prior,
        ContentPackDefinition definition,
        ContentCombatElement contentElement,
        CampaignElementStateV5 element,
        ContentHexEdge edge,
        ContentHex destination,
        string originLocationId,
        string destinationLocationId)
    {
        var mobility = Cna1979Movement.Mobility.SingleOrDefault(value => string.Equals(
            value.MobilityId,
            contentElement.MobilityId,
            StringComparison.Ordinal))
            ?? throw Unsupported("The element mobility is unsupported.");
        var terrain = RequireSupported(
            Cna1979Movement.LookupTerrain(destination.TerrainId, mobility.MobilityId),
            "The destination terrain is unsupported for this mobility.");
        var movingStacking = RequireSupported(
            Cna1979Movement.LookupStackingValue(contentElement.OrganizationId),
            "The moving element organization is unsupported for stacking.");
        CampaignMovementRouteAdjustment? routeAdjustment = null;
        var traversalStackingLimit = int.MaxValue;
        var crossedHexsides = new List<CampaignMovementHexsideCost>();
        var crossedFeatureIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var feature in edge.Features)
        {
            var route = Cna1979Movement.LookupRoute(feature.FeatureId, mobility.MobilityId);
            if (route.IsSupported)
            {
                if (feature.DirectionFromLocationId is not null || routeAdjustment is not null)
                {
                    throw Unsupported("The edge has an unsupported route combination.");
                }

                routeAdjustment = new CampaignMovementRouteAdjustment(
                    feature.FeatureId,
                    route.Value.CostKind,
                    route.Value.Amount,
                    route.Sources);
                traversalStackingLimit = route.Value.TraversalStackingLimit;
                continue;
            }

            var direction = feature.DirectionFromLocationId switch
            {
                null => MovementHexsideDirection.Either,
                var from when string.Equals(from, originLocationId, StringComparison.Ordinal) =>
                    MovementHexsideDirection.Up,
                var from when string.Equals(from, destinationLocationId, StringComparison.Ordinal) =>
                    MovementHexsideDirection.Down,
                _ => throw Unsupported("The edge feature has an unsupported direction."),
            };
            var hexside = RequireSupported(
                Cna1979Movement.LookupHexside(feature.FeatureId, direction, mobility.MobilityId),
                "The crossed hexside is unsupported for this mobility and direction.");
            if (!crossedFeatureIds.Add(feature.FeatureId))
            {
                throw Unsupported("A crossed hexside feature cannot be charged twice.");
            }

            crossedHexsides.Add(new CampaignMovementHexsideCost(
                feature.FeatureId,
                direction,
                hexside.Value.AddedCost,
                hexside.Sources));
        }

        var destinationStacking = prior.World.Elements
            .Where(value => string.Equals(
                value.CurrentLocationId,
                destinationLocationId,
                StringComparison.Ordinal))
            .Join(
                definition.Elements.Where(value => string.Equals(
                    value.SideId,
                    contentElement.SideId,
                    StringComparison.Ordinal)),
                state => state.ElementId,
                content => content.ElementId,
                (_, content) => RequireSupported(
                    Cna1979Movement.LookupStackingValue(content.OrganizationId),
                    "A destination occupant has unsupported stacking.").Value.StackingValue)
            .Aggregate(0, (sum, value) => checked(sum + value));
        var resultingStacking = checked(destinationStacking + movingStacking.Value.StackingValue);
        if (resultingStacking > terrain.Value.StoppingStackingLimit
            || resultingStacking > traversalStackingLimit)
        {
            throw Unsupported("The move exceeds a destination or traversal stacking limit.");
        }

        var adjustedTerrain = routeAdjustment switch
        {
            null => terrain.Value.Cost,
            { CostKind: MovementRouteCostKind.Override } => routeAdjustment.Amount,
            { CostKind: MovementRouteCostKind.ScaleUnderlying } => Scale(
                terrain.Value.Cost,
                routeAdjustment.Amount),
            _ => throw Unsupported("The route cost behavior is unsupported."),
        };
        var totalCost = crossedHexsides.Aggregate(
            adjustedTerrain,
            (current, value) => current + value.AddedCost);
        var expendedBefore = element.OperationalState.CapabilityPointsExpended;
        var expendedAfter = expendedBefore + totalCost;
        if (expendedAfter > new CapabilityPointAmount(
                contentElement.BaseCapabilityPointAllowance,
                1))
        {
            throw Unsupported("The move exceeds the element's Capability Point allowance.");
        }

        var cost = new CampaignMovementCost(
            destination.TerrainId,
            terrain.Value.Cost,
            terrain.Sources,
            routeAdjustment,
            crossedHexsides,
            totalCost);
        return new MovementResult(
            mobility.MobilityId,
            mobility.Sources,
            cost,
            ToActionCost(cost),
            expendedBefore,
            expendedAfter);
    }

    private static void ValidateAuthority(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        ElementMovedV2ReplayInput input)
    {
        var movement = prior.CurrentPosition.SequencePosition;
        var currentOrders = prior.OperationStageOrders
            .Where(order => movement is not null
                && order.GameTurn == movement.GameTurn
                && order.OperationStage == movement.OperationStage)
            .ToArray();
        if (!CampaignSnapshotV10Validator.IsValid(prior, artifact, scenario)
            || prior.ReactionWindow is not null
            || prior.CurrentPosition.Kind != CampaignPositionV10Kind.Sequence
            || movement is null
            || movement.OperationStage != 1
            || movement.PhaseId != LandPhaseIds.MovementAndCombat
            || movement.SegmentId != LandSegmentIds.Movement
            || movement.ActorRole != LandActorRole.FirstActingSide
            || currentOrders.Length != 1
            || movement.ActiveSide != currentOrders[0].FirstSide
            || input.ActingSide != currentOrders[0].FirstSide
            || !string.Equals(prior.CampaignId, input.CampaignId, StringComparison.Ordinal)
            || prior.StateVersion != input.PriorStateVersion
            || !string.Equals(movement.PositionId, input.FromPositionId, StringComparison.Ordinal))
        {
            throw Unsupported("Movement v2 authority is not admitted.");
        }

        _ = ContentContractGuards.RequireStableId(input.ElementId, nameof(input.ElementId));
        var origin = ContentContractGuards.RequireStableId(
            input.OriginLocationId,
            nameof(input.OriginLocationId));
        var destination = ContentContractGuards.RequireStableId(
            input.DestinationLocationId,
            nameof(input.DestinationLocationId));
        if (string.Equals(origin, destination, StringComparison.Ordinal))
        {
            throw Unsupported("A Movement command must change location.");
        }
    }

    private static void ValidateMovingElement(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ElementMovedV2ReplayInput input,
        CampaignElementStateV5 element,
        CampaignMapRepresentationState representation)
    {
        var movement = prior.CurrentPosition.SequencePosition!;
        var sideId = CampaignSnapshotSerializer.FormatSide(input.ActingSide);
        var contentById = artifact.Definition.LegacyDefinition.Elements.ToDictionary(
            value => value.ElementId,
            StringComparer.Ordinal);
        if (!string.Equals(element.CurrentLocationId, input.OriginLocationId, StringComparison.Ordinal)
            || !string.Equals(representation.CurrentLocationId, input.OriginLocationId, StringComparison.Ordinal)
            || element.ReserveStatus != CampaignElementReserveStatus.None
            || element.OperationalState.CohesionLevel <= -26
            || element.OperationalState.LedgerGameTurn != movement.GameTurn
            || element.OperationalState.LedgerOperationStage != movement.OperationStage
            || element.OperationalState.MovementEnded == new CampaignMovementEndedState(movement)
            || prior.World.Representations.Any(value =>
                (string.Equals(
                    value.CurrentLocationId,
                    input.OriginLocationId,
                    StringComparison.Ordinal)
                 || string.Equals(
                    value.CurrentLocationId,
                    input.DestinationLocationId,
                    StringComparison.Ordinal))
                && value.BoundElementIds.Any(elementId => !string.Equals(
                    contentById[elementId].SideId,
                    sideId,
                    StringComparison.Ordinal))))
        {
            throw Unsupported("The moving element is not eligible at the claimed origin.");
        }
    }

    internal static CampaignWorldSnapshotV5 ProjectMoveForAuthority(
        CampaignWorldSnapshotV5 world,
        CampaignElementStateV5 movedElement,
        CampaignMapRepresentationState movedRepresentation,
        CapabilityPointAmount expendedAfter) => new(
        CampaignWorldSnapshotV5.CurrentContractVersion,
        world.Elements.Select(value => string.Equals(
                value.ElementId,
                movedElement.ElementId,
                StringComparison.Ordinal)
            ? new CampaignElementStateV5(
                value.ElementId,
                movedRepresentation.CurrentLocationId,
                value.ReserveStatus,
                new CampaignElementOperationalStateV5(
                    value.OperationalState.LedgerGameTurn,
                    value.OperationalState.LedgerOperationStage,
                    expendedAfter,
                    value.OperationalState.CohesionLevel,
                    value.OperationalState.VehicleBreakdownState,
                    value.OperationalState.MovementEnded),
                value.Components)
            : value),
        world.Representations.Select(value => string.Equals(
                value.RepresentationId,
                movedRepresentation.RepresentationId,
                StringComparison.Ordinal)
            ? movedRepresentation
            : value));

    private static bool IsCombatElement(ContentElementCombatFacts facts) =>
        Cna1979Combat.FindClassification(facts.CombatClassificationId)?.Kind is
            ZocCombatClassificationKind.CombatUnit or
            ZocCombatClassificationKind.Headquarters;

    private static bool IsIndependentCombatUnit(ContentElementCombatFacts facts) =>
        Cna1979Combat.FindClassification(facts.CombatClassificationId)?.Kind ==
            ZocCombatClassificationKind.CombatUnit;

    internal static ContentHexEdge? FindEdge(
        ContentPackDefinition definition,
        string first,
        string second) => definition.Edges.SingleOrDefault(value =>
        (string.Equals(value.FirstLocationId, first, StringComparison.Ordinal)
         && string.Equals(value.SecondLocationId, second, StringComparison.Ordinal))
        || (string.Equals(value.FirstLocationId, second, StringComparison.Ordinal)
            && string.Equals(value.SecondLocationId, first, StringComparison.Ordinal)));

    private static LandSide Opposite(LandSide side) => side switch
    {
        LandSide.Axis => LandSide.Commonwealth,
        LandSide.Commonwealth => LandSide.Axis,
        _ => throw Unsupported("The acting side is unsupported."),
    };

    private static MovementRuleLookupResult<T> RequireSupported<T>(
        MovementRuleLookupResult<T> lookup,
        string message) => lookup.IsSupported ? lookup : throw Unsupported(message);

    private static CapabilityPointAmount Scale(
        CapabilityPointAmount amount,
        CapabilityPointAmount factor) => new(
        checked(amount.Numerator * factor.Numerator),
        checked(amount.Denominator * factor.Denominator));

    private static MovementActionCostBreakdown ToActionCost(CampaignMovementCost cost) => new(
        cost.DestinationTerrainId,
        cost.DestinationTerrainCost,
        cost.RouteAdjustment is null
            ? null
            : new MovementActionRouteAdjustment(
                cost.RouteAdjustment.RouteId,
                cost.RouteAdjustment.CostKind,
                cost.RouteAdjustment.Amount),
        cost.CrossedHexsideCosts.Select(value => new MovementActionHexsideCost(
            value.HexsideId,
            value.Direction,
            value.AddedCost)).ToArray(),
        cost.TotalCost);

    private static InvalidOperationException Unsupported(string message) => new(message);

    internal sealed record MovementResult(
        string MobilityId,
        IReadOnlyList<RuleReference> MobilitySources,
        CampaignMovementCost Cost,
        MovementActionCostBreakdown ActionCost,
        CapabilityPointAmount ExpendedBefore,
        CapabilityPointAmount ExpendedAfter);
}
