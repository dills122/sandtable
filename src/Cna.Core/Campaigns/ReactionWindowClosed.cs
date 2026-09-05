using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal enum CampaignReactionWindowCloseReason
{
    PlayerDecline = 1,
    ScriptedUnavailable = 2,
    Timeout = 3,
    NoEligibleReactor = 4,
}

internal sealed record ReactionWindowClosed : CampaignSuccessorEvent
{
    public const int CurrentContractVersion = 1;

    public ReactionWindowClosed(
        string campaignId,
        long stateVersion,
        long priorStateVersion,
        string fromPositionId,
        LandSide? actingSide,
        string actionId,
        string submittedWindowId,
        CampaignReactionWindowId windowId,
        CampaignReactionWindowCloseReason reason,
        IEnumerable<CampaignReactionOpportunityId> closedOpportunityIds,
        LandSequencePosition resumedSequencePosition)
        : base(CurrentContractVersion, campaignId, stateVersion)
    {
        ArgumentNullException.ThrowIfNull(windowId);
        ArgumentNullException.ThrowIfNull(closedOpportunityIds);
        ArgumentNullException.ThrowIfNull(resumedSequencePosition);
        PriorStateVersion = priorStateVersion;
        FromPositionId = ContentContractGuards.RequireStableId(
            fromPositionId,
            nameof(fromPositionId));
        ActingSide = actingSide;
        ActionId = ContentContractGuards.RequireSha256(actionId, nameof(actionId));
        SubmittedWindowId = ContentContractGuards.RequireSha256(
            submittedWindowId,
            nameof(submittedWindowId));
        WindowId = windowId;
        Reason = reason;
        var closed = closedOpportunityIds.ToArray();
        if (closed.Any(value => value is null)
            || closed.Select(value => value.Value)
                .Distinct(StringComparer.Ordinal).Count() != closed.Length)
        {
            throw new ArgumentException(
                "Closed Reaction opportunity IDs must be non-null and unique.",
                nameof(closedOpportunityIds));
        }

        ClosedOpportunityIds = Array.AsReadOnly(closed
            .OrderBy(value => value.Value, StringComparer.Ordinal)
            .ToArray());
        ResumedSequencePosition = resumedSequencePosition;
        ValidateContract();
    }

    public long PriorStateVersion { get; }

    public string FromPositionId { get; }

    public LandSide? ActingSide { get; }

    public string ActionId { get; }

    public string SubmittedWindowId { get; }

    public CampaignReactionWindowId WindowId { get; }

    public CampaignReactionWindowCloseReason Reason { get; }

    public IReadOnlyList<CampaignReactionOpportunityId> ClosedOpportunityIds { get; }

    public LandSequencePosition ResumedSequencePosition { get; }

    public ReactionWindowClosedReplayInput ToReplayInput() => new(
        CampaignId,
        PriorStateVersion,
        FromPositionId,
        ActingSide,
        ActionId,
        SubmittedWindowId,
        WindowId,
        Reason);

    internal void ValidateContract()
    {
        CampaignSequenceV5Guards.RequireMaterializedMovement(ResumedSequencePosition);
        if (ContractVersion != CurrentContractVersion
            || StateVersion < 13
            || PriorStateVersion < 12
            || checked(PriorStateVersion + 1) != StateVersion
            || !Enum.IsDefined(Reason)
            || (ActingSide is not null && !Enum.IsDefined(ActingSide.Value))
            || (Reason == CampaignReactionWindowCloseReason.PlayerDecline)
                != (ActingSide is not null)
            || !string.Equals(
                FromPositionId,
                ResumedSequencePosition.PositionId,
                StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The ReactionWindowClosed event contract is invalid.");
        }
    }

    public bool Equals(ReactionWindowClosed? other) => ReferenceEquals(this, other)
        || (other is not null
            && ContractVersion == other.ContractVersion
            && string.Equals(CampaignId, other.CampaignId, StringComparison.Ordinal)
            && StateVersion == other.StateVersion
            && PriorStateVersion == other.PriorStateVersion
            && string.Equals(FromPositionId, other.FromPositionId, StringComparison.Ordinal)
            && ActingSide == other.ActingSide
            && string.Equals(ActionId, other.ActionId, StringComparison.Ordinal)
            && string.Equals(SubmittedWindowId, other.SubmittedWindowId, StringComparison.Ordinal)
            && WindowId == other.WindowId
            && Reason == other.Reason
            && ClosedOpportunityIds.SequenceEqual(other.ClosedOpportunityIds)
            && ResumedSequencePosition == other.ResumedSequencePosition);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(ContractVersion);
        hash.Add(CampaignId, StringComparer.Ordinal);
        hash.Add(StateVersion);
        hash.Add(PriorStateVersion);
        hash.Add(FromPositionId, StringComparer.Ordinal);
        hash.Add(ActingSide);
        hash.Add(ActionId, StringComparer.Ordinal);
        hash.Add(SubmittedWindowId, StringComparer.Ordinal);
        hash.Add(WindowId);
        hash.Add(Reason);
        foreach (var opportunityId in ClosedOpportunityIds) hash.Add(opportunityId);
        hash.Add(ResumedSequencePosition);
        return hash.ToHashCode();
    }
}

internal sealed record ReactionWindowClosedReplayInput(
    string CampaignId,
    long PriorStateVersion,
    string FromPositionId,
    LandSide? ActingSide,
    string ActionId,
    string SubmittedWindowId,
    CampaignReactionWindowId WindowId,
    CampaignReactionWindowCloseReason Reason);

internal delegate ReactionWindowClosed CampaignReactionWindowClosedReconstructor(
    CampaignSnapshotV10 prior,
    ContentPackV5Artifact artifact,
    ContentScenario scenario,
    ReactionWindowClosedReplayInput input);
