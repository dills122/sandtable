using Cna.Core.Content;

namespace Cna.Core.Campaigns;

public sealed record CampaignCreationRequest
{
    public const int CurrentContractVersion = 1;

    public CampaignCreationRequest(int contractVersion, string campaignId, string rulesetHash,
        ulong seed, string setupId, string setupHash, string contentPackId, string contentHash,
        string scenarioId)
    {
        ContractVersion = contractVersion;
        CampaignId = campaignId;
        RulesetHash = rulesetHash;
        Seed = seed;
        SetupId = setupId;
        SetupHash = setupHash;
        ContentPackId = contentPackId;
        ContentHash = contentHash;
        ScenarioId = scenarioId;
    }

    public int ContractVersion { get; }
    public string CampaignId { get; }
    public string RulesetHash { get; }
    public ulong Seed { get; }
    public string SetupId { get; }
    public string SetupHash { get; }
    public string ContentPackId { get; }
    public string ContentHash { get; }
    public string ScenarioId { get; }
}

public enum CampaignCreationRejectionReason
{
    None,
    InvalidRequest,
    UnsupportedRuleset,
    UnknownSetup,
    SetupHashMismatch,
    UnknownContent,
    ContentHashMismatch,
    UnknownScenario,
    SetupContentMismatch,
    ScenarioStartMismatch,
    InvalidState,
}

public sealed record CampaignAuthorityCreationResult
{
    private CampaignAuthorityCreationResult(CampaignAuthorityHandle? handle,
        CampaignCreationRejectionReason rejectionReason)
    {
        Handle = handle;
        RejectionReason = rejectionReason;
    }

    public bool IsCreated => Handle is not null;
    public CampaignAuthorityHandle? Handle { get; }
    public CampaignCreationRejectionReason RejectionReason { get; }

    internal static CampaignAuthorityCreationResult Created(CampaignAuthorityHandle handle) =>
        new(handle, CampaignCreationRejectionReason.None);
    internal static CampaignAuthorityCreationResult Rejected(CampaignCreationRejectionReason reason) =>
        new(null, reason);
}

public static class CampaignAuthority
{
    public static CampaignAuthorityCreationResult Create(CampaignCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ContractVersion != CampaignCreationRequest.CurrentContractVersion)
            return CampaignAuthorityCreationResult.Rejected(CampaignCreationRejectionReason.InvalidRequest);

        var command = new CreateCampaign(request.CampaignId, request.RulesetHash, request.Seed,
            request.SetupId, request.SetupHash, request.ContentPackId, request.ContentHash,
            request.ScenarioId);
        var result = CampaignEngine.DecideCreation(null, command, Cna1979SyntheticContentResolver.Instance);
        if (!result.IsAccepted)
            return CampaignAuthorityCreationResult.Rejected(Map(result.RejectionReason));

        var created = (CampaignCreated)result.Events[0];
        var resolution = Cna1979SyntheticContentResolver.Instance.Resolve(
            created.Setup.Content.Pack.PackId, created.Setup.Content.Pack.Hash);
        if (!resolution.IsResolved)
            return CampaignAuthorityCreationResult.Rejected(CampaignCreationRejectionReason.InvalidState);
        var context = CampaignContentContext.Create(resolution.Artifact!, created.Setup.Content.ScenarioId);
        var snapshot = CampaignProjector.Apply(null, created, context);
        return CampaignAuthorityCreationResult.Created(new CampaignAuthorityHandle(snapshot, context));
    }

    private static CampaignCreationRejectionReason Map(CampaignCommandRejectionReason reason) => reason switch
    {
        CampaignCommandRejectionReason.InvalidCommand => CampaignCreationRejectionReason.InvalidRequest,
        CampaignCommandRejectionReason.UnsupportedRuleset => CampaignCreationRejectionReason.UnsupportedRuleset,
        CampaignCommandRejectionReason.UnknownSetup => CampaignCreationRejectionReason.UnknownSetup,
        CampaignCommandRejectionReason.SetupHashMismatch => CampaignCreationRejectionReason.SetupHashMismatch,
        CampaignCommandRejectionReason.UnknownContent => CampaignCreationRejectionReason.UnknownContent,
        CampaignCommandRejectionReason.ContentHashMismatch => CampaignCreationRejectionReason.ContentHashMismatch,
        CampaignCommandRejectionReason.UnknownScenario => CampaignCreationRejectionReason.UnknownScenario,
        CampaignCommandRejectionReason.SetupContentMismatch => CampaignCreationRejectionReason.SetupContentMismatch,
        CampaignCommandRejectionReason.ScenarioStartMismatch => CampaignCreationRejectionReason.ScenarioStartMismatch,
        _ => CampaignCreationRejectionReason.InvalidState,
    };
}
