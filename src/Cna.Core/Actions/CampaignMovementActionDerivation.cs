using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Actions;

internal static class CampaignMovementActionDerivation
{
    internal static IReadOnlyList<CampaignActionCandidate> Derive(
        CampaignObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (!IsSupportedMovementPosition(observation))
        {
            return Array.Empty<CampaignActionCandidate>();
        }

        var candidates = observation.OwnElements
            .SelectMany(element => DeriveMoves(observation, element))
            .Cast<CampaignActionCandidate>()
            .Append(new CompleteMovementSegmentAction())
            .OrderBy(candidate => candidate.Kind, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.ActionId, StringComparer.Ordinal)
            .ToArray();
        return Array.AsReadOnly(candidates);
    }

    private static bool IsSupportedMovementPosition(CampaignObservation observation) =>
        observation.Position.OperationStage == 1
        && observation.Position.StageId == LandStageIds.Operation
        && observation.Position.PhaseId == LandPhaseIds.MovementAndCombat
        && observation.Position.SegmentId == LandSegmentIds.Movement
        && observation.Position.ActorRole == LandActorRole.FirstActingSide
        && observation.Position.ActiveSide == observation.Observer;

    private static IEnumerable<MoveElementAction> DeriveMoves(
        CampaignObservation observation,
        ObservedOwnElement element)
    {
        if (element.ReserveStatus != CampaignObservationReserveStatus.None
            || element.CohesionLevel <= -26
            || element.LedgerGameTurn != observation.Position.GameTurn
            || element.LedgerOperationStage != observation.Position.OperationStage)
        {
            yield break;
        }

        foreach (var edge in observation.Edges.Where(value =>
            value.FirstLocationId == element.CurrentLocationId
            || value.SecondLocationId == element.CurrentLocationId))
        {
            var destinationId = edge.FirstLocationId == element.CurrentLocationId
                ? edge.SecondLocationId
                : edge.FirstLocationId;
            var candidate = TryCreateMove(observation, element, edge, destinationId);
            if (candidate is not null)
            {
                yield return candidate;
            }
        }
    }

    private static MoveElementAction? TryCreateMove(
        CampaignObservation observation,
        ObservedOwnElement element,
        CampaignObservationEdge edge,
        string destinationId)
    {
        if (observation.ApparentOpposingPresences.Any(presence => presence.ExertsZoc)
            || observation.ApparentOpposingPresences.Any(presence =>
                presence.CurrentLocationId == element.CurrentLocationId
                || presence.CurrentLocationId == destinationId))
        {
            return null;
        }

        var destination = observation.Locations.SingleOrDefault(location =>
            location.LocationId == destinationId);
        if (destination is null)
        {
            return null;
        }

        var terrain = Cna1979Movement.LookupTerrain(destination.TerrainId, element.MobilityId);
        var movingStacking = Cna1979Movement.LookupStackingValue(element.OrganizationId);
        if (!terrain.IsSupported || !movingStacking.IsSupported)
        {
            return null;
        }

        MovementActionRouteAdjustment? routeAdjustment = null;
        var routeTraversalLimit = int.MaxValue;
        var hexsideCosts = new List<MovementActionHexsideCost>();
        foreach (var feature in edge.Features)
        {
            var route = Cna1979Movement.LookupRoute(feature.FeatureId, element.MobilityId);
            if (route.IsSupported)
            {
                if (feature.DirectionFromLocationId is not null || routeAdjustment is not null)
                {
                    return null;
                }

                routeAdjustment = new MovementActionRouteAdjustment(
                    feature.FeatureId,
                    route.Value.CostKind,
                    route.Value.Amount);
                routeTraversalLimit = route.Value.TraversalStackingLimit;
                continue;
            }

            var direction = feature.DirectionFromLocationId switch
            {
                null => MovementHexsideDirection.Either,
                var from when from == element.CurrentLocationId => MovementHexsideDirection.Up,
                var from when from == destinationId => MovementHexsideDirection.Down,
                _ => (MovementHexsideDirection?)null,
            };
            if (direction is null)
            {
                return null;
            }

            var hexside = Cna1979Movement.LookupHexside(
                feature.FeatureId,
                direction.Value,
                element.MobilityId);
            if (!hexside.IsSupported)
            {
                return null;
            }

            hexsideCosts.Add(new MovementActionHexsideCost(
                feature.FeatureId,
                direction.Value,
                hexside.Value.AddedCost));
        }

        if (!HasSupportedStacking(
                observation,
                destinationId,
                movingStacking.Value.StackingValue,
                terrain.Value.StoppingStackingLimit,
                routeTraversalLimit))
        {
            return null;
        }

        try
        {
            var adjustedTerrain = routeAdjustment switch
            {
                null => terrain.Value.Cost,
                { CostKind: MovementRouteCostKind.Override } => routeAdjustment.Amount,
                { CostKind: MovementRouteCostKind.ScaleUnderlying } => Scale(
                    terrain.Value.Cost,
                    routeAdjustment.Amount),
                _ => throw new InvalidOperationException(),
            };
            var total = hexsideCosts.Aggregate(
                adjustedTerrain,
                (current, value) => current + value.AddedCost);
            var allowance = new CapabilityPointAmount(
                element.BaseCapabilityPointAllowance,
                1);
            if (element.CapabilityPointsExpended + total > allowance)
            {
                return null;
            }

            return new MoveElementAction(
                element.ElementId,
                element.CurrentLocationId,
                destinationId,
                new MovementActionCostBreakdown(
                    destination.TerrainId,
                    terrain.Value.Cost,
                    routeAdjustment,
                    hexsideCosts,
                    total));
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or OverflowException)
        {
            return null;
        }
    }

    private static bool HasSupportedStacking(
        CampaignObservation observation,
        string destinationId,
        int movingValue,
        int stoppingLimit,
        int traversalLimit)
    {
        try
        {
            var destinationValue = 0;
            foreach (var occupant in observation.OwnElements.Where(element =>
                element.CurrentLocationId == destinationId))
            {
                var lookup = Cna1979Movement.LookupStackingValue(occupant.OrganizationId);
                if (!lookup.IsSupported)
                {
                    return false;
                }

                destinationValue = checked(destinationValue + lookup.Value.StackingValue);
            }

            var resultingValue = checked(destinationValue + movingValue);
            return resultingValue <= stoppingLimit && resultingValue <= traversalLimit;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static CapabilityPointAmount Scale(
        CapabilityPointAmount amount,
        CapabilityPointAmount factor)
    {
        var numerator = checked(amount.Numerator * factor.Numerator);
        var denominator = checked(amount.Denominator * factor.Denominator);
        return new CapabilityPointAmount(numerator, denominator);
    }
}
