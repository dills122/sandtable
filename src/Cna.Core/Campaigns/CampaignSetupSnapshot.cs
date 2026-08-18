using System.Collections.ObjectModel;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal sealed record CampaignSetupSnapshot
{
    public CampaignSetupSnapshot(
        int schemaVersion,
        string setupId,
        string setupHash,
        bool isSynthetic,
        int initialGameTurn,
        InitiativePolicy initialInitiative,
        CampaignOpeningPreamblePolicy openingPreamble,
        CampaignContentSelection content,
        IReadOnlyList<RuleReference> sources)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(schemaVersion, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(setupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(setupHash);
        ArgumentOutOfRangeException.ThrowIfLessThan(initialGameTurn, 1);
        ArgumentNullException.ThrowIfNull(initialInitiative);
        ArgumentNullException.ThrowIfNull(openingPreamble);
        ArgumentNullException.ThrowIfNull(content);

        SchemaVersion = schemaVersion;
        SetupId = setupId;
        SetupHash = setupHash;
        IsSynthetic = isSynthetic;
        InitialGameTurn = initialGameTurn;
        InitialInitiative = initialInitiative;
        OpeningPreamble = openingPreamble;
        Content = content;
        Sources = CopySources(sources);
    }

    public int SchemaVersion { get; }
    public string SetupId { get; }
    public string SetupHash { get; }
    public bool IsSynthetic { get; }
    public int InitialGameTurn { get; }
    public InitiativePolicy InitialInitiative { get; }
    public CampaignOpeningPreamblePolicy OpeningPreamble { get; }
    public CampaignContentSelection Content { get; }
    public IReadOnlyList<RuleReference> Sources { get; }

    public static CampaignSetupSnapshot FromDefinition(CampaignSetupDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new CampaignSetupSnapshot(
            definition.SchemaVersion,
            definition.SetupId,
            definition.Hash,
            definition.IsSynthetic,
            definition.InitialGameTurn,
            definition.InitialInitiative,
            definition.OpeningPreamble,
            definition.Content,
            definition.Sources);
    }

    public bool Equals(CampaignSetupSnapshot? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && SchemaVersion == other.SchemaVersion
            && string.Equals(SetupId, other.SetupId, StringComparison.Ordinal)
            && string.Equals(SetupHash, other.SetupHash, StringComparison.Ordinal)
            && IsSynthetic == other.IsSynthetic
            && InitialGameTurn == other.InitialGameTurn
            && InitialInitiative == other.InitialInitiative
            && OpeningPreamble == other.OpeningPreamble
            && Content == other.Content
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SchemaVersion);
        hash.Add(SetupId, StringComparer.Ordinal);
        hash.Add(SetupHash, StringComparer.Ordinal);
        hash.Add(IsSynthetic);
        hash.Add(InitialGameTurn);
        hash.Add(InitialInitiative);
        hash.Add(OpeningPreamble);
        hash.Add(Content);

        foreach (var source in Sources)
        {
            hash.Add(source);
        }

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
            throw new ArgumentException(
                "Duplicate source references are not allowed.",
                nameof(sources));
        }

        return Array.AsReadOnly(copy
            .OrderBy(source => source.SourceId, StringComparer.Ordinal)
            .ThenBy(source => source.Locator, StringComparer.Ordinal)
            .ToArray());
    }
}
