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
    FinishCombatWithoutAttackAction? FinishOffer)
{
    // Defensive invariant only: unreachable for admitted content (see Project). Callers map it to
    // "no decision available" (audience surface) or "no owner opening" (Host); never a crash path.
    internal const string PositiveZocExceedsBoundedProfile =
        "Unsupported: positive opposing ZOC exceeds the bounded Combat profile.";

    public bool MayFinishWithoutAttack => FinishOffer is not null;

    public static CampaignContactCombatSelectionCut Project(CampaignSnapshotV11 snapshot,
        CampaignContentContext context, CampaignContactJournal contacts, LandSide side,
        string actingElementId, bool legacyInitiativeOwner = false)
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
            || (legacyInitiativeOwner
                ? snapshot.InitiativeHolder != side
                : !snapshot.OperationStageOrders.Any(order => order.GameTurn == position.GameTurn
                    && order.OperationStage == position.OperationStage && order.FirstSide == side))
            || snapshot.ReactionWindow is not null
            || snapshot.BreakdownFlow is not CampaignBreakdownFlow.Idle)
            throw new InvalidOperationException("Combat selection requires the acting side's position-determination cut.");

        var offers = DeriveAdjacentOffers(actingElementId,
            owner.ContactObservation.ActingFormationId,
            owner.ActingElement.CurrentLocationId, owner.Edges,
            owner.ContactObservation.CurrentTargets);
        // Land 10.3 makes attack mandatory in an opposing ZOC. Forced/surprise Combat is out of
        // the bounded profile (T020-Q2, ruled 2026-11-20). It is rejected at activation:
        // ContentPackV6Validator (BRK-CERT-003) admits only battalion organizations (stacking 1)
        // and at most one same-side combat/HQ battalion per hex, and CampaignWorldV6Validator
        // rejects any later World with two same-side combat elements in one hex. A ZOC source
        // needs more than one stacking point in a hex, so no admitted World reaches this branch.
        var opposing = side == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;
        var mayFinish = false;
        if (context.ArtifactV6 is { } artifact)
        {
            var enemyControl = CampaignElementMovedV3Factory.DeriveZocAuthority(
                snapshot.World, artifact, context.Scenario, opposing);
            if (enemyControl.SourceRepresentationIds.Count != 0 || enemyControl.ControlledLocationIds.Count != 0)
                throw new InvalidOperationException(PositiveZocExceedsBoundedProfile);
            mayFinish = true;
        }
        return new(snapshot.StateVersion, owner, Array.AsReadOnly(offers),
            mayFinish ? new FinishCombatWithoutAttackAction(actingElementId) : null);
    }

    // #394 gap 2/3: second-side analog of Project, built from (World, Position)
    // directly instead of a CampaignSnapshotV11 -- see
    // CampaignNativeSecondCombatDecisionV47's remarks for why. Reuses
    // DeriveAdjacentOffers below unchanged: it is already generic over where
    // its inputs come from.
    internal static CampaignContactCombatSelectionCut ProjectSecondCombat(
        CampaignWorldSnapshotV6 world, LandSequencePosition position, CampaignContentContext context,
        CampaignContactJournal contacts, LandSide side, string actingElementId)
    {
        var owner = CampaignContactOwnerMovementCut.ProjectSecondCombatOwner(world, position, context,
            contacts, side, actingElementId);
        var offers = DeriveAdjacentOffers(actingElementId,
            owner.ContactObservation.ActingFormationId,
            owner.ActingElement.CurrentLocationId, owner.Edges,
            owner.ContactObservation.CurrentTargets);
        // Mirrors Project's own Land 10.3 mandatory-attack check; ArtifactV6
        // is already guaranteed non-null by ProjectSecondCombatOwner above.
        var opposing = side == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;
        var enemyControl = CampaignElementMovedV3Factory.DeriveZocAuthority(
            world, context.ArtifactV6!, context.Scenario, opposing);
        if (enemyControl.SourceRepresentationIds.Count != 0 || enemyControl.ControlledLocationIds.Count != 0)
            throw new InvalidOperationException(PositiveZocExceedsBoundedProfile);
        return new(contacts.AuthorityVersion, owner, Array.AsReadOnly(offers),
            new FinishCombatWithoutAttackAction(actingElementId));
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
