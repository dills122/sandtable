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
        if (!CampaignSnapshotValidator.IsValid(handle.Snapshot, handle.Context))
            return CampaignLegalActionQueryResult.Rejected(
                CampaignLegalActionQueryRejectionReason.InvalidState);

        return CampaignLegalActionQueryResult.Success(audience == CampaignActionAudience.System
            ? GenerateForSystem(handle.Snapshot)
            : GenerateForSide(Project(handle, audience), audience));
    }

    public static CampaignActionSubmissionResult Submit(
        CampaignAuthorityHandle handle,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(handle);
        ArgumentNullException.ThrowIfNull(submission);
        var execution = CampaignActionExecution.Execute(handle.Snapshot, handle.Context, submission);
        return execution.IsAccepted
            ? CampaignActionSubmissionResult.Accepted(
                new CampaignAuthorityHandle(execution.SuccessorSnapshot!, handle.Context),
                execution.Receipt!)
            : CampaignActionSubmissionResult.Rejected(execution.RejectionReason);
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

        return [];
    }

    private static CampaignObservation Project(CampaignAuthorityHandle handle,
        CampaignActionAudience audience)
    {
        var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var sequence = handle.Snapshot.SequencePosition;
        var expectedActiveSide = sequence.ActorRole == LandActorRole.FirstActingSide
            ? FirstActingSideResolver.Resolve(handle.Snapshot)
            : sequence.ActiveSide;
        var result = CampaignObservationProjector.Project(handle.Snapshot, handle.Context, side);
        var observation = result.Observation
            ?? throw new InvalidOperationException("Admitted authority must project.");
        if (observation.Position.ActiveSide != expectedActiveSide)
        {
            throw new InvalidOperationException(
                "Legal-action projection must preserve the resolved active audience.");
        }

        return observation;
    }

    private static CampaignLegalActionSet GenerateForSystem(CampaignSnapshot snapshot)
    {
        var candidates = GenerateSystemCandidates(snapshot);
        return CreateSet(snapshot.CampaignId, snapshot.StateVersion, snapshot.RulesetHash,
            snapshot.SequencePosition.PositionId, CampaignActionAudience.System, candidates);
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

    private static CampaignLegalActionSet CreateSet(string campaignId, long stateVersion,
        string rulesetHash, string positionId, CampaignActionAudience audience,
        IReadOnlyList<CampaignActionCandidate> candidates) =>
        new(campaignId, stateVersion, rulesetHash, positionId, audience, candidates);

}
