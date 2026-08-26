namespace Cna.Core.Campaigns;

internal sealed record CampaignWorldSnapshot
{
    public const int CurrentContractVersion = 3;

    public CampaignWorldSnapshot(
        int contractVersion,
        IReadOnlyList<CampaignElementState> elements,
        IReadOnlyList<CampaignMapRepresentationState> representations)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            contractVersion,
            CurrentContractVersion);
        ArgumentNullException.ThrowIfNull(elements);
        var copy = elements.ToArray();

        if (copy.Any(element => element is null))
        {
            throw new ArgumentException(
                "Null campaign element states are not allowed.",
                nameof(elements));
        }

        if (copy.Select(element => element.ElementId).Distinct(StringComparer.Ordinal).Count()
            != copy.Length)
        {
            throw new ArgumentException(
                "Campaign element IDs must be unique.",
                nameof(elements));
        }

        ArgumentNullException.ThrowIfNull(representations);
        var representationCopy = representations.ToArray();
        if (representationCopy.Any(representation => representation is null))
        {
            throw new ArgumentException(
                "Null map representation states are not allowed.",
                nameof(representations));
        }

        if (representationCopy
                .Select(representation => representation.RepresentationId)
                .Distinct(StringComparer.Ordinal)
                .Count() != representationCopy.Length)
        {
            throw new ArgumentException(
                "Map representation IDs must be unique.",
                nameof(representations));
        }

        ContractVersion = contractVersion;
        Elements = Array.AsReadOnly(copy
            .OrderBy(element => element.ElementId, StringComparer.Ordinal)
            .ToArray());
        Representations = Array.AsReadOnly(representationCopy
            .OrderBy(representation => representation.RepresentationId, StringComparer.Ordinal)
            .ToArray());
    }

    public int ContractVersion { get; }

    public IReadOnlyList<CampaignElementState> Elements { get; }

    public IReadOnlyList<CampaignMapRepresentationState> Representations { get; }

    public bool Equals(CampaignWorldSnapshot? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && Elements.SequenceEqual(other.Elements)
            && Representations.SequenceEqual(other.Representations));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion);

        foreach (var element in Elements)
        {
            hash.Add(element);
        }

        foreach (var representation in Representations)
        {
            hash.Add(representation);
        }

        return hash.ToHashCode();
    }
}
