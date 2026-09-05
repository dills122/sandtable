using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignV10Projector
{
    public static CampaignSnapshotV10 ApplyReactionMove(
        CampaignSnapshotV10 prior,
        ReactingElementMoved moved,
        ContentPackV5Artifact artifact,
        ContentScenario scenario) => ApplyReactionMove(
        prior,
        moved,
        artifact,
        scenario,
        CampaignReactionParticipantEventFactory.CreateMove);

    public static CampaignSnapshotV10 ApplyReactionCompletion(
        CampaignSnapshotV10 prior,
        ReactionParticipantCompleted completed,
        ContentPackV5Artifact artifact,
        ContentScenario scenario) => ApplyReactionCompletion(
        prior,
        completed,
        artifact,
        scenario,
        CampaignReactionParticipantEventFactory.CreateCompletion);

    public static CampaignSnapshotV10 ApplyReactionClose(
        CampaignSnapshotV10 prior,
        ReactionWindowClosed closed,
        ContentPackV5Artifact artifact,
        ContentScenario scenario) => ApplyReactionClose(
            prior,
            closed,
            artifact,
            scenario,
            CampaignReactionWindowClosedFactory.Create);

    public static CampaignSnapshotV10 ApplyMovement(
        CampaignSnapshotV10 prior,
        ElementMovedV2 moved,
        ContentPackV5Artifact artifact,
        ContentScenario scenario) => ApplyMovement(
            prior,
            moved,
            artifact,
            scenario,
            (snapshot, input) => CampaignElementMovedV2Factory.Create(
                snapshot,
                artifact,
                scenario,
                input));

    public static CampaignSnapshotV10 ApplyCreation(
        CampaignCreatedV9 created,
        ContentPackV5Artifact artifact,
        ContentScenario scenario)
    {
        ArgumentNullException.ThrowIfNull(created);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        var expectedPosition = Cna1979LandSequence.CreateTurn(scenario.Start.GameTurn)[0];
        if (created.ContractVersion != CampaignCreatedV9.CurrentContractVersion
            || created.StateVersion != 1
            || !Cna1979Ruleset.IsCanonicalHash(created.RulesetHash)
            || created.Setup.Content.Pack != artifact.Identity
            || !string.Equals(created.Setup.Content.ScenarioId, scenario.ScenarioId,
                StringComparison.Ordinal)
            || created.Setup.InitialGameTurn != scenario.Start.GameTurn
            || created.Setup.StageEntry.OperationStage != scenario.Start.OperationStage
            || created.RandomState.ContractVersion != SandtableRandom.ContractVersion
            || !string.Equals(created.RandomState.AlgorithmId,
                SandtableRandom.AlgorithmId, StringComparison.Ordinal)
            || created.RandomState.NextByteCursor != 0
            || created.SequencePosition != expectedPosition
            || !CampaignWorldV5Validator.IsValidInitial(
                created.InitialWorld,
                artifact,
                scenario))
        {
            throw new InvalidCampaignHistoryException(
                "The CampaignCreated v9 event is inconsistent with admitted successor truth.");
        }

        var snapshot = new CampaignSnapshotV10(
            CampaignSnapshotV10.CurrentContractVersion,
            created.CampaignId,
            created.StateVersion,
            created.RulesetHash,
            created.Setup,
            created.InitialWorld,
            null,
            [],
            [],
            created.RandomState,
            CampaignPositionV10.FromSequence(created.SequencePosition),
            null);
        if (!CampaignSnapshotV10Validator.IsValid(snapshot, artifact, scenario))
        {
            throw new InvalidCampaignHistoryException(
                "The CampaignCreated v9 event produces invalid Snapshot 10 state.");
        }

        return snapshot;
    }

    public static CampaignSnapshotV10 ApplyMovement(
        CampaignSnapshotV10 prior,
        ElementMovedV2 moved,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        CampaignElementMovedV2Reconstructor reconstruct)
    {
        ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(moved);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(reconstruct);
        if (!CampaignSnapshotV10Validator.IsValid(prior, artifact, scenario)
            || prior.ReactionWindow is not null
            || prior.CurrentPosition.Kind != CampaignPositionV10Kind.Sequence
            || prior.StateVersion != moved.PriorStateVersion
            || !string.Equals(prior.CampaignId, moved.CampaignId, StringComparison.Ordinal)
            || !string.Equals(prior.CurrentPosition.SequencePosition!.PositionId,
                moved.FromPositionId, StringComparison.Ordinal))
        {
            throw new InvalidCampaignHistoryException(
                "The prior Snapshot 10 cannot admit the ElementMoved v2 event.");
        }

        ElementMovedV2 expected;
        try
        {
            expected = reconstruct(prior, moved.ToReplayInput());
            if (!CampaignSuccessorEventSerializer.Serialize(moved).SequenceEqual(
                    CampaignSuccessorEventSerializer.Serialize(expected)))
            {
                throw new InvalidCampaignHistoryException(
                    "The ElementMoved v2 event is inconsistent with historical authority.");
            }
        }
        catch (InvalidCampaignHistoryException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidOperationException
            or JsonException)
        {
            throw new InvalidCampaignHistoryException(exception.Message);
        }

        if (!HasValidMovementBinding(prior, moved, artifact)
            || !HasValidOpenedWindow(moved, prior.RulesetHash))
        {
            throw new InvalidCampaignHistoryException(
                "The ElementMoved v2 authority binding is invalid.");
        }

        var world = new CampaignWorldSnapshotV5(
            CampaignWorldSnapshotV5.CurrentContractVersion,
            prior.World.Elements.Select(element => string.Equals(
                    element.ElementId,
                    moved.ElementId,
                    StringComparison.Ordinal)
                ? new CampaignElementStateV5(
                    element.ElementId,
                    moved.DestinationLocationId,
                    element.ReserveStatus,
                    new CampaignElementOperationalStateV5(
                        element.OperationalState.LedgerGameTurn,
                        element.OperationalState.LedgerOperationStage,
                        moved.CapabilityPointsExpendedAfter,
                        moved.CohesionAfter,
                        element.OperationalState.VehicleBreakdownState,
                        moved.MovementEndedAfter),
                    element.Components)
                : element).ToArray(),
            prior.World.Representations.Select(representation => string.Equals(
                    representation.RepresentationId,
                    moved.RepresentationId,
                    StringComparison.Ordinal)
                ? new CampaignMapRepresentationState(
                    representation.RepresentationId,
                    moved.DestinationLocationId,
                    representation.BindingKind,
                    representation.BoundElementIds)
                : representation).ToArray());
        var currentPosition = moved.OpenedReactionWindow is null
            ? CampaignPositionV10.FromSequence(moved.SequencePosition)
            : CampaignPositionV10.FromReaction(
                moved.OpenedReactionWindow.ReactingPosition);
        var projected = new CampaignSnapshotV10(
            CampaignSnapshotV10.CurrentContractVersion,
            prior.CampaignId,
            moved.StateVersion,
            prior.RulesetHash,
            prior.Setup,
            world,
            prior.InitiativeHolder,
            prior.OperationStageOrders,
            prior.OperationStageWeather,
            prior.RandomState,
            currentPosition,
            moved.OpenedReactionWindow);
        if (!CampaignSnapshotV10Validator.IsValid(projected, artifact, scenario))
        {
            throw new InvalidCampaignHistoryException(
                "The ElementMoved v2 event produces invalid Snapshot 10 state.");
        }

        return projected;
    }

    public static CampaignSnapshotV10 ReplayMovementCheckpoint(
        CampaignSnapshotV10 checkpoint,
        IEnumerable<ElementMovedV2> events,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        CampaignElementMovedV2Reconstructor reconstruct)
    {
        ArgumentNullException.ThrowIfNull(events);
        var snapshot = checkpoint;
        foreach (var campaignEvent in events)
        {
            snapshot = ApplyMovement(
                snapshot,
                campaignEvent,
                artifact,
                scenario,
                reconstruct);
        }

        return snapshot;
    }

    public static CampaignSnapshotV10 ReplayMovementCheckpoint(
        CampaignSnapshotV10 checkpoint,
        IEnumerable<ElementMovedV2> events,
        ContentPackV5Artifact artifact,
        ContentScenario scenario) => ReplayMovementCheckpoint(
            checkpoint,
            events,
            artifact,
            scenario,
            (snapshot, input) => CampaignElementMovedV2Factory.Create(
                snapshot,
                artifact,
                scenario,
                input));

    public static CampaignSnapshotV10 ApplyReactionClose(
        CampaignSnapshotV10 prior,
        ReactionWindowClosed closed,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        CampaignReactionWindowClosedReconstructor reconstruct)
    {
        ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(closed);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(reconstruct);
        var window = prior.ReactionWindow;
        if (!CampaignSnapshotV10Validator.IsValid(prior, artifact, scenario)
            || window is null
            || prior.CurrentPosition.Kind != CampaignPositionV10Kind.Reaction
            || prior.StateVersion != closed.PriorStateVersion
            || !string.Equals(prior.CampaignId, closed.CampaignId, StringComparison.Ordinal)
            || window.WindowId != closed.WindowId
            || !string.Equals(
                window.ReactingPosition.SuspendedMovementPosition.PositionId,
                closed.FromPositionId,
                StringComparison.Ordinal))
        {
            throw new InvalidCampaignHistoryException(
                "The prior Snapshot 10 cannot admit the ReactionWindowClosed event.");
        }

        try
        {
            var expected = reconstruct(
                prior,
                artifact,
                scenario,
                closed.ToReplayInput());
            if (!CampaignSuccessorEventSerializer.Serialize(closed).SequenceEqual(
                    CampaignSuccessorEventSerializer.Serialize(expected)))
            {
                throw new InvalidCampaignHistoryException(
                    "The ReactionWindowClosed event is inconsistent with historical authority.");
            }
        }
        catch (InvalidCampaignHistoryException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidOperationException
            or JsonException)
        {
            throw new InvalidCampaignHistoryException(exception.Message);
        }

        var projected = new CampaignSnapshotV10(
            CampaignSnapshotV10.CurrentContractVersion,
            prior.CampaignId,
            closed.StateVersion,
            prior.RulesetHash,
            prior.Setup,
            prior.World,
            prior.InitiativeHolder,
            prior.OperationStageOrders,
            prior.OperationStageWeather,
            prior.RandomState,
            CampaignPositionV10.FromSequence(closed.ResumedSequencePosition),
            null);
        if (!CampaignSnapshotV10Validator.IsValid(projected, artifact, scenario))
        {
            throw new InvalidCampaignHistoryException(
                "The ReactionWindowClosed event produces invalid Snapshot 10 state.");
        }

        return projected;
    }

    public static CampaignSnapshotV10 ApplyReactionMove(
        CampaignSnapshotV10 prior,
        ReactingElementMoved moved,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        CampaignReactingElementMovedReconstructor reconstruct)
    {
        ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(moved);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(reconstruct);
        ValidateReactionEventPrior(
            prior,
            artifact,
            scenario,
            moved.CampaignId,
            moved.PriorStateVersion,
            moved.FromPositionId,
            moved.WindowId,
            "ReactingElementMoved");

        try
        {
            var expected = reconstruct(prior, artifact, scenario, moved.ToReplayInput());
            if (!CampaignSuccessorEventSerializer.Serialize(moved).SequenceEqual(
                    CampaignSuccessorEventSerializer.Serialize(expected)))
            {
                throw new InvalidCampaignHistoryException(
                    "The ReactingElementMoved event is inconsistent with historical authority.");
            }
        }
        catch (InvalidCampaignHistoryException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidOperationException
            or JsonException)
        {
            throw new InvalidCampaignHistoryException(exception.Message);
        }

        var element = prior.World.Elements.Single(value => string.Equals(
            value.ElementId,
            moved.ElementId,
            StringComparison.Ordinal));
        var representation = prior.World.Representations.Single(value => string.Equals(
            value.RepresentationId,
            moved.RepresentationId,
            StringComparison.Ordinal));
        var representationAfter = new CampaignMapRepresentationState(
            representation.RepresentationId,
            moved.DestinationLocationId,
            representation.BindingKind,
            representation.BoundElementIds);
        var world = CampaignElementMovedV2Factory.ProjectMoveForAuthority(
            prior.World,
            element,
            representationAfter,
            moved.CapabilityPointsExpendedAfter);
        return CreateReactionSnapshot(
            prior,
            moved.StateVersion,
            world,
            moved.ReactionWindowAfter,
            artifact,
            scenario,
            "The ReactingElementMoved event produces invalid Snapshot 10 state.");
    }

    public static CampaignSnapshotV10 ApplyReactionCompletion(
        CampaignSnapshotV10 prior,
        ReactionParticipantCompleted completed,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        CampaignReactionParticipantCompletedReconstructor reconstruct)
    {
        ArgumentNullException.ThrowIfNull(prior);
        ArgumentNullException.ThrowIfNull(completed);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(reconstruct);
        ValidateReactionEventPrior(
            prior,
            artifact,
            scenario,
            completed.CampaignId,
            completed.PriorStateVersion,
            completed.FromPositionId,
            completed.WindowId,
            "ReactionParticipantCompleted");

        try
        {
            var expected = reconstruct(prior, artifact, scenario, completed.ToReplayInput());
            if (!CampaignSuccessorEventSerializer.Serialize(completed).SequenceEqual(
                    CampaignSuccessorEventSerializer.Serialize(expected)))
            {
                throw new InvalidCampaignHistoryException(
                    "The ReactionParticipantCompleted event is inconsistent with historical authority.");
            }
        }
        catch (InvalidCampaignHistoryException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidOperationException
            or JsonException)
        {
            throw new InvalidCampaignHistoryException(exception.Message);
        }

        return CreateReactionSnapshot(
            prior,
            completed.StateVersion,
            prior.World,
            completed.ReactionWindowAfter,
            artifact,
            scenario,
            "The ReactionParticipantCompleted event produces invalid Snapshot 10 state.");
    }

    private static void ValidateReactionEventPrior(
        CampaignSnapshotV10 prior,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        string campaignId,
        long priorStateVersion,
        string fromPositionId,
        CampaignReactionWindowId windowId,
        string eventName)
    {
        var window = prior.ReactionWindow;
        if (!CampaignSnapshotV10Validator.IsValid(prior, artifact, scenario)
            || window is null
            || prior.CurrentPosition.Kind != CampaignPositionV10Kind.Reaction
            || prior.StateVersion != priorStateVersion
            || !string.Equals(prior.CampaignId, campaignId, StringComparison.Ordinal)
            || window.WindowId != windowId
            || !string.Equals(
                window.ReactingPosition.SuspendedMovementPosition.PositionId,
                fromPositionId,
                StringComparison.Ordinal))
        {
            throw new InvalidCampaignHistoryException(
                $"The prior Snapshot 10 cannot admit the {eventName} event.");
        }
    }

    private static CampaignSnapshotV10 CreateReactionSnapshot(
        CampaignSnapshotV10 prior,
        long stateVersion,
        CampaignWorldSnapshotV5 world,
        CampaignReactionWindow window,
        ContentPackV5Artifact artifact,
        ContentScenario scenario,
        string invalidMessage)
    {
        var projected = new CampaignSnapshotV10(
            CampaignSnapshotV10.CurrentContractVersion,
            prior.CampaignId,
            stateVersion,
            prior.RulesetHash,
            prior.Setup,
            world,
            prior.InitiativeHolder,
            prior.OperationStageOrders,
            prior.OperationStageWeather,
            prior.RandomState,
            CampaignPositionV10.FromReaction(window.ReactingPosition),
            window);
        if (!CampaignSnapshotV10Validator.IsValid(projected, artifact, scenario))
        {
            throw new InvalidCampaignHistoryException(invalidMessage);
        }

        return projected;
    }

    private static bool HasValidMovementBinding(
        CampaignSnapshotV10 prior,
        ElementMovedV2 moved,
        ContentPackV5Artifact artifact)
    {
        var element = prior.World.Elements.SingleOrDefault(value => string.Equals(
            value.ElementId,
            moved.ElementId,
            StringComparison.Ordinal));
        var representation = prior.World.Representations.SingleOrDefault(value => string.Equals(
            value.RepresentationId,
            moved.RepresentationId,
            StringComparison.Ordinal));
        var content = artifact.Definition.LegacyDefinition.Elements.SingleOrDefault(value =>
            string.Equals(value.ElementId, moved.ElementId, StringComparison.Ordinal));
        var expectedSide = moved.ActingSide switch
        {
            LandSide.Axis => "axis",
            LandSide.Commonwealth => "commonwealth",
            _ => null,
        };
        return element is not null
            && representation is not null
            && content is not null
            && string.Equals(content.SideId, expectedSide, StringComparison.Ordinal)
            && representation.BoundElementIds.Contains(moved.ElementId, StringComparer.Ordinal)
            && string.Equals(element.CurrentLocationId, moved.OriginLocationId,
                StringComparison.Ordinal)
            && string.Equals(representation.CurrentLocationId, moved.OriginLocationId,
                StringComparison.Ordinal)
            && element.OperationalState.CapabilityPointsExpended
                == moved.CapabilityPointsExpendedBefore
            && element.OperationalState.CohesionLevel == moved.CohesionBefore;
    }

    private static bool HasValidOpenedWindow(ElementMovedV2 moved, string rulesetHash)
    {
        if (moved.OpenedReactionWindow is null)
        {
            return true;
        }

        try
        {
            moved.OpenedReactionWindow.ValidateIdentities(moved.CampaignId, rulesetHash);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
