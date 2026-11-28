using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Visibility;

namespace Cna.Core.Exercises;

// Successor-turn play (Turn N+1 after End of Turn): Reserve Designation, Movement and Movement completion for the
// first acting side. Every step derives its cut from the latest accepted event and replays from its predecessor
// session; nothing here reads or writes an earlier turn's retained per-stage fields.
public static partial class CampaignExercises
{
    private const string TurnStartReservePosition = "land.position.operation-1.first-player.reserve-designation";

    private static LandSide? OwnerSide(CampaignActionAudience audience) => audience switch
    {
        CampaignActionAudience.Axis => LandSide.Axis,
        CampaignActionAudience.Commonwealth => LandSide.Commonwealth,
        _ => null,
    };

    private static (object Predecessor, CampaignWorldSnapshotV6 World) QueryTurnSuccessorSource(ExerciseSession session)
    {
        if (session.IsTurnSuccessorHead)
            return (session.CurrentHistory[^1], session.CurrentTurnSuccessorWorld
                ?? throw new InvalidOperationException("Successor-turn World is missing."));
        var (repair, world) = QueryTurnStartReserve(session);
        return (repair, world);
    }

    private static (object Event, CampaignWorldSnapshotV6? World, CampaignContactJournal Contacts) ReplayTurnSuccessor(
        ExerciseSession predecessor, object candidate)
    {
        var (source, world) = QueryTurnSuccessorSource(predecessor);
        var orders = TurnStartOrders(predecessor);
        var turn = CampaignGameTurn.CurrentGameTurn(predecessor);
        var version = predecessor.CurrentContacts.AuthorityVersion;
        switch (candidate)
        {
            case CampaignNativeSecondReserveV43 reserve:
                {
                    var (replayed, result) = CampaignNativeSecondReserveV43.Create(source, world, predecessor.Context,
                        orders, reserve.Side, reserve.ElementId, turn, version);
                    return (replayed, result, predecessor.CurrentContacts.AdvanceSecondReserve(replayed));
                }
            case CampaignNativeSecondMoveV44 move:
                {
                    var (replayed, result) = CampaignNativeSecondMoveV44.Create(source, world, predecessor.Context, orders,
                        predecessor.CurrentContacts, move.Side, turn, move.ElementId, move.ActionId,
                        predecessor.CurrentSnapshot.RulesetHash);
                    if (replayed.ReactionWindow is not null)
                        throw new CampaignRuleNotImplementedException("Successor-turn Reaction is not admitted.");
                    return (replayed, result, predecessor.CurrentContacts.AppendNativeSecondMove(world, result, replayed, predecessor.Context));
                }
            case CampaignNativeSecondMovementCompletedV45 completion:
                {
                    var replayed = CampaignNativeSecondMovementCompletedV45.Create(source, world, predecessor.Context,
                        orders, completion.Side, turn, version);
                    return (replayed, world, predecessor.CurrentContacts.AdvanceSecondMovementCompletion(replayed));
                }
            default:
                throw new InvalidCampaignHistoryException("Unsupported successor-turn event.");
        }
    }

    private static ExerciseStepResult AcceptTurnSuccessor(ExerciseSession session, object candidate,
        CampaignActionAudience audience, string actionId)
    {
        var (campaignEvent, world, contacts) = ReplayTurnSuccessor(session, candidate);
        var next = session.WithTurnSuccessorEvent(campaignEvent, world, contacts);
        var bytes = CampaignCurrentEventSerializer.Serialize(campaignEvent, session.Context);
        var checkpoint = QueryCheckpoint(next);
        var receipt = new CampaignActionAcceptanceReceipt(checkpoint.CampaignId, session.CurrentContacts.AuthorityVersion,
            checkpoint.StateVersion, checkpoint.PositionId, audience, actionId);
        return ExerciseStepResult.Accepted(next, new ExerciseStepEvidence(receipt, bytes, bytes));
    }

