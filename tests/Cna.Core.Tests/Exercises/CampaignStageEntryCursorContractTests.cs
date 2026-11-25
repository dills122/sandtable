using System.Security.Cryptography;
using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Exercises;

// Separate real-session fixture for the cursor contract. The existing bridge
// verification remains unchanged and independently covers the complete preamble.
public sealed class CampaignStageEntryCursorContractTests
{
    [Fact]
    public void BothPreamblesDeriveCursorAndHolderAndRejectTamperedReplay()
    {
        var request = CreateRequest();
        var session = CampaignExercises.Begin(request).Session!;

        session = AdvanceFirstSideToCombatPositionDetermination(session);
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

        session = CloseOutSecondSideChain(session, stage1SecondAudience, stage1SecondElementId);
        Assert.NotNull(session.CurrentSecondAssault);
        Assert.True(session.CurrentSecondAssault!.PostCombatPasses.Count > 0);
        Assert.Equal("land.position.operation-2.initiative-declaration",
            CampaignExercises.QueryCheckpoint(session).PositionId);

        var gameTurn = session.CurrentSnapshot.Setup.InitialGameTurn;
        var holder = session.CurrentSnapshot.InitiativeHolder
            ?? throw new InvalidOperationException("Initiative holder is unresolved.");
        var holderAudience = holder == LandSide.Axis
            ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var otherAudience = holderAudience == CampaignActionAudience.Axis
            ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        var holderElementId = holder == LandSide.Axis ? "axis-infantry" : "commonwealth-infantry";
        var otherElementId = holder == LandSide.Axis ? "commonwealth-infantry" : "axis-infantry";

        var wrongHolder = holder == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;
        var wrongDeclaration = CampaignNativeInitiativeDeclarationV49.Create(
            session.CurrentSecondAssault!.PostCombatPasses[^1], wrongHolder, actFirst: true,
            gameTurn, session.CurrentContacts.AuthorityVersion);
        var tamperedDeclaration = session.WithInitiativeDeclaration(wrongDeclaration,
            session.CurrentContacts.AdvanceInitiativeDeclaration(wrongDeclaration));
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory,
            CampaignExercises.Reconstruct(tamperedDeclaration).FailureReason);

        var declareResult = CampaignExercises.SubmitStageEntryInitiativeDeclaration(
            session, holderAudience, Hash("stage2-initiative-declaration"), actFirst: true);
        Assert.True(declareResult.IsAccepted, declareResult.RejectionReason.ToString());
        session = declareResult.SuccessorSession!;
        Assert.Equal(2, session.CurrentInitiativeDeclaration!.OperationStage);

