namespace Cna.Core.Content;

public sealed record ContentSourceCoordinate
{
    public ContentSourceCoordinate(string sectionId, string label)
    {
        SectionId = ContentContractGuards.RequireSourceAtom(sectionId, nameof(sectionId));
        Label = ContentContractGuards.RequireSourceAtom(label, nameof(label));
    }

    public string SectionId { get; }

    public string Label { get; }
}

public sealed record ContentHex
{
    public ContentHex(
        string locationId,
        string terrainId,
        ContentSourceCoordinate? sourceCoordinate,
        ContentOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(origin);
        LocationId = ContentContractGuards.RequireStableId(locationId, nameof(locationId));
        TerrainId = ContentContractGuards.RequireStableId(terrainId, nameof(terrainId));
        SourceCoordinate = sourceCoordinate;
        Origin = origin;
    }

    public string LocationId { get; }

    public string TerrainId { get; }

    public ContentSourceCoordinate? SourceCoordinate { get; }

    public ContentOrigin Origin { get; }
}

public sealed record ContentEdgeFeature
{
    public ContentEdgeFeature(
        string featureId,
        string? directionFromLocationId,
        ContentOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(origin);
        FeatureId = ContentContractGuards.RequireStableId(featureId, nameof(featureId));
        DirectionFromLocationId = directionFromLocationId is null
            ? null
            : ContentContractGuards.RequireStableId(
                directionFromLocationId,
                nameof(directionFromLocationId));
        Origin = origin;
    }

    public string FeatureId { get; }

    public string? DirectionFromLocationId { get; }

    public ContentOrigin Origin { get; }
}

public sealed record ContentHexEdge
{
    public ContentHexEdge(
        string firstLocationId,
        string secondLocationId,
        IEnumerable<ContentEdgeFeature> features,
        ContentOrigin origin)
    {
        ArgumentNullException.ThrowIfNull(origin);
        var first = ContentContractGuards.RequireStableId(
            firstLocationId,
            nameof(firstLocationId));
        var second = ContentContractGuards.RequireStableId(
            secondLocationId,
            nameof(secondLocationId));
        var featureCopy = ContentContractGuards.CopyValues(features, nameof(features));

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

        Features = Array.AsReadOnly(featureCopy
            .OrderBy(feature => feature.FeatureId, StringComparer.Ordinal)
            .ThenBy(feature => feature.DirectionFromLocationId is null ? 0 : 1)
            .ThenBy(
                feature => feature.DirectionFromLocationId,
                StringComparer.Ordinal)
            .ToArray());
        Origin = origin;
    }

    public string FirstLocationId { get; }

    public string SecondLocationId { get; }

    public IReadOnlyList<ContentEdgeFeature> Features { get; }

    public ContentOrigin Origin { get; }

    public bool Equals(ContentHexEdge? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(FirstLocationId, other.FirstLocationId, StringComparison.Ordinal)
            && string.Equals(SecondLocationId, other.SecondLocationId, StringComparison.Ordinal)
            && Features.SequenceEqual(other.Features)
            && Origin == other.Origin);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(FirstLocationId, StringComparer.Ordinal);
        hash.Add(SecondLocationId, StringComparer.Ordinal);

        foreach (var feature in Features)
        {
            hash.Add(feature);
        }

        hash.Add(Origin);
        return hash.ToHashCode();
    }
}
