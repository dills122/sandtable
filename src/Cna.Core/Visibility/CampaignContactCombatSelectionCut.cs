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
    IReadOnlyList<SelectContactAssaultAction> AssaultOffers,
    bool MayFinishWithoutAttack)
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
            || snapshot.InitiativeHolder != side
            || snapshot.ReactionWindow is not null
            || snapshot.BreakdownFlow is not CampaignBreakdownFlow.Idle)
            throw new InvalidOperationException("Combat selection requires the acting side's position-determination cut.");

        var offers = DeriveAdjacentOffers(actingElementId,
            owner.ContactObservation.ActingFormationId,
            owner.ActingElement.CurrentLocationId, owner.Edges,
            owner.ContactObservation.CurrentTargets);
        // Land 10.3 makes attack mandatory in an opposing ZOC. The currently admitted
        // V6 world has no positive ZOC source; future worlds must derive this from
        // their own certified source facts before they can offer a voluntary finish.
        var opposing = side == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;
        var mayFinish = context.ArtifactV6 is { } artifact
            && CampaignElementMovedV3Factory.DeriveControlledLocationIds(
                snapshot.World, artifact, context.Scenario, opposing).Count == 0;
        return new(snapshot.StateVersion, owner, Array.AsReadOnly(offers), mayFinish);
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
