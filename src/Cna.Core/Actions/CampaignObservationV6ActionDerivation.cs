using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Actions;

internal static class CampaignObservationV6ActionDerivation
{
    public static CampaignLegalActionSet DerivePlayer(CampaignObservationV6 observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var audience = ToAudience(observation.Observer);
        var candidates = observation.DecisionState switch
        {
            CampaignObservationNormalDecisionState => DeriveOrdinaryMovement(observation),
            CampaignObservationPhasingWaitingDecisionState => [],
            CampaignObservationReactingDecisionState reacting => DeriveReaction(reacting),
            _ => throw new ArgumentOutOfRangeException(nameof(observation)),
        };
        return CreateSet(observation, audience, candidates);
    }

    public static CampaignLegalActionSet DeriveSystem(CampaignObservationV6 observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        IReadOnlyList<CampaignActionCandidate> candidates = observation.DecisionState switch
        {
            CampaignObservationReactingDecisionState
            {
                OwnOpportunities.Count: 0,
                ActiveParticipant: null,
            } reacting =>
                [new CloseReactionWindowNoEligibleAction(reacting.WindowId)],
            CampaignObservationReactingDecisionState reacting =>
                [
                    new CloseReactionWindowUnavailableAction(reacting.WindowId),
                    new CloseReactionWindowTimeoutAction(reacting.WindowId),
                ],
            _ => [],
        };
        return CreateSet(observation, CampaignActionAudience.System, candidates);
    }

