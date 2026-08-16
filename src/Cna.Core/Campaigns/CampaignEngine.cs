using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

public static class CampaignEngine
{
    public static CampaignCommandResult Decide(
        CampaignSnapshot? snapshot,
        CampaignCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        return command switch
        {
            CreateCampaign create => DecideCreate(snapshot, create),
            CompleteCurrentSequenceStep advance => DecideAdvance(snapshot, advance),
            _ => CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidCommand),
        };
    }

    private static CampaignCommandResult DecideCreate(
        CampaignSnapshot? snapshot,
        CreateCampaign command)
    {
        if (snapshot is not null)
        {
            return CampaignCommandResult.Reject(
                CampaignCommandRejectionReason.CampaignAlreadyCreated);
        }

        if (command.ContractVersion != 1
            || command.ExpectedStateVersion != 0
            || string.IsNullOrWhiteSpace(command.CampaignId)
            || string.IsNullOrWhiteSpace(command.RulesetHash)
            || !Enum.IsDefined(command.FirstPlayer))
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidCommand);
        }

        var initialPosition = Cna1979LandSequence.CreateTurn(1, command.FirstPlayer)[0];

        return CampaignCommandResult.Accept(new CampaignCreated(
            command.CampaignId,
            1,
            command.RulesetHash,
            command.Seed,
            command.FirstPlayer,
            initialPosition));
    }

    private static CampaignCommandResult DecideAdvance(
        CampaignSnapshot? snapshot,
        CompleteCurrentSequenceStep command)
    {
        if (snapshot is null)
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.CampaignNotCreated);
        }

        if (command.ContractVersion != 1 || string.IsNullOrWhiteSpace(command.ExpectedPositionId))
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidCommand);
        }

        if (command.ExpectedStateVersion != snapshot.StateVersion)
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.StaleState);
        }

        if (!string.Equals(
            command.ExpectedPositionId,
            snapshot.SequencePosition.PositionId,
            StringComparison.Ordinal))
        {
            return CampaignCommandResult.Reject(
                CampaignCommandRejectionReason.UnexpectedSequenceStep);
        }

        if (snapshot.OperationStage == 1
            && snapshot.ActiveSide == snapshot.FirstPlayer
            && snapshot.PhaseId == LandPhaseIds.MovementAndCombat
            && snapshot.SegmentId == LandSegmentIds.Movement)
        {
            return CampaignCommandResult.Reject(
                CampaignCommandRejectionReason.UnsupportedTransition);
        }

        LandSequencePosition nextPosition;

        try
        {
            nextPosition = Cna1979LandSequence.GetNext(
                snapshot.SequencePosition,
                snapshot.FirstPlayer);
        }
        catch (ArgumentException)
        {
            return CampaignCommandResult.Reject(
                CampaignCommandRejectionReason.UnsupportedTransition);
        }

        return CampaignCommandResult.Accept(new CampaignSequenceAdvanced(
            snapshot.CampaignId,
            checked(snapshot.StateVersion + 1),
            snapshot.SequencePosition.PositionId,
            nextPosition));
    }
}
