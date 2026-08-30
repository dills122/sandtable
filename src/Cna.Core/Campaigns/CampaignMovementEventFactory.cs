using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignMovementEventFactory
{
    public static ElementMoved Create(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        MoveElement command)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(command);

        ValidateCommandShape(command);
        ValidateAuthority(snapshot, context, command);

        var sideId = CampaignSnapshotSerializer.FormatSide(command.ActingSide);
        var contentElement = context.Artifact.Definition.Elements.SingleOrDefault(element =>
            string.Equals(element.ElementId, command.ElementId, StringComparison.Ordinal)
            && string.Equals(element.SideId, sideId, StringComparison.Ordinal)
            && element.PlacementMode == ContentPlacementMode.Independent)
            ?? throw Unsupported("The moving element is not owned and independently placed.");
        var element = snapshot.World.Elements.SingleOrDefault(value => string.Equals(
            value.ElementId,
            command.ElementId,
            StringComparison.Ordinal))
            ?? throw Unsupported("The moving element is absent from the world.");
        var representation = snapshot.World.Representations.SingleOrDefault(value =>
            value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
            && value.BoundElementIds.Count == 1
            && string.Equals(
                value.BoundElementIds[0],
                command.ElementId,
                StringComparison.Ordinal))
            ?? throw Unsupported("The moving element has no unique independent representation.");

        if (!string.Equals(
                element.CurrentLocationId,
                command.OriginLocationId,
                StringComparison.Ordinal)
            || !string.Equals(
                representation.CurrentLocationId,
                command.OriginLocationId,
                StringComparison.Ordinal)
            || element.ReserveStatus != CampaignElementReserveStatus.None
            || element.OperationalState.CohesionLevel <= -26
            || element.OperationalState.LedgerGameTurn != snapshot.GameTurn
            || element.OperationalState.LedgerOperationStage != snapshot.OperationStage)
        {
            throw Unsupported("The moving element is not eligible at the claimed origin.");
        }

        var edge = context.Artifact.Definition.Edges.SingleOrDefault(value =>
            (string.Equals(value.FirstLocationId, command.OriginLocationId,
                 StringComparison.Ordinal)
             && string.Equals(value.SecondLocationId, command.DestinationLocationId,
                 StringComparison.Ordinal))
            || (string.Equals(value.SecondLocationId, command.OriginLocationId,
                 StringComparison.Ordinal)
                && string.Equals(value.FirstLocationId, command.DestinationLocationId,
                    StringComparison.Ordinal)))
            ?? throw Unsupported("The requested destination is not adjacent to the origin.");
        var destination = context.Artifact.Definition.Locations.SingleOrDefault(value =>
            string.Equals(
                value.LocationId,
                command.DestinationLocationId,
                StringComparison.Ordinal))
            ?? throw Unsupported("The requested destination is absent from the map.");

        var observationProjection = CampaignObservationProjector.Project(
            snapshot,
            context,
            command.ActingSide);
        if (!observationProjection.IsProjected)
        {
            throw Unsupported("The acting-side Movement observation cannot be projected.");
        }

        var observation = observationProjection.Observation!;
        if (observation.ApparentOpposingPresences.Any(value => value.ExertsZoc)
            || observation.ApparentOpposingPresences.Any(value =>
                string.Equals(value.CurrentLocationId, command.OriginLocationId,
                    StringComparison.Ordinal)
                || string.Equals(value.CurrentLocationId, command.DestinationLocationId,
                    StringComparison.Ordinal)))
        {
            throw Unsupported("Contact and enemy-ZOC Movement are unsupported.");
        }

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
        MovementActionRouteAdjustment? actionRouteAdjustment = null;
        var traversalStackingLimit = int.MaxValue;
        var crossedHexsides = new List<CampaignMovementHexsideCost>();
        var actionCrossedHexsides = new List<MovementActionHexsideCost>();
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
                actionRouteAdjustment = new MovementActionRouteAdjustment(
                    feature.FeatureId,
                    route.Value.CostKind,
                    route.Value.Amount);
                traversalStackingLimit = route.Value.TraversalStackingLimit;
                continue;
            }

            var direction = feature.DirectionFromLocationId switch
            {
                null => MovementHexsideDirection.Either,
                var from when string.Equals(
                    from,
                    command.OriginLocationId,
                    StringComparison.Ordinal) => MovementHexsideDirection.Up,
                var from when string.Equals(
                    from,
                    command.DestinationLocationId,
                    StringComparison.Ordinal) => MovementHexsideDirection.Down,
                _ => throw Unsupported("The edge feature has an unsupported direction."),
            };
            var hexside = RequireSupported(
                Cna1979Movement.LookupHexside(
                    feature.FeatureId,
                    direction,
                    mobility.MobilityId),
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
            actionCrossedHexsides.Add(new MovementActionHexsideCost(
                feature.FeatureId,
                direction,
                hexside.Value.AddedCost));
        }

        ValidateStacking(
            observation,
            command.DestinationLocationId,
            movingStacking.Value.StackingValue,
            terrain.Value.StoppingStackingLimit,
            traversalStackingLimit);

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

        var actionCost = new MovementActionCostBreakdown(
            destination.TerrainId,
            terrain.Value.Cost,
            actionRouteAdjustment,
            actionCrossedHexsides,
            totalCost);
        var candidate = new MoveElementAction(
            command.ElementId,
            command.OriginLocationId,
            command.DestinationLocationId,
            actionCost);
        if (!string.Equals(candidate.ActionId, command.CandidateId, StringComparison.Ordinal))
        {
            throw Unsupported("The Movement candidate identity is stale or forged.");
        }

        var eventCost = new CampaignMovementCost(
            destination.TerrainId,
            terrain.Value.Cost,
            terrain.Sources,
            routeAdjustment,
            crossedHexsides,
            totalCost);
        return new ElementMoved(
            snapshot.CampaignId,
            checked(snapshot.StateVersion + 1),
            snapshot.StateVersion,
            snapshot.SequencePosition.PositionId,
            snapshot.GameTurn,
            snapshot.OperationStage,
            command.ActingSide,
            command.ElementId,
            representation.RepresentationId,
            command.OriginLocationId,
            command.DestinationLocationId,
            mobility.MobilityId,
            mobility.Sources,
            eventCost,
            expendedBefore,
            expendedAfter,
            element.OperationalState.CohesionLevel,
            element.OperationalState.CohesionLevel,
            snapshot.SequencePosition);
    }

    private static void ValidateCommandShape(MoveElement command)
    {
        if (command.ContractVersion != 1 || !Enum.IsDefined(command.ActingSide))
        {
            throw Unsupported("The Movement command contract is unsupported.");
        }

        _ = ContentContractGuards.RequireStableId(
            command.ExpectedPositionId,
            nameof(command.ExpectedPositionId));
        _ = ContentContractGuards.RequireSha256(command.CandidateId, nameof(command.CandidateId));
        _ = ContentContractGuards.RequireStableId(command.ElementId, nameof(command.ElementId));
        var origin = ContentContractGuards.RequireStableId(
            command.OriginLocationId,
            nameof(command.OriginLocationId));
        var destination = ContentContractGuards.RequireStableId(
            command.DestinationLocationId,
            nameof(command.DestinationLocationId));
        if (string.Equals(origin, destination, StringComparison.Ordinal))
        {
            throw Unsupported("A Movement command must change location.");
        }
    }

    private static void ValidateAuthority(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        MoveElement command)
    {
        if (command.ExpectedStateVersion != snapshot.StateVersion)
        {
            throw Unsupported("Movement authority is stale.");
        }

        if (!CampaignSnapshotValidator.IsValid(snapshot, context)
            || !string.Equals(
                command.ExpectedPositionId,
                snapshot.SequencePosition.PositionId,
                StringComparison.Ordinal)
            || snapshot.OperationStage != 1
            || snapshot.PhaseId != LandPhaseIds.MovementAndCombat
            || snapshot.SegmentId != LandSegmentIds.Movement
            || snapshot.SequencePosition.ActorRole != LandActorRole.FirstActingSide
            || snapshot.SequencePosition.ActiveSide is not null
            || FirstActingSideResolver.Resolve(snapshot) != command.ActingSide)
        {
            throw Unsupported("Movement authority is not admitted.");
        }
    }

    private static void ValidateStacking(
        CampaignObservation observation,
        string destinationLocationId,
        int movingValue,
        int stoppingLimit,
        int traversalLimit)
    {
        var destinationValue = 0;
        foreach (var occupant in observation.OwnElements.Where(value => string.Equals(
            value.CurrentLocationId,
            destinationLocationId,
            StringComparison.Ordinal)))
        {
            var stacking = RequireSupported(
                Cna1979Movement.LookupStackingValue(occupant.OrganizationId),
                "A destination occupant has unsupported stacking.");
            destinationValue = checked(destinationValue + stacking.Value.StackingValue);
        }

        var resultingValue = checked(destinationValue + movingValue);
        if (resultingValue > stoppingLimit || resultingValue > traversalLimit)
        {
            throw Unsupported("The move exceeds a destination or traversal stacking limit.");
        }
    }

    private static MovementRuleLookupResult<T> RequireSupported<T>(
        MovementRuleLookupResult<T> lookup,
        string message)
    {
        if (!lookup.IsSupported)
        {
            throw Unsupported(message);
        }

        return lookup;
    }

    private static CapabilityPointAmount Scale(
        CapabilityPointAmount amount,
        CapabilityPointAmount factor) => new(
        checked(amount.Numerator * factor.Numerator),
        checked(amount.Denominator * factor.Denominator));

    private static InvalidOperationException Unsupported(string message) => new(message);
}
