namespace Cna.Core.Campaigns;

internal sealed partial record CampaignNativeReactionMoveV59
{
    internal static (CampaignNativeReactionMoveV59 Event, CampaignWorldSnapshotV6 World) Create(object predecessor,
        CampaignWorldSnapshotV6 world, CampaignContentContext context, int gameTurn, string rulesetHash,
        CampaignOperationStageWeather weather, CampaignReactionWindowId windowId,
        CampaignReactionOpportunityId opportunityId, string destinationLocationId)
    {
        var cut = CampaignNativeReactionCut.Bind(predecessor, world, context, gameTurn, rulesetHash, weather, windowId);
        var window = cut.Window;
        var opportunity = CampaignReactingElementMovedV2Factory.RequireOpportunity(window, opportunityId);
        var option = cut.MoveOptions(opportunity).SingleOrDefault(x => x.DestinationLocationId == destinationLocationId)
            ?? throw new InvalidOperationException("The Reaction destination is not currently legal.");
        var elementId = opportunity.ReactingRepresentation.BoundElementIds.Single();
        var element = world.Elements.Single(x => x.ElementId == elementId);
        var representation = world.Representations.Single(x => x.RepresentationId == opportunity.ReactingRepresentation.RepresentationId);
        var definition = cut.Artifact.Definition.LegacyDefinition;
        var content = definition.Elements.Single(x => x.ElementId == elementId);
        var edge = CampaignElementMovedV3Factory.FindEdge(definition, option.OriginLocationId, destinationLocationId)!;
        var destination = definition.Locations.Single(x => x.LocationId == destinationLocationId);
        var movement = CampaignElementMovedV3Factory.CalculateMovement(world, definition, content, element, edge,
            destination, option.OriginLocationId, destinationLocationId);
        var accounting = cut.Accounting(content, element, edge, destination);
        var moved = new CampaignMapRepresentationState(representation.RepresentationId, destinationLocationId,
            representation.BindingKind, representation.BoundElementIds);
        var projected = CampaignElementMovedV3Factory.ProjectMoveForAuthority(world, element, moved,
            movement.ExpendedAfter, accounting, element.OperationalState.MovementEnded);
        if (CampaignElementMovedV3Factory.FindAdjacentCombatRepresentations(projected, cut.Artifact,
                window.PhasingSide, destinationLocationId).Length != 0)
            throw new CampaignRuleNotImplementedException("A Reaction move cannot open another Reaction window yet.");
        if (!CampaignWorldV6Validator.IsValid(projected, cut.Artifact, cut.Scenario))
            throw new InvalidOperationException("Native Reaction move would leave invalid World authority.");
        var after = CampaignReactingElementMovedV2Factory.ActivateParticipant(window, opportunityId);
        return (new(CurrentContractVersion, cut.CampaignId, cut.Version, cut.PriorEventHash, gameTurn,
            windowId, window.ReactingSide, cut.WorldHash, CampaignNativeCombatPaidWorldV22.Hash(projected),
            opportunityId, elementId, option.OriginLocationId, destinationLocationId, movement.Cost, after), projected);
    }

    internal CampaignWorldSnapshotV6 Replay(object predecessor, CampaignWorldSnapshotV6 world, CampaignContentContext context,
        int gameTurn, string rulesetHash, CampaignOperationStageWeather weather)
    {
        var (expected, result) = Create(predecessor, world, context, gameTurn, rulesetHash, weather,
            WindowId, OpportunityId, DestinationLocationId);
        if (this != expected) throw new InvalidOperationException("Native Reaction move does not match replayed authority.");
        return result;
    }
}
