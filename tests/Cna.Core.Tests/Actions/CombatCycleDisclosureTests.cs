using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Campaigns;
using Cna.Core.Tests.Content;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Actions;

/// <summary>
/// Task 020A0 (dormant): audience-safe Combat Selection decision and submission. Equal owner-visible
/// histories must be indistinguishable in decision bytes, refs, revision and rejection envelopes;
/// adjudication is the unchanged private path.
/// </summary>
public sealed class CombatCycleDisclosureTests
{
    private const string Actor = "axis-infantry";

    [Fact]
    public void SelectionDecisionMatchesHostDraftAndCarriesNoPrivateFields()
    {
        var session = AdvanceToCombat("campaign-disclosure-selection", 12345);
        var decision = CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.Axis, Actor);
        Assert.NotNull(decision);
        var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(session,
            CampaignActionAudience.Axis, Actor);

        Assert.Equal(draft.VisibleBytes, decision.VisibleBytes);
        Assert.Equal(draft.PrivateCombatActionIds.Count, decision.OfferRefs.Count);
        Assert.Equal(2, decision.OfferRefs.Count); // one adjacent contact assault plus finish
        Assert.Equal(CampaignCombatAudienceDecisions.SelectionStage, decision.Stage);
        Assert.Equal(CampaignExercises.QueryContactOwnerCombatCut(session, LandSide.Axis, Actor)
            .OwnerCut.ContactObservation.AudienceRevision, decision.AudienceRevision);
        Assert.Equal(decision.OfferRefs.Count, decision.OfferRefs.Distinct(StringComparer.Ordinal).Count());

        var outward = Encoding.UTF8.GetString(decision.SerializeCanonical());
        foreach (var privateId in draft.PrivateCombatActionIds)
        {
            Assert.DoesNotContain(privateId, outward, StringComparison.Ordinal);
            Assert.DoesNotContain(privateId, decision.OfferRefs);
        }
        Assert.DoesNotContain("boundRepresentationId", outward, StringComparison.Ordinal);
        Assert.DoesNotContain("authorityVersion", outward, StringComparison.Ordinal);
        Assert.DoesNotContain("stateVersion", outward, StringComparison.Ordinal);
        Assert.DoesNotContain("actionId", outward, StringComparison.Ordinal);
        Assert.All(decision.OfferRefs, value => Assert.StartsWith("cmbref.v1.", value, StringComparison.Ordinal));

