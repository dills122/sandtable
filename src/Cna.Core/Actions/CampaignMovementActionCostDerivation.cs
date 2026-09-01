using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Actions;

internal static class CampaignMovementActionCostDerivation
{
    public static MovementActionCostBreakdown? TryCalculate(
        IReadOnlyList<CampaignObservationLocation> locations,
        IReadOnlyList<ObservedOwnElement> ownElements,
        ICampaignObservedMovementSubject element,
        CampaignObservationEdge edge,
        string destinationId)
    {
        ArgumentNullException.ThrowIfNull(ownElements);
        var destinationStackingValue = TryGetSupportedStackingValue(
            ownElements,
            destinationId);
        return destinationStackingValue is null
            ? null
            : TryCalculate(
                locations,
                destinationStackingValue.Value,
                element,
                edge,
                destinationId);
    }

    public static MovementActionCostBreakdown? TryCalculate(
        IReadOnlyList<CampaignObservationLocation> locations,
        int destinationStackingValue,
        ICampaignObservedMovementSubject element,
        CampaignObservationEdge edge,
        string destinationId)
    {
        ArgumentNullException.ThrowIfNull(locations);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(edge);
        if (destinationStackingValue < 0)
        {
            return null;
        }

        var destination = locations.SingleOrDefault(location =>
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
                destinationStackingValue,
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

            return new MovementActionCostBreakdown(
                destination.TerrainId,
                terrain.Value.Cost,
                routeAdjustment,
                hexsideCosts,
                total);
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or OverflowException)
        {
            return null;
        }
    }

    private static int? TryGetSupportedStackingValue(
        IReadOnlyList<ObservedOwnElement> ownElements,
        string destinationId)
    {
        try
        {
            var destinationValue = 0;
            foreach (var occupant in ownElements.Where(element =>
                element.CurrentLocationId == destinationId))
            {
                var lookup = Cna1979Movement.LookupStackingValue(occupant.OrganizationId);
                if (!lookup.IsSupported)
                {
                    return null;
                }

                destinationValue = checked(destinationValue + lookup.Value.StackingValue);
            }

            return destinationValue;
        }
        catch (OverflowException)
        {
            return null;
        }
    }

    private static bool HasSupportedStacking(
        int destinationValue,
        int movingValue,
        int stoppingLimit,
        int traversalLimit)
    {
        try
        {
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
        CapabilityPointAmount factor) => new(
            checked(amount.Numerator * factor.Numerator),
            checked(amount.Denominator * factor.Denominator));
}
