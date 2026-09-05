using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Actions;

internal static class CampaignObservationV7ActionDerivation
{
    public static CampaignLegalActionSet DerivePlayer(CampaignObservationV7 observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var audience = ToAudience(observation.Observer);
        var candidates = observation.DecisionState switch
        {
            CampaignObservationV7NormalDecisionState => DeriveOrdinaryMovement(observation),
            CampaignObservationV7PhasingWaitingDecisionState or CampaignObservationV7BreakdownWaitingDecisionState => [],
            CampaignObservationV7ReactingDecisionState reacting => DeriveReaction(reacting),
            _ => throw new ArgumentOutOfRangeException(nameof(observation)),
        };
        return CreateSet(observation, audience, candidates);
    }

    public static CampaignLegalActionSet DeriveSystem(CampaignObservationV7 observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        IReadOnlyList<CampaignActionCandidate> candidates = observation.DecisionState switch
        {
            CampaignObservationV7BreakdownWaitingDecisionState =>
                [new ResolveBreakdownStopAction(CreateSystemStopCapability(observation))],
            CampaignObservationV7ReactingDecisionState
            {
                OwnOpportunities.Count: 0,
                ActiveParticipant: null,
            } reacting =>
                [new CloseReactionWindowNoEligibleAction(reacting.WindowId)],
            CampaignObservationV7ReactingDecisionState reacting =>
                [
                    new CloseReactionWindowUnavailableAction(reacting.WindowId),
                    new CloseReactionWindowTimeoutAction(reacting.WindowId),
                ],
            CampaignObservationV7NormalDecisionState when observation.Position is
            {
                OperationStage: 1, SegmentId: LandSegmentIds.BreakdownDetermination,
                ActorRole: LandActorRole.FirstActingSide
            } => [new CompleteBreakdownSegmentAction()],
            CampaignObservationV7NormalDecisionState => DerivePreambleSystem(observation.Position),
            _ => [],
        };
        return CreateSet(observation, CampaignActionAudience.System, candidates);
    }

