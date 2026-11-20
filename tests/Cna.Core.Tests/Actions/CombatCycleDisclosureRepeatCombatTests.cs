using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;

namespace Cna.Core.Tests.Actions;

/// <summary>
/// Task 020E2: audience-safe repeated-cycle Combat selection (Core V31), a combat-audience-decision
/// v2 superset stage. Reached from the frozen Cycle Control decision's <c>Repeat</c>, one repeated
/// Movement and the System repeated Breakdown. The visible cut is the existing
/// <c>CampaignRepeatCombatOwnerCutV31</c> the Host already serves; adjudication is unchanged.
/// T020-Q8 applies as for first-cycle Selection (owner, 2026-11-20).
/// </summary>
public sealed class CombatCycleDisclosureRepeatCombatTests
{
    private static readonly string[] AxisElements = ["axis-infantry", "axis-infantry-second", "axis-truck"];

    [Fact]
    public void RepeatSelectionDecisionMatchesHostDraftAndCarriesNoPrivateFields()
    {
        var (session, actor) = AtRepeatCombat("campaign-disclosure-repeat-combat", 6);
        var decision = CampaignCombatAudienceDecisions.QueryRepeatCombatSelection(session,
            CampaignActionAudience.Axis, actor);
        Assert.NotNull(decision);
        var draft = CampaignHostAdapter.QueryNativeRepeatCombatOwnerDraft(session, CampaignActionAudience.Axis, actor);

        Assert.Equal(CampaignCombatAudienceDecisions.RepeatCombatSelectionStage, decision.Stage);
        Assert.Equal(CampaignCombatAudienceDecision.SuccessorContractVersion, decision.ContractVersion);
        Assert.StartsWith("{\"contractVersion\":2,", Encoding.UTF8.GetString(decision.SerializeCanonical()),
            StringComparison.Ordinal);
        Assert.Equal(draft.VisibleBytes, decision.VisibleBytes);
        Assert.Equal(draft.PrivateCombatActionIds.Count, decision.OfferRefs.Count);
        Assert.True(decision.OfferRefs.Count >= 2); // at least one current-contact assault plus finish
        Assert.True(decision.AudienceRevision >= 1);
        using (var visible = JsonDocument.Parse(decision.VisibleBytes))
        {
            Assert.Equal(31, visible.RootElement.GetProperty("contractVersion").GetInt32());
            Assert.Equal("axis", visible.RootElement.GetProperty("side").GetString());
            Assert.Equal(decision.AudienceRevision,
                visible.RootElement.GetProperty("contactAudienceRevision").GetInt64());
            var targets = visible.RootElement.GetProperty("contactObservation").GetProperty("currentTargets")
                .EnumerateArray().Select(value => value.GetProperty("contactHandle").GetString()).ToHashSet();
            Assert.All(visible.RootElement.GetProperty("assaultOffers").EnumerateArray(), offer =>
                Assert.Contains(offer.GetProperty("contactHandle").GetString(), targets));
        }
        var outward = Encoding.UTF8.GetString(decision.SerializeCanonical());
        foreach (var privateId in draft.PrivateCombatActionIds)
        {
            Assert.DoesNotContain(privateId, outward, StringComparison.Ordinal);
            Assert.DoesNotContain(privateId, decision.OfferRefs);
        }
        foreach (var key in new[] { "authorityVersion", "stateVersion", "actionId", "boundRepresentationId" })
            Assert.DoesNotContain("\"" + key + "\"", outward, StringComparison.Ordinal);

        // Only the acting side's owner; the first-cycle v1 Selection stage does not serve it.
        Assert.Null(CampaignCombatAudienceDecisions.QueryRepeatCombatSelection(session,
            CampaignActionAudience.Commonwealth, actor));
        Assert.Null(CampaignCombatAudienceDecisions.QueryRepeatCombatSelection(session,
            CampaignActionAudience.System, actor));
        Assert.Null(CampaignCombatAudienceDecisions.QuerySelection(session, CampaignActionAudience.Axis, actor));
        Assert.Contains("stage-uncovered", CampaignCombatAudienceDraftVerifier.VerifyDecision(
            decision.SerializeCanonical()));
    }

    [Fact]
    public void AcceptedRepeatSelectionUsesTheUnchangedV31Adjudication()
    {
        var (session, actor) = AtRepeatCombat("campaign-disclosure-repeat-combat-accept", 6);
        var decision = CampaignCombatAudienceDecisions.QueryRepeatCombatSelection(session,
            CampaignActionAudience.Axis, actor)!;
        var draft = CampaignHostAdapter.QueryNativeRepeatCombatOwnerDraft(session, CampaignActionAudience.Axis, actor);
        var rejection = CampaignCombatAudienceSubmissionResult.Rejected.SerializeOutwardCanonical();
        for (var ordinal = 0; ordinal < decision.OfferRefs.Count; ordinal++)
        {
            var viaAudience = CampaignCombatAudienceDecisions.Submit(session, decision.Choose(ordinal));
            Assert.True(viaAudience.IsAccepted);
            Assert.Equal(decision.OfferRefs[ordinal], viaAudience.AcceptedRef);
            var direct = CampaignHostAdapter.SubmitNativeRepeatCombat(session,
                Command(session, CampaignActionAudience.Axis, draft.PrivateCombatActionIds[ordinal]), actor);
            var trusted = viaAudience.TrustedStep!;
            Assert.True(direct.IsAccepted, direct.RejectionReason.ToString());
            Assert.Equal(direct.Evidence!.EventRecords, trusted.Evidence!.EventRecords);
            Assert.Equal(direct.Evidence.SnapshotCheckpoint, trusted.Evidence.SnapshotCheckpoint);
            Assert.Equal(direct.Evidence.Receipt, trusted.Evidence.Receipt);
            Assert.IsType<CampaignNativeRepeatCombatDecisionV31>(
                CampaignCurrentEventSerializer.Deserialize(trusted.Evidence.EventRecords[0]));
            Assert.True(CampaignExercises.Reconstruct(trusted.SuccessorSession!).IsVerified);
            Assert.Equal(rejection, CampaignCombatAudienceDecisions.Submit(trusted.SuccessorSession!,
                decision.Choose(ordinal)).SerializeOutwardCanonical());
        }
    }

