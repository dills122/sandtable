using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.Core.Tests.Actions;

/// <summary>
/// Task 020E1: audience-safe post-assault Retreat decision (defender-owned), the first stage of the
/// combat-audience-decision v2 superset. Its envelope carries <c>contractVersion:2</c>; the visible
/// cut is the existing <c>CampaignContactRetreatAfterAssaultOwnerVisibleCutV1</c> the Host already
/// serves, and adjudication is the unchanged private V23 path.
/// </summary>
public sealed class CombatCycleDisclosurePostAssaultRetreatTests
{
    private const string Defender = "commonwealth-infantry";

    [Fact]
    public void RetreatDecisionMatchesHostDraftAndCarriesNoPrivateFields()
    {
        var session = AtRetreat("campaign-disclosure-retreat", 6);
        var decision = CampaignCombatAudienceDecisions.QueryPostAssaultRetreat(session,
            CampaignActionAudience.Commonwealth, Defender);
        Assert.NotNull(decision);
        var draft = CampaignHostAdapter.QueryContactPostAssaultRetreatOwnerDraft(session,
            CampaignActionAudience.Commonwealth, Defender);
        var cut = CampaignExercises.QueryContactPostResultRetreatCut(session);

        Assert.Equal(CampaignCombatAudienceDecisions.PostAssaultRetreatStage, decision.Stage);
        Assert.Equal(CampaignCombatAudienceDecision.SuccessorContractVersion, decision.ContractVersion);
        Assert.StartsWith("{\"contractVersion\":2,", Encoding.UTF8.GetString(decision.SerializeCanonical()),
            StringComparison.Ordinal);
        Assert.Equal(draft.VisibleBytes, decision.VisibleBytes);
        Assert.Equal(draft.PrivateActionIds.Count, decision.OfferRefs.Count);
        Assert.Equal(cut.MoveOffers.Count + 1, decision.OfferRefs.Count); // moves plus explicit refusal
        Assert.Equal(cut.ContactObservation.AudienceRevision, decision.AudienceRevision);
        Assert.True(decision.AudienceRevision >= 1);
        Assert.Equal(decision.OfferRefs.Count, decision.OfferRefs.Distinct(StringComparer.Ordinal).Count());
        using (var visible = JsonDocument.Parse(decision.VisibleBytes))
            Assert.Equal("commonwealth", visible.RootElement.GetProperty("side").GetString());

        var outward = Encoding.UTF8.GetString(decision.SerializeCanonical());
        foreach (var privateId in draft.PrivateActionIds)
        {
            Assert.DoesNotContain(privateId, outward, StringComparison.Ordinal);
            Assert.DoesNotContain(privateId, decision.OfferRefs);
        }
        foreach (var key in new[] { "authorityVersion", "stateVersion", "actionId", "attackerElementId",
                     "boundRepresentationId", "resultHash", "worldHash" })
            Assert.DoesNotContain("\"" + key + "\"", outward, StringComparison.Ordinal);

        // Only the bound defender's own seat receives it.
        Assert.Null(CampaignCombatAudienceDecisions.QueryPostAssaultRetreat(session,
            CampaignActionAudience.Axis, Defender));
        Assert.Null(CampaignCombatAudienceDecisions.QueryPostAssaultRetreat(session,
            CampaignActionAudience.Axis, "axis-infantry"));
        Assert.Null(CampaignCombatAudienceDecisions.QueryPostAssaultRetreat(session,
            CampaignActionAudience.Commonwealth, "commonwealth-infantry-west"));
        Assert.Null(CampaignCombatAudienceDecisions.QueryPostAssaultRetreat(session,
            CampaignActionAudience.System, Defender));
        // v1 stays closed: the frozen v1 verifier does not cover this stage.
        Assert.Contains("stage-uncovered", CampaignCombatAudienceDraftVerifier.VerifyDecision(
            decision.SerializeCanonical()));
    }

    [Fact]
    public void AcceptedRetreatChoicesUseTheUnchangedPrivateAdjudication()
    {
        var session = AtRetreat("campaign-disclosure-retreat-accept", 6);
        var decision = CampaignCombatAudienceDecisions.QueryPostAssaultRetreat(session,
            CampaignActionAudience.Commonwealth, Defender)!;
        var draft = CampaignHostAdapter.QueryContactPostAssaultRetreatOwnerDraft(session,
            CampaignActionAudience.Commonwealth, Defender);
        var rejection = CampaignCombatAudienceSubmissionResult.Rejected.SerializeOutwardCanonical();
        for (var ordinal = 0; ordinal < decision.OfferRefs.Count; ordinal++)
        {
            var viaAudience = CampaignCombatAudienceDecisions.Submit(session, decision.Choose(ordinal));
            Assert.True(viaAudience.IsAccepted);
            Assert.Equal(decision.OfferRefs[ordinal], viaAudience.AcceptedRef);
            Assert.Equal($"{{\"contractVersion\":1,\"outcome\":\"accepted\",\"offerRef\":\"{decision.OfferRefs[ordinal]}\"}}",
                Encoding.UTF8.GetString(viaAudience.SerializeOutwardCanonical()));
            var direct = CampaignHostAdapter.SubmitContactPostAssaultRetreat(session,
                Command(session, CampaignActionAudience.Commonwealth, draft.PrivateActionIds[ordinal]), Defender);
            var trusted = viaAudience.TrustedStep!;
            Assert.True(direct.IsAccepted, direct.RejectionReason.ToString());
            Assert.Equal(direct.Evidence!.EventRecords, trusted.Evidence!.EventRecords);
            Assert.Equal(direct.Evidence.SnapshotCheckpoint, trusted.Evidence.SnapshotCheckpoint);
            Assert.Equal(direct.Evidence.Receipt, trusted.Evidence.Receipt);
            var retreat = Assert.IsType<CampaignNativeCombatRetreatAfterAssaultV23>(
                CampaignCurrentEventSerializer.Deserialize(trusted.Evidence.EventRecords[0]));
            Assert.Equal(ordinal == decision.OfferRefs.Count - 1 ? "refused" : "moved", retreat.Disposition);
            Assert.True(CampaignExercises.Reconstruct(trusted.SuccessorSession!).IsVerified);
            Assert.Null(CampaignCombatAudienceDecisions.QueryPostAssaultRetreat(trusted.SuccessorSession!,
                CampaignActionAudience.Commonwealth, Defender));
            Assert.Equal(rejection, CampaignCombatAudienceDecisions.Submit(trusted.SuccessorSession!,
                decision.Choose(ordinal)).SerializeOutwardCanonical());
        }
    }

