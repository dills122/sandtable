using Cna.Core.Rules;

namespace Cna.Core.Setups;

internal enum CampaignOpeningPreambleKind
{
    NoOpeningNavalConvoyObligations = 1,
}

internal sealed record CampaignOpeningPreamblePolicy
{
    public const int CurrentContractVersion = 1;

    public CampaignOpeningPreamblePolicy(
        int contractVersion,
        CampaignOpeningPreambleKind kind,
        IReadOnlyList<RuleReference> sources)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(contractVersion, CurrentContractVersion);

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        ContractVersion = contractVersion;
        Kind = kind;
        Sources = RuleReferenceValidation.CopySources(sources, nameof(sources));
    }

    public int ContractVersion { get; }

    public CampaignOpeningPreambleKind Kind { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(CampaignOpeningPreamblePolicy? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && Kind == other.Kind
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion);
        hash.Add(Kind);
        foreach (var source in Sources) hash.Add(source);
        return hash.ToHashCode();
    }
}
