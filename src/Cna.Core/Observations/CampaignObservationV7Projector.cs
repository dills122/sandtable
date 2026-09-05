using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

internal sealed record CampaignObservationV7AuthorityProjection(
    CampaignObservationV7 Observation,
    IReadOnlyList<CampaignObservationV6DisclosureAlias> ReactionAliases);

internal static class CampaignObservationV7Projector
{
    public static CampaignObservationV7 Project(
        CampaignSnapshotV11 snapshot,
        ContentPackV6Artifact artifact,
        ContentScenario scenario,
        LandSide observer,
        CampaignObservationV6AuthorityFacts authorityFacts) => ProjectWithAuthority(
        snapshot,
        artifact,
        scenario,
        observer,
        authorityFacts).Observation;

    public static CampaignObservationV7AuthorityProjection ProjectWithAuthority(
        CampaignSnapshotV11 snapshot,
        ContentPackV6Artifact artifact,
        ContentScenario scenario,
        LandSide observer,
        CampaignObservationV6AuthorityFacts authorityFacts)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(authorityFacts);
        if (!Enum.IsDefined(observer))
        {
            throw new ArgumentOutOfRangeException(nameof(observer));
        }

        if (!CampaignSnapshotV11Validator.IsValid(snapshot, artifact, scenario))
        {
            throw new ArgumentException(
                "Observation 7 projection requires one admitted Snapshot 11 authority.",
                nameof(snapshot));
        }

        var definition = artifact.Definition.LegacyDefinition;
        var locations = definition.Locations.Select(value =>
            new CampaignObservationLocation(value.LocationId, value.TerrainId)).ToArray();
        var knownLocations = locations.Select(value => value.LocationId)
            .ToHashSet(StringComparer.Ordinal);
        if (authorityFacts.ApparentEnemyControlledLocationIds.Any(value =>
            !knownLocations.Contains(value)))
        {
            throw new ArgumentException(
                "Apparent enemy-controlled locations must belong to published topology.",
                nameof(authorityFacts));
        }

        var edges = definition.Edges.Select(value => new CampaignObservationEdge(
            value.FirstLocationId,
            value.SecondLocationId,
            value.Features.Select(feature => new CampaignObservationEdgeFeature(
                feature.FeatureId,
                feature.DirectionFromLocationId)).ToArray())).ToArray();
        var sideId = FormatSideId(observer);
        var elementsById = definition.Elements.ToDictionary(
            value => value.ElementId,
            StringComparer.Ordinal);
        var worldByElement = snapshot.World.Elements.ToDictionary(
            value => value.ElementId,
            StringComparer.Ordinal);
        var ownElements = definition.Elements
            .Where(value => string.Equals(value.SideId, sideId, StringComparison.Ordinal)
                && value.PlacementMode == ContentPlacementMode.Independent)
            .Select(value => ProjectOwnElement(value, worldByElement[value.ElementId]))
            .ToArray();
        var opposingRepresentations = snapshot.World.Representations
            .Where(representation => representation.BoundElementIds.Any(elementId =>
                elementsById.TryGetValue(elementId, out var element)
                && !string.Equals(element.SideId, sideId, StringComparison.Ordinal)))
            .ToArray();
        var opposingRepresentationIds = opposingRepresentations
            .Select(value => value.RepresentationId)
            .ToHashSet(StringComparer.Ordinal);
        if (authorityFacts.ApparentZocRepresentationIds.Any(value =>
            !opposingRepresentationIds.Contains(value)))
        {
            throw new ArgumentException(
                "Apparent ZOC representation IDs must belong to visible opposing presences.",
                nameof(authorityFacts));
        }

