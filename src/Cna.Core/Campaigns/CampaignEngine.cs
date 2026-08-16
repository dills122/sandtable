using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

public static class CampaignEngine
{
    public static CampaignCommandResult Decide(
        CampaignSnapshot? snapshot,
        CampaignCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (snapshot is not null && !CampaignSnapshotValidator.IsValid(snapshot))
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidState);
        }

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
            || !Cna1979Ruleset.IsCanonicalHash(command.RulesetHash)
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

        return CampaignCommandResult.Reject(
            CampaignCommandRejectionReason.UnsupportedTransition);
    }
}
