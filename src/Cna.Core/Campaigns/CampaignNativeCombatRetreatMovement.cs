using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Visibility;

namespace Cna.Core.Campaigns;

// Shared authority projection for an accepted defender RBA move. The offered action
// comes from the side contact cut; the resulting world and costs come from Core rules.
internal static class CampaignNativeCombatRetreatMovement
{
    internal sealed record Result(
        CampaignWorldSnapshotV6 World,
        CampaignElementMovedV3Factory.MovementResult Movement,
        IReadOnlyList<CampaignBreakdownStep> BreakdownAccounting);

    internal static Result ProjectFirst(
        CampaignNativeCombatDecisionStateV12 selected,
        CampaignNativeCombatBarrageV14 barrage,
        CampaignContentContext context,
        CampaignContactJournal contacts,
        string actionId)
    {
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(barrage);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(contacts);
        var cut = CampaignContactRetreatBeforeAssaultCut.Project(selected, barrage,
            context, contacts);
        var offer = cut.MoveOffers.SingleOrDefault(value => value.ActionId == actionId)
            ?? throw new InvalidOperationException("Retreat move is absent from the defender contact cut.");
        var artifact = context.ArtifactV6
            ?? throw new InvalidOperationException("Retreat move lacks Content 6 authority.");
        var prior = selected.PriorSnapshot;
        var definition = artifact.Definition.LegacyDefinition;
        var content = definition.Elements.Single(value => value.ElementId == cut.DefenderElementId);
        var element = prior.World.Elements.Single(value => value.ElementId == cut.DefenderElementId);
        var representation = prior.World.Representations.Single(value =>
            value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
            && value.BoundElementIds.Count == 1
            && value.BoundElementIds[0] == cut.DefenderElementId);
        if (offer.OriginLocationId != element.CurrentLocationId
            || representation.CurrentLocationId != offer.OriginLocationId)
            throw new InvalidOperationException("Retreat origin differs from the retained defender world.");
        var edge = CampaignElementMovedV3Factory.FindEdge(definition,
            offer.OriginLocationId, offer.DestinationLocationId)
            ?? throw new InvalidOperationException("Retreat destination is not adjacent.");
        var destination = definition.Locations.Single(value =>
            value.LocationId == offer.DestinationLocationId);
        var movement = CampaignElementMovedV3Factory.CalculateMovement(prior,
            definition, content, element, edge, destination,
            offer.OriginLocationId, offer.DestinationLocationId);
        if (movement.ActionCost != offer.CostBreakdown)
            throw new InvalidOperationException("Retreat offer cost differs from Core movement rules.");
        var accounting = content.BreakdownVehicleCohort is null
            ? Array.Empty<CampaignBreakdownStep>()
            : [CampaignBreakdownAccounting.Calculate(content.BreakdownVehicleCohort,
                element.OperationalState.VehicleBreakdownState!, edge, destination,
                offer.OriginLocationId,
                CampaignSnapshotV11Validator.ApplicableWeather(prior, artifact,
                    offer.DestinationLocationId))];
        var movedRepresentation = new CampaignMapRepresentationState(
            representation.RepresentationId, offer.DestinationLocationId,
            representation.BindingKind, representation.BoundElementIds);
        var world = CampaignElementMovedV3Factory.ProjectMoveForAuthority(prior.World,
            element, movedRepresentation, movement.ExpendedAfter, accounting, null);
        return new Result(world, movement, accounting);
    }
}
