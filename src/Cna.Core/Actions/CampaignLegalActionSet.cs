using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Actions;

public sealed record CampaignLegalActionSet
{
    public const int CurrentContractVersion = 2;
    public const string CurrentPolicyId = "sandtable.legal-actions.v2";

    internal CampaignLegalActionSet(string campaignId, long stateVersion, string rulesetHash,
        string positionId, CampaignActionAudience audience,
        IReadOnlyList<CampaignActionCandidate> candidates)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(stateVersion, 1);
        if (!Cna1979Ruleset.IsCanonicalHash(rulesetHash))
        {
            throw new ArgumentException("The action set must use the canonical ruleset hash.",
                nameof(rulesetHash));
        }
        if (!Enum.IsDefined(audience)) throw new ArgumentOutOfRangeException(nameof(audience));
        ArgumentNullException.ThrowIfNull(candidates);
        var copy = candidates.ToArray();
        if (copy.Any(candidate => candidate is null)
            || copy.Any(candidate => candidate.ContractVersion != CampaignActionCandidate.CurrentContractVersion)
            || copy.Select(candidate => candidate.ActionId).Distinct(StringComparer.Ordinal).Count()
                != copy.Length)
        {
            throw new ArgumentException("Candidates must be non-null, current, and unique.",
                nameof(candidates));
        }
        foreach (var candidate in copy)
        {
            _ = ContentContractGuards.RequireSha256(candidate.ActionId, nameof(candidates));
        }

        ContractVersion = CurrentContractVersion;
        PolicyId = CurrentPolicyId;
        CampaignId = ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        StateVersion = stateVersion;
        RulesetHash = rulesetHash;
        PositionId = ContentContractGuards.RequireStableId(positionId, nameof(positionId));
        Audience = audience;
        Candidates = Array.AsReadOnly(copy
            .OrderBy(candidate => candidate.Kind, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.ActionId, StringComparer.Ordinal)
            .ToArray());
    }

    public int ContractVersion { get; }
    public string PolicyId { get; }
    public string CampaignId { get; }
    /// <summary>
    /// Gets the revision visible to this audience. A non-empty current set remains bound to the
    /// exact authority revision used by submission.
    /// </summary>
    public long StateVersion { get; }
    public string RulesetHash { get; }
    public string PositionId { get; }
    public CampaignActionAudience Audience { get; }
    public IReadOnlyList<CampaignActionCandidate> Candidates { get; }

    public bool Equals(CampaignLegalActionSet? other) => ReferenceEquals(this, other)
        || (other is not null && ContractVersion == other.ContractVersion && PolicyId == other.PolicyId
            && CampaignId == other.CampaignId && StateVersion == other.StateVersion
            && RulesetHash == other.RulesetHash && PositionId == other.PositionId
            && Audience == other.Audience && Candidates.SequenceEqual(other.Candidates));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion); hash.Add(PolicyId, StringComparer.Ordinal);
        hash.Add(CampaignId, StringComparer.Ordinal); hash.Add(StateVersion);
        hash.Add(RulesetHash, StringComparer.Ordinal); hash.Add(PositionId, StringComparer.Ordinal);
        hash.Add(Audience); foreach (var candidate in Candidates) hash.Add(candidate);
        return hash.ToHashCode();
    }
}

public enum CampaignLegalActionQueryRejectionReason
{
    None,
    InvalidAudience,
    InvalidState,
}

public sealed record CampaignLegalActionQueryResult
{
    private CampaignLegalActionQueryResult(CampaignLegalActionSet? actionSet,
        CampaignLegalActionQueryRejectionReason rejectionReason)
    { ActionSet = actionSet; RejectionReason = rejectionReason; }

    public bool IsSuccessful => ActionSet is not null;
    public CampaignLegalActionSet? ActionSet { get; }
    public CampaignLegalActionQueryRejectionReason RejectionReason { get; }
    internal static CampaignLegalActionQueryResult Success(CampaignLegalActionSet set) => new(set,
        CampaignLegalActionQueryRejectionReason.None);
    internal static CampaignLegalActionQueryResult Rejected(CampaignLegalActionQueryRejectionReason reason) =>
        new(null, reason);
}
