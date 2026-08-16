using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class InitiativeEventFactory
{
    public static InitiativeDetermined Create(CampaignSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var resolution = InitiativeResolver.Resolve(
            snapshot.GameTurn,
            snapshot.Setup.InitialInitiative,
            snapshot.RandomState,
            snapshot.Setup.Sources);
        var nextPosition = Cna1979LandSequence.GetNext(snapshot.SequencePosition);

        if (nextPosition.StageId != LandStageIds.NavalConvoy
            || nextPosition.PhaseId != LandPhaseIds.NavalConvoySchedule)
        {
            throw new InvalidOperationException(
                "Initiative Determination must advance exactly to Naval Convoy.");
        }

        return new InitiativeDetermined(
            snapshot.CampaignId,
            checked(snapshot.StateVersion + 1),
            snapshot.SequencePosition.PositionId,
            resolution.Outcome,
            snapshot.RandomState.AlgorithmId,
            snapshot.RandomState.NextByteCursor,
            resolution.RandomState.NextByteCursor,
            nextPosition,
            resolution.Sources);
    }
}
