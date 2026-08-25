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
            ownElements);

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
                var state = worldByElement[element.ElementId];
                return new ObservedOwnElement(
                    element.ElementId,
                    element.ParentFormationId,
                    element.OrganizationId,
                    element.BaseCapabilityPointAllowance,
                    state.CurrentLocationId,
                    ProjectReserveStatus(state.ReserveStatus));
            })
            .ToArray();
    }

    private static CampaignObservationReserveStatus ProjectReserveStatus(
        CampaignElementReserveStatus status) => status switch
        {
            CampaignElementReserveStatus.None => CampaignObservationReserveStatus.None,
            CampaignElementReserveStatus.ReserveI => CampaignObservationReserveStatus.ReserveI,
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };
}
