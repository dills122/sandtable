using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.Core.Tests.Actions;

/// <summary>
/// Task 020E3: audience-safe repeated-cycle Movement (Core V29), a combat-audience-decision v2
/// superset stage, one decision per side after Cycle Control <c>Repeat</c>. The visible cut is the
/// existing <c>CampaignRepeatMovementOwnerCutV29</c>, now spelling <c>side</c> lowercase (owner ruling
/// 2026-11-20). It carries no contact observation, so the revision is 0 and the visible digest is the
/// only fence, as for Reserve Release, Cycle Control and Custody.
/// </summary>
public sealed class CombatCycleDisclosureRepeatMovementTests
{
    [Fact]
    public void RepeatMovementDecisionMatchesHostDraftAndCarriesNoPrivateFields()
    {
        var session = CombatCycleDisclosureRepeatCombatTests.AtRepeatMovement("campaign-disclosure-repeat-move", 6);
        var decision = CampaignCombatAudienceDecisions.QueryRepeatMovement(session, CampaignActionAudience.Axis);
        Assert.NotNull(decision);
        var draft = CampaignHostAdapter.QueryNativeRepeatMovementOwnerDraft(session, CampaignActionAudience.Axis);

        Assert.Equal(CampaignCombatAudienceDecisions.RepeatMovementStage, decision.Stage);
        Assert.Equal(CampaignCombatAudienceDecisions.RepeatMovementSubject, decision.ActingElementId);
        Assert.Equal(CampaignCombatAudienceDecision.SuccessorContractVersion, decision.ContractVersion);
        Assert.StartsWith("{\"contractVersion\":2,", Encoding.UTF8.GetString(decision.SerializeCanonical()),
            StringComparison.Ordinal);
        Assert.Equal(draft.VisibleBytes, decision.VisibleBytes);
        Assert.Equal(draft.PrivateActionIds.Count, decision.OfferRefs.Count);
        Assert.NotEmpty(decision.OfferRefs);
        Assert.Equal(0, decision.AudienceRevision);
        using (var visible = JsonDocument.Parse(decision.VisibleBytes))
        {
            Assert.Equal(29, visible.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal("axis", visible.RootElement.GetProperty("side").GetString());
            Assert.Equal(decision.OfferRefs.Count, visible.RootElement.GetProperty("offers").GetArrayLength());
        }
        var outward = Encoding.UTF8.GetString(decision.SerializeCanonical());
        foreach (var privateId in draft.PrivateActionIds)
        {
            Assert.DoesNotContain(privateId, outward, StringComparison.Ordinal);
            Assert.DoesNotContain(privateId, decision.OfferRefs);
        }
        foreach (var key in new[] { "authorityVersion", "stateVersion", "actionId", "worldHash", "headEventHash" })
            Assert.DoesNotContain("\"" + key + "\"", outward, StringComparison.Ordinal);

        Assert.Null(CampaignCombatAudienceDecisions.QueryRepeatMovement(session, CampaignActionAudience.Commonwealth));
        Assert.Null(CampaignCombatAudienceDecisions.QueryRepeatMovement(session, CampaignActionAudience.System));
        Assert.Null(CampaignCombatAudienceDecisions.Query(session, CampaignActionAudience.Axis,
            CampaignCombatAudienceDecisions.RepeatMovementStage, "axis-infantry-second"));
        Assert.Contains("stage-uncovered", CampaignCombatAudienceDraftVerifier.VerifyDecision(
            decision.SerializeCanonical()));
    }

    [Fact]
    public void SideSpellingFixKeepsRetainedRepeatMovementActionIds()
    {
        // Captured from main c1e0a613, before the V29 side spelling fix, on this exact path. V29 action
        // IDs are MoveElementAction semantic IDs (kind + element, origin, destination, cost), not
        // hashes of the visible bytes, so the fix changes visible bytes only and retained
        // contact-repeat-movement steps replay unchanged (DET-001).
        var session = CombatCycleDisclosureRepeatCombatTests.AtRepeatMovement("campaign-legacy-v29-action-ids", 6);
        var draft = CampaignHostAdapter.QueryNativeRepeatMovementOwnerDraft(session, CampaignActionAudience.Axis);
        Assert.Equal(
            [
                "sha256:8b9545698c76bb9725069a11191a14be6f766ddc80eae770ed5160d2742ce736",
                "sha256:fa8ce7e3a663a89585dea948e2739bb624118ac0ec09e7a5166d012c918ee5fe",
            ],
            draft.PrivateActionIds);
        var visible = Encoding.UTF8.GetString(draft.VisibleBytes);
        Assert.Contains("\"side\":\"axis\"", visible, StringComparison.Ordinal);
        Assert.DoesNotContain("\"Axis\"", visible, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptedRepeatMovementUsesTheUnchangedV29Adjudication()
    {
        var session = CombatCycleDisclosureRepeatCombatTests.AtRepeatMovement("campaign-disclosure-repeat-move-accept", 6);
        var decision = CampaignCombatAudienceDecisions.QueryRepeatMovement(session, CampaignActionAudience.Axis)!;
        var draft = CampaignHostAdapter.QueryNativeRepeatMovementOwnerDraft(session, CampaignActionAudience.Axis);
        var rejection = CampaignCombatAudienceSubmissionResult.Rejected.SerializeOutwardCanonical();
        for (var ordinal = 0; ordinal < decision.OfferRefs.Count; ordinal++)
        {
            var viaAudience = CampaignCombatAudienceDecisions.Submit(session, decision.Choose(ordinal));
            Assert.True(viaAudience.IsAccepted);
            Assert.Equal(decision.OfferRefs[ordinal], viaAudience.AcceptedRef);
            var direct = CampaignHostAdapter.SubmitNativeRepeatMovement(session,
                Command(session, CampaignActionAudience.Axis, draft.PrivateActionIds[ordinal]));
            var trusted = viaAudience.TrustedStep!;
            Assert.True(direct.IsAccepted, direct.RejectionReason.ToString());
            Assert.Equal(direct.Evidence!.EventRecords, trusted.Evidence!.EventRecords);
            Assert.Equal(direct.Evidence.SnapshotCheckpoint, trusted.Evidence.SnapshotCheckpoint);
            Assert.Equal(direct.Evidence.Receipt, trusted.Evidence.Receipt);
            Assert.IsType<CampaignNativeRepeatMovementV29>(
                CampaignCurrentEventSerializer.Deserialize(trusted.Evidence.EventRecords[0]));
            Assert.True(CampaignExercises.Reconstruct(trusted.SuccessorSession!).IsVerified);
            Assert.Equal(rejection, CampaignCombatAudienceDecisions.Submit(trusted.SuccessorSession!,
                decision.Choose(ordinal)).SerializeOutwardCanonical());
        }
    }

    [Fact]
    public void EveryRepeatMovementRejectionCauseSharesOneOutwardEnvelope()
    {
        var session = CombatCycleDisclosureRepeatCombatTests.AtRepeatMovement("campaign-disclosure-repeat-move-reject", 6);
        var valid = CampaignCombatAudienceDecisions.QueryRepeatMovement(session, CampaignActionAudience.Axis)!.Choose(0);
        CampaignCombatAudienceSubmission[] invalid =
        [
            valid with { OfferRef = "cmbref.v1." + new string('0', 64) },
            valid with { Audience = CampaignActionAudience.Commonwealth },
            valid with { Audience = CampaignActionAudience.System },
            valid with { ActingElementId = "axis-infantry-second" },
            valid with { ActingElementId = CampaignCombatAudienceDecisions.CycleControlSubject },
            valid with { AudienceRevision = 1 },
            valid with { VisibleDigest = "sha256:" + new string('1', 64) },
            valid with { Stage = CampaignCombatAudienceDecisions.CycleControlStage },
            valid with { Stage = CampaignCombatAudienceDecisions.RepeatCombatSelectionStage },
            valid with { CampaignId = "another-campaign" },
            valid with { ContractVersion = CampaignCombatAudienceSubmission.CurrentContractVersion },
        ];
        var rejection = CampaignCombatAudienceSubmissionResult.Rejected.SerializeOutwardCanonical();
        foreach (var submission in invalid)
            Assert.Equal(rejection, CampaignCombatAudienceDecisions.Submit(session, submission)
                .SerializeOutwardCanonical());
        Assert.True(CampaignCombatAudienceDecisions.Submit(session, valid).IsAccepted);
    }

    [Fact]
    public void RepeatMovementDecisionEncodesNoOpponentPrivateFacts()
    {
        var left = CombatCycleDisclosureRepeatCombatTests.AtRepeatMovement("campaign-disclosure-repeat-move-fork", 6);
        var right = CombatCycleDisclosureRepeatCombatTests.AtRepeatMovement("campaign-disclosure-repeat-move-fork", 7);
        Assert.NotEqual(left.CurrentSnapshot.RandomState, right.CurrentSnapshot.RandomState);
        var opened = CampaignCombatAudienceDecisions.QueryRepeatMovement(left, CampaignActionAudience.Axis)!;
        Assert.Equal(opened.SerializeCanonical(), CampaignCombatAudienceDecisions.QueryRepeatMovement(right,
            CampaignActionAudience.Axis)!.SerializeCanonical());
        Assert.True(CampaignCombatAudienceDecisions.Submit(right, opened.Choose(0)).IsAccepted);
    }

    private static CampaignActionSubmission Command(ExerciseSession session, CampaignActionAudience audience, string action)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new(1, checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience, action);
    }
}
