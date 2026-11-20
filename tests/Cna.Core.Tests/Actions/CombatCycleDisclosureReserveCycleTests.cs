using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Actions;

/// <summary>
/// Task 020A2 (dormant): audience-safe Reserve Release and Cycle Control decisions. Custody is not
/// covered (blocked on T020-Q5); earlier Combat steps use the existing trusted path.
/// </summary>
public sealed class CombatCycleDisclosureReserveCycleTests
{
    private const string ReserveMember = "axis-infantry-second";

    [Fact]
    public void ReserveReleaseDecisionMatchesHostDraftAndCarriesNoPrivateFields()
    {
        var session = AtReserveRelease("campaign-disclosure-reserve", 6);
        var decision = CampaignCombatAudienceDecisions.QueryReserveRelease(session,
            CampaignActionAudience.Axis, ReserveMember);
        Assert.NotNull(decision);
        var draft = CampaignHostAdapter.QueryNativeReserveReleaseOwnerDraft(session,
            CampaignActionAudience.Axis, ReserveMember);
        Assert.Equal(draft.VisibleBytes, decision.VisibleBytes);
        Assert.Equal(draft.PrivateActionIds.Count, decision.OfferRefs.Count);
        Assert.Equal(0, decision.AudienceRevision);
        AssertNoPrivateFields(decision, draft.PrivateActionIds);
        Assert.Null(CampaignCombatAudienceDecisions.QueryReserveRelease(session,
            CampaignActionAudience.Commonwealth, ReserveMember));
        Assert.Null(CampaignCombatAudienceDecisions.QueryReserveRelease(session,
            CampaignActionAudience.Axis, "axis-infantry"));
        Assert.Null(CampaignCombatAudienceDecisions.QueryReserveRelease(session,
            CampaignActionAudience.System, ReserveMember));
        // Cycle control is not offered until Reserve Release completes.
        Assert.Null(CampaignCombatAudienceDecisions.QueryCycleControl(session, CampaignActionAudience.Axis));
    }

    [Fact]
    public void AcceptedReserveAndCycleChoicesUseTheUnchangedPrivateAdjudication()
    {
        var session = AtReserveRelease("campaign-disclosure-reserve-accept", 6);
        var release = CampaignCombatAudienceDecisions.QueryReserveRelease(session,
            CampaignActionAudience.Axis, ReserveMember)!;
        var releaseDraft = CampaignHostAdapter.QueryNativeReserveReleaseOwnerDraft(session,
            CampaignActionAudience.Axis, ReserveMember);
        ExerciseSession? afterRelease = null;
        for (var ordinal = 0; ordinal < release.OfferRefs.Count; ordinal++)
        {
            var viaAudience = CampaignCombatAudienceDecisions.Submit(session, release.Choose(ordinal));
            Assert.True(viaAudience.IsAccepted);
            var direct = CampaignHostAdapter.SubmitNativeReserveReleaseChoice(session,
                Command(session, CampaignActionAudience.Axis, releaseDraft.PrivateActionIds[ordinal]),
                ReserveMember);
            AssertSameEvidence(direct, viaAudience.TrustedStep!);
            Assert.False(CampaignCombatAudienceDecisions.Submit(viaAudience.TrustedStep!.SuccessorSession!,
                release.Choose(ordinal)).IsAccepted);
            afterRelease ??= viaAudience.TrustedStep.SuccessorSession;
        }

        var completed = CampaignHostAdapter.CompleteNativeReserveRelease(afterRelease!);
        Assert.True(completed.IsAccepted, completed.RejectionReason.ToString());
        var atControl = completed.SuccessorSession!;
        var control = CampaignCombatAudienceDecisions.QueryCycleControl(atControl, CampaignActionAudience.Axis);
        Assert.NotNull(control);
        var controlDraft = CampaignHostAdapter.QueryNativeCycleControlOwnerDraft(atControl,
            CampaignActionAudience.Axis);
        Assert.Equal(controlDraft.VisibleBytes, control.VisibleBytes);
        Assert.Equal(0, control.AudienceRevision);
        AssertNoPrivateFields(control, controlDraft.PrivateActionIds);
        Assert.Null(CampaignCombatAudienceDecisions.QueryCycleControl(atControl,
            CampaignActionAudience.Commonwealth));
        Assert.Null(CampaignCombatAudienceDecisions.Query(atControl, CampaignActionAudience.Axis,
            CampaignCombatAudienceDecisions.CycleControlStage, "axis-infantry"));
        for (var ordinal = 0; ordinal < control.OfferRefs.Count; ordinal++)
        {
            var viaAudience = CampaignCombatAudienceDecisions.Submit(atControl, control.Choose(ordinal));
            Assert.True(viaAudience.IsAccepted);
            var direct = CampaignHostAdapter.SubmitNativeCycleControl(atControl,
                Command(atControl, CampaignActionAudience.Axis, controlDraft.PrivateActionIds[ordinal]));
            AssertSameEvidence(direct, viaAudience.TrustedStep!);
            Assert.True(CampaignExercises.Reconstruct(viaAudience.TrustedStep!.SuccessorSession!).IsVerified);
            Assert.Null(CampaignCombatAudienceDecisions.QueryCycleControl(
                viaAudience.TrustedStep.SuccessorSession!, CampaignActionAudience.Axis));
        }
    }

