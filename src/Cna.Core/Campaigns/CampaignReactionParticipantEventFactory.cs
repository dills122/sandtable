using Cna.Core.Actions;
using Cna.Core.Content;
using Cna.Core.Observations;

namespace Cna.Core.Campaigns;

internal static class CampaignReactionParticipantEventFactory
{
    public static ReactingElementMoved CreateMove(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        MoveReactingElementIntent intent)
    {
        ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(intent);
        var authority = ResolveMove(
            prior,
            artifact,
            scenario,
            prior.CampaignId,
            intent.ExpectedStateVersion,
            intent.ExpectedPositionId,
            intent.Side,
            intent.ActionId,
            intent.WindowId,
            intent.OpportunityId,
            intent.OriginLocationId,
            intent.DestinationLocationId,
            expectedWindowId: null,
            expectedOpportunityId: null);
        return CreateMove(prior, artifact, scenario, authority);
    }

    public static ReactingElementMoved CreateMove(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        ReactingElementMovedReplayInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var authority = ResolveMove(
            prior,
            artifact,
            scenario,
            input.CampaignId,
            input.PriorStateVersion,
            input.FromPositionId,
            input.ActingSide,
            input.ActionId,
            input.SubmittedWindowId,
            input.SubmittedOpportunityId,
            input.OriginLocationId,
            input.DestinationLocationId,
            input.WindowId,
            input.OpportunityId);
        return CreateMove(prior, artifact, scenario, authority);
    }

    public static ReactionParticipantCompleted CreateCompletion(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        CompleteReactionParticipantIntent intent)
    {
        ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(intent);
        var authority = ResolveCompletion(
            prior,
            artifact,
            scenario,
            prior.CampaignId,
            intent.ExpectedStateVersion,
            intent.ExpectedPositionId,
            intent.Side,
            intent.ActionId,
            intent.WindowId,
            intent.OpportunityId,
            expectedWindowId: null,
            expectedOpportunityId: null);
        return CreateCompletion(prior, authority);
    }

    public static ReactionParticipantCompleted CreateCompletion(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        ReactionParticipantCompletedReplayInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var authority = ResolveCompletion(
            prior,
            artifact,
            scenario,
            input.CampaignId,
            input.PriorStateVersion,
            input.FromPositionId,
            input.ActingSide,
            input.ActionId,
            input.SubmittedWindowId,
            input.SubmittedOpportunityId,
            input.WindowId,
            input.OpportunityId);
        return CreateCompletion(prior, authority);
    }

    private static ReactingElementMoved CreateMove(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        MoveAuthority authority)
    {
        var definition = artifact.Definition.LegacyDefinition;
        var edge = CampaignElementMovedV2Factory.FindEdge(
            definition,
            authority.Candidate.OriginLocationId,
            authority.Candidate.DestinationLocationId)
            ?? throw Unsupported("The requested Reaction destination is not adjacent.");
        var destination = definition.Locations.SingleOrDefault(value => string.Equals(
            value.LocationId,
            authority.Candidate.DestinationLocationId,
            StringComparison.Ordinal))
            ?? throw Unsupported("The requested Reaction destination is absent from the map.");
        var movement = CampaignElementMovedV2Factory.CalculateMovement(
            prior,
            definition,
            authority.ContentElement,
            authority.Element,
            edge,
            destination,
            authority.Candidate.OriginLocationId,
            authority.Candidate.DestinationLocationId);
        if (movement.ActionCost != authority.Candidate.CostBreakdown)
        {
            throw Unsupported("The current Reaction action cost differs from authority.");
        }

        var windowAfter = CopyWindow(
            authority.Window,
            authority.Window.ResolvedOpportunityIds,
            authority.Opportunity.OpportunityId);
        return new ReactingElementMoved(
            prior.CampaignId,
            checked(prior.StateVersion + 1),
            prior.StateVersion,
            authority.FromPositionId,
            authority.Window.ReactingPosition.SuspendedMovementPosition.GameTurn,
            authority.Window.ReactingPosition.SuspendedMovementPosition.OperationStage,
            authority.Window.ReactingSide,
            authority.Candidate.ActionId,
            authority.Candidate.WindowId,
            authority.Candidate.OpportunityId,
            authority.Window.WindowId,
            authority.Opportunity.OpportunityId,
            authority.Element.ElementId,
            authority.Representation.RepresentationId,
            authority.Candidate.OriginLocationId,
            authority.Candidate.DestinationLocationId,
            movement.MobilityId,
            movement.MobilitySources,
            movement.Cost,
            movement.ExpendedBefore,
            movement.ExpendedAfter,
            authority.Element.OperationalState.CohesionLevel,
            authority.Element.OperationalState.CohesionLevel,
            windowAfter);
    }

