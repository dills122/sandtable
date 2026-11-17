using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiSourceAuthorityTests
{
    private static readonly string[] ExpectedGarrisonRemainderIds =
        ["giarabub-antitank", "giarabub-artillery", "giarabub-infantry", "giarabub-machine-gun",
            "soluch-antitank-company", "soluch-artillery-regiment"];
    [Theory]
    [InlineData(0UL, 3, 1)]
    [InlineData(14UL, 0, 0)]
    public void AxisFinalizesRealSupplyPlanAndReplaysWithoutStartingGame(ulong seed, int maximum, int allocated)
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var initialization = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "source-authority-transition", Cna1979Ruleset.Manifest.Hash, seed,
            CampaignPregameSourceCreation.SetupId,
            CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, initialization),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var session = CampaignExercises.Begin(request, artifact, initialization).Session!;
        var polishActionPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        var polishAction = new CampaignSourceTurnAction(17, "e1730");
        var polishSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            polishActionPosition.PositionId, CampaignActionAudience.Commonwealth, [polishAction], CampaignLegalActionSet.SourceTurnPolicyId);
        var polishBytes = CampaignLegalActionSerializer.Serialize(polishSet);
        Assert.Equal(polishSet, CampaignLegalActionSerializer.DeserializeCanonical(polishBytes));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            polishActionPosition.PositionId, CampaignActionAudience.Axis, [polishAction], CampaignLegalActionSet.SourceTurnPolicyId));
        var arrivalPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        var declineAttack = CampaignSourceTurnAction.DeclineGt1Lane2Attack(17);
        var arrivalSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            arrivalPosition.PositionId, CampaignActionAudience.Commonwealth, [declineAttack], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(arrivalSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(arrivalSet)));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            arrivalPosition.PositionId, CampaignActionAudience.Axis, [declineAttack], CampaignLegalActionSet.SourceTurnPolicyId));
        var convoyPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.PhaseId == LandPhaseIds.NavalConvoySchedule);
        var convoyRoll = CampaignSourceTurnAction.RollGt3Supply(17);
        var convoySet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Axis, [convoyRoll], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(convoySet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(convoySet)));
        var convoyChoices = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Axis,
            [CampaignSourceTurnAction.AdjustGt3Supply(17, 2, 1, 1), CampaignSourceTurnAction.FinishGt3Supply(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(convoyChoices, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(convoyChoices)));
        var cwRoll = CampaignSourceTurnAction.RollGt5Supply(17);
        var cwConvoySet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Commonwealth, [cwRoll], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(cwConvoySet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(cwConvoySet)));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Axis, [cwRoll], CampaignLegalActionSet.SourceTurnPolicyId));
        var shippingPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.PhaseId == LandPhaseIds.TacticalShipping);
        var shippingSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            shippingPosition.PositionId, CampaignActionAudience.Commonwealth, [CampaignSourceTurnAction.DeclineTacticalShipping(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(shippingSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(shippingSet)));
        var declarationPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1 && value.PhaseId == LandPhaseIds.InitiativeDeclaration);
        var declarationSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            declarationPosition.PositionId, CampaignActionAudience.Axis, [CampaignSourceTurnAction.ActFirst(17), CampaignSourceTurnAction.ActLast(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(declarationSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(declarationSet)));
        var organizationPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1 && value.PhaseId == LandPhaseIds.Organization);
        var organizationSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            organizationPosition.PositionId, CampaignActionAudience.Commonwealth, [CampaignSourceTurnAction.DeclineVoluntaryOrganization(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(organizationSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(organizationSet)));
        var interruptTraining = CampaignSourceTurnAction.InterruptSixthAustralianTraining(17);
        var trainingSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            organizationPosition.PositionId, CampaignActionAudience.Commonwealth, [interruptTraining], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(trainingSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(trainingSet)));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            organizationPosition.PositionId, CampaignActionAudience.Axis, [interruptTraining], CampaignLegalActionSet.SourceTurnPolicyId));
        var unknownTraining = CampaignSourceTrainingLedger.CreateSixthAustralian();
        var interruptedTraining = unknownTraining.Interrupt();
        Assert.True(interruptedTraining.Interrupted);
        Assert.Null(interruptedTraining.CurrentMorale);
        Assert.Null(interruptedTraining.QualifyingStages);
        Assert.NotEqual(unknownTraining.Hash(), interruptedTraining.Hash());
        Assert.Throws<InvalidOperationException>(() => interruptedTraining.Interrupt());
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Commonwealth, [convoyRoll], CampaignLegalActionSet.SourceTurnPolicyId));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Axis));
        var finishBeforeRoll = Submission(session, CampaignSourceAuthorityState.FinishActionId, CampaignActionAudience.Axis);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.FinishPregameAxisSupply(session, finishBeforeRoll).RejectionReason);
        var roll = Assert.Single(CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates);
        session = CampaignExercises.Submit(session, Submission(session, roll.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        if (allocated > 0)
        {
            var add = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates
                .OfType<PregameAxisSupplyAllocationAction>()
                .Single(value => value.Lane == 2 && value.Stage == 1 && value.Delta == 1);
            session = CampaignExercises.Submit(session, Submission(session, add.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        var legacyOffers = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates.Select(x => x.ActionId).ToArray();
        var draft = CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Axis);
        Assert.Single(draft.PrivateActionIds);
        Assert.DoesNotContain(draft.PrivateActionIds[0], legacyOffers);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.FinishPregameAxisSupply(session, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Commonwealth)).RejectionReason);
        var accepted = CampaignExercises.FinishPregameAxisSupply(session, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Axis));
        Assert.True(accepted.IsAccepted);
        var next = accepted.SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(next).IsVerified);
        var giarabubAction = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c1014", 1);
        var oldAxisMotorHash = next.SourceAuthority!.AxisMotorization.Hash();
        var oldAxisChoices = CampaignExercises.QuerySourceMotorizationDraft(next, CampaignActionAudience.Axis);
        Assert.DoesNotContain(giarabubAction, oldAxisChoices.PrivateActionIds);
        var admittedPolicy = CampaignExercises.AdmitSourceGiarabubMotorPlacementPolicy(next).SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceGiarabubMotorPlacementPolicyAdmitted(admittedPolicy));
        Assert.NotEqual(oldAxisMotorHash, admittedPolicy.SourceAuthority!.AxisMotorization.Hash());
        Assert.Equal(oldAxisMotorHash, next.SourceAuthority.AxisMotorization.Hash());
        Assert.DoesNotContain(giarabubAction, CampaignExercises.QuerySourceMotorizationDraft(next, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Contains(giarabubAction, CampaignExercises.QuerySourceMotorizationDraft(admittedPolicy, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.DoesNotContain(giarabubAction, CampaignExercises.QuerySourceMotorizationDraft(admittedPolicy, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.AdjustSourceMotorization(admittedPolicy,
            Submission(admittedPolicy, giarabubAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        var placedMotor = CampaignExercises.AdjustSourceMotorization(admittedPolicy,
            Submission(admittedPolicy, giarabubAction, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(124, placedMotor.SourceAuthority!.AxisMotorization.Remaining("axis-libya-unattached"));
        Assert.Contains(placedMotor.SourceAuthority.AxisMotorization.Allocations,
            value => value.PoolId == "axis-libya-unattached" && value.LocationId == "c1014" && value.Points == 1);
        Assert.True(CampaignExercises.Reconstruct(placedMotor).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceGiarabubMotorPlacementPolicy(placedMotor).RejectionReason);
        var cwBeforeFinish = CampaignExercises.QuerySourceActionsDraft(next, CampaignActionAudience.Commonwealth);
        var finishOwner = CampaignExercises.Query(next, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var ownerFinished = CampaignExercises.Submit(next, Submission(next, finishOwner.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(1, ownerFinished.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.FinalizedLegacyCheckpointContractVersion, CampaignExercises.QueryCheckpoint(ownerFinished).ContractVersion);
        Assert.Equal(next.SourceAuthority!.RandomState, ownerFinished.SourceAuthority.RandomState);
        Assert.Equal(next.SourceAuthority.World.PendingArrivals, ownerFinished.SourceAuthority.World.PendingArrivals);
        Assert.Equal(cwBeforeFinish.VisibleBytes, CampaignExercises.QuerySourceActionsDraft(ownerFinished, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.PlaceSourceAxisDump(ownerFinished,
            Submission(ownerFinished, CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507"), CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceCompactCheckpoint(ownerFinished).RejectionReason);
        var cwWhileAxisFinished = CampaignExercises.PlaceSourceCommonwealthSupply(ownerFinished,
            Submission(ownerFinished, CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-dump-1", "d3612"), CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, cwWhileAxisFinished.SourceAuthority!.FinalizedOwners);
        var finishCw = CampaignExercises.Query(cwWhileAxisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var bothFinished = CampaignExercises.Submit(cwWhileAxisFinished, Submission(cwWhileAxisFinished, finishCw.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(3, bothFinished.SourceAuthority!.FinalizedOwners);
        Assert.True(CampaignExercises.Reconstruct(bothFinished).IsVerified);
        var reopenAxis = CampaignExercises.Query(bothFinished, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var reopenedAxis = CampaignExercises.Submit(bothFinished, Submission(bothFinished, reopenAxis.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, reopenedAxis.SourceAuthority!.FinalizedOwners);
        Assert.True(CampaignExercises.Reconstruct(reopenedAxis).IsVerified);
        var reopenCw = CampaignExercises.Query(reopenedAxis, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var reopenedBoth = CampaignExercises.Submit(reopenedAxis, Submission(reopenedAxis, reopenCw.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(0, reopenedBoth.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.FinalizedLegacyCheckpointContractVersion, CampaignExercises.QueryCheckpoint(reopenedBoth).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(reopenedBoth).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.PlaceSourceCommonwealthSupply(reopenedAxis,
            Submission(reopenedAxis, CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-cairo-1", "e1730"), CampaignActionAudience.Commonwealth)).RejectionReason);
        var lockedCwView = CampaignExercises.QuerySourceActionsDraft(reopenedAxis, CampaignActionAudience.Commonwealth).VisibleBytes;
        var changedAxis = CampaignExercises.PlaceSourceAxisDump(reopenedAxis,
            Submission(reopenedAxis, CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507"), CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, changedAxis.SourceAuthority!.FinalizedOwners);
        Assert.Equal(lockedCwView, CampaignExercises.QuerySourceActionsDraft(changedAxis, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.True(CampaignExercises.Reconstruct(changedAxis).IsVerified);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourcePolishArrivalDraft(next, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.SubmitSourcePolishArrival(next, Submission(next, roll.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var earlyCompact = CampaignExercises.AdmitSourceCompactCheckpoint(next).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(earlyCompact).ContractVersion);
        var compactFinish = CampaignExercises.Query(earlyCompact, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var finalizedCompact = CampaignExercises.Submit(earlyCompact, Submission(earlyCompact, compactFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignSourceAuthorityState.FinalizedCompactCheckpointContractVersion, CampaignExercises.QueryCheckpoint(finalizedCompact).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(finalizedCompact).IsVerified);
        var earlyDump = CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507");
        earlyCompact = CampaignExercises.PlaceSourceAxisDump(earlyCompact,
            Submission(earlyCompact, earlyDump, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(earlyCompact).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(earlyCompact).IsVerified);
        var normalAxis = CampaignExercises.Query(next, CampaignActionAudience.Axis);
        Assert.True(normalAxis.IsSuccessful);
        Assert.Equal(CampaignLegalActionSet.SourceSetupPolicyId, normalAxis.ActionSet!.PolicyId);
        var encodedActions = CampaignLegalActionSerializer.Serialize(normalAxis.ActionSet);
        Assert.Equal(encodedActions, CampaignLegalActionSerializer.Serialize(CampaignLegalActionSerializer.DeserializeCanonical(encodedActions)));
        var unifiedDump = normalAxis.ActionSet.Candidates.OfType<CampaignSourceSetupAction>()
            .Single(value => value.Family == "axis-dump" && value.ChoiceJson.Contains("c4507", StringComparison.Ordinal)
                && value.ChoiceJson.Contains("axis-dump-1", StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(normalAxis.ActionSet.CampaignId,
            normalAxis.ActionSet.StateVersion, normalAxis.ActionSet.RulesetHash, normalAxis.ActionSet.PositionId,
            CampaignActionAudience.Axis, [unifiedDump], CampaignLegalActionSet.CurrentPolicyId));
        var ownerCut = CampaignExercises.QuerySourceActionsDraft(next, CampaignActionAudience.Axis);
        Assert.DoesNotContain(unifiedDump.ActionId, System.Text.Encoding.UTF8.GetString(ownerCut.VisibleBytes));
        var ownContext = System.Text.Encoding.UTF8.GetString(ownerCut.VisibleBytes);
        Assert.Contains("axis-gt1-supply", ownContext);
        Assert.Contains("maximumSupply", ownContext);
        var hiddenCwAction = CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-dump-1", "d3612");
        var hiddenAdvance = CampaignExercises.PlaceSourceCommonwealthSupply(next,
            Submission(next, hiddenCwAction, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var afterHidden = CampaignExercises.QuerySourceActionsDraft(hiddenAdvance, CampaignActionAudience.Axis);
        Assert.Equal(ownerCut.VisibleBytes, afterHidden.VisibleBytes);
        Assert.NotEqual(ownerCut.PrivateActionIds[0], afterHidden.PrivateActionIds[0]);
        var wrongSide = Submission(next, unifiedDump.ActionId, CampaignActionAudience.Commonwealth);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(next, wrongSide).RejectionReason);
        var unifiedResult = CampaignExercises.Submit(next, Submission(next, unifiedDump.ActionId, CampaignActionAudience.Axis));
        Assert.True(unifiedResult.IsAccepted);
        Assert.Equal(unifiedDump.ActionId, unifiedResult.Evidence!.Receipt.ActionId);
        Assert.True(CampaignExercises.Reconstruct(unifiedResult.SuccessorSession!).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(unifiedResult.SuccessorSession!,
            Submission(next, unifiedDump.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(unifiedResult.SuccessorSession!,
            Submission(unifiedResult.SuccessorSession!, unifiedDump.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.FinishPregameAxisSupply(next, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Axis)).RejectionReason);
        using var snapshot = JsonDocument.Parse(accepted.Evidence!.SnapshotCheckpoint);
        Assert.Equal("setup-in-progress", snapshot.RootElement.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, snapshot.RootElement.GetProperty("gameClock").ValueKind);
        using var plan = JsonDocument.Parse(snapshot.RootElement.GetProperty("convoyPlanBase64").GetBytesFromBase64());
        Assert.Equal(maximum, plan.RootElement.GetProperty("maximumSupply").GetInt32());
        Assert.Equal(maximum - allocated, plan.RootElement.GetProperty("unusedAvailability").GetInt32());
        using var source = JsonDocument.Parse(snapshot.RootElement.GetProperty("sourceSnapshotBase64").GetBytesFromBase64());
        Assert.Contains(source.RootElement.GetProperty("pendingArrivalGroupIds").EnumerateArray().Select(x => x.GetString()),
            id => id == Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId);
        var dumpDraft = CampaignExercises.QuerySourceAxisDumpDraft(next, CampaignActionAudience.Axis);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisDumpDraft(next, CampaignActionAudience.Commonwealth));
        Assert.Equal(8, dumpDraft.PrivateActionIds.Count);
        var chosenId = CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507");
        Assert.Contains(chosenId, dumpDraft.PrivateActionIds);
        var dumpCommand = Submission(next, chosenId, CampaignActionAudience.Axis);
        var placed = CampaignExercises.PlaceSourceAxisDump(next, dumpCommand);
        Assert.True(placed.IsAccepted);
        var after = placed.SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(after).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
            CampaignExercises.PlaceSourceAxisDump(after, dumpCommand).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceAxisDump(after, Submission(after,
                CampaignSourceAuthorityState.DumpActionId("axis-dump-2", "c4507"), CampaignActionAudience.Axis)).RejectionReason);
        var second = CampaignExercises.PlaceSourceAxisDump(after, Submission(after,
            CampaignSourceAuthorityState.DumpActionId("axis-dump-2", "c4707"), CampaignActionAudience.Axis));
        Assert.True(second.IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(second.SuccessorSession!).IsVerified);
        var corruptHistory = second.SuccessorSession!.PregameHistory.Select(value => value.ToArray()).ToArray();
        var lastEvent = System.Text.Encoding.UTF8.GetString(corruptHistory[^1]).Replace("c4707", "c4507", StringComparison.Ordinal);
        corruptHistory[^1] = System.Text.Encoding.UTF8.GetBytes(lastEvent);
        var corrupt = new ExerciseSession(second.SuccessorSession.Pregame!, second.SuccessorSession.CurrentContacts,
            corruptHistory, second.SuccessorSession.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(corrupt).IsVerified);
        using var finalSnapshot = JsonDocument.Parse(second.Evidence!.SnapshotCheckpoint);
        using var world = JsonDocument.Parse(finalSnapshot.RootElement.GetProperty("axisSupplyWorldBase64").GetBytesFromBase64());
        Assert.Equal(22, world.RootElement.GetProperty("units").GetArrayLength());
        Assert.False(world.RootElement.GetProperty("siteCoverageComplete").GetBoolean());
        Assert.Equal(JsonValueKind.Null, world.RootElement.GetProperty("units").EnumerateArray()
            .Single(x => x.GetProperty("id").GetString() == "axis-derna-1").GetProperty("ammo").ValueKind);
        var cwSession = second.SuccessorSession!;
        Assert.Equal(CampaignSourceAuthorityState.ContractVersion, cwSession.SourceAuthority!.ProfileVersion);
        Assert.True(CampaignExercises.IsSourceSetupSeat(cwSession, CampaignActionAudience.Axis));
        Assert.True(CampaignExercises.IsSourceSetupSeat(cwSession, CampaignActionAudience.Commonwealth));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Axis));
        var placements = new[]
        {
            ("cw-dump-1", "d3612"),
            ("cw-alexandria-1", "e3613"), ("cw-alexandria-2", "e3613"), ("cw-alexandria-3", "e3714"),
            ("cw-cairo-1", "e1829"), ("cw-cairo-2", "e1830"), ("cw-cairo-3", "e1930"),
        };
        foreach (var (id, location) in placements)
        {
            var idHash = CampaignSourceAuthorityState.CommonwealthSupplyActionId(id, location);
            var cwDraft = CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Commonwealth);
            Assert.Contains(idHash, cwDraft.PrivateActionIds);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCommonwealthSupply(cwSession, Submission(cwSession, idHash, CampaignActionAudience.Axis)).RejectionReason);
            var step = CampaignExercises.PlaceSourceCommonwealthSupply(cwSession, Submission(cwSession, idHash, CampaignActionAudience.Commonwealth));
            Assert.True(step.IsAccepted);
            cwSession = step.SuccessorSession!;
            Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        }
        Assert.Empty(CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.Equal(15, cwSession.SourceAuthority!.CommonwealthSupplyWorld.Units.Count);
        Assert.Equal(18, cwSession.SourceAuthority.CommonwealthInitialForces.Forces.Count);
        Assert.Null(cwSession.SourceAuthority.CommonwealthInitialForces.Forces
            .Single(value => value.ElementId == "central-india-horse").InitialToes);
        Assert.Null(cwSession.SourceAuthority.Position);
        var axisDummy = CampaignSourceAuthorityState.DummyActionId(true, "axis-dummy-1", "c4407");
        Assert.Contains(axisDummy, CampaignExercises.QuerySourceDummyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, axisDummy, CampaignActionAudience.Commonwealth)).RejectionReason);
        var dummyStep = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, axisDummy, CampaignActionAudience.Axis));
        Assert.True(dummyStep.IsAccepted);
        cwSession = dummyStep.SuccessorSession!;
        var omit = CampaignSourceAuthorityState.DummyActionId(true, "axis-dummy-2", null);
        cwSession = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, omit, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwDummy = CampaignSourceAuthorityState.DummyActionId(false, "cw-dummy-1", "d3611");
        Assert.Contains(cwDummy, CampaignExercises.QuerySourceDummyDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        cwSession = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, cwDummy, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var priorRandom = cwSession.SourceAuthority!.RandomState;
        var expectedGt1 = SandtableRandom.RollD6(priorRandom);
        var rollGt1 = CampaignSourceAuthorityState.OpeningConvoyActionId("roll", 0, 0, 0);
        Assert.Single(CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, rollGt1, CampaignActionAudience.Commonwealth)).RejectionReason);
        var gt1Step = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, rollGt1, CampaignActionAudience.Axis));
        Assert.True(gt1Step.IsAccepted);
        cwSession = gt1Step.SuccessorSession!;
        Assert.Equal(expectedGt1.Value, cwSession.SourceAuthority!.OpeningConvoyPlan.Die);
        Assert.Equal(expectedGt1.State.NextByteCursor, cwSession.SourceAuthority.RandomState.NextByteCursor);
        if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply > 0)
        {
            // Real source lane 3 under disclosed provisional Benghazi ruling.
            // This branches the same rolled plan; it does not advance a game.
            var lane3Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 3, 1, 1);
            var lane3Session = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession,
                Submission(cwSession, lane3Add, CampaignActionAudience.Axis)).SuccessorSession!;
            var lane3Finish = CampaignSourceAuthorityState.OpeningConvoyActionId("finish", 0, 0, 0);
            lane3Session = CampaignExercises.SubmitSourceAxisGt1Supply(lane3Session,
                Submission(lane3Session, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
            var lane3Source = lane3Session.SourceAuthority!;
            var axisBefore = CampaignSourceGt1AxisArrival.Create(lane3Source);
            var benghazi = CampaignSourceGt1BenghaziArrival.Create(lane3Source, axisBefore);
            Assert.True(benghazi.CanAnnounce);
            var announced = benghazi.Announce(lane3Source, axisBefore);
            var (delivered, axisAfter) = announced.DeclineAttackAndDeliver(lane3Source, axisBefore);
            Assert.True(delivered.Delivered);
            Assert.Equal("b4827", Assert.Single(lane3Source.AxisSupplyWorld.Units,
                value => value.Id == "axis-benghazi-1").LocationId);
            Assert.Equal("a4827", Assert.Single(axisAfter.Supply.Units,
                value => value.Id == "axis-benghazi-1").LocationId);
            var landed = Assert.Single(axisAfter.Supply.Units, value => value.Id == "axis-gt1-op1-lane3-1");
            Assert.Equal("a4827", landed.LocationId);
            Assert.Equal(40, landed.Ammo);
            Assert.Equal(60, landed.Fuel);
            Assert.Equal(2, axisAfter.Supply.Units.Count(value => value.LocationId == "a4827"));
            Assert.False(axisAfter.HasOtherStageOneLanes);
            Assert.Throws<InvalidOperationException>(() => announced.DeclineAttackAndDeliver(lane3Source, axisAfter));
            var (replayedBenghazi, replayedAxis) = benghazi.Announce(lane3Source, axisBefore)
                .DeclineAttackAndDeliver(lane3Source, axisBefore);
            Assert.Equal(delivered.Hash(), replayedBenghazi.Hash());
            Assert.Equal(axisAfter.Hash(), replayedAxis.Hash());
            using var landedJson = JsonDocument.Parse(axisAfter.Supply.SerializeCanonical());
            Assert.Equal(2, landedJson.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Contains("rul-gra-05-provisional", landedJson.RootElement.GetProperty("benghaziCoordinateStatus").GetString()!);
            if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply >= 2)
            {
                var mixed = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession,
                    Submission(cwSession, lane3Add, CampaignActionAudience.Axis)).SuccessorSession!;
                var lane2Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 2, 1, 1);
                mixed = CampaignExercises.SubmitSourceAxisGt1Supply(mixed,
                    Submission(mixed, lane2Add, CampaignActionAudience.Axis)).SuccessorSession!;
                mixed = CampaignExercises.SubmitSourceAxisGt1Supply(mixed,
                    Submission(mixed, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
                var mixedSource = mixed.SourceAuthority!;
                var mixedAxis = CampaignSourceGt1AxisArrival.Create(mixedSource);
                Assert.False(mixedAxis.CanAnnounce);
                Assert.False(CampaignSourceGt1BenghaziArrival.Create(mixedSource, mixedAxis).CanAnnounce);
                Assert.Throws<InvalidOperationException>(() => mixedAxis.DeliverSoleBenghaziLane(mixedSource, 1));
                var multi = CampaignSourceGt1MultiArrival.Announce(mixedSource, mixedAxis);
                Assert.Collection(multi.Lanes, value => Assert.Equal(2, value), value => Assert.Equal(3, value));
                var (firstThree, axisThree) = multi.DeclineAndDeliver(mixedSource, mixedAxis, 3);
                var (threeThenTwo, axisThreeThenTwo) = firstThree.DeclineAndDeliver(mixedSource, axisThree, 2);
                var (firstTwo, axisTwo) = multi.DeclineAndDeliver(mixedSource, mixedAxis, 2);
                var (twoThenThree, axisTwoThenThree) = firstTwo.DeclineAndDeliver(mixedSource, axisTwo, 3);
                Assert.True(threeThenTwo.IsDelivered(2));
                Assert.True(threeThenTwo.IsDelivered(3));
                Assert.Equal(axisThreeThenTwo.Supply.Hash(), axisTwoThenThree.Supply.Hash());
                Assert.Equal(threeThenTwo.Hash(), twoThenThree.Hash());
                Assert.Equal("box.tripoli", Assert.Single(axisThreeThenTwo.Supply.Units,
                    value => value.Id == "axis-gt1-op1-lane2-1").LocationId);
                Assert.Equal("a4827", Assert.Single(axisThreeThenTwo.Supply.Units,
                    value => value.Id == "axis-gt1-op1-lane3-1").LocationId);
                Assert.Throws<InvalidOperationException>(() => threeThenTwo.DeclineAndDeliver(mixedSource, axisThreeThenTwo, 3));
                Assert.DoesNotContain(mixedSource.AxisSupplyWorld.Units, value => value.Id == "axis-gt1-op1-lane3-1");
                if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply >= 3)
                {
                    var triple = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession,
                        Submission(cwSession, lane3Add, CampaignActionAudience.Axis)).SuccessorSession!;
                    triple = CampaignExercises.SubmitSourceAxisGt1Supply(triple,
                        Submission(triple, lane2Add, CampaignActionAudience.Axis)).SuccessorSession!;
                    var lane6Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 6, 1, 1);
                    triple = CampaignExercises.SubmitSourceAxisGt1Supply(triple,
                        Submission(triple, lane6Add, CampaignActionAudience.Axis)).SuccessorSession!;
                    triple = CampaignExercises.SubmitSourceAxisGt1Supply(triple,
                        Submission(triple, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
                    var tripleSource = triple.SourceAuthority!;
                    var tripleAxis = CampaignSourceGt1AxisArrival.Create(tripleSource);
                    var tripleMulti = CampaignSourceGt1MultiArrival.Announce(tripleSource, tripleAxis);
                    var (tripleTwo, tripleAfterTwo) = tripleMulti.DeclineAndDeliver(tripleSource, tripleAxis, 2);
                    var (tripleThree, tripleAfterThree) = tripleTwo.DeclineAndDeliver(tripleSource, tripleAfterTwo, 3);
                    Assert.True(tripleThree.IsDue(6));
                    Assert.False(tripleThree.IsDelivered(6));
                    Assert.True(tripleThree.CanDecline(6));
                    var (tripleSix, tripleAfterSix) = tripleThree.DeclineAndDeliver(tripleSource, tripleAfterThree, 6);
                    Assert.True(tripleSix.Lane6Declined);
                    Assert.True(tripleSix.IsDelivered(6));
                    Assert.Equal("c4807", Assert.Single(tripleAfterSix.Supply.Units,
                        value => value.Id == "axis-gt1-op1-lane6-1").LocationId);
                    Assert.Throws<InvalidOperationException>(() => tripleSix.DeclineAndDeliver(tripleSource, tripleAfterSix, 6));
                    (bool CanDecline, bool AttackDeclined)? oneBefore = null, oneAfter = null, threeBefore = null, threeAfter = null;
                    for (var amount = 1; amount <= 3; amount++)
                    {
                        var tobruk = cwSession;
                        for (var n = 0; n < amount; n++)
                            tobruk = CampaignExercises.SubmitSourceAxisGt1Supply(tobruk,
                                Submission(tobruk, lane6Add, CampaignActionAudience.Axis)).SuccessorSession!;
                        tobruk = CampaignExercises.SubmitSourceAxisGt1Supply(tobruk,
                            Submission(tobruk, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
                        var tobrukSource = tobruk.SourceAuthority!;
                        var tobrukAxis = CampaignSourceGt1AxisArrival.Create(tobrukSource);
                        var tobrukAnnounced = CampaignSourceGt1MultiArrival.Announce(tobrukSource, tobrukAxis);
                        var (tobrukDeclined, tobrukAfter) = tobrukAnnounced.DeclineAndDeliver(tobrukSource, tobrukAxis, 6);
                        if (amount == 1) { oneBefore = tobrukAnnounced.Lane6CommonwealthCut(); oneAfter = tobrukDeclined.Lane6CommonwealthCut(); }
                        if (amount == 3) { threeBefore = tobrukAnnounced.Lane6CommonwealthCut(); threeAfter = tobrukDeclined.Lane6CommonwealthCut(); }
                        Assert.True(tobrukDeclined.Lane6Declined);
                        Assert.False(tobrukDeclined.CanDecline(6));
                        Assert.Equal(amount <= 2, tobrukDeclined.IsDelivered(6));
                        Assert.Equal(3 + (amount <= 2 ? amount : 0),
                            tobrukAfter.Supply.Units.Count(value => value.LocationId == "c4807"));
                        if (amount == 3) Assert.Equal(tobrukAxis.Supply.Hash(), tobrukAfter.Supply.Hash());
                        Assert.Equal(3, tobrukSource.AxisSupplyWorld.Units.Count(value => value.LocationId == "c4807"));
                    }
                    Assert.Equal(oneBefore, threeBefore);
                    Assert.Equal(oneAfter, threeAfter);
                }
            }
            var gt1Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 2, 1, 1);
            cwSession = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, gt1Add, CampaignActionAudience.Axis)).SuccessorSession!;
            Assert.Single(cwSession.SourceAuthority!.OpeningConvoyPlan.Allocations());
        }
        var gt1Finish = CampaignSourceAuthorityState.OpeningConvoyActionId("finish", 0, 0, 0);
        cwSession = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, gt1Finish, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(cwSession.SourceAuthority!.OpeningConvoyPlan.IsFinished);
        var pendingTripoli = CampaignSourceGt1AxisArrival.Create(cwSession.SourceAuthority!);
        var sourceSupplyHash = cwSession.SourceAuthority!.AxisSupplyWorld.Hash();
        if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply > 0)
        {
            Assert.True(pendingTripoli.CanAnnounce);
            var announcedTripoli = pendingTripoli.Announce();
            Assert.True(announcedTripoli.Announced);
            Assert.False(announcedTripoli.Delivered);
            var deliveredTripoli = announcedTripoli.DeclineAttackAndDeliver();
            var deliveredUnit = Assert.Single(deliveredTripoli.Supply.Units, unit => unit.Id == "axis-gt1-op1-lane2-1");
            Assert.Equal("box.tripoli", deliveredUnit.LocationId);
            Assert.Equal(40, deliveredUnit.Ammo);
            Assert.Equal(60, deliveredUnit.Fuel);
            Assert.Equal(sourceSupplyHash, cwSession.SourceAuthority.AxisSupplyWorld.Hash());
            Assert.NotEqual(pendingTripoli.Hash(), deliveredTripoli.Hash());
            Assert.Throws<InvalidOperationException>(() => deliveredTripoli.DeclineAttackAndDeliver());
        }
        else
        {
            Assert.False(pendingTripoli.CanAnnounce);
            Assert.Throws<InvalidOperationException>(() => pendingTripoli.Announce());
        }

        Assert.Empty(CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var skipCwGt1 = CampaignSourceAuthorityState.CommonwealthSupplyPlanActionId("skip", 1);
        var cwPlanning = CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Commonwealth);
        Assert.Contains(skipCwGt1, cwPlanning.PrivateActionIds);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Axis));
        var beforeSkip = cwSession.SourceAuthority.RandomState.NextByteCursor;
        cwSession = CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession,
            Submission(cwSession, skipCwGt1, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(beforeSkip, cwSession.SourceAuthority!.RandomState.NextByteCursor);
        Assert.Equal("skipped-opening", cwSession.SourceAuthority.CommonwealthSupplyPlans!.Entries[0].Status);
        foreach (var turn in new[] { 2, 3, 4 })
        {
            var prior = cwSession.SourceAuthority.RandomState;
            var expected = SandtableRandom.RollD6(prior);
            var action = CampaignSourceAuthorityState.CommonwealthSupplyPlanActionId("roll", turn);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession, Submission(cwSession, action, CampaignActionAudience.Axis)).RejectionReason);
            cwSession = CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession,
                Submission(cwSession, action, CampaignActionAudience.Commonwealth)).SuccessorSession!;
            var entry = cwSession.SourceAuthority!.CommonwealthSupplyPlans!.Entries.Single(value => value.TargetGameTurn == turn);
            Assert.Equal(expected.Value, entry.Die);
            Assert.Equal((expected.Value + 1) / 2, entry.SupplyUnits);
            Assert.Equal(expected.State.NextByteCursor, cwSession.SourceAuthority.RandomState.NextByteCursor);
        }
        Assert.Empty(CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var gt1Plan = CampaignSourceGt1SupplyPlanning.Create(cwSession.SourceAuthority!, cwSession.SourceAuthority!.RandomState);
        var gt5 = CampaignSourceGt1CommonwealthSupplyPlanning.Create();
        var expectedGt5 = SandtableRandom.RollD6(gt1Plan.RandomState);
        var rolledGt5 = gt5.Roll(gt1Plan.RandomState);
        Assert.Equal(expectedGt5.Value, rolledGt5.Plan.Die);
        Assert.Equal((expectedGt5.Value + 1) / 2, rolledGt5.Plan.Units);
        Assert.Equal(expectedGt5.State, rolledGt5.Random);
        Assert.Throws<InvalidOperationException>(() => rolledGt5.Plan.Roll(rolledGt5.Random));
        Assert.Equal(expectedGt5.State, gt1Plan.WithRandomState(rolledGt5.Random).RandomState);
        var expectedGt3 = SandtableRandom.RollD6(gt1Plan.RandomState);
        var rolledGt3 = gt1Plan.Roll();
        Assert.Equal(expectedGt3.Value, rolledGt3.Die);
        Assert.Equal(expectedGt3.Value switch { 1 => 0, <= 3 => 1, <= 5 => 2, _ => 3 }, rolledGt3.Maximum);
        Assert.Equal(expectedGt3.State, rolledGt3.RandomState);
        Assert.Throws<InvalidOperationException>(() => rolledGt3.Roll());
        Assert.Empty(rolledGt3.ValidDeltas(1, 1));
        Assert.Empty(rolledGt3.ValidDeltas(2, 4));
        if (rolledGt3.Maximum > 0)
        {
            var assigned = rolledGt3.Adjust(2, 1, 1);
            Assert.Equal(1, assigned.QuantityAt(2, 1));
            Assert.Equal(rolledGt3.RandomState, assigned.RandomState);
            Assert.Equal(0, assigned.Adjust(2, 1, -1).QuantityAt(2, 1));
            for (var unit = 1; unit < rolledGt3.Maximum; unit++) assigned = assigned.Adjust(2, 1, 1);
            Assert.DoesNotContain(1, assigned.ValidDeltas(2, 1));
            Assert.DoesNotContain(1, assigned.ValidDeltas(3, 2));
            Assert.Throws<InvalidOperationException>(() => assigned.Adjust(3, 2, 1));
        }
        var finishedGt3 = rolledGt3.Finish();
        Assert.True(finishedGt3.Finished);
        Assert.Empty(finishedGt3.ValidDeltas(2, 1));
        // Each owner's roll may happen first; the shared stream must advance
        // once per roll without rewriting an already-finished Axis plan.
        var axisFirstPlan = gt1Plan.Roll().Finish();
        var axisFirstHash = axisFirstPlan.Hash();
        var cwAfterAxis = CampaignSourceGt1CommonwealthSupplyPlanning.Create().Roll(axisFirstPlan.RandomState);
        Assert.Equal(axisFirstHash, axisFirstPlan.Hash());
        var cwRollFirst = CampaignSourceGt1CommonwealthSupplyPlanning.Create().Roll(gt1Plan.RandomState);
        var axisAfterCw = gt1Plan.WithRandomState(cwRollFirst.Random).Roll().Finish();
        Assert.Equal(axisFirstPlan.Die, cwRollFirst.Plan.Die);
        Assert.Equal(cwAfterAxis.Plan.Die, axisAfterCw.Die);
        Assert.Equal(cwAfterAxis.Random, axisAfterCw.RandomState);
        Assert.Throws<InvalidOperationException>(() => axisAfterCw.Roll());
        Assert.Throws<InvalidOperationException>(() => cwAfterAxis.Plan.Roll(cwAfterAxis.Random));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        // Exercise the real prestart September plans on an independent branch of the retained source session.
        var september = cwSession;
        var beginSeptember = CampaignSourceAuthorityState.SeptemberSupplyActionId("begin", 0, 0, 0, 0);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceSeptemberSupplyDraft(september, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, beginSeptember, CampaignActionAudience.Commonwealth)).RejectionReason);
        var normalSeptember = CampaignExercises.Query(september, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "axis-september-supply");
        Assert.DoesNotContain(CampaignExercises.Query(september, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "axis-september-supply");
        var beginCommand = Submission(september, normalSeptember.ActionId, CampaignActionAudience.Axis);
        september = CampaignExercises.Submit(september, beginCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(september, beginCommand).RejectionReason);
        foreach (var turn in new[] { 3, 4 })
        {
            var expected = SandtableRandom.RollD6(september.SourceAuthority!.RandomState);
            var septemberRoll = CampaignSourceAuthorityState.SeptemberSupplyActionId("roll", turn, 0, 0, 0);
            september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, septemberRoll, CampaignActionAudience.Axis)).SuccessorSession!;
            var entry = september.SourceAuthority!.SeptemberPlans!.Entries.Single(value => value.TargetGameTurn == turn);
            Assert.Equal(expected.Value, entry.Die);
            Assert.Equal(expected.Value switch { 1 => 0, <= 3 => 1, <= 5 => 2, _ => 3 }, entry.Maximum);
            Assert.Equal(expected.State.NextByteCursor, september.SourceAuthority.RandomState.NextByteCursor);
            var finish = CampaignSourceAuthorityState.SeptemberSupplyActionId("finish", turn, 0, 0, 0);
            if (entry.Maximum > 0)
            {
                var zeroPlan = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, finish, CampaignActionAudience.Axis)).SuccessorSession!;
                Assert.True(zeroPlan.SourceAuthority!.SeptemberPlans!.Entries.Single(value => value.TargetGameTurn == turn).Finished);
                Assert.Equal(0, zeroPlan.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == turn).Allocations.Sum());
                var add = CampaignSourceAuthorityState.SeptemberSupplyActionId("adjust", turn, 2, 1, 1);
                september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, add, CampaignActionAudience.Axis)).SuccessorSession!;
            }
            september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, finish, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        Assert.Null(september.SourceAuthority!.SeptemberPlans!.NextTargetGameTurn);
        Assert.Equal(38, september.SourceAuthority.ProfileVersion);
        var retainedGt3 = CampaignSourceGt1SupplyPlanning.Create(september.SourceAuthority, september.SourceAuthority.RandomState);
        Assert.Equal(september.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == 3).Die, retainedGt3.Die);
        Assert.True(retainedGt3.CarriedPlanFinished);
        Assert.Equal(september.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == 3).Allocations[0], retainedGt3.QuantityAt(2, 1));
        Assert.Throws<InvalidOperationException>(() => retainedGt3.Roll());
        var septemberHash = september.SourceAuthority.SeptemberPlans.Hash();
        september = CampaignExercises.AdmitSourceAxisSupplement(september).SuccessorSession!;
        Assert.Equal(septemberHash, september.SourceAuthority!.SeptemberPlans!.Hash());
        Assert.True(CampaignExercises.Reconstruct(september).IsVerified);
        Assert.False(CampaignExercises.IsSourceAxisSupplementAdmitted(cwSession));
        var migrated = CampaignExercises.AdmitSourceAxisSupplement(cwSession);
        Assert.True(migrated.IsAccepted);
        cwSession = migrated.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceAxisSupplementAdmitted(cwSession));
        Assert.Equal(5, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceAxisSupplement(cwSession).RejectionReason);
        var replacements = CampaignSourceAuthorityState.AutoblindaActionId("replacements", "");
        Assert.Contains(replacements, CampaignExercises.QuerySourceAutoblindaDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, replacements, CampaignActionAudience.Commonwealth)).RejectionReason);
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, replacements, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, cwSession.SourceAuthority!.Autoblinda!.ReplacementPoints);
        var reset = CampaignSourceAuthorityState.AutoblindaActionId("reset", "");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, reset, CampaignActionAudience.Axis)).SuccessorSession!;
        var attach1 = CampaignSourceAuthorityState.AutoblindaActionId("attach", "1-141-infantry");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach1, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach1, CampaignActionAudience.Axis)).RejectionReason);
        var attach2 = CampaignSourceAuthorityState.AutoblindaActionId("attach", "2-141-infantry");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach2, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(cwSession.SourceAuthority!.Autoblinda!.IsComplete);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.False(CampaignExercises.IsSourceCommonwealthSupplementAdmitted(cwSession));
        var cwMigrated = CampaignExercises.AdmitSourceCommonwealthSupplement(cwSession);
        Assert.True(cwMigrated.IsAccepted);
        cwSession = cwMigrated.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCommonwealthSupplementAdmitted(cwSession));
        Assert.Equal(6, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(12, cwSession.SourceAuthority!.CommonwealthSupplement!.Forces.Count);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceCommonwealthSupplement(cwSession).RejectionReason);
        Assert.False(CampaignExercises.IsSourcePriorityMotorizationAdmitted(cwSession));
        var priorityMigration = CampaignExercises.AdmitSourcePriorityMotorization(cwSession);
        Assert.True(priorityMigration.IsAccepted);
        cwSession = priorityMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourcePriorityMotorizationAdmitted(cwSession));
        Assert.Equal(7, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourcePriorityMotorizationDraft(cwSession, CampaignActionAudience.Axis));
        var priorityId = CampaignSourceAuthorityState.PriorityMotorizationActionId("1-kings-royal-rifle-corps", 6);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourcePriorityMotorization(cwSession, Submission(cwSession, priorityId, CampaignActionAudience.Axis)).RejectionReason);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, priorityId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(14, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3922-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        var reversePriority = CampaignSourceAuthorityState.PriorityMotorizationActionId("1-kings-royal-rifle-corps", -6);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, reversePriority, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(20, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3922-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        var secondPriority = CampaignSourceAuthorityState.PriorityMotorizationActionId("2-rifle-brigade", 6);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, secondPriority, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(4, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3320-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var axisMigration = CampaignExercises.AdmitSourceAxisPriorityMotorization(cwSession);
        Assert.True(axisMigration.IsAccepted);
        cwSession = axisMigration.SuccessorSession!;
        Assert.Equal(8, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var targetAxis = "1-1-libyan-motor-infantry";
        var oldDetach = CampaignSourceAuthorityState.AxisPriorityActionId("detach", targetAxis, 0);
        var historical = CampaignExercises.ReplaySourceAxisPriority(cwSession,
            Submission(cwSession, oldDetach, CampaignActionAudience.Axis));
        Assert.True(historical.IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(historical.SuccessorSession!).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, oldDetach, CampaignActionAudience.Axis)).RejectionReason);
        var prematureLift = CampaignSourceAuthorityState.AxisPriorityActionId("adjust", targetAxis, 5);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, prematureLift, CampaignActionAudience.Axis)).RejectionReason);
        foreach (var axisTarget in CampaignSourceAxisPriorityMotorization.Targets)
        {
            var detach = CampaignSourceAuthorityState.AxisPriorityActionId("detach", axisTarget, 0);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, detach, CampaignActionAudience.Axis)).RejectionReason);
        }
        Assert.Equal(35, cwSession.SourceAuthority!.AxisPriorityMotorization!.Remaining(cwSession.SourceAuthority.AxisMotorization));
        var undoDetachment = CampaignSourceAuthorityState.AxisPriorityActionId("undo-detachment", targetAxis, 0);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, undoDetachment, CampaignActionAudience.Axis)).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var garrisonMigration = CampaignExercises.AdmitSourceAxisMinorGarrisons(cwSession);
        Assert.True(garrisonMigration.IsAccepted);
        cwSession = garrisonMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceAxisMinorGarrisonsAdmitted(cwSession));
        Assert.Equal(9, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(13, cwSession.SourceAuthority!.AxisGarrisons!.Forces.Count);
        Assert.Contains(cwSession.SourceAuthority.AxisGarrisons.Forces,
            value => value.ElementId == "giarabub-motor-machine-gun");
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceAxisMinorGarrisons(cwSession).RejectionReason);
        var battlefieldMigration = CampaignExercises.AdmitSourceBattlefield(cwSession);
        Assert.True(battlefieldMigration.IsAccepted);
        cwSession = battlefieldMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceBattlefieldAdmitted(cwSession));
        Assert.Equal(10, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var battlefield = cwSession.SourceAuthority!.Battlefield!;
        Assert.Equal(battlefield.Entries.Count, battlefield.Entries.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(2, battlefield.Pending.Count);
        Assert.Equal(8, battlefield.Pending.Single(value => value.GroupId == Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId).ElementIds.Count);
        Assert.Equal(4, battlefield.Pending.Single(value => value.GroupId == Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId).ElementIds.Count);
        using var battlefieldBytes = JsonDocument.Parse(battlefield.SerializeCanonical());
        Assert.False(battlefieldBytes.RootElement.GetProperty("battlefieldComplete").GetBoolean());
        Assert.False(battlefieldBytes.RootElement.GetProperty("controlCertified").GetBoolean());
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceBattlefield(cwSession).RejectionReason);
        var cairoAdmission = CampaignExercises.AdmitSourceCairoSetup(cwSession);
        Assert.True(cairoAdmission.IsAccepted);
        cwSession = cairoAdmission.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCairoSetupAdmitted(cwSession));
        Assert.Equal(11, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(4, cwSession.SourceAuthority!.CairoSetup!.PendingIds.Count);
        Assert.Equal(4, cwSession.SourceAuthority.Battlefield!.Entries.Count(x => x.AuthorityStatus == "source-setup-unplaced"));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCairoSetupDraft(cwSession, CampaignActionAudience.Axis));
        var invalidCity = CampaignSourceAuthorityState.CairoPlacementActionId("1-welch", "e1829");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, invalidCity, CampaignActionAudience.Commonwealth)).RejectionReason);
        var inferredCity = CampaignSourceAuthorityState.CairoPlacementActionId("1-welch", "e1931");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, inferredCity, CampaignActionAudience.Commonwealth)).RejectionReason);
        var chosenCairoHex = seed == 14 ? "e1830" : "e1730";
        foreach (var element in CampaignSourceCairoInitialSetup.Units.Select(value => value.ElementId))
        {
            var action = CampaignSourceAuthorityState.CairoPlacementActionId(element, chosenCairoHex);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, action, CampaignActionAudience.Axis)).RejectionReason);
            var command = Submission(cwSession, action, CampaignActionAudience.Commonwealth);
            var placedCairo = CampaignExercises.PlaceSourceCairoUnit(cwSession, command);
            Assert.True(placedCairo.IsAccepted);
            cwSession = placedCairo.SuccessorSession!;
            Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
                CampaignExercises.PlaceSourceCairoUnit(cwSession, command).RejectionReason);
        }
        Assert.Empty(cwSession.SourceAuthority!.CairoSetup!.PendingIds);
        Assert.Equal(4, cwSession.SourceAuthority.Battlefield!.Entries.Count(x => x.LocationId == chosenCairoHex && x.AuthorityStatus == "source-setup-placed"));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var activeMigration = CampaignExercises.AdmitSourceCairoWorld(cwSession);
        Assert.True(activeMigration.IsAccepted);
        cwSession = activeMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCairoWorldAdmitted(cwSession));
        Assert.Equal(12, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var active = cwSession.SourceAuthority!.CairoWorld!;
        Assert.Equal("sha256:3a7fddd510854c81b408e07742dd837f7be416206117ca58ea79357357a11ddd", active.Content.Hash());
        Assert.NotEqual(cwSession.SourceAuthority.Content.Identity.Hash, active.Content.Identity.Hash);
        Assert.Equal(active.Content.Identity, active.World.ContentIdentity);
        Assert.Equal(8, active.World.World.Elements.Count);
        Assert.Equal(8, active.World.World.Representations.Count);
        Assert.Equal(2, active.World.PendingArrivals.Count);
        foreach (var unit in CampaignSourceCairoInitialSetup.Units)
        {
            var element = Assert.Single(active.World.World.Elements, value => value.ElementId == unit.ElementId);
            Assert.Equal(chosenCairoHex, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
        }
        var alteredContentText = System.Text.Encoding.UTF8.GetString(active.Content.CanonicalBytes)
            .Replace("\"initialToe\":6", "\"initialToe\":7", StringComparison.Ordinal);
        var alteredContent = ContentSourceSetupPackArtifact.Read(System.Text.Encoding.UTF8.GetBytes(alteredContentText));
        Assert.NotEqual(active.Content.Hash(), alteredContent.Hash());
        var forgedHistory = cwSession.PregameHistory.Select(value => value.ToArray()).ToArray();
        var forgedEvent = System.Text.Encoding.UTF8.GetString(forgedHistory[^1])
            .Replace(active.Content.Hash(), alteredContent.Hash(), StringComparison.Ordinal);
        forgedHistory[^1] = System.Text.Encoding.UTF8.GetBytes(forgedEvent);
        var forgedSession = new ExerciseSession(cwSession.Pregame!, cwSession.CurrentContacts, forgedHistory, cwSession.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(forgedSession).IsVerified);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceCairoWorld(cwSession).RejectionReason);
        var directV12 = cwSession;
        Assert.True(CampaignExercises.CanPromoteSourceIndependent(directV12));
        var lockBeforePromotion = CampaignExercises.Query(directV12, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var lockedBeforePromotion = CampaignExercises.Submit(directV12,
            Submission(directV12, lockBeforePromotion.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.PromoteSourceIndependent(lockedBeforePromotion).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.ReplaySourceIndependentPromotion(lockedBeforePromotion).RejectionReason);
        directV12 = CampaignExercises.PromoteSourceIndependent(directV12).SuccessorSession!;
        Assert.Equal(14, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(10, directV12.SourceAuthority!.World.World.Elements.Count);
        var genericIdentity = directV12.SourceAuthority.World.ContentIdentity;
        Assert.False(CampaignExercises.CanPromoteSourceIndependent(directV12));
        directV12 = CampaignExercises.AdmitSourceLocalMap(directV12).SuccessorSession!;
        Assert.Equal(15, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var mapIdentity = directV12.SourceAuthority!.World.ContentIdentity;
        Assert.NotEqual(genericIdentity, mapIdentity);
        Assert.Equal(10, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.PromoteSourceExpandedIndependent(directV12).SuccessorSession!;
        Assert.Equal(16, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(13, directV12.SourceAuthority!.World.World.Elements.Count);
        var expandedIdentity = directV12.SourceAuthority.World.ContentIdentity;
        Assert.False(CampaignExercises.CanPromoteSourceExpandedIndependent(directV12));
        Assert.False(CampaignExercises.CanPromoteSourceMaletti(directV12));
        Assert.Equal(expandedIdentity, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(mapIdentity, directV12.SourceAuthority.LocalMapWorld!.Content.Identity);
        Assert.Equal(genericIdentity, directV12.SourceAuthority.IndependentWorld!.Content.Identity);
        Assert.Equal(13, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.AdmitSourceAttachedInitialLoads(directV12).SuccessorSession!;
        Assert.Equal(17, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        const string loadedTarget = "2-rifle-brigade";
        var attach = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("attach", loadedTarget);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, attach, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, attach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, directV12.SourceAuthority!.AttachedInitialLoads!.Reserved("cw-c3320-attached"));
        var fuel = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("fuel", loadedTarget);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, fuel, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, Assert.Single(directV12.SourceAuthority!.AttachedInitialLoads!.Targets).FuelPoints);
        var detachLoad = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("detach", loadedTarget);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, detachLoad, CampaignActionAudience.Commonwealth)).RejectionReason);
        var removeLift = CampaignSourceAuthorityState.PriorityMotorizationActionId(loadedTarget, -6);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourcePriorityMotorization(directV12, Submission(directV12, removeLift, CampaignActionAudience.Commonwealth)).RejectionReason);
        var clear = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("unload-fuel", loadedTarget);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, clear, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, detachLoad, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Empty(directV12.SourceAuthority!.AttachedInitialLoads!.Targets);
        directV12 = CampaignExercises.AdmitSourceInitialWrecks(directV12).SuccessorSession!;
        Assert.Equal(18, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(3, directV12.SourceAuthority!.InitialWrecks!.PendingIds.Count);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceInitialWrecksDraft(directV12, CampaignActionAudience.Axis));
        foreach (var id in directV12.SourceAuthority.InitialWrecks.PendingIds.ToArray())
        {
            var place = CampaignSourceAuthorityState.InitialWreckPlacementActionId(id, "e3613");
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceInitialWreck(directV12, Submission(directV12, place, CampaignActionAudience.Axis)).RejectionReason);
            directV12 = CampaignExercises.PlaceSourceInitialWreck(directV12, Submission(directV12, place, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        }
        Assert.Empty(directV12.SourceAuthority!.InitialWrecks!.PendingIds);
        Assert.Equal(3, directV12.SourceAuthority.InitialWrecks.Placements.Count);
        directV12 = CampaignExercises.AdmitSourceAxisFreeSetup(directV12).SuccessorSession!;
        Assert.Equal(19, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var forgedLegacyState = directV12.SourceAuthority!.ApplyAxisPriorityChoice("detach", targetAxis, 0);
        var forgedLegacyEvent = directV12.SourceAuthority.SerializeAxisPriorityEvent(forgedLegacyState, "detach", targetAxis, 0);
        var forgedLegacySession = new ExerciseSession(directV12.Pregame!, directV12.CurrentContacts.AdvancePregameSource(directV12.SourceAuthority.StateVersion),
            [.. directV12.PregameHistory, forgedLegacyEvent], forgedLegacyState);
        Assert.False(CampaignExercises.Reconstruct(forgedLegacySession).IsVerified);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisFreeSetupDraft(directV12, CampaignActionAudience.Commonwealth));
        var freeIds = directV12.SourceAuthority!.AxisFreeArtillery!.PendingIds.ToArray();
        for (var i = 0; i < freeIds.Length; i++)
        {
            var location = i < 2 ? "c4407" : "c4607";
            var action = CampaignSourceAuthorityState.AxisFreePlacementActionId(freeIds[i], location);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, action, CampaignActionAudience.Commonwealth)).RejectionReason);
            if (i == 2)
            {
                var overstack = CampaignSourceAuthorityState.AxisFreePlacementActionId(freeIds[i], "c4407");
                Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                    CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, overstack, CampaignActionAudience.Axis)).RejectionReason);
            }
            directV12 = CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, action, CampaignActionAudience.Axis)).SuccessorSession!;
            if (i == 0)
            {
                directV12 = CampaignExercises.PromoteSourceAxisFreeWorld(directV12).SuccessorSession!;
                Assert.Equal(20, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
                Assert.Equal(14, directV12.SourceAuthority!.World.World.Elements.Count);
                var identityAfterPartial = directV12.SourceAuthority.World.ContentIdentity;
                var adjustAxis = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", 1);
                directV12 = CampaignExercises.AdjustSourceMotorization(directV12,
                    Submission(directV12, adjustAxis, CampaignActionAudience.Axis)).SuccessorSession!;
                directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12,
                    Submission(directV12, attach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
                Assert.Equal(identityAfterPartial, directV12.SourceAuthority!.World.ContentIdentity);
                Assert.Equal(14, directV12.SourceAuthority.World.World.Elements.Count);
                Assert.False(CampaignExercises.CanPromoteSourceIndependent(directV12));
                Assert.False(CampaignExercises.CanPromoteSourceExpandedIndependent(directV12));
            }
        }
        Assert.Empty(directV12.SourceAuthority!.AxisFreeArtillery!.PendingIds);
        Assert.Equal(3, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-1ar-placed"));
        var axisContent = directV12.SourceAuthority.World.ContentIdentity;
        directV12 = CampaignExercises.PromoteSourceAxisFreeWorld(directV12).SuccessorSession!;
        Assert.Equal(axisContent, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(17, directV12.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Equal(4, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-1ar-active"));
        Assert.False(CampaignExercises.CanPromoteSourceAxisFreeWorld(directV12));
        directV12 = CampaignExercises.AdmitSourceAxisCorpsSetup(directV12).SuccessorSession!;
        Assert.Equal(21, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisCorpsSetupDraft(directV12, CampaignActionAudience.Commonwealth));
        var corpsIds = directV12.SourceAuthority!.AxisCorpsArtillery!.PendingIds.ToArray();
        for (var i = 0; i < corpsIds.Length; i++)
        {
            var location = i == 0 ? "c4307" : "c4308";
            var action = CampaignSourceAuthorityState.AxisCorpsPlacementActionId(corpsIds[i], location);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, action, CampaignActionAudience.Commonwealth)).RejectionReason);
            if (i == 1)
            {
                var occupied = CampaignSourceAuthorityState.AxisCorpsPlacementActionId(corpsIds[i], "c4307");
                Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                    CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, occupied, CampaignActionAudience.Axis)).RejectionReason);
            }
            directV12 = CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, action, CampaignActionAudience.Axis)).SuccessorSession!;
            directV12 = CampaignExercises.PromoteSourceAxisCorpsWorld(directV12).SuccessorSession!;
            Assert.Equal(22, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        }
        Assert.Equal(19, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Equal(2, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-corps-artillery-active"));
        foreach (var id in corpsIds)
            Assert.Equal(3, directV12.SourceAuthority.World.World.Elements.Single(value => value.ElementId == id).Components.Single(value => value.ComponentId.EndsWith(".light-aa", StringComparison.Ordinal)).CurrentToe);
        Assert.False(CampaignExercises.CanPromoteSourceAxisCorpsWorld(directV12));
        var corpsContent = directV12.SourceAuthority.World.ContentIdentity;
        var removeAxisPoint = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", -1);
        directV12 = CampaignExercises.AdjustSourceMotorization(directV12,
            Submission(directV12, removeAxisPoint, CampaignActionAudience.Axis)).SuccessorSession!;
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12,
            Submission(directV12, fuel, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(corpsContent, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(19, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.AdmitSourceMalettiHierarchy(directV12).SuccessorSession!;
        Assert.Equal(23, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var confirmHierarchy = CampaignSourceAuthorityState.MalettiHierarchyActionId("confirm", null);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMalettiHierarchy(directV12, Submission(directV12, confirmHierarchy, CampaignActionAudience.Commonwealth)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceMalettiHierarchy(directV12,
            Submission(directV12, confirmHierarchy, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwFirst = directV12;
        var frenchFirst = directV12;
        var certifiedDetach = CampaignSourceAuthorityState.MalettiHierarchyActionId("detach", targetAxis);
        directV12 = CampaignExercises.SubmitSourceMalettiHierarchy(directV12,
            Submission(directV12, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(targetAxis, directV12.SourceAuthority!.MalettiHierarchy!.DetachedId);
        var otherDetach = CampaignSourceAuthorityState.MalettiHierarchyActionId("detach", "3-1-libyan-motor-infantry");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMalettiHierarchy(directV12, Submission(directV12, otherDetach, CampaignActionAudience.Axis)).RejectionReason);
        Assert.True(CampaignExercises.CanPromoteSourceCertifiedMalettiWorld(directV12));
        directV12 = CampaignExercises.PromoteSourceCertifiedMalettiWorld(directV12).SuccessorSession!;
        Assert.Equal(24, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(20, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Contains(directV12.SourceAuthority.Battlefield!.Entries,
            value => value.ElementId == targetAxis && value.AuthorityStatus == "source-certified-maletti-active");
        Assert.False(CampaignExercises.CanPromoteSourceCertifiedMalettiWorld(directV12));
        var axisLift = CampaignSourceAuthorityState.AxisPriorityActionId("adjust", targetAxis, 5);
        var lockBeforeLift = CampaignExercises.Query(directV12, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var lockedBeforeLift = CampaignExercises.Submit(directV12,
            Submission(directV12, lockBeforeLift.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(lockedBeforeLift,
                Submission(lockedBeforeLift, axisLift, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceAxisPriority(directV12,
            Submission(directV12, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(5, directV12.SourceAuthority!.AxisPriorityMotorization!.AssignedPoints(targetAxis));
        directV12 = CampaignExercises.AdmitSourceMatruhFormation(directV12).SuccessorSession!;
        Assert.Equal(25, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceMatruhFormationDraft(directV12, CampaignActionAudience.Axis));
        var matruhConfirm = CampaignSourceAuthorityState.MatruhFormationActionId("confirm", null);
        directV12 = CampaignExercises.SubmitSourceMatruhFormation(directV12,
            Submission(directV12, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(5, directV12.SourceAuthority!.MatruhFormation!.AttachedIds.Count);
        var matruhDetach = CampaignSourceAuthorityState.MatruhFormationActionId("detach", "1-essex");
        directV12 = CampaignExercises.SubmitSourceMatruhFormation(directV12,
            Submission(directV12, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(4, directV12.SourceAuthority!.MatruhFormation!.AttachedIds.Count);
        Assert.Contains(directV12.SourceAuthority.Battlefield!.Entries,
            entry => entry.ElementId == "1-essex" && entry.AuthorityStatus == "source-matruh-detached");
        var secondMatruhDetach = CampaignSourceAuthorityState.MatruhFormationActionId("detach", "1-durham-light-infantry");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMatruhFormation(directV12, Submission(directV12, secondMatruhDetach, CampaignActionAudience.Commonwealth)).RejectionReason);
        directV12 = CampaignExercises.PromoteSourceMatruhDetachedWorld(directV12).SuccessorSession!;
        Assert.Equal(26, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(21, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(4, directV12.SourceAuthority.MatruhFormation!.AttachedIds.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        cwFirst = CampaignExercises.AdmitSourceMatruhFormation(cwFirst).SuccessorSession!;
        cwFirst = CampaignExercises.SubmitSourceMatruhFormation(cwFirst,
            Submission(cwFirst, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        cwFirst = CampaignExercises.SubmitSourceMatruhFormation(cwFirst,
            Submission(cwFirst, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        cwFirst = CampaignExercises.PromoteSourceMatruhDetachedWorld(cwFirst).SuccessorSession!;
        var cwFirstIdentity = cwFirst.SourceAuthority!.World.ContentIdentity;
        cwFirst = CampaignExercises.SubmitSourceMalettiHierarchy(cwFirst,
            Submission(cwFirst, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        cwFirst = CampaignExercises.PromoteSourceCertifiedMalettiWorld(cwFirst).SuccessorSession!;
        Assert.NotEqual(cwFirstIdentity, cwFirst.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(21, cwFirst.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, cwFirst.SourceAuthority.World.PendingArrivals.Count);
        Assert.True(CampaignExercises.Reconstruct(cwFirst).IsVerified);
        var rngBeforeGt2 = directV12.SourceAuthority!.RandomState;
        var worldBeforeGt2 = directV12.SourceAuthority.World.ContentIdentity;
        directV12 = CampaignExercises.AdmitSourceGt2Correction(directV12).SuccessorSession!;
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var finishGt2 = CampaignSourceAuthorityState.Gt2CorrectionActionId("finish", 0, 0, 0);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Commonwealth)).RejectionReason);
        if (maximum > 0)
        {
            foreach (var lane in new[] { 2, 3, 6 })
                for (var stage = 1; stage <= 3; stage++)
                    while (directV12.SourceAuthority!.Gt2Correction!.Quantity(lane, stage) > 0)
                    {
                        var removeUnit = CampaignSourceAuthorityState.Gt2CorrectionActionId("adjust", lane, stage, -1);
                        directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12,
                            Submission(directV12, removeUnit, CampaignActionAudience.Axis)).SuccessorSession!;
                    }
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Axis)).RejectionReason);
            var addGt2 = CampaignSourceAuthorityState.Gt2CorrectionActionId("adjust", 2, 1, 1);
            directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, addGt2, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(directV12.SourceAuthority!.Gt2Correction!.IsFinished);
        Assert.Equal(rngBeforeGt2, directV12.SourceAuthority.RandomState);
        Assert.Equal(worldBeforeGt2, directV12.SourceAuthority.World.ContentIdentity);
        directV12 = CampaignExercises.AdmitSourceCwPriorityCatalog(directV12).SuccessorSession!;
        Assert.Equal(28, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        directV12 = CampaignExercises.PromoteSourceCwPriorityWorld(directV12).SuccessorSession!;
        Assert.Equal(29, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(22, directV12.SourceAuthority!.World.World.Elements.Count);
        var frenchAdd = CampaignSourceAuthorityState.CwPriorityCatalogActionId("1-french-motor-marines", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceCwPriorityCatalog(directV12, Submission(directV12, frenchAdd, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceCwPriorityCatalog(directV12,
            Submission(directV12, frenchAdd, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, directV12.SourceAuthority!.CwPriorityCatalog!.FrenchPoints);
        Assert.Equal(1, directV12.SourceAuthority.CwPriorityCatalog.Remaining(directV12.SourceAuthority, "cw-c3926-attached"));
        var frenchRemove = CampaignSourceAuthorityState.CwPriorityCatalogActionId("1-french-motor-marines", -1);
        directV12 = CampaignExercises.SubmitSourceCwPriorityCatalog(directV12,
            Submission(directV12, frenchRemove, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(2, directV12.SourceAuthority!.CwPriorityCatalog!.Remaining(directV12.SourceAuthority, "cw-c3926-attached"));
        frenchFirst = CampaignExercises.AdmitSourceCwPriorityCatalog(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.PromoteSourceCwPriorityWorld(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.AdmitSourceCwFreeGuns(frenchFirst).SuccessorSession!;
        Assert.Equal(31, frenchFirst.SourceAuthority!.ProfileVersion);
        var freeGunIds = frenchFirst.SourceAuthority!.CwFreeGuns!.PendingIds.ToArray();
        var gunCairoHex = seed == 14 ? "e1730" : "e1830";
        foreach (var id in freeGunIds)
        {
            var placeGun = CampaignSourceAuthorityState.CwFreeGunPlacementActionId(id, gunCairoHex);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCwFreeGun(frenchFirst, Submission(frenchFirst, placeGun, CampaignActionAudience.Axis)).RejectionReason);
            frenchFirst = CampaignExercises.PlaceSourceCwFreeGun(frenchFirst,
                Submission(frenchFirst, placeGun, CampaignActionAudience.Commonwealth)).SuccessorSession!;
            frenchFirst = CampaignExercises.PromoteSourceCwFreeGunsWorld(frenchFirst).SuccessorSession!;
        }
        Assert.Equal(32, frenchFirst.SourceAuthority!.ProfileVersion);
        _ = ContentSourceFourthIndianPackArtifact.Create(frenchFirst.SourceAuthority!.CwFreeGunsWorld!.Content);
        var indianResult = CampaignExercises.AdmitSourceFourthIndianWorld(frenchFirst);
        Assert.True(indianResult.IsAccepted);
        var indian = indianResult.SuccessorSession!;
        var grouped = indian.SourceAuthority!.GroupedWorld!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(indian.SourceAuthority.SerializeLegacyCheckpoint()),
            indian.SourceAuthority.DigestLegacyCheckpoint());
        var beforeMinor = indian.SourceAuthority.SerializeCheckpoint();
        var pendingBeforeMinor = indian.SourceAuthority.World.PendingArrivals;
        var randomBeforeMinor = indian.SourceAuthority.RandomState;
        var minorResult = CampaignExercises.ActivateSourceAxisMinorGarrisons(indian);
        Assert.True(minorResult.IsAccepted);
        var minor = minorResult.SuccessorSession!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(minor.SourceAuthority!.CwFreeGunsWorld!.SerializeCanonical()),
            minor.SourceAuthority.CwFreeGunsWorld.Hash());
        Assert.Equal(CampaignPregameSourceCreation.Digest(minor.SourceAuthority!.SerializeLegacyCheckpoint()),
            minor.SourceAuthority.DigestLegacyCheckpoint());
        Assert.Equal(beforeMinor, indian.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.AxisMinorGarrisonsCheckpointContractVersion, minor.SourceAuthority!.ProfileVersion);
        Assert.Equal(randomBeforeMinor, minor.SourceAuthority.RandomState);
        Assert.Equal(pendingBeforeMinor, minor.SourceAuthority.World.PendingArrivals);
        Assert.Equal(grouped.ContentHash, minor.SourceAuthority.GroupedWorld!.ContentHash);
        Assert.Equal(grouped.Groups.Count, minor.SourceAuthority.GroupedWorld.Groups.Count);
        Assert.Equal(0, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("fort-maddalena-garrison", "c3019"));
        Assert.Equal(1, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("fort-maddalena-garrison", "c3020"));
        Assert.Equal(0, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("mechili-garrison", "b4921"));
        Assert.Equal(1, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("mechili-garrison", "b4922"));
        foreach (var unit in ContentSourceAxisMinorGarrisonsPackArtifact.Units)
        {
            Assert.Equal(0, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt(unit.Id, unit.Location));
            var element = Assert.Single(minor.SourceAuthority.World.World.Elements, value => value.ElementId == unit.Id);
            Assert.Equal(unit.Location, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
            Assert.Single(minor.SourceAuthority.World.World.Representations, value =>
                value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
                && value.CurrentLocationId == unit.Location && value.BoundElementIds.SequenceEqual([unit.Id]));
            Assert.Contains(minor.SourceAuthority.Battlefield!.Entries, value => value.ElementId == unit.Id
                && value.AuthorityStatus == "source-axis-minor-active");
        }
        // Use the actual activated counters, not source-evidence-only RTR or
        // Hussars. The two observers are independent counters, not formations.
        var minorContactWorld = CampaignSourcePlayableWorld.Create(minor.SourceAuthority);
        var activePair = CampaignSourcePolishArrivalContacts.EffectiveCounters(minorContactWorld)
            .Where(value => value.RepresentationId is "axis-minor.scheferzen-garrison" or "independent-setup.2-rifle-brigade")
            .ToArray();
        Assert.Equal(2, activePair.Length);
        Assert.All(activePair, value => Assert.Equal(CampaignContactObserverKind.IndependentCounter, value.ObserverKind));
        var pairAxis = new CampaignContactKnowledge(minor.SourceAuthority.Request.CampaignId, LandSide.Axis);
        var pairCw = new CampaignContactKnowledge(minor.SourceAuthority.Request.CampaignId, LandSide.Commonwealth);
        var axisPairContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(activePair, 1, pairAxis));
        var cwPairContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(activePair, 1, pairCw));
        Assert.Equal(CampaignContactKnowledge.IndependentObserverContractVersion, axisPairContact.ContractVersion);
        Assert.Equal(CampaignContactKnowledge.IndependentObserverContractVersion, cwPairContact.ContractVersion);
        var axisPairBytes = CampaignContactTransitionCodec.SerializeCanonical(axisPairContact);
        Assert.Equal(axisPairContact, CampaignContactTransitionCodec.DeserializeCanonical(axisPairBytes));
        Assert.Equal(axisPairBytes, CampaignContactTransitionCodec.SerializeCanonical(Assert.Single(
            CampaignSourceInitialContacts.DeriveCounters(activePair, 1,
                new CampaignContactKnowledge(minor.SourceAuthority.Request.CampaignId, LandSide.Axis)))));
        pairAxis.Apply(axisPairContact);
        pairCw.Apply(cwPairContact);
        Assert.Empty(pairAxis.ObserveFor(axisPairContact.ObserverFormationId));
        var ownCounterView = CampaignIndependentContactObservationV2.Project(minorContactWorld, pairAxis,
            "axis-minor.scheferzen-garrison");
        Assert.Single(ownCounterView.Contacts);
        Assert.DoesNotContain("independent-setup.2-rifle-brigade", System.Text.Encoding.UTF8.GetString(ownCounterView.SerializeCanonical()));
        Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(minorContactWorld,
            pairCw, "axis-minor.scheferzen-garrison"));
        var repeatedPairContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(activePair, 2, pairAxis));
        Assert.Equal(axisPairContact.ContactHandle, repeatedPairContact.ContactHandle);
        Assert.True(CampaignExercises.Reconstruct(minor).IsVerified);
        var compactMinorResult = CampaignExercises.AdmitSourceCompactCheckpoint(minor);
        Assert.True(compactMinorResult.IsAccepted);
        var compactMinor = compactMinorResult.SuccessorSession!;
        var compactMinorBytes = compactMinor.SourceAuthority!.SerializeCheckpoint();
        using (var compactMinorJson = JsonDocument.Parse(compactMinorBytes))
        {
            var baseBytes = compactMinorJson.RootElement.GetProperty("baseCheckpoint").GetBytesFromBase64();
            var artifactBytes = compactMinor.SourceAuthority.CwFreeGunsWorld!.MinorGarrisonContent!.CanonicalBytes;
            Console.WriteLine($"minor checkpoint bytes: compact={compactMinorBytes.Length}, base={baseBytes.Length}, artifact={artifactBytes.Length}");
            Assert.True(compactMinorBytes.Length < 16_384, $"Compact minor checkpoint was {compactMinorBytes.Length} bytes; base {baseBytes.Length}, artifact {artifactBytes.Length}.");
            Assert.False(compactMinorJson.RootElement.TryGetProperty("minorGarrisonContent", out _));
            Assert.Equal(compactMinor.SourceAuthority.World.ContentIdentity.Hash,
                compactMinorJson.RootElement.GetProperty("minorGarrisonContentHash").GetString());
        }
        Assert.True(CampaignExercises.Reconstruct(compactMinor).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceAxisGarrisonRemainder(minor).IsAccepted);
        var oldMinorCheckpoint = compactMinor.SourceAuthority.SerializeCheckpoint();
        var remainderResult = CampaignExercises.ActivateSourceAxisGarrisonRemainder(compactMinor);
        Assert.True(remainderResult.IsAccepted);
        var remainder = remainderResult.SuccessorSession!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(remainder.SourceAuthority!.CwFreeGunsWorld!.SerializeCanonical()),
            remainder.SourceAuthority.CwFreeGunsWorld.Hash());
        Console.WriteLine($"garrison remainder content hash: {remainder.SourceAuthority!.World.ContentIdentity.Hash}");
        if (seed == 14)
            Assert.Equal(CampaignPregameSourceCreation.Digest(remainder.SourceAuthority.SerializeLegacyCheckpoint()),
                remainder.SourceAuthority.DigestLegacyCheckpoint());
        var remainderCheckpointBytes = remainder.SourceAuthority!.SerializeCheckpoint();
        Assert.True(remainderCheckpointBytes.Length < 24 * 1024 * 1024,
            $"Legacy garrison remainder checkpoint was {remainderCheckpointBytes.Length} bytes.");
        Assert.Equal(oldMinorCheckpoint, compactMinor.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.AxisGarrisonRemainderCheckpointContractVersion, remainder.SourceAuthority.ProfileVersion);
        Assert.Equal(ContentSourceAxisGarrisonRemainderPackArtifact.ContentHash, remainder.SourceAuthority.World.ContentIdentity.Hash);
        Assert.Equal(minor.SourceAuthority.RandomState, remainder.SourceAuthority.RandomState);
        Assert.Equal(minor.SourceAuthority.World.PendingArrivals, remainder.SourceAuthority.World.PendingArrivals);
        Assert.Equal(minor.SourceAuthority.GroupedWorld!.ContentHash, remainder.SourceAuthority.GroupedWorld!.ContentHash);
        Assert.Equal(ExpectedGarrisonRemainderIds,
            ContentSourceAxisGarrisonRemainderPackArtifact.Units.Select(value => value.Id).Order(StringComparer.Ordinal));
        foreach (var unit in ContentSourceAxisGarrisonRemainderPackArtifact.Units)
        {
            var element = Assert.Single(remainder.SourceAuthority.World.World.Elements, value => value.ElementId == unit.Id);
            var sourceComponent = ContentSourceAxisGarrisonRemainderPackArtifact.Facts(unit.Id).Components.Single();
            Assert.Equal(unit.Location, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
            Assert.Equal(sourceComponent.ComponentId, element.Components[0].ComponentId);
            Assert.Equal(0, ContentSourceAxisGarrisonRemainderPackArtifact.StackAt(unit.Id, unit.Location));
            Assert.Single(remainder.SourceAuthority.World.World.Representations, value =>
                value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
                && value.CurrentLocationId == unit.Location && value.BoundElementIds.SequenceEqual([unit.Id]));
            Assert.Contains(remainder.SourceAuthority.Battlefield!.Entries, value => value.ElementId == unit.Id
                && value.AuthorityStatus == "source-axis-garrison-remainder-active");
        }
        var remainderContactWorld = CampaignSourcePlayableWorld.Create(remainder.SourceAuthority);
        var remainderCounters = CampaignSourcePolishArrivalContacts.EffectiveCounters(remainderContactWorld);
        var remainderAxisKnowledge = new CampaignContactKnowledge(remainder.SourceAuthority.Request.CampaignId, LandSide.Axis);
        var remainderCwKnowledge = new CampaignContactKnowledge(remainder.SourceAuthority.Request.CampaignId, LandSide.Commonwealth);
        // Both counters come from the admitted source world. The chart proves
        // Soluch and Matruh are separated without inferring a playable seam.
        var soluchAndIndian = remainderCounters.Where(value => value.RepresentationId is
            "axis-garrison.soluch-antitank-company" or "4-indian-division").ToArray();
        Assert.Equal(2, soluchAndIndian.Length);
        Assert.Empty(CampaignSourceInitialContacts.DeriveCounters(soluchAndIndian, 1, remainderAxisKnowledge));
        Assert.Empty(CampaignSourceInitialContacts.DeriveCounters(soluchAndIndian, 1, remainderCwKnowledge));
        Assert.Equal(6, ContentSourceAxisGarrisonRemainderPackArtifact.Units.Count);
        foreach (var removedId in ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds)
        {
            var observerId = "axis-garrison." + removedId;
            Assert.DoesNotContain(remainderCounters, value => value.RepresentationId == observerId);
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(remainderContactWorld, remainderAxisKnowledge, observerId));
        }
        foreach (var unit in ContentSourceAxisGarrisonRemainderPackArtifact.Units)
        {
            var id = "axis-garrison." + unit.Id;
            var counter = Assert.Single(remainderCounters, value => value.RepresentationId == id);
            Assert.Equal(CampaignContactObserverKind.IndependentCounter, counter.ObserverKind);
            Assert.Equal(id, counter.ObserverFormationId);
            Assert.Empty(CampaignIndependentContactObservationV2.Project(remainderContactWorld, remainderAxisKnowledge, id).Contacts);
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(remainderContactWorld, remainderCwKnowledge, id));
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(minorContactWorld, pairAxis, id));
        }
        Assert.Equal(1, ContentSourceAxisGarrisonRemainderPackArtifact.StackAt("soluch-artillery-regiment", "a4131"));
        Assert.Equal(1, ContentSourceAxisGarrisonRemainderPackArtifact.StackAt("giarabub-machine-gun", "c1015"));
        Assert.DoesNotContain(remainder.SourceAuthority.World.World.Elements, value => value.ElementId == "giarabub-motor-machine-gun");
        Assert.Contains(remainder.SourceAuthority.Battlefield!.Entries, value => value.ElementId == "giarabub-motor-machine-gun"
            && value.AuthorityStatus == "source-evidence");
        foreach (var id in ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds)
        {
            Assert.DoesNotContain(remainder.SourceAuthority.World.World.Elements, value => value.ElementId == id);
            Assert.DoesNotContain(remainder.SourceAuthority.World.World.Representations, value => value.BoundElementIds.Contains(id));
            var removedEntry = Assert.Single(remainder.SourceAuthority.Battlefield!.Entries, value => value.ElementId == id
                && value.AuthorityStatus == "source-land-only-aa-removed");
            Assert.DoesNotContain(CampaignSourcePlayableWorld.Create(remainder.SourceAuthority).UnresolvedAt(removedEntry.LocationId!),
                value => value.ElementId == id);
        }
        using (var contentDocument = JsonDocument.Parse(remainder.SourceAuthority.CwFreeGunsWorld!.GarrisonRemainderContent!.CanonicalBytes))
        {
            var removedIds = contentDocument.RootElement.GetProperty("removedAntiAirUnits").EnumerateArray()
                .Select(value => value.GetString()!).ToArray();
            Assert.Equal(ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds, removedIds);
            Assert.Equal("land:32.83(1)-italian-uu-removed-from-play", contentDocument.RootElement.GetProperty("antiAirTreatment").GetString());
        }
        Assert.True(CampaignExercises.Reconstruct(remainder).IsVerified);
        var remainderHistory = remainder.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var remainderEvent = JsonDocument.Parse(remainderHistory[^1]))
        {
            var worldHash = remainderEvent.RootElement.GetProperty("worldHash").GetString()!;
            var changedHash = worldHash[..^1] + (worldHash[^1] == '0' ? '1' : '0');
            remainderHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(remainderHistory[^1])
                .Replace(worldHash, changedHash, StringComparison.Ordinal));
        }
        var tamperedRemainder = new ExerciseSession(remainder.Pregame!, remainder.CurrentContacts,
            remainderHistory, remainder.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(tamperedRemainder).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceAxisGarrisonRemainder(remainder).IsAccepted);
        var compactRemainder = remainder;
        var compactRemainderBytes = compactRemainder.SourceAuthority!.SerializeCheckpoint();
        using (var remainderCheckpoint = JsonDocument.Parse(compactRemainderBytes))
        {
            Assert.True(compactRemainderBytes.Length < 16_384, $"Compact remainder checkpoint was {compactRemainderBytes.Length} bytes.");
            Assert.False(remainderCheckpoint.RootElement.TryGetProperty("garrisonRemainderContent", out _));
            Assert.Equal(compactRemainder.SourceAuthority.World.ContentIdentity.Hash,
                remainderCheckpoint.RootElement.GetProperty("garrisonRemainderContentHash").GetString());
        }
        Assert.True(CampaignExercises.Reconstruct(compactRemainder).IsVerified);
        var priorRemainderCheckpoint = remainder.SourceAuthority.SerializeCheckpoint();
        var priorAxisMotorization = remainder.SourceAuthority.AxisMotorization.Hash();
        var motorMgResult = CampaignExercises.ActivateSourceGiarabubMotorMachineGun(remainder);
        Assert.True(motorMgResult.IsAccepted);
        var motorMg = motorMgResult.SuccessorSession!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(motorMg.SourceAuthority!.CwFreeGunsWorld!.SerializeCanonical()),
            motorMg.SourceAuthority.CwFreeGunsWorld.Hash());
        Assert.Equal(priorRemainderCheckpoint, remainder.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.GiarabubMotorMachineGunCheckpointContractVersion, motorMg.SourceAuthority!.ProfileVersion);
        Assert.Equal(priorAxisMotorization, motorMg.SourceAuthority.AxisMotorization.Hash());
        Assert.Equal(remainder.SourceAuthority.RandomState, motorMg.SourceAuthority.RandomState);
        Assert.Equal(remainder.SourceAuthority.World.PendingArrivals, motorMg.SourceAuthority.World.PendingArrivals);
        var motorMgElement = Assert.Single(motorMg.SourceAuthority.World.World.Elements,
            value => value.ElementId == ContentSourceGiarabubMotorMachineGunArtifact.ElementId);
        Assert.Equal("c1014", motorMgElement.CurrentLocationId);
        Assert.Equal(1, Assert.Single(motorMgElement.Components).CurrentToe);
        Assert.Single(motorMg.SourceAuthority.World.World.Representations, value =>
            value.RepresentationId == "axis-garrison.giarabub-motor-machine-gun"
            && value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement);
        var motorMgContent = motorMg.SourceAuthority.CwFreeGunsWorld!.MotorMachineGunContent!;
        Console.WriteLine($"giarabub motor MG content hash: {motorMgContent.Identity.Hash}");
        Assert.Equal(ContentSourceGiarabubMotorMachineGunArtifact.ContentHash, motorMgContent.Identity.Hash);
        using (var document = JsonDocument.Parse(motorMgContent.CanonicalBytes))
        {
            var root = document.RootElement;
            Assert.Equal("8+", root.GetProperty("printedCapabilityPoints").GetString());
            Assert.Equal(JsonValueKind.Null, root.GetProperty("effectiveCapabilityPoints").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("mobilityId").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("motorPoolId").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("assignedMotorPoints").ValueKind);
        }
        var motorMgWorld = CampaignSourcePlayableWorld.Create(motorMg.SourceAuthority);
        Assert.Contains("giarabub-motor-mg-historical-motorization-priority-unresolved", motorMgWorld.ReadinessBlockers());
        Assert.Contains("giarabub-motor-mg-capability-and-mobility-unresolved", motorMgWorld.ReadinessBlockers());
        var motorMgCounter = Assert.Single(CampaignSourcePolishArrivalContacts.EffectiveCounters(motorMgWorld),
            value => value.RepresentationId == "axis-garrison.giarabub-motor-machine-gun");
        Assert.Equal(CampaignContactObserverKind.IndependentCounter, motorMgCounter.ObserverKind);
        Assert.Equal(motorMgCounter.RepresentationId, motorMgCounter.ObserverFormationId);
        var motorMgAxisKnowledge = new CampaignContactKnowledge(motorMg.SourceAuthority.Request.CampaignId, LandSide.Axis);
        var motorMgCwKnowledge = new CampaignContactKnowledge(motorMg.SourceAuthority.Request.CampaignId, LandSide.Commonwealth);
        Assert.Empty(CampaignIndependentContactObservationV2.Project(motorMgWorld, motorMgAxisKnowledge, motorMgCounter.RepresentationId).Contacts);
        Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(motorMgWorld, motorMgCwKnowledge, motorMgCounter.RepresentationId));
        Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(remainderContactWorld, motorMgAxisKnowledge, motorMgCounter.RepresentationId));
        var motorAndIndian = CampaignSourcePolishArrivalContacts.EffectiveCounters(motorMgWorld)
            .Where(value => value.RepresentationId is "axis-garrison.giarabub-motor-machine-gun" or "4-indian-division").ToArray();
        Assert.Equal(2, motorAndIndian.Length);
        Assert.Empty(CampaignSourceInitialContacts.DeriveCounters(motorAndIndian, 1, motorMgAxisKnowledge));
        // Exercise the verified Giarabub hexside using the real counters; this
        // hypothetical position is not an admitted campaign move.
        var adjacentIndian = motorAndIndian.Single(value => value.RepresentationId == "4-indian-division") with { LocationId = "c1015" };
        var motorContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters([motorMgCounter, adjacentIndian], 1, motorMgAxisKnowledge));
        Assert.Equal(CampaignContactObserverKind.IndependentCounter, motorContact.ObserverKind);
        Assert.Equal(motorMgCounter.RepresentationId, motorContact.ObserverFormationId);
        Assert.False(motorMgWorld.IsGameplayReady);
        Assert.False(CampaignExercises.StartSourceTurn(motorMg).IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(motorMg).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceGiarabubMotorMachineGun(motorMg).IsAccepted);
        var motorMgCheckpoint = motorMg.SourceAuthority.SerializeCheckpoint();
        Assert.True(motorMgCheckpoint.Length < 16_384, $"Compact motor MG checkpoint was {motorMgCheckpoint.Length} bytes.");
        using (var document = JsonDocument.Parse(motorMgCheckpoint))
            Assert.Equal(motorMgContent.Identity.Hash, document.RootElement.GetProperty("motorMachineGunContentHash").GetString());
        var cwFrontlineResult = CampaignExercises.ActivateSourceCwFrontline(motorMg);
        Assert.True(cwFrontlineResult.IsAccepted);
        var cwFrontline = cwFrontlineResult.SuccessorSession!;
        var cwAuthority = cwFrontline.SourceAuthority!;
        if (seed == 14)
        {
            var loads = cwFrontline;
            Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAttachedInitialLoadsDraft(loads, CampaignActionAudience.Axis));
            using (var opening = JsonDocument.Parse(CampaignExercises.QuerySourceAttachedInitialLoadsDraft(loads, CampaignActionAudience.Commonwealth).VisibleBytes))
            {
                Assert.Equal(2, opening.RootElement.GetProperty("contractVersion").GetInt32());
                Assert.Contains(opening.RootElement.GetProperty("frontlinePools").EnumerateArray(), value =>
                    value.GetProperty("elementId").GetString() == "1-rtr" && value.GetProperty("totalPoints").GetInt32() == 5);
            }
            Assert.False(CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-finish", "frontline"),
                    CampaignActionAudience.Commonwealth)).IsAccepted);
            foreach (var (id, count) in new[] { ("1-rtr", 5), ("11-hussars", 3) })
                for (var i = 0; i < count; i++)
                    loads = CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                        Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-attach", id),
                            CampaignActionAudience.Commonwealth)).SuccessorSession!;
            var attachOverflow = CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-attach", "1-rtr"),
                    CampaignActionAudience.Commonwealth));
            Assert.False(attachOverflow.IsAccepted);
            var emptyFinished = CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-finish", "frontline"),
                    CampaignActionAudience.Commonwealth)).SuccessorSession!;
            Assert.DoesNotContain("cw-frontline-initial-loads-unresolved",
                CampaignSourcePlayableWorld.Create(emptyFinished.SourceAuthority!).ReadinessBlockers());
            Assert.False(CampaignExercises.StartSourceTurn(emptyFinished).IsAccepted);
            Assert.True(CampaignExercises.Reconstruct(emptyFinished).IsVerified);
            var reopenedEmpty = CampaignExercises.SubmitSourceAttachedInitialLoads(emptyFinished,
                Submission(emptyFinished, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-reopen", "frontline"),
                    CampaignActionAudience.Commonwealth)).SuccessorSession!;
            Assert.Contains("cw-frontline-initial-loads-unresolved",
                CampaignSourcePlayableWorld.Create(reopenedEmpty.SourceAuthority!).ReadinessBlockers());
            foreach (var (kind, id) in new[] { ("frontline-ammo", "1-rtr"), ("frontline-fuel", "11-hussars"), ("frontline-finish", "frontline") })
                loads = CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                    Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId(kind, id),
                        CampaignActionAudience.Commonwealth)).SuccessorSession!;
            var loadState = loads.SourceAuthority!.AttachedInitialLoads!;
            Assert.True(loadState.FrontlineFinished);
            Assert.Equal(5, loadState.Attached("1-rtr"));
            Assert.Equal(3, loadState.Attached("11-hussars"));
            Assert.Equal(1, Assert.Single(loadState.Targets, value => value.ElementId == "1-rtr").AmmoPoints);
            Assert.Equal(1, Assert.Single(loadState.Targets, value => value.ElementId == "11-hussars").FuelPoints);
            Assert.Contains("cw-frontline-initial-loads-unresolved", CampaignSourcePlayableWorld.Create(loads.SourceAuthority).ReadinessBlockers());
            Assert.False(CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-ammo", "1-rtr"),
                    CampaignActionAudience.Commonwealth)).IsAccepted);
            Assert.True(CampaignExercises.Reconstruct(loads).IsVerified);
        }
        Assert.True(CampaignExercises.IsSourceCwFrontlineActivated(cwFrontline));
        Assert.Equal(motorMgCheckpoint, motorMg.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.CwFrontlineCheckpointContractVersion, cwAuthority.ProfileVersion);
        Assert.Equal(motorMg.SourceAuthority.CommonwealthMotorization.Hash(), cwAuthority.CommonwealthMotorization.Hash());
        Assert.Equal(motorMg.SourceAuthority.AxisMotorization.Hash(), cwAuthority.AxisMotorization.Hash());
        Assert.Equal(motorMg.SourceAuthority.RandomState, cwAuthority.RandomState);
        Assert.Equal(motorMg.SourceAuthority.World.PendingArrivals, cwAuthority.World.PendingArrivals);
        foreach (var unit in ContentSourceCwFrontlineArtifact.Units)
        {
            var element = Assert.Single(cwAuthority.World.World.Elements, value => value.ElementId == unit.Id);
            Assert.Equal(unit.Location, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
            Assert.Single(cwAuthority.World.World.Representations, value => value.RepresentationId == "cw-frontline." + unit.Id
                && value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement);
            Assert.Contains(cwAuthority.Battlefield!.Entries, value => value.ElementId == unit.Id
                && value.AuthorityStatus == "source-cw-frontline-active-initial-loads-unresolved");
        }
        var cwNativeWorld = cwAuthority.CwFreeGunsWorld!;
        var freshHashWorld = remainder.SourceAuthority.CwFreeGunsWorld!.ActivateGiarabubMotorMachineGun(
            remainder.SourceAuthority.AxisGarrisons!, remainder.SourceAuthority.Battlefield!);
        var canonicalWorldHash = CampaignPregameSourceCreation.Digest(freshHashWorld.SerializeCanonical());
        var concurrentWorldHashes = new string[8];
        Parallel.For(0, concurrentWorldHashes.Length, index => concurrentWorldHashes[index] = freshHashWorld.Hash());
        Assert.All(concurrentWorldHashes, hash => Assert.Equal(canonicalWorldHash, hash));
        var cwContent = cwNativeWorld.CwFrontlineContent!;
        var cwContactWorld = CampaignSourcePlayableWorld.Create(cwAuthority);
        var cwEffectiveCounters = CampaignSourcePolishArrivalContacts.EffectiveCounters(cwContactWorld);
        foreach (var (cwId, axisId) in new[]
        {
            ("cw-frontline.1-rtr", "axis-minor.scheferzen-garrison"),
            ("cw-frontline.11-hussars", "axis-minor.fort-maddalena-garrison"),
        })
        {
            var actualPair = cwEffectiveCounters.Where(value => value.RepresentationId == cwId || value.RepresentationId == axisId).ToArray();
            Assert.Equal(2, actualPair.Length);
            Assert.All(actualPair, value => Assert.Equal(CampaignContactObserverKind.IndependentCounter, value.ObserverKind));
            var cwKnowledge = new CampaignContactKnowledge(cwAuthority.Request.CampaignId, LandSide.Commonwealth);
            var axisKnowledge = new CampaignContactKnowledge(cwAuthority.Request.CampaignId, LandSide.Axis);
            var cwContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(actualPair, 1, cwKnowledge));
            var axisContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(actualPair, 1, axisKnowledge));
            Assert.Equal(cwId, cwContact.ObserverFormationId);
            Assert.Equal(axisId, axisContact.ObserverFormationId);
            var serialized = CampaignContactTransitionCodec.SerializeCanonical(cwContact);
            Assert.Equal(cwContact, CampaignContactTransitionCodec.DeserializeCanonical(serialized));
            Assert.Equal(serialized, CampaignContactTransitionCodec.SerializeCanonical(Assert.Single(
                CampaignSourceInitialContacts.DeriveCounters(actualPair, 1,
                    new CampaignContactKnowledge(cwAuthority.Request.CampaignId, LandSide.Commonwealth)))));
            cwKnowledge.Apply(cwContact);
            axisKnowledge.Apply(axisContact);
            var cwView = CampaignIndependentContactObservationV2.Project(cwContactWorld, cwKnowledge, cwId);
            Assert.Single(cwView.Contacts);
            Assert.DoesNotContain(axisId, System.Text.Encoding.UTF8.GetString(cwView.SerializeCanonical()));
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(cwContactWorld, axisKnowledge, cwId));
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(
                CampaignSourcePlayableWorld.Create(motorMg.SourceAuthority), cwKnowledge, cwId));
            Assert.Equal(cwContact.ContactHandle, Assert.Single(CampaignSourceInitialContacts.DeriveCounters(actualPair, 2, cwKnowledge)).ContactHandle);
        }
        Assert.Equal(ContentSourceCwFrontlineArtifact.ContentHash, cwContent.Identity.Hash);
        Assert.Equal(cwContent.Identity, ContentSourceCwFrontlineArtifact.Read(cwContent.CanonicalBytes).Identity);
        if (seed == 14)
        {
            var changedMotor = motorMgContent.CanonicalBytes;
            var motorFormat = changedMotor.AsSpan().IndexOf("sandtable.source-giarabub-motor-mg.v1"u8);
            Assert.True(motorFormat >= 0);
            changedMotor[motorFormat] = (byte)'X';
            Assert.Throws<InvalidOperationException>(() => ContentSourceGiarabubMotorMachineGunArtifact.Read(changedMotor));
            var changedFrontline = cwContent.CanonicalBytes;
            var frontlineFormat = changedFrontline.AsSpan().IndexOf("sandtable.source-cw-frontline.v1"u8);
            Assert.True(frontlineFormat >= 0);
            changedFrontline[frontlineFormat] = (byte)'X';
            Assert.Throws<InvalidOperationException>(() => ContentSourceCwFrontlineArtifact.Read(changedFrontline));
        }
        using (var document = JsonDocument.Parse(cwContent.CanonicalBytes))
            foreach (var unit in document.RootElement.GetProperty("units").EnumerateArray())
            {
                if (unit.GetProperty("elementId").GetString() == "11-hussars")
                    Assert.Equal(1, unit.GetProperty("chartArmorRating").GetInt32());
                Assert.Equal(JsonValueKind.Null, unit.GetProperty("initialCargo").ValueKind);
                Assert.Equal(JsonValueKind.Null, unit.GetProperty("parentFormationId").ValueKind);
            }
        Assert.Contains("cw-frontline-initial-loads-unresolved", CampaignSourcePlayableWorld.Create(cwAuthority).ReadinessBlockers());
        Assert.False(CampaignExercises.StartSourceTurn(cwFrontline).IsAccepted);
        Assert.False(CampaignExercises.ActivateSourceCwFrontline(cwFrontline).IsAccepted);
        var cwCheckpoint = cwAuthority.SerializeCheckpoint();
        Assert.True(cwCheckpoint.Length < 16_384, $"Compact CW frontline checkpoint was {cwCheckpoint.Length} bytes.");
        using (var document = JsonDocument.Parse(cwCheckpoint))
            Assert.Equal(cwContent.Identity.Hash, document.RootElement.GetProperty("cwFrontlineContentHash").GetString());
        Assert.True(CampaignExercises.Reconstruct(cwFrontline).IsVerified);
        var cwHistory = cwFrontline.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var document = JsonDocument.Parse(cwHistory[^1]))
        {
            var hash = document.RootElement.GetProperty("worldHash").GetString()!;
            var changed = hash[..^1] + (hash[^1] == '0' ? '1' : '0');
            cwHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(cwHistory[^1])
                .Replace(hash, changed, StringComparison.Ordinal));
        }
        Assert.False(CampaignExercises.Reconstruct(new ExerciseSession(cwFrontline.Pregame!, cwFrontline.CurrentContacts,
            cwHistory, cwFrontline.SourceAuthority)).IsVerified);
        var motorMgHistory = motorMg.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var document = JsonDocument.Parse(motorMgHistory[^1]))
        {
            var hash = document.RootElement.GetProperty("worldHash").GetString()!;
            var changed = hash[..^1] + (hash[^1] == '0' ? '1' : '0');
            motorMgHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(motorMgHistory[^1])
                .Replace(hash, changed, StringComparison.Ordinal));
        }
        Assert.False(CampaignExercises.Reconstruct(new ExerciseSession(motorMg.Pregame!, motorMg.CurrentContacts,
            motorMgHistory, motorMg.SourceAuthority)).IsVerified);
        var minorHistory = minor.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var eventDocument = JsonDocument.Parse(minorHistory[^1]))
        {
            var worldHash = eventDocument.RootElement.GetProperty("worldHash").GetString()!;
            var changedHash = worldHash[..^1] + (worldHash[^1] == '0' ? '1' : '0');
            minorHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(minorHistory[^1])
                .Replace(worldHash, changedHash, StringComparison.Ordinal));
        }
        var tamperedMinor = new ExerciseSession(minor.Pregame!, minor.CurrentContacts, minorHistory, minor.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(tamperedMinor).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceAxisMinorGarrisons(minor).IsAccepted);
        var minorFinish = CampaignExercises.Query(minor, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var minorFinished = CampaignExercises.Submit(minor,
            Submission(minor, minorFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(minorFinished).IsVerified);
        Assert.Equal(minor.SourceAuthority.World.ContentIdentity, minorFinished.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(ContentSourceFourthIndianPackArtifact.ContentHash, grouped.ContentHash);
        Assert.Equal(11, grouped.Groups.Single().Members.Count);
        var originalGroup = grouped.Groups.Single();
        var forgedMembers = originalGroup.Members.Select(value => value.ElementId == "25-field-artillery"
            ? value with { AdditionalComponents = Array.AsReadOnly(new[] { new CampaignGroupedComponent("extra-gun", 1) }) } : value);
        var forgedGroup = new CampaignGroupedFormation(originalGroup.GroupId, originalGroup.SideId, originalGroup.RootId,
            originalGroup.LocationId, originalGroup.StackValue, originalGroup.MotorPool, forgedMembers, originalGroup.DispositionConfirmed);
        var forgedWorld = CampaignGroupedWorld.Create(grouped.Prior, grouped.ContentHash, [forgedGroup]);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceFourthIndianDefinition.Validate(forgedWorld));
        var fieldArtillery = grouped.Groups.Single().Members.Single(value => value.ElementId == "25-field-artillery");
        Assert.Equal("3-7-inch-howitzer", fieldArtillery.ComponentType);
        Assert.Equal(6, fieldArtillery.ComponentToe);
        Assert.Equal(grouped.Prior.Initialization.CapabilityPointsExpended, fieldArtillery.CapabilityPointsExpended);
        Assert.Equal(11, indian.SourceAuthority.Battlefield!.Entries.Count(value => value.LocationId == "d3615" && value.AuthorityStatus == "source-fourth-indian-proposed"));
        Assert.Null(grouped.StackAt("commonwealth", "d3615", _ => null));
        Assert.Contains(grouped.Prior.PendingArrivals, value => value.GroupId == "graziani-7-indian-brigade");
        var priorityAction = CampaignSourceAuthorityState.FourthIndianMotorActionId("central-india-horse", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Axis)).RejectionReason);
        Assert.False(grouped.Groups.Single().DispositionConfirmed);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        var confirm = CampaignExercises.Query(indian, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "fourth-indian-motorization");
        Assert.Contains("confirm", confirm.ChoiceJson, StringComparison.Ordinal);
        var ownerDraft = System.Text.Encoding.UTF8.GetString(CampaignExercises.QuerySourceFourthIndianDraft(indian, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.Contains("provisional-source-default-alternatives-unavailable", ownerDraft, StringComparison.Ordinal);
        Assert.Contains("5-indian-brigade-hq", ownerDraft, StringComparison.Ordinal);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(indian,
            Submission(indian, confirm.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var confirmCommand = Submission(indian, confirm.ActionId, CampaignActionAudience.Commonwealth);
        indian = CampaignExercises.Submit(indian, confirmCommand).SuccessorSession!;
        Assert.True(indian.SourceAuthority!.GroupedWorld!.Groups.Single().DispositionConfirmed);
        Assert.Equal(5, indian.SourceAuthority.GroupedWorld.StackAt("commonwealth", "d3615", _ => null));
        Assert.Equal(11, indian.SourceAuthority.Battlefield!.Entries.Count(value => value.LocationId == "d3615" && value.AuthorityStatus == "source-fourth-indian-grouped"));
        Assert.Equal(34, indian.SourceAuthority.ProfileVersion);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(indian, confirmCommand).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        indian = CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(21, indian.SourceAuthority!.GroupedWorld!.RemainingMotorPoints("4-indian-division"));
        var footAction = CampaignSourceAuthorityState.FourthIndianMotorActionId("1-royal-fusiliers", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, footAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        var minorBeforePromotions = CampaignExercises.ActivateSourceAxisMinorGarrisons(indian).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.AdmitSourceCompactCheckpoint(minorBeforePromotions).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.ActivateSourceAxisGarrisonRemainder(minorBeforePromotions).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.ActivateSourceGiarabubMotorMachineGun(minorBeforePromotions).SuccessorSession!;
        var minorIdentity = minorBeforePromotions.SourceAuthority!.World.ContentIdentity;
        minorBeforePromotions = CampaignExercises.SubmitSourceMalettiHierarchy(minorBeforePromotions,
            Submission(minorBeforePromotions, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        var minorMaletti = CampaignExercises.PromoteSourceCertifiedMalettiWorld(minorBeforePromotions);
        Assert.True(minorMaletti.IsAccepted);
        minorBeforePromotions = minorMaletti.SuccessorSession!;
        minorBeforePromotions = CampaignExercises.AdmitSourceMatruhFormation(minorBeforePromotions).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.SubmitSourceMatruhFormation(minorBeforePromotions,
            Submission(minorBeforePromotions, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.SubmitSourceMatruhFormation(minorBeforePromotions,
            Submission(minorBeforePromotions, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var minorMatruh = CampaignExercises.PromoteSourceMatruhDetachedWorld(minorBeforePromotions);
        Assert.True(minorMatruh.IsAccepted);
        minorBeforePromotions = minorMatruh.SuccessorSession!;
        Assert.Equal(minorIdentity, minorBeforePromotions.SourceAuthority!.World.ContentIdentity);
        Assert.All(ContentSourceAxisMinorGarrisonsPackArtifact.Units, unit =>
            Assert.Contains(minorBeforePromotions.SourceAuthority.World.World.Elements, element => element.ElementId == unit.Id));
        Assert.True(CampaignExercises.Reconstruct(minorBeforePromotions).IsVerified);
        var indianWithoutMalettiDetachment = indian;
        indian = CampaignExercises.SubmitSourceMalettiHierarchy(indian,
            Submission(indian, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        indian = CampaignExercises.PromoteSourceCertifiedMalettiWorld(indian).SuccessorSession!;
        Assert.Equal(21, indian.SourceAuthority!.GroupedWorld!.RemainingMotorPoints("4-indian-division"));
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        var matruhBeforeDetach = CampaignExercises.AdmitSourceMatruhFormation(indian).SuccessorSession!;
        matruhBeforeDetach = CampaignExercises.SubmitSourceMatruhFormation(matruhBeforeDetach,
            Submission(matruhBeforeDetach, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var matruhAfterDetach = matruhBeforeDetach;
        var groupedResult = CampaignExercises.AdmitSourceMatruhGroupedWorld(matruhBeforeDetach);
        Assert.True(groupedResult.IsAccepted);
        matruhBeforeDetach = groupedResult.SuccessorSession!;
        Assert.Equal(2, matruhBeforeDetach.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(0, matruhBeforeDetach.SourceAuthority.GroupedWorld.StackAt("commonwealth", "d3714", _ => null));
        Assert.Equal(6, matruhBeforeDetach.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "matruh-garrison").Members.Count);
        Assert.Equal(35, matruhBeforeDetach.SourceAuthority.ProfileVersion);
        var activeMaletti = CampaignExercises.SubmitSourceAxisPriority(matruhBeforeDetach,
            Submission(matruhBeforeDetach, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        activeMaletti = CampaignExercises.AdmitSourceMalettiGroupedWorld(activeMaletti).SuccessorSession!;
        Assert.Equal(3, activeMaletti.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(30, activeMaletti.SourceAuthority.GroupedWorld.RemainingMotorPoints("maletti-division"));
        Assert.Equal(5, activeMaletti.SourceAuthority.GroupedWorld.ExternalReservations.Single(value => value.GroupId == "maletti-division").AssignedMotorPoints);
        Assert.Equal(36, activeMaletti.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(activeMaletti).IsVerified);
        var libyanAdmission = CampaignExercises.AdmitSourceLibyanGroupedWorld(activeMaletti);
        Assert.True(libyanAdmission.IsAccepted);
        var libyanProposed = libyanAdmission.SuccessorSession!;
        Assert.Equal(5, libyanProposed.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(22, libyanProposed.SourceAuthority.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-libyan-proposed"));
        Assert.Equal(37, libyanProposed.SourceAuthority.ProfileVersion);
        var secondArtillery = libyanProposed.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "2-libyan-division")
            .Members.Single(value => value.ElementId == "2-libyan-artillery");
        Assert.Equal("75-27-gun", secondArtillery.ComponentType);
        Assert.Equal(9, secondArtillery.ComponentToe);
        Assert.Contains(secondArtillery.AdditionalComponents!, value => value.ComponentType == "100-17-howitzer" && value.Toe == 3);
        var axisChoices = CampaignExercises.Query(libyanProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "libyan-disposition").ToArray();
        Assert.Equal(2, axisChoices.Length);
        Assert.DoesNotContain(CampaignExercises.Query(libyanProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "libyan-disposition");
        var firstChoice = axisChoices.Single(value => value.ChoiceJson.Contains("1-libyan-division", StringComparison.Ordinal));
        var secondChoice = axisChoices.Single(value => value.ChoiceJson.Contains("2-libyan-division", StringComparison.Ordinal));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(libyanProposed,
            Submission(libyanProposed, firstChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var firstCommand = Submission(libyanProposed, firstChoice.ActionId, CampaignActionAudience.Axis);
        var firstConfirmed = CampaignExercises.Submit(libyanProposed, firstCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(firstConfirmed, firstCommand).RejectionReason);
        var secondAfter = CampaignExercises.Query(firstConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "libyan-disposition");
        firstConfirmed = CampaignExercises.Submit(firstConfirmed, Submission(firstConfirmed, secondAfter.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(22, firstConfirmed.SourceAuthority!.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-libyan-grouped"));
        Assert.Equal(5, firstConfirmed.SourceAuthority.GroupedWorld!.StackAt("axis", "c4020", _ => null));
        Assert.Equal(5, firstConfirmed.SourceAuthority.GroupedWorld.StackAt("axis", "c3920", _ => null));
        Assert.True(CampaignExercises.Reconstruct(firstConfirmed).IsVerified);
        var italianBasisFourth = ContentSourceFourthIndianPackArtifact.Create(firstConfirmed.SourceAuthority.CwFreeGunsWorld!.Content);
        var italianBasisMatruh = ContentSourceMatruhGroupedPackArtifact.Create(italianBasisFourth);
        var italianBasisMaletti = ContentSourceMalettiGroupedPackArtifact.Create(italianBasisMatruh);
        var italianBasisLibyan = ContentSourceLibyanGroupedPackArtifact.Create(italianBasisMaletti);
        var italianContent = ContentSourceCireneMarmaricaGroupedPackArtifact.Create(italianBasisLibyan);
        Assert.Equal(ContentSourceCireneMarmaricaGroupedPackArtifact.ContentHash, italianContent.Hash());
        var italianAdmission = CampaignExercises.AdmitSourceItalianDivisionsGroupedWorld(firstConfirmed);
        Assert.True(italianAdmission.IsAccepted);
        var italianProposed = italianAdmission.SuccessorSession!;
        Assert.Equal(7, italianProposed.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(29, italianProposed.SourceAuthority.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-italian-division-proposed"));
        Assert.Equal(39, italianProposed.SourceAuthority.ProfileVersion);
        var italianChoices = CampaignExercises.Query(italianProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "italian-division-disposition").ToArray();
        Assert.Equal(2, italianChoices.Length);
        Assert.DoesNotContain(CampaignExercises.Query(italianProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "italian-division-disposition");
        var cireneChoice = italianChoices.Single(value => value.ChoiceJson.Contains("cirene-division", StringComparison.Ordinal));
        var italianCommand = Submission(italianProposed, cireneChoice.ActionId, CampaignActionAudience.Axis);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(italianProposed,
            Submission(italianProposed, cireneChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var italianConfirmed = CampaignExercises.Submit(italianProposed, italianCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(italianConfirmed, italianCommand).RejectionReason);
        var marmaricaChoice = CampaignExercises.Query(italianConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "italian-division-disposition");
        italianConfirmed = CampaignExercises.Submit(italianConfirmed, Submission(italianConfirmed, marmaricaChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(29, italianConfirmed.SourceAuthority!.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-italian-division-grouped"));
        Assert.Equal(5, italianConfirmed.SourceAuthority.GroupedWorld!.StackAt("axis", "c4120", _ => null));
        Assert.Equal(5, italianConfirmed.SourceAuthority.GroupedWorld.StackAt("axis", "c3918", _ => null));
        Assert.True(CampaignExercises.Reconstruct(italianConfirmed).IsVerified);
        var seventhDirect = italianConfirmed.SourceAuthority.AdmitSeventhArmoredGroupedWorld();
        Assert.NotNull(seventhDirect.GroupedWorld);
        var seventhResult = CampaignExercises.AdmitSourceSeventhArmoredGroupedWorld(italianConfirmed);
        Assert.True(seventhResult.IsAccepted);
        var seventhProposed = seventhResult.SuccessorSession!;
        var seventh = seventhProposed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division");
        Assert.Equal(7, seventh.Members.Count); Assert.Null(seventh.StackValue); Assert.False(seventh.DispositionConfirmed);
        Assert.Equal(10, seventh.MotorPool);
        Assert.Equal(40, seventhProposed.SourceAuthority!.ProfileVersion);
        var seventhChoice = CampaignExercises.Query(seventhProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "seventh-armored-disposition");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(seventhProposed,
            Submission(seventhProposed, seventhChoice.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var seventhCommand = Submission(seventhProposed, seventhChoice.ActionId, CampaignActionAudience.Commonwealth);
        var seventhConfirmedResult = CampaignExercises.Submit(seventhProposed, seventhCommand);
        Assert.True(seventhConfirmedResult.IsAccepted);
        var seventhConfirmed = seventhConfirmedResult.SuccessorSession!;
        Assert.Equal(5, seventhConfirmed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division").StackValue);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(seventhConfirmed, seventhCommand).RejectionReason);
        Assert.Equal(italianConfirmed.SourceAuthority.World.PendingArrivals, seventhConfirmed.SourceAuthority.World.PendingArrivals);
        Assert.True(CampaignExercises.Reconstruct(seventhConfirmed).IsVerified);
        var legacySeventhCheckpoint = seventhConfirmed.SourceAuthority.SerializeCheckpoint();
        var correctedSeventh = CampaignExercises.CorrectSourceSeventhShell(seventhConfirmed).SuccessorSession!;
        Assert.Equal(3, correctedSeventh.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division").StackValue);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedLegacyCheckpointContractVersion, correctedSeventh.SourceAuthority.ProfileVersion);
        Assert.Equal(legacySeventhCheckpoint, seventhConfirmed.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(seventhConfirmed.SourceAuthority.RandomState, correctedSeventh.SourceAuthority.RandomState);
        Assert.Contains("common-charts-9.4-division-shell-stacks-three", System.Text.Encoding.UTF8.GetString(
            CampaignExercises.QuerySourceSeventhArmoredDraft(correctedSeventh, CampaignActionAudience.Commonwealth).VisibleBytes));
        Assert.True(CampaignExercises.Reconstruct(correctedSeventh).IsVerified);
        Assert.False(CampaignExercises.CorrectSourceSeventhShell(correctedSeventh).IsAccepted);
        var compactBeforeShell = CampaignExercises.AdmitSourceCompactCheckpoint(seventhConfirmed).SuccessorSession!;
        var compactAxisFinish = CampaignExercises.Query(compactBeforeShell, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var compactAxisFinished = CampaignExercises.Submit(compactBeforeShell,
            Submission(compactBeforeShell, compactAxisFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var compactCwFinish = CampaignExercises.Query(compactAxisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var compactBothFinished = CampaignExercises.Submit(compactAxisFinished,
            Submission(compactAxisFinished, compactCwFinish.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var compactCorrected = CampaignExercises.CorrectSourceSeventhShell(compactBothFinished).SuccessorSession!;
        Assert.Equal(1, compactCorrected.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedFinalizedCompactCheckpointContractVersion, compactCorrected.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(compactCorrected).IsVerified);
        var compactAxisReopen = CampaignExercises.Query(compactCorrected, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var compactReopened = CampaignExercises.Submit(compactCorrected,
            Submission(compactCorrected, compactAxisReopen.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(0, compactReopened.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedFinalizedCompactCheckpointContractVersion, compactReopened.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(compactReopened).IsVerified);
        var sixteenthBasis = ContentSourceSeventhArmoredGroupedPackArtifact.Create(italianContent);
        var sixteenthContent = ContentSourceSixteenthBrigadeGroupedPackArtifact.Create(sixteenthBasis);
        Assert.Equal(ContentSourceSixteenthBrigadeGroupedPackArtifact.ContentHash, sixteenthContent.Hash());
        var sixteenthResult = CampaignExercises.AdmitSourceSixteenthBrigadeGroupedWorld(seventhConfirmed);
        Assert.True(sixteenthResult.IsAccepted);
        var sixteenthProposed = sixteenthResult.SuccessorSession!;
        var sourceWorld = sixteenthProposed.SourceAuthority!.GroupedWorld!;
        var sixteenth = sourceWorld.Groups.Single(value => value.GroupId == "16-infantry-brigade");
        Assert.Equal(4, sixteenth.Members.Count); Assert.Null(sixteenth.StackValue); Assert.True(sixteenth.UsesRetainedMembers);
        Assert.Equal(4, sourceWorld.Prior.World.Elements.Count(value => sixteenth.Members.Any(member => member.ElementId == value.ElementId)));
        Assert.Single(sourceWorld.OccupantsAt("e1829"), value => value.Formation?.GroupId == sixteenth.GroupId);
        var sixteenthChoice = CampaignExercises.Query(sixteenthProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "sixteenth-brigade-disposition");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(sixteenthProposed,
            Submission(sixteenthProposed, sixteenthChoice.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var sixteenthCommand = Submission(sixteenthProposed, sixteenthChoice.ActionId, CampaignActionAudience.Commonwealth);
        var sixteenthConfirmed = CampaignExercises.Submit(sixteenthProposed, sixteenthCommand).SuccessorSession!;
        Assert.Equal(2, sixteenthConfirmed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == sixteenth.GroupId).StackValue);
        Assert.Equal(41, sixteenthConfirmed.SourceAuthority.ProfileVersion);
        var playableWorld = CampaignSourcePlayableWorld.Create(sixteenthConfirmed.SourceAuthority);
        var mapBoundWorld = CampaignSourcePlayableWorld.Create(sixteenthConfirmed.SourceAuthority, mapIdentityBound: true);
        var cdWorld = CampaignSourcePlayableWorld.Create(sixteenthConfirmed.SourceAuthority, mapIdentityBound: true, cdSeamBound: true);
        Assert.True(mapBoundWorld.MapIdentityBound);
        Assert.True(mapBoundWorld.WithWorld(mapBoundWorld.GroupedWorld, mapBoundWorld.Battlefield).MapIdentityBound);
        Assert.False(playableWorld.MapIdentityBound);
        // Test the wire contract on the retained historical world without
        // claiming that the still-gated campaign can start yet.
        var turnConstructor = typeof(CampaignSourceTurnAuthority).GetConstructors(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Single();
        CampaignSourceTurnAuthority ContractTurn(CampaignSourcePlayableWorld world, LandSequencePosition? testPosition = null,
            int? organizationMask = null, CampaignSourceTrainingLedger? training = null,
            CampaignSourceGt1AxisArrival? axisArrival = null, LandSide? firstSide = null,
            (int GameTurn, int OperationStage)? paidStage = null)
        {
            var args = turnConstructor.GetParameters().Select(parameter => parameter.Name == "position" && testPosition is not null
                ? testPosition : parameter.Name == "voluntaryOrganizationDeclinedMask" && organizationMask is not null
                ? organizationMask : parameter.Name == "training" && training is not null ? training
                : parameter.Name == "axisArrival" && axisArrival is not null ? axisArrival
                : parameter.Name == "fourthIndianPaidStage" && paidStage is not null ? paidStage
                : parameter.Name == "stageOneFirstSide" && firstSide is not null ? firstSide : parameter.HasDefaultValue
                ? parameter.DefaultValue : parameter.Name switch
                {
                    "world" => world,
                    "stateVersion" => world.Source.StateVersion + 1,
                    "position" => Cna1979LandSequenceV4.CreateTurn(1)[0],
                    "randomState" => world.RandomState,
                    "initiative" => null,
                    _ => throw new InvalidOperationException("Unexpected source-turn constructor parameter."),
                }).ToArray();
            return (CampaignSourceTurnAuthority)turnConstructor.Invoke(args);
        }
        var mapTurn = ContractTurn(mapBoundWorld);
        var cdTurn = ContractTurn(cdWorld);
        Assert.Equal(3, new ExerciseCheckpoint(cdTurn).ContractVersion);
        Assert.True(cdWorld.WithWorld(cdWorld.GroupedWorld, cdWorld.Battlefield).CdSeamBound);
        using (var cdCheckpoint = JsonDocument.Parse(cdTurn.SerializeCheckpoint()))
        {
            Assert.Equal(3, cdCheckpoint.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(Cna1979HistoricalMap.CdSeamPolicyHash,
                cdCheckpoint.RootElement.GetProperty("historicalMap").GetProperty("cdSeamPolicyHash").GetString());
        }
        var legacyTurn = ContractTurn(playableWorld);
        var turnContacts = sixteenthConfirmed.CurrentContacts.AdvancePregameSource(mapTurn.Source.StateVersion);
        var turnSession = new ExerciseSession(mapTurn.Source.Pregame, turnContacts,
            sixteenthConfirmed.PregameHistory, mapTurn.Source, mapTurn);
        var initiativeResult = CampaignExercises.ResolveSourceInitiative(turnSession);
        Assert.True(initiativeResult.IsAccepted);
        Assert.Equal(initiativeResult.SuccessorSession!.SourceTurn!.Position.PositionId,
            initiativeResult.Evidence!.Receipt.ResultingPositionId);
        Assert.Equal(CampaignSourceTurnAuthority.InitiativeActionId, initiativeResult.Evidence.Receipt.ActionId);
        var organizationCut = Cna1979LandSequenceV4.CreateTurn(1).Single(value =>
            value.OperationStage == 1 && value.PhaseId == LandPhaseIds.Organization);
        var interrupted = CampaignSourceTrainingLedger.CreateSixthAustralian().Interrupt();
        var waitingOrganization = ContractTurn(mapBoundWorld, organizationCut, 1, interrupted);
        Assert.Throws<InvalidOperationException>(() => waitingOrganization.CompleteGt1Organization());
        Assert.Throws<InvalidOperationException>(() => ContractTurn(mapBoundWorld, organizationCut, 3).CompleteGt1Organization());
        var readyOrganization = ContractTurn(mapBoundWorld, organizationCut, 3, interrupted);
        var completedOrganization = readyOrganization.CompleteGt1Organization();
        Assert.Equal(Cna1979LandSequenceV4.GetNext(organizationCut), completedOrganization.Position);
        Assert.Same(readyOrganization.Training, completedOrganization.Training);
        Assert.Equal(readyOrganization.RandomState, completedOrganization.RandomState);
        Assert.Same(readyOrganization.World, completedOrganization.World);
        Assert.Equal(readyOrganization.StateVersion + 1, completedOrganization.StateVersion);
        Assert.Throws<InvalidOperationException>(() => completedOrganization.CompleteGt1Organization());
        // Exercise the real source session transition on retained historical
        // setup without claiming that the campaign-start gate has opened.
        var organizationContacts = sixteenthConfirmed.CurrentContacts.AdvancePregameSource(readyOrganization.Source.StateVersion);
        var organizationSession = new ExerciseSession(readyOrganization.Source.Pregame, organizationContacts,
            sixteenthConfirmed.PregameHistory, readyOrganization.Source, readyOrganization);
        var organizationResult = CampaignExercises.CompleteSourceGt1Organization(organizationSession);
        Assert.True(organizationResult.IsAccepted);
        Assert.Equal(completedOrganization.Position.PositionId, organizationResult.Evidence!.Receipt.ResultingPositionId);
        Assert.True(CampaignExercises.IsSourceGt1OrganizationCompleted(organizationResult.SuccessorSession!));
        Assert.Equal(completedOrganization.Position, organizationResult.SuccessorSession!.SourceTurn!.Position);
        var organizationCommand = CampaignHostAdapter.SourceTurnSystemCommand(organizationSession, organizationResult);
        var organizationStep = new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(organizationCommand), organizationResult.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(organizationResult.Evidence.Receipt),
            organizationResult.Evidence.SnapshotCheckpoint, CampaignHostAcceptedStep.SourceGt1OrganizationCompleteKind);
        Assert.Equal(CampaignHostAcceptedStep.SourceGt1OrganizationCompleteContractVersion, organizationStep.ContractVersion);
        using (var organizationEvent = JsonDocument.Parse(Assert.Single(organizationResult.Evidence!.EventRecords)))
        {
            Assert.Equal("source-gt1-organization-completed", organizationEvent.RootElement.GetProperty("eventType").GetString());
            Assert.Equal(CampaignSourceTurnAuthority.CompleteGt1OrganizationActionId, organizationEvent.RootElement.GetProperty("actionId").GetString());
        }
        Assert.Equal(2, new ExerciseCheckpoint(mapTurn).ContractVersion);
        Assert.Equal(1, new ExerciseCheckpoint(legacyTurn).ContractVersion);
        using (var boundCheckpoint = System.Text.Json.JsonDocument.Parse(mapTurn.SerializeCheckpoint()))
        using (var legacyCheckpoint = System.Text.Json.JsonDocument.Parse(legacyTurn.SerializeCheckpoint()))
        {
            Assert.Equal(2, boundCheckpoint.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(Cna1979HistoricalMap.HexHash, boundCheckpoint.RootElement.GetProperty("historicalMap").GetProperty("hexSha256").GetString());
            Assert.Equal(Cna1979HistoricalMap.EdgeHash, boundCheckpoint.RootElement.GetProperty("historicalMap").GetProperty("hexsideSha256").GetString());
            Assert.Equal(1, legacyCheckpoint.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.False(legacyCheckpoint.RootElement.TryGetProperty("historicalMap", out _));
        }
        using (var boundEvent = System.Text.Json.JsonDocument.Parse(mapTurn.SerializeEvent("source-turn-started",
                   CampaignSourceTurnAuthority.StartActionId, mapBoundWorld.Source.StateVersion,
                   mapBoundWorld.Source.SerializeCheckpoint())))
        {
            Assert.Equal(2, boundEvent.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(CampaignPregameSourceCreation.Digest(mapTurn.SerializeCheckpoint()),
                boundEvent.RootElement.GetProperty("checkpointHash").GetString());
        }
        using (var legacyEvent = System.Text.Json.JsonDocument.Parse(legacyTurn.SerializeEvent("source-turn-started",
                   CampaignSourceTurnAuthority.StartActionId, playableWorld.Source.StateVersion,
                   playableWorld.Source.SerializeCheckpoint())))
            Assert.Equal(1, legacyEvent.RootElement.GetProperty("contractVersion").GetInt32());
        Assert.True(mapTurn.ResolveInitiative().World.MapIdentityBound);
        Assert.Same(sixteenthConfirmed.SourceAuthority.GroupedWorld, playableWorld.GroupedWorld);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.RandomState, playableWorld.RandomState);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.World.PendingArrivals, playableWorld.World8.PendingArrivals);
        foreach (var entry in playableWorld.Battlefield.Entries.Where(value => value.LocationId is { Length: 5 }
                     && value.LocationId[0] is >= 'a' and <= 'e' && value.LocationId.Skip(1).All(char.IsAsciiDigit)))
            Assert.NotNull(Cna1979HistoricalMap.Instance.HexAt(entry.LocationId!));
        foreach (var location in Cna1979GrazianiSourceFacts.CairoEntryArea.LocationIds)
            Assert.Equal(Cna1979SourceContentVocabulary.MajorCityTerrainId, playableWorld.KnownTerrainAt(location));
        var polishArrivalPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        Assert.Contains("e1829", CampaignSourcePolishArrival.ValidDestinations(playableWorld, polishArrivalPosition));
        Assert.Null(playableWorld.KnownTerrainAt("e1932"));
        Assert.Single(playableWorld.OccupantsAt("e1829"), value => value.Formation?.GroupId == sixteenth.GroupId);
        Assert.DoesNotContain(playableWorld.UnresolvedAt("e1829"), value => sixteenth.Members.Any(member => member.ElementId == value.ElementId));
        Assert.False(playableWorld.IsGameplayReady);
        Assert.Contains("battlefield-incomplete", playableWorld.ReadinessBlockers());
        Assert.Contains("control-incomplete", playableWorld.ReadinessBlockers());
        Assert.Contains("map-incomplete", playableWorld.ReadinessBlockers());
        Assert.Same(playableWorld.GroupedWorld, playableWorld.WithWorld(playableWorld.GroupedWorld, playableWorld.Battlefield).GroupedWorld);
        var sourceSupply = playableWorld.Source.CommonwealthSupplyWorld;
        CampaignSourceFourthIndianRoad.RequirePinnedSource(playableWorld);
        Assert.Equal(new CapabilityPointAmount(1, 1), CampaignSourceFourthIndianRoad.Cost(false));
        Assert.Equal(new CapabilityPointAmount(1, 2), CampaignSourceFourthIndianRoad.Cost(true));
        Assert.Equal(new CapabilityPointAmount(1, 1), CampaignSourceFourthIndianRoad.SupplyTraceCost);
        var paidSupply = sourceSupply.SpendFuel("cw-matruh-1", "d3714", 2);
        Assert.Equal(60, Assert.Single(sourceSupply.Units, value => value.Id == "cw-matruh-1").Fuel);
        Assert.Equal(58, Assert.Single(paidSupply.Units, value => value.Id == "cw-matruh-1").Fuel);
        Assert.Throws<InvalidOperationException>(() => sourceSupply.SpendFuel("cw-matruh-1", "d3615", 2));
        Assert.Throws<InvalidOperationException>(() => sourceSupply.SpendFuel("cw-matruh-1", "d3714", 61));
        Assert.Throws<InvalidOperationException>(() => sourceSupply.SpendFuel("not-a-counter", "d3714", 2));
        var movementCatalog = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.ActorRole == LandActorRole.FirstActingSide && value.SegmentId == LandSegmentIds.Movement);
        var movementPosition = new LandSequencePosition(movementCatalog.ContractVersion, movementCatalog.PositionId,
            movementCatalog.GameTurn, movementCatalog.OperationStage, movementCatalog.StageId, movementCatalog.PhaseId,
            movementCatalog.SegmentId, movementCatalog.StepId, movementCatalog.ActorRole, LandSide.Commonwealth, movementCatalog.Sources);
        var season = Cna1979Weather.GetSeason(1);
        var normalRoll = Enumerable.Range(11, 56).First(value => value % 10 is >= 1 and <= 6
            && value / 10 is >= 1 and <= 6 && Cna1979Weather.GetKind(season, value) == WeatherKind.Normal);
        var normalWeather = new CampaignOperationStageWeather(1, 1, 1, LandSide.Axis, season,
            normalRoll / 10, normalRoll % 10, WeatherKind.Normal, WeatherScope.None, null, [], 0, 0, 0);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceFourthIndianMovement.Apply(playableWorld,
            sourceSupply, null, movementPosition, normalWeather, "cw-matruh-1"));
        Assert.Equal(60, Assert.Single(sourceSupply.Units, value => value.Id == "cw-matruh-1").Fuel);
        // Exercise the source-bound mutation and contact projection directly;
        // source readiness still prevents this from becoming a playable move.
        var fourth = Assert.Single(playableWorld.GroupedWorld.Groups,
            value => value.GroupId == CampaignSourceFourthIndianMovement.GroupId);
        var fourthCosts = fourth.Members.ToDictionary(value => value.ElementId,
            value => new CapabilityPointAmount(1, 1), StringComparer.Ordinal);
        var movedFourth = playableWorld.GroupedWorld.MoveFormation(fourth.GroupId, "d3715", fourthCosts, 5, 1, 1);
        var movedBattlefield = playableWorld.Battlefield.MoveFormation(playableWorld.GroupedWorld, movedFourth, fourth.GroupId);
        var movedWorld = playableWorld.WithWorld(movedFourth, movedBattlefield);
        Assert.True(mapBoundWorld.WithWorld(movedFourth, movedBattlefield).MapIdentityBound);
        var unknownMove = playableWorld.GroupedWorld.MoveFormation(fourth.GroupId, "c9999", fourthCosts, 5, 1, 1);
        var unknownBattlefield = playableWorld.Battlefield.MoveFormation(playableWorld.GroupedWorld, unknownMove, fourth.GroupId);
        Assert.Contains(unknownBattlefield.Entries, entry => entry.LocationId == "c9999");
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePlayableWorld.RequireKnownMapLocations(
            unknownBattlefield.Entries.Select(entry => entry.LocationId)));
        Assert.Throws<InvalidOperationException>(() => mapBoundWorld.WithWorld(unknownMove, unknownBattlefield));
        Assert.All(fourth.Members, member => Assert.Equal("d3715",
            Assert.Single(movedWorld.Battlefield.Entries, entry => entry.ElementId == member.ElementId).LocationId));
        Assert.All(fourth.Members, member => Assert.Equal("d3615",
            Assert.Single(playableWorld.Battlefield.Entries, entry => entry.ElementId == member.ElementId).LocationId));
        var polishPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePolishArrival.Apply(playableWorld,
            polishPosition, LandSide.Commonwealth, "e1730"));
        Assert.Equal(sixteenthConfirmed.SourceAuthority.World.PendingArrivals, playableWorld.World8.PendingArrivals);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.RandomState, playableWorld.RandomState);
        var polishGrouped = CampaignSourcePolishArrival.Materialize(playableWorld.GroupedWorld, playableWorld.Source.Content, "e1829");
        var polishBattlefield = playableWorld.Battlefield.ArriveRetainedFormation(playableWorld.GroupedWorld,
            polishGrouped, CampaignSourcePolishArrival.GroupId, "e1829");
        var polishSuccessor = playableWorld.WithWorld(polishGrouped, polishBattlefield);
        var mapBoundPolishSuccessor = mapBoundWorld.WithWorld(polishGrouped, polishBattlefield);
        Assert.True(mapBoundPolishSuccessor.MapIdentityBound);
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(mapBoundWorld, mapBoundPolishSuccessor,
            mapBoundWorld.Source.StateVersion + 1, new CampaignContactKnowledge(mapBoundWorld.Source.Request.CampaignId, LandSide.Axis)));
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(mapBoundWorld, mapBoundPolishSuccessor,
            mapBoundWorld.Source.StateVersion + 1, new CampaignContactKnowledge(mapBoundWorld.Source.Request.CampaignId, LandSide.Commonwealth)));
        Assert.DoesNotContain(polishSuccessor.World8.PendingArrivals, value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.DoesNotContain(polishSuccessor.Battlefield.Pending, value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.Contains(polishSuccessor.World8.PendingArrivals, value => value.GroupId == Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId);
        Assert.Equal(8, polishSuccessor.World8.World.Elements.Count(value => Cna1979GrazianiSourceFacts.PolishBrigadeArrival.ElementIds.Contains(value.ElementId)));
        Assert.Equal(playableWorld.RandomState, polishSuccessor.RandomState);
        Assert.False(polishSuccessor.IsGameplayReady);
        Assert.Contains(playableWorld.World8.PendingArrivals, value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(playableWorld, polishSuccessor,
            playableWorld.Source.StateVersion + 1, new CampaignContactKnowledge(playableWorld.Source.Request.CampaignId, LandSide.Axis)));
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(playableWorld, polishSuccessor,
            playableWorld.Source.StateVersion + 1, new CampaignContactKnowledge(playableWorld.Source.Request.CampaignId, LandSide.Commonwealth)));
        var begunEarlyPlan = CampaignExercises.SubmitSourceSeptemberSupply(sixteenthConfirmed,
            Submission(sixteenthConfirmed, CampaignSourceAuthorityState.SeptemberSupplyActionId("begin", 0, 0, 0, 0), CampaignActionAudience.Axis)).SuccessorSession!;
        var rolledEarlyPlan = CampaignExercises.SubmitSourceSeptemberSupply(begunEarlyPlan,
            Submission(begunEarlyPlan, CampaignSourceAuthorityState.SeptemberSupplyActionId("roll", 3, 0, 0, 0), CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Contains("axis-prestart-supply-plan-incomplete", CampaignSourcePlayableWorld.Create(rolledEarlyPlan.SourceAuthority!).ReadinessBlockers());
        var finishedEarlyPlan = CampaignExercises.SubmitSourceSeptemberSupply(rolledEarlyPlan,
            Submission(rolledEarlyPlan, CampaignSourceAuthorityState.SeptemberSupplyActionId("finish", 3, 0, 0, 0), CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.DoesNotContain("axis-prestart-supply-plan-incomplete", CampaignSourcePlayableWorld.Create(finishedEarlyPlan.SourceAuthority!).ReadinessBlockers());
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(sixteenthConfirmed, sixteenthCommand).RejectionReason);
        Assert.Equal(seventhConfirmed.SourceAuthority.World.PendingArrivals, sixteenthConfirmed.SourceAuthority.World.PendingArrivals);
        Assert.True(CampaignExercises.Reconstruct(sixteenthConfirmed).IsVerified);
        var sixteenthHistory = sixteenthConfirmed.PregameHistory.Select(value => value.ToArray()).ToArray();
        var sixteenthEvent = System.Text.Encoding.UTF8.GetString(sixteenthHistory[^1]);
        using (var eventDocument = System.Text.Json.JsonDocument.Parse(sixteenthHistory[^1]))
        {
            var worldHash = eventDocument.RootElement.GetProperty("worldHash").GetString()!;
            var alteredHash = worldHash[..^1] + (worldHash[^1] == '0' ? '1' : '0');
            sixteenthHistory[^1] = System.Text.Encoding.UTF8.GetBytes(sixteenthEvent.Replace(worldHash, alteredHash, StringComparison.Ordinal));
        }
        var tamperedSixteenth = new ExerciseSession(sixteenthConfirmed.Pregame!, sixteenthConfirmed.CurrentContacts,
            sixteenthHistory, sixteenthConfirmed.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(tamperedSixteenth).IsVerified);
        var catanzaroResult = CampaignExercises.AdmitSourceCatanzaroGroupedWorld(sixteenthConfirmed);
        Assert.True(catanzaroResult.IsAccepted);
        var catanzaroProposed = catanzaroResult.SuccessorSession!;
        var catanzaro = catanzaroProposed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "catanzaro-division");
        Assert.Equal(13, catanzaro.Members.Count);
        Assert.Null(catanzaro.StackValue);
        Assert.Equal(35, catanzaro.MotorPool);
        var catanzaroChoice = CampaignExercises.Query(catanzaroProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "catanzaro-disposition");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(catanzaroProposed,
            Submission(catanzaroProposed, catanzaroChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var catanzaroCommand = Submission(catanzaroProposed, catanzaroChoice.ActionId, CampaignActionAudience.Axis);
        var catanzaroConfirmed = CampaignExercises.Submit(catanzaroProposed, catanzaroCommand).SuccessorSession!;
        Assert.Equal(42, catanzaroConfirmed.SourceAuthority!.ProfileVersion);
        var axisFinish = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var axisFinished = CampaignExercises.Submit(catanzaroConfirmed,
            Submission(catanzaroConfirmed, axisFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwFinishAfterAxis = CampaignExercises.Query(axisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var bothFinishedBeforeCorrection = CampaignExercises.Submit(axisFinished,
            Submission(axisFinished, cwFinishAfterAxis.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(3, bothFinishedBeforeCorrection.SourceAuthority!.FinalizedOwners);
        var correctedFinalized = CampaignExercises.CorrectSourceSeventhShell(bothFinishedBeforeCorrection).SuccessorSession!;
        Assert.Equal(1, correctedFinalized.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedFinalizedLegacyCheckpointContractVersion, correctedFinalized.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(correctedFinalized).IsVerified);
        var choicesBefore = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "sixth-australian-placement")
            .Select(value => value.ChoiceJson).Order(StringComparer.Ordinal).ToArray();
        var choicesAfter = CampaignExercises.Query(axisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "sixth-australian-placement")
            .Select(value => value.ChoiceJson).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(choicesBefore, choicesAfter);
        var australianChoice = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "sixth-australian-placement"
                && value.SourceActionId == CampaignSourceAuthorityState.SixthAustralianActionId("e1829"));
        Assert.DoesNotContain(CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "sixth-australian-placement"
                && value.SourceActionId == CampaignSourceAuthorityState.SixthAustralianActionId(chosenCairoHex));
        var australianSession = CampaignExercises.Submit(catanzaroConfirmed,
            Submission(catanzaroConfirmed, australianChoice.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var australian = australianSession.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "6th-australian-division");
        Assert.Equal(10, australian.Members.Count);
        Assert.Equal(5, australian.StackValue);
        Assert.False(australian.MotorPoolKnown);
        var australianInfantry = australian.Members.Where(value => value.ComponentType == "infantry").ToArray();
        Assert.Equal(6, australianInfantry.Length);
        Assert.All(australianInfantry, value => { Assert.Equal(6, value.ComponentToe); Assert.Equal(10, value.CapabilityPointAllowance); });
        foreach (var id in new[] { "16-australian-infantry-brigade-hq", "17-australian-infantry-brigade-hq" })
        {
            var headquarters = australian.Members.Single(value => value.ElementId == id);
            Assert.Equal(1, headquarters.ComponentToe); Assert.Equal(20, headquarters.CapabilityPointAllowance);
        }
        var cavalry = australian.Members.Single(value => value.ElementId == "6-australian-divisional-cavalry");
        Assert.Equal(8, cavalry.ComponentToe); Assert.Equal(45, cavalry.CapabilityPointAllowance);
        Assert.Equal(catanzaroConfirmed.SourceAuthority!.RandomState, australianSession.SourceAuthority.RandomState);
        Assert.Equal(45, australianSession.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(australianSession).IsVerified);
        var axisBatchChoice = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "ccnn-batch-disposition");
        var afterAustralianAxisChoice = CampaignExercises.Query(australianSession, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "ccnn-batch-disposition");
        Assert.Equal(axisBatchChoice.ChoiceJson, afterAustralianAxisChoice.ChoiceJson);
        var australianFirst = CampaignExercises.Submit(australianSession,
            Submission(australianSession, afterAustralianAxisChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var fullyMotorized = australianFirst;
        for (var index = 0; index < 5; index++)
        {
            var adjustment = CampaignExercises.SubmitSourceFourthIndian(fullyMotorized,
                Submission(fullyMotorized, priorityAction, CampaignActionAudience.Commonwealth));
            Assert.True(adjustment.IsAccepted);
            fullyMotorized = adjustment.SuccessorSession!;
        }
        var motorizedWorld = CampaignSourcePlayableWorld.Create(fullyMotorized.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true);
        Assert.Contains("seventh-armored-shell-stack-correction-required", motorizedWorld.ReadinessBlockers());
        Assert.Contains("owner-setup-finalization-incomplete", motorizedWorld.ReadinessBlockers());
        Assert.Contains("central-india-horse-training-state-unresolved", motorizedWorld.ReadinessBlockers());
        var axisHorseApproved = Act(fullyMotorized, CampaignActionAudience.Axis, "central-india-horse-ruling", "approve");
        var cwHorseApproved = Act(axisHorseApproved, CampaignActionAudience.Commonwealth, "central-india-horse-ruling", "approve");
        Assert.Equal(3, cwHorseApproved.SourceAuthority!.HorseRulingVotes);
        var horseStageOneDeclined = Act(cwHorseApproved, CampaignActionAudience.Commonwealth, "central-india-horse-stage-one", "decline-stage1");
        Assert.True(horseStageOneDeclined.SourceAuthority!.HorseStageOneResolved);
        var shellCorrected = CampaignExercises.CorrectSourceSeventhShell(horseStageOneDeclined).SuccessorSession!;
        Assert.True(shellCorrected.SourceAuthority!.IsSeventhShellCorrected);
        var axisFinalizedForReadiness = Act(shellCorrected, CampaignActionAudience.Axis, "owner-finalization", "finish");
        var bothFinalizedForReadiness = Act(axisFinalizedForReadiness, CampaignActionAudience.Commonwealth, "owner-finalization", "finish");
        Assert.Equal(3, bothFinalizedForReadiness.SourceAuthority!.FinalizedOwners);
        var readyWorld = CampaignSourcePlayableWorld.Create(bothFinalizedForReadiness.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true);
        Assert.DoesNotContain("seventh-armored-shell-stack-correction-required", readyWorld.ReadinessBlockers());
        Assert.DoesNotContain("owner-setup-finalization-incomplete", readyWorld.ReadinessBlockers());
        Assert.DoesNotContain("central-india-horse-training-state-unresolved", readyWorld.ReadinessBlockers());
        var openingGroup = motorizedWorld.GroupedWorld.Groups.Single(value => value.GroupId == CampaignSourceFourthIndianMovement.GroupId);
        Assert.True(CampaignSourceFourthIndianMovement.IsOpeningFormation(openingGroup));
        var openingMoved = motorizedWorld.GroupedWorld.MoveFormation(openingGroup.GroupId, CampaignSourceFourthIndianRoad.Destination,
            CampaignSourceFourthIndianMovement.OpeningCosts(openingGroup), CampaignSourceFourthIndianRoad.RoadStackLimit, 1, 1);
        var openingBattlefield = motorizedWorld.Battlefield.MoveFormation(motorizedWorld.GroupedWorld, openingMoved, openingGroup.GroupId);
        var openingWorld = motorizedWorld.WithWorld(openingMoved, openingBattlefield);
        var breakdownPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.ActorRole == LandActorRole.FirstActingSide && value.SegmentId == LandSegmentIds.BreakdownDetermination);
        var movedBreakdown = ContractTurn(openingWorld, breakdownPosition, 3, interrupted,
            firstSide: LandSide.Commonwealth, paidStage: (1, 1));
        var afterMovedBreakdown = movedBreakdown.CompleteGt1FirstBreakdown();
        Assert.Equal(LandStepIds.PositionDetermination, afterMovedBreakdown.Position.StepId);
        Assert.Same(openingWorld, afterMovedBreakdown.World);
        var unexpectedMove = motorizedWorld.GroupedWorld.MoveFormation(openingGroup.GroupId, "d3716",
            CampaignSourceFourthIndianMovement.OpeningCosts(openingGroup), CampaignSourceFourthIndianRoad.RoadStackLimit, 1, 1);
        var unexpectedBattlefield = motorizedWorld.Battlefield.MoveFormation(motorizedWorld.GroupedWorld, unexpectedMove, openingGroup.GroupId);
        var unexpectedWorld = motorizedWorld.WithWorld(unexpectedMove, unexpectedBattlefield);
        Assert.Throws<InvalidOperationException>(() => ContractTurn(unexpectedWorld, breakdownPosition, 3, interrupted,
            firstSide: LandSide.Commonwealth, paidStage: (1, 1)).CompleteGt1FirstBreakdown());
        // Exercise the Organization choices against the retained real
        // Australian formation; turn start itself remains readiness-gated.
        var australianWorld = CampaignSourcePlayableWorld.Create(australianFirst.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true);
        var australianTurn = ContractTurn(australianWorld, organizationCut);
        var australianContacts = australianFirst.CurrentContacts.AdvancePregameSource(australianTurn.Source.StateVersion);
        var australianOrganization = new ExerciseSession(australianTurn.Source.Pregame, australianContacts,
            australianFirst.PregameHistory, australianTurn.Source, australianTurn);
        var beforeInterrupt = CampaignExercises.Query(australianOrganization, CampaignActionAudience.Commonwealth).ActionSet!.Candidates;
        Assert.DoesNotContain(beforeInterrupt, action => action.Kind == "source-voluntary-organization-decline");
        var interrupt = Assert.Single(beforeInterrupt, action => action.Kind == "source-sixth-australian-training-interrupt");
        var afterInterrupt = CampaignExercises.Submit(australianOrganization,
            Submission(australianOrganization, interrupt.ActionId, CampaignActionAudience.Commonwealth));
        Assert.True(afterInterrupt.IsAccepted);
        Assert.True(afterInterrupt.SuccessorSession!.SourceTurn!.Training.Interrupted);
        Assert.Contains(CampaignExercises.Query(afterInterrupt.SuccessorSession, CampaignActionAudience.Commonwealth).ActionSet!.Candidates,
            action => action.Kind == "source-voluntary-organization-decline");
        var cwDecline = CampaignExercises.Query(afterInterrupt.SuccessorSession, CampaignActionAudience.Commonwealth)
            .ActionSet!.Candidates.Single(action => action.Kind == "source-voluntary-organization-decline");
        var cwFinished = CampaignExercises.Submit(afterInterrupt.SuccessorSession,
            Submission(afterInterrupt.SuccessorSession, cwDecline.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.False(CampaignExercises.CompleteSourceGt1Organization(cwFinished).IsAccepted);
        var axisDecline = CampaignExercises.Query(cwFinished, CampaignActionAudience.Axis)
            .ActionSet!.Candidates.Single(action => action.Kind == "source-voluntary-organization-decline");
        var bothOrganizationOwnersFinished = CampaignExercises.Submit(cwFinished,
            Submission(cwFinished, axisDecline.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var completedRealOrganization = CampaignExercises.CompleteSourceGt1Organization(bothOrganizationOwnersFinished);
        Assert.True(completedRealOrganization.IsAccepted);
        Assert.Equal(Cna1979LandSequenceV4.GetNext(organizationCut), completedRealOrganization.SuccessorSession!.SourceTurn!.Position);
        Assert.True(completedRealOrganization.SuccessorSession.SourceTurn.Training.Interrupted);
        var stageOneArrival = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        var arrivalTurn = ContractTurn(australianWorld, stageOneArrival, 3, interrupted, firstSide: LandSide.Commonwealth);
        var dueStageOne = arrivalTurn.Source.OpeningConvoyPlan.Allocations().Where(value => value.OperationStage == 1).ToArray();
        if (dueStageOne.Length != 0)
        {
            Assert.Throws<InvalidOperationException>(() => arrivalTurn.CompleteGt1StageOneArrivals());
            if (dueStageOne.Length == 1 && dueStageOne[0].Lane == 2)
            {
                var deliveredAxis = CampaignSourceGt1AxisArrival.Create(arrivalTurn.Source).Announce().DeclineAttackAndDeliver();
                var deliveredTurn = ContractTurn(australianWorld, stageOneArrival, 3, interrupted, deliveredAxis);
                var afterDelivery = deliveredTurn.CompleteGt1StageOneArrivals();
                Assert.Equal(Cna1979LandSequenceV4.GetNext(stageOneArrival), afterDelivery.Position);
                Assert.Equal(deliveredAxis.Supply.Hash(), afterDelivery.AxisArrival.Supply.Hash());
            }
        }
        else
        {
            var arrivalContacts = australianFirst.CurrentContacts.AdvancePregameSource(arrivalTurn.Source.StateVersion);
            var arrivalSession = new ExerciseSession(arrivalTurn.Source.Pregame, arrivalContacts,
                australianFirst.PregameHistory, arrivalTurn.Source, arrivalTurn);
            var arrivalComplete = CampaignExercises.CompleteSourceGt1StageOneArrivals(arrivalSession);
            Assert.True(arrivalComplete.IsAccepted);
            Assert.Equal(Cna1979LandSequenceV4.GetNext(stageOneArrival), arrivalComplete.SuccessorSession!.SourceTurn!.Position);
            Assert.Equal(arrivalComplete.SuccessorSession.SourceTurn.Position.PositionId, arrivalComplete.Evidence!.Receipt.ResultingPositionId);
            Assert.True(CampaignExercises.IsSourceGt1StageOneArrivalsCompleted(arrivalComplete.SuccessorSession));
            var arrivalCommand = CampaignHostAdapter.SourceTurnSystemCommand(arrivalSession, arrivalComplete);
            var arrivalStep = new CampaignHostAcceptedStep(CampaignActionSubmissionSerializer.SerializeCanonical(arrivalCommand),
                arrivalComplete.Evidence.EventRecords, CampaignActionAcceptanceReceiptSerializer.Serialize(arrivalComplete.Evidence.Receipt),
                arrivalComplete.Evidence.SnapshotCheckpoint, CampaignHostAcceptedStep.SourceGt1ArrivalsCompleteKind);
            Assert.Equal(CampaignHostAcceptedStep.SourceGt1ArrivalsCompleteContractVersion, arrivalStep.ContractVersion);
            var fleet = CampaignExercises.CompleteSourceGt1StageOneFleet(arrivalComplete.SuccessorSession);
            Assert.True(fleet.IsAccepted);
            var repairPosition = Cna1979LandSequenceV4.GetNext(arrivalComplete.SuccessorSession.SourceTurn.Position);
            Assert.Equal(LandSegmentIds.FleetRepair, repairPosition.SegmentId);
            Assert.Equal(Cna1979LandSequenceV4.GetNext(repairPosition), fleet.SuccessorSession!.SourceTurn!.Position);
            Assert.Equal(fleet.SuccessorSession.SourceTurn.Position.PositionId, fleet.Evidence!.Receipt.ResultingPositionId);
            Assert.True(CampaignExercises.IsSourceGt1StageOneFleetCompleted(fleet.SuccessorSession));
            Assert.False(CampaignExercises.CompleteSourceGt1StageOneFleet(fleet.SuccessorSession).IsAccepted);
            var firstReserveSession = fleet.SuccessorSession;
            Assert.Empty(CampaignExercises.Query(firstReserveSession, CampaignActionAudience.Axis).ActionSet!.Candidates);
            var reserveChoice = Assert.Single(CampaignExercises.Query(firstReserveSession, CampaignActionAudience.Commonwealth).ActionSet!.Candidates,
                value => value.Kind == "source-stage1-first-reserves-decline");
            var visibleReserves = CampaignExercises.QuerySourceActionsDraft(firstReserveSession, CampaignActionAudience.Commonwealth);
            Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(firstReserveSession.SourceTurn!.StateVersion,
                visibleReserves.VisibleBytes, 0, reserveChoice.ActionId));
            var noReserves = CampaignExercises.Submit(firstReserveSession,
                Submission(firstReserveSession, reserveChoice.ActionId, CampaignActionAudience.Commonwealth));
            Assert.True(noReserves.IsAccepted);
            Assert.Equal(LandSegmentIds.Movement, noReserves.SuccessorSession!.SourceTurn!.Position.SegmentId);
            Assert.Equal(LandSide.Commonwealth, noReserves.SuccessorSession.SourceTurn.Position.ActiveSide);
            Assert.Equal(noReserves.SuccessorSession.SourceTurn.Position.PositionId, noReserves.Evidence!.Receipt.ResultingPositionId);
            var movementSession = noReserves.SuccessorSession;
            Assert.Empty(CampaignExercises.Query(movementSession, CampaignActionAudience.Axis).ActionSet!.Candidates);
            var finish = Assert.Single(CampaignExercises.Query(movementSession, CampaignActionAudience.Commonwealth).ActionSet!.Candidates,
                value => value.Kind == "source-stage1-first-movement-finish");
            var movementDraft = CampaignExercises.QuerySourceActionsDraft(movementSession, CampaignActionAudience.Commonwealth);
            Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(movementSession.SourceTurn!.StateVersion,
                movementDraft.VisibleBytes, 0, finish.ActionId));
            var movementFinished = CampaignExercises.Submit(movementSession,
                Submission(movementSession, finish.ActionId, CampaignActionAudience.Commonwealth));
            Assert.True(movementFinished.IsAccepted);
            Assert.Equal(LandSegmentIds.BreakdownDetermination, movementFinished.SuccessorSession!.SourceTurn!.Position.SegmentId);
            Assert.Same(movementSession.SourceTurn.World, movementFinished.SuccessorSession.SourceTurn.World);
            Assert.Same(movementSession.SourceTurn.CommonwealthSupply, movementFinished.SuccessorSession.SourceTurn.CommonwealthSupply);
            Assert.Equal(movementFinished.SuccessorSession.SourceTurn.Position.PositionId, movementFinished.Evidence!.Receipt.ResultingPositionId);
            var breakdownFinished = CampaignExercises.CompleteSourceGt1FirstBreakdown(movementFinished.SuccessorSession);
            Assert.True(breakdownFinished.IsAccepted);
            Assert.Equal(LandSegmentIds.Combat, breakdownFinished.SuccessorSession!.SourceTurn!.Position.SegmentId);
            Assert.Equal(LandStepIds.PositionDetermination, breakdownFinished.SuccessorSession.SourceTurn.Position.StepId);
            Assert.Equal(movementFinished.SuccessorSession.SourceTurn.RandomState, breakdownFinished.SuccessorSession.SourceTurn.RandomState);
            Assert.Same(movementFinished.SuccessorSession.SourceTurn.World, breakdownFinished.SuccessorSession.SourceTurn.World);
            Assert.True(CampaignExercises.IsSourceGt1FirstBreakdownCompleted(breakdownFinished.SuccessorSession));
            var breakdownCommand = CampaignHostAdapter.SourceTurnSystemCommand(movementFinished.SuccessorSession, breakdownFinished);
            var breakdownStep = new CampaignHostAcceptedStep(CampaignActionSubmissionSerializer.SerializeCanonical(breakdownCommand),
                breakdownFinished.Evidence!.EventRecords, CampaignActionAcceptanceReceiptSerializer.Serialize(breakdownFinished.Evidence.Receipt),
                breakdownFinished.Evidence.SnapshotCheckpoint, CampaignHostAcceptedStep.SourceGt1BreakdownCompleteKind);
            Assert.Equal(CampaignHostAcceptedStep.SourceGt1BreakdownCompleteContractVersion, breakdownStep.ContractVersion);
        }
        var ccnnFirst = CampaignExercises.Submit(catanzaroConfirmed,
            Submission(catanzaroConfirmed, axisBatchChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(47, ccnnFirst.SourceAuthority!.ProfileVersion);
        var australianAfterCcnn = CampaignExercises.Query(ccnnFirst, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "sixth-australian-placement").ToArray();
        Assert.Equal(choicesBefore, australianAfterCcnn.Select(value => value.ChoiceJson).Order(StringComparer.Ordinal).ToArray());
        var sameAustralianChoice = australianAfterCcnn.Single(value => value.SourceActionId == CampaignSourceAuthorityState.SixthAustralianActionId("e1829"));
        var ccnnThenAustralian = CampaignExercises.Submit(ccnnFirst,
            Submission(ccnnFirst, sameAustralianChoice.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(australianFirst.SourceAuthority!.GroupedWorld!.ContentHash, ccnnThenAustralian.SourceAuthority!.GroupedWorld!.ContentHash);
        Assert.Equal(australianFirst.SourceAuthority.GroupedWorld.Hash(), ccnnThenAustralian.SourceAuthority.GroupedWorld.Hash());
        Assert.Equal(australianFirst.SourceAuthority.Battlefield!.Hash(), ccnnThenAustralian.SourceAuthority.Battlefield!.Hash());
        Assert.Equal(49, ccnnThenAustralian.SourceAuthority.ProfileVersion);
        Assert.Equal(14, australianFirst.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "1-ccnn-division").Members.Count);
        Assert.Equal(12, australianFirst.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "4-ccnn-division").Members.Count);
        var ccnnUnits = australianFirst.SourceAuthority.GroupedWorld.Groups.Where(value => value.GroupId is "1-ccnn-division" or "4-ccnn-division")
            .SelectMany(value => value.Members).ToArray();
        Assert.All(ccnnUnits.Where(value => value.ElementId.EndsWith("-ccnn-infantry", StringComparison.Ordinal)),
            value => { Assert.Equal(5, value.InfantryToe); Assert.Equal(10, value.CapabilityPointAllowance); });
        Assert.Equal(20, ccnnUnits.Single(value => value.ElementId == "1-ccnn-hq").CapabilityPointAllowance);
        Assert.Equal(30, ccnnUnits.Single(value => value.ElementId == "4-ccnn-hq").CapabilityPointAllowance);
        Assert.Equal(30, ccnnUnits.Single(value => value.ElementId == "i-medium-tank").CapabilityPointAllowance);
        Assert.True(CampaignExercises.Reconstruct(australianFirst).IsVerified);
        Assert.True(CampaignExercises.Reconstruct(ccnnThenAustralian).IsVerified);
        var arescaChoiceBefore = CampaignExercises.Query(ccnnFirst, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "aresca-disposition");
        var arescaChoiceAfter = CampaignExercises.Query(australianFirst, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "aresca-disposition");
        Assert.Equal(arescaChoiceBefore.ChoiceJson, arescaChoiceAfter.ChoiceJson);
        var arescaFirst = CampaignExercises.Submit(ccnnFirst,
            Submission(ccnnFirst, arescaChoiceBefore.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(55, arescaFirst.SourceAuthority!.ProfileVersion);
        var ausAfterAresca = CampaignExercises.Query(arescaFirst, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "sixth-australian-placement").ToArray();
        Assert.Equal(choicesBefore, ausAfterAresca.Select(value => value.ChoiceJson).Order(StringComparer.Ordinal).ToArray());
        var ausArescaChoice = ausAfterAresca.Single(value => value.SourceActionId == CampaignSourceAuthorityState.SixthAustralianActionId("e1829"));
        var arescaThenAustralian = CampaignExercises.Submit(arescaFirst,
            Submission(arescaFirst, ausArescaChoice.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var australianThenAresca = CampaignExercises.Submit(australianFirst,
            Submission(australianFirst, arescaChoiceAfter.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(57, australianThenAresca.SourceAuthority!.ProfileVersion);
        var aresca = australianThenAresca.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "aresca-tank-regiment");
        Assert.Equal(1, aresca.StackValue); Assert.Equal(10, aresca.MotorPool);
        Assert.Equal(1, aresca.Members.Single(value => value.ElementId == "aresca-tank-regiment-hq").ComponentToe);
        Assert.Equal(9, aresca.Members.Single(value => value.ElementId == "xxi-libyan-light-tank").ComponentToe);
        Assert.Equal(australianThenAresca.SourceAuthority.GroupedWorld.Hash(), arescaThenAustralian.SourceAuthority!.GroupedWorld!.Hash());
        Assert.Equal(australianThenAresca.SourceAuthority.Battlefield!.Hash(), arescaThenAustralian.SourceAuthority.Battlefield!.Hash());
        Assert.True(CampaignExercises.Reconstruct(australianThenAresca).IsVerified);
        Assert.True(CampaignExercises.Reconstruct(arescaThenAustralian).IsVerified);
        var correctedCcnn = CampaignExercises.CorrectSourceSeventhShell(australianFirst).SuccessorSession!;
        var correctedArescaChoice = CampaignExercises.Query(correctedCcnn, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "aresca-disposition");
        var correctedAresca = CampaignExercises.Submit(correctedCcnn,
            Submission(correctedCcnn, correctedArescaChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(3, correctedAresca.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division").StackValue);
        Assert.Equal(1, correctedAresca.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "aresca-tank-regiment").StackValue);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedLegacyCheckpointContractVersion, correctedAresca.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(correctedAresca).IsVerified);
        var gunSiteChoice = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "sixth-australian-placement"
                && value.SourceActionId == CampaignSourceAuthorityState.SixthAustralianActionId(gunCairoHex));
        var australianWithGuns = CampaignExercises.Submit(catanzaroConfirmed,
            Submission(catanzaroConfirmed, gunSiteChoice.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(3, australianWithGuns.SourceAuthority!.Battlefield!.Entries.Count(value => value.LocationId == gunCairoHex
            && freeGunIds.Contains(value.ElementId)));
        Assert.Equal(5, australianWithGuns.SourceAuthority.GroupedWorld!.Groups.Single(value => value.GroupId == australian.GroupId).StackValue);
        Assert.True(CampaignExercises.Reconstruct(australianWithGuns).IsVerified);
        Assert.Equal(5, catanzaroConfirmed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == catanzaro.GroupId).StackValue);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(catanzaroConfirmed, catanzaroCommand).RejectionReason);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.World.PendingArrivals, catanzaroConfirmed.SourceAuthority.World.PendingArrivals);
        Assert.True(CampaignExercises.Reconstruct(catanzaroConfirmed).IsVerified);
        var beforeTurn = CampaignExercises.QuerySourceActionsDraft(catanzaroConfirmed, CampaignActionAudience.Axis);
        Assert.False(CampaignExercises.IsSourceTurnStarted(catanzaroConfirmed));
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.StartSourceTurn(catanzaroConfirmed).RejectionReason);
        Assert.Equal(beforeTurn.VisibleBytes,
            CampaignExercises.QuerySourceActionsDraft(catanzaroConfirmed, CampaignActionAudience.Axis).VisibleBytes);
        Assert.True(CampaignExercises.Reconstruct(catanzaroConfirmed).IsVerified);
        if (seed == 0)
        {
            // Stress the unified legal-action contract after real grouped admissions.
            // Each advertised family is submitted against the same retained state,
            // independently of the other choices, then checked for replay and loss.
            foreach (var audience in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
            {
                var actions = CampaignExercises.Query(italianConfirmed, audience).ActionSet!.Candidates
                    .OfType<CampaignSourceSetupAction>().GroupBy(value => value.Family, StringComparer.Ordinal)
                    .SelectMany(group => group.Take(1).Concat(group.TakeLast(1)).DistinctBy(value => value.ActionId));
                foreach (var action in actions)
                {
                    var result = CampaignExercises.Submit(italianConfirmed, Submission(italianConfirmed, action.ActionId, audience));
                    Assert.True(result.IsAccepted, $"Advertised {audience} action {action.Family} was rejected: {result.RejectionReason}");
                    var successor = result.SuccessorSession!;
                    Assert.Equal(italianConfirmed.SourceAuthority.World.PendingArrivals, successor.SourceAuthority!.World.PendingArrivals);
                    Assert.Equal(italianConfirmed.SourceAuthority.GroupedWorld.Groups.Count, successor.SourceAuthority.GroupedWorld!.Groups.Count);
                    Assert.True(CampaignExercises.Reconstruct(successor).IsVerified, $"Could not replay {audience} action {action.Family}");
                }
            }
            // Exercise a deterministic interleaving from the same historical state.
            // The second branch executes the same user choices independently.
            var random = new Random(3451979);
            var left = italianConfirmed;
            var right = italianConfirmed;
            for (var i = 0; i < 16; i++)
            {
                var audience = i % 2 == 0 ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
                var choices = CampaignExercises.Query(left, audience).ActionSet!.Candidates
                    .OfType<CampaignSourceSetupAction>().ToArray();
                Assert.NotEmpty(choices);
                var action = choices[random.Next(choices.Length)];
                var leftStep = CampaignExercises.Submit(left, Submission(left, action.ActionId, audience));
                var rightStep = CampaignExercises.Submit(right, Submission(right, action.ActionId, audience));
                Assert.True(leftStep.IsAccepted, $"Interleaved {audience} action {action.Family} failed at step {i}");
                Assert.True(rightStep.IsAccepted, $"Repeated {audience} action {action.Family} failed at step {i}");
                Assert.Equal(leftStep.Evidence!.SnapshotCheckpoint, rightStep.Evidence!.SnapshotCheckpoint);
                left = leftStep.SuccessorSession!;
                right = rightStep.SuccessorSession!;
                Assert.Equal(left.SourceAuthority!.RandomState, right.SourceAuthority!.RandomState);
                Assert.Equal(italianConfirmed.SourceAuthority.World.PendingArrivals, left.SourceAuthority.World.PendingArrivals);
            }
            Assert.True(CampaignExercises.Reconstruct(left).IsVerified);
            Assert.True(CampaignExercises.Reconstruct(right).IsVerified);
            if (Environment.GetEnvironmentVariable("SANDTABLE_GRAZIANI_SOAK") == "1")
                RunSourceSoak(italianConfirmed);
        }
        var secondConfirmed = CampaignExercises.Submit(libyanProposed, Submission(libyanProposed, secondChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var firstAfter = CampaignExercises.Query(secondConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "libyan-disposition");
        secondConfirmed = CampaignExercises.Submit(secondConfirmed, Submission(secondConfirmed, firstAfter.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(secondConfirmed).IsVerified);

        var noMaletti = CampaignExercises.AdmitSourceMatruhFormation(indianWithoutMalettiDetachment).SuccessorSession!;
        noMaletti = CampaignExercises.SubmitSourceMatruhFormation(noMaletti,
            Submission(noMaletti, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        noMaletti = CampaignExercises.AdmitSourceMatruhGroupedWorld(noMaletti).SuccessorSession!;
        var afterDetachBeforeGroup = CampaignExercises.SubmitSourceMalettiHierarchy(noMaletti,
            Submission(noMaletti, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        afterDetachBeforeGroup = CampaignExercises.SubmitSourceAxisPriority(afterDetachBeforeGroup,
            Submission(afterDetachBeforeGroup, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        afterDetachBeforeGroup = CampaignExercises.AdmitSourceMalettiGroupedWorld(afterDetachBeforeGroup).SuccessorSession!;
        Assert.Equal(5, afterDetachBeforeGroup.SourceAuthority!.GroupedWorld!.Detached.Single(value => value.GroupId == "maletti-division").Member.AssignedMotorPoints);
        afterDetachBeforeGroup = CampaignExercises.PromoteSourceCertifiedMalettiWorld(afterDetachBeforeGroup).SuccessorSession!;
        Assert.Equal(5, afterDetachBeforeGroup.SourceAuthority!.GroupedWorld!.ExternalReservations.Single(value => value.GroupId == "maletti-division").AssignedMotorPoints);
        Assert.True(CampaignExercises.Reconstruct(afterDetachBeforeGroup).IsVerified);

        var beforeDetach = CampaignExercises.AdmitSourceMalettiGroupedWorld(noMaletti).SuccessorSession!;
        beforeDetach = CampaignExercises.SubmitSourceMalettiHierarchy(beforeDetach,
            Submission(beforeDetach, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        beforeDetach = CampaignExercises.SubmitSourceAxisPriority(beforeDetach,
            Submission(beforeDetach, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(5, beforeDetach.SourceAuthority!.GroupedWorld!.Detached.Single(value => value.GroupId == "maletti-division").Member.AssignedMotorPoints);
        beforeDetach = CampaignExercises.PromoteSourceCertifiedMalettiWorld(beforeDetach).SuccessorSession!;
        Assert.Equal(5, beforeDetach.SourceAuthority!.GroupedWorld!.ExternalReservations.Single(value => value.GroupId == "maletti-division").AssignedMotorPoints);
        var axisRemove = CampaignSourceAuthorityState.AxisPriorityActionId("adjust", targetAxis, -5);
        beforeDetach = CampaignExercises.SubmitSourceAxisPriority(beforeDetach,
            Submission(beforeDetach, axisRemove, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(35, beforeDetach.SourceAuthority!.GroupedWorld!.RemainingMotorPoints("maletti-division"));
        Assert.True(CampaignExercises.Reconstruct(beforeDetach).IsVerified);
        matruhBeforeDetach = CampaignExercises.SubmitSourceMatruhFormation(matruhBeforeDetach,
            Submission(matruhBeforeDetach, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Single(matruhBeforeDetach.SourceAuthority!.GroupedWorld!.Detached);
        Assert.Equal(2, matruhBeforeDetach.SourceAuthority.GroupedWorld.OccupantsAt("d3714").Count);
        Assert.True(CampaignExercises.Reconstruct(matruhBeforeDetach).IsVerified);
        matruhBeforeDetach = CampaignExercises.PromoteSourceMatruhDetachedWorld(matruhBeforeDetach).SuccessorSession!;
        Assert.Empty(matruhBeforeDetach.SourceAuthority!.GroupedWorld!.Detached);
        Assert.Equal(2, matruhBeforeDetach.SourceAuthority.GroupedWorld.OccupantsAt("d3714").Count);
        Assert.True(CampaignExercises.Reconstruct(matruhBeforeDetach).IsVerified);
        matruhAfterDetach = CampaignExercises.SubmitSourceMatruhFormation(matruhAfterDetach,
            Submission(matruhAfterDetach, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var matruhBetween = CampaignExercises.AdmitSourceMatruhGroupedWorld(matruhAfterDetach).SuccessorSession!;
        Assert.Single(matruhBetween.SourceAuthority!.GroupedWorld!.Detached);
        matruhBetween = CampaignExercises.PromoteSourceMatruhDetachedWorld(matruhBetween).SuccessorSession!;
        Assert.Empty(matruhBetween.SourceAuthority!.GroupedWorld!.Detached);
        Assert.Equal(2, matruhBetween.SourceAuthority.GroupedWorld.OccupantsAt("d3714").Count);
        Assert.True(CampaignExercises.Reconstruct(matruhBetween).IsVerified);
        matruhAfterDetach = CampaignExercises.PromoteSourceMatruhDetachedWorld(matruhAfterDetach).SuccessorSession!;
        matruhAfterDetach = CampaignExercises.AdmitSourceMatruhGroupedWorld(matruhAfterDetach).SuccessorSession!;
        Assert.Empty(matruhAfterDetach.SourceAuthority!.GroupedWorld!.Detached);
        Assert.Equal(2, matruhAfterDetach.SourceAuthority.GroupedWorld.OccupantsAt("d3714").Count);
        Assert.Equal(5, matruhAfterDetach.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "matruh-garrison").Members.Count);
        Assert.True(CampaignExercises.Reconstruct(matruhAfterDetach).IsVerified);
        var normalizedIdentity = frenchFirst.SourceAuthority!.World.ContentIdentity;
        frenchFirst = CampaignExercises.SubmitSourceMalettiHierarchy(frenchFirst,
            Submission(frenchFirst, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        frenchFirst = CampaignExercises.PromoteSourceCertifiedMalettiWorld(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.AdmitSourceMatruhFormation(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.SubmitSourceMatruhFormation(frenchFirst,
            Submission(frenchFirst, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        frenchFirst = CampaignExercises.SubmitSourceMatruhFormation(frenchFirst,
            Submission(frenchFirst, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        frenchFirst = CampaignExercises.PromoteSourceMatruhDetachedWorld(frenchFirst).SuccessorSession!;
        Assert.Equal(normalizedIdentity, frenchFirst.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(25, frenchFirst.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, frenchFirst.SourceAuthority.World.PendingArrivals.Count);
        Assert.True(CampaignExercises.Reconstruct(frenchFirst).IsVerified);
        var oldCheckpointLength = directV12.SourceAuthority!.SerializeCheckpoint().Length;
        directV12 = CampaignExercises.AdmitSourceCompactCheckpoint(directV12).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.True(directV12.SourceAuthority!.SerializeCheckpoint().Length < oldCheckpointLength / 100);
        var contentBeforeCompactAction = directV12.SourceAuthority.World.ContentIdentity;
        directV12 = CampaignExercises.SubmitSourceCwPriorityCatalog(directV12,
            Submission(directV12, frenchAdd, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(contentBeforeCompactAction, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.True(CampaignExercises.Reconstruct(directV12).IsVerified);
        var beginCargo = CampaignSourceAuthorityState.UnattachedCargoActionId(true, "begin", null, 0);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceUnattachedCargo(directV12, Submission(directV12, beginCargo, CampaignActionAudience.Axis)).RejectionReason);
        foreach (var amount in new[] { 50, 50, 25 })
        {
            var allocate = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", amount);
            directV12 = CampaignExercises.AdjustSourceMotorization(directV12, Submission(directV12, allocate, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        directV12 = CampaignExercises.SubmitSourceUnattachedCargo(directV12, Submission(directV12, beginCargo, CampaignActionAudience.Axis)).SuccessorSession!;
        var addCargo = CampaignSourceAuthorityState.UnattachedCargoActionId(true, "adjust", "box.tripoli", 1);
        for (var i = 0; i < 6; i++)
            directV12 = CampaignExercises.SubmitSourceUnattachedCargo(directV12, Submission(directV12, addCargo, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(6, directV12.SourceAuthority!.AxisUnattachedCargo!.Quantity("box.tripoli"));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceUnattachedCargo(directV12, Submission(directV12, addCargo, CampaignActionAudience.Axis)).RejectionReason);
        var forbiddenMove = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", -1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourceMotorization(directV12, Submission(directV12, forbiddenMove, CampaignActionAudience.Axis)).RejectionReason);
        var finishCargo = CampaignSourceAuthorityState.UnattachedCargoActionId(true, "finish", null, 0);
        directV12 = CampaignExercises.SubmitSourceUnattachedCargo(directV12, Submission(directV12, finishCargo, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(directV12.SourceAuthority!.AxisUnattachedCargo!.IsFinished);
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(directV12).IsVerified);
        var lateAxis = CampaignExercises.QuerySourceActionsDraft(directV12, CampaignActionAudience.Axis);
        var lateCw = CampaignExercises.QuerySourceActionsDraft(directV12, CampaignActionAudience.Commonwealth);
        var lateAxisText = System.Text.Encoding.UTF8.GetString(lateAxis.VisibleBytes);
        var lateCwText = System.Text.Encoding.UTF8.GetString(lateCw.VisibleBytes);
        Assert.Contains("axis-gt2-correction", lateAxisText);
        Assert.DoesNotContain("cw-priority-catalog", lateAxisText);
        Assert.Contains("cw-priority-catalog", lateCwText);
        Assert.DoesNotContain("axis-gt2-correction", lateCwText);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(directV12, Submission(directV12, undoDetachment, CampaignActionAudience.Axis)).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(directV12).IsVerified);
        Assert.False(CampaignExercises.CanPromoteSourceMaletti(cwSession));
        cwSession = CampaignExercises.PromoteSourceIndependent(cwSession).SuccessorSession!;
        Assert.Equal(10, cwSession.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(2, cwSession.SourceAuthority.World.PendingArrivals.Count);
        Assert.Equal(genericIdentity, cwSession.SourceAuthority.World.ContentIdentity);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(3, cwSession.SourceAuthority!.AxisSupplyWorld.Units.Count(x => x.IsDummy));
        Assert.Equal(3, cwSession.SourceAuthority.CommonwealthSupplyWorld.Units.Count(x => x.IsDummy));
        Assert.Null(cwSession.SourceAuthority.Position);
        var motor = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", 50);
        Assert.Contains(motor, CampaignExercises.QuerySourceMotorizationDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourceMotorization(cwSession, Submission(cwSession, motor, CampaignActionAudience.Commonwealth)).RejectionReason);
        cwSession = CampaignExercises.AdjustSourceMotorization(cwSession, Submission(cwSession, motor, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(75, cwSession.SourceAuthority!.AxisMotorization.Remaining("axis-libya-unattached"));
        var remove = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", -10);
        cwSession = CampaignExercises.AdjustSourceMotorization(cwSession, Submission(cwSession, remove, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(85, cwSession.SourceAuthority!.AxisMotorization.Remaining("axis-libya-unattached"));
        var cwMotor = CampaignSourceAuthorityState.MotorizationActionId(false, "cw-cairo-unattached", "e1829", 25);
        cwSession = CampaignExercises.AdjustSourceMotorization(cwSession, Submission(cwSession, cwMotor, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(25, cwSession.SourceAuthority!.CommonwealthMotorization.Remaining("cw-cairo-unattached"));
        var tooMuch = CampaignSourceAuthorityState.MotorizationActionId(false, "cw-cairo-unattached", "e1829", 50);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourceMotorization(cwSession, Submission(cwSession, tooMuch, CampaignActionAudience.Commonwealth)).RejectionReason);
        Assert.Equal(cwSession.SourceAuthority!.IndependentWorld!.Content.Identity, cwSession.SourceAuthority.World.ContentIdentity);
        Assert.Equal(10, cwSession.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, cwSession.SourceAuthority.World.PendingArrivals.Count);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
    }

    // Standalone proof (pure addition, pinned Theory method untouched): Aresca
    // (aresca-tank-regiment-hq, xxi-libyan-light-tank) is NOT a missing-code gap -
    // CanAdmitAresca already exists and works, it just requires submission before
    // Axis owner finalization (see CanAdmitAresca's !IsOwnerFinalized(Axis) guard).
    // The pinned test's own readyWorld/motorizedWorld branch never submits the
    // aresca-disposition action at all, so these 2 elements show as
    // forces-incomplete there - a fixture-branch gap, not a code gap. This test
    // reuses the pinned test's own proven setup verbatim (identical prefix,
    // hardcoded to the seed: 0, maximum: 3, allocated: 1 InlineData row) through
    // its own ccnnThenAustralian/australianThenAresca checkpoints, which the
    // pinned test already asserts are otherwise identical (see the GroupedWorld/
    // Battlefield hash equality asserts on australianFirst vs ccnnThenAustralian
    // above), to isolate Aresca admission as the only delta.
    [Fact]
    public void AdmittingArescaBeforeOwnerFinalizationRemovesItsTwoElementsFromForcesIncomplete()
    {
        var seed = 0UL; var maximum = 3; var allocated = 1;
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var initialization = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "source-authority-transition", Cna1979Ruleset.Manifest.Hash, seed,
            CampaignPregameSourceCreation.SetupId,
            CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, initialization),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var session = CampaignExercises.Begin(request, artifact, initialization).Session!;
        var polishActionPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        var polishAction = new CampaignSourceTurnAction(17, "e1730");
        var polishSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            polishActionPosition.PositionId, CampaignActionAudience.Commonwealth, [polishAction], CampaignLegalActionSet.SourceTurnPolicyId);
        var polishBytes = CampaignLegalActionSerializer.Serialize(polishSet);
        Assert.Equal(polishSet, CampaignLegalActionSerializer.DeserializeCanonical(polishBytes));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            polishActionPosition.PositionId, CampaignActionAudience.Axis, [polishAction], CampaignLegalActionSet.SourceTurnPolicyId));
        var arrivalPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        var declineAttack = CampaignSourceTurnAction.DeclineGt1Lane2Attack(17);
        var arrivalSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            arrivalPosition.PositionId, CampaignActionAudience.Commonwealth, [declineAttack], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(arrivalSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(arrivalSet)));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            arrivalPosition.PositionId, CampaignActionAudience.Axis, [declineAttack], CampaignLegalActionSet.SourceTurnPolicyId));
        var convoyPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.PhaseId == LandPhaseIds.NavalConvoySchedule);
        var convoyRoll = CampaignSourceTurnAction.RollGt3Supply(17);
        var convoySet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Axis, [convoyRoll], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(convoySet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(convoySet)));
        var convoyChoices = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Axis,
            [CampaignSourceTurnAction.AdjustGt3Supply(17, 2, 1, 1), CampaignSourceTurnAction.FinishGt3Supply(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(convoyChoices, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(convoyChoices)));
        var cwRoll = CampaignSourceTurnAction.RollGt5Supply(17);
        var cwConvoySet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Commonwealth, [cwRoll], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(cwConvoySet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(cwConvoySet)));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Axis, [cwRoll], CampaignLegalActionSet.SourceTurnPolicyId));
        var shippingPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.PhaseId == LandPhaseIds.TacticalShipping);
        var shippingSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            shippingPosition.PositionId, CampaignActionAudience.Commonwealth, [CampaignSourceTurnAction.DeclineTacticalShipping(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(shippingSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(shippingSet)));
        var declarationPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1 && value.PhaseId == LandPhaseIds.InitiativeDeclaration);
        var declarationSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            declarationPosition.PositionId, CampaignActionAudience.Axis, [CampaignSourceTurnAction.ActFirst(17), CampaignSourceTurnAction.ActLast(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(declarationSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(declarationSet)));
        var organizationPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1 && value.PhaseId == LandPhaseIds.Organization);
        var organizationSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            organizationPosition.PositionId, CampaignActionAudience.Commonwealth, [CampaignSourceTurnAction.DeclineVoluntaryOrganization(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(organizationSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(organizationSet)));
        var interruptTraining = CampaignSourceTurnAction.InterruptSixthAustralianTraining(17);
        var trainingSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            organizationPosition.PositionId, CampaignActionAudience.Commonwealth, [interruptTraining], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(trainingSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(trainingSet)));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            organizationPosition.PositionId, CampaignActionAudience.Axis, [interruptTraining], CampaignLegalActionSet.SourceTurnPolicyId));
        var unknownTraining = CampaignSourceTrainingLedger.CreateSixthAustralian();
        var interruptedTraining = unknownTraining.Interrupt();
        Assert.True(interruptedTraining.Interrupted);
        Assert.Null(interruptedTraining.CurrentMorale);
        Assert.Null(interruptedTraining.QualifyingStages);
        Assert.NotEqual(unknownTraining.Hash(), interruptedTraining.Hash());
        Assert.Throws<InvalidOperationException>(() => interruptedTraining.Interrupt());
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Commonwealth, [convoyRoll], CampaignLegalActionSet.SourceTurnPolicyId));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Axis));
        var finishBeforeRoll = Submission(session, CampaignSourceAuthorityState.FinishActionId, CampaignActionAudience.Axis);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.FinishPregameAxisSupply(session, finishBeforeRoll).RejectionReason);
        var roll = Assert.Single(CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates);
        session = CampaignExercises.Submit(session, Submission(session, roll.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        if (allocated > 0)
        {
            var add = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates
                .OfType<PregameAxisSupplyAllocationAction>()
                .Single(value => value.Lane == 2 && value.Stage == 1 && value.Delta == 1);
            session = CampaignExercises.Submit(session, Submission(session, add.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        var legacyOffers = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates.Select(x => x.ActionId).ToArray();
        var draft = CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Axis);
        Assert.Single(draft.PrivateActionIds);
        Assert.DoesNotContain(draft.PrivateActionIds[0], legacyOffers);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.FinishPregameAxisSupply(session, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Commonwealth)).RejectionReason);
        var accepted = CampaignExercises.FinishPregameAxisSupply(session, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Axis));
        Assert.True(accepted.IsAccepted);
        var next = accepted.SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(next).IsVerified);
        var giarabubAction = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c1014", 1);
        var oldAxisMotorHash = next.SourceAuthority!.AxisMotorization.Hash();
        var oldAxisChoices = CampaignExercises.QuerySourceMotorizationDraft(next, CampaignActionAudience.Axis);
        Assert.DoesNotContain(giarabubAction, oldAxisChoices.PrivateActionIds);
        var admittedPolicy = CampaignExercises.AdmitSourceGiarabubMotorPlacementPolicy(next).SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceGiarabubMotorPlacementPolicyAdmitted(admittedPolicy));
        Assert.NotEqual(oldAxisMotorHash, admittedPolicy.SourceAuthority!.AxisMotorization.Hash());
        Assert.Equal(oldAxisMotorHash, next.SourceAuthority.AxisMotorization.Hash());
        Assert.DoesNotContain(giarabubAction, CampaignExercises.QuerySourceMotorizationDraft(next, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Contains(giarabubAction, CampaignExercises.QuerySourceMotorizationDraft(admittedPolicy, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.DoesNotContain(giarabubAction, CampaignExercises.QuerySourceMotorizationDraft(admittedPolicy, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.AdjustSourceMotorization(admittedPolicy,
            Submission(admittedPolicy, giarabubAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        var placedMotor = CampaignExercises.AdjustSourceMotorization(admittedPolicy,
            Submission(admittedPolicy, giarabubAction, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(124, placedMotor.SourceAuthority!.AxisMotorization.Remaining("axis-libya-unattached"));
        Assert.Contains(placedMotor.SourceAuthority.AxisMotorization.Allocations,
            value => value.PoolId == "axis-libya-unattached" && value.LocationId == "c1014" && value.Points == 1);
        Assert.True(CampaignExercises.Reconstruct(placedMotor).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceGiarabubMotorPlacementPolicy(placedMotor).RejectionReason);
        var cwBeforeFinish = CampaignExercises.QuerySourceActionsDraft(next, CampaignActionAudience.Commonwealth);
        var finishOwner = CampaignExercises.Query(next, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var ownerFinished = CampaignExercises.Submit(next, Submission(next, finishOwner.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(1, ownerFinished.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.FinalizedLegacyCheckpointContractVersion, CampaignExercises.QueryCheckpoint(ownerFinished).ContractVersion);
        Assert.Equal(next.SourceAuthority!.RandomState, ownerFinished.SourceAuthority.RandomState);
        Assert.Equal(next.SourceAuthority.World.PendingArrivals, ownerFinished.SourceAuthority.World.PendingArrivals);
        Assert.Equal(cwBeforeFinish.VisibleBytes, CampaignExercises.QuerySourceActionsDraft(ownerFinished, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.PlaceSourceAxisDump(ownerFinished,
            Submission(ownerFinished, CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507"), CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceCompactCheckpoint(ownerFinished).RejectionReason);
        var cwWhileAxisFinished = CampaignExercises.PlaceSourceCommonwealthSupply(ownerFinished,
            Submission(ownerFinished, CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-dump-1", "d3612"), CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, cwWhileAxisFinished.SourceAuthority!.FinalizedOwners);
        var finishCw = CampaignExercises.Query(cwWhileAxisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var bothFinished = CampaignExercises.Submit(cwWhileAxisFinished, Submission(cwWhileAxisFinished, finishCw.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(3, bothFinished.SourceAuthority!.FinalizedOwners);
        Assert.True(CampaignExercises.Reconstruct(bothFinished).IsVerified);
        var reopenAxis = CampaignExercises.Query(bothFinished, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var reopenedAxis = CampaignExercises.Submit(bothFinished, Submission(bothFinished, reopenAxis.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, reopenedAxis.SourceAuthority!.FinalizedOwners);
        Assert.True(CampaignExercises.Reconstruct(reopenedAxis).IsVerified);
        var reopenCw = CampaignExercises.Query(reopenedAxis, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var reopenedBoth = CampaignExercises.Submit(reopenedAxis, Submission(reopenedAxis, reopenCw.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(0, reopenedBoth.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.FinalizedLegacyCheckpointContractVersion, CampaignExercises.QueryCheckpoint(reopenedBoth).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(reopenedBoth).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.PlaceSourceCommonwealthSupply(reopenedAxis,
            Submission(reopenedAxis, CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-cairo-1", "e1730"), CampaignActionAudience.Commonwealth)).RejectionReason);
        var lockedCwView = CampaignExercises.QuerySourceActionsDraft(reopenedAxis, CampaignActionAudience.Commonwealth).VisibleBytes;
        var changedAxis = CampaignExercises.PlaceSourceAxisDump(reopenedAxis,
            Submission(reopenedAxis, CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507"), CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, changedAxis.SourceAuthority!.FinalizedOwners);
        Assert.Equal(lockedCwView, CampaignExercises.QuerySourceActionsDraft(changedAxis, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.True(CampaignExercises.Reconstruct(changedAxis).IsVerified);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourcePolishArrivalDraft(next, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.SubmitSourcePolishArrival(next, Submission(next, roll.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var earlyCompact = CampaignExercises.AdmitSourceCompactCheckpoint(next).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(earlyCompact).ContractVersion);
        var compactFinish = CampaignExercises.Query(earlyCompact, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var finalizedCompact = CampaignExercises.Submit(earlyCompact, Submission(earlyCompact, compactFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignSourceAuthorityState.FinalizedCompactCheckpointContractVersion, CampaignExercises.QueryCheckpoint(finalizedCompact).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(finalizedCompact).IsVerified);
        var earlyDump = CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507");
        earlyCompact = CampaignExercises.PlaceSourceAxisDump(earlyCompact,
            Submission(earlyCompact, earlyDump, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(earlyCompact).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(earlyCompact).IsVerified);
        var normalAxis = CampaignExercises.Query(next, CampaignActionAudience.Axis);
        Assert.True(normalAxis.IsSuccessful);
        Assert.Equal(CampaignLegalActionSet.SourceSetupPolicyId, normalAxis.ActionSet!.PolicyId);
        var encodedActions = CampaignLegalActionSerializer.Serialize(normalAxis.ActionSet);
        Assert.Equal(encodedActions, CampaignLegalActionSerializer.Serialize(CampaignLegalActionSerializer.DeserializeCanonical(encodedActions)));
        var unifiedDump = normalAxis.ActionSet.Candidates.OfType<CampaignSourceSetupAction>()
            .Single(value => value.Family == "axis-dump" && value.ChoiceJson.Contains("c4507", StringComparison.Ordinal)
                && value.ChoiceJson.Contains("axis-dump-1", StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(normalAxis.ActionSet.CampaignId,
            normalAxis.ActionSet.StateVersion, normalAxis.ActionSet.RulesetHash, normalAxis.ActionSet.PositionId,
            CampaignActionAudience.Axis, [unifiedDump], CampaignLegalActionSet.CurrentPolicyId));
        var ownerCut = CampaignExercises.QuerySourceActionsDraft(next, CampaignActionAudience.Axis);
        Assert.DoesNotContain(unifiedDump.ActionId, System.Text.Encoding.UTF8.GetString(ownerCut.VisibleBytes));
        var ownContext = System.Text.Encoding.UTF8.GetString(ownerCut.VisibleBytes);
        Assert.Contains("axis-gt1-supply", ownContext);
        Assert.Contains("maximumSupply", ownContext);
        var hiddenCwAction = CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-dump-1", "d3612");
        var hiddenAdvance = CampaignExercises.PlaceSourceCommonwealthSupply(next,
            Submission(next, hiddenCwAction, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var afterHidden = CampaignExercises.QuerySourceActionsDraft(hiddenAdvance, CampaignActionAudience.Axis);
        Assert.Equal(ownerCut.VisibleBytes, afterHidden.VisibleBytes);
        Assert.NotEqual(ownerCut.PrivateActionIds[0], afterHidden.PrivateActionIds[0]);
        var wrongSide = Submission(next, unifiedDump.ActionId, CampaignActionAudience.Commonwealth);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(next, wrongSide).RejectionReason);
        var unifiedResult = CampaignExercises.Submit(next, Submission(next, unifiedDump.ActionId, CampaignActionAudience.Axis));
        Assert.True(unifiedResult.IsAccepted);
        Assert.Equal(unifiedDump.ActionId, unifiedResult.Evidence!.Receipt.ActionId);
        Assert.True(CampaignExercises.Reconstruct(unifiedResult.SuccessorSession!).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(unifiedResult.SuccessorSession!,
            Submission(next, unifiedDump.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(unifiedResult.SuccessorSession!,
            Submission(unifiedResult.SuccessorSession!, unifiedDump.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.FinishPregameAxisSupply(next, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Axis)).RejectionReason);
        using var snapshot = JsonDocument.Parse(accepted.Evidence!.SnapshotCheckpoint);
        Assert.Equal("setup-in-progress", snapshot.RootElement.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, snapshot.RootElement.GetProperty("gameClock").ValueKind);
        using var plan = JsonDocument.Parse(snapshot.RootElement.GetProperty("convoyPlanBase64").GetBytesFromBase64());
        Assert.Equal(maximum, plan.RootElement.GetProperty("maximumSupply").GetInt32());
        Assert.Equal(maximum - allocated, plan.RootElement.GetProperty("unusedAvailability").GetInt32());
        using var source = JsonDocument.Parse(snapshot.RootElement.GetProperty("sourceSnapshotBase64").GetBytesFromBase64());
        Assert.Contains(source.RootElement.GetProperty("pendingArrivalGroupIds").EnumerateArray().Select(x => x.GetString()),
            id => id == Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId);
        var dumpDraft = CampaignExercises.QuerySourceAxisDumpDraft(next, CampaignActionAudience.Axis);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisDumpDraft(next, CampaignActionAudience.Commonwealth));
        Assert.Equal(8, dumpDraft.PrivateActionIds.Count);
        var chosenId = CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507");
        Assert.Contains(chosenId, dumpDraft.PrivateActionIds);
        var dumpCommand = Submission(next, chosenId, CampaignActionAudience.Axis);
        var placed = CampaignExercises.PlaceSourceAxisDump(next, dumpCommand);
        Assert.True(placed.IsAccepted);
        var after = placed.SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(after).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
            CampaignExercises.PlaceSourceAxisDump(after, dumpCommand).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceAxisDump(after, Submission(after,
                CampaignSourceAuthorityState.DumpActionId("axis-dump-2", "c4507"), CampaignActionAudience.Axis)).RejectionReason);
        var second = CampaignExercises.PlaceSourceAxisDump(after, Submission(after,
            CampaignSourceAuthorityState.DumpActionId("axis-dump-2", "c4707"), CampaignActionAudience.Axis));
        Assert.True(second.IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(second.SuccessorSession!).IsVerified);
        var corruptHistory = second.SuccessorSession!.PregameHistory.Select(value => value.ToArray()).ToArray();
        var lastEvent = System.Text.Encoding.UTF8.GetString(corruptHistory[^1]).Replace("c4707", "c4507", StringComparison.Ordinal);
        corruptHistory[^1] = System.Text.Encoding.UTF8.GetBytes(lastEvent);
        var corrupt = new ExerciseSession(second.SuccessorSession.Pregame!, second.SuccessorSession.CurrentContacts,
            corruptHistory, second.SuccessorSession.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(corrupt).IsVerified);
        using var finalSnapshot = JsonDocument.Parse(second.Evidence!.SnapshotCheckpoint);
        using var world = JsonDocument.Parse(finalSnapshot.RootElement.GetProperty("axisSupplyWorldBase64").GetBytesFromBase64());
        Assert.Equal(22, world.RootElement.GetProperty("units").GetArrayLength());
        Assert.False(world.RootElement.GetProperty("siteCoverageComplete").GetBoolean());
        Assert.Equal(JsonValueKind.Null, world.RootElement.GetProperty("units").EnumerateArray()
            .Single(x => x.GetProperty("id").GetString() == "axis-derna-1").GetProperty("ammo").ValueKind);
        var cwSession = second.SuccessorSession!;
        Assert.Equal(CampaignSourceAuthorityState.ContractVersion, cwSession.SourceAuthority!.ProfileVersion);
        Assert.True(CampaignExercises.IsSourceSetupSeat(cwSession, CampaignActionAudience.Axis));
        Assert.True(CampaignExercises.IsSourceSetupSeat(cwSession, CampaignActionAudience.Commonwealth));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Axis));
        var placements = new[]
        {
            ("cw-dump-1", "d3612"),
            ("cw-alexandria-1", "e3613"), ("cw-alexandria-2", "e3613"), ("cw-alexandria-3", "e3714"),
            ("cw-cairo-1", "e1829"), ("cw-cairo-2", "e1830"), ("cw-cairo-3", "e1930"),
        };
        foreach (var (id, location) in placements)
        {
            var idHash = CampaignSourceAuthorityState.CommonwealthSupplyActionId(id, location);
            var cwDraft = CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Commonwealth);
            Assert.Contains(idHash, cwDraft.PrivateActionIds);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCommonwealthSupply(cwSession, Submission(cwSession, idHash, CampaignActionAudience.Axis)).RejectionReason);
            var step = CampaignExercises.PlaceSourceCommonwealthSupply(cwSession, Submission(cwSession, idHash, CampaignActionAudience.Commonwealth));
            Assert.True(step.IsAccepted);
            cwSession = step.SuccessorSession!;
            Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        }
        Assert.Empty(CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.Equal(15, cwSession.SourceAuthority!.CommonwealthSupplyWorld.Units.Count);
        Assert.Equal(18, cwSession.SourceAuthority.CommonwealthInitialForces.Forces.Count);
        Assert.Null(cwSession.SourceAuthority.CommonwealthInitialForces.Forces
            .Single(value => value.ElementId == "central-india-horse").InitialToes);
        Assert.Null(cwSession.SourceAuthority.Position);
        var axisDummy = CampaignSourceAuthorityState.DummyActionId(true, "axis-dummy-1", "c4407");
        Assert.Contains(axisDummy, CampaignExercises.QuerySourceDummyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, axisDummy, CampaignActionAudience.Commonwealth)).RejectionReason);
        var dummyStep = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, axisDummy, CampaignActionAudience.Axis));
        Assert.True(dummyStep.IsAccepted);
        cwSession = dummyStep.SuccessorSession!;
        var omit = CampaignSourceAuthorityState.DummyActionId(true, "axis-dummy-2", null);
        cwSession = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, omit, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwDummy = CampaignSourceAuthorityState.DummyActionId(false, "cw-dummy-1", "d3611");
        Assert.Contains(cwDummy, CampaignExercises.QuerySourceDummyDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        cwSession = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, cwDummy, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var priorRandom = cwSession.SourceAuthority!.RandomState;
        var expectedGt1 = SandtableRandom.RollD6(priorRandom);
        var rollGt1 = CampaignSourceAuthorityState.OpeningConvoyActionId("roll", 0, 0, 0);
        Assert.Single(CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, rollGt1, CampaignActionAudience.Commonwealth)).RejectionReason);
        var gt1Step = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, rollGt1, CampaignActionAudience.Axis));
        Assert.True(gt1Step.IsAccepted);
        cwSession = gt1Step.SuccessorSession!;
        Assert.Equal(expectedGt1.Value, cwSession.SourceAuthority!.OpeningConvoyPlan.Die);
        Assert.Equal(expectedGt1.State.NextByteCursor, cwSession.SourceAuthority.RandomState.NextByteCursor);
        if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply > 0)
        {
            // Real source lane 3 under disclosed provisional Benghazi ruling.
            // This branches the same rolled plan; it does not advance a game.
            var lane3Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 3, 1, 1);
            var lane3Session = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession,
                Submission(cwSession, lane3Add, CampaignActionAudience.Axis)).SuccessorSession!;
            var lane3Finish = CampaignSourceAuthorityState.OpeningConvoyActionId("finish", 0, 0, 0);
            lane3Session = CampaignExercises.SubmitSourceAxisGt1Supply(lane3Session,
                Submission(lane3Session, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
            var lane3Source = lane3Session.SourceAuthority!;
            var axisBefore = CampaignSourceGt1AxisArrival.Create(lane3Source);
            var benghazi = CampaignSourceGt1BenghaziArrival.Create(lane3Source, axisBefore);
            Assert.True(benghazi.CanAnnounce);
            var announced = benghazi.Announce(lane3Source, axisBefore);
            var (delivered, axisAfter) = announced.DeclineAttackAndDeliver(lane3Source, axisBefore);
            Assert.True(delivered.Delivered);
            Assert.Equal("b4827", Assert.Single(lane3Source.AxisSupplyWorld.Units,
                value => value.Id == "axis-benghazi-1").LocationId);
            Assert.Equal("a4827", Assert.Single(axisAfter.Supply.Units,
                value => value.Id == "axis-benghazi-1").LocationId);
            var landed = Assert.Single(axisAfter.Supply.Units, value => value.Id == "axis-gt1-op1-lane3-1");
            Assert.Equal("a4827", landed.LocationId);
            Assert.Equal(40, landed.Ammo);
            Assert.Equal(60, landed.Fuel);
            Assert.Equal(2, axisAfter.Supply.Units.Count(value => value.LocationId == "a4827"));
            Assert.False(axisAfter.HasOtherStageOneLanes);
            Assert.Throws<InvalidOperationException>(() => announced.DeclineAttackAndDeliver(lane3Source, axisAfter));
            var (replayedBenghazi, replayedAxis) = benghazi.Announce(lane3Source, axisBefore)
                .DeclineAttackAndDeliver(lane3Source, axisBefore);
            Assert.Equal(delivered.Hash(), replayedBenghazi.Hash());
            Assert.Equal(axisAfter.Hash(), replayedAxis.Hash());
            using var landedJson = JsonDocument.Parse(axisAfter.Supply.SerializeCanonical());
            Assert.Equal(2, landedJson.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Contains("rul-gra-05-provisional", landedJson.RootElement.GetProperty("benghaziCoordinateStatus").GetString()!);
            if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply >= 2)
            {
                var mixed = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession,
                    Submission(cwSession, lane3Add, CampaignActionAudience.Axis)).SuccessorSession!;
                var lane2Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 2, 1, 1);
                mixed = CampaignExercises.SubmitSourceAxisGt1Supply(mixed,
                    Submission(mixed, lane2Add, CampaignActionAudience.Axis)).SuccessorSession!;
                mixed = CampaignExercises.SubmitSourceAxisGt1Supply(mixed,
                    Submission(mixed, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
                var mixedSource = mixed.SourceAuthority!;
                var mixedAxis = CampaignSourceGt1AxisArrival.Create(mixedSource);
                Assert.False(mixedAxis.CanAnnounce);
                Assert.False(CampaignSourceGt1BenghaziArrival.Create(mixedSource, mixedAxis).CanAnnounce);
                Assert.Throws<InvalidOperationException>(() => mixedAxis.DeliverSoleBenghaziLane(mixedSource, 1));
                var multi = CampaignSourceGt1MultiArrival.Announce(mixedSource, mixedAxis);
                Assert.Collection(multi.Lanes, value => Assert.Equal(2, value), value => Assert.Equal(3, value));
                var (firstThree, axisThree) = multi.DeclineAndDeliver(mixedSource, mixedAxis, 3);
                var (threeThenTwo, axisThreeThenTwo) = firstThree.DeclineAndDeliver(mixedSource, axisThree, 2);
                var (firstTwo, axisTwo) = multi.DeclineAndDeliver(mixedSource, mixedAxis, 2);
                var (twoThenThree, axisTwoThenThree) = firstTwo.DeclineAndDeliver(mixedSource, axisTwo, 3);
                Assert.True(threeThenTwo.IsDelivered(2));
                Assert.True(threeThenTwo.IsDelivered(3));
                Assert.Equal(axisThreeThenTwo.Supply.Hash(), axisTwoThenThree.Supply.Hash());
                Assert.Equal(threeThenTwo.Hash(), twoThenThree.Hash());
                Assert.Equal("box.tripoli", Assert.Single(axisThreeThenTwo.Supply.Units,
                    value => value.Id == "axis-gt1-op1-lane2-1").LocationId);
                Assert.Equal("a4827", Assert.Single(axisThreeThenTwo.Supply.Units,
                    value => value.Id == "axis-gt1-op1-lane3-1").LocationId);
                Assert.Throws<InvalidOperationException>(() => threeThenTwo.DeclineAndDeliver(mixedSource, axisThreeThenTwo, 3));
                Assert.DoesNotContain(mixedSource.AxisSupplyWorld.Units, value => value.Id == "axis-gt1-op1-lane3-1");
                if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply >= 3)
                {
                    var triple = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession,
                        Submission(cwSession, lane3Add, CampaignActionAudience.Axis)).SuccessorSession!;
                    triple = CampaignExercises.SubmitSourceAxisGt1Supply(triple,
                        Submission(triple, lane2Add, CampaignActionAudience.Axis)).SuccessorSession!;
                    var lane6Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 6, 1, 1);
                    triple = CampaignExercises.SubmitSourceAxisGt1Supply(triple,
                        Submission(triple, lane6Add, CampaignActionAudience.Axis)).SuccessorSession!;
                    triple = CampaignExercises.SubmitSourceAxisGt1Supply(triple,
                        Submission(triple, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
                    var tripleSource = triple.SourceAuthority!;
                    var tripleAxis = CampaignSourceGt1AxisArrival.Create(tripleSource);
                    var tripleMulti = CampaignSourceGt1MultiArrival.Announce(tripleSource, tripleAxis);
                    var (tripleTwo, tripleAfterTwo) = tripleMulti.DeclineAndDeliver(tripleSource, tripleAxis, 2);
                    var (tripleThree, tripleAfterThree) = tripleTwo.DeclineAndDeliver(tripleSource, tripleAfterTwo, 3);
                    Assert.True(tripleThree.IsDue(6));
                    Assert.False(tripleThree.IsDelivered(6));
                    Assert.True(tripleThree.CanDecline(6));
                    var (tripleSix, tripleAfterSix) = tripleThree.DeclineAndDeliver(tripleSource, tripleAfterThree, 6);
                    Assert.True(tripleSix.Lane6Declined);
                    Assert.True(tripleSix.IsDelivered(6));
                    Assert.Equal("c4807", Assert.Single(tripleAfterSix.Supply.Units,
                        value => value.Id == "axis-gt1-op1-lane6-1").LocationId);
                    Assert.Throws<InvalidOperationException>(() => tripleSix.DeclineAndDeliver(tripleSource, tripleAfterSix, 6));
                    (bool CanDecline, bool AttackDeclined)? oneBefore = null, oneAfter = null, threeBefore = null, threeAfter = null;
                    for (var amount = 1; amount <= 3; amount++)
                    {
                        var tobruk = cwSession;
                        for (var n = 0; n < amount; n++)
                            tobruk = CampaignExercises.SubmitSourceAxisGt1Supply(tobruk,
                                Submission(tobruk, lane6Add, CampaignActionAudience.Axis)).SuccessorSession!;
                        tobruk = CampaignExercises.SubmitSourceAxisGt1Supply(tobruk,
                            Submission(tobruk, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
                        var tobrukSource = tobruk.SourceAuthority!;
                        var tobrukAxis = CampaignSourceGt1AxisArrival.Create(tobrukSource);
                        var tobrukAnnounced = CampaignSourceGt1MultiArrival.Announce(tobrukSource, tobrukAxis);
                        var (tobrukDeclined, tobrukAfter) = tobrukAnnounced.DeclineAndDeliver(tobrukSource, tobrukAxis, 6);
                        if (amount == 1) { oneBefore = tobrukAnnounced.Lane6CommonwealthCut(); oneAfter = tobrukDeclined.Lane6CommonwealthCut(); }
                        if (amount == 3) { threeBefore = tobrukAnnounced.Lane6CommonwealthCut(); threeAfter = tobrukDeclined.Lane6CommonwealthCut(); }
                        Assert.True(tobrukDeclined.Lane6Declined);
                        Assert.False(tobrukDeclined.CanDecline(6));
                        Assert.Equal(amount <= 2, tobrukDeclined.IsDelivered(6));
                        Assert.Equal(3 + (amount <= 2 ? amount : 0),
                            tobrukAfter.Supply.Units.Count(value => value.LocationId == "c4807"));
                        if (amount == 3) Assert.Equal(tobrukAxis.Supply.Hash(), tobrukAfter.Supply.Hash());
                        Assert.Equal(3, tobrukSource.AxisSupplyWorld.Units.Count(value => value.LocationId == "c4807"));
                    }
                    Assert.Equal(oneBefore, threeBefore);
                    Assert.Equal(oneAfter, threeAfter);
                }
            }
            var gt1Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 2, 1, 1);
            cwSession = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, gt1Add, CampaignActionAudience.Axis)).SuccessorSession!;
            Assert.Single(cwSession.SourceAuthority!.OpeningConvoyPlan.Allocations());
        }
        var gt1Finish = CampaignSourceAuthorityState.OpeningConvoyActionId("finish", 0, 0, 0);
        cwSession = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, gt1Finish, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(cwSession.SourceAuthority!.OpeningConvoyPlan.IsFinished);
        var pendingTripoli = CampaignSourceGt1AxisArrival.Create(cwSession.SourceAuthority!);
        var sourceSupplyHash = cwSession.SourceAuthority!.AxisSupplyWorld.Hash();
        if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply > 0)
        {
            Assert.True(pendingTripoli.CanAnnounce);
            var announcedTripoli = pendingTripoli.Announce();
            Assert.True(announcedTripoli.Announced);
            Assert.False(announcedTripoli.Delivered);
            var deliveredTripoli = announcedTripoli.DeclineAttackAndDeliver();
            var deliveredUnit = Assert.Single(deliveredTripoli.Supply.Units, unit => unit.Id == "axis-gt1-op1-lane2-1");
            Assert.Equal("box.tripoli", deliveredUnit.LocationId);
            Assert.Equal(40, deliveredUnit.Ammo);
            Assert.Equal(60, deliveredUnit.Fuel);
            Assert.Equal(sourceSupplyHash, cwSession.SourceAuthority.AxisSupplyWorld.Hash());
            Assert.NotEqual(pendingTripoli.Hash(), deliveredTripoli.Hash());
            Assert.Throws<InvalidOperationException>(() => deliveredTripoli.DeclineAttackAndDeliver());
        }
        else
        {
            Assert.False(pendingTripoli.CanAnnounce);
            Assert.Throws<InvalidOperationException>(() => pendingTripoli.Announce());
        }

        Assert.Empty(CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var skipCwGt1 = CampaignSourceAuthorityState.CommonwealthSupplyPlanActionId("skip", 1);
        var cwPlanning = CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Commonwealth);
        Assert.Contains(skipCwGt1, cwPlanning.PrivateActionIds);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Axis));
        var beforeSkip = cwSession.SourceAuthority.RandomState.NextByteCursor;
        cwSession = CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession,
            Submission(cwSession, skipCwGt1, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(beforeSkip, cwSession.SourceAuthority!.RandomState.NextByteCursor);
        Assert.Equal("skipped-opening", cwSession.SourceAuthority.CommonwealthSupplyPlans!.Entries[0].Status);
        foreach (var turn in new[] { 2, 3, 4 })
        {
            var prior = cwSession.SourceAuthority.RandomState;
            var expected = SandtableRandom.RollD6(prior);
            var action = CampaignSourceAuthorityState.CommonwealthSupplyPlanActionId("roll", turn);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession, Submission(cwSession, action, CampaignActionAudience.Axis)).RejectionReason);
            cwSession = CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession,
                Submission(cwSession, action, CampaignActionAudience.Commonwealth)).SuccessorSession!;
            var entry = cwSession.SourceAuthority!.CommonwealthSupplyPlans!.Entries.Single(value => value.TargetGameTurn == turn);
            Assert.Equal(expected.Value, entry.Die);
            Assert.Equal((expected.Value + 1) / 2, entry.SupplyUnits);
            Assert.Equal(expected.State.NextByteCursor, cwSession.SourceAuthority.RandomState.NextByteCursor);
        }
        Assert.Empty(CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var gt1Plan = CampaignSourceGt1SupplyPlanning.Create(cwSession.SourceAuthority!, cwSession.SourceAuthority!.RandomState);
        var gt5 = CampaignSourceGt1CommonwealthSupplyPlanning.Create();
        var expectedGt5 = SandtableRandom.RollD6(gt1Plan.RandomState);
        var rolledGt5 = gt5.Roll(gt1Plan.RandomState);
        Assert.Equal(expectedGt5.Value, rolledGt5.Plan.Die);
        Assert.Equal((expectedGt5.Value + 1) / 2, rolledGt5.Plan.Units);
        Assert.Equal(expectedGt5.State, rolledGt5.Random);
        Assert.Throws<InvalidOperationException>(() => rolledGt5.Plan.Roll(rolledGt5.Random));
        Assert.Equal(expectedGt5.State, gt1Plan.WithRandomState(rolledGt5.Random).RandomState);
        var expectedGt3 = SandtableRandom.RollD6(gt1Plan.RandomState);
        var rolledGt3 = gt1Plan.Roll();
        Assert.Equal(expectedGt3.Value, rolledGt3.Die);
        Assert.Equal(expectedGt3.Value switch { 1 => 0, <= 3 => 1, <= 5 => 2, _ => 3 }, rolledGt3.Maximum);
        Assert.Equal(expectedGt3.State, rolledGt3.RandomState);
        Assert.Throws<InvalidOperationException>(() => rolledGt3.Roll());
        Assert.Empty(rolledGt3.ValidDeltas(1, 1));
        Assert.Empty(rolledGt3.ValidDeltas(2, 4));
        if (rolledGt3.Maximum > 0)
        {
            var assigned = rolledGt3.Adjust(2, 1, 1);
            Assert.Equal(1, assigned.QuantityAt(2, 1));
            Assert.Equal(rolledGt3.RandomState, assigned.RandomState);
            Assert.Equal(0, assigned.Adjust(2, 1, -1).QuantityAt(2, 1));
            for (var unit = 1; unit < rolledGt3.Maximum; unit++) assigned = assigned.Adjust(2, 1, 1);
            Assert.DoesNotContain(1, assigned.ValidDeltas(2, 1));
            Assert.DoesNotContain(1, assigned.ValidDeltas(3, 2));
            Assert.Throws<InvalidOperationException>(() => assigned.Adjust(3, 2, 1));
        }
        var finishedGt3 = rolledGt3.Finish();
        Assert.True(finishedGt3.Finished);
        Assert.Empty(finishedGt3.ValidDeltas(2, 1));
        // Each owner's roll may happen first; the shared stream must advance
        // once per roll without rewriting an already-finished Axis plan.
        var axisFirstPlan = gt1Plan.Roll().Finish();
        var axisFirstHash = axisFirstPlan.Hash();
        var cwAfterAxis = CampaignSourceGt1CommonwealthSupplyPlanning.Create().Roll(axisFirstPlan.RandomState);
        Assert.Equal(axisFirstHash, axisFirstPlan.Hash());
        var cwRollFirst = CampaignSourceGt1CommonwealthSupplyPlanning.Create().Roll(gt1Plan.RandomState);
        var axisAfterCw = gt1Plan.WithRandomState(cwRollFirst.Random).Roll().Finish();
        Assert.Equal(axisFirstPlan.Die, cwRollFirst.Plan.Die);
        Assert.Equal(cwAfterAxis.Plan.Die, axisAfterCw.Die);
        Assert.Equal(cwAfterAxis.Random, axisAfterCw.RandomState);
        Assert.Throws<InvalidOperationException>(() => axisAfterCw.Roll());
        Assert.Throws<InvalidOperationException>(() => cwAfterAxis.Plan.Roll(cwAfterAxis.Random));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        // Exercise the real prestart September plans on an independent branch of the retained source session.
        var september = cwSession;
        var beginSeptember = CampaignSourceAuthorityState.SeptemberSupplyActionId("begin", 0, 0, 0, 0);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceSeptemberSupplyDraft(september, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, beginSeptember, CampaignActionAudience.Commonwealth)).RejectionReason);
        var normalSeptember = CampaignExercises.Query(september, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "axis-september-supply");
        Assert.DoesNotContain(CampaignExercises.Query(september, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "axis-september-supply");
        var beginCommand = Submission(september, normalSeptember.ActionId, CampaignActionAudience.Axis);
        september = CampaignExercises.Submit(september, beginCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(september, beginCommand).RejectionReason);
        foreach (var turn in new[] { 3, 4 })
        {
            var expected = SandtableRandom.RollD6(september.SourceAuthority!.RandomState);
            var septemberRoll = CampaignSourceAuthorityState.SeptemberSupplyActionId("roll", turn, 0, 0, 0);
            september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, septemberRoll, CampaignActionAudience.Axis)).SuccessorSession!;
            var entry = september.SourceAuthority!.SeptemberPlans!.Entries.Single(value => value.TargetGameTurn == turn);
            Assert.Equal(expected.Value, entry.Die);
            Assert.Equal(expected.Value switch { 1 => 0, <= 3 => 1, <= 5 => 2, _ => 3 }, entry.Maximum);
            Assert.Equal(expected.State.NextByteCursor, september.SourceAuthority.RandomState.NextByteCursor);
            var finish = CampaignSourceAuthorityState.SeptemberSupplyActionId("finish", turn, 0, 0, 0);
            if (entry.Maximum > 0)
            {
                var zeroPlan = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, finish, CampaignActionAudience.Axis)).SuccessorSession!;
                Assert.True(zeroPlan.SourceAuthority!.SeptemberPlans!.Entries.Single(value => value.TargetGameTurn == turn).Finished);
                Assert.Equal(0, zeroPlan.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == turn).Allocations.Sum());
                var add = CampaignSourceAuthorityState.SeptemberSupplyActionId("adjust", turn, 2, 1, 1);
                september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, add, CampaignActionAudience.Axis)).SuccessorSession!;
            }
            september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, finish, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        Assert.Null(september.SourceAuthority!.SeptemberPlans!.NextTargetGameTurn);
        Assert.Equal(38, september.SourceAuthority.ProfileVersion);
        var retainedGt3 = CampaignSourceGt1SupplyPlanning.Create(september.SourceAuthority, september.SourceAuthority.RandomState);
        Assert.Equal(september.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == 3).Die, retainedGt3.Die);
        Assert.True(retainedGt3.CarriedPlanFinished);
        Assert.Equal(september.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == 3).Allocations[0], retainedGt3.QuantityAt(2, 1));
        Assert.Throws<InvalidOperationException>(() => retainedGt3.Roll());
        var septemberHash = september.SourceAuthority.SeptemberPlans.Hash();
        september = CampaignExercises.AdmitSourceAxisSupplement(september).SuccessorSession!;
        Assert.Equal(septemberHash, september.SourceAuthority!.SeptemberPlans!.Hash());
        Assert.True(CampaignExercises.Reconstruct(september).IsVerified);
        Assert.False(CampaignExercises.IsSourceAxisSupplementAdmitted(cwSession));
        var migrated = CampaignExercises.AdmitSourceAxisSupplement(cwSession);
        Assert.True(migrated.IsAccepted);
        cwSession = migrated.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceAxisSupplementAdmitted(cwSession));
        Assert.Equal(5, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceAxisSupplement(cwSession).RejectionReason);
        var replacements = CampaignSourceAuthorityState.AutoblindaActionId("replacements", "");
        Assert.Contains(replacements, CampaignExercises.QuerySourceAutoblindaDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, replacements, CampaignActionAudience.Commonwealth)).RejectionReason);
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, replacements, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, cwSession.SourceAuthority!.Autoblinda!.ReplacementPoints);
        var reset = CampaignSourceAuthorityState.AutoblindaActionId("reset", "");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, reset, CampaignActionAudience.Axis)).SuccessorSession!;
        var attach1 = CampaignSourceAuthorityState.AutoblindaActionId("attach", "1-141-infantry");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach1, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach1, CampaignActionAudience.Axis)).RejectionReason);
        var attach2 = CampaignSourceAuthorityState.AutoblindaActionId("attach", "2-141-infantry");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach2, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(cwSession.SourceAuthority!.Autoblinda!.IsComplete);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.False(CampaignExercises.IsSourceCommonwealthSupplementAdmitted(cwSession));
        var cwMigrated = CampaignExercises.AdmitSourceCommonwealthSupplement(cwSession);
        Assert.True(cwMigrated.IsAccepted);
        cwSession = cwMigrated.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCommonwealthSupplementAdmitted(cwSession));
        Assert.Equal(6, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(12, cwSession.SourceAuthority!.CommonwealthSupplement!.Forces.Count);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceCommonwealthSupplement(cwSession).RejectionReason);
        Assert.False(CampaignExercises.IsSourcePriorityMotorizationAdmitted(cwSession));
        var priorityMigration = CampaignExercises.AdmitSourcePriorityMotorization(cwSession);
        Assert.True(priorityMigration.IsAccepted);
        cwSession = priorityMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourcePriorityMotorizationAdmitted(cwSession));
        Assert.Equal(7, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourcePriorityMotorizationDraft(cwSession, CampaignActionAudience.Axis));
        var priorityId = CampaignSourceAuthorityState.PriorityMotorizationActionId("1-kings-royal-rifle-corps", 6);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourcePriorityMotorization(cwSession, Submission(cwSession, priorityId, CampaignActionAudience.Axis)).RejectionReason);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, priorityId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(14, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3922-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        var reversePriority = CampaignSourceAuthorityState.PriorityMotorizationActionId("1-kings-royal-rifle-corps", -6);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, reversePriority, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(20, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3922-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        var secondPriority = CampaignSourceAuthorityState.PriorityMotorizationActionId("2-rifle-brigade", 6);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, secondPriority, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(4, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3320-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var axisMigration = CampaignExercises.AdmitSourceAxisPriorityMotorization(cwSession);
        Assert.True(axisMigration.IsAccepted);
        cwSession = axisMigration.SuccessorSession!;
        Assert.Equal(8, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var targetAxis = "1-1-libyan-motor-infantry";
        var oldDetach = CampaignSourceAuthorityState.AxisPriorityActionId("detach", targetAxis, 0);
        var historical = CampaignExercises.ReplaySourceAxisPriority(cwSession,
            Submission(cwSession, oldDetach, CampaignActionAudience.Axis));
        Assert.True(historical.IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(historical.SuccessorSession!).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, oldDetach, CampaignActionAudience.Axis)).RejectionReason);
        var prematureLift = CampaignSourceAuthorityState.AxisPriorityActionId("adjust", targetAxis, 5);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, prematureLift, CampaignActionAudience.Axis)).RejectionReason);
        foreach (var axisTarget in CampaignSourceAxisPriorityMotorization.Targets)
        {
            var detach = CampaignSourceAuthorityState.AxisPriorityActionId("detach", axisTarget, 0);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, detach, CampaignActionAudience.Axis)).RejectionReason);
        }
        Assert.Equal(35, cwSession.SourceAuthority!.AxisPriorityMotorization!.Remaining(cwSession.SourceAuthority.AxisMotorization));
        var undoDetachment = CampaignSourceAuthorityState.AxisPriorityActionId("undo-detachment", targetAxis, 0);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, undoDetachment, CampaignActionAudience.Axis)).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var garrisonMigration = CampaignExercises.AdmitSourceAxisMinorGarrisons(cwSession);
        Assert.True(garrisonMigration.IsAccepted);
        cwSession = garrisonMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceAxisMinorGarrisonsAdmitted(cwSession));
        Assert.Equal(9, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(13, cwSession.SourceAuthority!.AxisGarrisons!.Forces.Count);
        Assert.Contains(cwSession.SourceAuthority.AxisGarrisons.Forces,
            value => value.ElementId == "giarabub-motor-machine-gun");
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceAxisMinorGarrisons(cwSession).RejectionReason);
        var battlefieldMigration = CampaignExercises.AdmitSourceBattlefield(cwSession);
        Assert.True(battlefieldMigration.IsAccepted);
        cwSession = battlefieldMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceBattlefieldAdmitted(cwSession));
        Assert.Equal(10, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var battlefield = cwSession.SourceAuthority!.Battlefield!;
        Assert.Equal(battlefield.Entries.Count, battlefield.Entries.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(2, battlefield.Pending.Count);
        Assert.Equal(8, battlefield.Pending.Single(value => value.GroupId == Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId).ElementIds.Count);
        Assert.Equal(4, battlefield.Pending.Single(value => value.GroupId == Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId).ElementIds.Count);
        using var battlefieldBytes = JsonDocument.Parse(battlefield.SerializeCanonical());
        Assert.False(battlefieldBytes.RootElement.GetProperty("battlefieldComplete").GetBoolean());
        Assert.False(battlefieldBytes.RootElement.GetProperty("controlCertified").GetBoolean());
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceBattlefield(cwSession).RejectionReason);
        var cairoAdmission = CampaignExercises.AdmitSourceCairoSetup(cwSession);
        Assert.True(cairoAdmission.IsAccepted);
        cwSession = cairoAdmission.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCairoSetupAdmitted(cwSession));
        Assert.Equal(11, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(4, cwSession.SourceAuthority!.CairoSetup!.PendingIds.Count);
        Assert.Equal(4, cwSession.SourceAuthority.Battlefield!.Entries.Count(x => x.AuthorityStatus == "source-setup-unplaced"));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCairoSetupDraft(cwSession, CampaignActionAudience.Axis));
        var invalidCity = CampaignSourceAuthorityState.CairoPlacementActionId("1-welch", "e1829");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, invalidCity, CampaignActionAudience.Commonwealth)).RejectionReason);
        var inferredCity = CampaignSourceAuthorityState.CairoPlacementActionId("1-welch", "e1931");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, inferredCity, CampaignActionAudience.Commonwealth)).RejectionReason);
        var chosenCairoHex = seed == 14 ? "e1830" : "e1730";
        foreach (var element in CampaignSourceCairoInitialSetup.Units.Select(value => value.ElementId))
        {
            var action = CampaignSourceAuthorityState.CairoPlacementActionId(element, chosenCairoHex);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, action, CampaignActionAudience.Axis)).RejectionReason);
            var command = Submission(cwSession, action, CampaignActionAudience.Commonwealth);
            var placedCairo = CampaignExercises.PlaceSourceCairoUnit(cwSession, command);
            Assert.True(placedCairo.IsAccepted);
            cwSession = placedCairo.SuccessorSession!;
            Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
                CampaignExercises.PlaceSourceCairoUnit(cwSession, command).RejectionReason);
        }
        Assert.Empty(cwSession.SourceAuthority!.CairoSetup!.PendingIds);
        Assert.Equal(4, cwSession.SourceAuthority.Battlefield!.Entries.Count(x => x.LocationId == chosenCairoHex && x.AuthorityStatus == "source-setup-placed"));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var activeMigration = CampaignExercises.AdmitSourceCairoWorld(cwSession);
        Assert.True(activeMigration.IsAccepted);
        cwSession = activeMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCairoWorldAdmitted(cwSession));
        Assert.Equal(12, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var active = cwSession.SourceAuthority!.CairoWorld!;
        Assert.Equal("sha256:3a7fddd510854c81b408e07742dd837f7be416206117ca58ea79357357a11ddd", active.Content.Hash());
        Assert.NotEqual(cwSession.SourceAuthority.Content.Identity.Hash, active.Content.Identity.Hash);
        Assert.Equal(active.Content.Identity, active.World.ContentIdentity);
        Assert.Equal(8, active.World.World.Elements.Count);
        Assert.Equal(8, active.World.World.Representations.Count);
        Assert.Equal(2, active.World.PendingArrivals.Count);
        foreach (var unit in CampaignSourceCairoInitialSetup.Units)
        {
            var element = Assert.Single(active.World.World.Elements, value => value.ElementId == unit.ElementId);
            Assert.Equal(chosenCairoHex, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
        }
        var alteredContentText = System.Text.Encoding.UTF8.GetString(active.Content.CanonicalBytes)
            .Replace("\"initialToe\":6", "\"initialToe\":7", StringComparison.Ordinal);
        var alteredContent = ContentSourceSetupPackArtifact.Read(System.Text.Encoding.UTF8.GetBytes(alteredContentText));
        Assert.NotEqual(active.Content.Hash(), alteredContent.Hash());
        var forgedHistory = cwSession.PregameHistory.Select(value => value.ToArray()).ToArray();
        var forgedEvent = System.Text.Encoding.UTF8.GetString(forgedHistory[^1])
            .Replace(active.Content.Hash(), alteredContent.Hash(), StringComparison.Ordinal);
        forgedHistory[^1] = System.Text.Encoding.UTF8.GetBytes(forgedEvent);
        var forgedSession = new ExerciseSession(cwSession.Pregame!, cwSession.CurrentContacts, forgedHistory, cwSession.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(forgedSession).IsVerified);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceCairoWorld(cwSession).RejectionReason);
        var directV12 = cwSession;
        Assert.True(CampaignExercises.CanPromoteSourceIndependent(directV12));
        var lockBeforePromotion = CampaignExercises.Query(directV12, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var lockedBeforePromotion = CampaignExercises.Submit(directV12,
            Submission(directV12, lockBeforePromotion.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.PromoteSourceIndependent(lockedBeforePromotion).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.ReplaySourceIndependentPromotion(lockedBeforePromotion).RejectionReason);
        directV12 = CampaignExercises.PromoteSourceIndependent(directV12).SuccessorSession!;
        Assert.Equal(14, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(10, directV12.SourceAuthority!.World.World.Elements.Count);
        var genericIdentity = directV12.SourceAuthority.World.ContentIdentity;
        Assert.False(CampaignExercises.CanPromoteSourceIndependent(directV12));
        directV12 = CampaignExercises.AdmitSourceLocalMap(directV12).SuccessorSession!;
        Assert.Equal(15, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var mapIdentity = directV12.SourceAuthority!.World.ContentIdentity;
        Assert.NotEqual(genericIdentity, mapIdentity);
        Assert.Equal(10, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.PromoteSourceExpandedIndependent(directV12).SuccessorSession!;
        Assert.Equal(16, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(13, directV12.SourceAuthority!.World.World.Elements.Count);
        var expandedIdentity = directV12.SourceAuthority.World.ContentIdentity;
        Assert.False(CampaignExercises.CanPromoteSourceExpandedIndependent(directV12));
        Assert.False(CampaignExercises.CanPromoteSourceMaletti(directV12));
        Assert.Equal(expandedIdentity, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(mapIdentity, directV12.SourceAuthority.LocalMapWorld!.Content.Identity);
        Assert.Equal(genericIdentity, directV12.SourceAuthority.IndependentWorld!.Content.Identity);
        Assert.Equal(13, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.AdmitSourceAttachedInitialLoads(directV12).SuccessorSession!;
        Assert.Equal(17, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        const string loadedTarget = "2-rifle-brigade";
        var attach = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("attach", loadedTarget);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, attach, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, attach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, directV12.SourceAuthority!.AttachedInitialLoads!.Reserved("cw-c3320-attached"));
        var fuel = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("fuel", loadedTarget);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, fuel, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, Assert.Single(directV12.SourceAuthority!.AttachedInitialLoads!.Targets).FuelPoints);
        var detachLoad = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("detach", loadedTarget);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, detachLoad, CampaignActionAudience.Commonwealth)).RejectionReason);
        var removeLift = CampaignSourceAuthorityState.PriorityMotorizationActionId(loadedTarget, -6);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourcePriorityMotorization(directV12, Submission(directV12, removeLift, CampaignActionAudience.Commonwealth)).RejectionReason);
        var clear = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("unload-fuel", loadedTarget);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, clear, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, detachLoad, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Empty(directV12.SourceAuthority!.AttachedInitialLoads!.Targets);
        directV12 = CampaignExercises.AdmitSourceInitialWrecks(directV12).SuccessorSession!;
        Assert.Equal(18, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(3, directV12.SourceAuthority!.InitialWrecks!.PendingIds.Count);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceInitialWrecksDraft(directV12, CampaignActionAudience.Axis));
        foreach (var id in directV12.SourceAuthority.InitialWrecks.PendingIds.ToArray())
        {
            var place = CampaignSourceAuthorityState.InitialWreckPlacementActionId(id, "e3613");
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceInitialWreck(directV12, Submission(directV12, place, CampaignActionAudience.Axis)).RejectionReason);
            directV12 = CampaignExercises.PlaceSourceInitialWreck(directV12, Submission(directV12, place, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        }
        Assert.Empty(directV12.SourceAuthority!.InitialWrecks!.PendingIds);
        Assert.Equal(3, directV12.SourceAuthority.InitialWrecks.Placements.Count);
        directV12 = CampaignExercises.AdmitSourceAxisFreeSetup(directV12).SuccessorSession!;
        Assert.Equal(19, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var forgedLegacyState = directV12.SourceAuthority!.ApplyAxisPriorityChoice("detach", targetAxis, 0);
        var forgedLegacyEvent = directV12.SourceAuthority.SerializeAxisPriorityEvent(forgedLegacyState, "detach", targetAxis, 0);
        var forgedLegacySession = new ExerciseSession(directV12.Pregame!, directV12.CurrentContacts.AdvancePregameSource(directV12.SourceAuthority.StateVersion),
            [.. directV12.PregameHistory, forgedLegacyEvent], forgedLegacyState);
        Assert.False(CampaignExercises.Reconstruct(forgedLegacySession).IsVerified);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisFreeSetupDraft(directV12, CampaignActionAudience.Commonwealth));
        var freeIds = directV12.SourceAuthority!.AxisFreeArtillery!.PendingIds.ToArray();
        for (var i = 0; i < freeIds.Length; i++)
        {
            var location = i < 2 ? "c4407" : "c4607";
            var action = CampaignSourceAuthorityState.AxisFreePlacementActionId(freeIds[i], location);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, action, CampaignActionAudience.Commonwealth)).RejectionReason);
            if (i == 2)
            {
                var overstack = CampaignSourceAuthorityState.AxisFreePlacementActionId(freeIds[i], "c4407");
                Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                    CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, overstack, CampaignActionAudience.Axis)).RejectionReason);
            }
            directV12 = CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, action, CampaignActionAudience.Axis)).SuccessorSession!;
            if (i == 0)
            {
                directV12 = CampaignExercises.PromoteSourceAxisFreeWorld(directV12).SuccessorSession!;
                Assert.Equal(20, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
                Assert.Equal(14, directV12.SourceAuthority!.World.World.Elements.Count);
                var identityAfterPartial = directV12.SourceAuthority.World.ContentIdentity;
                var adjustAxis = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", 1);
                directV12 = CampaignExercises.AdjustSourceMotorization(directV12,
                    Submission(directV12, adjustAxis, CampaignActionAudience.Axis)).SuccessorSession!;
                directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12,
                    Submission(directV12, attach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
                Assert.Equal(identityAfterPartial, directV12.SourceAuthority!.World.ContentIdentity);
                Assert.Equal(14, directV12.SourceAuthority.World.World.Elements.Count);
                Assert.False(CampaignExercises.CanPromoteSourceIndependent(directV12));
                Assert.False(CampaignExercises.CanPromoteSourceExpandedIndependent(directV12));
            }
        }
        Assert.Empty(directV12.SourceAuthority!.AxisFreeArtillery!.PendingIds);
        Assert.Equal(3, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-1ar-placed"));
        var axisContent = directV12.SourceAuthority.World.ContentIdentity;
        directV12 = CampaignExercises.PromoteSourceAxisFreeWorld(directV12).SuccessorSession!;
        Assert.Equal(axisContent, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(17, directV12.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Equal(4, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-1ar-active"));
        Assert.False(CampaignExercises.CanPromoteSourceAxisFreeWorld(directV12));
        directV12 = CampaignExercises.AdmitSourceAxisCorpsSetup(directV12).SuccessorSession!;
        Assert.Equal(21, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisCorpsSetupDraft(directV12, CampaignActionAudience.Commonwealth));
        var corpsIds = directV12.SourceAuthority!.AxisCorpsArtillery!.PendingIds.ToArray();
        for (var i = 0; i < corpsIds.Length; i++)
        {
            var location = i == 0 ? "c4307" : "c4308";
            var action = CampaignSourceAuthorityState.AxisCorpsPlacementActionId(corpsIds[i], location);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, action, CampaignActionAudience.Commonwealth)).RejectionReason);
            if (i == 1)
            {
                var occupied = CampaignSourceAuthorityState.AxisCorpsPlacementActionId(corpsIds[i], "c4307");
                Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                    CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, occupied, CampaignActionAudience.Axis)).RejectionReason);
            }
            directV12 = CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, action, CampaignActionAudience.Axis)).SuccessorSession!;
            directV12 = CampaignExercises.PromoteSourceAxisCorpsWorld(directV12).SuccessorSession!;
            Assert.Equal(22, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        }
        Assert.Equal(19, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Equal(2, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-corps-artillery-active"));
        foreach (var id in corpsIds)
            Assert.Equal(3, directV12.SourceAuthority.World.World.Elements.Single(value => value.ElementId == id).Components.Single(value => value.ComponentId.EndsWith(".light-aa", StringComparison.Ordinal)).CurrentToe);
        Assert.False(CampaignExercises.CanPromoteSourceAxisCorpsWorld(directV12));
        var corpsContent = directV12.SourceAuthority.World.ContentIdentity;
        var removeAxisPoint = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", -1);
        directV12 = CampaignExercises.AdjustSourceMotorization(directV12,
            Submission(directV12, removeAxisPoint, CampaignActionAudience.Axis)).SuccessorSession!;
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12,
            Submission(directV12, fuel, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(corpsContent, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(19, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.AdmitSourceMalettiHierarchy(directV12).SuccessorSession!;
        Assert.Equal(23, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var confirmHierarchy = CampaignSourceAuthorityState.MalettiHierarchyActionId("confirm", null);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMalettiHierarchy(directV12, Submission(directV12, confirmHierarchy, CampaignActionAudience.Commonwealth)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceMalettiHierarchy(directV12,
            Submission(directV12, confirmHierarchy, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwFirst = directV12;
        var frenchFirst = directV12;
        var certifiedDetach = CampaignSourceAuthorityState.MalettiHierarchyActionId("detach", targetAxis);
        directV12 = CampaignExercises.SubmitSourceMalettiHierarchy(directV12,
            Submission(directV12, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(targetAxis, directV12.SourceAuthority!.MalettiHierarchy!.DetachedId);
        var otherDetach = CampaignSourceAuthorityState.MalettiHierarchyActionId("detach", "3-1-libyan-motor-infantry");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMalettiHierarchy(directV12, Submission(directV12, otherDetach, CampaignActionAudience.Axis)).RejectionReason);
        Assert.True(CampaignExercises.CanPromoteSourceCertifiedMalettiWorld(directV12));
        directV12 = CampaignExercises.PromoteSourceCertifiedMalettiWorld(directV12).SuccessorSession!;
        Assert.Equal(24, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(20, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Contains(directV12.SourceAuthority.Battlefield!.Entries,
            value => value.ElementId == targetAxis && value.AuthorityStatus == "source-certified-maletti-active");
        Assert.False(CampaignExercises.CanPromoteSourceCertifiedMalettiWorld(directV12));
        var axisLift = CampaignSourceAuthorityState.AxisPriorityActionId("adjust", targetAxis, 5);
        var lockBeforeLift = CampaignExercises.Query(directV12, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var lockedBeforeLift = CampaignExercises.Submit(directV12,
            Submission(directV12, lockBeforeLift.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(lockedBeforeLift,
                Submission(lockedBeforeLift, axisLift, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceAxisPriority(directV12,
            Submission(directV12, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(5, directV12.SourceAuthority!.AxisPriorityMotorization!.AssignedPoints(targetAxis));
        directV12 = CampaignExercises.AdmitSourceMatruhFormation(directV12).SuccessorSession!;
        Assert.Equal(25, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceMatruhFormationDraft(directV12, CampaignActionAudience.Axis));
        var matruhConfirm = CampaignSourceAuthorityState.MatruhFormationActionId("confirm", null);
        directV12 = CampaignExercises.SubmitSourceMatruhFormation(directV12,
            Submission(directV12, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(5, directV12.SourceAuthority!.MatruhFormation!.AttachedIds.Count);
        var matruhDetach = CampaignSourceAuthorityState.MatruhFormationActionId("detach", "1-essex");
        directV12 = CampaignExercises.SubmitSourceMatruhFormation(directV12,
            Submission(directV12, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(4, directV12.SourceAuthority!.MatruhFormation!.AttachedIds.Count);
        Assert.Contains(directV12.SourceAuthority.Battlefield!.Entries,
            entry => entry.ElementId == "1-essex" && entry.AuthorityStatus == "source-matruh-detached");
        var secondMatruhDetach = CampaignSourceAuthorityState.MatruhFormationActionId("detach", "1-durham-light-infantry");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMatruhFormation(directV12, Submission(directV12, secondMatruhDetach, CampaignActionAudience.Commonwealth)).RejectionReason);
        directV12 = CampaignExercises.PromoteSourceMatruhDetachedWorld(directV12).SuccessorSession!;
        Assert.Equal(26, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(21, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(4, directV12.SourceAuthority.MatruhFormation!.AttachedIds.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        cwFirst = CampaignExercises.AdmitSourceMatruhFormation(cwFirst).SuccessorSession!;
        cwFirst = CampaignExercises.SubmitSourceMatruhFormation(cwFirst,
            Submission(cwFirst, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        cwFirst = CampaignExercises.SubmitSourceMatruhFormation(cwFirst,
            Submission(cwFirst, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        cwFirst = CampaignExercises.PromoteSourceMatruhDetachedWorld(cwFirst).SuccessorSession!;
        var cwFirstIdentity = cwFirst.SourceAuthority!.World.ContentIdentity;
        cwFirst = CampaignExercises.SubmitSourceMalettiHierarchy(cwFirst,
            Submission(cwFirst, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        cwFirst = CampaignExercises.PromoteSourceCertifiedMalettiWorld(cwFirst).SuccessorSession!;
        Assert.NotEqual(cwFirstIdentity, cwFirst.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(21, cwFirst.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, cwFirst.SourceAuthority.World.PendingArrivals.Count);
        Assert.True(CampaignExercises.Reconstruct(cwFirst).IsVerified);
        var rngBeforeGt2 = directV12.SourceAuthority!.RandomState;
        var worldBeforeGt2 = directV12.SourceAuthority.World.ContentIdentity;
        directV12 = CampaignExercises.AdmitSourceGt2Correction(directV12).SuccessorSession!;
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var finishGt2 = CampaignSourceAuthorityState.Gt2CorrectionActionId("finish", 0, 0, 0);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Commonwealth)).RejectionReason);
        if (maximum > 0)
        {
            foreach (var lane in new[] { 2, 3, 6 })
                for (var stage = 1; stage <= 3; stage++)
                    while (directV12.SourceAuthority!.Gt2Correction!.Quantity(lane, stage) > 0)
                    {
                        var removeUnit = CampaignSourceAuthorityState.Gt2CorrectionActionId("adjust", lane, stage, -1);
                        directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12,
                            Submission(directV12, removeUnit, CampaignActionAudience.Axis)).SuccessorSession!;
                    }
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Axis)).RejectionReason);
            var addGt2 = CampaignSourceAuthorityState.Gt2CorrectionActionId("adjust", 2, 1, 1);
            directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, addGt2, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(directV12.SourceAuthority!.Gt2Correction!.IsFinished);
        Assert.Equal(rngBeforeGt2, directV12.SourceAuthority.RandomState);
        Assert.Equal(worldBeforeGt2, directV12.SourceAuthority.World.ContentIdentity);
        directV12 = CampaignExercises.AdmitSourceCwPriorityCatalog(directV12).SuccessorSession!;
        Assert.Equal(28, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        directV12 = CampaignExercises.PromoteSourceCwPriorityWorld(directV12).SuccessorSession!;
        Assert.Equal(29, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(22, directV12.SourceAuthority!.World.World.Elements.Count);
        var frenchAdd = CampaignSourceAuthorityState.CwPriorityCatalogActionId("1-french-motor-marines", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceCwPriorityCatalog(directV12, Submission(directV12, frenchAdd, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceCwPriorityCatalog(directV12,
            Submission(directV12, frenchAdd, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, directV12.SourceAuthority!.CwPriorityCatalog!.FrenchPoints);
        Assert.Equal(1, directV12.SourceAuthority.CwPriorityCatalog.Remaining(directV12.SourceAuthority, "cw-c3926-attached"));
        var frenchRemove = CampaignSourceAuthorityState.CwPriorityCatalogActionId("1-french-motor-marines", -1);
        directV12 = CampaignExercises.SubmitSourceCwPriorityCatalog(directV12,
            Submission(directV12, frenchRemove, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(2, directV12.SourceAuthority!.CwPriorityCatalog!.Remaining(directV12.SourceAuthority, "cw-c3926-attached"));
        frenchFirst = CampaignExercises.AdmitSourceCwPriorityCatalog(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.PromoteSourceCwPriorityWorld(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.AdmitSourceCwFreeGuns(frenchFirst).SuccessorSession!;
        Assert.Equal(31, frenchFirst.SourceAuthority!.ProfileVersion);
        var freeGunIds = frenchFirst.SourceAuthority!.CwFreeGuns!.PendingIds.ToArray();
        var gunCairoHex = seed == 14 ? "e1730" : "e1830";
        foreach (var id in freeGunIds)
        {
            var placeGun = CampaignSourceAuthorityState.CwFreeGunPlacementActionId(id, gunCairoHex);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCwFreeGun(frenchFirst, Submission(frenchFirst, placeGun, CampaignActionAudience.Axis)).RejectionReason);
            frenchFirst = CampaignExercises.PlaceSourceCwFreeGun(frenchFirst,
                Submission(frenchFirst, placeGun, CampaignActionAudience.Commonwealth)).SuccessorSession!;
            frenchFirst = CampaignExercises.PromoteSourceCwFreeGunsWorld(frenchFirst).SuccessorSession!;
        }
        Assert.Equal(32, frenchFirst.SourceAuthority!.ProfileVersion);
        _ = ContentSourceFourthIndianPackArtifact.Create(frenchFirst.SourceAuthority!.CwFreeGunsWorld!.Content);
        var indianResult = CampaignExercises.AdmitSourceFourthIndianWorld(frenchFirst);
        Assert.True(indianResult.IsAccepted);
        var indian = indianResult.SuccessorSession!;
        var grouped = indian.SourceAuthority!.GroupedWorld!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(indian.SourceAuthority.SerializeLegacyCheckpoint()),
            indian.SourceAuthority.DigestLegacyCheckpoint());
        var beforeMinor = indian.SourceAuthority.SerializeCheckpoint();
        var pendingBeforeMinor = indian.SourceAuthority.World.PendingArrivals;
        var randomBeforeMinor = indian.SourceAuthority.RandomState;
        var minorResult = CampaignExercises.ActivateSourceAxisMinorGarrisons(indian);
        Assert.True(minorResult.IsAccepted);
        var minor = minorResult.SuccessorSession!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(minor.SourceAuthority!.CwFreeGunsWorld!.SerializeCanonical()),
            minor.SourceAuthority.CwFreeGunsWorld.Hash());
        Assert.Equal(CampaignPregameSourceCreation.Digest(minor.SourceAuthority!.SerializeLegacyCheckpoint()),
            minor.SourceAuthority.DigestLegacyCheckpoint());
        Assert.Equal(beforeMinor, indian.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.AxisMinorGarrisonsCheckpointContractVersion, minor.SourceAuthority!.ProfileVersion);
        Assert.Equal(randomBeforeMinor, minor.SourceAuthority.RandomState);
        Assert.Equal(pendingBeforeMinor, minor.SourceAuthority.World.PendingArrivals);
        Assert.Equal(grouped.ContentHash, minor.SourceAuthority.GroupedWorld!.ContentHash);
        Assert.Equal(grouped.Groups.Count, minor.SourceAuthority.GroupedWorld.Groups.Count);
        Assert.Equal(0, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("fort-maddalena-garrison", "c3019"));
        Assert.Equal(1, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("fort-maddalena-garrison", "c3020"));
        Assert.Equal(0, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("mechili-garrison", "b4921"));
        Assert.Equal(1, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("mechili-garrison", "b4922"));
        foreach (var unit in ContentSourceAxisMinorGarrisonsPackArtifact.Units)
        {
            Assert.Equal(0, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt(unit.Id, unit.Location));
            var element = Assert.Single(minor.SourceAuthority.World.World.Elements, value => value.ElementId == unit.Id);
            Assert.Equal(unit.Location, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
            Assert.Single(minor.SourceAuthority.World.World.Representations, value =>
                value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
                && value.CurrentLocationId == unit.Location && value.BoundElementIds.SequenceEqual([unit.Id]));
            Assert.Contains(minor.SourceAuthority.Battlefield!.Entries, value => value.ElementId == unit.Id
                && value.AuthorityStatus == "source-axis-minor-active");
        }
        // Use the actual activated counters, not source-evidence-only RTR or
        // Hussars. The two observers are independent counters, not formations.
        var minorContactWorld = CampaignSourcePlayableWorld.Create(minor.SourceAuthority);
        var activePair = CampaignSourcePolishArrivalContacts.EffectiveCounters(minorContactWorld)
            .Where(value => value.RepresentationId is "axis-minor.scheferzen-garrison" or "independent-setup.2-rifle-brigade")
            .ToArray();
        Assert.Equal(2, activePair.Length);
        Assert.All(activePair, value => Assert.Equal(CampaignContactObserverKind.IndependentCounter, value.ObserverKind));
        var pairAxis = new CampaignContactKnowledge(minor.SourceAuthority.Request.CampaignId, LandSide.Axis);
        var pairCw = new CampaignContactKnowledge(minor.SourceAuthority.Request.CampaignId, LandSide.Commonwealth);
        var axisPairContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(activePair, 1, pairAxis));
        var cwPairContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(activePair, 1, pairCw));
        Assert.Equal(CampaignContactKnowledge.IndependentObserverContractVersion, axisPairContact.ContractVersion);
        Assert.Equal(CampaignContactKnowledge.IndependentObserverContractVersion, cwPairContact.ContractVersion);
        var axisPairBytes = CampaignContactTransitionCodec.SerializeCanonical(axisPairContact);
        Assert.Equal(axisPairContact, CampaignContactTransitionCodec.DeserializeCanonical(axisPairBytes));
        Assert.Equal(axisPairBytes, CampaignContactTransitionCodec.SerializeCanonical(Assert.Single(
            CampaignSourceInitialContacts.DeriveCounters(activePair, 1,
                new CampaignContactKnowledge(minor.SourceAuthority.Request.CampaignId, LandSide.Axis)))));
        pairAxis.Apply(axisPairContact);
        pairCw.Apply(cwPairContact);
        Assert.Empty(pairAxis.ObserveFor(axisPairContact.ObserverFormationId));
        var ownCounterView = CampaignIndependentContactObservationV2.Project(minorContactWorld, pairAxis,
            "axis-minor.scheferzen-garrison");
        Assert.Single(ownCounterView.Contacts);
        Assert.DoesNotContain("independent-setup.2-rifle-brigade", System.Text.Encoding.UTF8.GetString(ownCounterView.SerializeCanonical()));
        Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(minorContactWorld,
            pairCw, "axis-minor.scheferzen-garrison"));
        var repeatedPairContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(activePair, 2, pairAxis));
        Assert.Equal(axisPairContact.ContactHandle, repeatedPairContact.ContactHandle);
        Assert.True(CampaignExercises.Reconstruct(minor).IsVerified);
        var compactMinorResult = CampaignExercises.AdmitSourceCompactCheckpoint(minor);
        Assert.True(compactMinorResult.IsAccepted);
        var compactMinor = compactMinorResult.SuccessorSession!;
        var compactMinorBytes = compactMinor.SourceAuthority!.SerializeCheckpoint();
        using (var compactMinorJson = JsonDocument.Parse(compactMinorBytes))
        {
            var baseBytes = compactMinorJson.RootElement.GetProperty("baseCheckpoint").GetBytesFromBase64();
            var artifactBytes = compactMinor.SourceAuthority.CwFreeGunsWorld!.MinorGarrisonContent!.CanonicalBytes;
            Console.WriteLine($"minor checkpoint bytes: compact={compactMinorBytes.Length}, base={baseBytes.Length}, artifact={artifactBytes.Length}");
            Assert.True(compactMinorBytes.Length < 16_384, $"Compact minor checkpoint was {compactMinorBytes.Length} bytes; base {baseBytes.Length}, artifact {artifactBytes.Length}.");
            Assert.False(compactMinorJson.RootElement.TryGetProperty("minorGarrisonContent", out _));
            Assert.Equal(compactMinor.SourceAuthority.World.ContentIdentity.Hash,
                compactMinorJson.RootElement.GetProperty("minorGarrisonContentHash").GetString());
        }
        Assert.True(CampaignExercises.Reconstruct(compactMinor).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceAxisGarrisonRemainder(minor).IsAccepted);
        var oldMinorCheckpoint = compactMinor.SourceAuthority.SerializeCheckpoint();
        var remainderResult = CampaignExercises.ActivateSourceAxisGarrisonRemainder(compactMinor);
        Assert.True(remainderResult.IsAccepted);
        var remainder = remainderResult.SuccessorSession!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(remainder.SourceAuthority!.CwFreeGunsWorld!.SerializeCanonical()),
            remainder.SourceAuthority.CwFreeGunsWorld.Hash());
        Console.WriteLine($"garrison remainder content hash: {remainder.SourceAuthority!.World.ContentIdentity.Hash}");
        if (seed == 14)
            Assert.Equal(CampaignPregameSourceCreation.Digest(remainder.SourceAuthority.SerializeLegacyCheckpoint()),
                remainder.SourceAuthority.DigestLegacyCheckpoint());
        var remainderCheckpointBytes = remainder.SourceAuthority!.SerializeCheckpoint();
        Assert.True(remainderCheckpointBytes.Length < 24 * 1024 * 1024,
            $"Legacy garrison remainder checkpoint was {remainderCheckpointBytes.Length} bytes.");
        Assert.Equal(oldMinorCheckpoint, compactMinor.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.AxisGarrisonRemainderCheckpointContractVersion, remainder.SourceAuthority.ProfileVersion);
        Assert.Equal(ContentSourceAxisGarrisonRemainderPackArtifact.ContentHash, remainder.SourceAuthority.World.ContentIdentity.Hash);
        Assert.Equal(minor.SourceAuthority.RandomState, remainder.SourceAuthority.RandomState);
        Assert.Equal(minor.SourceAuthority.World.PendingArrivals, remainder.SourceAuthority.World.PendingArrivals);
        Assert.Equal(minor.SourceAuthority.GroupedWorld!.ContentHash, remainder.SourceAuthority.GroupedWorld!.ContentHash);
        Assert.Equal(ExpectedGarrisonRemainderIds,
            ContentSourceAxisGarrisonRemainderPackArtifact.Units.Select(value => value.Id).Order(StringComparer.Ordinal));
        foreach (var unit in ContentSourceAxisGarrisonRemainderPackArtifact.Units)
        {
            var element = Assert.Single(remainder.SourceAuthority.World.World.Elements, value => value.ElementId == unit.Id);
            var sourceComponent = ContentSourceAxisGarrisonRemainderPackArtifact.Facts(unit.Id).Components.Single();
            Assert.Equal(unit.Location, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
            Assert.Equal(sourceComponent.ComponentId, element.Components[0].ComponentId);
            Assert.Equal(0, ContentSourceAxisGarrisonRemainderPackArtifact.StackAt(unit.Id, unit.Location));
            Assert.Single(remainder.SourceAuthority.World.World.Representations, value =>
                value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
                && value.CurrentLocationId == unit.Location && value.BoundElementIds.SequenceEqual([unit.Id]));
            Assert.Contains(remainder.SourceAuthority.Battlefield!.Entries, value => value.ElementId == unit.Id
                && value.AuthorityStatus == "source-axis-garrison-remainder-active");
        }
        var remainderContactWorld = CampaignSourcePlayableWorld.Create(remainder.SourceAuthority);
        var remainderCounters = CampaignSourcePolishArrivalContacts.EffectiveCounters(remainderContactWorld);
        var remainderAxisKnowledge = new CampaignContactKnowledge(remainder.SourceAuthority.Request.CampaignId, LandSide.Axis);
        var remainderCwKnowledge = new CampaignContactKnowledge(remainder.SourceAuthority.Request.CampaignId, LandSide.Commonwealth);
        // Both counters come from the admitted source world. The chart proves
        // Soluch and Matruh are separated without inferring a playable seam.
        var soluchAndIndian = remainderCounters.Where(value => value.RepresentationId is
            "axis-garrison.soluch-antitank-company" or "4-indian-division").ToArray();
        Assert.Equal(2, soluchAndIndian.Length);
        Assert.Empty(CampaignSourceInitialContacts.DeriveCounters(soluchAndIndian, 1, remainderAxisKnowledge));
        Assert.Empty(CampaignSourceInitialContacts.DeriveCounters(soluchAndIndian, 1, remainderCwKnowledge));
        Assert.Equal(6, ContentSourceAxisGarrisonRemainderPackArtifact.Units.Count);
        foreach (var removedId in ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds)
        {
            var observerId = "axis-garrison." + removedId;
            Assert.DoesNotContain(remainderCounters, value => value.RepresentationId == observerId);
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(remainderContactWorld, remainderAxisKnowledge, observerId));
        }
        foreach (var unit in ContentSourceAxisGarrisonRemainderPackArtifact.Units)
        {
            var id = "axis-garrison." + unit.Id;
            var counter = Assert.Single(remainderCounters, value => value.RepresentationId == id);
            Assert.Equal(CampaignContactObserverKind.IndependentCounter, counter.ObserverKind);
            Assert.Equal(id, counter.ObserverFormationId);
            Assert.Empty(CampaignIndependentContactObservationV2.Project(remainderContactWorld, remainderAxisKnowledge, id).Contacts);
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(remainderContactWorld, remainderCwKnowledge, id));
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(minorContactWorld, pairAxis, id));
        }
        Assert.Equal(1, ContentSourceAxisGarrisonRemainderPackArtifact.StackAt("soluch-artillery-regiment", "a4131"));
        Assert.Equal(1, ContentSourceAxisGarrisonRemainderPackArtifact.StackAt("giarabub-machine-gun", "c1015"));
        Assert.DoesNotContain(remainder.SourceAuthority.World.World.Elements, value => value.ElementId == "giarabub-motor-machine-gun");
        Assert.Contains(remainder.SourceAuthority.Battlefield!.Entries, value => value.ElementId == "giarabub-motor-machine-gun"
            && value.AuthorityStatus == "source-evidence");
        foreach (var id in ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds)
        {
            Assert.DoesNotContain(remainder.SourceAuthority.World.World.Elements, value => value.ElementId == id);
            Assert.DoesNotContain(remainder.SourceAuthority.World.World.Representations, value => value.BoundElementIds.Contains(id));
            var removedEntry = Assert.Single(remainder.SourceAuthority.Battlefield!.Entries, value => value.ElementId == id
                && value.AuthorityStatus == "source-land-only-aa-removed");
            Assert.DoesNotContain(CampaignSourcePlayableWorld.Create(remainder.SourceAuthority).UnresolvedAt(removedEntry.LocationId!),
                value => value.ElementId == id);
        }
        using (var contentDocument = JsonDocument.Parse(remainder.SourceAuthority.CwFreeGunsWorld!.GarrisonRemainderContent!.CanonicalBytes))
        {
            var removedIds = contentDocument.RootElement.GetProperty("removedAntiAirUnits").EnumerateArray()
                .Select(value => value.GetString()!).ToArray();
            Assert.Equal(ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds, removedIds);
            Assert.Equal("land:32.83(1)-italian-uu-removed-from-play", contentDocument.RootElement.GetProperty("antiAirTreatment").GetString());
        }
        Assert.True(CampaignExercises.Reconstruct(remainder).IsVerified);
        var remainderHistory = remainder.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var remainderEvent = JsonDocument.Parse(remainderHistory[^1]))
        {
            var worldHash = remainderEvent.RootElement.GetProperty("worldHash").GetString()!;
            var changedHash = worldHash[..^1] + (worldHash[^1] == '0' ? '1' : '0');
            remainderHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(remainderHistory[^1])
                .Replace(worldHash, changedHash, StringComparison.Ordinal));
        }
        var tamperedRemainder = new ExerciseSession(remainder.Pregame!, remainder.CurrentContacts,
            remainderHistory, remainder.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(tamperedRemainder).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceAxisGarrisonRemainder(remainder).IsAccepted);
        var compactRemainder = remainder;
        var compactRemainderBytes = compactRemainder.SourceAuthority!.SerializeCheckpoint();
        using (var remainderCheckpoint = JsonDocument.Parse(compactRemainderBytes))
        {
            Assert.True(compactRemainderBytes.Length < 16_384, $"Compact remainder checkpoint was {compactRemainderBytes.Length} bytes.");
            Assert.False(remainderCheckpoint.RootElement.TryGetProperty("garrisonRemainderContent", out _));
            Assert.Equal(compactRemainder.SourceAuthority.World.ContentIdentity.Hash,
                remainderCheckpoint.RootElement.GetProperty("garrisonRemainderContentHash").GetString());
        }
        Assert.True(CampaignExercises.Reconstruct(compactRemainder).IsVerified);
        var priorRemainderCheckpoint = remainder.SourceAuthority.SerializeCheckpoint();
        var priorAxisMotorization = remainder.SourceAuthority.AxisMotorization.Hash();
        var motorMgResult = CampaignExercises.ActivateSourceGiarabubMotorMachineGun(remainder);
        Assert.True(motorMgResult.IsAccepted);
        var motorMg = motorMgResult.SuccessorSession!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(motorMg.SourceAuthority!.CwFreeGunsWorld!.SerializeCanonical()),
            motorMg.SourceAuthority.CwFreeGunsWorld.Hash());
        Assert.Equal(priorRemainderCheckpoint, remainder.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.GiarabubMotorMachineGunCheckpointContractVersion, motorMg.SourceAuthority!.ProfileVersion);
        Assert.Equal(priorAxisMotorization, motorMg.SourceAuthority.AxisMotorization.Hash());
        Assert.Equal(remainder.SourceAuthority.RandomState, motorMg.SourceAuthority.RandomState);
        Assert.Equal(remainder.SourceAuthority.World.PendingArrivals, motorMg.SourceAuthority.World.PendingArrivals);
        var motorMgElement = Assert.Single(motorMg.SourceAuthority.World.World.Elements,
            value => value.ElementId == ContentSourceGiarabubMotorMachineGunArtifact.ElementId);
        Assert.Equal("c1014", motorMgElement.CurrentLocationId);
        Assert.Equal(1, Assert.Single(motorMgElement.Components).CurrentToe);
        Assert.Single(motorMg.SourceAuthority.World.World.Representations, value =>
            value.RepresentationId == "axis-garrison.giarabub-motor-machine-gun"
            && value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement);
        var motorMgContent = motorMg.SourceAuthority.CwFreeGunsWorld!.MotorMachineGunContent!;
        Console.WriteLine($"giarabub motor MG content hash: {motorMgContent.Identity.Hash}");
        Assert.Equal(ContentSourceGiarabubMotorMachineGunArtifact.ContentHash, motorMgContent.Identity.Hash);
        using (var document = JsonDocument.Parse(motorMgContent.CanonicalBytes))
        {
            var root = document.RootElement;
            Assert.Equal("8+", root.GetProperty("printedCapabilityPoints").GetString());
            Assert.Equal(JsonValueKind.Null, root.GetProperty("effectiveCapabilityPoints").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("mobilityId").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("motorPoolId").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("assignedMotorPoints").ValueKind);
        }
        var motorMgWorld = CampaignSourcePlayableWorld.Create(motorMg.SourceAuthority);
        Assert.Contains("giarabub-motor-mg-historical-motorization-priority-unresolved", motorMgWorld.ReadinessBlockers());
        Assert.Contains("giarabub-motor-mg-capability-and-mobility-unresolved", motorMgWorld.ReadinessBlockers());
        var motorMgCounter = Assert.Single(CampaignSourcePolishArrivalContacts.EffectiveCounters(motorMgWorld),
            value => value.RepresentationId == "axis-garrison.giarabub-motor-machine-gun");
        Assert.Equal(CampaignContactObserverKind.IndependentCounter, motorMgCounter.ObserverKind);
        Assert.Equal(motorMgCounter.RepresentationId, motorMgCounter.ObserverFormationId);
        var motorMgAxisKnowledge = new CampaignContactKnowledge(motorMg.SourceAuthority.Request.CampaignId, LandSide.Axis);
        var motorMgCwKnowledge = new CampaignContactKnowledge(motorMg.SourceAuthority.Request.CampaignId, LandSide.Commonwealth);
        Assert.Empty(CampaignIndependentContactObservationV2.Project(motorMgWorld, motorMgAxisKnowledge, motorMgCounter.RepresentationId).Contacts);
        Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(motorMgWorld, motorMgCwKnowledge, motorMgCounter.RepresentationId));
        Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(remainderContactWorld, motorMgAxisKnowledge, motorMgCounter.RepresentationId));
        var motorAndIndian = CampaignSourcePolishArrivalContacts.EffectiveCounters(motorMgWorld)
            .Where(value => value.RepresentationId is "axis-garrison.giarabub-motor-machine-gun" or "4-indian-division").ToArray();
        Assert.Equal(2, motorAndIndian.Length);
        Assert.Empty(CampaignSourceInitialContacts.DeriveCounters(motorAndIndian, 1, motorMgAxisKnowledge));
        // Exercise the verified Giarabub hexside using the real counters; this
        // hypothetical position is not an admitted campaign move.
        var adjacentIndian = motorAndIndian.Single(value => value.RepresentationId == "4-indian-division") with { LocationId = "c1015" };
        var motorContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters([motorMgCounter, adjacentIndian], 1, motorMgAxisKnowledge));
        Assert.Equal(CampaignContactObserverKind.IndependentCounter, motorContact.ObserverKind);
        Assert.Equal(motorMgCounter.RepresentationId, motorContact.ObserverFormationId);
        Assert.False(motorMgWorld.IsGameplayReady);
        Assert.False(CampaignExercises.StartSourceTurn(motorMg).IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(motorMg).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceGiarabubMotorMachineGun(motorMg).IsAccepted);
        var motorMgCheckpoint = motorMg.SourceAuthority.SerializeCheckpoint();
        Assert.True(motorMgCheckpoint.Length < 16_384, $"Compact motor MG checkpoint was {motorMgCheckpoint.Length} bytes.");
        using (var document = JsonDocument.Parse(motorMgCheckpoint))
            Assert.Equal(motorMgContent.Identity.Hash, document.RootElement.GetProperty("motorMachineGunContentHash").GetString());
        var cwFrontlineResult = CampaignExercises.ActivateSourceCwFrontline(motorMg);
        Assert.True(cwFrontlineResult.IsAccepted);
        var cwFrontline = cwFrontlineResult.SuccessorSession!;
        var cwAuthority = cwFrontline.SourceAuthority!;
        if (seed == 14)
        {
            var loads = cwFrontline;
            Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAttachedInitialLoadsDraft(loads, CampaignActionAudience.Axis));
            using (var opening = JsonDocument.Parse(CampaignExercises.QuerySourceAttachedInitialLoadsDraft(loads, CampaignActionAudience.Commonwealth).VisibleBytes))
            {
                Assert.Equal(2, opening.RootElement.GetProperty("contractVersion").GetInt32());
                Assert.Contains(opening.RootElement.GetProperty("frontlinePools").EnumerateArray(), value =>
                    value.GetProperty("elementId").GetString() == "1-rtr" && value.GetProperty("totalPoints").GetInt32() == 5);
            }
            Assert.False(CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-finish", "frontline"),
                    CampaignActionAudience.Commonwealth)).IsAccepted);
            foreach (var (id, count) in new[] { ("1-rtr", 5), ("11-hussars", 3) })
                for (var i = 0; i < count; i++)
                    loads = CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                        Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-attach", id),
                            CampaignActionAudience.Commonwealth)).SuccessorSession!;
            var attachOverflow = CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-attach", "1-rtr"),
                    CampaignActionAudience.Commonwealth));
            Assert.False(attachOverflow.IsAccepted);
            var emptyFinished = CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-finish", "frontline"),
                    CampaignActionAudience.Commonwealth)).SuccessorSession!;
            Assert.DoesNotContain("cw-frontline-initial-loads-unresolved",
                CampaignSourcePlayableWorld.Create(emptyFinished.SourceAuthority!).ReadinessBlockers());
            Assert.False(CampaignExercises.StartSourceTurn(emptyFinished).IsAccepted);
            Assert.True(CampaignExercises.Reconstruct(emptyFinished).IsVerified);
            var reopenedEmpty = CampaignExercises.SubmitSourceAttachedInitialLoads(emptyFinished,
                Submission(emptyFinished, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-reopen", "frontline"),
                    CampaignActionAudience.Commonwealth)).SuccessorSession!;
            Assert.Contains("cw-frontline-initial-loads-unresolved",
                CampaignSourcePlayableWorld.Create(reopenedEmpty.SourceAuthority!).ReadinessBlockers());
            foreach (var (kind, id) in new[] { ("frontline-ammo", "1-rtr"), ("frontline-fuel", "11-hussars"), ("frontline-finish", "frontline") })
                loads = CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                    Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId(kind, id),
                        CampaignActionAudience.Commonwealth)).SuccessorSession!;
            var loadState = loads.SourceAuthority!.AttachedInitialLoads!;
            Assert.True(loadState.FrontlineFinished);
            Assert.Equal(5, loadState.Attached("1-rtr"));
            Assert.Equal(3, loadState.Attached("11-hussars"));
            Assert.Equal(1, Assert.Single(loadState.Targets, value => value.ElementId == "1-rtr").AmmoPoints);
            Assert.Equal(1, Assert.Single(loadState.Targets, value => value.ElementId == "11-hussars").FuelPoints);
            Assert.Contains("cw-frontline-initial-loads-unresolved", CampaignSourcePlayableWorld.Create(loads.SourceAuthority).ReadinessBlockers());
            Assert.False(CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-ammo", "1-rtr"),
                    CampaignActionAudience.Commonwealth)).IsAccepted);
            Assert.True(CampaignExercises.Reconstruct(loads).IsVerified);
        }
        Assert.True(CampaignExercises.IsSourceCwFrontlineActivated(cwFrontline));
        Assert.Equal(motorMgCheckpoint, motorMg.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.CwFrontlineCheckpointContractVersion, cwAuthority.ProfileVersion);
        Assert.Equal(motorMg.SourceAuthority.CommonwealthMotorization.Hash(), cwAuthority.CommonwealthMotorization.Hash());
        Assert.Equal(motorMg.SourceAuthority.AxisMotorization.Hash(), cwAuthority.AxisMotorization.Hash());
        Assert.Equal(motorMg.SourceAuthority.RandomState, cwAuthority.RandomState);
        Assert.Equal(motorMg.SourceAuthority.World.PendingArrivals, cwAuthority.World.PendingArrivals);
        foreach (var unit in ContentSourceCwFrontlineArtifact.Units)
        {
            var element = Assert.Single(cwAuthority.World.World.Elements, value => value.ElementId == unit.Id);
            Assert.Equal(unit.Location, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
            Assert.Single(cwAuthority.World.World.Representations, value => value.RepresentationId == "cw-frontline." + unit.Id
                && value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement);
            Assert.Contains(cwAuthority.Battlefield!.Entries, value => value.ElementId == unit.Id
                && value.AuthorityStatus == "source-cw-frontline-active-initial-loads-unresolved");
        }
        var cwNativeWorld = cwAuthority.CwFreeGunsWorld!;
        var freshHashWorld = remainder.SourceAuthority.CwFreeGunsWorld!.ActivateGiarabubMotorMachineGun(
            remainder.SourceAuthority.AxisGarrisons!, remainder.SourceAuthority.Battlefield!);
        var canonicalWorldHash = CampaignPregameSourceCreation.Digest(freshHashWorld.SerializeCanonical());
        var concurrentWorldHashes = new string[8];
        Parallel.For(0, concurrentWorldHashes.Length, index => concurrentWorldHashes[index] = freshHashWorld.Hash());
        Assert.All(concurrentWorldHashes, hash => Assert.Equal(canonicalWorldHash, hash));
        var cwContent = cwNativeWorld.CwFrontlineContent!;
        var cwContactWorld = CampaignSourcePlayableWorld.Create(cwAuthority);
        var cwEffectiveCounters = CampaignSourcePolishArrivalContacts.EffectiveCounters(cwContactWorld);
        foreach (var (cwId, axisId) in new[]
        {
            ("cw-frontline.1-rtr", "axis-minor.scheferzen-garrison"),
            ("cw-frontline.11-hussars", "axis-minor.fort-maddalena-garrison"),
        })
        {
            var actualPair = cwEffectiveCounters.Where(value => value.RepresentationId == cwId || value.RepresentationId == axisId).ToArray();
            Assert.Equal(2, actualPair.Length);
            Assert.All(actualPair, value => Assert.Equal(CampaignContactObserverKind.IndependentCounter, value.ObserverKind));
            var cwKnowledge = new CampaignContactKnowledge(cwAuthority.Request.CampaignId, LandSide.Commonwealth);
            var axisKnowledge = new CampaignContactKnowledge(cwAuthority.Request.CampaignId, LandSide.Axis);
            var cwContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(actualPair, 1, cwKnowledge));
            var axisContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(actualPair, 1, axisKnowledge));
            Assert.Equal(cwId, cwContact.ObserverFormationId);
            Assert.Equal(axisId, axisContact.ObserverFormationId);
            var serialized = CampaignContactTransitionCodec.SerializeCanonical(cwContact);
            Assert.Equal(cwContact, CampaignContactTransitionCodec.DeserializeCanonical(serialized));
            Assert.Equal(serialized, CampaignContactTransitionCodec.SerializeCanonical(Assert.Single(
                CampaignSourceInitialContacts.DeriveCounters(actualPair, 1,
                    new CampaignContactKnowledge(cwAuthority.Request.CampaignId, LandSide.Commonwealth)))));
            cwKnowledge.Apply(cwContact);
            axisKnowledge.Apply(axisContact);
            var cwView = CampaignIndependentContactObservationV2.Project(cwContactWorld, cwKnowledge, cwId);
            Assert.Single(cwView.Contacts);
            Assert.DoesNotContain(axisId, System.Text.Encoding.UTF8.GetString(cwView.SerializeCanonical()));
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(cwContactWorld, axisKnowledge, cwId));
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(
                CampaignSourcePlayableWorld.Create(motorMg.SourceAuthority), cwKnowledge, cwId));
            Assert.Equal(cwContact.ContactHandle, Assert.Single(CampaignSourceInitialContacts.DeriveCounters(actualPair, 2, cwKnowledge)).ContactHandle);
        }
        Assert.Equal(ContentSourceCwFrontlineArtifact.ContentHash, cwContent.Identity.Hash);
        Assert.Equal(cwContent.Identity, ContentSourceCwFrontlineArtifact.Read(cwContent.CanonicalBytes).Identity);
        if (seed == 14)
        {
            var changedMotor = motorMgContent.CanonicalBytes;
            var motorFormat = changedMotor.AsSpan().IndexOf("sandtable.source-giarabub-motor-mg.v1"u8);
            Assert.True(motorFormat >= 0);
            changedMotor[motorFormat] = (byte)'X';
            Assert.Throws<InvalidOperationException>(() => ContentSourceGiarabubMotorMachineGunArtifact.Read(changedMotor));
            var changedFrontline = cwContent.CanonicalBytes;
            var frontlineFormat = changedFrontline.AsSpan().IndexOf("sandtable.source-cw-frontline.v1"u8);
            Assert.True(frontlineFormat >= 0);
            changedFrontline[frontlineFormat] = (byte)'X';
            Assert.Throws<InvalidOperationException>(() => ContentSourceCwFrontlineArtifact.Read(changedFrontline));
        }
        using (var document = JsonDocument.Parse(cwContent.CanonicalBytes))
            foreach (var unit in document.RootElement.GetProperty("units").EnumerateArray())
            {
                if (unit.GetProperty("elementId").GetString() == "11-hussars")
                    Assert.Equal(1, unit.GetProperty("chartArmorRating").GetInt32());
                Assert.Equal(JsonValueKind.Null, unit.GetProperty("initialCargo").ValueKind);
                Assert.Equal(JsonValueKind.Null, unit.GetProperty("parentFormationId").ValueKind);
            }
        Assert.Contains("cw-frontline-initial-loads-unresolved", CampaignSourcePlayableWorld.Create(cwAuthority).ReadinessBlockers());
        Assert.False(CampaignExercises.StartSourceTurn(cwFrontline).IsAccepted);
        Assert.False(CampaignExercises.ActivateSourceCwFrontline(cwFrontline).IsAccepted);
        var cwCheckpoint = cwAuthority.SerializeCheckpoint();
        Assert.True(cwCheckpoint.Length < 16_384, $"Compact CW frontline checkpoint was {cwCheckpoint.Length} bytes.");
        using (var document = JsonDocument.Parse(cwCheckpoint))
            Assert.Equal(cwContent.Identity.Hash, document.RootElement.GetProperty("cwFrontlineContentHash").GetString());
        Assert.True(CampaignExercises.Reconstruct(cwFrontline).IsVerified);
        var cwHistory = cwFrontline.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var document = JsonDocument.Parse(cwHistory[^1]))
        {
            var hash = document.RootElement.GetProperty("worldHash").GetString()!;
            var changed = hash[..^1] + (hash[^1] == '0' ? '1' : '0');
            cwHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(cwHistory[^1])
                .Replace(hash, changed, StringComparison.Ordinal));
        }
        Assert.False(CampaignExercises.Reconstruct(new ExerciseSession(cwFrontline.Pregame!, cwFrontline.CurrentContacts,
            cwHistory, cwFrontline.SourceAuthority)).IsVerified);
        var motorMgHistory = motorMg.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var document = JsonDocument.Parse(motorMgHistory[^1]))
        {
            var hash = document.RootElement.GetProperty("worldHash").GetString()!;
            var changed = hash[..^1] + (hash[^1] == '0' ? '1' : '0');
            motorMgHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(motorMgHistory[^1])
                .Replace(hash, changed, StringComparison.Ordinal));
        }
        Assert.False(CampaignExercises.Reconstruct(new ExerciseSession(motorMg.Pregame!, motorMg.CurrentContacts,
            motorMgHistory, motorMg.SourceAuthority)).IsVerified);
        var minorHistory = minor.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var eventDocument = JsonDocument.Parse(minorHistory[^1]))
        {
            var worldHash = eventDocument.RootElement.GetProperty("worldHash").GetString()!;
            var changedHash = worldHash[..^1] + (worldHash[^1] == '0' ? '1' : '0');
            minorHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(minorHistory[^1])
                .Replace(worldHash, changedHash, StringComparison.Ordinal));
        }
        var tamperedMinor = new ExerciseSession(minor.Pregame!, minor.CurrentContacts, minorHistory, minor.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(tamperedMinor).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceAxisMinorGarrisons(minor).IsAccepted);
        var minorFinish = CampaignExercises.Query(minor, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var minorFinished = CampaignExercises.Submit(minor,
            Submission(minor, minorFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(minorFinished).IsVerified);
        Assert.Equal(minor.SourceAuthority.World.ContentIdentity, minorFinished.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(ContentSourceFourthIndianPackArtifact.ContentHash, grouped.ContentHash);
        Assert.Equal(11, grouped.Groups.Single().Members.Count);
        var originalGroup = grouped.Groups.Single();
        var forgedMembers = originalGroup.Members.Select(value => value.ElementId == "25-field-artillery"
            ? value with { AdditionalComponents = Array.AsReadOnly(new[] { new CampaignGroupedComponent("extra-gun", 1) }) } : value);
        var forgedGroup = new CampaignGroupedFormation(originalGroup.GroupId, originalGroup.SideId, originalGroup.RootId,
            originalGroup.LocationId, originalGroup.StackValue, originalGroup.MotorPool, forgedMembers, originalGroup.DispositionConfirmed);
        var forgedWorld = CampaignGroupedWorld.Create(grouped.Prior, grouped.ContentHash, [forgedGroup]);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceFourthIndianDefinition.Validate(forgedWorld));
        var fieldArtillery = grouped.Groups.Single().Members.Single(value => value.ElementId == "25-field-artillery");
        Assert.Equal("3-7-inch-howitzer", fieldArtillery.ComponentType);
        Assert.Equal(6, fieldArtillery.ComponentToe);
        Assert.Equal(grouped.Prior.Initialization.CapabilityPointsExpended, fieldArtillery.CapabilityPointsExpended);
        Assert.Equal(11, indian.SourceAuthority.Battlefield!.Entries.Count(value => value.LocationId == "d3615" && value.AuthorityStatus == "source-fourth-indian-proposed"));
        Assert.Null(grouped.StackAt("commonwealth", "d3615", _ => null));
        Assert.Contains(grouped.Prior.PendingArrivals, value => value.GroupId == "graziani-7-indian-brigade");
        var priorityAction = CampaignSourceAuthorityState.FourthIndianMotorActionId("central-india-horse", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Axis)).RejectionReason);
        Assert.False(grouped.Groups.Single().DispositionConfirmed);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        var confirm = CampaignExercises.Query(indian, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "fourth-indian-motorization");
        Assert.Contains("confirm", confirm.ChoiceJson, StringComparison.Ordinal);
        var ownerDraft = System.Text.Encoding.UTF8.GetString(CampaignExercises.QuerySourceFourthIndianDraft(indian, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.Contains("provisional-source-default-alternatives-unavailable", ownerDraft, StringComparison.Ordinal);
        Assert.Contains("5-indian-brigade-hq", ownerDraft, StringComparison.Ordinal);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(indian,
            Submission(indian, confirm.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var confirmCommand = Submission(indian, confirm.ActionId, CampaignActionAudience.Commonwealth);
        indian = CampaignExercises.Submit(indian, confirmCommand).SuccessorSession!;
        Assert.True(indian.SourceAuthority!.GroupedWorld!.Groups.Single().DispositionConfirmed);
        Assert.Equal(5, indian.SourceAuthority.GroupedWorld.StackAt("commonwealth", "d3615", _ => null));
        Assert.Equal(11, indian.SourceAuthority.Battlefield!.Entries.Count(value => value.LocationId == "d3615" && value.AuthorityStatus == "source-fourth-indian-grouped"));
        Assert.Equal(34, indian.SourceAuthority.ProfileVersion);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(indian, confirmCommand).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        indian = CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(21, indian.SourceAuthority!.GroupedWorld!.RemainingMotorPoints("4-indian-division"));
        var footAction = CampaignSourceAuthorityState.FourthIndianMotorActionId("1-royal-fusiliers", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, footAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        var minorBeforePromotions = CampaignExercises.ActivateSourceAxisMinorGarrisons(indian).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.AdmitSourceCompactCheckpoint(minorBeforePromotions).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.ActivateSourceAxisGarrisonRemainder(minorBeforePromotions).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.ActivateSourceGiarabubMotorMachineGun(minorBeforePromotions).SuccessorSession!;
        var minorIdentity = minorBeforePromotions.SourceAuthority!.World.ContentIdentity;
        minorBeforePromotions = CampaignExercises.SubmitSourceMalettiHierarchy(minorBeforePromotions,
            Submission(minorBeforePromotions, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        var minorMaletti = CampaignExercises.PromoteSourceCertifiedMalettiWorld(minorBeforePromotions);
        Assert.True(minorMaletti.IsAccepted);
        minorBeforePromotions = minorMaletti.SuccessorSession!;
        minorBeforePromotions = CampaignExercises.AdmitSourceMatruhFormation(minorBeforePromotions).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.SubmitSourceMatruhFormation(minorBeforePromotions,
            Submission(minorBeforePromotions, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.SubmitSourceMatruhFormation(minorBeforePromotions,
            Submission(minorBeforePromotions, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var minorMatruh = CampaignExercises.PromoteSourceMatruhDetachedWorld(minorBeforePromotions);
        Assert.True(minorMatruh.IsAccepted);
        minorBeforePromotions = minorMatruh.SuccessorSession!;
        Assert.Equal(minorIdentity, minorBeforePromotions.SourceAuthority!.World.ContentIdentity);
        Assert.All(ContentSourceAxisMinorGarrisonsPackArtifact.Units, unit =>
            Assert.Contains(minorBeforePromotions.SourceAuthority.World.World.Elements, element => element.ElementId == unit.Id));
        Assert.True(CampaignExercises.Reconstruct(minorBeforePromotions).IsVerified);
        var indianWithoutMalettiDetachment = indian;
        indian = CampaignExercises.SubmitSourceMalettiHierarchy(indian,
            Submission(indian, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        indian = CampaignExercises.PromoteSourceCertifiedMalettiWorld(indian).SuccessorSession!;
        Assert.Equal(21, indian.SourceAuthority!.GroupedWorld!.RemainingMotorPoints("4-indian-division"));
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        var matruhBeforeDetach = CampaignExercises.AdmitSourceMatruhFormation(indian).SuccessorSession!;
        matruhBeforeDetach = CampaignExercises.SubmitSourceMatruhFormation(matruhBeforeDetach,
            Submission(matruhBeforeDetach, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var matruhAfterDetach = matruhBeforeDetach;
        var groupedResult = CampaignExercises.AdmitSourceMatruhGroupedWorld(matruhBeforeDetach);
        Assert.True(groupedResult.IsAccepted);
        matruhBeforeDetach = groupedResult.SuccessorSession!;
        Assert.Equal(2, matruhBeforeDetach.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(0, matruhBeforeDetach.SourceAuthority.GroupedWorld.StackAt("commonwealth", "d3714", _ => null));
        Assert.Equal(6, matruhBeforeDetach.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "matruh-garrison").Members.Count);
        Assert.Equal(35, matruhBeforeDetach.SourceAuthority.ProfileVersion);
        var activeMaletti = CampaignExercises.SubmitSourceAxisPriority(matruhBeforeDetach,
            Submission(matruhBeforeDetach, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        activeMaletti = CampaignExercises.AdmitSourceMalettiGroupedWorld(activeMaletti).SuccessorSession!;
        Assert.Equal(3, activeMaletti.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(30, activeMaletti.SourceAuthority.GroupedWorld.RemainingMotorPoints("maletti-division"));
        Assert.Equal(5, activeMaletti.SourceAuthority.GroupedWorld.ExternalReservations.Single(value => value.GroupId == "maletti-division").AssignedMotorPoints);
        Assert.Equal(36, activeMaletti.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(activeMaletti).IsVerified);
        var libyanAdmission = CampaignExercises.AdmitSourceLibyanGroupedWorld(activeMaletti);
        Assert.True(libyanAdmission.IsAccepted);
        var libyanProposed = libyanAdmission.SuccessorSession!;
        Assert.Equal(5, libyanProposed.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(22, libyanProposed.SourceAuthority.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-libyan-proposed"));
        Assert.Equal(37, libyanProposed.SourceAuthority.ProfileVersion);
        var secondArtillery = libyanProposed.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "2-libyan-division")
            .Members.Single(value => value.ElementId == "2-libyan-artillery");
        Assert.Equal("75-27-gun", secondArtillery.ComponentType);
        Assert.Equal(9, secondArtillery.ComponentToe);
        Assert.Contains(secondArtillery.AdditionalComponents!, value => value.ComponentType == "100-17-howitzer" && value.Toe == 3);
        var axisChoices = CampaignExercises.Query(libyanProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "libyan-disposition").ToArray();
        Assert.Equal(2, axisChoices.Length);
        Assert.DoesNotContain(CampaignExercises.Query(libyanProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "libyan-disposition");
        var firstChoice = axisChoices.Single(value => value.ChoiceJson.Contains("1-libyan-division", StringComparison.Ordinal));
        var secondChoice = axisChoices.Single(value => value.ChoiceJson.Contains("2-libyan-division", StringComparison.Ordinal));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(libyanProposed,
            Submission(libyanProposed, firstChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var firstCommand = Submission(libyanProposed, firstChoice.ActionId, CampaignActionAudience.Axis);
        var firstConfirmed = CampaignExercises.Submit(libyanProposed, firstCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(firstConfirmed, firstCommand).RejectionReason);
        var secondAfter = CampaignExercises.Query(firstConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "libyan-disposition");
        firstConfirmed = CampaignExercises.Submit(firstConfirmed, Submission(firstConfirmed, secondAfter.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(22, firstConfirmed.SourceAuthority!.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-libyan-grouped"));
        Assert.Equal(5, firstConfirmed.SourceAuthority.GroupedWorld!.StackAt("axis", "c4020", _ => null));
        Assert.Equal(5, firstConfirmed.SourceAuthority.GroupedWorld.StackAt("axis", "c3920", _ => null));
        Assert.True(CampaignExercises.Reconstruct(firstConfirmed).IsVerified);
        var italianBasisFourth = ContentSourceFourthIndianPackArtifact.Create(firstConfirmed.SourceAuthority.CwFreeGunsWorld!.Content);
        var italianBasisMatruh = ContentSourceMatruhGroupedPackArtifact.Create(italianBasisFourth);
        var italianBasisMaletti = ContentSourceMalettiGroupedPackArtifact.Create(italianBasisMatruh);
        var italianBasisLibyan = ContentSourceLibyanGroupedPackArtifact.Create(italianBasisMaletti);
        var italianContent = ContentSourceCireneMarmaricaGroupedPackArtifact.Create(italianBasisLibyan);
        Assert.Equal(ContentSourceCireneMarmaricaGroupedPackArtifact.ContentHash, italianContent.Hash());
        var italianAdmission = CampaignExercises.AdmitSourceItalianDivisionsGroupedWorld(firstConfirmed);
        Assert.True(italianAdmission.IsAccepted);
        var italianProposed = italianAdmission.SuccessorSession!;
        Assert.Equal(7, italianProposed.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(29, italianProposed.SourceAuthority.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-italian-division-proposed"));
        Assert.Equal(39, italianProposed.SourceAuthority.ProfileVersion);
        var italianChoices = CampaignExercises.Query(italianProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "italian-division-disposition").ToArray();
        Assert.Equal(2, italianChoices.Length);
        Assert.DoesNotContain(CampaignExercises.Query(italianProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "italian-division-disposition");
        var cireneChoice = italianChoices.Single(value => value.ChoiceJson.Contains("cirene-division", StringComparison.Ordinal));
        var italianCommand = Submission(italianProposed, cireneChoice.ActionId, CampaignActionAudience.Axis);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(italianProposed,
            Submission(italianProposed, cireneChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var italianConfirmed = CampaignExercises.Submit(italianProposed, italianCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(italianConfirmed, italianCommand).RejectionReason);
        var marmaricaChoice = CampaignExercises.Query(italianConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "italian-division-disposition");
        italianConfirmed = CampaignExercises.Submit(italianConfirmed, Submission(italianConfirmed, marmaricaChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(29, italianConfirmed.SourceAuthority!.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-italian-division-grouped"));
        Assert.Equal(5, italianConfirmed.SourceAuthority.GroupedWorld!.StackAt("axis", "c4120", _ => null));
        Assert.Equal(5, italianConfirmed.SourceAuthority.GroupedWorld.StackAt("axis", "c3918", _ => null));
        Assert.True(CampaignExercises.Reconstruct(italianConfirmed).IsVerified);
        var seventhDirect = italianConfirmed.SourceAuthority.AdmitSeventhArmoredGroupedWorld();
        Assert.NotNull(seventhDirect.GroupedWorld);
        var seventhResult = CampaignExercises.AdmitSourceSeventhArmoredGroupedWorld(italianConfirmed);
        Assert.True(seventhResult.IsAccepted);
        var seventhProposed = seventhResult.SuccessorSession!;
        var seventh = seventhProposed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division");
        Assert.Equal(7, seventh.Members.Count); Assert.Null(seventh.StackValue); Assert.False(seventh.DispositionConfirmed);
        Assert.Equal(10, seventh.MotorPool);
        Assert.Equal(40, seventhProposed.SourceAuthority!.ProfileVersion);
        var seventhChoice = CampaignExercises.Query(seventhProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "seventh-armored-disposition");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(seventhProposed,
            Submission(seventhProposed, seventhChoice.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var seventhCommand = Submission(seventhProposed, seventhChoice.ActionId, CampaignActionAudience.Commonwealth);
        var seventhConfirmedResult = CampaignExercises.Submit(seventhProposed, seventhCommand);
        Assert.True(seventhConfirmedResult.IsAccepted);
        var seventhConfirmed = seventhConfirmedResult.SuccessorSession!;
        Assert.Equal(5, seventhConfirmed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division").StackValue);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(seventhConfirmed, seventhCommand).RejectionReason);
        Assert.Equal(italianConfirmed.SourceAuthority.World.PendingArrivals, seventhConfirmed.SourceAuthority.World.PendingArrivals);
        Assert.True(CampaignExercises.Reconstruct(seventhConfirmed).IsVerified);
        var legacySeventhCheckpoint = seventhConfirmed.SourceAuthority.SerializeCheckpoint();
        var correctedSeventh = CampaignExercises.CorrectSourceSeventhShell(seventhConfirmed).SuccessorSession!;
        Assert.Equal(3, correctedSeventh.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division").StackValue);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedLegacyCheckpointContractVersion, correctedSeventh.SourceAuthority.ProfileVersion);
        Assert.Equal(legacySeventhCheckpoint, seventhConfirmed.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(seventhConfirmed.SourceAuthority.RandomState, correctedSeventh.SourceAuthority.RandomState);
        Assert.Contains("common-charts-9.4-division-shell-stacks-three", System.Text.Encoding.UTF8.GetString(
            CampaignExercises.QuerySourceSeventhArmoredDraft(correctedSeventh, CampaignActionAudience.Commonwealth).VisibleBytes));
        Assert.True(CampaignExercises.Reconstruct(correctedSeventh).IsVerified);
        Assert.False(CampaignExercises.CorrectSourceSeventhShell(correctedSeventh).IsAccepted);
        var compactBeforeShell = CampaignExercises.AdmitSourceCompactCheckpoint(seventhConfirmed).SuccessorSession!;
        var compactAxisFinish = CampaignExercises.Query(compactBeforeShell, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var compactAxisFinished = CampaignExercises.Submit(compactBeforeShell,
            Submission(compactBeforeShell, compactAxisFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var compactCwFinish = CampaignExercises.Query(compactAxisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var compactBothFinished = CampaignExercises.Submit(compactAxisFinished,
            Submission(compactAxisFinished, compactCwFinish.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var compactCorrected = CampaignExercises.CorrectSourceSeventhShell(compactBothFinished).SuccessorSession!;
        Assert.Equal(1, compactCorrected.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedFinalizedCompactCheckpointContractVersion, compactCorrected.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(compactCorrected).IsVerified);
        var compactAxisReopen = CampaignExercises.Query(compactCorrected, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var compactReopened = CampaignExercises.Submit(compactCorrected,
            Submission(compactCorrected, compactAxisReopen.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(0, compactReopened.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedFinalizedCompactCheckpointContractVersion, compactReopened.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(compactReopened).IsVerified);
        var sixteenthBasis = ContentSourceSeventhArmoredGroupedPackArtifact.Create(italianContent);
        var sixteenthContent = ContentSourceSixteenthBrigadeGroupedPackArtifact.Create(sixteenthBasis);
        Assert.Equal(ContentSourceSixteenthBrigadeGroupedPackArtifact.ContentHash, sixteenthContent.Hash());
        var sixteenthResult = CampaignExercises.AdmitSourceSixteenthBrigadeGroupedWorld(seventhConfirmed);
        Assert.True(sixteenthResult.IsAccepted);
        var sixteenthProposed = sixteenthResult.SuccessorSession!;
        var sourceWorld = sixteenthProposed.SourceAuthority!.GroupedWorld!;
        var sixteenth = sourceWorld.Groups.Single(value => value.GroupId == "16-infantry-brigade");
        Assert.Equal(4, sixteenth.Members.Count); Assert.Null(sixteenth.StackValue); Assert.True(sixteenth.UsesRetainedMembers);
        Assert.Equal(4, sourceWorld.Prior.World.Elements.Count(value => sixteenth.Members.Any(member => member.ElementId == value.ElementId)));
        Assert.Single(sourceWorld.OccupantsAt("e1829"), value => value.Formation?.GroupId == sixteenth.GroupId);
        var sixteenthChoice = CampaignExercises.Query(sixteenthProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "sixteenth-brigade-disposition");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(sixteenthProposed,
            Submission(sixteenthProposed, sixteenthChoice.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var sixteenthCommand = Submission(sixteenthProposed, sixteenthChoice.ActionId, CampaignActionAudience.Commonwealth);
        var sixteenthConfirmed = CampaignExercises.Submit(sixteenthProposed, sixteenthCommand).SuccessorSession!;
        Assert.Equal(2, sixteenthConfirmed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == sixteenth.GroupId).StackValue);
        Assert.Equal(41, sixteenthConfirmed.SourceAuthority.ProfileVersion);
        var playableWorld = CampaignSourcePlayableWorld.Create(sixteenthConfirmed.SourceAuthority);
        var mapBoundWorld = CampaignSourcePlayableWorld.Create(sixteenthConfirmed.SourceAuthority, mapIdentityBound: true);
        var cdWorld = CampaignSourcePlayableWorld.Create(sixteenthConfirmed.SourceAuthority, mapIdentityBound: true, cdSeamBound: true);
        Assert.True(mapBoundWorld.MapIdentityBound);
        Assert.True(mapBoundWorld.WithWorld(mapBoundWorld.GroupedWorld, mapBoundWorld.Battlefield).MapIdentityBound);
        Assert.False(playableWorld.MapIdentityBound);
        // Test the wire contract on the retained historical world without
        // claiming that the still-gated campaign can start yet.
        var turnConstructor = typeof(CampaignSourceTurnAuthority).GetConstructors(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Single();
        CampaignSourceTurnAuthority ContractTurn(CampaignSourcePlayableWorld world, LandSequencePosition? testPosition = null,
            int? organizationMask = null, CampaignSourceTrainingLedger? training = null,
            CampaignSourceGt1AxisArrival? axisArrival = null, LandSide? firstSide = null,
            (int GameTurn, int OperationStage)? paidStage = null)
        {
            var args = turnConstructor.GetParameters().Select(parameter => parameter.Name == "position" && testPosition is not null
                ? testPosition : parameter.Name == "voluntaryOrganizationDeclinedMask" && organizationMask is not null
                ? organizationMask : parameter.Name == "training" && training is not null ? training
                : parameter.Name == "axisArrival" && axisArrival is not null ? axisArrival
                : parameter.Name == "fourthIndianPaidStage" && paidStage is not null ? paidStage
                : parameter.Name == "stageOneFirstSide" && firstSide is not null ? firstSide : parameter.HasDefaultValue
                ? parameter.DefaultValue : parameter.Name switch
                {
                    "world" => world,
                    "stateVersion" => world.Source.StateVersion + 1,
                    "position" => Cna1979LandSequenceV4.CreateTurn(1)[0],
                    "randomState" => world.RandomState,
                    "initiative" => null,
                    _ => throw new InvalidOperationException("Unexpected source-turn constructor parameter."),
                }).ToArray();
            return (CampaignSourceTurnAuthority)turnConstructor.Invoke(args);
        }
        var mapTurn = ContractTurn(mapBoundWorld);
        var cdTurn = ContractTurn(cdWorld);
        Assert.Equal(3, new ExerciseCheckpoint(cdTurn).ContractVersion);
        Assert.True(cdWorld.WithWorld(cdWorld.GroupedWorld, cdWorld.Battlefield).CdSeamBound);
        using (var cdCheckpoint = JsonDocument.Parse(cdTurn.SerializeCheckpoint()))
        {
            Assert.Equal(3, cdCheckpoint.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(Cna1979HistoricalMap.CdSeamPolicyHash,
                cdCheckpoint.RootElement.GetProperty("historicalMap").GetProperty("cdSeamPolicyHash").GetString());
        }
        var legacyTurn = ContractTurn(playableWorld);
        var turnContacts = sixteenthConfirmed.CurrentContacts.AdvancePregameSource(mapTurn.Source.StateVersion);
        var turnSession = new ExerciseSession(mapTurn.Source.Pregame, turnContacts,
            sixteenthConfirmed.PregameHistory, mapTurn.Source, mapTurn);
        var initiativeResult = CampaignExercises.ResolveSourceInitiative(turnSession);
        Assert.True(initiativeResult.IsAccepted);
        Assert.Equal(initiativeResult.SuccessorSession!.SourceTurn!.Position.PositionId,
            initiativeResult.Evidence!.Receipt.ResultingPositionId);
        Assert.Equal(CampaignSourceTurnAuthority.InitiativeActionId, initiativeResult.Evidence.Receipt.ActionId);
        var organizationCut = Cna1979LandSequenceV4.CreateTurn(1).Single(value =>
            value.OperationStage == 1 && value.PhaseId == LandPhaseIds.Organization);
        var interrupted = CampaignSourceTrainingLedger.CreateSixthAustralian().Interrupt();
        var waitingOrganization = ContractTurn(mapBoundWorld, organizationCut, 1, interrupted);
        Assert.Throws<InvalidOperationException>(() => waitingOrganization.CompleteGt1Organization());
        Assert.Throws<InvalidOperationException>(() => ContractTurn(mapBoundWorld, organizationCut, 3).CompleteGt1Organization());
        var readyOrganization = ContractTurn(mapBoundWorld, organizationCut, 3, interrupted);
        var completedOrganization = readyOrganization.CompleteGt1Organization();
        Assert.Equal(Cna1979LandSequenceV4.GetNext(organizationCut), completedOrganization.Position);
        Assert.Same(readyOrganization.Training, completedOrganization.Training);
        Assert.Equal(readyOrganization.RandomState, completedOrganization.RandomState);
        Assert.Same(readyOrganization.World, completedOrganization.World);
        Assert.Equal(readyOrganization.StateVersion + 1, completedOrganization.StateVersion);
        Assert.Throws<InvalidOperationException>(() => completedOrganization.CompleteGt1Organization());
        // Exercise the real source session transition on retained historical
        // setup without claiming that the campaign-start gate has opened.
        var organizationContacts = sixteenthConfirmed.CurrentContacts.AdvancePregameSource(readyOrganization.Source.StateVersion);
        var organizationSession = new ExerciseSession(readyOrganization.Source.Pregame, organizationContacts,
            sixteenthConfirmed.PregameHistory, readyOrganization.Source, readyOrganization);
        var organizationResult = CampaignExercises.CompleteSourceGt1Organization(organizationSession);
        Assert.True(organizationResult.IsAccepted);
        Assert.Equal(completedOrganization.Position.PositionId, organizationResult.Evidence!.Receipt.ResultingPositionId);
        Assert.True(CampaignExercises.IsSourceGt1OrganizationCompleted(organizationResult.SuccessorSession!));
        Assert.Equal(completedOrganization.Position, organizationResult.SuccessorSession!.SourceTurn!.Position);
        var organizationCommand = CampaignHostAdapter.SourceTurnSystemCommand(organizationSession, organizationResult);
        var organizationStep = new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(organizationCommand), organizationResult.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(organizationResult.Evidence.Receipt),
            organizationResult.Evidence.SnapshotCheckpoint, CampaignHostAcceptedStep.SourceGt1OrganizationCompleteKind);
        Assert.Equal(CampaignHostAcceptedStep.SourceGt1OrganizationCompleteContractVersion, organizationStep.ContractVersion);
        using (var organizationEvent = JsonDocument.Parse(Assert.Single(organizationResult.Evidence!.EventRecords)))
        {
            Assert.Equal("source-gt1-organization-completed", organizationEvent.RootElement.GetProperty("eventType").GetString());
            Assert.Equal(CampaignSourceTurnAuthority.CompleteGt1OrganizationActionId, organizationEvent.RootElement.GetProperty("actionId").GetString());
        }
        Assert.Equal(2, new ExerciseCheckpoint(mapTurn).ContractVersion);
        Assert.Equal(1, new ExerciseCheckpoint(legacyTurn).ContractVersion);
        using (var boundCheckpoint = System.Text.Json.JsonDocument.Parse(mapTurn.SerializeCheckpoint()))
        using (var legacyCheckpoint = System.Text.Json.JsonDocument.Parse(legacyTurn.SerializeCheckpoint()))
        {
            Assert.Equal(2, boundCheckpoint.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(Cna1979HistoricalMap.HexHash, boundCheckpoint.RootElement.GetProperty("historicalMap").GetProperty("hexSha256").GetString());
            Assert.Equal(Cna1979HistoricalMap.EdgeHash, boundCheckpoint.RootElement.GetProperty("historicalMap").GetProperty("hexsideSha256").GetString());
            Assert.Equal(1, legacyCheckpoint.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.False(legacyCheckpoint.RootElement.TryGetProperty("historicalMap", out _));
        }
        using (var boundEvent = System.Text.Json.JsonDocument.Parse(mapTurn.SerializeEvent("source-turn-started",
                   CampaignSourceTurnAuthority.StartActionId, mapBoundWorld.Source.StateVersion,
                   mapBoundWorld.Source.SerializeCheckpoint())))
        {
            Assert.Equal(2, boundEvent.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(CampaignPregameSourceCreation.Digest(mapTurn.SerializeCheckpoint()),
                boundEvent.RootElement.GetProperty("checkpointHash").GetString());
        }
        using (var legacyEvent = System.Text.Json.JsonDocument.Parse(legacyTurn.SerializeEvent("source-turn-started",
                   CampaignSourceTurnAuthority.StartActionId, playableWorld.Source.StateVersion,
                   playableWorld.Source.SerializeCheckpoint())))
            Assert.Equal(1, legacyEvent.RootElement.GetProperty("contractVersion").GetInt32());
        Assert.True(mapTurn.ResolveInitiative().World.MapIdentityBound);
        Assert.Same(sixteenthConfirmed.SourceAuthority.GroupedWorld, playableWorld.GroupedWorld);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.RandomState, playableWorld.RandomState);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.World.PendingArrivals, playableWorld.World8.PendingArrivals);
        foreach (var entry in playableWorld.Battlefield.Entries.Where(value => value.LocationId is { Length: 5 }
                     && value.LocationId[0] is >= 'a' and <= 'e' && value.LocationId.Skip(1).All(char.IsAsciiDigit)))
            Assert.NotNull(Cna1979HistoricalMap.Instance.HexAt(entry.LocationId!));
        foreach (var location in Cna1979GrazianiSourceFacts.CairoEntryArea.LocationIds)
            Assert.Equal(Cna1979SourceContentVocabulary.MajorCityTerrainId, playableWorld.KnownTerrainAt(location));
        var polishArrivalPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        Assert.Contains("e1829", CampaignSourcePolishArrival.ValidDestinations(playableWorld, polishArrivalPosition));
        Assert.Null(playableWorld.KnownTerrainAt("e1932"));
        Assert.Single(playableWorld.OccupantsAt("e1829"), value => value.Formation?.GroupId == sixteenth.GroupId);
        Assert.DoesNotContain(playableWorld.UnresolvedAt("e1829"), value => sixteenth.Members.Any(member => member.ElementId == value.ElementId));
        Assert.False(playableWorld.IsGameplayReady);
        Assert.Contains("battlefield-incomplete", playableWorld.ReadinessBlockers());
        Assert.Contains("control-incomplete", playableWorld.ReadinessBlockers());
        Assert.Contains("map-incomplete", playableWorld.ReadinessBlockers());
        Assert.Same(playableWorld.GroupedWorld, playableWorld.WithWorld(playableWorld.GroupedWorld, playableWorld.Battlefield).GroupedWorld);
        var sourceSupply = playableWorld.Source.CommonwealthSupplyWorld;
        CampaignSourceFourthIndianRoad.RequirePinnedSource(playableWorld);
        Assert.Equal(new CapabilityPointAmount(1, 1), CampaignSourceFourthIndianRoad.Cost(false));
        Assert.Equal(new CapabilityPointAmount(1, 2), CampaignSourceFourthIndianRoad.Cost(true));
        Assert.Equal(new CapabilityPointAmount(1, 1), CampaignSourceFourthIndianRoad.SupplyTraceCost);
        var paidSupply = sourceSupply.SpendFuel("cw-matruh-1", "d3714", 2);
        Assert.Equal(60, Assert.Single(sourceSupply.Units, value => value.Id == "cw-matruh-1").Fuel);
        Assert.Equal(58, Assert.Single(paidSupply.Units, value => value.Id == "cw-matruh-1").Fuel);
        Assert.Throws<InvalidOperationException>(() => sourceSupply.SpendFuel("cw-matruh-1", "d3615", 2));
        Assert.Throws<InvalidOperationException>(() => sourceSupply.SpendFuel("cw-matruh-1", "d3714", 61));
        Assert.Throws<InvalidOperationException>(() => sourceSupply.SpendFuel("not-a-counter", "d3714", 2));
        var movementCatalog = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.ActorRole == LandActorRole.FirstActingSide && value.SegmentId == LandSegmentIds.Movement);
        var movementPosition = new LandSequencePosition(movementCatalog.ContractVersion, movementCatalog.PositionId,
            movementCatalog.GameTurn, movementCatalog.OperationStage, movementCatalog.StageId, movementCatalog.PhaseId,
            movementCatalog.SegmentId, movementCatalog.StepId, movementCatalog.ActorRole, LandSide.Commonwealth, movementCatalog.Sources);
        var season = Cna1979Weather.GetSeason(1);
        var normalRoll = Enumerable.Range(11, 56).First(value => value % 10 is >= 1 and <= 6
            && value / 10 is >= 1 and <= 6 && Cna1979Weather.GetKind(season, value) == WeatherKind.Normal);
        var normalWeather = new CampaignOperationStageWeather(1, 1, 1, LandSide.Axis, season,
            normalRoll / 10, normalRoll % 10, WeatherKind.Normal, WeatherScope.None, null, [], 0, 0, 0);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceFourthIndianMovement.Apply(playableWorld,
            sourceSupply, null, movementPosition, normalWeather, "cw-matruh-1"));
        Assert.Equal(60, Assert.Single(sourceSupply.Units, value => value.Id == "cw-matruh-1").Fuel);
        // Exercise the source-bound mutation and contact projection directly;
        // source readiness still prevents this from becoming a playable move.
        var fourth = Assert.Single(playableWorld.GroupedWorld.Groups,
            value => value.GroupId == CampaignSourceFourthIndianMovement.GroupId);
        var fourthCosts = fourth.Members.ToDictionary(value => value.ElementId,
            value => new CapabilityPointAmount(1, 1), StringComparer.Ordinal);
        var movedFourth = playableWorld.GroupedWorld.MoveFormation(fourth.GroupId, "d3715", fourthCosts, 5, 1, 1);
        var movedBattlefield = playableWorld.Battlefield.MoveFormation(playableWorld.GroupedWorld, movedFourth, fourth.GroupId);
        var movedWorld = playableWorld.WithWorld(movedFourth, movedBattlefield);
        Assert.True(mapBoundWorld.WithWorld(movedFourth, movedBattlefield).MapIdentityBound);
        var unknownMove = playableWorld.GroupedWorld.MoveFormation(fourth.GroupId, "c9999", fourthCosts, 5, 1, 1);
        var unknownBattlefield = playableWorld.Battlefield.MoveFormation(playableWorld.GroupedWorld, unknownMove, fourth.GroupId);
        Assert.Contains(unknownBattlefield.Entries, entry => entry.LocationId == "c9999");
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePlayableWorld.RequireKnownMapLocations(
            unknownBattlefield.Entries.Select(entry => entry.LocationId)));
        Assert.Throws<InvalidOperationException>(() => mapBoundWorld.WithWorld(unknownMove, unknownBattlefield));
        Assert.All(fourth.Members, member => Assert.Equal("d3715",
            Assert.Single(movedWorld.Battlefield.Entries, entry => entry.ElementId == member.ElementId).LocationId));
        Assert.All(fourth.Members, member => Assert.Equal("d3615",
            Assert.Single(playableWorld.Battlefield.Entries, entry => entry.ElementId == member.ElementId).LocationId));
        var polishPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePolishArrival.Apply(playableWorld,
            polishPosition, LandSide.Commonwealth, "e1730"));
        Assert.Equal(sixteenthConfirmed.SourceAuthority.World.PendingArrivals, playableWorld.World8.PendingArrivals);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.RandomState, playableWorld.RandomState);
        var polishGrouped = CampaignSourcePolishArrival.Materialize(playableWorld.GroupedWorld, playableWorld.Source.Content, "e1829");
        var polishBattlefield = playableWorld.Battlefield.ArriveRetainedFormation(playableWorld.GroupedWorld,
            polishGrouped, CampaignSourcePolishArrival.GroupId, "e1829");
        var polishSuccessor = playableWorld.WithWorld(polishGrouped, polishBattlefield);
        var mapBoundPolishSuccessor = mapBoundWorld.WithWorld(polishGrouped, polishBattlefield);
        Assert.True(mapBoundPolishSuccessor.MapIdentityBound);
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(mapBoundWorld, mapBoundPolishSuccessor,
            mapBoundWorld.Source.StateVersion + 1, new CampaignContactKnowledge(mapBoundWorld.Source.Request.CampaignId, LandSide.Axis)));
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(mapBoundWorld, mapBoundPolishSuccessor,
            mapBoundWorld.Source.StateVersion + 1, new CampaignContactKnowledge(mapBoundWorld.Source.Request.CampaignId, LandSide.Commonwealth)));
        Assert.DoesNotContain(polishSuccessor.World8.PendingArrivals, value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.DoesNotContain(polishSuccessor.Battlefield.Pending, value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.Contains(polishSuccessor.World8.PendingArrivals, value => value.GroupId == Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId);
        Assert.Equal(8, polishSuccessor.World8.World.Elements.Count(value => Cna1979GrazianiSourceFacts.PolishBrigadeArrival.ElementIds.Contains(value.ElementId)));
        Assert.Equal(playableWorld.RandomState, polishSuccessor.RandomState);
        Assert.False(polishSuccessor.IsGameplayReady);
        Assert.Contains(playableWorld.World8.PendingArrivals, value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(playableWorld, polishSuccessor,
            playableWorld.Source.StateVersion + 1, new CampaignContactKnowledge(playableWorld.Source.Request.CampaignId, LandSide.Axis)));
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(playableWorld, polishSuccessor,
            playableWorld.Source.StateVersion + 1, new CampaignContactKnowledge(playableWorld.Source.Request.CampaignId, LandSide.Commonwealth)));
        var begunEarlyPlan = CampaignExercises.SubmitSourceSeptemberSupply(sixteenthConfirmed,
            Submission(sixteenthConfirmed, CampaignSourceAuthorityState.SeptemberSupplyActionId("begin", 0, 0, 0, 0), CampaignActionAudience.Axis)).SuccessorSession!;
        var rolledEarlyPlan = CampaignExercises.SubmitSourceSeptemberSupply(begunEarlyPlan,
            Submission(begunEarlyPlan, CampaignSourceAuthorityState.SeptemberSupplyActionId("roll", 3, 0, 0, 0), CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Contains("axis-prestart-supply-plan-incomplete", CampaignSourcePlayableWorld.Create(rolledEarlyPlan.SourceAuthority!).ReadinessBlockers());
        var finishedEarlyPlan = CampaignExercises.SubmitSourceSeptemberSupply(rolledEarlyPlan,
            Submission(rolledEarlyPlan, CampaignSourceAuthorityState.SeptemberSupplyActionId("finish", 3, 0, 0, 0), CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.DoesNotContain("axis-prestart-supply-plan-incomplete", CampaignSourcePlayableWorld.Create(finishedEarlyPlan.SourceAuthority!).ReadinessBlockers());
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(sixteenthConfirmed, sixteenthCommand).RejectionReason);
        Assert.Equal(seventhConfirmed.SourceAuthority.World.PendingArrivals, sixteenthConfirmed.SourceAuthority.World.PendingArrivals);
        Assert.True(CampaignExercises.Reconstruct(sixteenthConfirmed).IsVerified);
        var sixteenthHistory = sixteenthConfirmed.PregameHistory.Select(value => value.ToArray()).ToArray();
        var sixteenthEvent = System.Text.Encoding.UTF8.GetString(sixteenthHistory[^1]);
        using (var eventDocument = System.Text.Json.JsonDocument.Parse(sixteenthHistory[^1]))
        {
            var worldHash = eventDocument.RootElement.GetProperty("worldHash").GetString()!;
            var alteredHash = worldHash[..^1] + (worldHash[^1] == '0' ? '1' : '0');
            sixteenthHistory[^1] = System.Text.Encoding.UTF8.GetBytes(sixteenthEvent.Replace(worldHash, alteredHash, StringComparison.Ordinal));
        }
        var tamperedSixteenth = new ExerciseSession(sixteenthConfirmed.Pregame!, sixteenthConfirmed.CurrentContacts,
            sixteenthHistory, sixteenthConfirmed.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(tamperedSixteenth).IsVerified);
        var catanzaroResult = CampaignExercises.AdmitSourceCatanzaroGroupedWorld(sixteenthConfirmed);
        Assert.True(catanzaroResult.IsAccepted);
        var catanzaroProposed = catanzaroResult.SuccessorSession!;
        var catanzaro = catanzaroProposed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "catanzaro-division");
        Assert.Equal(13, catanzaro.Members.Count);
        Assert.Null(catanzaro.StackValue);
        Assert.Equal(35, catanzaro.MotorPool);
        var catanzaroChoice = CampaignExercises.Query(catanzaroProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "catanzaro-disposition");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(catanzaroProposed,
            Submission(catanzaroProposed, catanzaroChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var catanzaroCommand = Submission(catanzaroProposed, catanzaroChoice.ActionId, CampaignActionAudience.Axis);
        var catanzaroConfirmed = CampaignExercises.Submit(catanzaroProposed, catanzaroCommand).SuccessorSession!;
        Assert.Equal(42, catanzaroConfirmed.SourceAuthority!.ProfileVersion);
        var axisFinish = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var axisFinished = CampaignExercises.Submit(catanzaroConfirmed,
            Submission(catanzaroConfirmed, axisFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwFinishAfterAxis = CampaignExercises.Query(axisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var bothFinishedBeforeCorrection = CampaignExercises.Submit(axisFinished,
            Submission(axisFinished, cwFinishAfterAxis.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(3, bothFinishedBeforeCorrection.SourceAuthority!.FinalizedOwners);
        var correctedFinalized = CampaignExercises.CorrectSourceSeventhShell(bothFinishedBeforeCorrection).SuccessorSession!;
        Assert.Equal(1, correctedFinalized.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedFinalizedLegacyCheckpointContractVersion, correctedFinalized.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(correctedFinalized).IsVerified);
        var choicesBefore = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "sixth-australian-placement")
            .Select(value => value.ChoiceJson).Order(StringComparer.Ordinal).ToArray();
        var choicesAfter = CampaignExercises.Query(axisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "sixth-australian-placement")
            .Select(value => value.ChoiceJson).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(choicesBefore, choicesAfter);
        var australianChoice = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "sixth-australian-placement"
                && value.SourceActionId == CampaignSourceAuthorityState.SixthAustralianActionId("e1829"));
        Assert.DoesNotContain(CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "sixth-australian-placement"
                && value.SourceActionId == CampaignSourceAuthorityState.SixthAustralianActionId(chosenCairoHex));
        var australianSession = CampaignExercises.Submit(catanzaroConfirmed,
            Submission(catanzaroConfirmed, australianChoice.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var australian = australianSession.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "6th-australian-division");
        Assert.Equal(10, australian.Members.Count);
        Assert.Equal(5, australian.StackValue);
        Assert.False(australian.MotorPoolKnown);
        var australianInfantry = australian.Members.Where(value => value.ComponentType == "infantry").ToArray();
        Assert.Equal(6, australianInfantry.Length);
        Assert.All(australianInfantry, value => { Assert.Equal(6, value.ComponentToe); Assert.Equal(10, value.CapabilityPointAllowance); });
        foreach (var id in new[] { "16-australian-infantry-brigade-hq", "17-australian-infantry-brigade-hq" })
        {
            var headquarters = australian.Members.Single(value => value.ElementId == id);
            Assert.Equal(1, headquarters.ComponentToe); Assert.Equal(20, headquarters.CapabilityPointAllowance);
        }
        var cavalry = australian.Members.Single(value => value.ElementId == "6-australian-divisional-cavalry");
        Assert.Equal(8, cavalry.ComponentToe); Assert.Equal(45, cavalry.CapabilityPointAllowance);
        Assert.Equal(catanzaroConfirmed.SourceAuthority!.RandomState, australianSession.SourceAuthority.RandomState);
        Assert.Equal(45, australianSession.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(australianSession).IsVerified);
        var axisBatchChoice = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "ccnn-batch-disposition");
        var afterAustralianAxisChoice = CampaignExercises.Query(australianSession, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "ccnn-batch-disposition");
        Assert.Equal(axisBatchChoice.ChoiceJson, afterAustralianAxisChoice.ChoiceJson);
        var australianFirst = CampaignExercises.Submit(australianSession,
            Submission(australianSession, afterAustralianAxisChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var fullyMotorized = australianFirst;
        for (var index = 0; index < 5; index++)
        {
            var adjustment = CampaignExercises.SubmitSourceFourthIndian(fullyMotorized,
                Submission(fullyMotorized, priorityAction, CampaignActionAudience.Commonwealth));
            Assert.True(adjustment.IsAccepted);
            fullyMotorized = adjustment.SuccessorSession!;
        }
        var motorizedWorld = CampaignSourcePlayableWorld.Create(fullyMotorized.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true);
        Assert.Contains("seventh-armored-shell-stack-correction-required", motorizedWorld.ReadinessBlockers());
        Assert.Contains("owner-setup-finalization-incomplete", motorizedWorld.ReadinessBlockers());
        Assert.Contains("central-india-horse-training-state-unresolved", motorizedWorld.ReadinessBlockers());
        var axisHorseApproved = Act(fullyMotorized, CampaignActionAudience.Axis, "central-india-horse-ruling", "approve");
        var cwHorseApproved = Act(axisHorseApproved, CampaignActionAudience.Commonwealth, "central-india-horse-ruling", "approve");
        Assert.Equal(3, cwHorseApproved.SourceAuthority!.HorseRulingVotes);
        var horseStageOneDeclined = Act(cwHorseApproved, CampaignActionAudience.Commonwealth, "central-india-horse-stage-one", "decline-stage1");
        Assert.True(horseStageOneDeclined.SourceAuthority!.HorseStageOneResolved);
        var shellCorrected = CampaignExercises.CorrectSourceSeventhShell(horseStageOneDeclined).SuccessorSession!;
        Assert.True(shellCorrected.SourceAuthority!.IsSeventhShellCorrected);
        var axisFinalizedForReadiness = Act(shellCorrected, CampaignActionAudience.Axis, "owner-finalization", "finish");
        var bothFinalizedForReadiness = Act(axisFinalizedForReadiness, CampaignActionAudience.Commonwealth, "owner-finalization", "finish");
        Assert.Equal(3, bothFinalizedForReadiness.SourceAuthority!.FinalizedOwners);
        var readyWorld = CampaignSourcePlayableWorld.Create(bothFinalizedForReadiness.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true);
        Assert.DoesNotContain("seventh-armored-shell-stack-correction-required", readyWorld.ReadinessBlockers());
        Assert.DoesNotContain("owner-setup-finalization-incomplete", readyWorld.ReadinessBlockers());
        Assert.DoesNotContain("central-india-horse-training-state-unresolved", readyWorld.ReadinessBlockers());
        var openingGroup = motorizedWorld.GroupedWorld.Groups.Single(value => value.GroupId == CampaignSourceFourthIndianMovement.GroupId);
        Assert.True(CampaignSourceFourthIndianMovement.IsOpeningFormation(openingGroup));
        var openingMoved = motorizedWorld.GroupedWorld.MoveFormation(openingGroup.GroupId, CampaignSourceFourthIndianRoad.Destination,
            CampaignSourceFourthIndianMovement.OpeningCosts(openingGroup), CampaignSourceFourthIndianRoad.RoadStackLimit, 1, 1);
        var openingBattlefield = motorizedWorld.Battlefield.MoveFormation(motorizedWorld.GroupedWorld, openingMoved, openingGroup.GroupId);
        var openingWorld = motorizedWorld.WithWorld(openingMoved, openingBattlefield);
        var breakdownPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.ActorRole == LandActorRole.FirstActingSide && value.SegmentId == LandSegmentIds.BreakdownDetermination);
        var movedBreakdown = ContractTurn(openingWorld, breakdownPosition, 3, interrupted,
            firstSide: LandSide.Commonwealth, paidStage: (1, 1));
        var afterMovedBreakdown = movedBreakdown.CompleteGt1FirstBreakdown();
        Assert.Equal(LandStepIds.PositionDetermination, afterMovedBreakdown.Position.StepId);
        Assert.Same(openingWorld, afterMovedBreakdown.World);
        var unexpectedMove = motorizedWorld.GroupedWorld.MoveFormation(openingGroup.GroupId, "d3716",
            CampaignSourceFourthIndianMovement.OpeningCosts(openingGroup), CampaignSourceFourthIndianRoad.RoadStackLimit, 1, 1);
        var unexpectedBattlefield = motorizedWorld.Battlefield.MoveFormation(motorizedWorld.GroupedWorld, unexpectedMove, openingGroup.GroupId);
        var unexpectedWorld = motorizedWorld.WithWorld(unexpectedMove, unexpectedBattlefield);
        Assert.Throws<InvalidOperationException>(() => ContractTurn(unexpectedWorld, breakdownPosition, 3, interrupted,
            firstSide: LandSide.Commonwealth, paidStage: (1, 1)).CompleteGt1FirstBreakdown());
        // Exercise the Organization choices against the retained real
        // Australian formation; turn start itself remains readiness-gated.
        var australianWorld = CampaignSourcePlayableWorld.Create(australianFirst.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true);
        var australianTurn = ContractTurn(australianWorld, organizationCut);
        var australianContacts = australianFirst.CurrentContacts.AdvancePregameSource(australianTurn.Source.StateVersion);
        var australianOrganization = new ExerciseSession(australianTurn.Source.Pregame, australianContacts,
            australianFirst.PregameHistory, australianTurn.Source, australianTurn);
        var beforeInterrupt = CampaignExercises.Query(australianOrganization, CampaignActionAudience.Commonwealth).ActionSet!.Candidates;
        Assert.DoesNotContain(beforeInterrupt, action => action.Kind == "source-voluntary-organization-decline");
        var interrupt = Assert.Single(beforeInterrupt, action => action.Kind == "source-sixth-australian-training-interrupt");
        var afterInterrupt = CampaignExercises.Submit(australianOrganization,
            Submission(australianOrganization, interrupt.ActionId, CampaignActionAudience.Commonwealth));
        Assert.True(afterInterrupt.IsAccepted);
        Assert.True(afterInterrupt.SuccessorSession!.SourceTurn!.Training.Interrupted);
        Assert.Contains(CampaignExercises.Query(afterInterrupt.SuccessorSession, CampaignActionAudience.Commonwealth).ActionSet!.Candidates,
            action => action.Kind == "source-voluntary-organization-decline");
        var cwDecline = CampaignExercises.Query(afterInterrupt.SuccessorSession, CampaignActionAudience.Commonwealth)
            .ActionSet!.Candidates.Single(action => action.Kind == "source-voluntary-organization-decline");
        var cwFinished = CampaignExercises.Submit(afterInterrupt.SuccessorSession,
            Submission(afterInterrupt.SuccessorSession, cwDecline.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.False(CampaignExercises.CompleteSourceGt1Organization(cwFinished).IsAccepted);
        var axisDecline = CampaignExercises.Query(cwFinished, CampaignActionAudience.Axis)
            .ActionSet!.Candidates.Single(action => action.Kind == "source-voluntary-organization-decline");
        var bothOrganizationOwnersFinished = CampaignExercises.Submit(cwFinished,
            Submission(cwFinished, axisDecline.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var completedRealOrganization = CampaignExercises.CompleteSourceGt1Organization(bothOrganizationOwnersFinished);
        Assert.True(completedRealOrganization.IsAccepted);
        Assert.Equal(Cna1979LandSequenceV4.GetNext(organizationCut), completedRealOrganization.SuccessorSession!.SourceTurn!.Position);
        Assert.True(completedRealOrganization.SuccessorSession.SourceTurn.Training.Interrupted);
        var stageOneArrival = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        var arrivalTurn = ContractTurn(australianWorld, stageOneArrival, 3, interrupted, firstSide: LandSide.Commonwealth);
        var dueStageOne = arrivalTurn.Source.OpeningConvoyPlan.Allocations().Where(value => value.OperationStage == 1).ToArray();
        if (dueStageOne.Length != 0)
        {
            Assert.Throws<InvalidOperationException>(() => arrivalTurn.CompleteGt1StageOneArrivals());
            if (dueStageOne.Length == 1 && dueStageOne[0].Lane == 2)
            {
                var deliveredAxis = CampaignSourceGt1AxisArrival.Create(arrivalTurn.Source).Announce().DeclineAttackAndDeliver();
                var deliveredTurn = ContractTurn(australianWorld, stageOneArrival, 3, interrupted, deliveredAxis);
                var afterDelivery = deliveredTurn.CompleteGt1StageOneArrivals();
                Assert.Equal(Cna1979LandSequenceV4.GetNext(stageOneArrival), afterDelivery.Position);
                Assert.Equal(deliveredAxis.Supply.Hash(), afterDelivery.AxisArrival.Supply.Hash());
            }
        }
        else
        {
            var arrivalContacts = australianFirst.CurrentContacts.AdvancePregameSource(arrivalTurn.Source.StateVersion);
            var arrivalSession = new ExerciseSession(arrivalTurn.Source.Pregame, arrivalContacts,
                australianFirst.PregameHistory, arrivalTurn.Source, arrivalTurn);
            var arrivalComplete = CampaignExercises.CompleteSourceGt1StageOneArrivals(arrivalSession);
            Assert.True(arrivalComplete.IsAccepted);
            Assert.Equal(Cna1979LandSequenceV4.GetNext(stageOneArrival), arrivalComplete.SuccessorSession!.SourceTurn!.Position);
            Assert.Equal(arrivalComplete.SuccessorSession.SourceTurn.Position.PositionId, arrivalComplete.Evidence!.Receipt.ResultingPositionId);
            Assert.True(CampaignExercises.IsSourceGt1StageOneArrivalsCompleted(arrivalComplete.SuccessorSession));
            var arrivalCommand = CampaignHostAdapter.SourceTurnSystemCommand(arrivalSession, arrivalComplete);
            var arrivalStep = new CampaignHostAcceptedStep(CampaignActionSubmissionSerializer.SerializeCanonical(arrivalCommand),
                arrivalComplete.Evidence.EventRecords, CampaignActionAcceptanceReceiptSerializer.Serialize(arrivalComplete.Evidence.Receipt),
                arrivalComplete.Evidence.SnapshotCheckpoint, CampaignHostAcceptedStep.SourceGt1ArrivalsCompleteKind);
            Assert.Equal(CampaignHostAcceptedStep.SourceGt1ArrivalsCompleteContractVersion, arrivalStep.ContractVersion);
            var fleet = CampaignExercises.CompleteSourceGt1StageOneFleet(arrivalComplete.SuccessorSession);
            Assert.True(fleet.IsAccepted);
            var repairPosition = Cna1979LandSequenceV4.GetNext(arrivalComplete.SuccessorSession.SourceTurn.Position);
            Assert.Equal(LandSegmentIds.FleetRepair, repairPosition.SegmentId);
            Assert.Equal(Cna1979LandSequenceV4.GetNext(repairPosition), fleet.SuccessorSession!.SourceTurn!.Position);
            Assert.Equal(fleet.SuccessorSession.SourceTurn.Position.PositionId, fleet.Evidence!.Receipt.ResultingPositionId);
            Assert.True(CampaignExercises.IsSourceGt1StageOneFleetCompleted(fleet.SuccessorSession));
            Assert.False(CampaignExercises.CompleteSourceGt1StageOneFleet(fleet.SuccessorSession).IsAccepted);
            var firstReserveSession = fleet.SuccessorSession;
            Assert.Empty(CampaignExercises.Query(firstReserveSession, CampaignActionAudience.Axis).ActionSet!.Candidates);
            var reserveChoice = Assert.Single(CampaignExercises.Query(firstReserveSession, CampaignActionAudience.Commonwealth).ActionSet!.Candidates,
                value => value.Kind == "source-stage1-first-reserves-decline");
            var visibleReserves = CampaignExercises.QuerySourceActionsDraft(firstReserveSession, CampaignActionAudience.Commonwealth);
            Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(firstReserveSession.SourceTurn!.StateVersion,
                visibleReserves.VisibleBytes, 0, reserveChoice.ActionId));
            var noReserves = CampaignExercises.Submit(firstReserveSession,
                Submission(firstReserveSession, reserveChoice.ActionId, CampaignActionAudience.Commonwealth));
            Assert.True(noReserves.IsAccepted);
            Assert.Equal(LandSegmentIds.Movement, noReserves.SuccessorSession!.SourceTurn!.Position.SegmentId);
            Assert.Equal(LandSide.Commonwealth, noReserves.SuccessorSession.SourceTurn.Position.ActiveSide);
            Assert.Equal(noReserves.SuccessorSession.SourceTurn.Position.PositionId, noReserves.Evidence!.Receipt.ResultingPositionId);
            var movementSession = noReserves.SuccessorSession;
            Assert.Empty(CampaignExercises.Query(movementSession, CampaignActionAudience.Axis).ActionSet!.Candidates);
            var finish = Assert.Single(CampaignExercises.Query(movementSession, CampaignActionAudience.Commonwealth).ActionSet!.Candidates,
                value => value.Kind == "source-stage1-first-movement-finish");
            var movementDraft = CampaignExercises.QuerySourceActionsDraft(movementSession, CampaignActionAudience.Commonwealth);
            Assert.True(CampaignHostAdapter.MatchesIssuedSourceAction(movementSession.SourceTurn!.StateVersion,
                movementDraft.VisibleBytes, 0, finish.ActionId));
            var movementFinished = CampaignExercises.Submit(movementSession,
                Submission(movementSession, finish.ActionId, CampaignActionAudience.Commonwealth));
            Assert.True(movementFinished.IsAccepted);
            Assert.Equal(LandSegmentIds.BreakdownDetermination, movementFinished.SuccessorSession!.SourceTurn!.Position.SegmentId);
            Assert.Same(movementSession.SourceTurn.World, movementFinished.SuccessorSession.SourceTurn.World);
            Assert.Same(movementSession.SourceTurn.CommonwealthSupply, movementFinished.SuccessorSession.SourceTurn.CommonwealthSupply);
            Assert.Equal(movementFinished.SuccessorSession.SourceTurn.Position.PositionId, movementFinished.Evidence!.Receipt.ResultingPositionId);
            var breakdownFinished = CampaignExercises.CompleteSourceGt1FirstBreakdown(movementFinished.SuccessorSession);
            Assert.True(breakdownFinished.IsAccepted);
            Assert.Equal(LandSegmentIds.Combat, breakdownFinished.SuccessorSession!.SourceTurn!.Position.SegmentId);
            Assert.Equal(LandStepIds.PositionDetermination, breakdownFinished.SuccessorSession.SourceTurn.Position.StepId);
            Assert.Equal(movementFinished.SuccessorSession.SourceTurn.RandomState, breakdownFinished.SuccessorSession.SourceTurn.RandomState);
            Assert.Same(movementFinished.SuccessorSession.SourceTurn.World, breakdownFinished.SuccessorSession.SourceTurn.World);
            Assert.True(CampaignExercises.IsSourceGt1FirstBreakdownCompleted(breakdownFinished.SuccessorSession));
            var breakdownCommand = CampaignHostAdapter.SourceTurnSystemCommand(movementFinished.SuccessorSession, breakdownFinished);
            var breakdownStep = new CampaignHostAcceptedStep(CampaignActionSubmissionSerializer.SerializeCanonical(breakdownCommand),
                breakdownFinished.Evidence!.EventRecords, CampaignActionAcceptanceReceiptSerializer.Serialize(breakdownFinished.Evidence.Receipt),
                breakdownFinished.Evidence.SnapshotCheckpoint, CampaignHostAcceptedStep.SourceGt1BreakdownCompleteKind);
            Assert.Equal(CampaignHostAcceptedStep.SourceGt1BreakdownCompleteContractVersion, breakdownStep.ContractVersion);
        }
        var ccnnFirst = CampaignExercises.Submit(catanzaroConfirmed,
            Submission(catanzaroConfirmed, axisBatchChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(47, ccnnFirst.SourceAuthority!.ProfileVersion);
        var australianAfterCcnn = CampaignExercises.Query(ccnnFirst, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "sixth-australian-placement").ToArray();
        Assert.Equal(choicesBefore, australianAfterCcnn.Select(value => value.ChoiceJson).Order(StringComparer.Ordinal).ToArray());
        var sameAustralianChoice = australianAfterCcnn.Single(value => value.SourceActionId == CampaignSourceAuthorityState.SixthAustralianActionId("e1829"));
        var ccnnThenAustralian = CampaignExercises.Submit(ccnnFirst,
            Submission(ccnnFirst, sameAustralianChoice.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(australianFirst.SourceAuthority!.GroupedWorld!.ContentHash, ccnnThenAustralian.SourceAuthority!.GroupedWorld!.ContentHash);
        Assert.Equal(australianFirst.SourceAuthority.GroupedWorld.Hash(), ccnnThenAustralian.SourceAuthority.GroupedWorld.Hash());
        Assert.Equal(australianFirst.SourceAuthority.Battlefield!.Hash(), ccnnThenAustralian.SourceAuthority.Battlefield!.Hash());
        Assert.Equal(49, ccnnThenAustralian.SourceAuthority.ProfileVersion);
        Assert.Equal(14, australianFirst.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "1-ccnn-division").Members.Count);
        Assert.Equal(12, australianFirst.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "4-ccnn-division").Members.Count);
        var ccnnUnits = australianFirst.SourceAuthority.GroupedWorld.Groups.Where(value => value.GroupId is "1-ccnn-division" or "4-ccnn-division")
            .SelectMany(value => value.Members).ToArray();
        Assert.All(ccnnUnits.Where(value => value.ElementId.EndsWith("-ccnn-infantry", StringComparison.Ordinal)),
            value => { Assert.Equal(5, value.InfantryToe); Assert.Equal(10, value.CapabilityPointAllowance); });
        Assert.Equal(20, ccnnUnits.Single(value => value.ElementId == "1-ccnn-hq").CapabilityPointAllowance);
        Assert.Equal(30, ccnnUnits.Single(value => value.ElementId == "4-ccnn-hq").CapabilityPointAllowance);
        Assert.Equal(30, ccnnUnits.Single(value => value.ElementId == "i-medium-tank").CapabilityPointAllowance);
        Assert.True(CampaignExercises.Reconstruct(australianFirst).IsVerified);
        Assert.True(CampaignExercises.Reconstruct(ccnnThenAustralian).IsVerified);
        var arescaChoiceBefore = CampaignExercises.Query(ccnnFirst, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "aresca-disposition");
        var arescaChoiceAfter = CampaignExercises.Query(australianFirst, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "aresca-disposition");
        Assert.Equal(arescaChoiceBefore.ChoiceJson, arescaChoiceAfter.ChoiceJson);
        var arescaFirst = CampaignExercises.Submit(ccnnFirst,
            Submission(ccnnFirst, arescaChoiceBefore.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(55, arescaFirst.SourceAuthority!.ProfileVersion);
        var ausAfterAresca = CampaignExercises.Query(arescaFirst, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "sixth-australian-placement").ToArray();
        Assert.Equal(choicesBefore, ausAfterAresca.Select(value => value.ChoiceJson).Order(StringComparer.Ordinal).ToArray());
        var ausArescaChoice = ausAfterAresca.Single(value => value.SourceActionId == CampaignSourceAuthorityState.SixthAustralianActionId("e1829"));
        var arescaThenAustralian = CampaignExercises.Submit(arescaFirst,
            Submission(arescaFirst, ausArescaChoice.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var australianThenAresca = CampaignExercises.Submit(australianFirst,
            Submission(australianFirst, arescaChoiceAfter.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(57, australianThenAresca.SourceAuthority!.ProfileVersion);
        var aresca = australianThenAresca.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "aresca-tank-regiment");
        Assert.Equal(1, aresca.StackValue); Assert.Equal(10, aresca.MotorPool);
        Assert.Equal(1, aresca.Members.Single(value => value.ElementId == "aresca-tank-regiment-hq").ComponentToe);
        Assert.Equal(9, aresca.Members.Single(value => value.ElementId == "xxi-libyan-light-tank").ComponentToe);
        Assert.Equal(australianThenAresca.SourceAuthority.GroupedWorld.Hash(), arescaThenAustralian.SourceAuthority!.GroupedWorld!.Hash());
        Assert.Equal(australianThenAresca.SourceAuthority.Battlefield!.Hash(), arescaThenAustralian.SourceAuthority.Battlefield!.Hash());
        Assert.True(CampaignExercises.Reconstruct(australianThenAresca).IsVerified);
        Assert.True(CampaignExercises.Reconstruct(arescaThenAustralian).IsVerified);
        static HashSet<string> RepresentedIds(CampaignSourcePlayableWorld world) => world.GroupedWorld.Prior.World.Representations
            .SelectMany(value => value.BoundElementIds)
            .Concat(world.GroupedWorld.Groups.SelectMany(value => value.Members.Select(member => member.ElementId)))
            .Concat(world.GroupedWorld.Detached.Select(value => value.Member.ElementId))
            .ToHashSet(StringComparer.Ordinal);
        static bool IsRemovedFromPlay(CampaignSourcePlayableWorld world, CampaignSourceBattlefieldEntry value) =>
            world.Source.CwFreeGunsWorld?.GarrisonRemainderContent is not null
            && value.AuthorityStatus == "source-land-only-aa-removed"
            && ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds.Contains(value.ElementId, StringComparer.Ordinal);
        var arescaIds = new[] { "aresca-tank-regiment-hq", "xxi-libyan-light-tank" };
        var beforeWorld = CampaignSourcePlayableWorld.Create(ccnnThenAustralian.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true);
        var representedBefore = RepresentedIds(beforeWorld);
        var unrepresentedBefore = beforeWorld.Battlefield.Entries
            .Where(entry => !representedBefore.Contains(entry.ElementId) && !IsRemovedFromPlay(beforeWorld, entry))
            .Select(entry => entry.ElementId).ToHashSet(StringComparer.Ordinal);
        Assert.All(arescaIds, id => Assert.Contains(id, unrepresentedBefore));
        Assert.Contains("forces-incomplete", beforeWorld.ReadinessBlockers());
        var afterWorld = CampaignSourcePlayableWorld.Create(australianThenAresca.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true);
        var representedAfter = RepresentedIds(afterWorld);
        var unrepresentedAfter = afterWorld.Battlefield.Entries
            .Where(entry => !representedAfter.Contains(entry.ElementId) && !IsRemovedFromPlay(afterWorld, entry))
            .Select(entry => entry.ElementId).ToHashSet(StringComparer.Ordinal);
        Assert.All(arescaIds, id => Assert.DoesNotContain(id, unrepresentedAfter));
        Assert.Contains("forces-incomplete", afterWorld.ReadinessBlockers());
        // Only Aresca's 2 elements change between these two checkpoints - every
        // other still-unrepresented element (the real Category B gap and any other
        // not-yet-reached formation) is identical before and after.
        Assert.Equal(unrepresentedBefore.Except(arescaIds, StringComparer.Ordinal).Order(StringComparer.Ordinal),
            unrepresentedAfter.Order(StringComparer.Ordinal));
    }

    // Standalone proof (pure addition, pinned Theory method untouched) for the
    // real Category B gap: 12 elements with real sourced facts/citations in
    // Cna1979GrazianiSourceFacts.cs but zero admission wiring anywhere -
    // 4 CW attached artillery/antitank pieces (3-royal-horse-artillery-antitank,
    // 4-royal-horse-artillery, 7-medium-artillery, 31-field-artillery), the 5
    // Bardia elements ContentSourceTobrukBardiaGarrisonPackArtifact's part-1 pack
    // deliberately left out (vec-machine-gun-battalion, 30-gaf-antitank-battalion,
    // 8/9/10-gaf-artillery), and the 3-element Tripoli Tank Regiment
    // (tripoli-tank-regiment-hq, xx-libyan-light-tank, lxi-libyan-light-tank).
    // Reuses the pinned test's own proven setup verbatim through its
    // fullyMotorized/motorizedWorld checkpoint (hardcoded to the seed: 0,
    // maximum: 3, allocated: 1 InlineData row), then activates the Giarabub
    // cluster's own already-proven chain (same as PR #352's proof test) only as
    // far as needed to reach MotorMachineGunContent, then CW frontline and
    // Tobruk/Bardia (both already-merged, PR #355), before admitting/activating
    // these 3 new packs.
    [Fact]
    public void ActivatingTheCategoryBClusterRemovesItsTwelveElementsFromForcesIncomplete()
    {
        var seed = 0UL; var maximum = 3; var allocated = 1;
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var initialization = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "source-authority-transition", Cna1979Ruleset.Manifest.Hash, seed,
            CampaignPregameSourceCreation.SetupId,
            CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, initialization),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var session = CampaignExercises.Begin(request, artifact, initialization).Session!;
        var polishActionPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        var polishAction = new CampaignSourceTurnAction(17, "e1730");
        var polishSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            polishActionPosition.PositionId, CampaignActionAudience.Commonwealth, [polishAction], CampaignLegalActionSet.SourceTurnPolicyId);
        var polishBytes = CampaignLegalActionSerializer.Serialize(polishSet);
        Assert.Equal(polishSet, CampaignLegalActionSerializer.DeserializeCanonical(polishBytes));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            polishActionPosition.PositionId, CampaignActionAudience.Axis, [polishAction], CampaignLegalActionSet.SourceTurnPolicyId));
        var arrivalPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        var declineAttack = CampaignSourceTurnAction.DeclineGt1Lane2Attack(17);
        var arrivalSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            arrivalPosition.PositionId, CampaignActionAudience.Commonwealth, [declineAttack], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(arrivalSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(arrivalSet)));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            arrivalPosition.PositionId, CampaignActionAudience.Axis, [declineAttack], CampaignLegalActionSet.SourceTurnPolicyId));
        var convoyPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.PhaseId == LandPhaseIds.NavalConvoySchedule);
        var convoyRoll = CampaignSourceTurnAction.RollGt3Supply(17);
        var convoySet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Axis, [convoyRoll], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(convoySet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(convoySet)));
        var convoyChoices = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Axis,
            [CampaignSourceTurnAction.AdjustGt3Supply(17, 2, 1, 1), CampaignSourceTurnAction.FinishGt3Supply(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(convoyChoices, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(convoyChoices)));
        var cwRoll = CampaignSourceTurnAction.RollGt5Supply(17);
        var cwConvoySet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Commonwealth, [cwRoll], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(cwConvoySet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(cwConvoySet)));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Axis, [cwRoll], CampaignLegalActionSet.SourceTurnPolicyId));
        var shippingPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.PhaseId == LandPhaseIds.TacticalShipping);
        var shippingSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            shippingPosition.PositionId, CampaignActionAudience.Commonwealth, [CampaignSourceTurnAction.DeclineTacticalShipping(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(shippingSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(shippingSet)));
        var declarationPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1 && value.PhaseId == LandPhaseIds.InitiativeDeclaration);
        var declarationSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            declarationPosition.PositionId, CampaignActionAudience.Axis, [CampaignSourceTurnAction.ActFirst(17), CampaignSourceTurnAction.ActLast(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(declarationSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(declarationSet)));
        var organizationPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1 && value.PhaseId == LandPhaseIds.Organization);
        var organizationSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            organizationPosition.PositionId, CampaignActionAudience.Commonwealth, [CampaignSourceTurnAction.DeclineVoluntaryOrganization(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(organizationSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(organizationSet)));
        var interruptTraining = CampaignSourceTurnAction.InterruptSixthAustralianTraining(17);
        var trainingSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            organizationPosition.PositionId, CampaignActionAudience.Commonwealth, [interruptTraining], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(trainingSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(trainingSet)));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            organizationPosition.PositionId, CampaignActionAudience.Axis, [interruptTraining], CampaignLegalActionSet.SourceTurnPolicyId));
        var unknownTraining = CampaignSourceTrainingLedger.CreateSixthAustralian();
        var interruptedTraining = unknownTraining.Interrupt();
        Assert.True(interruptedTraining.Interrupted);
        Assert.Null(interruptedTraining.CurrentMorale);
        Assert.Null(interruptedTraining.QualifyingStages);
        Assert.NotEqual(unknownTraining.Hash(), interruptedTraining.Hash());
        Assert.Throws<InvalidOperationException>(() => interruptedTraining.Interrupt());
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Commonwealth, [convoyRoll], CampaignLegalActionSet.SourceTurnPolicyId));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Axis));
        var finishBeforeRoll = Submission(session, CampaignSourceAuthorityState.FinishActionId, CampaignActionAudience.Axis);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.FinishPregameAxisSupply(session, finishBeforeRoll).RejectionReason);
        var roll = Assert.Single(CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates);
        session = CampaignExercises.Submit(session, Submission(session, roll.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        if (allocated > 0)
        {
            var add = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates
                .OfType<PregameAxisSupplyAllocationAction>()
                .Single(value => value.Lane == 2 && value.Stage == 1 && value.Delta == 1);
            session = CampaignExercises.Submit(session, Submission(session, add.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        var legacyOffers = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates.Select(x => x.ActionId).ToArray();
        var draft = CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Axis);
        Assert.Single(draft.PrivateActionIds);
        Assert.DoesNotContain(draft.PrivateActionIds[0], legacyOffers);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.FinishPregameAxisSupply(session, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Commonwealth)).RejectionReason);
        var accepted = CampaignExercises.FinishPregameAxisSupply(session, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Axis));
        Assert.True(accepted.IsAccepted);
        var next = accepted.SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(next).IsVerified);
        var giarabubAction = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c1014", 1);
        var oldAxisMotorHash = next.SourceAuthority!.AxisMotorization.Hash();
        var oldAxisChoices = CampaignExercises.QuerySourceMotorizationDraft(next, CampaignActionAudience.Axis);
        Assert.DoesNotContain(giarabubAction, oldAxisChoices.PrivateActionIds);
        var admittedPolicy = CampaignExercises.AdmitSourceGiarabubMotorPlacementPolicy(next).SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceGiarabubMotorPlacementPolicyAdmitted(admittedPolicy));
        Assert.NotEqual(oldAxisMotorHash, admittedPolicy.SourceAuthority!.AxisMotorization.Hash());
        Assert.Equal(oldAxisMotorHash, next.SourceAuthority.AxisMotorization.Hash());
        Assert.DoesNotContain(giarabubAction, CampaignExercises.QuerySourceMotorizationDraft(next, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Contains(giarabubAction, CampaignExercises.QuerySourceMotorizationDraft(admittedPolicy, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.DoesNotContain(giarabubAction, CampaignExercises.QuerySourceMotorizationDraft(admittedPolicy, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.AdjustSourceMotorization(admittedPolicy,
            Submission(admittedPolicy, giarabubAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        var placedMotor = CampaignExercises.AdjustSourceMotorization(admittedPolicy,
            Submission(admittedPolicy, giarabubAction, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(124, placedMotor.SourceAuthority!.AxisMotorization.Remaining("axis-libya-unattached"));
        Assert.Contains(placedMotor.SourceAuthority.AxisMotorization.Allocations,
            value => value.PoolId == "axis-libya-unattached" && value.LocationId == "c1014" && value.Points == 1);
        Assert.True(CampaignExercises.Reconstruct(placedMotor).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceGiarabubMotorPlacementPolicy(placedMotor).RejectionReason);
        var cwBeforeFinish = CampaignExercises.QuerySourceActionsDraft(next, CampaignActionAudience.Commonwealth);
        var finishOwner = CampaignExercises.Query(next, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var ownerFinished = CampaignExercises.Submit(next, Submission(next, finishOwner.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(1, ownerFinished.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.FinalizedLegacyCheckpointContractVersion, CampaignExercises.QueryCheckpoint(ownerFinished).ContractVersion);
        Assert.Equal(next.SourceAuthority!.RandomState, ownerFinished.SourceAuthority.RandomState);
        Assert.Equal(next.SourceAuthority.World.PendingArrivals, ownerFinished.SourceAuthority.World.PendingArrivals);
        Assert.Equal(cwBeforeFinish.VisibleBytes, CampaignExercises.QuerySourceActionsDraft(ownerFinished, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.PlaceSourceAxisDump(ownerFinished,
            Submission(ownerFinished, CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507"), CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceCompactCheckpoint(ownerFinished).RejectionReason);
        var cwWhileAxisFinished = CampaignExercises.PlaceSourceCommonwealthSupply(ownerFinished,
            Submission(ownerFinished, CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-dump-1", "d3612"), CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, cwWhileAxisFinished.SourceAuthority!.FinalizedOwners);
        var finishCw = CampaignExercises.Query(cwWhileAxisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var bothFinished = CampaignExercises.Submit(cwWhileAxisFinished, Submission(cwWhileAxisFinished, finishCw.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(3, bothFinished.SourceAuthority!.FinalizedOwners);
        Assert.True(CampaignExercises.Reconstruct(bothFinished).IsVerified);
        var reopenAxis = CampaignExercises.Query(bothFinished, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var reopenedAxis = CampaignExercises.Submit(bothFinished, Submission(bothFinished, reopenAxis.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, reopenedAxis.SourceAuthority!.FinalizedOwners);
        Assert.True(CampaignExercises.Reconstruct(reopenedAxis).IsVerified);
        var reopenCw = CampaignExercises.Query(reopenedAxis, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var reopenedBoth = CampaignExercises.Submit(reopenedAxis, Submission(reopenedAxis, reopenCw.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(0, reopenedBoth.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.FinalizedLegacyCheckpointContractVersion, CampaignExercises.QueryCheckpoint(reopenedBoth).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(reopenedBoth).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.PlaceSourceCommonwealthSupply(reopenedAxis,
            Submission(reopenedAxis, CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-cairo-1", "e1730"), CampaignActionAudience.Commonwealth)).RejectionReason);
        var lockedCwView = CampaignExercises.QuerySourceActionsDraft(reopenedAxis, CampaignActionAudience.Commonwealth).VisibleBytes;
        var changedAxis = CampaignExercises.PlaceSourceAxisDump(reopenedAxis,
            Submission(reopenedAxis, CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507"), CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, changedAxis.SourceAuthority!.FinalizedOwners);
        Assert.Equal(lockedCwView, CampaignExercises.QuerySourceActionsDraft(changedAxis, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.True(CampaignExercises.Reconstruct(changedAxis).IsVerified);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourcePolishArrivalDraft(next, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.SubmitSourcePolishArrival(next, Submission(next, roll.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var earlyCompact = CampaignExercises.AdmitSourceCompactCheckpoint(next).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(earlyCompact).ContractVersion);
        var compactFinish = CampaignExercises.Query(earlyCompact, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var finalizedCompact = CampaignExercises.Submit(earlyCompact, Submission(earlyCompact, compactFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignSourceAuthorityState.FinalizedCompactCheckpointContractVersion, CampaignExercises.QueryCheckpoint(finalizedCompact).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(finalizedCompact).IsVerified);
        var earlyDump = CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507");
        earlyCompact = CampaignExercises.PlaceSourceAxisDump(earlyCompact,
            Submission(earlyCompact, earlyDump, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(earlyCompact).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(earlyCompact).IsVerified);
        var normalAxis = CampaignExercises.Query(next, CampaignActionAudience.Axis);
        Assert.True(normalAxis.IsSuccessful);
        Assert.Equal(CampaignLegalActionSet.SourceSetupPolicyId, normalAxis.ActionSet!.PolicyId);
        var encodedActions = CampaignLegalActionSerializer.Serialize(normalAxis.ActionSet);
        Assert.Equal(encodedActions, CampaignLegalActionSerializer.Serialize(CampaignLegalActionSerializer.DeserializeCanonical(encodedActions)));
        var unifiedDump = normalAxis.ActionSet.Candidates.OfType<CampaignSourceSetupAction>()
            .Single(value => value.Family == "axis-dump" && value.ChoiceJson.Contains("c4507", StringComparison.Ordinal)
                && value.ChoiceJson.Contains("axis-dump-1", StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(normalAxis.ActionSet.CampaignId,
            normalAxis.ActionSet.StateVersion, normalAxis.ActionSet.RulesetHash, normalAxis.ActionSet.PositionId,
            CampaignActionAudience.Axis, [unifiedDump], CampaignLegalActionSet.CurrentPolicyId));
        var ownerCut = CampaignExercises.QuerySourceActionsDraft(next, CampaignActionAudience.Axis);
        Assert.DoesNotContain(unifiedDump.ActionId, System.Text.Encoding.UTF8.GetString(ownerCut.VisibleBytes));
        var ownContext = System.Text.Encoding.UTF8.GetString(ownerCut.VisibleBytes);
        Assert.Contains("axis-gt1-supply", ownContext);
        Assert.Contains("maximumSupply", ownContext);
        var hiddenCwAction = CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-dump-1", "d3612");
        var hiddenAdvance = CampaignExercises.PlaceSourceCommonwealthSupply(next,
            Submission(next, hiddenCwAction, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var afterHidden = CampaignExercises.QuerySourceActionsDraft(hiddenAdvance, CampaignActionAudience.Axis);
        Assert.Equal(ownerCut.VisibleBytes, afterHidden.VisibleBytes);
        Assert.NotEqual(ownerCut.PrivateActionIds[0], afterHidden.PrivateActionIds[0]);
        var wrongSide = Submission(next, unifiedDump.ActionId, CampaignActionAudience.Commonwealth);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(next, wrongSide).RejectionReason);
        var unifiedResult = CampaignExercises.Submit(next, Submission(next, unifiedDump.ActionId, CampaignActionAudience.Axis));
        Assert.True(unifiedResult.IsAccepted);
        Assert.Equal(unifiedDump.ActionId, unifiedResult.Evidence!.Receipt.ActionId);
        Assert.True(CampaignExercises.Reconstruct(unifiedResult.SuccessorSession!).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(unifiedResult.SuccessorSession!,
            Submission(next, unifiedDump.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(unifiedResult.SuccessorSession!,
            Submission(unifiedResult.SuccessorSession!, unifiedDump.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.FinishPregameAxisSupply(next, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Axis)).RejectionReason);
        using var snapshot = JsonDocument.Parse(accepted.Evidence!.SnapshotCheckpoint);
        Assert.Equal("setup-in-progress", snapshot.RootElement.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, snapshot.RootElement.GetProperty("gameClock").ValueKind);
        using var plan = JsonDocument.Parse(snapshot.RootElement.GetProperty("convoyPlanBase64").GetBytesFromBase64());
        Assert.Equal(maximum, plan.RootElement.GetProperty("maximumSupply").GetInt32());
        Assert.Equal(maximum - allocated, plan.RootElement.GetProperty("unusedAvailability").GetInt32());
        using var source = JsonDocument.Parse(snapshot.RootElement.GetProperty("sourceSnapshotBase64").GetBytesFromBase64());
        Assert.Contains(source.RootElement.GetProperty("pendingArrivalGroupIds").EnumerateArray().Select(x => x.GetString()),
            id => id == Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId);
        var dumpDraft = CampaignExercises.QuerySourceAxisDumpDraft(next, CampaignActionAudience.Axis);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisDumpDraft(next, CampaignActionAudience.Commonwealth));
        Assert.Equal(8, dumpDraft.PrivateActionIds.Count);
        var chosenId = CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507");
        Assert.Contains(chosenId, dumpDraft.PrivateActionIds);
        var dumpCommand = Submission(next, chosenId, CampaignActionAudience.Axis);
        var placed = CampaignExercises.PlaceSourceAxisDump(next, dumpCommand);
        Assert.True(placed.IsAccepted);
        var after = placed.SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(after).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
            CampaignExercises.PlaceSourceAxisDump(after, dumpCommand).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceAxisDump(after, Submission(after,
                CampaignSourceAuthorityState.DumpActionId("axis-dump-2", "c4507"), CampaignActionAudience.Axis)).RejectionReason);
        var second = CampaignExercises.PlaceSourceAxisDump(after, Submission(after,
            CampaignSourceAuthorityState.DumpActionId("axis-dump-2", "c4707"), CampaignActionAudience.Axis));
        Assert.True(second.IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(second.SuccessorSession!).IsVerified);
        var corruptHistory = second.SuccessorSession!.PregameHistory.Select(value => value.ToArray()).ToArray();
        var lastEvent = System.Text.Encoding.UTF8.GetString(corruptHistory[^1]).Replace("c4707", "c4507", StringComparison.Ordinal);
        corruptHistory[^1] = System.Text.Encoding.UTF8.GetBytes(lastEvent);
        var corrupt = new ExerciseSession(second.SuccessorSession.Pregame!, second.SuccessorSession.CurrentContacts,
            corruptHistory, second.SuccessorSession.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(corrupt).IsVerified);
        using var finalSnapshot = JsonDocument.Parse(second.Evidence!.SnapshotCheckpoint);
        using var world = JsonDocument.Parse(finalSnapshot.RootElement.GetProperty("axisSupplyWorldBase64").GetBytesFromBase64());
        Assert.Equal(22, world.RootElement.GetProperty("units").GetArrayLength());
        Assert.False(world.RootElement.GetProperty("siteCoverageComplete").GetBoolean());
        Assert.Equal(JsonValueKind.Null, world.RootElement.GetProperty("units").EnumerateArray()
            .Single(x => x.GetProperty("id").GetString() == "axis-derna-1").GetProperty("ammo").ValueKind);
        var cwSession = second.SuccessorSession!;
        Assert.Equal(CampaignSourceAuthorityState.ContractVersion, cwSession.SourceAuthority!.ProfileVersion);
        Assert.True(CampaignExercises.IsSourceSetupSeat(cwSession, CampaignActionAudience.Axis));
        Assert.True(CampaignExercises.IsSourceSetupSeat(cwSession, CampaignActionAudience.Commonwealth));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Axis));
        var placements = new[]
        {
            ("cw-dump-1", "d3612"),
            ("cw-alexandria-1", "e3613"), ("cw-alexandria-2", "e3613"), ("cw-alexandria-3", "e3714"),
            ("cw-cairo-1", "e1829"), ("cw-cairo-2", "e1830"), ("cw-cairo-3", "e1930"),
        };
        foreach (var (id, location) in placements)
        {
            var idHash = CampaignSourceAuthorityState.CommonwealthSupplyActionId(id, location);
            var cwDraft = CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Commonwealth);
            Assert.Contains(idHash, cwDraft.PrivateActionIds);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCommonwealthSupply(cwSession, Submission(cwSession, idHash, CampaignActionAudience.Axis)).RejectionReason);
            var step = CampaignExercises.PlaceSourceCommonwealthSupply(cwSession, Submission(cwSession, idHash, CampaignActionAudience.Commonwealth));
            Assert.True(step.IsAccepted);
            cwSession = step.SuccessorSession!;
            Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        }
        Assert.Empty(CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.Equal(15, cwSession.SourceAuthority!.CommonwealthSupplyWorld.Units.Count);
        Assert.Equal(18, cwSession.SourceAuthority.CommonwealthInitialForces.Forces.Count);
        Assert.Null(cwSession.SourceAuthority.CommonwealthInitialForces.Forces
            .Single(value => value.ElementId == "central-india-horse").InitialToes);
        Assert.Null(cwSession.SourceAuthority.Position);
        var axisDummy = CampaignSourceAuthorityState.DummyActionId(true, "axis-dummy-1", "c4407");
        Assert.Contains(axisDummy, CampaignExercises.QuerySourceDummyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, axisDummy, CampaignActionAudience.Commonwealth)).RejectionReason);
        var dummyStep = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, axisDummy, CampaignActionAudience.Axis));
        Assert.True(dummyStep.IsAccepted);
        cwSession = dummyStep.SuccessorSession!;
        var omit = CampaignSourceAuthorityState.DummyActionId(true, "axis-dummy-2", null);
        cwSession = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, omit, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwDummy = CampaignSourceAuthorityState.DummyActionId(false, "cw-dummy-1", "d3611");
        Assert.Contains(cwDummy, CampaignExercises.QuerySourceDummyDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        cwSession = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, cwDummy, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var priorRandom = cwSession.SourceAuthority!.RandomState;
        var expectedGt1 = SandtableRandom.RollD6(priorRandom);
        var rollGt1 = CampaignSourceAuthorityState.OpeningConvoyActionId("roll", 0, 0, 0);
        Assert.Single(CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, rollGt1, CampaignActionAudience.Commonwealth)).RejectionReason);
        var gt1Step = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, rollGt1, CampaignActionAudience.Axis));
        Assert.True(gt1Step.IsAccepted);
        cwSession = gt1Step.SuccessorSession!;
        Assert.Equal(expectedGt1.Value, cwSession.SourceAuthority!.OpeningConvoyPlan.Die);
        Assert.Equal(expectedGt1.State.NextByteCursor, cwSession.SourceAuthority.RandomState.NextByteCursor);
        if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply > 0)
        {
            // Real source lane 3 under disclosed provisional Benghazi ruling.
            // This branches the same rolled plan; it does not advance a game.
            var lane3Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 3, 1, 1);
            var lane3Session = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession,
                Submission(cwSession, lane3Add, CampaignActionAudience.Axis)).SuccessorSession!;
            var lane3Finish = CampaignSourceAuthorityState.OpeningConvoyActionId("finish", 0, 0, 0);
            lane3Session = CampaignExercises.SubmitSourceAxisGt1Supply(lane3Session,
                Submission(lane3Session, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
            var lane3Source = lane3Session.SourceAuthority!;
            var axisBefore = CampaignSourceGt1AxisArrival.Create(lane3Source);
            var benghazi = CampaignSourceGt1BenghaziArrival.Create(lane3Source, axisBefore);
            Assert.True(benghazi.CanAnnounce);
            var announced = benghazi.Announce(lane3Source, axisBefore);
            var (delivered, axisAfter) = announced.DeclineAttackAndDeliver(lane3Source, axisBefore);
            Assert.True(delivered.Delivered);
            Assert.Equal("b4827", Assert.Single(lane3Source.AxisSupplyWorld.Units,
                value => value.Id == "axis-benghazi-1").LocationId);
            Assert.Equal("a4827", Assert.Single(axisAfter.Supply.Units,
                value => value.Id == "axis-benghazi-1").LocationId);
            var landed = Assert.Single(axisAfter.Supply.Units, value => value.Id == "axis-gt1-op1-lane3-1");
            Assert.Equal("a4827", landed.LocationId);
            Assert.Equal(40, landed.Ammo);
            Assert.Equal(60, landed.Fuel);
            Assert.Equal(2, axisAfter.Supply.Units.Count(value => value.LocationId == "a4827"));
            Assert.False(axisAfter.HasOtherStageOneLanes);
            Assert.Throws<InvalidOperationException>(() => announced.DeclineAttackAndDeliver(lane3Source, axisAfter));
            var (replayedBenghazi, replayedAxis) = benghazi.Announce(lane3Source, axisBefore)
                .DeclineAttackAndDeliver(lane3Source, axisBefore);
            Assert.Equal(delivered.Hash(), replayedBenghazi.Hash());
            Assert.Equal(axisAfter.Hash(), replayedAxis.Hash());
            using var landedJson = JsonDocument.Parse(axisAfter.Supply.SerializeCanonical());
            Assert.Equal(2, landedJson.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Contains("rul-gra-05-provisional", landedJson.RootElement.GetProperty("benghaziCoordinateStatus").GetString()!);
            if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply >= 2)
            {
                var mixed = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession,
                    Submission(cwSession, lane3Add, CampaignActionAudience.Axis)).SuccessorSession!;
                var lane2Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 2, 1, 1);
                mixed = CampaignExercises.SubmitSourceAxisGt1Supply(mixed,
                    Submission(mixed, lane2Add, CampaignActionAudience.Axis)).SuccessorSession!;
                mixed = CampaignExercises.SubmitSourceAxisGt1Supply(mixed,
                    Submission(mixed, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
                var mixedSource = mixed.SourceAuthority!;
                var mixedAxis = CampaignSourceGt1AxisArrival.Create(mixedSource);
                Assert.False(mixedAxis.CanAnnounce);
                Assert.False(CampaignSourceGt1BenghaziArrival.Create(mixedSource, mixedAxis).CanAnnounce);
                Assert.Throws<InvalidOperationException>(() => mixedAxis.DeliverSoleBenghaziLane(mixedSource, 1));
                var multi = CampaignSourceGt1MultiArrival.Announce(mixedSource, mixedAxis);
                Assert.Collection(multi.Lanes, value => Assert.Equal(2, value), value => Assert.Equal(3, value));
                var (firstThree, axisThree) = multi.DeclineAndDeliver(mixedSource, mixedAxis, 3);
                var (threeThenTwo, axisThreeThenTwo) = firstThree.DeclineAndDeliver(mixedSource, axisThree, 2);
                var (firstTwo, axisTwo) = multi.DeclineAndDeliver(mixedSource, mixedAxis, 2);
                var (twoThenThree, axisTwoThenThree) = firstTwo.DeclineAndDeliver(mixedSource, axisTwo, 3);
                Assert.True(threeThenTwo.IsDelivered(2));
                Assert.True(threeThenTwo.IsDelivered(3));
                Assert.Equal(axisThreeThenTwo.Supply.Hash(), axisTwoThenThree.Supply.Hash());
                Assert.Equal(threeThenTwo.Hash(), twoThenThree.Hash());
                Assert.Equal("box.tripoli", Assert.Single(axisThreeThenTwo.Supply.Units,
                    value => value.Id == "axis-gt1-op1-lane2-1").LocationId);
                Assert.Equal("a4827", Assert.Single(axisThreeThenTwo.Supply.Units,
                    value => value.Id == "axis-gt1-op1-lane3-1").LocationId);
                Assert.Throws<InvalidOperationException>(() => threeThenTwo.DeclineAndDeliver(mixedSource, axisThreeThenTwo, 3));
                Assert.DoesNotContain(mixedSource.AxisSupplyWorld.Units, value => value.Id == "axis-gt1-op1-lane3-1");
                if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply >= 3)
                {
                    var triple = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession,
                        Submission(cwSession, lane3Add, CampaignActionAudience.Axis)).SuccessorSession!;
                    triple = CampaignExercises.SubmitSourceAxisGt1Supply(triple,
                        Submission(triple, lane2Add, CampaignActionAudience.Axis)).SuccessorSession!;
                    var lane6Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 6, 1, 1);
                    triple = CampaignExercises.SubmitSourceAxisGt1Supply(triple,
                        Submission(triple, lane6Add, CampaignActionAudience.Axis)).SuccessorSession!;
                    triple = CampaignExercises.SubmitSourceAxisGt1Supply(triple,
                        Submission(triple, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
                    var tripleSource = triple.SourceAuthority!;
                    var tripleAxis = CampaignSourceGt1AxisArrival.Create(tripleSource);
                    var tripleMulti = CampaignSourceGt1MultiArrival.Announce(tripleSource, tripleAxis);
                    var (tripleTwo, tripleAfterTwo) = tripleMulti.DeclineAndDeliver(tripleSource, tripleAxis, 2);
                    var (tripleThree, tripleAfterThree) = tripleTwo.DeclineAndDeliver(tripleSource, tripleAfterTwo, 3);
                    Assert.True(tripleThree.IsDue(6));
                    Assert.False(tripleThree.IsDelivered(6));
                    Assert.True(tripleThree.CanDecline(6));
                    var (tripleSix, tripleAfterSix) = tripleThree.DeclineAndDeliver(tripleSource, tripleAfterThree, 6);
                    Assert.True(tripleSix.Lane6Declined);
                    Assert.True(tripleSix.IsDelivered(6));
                    Assert.Equal("c4807", Assert.Single(tripleAfterSix.Supply.Units,
                        value => value.Id == "axis-gt1-op1-lane6-1").LocationId);
                    Assert.Throws<InvalidOperationException>(() => tripleSix.DeclineAndDeliver(tripleSource, tripleAfterSix, 6));
                    (bool CanDecline, bool AttackDeclined)? oneBefore = null, oneAfter = null, threeBefore = null, threeAfter = null;
                    for (var amount = 1; amount <= 3; amount++)
                    {
                        var tobruk = cwSession;
                        for (var n = 0; n < amount; n++)
                            tobruk = CampaignExercises.SubmitSourceAxisGt1Supply(tobruk,
                                Submission(tobruk, lane6Add, CampaignActionAudience.Axis)).SuccessorSession!;
                        tobruk = CampaignExercises.SubmitSourceAxisGt1Supply(tobruk,
                            Submission(tobruk, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
                        var tobrukSource = tobruk.SourceAuthority!;
                        var tobrukAxis = CampaignSourceGt1AxisArrival.Create(tobrukSource);
                        var tobrukAnnounced = CampaignSourceGt1MultiArrival.Announce(tobrukSource, tobrukAxis);
                        var (tobrukDeclined, tobrukAfter) = tobrukAnnounced.DeclineAndDeliver(tobrukSource, tobrukAxis, 6);
                        if (amount == 1) { oneBefore = tobrukAnnounced.Lane6CommonwealthCut(); oneAfter = tobrukDeclined.Lane6CommonwealthCut(); }
                        if (amount == 3) { threeBefore = tobrukAnnounced.Lane6CommonwealthCut(); threeAfter = tobrukDeclined.Lane6CommonwealthCut(); }
                        Assert.True(tobrukDeclined.Lane6Declined);
                        Assert.False(tobrukDeclined.CanDecline(6));
                        Assert.Equal(amount <= 2, tobrukDeclined.IsDelivered(6));
                        Assert.Equal(3 + (amount <= 2 ? amount : 0),
                            tobrukAfter.Supply.Units.Count(value => value.LocationId == "c4807"));
                        if (amount == 3) Assert.Equal(tobrukAxis.Supply.Hash(), tobrukAfter.Supply.Hash());
                        Assert.Equal(3, tobrukSource.AxisSupplyWorld.Units.Count(value => value.LocationId == "c4807"));
                    }
                    Assert.Equal(oneBefore, threeBefore);
                    Assert.Equal(oneAfter, threeAfter);
                }
            }
            var gt1Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 2, 1, 1);
            cwSession = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, gt1Add, CampaignActionAudience.Axis)).SuccessorSession!;
            Assert.Single(cwSession.SourceAuthority!.OpeningConvoyPlan.Allocations());
        }
        var gt1Finish = CampaignSourceAuthorityState.OpeningConvoyActionId("finish", 0, 0, 0);
        cwSession = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, gt1Finish, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(cwSession.SourceAuthority!.OpeningConvoyPlan.IsFinished);
        var pendingTripoli = CampaignSourceGt1AxisArrival.Create(cwSession.SourceAuthority!);
        var sourceSupplyHash = cwSession.SourceAuthority!.AxisSupplyWorld.Hash();
        if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply > 0)
        {
            Assert.True(pendingTripoli.CanAnnounce);
            var announcedTripoli = pendingTripoli.Announce();
            Assert.True(announcedTripoli.Announced);
            Assert.False(announcedTripoli.Delivered);
            var deliveredTripoli = announcedTripoli.DeclineAttackAndDeliver();
            var deliveredUnit = Assert.Single(deliveredTripoli.Supply.Units, unit => unit.Id == "axis-gt1-op1-lane2-1");
            Assert.Equal("box.tripoli", deliveredUnit.LocationId);
            Assert.Equal(40, deliveredUnit.Ammo);
            Assert.Equal(60, deliveredUnit.Fuel);
            Assert.Equal(sourceSupplyHash, cwSession.SourceAuthority.AxisSupplyWorld.Hash());
            Assert.NotEqual(pendingTripoli.Hash(), deliveredTripoli.Hash());
            Assert.Throws<InvalidOperationException>(() => deliveredTripoli.DeclineAttackAndDeliver());
        }
        else
        {
            Assert.False(pendingTripoli.CanAnnounce);
            Assert.Throws<InvalidOperationException>(() => pendingTripoli.Announce());
        }

        Assert.Empty(CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var skipCwGt1 = CampaignSourceAuthorityState.CommonwealthSupplyPlanActionId("skip", 1);
        var cwPlanning = CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Commonwealth);
        Assert.Contains(skipCwGt1, cwPlanning.PrivateActionIds);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Axis));
        var beforeSkip = cwSession.SourceAuthority.RandomState.NextByteCursor;
        cwSession = CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession,
            Submission(cwSession, skipCwGt1, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(beforeSkip, cwSession.SourceAuthority!.RandomState.NextByteCursor);
        Assert.Equal("skipped-opening", cwSession.SourceAuthority.CommonwealthSupplyPlans!.Entries[0].Status);
        foreach (var turn in new[] { 2, 3, 4 })
        {
            var prior = cwSession.SourceAuthority.RandomState;
            var expected = SandtableRandom.RollD6(prior);
            var action = CampaignSourceAuthorityState.CommonwealthSupplyPlanActionId("roll", turn);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession, Submission(cwSession, action, CampaignActionAudience.Axis)).RejectionReason);
            cwSession = CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession,
                Submission(cwSession, action, CampaignActionAudience.Commonwealth)).SuccessorSession!;
            var entry = cwSession.SourceAuthority!.CommonwealthSupplyPlans!.Entries.Single(value => value.TargetGameTurn == turn);
            Assert.Equal(expected.Value, entry.Die);
            Assert.Equal((expected.Value + 1) / 2, entry.SupplyUnits);
            Assert.Equal(expected.State.NextByteCursor, cwSession.SourceAuthority.RandomState.NextByteCursor);
        }
        Assert.Empty(CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var gt1Plan = CampaignSourceGt1SupplyPlanning.Create(cwSession.SourceAuthority!, cwSession.SourceAuthority!.RandomState);
        var gt5 = CampaignSourceGt1CommonwealthSupplyPlanning.Create();
        var expectedGt5 = SandtableRandom.RollD6(gt1Plan.RandomState);
        var rolledGt5 = gt5.Roll(gt1Plan.RandomState);
        Assert.Equal(expectedGt5.Value, rolledGt5.Plan.Die);
        Assert.Equal((expectedGt5.Value + 1) / 2, rolledGt5.Plan.Units);
        Assert.Equal(expectedGt5.State, rolledGt5.Random);
        Assert.Throws<InvalidOperationException>(() => rolledGt5.Plan.Roll(rolledGt5.Random));
        Assert.Equal(expectedGt5.State, gt1Plan.WithRandomState(rolledGt5.Random).RandomState);
        var expectedGt3 = SandtableRandom.RollD6(gt1Plan.RandomState);
        var rolledGt3 = gt1Plan.Roll();
        Assert.Equal(expectedGt3.Value, rolledGt3.Die);
        Assert.Equal(expectedGt3.Value switch { 1 => 0, <= 3 => 1, <= 5 => 2, _ => 3 }, rolledGt3.Maximum);
        Assert.Equal(expectedGt3.State, rolledGt3.RandomState);
        Assert.Throws<InvalidOperationException>(() => rolledGt3.Roll());
        Assert.Empty(rolledGt3.ValidDeltas(1, 1));
        Assert.Empty(rolledGt3.ValidDeltas(2, 4));
        if (rolledGt3.Maximum > 0)
        {
            var assigned = rolledGt3.Adjust(2, 1, 1);
            Assert.Equal(1, assigned.QuantityAt(2, 1));
            Assert.Equal(rolledGt3.RandomState, assigned.RandomState);
            Assert.Equal(0, assigned.Adjust(2, 1, -1).QuantityAt(2, 1));
            for (var unit = 1; unit < rolledGt3.Maximum; unit++) assigned = assigned.Adjust(2, 1, 1);
            Assert.DoesNotContain(1, assigned.ValidDeltas(2, 1));
            Assert.DoesNotContain(1, assigned.ValidDeltas(3, 2));
            Assert.Throws<InvalidOperationException>(() => assigned.Adjust(3, 2, 1));
        }
        var finishedGt3 = rolledGt3.Finish();
        Assert.True(finishedGt3.Finished);
        Assert.Empty(finishedGt3.ValidDeltas(2, 1));
        // Each owner's roll may happen first; the shared stream must advance
        // once per roll without rewriting an already-finished Axis plan.
        var axisFirstPlan = gt1Plan.Roll().Finish();
        var axisFirstHash = axisFirstPlan.Hash();
        var cwAfterAxis = CampaignSourceGt1CommonwealthSupplyPlanning.Create().Roll(axisFirstPlan.RandomState);
        Assert.Equal(axisFirstHash, axisFirstPlan.Hash());
        var cwRollFirst = CampaignSourceGt1CommonwealthSupplyPlanning.Create().Roll(gt1Plan.RandomState);
        var axisAfterCw = gt1Plan.WithRandomState(cwRollFirst.Random).Roll().Finish();
        Assert.Equal(axisFirstPlan.Die, cwRollFirst.Plan.Die);
        Assert.Equal(cwAfterAxis.Plan.Die, axisAfterCw.Die);
        Assert.Equal(cwAfterAxis.Random, axisAfterCw.RandomState);
        Assert.Throws<InvalidOperationException>(() => axisAfterCw.Roll());
        Assert.Throws<InvalidOperationException>(() => cwAfterAxis.Plan.Roll(cwAfterAxis.Random));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        // Exercise the real prestart September plans on an independent branch of the retained source session.
        var september = cwSession;
        var beginSeptember = CampaignSourceAuthorityState.SeptemberSupplyActionId("begin", 0, 0, 0, 0);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceSeptemberSupplyDraft(september, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, beginSeptember, CampaignActionAudience.Commonwealth)).RejectionReason);
        var normalSeptember = CampaignExercises.Query(september, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "axis-september-supply");
        Assert.DoesNotContain(CampaignExercises.Query(september, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "axis-september-supply");
        var beginCommand = Submission(september, normalSeptember.ActionId, CampaignActionAudience.Axis);
        september = CampaignExercises.Submit(september, beginCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(september, beginCommand).RejectionReason);
        foreach (var turn in new[] { 3, 4 })
        {
            var expected = SandtableRandom.RollD6(september.SourceAuthority!.RandomState);
            var septemberRoll = CampaignSourceAuthorityState.SeptemberSupplyActionId("roll", turn, 0, 0, 0);
            september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, septemberRoll, CampaignActionAudience.Axis)).SuccessorSession!;
            var entry = september.SourceAuthority!.SeptemberPlans!.Entries.Single(value => value.TargetGameTurn == turn);
            Assert.Equal(expected.Value, entry.Die);
            Assert.Equal(expected.Value switch { 1 => 0, <= 3 => 1, <= 5 => 2, _ => 3 }, entry.Maximum);
            Assert.Equal(expected.State.NextByteCursor, september.SourceAuthority.RandomState.NextByteCursor);
            var finish = CampaignSourceAuthorityState.SeptemberSupplyActionId("finish", turn, 0, 0, 0);
            if (entry.Maximum > 0)
            {
                var zeroPlan = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, finish, CampaignActionAudience.Axis)).SuccessorSession!;
                Assert.True(zeroPlan.SourceAuthority!.SeptemberPlans!.Entries.Single(value => value.TargetGameTurn == turn).Finished);
                Assert.Equal(0, zeroPlan.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == turn).Allocations.Sum());
                var add = CampaignSourceAuthorityState.SeptemberSupplyActionId("adjust", turn, 2, 1, 1);
                september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, add, CampaignActionAudience.Axis)).SuccessorSession!;
            }
            september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, finish, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        Assert.Null(september.SourceAuthority!.SeptemberPlans!.NextTargetGameTurn);
        Assert.Equal(38, september.SourceAuthority.ProfileVersion);
        var retainedGt3 = CampaignSourceGt1SupplyPlanning.Create(september.SourceAuthority, september.SourceAuthority.RandomState);
        Assert.Equal(september.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == 3).Die, retainedGt3.Die);
        Assert.True(retainedGt3.CarriedPlanFinished);
        Assert.Equal(september.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == 3).Allocations[0], retainedGt3.QuantityAt(2, 1));
        Assert.Throws<InvalidOperationException>(() => retainedGt3.Roll());
        var septemberHash = september.SourceAuthority.SeptemberPlans.Hash();
        september = CampaignExercises.AdmitSourceAxisSupplement(september).SuccessorSession!;
        Assert.Equal(septemberHash, september.SourceAuthority!.SeptemberPlans!.Hash());
        Assert.True(CampaignExercises.Reconstruct(september).IsVerified);
        Assert.False(CampaignExercises.IsSourceAxisSupplementAdmitted(cwSession));
        var migrated = CampaignExercises.AdmitSourceAxisSupplement(cwSession);
        Assert.True(migrated.IsAccepted);
        cwSession = migrated.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceAxisSupplementAdmitted(cwSession));
        Assert.Equal(5, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceAxisSupplement(cwSession).RejectionReason);
        var replacements = CampaignSourceAuthorityState.AutoblindaActionId("replacements", "");
        Assert.Contains(replacements, CampaignExercises.QuerySourceAutoblindaDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, replacements, CampaignActionAudience.Commonwealth)).RejectionReason);
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, replacements, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, cwSession.SourceAuthority!.Autoblinda!.ReplacementPoints);
        var reset = CampaignSourceAuthorityState.AutoblindaActionId("reset", "");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, reset, CampaignActionAudience.Axis)).SuccessorSession!;
        var attach1 = CampaignSourceAuthorityState.AutoblindaActionId("attach", "1-141-infantry");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach1, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach1, CampaignActionAudience.Axis)).RejectionReason);
        var attach2 = CampaignSourceAuthorityState.AutoblindaActionId("attach", "2-141-infantry");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach2, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(cwSession.SourceAuthority!.Autoblinda!.IsComplete);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.False(CampaignExercises.IsSourceCommonwealthSupplementAdmitted(cwSession));
        var cwMigrated = CampaignExercises.AdmitSourceCommonwealthSupplement(cwSession);
        Assert.True(cwMigrated.IsAccepted);
        cwSession = cwMigrated.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCommonwealthSupplementAdmitted(cwSession));
        Assert.Equal(6, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(12, cwSession.SourceAuthority!.CommonwealthSupplement!.Forces.Count);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceCommonwealthSupplement(cwSession).RejectionReason);
        Assert.False(CampaignExercises.IsSourcePriorityMotorizationAdmitted(cwSession));
        var priorityMigration = CampaignExercises.AdmitSourcePriorityMotorization(cwSession);
        Assert.True(priorityMigration.IsAccepted);
        cwSession = priorityMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourcePriorityMotorizationAdmitted(cwSession));
        Assert.Equal(7, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourcePriorityMotorizationDraft(cwSession, CampaignActionAudience.Axis));
        var priorityId = CampaignSourceAuthorityState.PriorityMotorizationActionId("1-kings-royal-rifle-corps", 6);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourcePriorityMotorization(cwSession, Submission(cwSession, priorityId, CampaignActionAudience.Axis)).RejectionReason);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, priorityId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(14, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3922-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        var reversePriority = CampaignSourceAuthorityState.PriorityMotorizationActionId("1-kings-royal-rifle-corps", -6);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, reversePriority, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(20, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3922-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        var secondPriority = CampaignSourceAuthorityState.PriorityMotorizationActionId("2-rifle-brigade", 6);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, secondPriority, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(4, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3320-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var axisMigration = CampaignExercises.AdmitSourceAxisPriorityMotorization(cwSession);
        Assert.True(axisMigration.IsAccepted);
        cwSession = axisMigration.SuccessorSession!;
        Assert.Equal(8, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var targetAxis = "1-1-libyan-motor-infantry";
        var oldDetach = CampaignSourceAuthorityState.AxisPriorityActionId("detach", targetAxis, 0);
        var historical = CampaignExercises.ReplaySourceAxisPriority(cwSession,
            Submission(cwSession, oldDetach, CampaignActionAudience.Axis));
        Assert.True(historical.IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(historical.SuccessorSession!).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, oldDetach, CampaignActionAudience.Axis)).RejectionReason);
        var prematureLift = CampaignSourceAuthorityState.AxisPriorityActionId("adjust", targetAxis, 5);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, prematureLift, CampaignActionAudience.Axis)).RejectionReason);
        foreach (var axisTarget in CampaignSourceAxisPriorityMotorization.Targets)
        {
            var detach = CampaignSourceAuthorityState.AxisPriorityActionId("detach", axisTarget, 0);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, detach, CampaignActionAudience.Axis)).RejectionReason);
        }
        Assert.Equal(35, cwSession.SourceAuthority!.AxisPriorityMotorization!.Remaining(cwSession.SourceAuthority.AxisMotorization));
        var undoDetachment = CampaignSourceAuthorityState.AxisPriorityActionId("undo-detachment", targetAxis, 0);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, undoDetachment, CampaignActionAudience.Axis)).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var garrisonMigration = CampaignExercises.AdmitSourceAxisMinorGarrisons(cwSession);
        Assert.True(garrisonMigration.IsAccepted);
        cwSession = garrisonMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceAxisMinorGarrisonsAdmitted(cwSession));
        Assert.Equal(9, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(13, cwSession.SourceAuthority!.AxisGarrisons!.Forces.Count);
        Assert.Contains(cwSession.SourceAuthority.AxisGarrisons.Forces,
            value => value.ElementId == "giarabub-motor-machine-gun");
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceAxisMinorGarrisons(cwSession).RejectionReason);
        var battlefieldMigration = CampaignExercises.AdmitSourceBattlefield(cwSession);
        Assert.True(battlefieldMigration.IsAccepted);
        cwSession = battlefieldMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceBattlefieldAdmitted(cwSession));
        Assert.Equal(10, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var battlefield = cwSession.SourceAuthority!.Battlefield!;
        Assert.Equal(battlefield.Entries.Count, battlefield.Entries.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(2, battlefield.Pending.Count);
        Assert.Equal(8, battlefield.Pending.Single(value => value.GroupId == Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId).ElementIds.Count);
        Assert.Equal(4, battlefield.Pending.Single(value => value.GroupId == Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId).ElementIds.Count);
        using var battlefieldBytes = JsonDocument.Parse(battlefield.SerializeCanonical());
        Assert.False(battlefieldBytes.RootElement.GetProperty("battlefieldComplete").GetBoolean());
        Assert.False(battlefieldBytes.RootElement.GetProperty("controlCertified").GetBoolean());
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceBattlefield(cwSession).RejectionReason);
        var cairoAdmission = CampaignExercises.AdmitSourceCairoSetup(cwSession);
        Assert.True(cairoAdmission.IsAccepted);
        cwSession = cairoAdmission.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCairoSetupAdmitted(cwSession));
        Assert.Equal(11, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(4, cwSession.SourceAuthority!.CairoSetup!.PendingIds.Count);
        Assert.Equal(4, cwSession.SourceAuthority.Battlefield!.Entries.Count(x => x.AuthorityStatus == "source-setup-unplaced"));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCairoSetupDraft(cwSession, CampaignActionAudience.Axis));
        var invalidCity = CampaignSourceAuthorityState.CairoPlacementActionId("1-welch", "e1829");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, invalidCity, CampaignActionAudience.Commonwealth)).RejectionReason);
        var inferredCity = CampaignSourceAuthorityState.CairoPlacementActionId("1-welch", "e1931");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, inferredCity, CampaignActionAudience.Commonwealth)).RejectionReason);
        var chosenCairoHex = seed == 14 ? "e1830" : "e1730";
        foreach (var element in CampaignSourceCairoInitialSetup.Units.Select(value => value.ElementId))
        {
            var action = CampaignSourceAuthorityState.CairoPlacementActionId(element, chosenCairoHex);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, action, CampaignActionAudience.Axis)).RejectionReason);
            var command = Submission(cwSession, action, CampaignActionAudience.Commonwealth);
            var placedCairo = CampaignExercises.PlaceSourceCairoUnit(cwSession, command);
            Assert.True(placedCairo.IsAccepted);
            cwSession = placedCairo.SuccessorSession!;
            Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
                CampaignExercises.PlaceSourceCairoUnit(cwSession, command).RejectionReason);
        }
        Assert.Empty(cwSession.SourceAuthority!.CairoSetup!.PendingIds);
        Assert.Equal(4, cwSession.SourceAuthority.Battlefield!.Entries.Count(x => x.LocationId == chosenCairoHex && x.AuthorityStatus == "source-setup-placed"));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var activeMigration = CampaignExercises.AdmitSourceCairoWorld(cwSession);
        Assert.True(activeMigration.IsAccepted);
        cwSession = activeMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCairoWorldAdmitted(cwSession));
        Assert.Equal(12, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var active = cwSession.SourceAuthority!.CairoWorld!;
        Assert.Equal("sha256:3a7fddd510854c81b408e07742dd837f7be416206117ca58ea79357357a11ddd", active.Content.Hash());
        Assert.NotEqual(cwSession.SourceAuthority.Content.Identity.Hash, active.Content.Identity.Hash);
        Assert.Equal(active.Content.Identity, active.World.ContentIdentity);
        Assert.Equal(8, active.World.World.Elements.Count);
        Assert.Equal(8, active.World.World.Representations.Count);
        Assert.Equal(2, active.World.PendingArrivals.Count);
        foreach (var unit in CampaignSourceCairoInitialSetup.Units)
        {
            var element = Assert.Single(active.World.World.Elements, value => value.ElementId == unit.ElementId);
            Assert.Equal(chosenCairoHex, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
        }
        var alteredContentText = System.Text.Encoding.UTF8.GetString(active.Content.CanonicalBytes)
            .Replace("\"initialToe\":6", "\"initialToe\":7", StringComparison.Ordinal);
        var alteredContent = ContentSourceSetupPackArtifact.Read(System.Text.Encoding.UTF8.GetBytes(alteredContentText));
        Assert.NotEqual(active.Content.Hash(), alteredContent.Hash());
        var forgedHistory = cwSession.PregameHistory.Select(value => value.ToArray()).ToArray();
        var forgedEvent = System.Text.Encoding.UTF8.GetString(forgedHistory[^1])
            .Replace(active.Content.Hash(), alteredContent.Hash(), StringComparison.Ordinal);
        forgedHistory[^1] = System.Text.Encoding.UTF8.GetBytes(forgedEvent);
        var forgedSession = new ExerciseSession(cwSession.Pregame!, cwSession.CurrentContacts, forgedHistory, cwSession.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(forgedSession).IsVerified);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceCairoWorld(cwSession).RejectionReason);
        var directV12 = cwSession;
        Assert.True(CampaignExercises.CanPromoteSourceIndependent(directV12));
        var lockBeforePromotion = CampaignExercises.Query(directV12, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var lockedBeforePromotion = CampaignExercises.Submit(directV12,
            Submission(directV12, lockBeforePromotion.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.PromoteSourceIndependent(lockedBeforePromotion).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.ReplaySourceIndependentPromotion(lockedBeforePromotion).RejectionReason);
        directV12 = CampaignExercises.PromoteSourceIndependent(directV12).SuccessorSession!;
        Assert.Equal(14, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(10, directV12.SourceAuthority!.World.World.Elements.Count);
        var genericIdentity = directV12.SourceAuthority.World.ContentIdentity;
        Assert.False(CampaignExercises.CanPromoteSourceIndependent(directV12));
        directV12 = CampaignExercises.AdmitSourceLocalMap(directV12).SuccessorSession!;
        Assert.Equal(15, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var mapIdentity = directV12.SourceAuthority!.World.ContentIdentity;
        Assert.NotEqual(genericIdentity, mapIdentity);
        Assert.Equal(10, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.PromoteSourceExpandedIndependent(directV12).SuccessorSession!;
        Assert.Equal(16, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(13, directV12.SourceAuthority!.World.World.Elements.Count);
        var expandedIdentity = directV12.SourceAuthority.World.ContentIdentity;
        Assert.False(CampaignExercises.CanPromoteSourceExpandedIndependent(directV12));
        Assert.False(CampaignExercises.CanPromoteSourceMaletti(directV12));
        Assert.Equal(expandedIdentity, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(mapIdentity, directV12.SourceAuthority.LocalMapWorld!.Content.Identity);
        Assert.Equal(genericIdentity, directV12.SourceAuthority.IndependentWorld!.Content.Identity);
        Assert.Equal(13, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.AdmitSourceAttachedInitialLoads(directV12).SuccessorSession!;
        Assert.Equal(17, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        const string loadedTarget = "2-rifle-brigade";
        var attach = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("attach", loadedTarget);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, attach, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, attach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, directV12.SourceAuthority!.AttachedInitialLoads!.Reserved("cw-c3320-attached"));
        var fuel = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("fuel", loadedTarget);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, fuel, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, Assert.Single(directV12.SourceAuthority!.AttachedInitialLoads!.Targets).FuelPoints);
        var detachLoad = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("detach", loadedTarget);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, detachLoad, CampaignActionAudience.Commonwealth)).RejectionReason);
        var removeLift = CampaignSourceAuthorityState.PriorityMotorizationActionId(loadedTarget, -6);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourcePriorityMotorization(directV12, Submission(directV12, removeLift, CampaignActionAudience.Commonwealth)).RejectionReason);
        var clear = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("unload-fuel", loadedTarget);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, clear, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, detachLoad, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Empty(directV12.SourceAuthority!.AttachedInitialLoads!.Targets);
        directV12 = CampaignExercises.AdmitSourceInitialWrecks(directV12).SuccessorSession!;
        Assert.Equal(18, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(3, directV12.SourceAuthority!.InitialWrecks!.PendingIds.Count);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceInitialWrecksDraft(directV12, CampaignActionAudience.Axis));
        foreach (var id in directV12.SourceAuthority.InitialWrecks.PendingIds.ToArray())
        {
            var place = CampaignSourceAuthorityState.InitialWreckPlacementActionId(id, "e3613");
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceInitialWreck(directV12, Submission(directV12, place, CampaignActionAudience.Axis)).RejectionReason);
            directV12 = CampaignExercises.PlaceSourceInitialWreck(directV12, Submission(directV12, place, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        }
        Assert.Empty(directV12.SourceAuthority!.InitialWrecks!.PendingIds);
        Assert.Equal(3, directV12.SourceAuthority.InitialWrecks.Placements.Count);
        directV12 = CampaignExercises.AdmitSourceAxisFreeSetup(directV12).SuccessorSession!;
        Assert.Equal(19, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var forgedLegacyState = directV12.SourceAuthority!.ApplyAxisPriorityChoice("detach", targetAxis, 0);
        var forgedLegacyEvent = directV12.SourceAuthority.SerializeAxisPriorityEvent(forgedLegacyState, "detach", targetAxis, 0);
        var forgedLegacySession = new ExerciseSession(directV12.Pregame!, directV12.CurrentContacts.AdvancePregameSource(directV12.SourceAuthority.StateVersion),
            [.. directV12.PregameHistory, forgedLegacyEvent], forgedLegacyState);
        Assert.False(CampaignExercises.Reconstruct(forgedLegacySession).IsVerified);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisFreeSetupDraft(directV12, CampaignActionAudience.Commonwealth));
        var freeIds = directV12.SourceAuthority!.AxisFreeArtillery!.PendingIds.ToArray();
        for (var i = 0; i < freeIds.Length; i++)
        {
            var location = i < 2 ? "c4407" : "c4607";
            var action = CampaignSourceAuthorityState.AxisFreePlacementActionId(freeIds[i], location);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, action, CampaignActionAudience.Commonwealth)).RejectionReason);
            if (i == 2)
            {
                var overstack = CampaignSourceAuthorityState.AxisFreePlacementActionId(freeIds[i], "c4407");
                Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                    CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, overstack, CampaignActionAudience.Axis)).RejectionReason);
            }
            directV12 = CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, action, CampaignActionAudience.Axis)).SuccessorSession!;
            if (i == 0)
            {
                directV12 = CampaignExercises.PromoteSourceAxisFreeWorld(directV12).SuccessorSession!;
                Assert.Equal(20, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
                Assert.Equal(14, directV12.SourceAuthority!.World.World.Elements.Count);
                var identityAfterPartial = directV12.SourceAuthority.World.ContentIdentity;
                var adjustAxis = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", 1);
                directV12 = CampaignExercises.AdjustSourceMotorization(directV12,
                    Submission(directV12, adjustAxis, CampaignActionAudience.Axis)).SuccessorSession!;
                directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12,
                    Submission(directV12, attach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
                Assert.Equal(identityAfterPartial, directV12.SourceAuthority!.World.ContentIdentity);
                Assert.Equal(14, directV12.SourceAuthority.World.World.Elements.Count);
                Assert.False(CampaignExercises.CanPromoteSourceIndependent(directV12));
                Assert.False(CampaignExercises.CanPromoteSourceExpandedIndependent(directV12));
            }
        }
        Assert.Empty(directV12.SourceAuthority!.AxisFreeArtillery!.PendingIds);
        Assert.Equal(3, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-1ar-placed"));
        var axisContent = directV12.SourceAuthority.World.ContentIdentity;
        directV12 = CampaignExercises.PromoteSourceAxisFreeWorld(directV12).SuccessorSession!;
        Assert.Equal(axisContent, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(17, directV12.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Equal(4, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-1ar-active"));
        Assert.False(CampaignExercises.CanPromoteSourceAxisFreeWorld(directV12));
        directV12 = CampaignExercises.AdmitSourceAxisCorpsSetup(directV12).SuccessorSession!;
        Assert.Equal(21, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisCorpsSetupDraft(directV12, CampaignActionAudience.Commonwealth));
        var corpsIds = directV12.SourceAuthority!.AxisCorpsArtillery!.PendingIds.ToArray();
        for (var i = 0; i < corpsIds.Length; i++)
        {
            var location = i == 0 ? "c4307" : "c4308";
            var action = CampaignSourceAuthorityState.AxisCorpsPlacementActionId(corpsIds[i], location);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, action, CampaignActionAudience.Commonwealth)).RejectionReason);
            if (i == 1)
            {
                var occupied = CampaignSourceAuthorityState.AxisCorpsPlacementActionId(corpsIds[i], "c4307");
                Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                    CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, occupied, CampaignActionAudience.Axis)).RejectionReason);
            }
            directV12 = CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, action, CampaignActionAudience.Axis)).SuccessorSession!;
            directV12 = CampaignExercises.PromoteSourceAxisCorpsWorld(directV12).SuccessorSession!;
            Assert.Equal(22, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        }
        Assert.Equal(19, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Equal(2, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-corps-artillery-active"));
        foreach (var id in corpsIds)
            Assert.Equal(3, directV12.SourceAuthority.World.World.Elements.Single(value => value.ElementId == id).Components.Single(value => value.ComponentId.EndsWith(".light-aa", StringComparison.Ordinal)).CurrentToe);
        Assert.False(CampaignExercises.CanPromoteSourceAxisCorpsWorld(directV12));
        var corpsContent = directV12.SourceAuthority.World.ContentIdentity;
        var removeAxisPoint = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", -1);
        directV12 = CampaignExercises.AdjustSourceMotorization(directV12,
            Submission(directV12, removeAxisPoint, CampaignActionAudience.Axis)).SuccessorSession!;
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12,
            Submission(directV12, fuel, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(corpsContent, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(19, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.AdmitSourceMalettiHierarchy(directV12).SuccessorSession!;
        Assert.Equal(23, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var confirmHierarchy = CampaignSourceAuthorityState.MalettiHierarchyActionId("confirm", null);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMalettiHierarchy(directV12, Submission(directV12, confirmHierarchy, CampaignActionAudience.Commonwealth)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceMalettiHierarchy(directV12,
            Submission(directV12, confirmHierarchy, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwFirst = directV12;
        var frenchFirst = directV12;
        var certifiedDetach = CampaignSourceAuthorityState.MalettiHierarchyActionId("detach", targetAxis);
        directV12 = CampaignExercises.SubmitSourceMalettiHierarchy(directV12,
            Submission(directV12, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(targetAxis, directV12.SourceAuthority!.MalettiHierarchy!.DetachedId);
        var otherDetach = CampaignSourceAuthorityState.MalettiHierarchyActionId("detach", "3-1-libyan-motor-infantry");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMalettiHierarchy(directV12, Submission(directV12, otherDetach, CampaignActionAudience.Axis)).RejectionReason);
        Assert.True(CampaignExercises.CanPromoteSourceCertifiedMalettiWorld(directV12));
        directV12 = CampaignExercises.PromoteSourceCertifiedMalettiWorld(directV12).SuccessorSession!;
        Assert.Equal(24, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(20, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Contains(directV12.SourceAuthority.Battlefield!.Entries,
            value => value.ElementId == targetAxis && value.AuthorityStatus == "source-certified-maletti-active");
        Assert.False(CampaignExercises.CanPromoteSourceCertifiedMalettiWorld(directV12));
        var axisLift = CampaignSourceAuthorityState.AxisPriorityActionId("adjust", targetAxis, 5);
        var lockBeforeLift = CampaignExercises.Query(directV12, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var lockedBeforeLift = CampaignExercises.Submit(directV12,
            Submission(directV12, lockBeforeLift.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(lockedBeforeLift,
                Submission(lockedBeforeLift, axisLift, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceAxisPriority(directV12,
            Submission(directV12, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(5, directV12.SourceAuthority!.AxisPriorityMotorization!.AssignedPoints(targetAxis));
        directV12 = CampaignExercises.AdmitSourceMatruhFormation(directV12).SuccessorSession!;
        Assert.Equal(25, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceMatruhFormationDraft(directV12, CampaignActionAudience.Axis));
        var matruhConfirm = CampaignSourceAuthorityState.MatruhFormationActionId("confirm", null);
        directV12 = CampaignExercises.SubmitSourceMatruhFormation(directV12,
            Submission(directV12, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(5, directV12.SourceAuthority!.MatruhFormation!.AttachedIds.Count);
        var matruhDetach = CampaignSourceAuthorityState.MatruhFormationActionId("detach", "1-essex");
        directV12 = CampaignExercises.SubmitSourceMatruhFormation(directV12,
            Submission(directV12, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(4, directV12.SourceAuthority!.MatruhFormation!.AttachedIds.Count);
        Assert.Contains(directV12.SourceAuthority.Battlefield!.Entries,
            entry => entry.ElementId == "1-essex" && entry.AuthorityStatus == "source-matruh-detached");
        var secondMatruhDetach = CampaignSourceAuthorityState.MatruhFormationActionId("detach", "1-durham-light-infantry");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMatruhFormation(directV12, Submission(directV12, secondMatruhDetach, CampaignActionAudience.Commonwealth)).RejectionReason);
        directV12 = CampaignExercises.PromoteSourceMatruhDetachedWorld(directV12).SuccessorSession!;
        Assert.Equal(26, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(21, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(4, directV12.SourceAuthority.MatruhFormation!.AttachedIds.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        cwFirst = CampaignExercises.AdmitSourceMatruhFormation(cwFirst).SuccessorSession!;
        cwFirst = CampaignExercises.SubmitSourceMatruhFormation(cwFirst,
            Submission(cwFirst, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        cwFirst = CampaignExercises.SubmitSourceMatruhFormation(cwFirst,
            Submission(cwFirst, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        cwFirst = CampaignExercises.PromoteSourceMatruhDetachedWorld(cwFirst).SuccessorSession!;
        var cwFirstIdentity = cwFirst.SourceAuthority!.World.ContentIdentity;
        cwFirst = CampaignExercises.SubmitSourceMalettiHierarchy(cwFirst,
            Submission(cwFirst, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        cwFirst = CampaignExercises.PromoteSourceCertifiedMalettiWorld(cwFirst).SuccessorSession!;
        Assert.NotEqual(cwFirstIdentity, cwFirst.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(21, cwFirst.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, cwFirst.SourceAuthority.World.PendingArrivals.Count);
        Assert.True(CampaignExercises.Reconstruct(cwFirst).IsVerified);
        var rngBeforeGt2 = directV12.SourceAuthority!.RandomState;
        var worldBeforeGt2 = directV12.SourceAuthority.World.ContentIdentity;
        directV12 = CampaignExercises.AdmitSourceGt2Correction(directV12).SuccessorSession!;
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var finishGt2 = CampaignSourceAuthorityState.Gt2CorrectionActionId("finish", 0, 0, 0);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Commonwealth)).RejectionReason);
        if (maximum > 0)
        {
            foreach (var lane in new[] { 2, 3, 6 })
                for (var stage = 1; stage <= 3; stage++)
                    while (directV12.SourceAuthority!.Gt2Correction!.Quantity(lane, stage) > 0)
                    {
                        var removeUnit = CampaignSourceAuthorityState.Gt2CorrectionActionId("adjust", lane, stage, -1);
                        directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12,
                            Submission(directV12, removeUnit, CampaignActionAudience.Axis)).SuccessorSession!;
                    }
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Axis)).RejectionReason);
            var addGt2 = CampaignSourceAuthorityState.Gt2CorrectionActionId("adjust", 2, 1, 1);
            directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, addGt2, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(directV12.SourceAuthority!.Gt2Correction!.IsFinished);
        Assert.Equal(rngBeforeGt2, directV12.SourceAuthority.RandomState);
        Assert.Equal(worldBeforeGt2, directV12.SourceAuthority.World.ContentIdentity);
        directV12 = CampaignExercises.AdmitSourceCwPriorityCatalog(directV12).SuccessorSession!;
        Assert.Equal(28, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        directV12 = CampaignExercises.PromoteSourceCwPriorityWorld(directV12).SuccessorSession!;
        Assert.Equal(29, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(22, directV12.SourceAuthority!.World.World.Elements.Count);
        var frenchAdd = CampaignSourceAuthorityState.CwPriorityCatalogActionId("1-french-motor-marines", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceCwPriorityCatalog(directV12, Submission(directV12, frenchAdd, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceCwPriorityCatalog(directV12,
            Submission(directV12, frenchAdd, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, directV12.SourceAuthority!.CwPriorityCatalog!.FrenchPoints);
        Assert.Equal(1, directV12.SourceAuthority.CwPriorityCatalog.Remaining(directV12.SourceAuthority, "cw-c3926-attached"));
        var frenchRemove = CampaignSourceAuthorityState.CwPriorityCatalogActionId("1-french-motor-marines", -1);
        directV12 = CampaignExercises.SubmitSourceCwPriorityCatalog(directV12,
            Submission(directV12, frenchRemove, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(2, directV12.SourceAuthority!.CwPriorityCatalog!.Remaining(directV12.SourceAuthority, "cw-c3926-attached"));
        frenchFirst = CampaignExercises.AdmitSourceCwPriorityCatalog(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.PromoteSourceCwPriorityWorld(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.AdmitSourceCwFreeGuns(frenchFirst).SuccessorSession!;
        Assert.Equal(31, frenchFirst.SourceAuthority!.ProfileVersion);
        var freeGunIds = frenchFirst.SourceAuthority!.CwFreeGuns!.PendingIds.ToArray();
        var gunCairoHex = seed == 14 ? "e1730" : "e1830";
        foreach (var id in freeGunIds)
        {
            var placeGun = CampaignSourceAuthorityState.CwFreeGunPlacementActionId(id, gunCairoHex);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCwFreeGun(frenchFirst, Submission(frenchFirst, placeGun, CampaignActionAudience.Axis)).RejectionReason);
            frenchFirst = CampaignExercises.PlaceSourceCwFreeGun(frenchFirst,
                Submission(frenchFirst, placeGun, CampaignActionAudience.Commonwealth)).SuccessorSession!;
            frenchFirst = CampaignExercises.PromoteSourceCwFreeGunsWorld(frenchFirst).SuccessorSession!;
        }
        Assert.Equal(32, frenchFirst.SourceAuthority!.ProfileVersion);
        _ = ContentSourceFourthIndianPackArtifact.Create(frenchFirst.SourceAuthority!.CwFreeGunsWorld!.Content);
        var indianResult = CampaignExercises.AdmitSourceFourthIndianWorld(frenchFirst);
        Assert.True(indianResult.IsAccepted);
        var indian = indianResult.SuccessorSession!;
        var grouped = indian.SourceAuthority!.GroupedWorld!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(indian.SourceAuthority.SerializeLegacyCheckpoint()),
            indian.SourceAuthority.DigestLegacyCheckpoint());
        var beforeMinor = indian.SourceAuthority.SerializeCheckpoint();
        var pendingBeforeMinor = indian.SourceAuthority.World.PendingArrivals;
        var randomBeforeMinor = indian.SourceAuthority.RandomState;
        var minorResult = CampaignExercises.ActivateSourceAxisMinorGarrisons(indian);
        Assert.True(minorResult.IsAccepted);
        var minor = minorResult.SuccessorSession!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(minor.SourceAuthority!.CwFreeGunsWorld!.SerializeCanonical()),
            minor.SourceAuthority.CwFreeGunsWorld.Hash());
        Assert.Equal(CampaignPregameSourceCreation.Digest(minor.SourceAuthority!.SerializeLegacyCheckpoint()),
            minor.SourceAuthority.DigestLegacyCheckpoint());
        Assert.Equal(beforeMinor, indian.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.AxisMinorGarrisonsCheckpointContractVersion, minor.SourceAuthority!.ProfileVersion);
        Assert.Equal(randomBeforeMinor, minor.SourceAuthority.RandomState);
        Assert.Equal(pendingBeforeMinor, minor.SourceAuthority.World.PendingArrivals);
        Assert.Equal(grouped.ContentHash, minor.SourceAuthority.GroupedWorld!.ContentHash);
        Assert.Equal(grouped.Groups.Count, minor.SourceAuthority.GroupedWorld.Groups.Count);
        Assert.Equal(0, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("fort-maddalena-garrison", "c3019"));
        Assert.Equal(1, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("fort-maddalena-garrison", "c3020"));
        Assert.Equal(0, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("mechili-garrison", "b4921"));
        Assert.Equal(1, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("mechili-garrison", "b4922"));
        foreach (var unit in ContentSourceAxisMinorGarrisonsPackArtifact.Units)
        {
            Assert.Equal(0, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt(unit.Id, unit.Location));
            var element = Assert.Single(minor.SourceAuthority.World.World.Elements, value => value.ElementId == unit.Id);
            Assert.Equal(unit.Location, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
            Assert.Single(minor.SourceAuthority.World.World.Representations, value =>
                value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
                && value.CurrentLocationId == unit.Location && value.BoundElementIds.SequenceEqual([unit.Id]));
            Assert.Contains(minor.SourceAuthority.Battlefield!.Entries, value => value.ElementId == unit.Id
                && value.AuthorityStatus == "source-axis-minor-active");
        }
        // Use the actual activated counters, not source-evidence-only RTR or
        // Hussars. The two observers are independent counters, not formations.
        var minorContactWorld = CampaignSourcePlayableWorld.Create(minor.SourceAuthority);
        var activePair = CampaignSourcePolishArrivalContacts.EffectiveCounters(minorContactWorld)
            .Where(value => value.RepresentationId is "axis-minor.scheferzen-garrison" or "independent-setup.2-rifle-brigade")
            .ToArray();
        Assert.Equal(2, activePair.Length);
        Assert.All(activePair, value => Assert.Equal(CampaignContactObserverKind.IndependentCounter, value.ObserverKind));
        var pairAxis = new CampaignContactKnowledge(minor.SourceAuthority.Request.CampaignId, LandSide.Axis);
        var pairCw = new CampaignContactKnowledge(minor.SourceAuthority.Request.CampaignId, LandSide.Commonwealth);
        var axisPairContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(activePair, 1, pairAxis));
        var cwPairContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(activePair, 1, pairCw));
        Assert.Equal(CampaignContactKnowledge.IndependentObserverContractVersion, axisPairContact.ContractVersion);
        Assert.Equal(CampaignContactKnowledge.IndependentObserverContractVersion, cwPairContact.ContractVersion);
        var axisPairBytes = CampaignContactTransitionCodec.SerializeCanonical(axisPairContact);
        Assert.Equal(axisPairContact, CampaignContactTransitionCodec.DeserializeCanonical(axisPairBytes));
        Assert.Equal(axisPairBytes, CampaignContactTransitionCodec.SerializeCanonical(Assert.Single(
            CampaignSourceInitialContacts.DeriveCounters(activePair, 1,
                new CampaignContactKnowledge(minor.SourceAuthority.Request.CampaignId, LandSide.Axis)))));
        pairAxis.Apply(axisPairContact);
        pairCw.Apply(cwPairContact);
        Assert.Empty(pairAxis.ObserveFor(axisPairContact.ObserverFormationId));
        var ownCounterView = CampaignIndependentContactObservationV2.Project(minorContactWorld, pairAxis,
            "axis-minor.scheferzen-garrison");
        Assert.Single(ownCounterView.Contacts);
        Assert.DoesNotContain("independent-setup.2-rifle-brigade", System.Text.Encoding.UTF8.GetString(ownCounterView.SerializeCanonical()));
        Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(minorContactWorld,
            pairCw, "axis-minor.scheferzen-garrison"));
        var repeatedPairContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(activePair, 2, pairAxis));
        Assert.Equal(axisPairContact.ContactHandle, repeatedPairContact.ContactHandle);
        Assert.True(CampaignExercises.Reconstruct(minor).IsVerified);
        var compactMinorResult = CampaignExercises.AdmitSourceCompactCheckpoint(minor);
        Assert.True(compactMinorResult.IsAccepted);
        var compactMinor = compactMinorResult.SuccessorSession!;
        var compactMinorBytes = compactMinor.SourceAuthority!.SerializeCheckpoint();
        using (var compactMinorJson = JsonDocument.Parse(compactMinorBytes))
        {
            var baseBytes = compactMinorJson.RootElement.GetProperty("baseCheckpoint").GetBytesFromBase64();
            var artifactBytes = compactMinor.SourceAuthority.CwFreeGunsWorld!.MinorGarrisonContent!.CanonicalBytes;
            Console.WriteLine($"minor checkpoint bytes: compact={compactMinorBytes.Length}, base={baseBytes.Length}, artifact={artifactBytes.Length}");
            Assert.True(compactMinorBytes.Length < 16_384, $"Compact minor checkpoint was {compactMinorBytes.Length} bytes; base {baseBytes.Length}, artifact {artifactBytes.Length}.");
            Assert.False(compactMinorJson.RootElement.TryGetProperty("minorGarrisonContent", out _));
            Assert.Equal(compactMinor.SourceAuthority.World.ContentIdentity.Hash,
                compactMinorJson.RootElement.GetProperty("minorGarrisonContentHash").GetString());
        }
        Assert.True(CampaignExercises.Reconstruct(compactMinor).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceAxisGarrisonRemainder(minor).IsAccepted);
        var oldMinorCheckpoint = compactMinor.SourceAuthority.SerializeCheckpoint();
        var remainderResult = CampaignExercises.ActivateSourceAxisGarrisonRemainder(compactMinor);
        Assert.True(remainderResult.IsAccepted);
        var remainder = remainderResult.SuccessorSession!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(remainder.SourceAuthority!.CwFreeGunsWorld!.SerializeCanonical()),
            remainder.SourceAuthority.CwFreeGunsWorld.Hash());
        Console.WriteLine($"garrison remainder content hash: {remainder.SourceAuthority!.World.ContentIdentity.Hash}");
        if (seed == 14)
            Assert.Equal(CampaignPregameSourceCreation.Digest(remainder.SourceAuthority.SerializeLegacyCheckpoint()),
                remainder.SourceAuthority.DigestLegacyCheckpoint());
        var remainderCheckpointBytes = remainder.SourceAuthority!.SerializeCheckpoint();
        Assert.True(remainderCheckpointBytes.Length < 24 * 1024 * 1024,
            $"Legacy garrison remainder checkpoint was {remainderCheckpointBytes.Length} bytes.");
        Assert.Equal(oldMinorCheckpoint, compactMinor.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.AxisGarrisonRemainderCheckpointContractVersion, remainder.SourceAuthority.ProfileVersion);
        Assert.Equal(ContentSourceAxisGarrisonRemainderPackArtifact.ContentHash, remainder.SourceAuthority.World.ContentIdentity.Hash);
        Assert.Equal(minor.SourceAuthority.RandomState, remainder.SourceAuthority.RandomState);
        Assert.Equal(minor.SourceAuthority.World.PendingArrivals, remainder.SourceAuthority.World.PendingArrivals);
        Assert.Equal(minor.SourceAuthority.GroupedWorld!.ContentHash, remainder.SourceAuthority.GroupedWorld!.ContentHash);
        Assert.Equal(ExpectedGarrisonRemainderIds,
            ContentSourceAxisGarrisonRemainderPackArtifact.Units.Select(value => value.Id).Order(StringComparer.Ordinal));
        foreach (var unit in ContentSourceAxisGarrisonRemainderPackArtifact.Units)
        {
            var element = Assert.Single(remainder.SourceAuthority.World.World.Elements, value => value.ElementId == unit.Id);
            var sourceComponent = ContentSourceAxisGarrisonRemainderPackArtifact.Facts(unit.Id).Components.Single();
            Assert.Equal(unit.Location, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
            Assert.Equal(sourceComponent.ComponentId, element.Components[0].ComponentId);
            Assert.Equal(0, ContentSourceAxisGarrisonRemainderPackArtifact.StackAt(unit.Id, unit.Location));
            Assert.Single(remainder.SourceAuthority.World.World.Representations, value =>
                value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
                && value.CurrentLocationId == unit.Location && value.BoundElementIds.SequenceEqual([unit.Id]));
            Assert.Contains(remainder.SourceAuthority.Battlefield!.Entries, value => value.ElementId == unit.Id
                && value.AuthorityStatus == "source-axis-garrison-remainder-active");
        }
        var remainderContactWorld = CampaignSourcePlayableWorld.Create(remainder.SourceAuthority);
        var remainderCounters = CampaignSourcePolishArrivalContacts.EffectiveCounters(remainderContactWorld);
        var remainderAxisKnowledge = new CampaignContactKnowledge(remainder.SourceAuthority.Request.CampaignId, LandSide.Axis);
        var remainderCwKnowledge = new CampaignContactKnowledge(remainder.SourceAuthority.Request.CampaignId, LandSide.Commonwealth);
        // Both counters come from the admitted source world. The chart proves
        // Soluch and Matruh are separated without inferring a playable seam.
        var soluchAndIndian = remainderCounters.Where(value => value.RepresentationId is
            "axis-garrison.soluch-antitank-company" or "4-indian-division").ToArray();
        Assert.Equal(2, soluchAndIndian.Length);
        Assert.Empty(CampaignSourceInitialContacts.DeriveCounters(soluchAndIndian, 1, remainderAxisKnowledge));
        Assert.Empty(CampaignSourceInitialContacts.DeriveCounters(soluchAndIndian, 1, remainderCwKnowledge));
        Assert.Equal(6, ContentSourceAxisGarrisonRemainderPackArtifact.Units.Count);
        foreach (var removedId in ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds)
        {
            var observerId = "axis-garrison." + removedId;
            Assert.DoesNotContain(remainderCounters, value => value.RepresentationId == observerId);
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(remainderContactWorld, remainderAxisKnowledge, observerId));
        }
        foreach (var unit in ContentSourceAxisGarrisonRemainderPackArtifact.Units)
        {
            var id = "axis-garrison." + unit.Id;
            var counter = Assert.Single(remainderCounters, value => value.RepresentationId == id);
            Assert.Equal(CampaignContactObserverKind.IndependentCounter, counter.ObserverKind);
            Assert.Equal(id, counter.ObserverFormationId);
            Assert.Empty(CampaignIndependentContactObservationV2.Project(remainderContactWorld, remainderAxisKnowledge, id).Contacts);
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(remainderContactWorld, remainderCwKnowledge, id));
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(minorContactWorld, pairAxis, id));
        }
        Assert.Equal(1, ContentSourceAxisGarrisonRemainderPackArtifact.StackAt("soluch-artillery-regiment", "a4131"));
        Assert.Equal(1, ContentSourceAxisGarrisonRemainderPackArtifact.StackAt("giarabub-machine-gun", "c1015"));
        Assert.DoesNotContain(remainder.SourceAuthority.World.World.Elements, value => value.ElementId == "giarabub-motor-machine-gun");
        Assert.Contains(remainder.SourceAuthority.Battlefield!.Entries, value => value.ElementId == "giarabub-motor-machine-gun"
            && value.AuthorityStatus == "source-evidence");
        foreach (var id in ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds)
        {
            Assert.DoesNotContain(remainder.SourceAuthority.World.World.Elements, value => value.ElementId == id);
            Assert.DoesNotContain(remainder.SourceAuthority.World.World.Representations, value => value.BoundElementIds.Contains(id));
            var removedEntry = Assert.Single(remainder.SourceAuthority.Battlefield!.Entries, value => value.ElementId == id
                && value.AuthorityStatus == "source-land-only-aa-removed");
            Assert.DoesNotContain(CampaignSourcePlayableWorld.Create(remainder.SourceAuthority).UnresolvedAt(removedEntry.LocationId!),
                value => value.ElementId == id);
        }
        using (var contentDocument = JsonDocument.Parse(remainder.SourceAuthority.CwFreeGunsWorld!.GarrisonRemainderContent!.CanonicalBytes))
        {
            var removedIds = contentDocument.RootElement.GetProperty("removedAntiAirUnits").EnumerateArray()
                .Select(value => value.GetString()!).ToArray();
            Assert.Equal(ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds, removedIds);
            Assert.Equal("land:32.83(1)-italian-uu-removed-from-play", contentDocument.RootElement.GetProperty("antiAirTreatment").GetString());
        }
        Assert.True(CampaignExercises.Reconstruct(remainder).IsVerified);
        var remainderHistory = remainder.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var remainderEvent = JsonDocument.Parse(remainderHistory[^1]))
        {
            var worldHash = remainderEvent.RootElement.GetProperty("worldHash").GetString()!;
            var changedHash = worldHash[..^1] + (worldHash[^1] == '0' ? '1' : '0');
            remainderHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(remainderHistory[^1])
                .Replace(worldHash, changedHash, StringComparison.Ordinal));
        }
        var tamperedRemainder = new ExerciseSession(remainder.Pregame!, remainder.CurrentContacts,
            remainderHistory, remainder.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(tamperedRemainder).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceAxisGarrisonRemainder(remainder).IsAccepted);
        var compactRemainder = remainder;
        var compactRemainderBytes = compactRemainder.SourceAuthority!.SerializeCheckpoint();
        using (var remainderCheckpoint = JsonDocument.Parse(compactRemainderBytes))
        {
            Assert.True(compactRemainderBytes.Length < 16_384, $"Compact remainder checkpoint was {compactRemainderBytes.Length} bytes.");
            Assert.False(remainderCheckpoint.RootElement.TryGetProperty("garrisonRemainderContent", out _));
            Assert.Equal(compactRemainder.SourceAuthority.World.ContentIdentity.Hash,
                remainderCheckpoint.RootElement.GetProperty("garrisonRemainderContentHash").GetString());
        }
        Assert.True(CampaignExercises.Reconstruct(compactRemainder).IsVerified);
        var priorRemainderCheckpoint = remainder.SourceAuthority.SerializeCheckpoint();
        var priorAxisMotorization = remainder.SourceAuthority.AxisMotorization.Hash();
        var motorMgResult = CampaignExercises.ActivateSourceGiarabubMotorMachineGun(remainder);
        Assert.True(motorMgResult.IsAccepted);
        var motorMg = motorMgResult.SuccessorSession!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(motorMg.SourceAuthority!.CwFreeGunsWorld!.SerializeCanonical()),
            motorMg.SourceAuthority.CwFreeGunsWorld.Hash());
        Assert.Equal(priorRemainderCheckpoint, remainder.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.GiarabubMotorMachineGunCheckpointContractVersion, motorMg.SourceAuthority!.ProfileVersion);
        Assert.Equal(priorAxisMotorization, motorMg.SourceAuthority.AxisMotorization.Hash());
        Assert.Equal(remainder.SourceAuthority.RandomState, motorMg.SourceAuthority.RandomState);
        Assert.Equal(remainder.SourceAuthority.World.PendingArrivals, motorMg.SourceAuthority.World.PendingArrivals);
        var motorMgElement = Assert.Single(motorMg.SourceAuthority.World.World.Elements,
            value => value.ElementId == ContentSourceGiarabubMotorMachineGunArtifact.ElementId);
        Assert.Equal("c1014", motorMgElement.CurrentLocationId);
        Assert.Equal(1, Assert.Single(motorMgElement.Components).CurrentToe);
        Assert.Single(motorMg.SourceAuthority.World.World.Representations, value =>
            value.RepresentationId == "axis-garrison.giarabub-motor-machine-gun"
            && value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement);
        var motorMgContent = motorMg.SourceAuthority.CwFreeGunsWorld!.MotorMachineGunContent!;
        Console.WriteLine($"giarabub motor MG content hash: {motorMgContent.Identity.Hash}");
        Assert.Equal(ContentSourceGiarabubMotorMachineGunArtifact.ContentHash, motorMgContent.Identity.Hash);
        using (var document = JsonDocument.Parse(motorMgContent.CanonicalBytes))
        {
            var root = document.RootElement;
            Assert.Equal("8+", root.GetProperty("printedCapabilityPoints").GetString());
            Assert.Equal(JsonValueKind.Null, root.GetProperty("effectiveCapabilityPoints").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("mobilityId").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("motorPoolId").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("assignedMotorPoints").ValueKind);
        }
        var motorMgWorld = CampaignSourcePlayableWorld.Create(motorMg.SourceAuthority);
        Assert.Contains("giarabub-motor-mg-historical-motorization-priority-unresolved", motorMgWorld.ReadinessBlockers());
        Assert.Contains("giarabub-motor-mg-capability-and-mobility-unresolved", motorMgWorld.ReadinessBlockers());
        var motorMgCounter = Assert.Single(CampaignSourcePolishArrivalContacts.EffectiveCounters(motorMgWorld),
            value => value.RepresentationId == "axis-garrison.giarabub-motor-machine-gun");
        Assert.Equal(CampaignContactObserverKind.IndependentCounter, motorMgCounter.ObserverKind);
        Assert.Equal(motorMgCounter.RepresentationId, motorMgCounter.ObserverFormationId);
        var motorMgAxisKnowledge = new CampaignContactKnowledge(motorMg.SourceAuthority.Request.CampaignId, LandSide.Axis);
        var motorMgCwKnowledge = new CampaignContactKnowledge(motorMg.SourceAuthority.Request.CampaignId, LandSide.Commonwealth);
        Assert.Empty(CampaignIndependentContactObservationV2.Project(motorMgWorld, motorMgAxisKnowledge, motorMgCounter.RepresentationId).Contacts);
        Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(motorMgWorld, motorMgCwKnowledge, motorMgCounter.RepresentationId));
        Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(remainderContactWorld, motorMgAxisKnowledge, motorMgCounter.RepresentationId));
        var motorAndIndian = CampaignSourcePolishArrivalContacts.EffectiveCounters(motorMgWorld)
            .Where(value => value.RepresentationId is "axis-garrison.giarabub-motor-machine-gun" or "4-indian-division").ToArray();
        Assert.Equal(2, motorAndIndian.Length);
        Assert.Empty(CampaignSourceInitialContacts.DeriveCounters(motorAndIndian, 1, motorMgAxisKnowledge));
        // Exercise the verified Giarabub hexside using the real counters; this
        // hypothetical position is not an admitted campaign move.
        var adjacentIndian = motorAndIndian.Single(value => value.RepresentationId == "4-indian-division") with { LocationId = "c1015" };
        var motorContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters([motorMgCounter, adjacentIndian], 1, motorMgAxisKnowledge));
        Assert.Equal(CampaignContactObserverKind.IndependentCounter, motorContact.ObserverKind);
        Assert.Equal(motorMgCounter.RepresentationId, motorContact.ObserverFormationId);
        Assert.False(motorMgWorld.IsGameplayReady);
        Assert.False(CampaignExercises.StartSourceTurn(motorMg).IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(motorMg).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceGiarabubMotorMachineGun(motorMg).IsAccepted);
        var motorMgCheckpoint = motorMg.SourceAuthority.SerializeCheckpoint();
        Assert.True(motorMgCheckpoint.Length < 16_384, $"Compact motor MG checkpoint was {motorMgCheckpoint.Length} bytes.");
        using (var document = JsonDocument.Parse(motorMgCheckpoint))
            Assert.Equal(motorMgContent.Identity.Hash, document.RootElement.GetProperty("motorMachineGunContentHash").GetString());
        var cwFrontlineResult = CampaignExercises.ActivateSourceCwFrontline(motorMg);
        Assert.True(cwFrontlineResult.IsAccepted);
        var cwFrontline = cwFrontlineResult.SuccessorSession!;
        var cwAuthority = cwFrontline.SourceAuthority!;
        if (seed == 14)
        {
            var loads = cwFrontline;
            Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAttachedInitialLoadsDraft(loads, CampaignActionAudience.Axis));
            using (var opening = JsonDocument.Parse(CampaignExercises.QuerySourceAttachedInitialLoadsDraft(loads, CampaignActionAudience.Commonwealth).VisibleBytes))
            {
                Assert.Equal(2, opening.RootElement.GetProperty("contractVersion").GetInt32());
                Assert.Contains(opening.RootElement.GetProperty("frontlinePools").EnumerateArray(), value =>
                    value.GetProperty("elementId").GetString() == "1-rtr" && value.GetProperty("totalPoints").GetInt32() == 5);
            }
            Assert.False(CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-finish", "frontline"),
                    CampaignActionAudience.Commonwealth)).IsAccepted);
            foreach (var (id, count) in new[] { ("1-rtr", 5), ("11-hussars", 3) })
                for (var i = 0; i < count; i++)
                    loads = CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                        Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-attach", id),
                            CampaignActionAudience.Commonwealth)).SuccessorSession!;
            var attachOverflow = CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-attach", "1-rtr"),
                    CampaignActionAudience.Commonwealth));
            Assert.False(attachOverflow.IsAccepted);
            var emptyFinished = CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-finish", "frontline"),
                    CampaignActionAudience.Commonwealth)).SuccessorSession!;
            Assert.DoesNotContain("cw-frontline-initial-loads-unresolved",
                CampaignSourcePlayableWorld.Create(emptyFinished.SourceAuthority!).ReadinessBlockers());
            Assert.False(CampaignExercises.StartSourceTurn(emptyFinished).IsAccepted);
            Assert.True(CampaignExercises.Reconstruct(emptyFinished).IsVerified);
            var reopenedEmpty = CampaignExercises.SubmitSourceAttachedInitialLoads(emptyFinished,
                Submission(emptyFinished, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-reopen", "frontline"),
                    CampaignActionAudience.Commonwealth)).SuccessorSession!;
            Assert.Contains("cw-frontline-initial-loads-unresolved",
                CampaignSourcePlayableWorld.Create(reopenedEmpty.SourceAuthority!).ReadinessBlockers());
            foreach (var (kind, id) in new[] { ("frontline-ammo", "1-rtr"), ("frontline-fuel", "11-hussars"), ("frontline-finish", "frontline") })
                loads = CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                    Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId(kind, id),
                        CampaignActionAudience.Commonwealth)).SuccessorSession!;
            var loadState = loads.SourceAuthority!.AttachedInitialLoads!;
            Assert.True(loadState.FrontlineFinished);
            Assert.Equal(5, loadState.Attached("1-rtr"));
            Assert.Equal(3, loadState.Attached("11-hussars"));
            Assert.Equal(1, Assert.Single(loadState.Targets, value => value.ElementId == "1-rtr").AmmoPoints);
            Assert.Equal(1, Assert.Single(loadState.Targets, value => value.ElementId == "11-hussars").FuelPoints);
            Assert.Contains("cw-frontline-initial-loads-unresolved", CampaignSourcePlayableWorld.Create(loads.SourceAuthority).ReadinessBlockers());
            Assert.False(CampaignExercises.SubmitSourceAttachedInitialLoads(loads,
                Submission(loads, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-ammo", "1-rtr"),
                    CampaignActionAudience.Commonwealth)).IsAccepted);
            Assert.True(CampaignExercises.Reconstruct(loads).IsVerified);
        }
        Assert.True(CampaignExercises.IsSourceCwFrontlineActivated(cwFrontline));
        Assert.Equal(motorMgCheckpoint, motorMg.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.CwFrontlineCheckpointContractVersion, cwAuthority.ProfileVersion);
        Assert.Equal(motorMg.SourceAuthority.CommonwealthMotorization.Hash(), cwAuthority.CommonwealthMotorization.Hash());
        Assert.Equal(motorMg.SourceAuthority.AxisMotorization.Hash(), cwAuthority.AxisMotorization.Hash());
        Assert.Equal(motorMg.SourceAuthority.RandomState, cwAuthority.RandomState);
        Assert.Equal(motorMg.SourceAuthority.World.PendingArrivals, cwAuthority.World.PendingArrivals);
        foreach (var unit in ContentSourceCwFrontlineArtifact.Units)
        {
            var element = Assert.Single(cwAuthority.World.World.Elements, value => value.ElementId == unit.Id);
            Assert.Equal(unit.Location, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
            Assert.Single(cwAuthority.World.World.Representations, value => value.RepresentationId == "cw-frontline." + unit.Id
                && value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement);
            Assert.Contains(cwAuthority.Battlefield!.Entries, value => value.ElementId == unit.Id
                && value.AuthorityStatus == "source-cw-frontline-active-initial-loads-unresolved");
        }
        var cwNativeWorld = cwAuthority.CwFreeGunsWorld!;
        var freshHashWorld = remainder.SourceAuthority.CwFreeGunsWorld!.ActivateGiarabubMotorMachineGun(
            remainder.SourceAuthority.AxisGarrisons!, remainder.SourceAuthority.Battlefield!);
        var canonicalWorldHash = CampaignPregameSourceCreation.Digest(freshHashWorld.SerializeCanonical());
        var concurrentWorldHashes = new string[8];
        Parallel.For(0, concurrentWorldHashes.Length, index => concurrentWorldHashes[index] = freshHashWorld.Hash());
        Assert.All(concurrentWorldHashes, hash => Assert.Equal(canonicalWorldHash, hash));
        var cwContent = cwNativeWorld.CwFrontlineContent!;
        var cwContactWorld = CampaignSourcePlayableWorld.Create(cwAuthority);
        var cwEffectiveCounters = CampaignSourcePolishArrivalContacts.EffectiveCounters(cwContactWorld);
        foreach (var (cwId, axisId) in new[]
        {
            ("cw-frontline.1-rtr", "axis-minor.scheferzen-garrison"),
            ("cw-frontline.11-hussars", "axis-minor.fort-maddalena-garrison"),
        })
        {
            var actualPair = cwEffectiveCounters.Where(value => value.RepresentationId == cwId || value.RepresentationId == axisId).ToArray();
            Assert.Equal(2, actualPair.Length);
            Assert.All(actualPair, value => Assert.Equal(CampaignContactObserverKind.IndependentCounter, value.ObserverKind));
            var cwKnowledge = new CampaignContactKnowledge(cwAuthority.Request.CampaignId, LandSide.Commonwealth);
            var axisKnowledge = new CampaignContactKnowledge(cwAuthority.Request.CampaignId, LandSide.Axis);
            var cwContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(actualPair, 1, cwKnowledge));
            var axisContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(actualPair, 1, axisKnowledge));
            Assert.Equal(cwId, cwContact.ObserverFormationId);
            Assert.Equal(axisId, axisContact.ObserverFormationId);
            var serialized = CampaignContactTransitionCodec.SerializeCanonical(cwContact);
            Assert.Equal(cwContact, CampaignContactTransitionCodec.DeserializeCanonical(serialized));
            Assert.Equal(serialized, CampaignContactTransitionCodec.SerializeCanonical(Assert.Single(
                CampaignSourceInitialContacts.DeriveCounters(actualPair, 1,
                    new CampaignContactKnowledge(cwAuthority.Request.CampaignId, LandSide.Commonwealth)))));
            cwKnowledge.Apply(cwContact);
            axisKnowledge.Apply(axisContact);
            var cwView = CampaignIndependentContactObservationV2.Project(cwContactWorld, cwKnowledge, cwId);
            Assert.Single(cwView.Contacts);
            Assert.DoesNotContain(axisId, System.Text.Encoding.UTF8.GetString(cwView.SerializeCanonical()));
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(cwContactWorld, axisKnowledge, cwId));
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(
                CampaignSourcePlayableWorld.Create(motorMg.SourceAuthority), cwKnowledge, cwId));
            Assert.Equal(cwContact.ContactHandle, Assert.Single(CampaignSourceInitialContacts.DeriveCounters(actualPair, 2, cwKnowledge)).ContactHandle);
        }
        Assert.Equal(ContentSourceCwFrontlineArtifact.ContentHash, cwContent.Identity.Hash);
        Assert.Equal(cwContent.Identity, ContentSourceCwFrontlineArtifact.Read(cwContent.CanonicalBytes).Identity);
        if (seed == 14)
        {
            var changedMotor = motorMgContent.CanonicalBytes;
            var motorFormat = changedMotor.AsSpan().IndexOf("sandtable.source-giarabub-motor-mg.v1"u8);
            Assert.True(motorFormat >= 0);
            changedMotor[motorFormat] = (byte)'X';
            Assert.Throws<InvalidOperationException>(() => ContentSourceGiarabubMotorMachineGunArtifact.Read(changedMotor));
            var changedFrontline = cwContent.CanonicalBytes;
            var frontlineFormat = changedFrontline.AsSpan().IndexOf("sandtable.source-cw-frontline.v1"u8);
            Assert.True(frontlineFormat >= 0);
            changedFrontline[frontlineFormat] = (byte)'X';
            Assert.Throws<InvalidOperationException>(() => ContentSourceCwFrontlineArtifact.Read(changedFrontline));
        }
        using (var document = JsonDocument.Parse(cwContent.CanonicalBytes))
            foreach (var unit in document.RootElement.GetProperty("units").EnumerateArray())
            {
                if (unit.GetProperty("elementId").GetString() == "11-hussars")
                    Assert.Equal(1, unit.GetProperty("chartArmorRating").GetInt32());
                Assert.Equal(JsonValueKind.Null, unit.GetProperty("initialCargo").ValueKind);
                Assert.Equal(JsonValueKind.Null, unit.GetProperty("parentFormationId").ValueKind);
            }
        Assert.Contains("cw-frontline-initial-loads-unresolved", CampaignSourcePlayableWorld.Create(cwAuthority).ReadinessBlockers());
        Assert.False(CampaignExercises.StartSourceTurn(cwFrontline).IsAccepted);
        Assert.False(CampaignExercises.ActivateSourceCwFrontline(cwFrontline).IsAccepted);
        var cwCheckpoint = cwAuthority.SerializeCheckpoint();
        Assert.True(cwCheckpoint.Length < 16_384, $"Compact CW frontline checkpoint was {cwCheckpoint.Length} bytes.");
        using (var document = JsonDocument.Parse(cwCheckpoint))
            Assert.Equal(cwContent.Identity.Hash, document.RootElement.GetProperty("cwFrontlineContentHash").GetString());
        Assert.True(CampaignExercises.Reconstruct(cwFrontline).IsVerified);
        var cwHistory = cwFrontline.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var document = JsonDocument.Parse(cwHistory[^1]))
        {
            var hash = document.RootElement.GetProperty("worldHash").GetString()!;
            var changed = hash[..^1] + (hash[^1] == '0' ? '1' : '0');
            cwHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(cwHistory[^1])
                .Replace(hash, changed, StringComparison.Ordinal));
        }
        Assert.False(CampaignExercises.Reconstruct(new ExerciseSession(cwFrontline.Pregame!, cwFrontline.CurrentContacts,
            cwHistory, cwFrontline.SourceAuthority)).IsVerified);
        var motorMgHistory = motorMg.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var document = JsonDocument.Parse(motorMgHistory[^1]))
        {
            var hash = document.RootElement.GetProperty("worldHash").GetString()!;
            var changed = hash[..^1] + (hash[^1] == '0' ? '1' : '0');
            motorMgHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(motorMgHistory[^1])
                .Replace(hash, changed, StringComparison.Ordinal));
        }
        Assert.False(CampaignExercises.Reconstruct(new ExerciseSession(motorMg.Pregame!, motorMg.CurrentContacts,
            motorMgHistory, motorMg.SourceAuthority)).IsVerified);
        var minorHistory = minor.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var eventDocument = JsonDocument.Parse(minorHistory[^1]))
        {
            var worldHash = eventDocument.RootElement.GetProperty("worldHash").GetString()!;
            var changedHash = worldHash[..^1] + (worldHash[^1] == '0' ? '1' : '0');
            minorHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(minorHistory[^1])
                .Replace(worldHash, changedHash, StringComparison.Ordinal));
        }
        var tamperedMinor = new ExerciseSession(minor.Pregame!, minor.CurrentContacts, minorHistory, minor.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(tamperedMinor).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceAxisMinorGarrisons(minor).IsAccepted);
        var minorFinish = CampaignExercises.Query(minor, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var minorFinished = CampaignExercises.Submit(minor,
            Submission(minor, minorFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(minorFinished).IsVerified);
        Assert.Equal(minor.SourceAuthority.World.ContentIdentity, minorFinished.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(ContentSourceFourthIndianPackArtifact.ContentHash, grouped.ContentHash);
        Assert.Equal(11, grouped.Groups.Single().Members.Count);
        var originalGroup = grouped.Groups.Single();
        var forgedMembers = originalGroup.Members.Select(value => value.ElementId == "25-field-artillery"
            ? value with { AdditionalComponents = Array.AsReadOnly(new[] { new CampaignGroupedComponent("extra-gun", 1) }) } : value);
        var forgedGroup = new CampaignGroupedFormation(originalGroup.GroupId, originalGroup.SideId, originalGroup.RootId,
            originalGroup.LocationId, originalGroup.StackValue, originalGroup.MotorPool, forgedMembers, originalGroup.DispositionConfirmed);
        var forgedWorld = CampaignGroupedWorld.Create(grouped.Prior, grouped.ContentHash, [forgedGroup]);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceFourthIndianDefinition.Validate(forgedWorld));
        var fieldArtillery = grouped.Groups.Single().Members.Single(value => value.ElementId == "25-field-artillery");
        Assert.Equal("3-7-inch-howitzer", fieldArtillery.ComponentType);
        Assert.Equal(6, fieldArtillery.ComponentToe);
        Assert.Equal(grouped.Prior.Initialization.CapabilityPointsExpended, fieldArtillery.CapabilityPointsExpended);
        Assert.Equal(11, indian.SourceAuthority.Battlefield!.Entries.Count(value => value.LocationId == "d3615" && value.AuthorityStatus == "source-fourth-indian-proposed"));
        Assert.Null(grouped.StackAt("commonwealth", "d3615", _ => null));
        Assert.Contains(grouped.Prior.PendingArrivals, value => value.GroupId == "graziani-7-indian-brigade");
        var priorityAction = CampaignSourceAuthorityState.FourthIndianMotorActionId("central-india-horse", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Axis)).RejectionReason);
        Assert.False(grouped.Groups.Single().DispositionConfirmed);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        var confirm = CampaignExercises.Query(indian, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "fourth-indian-motorization");
        Assert.Contains("confirm", confirm.ChoiceJson, StringComparison.Ordinal);
        var ownerDraft = System.Text.Encoding.UTF8.GetString(CampaignExercises.QuerySourceFourthIndianDraft(indian, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.Contains("provisional-source-default-alternatives-unavailable", ownerDraft, StringComparison.Ordinal);
        Assert.Contains("5-indian-brigade-hq", ownerDraft, StringComparison.Ordinal);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(indian,
            Submission(indian, confirm.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var confirmCommand = Submission(indian, confirm.ActionId, CampaignActionAudience.Commonwealth);
        indian = CampaignExercises.Submit(indian, confirmCommand).SuccessorSession!;
        Assert.True(indian.SourceAuthority!.GroupedWorld!.Groups.Single().DispositionConfirmed);
        Assert.Equal(5, indian.SourceAuthority.GroupedWorld.StackAt("commonwealth", "d3615", _ => null));
        Assert.Equal(11, indian.SourceAuthority.Battlefield!.Entries.Count(value => value.LocationId == "d3615" && value.AuthorityStatus == "source-fourth-indian-grouped"));
        Assert.Equal(34, indian.SourceAuthority.ProfileVersion);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(indian, confirmCommand).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        indian = CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(21, indian.SourceAuthority!.GroupedWorld!.RemainingMotorPoints("4-indian-division"));
        var footAction = CampaignSourceAuthorityState.FourthIndianMotorActionId("1-royal-fusiliers", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, footAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        var minorBeforePromotions = CampaignExercises.ActivateSourceAxisMinorGarrisons(indian).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.AdmitSourceCompactCheckpoint(minorBeforePromotions).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.ActivateSourceAxisGarrisonRemainder(minorBeforePromotions).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.ActivateSourceGiarabubMotorMachineGun(minorBeforePromotions).SuccessorSession!;
        var minorIdentity = minorBeforePromotions.SourceAuthority!.World.ContentIdentity;
        minorBeforePromotions = CampaignExercises.SubmitSourceMalettiHierarchy(minorBeforePromotions,
            Submission(minorBeforePromotions, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        var minorMaletti = CampaignExercises.PromoteSourceCertifiedMalettiWorld(minorBeforePromotions);
        Assert.True(minorMaletti.IsAccepted);
        minorBeforePromotions = minorMaletti.SuccessorSession!;
        minorBeforePromotions = CampaignExercises.AdmitSourceMatruhFormation(minorBeforePromotions).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.SubmitSourceMatruhFormation(minorBeforePromotions,
            Submission(minorBeforePromotions, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.SubmitSourceMatruhFormation(minorBeforePromotions,
            Submission(minorBeforePromotions, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var minorMatruh = CampaignExercises.PromoteSourceMatruhDetachedWorld(minorBeforePromotions);
        Assert.True(minorMatruh.IsAccepted);
        minorBeforePromotions = minorMatruh.SuccessorSession!;
        Assert.Equal(minorIdentity, minorBeforePromotions.SourceAuthority!.World.ContentIdentity);
        Assert.All(ContentSourceAxisMinorGarrisonsPackArtifact.Units, unit =>
            Assert.Contains(minorBeforePromotions.SourceAuthority.World.World.Elements, element => element.ElementId == unit.Id));
        Assert.True(CampaignExercises.Reconstruct(minorBeforePromotions).IsVerified);
        var indianWithoutMalettiDetachment = indian;
        indian = CampaignExercises.SubmitSourceMalettiHierarchy(indian,
            Submission(indian, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        indian = CampaignExercises.PromoteSourceCertifiedMalettiWorld(indian).SuccessorSession!;
        Assert.Equal(21, indian.SourceAuthority!.GroupedWorld!.RemainingMotorPoints("4-indian-division"));
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        var matruhBeforeDetach = CampaignExercises.AdmitSourceMatruhFormation(indian).SuccessorSession!;
        matruhBeforeDetach = CampaignExercises.SubmitSourceMatruhFormation(matruhBeforeDetach,
            Submission(matruhBeforeDetach, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var matruhAfterDetach = matruhBeforeDetach;
        var groupedResult = CampaignExercises.AdmitSourceMatruhGroupedWorld(matruhBeforeDetach);
        Assert.True(groupedResult.IsAccepted);
        matruhBeforeDetach = groupedResult.SuccessorSession!;
        Assert.Equal(2, matruhBeforeDetach.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(0, matruhBeforeDetach.SourceAuthority.GroupedWorld.StackAt("commonwealth", "d3714", _ => null));
        Assert.Equal(6, matruhBeforeDetach.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "matruh-garrison").Members.Count);
        Assert.Equal(35, matruhBeforeDetach.SourceAuthority.ProfileVersion);
        var activeMaletti = CampaignExercises.SubmitSourceAxisPriority(matruhBeforeDetach,
            Submission(matruhBeforeDetach, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        activeMaletti = CampaignExercises.AdmitSourceMalettiGroupedWorld(activeMaletti).SuccessorSession!;
        Assert.Equal(3, activeMaletti.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(30, activeMaletti.SourceAuthority.GroupedWorld.RemainingMotorPoints("maletti-division"));
        Assert.Equal(5, activeMaletti.SourceAuthority.GroupedWorld.ExternalReservations.Single(value => value.GroupId == "maletti-division").AssignedMotorPoints);
        Assert.Equal(36, activeMaletti.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(activeMaletti).IsVerified);
        var libyanAdmission = CampaignExercises.AdmitSourceLibyanGroupedWorld(activeMaletti);
        Assert.True(libyanAdmission.IsAccepted);
        var libyanProposed = libyanAdmission.SuccessorSession!;
        Assert.Equal(5, libyanProposed.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(22, libyanProposed.SourceAuthority.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-libyan-proposed"));
        Assert.Equal(37, libyanProposed.SourceAuthority.ProfileVersion);
        var secondArtillery = libyanProposed.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "2-libyan-division")
            .Members.Single(value => value.ElementId == "2-libyan-artillery");
        Assert.Equal("75-27-gun", secondArtillery.ComponentType);
        Assert.Equal(9, secondArtillery.ComponentToe);
        Assert.Contains(secondArtillery.AdditionalComponents!, value => value.ComponentType == "100-17-howitzer" && value.Toe == 3);
        var axisChoices = CampaignExercises.Query(libyanProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "libyan-disposition").ToArray();
        Assert.Equal(2, axisChoices.Length);
        Assert.DoesNotContain(CampaignExercises.Query(libyanProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "libyan-disposition");
        var firstChoice = axisChoices.Single(value => value.ChoiceJson.Contains("1-libyan-division", StringComparison.Ordinal));
        var secondChoice = axisChoices.Single(value => value.ChoiceJson.Contains("2-libyan-division", StringComparison.Ordinal));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(libyanProposed,
            Submission(libyanProposed, firstChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var firstCommand = Submission(libyanProposed, firstChoice.ActionId, CampaignActionAudience.Axis);
        var firstConfirmed = CampaignExercises.Submit(libyanProposed, firstCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(firstConfirmed, firstCommand).RejectionReason);
        var secondAfter = CampaignExercises.Query(firstConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "libyan-disposition");
        firstConfirmed = CampaignExercises.Submit(firstConfirmed, Submission(firstConfirmed, secondAfter.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(22, firstConfirmed.SourceAuthority!.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-libyan-grouped"));
        Assert.Equal(5, firstConfirmed.SourceAuthority.GroupedWorld!.StackAt("axis", "c4020", _ => null));
        Assert.Equal(5, firstConfirmed.SourceAuthority.GroupedWorld.StackAt("axis", "c3920", _ => null));
        Assert.True(CampaignExercises.Reconstruct(firstConfirmed).IsVerified);
        var italianBasisFourth = ContentSourceFourthIndianPackArtifact.Create(firstConfirmed.SourceAuthority.CwFreeGunsWorld!.Content);
        var italianBasisMatruh = ContentSourceMatruhGroupedPackArtifact.Create(italianBasisFourth);
        var italianBasisMaletti = ContentSourceMalettiGroupedPackArtifact.Create(italianBasisMatruh);
        var italianBasisLibyan = ContentSourceLibyanGroupedPackArtifact.Create(italianBasisMaletti);
        var italianContent = ContentSourceCireneMarmaricaGroupedPackArtifact.Create(italianBasisLibyan);
        Assert.Equal(ContentSourceCireneMarmaricaGroupedPackArtifact.ContentHash, italianContent.Hash());
        var italianAdmission = CampaignExercises.AdmitSourceItalianDivisionsGroupedWorld(firstConfirmed);
        Assert.True(italianAdmission.IsAccepted);
        var italianProposed = italianAdmission.SuccessorSession!;
        Assert.Equal(7, italianProposed.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(29, italianProposed.SourceAuthority.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-italian-division-proposed"));
        Assert.Equal(39, italianProposed.SourceAuthority.ProfileVersion);
        var italianChoices = CampaignExercises.Query(italianProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "italian-division-disposition").ToArray();
        Assert.Equal(2, italianChoices.Length);
        Assert.DoesNotContain(CampaignExercises.Query(italianProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "italian-division-disposition");
        var cireneChoice = italianChoices.Single(value => value.ChoiceJson.Contains("cirene-division", StringComparison.Ordinal));
        var italianCommand = Submission(italianProposed, cireneChoice.ActionId, CampaignActionAudience.Axis);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(italianProposed,
            Submission(italianProposed, cireneChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var italianConfirmed = CampaignExercises.Submit(italianProposed, italianCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(italianConfirmed, italianCommand).RejectionReason);
        var marmaricaChoice = CampaignExercises.Query(italianConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "italian-division-disposition");
        italianConfirmed = CampaignExercises.Submit(italianConfirmed, Submission(italianConfirmed, marmaricaChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(29, italianConfirmed.SourceAuthority!.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-italian-division-grouped"));
        Assert.Equal(5, italianConfirmed.SourceAuthority.GroupedWorld!.StackAt("axis", "c4120", _ => null));
        Assert.Equal(5, italianConfirmed.SourceAuthority.GroupedWorld.StackAt("axis", "c3918", _ => null));
        Assert.True(CampaignExercises.Reconstruct(italianConfirmed).IsVerified);
        var seventhDirect = italianConfirmed.SourceAuthority.AdmitSeventhArmoredGroupedWorld();
        Assert.NotNull(seventhDirect.GroupedWorld);
        var seventhResult = CampaignExercises.AdmitSourceSeventhArmoredGroupedWorld(italianConfirmed);
        Assert.True(seventhResult.IsAccepted);
        var seventhProposed = seventhResult.SuccessorSession!;
        var seventh = seventhProposed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division");
        Assert.Equal(7, seventh.Members.Count); Assert.Null(seventh.StackValue); Assert.False(seventh.DispositionConfirmed);
        Assert.Equal(10, seventh.MotorPool);
        Assert.Equal(40, seventhProposed.SourceAuthority!.ProfileVersion);
        var seventhChoice = CampaignExercises.Query(seventhProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "seventh-armored-disposition");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(seventhProposed,
            Submission(seventhProposed, seventhChoice.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var seventhCommand = Submission(seventhProposed, seventhChoice.ActionId, CampaignActionAudience.Commonwealth);
        var seventhConfirmedResult = CampaignExercises.Submit(seventhProposed, seventhCommand);
        Assert.True(seventhConfirmedResult.IsAccepted);
        var seventhConfirmed = seventhConfirmedResult.SuccessorSession!;
        Assert.Equal(5, seventhConfirmed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division").StackValue);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(seventhConfirmed, seventhCommand).RejectionReason);
        Assert.Equal(italianConfirmed.SourceAuthority.World.PendingArrivals, seventhConfirmed.SourceAuthority.World.PendingArrivals);
        Assert.True(CampaignExercises.Reconstruct(seventhConfirmed).IsVerified);
        var legacySeventhCheckpoint = seventhConfirmed.SourceAuthority.SerializeCheckpoint();
        var correctedSeventh = CampaignExercises.CorrectSourceSeventhShell(seventhConfirmed).SuccessorSession!;
        Assert.Equal(3, correctedSeventh.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division").StackValue);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedLegacyCheckpointContractVersion, correctedSeventh.SourceAuthority.ProfileVersion);
        Assert.Equal(legacySeventhCheckpoint, seventhConfirmed.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(seventhConfirmed.SourceAuthority.RandomState, correctedSeventh.SourceAuthority.RandomState);
        Assert.Contains("common-charts-9.4-division-shell-stacks-three", System.Text.Encoding.UTF8.GetString(
            CampaignExercises.QuerySourceSeventhArmoredDraft(correctedSeventh, CampaignActionAudience.Commonwealth).VisibleBytes));
        Assert.True(CampaignExercises.Reconstruct(correctedSeventh).IsVerified);
        Assert.False(CampaignExercises.CorrectSourceSeventhShell(correctedSeventh).IsAccepted);
        var compactBeforeShell = CampaignExercises.AdmitSourceCompactCheckpoint(seventhConfirmed).SuccessorSession!;
        var compactAxisFinish = CampaignExercises.Query(compactBeforeShell, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var compactAxisFinished = CampaignExercises.Submit(compactBeforeShell,
            Submission(compactBeforeShell, compactAxisFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var compactCwFinish = CampaignExercises.Query(compactAxisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var compactBothFinished = CampaignExercises.Submit(compactAxisFinished,
            Submission(compactAxisFinished, compactCwFinish.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var compactCorrected = CampaignExercises.CorrectSourceSeventhShell(compactBothFinished).SuccessorSession!;
        Assert.Equal(1, compactCorrected.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedFinalizedCompactCheckpointContractVersion, compactCorrected.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(compactCorrected).IsVerified);
        var compactAxisReopen = CampaignExercises.Query(compactCorrected, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var compactReopened = CampaignExercises.Submit(compactCorrected,
            Submission(compactCorrected, compactAxisReopen.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(0, compactReopened.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedFinalizedCompactCheckpointContractVersion, compactReopened.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(compactReopened).IsVerified);
        var sixteenthBasis = ContentSourceSeventhArmoredGroupedPackArtifact.Create(italianContent);
        var sixteenthContent = ContentSourceSixteenthBrigadeGroupedPackArtifact.Create(sixteenthBasis);
        Assert.Equal(ContentSourceSixteenthBrigadeGroupedPackArtifact.ContentHash, sixteenthContent.Hash());
        var sixteenthResult = CampaignExercises.AdmitSourceSixteenthBrigadeGroupedWorld(seventhConfirmed);
        Assert.True(sixteenthResult.IsAccepted);
        var sixteenthProposed = sixteenthResult.SuccessorSession!;
        var sourceWorld = sixteenthProposed.SourceAuthority!.GroupedWorld!;
        var sixteenth = sourceWorld.Groups.Single(value => value.GroupId == "16-infantry-brigade");
        Assert.Equal(4, sixteenth.Members.Count); Assert.Null(sixteenth.StackValue); Assert.True(sixteenth.UsesRetainedMembers);
        Assert.Equal(4, sourceWorld.Prior.World.Elements.Count(value => sixteenth.Members.Any(member => member.ElementId == value.ElementId)));
        Assert.Single(sourceWorld.OccupantsAt("e1829"), value => value.Formation?.GroupId == sixteenth.GroupId);
        var sixteenthChoice = CampaignExercises.Query(sixteenthProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "sixteenth-brigade-disposition");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(sixteenthProposed,
            Submission(sixteenthProposed, sixteenthChoice.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var sixteenthCommand = Submission(sixteenthProposed, sixteenthChoice.ActionId, CampaignActionAudience.Commonwealth);
        var sixteenthConfirmed = CampaignExercises.Submit(sixteenthProposed, sixteenthCommand).SuccessorSession!;
        Assert.Equal(2, sixteenthConfirmed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == sixteenth.GroupId).StackValue);
        Assert.Equal(41, sixteenthConfirmed.SourceAuthority.ProfileVersion);
        var playableWorld = CampaignSourcePlayableWorld.Create(sixteenthConfirmed.SourceAuthority);
        var mapBoundWorld = CampaignSourcePlayableWorld.Create(sixteenthConfirmed.SourceAuthority, mapIdentityBound: true);
        var cdWorld = CampaignSourcePlayableWorld.Create(sixteenthConfirmed.SourceAuthority, mapIdentityBound: true, cdSeamBound: true);
        Assert.True(mapBoundWorld.MapIdentityBound);
        Assert.True(mapBoundWorld.WithWorld(mapBoundWorld.GroupedWorld, mapBoundWorld.Battlefield).MapIdentityBound);
        Assert.False(playableWorld.MapIdentityBound);
        // Test the wire contract on the retained historical world without
        // claiming that the still-gated campaign can start yet.
        var turnConstructor = typeof(CampaignSourceTurnAuthority).GetConstructors(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Single();
        CampaignSourceTurnAuthority ContractTurn(CampaignSourcePlayableWorld world, LandSequencePosition? testPosition = null,
            int? organizationMask = null, CampaignSourceTrainingLedger? training = null,
            CampaignSourceGt1AxisArrival? axisArrival = null, LandSide? firstSide = null,
            (int GameTurn, int OperationStage)? paidStage = null)
        {
            var args = turnConstructor.GetParameters().Select(parameter => parameter.Name == "position" && testPosition is not null
                ? testPosition : parameter.Name == "voluntaryOrganizationDeclinedMask" && organizationMask is not null
                ? organizationMask : parameter.Name == "training" && training is not null ? training
                : parameter.Name == "axisArrival" && axisArrival is not null ? axisArrival
                : parameter.Name == "fourthIndianPaidStage" && paidStage is not null ? paidStage
                : parameter.Name == "stageOneFirstSide" && firstSide is not null ? firstSide : parameter.HasDefaultValue
                ? parameter.DefaultValue : parameter.Name switch
                {
                    "world" => world,
                    "stateVersion" => world.Source.StateVersion + 1,
                    "position" => Cna1979LandSequenceV4.CreateTurn(1)[0],
                    "randomState" => world.RandomState,
                    "initiative" => null,
                    _ => throw new InvalidOperationException("Unexpected source-turn constructor parameter."),
                }).ToArray();
            return (CampaignSourceTurnAuthority)turnConstructor.Invoke(args);
        }
        var mapTurn = ContractTurn(mapBoundWorld);
        var cdTurn = ContractTurn(cdWorld);
        Assert.Equal(3, new ExerciseCheckpoint(cdTurn).ContractVersion);
        Assert.True(cdWorld.WithWorld(cdWorld.GroupedWorld, cdWorld.Battlefield).CdSeamBound);
        using (var cdCheckpoint = JsonDocument.Parse(cdTurn.SerializeCheckpoint()))
        {
            Assert.Equal(3, cdCheckpoint.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(Cna1979HistoricalMap.CdSeamPolicyHash,
                cdCheckpoint.RootElement.GetProperty("historicalMap").GetProperty("cdSeamPolicyHash").GetString());
        }
        var legacyTurn = ContractTurn(playableWorld);
        var turnContacts = sixteenthConfirmed.CurrentContacts.AdvancePregameSource(mapTurn.Source.StateVersion);
        var turnSession = new ExerciseSession(mapTurn.Source.Pregame, turnContacts,
            sixteenthConfirmed.PregameHistory, mapTurn.Source, mapTurn);
        var initiativeResult = CampaignExercises.ResolveSourceInitiative(turnSession);
        Assert.True(initiativeResult.IsAccepted);
        Assert.Equal(initiativeResult.SuccessorSession!.SourceTurn!.Position.PositionId,
            initiativeResult.Evidence!.Receipt.ResultingPositionId);
        Assert.Equal(CampaignSourceTurnAuthority.InitiativeActionId, initiativeResult.Evidence.Receipt.ActionId);
        var organizationCut = Cna1979LandSequenceV4.CreateTurn(1).Single(value =>
            value.OperationStage == 1 && value.PhaseId == LandPhaseIds.Organization);
        var interrupted = CampaignSourceTrainingLedger.CreateSixthAustralian().Interrupt();
        var waitingOrganization = ContractTurn(mapBoundWorld, organizationCut, 1, interrupted);
        Assert.Throws<InvalidOperationException>(() => waitingOrganization.CompleteGt1Organization());
        Assert.Throws<InvalidOperationException>(() => ContractTurn(mapBoundWorld, organizationCut, 3).CompleteGt1Organization());
        var readyOrganization = ContractTurn(mapBoundWorld, organizationCut, 3, interrupted);
        var completedOrganization = readyOrganization.CompleteGt1Organization();
        Assert.Equal(Cna1979LandSequenceV4.GetNext(organizationCut), completedOrganization.Position);
        Assert.Same(readyOrganization.Training, completedOrganization.Training);
        Assert.Equal(readyOrganization.RandomState, completedOrganization.RandomState);
        Assert.Same(readyOrganization.World, completedOrganization.World);
        Assert.Equal(readyOrganization.StateVersion + 1, completedOrganization.StateVersion);
        Assert.Throws<InvalidOperationException>(() => completedOrganization.CompleteGt1Organization());
        // Exercise the real source session transition on retained historical
        // setup without claiming that the campaign-start gate has opened.
        var organizationContacts = sixteenthConfirmed.CurrentContacts.AdvancePregameSource(readyOrganization.Source.StateVersion);
        var organizationSession = new ExerciseSession(readyOrganization.Source.Pregame, organizationContacts,
            sixteenthConfirmed.PregameHistory, readyOrganization.Source, readyOrganization);
        var organizationResult = CampaignExercises.CompleteSourceGt1Organization(organizationSession);
        Assert.True(organizationResult.IsAccepted);
        Assert.Equal(completedOrganization.Position.PositionId, organizationResult.Evidence!.Receipt.ResultingPositionId);
        Assert.True(CampaignExercises.IsSourceGt1OrganizationCompleted(organizationResult.SuccessorSession!));
        Assert.Equal(completedOrganization.Position, organizationResult.SuccessorSession!.SourceTurn!.Position);
        var organizationCommand = CampaignHostAdapter.SourceTurnSystemCommand(organizationSession, organizationResult);
        var organizationStep = new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(organizationCommand), organizationResult.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(organizationResult.Evidence.Receipt),
            organizationResult.Evidence.SnapshotCheckpoint, CampaignHostAcceptedStep.SourceGt1OrganizationCompleteKind);
        Assert.Equal(CampaignHostAcceptedStep.SourceGt1OrganizationCompleteContractVersion, organizationStep.ContractVersion);
        using (var organizationEvent = JsonDocument.Parse(Assert.Single(organizationResult.Evidence!.EventRecords)))
        {
            Assert.Equal("source-gt1-organization-completed", organizationEvent.RootElement.GetProperty("eventType").GetString());
            Assert.Equal(CampaignSourceTurnAuthority.CompleteGt1OrganizationActionId, organizationEvent.RootElement.GetProperty("actionId").GetString());
        }
        Assert.Equal(2, new ExerciseCheckpoint(mapTurn).ContractVersion);
        Assert.Equal(1, new ExerciseCheckpoint(legacyTurn).ContractVersion);
        using (var boundCheckpoint = System.Text.Json.JsonDocument.Parse(mapTurn.SerializeCheckpoint()))
        using (var legacyCheckpoint = System.Text.Json.JsonDocument.Parse(legacyTurn.SerializeCheckpoint()))
        {
            Assert.Equal(2, boundCheckpoint.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(Cna1979HistoricalMap.HexHash, boundCheckpoint.RootElement.GetProperty("historicalMap").GetProperty("hexSha256").GetString());
            Assert.Equal(Cna1979HistoricalMap.EdgeHash, boundCheckpoint.RootElement.GetProperty("historicalMap").GetProperty("hexsideSha256").GetString());
            Assert.Equal(1, legacyCheckpoint.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.False(legacyCheckpoint.RootElement.TryGetProperty("historicalMap", out _));
        }
        using (var boundEvent = System.Text.Json.JsonDocument.Parse(mapTurn.SerializeEvent("source-turn-started",
                   CampaignSourceTurnAuthority.StartActionId, mapBoundWorld.Source.StateVersion,
                   mapBoundWorld.Source.SerializeCheckpoint())))
        {
            Assert.Equal(2, boundEvent.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(CampaignPregameSourceCreation.Digest(mapTurn.SerializeCheckpoint()),
                boundEvent.RootElement.GetProperty("checkpointHash").GetString());
        }
        using (var legacyEvent = System.Text.Json.JsonDocument.Parse(legacyTurn.SerializeEvent("source-turn-started",
                   CampaignSourceTurnAuthority.StartActionId, playableWorld.Source.StateVersion,
                   playableWorld.Source.SerializeCheckpoint())))
            Assert.Equal(1, legacyEvent.RootElement.GetProperty("contractVersion").GetInt32());
        Assert.True(mapTurn.ResolveInitiative().World.MapIdentityBound);
        Assert.Same(sixteenthConfirmed.SourceAuthority.GroupedWorld, playableWorld.GroupedWorld);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.RandomState, playableWorld.RandomState);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.World.PendingArrivals, playableWorld.World8.PendingArrivals);
        foreach (var entry in playableWorld.Battlefield.Entries.Where(value => value.LocationId is { Length: 5 }
                     && value.LocationId[0] is >= 'a' and <= 'e' && value.LocationId.Skip(1).All(char.IsAsciiDigit)))
            Assert.NotNull(Cna1979HistoricalMap.Instance.HexAt(entry.LocationId!));
        foreach (var location in Cna1979GrazianiSourceFacts.CairoEntryArea.LocationIds)
            Assert.Equal(Cna1979SourceContentVocabulary.MajorCityTerrainId, playableWorld.KnownTerrainAt(location));
        var polishArrivalPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        Assert.Contains("e1829", CampaignSourcePolishArrival.ValidDestinations(playableWorld, polishArrivalPosition));
        Assert.Null(playableWorld.KnownTerrainAt("e1932"));
        Assert.Single(playableWorld.OccupantsAt("e1829"), value => value.Formation?.GroupId == sixteenth.GroupId);
        Assert.DoesNotContain(playableWorld.UnresolvedAt("e1829"), value => sixteenth.Members.Any(member => member.ElementId == value.ElementId));
        Assert.False(playableWorld.IsGameplayReady);
        Assert.Contains("battlefield-incomplete", playableWorld.ReadinessBlockers());
        Assert.Contains("control-incomplete", playableWorld.ReadinessBlockers());
        Assert.Contains("map-incomplete", playableWorld.ReadinessBlockers());
        Assert.Same(playableWorld.GroupedWorld, playableWorld.WithWorld(playableWorld.GroupedWorld, playableWorld.Battlefield).GroupedWorld);
        var sourceSupply = playableWorld.Source.CommonwealthSupplyWorld;
        CampaignSourceFourthIndianRoad.RequirePinnedSource(playableWorld);
        Assert.Equal(new CapabilityPointAmount(1, 1), CampaignSourceFourthIndianRoad.Cost(false));
        Assert.Equal(new CapabilityPointAmount(1, 2), CampaignSourceFourthIndianRoad.Cost(true));
        Assert.Equal(new CapabilityPointAmount(1, 1), CampaignSourceFourthIndianRoad.SupplyTraceCost);
        var paidSupply = sourceSupply.SpendFuel("cw-matruh-1", "d3714", 2);
        Assert.Equal(60, Assert.Single(sourceSupply.Units, value => value.Id == "cw-matruh-1").Fuel);
        Assert.Equal(58, Assert.Single(paidSupply.Units, value => value.Id == "cw-matruh-1").Fuel);
        Assert.Throws<InvalidOperationException>(() => sourceSupply.SpendFuel("cw-matruh-1", "d3615", 2));
        Assert.Throws<InvalidOperationException>(() => sourceSupply.SpendFuel("cw-matruh-1", "d3714", 61));
        Assert.Throws<InvalidOperationException>(() => sourceSupply.SpendFuel("not-a-counter", "d3714", 2));
        var movementCatalog = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.ActorRole == LandActorRole.FirstActingSide && value.SegmentId == LandSegmentIds.Movement);
        var movementPosition = new LandSequencePosition(movementCatalog.ContractVersion, movementCatalog.PositionId,
            movementCatalog.GameTurn, movementCatalog.OperationStage, movementCatalog.StageId, movementCatalog.PhaseId,
            movementCatalog.SegmentId, movementCatalog.StepId, movementCatalog.ActorRole, LandSide.Commonwealth, movementCatalog.Sources);
        var season = Cna1979Weather.GetSeason(1);
        var normalRoll = Enumerable.Range(11, 56).First(value => value % 10 is >= 1 and <= 6
            && value / 10 is >= 1 and <= 6 && Cna1979Weather.GetKind(season, value) == WeatherKind.Normal);
        var normalWeather = new CampaignOperationStageWeather(1, 1, 1, LandSide.Axis, season,
            normalRoll / 10, normalRoll % 10, WeatherKind.Normal, WeatherScope.None, null, [], 0, 0, 0);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceFourthIndianMovement.Apply(playableWorld,
            sourceSupply, null, movementPosition, normalWeather, "cw-matruh-1"));
        Assert.Equal(60, Assert.Single(sourceSupply.Units, value => value.Id == "cw-matruh-1").Fuel);
        // Exercise the source-bound mutation and contact projection directly;
        // source readiness still prevents this from becoming a playable move.
        var fourth = Assert.Single(playableWorld.GroupedWorld.Groups,
            value => value.GroupId == CampaignSourceFourthIndianMovement.GroupId);
        var fourthCosts = fourth.Members.ToDictionary(value => value.ElementId,
            value => new CapabilityPointAmount(1, 1), StringComparer.Ordinal);
        var movedFourth = playableWorld.GroupedWorld.MoveFormation(fourth.GroupId, "d3715", fourthCosts, 5, 1, 1);
        var movedBattlefield = playableWorld.Battlefield.MoveFormation(playableWorld.GroupedWorld, movedFourth, fourth.GroupId);
        var movedWorld = playableWorld.WithWorld(movedFourth, movedBattlefield);
        Assert.True(mapBoundWorld.WithWorld(movedFourth, movedBattlefield).MapIdentityBound);
        var unknownMove = playableWorld.GroupedWorld.MoveFormation(fourth.GroupId, "c9999", fourthCosts, 5, 1, 1);
        var unknownBattlefield = playableWorld.Battlefield.MoveFormation(playableWorld.GroupedWorld, unknownMove, fourth.GroupId);
        Assert.Contains(unknownBattlefield.Entries, entry => entry.LocationId == "c9999");
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePlayableWorld.RequireKnownMapLocations(
            unknownBattlefield.Entries.Select(entry => entry.LocationId)));
        Assert.Throws<InvalidOperationException>(() => mapBoundWorld.WithWorld(unknownMove, unknownBattlefield));
        Assert.All(fourth.Members, member => Assert.Equal("d3715",
            Assert.Single(movedWorld.Battlefield.Entries, entry => entry.ElementId == member.ElementId).LocationId));
        Assert.All(fourth.Members, member => Assert.Equal("d3615",
            Assert.Single(playableWorld.Battlefield.Entries, entry => entry.ElementId == member.ElementId).LocationId));
        var polishPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePolishArrival.Apply(playableWorld,
            polishPosition, LandSide.Commonwealth, "e1730"));
        Assert.Equal(sixteenthConfirmed.SourceAuthority.World.PendingArrivals, playableWorld.World8.PendingArrivals);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.RandomState, playableWorld.RandomState);
        var polishGrouped = CampaignSourcePolishArrival.Materialize(playableWorld.GroupedWorld, playableWorld.Source.Content, "e1829");
        var polishBattlefield = playableWorld.Battlefield.ArriveRetainedFormation(playableWorld.GroupedWorld,
            polishGrouped, CampaignSourcePolishArrival.GroupId, "e1829");
        var polishSuccessor = playableWorld.WithWorld(polishGrouped, polishBattlefield);
        var mapBoundPolishSuccessor = mapBoundWorld.WithWorld(polishGrouped, polishBattlefield);
        Assert.True(mapBoundPolishSuccessor.MapIdentityBound);
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(mapBoundWorld, mapBoundPolishSuccessor,
            mapBoundWorld.Source.StateVersion + 1, new CampaignContactKnowledge(mapBoundWorld.Source.Request.CampaignId, LandSide.Axis)));
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(mapBoundWorld, mapBoundPolishSuccessor,
            mapBoundWorld.Source.StateVersion + 1, new CampaignContactKnowledge(mapBoundWorld.Source.Request.CampaignId, LandSide.Commonwealth)));
        Assert.DoesNotContain(polishSuccessor.World8.PendingArrivals, value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.DoesNotContain(polishSuccessor.Battlefield.Pending, value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.Contains(polishSuccessor.World8.PendingArrivals, value => value.GroupId == Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId);
        Assert.Equal(8, polishSuccessor.World8.World.Elements.Count(value => Cna1979GrazianiSourceFacts.PolishBrigadeArrival.ElementIds.Contains(value.ElementId)));
        Assert.Equal(playableWorld.RandomState, polishSuccessor.RandomState);
        Assert.False(polishSuccessor.IsGameplayReady);
        Assert.Contains(playableWorld.World8.PendingArrivals, value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(playableWorld, polishSuccessor,
            playableWorld.Source.StateVersion + 1, new CampaignContactKnowledge(playableWorld.Source.Request.CampaignId, LandSide.Axis)));
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(playableWorld, polishSuccessor,
            playableWorld.Source.StateVersion + 1, new CampaignContactKnowledge(playableWorld.Source.Request.CampaignId, LandSide.Commonwealth)));
        var begunEarlyPlan = CampaignExercises.SubmitSourceSeptemberSupply(sixteenthConfirmed,
            Submission(sixteenthConfirmed, CampaignSourceAuthorityState.SeptemberSupplyActionId("begin", 0, 0, 0, 0), CampaignActionAudience.Axis)).SuccessorSession!;
        var rolledEarlyPlan = CampaignExercises.SubmitSourceSeptemberSupply(begunEarlyPlan,
            Submission(begunEarlyPlan, CampaignSourceAuthorityState.SeptemberSupplyActionId("roll", 3, 0, 0, 0), CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Contains("axis-prestart-supply-plan-incomplete", CampaignSourcePlayableWorld.Create(rolledEarlyPlan.SourceAuthority!).ReadinessBlockers());
        var finishedEarlyPlan = CampaignExercises.SubmitSourceSeptemberSupply(rolledEarlyPlan,
            Submission(rolledEarlyPlan, CampaignSourceAuthorityState.SeptemberSupplyActionId("finish", 3, 0, 0, 0), CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.DoesNotContain("axis-prestart-supply-plan-incomplete", CampaignSourcePlayableWorld.Create(finishedEarlyPlan.SourceAuthority!).ReadinessBlockers());
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(sixteenthConfirmed, sixteenthCommand).RejectionReason);
        Assert.Equal(seventhConfirmed.SourceAuthority.World.PendingArrivals, sixteenthConfirmed.SourceAuthority.World.PendingArrivals);
        Assert.True(CampaignExercises.Reconstruct(sixteenthConfirmed).IsVerified);
        var sixteenthHistory = sixteenthConfirmed.PregameHistory.Select(value => value.ToArray()).ToArray();
        var sixteenthEvent = System.Text.Encoding.UTF8.GetString(sixteenthHistory[^1]);
        using (var eventDocument = System.Text.Json.JsonDocument.Parse(sixteenthHistory[^1]))
        {
            var worldHash = eventDocument.RootElement.GetProperty("worldHash").GetString()!;
            var alteredHash = worldHash[..^1] + (worldHash[^1] == '0' ? '1' : '0');
            sixteenthHistory[^1] = System.Text.Encoding.UTF8.GetBytes(sixteenthEvent.Replace(worldHash, alteredHash, StringComparison.Ordinal));
        }
        var tamperedSixteenth = new ExerciseSession(sixteenthConfirmed.Pregame!, sixteenthConfirmed.CurrentContacts,
            sixteenthHistory, sixteenthConfirmed.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(tamperedSixteenth).IsVerified);
        var catanzaroResult = CampaignExercises.AdmitSourceCatanzaroGroupedWorld(sixteenthConfirmed);
        Assert.True(catanzaroResult.IsAccepted);
        var catanzaroProposed = catanzaroResult.SuccessorSession!;
        var catanzaro = catanzaroProposed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "catanzaro-division");
        Assert.Equal(13, catanzaro.Members.Count);
        Assert.Null(catanzaro.StackValue);
        Assert.Equal(35, catanzaro.MotorPool);
        var catanzaroChoice = CampaignExercises.Query(catanzaroProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "catanzaro-disposition");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(catanzaroProposed,
            Submission(catanzaroProposed, catanzaroChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var catanzaroCommand = Submission(catanzaroProposed, catanzaroChoice.ActionId, CampaignActionAudience.Axis);
        var catanzaroConfirmed = CampaignExercises.Submit(catanzaroProposed, catanzaroCommand).SuccessorSession!;
        Assert.Equal(42, catanzaroConfirmed.SourceAuthority!.ProfileVersion);
        var axisFinish = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var axisFinished = CampaignExercises.Submit(catanzaroConfirmed,
            Submission(catanzaroConfirmed, axisFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwFinishAfterAxis = CampaignExercises.Query(axisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var bothFinishedBeforeCorrection = CampaignExercises.Submit(axisFinished,
            Submission(axisFinished, cwFinishAfterAxis.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(3, bothFinishedBeforeCorrection.SourceAuthority!.FinalizedOwners);
        var correctedFinalized = CampaignExercises.CorrectSourceSeventhShell(bothFinishedBeforeCorrection).SuccessorSession!;
        Assert.Equal(1, correctedFinalized.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedFinalizedLegacyCheckpointContractVersion, correctedFinalized.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(correctedFinalized).IsVerified);
        var choicesBefore = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "sixth-australian-placement")
            .Select(value => value.ChoiceJson).Order(StringComparer.Ordinal).ToArray();
        var choicesAfter = CampaignExercises.Query(axisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "sixth-australian-placement")
            .Select(value => value.ChoiceJson).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(choicesBefore, choicesAfter);
        var australianChoice = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "sixth-australian-placement"
                && value.SourceActionId == CampaignSourceAuthorityState.SixthAustralianActionId("e1829"));
        Assert.DoesNotContain(CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "sixth-australian-placement"
                && value.SourceActionId == CampaignSourceAuthorityState.SixthAustralianActionId(chosenCairoHex));
        var australianSession = CampaignExercises.Submit(catanzaroConfirmed,
            Submission(catanzaroConfirmed, australianChoice.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var australian = australianSession.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "6th-australian-division");
        Assert.Equal(10, australian.Members.Count);
        Assert.Equal(5, australian.StackValue);
        Assert.False(australian.MotorPoolKnown);
        var australianInfantry = australian.Members.Where(value => value.ComponentType == "infantry").ToArray();
        Assert.Equal(6, australianInfantry.Length);
        Assert.All(australianInfantry, value => { Assert.Equal(6, value.ComponentToe); Assert.Equal(10, value.CapabilityPointAllowance); });
        foreach (var id in new[] { "16-australian-infantry-brigade-hq", "17-australian-infantry-brigade-hq" })
        {
            var headquarters = australian.Members.Single(value => value.ElementId == id);
            Assert.Equal(1, headquarters.ComponentToe); Assert.Equal(20, headquarters.CapabilityPointAllowance);
        }
        var cavalry = australian.Members.Single(value => value.ElementId == "6-australian-divisional-cavalry");
        Assert.Equal(8, cavalry.ComponentToe); Assert.Equal(45, cavalry.CapabilityPointAllowance);
        Assert.Equal(catanzaroConfirmed.SourceAuthority!.RandomState, australianSession.SourceAuthority.RandomState);
        Assert.Equal(45, australianSession.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(australianSession).IsVerified);
        var axisBatchChoice = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "ccnn-batch-disposition");
        var afterAustralianAxisChoice = CampaignExercises.Query(australianSession, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "ccnn-batch-disposition");
        Assert.Equal(axisBatchChoice.ChoiceJson, afterAustralianAxisChoice.ChoiceJson);
        var australianFirst = CampaignExercises.Submit(australianSession,
            Submission(australianSession, afterAustralianAxisChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var fullyMotorized = australianFirst;
        for (var index = 0; index < 5; index++)
        {
            var adjustment = CampaignExercises.SubmitSourceFourthIndian(fullyMotorized,
                Submission(fullyMotorized, priorityAction, CampaignActionAudience.Commonwealth));
            Assert.True(adjustment.IsAccepted);
            fullyMotorized = adjustment.SuccessorSession!;
        }
        var motorizedWorld = CampaignSourcePlayableWorld.Create(fullyMotorized.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true);
        Assert.Contains("seventh-armored-shell-stack-correction-required", motorizedWorld.ReadinessBlockers());
        Assert.Contains("owner-setup-finalization-incomplete", motorizedWorld.ReadinessBlockers());
        Assert.Contains("central-india-horse-training-state-unresolved", motorizedWorld.ReadinessBlockers());
        static HashSet<string> RepresentedIds(CampaignSourcePlayableWorld world) => world.GroupedWorld.Prior.World.Representations
            .SelectMany(value => value.BoundElementIds)
            .Concat(world.GroupedWorld.Groups.SelectMany(value => value.Members.Select(member => member.ElementId)))
            .Concat(world.GroupedWorld.Detached.Select(value => value.Member.ElementId))
            .ToHashSet(StringComparer.Ordinal);
        static bool IsRemovedFromPlay(CampaignSourcePlayableWorld world, CampaignSourceBattlefieldEntry value) =>
            world.Source.CwFreeGunsWorld?.GarrisonRemainderContent is not null
            && value.AuthorityStatus == "source-land-only-aa-removed"
            && ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds.Contains(value.ElementId, StringComparer.Ordinal);
        var categoryBIds = ContentSourceCwAttachedArtilleryPackArtifact.AllIds
            .Concat(ContentSourceBardiaGarrisonRemainderPackArtifact.AllIds)
            .Concat(ContentSourceTripoliTankRegimentPackArtifact.AllIds).ToArray();
        Assert.Equal(12, categoryBIds.Length);
        var representedBefore = RepresentedIds(motorizedWorld);
        var unrepresentedBefore = motorizedWorld.Battlefield.Entries
            .Where(entry => !representedBefore.Contains(entry.ElementId) && !IsRemovedFromPlay(motorizedWorld, entry))
            .Select(entry => entry.ElementId).ToHashSet(StringComparer.Ordinal);
        Assert.All(categoryBIds, id => Assert.Contains(id, unrepresentedBefore));
        Assert.Contains("forces-incomplete", motorizedWorld.ReadinessBlockers());
        // Build up to MotorMachineGunContent via the already-merged Giarabub chain
        // (production methods, not touched by this PR) so CW frontline has its
        // required predecessor.
        var activated = CampaignExercises.ActivateSourceAxisMinorGarrisons(fullyMotorized).SuccessorSession!;
        activated = CampaignExercises.AdmitSourceCompactCheckpoint(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceAxisGarrisonRemainder(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceGiarabubMotorMachineGun(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceCwFrontline(activated).SuccessorSession!;
        // Reconstruct (full replay verification) is confirmed through this point,
        // which is covered by the replay dispatcher. Tobruk/Bardia and this PR's 3
        // new packs are not: PR #355's own dedicated test
        // (GrazianiSourceTobrukBardiaGarrisonTests.cs) never calls Reconstruct after
        // activating Tobruk/Bardia either - its event types have no replay-dispatcher
        // case, a pre-existing gap this PR does not attempt to close.
        Assert.True(CampaignExercises.Reconstruct(activated).IsVerified);
        activated = CampaignExercises.AdmitSourceTobrukBardiaGarrison(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceTobrukBardiaGarrison(activated).SuccessorSession!;
        // The 3 new Category B packs this PR adds.
        activated = CampaignExercises.AdmitSourceBardiaGarrisonRemainder(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceBardiaGarrisonRemainder(activated).SuccessorSession!;
        activated = CampaignExercises.AdmitSourceTripoliTankRegiment(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceTripoliTankRegiment(activated).SuccessorSession!;
        activated = CampaignExercises.AdmitSourceCwAttachedArtillery(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceCwAttachedArtillery(activated).SuccessorSession!;
        var activatedWorld = CampaignSourcePlayableWorld.Create(activated.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true);
        var afterBlockers = activatedWorld.ReadinessBlockers();
        var representedAfter = RepresentedIds(activatedWorld);
        var unrepresentedAfter = activatedWorld.Battlefield.Entries
            .Where(entry => !representedAfter.Contains(entry.ElementId) && !IsRemovedFromPlay(activatedWorld, entry))
            .Select(entry => entry.ElementId).ToHashSet(StringComparer.Ordinal);
        Assert.All(categoryBIds, id => Assert.DoesNotContain(id, unrepresentedAfter));
        // forces-incomplete still fires: these 12 elements plus the Giarabub (13)
        // and Tobruk/Bardia (28) clusters activated along the way were only 53 of
        // this fixture's unrepresented elements - the 2 Aresca elements (fixed by
        // sequencing, see the other new proof test above) and any remaining
        // research-blocked elements are untouched by this chain.
        Assert.Contains("forces-incomplete", afterBlockers);
        Assert.True(unrepresentedAfter.Count < unrepresentedBefore.Count);
        // Only this chain's activated elements leave the unrepresented set.
        Assert.All(categoryBIds, id => Assert.Contains(id, unrepresentedBefore.Except(unrepresentedAfter, StringComparer.Ordinal)));
    }

    private static void RunSourceSoak(ExerciseSession initial)
    {
        foreach (var seed in new[] { 104, 345, 1979 })
        {
            var random = new Random(seed);
            var left = initial;
            var right = initial;
            var step = 0;
            try
            {
                for (; step < 32; step++)
                {
                    var audience = step % 2 == 0 ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
                    var options = CampaignExercises.Query(left, audience).ActionSet!.Candidates.OfType<CampaignSourceSetupAction>().ToArray();
                    Assert.True(options.Length > 0, $"No action at seed {seed}, step {step}");
                    var choice = options[random.Next(options.Length)];
                    var before = left.SourceAuthority!.SerializeCheckpoint();
                    var forged = Submission(left, "sha256:" + new string('0', 64), audience);
                    Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(left, forged).RejectionReason);
                    Assert.Equal(before, left.SourceAuthority.SerializeCheckpoint());
                    var command = Submission(left, choice.ActionId, audience);
                    var stale = command with { ExpectedStateVersion = command.ExpectedStateVersion - 1 };
                    Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(left, stale).RejectionReason);
                    Assert.Equal(before, left.SourceAuthority.SerializeCheckpoint());
                    var one = CampaignExercises.Submit(left, command);
                    var two = CampaignExercises.Submit(right, Submission(right, choice.ActionId, audience));
                    Assert.True(one.IsAccepted && two.IsAccepted, $"Rejected {choice.Family} at seed {seed}, step {step}");
                    Assert.Equal(one.Evidence!.SnapshotCheckpoint, two.Evidence!.SnapshotCheckpoint);
                    left = one.SuccessorSession!;
                    right = two.SuccessorSession!;
                    Assert.Equal(left.SourceAuthority!.RandomState, right.SourceAuthority!.RandomState);
                    Assert.Equal(initial.SourceAuthority!.World.PendingArrivals, left.SourceAuthority.World.PendingArrivals);
                    Assert.Equal(initial.SourceAuthority.GroupedWorld!.Groups.Count, left.SourceAuthority.GroupedWorld!.Groups.Count);
                }
                Assert.True(CampaignExercises.Reconstruct(left).IsVerified, $"Replay failed at seed {seed}");
                Assert.True(CampaignExercises.Reconstruct(right).IsVerified, $"Repeated replay failed at seed {seed}");
            }
            catch (Exception error)
            {
                throw new InvalidOperationException($"Graziani soak failed at seed {seed}, step {step}.", error);
            }
        }
    }

    [Fact]
    public void ActivatingTheGiarabubClusterRemovesItsFourteenElementsFromForcesIncomplete()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var initialization = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "source-authority-transition", Cna1979Ruleset.Manifest.Hash, 0UL,
            CampaignPregameSourceCreation.SetupId,
            CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, initialization),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var session = CampaignExercises.Begin(request, artifact, initialization).Session!;
        var polishActionPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        var polishAction = new CampaignSourceTurnAction(17, "e1730");
        var polishSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            polishActionPosition.PositionId, CampaignActionAudience.Commonwealth, [polishAction], CampaignLegalActionSet.SourceTurnPolicyId);
        var polishBytes = CampaignLegalActionSerializer.Serialize(polishSet);
        Assert.Equal(polishSet, CampaignLegalActionSerializer.DeserializeCanonical(polishBytes));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            polishActionPosition.PositionId, CampaignActionAudience.Axis, [polishAction], CampaignLegalActionSet.SourceTurnPolicyId));
        var arrivalPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        var declineAttack = CampaignSourceTurnAction.DeclineGt1Lane2Attack(17);
        var arrivalSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            arrivalPosition.PositionId, CampaignActionAudience.Commonwealth, [declineAttack], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(arrivalSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(arrivalSet)));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            arrivalPosition.PositionId, CampaignActionAudience.Axis, [declineAttack], CampaignLegalActionSet.SourceTurnPolicyId));
        var convoyPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.PhaseId == LandPhaseIds.NavalConvoySchedule);
        var convoyRoll = CampaignSourceTurnAction.RollGt3Supply(17);
        var convoySet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Axis, [convoyRoll], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(convoySet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(convoySet)));
        var convoyChoices = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Axis,
            [CampaignSourceTurnAction.AdjustGt3Supply(17, 2, 1, 1), CampaignSourceTurnAction.FinishGt3Supply(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(convoyChoices, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(convoyChoices)));
        var cwRoll = CampaignSourceTurnAction.RollGt5Supply(17);
        var cwConvoySet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Commonwealth, [cwRoll], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(cwConvoySet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(cwConvoySet)));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Axis, [cwRoll], CampaignLegalActionSet.SourceTurnPolicyId));
        var shippingPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.PhaseId == LandPhaseIds.TacticalShipping);
        var shippingSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            shippingPosition.PositionId, CampaignActionAudience.Commonwealth, [CampaignSourceTurnAction.DeclineTacticalShipping(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(shippingSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(shippingSet)));
        var declarationPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1 && value.PhaseId == LandPhaseIds.InitiativeDeclaration);
        var declarationSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            declarationPosition.PositionId, CampaignActionAudience.Axis, [CampaignSourceTurnAction.ActFirst(17), CampaignSourceTurnAction.ActLast(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(declarationSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(declarationSet)));
        var organizationPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1 && value.PhaseId == LandPhaseIds.Organization);
        var organizationSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            organizationPosition.PositionId, CampaignActionAudience.Commonwealth, [CampaignSourceTurnAction.DeclineVoluntaryOrganization(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(organizationSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(organizationSet)));
        var interruptTraining = CampaignSourceTurnAction.InterruptSixthAustralianTraining(17);
        var trainingSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            organizationPosition.PositionId, CampaignActionAudience.Commonwealth, [interruptTraining], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(trainingSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(trainingSet)));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            organizationPosition.PositionId, CampaignActionAudience.Axis, [interruptTraining], CampaignLegalActionSet.SourceTurnPolicyId));
        var unknownTraining = CampaignSourceTrainingLedger.CreateSixthAustralian();
        var interruptedTraining = unknownTraining.Interrupt();
        Assert.True(interruptedTraining.Interrupted);
        Assert.Null(interruptedTraining.CurrentMorale);
        Assert.Null(interruptedTraining.QualifyingStages);
        Assert.NotEqual(unknownTraining.Hash(), interruptedTraining.Hash());
        Assert.Throws<InvalidOperationException>(() => interruptedTraining.Interrupt());
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Commonwealth, [convoyRoll], CampaignLegalActionSet.SourceTurnPolicyId));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Axis));
        var finishBeforeRoll = Submission(session, CampaignSourceAuthorityState.FinishActionId, CampaignActionAudience.Axis);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.FinishPregameAxisSupply(session, finishBeforeRoll).RejectionReason);
        var roll = Assert.Single(CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates);
        session = CampaignExercises.Submit(session, Submission(session, roll.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        if (1 > 0)
        {
            var add = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates
                .OfType<PregameAxisSupplyAllocationAction>()
                .Single(value => value.Lane == 2 && value.Stage == 1 && value.Delta == 1);
            session = CampaignExercises.Submit(session, Submission(session, add.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        var legacyOffers = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates.Select(x => x.ActionId).ToArray();
        var draft = CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Axis);
        Assert.Single(draft.PrivateActionIds);
        Assert.DoesNotContain(draft.PrivateActionIds[0], legacyOffers);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.FinishPregameAxisSupply(session, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Commonwealth)).RejectionReason);
        var accepted = CampaignExercises.FinishPregameAxisSupply(session, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Axis));
        Assert.True(accepted.IsAccepted);
        var next = accepted.SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(next).IsVerified);
        var giarabubAction = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c1014", 1);
        var oldAxisMotorHash = next.SourceAuthority!.AxisMotorization.Hash();
        var oldAxisChoices = CampaignExercises.QuerySourceMotorizationDraft(next, CampaignActionAudience.Axis);
        Assert.DoesNotContain(giarabubAction, oldAxisChoices.PrivateActionIds);
        var admittedPolicy = CampaignExercises.AdmitSourceGiarabubMotorPlacementPolicy(next).SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceGiarabubMotorPlacementPolicyAdmitted(admittedPolicy));
        Assert.NotEqual(oldAxisMotorHash, admittedPolicy.SourceAuthority!.AxisMotorization.Hash());
        Assert.Equal(oldAxisMotorHash, next.SourceAuthority.AxisMotorization.Hash());
        Assert.DoesNotContain(giarabubAction, CampaignExercises.QuerySourceMotorizationDraft(next, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Contains(giarabubAction, CampaignExercises.QuerySourceMotorizationDraft(admittedPolicy, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.DoesNotContain(giarabubAction, CampaignExercises.QuerySourceMotorizationDraft(admittedPolicy, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.AdjustSourceMotorization(admittedPolicy,
            Submission(admittedPolicy, giarabubAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        var placedMotor = CampaignExercises.AdjustSourceMotorization(admittedPolicy,
            Submission(admittedPolicy, giarabubAction, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(124, placedMotor.SourceAuthority!.AxisMotorization.Remaining("axis-libya-unattached"));
        Assert.Contains(placedMotor.SourceAuthority.AxisMotorization.Allocations,
            value => value.PoolId == "axis-libya-unattached" && value.LocationId == "c1014" && value.Points == 1);
        Assert.True(CampaignExercises.Reconstruct(placedMotor).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceGiarabubMotorPlacementPolicy(placedMotor).RejectionReason);
        var cwBeforeFinish = CampaignExercises.QuerySourceActionsDraft(next, CampaignActionAudience.Commonwealth);
        var finishOwner = CampaignExercises.Query(next, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var ownerFinished = CampaignExercises.Submit(next, Submission(next, finishOwner.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(1, ownerFinished.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.FinalizedLegacyCheckpointContractVersion, CampaignExercises.QueryCheckpoint(ownerFinished).ContractVersion);
        Assert.Equal(next.SourceAuthority!.RandomState, ownerFinished.SourceAuthority.RandomState);
        Assert.Equal(next.SourceAuthority.World.PendingArrivals, ownerFinished.SourceAuthority.World.PendingArrivals);
        Assert.Equal(cwBeforeFinish.VisibleBytes, CampaignExercises.QuerySourceActionsDraft(ownerFinished, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.PlaceSourceAxisDump(ownerFinished,
            Submission(ownerFinished, CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507"), CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceCompactCheckpoint(ownerFinished).RejectionReason);
        var cwWhileAxisFinished = CampaignExercises.PlaceSourceCommonwealthSupply(ownerFinished,
            Submission(ownerFinished, CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-dump-1", "d3612"), CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, cwWhileAxisFinished.SourceAuthority!.FinalizedOwners);
        var finishCw = CampaignExercises.Query(cwWhileAxisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var bothFinished = CampaignExercises.Submit(cwWhileAxisFinished, Submission(cwWhileAxisFinished, finishCw.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(3, bothFinished.SourceAuthority!.FinalizedOwners);
        Assert.True(CampaignExercises.Reconstruct(bothFinished).IsVerified);
        var reopenAxis = CampaignExercises.Query(bothFinished, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var reopenedAxis = CampaignExercises.Submit(bothFinished, Submission(bothFinished, reopenAxis.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, reopenedAxis.SourceAuthority!.FinalizedOwners);
        Assert.True(CampaignExercises.Reconstruct(reopenedAxis).IsVerified);
        var reopenCw = CampaignExercises.Query(reopenedAxis, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var reopenedBoth = CampaignExercises.Submit(reopenedAxis, Submission(reopenedAxis, reopenCw.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(0, reopenedBoth.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.FinalizedLegacyCheckpointContractVersion, CampaignExercises.QueryCheckpoint(reopenedBoth).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(reopenedBoth).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.PlaceSourceCommonwealthSupply(reopenedAxis,
            Submission(reopenedAxis, CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-cairo-1", "e1730"), CampaignActionAudience.Commonwealth)).RejectionReason);
        var lockedCwView = CampaignExercises.QuerySourceActionsDraft(reopenedAxis, CampaignActionAudience.Commonwealth).VisibleBytes;
        var changedAxis = CampaignExercises.PlaceSourceAxisDump(reopenedAxis,
            Submission(reopenedAxis, CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507"), CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, changedAxis.SourceAuthority!.FinalizedOwners);
        Assert.Equal(lockedCwView, CampaignExercises.QuerySourceActionsDraft(changedAxis, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.True(CampaignExercises.Reconstruct(changedAxis).IsVerified);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourcePolishArrivalDraft(next, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.SubmitSourcePolishArrival(next, Submission(next, roll.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var earlyCompact = CampaignExercises.AdmitSourceCompactCheckpoint(next).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(earlyCompact).ContractVersion);
        var compactFinish = CampaignExercises.Query(earlyCompact, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var finalizedCompact = CampaignExercises.Submit(earlyCompact, Submission(earlyCompact, compactFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignSourceAuthorityState.FinalizedCompactCheckpointContractVersion, CampaignExercises.QueryCheckpoint(finalizedCompact).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(finalizedCompact).IsVerified);
        var earlyDump = CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507");
        earlyCompact = CampaignExercises.PlaceSourceAxisDump(earlyCompact,
            Submission(earlyCompact, earlyDump, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(earlyCompact).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(earlyCompact).IsVerified);
        var normalAxis = CampaignExercises.Query(next, CampaignActionAudience.Axis);
        Assert.True(normalAxis.IsSuccessful);
        Assert.Equal(CampaignLegalActionSet.SourceSetupPolicyId, normalAxis.ActionSet!.PolicyId);
        var encodedActions = CampaignLegalActionSerializer.Serialize(normalAxis.ActionSet);
        Assert.Equal(encodedActions, CampaignLegalActionSerializer.Serialize(CampaignLegalActionSerializer.DeserializeCanonical(encodedActions)));
        var unifiedDump = normalAxis.ActionSet.Candidates.OfType<CampaignSourceSetupAction>()
            .Single(value => value.Family == "axis-dump" && value.ChoiceJson.Contains("c4507", StringComparison.Ordinal)
                && value.ChoiceJson.Contains("axis-dump-1", StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(normalAxis.ActionSet.CampaignId,
            normalAxis.ActionSet.StateVersion, normalAxis.ActionSet.RulesetHash, normalAxis.ActionSet.PositionId,
            CampaignActionAudience.Axis, [unifiedDump], CampaignLegalActionSet.CurrentPolicyId));
        var ownerCut = CampaignExercises.QuerySourceActionsDraft(next, CampaignActionAudience.Axis);
        Assert.DoesNotContain(unifiedDump.ActionId, System.Text.Encoding.UTF8.GetString(ownerCut.VisibleBytes));
        var ownContext = System.Text.Encoding.UTF8.GetString(ownerCut.VisibleBytes);
        Assert.Contains("axis-gt1-supply", ownContext);
        Assert.Contains("maximumSupply", ownContext);
        var hiddenCwAction = CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-dump-1", "d3612");
        var hiddenAdvance = CampaignExercises.PlaceSourceCommonwealthSupply(next,
            Submission(next, hiddenCwAction, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var afterHidden = CampaignExercises.QuerySourceActionsDraft(hiddenAdvance, CampaignActionAudience.Axis);
        Assert.Equal(ownerCut.VisibleBytes, afterHidden.VisibleBytes);
        Assert.NotEqual(ownerCut.PrivateActionIds[0], afterHidden.PrivateActionIds[0]);
        var wrongSide = Submission(next, unifiedDump.ActionId, CampaignActionAudience.Commonwealth);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(next, wrongSide).RejectionReason);
        var unifiedResult = CampaignExercises.Submit(next, Submission(next, unifiedDump.ActionId, CampaignActionAudience.Axis));
        Assert.True(unifiedResult.IsAccepted);
        Assert.Equal(unifiedDump.ActionId, unifiedResult.Evidence!.Receipt.ActionId);
        Assert.True(CampaignExercises.Reconstruct(unifiedResult.SuccessorSession!).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(unifiedResult.SuccessorSession!,
            Submission(next, unifiedDump.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(unifiedResult.SuccessorSession!,
            Submission(unifiedResult.SuccessorSession!, unifiedDump.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.FinishPregameAxisSupply(next, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Axis)).RejectionReason);
        using var snapshot = JsonDocument.Parse(accepted.Evidence!.SnapshotCheckpoint);
        Assert.Equal("setup-in-progress", snapshot.RootElement.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, snapshot.RootElement.GetProperty("gameClock").ValueKind);
        using var plan = JsonDocument.Parse(snapshot.RootElement.GetProperty("convoyPlanBase64").GetBytesFromBase64());
        Assert.Equal(3, plan.RootElement.GetProperty("maximumSupply").GetInt32());
        Assert.Equal(3 - 1, plan.RootElement.GetProperty("unusedAvailability").GetInt32());
        using var source = JsonDocument.Parse(snapshot.RootElement.GetProperty("sourceSnapshotBase64").GetBytesFromBase64());
        Assert.Contains(source.RootElement.GetProperty("pendingArrivalGroupIds").EnumerateArray().Select(x => x.GetString()),
            id => id == Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId);
        var dumpDraft = CampaignExercises.QuerySourceAxisDumpDraft(next, CampaignActionAudience.Axis);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisDumpDraft(next, CampaignActionAudience.Commonwealth));
        Assert.Equal(8, dumpDraft.PrivateActionIds.Count);
        var chosenId = CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507");
        Assert.Contains(chosenId, dumpDraft.PrivateActionIds);
        var dumpCommand = Submission(next, chosenId, CampaignActionAudience.Axis);
        var placed = CampaignExercises.PlaceSourceAxisDump(next, dumpCommand);
        Assert.True(placed.IsAccepted);
        var after = placed.SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(after).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
            CampaignExercises.PlaceSourceAxisDump(after, dumpCommand).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceAxisDump(after, Submission(after,
                CampaignSourceAuthorityState.DumpActionId("axis-dump-2", "c4507"), CampaignActionAudience.Axis)).RejectionReason);
        var second = CampaignExercises.PlaceSourceAxisDump(after, Submission(after,
            CampaignSourceAuthorityState.DumpActionId("axis-dump-2", "c4707"), CampaignActionAudience.Axis));
        Assert.True(second.IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(second.SuccessorSession!).IsVerified);
        var corruptHistory = second.SuccessorSession!.PregameHistory.Select(value => value.ToArray()).ToArray();
        var lastEvent = System.Text.Encoding.UTF8.GetString(corruptHistory[^1]).Replace("c4707", "c4507", StringComparison.Ordinal);
        corruptHistory[^1] = System.Text.Encoding.UTF8.GetBytes(lastEvent);
        var corrupt = new ExerciseSession(second.SuccessorSession.Pregame!, second.SuccessorSession.CurrentContacts,
            corruptHistory, second.SuccessorSession.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(corrupt).IsVerified);
        using var finalSnapshot = JsonDocument.Parse(second.Evidence!.SnapshotCheckpoint);
        using var world = JsonDocument.Parse(finalSnapshot.RootElement.GetProperty("axisSupplyWorldBase64").GetBytesFromBase64());
        Assert.Equal(22, world.RootElement.GetProperty("units").GetArrayLength());
        Assert.False(world.RootElement.GetProperty("siteCoverageComplete").GetBoolean());
        Assert.Equal(JsonValueKind.Null, world.RootElement.GetProperty("units").EnumerateArray()
            .Single(x => x.GetProperty("id").GetString() == "axis-derna-1").GetProperty("ammo").ValueKind);
        var cwSession = second.SuccessorSession!;
        Assert.Equal(CampaignSourceAuthorityState.ContractVersion, cwSession.SourceAuthority!.ProfileVersion);
        Assert.True(CampaignExercises.IsSourceSetupSeat(cwSession, CampaignActionAudience.Axis));
        Assert.True(CampaignExercises.IsSourceSetupSeat(cwSession, CampaignActionAudience.Commonwealth));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Axis));
        var placements = new[]
        {
            ("cw-dump-1", "d3612"),
            ("cw-alexandria-1", "e3613"), ("cw-alexandria-2", "e3613"), ("cw-alexandria-3", "e3714"),
            ("cw-cairo-1", "e1829"), ("cw-cairo-2", "e1830"), ("cw-cairo-3", "e1930"),
        };
        foreach (var (id, location) in placements)
        {
            var idHash = CampaignSourceAuthorityState.CommonwealthSupplyActionId(id, location);
            var cwDraft = CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Commonwealth);
            Assert.Contains(idHash, cwDraft.PrivateActionIds);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCommonwealthSupply(cwSession, Submission(cwSession, idHash, CampaignActionAudience.Axis)).RejectionReason);
            var step = CampaignExercises.PlaceSourceCommonwealthSupply(cwSession, Submission(cwSession, idHash, CampaignActionAudience.Commonwealth));
            Assert.True(step.IsAccepted);
            cwSession = step.SuccessorSession!;
            Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        }
        Assert.Empty(CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.Equal(15, cwSession.SourceAuthority!.CommonwealthSupplyWorld.Units.Count);
        Assert.Equal(18, cwSession.SourceAuthority.CommonwealthInitialForces.Forces.Count);
        Assert.Null(cwSession.SourceAuthority.CommonwealthInitialForces.Forces
            .Single(value => value.ElementId == "central-india-horse").InitialToes);
        Assert.Null(cwSession.SourceAuthority.Position);
        var axisDummy = CampaignSourceAuthorityState.DummyActionId(true, "axis-dummy-1", "c4407");
        Assert.Contains(axisDummy, CampaignExercises.QuerySourceDummyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, axisDummy, CampaignActionAudience.Commonwealth)).RejectionReason);
        var dummyStep = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, axisDummy, CampaignActionAudience.Axis));
        Assert.True(dummyStep.IsAccepted);
        cwSession = dummyStep.SuccessorSession!;
        var omit = CampaignSourceAuthorityState.DummyActionId(true, "axis-dummy-2", null);
        cwSession = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, omit, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwDummy = CampaignSourceAuthorityState.DummyActionId(false, "cw-dummy-1", "d3611");
        Assert.Contains(cwDummy, CampaignExercises.QuerySourceDummyDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        cwSession = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, cwDummy, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var priorRandom = cwSession.SourceAuthority!.RandomState;
        var expectedGt1 = SandtableRandom.RollD6(priorRandom);
        var rollGt1 = CampaignSourceAuthorityState.OpeningConvoyActionId("roll", 0, 0, 0);
        Assert.Single(CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, rollGt1, CampaignActionAudience.Commonwealth)).RejectionReason);
        var gt1Step = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, rollGt1, CampaignActionAudience.Axis));
        Assert.True(gt1Step.IsAccepted);
        cwSession = gt1Step.SuccessorSession!;
        Assert.Equal(expectedGt1.Value, cwSession.SourceAuthority!.OpeningConvoyPlan.Die);
        Assert.Equal(expectedGt1.State.NextByteCursor, cwSession.SourceAuthority.RandomState.NextByteCursor);
        if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply > 0)
        {
            // Real source lane 3 under disclosed provisional Benghazi ruling.
            // This branches the same rolled plan; it does not advance a game.
            var lane3Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 3, 1, 1);
            var lane3Session = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession,
                Submission(cwSession, lane3Add, CampaignActionAudience.Axis)).SuccessorSession!;
            var lane3Finish = CampaignSourceAuthorityState.OpeningConvoyActionId("finish", 0, 0, 0);
            lane3Session = CampaignExercises.SubmitSourceAxisGt1Supply(lane3Session,
                Submission(lane3Session, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
            var lane3Source = lane3Session.SourceAuthority!;
            var axisBefore = CampaignSourceGt1AxisArrival.Create(lane3Source);
            var benghazi = CampaignSourceGt1BenghaziArrival.Create(lane3Source, axisBefore);
            Assert.True(benghazi.CanAnnounce);
            var announced = benghazi.Announce(lane3Source, axisBefore);
            var (delivered, axisAfter) = announced.DeclineAttackAndDeliver(lane3Source, axisBefore);
            Assert.True(delivered.Delivered);
            Assert.Equal("b4827", Assert.Single(lane3Source.AxisSupplyWorld.Units,
                value => value.Id == "axis-benghazi-1").LocationId);
            Assert.Equal("a4827", Assert.Single(axisAfter.Supply.Units,
                value => value.Id == "axis-benghazi-1").LocationId);
            var landed = Assert.Single(axisAfter.Supply.Units, value => value.Id == "axis-gt1-op1-lane3-1");
            Assert.Equal("a4827", landed.LocationId);
            Assert.Equal(40, landed.Ammo);
            Assert.Equal(60, landed.Fuel);
            Assert.Equal(2, axisAfter.Supply.Units.Count(value => value.LocationId == "a4827"));
            Assert.False(axisAfter.HasOtherStageOneLanes);
            Assert.Throws<InvalidOperationException>(() => announced.DeclineAttackAndDeliver(lane3Source, axisAfter));
            var (replayedBenghazi, replayedAxis) = benghazi.Announce(lane3Source, axisBefore)
                .DeclineAttackAndDeliver(lane3Source, axisBefore);
            Assert.Equal(delivered.Hash(), replayedBenghazi.Hash());
            Assert.Equal(axisAfter.Hash(), replayedAxis.Hash());
            using var landedJson = JsonDocument.Parse(axisAfter.Supply.SerializeCanonical());
            Assert.Equal(2, landedJson.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Contains("rul-gra-05-provisional", landedJson.RootElement.GetProperty("benghaziCoordinateStatus").GetString()!);
            if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply >= 2)
            {
                var mixed = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession,
                    Submission(cwSession, lane3Add, CampaignActionAudience.Axis)).SuccessorSession!;
                var lane2Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 2, 1, 1);
                mixed = CampaignExercises.SubmitSourceAxisGt1Supply(mixed,
                    Submission(mixed, lane2Add, CampaignActionAudience.Axis)).SuccessorSession!;
                mixed = CampaignExercises.SubmitSourceAxisGt1Supply(mixed,
                    Submission(mixed, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
                var mixedSource = mixed.SourceAuthority!;
                var mixedAxis = CampaignSourceGt1AxisArrival.Create(mixedSource);
                Assert.False(mixedAxis.CanAnnounce);
                Assert.False(CampaignSourceGt1BenghaziArrival.Create(mixedSource, mixedAxis).CanAnnounce);
                Assert.Throws<InvalidOperationException>(() => mixedAxis.DeliverSoleBenghaziLane(mixedSource, 1));
                var multi = CampaignSourceGt1MultiArrival.Announce(mixedSource, mixedAxis);
                Assert.Collection(multi.Lanes, value => Assert.Equal(2, value), value => Assert.Equal(3, value));
                var (firstThree, axisThree) = multi.DeclineAndDeliver(mixedSource, mixedAxis, 3);
                var (threeThenTwo, axisThreeThenTwo) = firstThree.DeclineAndDeliver(mixedSource, axisThree, 2);
                var (firstTwo, axisTwo) = multi.DeclineAndDeliver(mixedSource, mixedAxis, 2);
                var (twoThenThree, axisTwoThenThree) = firstTwo.DeclineAndDeliver(mixedSource, axisTwo, 3);
                Assert.True(threeThenTwo.IsDelivered(2));
                Assert.True(threeThenTwo.IsDelivered(3));
                Assert.Equal(axisThreeThenTwo.Supply.Hash(), axisTwoThenThree.Supply.Hash());
                Assert.Equal(threeThenTwo.Hash(), twoThenThree.Hash());
                Assert.Equal("box.tripoli", Assert.Single(axisThreeThenTwo.Supply.Units,
                    value => value.Id == "axis-gt1-op1-lane2-1").LocationId);
                Assert.Equal("a4827", Assert.Single(axisThreeThenTwo.Supply.Units,
                    value => value.Id == "axis-gt1-op1-lane3-1").LocationId);
                Assert.Throws<InvalidOperationException>(() => threeThenTwo.DeclineAndDeliver(mixedSource, axisThreeThenTwo, 3));
                Assert.DoesNotContain(mixedSource.AxisSupplyWorld.Units, value => value.Id == "axis-gt1-op1-lane3-1");
                if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply >= 3)
                {
                    var triple = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession,
                        Submission(cwSession, lane3Add, CampaignActionAudience.Axis)).SuccessorSession!;
                    triple = CampaignExercises.SubmitSourceAxisGt1Supply(triple,
                        Submission(triple, lane2Add, CampaignActionAudience.Axis)).SuccessorSession!;
                    var lane6Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 6, 1, 1);
                    triple = CampaignExercises.SubmitSourceAxisGt1Supply(triple,
                        Submission(triple, lane6Add, CampaignActionAudience.Axis)).SuccessorSession!;
                    triple = CampaignExercises.SubmitSourceAxisGt1Supply(triple,
                        Submission(triple, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
                    var tripleSource = triple.SourceAuthority!;
                    var tripleAxis = CampaignSourceGt1AxisArrival.Create(tripleSource);
                    var tripleMulti = CampaignSourceGt1MultiArrival.Announce(tripleSource, tripleAxis);
                    var (tripleTwo, tripleAfterTwo) = tripleMulti.DeclineAndDeliver(tripleSource, tripleAxis, 2);
                    var (tripleThree, tripleAfterThree) = tripleTwo.DeclineAndDeliver(tripleSource, tripleAfterTwo, 3);
                    Assert.True(tripleThree.IsDue(6));
                    Assert.False(tripleThree.IsDelivered(6));
                    Assert.True(tripleThree.CanDecline(6));
                    var (tripleSix, tripleAfterSix) = tripleThree.DeclineAndDeliver(tripleSource, tripleAfterThree, 6);
                    Assert.True(tripleSix.Lane6Declined);
                    Assert.True(tripleSix.IsDelivered(6));
                    Assert.Equal("c4807", Assert.Single(tripleAfterSix.Supply.Units,
                        value => value.Id == "axis-gt1-op1-lane6-1").LocationId);
                    Assert.Throws<InvalidOperationException>(() => tripleSix.DeclineAndDeliver(tripleSource, tripleAfterSix, 6));
                    (bool CanDecline, bool AttackDeclined)? oneBefore = null, oneAfter = null, threeBefore = null, threeAfter = null;
                    for (var amount = 1; amount <= 3; amount++)
                    {
                        var tobruk = cwSession;
                        for (var n = 0; n < amount; n++)
                            tobruk = CampaignExercises.SubmitSourceAxisGt1Supply(tobruk,
                                Submission(tobruk, lane6Add, CampaignActionAudience.Axis)).SuccessorSession!;
                        tobruk = CampaignExercises.SubmitSourceAxisGt1Supply(tobruk,
                            Submission(tobruk, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
                        var tobrukSource = tobruk.SourceAuthority!;
                        var tobrukAxis = CampaignSourceGt1AxisArrival.Create(tobrukSource);
                        var tobrukAnnounced = CampaignSourceGt1MultiArrival.Announce(tobrukSource, tobrukAxis);
                        var (tobrukDeclined, tobrukAfter) = tobrukAnnounced.DeclineAndDeliver(tobrukSource, tobrukAxis, 6);
                        if (amount == 1) { oneBefore = tobrukAnnounced.Lane6CommonwealthCut(); oneAfter = tobrukDeclined.Lane6CommonwealthCut(); }
                        if (amount == 3) { threeBefore = tobrukAnnounced.Lane6CommonwealthCut(); threeAfter = tobrukDeclined.Lane6CommonwealthCut(); }
                        Assert.True(tobrukDeclined.Lane6Declined);
                        Assert.False(tobrukDeclined.CanDecline(6));
                        Assert.Equal(amount <= 2, tobrukDeclined.IsDelivered(6));
                        Assert.Equal(3 + (amount <= 2 ? amount : 0),
                            tobrukAfter.Supply.Units.Count(value => value.LocationId == "c4807"));
                        if (amount == 3) Assert.Equal(tobrukAxis.Supply.Hash(), tobrukAfter.Supply.Hash());
                        Assert.Equal(3, tobrukSource.AxisSupplyWorld.Units.Count(value => value.LocationId == "c4807"));
                    }
                    Assert.Equal(oneBefore, threeBefore);
                    Assert.Equal(oneAfter, threeAfter);
                }
            }
            var gt1Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 2, 1, 1);
            cwSession = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, gt1Add, CampaignActionAudience.Axis)).SuccessorSession!;
            Assert.Single(cwSession.SourceAuthority!.OpeningConvoyPlan.Allocations());
        }
        var gt1Finish = CampaignSourceAuthorityState.OpeningConvoyActionId("finish", 0, 0, 0);
        cwSession = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, gt1Finish, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(cwSession.SourceAuthority!.OpeningConvoyPlan.IsFinished);
        var pendingTripoli = CampaignSourceGt1AxisArrival.Create(cwSession.SourceAuthority!);
        var sourceSupplyHash = cwSession.SourceAuthority!.AxisSupplyWorld.Hash();
        if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply > 0)
        {
            Assert.True(pendingTripoli.CanAnnounce);
            var announcedTripoli = pendingTripoli.Announce();
            Assert.True(announcedTripoli.Announced);
            Assert.False(announcedTripoli.Delivered);
            var deliveredTripoli = announcedTripoli.DeclineAttackAndDeliver();
            var deliveredUnit = Assert.Single(deliveredTripoli.Supply.Units, unit => unit.Id == "axis-gt1-op1-lane2-1");
            Assert.Equal("box.tripoli", deliveredUnit.LocationId);
            Assert.Equal(40, deliveredUnit.Ammo);
            Assert.Equal(60, deliveredUnit.Fuel);
            Assert.Equal(sourceSupplyHash, cwSession.SourceAuthority.AxisSupplyWorld.Hash());
            Assert.NotEqual(pendingTripoli.Hash(), deliveredTripoli.Hash());
            Assert.Throws<InvalidOperationException>(() => deliveredTripoli.DeclineAttackAndDeliver());
        }
        else
        {
            Assert.False(pendingTripoli.CanAnnounce);
            Assert.Throws<InvalidOperationException>(() => pendingTripoli.Announce());
        }

        Assert.Empty(CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var skipCwGt1 = CampaignSourceAuthorityState.CommonwealthSupplyPlanActionId("skip", 1);
        var cwPlanning = CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Commonwealth);
        Assert.Contains(skipCwGt1, cwPlanning.PrivateActionIds);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Axis));
        var beforeSkip = cwSession.SourceAuthority.RandomState.NextByteCursor;
        cwSession = CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession,
            Submission(cwSession, skipCwGt1, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(beforeSkip, cwSession.SourceAuthority!.RandomState.NextByteCursor);
        Assert.Equal("skipped-opening", cwSession.SourceAuthority.CommonwealthSupplyPlans!.Entries[0].Status);
        foreach (var turn in new[] { 2, 3, 4 })
        {
            var prior = cwSession.SourceAuthority.RandomState;
            var expected = SandtableRandom.RollD6(prior);
            var action = CampaignSourceAuthorityState.CommonwealthSupplyPlanActionId("roll", turn);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession, Submission(cwSession, action, CampaignActionAudience.Axis)).RejectionReason);
            cwSession = CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession,
                Submission(cwSession, action, CampaignActionAudience.Commonwealth)).SuccessorSession!;
            var entry = cwSession.SourceAuthority!.CommonwealthSupplyPlans!.Entries.Single(value => value.TargetGameTurn == turn);
            Assert.Equal(expected.Value, entry.Die);
            Assert.Equal((expected.Value + 1) / 2, entry.SupplyUnits);
            Assert.Equal(expected.State.NextByteCursor, cwSession.SourceAuthority.RandomState.NextByteCursor);
        }
        Assert.Empty(CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var gt1Plan = CampaignSourceGt1SupplyPlanning.Create(cwSession.SourceAuthority!, cwSession.SourceAuthority!.RandomState);
        var gt5 = CampaignSourceGt1CommonwealthSupplyPlanning.Create();
        var expectedGt5 = SandtableRandom.RollD6(gt1Plan.RandomState);
        var rolledGt5 = gt5.Roll(gt1Plan.RandomState);
        Assert.Equal(expectedGt5.Value, rolledGt5.Plan.Die);
        Assert.Equal((expectedGt5.Value + 1) / 2, rolledGt5.Plan.Units);
        Assert.Equal(expectedGt5.State, rolledGt5.Random);
        Assert.Throws<InvalidOperationException>(() => rolledGt5.Plan.Roll(rolledGt5.Random));
        Assert.Equal(expectedGt5.State, gt1Plan.WithRandomState(rolledGt5.Random).RandomState);
        var expectedGt3 = SandtableRandom.RollD6(gt1Plan.RandomState);
        var rolledGt3 = gt1Plan.Roll();
        Assert.Equal(expectedGt3.Value, rolledGt3.Die);
        Assert.Equal(expectedGt3.Value switch { 1 => 0, <= 3 => 1, <= 5 => 2, _ => 3 }, rolledGt3.Maximum);
        Assert.Equal(expectedGt3.State, rolledGt3.RandomState);
        Assert.Throws<InvalidOperationException>(() => rolledGt3.Roll());
        Assert.Empty(rolledGt3.ValidDeltas(1, 1));
        Assert.Empty(rolledGt3.ValidDeltas(2, 4));
        if (rolledGt3.Maximum > 0)
        {
            var assigned = rolledGt3.Adjust(2, 1, 1);
            Assert.Equal(1, assigned.QuantityAt(2, 1));
            Assert.Equal(rolledGt3.RandomState, assigned.RandomState);
            Assert.Equal(0, assigned.Adjust(2, 1, -1).QuantityAt(2, 1));
            for (var unit = 1; unit < rolledGt3.Maximum; unit++) assigned = assigned.Adjust(2, 1, 1);
            Assert.DoesNotContain(1, assigned.ValidDeltas(2, 1));
            Assert.DoesNotContain(1, assigned.ValidDeltas(3, 2));
            Assert.Throws<InvalidOperationException>(() => assigned.Adjust(3, 2, 1));
        }
        var finishedGt3 = rolledGt3.Finish();
        Assert.True(finishedGt3.Finished);
        Assert.Empty(finishedGt3.ValidDeltas(2, 1));
        // Each owner's roll may happen first; the shared stream must advance
        // once per roll without rewriting an already-finished Axis plan.
        var axisFirstPlan = gt1Plan.Roll().Finish();
        var axisFirstHash = axisFirstPlan.Hash();
        var cwAfterAxis = CampaignSourceGt1CommonwealthSupplyPlanning.Create().Roll(axisFirstPlan.RandomState);
        Assert.Equal(axisFirstHash, axisFirstPlan.Hash());
        var cwRollFirst = CampaignSourceGt1CommonwealthSupplyPlanning.Create().Roll(gt1Plan.RandomState);
        var axisAfterCw = gt1Plan.WithRandomState(cwRollFirst.Random).Roll().Finish();
        Assert.Equal(axisFirstPlan.Die, cwRollFirst.Plan.Die);
        Assert.Equal(cwAfterAxis.Plan.Die, axisAfterCw.Die);
        Assert.Equal(cwAfterAxis.Random, axisAfterCw.RandomState);
        Assert.Throws<InvalidOperationException>(() => axisAfterCw.Roll());
        Assert.Throws<InvalidOperationException>(() => cwAfterAxis.Plan.Roll(cwAfterAxis.Random));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        // Exercise the real prestart September plans on an independent branch of the retained source session.
        var september = cwSession;
        var beginSeptember = CampaignSourceAuthorityState.SeptemberSupplyActionId("begin", 0, 0, 0, 0);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceSeptemberSupplyDraft(september, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, beginSeptember, CampaignActionAudience.Commonwealth)).RejectionReason);
        var normalSeptember = CampaignExercises.Query(september, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "axis-september-supply");
        Assert.DoesNotContain(CampaignExercises.Query(september, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "axis-september-supply");
        var beginCommand = Submission(september, normalSeptember.ActionId, CampaignActionAudience.Axis);
        september = CampaignExercises.Submit(september, beginCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(september, beginCommand).RejectionReason);
        foreach (var turn in new[] { 3, 4 })
        {
            var expected = SandtableRandom.RollD6(september.SourceAuthority!.RandomState);
            var septemberRoll = CampaignSourceAuthorityState.SeptemberSupplyActionId("roll", turn, 0, 0, 0);
            september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, septemberRoll, CampaignActionAudience.Axis)).SuccessorSession!;
            var entry = september.SourceAuthority!.SeptemberPlans!.Entries.Single(value => value.TargetGameTurn == turn);
            Assert.Equal(expected.Value, entry.Die);
            Assert.Equal(expected.Value switch { 1 => 0, <= 3 => 1, <= 5 => 2, _ => 3 }, entry.Maximum);
            Assert.Equal(expected.State.NextByteCursor, september.SourceAuthority.RandomState.NextByteCursor);
            var finish = CampaignSourceAuthorityState.SeptemberSupplyActionId("finish", turn, 0, 0, 0);
            if (entry.Maximum > 0)
            {
                var zeroPlan = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, finish, CampaignActionAudience.Axis)).SuccessorSession!;
                Assert.True(zeroPlan.SourceAuthority!.SeptemberPlans!.Entries.Single(value => value.TargetGameTurn == turn).Finished);
                Assert.Equal(0, zeroPlan.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == turn).Allocations.Sum());
                var add = CampaignSourceAuthorityState.SeptemberSupplyActionId("adjust", turn, 2, 1, 1);
                september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, add, CampaignActionAudience.Axis)).SuccessorSession!;
            }
            september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, finish, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        Assert.Null(september.SourceAuthority!.SeptemberPlans!.NextTargetGameTurn);
        Assert.Equal(38, september.SourceAuthority.ProfileVersion);
        var retainedGt3 = CampaignSourceGt1SupplyPlanning.Create(september.SourceAuthority, september.SourceAuthority.RandomState);
        Assert.Equal(september.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == 3).Die, retainedGt3.Die);
        Assert.True(retainedGt3.CarriedPlanFinished);
        Assert.Equal(september.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == 3).Allocations[0], retainedGt3.QuantityAt(2, 1));
        Assert.Throws<InvalidOperationException>(() => retainedGt3.Roll());
        var septemberHash = september.SourceAuthority.SeptemberPlans.Hash();
        september = CampaignExercises.AdmitSourceAxisSupplement(september).SuccessorSession!;
        Assert.Equal(septemberHash, september.SourceAuthority!.SeptemberPlans!.Hash());
        Assert.True(CampaignExercises.Reconstruct(september).IsVerified);
        Assert.False(CampaignExercises.IsSourceAxisSupplementAdmitted(cwSession));
        var migrated = CampaignExercises.AdmitSourceAxisSupplement(cwSession);
        Assert.True(migrated.IsAccepted);
        cwSession = migrated.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceAxisSupplementAdmitted(cwSession));
        Assert.Equal(5, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceAxisSupplement(cwSession).RejectionReason);
        var replacements = CampaignSourceAuthorityState.AutoblindaActionId("replacements", "");
        Assert.Contains(replacements, CampaignExercises.QuerySourceAutoblindaDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, replacements, CampaignActionAudience.Commonwealth)).RejectionReason);
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, replacements, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, cwSession.SourceAuthority!.Autoblinda!.ReplacementPoints);
        var reset = CampaignSourceAuthorityState.AutoblindaActionId("reset", "");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, reset, CampaignActionAudience.Axis)).SuccessorSession!;
        var attach1 = CampaignSourceAuthorityState.AutoblindaActionId("attach", "1-141-infantry");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach1, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach1, CampaignActionAudience.Axis)).RejectionReason);
        var attach2 = CampaignSourceAuthorityState.AutoblindaActionId("attach", "2-141-infantry");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach2, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(cwSession.SourceAuthority!.Autoblinda!.IsComplete);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.False(CampaignExercises.IsSourceCommonwealthSupplementAdmitted(cwSession));
        var cwMigrated = CampaignExercises.AdmitSourceCommonwealthSupplement(cwSession);
        Assert.True(cwMigrated.IsAccepted);
        cwSession = cwMigrated.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCommonwealthSupplementAdmitted(cwSession));
        Assert.Equal(6, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(12, cwSession.SourceAuthority!.CommonwealthSupplement!.Forces.Count);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceCommonwealthSupplement(cwSession).RejectionReason);
        Assert.False(CampaignExercises.IsSourcePriorityMotorizationAdmitted(cwSession));
        var priorityMigration = CampaignExercises.AdmitSourcePriorityMotorization(cwSession);
        Assert.True(priorityMigration.IsAccepted);
        cwSession = priorityMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourcePriorityMotorizationAdmitted(cwSession));
        Assert.Equal(7, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourcePriorityMotorizationDraft(cwSession, CampaignActionAudience.Axis));
        var priorityId = CampaignSourceAuthorityState.PriorityMotorizationActionId("1-kings-royal-rifle-corps", 6);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourcePriorityMotorization(cwSession, Submission(cwSession, priorityId, CampaignActionAudience.Axis)).RejectionReason);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, priorityId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(14, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3922-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        var reversePriority = CampaignSourceAuthorityState.PriorityMotorizationActionId("1-kings-royal-rifle-corps", -6);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, reversePriority, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(20, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3922-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        var secondPriority = CampaignSourceAuthorityState.PriorityMotorizationActionId("2-rifle-brigade", 6);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, secondPriority, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(4, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3320-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var axisMigration = CampaignExercises.AdmitSourceAxisPriorityMotorization(cwSession);
        Assert.True(axisMigration.IsAccepted);
        cwSession = axisMigration.SuccessorSession!;
        Assert.Equal(8, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var targetAxis = "1-1-libyan-motor-infantry";
        var oldDetach = CampaignSourceAuthorityState.AxisPriorityActionId("detach", targetAxis, 0);
        var historical = CampaignExercises.ReplaySourceAxisPriority(cwSession,
            Submission(cwSession, oldDetach, CampaignActionAudience.Axis));
        Assert.True(historical.IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(historical.SuccessorSession!).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, oldDetach, CampaignActionAudience.Axis)).RejectionReason);
        var prematureLift = CampaignSourceAuthorityState.AxisPriorityActionId("adjust", targetAxis, 5);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, prematureLift, CampaignActionAudience.Axis)).RejectionReason);
        foreach (var axisTarget in CampaignSourceAxisPriorityMotorization.Targets)
        {
            var detach = CampaignSourceAuthorityState.AxisPriorityActionId("detach", axisTarget, 0);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, detach, CampaignActionAudience.Axis)).RejectionReason);
        }
        Assert.Equal(35, cwSession.SourceAuthority!.AxisPriorityMotorization!.Remaining(cwSession.SourceAuthority.AxisMotorization));
        var undoDetachment = CampaignSourceAuthorityState.AxisPriorityActionId("undo-detachment", targetAxis, 0);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, undoDetachment, CampaignActionAudience.Axis)).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var garrisonMigration = CampaignExercises.AdmitSourceAxisMinorGarrisons(cwSession);
        Assert.True(garrisonMigration.IsAccepted);
        cwSession = garrisonMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceAxisMinorGarrisonsAdmitted(cwSession));
        Assert.Equal(9, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(13, cwSession.SourceAuthority!.AxisGarrisons!.Forces.Count);
        Assert.Contains(cwSession.SourceAuthority.AxisGarrisons.Forces,
            value => value.ElementId == "giarabub-motor-machine-gun");
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceAxisMinorGarrisons(cwSession).RejectionReason);
        var battlefieldMigration = CampaignExercises.AdmitSourceBattlefield(cwSession);
        Assert.True(battlefieldMigration.IsAccepted);
        cwSession = battlefieldMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceBattlefieldAdmitted(cwSession));
        Assert.Equal(10, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var battlefield = cwSession.SourceAuthority!.Battlefield!;
        Assert.Equal(battlefield.Entries.Count, battlefield.Entries.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(2, battlefield.Pending.Count);
        Assert.Equal(8, battlefield.Pending.Single(value => value.GroupId == Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId).ElementIds.Count);
        Assert.Equal(4, battlefield.Pending.Single(value => value.GroupId == Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId).ElementIds.Count);
        using var battlefieldBytes = JsonDocument.Parse(battlefield.SerializeCanonical());
        Assert.False(battlefieldBytes.RootElement.GetProperty("battlefieldComplete").GetBoolean());
        Assert.False(battlefieldBytes.RootElement.GetProperty("controlCertified").GetBoolean());
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceBattlefield(cwSession).RejectionReason);
        var cairoAdmission = CampaignExercises.AdmitSourceCairoSetup(cwSession);
        Assert.True(cairoAdmission.IsAccepted);
        cwSession = cairoAdmission.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCairoSetupAdmitted(cwSession));
        Assert.Equal(11, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(4, cwSession.SourceAuthority!.CairoSetup!.PendingIds.Count);
        Assert.Equal(4, cwSession.SourceAuthority.Battlefield!.Entries.Count(x => x.AuthorityStatus == "source-setup-unplaced"));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCairoSetupDraft(cwSession, CampaignActionAudience.Axis));
        var invalidCity = CampaignSourceAuthorityState.CairoPlacementActionId("1-welch", "e1829");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, invalidCity, CampaignActionAudience.Commonwealth)).RejectionReason);
        var inferredCity = CampaignSourceAuthorityState.CairoPlacementActionId("1-welch", "e1931");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, inferredCity, CampaignActionAudience.Commonwealth)).RejectionReason);
        var chosenCairoHex = 0UL == 14 ? "e1830" : "e1730";
        foreach (var element in CampaignSourceCairoInitialSetup.Units.Select(value => value.ElementId))
        {
            var action = CampaignSourceAuthorityState.CairoPlacementActionId(element, chosenCairoHex);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, action, CampaignActionAudience.Axis)).RejectionReason);
            var command = Submission(cwSession, action, CampaignActionAudience.Commonwealth);
            var placedCairo = CampaignExercises.PlaceSourceCairoUnit(cwSession, command);
            Assert.True(placedCairo.IsAccepted);
            cwSession = placedCairo.SuccessorSession!;
            Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
                CampaignExercises.PlaceSourceCairoUnit(cwSession, command).RejectionReason);
        }
        Assert.Empty(cwSession.SourceAuthority!.CairoSetup!.PendingIds);
        Assert.Equal(4, cwSession.SourceAuthority.Battlefield!.Entries.Count(x => x.LocationId == chosenCairoHex && x.AuthorityStatus == "source-setup-placed"));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var activeMigration = CampaignExercises.AdmitSourceCairoWorld(cwSession);
        Assert.True(activeMigration.IsAccepted);
        cwSession = activeMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCairoWorldAdmitted(cwSession));
        Assert.Equal(12, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var active = cwSession.SourceAuthority!.CairoWorld!;
        Assert.Equal("sha256:3a7fddd510854c81b408e07742dd837f7be416206117ca58ea79357357a11ddd", active.Content.Hash());
        Assert.NotEqual(cwSession.SourceAuthority.Content.Identity.Hash, active.Content.Identity.Hash);
        Assert.Equal(active.Content.Identity, active.World.ContentIdentity);
        Assert.Equal(8, active.World.World.Elements.Count);
        Assert.Equal(8, active.World.World.Representations.Count);
        Assert.Equal(2, active.World.PendingArrivals.Count);
        foreach (var unit in CampaignSourceCairoInitialSetup.Units)
        {
            var element = Assert.Single(active.World.World.Elements, value => value.ElementId == unit.ElementId);
            Assert.Equal(chosenCairoHex, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
        }
        var alteredContentText = System.Text.Encoding.UTF8.GetString(active.Content.CanonicalBytes)
            .Replace("\"initialToe\":6", "\"initialToe\":7", StringComparison.Ordinal);
        var alteredContent = ContentSourceSetupPackArtifact.Read(System.Text.Encoding.UTF8.GetBytes(alteredContentText));
        Assert.NotEqual(active.Content.Hash(), alteredContent.Hash());
        var forgedHistory = cwSession.PregameHistory.Select(value => value.ToArray()).ToArray();
        var forgedEvent = System.Text.Encoding.UTF8.GetString(forgedHistory[^1])
            .Replace(active.Content.Hash(), alteredContent.Hash(), StringComparison.Ordinal);
        forgedHistory[^1] = System.Text.Encoding.UTF8.GetBytes(forgedEvent);
        var forgedSession = new ExerciseSession(cwSession.Pregame!, cwSession.CurrentContacts, forgedHistory, cwSession.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(forgedSession).IsVerified);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceCairoWorld(cwSession).RejectionReason);
        var directV12 = cwSession;
        Assert.True(CampaignExercises.CanPromoteSourceIndependent(directV12));
        var lockBeforePromotion = CampaignExercises.Query(directV12, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var lockedBeforePromotion = CampaignExercises.Submit(directV12,
            Submission(directV12, lockBeforePromotion.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.PromoteSourceIndependent(lockedBeforePromotion).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.ReplaySourceIndependentPromotion(lockedBeforePromotion).RejectionReason);
        directV12 = CampaignExercises.PromoteSourceIndependent(directV12).SuccessorSession!;
        Assert.Equal(14, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(10, directV12.SourceAuthority!.World.World.Elements.Count);
        var genericIdentity = directV12.SourceAuthority.World.ContentIdentity;
        Assert.False(CampaignExercises.CanPromoteSourceIndependent(directV12));
        directV12 = CampaignExercises.AdmitSourceLocalMap(directV12).SuccessorSession!;
        Assert.Equal(15, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var mapIdentity = directV12.SourceAuthority!.World.ContentIdentity;
        Assert.NotEqual(genericIdentity, mapIdentity);
        Assert.Equal(10, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.PromoteSourceExpandedIndependent(directV12).SuccessorSession!;
        Assert.Equal(16, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(13, directV12.SourceAuthority!.World.World.Elements.Count);
        var expandedIdentity = directV12.SourceAuthority.World.ContentIdentity;
        Assert.False(CampaignExercises.CanPromoteSourceExpandedIndependent(directV12));
        Assert.False(CampaignExercises.CanPromoteSourceMaletti(directV12));
        Assert.Equal(expandedIdentity, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(mapIdentity, directV12.SourceAuthority.LocalMapWorld!.Content.Identity);
        Assert.Equal(genericIdentity, directV12.SourceAuthority.IndependentWorld!.Content.Identity);
        Assert.Equal(13, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.AdmitSourceAttachedInitialLoads(directV12).SuccessorSession!;
        Assert.Equal(17, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        const string loadedTarget = "2-rifle-brigade";
        var attach = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("attach", loadedTarget);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, attach, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, attach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, directV12.SourceAuthority!.AttachedInitialLoads!.Reserved("cw-c3320-attached"));
        var fuel = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("fuel", loadedTarget);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, fuel, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, Assert.Single(directV12.SourceAuthority!.AttachedInitialLoads!.Targets).FuelPoints);
        var detachLoad = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("detach", loadedTarget);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, detachLoad, CampaignActionAudience.Commonwealth)).RejectionReason);
        var removeLift = CampaignSourceAuthorityState.PriorityMotorizationActionId(loadedTarget, -6);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourcePriorityMotorization(directV12, Submission(directV12, removeLift, CampaignActionAudience.Commonwealth)).RejectionReason);
        var clear = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("unload-fuel", loadedTarget);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, clear, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, detachLoad, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Empty(directV12.SourceAuthority!.AttachedInitialLoads!.Targets);
        directV12 = CampaignExercises.AdmitSourceInitialWrecks(directV12).SuccessorSession!;
        Assert.Equal(18, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(3, directV12.SourceAuthority!.InitialWrecks!.PendingIds.Count);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceInitialWrecksDraft(directV12, CampaignActionAudience.Axis));
        foreach (var id in directV12.SourceAuthority.InitialWrecks.PendingIds.ToArray())
        {
            var place = CampaignSourceAuthorityState.InitialWreckPlacementActionId(id, "e3613");
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceInitialWreck(directV12, Submission(directV12, place, CampaignActionAudience.Axis)).RejectionReason);
            directV12 = CampaignExercises.PlaceSourceInitialWreck(directV12, Submission(directV12, place, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        }
        Assert.Empty(directV12.SourceAuthority!.InitialWrecks!.PendingIds);
        Assert.Equal(3, directV12.SourceAuthority.InitialWrecks.Placements.Count);
        directV12 = CampaignExercises.AdmitSourceAxisFreeSetup(directV12).SuccessorSession!;
        Assert.Equal(19, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var forgedLegacyState = directV12.SourceAuthority!.ApplyAxisPriorityChoice("detach", targetAxis, 0);
        var forgedLegacyEvent = directV12.SourceAuthority.SerializeAxisPriorityEvent(forgedLegacyState, "detach", targetAxis, 0);
        var forgedLegacySession = new ExerciseSession(directV12.Pregame!, directV12.CurrentContacts.AdvancePregameSource(directV12.SourceAuthority.StateVersion),
            [.. directV12.PregameHistory, forgedLegacyEvent], forgedLegacyState);
        Assert.False(CampaignExercises.Reconstruct(forgedLegacySession).IsVerified);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisFreeSetupDraft(directV12, CampaignActionAudience.Commonwealth));
        var freeIds = directV12.SourceAuthority!.AxisFreeArtillery!.PendingIds.ToArray();
        for (var i = 0; i < freeIds.Length; i++)
        {
            var location = i < 2 ? "c4407" : "c4607";
            var action = CampaignSourceAuthorityState.AxisFreePlacementActionId(freeIds[i], location);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, action, CampaignActionAudience.Commonwealth)).RejectionReason);
            if (i == 2)
            {
                var overstack = CampaignSourceAuthorityState.AxisFreePlacementActionId(freeIds[i], "c4407");
                Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                    CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, overstack, CampaignActionAudience.Axis)).RejectionReason);
            }
            directV12 = CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, action, CampaignActionAudience.Axis)).SuccessorSession!;
            if (i == 0)
            {
                directV12 = CampaignExercises.PromoteSourceAxisFreeWorld(directV12).SuccessorSession!;
                Assert.Equal(20, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
                Assert.Equal(14, directV12.SourceAuthority!.World.World.Elements.Count);
                var identityAfterPartial = directV12.SourceAuthority.World.ContentIdentity;
                var adjustAxis = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", 1);
                directV12 = CampaignExercises.AdjustSourceMotorization(directV12,
                    Submission(directV12, adjustAxis, CampaignActionAudience.Axis)).SuccessorSession!;
                directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12,
                    Submission(directV12, attach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
                Assert.Equal(identityAfterPartial, directV12.SourceAuthority!.World.ContentIdentity);
                Assert.Equal(14, directV12.SourceAuthority.World.World.Elements.Count);
                Assert.False(CampaignExercises.CanPromoteSourceIndependent(directV12));
                Assert.False(CampaignExercises.CanPromoteSourceExpandedIndependent(directV12));
            }
        }
        Assert.Empty(directV12.SourceAuthority!.AxisFreeArtillery!.PendingIds);
        Assert.Equal(3, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-1ar-placed"));
        var axisContent = directV12.SourceAuthority.World.ContentIdentity;
        directV12 = CampaignExercises.PromoteSourceAxisFreeWorld(directV12).SuccessorSession!;
        Assert.Equal(axisContent, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(17, directV12.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Equal(4, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-1ar-active"));
        Assert.False(CampaignExercises.CanPromoteSourceAxisFreeWorld(directV12));
        directV12 = CampaignExercises.AdmitSourceAxisCorpsSetup(directV12).SuccessorSession!;
        Assert.Equal(21, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisCorpsSetupDraft(directV12, CampaignActionAudience.Commonwealth));
        var corpsIds = directV12.SourceAuthority!.AxisCorpsArtillery!.PendingIds.ToArray();
        for (var i = 0; i < corpsIds.Length; i++)
        {
            var location = i == 0 ? "c4307" : "c4308";
            var action = CampaignSourceAuthorityState.AxisCorpsPlacementActionId(corpsIds[i], location);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, action, CampaignActionAudience.Commonwealth)).RejectionReason);
            if (i == 1)
            {
                var occupied = CampaignSourceAuthorityState.AxisCorpsPlacementActionId(corpsIds[i], "c4307");
                Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                    CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, occupied, CampaignActionAudience.Axis)).RejectionReason);
            }
            directV12 = CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, action, CampaignActionAudience.Axis)).SuccessorSession!;
            directV12 = CampaignExercises.PromoteSourceAxisCorpsWorld(directV12).SuccessorSession!;
            Assert.Equal(22, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        }
        Assert.Equal(19, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Equal(2, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-corps-artillery-active"));
        foreach (var id in corpsIds)
            Assert.Equal(3, directV12.SourceAuthority.World.World.Elements.Single(value => value.ElementId == id).Components.Single(value => value.ComponentId.EndsWith(".light-aa", StringComparison.Ordinal)).CurrentToe);
        Assert.False(CampaignExercises.CanPromoteSourceAxisCorpsWorld(directV12));
        var corpsContent = directV12.SourceAuthority.World.ContentIdentity;
        var removeAxisPoint = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", -1);
        directV12 = CampaignExercises.AdjustSourceMotorization(directV12,
            Submission(directV12, removeAxisPoint, CampaignActionAudience.Axis)).SuccessorSession!;
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12,
            Submission(directV12, fuel, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(corpsContent, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(19, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.AdmitSourceMalettiHierarchy(directV12).SuccessorSession!;
        Assert.Equal(23, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var confirmHierarchy = CampaignSourceAuthorityState.MalettiHierarchyActionId("confirm", null);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMalettiHierarchy(directV12, Submission(directV12, confirmHierarchy, CampaignActionAudience.Commonwealth)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceMalettiHierarchy(directV12,
            Submission(directV12, confirmHierarchy, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwFirst = directV12;
        var frenchFirst = directV12;
        var certifiedDetach = CampaignSourceAuthorityState.MalettiHierarchyActionId("detach", targetAxis);
        directV12 = CampaignExercises.SubmitSourceMalettiHierarchy(directV12,
            Submission(directV12, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(targetAxis, directV12.SourceAuthority!.MalettiHierarchy!.DetachedId);
        var otherDetach = CampaignSourceAuthorityState.MalettiHierarchyActionId("detach", "3-1-libyan-motor-infantry");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMalettiHierarchy(directV12, Submission(directV12, otherDetach, CampaignActionAudience.Axis)).RejectionReason);
        Assert.True(CampaignExercises.CanPromoteSourceCertifiedMalettiWorld(directV12));
        directV12 = CampaignExercises.PromoteSourceCertifiedMalettiWorld(directV12).SuccessorSession!;
        Assert.Equal(24, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(20, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Contains(directV12.SourceAuthority.Battlefield!.Entries,
            value => value.ElementId == targetAxis && value.AuthorityStatus == "source-certified-maletti-active");
        Assert.False(CampaignExercises.CanPromoteSourceCertifiedMalettiWorld(directV12));
        var axisLift = CampaignSourceAuthorityState.AxisPriorityActionId("adjust", targetAxis, 5);
        var lockBeforeLift = CampaignExercises.Query(directV12, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var lockedBeforeLift = CampaignExercises.Submit(directV12,
            Submission(directV12, lockBeforeLift.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(lockedBeforeLift,
                Submission(lockedBeforeLift, axisLift, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceAxisPriority(directV12,
            Submission(directV12, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(5, directV12.SourceAuthority!.AxisPriorityMotorization!.AssignedPoints(targetAxis));
        directV12 = CampaignExercises.AdmitSourceMatruhFormation(directV12).SuccessorSession!;
        Assert.Equal(25, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceMatruhFormationDraft(directV12, CampaignActionAudience.Axis));
        var matruhConfirm = CampaignSourceAuthorityState.MatruhFormationActionId("confirm", null);
        directV12 = CampaignExercises.SubmitSourceMatruhFormation(directV12,
            Submission(directV12, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(5, directV12.SourceAuthority!.MatruhFormation!.AttachedIds.Count);
        var matruhDetach = CampaignSourceAuthorityState.MatruhFormationActionId("detach", "1-essex");
        directV12 = CampaignExercises.SubmitSourceMatruhFormation(directV12,
            Submission(directV12, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(4, directV12.SourceAuthority!.MatruhFormation!.AttachedIds.Count);
        Assert.Contains(directV12.SourceAuthority.Battlefield!.Entries,
            entry => entry.ElementId == "1-essex" && entry.AuthorityStatus == "source-matruh-detached");
        var secondMatruhDetach = CampaignSourceAuthorityState.MatruhFormationActionId("detach", "1-durham-light-infantry");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMatruhFormation(directV12, Submission(directV12, secondMatruhDetach, CampaignActionAudience.Commonwealth)).RejectionReason);
        directV12 = CampaignExercises.PromoteSourceMatruhDetachedWorld(directV12).SuccessorSession!;
        Assert.Equal(26, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(21, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(4, directV12.SourceAuthority.MatruhFormation!.AttachedIds.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        cwFirst = CampaignExercises.AdmitSourceMatruhFormation(cwFirst).SuccessorSession!;
        cwFirst = CampaignExercises.SubmitSourceMatruhFormation(cwFirst,
            Submission(cwFirst, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        cwFirst = CampaignExercises.SubmitSourceMatruhFormation(cwFirst,
            Submission(cwFirst, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        cwFirst = CampaignExercises.PromoteSourceMatruhDetachedWorld(cwFirst).SuccessorSession!;
        var cwFirstIdentity = cwFirst.SourceAuthority!.World.ContentIdentity;
        cwFirst = CampaignExercises.SubmitSourceMalettiHierarchy(cwFirst,
            Submission(cwFirst, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        cwFirst = CampaignExercises.PromoteSourceCertifiedMalettiWorld(cwFirst).SuccessorSession!;
        Assert.NotEqual(cwFirstIdentity, cwFirst.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(21, cwFirst.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, cwFirst.SourceAuthority.World.PendingArrivals.Count);
        Assert.True(CampaignExercises.Reconstruct(cwFirst).IsVerified);
        var rngBeforeGt2 = directV12.SourceAuthority!.RandomState;
        var worldBeforeGt2 = directV12.SourceAuthority.World.ContentIdentity;
        directV12 = CampaignExercises.AdmitSourceGt2Correction(directV12).SuccessorSession!;
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var finishGt2 = CampaignSourceAuthorityState.Gt2CorrectionActionId("finish", 0, 0, 0);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Commonwealth)).RejectionReason);
        if (3 > 0)
        {
            foreach (var lane in new[] { 2, 3, 6 })
                for (var stage = 1; stage <= 3; stage++)
                    while (directV12.SourceAuthority!.Gt2Correction!.Quantity(lane, stage) > 0)
                    {
                        var removeUnit = CampaignSourceAuthorityState.Gt2CorrectionActionId("adjust", lane, stage, -1);
                        directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12,
                            Submission(directV12, removeUnit, CampaignActionAudience.Axis)).SuccessorSession!;
                    }
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Axis)).RejectionReason);
            var addGt2 = CampaignSourceAuthorityState.Gt2CorrectionActionId("adjust", 2, 1, 1);
            directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, addGt2, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(directV12.SourceAuthority!.Gt2Correction!.IsFinished);
        Assert.Equal(rngBeforeGt2, directV12.SourceAuthority.RandomState);
        Assert.Equal(worldBeforeGt2, directV12.SourceAuthority.World.ContentIdentity);
        directV12 = CampaignExercises.AdmitSourceCwPriorityCatalog(directV12).SuccessorSession!;
        Assert.Equal(28, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        directV12 = CampaignExercises.PromoteSourceCwPriorityWorld(directV12).SuccessorSession!;
        Assert.Equal(29, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(22, directV12.SourceAuthority!.World.World.Elements.Count);
        var frenchAdd = CampaignSourceAuthorityState.CwPriorityCatalogActionId("1-french-motor-marines", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceCwPriorityCatalog(directV12, Submission(directV12, frenchAdd, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceCwPriorityCatalog(directV12,
            Submission(directV12, frenchAdd, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, directV12.SourceAuthority!.CwPriorityCatalog!.FrenchPoints);
        Assert.Equal(1, directV12.SourceAuthority.CwPriorityCatalog.Remaining(directV12.SourceAuthority, "cw-c3926-attached"));
        var frenchRemove = CampaignSourceAuthorityState.CwPriorityCatalogActionId("1-french-motor-marines", -1);
        directV12 = CampaignExercises.SubmitSourceCwPriorityCatalog(directV12,
            Submission(directV12, frenchRemove, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(2, directV12.SourceAuthority!.CwPriorityCatalog!.Remaining(directV12.SourceAuthority, "cw-c3926-attached"));
        frenchFirst = CampaignExercises.AdmitSourceCwPriorityCatalog(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.PromoteSourceCwPriorityWorld(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.AdmitSourceCwFreeGuns(frenchFirst).SuccessorSession!;
        Assert.Equal(31, frenchFirst.SourceAuthority!.ProfileVersion);
        var freeGunIds = frenchFirst.SourceAuthority!.CwFreeGuns!.PendingIds.ToArray();
        var gunCairoHex = 0UL == 14 ? "e1730" : "e1830";
        foreach (var id in freeGunIds)
        {
            var placeGun = CampaignSourceAuthorityState.CwFreeGunPlacementActionId(id, gunCairoHex);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCwFreeGun(frenchFirst, Submission(frenchFirst, placeGun, CampaignActionAudience.Axis)).RejectionReason);
            frenchFirst = CampaignExercises.PlaceSourceCwFreeGun(frenchFirst,
                Submission(frenchFirst, placeGun, CampaignActionAudience.Commonwealth)).SuccessorSession!;
            frenchFirst = CampaignExercises.PromoteSourceCwFreeGunsWorld(frenchFirst).SuccessorSession!;
        }
        Assert.Equal(32, frenchFirst.SourceAuthority!.ProfileVersion);
        _ = ContentSourceFourthIndianPackArtifact.Create(frenchFirst.SourceAuthority!.CwFreeGunsWorld!.Content);
        var indianResult = CampaignExercises.AdmitSourceFourthIndianWorld(frenchFirst);
        Assert.True(indianResult.IsAccepted);
        var indian = indianResult.SuccessorSession!;
        var grouped = indian.SourceAuthority!.GroupedWorld!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(indian.SourceAuthority.SerializeLegacyCheckpoint()),
            indian.SourceAuthority.DigestLegacyCheckpoint());
        var beforeMinor = indian.SourceAuthority.SerializeCheckpoint();
        var pendingBeforeMinor = indian.SourceAuthority.World.PendingArrivals;
        var randomBeforeMinor = indian.SourceAuthority.RandomState;
        var minorResult = CampaignExercises.ActivateSourceAxisMinorGarrisons(indian);
        Assert.True(minorResult.IsAccepted);
        var minor = minorResult.SuccessorSession!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(minor.SourceAuthority!.CwFreeGunsWorld!.SerializeCanonical()),
            minor.SourceAuthority.CwFreeGunsWorld.Hash());
        Assert.Equal(CampaignPregameSourceCreation.Digest(minor.SourceAuthority!.SerializeLegacyCheckpoint()),
            minor.SourceAuthority.DigestLegacyCheckpoint());
        Assert.Equal(beforeMinor, indian.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.AxisMinorGarrisonsCheckpointContractVersion, minor.SourceAuthority!.ProfileVersion);
        Assert.Equal(randomBeforeMinor, minor.SourceAuthority.RandomState);
        Assert.Equal(pendingBeforeMinor, minor.SourceAuthority.World.PendingArrivals);
        Assert.Equal(grouped.ContentHash, minor.SourceAuthority.GroupedWorld!.ContentHash);
        Assert.Equal(grouped.Groups.Count, minor.SourceAuthority.GroupedWorld.Groups.Count);
        Assert.Equal(0, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("fort-maddalena-garrison", "c3019"));
        Assert.Equal(1, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("fort-maddalena-garrison", "c3020"));
        Assert.Equal(0, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("mechili-garrison", "b4921"));
        Assert.Equal(1, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("mechili-garrison", "b4922"));
        foreach (var unit in ContentSourceAxisMinorGarrisonsPackArtifact.Units)
        {
            Assert.Equal(0, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt(unit.Id, unit.Location));
            var element = Assert.Single(minor.SourceAuthority.World.World.Elements, value => value.ElementId == unit.Id);
            Assert.Equal(unit.Location, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
            Assert.Single(minor.SourceAuthority.World.World.Representations, value =>
                value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
                && value.CurrentLocationId == unit.Location && value.BoundElementIds.SequenceEqual([unit.Id]));
            Assert.Contains(minor.SourceAuthority.Battlefield!.Entries, value => value.ElementId == unit.Id
                && value.AuthorityStatus == "source-axis-minor-active");
        }
        // Use the actual activated counters, not source-evidence-only RTR or
        // Hussars. The two observers are independent counters, not formations.
        var minorContactWorld = CampaignSourcePlayableWorld.Create(minor.SourceAuthority);
        var activePair = CampaignSourcePolishArrivalContacts.EffectiveCounters(minorContactWorld)
            .Where(value => value.RepresentationId is "axis-minor.scheferzen-garrison" or "independent-setup.2-rifle-brigade")
            .ToArray();
        Assert.Equal(2, activePair.Length);
        Assert.All(activePair, value => Assert.Equal(CampaignContactObserverKind.IndependentCounter, value.ObserverKind));
        var pairAxis = new CampaignContactKnowledge(minor.SourceAuthority.Request.CampaignId, LandSide.Axis);
        var pairCw = new CampaignContactKnowledge(minor.SourceAuthority.Request.CampaignId, LandSide.Commonwealth);
        var axisPairContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(activePair, 1, pairAxis));
        var cwPairContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(activePair, 1, pairCw));
        Assert.Equal(CampaignContactKnowledge.IndependentObserverContractVersion, axisPairContact.ContractVersion);
        Assert.Equal(CampaignContactKnowledge.IndependentObserverContractVersion, cwPairContact.ContractVersion);
        var axisPairBytes = CampaignContactTransitionCodec.SerializeCanonical(axisPairContact);
        Assert.Equal(axisPairContact, CampaignContactTransitionCodec.DeserializeCanonical(axisPairBytes));
        Assert.Equal(axisPairBytes, CampaignContactTransitionCodec.SerializeCanonical(Assert.Single(
            CampaignSourceInitialContacts.DeriveCounters(activePair, 1,
                new CampaignContactKnowledge(minor.SourceAuthority.Request.CampaignId, LandSide.Axis)))));
        pairAxis.Apply(axisPairContact);
        pairCw.Apply(cwPairContact);
        Assert.Empty(pairAxis.ObserveFor(axisPairContact.ObserverFormationId));
        var ownCounterView = CampaignIndependentContactObservationV2.Project(minorContactWorld, pairAxis,
            "axis-minor.scheferzen-garrison");
        Assert.Single(ownCounterView.Contacts);
        Assert.DoesNotContain("independent-setup.2-rifle-brigade", System.Text.Encoding.UTF8.GetString(ownCounterView.SerializeCanonical()));
        Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(minorContactWorld,
            pairCw, "axis-minor.scheferzen-garrison"));
        var repeatedPairContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(activePair, 2, pairAxis));
        Assert.Equal(axisPairContact.ContactHandle, repeatedPairContact.ContactHandle);
        Assert.True(CampaignExercises.Reconstruct(minor).IsVerified);
        var compactMinorResult = CampaignExercises.AdmitSourceCompactCheckpoint(minor);
        Assert.True(compactMinorResult.IsAccepted);
        var compactMinor = compactMinorResult.SuccessorSession!;
        var compactMinorBytes = compactMinor.SourceAuthority!.SerializeCheckpoint();
        using (var compactMinorJson = JsonDocument.Parse(compactMinorBytes))
        {
            var baseBytes = compactMinorJson.RootElement.GetProperty("baseCheckpoint").GetBytesFromBase64();
            var artifactBytes = compactMinor.SourceAuthority.CwFreeGunsWorld!.MinorGarrisonContent!.CanonicalBytes;
            Console.WriteLine($"minor checkpoint bytes: compact={compactMinorBytes.Length}, base={baseBytes.Length}, artifact={artifactBytes.Length}");
            Assert.True(compactMinorBytes.Length < 16_384, $"Compact minor checkpoint was {compactMinorBytes.Length} bytes; base {baseBytes.Length}, artifact {artifactBytes.Length}.");
            Assert.False(compactMinorJson.RootElement.TryGetProperty("minorGarrisonContent", out _));
            Assert.Equal(compactMinor.SourceAuthority.World.ContentIdentity.Hash,
                compactMinorJson.RootElement.GetProperty("minorGarrisonContentHash").GetString());
        }
        Assert.True(CampaignExercises.Reconstruct(compactMinor).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceAxisGarrisonRemainder(minor).IsAccepted);
        var oldMinorCheckpoint = compactMinor.SourceAuthority.SerializeCheckpoint();
        var remainderResult = CampaignExercises.ActivateSourceAxisGarrisonRemainder(compactMinor);
        Assert.True(remainderResult.IsAccepted);
        var remainder = remainderResult.SuccessorSession!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(remainder.SourceAuthority!.CwFreeGunsWorld!.SerializeCanonical()),
            remainder.SourceAuthority.CwFreeGunsWorld.Hash());
        Console.WriteLine($"garrison remainder content hash: {remainder.SourceAuthority!.World.ContentIdentity.Hash}");
        var remainderCheckpointBytes = remainder.SourceAuthority!.SerializeCheckpoint();
        Assert.True(remainderCheckpointBytes.Length < 24 * 1024 * 1024,
            $"Legacy garrison remainder checkpoint was {remainderCheckpointBytes.Length} bytes.");
        Assert.Equal(oldMinorCheckpoint, compactMinor.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.AxisGarrisonRemainderCheckpointContractVersion, remainder.SourceAuthority.ProfileVersion);
        Assert.Equal(ContentSourceAxisGarrisonRemainderPackArtifact.ContentHash, remainder.SourceAuthority.World.ContentIdentity.Hash);
        Assert.Equal(minor.SourceAuthority.RandomState, remainder.SourceAuthority.RandomState);
        Assert.Equal(minor.SourceAuthority.World.PendingArrivals, remainder.SourceAuthority.World.PendingArrivals);
        Assert.Equal(minor.SourceAuthority.GroupedWorld!.ContentHash, remainder.SourceAuthority.GroupedWorld!.ContentHash);
        Assert.Equal(ExpectedGarrisonRemainderIds,
            ContentSourceAxisGarrisonRemainderPackArtifact.Units.Select(value => value.Id).Order(StringComparer.Ordinal));
        foreach (var unit in ContentSourceAxisGarrisonRemainderPackArtifact.Units)
        {
            var element = Assert.Single(remainder.SourceAuthority.World.World.Elements, value => value.ElementId == unit.Id);
            var sourceComponent = ContentSourceAxisGarrisonRemainderPackArtifact.Facts(unit.Id).Components.Single();
            Assert.Equal(unit.Location, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
            Assert.Equal(sourceComponent.ComponentId, element.Components[0].ComponentId);
            Assert.Equal(0, ContentSourceAxisGarrisonRemainderPackArtifact.StackAt(unit.Id, unit.Location));
            Assert.Single(remainder.SourceAuthority.World.World.Representations, value =>
                value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
                && value.CurrentLocationId == unit.Location && value.BoundElementIds.SequenceEqual([unit.Id]));
            Assert.Contains(remainder.SourceAuthority.Battlefield!.Entries, value => value.ElementId == unit.Id
                && value.AuthorityStatus == "source-axis-garrison-remainder-active");
        }
        var remainderContactWorld = CampaignSourcePlayableWorld.Create(remainder.SourceAuthority);
        var remainderCounters = CampaignSourcePolishArrivalContacts.EffectiveCounters(remainderContactWorld);
        var remainderAxisKnowledge = new CampaignContactKnowledge(remainder.SourceAuthority.Request.CampaignId, LandSide.Axis);
        var remainderCwKnowledge = new CampaignContactKnowledge(remainder.SourceAuthority.Request.CampaignId, LandSide.Commonwealth);
        // Both counters come from the admitted source world. The chart proves
        // Soluch and Matruh are separated without inferring a playable seam.
        var soluchAndIndian = remainderCounters.Where(value => value.RepresentationId is
            "axis-garrison.soluch-antitank-company" or "4-indian-division").ToArray();
        Assert.Equal(2, soluchAndIndian.Length);
        Assert.Empty(CampaignSourceInitialContacts.DeriveCounters(soluchAndIndian, 1, remainderAxisKnowledge));
        Assert.Empty(CampaignSourceInitialContacts.DeriveCounters(soluchAndIndian, 1, remainderCwKnowledge));
        Assert.Equal(6, ContentSourceAxisGarrisonRemainderPackArtifact.Units.Count);
        foreach (var removedId in ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds)
        {
            var observerId = "axis-garrison." + removedId;
            Assert.DoesNotContain(remainderCounters, value => value.RepresentationId == observerId);
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(remainderContactWorld, remainderAxisKnowledge, observerId));
        }
        foreach (var unit in ContentSourceAxisGarrisonRemainderPackArtifact.Units)
        {
            var id = "axis-garrison." + unit.Id;
            var counter = Assert.Single(remainderCounters, value => value.RepresentationId == id);
            Assert.Equal(CampaignContactObserverKind.IndependentCounter, counter.ObserverKind);
            Assert.Equal(id, counter.ObserverFormationId);
            Assert.Empty(CampaignIndependentContactObservationV2.Project(remainderContactWorld, remainderAxisKnowledge, id).Contacts);
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(remainderContactWorld, remainderCwKnowledge, id));
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(minorContactWorld, pairAxis, id));
        }
        Assert.Equal(1, ContentSourceAxisGarrisonRemainderPackArtifact.StackAt("soluch-artillery-regiment", "a4131"));
        Assert.Equal(1, ContentSourceAxisGarrisonRemainderPackArtifact.StackAt("giarabub-machine-gun", "c1015"));
        Assert.DoesNotContain(remainder.SourceAuthority.World.World.Elements, value => value.ElementId == "giarabub-motor-machine-gun");
        Assert.Contains(remainder.SourceAuthority.Battlefield!.Entries, value => value.ElementId == "giarabub-motor-machine-gun"
            && value.AuthorityStatus == "source-evidence");
        foreach (var id in ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds)
        {
            Assert.DoesNotContain(remainder.SourceAuthority.World.World.Elements, value => value.ElementId == id);
            Assert.DoesNotContain(remainder.SourceAuthority.World.World.Representations, value => value.BoundElementIds.Contains(id));
            var removedEntry = Assert.Single(remainder.SourceAuthority.Battlefield!.Entries, value => value.ElementId == id
                && value.AuthorityStatus == "source-land-only-aa-removed");
            Assert.DoesNotContain(CampaignSourcePlayableWorld.Create(remainder.SourceAuthority).UnresolvedAt(removedEntry.LocationId!),
                value => value.ElementId == id);
        }
        using (var contentDocument = JsonDocument.Parse(remainder.SourceAuthority.CwFreeGunsWorld!.GarrisonRemainderContent!.CanonicalBytes))
        {
            var removedIds = contentDocument.RootElement.GetProperty("removedAntiAirUnits").EnumerateArray()
                .Select(value => value.GetString()!).ToArray();
            Assert.Equal(ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds, removedIds);
            Assert.Equal("land:32.83(1)-italian-uu-removed-from-play", contentDocument.RootElement.GetProperty("antiAirTreatment").GetString());
        }
        Assert.True(CampaignExercises.Reconstruct(remainder).IsVerified);
        var remainderHistory = remainder.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var remainderEvent = JsonDocument.Parse(remainderHistory[^1]))
        {
            var worldHash = remainderEvent.RootElement.GetProperty("worldHash").GetString()!;
            var changedHash = worldHash[..^1] + (worldHash[^1] == '0' ? '1' : '0');
            remainderHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(remainderHistory[^1])
                .Replace(worldHash, changedHash, StringComparison.Ordinal));
        }
        var tamperedRemainder = new ExerciseSession(remainder.Pregame!, remainder.CurrentContacts,
            remainderHistory, remainder.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(tamperedRemainder).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceAxisGarrisonRemainder(remainder).IsAccepted);
        var compactRemainder = remainder;
        var compactRemainderBytes = compactRemainder.SourceAuthority!.SerializeCheckpoint();
        using (var remainderCheckpoint = JsonDocument.Parse(compactRemainderBytes))
        {
            Assert.True(compactRemainderBytes.Length < 16_384, $"Compact remainder checkpoint was {compactRemainderBytes.Length} bytes.");
            Assert.False(remainderCheckpoint.RootElement.TryGetProperty("garrisonRemainderContent", out _));
            Assert.Equal(compactRemainder.SourceAuthority.World.ContentIdentity.Hash,
                remainderCheckpoint.RootElement.GetProperty("garrisonRemainderContentHash").GetString());
        }
        Assert.True(CampaignExercises.Reconstruct(compactRemainder).IsVerified);
        var priorRemainderCheckpoint = remainder.SourceAuthority.SerializeCheckpoint();
        var priorAxisMotorization = remainder.SourceAuthority.AxisMotorization.Hash();
        var motorMgResult = CampaignExercises.ActivateSourceGiarabubMotorMachineGun(remainder);
        Assert.True(motorMgResult.IsAccepted);
        var motorMg = motorMgResult.SuccessorSession!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(motorMg.SourceAuthority!.CwFreeGunsWorld!.SerializeCanonical()),
            motorMg.SourceAuthority.CwFreeGunsWorld.Hash());
        Assert.Equal(priorRemainderCheckpoint, remainder.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.GiarabubMotorMachineGunCheckpointContractVersion, motorMg.SourceAuthority!.ProfileVersion);
        Assert.Equal(priorAxisMotorization, motorMg.SourceAuthority.AxisMotorization.Hash());
        Assert.Equal(remainder.SourceAuthority.RandomState, motorMg.SourceAuthority.RandomState);
        Assert.Equal(remainder.SourceAuthority.World.PendingArrivals, motorMg.SourceAuthority.World.PendingArrivals);
        var motorMgElement = Assert.Single(motorMg.SourceAuthority.World.World.Elements,
            value => value.ElementId == ContentSourceGiarabubMotorMachineGunArtifact.ElementId);
        Assert.Equal("c1014", motorMgElement.CurrentLocationId);
        Assert.Equal(1, Assert.Single(motorMgElement.Components).CurrentToe);
        Assert.Single(motorMg.SourceAuthority.World.World.Representations, value =>
            value.RepresentationId == "axis-garrison.giarabub-motor-machine-gun"
            && value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement);
        var motorMgContent = motorMg.SourceAuthority.CwFreeGunsWorld!.MotorMachineGunContent!;
        Console.WriteLine($"giarabub motor MG content hash: {motorMgContent.Identity.Hash}");
        Assert.Equal(ContentSourceGiarabubMotorMachineGunArtifact.ContentHash, motorMgContent.Identity.Hash);
        using (var document = JsonDocument.Parse(motorMgContent.CanonicalBytes))
        {
            var root = document.RootElement;
            Assert.Equal("8+", root.GetProperty("printedCapabilityPoints").GetString());
            Assert.Equal(JsonValueKind.Null, root.GetProperty("effectiveCapabilityPoints").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("mobilityId").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("motorPoolId").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("assignedMotorPoints").ValueKind);
        }
        var motorMgWorld = CampaignSourcePlayableWorld.Create(motorMg.SourceAuthority);
        Assert.Contains("giarabub-motor-mg-historical-motorization-priority-unresolved", motorMgWorld.ReadinessBlockers());
        Assert.Contains("giarabub-motor-mg-capability-and-mobility-unresolved", motorMgWorld.ReadinessBlockers());
        var motorMgCounter = Assert.Single(CampaignSourcePolishArrivalContacts.EffectiveCounters(motorMgWorld),
            value => value.RepresentationId == "axis-garrison.giarabub-motor-machine-gun");
        Assert.Equal(CampaignContactObserverKind.IndependentCounter, motorMgCounter.ObserverKind);
        Assert.Equal(motorMgCounter.RepresentationId, motorMgCounter.ObserverFormationId);
        var motorMgAxisKnowledge = new CampaignContactKnowledge(motorMg.SourceAuthority.Request.CampaignId, LandSide.Axis);
        var motorMgCwKnowledge = new CampaignContactKnowledge(motorMg.SourceAuthority.Request.CampaignId, LandSide.Commonwealth);
        Assert.Empty(CampaignIndependentContactObservationV2.Project(motorMgWorld, motorMgAxisKnowledge, motorMgCounter.RepresentationId).Contacts);
        Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(motorMgWorld, motorMgCwKnowledge, motorMgCounter.RepresentationId));
        Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(remainderContactWorld, motorMgAxisKnowledge, motorMgCounter.RepresentationId));
        var motorAndIndian = CampaignSourcePolishArrivalContacts.EffectiveCounters(motorMgWorld)
            .Where(value => value.RepresentationId is "axis-garrison.giarabub-motor-machine-gun" or "4-indian-division").ToArray();
        Assert.Equal(2, motorAndIndian.Length);
        Assert.Empty(CampaignSourceInitialContacts.DeriveCounters(motorAndIndian, 1, motorMgAxisKnowledge));
        // Exercise the verified Giarabub hexside using the real counters; this
        // hypothetical position is not an admitted campaign move.
        var adjacentIndian = motorAndIndian.Single(value => value.RepresentationId == "4-indian-division") with { LocationId = "c1015" };
        var motorContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters([motorMgCounter, adjacentIndian], 1, motorMgAxisKnowledge));
        Assert.Equal(CampaignContactObserverKind.IndependentCounter, motorContact.ObserverKind);
        Assert.Equal(motorMgCounter.RepresentationId, motorContact.ObserverFormationId);
        Assert.False(motorMgWorld.IsGameplayReady);
        Assert.False(CampaignExercises.StartSourceTurn(motorMg).IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(motorMg).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceGiarabubMotorMachineGun(motorMg).IsAccepted);
        var motorMgCheckpoint = motorMg.SourceAuthority.SerializeCheckpoint();
        Assert.True(motorMgCheckpoint.Length < 16_384, $"Compact motor MG checkpoint was {motorMgCheckpoint.Length} bytes.");
        using (var document = JsonDocument.Parse(motorMgCheckpoint))
            Assert.Equal(motorMgContent.Identity.Hash, document.RootElement.GetProperty("motorMachineGunContentHash").GetString());
        var cwFrontlineResult = CampaignExercises.ActivateSourceCwFrontline(motorMg);
        Assert.True(cwFrontlineResult.IsAccepted);
        var cwFrontline = cwFrontlineResult.SuccessorSession!;
        var cwAuthority = cwFrontline.SourceAuthority!;
        Assert.True(CampaignExercises.IsSourceCwFrontlineActivated(cwFrontline));
        Assert.Equal(motorMgCheckpoint, motorMg.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.CwFrontlineCheckpointContractVersion, cwAuthority.ProfileVersion);
        Assert.Equal(motorMg.SourceAuthority.CommonwealthMotorization.Hash(), cwAuthority.CommonwealthMotorization.Hash());
        Assert.Equal(motorMg.SourceAuthority.AxisMotorization.Hash(), cwAuthority.AxisMotorization.Hash());
        Assert.Equal(motorMg.SourceAuthority.RandomState, cwAuthority.RandomState);
        Assert.Equal(motorMg.SourceAuthority.World.PendingArrivals, cwAuthority.World.PendingArrivals);
        foreach (var unit in ContentSourceCwFrontlineArtifact.Units)
        {
            var element = Assert.Single(cwAuthority.World.World.Elements, value => value.ElementId == unit.Id);
            Assert.Equal(unit.Location, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
            Assert.Single(cwAuthority.World.World.Representations, value => value.RepresentationId == "cw-frontline." + unit.Id
                && value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement);
            Assert.Contains(cwAuthority.Battlefield!.Entries, value => value.ElementId == unit.Id
                && value.AuthorityStatus == "source-cw-frontline-active-initial-loads-unresolved");
        }
        var cwNativeWorld = cwAuthority.CwFreeGunsWorld!;
        var freshHashWorld = remainder.SourceAuthority.CwFreeGunsWorld!.ActivateGiarabubMotorMachineGun(
            remainder.SourceAuthority.AxisGarrisons!, remainder.SourceAuthority.Battlefield!);
        var canonicalWorldHash = CampaignPregameSourceCreation.Digest(freshHashWorld.SerializeCanonical());
        var concurrentWorldHashes = new string[8];
        Parallel.For(0, concurrentWorldHashes.Length, index => concurrentWorldHashes[index] = freshHashWorld.Hash());
        Assert.All(concurrentWorldHashes, hash => Assert.Equal(canonicalWorldHash, hash));
        var cwContent = cwNativeWorld.CwFrontlineContent!;
        var cwContactWorld = CampaignSourcePlayableWorld.Create(cwAuthority);
        var cwEffectiveCounters = CampaignSourcePolishArrivalContacts.EffectiveCounters(cwContactWorld);
        foreach (var (cwId, axisId) in new[]
        {
            ("cw-frontline.1-rtr", "axis-minor.scheferzen-garrison"),
            ("cw-frontline.11-hussars", "axis-minor.fort-maddalena-garrison"),
        })
        {
            var actualPair = cwEffectiveCounters.Where(value => value.RepresentationId == cwId || value.RepresentationId == axisId).ToArray();
            Assert.Equal(2, actualPair.Length);
            Assert.All(actualPair, value => Assert.Equal(CampaignContactObserverKind.IndependentCounter, value.ObserverKind));
            var cwKnowledge = new CampaignContactKnowledge(cwAuthority.Request.CampaignId, LandSide.Commonwealth);
            var axisKnowledge = new CampaignContactKnowledge(cwAuthority.Request.CampaignId, LandSide.Axis);
            var cwContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(actualPair, 1, cwKnowledge));
            var axisContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(actualPair, 1, axisKnowledge));
            Assert.Equal(cwId, cwContact.ObserverFormationId);
            Assert.Equal(axisId, axisContact.ObserverFormationId);
            var serialized = CampaignContactTransitionCodec.SerializeCanonical(cwContact);
            Assert.Equal(cwContact, CampaignContactTransitionCodec.DeserializeCanonical(serialized));
            Assert.Equal(serialized, CampaignContactTransitionCodec.SerializeCanonical(Assert.Single(
                CampaignSourceInitialContacts.DeriveCounters(actualPair, 1,
                    new CampaignContactKnowledge(cwAuthority.Request.CampaignId, LandSide.Commonwealth)))));
            cwKnowledge.Apply(cwContact);
            axisKnowledge.Apply(axisContact);
            var cwView = CampaignIndependentContactObservationV2.Project(cwContactWorld, cwKnowledge, cwId);
            Assert.Single(cwView.Contacts);
            Assert.DoesNotContain(axisId, System.Text.Encoding.UTF8.GetString(cwView.SerializeCanonical()));
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(cwContactWorld, axisKnowledge, cwId));
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(
                CampaignSourcePlayableWorld.Create(motorMg.SourceAuthority), cwKnowledge, cwId));
            Assert.Equal(cwContact.ContactHandle, Assert.Single(CampaignSourceInitialContacts.DeriveCounters(actualPair, 2, cwKnowledge)).ContactHandle);
        }
        Assert.Equal(ContentSourceCwFrontlineArtifact.ContentHash, cwContent.Identity.Hash);
        Assert.Equal(cwContent.Identity, ContentSourceCwFrontlineArtifact.Read(cwContent.CanonicalBytes).Identity);
        using (var document = JsonDocument.Parse(cwContent.CanonicalBytes))
            foreach (var unit in document.RootElement.GetProperty("units").EnumerateArray())
            {
                if (unit.GetProperty("elementId").GetString() == "11-hussars")
                    Assert.Equal(1, unit.GetProperty("chartArmorRating").GetInt32());
                Assert.Equal(JsonValueKind.Null, unit.GetProperty("initialCargo").ValueKind);
                Assert.Equal(JsonValueKind.Null, unit.GetProperty("parentFormationId").ValueKind);
            }
        Assert.Contains("cw-frontline-initial-loads-unresolved", CampaignSourcePlayableWorld.Create(cwAuthority).ReadinessBlockers());
        Assert.False(CampaignExercises.StartSourceTurn(cwFrontline).IsAccepted);
        Assert.False(CampaignExercises.ActivateSourceCwFrontline(cwFrontline).IsAccepted);
        var cwCheckpoint = cwAuthority.SerializeCheckpoint();
        Assert.True(cwCheckpoint.Length < 16_384, $"Compact CW frontline checkpoint was {cwCheckpoint.Length} bytes.");
        using (var document = JsonDocument.Parse(cwCheckpoint))
            Assert.Equal(cwContent.Identity.Hash, document.RootElement.GetProperty("cwFrontlineContentHash").GetString());
        Assert.True(CampaignExercises.Reconstruct(cwFrontline).IsVerified);
        var cwHistory = cwFrontline.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var document = JsonDocument.Parse(cwHistory[^1]))
        {
            var hash = document.RootElement.GetProperty("worldHash").GetString()!;
            var changed = hash[..^1] + (hash[^1] == '0' ? '1' : '0');
            cwHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(cwHistory[^1])
                .Replace(hash, changed, StringComparison.Ordinal));
        }
        Assert.False(CampaignExercises.Reconstruct(new ExerciseSession(cwFrontline.Pregame!, cwFrontline.CurrentContacts,
            cwHistory, cwFrontline.SourceAuthority)).IsVerified);
        var motorMgHistory = motorMg.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var document = JsonDocument.Parse(motorMgHistory[^1]))
        {
            var hash = document.RootElement.GetProperty("worldHash").GetString()!;
            var changed = hash[..^1] + (hash[^1] == '0' ? '1' : '0');
            motorMgHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(motorMgHistory[^1])
                .Replace(hash, changed, StringComparison.Ordinal));
        }
        Assert.False(CampaignExercises.Reconstruct(new ExerciseSession(motorMg.Pregame!, motorMg.CurrentContacts,
            motorMgHistory, motorMg.SourceAuthority)).IsVerified);
        var minorHistory = minor.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var eventDocument = JsonDocument.Parse(minorHistory[^1]))
        {
            var worldHash = eventDocument.RootElement.GetProperty("worldHash").GetString()!;
            var changedHash = worldHash[..^1] + (worldHash[^1] == '0' ? '1' : '0');
            minorHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(minorHistory[^1])
                .Replace(worldHash, changedHash, StringComparison.Ordinal));
        }
        var tamperedMinor = new ExerciseSession(minor.Pregame!, minor.CurrentContacts, minorHistory, minor.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(tamperedMinor).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceAxisMinorGarrisons(minor).IsAccepted);
        var minorFinish = CampaignExercises.Query(minor, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var minorFinished = CampaignExercises.Submit(minor,
            Submission(minor, minorFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(minorFinished).IsVerified);
        Assert.Equal(minor.SourceAuthority.World.ContentIdentity, minorFinished.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(ContentSourceFourthIndianPackArtifact.ContentHash, grouped.ContentHash);
        Assert.Equal(11, grouped.Groups.Single().Members.Count);
        var originalGroup = grouped.Groups.Single();
        var forgedMembers = originalGroup.Members.Select(value => value.ElementId == "25-field-artillery"
            ? value with { AdditionalComponents = Array.AsReadOnly(new[] { new CampaignGroupedComponent("extra-gun", 1) }) } : value);
        var forgedGroup = new CampaignGroupedFormation(originalGroup.GroupId, originalGroup.SideId, originalGroup.RootId,
            originalGroup.LocationId, originalGroup.StackValue, originalGroup.MotorPool, forgedMembers, originalGroup.DispositionConfirmed);
        var forgedWorld = CampaignGroupedWorld.Create(grouped.Prior, grouped.ContentHash, [forgedGroup]);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceFourthIndianDefinition.Validate(forgedWorld));
        var fieldArtillery = grouped.Groups.Single().Members.Single(value => value.ElementId == "25-field-artillery");
        Assert.Equal("3-7-inch-howitzer", fieldArtillery.ComponentType);
        Assert.Equal(6, fieldArtillery.ComponentToe);
        Assert.Equal(grouped.Prior.Initialization.CapabilityPointsExpended, fieldArtillery.CapabilityPointsExpended);
        Assert.Equal(11, indian.SourceAuthority.Battlefield!.Entries.Count(value => value.LocationId == "d3615" && value.AuthorityStatus == "source-fourth-indian-proposed"));
        Assert.Null(grouped.StackAt("commonwealth", "d3615", _ => null));
        Assert.Contains(grouped.Prior.PendingArrivals, value => value.GroupId == "graziani-7-indian-brigade");
        var priorityAction = CampaignSourceAuthorityState.FourthIndianMotorActionId("central-india-horse", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Axis)).RejectionReason);
        Assert.False(grouped.Groups.Single().DispositionConfirmed);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        var confirm = CampaignExercises.Query(indian, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "fourth-indian-motorization");
        Assert.Contains("confirm", confirm.ChoiceJson, StringComparison.Ordinal);
        var ownerDraft = System.Text.Encoding.UTF8.GetString(CampaignExercises.QuerySourceFourthIndianDraft(indian, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.Contains("provisional-source-default-alternatives-unavailable", ownerDraft, StringComparison.Ordinal);
        Assert.Contains("5-indian-brigade-hq", ownerDraft, StringComparison.Ordinal);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(indian,
            Submission(indian, confirm.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var confirmCommand = Submission(indian, confirm.ActionId, CampaignActionAudience.Commonwealth);
        indian = CampaignExercises.Submit(indian, confirmCommand).SuccessorSession!;
        Assert.True(indian.SourceAuthority!.GroupedWorld!.Groups.Single().DispositionConfirmed);
        Assert.Equal(5, indian.SourceAuthority.GroupedWorld.StackAt("commonwealth", "d3615", _ => null));
        Assert.Equal(11, indian.SourceAuthority.Battlefield!.Entries.Count(value => value.LocationId == "d3615" && value.AuthorityStatus == "source-fourth-indian-grouped"));
        Assert.Equal(34, indian.SourceAuthority.ProfileVersion);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(indian, confirmCommand).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        indian = CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(21, indian.SourceAuthority!.GroupedWorld!.RemainingMotorPoints("4-indian-division"));
        var footAction = CampaignSourceAuthorityState.FourthIndianMotorActionId("1-royal-fusiliers", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, footAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        var minorBeforePromotions = CampaignExercises.ActivateSourceAxisMinorGarrisons(indian).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.AdmitSourceCompactCheckpoint(minorBeforePromotions).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.ActivateSourceAxisGarrisonRemainder(minorBeforePromotions).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.ActivateSourceGiarabubMotorMachineGun(minorBeforePromotions).SuccessorSession!;
        var minorIdentity = minorBeforePromotions.SourceAuthority!.World.ContentIdentity;
        minorBeforePromotions = CampaignExercises.SubmitSourceMalettiHierarchy(minorBeforePromotions,
            Submission(minorBeforePromotions, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        var minorMaletti = CampaignExercises.PromoteSourceCertifiedMalettiWorld(minorBeforePromotions);
        Assert.True(minorMaletti.IsAccepted);
        minorBeforePromotions = minorMaletti.SuccessorSession!;
        minorBeforePromotions = CampaignExercises.AdmitSourceMatruhFormation(minorBeforePromotions).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.SubmitSourceMatruhFormation(minorBeforePromotions,
            Submission(minorBeforePromotions, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.SubmitSourceMatruhFormation(minorBeforePromotions,
            Submission(minorBeforePromotions, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var minorMatruh = CampaignExercises.PromoteSourceMatruhDetachedWorld(minorBeforePromotions);
        Assert.True(minorMatruh.IsAccepted);
        minorBeforePromotions = minorMatruh.SuccessorSession!;
        Assert.Equal(minorIdentity, minorBeforePromotions.SourceAuthority!.World.ContentIdentity);
        Assert.All(ContentSourceAxisMinorGarrisonsPackArtifact.Units, unit =>
            Assert.Contains(minorBeforePromotions.SourceAuthority.World.World.Elements, element => element.ElementId == unit.Id));
        Assert.True(CampaignExercises.Reconstruct(minorBeforePromotions).IsVerified);
        var indianWithoutMalettiDetachment = indian;
        indian = CampaignExercises.SubmitSourceMalettiHierarchy(indian,
            Submission(indian, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        indian = CampaignExercises.PromoteSourceCertifiedMalettiWorld(indian).SuccessorSession!;
        Assert.Equal(21, indian.SourceAuthority!.GroupedWorld!.RemainingMotorPoints("4-indian-division"));
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        var matruhBeforeDetach = CampaignExercises.AdmitSourceMatruhFormation(indian).SuccessorSession!;
        matruhBeforeDetach = CampaignExercises.SubmitSourceMatruhFormation(matruhBeforeDetach,
            Submission(matruhBeforeDetach, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var matruhAfterDetach = matruhBeforeDetach;
        var groupedResult = CampaignExercises.AdmitSourceMatruhGroupedWorld(matruhBeforeDetach);
        Assert.True(groupedResult.IsAccepted);
        matruhBeforeDetach = groupedResult.SuccessorSession!;
        Assert.Equal(2, matruhBeforeDetach.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(0, matruhBeforeDetach.SourceAuthority.GroupedWorld.StackAt("commonwealth", "d3714", _ => null));
        Assert.Equal(6, matruhBeforeDetach.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "matruh-garrison").Members.Count);
        Assert.Equal(35, matruhBeforeDetach.SourceAuthority.ProfileVersion);
        var activeMaletti = CampaignExercises.SubmitSourceAxisPriority(matruhBeforeDetach,
            Submission(matruhBeforeDetach, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        activeMaletti = CampaignExercises.AdmitSourceMalettiGroupedWorld(activeMaletti).SuccessorSession!;
        Assert.Equal(3, activeMaletti.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(30, activeMaletti.SourceAuthority.GroupedWorld.RemainingMotorPoints("maletti-division"));
        Assert.Equal(5, activeMaletti.SourceAuthority.GroupedWorld.ExternalReservations.Single(value => value.GroupId == "maletti-division").AssignedMotorPoints);
        Assert.Equal(36, activeMaletti.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(activeMaletti).IsVerified);
        var libyanAdmission = CampaignExercises.AdmitSourceLibyanGroupedWorld(activeMaletti);
        Assert.True(libyanAdmission.IsAccepted);
        var libyanProposed = libyanAdmission.SuccessorSession!;
        Assert.Equal(5, libyanProposed.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(22, libyanProposed.SourceAuthority.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-libyan-proposed"));
        Assert.Equal(37, libyanProposed.SourceAuthority.ProfileVersion);
        var secondArtillery = libyanProposed.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "2-libyan-division")
            .Members.Single(value => value.ElementId == "2-libyan-artillery");
        Assert.Equal("75-27-gun", secondArtillery.ComponentType);
        Assert.Equal(9, secondArtillery.ComponentToe);
        Assert.Contains(secondArtillery.AdditionalComponents!, value => value.ComponentType == "100-17-howitzer" && value.Toe == 3);
        var axisChoices = CampaignExercises.Query(libyanProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "libyan-disposition").ToArray();
        Assert.Equal(2, axisChoices.Length);
        Assert.DoesNotContain(CampaignExercises.Query(libyanProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "libyan-disposition");
        var firstChoice = axisChoices.Single(value => value.ChoiceJson.Contains("1-libyan-division", StringComparison.Ordinal));
        var secondChoice = axisChoices.Single(value => value.ChoiceJson.Contains("2-libyan-division", StringComparison.Ordinal));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(libyanProposed,
            Submission(libyanProposed, firstChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var firstCommand = Submission(libyanProposed, firstChoice.ActionId, CampaignActionAudience.Axis);
        var firstConfirmed = CampaignExercises.Submit(libyanProposed, firstCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(firstConfirmed, firstCommand).RejectionReason);
        var secondAfter = CampaignExercises.Query(firstConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "libyan-disposition");
        firstConfirmed = CampaignExercises.Submit(firstConfirmed, Submission(firstConfirmed, secondAfter.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(22, firstConfirmed.SourceAuthority!.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-libyan-grouped"));
        Assert.Equal(5, firstConfirmed.SourceAuthority.GroupedWorld!.StackAt("axis", "c4020", _ => null));
        Assert.Equal(5, firstConfirmed.SourceAuthority.GroupedWorld.StackAt("axis", "c3920", _ => null));
        Assert.True(CampaignExercises.Reconstruct(firstConfirmed).IsVerified);
        var italianBasisFourth = ContentSourceFourthIndianPackArtifact.Create(firstConfirmed.SourceAuthority.CwFreeGunsWorld!.Content);
        var italianBasisMatruh = ContentSourceMatruhGroupedPackArtifact.Create(italianBasisFourth);
        var italianBasisMaletti = ContentSourceMalettiGroupedPackArtifact.Create(italianBasisMatruh);
        var italianBasisLibyan = ContentSourceLibyanGroupedPackArtifact.Create(italianBasisMaletti);
        var italianContent = ContentSourceCireneMarmaricaGroupedPackArtifact.Create(italianBasisLibyan);
        Assert.Equal(ContentSourceCireneMarmaricaGroupedPackArtifact.ContentHash, italianContent.Hash());
        var italianAdmission = CampaignExercises.AdmitSourceItalianDivisionsGroupedWorld(firstConfirmed);
        Assert.True(italianAdmission.IsAccepted);
        var italianProposed = italianAdmission.SuccessorSession!;
        Assert.Equal(7, italianProposed.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(29, italianProposed.SourceAuthority.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-italian-division-proposed"));
        Assert.Equal(39, italianProposed.SourceAuthority.ProfileVersion);
        var italianChoices = CampaignExercises.Query(italianProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "italian-division-disposition").ToArray();
        Assert.Equal(2, italianChoices.Length);
        Assert.DoesNotContain(CampaignExercises.Query(italianProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "italian-division-disposition");
        var cireneChoice = italianChoices.Single(value => value.ChoiceJson.Contains("cirene-division", StringComparison.Ordinal));
        var italianCommand = Submission(italianProposed, cireneChoice.ActionId, CampaignActionAudience.Axis);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(italianProposed,
            Submission(italianProposed, cireneChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var italianConfirmed = CampaignExercises.Submit(italianProposed, italianCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(italianConfirmed, italianCommand).RejectionReason);
        var marmaricaChoice = CampaignExercises.Query(italianConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "italian-division-disposition");
        italianConfirmed = CampaignExercises.Submit(italianConfirmed, Submission(italianConfirmed, marmaricaChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(29, italianConfirmed.SourceAuthority!.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-italian-division-grouped"));
        Assert.Equal(5, italianConfirmed.SourceAuthority.GroupedWorld!.StackAt("axis", "c4120", _ => null));
        Assert.Equal(5, italianConfirmed.SourceAuthority.GroupedWorld.StackAt("axis", "c3918", _ => null));
        Assert.True(CampaignExercises.Reconstruct(italianConfirmed).IsVerified);
        var seventhDirect = italianConfirmed.SourceAuthority.AdmitSeventhArmoredGroupedWorld();
        Assert.NotNull(seventhDirect.GroupedWorld);
        var seventhResult = CampaignExercises.AdmitSourceSeventhArmoredGroupedWorld(italianConfirmed);
        Assert.True(seventhResult.IsAccepted);
        var seventhProposed = seventhResult.SuccessorSession!;
        var seventh = seventhProposed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division");
        Assert.Equal(7, seventh.Members.Count); Assert.Null(seventh.StackValue); Assert.False(seventh.DispositionConfirmed);
        Assert.Equal(10, seventh.MotorPool);
        Assert.Equal(40, seventhProposed.SourceAuthority!.ProfileVersion);
        var seventhChoice = CampaignExercises.Query(seventhProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "seventh-armored-disposition");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(seventhProposed,
            Submission(seventhProposed, seventhChoice.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var seventhCommand = Submission(seventhProposed, seventhChoice.ActionId, CampaignActionAudience.Commonwealth);
        var seventhConfirmedResult = CampaignExercises.Submit(seventhProposed, seventhCommand);
        Assert.True(seventhConfirmedResult.IsAccepted);
        var seventhConfirmed = seventhConfirmedResult.SuccessorSession!;
        Assert.Equal(5, seventhConfirmed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division").StackValue);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(seventhConfirmed, seventhCommand).RejectionReason);
        Assert.Equal(italianConfirmed.SourceAuthority.World.PendingArrivals, seventhConfirmed.SourceAuthority.World.PendingArrivals);
        Assert.True(CampaignExercises.Reconstruct(seventhConfirmed).IsVerified);
        var legacySeventhCheckpoint = seventhConfirmed.SourceAuthority.SerializeCheckpoint();
        var correctedSeventh = CampaignExercises.CorrectSourceSeventhShell(seventhConfirmed).SuccessorSession!;
        Assert.Equal(3, correctedSeventh.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division").StackValue);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedLegacyCheckpointContractVersion, correctedSeventh.SourceAuthority.ProfileVersion);
        Assert.Equal(legacySeventhCheckpoint, seventhConfirmed.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(seventhConfirmed.SourceAuthority.RandomState, correctedSeventh.SourceAuthority.RandomState);
        Assert.Contains("common-charts-9.4-division-shell-stacks-three", System.Text.Encoding.UTF8.GetString(
            CampaignExercises.QuerySourceSeventhArmoredDraft(correctedSeventh, CampaignActionAudience.Commonwealth).VisibleBytes));
        Assert.True(CampaignExercises.Reconstruct(correctedSeventh).IsVerified);
        Assert.False(CampaignExercises.CorrectSourceSeventhShell(correctedSeventh).IsAccepted);
        var compactBeforeShell = CampaignExercises.AdmitSourceCompactCheckpoint(seventhConfirmed).SuccessorSession!;
        var compactAxisFinish = CampaignExercises.Query(compactBeforeShell, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var compactAxisFinished = CampaignExercises.Submit(compactBeforeShell,
            Submission(compactBeforeShell, compactAxisFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var compactCwFinish = CampaignExercises.Query(compactAxisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var compactBothFinished = CampaignExercises.Submit(compactAxisFinished,
            Submission(compactAxisFinished, compactCwFinish.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var compactCorrected = CampaignExercises.CorrectSourceSeventhShell(compactBothFinished).SuccessorSession!;
        Assert.Equal(1, compactCorrected.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedFinalizedCompactCheckpointContractVersion, compactCorrected.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(compactCorrected).IsVerified);
        var compactAxisReopen = CampaignExercises.Query(compactCorrected, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var compactReopened = CampaignExercises.Submit(compactCorrected,
            Submission(compactCorrected, compactAxisReopen.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(0, compactReopened.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedFinalizedCompactCheckpointContractVersion, compactReopened.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(compactReopened).IsVerified);
        var sixteenthBasis = ContentSourceSeventhArmoredGroupedPackArtifact.Create(italianContent);
        var sixteenthContent = ContentSourceSixteenthBrigadeGroupedPackArtifact.Create(sixteenthBasis);
        Assert.Equal(ContentSourceSixteenthBrigadeGroupedPackArtifact.ContentHash, sixteenthContent.Hash());
        var sixteenthResult = CampaignExercises.AdmitSourceSixteenthBrigadeGroupedWorld(seventhConfirmed);
        Assert.True(sixteenthResult.IsAccepted);
        var sixteenthProposed = sixteenthResult.SuccessorSession!;
        var sourceWorld = sixteenthProposed.SourceAuthority!.GroupedWorld!;
        var sixteenth = sourceWorld.Groups.Single(value => value.GroupId == "16-infantry-brigade");
        Assert.Equal(4, sixteenth.Members.Count); Assert.Null(sixteenth.StackValue); Assert.True(sixteenth.UsesRetainedMembers);
        Assert.Equal(4, sourceWorld.Prior.World.Elements.Count(value => sixteenth.Members.Any(member => member.ElementId == value.ElementId)));
        Assert.Single(sourceWorld.OccupantsAt("e1829"), value => value.Formation?.GroupId == sixteenth.GroupId);
        var sixteenthChoice = CampaignExercises.Query(sixteenthProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "sixteenth-brigade-disposition");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(sixteenthProposed,
            Submission(sixteenthProposed, sixteenthChoice.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var sixteenthCommand = Submission(sixteenthProposed, sixteenthChoice.ActionId, CampaignActionAudience.Commonwealth);
        var sixteenthConfirmed = CampaignExercises.Submit(sixteenthProposed, sixteenthCommand).SuccessorSession!;
        Assert.Equal(2, sixteenthConfirmed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == sixteenth.GroupId).StackValue);
        Assert.Equal(41, sixteenthConfirmed.SourceAuthority.ProfileVersion);
        var playableWorld = CampaignSourcePlayableWorld.Create(sixteenthConfirmed.SourceAuthority);
        var mapBoundWorld = CampaignSourcePlayableWorld.Create(sixteenthConfirmed.SourceAuthority, mapIdentityBound: true);
        var cdWorld = CampaignSourcePlayableWorld.Create(sixteenthConfirmed.SourceAuthority, mapIdentityBound: true, cdSeamBound: true);
        Assert.True(mapBoundWorld.MapIdentityBound);
        Assert.True(mapBoundWorld.WithWorld(mapBoundWorld.GroupedWorld, mapBoundWorld.Battlefield).MapIdentityBound);
        Assert.False(playableWorld.MapIdentityBound);
        // Test the wire contract on the retained historical world without
        // claiming that the still-gated campaign can start yet.
        var turnConstructor = typeof(CampaignSourceTurnAuthority).GetConstructors(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Single();
        CampaignSourceTurnAuthority ContractTurn(CampaignSourcePlayableWorld world, LandSequencePosition? testPosition = null,
            int? organizationMask = null, CampaignSourceTrainingLedger? training = null,
            CampaignSourceGt1AxisArrival? axisArrival = null, LandSide? firstSide = null,
            (int GameTurn, int OperationStage)? paidStage = null)
        {
            var args = turnConstructor.GetParameters().Select(parameter => parameter.Name == "position" && testPosition is not null
                ? testPosition : parameter.Name == "voluntaryOrganizationDeclinedMask" && organizationMask is not null
                ? organizationMask : parameter.Name == "training" && training is not null ? training
                : parameter.Name == "axisArrival" && axisArrival is not null ? axisArrival
                : parameter.Name == "fourthIndianPaidStage" && paidStage is not null ? paidStage
                : parameter.Name == "stageOneFirstSide" && firstSide is not null ? firstSide : parameter.HasDefaultValue
                ? parameter.DefaultValue : parameter.Name switch
                {
                    "world" => world,
                    "stateVersion" => world.Source.StateVersion + 1,
                    "position" => Cna1979LandSequenceV4.CreateTurn(1)[0],
                    "randomState" => world.RandomState,
                    "initiative" => null,
                    _ => throw new InvalidOperationException("Unexpected source-turn constructor parameter."),
                }).ToArray();
            return (CampaignSourceTurnAuthority)turnConstructor.Invoke(args);
        }
        var mapTurn = ContractTurn(mapBoundWorld);
        var cdTurn = ContractTurn(cdWorld);
        Assert.Equal(3, new ExerciseCheckpoint(cdTurn).ContractVersion);
        Assert.True(cdWorld.WithWorld(cdWorld.GroupedWorld, cdWorld.Battlefield).CdSeamBound);
        using (var cdCheckpoint = JsonDocument.Parse(cdTurn.SerializeCheckpoint()))
        {
            Assert.Equal(3, cdCheckpoint.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(Cna1979HistoricalMap.CdSeamPolicyHash,
                cdCheckpoint.RootElement.GetProperty("historicalMap").GetProperty("cdSeamPolicyHash").GetString());
        }
        var legacyTurn = ContractTurn(playableWorld);
        var turnContacts = sixteenthConfirmed.CurrentContacts.AdvancePregameSource(mapTurn.Source.StateVersion);
        var turnSession = new ExerciseSession(mapTurn.Source.Pregame, turnContacts,
            sixteenthConfirmed.PregameHistory, mapTurn.Source, mapTurn);
        var initiativeResult = CampaignExercises.ResolveSourceInitiative(turnSession);
        Assert.True(initiativeResult.IsAccepted);
        Assert.Equal(initiativeResult.SuccessorSession!.SourceTurn!.Position.PositionId,
            initiativeResult.Evidence!.Receipt.ResultingPositionId);
        Assert.Equal(CampaignSourceTurnAuthority.InitiativeActionId, initiativeResult.Evidence.Receipt.ActionId);
        var organizationCut = Cna1979LandSequenceV4.CreateTurn(1).Single(value =>
            value.OperationStage == 1 && value.PhaseId == LandPhaseIds.Organization);
        var interrupted = CampaignSourceTrainingLedger.CreateSixthAustralian().Interrupt();
        var waitingOrganization = ContractTurn(mapBoundWorld, organizationCut, 1, interrupted);
        Assert.Throws<InvalidOperationException>(() => waitingOrganization.CompleteGt1Organization());
        Assert.Throws<InvalidOperationException>(() => ContractTurn(mapBoundWorld, organizationCut, 3).CompleteGt1Organization());
        var readyOrganization = ContractTurn(mapBoundWorld, organizationCut, 3, interrupted);
        var completedOrganization = readyOrganization.CompleteGt1Organization();
        Assert.Equal(Cna1979LandSequenceV4.GetNext(organizationCut), completedOrganization.Position);
        Assert.Same(readyOrganization.Training, completedOrganization.Training);
        Assert.Equal(readyOrganization.RandomState, completedOrganization.RandomState);
        Assert.Same(readyOrganization.World, completedOrganization.World);
        Assert.Equal(readyOrganization.StateVersion + 1, completedOrganization.StateVersion);
        Assert.Throws<InvalidOperationException>(() => completedOrganization.CompleteGt1Organization());
        // Exercise the real source session transition on retained historical
        // setup without claiming that the campaign-start gate has opened.
        var organizationContacts = sixteenthConfirmed.CurrentContacts.AdvancePregameSource(readyOrganization.Source.StateVersion);
        var organizationSession = new ExerciseSession(readyOrganization.Source.Pregame, organizationContacts,
            sixteenthConfirmed.PregameHistory, readyOrganization.Source, readyOrganization);
        var organizationResult = CampaignExercises.CompleteSourceGt1Organization(organizationSession);
        Assert.True(organizationResult.IsAccepted);
        Assert.Equal(completedOrganization.Position.PositionId, organizationResult.Evidence!.Receipt.ResultingPositionId);
        Assert.True(CampaignExercises.IsSourceGt1OrganizationCompleted(organizationResult.SuccessorSession!));
        Assert.Equal(completedOrganization.Position, organizationResult.SuccessorSession!.SourceTurn!.Position);
        var organizationCommand = CampaignHostAdapter.SourceTurnSystemCommand(organizationSession, organizationResult);
        var organizationStep = new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(organizationCommand), organizationResult.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(organizationResult.Evidence.Receipt),
            organizationResult.Evidence.SnapshotCheckpoint, CampaignHostAcceptedStep.SourceGt1OrganizationCompleteKind);
        Assert.Equal(CampaignHostAcceptedStep.SourceGt1OrganizationCompleteContractVersion, organizationStep.ContractVersion);
        using (var organizationEvent = JsonDocument.Parse(Assert.Single(organizationResult.Evidence!.EventRecords)))
        {
            Assert.Equal("source-gt1-organization-completed", organizationEvent.RootElement.GetProperty("eventType").GetString());
            Assert.Equal(CampaignSourceTurnAuthority.CompleteGt1OrganizationActionId, organizationEvent.RootElement.GetProperty("actionId").GetString());
        }
        Assert.Equal(2, new ExerciseCheckpoint(mapTurn).ContractVersion);
        Assert.Equal(1, new ExerciseCheckpoint(legacyTurn).ContractVersion);
        using (var boundCheckpoint = System.Text.Json.JsonDocument.Parse(mapTurn.SerializeCheckpoint()))
        using (var legacyCheckpoint = System.Text.Json.JsonDocument.Parse(legacyTurn.SerializeCheckpoint()))
        {
            Assert.Equal(2, boundCheckpoint.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(Cna1979HistoricalMap.HexHash, boundCheckpoint.RootElement.GetProperty("historicalMap").GetProperty("hexSha256").GetString());
            Assert.Equal(Cna1979HistoricalMap.EdgeHash, boundCheckpoint.RootElement.GetProperty("historicalMap").GetProperty("hexsideSha256").GetString());
            Assert.Equal(1, legacyCheckpoint.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.False(legacyCheckpoint.RootElement.TryGetProperty("historicalMap", out _));
        }
        using (var boundEvent = System.Text.Json.JsonDocument.Parse(mapTurn.SerializeEvent("source-turn-started",
                   CampaignSourceTurnAuthority.StartActionId, mapBoundWorld.Source.StateVersion,
                   mapBoundWorld.Source.SerializeCheckpoint())))
        {
            Assert.Equal(2, boundEvent.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(CampaignPregameSourceCreation.Digest(mapTurn.SerializeCheckpoint()),
                boundEvent.RootElement.GetProperty("checkpointHash").GetString());
        }
        using (var legacyEvent = System.Text.Json.JsonDocument.Parse(legacyTurn.SerializeEvent("source-turn-started",
                   CampaignSourceTurnAuthority.StartActionId, playableWorld.Source.StateVersion,
                   playableWorld.Source.SerializeCheckpoint())))
            Assert.Equal(1, legacyEvent.RootElement.GetProperty("contractVersion").GetInt32());
        Assert.True(mapTurn.ResolveInitiative().World.MapIdentityBound);
        Assert.Same(sixteenthConfirmed.SourceAuthority.GroupedWorld, playableWorld.GroupedWorld);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.RandomState, playableWorld.RandomState);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.World.PendingArrivals, playableWorld.World8.PendingArrivals);
        foreach (var entry in playableWorld.Battlefield.Entries.Where(value => value.LocationId is { Length: 5 }
                     && value.LocationId[0] is >= 'a' and <= 'e' && value.LocationId.Skip(1).All(char.IsAsciiDigit)))
            Assert.NotNull(Cna1979HistoricalMap.Instance.HexAt(entry.LocationId!));
        foreach (var location in Cna1979GrazianiSourceFacts.CairoEntryArea.LocationIds)
            Assert.Equal(Cna1979SourceContentVocabulary.MajorCityTerrainId, playableWorld.KnownTerrainAt(location));
        var polishArrivalPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        Assert.Contains("e1829", CampaignSourcePolishArrival.ValidDestinations(playableWorld, polishArrivalPosition));
        Assert.Null(playableWorld.KnownTerrainAt("e1932"));
        Assert.Single(playableWorld.OccupantsAt("e1829"), value => value.Formation?.GroupId == sixteenth.GroupId);
        Assert.DoesNotContain(playableWorld.UnresolvedAt("e1829"), value => sixteenth.Members.Any(member => member.ElementId == value.ElementId));
        Assert.False(playableWorld.IsGameplayReady);
        Assert.Contains("battlefield-incomplete", playableWorld.ReadinessBlockers());
        Assert.Contains("control-incomplete", playableWorld.ReadinessBlockers());
        Assert.Contains("map-incomplete", playableWorld.ReadinessBlockers());
        Assert.Same(playableWorld.GroupedWorld, playableWorld.WithWorld(playableWorld.GroupedWorld, playableWorld.Battlefield).GroupedWorld);
        var sourceSupply = playableWorld.Source.CommonwealthSupplyWorld;
        CampaignSourceFourthIndianRoad.RequirePinnedSource(playableWorld);
        Assert.Equal(new CapabilityPointAmount(1, 1), CampaignSourceFourthIndianRoad.Cost(false));
        Assert.Equal(new CapabilityPointAmount(1, 2), CampaignSourceFourthIndianRoad.Cost(true));
        Assert.Equal(new CapabilityPointAmount(1, 1), CampaignSourceFourthIndianRoad.SupplyTraceCost);
        var paidSupply = sourceSupply.SpendFuel("cw-matruh-1", "d3714", 2);
        Assert.Equal(60, Assert.Single(sourceSupply.Units, value => value.Id == "cw-matruh-1").Fuel);
        Assert.Equal(58, Assert.Single(paidSupply.Units, value => value.Id == "cw-matruh-1").Fuel);
        Assert.Throws<InvalidOperationException>(() => sourceSupply.SpendFuel("cw-matruh-1", "d3615", 2));
        Assert.Throws<InvalidOperationException>(() => sourceSupply.SpendFuel("cw-matruh-1", "d3714", 61));
        Assert.Throws<InvalidOperationException>(() => sourceSupply.SpendFuel("not-a-counter", "d3714", 2));
        var movementCatalog = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.ActorRole == LandActorRole.FirstActingSide && value.SegmentId == LandSegmentIds.Movement);
        var movementPosition = new LandSequencePosition(movementCatalog.ContractVersion, movementCatalog.PositionId,
            movementCatalog.GameTurn, movementCatalog.OperationStage, movementCatalog.StageId, movementCatalog.PhaseId,
            movementCatalog.SegmentId, movementCatalog.StepId, movementCatalog.ActorRole, LandSide.Commonwealth, movementCatalog.Sources);
        var season = Cna1979Weather.GetSeason(1);
        var normalRoll = Enumerable.Range(11, 56).First(value => value % 10 is >= 1 and <= 6
            && value / 10 is >= 1 and <= 6 && Cna1979Weather.GetKind(season, value) == WeatherKind.Normal);
        var normalWeather = new CampaignOperationStageWeather(1, 1, 1, LandSide.Axis, season,
            normalRoll / 10, normalRoll % 10, WeatherKind.Normal, WeatherScope.None, null, [], 0, 0, 0);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceFourthIndianMovement.Apply(playableWorld,
            sourceSupply, null, movementPosition, normalWeather, "cw-matruh-1"));
        Assert.Equal(60, Assert.Single(sourceSupply.Units, value => value.Id == "cw-matruh-1").Fuel);
        // Exercise the source-bound mutation and contact projection directly;
        // source readiness still prevents this from becoming a playable move.
        var fourth = Assert.Single(playableWorld.GroupedWorld.Groups,
            value => value.GroupId == CampaignSourceFourthIndianMovement.GroupId);
        var fourthCosts = fourth.Members.ToDictionary(value => value.ElementId,
            value => new CapabilityPointAmount(1, 1), StringComparer.Ordinal);
        var movedFourth = playableWorld.GroupedWorld.MoveFormation(fourth.GroupId, "d3715", fourthCosts, 5, 1, 1);
        var movedBattlefield = playableWorld.Battlefield.MoveFormation(playableWorld.GroupedWorld, movedFourth, fourth.GroupId);
        var movedWorld = playableWorld.WithWorld(movedFourth, movedBattlefield);
        Assert.True(mapBoundWorld.WithWorld(movedFourth, movedBattlefield).MapIdentityBound);
        var unknownMove = playableWorld.GroupedWorld.MoveFormation(fourth.GroupId, "c9999", fourthCosts, 5, 1, 1);
        var unknownBattlefield = playableWorld.Battlefield.MoveFormation(playableWorld.GroupedWorld, unknownMove, fourth.GroupId);
        Assert.Contains(unknownBattlefield.Entries, entry => entry.LocationId == "c9999");
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePlayableWorld.RequireKnownMapLocations(
            unknownBattlefield.Entries.Select(entry => entry.LocationId)));
        Assert.Throws<InvalidOperationException>(() => mapBoundWorld.WithWorld(unknownMove, unknownBattlefield));
        Assert.All(fourth.Members, member => Assert.Equal("d3715",
            Assert.Single(movedWorld.Battlefield.Entries, entry => entry.ElementId == member.ElementId).LocationId));
        Assert.All(fourth.Members, member => Assert.Equal("d3615",
            Assert.Single(playableWorld.Battlefield.Entries, entry => entry.ElementId == member.ElementId).LocationId));
        var polishPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePolishArrival.Apply(playableWorld,
            polishPosition, LandSide.Commonwealth, "e1730"));
        Assert.Equal(sixteenthConfirmed.SourceAuthority.World.PendingArrivals, playableWorld.World8.PendingArrivals);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.RandomState, playableWorld.RandomState);
        var polishGrouped = CampaignSourcePolishArrival.Materialize(playableWorld.GroupedWorld, playableWorld.Source.Content, "e1829");
        var polishBattlefield = playableWorld.Battlefield.ArriveRetainedFormation(playableWorld.GroupedWorld,
            polishGrouped, CampaignSourcePolishArrival.GroupId, "e1829");
        var polishSuccessor = playableWorld.WithWorld(polishGrouped, polishBattlefield);
        var mapBoundPolishSuccessor = mapBoundWorld.WithWorld(polishGrouped, polishBattlefield);
        Assert.True(mapBoundPolishSuccessor.MapIdentityBound);
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(mapBoundWorld, mapBoundPolishSuccessor,
            mapBoundWorld.Source.StateVersion + 1, new CampaignContactKnowledge(mapBoundWorld.Source.Request.CampaignId, LandSide.Axis)));
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(mapBoundWorld, mapBoundPolishSuccessor,
            mapBoundWorld.Source.StateVersion + 1, new CampaignContactKnowledge(mapBoundWorld.Source.Request.CampaignId, LandSide.Commonwealth)));
        Assert.DoesNotContain(polishSuccessor.World8.PendingArrivals, value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.DoesNotContain(polishSuccessor.Battlefield.Pending, value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.Contains(polishSuccessor.World8.PendingArrivals, value => value.GroupId == Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId);
        Assert.Equal(8, polishSuccessor.World8.World.Elements.Count(value => Cna1979GrazianiSourceFacts.PolishBrigadeArrival.ElementIds.Contains(value.ElementId)));
        Assert.Equal(playableWorld.RandomState, polishSuccessor.RandomState);
        Assert.False(polishSuccessor.IsGameplayReady);
        Assert.Contains(playableWorld.World8.PendingArrivals, value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(playableWorld, polishSuccessor,
            playableWorld.Source.StateVersion + 1, new CampaignContactKnowledge(playableWorld.Source.Request.CampaignId, LandSide.Axis)));
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(playableWorld, polishSuccessor,
            playableWorld.Source.StateVersion + 1, new CampaignContactKnowledge(playableWorld.Source.Request.CampaignId, LandSide.Commonwealth)));
        var begunEarlyPlan = CampaignExercises.SubmitSourceSeptemberSupply(sixteenthConfirmed,
            Submission(sixteenthConfirmed, CampaignSourceAuthorityState.SeptemberSupplyActionId("begin", 0, 0, 0, 0), CampaignActionAudience.Axis)).SuccessorSession!;
        var rolledEarlyPlan = CampaignExercises.SubmitSourceSeptemberSupply(begunEarlyPlan,
            Submission(begunEarlyPlan, CampaignSourceAuthorityState.SeptemberSupplyActionId("roll", 3, 0, 0, 0), CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Contains("axis-prestart-supply-plan-incomplete", CampaignSourcePlayableWorld.Create(rolledEarlyPlan.SourceAuthority!).ReadinessBlockers());
        var finishedEarlyPlan = CampaignExercises.SubmitSourceSeptemberSupply(rolledEarlyPlan,
            Submission(rolledEarlyPlan, CampaignSourceAuthorityState.SeptemberSupplyActionId("finish", 3, 0, 0, 0), CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.DoesNotContain("axis-prestart-supply-plan-incomplete", CampaignSourcePlayableWorld.Create(finishedEarlyPlan.SourceAuthority!).ReadinessBlockers());
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(sixteenthConfirmed, sixteenthCommand).RejectionReason);
        Assert.Equal(seventhConfirmed.SourceAuthority.World.PendingArrivals, sixteenthConfirmed.SourceAuthority.World.PendingArrivals);
        Assert.True(CampaignExercises.Reconstruct(sixteenthConfirmed).IsVerified);
        var sixteenthHistory = sixteenthConfirmed.PregameHistory.Select(value => value.ToArray()).ToArray();
        var sixteenthEvent = System.Text.Encoding.UTF8.GetString(sixteenthHistory[^1]);
        using (var eventDocument = System.Text.Json.JsonDocument.Parse(sixteenthHistory[^1]))
        {
            var worldHash = eventDocument.RootElement.GetProperty("worldHash").GetString()!;
            var alteredHash = worldHash[..^1] + (worldHash[^1] == '0' ? '1' : '0');
            sixteenthHistory[^1] = System.Text.Encoding.UTF8.GetBytes(sixteenthEvent.Replace(worldHash, alteredHash, StringComparison.Ordinal));
        }
        var tamperedSixteenth = new ExerciseSession(sixteenthConfirmed.Pregame!, sixteenthConfirmed.CurrentContacts,
            sixteenthHistory, sixteenthConfirmed.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(tamperedSixteenth).IsVerified);
        var catanzaroResult = CampaignExercises.AdmitSourceCatanzaroGroupedWorld(sixteenthConfirmed);
        Assert.True(catanzaroResult.IsAccepted);
        var catanzaroProposed = catanzaroResult.SuccessorSession!;
        var catanzaro = catanzaroProposed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "catanzaro-division");
        Assert.Equal(13, catanzaro.Members.Count);
        Assert.Null(catanzaro.StackValue);
        Assert.Equal(35, catanzaro.MotorPool);
        var catanzaroChoice = CampaignExercises.Query(catanzaroProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "catanzaro-disposition");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(catanzaroProposed,
            Submission(catanzaroProposed, catanzaroChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var catanzaroCommand = Submission(catanzaroProposed, catanzaroChoice.ActionId, CampaignActionAudience.Axis);
        var catanzaroConfirmed = CampaignExercises.Submit(catanzaroProposed, catanzaroCommand).SuccessorSession!;
        Assert.Equal(42, catanzaroConfirmed.SourceAuthority!.ProfileVersion);
        var axisFinish = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var axisFinished = CampaignExercises.Submit(catanzaroConfirmed,
            Submission(catanzaroConfirmed, axisFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwFinishAfterAxis = CampaignExercises.Query(axisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var bothFinishedBeforeCorrection = CampaignExercises.Submit(axisFinished,
            Submission(axisFinished, cwFinishAfterAxis.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(3, bothFinishedBeforeCorrection.SourceAuthority!.FinalizedOwners);
        var correctedFinalized = CampaignExercises.CorrectSourceSeventhShell(bothFinishedBeforeCorrection).SuccessorSession!;
        Assert.Equal(1, correctedFinalized.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedFinalizedLegacyCheckpointContractVersion, correctedFinalized.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(correctedFinalized).IsVerified);
        var choicesBefore = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "sixth-australian-placement")
            .Select(value => value.ChoiceJson).Order(StringComparer.Ordinal).ToArray();
        var choicesAfter = CampaignExercises.Query(axisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "sixth-australian-placement")
            .Select(value => value.ChoiceJson).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(choicesBefore, choicesAfter);
        var australianChoice = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "sixth-australian-placement"
                && value.SourceActionId == CampaignSourceAuthorityState.SixthAustralianActionId("e1829"));
        Assert.DoesNotContain(CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "sixth-australian-placement"
                && value.SourceActionId == CampaignSourceAuthorityState.SixthAustralianActionId(chosenCairoHex));
        var australianSession = CampaignExercises.Submit(catanzaroConfirmed,
            Submission(catanzaroConfirmed, australianChoice.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var australian = australianSession.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "6th-australian-division");
        Assert.Equal(10, australian.Members.Count);
        Assert.Equal(5, australian.StackValue);
        Assert.False(australian.MotorPoolKnown);
        var australianInfantry = australian.Members.Where(value => value.ComponentType == "infantry").ToArray();
        Assert.Equal(6, australianInfantry.Length);
        Assert.All(australianInfantry, value => { Assert.Equal(6, value.ComponentToe); Assert.Equal(10, value.CapabilityPointAllowance); });
        foreach (var id in new[] { "16-australian-infantry-brigade-hq", "17-australian-infantry-brigade-hq" })
        {
            var headquarters = australian.Members.Single(value => value.ElementId == id);
            Assert.Equal(1, headquarters.ComponentToe); Assert.Equal(20, headquarters.CapabilityPointAllowance);
        }
        var cavalry = australian.Members.Single(value => value.ElementId == "6-australian-divisional-cavalry");
        Assert.Equal(8, cavalry.ComponentToe); Assert.Equal(45, cavalry.CapabilityPointAllowance);
        Assert.Equal(catanzaroConfirmed.SourceAuthority!.RandomState, australianSession.SourceAuthority.RandomState);
        Assert.Equal(45, australianSession.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(australianSession).IsVerified);
        var axisBatchChoice = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "ccnn-batch-disposition");
        var afterAustralianAxisChoice = CampaignExercises.Query(australianSession, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "ccnn-batch-disposition");
        Assert.Equal(axisBatchChoice.ChoiceJson, afterAustralianAxisChoice.ChoiceJson);
        var australianFirst = CampaignExercises.Submit(australianSession,
            Submission(australianSession, afterAustralianAxisChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var fullyMotorized = australianFirst;
        for (var index = 0; index < 5; index++)
        {
            var adjustment = CampaignExercises.SubmitSourceFourthIndian(fullyMotorized,
                Submission(fullyMotorized, priorityAction, CampaignActionAudience.Commonwealth));
            Assert.True(adjustment.IsAccepted);
            fullyMotorized = adjustment.SuccessorSession!;
        }
        var motorizedWorld = CampaignSourcePlayableWorld.Create(fullyMotorized.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true);
        Assert.Contains("seventh-armored-shell-stack-correction-required", motorizedWorld.ReadinessBlockers());
        Assert.Contains("owner-setup-finalization-incomplete", motorizedWorld.ReadinessBlockers());
        Assert.Contains("central-india-horse-training-state-unresolved", motorizedWorld.ReadinessBlockers());
        var axisHorseApproved = Act(fullyMotorized, CampaignActionAudience.Axis, "central-india-horse-ruling", "approve");
        var cwHorseApproved = Act(axisHorseApproved, CampaignActionAudience.Commonwealth, "central-india-horse-ruling", "approve");
        Assert.Equal(3, cwHorseApproved.SourceAuthority!.HorseRulingVotes);
        var horseStageOneDeclined = Act(cwHorseApproved, CampaignActionAudience.Commonwealth, "central-india-horse-stage-one", "decline-stage1");
        Assert.True(horseStageOneDeclined.SourceAuthority!.HorseStageOneResolved);
        var shellCorrected = CampaignExercises.CorrectSourceSeventhShell(horseStageOneDeclined).SuccessorSession!;
        Assert.True(shellCorrected.SourceAuthority!.IsSeventhShellCorrected);
        var axisFinalizedForReadiness = Act(shellCorrected, CampaignActionAudience.Axis, "owner-finalization", "finish");
        var bothFinalizedForReadiness = Act(axisFinalizedForReadiness, CampaignActionAudience.Commonwealth, "owner-finalization", "finish");
        Assert.Equal(3, bothFinalizedForReadiness.SourceAuthority!.FinalizedOwners);
        var readyWorld = CampaignSourcePlayableWorld.Create(bothFinalizedForReadiness.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true);
        Assert.DoesNotContain("seventh-armored-shell-stack-correction-required", readyWorld.ReadinessBlockers());
        Assert.DoesNotContain("owner-setup-finalization-incomplete", readyWorld.ReadinessBlockers());
        Assert.DoesNotContain("central-india-horse-training-state-unresolved", readyWorld.ReadinessBlockers());
        var beforeBlockers = motorizedWorld.ReadinessBlockers();
        Assert.Contains("forces-incomplete", beforeBlockers);
        static HashSet<string> RepresentedIds(CampaignSourcePlayableWorld world) => world.GroupedWorld.Prior.World.Representations
            .SelectMany(value => value.BoundElementIds)
            .Concat(world.GroupedWorld.Groups.SelectMany(value => value.Members.Select(member => member.ElementId)))
            .Concat(world.GroupedWorld.Detached.Select(value => value.Member.ElementId))
            .ToHashSet(StringComparer.Ordinal);
        static bool IsRemovedFromPlay(CampaignSourcePlayableWorld world, CampaignSourceBattlefieldEntry value) =>
            world.Source.CwFreeGunsWorld?.GarrisonRemainderContent is not null
            && value.AuthorityStatus == "source-land-only-aa-removed"
            && ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds.Contains(value.ElementId, StringComparer.Ordinal);
        var representedBefore = RepresentedIds(motorizedWorld);
        var unrepresentedBefore = motorizedWorld.Battlefield.Entries
            .Where(entry => !representedBefore.Contains(entry.ElementId) && !IsRemovedFromPlay(motorizedWorld, entry))
            .Select(entry => entry.ElementId).ToHashSet(StringComparer.Ordinal);
        var giarabubClusterIds = new[]
        {
            "scheferzen-garrison", "fort-maddalena-garrison", "el-grein-garrison", "mechili-garrison",
            "giarabub-infantry", "giarabub-machine-gun", "giarabub-antitank", "giarabub-artillery",
            "soluch-antitank-company", "soluch-artillery-regiment", "giarabub-motor-machine-gun",
            "benina-aa-battery", "giarabub-light-aa",
        };
        Assert.All(giarabubClusterIds, id => Assert.Contains(id, unrepresentedBefore));
        var activated = CampaignExercises.ActivateSourceAxisMinorGarrisons(fullyMotorized).SuccessorSession!;
        activated = CampaignExercises.AdmitSourceCompactCheckpoint(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceAxisGarrisonRemainder(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceGiarabubMotorMachineGun(activated).SuccessorSession!;
        var activatedWorld = CampaignSourcePlayableWorld.Create(activated.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true);
        var afterBlockers = activatedWorld.ReadinessBlockers();
        var representedAfter = RepresentedIds(activatedWorld);
        var unrepresentedAfter = activatedWorld.Battlefield.Entries
            .Where(entry => !representedAfter.Contains(entry.ElementId) && !IsRemovedFromPlay(activatedWorld, entry))
            .Select(entry => entry.ElementId).ToHashSet(StringComparer.Ordinal);
        Assert.All(giarabubClusterIds, id => Assert.DoesNotContain(id, unrepresentedAfter));
        // forces-incomplete still fires: the Giarabub cluster was 13 of 57
        // unrepresented elements at readyWorld - activating it alone does not
        // clear every remaining element, only these.
        Assert.Contains("forces-incomplete", afterBlockers);
        Assert.True(unrepresentedAfter.Count < unrepresentedBefore.Count);
    }

    // Combines every proven admission/correction action from this session's 3
    // PRs (#352 Giarabub, #355 Tobruk/Bardia, #359 Category B + Aresca
    // sequencing) into one continuous sequence against the single most-advanced
    // real fixture, instead of each living in its own standalone proof branch
    // that is never exercised together with the others. See which of the 8
    // readiness blockers confirmed present on main actually clear once all of
    // them are chained before Axis/Commonwealth owner finalization.
    [Fact]
    public void IntegratingEveryAdmittedClusterBeforeOwnerFinalizationRevealsRealRemainingBlockers()
    {
        var artifact = Cna1979GrazianiCairoSourceExcerpt.Build();
        var initialization = new CampaignSourceInitialization(CapabilityPointAmount.Zero, 0);
        var request = new CampaignCreationRequest(CampaignPregameSourceCreation.RequestVersion,
            "source-authority-transition", Cna1979Ruleset.Manifest.Hash, 0UL,
            CampaignPregameSourceCreation.SetupId,
            CampaignPregameSourceCreation.CalculateSetupHash(artifact.Identity, initialization),
            artifact.Identity.PackId, artifact.Identity.Hash, Cna1979GrazianiCairoSourceExcerpt.ScenarioId);
        var session = CampaignExercises.Begin(request, artifact, initialization).Session!;
        var polishActionPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        var polishAction = new CampaignSourceTurnAction(17, "e1730");
        var polishSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            polishActionPosition.PositionId, CampaignActionAudience.Commonwealth, [polishAction], CampaignLegalActionSet.SourceTurnPolicyId);
        var polishBytes = CampaignLegalActionSerializer.Serialize(polishSet);
        Assert.Equal(polishSet, CampaignLegalActionSerializer.DeserializeCanonical(polishBytes));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            polishActionPosition.PositionId, CampaignActionAudience.Axis, [polishAction], CampaignLegalActionSet.SourceTurnPolicyId));
        var arrivalPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        var declineAttack = CampaignSourceTurnAction.DeclineGt1Lane2Attack(17);
        var arrivalSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            arrivalPosition.PositionId, CampaignActionAudience.Commonwealth, [declineAttack], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(arrivalSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(arrivalSet)));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            arrivalPosition.PositionId, CampaignActionAudience.Axis, [declineAttack], CampaignLegalActionSet.SourceTurnPolicyId));
        var convoyPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.PhaseId == LandPhaseIds.NavalConvoySchedule);
        var convoyRoll = CampaignSourceTurnAction.RollGt3Supply(17);
        var convoySet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Axis, [convoyRoll], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(convoySet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(convoySet)));
        var convoyChoices = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Axis,
            [CampaignSourceTurnAction.AdjustGt3Supply(17, 2, 1, 1), CampaignSourceTurnAction.FinishGt3Supply(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(convoyChoices, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(convoyChoices)));
        var cwRoll = CampaignSourceTurnAction.RollGt5Supply(17);
        var cwConvoySet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Commonwealth, [cwRoll], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(cwConvoySet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(cwConvoySet)));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Axis, [cwRoll], CampaignLegalActionSet.SourceTurnPolicyId));
        var shippingPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.PhaseId == LandPhaseIds.TacticalShipping);
        var shippingSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            shippingPosition.PositionId, CampaignActionAudience.Commonwealth, [CampaignSourceTurnAction.DeclineTacticalShipping(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(shippingSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(shippingSet)));
        var declarationPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1 && value.PhaseId == LandPhaseIds.InitiativeDeclaration);
        var declarationSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            declarationPosition.PositionId, CampaignActionAudience.Axis, [CampaignSourceTurnAction.ActFirst(17), CampaignSourceTurnAction.ActLast(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(declarationSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(declarationSet)));
        var organizationPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1 && value.PhaseId == LandPhaseIds.Organization);
        var organizationSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            organizationPosition.PositionId, CampaignActionAudience.Commonwealth, [CampaignSourceTurnAction.DeclineVoluntaryOrganization(17)], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(organizationSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(organizationSet)));
        var interruptTraining = CampaignSourceTurnAction.InterruptSixthAustralianTraining(17);
        var trainingSet = new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            organizationPosition.PositionId, CampaignActionAudience.Commonwealth, [interruptTraining], CampaignLegalActionSet.SourceTurnPolicyId);
        Assert.Equal(trainingSet, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(trainingSet)));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            organizationPosition.PositionId, CampaignActionAudience.Axis, [interruptTraining], CampaignLegalActionSet.SourceTurnPolicyId));
        var unknownTraining = CampaignSourceTrainingLedger.CreateSixthAustralian();
        var interruptedTraining = unknownTraining.Interrupt();
        Assert.True(interruptedTraining.Interrupted);
        Assert.Null(interruptedTraining.CurrentMorale);
        Assert.Null(interruptedTraining.QualifyingStages);
        Assert.NotEqual(unknownTraining.Hash(), interruptedTraining.Hash());
        Assert.Throws<InvalidOperationException>(() => interruptedTraining.Interrupt());
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(request.CampaignId, 17, request.RulesetHash,
            convoyPosition.PositionId, CampaignActionAudience.Commonwealth, [convoyRoll], CampaignLegalActionSet.SourceTurnPolicyId));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Axis));
        var finishBeforeRoll = Submission(session, CampaignSourceAuthorityState.FinishActionId, CampaignActionAudience.Axis);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.FinishPregameAxisSupply(session, finishBeforeRoll).RejectionReason);
        var roll = Assert.Single(CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates);
        session = CampaignExercises.Submit(session, Submission(session, roll.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        if (1 > 0)
        {
            var add = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates
                .OfType<PregameAxisSupplyAllocationAction>()
                .Single(value => value.Lane == 2 && value.Stage == 1 && value.Delta == 1);
            session = CampaignExercises.Submit(session, Submission(session, add.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        var legacyOffers = CampaignExercises.Query(session, CampaignActionAudience.Axis).ActionSet!.Candidates.Select(x => x.ActionId).ToArray();
        var draft = CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Axis);
        Assert.Single(draft.PrivateActionIds);
        Assert.DoesNotContain(draft.PrivateActionIds[0], legacyOffers);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QueryPregameAxisSupplyFinishDraft(session, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.FinishPregameAxisSupply(session, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Commonwealth)).RejectionReason);
        var accepted = CampaignExercises.FinishPregameAxisSupply(session, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Axis));
        Assert.True(accepted.IsAccepted);
        var next = accepted.SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(next).IsVerified);
        var giarabubAction = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c1014", 1);
        var oldAxisMotorHash = next.SourceAuthority!.AxisMotorization.Hash();
        var oldAxisChoices = CampaignExercises.QuerySourceMotorizationDraft(next, CampaignActionAudience.Axis);
        Assert.DoesNotContain(giarabubAction, oldAxisChoices.PrivateActionIds);
        var admittedPolicy = CampaignExercises.AdmitSourceGiarabubMotorPlacementPolicy(next).SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceGiarabubMotorPlacementPolicyAdmitted(admittedPolicy));
        Assert.NotEqual(oldAxisMotorHash, admittedPolicy.SourceAuthority!.AxisMotorization.Hash());
        Assert.Equal(oldAxisMotorHash, next.SourceAuthority.AxisMotorization.Hash());
        Assert.DoesNotContain(giarabubAction, CampaignExercises.QuerySourceMotorizationDraft(next, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Contains(giarabubAction, CampaignExercises.QuerySourceMotorizationDraft(admittedPolicy, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.DoesNotContain(giarabubAction, CampaignExercises.QuerySourceMotorizationDraft(admittedPolicy, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.AdjustSourceMotorization(admittedPolicy,
            Submission(admittedPolicy, giarabubAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        var placedMotor = CampaignExercises.AdjustSourceMotorization(admittedPolicy,
            Submission(admittedPolicy, giarabubAction, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(124, placedMotor.SourceAuthority!.AxisMotorization.Remaining("axis-libya-unattached"));
        Assert.Contains(placedMotor.SourceAuthority.AxisMotorization.Allocations,
            value => value.PoolId == "axis-libya-unattached" && value.LocationId == "c1014" && value.Points == 1);
        Assert.True(CampaignExercises.Reconstruct(placedMotor).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceGiarabubMotorPlacementPolicy(placedMotor).RejectionReason);
        var cwBeforeFinish = CampaignExercises.QuerySourceActionsDraft(next, CampaignActionAudience.Commonwealth);
        var finishOwner = CampaignExercises.Query(next, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var ownerFinished = CampaignExercises.Submit(next, Submission(next, finishOwner.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(1, ownerFinished.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.FinalizedLegacyCheckpointContractVersion, CampaignExercises.QueryCheckpoint(ownerFinished).ContractVersion);
        Assert.Equal(next.SourceAuthority!.RandomState, ownerFinished.SourceAuthority.RandomState);
        Assert.Equal(next.SourceAuthority.World.PendingArrivals, ownerFinished.SourceAuthority.World.PendingArrivals);
        Assert.Equal(cwBeforeFinish.VisibleBytes, CampaignExercises.QuerySourceActionsDraft(ownerFinished, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.PlaceSourceAxisDump(ownerFinished,
            Submission(ownerFinished, CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507"), CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceCompactCheckpoint(ownerFinished).RejectionReason);
        var cwWhileAxisFinished = CampaignExercises.PlaceSourceCommonwealthSupply(ownerFinished,
            Submission(ownerFinished, CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-dump-1", "d3612"), CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, cwWhileAxisFinished.SourceAuthority!.FinalizedOwners);
        var finishCw = CampaignExercises.Query(cwWhileAxisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var bothFinished = CampaignExercises.Submit(cwWhileAxisFinished, Submission(cwWhileAxisFinished, finishCw.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(3, bothFinished.SourceAuthority!.FinalizedOwners);
        Assert.True(CampaignExercises.Reconstruct(bothFinished).IsVerified);
        var reopenAxis = CampaignExercises.Query(bothFinished, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var reopenedAxis = CampaignExercises.Submit(bothFinished, Submission(bothFinished, reopenAxis.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, reopenedAxis.SourceAuthority!.FinalizedOwners);
        Assert.True(CampaignExercises.Reconstruct(reopenedAxis).IsVerified);
        var reopenCw = CampaignExercises.Query(reopenedAxis, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var reopenedBoth = CampaignExercises.Submit(reopenedAxis, Submission(reopenedAxis, reopenCw.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(0, reopenedBoth.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.FinalizedLegacyCheckpointContractVersion, CampaignExercises.QueryCheckpoint(reopenedBoth).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(reopenedBoth).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.PlaceSourceCommonwealthSupply(reopenedAxis,
            Submission(reopenedAxis, CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-cairo-1", "e1730"), CampaignActionAudience.Commonwealth)).RejectionReason);
        var lockedCwView = CampaignExercises.QuerySourceActionsDraft(reopenedAxis, CampaignActionAudience.Commonwealth).VisibleBytes;
        var changedAxis = CampaignExercises.PlaceSourceAxisDump(reopenedAxis,
            Submission(reopenedAxis, CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507"), CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, changedAxis.SourceAuthority!.FinalizedOwners);
        Assert.Equal(lockedCwView, CampaignExercises.QuerySourceActionsDraft(changedAxis, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.True(CampaignExercises.Reconstruct(changedAxis).IsVerified);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourcePolishArrivalDraft(next, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.SubmitSourcePolishArrival(next, Submission(next, roll.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var earlyCompact = CampaignExercises.AdmitSourceCompactCheckpoint(next).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(earlyCompact).ContractVersion);
        var compactFinish = CampaignExercises.Query(earlyCompact, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var finalizedCompact = CampaignExercises.Submit(earlyCompact, Submission(earlyCompact, compactFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignSourceAuthorityState.FinalizedCompactCheckpointContractVersion, CampaignExercises.QueryCheckpoint(finalizedCompact).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(finalizedCompact).IsVerified);
        var earlyDump = CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507");
        earlyCompact = CampaignExercises.PlaceSourceAxisDump(earlyCompact,
            Submission(earlyCompact, earlyDump, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(30, CampaignExercises.QueryCheckpoint(earlyCompact).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(earlyCompact).IsVerified);
        var normalAxis = CampaignExercises.Query(next, CampaignActionAudience.Axis);
        Assert.True(normalAxis.IsSuccessful);
        Assert.Equal(CampaignLegalActionSet.SourceSetupPolicyId, normalAxis.ActionSet!.PolicyId);
        var encodedActions = CampaignLegalActionSerializer.Serialize(normalAxis.ActionSet);
        Assert.Equal(encodedActions, CampaignLegalActionSerializer.Serialize(CampaignLegalActionSerializer.DeserializeCanonical(encodedActions)));
        var unifiedDump = normalAxis.ActionSet.Candidates.OfType<CampaignSourceSetupAction>()
            .Single(value => value.Family == "axis-dump" && value.ChoiceJson.Contains("c4507", StringComparison.Ordinal)
                && value.ChoiceJson.Contains("axis-dump-1", StringComparison.Ordinal));
        Assert.Throws<ArgumentException>(() => new CampaignLegalActionSet(normalAxis.ActionSet.CampaignId,
            normalAxis.ActionSet.StateVersion, normalAxis.ActionSet.RulesetHash, normalAxis.ActionSet.PositionId,
            CampaignActionAudience.Axis, [unifiedDump], CampaignLegalActionSet.CurrentPolicyId));
        var ownerCut = CampaignExercises.QuerySourceActionsDraft(next, CampaignActionAudience.Axis);
        Assert.DoesNotContain(unifiedDump.ActionId, System.Text.Encoding.UTF8.GetString(ownerCut.VisibleBytes));
        var ownContext = System.Text.Encoding.UTF8.GetString(ownerCut.VisibleBytes);
        Assert.Contains("axis-gt1-supply", ownContext);
        Assert.Contains("maximumSupply", ownContext);
        var hiddenCwAction = CampaignSourceAuthorityState.CommonwealthSupplyActionId("cw-dump-1", "d3612");
        var hiddenAdvance = CampaignExercises.PlaceSourceCommonwealthSupply(next,
            Submission(next, hiddenCwAction, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var afterHidden = CampaignExercises.QuerySourceActionsDraft(hiddenAdvance, CampaignActionAudience.Axis);
        Assert.Equal(ownerCut.VisibleBytes, afterHidden.VisibleBytes);
        Assert.NotEqual(ownerCut.PrivateActionIds[0], afterHidden.PrivateActionIds[0]);
        var wrongSide = Submission(next, unifiedDump.ActionId, CampaignActionAudience.Commonwealth);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(next, wrongSide).RejectionReason);
        var unifiedResult = CampaignExercises.Submit(next, Submission(next, unifiedDump.ActionId, CampaignActionAudience.Axis));
        Assert.True(unifiedResult.IsAccepted);
        Assert.Equal(unifiedDump.ActionId, unifiedResult.Evidence!.Receipt.ActionId);
        Assert.True(CampaignExercises.Reconstruct(unifiedResult.SuccessorSession!).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(unifiedResult.SuccessorSession!,
            Submission(next, unifiedDump.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(unifiedResult.SuccessorSession!,
            Submission(unifiedResult.SuccessorSession!, unifiedDump.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.FinishPregameAxisSupply(next, Submission(session, draft.PrivateActionIds[0], CampaignActionAudience.Axis)).RejectionReason);
        using var snapshot = JsonDocument.Parse(accepted.Evidence!.SnapshotCheckpoint);
        Assert.Equal("setup-in-progress", snapshot.RootElement.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, snapshot.RootElement.GetProperty("gameClock").ValueKind);
        using var plan = JsonDocument.Parse(snapshot.RootElement.GetProperty("convoyPlanBase64").GetBytesFromBase64());
        Assert.Equal(3, plan.RootElement.GetProperty("maximumSupply").GetInt32());
        Assert.Equal(3 - 1, plan.RootElement.GetProperty("unusedAvailability").GetInt32());
        using var source = JsonDocument.Parse(snapshot.RootElement.GetProperty("sourceSnapshotBase64").GetBytesFromBase64());
        Assert.Contains(source.RootElement.GetProperty("pendingArrivalGroupIds").EnumerateArray().Select(x => x.GetString()),
            id => id == Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId);
        var dumpDraft = CampaignExercises.QuerySourceAxisDumpDraft(next, CampaignActionAudience.Axis);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisDumpDraft(next, CampaignActionAudience.Commonwealth));
        Assert.Equal(8, dumpDraft.PrivateActionIds.Count);
        var chosenId = CampaignSourceAuthorityState.DumpActionId("axis-dump-1", "c4507");
        Assert.Contains(chosenId, dumpDraft.PrivateActionIds);
        var dumpCommand = Submission(next, chosenId, CampaignActionAudience.Axis);
        var placed = CampaignExercises.PlaceSourceAxisDump(next, dumpCommand);
        Assert.True(placed.IsAccepted);
        var after = placed.SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(after).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
            CampaignExercises.PlaceSourceAxisDump(after, dumpCommand).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceAxisDump(after, Submission(after,
                CampaignSourceAuthorityState.DumpActionId("axis-dump-2", "c4507"), CampaignActionAudience.Axis)).RejectionReason);
        var second = CampaignExercises.PlaceSourceAxisDump(after, Submission(after,
            CampaignSourceAuthorityState.DumpActionId("axis-dump-2", "c4707"), CampaignActionAudience.Axis));
        Assert.True(second.IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(second.SuccessorSession!).IsVerified);
        var corruptHistory = second.SuccessorSession!.PregameHistory.Select(value => value.ToArray()).ToArray();
        var lastEvent = System.Text.Encoding.UTF8.GetString(corruptHistory[^1]).Replace("c4707", "c4507", StringComparison.Ordinal);
        corruptHistory[^1] = System.Text.Encoding.UTF8.GetBytes(lastEvent);
        var corrupt = new ExerciseSession(second.SuccessorSession.Pregame!, second.SuccessorSession.CurrentContacts,
            corruptHistory, second.SuccessorSession.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(corrupt).IsVerified);
        using var finalSnapshot = JsonDocument.Parse(second.Evidence!.SnapshotCheckpoint);
        using var world = JsonDocument.Parse(finalSnapshot.RootElement.GetProperty("axisSupplyWorldBase64").GetBytesFromBase64());
        Assert.Equal(22, world.RootElement.GetProperty("units").GetArrayLength());
        Assert.False(world.RootElement.GetProperty("siteCoverageComplete").GetBoolean());
        Assert.Equal(JsonValueKind.Null, world.RootElement.GetProperty("units").EnumerateArray()
            .Single(x => x.GetProperty("id").GetString() == "axis-derna-1").GetProperty("ammo").ValueKind);
        var cwSession = second.SuccessorSession!;
        Assert.Equal(CampaignSourceAuthorityState.ContractVersion, cwSession.SourceAuthority!.ProfileVersion);
        Assert.True(CampaignExercises.IsSourceSetupSeat(cwSession, CampaignActionAudience.Axis));
        Assert.True(CampaignExercises.IsSourceSetupSeat(cwSession, CampaignActionAudience.Commonwealth));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Axis));
        var placements = new[]
        {
            ("cw-dump-1", "d3612"),
            ("cw-alexandria-1", "e3613"), ("cw-alexandria-2", "e3613"), ("cw-alexandria-3", "e3714"),
            ("cw-cairo-1", "e1829"), ("cw-cairo-2", "e1830"), ("cw-cairo-3", "e1930"),
        };
        foreach (var (id, location) in placements)
        {
            var idHash = CampaignSourceAuthorityState.CommonwealthSupplyActionId(id, location);
            var cwDraft = CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Commonwealth);
            Assert.Contains(idHash, cwDraft.PrivateActionIds);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCommonwealthSupply(cwSession, Submission(cwSession, idHash, CampaignActionAudience.Axis)).RejectionReason);
            var step = CampaignExercises.PlaceSourceCommonwealthSupply(cwSession, Submission(cwSession, idHash, CampaignActionAudience.Commonwealth));
            Assert.True(step.IsAccepted);
            cwSession = step.SuccessorSession!;
            Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        }
        Assert.Empty(CampaignExercises.QuerySourceCommonwealthSupplyDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.Equal(15, cwSession.SourceAuthority!.CommonwealthSupplyWorld.Units.Count);
        Assert.Equal(18, cwSession.SourceAuthority.CommonwealthInitialForces.Forces.Count);
        Assert.Null(cwSession.SourceAuthority.CommonwealthInitialForces.Forces
            .Single(value => value.ElementId == "central-india-horse").InitialToes);
        Assert.Null(cwSession.SourceAuthority.Position);
        var axisDummy = CampaignSourceAuthorityState.DummyActionId(true, "axis-dummy-1", "c4407");
        Assert.Contains(axisDummy, CampaignExercises.QuerySourceDummyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, axisDummy, CampaignActionAudience.Commonwealth)).RejectionReason);
        var dummyStep = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, axisDummy, CampaignActionAudience.Axis));
        Assert.True(dummyStep.IsAccepted);
        cwSession = dummyStep.SuccessorSession!;
        var omit = CampaignSourceAuthorityState.DummyActionId(true, "axis-dummy-2", null);
        cwSession = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, omit, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwDummy = CampaignSourceAuthorityState.DummyActionId(false, "cw-dummy-1", "d3611");
        Assert.Contains(cwDummy, CampaignExercises.QuerySourceDummyDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        cwSession = CampaignExercises.DecideSourceDummy(cwSession, Submission(cwSession, cwDummy, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var priorRandom = cwSession.SourceAuthority!.RandomState;
        var expectedGt1 = SandtableRandom.RollD6(priorRandom);
        var rollGt1 = CampaignSourceAuthorityState.OpeningConvoyActionId("roll", 0, 0, 0);
        Assert.Single(CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, rollGt1, CampaignActionAudience.Commonwealth)).RejectionReason);
        var gt1Step = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, rollGt1, CampaignActionAudience.Axis));
        Assert.True(gt1Step.IsAccepted);
        cwSession = gt1Step.SuccessorSession!;
        Assert.Equal(expectedGt1.Value, cwSession.SourceAuthority!.OpeningConvoyPlan.Die);
        Assert.Equal(expectedGt1.State.NextByteCursor, cwSession.SourceAuthority.RandomState.NextByteCursor);
        if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply > 0)
        {
            // Real source lane 3 under disclosed provisional Benghazi ruling.
            // This branches the same rolled plan; it does not advance a game.
            var lane3Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 3, 1, 1);
            var lane3Session = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession,
                Submission(cwSession, lane3Add, CampaignActionAudience.Axis)).SuccessorSession!;
            var lane3Finish = CampaignSourceAuthorityState.OpeningConvoyActionId("finish", 0, 0, 0);
            lane3Session = CampaignExercises.SubmitSourceAxisGt1Supply(lane3Session,
                Submission(lane3Session, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
            var lane3Source = lane3Session.SourceAuthority!;
            var axisBefore = CampaignSourceGt1AxisArrival.Create(lane3Source);
            var benghazi = CampaignSourceGt1BenghaziArrival.Create(lane3Source, axisBefore);
            Assert.True(benghazi.CanAnnounce);
            var announced = benghazi.Announce(lane3Source, axisBefore);
            var (delivered, axisAfter) = announced.DeclineAttackAndDeliver(lane3Source, axisBefore);
            Assert.True(delivered.Delivered);
            Assert.Equal("b4827", Assert.Single(lane3Source.AxisSupplyWorld.Units,
                value => value.Id == "axis-benghazi-1").LocationId);
            Assert.Equal("a4827", Assert.Single(axisAfter.Supply.Units,
                value => value.Id == "axis-benghazi-1").LocationId);
            var landed = Assert.Single(axisAfter.Supply.Units, value => value.Id == "axis-gt1-op1-lane3-1");
            Assert.Equal("a4827", landed.LocationId);
            Assert.Equal(40, landed.Ammo);
            Assert.Equal(60, landed.Fuel);
            Assert.Equal(2, axisAfter.Supply.Units.Count(value => value.LocationId == "a4827"));
            Assert.False(axisAfter.HasOtherStageOneLanes);
            Assert.Throws<InvalidOperationException>(() => announced.DeclineAttackAndDeliver(lane3Source, axisAfter));
            var (replayedBenghazi, replayedAxis) = benghazi.Announce(lane3Source, axisBefore)
                .DeclineAttackAndDeliver(lane3Source, axisBefore);
            Assert.Equal(delivered.Hash(), replayedBenghazi.Hash());
            Assert.Equal(axisAfter.Hash(), replayedAxis.Hash());
            using var landedJson = JsonDocument.Parse(axisAfter.Supply.SerializeCanonical());
            Assert.Equal(2, landedJson.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Contains("rul-gra-05-provisional", landedJson.RootElement.GetProperty("benghaziCoordinateStatus").GetString()!);
            if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply >= 2)
            {
                var mixed = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession,
                    Submission(cwSession, lane3Add, CampaignActionAudience.Axis)).SuccessorSession!;
                var lane2Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 2, 1, 1);
                mixed = CampaignExercises.SubmitSourceAxisGt1Supply(mixed,
                    Submission(mixed, lane2Add, CampaignActionAudience.Axis)).SuccessorSession!;
                mixed = CampaignExercises.SubmitSourceAxisGt1Supply(mixed,
                    Submission(mixed, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
                var mixedSource = mixed.SourceAuthority!;
                var mixedAxis = CampaignSourceGt1AxisArrival.Create(mixedSource);
                Assert.False(mixedAxis.CanAnnounce);
                Assert.False(CampaignSourceGt1BenghaziArrival.Create(mixedSource, mixedAxis).CanAnnounce);
                Assert.Throws<InvalidOperationException>(() => mixedAxis.DeliverSoleBenghaziLane(mixedSource, 1));
                var multi = CampaignSourceGt1MultiArrival.Announce(mixedSource, mixedAxis);
                Assert.Collection(multi.Lanes, value => Assert.Equal(2, value), value => Assert.Equal(3, value));
                var (firstThree, axisThree) = multi.DeclineAndDeliver(mixedSource, mixedAxis, 3);
                var (threeThenTwo, axisThreeThenTwo) = firstThree.DeclineAndDeliver(mixedSource, axisThree, 2);
                var (firstTwo, axisTwo) = multi.DeclineAndDeliver(mixedSource, mixedAxis, 2);
                var (twoThenThree, axisTwoThenThree) = firstTwo.DeclineAndDeliver(mixedSource, axisTwo, 3);
                Assert.True(threeThenTwo.IsDelivered(2));
                Assert.True(threeThenTwo.IsDelivered(3));
                Assert.Equal(axisThreeThenTwo.Supply.Hash(), axisTwoThenThree.Supply.Hash());
                Assert.Equal(threeThenTwo.Hash(), twoThenThree.Hash());
                Assert.Equal("box.tripoli", Assert.Single(axisThreeThenTwo.Supply.Units,
                    value => value.Id == "axis-gt1-op1-lane2-1").LocationId);
                Assert.Equal("a4827", Assert.Single(axisThreeThenTwo.Supply.Units,
                    value => value.Id == "axis-gt1-op1-lane3-1").LocationId);
                Assert.Throws<InvalidOperationException>(() => threeThenTwo.DeclineAndDeliver(mixedSource, axisThreeThenTwo, 3));
                Assert.DoesNotContain(mixedSource.AxisSupplyWorld.Units, value => value.Id == "axis-gt1-op1-lane3-1");
                if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply >= 3)
                {
                    var triple = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession,
                        Submission(cwSession, lane3Add, CampaignActionAudience.Axis)).SuccessorSession!;
                    triple = CampaignExercises.SubmitSourceAxisGt1Supply(triple,
                        Submission(triple, lane2Add, CampaignActionAudience.Axis)).SuccessorSession!;
                    var lane6Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 6, 1, 1);
                    triple = CampaignExercises.SubmitSourceAxisGt1Supply(triple,
                        Submission(triple, lane6Add, CampaignActionAudience.Axis)).SuccessorSession!;
                    triple = CampaignExercises.SubmitSourceAxisGt1Supply(triple,
                        Submission(triple, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
                    var tripleSource = triple.SourceAuthority!;
                    var tripleAxis = CampaignSourceGt1AxisArrival.Create(tripleSource);
                    var tripleMulti = CampaignSourceGt1MultiArrival.Announce(tripleSource, tripleAxis);
                    var (tripleTwo, tripleAfterTwo) = tripleMulti.DeclineAndDeliver(tripleSource, tripleAxis, 2);
                    var (tripleThree, tripleAfterThree) = tripleTwo.DeclineAndDeliver(tripleSource, tripleAfterTwo, 3);
                    Assert.True(tripleThree.IsDue(6));
                    Assert.False(tripleThree.IsDelivered(6));
                    Assert.True(tripleThree.CanDecline(6));
                    var (tripleSix, tripleAfterSix) = tripleThree.DeclineAndDeliver(tripleSource, tripleAfterThree, 6);
                    Assert.True(tripleSix.Lane6Declined);
                    Assert.True(tripleSix.IsDelivered(6));
                    Assert.Equal("c4807", Assert.Single(tripleAfterSix.Supply.Units,
                        value => value.Id == "axis-gt1-op1-lane6-1").LocationId);
                    Assert.Throws<InvalidOperationException>(() => tripleSix.DeclineAndDeliver(tripleSource, tripleAfterSix, 6));
                    (bool CanDecline, bool AttackDeclined)? oneBefore = null, oneAfter = null, threeBefore = null, threeAfter = null;
                    for (var amount = 1; amount <= 3; amount++)
                    {
                        var tobruk = cwSession;
                        for (var n = 0; n < amount; n++)
                            tobruk = CampaignExercises.SubmitSourceAxisGt1Supply(tobruk,
                                Submission(tobruk, lane6Add, CampaignActionAudience.Axis)).SuccessorSession!;
                        tobruk = CampaignExercises.SubmitSourceAxisGt1Supply(tobruk,
                            Submission(tobruk, lane3Finish, CampaignActionAudience.Axis)).SuccessorSession!;
                        var tobrukSource = tobruk.SourceAuthority!;
                        var tobrukAxis = CampaignSourceGt1AxisArrival.Create(tobrukSource);
                        var tobrukAnnounced = CampaignSourceGt1MultiArrival.Announce(tobrukSource, tobrukAxis);
                        var (tobrukDeclined, tobrukAfter) = tobrukAnnounced.DeclineAndDeliver(tobrukSource, tobrukAxis, 6);
                        if (amount == 1) { oneBefore = tobrukAnnounced.Lane6CommonwealthCut(); oneAfter = tobrukDeclined.Lane6CommonwealthCut(); }
                        if (amount == 3) { threeBefore = tobrukAnnounced.Lane6CommonwealthCut(); threeAfter = tobrukDeclined.Lane6CommonwealthCut(); }
                        Assert.True(tobrukDeclined.Lane6Declined);
                        Assert.False(tobrukDeclined.CanDecline(6));
                        Assert.Equal(amount <= 2, tobrukDeclined.IsDelivered(6));
                        Assert.Equal(3 + (amount <= 2 ? amount : 0),
                            tobrukAfter.Supply.Units.Count(value => value.LocationId == "c4807"));
                        if (amount == 3) Assert.Equal(tobrukAxis.Supply.Hash(), tobrukAfter.Supply.Hash());
                        Assert.Equal(3, tobrukSource.AxisSupplyWorld.Units.Count(value => value.LocationId == "c4807"));
                    }
                    Assert.Equal(oneBefore, threeBefore);
                    Assert.Equal(oneAfter, threeAfter);
                }
            }
            var gt1Add = CampaignSourceAuthorityState.OpeningConvoyActionId("adjust", 2, 1, 1);
            cwSession = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, gt1Add, CampaignActionAudience.Axis)).SuccessorSession!;
            Assert.Single(cwSession.SourceAuthority!.OpeningConvoyPlan.Allocations());
        }
        var gt1Finish = CampaignSourceAuthorityState.OpeningConvoyActionId("finish", 0, 0, 0);
        cwSession = CampaignExercises.SubmitSourceAxisGt1Supply(cwSession, Submission(cwSession, gt1Finish, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(cwSession.SourceAuthority!.OpeningConvoyPlan.IsFinished);
        var pendingTripoli = CampaignSourceGt1AxisArrival.Create(cwSession.SourceAuthority!);
        var sourceSupplyHash = cwSession.SourceAuthority!.AxisSupplyWorld.Hash();
        if (cwSession.SourceAuthority.OpeningConvoyPlan.MaximumSupply > 0)
        {
            Assert.True(pendingTripoli.CanAnnounce);
            var announcedTripoli = pendingTripoli.Announce();
            Assert.True(announcedTripoli.Announced);
            Assert.False(announcedTripoli.Delivered);
            var deliveredTripoli = announcedTripoli.DeclineAttackAndDeliver();
            var deliveredUnit = Assert.Single(deliveredTripoli.Supply.Units, unit => unit.Id == "axis-gt1-op1-lane2-1");
            Assert.Equal("box.tripoli", deliveredUnit.LocationId);
            Assert.Equal(40, deliveredUnit.Ammo);
            Assert.Equal(60, deliveredUnit.Fuel);
            Assert.Equal(sourceSupplyHash, cwSession.SourceAuthority.AxisSupplyWorld.Hash());
            Assert.NotEqual(pendingTripoli.Hash(), deliveredTripoli.Hash());
            Assert.Throws<InvalidOperationException>(() => deliveredTripoli.DeclineAttackAndDeliver());
        }
        else
        {
            Assert.False(pendingTripoli.CanAnnounce);
            Assert.Throws<InvalidOperationException>(() => pendingTripoli.Announce());
        }

        Assert.Empty(CampaignExercises.QuerySourceAxisGt1SupplyDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var skipCwGt1 = CampaignSourceAuthorityState.CommonwealthSupplyPlanActionId("skip", 1);
        var cwPlanning = CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Commonwealth);
        Assert.Contains(skipCwGt1, cwPlanning.PrivateActionIds);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Axis));
        var beforeSkip = cwSession.SourceAuthority.RandomState.NextByteCursor;
        cwSession = CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession,
            Submission(cwSession, skipCwGt1, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(beforeSkip, cwSession.SourceAuthority!.RandomState.NextByteCursor);
        Assert.Equal("skipped-opening", cwSession.SourceAuthority.CommonwealthSupplyPlans!.Entries[0].Status);
        foreach (var turn in new[] { 2, 3, 4 })
        {
            var prior = cwSession.SourceAuthority.RandomState;
            var expected = SandtableRandom.RollD6(prior);
            var action = CampaignSourceAuthorityState.CommonwealthSupplyPlanActionId("roll", turn);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession, Submission(cwSession, action, CampaignActionAudience.Axis)).RejectionReason);
            cwSession = CampaignExercises.SubmitSourceCommonwealthSupplyPlan(cwSession,
                Submission(cwSession, action, CampaignActionAudience.Commonwealth)).SuccessorSession!;
            var entry = cwSession.SourceAuthority!.CommonwealthSupplyPlans!.Entries.Single(value => value.TargetGameTurn == turn);
            Assert.Equal(expected.Value, entry.Die);
            Assert.Equal((expected.Value + 1) / 2, entry.SupplyUnits);
            Assert.Equal(expected.State.NextByteCursor, cwSession.SourceAuthority.RandomState.NextByteCursor);
        }
        Assert.Empty(CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(cwSession, CampaignActionAudience.Commonwealth).PrivateActionIds);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var gt1Plan = CampaignSourceGt1SupplyPlanning.Create(cwSession.SourceAuthority!, cwSession.SourceAuthority!.RandomState);
        var gt5 = CampaignSourceGt1CommonwealthSupplyPlanning.Create();
        var expectedGt5 = SandtableRandom.RollD6(gt1Plan.RandomState);
        var rolledGt5 = gt5.Roll(gt1Plan.RandomState);
        Assert.Equal(expectedGt5.Value, rolledGt5.Plan.Die);
        Assert.Equal((expectedGt5.Value + 1) / 2, rolledGt5.Plan.Units);
        Assert.Equal(expectedGt5.State, rolledGt5.Random);
        Assert.Throws<InvalidOperationException>(() => rolledGt5.Plan.Roll(rolledGt5.Random));
        Assert.Equal(expectedGt5.State, gt1Plan.WithRandomState(rolledGt5.Random).RandomState);
        var expectedGt3 = SandtableRandom.RollD6(gt1Plan.RandomState);
        var rolledGt3 = gt1Plan.Roll();
        Assert.Equal(expectedGt3.Value, rolledGt3.Die);
        Assert.Equal(expectedGt3.Value switch { 1 => 0, <= 3 => 1, <= 5 => 2, _ => 3 }, rolledGt3.Maximum);
        Assert.Equal(expectedGt3.State, rolledGt3.RandomState);
        Assert.Throws<InvalidOperationException>(() => rolledGt3.Roll());
        Assert.Empty(rolledGt3.ValidDeltas(1, 1));
        Assert.Empty(rolledGt3.ValidDeltas(2, 4));
        if (rolledGt3.Maximum > 0)
        {
            var assigned = rolledGt3.Adjust(2, 1, 1);
            Assert.Equal(1, assigned.QuantityAt(2, 1));
            Assert.Equal(rolledGt3.RandomState, assigned.RandomState);
            Assert.Equal(0, assigned.Adjust(2, 1, -1).QuantityAt(2, 1));
            for (var unit = 1; unit < rolledGt3.Maximum; unit++) assigned = assigned.Adjust(2, 1, 1);
            Assert.DoesNotContain(1, assigned.ValidDeltas(2, 1));
            Assert.DoesNotContain(1, assigned.ValidDeltas(3, 2));
            Assert.Throws<InvalidOperationException>(() => assigned.Adjust(3, 2, 1));
        }
        var finishedGt3 = rolledGt3.Finish();
        Assert.True(finishedGt3.Finished);
        Assert.Empty(finishedGt3.ValidDeltas(2, 1));
        // Each owner's roll may happen first; the shared stream must advance
        // once per roll without rewriting an already-finished Axis plan.
        var axisFirstPlan = gt1Plan.Roll().Finish();
        var axisFirstHash = axisFirstPlan.Hash();
        var cwAfterAxis = CampaignSourceGt1CommonwealthSupplyPlanning.Create().Roll(axisFirstPlan.RandomState);
        Assert.Equal(axisFirstHash, axisFirstPlan.Hash());
        var cwRollFirst = CampaignSourceGt1CommonwealthSupplyPlanning.Create().Roll(gt1Plan.RandomState);
        var axisAfterCw = gt1Plan.WithRandomState(cwRollFirst.Random).Roll().Finish();
        Assert.Equal(axisFirstPlan.Die, cwRollFirst.Plan.Die);
        Assert.Equal(cwAfterAxis.Plan.Die, axisAfterCw.Die);
        Assert.Equal(cwAfterAxis.Random, axisAfterCw.RandomState);
        Assert.Throws<InvalidOperationException>(() => axisAfterCw.Roll());
        Assert.Throws<InvalidOperationException>(() => cwAfterAxis.Plan.Roll(cwAfterAxis.Random));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        // Exercise the real prestart September plans on an independent branch of the retained source session.
        var september = cwSession;
        var beginSeptember = CampaignSourceAuthorityState.SeptemberSupplyActionId("begin", 0, 0, 0, 0);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceSeptemberSupplyDraft(september, CampaignActionAudience.Commonwealth));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, beginSeptember, CampaignActionAudience.Commonwealth)).RejectionReason);
        var normalSeptember = CampaignExercises.Query(september, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "axis-september-supply");
        Assert.DoesNotContain(CampaignExercises.Query(september, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "axis-september-supply");
        var beginCommand = Submission(september, normalSeptember.ActionId, CampaignActionAudience.Axis);
        september = CampaignExercises.Submit(september, beginCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(september, beginCommand).RejectionReason);
        foreach (var turn in new[] { 3, 4 })
        {
            var expected = SandtableRandom.RollD6(september.SourceAuthority!.RandomState);
            var septemberRoll = CampaignSourceAuthorityState.SeptemberSupplyActionId("roll", turn, 0, 0, 0);
            september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, septemberRoll, CampaignActionAudience.Axis)).SuccessorSession!;
            var entry = september.SourceAuthority!.SeptemberPlans!.Entries.Single(value => value.TargetGameTurn == turn);
            Assert.Equal(expected.Value, entry.Die);
            Assert.Equal(expected.Value switch { 1 => 0, <= 3 => 1, <= 5 => 2, _ => 3 }, entry.Maximum);
            Assert.Equal(expected.State.NextByteCursor, september.SourceAuthority.RandomState.NextByteCursor);
            var finish = CampaignSourceAuthorityState.SeptemberSupplyActionId("finish", turn, 0, 0, 0);
            if (entry.Maximum > 0)
            {
                var zeroPlan = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, finish, CampaignActionAudience.Axis)).SuccessorSession!;
                Assert.True(zeroPlan.SourceAuthority!.SeptemberPlans!.Entries.Single(value => value.TargetGameTurn == turn).Finished);
                Assert.Equal(0, zeroPlan.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == turn).Allocations.Sum());
                var add = CampaignSourceAuthorityState.SeptemberSupplyActionId("adjust", turn, 2, 1, 1);
                september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, add, CampaignActionAudience.Axis)).SuccessorSession!;
            }
            september = CampaignExercises.SubmitSourceSeptemberSupply(september, Submission(september, finish, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        Assert.Null(september.SourceAuthority!.SeptemberPlans!.NextTargetGameTurn);
        Assert.Equal(38, september.SourceAuthority.ProfileVersion);
        var retainedGt3 = CampaignSourceGt1SupplyPlanning.Create(september.SourceAuthority, september.SourceAuthority.RandomState);
        Assert.Equal(september.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == 3).Die, retainedGt3.Die);
        Assert.True(retainedGt3.CarriedPlanFinished);
        Assert.Equal(september.SourceAuthority.SeptemberPlans.Entries.Single(value => value.TargetGameTurn == 3).Allocations[0], retainedGt3.QuantityAt(2, 1));
        Assert.Throws<InvalidOperationException>(() => retainedGt3.Roll());
        var septemberHash = september.SourceAuthority.SeptemberPlans.Hash();
        september = CampaignExercises.AdmitSourceAxisSupplement(september).SuccessorSession!;
        Assert.Equal(septemberHash, september.SourceAuthority!.SeptemberPlans!.Hash());
        Assert.True(CampaignExercises.Reconstruct(september).IsVerified);
        Assert.False(CampaignExercises.IsSourceAxisSupplementAdmitted(cwSession));
        var migrated = CampaignExercises.AdmitSourceAxisSupplement(cwSession);
        Assert.True(migrated.IsAccepted);
        cwSession = migrated.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceAxisSupplementAdmitted(cwSession));
        Assert.Equal(5, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceAxisSupplement(cwSession).RejectionReason);
        var replacements = CampaignSourceAuthorityState.AutoblindaActionId("replacements", "");
        Assert.Contains(replacements, CampaignExercises.QuerySourceAutoblindaDraft(cwSession, CampaignActionAudience.Axis).PrivateActionIds);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, replacements, CampaignActionAudience.Commonwealth)).RejectionReason);
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, replacements, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(2, cwSession.SourceAuthority!.Autoblinda!.ReplacementPoints);
        var reset = CampaignSourceAuthorityState.AutoblindaActionId("reset", "");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, reset, CampaignActionAudience.Axis)).SuccessorSession!;
        var attach1 = CampaignSourceAuthorityState.AutoblindaActionId("attach", "1-141-infantry");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach1, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach1, CampaignActionAudience.Axis)).RejectionReason);
        var attach2 = CampaignSourceAuthorityState.AutoblindaActionId("attach", "2-141-infantry");
        cwSession = CampaignExercises.SubmitSourceAutoblinda(cwSession, Submission(cwSession, attach2, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(cwSession.SourceAuthority!.Autoblinda!.IsComplete);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.False(CampaignExercises.IsSourceCommonwealthSupplementAdmitted(cwSession));
        var cwMigrated = CampaignExercises.AdmitSourceCommonwealthSupplement(cwSession);
        Assert.True(cwMigrated.IsAccepted);
        cwSession = cwMigrated.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCommonwealthSupplementAdmitted(cwSession));
        Assert.Equal(6, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(12, cwSession.SourceAuthority!.CommonwealthSupplement!.Forces.Count);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceCommonwealthSupplement(cwSession).RejectionReason);
        Assert.False(CampaignExercises.IsSourcePriorityMotorizationAdmitted(cwSession));
        var priorityMigration = CampaignExercises.AdmitSourcePriorityMotorization(cwSession);
        Assert.True(priorityMigration.IsAccepted);
        cwSession = priorityMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourcePriorityMotorizationAdmitted(cwSession));
        Assert.Equal(7, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourcePriorityMotorizationDraft(cwSession, CampaignActionAudience.Axis));
        var priorityId = CampaignSourceAuthorityState.PriorityMotorizationActionId("1-kings-royal-rifle-corps", 6);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourcePriorityMotorization(cwSession, Submission(cwSession, priorityId, CampaignActionAudience.Axis)).RejectionReason);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, priorityId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(14, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3922-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        var reversePriority = CampaignSourceAuthorityState.PriorityMotorizationActionId("1-kings-royal-rifle-corps", -6);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, reversePriority, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(20, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3922-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        var secondPriority = CampaignSourceAuthorityState.PriorityMotorizationActionId("2-rifle-brigade", 6);
        cwSession = CampaignExercises.AdjustSourcePriorityMotorization(cwSession,
            Submission(cwSession, secondPriority, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(4, cwSession.SourceAuthority!.PriorityMotorization!.Remaining("cw-c3320-attached", cwSession.SourceAuthority.CommonwealthMotorization));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var axisMigration = CampaignExercises.AdmitSourceAxisPriorityMotorization(cwSession);
        Assert.True(axisMigration.IsAccepted);
        cwSession = axisMigration.SuccessorSession!;
        Assert.Equal(8, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var targetAxis = "1-1-libyan-motor-infantry";
        var oldDetach = CampaignSourceAuthorityState.AxisPriorityActionId("detach", targetAxis, 0);
        var historical = CampaignExercises.ReplaySourceAxisPriority(cwSession,
            Submission(cwSession, oldDetach, CampaignActionAudience.Axis));
        Assert.True(historical.IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(historical.SuccessorSession!).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, oldDetach, CampaignActionAudience.Axis)).RejectionReason);
        var prematureLift = CampaignSourceAuthorityState.AxisPriorityActionId("adjust", targetAxis, 5);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, prematureLift, CampaignActionAudience.Axis)).RejectionReason);
        foreach (var axisTarget in CampaignSourceAxisPriorityMotorization.Targets)
        {
            var detach = CampaignSourceAuthorityState.AxisPriorityActionId("detach", axisTarget, 0);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, detach, CampaignActionAudience.Axis)).RejectionReason);
        }
        Assert.Equal(35, cwSession.SourceAuthority!.AxisPriorityMotorization!.Remaining(cwSession.SourceAuthority.AxisMotorization));
        var undoDetachment = CampaignSourceAuthorityState.AxisPriorityActionId("undo-detachment", targetAxis, 0);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(cwSession, Submission(cwSession, undoDetachment, CampaignActionAudience.Axis)).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var garrisonMigration = CampaignExercises.AdmitSourceAxisMinorGarrisons(cwSession);
        Assert.True(garrisonMigration.IsAccepted);
        cwSession = garrisonMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceAxisMinorGarrisonsAdmitted(cwSession));
        Assert.Equal(9, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(13, cwSession.SourceAuthority!.AxisGarrisons!.Forces.Count);
        Assert.Contains(cwSession.SourceAuthority.AxisGarrisons.Forces,
            value => value.ElementId == "giarabub-motor-machine-gun");
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.AdmitSourceAxisMinorGarrisons(cwSession).RejectionReason);
        var battlefieldMigration = CampaignExercises.AdmitSourceBattlefield(cwSession);
        Assert.True(battlefieldMigration.IsAccepted);
        cwSession = battlefieldMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceBattlefieldAdmitted(cwSession));
        Assert.Equal(10, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var battlefield = cwSession.SourceAuthority!.Battlefield!;
        Assert.Equal(battlefield.Entries.Count, battlefield.Entries.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(2, battlefield.Pending.Count);
        Assert.Equal(8, battlefield.Pending.Single(value => value.GroupId == Cna1979GrazianiSourceFacts.PolishBrigadeArrival.GroupId).ElementIds.Count);
        Assert.Equal(4, battlefield.Pending.Single(value => value.GroupId == Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId).ElementIds.Count);
        using var battlefieldBytes = JsonDocument.Parse(battlefield.SerializeCanonical());
        Assert.False(battlefieldBytes.RootElement.GetProperty("battlefieldComplete").GetBoolean());
        Assert.False(battlefieldBytes.RootElement.GetProperty("controlCertified").GetBoolean());
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceBattlefield(cwSession).RejectionReason);
        var cairoAdmission = CampaignExercises.AdmitSourceCairoSetup(cwSession);
        Assert.True(cairoAdmission.IsAccepted);
        cwSession = cairoAdmission.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCairoSetupAdmitted(cwSession));
        Assert.Equal(11, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        Assert.Equal(4, cwSession.SourceAuthority!.CairoSetup!.PendingIds.Count);
        Assert.Equal(4, cwSession.SourceAuthority.Battlefield!.Entries.Count(x => x.AuthorityStatus == "source-setup-unplaced"));
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceCairoSetupDraft(cwSession, CampaignActionAudience.Axis));
        var invalidCity = CampaignSourceAuthorityState.CairoPlacementActionId("1-welch", "e1829");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, invalidCity, CampaignActionAudience.Commonwealth)).RejectionReason);
        var inferredCity = CampaignSourceAuthorityState.CairoPlacementActionId("1-welch", "e1931");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, inferredCity, CampaignActionAudience.Commonwealth)).RejectionReason);
        var chosenCairoHex = 0UL == 14 ? "e1830" : "e1730";
        foreach (var element in CampaignSourceCairoInitialSetup.Units.Select(value => value.ElementId))
        {
            var action = CampaignSourceAuthorityState.CairoPlacementActionId(element, chosenCairoHex);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCairoUnit(cwSession, Submission(cwSession, action, CampaignActionAudience.Axis)).RejectionReason);
            var command = Submission(cwSession, action, CampaignActionAudience.Commonwealth);
            var placedCairo = CampaignExercises.PlaceSourceCairoUnit(cwSession, command);
            Assert.True(placedCairo.IsAccepted);
            cwSession = placedCairo.SuccessorSession!;
            Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState,
                CampaignExercises.PlaceSourceCairoUnit(cwSession, command).RejectionReason);
        }
        Assert.Empty(cwSession.SourceAuthority!.CairoSetup!.PendingIds);
        Assert.Equal(4, cwSession.SourceAuthority.Battlefield!.Entries.Count(x => x.LocationId == chosenCairoHex && x.AuthorityStatus == "source-setup-placed"));
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        var activeMigration = CampaignExercises.AdmitSourceCairoWorld(cwSession);
        Assert.True(activeMigration.IsAccepted);
        cwSession = activeMigration.SuccessorSession!;
        Assert.True(CampaignExercises.IsSourceCairoWorldAdmitted(cwSession));
        Assert.Equal(12, CampaignExercises.QueryCheckpoint(cwSession).ContractVersion);
        var active = cwSession.SourceAuthority!.CairoWorld!;
        Assert.Equal("sha256:3a7fddd510854c81b408e07742dd837f7be416206117ca58ea79357357a11ddd", active.Content.Hash());
        Assert.NotEqual(cwSession.SourceAuthority.Content.Identity.Hash, active.Content.Identity.Hash);
        Assert.Equal(active.Content.Identity, active.World.ContentIdentity);
        Assert.Equal(8, active.World.World.Elements.Count);
        Assert.Equal(8, active.World.World.Representations.Count);
        Assert.Equal(2, active.World.PendingArrivals.Count);
        foreach (var unit in CampaignSourceCairoInitialSetup.Units)
        {
            var element = Assert.Single(active.World.World.Elements, value => value.ElementId == unit.ElementId);
            Assert.Equal(chosenCairoHex, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
        }
        var alteredContentText = System.Text.Encoding.UTF8.GetString(active.Content.CanonicalBytes)
            .Replace("\"initialToe\":6", "\"initialToe\":7", StringComparison.Ordinal);
        var alteredContent = ContentSourceSetupPackArtifact.Read(System.Text.Encoding.UTF8.GetBytes(alteredContentText));
        Assert.NotEqual(active.Content.Hash(), alteredContent.Hash());
        var forgedHistory = cwSession.PregameHistory.Select(value => value.ToArray()).ToArray();
        var forgedEvent = System.Text.Encoding.UTF8.GetString(forgedHistory[^1])
            .Replace(active.Content.Hash(), alteredContent.Hash(), StringComparison.Ordinal);
        forgedHistory[^1] = System.Text.Encoding.UTF8.GetBytes(forgedEvent);
        var forgedSession = new ExerciseSession(cwSession.Pregame!, cwSession.CurrentContacts, forgedHistory, cwSession.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(forgedSession).IsVerified);
        Assert.True(CampaignExercises.Reconstruct(cwSession).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority, CampaignExercises.AdmitSourceCairoWorld(cwSession).RejectionReason);
        var directV12 = cwSession;
        Assert.True(CampaignExercises.CanPromoteSourceIndependent(directV12));
        var lockBeforePromotion = CampaignExercises.Query(directV12, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var lockedBeforePromotion = CampaignExercises.Submit(directV12,
            Submission(directV12, lockBeforePromotion.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.PromoteSourceIndependent(lockedBeforePromotion).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.ReplaySourceIndependentPromotion(lockedBeforePromotion).RejectionReason);
        directV12 = CampaignExercises.PromoteSourceIndependent(directV12).SuccessorSession!;
        Assert.Equal(14, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(10, directV12.SourceAuthority!.World.World.Elements.Count);
        var genericIdentity = directV12.SourceAuthority.World.ContentIdentity;
        Assert.False(CampaignExercises.CanPromoteSourceIndependent(directV12));
        directV12 = CampaignExercises.AdmitSourceLocalMap(directV12).SuccessorSession!;
        Assert.Equal(15, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var mapIdentity = directV12.SourceAuthority!.World.ContentIdentity;
        Assert.NotEqual(genericIdentity, mapIdentity);
        Assert.Equal(10, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.PromoteSourceExpandedIndependent(directV12).SuccessorSession!;
        Assert.Equal(16, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(13, directV12.SourceAuthority!.World.World.Elements.Count);
        var expandedIdentity = directV12.SourceAuthority.World.ContentIdentity;
        Assert.False(CampaignExercises.CanPromoteSourceExpandedIndependent(directV12));
        Assert.False(CampaignExercises.CanPromoteSourceMaletti(directV12));
        Assert.Equal(expandedIdentity, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(mapIdentity, directV12.SourceAuthority.LocalMapWorld!.Content.Identity);
        Assert.Equal(genericIdentity, directV12.SourceAuthority.IndependentWorld!.Content.Identity);
        Assert.Equal(13, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.AdmitSourceAttachedInitialLoads(directV12).SuccessorSession!;
        Assert.Equal(17, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        const string loadedTarget = "2-rifle-brigade";
        var attach = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("attach", loadedTarget);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, attach, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, attach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, directV12.SourceAuthority!.AttachedInitialLoads!.Reserved("cw-c3320-attached"));
        var fuel = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("fuel", loadedTarget);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, fuel, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, Assert.Single(directV12.SourceAuthority!.AttachedInitialLoads!.Targets).FuelPoints);
        var detachLoad = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("detach", loadedTarget);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, detachLoad, CampaignActionAudience.Commonwealth)).RejectionReason);
        var removeLift = CampaignSourceAuthorityState.PriorityMotorizationActionId(loadedTarget, -6);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.AdjustSourcePriorityMotorization(directV12, Submission(directV12, removeLift, CampaignActionAudience.Commonwealth)).RejectionReason);
        var clear = CampaignSourceAuthorityState.AttachedInitialLoadsActionId("unload-fuel", loadedTarget);
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, clear, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12, Submission(directV12, detachLoad, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Empty(directV12.SourceAuthority!.AttachedInitialLoads!.Targets);
        directV12 = CampaignExercises.AdmitSourceInitialWrecks(directV12).SuccessorSession!;
        Assert.Equal(18, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(3, directV12.SourceAuthority!.InitialWrecks!.PendingIds.Count);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceInitialWrecksDraft(directV12, CampaignActionAudience.Axis));
        foreach (var id in directV12.SourceAuthority.InitialWrecks.PendingIds.ToArray())
        {
            var place = CampaignSourceAuthorityState.InitialWreckPlacementActionId(id, "e3613");
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceInitialWreck(directV12, Submission(directV12, place, CampaignActionAudience.Axis)).RejectionReason);
            directV12 = CampaignExercises.PlaceSourceInitialWreck(directV12, Submission(directV12, place, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        }
        Assert.Empty(directV12.SourceAuthority!.InitialWrecks!.PendingIds);
        Assert.Equal(3, directV12.SourceAuthority.InitialWrecks.Placements.Count);
        directV12 = CampaignExercises.AdmitSourceAxisFreeSetup(directV12).SuccessorSession!;
        Assert.Equal(19, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var forgedLegacyState = directV12.SourceAuthority!.ApplyAxisPriorityChoice("detach", targetAxis, 0);
        var forgedLegacyEvent = directV12.SourceAuthority.SerializeAxisPriorityEvent(forgedLegacyState, "detach", targetAxis, 0);
        var forgedLegacySession = new ExerciseSession(directV12.Pregame!, directV12.CurrentContacts.AdvancePregameSource(directV12.SourceAuthority.StateVersion),
            [.. directV12.PregameHistory, forgedLegacyEvent], forgedLegacyState);
        Assert.False(CampaignExercises.Reconstruct(forgedLegacySession).IsVerified);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisFreeSetupDraft(directV12, CampaignActionAudience.Commonwealth));
        var freeIds = directV12.SourceAuthority!.AxisFreeArtillery!.PendingIds.ToArray();
        for (var i = 0; i < freeIds.Length; i++)
        {
            var location = i < 2 ? "c4407" : "c4607";
            var action = CampaignSourceAuthorityState.AxisFreePlacementActionId(freeIds[i], location);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, action, CampaignActionAudience.Commonwealth)).RejectionReason);
            if (i == 2)
            {
                var overstack = CampaignSourceAuthorityState.AxisFreePlacementActionId(freeIds[i], "c4407");
                Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                    CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, overstack, CampaignActionAudience.Axis)).RejectionReason);
            }
            directV12 = CampaignExercises.PlaceSourceAxisFreeUnit(directV12, Submission(directV12, action, CampaignActionAudience.Axis)).SuccessorSession!;
            if (i == 0)
            {
                directV12 = CampaignExercises.PromoteSourceAxisFreeWorld(directV12).SuccessorSession!;
                Assert.Equal(20, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
                Assert.Equal(14, directV12.SourceAuthority!.World.World.Elements.Count);
                var identityAfterPartial = directV12.SourceAuthority.World.ContentIdentity;
                var adjustAxis = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", 1);
                directV12 = CampaignExercises.AdjustSourceMotorization(directV12,
                    Submission(directV12, adjustAxis, CampaignActionAudience.Axis)).SuccessorSession!;
                directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12,
                    Submission(directV12, attach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
                Assert.Equal(identityAfterPartial, directV12.SourceAuthority!.World.ContentIdentity);
                Assert.Equal(14, directV12.SourceAuthority.World.World.Elements.Count);
                Assert.False(CampaignExercises.CanPromoteSourceIndependent(directV12));
                Assert.False(CampaignExercises.CanPromoteSourceExpandedIndependent(directV12));
            }
        }
        Assert.Empty(directV12.SourceAuthority!.AxisFreeArtillery!.PendingIds);
        Assert.Equal(3, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-1ar-placed"));
        var axisContent = directV12.SourceAuthority.World.ContentIdentity;
        directV12 = CampaignExercises.PromoteSourceAxisFreeWorld(directV12).SuccessorSession!;
        Assert.Equal(axisContent, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(17, directV12.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Equal(4, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-1ar-active"));
        Assert.False(CampaignExercises.CanPromoteSourceAxisFreeWorld(directV12));
        directV12 = CampaignExercises.AdmitSourceAxisCorpsSetup(directV12).SuccessorSession!;
        Assert.Equal(21, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceAxisCorpsSetupDraft(directV12, CampaignActionAudience.Commonwealth));
        var corpsIds = directV12.SourceAuthority!.AxisCorpsArtillery!.PendingIds.ToArray();
        for (var i = 0; i < corpsIds.Length; i++)
        {
            var location = i == 0 ? "c4307" : "c4308";
            var action = CampaignSourceAuthorityState.AxisCorpsPlacementActionId(corpsIds[i], location);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, action, CampaignActionAudience.Commonwealth)).RejectionReason);
            if (i == 1)
            {
                var occupied = CampaignSourceAuthorityState.AxisCorpsPlacementActionId(corpsIds[i], "c4307");
                Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                    CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, occupied, CampaignActionAudience.Axis)).RejectionReason);
            }
            directV12 = CampaignExercises.PlaceSourceAxisCorpsUnit(directV12, Submission(directV12, action, CampaignActionAudience.Axis)).SuccessorSession!;
            directV12 = CampaignExercises.PromoteSourceAxisCorpsWorld(directV12).SuccessorSession!;
            Assert.Equal(22, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        }
        Assert.Equal(19, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Equal(2, directV12.SourceAuthority.Battlefield!.Entries.Count(entry => entry.AuthorityStatus == "source-corps-artillery-active"));
        foreach (var id in corpsIds)
            Assert.Equal(3, directV12.SourceAuthority.World.World.Elements.Single(value => value.ElementId == id).Components.Single(value => value.ComponentId.EndsWith(".light-aa", StringComparison.Ordinal)).CurrentToe);
        Assert.False(CampaignExercises.CanPromoteSourceAxisCorpsWorld(directV12));
        var corpsContent = directV12.SourceAuthority.World.ContentIdentity;
        var removeAxisPoint = CampaignSourceAuthorityState.MotorizationActionId(true, "axis-libya-unattached", "c4507", -1);
        directV12 = CampaignExercises.AdjustSourceMotorization(directV12,
            Submission(directV12, removeAxisPoint, CampaignActionAudience.Axis)).SuccessorSession!;
        directV12 = CampaignExercises.SubmitSourceAttachedInitialLoads(directV12,
            Submission(directV12, fuel, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(corpsContent, directV12.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(19, directV12.SourceAuthority.World.World.Elements.Count);
        directV12 = CampaignExercises.AdmitSourceMalettiHierarchy(directV12).SuccessorSession!;
        Assert.Equal(23, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var confirmHierarchy = CampaignSourceAuthorityState.MalettiHierarchyActionId("confirm", null);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMalettiHierarchy(directV12, Submission(directV12, confirmHierarchy, CampaignActionAudience.Commonwealth)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceMalettiHierarchy(directV12,
            Submission(directV12, confirmHierarchy, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwFirst = directV12;
        var frenchFirst = directV12;
        var certifiedDetach = CampaignSourceAuthorityState.MalettiHierarchyActionId("detach", targetAxis);
        directV12 = CampaignExercises.SubmitSourceMalettiHierarchy(directV12,
            Submission(directV12, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(targetAxis, directV12.SourceAuthority!.MalettiHierarchy!.DetachedId);
        var otherDetach = CampaignSourceAuthorityState.MalettiHierarchyActionId("detach", "3-1-libyan-motor-infantry");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMalettiHierarchy(directV12, Submission(directV12, otherDetach, CampaignActionAudience.Axis)).RejectionReason);
        Assert.True(CampaignExercises.CanPromoteSourceCertifiedMalettiWorld(directV12));
        directV12 = CampaignExercises.PromoteSourceCertifiedMalettiWorld(directV12).SuccessorSession!;
        Assert.Equal(24, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(20, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        Assert.Contains(directV12.SourceAuthority.Battlefield!.Entries,
            value => value.ElementId == targetAxis && value.AuthorityStatus == "source-certified-maletti-active");
        Assert.False(CampaignExercises.CanPromoteSourceCertifiedMalettiWorld(directV12));
        var axisLift = CampaignSourceAuthorityState.AxisPriorityActionId("adjust", targetAxis, 5);
        var lockBeforeLift = CampaignExercises.Query(directV12, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var lockedBeforeLift = CampaignExercises.Submit(directV12,
            Submission(directV12, lockBeforeLift.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceAxisPriority(lockedBeforeLift,
                Submission(lockedBeforeLift, axisLift, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceAxisPriority(directV12,
            Submission(directV12, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(5, directV12.SourceAuthority!.AxisPriorityMotorization!.AssignedPoints(targetAxis));
        directV12 = CampaignExercises.AdmitSourceMatruhFormation(directV12).SuccessorSession!;
        Assert.Equal(25, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Throws<InvalidOperationException>(() => CampaignExercises.QuerySourceMatruhFormationDraft(directV12, CampaignActionAudience.Axis));
        var matruhConfirm = CampaignSourceAuthorityState.MatruhFormationActionId("confirm", null);
        directV12 = CampaignExercises.SubmitSourceMatruhFormation(directV12,
            Submission(directV12, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(5, directV12.SourceAuthority!.MatruhFormation!.AttachedIds.Count);
        var matruhDetach = CampaignSourceAuthorityState.MatruhFormationActionId("detach", "1-essex");
        directV12 = CampaignExercises.SubmitSourceMatruhFormation(directV12,
            Submission(directV12, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(4, directV12.SourceAuthority!.MatruhFormation!.AttachedIds.Count);
        Assert.Contains(directV12.SourceAuthority.Battlefield!.Entries,
            entry => entry.ElementId == "1-essex" && entry.AuthorityStatus == "source-matruh-detached");
        var secondMatruhDetach = CampaignSourceAuthorityState.MatruhFormationActionId("detach", "1-durham-light-infantry");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceMatruhFormation(directV12, Submission(directV12, secondMatruhDetach, CampaignActionAudience.Commonwealth)).RejectionReason);
        directV12 = CampaignExercises.PromoteSourceMatruhDetachedWorld(directV12).SuccessorSession!;
        Assert.Equal(26, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(21, directV12.SourceAuthority!.World.World.Elements.Count);
        Assert.Equal(4, directV12.SourceAuthority.MatruhFormation!.AttachedIds.Count);
        Assert.Equal(2, directV12.SourceAuthority.World.PendingArrivals.Count);
        cwFirst = CampaignExercises.AdmitSourceMatruhFormation(cwFirst).SuccessorSession!;
        cwFirst = CampaignExercises.SubmitSourceMatruhFormation(cwFirst,
            Submission(cwFirst, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        cwFirst = CampaignExercises.SubmitSourceMatruhFormation(cwFirst,
            Submission(cwFirst, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        cwFirst = CampaignExercises.PromoteSourceMatruhDetachedWorld(cwFirst).SuccessorSession!;
        var cwFirstIdentity = cwFirst.SourceAuthority!.World.ContentIdentity;
        cwFirst = CampaignExercises.SubmitSourceMalettiHierarchy(cwFirst,
            Submission(cwFirst, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        cwFirst = CampaignExercises.PromoteSourceCertifiedMalettiWorld(cwFirst).SuccessorSession!;
        Assert.NotEqual(cwFirstIdentity, cwFirst.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(21, cwFirst.SourceAuthority.World.World.Elements.Count);
        Assert.Equal(2, cwFirst.SourceAuthority.World.PendingArrivals.Count);
        Assert.True(CampaignExercises.Reconstruct(cwFirst).IsVerified);
        var rngBeforeGt2 = directV12.SourceAuthority!.RandomState;
        var worldBeforeGt2 = directV12.SourceAuthority.World.ContentIdentity;
        directV12 = CampaignExercises.AdmitSourceGt2Correction(directV12).SuccessorSession!;
        Assert.Equal(27, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        var finishGt2 = CampaignSourceAuthorityState.Gt2CorrectionActionId("finish", 0, 0, 0);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Commonwealth)).RejectionReason);
        if (3 > 0)
        {
            foreach (var lane in new[] { 2, 3, 6 })
                for (var stage = 1; stage <= 3; stage++)
                    while (directV12.SourceAuthority!.Gt2Correction!.Quantity(lane, stage) > 0)
                    {
                        var removeUnit = CampaignSourceAuthorityState.Gt2CorrectionActionId("adjust", lane, stage, -1);
                        directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12,
                            Submission(directV12, removeUnit, CampaignActionAudience.Axis)).SuccessorSession!;
                    }
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Axis)).RejectionReason);
            var addGt2 = CampaignSourceAuthorityState.Gt2CorrectionActionId("adjust", 2, 1, 1);
            directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, addGt2, CampaignActionAudience.Axis)).SuccessorSession!;
        }
        directV12 = CampaignExercises.SubmitSourceGt2Correction(directV12, Submission(directV12, finishGt2, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(directV12.SourceAuthority!.Gt2Correction!.IsFinished);
        Assert.Equal(rngBeforeGt2, directV12.SourceAuthority.RandomState);
        Assert.Equal(worldBeforeGt2, directV12.SourceAuthority.World.ContentIdentity);
        directV12 = CampaignExercises.AdmitSourceCwPriorityCatalog(directV12).SuccessorSession!;
        Assert.Equal(28, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        directV12 = CampaignExercises.PromoteSourceCwPriorityWorld(directV12).SuccessorSession!;
        Assert.Equal(29, CampaignExercises.QueryCheckpoint(directV12).ContractVersion);
        Assert.Equal(22, directV12.SourceAuthority!.World.World.Elements.Count);
        var frenchAdd = CampaignSourceAuthorityState.CwPriorityCatalogActionId("1-french-motor-marines", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignExercises.SubmitSourceCwPriorityCatalog(directV12, Submission(directV12, frenchAdd, CampaignActionAudience.Axis)).RejectionReason);
        directV12 = CampaignExercises.SubmitSourceCwPriorityCatalog(directV12,
            Submission(directV12, frenchAdd, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(1, directV12.SourceAuthority!.CwPriorityCatalog!.FrenchPoints);
        Assert.Equal(1, directV12.SourceAuthority.CwPriorityCatalog.Remaining(directV12.SourceAuthority, "cw-c3926-attached"));
        var frenchRemove = CampaignSourceAuthorityState.CwPriorityCatalogActionId("1-french-motor-marines", -1);
        directV12 = CampaignExercises.SubmitSourceCwPriorityCatalog(directV12,
            Submission(directV12, frenchRemove, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(2, directV12.SourceAuthority!.CwPriorityCatalog!.Remaining(directV12.SourceAuthority, "cw-c3926-attached"));
        frenchFirst = CampaignExercises.AdmitSourceCwPriorityCatalog(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.PromoteSourceCwPriorityWorld(frenchFirst).SuccessorSession!;
        frenchFirst = CampaignExercises.AdmitSourceCwFreeGuns(frenchFirst).SuccessorSession!;
        Assert.Equal(31, frenchFirst.SourceAuthority!.ProfileVersion);
        var freeGunIds = frenchFirst.SourceAuthority!.CwFreeGuns!.PendingIds.ToArray();
        var gunCairoHex = 0UL == 14 ? "e1730" : "e1830";
        foreach (var id in freeGunIds)
        {
            var placeGun = CampaignSourceAuthorityState.CwFreeGunPlacementActionId(id, gunCairoHex);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                CampaignExercises.PlaceSourceCwFreeGun(frenchFirst, Submission(frenchFirst, placeGun, CampaignActionAudience.Axis)).RejectionReason);
            frenchFirst = CampaignExercises.PlaceSourceCwFreeGun(frenchFirst,
                Submission(frenchFirst, placeGun, CampaignActionAudience.Commonwealth)).SuccessorSession!;
            frenchFirst = CampaignExercises.PromoteSourceCwFreeGunsWorld(frenchFirst).SuccessorSession!;
        }
        Assert.Equal(32, frenchFirst.SourceAuthority!.ProfileVersion);
        _ = ContentSourceFourthIndianPackArtifact.Create(frenchFirst.SourceAuthority!.CwFreeGunsWorld!.Content);
        var indianResult = CampaignExercises.AdmitSourceFourthIndianWorld(frenchFirst);
        Assert.True(indianResult.IsAccepted);
        var indian = indianResult.SuccessorSession!;
        var grouped = indian.SourceAuthority!.GroupedWorld!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(indian.SourceAuthority.SerializeLegacyCheckpoint()),
            indian.SourceAuthority.DigestLegacyCheckpoint());
        var beforeMinor = indian.SourceAuthority.SerializeCheckpoint();
        var pendingBeforeMinor = indian.SourceAuthority.World.PendingArrivals;
        var randomBeforeMinor = indian.SourceAuthority.RandomState;
        var minorResult = CampaignExercises.ActivateSourceAxisMinorGarrisons(indian);
        Assert.True(minorResult.IsAccepted);
        var minor = minorResult.SuccessorSession!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(minor.SourceAuthority!.CwFreeGunsWorld!.SerializeCanonical()),
            minor.SourceAuthority.CwFreeGunsWorld.Hash());
        Assert.Equal(CampaignPregameSourceCreation.Digest(minor.SourceAuthority!.SerializeLegacyCheckpoint()),
            minor.SourceAuthority.DigestLegacyCheckpoint());
        Assert.Equal(beforeMinor, indian.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.AxisMinorGarrisonsCheckpointContractVersion, minor.SourceAuthority!.ProfileVersion);
        Assert.Equal(randomBeforeMinor, minor.SourceAuthority.RandomState);
        Assert.Equal(pendingBeforeMinor, minor.SourceAuthority.World.PendingArrivals);
        Assert.Equal(grouped.ContentHash, minor.SourceAuthority.GroupedWorld!.ContentHash);
        Assert.Equal(grouped.Groups.Count, minor.SourceAuthority.GroupedWorld.Groups.Count);
        Assert.Equal(0, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("fort-maddalena-garrison", "c3019"));
        Assert.Equal(1, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("fort-maddalena-garrison", "c3020"));
        Assert.Equal(0, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("mechili-garrison", "b4921"));
        Assert.Equal(1, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt("mechili-garrison", "b4922"));
        foreach (var unit in ContentSourceAxisMinorGarrisonsPackArtifact.Units)
        {
            Assert.Equal(0, ContentSourceAxisMinorGarrisonsPackArtifact.StackAt(unit.Id, unit.Location));
            var element = Assert.Single(minor.SourceAuthority.World.World.Elements, value => value.ElementId == unit.Id);
            Assert.Equal(unit.Location, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
            Assert.Single(minor.SourceAuthority.World.World.Representations, value =>
                value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
                && value.CurrentLocationId == unit.Location && value.BoundElementIds.SequenceEqual([unit.Id]));
            Assert.Contains(minor.SourceAuthority.Battlefield!.Entries, value => value.ElementId == unit.Id
                && value.AuthorityStatus == "source-axis-minor-active");
        }
        // Use the actual activated counters, not source-evidence-only RTR or
        // Hussars. The two observers are independent counters, not formations.
        var minorContactWorld = CampaignSourcePlayableWorld.Create(minor.SourceAuthority);
        var activePair = CampaignSourcePolishArrivalContacts.EffectiveCounters(minorContactWorld)
            .Where(value => value.RepresentationId is "axis-minor.scheferzen-garrison" or "independent-setup.2-rifle-brigade")
            .ToArray();
        Assert.Equal(2, activePair.Length);
        Assert.All(activePair, value => Assert.Equal(CampaignContactObserverKind.IndependentCounter, value.ObserverKind));
        var pairAxis = new CampaignContactKnowledge(minor.SourceAuthority.Request.CampaignId, LandSide.Axis);
        var pairCw = new CampaignContactKnowledge(minor.SourceAuthority.Request.CampaignId, LandSide.Commonwealth);
        var axisPairContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(activePair, 1, pairAxis));
        var cwPairContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(activePair, 1, pairCw));
        Assert.Equal(CampaignContactKnowledge.IndependentObserverContractVersion, axisPairContact.ContractVersion);
        Assert.Equal(CampaignContactKnowledge.IndependentObserverContractVersion, cwPairContact.ContractVersion);
        var axisPairBytes = CampaignContactTransitionCodec.SerializeCanonical(axisPairContact);
        Assert.Equal(axisPairContact, CampaignContactTransitionCodec.DeserializeCanonical(axisPairBytes));
        Assert.Equal(axisPairBytes, CampaignContactTransitionCodec.SerializeCanonical(Assert.Single(
            CampaignSourceInitialContacts.DeriveCounters(activePair, 1,
                new CampaignContactKnowledge(minor.SourceAuthority.Request.CampaignId, LandSide.Axis)))));
        pairAxis.Apply(axisPairContact);
        pairCw.Apply(cwPairContact);
        Assert.Empty(pairAxis.ObserveFor(axisPairContact.ObserverFormationId));
        var ownCounterView = CampaignIndependentContactObservationV2.Project(minorContactWorld, pairAxis,
            "axis-minor.scheferzen-garrison");
        Assert.Single(ownCounterView.Contacts);
        Assert.DoesNotContain("independent-setup.2-rifle-brigade", System.Text.Encoding.UTF8.GetString(ownCounterView.SerializeCanonical()));
        Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(minorContactWorld,
            pairCw, "axis-minor.scheferzen-garrison"));
        var repeatedPairContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(activePair, 2, pairAxis));
        Assert.Equal(axisPairContact.ContactHandle, repeatedPairContact.ContactHandle);
        Assert.True(CampaignExercises.Reconstruct(minor).IsVerified);
        var compactMinorResult = CampaignExercises.AdmitSourceCompactCheckpoint(minor);
        Assert.True(compactMinorResult.IsAccepted);
        var compactMinor = compactMinorResult.SuccessorSession!;
        var compactMinorBytes = compactMinor.SourceAuthority!.SerializeCheckpoint();
        using (var compactMinorJson = JsonDocument.Parse(compactMinorBytes))
        {
            var baseBytes = compactMinorJson.RootElement.GetProperty("baseCheckpoint").GetBytesFromBase64();
            var artifactBytes = compactMinor.SourceAuthority.CwFreeGunsWorld!.MinorGarrisonContent!.CanonicalBytes;
            Console.WriteLine($"minor checkpoint bytes: compact={compactMinorBytes.Length}, base={baseBytes.Length}, artifact={artifactBytes.Length}");
            Assert.True(compactMinorBytes.Length < 16_384, $"Compact minor checkpoint was {compactMinorBytes.Length} bytes; base {baseBytes.Length}, artifact {artifactBytes.Length}.");
            Assert.False(compactMinorJson.RootElement.TryGetProperty("minorGarrisonContent", out _));
            Assert.Equal(compactMinor.SourceAuthority.World.ContentIdentity.Hash,
                compactMinorJson.RootElement.GetProperty("minorGarrisonContentHash").GetString());
        }
        Assert.True(CampaignExercises.Reconstruct(compactMinor).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceAxisGarrisonRemainder(minor).IsAccepted);
        var oldMinorCheckpoint = compactMinor.SourceAuthority.SerializeCheckpoint();
        var remainderResult = CampaignExercises.ActivateSourceAxisGarrisonRemainder(compactMinor);
        Assert.True(remainderResult.IsAccepted);
        var remainder = remainderResult.SuccessorSession!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(remainder.SourceAuthority!.CwFreeGunsWorld!.SerializeCanonical()),
            remainder.SourceAuthority.CwFreeGunsWorld.Hash());
        Console.WriteLine($"garrison remainder content hash: {remainder.SourceAuthority!.World.ContentIdentity.Hash}");
        var remainderCheckpointBytes = remainder.SourceAuthority!.SerializeCheckpoint();
        Assert.True(remainderCheckpointBytes.Length < 24 * 1024 * 1024,
            $"Legacy garrison remainder checkpoint was {remainderCheckpointBytes.Length} bytes.");
        Assert.Equal(oldMinorCheckpoint, compactMinor.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.AxisGarrisonRemainderCheckpointContractVersion, remainder.SourceAuthority.ProfileVersion);
        Assert.Equal(ContentSourceAxisGarrisonRemainderPackArtifact.ContentHash, remainder.SourceAuthority.World.ContentIdentity.Hash);
        Assert.Equal(minor.SourceAuthority.RandomState, remainder.SourceAuthority.RandomState);
        Assert.Equal(minor.SourceAuthority.World.PendingArrivals, remainder.SourceAuthority.World.PendingArrivals);
        Assert.Equal(minor.SourceAuthority.GroupedWorld!.ContentHash, remainder.SourceAuthority.GroupedWorld!.ContentHash);
        Assert.Equal(ExpectedGarrisonRemainderIds,
            ContentSourceAxisGarrisonRemainderPackArtifact.Units.Select(value => value.Id).Order(StringComparer.Ordinal));
        foreach (var unit in ContentSourceAxisGarrisonRemainderPackArtifact.Units)
        {
            var element = Assert.Single(remainder.SourceAuthority.World.World.Elements, value => value.ElementId == unit.Id);
            var sourceComponent = ContentSourceAxisGarrisonRemainderPackArtifact.Facts(unit.Id).Components.Single();
            Assert.Equal(unit.Location, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
            Assert.Equal(sourceComponent.ComponentId, element.Components[0].ComponentId);
            Assert.Equal(0, ContentSourceAxisGarrisonRemainderPackArtifact.StackAt(unit.Id, unit.Location));
            Assert.Single(remainder.SourceAuthority.World.World.Representations, value =>
                value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement
                && value.CurrentLocationId == unit.Location && value.BoundElementIds.SequenceEqual([unit.Id]));
            Assert.Contains(remainder.SourceAuthority.Battlefield!.Entries, value => value.ElementId == unit.Id
                && value.AuthorityStatus == "source-axis-garrison-remainder-active");
        }
        var remainderContactWorld = CampaignSourcePlayableWorld.Create(remainder.SourceAuthority);
        var remainderCounters = CampaignSourcePolishArrivalContacts.EffectiveCounters(remainderContactWorld);
        var remainderAxisKnowledge = new CampaignContactKnowledge(remainder.SourceAuthority.Request.CampaignId, LandSide.Axis);
        var remainderCwKnowledge = new CampaignContactKnowledge(remainder.SourceAuthority.Request.CampaignId, LandSide.Commonwealth);
        // Both counters come from the admitted source world. The chart proves
        // Soluch and Matruh are separated without inferring a playable seam.
        var soluchAndIndian = remainderCounters.Where(value => value.RepresentationId is
            "axis-garrison.soluch-antitank-company" or "4-indian-division").ToArray();
        Assert.Equal(2, soluchAndIndian.Length);
        Assert.Empty(CampaignSourceInitialContacts.DeriveCounters(soluchAndIndian, 1, remainderAxisKnowledge));
        Assert.Empty(CampaignSourceInitialContacts.DeriveCounters(soluchAndIndian, 1, remainderCwKnowledge));
        Assert.Equal(6, ContentSourceAxisGarrisonRemainderPackArtifact.Units.Count);
        foreach (var removedId in ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds)
        {
            var observerId = "axis-garrison." + removedId;
            Assert.DoesNotContain(remainderCounters, value => value.RepresentationId == observerId);
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(remainderContactWorld, remainderAxisKnowledge, observerId));
        }
        foreach (var unit in ContentSourceAxisGarrisonRemainderPackArtifact.Units)
        {
            var id = "axis-garrison." + unit.Id;
            var counter = Assert.Single(remainderCounters, value => value.RepresentationId == id);
            Assert.Equal(CampaignContactObserverKind.IndependentCounter, counter.ObserverKind);
            Assert.Equal(id, counter.ObserverFormationId);
            Assert.Empty(CampaignIndependentContactObservationV2.Project(remainderContactWorld, remainderAxisKnowledge, id).Contacts);
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(remainderContactWorld, remainderCwKnowledge, id));
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(minorContactWorld, pairAxis, id));
        }
        Assert.Equal(1, ContentSourceAxisGarrisonRemainderPackArtifact.StackAt("soluch-artillery-regiment", "a4131"));
        Assert.Equal(1, ContentSourceAxisGarrisonRemainderPackArtifact.StackAt("giarabub-machine-gun", "c1015"));
        Assert.DoesNotContain(remainder.SourceAuthority.World.World.Elements, value => value.ElementId == "giarabub-motor-machine-gun");
        Assert.Contains(remainder.SourceAuthority.Battlefield!.Entries, value => value.ElementId == "giarabub-motor-machine-gun"
            && value.AuthorityStatus == "source-evidence");
        foreach (var id in ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds)
        {
            Assert.DoesNotContain(remainder.SourceAuthority.World.World.Elements, value => value.ElementId == id);
            Assert.DoesNotContain(remainder.SourceAuthority.World.World.Representations, value => value.BoundElementIds.Contains(id));
            var removedEntry = Assert.Single(remainder.SourceAuthority.Battlefield!.Entries, value => value.ElementId == id
                && value.AuthorityStatus == "source-land-only-aa-removed");
            Assert.DoesNotContain(CampaignSourcePlayableWorld.Create(remainder.SourceAuthority).UnresolvedAt(removedEntry.LocationId!),
                value => value.ElementId == id);
        }
        using (var contentDocument = JsonDocument.Parse(remainder.SourceAuthority.CwFreeGunsWorld!.GarrisonRemainderContent!.CanonicalBytes))
        {
            var removedIds = contentDocument.RootElement.GetProperty("removedAntiAirUnits").EnumerateArray()
                .Select(value => value.GetString()!).ToArray();
            Assert.Equal(ContentSourceAxisGarrisonRemainderPackArtifact.RemovedAntiAirIds, removedIds);
            Assert.Equal("land:32.83(1)-italian-uu-removed-from-play", contentDocument.RootElement.GetProperty("antiAirTreatment").GetString());
        }
        Assert.True(CampaignExercises.Reconstruct(remainder).IsVerified);
        var remainderHistory = remainder.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var remainderEvent = JsonDocument.Parse(remainderHistory[^1]))
        {
            var worldHash = remainderEvent.RootElement.GetProperty("worldHash").GetString()!;
            var changedHash = worldHash[..^1] + (worldHash[^1] == '0' ? '1' : '0');
            remainderHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(remainderHistory[^1])
                .Replace(worldHash, changedHash, StringComparison.Ordinal));
        }
        var tamperedRemainder = new ExerciseSession(remainder.Pregame!, remainder.CurrentContacts,
            remainderHistory, remainder.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(tamperedRemainder).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceAxisGarrisonRemainder(remainder).IsAccepted);
        var compactRemainder = remainder;
        var compactRemainderBytes = compactRemainder.SourceAuthority!.SerializeCheckpoint();
        using (var remainderCheckpoint = JsonDocument.Parse(compactRemainderBytes))
        {
            Assert.True(compactRemainderBytes.Length < 16_384, $"Compact remainder checkpoint was {compactRemainderBytes.Length} bytes.");
            Assert.False(remainderCheckpoint.RootElement.TryGetProperty("garrisonRemainderContent", out _));
            Assert.Equal(compactRemainder.SourceAuthority.World.ContentIdentity.Hash,
                remainderCheckpoint.RootElement.GetProperty("garrisonRemainderContentHash").GetString());
        }
        Assert.True(CampaignExercises.Reconstruct(compactRemainder).IsVerified);
        var priorRemainderCheckpoint = remainder.SourceAuthority.SerializeCheckpoint();
        var priorAxisMotorization = remainder.SourceAuthority.AxisMotorization.Hash();
        var motorMgResult = CampaignExercises.ActivateSourceGiarabubMotorMachineGun(remainder);
        Assert.True(motorMgResult.IsAccepted);
        var motorMg = motorMgResult.SuccessorSession!;
        Assert.Equal(CampaignPregameSourceCreation.Digest(motorMg.SourceAuthority!.CwFreeGunsWorld!.SerializeCanonical()),
            motorMg.SourceAuthority.CwFreeGunsWorld.Hash());
        Assert.Equal(priorRemainderCheckpoint, remainder.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.GiarabubMotorMachineGunCheckpointContractVersion, motorMg.SourceAuthority!.ProfileVersion);
        Assert.Equal(priorAxisMotorization, motorMg.SourceAuthority.AxisMotorization.Hash());
        Assert.Equal(remainder.SourceAuthority.RandomState, motorMg.SourceAuthority.RandomState);
        Assert.Equal(remainder.SourceAuthority.World.PendingArrivals, motorMg.SourceAuthority.World.PendingArrivals);
        var motorMgElement = Assert.Single(motorMg.SourceAuthority.World.World.Elements,
            value => value.ElementId == ContentSourceGiarabubMotorMachineGunArtifact.ElementId);
        Assert.Equal("c1014", motorMgElement.CurrentLocationId);
        Assert.Equal(1, Assert.Single(motorMgElement.Components).CurrentToe);
        Assert.Single(motorMg.SourceAuthority.World.World.Representations, value =>
            value.RepresentationId == "axis-garrison.giarabub-motor-machine-gun"
            && value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement);
        var motorMgContent = motorMg.SourceAuthority.CwFreeGunsWorld!.MotorMachineGunContent!;
        Console.WriteLine($"giarabub motor MG content hash: {motorMgContent.Identity.Hash}");
        Assert.Equal(ContentSourceGiarabubMotorMachineGunArtifact.ContentHash, motorMgContent.Identity.Hash);
        using (var document = JsonDocument.Parse(motorMgContent.CanonicalBytes))
        {
            var root = document.RootElement;
            Assert.Equal("8+", root.GetProperty("printedCapabilityPoints").GetString());
            Assert.Equal(JsonValueKind.Null, root.GetProperty("effectiveCapabilityPoints").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("mobilityId").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("motorPoolId").ValueKind);
            Assert.Equal(JsonValueKind.Null, root.GetProperty("assignedMotorPoints").ValueKind);
        }
        var motorMgWorld = CampaignSourcePlayableWorld.Create(motorMg.SourceAuthority);
        Assert.Contains("giarabub-motor-mg-historical-motorization-priority-unresolved", motorMgWorld.ReadinessBlockers());
        Assert.Contains("giarabub-motor-mg-capability-and-mobility-unresolved", motorMgWorld.ReadinessBlockers());
        var motorMgCounter = Assert.Single(CampaignSourcePolishArrivalContacts.EffectiveCounters(motorMgWorld),
            value => value.RepresentationId == "axis-garrison.giarabub-motor-machine-gun");
        Assert.Equal(CampaignContactObserverKind.IndependentCounter, motorMgCounter.ObserverKind);
        Assert.Equal(motorMgCounter.RepresentationId, motorMgCounter.ObserverFormationId);
        var motorMgAxisKnowledge = new CampaignContactKnowledge(motorMg.SourceAuthority.Request.CampaignId, LandSide.Axis);
        var motorMgCwKnowledge = new CampaignContactKnowledge(motorMg.SourceAuthority.Request.CampaignId, LandSide.Commonwealth);
        Assert.Empty(CampaignIndependentContactObservationV2.Project(motorMgWorld, motorMgAxisKnowledge, motorMgCounter.RepresentationId).Contacts);
        Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(motorMgWorld, motorMgCwKnowledge, motorMgCounter.RepresentationId));
        Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(remainderContactWorld, motorMgAxisKnowledge, motorMgCounter.RepresentationId));
        var motorAndIndian = CampaignSourcePolishArrivalContacts.EffectiveCounters(motorMgWorld)
            .Where(value => value.RepresentationId is "axis-garrison.giarabub-motor-machine-gun" or "4-indian-division").ToArray();
        Assert.Equal(2, motorAndIndian.Length);
        Assert.Empty(CampaignSourceInitialContacts.DeriveCounters(motorAndIndian, 1, motorMgAxisKnowledge));
        // Exercise the verified Giarabub hexside using the real counters; this
        // hypothetical position is not an admitted campaign move.
        var adjacentIndian = motorAndIndian.Single(value => value.RepresentationId == "4-indian-division") with { LocationId = "c1015" };
        var motorContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters([motorMgCounter, adjacentIndian], 1, motorMgAxisKnowledge));
        Assert.Equal(CampaignContactObserverKind.IndependentCounter, motorContact.ObserverKind);
        Assert.Equal(motorMgCounter.RepresentationId, motorContact.ObserverFormationId);
        Assert.False(motorMgWorld.IsGameplayReady);
        Assert.False(CampaignExercises.StartSourceTurn(motorMg).IsAccepted);
        Assert.True(CampaignExercises.Reconstruct(motorMg).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceGiarabubMotorMachineGun(motorMg).IsAccepted);
        var motorMgCheckpoint = motorMg.SourceAuthority.SerializeCheckpoint();
        Assert.True(motorMgCheckpoint.Length < 16_384, $"Compact motor MG checkpoint was {motorMgCheckpoint.Length} bytes.");
        using (var document = JsonDocument.Parse(motorMgCheckpoint))
            Assert.Equal(motorMgContent.Identity.Hash, document.RootElement.GetProperty("motorMachineGunContentHash").GetString());
        var cwFrontlineResult = CampaignExercises.ActivateSourceCwFrontline(motorMg);
        Assert.True(cwFrontlineResult.IsAccepted);
        var cwFrontline = cwFrontlineResult.SuccessorSession!;
        var cwAuthority = cwFrontline.SourceAuthority!;
        Assert.True(CampaignExercises.IsSourceCwFrontlineActivated(cwFrontline));
        Assert.Equal(motorMgCheckpoint, motorMg.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(CampaignSourceAuthorityState.CwFrontlineCheckpointContractVersion, cwAuthority.ProfileVersion);
        Assert.Equal(motorMg.SourceAuthority.CommonwealthMotorization.Hash(), cwAuthority.CommonwealthMotorization.Hash());
        Assert.Equal(motorMg.SourceAuthority.AxisMotorization.Hash(), cwAuthority.AxisMotorization.Hash());
        Assert.Equal(motorMg.SourceAuthority.RandomState, cwAuthority.RandomState);
        Assert.Equal(motorMg.SourceAuthority.World.PendingArrivals, cwAuthority.World.PendingArrivals);
        foreach (var unit in ContentSourceCwFrontlineArtifact.Units)
        {
            var element = Assert.Single(cwAuthority.World.World.Elements, value => value.ElementId == unit.Id);
            Assert.Equal(unit.Location, element.CurrentLocationId);
            Assert.Equal(unit.Toe, Assert.Single(element.Components).CurrentToe);
            Assert.Single(cwAuthority.World.World.Representations, value => value.RepresentationId == "cw-frontline." + unit.Id
                && value.BindingKind == CampaignMapRepresentationBindingKind.IndependentElement);
            Assert.Contains(cwAuthority.Battlefield!.Entries, value => value.ElementId == unit.Id
                && value.AuthorityStatus == "source-cw-frontline-active-initial-loads-unresolved");
        }
        var cwNativeWorld = cwAuthority.CwFreeGunsWorld!;
        var freshHashWorld = remainder.SourceAuthority.CwFreeGunsWorld!.ActivateGiarabubMotorMachineGun(
            remainder.SourceAuthority.AxisGarrisons!, remainder.SourceAuthority.Battlefield!);
        var canonicalWorldHash = CampaignPregameSourceCreation.Digest(freshHashWorld.SerializeCanonical());
        var concurrentWorldHashes = new string[8];
        Parallel.For(0, concurrentWorldHashes.Length, index => concurrentWorldHashes[index] = freshHashWorld.Hash());
        Assert.All(concurrentWorldHashes, hash => Assert.Equal(canonicalWorldHash, hash));
        var cwContent = cwNativeWorld.CwFrontlineContent!;
        var cwContactWorld = CampaignSourcePlayableWorld.Create(cwAuthority);
        var cwEffectiveCounters = CampaignSourcePolishArrivalContacts.EffectiveCounters(cwContactWorld);
        foreach (var (cwId, axisId) in new[]
        {
            ("cw-frontline.1-rtr", "axis-minor.scheferzen-garrison"),
            ("cw-frontline.11-hussars", "axis-minor.fort-maddalena-garrison"),
        })
        {
            var actualPair = cwEffectiveCounters.Where(value => value.RepresentationId == cwId || value.RepresentationId == axisId).ToArray();
            Assert.Equal(2, actualPair.Length);
            Assert.All(actualPair, value => Assert.Equal(CampaignContactObserverKind.IndependentCounter, value.ObserverKind));
            var cwKnowledge = new CampaignContactKnowledge(cwAuthority.Request.CampaignId, LandSide.Commonwealth);
            var axisKnowledge = new CampaignContactKnowledge(cwAuthority.Request.CampaignId, LandSide.Axis);
            var cwContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(actualPair, 1, cwKnowledge));
            var axisContact = Assert.Single(CampaignSourceInitialContacts.DeriveCounters(actualPair, 1, axisKnowledge));
            Assert.Equal(cwId, cwContact.ObserverFormationId);
            Assert.Equal(axisId, axisContact.ObserverFormationId);
            var serialized = CampaignContactTransitionCodec.SerializeCanonical(cwContact);
            Assert.Equal(cwContact, CampaignContactTransitionCodec.DeserializeCanonical(serialized));
            Assert.Equal(serialized, CampaignContactTransitionCodec.SerializeCanonical(Assert.Single(
                CampaignSourceInitialContacts.DeriveCounters(actualPair, 1,
                    new CampaignContactKnowledge(cwAuthority.Request.CampaignId, LandSide.Commonwealth)))));
            cwKnowledge.Apply(cwContact);
            axisKnowledge.Apply(axisContact);
            var cwView = CampaignIndependentContactObservationV2.Project(cwContactWorld, cwKnowledge, cwId);
            Assert.Single(cwView.Contacts);
            Assert.DoesNotContain(axisId, System.Text.Encoding.UTF8.GetString(cwView.SerializeCanonical()));
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(cwContactWorld, axisKnowledge, cwId));
            Assert.Throws<InvalidOperationException>(() => CampaignIndependentContactObservationV2.Project(
                CampaignSourcePlayableWorld.Create(motorMg.SourceAuthority), cwKnowledge, cwId));
            Assert.Equal(cwContact.ContactHandle, Assert.Single(CampaignSourceInitialContacts.DeriveCounters(actualPair, 2, cwKnowledge)).ContactHandle);
        }
        Assert.Equal(ContentSourceCwFrontlineArtifact.ContentHash, cwContent.Identity.Hash);
        Assert.Equal(cwContent.Identity, ContentSourceCwFrontlineArtifact.Read(cwContent.CanonicalBytes).Identity);
        using (var document = JsonDocument.Parse(cwContent.CanonicalBytes))
            foreach (var unit in document.RootElement.GetProperty("units").EnumerateArray())
            {
                if (unit.GetProperty("elementId").GetString() == "11-hussars")
                    Assert.Equal(1, unit.GetProperty("chartArmorRating").GetInt32());
                Assert.Equal(JsonValueKind.Null, unit.GetProperty("initialCargo").ValueKind);
                Assert.Equal(JsonValueKind.Null, unit.GetProperty("parentFormationId").ValueKind);
            }
        Assert.Contains("cw-frontline-initial-loads-unresolved", CampaignSourcePlayableWorld.Create(cwAuthority).ReadinessBlockers());
        Assert.False(CampaignExercises.StartSourceTurn(cwFrontline).IsAccepted);
        Assert.False(CampaignExercises.ActivateSourceCwFrontline(cwFrontline).IsAccepted);
        var cwCheckpoint = cwAuthority.SerializeCheckpoint();
        Assert.True(cwCheckpoint.Length < 16_384, $"Compact CW frontline checkpoint was {cwCheckpoint.Length} bytes.");
        using (var document = JsonDocument.Parse(cwCheckpoint))
            Assert.Equal(cwContent.Identity.Hash, document.RootElement.GetProperty("cwFrontlineContentHash").GetString());
        Assert.True(CampaignExercises.Reconstruct(cwFrontline).IsVerified);
        var cwHistory = cwFrontline.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var document = JsonDocument.Parse(cwHistory[^1]))
        {
            var hash = document.RootElement.GetProperty("worldHash").GetString()!;
            var changed = hash[..^1] + (hash[^1] == '0' ? '1' : '0');
            cwHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(cwHistory[^1])
                .Replace(hash, changed, StringComparison.Ordinal));
        }
        Assert.False(CampaignExercises.Reconstruct(new ExerciseSession(cwFrontline.Pregame!, cwFrontline.CurrentContacts,
            cwHistory, cwFrontline.SourceAuthority)).IsVerified);
        var motorMgHistory = motorMg.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var document = JsonDocument.Parse(motorMgHistory[^1]))
        {
            var hash = document.RootElement.GetProperty("worldHash").GetString()!;
            var changed = hash[..^1] + (hash[^1] == '0' ? '1' : '0');
            motorMgHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(motorMgHistory[^1])
                .Replace(hash, changed, StringComparison.Ordinal));
        }
        Assert.False(CampaignExercises.Reconstruct(new ExerciseSession(motorMg.Pregame!, motorMg.CurrentContacts,
            motorMgHistory, motorMg.SourceAuthority)).IsVerified);
        var minorHistory = minor.PregameHistory.Select(value => value.ToArray()).ToArray();
        using (var eventDocument = JsonDocument.Parse(minorHistory[^1]))
        {
            var worldHash = eventDocument.RootElement.GetProperty("worldHash").GetString()!;
            var changedHash = worldHash[..^1] + (worldHash[^1] == '0' ? '1' : '0');
            minorHistory[^1] = System.Text.Encoding.UTF8.GetBytes(System.Text.Encoding.UTF8.GetString(minorHistory[^1])
                .Replace(worldHash, changedHash, StringComparison.Ordinal));
        }
        var tamperedMinor = new ExerciseSession(minor.Pregame!, minor.CurrentContacts, minorHistory, minor.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(tamperedMinor).IsVerified);
        Assert.False(CampaignExercises.ActivateSourceAxisMinorGarrisons(minor).IsAccepted);
        var minorFinish = CampaignExercises.Query(minor, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var minorFinished = CampaignExercises.Submit(minor,
            Submission(minor, minorFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.True(CampaignExercises.Reconstruct(minorFinished).IsVerified);
        Assert.Equal(minor.SourceAuthority.World.ContentIdentity, minorFinished.SourceAuthority!.World.ContentIdentity);
        Assert.Equal(ContentSourceFourthIndianPackArtifact.ContentHash, grouped.ContentHash);
        Assert.Equal(11, grouped.Groups.Single().Members.Count);
        var originalGroup = grouped.Groups.Single();
        var forgedMembers = originalGroup.Members.Select(value => value.ElementId == "25-field-artillery"
            ? value with { AdditionalComponents = Array.AsReadOnly(new[] { new CampaignGroupedComponent("extra-gun", 1) }) } : value);
        var forgedGroup = new CampaignGroupedFormation(originalGroup.GroupId, originalGroup.SideId, originalGroup.RootId,
            originalGroup.LocationId, originalGroup.StackValue, originalGroup.MotorPool, forgedMembers, originalGroup.DispositionConfirmed);
        var forgedWorld = CampaignGroupedWorld.Create(grouped.Prior, grouped.ContentHash, [forgedGroup]);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceFourthIndianDefinition.Validate(forgedWorld));
        var fieldArtillery = grouped.Groups.Single().Members.Single(value => value.ElementId == "25-field-artillery");
        Assert.Equal("3-7-inch-howitzer", fieldArtillery.ComponentType);
        Assert.Equal(6, fieldArtillery.ComponentToe);
        Assert.Equal(grouped.Prior.Initialization.CapabilityPointsExpended, fieldArtillery.CapabilityPointsExpended);
        Assert.Equal(11, indian.SourceAuthority.Battlefield!.Entries.Count(value => value.LocationId == "d3615" && value.AuthorityStatus == "source-fourth-indian-proposed"));
        Assert.Null(grouped.StackAt("commonwealth", "d3615", _ => null));
        Assert.Contains(grouped.Prior.PendingArrivals, value => value.GroupId == "graziani-7-indian-brigade");
        var priorityAction = CampaignSourceAuthorityState.FourthIndianMotorActionId("central-india-horse", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Axis)).RejectionReason);
        Assert.False(grouped.Groups.Single().DispositionConfirmed);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        var confirm = CampaignExercises.Query(indian, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "fourth-indian-motorization");
        Assert.Contains("confirm", confirm.ChoiceJson, StringComparison.Ordinal);
        var ownerDraft = System.Text.Encoding.UTF8.GetString(CampaignExercises.QuerySourceFourthIndianDraft(indian, CampaignActionAudience.Commonwealth).VisibleBytes);
        Assert.Contains("provisional-source-default-alternatives-unavailable", ownerDraft, StringComparison.Ordinal);
        Assert.Contains("5-indian-brigade-hq", ownerDraft, StringComparison.Ordinal);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(indian,
            Submission(indian, confirm.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var confirmCommand = Submission(indian, confirm.ActionId, CampaignActionAudience.Commonwealth);
        indian = CampaignExercises.Submit(indian, confirmCommand).SuccessorSession!;
        Assert.True(indian.SourceAuthority!.GroupedWorld!.Groups.Single().DispositionConfirmed);
        Assert.Equal(5, indian.SourceAuthority.GroupedWorld.StackAt("commonwealth", "d3615", _ => null));
        Assert.Equal(11, indian.SourceAuthority.Battlefield!.Entries.Count(value => value.LocationId == "d3615" && value.AuthorityStatus == "source-fourth-indian-grouped"));
        Assert.Equal(34, indian.SourceAuthority.ProfileVersion);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(indian, confirmCommand).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        indian = CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, priorityAction, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(21, indian.SourceAuthority!.GroupedWorld!.RemainingMotorPoints("4-indian-division"));
        var footAction = CampaignSourceAuthorityState.FourthIndianMotorActionId("1-royal-fusiliers", 1);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.SubmitSourceFourthIndian(indian,
            Submission(indian, footAction, CampaignActionAudience.Commonwealth)).RejectionReason);
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        var minorBeforePromotions = CampaignExercises.ActivateSourceAxisMinorGarrisons(indian).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.AdmitSourceCompactCheckpoint(minorBeforePromotions).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.ActivateSourceAxisGarrisonRemainder(minorBeforePromotions).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.ActivateSourceGiarabubMotorMachineGun(minorBeforePromotions).SuccessorSession!;
        var minorIdentity = minorBeforePromotions.SourceAuthority!.World.ContentIdentity;
        minorBeforePromotions = CampaignExercises.SubmitSourceMalettiHierarchy(minorBeforePromotions,
            Submission(minorBeforePromotions, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        var minorMaletti = CampaignExercises.PromoteSourceCertifiedMalettiWorld(minorBeforePromotions);
        Assert.True(minorMaletti.IsAccepted);
        minorBeforePromotions = minorMaletti.SuccessorSession!;
        minorBeforePromotions = CampaignExercises.AdmitSourceMatruhFormation(minorBeforePromotions).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.SubmitSourceMatruhFormation(minorBeforePromotions,
            Submission(minorBeforePromotions, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        minorBeforePromotions = CampaignExercises.SubmitSourceMatruhFormation(minorBeforePromotions,
            Submission(minorBeforePromotions, matruhDetach, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var minorMatruh = CampaignExercises.PromoteSourceMatruhDetachedWorld(minorBeforePromotions);
        Assert.True(minorMatruh.IsAccepted);
        minorBeforePromotions = minorMatruh.SuccessorSession!;
        Assert.Equal(minorIdentity, minorBeforePromotions.SourceAuthority!.World.ContentIdentity);
        Assert.All(ContentSourceAxisMinorGarrisonsPackArtifact.Units, unit =>
            Assert.Contains(minorBeforePromotions.SourceAuthority.World.World.Elements, element => element.ElementId == unit.Id));
        Assert.True(CampaignExercises.Reconstruct(minorBeforePromotions).IsVerified);
        var indianWithoutMalettiDetachment = indian;
        indian = CampaignExercises.SubmitSourceMalettiHierarchy(indian,
            Submission(indian, certifiedDetach, CampaignActionAudience.Axis)).SuccessorSession!;
        indian = CampaignExercises.PromoteSourceCertifiedMalettiWorld(indian).SuccessorSession!;
        Assert.Equal(21, indian.SourceAuthority!.GroupedWorld!.RemainingMotorPoints("4-indian-division"));
        Assert.True(CampaignExercises.Reconstruct(indian).IsVerified);
        var matruhBeforeDetach = CampaignExercises.AdmitSourceMatruhFormation(indian).SuccessorSession!;
        matruhBeforeDetach = CampaignExercises.SubmitSourceMatruhFormation(matruhBeforeDetach,
            Submission(matruhBeforeDetach, matruhConfirm, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var matruhAfterDetach = matruhBeforeDetach;
        var groupedResult = CampaignExercises.AdmitSourceMatruhGroupedWorld(matruhBeforeDetach);
        Assert.True(groupedResult.IsAccepted);
        matruhBeforeDetach = groupedResult.SuccessorSession!;
        Assert.Equal(2, matruhBeforeDetach.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(0, matruhBeforeDetach.SourceAuthority.GroupedWorld.StackAt("commonwealth", "d3714", _ => null));
        Assert.Equal(6, matruhBeforeDetach.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "matruh-garrison").Members.Count);
        Assert.Equal(35, matruhBeforeDetach.SourceAuthority.ProfileVersion);
        var activeMaletti = CampaignExercises.SubmitSourceAxisPriority(matruhBeforeDetach,
            Submission(matruhBeforeDetach, axisLift, CampaignActionAudience.Axis)).SuccessorSession!;
        activeMaletti = CampaignExercises.AdmitSourceMalettiGroupedWorld(activeMaletti).SuccessorSession!;
        Assert.Equal(3, activeMaletti.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(30, activeMaletti.SourceAuthority.GroupedWorld.RemainingMotorPoints("maletti-division"));
        Assert.Equal(5, activeMaletti.SourceAuthority.GroupedWorld.ExternalReservations.Single(value => value.GroupId == "maletti-division").AssignedMotorPoints);
        Assert.Equal(36, activeMaletti.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(activeMaletti).IsVerified);
        var libyanAdmission = CampaignExercises.AdmitSourceLibyanGroupedWorld(activeMaletti);
        Assert.True(libyanAdmission.IsAccepted);
        var libyanProposed = libyanAdmission.SuccessorSession!;
        Assert.Equal(5, libyanProposed.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(22, libyanProposed.SourceAuthority.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-libyan-proposed"));
        Assert.Equal(37, libyanProposed.SourceAuthority.ProfileVersion);
        var secondArtillery = libyanProposed.SourceAuthority.GroupedWorld.Groups.Single(value => value.GroupId == "2-libyan-division")
            .Members.Single(value => value.ElementId == "2-libyan-artillery");
        Assert.Equal("75-27-gun", secondArtillery.ComponentType);
        Assert.Equal(9, secondArtillery.ComponentToe);
        Assert.Contains(secondArtillery.AdditionalComponents!, value => value.ComponentType == "100-17-howitzer" && value.Toe == 3);
        var axisChoices = CampaignExercises.Query(libyanProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "libyan-disposition").ToArray();
        Assert.Equal(2, axisChoices.Length);
        Assert.DoesNotContain(CampaignExercises.Query(libyanProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "libyan-disposition");
        var firstChoice = axisChoices.Single(value => value.ChoiceJson.Contains("1-libyan-division", StringComparison.Ordinal));
        var secondChoice = axisChoices.Single(value => value.ChoiceJson.Contains("2-libyan-division", StringComparison.Ordinal));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(libyanProposed,
            Submission(libyanProposed, firstChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var firstCommand = Submission(libyanProposed, firstChoice.ActionId, CampaignActionAudience.Axis);
        var firstConfirmed = CampaignExercises.Submit(libyanProposed, firstCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(firstConfirmed, firstCommand).RejectionReason);
        var secondAfter = CampaignExercises.Query(firstConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "libyan-disposition");
        firstConfirmed = CampaignExercises.Submit(firstConfirmed, Submission(firstConfirmed, secondAfter.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(22, firstConfirmed.SourceAuthority!.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-libyan-grouped"));
        Assert.Equal(5, firstConfirmed.SourceAuthority.GroupedWorld!.StackAt("axis", "c4020", _ => null));
        Assert.Equal(5, firstConfirmed.SourceAuthority.GroupedWorld.StackAt("axis", "c3920", _ => null));
        Assert.True(CampaignExercises.Reconstruct(firstConfirmed).IsVerified);
        var italianBasisFourth = ContentSourceFourthIndianPackArtifact.Create(firstConfirmed.SourceAuthority.CwFreeGunsWorld!.Content);
        var italianBasisMatruh = ContentSourceMatruhGroupedPackArtifact.Create(italianBasisFourth);
        var italianBasisMaletti = ContentSourceMalettiGroupedPackArtifact.Create(italianBasisMatruh);
        var italianBasisLibyan = ContentSourceLibyanGroupedPackArtifact.Create(italianBasisMaletti);
        var italianContent = ContentSourceCireneMarmaricaGroupedPackArtifact.Create(italianBasisLibyan);
        Assert.Equal(ContentSourceCireneMarmaricaGroupedPackArtifact.ContentHash, italianContent.Hash());
        var italianAdmission = CampaignExercises.AdmitSourceItalianDivisionsGroupedWorld(firstConfirmed);
        Assert.True(italianAdmission.IsAccepted);
        var italianProposed = italianAdmission.SuccessorSession!;
        Assert.Equal(7, italianProposed.SourceAuthority!.GroupedWorld!.Groups.Count);
        Assert.Equal(29, italianProposed.SourceAuthority.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-italian-division-proposed"));
        Assert.Equal(39, italianProposed.SourceAuthority.ProfileVersion);
        var italianChoices = CampaignExercises.Query(italianProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "italian-division-disposition").ToArray();
        Assert.Equal(2, italianChoices.Length);
        Assert.DoesNotContain(CampaignExercises.Query(italianProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "italian-division-disposition");
        var cireneChoice = italianChoices.Single(value => value.ChoiceJson.Contains("cirene-division", StringComparison.Ordinal));
        var italianCommand = Submission(italianProposed, cireneChoice.ActionId, CampaignActionAudience.Axis);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(italianProposed,
            Submission(italianProposed, cireneChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var italianConfirmed = CampaignExercises.Submit(italianProposed, italianCommand).SuccessorSession!;
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(italianConfirmed, italianCommand).RejectionReason);
        var marmaricaChoice = CampaignExercises.Query(italianConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "italian-division-disposition");
        italianConfirmed = CampaignExercises.Submit(italianConfirmed, Submission(italianConfirmed, marmaricaChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(29, italianConfirmed.SourceAuthority!.Battlefield!.Entries.Count(value => value.AuthorityStatus == "source-italian-division-grouped"));
        Assert.Equal(5, italianConfirmed.SourceAuthority.GroupedWorld!.StackAt("axis", "c4120", _ => null));
        Assert.Equal(5, italianConfirmed.SourceAuthority.GroupedWorld.StackAt("axis", "c3918", _ => null));
        Assert.True(CampaignExercises.Reconstruct(italianConfirmed).IsVerified);
        var seventhDirect = italianConfirmed.SourceAuthority.AdmitSeventhArmoredGroupedWorld();
        Assert.NotNull(seventhDirect.GroupedWorld);
        var seventhResult = CampaignExercises.AdmitSourceSeventhArmoredGroupedWorld(italianConfirmed);
        Assert.True(seventhResult.IsAccepted);
        var seventhProposed = seventhResult.SuccessorSession!;
        var seventh = seventhProposed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division");
        Assert.Equal(7, seventh.Members.Count); Assert.Null(seventh.StackValue); Assert.False(seventh.DispositionConfirmed);
        Assert.Equal(10, seventh.MotorPool);
        Assert.Equal(40, seventhProposed.SourceAuthority!.ProfileVersion);
        var seventhChoice = CampaignExercises.Query(seventhProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "seventh-armored-disposition");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(seventhProposed,
            Submission(seventhProposed, seventhChoice.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var seventhCommand = Submission(seventhProposed, seventhChoice.ActionId, CampaignActionAudience.Commonwealth);
        var seventhConfirmedResult = CampaignExercises.Submit(seventhProposed, seventhCommand);
        Assert.True(seventhConfirmedResult.IsAccepted);
        var seventhConfirmed = seventhConfirmedResult.SuccessorSession!;
        Assert.Equal(5, seventhConfirmed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division").StackValue);
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(seventhConfirmed, seventhCommand).RejectionReason);
        Assert.Equal(italianConfirmed.SourceAuthority.World.PendingArrivals, seventhConfirmed.SourceAuthority.World.PendingArrivals);
        Assert.True(CampaignExercises.Reconstruct(seventhConfirmed).IsVerified);
        var legacySeventhCheckpoint = seventhConfirmed.SourceAuthority.SerializeCheckpoint();
        var correctedSeventh = CampaignExercises.CorrectSourceSeventhShell(seventhConfirmed).SuccessorSession!;
        Assert.Equal(3, correctedSeventh.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "7-armored-division").StackValue);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedLegacyCheckpointContractVersion, correctedSeventh.SourceAuthority.ProfileVersion);
        Assert.Equal(legacySeventhCheckpoint, seventhConfirmed.SourceAuthority.SerializeCheckpoint());
        Assert.Equal(seventhConfirmed.SourceAuthority.RandomState, correctedSeventh.SourceAuthority.RandomState);
        Assert.Contains("common-charts-9.4-division-shell-stacks-three", System.Text.Encoding.UTF8.GetString(
            CampaignExercises.QuerySourceSeventhArmoredDraft(correctedSeventh, CampaignActionAudience.Commonwealth).VisibleBytes));
        Assert.True(CampaignExercises.Reconstruct(correctedSeventh).IsVerified);
        Assert.False(CampaignExercises.CorrectSourceSeventhShell(correctedSeventh).IsAccepted);
        var compactBeforeShell = CampaignExercises.AdmitSourceCompactCheckpoint(seventhConfirmed).SuccessorSession!;
        var compactAxisFinish = CampaignExercises.Query(compactBeforeShell, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var compactAxisFinished = CampaignExercises.Submit(compactBeforeShell,
            Submission(compactBeforeShell, compactAxisFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var compactCwFinish = CampaignExercises.Query(compactAxisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var compactBothFinished = CampaignExercises.Submit(compactAxisFinished,
            Submission(compactAxisFinished, compactCwFinish.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var compactCorrected = CampaignExercises.CorrectSourceSeventhShell(compactBothFinished).SuccessorSession!;
        Assert.Equal(1, compactCorrected.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedFinalizedCompactCheckpointContractVersion, compactCorrected.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(compactCorrected).IsVerified);
        var compactAxisReopen = CampaignExercises.Query(compactCorrected, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var compactReopened = CampaignExercises.Submit(compactCorrected,
            Submission(compactCorrected, compactAxisReopen.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Equal(0, compactReopened.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedFinalizedCompactCheckpointContractVersion, compactReopened.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(compactReopened).IsVerified);
        var sixteenthBasis = ContentSourceSeventhArmoredGroupedPackArtifact.Create(italianContent);
        var sixteenthContent = ContentSourceSixteenthBrigadeGroupedPackArtifact.Create(sixteenthBasis);
        Assert.Equal(ContentSourceSixteenthBrigadeGroupedPackArtifact.ContentHash, sixteenthContent.Hash());
        var sixteenthResult = CampaignExercises.AdmitSourceSixteenthBrigadeGroupedWorld(seventhConfirmed);
        Assert.True(sixteenthResult.IsAccepted);
        var sixteenthProposed = sixteenthResult.SuccessorSession!;
        var sourceWorld = sixteenthProposed.SourceAuthority!.GroupedWorld!;
        var sixteenth = sourceWorld.Groups.Single(value => value.GroupId == "16-infantry-brigade");
        Assert.Equal(4, sixteenth.Members.Count); Assert.Null(sixteenth.StackValue); Assert.True(sixteenth.UsesRetainedMembers);
        Assert.Equal(4, sourceWorld.Prior.World.Elements.Count(value => sixteenth.Members.Any(member => member.ElementId == value.ElementId)));
        Assert.Single(sourceWorld.OccupantsAt("e1829"), value => value.Formation?.GroupId == sixteenth.GroupId);
        var sixteenthChoice = CampaignExercises.Query(sixteenthProposed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "sixteenth-brigade-disposition");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(sixteenthProposed,
            Submission(sixteenthProposed, sixteenthChoice.ActionId, CampaignActionAudience.Axis)).RejectionReason);
        var sixteenthCommand = Submission(sixteenthProposed, sixteenthChoice.ActionId, CampaignActionAudience.Commonwealth);
        var sixteenthConfirmed = CampaignExercises.Submit(sixteenthProposed, sixteenthCommand).SuccessorSession!;
        Assert.Equal(2, sixteenthConfirmed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == sixteenth.GroupId).StackValue);
        Assert.Equal(41, sixteenthConfirmed.SourceAuthority.ProfileVersion);
        var playableWorld = CampaignSourcePlayableWorld.Create(sixteenthConfirmed.SourceAuthority);
        var mapBoundWorld = CampaignSourcePlayableWorld.Create(sixteenthConfirmed.SourceAuthority, mapIdentityBound: true);
        var cdWorld = CampaignSourcePlayableWorld.Create(sixteenthConfirmed.SourceAuthority, mapIdentityBound: true, cdSeamBound: true);
        Assert.True(mapBoundWorld.MapIdentityBound);
        Assert.True(mapBoundWorld.WithWorld(mapBoundWorld.GroupedWorld, mapBoundWorld.Battlefield).MapIdentityBound);
        Assert.False(playableWorld.MapIdentityBound);
        // Test the wire contract on the retained historical world without
        // claiming that the still-gated campaign can start yet.
        var turnConstructor = typeof(CampaignSourceTurnAuthority).GetConstructors(
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Single();
        CampaignSourceTurnAuthority ContractTurn(CampaignSourcePlayableWorld world, LandSequencePosition? testPosition = null,
            int? organizationMask = null, CampaignSourceTrainingLedger? training = null,
            CampaignSourceGt1AxisArrival? axisArrival = null, LandSide? firstSide = null,
            (int GameTurn, int OperationStage)? paidStage = null)
        {
            var args = turnConstructor.GetParameters().Select(parameter => parameter.Name == "position" && testPosition is not null
                ? testPosition : parameter.Name == "voluntaryOrganizationDeclinedMask" && organizationMask is not null
                ? organizationMask : parameter.Name == "training" && training is not null ? training
                : parameter.Name == "axisArrival" && axisArrival is not null ? axisArrival
                : parameter.Name == "fourthIndianPaidStage" && paidStage is not null ? paidStage
                : parameter.Name == "stageOneFirstSide" && firstSide is not null ? firstSide : parameter.HasDefaultValue
                ? parameter.DefaultValue : parameter.Name switch
                {
                    "world" => world,
                    "stateVersion" => world.Source.StateVersion + 1,
                    "position" => Cna1979LandSequenceV4.CreateTurn(1)[0],
                    "randomState" => world.RandomState,
                    "initiative" => null,
                    _ => throw new InvalidOperationException("Unexpected source-turn constructor parameter."),
                }).ToArray();
            return (CampaignSourceTurnAuthority)turnConstructor.Invoke(args);
        }
        var mapTurn = ContractTurn(mapBoundWorld);
        var cdTurn = ContractTurn(cdWorld);
        Assert.Equal(3, new ExerciseCheckpoint(cdTurn).ContractVersion);
        Assert.True(cdWorld.WithWorld(cdWorld.GroupedWorld, cdWorld.Battlefield).CdSeamBound);
        using (var cdCheckpoint = JsonDocument.Parse(cdTurn.SerializeCheckpoint()))
        {
            Assert.Equal(3, cdCheckpoint.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(Cna1979HistoricalMap.CdSeamPolicyHash,
                cdCheckpoint.RootElement.GetProperty("historicalMap").GetProperty("cdSeamPolicyHash").GetString());
        }
        var legacyTurn = ContractTurn(playableWorld);
        var turnContacts = sixteenthConfirmed.CurrentContacts.AdvancePregameSource(mapTurn.Source.StateVersion);
        var turnSession = new ExerciseSession(mapTurn.Source.Pregame, turnContacts,
            sixteenthConfirmed.PregameHistory, mapTurn.Source, mapTurn);
        var initiativeResult = CampaignExercises.ResolveSourceInitiative(turnSession);
        Assert.True(initiativeResult.IsAccepted);
        Assert.Equal(initiativeResult.SuccessorSession!.SourceTurn!.Position.PositionId,
            initiativeResult.Evidence!.Receipt.ResultingPositionId);
        Assert.Equal(CampaignSourceTurnAuthority.InitiativeActionId, initiativeResult.Evidence.Receipt.ActionId);
        var organizationCut = Cna1979LandSequenceV4.CreateTurn(1).Single(value =>
            value.OperationStage == 1 && value.PhaseId == LandPhaseIds.Organization);
        var interrupted = CampaignSourceTrainingLedger.CreateSixthAustralian().Interrupt();
        var waitingOrganization = ContractTurn(mapBoundWorld, organizationCut, 1, interrupted);
        Assert.Throws<InvalidOperationException>(() => waitingOrganization.CompleteGt1Organization());
        Assert.Throws<InvalidOperationException>(() => ContractTurn(mapBoundWorld, organizationCut, 3).CompleteGt1Organization());
        var readyOrganization = ContractTurn(mapBoundWorld, organizationCut, 3, interrupted);
        var completedOrganization = readyOrganization.CompleteGt1Organization();
        Assert.Equal(Cna1979LandSequenceV4.GetNext(organizationCut), completedOrganization.Position);
        Assert.Same(readyOrganization.Training, completedOrganization.Training);
        Assert.Equal(readyOrganization.RandomState, completedOrganization.RandomState);
        Assert.Same(readyOrganization.World, completedOrganization.World);
        Assert.Equal(readyOrganization.StateVersion + 1, completedOrganization.StateVersion);
        Assert.Throws<InvalidOperationException>(() => completedOrganization.CompleteGt1Organization());
        // Exercise the real source session transition on retained historical
        // setup without claiming that the campaign-start gate has opened.
        var organizationContacts = sixteenthConfirmed.CurrentContacts.AdvancePregameSource(readyOrganization.Source.StateVersion);
        var organizationSession = new ExerciseSession(readyOrganization.Source.Pregame, organizationContacts,
            sixteenthConfirmed.PregameHistory, readyOrganization.Source, readyOrganization);
        var organizationResult = CampaignExercises.CompleteSourceGt1Organization(organizationSession);
        Assert.True(organizationResult.IsAccepted);
        Assert.Equal(completedOrganization.Position.PositionId, organizationResult.Evidence!.Receipt.ResultingPositionId);
        Assert.True(CampaignExercises.IsSourceGt1OrganizationCompleted(organizationResult.SuccessorSession!));
        Assert.Equal(completedOrganization.Position, organizationResult.SuccessorSession!.SourceTurn!.Position);
        var organizationCommand = CampaignHostAdapter.SourceTurnSystemCommand(organizationSession, organizationResult);
        var organizationStep = new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(organizationCommand), organizationResult.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(organizationResult.Evidence.Receipt),
            organizationResult.Evidence.SnapshotCheckpoint, CampaignHostAcceptedStep.SourceGt1OrganizationCompleteKind);
        Assert.Equal(CampaignHostAcceptedStep.SourceGt1OrganizationCompleteContractVersion, organizationStep.ContractVersion);
        using (var organizationEvent = JsonDocument.Parse(Assert.Single(organizationResult.Evidence!.EventRecords)))
        {
            Assert.Equal("source-gt1-organization-completed", organizationEvent.RootElement.GetProperty("eventType").GetString());
            Assert.Equal(CampaignSourceTurnAuthority.CompleteGt1OrganizationActionId, organizationEvent.RootElement.GetProperty("actionId").GetString());
        }
        Assert.Equal(2, new ExerciseCheckpoint(mapTurn).ContractVersion);
        Assert.Equal(1, new ExerciseCheckpoint(legacyTurn).ContractVersion);
        using (var boundCheckpoint = System.Text.Json.JsonDocument.Parse(mapTurn.SerializeCheckpoint()))
        using (var legacyCheckpoint = System.Text.Json.JsonDocument.Parse(legacyTurn.SerializeCheckpoint()))
        {
            Assert.Equal(2, boundCheckpoint.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(Cna1979HistoricalMap.HexHash, boundCheckpoint.RootElement.GetProperty("historicalMap").GetProperty("hexSha256").GetString());
            Assert.Equal(Cna1979HistoricalMap.EdgeHash, boundCheckpoint.RootElement.GetProperty("historicalMap").GetProperty("hexsideSha256").GetString());
            Assert.Equal(1, legacyCheckpoint.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.False(legacyCheckpoint.RootElement.TryGetProperty("historicalMap", out _));
        }
        using (var boundEvent = System.Text.Json.JsonDocument.Parse(mapTurn.SerializeEvent("source-turn-started",
                   CampaignSourceTurnAuthority.StartActionId, mapBoundWorld.Source.StateVersion,
                   mapBoundWorld.Source.SerializeCheckpoint())))
        {
            Assert.Equal(2, boundEvent.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal(CampaignPregameSourceCreation.Digest(mapTurn.SerializeCheckpoint()),
                boundEvent.RootElement.GetProperty("checkpointHash").GetString());
        }
        using (var legacyEvent = System.Text.Json.JsonDocument.Parse(legacyTurn.SerializeEvent("source-turn-started",
                   CampaignSourceTurnAuthority.StartActionId, playableWorld.Source.StateVersion,
                   playableWorld.Source.SerializeCheckpoint())))
            Assert.Equal(1, legacyEvent.RootElement.GetProperty("contractVersion").GetInt32());
        Assert.True(mapTurn.ResolveInitiative().World.MapIdentityBound);
        Assert.Same(sixteenthConfirmed.SourceAuthority.GroupedWorld, playableWorld.GroupedWorld);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.RandomState, playableWorld.RandomState);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.World.PendingArrivals, playableWorld.World8.PendingArrivals);
        foreach (var entry in playableWorld.Battlefield.Entries.Where(value => value.LocationId is { Length: 5 }
                     && value.LocationId[0] is >= 'a' and <= 'e' && value.LocationId.Skip(1).All(char.IsAsciiDigit)))
            Assert.NotNull(Cna1979HistoricalMap.Instance.HexAt(entry.LocationId!));
        foreach (var location in Cna1979GrazianiSourceFacts.CairoEntryArea.LocationIds)
            Assert.Equal(Cna1979SourceContentVocabulary.MajorCityTerrainId, playableWorld.KnownTerrainAt(location));
        var polishArrivalPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        Assert.Contains("e1829", CampaignSourcePolishArrival.ValidDestinations(playableWorld, polishArrivalPosition));
        Assert.Null(playableWorld.KnownTerrainAt("e1932"));
        Assert.Single(playableWorld.OccupantsAt("e1829"), value => value.Formation?.GroupId == sixteenth.GroupId);
        Assert.DoesNotContain(playableWorld.UnresolvedAt("e1829"), value => sixteenth.Members.Any(member => member.ElementId == value.ElementId));
        Assert.False(playableWorld.IsGameplayReady);
        Assert.Contains("battlefield-incomplete", playableWorld.ReadinessBlockers());
        Assert.Contains("control-incomplete", playableWorld.ReadinessBlockers());
        Assert.Contains("map-incomplete", playableWorld.ReadinessBlockers());
        Assert.Same(playableWorld.GroupedWorld, playableWorld.WithWorld(playableWorld.GroupedWorld, playableWorld.Battlefield).GroupedWorld);
        var sourceSupply = playableWorld.Source.CommonwealthSupplyWorld;
        CampaignSourceFourthIndianRoad.RequirePinnedSource(playableWorld);
        Assert.Equal(new CapabilityPointAmount(1, 1), CampaignSourceFourthIndianRoad.Cost(false));
        Assert.Equal(new CapabilityPointAmount(1, 2), CampaignSourceFourthIndianRoad.Cost(true));
        Assert.Equal(new CapabilityPointAmount(1, 1), CampaignSourceFourthIndianRoad.SupplyTraceCost);
        var paidSupply = sourceSupply.SpendFuel("cw-matruh-1", "d3714", 2);
        Assert.Equal(60, Assert.Single(sourceSupply.Units, value => value.Id == "cw-matruh-1").Fuel);
        Assert.Equal(58, Assert.Single(paidSupply.Units, value => value.Id == "cw-matruh-1").Fuel);
        Assert.Throws<InvalidOperationException>(() => sourceSupply.SpendFuel("cw-matruh-1", "d3615", 2));
        Assert.Throws<InvalidOperationException>(() => sourceSupply.SpendFuel("cw-matruh-1", "d3714", 61));
        Assert.Throws<InvalidOperationException>(() => sourceSupply.SpendFuel("not-a-counter", "d3714", 2));
        var movementCatalog = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1
            && value.ActorRole == LandActorRole.FirstActingSide && value.SegmentId == LandSegmentIds.Movement);
        var movementPosition = new LandSequencePosition(movementCatalog.ContractVersion, movementCatalog.PositionId,
            movementCatalog.GameTurn, movementCatalog.OperationStage, movementCatalog.StageId, movementCatalog.PhaseId,
            movementCatalog.SegmentId, movementCatalog.StepId, movementCatalog.ActorRole, LandSide.Commonwealth, movementCatalog.Sources);
        var season = Cna1979Weather.GetSeason(1);
        var normalRoll = Enumerable.Range(11, 56).First(value => value % 10 is >= 1 and <= 6
            && value / 10 is >= 1 and <= 6 && Cna1979Weather.GetKind(season, value) == WeatherKind.Normal);
        var normalWeather = new CampaignOperationStageWeather(1, 1, 1, LandSide.Axis, season,
            normalRoll / 10, normalRoll % 10, WeatherKind.Normal, WeatherScope.None, null, [], 0, 0, 0);
        Assert.Throws<InvalidOperationException>(() => CampaignSourceFourthIndianMovement.Apply(playableWorld,
            sourceSupply, null, movementPosition, normalWeather, "cw-matruh-1"));
        Assert.Equal(60, Assert.Single(sourceSupply.Units, value => value.Id == "cw-matruh-1").Fuel);
        // Exercise the source-bound mutation and contact projection directly;
        // source readiness still prevents this from becoming a playable move.
        var fourth = Assert.Single(playableWorld.GroupedWorld.Groups,
            value => value.GroupId == CampaignSourceFourthIndianMovement.GroupId);
        var fourthCosts = fourth.Members.ToDictionary(value => value.ElementId,
            value => new CapabilityPointAmount(1, 1), StringComparer.Ordinal);
        var movedFourth = playableWorld.GroupedWorld.MoveFormation(fourth.GroupId, "d3715", fourthCosts, 5, 1, 1);
        var movedBattlefield = playableWorld.Battlefield.MoveFormation(playableWorld.GroupedWorld, movedFourth, fourth.GroupId);
        var movedWorld = playableWorld.WithWorld(movedFourth, movedBattlefield);
        Assert.True(mapBoundWorld.WithWorld(movedFourth, movedBattlefield).MapIdentityBound);
        var unknownMove = playableWorld.GroupedWorld.MoveFormation(fourth.GroupId, "c9999", fourthCosts, 5, 1, 1);
        var unknownBattlefield = playableWorld.Battlefield.MoveFormation(playableWorld.GroupedWorld, unknownMove, fourth.GroupId);
        Assert.Contains(unknownBattlefield.Entries, entry => entry.LocationId == "c9999");
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePlayableWorld.RequireKnownMapLocations(
            unknownBattlefield.Entries.Select(entry => entry.LocationId)));
        Assert.Throws<InvalidOperationException>(() => mapBoundWorld.WithWorld(unknownMove, unknownBattlefield));
        Assert.All(fourth.Members, member => Assert.Equal("d3715",
            Assert.Single(movedWorld.Battlefield.Entries, entry => entry.ElementId == member.ElementId).LocationId));
        Assert.All(fourth.Members, member => Assert.Equal("d3615",
            Assert.Single(playableWorld.Battlefield.Entries, entry => entry.ElementId == member.ElementId).LocationId));
        var polishPosition = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 3
            && value.PhaseId == LandPhaseIds.NavalConvoyArrival && value.SegmentId is null);
        Assert.Throws<InvalidOperationException>(() => CampaignSourcePolishArrival.Apply(playableWorld,
            polishPosition, LandSide.Commonwealth, "e1730"));
        Assert.Equal(sixteenthConfirmed.SourceAuthority.World.PendingArrivals, playableWorld.World8.PendingArrivals);
        Assert.Equal(sixteenthConfirmed.SourceAuthority.RandomState, playableWorld.RandomState);
        var polishGrouped = CampaignSourcePolishArrival.Materialize(playableWorld.GroupedWorld, playableWorld.Source.Content, "e1829");
        var polishBattlefield = playableWorld.Battlefield.ArriveRetainedFormation(playableWorld.GroupedWorld,
            polishGrouped, CampaignSourcePolishArrival.GroupId, "e1829");
        var polishSuccessor = playableWorld.WithWorld(polishGrouped, polishBattlefield);
        var mapBoundPolishSuccessor = mapBoundWorld.WithWorld(polishGrouped, polishBattlefield);
        Assert.True(mapBoundPolishSuccessor.MapIdentityBound);
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(mapBoundWorld, mapBoundPolishSuccessor,
            mapBoundWorld.Source.StateVersion + 1, new CampaignContactKnowledge(mapBoundWorld.Source.Request.CampaignId, LandSide.Axis)));
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(mapBoundWorld, mapBoundPolishSuccessor,
            mapBoundWorld.Source.StateVersion + 1, new CampaignContactKnowledge(mapBoundWorld.Source.Request.CampaignId, LandSide.Commonwealth)));
        Assert.DoesNotContain(polishSuccessor.World8.PendingArrivals, value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.DoesNotContain(polishSuccessor.Battlefield.Pending, value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.Contains(polishSuccessor.World8.PendingArrivals, value => value.GroupId == Cna1979GrazianiSourceFacts.SeventhIndianBrigadeArrival.GroupId);
        Assert.Equal(8, polishSuccessor.World8.World.Elements.Count(value => Cna1979GrazianiSourceFacts.PolishBrigadeArrival.ElementIds.Contains(value.ElementId)));
        Assert.Equal(playableWorld.RandomState, polishSuccessor.RandomState);
        Assert.False(polishSuccessor.IsGameplayReady);
        Assert.Contains(playableWorld.World8.PendingArrivals, value => value.GroupId == CampaignSourcePolishArrival.GroupId);
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(playableWorld, polishSuccessor,
            playableWorld.Source.StateVersion + 1, new CampaignContactKnowledge(playableWorld.Source.Request.CampaignId, LandSide.Axis)));
        Assert.Empty(CampaignSourcePolishArrivalContacts.Derive(playableWorld, polishSuccessor,
            playableWorld.Source.StateVersion + 1, new CampaignContactKnowledge(playableWorld.Source.Request.CampaignId, LandSide.Commonwealth)));
        var begunEarlyPlan = CampaignExercises.SubmitSourceSeptemberSupply(sixteenthConfirmed,
            Submission(sixteenthConfirmed, CampaignSourceAuthorityState.SeptemberSupplyActionId("begin", 0, 0, 0, 0), CampaignActionAudience.Axis)).SuccessorSession!;
        var rolledEarlyPlan = CampaignExercises.SubmitSourceSeptemberSupply(begunEarlyPlan,
            Submission(begunEarlyPlan, CampaignSourceAuthorityState.SeptemberSupplyActionId("roll", 3, 0, 0, 0), CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.Contains("axis-prestart-supply-plan-incomplete", CampaignSourcePlayableWorld.Create(rolledEarlyPlan.SourceAuthority!).ReadinessBlockers());
        var finishedEarlyPlan = CampaignExercises.SubmitSourceSeptemberSupply(rolledEarlyPlan,
            Submission(rolledEarlyPlan, CampaignSourceAuthorityState.SeptemberSupplyActionId("finish", 3, 0, 0, 0), CampaignActionAudience.Axis)).SuccessorSession!;
        Assert.DoesNotContain("axis-prestart-supply-plan-incomplete", CampaignSourcePlayableWorld.Create(finishedEarlyPlan.SourceAuthority!).ReadinessBlockers());
        Assert.Equal(CampaignActionSubmissionRejectionReason.StaleState, CampaignExercises.Submit(sixteenthConfirmed, sixteenthCommand).RejectionReason);
        Assert.Equal(seventhConfirmed.SourceAuthority.World.PendingArrivals, sixteenthConfirmed.SourceAuthority.World.PendingArrivals);
        Assert.True(CampaignExercises.Reconstruct(sixteenthConfirmed).IsVerified);
        var sixteenthHistory = sixteenthConfirmed.PregameHistory.Select(value => value.ToArray()).ToArray();
        var sixteenthEvent = System.Text.Encoding.UTF8.GetString(sixteenthHistory[^1]);
        using (var eventDocument = System.Text.Json.JsonDocument.Parse(sixteenthHistory[^1]))
        {
            var worldHash = eventDocument.RootElement.GetProperty("worldHash").GetString()!;
            var alteredHash = worldHash[..^1] + (worldHash[^1] == '0' ? '1' : '0');
            sixteenthHistory[^1] = System.Text.Encoding.UTF8.GetBytes(sixteenthEvent.Replace(worldHash, alteredHash, StringComparison.Ordinal));
        }
        var tamperedSixteenth = new ExerciseSession(sixteenthConfirmed.Pregame!, sixteenthConfirmed.CurrentContacts,
            sixteenthHistory, sixteenthConfirmed.SourceAuthority);
        Assert.False(CampaignExercises.Reconstruct(tamperedSixteenth).IsVerified);
        var catanzaroResult = CampaignExercises.AdmitSourceCatanzaroGroupedWorld(sixteenthConfirmed);
        Assert.True(catanzaroResult.IsAccepted);
        var catanzaroProposed = catanzaroResult.SuccessorSession!;
        var catanzaro = catanzaroProposed.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "catanzaro-division");
        Assert.Equal(13, catanzaro.Members.Count);
        Assert.Null(catanzaro.StackValue);
        Assert.Equal(35, catanzaro.MotorPool);
        var catanzaroChoice = CampaignExercises.Query(catanzaroProposed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "catanzaro-disposition");
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal, CampaignExercises.Submit(catanzaroProposed,
            Submission(catanzaroProposed, catanzaroChoice.ActionId, CampaignActionAudience.Commonwealth)).RejectionReason);
        var catanzaroCommand = Submission(catanzaroProposed, catanzaroChoice.ActionId, CampaignActionAudience.Axis);
        var catanzaroConfirmed = CampaignExercises.Submit(catanzaroProposed, catanzaroCommand).SuccessorSession!;
        Assert.Equal(42, catanzaroConfirmed.SourceAuthority!.ProfileVersion);
        var axisFinish = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var axisFinished = CampaignExercises.Submit(catanzaroConfirmed,
            Submission(catanzaroConfirmed, axisFinish.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var cwFinishAfterAxis = CampaignExercises.Query(axisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "owner-finalization");
        var bothFinishedBeforeCorrection = CampaignExercises.Submit(axisFinished,
            Submission(axisFinished, cwFinishAfterAxis.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.Equal(3, bothFinishedBeforeCorrection.SourceAuthority!.FinalizedOwners);
        var correctedFinalized = CampaignExercises.CorrectSourceSeventhShell(bothFinishedBeforeCorrection).SuccessorSession!;
        Assert.Equal(1, correctedFinalized.SourceAuthority!.FinalizedOwners);
        Assert.Equal(CampaignSourceAuthorityState.ShellCorrectedFinalizedLegacyCheckpointContractVersion, correctedFinalized.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(correctedFinalized).IsVerified);
        var choicesBefore = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "sixth-australian-placement")
            .Select(value => value.ChoiceJson).Order(StringComparer.Ordinal).ToArray();
        var choicesAfter = CampaignExercises.Query(axisFinished, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Where(value => value.Family == "sixth-australian-placement")
            .Select(value => value.ChoiceJson).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(choicesBefore, choicesAfter);
        var australianChoice = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "sixth-australian-placement"
                && value.SourceActionId == CampaignSourceAuthorityState.SixthAustralianActionId("e1829"));
        Assert.DoesNotContain(CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Commonwealth).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>(), value => value.Family == "sixth-australian-placement"
                && value.SourceActionId == CampaignSourceAuthorityState.SixthAustralianActionId(chosenCairoHex));
        var australianSession = CampaignExercises.Submit(catanzaroConfirmed,
            Submission(catanzaroConfirmed, australianChoice.ActionId, CampaignActionAudience.Commonwealth)).SuccessorSession!;
        var australian = australianSession.SourceAuthority!.GroupedWorld!.Groups.Single(value => value.GroupId == "6th-australian-division");
        Assert.Equal(10, australian.Members.Count);
        Assert.Equal(5, australian.StackValue);
        Assert.False(australian.MotorPoolKnown);
        var australianInfantry = australian.Members.Where(value => value.ComponentType == "infantry").ToArray();
        Assert.Equal(6, australianInfantry.Length);
        Assert.All(australianInfantry, value => { Assert.Equal(6, value.ComponentToe); Assert.Equal(10, value.CapabilityPointAllowance); });
        foreach (var id in new[] { "16-australian-infantry-brigade-hq", "17-australian-infantry-brigade-hq" })
        {
            var headquarters = australian.Members.Single(value => value.ElementId == id);
            Assert.Equal(1, headquarters.ComponentToe); Assert.Equal(20, headquarters.CapabilityPointAllowance);
        }
        var cavalry = australian.Members.Single(value => value.ElementId == "6-australian-divisional-cavalry");
        Assert.Equal(8, cavalry.ComponentToe); Assert.Equal(45, cavalry.CapabilityPointAllowance);
        Assert.Equal(catanzaroConfirmed.SourceAuthority!.RandomState, australianSession.SourceAuthority.RandomState);
        Assert.Equal(45, australianSession.SourceAuthority.ProfileVersion);
        Assert.True(CampaignExercises.Reconstruct(australianSession).IsVerified);
        var axisBatchChoice = CampaignExercises.Query(catanzaroConfirmed, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "ccnn-batch-disposition");
        var afterAustralianAxisChoice = CampaignExercises.Query(australianSession, CampaignActionAudience.Axis).ActionSet!.Candidates
            .OfType<CampaignSourceSetupAction>().Single(value => value.Family == "ccnn-batch-disposition");
        Assert.Equal(axisBatchChoice.ChoiceJson, afterAustralianAxisChoice.ChoiceJson);
        var australianFirst = CampaignExercises.Submit(australianSession,
            Submission(australianSession, afterAustralianAxisChoice.ActionId, CampaignActionAudience.Axis)).SuccessorSession!;
        var fullyMotorized = australianFirst;
        for (var index = 0; index < 5; index++)
        {
            var adjustment = CampaignExercises.SubmitSourceFourthIndian(fullyMotorized,
                Submission(fullyMotorized, priorityAction, CampaignActionAudience.Commonwealth));
            Assert.True(adjustment.IsAccepted);
            fullyMotorized = adjustment.SuccessorSession!;
        }

        var motorizedWorld = CampaignSourcePlayableWorld.Create(fullyMotorized.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true);
        Assert.Contains("forces-incomplete", motorizedWorld.ReadinessBlockers());
        Assert.Contains("seventh-armored-shell-stack-correction-required", motorizedWorld.ReadinessBlockers());
        Assert.Contains("owner-setup-finalization-incomplete", motorizedWorld.ReadinessBlockers());
        Assert.Contains("central-india-horse-training-state-unresolved", motorizedWorld.ReadinessBlockers());

        // Combine every proven admission/correction from PRs #352 (Giarabub),
        // #355 (Tobruk/Bardia), and #359 (Category B + Aresca sequencing) into
        // one continuous chain, all before Axis owner finalization, instead of
        // each living in its own standalone proof branch against the same
        // fullyMotorized checkpoint. Order here follows the already-proven
        // chains verbatim: Giarabub cluster + CW frontline (precondition for
        // Tobruk/Bardia's own CwFreeGunsWorld stage) first, then Tobruk/Bardia,
        // then Category B's 3 new packs, then Aresca (CanAdmitAresca only needs
        // IsCcnnBatchGrouped, already true from the shared setup above - it does
        // not depend on any of these other clusters), then the already-proven
        // Central India Horse ruling / seventh-armored shell correction /
        // both-owner finalization sequence.
        var activated = CampaignExercises.ActivateSourceAxisMinorGarrisons(fullyMotorized).SuccessorSession!;
        activated = CampaignExercises.AdmitSourceCompactCheckpoint(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceAxisGarrisonRemainder(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceGiarabubMotorMachineGun(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceCwFrontline(activated).SuccessorSession!;
        // Reconstruct (full replay verification) is confirmed through this point
        // by the replay dispatcher, matching PR #359's own observation. Tobruk/
        // Bardia and Category B's event types have no replay-dispatcher case yet
        // (a pre-existing gap neither #355 nor #359 closed), so Reconstruct is
        // not re-checked after them here either.
        Assert.True(CampaignExercises.Reconstruct(activated).IsVerified);
        activated = CampaignExercises.AdmitSourceTobrukBardiaGarrison(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceTobrukBardiaGarrison(activated).SuccessorSession!;
        activated = CampaignExercises.AdmitSourceBardiaGarrisonRemainder(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceBardiaGarrisonRemainder(activated).SuccessorSession!;
        activated = CampaignExercises.AdmitSourceTripoliTankRegiment(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceTripoliTankRegiment(activated).SuccessorSession!;
        activated = CampaignExercises.AdmitSourceCwAttachedArtillery(activated).SuccessorSession!;
        activated = CampaignExercises.ActivateSourceCwAttachedArtillery(activated).SuccessorSession!;

        // Resolve cw-frontline-initial-loads-unresolved: AttachedInitialLoads and
        // ExpandedIndependentWorld are already admitted/promoted earlier in this
        // chain's shared setup (confirmed empirically - no Admit/Promote call is
        // needed here). Fully attach both frontline pools using the same
        // CampaignExercises action-submission pattern as the existing CW frontline
        // proofs elsewhere in this file, then finish the frontline.
        foreach (var frontlineId in new[] { "1-rtr", "11-hussars" })
        {
            ExerciseStepResult attachStep;
            while ((attachStep = CampaignExercises.SubmitSourceAttachedInitialLoads(activated,
                Submission(activated, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-attach", frontlineId),
                    CampaignActionAudience.Commonwealth))).IsAccepted)
                activated = attachStep.SuccessorSession!;
        }
        activated = CampaignExercises.SubmitSourceAttachedInitialLoads(activated,
            Submission(activated, CampaignSourceAuthorityState.AttachedInitialLoadsActionId("frontline-finish", "frontline"),
                CampaignActionAudience.Commonwealth)).SuccessorSession!;
        Assert.True(activated.SourceAuthority!.AttachedInitialLoads!.FrontlineFinished);
        Assert.DoesNotContain("cw-frontline-initial-loads-unresolved",
            CampaignSourcePlayableWorld.Create(activated.SourceAuthority, mapIdentityBound: true, cdSeamBound: true).ReadinessBlockers());

        var beforeArescaBlockers = CampaignSourcePlayableWorld.Create(activated.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true).ReadinessBlockers();
        Assert.Contains("forces-incomplete", beforeArescaBlockers);
        Assert.True(activated.SourceAuthority!.CanAdmitAresca);
        var withAresca = CampaignExercises.SubmitSourceAresca(activated,
            Submission(activated, CampaignSourceAuthorityState.ArescaActionId, CampaignActionAudience.Axis)).SuccessorSession!;

        // Both giarabub-motor-mg-* blockers are present before the ruling is
        // submitted, and the ruling action is only offered once the content
        // exists (it was activated earlier in this chain, at ActivateSourceGiarabubMotorMachineGun above).
        var beforeMotorMgRulingBlockers = CampaignSourcePlayableWorld.Create(withAresca.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true).ReadinessBlockers();
        Assert.Contains("giarabub-motor-mg-historical-motorization-priority-unresolved", beforeMotorMgRulingBlockers);
        Assert.Contains("giarabub-motor-mg-capability-and-mobility-unresolved", beforeMotorMgRulingBlockers);
        Assert.True(withAresca.SourceAuthority!.CanAcceptGiarabubMotorMgRuling);
        var motorMgRulingAccepted = Act(withAresca, CampaignActionAudience.Axis, "giarabub-motor-mg-ruling", "approve");
        Assert.True(motorMgRulingAccepted.SourceAuthority!.CwFreeGunsWorld!.MotorMachineGunRulingAccepted);
        Assert.False(motorMgRulingAccepted.SourceAuthority!.CanAcceptGiarabubMotorMgRuling);
        var afterMotorMgRulingBlockers = CampaignSourcePlayableWorld.Create(motorMgRulingAccepted.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true).ReadinessBlockers();
        Assert.DoesNotContain("giarabub-motor-mg-historical-motorization-priority-unresolved", afterMotorMgRulingBlockers);
        Assert.DoesNotContain("giarabub-motor-mg-capability-and-mobility-unresolved", afterMotorMgRulingBlockers);

        var axisHorseApproved = Act(motorMgRulingAccepted, CampaignActionAudience.Axis, "central-india-horse-ruling", "approve");
        var cwHorseApproved = Act(axisHorseApproved, CampaignActionAudience.Commonwealth, "central-india-horse-ruling", "approve");
        Assert.Equal(3, cwHorseApproved.SourceAuthority!.HorseRulingVotes);
        var horseStageOneDeclined = Act(cwHorseApproved, CampaignActionAudience.Commonwealth, "central-india-horse-stage-one", "decline-stage1");
        Assert.True(horseStageOneDeclined.SourceAuthority!.HorseStageOneResolved);
        var shellCorrected = CampaignExercises.CorrectSourceSeventhShell(horseStageOneDeclined).SuccessorSession!;
        Assert.True(shellCorrected.SourceAuthority!.IsSeventhShellCorrected);
        var axisFinalized = Act(shellCorrected, CampaignActionAudience.Axis, "owner-finalization", "finish");
        var bothFinalized = Act(axisFinalized, CampaignActionAudience.Commonwealth, "owner-finalization", "finish");
        Assert.Equal(3, bothFinalized.SourceAuthority!.FinalizedOwners);

        var combinedWorld = CampaignSourcePlayableWorld.Create(bothFinalized.SourceAuthority!, mapIdentityBound: true, cdSeamBound: true);
        var finalBlockers = combinedWorld.ReadinessBlockers();
        // source-grouped-movement-domain-incomplete root cause, traced (via a
        // temporary diagnostic walking CampaignSourceGroupedZoc.EvaluateFormationSource/
        // EvaluateDetachedSource per group/detached member per side, then removed):
        // CanDeriveForSide fails for BOTH sides, each for the same reason - a
        // genuine missing-chart-citation gap, not a missing action call. Axis:
        // the "aresca-tank-regiment" formation's CV33 light tank component
        // (CampaignSourceArescaGroupedDefinition.BattalionId) has no Close Assault
        // defensive rating entry anywhere in CampaignSourceGroupedZoc's hand-cited
        // rating table (TryDefensiveCloseAssaultRating has no case for
        // componentType "cv33" at all), so its defense estimate is unresolved
        // (AllKnown=false, LowerBound=0). Commonwealth: the "6th-australian-division"
        // and "matruh-garrison" formations are in the exact same position - neither
        // groupId appears anywhere in that same rating table for any of their
        // member component types. None of these three gaps are closeable without
        // inventing an uncited rating value, which this project's conventions
        // forbid; each needs its own researched chart citation, out of scope here.
        // source-initial-contacts-unavailable is a genuine, separate gap (see PR
        // description / final report): CampaignSourcePolishArrivalContacts.EffectiveCounters
        // leaves many independent/attached/priority commonwealth counters with a
        // null ObserverFormationId (no case recognizes them as "independent" nor
        // are they grouped), and CampaignSourceInitialContacts.TryAdjacent has no
        // geometry rule connecting rear-area axis locations (e.g. Soluch a4130,
        // Mechili b4921) to the forward commonwealth positions this chain admits
        // (c3520, c3020, c3721, c3922, c3926, c4131, c3320) - both failures make
        // CanProject fail closed for one side or the other. Fixing either requires
        // new classification/geometry rules, not a missing action call; not
        // attempted here per this task's remit.

        // Empirically observed (via a temporary Assert.Fail("DIAG:" + string.Join(",",
        // combinedWorld.ReadinessBlockers())) diagnostic, per this project's own
        // documented technique, run through `just test-method core
        // '*IntegratingEveryAdmittedClusterBeforeOwnerFinalizationRevealsRealRemainingBlockers*'`
        // and then removed) to now be exactly these 5, three fewer than the 8 this
        // fixture originally landed with (cw-frontline-initial-loads-unresolved and
        // both giarabub-motor-mg-* blockers cleared - see below):
        //   battlefield-incomplete, control-incomplete, ccnn-characteristics-incomplete,
        //   source-initial-contacts-unavailable, source-grouped-movement-domain-incomplete
        // 6 of the original 8 pre-this-PR blockers cleared: owner-setup-finalization-incomplete,
        // seventh-armored-shell-stack-correction-required, central-india-horse-
        // training-state-unresolved (each already individually proven; this is the
        // first fixture to chain them with the admitted-content clusters before the
        // same finalization), and forces-incomplete itself - every element these 3
        // PRs admitted, plus Aresca's 2, is now represented.
        // source-grouped-movement-domain-incomplete did NOT clear as a side effect
        // of resolving forces-incomplete - the hypothesis in this project's plan
        // doc is settled false; CampaignSourceGroupedZoc.CanDeriveForSide still
        // rejects this fixture for some other reason and needs its own fix.
        // ccnn-characteristics-incomplete/battlefield-incomplete/control-incomplete
        // remain, as expected (out of scope: a real research gap and a pending
        // product decision, not code gaps).
        // cw-frontline-initial-loads-unresolved is now resolved above: it was pure
        // wiring, exactly as scoped - CampaignSourceAttachedInitialLoads.FinishFrontline
        // (via the existing frontline-attach/frontline-finish CampaignExercises
        // action-submission pattern already proven elsewhere in this file) is all
        // that was missing; AttachedInitialLoads and ExpandedIndependentWorld were
        // already admitted/promoted earlier in this chain's own shared setup, so no
        // extra promotion call was needed.
        // giarabub-motor-mg-historical-motorization-priority-unresolved and
        // giarabub-motor-mg-capability-and-mobility-unresolved are now cleared too
        // (see beforeMotorMgRulingBlockers/afterMotorMgRulingBlockers above):
        // activating the Giarabub Motor Machine Gun was already required (not
        // redundant) to represent the "giarabub-motor-machine-gun" roster element
        // for forces-incomplete (that element's Battlefield entry has no other
        // action that gives it a LocationId - it is not covered by the Giarabub
        // cluster activation from PR #352, which activates a disjoint set of
        // roster elements: giarabub-infantry, giarabub-machine-gun, etc., not
        // giarabub-motor-machine-gun itself), and that activation unconditionally
        // adds both giarabub-motor-mg-* blockers once CwFreeGunsWorld.MotorMachineGunContent
        // is non-null. This is not a missing-mechanism gap, though: the content
        // artifact (ContentSourceGiarabubMotorMachineGunArtifact) already states the
        // answer at creation - the source identifies historical motorization
        // priority for this element but no assigned motor pool or effective
        // current allowance. Nobody had built the action that formally accepts
        // that already-documented finding as the terminal, resolved state; the new
        // Axis-only "giarabub-motor-mg-ruling"/"approve" action
        // (CampaignSourceCwFreeGunsWorld.AcceptMotorMachineGunRuling) does exactly
        // that, and both blockers clear together once it is submitted, since they
        // are two facets of the same underlying no-scenario-pool fact.
        // source-initial-contacts-unavailable also remains a genuine, separate gap,
        // NOT closed here (see the comment above combinedWorld): CampaignSourcePolishArrivalContacts.EffectiveCounters
        // has no "independent" classification case for several of this chain's
        // admitted/pre-existing commonwealth counters (cw-attached-artillery.*,
        // independent-setup.*, cw-priority.*), leaving them with a null
        // ObserverFormationId, and CampaignSourceInitialContacts.TryAdjacent has no
        // geometry rule connecting rear-area axis locations (Soluch a4130, Mechili
        // b4921) to the forward commonwealth positions this chain admits. Tracing
        // confirmed this is not a missing action call (unlike cw-frontline above) -
        // it needs new classification and/or geometry rules.
        Assert.DoesNotContain("owner-setup-finalization-incomplete", finalBlockers);
        Assert.DoesNotContain("seventh-armored-shell-stack-correction-required", finalBlockers);
        Assert.DoesNotContain("central-india-horse-training-state-unresolved", finalBlockers);
        Assert.DoesNotContain("forces-incomplete", finalBlockers);
        Assert.DoesNotContain("cw-frontline-initial-loads-unresolved", finalBlockers);
        Assert.DoesNotContain("giarabub-motor-mg-historical-motorization-priority-unresolved", finalBlockers);
        Assert.DoesNotContain("giarabub-motor-mg-capability-and-mobility-unresolved", finalBlockers);
        Assert.Contains("ccnn-characteristics-incomplete", finalBlockers);
        Assert.Contains("battlefield-incomplete", finalBlockers);
        Assert.Contains("control-incomplete", finalBlockers);
        Assert.Contains("source-grouped-movement-domain-incomplete", finalBlockers);
        Assert.Contains("source-initial-contacts-unavailable", finalBlockers);
        Assert.Equal(5, finalBlockers.Count);
    }

    private static CampaignActionSubmission Submission(ExerciseSession session, string actionId, CampaignActionAudience audience)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new(CampaignActionSubmission.CurrentContractVersion, checkpoint.CampaignId,
            checkpoint.StateVersion, checkpoint.PositionId, audience, actionId);
    }

    private static ExerciseSession Act(ExerciseSession session, CampaignActionAudience audience, string family, string kind)
    {
        var candidate = CampaignExercises.Query(session, audience).ActionSet!.Candidates.OfType<CampaignSourceSetupAction>()
            .Single(value => value.Family == family && JsonDocument.Parse(value.ChoiceJson).RootElement.GetProperty("kind").GetString() == kind);
        var result = CampaignExercises.Submit(session, Submission(session, candidate.ActionId, audience));
        Assert.True(result.IsAccepted);
        return result.SuccessorSession!;
    }
}