    public static CampaignObservationV6ActionIntent? MapSubmission(
        CampaignObservationV6 observation,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(observation);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission)
            || !string.Equals(submission.CampaignId, observation.CampaignId,
                StringComparison.Ordinal)
            || submission.ExpectedStateVersion != observation.StateVersion
            || !string.Equals(submission.ExpectedPositionId, observation.Position.PositionId,
                StringComparison.Ordinal))
        {
            return null;
        }

        var set = submission.Audience == CampaignActionAudience.System
            ? DeriveSystem(observation)
            : submission.Audience == ToAudience(observation.Observer)
                ? DerivePlayer(observation)
                : null;
        var candidate = set?.Candidates.SingleOrDefault(value => string.Equals(
            value.ActionId,
            submission.ActionId,
            StringComparison.Ordinal));
        if (candidate is null)
        {
            return null;
        }

        return candidate switch
        {
            MoveElementAction move => new MoveElementV6Intent(
                submission.ExpectedStateVersion,
                submission.ExpectedPositionId,
                observation.Observer,
                move.ActionId,
                move.ElementId,
                move.OriginLocationId,
                move.DestinationLocationId),
            CompleteMovementSegmentAction => new CompleteMovementSegmentV6Intent(
                submission.ExpectedStateVersion,
                submission.ExpectedPositionId,
                observation.Observer,
                candidate.ActionId),
            MoveReactingElementAction move => new MoveReactingElementIntent(
                submission.ExpectedStateVersion,
                submission.ExpectedPositionId,
                observation.Observer,
                move.ActionId,
                move.WindowId,
                move.OpportunityId,
                move.OriginLocationId,
                move.DestinationLocationId),
            CompleteReactionParticipantAction complete =>
                new CompleteReactionParticipantIntent(
                    submission.ExpectedStateVersion,
                    submission.ExpectedPositionId,
                    observation.Observer,
                    complete.ActionId,
                    complete.WindowId,
                    complete.OpportunityId),
            DeclineReactionWindowAction close => new CloseReactionWindowIntent(
                submission.ExpectedStateVersion,
                submission.ExpectedPositionId,
                observation.Observer,
                close.ActionId,
                close.WindowId,
                CampaignReactionCloseIntentKind.PlayerDecline),
            CloseReactionWindowUnavailableAction close => new CloseReactionWindowIntent(
                submission.ExpectedStateVersion,
                submission.ExpectedPositionId,
                null,
                close.ActionId,
                close.WindowId,
                CampaignReactionCloseIntentKind.ScriptedUnavailable),
            CloseReactionWindowTimeoutAction close => new CloseReactionWindowIntent(
                submission.ExpectedStateVersion,
                submission.ExpectedPositionId,
                null,
                close.ActionId,
                close.WindowId,
                CampaignReactionCloseIntentKind.Timeout),
            CloseReactionWindowNoEligibleAction close => new CloseReactionWindowIntent(
                submission.ExpectedStateVersion,
                submission.ExpectedPositionId,
                null,
                close.ActionId,
                close.WindowId,
                CampaignReactionCloseIntentKind.NoEligibleReactor),
            _ => null,
        };
    }

    private static CampaignLegalActionSet CreateSet(
        CampaignObservationV6 observation,
        CampaignActionAudience audience,
        IReadOnlyList<CampaignActionCandidate> candidates) => new(
            observation.CampaignId,
            observation.StateVersion,
            observation.RulesetHash,
            observation.Position.PositionId,
            audience,
            candidates);

    private static CampaignActionCandidate[] DeriveOrdinaryMovement(
        CampaignObservationV6 observation)
    {
        if (!IsSupportedMovementPosition(observation))
        {
            return [];
        }

        var candidates = observation.OwnElements
            .Where(element => !observation.MovementEndedElementIds.Contains(
                element.ElementId,
                StringComparer.Ordinal))
            .SelectMany(element => DeriveMoves(
                observation.Position,
                observation.Locations,
                observation.Edges,
                observation.ApparentOpposingPresences,
                element,
                destinationId => !(IsControlled(observation, element.CurrentLocationId)
                    && IsControlled(observation, destinationId)),
                observation.OwnElements,
                ownStacking: null,
                (destinationId, cost) => new MoveElementAction(
                    element.ElementId,
                    element.CurrentLocationId,
                    destinationId,
                    cost)))
            .Cast<CampaignActionCandidate>()
            .Append(new CompleteMovementSegmentAction())
            .ToArray();
        return candidates;
    }

    private static CampaignActionCandidate[] DeriveReaction(
        CampaignObservationReactingDecisionState reacting)
    {
        var opportunities = reacting.ActiveParticipant is null
            ? reacting.OwnOpportunities
            : reacting.OwnOpportunities.Where(value => string.Equals(
                value.OpportunityId,
                reacting.ActiveParticipant.OpportunityId,
                StringComparison.Ordinal)).ToArray();
        var candidates = new List<CampaignActionCandidate>();
        foreach (var opportunity in opportunities)
        {
            foreach (var option in opportunity.MoveOptions)
            {
                candidates.Add(new MoveReactingElementAction(
                    reacting.WindowId,
                    opportunity.OpportunityId,
                    option.OriginLocationId,
                    option.DestinationLocationId,
                    option.CostBreakdown));
            }
        }

        if (reacting.ActiveParticipant is null)
        {
            if (reacting.OwnOpportunities.Count > 0)
            {
                candidates.Add(new DeclineReactionWindowAction(reacting.WindowId));
            }
        }
        else
        {
            candidates.Add(new CompleteReactionParticipantAction(
                reacting.WindowId,
                reacting.ActiveParticipant.OpportunityId));
        }

        return candidates.ToArray();
    }

    internal static IReadOnlyList<ObservedReactionMoveOption> DeriveReactionMoveOptions(
        CampaignObservationPosition position,
        IReadOnlyList<CampaignObservationLocation> locations,
        IReadOnlyList<CampaignObservationEdge> edges,
        IReadOnlyList<ObservedApparentPresence> apparentOpposingPresences,
        IReadOnlyList<string> apparentEnemyControlledLocationIds,
        ICampaignObservedMovementSubject element,
        IReadOnlyDictionary<string, int> ownStacking)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(locations);
        ArgumentNullException.ThrowIfNull(edges);
        ArgumentNullException.ThrowIfNull(apparentOpposingPresences);
        ArgumentNullException.ThrowIfNull(apparentEnemyControlledLocationIds);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(ownStacking);
        var controlled = apparentEnemyControlledLocationIds.ToHashSet(StringComparer.Ordinal);
        if (controlled.Contains(element.CurrentLocationId))
        {
            return [];
        }

        return DeriveMoves(
                position,
                locations,
                edges,
                apparentOpposingPresences,
                element,
                destinationId => !controlled.Contains(destinationId),
                ownElements: null,
                ownStacking,
                (destinationId, cost) => new ObservedReactionMoveOption(
                    element.CurrentLocationId,
                    destinationId,
                    cost))
            .ToArray();
    }

    private static IEnumerable<TCandidate> DeriveMoves<TCandidate>(
        CampaignObservationPosition position,
        IReadOnlyList<CampaignObservationLocation> locations,
        IReadOnlyList<CampaignObservationEdge> edges,
        IReadOnlyList<ObservedApparentPresence> apparentOpposingPresences,
        ICampaignObservedMovementSubject element,
        Func<string, bool> permitsDestination,
        IReadOnlyList<ObservedOwnElement>? ownElements,
        IReadOnlyDictionary<string, int>? ownStacking,
        Func<string, MovementActionCostBreakdown, TCandidate> create)
    {
        if (element.ReserveStatus != CampaignObservationReserveStatus.None
            || element.CohesionLevel <= -26
            || element.LedgerGameTurn != position.GameTurn
            || element.LedgerOperationStage != position.OperationStage)
        {
            yield break;
        }

        foreach (var edge in edges.Where(value =>
            value.FirstLocationId == element.CurrentLocationId
            || value.SecondLocationId == element.CurrentLocationId))
        {
            var destinationId = edge.FirstLocationId == element.CurrentLocationId
                ? edge.SecondLocationId
                : edge.FirstLocationId;
            if (!permitsDestination(destinationId)
                || apparentOpposingPresences.Any(presence =>
                    presence.CurrentLocationId == element.CurrentLocationId
                    || presence.CurrentLocationId == destinationId))
            {
                continue;
            }

            var cost = ownStacking is null
                ? CampaignMovementActionCostDerivation.TryCalculate(
                    locations,
                    ownElements ?? throw new ArgumentNullException(nameof(ownElements)),
                    element,
                    edge,
                    destinationId)
                : CampaignMovementActionCostDerivation.TryCalculate(
                    locations,
                    ownStacking.GetValueOrDefault(destinationId),
                    element,
                    edge,
                    destinationId);
            if (cost is not null)
            {
                yield return create(destinationId, cost);
            }
        }
    }

    private static bool IsSupportedMovementPosition(CampaignObservationV6 observation) =>
        observation.Position.OperationStage == 1
        && observation.Position.StageId == LandStageIds.Operation
        && observation.Position.PhaseId == LandPhaseIds.MovementAndCombat
        && observation.Position.SegmentId == LandSegmentIds.Movement
        && observation.Position.ActorRole == LandActorRole.FirstActingSide
        && observation.Position.ActiveSide == observation.Observer;

    private static bool IsControlled(CampaignObservationV6 observation, string locationId) =>
        observation.ApparentEnemyControlledLocationIds.Contains(
            locationId,
            StringComparer.Ordinal);

    private static CampaignActionAudience ToAudience(LandSide side) => side switch
    {
        LandSide.Axis => CampaignActionAudience.Axis,
        LandSide.Commonwealth => CampaignActionAudience.Commonwealth,
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };
}

