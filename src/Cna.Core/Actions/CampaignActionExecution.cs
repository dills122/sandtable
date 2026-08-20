using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Actions;

internal sealed record CampaignActionExecutionResult
{
    private CampaignActionExecutionResult(
        CampaignEvent? acceptedEvent,
        CampaignSnapshot? successorSnapshot,
        CampaignActionAcceptanceReceipt? receipt,
        CampaignActionSubmissionRejectionReason rejectionReason)
    {
        AcceptedEvent = acceptedEvent;
        SuccessorSnapshot = successorSnapshot;
        Receipt = receipt;
        RejectionReason = rejectionReason;
    }

    public bool IsAccepted => SuccessorSnapshot is not null;
    public CampaignEvent? AcceptedEvent { get; }
    public CampaignSnapshot? SuccessorSnapshot { get; }
    public CampaignActionAcceptanceReceipt? Receipt { get; }
    public CampaignActionSubmissionRejectionReason RejectionReason { get; }

    public static CampaignActionExecutionResult Accepted(
        CampaignEvent acceptedEvent,
        CampaignSnapshot successorSnapshot,
        CampaignActionAcceptanceReceipt receipt) =>
        new(
            acceptedEvent ?? throw new ArgumentNullException(nameof(acceptedEvent)),
            successorSnapshot ?? throw new ArgumentNullException(nameof(successorSnapshot)),
            receipt ?? throw new ArgumentNullException(nameof(receipt)),
            CampaignActionSubmissionRejectionReason.None);

    public static CampaignActionExecutionResult Rejected(
        CampaignActionSubmissionRejectionReason rejectionReason)
    {
        if (rejectionReason == CampaignActionSubmissionRejectionReason.None)
            throw new ArgumentOutOfRangeException(nameof(rejectionReason));
        return new CampaignActionExecutionResult(null, null, null, rejectionReason);
    }
}

internal static class CampaignActionExecution
{
    public static CampaignActionExecutionResult Execute(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(submission);
        if (!IsValidSubmission(submission))
            return CampaignActionExecutionResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (!CampaignSnapshotValidator.IsValid(snapshot, context))
            return CampaignActionExecutionResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        if (!string.Equals(submission.CampaignId, snapshot.CampaignId, StringComparison.Ordinal))
            return CampaignActionExecutionResult.Rejected(
                CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != snapshot.StateVersion)
            return CampaignActionExecutionResult.Rejected(
                CampaignActionSubmissionRejectionReason.StaleState);
        if (!string.Equals(
                submission.ExpectedPositionId,
                snapshot.SequencePosition.PositionId,
                StringComparison.Ordinal))
            return CampaignActionExecutionResult.Rejected(
                CampaignActionSubmissionRejectionReason.UnexpectedPosition);

        var handle = new CampaignAuthorityHandle(snapshot, context);
        var query = CampaignLegalActions.Query(handle, submission.Audience);
        if (!query.IsSuccessful)
            return CampaignActionExecutionResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
        var candidate = query.ActionSet!.Candidates.SingleOrDefault(value => string.Equals(
            value.ActionId,
            submission.ActionId,
            StringComparison.Ordinal));
        if (candidate is null)
            return CampaignActionExecutionResult.Rejected(
                CampaignActionSubmissionRejectionReason.ActionNotLegal);

        var command = ToCommand(snapshot, submission.Audience, candidate);
        var decision = CampaignEngine.Decide(snapshot, command, context);
        return decision.IsAccepted
            ? Complete(snapshot, context, submission.Audience, candidate, decision.Events)
            : CampaignActionExecutionResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);
    }

    internal static CampaignActionExecutionResult Complete(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        CampaignActionAudience audience,
        CampaignActionCandidate candidate,
        IReadOnlyList<CampaignEvent> events)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(events);
        if (events.Count != 1)
            return CampaignActionExecutionResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidAuthority);

        var acceptedEvent = events[0];
        var successor = CampaignProjector.Apply(snapshot, acceptedEvent, context);
        var receipt = new CampaignActionAcceptanceReceipt(
            snapshot.CampaignId,
            snapshot.StateVersion,
            successor.StateVersion,
            successor.SequencePosition.PositionId,
            audience,
            candidate.ActionId);
        return CampaignActionExecutionResult.Accepted(acceptedEvent, successor, receipt);
    }

    private static CampaignCommand ToCommand(
        CampaignSnapshot snapshot,
        CampaignActionAudience audience,
        CampaignActionCandidate candidate) => candidate switch
        {
            ResolveInitiativeAction when audience == CampaignActionAudience.System =>
                new ResolveInitiative(snapshot.StateVersion, snapshot.SequencePosition.PositionId),
            ResolveNoObligationNavalConvoyScheduleAction when audience == CampaignActionAudience.System =>
                new ResolveNoObligationNavalConvoySchedule(
                    snapshot.StateVersion,
                    snapshot.SequencePosition.PositionId),
            ResolveNoObligationTacticalShippingAction when audience == CampaignActionAudience.System =>
                new ResolveNoObligationTacticalShipping(
                    snapshot.StateVersion,
                    snapshot.SequencePosition.PositionId),
            ResolveWeatherAction when audience == CampaignActionAudience.System =>
                new ResolveWeather(snapshot.StateVersion, snapshot.SequencePosition.PositionId),
            ActFirstAction first => new DeclareInitiativeOrder(
                snapshot.StateVersion,
                snapshot.SequencePosition.PositionId,
                first.OperationStage!.Value,
                ToSide(audience),
                InitiativeOrderChoice.ActFirst),
            ActLastAction last => new DeclareInitiativeOrder(
                snapshot.StateVersion,
                snapshot.SequencePosition.PositionId,
                last.OperationStage!.Value,
                ToSide(audience),
                InitiativeOrderChoice.ActLast),
            _ => throw new InvalidOperationException(
                "The legal candidate has no command mapping."),
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
        && submission.ActionId[7..].All(character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');

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