    public static CampaignObservationV7ActionIntent? MapSubmission(
        CampaignObservationV7 observation,
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
            MoveElementAction move => new MoveElementV7Intent(
                submission.ExpectedStateVersion,
                submission.ExpectedPositionId,
                observation.Observer,
                move.ActionId,
                move.ElementId,
                move.OriginLocationId,
                move.DestinationLocationId),
            StopElementMovementAction stop => new StopElementMovementV7Intent(
                submission.ExpectedStateVersion, submission.ExpectedPositionId, observation.Observer,
                stop.ActionId, stop.RouteId),
            ResolveBreakdownStopAction resolve => new ResolveBreakdownStopV7Intent(
                submission.ExpectedStateVersion, submission.ExpectedPositionId, resolve.ActionId, resolve.StopId),
            CompleteBreakdownSegmentAction complete => new CompleteBreakdownSegmentV7Intent(
                submission.ExpectedStateVersion, submission.ExpectedPositionId, complete.ActionId),
            CompleteMovementSegmentAction => new CompleteMovementSegmentV7Intent(
                submission.ExpectedStateVersion,
                submission.ExpectedPositionId,
                observation.Observer,
                candidate.ActionId),
            MoveReactingElementAction move => new MoveReactingElementV7Intent(
                submission.ExpectedStateVersion,
                submission.ExpectedPositionId,
                observation.Observer,
                move.ActionId,
                move.WindowId,
                move.OpportunityId,
                move.OriginLocationId,
                move.DestinationLocationId),
            CompleteReactionParticipantAction complete =>
                new CompleteReactionParticipantV7Intent(
                    submission.ExpectedStateVersion,
                    submission.ExpectedPositionId,
                    observation.Observer,
                    complete.ActionId,
                    complete.WindowId,
                    complete.OpportunityId),
            DeclineReactionWindowAction close => new CloseReactionWindowV7Intent(
                submission.ExpectedStateVersion,
                submission.ExpectedPositionId,
                observation.Observer,
                close.ActionId,
                close.WindowId,
                CampaignReactionCloseIntentKind.PlayerDecline),
            CloseReactionWindowUnavailableAction close => new CloseReactionWindowV7Intent(
                submission.ExpectedStateVersion,
                submission.ExpectedPositionId,
                null,
                close.ActionId,
                close.WindowId,
                CampaignReactionCloseIntentKind.ScriptedUnavailable),
            CloseReactionWindowTimeoutAction close => new CloseReactionWindowV7Intent(
                submission.ExpectedStateVersion,
                submission.ExpectedPositionId,
                null,
                close.ActionId,
                close.WindowId,
                CampaignReactionCloseIntentKind.Timeout),
            CloseReactionWindowNoEligibleAction close => new CloseReactionWindowV7Intent(
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
        CampaignObservationV7 observation,
        CampaignActionAudience audience,
        IReadOnlyList<CampaignActionCandidate> candidates) => new(
            observation.CampaignId,
            observation.StateVersion,
            observation.RulesetHash,
            observation.Position.PositionId,
            audience,
            candidates);

    private static CampaignActionCandidate[] DeriveOrdinaryMovement(
        CampaignObservationV7 observation)
    {
        if (!IsSupportedMovementPosition(observation))
            return CampaignLegalActions.GenerateForSide(observation, ToAudience(observation.Observer))
                .Candidates.ToArray();

        var route = ((CampaignObservationV7NormalDecisionState)observation.DecisionState).ActiveMovement;
        var candidates = observation.OwnElements
            .Where(element => (route is null || route.ElementId == element.ElementId)
                && element.VehicleBreakdownRisk is not { WorkingPointCount: 0 })
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
                    && IsControlled(observation, destinationId))
                    && PermitsCombatStack(observation.OwnElements, element, destinationId),
                observation.OwnElements,
                ownStacking: null,
                (destinationId, cost) => new MoveElementAction(
                    element.ElementId,
                    element.CurrentLocationId,
                    destinationId,
                    cost)))
            .Cast<CampaignActionCandidate>()
            .Concat(route is null
                ? [new CompleteMovementSegmentAction()]
                : new CampaignActionCandidate[] { new StopElementMovementAction(route.RouteId) })
            .ToArray();
        return candidates;
    }

    private static CampaignActionCandidate[] DeriveReaction(
        CampaignObservationV7ReactingDecisionState reacting)
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

    private static CampaignActionCandidate[] DerivePreambleSystem(CampaignObservationPosition position) => position switch
    {
        {
            OperationStage: 0, StageId: LandStageIds.InitiativeDetermination,
            PhaseId: LandPhaseIds.InitiativeDetermination
        } => [new ResolveInitiativeAction()],
        {
            OperationStage: 0, StageId: LandStageIds.NavalConvoy,
            PhaseId: LandPhaseIds.NavalConvoySchedule
        } => [new ResolveNoObligationNavalConvoyScheduleAction()],
        {
            OperationStage: 0, StageId: LandStageIds.NavalConvoy,
            PhaseId: LandPhaseIds.TacticalShipping
        } => [new ResolveNoObligationTacticalShippingAction()],
        {
            OperationStage: 1, StageId: LandStageIds.Operation,
            PhaseId: LandPhaseIds.WeatherDetermination
        } => [new ResolveWeatherAction()],
        {
            OperationStage: 1, StageId: LandStageIds.Operation,
            PhaseId: LandPhaseIds.Organization, SegmentId: null
        } => [new ResolveNoObligationOrganizationAction()],
        {
            OperationStage: 1, StageId: LandStageIds.Operation,
            PhaseId: LandPhaseIds.NavalConvoyArrival, SegmentId: null
        } => [new ResolveNoObligationNavalConvoyArrivalAction()],
        {
            OperationStage: 1, StageId: LandStageIds.Operation,
            PhaseId: LandPhaseIds.CommonwealthFleet, SegmentId: LandSegmentIds.FleetAssignment
        } => [new ResolveNoObligationFleetAssignmentAction()],
        {
            OperationStage: 1, StageId: LandStageIds.Operation,
            PhaseId: LandPhaseIds.CommonwealthFleet, SegmentId: LandSegmentIds.FleetRepair
        } => [new ResolveNoObligationFleetRepairAction()],
        _ => [],
    };

    private static bool PermitsCombatStack(IReadOnlyList<ObservedOwnElement> own,
        ObservedOwnElement mover, string destination)
    {
        if (mover.VehicleBreakdownRisk is not null) return true;
        var moverSize = Cna1979Movement.LookupStackingValue(mover.OrganizationId);
        if (!moverSize.IsSupported) return false;
        var total = moverSize.Value.StackingValue;
        foreach (var other in own.Where(value => value.ElementId != mover.ElementId
            && value.CurrentLocationId == destination && value.VehicleBreakdownRisk is null))
        {
            var size = Cna1979Movement.LookupStackingValue(other.OrganizationId);
            if (!size.IsSupported) return false;
            total = checked(total + size.Value.StackingValue);
        }
        return total <= 1;
    }

    private static string CreateSystemStopCapability(CampaignObservationV7 observation)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("domain", "sandtable.action.breakdown-stop.v1");
            writer.WriteString("campaignId", observation.CampaignId);
            writer.WriteString("rulesetHash", observation.RulesetHash);
            writer.WriteNumber("stateVersion", observation.StateVersion);
            writer.WriteString("audience", "system");
            writer.WriteEndObject();
        }
        return $"sha256:{Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()))}";
    }

    private static bool IsSupportedMovementPosition(CampaignObservationV7 observation) =>
        observation.Position.OperationStage == 1
        && observation.Position.StageId == LandStageIds.Operation
        && observation.Position.PhaseId == LandPhaseIds.MovementAndCombat
        && observation.Position.SegmentId == LandSegmentIds.Movement
        && observation.Position.ActorRole == LandActorRole.FirstActingSide
        && observation.Position.ActiveSide == observation.Observer;

    private static bool IsControlled(CampaignObservationV7 observation, string locationId) =>
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