internal abstract record CampaignObservationV6ActionIntent(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide? ActingSide,
    string ActionId);

internal sealed record MoveElementV6Intent(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide Side,
    string ActionId,
    string ElementId,
    string OriginLocationId,
    string DestinationLocationId)
    : CampaignObservationV6ActionIntent(
        ExpectedStateVersion,
        ExpectedPositionId,
        Side,
        ActionId);

internal sealed record CompleteMovementSegmentV6Intent(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide Side,
    string ActionId)
    : CampaignObservationV6ActionIntent(
        ExpectedStateVersion,
        ExpectedPositionId,
        Side,
        ActionId);

internal sealed record MoveReactingElementIntent(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide Side,
    string ActionId,
    string WindowId,
    string OpportunityId,
    string OriginLocationId,
    string DestinationLocationId)
    : CampaignObservationV6ActionIntent(
        ExpectedStateVersion,
        ExpectedPositionId,
        Side,
        ActionId);

internal sealed record CompleteReactionParticipantIntent(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide Side,
    string ActionId,
    string WindowId,
    string OpportunityId)
    : CampaignObservationV6ActionIntent(
        ExpectedStateVersion,
        ExpectedPositionId,
        Side,
        ActionId);

internal enum CampaignReactionCloseIntentKind
{
    PlayerDecline = 1,
    ScriptedUnavailable = 2,
    Timeout = 3,
    NoEligibleReactor = 4,
}

internal sealed record CloseReactionWindowIntent(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide? Side,
    string ActionId,
    string WindowId,
    CampaignReactionCloseIntentKind CloseKind)
    : CampaignObservationV6ActionIntent(
        ExpectedStateVersion,
        ExpectedPositionId,
        Side,
        ActionId);
