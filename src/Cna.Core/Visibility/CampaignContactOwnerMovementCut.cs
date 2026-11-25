using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

/// <summary>
/// One Core authority cut for an acting element. Contact disclosure and movement offers are
/// derived together; neither offer membership nor ordering consults opposing World state.
/// The cut stays inside Core until the normal campaign host can bind and durably issue it.
/// </summary>
internal sealed record CampaignContactOwnerMovementCut(
    long AuthorityVersion,
    CampaignContactObservationV1 ContactObservation,
    IReadOnlyList<CampaignObservationLocation> Locations,
    IReadOnlyList<CampaignObservationEdge> Edges,
    ObservedOwnElement ActingElement,
    IReadOnlyList<MoveElementAction> MovementOffers)
{
    public static CampaignContactOwnerMovementCut Project(
        CampaignSnapshotV11 snapshot,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        LandSide side,
        string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contacts);
        ArgumentException.ThrowIfNullOrWhiteSpace(actingElementId);
        if (!Enum.IsDefined(side)
            || contacts.CampaignId != snapshot.CampaignId
            || contacts.AuthorityVersion != snapshot.StateVersion
            || context.ArtifactV6 is null
            || !CampaignSnapshotV11Validator.IsValid(snapshot, context.ArtifactV6, context.Scenario))
            throw new ArgumentException("Owner movement cut requires matching admitted authority.");

        var definition = context.ArtifactV6.Definition.LegacyDefinition;
        var sideId = CampaignSnapshotSerializer.FormatSide(side);
        var content = definition.Elements.SingleOrDefault(element =>
            element.ElementId == actingElementId
            && element.SideId == sideId
            && element.PlacementMode == ContentPlacementMode.Independent)
            ?? throw new ArgumentException("Acting element is not independently owned by this side.",
                nameof(actingElementId));
        var own = CampaignOwnElementProjection.ProjectPlaced(definition, snapshot.World, side);
        var owner = own.Single(element => element.ElementId == content.ElementId);
        var knowledge = contacts.ObserveFor(side, content.ParentFormationId);
        var locations = definition.Locations.Select(location =>
            new CampaignObservationLocation(location.LocationId, location.TerrainId)).ToArray();
        var edges = definition.Edges.Select(edge => new CampaignObservationEdge(
            edge.FirstLocationId, edge.SecondLocationId,
            edge.Features.Select(feature => new CampaignObservationEdgeFeature(
                feature.FeatureId, feature.DirectionFromLocationId)).ToArray())).ToArray();
        var offers = DeriveMovementOffers(snapshot, owner, own, knowledge, locations, edges);
        return new CampaignContactOwnerMovementCut(snapshot.StateVersion, knowledge,
            Array.AsReadOnly(locations), Array.AsReadOnly(edges), owner,
            Array.AsReadOnly(offers));
    }

    // MTS-A (2026-11-22, docs/work/plans/2026-11-22-multi-turn-multi-stage-scoping.md):
    // generalized from "ActorRole != SecondActingSide" to the FirstActingSide-or-
    // SecondActingSide shape already proven by CampaignNativePostCombatPassV42/
    // CampaignNativeCycleControlV28 -- this was never actually tied to "second"
    // specifically, only to acting at all. Stage 2+ needs both acting sides on
    // this native path, since CampaignSnapshotV11 stays Stage 1/Turn 1-only per
    // the owner's Option-2 ruling. Side/position agreement (position.ActiveSide
    // != side, two lines below) is unchanged and still rejects a mismatched pair.
    internal static CampaignContactOwnerMovementCut ProjectSecondMovement(
        CampaignWorldSnapshotV6 world, LandSequencePosition position, CampaignContentContext context,
        CampaignContactJournal contacts, LandSide side, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contacts);
        ArgumentException.ThrowIfNullOrWhiteSpace(actingElementId);
        if (context.ArtifactV6 is not { } artifact || !Enum.IsDefined(side)
            || position.ActorRole is not (LandActorRole.FirstActingSide or LandActorRole.SecondActingSide)
            || position.ActiveSide != side
            || position.SegmentId != LandSegmentIds.Movement
            || !CampaignWorldV6Validator.IsValid(world, artifact, context.Scenario))
            throw new ArgumentException("Second Movement requires a valid owner World and position.");
        Cna1979LandSequenceV4.RequireMaterializedMovement(position);
        var definition = artifact.Definition.LegacyDefinition;
        var content = definition.Elements.SingleOrDefault(element => element.ElementId == actingElementId
            && element.SideId == CampaignSnapshotSerializer.FormatSide(side)
            && element.PlacementMode == ContentPlacementMode.Independent && element.BreakdownVehicleCohort is null)
            ?? throw new ArgumentException("Second Movement requires an independently owned non-vehicle element.");
        var classification = artifact.Definition.ElementCombatFacts.Single(value => value.ElementId == actingElementId);
        if (Cna1979Combat.FindClassification(classification.CombatClassificationId)?.Kind is not
            (ZocCombatClassificationKind.CombatUnit or ZocCombatClassificationKind.Headquarters))
            throw new ArgumentException("Second Movement requires a supported combat element.");
        var own = CampaignOwnElementProjection.ProjectPlaced(definition, world, side);
        var owner = own.Single(value => value.ElementId == actingElementId);
        var knowledge = contacts.ObserveFor(side, content.ParentFormationId);
        var locations = definition.Locations.Select(location =>
            new CampaignObservationLocation(location.LocationId, location.TerrainId)).ToArray();
        var edges = definition.Edges.Select(edge => new CampaignObservationEdge(edge.FirstLocationId,
            edge.SecondLocationId, edge.Features.Select(feature => new CampaignObservationEdgeFeature(
                feature.FeatureId, feature.DirectionFromLocationId)).ToArray())).ToArray();
        var state = world.Elements.Single(value => value.ElementId == actingElementId);
        var eligible = owner.ReserveStatus == CampaignObservationReserveStatus.None
            && owner.VehicleBreakdownRisk is null && owner.CohesionLevel > Cna1979Zoc.MinimumExclusiveCohesion
            && owner.LedgerGameTurn == position.GameTurn && owner.LedgerOperationStage == position.OperationStage
            && !CampaignOwnElementProjection.IsMovementEndedFor(state.OperationalState.MovementEnded, position);
        var offers = eligible ? DeriveAdjacentOffers(owner, own, knowledge, locations, edges) : [];
        return new CampaignContactOwnerMovementCut(contacts.AuthorityVersion, knowledge,
            Array.AsReadOnly(locations), Array.AsReadOnly(edges), owner, Array.AsReadOnly(offers));
    }

    // #394 gap 2/3: second-side analog of the owner-visibility half of
    // CampaignContactCombatSelectionCut.Project, built from (World, Position)
    // directly -- mirroring ProjectSecondMovement immediately above for the
    // same reason (no CampaignSnapshotV11 can represent this position; see
    // CampaignNativeSecondCombatDecisionV47's remarks). MovementOffers is
    // irrelevant to a Combat cut and left empty.
    // MTS-A (2026-11-22): generalized to FirstActingSide-or-SecondActingSide,
    // same reasoning as ProjectSecondMovement above.
    internal static CampaignContactOwnerMovementCut ProjectSecondCombatOwner(
        CampaignWorldSnapshotV6 world, LandSequencePosition position, CampaignContentContext context,
        CampaignContactJournal contacts, LandSide side, string actingElementId)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contacts);
        ArgumentException.ThrowIfNullOrWhiteSpace(actingElementId);
        if (context.ArtifactV6 is not { } artifact || !Enum.IsDefined(side)
            || position.ActorRole is not (LandActorRole.FirstActingSide or LandActorRole.SecondActingSide)
            || position.ActiveSide != side
            || position.SegmentId != LandSegmentIds.Combat || position.StepId != LandStepIds.PositionDetermination
            || !CampaignWorldV6Validator.IsValid(world, artifact, context.Scenario))
            throw new ArgumentException("Second Combat selection requires a valid owner World and position.");
        var definition = artifact.Definition.LegacyDefinition;
        var content = definition.Elements.SingleOrDefault(element => element.ElementId == actingElementId
            && element.SideId == CampaignSnapshotSerializer.FormatSide(side)
            && element.PlacementMode == ContentPlacementMode.Independent)
            ?? throw new ArgumentException("Second Combat selection requires an independently owned element.",
                nameof(actingElementId));
        var own = CampaignOwnElementProjection.ProjectPlaced(definition, world, side);
        var owner = own.Single(value => value.ElementId == content.ElementId);
        var knowledge = contacts.ObserveFor(side, content.ParentFormationId);
        var locations = definition.Locations.Select(location =>
            new CampaignObservationLocation(location.LocationId, location.TerrainId)).ToArray();
        var edges = definition.Edges.Select(edge => new CampaignObservationEdge(edge.FirstLocationId,
            edge.SecondLocationId, edge.Features.Select(feature => new CampaignObservationEdgeFeature(
                feature.FeatureId, feature.DirectionFromLocationId)).ToArray())).ToArray();
        return new CampaignContactOwnerMovementCut(contacts.AuthorityVersion, knowledge,
            Array.AsReadOnly(locations), Array.AsReadOnly(edges), owner, []);
    }

    private static MoveElementAction[] DeriveMovementOffers(
        CampaignSnapshotV11 snapshot,
        ObservedOwnElement owner,
        IReadOnlyList<ObservedOwnElement> own,
        CampaignContactObservationV1 knowledge,
        IReadOnlyList<CampaignObservationLocation> locations,
        IReadOnlyList<CampaignObservationEdge> edges)
    {
        var sequence = snapshot.CurrentPosition.SequencePosition;
        if (sequence is null
            || snapshot.CurrentPosition.Kind != CampaignPositionV11Kind.Sequence
            || snapshot.ReactionWindow is not null
            || snapshot.BreakdownFlow is not (CampaignBreakdownFlow.Idle or CampaignBreakdownFlow.Moving)
            || (snapshot.BreakdownFlow is CampaignBreakdownFlow.Moving moving
                && (moving.Route.ElementId != owner.ElementId
                    || moving.Route.CurrentLocationId != owner.CurrentLocationId))
            || sequence.OperationStage != 1
            || sequence.PhaseId != LandPhaseIds.MovementAndCombat
            || sequence.SegmentId != LandSegmentIds.Movement
            || sequence.ActorRole != LandActorRole.FirstActingSide
            || sequence.ActiveSide != knowledge.Side
            || owner.ReserveStatus != CampaignObservationReserveStatus.None
            || owner.VehicleBreakdownRisk is { WorkingPointCount: 0 }
            || owner.CohesionLevel <= Cna1979Zoc.MinimumExclusiveCohesion
            || owner.LedgerGameTurn != sequence.GameTurn
            || owner.LedgerOperationStage != sequence.OperationStage
            || CampaignOwnElementProjection.IsMovementEndedFor(
                snapshot.World.Elements.Single(element => element.ElementId == owner.ElementId)
                    .OperationalState.MovementEnded, sequence))
            return [];

        return DeriveAdjacentOffers(owner, own, knowledge, locations, edges);
    }

    private static MoveElementAction[] DeriveAdjacentOffers(ObservedOwnElement owner,
        IReadOnlyList<ObservedOwnElement> own, CampaignContactObservationV1 knowledge,
        IReadOnlyList<CampaignObservationLocation> locations, IReadOnlyList<CampaignObservationEdge> edges)
    {
        var occupied = knowledge.CurrentTargets
            .Select(target => target.ApparentLocationId)
            .ToHashSet(StringComparer.Ordinal);
        return edges.Where(edge => edge.FirstLocationId == owner.CurrentLocationId
                || edge.SecondLocationId == owner.CurrentLocationId)
            .Select(edge => (Edge: edge, Destination: edge.FirstLocationId == owner.CurrentLocationId
                ? edge.SecondLocationId : edge.FirstLocationId))
            .Where(value => !occupied.Contains(value.Destination)
                && !occupied.Contains(owner.CurrentLocationId))
            .Select(value => (value.Destination, Cost: CampaignMovementActionCostDerivation.TryCalculate(
                locations, own, owner, value.Edge, value.Destination)))
            .Where(value => value.Cost is not null)
            .Select(value => new MoveElementAction(owner.ElementId, owner.CurrentLocationId,
                value.Destination, value.Cost!))
            .OrderBy(value => value.ActionId, StringComparer.Ordinal)
            .ToArray();
    }
}
