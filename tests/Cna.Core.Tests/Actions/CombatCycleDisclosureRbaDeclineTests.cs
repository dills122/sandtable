using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.Core.Tests.Actions;

/// <summary>
/// Task 020A4 (dormant): decline-only Retreat Before Assault decision with its own visible codec
/// (T020-Q7 ruled 2026-11-20, option b). RBA movement stays outside the bounded profile, so the
/// defender sees and can take only the decline; Core's move derivation is unchanged.
/// </summary>
public sealed class CombatCycleDisclosureRbaDeclineTests
{
    private const string Defender = "commonwealth-infantry";

    [Fact]
    public void DeclineOnlyDecisionOmitsMoveOffersAndPrivateFields()
    {
        var session = AtRba("campaign-disclosure-rba", 12345);
        var cut = CampaignExercises.QueryContactRbaCut(session);
        Assert.Equal(Defender, cut.DefenderElementId);
        // Core still derives move offers; the decline-only codec must not publish them.
        Assert.NotEmpty(cut.MoveOffers);
        var decision = CampaignCombatAudienceDecisions.QueryRbaDecline(session, CampaignActionAudience.Commonwealth, Defender);
        Assert.NotNull(decision);
        Assert.Equal(CampaignCombatAudienceDecisions.RbaDeclineStage, decision.Stage);
        Assert.Single(decision.OfferRefs);
        Assert.Equal(cut.ContactObservation.AudienceRevision, decision.AudienceRevision);
        using (var visible = JsonDocument.Parse(decision.VisibleBytes))
        {
            Assert.Equal(["contractVersion", "campaignId", "side", "positionId", "defenderFormationId",
                    "contactAudienceRevision", "contactObservation", "ownElements", "declineOffer"],
                visible.RootElement.EnumerateObject().Select(value => value.Name).ToArray());
            Assert.Equal("commonwealth", visible.RootElement.GetProperty("side").GetString());
            Assert.Equal("decline-retreat-before-assault",
                visible.RootElement.GetProperty("declineOffer").GetProperty("kind").GetString());
        }
        var outward = Encoding.UTF8.GetString(decision.SerializeCanonical());
        Assert.DoesNotContain("moveOffers", outward, StringComparison.Ordinal);
        Assert.DoesNotContain("retreat-before-assault-move", outward, StringComparison.Ordinal);
        Assert.DoesNotContain("initiallyAdjacentToKnownEnemy", outward, StringComparison.Ordinal);
        foreach (var privateId in cut.MoveOffers.Select(value => value.ActionId).Append(cut.DeclineOffer.ActionId))
        {
            Assert.DoesNotContain(privateId, outward, StringComparison.Ordinal);
            Assert.DoesNotContain(privateId, decision.OfferRefs);
        }
        Assert.DoesNotContain("authorityVersion", outward, StringComparison.Ordinal);
        Assert.DoesNotContain("stateVersion", outward, StringComparison.Ordinal);
        Assert.DoesNotContain("boundRepresentationId", outward, StringComparison.Ordinal);
        Assert.Empty(CampaignCombatAudienceDraftVerifier.VerifyDecision(decision.SerializeCanonical()));

        // Only the bound defender's seat and element receive it.
        Assert.Null(CampaignCombatAudienceDecisions.QueryRbaDecline(session, CampaignActionAudience.Axis, Defender));
        Assert.Null(CampaignCombatAudienceDecisions.QueryRbaDecline(session, CampaignActionAudience.Axis, "axis-infantry"));
        Assert.Null(CampaignCombatAudienceDecisions.QueryRbaDecline(session, CampaignActionAudience.Commonwealth,
            "commonwealth-truck"));
        Assert.Null(CampaignCombatAudienceDecisions.QueryRbaDecline(session, CampaignActionAudience.System, Defender));
        Assert.Equal(decision.SerializeCanonical(), CampaignCombatAudienceDecisions.QueryRbaDecline(session,
            CampaignActionAudience.Commonwealth, Defender)!.SerializeCanonical());
    }