    private static ReactionParticipantCompleted CreateCompletion(
        CampaignSnapshotV10 prior,
        CompletionAuthority authority)
    {
        var resolved = authority.Window.ResolvedOpportunityIds
            .Append(authority.Opportunity.OpportunityId)
            .ToArray();
        var windowAfter = CopyWindow(authority.Window, resolved, activeOpportunityId: null);
        return new ReactionParticipantCompleted(
            prior.CampaignId,
            checked(prior.StateVersion + 1),
            prior.StateVersion,
            authority.FromPositionId,
            authority.Window.ReactingSide,
            authority.Candidate.ActionId,
            authority.Candidate.WindowId,
            authority.Candidate.OpportunityId,
            authority.Window.WindowId,
            authority.Opportunity.OpportunityId,
            windowAfter);
    }

    private static MoveAuthority ResolveMove(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        string campaignId,
        long priorStateVersion,
        string fromPositionId,
        Cna.Core.Rules.LandSide actingSide,
        string actionId,
        string submittedWindowId,
        string submittedOpportunityId,
        string originLocationId,
        string destinationLocationId,
        CampaignReactionWindowId? expectedWindowId,
        CampaignReactionOpportunityId? expectedOpportunityId)
    {
        var projection = ProjectAuthority(
            prior,
            artifact,
            scenario,
            campaignId,
            priorStateVersion,
            fromPositionId,
            actingSide);
        var candidate = CampaignObservationV6ActionDerivation
            .DerivePlayer(projection.Observation)
            .Candidates
            .OfType<MoveReactingElementAction>()
            .SingleOrDefault(value => string.Equals(
                value.ActionId,
                actionId,
                StringComparison.Ordinal));
        if (candidate is null
            || !string.Equals(candidate.WindowId, submittedWindowId, StringComparison.Ordinal)
            || !string.Equals(candidate.OpportunityId, submittedOpportunityId,
                StringComparison.Ordinal)
            || !string.Equals(candidate.OriginLocationId, originLocationId,
                StringComparison.Ordinal)
            || !string.Equals(candidate.DestinationLocationId, destinationLocationId,
                StringComparison.Ordinal))
        {
            throw Unsupported("The requested Reaction move is not a current exact action.");
        }

        var binding = ResolveBinding(
            prior.ReactionWindow!,
            projection,
            submittedOpportunityId,
            expectedWindowId,
            expectedOpportunityId);
        var sideId = CampaignSnapshotSerializer.FormatSide(actingSide);
        var definition = artifact.Definition.LegacyDefinition;
        var contentElement = definition.Elements.SingleOrDefault(value =>
            string.Equals(value.ElementId, binding.ElementId, StringComparison.Ordinal)
            && string.Equals(value.SideId, sideId, StringComparison.Ordinal)
            && value.PlacementMode == ContentPlacementMode.Independent)
            ?? throw Unsupported("The reacting element is not owned and independently placed.");
        var element = prior.World.Elements.Single(value => string.Equals(
            value.ElementId,
            binding.ElementId,
            StringComparison.Ordinal));
        var representation = prior.World.Representations.SingleOrDefault(value =>
            string.Equals(
                value.RepresentationId,
                binding.Opportunity.ReactingRepresentation.RepresentationId,
                StringComparison.Ordinal)
            && value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
            && value.BoundElementIds.Count == 1
            && string.Equals(value.BoundElementIds[0], element.ElementId, StringComparison.Ordinal))
            ?? throw Unsupported("The reacting element has no current unique representation.");
        if (!string.Equals(element.CurrentLocationId, candidate.OriginLocationId,
                StringComparison.Ordinal)
            || !string.Equals(representation.CurrentLocationId, candidate.OriginLocationId,
                StringComparison.Ordinal))
        {
            throw Unsupported("The reacting element is absent from the current action origin.");
        }

        return new MoveAuthority(
            prior.ReactionWindow!,
            binding.Opportunity,
            candidate,
            contentElement,
            element,
            representation,
            fromPositionId);
    }

