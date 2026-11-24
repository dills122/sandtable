using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Exercises;

// docs/work/plans/2026-11-23-stage-3-scoping-verification.md: that doc found
// the Stage 1->2 native preamble bridge (CampaignNativeInitiativeDeclarationV49
// through CampaignNativeNoObligationFleetRepairV54) resolves OperationStage
// from the real Cna1979LandSequenceV4 catalog position, not a literal, so the
// Stage 2->3 bridge should fire through the identical predecessor-chain match
// with zero new native types -- "provided the predecessor-chain type matching
// resolves correctly", which the doc flagged as its one empirically unproven
// assumption (Stage 2's own second-side close-out had not landed yet when it
// was written). MTS-TASK-005/006 have since landed both acting sides' own
// Reserve/Movement/Breakdown/Combat Decision/Assault/No-Attack for Stage 2.
//
// This test drives a REAL session (not a synthetic predecessor, unlike
// CampaignNativeStageEntryPreambleTests/CampaignNativeStageEntryReserveTests,
// which predate Stage 2's own close-out existing at all) through Operation
// Stage 1 (both sides) and Stage 2 (both sides, preamble chain included), all
// via the real Core dispatch (CampaignExercises/ExerciseSession -- the same
// internals the Host itself delegates to; the Host has wired only Stage 2's
// own Initiative Declaration to real HTTP so far, MTS-TASK-003, not the rest
// of the six-event preamble chain nor Stage 2's own Reserve/Movement/
// Breakdown/Combat for either side), to reach a REAL terminal second-side
// Post Combat Pass at Operation Stage 2. That real event (not a hand-built
// record) is then fed directly into CampaignNativeInitiativeDeclarationV49.Create
// to confirm, empirically, that it advances cleanly into Operation Stage 3
// with no change to any native class -- the doc's one unproven "provided".
public sealed class CampaignStageThreeEntryVerificationTests
{
    [Fact]
    public void StageTwoSecondSideCloseOutFeedsInitiativeDeclarationIntoOperationStageThree()
    {
        var request = CreateRequest();
        var session = CampaignExercises.Begin(request).Session!;

        // Operation Stage 1's first acting side: the original, pre-#394
        // legacy Reserve/Movement mechanic (CampaignLegalActions.Query/
        // CampaignExercises.Submit), same path CampaignCombatExerciseAssaultTests.
        // AtCombatPosition already proves reaches Combat Position-Determination,
        // just without ever picking a reserve/move action here -- nobody needs
        // to be in reserve or move for a voluntary "finish without attack".
        session = AdvanceFirstSideToCombatPositionDetermination(session);
        // Which side actually acts first in Operation Stage 1 was itself a
        // legal-action-set choice (ActFirstAction/ActLastAction) the generic
        // "pick whatever's offered" loop above already made automatically --
        // read the real resolved order back rather than assuming it matches
        // whoever holds initiative.
        var stage1Order = session.CurrentSnapshot.OperationStageOrders.Single(value => value.OperationStage == 1);
        var stage1FirstAudience = stage1Order.FirstSide == LandSide.Axis
            ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var stage1SecondAudience = stage1FirstAudience == CampaignActionAudience.Axis
            ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        var stage1FirstElementId = stage1Order.FirstSide == LandSide.Axis ? "axis-infantry" : "commonwealth-infantry";
        var stage1SecondElementId = stage1Order.FirstSide == LandSide.Axis ? "commonwealth-infantry" : "axis-infantry";

        session = SubmitFinishWithoutAttack(session, stage1FirstAudience, stage1FirstElementId,
            CampaignExercises.CompleteNativeCombatNoAttack);
        session = FinishCycleThroughPostCombatPasses(session, stage1FirstAudience);
        Assert.Null(session.CurrentSecondAssault);

        // Operation Stage 1's second (and last) acting side: the newer
        // (role-as-data generic) V43-V47 chain, same shape Stage 2 itself
        // reuses unchanged.
        session = CloseOutSecondSideChain(session, stage1SecondAudience, stage1SecondElementId);
        Assert.NotNull(session.CurrentSecondAssault);
        Assert.True(session.CurrentSecondAssault!.PostCombatPasses.Count > 0);
        Assert.Equal("land.position.operation-2.initiative-declaration",
            CampaignExercises.QueryCheckpoint(session).PositionId);

        var gameTurn = session.CurrentSnapshot.Setup.InitialGameTurn;
        // Stage 2's own first/second acting side is this test's own choice
        // (actFirst below), independent of how Operation Stage 1 resolved it
        // above -- the holder is fixed for the whole Game Turn (frozen at
        // genesis), so it is read directly rather than re-derived.
        var holder = session.CurrentSnapshot.InitiativeHolder
            ?? throw new InvalidOperationException("Initiative holder is unresolved.");
        var holderAudience = holder == LandSide.Axis
            ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var otherAudience = holderAudience == CampaignActionAudience.Axis
            ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        var holderElementId = holder == LandSide.Axis ? "axis-infantry" : "commonwealth-infantry";
        var otherElementId = holder == LandSide.Axis ? "commonwealth-infantry" : "axis-infantry";

        // Stage 2's Initiative Declaration: the one preamble step the Host
        // has wired to real HTTP (MTS-TASK-003). Still reached here through
        // the Core dispatch directly, matching the rest of this test.
        var declareResult = CampaignExercises.SubmitStageEntryInitiativeDeclaration(
            session, holderAudience, Hash("stage2-initiative-declaration"), actFirst: true);
        Assert.True(declareResult.IsAccepted, declareResult.RejectionReason.ToString());
        session = declareResult.SuccessorSession!;
        Assert.Equal(2, session.CurrentInitiativeDeclaration!.OperationStage);

        // The five remaining preamble mechanics have no owner decision and no
        // Host wiring yet (CampaignExercises.SubmitStageEntryInitiativeDeclaration's
        // own remarks) -- advanced directly via their own real native Create
        // methods and ExerciseSession.WithXxx, the same chain shape
        // CampaignNativeStageEntryPreambleTests proves in isolation, here fed
        // from this real driven session instead of a synthetic predecessor.
        var weather = CampaignNativeWeatherDeterminationV50.Create(session.CurrentInitiativeDeclaration!,
            session.CurrentSnapshot.RandomState, gameTurn, session.CurrentContacts.AuthorityVersion);
        session = session.WithStageEntryWeather(weather, session.CurrentContacts.AdvanceWeatherDetermination(weather));

        var organization = CampaignNativeNoObligationOrganizationV51.Create(weather, weather.StateVersion);
        session = session.WithNoObligationOrganization(organization,
            session.CurrentContacts.AdvanceNoObligationOrganization(organization));

        var convoyArrival = CampaignNativeNoObligationNavalConvoyArrivalV52.Create(organization,
            session.Context, organization.StateVersion);
        session = session.WithNoObligationNavalConvoyArrival(convoyArrival,
            session.CurrentContacts.AdvanceNoObligationNavalConvoyArrival(convoyArrival));

        var fleetAssignment = CampaignNativeNoObligationFleetAssignmentV53.Create(
            convoyArrival, convoyArrival.StateVersion);
        session = session.WithNoObligationFleetAssignment(fleetAssignment,
            session.CurrentContacts.AdvanceNoObligationFleetAssignment(fleetAssignment));

        var fleetRepair = CampaignNativeNoObligationFleetRepairV54.Create(fleetAssignment, fleetAssignment.StateVersion);
        session = session.WithNoObligationFleetRepair(fleetRepair,
            session.CurrentContacts.AdvanceNoObligationFleetRepair(fleetRepair));
        Assert.Equal(2, fleetRepair.ToOrder().OperationStage);

        // Stage 2's own first-acting-side close-out: Reserve (finish, nobody
        // designated) -> Movement completion -> Breakdown -> Combat (finish
        // without attack) -> Cycle Control Finish -> Truck Convoy -> four
        // Post Combat Passes. Core-level direct dispatch only -- no Host
        // wiring exists yet for any of this.
        session = CloseOutStageEntrySide(session, holderAudience, holderElementId, isSecondSide: false);
        Assert.NotNull(session.CurrentStageEntryAssault);
        Assert.True(session.CurrentStageEntryAssault!.PostCombatPasses.Count > 0);

        // Stage 2's own SECOND-acting-side close-out -- the exact fact this
        // test exists to drive to, for real, for the first time.
        session = CloseOutStageEntrySide(session, otherAudience, otherElementId, isSecondSide: true);
        Assert.NotNull(session.CurrentStageEntrySecondAssault);
        Assert.True(session.CurrentStageEntrySecondAssault!.PostCombatPasses.Count > 0);

        // The real terminal: Stage 2's own second-acting side's last Post
        // Combat Pass (Patrol), at Operation Stage 2 -- not hand-built, not
        // synthetic. This is the exact fact
        // CampaignNativeInitiativeDeclarationV49.Create's predecessor switch
        // must match, unchanged, to proceed into Operation Stage 3.
        var realPredecessor = session.CurrentStageEntrySecondAssault!.PostCombatPasses[^1];
        var predecessorPosition = Cna1979LandSequenceV4.CreateTurn(gameTurn)
            .Single(value => value.PositionId == realPredecessor.PositionId);
        Assert.Equal(2, predecessorPosition.OperationStage);
        Assert.Equal(LandPhaseIds.Patrol, predecessorPosition.PhaseId);
        Assert.Equal(LandActorRole.SecondActingSide, predecessorPosition.ActorRole);

        // Zero new native types, zero changes to CampaignNativeInitiativeDeclarationV49:
        // this is the unmodified Create method, called with a real Stage-2-produced
        // predecessor instead of a synthetic one.
        var stageThreeDeclared = CampaignNativeInitiativeDeclarationV49.Create(
            realPredecessor, holder, actFirst: true, gameTurn, realPredecessor.StateVersion);

        Assert.Equal(3, stageThreeDeclared.OperationStage);
        Assert.Equal(realPredecessor.NextPositionId, stageThreeDeclared.PositionId);
        Assert.Equal(realPredecessor.WorldHash, stageThreeDeclared.WorldHash);
        Assert.Contains("operation-3", stageThreeDeclared.PositionId, StringComparison.Ordinal);
        Assert.Contains("weather-determination", stageThreeDeclared.NextPositionId, StringComparison.Ordinal);

        var bytes = stageThreeDeclared.SerializeCanonical();
        Assert.Equal(stageThreeDeclared, CampaignNativeInitiativeDeclarationV49.Deserialize(bytes));
    }

