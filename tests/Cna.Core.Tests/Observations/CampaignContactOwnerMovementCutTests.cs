using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Campaigns;
using Cna.Core.Tests.Content;
using Cna.Core.Visibility;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignContactOwnerMovementCutTests
{
    [Fact]
    public void ContactDerivedMovementExecutesAtSameCutWithoutHistoricalV7OfferLookup()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(
            Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
                value.SetupId == Cna1979BreakdownSetupCatalog.ReactionSetupId));
        var request = new CampaignCreationRequest(1, "contact-owner-submit-test",
            Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.SetupHash,
            setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId);
        var started = CampaignHostAdapter.Create(request);
        var session = started.Session!;
        var retained = new List<CampaignHostAcceptedStep>();
        for (var index = 0; index < 20; index++)
        {
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .First(value => value.Candidates.Count > 0);
            if (set.Candidates.OfType<MoveElementAction>().Any()) break;
            var next = set.Candidates.FirstOrDefault(value => value is ActFirstAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? set.Candidates[0];
            var command = new CampaignActionSubmission(1, set.CampaignId, set.StateVersion,
                set.PositionId, set.Audience, next.ActionId);
            var step = CampaignHostAdapter.Submit(session, command);
            Assert.True(step.IsAccepted);
            retained.Add(new CampaignHostAcceptedStep(
                CampaignActionSubmissionSerializer.SerializeCanonical(command),
                step.Evidence!.EventRecords, CampaignActionAcceptanceReceiptSerializer.Serialize(
                    step.Evidence.Receipt), step.Evidence.SnapshotCheckpoint));
            session = step.SuccessorSession!;
        }
        var snapshot = session.CurrentSnapshot;
        var context = session.Context;
        var cut = CampaignContactOwnerMovementCut.Project(snapshot, context,
            session.CurrentContacts, LandSide.Axis, "axis-infantry");
        var move = cut.MovementOffers[0];
        var submission = new CampaignActionSubmission(1, snapshot.CampaignId,
            snapshot.StateVersion, CampaignCurrentActionExecution.CurrentPositionId(snapshot),
            CampaignActionAudience.Axis, move.ActionId);

        var accepted = CampaignHostAdapter.SubmitContactOwnerMovement(session, submission,
            "axis-infantry");

        Assert.True(accepted.IsAccepted, accepted.RejectionReason.ToString());
        Assert.NotNull(accepted.SuccessorSession);
        Assert.Equal(snapshot.StateVersion + 1,
            CampaignExercises.QueryCheckpoint(accepted.SuccessorSession).StateVersion);
        Assert.Equal(snapshot.StateVersion + 1,
            accepted.SuccessorSession.CurrentContacts.AuthorityVersion);
        retained.Add(new CampaignHostAcceptedStep(
            CampaignActionSubmissionSerializer.SerializeCanonical(submission),
            accepted.Evidence!.EventRecords, CampaignActionAcceptanceReceiptSerializer.Serialize(
                accepted.Evidence.Receipt), accepted.Evidence.SnapshotCheckpoint,
            CampaignHostAcceptedStep.ContactMovementKind, "axis-infantry"));
        var replayed = CampaignHostAdapter.Import(request, started.CreationEventBytes!,
            started.InitialSnapshotBytes!, retained);
        Assert.True(replayed.IsVerified);
        Assert.Equal(accepted.Evidence.SnapshotCheckpoint,
            CampaignCurrentSnapshotSerializer.Serialize(replayed.Session!.CurrentSnapshot));
        Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignHostAdapter.SubmitContactOwnerMovement(session,
                submission with { ActionId = "sha256:" + new string('0', 64) },
                "axis-infantry").RejectionReason);
    }

    [Fact]
    public void HiddenEnemyPositionDoesNotChangeOwnerContactsOrMovementOffers()
    {
        var original = BreakdownSnapshotTests.Create(true);
        var left = BreakdownMovementTests.ChangeElement(original, "commonwealth-infantry", "east");
        var right = BreakdownMovementTests.ChangeElement(original, "commonwealth-infantry", "south-east");
        var context = CampaignContentContext.Create(BreakdownContentFixture.Artifact(),
            BreakdownSnapshotTests.Scenario().ScenarioId);

        var first = CampaignContactOwnerMovementCut.Project(left, context,
            CampaignContactJournal.Initialize(left, context), LandSide.Axis, "axis-infantry");
        var second = CampaignContactOwnerMovementCut.Project(right, context,
            CampaignContactJournal.Initialize(right, context), LandSide.Axis, "axis-infantry");

        Assert.Empty(first.ContactObservation.CurrentTargets);
        Assert.Equal(first.ContactObservation.SerializeCanonical(),
            second.ContactObservation.SerializeCanonical());
        Assert.NotEmpty(first.MovementOffers);
        Assert.Equal(first.MovementOffers.Select(offer => offer.ActionId),
            second.MovementOffers.Select(offer => offer.ActionId));
        var visible = CampaignContactOwnerVisibleCutV1.SerializeCanonical(first);
        Assert.Equal(visible, CampaignContactOwnerVisibleCutV1.SerializeCanonical(second));
        var json = Encoding.UTF8.GetString(visible);
        Assert.DoesNotContain("authorityVersion", json, StringComparison.Ordinal);
        Assert.DoesNotContain("representationId", json, StringComparison.Ordinal);
        Assert.DoesNotContain(first.MovementOffers[0].ActionId, json, StringComparison.Ordinal);
    }

    [Fact]
    public void ContactAtDestinationChangesOfferFromTheSameCut()
    {
        var original = BreakdownSnapshotTests.Create(true);
        var hidden = BreakdownMovementTests.ChangeElement(original, "commonwealth-infantry", "east");
        var visible = BreakdownMovementTests.ChangeElement(original, "commonwealth-infantry", "north");
        var context = CampaignContentContext.Create(BreakdownContentFixture.Artifact(),
            BreakdownSnapshotTests.Scenario().ScenarioId);

        var unseenCut = CampaignContactOwnerMovementCut.Project(hidden, context,
            CampaignContactJournal.Initialize(hidden, context), LandSide.Axis, "axis-infantry");
        var seenCut = CampaignContactOwnerMovementCut.Project(visible, context,
            CampaignContactJournal.Initialize(visible, context), LandSide.Axis, "axis-infantry");

        Assert.Contains(unseenCut.MovementOffers, offer => offer.DestinationLocationId == "north");
        var offered = unseenCut.MovementOffers.Single(offer => offer.DestinationLocationId == "north");
        var accepted = CampaignElementMovedV3Factory.Create(hidden, context.ArtifactV6!,
            context.Scenario, new ElementMovedV3ReplayInput(hidden.CampaignId,
                hidden.StateVersion, hidden.CurrentPosition.SequencePosition!.PositionId,
                LandSide.Axis, offered.ElementId, offered.OriginLocationId,
                offered.DestinationLocationId));
        Assert.Equal(offered.DestinationLocationId, accepted.DestinationLocationId);
        Assert.Contains(seenCut.ContactObservation.CurrentTargets,
            target => target.ApparentLocationId == "north");
        Assert.Equal("axis-infantry.formation", seenCut.ContactObservation.ActingFormationId);
        Assert.All(seenCut.ContactObservation.Contacts, contact =>
            Assert.Equal("axis-infantry.formation", contact.ObserverFormationId));
        Assert.DoesNotContain(seenCut.MovementOffers, offer => offer.DestinationLocationId == "north");
    }

    [Fact]
    public void OwnerCutRejectsAContactJournalFromAnotherAuthorityVersion()
    {
        var current = BreakdownSnapshotTests.Create(true);
        var context = CampaignContentContext.Create(BreakdownContentFixture.Artifact(),
            BreakdownSnapshotTests.Scenario().ScenarioId);
        var journal = CampaignContactJournal.Initialize(current, context);
        var next = new CampaignSnapshotV11(11, current.CampaignId, current.StateVersion + 1,
            current.RulesetHash, current.Setup, current.World, current.InitiativeHolder,
            current.OperationStageOrders, current.OperationStageWeather, current.RandomState,
            current.CurrentPosition, current.ReactionWindow, current.BreakdownFlow);

        Assert.Throws<ArgumentException>(() => CampaignContactOwnerMovementCut.Project(next,
            context, journal, LandSide.Axis, "axis-infantry"));
        Assert.Equal(next.StateVersion, journal.AdvanceNonMovement(current, next).AuthorityVersion);
    }

    [Fact]
    public void NonVisibleAuthorityAdvanceDoesNotChangeOwnerVisibleCutBytes()
    {
        var current = BreakdownSnapshotTests.Create(true);
        var context = CampaignContentContext.Create(BreakdownContentFixture.Artifact(),
            BreakdownSnapshotTests.Scenario().ScenarioId);
        var journal = CampaignContactJournal.Initialize(current, context);
        var initial = CampaignContactOwnerMovementCut.Project(current, context,
            journal, LandSide.Axis, "axis-infantry");
        var next = new CampaignSnapshotV11(11, current.CampaignId, current.StateVersion + 1,
            current.RulesetHash, current.Setup, current.World, current.InitiativeHolder,
            current.OperationStageOrders, current.OperationStageWeather, current.RandomState,
            current.CurrentPosition, current.ReactionWindow, current.BreakdownFlow);
        var successor = CampaignContactOwnerMovementCut.Project(next, context,
            journal.AdvanceNonMovement(current, next), LandSide.Axis, "axis-infantry");

        Assert.NotEqual(initial.AuthorityVersion, successor.AuthorityVersion);
        Assert.Equal(CampaignContactOwnerVisibleCutV1.SerializeCanonical(initial),
            CampaignContactOwnerVisibleCutV1.SerializeCanonical(successor));
    }

    [Fact]
    public void TrustedHostDraftKeepsPrivateFenceAndActionMapOutOfVisibleBytes()
    {
        var snapshot = BreakdownMovementTests.ChangeElement(BreakdownSnapshotTests.Create(true),
            "commonwealth-infantry", "east");
        var context = CampaignContentContext.Create(BreakdownContentFixture.Artifact(),
            BreakdownSnapshotTests.Scenario().ScenarioId);
        var session = new ExerciseSession(snapshot, context, [],
            CampaignContactJournal.Initialize(snapshot, context));

        var draft = CampaignHostAdapter.QueryContactOwnerDraft(session,
            CampaignActionAudience.Axis, "axis-infantry");
        Assert.Equal(snapshot.StateVersion, draft.AuthorityVersion);
        Assert.NotEmpty(draft.PrivateMovementActions);
        var bytes = draft.VisibleBytes;
        Assert.DoesNotContain("authorityVersion", Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
        Assert.DoesNotContain(draft.PrivateMovementActions[0].ActionId,
            Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
        Assert.Equal("{}", JsonSerializer.Serialize(draft));
        bytes[0] ^= 1;
        Assert.NotEqual(bytes, draft.VisibleBytes);
        Assert.Throws<ArgumentOutOfRangeException>(() => CampaignHostAdapter.QueryContactOwnerDraft(
            session, CampaignActionAudience.System, "axis-infantry"));
        Assert.Throws<ArgumentException>(() => CampaignHostAdapter.QueryContactOwnerDraft(
            session, CampaignActionAudience.Commonwealth, "axis-infantry"));
    }
}
