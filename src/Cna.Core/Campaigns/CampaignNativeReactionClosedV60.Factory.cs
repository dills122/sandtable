namespace Cna.Core.Campaigns;

internal sealed partial record CampaignNativeReactionClosedV60
{
    internal static CampaignNativeReactionClosedV60 Create(object predecessor, CampaignWorldSnapshotV6 world,
        CampaignContentContext context, int gameTurn, string rulesetHash, CampaignOperationStageWeather weather,
        CampaignReactionWindowId windowId, CampaignNativeReactionCloseKind closeKind, CampaignReactionOpportunityId? opportunityId = null)
    {
        var cut = CampaignNativeReactionCut.Bind(predecessor, world, context, gameTurn, rulesetHash, weather, windowId);
        var window = cut.Window;
        CampaignReactionWindow? after = null;
        if (closeKind == CampaignNativeReactionCloseKind.ParticipantCompleted)
        {
            if (opportunityId is null || window.ActiveOpportunityId != opportunityId)
                throw new InvalidOperationException("Only an active Reaction participant may complete.");
            var completed = CampaignBreakdownLifecycleFactory.CompleteParticipant(window, opportunityId);
            if (completed.ResolvedOpportunityIds.Count != completed.FrozenOpportunities.Count) after = completed;
        }
        else
        {
            if (opportunityId is not null) throw new InvalidOperationException("Window closure cannot select a participant.");
            var reason = closeKind switch
            {
                CampaignNativeReactionCloseKind.PlayerDecline => CampaignReactionWindowCloseReason.PlayerDecline,
                CampaignNativeReactionCloseKind.Timeout => CampaignReactionWindowCloseReason.Timeout,
                CampaignNativeReactionCloseKind.ScriptedUnavailable => CampaignReactionWindowCloseReason.ScriptedUnavailable,
                CampaignNativeReactionCloseKind.NoEligibleReactor => CampaignReactionWindowCloseReason.NoEligibleReactor,
                _ => throw new InvalidOperationException("Unsupported Reaction close reason."),
            };
            var anyOptions = window.FrozenOpportunities.Any(x => cut.MoveOptions(x).Length > 0);
            CampaignBreakdownLifecycleFactory.RequireCloseReason(reason, window.ActiveOpportunityId is not null, anyOptions);
        }
        return new(CurrentContractVersion, cut.CampaignId, cut.Version, cut.PriorEventHash, gameTurn,
            windowId, window.ReactingSide, cut.WorldHash, cut.WorldHash, closeKind, opportunityId,
            window.ReactingPosition.SuspendedMovementPosition.PositionId, after);
    }

    internal CampaignWorldSnapshotV6 Replay(object predecessor, CampaignWorldSnapshotV6 world, CampaignContentContext context,
        int gameTurn, string rulesetHash, CampaignOperationStageWeather weather)
    {
        var expected = Create(predecessor, world, context, gameTurn, rulesetHash, weather, WindowId, CloseKind, OpportunityId);
        if (this != expected) throw new InvalidOperationException("Native Reaction closure does not match replayed authority.");
        return world;
    }
}
