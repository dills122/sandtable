using System.Reflection;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.Core.Tests.Exercises.Sequencing;

namespace Cna.Core.Tests.Exercises;

// Stage 3 chain, S3-6..S3-8: Operation Stage 3's own SECOND acting side's Reserve Designation,
// Movement + Movement Completion and Breakdown, entered from the first side's terminal Post Combat Pass.
// Sessions come from the shared real-session scenario (SequencingScenarios, "pregame-no-attack").
// What this proves: Reconstruct verifies and tamper-checks Reserve, Movement Completion and Breakdown
// (the observable subtypes); QueryCheckpoint names a Stage 3 position; the first side's Stage 3 slots are
// untouched; Combat entry points stay closed until Breakdown. What it does NOT prove: ThirdStageSecondMovePending (no own
// element has a legal movement offer in the only drivable setup, same limitation as S3-2/StageEntry*Move),
// a real attack, repeat cycle, or any Host/Runner flow. The finish-without-attack tail is driven.
public sealed class CampaignStageThreeSecondSideTests
{
    private static readonly Lazy<IReadOnlyList<SequencingSample>> Samples = new(() =>
        SequencingScenarios.All.Single(r => r.Samples[0].Scenario == "pregame-no-attack").Samples);

    private static ExerciseSession At(string label) => Samples.Value.Single(s => s.Label == label).Session;

    private static ExerciseSession AfterFirstSide => At("op3.first.post-combat-pass-3");

    private static ExerciseSession Reserve => At("op3.second.reserve");
    private static ExerciseSession Completion => At("op3.second.movement-completion");
    private static ExerciseSession Breakdown => At("op3.second.breakdown");

    public static TheoryData<string> Subtypes() =>
    [
        nameof(ThirdStageSecondReservePending),
        nameof(ThirdStageSecondMovementCompletionPending),
        nameof(ThirdStageSecondBreakdownPending),
    ];

    private static ExerciseSession SessionFor(string name) => name switch
    {
        nameof(ThirdStageSecondReservePending) => Reserve,
        nameof(ThirdStageSecondMovementCompletionPending) => Completion,
        nameof(ThirdStageSecondBreakdownPending) => Breakdown,
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    [Fact]
    public void FixtureLabelsExist()
    {
        Assert.NotNull(Reserve);
        Assert.NotNull(Completion);
        Assert.NotNull(Breakdown);
    }

    [Theory]
    [MemberData(nameof(Subtypes))]
    public void ReconstructVerifiesEachSubtypeAndQueryCheckpointNamesStageThree(string typeName)
    {
        var session = SessionFor(typeName);
        Assert.Equal(typeName, session.Pending!.GetType().Name);
        var result = CampaignExercises.Reconstruct(session);
        Assert.True(result.IsVerified, typeName + ": " + result.FailureReason);
        Assert.Equal(CampaignCurrentEventSerializer.Serialize(session.CurrentHistory[^1], session.Context),
            result.ReconstructedSnapshotBytes);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        Assert.Contains("operation-3", checkpoint.PositionId, StringComparison.Ordinal);
        Assert.Equal(session.CurrentContacts.AuthorityVersion, checkpoint.StateVersion);
    }

    [Theory]
    [MemberData(nameof(Subtypes))]
    public void OneByteTamperOfTheOwnRecordReturnsInvalidHistoryNotAnException(string typeName)
    {
        var (tampered, property) = SessionTamper.TamperOwnRecord(SessionFor(typeName));
        Assert.NotNull(tampered);
        ExerciseReconstructionResult result = null!;
        Assert.Null(Record.Exception(() => result = CampaignExercises.Reconstruct(tampered)));
        Assert.False(result.IsVerified, typeName + " tampered " + property + " still verified");
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, result.FailureReason);
    }

    // A tampered second-side Reserve is caught while verifying the Breakdown head.
    [Fact]
    public void TamperingTheSecondSideReserveIsCaughtFromTheBreakdownHead()
    {
        var chain = new List<ExerciseSession>();
        for (var cursor = Breakdown; cursor.Pending is { } pending && pending.Predecessor is not null; cursor = pending.Predecessor)
            chain.Add(cursor);
        var target = chain.Single(value => value.Pending is ThirdStageSecondReservePending);
        var (tamperedTarget, _) = SessionTamper.TamperOwnRecord(target);
        Assert.NotNull(tamperedTarget);
        var rebuilt = tamperedTarget!;
        for (var index = chain.IndexOf(target) - 1; index >= 0; index--)
        {
            var clone = (ExerciseSession)typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(chain[index], null)!;
            typeof(ExerciseSession).GetProperty(chain[index].Pending!.GetType().Name[..^"Pending".Length] + "Predecessor",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(clone, rebuilt);
            rebuilt = clone;
        }
        var result = CampaignExercises.Reconstruct(rebuilt);
        Assert.False(result.IsVerified);
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, result.FailureReason);
    }

