using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Actions;

public static class CampaignLegalActions
{
    public static CampaignLegalActionQueryResult Query(
        CampaignAuthorityHandle handle,
        CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(handle);
        if (!Enum.IsDefined(audience))
            return CampaignLegalActionQueryResult.Rejected(
                CampaignLegalActionQueryRejectionReason.InvalidAudience);
        if (handle.CurrentSnapshot is null)
        {
            return QueryLegacy(handle.Snapshot, handle.Context, audience);
        }

        var snapshot = handle.CurrentSnapshot;
        if (handle.Context.ArtifactV5 is null
            || !CampaignSnapshotV10Validator.IsValid(
                snapshot,
                handle.Context.ArtifactV5,
                handle.Context.Scenario))
            return CampaignLegalActionQueryResult.Rejected(
                CampaignLegalActionQueryRejectionReason.InvalidState);

        if (audience == CampaignActionAudience.System)
        {
            return CampaignLegalActionQueryResult.Success(
                snapshot.ReactionWindow is null
                    ? GenerateForSystem(snapshot)
                    : CampaignObservationV6ActionDerivation.DeriveSystem(
                        ProjectV6(handle, snapshot.ReactionWindow.ReactingSide)));
        }

        var observer = ToSide(audience);
        var observation = ProjectV6(handle, observer);
        return CampaignLegalActionQueryResult.Success(
            snapshot.ReactionWindow is not null
                || IsMovementPosition(observation)
                ? CampaignObservationV6ActionDerivation.DerivePlayer(observation)
                : GenerateForSide(observation, audience));
    }

    public static CampaignActionSubmissionResult Submit(
        CampaignAuthorityHandle handle,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(submission);
        if (handle.CurrentSnapshot is null)
        {
            var legacy = CampaignActionExecution.Execute(
                handle.Snapshot,
                handle.Context,
                submission);
            return legacy.IsAccepted
                ? CampaignActionSubmissionResult.Accepted(
                    new CampaignAuthorityHandle(legacy.SuccessorSnapshot!, handle.Context),
                    legacy.Receipt!)
                : CampaignActionSubmissionResult.Rejected(legacy.RejectionReason);
        }

        var current = CampaignCurrentActionExecution.Execute(
            handle.CurrentSnapshot,
            handle.Context,
            submission);
        return current.IsAccepted
            ? CampaignActionSubmissionResult.Accepted(
                new CampaignAuthorityHandle(current.SuccessorSnapshot!, handle.Context),
                current.Receipt!)
            : CampaignActionSubmissionResult.Rejected(current.RejectionReason);
    }

