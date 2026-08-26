using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal enum CampaignMapRepresentationBindingKind
{
    IndependentElement = 0,
}

internal sealed record CampaignMapRepresentationState
{
    public CampaignMapRepresentationState(
        string representationId,
        string currentLocationId,
        CampaignMapRepresentationBindingKind bindingKind,
        IEnumerable<string> boundElementIds)
    {
        if (!Enum.IsDefined(bindingKind))
        {
            throw new ArgumentOutOfRangeException(nameof(bindingKind));
        }

        ArgumentNullException.ThrowIfNull(boundElementIds);
        var bindings = boundElementIds
            .Select(elementId => ContentContractGuards.RequireStableId(
                elementId,
                nameof(boundElementIds)))
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (bindings.Length != 1 || bindings.Distinct(StringComparer.Ordinal).Count() != 1)
        {
            throw new ArgumentException(
                "An independent-element representation must bind exactly one element.",
                nameof(boundElementIds));
        }

        RepresentationId = ContentContractGuards.RequireStableId(
            representationId,
            nameof(representationId));
        CurrentLocationId = ContentContractGuards.RequireStableId(
            currentLocationId,
            nameof(currentLocationId));
        BindingKind = bindingKind;
        BoundElementIds = Array.AsReadOnly(bindings);
    }

    public string RepresentationId { get; }

    public string CurrentLocationId { get; }

    public CampaignMapRepresentationBindingKind BindingKind { get; }

    public IReadOnlyList<string> BoundElementIds { get; }

    public bool Equals(CampaignMapRepresentationState? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && string.Equals(RepresentationId, other.RepresentationId, StringComparison.Ordinal)
            && string.Equals(CurrentLocationId, other.CurrentLocationId, StringComparison.Ordinal)
            && BindingKind == other.BindingKind
            && BoundElementIds.SequenceEqual(other.BoundElementIds, StringComparer.Ordinal));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(RepresentationId, StringComparer.Ordinal);
        hash.Add(CurrentLocationId, StringComparer.Ordinal);
        hash.Add(BindingKind);
        foreach (var elementId in BoundElementIds)
        {
            hash.Add(elementId, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
