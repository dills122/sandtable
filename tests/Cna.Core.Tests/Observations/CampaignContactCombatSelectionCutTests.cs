using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Campaigns;
using Cna.Core.Tests.Content;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignContactCombatSelectionCutTests
{
    [Fact]
    public void AcceptedContactSelectionRetainsReceiptAndReplaysFromOrdinaryExercise()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(
            Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
                value.SetupId == Cna1979BreakdownSetupCatalog.TruckSetupId));
        var request = new CampaignCreationRequest(1, "campaign-native-combat",
            Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.SetupHash,
            setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId);
        var started = CampaignExercises.Begin(request);
        Assert.True(started.IsStarted);
        var session = started.Session!;
        var retainedSteps = new List<CampaignHostAcceptedStep>();
        for (var index = 0; index < 80; index++)
        {
            var position = session.CurrentSnapshot.CurrentPosition.SequenceContext;
            if (position.PhaseId == LandPhaseIds.MovementAndCombat
                && position.SegmentId == LandSegmentIds.Combat
                && position.StepId == LandStepIds.PositionDetermination)
                break;
            var actionSet = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet)
                .FirstOrDefault(value => value?.Candidates.Count > 0);
            Assert.NotNull(actionSet);
            var action = position.SegmentId == LandSegmentIds.Movement
                ? actionSet.Candidates.OfType<MoveElementAction>().FirstOrDefault(value =>
                    value.ElementId == "axis-infantry" && value.DestinationLocationId == "north")
                    ?? actionSet.Candidates[0]
                : actionSet.Candidates[0];
            var command = new CampaignActionSubmission(
                CampaignActionSubmission.CurrentContractVersion, actionSet.CampaignId,
                actionSet.StateVersion, actionSet.PositionId, actionSet.Audience,
                action.ActionId);
            var accepted = CampaignExercises.Submit(session, command);
            Assert.True(accepted.IsAccepted, accepted.RejectionReason.ToString());
            retainedSteps.Add(new CampaignHostAcceptedStep(
                CampaignActionSubmissionSerializer.SerializeCanonical(command),
                accepted.Evidence!.EventRecords,
                CampaignActionAcceptanceReceiptSerializer.Serialize(accepted.Evidence.Receipt),
                accepted.Evidence.SnapshotCheckpoint));
            session = accepted.SuccessorSession!;
        }
        Assert.Equal(LandStepIds.PositionDetermination,
            session.CurrentSnapshot.CurrentPosition.SequenceContext.StepId);
        var actor = session.CurrentSnapshot.World.Elements
            .First(value => value.ElementId == "axis-infantry");
        var cut = CampaignExercises.QueryContactOwnerCombatCut(session, LandSide.Axis,
            actor.ElementId);
        Assert.True(cut.MayFinishWithoutAttack);
        Assert.True(cut.AssaultOffers.Count > 0,
            string.Join(",", session.CurrentSnapshot.World.Elements.Select(value =>
                $"{value.ElementId}:{value.CurrentLocationId}"))
            + " targets=" + string.Join(",", cut.OwnerCut.ContactObservation.CurrentTargets.Select(value =>
                value.ApparentLocationId)));
        var offer = Assert.Single(cut.AssaultOffers);
        var submission = new CampaignActionSubmission(
            CampaignActionSubmission.CurrentContractVersion, session.CurrentSnapshot.CampaignId,
            session.CurrentSnapshot.StateVersion,
            CampaignCurrentActionExecution.CurrentPositionId(session.CurrentSnapshot),
            CampaignActionAudience.Axis, offer.ActionId);
        var acceptedCombat = CampaignHostAdapter.SubmitContactOwnerCombatSelection(
            session, submission, actor.ElementId);
        Assert.True(acceptedCombat.IsAccepted, acceptedCombat.RejectionReason.ToString());
        var successor = acceptedCombat.SuccessorSession!;
        Assert.Equal(session.CurrentSnapshot.StateVersion + 1,
            CampaignExercises.QueryCheckpoint(successor).StateVersion);
        Assert.True(CampaignExercises.Reconstruct(successor).IsVerified);
        Assert.Equal(CampaignHostQueryRejectionReason.InvalidState,
            CampaignHostAdapter.QueryAudience(successor, CampaignActionAudience.Axis).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignExercises.Submit(successor, submission).RejectionReason);
        retainedSteps.Add(new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(submission),
            acceptedCombat.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(acceptedCombat.Evidence.Receipt),
            acceptedCombat.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.ContactCombatSelectionKind, actor.ElementId));
        var imported = CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, retainedSteps);
        Assert.True(imported.IsVerified, imported.RejectionReason.ToString());
        Assert.Equal(acceptedCombat.Evidence.SnapshotCheckpoint,
            imported.Session!.CurrentCombatDecision!.SerializeCanonical());
        Assert.True(CampaignExercises.Reconstruct(imported.Session).IsVerified);
        var barrage = CampaignHostAdapter.CompleteNativeCombatBarrage(imported.Session);
        Assert.True(barrage.IsAccepted, barrage.RejectionReason.ToString());
        Assert.True(CampaignExercises.Reconstruct(barrage.SuccessorSession!).IsVerified);
        var barrageCommand = CampaignHostAdapter.NativeCombatBarrageCommand(imported.Session,
            barrage);
        var barrageSteps = retainedSteps.Append(new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(barrageCommand),
            barrage.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(barrage.Evidence.Receipt),
            barrage.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.NativeCombatBarrageKind)).ToArray();
        Assert.True(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, barrageSteps).IsVerified);
        var forgedBarrage = barrage.SuccessorSession!.CurrentCombatBarrage! with
        {
            NextPositionId = "forged-barrage-route",
        };
        barrageSteps[^1] = new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(barrageCommand),
            [forgedBarrage.SerializeCanonical()],
            CampaignActionAcceptanceReceiptSerializer.Serialize(barrage.Evidence.Receipt),
            barrage.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.NativeCombatBarrageKind);
        Assert.False(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, barrageSteps).IsVerified);
        var forgedSelection = imported.Session.CurrentCombatDecision.Decision with
        {
            BoundRepresentationId = "forged",
        };
        var forgedSteps = retainedSteps.ToArray();
        forgedSteps[^1] = new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(submission),
            [forgedSelection.SerializeCanonical()],
            CampaignActionAcceptanceReceiptSerializer.Serialize(acceptedCombat.Evidence.Receipt),
            acceptedCombat.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.ContactCombatSelectionKind, actor.ElementId);
        Assert.False(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, forgedSteps).IsVerified);
        Assert.False(CampaignHostAdapter.SubmitContactOwnerCombatSelection(
            successor, submission, actor.ElementId).IsAccepted);
        var finishSubmission = submission with { ActionId = cut.FinishOffer!.ActionId };
        var acceptedFinish = CampaignHostAdapter.SubmitContactOwnerCombatSelection(
            session, finishSubmission, actor.ElementId);
        Assert.True(acceptedFinish.IsAccepted);
        Assert.Equal("finish-combat-without-attack",
            acceptedFinish.SuccessorSession!.CurrentCombatDecision!.Decision.Choice);
        Assert.Equal(acceptedFinish.SuccessorSession.CurrentCombatDecision.Decision.NextPositionId,
            CampaignExercises.QueryCheckpoint(acceptedFinish.SuccessorSession).PositionId);
        Assert.True(CampaignExercises.Reconstruct(acceptedFinish.SuccessorSession).IsVerified);
        var directCompletion = CampaignNativeCombatNoAttackCompletionV13.Create(
            acceptedFinish.SuccessorSession.CurrentCombatDecision!,
            acceptedFinish.SuccessorSession.Context);
        Assert.Equal(acceptedFinish.SuccessorSession.CurrentCombatDecision!.StateVersion,
            acceptedFinish.SuccessorSession.CurrentContacts.AuthorityVersion);
        _ = acceptedFinish.SuccessorSession.CurrentContacts
            .AdvanceNativeCombatNoAttackCompletion(directCompletion);
        _ = directCompletion.SerializeStateCanonical(
            acceptedFinish.SuccessorSession.CurrentCombatDecision);
        var completed = CampaignHostAdapter.CompleteNativeCombatNoAttack(
            acceptedFinish.SuccessorSession);
        Assert.True(completed.IsAccepted, completed.RejectionReason.ToString());
        Assert.Equal(CampaignExercises.QueryCheckpoint(completed.SuccessorSession!).PositionId,
            completed.Evidence!.Receipt.ResultingPositionId);
        Assert.Equal(LandSegmentIds.ReserveRelease,
            Cna1979LandSequenceV4.CreateTurn(
                session.CurrentSnapshot.CurrentPosition.SequencePosition!.GameTurn)
                .Single(position => position.PositionId ==
                    CampaignExercises.QueryCheckpoint(completed.SuccessorSession!).PositionId)
                .SegmentId);
        Assert.True(CampaignExercises.Reconstruct(completed.SuccessorSession!).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignHostAdapter.CompleteNativeCombatNoAttack(completed.SuccessorSession!).RejectionReason);
        Assert.Equal(CampaignActionSubmissionRejectionReason.InvalidAuthority,
            CampaignHostAdapter.CompleteNativeCombatNoAttack(acceptedCombat.SuccessorSession!).RejectionReason);
        var finishSteps = retainedSteps.Take(retainedSteps.Count - 1).Append(new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(finishSubmission),
            acceptedFinish.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(acceptedFinish.Evidence.Receipt),
            acceptedFinish.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.ContactCombatSelectionKind, actor.ElementId)).ToArray();
        Assert.True(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes, finishSteps).IsVerified);
        var completionCommand = CampaignHostAdapter.NativeCombatNoAttackCommand(
            acceptedFinish.SuccessorSession, completed);
        var completionStep = new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(completionCommand),
            completed.Evidence!.EventRecords,
            CampaignActionAcceptanceReceiptSerializer.Serialize(completed.Evidence.Receipt),
            completed.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.NativeCombatNoAttackKind);
        var completedSteps = finishSteps.Append(completionStep).ToArray();
        var importedCompletion = CampaignHostAdapter.Import(request,
            started.CreationEventBytes, started.InitialSnapshotBytes, completedSteps);
        Assert.True(importedCompletion.IsVerified, importedCompletion.RejectionReason.ToString());
        Assert.True(CampaignExercises.Reconstruct(importedCompletion.Session!).IsVerified);
        var forgedRoute = completed.SuccessorSession!.CurrentNoAttackCompletion! with
        {
            Steps = completed.SuccessorSession.CurrentNoAttackCompletion!.Steps
                .Select((step, index) => index == 3
                    ? step with { NextPositionId = step.PositionId }
                    : step).ToArray(),
        };
        var forgedCompletionStep = new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(completionCommand),
            [forgedRoute.SerializeCanonical()],
            CampaignActionAcceptanceReceiptSerializer.Serialize(completed.Evidence.Receipt),
            completed.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.NativeCombatNoAttackKind);
        Assert.False(CampaignHostAdapter.Import(request, started.CreationEventBytes,
            started.InitialSnapshotBytes,
            finishSteps.Append(forgedCompletionStep).ToArray()).IsVerified);
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignHostAdapter.SubmitContactOwnerCombatSelection(session,
                submission with { ActionId = $"sha256:{new string('0', 64)}" },
                actor.ElementId).RejectionReason);
    }

    [Fact]
    public void AssaultOffersUseOnlyCurrentAdjacentContactHandlesAndApparentHexes()
    {
        CampaignObservationEdge[] edges =
        [
            new("hex-a", "hex-b", []),
            new("hex-b", "hex-c", []),
        ];
        CampaignContactTargetCandidate[] current =
        [
            new("axis-formation", "contact-adjacent", "hex-b"),
            new("axis-formation", "contact-remote", "hex-c"),
            new("other-formation", "contact-other", "hex-b"),
        ];

        var offers = CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, current);

        var offer = Assert.Single(offers);
        Assert.Equal("axis-element", offer.ActingElementId);
        Assert.Equal("contact-adjacent", offer.ContactHandle);
        Assert.Equal("hex-b", offer.ApparentLocationId);
        Assert.Empty(CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, []));
    }

    [Fact]
    public void SameVisibleContactsProduceSameStableActionReferences()
    {
        CampaignObservationEdge[] edges = [new("hex-a", "hex-b", [])];
        CampaignContactTargetCandidate[] first =
        [
            new("axis-formation", "contact-two", "hex-b"),
            new("axis-formation", "contact-one", "hex-b"),
        ];
        CampaignContactTargetCandidate[] reversed = [first[1], first[0]];

        var left = CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, first);
        var right = CampaignContactCombatSelectionCut.DeriveAdjacentOffers(
            "axis-element", "axis-formation", "hex-a", edges, reversed);

        Assert.Equal(left.Select(value => value.ActionId), right.Select(value => value.ActionId));
        Assert.Equal(2, left.Select(value => value.ActionId).Distinct().Count());
    }

    [Fact]
    public void RealCombatCutSelectsCurrentContactAndRejectsForgedPrivateBinding()
    {
        var prior = BreakdownMovementTests.ChangeElement(
            BreakdownSnapshotTests.Create(true), "commonwealth-infantry", "north");
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        var atCombat = CombatCutFrom(prior, artifact, scenario);
        Assert.Equal(LandStepIds.PositionDetermination,
            atCombat.CurrentPosition.SequenceContext.StepId);
        Assert.Equal(LandPhaseIds.MovementAndCombat,
            atCombat.CurrentPosition.SequenceContext.PhaseId);
        Assert.Equal(LandSegmentIds.Combat,
            atCombat.CurrentPosition.SequenceContext.SegmentId);
        Assert.Equal(LandActorRole.FirstActingSide,
            atCombat.CurrentPosition.SequenceContext.ActorRole);
        Assert.Equal(LandSide.Axis, atCombat.InitiativeHolder);
        var contacts = CampaignContactJournal.Initialize(atCombat, context);
        var cut = CampaignContactCombatSelectionCut.Project(atCombat, context,
            contacts, LandSide.Axis, "axis-infantry");
        Assert.True(cut.MayFinishWithoutAttack);
        var offer = Assert.Single(cut.AssaultOffers);
        var draft = CampaignHostAdapter.QueryContactCombatOwnerDraft(
            new ExerciseSession(atCombat, context, [], contacts),
            CampaignActionAudience.Axis, "axis-infantry");
        var visible = Encoding.UTF8.GetString(draft.VisibleBytes);
        Assert.Contains(offer.ContactHandle, visible, StringComparison.Ordinal);
        Assert.Contains("finish-combat-without-attack", visible, StringComparison.Ordinal);
        Assert.DoesNotContain(offer.ActionId, visible, StringComparison.Ordinal);
        Assert.DoesNotContain("boundRepresentationId", visible, StringComparison.Ordinal);
        Assert.Equal("{}", JsonSerializer.Serialize(draft));

        var selected = CampaignNativeCombatDecisionStateV12.Select(atCombat,
            context, contacts, LandSide.Axis, "axis-infantry", offer.ActionId);
        var canonicalEvent = selected.Decision.SerializeCanonical();
        var replayed = CampaignNativeCombatDecisionStateV12.Replay(atCombat,
            context, contacts, canonicalEvent);
        var readback = CampaignNativeCombatDecisionStateV12.DeserializeCanonical(
            selected.SerializeCanonical(), context, contacts);

        Assert.Equal(atCombat.StateVersion + 1, selected.StateVersion);
        Assert.Equal(LandStepIds.Barrage,
            Cna1979LandSequenceV4.GetNext(atCombat.CurrentPosition.SequencePosition!).StepId);
        Assert.Equal(Cna1979LandSequenceV4.GetNext(
            atCombat.CurrentPosition.SequencePosition!).PositionId,
            selected.Decision.NextPositionId);
        Assert.Equal(selected.SerializeCanonical(), replayed.SerializeCanonical());
        Assert.Equal(selected.SerializeCanonical(), readback.SerializeCanonical());
        Assert.Equal(offer.ContactHandle, selected.Decision.ContactHandle);
        Assert.NotEqual("forged", selected.Decision.BoundRepresentationId);
        var forged = selected.Decision with { BoundRepresentationId = "forged" };
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignNativeCombatDecisionStateV12.Replay(atCombat, context,
                contacts, forged.SerializeCanonical()));
        var forgedRoute = selected.Decision with { NextPositionId = selected.Decision.PositionId };
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignNativeCombatDecisionStateV12.Replay(atCombat, context,
                contacts, forgedRoute.SerializeCanonical()));

        var finish = CampaignNativeCombatDecisionStateV12.Select(atCombat,
            context, contacts, LandSide.Axis, "axis-infantry", cut.FinishOffer!.ActionId);
        Assert.Equal("finish-combat-without-attack", finish.Decision.Choice);
        Assert.Null(finish.Decision.ContactHandle);
        Assert.Null(finish.Decision.BoundRepresentationId);
        Assert.Equal(finish.SerializeCanonical(),
            CampaignNativeCombatDecisionStateV12.Replay(atCombat, context,
                contacts, finish.Decision.SerializeCanonical()).SerializeCanonical());
        Assert.Throws<JsonException>(() => (finish.Decision with
        {
            BoundRepresentationId = selected.Decision.BoundRepresentationId,
        }).SerializeCanonical());

        var completion = CampaignNativeCombatNoAttackCompletionV13.Create(finish, context);
        var completionBytes = completion.SerializeCanonical();
        Assert.Equal(5, completion.Steps.Count);
        Assert.Equal(finish.Decision.NextPositionId, completion.Steps[0].PositionId);
        Assert.Equal(LandSegmentIds.ReserveRelease,
            Cna1979LandSequenceV4.CreateTurn(atCombat.CurrentPosition.SequencePosition!.GameTurn)
                .Single(position => position.PositionId == completion.Steps[^1].NextPositionId).SegmentId);
        Assert.Equal(completionBytes,
            CampaignNativeCombatNoAttackCompletionV13.Replay(finish, context,
                completionBytes).SerializeCanonical());
        Assert.Throws<InvalidOperationException>(() =>
            CampaignNativeCombatNoAttackCompletionV13.Create(selected, context));
        var forgedCompletion = completion with
        {
            Steps = completion.Steps.Select((step, index) => index == 2
                ? step with { NextPositionId = completion.Steps[0].NextPositionId }
                : step).ToArray(),
        };
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignNativeCombatNoAttackCompletionV13.Replay(finish, context,
                forgedCompletion.SerializeCanonical()));

    }

    [Fact]
    public void HiddenEnemyPositionsKeepFinishOfferAndVisibleBytesEqual()
    {
        var source = BreakdownSnapshotTests.Create(true);
        var artifact = BreakdownContentFixture.Artifact();
        var scenario = BreakdownSnapshotTests.Scenario();
        var context = CampaignContentContext.Create(artifact, scenario.ScenarioId);
        var left = CombatCutFrom(BreakdownMovementTests.ChangeElement(source,
            "commonwealth-infantry", "east"), artifact, scenario);
        var right = CombatCutFrom(BreakdownMovementTests.ChangeElement(source,
            "commonwealth-infantry", "south-east"), artifact, scenario);
        var first = CampaignContactCombatSelectionCut.Project(left, context,
            CampaignContactJournal.Initialize(left, context), LandSide.Axis, "axis-infantry");
        var second = CampaignContactCombatSelectionCut.Project(right, context,
            CampaignContactJournal.Initialize(right, context), LandSide.Axis, "axis-infantry");

        Assert.NotNull(first.FinishOffer);
        Assert.Equal(first.FinishOffer.ActionId, second.FinishOffer!.ActionId);
        Assert.Equal(CampaignContactCombatOwnerVisibleCutV2.SerializeCanonical(first),
            CampaignContactCombatOwnerVisibleCutV2.SerializeCanonical(second));
    }

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
