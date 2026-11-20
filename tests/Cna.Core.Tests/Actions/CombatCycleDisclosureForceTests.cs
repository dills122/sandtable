using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Actions;

/// <summary>
/// Task 020A1 (dormant): audience-safe Force Assignment seals. Each owner's decision is
/// independent of the other owner's seal in either order; adjudication is the unchanged private path.
/// </summary>
public sealed class CombatCycleDisclosureForceTests
{
    private const string Attacker = "axis-infantry";
    private const string Defender = "commonwealth-infantry";

    [Fact]
    public void ForceDecisionsMatchHostDraftsAndCarryNoPrivateFields()
    {
        var session = AdvanceToForce("campaign-disclosure-force", 12345);
        foreach (var (audience, element) in Owners)
        {
            var decision = CampaignCombatAudienceDecisions.QueryForceAssignment(session, audience, element);
            Assert.NotNull(decision);
            var draft = CampaignHostAdapter.QueryContactForceOwnerDraft(session, audience, element);
            Assert.Equal(draft.VisibleBytes, decision.VisibleBytes);
            Assert.Single(decision.OfferRefs);
            Assert.Equal(CampaignCombatAudienceDecisions.ForceAssignmentStage, decision.Stage);
            var outward = Encoding.UTF8.GetString(decision.SerializeCanonical());
            Assert.DoesNotContain(draft.PrivateActionIds[0], outward, StringComparison.Ordinal);
            Assert.DoesNotContain("authorityVersion", outward, StringComparison.Ordinal);
            Assert.DoesNotContain("stateVersion", outward, StringComparison.Ordinal);
            Assert.DoesNotContain("actionId", outward, StringComparison.Ordinal);
            Assert.DoesNotContain("boundRepresentationId", outward, StringComparison.Ordinal);
        }
        // Each owner sees only its own slot; the other owner's element or audience yields nothing.
        Assert.Null(CampaignCombatAudienceDecisions.QueryForceAssignment(session,
            CampaignActionAudience.Axis, Defender));
        Assert.Null(CampaignCombatAudienceDecisions.QueryForceAssignment(session,
            CampaignActionAudience.Commonwealth, Attacker));
        Assert.Null(CampaignCombatAudienceDecisions.QueryForceAssignment(session,
            CampaignActionAudience.System, Attacker));
        // A Selection-stage query at the Force cut is unavailable, not a different error.
        Assert.Null(CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.Axis, Attacker));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FirstSealLeavesTheOtherOwnersDecisionAndRefUnchanged(bool attackerFirst)
    {
        var session = AdvanceToForce("campaign-disclosure-force-order", 12345);
        var (firstAudience, firstElement) = attackerFirst ? Owners[0] : Owners[1];
        var (secondAudience, secondElement) = attackerFirst ? Owners[1] : Owners[0];
        var firstOpened = CampaignCombatAudienceDecisions.QueryForceAssignment(session,
            firstAudience, firstElement)!;
        var secondOpened = CampaignCombatAudienceDecisions.QueryForceAssignment(session,
            secondAudience, secondElement)!;

        var firstSeal = CampaignCombatAudienceDecisions.SubmitForceAssignment(session, firstOpened.Choose(0));
        Assert.True(firstSeal.IsAccepted);
        var afterFirst = firstSeal.TrustedStep!.SuccessorSession!;
        Assert.NotEqual(CampaignExercises.QueryCheckpoint(session).StateVersion,
            CampaignExercises.QueryCheckpoint(afterFirst).StateVersion);

        // Hidden progress: the other owner's decision, revision and ref are byte-identical.
        var secondAfter = CampaignCombatAudienceDecisions.QueryForceAssignment(afterFirst,
            secondAudience, secondElement);
        Assert.NotNull(secondAfter);
        Assert.Equal(secondOpened.SerializeCanonical(), secondAfter.SerializeCanonical());
        // The sealed owner's slot is closed; its retry is the generic rejection.
        Assert.Null(CampaignCombatAudienceDecisions.QueryForceAssignment(afterFirst,
            firstAudience, firstElement));
        Assert.Equal(CampaignCombatAudienceSubmissionResult.Rejected.SerializeOutwardCanonical(),
            CampaignCombatAudienceDecisions.SubmitForceAssignment(afterFirst, firstOpened.Choose(0))
                .SerializeOutwardCanonical());

        // The ref opened before the first seal is still accepted after it.
        var secondSeal = CampaignCombatAudienceDecisions.SubmitForceAssignment(afterFirst,
            secondOpened.Choose(0));
        Assert.True(secondSeal.IsAccepted);
        var sealedSession = secondSeal.TrustedStep!.SuccessorSession!;
        Assert.Equal(2, sealedSession.CurrentCombatForceSeals.Count);
        Assert.True(CampaignExercises.Reconstruct(sealedSession).IsVerified);
        Assert.True(CampaignHostAdapter.CompleteNativeCombatForceAssignment(sealedSession).IsAccepted);
    }

