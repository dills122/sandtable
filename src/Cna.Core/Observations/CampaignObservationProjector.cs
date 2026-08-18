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

        var sideId = observer switch
        {
            LandSide.Axis => "axis",
            LandSide.Commonwealth => "commonwealth",
            _ => throw new ArgumentOutOfRangeException(nameof(observer)),
        };
        var worldByElement = snapshot.World.Elements.ToDictionary(
            element => element.ElementId,
            StringComparer.Ordinal);
        var definition = context.Artifact.Definition;
        var locations = definition.Locations.Select(location =>
            new CampaignObservationLocation(location.LocationId, location.TerrainId)).ToArray();
        var edges = definition.Edges.Select(edge => new CampaignObservationEdge(
            edge.FirstLocationId,
            edge.SecondLocationId,
            edge.Features.Select(feature => new CampaignObservationEdgeFeature(
                feature.FeatureId,
                feature.DirectionFromLocationId)).ToArray())).ToArray();
        var ownElements = definition.Elements
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
                    state.CurrentLocationId);
            })
            .ToArray();
        var sequence = snapshot.SequencePosition;
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
        var observation = new CampaignObservation(
            CampaignObservation.CurrentContractVersion,
            CampaignObservation.CurrentPolicyId,
            snapshot.CampaignId,
            snapshot.StateVersion,
            snapshot.RulesetHash,
            context.Scenario.ScenarioId,
            observer,
            position,
            locations,
            edges,
            ownElements);

        return CampaignObservationProjectionResult.Projected(observation);
    }
}
