using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignReactionParticipantReplayTests
{
    [Fact]
    public void FirstStepSelectsParticipantAndCommitsExactMovementAtomically()
    {
        var fixture = OpenWindow();
        var selection = SelectMove(fixture, fixture.Snapshot, "north", "center");

        var moved = CampaignReactionParticipantEventFactory.CreateMove(
            fixture.Snapshot,
            fixture.Artifact,
            fixture.Scenario,
            selection.Intent);
        var projected = CampaignV10Projector.ApplyReactionMove(
            fixture.Snapshot,
            moved,
            fixture.Artifact,
            fixture.Scenario);

        var opportunity = Assert.Single(fixture.Snapshot.ReactionWindow!.FrozenOpportunities);
        var elementId = Assert.Single(opportunity.ReactingRepresentation.BoundElementIds);
        Assert.Equal(13, moved.StateVersion);
        Assert.Equal(opportunity.OpportunityId, moved.OpportunityId);
        Assert.Equal(selection.Candidate.ActionId, moved.ActionId);
        Assert.Equal(selection.Candidate.CostBreakdown.TotalCost, moved.Cost.TotalCost);
        Assert.Equal(CapabilityPointAmount.Zero, moved.CapabilityPointsExpendedBefore);
        Assert.Equal(
            moved.CapabilityPointsExpendedBefore + moved.Cost.TotalCost,
            moved.CapabilityPointsExpendedAfter);
        Assert.Equal(opportunity.OpportunityId, moved.ReactionWindowAfter.ActiveOpportunityId);
        Assert.Empty(moved.ReactionWindowAfter.ResolvedOpportunityIds);
        Assert.Equal(
            fixture.Snapshot.ReactionWindow.FrozenOpportunities,
            moved.ReactionWindowAfter.FrozenOpportunities);
        Assert.Equal("center", projected.World.Elements.Single(value =>
            value.ElementId == elementId).CurrentLocationId);
        Assert.Equal("center", projected.World.Representations.Single(value =>
            value.RepresentationId == moved.RepresentationId).CurrentLocationId);
        Assert.Equal(moved.ReactionWindowAfter, projected.ReactionWindow);
        Assert.Equal(CampaignPositionV10Kind.Reaction, projected.CurrentPosition.Kind);
        Assert.Equal(fixture.Snapshot.RandomState, projected.RandomState);
    }

    [Fact]
    public void LaterStepKeepsActiveParticipantAndAccumulatesExactMovementCost()
    {
        var fixture = OpenWindow(includeContinuation: true);
        var firstSelection = SelectMove(fixture, fixture.Snapshot, "north", "center");
        var first = CampaignReactionParticipantEventFactory.CreateMove(
            fixture.Snapshot,
            fixture.Artifact,
            fixture.Scenario,
            firstSelection.Intent);
        var afterFirst = CampaignV10Projector.ApplyReactionMove(
            fixture.Snapshot,
            first,
            fixture.Artifact,
            fixture.Scenario);
        var secondSelection = SelectMove(fixture, afterFirst, "center", "south");

        var second = CampaignReactionParticipantEventFactory.CreateMove(
            afterFirst,
            fixture.Artifact,
            fixture.Scenario,
            secondSelection.Intent);
        var afterSecond = CampaignV10Projector.ApplyReactionMove(
            afterFirst,
            second,
            fixture.Artifact,
            fixture.Scenario);

        Assert.Equal(14, second.StateVersion);
        Assert.Equal(first.OpportunityId, second.OpportunityId);
        Assert.NotEqual(
            firstSelection.Intent.OpportunityId,
            secondSelection.Intent.OpportunityId);
        Assert.Equal(first.CapabilityPointsExpendedAfter,
            second.CapabilityPointsExpendedBefore);
        Assert.Equal(
            first.CapabilityPointsExpendedAfter + second.Cost.TotalCost,
            second.CapabilityPointsExpendedAfter);
        Assert.Equal(second.OpportunityId, second.ReactionWindowAfter.ActiveOpportunityId);
        Assert.Empty(second.ReactionWindowAfter.ResolvedOpportunityIds);
        Assert.Equal("south", afterSecond.World.Elements.Single(value =>
            value.ElementId == second.ElementId).CurrentLocationId);
        Assert.Equal(afterFirst.RandomState, afterSecond.RandomState);
    }

    [Fact]
    public void CompletionResolvesActiveParticipantAndAllowsAnotherFrozenOpportunity()
    {
        const string firstElement = "commonwealth-reactor-alpha";
        const string secondElement = "commonwealth-reactor-bravo";
        var fixture = OpenWindow(
            includeContinuation: true,
            reactorIds: [firstElement, secondElement],
            reactorLocations: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [firstElement] = "north",
                [secondElement] = "north-two",
            });
        var firstSelection = SelectMove(fixture, fixture.Snapshot, "north", "center");
        var first = CampaignReactionParticipantEventFactory.CreateMove(
            fixture.Snapshot,
            fixture.Artifact,
            fixture.Scenario,
            firstSelection.Intent);
        var active = CampaignV10Projector.ApplyReactionMove(
            fixture.Snapshot,
            first,
            fixture.Artifact,
            fixture.Scenario);
        var completionIntent = SelectCompletion(fixture, active);

        var completed = CampaignReactionParticipantEventFactory.CreateCompletion(
            active,
            fixture.Artifact,
            fixture.Scenario,
            completionIntent);
        var afterCompletion = CampaignV10Projector.ApplyReactionCompletion(
            active,
            completed,
            fixture.Artifact,
            fixture.Scenario);
        var secondSelection = SelectMove(fixture, afterCompletion, "north-two", "center");
        var second = CampaignReactionParticipantEventFactory.CreateMove(
            afterCompletion,
            fixture.Artifact,
            fixture.Scenario,
            secondSelection.Intent);

        Assert.Equal(14, completed.StateVersion);
        Assert.Equal(first.OpportunityId, completed.OpportunityId);
        Assert.Null(completed.ReactionWindowAfter.ActiveOpportunityId);
        Assert.Equal([first.OpportunityId], completed.ReactionWindowAfter.ResolvedOpportunityIds);
        Assert.Equal(active.World, afterCompletion.World);
        Assert.Equal(active.RandomState, afterCompletion.RandomState);
        Assert.NotEqual(first.OpportunityId, second.OpportunityId);
        Assert.Equal(second.OpportunityId, second.ReactionWindowAfter.ActiveOpportunityId);
    }

    [Fact]
    public void CompletionRejectsBeforeFirstAcceptedStep()
    {
        var fixture = OpenWindow();
        var observation = ProjectReacting(fixture, fixture.Snapshot);
        var state = Assert.IsType<CampaignObservationReactingDecisionState>(
            observation.DecisionState);
        var opportunity = Assert.Single(state.OwnOpportunities);
        var action = new CompleteReactionParticipantAction(state.WindowId, opportunity.OpportunityId);
        var intent = new CompleteReactionParticipantIntent(
            fixture.Snapshot.StateVersion,
            observation.Position.PositionId,
            observation.Observer,
            action.ActionId,
            state.WindowId,
            opportunity.OpportunityId);

        Assert.Throws<InvalidOperationException>(() =>
            CampaignReactionParticipantEventFactory.CreateCompletion(
                fixture.Snapshot,
                fixture.Artifact,
                fixture.Scenario,
                intent));
    }

    [Fact]
    public void MoveAuthorityRejectsStaleWrongAudienceWindowOpportunityRouteAndAction()
    {
        var fixture = OpenWindow(includeContinuation: true);
        var valid = SelectMove(fixture, fixture.Snapshot, "north", "center").Intent;
        MoveReactingElementIntent[] invalid =
        [
            valid with { ExpectedStateVersion = valid.ExpectedStateVersion + 1 },
            valid with { ExpectedPositionId = "land.position.invalid" },
            valid with { Side = LandSide.Axis },
            valid with
            {
                WindowId = "sha256:ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
            },
            valid with
            {
                OpportunityId = "sha256:dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd",
            },
            valid with { DestinationLocationId = "south" },
            valid with
            {
                ActionId = "sha256:eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee",
            },
        ];

        Assert.All(invalid, intent => Assert.Throws<InvalidOperationException>(() =>
            CampaignReactionParticipantEventFactory.CreateMove(
                fixture.Snapshot,
                fixture.Artifact,
                fixture.Scenario,
                intent)));
    }

    [Fact]
    public void ActiveEpisodeRejectsAnotherParticipantUntilCompletion()
    {
        const string firstElement = "commonwealth-reactor-alpha";
        const string secondElement = "commonwealth-reactor-bravo";
        var fixture = OpenWindow(
            includeContinuation: true,
            reactorIds: [firstElement, secondElement],
            reactorLocations: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [firstElement] = "north",
                [secondElement] = "north-two",
            });
        var first = CampaignReactionParticipantEventFactory.CreateMove(
            fixture.Snapshot,
            fixture.Artifact,
            fixture.Scenario,
            SelectMove(fixture, fixture.Snapshot, "north", "center").Intent);
        var active = CampaignV10Projector.ApplyReactionMove(
            fixture.Snapshot,
            first,
            fixture.Artifact,
            fixture.Scenario);
        var observation = ProjectReacting(fixture, active);
        var state = Assert.IsType<CampaignObservationReactingDecisionState>(
            observation.DecisionState);
        var other = state.OwnOpportunities.Single(value =>
            value.OpportunityId != state.ActiveParticipant!.OpportunityId);
        var option = Assert.Single(other.MoveOptions, value =>
            value.OriginLocationId == "north-two" && value.DestinationLocationId == "center");
        var action = new MoveReactingElementAction(
            state.WindowId,
            other.OpportunityId,
            option.OriginLocationId,
            option.DestinationLocationId,
            option.CostBreakdown);
        var forged = new MoveReactingElementIntent(
            active.StateVersion,
            observation.Position.PositionId,
            observation.Observer,
            action.ActionId,
            action.WindowId,
            action.OpportunityId,
            action.OriginLocationId,
            action.DestinationLocationId);

        Assert.DoesNotContain(
            CampaignObservationV6ActionDerivation.DerivePlayer(observation).Candidates,
            value => string.Equals(value.ActionId, action.ActionId, StringComparison.Ordinal));
        Assert.Throws<InvalidOperationException>(() =>
            CampaignReactionParticipantEventFactory.CreateMove(
                active,
                fixture.Artifact,
                fixture.Scenario,
                forged));
    }

    [Fact]
    public void ParticipantEventsRoundTripReplayAndRejectTamperingAndDuplicates()
    {
        var fixture = OpenWindow(includeContinuation: true);
        var moved = CampaignReactionParticipantEventFactory.CreateMove(
            fixture.Snapshot,
            fixture.Artifact,
            fixture.Scenario,
            SelectMove(fixture, fixture.Snapshot, "north", "center").Intent);
        var movedBytes = CampaignSuccessorEventSerializer.Serialize(moved);
        var movedRoundTrip = Assert.IsType<ReactingElementMoved>(
            CampaignSuccessorEventSerializer.Deserialize(movedBytes));
        var active = CampaignV10Projector.ApplyReactionMove(
            fixture.Snapshot,
            movedRoundTrip,
            fixture.Artifact,
            fixture.Scenario);
        var completed = CampaignReactionParticipantEventFactory.CreateCompletion(
            active,
            fixture.Artifact,
            fixture.Scenario,
            SelectCompletion(fixture, active));
        var completedBytes = CampaignSuccessorEventSerializer.Serialize(completed);
        var completedRoundTrip = Assert.IsType<ReactionParticipantCompleted>(
            CampaignSuccessorEventSerializer.Deserialize(completedBytes));
        var projected = CampaignV10Projector.ApplyReactionCompletion(
            active,
            completedRoundTrip,
            fixture.Artifact,
            fixture.Scenario);

        Assert.Equal(movedBytes, CampaignSuccessorEventSerializer.Serialize(movedRoundTrip));
        Assert.Equal(completedBytes, CampaignSuccessorEventSerializer.Serialize(completedRoundTrip));
        Assert.Equal(completed.ReactionWindowAfter, projected.ReactionWindow);
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignV10Projector.ApplyReactionMove(
                active,
                movedRoundTrip,
                fixture.Artifact,
                fixture.Scenario));
        Assert.Throws<JsonException>(() => CampaignEventSerializer.Deserialize(movedBytes));
        Assert.Throws<JsonException>(() => CampaignEventSerializer.Deserialize(completedBytes));

        var forged = CopyMove(
            moved,
            [.. moved.MobilitySources, new RuleReference("spi-1979-land-rules", "forged")]);
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignV10Projector.ApplyReactionMove(
                fixture.Snapshot,
                forged,
                fixture.Artifact,
                fixture.Scenario));
    }

    [Fact]
    public void SuccessorReaderRejectsNonCanonicalParticipantEventShapes()
    {
        var fixture = OpenWindow();
        var moved = CampaignReactionParticipantEventFactory.CreateMove(
            fixture.Snapshot,
            fixture.Artifact,
            fixture.Scenario,
            SelectMove(fixture, fixture.Snapshot, "north", "center").Intent);
        var canonical = Encoding.UTF8.GetString(
            CampaignSuccessorEventSerializer.Serialize(moved));

        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(canonical.Replace(
                "{\"contractVersion\":1,",
                "{\"contractVersion\":1,\"unexpected\":true,",
                StringComparison.Ordinal))));
        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(canonical.Replace(
                "{\"contractVersion\":1,",
                "{\"contractVersion\":1,\"contractVersion\":1,",
                StringComparison.Ordinal))));
        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(canonical.Replace(
                "{\"contractVersion\":1,\"eventType\":\"reacting-element-moved\",",
                "{\"eventType\":\"reacting-element-moved\",\"contractVersion\":1,",
                StringComparison.Ordinal))));
    }

    private static ParticipantFixture OpenWindow(
        bool includeContinuation = false,
        IReadOnlyList<string>? reactorIds = null,
        IReadOnlyDictionary<string, string>? reactorLocations = null)
    {
        reactorIds ??= ["commonwealth-reactor-alpha"];
        var source = CampaignV10TestData.CreateWithReactors(
            reactorIds,
            includeReactionExit: true,
            reactorLocationId: "north",
            includeReactionContinuation: includeContinuation,
            reactorLocationIds: reactorLocations);
        var triggering = CampaignElementMovedV2Factory.Create(
            source.MovementSnapshot,
            source.Artifact,
            source.Scenario,
            source.TriggeringMove.ToReplayInput());
        var snapshot = CampaignV10Projector.ApplyMovement(
            source.MovementSnapshot,
            triggering,
            source.Artifact,
            source.Scenario);
        return new ParticipantFixture(source.Artifact, source.Scenario, snapshot);
    }

    private static MoveSelection SelectMove(
        ParticipantFixture fixture,
        CampaignSnapshotV10 snapshot,
        string origin,
        string destination)
    {
        var observation = ProjectReacting(fixture, snapshot);
        var set = CampaignObservationV6ActionDerivation.DerivePlayer(observation);
        var candidate = Assert.Single(set.Candidates.OfType<MoveReactingElementAction>(), value =>
            value.OriginLocationId == origin && value.DestinationLocationId == destination);
        var intent = Assert.IsType<MoveReactingElementIntent>(
            CampaignObservationV6ActionDerivation.MapSubmission(
                observation,
                Submission(set, candidate)));
        return new MoveSelection(candidate, intent);
    }

    private static CompleteReactionParticipantIntent SelectCompletion(
        ParticipantFixture fixture,
        CampaignSnapshotV10 snapshot)
    {
        var observation = ProjectReacting(fixture, snapshot);
        var set = CampaignObservationV6ActionDerivation.DerivePlayer(observation);
        var candidate = Assert.Single(
            set.Candidates.OfType<CompleteReactionParticipantAction>());
        return Assert.IsType<CompleteReactionParticipantIntent>(
            CampaignObservationV6ActionDerivation.MapSubmission(
                observation,
                Submission(set, candidate)));
    }

    private static CampaignObservationV6 ProjectReacting(
        ParticipantFixture fixture,
        CampaignSnapshotV10 snapshot)
    {
        var window = snapshot.ReactionWindow!;
        var controlled = CampaignElementMovedV2Factory.DeriveControlledLocationIds(
            snapshot.World,
            fixture.Artifact,
            fixture.Scenario,
            window.PhasingSide);
        return CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            window.ReactingSide,
            new CampaignObservationV6AuthorityFacts(controlled, []));
    }

    private static CampaignActionSubmission Submission(
        CampaignLegalActionSet set,
        CampaignActionCandidate candidate) => new(
            CampaignActionSubmission.CurrentContractVersion,
            set.CampaignId,
            set.StateVersion,
            set.PositionId,
            set.Audience,
            candidate.ActionId);

    private static ReactingElementMoved CopyMove(
        ReactingElementMoved moved,
        IReadOnlyList<RuleReference> mobilitySources) => new(
            moved.CampaignId,
            moved.StateVersion,
            moved.PriorStateVersion,
            moved.FromPositionId,
            moved.GameTurn,
            moved.OperationStage,
            moved.ActingSide,
            moved.ActionId,
            moved.SubmittedWindowId,
            moved.SubmittedOpportunityId,
            moved.WindowId,
            moved.OpportunityId,
            moved.ElementId,
            moved.RepresentationId,
            moved.OriginLocationId,
            moved.DestinationLocationId,
            moved.MobilityId,
            mobilitySources,
            moved.Cost,
            moved.CapabilityPointsExpendedBefore,
            moved.CapabilityPointsExpendedAfter,
            moved.CohesionBefore,
            moved.CohesionAfter,
            moved.ReactionWindowAfter);

    private sealed record ParticipantFixture(
        ContentPackV5Artifact Artifact,
        ContentScenario Scenario,
        CampaignSnapshotV10 Snapshot);

    private sealed record MoveSelection(
        MoveReactingElementAction Candidate,
        MoveReactingElementIntent Intent);
}
