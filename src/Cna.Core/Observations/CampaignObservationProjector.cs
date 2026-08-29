using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

internal static class CampaignObservationProjector
{
    public static CampaignObservationProjectionResult Project(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        LandSide observer)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(context);

        if (!Enum.IsDefined(observer))
        {
            return CampaignObservationProjectionResult.Rejected(
                CampaignObservationRejectionReason.InvalidObserver);
        }

        if (!CampaignSnapshotValidator.IsValid(snapshot, context))
        {
            return CampaignObservationProjectionResult.Rejected(
                CampaignObservationRejectionReason.InvalidState);
        }

        var definition = context.Artifact.Definition;
        var locations = definition.Locations.Select(location =>
            new CampaignObservationLocation(location.LocationId, location.TerrainId)).ToArray();
        var edges = definition.Edges.Select(edge => new CampaignObservationEdge(
            edge.FirstLocationId,
            edge.SecondLocationId,
            edge.Features.Select(feature => new CampaignObservationEdgeFeature(
                feature.FeatureId,
                feature.DirectionFromLocationId)).ToArray())).ToArray();
        var ownElements = ProjectOwnElements(definition, snapshot.World, observer);
        var apparentOpposingPresences = ProjectApparentOpposingPresences(
            definition,
            snapshot.World,
            observer);
        var sequence = snapshot.SequencePosition;
        var activeSide = sequence.ActorRole == LandActorRole.FirstActingSide
            ? FirstActingSideResolver.Resolve(snapshot)
            : sequence.ActiveSide;
        var position = new CampaignObservationPosition(
            sequence.PositionId,
            sequence.GameTurn,
            sequence.OperationStage,
            sequence.StageId,
            sequence.PhaseId,
            sequence.SegmentId,
            sequence.StepId,
            sequence.ActorRole,
            activeSide,
            snapshot.InitiativeHolder);
        var observation = new CampaignObservation(
            CampaignObservation.CurrentContractVersion,
            CampaignObservation.CurrentPolicyId,
            snapshot.CampaignId,
            ProjectAudienceStateVersion(snapshot, observer),
            snapshot.RulesetHash,
            context.Scenario.ScenarioId,
            observer,
            position,
            CampaignObservationWeatherSelector.Select(
                snapshot.GameTurn,
                snapshot.OperationStage,
                snapshot.OperationStageWeather),
            locations,
            edges,
            ownElements,
            apparentOpposingPresences);

        return CampaignObservationProjectionResult.Projected(observation);
    }

    private static long ProjectAudienceStateVersion(
        CampaignSnapshot snapshot,
        LandSide observer)
    {
        var reserve = ReserveDesignationEvent.ReservePosition(snapshot.GameTurn);
        var movement = Cna1979LandSequence.GetNext(reserve);
        if (snapshot.SequencePosition != reserve
            && snapshot.SequencePosition != movement)
        {
            return snapshot.StateVersion;
        }

        var firstSide = FirstActingSideResolver.Resolve(snapshot);
        if (observer == firstSide)
        {
            return snapshot.StateVersion;
        }

        var hiddenDesignationCount = snapshot.World.Elements.Count(element =>
            element.ReserveStatus == CampaignElementReserveStatus.ReserveI);
        return checked(snapshot.StateVersion - hiddenDesignationCount);
    }

    internal static IReadOnlyList<ObservedOwnElement> ProjectOwnElements(
        ContentPackDefinition definition,
        CampaignWorldSnapshot world,
        LandSide observer)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(world);

        var sideId = observer switch
        {
            LandSide.Axis => "axis",
            LandSide.Commonwealth => "commonwealth",
            _ => throw new ArgumentOutOfRangeException(nameof(observer)),
        };
        var worldByElement = world.Elements.ToDictionary(
            element => element.ElementId,
            StringComparer.Ordinal);

        return definition.Elements
            .Where(element => string.Equals(element.SideId, sideId, StringComparison.Ordinal)
                && element.PlacementMode == ContentPlacementMode.Independent)
            .Select(element =>
            {
                if (!worldByElement.TryGetValue(element.ElementId, out var state))
                {
                    throw new InvalidOperationException(
                        $"World snapshot is missing element '{element.ElementId}' defined by the content pack.");
                }

                return new ObservedOwnElement(
                    element.ElementId,
                    element.ParentFormationId,
                    element.OrganizationId,
                    element.BaseCapabilityPointAllowance,
                    state.CurrentLocationId,
                    ProjectReserveStatus(state.ReserveStatus),
                    element.MobilityId,
                    state.OperationalState.LedgerGameTurn,
                    state.OperationalState.LedgerOperationStage,
                    state.OperationalState.CapabilityPointsExpended,
                    state.OperationalState.CohesionLevel,
                    ProjectVehicleBreakdownRisk(element, state));
            })
            .ToArray();
    }

    private static ObservedApparentPresence[] ProjectApparentOpposingPresences(
        ContentPackDefinition definition,
        CampaignWorldSnapshot world,
        LandSide observer)
    {
        var observerSideId = observer switch
        {
            LandSide.Axis => "axis",
            LandSide.Commonwealth => "commonwealth",
            _ => throw new ArgumentOutOfRangeException(nameof(observer)),
        };
        var contentByElementId = definition.Elements.ToDictionary(
            element => element.ElementId,
            StringComparer.Ordinal);

        return world.Representations
            .Where(representation => representation.BoundElementIds.Any(elementId =>
                contentByElementId.TryGetValue(elementId, out var element)
                && !string.Equals(element.SideId, observerSideId, StringComparison.Ordinal)))
            .Select(representation => new ObservedApparentPresence(
                representation.RepresentationId,
                representation.CurrentLocationId,
                exertsZoc: false))
            .ToArray();
    }

    private static ObservedOwnVehicleBreakdownRisk? ProjectVehicleBreakdownRisk(
        ContentCombatElement element,
        CampaignElementState state)
    {
        var cohort = element.BreakdownVehicleCohort;
        var breakdown = state.OperationalState.VehicleBreakdownState;
        if (cohort is null || breakdown is null)
        {
            return null;
        }

        return new ObservedOwnVehicleBreakdownRisk(
            breakdown.CohortId,
            cohort.VehicleTypeId,
            cohort.ProfileId,
            breakdown.CumulativeBreakdownPoints,
            breakdown.SandstormAttributedBreakdownPoints,
            breakdown.HighestEffectiveCheckedBandId,
            breakdown.WorkingPointCount,
            breakdown.BrokenPointCount);
    }

    private static CampaignObservationReserveStatus ProjectReserveStatus(
        CampaignElementReserveStatus status) => status switch
        {
            CampaignElementReserveStatus.None => CampaignObservationReserveStatus.None,
            CampaignElementReserveStatus.ReserveI => CampaignObservationReserveStatus.ReserveI,
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };
}
