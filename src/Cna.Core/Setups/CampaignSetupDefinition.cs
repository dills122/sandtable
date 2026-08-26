using Cna.Core.Rules;

namespace Cna.Core.Setups;

internal sealed record CampaignSetupDefinition
{
    public CampaignSetupDefinition(
        int schemaVersion,
        string setupId,
        string displayName,
        bool isSynthetic,
        int initialGameTurn,
        InitiativePolicy initialInitiative,
        CampaignOpeningPreamblePolicy openingPreamble,
        CampaignWeatherPolicy weather,
        CampaignStageEntryPolicy stageEntry,
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
        ArgumentNullException.ThrowIfNull(openingPreamble);
        ArgumentNullException.ThrowIfNull(weather);
        ArgumentNullException.ThrowIfNull(stageEntry);
        ArgumentNullException.ThrowIfNull(content);

        SchemaVersion = schemaVersion;
        SetupId = setupId;
        DisplayName = displayName;
        IsSynthetic = isSynthetic;
        InitialGameTurn = initialGameTurn;
        InitialInitiative = initialInitiative;
        OpeningPreamble = openingPreamble;
        Weather = weather;
        StageEntry = stageEntry;
        Content = content;
        Sources = RuleReferenceValidation.CopySources(sources, nameof(sources));
        Hash = CampaignSetupHash.Calculate(this);
    }

    public int SchemaVersion { get; }

    public string SetupId { get; }

    public string DisplayName { get; }

    public bool IsSynthetic { get; }

    public int InitialGameTurn { get; }

    public InitiativePolicy InitialInitiative { get; }

    internal CampaignOpeningPreamblePolicy OpeningPreamble { get; }

    internal CampaignWeatherPolicy Weather { get; }

    internal CampaignStageEntryPolicy StageEntry { get; }

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
            && OpeningPreamble == other.OpeningPreamble
            && Weather == other.Weather
            && StageEntry == other.StageEntry
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
        hash.Add(OpeningPreamble);
        hash.Add(Weather);
        hash.Add(StageEntry);
        hash.Add(Content);

        foreach (var source in Sources)
        {
            hash.Add(source);
        }

        hash.Add(Hash, StringComparer.Ordinal);
        return hash.ToHashCode();
    }
}
