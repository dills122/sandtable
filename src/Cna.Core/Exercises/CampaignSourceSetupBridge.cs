using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Exercises;

// Maps the normal action contract onto the existing source adjudicators.
// Only the requesting side's existing visible choices enter this bridge.
internal static class CampaignSourceSetupBridge
{
    internal static CampaignLegalActionQueryResult QueryTurn(ExerciseSession session, CampaignActionAudience audience)
    {
        if (session.SourceTurn is not { } turn) return CampaignLegalActionQueryResult.Rejected(CampaignLegalActionQueryRejectionReason.InvalidState);
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            return CampaignLegalActionQueryResult.Rejected(CampaignLegalActionQueryRejectionReason.InvalidAudience);
        var position = turn.Position;
        if (position.GameTurn == 1 && position.PhaseId == LandPhaseIds.NavalConvoySchedule)
        {
            var scheduling = new List<CampaignActionCandidate>();
            if (audience == CampaignActionAudience.Axis && turn.Gt3Supply is { Finished: false } plan)
            {
                if (plan.Die is null) scheduling.Add(CampaignSourceTurnAction.RollGt3Supply(turn.StateVersion));
                else
                {
                    foreach (var lane in new[] { 2, 3, 6 })
                        for (var stage = 1; stage <= 3; stage++)
                            foreach (var delta in plan.ValidDeltas(lane, stage))
                                scheduling.Add(CampaignSourceTurnAction.AdjustGt3Supply(turn.StateVersion, lane, stage, delta));
                    scheduling.Add(CampaignSourceTurnAction.FinishGt3Supply(turn.StateVersion));
                }
            }
            if (audience == CampaignActionAudience.Commonwealth && turn.Gt5Supply is { Die: null })
                scheduling.Add(CampaignSourceTurnAction.RollGt5Supply(turn.StateVersion));
            return CampaignLegalActionQueryResult.Success(new CampaignLegalActionSet(turn.Source.Request.CampaignId, turn.StateVersion,
                turn.Source.Request.RulesetHash, position.PositionId, audience, scheduling, CampaignLegalActionSet.SourceTurnPolicyId));
        }
        if (position.GameTurn == 1 && position.PhaseId == LandPhaseIds.TacticalShipping)
        {
            var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
            CampaignActionCandidate[] options = turn.HasDeclinedTacticalShipping(side)
                ? [] : [CampaignSourceTurnAction.DeclineTacticalShipping(turn.StateVersion)];
            return CampaignLegalActionQueryResult.Success(new CampaignLegalActionSet(turn.Source.Request.CampaignId, turn.StateVersion,
                turn.Source.Request.RulesetHash, position.PositionId, audience, options, CampaignLegalActionSet.SourceTurnPolicyId));
        }
        if (position.GameTurn == 1 && position.OperationStage == 1 && position.PhaseId == LandPhaseIds.InitiativeDeclaration)
        {
            CampaignActionCandidate[] options = audience == CampaignActionAudience.Axis
                ? [CampaignSourceTurnAction.ActFirst(turn.StateVersion), CampaignSourceTurnAction.ActLast(turn.StateVersion)] : [];
            return CampaignLegalActionQueryResult.Success(new CampaignLegalActionSet(turn.Source.Request.CampaignId, turn.StateVersion,
                turn.Source.Request.RulesetHash, position.PositionId, audience, options, CampaignLegalActionSet.SourceTurnPolicyId));
        }
        if (position.GameTurn == 1 && position.OperationStage == 1 && position.PhaseId == LandPhaseIds.Organization)
        {
            var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
            var options = new List<CampaignActionCandidate>();
            if (!turn.HasDeclinedVoluntaryOrganization(side))
            {
                // The bounded completion path cannot yet advance ongoing
                // Australian training. Interrupt it before the Commonwealth
                // closes its voluntary Organization choices.
                if (audience != CampaignActionAudience.Commonwealth || turn.Training.Interrupted)
                    options.Add(CampaignSourceTurnAction.DeclineVoluntaryOrganization(turn.StateVersion));
                if (audience == CampaignActionAudience.Commonwealth && !turn.Training.Interrupted
                    && turn.World.GroupedWorld.Groups.Any(group => group.GroupId == CampaignSourceTrainingLedger.FormationId))
                    options.Add(CampaignSourceTurnAction.InterruptSixthAustralianTraining(turn.StateVersion));
            }
            if (turn.Training.LastRecordedStage == 0
                && (turn.Training.InitialRuling is null || turn.Training.InitialRuling == CampaignSourceTrainingLedger.ProvisionalBaselineId)
                && turn.World.GroupedWorld.Groups.Any(group => group.GroupId == CampaignSourceTrainingLedger.FormationId))
            {
                var voted = turn.Training.HasBaselineVote(audience == CampaignActionAudience.Axis);
                options.Add(voted ? CampaignSourceTurnAction.WithdrawSixthAustralianBaseline(turn.StateVersion)
                    : CampaignSourceTurnAction.ApproveSixthAustralianBaseline(turn.StateVersion));
            }
            return CampaignLegalActionQueryResult.Success(new CampaignLegalActionSet(turn.Source.Request.CampaignId, turn.StateVersion,
                turn.Source.Request.RulesetHash, position.PositionId, audience, options, CampaignLegalActionSet.SourceTurnPolicyId));
        }
        if (position.GameTurn == 1 && position.OperationStage == 1 && position.PhaseId == LandPhaseIds.ReserveDesignation
            && position.ActorRole == LandActorRole.FirstActingSide)
        {
            var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
            CampaignActionCandidate[] options = turn.StageOneFirstSide == side
                && turn.World.World8.World.Elements.All(value => value.ReserveStatus == CampaignElementReserveStatus.None)
                ? [CampaignSourceTurnAction.DeclineFirstReserves(turn.StateVersion)] : [];
            return CampaignLegalActionQueryResult.Success(new CampaignLegalActionSet(turn.Source.Request.CampaignId, turn.StateVersion,
                turn.Source.Request.RulesetHash, position.PositionId, audience, options, CampaignLegalActionSet.SourceTurnPolicyId));
        }
        if (position.GameTurn == 1 && position.OperationStage == 1 && position.SegmentId == LandSegmentIds.Movement
            && position.ActorRole == LandActorRole.FirstActingSide && position.ActiveSide is { } active
            && (active == LandSide.Commonwealth || turn.World.CdSeamBound))
        {
            var side = audience == CampaignActionAudience.Axis ? LandSide.Axis : LandSide.Commonwealth;
            var choices = active == LandSide.Commonwealth && audience == CampaignActionAudience.Commonwealth && turn.StageOneWeather is not null
                ? CampaignSourceFourthIndianMovement.ValidSupplyChoices(turn.World, turn.CommonwealthSupply,
                    turn.FourthIndianPaidStage, position, turn.StageOneMovementWeather()) : [];
            var options = choices.Select(id => (CampaignActionCandidate)CampaignSourceTurnAction.MoveFourthIndianOpening(turn.StateVersion, id)).ToList();
            if (turn.World.CdSeamBound && active == side && turn.StageOneFirstSide == side)
                options.Add(CampaignSourceTurnAction.FinishFirstMovement(turn.StateVersion));
            return CampaignLegalActionQueryResult.Success(new CampaignLegalActionSet(turn.Source.Request.CampaignId, turn.StateVersion,
                turn.Source.Request.RulesetHash, position.PositionId, audience, options, CampaignLegalActionSet.SourceTurnPolicyId));
        }
        if (position.GameTurn == 1 && position.OperationStage == 1 && position.PhaseId == LandPhaseIds.NavalConvoyArrival && position.SegmentId is null)
        {
            var options = new List<CampaignActionCandidate>();
            if (audience == CampaignActionAudience.Commonwealth)
            {
                if (turn.MultiArrival?.CanDecline(2) == true) options.Add(CampaignSourceTurnAction.DeclineGt1Lane2Attack(turn.StateVersion));
                if (turn.MultiArrival?.CanDecline(3) == true) options.Add(CampaignSourceTurnAction.DeclineGt1Lane3Attack(turn.StateVersion));
                if (turn.MultiArrival?.Lane6CommonwealthCut().CanDecline == true) options.Add(CampaignSourceTurnAction.DeclineGt1Lane6Attack(turn.StateVersion));
                if (turn.AxisArrival is { Announced: true, Delivered: false }) options.Add(CampaignSourceTurnAction.DeclineGt1Lane2Attack(turn.StateVersion));
                if (turn.BenghaziArrival is { Announced: true, Delivered: false }) options.Add(CampaignSourceTurnAction.DeclineGt1Lane3Attack(turn.StateVersion));
            }
            return CampaignLegalActionQueryResult.Success(new CampaignLegalActionSet(turn.Source.Request.CampaignId, turn.StateVersion,
                turn.Source.Request.RulesetHash, position.PositionId, audience, options, CampaignLegalActionSet.SourceTurnPolicyId));
        }
        if (position.GameTurn != 1 || position.OperationStage != 3 || position.PhaseId != LandPhaseIds.NavalConvoyArrival || position.SegmentId is not null)
            return CampaignLegalActionQueryResult.Rejected(CampaignLegalActionQueryRejectionReason.InvalidState);
        var candidates = audience == CampaignActionAudience.Commonwealth
            ? CampaignSourcePolishArrival.ValidDestinations(turn.World, position)
                .Select(destination => (CampaignActionCandidate)new CampaignSourceTurnAction(turn.StateVersion, destination)).ToArray()
            : [];
        return CampaignLegalActionQueryResult.Success(new CampaignLegalActionSet(turn.Source.Request.CampaignId, turn.StateVersion,
            turn.Source.Request.RulesetHash, position.PositionId, audience, candidates, CampaignLegalActionSet.SourceTurnPolicyId));
    }

