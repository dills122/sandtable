using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Campaigns;

internal static class CampaignEngine
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

        if (!Cna1979SetupCatalog.IsAdmittedStageEntryPolicy(
            setup.StageEntry,
            setup.InitialGameTurn))
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidState);
        }

        if (!Cna1979SetupCatalog.IsAdmittedWeatherPolicy(setup.Weather))
        {
            return CampaignCommandResult.Reject(
                CampaignCommandRejectionReason.UnsupportedWeatherPolicy);
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
            ResolveNoObligationNavalConvoySchedule resolve => DecideSchedule(snapshot, resolve),
            ResolveNoObligationTacticalShipping resolve => DecideTactical(snapshot, resolve),
            DeclareInitiativeOrder declare => DecideDeclaration(snapshot, declare),
            ResolveWeather resolve => DecideWeather(snapshot, resolve),
            ResolveNoObligationOrganization resolve => DecideOrganization(snapshot, resolve),
            ResolveNoObligationNavalConvoyArrival resolve => DecideArrival(snapshot, resolve),
            ResolveNoObligationFleetAssignment resolve => DecideAssignment(snapshot, resolve),
            ResolveNoObligationFleetRepair resolve => DecideRepair(snapshot, resolve),
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
            || !Cna1979SetupCatalog.IsAdmittedStageEntryPolicy(
                setup.StageEntry,
                setup.InitialGameTurn)
            || !Cna1979SetupCatalog.IsAdmittedWeatherPolicy(setup.Weather)
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
        if (command.ContractVersion != 5
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

    private static CampaignCommandResult DecideSchedule(
        CampaignSnapshot? snapshot,
        ResolveNoObligationNavalConvoySchedule command) => DecidePreamble(
            snapshot,
            command.ContractVersion,
            command.ExpectedStateVersion,
            command.ExpectedPositionId,
            LandPhaseIds.NavalConvoySchedule,
            OpeningPreambleEventFactory.CreateSchedule);

    private static CampaignCommandResult DecideTactical(
        CampaignSnapshot? snapshot,
        ResolveNoObligationTacticalShipping command) => DecidePreamble(
            snapshot,
            command.ContractVersion,
            command.ExpectedStateVersion,
            command.ExpectedPositionId,
            LandPhaseIds.TacticalShipping,
            OpeningPreambleEventFactory.CreateTactical);

    private static CampaignCommandResult DecidePreamble(
        CampaignSnapshot? snapshot,
        int contractVersion,
        long expectedStateVersion,
        string expectedPositionId,
        string phaseId,
        Func<CampaignSnapshot, CampaignEvent> create)
    {
        var rejection = ValidateCurrent(snapshot, contractVersion, expectedStateVersion, expectedPositionId);
        if (rejection != CampaignCommandRejectionReason.None) return CampaignCommandResult.Reject(rejection);
        if (snapshot!.SequencePosition.PhaseId != phaseId || snapshot.InitiativeHolder is null)
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.UnsupportedTransition);
        }
        try { return CampaignCommandResult.Accept(create(snapshot)); }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException or InvalidOperationException)
        { return CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidState); }
    }

    private static CampaignCommandResult DecideDeclaration(CampaignSnapshot? snapshot, DeclareInitiativeOrder command)
    {
        var rejection = ValidateCurrent(snapshot, command.ContractVersion, command.ExpectedStateVersion,
            command.ExpectedPositionId);
        if (rejection != CampaignCommandRejectionReason.None) return CampaignCommandResult.Reject(rejection);
        if (command.OperationStage != 1 || !Enum.IsDefined(command.DeclaringSide)
            || !Enum.IsDefined(command.Choice))
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidCommand);
        }
        try { return CampaignCommandResult.Accept(OpeningPreambleEventFactory.CreateDeclaration(snapshot!, command)); }
        catch (InvalidOperationException) { return CampaignCommandResult.Reject(CampaignCommandRejectionReason.UnsupportedTransition); }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException)
        { return CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidState); }
    }

    private static CampaignCommandRejectionReason ValidateCurrent(CampaignSnapshot? snapshot,
        int contractVersion, long expectedStateVersion, string expectedPositionId)
    {
        if (snapshot is null) return CampaignCommandRejectionReason.CampaignNotCreated;
        if (contractVersion != 1 || string.IsNullOrWhiteSpace(expectedPositionId))
            return CampaignCommandRejectionReason.InvalidCommand;
        if (expectedStateVersion != snapshot.StateVersion) return CampaignCommandRejectionReason.StaleState;
        return string.Equals(expectedPositionId, snapshot.SequencePosition.PositionId, StringComparison.Ordinal)
            ? CampaignCommandRejectionReason.None
            : CampaignCommandRejectionReason.UnexpectedSequenceStep;
    }

    private static CampaignCommandResult DecideWeather(
        CampaignSnapshot? snapshot,
        ResolveWeather command)
    {
        var rejection = ValidateCurrent(snapshot, command.ContractVersion,
            command.ExpectedStateVersion, command.ExpectedPositionId);
        if (rejection != CampaignCommandRejectionReason.None)
        {
            return CampaignCommandResult.Reject(rejection);
        }
        if (snapshot!.PhaseId != LandPhaseIds.WeatherDetermination)
        {
            return CampaignCommandResult.Reject(
                CampaignCommandRejectionReason.UnsupportedTransition);
        }
        try
        {
            return CampaignCommandResult.Accept(WeatherEventFactory.Create(snapshot));
        }
        catch (ArgumentOutOfRangeException)
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.UnsupportedTransition);
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidOperationException)
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidState);
        }
    }

    private static CampaignCommandResult DecideOrganization(
        CampaignSnapshot? snapshot,
        ResolveNoObligationOrganization command) => DecideStageEntry(
            snapshot,
            command.ContractVersion,
            command.ExpectedStateVersion,
            command.ExpectedPositionId,
            LandPhaseIds.Organization,
            null,
            StageEntryEventFactory.CreateOrganization);

    private static CampaignCommandResult DecideArrival(
        CampaignSnapshot? snapshot,
        ResolveNoObligationNavalConvoyArrival command) => DecideStageEntry(
            snapshot,
            command.ContractVersion,
            command.ExpectedStateVersion,
            command.ExpectedPositionId,
            LandPhaseIds.NavalConvoyArrival,
            null,
            StageEntryEventFactory.CreateArrival);

    private static CampaignCommandResult DecideAssignment(
        CampaignSnapshot? snapshot,
        ResolveNoObligationFleetAssignment command) => DecideStageEntry(
            snapshot,
            command.ContractVersion,
            command.ExpectedStateVersion,
            command.ExpectedPositionId,
            LandPhaseIds.CommonwealthFleet,
            LandSegmentIds.FleetAssignment,
            StageEntryEventFactory.CreateAssignment);

    private static CampaignCommandResult DecideRepair(
        CampaignSnapshot? snapshot,
        ResolveNoObligationFleetRepair command) => DecideStageEntry(
            snapshot,
            command.ContractVersion,
            command.ExpectedStateVersion,
            command.ExpectedPositionId,
            LandPhaseIds.CommonwealthFleet,
            LandSegmentIds.FleetRepair,
            StageEntryEventFactory.CreateRepair);

    private static CampaignCommandResult DecideStageEntry(
        CampaignSnapshot? snapshot,
        int contractVersion,
        long expectedStateVersion,
        string expectedPositionId,
        string expectedPhaseId,
        string? expectedSegmentId,
        Func<CampaignSnapshot, CampaignEvent> create)
    {
        var rejection = ValidateCurrent(snapshot, contractVersion,
            expectedStateVersion, expectedPositionId);
        if (rejection != CampaignCommandRejectionReason.None)
            return CampaignCommandResult.Reject(rejection);
        if (!string.Equals(snapshot!.PhaseId, expectedPhaseId, StringComparison.Ordinal)
            || !string.Equals(snapshot.SegmentId, expectedSegmentId, StringComparison.Ordinal))
            return CampaignCommandResult.Reject(
                CampaignCommandRejectionReason.UnsupportedTransition);
        try
        {
            return CampaignCommandResult.Accept(create(snapshot));
        }
        catch (InvalidOperationException)
        {
            return CampaignCommandResult.Reject(
                CampaignCommandRejectionReason.UnsupportedTransition);
        }
        catch (Exception exception) when (exception is ArgumentException or ArithmeticException)
        {
            return CampaignCommandResult.Reject(CampaignCommandRejectionReason.InvalidState);
        }
    }
}