    [Fact]
    public void EveryRetreatRejectionCauseSharesOneOutwardEnvelope()
    {
        var session = AtRetreat("campaign-disclosure-retreat-reject", 6);
        var valid = CampaignCombatAudienceDecisions.QueryPostAssaultRetreat(session,
            CampaignActionAudience.Commonwealth, Defender)!.Choose(0);
        Assert.Equal(CampaignCombatAudienceDecision.SuccessorContractVersion, valid.ContractVersion);
        CampaignCombatAudienceSubmission[] invalid =
        [
            valid with { OfferRef = "cmbref.v1." + new string('0', 64) },
            valid with { Audience = CampaignActionAudience.Axis },
            valid with { Audience = CampaignActionAudience.System },
            valid with { ActingElementId = "axis-infantry" },
            valid with { AudienceRevision = valid.AudienceRevision + 1 },
            valid with { AudienceRevision = 0 },
            valid with { VisibleDigest = "sha256:" + new string('1', 64) },
            valid with { Stage = CampaignCombatAudienceDecisions.RbaDeclineStage },
            valid with { Stage = CampaignCombatAudienceDecisions.SelectionStage },
            valid with { CampaignId = "another-campaign" },
            valid with { ContractVersion = CampaignCombatAudienceSubmission.CurrentContractVersion },
            valid with { ContractVersion = 3 },
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
    public void NoRetreatResultOpensNoRetreatDecision()
    {
        // Seed 5 (the custody golden path) requires no retreat; a System step closes it.
        var session = CombatCycleDisclosureReserveCycleTests.AtAssaultResult("campaign-disclosure-no-retreat", 5);
        Assert.Equal(0, session.CurrentCombatAssaultResult!.Facts.RequiredRetreat);
        foreach (var audience in Enum.GetValues<CampaignActionAudience>())
            foreach (var element in new[] { Defender, "axis-infantry", "commonwealth-infantry-west" })
                Assert.Null(CampaignCombatAudienceDecisions.QueryPostAssaultRetreat(session, audience, element));
    }

    [Fact]
    public void RetreatDecisionEncodesNoOpponentPrivateFacts()
    {
        // Two seeds that both require a retreat have different private random streams and dice. The
        // defender's visible cut names only its own contacts, own unit and route semantics, so the
        // decision bytes and refs match, and a ref opened on one fork is accepted on the other.
        var left = AtRetreat("campaign-disclosure-retreat-fork", 6);
        var rightSeed = Enumerable.Range(7, 60).Select(value => (ulong)value).First(seed =>
            CombatCycleDisclosureReserveCycleTests.AtAssaultResult("campaign-disclosure-retreat-fork", seed)
                .CurrentCombatAssaultResult!.Facts.RequiredRetreat == 1);
        var right = AtRetreat("campaign-disclosure-retreat-fork", rightSeed);
        Assert.NotEqual(left.CurrentCombatAssaultResult!.AfterRandomState, right.CurrentCombatAssaultResult!.AfterRandomState);
        var opened = CampaignCombatAudienceDecisions.QueryPostAssaultRetreat(left,
            CampaignActionAudience.Commonwealth, Defender)!;
        var other = CampaignCombatAudienceDecisions.QueryPostAssaultRetreat(right,
            CampaignActionAudience.Commonwealth, Defender)!;
        Assert.Equal(opened.SerializeCanonical(), other.SerializeCanonical());
        Assert.True(CampaignCombatAudienceDecisions.Submit(right, opened.Choose(opened.OfferRefs.Count - 1)).IsAccepted);
    }

    internal static ExerciseSession AtRetreat(string campaignId, ulong seed)
    {
        var session = CombatCycleDisclosureReserveCycleTests.AtAssaultResult(campaignId, seed);
        Assert.Equal(1, session.CurrentCombatAssaultResult!.Facts.RequiredRetreat);
        return session;
    }

    private static CampaignActionSubmission Command(ExerciseSession session, CampaignActionAudience audience, string action)
    {
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        return new(1, checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience, action);
    }
}
