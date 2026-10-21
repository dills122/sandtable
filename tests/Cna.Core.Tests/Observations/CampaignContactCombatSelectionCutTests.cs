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
            imported.Session!.CurrentCombatSelection!.SerializeCanonical());
        Assert.True(CampaignExercises.Reconstruct(imported.Session).IsVerified);
        var forgedSelection = imported.Session.CurrentCombatSelection.Selection with
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
        var atCombat = new CampaignSnapshotV11(11, prior.CampaignId,
            completion.StateVersion, prior.RulesetHash, prior.Setup, prior.World,
            prior.InitiativeHolder, prior.OperationStageOrders,
            prior.OperationStageWeather, prior.RandomState,
            CampaignPositionV11.FromSequence(completion.SequencePosition),
            null, new CampaignBreakdownFlow.Idle());
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
        var offer = Assert.Single(cut.AssaultOffers);

        var selected = CampaignNativeCombatSelectionStateV12.Select(atCombat,
            context, contacts, LandSide.Axis, "axis-infantry", offer.ActionId);
        var canonicalEvent = selected.Selection.SerializeCanonical();
        var replayed = CampaignNativeCombatSelectionStateV12.Replay(atCombat,
            context, contacts, canonicalEvent);
        var readback = CampaignNativeCombatSelectionStateV12.DeserializeCanonical(
            selected.SerializeCanonical(), context, contacts);

        Assert.Equal(atCombat.StateVersion + 1, selected.StateVersion);
        Assert.Equal(selected.SerializeCanonical(), replayed.SerializeCanonical());
        Assert.Equal(selected.SerializeCanonical(), readback.SerializeCanonical());
        Assert.Equal(offer.ContactHandle, selected.Selection.ContactHandle);
        Assert.NotEqual("forged", selected.Selection.BoundRepresentationId);
        var forged = selected.Selection with { BoundRepresentationId = "forged" };
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignNativeCombatSelectionStateV12.Replay(atCombat, context,
                contacts, forged.SerializeCanonical()));

    }
}
