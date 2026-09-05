using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class FirstActingSideResolver
{
    public static LandSide Resolve(CampaignSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return Resolve(
            snapshot.SequencePosition,
            snapshot.OperationStageOrders);
    }

    public static LandSide Resolve(CampaignSnapshotV10 snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var sequence = snapshot.CurrentPosition.Kind == CampaignPositionV10Kind.Sequence
            ? snapshot.CurrentPosition.SequencePosition!
            : snapshot.CurrentPosition.ReactingPosition!.SuspendedMovementPosition;
        return Resolve(sequence, snapshot.OperationStageOrders);
    }

    private static LandSide Resolve(
        LandSequencePosition position,
        IReadOnlyList<CampaignOperationStageOrder> operationStageOrders)
    {
        if (position.ActorRole != LandActorRole.FirstActingSide)
        {
            throw new InvalidOperationException(
                "The current position is not assigned to the first-acting side.");
        }

        var currentOrders = operationStageOrders
            .Where(order => order.GameTurn == position.GameTurn
                && order.OperationStage == position.OperationStage)
            .ToArray();

        if (currentOrders.Length != 1)
        {
            throw new InvalidOperationException(
                "The current operation stage must have exactly one retained actor order.");
        }

        return currentOrders[0].FirstSide;
    }
}