internal static class StageEntryEventFactory
{
    public static NoObligationOrganizationResolved CreateOrganization(
        CampaignSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var policy = snapshot.Setup.StageEntry;
        var expectedPosition = Cna1979LandSequence.CreateTurn(snapshot.GameTurn).Single(value =>
            value.OperationStage == 1
            && value.PhaseId == LandPhaseIds.Organization
            && value.SegmentId is null);

        if (!Cna1979SetupCatalog.IsAdmittedStageEntryPolicy(
                policy,
                snapshot.Setup.InitialGameTurn)
            || policy.GameTurn != snapshot.GameTurn
            || policy.OperationStage != snapshot.OperationStage
            || policy.Organization != StageEntryObligationKind.ExplicitNone
            || snapshot.SequencePosition != expectedPosition)
        {
            throw new InvalidOperationException("Organization authority is not admitted.");
        }

        var successor = Cna1979LandSequence.GetNext(snapshot.SequencePosition);
        if (successor.GameTurn != snapshot.GameTurn
            || successor.OperationStage != snapshot.OperationStage
            || successor.PhaseId != LandPhaseIds.NavalConvoyArrival
            || successor.SegmentId is not null)
        {
            throw new InvalidOperationException(
                "Organization must advance to Naval Convoy Arrival in the same pair.");
        }

        return new NoObligationOrganizationResolved(
            snapshot.CampaignId,
            checked(snapshot.StateVersion + 1),
            snapshot.SequencePosition.PositionId,
            snapshot.GameTurn,
            snapshot.OperationStage,
            successor,
            NoObligationOrganizationResolved.RequiredSources);
    }

