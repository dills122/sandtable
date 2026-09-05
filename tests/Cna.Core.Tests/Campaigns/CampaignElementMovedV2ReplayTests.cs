using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Tests.Content;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignElementMovedV2ReplayTests
{
    [Fact]
    public void AuthorityFactoryCreatesAndReplaysAtomicNonZocTrigger()
    {
        var fixture = CampaignV10TestData.CreateWithReactors(
            [ZocReactionContentTestData.SecondElementId],
            reactorLocationId: "north");

        var moved = CampaignElementMovedV2Factory.Create(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            fixture.TriggeringMove.ToReplayInput());
        var projected = CampaignV10Projector.ApplyMovement(
            fixture.MovementSnapshot,
            moved,
            fixture.Artifact,
            fixture.Scenario);

        Assert.NotNull(moved.OpenedReactionWindow);
        Assert.Null(moved.MovementEndedAfter);
        Assert.Single(moved.OpenedReactionWindow.FrozenOpportunities);
        Assert.Equal(12, projected.StateVersion);
        Assert.Equal(CampaignPositionV10Kind.Reaction, projected.CurrentPosition.Kind);
        Assert.Equal(
            CampaignSuccessorEventSerializer.Serialize(moved),
            CampaignSuccessorEventSerializer.Serialize(
                CampaignElementMovedV2Factory.Create(
                    fixture.MovementSnapshot,
                    fixture.Artifact,
                    fixture.Scenario,
                    moved.ToReplayInput())));
    }

    [Fact]
    public void TriggerUsesIndividualCombatAdjacencyAndExcludesRemoteParticipants()
    {
        const string firstReactor = "commonwealth-reactor-alpha";
        const string secondReactor = "commonwealth-reactor-bravo";
        const string remoteReactor = "commonwealth-reactor-remote";
        var fixture = CampaignV10TestData.CreateWithReactors(
            [firstReactor, secondReactor, remoteReactor],
            includeReactionExit: true,
            reactorLocationId: "north");
        var prior = Relocate(
            fixture.MovementSnapshot,
            (remoteReactor, "center"));

        var moved = CampaignElementMovedV2Factory.Create(
            prior,
            fixture.Artifact,
            fixture.Scenario,
            fixture.TriggeringMove.ToReplayInput());

        var window = Assert.IsType<CampaignReactionWindow>(moved.OpenedReactionWindow);
        Assert.Equal(
            [firstReactor, secondReactor],
            window.FrozenOpportunities
                .Select(value => Assert.Single(value.ReactingRepresentation.BoundElementIds))
                .Order(StringComparer.Ordinal));
        Assert.DoesNotContain(window.FrozenOpportunities, value =>
            value.ReactingRepresentation.BoundElementIds.Contains(
                remoteReactor,
                StringComparer.Ordinal));
        Assert.All(window.FrozenOpportunities, value =>
        {
            Assert.Equal("north", value.AdjacencyEvidence.TriggerLocationId);
            Assert.Equal("east", value.AdjacencyEvidence.CommittedDestinationLocationId);
            Assert.True(value.AdjacencyEvidence.IsAdjacent);
        });
    }

    [Fact]
    public void RemoteCombatAndAdjacentNoncombatDoNotTrigger()
    {
        var remoteFixture = CampaignV10TestData.CreateWithReactors(
            [ZocReactionContentTestData.SecondElementId],
            includeReactionExit: true,
            reactorLocationId: "north");
        var remotePrior = Relocate(
            remoteFixture.MovementSnapshot,
            (ZocReactionContentTestData.SecondElementId, "center"));
        var remoteMove = CampaignElementMovedV2Factory.Create(
            remotePrior,
            remoteFixture.Artifact,
            remoteFixture.Scenario,
            remoteFixture.TriggeringMove.ToReplayInput());
        var noncombatFixture = CampaignV10TestData.CreateWithReactors(
            [ZocReactionContentTestData.SecondElementId],
            reactorClassificationId: Cna1979Combat.InformationalMarkerClassificationId,
            reactorLocationId: "north");
        var noncombatMove = CampaignElementMovedV2Factory.Create(
            noncombatFixture.MovementSnapshot,
            noncombatFixture.Artifact,
            noncombatFixture.Scenario,
            noncombatFixture.TriggeringMove.ToReplayInput());

        Assert.Null(remoteMove.OpenedReactionWindow);
        Assert.Null(noncombatMove.OpenedReactionWindow);
    }

    [Fact]
    public void IneligibleAdjacentCombatStillOpensEmptyWindow()
    {
        var fixture = CampaignV10TestData.CreateWithReactors(
            [ZocReactionContentTestData.SecondElementId],
            reactorLocationId: "north");
        var prior = ChangeElement(
            fixture.MovementSnapshot,
            ZocReactionContentTestData.SecondElementId,
            element => new CampaignElementStateV5(
                element.ElementId,
                element.CurrentLocationId,
                CampaignElementReserveStatus.ReserveI,
                element.OperationalState,
                element.Components));

        var moved = CampaignElementMovedV2Factory.Create(
            prior,
            fixture.Artifact,
            fixture.Scenario,
            fixture.TriggeringMove.ToReplayInput());

        Assert.Empty(Assert.IsType<CampaignReactionWindow>(
            moved.OpenedReactionWindow).FrozenOpportunities);
    }

    [Fact]
    public void PositiveDestinationZocEndsMovementButLocalExitDoesNot()
    {
        const string firstReactor = "commonwealth-zoc-alpha";
        const string secondReactor = "commonwealth-zoc-bravo";
        var entryFixture = CampaignV10TestData.CreateWithReactors(
            [firstReactor, secondReactor],
            reactorLocationId: "north");
        var entry = CampaignElementMovedV2Factory.Create(
            entryFixture.MovementSnapshot,
            entryFixture.Artifact,
            entryFixture.Scenario,
            entryFixture.TriggeringMove.ToReplayInput());
        var exitFixture = CampaignV10TestData.CreateWithReactors(
            [firstReactor, secondReactor],
            includeReactionExit: true,
            reactorLocationId: "north");
        var exitPrior = Relocate(
            exitFixture.MovementSnapshot,
            (firstReactor, "center"),
            (secondReactor, "center"));
        var exit = CampaignElementMovedV2Factory.Create(
            exitPrior,
            exitFixture.Artifact,
            exitFixture.Scenario,
            exitFixture.TriggeringMove.ToReplayInput());

        Assert.Equal(
            new CampaignMovementEndedState(
                entryFixture.MovementSnapshot.CurrentPosition.SequencePosition!),
            entry.MovementEndedAfter);
        Assert.Null(exit.MovementEndedAfter);
        Assert.Null(exit.OpenedReactionWindow);
    }

    [Fact]
    public void MovementBetweenTwoLocallyControlledLocationsRejects()
    {
        const string eastSourceOne = "commonwealth-east-source-alpha";
        const string eastSourceTwo = "commonwealth-east-source-bravo";
        const string westSourceOne = "commonwealth-west-source-alpha";
        const string westSourceTwo = "commonwealth-west-source-bravo";
        var fixture = CampaignV10TestData.CreateWithReactors(
            [eastSourceOne, eastSourceTwo, westSourceOne, westSourceTwo],
            includeReactionExit: true,
            reactorLocationId: "north");
        var prior = Relocate(
            fixture.MovementSnapshot,
            (westSourceOne, "center"),
            (westSourceTwo, "center"));

        Assert.Throws<InvalidOperationException>(() =>
            CampaignElementMovedV2Factory.Create(
                prior,
                fixture.Artifact,
                fixture.Scenario,
                fixture.TriggeringMove.ToReplayInput()));
    }

    [Fact]
    public void CoLocatedHeadquartersDoesNotSupplyAttachmentOrReactionAuthority()
    {
        const string headquarters = "commonwealth-headquarters";
        const string combatUnit = "commonwealth-combat-unit";
        var fixture = CampaignV10TestData.CreateWithReactors(
            [headquarters, combatUnit],
            reactorLocationId: "north",
            reactorClassificationIds: new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [headquarters] = Cna1979Combat.HeadquartersClassificationId,
                [combatUnit] = Cna1979Combat.CombatUnitClassificationId,
            });

        var moved = CampaignElementMovedV2Factory.Create(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            fixture.TriggeringMove.ToReplayInput());

        Assert.Null(moved.MovementEndedAfter);
        var opportunity = Assert.Single(Assert.IsType<CampaignReactionWindow>(
            moved.OpenedReactionWindow).FrozenOpportunities);
        Assert.Equal(combatUnit, Assert.Single(opportunity.ReactingRepresentation.BoundElementIds));
    }

    [Fact]
    public void HeadquartersAdjacencyOpensEmptyWindowWithoutZocAuthority()
    {
        var fixture = CampaignV10TestData.CreateWithReactors(
            ["commonwealth-headquarters"],
            reactorClassificationId: Cna1979Combat.HeadquartersClassificationId,
            reactorLocationId: "north");

        var moved = CampaignElementMovedV2Factory.Create(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            fixture.TriggeringMove.ToReplayInput());

        Assert.Null(moved.MovementEndedAfter);
        Assert.Empty(Assert.IsType<CampaignReactionWindow>(
            moved.OpenedReactionWindow).FrozenOpportunities);
    }

    [Fact]
    public void RemoteQualifyingZocSourceDoesNotAffectMovementEndpointsOrReactionRoster()
    {
        const string localReactor = "commonwealth-local-reactor";
        const string firstRemoteSource = "commonwealth-remote-source-alpha";
        const string secondRemoteSource = "commonwealth-remote-source-bravo";
        var fixture = CampaignV10TestData.CreateWithReactors(
            [localReactor, firstRemoteSource, secondRemoteSource],
            reactorLocationId: "north",
            includeRemoteArea: true);
        var prior = Relocate(
            fixture.MovementSnapshot,
            (firstRemoteSource, "remote-source"),
            (secondRemoteSource, "remote-source"));

        var controlled = CampaignElementMovedV2Factory.DeriveControlledLocationIds(
            prior.World,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth);
        var moved = CampaignElementMovedV2Factory.Create(
            prior,
            fixture.Artifact,
            fixture.Scenario,
            fixture.TriggeringMove.ToReplayInput());

        Assert.Contains("remote-neighbor", controlled);
        Assert.DoesNotContain("west", controlled);
        Assert.DoesNotContain("east", controlled);
        Assert.Null(moved.MovementEndedAfter);
        var opportunity = Assert.Single(Assert.IsType<CampaignReactionWindow>(
            moved.OpenedReactionWindow).FrozenOpportunities);
        Assert.Equal(localReactor, Assert.Single(opportunity.ReactingRepresentation.BoundElementIds));
    }

    [Fact]
    public void MaterializedActiveSideMustMatchRetainedFirstActingSide()
    {
        const string reactor = ZocReactionContentTestData.SecondElementId;
        var fixture = CampaignV10TestData.CreateWithReactors(
            [reactor],
            reactorLocationId: "north");
        var movement = fixture.MovementSnapshot.CurrentPosition.SequencePosition!;
        var wrongSideMovement = new LandSequencePosition(
            movement.ContractVersion,
            movement.PositionId,
            movement.GameTurn,
            movement.OperationStage,
            movement.StageId,
            movement.PhaseId,
            movement.SegmentId,
            movement.StepId,
            movement.ActorRole,
            LandSide.Commonwealth,
            movement.Sources);
        Assert.Throws<ArgumentException>(() => CopyWithAuthority(
            fixture.MovementSnapshot,
            CampaignPositionV10.FromSequence(wrongSideMovement),
            fixture.MovementSnapshot.OperationStageOrders));
    }

    [Fact]
    public void MovementRequiresRetainedActorOrder()
    {
        var fixture = CampaignV10TestData.Create();
        Assert.Throws<ArgumentException>(() => CopyWithAuthority(
            fixture.MovementSnapshot,
            fixture.MovementSnapshot.CurrentPosition,
            []));
    }

    [Fact]
    public void SnapshotRejectsDuplicateRetainedActorOrder()
    {
        var fixture = CampaignV10TestData.Create();
        var current = Assert.Single(fixture.MovementSnapshot.OperationStageOrders);

        Assert.Throws<ArgumentException>(() => CopyWithAuthority(
            fixture.MovementSnapshot,
            fixture.MovementSnapshot.CurrentPosition,
            [current, current]));
    }

    [Fact]
    public void InternalTriggerProjectsDormantReactingAndWaitingActionsDirectly()
    {
        var fixture = CampaignV10TestData.CreateWithReactors(
            [ZocReactionContentTestData.SecondElementId],
            includeReactionExit: true,
            reactorLocationId: "north");
        var moved = CampaignElementMovedV2Factory.Create(
            fixture.MovementSnapshot,
            fixture.Artifact,
            fixture.Scenario,
            fixture.TriggeringMove.ToReplayInput());
        var snapshot = CampaignV10Projector.ApplyMovement(
            fixture.MovementSnapshot,
            moved,
            fixture.Artifact,
            fixture.Scenario);
        var reacting = CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Commonwealth,
            new CampaignObservationV6AuthorityFacts([], []));
        var waiting = CampaignObservationV6Projector.Project(
            snapshot,
            fixture.Artifact,
            fixture.Scenario,
            LandSide.Axis,
            new CampaignObservationV6AuthorityFacts([], []));

        var reactingActions = CampaignObservationV6ActionDerivation.DerivePlayer(reacting);
        var waitingActions = CampaignObservationV6ActionDerivation.DerivePlayer(waiting);

        Assert.IsType<CampaignObservationReactingDecisionState>(reacting.DecisionState);
        Assert.Contains(reactingActions.Candidates, value => value is MoveReactingElementAction);
        Assert.Contains(reactingActions.Candidates, value => value is DeclineReactionWindowAction);
        Assert.IsType<CampaignObservationPhasingWaitingDecisionState>(waiting.DecisionState);
        Assert.Empty(waitingActions.Candidates);
    }

    [Fact]
    public void EventRoundTripsAndProjectsAtomicMoveAndWindowTruth()
    {
        var fixture = CampaignV10TestData.Create();
        var eventBytes = CampaignSuccessorEventSerializer.Serialize(fixture.TriggeringMove);
        var roundTripped = Assert.IsType<ElementMovedV2>(
            CampaignSuccessorEventSerializer.Deserialize(eventBytes));

        var projected = CampaignV10Projector.ApplyMovement(
            fixture.MovementSnapshot,
            roundTripped,
            fixture.Artifact,
            fixture.Scenario,
            (_, _) => fixture.TriggeringMove);

        Assert.Equal(2, roundTripped.ContractVersion);
        Assert.Equal(12, projected.StateVersion);
        Assert.Equal("east", projected.World.Elements.Single(element =>
            element.ElementId == roundTripped.ElementId).CurrentLocationId);
        Assert.Equal("east", projected.World.Representations.Single(value =>
            value.RepresentationId == roundTripped.RepresentationId).CurrentLocationId);
        Assert.Equal(roundTripped.OpenedReactionWindow, projected.ReactionWindow);
        Assert.Equal(CampaignPositionV10Kind.Reaction, projected.CurrentPosition.Kind);
        Assert.Equal(
            eventBytes,
            CampaignSuccessorEventSerializer.Serialize(roundTripped));
    }

    [Fact]
    public void ReplayReconstructsAgainstHistoricalPreStateAndRejectsSemanticTampering()
    {
        var fixture = CampaignV10TestData.Create();
        var actual = CampaignV10TestData.CreateTriggeringMove(
            fixture.MovementSnapshot,
            apparentRepresentationId: "apparent-forged");
        var before = CampaignSnapshotV10Serializer.Serialize(fixture.MovementSnapshot);
        var calls = 0;

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignV10Projector.ApplyMovement(
                fixture.MovementSnapshot,
                actual,
                fixture.Artifact,
                fixture.Scenario,
                (prior, input) =>
                {
                    calls++;
                    Assert.Equal(fixture.MovementSnapshot, prior);
                    Assert.Equal(fixture.TriggeringMove.ElementId, input.ElementId);
                    Assert.Equal(fixture.TriggeringMove.DestinationLocationId,
                        input.DestinationLocationId);
                    return fixture.TriggeringMove;
                }));

        Assert.Equal(1, calls);
        Assert.Equal(before, CampaignSnapshotV10Serializer.Serialize(fixture.MovementSnapshot));
    }

    [Fact]
    public void CheckpointReplayIsByteIdenticalAndDuplicateApplicationRejects()
    {
        var fixture = CampaignV10TestData.Create();
        var canonicalEvent = CampaignSuccessorEventSerializer.Deserialize(
            CampaignSuccessorEventSerializer.Serialize(fixture.TriggeringMove));
        var move = Assert.IsType<ElementMovedV2>(canonicalEvent);
        var expected = CampaignV10Projector.ApplyMovement(
            fixture.MovementSnapshot,
            move,
            fixture.Artifact,
            fixture.Scenario,
            (_, _) => fixture.TriggeringMove);
        var replayed = CampaignV10Projector.ReplayMovementCheckpoint(
            fixture.MovementSnapshot,
            [move],
            fixture.Artifact,
            fixture.Scenario,
            (_, _) => fixture.TriggeringMove);

        Assert.Equal(
            CampaignSnapshotV10Serializer.Serialize(expected),
            CampaignSnapshotV10Serializer.Serialize(replayed));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignV10Projector.ApplyMovement(
                replayed,
                move,
                fixture.Artifact,
                fixture.Scenario,
                (_, _) => fixture.TriggeringMove));
    }

    [Fact]
    public void MovementEndedAndComponentToeProvenanceRoundTripThroughReplay()
    {
        var fixture = CampaignV10TestData.Create();
        var ended = new CampaignMovementEndedState(
            fixture.TriggeringMove.SequencePosition);
        var moved = CampaignV10TestData.CopyMove(
            fixture.TriggeringMove,
            ended,
            fixture.TriggeringMove.OpenedReactionWindow);
        var roundTripped = Assert.IsType<ElementMovedV2>(
            CampaignSuccessorEventSerializer.Deserialize(
                CampaignSuccessorEventSerializer.Serialize(moved)));
        var projected = CampaignV10Projector.ApplyMovement(
            fixture.MovementSnapshot,
            roundTripped,
            fixture.Artifact,
            fixture.Scenario,
            (_, _) => moved);
        var before = fixture.MovementSnapshot.World.Elements.Single(value =>
            value.ElementId == moved.ElementId);
        var after = projected.World.Elements.Single(value =>
            value.ElementId == moved.ElementId);

        Assert.Equal(ended, after.OperationalState.MovementEnded);
        Assert.Equal(before.Components, after.Components);
        Assert.Equal(
            CampaignSnapshotV10Serializer.Serialize(projected),
            CampaignSnapshotV10Serializer.Serialize(
                CampaignSnapshotV10Serializer.Deserialize(
                    CampaignSnapshotV10Serializer.Serialize(projected))));
    }

    [Fact]
    public void RehashedFrozenOpportunityTamperingStillRejects()
    {
        var fixture = CampaignV10TestData.Create();
        var window = fixture.TriggeringMove.OpenedReactionWindow!;
        var original = Assert.Single(window.FrozenOpportunities);
        var forgedRepresentation = new CampaignMapRepresentationState(
            "map-representation.9999",
            original.ReactingRepresentation.CurrentLocationId,
            original.ReactingRepresentation.BindingKind,
            original.ReactingRepresentation.BoundElementIds);
        var forgedOpportunity = new CampaignFrozenReactionOpportunity(
            CampaignReactionIdentity.CreateOpportunity(window.WindowId, forgedRepresentation),
            forgedRepresentation,
            original.AdjacencyEvidence);
        var forgedWindow = new CampaignReactionWindow(
            window.WindowId,
            window.TriggerCommittedStateVersion,
            window.PhasingSide,
            window.ReactingSide,
            window.ReactingPosition,
            window.TriggerAuthority,
            window.ApparentTrigger,
            [forgedOpportunity],
            [],
            null);
        var forged = CampaignV10TestData.CopyMove(
            fixture.TriggeringMove,
            fixture.TriggeringMove.MovementEndedAfter,
            forgedWindow);
        var canonicalForged = Assert.IsType<ElementMovedV2>(
            CampaignSuccessorEventSerializer.Deserialize(
                CampaignSuccessorEventSerializer.Serialize(forged)));

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignV10Projector.ApplyMovement(
                fixture.MovementSnapshot,
                canonicalForged,
                fixture.Artifact,
                fixture.Scenario,
                (_, _) => fixture.TriggeringMove));
    }

    [Fact]
    public void EventReaderRejectsNonCanonicalAndActiveReaderRejectsSuccessor()
    {
        var fixture = CampaignV10TestData.Create();
        var canonical = Encoding.UTF8.GetString(
            CampaignSuccessorEventSerializer.Serialize(fixture.TriggeringMove));
        var extra = canonical.Replace(
            "{\"contractVersion\":2,",
            "{\"contractVersion\":2,\"unexpected\":true,",
            StringComparison.Ordinal);
        var reordered = canonical.Replace(
            "{\"contractVersion\":2,\"eventType\":\"element-moved\"",
            "{\"eventType\":\"element-moved\",\"contractVersion\":2",
            StringComparison.Ordinal);
        var missing = canonical.Replace(
            "\"movementEndedAfter\":null,",
            string.Empty,
            StringComparison.Ordinal);
        var duplicate = canonical.Replace(
            "{\"contractVersion\":2,",
            "{\"contractVersion\":2,\"contractVersion\":2,",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(extra)));
        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(reordered)));
        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(missing)));
        Assert.Throws<JsonException>(() => CampaignSuccessorEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(duplicate)));
        Assert.Throws<JsonException>(() => CampaignEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(canonical)));
    }

    private static CampaignSnapshotV10 Relocate(
        CampaignSnapshotV10 snapshot,
        params (string ElementId, string LocationId)[] changes)
    {
        var locations = changes.ToDictionary(
            value => value.ElementId,
            value => value.LocationId,
            StringComparer.Ordinal);
        var world = new CampaignWorldSnapshotV5(
            CampaignWorldSnapshotV5.CurrentContractVersion,
            snapshot.World.Elements.Select(element => locations.TryGetValue(
                    element.ElementId,
                    out var locationId)
                ? new CampaignElementStateV5(
                    element.ElementId,
                    locationId,
                    element.ReserveStatus,
                    element.OperationalState,
                    element.Components)
                : element),
            snapshot.World.Representations.Select(representation =>
            {
                var changed = representation.BoundElementIds
                    .Where(locations.ContainsKey)
                    .Select(elementId => locations[elementId])
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();
                return changed.Length == 0
                    ? representation
                    : new CampaignMapRepresentationState(
                        representation.RepresentationId,
                        Assert.Single(changed),
                        representation.BindingKind,
                        representation.BoundElementIds);
            }));
        return CopyWithWorld(snapshot, world);
    }

    private static CampaignSnapshotV10 ChangeElement(
        CampaignSnapshotV10 snapshot,
        string elementId,
        Func<CampaignElementStateV5, CampaignElementStateV5> change)
    {
        var world = new CampaignWorldSnapshotV5(
            CampaignWorldSnapshotV5.CurrentContractVersion,
            snapshot.World.Elements.Select(element => string.Equals(
                    element.ElementId,
                    elementId,
                    StringComparison.Ordinal)
                ? change(element)
                : element),
            snapshot.World.Representations);
        return CopyWithWorld(snapshot, world);
    }

    private static CampaignSnapshotV10 CopyWithWorld(
        CampaignSnapshotV10 snapshot,
        CampaignWorldSnapshotV5 world) => new(
        snapshot.ContractVersion,
        snapshot.CampaignId,
        snapshot.StateVersion,
        snapshot.RulesetHash,
        snapshot.Setup,
        world,
        snapshot.InitiativeHolder,
        snapshot.OperationStageOrders,
        snapshot.OperationStageWeather,
        snapshot.RandomState,
        snapshot.CurrentPosition,
        snapshot.ReactionWindow);

    private static CampaignSnapshotV10 CopyWithAuthority(
        CampaignSnapshotV10 snapshot,
        CampaignPositionV10 position,
        IReadOnlyList<CampaignOperationStageOrder> operationStageOrders) => new(
        snapshot.ContractVersion,
        snapshot.CampaignId,
        snapshot.StateVersion,
        snapshot.RulesetHash,
        snapshot.Setup,
        snapshot.World,
        snapshot.InitiativeHolder,
        operationStageOrders,
        snapshot.OperationStageWeather,
        snapshot.RandomState,
        position,
        snapshot.ReactionWindow);
}
