using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal static class CampaignV11MoveProjector
{
    public static CampaignSnapshotV11 ApplyMovement(CampaignSnapshotV11 prior, ElementMovedV3 moved,
        ContentPackV6Artifact artifact, ContentScenario scenario) => Reconstruct(() =>
    {
        var expected = CampaignElementMovedV3Factory.Create(prior, artifact, scenario, moved.ToReplayInput());
        RequireExact(moved, expected);
        return Project(prior, moved.StateVersion, moved.ElementId, moved.RepresentationId,
            moved.DestinationLocationId, moved.CapabilityPointsExpendedAfter, moved.BreakdownAccounting,
            moved.MovementEndedAfter, moved.OpenedReactionWindow, moved.BreakdownFlowAfter, artifact, scenario);
    });

    public static CampaignSnapshotV11 ApplyReactionMove(CampaignSnapshotV11 prior, ReactingElementMovedV2 moved,
        ContentPackV6Artifact artifact, ContentScenario scenario) => Reconstruct(() =>
    {
        var expected = CampaignReactingElementMovedV2Factory.Create(prior, artifact, scenario, moved.ToReplayInput());
        RequireExact(moved, expected);
        var element = prior.World.Elements.Single(x => x.ElementId == moved.ElementId);
        return Project(prior, moved.StateVersion, moved.ElementId, moved.RepresentationId,
            moved.DestinationLocationId, moved.CapabilityPointsExpendedAfter, moved.BreakdownAccounting,
            element.OperationalState.MovementEnded, moved.ReactionWindowAfter, moved.BreakdownFlowAfter, artifact, scenario);
    });

    private static CampaignSnapshotV11 Project(CampaignSnapshotV11 prior, long version, string elementId,
        string representationId, string destination, Rules.CapabilityPointAmount spent,
        IReadOnlyList<CampaignBreakdownStep> accounting, CampaignMovementEndedState? ended,
        CampaignReactionWindow? window, CampaignBreakdownFlow flow, ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        var element = prior.World.Elements.Single(x => x.ElementId == elementId);
        var representation = prior.World.Representations.Single(x => x.RepresentationId == representationId);
        var movedRepresentation = new CampaignMapRepresentationState(representationId, destination,
            representation.BindingKind, representation.BoundElementIds);
        var world = CampaignElementMovedV3Factory.ProjectMoveForAuthority(prior.World, element,
            movedRepresentation, spent, accounting, ended);
        var sequence = prior.CurrentPosition.SequenceContext;
        var position = flow switch
        {
            CampaignBreakdownFlow.Reacting => CampaignPositionV11.FromReaction(window!.ReactingPosition),
            CampaignBreakdownFlow.PhasingStop => CampaignPositionV11.FromBreakdownStop(sequence),
            CampaignBreakdownFlow.Moving => CampaignPositionV11.FromSequence(sequence),
            _ => throw new InvalidCampaignHistoryException("A move produced an invalid flow variant."),
        };
        var after = new CampaignSnapshotV11(11, prior.CampaignId, version, prior.RulesetHash, prior.Setup,
            world, prior.InitiativeHolder, prior.OperationStageOrders, prior.OperationStageWeather, prior.RandomState,
            position, window, flow);
        if (!CampaignSnapshotV11Validator.IsValid(after, artifact, scenario))
            throw new InvalidCampaignHistoryException("The move produces uncertified Snapshot 11 state.");
        return after;
    }

    private static void RequireExact(CampaignSuccessorEvent actual, CampaignSuccessorEvent expected)
    {
        if (!CampaignBreakdownMoveEventSerializer.Serialize(actual).SequenceEqual(CampaignBreakdownMoveEventSerializer.Serialize(expected)))
            throw new InvalidCampaignHistoryException("Move evidence differs from rederived authority.");
    }
    private static CampaignSnapshotV11 Reconstruct(Func<CampaignSnapshotV11> apply)
    {
        try { return apply(); }
        catch (InvalidCampaignHistoryException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException or InvalidOperationException or JsonException)
        { throw new InvalidCampaignHistoryException(exception.Message); }
    }
}
