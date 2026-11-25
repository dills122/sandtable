using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Exercises;

// Stage 3 chain, S3-1..S3-5 (docs/work/plans/2026-11-24-stage3-movement-breakdown-combat-scoping.md):
// Operation Stage 3's own first-acting-side Reserve Designation, Movement + Movement Completion,
// Breakdown, Combat Decision and Assault/No-Attack chain (through Post Combat Passes), entered from the
// Stage 2->3 preamble chain's terminal event. Each new PendingDecision subtype must Reconstruct, answer
// QueryCheckpoint without throwing, and return InvalidHistory (never throw) after a one-byte tamper.
// Entry points that read never-reset Stage 1/2 fields must not answer for Stage 3.
public sealed class CampaignStageThreeFirstSideTests
{
    private static readonly Lazy<StageThreeFixture> Fixture = new(StageThreeDriver.Build);

    private static readonly string[] CoveredSubtypes =
    [
        nameof(ThirdStageReservePending),
        nameof(ThirdStageMovementCompletionPending),
        nameof(ThirdStageBreakdownPending),
        nameof(ThirdStageCombatSelectionPending),
    ];

    public static TheoryData<string> Subtypes() => [.. CoveredSubtypes];

    private static ExerciseSession SessionFor(string typeName) => typeName switch
    {
        nameof(ThirdStageReservePending) => Fixture.Value.AfterReserve,
        nameof(ThirdStageMovementCompletionPending) => Fixture.Value.AfterMovementCompletion,
        nameof(ThirdStageBreakdownPending) => Fixture.Value.AfterBreakdown,
        nameof(ThirdStageCombatSelectionPending) => Fixture.Value.AfterCombatDecision,
        _ => throw new ArgumentOutOfRangeException(nameof(typeName)),
    };

    private static string OwnRecordProperty(string typeName) => typeName switch
    {
        nameof(ThirdStageReservePending) => nameof(ExerciseSession.CurrentThirdStageReserve),
        nameof(ThirdStageMovementCompletionPending) => nameof(ExerciseSession.CurrentThirdStageMovementCompletion),
        nameof(ThirdStageBreakdownPending) => nameof(ExerciseSession.CurrentThirdStageBreakdown),
        nameof(ThirdStageCombatSelectionPending) => nameof(ExerciseSession.CurrentThirdStageCombatDecision),
        _ => throw new ArgumentOutOfRangeException(nameof(typeName)),
    };

    // ThirdStageMovePending is deliberately not in Subtypes(): no own element has a legal movement
    // offer in the only setups this suite can drive (rules-lab truck/reaction: every infantry element has
    // zero offers at Stage 3's movement cut), so no real V44 can be retained. Its ReconstructOne case is a
    // line-for-line mirror of StageEntryMovePending's (itself unreached by any end-to-end test), and its
    // precedence in Pending is covered below. The V46 retained-move replay loop is covered with
    // Stage 1/2 occurrences by existing tests; Stage 3's Slot reuses it unchanged.
    [Fact]
    public void ThirdStageMovePendingIsTheOnlyNewSubtypeWithoutAnEndToEndFixture()
    {
        var covered = CoveredSubtypes.ToHashSet(StringComparer.Ordinal);
        var all = typeof(PendingDecision).Assembly.GetTypes()
            .Where(type => typeof(PendingDecision).IsAssignableFrom(type) && !type.IsAbstract
                && type.Name is "ThirdStageReservePending" or "ThirdStageMovePending"
                    or "ThirdStageMovementCompletionPending" or "ThirdStageBreakdownPending"
                    or "ThirdStageCombatSelectionPending" or "ThirdStageAssaultPending" or "ThirdStageNoAttackPending")
            .Select(type => type.Name).ToHashSet(StringComparer.Ordinal);
        // Assault is covered by the dedicated Assault tests below; NoAttack is unobservable (the no-attack
        // completion installs a non-empty Assault runtime, which Pending checks first).
        covered.Add("ThirdStageAssaultPending");
        covered.Add("ThirdStageNoAttackPending");
        Assert.Equal(["ThirdStageMovePending"], all.Except(covered));
    }

