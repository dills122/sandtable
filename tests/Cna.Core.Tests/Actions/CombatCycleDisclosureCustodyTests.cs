using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Actions;

/// <summary>
/// Task 020A3 (dormant): audience-safe captor Custody decision against the existing V1 captor cut,
/// whose field set is the confirmed inference allowlist (T020-Q5, resolved 2026-11-20).
/// </summary>
public sealed class CombatCycleDisclosureCustodyTests
{
    private const string Captor = "axis-infantry";

    [Fact]
    public void CustodyDecisionMatchesHostDraftAndCarriesOnlyTheAllowlistedFields()
    {
        var session = AtCustody("campaign-disclosure-custody", 5, out _);
        var cut = CampaignExercises.QueryNativeCombatCustodyCut(session)!;
        Assert.Equal(Captor, cut.CaptorElementId);
        var decision = CampaignCombatAudienceDecisions.QueryCustody(session, CampaignActionAudience.Axis, Captor);
        Assert.NotNull(decision);
        var draft = CampaignHostAdapter.QueryContactCustodyOwnerDraft(session, CampaignActionAudience.Axis, Captor);
        Assert.Equal(draft.VisibleBytes, decision.VisibleBytes);
        Assert.Equal(draft.PrivateActionIds.Count, decision.OfferRefs.Count);
        Assert.Equal(0, decision.AudienceRevision);
        Assert.Equal(CampaignCombatAudienceDecisions.CustodyStage, decision.Stage);

        // Allowlist (T020-Q5): exactly the V1 captor fields, in order.
        using (var visible = System.Text.Json.JsonDocument.Parse(decision.VisibleBytes))
        {
            Assert.Equal(["contractVersion", "campaignId", "captorElementId", "captiveQuantity", "offers"],
                visible.RootElement.EnumerateObject().Select(value => value.Name).ToArray());
            Assert.All(visible.RootElement.GetProperty("offers").EnumerateArray(), offer =>
                Assert.Equal(["kind", "route"], offer.EnumerateObject().Select(value => value.Name).ToArray()));
        }
        var outward = Encoding.UTF8.GetString(decision.SerializeCanonical());
        foreach (var privateId in draft.PrivateActionIds)
        {
            Assert.DoesNotContain(privateId, outward, StringComparison.Ordinal);
            Assert.DoesNotContain(privateId, decision.OfferRefs);
        }
        Assert.DoesNotContain(cut.VictimElementId, outward, StringComparison.Ordinal);
        Assert.DoesNotContain(cut.LotId, outward, StringComparison.Ordinal);
        Assert.DoesNotContain(cut.SettlementHash, outward, StringComparison.Ordinal);
        Assert.DoesNotContain("authorityVersion", outward, StringComparison.Ordinal);
        Assert.DoesNotContain("stateVersion", outward, StringComparison.Ordinal);

        Assert.Null(CampaignCombatAudienceDecisions.QueryCustody(session, CampaignActionAudience.Commonwealth, Captor));
        Assert.Null(CampaignCombatAudienceDecisions.QueryCustody(session, CampaignActionAudience.Commonwealth,
            cut.VictimElementId));
        Assert.Null(CampaignCombatAudienceDecisions.QueryCustody(session, CampaignActionAudience.Axis,
            "axis-infantry-second"));
        Assert.Null(CampaignCombatAudienceDecisions.QueryCustody(session, CampaignActionAudience.System, Captor));
    }

    [Fact]
    public void NoCaptureYieldsNoCustodyDecision()
    {
        var session = AtCustody("campaign-disclosure-custody-none", 6, out _);
        Assert.Null(CampaignExercises.QueryNativeCombatCustodyCut(session));
        Assert.Null(CampaignCombatAudienceDecisions.QueryCustody(session, CampaignActionAudience.Axis, Captor));
        Assert.Null(CampaignCombatAudienceDecisions.QueryCustody(session, CampaignActionAudience.Commonwealth,
            "commonwealth-infantry"));
    }

