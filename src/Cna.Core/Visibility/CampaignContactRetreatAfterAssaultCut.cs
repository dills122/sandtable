using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Visibility;

// Source-directed one-hex retreat offers use only published map, defender
// ownership and its current contact knowledge. Refusal is always explicit.
internal sealed record CampaignContactRetreatAfterAssaultCut(
    long AuthorityVersion, string PositionId, LandSide DefenderSide,
    string DefenderElementId, string DefenderFormationId,
    string RetreatSourceHash, CampaignContactObservationV1 ContactObservation,
    ObservedOwnElement Defender,
    IReadOnlyList<RetreatAfterAssaultMoveAction> MoveOffers,
    RefuseRetreatAfterAssaultAction RefuseOffer)
{
    internal static CampaignContactRetreatAfterAssaultCut Project(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatAssaultResultV22 result,
        CampaignWorldSnapshotV6 paidWorld,
        CampaignContentContext context,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(paidWorld);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contacts);
        var artifact = context.ArtifactV6
            ?? throw new InvalidOperationException("Retreat requires normal Content 6 authority.");
        var source = context.CombatRetreatSource
            ?? throw new InvalidOperationException("Retreat requires an exact-pack direction source.");
        if (selected.Decision.Choice != "select-contact-assault"
            || result.CampaignId != selected.CampaignId
            || result.DecisionActionId != selected.Decision.ActionId
            || result.Facts.RequiredRetreat != 1
            || source.ContentHash != artifact.Identity.Hash
            || result.PaidWorldHash != CampaignNativeCombatPaidWorldV22.Hash(paidWorld)
            || paidWorld.Representations.Single(value =>
                    value.RepresentationId == selected.Decision.BoundRepresentationId)
                .BoundElementIds.Single() != result.DefenderElementId
            || !CampaignWorldV6Validator.IsValid(paidWorld, artifact, context.Scenario)
            || contacts.CampaignId != selected.CampaignId
            || contacts.AuthorityVersion != result.StateVersion)
            throw new InvalidOperationException("Retreat cut lacks retained result authority.");
        var definition = artifact.Definition.LegacyDefinition;
        var defender = definition.Elements.Single(value =>
            value.ElementId == result.DefenderElementId);
        var side = defender.SideId switch
        {
            "axis" => LandSide.Axis,
            "commonwealth" => LandSide.Commonwealth,
            _ => throw new InvalidOperationException("Unknown defender side.")
        };
        if (side == selected.Decision.Side)
            throw new InvalidOperationException("Assault owner cannot choose defender retreat.");
        var state = paidWorld.Elements.Single(value =>
            value.ElementId == defender.ElementId);
        var observed = CampaignOwnElementProjection.Project(defender, state);
        var knowledge = contacts.ObserveFor(side, defender.ParentFormationId);
        var anchor = source.Anchor(defender.SideId);
        var edges = definition.Edges;
        var distances = DistancesFrom(anchor, edges);
        if (!distances.TryGetValue(observed.CurrentLocationId, out var originDistance))
            throw new InvalidOperationException("Defender has no source-directed retreat route.");
        var currentTargets = knowledge.CurrentTargets.Select(value => value.ApparentLocationId)
            .ToHashSet(StringComparer.Ordinal);
        var combatFacts = artifact.Definition.ElementCombatFacts.ToDictionary(value =>
            value.ElementId, StringComparer.Ordinal);
        var friendlyCombat = definition.Elements.Where(value =>
                value.SideId == defender.SideId
                && combatFacts[value.ElementId].CombatClassificationId
                    != Cna1979Combat.TruckConvoyClassificationId
                && value.ElementId != defender.ElementId)
            .Select(value => paidWorld.Elements.Single(stateValue =>
                stateValue.ElementId == value.ElementId).CurrentLocationId)
            .ToHashSet(StringComparer.Ordinal);
        var locations = definition.Locations.ToDictionary(value =>
            value.LocationId, StringComparer.Ordinal);
        var moves = Neighbors(observed.CurrentLocationId, edges)
            .Where(destination => distances.TryGetValue(destination, out var distance)
                && distance < originDistance
                && locations[destination].TerrainId == "land.terrain.clear"
                && !currentTargets.Contains(destination)
                && !friendlyCombat.Contains(destination))
            .Select(destination => new RetreatAfterAssaultMoveAction(
                defender.ElementId, observed.CurrentLocationId, destination))
            .OrderBy(value => value.ActionId, StringComparer.Ordinal).ToArray();
        return new(result.StateVersion, result.PositionId, side,
            defender.ElementId, defender.ParentFormationId, source.Hash,
            knowledge, observed, Array.AsReadOnly(moves),
            new RefuseRetreatAfterAssaultAction(defender.ElementId));
    }

    private static Dictionary<string, int> DistancesFrom(string anchor,
        IReadOnlyList<ContentHexEdge> edges)
    {
        var distances = new Dictionary<string, int>(StringComparer.Ordinal) { [anchor] = 0 };
        var queue = new Queue<string>();
        queue.Enqueue(anchor);
        while (queue.TryDequeue(out var location))
            foreach (var neighbor in Neighbors(location, edges))
                if (distances.TryAdd(neighbor, checked(distances[location] + 1)))
                    queue.Enqueue(neighbor);
        return distances;
    }

    private static IEnumerable<string> Neighbors(string location,
        IReadOnlyList<ContentHexEdge> edges) => edges
        .Where(edge => edge.FirstLocationId == location
            || edge.SecondLocationId == location)
        .Select(edge => edge.FirstLocationId == location
            ? edge.SecondLocationId : edge.FirstLocationId)
        .Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
}
