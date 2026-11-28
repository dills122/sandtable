using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignReactingElementMovedV2Factory
{
    public static ReactingElementMovedV2 Create(CampaignSnapshotV11 prior, ContentPackV6Artifact artifact,
        ContentScenario scenario, ReactingElementMovedReplayInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var expected = CreateReplayInput(prior, artifact, scenario, input.OpportunityId, input.DestinationLocationId);
        if (expected != input) throw Unsupported("The Reaction move is not an exact current capability.");
        var window = prior.ReactionWindow!;
        var opportunity = window.FrozenOpportunities.Single(x => x.OpportunityId == input.OpportunityId);
        var elementId = opportunity.ReactingRepresentation.BoundElementIds.Single();
        var element = prior.World.Elements.Single(x => x.ElementId == elementId);
        var representation = prior.World.Representations.Single(x => x.RepresentationId == opportunity.ReactingRepresentation.RepresentationId);
        var content = artifact.Definition.LegacyDefinition.Elements.Single(x => x.ElementId == elementId);
        var definition = artifact.Definition.LegacyDefinition;
        var edge = CampaignElementMovedV2Factory.FindEdge(definition, input.OriginLocationId, input.DestinationLocationId)!;
        var destination = definition.Locations.Single(x => x.LocationId == input.DestinationLocationId);
        var movement = CampaignElementMovedV3Factory.CalculateMovement(prior, definition, content, element, edge,
            destination, input.OriginLocationId, input.DestinationLocationId);
        // Both movement paths call the same accounting seam. Certified combat reactors currently have no cohort.
        var accounting = CalculateAccounting(prior, artifact, content, element, edge, destination);
        var flow = (CampaignBreakdownFlow.Reacting)prior.BreakdownFlow;
        var route = flow.ReactorRoute is { } existing
            ? existing.AtLocation(input.DestinationLocationId)
            : CampaignBreakdownRoute.Create(prior.CampaignId, prior.RulesetHash, checked(prior.StateVersion + 1),
                element.ElementId, representation.RepresentationId, window.ReactingSide, input.OriginLocationId,
                input.DestinationLocationId, content.BreakdownVehicleCohort is { } cohort ? [cohort.CohortId] : []);
        var windowAfter = ActivateParticipant(window, opportunity.OpportunityId);
        return new ReactingElementMovedV2(prior.CampaignId, checked(prior.StateVersion + 1), prior.StateVersion,
            input.FromPositionId, prior.CurrentPosition.SequenceContext.GameTurn, prior.CurrentPosition.SequenceContext.OperationStage,
            input.ActingSide, input.ActionId, input.SubmittedWindowId, input.SubmittedOpportunityId,
            input.WindowId, input.OpportunityId, element.ElementId, representation.RepresentationId,
            input.OriginLocationId, input.DestinationLocationId, movement.MobilityId, movement.MobilitySources,
            movement.Cost, movement.ExpendedBefore, movement.ExpendedAfter, element.OperationalState.CohesionLevel,
            element.OperationalState.CohesionLevel, windowAfter, prior.RulesetHash, accounting,
            new CampaignBreakdownFlow.Reacting(flow.PhasingContinuation, route));
    }

    public static ReactingElementMovedReplayInput CreateReplayInput(CampaignSnapshotV11 prior,
        ContentPackV6Artifact artifact, ContentScenario scenario, CampaignReactionOpportunityId opportunityId,
        string destination)
    {
        RequireContext(prior, artifact, scenario);
        var window = prior.ReactionWindow!;
        var opportunity = RequireOpportunity(window, opportunityId);
        var options = MoveOptions(prior, artifact, scenario, opportunity);
        var option = options.SingleOrDefault(x => x.DestinationLocationId == destination)
            ?? throw Unsupported("The Reaction destination is not currently legal.");
        var publicWindow = CreateWindowCapability(prior);
        var publicOpportunity = CampaignObservationV6DisclosureIdentity.CreateOpportunity(publicWindow,
            prior.StateVersion, CampaignObservationV6DisclosureIdentity.CreateCapabilityKey(options));
        var candidate = new MoveReactingElementAction(publicWindow, publicOpportunity, option.OriginLocationId,
            destination, option.CostBreakdown);
        return new ReactingElementMovedReplayInput(prior.CampaignId, prior.StateVersion,
            prior.CurrentPosition.SequenceContext.PositionId, window.ReactingSide, candidate.ActionId,
            publicWindow, publicOpportunity, window.WindowId, opportunityId, option.OriginLocationId, destination);
    }

    internal static ObservedReactionMoveOption[] MoveOptions(CampaignSnapshotV11 prior,
        ContentPackV6Artifact artifact, ContentScenario scenario, CampaignFrozenReactionOpportunity opportunity)
    {
        var route = ((CampaignBreakdownFlow.Reacting)prior.BreakdownFlow).ReactorRoute;
        return MoveOptions(prior.World, artifact, scenario, prior.ReactionWindow!, opportunity,
            route is null ? null : (route.ElementId, route.RepresentationId));
    }

    internal static CampaignFrozenReactionOpportunity RequireOpportunity(CampaignReactionWindow window,
        CampaignReactionOpportunityId opportunityId)
    {
        if (window.ResolvedOpportunityIds.Contains(opportunityId)
            || (window.ActiveOpportunityId is { } active && active != opportunityId))
            throw Unsupported("Only an unresolved current Reaction participant may move.");
        return window.FrozenOpportunities.SingleOrDefault(x => x.OpportunityId == opportunityId)
            ?? throw Unsupported("Unknown frozen Reaction opportunity.");
    }

    internal static CampaignReactionWindow ActivateParticipant(CampaignReactionWindow window,
        CampaignReactionOpportunityId opportunityId) => new(window.WindowId, window.TriggerCommittedStateVersion,
        window.PhasingSide, window.ReactingSide, window.ReactingPosition, window.TriggerAuthority,
        window.ApparentTrigger, window.FrozenOpportunities, window.ResolvedOpportunityIds, opportunityId);

    internal static ObservedReactionMoveOption[] MoveOptions(CampaignWorldSnapshotV6 world, ContentPackV6Artifact artifact,
        ContentScenario scenario, CampaignReactionWindow window, CampaignFrozenReactionOpportunity opportunity,
        (string ElementId, string RepresentationId)? activeBinding,
        Func<ContentCombatElement, CampaignElementStateV5, ContentHexEdge, ContentHex, IReadOnlyList<CampaignBreakdownStep>>? accounting = null)
    {
        var definition = artifact.Definition.LegacyDefinition;
        var id = opportunity.ReactingRepresentation.BoundElementIds.Single();
        var content = definition.Elements.Single(x => x.ElementId == id);
        var element = world.Elements.Single(x => x.ElementId == id);
        var representation = world.Representations.Single(x => x.RepresentationId == opportunity.ReactingRepresentation.RepresentationId);
        var facts = artifact.Definition.ElementCombatFacts.Single(x => x.ElementId == id);
        if (Cna1979Combat.FindClassification(facts.CombatClassificationId)?.Kind != ZocCombatClassificationKind.CombatUnit
            || content.SideId != CampaignSnapshotSerializer.FormatSide(window.ReactingSide)
            || element.ReserveStatus != CampaignElementReserveStatus.None
            || element.OperationalState.CohesionLevel <= Cna1979Zoc.MinimumExclusiveCohesion
            || (activeBinding is { } route && (route.ElementId != id || route.RepresentationId != representation.RepresentationId))
            || (activeBinding is null && element.CurrentLocationId != opportunity.ReactingRepresentation.CurrentLocationId))
            return [];
        var controlled = CampaignElementMovedV3Factory.DeriveControlledLocationIds(world, artifact, scenario, window.PhasingSide);
        var enemyLocations = world.Elements.Join(definition.Elements, x => x.ElementId, x => x.ElementId,
                (state, immutable) => (state, immutable))
            .Where(x => x.immutable.SideId != content.SideId).Select(x => x.state.CurrentLocationId).ToHashSet(StringComparer.Ordinal);
        if (controlled.Contains(element.CurrentLocationId, StringComparer.Ordinal) || enemyLocations.Contains(element.CurrentLocationId)) return [];
        var result = new List<ObservedReactionMoveOption>();
        foreach (var edge in definition.Edges.Where(x => x.FirstLocationId == element.CurrentLocationId || x.SecondLocationId == element.CurrentLocationId))
        {
            var destinationId = edge.FirstLocationId == element.CurrentLocationId ? edge.SecondLocationId : edge.FirstLocationId;
            if (enemyLocations.Contains(destinationId) || controlled.Contains(destinationId, StringComparer.Ordinal)) continue;
            var destination = definition.Locations.Single(x => x.LocationId == destinationId);
            try
            {
                var movement = CampaignElementMovedV3Factory.CalculateMovement(world, definition, content, element,
                    edge, destination, element.CurrentLocationId, destinationId);
                var movedRepresentation = new CampaignMapRepresentationState(representation.RepresentationId,
                    destinationId, representation.BindingKind, representation.BoundElementIds);
                var projected = CampaignElementMovedV3Factory.ProjectMoveForAuthority(world, element,
                    movedRepresentation, movement.ExpendedAfter, accounting?.Invoke(content, element, edge, destination) ?? [],
                    element.OperationalState.MovementEnded);
                if (CampaignWorldV6Validator.IsValid(projected, artifact, scenario))
                    result.Add(new ObservedReactionMoveOption(element.CurrentLocationId, destinationId, movement.ActionCost));
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or ArithmeticException)
            { /* An inadmissible edge contributes no capability. */ }
        }
        return result.OrderBy(x => x.DestinationLocationId, StringComparer.Ordinal).ToArray();
    }

    internal static IReadOnlyList<CampaignBreakdownStep> CalculateAccounting(CampaignSnapshotV11 prior,
        ContentPackV6Artifact artifact, ContentCombatElement content, CampaignElementStateV5 element,
        ContentHexEdge edge, ContentHex destination) => CalculateAccounting(content, element, edge, destination,
            () => CampaignSnapshotV11Validator.ApplicableWeather(prior, artifact, destination.LocationId));

    internal static IReadOnlyList<CampaignBreakdownStep> CalculateAccounting(ContentCombatElement content,
        CampaignElementStateV5 element, ContentHexEdge edge, ContentHex destination, Func<BreakdownWeatherKind> weather) =>
        content.BreakdownVehicleCohort is { } cohort
            ? [CampaignBreakdownAccounting.Calculate(cohort,
                element.OperationalState.VehicleBreakdownState ?? throw Unsupported("Missing vehicle ledger."), edge,
                destination, element.CurrentLocationId, weather())]
            : [];

    private static void RequireContext(CampaignSnapshotV11 prior, ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        if (!CampaignSnapshotV11Validator.IsValid(prior, artifact, scenario)
            || prior.CurrentPosition.Kind != CampaignPositionV11Kind.Reaction
            || prior.BreakdownFlow is not CampaignBreakdownFlow.Reacting || prior.ReactionWindow is null)
            throw Unsupported("Reaction movement requires certified active Reaction authority.");
    }

    // Same public domain and ordered input as the predecessor; only the dormant ruleset identity changes.
    internal static string CreateWindowCapability(CampaignSnapshotV11 prior)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("domain", "sandtable.observation.reaction-window.v1");
            writer.WriteString("campaignId", prior.CampaignId);
            writer.WriteString("rulesetHash", prior.RulesetHash);
            writer.WriteNumber("committedStateVersion", prior.ReactionWindow!.TriggerCommittedStateVersion);
            writer.WriteString("reactingSide", CampaignSnapshotSerializer.FormatSide(prior.ReactionWindow.ReactingSide));
            writer.WriteEndObject();
        }
        return $"sha256:{Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()))}";
    }
    private static InvalidOperationException Unsupported(string message) => new(message);
}