        // Deterministic: the same cut yields the same outward bytes.
        Assert.Equal(decision.SerializeCanonical(), CampaignCombatAudienceDecisions.QuerySelection(
            session, CampaignActionAudience.Axis, Actor)!.SerializeCanonical());
    }

    [Fact]
    public void AcceptedAudienceSelectionUsesTheUnchangedPrivateAdjudication()
    {
        var session = AdvanceToCombat("campaign-disclosure-accept", 12345);
        var decision = CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.Axis, Actor)!;
        var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(session,
            CampaignActionAudience.Axis, Actor);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);

        for (var ordinal = 0; ordinal < decision.OfferRefs.Count; ordinal++)
        {
            var audience = CampaignCombatAudienceDecisions.SubmitSelection(session, decision.Choose(ordinal));
            Assert.True(audience.IsAccepted);
            Assert.Equal(decision.OfferRefs[ordinal], audience.AcceptedRef);
            var trusted = audience.TrustedStep!;
            var direct = CampaignHostAdapter.SubmitContactOwnerCombatSelection(session,
                new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
                    checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId,
                    CampaignActionAudience.Axis, draft.PrivateCombatActionIds[ordinal]), Actor);
            Assert.True(direct.IsAccepted, direct.RejectionReason.ToString());
            Assert.Equal(direct.Evidence!.EventRecords.Single(), trusted.Evidence!.EventRecords.Single());
            Assert.Equal(direct.Evidence.SnapshotCheckpoint, trusted.Evidence.SnapshotCheckpoint);
            Assert.Equal(direct.Evidence.Receipt, trusted.Evidence.Receipt);
            Assert.True(CampaignExercises.Reconstruct(trusted.SuccessorSession!).IsVerified);

            // The accepted cut is closed: a retry of the same ref cannot advance again.
            Assert.False(CampaignCombatAudienceDecisions.SubmitSelection(trusted.SuccessorSession!,
                decision.Choose(ordinal)).IsAccepted);
            Assert.Null(CampaignCombatAudienceDecisions.QuerySelection(trusted.SuccessorSession!,
                CampaignActionAudience.Axis, Actor));
        }
    }

    [Fact]
    public void EveryRejectionCauseSharesOneOutwardEnvelope()
    {
        var session = AdvanceToCombat("campaign-disclosure-reject", 12345);
        var decision = CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.Axis, Actor)!;
        var valid = decision.Choose(0);
        var accepted = CampaignCombatAudienceDecisions.SubmitSelection(session, valid);
        Assert.True(accepted.IsAccepted);

        CampaignCombatAudienceSubmission[] invalid =
        [
            valid with { OfferRef = "cmbref.v1." + new string('0', 64) },
            valid with { OfferRef = CampaignHostAdapter.QueryContactCombatOwnerDraft(session,
                CampaignActionAudience.Axis, Actor).PrivateCombatActionIds[0] },
            valid with { Audience = CampaignActionAudience.Commonwealth },
            valid with { Audience = CampaignActionAudience.System },
            valid with { ActingElementId = "axis-truck" },
            valid with { ActingElementId = "commonwealth-infantry" },
            valid with { AudienceRevision = valid.AudienceRevision + 1 },
            valid with { VisibleDigest = "sha256:" + new string('1', 64) },
            valid with { Stage = "retreat-before-assault" },
            valid with { CampaignId = "another-campaign" },
            valid with { ContractVersion = 2 },
            valid with { OfferRef = "" },
        ];
        var rejection = CampaignCombatAudienceSubmissionResult.Rejected.SerializeOutwardCanonical();
        foreach (var submission in invalid)
        {
            var result = CampaignCombatAudienceDecisions.SubmitSelection(session, submission);
            Assert.False(result.IsAccepted);
            Assert.Null(result.TrustedStep);
            Assert.Equal(rejection, result.SerializeOutwardCanonical());
        }
        var replay = CampaignCombatAudienceDecisions.SubmitSelection(
            accepted.TrustedStep!.SuccessorSession!, valid);
        Assert.Equal(rejection, replay.SerializeOutwardCanonical());
        Assert.Equal("{\"contractVersion\":1,\"outcome\":\"rejected\"}", Encoding.UTF8.GetString(rejection));
    }

    [Fact]
    public void OpponentSeatReceivesNoDecisionAtTheActingSidesCut()
    {
        var session = AdvanceToCombat("campaign-disclosure-opponent", 12345);
        Assert.Null(CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.Commonwealth, "commonwealth-infantry"));
        Assert.Null(CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.Commonwealth, Actor));
        Assert.Null(CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.System, Actor));
        Assert.Null(CampaignCombatAudienceDecisions.QuerySelection(session,
            CampaignActionAudience.Axis, "unknown-element"));
    }

    [Fact]
    public void EqualVisibleHiddenForksYieldIdenticalAudienceDecisions()
    {
        var source = BreakdownSnapshotTests.Create(true);
        var visibleContact = BreakdownMovementTests.ChangeElement(source, "commonwealth-infantry", "north");
        (string Name, CampaignSnapshotV11 Left, CampaignSnapshotV11 Right, int Offers)[] forks =
        [
            ("unseen enemy position",
                BreakdownMovementTests.ChangeElement(source, "commonwealth-infantry", "east"),
                BreakdownMovementTests.ChangeElement(source, "commonwealth-infantry", "south-east"), 1),
            ("hidden enemy capability expenditure",
                visibleContact,
                BreakdownMovementTests.ChangeElement(source, "commonwealth-infantry", "north", expended: 4), 2),
            ("private random stream",
                visibleContact,
                WithRandom(visibleContact, 98765), 2),
            ("hidden authority version advance",
                visibleContact,
                WithStateVersion(visibleContact, visibleContact.StateVersion + 3), 2),
        ];
        foreach (var (name, left, right, offers) in forks)
        {
            var first = SyntheticSession(left);
            var second = SyntheticSession(right);
            var leftDecision = CampaignCombatAudienceDecisions.QuerySelection(first,
                CampaignActionAudience.Axis, Actor);
            var rightDecision = CampaignCombatAudienceDecisions.QuerySelection(second,
                CampaignActionAudience.Axis, Actor);
            Assert.True(leftDecision is not null, name);
            Assert.True(rightDecision is not null, name);
            Assert.Equal(offers, leftDecision.OfferRefs.Count);
            Assert.Equal(leftDecision.SerializeCanonical(), rightDecision.SerializeCanonical());
            Assert.Equal(leftDecision.AudienceRevision, rightDecision.AudienceRevision);
            Assert.Equal(leftDecision.OfferRefs, rightDecision.OfferRefs);
            // The opponent seat is rejected identically on both forks.
            Assert.Null(CampaignCombatAudienceDecisions.QuerySelection(first,
                CampaignActionAudience.Commonwealth, "commonwealth-infantry"));
            Assert.Null(CampaignCombatAudienceDecisions.QuerySelection(second,
                CampaignActionAudience.Commonwealth, "commonwealth-infantry"));
        }
    }

    [Fact]
    public void VisibleDifferenceChangesTheDecisionAndStalesTheOpenedRef()
    {
        // Sensitivity control for the equal-visible forks: an actually observed contact must change
        // the decision bytes, revision-bound digest and refs.
        var source = BreakdownSnapshotTests.Create(true);
        var unseen = SyntheticSession(BreakdownMovementTests.ChangeElement(source,
            "commonwealth-infantry", "east"));
        var seen = SyntheticSession(BreakdownMovementTests.ChangeElement(source,
            "commonwealth-infantry", "north"));
        var withoutContact = CampaignCombatAudienceDecisions.QuerySelection(unseen,
            CampaignActionAudience.Axis, Actor)!;
        var withContact = CampaignCombatAudienceDecisions.QuerySelection(seen,
            CampaignActionAudience.Axis, Actor)!;
        Assert.NotEqual(withoutContact.SerializeCanonical(), withContact.SerializeCanonical());
        Assert.NotEqual(withoutContact.VisibleDigest, withContact.VisibleDigest);
        Assert.Empty(withoutContact.OfferRefs.Intersect(withContact.OfferRefs));
    }

    [Fact]
    public void RefOpenedOnOneHiddenForkSelectsTheSameOwnerChoiceOnTheOther()
    {
        // Same campaign and visible history; only the private random stream differs.
        var left = AdvanceToCombat("campaign-disclosure-fork", 12345);
        var right = AdvanceToCombat("campaign-disclosure-fork", 54321);
        Assert.NotEqual(left.CurrentSnapshot.RandomState, right.CurrentSnapshot.RandomState);
        var opened = CampaignCombatAudienceDecisions.QuerySelection(left,
            CampaignActionAudience.Axis, Actor)!;
        Assert.Equal(opened.SerializeCanonical(), CampaignCombatAudienceDecisions.QuerySelection(right,
            CampaignActionAudience.Axis, Actor)!.SerializeCanonical());
        for (var ordinal = 0; ordinal < opened.OfferRefs.Count; ordinal++)
        {
            var onLeft = CampaignCombatAudienceDecisions.SubmitSelection(left, opened.Choose(ordinal));
            var onRight = CampaignCombatAudienceDecisions.SubmitSelection(right, opened.Choose(ordinal));
            Assert.True(onLeft.IsAccepted);
            Assert.True(onRight.IsAccepted);
            Assert.Equal(onLeft.SerializeOutwardCanonical(), onRight.SerializeOutwardCanonical());
            var leftChoice = onLeft.TrustedStep!.SuccessorSession!.CurrentCombatDecision!.Decision;
            var rightChoice = onRight.TrustedStep!.SuccessorSession!.CurrentCombatDecision!.Decision;
            Assert.Equal(leftChoice.Choice, rightChoice.Choice);
            Assert.Equal(leftChoice.ContactHandle, rightChoice.ContactHandle);
            Assert.Equal(leftChoice.ActionId, rightChoice.ActionId);
            Assert.True(CampaignExercises.Reconstruct(onRight.TrustedStep.SuccessorSession!).IsVerified);
        }
    }

    private static ExerciseSession AdvanceToCombat(string campaignId, ulong seed)
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(
            Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
                value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
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
                ? actionSet.Candidates.OfType<MoveElementAction>().FirstOrDefault(value =>
                    value.ElementId == Actor && value.DestinationLocationId == "north")
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

    private static ExerciseSession SyntheticSession(CampaignSnapshotV11 prior)
    {
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        var atCombat = CombatCutFrom(prior, artifact, scenario);
        return new ExerciseSession(atCombat, context, [],
            CampaignContactJournal.Initialize(atCombat, context));
    }

    private static CampaignSnapshotV11 WithRandom(CampaignSnapshotV11 prior, ulong seed) =>
        new(11, prior.CampaignId, prior.StateVersion, prior.RulesetHash, prior.Setup, prior.World,
            prior.InitiativeHolder, prior.OperationStageOrders, prior.OperationStageWeather,
            new RandomStreamState(prior.RandomState.ContractVersion, prior.RandomState.AlgorithmId,
                seed, prior.RandomState.NextByteCursor),
            prior.CurrentPosition, prior.ReactionWindow, prior.BreakdownFlow);

    private static CampaignSnapshotV11 WithStateVersion(CampaignSnapshotV11 prior, long stateVersion) =>
        new(11, prior.CampaignId, stateVersion, prior.RulesetHash, prior.Setup, prior.World,
            prior.InitiativeHolder, prior.OperationStageOrders, prior.OperationStageWeather,
            prior.RandomState, prior.CurrentPosition, prior.ReactionWindow, prior.BreakdownFlow);

    // Same synthetic Combat-entry construction as CampaignContactCombatSelectionCutTests.
    private static CampaignSnapshotV11 CombatCutFrom(CampaignSnapshotV11 prior,
        ContentPackV6Artifact artifact, ContentScenario scenario)
    {
        var movement = CampaignBreakdownLifecycleFactory.CreateMovementCompletion(
            prior, artifact, scenario);
        var atBreakdown = new CampaignSnapshotV11(11, prior.CampaignId,
            movement.StateVersion, prior.RulesetHash, prior.Setup, prior.World,
            prior.InitiativeHolder, prior.OperationStageOrders,
            prior.OperationStageWeather, prior.RandomState,
            CampaignPositionV11.FromSequence(movement.SequencePosition),
            null, new CampaignBreakdownFlow.Idle());
        var completion = CampaignBreakdownLifecycleFactory.CreateBreakdownCompletion(
            atBreakdown, artifact, scenario,
            CampaignBreakdownLifecycleFactory.CreateBreakdownCompletionActionId());
        return new CampaignSnapshotV11(11, prior.CampaignId,
            completion.StateVersion, prior.RulesetHash, prior.Setup, prior.World,
            prior.InitiativeHolder, prior.OperationStageOrders,
            prior.OperationStageWeather, prior.RandomState,
            CampaignPositionV11.FromSequence(completion.SequencePosition),
            null, new CampaignBreakdownFlow.Idle());
    }
}
