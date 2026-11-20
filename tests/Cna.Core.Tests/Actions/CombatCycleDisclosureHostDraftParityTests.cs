using Cna.Core.Actions;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.Core.Tests.Actions;

/// <summary>
/// Task 020D parity, option (b) (owner ruling 2026-11-20), Core hop: for every round kind 020D ships,
/// the trusted <see cref="CampaignHostAdapter"/> draft the Host serves equals the internal Core audience
/// decision (visible bytes, offer count and order), and submitting draft action <c>i</c> records the same
/// event, snapshot checkpoint and receipt as submitting Core decision ordinal <c>i</c>. The Host hop
/// (Host opening == draft) lives in Cna.OrleansHost.Tests (HostOpeningsMatchAdapterDrafts...), because
/// Core friend access is limited to this test assembly. No Host-calls-Core cutover is made here.
/// </summary>
public sealed class CombatCycleDisclosureHostDraftParityTests
{
    [Fact]
    public void SelectionDraftMatchesCoreDecisionOrdinalByOrdinal()
    {
        var session = CombatCycleDisclosureForceTests.AdvanceToCombat("campaign-parity-selection", 12345);
        var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(session, CampaignActionAudience.Axis, "axis-infantry");
        AssertParity(session, CampaignCombatAudienceDecisions.QuerySelection(session,
                CampaignActionAudience.Axis, "axis-infantry")!,
            draft.VisibleBytes, draft.PrivateCombatActionIds,
            (command, _) => CampaignHostAdapter.SubmitContactOwnerCombatSelection(session, command, "axis-infantry"));
    }

    [Fact]
    public void DeclineOnlyRbaDraftMatchesCoreDecision()
    {
        var session = CombatCycleDisclosureRbaDeclineTests.AtRba("campaign-parity-rba", 12345);
        var draft = CampaignHostAdapter.QueryContactRbaDeclineOwnerDraft(session,
            CampaignActionAudience.Commonwealth, "commonwealth-infantry");
        // The full V1 draft still carries Core's move offers; the Host route no longer serves it.
        Assert.True(CampaignHostAdapter.QueryContactRbaOwnerDraft(session, CampaignActionAudience.Commonwealth,
            "commonwealth-infantry").PrivateActionIds.Count > 1);
        Assert.Throws<InvalidOperationException>(() => CampaignHostAdapter.QueryContactRbaDeclineOwnerDraft(
            session, CampaignActionAudience.Axis, "commonwealth-infantry"));
        Assert.Throws<InvalidOperationException>(() => CampaignHostAdapter.QueryContactRbaDeclineOwnerDraft(
            session, CampaignActionAudience.Commonwealth, "commonwealth-truck"));
        AssertParity(session, CampaignCombatAudienceDecisions.QueryRbaDecline(session,
                CampaignActionAudience.Commonwealth, "commonwealth-infantry")!,
            draft.VisibleBytes, draft.PrivateActionIds,
            (command, _) => CampaignHostAdapter.SubmitContactDefenderRbaDecline(session, command, "commonwealth-infantry"));
    }

    [Fact]
    public void ForceDraftsMatchCoreDecisionsForBothOwners()
    {
        var session = CombatCycleDisclosureForceTests.AdvanceToForce("campaign-parity-force", 12345);
        foreach (var (audience, element) in new[]
                 {
                     (CampaignActionAudience.Axis, "axis-infantry"),
                     (CampaignActionAudience.Commonwealth, "commonwealth-infantry"),
                 })
        {
            var draft = CampaignHostAdapter.QueryContactForceOwnerDraft(session, audience, element);
            AssertParity(session, CampaignCombatAudienceDecisions.QueryForceAssignment(session, audience, element)!,
                draft.VisibleBytes, draft.PrivateActionIds,
                (command, _) => CampaignHostAdapter.SubmitContactOwnerForceSeal(session, command with { Audience = audience }, element));
        }
    }

    [Fact]
    public void CustodyDraftMatchesCoreDecisionOrdinalByOrdinal()
    {
        var session = CombatCycleDisclosureCustodyTests.AtCustody("campaign-parity-custody", 5, out _);
        var draft = CampaignHostAdapter.QueryContactCustodyOwnerDraft(session, CampaignActionAudience.Axis, "axis-infantry");
        AssertParity(session, CampaignCombatAudienceDecisions.QueryCustody(session,
                CampaignActionAudience.Axis, "axis-infantry")!,
            draft.VisibleBytes, draft.PrivateActionIds,
            (command, _) => CampaignHostAdapter.SubmitNativeCombatCustody(session, command, "axis-infantry"));
    }