    internal static CampaignLegalActionQueryResult Query(ExerciseSession session, CampaignActionAudience audience)
    {
        if (session.SourceAuthority is not { } authority) return CampaignLegalActionQueryResult.Rejected(CampaignLegalActionQueryRejectionReason.InvalidState);
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth))
            return CampaignLegalActionQueryResult.Rejected(CampaignLegalActionQueryRejectionReason.InvalidAudience);
        var items = new List<CampaignActionCandidate>();
        foreach (var (family, draft) in Drafts(session, audience))
        {
            using var document = JsonDocument.Parse(draft.VisibleBytes);
            var choices = document.RootElement.GetProperty("choices").EnumerateArray().ToArray();
            if (choices.Length != draft.PrivateActionIds.Count) throw new InvalidOperationException("Source visible and private choices differ.");
            for (var i = 0; i < choices.Length; i++) items.Add(new CampaignSourceSetupAction(authority.StateVersion, family, draft.PrivateActionIds[i], choices[i]));
        }
        return CampaignLegalActionQueryResult.Success(new CampaignLegalActionSet(authority.Request.CampaignId, authority.StateVersion,
            authority.Request.RulesetHash, CampaignSourceAuthorityState.SetupPositionId, audience, items, CampaignLegalActionSet.SourceSetupPolicyId));
    }

    internal static IReadOnlyList<(string Family, CampaignPregameAxisDraft Draft)> Drafts(ExerciseSession session, CampaignActionAudience audience)
    {
        var authority = session.SourceAuthority ?? throw new InvalidOperationException("Source authority missing.");
        if (audience is not (CampaignActionAudience.Axis or CampaignActionAudience.Commonwealth)) throw new InvalidOperationException("Source audience invalid.");
        var drafts = new List<(string Family, CampaignPregameAxisDraft Draft)>();
        void Add(string family, CampaignPregameAxisDraft draft) => drafts.Add((family, draft));
        var finished = authority.IsOwnerFinalized(audience);
        using (var stream = new MemoryStream())
        {
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteBoolean("ownSetupFinalized", finished);
                writer.WriteStartArray("choices");
                writer.WriteStartObject(); writer.WriteString("kind", finished ? "reopen" : "finish"); writer.WriteEndObject();
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            Add("owner-finalization", new CampaignPregameAxisDraft(authority.StateVersion, stream.ToArray(),
                [CampaignSourceAuthorityState.SetupFinalizationActionId(audience, !finished)]));
        }
        if (finished) return drafts.AsReadOnly();
        Add("central-india-horse-ruling", CampaignExercises.QuerySourceHorseSetupDraft(session, audience, false));
        if (audience == CampaignActionAudience.Commonwealth)
            Add("central-india-horse-stage-one", CampaignExercises.QuerySourceHorseSetupDraft(session, audience, true));
        var axis = audience == CampaignActionAudience.Axis;
        Add("dummy", CampaignExercises.QuerySourceDummyDraft(session, audience));
        Add("motorization", CampaignExercises.QuerySourceMotorizationDraft(session, audience));
        Add("unattached-cargo", CampaignExercises.QuerySourceUnattachedCargoDraft(session, audience));
        if (axis)
        {
            Add("axis-dump", CampaignExercises.QuerySourceAxisDumpDraft(session, audience));
            Add("axis-september-supply", CampaignExercises.QuerySourceSeptemberSupplyDraft(session, audience));
            Add("axis-gt1-supply", CampaignExercises.QuerySourceAxisGt1SupplyDraft(session, audience));
            if (authority.Autoblinda is not null) Add("autoblinda", CampaignExercises.QuerySourceAutoblindaDraft(session, audience));
            if (authority.AxisPriorityMotorization is not null) Add("axis-priority", CampaignExercises.QuerySourceAxisPriorityDraft(session, audience));
            if (authority.AxisFreeArtillery is not null) Add("axis-free", CampaignExercises.QuerySourceAxisFreeSetupDraft(session, audience));
            if (authority.AxisCorpsArtillery is not null) Add("axis-corps", CampaignExercises.QuerySourceAxisCorpsSetupDraft(session, audience));
            if (authority.MalettiHierarchy is not null) Add("maletti-hierarchy", CampaignExercises.QuerySourceMalettiHierarchyDraft(session, audience));
            if (authority.Gt2Correction is not null) Add("axis-gt2-correction", CampaignExercises.QuerySourceGt2CorrectionDraft(session, audience));
            if (authority.IsLibyanGrouped) Add("libyan-disposition", CampaignExercises.QuerySourceLibyanDraft(session, audience));
            if (authority.IsItalianDivisionsGrouped) Add("italian-division-disposition", CampaignExercises.QuerySourceItalianDivisionsDraft(session, audience));
            if (authority.IsCatanzaroGrouped) Add("catanzaro-disposition", CampaignExercises.QuerySourceCatanzaroDraft(session, audience));
            if (authority.CanAdmitCcnnBatch) Add("ccnn-batch-disposition", CampaignExercises.QuerySourceCcnnBatchDraft(session, audience));
            if (authority.CanAdmitAresca) Add("aresca-disposition", CampaignExercises.QuerySourceArescaDraft(session, audience));
        }
        else
        {
            Add("cw-supply", CampaignExercises.QuerySourceCommonwealthSupplyDraft(session, audience));
            if (authority.CommonwealthSupplyPlans is not null) Add("cw-supply-plans", CampaignExercises.QuerySourceCommonwealthSupplyPlanDraft(session, audience));
            if (authority.PriorityMotorization is not null) Add("cw-priority", CampaignExercises.QuerySourcePriorityMotorizationDraft(session, audience));
            if (authority.CairoSetup is not null) Add("cairo-setup", CampaignExercises.QuerySourceCairoSetupDraft(session, audience));
            if (authority.AttachedInitialLoads is not null) Add("attached-loads", CampaignExercises.QuerySourceAttachedInitialLoadsDraft(session, audience));
            if (authority.InitialWrecks is not null) Add("initial-wrecks", CampaignExercises.QuerySourceInitialWrecksDraft(session, audience));
            if (authority.MatruhFormation is not null) Add("matruh-formation", CampaignExercises.QuerySourceMatruhFormationDraft(session, audience));
            if (authority.CwPriorityCatalog is not null) Add("cw-priority-catalog", CampaignExercises.QuerySourceCwPriorityCatalogDraft(session, audience));
            if (authority.CwFreeGuns is not null) Add("cw-free-guns", CampaignExercises.QuerySourceCwFreeGunsDraft(session, audience));
            if (authority.GroupedWorld is not null) Add("fourth-indian-motorization", CampaignExercises.QuerySourceFourthIndianDraft(session, audience));
            if (authority.IsSeventhArmoredGrouped) Add("seventh-armored-disposition", CampaignExercises.QuerySourceSeventhArmoredDraft(session, audience));
            if (authority.IsSixteenthBrigadeGrouped) Add("sixteenth-brigade-disposition", CampaignExercises.QuerySourceSixteenthBrigadeDraft(session, audience));
            if (authority.CanPlaceSixthAustralian) Add("sixth-australian-placement", CampaignExercises.QuerySourceSixthAustralianDraft(session, audience));
        }
        return drafts.OrderBy(value => value.Family, StringComparer.Ordinal).ToArray();
    }

    internal static ExerciseStepResult Submit(ExerciseSession session, CampaignActionSubmission submission)
    {
        var authority = session.SourceAuthority ?? throw new InvalidOperationException("Source authority missing.");
        if (submission.ContractVersion != CampaignActionSubmission.CurrentContractVersion || !Enum.IsDefined(submission.Audience)) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.InvalidSubmission);
        if (submission.CampaignId != authority.Request.CampaignId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.CampaignMismatch);
        if (submission.ExpectedStateVersion != authority.StateVersion) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.StaleState);
        if (submission.ExpectedPositionId != CampaignSourceAuthorityState.SetupPositionId) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.UnexpectedPosition);
        var query = Query(session, submission.Audience);
        var action = query.ActionSet?.Candidates.OfType<CampaignSourceSetupAction>().SingleOrDefault(value => value.ActionId == submission.ActionId);
        if (action is null) return ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal);
        var inner = submission with { ActionId = action.SourceActionId };
        var result = action.Family switch
        {
            "owner-finalization" => CampaignExercises.SubmitSourceOwnerFinalization(session, inner),
            "central-india-horse-ruling" or "central-india-horse-stage-one" => CampaignExercises.SubmitSourceHorseSetup(session, inner),
            "dummy" => CampaignExercises.DecideSourceDummy(session, inner),
            "motorization" => CampaignExercises.AdjustSourceMotorization(session, inner),
            "unattached-cargo" => CampaignExercises.SubmitSourceUnattachedCargo(session, inner),
            "axis-dump" => CampaignExercises.PlaceSourceAxisDump(session, inner),
            "axis-september-supply" => CampaignExercises.SubmitSourceSeptemberSupply(session, inner),
            "axis-gt1-supply" => CampaignExercises.SubmitSourceAxisGt1Supply(session, inner),
            "autoblinda" => CampaignExercises.SubmitSourceAutoblinda(session, inner),
            "axis-priority" => CampaignExercises.SubmitSourceAxisPriority(session, inner),
            "axis-free" => CampaignExercises.PlaceSourceAxisFreeUnit(session, inner),
            "axis-corps" => CampaignExercises.PlaceSourceAxisCorpsUnit(session, inner),
            "maletti-hierarchy" => CampaignExercises.SubmitSourceMalettiHierarchy(session, inner),
            "axis-gt2-correction" => CampaignExercises.SubmitSourceGt2Correction(session, inner),
            "libyan-disposition" => CampaignExercises.SubmitSourceLibyan(session, inner),
            "italian-division-disposition" => CampaignExercises.SubmitSourceItalianDivision(session, inner),
            "cw-supply" => CampaignExercises.PlaceSourceCommonwealthSupply(session, inner),
            "cw-supply-plans" => CampaignExercises.SubmitSourceCommonwealthSupplyPlan(session, inner),
            "cw-priority" => CampaignExercises.AdjustSourcePriorityMotorization(session, inner),
            "cairo-setup" => CampaignExercises.PlaceSourceCairoUnit(session, inner),
            "attached-loads" => CampaignExercises.SubmitSourceAttachedInitialLoads(session, inner),
            "initial-wrecks" => CampaignExercises.PlaceSourceInitialWreck(session, inner),
            "matruh-formation" => CampaignExercises.SubmitSourceMatruhFormation(session, inner),
            "cw-priority-catalog" => CampaignExercises.SubmitSourceCwPriorityCatalog(session, inner),
            "cw-free-guns" => CampaignExercises.PlaceSourceCwFreeGun(session, inner),
            "fourth-indian-motorization" => CampaignExercises.SubmitSourceFourthIndian(session, inner),
            "seventh-armored-disposition" => CampaignExercises.SubmitSourceSeventhArmored(session, inner),
            "catanzaro-disposition" => CampaignExercises.SubmitSourceCatanzaro(session, inner),
            "ccnn-batch-disposition" => CampaignExercises.SubmitSourceCcnnBatch(session, inner),
            "aresca-disposition" => CampaignExercises.SubmitSourceAresca(session, inner),
            "sixteenth-brigade-disposition" => CampaignExercises.SubmitSourceSixteenthBrigade(session, inner),
            "sixth-australian-placement" => CampaignExercises.SubmitSourceSixthAustralian(session, inner),
            _ => ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal),
        };
        if (!result.IsAccepted) return result;
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, result.Evidence!.Receipt.CommittedStateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(result.SuccessorSession!, new ExerciseStepEvidence(receipt, result.Evidence.EventRecords.Single(), result.Evidence.SnapshotCheckpoint));
    }
}
