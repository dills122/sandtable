using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

// One authority cut produces both the player's contact view and candidate assault intents.
// Private defender identity is deliberately absent from this projection.
internal sealed record CampaignContactCombatSelectionCut(
    long AuthorityVersion,
    CampaignContactOwnerMovementCut OwnerCut,
    IReadOnlyList<SelectContactAssaultAction> AssaultOffers)
{
    public static CampaignContactCombatSelectionCut Project(CampaignSnapshotV11 snapshot,
        CampaignContentContext context, CampaignContactJournal contacts, LandSide side,
        string actingElementId)
    {
        var owner = CampaignContactOwnerMovementCut.Project(snapshot, context, contacts,
            side, actingElementId);
        var position = snapshot.CurrentPosition.SequencePosition;
        if (snapshot.CurrentPosition.Kind != CampaignPositionV11Kind.Sequence
            || position is null
            || position.PhaseId != LandPhaseIds.MovementAndCombat
            || position.SegmentId != LandSegmentIds.Combat
            || position.StepId != LandStepIds.PositionDetermination
            || position.ActorRole != LandActorRole.FirstActingSide
            || position.ActiveSide != side
            || snapshot.ReactionWindow is not null
            || snapshot.BreakdownFlow is not CampaignBreakdownFlow.Idle)
            throw new InvalidOperationException("Combat selection requires the acting side's position-determination cut.");

        var offers = DeriveAdjacentOffers(actingElementId,
            owner.ContactObservation.ActingFormationId,
            owner.ActingElement.CurrentLocationId, owner.Edges,
            owner.ContactObservation.CurrentTargets);
        return new(snapshot.StateVersion, owner, Array.AsReadOnly(offers));
    }

    internal static SelectContactAssaultAction[] DeriveAdjacentOffers(string actingElementId,
        string actingFormationId, string originLocationId,
        IReadOnlyList<CampaignObservationEdge> edges,
        IReadOnlyList<CampaignContactTargetCandidate> currentTargets)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actingElementId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actingFormationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(originLocationId);
        ArgumentNullException.ThrowIfNull(edges);
        ArgumentNullException.ThrowIfNull(currentTargets);
        var adjacent = edges.Where(edge => edge.FirstLocationId == originLocationId
                || edge.SecondLocationId == originLocationId)
            .Select(edge => edge.FirstLocationId == originLocationId
                ? edge.SecondLocationId : edge.FirstLocationId)
            .ToHashSet(StringComparer.Ordinal);
        return currentTargets
            .Where(target => target.ActingFormationId == actingFormationId
                && adjacent.Contains(target.ApparentLocationId))
            .DistinctBy(target => (target.ContactHandle, target.ApparentLocationId))
            .Select(target => new SelectContactAssaultAction(actingElementId,
                target.ContactHandle, target.ApparentLocationId))
            .OrderBy(action => action.ActionId, StringComparer.Ordinal)
            .ToArray();
    }
}
