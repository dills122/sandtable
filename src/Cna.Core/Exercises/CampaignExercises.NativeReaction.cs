using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Exercises;

// E4b (#655) slice R2: native Reaction Query/Submit/Reconstruct over World V6. The turn is always derived
// from accepted history (CampaignGameTurn); no stage or turn literal appears in this file.
public static partial class CampaignExercises
{
    internal static CampaignOperationStageWeather NativeReactionWeather(ExerciseSession session, int gameTurn, int stage)
    {
        if (session.CurrentHistory.OfType<CampaignNativeWeatherDeterminationV50>()
                .LastOrDefault(value => value.GameTurn == gameTurn && value.OperationStage == stage) is { } determined)
            return new CampaignOperationStageWeather(CampaignOperationStageWeather.CurrentContractVersion,
                determined.GameTurn, determined.OperationStage, determined.DeterminingSide, determined.Season,
                determined.FirstDie, determined.SecondDie, determined.Kind, determined.Scope, determined.LocationDie,
                determined.AffectedAreas, 0, 0, 0);
        return session.CurrentSnapshot.OperationStageWeather.SingleOrDefault(value =>
                value.GameTurn == gameTurn && value.OperationStage == stage)
            ?? throw new InvalidOperationException("Reaction requires the accepted Weather of its Operation Stage.");
    }

    private static (NativeReactionCut Cut, CampaignNativeReactionCut Bound, int GameTurn, CampaignOperationStageWeather Weather)
        BindNativeReaction(ExerciseSession session)
    {
        var cut = session.NativeReaction is { IsOpen: true } open ? open
            : throw new InvalidOperationException("No native Reaction window is open.");
        var gameTurn = CampaignGameTurn.CurrentGameTurn(session);
        var weather = NativeReactionWeather(session, gameTurn, cut.Window!.ReactingPosition.SuspendedMovementPosition.OperationStage);
        var bound = CampaignNativeReactionCut.Bind(cut.Last, cut.World, session.Context,
            gameTurn, session.CurrentSnapshot.RulesetHash, weather, cut.WindowId);
        return (cut, bound, gameTurn, weather);
    }

