using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignV11BreakdownProjector
{
    public static CampaignSnapshotV11 Apply(CampaignSnapshotV11 prior, CampaignSuccessorEvent value,
        ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        try
        {
            if (value is ElementMovedV3 move) return CampaignV11MoveProjector.ApplyMovement(prior, move, artifact, scenario);
            if (value is ReactingElementMovedV2 reactingMove) return CampaignV11MoveProjector.ApplyReactionMove(prior, reactingMove, artifact, scenario);
            if (!CampaignSnapshotV11Validator.IsValid(prior, artifact, scenario))
                throw new InvalidCampaignHistoryException("Invalid prior Breakdown authority.");
            CampaignSuccessorEvent expected = value switch
            {
                BreakdownStopResolved resolved => CampaignBreakdownStopResolvedFactory.Create(prior, artifact, scenario, resolved.ActionId),
                ElementMovementStopped stopped => CampaignBreakdownLifecycleFactory.CreateStop(prior, artifact, scenario,
                    stopped.ActingSide, stopped.SubmittedRouteId, stopped.ActionId),
                ReactionParticipantCompletedV2 completed => CampaignBreakdownLifecycleFactory.CreateReactionCompletion(prior, artifact, scenario, completed.ToReplayInput()),
                ReactionWindowClosedV2 closed => CampaignBreakdownLifecycleFactory.CreateReactionClose(prior, artifact, scenario, closed.ToReplayInput()),
                MovementSegmentCompletedV2 => CampaignBreakdownLifecycleFactory.CreateMovementCompletion(prior, artifact, scenario),
                BreakdownSegmentCompleted completed => CampaignBreakdownLifecycleFactory.CreateBreakdownCompletion(prior, artifact, scenario, completed.ActionId),
                _ => throw new InvalidCampaignHistoryException("Unsupported Breakdown event."),
            };
            if (!CampaignBreakdownEventSerializer.Serialize(expected).SequenceEqual(CampaignBreakdownEventSerializer.Serialize(value)))
                throw new InvalidCampaignHistoryException("Breakdown event differs from reconstructed authority.");
            return Project(prior, value, artifact, scenario);
        }
        catch (InvalidCampaignHistoryException) { throw; }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or ArithmeticException or JsonException or KeyNotFoundException)
        { throw new InvalidCampaignHistoryException(error.Message); }
    }

    private static CampaignSnapshotV11 Project(CampaignSnapshotV11 prior, CampaignSuccessorEvent value,
        ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        var world = prior.World;
        var random = prior.RandomState;
        var sequence = prior.CurrentPosition.SequenceContext;
        CampaignReactionWindow? window = prior.ReactionWindow;
        CampaignBreakdownFlow flow;
        switch (value)
        {
            case BreakdownStopResolved resolved:
                var checks = resolved.Checks.ToDictionary(x => x.Input.CohortId, StringComparer.Ordinal);
                world = new CampaignWorldSnapshotV6(6, world.Elements.Select(element =>
                {
                    var operational = element.OperationalState;
                    if (operational.VehicleBreakdownState is not { } ledger || !checks.TryGetValue(ledger.CohortId, out var check)) return element;
                    var changed = new CampaignVehicleBreakdownState(ledger.CohortId, ledger.CumulativeBreakdownPoints,
                        ledger.SandstormAttributedBreakdownPoints, check.HighestEffectiveCheckedBandIdAfter, check.WorkingAfter, check.BrokenAfter);
                    return new CampaignElementStateV5(element.ElementId, element.CurrentLocationId, element.ReserveStatus,
                        new CampaignElementOperationalStateV5(operational.LedgerGameTurn, operational.LedgerOperationStage,
                            operational.CapabilityPointsExpended, operational.CohesionLevel, changed, operational.MovementEnded), element.Components);
                }), world.Representations, world.BrokenVehicleLots.Concat(resolved.CreatedLots));
                random = resolved.RandomStateAfter; flow = resolved.BreakdownFlowAfter;
                break;
            case ElementMovementStopped stopped: flow = stopped.BreakdownFlowAfter; break;
            case ReactionParticipantCompletedV2 completed:
                flow = completed.BreakdownFlowAfter; window = completed.ReactionWindowAfter; break;
            case ReactionWindowClosedV2 closed:
                flow = closed.BreakdownFlowAfter; window = null; sequence = closed.SuspendedSequencePosition; break;
            case MovementSegmentCompletedV2 completed:
                flow = completed.BreakdownFlowAfter; sequence = completed.SequencePosition; window = null; break;
            case BreakdownSegmentCompleted completed:
                flow = completed.BreakdownFlowAfter; sequence = completed.SequencePosition; window = null; break;
            default: throw new InvalidCampaignHistoryException("Unsupported Breakdown event.");
        }
        var position = flow switch
        {
            CampaignBreakdownFlow.Idle or CampaignBreakdownFlow.Moving => CampaignPositionV11.FromSequence(sequence),
            CampaignBreakdownFlow.Reacting => CampaignPositionV11.FromReaction(window!.ReactingPosition),
            CampaignBreakdownFlow.ReactorStopOpen or CampaignBreakdownFlow.ReactorStopClosed or CampaignBreakdownFlow.PhasingStop
                => CampaignPositionV11.FromBreakdownStop(sequence),
            _ => throw new InvalidCampaignHistoryException("Invalid post-resolution flow."),
        };
        var after = new CampaignSnapshotV11(11, prior.CampaignId, value.StateVersion, prior.RulesetHash, prior.Setup,
            world, prior.InitiativeHolder, prior.OperationStageOrders, prior.OperationStageWeather, random, position, window, flow);
        if (!CampaignSnapshotV11Validator.IsValid(after, artifact, scenario))
            throw new InvalidCampaignHistoryException("Breakdown event produces uncertified authority.");
        return after;
    }
}
