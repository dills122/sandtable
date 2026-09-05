using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal sealed record ElementMovementStopped(string CampaignId, long StateVersion, long PriorStateVersion,
    string RulesetHash, string FromPositionId, string ActionId, LandSide ActingSide, string SubmittedRouteId,
    CampaignBreakdownFlow BreakdownFlowAfter) : CampaignSuccessorEvent(1, CampaignId, StateVersion);

internal sealed record ReactionParticipantCompletedV2(string CampaignId, long StateVersion, long PriorStateVersion,
    string FromPositionId, LandSide ActingSide, string ActionId, string SubmittedWindowId,
    string SubmittedOpportunityId, CampaignReactionWindowId WindowId, CampaignReactionOpportunityId OpportunityId,
    CampaignReactionWindow ReactionWindowAfter, string RulesetHash, CampaignBreakdownFlow BreakdownFlowAfter)
    : CampaignSuccessorEvent(2, CampaignId, StateVersion)
{
    public ReactionParticipantCompletedReplayInput ToReplayInput() => new(CampaignId, PriorStateVersion,
        FromPositionId, ActingSide, ActionId, SubmittedWindowId, SubmittedOpportunityId, WindowId, OpportunityId);
}

internal sealed record ReactionWindowClosedV2 : CampaignSuccessorEvent
{
    public ReactionWindowClosedV2(string campaignId, long stateVersion, long priorStateVersion,
        string fromPositionId, LandSide? actingSide, string actionId, string submittedWindowId,
        CampaignReactionWindowId windowId, CampaignReactionWindowCloseReason reason,
        IEnumerable<CampaignReactionOpportunityId> closedOpportunityIds, LandSequencePosition suspendedSequencePosition,
        string rulesetHash, CampaignBreakdownFlow breakdownFlowAfter) : base(2, campaignId, stateVersion)
    {
        PriorStateVersion = priorStateVersion; FromPositionId = fromPositionId; ActingSide = actingSide;
        ActionId = actionId; SubmittedWindowId = submittedWindowId; WindowId = windowId; Reason = reason;
        var ids = closedOpportunityIds.ToArray();
        if (ids.Any(x => x is null) || ids.Distinct().Count() != ids.Length)
            throw new ArgumentException("Closed opportunity IDs must be unique.", nameof(closedOpportunityIds));
        ClosedOpportunityIds = Array.AsReadOnly(ids.OrderBy(x => x.Value, StringComparer.Ordinal).ToArray());
        SuspendedSequencePosition = suspendedSequencePosition; RulesetHash = rulesetHash; BreakdownFlowAfter = breakdownFlowAfter;
    }
    public long PriorStateVersion { get; }
    public string FromPositionId { get; }
    public LandSide? ActingSide { get; }
    public string ActionId { get; }
    public string SubmittedWindowId { get; }
    public CampaignReactionWindowId WindowId { get; }
    public CampaignReactionWindowCloseReason Reason { get; }
    public IReadOnlyList<CampaignReactionOpportunityId> ClosedOpportunityIds { get; }
    public LandSequencePosition SuspendedSequencePosition { get; }
    public string RulesetHash { get; }
    public CampaignBreakdownFlow BreakdownFlowAfter { get; }
    public ReactionWindowClosedReplayInput ToReplayInput() => new(CampaignId, PriorStateVersion,
        FromPositionId, ActingSide, ActionId, SubmittedWindowId, WindowId, Reason);
}

internal sealed record MovementSegmentCompletedV2(string CampaignId, long StateVersion, long PriorStateVersion,
    string FromPositionId, int GameTurn, int OperationStage, LandSide ActingSide,
    LandSequencePosition SequencePosition, string RulesetHash, CampaignBreakdownFlow BreakdownFlowAfter)
    : CampaignSuccessorEvent(2, CampaignId, StateVersion);

internal sealed record BreakdownSegmentCompleted : CampaignSuccessorEvent
{
    public BreakdownSegmentCompleted(string campaignId, long stateVersion, long priorStateVersion,
        string rulesetHash, string fromPositionId, string actionId, LandSequencePosition sequencePosition,
        CampaignBreakdownFlow breakdownFlowAfter, IEnumerable<RuleReference> sources) : base(1, campaignId, stateVersion)
    {
        PriorStateVersion = priorStateVersion; RulesetHash = rulesetHash; FromPositionId = fromPositionId;
        ActionId = actionId; SequencePosition = sequencePosition; BreakdownFlowAfter = breakdownFlowAfter;
        Sources = RuleReferenceValidation.CopySources(sources, nameof(sources));
    }
    public long PriorStateVersion { get; }
    public string RulesetHash { get; }
    public string FromPositionId { get; }
    public string ActionId { get; }
    public LandSequencePosition SequencePosition { get; }
    public CampaignBreakdownFlow BreakdownFlowAfter { get; }
    public IReadOnlyList<RuleReference> Sources { get; }
}