    private static ExerciseStepResult? SubmitTurnStartReserve(ExerciseSession session, CampaignActionSubmission submission)
    {
        if (session.CurrentHistory.Count == 0 || session.CurrentHistory[^1] is not CampaignNativeNoObligationFleetRepairV54
            || !Cna1979BreakdownSetupCatalog.AdmitsLaterTurnPasses(session.CurrentSnapshot.Setup))
            return null;
        if (!CampaignActionContractValidator.IsValidSubmission(submission))
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        var checkpoint = QueryCheckpoint(session);
        if (checkpoint.PositionId != TurnStartReservePosition) return null;
        if (submission.CampaignId != checkpoint.CampaignId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != checkpoint.StateVersion)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != checkpoint.PositionId)
            return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        try
        {
            var declaration = session.CurrentHistory.OfType<CampaignNativeInitiativeDeclarationV49>().Last();
            if (OwnerSide(submission.Audience) != declaration.FirstSide)
                return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var (_, world) = QueryTurnStartReserve(session);
            var candidates = world.Elements.Select(value => (CampaignActionCandidate)new DesignateReserveAction(value.ElementId))
                .Append(new CompleteReserveDesignationAction())
                .SingleOrDefault(value => value.ActionId == submission.ActionId);
            if (candidates is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
            var side = declaration.FirstSide;
            var (repair, _) = QueryTurnStartReserve(session);
            var (reserve, _) = CampaignNativeSecondReserveV43.Create(repair, world, session.Context, TurnStartOrders(session),
                side, candidates is DesignateReserveAction designated ? designated.ElementId : null,
                CampaignGameTurn.CurrentGameTurn(session), session.CurrentContacts.AuthorityVersion);
            return AcceptTurnSuccessor(session, reserve, submission.Audience, submission.ActionId);
        }
        catch (CampaignRuleNotImplementedException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.RuleNotImplemented); }
        catch (Exception error) when (IsExpectedCampaignException(error))
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
    }

    internal static CampaignContactOwnerMovementCut QueryTurnSuccessorMovementCut(ExerciseSession session,
        LandSide side, string elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (!session.IsTurnSuccessorHead)
            throw new InvalidOperationException("Successor-turn Movement requires a successor-turn cut.");
        var (source, world) = QueryTurnSuccessorSource(session);
        var (_, _, position) = CampaignNativeSecondMoveV44.Source(source, world, session.Context,
            TurnStartOrders(session), side, CampaignGameTurn.CurrentGameTurn(session), session.CurrentContacts.AuthorityVersion);
        return CampaignContactOwnerMovementCut.ProjectSecondMovement(world, position, session.Context,
            session.CurrentContacts, side, elementId);
    }

    internal static ExerciseStepResult SubmitTurnSuccessorMovement(ExerciseSession session,
        CampaignActionAudience audience, string actionId, string elementId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            if (!session.IsTurnSuccessorHead || OwnerSide(audience) is not { } side)
                throw new InvalidOperationException("Successor-turn Movement requires a successor-turn cut and an owner.");
            var (source, world) = QueryTurnSuccessorSource(session);
            var (move, _) = CampaignNativeSecondMoveV44.Create(source, world, session.Context, TurnStartOrders(session),
                session.CurrentContacts, side, CampaignGameTurn.CurrentGameTurn(session), elementId, actionId,
                session.CurrentSnapshot.RulesetHash);
            return AcceptTurnSuccessor(session, move, audience, actionId);
        }
        catch (CampaignRuleNotImplementedException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.RuleNotImplemented); }
        catch (Exception error) when (IsExpectedCampaignException(error))
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
    }

    internal static ExerciseStepResult CompleteTurnSuccessorMovement(ExerciseSession session,
        CampaignActionAudience audience, string actionId)
    {
        ArgumentNullException.ThrowIfNull(session);
        try
        {
            if (!session.IsTurnSuccessorHead || OwnerSide(audience) is not { } side)
                throw new InvalidOperationException("Successor-turn Movement completion requires a successor-turn cut and an owner.");
            var (source, world) = QueryTurnSuccessorSource(session);
            var completed = CampaignNativeSecondMovementCompletedV45.Create(source, world, session.Context,
                TurnStartOrders(session), side, CampaignGameTurn.CurrentGameTurn(session), session.CurrentContacts.AuthorityVersion);
            return AcceptTurnSuccessor(session, completed, audience, actionId);
        }
        catch (CampaignRuleNotImplementedException)
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.RuleNotImplemented); }
        catch (Exception error) when (IsExpectedCampaignException(error))
        { return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidAuthority); }
    }

    private static ExerciseReconstructionResult ReconstructTurnSuccessor(ExerciseSession session,
        bool includeStreamHash, bool predecessorVerified)
    {
        var expectedHash = Hash([]);
        try
        {
            var predecessor = session.TurnBoundaryPredecessor
                ?? throw new InvalidCampaignHistoryException("Missing successor-turn predecessor.");
            if (!predecessorVerified && !Reconstruct(predecessor, includeStreamHash: false).IsVerified)
                throw new InvalidCampaignHistoryException("Successor-turn predecessor did not replay.");
            if (session.CurrentHistory.Count != predecessor.CurrentHistory.Count + 1
                || !session.CurrentHistory.Take(predecessor.CurrentHistory.Count).SequenceEqual(predecessor.CurrentHistory))
                throw new InvalidCampaignHistoryException("Successor-turn history does not extend its predecessor.");
            var candidate = session.CurrentHistory[^1];
            var expected = CampaignCurrentEventSerializer.Serialize(candidate, session.Context);
            expectedHash = Hash(expected);
            var (regenerated, world, contacts) = ReplayTurnSuccessor(predecessor, candidate);
            var replayed = CampaignCurrentEventSerializer.Serialize(regenerated, session.Context);
            if (!expected.AsSpan().SequenceEqual(replayed) || session.CurrentTurnSuccessorWorld != world
                || session.CurrentContacts.AuthorityVersion != contacts.AuthorityVersion
                || session.CurrentContacts.Records.Count != contacts.Records.Count
                || session.CurrentContacts.Records.Zip(contacts.Records).Any(pair => !pair.First.AsSpan().SequenceEqual(pair.Second)))
                throw new InvalidCampaignHistoryException("Successor-turn event differs from replay.");
            var streamHash = includeStreamHash
                ? HashFramed(session.CurrentHistory.Select(value => CampaignCurrentEventSerializer.Serialize(value, session.Context))) : null;
            return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.None,
                replayed, streamHash, expectedHash, Hash(replayed));
        }
        catch (Exception error) when (IsExpectedCampaignException(error) || error is KeyNotFoundException)
        {
            return new ExerciseReconstructionResult(ExerciseReconstructionFailureReason.InvalidHistory,
                null, null, expectedHash, null);
        }
    }
}