    [Fact]
    public void EveryRepeatSelectionRejectionCauseSharesOneOutwardEnvelope()
    {
        var (session, actor) = AtRepeatCombat("campaign-disclosure-repeat-combat-reject", 6);
        var valid = CampaignCombatAudienceDecisions.QueryRepeatCombatSelection(session,
            CampaignActionAudience.Axis, actor)!.Choose(0);
        Assert.Equal(CampaignCombatAudienceDecision.SuccessorContractVersion, valid.ContractVersion);
        CampaignCombatAudienceSubmission[] invalid =
        [
            valid with { OfferRef = "cmbref.v1." + new string('0', 64) },
            valid with { Audience = CampaignActionAudience.Commonwealth },
            valid with { Audience = CampaignActionAudience.System },
            valid with { ActingElementId = "commonwealth-infantry" },
            valid with { AudienceRevision = valid.AudienceRevision + 1 },
            valid with { VisibleDigest = "sha256:" + new string('1', 64) },
            valid with { Stage = CampaignCombatAudienceDecisions.SelectionStage },
            valid with { Stage = CampaignCombatAudienceDecisions.PostAssaultRetreatStage },
            valid with { CampaignId = "another-campaign" },
            valid with { ContractVersion = CampaignCombatAudienceSubmission.CurrentContractVersion },
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
    public void RepeatSelectionDecisionEncodesNoOpponentPrivateFacts()
    {
        // Seeds 6 and 7 differ in private random streams; when the owner's own cut matches, the
        // decision bytes and refs match and a ref opened on one fork is accepted on the other.
        var (left, actor) = AtRepeatCombat("campaign-disclosure-repeat-combat-fork", 6);
        var (right, rightActor) = AtRepeatCombat("campaign-disclosure-repeat-combat-fork", 7);
        Assert.Equal(actor, rightActor);
        Assert.NotEqual(left.CurrentSnapshot.RandomState, right.CurrentSnapshot.RandomState);
        var opened = CampaignCombatAudienceDecisions.QueryRepeatCombatSelection(left, CampaignActionAudience.Axis, actor)!;
        var other = CampaignCombatAudienceDecisions.QueryRepeatCombatSelection(right, CampaignActionAudience.Axis, actor)!;
        Assert.Equal(opened.SerializeCanonical(), other.SerializeCanonical());
        Assert.True(CampaignCombatAudienceDecisions.Submit(right, opened.Choose(0)).IsAccepted);
    }

    // Frozen Reserve Release golden path, then Cycle Control Repeat (accepted through the audience
    // surface), the first offered repeated Movement and the System repeated Breakdown.
    internal static ExerciseSession AtRepeatMovement(string campaignId, ulong seed)
    {
        var session = CombatCycleDisclosureReserveCycleTests.AtReserveRelease(campaignId, seed);
        var release = CampaignCombatAudienceDecisions.QueryReserveRelease(session, CampaignActionAudience.Axis,
            "axis-infantry-second")!;
        session = Accepted(CampaignCombatAudienceDecisions.Submit(session, release.Choose(0)));
        session = CombatCycleDisclosureReserveCycleTests.Complete(session);
        var control = CampaignCombatAudienceDecisions.QueryCycleControl(session, CampaignActionAudience.Axis)!;
        string[] choices;
        using (var visible = JsonDocument.Parse(control.VisibleBytes))
            choices = visible.RootElement.GetProperty("choices").EnumerateArray()
                .Select(value => value.GetString()!).ToArray();
        return Accepted(CampaignCombatAudienceDecisions.Submit(session, control.Choose(Array.IndexOf(choices, "Repeat"))));
    }

    internal static (ExerciseSession Session, string Actor) AtRepeatCombat(string campaignId, ulong seed)
    {
        var session = AtRepeatMovement(campaignId, seed);
        var moves = CampaignHostAdapter.QueryNativeRepeatMovementOwnerDraft(session, CampaignActionAudience.Axis);
        session = Accept(CampaignHostAdapter.SubmitNativeRepeatMovement(session,
            Command(session, CampaignActionAudience.Axis, moves.PrivateActionIds[0])));
        session = Accept(CampaignHostAdapter.CompleteNativeRepeatBreakdown(session));
        var actor = AxisElements.First(element =>
        {
            try
            {
                return CampaignHostAdapter.QueryNativeRepeatCombatOwnerDraft(session, CampaignActionAudience.Axis,
                    element).PrivateCombatActionIds.Count > 1;
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                return false;
            }
        });
        return (session, actor);
    }

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
