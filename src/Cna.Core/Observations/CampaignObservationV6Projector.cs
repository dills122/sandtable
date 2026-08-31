using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

internal static class CampaignObservationV6Projector
{
    public static CampaignObservationV6 Project(
        CampaignSnapshotV10 snapshot,
        ContentPackV5Artifact artifact,
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

        if (!CampaignSnapshotV10Validator.IsValid(snapshot, artifact, scenario))
        {
            throw new ArgumentException(
                "Observation 6 projection requires one admitted Snapshot 10 authority.",
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
        var sequence = snapshot.CurrentPosition.Kind == CampaignPositionV10Kind.Sequence
            ? snapshot.CurrentPosition.SequencePosition!
            : snapshot.CurrentPosition.ReactingPosition!.SuspendedMovementPosition;
        var position = new CampaignObservationPosition(
            sequence.PositionId,
            sequence.GameTurn,
            sequence.OperationStage,
            sequence.StageId,
            sequence.PhaseId,
            sequence.SegmentId,
            sequence.StepId,
            sequence.ActorRole,
            sequence.ActiveSide,
            snapshot.InitiativeHolder);
        var decision = ProjectDecisionState(snapshot, observer, ownElements);

        return new CampaignObservationV6(
            CampaignObservationV6.CurrentContractVersion,
            CampaignObservationV6.CurrentPolicyId,
            snapshot.CampaignId,
            snapshot.StateVersion,
            snapshot.RulesetHash,
            scenario.ScenarioId,
            observer,
            position,
            CampaignObservationWeatherSelector.Select(
                sequence.GameTurn,
                sequence.OperationStage,
                snapshot.OperationStageWeather),
            locations,
            edges,
            ownElements,
            apparent,
            authorityFacts.ApparentEnemyControlledLocationIds,
            decision);
    }

    private static CampaignObservationDecisionState ProjectDecisionState(
        CampaignSnapshotV10 snapshot,
        LandSide observer,
        IReadOnlyList<ObservedOwnElement> ownElements)
    {
        var window = snapshot.ReactionWindow;
        if (window is null)
        {
            return new CampaignObservationNormalDecisionState();
        }

        if (observer == window.PhasingSide)
        {
            return new CampaignObservationPhasingWaitingDecisionState(window.WindowId.Value);
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
        var opportunities = window.FrozenOpportunities
            .Where(value => !resolvedIds.Contains(value.OpportunityId.Value))
            .Select(value =>
            {
                if (!value.ReactingRepresentation.BoundElementIds.All(ownElementIds.Contains))
                {
                    throw new ArgumentException(
                        "A reacting Observation opportunity must belong entirely to the audience.",
                        nameof(snapshot));
                }

                return new ObservedReactionOpportunity(
                    value.OpportunityId.Value,
                    value.ReactingRepresentation.RepresentationId);
            })
            .ToArray();
        ObservedReactionParticipant? active = null;
        if (window.ActiveOpportunityId is not null)
        {
            var matching = opportunities.Single(value => string.Equals(
                value.OpportunityId,
                window.ActiveOpportunityId.Value,
                StringComparison.Ordinal));
            active = new ObservedReactionParticipant(
                matching.OpportunityId,
                matching.RepresentationId);
        }

        return new CampaignObservationReactingDecisionState(
            window.WindowId.Value,
            new ObservedApparentReactionTrigger(
                window.ApparentTrigger.ApparentRepresentationId,
                window.ApparentTrigger.OriginLocationId,
                window.ApparentTrigger.DestinationLocationId),
            opportunities,
            active);
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
}
