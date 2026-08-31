using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal sealed record CampaignContentV5Selection
{
    public CampaignContentV5Selection(ContentPackV5Identity pack, string scenarioId)
    {
        ArgumentNullException.ThrowIfNull(pack);
        Pack = pack;
        ScenarioId = ContentContractGuards.RequireStableId(scenarioId, nameof(scenarioId));
    }

    public ContentPackV5Identity Pack { get; }

    public string ScenarioId { get; }
}

internal sealed record CampaignSetupSnapshotV5
{
    private CampaignSetupSnapshotV5(
        CampaignSetupSnapshot predecessor,
        CampaignContentV5Selection content)
    {
        ArgumentNullException.ThrowIfNull(predecessor);
        ArgumentNullException.ThrowIfNull(content);
        SchemaVersion = predecessor.SchemaVersion;
        SetupId = predecessor.SetupId;
        IsSynthetic = predecessor.IsSynthetic;
        InitialGameTurn = predecessor.InitialGameTurn;
        InitialInitiative = predecessor.InitialInitiative;
        OpeningPreamble = predecessor.OpeningPreamble;
        Weather = predecessor.Weather;
        StageEntry = predecessor.StageEntry;
        Content = content;
        Sources = Array.AsReadOnly(predecessor.Sources.ToArray());
        SetupHash = CampaignSetupHash.CalculateV5(
            SchemaVersion,
            SetupId,
            IsSynthetic,
            InitialGameTurn,
            InitialInitiative,
            OpeningPreamble,
            Weather,
            StageEntry,
            Content.Pack,
            Content.ScenarioId,
            Sources);
    }

    public int SchemaVersion { get; }

    public string SetupId { get; }

    public string SetupHash { get; }

    public bool IsSynthetic { get; }

    public int InitialGameTurn { get; }

    public InitiativePolicy InitialInitiative { get; }

    public CampaignOpeningPreamblePolicy OpeningPreamble { get; }

    public CampaignWeatherPolicy Weather { get; }

    public CampaignStageEntryPolicy StageEntry { get; }

    public CampaignContentV5Selection Content { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public static CampaignSetupSnapshotV5 FromPredecessor(
        CampaignSetupSnapshot predecessor,
        CampaignContentV5Selection content) => new(predecessor, content);

    public bool Equals(CampaignSetupSnapshotV5? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && SchemaVersion == other.SchemaVersion
            && string.Equals(SetupId, other.SetupId, StringComparison.Ordinal)
            && string.Equals(SetupHash, other.SetupHash, StringComparison.Ordinal)
            && IsSynthetic == other.IsSynthetic
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

internal sealed record CampaignCreatedV9
{
    public const int CurrentContractVersion = 9;

    public CampaignCreatedV9(
        string campaignId,
        long stateVersion,
        string rulesetHash,
        CampaignSetupSnapshotV5 setup,
        CampaignWorldSnapshotV5 initialWorld,
        RandomStreamState randomState,
        LandSequencePosition sequencePosition)
    {
        CampaignId = ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        ArgumentOutOfRangeException.ThrowIfNotEqual(stateVersion, 1);
        if (!CampaignSnapshotValidator.IsRulesHash(rulesetHash))
        {
            throw new ArgumentException(
                "A ruleset hash must contain 64 lowercase hexadecimal digits.",
                nameof(rulesetHash));
        }

        RulesetHash = rulesetHash;
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(initialWorld);
        ArgumentNullException.ThrowIfNull(randomState);
        ArgumentNullException.ThrowIfNull(sequencePosition);
        ContractVersion = CurrentContractVersion;
        StateVersion = stateVersion;
        Setup = setup;
        InitialWorld = initialWorld;
        RandomState = randomState;
        SequencePosition = sequencePosition;
    }

    public int ContractVersion { get; }

    public string CampaignId { get; }

    public long StateVersion { get; }

    public string RulesetHash { get; }

    public CampaignSetupSnapshotV5 Setup { get; }

    public CampaignWorldSnapshotV5 InitialWorld { get; }

    public RandomStreamState RandomState { get; }

    public LandSequencePosition SequencePosition { get; }
}

internal static class CampaignCreationV9Factory
{
    public static CampaignCreatedV9 Create(
        string campaignId,
        string rulesetHash,
        CampaignSetupSnapshot setup,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        RandomStreamState randomState,
        LandSequencePosition sequencePosition)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(randomState);
        ArgumentNullException.ThrowIfNull(sequencePosition);
        CampaignWorldV5Validator.RequireValidContent(artifact);
        var legacyIdentity = ContentPackArtifact.Create(
            artifact.Definition.LegacyDefinition).Identity;
        var expectedPosition = Cna1979LandSequence.CreateTurn(scenario.Start.GameTurn)[0];
        if (!Cna1979Ruleset.IsCanonicalHash(rulesetHash)
            || !CampaignWorldV5Validator.ContainsScenario(artifact, scenario)
            || setup.Content.Pack != legacyIdentity
            || !string.Equals(setup.Content.ScenarioId, scenario.ScenarioId,
                StringComparison.Ordinal)
            || setup.InitialGameTurn != scenario.Start.GameTurn
            || setup.StageEntry.OperationStage != scenario.Start.OperationStage
            || randomState.ContractVersion != 1
            || !string.Equals(randomState.AlgorithmId, SandtableRandom.AlgorithmId,
                StringComparison.Ordinal)
            || randomState.NextByteCursor != 0
            || sequencePosition != expectedPosition)
        {
            throw new ArgumentException(
                "The CampaignCreated v9 inputs do not bind one valid initial campaign truth.");
        }

        var successorSetup = CampaignSetupSnapshotV5.FromPredecessor(
            setup,
            new CampaignContentV5Selection(artifact.Identity, scenario.ScenarioId));
        return new CampaignCreatedV9(
            campaignId,
            1,
            rulesetHash,
            successorSetup,
            CampaignWorldV5Factory.CreateInitial(artifact, scenario),
            randomState,
            sequencePosition);
    }
}
