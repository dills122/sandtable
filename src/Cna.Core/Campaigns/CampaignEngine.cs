using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

public static class CampaignEngine
{
    public static CampaignCommandResult DecideCreation(
        CampaignSnapshot? snapshot,
        CreateCampaign command,
        IContentPackResolver resolver) => DecideCreation(
            snapshot,
            command,
            resolver,
            Cna1979SetupCatalog.Definitions);

    internal static CampaignCommandResult DecideCreation(
        CampaignSnapshot? snapshot,
        CreateCampaign command,
        IContentPackResolver resolver,
        IReadOnlyList<CampaignSetupDefinition> setupDefinitions)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(resolver);
        ArgumentNullException.ThrowIfNull(setupDefinitions);

        if (snapshot is not null)
        {
            var priorContext = ResolveContext(snapshot.Setup?.Content, resolver);
            return priorContext is null
                || !CampaignSnapshotValidator.IsValid(snapshot, priorContext)
                ? CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidState)
                : CampaignCommandResult.Reject(
                    CampaignCommandRejectionReason.CampaignAlreadyCreated);
        }

        if (!IsLocallyValid(command))
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidCommand);
        }

        if (!Cna1979Ruleset.IsCanonicalHash(command.RulesetHash))
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.UnsupportedRuleset);
        }

        var setup = setupDefinitions.FirstOrDefault(candidate => string.Equals(
            candidate.SetupId,
            command.SetupId,
            StringComparison.Ordinal));

        if (setup is null)
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.UnknownSetup);
        }

        if (!string.Equals(command.SetupHash, setup.Hash, StringComparison.Ordinal))
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.SetupHashMismatch);
        }

        var resolution = resolver.Resolve(command.ContentPackId, command.ContentHash);

        if (!resolution.IsResolved)
        {
            return CampaignCommandResult.Reject(resolution.RejectionReason switch
            {
                ContentCatalogRejectionReason.UnknownPackId => CampaignCommandRejectionReason.UnknownContent,
                ContentCatalogRejectionReason.HashMismatch => CampaignCommandRejectionReason.ContentHashMismatch,
                _ => CampaignCommandRejectionReason.InvalidCommand,
            });
        }

        var artifact = resolution.Artifact!;

        if (!string.Equals(artifact.Identity.RulesetId, Cna1979Ruleset.RulesetId, StringComparison.Ordinal))
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.UnsupportedRuleset);
        }

        if (!artifact.Definition.Scenarios.Any(candidate => string.Equals(
            candidate.ScenarioId,
            command.ScenarioId,
            StringComparison.Ordinal)))
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.UnknownScenario);
        }

        var commandSelection = new CampaignContentSelection(artifact.Identity, command.ScenarioId);

        if (setup.Content != commandSelection)
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.SetupContentMismatch);
        }

        var context = CampaignContentContext.Create(artifact, command.ScenarioId);

        if (context.Scenario.Start.GameTurn != setup.InitialGameTurn)
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.ScenarioStartMismatch);
        }

        return Decide(null, command, context);
    }

    public static CampaignCommandResult Decide(
        CampaignSnapshot? snapshot,
        CampaignCommand command,
        CampaignContentContext context)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(context);

        if (snapshot is not null && !CampaignSnapshotValidator.IsValid(snapshot, context))
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidState);
        }

        return command switch
        {
            CreateCampaign create => DecideCreate(snapshot, create, context),
            ResolveInitiative resolve => DecideInitiative(snapshot, resolve),
            CompleteCurrentSequenceStep advance => DecideAdvance(snapshot, advance),
            _ => CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidCommand),
        };
    }

    private static CampaignCommandResult DecideCreate(
        CampaignSnapshot? snapshot,
        CreateCampaign command,
        CampaignContentContext context)
    {
        if (snapshot is not null)
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.CampaignAlreadyCreated);
        }

        if (!IsLocallyValid(command)
            || !Cna1979Ruleset.IsCanonicalHash(command.RulesetHash)
            || !Cna1979SetupCatalog.TryGet(command.SetupId, out var setup)
            || !string.Equals(command.SetupHash, setup.Hash, StringComparison.Ordinal)
            || setup.Content != context.Selection
            || !string.Equals(command.ContentPackId, context.Artifact.Identity.PackId, StringComparison.Ordinal)
            || !string.Equals(command.ContentHash, context.Artifact.Identity.Hash, StringComparison.Ordinal)
            || !string.Equals(command.ScenarioId, context.Scenario.ScenarioId, StringComparison.Ordinal)
            || context.Scenario.Start.GameTurn != setup.InitialGameTurn)
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidCommand);
        }

        var initialPosition = Cna1979LandSequence.CreateTurn(setup.InitialGameTurn)[0];
        var initialWorld = CampaignWorldFactory.CreateInitial(context.Artifact, context.Scenario);

        return CampaignCommandResult.Accept(new CampaignCreated(
            command.CampaignId,
            1,
            command.RulesetHash,
            CampaignSetupSnapshot.FromDefinition(setup),
            initialWorld,
            SandtableRandom.Create(command.Seed),
            initialPosition));
    }

    private static bool IsLocallyValid(CreateCampaign command)
    {
        if (command.ContractVersion != 3
            || command.ExpectedStateVersion != 0
            || !CampaignSnapshotValidator.IsRulesHash(command.RulesetHash))
        {
            return false;
        }

        try
        {
            _ = ContentContractGuards.RequireStableId(command.CampaignId, nameof(command.CampaignId));
            _ = ContentContractGuards.RequireStableId(command.SetupId, nameof(command.SetupId));
            _ = ContentContractGuards.RequireSha256(command.SetupHash, nameof(command.SetupHash));
            _ = ContentContractGuards.RequireStableId(command.ContentPackId, nameof(command.ContentPackId));
            _ = ContentContractGuards.RequireSha256(command.ContentHash, nameof(command.ContentHash));
            _ = ContentContractGuards.RequireStableId(command.ScenarioId, nameof(command.ScenarioId));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static CampaignContentContext? ResolveContext(
        CampaignContentSelection? selection,
        IContentPackResolver resolver)
    {
        if (selection is null)
        {
            return null;
        }

        var resolution = resolver.Resolve(selection.Pack.PackId, selection.Pack.Hash);

        if (!resolution.IsResolved)
        {
            return null;
        }

        try
        {
            return CampaignContentContext.Create(resolution.Artifact!, selection.ScenarioId);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static CampaignCommandResult DecideAdvance(CampaignSnapshot? snapshot, CompleteCurrentSequenceStep command)
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

        if (!string.Equals(command.ExpectedPositionId, snapshot.SequencePosition.PositionId, StringComparison.Ordinal))
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.UnexpectedSequenceStep);
        }

        return CampaignCommandResult.Reject(CampaignCommandRejectionReason.UnsupportedTransition);
    }

    private static CampaignCommandResult DecideInitiative(CampaignSnapshot? snapshot, ResolveInitiative command)
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

        if (!string.Equals(command.ExpectedPositionId, snapshot.SequencePosition.PositionId, StringComparison.Ordinal))
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.UnexpectedSequenceStep);
        }

        if (snapshot.SequencePosition.StageId != LandStageIds.InitiativeDetermination
            || snapshot.InitiativeHolder is not null)
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.UnsupportedTransition);
        }

        try
        {
            return CampaignCommandResult.Accept(InitiativeEventFactory.Create(snapshot));
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException or InvalidOperationException)
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidState);
        }
    }
}
