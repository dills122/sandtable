using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// A released unit's immediately following Movement is one supported
// continuation witness. Other source-legal continuations need their own
// implementation before they can be offered as a repeat edge.
internal static class CampaignCycleContinuationMovementWitnessV28
{
    internal static MoveElementAction? Find(
        CampaignNativeReserveReleaseStateV27 release,
        CampaignContentContext context,
        CampaignContactJournal contacts)
    {
        var offers = Offers(release, context, contacts);
        return offers.Count == 0 ? null : offers[0];
    }

    internal static IReadOnlyList<MoveElementAction> Offers(
        CampaignNativeReserveReleaseStateV27 release,
        CampaignContentContext context,
        CampaignContactJournal contacts)
    {
        ArgumentNullException.ThrowIfNull(release);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contacts);
        if (!release.Completed || contacts.CampaignId != release.CampaignId
            || contacts.AuthorityVersion != release.StateVersion
            || release.CycleOrdinal == int.MaxValue)
            throw new InvalidOperationException(
                "Continuation needs completed same-cut Reserve Release authority.");
        var definition = context.ArtifactV6?.Definition.LegacyDefinition
            ?? throw new InvalidOperationException("Continuation needs normal Content 6.");
        var sideId = CampaignSnapshotSerializer.FormatSide(release.ActingSide);
        var world = release.World.Elements.ToDictionary(
            element => element.ElementId, StringComparer.Ordinal);
        var ownContent = definition.Elements.Where(element =>
                element.SideId == sideId
                && element.PlacementMode == ContentPlacementMode.Independent)
            .ToDictionary(element => element.ElementId, StringComparer.Ordinal);
        var own = ownContent.Values.Select(element =>
            CampaignOwnElementProjection.Project(element, world[element.ElementId])).ToArray();
        var locations = definition.Locations.Select(location =>
            new CampaignObservationLocation(location.LocationId, location.TerrainId)).ToArray();
        var edges = definition.Edges.Select(edge => new CampaignObservationEdge(
            edge.FirstLocationId, edge.SecondLocationId,
            edge.Features.Select(feature => new CampaignObservationEdgeFeature(
                feature.FeatureId, feature.DirectionFromLocationId)).ToArray())).ToArray();
        var offers = new List<MoveElementAction>();

        foreach (var allowance in release.NextMovement.OrderBy(value =>
                     value.ElementId, StringComparer.Ordinal))
        {
            if (!ownContent.TryGetValue(allowance.ElementId, out var content)
                || allowance.NextCycleOrdinal != release.CycleOrdinal + 1
                || allowance.CapabilityPointCeiling < 1)
                continue;
            var owner = own.Single(value => value.ElementId == allowance.ElementId);
            if (owner.ReserveStatus != CampaignObservationReserveStatus.None
                || owner.VehicleBreakdownRisk is { WorkingPointCount: 0 }
                || owner.CohesionLevel <= -26)
                continue;
            var knowledge = contacts.ObserveFor(release.ActingSide,
                content.ParentFormationId);
            var occupied = knowledge.CurrentTargets.Select(target =>
                target.ApparentLocationId).ToHashSet(StringComparer.Ordinal);
            // A contact at origin needs break-off rules, so it cannot be this
            // simple movement witness. Enemy occupancy at destination is
            // excluded from the owner-visible offer by the contact rule.
            if (occupied.Contains(owner.CurrentLocationId)) continue;
            var subject = new ObservedOwnElement(owner.ElementId,
                owner.ParentFormationId, owner.OrganizationId,
                allowance.CapabilityPointCeiling, owner.CurrentLocationId,
                owner.ReserveStatus, owner.MobilityId, owner.LedgerGameTurn,
                owner.LedgerOperationStage, owner.CapabilityPointsExpended,
                owner.CohesionLevel, owner.VehicleBreakdownRisk);
            foreach (var edge in edges.Where(value =>
                         value.FirstLocationId == owner.CurrentLocationId
                         || value.SecondLocationId == owner.CurrentLocationId))
            {
                var destination = edge.FirstLocationId == owner.CurrentLocationId
                    ? edge.SecondLocationId : edge.FirstLocationId;
                if (occupied.Contains(destination)) continue;
                var cost = CampaignMovementActionCostDerivation.TryCalculate(
                    locations, own, subject, edge, destination);
                if (cost is not null)
                    offers.Add(new MoveElementAction(owner.ElementId,
                        owner.CurrentLocationId, destination, cost));
            }
        }
        return Array.AsReadOnly(offers.OrderBy(value => value.ActionId,
            StringComparer.Ordinal).ToArray());
    }
}
