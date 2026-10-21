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
            || context.ArtifactV6 is null)
            throw new InvalidOperationException("RBA requires the selected, retained Barrage authority cut.");
        _ = CampaignNativeCombatBarrageV14.Replay(selected, context, barrage.SerializeCanonical());
        return ProjectAtWorld(selected, barrage, context, contacts,
            selected.PriorSnapshot.World, barrage.StateVersion, null,
            selected.PriorSnapshot.World.Representations.Single(value =>
                value.RepresentationId == selected.Decision.BoundRepresentationId).CurrentLocationId,
            allowFurtherMoves: true);
    }

    internal static CampaignContactRetreatBeforeAssaultCut ProjectAfterFirstMove(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatBarrageV14 barrage,
        CampaignNativeCombatRetreatMoveV16 move,
        CampaignWorldSnapshotV6 world,
        CampaignContentContext context,
        CampaignContactJournal contacts)
    {
        if (move.PriorStateVersion != barrage.StateVersion)
            throw new InvalidOperationException("First RBA move must follow retained Barrage.");
        return ProjectAfterMove(selected, barrage, move, world,
            move.OriginLocationId, context, contacts);
    }

    internal static CampaignContactRetreatBeforeAssaultCut ProjectAfterMove(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatBarrageV14 barrage,
        CampaignNativeCombatRetreatMoveV16 move,
        CampaignWorldSnapshotV6 world,
        string initialLocationId,
        CampaignContentContext context,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(move);
        ArgumentNullException.ThrowIfNull(world);
        if (move.CampaignId != selected.CampaignId
            || move.PriorStateVersion < barrage.StateVersion
            || move.DecisionActionId != selected.Decision.ActionId
            || move.PositionId != barrage.NextPositionId
            || contacts.AuthorityVersion != move.StateVersion
            || contacts.CampaignId != selected.CampaignId)
            throw new InvalidOperationException("Post-move RBA cut lacks retained authority.");
        return ProjectAtWorld(selected, barrage, context, contacts, world,
            move.StateVersion, move.InitiallyAdjacentToKnownEnemy,
            initialLocationId, move.InitiallyAdjacentToKnownEnemy);
    }

    private static CampaignContactRetreatBeforeAssaultCut ProjectAtWorld(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatBarrageV14 barrage,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        CampaignWorldSnapshotV6 world,
        long authorityVersion,
        bool? initialAdjacency,
        string initialLocationId,
        bool allowFurtherMoves)
    {
        var artifact = context.ArtifactV6
            ?? throw new InvalidOperationException("RBA requires Content 6 authority.");

        var representation = world.Representations.SingleOrDefault(value =>
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
        var state = world.Elements.Single(value => value.ElementId == defenderId);
        var own = definition.Elements.Where(value => value.SideId == defender.SideId
                && value.PlacementMode == ContentPlacementMode.Independent)
            .Select(value => CampaignOwnElementProjection.Project(value,
                world.Elements.Single(element => element.ElementId == value.ElementId)))
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
        var currentlyAdjacent = knowledge.CurrentTargets.Any(target =>
            adjacent.Contains(target.ApparentLocationId));
        var initiallyAdjacent = initialAdjacency ?? knowledge.CurrentTargets.Any(target =>
            adjacent.Contains(target.ApparentLocationId));
        var eligible = state.ReserveStatus == CampaignElementReserveStatus.None
            && observed.CohesionLevel > -26
            && observed.VehicleBreakdownRisk is not { WorkingPointCount: 0 };
        var moves = eligible && allowFurtherMoves
            && (initialAdjacency is null || !currentlyAdjacent)
            ? DeriveOneEdgeMoves(observed, own, knowledge.CurrentTargets, locations, edges)
            : [];
        return new(authorityVersion, barrage.NextPositionId, side, defenderId,
            defender.ParentFormationId, initialLocationId, initiallyAdjacent,
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
