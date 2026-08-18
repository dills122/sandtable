using Cna.Core.Campaigns;
using Cna.Core.Content;
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
        if (!IsValidSubmission(submission))
            return CampaignActionSubmissionResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (!CampaignSnapshotValidator.IsValid(handle.Snapshot, handle.Context))
            return CampaignActionSubmissionResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var snapshot = handle.Snapshot;
        if (!string.Equals(submission.CampaignId, snapshot.CampaignId, StringComparison.Ordinal))
            return CampaignActionSubmissionResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != snapshot.StateVersion)
            return CampaignActionSubmissionResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (!string.Equals(submission.ExpectedPositionId, snapshot.SequencePosition.PositionId,
            StringComparison.Ordinal))
            return CampaignActionSubmissionResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);

        var query = Query(handle, submission.Audience);
        if (!query.IsSuccessful)
            return CampaignActionSubmissionResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var candidate = query.ActionSet!.Candidates.SingleOrDefault(value => string.Equals(
            value.ActionId, submission.ActionId, StringComparison.Ordinal));
        if (candidate is null)
            return CampaignActionSubmissionResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);

        var command = ToCommand(snapshot, submission.Audience, candidate);
        var decision = CampaignEngine.Decide(snapshot, command, handle.Context);
        if (!decision.IsAccepted)
            return CampaignActionSubmissionResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var successor = CampaignProjector.Apply(snapshot, decision.Events[0], handle.Context);
        var receipt = new CampaignActionAcceptanceReceipt(snapshot.CampaignId, snapshot.StateVersion,
            successor.StateVersion, successor.SequencePosition.PositionId, submission.Audience,
            candidate.ActionId);
        return CampaignActionSubmissionResult.Accepted(
            new CampaignAuthorityHandle(successor, handle.Context), receipt);
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
            _ => [],
        };
        return CreateSet(snapshot.CampaignId, snapshot.StateVersion, snapshot.RulesetHash,
            snapshot.SequencePosition.PositionId, CampaignActionAudience.System, candidates);
    }

    private static CampaignLegalActionSet CreateSet(string campaignId, long stateVersion,
        string rulesetHash, string positionId, CampaignActionAudience audience,
        IReadOnlyList<CampaignActionCandidate> candidates) =>
        new(campaignId, stateVersion, rulesetHash, positionId, audience, candidates);

    private static CampaignCommand ToCommand(CampaignSnapshot snapshot,
        CampaignActionAudience audience, CampaignActionCandidate candidate) => candidate switch
        {
            ResolveInitiativeAction when audience == CampaignActionAudience.System =>
                new ResolveInitiative(snapshot.StateVersion, snapshot.SequencePosition.PositionId),
            ResolveNoObligationNavalConvoyScheduleAction when audience == CampaignActionAudience.System =>
                new ResolveNoObligationNavalConvoySchedule(snapshot.StateVersion, snapshot.SequencePosition.PositionId),
            ResolveNoObligationTacticalShippingAction when audience == CampaignActionAudience.System =>
                new ResolveNoObligationTacticalShipping(snapshot.StateVersion, snapshot.SequencePosition.PositionId),
            ActFirstAction first => new DeclareInitiativeOrder(snapshot.StateVersion,
                snapshot.SequencePosition.PositionId, first.OperationStage!.Value, ToSide(audience),
                InitiativeOrderChoice.ActFirst),
            ActLastAction last => new DeclareInitiativeOrder(snapshot.StateVersion,
                snapshot.SequencePosition.PositionId, last.OperationStage!.Value, ToSide(audience),
                InitiativeOrderChoice.ActLast),
            _ => throw new InvalidOperationException("The legal candidate has no command mapping."),
        };

    private static LandSide ToSide(CampaignActionAudience audience) => audience switch
    {
        CampaignActionAudience.Axis => LandSide.Axis,
        CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
        _ => throw new ArgumentOutOfRangeException(nameof(audience)),
    };

    private static bool IsValidSubmission(CampaignActionSubmission submission) =>
        submission.ContractVersion == CampaignActionSubmission.CurrentContractVersion
        && IsStableId(submission.CampaignId)
        && submission.ExpectedStateVersion >= 1
        && IsStableId(submission.ExpectedPositionId)
        && Enum.IsDefined(submission.Audience)
        && submission.ActionId is { Length: 71 }
        && submission.ActionId.StartsWith("sha256:", StringComparison.Ordinal)
        && submission.ActionId[7..].All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static bool IsStableId(string? value)
    {
        try
        {
            _ = ContentContractGuards.RequireStableId(value!, nameof(value));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}
