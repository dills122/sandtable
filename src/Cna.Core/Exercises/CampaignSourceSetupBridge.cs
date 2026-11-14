using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;

namespace Cna.Core.Exercises;

// Maps the normal action contract onto the existing source adjudicators.
// Only the requesting side's existing visible choices enter this bridge.
internal static class CampaignSourceSetupBridge
{
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
            _ => ExerciseStepResult.Rejected(CampaignActionSubmissionRejectionReason.ActionNotLegal),
        };
        if (!result.IsAccepted) return result;
        var receipt = new CampaignActionAcceptanceReceipt(authority.Request.CampaignId, authority.StateVersion, result.Evidence!.Receipt.CommittedStateVersion,
            CampaignSourceAuthorityState.SetupPositionId, submission.Audience, submission.ActionId);
        return ExerciseStepResult.Accepted(result.SuccessorSession!, new ExerciseStepEvidence(receipt, result.Evidence.EventRecords.Single(), result.Evidence.SnapshotCheckpoint));
    }
}