    [Fact]
    public void ReserveReleaseAndCycleControlDraftsMatchCoreDecisions()
    {
        var reserve = CombatCycleDisclosureReserveCycleTests.AtReserveRelease("campaign-parity-reserve", 6);
        var releaseDraft = CampaignHostAdapter.QueryNativeReserveReleaseOwnerDraft(reserve,
            CampaignActionAudience.Axis, "axis-infantry-second");
        var release = CampaignCombatAudienceDecisions.QueryReserveRelease(reserve,
            CampaignActionAudience.Axis, "axis-infantry-second")!;
        AssertParity(reserve, release, releaseDraft.VisibleBytes, releaseDraft.PrivateActionIds,
            (command, _) => CampaignHostAdapter.SubmitNativeReserveReleaseChoice(reserve, command, "axis-infantry-second"));

        var control = CombatCycleDisclosureReserveCycleTests.Complete(
            CampaignCombatAudienceDecisions.Submit(reserve, release.Choose(0)).TrustedStep!.SuccessorSession!);
        var controlDraft = CampaignHostAdapter.QueryNativeCycleControlOwnerDraft(control, CampaignActionAudience.Axis);
        AssertParity(control, CampaignCombatAudienceDecisions.QueryCycleControl(control, CampaignActionAudience.Axis)!,
            controlDraft.VisibleBytes, controlDraft.PrivateActionIds,
            (command, _) => CampaignHostAdapter.SubmitNativeCycleControl(control, command));
    }

    [Fact]
    public void SideSpellingFixKeepsRetainedReserveAndCycleActionIds()
    {
        // CMB-CON-005 freeze (2026-11-20): V27/V28 owner cuts now spell side lowercase outward, but the
        // private action IDs that retained contact-reserve-release-choice / contact-cycle-control
        // commands and receipts carry must not change (DET-001). These IDs were captured from main at
        // 513391c7, before the spelling fix, on this exact path.
        var reserve = CombatCycleDisclosureReserveCycleTests.AtReserveRelease("campaign-legacy-action-ids", 6);
        var release = CampaignHostAdapter.QueryNativeReserveReleaseOwnerDraft(reserve,
            CampaignActionAudience.Axis, "axis-infantry-second");
        Assert.Equal(
        [
            "sha256:6d259255e58bf37b7c71b72d5d4e53f0b49e594b3f39b87d659e9c42cf6e8409",
            "sha256:247ff4adf1a9bc05ff756dfe91b584489f93803ef0085f6aedf689a35bfeb4bb",
        ], release.PrivateActionIds);
        Assert.Contains("\"side\":\"axis\"", System.Text.Encoding.UTF8.GetString(release.VisibleBytes),
            StringComparison.Ordinal);
        var decision = CampaignCombatAudienceDecisions.QueryReserveRelease(reserve,
            CampaignActionAudience.Axis, "axis-infantry-second")!;
        var control = CombatCycleDisclosureReserveCycleTests.Complete(
            CampaignCombatAudienceDecisions.Submit(reserve, decision.Choose(0)).TrustedStep!.SuccessorSession!);
        var cycle = CampaignHostAdapter.QueryNativeCycleControlOwnerDraft(control, CampaignActionAudience.Axis);
        Assert.Equal(
        [
            "sha256:5776ff4f5961f29720712c2210621f631fd1bfeee9fa87d6fd98f39953e191dc",
            "sha256:5df6a8186fb502a817168c241385748b00399cc2d1bb4c52c0bb151e313d205f",
        ], cycle.PrivateActionIds);
        Assert.Contains("\"side\":\"axis\"", System.Text.Encoding.UTF8.GetString(cycle.VisibleBytes),
            StringComparison.Ordinal);
    }

    private static void AssertParity(ExerciseSession session, CampaignCombatAudienceDecision decision,
        byte[] draftVisible, IReadOnlyList<string> draftActionIds,
        Func<CampaignActionSubmission, int, ExerciseStepResult> submitDraft)
    {
        Assert.Equal(draftVisible, decision.VisibleBytes);
        Assert.Equal(draftActionIds.Count, decision.OfferRefs.Count);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        for (var ordinal = 0; ordinal < draftActionIds.Count; ordinal++)
        {
            var viaCore = CampaignCombatAudienceDecisions.Submit(session, decision.Choose(ordinal));
            Assert.True(viaCore.IsAccepted, $"{decision.Stage}[{ordinal}]");
            var viaDraft = submitDraft(new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
                checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, decision.Audience,
                draftActionIds[ordinal]), ordinal);
            Assert.True(viaDraft.IsAccepted, $"{decision.Stage}[{ordinal}]: {viaDraft.RejectionReason}");
            var core = viaCore.TrustedStep!.Evidence!;
            Assert.Equal(viaDraft.Evidence!.EventRecords.Single(), core.EventRecords.Single());
            Assert.Equal(viaDraft.Evidence.SnapshotCheckpoint, core.SnapshotCheckpoint);
            Assert.Equal(viaDraft.Evidence.Receipt, core.Receipt);
        }
    }
}
