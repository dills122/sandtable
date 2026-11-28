using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Exercises.Sequencing;
using Cna.Core.Rules;
using Cna.Core.Hosting;
using Cna.Core.Tests.Exercises.Sequencing;

namespace Cna.Core.Tests.Exercises;

// E4b (#655) slice R2: session + NativeReactionPending + native reaction actions/query/submit/reconstruct.
// The five stage refusals stay in place, so later-stage windows are opened by retaining a V44 directly.
public sealed class NativeReactionPendingTests
{
    public static TheoryData<int, bool> Slots => new()
    {
        { 1, true }, { 2, false }, { 2, true }, { 3, false }, { 3, true },
    };

    private static readonly Lazy<IReadOnlyList<SequencingSample>> Samples = new(
        () => SequencingScenarios.DriveReactionRefusalFixture().Samples);

    private sealed record Opened(ExerciseSession Before, ExerciseSession Window, CampaignNativeSecondMoveV44 Trigger,
        CampaignActionAudience Reactor, CampaignActionAudience Phasing, string ElementId, string ActionId);

    private static CampaignActionAudience Audience(LandSide side) =>
        side == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;

    private static Opened Open(int stage, bool second)
    {
        var session = Samples.Value.Single(sample => sample.Label == $"op{stage}.{(second ? "second" : "first")}.reserve").Session;
        var reserve = Assert.IsType<CampaignNativeSecondReserveV43>(session.CurrentHistory[^1]);
        var audience = Audience(reserve.Side);
        var element = reserve.Side == LandSide.Axis ? "axis-infantry" : "commonwealth-infantry";
        var cut = (stage, second) switch
        {
            (1, _) => CampaignExercises.QuerySecondMovementCut(session, reserve.Side, element),
            (2, false) => CampaignExercises.QueryStageEntryMovementCut(session, reserve.Side, element),
            (2, true) => CampaignExercises.QueryStageEntrySecondMovementCut(session, reserve.Side, element),
            (3, false) => CampaignExercises.QueryThirdStageMovementCut(session, reserve.Side, element),
            _ => CampaignExercises.QueryThirdStageSecondMovementCut(session, reserve.Side, element),
        };
        var action = CampaignNativeSecondMoveV44.ActionIdFor(Assert.Single(cut.MovementOffers, value => value.DestinationLocationId == "north"));
        var (source, world) = (stage, second) switch
        {
            (1, _) => CampaignExercises.QuerySecondMovementSource(session),
            (2, false) => CampaignExercises.QueryStageEntryMovementSource(session),
            (2, true) => CampaignExercises.QueryStageEntrySecondMovementSource(session),
            (3, false) => CampaignExercises.QueryThirdStageMovementSource(session),
            _ => CampaignExercises.QueryThirdStageSecondMovementSource(session),
        };
        var orders = stage switch
        {
            1 => session.CurrentSnapshot.OperationStageOrders,
            2 => CampaignExercises.StageEntryOrders(session),
            _ => CampaignExercises.ThirdStageOrders(session),
        };
        var (trigger, projected) = CampaignNativeSecondMoveV44.Create(source, world, session.Context, orders,
            session.CurrentContacts, reserve.Side, CampaignGameTurn.CurrentGameTurn(session), element, action,
            session.CurrentSnapshot.RulesetHash);
        Assert.NotNull(trigger.ReactionWindow);
        var contacts = session.CurrentContacts.AppendNativeSecondMove(world, projected, trigger, session.Context);
        var detached = (stage, second) switch
        {
            (1, _) => session.WithSecondMove(trigger, projected, contacts),
            (2, false) => session.WithStageEntryMove(trigger, projected, contacts),
            (2, true) => session.WithStageEntrySecondMove(trigger, projected, contacts),
            (3, false) => session.WithThirdStageMove(trigger, projected, contacts),
            _ => session.WithThirdStageSecondMove(trigger, projected, contacts),
        };
        return new(session, detached, trigger, Audience(trigger.ReactionWindow!.ReactingSide), audience, element, action);
    }