    internal static CampaignLegalActionQueryResult QueryNativeReaction(ExerciseSession session, CampaignActionAudience audience)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!Enum.IsDefined(audience))
            return CampaignLegalActionQueryResult.Rejected(CampaignLegalActionQueryRejectionReason.InvalidAudience);
        try
        {
            var (cut, bound, _, _) = BindNativeReaction(session);
            if (cut.StateVersion != session.CurrentContacts.AuthorityVersion)
                return CampaignLegalActionQueryResult.Rejected(CampaignLegalActionQueryRejectionReason.InvalidState);
            return CampaignLegalActionQueryResult.Success(new CampaignLegalActionSet(session.CurrentSnapshot.CampaignId,
                cut.StateVersion, session.CurrentSnapshot.RulesetHash, cut.PositionId, audience,
                CampaignNativeReactionActions.Derive(bound, audience)));
        }
        catch (Exception error) when (IsExpectedCampaignException(error))
        { return CampaignLegalActionQueryResult.Rejected(CampaignLegalActionQueryRejectionReason.InvalidState); }
    }

    internal static ExerciseStepResult SubmitNativeReaction(ExerciseSession session, CampaignActionSubmission submission)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(submission);
        try
        {
            var (cut, bound, gameTurn, weather) = BindNativeReaction(session);
            if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion
                || !Enum.IsDefined(submission.Audience))
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
            if (submission.CampaignId != session.CurrentSnapshot.CampaignId)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
            if (submission.ExpectedStateVersion != cut.StateVersion || cut.StateVersion != session.CurrentContacts.AuthorityVersion)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
            if (submission.ExpectedPositionId != cut.PositionId)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
            var candidate = CampaignNativeReactionActions.Derive(bound, submission.Audience)
                .SingleOrDefault(value => value.ActionId == submission.ActionId);
            if (candidate is null)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var ruleset = session.CurrentSnapshot.RulesetHash;
            ExerciseSession next;
            byte[] bytes;
            long prior;
            long version;
            if (candidate is MoveReactingElementAction move)
            {
                var (moved, result) = CampaignNativeReactionMoveV59.Create(cut.Last, cut.World, session.Context, gameTurn,
                    ruleset, weather, new(move.WindowId), new(move.OpportunityId), move.DestinationLocationId);
                var contacts = session.CurrentContacts.AppendNativeReactionMove(cut.World, result, moved, session.Context);
                next = session.WithNativeReactionMove(moved, result, contacts);
                (bytes, prior, version) = (moved.SerializeCanonical(), moved.PriorStateVersion, moved.StateVersion);
            }
            else
            {
                var window = new CampaignReactionWindowId(WindowIdOf(candidate));
                var kind = candidate switch
                {
                    DeclineReactionWindowAction => CampaignNativeReactionCloseKind.PlayerDecline,
                    CloseReactionWindowUnavailableAction => CampaignNativeReactionCloseKind.ScriptedUnavailable,
                    CloseReactionWindowTimeoutAction => CampaignNativeReactionCloseKind.Timeout,
                    CloseReactionWindowNoEligibleAction => CampaignNativeReactionCloseKind.NoEligibleReactor,
                    CompleteReactionParticipantAction => CampaignNativeReactionCloseKind.ParticipantCompleted,
                    _ => throw new InvalidOperationException("Unknown native Reaction capability."),
                };
                var opportunity = candidate is CompleteReactionParticipantAction complete ? new CampaignReactionOpportunityId(complete.OpportunityId) : null;
                var closed = CampaignNativeReactionClosedV60.Create(cut.Last, cut.World, session.Context, gameTurn,
                    ruleset, weather, window, kind, opportunity);
                next = session.WithNativeReactionClose(closed, session.CurrentContacts.AdvanceNativeReactionClosed(closed));
                (bytes, prior, version) = (closed.SerializeCanonical(), closed.PriorStateVersion, closed.StateVersion);
            }
            var receipt = new CampaignActionAcceptanceReceipt(session.CurrentSnapshot.CampaignId, prior, version,
                cut.PositionId, submission.Audience, candidate.ActionId);
            return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
        }
        catch (CampaignRuleNotImplementedException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.RuleNotImplemented); }
        catch (Exception error) when (IsExpectedCampaignException(error))
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
    }

    private static string WindowIdOf(CampaignActionCandidate candidate) => candidate switch
    {
        ReactionWindowAction action => action.WindowId,
        CompleteReactionParticipantAction complete => complete.WindowId,
        _ => throw new InvalidOperationException("Capability has no Reaction window."),
    };

    // Replays the V59/V60 at the head of `completedSession` from its predecessor cut.
    private static ExerciseReconstructionResult ReconstructNativeReaction(ExerciseSession completedSession,
        bool includeStreamHash, bool predecessorVerified)
    {
        var last = completedSession.CurrentHistory[^1];
        var expectedHash = string.Empty;
        try
        {
            var expected = CampaignCurrentEventSerializer.Serialize(last, completedSession.Context);
            expectedHash = Hash(expected);
            var predecessor = completedSession.NativeReactionPredecessor
                ?? throw new InvalidCampaignHistoryException("Native Reaction event has no predecessor.");
            if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                throw new InvalidCampaignHistoryException("Native Reaction predecessor did not replay.");
            var (cut, _, gameTurn, weather) = BindNativeReaction(predecessor);
            var ruleset = predecessor.CurrentSnapshot.RulesetHash;
            byte[] bytes;
            CampaignWorldSnapshotV6 world;
            CampaignContactJournal contacts;
            switch (last)
            {
                case CampaignNativeReactionMoveV59 moved:
                    var (replayedMove, result) = CampaignNativeReactionMoveV59.Create(cut.Last, cut.World,
                        predecessor.Context, gameTurn, ruleset, weather, moved.WindowId, moved.OpportunityId,
                        moved.DestinationLocationId);
                    bytes = replayedMove.SerializeCanonical();
                    world = result;
                    contacts = predecessor.CurrentContacts.AppendNativeReactionMove(cut.World, result, replayedMove, predecessor.Context);
                    break;
                case CampaignNativeReactionClosedV60 closed:
                    var replayedClose = CampaignNativeReactionClosedV60.Create(cut.Last, cut.World, predecessor.Context,
                        gameTurn, ruleset, weather, closed.WindowId, closed.CloseKind, closed.OpportunityId);
                    bytes = replayedClose.SerializeCanonical();
                    world = cut.World;
                    contacts = predecessor.CurrentContacts.AdvanceNativeReactionClosed(replayedClose);
                    break;
                default:
                    throw new InvalidCampaignHistoryException("Native Reaction head is not a Reaction event.");
            }
            var current = completedSession.NativeReaction
                ?? throw new InvalidCampaignHistoryException("Native Reaction head lost its window.");
            if (!expected.AsSpan().SequenceEqual(bytes)
                || CampaignNativeCombatPaidWorldV22.Hash(current.World) != CampaignNativeCombatPaidWorldV22.Hash(world)
                || completedSession.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                || completedSession.CurrentContacts.Records.Count != contacts.Records.Count
                || completedSession.CurrentContacts.Records.Zip(contacts.Records)
                    .Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                throw new InvalidCampaignHistoryException("Native Reaction event differs from replay.");
            var streamHash = includeStreamHash
                ? HashFramed(completedSession.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, completedSession.Context))) : null;
            return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                bytes, streamHash, expectedHash, Hash(bytes));
        }
        catch (Exception error) when (error is System.Text.Json.JsonException or InvalidCampaignHistoryException
            or ArgumentException or InvalidOperationException)
        { return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory, null, null, expectedHash, null); }
    }
}
