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

public sealed class CampaignContactJournalTests
{
    [Fact]
    public void AdjacentMovementIsRecordedForBothSidesAndSurvivesColdReplay()
    {
        var (prior, after, moved, context, _) = AcceptedReactionMovement();
        var initial = CampaignContactJournal.Initialize(prior, context);
        var committed = initial.AppendMovement(prior, after, moved, context);
        var replayed = CampaignContactJournal.Replay(prior, context, [moved], committed.Records);

        Assert.True(committed.Records.Count > initial.Records.Count);
        Assert.Equal(committed.ObserveCanonical(LandSide.Commonwealth, "commonwealth-infantry.formation"),
            replayed.ObserveCanonical(LandSide.Commonwealth, "commonwealth-infantry.formation"));
        var reacting = Assert.Single(moved.OpenedReactionWindow!.FrozenOpportunities);
        var observer = Assert.Single(reacting.ReactingRepresentation.BoundElementIds) + ".formation";
        Assert.Contains(replayed.CurrentTargetsFor(LandSide.Commonwealth, observer),
            candidate => candidate.ApparentLocationId == moved.DestinationLocationId);
        Assert.DoesNotContain(moved.RepresentationId,
            Encoding.UTF8.GetString(replayed.ObserveCanonical(LandSide.Commonwealth, observer)),
            StringComparison.Ordinal);
    }

    [Fact]
    public void ColdReplayRejectsMissingOrAlteredContactRecord()
    {
        var (prior, after, moved, context, _) = AcceptedReactionMovement();
        var committed = CampaignContactJournal.Initialize(prior, context)
            .AppendMovement(prior, after, moved, context);
        var records = committed.Records.ToArray();
        records[^1][^2] ^= 1;

        Assert.Throws<JsonException>(() => CampaignContactJournal.Replay(prior, context,
            [moved], records));
        Assert.Throws<JsonException>(() => CampaignContactJournal.Replay(prior, context,
            [moved], committed.Records.SkipLast(1).ToArray()));
        Assert.Throws<JsonException>(() => CampaignContactJournal.Replay(prior, context,
            [], committed.Records));
        Assert.Throws<ArgumentException>(() => CampaignContactJournal.Empty("wrong-campaign")
            .AppendMovement(prior, after, moved, context));
    }

    [Fact]
    public void MovementDoesNotPublishContactsToAnUnrelatedFormation()
    {
        var (prior, after, moved, context, _) = AcceptedReactionMovement();
        var initial = CampaignContactJournal.Initialize(prior, context);
        var committed = initial.AppendMovement(prior, after, moved, context);

        Assert.Equal(initial.ObserveCanonical(LandSide.Commonwealth, "unrelated-observer"),
            committed.ObserveCanonical(LandSide.Commonwealth, "unrelated-observer"));
    }

    [Fact]
    public void AdjacentContactBecomesStaleWhenMovementBreaksSight()
    {
        var prior = BreakdownMovementTests.ChangeElement(BreakdownSnapshotTests.Create(true),
            "axis-infantry", "north");
        prior = BreakdownMovementTests.ChangeElement(prior, "commonwealth-infantry", "north-east");
        var context = CampaignContentContext.Create(BreakdownContentFixture.Artifact(),
            BreakdownSnapshotTests.Scenario().ScenarioId);
        var knowledge = new CampaignContactKnowledge(prior.CampaignId, LandSide.Commonwealth);
        foreach (var transition in CampaignContactAdjacencySource.OnCreation(prior, context, knowledge))
            knowledge.Apply(transition);
        var before = Assert.Single(knowledge.CurrentTargetsFor("commonwealth-infantry.formation"));
        var movement = BreakdownMovementTests.Move(prior, "axis-infantry", "north", "north-west");
        var movedWorld = BreakdownMovementTests.ChangeElement(prior, "axis-infantry", "north-west").World;
        var after = new CampaignSnapshotV11(11, prior.CampaignId, movement.StateVersion,
            prior.RulesetHash, prior.Setup, movedWorld, prior.InitiativeHolder,
            prior.OperationStageOrders, prior.OperationStageWeather, prior.RandomState,
            prior.CurrentPosition, null, prior.BreakdownFlow);

        foreach (var transition in CampaignContactAdjacencySource.OnMovement(
                     prior, after, movement, context, knowledge))
            knowledge.Apply(transition);
        Assert.Empty(knowledge.CurrentTargetsFor("commonwealth-infantry.formation"));
        var stale = Assert.Single(knowledge.ObserveFor("commonwealth-infantry.formation"),
            value => value.Handle == before.ContactHandle);
        Assert.False(stale.IsCurrent);
        Assert.Equal("north", stale.ApparentLocationId);
    }

