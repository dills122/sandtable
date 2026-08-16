using System.Collections.ObjectModel;
using Cna.Core.Rules;

namespace Cna.Core.Setups;

public sealed record CampaignSetupDefinition
{
    public CampaignSetupDefinition(
        int schemaVersion,
        string setupId,
        string displayName,
        bool isSynthetic,
        int initialGameTurn,
        InitiativePolicy initialInitiative,
        CampaignContentSelection content,
        IReadOnlyList<RuleReference> sources)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(schemaVersion, 1);
        ArgumentException.ThrowIfNullOrWhiteSpace(setupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);

        if (initialGameTurn is < 1 or > 111)
        {
            throw new ArgumentOutOfRangeException(nameof(initialGameTurn));
        }

        ArgumentNullException.ThrowIfNull(initialInitiative);
        ArgumentNullException.ThrowIfNull(content);

        SchemaVersion = schemaVersion;
        SetupId = setupId;
        DisplayName = displayName;
        IsSynthetic = isSynthetic;
        InitialGameTurn = initialGameTurn;
        InitialInitiative = initialInitiative;
        Content = content;
        Sources = CopySources(sources);
        Hash = CampaignSetupHash.Calculate(this);
    }

    public int SchemaVersion { get; }

    public string SetupId { get; }

    public string DisplayName { get; }

    public bool IsSynthetic { get; }

    public int InitialGameTurn { get; }

    public InitiativePolicy InitialInitiative { get; }

    public CampaignContentSelection Content { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public string Hash { get; }

    public bool Equals(CampaignSetupDefinition? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && SchemaVersion == other.SchemaVersion
            && string.Equals(SetupId, other.SetupId, StringComparison.Ordinal)
            && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
            && IsSynthetic == other.IsSynthetic
            && InitialGameTurn == other.InitialGameTurn
            && InitialInitiative == other.InitialInitiative
            && Content == other.Content
            && Sources.SequenceEqual(other.Sources)
            && string.Equals(Hash, other.Hash, StringComparison.Ordinal));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SchemaVersion);
        hash.Add(SetupId, StringComparer.Ordinal);
        hash.Add(DisplayName, StringComparer.Ordinal);
        hash.Add(IsSynthetic);
        hash.Add(InitialGameTurn);
        hash.Add(InitialInitiative);
        hash.Add(Content);

        foreach (var source in Sources)
        {
            hash.Add(source);
        }

        hash.Add(Hash, StringComparer.Ordinal);
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