    public static NoObligationNavalConvoyArrivalResolved CreateArrival(
        CampaignSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var policy = snapshot.Setup.StageEntry;
        var expectedPosition = Cna1979LandSequence.CreateTurn(snapshot.GameTurn).Single(value =>
            value.OperationStage == 1
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival
            && value.SegmentId is null);

        if (!Cna1979SetupCatalog.IsAdmittedStageEntryPolicy(
                policy,
                snapshot.Setup.InitialGameTurn)
            || policy.GameTurn != snapshot.GameTurn
            || policy.OperationStage != snapshot.OperationStage
            || policy.NavalConvoyArrival != StageEntryObligationKind.ExplicitNone
            || snapshot.SequencePosition != expectedPosition)
        {
            throw new InvalidOperationException(
                "Naval Convoy Arrival authority is not admitted.");
        }

        var successor = Cna1979LandSequence.GetNext(snapshot.SequencePosition);
        if (successor.GameTurn != snapshot.GameTurn
            || successor.OperationStage != snapshot.OperationStage
            || successor.PhaseId != LandPhaseIds.CommonwealthFleet
            || successor.SegmentId != LandSegmentIds.FleetAssignment)
        {
            throw new InvalidOperationException(
                "Naval Convoy Arrival must advance to Fleet Assignment in the same pair.");
        }

        return new NoObligationNavalConvoyArrivalResolved(
            snapshot.CampaignId,
            checked(snapshot.StateVersion + 1),
            snapshot.SequencePosition.PositionId,
            snapshot.GameTurn,
            snapshot.OperationStage,
            successor,
            NoObligationNavalConvoyArrivalResolved.RequiredSources);
    }