    [Fact]
    public void UnseenMovementLeavesOwnerObservationByteIdentical()
    {
        var prior = BreakdownMovementTests.ChangeElement(BreakdownSnapshotTests.Create(true),
            "commonwealth-infantry", "east");
        var context = CampaignContentContext.Create(BreakdownContentFixture.Artifact(),
            BreakdownSnapshotTests.Scenario().ScenarioId);
        var journal = CampaignContactJournal.Initialize(prior, context);
        var before = journal.ObserveCanonical(LandSide.Commonwealth, "commonwealth-infantry.formation");
        var movement = BreakdownMovementTests.Move(prior, "axis-infantry", "north-west", "north");
        var movedWorld = BreakdownMovementTests.ChangeElement(prior, "axis-infantry", "north").World;
        var after = new CampaignSnapshotV11(11, prior.CampaignId, movement.StateVersion,
            prior.RulesetHash, prior.Setup, movedWorld, prior.InitiativeHolder,
            prior.OperationStageOrders, prior.OperationStageWeather, prior.RandomState,
            prior.CurrentPosition, null, prior.BreakdownFlow);

        journal = journal.AppendMovement(prior, after, movement, context);
        Assert.Equal(before, journal.ObserveCanonical(LandSide.Commonwealth, "commonwealth-infantry.formation"));
    }

    [Fact]
    public void AcceptedReactionMovementUpdatesContactsAndSurvivesColdReadback()
    {
        var (_, _, _, context, opened) = AcceptedReactionMovement();
        var reactingSet = Enum.GetValues<CampaignActionAudience>()
            .Select(audience => CampaignExercises.Query(opened, audience).ActionSet!)
            .First(value => value.Candidates.OfType<MoveReactingElementAction>().Any());
        var reaction = reactingSet.Candidates.OfType<MoveReactingElementAction>()
            .First();
        var accepted = CampaignHostAdapter.Submit(opened, new CampaignActionSubmission(1,
            reactingSet.CampaignId, reactingSet.StateVersion, reactingSet.PositionId,
            reactingSet.Audience, reaction.ActionId));
        Assert.True(accepted.IsAccepted);
        var session = accepted.SuccessorSession!;
        Assert.IsType<ReactingElementMovedV2>(session.CurrentHistory[^1]);
        Assert.True(session.CurrentContacts.Records.Count > opened.CurrentContacts.Records.Count);

        var readback = CampaignContactJournal.FromEvents(context, session.CurrentHistory);
        Assert.Equal(session.CurrentSnapshot.StateVersion, readback.AuthorityVersion);
        Assert.Equal(session.CurrentContacts.Records.Count, readback.Records.Count);
        Assert.True(session.CurrentContacts.Records.Zip(readback.Records)
            .All(pair => pair.First.AsSpan().SequenceEqual(pair.Second)));
        Assert.Equal(session.CurrentContacts.ObserveCanonical(LandSide.Axis, "axis-infantry.formation"),
            readback.ObserveCanonical(LandSide.Axis, "axis-infantry.formation"));
        Assert.Equal(session.CurrentContacts.ObserveCanonical(LandSide.Commonwealth, "commonwealth-infantry.formation"),
            readback.ObserveCanonical(LandSide.Commonwealth, "commonwealth-infantry.formation"));
    }

    private static (CampaignSnapshotV11 Prior, CampaignSnapshotV11 After,
        ElementMovedV3 Movement, CampaignContentContext Context, ExerciseSession Opened) AcceptedReactionMovement()
    {
        var setup = CampaignSetupSnapshotV6.FromDefinition(
            Cna1979BreakdownSetupCatalog.Definitions.Single(value =>
                value.SetupId == Cna1979BreakdownSetupCatalog.ReactionSetupId));
        var request = new CampaignCreationRequest(1, "contact-journal-test",
            Cna1979Ruleset.Manifest.Hash, 12345, setup.SetupId, setup.SetupHash,
            setup.Content.Pack.PackId, setup.Content.Pack.Hash, setup.Content.ScenarioId);
        var session = CampaignHostAdapter.Create(request).Session!;
        for (var index = 0; index < 20; index++)
        {
            var set = Enum.GetValues<CampaignActionAudience>()
                .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
                .First(value => value.Candidates.Count > 0);
            if (set.Candidates.OfType<MoveElementAction>().Any()) break;
            var next = set.Candidates.FirstOrDefault(value => value is ActFirstAction)
                ?? set.Candidates.FirstOrDefault(value => value is CompleteReserveDesignationAction)
                ?? set.Candidates[0];
            session = CampaignHostAdapter.Submit(session, new CampaignActionSubmission(1,
                set.CampaignId, set.StateVersion, set.PositionId, set.Audience, next.ActionId))
                .SuccessorSession!;
        }
        var ownerSet = Enum.GetValues<CampaignActionAudience>()
            .Select(audience => CampaignExercises.Query(session, audience).ActionSet!)
            .First(value => value.Candidates.OfType<MoveElementAction>().Any());
        var trigger = ownerSet.Candidates.OfType<MoveElementAction>()
            .Select(candidate => (candidate, result: CampaignHostAdapter.Submit(session,
                new CampaignActionSubmission(1, ownerSet.CampaignId, ownerSet.StateVersion,
                    ownerSet.PositionId, ownerSet.Audience, candidate.ActionId))))
            .First(value => value.result.IsAccepted
                && value.result.SuccessorSession!.CurrentSnapshot.ReactionWindow is not null);
        var after = trigger.result.SuccessorSession!;
        return (session.CurrentSnapshot, after.CurrentSnapshot,
            Assert.IsType<ElementMovedV3>(after.CurrentHistory[^1]), session.Context, after);
    }
}
