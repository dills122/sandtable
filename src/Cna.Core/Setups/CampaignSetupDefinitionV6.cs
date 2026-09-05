using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Setups;

internal sealed record CampaignSetupDefinitionV6
{
    public CampaignSetupDefinitionV6(
        int schemaVersion,
        string setupId,
        string displayName,
        bool isSynthetic,
        string capabilityProfileId,
        int initialGameTurn,
        InitiativePolicy initialInitiative,
        CampaignOpeningPreamblePolicy openingPreamble,
        CampaignWeatherPolicy weather,
        CampaignStageEntryPolicy stageEntry,
        CampaignContentV6Selection content,
        IEnumerable<RuleReference> sources)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        DisplayName = displayName;
        Snapshot = CampaignSetupSnapshotV6.Create(
            schemaVersion, setupId, isSynthetic, capabilityProfileId, initialGameTurn,
            initialInitiative, openingPreamble, weather, stageEntry, content, sources);
    }

    internal CampaignSetupSnapshotV6 Snapshot { get; }

    public int SchemaVersion => Snapshot.SchemaVersion;
    public string SetupId => Snapshot.SetupId;
    public string DisplayName { get; }
    public bool IsSynthetic => Snapshot.IsSynthetic;
    public string CapabilityProfileId => Snapshot.CapabilityProfileId;
    public int InitialGameTurn => Snapshot.InitialGameTurn;
    public InitiativePolicy InitialInitiative => Snapshot.InitialInitiative;
    public CampaignOpeningPreamblePolicy OpeningPreamble => Snapshot.OpeningPreamble;
    public CampaignWeatherPolicy Weather => Snapshot.Weather;
    public CampaignStageEntryPolicy StageEntry => Snapshot.StageEntry;
    public CampaignContentV6Selection Content => Snapshot.Content;
    public IReadOnlyList<RuleReference> Sources => Snapshot.Sources;
    public string Hash => Snapshot.SetupHash;
}
