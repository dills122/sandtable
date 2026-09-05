using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCreatedV10
{
    public const int CurrentContractVersion = 10;

    public CampaignCreatedV10(string campaignId, long stateVersion, string rulesetHash,
        CampaignSetupSnapshotV6 setup, CampaignWorldSnapshotV6 initialWorld,
        RandomStreamState randomState, LandSequencePosition sequencePosition, CampaignBreakdownFlow breakdownFlow)
    {
        CampaignId = ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        ArgumentOutOfRangeException.ThrowIfNotEqual(stateVersion, 1);
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(initialWorld);
        ArgumentNullException.ThrowIfNull(randomState);
        ArgumentNullException.ThrowIfNull(sequencePosition);
        ArgumentNullException.ThrowIfNull(breakdownFlow);
        StateVersion = stateVersion;
        RulesetHash = rulesetHash;
        Setup = setup;
        InitialWorld = initialWorld;
        RandomState = randomState;
        SequencePosition = sequencePosition;
        BreakdownFlow = breakdownFlow;
    }

    public int ContractVersion { get; } = CurrentContractVersion;
    public string CampaignId { get; }
    public long StateVersion { get; }
    public string RulesetHash { get; }
    public CampaignSetupSnapshotV6 Setup { get; }
    public CampaignWorldSnapshotV6 InitialWorld { get; }
    public RandomStreamState RandomState { get; }
    public LandSequencePosition SequencePosition { get; }
    public CampaignBreakdownFlow BreakdownFlow { get; }
}

internal static class CampaignCreationV10Factory
{
    public static CampaignCreatedV10 Create(string campaignId, string rulesetHash,
        CampaignSetupSnapshotV6 setup, ContentPackV6Artifact artifact, ContentScenario scenario,
        RandomStreamState randomState, LandSequencePosition sequencePosition)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(randomState);
        ArgumentNullException.ThrowIfNull(sequencePosition);
        CampaignWorldV6Validator.RequireValidContent(artifact);
        if (!Cna1979BreakdownRuleset.IsCanonicalHash(rulesetHash)
            || !CampaignWorldV6Validator.ContainsScenario(artifact, scenario)
            || setup.SchemaVersion != CampaignSetupSnapshotV6.CurrentSchemaVersion
            || setup.Content.Pack != artifact.Identity
            || setup.Content.ScenarioId != scenario.ScenarioId
            || !setup.IsSynthetic
            || setup.CapabilityProfileId != artifact.Definition.CapabilityProfileId
            || setup.SetupHash != CampaignSetupV6Codec.CalculateHash(setup)
            || setup.InitialGameTurn != scenario.Start.GameTurn
            || setup.StageEntry.OperationStage != scenario.Start.OperationStage
            || setup.OpeningPreamble != Cna1979SetupCatalog.OpeningPreamblePolicy
            || !Cna1979SetupCatalog.IsAdmittedWeatherPolicy(setup.Weather)
            || !Cna1979SetupCatalog.IsAdmittedStageEntryPolicy(setup.StageEntry, setup.InitialGameTurn)
            || randomState.ContractVersion != SandtableRandom.ContractVersion
            || randomState.AlgorithmId != SandtableRandom.AlgorithmId
            || randomState.NextByteCursor != 0
            || sequencePosition != Cna1979LandSequenceV4.CreateTurn(scenario.Start.GameTurn)[0])
        {
            throw new ArgumentException("CampaignCreated 10 inputs must bind one certified initial campaign truth.");
        }

        var world = CampaignWorldV6Factory.CreateInitial(artifact, scenario);
        if (!CampaignWorldV6Validator.IsValidInitial(world, artifact, scenario))
            throw new ArgumentException("CampaignCreated 10 requires certified initial World 6.");
        return new CampaignCreatedV10(campaignId, 1, rulesetHash, setup, world, randomState,
            sequencePosition, new CampaignBreakdownFlow.Idle());
    }

    public static CampaignSnapshotV11 CreateSnapshot(CampaignCreatedV10 created,
        ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        _ = CampaignCreatedV10Serializer.Serialize(created, artifact, scenario);
        var snapshot = new CampaignSnapshotV11(11, created.CampaignId, created.StateVersion,
            created.RulesetHash, created.Setup, created.InitialWorld, null, [], [], created.RandomState,
            CampaignPositionV11.FromSequence(created.SequencePosition), null, created.BreakdownFlow);
        if (!CampaignSnapshotV11Validator.IsValid(snapshot, artifact, scenario))
            throw new ArgumentException("CampaignCreated 10 does not project a valid initial Snapshot 11.");
        return snapshot;
    }
}
