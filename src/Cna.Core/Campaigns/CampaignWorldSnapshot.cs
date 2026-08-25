namespace Cna.Core.Campaigns;

internal sealed record CampaignWorldSnapshot
{
    public const int CurrentContractVersion = 2;

    public CampaignWorldSnapshot(
        int contractVersion,
        IReadOnlyList<CampaignElementState> elements)
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

        ContractVersion = contractVersion;
        Elements = Array.AsReadOnly(copy
            .OrderBy(element => element.ElementId, StringComparer.Ordinal)
            .ToArray());
    }

    public int ContractVersion { get; }

    public IReadOnlyList<CampaignElementState> Elements { get; }

    public bool Equals(CampaignWorldSnapshot? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && Elements.SequenceEqual(other.Elements));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion);

        foreach (var element in Elements)
        {
            hash.Add(element);
        }

        return hash.ToHashCode();
    }
}