    [Fact]
    public void PendingResolvesToTheLatestStageThreeFirstSideCut()
    {
        static ExerciseSession Bare() =>
            (ExerciseSession)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ExerciseSession));
        static void Set(ExerciseSession session, string name, object value) =>
            typeof(ExerciseSession).GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                .SetValue(session, value);
        static T Record<T>() where T : class =>
            (T)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(T));

        var session = Bare();
        Set(session, "CurrentThirdStageNoObligationFleetRepair", Record<CampaignNativeNoObligationFleetRepairV54>());
        Set(session, "CurrentThirdStageReserve", Record<CampaignNativeSecondReserveV43>());
        // The never-reset preamble fields must not shadow Stage 3's own Reserve.
        Assert.IsType<ThirdStageReservePending>(session.Pending);
        Set(session, "CurrentThirdStageMove", Record<CampaignNativeSecondMoveV44>());
        Assert.IsType<ThirdStageMovePending>(session.Pending);
        Set(session, "CurrentThirdStageMovementCompletion", Record<CampaignNativeSecondMovementCompletedV45>());
        Assert.IsType<ThirdStageMovementCompletionPending>(session.Pending);
        Set(session, "CurrentThirdStageBreakdown", Record<CampaignNativeSecondBreakdownV46>());
        Assert.IsType<ThirdStageBreakdownPending>(session.Pending);
        Set(session, "CurrentThirdStageCombatDecision", Record<CampaignNativeSecondCombatDecisionStateV47>());
        Assert.IsType<ThirdStageCombatSelectionPending>(session.Pending);
        // Stale Stage 2 fields (never reset) must not shadow Stage 3's cuts either.
        Set(session, "CurrentStageEntrySecondBreakdown", Record<CampaignNativeSecondBreakdownV46>());
        Set(session, "CurrentStageEntrySecondCombatDecision", Record<CampaignNativeSecondCombatDecisionStateV47>());
        Assert.IsType<ThirdStageCombatSelectionPending>(session.Pending);
        // S3-5: Stage 3's own no-attack completion and assault runtime take precedence over its Decision
        // and over every stale Stage 2 assault/no-attack slot.
        Set(session, "CurrentStageEntrySecondNoAttackCompletion", Record<CampaignNativeCombatNoAttackCompletionV13>());
        Assert.IsType<ThirdStageCombatSelectionPending>(session.Pending);
        Set(session, "CurrentThirdStageNoAttackCompletion", Record<CampaignNativeCombatNoAttackCompletionV13>());
        Assert.IsType<ThirdStageNoAttackPending>(session.Pending);
    }

    [Theory]
    [MemberData(nameof(Subtypes))]
    public void ReconstructVerifiesEachSubtype(string typeName)
    {
        var session = SessionFor(typeName);
        Assert.Equal(typeName, session.Pending!.GetType().Name);
        var result = CampaignExercises.Reconstruct(session);
        Assert.True(result.IsVerified, typeName + ": " + result.FailureReason);
        Assert.NotNull(result.EventStreamHash);
        Assert.Equal(CampaignCurrentEventSerializer.Serialize(session.CurrentHistory[^1], session.Context),
            result.ReconstructedSnapshotBytes);
    }

    [Theory]
    [MemberData(nameof(Subtypes))]
    public void QueryCheckpointDoesNotThrowAndNamesAStageThreePosition(string typeName)
    {
        var session = SessionFor(typeName);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        Assert.Contains("operation-3", checkpoint.PositionId, StringComparison.Ordinal);
        Assert.Equal(session.CurrentContacts.AuthorityVersion, checkpoint.StateVersion);
    }

    [Theory]
    [MemberData(nameof(Subtypes))]
    public void OneByteTamperOfTheOwnRecordReturnsInvalidHistoryNotAnException(string typeName)
    {
        var session = SessionFor(typeName);
        var tampered = TamperOwnRecord(session, OwnRecordProperty(typeName));
        ExerciseReconstructionResult result = null!;
        var thrown = Record.Exception(() => result = CampaignExercises.Reconstruct(tampered));
        Assert.Null(thrown);
        Assert.False(result.IsVerified);
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, result.FailureReason);
    }

    [Theory]
    [MemberData(nameof(Subtypes))]
    public void TamperingAnEarlierStageThreeRecordIsCaughtFromALaterHead(string typeName)
    {
        // Head = Breakdown; flip a byte of this subtype's own record on the retained chain by
        // walking to that predecessor session, then re-pointing the head chain at the tampered clone.
        var head = Fixture.Value.AfterCombatDecision;
        var chain = new List<ExerciseSession>();
        for (var cursor = head; cursor.Pending is { } pending && pending.Predecessor is not null; cursor = pending.Predecessor)
            chain.Add(cursor);
        var target = chain.Single(value => value.Pending!.GetType().Name == typeName);
        var tamperedTarget = TamperOwnRecord(target, OwnRecordProperty(typeName));
        // Rebuild the successors above the tampered session so the head's predecessor links lead through it.
        var rebuilt = tamperedTarget;
        for (var index = chain.IndexOf(target) - 1; index >= 0; index--)
            rebuilt = Repoint(chain[index], rebuilt);
        var result = CampaignExercises.Reconstruct(rebuilt);
        Assert.False(result.IsVerified);
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, result.FailureReason);
    }

    [Fact]
    public void ReserveMovementAndBreakdownRetainTheirNativeEventsInOrder()
    {
        var f = Fixture.Value;
        Assert.Same(f.PreambleTerminal.CurrentThirdStageNoObligationFleetRepair, f.AfterReserve.ThirdStageReservePredecessor!
            .CurrentThirdStageNoObligationFleetRepair);
        Assert.True(f.AfterReserve.CurrentThirdStageReserve!.Completed);
        Assert.IsType<CampaignNativeSecondReserveV43>(f.AfterReserve.CurrentHistory[^1]);
        Assert.IsType<CampaignNativeSecondMovementCompletedV45>(f.AfterMovementCompletion.CurrentHistory[^1]);
        Assert.IsType<CampaignNativeSecondBreakdownV46>(f.AfterBreakdown.CurrentHistory[^1]);
        // Stage 2's own slots are untouched by Stage 3 (no cross-stage field reuse).
        Assert.Same(f.PreambleTerminal.CurrentStageEntryBreakdown, f.AfterBreakdown.CurrentStageEntryBreakdown);
        Assert.Same(f.PreambleTerminal.CurrentStageEntrySecondBreakdown, f.AfterBreakdown.CurrentStageEntrySecondBreakdown);
        Assert.Equal(f.PreambleTerminal.CurrentHistory.Count + 3, f.AfterBreakdown.CurrentHistory.Count);
        Assert.Equal(3, Cna1979LandSequenceV4.CreateTurn(f.AfterBreakdown.CurrentSnapshot.Setup.InitialGameTurn)
            .Single(value => value.PositionId == f.AfterBreakdown.CurrentThirdStageBreakdown!.PositionId).OperationStage);
        Assert.Equal(f.StageThreeFirstSide, f.AfterBreakdown.CurrentThirdStageBreakdown!.Side);
        // Canonical bytes round-trip (EVT-001: the events are the existing V43-V46 types, unchanged).
        foreach (object value in new object[] { f.AfterReserve.CurrentThirdStageReserve!,
            f.AfterMovementCompletion.CurrentThirdStageMovementCompletion!, f.AfterBreakdown.CurrentThirdStageBreakdown! })
        {
            var bytes = CampaignCurrentEventSerializer.Serialize(value);
            Assert.Equal(bytes, CampaignCurrentEventSerializer.Serialize(CampaignCurrentEventSerializer.Deserialize(bytes)));
        }
    }

    [Fact]
    public void WrongSideDuplicateAndOutOfOrderSubmissionsAreRejected()
    {
        var f = Fixture.Value;
        var first = f.FirstAudience;
        var second = first == CampaignActionAudience.Axis ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        var hash = Hash("reject");
        // Wrong side for Reserve.
        Assert.False(CampaignExercises.SubmitThirdStageReserve(f.PreambleTerminal, second, hash, null).IsAccepted);
        // System audience is not an owner.
        Assert.False(CampaignExercises.SubmitThirdStageReserve(f.PreambleTerminal, CampaignActionAudience.System, hash, null).IsAccepted);
        // Movement, Movement Completion and Breakdown before Reserve / Completion.
        Assert.False(CampaignExercises.CompleteThirdStageMovement(f.PreambleTerminal, first, hash).IsAccepted);
        Assert.False(CampaignExercises.CompleteThirdStageBreakdown(f.PreambleTerminal).IsAccepted);
        Assert.False(CampaignExercises.CompleteThirdStageBreakdown(f.AfterReserve).IsAccepted);
        // Duplicates.
        Assert.False(CampaignExercises.SubmitThirdStageReserve(f.AfterReserve, first, hash, null).IsAccepted);
        Assert.False(CampaignExercises.CompleteThirdStageMovement(f.AfterMovementCompletion, first, hash).IsAccepted);
        Assert.False(CampaignExercises.CompleteThirdStageBreakdown(f.AfterBreakdown).IsAccepted);
        // Movement after its own completion.
        Assert.False(CampaignExercises.SubmitThirdStageMovement(f.AfterMovementCompletion, first, hash, f.FirstElementId).IsAccepted);
        // Wrong side for Movement Completion.
        Assert.False(CampaignExercises.CompleteThirdStageMovement(f.AfterReserve, second, hash).IsAccepted);
        // The Stage 2 siblings must not accept Stage 3 state.
        Assert.False(CampaignExercises.SubmitStageEntryReserve(f.AfterReserve, first, hash, null).IsAccepted);
        Assert.False(CampaignExercises.CompleteStageEntryBreakdown(f.AfterMovementCompletion).IsAccepted);
    }

    // S3-4: Stage 3's own first-side Combat Decision is wired at the Breakdown cut; the owner,
    // element, action, state version and position are all enforced.
    [Fact]
    public void CombatDecisionAtTheBreakdownCutRequiresTheActingSideElementAndLiveAction()
    {
        var f = Fixture.Value;
        var session = f.AfterBreakdown;
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        var other = f.FirstAudience == CampaignActionAudience.Axis ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        var otherElement = other == CampaignActionAudience.Axis ? "axis-infantry" : "commonwealth-infantry";
        CampaignActionSubmission Submission(CampaignActionAudience audience, string actionId, long? version = null, string? position = null) =>
            new(CampaignActionSubmission.CurrentContractVersion, checkpoint.CampaignId, version ?? checkpoint.StateVersion,
                position ?? checkpoint.PositionId, audience, actionId);
        var finish = new FinishCombatWithoutAttackAction(f.FirstElementId).ActionId;

        // Wrong side / wrong element / System audience / bogus action / stale cut.
        Assert.False(CampaignExercises.SubmitContactOwnerCombatSelection(session,
            Submission(other, new FinishCombatWithoutAttackAction(otherElement).ActionId), otherElement).IsAccepted);
        Assert.False(CampaignExercises.SubmitContactOwnerCombatSelection(session,
            Submission(f.FirstAudience, finish), otherElement).IsAccepted);
        Assert.False(CampaignExercises.SubmitContactOwnerCombatSelection(session,
            Submission(CampaignActionAudience.System, finish), f.FirstElementId).IsAccepted);
        Assert.False(CampaignExercises.SubmitContactOwnerCombatSelection(session,
            Submission(f.FirstAudience, Hash("bogus-action")), f.FirstElementId).IsAccepted);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.SubmitContactOwnerCombatSelection(session,
            Submission(f.FirstAudience, finish, version: checkpoint.StateVersion - 1), f.FirstElementId).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.UnexpectedPosition, CampaignExercises.SubmitContactOwnerCombatSelection(session,
            Submission(f.FirstAudience, finish, position: "land.position.operation-2.first-player.movement-and-combat.combat"),
            f.FirstElementId).RejectionReason);

        // The cut projects from Stage 3's own World and position; the accepted decision names Stage 3.
        var cut = CampaignExercises.QueryContactOwnerCombatCut(session,
            f.StageThreeFirstSide, f.FirstElementId);
        Assert.Equal(finish, cut.FinishOffer?.ActionId);
        var decision = f.AfterCombatDecision.CurrentThirdStageCombatDecision!.Decision;
        Assert.Equal(f.StageThreeFirstSide, decision.Side);
        Assert.Equal(f.FirstElementId, decision.ActingElementId);
        Assert.Contains("operation-3", decision.PositionId, StringComparison.Ordinal);
        Assert.Equal(f.AfterBreakdown.CurrentThirdStageBreakdown!.NextPositionId, decision.PositionId);
        Assert.Same(f.AfterBreakdown, f.AfterCombatDecision.ThirdStageCombatSelectionPredecessor);

        // Dice cursor (#593): selection does not draw, and the state carries the retained history's latest cursor.
        var expectedCursor = ExerciseSession.LatestCursor(session.CurrentSnapshot.RandomState, session.CurrentHistory);
        Assert.Equal(expectedCursor, f.AfterCombatDecision.CurrentThirdStageCombatDecision!.RandomState);
        Assert.NotEqual(session.CurrentSnapshot.RandomState, expectedCursor);

        // Everything after the Decision stays closed at this cut.
        AssertPostDecisionEntryPointsClosed(session, f.FirstAudience);
    }

    // Stale-field checklist, S3-5: once the Decision exists Stage 3's own assault runtime is the only active
    // one; the never-reset Stage 2 runtimes and Stage 1/2 Combat entry points must not answer for Stage 3.
    [Fact]
    public void AfterTheStageThreeDecisionOnlyStageThreesOwnAssaultRuntimeAnswers()
    {
        var f = Fixture.Value;
        var session = f.AfterCombatDecision;
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        Assert.Contains("operation-3", checkpoint.PositionId, StringComparison.Ordinal);
        var finish = new FinishCombatWithoutAttackAction(f.FirstElementId).ActionId;
        var submission = new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, f.FirstAudience, finish);
        // Duplicate Decision.
        Assert.False(CampaignExercises.SubmitContactOwnerCombatSelection(session, submission, f.FirstElementId).IsAccepted);
        Assert.Throws<InvalidOperationException>(() =>
            CampaignExercises.QueryContactOwnerCombatCut(session, f.StageThreeFirstSide, f.FirstElementId));
        Assert.Throws<InvalidOperationException>(() => session.WithThirdStageCombatSelection(
            session.CurrentThirdStageCombatDecision!, session.CurrentContacts));
        // Stage 3's runtime is active (zero events); the stale Stage 2 runtimes stay bound to Stage 2.
        Assert.NotNull(session.CurrentThirdStageAssault);
        Assert.Empty(session.CurrentThirdStageAssault!.Events);
        // The fixture's Decision is a voluntary finish (IsSecondSideFinish), so CurrentRepeatAssault is null:
        // crucially it is not the stale Stage 2 runtime. Every Tier-2 method reads CurrentThirdStageAssault directly.
        Assert.True(session.CurrentThirdStageAssault.IsSecondSideFinish);
        Assert.Null(session.CurrentRepeatAssault);
        Assert.NotNull(session.CurrentStageEntrySecondAssault);
        Assert.Same(f.PreambleTerminal.CurrentStageEntrySecondAssault, session.CurrentStageEntrySecondAssault);
        Assert.Same(f.PreambleTerminal.CurrentStageEntryAssault, session.CurrentStageEntryAssault);
        AssertPostDecisionEntryPointsClosed(session, f.FirstAudience);
        // The Stage 2 siblings never accept Stage 3 state, and nothing runs ahead of the No-Attack completion.
        Assert.False(CampaignExercises.CompleteStageEntryCombatNoAttack(session).IsAccepted);
        Assert.False(CampaignExercises.CompleteStageEntrySecondCombatNoAttack(session).IsAccepted);
        Assert.False(CampaignExercises.CompleteNativeReserveRelease(session).IsAccepted);
        Assert.False(CampaignExercises.SubmitNativeCycleControl(session, f.FirstAudience,
            CampaignNativeCycleControlChoiceV28.Finish, Hash("cycle-control")).IsAccepted);
        Assert.False(CampaignExercises.SubmitTruckConvoyPass(session, f.FirstAudience, Hash("truck")).IsAccepted);
        // The Stage 3 No-Attack completion is single-use and gated on Stage 3's own Decision.
        Assert.False(CampaignExercises.CompleteThirdStageCombatNoAttack(f.AfterBreakdown).IsAccepted);
        Assert.False(CampaignExercises.CompleteThirdStageCombatNoAttack(f.AfterNoAttack).IsAccepted);
        Assert.False(CampaignExercises.CompleteThirdStageCombatNoAttack(f.PreambleTerminal).IsAccepted);
        // Stale Stage 1/2 Release/Control/Post-Combat sources must not answer for Stage 3 mid-chain.
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryNativeReserveReleaseState(session));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryPostCombatPassSource(session));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryTruckConvoyPassSource(session));
    }

    [Fact]
    public void StageThreeAssaultChainAdvancesInOrderThroughPostCombatPassesAndStageTwoStaysUntouched()
    {
        var f = Fixture.Value;
        var chain = new[] { f.AfterCombatDecision, f.AfterNoAttack, f.AfterReserveRelease, f.AfterCycleControl,
            f.AfterTruckConvoy, f.AfterPostCombatPasses };
        Assert.Equal(typeof(ThirdStageCombatSelectionPending), f.AfterCombatDecision.Pending!.GetType());
        // The No-Attack completion installs a non-empty runtime, so every later cut resolves to the Assault arm.
        Assert.All(chain.Skip(1), session => Assert.IsType<ThirdStageAssaultPending>(session.Pending));
        Assert.NotNull(f.AfterNoAttack.CurrentThirdStageNoAttackCompletion);
        Assert.IsType<CampaignNativeCombatNoAttackCompletionV13>(f.AfterNoAttack.CurrentHistory[^1]);
        Assert.NotNull(f.AfterReserveRelease.CurrentThirdStageAssault!.Release);
        Assert.Equal(CampaignNativeCycleControlChoiceV28.Finish, f.AfterCycleControl.CurrentThirdStageAssault!.Control!.Choice);
        Assert.NotNull(f.AfterTruckConvoy.CurrentThirdStageAssault!.TruckConvoyPass);
        Assert.Equal(4, f.AfterPostCombatPasses.CurrentThirdStageAssault!.PostCombatPasses.Count);
        // Each step appended history and advanced the contact authority version.
        for (var index = 1; index < chain.Length; index++)
        {
            Assert.True(chain[index].CurrentHistory.Count > chain[index - 1].CurrentHistory.Count);
            Assert.True(chain[index].CurrentContacts.AuthorityVersion > chain[index - 1].CurrentContacts.AuthorityVersion);
        }
        // Stage 3 post-combat passes carry Stage 3 positions; checkpoints never throw along the chain.
        Assert.All(chain, session => Assert.Contains("operation-3",
            CampaignExercises.QueryCheckpoint(session).PositionId, StringComparison.Ordinal));
        // Stage 2's own slots and runtimes are unchanged by the whole Stage 3 chain.
        Assert.Same(f.PreambleTerminal.CurrentStageEntryAssault, f.AfterPostCombatPasses.CurrentStageEntryAssault);
        Assert.Same(f.PreambleTerminal.CurrentStageEntrySecondAssault, f.AfterPostCombatPasses.CurrentStageEntrySecondAssault);
        Assert.Null(f.PreambleTerminal.CurrentThirdStageAssault);
        // Once Stage 3's own chain finished no runtime is active (never the stale Stage 2 one).
        Assert.Null(f.AfterPostCombatPasses.CurrentRepeatAssault);
        // EVT-001: the retained events are the existing types and round-trip canonically.
        foreach (var session in chain)
        {
            var bytes = CampaignCurrentEventSerializer.Serialize(session.CurrentHistory[^1], session.Context);
            Assert.Equal(bytes, CampaignCurrentEventSerializer.Serialize(
                CampaignCurrentEventSerializer.Deserialize(bytes), session.Context));
        }
    }

    // Reconstruct happy case for every observable Stage 3 Assault cut (the no-attack completion, release,
    // cycle control, truck convoy and each post-combat pass), plus QueryCheckpoint never throwing.
    [Fact]
    public void ReconstructVerifiesEveryStageThreeAssaultCut()
    {
        var f = Fixture.Value;
        var cuts = new List<ExerciseSession> { f.AfterNoAttack, f.AfterReserveRelease, f.AfterCycleControl,
            f.AfterTruckConvoy, f.AfterPostCombatPasses };
        for (var session = f.AfterPostCombatPasses; session.Pending is ThirdStageAssaultPending pending
            && pending.Predecessor is { } predecessor; session = predecessor)
            if (!cuts.Contains(predecessor)) cuts.Add(predecessor);
        foreach (var session in cuts.Where(value => value.Pending is ThirdStageAssaultPending))
        {
            var result = CampaignExercises.Reconstruct(session);
            Assert.True(result.IsVerified, "assault step " + session.CurrentThirdStageAssault!.Events.Count + ": " + result.FailureReason);
            Assert.NotNull(result.EventStreamHash);
            Assert.Equal(CampaignCurrentEventSerializer.Serialize(session.CurrentHistory[^1], session.Context),
                result.ReconstructedSnapshotBytes);
        }
    }

    // One-byte tamper of the Assault step's own retained event (the runtime itself has no byte form, so the
    // last event inside a cloned runtime is flipped). Reconstruct must return InvalidHistory and never throw.
    [Fact]
    public void OneByteTamperOfTheStageThreeAssaultStepsOwnEventReturnsInvalidHistoryNotAnException()
    {
        var f = Fixture.Value;
        foreach (var session in new[] { f.AfterNoAttack, f.AfterReserveRelease, f.AfterCycleControl, f.AfterTruckConvoy,
            f.AfterPostCombatPasses })
        {
            var tampered = TamperAssaultLastEvent(session);
            ExerciseReconstructionResult result = null!;
            var thrown = Record.Exception(() => result = CampaignExercises.Reconstruct(tampered));
            Assert.Null(thrown);
            Assert.False(result.IsVerified);
            Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, result.FailureReason);
        }
    }

    // A tampered earlier Stage 3 record (here the Combat Decision) is caught from the terminal Assault head.
    [Fact]
    public void TamperingTheStageThreeDecisionIsCaughtFromTheTerminalAssaultHead()
    {
        var f = Fixture.Value;
        var chain = new List<ExerciseSession>();
        for (var cursor = f.AfterPostCombatPasses; cursor.Pending is { } pending && pending.Predecessor is not null; cursor = pending.Predecessor)
            chain.Add(cursor);
        var target = chain.Single(value => value.Pending is ThirdStageCombatSelectionPending);
        var rebuilt = TamperOwnRecord(target, nameof(ExerciseSession.CurrentThirdStageCombatDecision));
        for (var index = chain.IndexOf(target) - 1; index >= 0; index--)
            rebuilt = Repoint(chain[index], rebuilt);
        var result = CampaignExercises.Reconstruct(rebuilt);
        Assert.False(result.IsVerified);
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory, result.FailureReason);
    }

    // Rejections: wrong audience / duplicates / out of order along the Stage 3 assault tail.
    [Fact]
    public void WrongAudienceDuplicateAndOutOfOrderAssaultTailSubmissionsAreRejected()
    {
        var f = Fixture.Value;
        var first = f.FirstAudience;
        var second = first == CampaignActionAudience.Axis ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        // Reserve Release only after the No-Attack completion; Cycle Control only after Release.
        Assert.False(CampaignExercises.CompleteNativeReserveRelease(f.AfterReserveRelease).IsAccepted);
        Assert.False(CampaignExercises.SubmitNativeCycleControl(f.AfterNoAttack, first,
            CampaignNativeCycleControlChoiceV28.Finish, Hash("early-cycle")).IsAccepted);
        // Wrong side for Cycle Control and Truck Convoy / Post Combat Pass.
        Assert.False(CampaignExercises.SubmitNativeCycleControl(f.AfterReserveRelease, second,
            CampaignNativeCycleControlChoiceV28.Finish, Hash("wrong-side-cycle")).IsAccepted);
        Assert.False(CampaignExercises.SubmitTruckConvoyPass(f.AfterCycleControl, CampaignActionAudience.System, Hash("sys-truck")).IsAccepted);
        // Duplicates.
        Assert.False(CampaignExercises.SubmitNativeCycleControl(f.AfterCycleControl, first,
            CampaignNativeCycleControlChoiceV28.Finish, Hash("dup-cycle")).IsAccepted);
        Assert.False(CampaignExercises.SubmitTruckConvoyPass(f.AfterTruckConvoy, first, Hash("dup-truck")).IsAccepted);
        // Post Combat Pass before Truck Convoy; System is not an owner.
        Assert.False(CampaignExercises.SubmitPostCombatPass(f.AfterCycleControl, first, Hash("early-pass")).IsAccepted);
        Assert.False(CampaignExercises.SubmitPostCombatPass(f.AfterTruckConvoy, CampaignActionAudience.System, Hash("sys-pass")).IsAccepted);
        // The fifth pass does not exist.
        Assert.False(CampaignExercises.SubmitPostCombatPass(f.AfterPostCombatPasses, first, Hash("fifth-pass")).IsAccepted);
    }

    private static ExerciseSession TamperAssaultLastEvent(ExerciseSession session)
    {
        const BindingFlags any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var runtime = session.CurrentThirdStageAssault!;
        var last = runtime.Events[^1];
        var bytes = CampaignCurrentEventSerializer.Serialize(last, session.Context);
        for (var index = bytes.Length - 1; index >= 0; index--)
        {
            var altered = (byte[])bytes.Clone();
            altered[index] ^= 1;
            object replacement;
            try { replacement = CampaignCurrentEventSerializer.Deserialize(altered); }
            catch (Exception error) when (error is System.Text.Json.JsonException or ArgumentException
                or InvalidOperationException or FormatException or KeyNotFoundException)
            { continue; }
            if (replacement.Equals(last) || replacement.GetType() != last.GetType()) continue;
            var runtimeClone = (CampaignNativeRepeatAssaultRuntime)typeof(object)
                .GetMethod("MemberwiseClone", any)!.Invoke(runtime, null)!;
            var eventsField = typeof(CampaignNativeRepeatAssaultRuntime).GetField("events", any)!;
            var events = new List<object>(runtime.Events) { [runtime.Events.Count - 1] = replacement };
            eventsField.SetValue(runtimeClone, events);
            var clone = Clone(session);
            typeof(ExerciseSession).GetProperty(nameof(ExerciseSession.CurrentThirdStageAssault), any)!
                .SetValue(clone, runtimeClone);
            return clone;
        }
        throw new InvalidOperationException("No one-byte flip of the Stage 3 assault event still deserializes.");
    }

    private static void AssertPostDecisionEntryPointsClosed(ExerciseSession session, CampaignActionAudience audience)
    {
        // CampaignCombatExerciseActions.Classify: reads Stage 1 fields, had no Stage Entry guard.
        Assert.Null(CampaignCombatExerciseActions.Query(session, audience));
        // SubmitPostCombatPass lacked the CurrentInitiativeDeclaration-is-null guard its siblings have.
        Assert.False(CampaignExercises.SubmitPostCombatPass(session, audience, Hash("post-combat")).IsAccepted);
        // Round binding / timeout keyed off the never-reset CurrentSecondBreakdown.
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryNativeCombatSelectionRoundBinding(session));
        Assert.False(CampaignExercises.TimeoutNativeCombatSelection(session, null, 0, 0, 0, null, "selection-unavailable").IsAccepted);
    }

    // The same fail-closed guard must also hold at every earlier Stage 3 cut (preamble, Reserve, ...).
    [Theory]
    [MemberData(nameof(Subtypes))]
    public void CombatEntryPointsFailClosedAtEveryStageThreeCut(string typeName)
    {
        var session = SessionFor(typeName);
        Assert.Null(CampaignCombatExerciseActions.Query(session, Fixture.Value.FirstAudience));
        Assert.False(CampaignExercises.SubmitPostCombatPass(session, Fixture.Value.FirstAudience, Hash("post-combat")).IsAccepted);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryNativeCombatSelectionRoundBinding(session));
    }

    [Fact]
    public void StageTwoSessionsAreNotAffectedByTheStageThreeCombatGuard()
    {
        // The guard keys on Stage 3's own Initiative Declaration, so a Stage 2 terminal still reaches Classify as before.
        Assert.False(CampaignExercises.IsStageThreeCombatUnwired(Fixture.Value.StageTwoTerminal));
        Assert.True(CampaignExercises.IsStageThreeCombatUnwired(Fixture.Value.PreambleTerminal));
    }

    [Fact]
    public void StageThreeOrdersCarryAllThreeStagesOrdersAndStageTwoOrdersAreUnchanged()
    {
        var f = Fixture.Value;
        var orders = CampaignExercises.ThirdStageOrders(f.PreambleTerminal);
        Assert.Equal([1, 2, 3], orders.Select(value => value.OperationStage).Order());
        Assert.Equal([1, 2], CampaignExercises.StageEntryOrders(f.PreambleTerminal).Select(value => value.OperationStage).Order());
    }

    private static ExerciseSession TamperOwnRecord(ExerciseSession session, string propertyName)
    {
        const BindingFlags any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var property = typeof(ExerciseSession).GetProperty(propertyName, any)!;
        var current = property.GetValue(session)!;
        if (current is CampaignNativeSecondCombatDecisionStateV47 state)
        {
            // The retained V47 state wraps a byte-addressable Decision; flip one byte of it.
            var decisionBytes = state.Decision.SerializeCanonical();
            for (var index = decisionBytes.Length - 1; index >= 0; index--)
            {
                var altered = (byte[])decisionBytes.Clone();
                altered[index] ^= 1;
                CampaignNativeSecondCombatDecisionV47 replacement;
                try { replacement = CampaignNativeSecondCombatDecisionV47.Deserialize(altered); }
                catch (Exception error) when (error is System.Text.Json.JsonException or ArgumentException
                    or InvalidOperationException or FormatException)
                { continue; }
                if (replacement.Equals(state.Decision)) continue;
                var tamperedClone = Clone(session);
                property.SetValue(tamperedClone, state with { Decision = replacement });
                return tamperedClone;
            }
            throw new InvalidOperationException("No one-byte flip of the Stage 3 Combat Decision still deserializes.");
        }
        var serialize = current.GetType().GetMethod("SerializeCanonical", any, Type.EmptyTypes)!;
        var deserialize = current.GetType().GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public
            | BindingFlags.NonPublic, [typeof(ReadOnlyMemory<byte>)])!;
        var bytes = (byte[])serialize.Invoke(current, null)!;
        for (var index = bytes.Length - 1; index >= 0; index--)
        {
            var altered = (byte[])bytes.Clone();
            altered[index] ^= 1;
            object? replacement;
            try { replacement = deserialize.Invoke(null, [new ReadOnlyMemory<byte>(altered)]); }
            catch (TargetInvocationException) { continue; }
            if (replacement is null || replacement.Equals(current)) continue;
            var clone = Clone(session);
            property.SetValue(clone, replacement);
            return clone;
        }
        throw new InvalidOperationException("No one-byte flip of " + propertyName + " still deserializes.");
    }

    private static ExerciseSession Clone(ExerciseSession session) =>
        (ExerciseSession)typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(session, null)!;

    // Clone `successor` with its own-predecessor slot replaced by `newPredecessor`.
    private static ExerciseSession Repoint(ExerciseSession successor, ExerciseSession newPredecessor)
    {
        var name = successor.Pending!.GetType().Name;
        var property = typeof(ExerciseSession).GetProperty(name[..^"Pending".Length] + "Predecessor",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;
        var clone = Clone(successor);
        property.SetValue(clone, newPredecessor);
        return clone;
    }

    private static string Hash(string label) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(label)));

    private sealed record StageThreeFixture(ExerciseSession StageTwoTerminal, ExerciseSession PreambleTerminal,
        ExerciseSession AfterReserve, ExerciseSession AfterMovementCompletion,
        ExerciseSession AfterBreakdown, ExerciseSession AfterCombatDecision, ExerciseSession AfterNoAttack,
        ExerciseSession AfterReserveRelease, ExerciseSession AfterCycleControl, ExerciseSession AfterTruckConvoy,
        ExerciseSession AfterPostCombatPasses, CampaignActionAudience FirstAudience, LandSide StageThreeFirstSide,
        string FirstElementId);

    // Drives one real session through Operation Stages 1 and 2 (both acting sides) and the Stage 2->3
    // preamble, then through Stage 3's own first-side Reserve, Movement Completion, Breakdown, Combat Decision,
    // No-Attack, Reserve Release, Cycle Control Finish, Truck Convoy and the four Post Combat Passes.
    // Same Core dispatch shape as CampaignStageThreeEntryVerificationTests.
    private static class StageThreeDriver
    {
        internal static StageThreeFixture Build()
        {
            var session = CampaignExercises.Begin(CreateRequest()).Session!;
            session = AdvanceFirstSideToCombatPositionDetermination(session);
            var stage1Order = session.CurrentSnapshot.OperationStageOrders.Single(value => value.OperationStage == 1);
            var firstAudience = stage1Order.FirstSide == LandSide.Axis
                ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            var secondAudience = Other(firstAudience);
            session = SubmitFinishWithoutAttack(session, firstAudience, Element(firstAudience),
                CampaignExercises.CompleteNativeCombatNoAttack);
            session = FinishCycleThroughPostCombatPasses(session, firstAudience);
            session = CloseOutSecondSideChain(session, secondAudience, Element(secondAudience));

            var gameTurn = session.CurrentSnapshot.Setup.InitialGameTurn;
            var holder = session.CurrentSnapshot.InitiativeHolder
                ?? throw new InvalidOperationException("Initiative holder is unresolved.");
            var holderAudience = holder == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            var otherAudience = Other(holderAudience);
            var declared = CampaignExercises.SubmitStageEntryInitiativeDeclaration(session, holderAudience,
                Hash("stage2-initiative-declaration"), actFirst: true);
            Assert.True(declared.IsAccepted, declared.RejectionReason.ToString());
            session = declared.SuccessorSession!;
            var weather = CampaignNativeWeatherDeterminationV50.Create(session.CurrentInitiativeDeclaration!,
                session.CurrentSnapshot.RandomState, gameTurn, session.CurrentContacts.AuthorityVersion);
            session = session.WithStageEntryWeather(weather, session.CurrentContacts.AdvanceWeatherDetermination(weather));
            var organization = CampaignNativeNoObligationOrganizationV51.Create(weather, weather.StateVersion);
            session = session.WithNoObligationOrganization(organization,
                session.CurrentContacts.AdvanceNoObligationOrganization(organization));
            var convoy = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(organization, session.Context, organization.StateVersion);
            session = session.WithNoObligationNavalConvoyArrival(convoy,
                session.CurrentContacts.AdvanceNoObligationNavalConvoyArrival(convoy));
            var assignment = CampaignNativeNoObligationFleetAssignmentV53.Create(convoy, convoy.StateVersion);
            session = session.WithNoObligationFleetAssignment(assignment,
                session.CurrentContacts.AdvanceNoObligationFleetAssignment(assignment));
            var repair = CampaignNativeNoObligationFleetRepairV54.Create(assignment, assignment.StateVersion);
            session = session.WithNoObligationFleetRepair(repair, session.CurrentContacts.AdvanceNoObligationFleetRepair(repair));
            session = CloseOutStageEntrySide(session, holderAudience, Element(holderAudience), isSecondSide: false);
            session = CloseOutStageEntrySide(session, otherAudience, Element(otherAudience), isSecondSide: true);
            var stageTwoTerminal = session;

            var predecessor = session.CurrentStageEntrySecondAssault!.PostCombatPasses[^1];
            var thirdDeclared = CampaignNativeInitiativeDeclarationV49.Create(predecessor, holder, actFirst: true,
                gameTurn, predecessor.StateVersion);
            session = session.WithThirdStageInitiativeDeclaration(thirdDeclared,
                session.CurrentContacts.AdvanceInitiativeDeclaration(thirdDeclared));
            var initialRandom = session.CurrentSnapshot.RandomState;
            var random = new RandomStreamState(initialRandom.ContractVersion, initialRandom.AlgorithmId,
                initialRandom.Seed, weather.ResultRandomCursor);
            var thirdWeather = CampaignNativeWeatherDeterminationV50.Create(thirdDeclared, random, gameTurn,
                session.CurrentContacts.AuthorityVersion);
            session = session.WithThirdStageWeather(thirdWeather, session.CurrentContacts.AdvanceWeatherDetermination(thirdWeather));
            var thirdOrganization = CampaignNativeNoObligationOrganizationV51.Create(thirdWeather, thirdWeather.StateVersion);
            session = session.WithThirdStageNoObligationOrganization(thirdOrganization,
                session.CurrentContacts.AdvanceNoObligationOrganization(thirdOrganization));
            var thirdArrival = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(thirdOrganization,
                session.Context, thirdOrganization.StateVersion);
            session = session.WithThirdStageNoObligationNavalConvoyArrival(thirdArrival,
                session.CurrentContacts.AdvanceNoObligationNavalConvoyArrival(thirdArrival));
            var thirdAssignment = CampaignNativeNoObligationFleetAssignmentV53.Create(thirdArrival, thirdArrival.StateVersion);
            session = session.WithThirdStageNoObligationFleetAssignment(thirdAssignment,
                session.CurrentContacts.AdvanceNoObligationFleetAssignment(thirdAssignment));
            var thirdRepair = CampaignNativeNoObligationFleetRepairV54.Create(thirdAssignment, thirdAssignment.StateVersion);
            session = session.WithThirdStageNoObligationFleetRepair(thirdRepair,
                session.CurrentContacts.AdvanceNoObligationFleetRepair(thirdRepair));
            var preambleTerminal = session;

            var stageThreeFirstSide = thirdRepair.ToOrder().FirstSide;
            var audience = stageThreeFirstSide == LandSide.Axis ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
            var elementId = Element(audience);

            var afterReserve = Ok(CampaignExercises.SubmitThirdStageReserve(preambleTerminal, audience, Hash("stage3-reserve"), elementId: null));

            var afterMovementCompletion = Ok(CampaignExercises.CompleteThirdStageMovement(afterReserve, audience, Hash("stage3-movement-completion")));
            var afterBreakdown = Ok(CampaignExercises.CompleteThirdStageBreakdown(afterMovementCompletion));
            var breakdownCut = CampaignExercises.QueryCheckpoint(afterBreakdown);
            var afterCombatDecision = Ok(CampaignExercises.SubmitContactOwnerCombatSelection(afterBreakdown,
                new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion, breakdownCut.CampaignId,
                    breakdownCut.StateVersion, breakdownCut.PositionId, audience,
                    new FinishCombatWithoutAttackAction(elementId).ActionId), elementId));
            var afterNoAttack = Ok(CampaignExercises.CompleteThirdStageCombatNoAttack(afterCombatDecision));
            var afterRelease = Ok(CampaignExercises.CompleteNativeReserveRelease(afterNoAttack));
            var afterCycle = Ok(CampaignExercises.SubmitNativeCycleControl(afterRelease, audience,
                CampaignNativeCycleControlChoiceV28.Finish, Hash("stage3-cycle-control-finish")));
            var afterTruck = Ok(CampaignExercises.SubmitTruckConvoyPass(afterCycle, audience, Hash("stage3-truck-convoy-pass")));
            var afterPasses = afterTruck;
            for (var index = 0; index < 4; index++)
                afterPasses = Ok(CampaignExercises.SubmitPostCombatPass(afterPasses, audience, Hash($"stage3-post-combat-pass-{index}")));
            return new StageThreeFixture(stageTwoTerminal, preambleTerminal, afterReserve,
                afterMovementCompletion, afterBreakdown, afterCombatDecision, afterNoAttack, afterRelease, afterCycle,
                afterTruck, afterPasses, audience, stageThreeFirstSide, elementId);
        }

        private static ExerciseSession Ok(ExerciseStepResult result)
        {
            Assert.True(result.IsAccepted, result.RejectionReason.ToString());
            return result.SuccessorSession!;
        }

        private static CampaignActionAudience Other(CampaignActionAudience audience) =>
            audience == CampaignActionAudience.Axis ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;

        private static string Element(CampaignActionAudience audience) =>
            audience == CampaignActionAudience.Axis ? "axis-infantry" : "commonwealth-infantry";

        private static CampaignCreationRequest CreateRequest()
        {
            var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions
                .Single(value => value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
            return new CampaignCreationRequest(1, "campaign-stage-three-first-side", Cna1979Ruleset.Manifest.Hash,
                12345, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId,
                setup.Content.Pack.Hash, setup.Content.ScenarioId);
        }

        private static ExerciseSession AdvanceFirstSideToCombatPositionDetermination(ExerciseSession session)
        {
            for (var index = 0; index < 80; index++)
            {
                var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
                if (position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination)
                    return session;
                var set = Enum.GetValues<CampaignActionAudience>()
                    .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                    .First(value => value?.Candidates.Count > 0)!;
                var action = set.Candidates.FirstOrDefault(value => value is not MoveElementAction) ?? set.Candidates[0];
                session = CampaignExercises.Submit(session, new CampaignActionSubmission(1, set.CampaignId,
                    set.StateVersion, set.PositionId, set.Audience, action.ActionId)).SuccessorSession!;
            }
            throw new InvalidOperationException("Combat position was not reached.");
        }

        private static ExerciseSession FinishCycleThroughPostCombatPasses(ExerciseSession session, CampaignActionAudience audience)
        {
            session = Ok(CampaignExercises.SubmitNativeCycleControl(session, audience,
                CampaignNativeCycleControlChoiceV28.Finish, Hash("cycle-control-finish")));
            session = Ok(CampaignExercises.SubmitTruckConvoyPass(session, audience, Hash("truck-convoy-pass")));
            for (var index = 0; index < 4; index++)
                session = Ok(CampaignExercises.SubmitPostCombatPass(session, audience, Hash($"post-combat-pass-{index}")));
            return session;
        }

        private static ExerciseSession CloseOutSecondSideChain(ExerciseSession session,
            CampaignActionAudience audience, string actingElementId)
        {
            session = Ok(CampaignExercises.SubmitSecondReserve(session, audience, Hash("second-reserve"), elementId: null));
            session = Ok(CampaignExercises.CompleteSecondMovement(session, audience, Hash("second-movement-completion")));
            session = Ok(CampaignExercises.CompleteSecondBreakdown(session));
            session = SubmitFinishWithoutAttack(session, audience, actingElementId, CampaignExercises.CompleteSecondCombatNoAttack);
            return FinishCycleThroughPostCombatPasses(session, audience);
        }

        private static ExerciseSession CloseOutStageEntrySide(ExerciseSession session,
            CampaignActionAudience audience, string actingElementId, bool isSecondSide)
        {
            session = Ok(isSecondSide
                ? CampaignExercises.SubmitStageEntrySecondReserve(session, audience, Hash("stage2-reserve"), elementId: null)
                : CampaignExercises.SubmitStageEntryReserve(session, audience, Hash("stage2-reserve"), elementId: null));
            session = Ok(isSecondSide
                ? CampaignExercises.CompleteStageEntrySecondMovement(session, audience, Hash("stage2-movement-completion"))
                : CampaignExercises.CompleteStageEntryMovement(session, audience, Hash("stage2-movement-completion")));
            session = Ok(isSecondSide
                ? CampaignExercises.CompleteStageEntrySecondBreakdown(session)
                : CampaignExercises.CompleteStageEntryBreakdown(session));
            session = SubmitFinishWithoutAttack(session, audience, actingElementId,
                isSecondSide ? CampaignExercises.CompleteStageEntrySecondCombatNoAttack
                    : CampaignExercises.CompleteStageEntryCombatNoAttack);
            return FinishCycleThroughPostCombatPasses(session, audience);
        }

        private static ExerciseSession SubmitFinishWithoutAttack(ExerciseSession session,
            CampaignActionAudience audience, string actingElementId, Func<ExerciseSession, ExerciseStepResult> completeNoAttack)
        {
            var checkpoint = CampaignExercises.QueryCheckpoint(session);
            var actionId = new FinishCombatWithoutAttackAction(actingElementId).ActionId;
            var submission = new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
                checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience, actionId);
            session = Ok(CampaignExercises.SubmitContactOwnerCombatSelection(session, submission, actingElementId));
            session = Ok(completeNoAttack(session));
            return Ok(CampaignExercises.CompleteNativeReserveRelease(session));
        }
    }
}
