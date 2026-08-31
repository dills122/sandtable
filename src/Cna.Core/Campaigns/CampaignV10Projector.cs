using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignV10Projector
{
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
