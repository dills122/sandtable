using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;

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
        IReadOnlyList<CampaignActionCandidate> candidates =
            observation.Observer == expectedObserver
            && observation.Position.PhaseId == LandPhaseIds.InitiativeDeclaration
            && observation.Position.OperationStage == 1
            && observation.Position.InitiativeHolder == expectedObserver
                ? [new ActFirstAction(1), new ActLastAction(1)]
                : [];
        return CreateSet(observation.CampaignId, observation.StateVersion, observation.RulesetHash,
            observation.Position.PositionId, audience, candidates);
    }

    private static CampaignObservation Project(CampaignAuthorityHandle handle,
        CampaignActionAudience audience)
    {
        var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
        var result = CampaignObservationProjector.Project(handle.Snapshot, handle.Context, side);
        return result.Observation ?? throw new InvalidOperationException("Admitted authority must project.");
    }

    private static CampaignLegalActionSet GenerateForSystem(CampaignSnapshot snapshot)
    {
        IReadOnlyList<CampaignActionCandidate> candidates = snapshot.StateVersion switch
        {
            1 => [new ResolveInitiativeAction()],
            2 => [new ResolveNoObligationNavalConvoyScheduleAction()],
            3 => [new ResolveNoObligationTacticalShippingAction()],
            5 when snapshot.PhaseId == LandPhaseIds.WeatherDetermination =>
                [new ResolveWeatherAction()],
            _ => [],
        };
        return CreateSet(snapshot.CampaignId, snapshot.StateVersion, snapshot.RulesetHash,
            snapshot.SequencePosition.PositionId, CampaignActionAudience.System, candidates);
    }

    private static CampaignLegalActionSet CreateSet(string campaignId, long stateVersion,
        string rulesetHash, string positionId, CampaignActionAudience audience,
        IReadOnlyList<CampaignActionCandidate> candidates) =>
        new(campaignId, stateVersion, rulesetHash, positionId, audience, candidates);

}