    private static CampaignCreationRequest CreateRequest()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(
            Cna1979BreakdownSetupCatalog.Definitions.Single(value => value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
        return new CampaignCreationRequest(1, "campaign-stage-three-entry-verification", Cna1979Ruleset.Manifest.Hash,
            12345, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId,
            setup.Content.Pack.Hash, setup.Content.ScenarioId);
    }

    // Every Submit method whose receipt carries a real (non-System) audience
    // requires its actionId to be an actual sha256 content hash
    // (ContentContractGuards.RequireSha256, via CampaignActionAcceptanceReceipt's
    // own constructor) -- a plain label is rejected. This test's own labels
    // never need to match a real legal-action-set entry (unlike
    // FinishCombatWithoutAttackAction's own ActionId, which must and is
    // computed directly from CampaignActionCandidate's own rule instead), so
    // any stable hash of the label satisfies the contract.
    private static string Hash(string label) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(label)));

    // Operation Stage 1's first acting side's own pre-#394 legacy Reserve and
    // Movement: picks whatever first non-move candidate is offered (System
    // auto-resolutions included) until Combat's own Position-Determination
    // step is reached, same loop shape as
    // CampaignCombatExerciseAssaultTests.AtCombatPosition, minus that test's
    // own reserve-designation pick (nobody needs to be in reserve here).
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

    // After a side's own Combat decision, Cycle Control Finish -> Truck
    // Convoy -> four Post Combat Passes close that side's own Rail/Repair/
    // Patrol block, generically dispatched (CurrentSecondAssault/
    // CurrentStageEntryAssault/CurrentStageEntrySecondAssault/fallback) by
    // every one of these CampaignExercises methods already.
    private static ExerciseSession FinishCycleThroughPostCombatPasses(ExerciseSession session,
        CampaignActionAudience audience)
    {
        var cycleControlResult = CampaignExercises.SubmitNativeCycleControl(session, audience,
            CampaignNativeCycleControlChoiceV28.Finish, Hash("cycle-control-finish"));
        Assert.True(cycleControlResult.IsAccepted, cycleControlResult.RejectionReason.ToString());
        session = cycleControlResult.SuccessorSession!;

        var convoyPassResult = CampaignExercises.SubmitTruckConvoyPass(session, audience, Hash("truck-convoy-pass"));
        Assert.True(convoyPassResult.IsAccepted, convoyPassResult.RejectionReason.ToString());
        session = convoyPassResult.SuccessorSession!;

        for (var index = 0; index < 4; index++)
        {
            var passResult = CampaignExercises.SubmitPostCombatPass(session, audience, Hash($"post-combat-pass-{index}"));
            Assert.True(passResult.IsAccepted, passResult.RejectionReason.ToString());
            session = passResult.SuccessorSession!;
        }
        return session;
    }

    // Operation Stage 1's second acting side's own Reserve (finish, nobody
    // designated) -> Movement completion -> Breakdown -> Combat (finish
    // without attack) -> its own Cycle Control/Truck Convoy/Post Combat Pass
    // close-out. The same CampaignNativeSecondReserveV43-V47 chain Stage 2
    // itself reuses unchanged.
    private static ExerciseSession CloseOutSecondSideChain(ExerciseSession session,
        CampaignActionAudience audience, string actingElementId)
    {
        var reserveResult = CampaignExercises.SubmitSecondReserve(session, audience, Hash("second-reserve"), elementId: null);
        Assert.True(reserveResult.IsAccepted, reserveResult.RejectionReason.ToString());
        session = reserveResult.SuccessorSession!;

        var moveCompletionResult = CampaignExercises.CompleteSecondMovement(session, audience,
            Hash("second-movement-completion"));
        Assert.True(moveCompletionResult.IsAccepted, moveCompletionResult.RejectionReason.ToString());
        session = moveCompletionResult.SuccessorSession!;

        var breakdownResult = CampaignExercises.CompleteSecondBreakdown(session);
        Assert.True(breakdownResult.IsAccepted, breakdownResult.RejectionReason.ToString());
        session = breakdownResult.SuccessorSession!;

        session = SubmitFinishWithoutAttack(session, audience, actingElementId,
            CampaignExercises.CompleteSecondCombatNoAttack);
        return FinishCycleThroughPostCombatPasses(session, audience);
    }

    // One acting side's own full close-out for Stage 2 (first or second),
    // reusing CampaignNativeSecondReserveV43/V44/V45/V46/V47 unchanged -- the
    // StageEntry/StageEntrySecond-prefixed CampaignExercises dispatch
    // siblings, same shape proven for Operation Stage 1's own two sides.
    private static ExerciseSession CloseOutStageEntrySide(ExerciseSession session,
        CampaignActionAudience audience, string actingElementId, bool isSecondSide)
    {
        var reserveResult = isSecondSide
            ? CampaignExercises.SubmitStageEntrySecondReserve(session, audience, Hash("stage2-reserve"), elementId: null)
            : CampaignExercises.SubmitStageEntryReserve(session, audience, Hash("stage2-reserve"), elementId: null);
        Assert.True(reserveResult.IsAccepted, reserveResult.RejectionReason.ToString());
        session = reserveResult.SuccessorSession!;

        var moveCompletionResult = isSecondSide
            ? CampaignExercises.CompleteStageEntrySecondMovement(session, audience, Hash("stage2-movement-completion"))
            : CampaignExercises.CompleteStageEntryMovement(session, audience, Hash("stage2-movement-completion"));
        Assert.True(moveCompletionResult.IsAccepted, moveCompletionResult.RejectionReason.ToString());
        session = moveCompletionResult.SuccessorSession!;

        var breakdownResult = isSecondSide
            ? CampaignExercises.CompleteStageEntrySecondBreakdown(session)
            : CampaignExercises.CompleteStageEntryBreakdown(session);
        Assert.True(breakdownResult.IsAccepted, breakdownResult.RejectionReason.ToString());
        session = breakdownResult.SuccessorSession!;

        session = SubmitFinishWithoutAttack(session, audience, actingElementId,
            isSecondSide ? CampaignExercises.CompleteStageEntrySecondCombatNoAttack
                : CampaignExercises.CompleteStageEntryCombatNoAttack);
        return FinishCycleThroughPostCombatPasses(session, audience);
    }

    // Submits "finish combat without attack" for the given side/element via
    // the real Core dispatch (CampaignExercises.SubmitContactOwnerCombatSelection),
    // which already generically covers all four occurrences (Operation Stage
    // 1's two sides, Stage 2's two sides) via CurrentStageEntrySecondBreakdown/
    // CurrentStageEntryBreakdown/CurrentSecondBreakdown dispatch.
    // FinishCombatWithoutAttackAction's own ActionId is a pure function of
    // actingElementId (CampaignActionCandidate.CalculateId over a fixed
    // "kind"+contractVersion+actingElementId JSON blob) -- it does not depend
    // on World, Position or session state, so it can be computed directly
    // here instead of re-deriving the whole combat-selection cut.
    //
    // Selecting "finish" only records the Decision; a separate System-audience
    // completion (CompleteNativeCombatNoAttack/CompleteSecondCombatNoAttack/
    // CompleteStageEntryCombatNoAttack/CompleteStageEntrySecondCombatNoAttack,
    // one per occurrence, same four-way split every other mechanic in this
    // chain uses) is what actually initializes Reserve Release -- without it
    // Cycle Control's own QueryNativeReserveReleaseState finds no completion
    // and this rejects. Reserve Release itself then needs its own explicit
    // completion too (nobody was ever designated into reserve, so this
    // resolves immediately with no member choices).
    private static ExerciseSession SubmitFinishWithoutAttack(ExerciseSession session,
        CampaignActionAudience audience, string actingElementId,
        Func<ExerciseSession, ExerciseStepResult> completeNoAttack)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        var actionId = new FinishCombatWithoutAttackAction(actingElementId).ActionId;
        var submission = new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience, actionId);
        var result = CampaignExercises.SubmitContactOwnerCombatSelection(session, submission, actingElementId);
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        session = result.SuccessorSession!;

        var completionResult = completeNoAttack(session);
        Assert.True(completionResult.IsAccepted, completionResult.RejectionReason.ToString());
        session = completionResult.SuccessorSession!;

        var releaseResult = CampaignExercises.CompleteNativeReserveRelease(session);
        Assert.True(releaseResult.IsAccepted, releaseResult.RejectionReason.ToString());
        return releaseResult.SuccessorSession!;
    }
}
