using Cna.Core.Content;

namespace Cna.Core.Observations;

public sealed record CampaignObservationLocation
{
    internal CampaignObservationLocation(string locationId, string terrainId)
    {
        LocationId = ContentContractGuards.RequireStableId(locationId, nameof(locationId));
        TerrainId = ContentContractGuards.RequireStableId(terrainId, nameof(terrainId));
    }

    public string LocationId { get; }

    public string TerrainId { get; }
}

public sealed record CampaignObservationEdgeFeature
{
    internal CampaignObservationEdgeFeature(string featureId, string? directionFromLocationId)
    {
        FeatureId = ContentContractGuards.RequireStableId(featureId, nameof(featureId));
        DirectionFromLocationId = directionFromLocationId is null
            ? null
            : ContentContractGuards.RequireStableId(
                directionFromLocationId,
                nameof(directionFromLocationId));
    }

    public string FeatureId { get; }

    public string? DirectionFromLocationId { get; }
}

public sealed record CampaignObservationEdge
{
    internal CampaignObservationEdge(
        string firstLocationId,
        string secondLocationId,
        IReadOnlyList<CampaignObservationEdgeFeature> features)
    {
        var first = ContentContractGuards.RequireStableId(
            firstLocationId,
            nameof(firstLocationId));
        var second = ContentContractGuards.RequireStableId(
            secondLocationId,
            nameof(secondLocationId));

        if (string.Equals(first, second, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "An observation edge must connect two different locations.",
                nameof(secondLocationId));
        }

        if (StringComparer.Ordinal.Compare(first, second) <= 0)
        {
            FirstLocationId = first;
            SecondLocationId = second;
        }
        else
        {
            FirstLocationId = second;
            SecondLocationId = first;
        }

        var copy = ContentContractGuards.CopyValues(features, nameof(features));

        if (copy.Any(feature => feature.DirectionFromLocationId is not null
            && !string.Equals(
                feature.DirectionFromLocationId,
                FirstLocationId,
                StringComparison.Ordinal)
            && !string.Equals(
                feature.DirectionFromLocationId,
                SecondLocationId,
                StringComparison.Ordinal)))
        {
            throw new ArgumentException(
                "A directional edge feature must point from one of the edge endpoints.",
                nameof(features));
        }

        if (copy
            .Select(feature => (feature.FeatureId, feature.DirectionFromLocationId))
            .Distinct()
            .Count() != copy.Length)
        {
            throw new ArgumentException(
                "Duplicate observation edge features are not allowed.",
                nameof(features));
        }

        Features = Array.AsReadOnly(copy
            .OrderBy(feature => feature.FeatureId, StringComparer.Ordinal)
            .ThenBy(feature => feature.DirectionFromLocationId is null ? 0 : 1)
            .ThenBy(feature => feature.DirectionFromLocationId, StringComparer.Ordinal)
            .ToArray());
    }

    public string FirstLocationId { get; }

    public string SecondLocationId { get; }

    public IReadOnlyList<CampaignObservationEdgeFeature> Features { get; }

    public bool Equals(CampaignObservationEdge? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(FirstLocationId, other.FirstLocationId, StringComparison.Ordinal)
            && string.Equals(SecondLocationId, other.SecondLocationId, StringComparison.Ordinal)
            && Features.SequenceEqual(other.Features));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(FirstLocationId, StringComparer.Ordinal);
        hash.Add(SecondLocationId, StringComparer.Ordinal);

        foreach (var feature in Features)
        {
            hash.Add(feature);
        }

        return hash.ToHashCode();
    }
}
