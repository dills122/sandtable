using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Actions;

internal static class CampaignMovementActionDerivation
{
    internal static IReadOnlyList<CampaignActionCandidate> Derive(
        CampaignObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        if (!IsSupportedMovementPosition(observation))
        {
            return Array.Empty<CampaignActionCandidate>();
        }

        var candidates = observation.OwnElements
            .SelectMany(element => DeriveMoves(observation, element))
            .Cast<CampaignActionCandidate>()
            .Append(new CompleteMovementSegmentAction())
            .OrderBy(candidate => candidate.Kind, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.ActionId, StringComparer.Ordinal)
            .ToArray();
        return Array.AsReadOnly(candidates);
    }

    private static bool IsSupportedMovementPosition(CampaignObservation observation) =>
        observation.Position.OperationStage == 1
        && observation.Position.StageId == LandStageIds.Operation
        && observation.Position.PhaseId == LandPhaseIds.MovementAndCombat
        && observation.Position.SegmentId == LandSegmentIds.Movement
        && observation.Position.ActorRole == LandActorRole.FirstActingSide
        && observation.Position.ActiveSide == observation.Observer;

    private static IEnumerable<MoveElementAction> DeriveMoves(
        CampaignObservation observation,
        ObservedOwnElement element)
    {
        if (element.ReserveStatus != CampaignObservationReserveStatus.None
            || element.CohesionLevel <= -26
            || element.LedgerGameTurn != observation.Position.GameTurn
            || element.LedgerOperationStage != observation.Position.OperationStage)
        {
            yield break;
        }

        foreach (var edge in observation.Edges.Where(value =>
            value.FirstLocationId == element.CurrentLocationId
            || value.SecondLocationId == element.CurrentLocationId))
        {
            var destinationId = edge.FirstLocationId == element.CurrentLocationId
                ? edge.SecondLocationId
                : edge.FirstLocationId;
            var candidate = TryCreateMove(observation, element, edge, destinationId);
            if (candidate is not null)
            {
                yield return candidate;
            }
        }
    }

    private static MoveElementAction? TryCreateMove(
        CampaignObservation observation,
        ObservedOwnElement element,
        CampaignObservationEdge edge,
        string destinationId)
    {
        if (observation.ApparentOpposingPresences.Any(presence => presence.ExertsZoc)
            || observation.ApparentOpposingPresences.Any(presence =>
                presence.CurrentLocationId == element.CurrentLocationId
                || presence.CurrentLocationId == destinationId))
        {
            return null;
        }

        var cost = CampaignMovementActionCostDerivation.TryCalculate(
            observation.Locations,
            observation.OwnElements,
            element,
            edge,
            destinationId);
        return cost is null
            ? null
            : new MoveElementAction(
                element.ElementId,
                element.CurrentLocationId,
                destinationId,
                cost);
    }
}
