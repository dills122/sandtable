using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCreationExecutionResult
{
    private CampaignCreationExecutionResult(
        CampaignCreatedV9? currentCreatedEvent,
        CampaignSnapshotV10? currentSnapshot,
        CampaignCreated? createdEvent,
        CampaignSnapshot? snapshot,
        CampaignContentContext? context,
        CampaignCreationRejectionReason rejectionReason)
    {
        CurrentCreatedEvent = currentCreatedEvent;
        CurrentSnapshot = currentSnapshot;
        CreatedEvent = createdEvent;
        Snapshot = snapshot;
        Context = context;
        RejectionReason = rejectionReason;
    }

    public bool IsCreated => CurrentSnapshot is not null;
    public CampaignCreatedV9? CurrentCreatedEvent { get; }
    public CampaignSnapshotV10? CurrentSnapshot { get; }
    public CampaignCreated? CreatedEvent { get; }
    public CampaignSnapshot? Snapshot { get; }
    public CampaignContentContext? Context { get; }
    public CampaignCreationRejectionReason RejectionReason { get; }

    public static CampaignCreationExecutionResult Created(
        CampaignCreatedV9 currentCreatedEvent,
        CampaignSnapshotV10 currentSnapshot,
        CampaignCreated createdEvent,
        CampaignSnapshot snapshot,
        CampaignContentContext context) =>
        new(
            currentCreatedEvent ?? throw new ArgumentNullException(nameof(currentCreatedEvent)),
            currentSnapshot ?? throw new ArgumentNullException(nameof(currentSnapshot)),
            createdEvent ?? throw new ArgumentNullException(nameof(createdEvent)),
            snapshot ?? throw new ArgumentNullException(nameof(snapshot)),
            context ?? throw new ArgumentNullException(nameof(context)),
            CampaignCreationRejectionReason.None);

    public static CampaignCreationExecutionResult Rejected(
        CampaignCreationRejectionReason rejectionReason)
    {
        if (rejectionReason == CampaignCreationRejectionReason.None)
            throw new ArgumentOutOfRangeException(nameof(rejectionReason));
        return new CampaignCreationExecutionResult(null, null, null, null, null, rejectionReason);
    }
}

internal static class CampaignCreationExecution
{
    public static CampaignCreationExecutionResult Execute(CampaignCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ContractVersion != CampaignCreationRequest.CurrentContractVersion)
            return CampaignCreationExecutionResult.Rejected(
                CampaignCreationRejectionReason.InvalidRequest);

        if (string.IsNullOrWhiteSpace(request.CampaignId)
            || string.IsNullOrWhiteSpace(request.SetupId)
            || string.IsNullOrWhiteSpace(request.SetupHash)
            || string.IsNullOrWhiteSpace(request.ContentPackId)
            || string.IsNullOrWhiteSpace(request.ContentHash)
            || string.IsNullOrWhiteSpace(request.ScenarioId))
        {
            return CampaignCreationExecutionResult.Rejected(
                CampaignCreationRejectionReason.InvalidRequest);
        }

        if (!Cna.Core.Rules.Cna1979Ruleset.IsCanonicalHash(request.RulesetHash))
        {
            return CampaignCreationExecutionResult.Rejected(
                CampaignCreationRejectionReason.UnsupportedRuleset);
        }

        if (!Cna.Core.Setups.Cna1979SetupCatalog.TryGet(
                request.SetupId,
                out var definition))
        {
            return CampaignCreationExecutionResult.Rejected(
                CampaignCreationRejectionReason.UnknownSetup);
        }

        var resolution = Cna1979SyntheticContentResolver.Instance.ResolveV5(
            request.ContentPackId,
            request.ContentHash);
        if (!resolution.IsResolved)
        {
            return CampaignCreationExecutionResult.Rejected(
                resolution.RejectionReason == ContentCatalogRejectionReason.UnknownPackId
                    ? CampaignCreationRejectionReason.UnknownContent
                    : CampaignCreationRejectionReason.ContentHashMismatch);
        }

        var artifact = resolution.Artifact!;
        var scenario = artifact.Definition.LegacyDefinition.Scenarios.SingleOrDefault(value =>
            string.Equals(value.ScenarioId, request.ScenarioId, StringComparison.Ordinal));
        if (scenario is null)
        {
            return CampaignCreationExecutionResult.Rejected(
                CampaignCreationRejectionReason.UnknownScenario);
        }

        if (!string.Equals(definition.Content.Pack.PackId, artifact.Identity.PackId,
                StringComparison.Ordinal)
            || !string.Equals(definition.Content.ScenarioId, scenario.ScenarioId,
                StringComparison.Ordinal))
        {
            return CampaignCreationExecutionResult.Rejected(
                CampaignCreationRejectionReason.SetupContentMismatch);
        }

        var predecessor = CampaignSetupSnapshot.FromDefinition(definition);
        var successorSetup = CampaignSetupSnapshotV5.FromPredecessor(
            predecessor,
            new CampaignContentV5Selection(artifact.Identity, scenario.ScenarioId));
        if (!string.Equals(request.SetupHash, successorSetup.SetupHash, StringComparison.Ordinal))
        {
            return CampaignCreationExecutionResult.Rejected(
                CampaignCreationRejectionReason.SetupHashMismatch);
        }

        if (definition.InitialGameTurn != scenario.Start.GameTurn
            || definition.StageEntry.OperationStage != scenario.Start.OperationStage)
        {
            return CampaignCreationExecutionResult.Rejected(
                CampaignCreationRejectionReason.ScenarioStartMismatch);
        }

        try
        {
            var created = CampaignCreationV9Factory.Create(
                request.CampaignId,
                request.RulesetHash,
                predecessor,
                artifact,
                scenario,
                Cna.Core.Randomness.SandtableRandom.Create(request.Seed),
                Cna.Core.Rules.Cna1979LandSequence.CreateTurn(scenario.Start.GameTurn)[0]);
            var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
            var snapshot = CampaignV10Projector.ApplyCreation(created, artifact, scenario);
            var legacySnapshot = CampaignV10LegacyBridge.ToLegacy(snapshot, context);
            var legacyCreated = new CampaignCreated(
                legacySnapshot.CampaignId,
                legacySnapshot.StateVersion,
                legacySnapshot.RulesetHash,
                legacySnapshot.Setup,
                legacySnapshot.World,
                legacySnapshot.RandomState,
                legacySnapshot.SequencePosition);
            return CampaignCreationExecutionResult.Created(
                created,
                snapshot,
                legacyCreated,
                legacySnapshot,
                context);
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidCampaignHistoryException
            or InvalidOperationException)
        {
            return CampaignCreationExecutionResult.Rejected(
                CampaignCreationRejectionReason.InvalidState);
        }
    }
}