    internal static CampaignLegalActionSet GenerateForSide(
        CampaignObservation observation,
        CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var expectedObserver = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => throw new ArgumentOutOfRangeException(nameof(audience)),
        };
        IReadOnlyList<CampaignActionCandidate> candidates = observation.Observer == expectedObserver
            ? GenerateSideCandidates(observation, expectedObserver)
            : [];
        return CreateSet(observation.CampaignId, observation.StateVersion, observation.RulesetHash,
            observation.Position.PositionId, audience, candidates);
    }

    internal static CampaignLegalActionQueryResult QueryLegacy(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        CampaignActionAudience audience)
    {
        if (!Enum.IsDefined(audience))
            return CampaignLegalActionQueryResult.Rejected(
                CampaignLegalActionQueryRejectionReason.InvalidAudience);
        if (!CampaignSnapshotValidator.IsValid(snapshot, context))
            return CampaignLegalActionQueryResult.Rejected(
                CampaignLegalActionQueryRejectionReason.InvalidState);
        return CampaignLegalActionQueryResult.Success(audience == CampaignActionAudience.System
            ? GenerateForSystem(snapshot)
            : GenerateForSide(ProjectLegacy(snapshot, context, audience), audience));
    }

    private static CampaignLegalActionSet GenerateForSide(
        CampaignObservationV6 observation,
        CampaignActionAudience audience)
    {
        var observer = ToSide(audience);
        IReadOnlyList<CampaignActionCandidate> candidates = observation.Observer != observer
            ? []
            : observation.Position switch
            {
                {
                    PhaseId: LandPhaseIds.InitiativeDeclaration,
                    OperationStage: 1,
                    InitiativeHolder: var holder,
                } when holder == observer => [new ActFirstAction(1), new ActLastAction(1)],
                {
                    StageId: LandStageIds.Operation,
                    PhaseId: LandPhaseIds.ReserveDesignation,
                    OperationStage: 1,
                    ActorRole: LandActorRole.FirstActingSide,
                    ActiveSide: var active,
                } when active == observer => observation.OwnElements
                    .Where(element =>
                        element.ReserveStatus == CampaignObservationReserveStatus.None)
                    .Select(element => (CampaignActionCandidate)new DesignateReserveAction(
                        element.ElementId))
                    .Append(new CompleteReserveDesignationAction())
                    .ToArray(),
                _ => [],
            };
        return CreateSet(
            observation.CampaignId,
            observation.StateVersion,
            observation.RulesetHash,
            observation.Position.PositionId,
            audience,
            candidates);
    }

    private static CampaignActionCandidate[] GenerateSideCandidates(
        CampaignObservation observation,
        LandSide observer)
    {
        if (observation.Position.PhaseId == LandPhaseIds.InitiativeDeclaration
            && observation.Position.OperationStage == 1
            && observation.Position.InitiativeHolder == observer)
        {
            return [new ActFirstAction(1), new ActLastAction(1)];
        }

        if (observation.Position.StageId == LandStageIds.Operation
            && observation.Position.PhaseId == LandPhaseIds.ReserveDesignation
            && observation.Position.OperationStage == 1
            && observation.Position.ActorRole == LandActorRole.FirstActingSide
            && observation.Position.ActiveSide == observer)
        {
            return observation.OwnElements
                .Where(element => element.ReserveStatus == CampaignObservationReserveStatus.None)
                .Select(element => (CampaignActionCandidate)new DesignateReserveAction(
                    element.ElementId))
                .Append(new CompleteReserveDesignationAction())
                .ToArray();
        }

        if (observation.Position.StageId == LandStageIds.Operation
            && observation.Position.PhaseId == LandPhaseIds.MovementAndCombat
            && observation.Position.SegmentId == LandSegmentIds.Movement
            && observation.Position.OperationStage == 1
            && observation.Position.ActorRole == LandActorRole.FirstActingSide
            && observation.Position.ActiveSide == observer)
        {
            return CampaignMovementActionDerivation.Derive(observation).ToArray();
        }

        return [];
    }

    private static CampaignObservation ProjectLegacy(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        CampaignActionAudience audience)
    {
        var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var sequence = snapshot.SequencePosition;
        var expectedActiveSide = sequence.ActorRole == LandActorRole.FirstActingSide
            ? FirstActingSideResolver.Resolve(snapshot)
            : sequence.ActiveSide;
        var result = CampaignObservationProjector.Project(snapshot, context, side);
        var observation = result.Observation
            ?? throw new InvalidOperationException("Admitted authority must project.");
        if (observation.Position.ActiveSide != expectedActiveSide)
        {
            throw new InvalidOperationException(
                "Legal-action projection must preserve the resolved active audience.");
        }

        return observation;
    }

    internal static CampaignObservationV6 ProjectV6(
        CampaignAuthorityHandle handle,
        LandSide observer)
    {
        var result = CampaignObservations.Query(handle, observer);
        return result.Observation
            ?? throw new InvalidOperationException("Admitted current authority must project.");
    }

    private static CampaignLegalActionSet GenerateForSystem(CampaignSnapshot snapshot)
    {
        var candidates = GenerateSystemCandidates(snapshot);
        return CreateSet(snapshot.CampaignId, snapshot.StateVersion, snapshot.RulesetHash,
            snapshot.SequencePosition.PositionId, CampaignActionAudience.System, candidates);
    }

    private static CampaignLegalActionSet GenerateForSystem(CampaignSnapshotV10 snapshot)
    {
        var sequence = snapshot.CurrentPosition.SequencePosition
            ?? throw new InvalidOperationException(
                "Normal System generation requires a sequence position.");
        IReadOnlyList<CampaignActionCandidate> candidates = sequence switch
        {
            {
                OperationStage: 0,
                StageId: LandStageIds.InitiativeDetermination,
                PhaseId: LandPhaseIds.InitiativeDetermination,
            } when HasAdmittedInitiativePolicy(snapshot) => [new ResolveInitiativeAction()],
            {
                OperationStage: 0,
                StageId: LandStageIds.NavalConvoy,
                PhaseId: LandPhaseIds.NavalConvoySchedule,
            } when HasAdmittedOpeningPreamblePolicy(snapshot) =>
                [new ResolveNoObligationNavalConvoyScheduleAction()],
            {
                OperationStage: 0,
                StageId: LandStageIds.NavalConvoy,
                PhaseId: LandPhaseIds.TacticalShipping,
            } when HasAdmittedOpeningPreamblePolicy(snapshot) =>
                [new ResolveNoObligationTacticalShippingAction()],
            {
                OperationStage: 1,
                StageId: LandStageIds.Operation,
                PhaseId: LandPhaseIds.WeatherDetermination,
            } when Cna1979SetupCatalog.IsAdmittedWeatherPolicy(snapshot.Setup.Weather) =>
                [new ResolveWeatherAction()],
            {
                OperationStage: 1,
                StageId: LandStageIds.Operation,
                PhaseId: LandPhaseIds.Organization,
                SegmentId: null,
            } when HasAdmittedStageEntryPolicy(snapshot, snapshot.Setup.StageEntry.Organization) =>
                [new ResolveNoObligationOrganizationAction()],
            {
                OperationStage: 1,
                StageId: LandStageIds.Operation,
                PhaseId: LandPhaseIds.NavalConvoyArrival,
                SegmentId: null,
            } when HasAdmittedStageEntryPolicy(
                snapshot,
                snapshot.Setup.StageEntry.NavalConvoyArrival) =>
                [new ResolveNoObligationNavalConvoyArrivalAction()],
            {
                OperationStage: 1,
                StageId: LandStageIds.Operation,
                PhaseId: LandPhaseIds.CommonwealthFleet,
                SegmentId: LandSegmentIds.FleetAssignment,
            } when HasAdmittedStageEntryPolicy(
                snapshot,
                snapshot.Setup.StageEntry.FleetAssignment) =>
                [new ResolveNoObligationFleetAssignmentAction()],
            {
                OperationStage: 1,
                StageId: LandStageIds.Operation,
                PhaseId: LandPhaseIds.CommonwealthFleet,
                SegmentId: LandSegmentIds.FleetRepair,
            } when HasAdmittedStageEntryPolicy(
                snapshot,
                snapshot.Setup.StageEntry.FleetRepair) =>
                [new ResolveNoObligationFleetRepairAction()],
            _ => [],
        };
        return CreateSet(
            snapshot.CampaignId,
            snapshot.StateVersion,
            snapshot.RulesetHash,
            sequence.PositionId,
            CampaignActionAudience.System,
            candidates);
    }

    private static IReadOnlyList<CampaignActionCandidate> GenerateSystemCandidates(
        CampaignSnapshot snapshot) => snapshot.SequencePosition switch
        {
            {
                OperationStage: 0,
                StageId: LandStageIds.InitiativeDetermination,
                PhaseId: LandPhaseIds.InitiativeDetermination,
            } when HasAdmittedInitiativePolicy(snapshot) => [new ResolveInitiativeAction()],
            {
                OperationStage: 0,
                StageId: LandStageIds.NavalConvoy,
                PhaseId: LandPhaseIds.NavalConvoySchedule,
            } when HasAdmittedOpeningPreamblePolicy(snapshot) =>
                [new ResolveNoObligationNavalConvoyScheduleAction()],
            {
                OperationStage: 0,
                StageId: LandStageIds.NavalConvoy,
                PhaseId: LandPhaseIds.TacticalShipping,
            } when HasAdmittedOpeningPreamblePolicy(snapshot) =>
                [new ResolveNoObligationTacticalShippingAction()],
            {
                OperationStage: 1,
                StageId: LandStageIds.Operation,
                PhaseId: LandPhaseIds.WeatherDetermination,
            } when Cna1979SetupCatalog.IsAdmittedWeatherPolicy(snapshot.Setup.Weather) =>
                [new ResolveWeatherAction()],
            {
                OperationStage: 1,
                StageId: LandStageIds.Operation,
                PhaseId: LandPhaseIds.Organization,
                SegmentId: null,
            } when HasAdmittedStageEntryPolicy(
                snapshot,
                snapshot.Setup.StageEntry.Organization) =>
                [new ResolveNoObligationOrganizationAction()],
            {
                OperationStage: 1,
                StageId: LandStageIds.Operation,
                PhaseId: LandPhaseIds.NavalConvoyArrival,
                SegmentId: null,
            } when HasAdmittedStageEntryPolicy(
                snapshot,
                snapshot.Setup.StageEntry.NavalConvoyArrival) =>
                [new ResolveNoObligationNavalConvoyArrivalAction()],
            {
                OperationStage: 1,
                StageId: LandStageIds.Operation,
                PhaseId: LandPhaseIds.CommonwealthFleet,
                SegmentId: LandSegmentIds.FleetAssignment,
            } when HasAdmittedStageEntryPolicy(
                snapshot,
                snapshot.Setup.StageEntry.FleetAssignment) =>
                [new ResolveNoObligationFleetAssignmentAction()],
            {
                OperationStage: 1,
                StageId: LandStageIds.Operation,
                PhaseId: LandPhaseIds.CommonwealthFleet,
                SegmentId: LandSegmentIds.FleetRepair,
            } when HasAdmittedStageEntryPolicy(
                snapshot,
                snapshot.Setup.StageEntry.FleetRepair) =>
                [new ResolveNoObligationFleetRepairAction()],
            _ => [],
        };

    private static bool HasAdmittedInitiativePolicy(CampaignSnapshot snapshot) =>
        Cna1979SetupCatalog.TryGet(snapshot.Setup.SetupId, out var definition)
        && snapshot.Setup.InitialInitiative == definition.InitialInitiative;

    private static bool HasAdmittedOpeningPreamblePolicy(CampaignSnapshot snapshot) =>
        snapshot.Setup.OpeningPreamble == Cna1979SetupCatalog.OpeningPreamblePolicy;

    private static bool HasAdmittedStageEntryPolicy(
        CampaignSnapshot snapshot,
        StageEntryObligationKind obligation) =>
        Cna1979SetupCatalog.IsAdmittedStageEntryPolicy(
            snapshot.Setup.StageEntry,
            snapshot.Setup.InitialGameTurn)
        && snapshot.Setup.StageEntry.GameTurn == snapshot.GameTurn
        && snapshot.Setup.StageEntry.OperationStage == snapshot.OperationStage
        && obligation == StageEntryObligationKind.ExplicitNone;

    private static bool HasAdmittedInitiativePolicy(CampaignSnapshotV10 snapshot) =>
        Cna1979SetupCatalog.TryGet(snapshot.Setup.SetupId, out var definition)
        && snapshot.Setup.InitialInitiative == definition.InitialInitiative;

    private static bool HasAdmittedOpeningPreamblePolicy(CampaignSnapshotV10 snapshot) =>
        snapshot.Setup.OpeningPreamble == Cna1979SetupCatalog.OpeningPreamblePolicy;

    private static bool HasAdmittedStageEntryPolicy(
        CampaignSnapshotV10 snapshot,
        StageEntryObligationKind obligation) =>
        Cna1979SetupCatalog.IsAdmittedStageEntryPolicy(
            snapshot.Setup.StageEntry,
            snapshot.Setup.InitialGameTurn)
        && snapshot.Setup.StageEntry.GameTurn
            == snapshot.CurrentPosition.SequencePosition!.GameTurn
        && snapshot.Setup.StageEntry.OperationStage
            == snapshot.CurrentPosition.SequencePosition.OperationStage
        && obligation == StageEntryObligationKind.ExplicitNone;

    private static bool IsMovementPosition(CampaignObservationV6 observation) =>
        observation.Position.StageId == LandStageIds.Operation
        && observation.Position.PhaseId == LandPhaseIds.MovementAndCombat
        && observation.Position.SegmentId == LandSegmentIds.Movement;

    private static LandSide ToSide(CampaignActionAudience audience) => audience switch
    {
        CampaignActionAudience.Axis => LandSide.Axis,
        CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
        _ => throw new ArgumentOutOfRangeException(nameof(audience)),
    };

    private static CampaignLegalActionSet CreateSet(string campaignId, long stateVersion,
        string rulesetHash, string positionId, CampaignActionAudience audience,
        IReadOnlyList<CampaignActionCandidate> candidates) =>
        new(campaignId, stateVersion, rulesetHash, positionId, audience, candidates);

}
