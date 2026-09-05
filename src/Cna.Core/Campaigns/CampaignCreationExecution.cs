using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCreationExecutionResult
{
    private CampaignCreationExecutionResult(
        CampaignCreatedV10? currentCreatedEvent,
        CampaignSnapshotV11? currentSnapshot,
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
    public CampaignCreatedV10? CurrentCreatedEvent { get; }
    public CampaignSnapshotV11? CurrentSnapshot { get; }
    public CampaignCreated? CreatedEvent { get; }
    public CampaignSnapshot? Snapshot { get; }
    public CampaignContentContext? Context { get; }
    public CampaignCreationRejectionReason RejectionReason { get; }

    public static CampaignCreationExecutionResult Created(
        CampaignCreatedV10 currentCreatedEvent,
        CampaignSnapshotV11 currentSnapshot,
        CampaignContentContext context) =>
        new(
            currentCreatedEvent ?? throw new ArgumentNullException(nameof(currentCreatedEvent)),
            currentSnapshot ?? throw new ArgumentNullException(nameof(currentSnapshot)),
            null,
            null,
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

        if (!Cna.Core.Setups.Cna1979BreakdownSetupCatalog.TryGet(
                request.SetupId,
                out var definition))
        {
            return CampaignCreationExecutionResult.Rejected(
                CampaignCreationRejectionReason.UnknownSetup);
        }

        var resolution = Cna1979SyntheticContentResolver.Instance.ResolveV6(
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

        var successorSetup = CampaignSetupSnapshotV6.FromDefinition(definition);
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
            if (successorSetup.CapabilityProfileId != artifact.Definition.CapabilityProfileId
                || !ContentPackV6Validator.Validate(artifact.Definition).IsValid)
                return CampaignCreationExecutionResult.Rejected(
                    CampaignCreationRejectionReason.UnsupportedCapabilityProfile);
            var created = CampaignCreationV10Factory.Create(
                request.CampaignId, request.RulesetHash, successorSetup, artifact, scenario,
                Cna.Core.Randomness.SandtableRandom.Create(request.Seed),
                Cna.Core.Rules.Cna1979LandSequenceV4.CreateTurn(scenario.Start.GameTurn)[0]);
            var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
            var snapshot = CampaignCreationV10Factory.CreateSnapshot(created, artifact, scenario);
            return CampaignCreationExecutionResult.Created(created, snapshot, context);
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
