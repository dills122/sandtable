using Cna.Core.Content;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal sealed record CampaignContentV6Selection
{
    public CampaignContentV6Selection(ContentPackV6Identity pack, string scenarioId)
    {
        ArgumentNullException.ThrowIfNull(pack);
        Pack = pack;
        ScenarioId = ContentContractGuards.RequireStableId(scenarioId, nameof(scenarioId));
    }

    public ContentPackV6Identity Pack { get; }

    public string ScenarioId { get; }
}

internal sealed record CampaignSetupSnapshotV6
{
    public const int CurrentSchemaVersion = 6;

    private CampaignSetupSnapshotV6(
        int schemaVersion,
        string setupId,
        bool isSynthetic,
        string capabilityProfileId,
        int initialGameTurn,
        InitiativePolicy initialInitiative,
        CampaignOpeningPreamblePolicy openingPreamble,
        CampaignWeatherPolicy weather,
        CampaignStageEntryPolicy stageEntry,
        CampaignContentV6Selection content,
        IEnumerable<RuleReference> sources,
        string? expectedSetupHash)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(schemaVersion, CurrentSchemaVersion);
        ArgumentOutOfRangeException.ThrowIfLessThan(initialGameTurn, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(initialGameTurn, 111);
        ArgumentNullException.ThrowIfNull(initialInitiative);
        ArgumentNullException.ThrowIfNull(openingPreamble);
        ArgumentNullException.ThrowIfNull(weather);
        ArgumentNullException.ThrowIfNull(stageEntry);
        ArgumentNullException.ThrowIfNull(content);
        if (!isSynthetic
            || capabilityProfileId != ContentPackV6Definition.SupportedCapabilityProfileId
            || content.Pack.RulesetId != Cna1979Ruleset.RulesetId)
        {
            throw new ArgumentException(
                $"{BreakdownCapabilityDiagnostics.Identity}: Setup requires the supported synthetic profile and ruleset.",
                nameof(capabilityProfileId));
        }

        SchemaVersion = schemaVersion;
        SetupId = ContentContractGuards.RequireStableId(setupId, nameof(setupId));
        IsSynthetic = isSynthetic;
        CapabilityProfileId = capabilityProfileId;
        InitialGameTurn = initialGameTurn;
        InitialInitiative = initialInitiative;
        OpeningPreamble = openingPreamble;
        Weather = weather;
        StageEntry = stageEntry;
        Content = content;
        Sources = RuleReferenceValidation.CopySources(sources, nameof(sources));
        SetupHash = CampaignSetupV6Codec.CalculateHash(this);
        if (expectedSetupHash is not null
            && !string.Equals(SetupHash, expectedSetupHash, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The Campaign setup v6 hash does not match its canonical fields.",
                nameof(expectedSetupHash));
        }
    }

    public int SchemaVersion { get; }

    public string SetupId { get; }

    public string SetupHash { get; }

    public bool IsSynthetic { get; }

    public string CapabilityProfileId { get; }

    public int InitialGameTurn { get; }

    public InitiativePolicy InitialInitiative { get; }

    public CampaignOpeningPreamblePolicy OpeningPreamble { get; }

    public CampaignWeatherPolicy Weather { get; }

    public CampaignStageEntryPolicy StageEntry { get; }

    public CampaignContentV6Selection Content { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public static CampaignSetupSnapshotV6 FromPredecessor(
        CampaignSetupSnapshot predecessor,
        CampaignContentV6Selection content)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        return new CampaignSetupSnapshotV6(
            6,
            predecessor.SetupId,
            predecessor.IsSynthetic,
            ContentPackV6Definition.SupportedCapabilityProfileId,
            predecessor.InitialGameTurn,
            predecessor.InitialInitiative,
            predecessor.OpeningPreamble,
            predecessor.Weather,
            predecessor.StageEntry,
            content,
            predecessor.Sources,
            null);
    }

    public static CampaignSetupSnapshotV6 FromDefinition(CampaignSetupDefinitionV6 definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return definition.Snapshot;
    }

    internal static CampaignSetupSnapshotV6 Create(
        int schemaVersion,
        string setupId,
        bool isSynthetic,
        string capabilityProfileId,
        int initialGameTurn,
        InitiativePolicy initialInitiative,
        CampaignOpeningPreamblePolicy openingPreamble,
        CampaignWeatherPolicy weather,
        CampaignStageEntryPolicy stageEntry,
        CampaignContentV6Selection content,
        IEnumerable<RuleReference> sources) => new(
            schemaVersion, setupId, isSynthetic, capabilityProfileId, initialGameTurn,
            initialInitiative, openingPreamble, weather, stageEntry, content, sources, null);

    internal static CampaignSetupSnapshotV6 FromCanonical(
        int schemaVersion,
        string setupId,
        string setupHash,
        bool isSynthetic,
        string capabilityProfileId,
        int initialGameTurn,
        InitiativePolicy initialInitiative,
        CampaignOpeningPreamblePolicy openingPreamble,
        CampaignWeatherPolicy weather,
        CampaignStageEntryPolicy stageEntry,
        CampaignContentV6Selection content,
        IEnumerable<RuleReference> sources) => new(
            schemaVersion,
            setupId,
            isSynthetic,
            capabilityProfileId,
            initialGameTurn,
            initialInitiative,
            openingPreamble,
            weather,
            stageEntry,
            content,
            sources,
            ContentContractGuards.RequireSha256(setupHash, nameof(setupHash)));

    public bool Equals(CampaignSetupSnapshotV6? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && SchemaVersion == other.SchemaVersion
            && string.Equals(SetupId, other.SetupId, StringComparison.Ordinal)
            && string.Equals(SetupHash, other.SetupHash, StringComparison.Ordinal)
            && IsSynthetic == other.IsSynthetic
            && CapabilityProfileId == other.CapabilityProfileId
            && InitialGameTurn == other.InitialGameTurn
            && InitialInitiative == other.InitialInitiative
            && OpeningPreamble == other.OpeningPreamble
            && Weather == other.Weather
            && StageEntry == other.StageEntry
            && Content == other.Content
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SchemaVersion);
        hash.Add(SetupId, StringComparer.Ordinal);
        hash.Add(SetupHash, StringComparer.Ordinal);
        hash.Add(IsSynthetic);
        hash.Add(CapabilityProfileId, StringComparer.Ordinal);
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

        return hash.ToHashCode();
    }
}
