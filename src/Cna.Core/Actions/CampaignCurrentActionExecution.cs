using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Actions;

internal sealed record CampaignCurrentActionExecutionResult
{
    private CampaignCurrentActionExecutionResult(
        object? acceptedEvent,
        CampaignSnapshotV11? successorSnapshot,
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
    public CampaignSnapshotV11? SuccessorSnapshot { get; }
    public CampaignActionAcceptanceReceipt? Receipt { get; }
    public CampaignActionSubmissionRejectionReason RejectionReason { get; }

    public static CampaignCurrentActionExecutionResult Accepted(
        object acceptedEvent,
        CampaignSnapshotV11 successorSnapshot,
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
        CampaignSnapshotV11 snapshot,
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

        var artifact = context.ArtifactV6;
        if (artifact is null
            || !CampaignSnapshotV11Admission.IsValid(snapshot, artifact, context.Scenario))
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
            var campaignEvent = CreateEvent(snapshot, context, candidate, submission.Audience);
            var successor = campaignEvent switch
            {
                CampaignSuccessorEvent value => CampaignV11BreakdownProjector.Apply(
                    snapshot, value, artifact, context.Scenario),
                CampaignEvent value => CampaignV11Preamble.Apply(snapshot, value, artifact, context.Scenario),
                _ => throw new InvalidOperationException("Unsupported current event."),
            };
            if (!CampaignSnapshotV11Admission.IsValid(successor, artifact, context.Scenario))
                return Reject(CampaignActionSubmissionRejectionReason.InvalidAuthority);
            return Complete(snapshot, successor, campaignEvent, submission.Audience, candidate.ActionId);
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidCampaignHistoryException
            or InvalidOperationException)
        {
            return Reject(CampaignActionSubmissionRejectionReason.InvalidAuthority);
        }
    }

    private static object CreateEvent(CampaignSnapshotV11 snapshot, CampaignContentContext context,
        CampaignActionCandidate candidate, CampaignActionAudience audience)
    {
        var artifact = context.ArtifactV6!;
        var scenario = context.Scenario;
        var side = audience switch
        {
            CampaignActionAudience.Axis => LandSide.Axis,
            CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
            _ => (LandSide?)null,
        };
        switch (candidate)
        {
            case MoveElementAction move:
                return CampaignElementMovedV3Factory.Create(snapshot, artifact, scenario,
                    new ElementMovedV3ReplayInput(snapshot.CampaignId, snapshot.StateVersion,
                        CurrentPositionId(snapshot), side!.Value, move.ElementId,
                        move.OriginLocationId, move.DestinationLocationId));
            case StopElementMovementAction stop:
                return CampaignBreakdownLifecycleFactory.CreateStop(snapshot, artifact, scenario,
                    side!.Value, stop.RouteId, stop.ActionId);
            case ResolveBreakdownStopAction resolve:
                return CampaignBreakdownStopResolvedFactory.Create(snapshot, artifact, scenario, resolve.ActionId);
            case CompleteMovementSegmentAction:
                return CampaignBreakdownLifecycleFactory.CreateMovementCompletion(snapshot, artifact, scenario);
            case CompleteBreakdownSegmentAction complete:
                return CampaignBreakdownLifecycleFactory.CreateBreakdownCompletion(snapshot, artifact, scenario, complete.ActionId);
            case MoveReactingElementAction move:
                var projection = Cna.Core.Observations.CampaignObservationV7Projector.ProjectWithAuthority(
                    snapshot, artifact, scenario, side!.Value,
                    new Cna.Core.Observations.CampaignObservationV6AuthorityFacts([], []));
                var alias = projection.ReactionAliases.Single(value => value.PublicId == move.OpportunityId);
                var opportunity = snapshot.ReactionWindow!.FrozenOpportunities.Single(
                    value => value.OpportunityId.Value == alias.AuthorityId).OpportunityId;
                var input = CampaignReactingElementMovedV2Factory.CreateReplayInput(snapshot, artifact,
                    scenario, opportunity, move.DestinationLocationId);
                if (input.ActionId != move.ActionId)
                    throw new InvalidOperationException("Reaction capability differs from authority.");
                return CampaignReactingElementMovedV2Factory.Create(snapshot, artifact, scenario, input);
            case CompleteReactionParticipantAction complete:
                var completionInput = CampaignBreakdownLifecycleFactory.CreateReactionCompletionInput(snapshot, artifact, scenario);
                if (completionInput.ActionId != complete.ActionId)
                    throw new InvalidOperationException("Reaction completion differs from authority.");
                return CampaignBreakdownLifecycleFactory.CreateReactionCompletion(snapshot, artifact, scenario, completionInput);
            case ReactionWindowAction close:
                var reason = close switch
                {
                    DeclineReactionWindowAction => CampaignReactionWindowCloseReason.PlayerDecline,
                    CloseReactionWindowUnavailableAction => CampaignReactionWindowCloseReason.ScriptedUnavailable,
                    CloseReactionWindowTimeoutAction => CampaignReactionWindowCloseReason.Timeout,
                    CloseReactionWindowNoEligibleAction => CampaignReactionWindowCloseReason.NoEligibleReactor,
                    _ => throw new InvalidOperationException("Unknown Reaction close capability."),
                };
                var closeInput = CampaignBreakdownLifecycleFactory.CreateReactionCloseInput(snapshot, artifact, scenario, reason);
                if (closeInput.ActionId != close.ActionId)
                    throw new InvalidOperationException("Reaction close differs from authority.");
                return CampaignBreakdownLifecycleFactory.CreateReactionClose(snapshot, artifact, scenario, closeInput);
            default:
                return CampaignV11Preamble.Create(snapshot, artifact, scenario, candidate);
        }
    }

    private static CampaignCurrentActionExecutionResult Complete(
        CampaignSnapshotV11 prior,
        CampaignSnapshotV11 successor,
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

    internal static string CurrentPositionId(CampaignSnapshotV11 snapshot) =>
        snapshot.CurrentPosition.Kind == CampaignPositionV11Kind.BreakdownStop
            ? "land.position.breakdown-stop"
            : snapshot.CurrentPosition.SequenceContext.PositionId;

    private static CampaignCurrentActionExecutionResult Reject(
        CampaignActionSubmissionRejectionReason reason) =>
        CampaignCurrentActionExecutionResult.Rejected(reason);
}