    [Fact]
    public void EveryReserveAndCycleRejectionCauseSharesOneOutwardEnvelope()
    {
        var session = AtReserveRelease("campaign-disclosure-reserve-reject", 6);
        var valid = CampaignCombatAudienceDecisions.QueryReserveRelease(session,
            CampaignActionAudience.Axis, ReserveMember)!.Choose(0);
        CampaignCombatAudienceSubmission[] invalid =
        [
            valid with { OfferRef = "cmbref.v1." + new string('0', 64) },
            valid with { Audience = CampaignActionAudience.Commonwealth },
            valid with { ActingElementId = "axis-infantry" },
            valid with { AudienceRevision = 1 },
            valid with { VisibleDigest = "sha256:" + new string('1', 64) },
            valid with { Stage = CampaignCombatAudienceDecisions.CycleControlStage },
            valid with { Stage = CampaignCombatAudienceDecisions.ForceAssignmentStage },
            valid with { CampaignId = "another-campaign" },
        ];
        var rejection = CampaignCombatAudienceSubmissionResult.Rejected.SerializeOutwardCanonical();
        foreach (var submission in invalid)
            Assert.Equal(rejection, CampaignCombatAudienceDecisions.Submit(session, submission)
                .SerializeOutwardCanonical());
        Assert.Equal(rejection, CampaignCombatAudienceDecisions.SubmitSelection(session, valid)
            .SerializeOutwardCanonical());
        Assert.True(CampaignCombatAudienceDecisions.Submit(session, valid).IsAccepted);
    }

    [Fact]
    public void ReserveAndCycleDecisionsEncodeNoSettlementOrOpponentFacts()
    {
        // Different seeds give different private random streams (and may give different assault
        // dice). This is a non-leak check, not an equal-visible-history proof. The owner's
        // Reserve Release and Cycle Control decisions name only own reserve status and choices, so
        // they are byte-identical whenever those match, and a ref opened on one fork applies to the other.
        var left = AtReserveRelease("campaign-disclosure-reserve-fork", 6);
        var right = AtReserveRelease("campaign-disclosure-reserve-fork", 7);
        Assert.NotEqual(left.CurrentSnapshot.RandomState, right.CurrentSnapshot.RandomState);
        var opened = CampaignCombatAudienceDecisions.QueryReserveRelease(left,
            CampaignActionAudience.Axis, ReserveMember)!;
        Assert.Equal(opened.SerializeCanonical(), CampaignCombatAudienceDecisions.QueryReserveRelease(right,
            CampaignActionAudience.Axis, ReserveMember)!.SerializeCanonical());
        var onLeft = CampaignCombatAudienceDecisions.Submit(left, opened.Choose(0));
        var onRight = CampaignCombatAudienceDecisions.Submit(right, opened.Choose(0));
        Assert.True(onLeft.IsAccepted);
        Assert.True(onRight.IsAccepted);
        var leftControl = Complete(onLeft.TrustedStep!.SuccessorSession!);
        var rightControl = Complete(onRight.TrustedStep!.SuccessorSession!);
        var leftDecision = CampaignCombatAudienceDecisions.QueryCycleControl(leftControl, CampaignActionAudience.Axis)!;
        var rightDecision = CampaignCombatAudienceDecisions.QueryCycleControl(rightControl, CampaignActionAudience.Axis)!;
        Assert.Equal(leftDecision.SerializeCanonical(), rightDecision.SerializeCanonical());
        Assert.True(CampaignCombatAudienceDecisions.Submit(rightControl, leftDecision.Choose(
            leftDecision.OfferRefs.Count - 1)).IsAccepted);
    }

    internal static ExerciseSession Complete(ExerciseSession session)
    {
        var completed = CampaignHostAdapter.CompleteNativeReserveRelease(session);
        Assert.True(completed.IsAccepted, completed.RejectionReason.ToString());
        return completed.SuccessorSession!;
    }

    private static void AssertNoPrivateFields(CampaignCombatAudienceDecision decision,
        IReadOnlyList<string> privateIds)
    {
        var outward = Encoding.UTF8.GetString(decision.SerializeCanonical());
        foreach (var privateId in privateIds)
        {
            Assert.DoesNotContain(privateId, outward, StringComparison.Ordinal);
            Assert.DoesNotContain(privateId, decision.OfferRefs);
        }
        Assert.DoesNotContain("authorityVersion", outward, StringComparison.Ordinal);
        Assert.DoesNotContain("stateVersion", outward, StringComparison.Ordinal);
        Assert.DoesNotContain("headEventHash", outward, StringComparison.Ordinal);
        Assert.DoesNotContain("worldHash", outward, StringComparison.Ordinal);
    }

