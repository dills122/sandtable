using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Actions;

internal sealed record CampaignCurrentActionExecutionResult
{
    private CampaignCurrentActionExecutionResult(
        object? acceptedEvent,
        CampaignSnapshotV10? successorSnapshot,
        CampaignActionAcceptanceReceipt? receipt,
        CampaignActionSubmissionRejectionReason rejectionReason)
    {
        AcceptedEvent = acceptedEvent;
        SuccessorSnapshot = successorSnapshot;
        Receipt = receipt;
        RejectionReason = rejectionReason;
    }

    public bool IsAccepted => SuccessorSnapshot is not null;
    public object? AcceptedEvent { get; }
    public CampaignSnapshotV10? SuccessorSnapshot { get; }
    public CampaignActionAcceptanceReceipt? Receipt { get; }
    public CampaignActionSubmissionRejectionReason RejectionReason { get; }

    public static CampaignCurrentActionExecutionResult Accepted(
        object acceptedEvent,
        CampaignSnapshotV10 successorSnapshot,
        CampaignActionAcceptanceReceipt receipt) => new(
        acceptedEvent ?? throw new ArgumentNullException(nameof(acceptedEvent)),
        successorSnapshot ?? throw new ArgumentNullException(nameof(successorSnapshot)),
        receipt ?? throw new ArgumentNullException(nameof(receipt)),
        CampaignActionSubmissionRejectionReason.None);

    public static CampaignCurrentActionExecutionResult Rejected(
        CampaignActionSubmissionRejectionReason reason) => new(null, null, null, reason);
}

