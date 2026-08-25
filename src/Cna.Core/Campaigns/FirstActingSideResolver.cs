using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class FirstActingSideResolver
{
    public static LandSide Resolve(CampaignSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (snapshot.SequencePosition.ActorRole != LandActorRole.FirstActingSide)
        {
            throw new InvalidOperationException(
                "The current position is not assigned to the first-acting side.");
        }

        var currentOrders = snapshot.OperationStageOrders
            .Where(order => order.GameTurn == snapshot.GameTurn
                && order.OperationStage == snapshot.OperationStage)
            .ToArray();

        if (currentOrders.Length != 1)
        {
            throw new InvalidOperationException(
                "The current operation stage must have exactly one retained actor order.");
        }

        return currentOrders[0].FirstSide;
    }
}