    [Fact]
    public void SecondSideEventsAreTheExistingNativeTypesInOrderAndFirstSideSlotsAreUntouched()
    {
        Assert.IsType<CampaignNativeSecondReserveV43>(Reserve.CurrentHistory[^1]);
        Assert.IsType<CampaignNativeSecondMovementCompletedV45>(Completion.CurrentHistory[^1]);
        Assert.IsType<CampaignNativeSecondBreakdownV46>(Breakdown.CurrentHistory[^1]);
        Assert.Equal(AfterFirstSide.CurrentHistory.Count + 3, Breakdown.CurrentHistory.Count);
        Assert.True(Reserve.CurrentThirdStageSecondReserve!.Completed);
        // The first side's retained Stage 3 records are the same instances; no field was reused or reset.
        Assert.Same(AfterFirstSide.CurrentThirdStageBreakdown, Breakdown.CurrentThirdStageBreakdown);
        Assert.Same(AfterFirstSide.CurrentThirdStageAssault, Breakdown.CurrentThirdStageAssault);
        Assert.Same(AfterFirstSide.CurrentStageEntrySecondBreakdown, Breakdown.CurrentStageEntrySecondBreakdown);
        Assert.Null(AfterFirstSide.CurrentThirdStageSecondReserve);
        // Operation Stage 3, acting role SecondActingSide, side opposite the first side's.
        var turn = Cna1979LandSequenceV4.CreateTurn(Breakdown.CurrentSnapshot.Setup.InitialGameTurn);
        var position = turn.Single(value => value.PositionId == Breakdown.CurrentThirdStageSecondBreakdown!.PositionId);
        Assert.Equal(3, position.OperationStage);
        Assert.Equal(LandActorRole.SecondActingSide, position.ActorRole);
        Assert.NotEqual(Breakdown.CurrentThirdStageBreakdown!.Side, Breakdown.CurrentThirdStageSecondBreakdown!.Side);
        // EVT-001: canonical bytes round-trip unchanged.
        foreach (var session in new[] { Reserve, Completion, Breakdown })
        {
            var bytes = CampaignCurrentEventSerializer.Serialize(session.CurrentHistory[^1], session.Context);
            Assert.Equal(bytes, CampaignCurrentEventSerializer.Serialize(
                CampaignCurrentEventSerializer.Deserialize(bytes), session.Context));
        }
    }

    // Dice cursor (#593): none of these steps draws; the retained cursor is the history's latest one.
    [Fact]
    public void NoSecondSideStepDrawsDiceAndTheLatestCursorIsUnchanged()
    {
        var before = ExerciseSession.LatestCursor(AfterFirstSide.CurrentSnapshot.RandomState, AfterFirstSide.CurrentHistory);
        var after = ExerciseSession.LatestCursor(Breakdown.CurrentSnapshot.RandomState, Breakdown.CurrentHistory);
        Assert.Equal(before, after);
    }