        var exerting = authorityFacts.ApparentZocRepresentationIds
            .ToHashSet(StringComparer.Ordinal);
        var apparent = opposingRepresentations.Select(value =>
            new ObservedApparentPresence(
                value.RepresentationId,
                value.CurrentLocationId,
                exerting.Contains(value.RepresentationId))).ToArray();
        var sequence = snapshot.CurrentPosition.SequenceContext;
        var publicSequence = snapshot.CurrentPosition.Kind == CampaignPositionV11Kind.BreakdownStop
            ? Cna1979LandSequenceV4.CreateBreakdownStopPosition(sequence) : sequence;
        var activeSide = publicSequence.ActorRole == LandActorRole.FirstActingSide
            ? snapshot.OperationStageOrders.Single(value => value.GameTurn == sequence.GameTurn
                && value.OperationStage == sequence.OperationStage).FirstSide
            : publicSequence.ActiveSide;
        var position = new CampaignObservationPosition(
            publicSequence.PositionId,
            publicSequence.GameTurn,
            publicSequence.OperationStage,
            publicSequence.StageId,
            publicSequence.PhaseId,
            publicSequence.SegmentId,
            publicSequence.StepId,
            publicSequence.ActorRole,
            activeSide,
            snapshot.InitiativeHolder);
        var decisionProjection = ProjectDecisionState(
            snapshot,
            artifact,
            scenario,
            observer,
            ownElements);
        var decision = decisionProjection.DecisionState;
        var suppressOwn = decision is CampaignObservationV7ReactingDecisionState
            || (decision is CampaignObservationV7BreakdownWaitingDecisionState && observer != sequence.ActiveSide);
        var publishedOwnElements = suppressOwn ? [] : ownElements;
        var publishedLots = suppressOwn ? [] : snapshot.World.BrokenVehicleLots.Where(value => value.Owner == observer)
            .GroupBy(value => (value.CohortId, value.LocationId))
            .Select(group => new ObservedBrokenVehicleLot(group.Key.CohortId, group.Key.LocationId,
                group.Aggregate(0, (total, value) => checked(total + value.PointCount))))
            .ToArray();
        var movementEndedElementIds = publishedOwnElements
            .Where(element => IsMovementEndedFor(
                worldByElement[element.ElementId].OperationalState.MovementEnded,
                sequence))
            .Select(element => element.ElementId)
            .ToArray();