    private static void AssertSameEvidence(ExerciseStepResult direct, ExerciseStepResult trusted)
    {
        Assert.True(direct.IsAccepted, direct.RejectionReason.ToString());
        Assert.Equal(direct.Evidence!.EventRecords.Single(), trusted.Evidence!.EventRecords.Single());
        Assert.Equal(direct.Evidence.SnapshotCheckpoint, trusted.Evidence.SnapshotCheckpoint);
        Assert.Equal(direct.Evidence.Receipt, trusted.Evidence.Receipt);
    }

    // Same trusted path as CampaignNativeSelectionTimeoutTests.AtSelection(repeated: true), stopped at
    // the first owned Reserve Release member. Pre-Release Combat steps are outside this slice.
    internal static ExerciseSession AtReserveRelease(string campaignId, ulong seed)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
            value.SetupId == "rules-lab.breakdown.reaction.combat-double-fresh.v1"));
        var request = new CampaignCreationRequest(1, campaignId, Cna1979Ruleset.Manifest.Hash,
            seed, setup.SetupId, setup.SetupHash, setup.Content.Pack.PackId, setup.Content.Pack.Hash,
            setup.Content.ScenarioId);
        var session = CampaignExercises.Begin(request).Session!;
        var reserveDesignated = false;
        for (var index = 0; index < 80; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.SegmentId == LandSegmentIds.Combat && position.StepId == LandStepIds.PositionDetermination) break;
            var set = Enum.GetValues<CampaignActionAudience>().Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .First(value => value?.Candidates.Count > 0)!;
            var reserve = !reserveDesignated ? set.Candidates.OfType<DesignateReserveAction>()
                .FirstOrDefault(value => value.ElementId == ReserveMember) : null;
            var action = reserve ?? set.Candidates.FirstOrDefault(value => value is not MoveElementAction) ?? set.Candidates[0];
            if (reserve is not null) reserveDesignated = true;
            session = CampaignExercises.Submit(session, new CampaignActionSubmission(1, set.CampaignId,
                set.StateVersion, set.PositionId, set.Audience, action.ActionId)).SuccessorSession!;
        }
        session = Accept(CampaignHostAdapter.SubmitContactOwnerCombatSelection(session, Command(session,
            CampaignActionAudience.Axis, CampaignExercises.QueryContactOwnerCombatCut(session, LandSide.Axis, "axis-infantry")
                .AssaultOffers.Single(value => value.ApparentLocationId == "north-east").ActionId), "axis-infantry"));
        session = Accept(CampaignHostAdapter.CompleteNativeCombatBarrage(session));
        var rba = CampaignExercises.QueryContactRbaCut(session);
        session = Accept(CampaignExercises.SubmitContactDefenderRbaDecline(session,
            Command(session, CampaignActionAudience.Commonwealth, rba.DeclineOffer.ActionId), rba.DefenderElementId));
        foreach (var audience in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
        {
            var element = audience == CampaignActionAudience.Axis ? "axis-infantry" : rba.DefenderElementId;
            var force = CampaignHostAdapter.QueryContactForceOwnerDraft(session, audience, element);
            session = Accept(CampaignHostAdapter.SubmitContactOwnerForceSeal(session,
                Command(session, audience, force.PrivateActionIds[0]), element));
        }
        session = Accept(CampaignHostAdapter.CompleteNativeCombatForceAssignment(session));
        session = Accept(CampaignHostAdapter.CompleteNativeCombatAntiArmor(session));
        session = Accept(CampaignHostAdapter.ResolveNativeCombatAssault(session));
        if (session.CurrentCombatAssaultResult!.Facts.RequiredRetreat == 1)
        {
            var cut = CampaignExercises.QueryContactPostResultRetreatCut(session);
            session = Accept(CampaignExercises.SubmitContactPostResultRetreat(session,
                Command(session, CampaignActionAudience.Commonwealth, cut.RefuseOffer.ActionId), cut.DefenderElementId));
        }
        else session = Accept(CampaignHostAdapter.CompleteNoPostAssaultRetreat(session));
        session = Accept(CampaignHostAdapter.SettleNativeCombatWorld(session));
        var custody = CampaignExercises.QueryNativeCombatCustodyCut(session);
        if (custody is not null)
        {
            var unguarded = custody.Choices.Single(value => value.Kind == "leave-unguarded");
            session = Accept(CampaignExercises.SubmitNativeCombatCustody(session,
                Command(session, CampaignActionAudience.Axis, unguarded.ActionId)));
        }
        else session = Accept(CampaignHostAdapter.CompleteNoNativeCombatCustody(session));
        session = Accept(CampaignHostAdapter.CompleteNativeCombatSuccessor(session));
        Assert.NotEmpty(CampaignExercises.QueryNativeReserveReleaseState(session).Pending(session.Context));
        return session;
    }

    private static CampaignActionSubmission Command(ExerciseSession session, CampaignActionAudience audience, string action)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new(1, checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience, action);
    }

    private static ExerciseSession Accept(ExerciseStepResult result)
    {
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        return result.SuccessorSession!;
    }
}
