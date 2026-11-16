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
        var preserved = committed.AdvanceSourceTurnNonMovement(committed.CampaignId,
            committed.AuthorityVersion, committed.AuthorityVersion + 1);
        Assert.Equal(committed.Records, preserved.Records);
        Assert.Equal(committed.AuthorityVersion + 1, preserved.AuthorityVersion);
        Assert.Throws<InvalidOperationException>(() => committed.AdvanceSourceTurnNonMovement(
            "other-campaign", committed.AuthorityVersion, committed.AuthorityVersion + 1));
        Assert.Throws<InvalidOperationException>(() => committed.AdvanceSourceTurnNonMovement(
            committed.CampaignId, committed.AuthorityVersion, committed.AuthorityVersion + 2));
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

    [Fact]
    public void SecondSideMovementReactionUsesItsOwnIdentityAndRoundTrips()
    {
        var (prior, after, moved, context, _) = AcceptedReactionMovement();
        var catalog = Cna1979LandSequenceV4.CreateTurn(moved.GameTurn).Single(value =>
            value.OperationStage == moved.OperationStage && value.ActorRole == LandActorRole.SecondActingSide
            && value.SegmentId == LandSegmentIds.Movement);
        var position = new LandSequencePosition(catalog.ContractVersion, catalog.PositionId, catalog.GameTurn,
            catalog.OperationStage, catalog.StageId, catalog.PhaseId, catalog.SegmentId, catalog.StepId,
            catalog.ActorRole, moved.ActingSide, catalog.Sources);
        var input = new ElementMovedV3ReplayInput(prior.CampaignId, prior.StateVersion, position.PositionId,
            moved.ActingSide, moved.ElementId, moved.OriginLocationId, moved.DestinationLocationId);
        var representation = after.World.Representations.Single(value => value.RepresentationId == moved.RepresentationId);
        var opponent = moved.ActingSide == LandSide.Axis ? LandSide.Commonwealth : LandSide.Axis;
        var hash = "sha256:" + new string('a', 64);
        var worldHash = CampaignNativeCombatPaidWorldV22.Hash(prior.World);
        var reservePosition = Cna1979LandSequenceV4.CreateTurn(moved.GameTurn).Single(value =>
            value.OperationStage == moved.OperationStage && value.ActorRole == LandActorRole.SecondActingSide
            && value.PhaseId == LandPhaseIds.ReserveDesignation);
        var reserve = new CampaignNativeSecondReserveV43(43, prior.CampaignId, prior.StateVersion - 1, hash,
            moved.ActingSide, reservePosition.PositionId, position.PositionId, null, worldHash, worldHash);
        var orders = new[] { new CampaignOperationStageOrder(CampaignOperationStageOrder.CurrentContractVersion,
            moved.GameTurn, moved.OperationStage, opponent, moved.ActingSide) };
        var contacts = CampaignContactJournal.Initialize(prior, context);
        var ownerCut = CampaignContactOwnerMovementCut.ProjectSecondMovement(prior.World, position, context,
            contacts, moved.ActingSide, moved.ElementId);
        var offer = Assert.Single(ownerCut.MovementOffers, value => value.DestinationLocationId == moved.DestinationLocationId);
        var actual = CampaignNativeSecondMoveV44.Create(reserve, prior.World, context, orders, contacts,
            moved.ActingSide, moved.GameTurn, moved.ElementId, CampaignNativeSecondMoveV44.ActionIdFor(offer), prior.RulesetHash);
        Assert.NotNull(actual.Event.ReactionWindow);
        Assert.Throws<InvalidOperationException>(() => CampaignNativeSecondMoveV44.Source(actual.Event, actual.World,
            context, orders, moved.ActingSide, moved.GameTurn, actual.Event.StateVersion));
        var window = CampaignElementMovedV3Factory.CreateWindowForMovement(prior.CampaignId, prior.RulesetHash,
            prior.StateVersion, position, context.ArtifactV6!, context.Scenario, input, representation,
            after.World, opponent, CampaignNativeSecondMoveV44.CurrentContractVersion);

        Assert.NotNull(window);
        Assert.NotEmpty(window.FrozenOpportunities);
        Assert.Equal(CampaignNativeSecondMoveV44.CurrentContractVersion, window.TriggerAuthority.MoveContractVersion);
        window.ValidateIdentities(prior.CampaignId, prior.RulesetHash);
        var value = new CampaignNativeSecondMoveV44(44, prior.CampaignId, prior.StateVersion, hash,
            moved.ActingSide, position.PositionId, hash, moved.ElementId, moved.RepresentationId,
            moved.OriginLocationId, moved.DestinationLocationId, hash, hash, window);
        var bytes = value.SerializeCanonical();
        var replayed = CampaignNativeSecondMoveV44.Deserialize(bytes);
        Assert.Equal(bytes, replayed.SerializeCanonical());
        replayed.ReactionWindow!.ValidateIdentities(prior.CampaignId, prior.RulesetHash);
        Assert.Throws<JsonException>(() => (value with { ElementId = "different-element" }).SerializeCanonical());
        Assert.Throws<JsonException>(() => (value with { ReactionWindow = moved.OpenedReactionWindow }).SerializeCanonical());
        Assert.Throws<JsonException>(() => (value with { PriorStateVersion = long.MaxValue }).SerializeCanonical());
        using var document = JsonDocument.Parse(bytes);
        Assert.Throws<JsonException>(() => CampaignV11CanonicalCodec.ParseReactionWindow(
            document.RootElement.GetProperty("reactionWindow")));
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