        var observation = new CampaignObservationV7(
            CampaignObservationV7.CurrentContractVersion,
            CampaignObservationV7.CurrentPolicyId,
            snapshot.CampaignId,
            snapshot.StateVersion,
            snapshot.RulesetHash,
            scenario.ScenarioId,
            artifact.Definition.CapabilityProfileId,
            observer,
            position,
            CampaignObservationWeatherSelector.Select(
                sequence.GameTurn,
                sequence.OperationStage,
                snapshot.OperationStageWeather),
            locations,
            edges,
            publishedOwnElements,
            apparent,
            authorityFacts.ApparentEnemyControlledLocationIds,
            movementEndedElementIds,
            decision,
            publishedLots);
        return new CampaignObservationV7AuthorityProjection(
            observation,
            decisionProjection.ReactionAliases);
    }

    private static DecisionProjection ProjectDecisionState(
        CampaignSnapshotV11 snapshot,
        ContentPackV6Artifact artifact,
        ContentScenario scenario,
        LandSide observer,
        IReadOnlyList<ObservedOwnElement> ownElements)
    {
        if (snapshot.CurrentPosition.Kind == CampaignPositionV11Kind.BreakdownStop)
            return new DecisionProjection(new CampaignObservationV7BreakdownWaitingDecisionState(), []);
        var window = snapshot.ReactionWindow;
        if (window is null)
        {
            return new DecisionProjection(
                new CampaignObservationV7NormalDecisionState(
                    snapshot.BreakdownFlow is CampaignBreakdownFlow.Moving moving && moving.Route.Owner == observer
                        ? new ObservedMovementRoute(CampaignObservationV7DisclosureIdentity.CreateRoute(snapshot.CampaignId,
                            snapshot.RulesetHash, snapshot.StateVersion, observer, moving.Route.ElementId,
                            moving.Route.OriginLocationId, moving.Route.CurrentLocationId), moving.Route.ElementId,
                            moving.Route.OriginLocationId, moving.Route.CurrentLocationId) : null),
                []);
        }

        var publicWindowId = CampaignObservationV7DisclosureIdentity.CreateWindow(
            snapshot.CampaignId,
            snapshot.RulesetHash,
            window.TriggerCommittedStateVersion,
            window.ReactingSide);

        if (observer == window.PhasingSide)
        {
            return new DecisionProjection(
                new CampaignObservationV7PhasingWaitingDecisionState(publicWindowId),
                []);
        }

        if (observer != window.ReactingSide)
        {
            throw new ArgumentException(
                "The Observation audience does not belong to the current Reaction window.",
                nameof(observer));
        }

        var ownElementIds = ownElements.Select(value => value.ElementId)
            .ToHashSet(StringComparer.Ordinal);
        var resolvedIds = window.ResolvedOpportunityIds.Select(value => value.Value)
            .ToHashSet(StringComparer.Ordinal);
        var projectedCapabilities = window.FrozenOpportunities
            .Where(value => !resolvedIds.Contains(value.OpportunityId.Value))
            .Select(value =>
            {
                if (!value.ReactingRepresentation.BoundElementIds.All(ownElementIds.Contains))
                {
                    throw new ArgumentException(
                        "A reacting Observation opportunity must belong entirely to the audience.",
                        nameof(snapshot));
                }

                if (value.ReactingRepresentation.BoundElementIds.Count != 1)
                {
                    throw new ArgumentException(
                        "A reacting opportunity must bind one audience-owned element.",
                        nameof(snapshot));
                }

                var elementId = value.ReactingRepresentation.BoundElementIds[0];
                if (!ownElementIds.Contains(elementId))
                {
                    throw new ArgumentException(
                        "A reacting opportunity must bind one audience-owned element.",
                        nameof(snapshot));
                }

                var moveOptions = CampaignReactingElementMovedV2Factory.MoveOptions(snapshot, artifact, scenario, value);
                return new CampaignObservationV6DisclosureCapability(
                    value.OpportunityId.Value,
                    moveOptions,
                    window.ActiveOpportunityId == value.OpportunityId);
            })
            .Where(value => value.IsActive || value.MoveOptions.Count > 0)
            .ToArray();
        var aliases = CampaignObservationV6DisclosureIdentity.CreateAliases(
            publicWindowId,
            snapshot.StateVersion,
            projectedCapabilities);
        var opportunities = aliases.Select(value => new ObservedReactionOpportunity(
            value.PublicId,
            value.MoveOptions)).ToArray();
        ObservedReactionParticipant? active = null;
        if (window.ActiveOpportunityId is not null)
        {
            active = new ObservedReactionParticipant(
                aliases.Single(value => string.Equals(
                    value.AuthorityId,
                    window.ActiveOpportunityId.Value,
                    StringComparison.Ordinal)).PublicId);
        }

        return new DecisionProjection(
            new CampaignObservationV7ReactingDecisionState(
                publicWindowId,
                new ObservedApparentReactionTrigger(
                    window.ApparentTrigger.ApparentRepresentationId,
                    window.ApparentTrigger.OriginLocationId,
                    window.ApparentTrigger.DestinationLocationId),
                opportunities,
                active),
            aliases);
    }

    private static ObservedOwnElement ProjectOwnElement(
        ContentCombatElement content,
        CampaignElementStateV5 state) => new(
            content.ElementId,
            content.ParentFormationId,
            content.OrganizationId,
            content.BaseCapabilityPointAllowance,
            state.CurrentLocationId,
            ProjectReserveStatus(state.ReserveStatus),
            content.MobilityId,
            state.OperationalState.LedgerGameTurn,
            state.OperationalState.LedgerOperationStage,
            state.OperationalState.CapabilityPointsExpended,
            state.OperationalState.CohesionLevel,
            ProjectVehicleBreakdownRisk(content, state));

    private static bool IsMovementEndedFor(
        CampaignMovementEndedState? ended,
        LandSequencePosition position) => ended is not null
        && ended.SequenceContractVersion == position.ContractVersion
        && string.Equals(ended.PositionId, position.PositionId, StringComparison.Ordinal)
        && ended.GameTurn == position.GameTurn
        && ended.OperationStage == position.OperationStage
        && string.Equals(ended.StageId, position.StageId, StringComparison.Ordinal)
        && string.Equals(ended.PhaseId, position.PhaseId, StringComparison.Ordinal)
        && string.Equals(ended.SegmentId, position.SegmentId, StringComparison.Ordinal)
        && ended.PhasingSide == position.ActiveSide;

    private static ObservedOwnVehicleBreakdownRisk? ProjectVehicleBreakdownRisk(
        ContentCombatElement content,
        CampaignElementStateV5 state)
    {
        var cohort = content.BreakdownVehicleCohort;
        var breakdown = state.OperationalState.VehicleBreakdownState;
        return cohort is null || breakdown is null
            ? null
            : new ObservedOwnVehicleBreakdownRisk(
                breakdown.CohortId,
                cohort.VehicleTypeId,
                cohort.ProfileId,
                breakdown.CumulativeBreakdownPoints,
                breakdown.SandstormAttributedBreakdownPoints,
                breakdown.HighestEffectiveCheckedBandId,
                breakdown.WorkingPointCount,
                breakdown.BrokenPointCount);
    }

    private static string FormatSideId(LandSide side) => side switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };

    private static CampaignObservationReserveStatus ProjectReserveStatus(
        CampaignElementReserveStatus status) => status switch
        {
            CampaignElementReserveStatus.None => CampaignObservationReserveStatus.None,
            CampaignElementReserveStatus.ReserveI => CampaignObservationReserveStatus.ReserveI,
            CampaignElementReserveStatus.ReserveII => CampaignObservationReserveStatus.ReserveII,
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

    private sealed record DecisionProjection(
        CampaignObservationV7DecisionState DecisionState,
        IReadOnlyList<CampaignObservationV6DisclosureAlias> ReactionAliases);
}