    private static CompletionAuthority ResolveCompletion(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        string campaignId,
        long priorStateVersion,
        string fromPositionId,
        Cna.Core.Rules.LandSide actingSide,
        string actionId,
        string submittedWindowId,
        string submittedOpportunityId,
        CampaignReactionWindowId? expectedWindowId,
        CampaignReactionOpportunityId? expectedOpportunityId)
    {
        var projection = ProjectAuthority(
            prior,
            artifact,
            scenario,
            campaignId,
            priorStateVersion,
            fromPositionId,
            actingSide);
        var candidate = CampaignObservationV6ActionDerivation
            .DerivePlayer(projection.Observation)
            .Candidates
            .OfType<CompleteReactionParticipantAction>()
            .SingleOrDefault(value => string.Equals(
                value.ActionId,
                actionId,
                StringComparison.Ordinal));
        if (candidate is null
            || !string.Equals(candidate.WindowId, submittedWindowId, StringComparison.Ordinal)
            || !string.Equals(candidate.OpportunityId, submittedOpportunityId,
                StringComparison.Ordinal))
        {
            throw Unsupported("The requested participant completion is not a current exact action.");
        }

        var binding = ResolveBinding(
            prior.ReactionWindow!,
            projection,
            submittedOpportunityId,
            expectedWindowId,
            expectedOpportunityId);
        if (prior.ReactionWindow!.ActiveOpportunityId != binding.Opportunity.OpportunityId)
        {
            throw Unsupported("Only the active Reaction participant can complete.");
        }

        return new CompletionAuthority(
            prior.ReactionWindow,
            binding.Opportunity,
            candidate,
            fromPositionId);
    }

    private static CampaignObservationV6AuthorityProjection ProjectAuthority(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        string campaignId,
        long priorStateVersion,
        string fromPositionId,
        Cna.Core.Rules.LandSide actingSide)
    {
        ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        var window = prior.ReactionWindow;
        if (!CampaignSnapshotV10Validator.IsValid(prior, artifact, scenario)
            || window is null
            || prior.CurrentPosition.Kind != CampaignPositionV10Kind.Reaction
            || prior.StateVersion != priorStateVersion
            || !string.Equals(prior.CampaignId, campaignId, StringComparison.Ordinal)
            || !string.Equals(
                window.ReactingPosition.SuspendedMovementPosition.PositionId,
                fromPositionId,
                StringComparison.Ordinal)
            || window.ReactingSide != actingSide)
        {
            throw Unsupported("Reaction participant authority is not admitted.");
        }

        var controlled = CampaignElementMovedV2Factory.DeriveControlledLocationIds(
            prior.World,
            artifact,
            scenario,
            window.PhasingSide);
        return CampaignObservationV6Projector.ProjectWithAuthority(
            prior,
            artifact,
            scenario,
            actingSide,
            new CampaignObservationV6AuthorityFacts(controlled, []));
    }

    private static AuthorityBinding ResolveBinding(
        CampaignReactionWindow window,
        CampaignObservationV6AuthorityProjection projection,
        string submittedOpportunityId,
        CampaignReactionWindowId? expectedWindowId,
        CampaignReactionOpportunityId? expectedOpportunityId)
    {
        var alias = projection.ReactionAliases.SingleOrDefault(value => string.Equals(
            value.PublicId,
            submittedOpportunityId,
            StringComparison.Ordinal));
        if (alias is null
            || (expectedWindowId is not null && expectedWindowId != window.WindowId)
            || (expectedOpportunityId is not null
                && !string.Equals(expectedOpportunityId.Value, alias.AuthorityId,
                    StringComparison.Ordinal)))
        {
            throw Unsupported("The submitted Reaction capability has no current authority binding.");
        }

        var opportunity = window.FrozenOpportunities.Single(value => string.Equals(
            value.OpportunityId.Value,
            alias.AuthorityId,
            StringComparison.Ordinal));
        return new AuthorityBinding(
            opportunity,
            AssertSingleElement(opportunity.ReactingRepresentation));
    }

    private static string AssertSingleElement(CampaignMapRepresentationState representation) =>
        representation.BoundElementIds.Count == 1
            ? representation.BoundElementIds[0]
            : throw Unsupported("A Reaction participant must bind one element.");

    private static CampaignReactionWindow CopyWindow(
        CampaignReactionWindow window,
        IEnumerable<CampaignReactionOpportunityId> resolvedOpportunityIds,
        CampaignReactionOpportunityId? activeOpportunityId) => new(
        window.WindowId,
        window.TriggerCommittedStateVersion,
        window.PhasingSide,
        window.ReactingSide,
        window.ReactingPosition,
        window.TriggerAuthority,
        window.ApparentTrigger,
        window.FrozenOpportunities,
        resolvedOpportunityIds,
        activeOpportunityId);

    private static InvalidOperationException Unsupported(string message) => new(message);

    private sealed record AuthorityBinding(
        CampaignFrozenReactionOpportunity Opportunity,
        string ElementId);

    private sealed record MoveAuthority(
        CampaignReactionWindow Window,
        CampaignFrozenReactionOpportunity Opportunity,
        MoveReactingElementAction Candidate,
        ContentCombatElement ContentElement,
        CampaignElementStateV5 Element,
        CampaignMapRepresentationState Representation,
        string FromPositionId);

    private sealed record CompletionAuthority(
        CampaignReactionWindow Window,
        CampaignFrozenReactionOpportunity Opportunity,
        CompleteReactionParticipantAction Candidate,
        string FromPositionId);
}
