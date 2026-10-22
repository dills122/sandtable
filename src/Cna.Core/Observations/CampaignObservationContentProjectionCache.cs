using System.Runtime.CompilerServices;
using Cna.Core.Content;

namespace Cna.Core.Observations;

/// <summary>
/// Caches the locations/edges/elements-by-id projections derived from a
/// <see cref="ContentPackDefinition"/>, which is immutable for the life of a campaign.
/// V6/V7 observation projection runs once per observer per decision point, so without
/// this cache every call re-walks the whole content pack to rebuild data that never
/// changes after the campaign starts.
/// </summary>
internal static class CampaignObservationContentProjectionCache
{
    private static readonly ConditionalWeakTable<ContentPackDefinition, Entry> Cache = new();

    public static Entry Get(ContentPackDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return Cache.GetValue(definition, Build);
    }

    private static Entry Build(ContentPackDefinition definition) => new(
        definition.Locations.Select(value =>
            new CampaignObservationLocation(value.LocationId, value.TerrainId)).ToArray(),
        definition.Edges.Select(value => new CampaignObservationEdge(
            value.FirstLocationId,
            value.SecondLocationId,
            value.Features.Select(feature => new CampaignObservationEdgeFeature(
                feature.FeatureId,
                feature.DirectionFromLocationId)).ToArray())).ToArray(),
        definition.Elements.ToDictionary(value => value.ElementId, StringComparer.Ordinal));

    internal sealed record Entry(
        IReadOnlyList<CampaignObservationLocation> Locations,
        IReadOnlyList<CampaignObservationEdge> Edges,
        IReadOnlyDictionary<string, ContentCombatElement> ElementsById);
}