    [Fact]
    public void AcceptedCustodyChoiceUsesTheUnchangedPrivateAdjudication()
    {
        var session = AtCustody("campaign-disclosure-custody-accept", 5, out _);
        var decision = CampaignCombatAudienceDecisions.QueryCustody(session, CampaignActionAudience.Axis, Captor)!;
        var draft = CampaignHostAdapter.QueryContactCustodyOwnerDraft(session, CampaignActionAudience.Axis, Captor);
        Assert.Equal(2, decision.OfferRefs.Count);
        for (var ordinal = 0; ordinal < decision.OfferRefs.Count; ordinal++)
        {
            var viaAudience = CampaignCombatAudienceDecisions.Submit(session, decision.Choose(ordinal));
            Assert.True(viaAudience.IsAccepted);
            var direct = CampaignHostAdapter.SubmitNativeCombatCustody(session,
                Command(session, CampaignActionAudience.Axis, draft.PrivateActionIds[ordinal]), Captor);
            Assert.True(direct.IsAccepted, direct.RejectionReason.ToString());
            var trusted = viaAudience.TrustedStep!;
            Assert.Equal(direct.Evidence!.EventRecords.Single(), trusted.Evidence!.EventRecords.Single());
            Assert.Equal(direct.Evidence.SnapshotCheckpoint, trusted.Evidence.SnapshotCheckpoint);
            Assert.Equal(direct.Evidence.Receipt, trusted.Evidence.Receipt);
            Assert.True(CampaignExercises.Reconstruct(trusted.SuccessorSession!).IsVerified);
            Assert.False(CampaignCombatAudienceDecisions.Submit(trusted.SuccessorSession!,
                decision.Choose(ordinal)).IsAccepted);
            Assert.Null(CampaignCombatAudienceDecisions.QueryCustody(trusted.SuccessorSession!,
                CampaignActionAudience.Axis, Captor));
        }
    }

    [Fact]
    public void EveryCustodyRejectionCauseSharesOneOutwardEnvelope()
    {
        var session = AtCustody("campaign-disclosure-custody-reject", 5, out _);
        var valid = CampaignCombatAudienceDecisions.QueryCustody(session, CampaignActionAudience.Axis, Captor)!
            .Choose(0);
        var victim = CampaignExercises.QueryNativeCombatCustodyCut(session)!.VictimElementId;
        CampaignCombatAudienceSubmission[] invalid =
        [
            valid with { OfferRef = "cmbref.v1." + new string('0', 64) },
            valid with { Audience = CampaignActionAudience.Commonwealth },
            valid with { Audience = CampaignActionAudience.System },
            valid with { ActingElementId = victim },
            valid with { ActingElementId = "axis-infantry-second" },
            valid with { AudienceRevision = 1 },
            valid with { VisibleDigest = "sha256:" + new string('1', 64) },
            valid with { Stage = CampaignCombatAudienceDecisions.ReserveReleaseStage },
            valid with { Stage = CampaignCombatAudienceDecisions.SelectionStage },
            valid with { CampaignId = "another-campaign" },
            valid with { ContractVersion = 2 },
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
    public void CustodyDecisionsEncodeNoSettlementPrivateFacts()
    {
        // Seeds 5 and 20 each capture with Axis as captor; their private streams and settlement
        // hashes differ. A non-leak check (not an equal-visible-history proof): when the allowlisted
        // fields match, decision bytes and refs match and a ref opened on one fork applies to the other.
        var left = AtCustody("campaign-disclosure-custody-fork", 5, out _);
        var right = AtCustody("campaign-disclosure-custody-fork", 20, out _);
        var leftCut = CampaignExercises.QueryNativeCombatCustodyCut(left)!;
        var rightCut = CampaignExercises.QueryNativeCombatCustodyCut(right)!;
        Assert.NotEqual(left.CurrentSnapshot.RandomState, right.CurrentSnapshot.RandomState);
        var opened = CampaignCombatAudienceDecisions.QueryCustody(left, CampaignActionAudience.Axis, Captor)!;
        var other = CampaignCombatAudienceDecisions.QueryCustody(right, CampaignActionAudience.Axis, Captor)!;
        Assert.Equal(leftCut.Quantity, rightCut.Quantity);
        Assert.Equal(opened.SerializeCanonical(), other.SerializeCanonical());
        Assert.True(CampaignCombatAudienceDecisions.Submit(right, opened.Choose(0)).IsAccepted);
    }

    // Same trusted path as CombatCycleDisclosureReserveCycleTests, stopped after World settlement.
    private static ExerciseSession AtCustody(string campaignId, ulong seed, out bool refusedRetreat)
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
                .FirstOrDefault(value => value.ElementId == "axis-infantry-second") : null;
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
        refusedRetreat = session.CurrentCombatAssaultResult!.Facts.RequiredRetreat == 1;
        if (refusedRetreat)
        {
            var cut = CampaignExercises.QueryContactPostResultRetreatCut(session);
            session = Accept(CampaignExercises.SubmitContactPostResultRetreat(session,
                Command(session, CampaignActionAudience.Commonwealth, cut.RefuseOffer.ActionId), cut.DefenderElementId));
        }
        else session = Accept(CampaignHostAdapter.CompleteNoPostAssaultRetreat(session));
        return Accept(CampaignHostAdapter.SettleNativeCombatWorld(session));
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
