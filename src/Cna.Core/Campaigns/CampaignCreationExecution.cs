using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal sealed record CampaignCreationExecutionResult
{
    private CampaignCreationExecutionResult(
        CampaignCreated? createdEvent,
        CampaignSnapshot? snapshot,
        CampaignContentContext? context,
        CampaignCreationRejectionReason rejectionReason)
    {
        CreatedEvent = createdEvent;
        Snapshot = snapshot;
        Context = context;
        RejectionReason = rejectionReason;
    }

    public bool IsCreated => Snapshot is not null;
    public CampaignCreated? CreatedEvent { get; }
    public CampaignSnapshot? Snapshot { get; }
    public CampaignContentContext? Context { get; }
    public CampaignCreationRejectionReason RejectionReason { get; }

    public static CampaignCreationExecutionResult Created(
        CampaignCreated createdEvent,
        CampaignSnapshot snapshot,
        CampaignContentContext context) =>
        new(
            createdEvent ?? throw new ArgumentNullException(nameof(createdEvent)),
            snapshot ?? throw new ArgumentNullException(nameof(snapshot)),
            context ?? throw new ArgumentNullException(nameof(context)),
            CampaignCreationRejectionReason.None);

    public static CampaignCreationExecutionResult Rejected(
        CampaignCreationRejectionReason rejectionReason)
    {
        if (rejectionReason == CampaignCreationRejectionReason.None)
            throw new ArgumentOutOfRangeException(nameof(rejectionReason));
        return new CampaignCreationExecutionResult(null, null, null, rejectionReason);
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

        var command = new CreateCampaign(request.CampaignId, request.RulesetHash, request.Seed,
            request.SetupId, request.SetupHash, request.ContentPackId, request.ContentHash,
            request.ScenarioId);
        var decision = CampaignEngine.DecideCreation(
            null,
            command,
            Cna1979SyntheticContentResolver.Instance);
        if (!decision.IsAccepted)
            return CampaignCreationExecutionResult.Rejected(Map(decision.RejectionReason));
        if (decision.Events.Count != 1 || decision.Events[0] is not CampaignCreated created)
            return CampaignCreationExecutionResult.Rejected(CampaignCreationRejectionReason.InvalidState);

        var resolution = Cna1979SyntheticContentResolver.Instance.Resolve(
            created.Setup.Content.Pack.PackId,
            created.Setup.Content.Pack.Hash);
        if (!resolution.IsResolved)
            return CampaignCreationExecutionResult.Rejected(CampaignCreationRejectionReason.InvalidState);

        var context = CampaignContentContext.Create(
            resolution.Artifact!,
            created.Setup.Content.ScenarioId);
        var snapshot = CampaignProjector.Apply(null, created, context);
        return CampaignCreationExecutionResult.Created(created, snapshot, context);
    }

    private static CampaignCreationRejectionReason Map(CampaignCommandRejectionReason reason) =>
        reason switch
        {
            CampaignCommandRejectionReason.InvalidCommand =>
                CampaignCreationRejectionReason.InvalidRequest,
            CampaignCommandRejectionReason.UnsupportedRuleset =>
                CampaignCreationRejectionReason.UnsupportedRuleset,
            CampaignCommandRejectionReason.UnknownSetup => CampaignCreationRejectionReason.UnknownSetup,
            CampaignCommandRejectionReason.SetupHashMismatch =>
                CampaignCreationRejectionReason.SetupHashMismatch,
            CampaignCommandRejectionReason.UnknownContent =>
                CampaignCreationRejectionReason.UnknownContent,
            CampaignCommandRejectionReason.ContentHashMismatch =>
                CampaignCreationRejectionReason.ContentHashMismatch,
            CampaignCommandRejectionReason.UnknownScenario =>
                CampaignCreationRejectionReason.UnknownScenario,
            CampaignCommandRejectionReason.SetupContentMismatch =>
                CampaignCreationRejectionReason.SetupContentMismatch,
            CampaignCommandRejectionReason.ScenarioStartMismatch =>
                CampaignCreationRejectionReason.ScenarioStartMismatch,
            _ => CampaignCreationRejectionReason.InvalidState,
        };
}