    public static NoObligationFleetAssignmentResolved CreateAssignment(
        CampaignSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var policy = snapshot.Setup.StageEntry;
        var expectedPosition = Cna1979LandSequence.CreateTurn(snapshot.GameTurn).Single(value =>
            value.OperationStage == 1
            && value.PhaseId == LandPhaseIds.CommonwealthFleet
            && value.SegmentId == LandSegmentIds.FleetAssignment);
        if (!Cna1979SetupCatalog.IsAdmittedStageEntryPolicy(
                policy, snapshot.Setup.InitialGameTurn)
            || policy.GameTurn != snapshot.GameTurn
            || policy.OperationStage != snapshot.OperationStage
            || policy.FleetAssignment != StageEntryObligationKind.ExplicitNone
            || snapshot.SequencePosition != expectedPosition)
            throw new InvalidOperationException("Fleet Assignment authority is not admitted.");

        var successor = Cna1979LandSequence.GetNext(snapshot.SequencePosition);
        if (successor.GameTurn != snapshot.GameTurn
            || successor.OperationStage != snapshot.OperationStage
            || successor.PhaseId != LandPhaseIds.CommonwealthFleet
            || successor.SegmentId != LandSegmentIds.FleetRepair)
            throw new InvalidOperationException(
                "Fleet Assignment must advance to Fleet Repair in the same pair.");

        return new NoObligationFleetAssignmentResolved(snapshot.CampaignId,
            checked(snapshot.StateVersion + 1), snapshot.SequencePosition.PositionId,
            snapshot.GameTurn, snapshot.OperationStage, successor,
            NoObligationFleetAssignmentResolved.RequiredSources);
    }

    public static NoObligationFleetRepairResolved CreateRepair(CampaignSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var policy = snapshot.Setup.StageEntry;
        var expectedPosition = Cna1979LandSequence.CreateTurn(snapshot.GameTurn).Single(value =>
            value.OperationStage == 1
            && value.PhaseId == LandPhaseIds.CommonwealthFleet
            && value.SegmentId == LandSegmentIds.FleetRepair);
        var pairOrders = snapshot.OperationStageOrders.Where(value =>
            value.GameTurn == snapshot.GameTurn
            && value.OperationStage == snapshot.OperationStage).ToArray();
        if (!Cna1979SetupCatalog.IsAdmittedStageEntryPolicy(
                policy, snapshot.Setup.InitialGameTurn)
            || policy.GameTurn != snapshot.GameTurn
            || policy.OperationStage != snapshot.OperationStage
            || policy.FleetRepair != StageEntryObligationKind.ExplicitNone
            || snapshot.SequencePosition != expectedPosition
            || pairOrders.Length != 1
            || !Enum.IsDefined(pairOrders[0].FirstSide))
            throw new InvalidOperationException("Fleet Repair authority is not admitted.");

        var successor = Cna1979LandSequence.GetNext(snapshot.SequencePosition);
        if (successor.GameTurn != snapshot.GameTurn
            || successor.OperationStage != snapshot.OperationStage
            || successor.PhaseId != LandPhaseIds.ReserveDesignation
            || successor.SegmentId is not null
            || successor.ActorRole != LandActorRole.FirstActingSide
            || successor.ActiveSide is not null)
            throw new InvalidOperationException(
                "Fleet Repair must advance to unmaterialized first-side Reserve authority.");

        return new NoObligationFleetRepairResolved(snapshot.CampaignId,
            checked(snapshot.StateVersion + 1), snapshot.SequencePosition.PositionId,
            snapshot.GameTurn, snapshot.OperationStage, successor,
            NoObligationFleetRepairResolved.RequiredSources);
    }
}