internal abstract record CampaignObservationV7ActionIntent(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide? ActingSide,
    string ActionId);

internal sealed record MoveElementV7Intent(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide Side,
    string ActionId,
    string ElementId,
    string OriginLocationId,
    string DestinationLocationId)
    : CampaignObservationV7ActionIntent(
        ExpectedStateVersion,
        ExpectedPositionId,
        Side,
        ActionId);

internal sealed record CompleteMovementSegmentV7Intent(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide Side,
    string ActionId)
    : CampaignObservationV7ActionIntent(
        ExpectedStateVersion,
        ExpectedPositionId,
        Side,
        ActionId);

internal sealed record MoveReactingElementV7Intent(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide Side,
    string ActionId,
    string WindowId,
    string OpportunityId,
    string OriginLocationId,
    string DestinationLocationId)
    : CampaignObservationV7ActionIntent(
        ExpectedStateVersion,
        ExpectedPositionId,
        Side,
        ActionId);

internal sealed record CompleteReactionParticipantV7Intent(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide Side,
    string ActionId,
    string WindowId,
    string OpportunityId)
    : CampaignObservationV7ActionIntent(
        ExpectedStateVersion,
        ExpectedPositionId,
        Side,
        ActionId);

internal sealed record CloseReactionWindowV7Intent(
    long ExpectedStateVersion,
    string ExpectedPositionId,
    LandSide? Side,
    string ActionId,
    string WindowId,
    CampaignReactionCloseIntentKind CloseKind)
    : CampaignObservationV7ActionIntent(
        ExpectedStateVersion,
        ExpectedPositionId,
        Side,
        ActionId);

internal sealed record StopElementMovementV7Intent(long ExpectedStateVersion, string ExpectedPositionId,
    LandSide Side, string ActionId, string RouteId)
    : CampaignObservationV7ActionIntent(ExpectedStateVersion, ExpectedPositionId, Side, ActionId);

internal sealed record ResolveBreakdownStopV7Intent(long ExpectedStateVersion, string ExpectedPositionId,
    string ActionId, string StopId)
    : CampaignObservationV7ActionIntent(ExpectedStateVersion, ExpectedPositionId, null, ActionId);

internal sealed record CompleteBreakdownSegmentV7Intent(long ExpectedStateVersion, string ExpectedPositionId,
    string ActionId)
    : CampaignObservationV7ActionIntent(ExpectedStateVersion, ExpectedPositionId, null, ActionId);
