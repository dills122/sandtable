using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignElementMovedV3Factory
{
    private static readonly RuleReference ReactionAdjacencySource = new(
        "spi-1979-land-rules",
        "8.51");

    public static ElementMovedV3 Create(
        CampaignSnapshotV11 prior,
        ContentPackV6Artifact artifact,
        ContentScenario scenario,
        ElementMovedV3ReplayInput input)
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
        if (!IsCombatElement(combatFacts)
            && combatFacts.CombatClassificationId != Cna1979Combat.TruckConvoyClassificationId)
        {
            throw Unsupported("The moving element classification is outside the certified Breakdown profile.");
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
        var weather = CampaignSnapshotV11Validator.ApplicableWeather(prior, artifact, input.DestinationLocationId);
        CampaignBreakdownStep[] accounting = contentElement.BreakdownVehicleCohort is null ? [] :
            [CampaignBreakdownAccounting.Calculate(contentElement.BreakdownVehicleCohort,
                element.OperationalState.VehicleBreakdownState!, edge, destination, input.OriginLocationId, weather)];
        var postMoveWorld = ProjectMoveForAuthority(
            prior.World, element, triggerAfter, movement.ExpendedAfter, accounting, null);
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
            ? CampaignMovementEndedState.CreateForBreakdown(prior.CurrentPosition.SequencePosition!)
            : null;
        var window = IsCombatElement(combatFacts) ? CreateWindow(
            prior,
            artifact,
            scenario,
            input,
            triggerAfter,
            postMoveWorld,
            reactingSide) : null;
        if (movementEnded is not null)
            postMoveWorld = ProjectMoveForAuthority(prior.World, element, triggerAfter,
                movement.ExpendedAfter, accounting, movementEnded);
        var committedVersion = checked(prior.StateVersion + 1);
        var route = prior.BreakdownFlow switch
        {
            CampaignBreakdownFlow.Idle => CampaignBreakdownRoute.Create(prior.CampaignId, prior.RulesetHash,
                committedVersion, element.ElementId, representation.RepresentationId, input.ActingSide,
                input.OriginLocationId, input.DestinationLocationId,
                contentElement.BreakdownVehicleCohort is null ? [] : [contentElement.BreakdownVehicleCohort.CohortId]),
            CampaignBreakdownFlow.Moving moving => moving.Route.AtLocation(input.DestinationLocationId),
            _ => throw Unsupported("An ordinary move requires an idle or moving phasing route."),
        };
        CampaignBreakdownStop? stop = null;
        var reason = movementEnded is not null ? CampaignBreakdownStopReason.MovementEnded
            : movement.ExpendedAfter == new CapabilityPointAmount(contentElement.BaseCapabilityPointAllowance, 1)
                ? CampaignBreakdownStopReason.CpExhausted : (CampaignBreakdownStopReason?)null;
        if (reason is not null)
        {
            var ledger = postMoveWorld.Elements.Single(value => value.ElementId == element.ElementId)
                .OperationalState.VehicleBreakdownState;
            CampaignBreakdownCheckInput[] inputs = ledger is null ? [] :
                [new CampaignBreakdownCheckInput(ledger.CohortId, contentElement.BreakdownVehicleCohort!.VehicleTypeId,
                    contentElement.BreakdownVehicleCohort.ProfileId, ledger.WorkingPointCount,
                    ledger.CumulativeBreakdownPoints, ledger.SandstormAttributedBreakdownPoints,
                    ledger.HighestEffectiveCheckedBandId)];
            stop = CampaignBreakdownStop.Create(prior.CampaignId, prior.RulesetHash, committedVersion,
                route, reason.Value, weather, inputs);
        }
        CampaignBreakdownFlow flow = window is not null
            ? new CampaignBreakdownFlow.Reacting(stop is null
                ? new CampaignPhasingContinuation.ResumeRoute(route)
                : new CampaignPhasingContinuation.ResolveStop(stop), null)
            : stop is null ? new CampaignBreakdownFlow.Moving(route) : new CampaignBreakdownFlow.PhasingStop(stop);
        var position = window is not null ? CampaignPositionV11.FromReaction(window.ReactingPosition)
            : stop is null ? prior.CurrentPosition : CampaignPositionV11.FromBreakdownStop(prior.CurrentPosition.SequenceContext);
        var projected = new CampaignSnapshotV11(11, prior.CampaignId, committedVersion, prior.RulesetHash, prior.Setup,
            postMoveWorld, prior.InitiativeHolder, prior.OperationStageOrders, prior.OperationStageWeather,
            prior.RandomState, position, window, flow);
        if (!CampaignSnapshotV11Validator.IsValid(projected, artifact, scenario))
            throw Unsupported("The ordinary move would leave the certified Breakdown profile.");

        return new ElementMovedV3(
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
            window,
            prior.RulesetHash,
            accounting,
            flow);
    }

    private static CampaignReactionWindow? CreateWindow(
        CampaignSnapshotV11 prior,
        ContentPackV6Artifact artifact,
        ContentScenario scenario,
        ElementMovedV3ReplayInput input,
        CampaignMapRepresentationState triggerAfter,
        CampaignWorldSnapshotV6 postMoveWorld,
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
            ElementMovedV3.CurrentContractVersion,
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
            CampaignReactingPosition.CreateForBreakdown(prior.CurrentPosition.SequencePosition!),
            CampaignReactionTriggerAuthority.CreateForBreakdown(
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
            CampaignWorldSnapshotV6 world,
            ContentPackV6Artifact artifact,
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
                    || element.OperationalState.MovementEnded != CampaignMovementEndedState.CreateForBreakdown(movement));
        });
    }

    internal static IReadOnlyList<string> DeriveControlledLocationIds(
        CampaignWorldSnapshotV6 world, ContentPackV6Artifact artifact, ContentScenario scenario, LandSide side)
    {
        if (!Enum.IsDefined(side) || !CampaignWorldV6Validator.IsValid(world, artifact, scenario))
            throw Unsupported("ZOC authority requires the certified Breakdown profile.");
        // This profile admits at most one combat/HQ battalion at each own location.
        // A positive ZOC source requires aggregate stacking greater than one (10.11).
        // Broader dormant worlds must receive their own versioned certification path.
        return [];
    }

    internal static MovementResult CalculateMovement(
        CampaignSnapshotV11 prior,
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
        CampaignSnapshotV11 prior,
        ContentPackV6Artifact artifact,
        ContentScenario scenario,
        ElementMovedV3ReplayInput input)
    {
        var movement = prior.CurrentPosition.SequencePosition;
        var currentOrders = prior.OperationStageOrders
            .Where(order => movement is not null
                && order.GameTurn == movement.GameTurn
                && order.OperationStage == movement.OperationStage)
            .ToArray();
        if (!CampaignSnapshotV11Validator.IsValid(prior, artifact, scenario)
            || prior.ReactionWindow is not null
            || prior.BreakdownFlow is not (CampaignBreakdownFlow.Idle or CampaignBreakdownFlow.Moving)
            || (prior.BreakdownFlow is CampaignBreakdownFlow.Moving moving
                && (moving.Route.ElementId != input.ElementId
                    || moving.Route.CurrentLocationId != input.OriginLocationId))
            || prior.CurrentPosition.Kind != CampaignPositionV11Kind.Sequence
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
            throw Unsupported("Movement v3 authority is not admitted.");
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
        CampaignSnapshotV11 prior,
        ContentPackV6Artifact artifact,
        ElementMovedV3ReplayInput input,
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
            || element.OperationalState.VehicleBreakdownState is { WorkingPointCount: 0 }
            || element.OperationalState.CohesionLevel <= -26
            || element.OperationalState.LedgerGameTurn != movement.GameTurn
            || element.OperationalState.LedgerOperationStage != movement.OperationStage
            || element.OperationalState.MovementEnded == CampaignMovementEndedState.CreateForBreakdown(movement)
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

    internal static CampaignWorldSnapshotV6 ProjectMoveForAuthority(
        CampaignWorldSnapshotV6 world,
        CampaignElementStateV5 movedElement,
        CampaignMapRepresentationState movedRepresentation,
        CapabilityPointAmount expendedAfter,
        IReadOnlyList<CampaignBreakdownStep> breakdownAccounting,
        CampaignMovementEndedState? endedAfter) => new(
        CampaignWorldSnapshotV6.CurrentContractVersion,
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
                    ApplyBreakdown(value.OperationalState.VehicleBreakdownState, breakdownAccounting),
                    endedAfter),
                value.Components)
            : value),
        world.Representations.Select(value => string.Equals(
                value.RepresentationId,
                movedRepresentation.RepresentationId,
                StringComparison.Ordinal)
            ? movedRepresentation
            : value),
        world.BrokenVehicleLots);

    private static CampaignVehicleBreakdownState? ApplyBreakdown(CampaignVehicleBreakdownState? ledger,
        IReadOnlyList<CampaignBreakdownStep> accounting)
    {
        ArgumentNullException.ThrowIfNull(accounting);
        if (ledger is null && accounting.Count == 0) return null;
        if (ledger is null || accounting.Count != 1)
            throw Unsupported("Move accounting must exactly match the immutable cohort membership.");
        return CampaignBreakdownAccounting.Apply(ledger, accounting[0]);
    }

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
