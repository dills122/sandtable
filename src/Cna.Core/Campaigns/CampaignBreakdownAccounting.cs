using System.Numerics;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignBreakdownAccounting
{
    public static CampaignBreakdownStep Calculate(ContentBreakdownVehicleCohort cohort,
        CampaignVehicleBreakdownState prior, ContentHexEdge edge, ContentHex destination,
        string originLocationId, BreakdownWeatherKind weatherKind)
    {
        ArgumentNullException.ThrowIfNull(cohort); ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(edge); ArgumentNullException.ThrowIfNull(destination);
        if (!Enum.IsDefined(weatherKind) || cohort.CohortId != prior.CohortId
            || !Cna1979Breakdown.IsSupportedVehicleProfile(cohort.VehicleTypeId, cohort.ProfileId)
            || (long)prior.WorkingPointCount + prior.BrokenPointCount != cohort.WorkingPointCount)
            throw new ArgumentException("Invalid Breakdown cohort or weather binding.");
        if (originLocationId == destination.LocationId
            || !((edge.FirstLocationId == originLocationId && edge.SecondLocationId == destination.LocationId)
                || (edge.SecondLocationId == originLocationId && edge.FirstLocationId == destination.LocationId)))
            throw new ArgumentException("The Breakdown edge does not bind the move endpoints.");

        var terrain = Require(Cna1979Breakdown.LookupTerrain(destination.TerrainId));
        var delta = terrain.Value.Points;
        var sources = new List<RuleReference>(cohort.Origin.References);
        sources.AddRange(edge.Origin.References); sources.AddRange(destination.Origin.References);
        sources.AddRange(terrain.Sources);
        var weatherSources = Cna1979Breakdown.Definition.WeatherShifts
            .Where(value => value.WeatherKind == weatherKind).SelectMany(value => value.Sources)
            .Concat(Cna1979Breakdown.Definition.WeatherInputTransformations
                .Where(value => value.WeatherKind == weatherKind).SelectMany(value => value.Sources));
        sources.AddRange(weatherSources);
        string? inputRouteId = null;
        string? effectiveRouteId = null;
        var crossed = new List<CampaignBreakdownCrossedHexside>();
        foreach (var feature in edge.Features)
        {
            sources.AddRange(feature.Origin.References);
            var route = Cna1979Breakdown.LookupRoute(feature.FeatureId);
            if (route.IsSupported)
            {
                if (inputRouteId is not null || feature.DirectionFromLocationId is not null)
                    throw new ArgumentException("Unsupported Breakdown route combination.");
                inputRouteId = feature.FeatureId;
                effectiveRouteId = inputRouteId;
                sources.AddRange(route.Sources);
                var transformation = Cna1979Breakdown.LookupWeatherInputTransformation(weatherKind);
                if (transformation.IsSupported && transformation.Value.InputRouteId == inputRouteId)
                {
                    effectiveRouteId = transformation.Value.TreatedAsRouteId;
                    sources.AddRange(transformation.Sources);
                }
                var effective = Require(Cna1979Breakdown.LookupRoute(effectiveRouteId));
                sources.AddRange(effective.Sources);
                delta = effective.Value.Operation switch
                {
                    BreakdownInputOperation.Override => effective.Value.Amount,
                    BreakdownInputOperation.ScaleUnderlying => Scale(terrain.Value.Points, effective.Value.Amount),
                    _ => throw new ArgumentException("Unsupported Breakdown route operation."),
                };
                continue;
            }

            var direction = feature.DirectionFromLocationId switch
            {
                null => BreakdownHexsideDirection.Either,
                var from when from == originLocationId => BreakdownHexsideDirection.Up,
                var from when from == destination.LocationId => BreakdownHexsideDirection.Down,
                _ => throw new ArgumentException("The Breakdown hexside direction does not bind this edge."),
            };
            var rule = Require(Cna1979Breakdown.LookupHexside(feature.FeatureId, direction));
            var hexsideSources = Union(feature.Origin.References.Concat(rule.Sources));
            crossed.Add(new(feature.FeatureId, direction, rule.Value.AddedPoints, hexsideSources));
            sources.AddRange(rule.Sources);
        }
        // Apply the route to terrain first, regardless of canonical feature order.
        delta = crossed.Aggregate(delta, (sum, value) => sum + value.AddedPoints);
        var sandstormDelta = weatherKind == BreakdownWeatherKind.Sandstorm ? delta : BreakdownPointAmount.Zero;
        return new(cohort.CohortId, cohort.VehicleTypeId, cohort.ProfileId, destination.TerrainId,
            inputRouteId, effectiveRouteId, crossed, weatherKind, prior.CumulativeBreakdownPoints,
            delta, prior.CumulativeBreakdownPoints + delta, prior.SandstormAttributedBreakdownPoints,
            sandstormDelta, prior.SandstormAttributedBreakdownPoints + sandstormDelta, Union(sources));
    }

    public static CampaignVehicleBreakdownState Apply(CampaignVehicleBreakdownState prior,
        CampaignBreakdownStep step)
    {
        ArgumentNullException.ThrowIfNull(prior); ArgumentNullException.ThrowIfNull(step);
        if (prior.CohortId != step.CohortId || prior.CumulativeBreakdownPoints != step.Before
            || prior.SandstormAttributedBreakdownPoints != step.SandstormBefore)
            throw new ArgumentException("The Breakdown step does not bind the prior ledger.");
        return new(prior.CohortId, step.After, step.SandstormAfter, prior.HighestEffectiveCheckedBandId,
            prior.WorkingPointCount, prior.BrokenPointCount);
    }

    private static BreakdownRuleLookupResult<T> Require<T>(BreakdownRuleLookupResult<T> result) =>
        result.IsSupported ? result : throw new ArgumentException("Unsupported Breakdown movement input.");

    private static RuleReference[] Union(IEnumerable<RuleReference> sources) => sources.Distinct()
        .OrderBy(value => value.SourceId, StringComparer.Ordinal).ThenBy(value => value.Locator, StringComparer.Ordinal).ToArray();

    private static BreakdownPointAmount Scale(BreakdownPointAmount amount, BreakdownPointAmount factor)
    {
        var numerator = (BigInteger)amount.Numerator * factor.Numerator;
        var denominator = (BigInteger)amount.Denominator * factor.Denominator;
        var divisor = BigInteger.GreatestCommonDivisor(numerator, denominator);
        numerator /= divisor; denominator /= divisor;
        return new(checked((long)numerator), checked((int)denominator));
    }
}