    [Fact]
    public void WrongSideDuplicateAndOutOfOrderSubmissionsAreRejected()
    {
        var firstSideAudience = Breakdown.CurrentThirdStageBreakdown!.Side == LandSide.Axis
            ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var secondAudience = firstSideAudience == CampaignActionAudience.Axis ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        var hash = "sha256:" + new string('a', 64);
        // Before the first side's chain finished: no second-side source.
        var midFirstSide = At("op3.first.breakdown");
        Assert.False(CampaignExercises.SubmitThirdStageSecondReserve(midFirstSide, secondAudience, hash, null).IsAccepted);
        // Wrong side / System / duplicate Reserve.
        Assert.False(CampaignExercises.SubmitThirdStageSecondReserve(AfterFirstSide, firstSideAudience, hash, null).IsAccepted);
        Assert.False(CampaignExercises.SubmitThirdStageSecondReserve(AfterFirstSide, CampaignActionAudience.System, hash, null).IsAccepted);
        Assert.False(CampaignExercises.SubmitThirdStageSecondReserve(Reserve, secondAudience, hash, null).IsAccepted);
        // Movement Completion / Breakdown before their predecessors, wrong side, duplicates.
        Assert.False(CampaignExercises.CompleteThirdStageSecondMovement(AfterFirstSide, secondAudience, hash).IsAccepted);
        Assert.False(CampaignExercises.CompleteThirdStageSecondMovement(Reserve, firstSideAudience, hash).IsAccepted);
        Assert.False(CampaignExercises.CompleteThirdStageSecondMovement(Completion, secondAudience, hash).IsAccepted);
        Assert.False(CampaignExercises.CompleteThirdStageSecondBreakdown(AfterFirstSide).IsAccepted);
        Assert.False(CampaignExercises.CompleteThirdStageSecondBreakdown(Reserve).IsAccepted);
        Assert.False(CampaignExercises.CompleteThirdStageSecondBreakdown(Breakdown).IsAccepted);
        // Movement after its own completion.
        Assert.False(CampaignExercises.SubmitThirdStageSecondMovement(Completion, secondAudience, hash, "x").IsAccepted);
        // First-side and Stage 2 siblings never accept second-side state.
        Assert.False(CampaignExercises.SubmitThirdStageReserve(Reserve, secondAudience, hash, null).IsAccepted);
        Assert.False(CampaignExercises.CompleteThirdStageBreakdown(Completion).IsAccepted);
        Assert.False(CampaignExercises.CompleteStageEntrySecondBreakdown(Completion).IsAccepted);
        Assert.False(CampaignExercises.SubmitStageEntrySecondReserve(AfterFirstSide, secondAudience, hash, null).IsAccepted);
    }