    private static CampaignActionSubmission Submission(ExerciseSession session, CampaignActionAudience audience, CampaignActionCandidate action)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new(1, checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience, action.ActionId);
    }

    private static ExerciseSession Accept(ExerciseSession session, CampaignActionAudience audience, CampaignActionCandidate action)
    {
        var result = CampaignExercises.SubmitNativeReaction(session, Submission(session, audience, action));
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        Assert.NotNull(result.Evidence);
        Assert.Equal(CampaignExercises.QueryCheckpoint(result.SuccessorSession!).StateVersion, result.Evidence!.Receipt.CommittedStateVersion);
        return result.SuccessorSession!;
    }

    private static CampaignActionCandidate[] Candidates(ExerciseSession session, CampaignActionAudience audience)
    {
        var query = CampaignExercises.QueryNativeReaction(session, audience);
        Assert.True(query.IsSuccessful);
        return [.. query.ActionSet!.Candidates];
    }

    private static ExerciseStepResult CompleteMovement(ExerciseSession session, int stage, bool second, CampaignActionAudience phasing)
    {
        var id = SequencingScenarios.Hash("movement-completion");
        return (stage, second) switch
        {
            (1, _) => CampaignExercises.CompleteSecondMovement(session, phasing, id),
            (2, false) => CampaignExercises.CompleteStageEntryMovement(session, phasing, id),
            (2, true) => CampaignExercises.CompleteStageEntrySecondMovement(session, phasing, id),
            (3, false) => CampaignExercises.CompleteThirdStageMovement(session, phasing, id),
            _ => CampaignExercises.CompleteThirdStageSecondMovement(session, phasing, id),
        };
    }

    private static ExerciseStepResult CompleteBreakdown(ExerciseSession session, int stage, bool second) => (stage, second) switch
    {
        (1, _) => CampaignExercises.CompleteSecondBreakdown(session),
        (2, false) => CampaignExercises.CompleteStageEntryBreakdown(session),
        (2, true) => CampaignExercises.CompleteStageEntrySecondBreakdown(session),
        (3, false) => CampaignExercises.CompleteThirdStageBreakdown(session),
        _ => CampaignExercises.CompleteThirdStageSecondBreakdown(session),
    };

    private static void AssertEveryPrefixReplays(ExerciseSession head)
    {
        for (var cursor = head; cursor is not null; cursor = cursor.ReconstructionPending?.Predecessor)
            Assert.True(CampaignExercises.Reconstruct(cursor).IsVerified);
    }

    [Theory]
    [MemberData(nameof(Slots))]
    public void OpenWindowIsFirstPendingOwnedByTheReactingSide(int stage, bool second)
    {
        var opened = Open(stage, second);
        var window = opened.Trigger.ReactionWindow!;
        var pending = Assert.IsType<NativeReactionPending>(opened.Window.Pending);
        Assert.Same(opened.Before, pending.Predecessor);
        Assert.IsNotType<NativeReactionPending>(opened.Window.StagePending);
        Assert.NotEqual(Audience(window.PhasingSide), opened.Reactor);
        Assert.Equal(opened.Trigger.StateVersion, CampaignExercises.QueryCheckpoint(opened.Window).StateVersion);
        Assert.Equal(opened.Trigger.PositionId, CampaignExercises.QueryCheckpoint(opened.Window).PositionId);
        Assert.True(CampaignExercises.Reconstruct(opened.Window).IsVerified);
        // Pending slot is derived from the retained window, never from a stage literal.
        var position = window.ReactingPosition.SuspendedMovementPosition;
        Assert.Equal(stage, position.OperationStage);
        Assert.Equal(second ? LandActorRole.SecondActingSide : LandActorRole.FirstActingSide, position.ActorRole);

        var reactor = Candidates(opened.Window, opened.Reactor);
        Assert.Contains(reactor, value => value is DeclineReactionWindowAction);
        Assert.Contains(reactor, value => value is MoveReactingElementAction);
        Assert.DoesNotContain(reactor, value => value is CompleteReactionParticipantAction);
        // Phasing seat sees only a waiting state: no hidden opportunity list (ZOR-DEC-004).
        Assert.Empty(Candidates(opened.Window, opened.Phasing));
        var system = Candidates(opened.Window, CampaignActionAudience.System);
        Assert.Contains(system, value => value is CloseReactionWindowUnavailableAction);
        Assert.Contains(system, value => value is CloseReactionWindowTimeoutAction);
        // Movement completion is refused while the window is open (V44.Source needs a closing V60).
        Assert.False(CompleteMovement(opened.Window, stage, second, opened.Phasing).IsAccepted);
    }

    [Theory]
    [MemberData(nameof(Slots))]
    public void HostNativeReactionOpeningAndAcceptedKindReplayAreSlotIndependent(int stage, bool second)
    {
        var opened = Open(stage, second);
        var stages = CampaignOpenSeatCatalog.CandidatesFor(opened.Window);
        Assert.False(stages.IsLegacyChain);
        Assert.Equal(["reaction"], stages.Stages);
        var draft = CampaignHostAdapter.QueryReactionOwnerDraft(opened.Window, opened.Reactor);
        Assert.Equal(opened.Trigger.StateVersion, draft.AuthorityVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignHostAdapter.QueryReactionOwnerDraft(opened.Window, opened.Phasing));
        var decline = Candidates(opened.Window, opened.Reactor).OfType<DeclineReactionWindowAction>().Single();
        Assert.Contains(decline.ActionId, draft.PrivateActionIds);
        var submission = Submission(opened.Window, opened.Reactor, decline);
        var result = CampaignHostAdapter.SubmitReaction(opened.Window, submission);
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        var evidence = result.Evidence!;
        CampaignHostAcceptedStep Step(string kind) => new(CampaignActionSubmissionSerializer.SerializeCanonical(submission),
            evidence.EventRecords, CampaignActionAcceptanceReceiptSerializer.Serialize(evidence.Receipt),
            evidence.SnapshotCheckpoint, kind, "land.reaction");
        var native = Step(CampaignHostAcceptedStep.NativeReactionKind);
        Assert.Equal(150, native.ContractVersion);
        var replayed = CampaignHostAdapter.ImportResumed(opened.Window, [native]);
        Assert.True(replayed.IsVerified, replayed.RejectionReason.ToString());
        Assert.Equal(CampaignExercises.Reconstruct(result.SuccessorSession!).EventStreamHash,
            CampaignExercises.Reconstruct(replayed.Session!).EventStreamHash);
        var legacy = Step(CampaignHostAcceptedStep.ContactReactionKind);
        Assert.Equal(50, legacy.ContractVersion);
        Assert.False(CampaignHostAdapter.ImportResumed(opened.Window, [legacy]).IsVerified);
    }

    [Theory]
    [MemberData(nameof(Slots))]
    public void DeclineReturnsTheStagePendingThenMovementAndBreakdownComplete(int stage, bool second)
    {
        var opened = Open(stage, second);
        var decline = Candidates(opened.Window, opened.Reactor).OfType<DeclineReactionWindowAction>().Single();
        // Wrong seat, stale version and an unlisted action are all refused without advancing authority.
        var wrongSeat = CampaignExercises.SubmitNativeReaction(opened.Window,
            Submission(opened.Window, opened.Phasing, decline));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, wrongSeat.RejectionReason);
        var stale = CampaignExercises.SubmitNativeReaction(opened.Window,
            Submission(opened.Window, opened.Reactor, decline) with { ExpectedStateVersion = opened.Trigger.StateVersion - 1 });
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, stale.RejectionReason);

        var closedSession = Accept(opened.Window, opened.Reactor, decline);
        var closed = Assert.IsType<CampaignNativeReactionClosedV60>(closedSession.CurrentHistory[^1]);
        Assert.Null(closed.WindowAfter);
        Assert.Equal(CampaignNativeReactionCloseKind.PlayerDecline, closed.CloseKind);
        Assert.IsNotType<NativeReactionPending>(closedSession.Pending);
        Assert.Equal(opened.Trigger.PositionId, closed.ResumePositionId);
        Assert.Equal(closed.StateVersion, closedSession.CurrentContacts.AuthorityVersion);
        Assert.Equal(closed.StateVersion, CampaignExercises.QueryCheckpoint(closedSession).StateVersion);
        Assert.False(CampaignExercises.SubmitNativeReaction(closedSession, Submission(closedSession, opened.Reactor, decline)).IsAccepted);

        var completed = CompleteMovement(closedSession, stage, second, opened.Phasing);
        Assert.True(completed.IsAccepted, completed.RejectionReason.ToString());
        var breakdown = CompleteBreakdown(completed.SuccessorSession!, stage, second);
        Assert.True(breakdown.IsAccepted, breakdown.RejectionReason.ToString());
        AssertEveryPrefixReplays(breakdown.SuccessorSession!);
    }

    [Theory]
    [MemberData(nameof(Slots))]
    public void ReactorMoveAdvancesContactAuthorityAndCompletesThroughV60(int stage, bool second)
    {
        var opened = Open(stage, second);
        var move = Candidates(opened.Window, opened.Reactor).OfType<MoveReactingElementAction>().First();
        var moved = Accept(opened.Window, opened.Reactor, move);
        var v59 = Assert.IsType<CampaignNativeReactionMoveV59>(moved.CurrentHistory[^1]);
        // Design risk 2: contact authority must advance with the move and bind the post-move World.
        Assert.Equal(v59.StateVersion, moved.CurrentContacts.AuthorityVersion);
        Assert.Equal(v59.StateVersion, CampaignExercises.QueryCheckpoint(moved).StateVersion);
        Assert.NotSame(opened.Window.CurrentContacts, moved.CurrentContacts);
        Assert.Equal(v59.ResultWorldHash, CampaignNativeCombatPaidWorldV22.Hash(MovementWorld(moved, stage, second)));
        Assert.NotEqual(v59.PriorWorldHash, v59.ResultWorldHash);
        Assert.IsType<NativeReactionPending>(moved.Pending);
        Assert.True(CampaignExercises.Reconstruct(moved).IsVerified);
        Assert.Throws<InvalidOperationException>(() => opened.Window.WithNativeReactionMove(v59,
            MovementWorld(moved, stage, second), opened.Window.CurrentContacts));
        // Only the active participant may continue: complete it, others stay frozen.
        var active = Candidates(moved, opened.Reactor);
        Assert.Contains(active, value => value is CompleteReactionParticipantAction);
        Assert.DoesNotContain(active, value => value is DeclineReactionWindowAction);

        var closedSession = Accept(moved, opened.Reactor, active.OfType<CompleteReactionParticipantAction>().Single());
        var closed = Assert.IsType<CampaignNativeReactionClosedV60>(closedSession.CurrentHistory[^1]);
        Assert.Equal(CampaignNativeReactionCloseKind.ParticipantCompleted, closed.CloseKind);
        Assert.Null(closed.WindowAfter);
        Assert.IsNotType<NativeReactionPending>(closedSession.Pending);
        Assert.Equal(CampaignNativeCombatPaidWorldV22.Hash(MovementWorld(moved, stage, second)),
            CampaignNativeCombatPaidWorldV22.Hash(MovementWorld(closedSession, stage, second)));
        var completed = CompleteMovement(closedSession, stage, second, opened.Phasing);
        Assert.True(completed.IsAccepted, completed.RejectionReason.ToString());
        var movementCompletion = Assert.IsType<CampaignNativeSecondMovementCompletedV45>(completed.SuccessorSession!.CurrentHistory[^1]);
        Assert.Equal(CampaignNativeCombatPaidWorldV22.Hash(MovementWorld(closedSession, stage, second)), movementCompletion.WorldHash);
        Assert.Equal(closed.StateVersion, movementCompletion.PriorStateVersion);
        var breakdown = CompleteBreakdown(completed.SuccessorSession!, stage, second);
        Assert.True(breakdown.IsAccepted, breakdown.RejectionReason.ToString());
        AssertEveryPrefixReplays(breakdown.SuccessorSession!);
    }

    private static CampaignWorldSnapshotV6 MovementWorld(ExerciseSession session, int stage, bool second) => (stage, second) switch
    {
        (1, _) => session.CurrentSecondMoveWorld!,
        (2, false) => session.CurrentStageEntryMoveWorld!,
        (2, true) => session.CurrentStageEntrySecondMoveWorld!,
        (3, false) => session.CurrentThirdStageMoveWorld!,
        _ => session.CurrentThirdStageSecondMoveWorld!,
    };

    [Theory]
    [MemberData(nameof(Slots))]
    public void ScriptedClosuresAndTamperedHistoryAreHandled(int stage, bool second)
    {
        var opened = Open(stage, second);
        foreach (var kind in new[] { typeof(CloseReactionWindowTimeoutAction), typeof(CloseReactionWindowUnavailableAction) })
        {
            var action = Candidates(opened.Window, CampaignActionAudience.System).Single(value => value.GetType() == kind);
            var closedSession = Accept(opened.Window, CampaignActionAudience.System, action);
            Assert.IsNotType<NativeReactionPending>(closedSession.Pending);
            Assert.True(CampaignExercises.Reconstruct(closedSession).IsVerified);
        }
        var decline = Candidates(opened.Window, opened.Reactor).OfType<DeclineReactionWindowAction>().Single();
        var declined = Accept(opened.Window, opened.Reactor, decline);
        var v60 = Assert.IsType<CampaignNativeReactionClosedV60>(declined.CurrentHistory[^1]);
        var tampered = SessionTamper.Clone(declined);
        SessionTamper.Set(tampered, nameof(ExerciseSession.CurrentHistory),
            declined.CurrentHistory.SetItem(declined.CurrentHistory.Count - 1, v60 with { ResumePositionId = "land.position.wrong" }));
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, CampaignExercises.Reconstruct(tampered).FailureReason);
        var reordered = SessionTamper.Clone(declined);
        SessionTamper.Set(reordered, nameof(ExerciseSession.NativeReactionPredecessor), null);
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, CampaignExercises.Reconstruct(reordered).FailureReason);
    }

    [Fact]
    public void LegacyStageOneSecondSideWindowReplaysThroughTheRealSubmitPath()
    {
        // The stalled first-side-acting prefix: a window already committed by the accepted V44 contract.
        var opened = Open(1, true);
        var session = Samples.Value.Single(sample => sample.Label == "op1.second.reserve").Session;
        var committed = CampaignExercises.SubmitSecondMovement(session, opened.Phasing, opened.ActionId, opened.ElementId);
        Assert.True(committed.IsAccepted, committed.RejectionReason.ToString());
        var stalled = committed.SuccessorSession!;
        Assert.IsType<NativeReactionPending>(stalled.Pending);
        Assert.Equal(committed.Evidence!.EventRecords.Single(), CampaignCurrentEventSerializer.Serialize(stalled.CurrentHistory[^1], stalled.Context));
        Assert.True(CampaignExercises.Reconstruct(stalled).IsVerified);
        var decline = Candidates(stalled, opened.Reactor).OfType<DeclineReactionWindowAction>().Single();
        var resumed = Accept(stalled, opened.Reactor, decline);
        Assert.True(CampaignExercises.CompleteSecondMovement(resumed, opened.Phasing, SequencingScenarios.Hash("resume")).IsAccepted);
        AssertEveryPrefixReplays(resumed);
    }
}