        var stageTwoInitiative = session;
        var initialRandom = session.CurrentSnapshot.RandomState;
        var wrongCursor = new RandomStreamState(initialRandom.ContractVersion, initialRandom.AlgorithmId,
            initialRandom.Seed, initialRandom.NextByteCursor + 1);
        var wrongWeather = CampaignNativeWeatherDeterminationV50.Create(session.CurrentInitiativeDeclaration!,
            wrongCursor, gameTurn, session.CurrentContacts.AuthorityVersion);
        var tamperedWeather = session.WithStageEntryWeather(wrongWeather,
            session.CurrentContacts.AdvanceWeatherDetermination(wrongWeather));
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory,
            CampaignExercises.Reconstruct(tamperedWeather).FailureReason);

        var weatherResult = CampaignExercises.CompleteStageEntryWeather(session);
        Assert.True(weatherResult.IsAccepted, weatherResult.RejectionReason.ToString());
        session = weatherResult.SuccessorSession!;
        var weather = session.CurrentStageEntryWeather!;
        Assert.Equal(initialRandom.NextByteCursor, weather.PriorRandomCursor);
        Assert.Same(stageTwoInitiative, session.StageEntryWeatherPredecessor);
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);
        Assert.False(CampaignExercises.CompleteStageEntryWeather(session).IsAccepted);

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

        session = CloseOutStageEntrySide(session, holderAudience, holderElementId, isSecondSide: false);
        Assert.NotNull(session.CurrentStageEntryAssault);
        Assert.True(session.CurrentStageEntryAssault!.PostCombatPasses.Count > 0);
        Assert.Equal(weather.ResultRandomCursor, session.CurrentStageEntryCombatDecision!.RandomState.NextByteCursor);
        Assert.Equal(weather.ResultRandomCursor, ExerciseSession.SecondAssaultFrame(
            session.CurrentStageEntryCombatDecision, session.CurrentSnapshot, session.CurrentHistory).RandomState.NextByteCursor);

        session = CloseOutStageEntrySide(session, otherAudience, otherElementId, isSecondSide: true);
        Assert.NotNull(session.CurrentStageEntrySecondAssault);
        Assert.True(session.CurrentStageEntrySecondAssault!.PostCombatPasses.Count > 0);
        Assert.Equal(weather.ResultRandomCursor, session.CurrentStageEntrySecondCombatDecision!.RandomState.NextByteCursor);

        var realPredecessor = session.CurrentStageEntrySecondAssault!.PostCombatPasses[^1];
        var predecessorPosition = Cna1979LandSequenceV4.CreateTurn(gameTurn)
            .Single(value => value.PositionId == realPredecessor.PositionId);
        Assert.Equal(2, predecessorPosition.OperationStage);
        Assert.Equal(LandPhaseIds.Patrol, predecessorPosition.PhaseId);
        Assert.Equal(LandActorRole.SecondActingSide, predecessorPosition.ActorRole);

        var stageThreeDeclared = CampaignNativeInitiativeDeclarationV49.Create(
            realPredecessor, holder, actFirst: false, gameTurn, realPredecessor.StateVersion);

        Assert.Equal(3, stageThreeDeclared.OperationStage);
        Assert.Equal(realPredecessor.NextPositionId, stageThreeDeclared.PositionId);
        Assert.Equal(realPredecessor.WorldHash, stageThreeDeclared.WorldHash);
        Assert.Contains("operation-3", stageThreeDeclared.PositionId, StringComparison.Ordinal);
        Assert.Contains("weather-determination", stageThreeDeclared.NextPositionId, StringComparison.Ordinal);

        var bytes = stageThreeDeclared.SerializeCanonical();
        Assert.Equal(stageThreeDeclared, CampaignNativeInitiativeDeclarationV49.Deserialize(bytes));

        var stageTwoTerminal = session;
        var wrongThirdDeclaration = CampaignNativeInitiativeDeclarationV49.Create(
            realPredecessor, wrongHolder, actFirst: false, gameTurn, realPredecessor.StateVersion);
        var tamperedThirdDeclaration = session.WithThirdStageInitiativeDeclaration(wrongThirdDeclaration,
            session.CurrentContacts.AdvanceInitiativeDeclaration(wrongThirdDeclaration));
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory,
            CampaignExercises.Reconstruct(tamperedThirdDeclaration).FailureReason);
        Assert.True(CampaignExercises.Reconstruct(stageTwoTerminal).IsVerified, "Stage 2 terminal did not replay.");
        session = session.WithThirdStageInitiativeDeclaration(stageThreeDeclared,
            session.CurrentContacts.AdvanceInitiativeDeclaration(stageThreeDeclared));
        Assert.True(CampaignExercises.Reconstruct(session).IsVerified);

        var wrongThirdCursor = new RandomStreamState(initialRandom.ContractVersion, initialRandom.AlgorithmId,
            initialRandom.Seed, weather.ResultRandomCursor + 1);
        var wrongThirdWeather = CampaignNativeWeatherDeterminationV50.Create(stageThreeDeclared,
            wrongThirdCursor, gameTurn, session.CurrentContacts.AuthorityVersion);
        var tamperedThirdWeather = session.WithThirdStageWeather(wrongThirdWeather,
            session.CurrentContacts.AdvanceWeatherDetermination(wrongThirdWeather));
        Assert.Equal(ExerciseReconstructionFailureReason.InvalidHistory,
            CampaignExercises.Reconstruct(tamperedThirdWeather).FailureReason);

        var predecessor = session;
        var thirdWeatherResult = CampaignExercises.CompleteStageEntryWeather(session);
        Assert.True(thirdWeatherResult.IsAccepted, thirdWeatherResult.RejectionReason.ToString());
        session = thirdWeatherResult.SuccessorSession!;
        var thirdWeather = session.CurrentThirdStageWeather!;
        Assert.Equal(weather.ResultRandomCursor, thirdWeather.PriorRandomCursor);
        Assert.Same(predecessor, session.ThirdStageWeatherPredecessor);
        Assert.False(CampaignExercises.CompleteStageEntryWeather(session).IsAccepted);

        var reconstructed = CampaignExercises.Reconstruct(session);
        Assert.True(reconstructed.IsVerified);
        Assert.Equal(thirdWeather.SerializeCanonical(), reconstructed.ReconstructedSnapshotBytes);
    }

    private static CampaignCreationRequest CreateRequest()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(
            Cna1979BreakdownSetupCatalog.Definitions.Single(value => value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
        return new CampaignCreationRequest(1, "campaign-stage-entry-cursor-contract", Cna1979Ruleset.Manifest.Hash,
            12345, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId,
            setup.Content.Pack.Hash, setup.Content.ScenarioId);
    }

    internal static string Hash(string label) =>
        "sha256:" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(label)));

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

    internal static ExerciseSession FinishCycleThroughPostCombatPasses(ExerciseSession session,
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
