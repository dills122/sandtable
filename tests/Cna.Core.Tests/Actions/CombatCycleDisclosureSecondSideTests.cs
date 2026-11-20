using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Actions;

/// <summary>
/// Task 020E0 (test-only): the second-acting side's own first Combat selection (Core V47) is served by
/// the existing frozen <c>combat-selection</c> stage. Core projects it through the same V2 owner cut
/// (<c>CampaignContactCombatSelectionCut.ProjectSecondCombat</c>) and accepts it through the same
/// submission path, and the Host already maps it under <c>combat-selection</c>. These tests prove the
/// v1 bytes, refs and outcome envelopes cover it unchanged; no stage or byte is added.
/// </summary>
public sealed class CombatCycleDisclosureSecondSideTests
{
    private const string SecondSideActor = "commonwealth-infantry";

    [Fact]
    public void SecondSideSelectionIsAFrozenCombatSelectionDecision()
    {
        var session = AtSecondSideCombat("campaign-disclosure-second-side", 6);
        Assert.NotNull(session.CurrentSecondBreakdown);
        var decision = CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.Commonwealth, SecondSideActor);
        Assert.NotNull(decision);
        var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(session,
            CampaignActionAudience.Commonwealth, SecondSideActor);

        Assert.Equal(CampaignCombatAudienceDecisions.SelectionStage, decision.Stage);
        Assert.Equal(1, decision.ContractVersion);
        Assert.Equal(draft.VisibleBytes, decision.VisibleBytes);
        Assert.Equal(draft.PrivateCombatActionIds.Count, decision.OfferRefs.Count);
        Assert.Equal(2, decision.OfferRefs.Count); // one current-contact assault plus finish
        Assert.True(decision.AudienceRevision >= 1);
        using (var visible = JsonDocument.Parse(decision.VisibleBytes))
        {
            Assert.Equal(2, visible.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal("commonwealth", visible.RootElement.GetProperty("side").GetString());
        }
        // The frozen v1 verifier accepts the second-side decision as-is: same envelope, same V2 cut,
        // same ref preimage and byte-equal to the production serializer.
        Assert.Empty(CampaignCombatAudienceDraftVerifier.VerifyDecision(decision.SerializeCanonical()));

        var outward = Encoding.UTF8.GetString(decision.SerializeCanonical());
        foreach (var privateId in draft.PrivateCombatActionIds)
        {
            Assert.DoesNotContain(privateId, outward, StringComparison.Ordinal);
            Assert.DoesNotContain(privateId, decision.OfferRefs);
        }
        foreach (var key in new[] { "authorityVersion", "stateVersion", "actionId", "boundRepresentationId" })
            Assert.DoesNotContain(key, outward, StringComparison.Ordinal);

        // Only the second-acting side owns this decision.
        Assert.Null(CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.Axis, SecondSideActor));
        Assert.Null(CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.Axis, "axis-infantry"));
        Assert.Null(CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.System, SecondSideActor));
    }

    [Fact]
    public void SecondSideSelectionUsesTheUnchangedV47Adjudication()
    {
        var session = AtSecondSideCombat("campaign-disclosure-second-side-accept", 6);
        var decision = CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.Commonwealth, SecondSideActor)!;
        var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(session,
            CampaignActionAudience.Commonwealth, SecondSideActor);
        var rejection = CampaignCombatAudienceSubmissionResult.Rejected.SerializeOutwardCanonical();

        for (var ordinal = 0; ordinal < decision.OfferRefs.Count; ordinal++)
        {
            var viaAudience = CampaignCombatAudienceDecisions.SubmitSelection(session, decision.Choose(ordinal));
            Assert.True(viaAudience.IsAccepted);
            Assert.Equal(decision.OfferRefs[ordinal], viaAudience.AcceptedRef);
            Assert.Empty(CampaignCombatAudienceDraftVerifier.VerifyOutcome(
                viaAudience.SerializeOutwardCanonical(), decision.SerializeCanonical()));
            var trusted = viaAudience.TrustedStep!;
            var direct = CampaignHostAdapter.SubmitContactOwnerCombatSelection(session,
                Command(session, CampaignActionAudience.Commonwealth, draft.PrivateCombatActionIds[ordinal]),
                SecondSideActor);
            Assert.True(direct.IsAccepted, direct.RejectionReason.ToString());
            Assert.Equal(direct.Evidence!.EventRecords, trusted.Evidence!.EventRecords);
            Assert.Equal(direct.Evidence.SnapshotCheckpoint, trusted.Evidence.SnapshotCheckpoint);
            Assert.Equal(direct.Evidence.Receipt, trusted.Evidence.Receipt);
            // The accepted step is the second-side V47 decision, not a first-side V12 one.
            Assert.IsType<CampaignNativeSecondCombatDecisionV47>(
                CampaignCurrentEventSerializer.Deserialize(trusted.Evidence.EventRecords[0]));
            Assert.True(CampaignExercises.Reconstruct(trusted.SuccessorSession!).IsVerified);
            // Consumed: the same ref no longer applies on the successor.
            Assert.Equal(rejection, CampaignCombatAudienceDecisions.SubmitSelection(trusted.SuccessorSession!,
                decision.Choose(ordinal)).SerializeOutwardCanonical());
        }
    }

    [Fact]
    public void EverySecondSideRejectionCauseSharesOneOutwardEnvelope()
    {
        var session = AtSecondSideCombat("campaign-disclosure-second-side-reject", 6);
        var valid = CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.Commonwealth, SecondSideActor)!.Choose(0);
        CampaignCombatAudienceSubmission[] invalid =
        [
            valid with { OfferRef = "cmbref.v1." + new string('0', 64) },
            valid with { Audience = CampaignActionAudience.Axis },
            valid with { Audience = CampaignActionAudience.System },
            valid with { ActingElementId = "axis-infantry" },
            valid with { AudienceRevision = valid.AudienceRevision + 1 },
            valid with { VisibleDigest = "sha256:" + new string('1', 64) },
            valid with { Stage = CampaignCombatAudienceDecisions.ForceAssignmentStage },
            valid with { CampaignId = "another-campaign" },
            valid with { ContractVersion = 2 },
        ];
        var rejection = CampaignCombatAudienceSubmissionResult.Rejected.SerializeOutwardCanonical();
        foreach (var submission in invalid)
        {
            Assert.Equal(rejection, CampaignCombatAudienceDecisions.Submit(session, submission)
                .SerializeOutwardCanonical());
            Assert.Empty(CampaignCombatAudienceDraftVerifier.VerifyOutcome(rejection));
        }
        Assert.Equal(rejection, CampaignCombatAudienceDecisions.SubmitForceAssignment(session, valid)
            .SerializeOutwardCanonical());
        Assert.True(CampaignCombatAudienceDecisions.Submit(session, valid).IsAccepted);
    }

    [Fact]
    public void SecondSideDecisionEncodesNoOpponentPrivateFacts()
    {
        // Different seeds give different private random streams. This is a non-leak check: when the
        // second side's own visible cut matches across forks, the decision bytes and refs match, and a
        // ref opened on one fork is accepted on the other.
        var left = AtSecondSideCombat("campaign-disclosure-second-side-fork", 6);
        var right = AtSecondSideCombat("campaign-disclosure-second-side-fork", 7);
        Assert.NotEqual(left.CurrentSnapshot.RandomState, right.CurrentSnapshot.RandomState);
        var opened = CampaignCombatAudienceDecisions.QuerySelection(left,
            CampaignActionAudience.Commonwealth, SecondSideActor)!;
        var other = CampaignCombatAudienceDecisions.QuerySelection(right,
            CampaignActionAudience.Commonwealth, SecondSideActor)!;
        Assert.Equal(opened.VisibleBytes, other.VisibleBytes);
        Assert.Equal(opened.SerializeCanonical(), other.SerializeCanonical());
        Assert.True(CampaignCombatAudienceDecisions.Submit(right, opened.Choose(0)).IsAccepted);
    }

    // First side: the frozen Reserve Release golden path (seed 6), then Reserve Release, Cycle Control
    // Finish, Truck Convoy and post-Combat passes, then the second side's Reserve, Movement completion
    // and System Breakdown, stopping at the second side's Combat position.
    internal static ExerciseSession AtSecondSideCombat(string campaignId, ulong seed)
    {
        var session = CombatCycleDisclosureReserveCycleTests.AtReserveRelease(campaignId, seed);
        while (CampaignExercises.QueryNativeReserveReleaseState(session).Pending(session.Context).Count > 0)
        {
            var member = CampaignExercises.QueryNativeReserveReleaseState(session).Pending(session.Context)[0];
            var release = CampaignCombatAudienceDecisions.QueryReserveRelease(session,
                CampaignActionAudience.Axis, member.ElementId)!;
            session = Accepted(CampaignCombatAudienceDecisions.Submit(session, release.Choose(0)));
        }
        session = CombatCycleDisclosureReserveCycleTests.Complete(session);
        var control = CampaignCombatAudienceDecisions.QueryCycleControl(session, CampaignActionAudience.Axis)!;
        string[] choices;
        using (var visible = JsonDocument.Parse(control.VisibleBytes))
            choices = visible.RootElement.GetProperty("choices").EnumerateArray()
                .Select(value => value.GetString()!).ToArray();
        session = Accepted(CampaignCombatAudienceDecisions.Submit(session,
            control.Choose(Array.IndexOf(choices, "Finish"))));

        const CampaignActionAudience first = CampaignActionAudience.Axis;
        const CampaignActionAudience second = CampaignActionAudience.Commonwealth;
        var convoy = CampaignHostAdapter.QueryTruckConvoyPassOwnerDraft(session, first);
        session = Accept(CampaignHostAdapter.SubmitTruckConvoyPass(session,
            Command(session, first, Assert.Single(convoy.PrivateActionIds))));
        while (Position(session) is { PhaseId: var phase } && phase != LandPhaseIds.ReserveDesignation)
        {
            var pass = CampaignHostAdapter.QueryPostCombatPassOwnerDraft(session, first);
            session = Accept(CampaignHostAdapter.SubmitPostCombatPass(session,
                Command(session, first, Assert.Single(pass.PrivateActionIds))));
        }
        Assert.Equal(LandActorRole.SecondActingSide, Position(session).ActorRole);
        while (Position(session).PhaseId == LandPhaseIds.ReserveDesignation)
        {
            var reserve = CampaignHostAdapter.QuerySecondReserveOwnerDraft(session, second);
            session = Accept(CampaignHostAdapter.SubmitSecondReserve(session,
                Command(session, second, reserve.PrivateActionIds[^1])));
        }
        var completion = CampaignHostAdapter.QuerySecondMovementCompletionOwnerDraft(session, second);
        session = Accept(CampaignHostAdapter.SubmitSecondMovementCompletion(session,
            Command(session, second, Assert.Single(completion.PrivateActionIds))));
        session = Accept(CampaignHostAdapter.CompleteSecondBreakdown(session));
        var position = Position(session);
        Assert.Equal(LandActorRole.SecondActingSide, position.ActorRole);
        Assert.Equal(LandSegmentIds.Combat, position.SegmentId);
        Assert.Equal(LandStepIds.PositionDetermination, position.StepId);
        return session;
    }

    private static LandSequencePosition Position(ExerciseSession session) =>
        Cna1979LandSequenceV4.CreateTurn(1).Single(value =>
            value.PositionId == CampaignExercises.QueryCheckpoint(session).PositionId);

    private static CampaignActionSubmission Command(ExerciseSession session, CampaignActionAudience audience, string action)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new(1, checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience, action);
    }

    private static ExerciseSession Accepted(CampaignCombatAudienceSubmissionResult result)
    {
        Assert.True(result.IsAccepted);
        return result.TrustedStep!.SuccessorSession!;
    }

    private static ExerciseSession Accept(ExerciseStepResult result)
    {
        Assert.True(result.IsAccepted, result.RejectionReason.ToString());
        return result.SuccessorSession!;
    }
}
