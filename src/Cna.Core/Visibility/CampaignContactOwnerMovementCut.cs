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
        var worldByElement = snapshot.World.Elements.ToDictionary(
            element => element.ElementId, StringComparer.Ordinal);
        var own = definition.Elements.Where(element =>
                element.SideId == sideId && element.PlacementMode == ContentPlacementMode.Independent)
            .Select(element => CampaignOwnElementProjection.Project(
                element, worldByElement[element.ElementId]))
            .ToArray();
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
            || owner.CohesionLevel <= -26
            || owner.LedgerGameTurn != sequence.GameTurn
            || owner.LedgerOperationStage != sequence.OperationStage
            || CampaignOwnElementProjection.IsMovementEndedFor(
                snapshot.World.Elements.Single(element => element.ElementId == owner.ElementId)
                    .OperationalState.MovementEnded, sequence))
            return [];

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
