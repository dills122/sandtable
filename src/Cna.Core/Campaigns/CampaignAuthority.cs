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
        var execution = CampaignCreationExecution.Execute(request);
        return execution.IsCreated
            ? CampaignAuthorityCreationResult.Created(
                new CampaignAuthorityHandle(execution.CurrentSnapshot!, execution.Context!))
            : CampaignAuthorityCreationResult.Rejected(execution.RejectionReason);
    }
}