internal static class CampaignCurrentActionExecution
{
    public static CampaignCurrentActionExecutionResult Execute(
        CampaignSnapshotV10 snapshot,
        CampaignContentContext context,
        CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(submission);
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
        {
            return Reject(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        }

        var artifact = context.ArtifactV5;
        if (artifact is null
            || !CampaignSnapshotV10Validator.IsValid(snapshot, artifact, context.Scenario))
        {
            return Reject(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }

        if (!string.Equals(submission.CampaignId, snapshot.CampaignId, StringComparison.Ordinal))
        {
            return Reject(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        }

        if (submission.ExpectedStateVersion != snapshot.StateVersion)
        {
            return Reject(CampaignActionSubmissionRejectionReason.StaleState);
        }

        if (!string.Equals(
                submission.ExpectedPositionId,
                CurrentPositionId(snapshot),
                StringComparison.Ordinal))
        {
            return Reject(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        }

        var handle = new CampaignAuthorityHandle(snapshot, context);
        var query = CampaignLegalActions.Query(handle, submission.Audience);
        var candidate = query.ActionSet?.Candidates.SingleOrDefault(value => string.Equals(
            value.ActionId,
            submission.ActionId,
            StringComparison.Ordinal));
        if (candidate is null)
        {
            return Reject(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        }

        try
        {
            var intent = MapSuccessorIntent(handle, submission);
            if (intent is not null)
            {
                return ExecuteSuccessor(
                    snapshot,
                    context,
                    submission.Audience,
                    candidate,
                    intent);
            }

            var legacy = CampaignV10LegacyBridge.ToLegacy(snapshot, context);
            var execution = CampaignActionExecution.Execute(legacy, context, submission);
            if (!execution.IsAccepted)
            {
                return Reject(execution.RejectionReason);
            }

            var successor = CampaignV10LegacyBridge.FromLegacy(
                snapshot,
                execution.SuccessorSnapshot!,
                context);
            return Complete(
                snapshot,
                successor,
                execution.AcceptedEvent!,
                submission.Audience,
                candidate.ActionId);
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidCampaignHistoryException
            or InvalidOperationException)
        {
            return Reject(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static CampaignObservationV6ActionIntent? MapSuccessorIntent(
        CampaignAuthorityHandle handle,
        CampaignActionSubmission submission)
    {
        var snapshot = handle.CurrentSnapshot
            ?? throw new InvalidOperationException("Current action mapping requires Snapshot 10.");
        if (snapshot.ReactionWindow is null
            && snapshot.CurrentPosition.SequencePosition?.SegmentId
                != LandSegmentIds.Movement)
        {
            return null;
        }

        var observer = submission.Audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            CampaignActionAudience.System when snapshot.ReactionWindow is not null =>
                snapshot.ReactionWindow.ReactingSide,
            _ => (LandSide?)null,
        };
        return observer is null
            ? null
            : CampaignObservationV6ActionDerivation.MapSubmission(
                CampaignLegalActions.ProjectV6(handle, observer.Value),
                submission);
    }

    private static CampaignCurrentActionExecutionResult ExecuteSuccessor(
        CampaignSnapshotV10 snapshot,
        CampaignContentContext context,
        CampaignActionAudience audience,
        CampaignActionCandidate candidate,
        CampaignObservationV6ActionIntent intent)
    {
        var artifact = context.ArtifactV5!;
        object campaignEvent;
        CampaignSnapshotV10 successor;
        switch (intent)
        {
            case MoveElementV6Intent move:
                var moved = CampaignElementMovedV2Factory.Create(
                    snapshot,
                    artifact,
                    context.Scenario,
                    new ElementMovedV2ReplayInput(
                        snapshot.CampaignId,
                        move.ExpectedStateVersion,
                        move.ExpectedPositionId,
                        move.Side,
                        move.ElementId,
                        move.OriginLocationId,
                        move.DestinationLocationId));
                campaignEvent = moved;
                successor = CampaignV10Projector.ApplyMovement(
                    snapshot,
                    moved,
                    artifact,
                    context.Scenario);
                break;
            case MoveReactingElementIntent reactionMove:
                var reactingMoved = CampaignReactionParticipantEventFactory.CreateMove(
                    snapshot,
                    artifact,
                    context.Scenario,
                    reactionMove);
                campaignEvent = reactingMoved;
                successor = CampaignV10Projector.ApplyReactionMove(
                    snapshot,
                    reactingMoved,
                    artifact,
                    context.Scenario);
                break;
            case CompleteReactionParticipantIntent completion:
                var completed = CampaignReactionParticipantEventFactory.CreateCompletion(
                    snapshot,
                    artifact,
                    context.Scenario,
                    completion);
                campaignEvent = completed;
                successor = CampaignV10Projector.ApplyReactionCompletion(
                    snapshot,
                    completed,
                    artifact,
                    context.Scenario);
                break;
            case CloseReactionWindowIntent close:
                var closed = CampaignReactionWindowClosedFactory.Create(
                    snapshot,
                    artifact,
                    context.Scenario,
                    close);
                campaignEvent = closed;
                successor = CampaignV10Projector.ApplyReactionClose(
                    snapshot,
                    closed,
                    artifact,
                    context.Scenario);
                break;
            case CompleteMovementSegmentV6Intent:
                var legacy = CampaignV10LegacyBridge.ToLegacy(snapshot, context);
                var submission = new CampaignActionSubmission(
                    CampaignActionSubmission.CurrentContractVersion,
                    snapshot.CampaignId,
                    snapshot.StateVersion,
                    CurrentPositionId(snapshot),
                    audience,
                    candidate.ActionId);
                var execution = CampaignActionExecution.Execute(legacy, context, submission);
                if (!execution.IsAccepted)
                {
                    return Reject(execution.RejectionReason);
                }

                campaignEvent = execution.AcceptedEvent!;
                successor = CampaignV10LegacyBridge.FromLegacy(
                    snapshot,
                    execution.SuccessorSnapshot!,
                    context);
                break;
            default:
                return Reject(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        }

        return Complete(snapshot, successor, campaignEvent, audience, candidate.ActionId);
    }

    private static CampaignCurrentActionExecutionResult Complete(
        CampaignSnapshotV10 prior,
        CampaignSnapshotV10 successor,
        object campaignEvent,
        CampaignActionAudience audience,
        string actionId) => CampaignCurrentActionExecutionResult.Accepted(
        campaignEvent,
        successor,
        new CampaignActionAcceptanceReceipt(
            prior.CampaignId,
            prior.StateVersion,
            successor.StateVersion,
            CurrentPositionId(successor),
            audience,
            actionId));

    internal static string CurrentPositionId(CampaignSnapshotV10 snapshot) =>
        snapshot.CurrentPosition.SequencePosition?.PositionId
        ?? snapshot.CurrentPosition.ReactingPosition!.SuspendedMovementPosition.PositionId;

    private static CampaignCurrentActionExecutionResult Reject(
        CampaignActionSubmissionRejectionReason reason) =>
        CampaignCurrentActionExecutionResult.Rejected(reason);
}
