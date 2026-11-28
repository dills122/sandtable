using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Actions;

// E4b (#655): native Reaction legal actions over World V6. The window (and its frozen opportunities)
// is the one opened by the trigger move from the pre-move World; nothing here reads a legacy snapshot.
internal static class CampaignNativeReactionActions
{
    internal static LandSide? Owner(CampaignActionAudience audience) => audience switch
    {
        CampaignActionAudience.Axis => LandSide.Axis,
        CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
        _ => null,
    };

    internal static CampaignActionCandidate[] Derive(CampaignNativeReactionCut cut, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(cut);
        var window = cut.Window;
        var windowId = window.WindowId.Value;
        var unresolved = window.FrozenOpportunities
            .Where(value => !window.ResolvedOpportunityIds.Contains(value.OpportunityId)).ToArray();
        var anyOptions = unresolved.Any(value => cut.MoveOptions(value).Length > 0);
        var active = window.ActiveOpportunityId;
        if (audience == CampaignActionAudience.System)
            return active is null && !anyOptions
                ? [new CloseReactionWindowNoEligibleAction(windowId)]
                : [new CloseReactionWindowUnavailableAction(windowId), new CloseReactionWindowTimeoutAction(windowId)];
        // Only the reacting side sees (or may act on) the window; the phasing side is waiting.
        if (Owner(audience) != window.ReactingSide) return [];
        var candidates = new List<CampaignActionCandidate>();
        foreach (var opportunity in unresolved)
            foreach (var option in cut.MoveOptions(opportunity))
                candidates.Add(new MoveReactingElementAction(windowId, opportunity.OpportunityId.Value,
                    option.OriginLocationId, option.DestinationLocationId, option.CostBreakdown));
        if (active is null)
        {
            if (anyOptions) candidates.Add(new DeclineReactionWindowAction(windowId));
        }
        else candidates.Add(new CompleteReactionParticipantAction(windowId, active.Value));
        return [.. candidates];
    }
}