    [Fact]
    public void AcceptedAudienceSealUsesTheUnchangedPrivateAdjudication()
    {
        var session = AdvanceToForce("campaign-disclosure-force-accept", 12345);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        foreach (var (audience, element) in Owners)
        {
            var decision = CampaignCombatAudienceDecisions.QueryForceAssignment(session, audience, element)!;
            var viaAudience = CampaignCombatAudienceDecisions.SubmitForceAssignment(session, decision.Choose(0));
            Assert.True(viaAudience.IsAccepted);
            var direct = CampaignHostAdapter.SubmitContactOwnerForceSeal(session,
                new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
                    checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, audience,
                    CampaignHostAdapter.QueryContactForceOwnerDraft(session, audience, element)
                        .PrivateActionIds[0]), element);
            Assert.True(direct.IsAccepted, direct.RejectionReason.ToString());
            var trusted = viaAudience.TrustedStep!.Evidence!;
            Assert.Equal(direct.Evidence!.EventRecords.Single(), trusted.EventRecords.Single());
            Assert.Equal(direct.Evidence.SnapshotCheckpoint, trusted.SnapshotCheckpoint);
            Assert.Equal(direct.Evidence.Receipt, trusted.Receipt);
        }
    }

    [Fact]
    public void EveryForceRejectionCauseSharesOneOutwardEnvelope()
    {
        var session = AdvanceToForce("campaign-disclosure-force-reject", 12345);
        var decision = CampaignCombatAudienceDecisions.QueryForceAssignment(session,
            CampaignActionAudience.Axis, Attacker)!;
        var valid = decision.Choose(0);
        var defender = CampaignCombatAudienceDecisions.QueryForceAssignment(session,
            CampaignActionAudience.Commonwealth, Defender)!.Choose(0);
        CampaignCombatAudienceSubmission[] invalid =
        [
            valid with { OfferRef = "cmbref.v1." + new string('0', 64) },
            valid with { OfferRef = defender.OfferRef },
            valid with { Audience = CampaignActionAudience.Commonwealth },
            valid with { ActingElementId = Defender },
            valid with { AudienceRevision = valid.AudienceRevision + 1 },
            valid with { VisibleDigest = defender.VisibleDigest },
            valid with { Stage = CampaignCombatAudienceDecisions.SelectionStage },
            valid with { CampaignId = "another-campaign" },
        ];
        var rejection = CampaignCombatAudienceSubmissionResult.Rejected.SerializeOutwardCanonical();
        foreach (var submission in invalid)
        {
            Assert.Equal(rejection, CampaignCombatAudienceDecisions.SubmitForceAssignment(session,
                submission).SerializeOutwardCanonical());
            Assert.Equal(rejection, CampaignCombatAudienceDecisions.Submit(session,
                submission).SerializeOutwardCanonical());
        }
        // A Force submission is not accepted through the Selection entry point either.
        Assert.Equal(rejection, CampaignCombatAudienceDecisions.SubmitSelection(session, valid)
            .SerializeOutwardCanonical());
        Assert.True(CampaignCombatAudienceDecisions.SubmitForceAssignment(session, valid).IsAccepted);
    }

    [Fact]
    public void ForceDecisionsAreEqualAcrossAHiddenRandomFork()
    {
        var left = AdvanceToForce("campaign-disclosure-force-fork", 12345);
        var right = AdvanceToForce("campaign-disclosure-force-fork", 54321);
        Assert.NotEqual(left.CurrentSnapshot.RandomState, right.CurrentSnapshot.RandomState);
        foreach (var (audience, element) in Owners)
        {
            var opened = CampaignCombatAudienceDecisions.QueryForceAssignment(left, audience, element)!;
            Assert.Equal(opened.SerializeCanonical(), CampaignCombatAudienceDecisions
                .QueryForceAssignment(right, audience, element)!.SerializeCanonical());
            var onRight = CampaignCombatAudienceDecisions.SubmitForceAssignment(right, opened.Choose(0));
            Assert.True(onRight.IsAccepted);
            Assert.Equal(CampaignCombatAudienceDecisions.SubmitForceAssignment(left, opened.Choose(0))
                .SerializeOutwardCanonical(), onRight.SerializeOutwardCanonical());
        }
    }

    private static readonly (CampaignActionAudience Audience, string Element)[] Owners =
    [
        (CampaignActionAudience.Axis, Attacker),
        (CampaignActionAudience.Commonwealth, Defender),
    ];

    // The Truck setup's adjacent contact is bound to a truck with no combat component, which has
    // no Force slot (a documented v2 gap). The 10-TOE infantry assault setup reaches Force.
    internal static ExerciseSession AdvanceToCombat(string campaignId, ulong seed)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(
            Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
                value.SetupId == "rules-lab.breakdown.reaction.combat-assault.v1"));
        var request = new CampaignCreationRequest(1, campaignId,
            Cna1979Ruleset.Manifest.Hash, seed, setup.SetupId, setup.SetupHash,
            setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId);
        var started = CampaignExercises.Begin(request);
        Assert.True(started.IsStarted);
        var session = started.Session!;
        for (var index = 0; index < 80; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.PhaseId == LandPhaseIds.MovementAndCombat
                && position.SegmentId == LandSegmentIds.Combat
                && position.StepId == LandStepIds.PositionDetermination)
                return session;
            var actionSet = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .FirstOrDefault(value => value?.Candidates.Count > 0);
            Assert.NotNull(actionSet);
            var action = position.SegmentId == LandSegmentIds.Movement
                ? actionSet.Candidates.OfType<CompleteMovementSegmentAction>().FirstOrDefault()
                    ?? actionSet.Candidates[0]
                : actionSet.Candidates[0];
            var accepted = CampaignExercises.Submit(session, new CampaignActionSubmission(
                CampaignActionSubmission.CurrentContractVersion, actionSet.CampaignId,
                actionSet.StateVersion, actionSet.PositionId, actionSet.Audience, action.ActionId));
            Assert.True(accepted.IsAccepted, accepted.RejectionReason.ToString());
            session = accepted.SuccessorSession!;
        }
        throw new InvalidOperationException("Combat position was not reached.");
    }

    internal static ExerciseSession AdvanceToForce(string campaignId, ulong seed)
    {
        var session = AdvanceToCombat(campaignId, seed);
        var selection = CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.Axis, Attacker)!;
        // Ordinal 0 is the single adjacent assault offer; the finish offer is last.
        var selected = CampaignCombatAudienceDecisions.SubmitSelection(session, selection.Choose(0));
        Assert.True(selected.IsAccepted);
        session = selected.TrustedStep!.SuccessorSession!;
        Assert.Equal("select-contact-assault", session.CurrentCombatDecision!.Decision.Choice);
        var barrage = CampaignHostAdapter.CompleteNativeCombatBarrage(session);
        Assert.True(barrage.IsAccepted, barrage.RejectionReason.ToString());
        session = barrage.SuccessorSession!;
        // RBA is outside this slice: decline through the existing trusted path.
        var rba = CampaignExercises.QueryContactRbaCut(session);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        var declined = CampaignHostAdapter.SubmitContactDefenderRbaDecline(session,
            new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
                checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
                CampaignActionAudience.Commonwealth, rba.DeclineOffer.ActionId), rba.DefenderElementId);
        Assert.True(declined.IsAccepted, declined.RejectionReason.ToString());
        Assert.Equal(Defender, rba.DefenderElementId);
        return declined.SuccessorSession!;
    }
}
