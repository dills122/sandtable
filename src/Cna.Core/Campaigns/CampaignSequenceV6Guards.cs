using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignSequenceV6Guards
{
    public static void RequireCurrentPosition(
        LandSequencePosition position,
        IReadOnlyList<CampaignOperationStageOrder> operationStageOrders)
    {
        ArgumentNullException.ThrowIfNull(position);
        ArgumentNullException.ThrowIfNull(operationStageOrders);
        if (!Cna1979LandSequenceV4.IsSupportedCheckpoint(position))
            throw new ArgumentException("The position is outside the supported sequence 4 checkpoint boundary.", nameof(position));
        var catalogPosition = Cna1979LandSequenceV4.CreateTurn(position.GameTurn)
            .Single(candidate => candidate.PositionId == position.PositionId);
        if (catalogPosition == position) return;
        Cna1979LandSequenceV4.RequireMaterializedMovement(position);
        var orders = operationStageOrders.Where(order => order.GameTurn == position.GameTurn
            && order.OperationStage == position.OperationStage).ToArray();
        if (orders.Length != 1 || position.ActiveSide != orders[0].FirstSide)
            throw new ArgumentException("The materialized Movement side must match the retained first-side order.", nameof(position));
    }
}
