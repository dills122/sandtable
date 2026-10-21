using Cna.Core.Campaigns;

namespace Cna.Core.Visibility;

// A committed native movement event with a frozen Reaction opportunity is
// direct, source-backed sight for that reacting formation. This source feeds
// the same permanent contact ledger as later detection and sharing sources.
internal static class CampaignContactReactionSource
{
    public const string RuleId = "sandtable.visibility.reaction-adjacency.v1";

    public static IReadOnlyList<CampaignContactTransition> Derive(
        ElementMovedV3 movement, CampaignContactKnowledge knowledge)
    {
        ArgumentNullException.ThrowIfNull(movement);
        ArgumentNullException.ThrowIfNull(knowledge);
        var window = movement.OpenedReactionWindow;
        if (window is null) return [];
        if (!string.Equals(movement.CampaignId, knowledge.CampaignId, StringComparison.Ordinal)
            || window.ReactingSide != knowledge.Side
            || window.TriggerCommittedStateVersion != movement.StateVersion
            || !string.Equals(window.TriggerAuthority.ElementId, movement.ElementId, StringComparison.Ordinal)
            || !string.Equals(window.ApparentTrigger.DestinationLocationId,
                movement.DestinationLocationId, StringComparison.Ordinal))
            throw new ArgumentException("Reaction sight does not match its committed movement.", nameof(movement));

        var transitions = new List<CampaignContactTransition>();
        var working = knowledge.Copy();
        foreach (var opportunity in window.FrozenOpportunities.OrderBy(value => value.OpportunityId.Value,
                     StringComparer.Ordinal))
        {
            if (!opportunity.AdjacencyEvidence.IsAdjacent
                || opportunity.ReactingRepresentation.BoundElementIds.Count != 1)
                throw new ArgumentException("Reaction sight lacks a unique observing formation.", nameof(movement));
            var observer = opportunity.ReactingRepresentation.BoundElementIds[0];
            var prior = working.FindPrivateContact(movement.ElementId, observer);
            var transition = new CampaignContactTransition(
                CampaignContactKnowledge.CurrentContractVersion,
                movement.CampaignId,
                knowledge.Side,
                movement.StateVersion,
                $"{window.WindowId.Value}/{opportunity.OpportunityId.Value}",
                RuleId,
                prior is null ? CampaignContactTransitionKind.Observed
                    : prior.IsCurrent ? CampaignContactTransitionKind.Updated
                    : CampaignContactTransitionKind.Rediscovered,
                prior?.Handle ?? working.NextHandle(),
                movement.ElementId,
                observer,
                window.ApparentTrigger.DestinationLocationId,
                CampaignContactIdentification.Presence,
                CampaignContactAccess.Observer);
            working.Apply(transition);
            transitions.Add(transition);
        }

        return transitions;
    }
}