    [Fact]
    public void AcceptedDeclineUsesTheUnchangedPrivateAdjudication()
    {
        var session = AtRba("campaign-disclosure-rba-accept", 12345);
        var decision = CampaignCombatAudienceDecisions.QueryRbaDecline(session, CampaignActionAudience.Commonwealth, Defender)!;
        var viaAudience = CampaignCombatAudienceDecisions.Submit(session, decision.Choose(0));
        Assert.True(viaAudience.IsAccepted);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        var direct = CampaignHostAdapter.SubmitContactDefenderRbaDecline(session,
            new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion, checkpoint.CampaignId,
                checkpoint.StateVersion, checkpoint.PositionId, CampaignActionAudience.Commonwealth,
                CampaignExercises.QueryContactRbaCut(session).DeclineOffer.ActionId), Defender);
        Assert.True(direct.IsAccepted, direct.RejectionReason.ToString());
        var trusted = viaAudience.TrustedStep!;
        Assert.Equal(direct.Evidence!.EventRecords.Single(), trusted.Evidence!.EventRecords.Single());
        Assert.Equal(direct.Evidence.SnapshotCheckpoint, trusted.Evidence.SnapshotCheckpoint);
        Assert.Equal(direct.Evidence.Receipt, trusted.Evidence.Receipt);
        Assert.True(CampaignExercises.Reconstruct(trusted.SuccessorSession!).IsVerified);
        Assert.Null(CampaignCombatAudienceDecisions.QueryRbaDecline(trusted.SuccessorSession!,
            CampaignActionAudience.Commonwealth, Defender));
        Assert.False(CampaignCombatAudienceDecisions.Submit(trusted.SuccessorSession!, decision.Choose(0)).IsAccepted);
        // The decline leads into Force Assignment, where both owners now have seal decisions.
        Assert.NotNull(CampaignCombatAudienceDecisions.QueryForceAssignment(trusted.SuccessorSession!,
            CampaignActionAudience.Commonwealth, Defender));
    }

    [Fact]
    public void EveryRbaDeclineRejectionCauseSharesOneOutwardEnvelope()
    {
        var session = AtRba("campaign-disclosure-rba-reject", 12345);
        var valid = CampaignCombatAudienceDecisions.QueryRbaDecline(session, CampaignActionAudience.Commonwealth,
            Defender)!.Choose(0);
        var cut = CampaignExercises.QueryContactRbaCut(session);
        CampaignCombatAudienceSubmission[] invalid =
        [
            valid with { OfferRef = "cmbref.v1." + new string('0', 64) },
            valid with { OfferRef = cut.DeclineOffer.ActionId },
            valid with { OfferRef = cut.MoveOffers[0].ActionId },
            valid with { Audience = CampaignActionAudience.Axis },
            valid with { Audience = CampaignActionAudience.System },
            valid with { ActingElementId = "axis-infantry" },
            valid with { ActingElementId = "commonwealth-truck" },
            valid with { AudienceRevision = valid.AudienceRevision + 1 },
            valid with { VisibleDigest = "sha256:" + new string('1', 64) },
            valid with { Stage = "retreat-before-assault" },
            valid with { Stage = CampaignCombatAudienceDecisions.ForceAssignmentStage },
            valid with { CampaignId = "another-campaign" },
            valid with { ContractVersion = 2 },
        ];
        var rejection = CampaignCombatAudienceSubmissionResult.Rejected.SerializeOutwardCanonical();
        foreach (var submission in invalid)
        {
            var result = CampaignCombatAudienceDecisions.Submit(session, submission);
            Assert.Null(result.TrustedStep);
            Assert.Equal(rejection, result.SerializeOutwardCanonical());
        }
        Assert.Equal(rejection, CampaignCombatAudienceDecisions.SubmitSelection(session, valid)
            .SerializeOutwardCanonical());
        Assert.True(CampaignCombatAudienceDecisions.Submit(session, valid).IsAccepted);
    }

    [Fact]
    public void CancelledRbaWindowOffersNoDecision()
    {
        var session = AtRba("campaign-disclosure-rba-cancel", 12345);
        var timing = new CampaignDecisionTimingConfigurationV1("rules-lab.rba-timing.1",
            session.CurrentSnapshot.RulesetHash, session.Context.ArtifactV6!.Identity.Hash,
            [new("custody", 30000, "leave-unguarded"), new("cycle-control", 30000, "finish-phase"),
                new("force-assignment", 30000, "cancel-incomplete-voluntary-round"),
                new("rba", 30000, "cancel-without-decline"), new("reserve-release", 30000, "convert-unresolved-i-retain-ii"),
                new("retreat", 30000, "refuse-retreat"), new("selection", 30000, "no-selection")]);
        var opened = CampaignCombatAudienceDecisions.QueryRbaDecline(session, CampaignActionAudience.Commonwealth, Defender)!;
        var cancelled = CampaignHostAdapter.CancelNativeCombatRba(session, timing, 1000, 31000, 2000, 31000, "deadline");
        Assert.True(cancelled.IsAccepted, cancelled.RejectionReason.ToString());
        Assert.Null(CampaignCombatAudienceDecisions.QueryRbaDecline(cancelled.SuccessorSession!,
            CampaignActionAudience.Commonwealth, Defender));
        Assert.False(CampaignCombatAudienceDecisions.Submit(cancelled.SuccessorSession!, opened.Choose(0)).IsAccepted);
    }

    [Fact]
    public void DeclineDecisionsAreEqualAcrossAHiddenRandomFork()
    {
        var left = AtRba("campaign-disclosure-rba-fork", 12345);
        var right = AtRba("campaign-disclosure-rba-fork", 54321);
        Assert.NotEqual(left.CurrentSnapshot.RandomState, right.CurrentSnapshot.RandomState);
        var opened = CampaignCombatAudienceDecisions.QueryRbaDecline(left, CampaignActionAudience.Commonwealth, Defender)!;
        Assert.Equal(opened.SerializeCanonical(), CampaignCombatAudienceDecisions.QueryRbaDecline(right,
            CampaignActionAudience.Commonwealth, Defender)!.SerializeCanonical());
        Assert.True(CampaignCombatAudienceDecisions.Submit(right, opened.Choose(0)).IsAccepted);
    }

    // Combat-assault fixture: selection through the audience surface, then the System Barrage.
    internal static ExerciseSession AtRba(string campaignId, ulong seed)
    {
        var session = CombatCycleDisclosureForceTests.AdvanceToCombat(campaignId, seed);
        var selection = CampaignCombatAudienceDecisions.QuerySelection(session, CampaignActionAudience.Axis,
            "axis-infantry")!;
        var selected = CampaignCombatAudienceDecisions.SubmitSelection(session, selection.Choose(0));
        Assert.True(selected.IsAccepted);
        var barrage = CampaignHostAdapter.CompleteNativeCombatBarrage(selected.TrustedStep!.SuccessorSession!);
        Assert.True(barrage.IsAccepted, barrage.RejectionReason.ToString());
        return barrage.SuccessorSession!;
    }
}
