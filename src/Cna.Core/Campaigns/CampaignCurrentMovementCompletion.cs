using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignCurrentMovementCompletion
{
    internal static MovementSegmentCompleted Create(
        CampaignSnapshotV10 prior,
        CampaignContentContext context)
    {
        var position = prior.CurrentPosition.SequencePosition;
        if (context.ArtifactV5 is not { } artifact
            || !CampaignSnapshotV10Validator.IsValid(prior, artifact, context.Scenario)
            || prior.ReactionWindow is not null
            || position is null
            || position.OperationStage != 1
            || position.PhaseId != LandPhaseIds.MovementAndCombat
            || position.SegmentId != LandSegmentIds.Movement
            || position.ActorRole != LandActorRole.FirstActingSide
            || position.ActiveSide is null)
            throw new InvalidCampaignHistoryException("Current Movement completion authority is not admitted.");

        var canonical = Cna1979LandSequence.CreateTurn(position.GameTurn)
            .Single(value => value.PositionId == position.PositionId);
        return new MovementSegmentCompleted(prior.CampaignId, checked(prior.StateVersion + 1),
            prior.StateVersion, position.PositionId, position.GameTurn, position.OperationStage,
            position.ActiveSide.Value, Cna1979LandSequence.GetNext(canonical));
    }

    internal static CampaignSnapshotV10 Apply(
        CampaignSnapshotV10 prior,
        MovementSegmentCompleted completed,
        CampaignContentContext context)
    {
        var expected = Create(prior, context);
        if (!CampaignEventSerializer.Serialize(expected).AsSpan()
            .SequenceEqual(CampaignEventSerializer.Serialize(completed)))
            throw new InvalidCampaignHistoryException("Movement completion differs from current authority.");

        var successor = new CampaignSnapshotV10(CampaignSnapshotV10.CurrentContractVersion,
            prior.CampaignId, completed.StateVersion, prior.RulesetHash, prior.Setup, prior.World,
            prior.InitiativeHolder, prior.OperationStageOrders, prior.OperationStageWeather,
            prior.RandomState, CampaignPositionV10.FromSequence(completed.SequencePosition), null);
        if (!CampaignSnapshotV10Validator.IsValid(successor, context.ArtifactV5!, context.Scenario))
            throw new InvalidCampaignHistoryException("Movement completion produced invalid current authority.");
        return successor;
    }
}
