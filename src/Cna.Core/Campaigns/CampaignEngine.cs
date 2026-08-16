using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

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
            ResolveInitiative resolve => DecideInitiative(snapshot, resolve),
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

        if (command.ContractVersion != 2
            || command.ExpectedStateVersion != 0
            || string.IsNullOrWhiteSpace(command.CampaignId)
            || !Cna1979Ruleset.IsCanonicalHash(command.RulesetHash)
            || string.IsNullOrWhiteSpace(command.SetupId)
            || string.IsNullOrWhiteSpace(command.SetupHash)
            || !Cna1979SetupCatalog.TryGet(command.SetupId, out var setup)
            || !string.Equals(command.SetupHash, setup.Hash, StringComparison.Ordinal))
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidCommand);
        }

        var initialPosition = Cna1979LandSequence.CreateTurn(setup.InitialGameTurn)[0];

        return CampaignCommandResult.Accept(new CampaignCreated(
            command.CampaignId,
            1,
            command.RulesetHash,
            CampaignSetupSnapshot.FromDefinition(setup),
            SandtableRandom.Create(command.Seed),
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

        if (command.ContractVersion != 2 || string.IsNullOrWhiteSpace(command.ExpectedPositionId))
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

    private static CampaignCommandResult DecideInitiative(
        CampaignSnapshot? snapshot,
        ResolveInitiative command)
    {
        if (snapshot is null)
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.CampaignNotCreated);
        }

        if (command.ContractVersion != 2 || string.IsNullOrWhiteSpace(command.ExpectedPositionId))
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

        if (snapshot.SequencePosition.StageId != LandStageIds.InitiativeDetermination
            || snapshot.InitiativeHolder is not null)
        {
            return CampaignCommandResult.Reject(
                CampaignCommandRejectionReason.UnsupportedTransition);
        }

        try
        {
            return CampaignCommandResult.Accept(InitiativeEventFactory.Create(snapshot));
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidOperationException)
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidState);
        }
    }
}