    // Stale-field checklist: Combat selection opens only at the current Breakdown; its tail
    // must stay closed and never answer from the retained Stage 2 / first-side runtimes.
    [Theory]
    [MemberData(nameof(Subtypes))]
    public void CombatAndTailEntryPointsFailClosedAtEverySecondSideCut(string typeName)
    {
        var session = SessionFor(typeName);
        var audience = session.CurrentThirdStageSecondReserve!.Side == LandSide.Axis
            ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        Assert.Null(CampaignCombatExerciseActions.Query(session, audience));
        Assert.Null(session.CurrentRepeatAssault);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryNativeReserveReleaseState(session));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryPostCombatPassSource(session));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryTruckConvoyPassSource(session));
        Assert.False(CampaignExercises.SubmitPostCombatPass(session, audience, "sha256:" + new string('b', 64)).IsAccepted);
        if (session.Pending is ThirdStageSecondBreakdownPending)
            Assert.Equal(session.CurrentContacts.AuthorityVersion, CampaignExercises.QueryNativeCombatSelectionRoundBinding(session).StateVersion);
        else
        {
            Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryNativeCombatSelectionRoundBinding(session));
            Assert.Throws<InvalidOperationException>(() =>
                CampaignExercises.QueryContactOwnerCombatCut(session, session.CurrentThirdStageSecondReserve.Side, "axis-infantry"));
        }
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        var submission = new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion, checkpoint.CampaignId,
            checkpoint.StateVersion, checkpoint.PositionId, audience, new FinishCombatWithoutAttackAction("axis-infantry").ActionId);
        if (session.Pending is not ThirdStageSecondBreakdownPending)
            Assert.False(CampaignExercises.SubmitContactOwnerCombatSelection(session, submission, "axis-infantry").IsAccepted);
        Assert.False(CampaignExercises.SubmitTruckConvoyPass(session, audience, "sha256:" + new string('c', 64)).IsAccepted);
    }

    [Theory]
    [InlineData("combat-selection")]
    [InlineData("combat-no-attack")]
    [InlineData("reserve-release")]
    [InlineData("cycle-control-finish")]
    [InlineData("truck-convoy-pass")]
    [InlineData("post-combat-pass-0")]
    [InlineData("post-combat-pass-1")]
    [InlineData("post-combat-pass-2")]
    [InlineData("post-combat-pass-3")]
    public void SecondSideCombatCutsReconstructAndRejectTheirOwnTamperedEvents(string label)
    {
        var session = At("op3.second." + label);
        Assert.Equal(label == "combat-selection" ? nameof(ThirdStageSecondCombatSelectionPending) : nameof(ThirdStageSecondAssaultPending),
            session.Pending!.GetType().Name);
        var bytes = CampaignCurrentEventSerializer.Serialize(session.CurrentHistory[^1], session.Context);
        var reconstructed = CampaignExercises.Reconstruct(session);
        Assert.True(reconstructed.IsVerified, reconstructed.FailureReason.ToString());
        Assert.Equal(bytes, reconstructed.ReconstructedSnapshotBytes);
        Assert.Equal(session.CurrentContacts.AuthorityVersion, CampaignExercises.QueryCheckpoint(session).StateVersion);
        var (tampered, _) = SessionTamper.TamperOwnRecord(session);
        Assert.NotNull(tampered);
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, CampaignExercises.Reconstruct(tampered).FailureReason);
        Assert.Same(Breakdown.CurrentThirdStageAssault, session.CurrentThirdStageAssault);
        Assert.Same(Breakdown.CurrentStageEntrySecondAssault, session.CurrentStageEntrySecondAssault);
        Assert.Same(session.CurrentThirdStageSecondAssault, session.ActiveThirdStageAssault);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryNativeCombatSelectionRoundBinding(session));
    }

    [Fact]
    public void SecondSideSelectionAndTailRejectWrongAudienceAndDuplicateSteps()
    {
        var breakdown = Breakdown.CurrentThirdStageSecondBreakdown!;
        var side = breakdown.Side;
        var audience = side == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var other = side == LandSide.Axis ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        var element = side == LandSide.Axis ? "axis-infantry" : "commonwealth-infantry";
        var checkpoint = CampaignExercises.QueryCheckpoint(Breakdown);
        CampaignActionSubmission Submission(CampaignActionAudience who) => new(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, who, new FinishCombatWithoutAttackAction(element).ActionId);
        Assert.Equal(Submission(audience).ActionId,
            CampaignExercises.QueryContactOwnerCombatCut(Breakdown, side, element).FinishOffer?.ActionId);
        Assert.False(CampaignExercises.SubmitContactOwnerCombatSelection(Breakdown, Submission(other), element).IsAccepted);
        Assert.False(CampaignExercises.SubmitContactOwnerCombatSelection(Breakdown, Submission(CampaignActionAudience.System), element).IsAccepted);
        var selected = At("op3.second.combat-selection");
        Assert.False(CampaignExercises.SubmitContactOwnerCombatSelection(selected, Submission(audience), element).IsAccepted);
        Assert.False(CampaignExercises.CompleteThirdStageCombatNoAttack(selected).IsAccepted);
        Assert.False(CampaignExercises.CompleteThirdStageSecondCombatNoAttack(Breakdown).IsAccepted);
        var noAttack = At("op3.second.combat-no-attack");
        Assert.False(CampaignExercises.CompleteThirdStageSecondCombatNoAttack(noAttack).IsAccepted);
        var release = At("op3.second.reserve-release");
        var hash = SequencingScenarios.Hash("invalid-tail");
        Assert.False(CampaignExercises.SubmitNativeCycleControl(release, other, CampaignNativeCycleControlChoiceV28.Finish, hash).IsAccepted);
        var terminal = At("op3.second.post-combat-pass-3");
        Assert.Null(terminal.CurrentRepeatAssault);
        Assert.False(CampaignExercises.SubmitPostCombatPass(terminal, audience, hash).IsAccepted);
    }

    [Fact]
    public void PendingResolvesToTheLatestSecondSideCutAheadOfEveryFirstSideAndStageTwoCut()
    {
        static T Record<T>() where T : class => (T)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(T));
        static void Set(ExerciseSession s, string name, object value) =>
            typeof(ExerciseSession).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.SetValue(s, value);
        var session = (ExerciseSession)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ExerciseSession));
        Set(session, "CurrentThirdStageBreakdown", Record<CampaignNativeSecondBreakdownV46>());
        Set(session, "CurrentThirdStageNoAttackCompletion", Record<CampaignNativeCombatNoAttackCompletionV13>());
        Set(session, "CurrentStageEntrySecondBreakdown", Record<CampaignNativeSecondBreakdownV46>());
        Assert.IsType<ThirdStageNoAttackPending>(session.Pending);
        Set(session, "CurrentThirdStageSecondReserve", Record<CampaignNativeSecondReserveV43>());
        Assert.IsType<ThirdStageSecondReservePending>(session.Pending);
        Set(session, "CurrentThirdStageSecondMove", Record<CampaignNativeSecondMoveV44>());
        Assert.IsType<ThirdStageSecondMovePending>(session.Pending);
        Set(session, "CurrentThirdStageSecondMovementCompletion", Record<CampaignNativeSecondMovementCompletedV45>());
        Assert.IsType<ThirdStageSecondMovementCompletionPending>(session.Pending);
        Set(session, "CurrentThirdStageSecondBreakdown", Record<CampaignNativeSecondBreakdownV46>());
        Assert.IsType<ThirdStageSecondBreakdownPending>(session.Pending);
    }
}
