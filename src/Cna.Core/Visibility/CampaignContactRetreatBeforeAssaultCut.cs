using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

// The defender's first RBA cut comes from the same retained contact authority as the
// selected attack. Hidden opposing occupancy never controls offer presence.
internal sealed record CampaignContactRetreatBeforeAssaultCut(
    long AuthorityVersion,
    string PositionId,
    LandSide DefenderSide,
    string DefenderElementId,
    string DefenderFormationId,
    string InitialLocationId,
    bool InitiallyAdjacentToKnownEnemy,
    CampaignContactObservationV1 ContactObservation,
    ObservedOwnElement Defender,
    IReadOnlyList<CampaignObservationLocation> Locations,
    IReadOnlyList<CampaignObservationEdge> Edges,
    IReadOnlyList<RetreatBeforeAssaultMoveAction> MoveOffers,
    DeclineRetreatBeforeAssaultAction DeclineOffer)
{
    public static CampaignContactRetreatBeforeAssaultCut Project(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatBarrageV14 barrage,
        CampaignContentContext context,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(barrage);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contacts);
        if (selected.Decision.Choice != "select-contact-assault"
            || barrage.CampaignId != selected.CampaignId
            || barrage.PriorStateVersion != selected.StateVersion
            || contacts.CampaignId != selected.CampaignId
            || contacts.AuthorityVersion != barrage.StateVersion
            || context.ArtifactV6 is not { } artifact)
            throw new InvalidOperationException("RBA requires the selected, retained Barrage authority cut.");
        _ = CampaignNativeCombatBarrageV14.Replay(selected, context, barrage.SerializeCanonical());

        var prior = selected.PriorSnapshot;
        var representation = prior.World.Representations.SingleOrDefault(value =>
            value.RepresentationId == selected.Decision.BoundRepresentationId)
            ?? throw new InvalidOperationException("Selected defender binding is absent.");
        var defenderId = representation.BoundElementIds.Single();
        var definition = artifact.Definition.LegacyDefinition;
        var defender = definition.Elements.Single(value => value.ElementId == defenderId);
        var side = defender.SideId switch
        {
            "axis" => LandSide.Axis,
            "commonwealth" => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Selected defender has an unknown side."),
        };
        if (side == selected.Decision.Side)
            throw new InvalidOperationException("RBA defender cannot own the selected assault.");
        var state = prior.World.Elements.Single(value => value.ElementId == defenderId);
        var own = definition.Elements.Where(value => value.SideId == defender.SideId
                && value.PlacementMode == ContentPlacementMode.Independent)
            .Select(value => CampaignOwnElementProjection.Project(value,
                prior.World.Elements.Single(world => world.ElementId == value.ElementId)))
            .ToArray();
        var observed = own.Single(value => value.ElementId == defenderId);
        var knowledge = contacts.ObserveFor(side, defender.ParentFormationId);
        var locations = definition.Locations.Select(value =>
            new CampaignObservationLocation(value.LocationId, value.TerrainId)).ToArray();
        var edges = definition.Edges.Select(value => new CampaignObservationEdge(
            value.FirstLocationId, value.SecondLocationId,
            value.Features.Select(feature => new CampaignObservationEdgeFeature(
                feature.FeatureId, feature.DirectionFromLocationId)).ToArray())).ToArray();
        var adjacent = AdjacentLocationIds(observed.CurrentLocationId, edges);
        var initiallyAdjacent = knowledge.CurrentTargets.Any(target =>
            adjacent.Contains(target.ApparentLocationId));
        var eligible = state.ReserveStatus == CampaignElementReserveStatus.None
            && observed.CohesionLevel > -26
            && observed.VehicleBreakdownRisk is not { WorkingPointCount: 0 };
        var moves = eligible
            ? DeriveOneEdgeMoves(observed, own, knowledge.CurrentTargets, locations, edges)
            : [];
        return new(barrage.StateVersion, barrage.NextPositionId, side, defenderId,
            defender.ParentFormationId, observed.CurrentLocationId, initiallyAdjacent,
            knowledge, observed, Array.AsReadOnly(locations), Array.AsReadOnly(edges),
            Array.AsReadOnly(moves), new DeclineRetreatBeforeAssaultAction(defenderId));
    }

    internal static RetreatBeforeAssaultMoveAction[] DeriveOneEdgeMoves(
        ObservedOwnElement defender,
        IReadOnlyList<ObservedOwnElement> own,
        IReadOnlyList<CampaignContactTargetCandidate> currentTargets,
        IReadOnlyList<CampaignObservationLocation> locations,
        IReadOnlyList<CampaignObservationEdge> edges)
    {
        ArgumentNullException.ThrowIfNull(defender);
        ArgumentNullException.ThrowIfNull(own);
        ArgumentNullException.ThrowIfNull(currentTargets);
        ArgumentNullException.ThrowIfNull(locations);
        ArgumentNullException.ThrowIfNull(edges);
        var knownOccupied = currentTargets.Select(value => value.ApparentLocationId)
            .ToHashSet(StringComparer.Ordinal);
        return edges.Where(edge => edge.FirstLocationId == defender.CurrentLocationId
                || edge.SecondLocationId == defender.CurrentLocationId)
            .Select(edge => (Edge: edge, Destination: edge.FirstLocationId == defender.CurrentLocationId
                ? edge.SecondLocationId : edge.FirstLocationId))
            .Where(value => !knownOccupied.Contains(value.Destination))
            .Select(value => (value.Destination, Cost: CampaignMovementActionCostDerivation.TryCalculate(
                locations, own, defender, value.Edge, value.Destination)))
            .Where(value => value.Cost is not null)
            .Select(value => new RetreatBeforeAssaultMoveAction(defender.ElementId,
                defender.CurrentLocationId, value.Destination, value.Cost!))
            .OrderBy(value => value.ActionId, StringComparer.Ordinal)
            .ToArray();
    }

    private static HashSet<string> AdjacentLocationIds(string origin,
        IReadOnlyList<CampaignObservationEdge> edges) => edges
        .Where(edge => edge.FirstLocationId == origin || edge.SecondLocationId == origin)
        .Select(edge => edge.FirstLocationId == origin ? edge.SecondLocationId : edge.FirstLocationId)
        .ToHashSet(StringComparer.Ordinal);
}
