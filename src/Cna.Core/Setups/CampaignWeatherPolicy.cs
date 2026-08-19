using System.Collections.ObjectModel;
using Cna.Core.Rules;

namespace Cna.Core.Setups;

internal enum CampaignWeatherPolicyKind
{
    NoImmediateWeatherEffectSubjects = 1,
}

internal sealed record CampaignWeatherPolicy
{
    public const int CurrentContractVersion = 1;

    public CampaignWeatherPolicy(
        int contractVersion,
        CampaignWeatherPolicyKind kind,
        IReadOnlyList<RuleReference> sources)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(contractVersion, CurrentContractVersion);

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        ContractVersion = contractVersion;
        Kind = kind;
        Sources = CopySources(sources);
    }

    public int ContractVersion { get; }

    public CampaignWeatherPolicyKind Kind { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(CampaignWeatherPolicy? other) =>
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

    private static ReadOnlyCollection<RuleReference> CopySources(
        IReadOnlyList<RuleReference> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);
        var copy = sources.ToArray();

        if (copy.Length == 0 || copy.Any(source => source is null))
        {
            throw new ArgumentException(
                "At least one non-null source reference is required.",
                nameof(sources));
        }

        if (copy.Distinct().Count() != copy.Length)
        {
            throw new ArgumentException("Duplicate sources are not allowed.", nameof(sources));
        }

        return Array.AsReadOnly(copy
            .OrderBy(source => source.SourceId, StringComparer.Ordinal)
            .ThenBy(source => source.Locator, StringComparer.Ordinal)
            .ToArray());
    }
}
