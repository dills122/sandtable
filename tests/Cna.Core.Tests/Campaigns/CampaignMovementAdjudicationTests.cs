using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignMovementAdjudicationTests
{
    [Fact]
    public void SupportedMoveRecalculatesCostAndProjectsOneAtomicEvent()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var before = evidence.Snapshot;
        var beforeBytes = CampaignSnapshotSerializer.Serialize(before);
        var candidate = CampaignMovementTestData.FindMove(
            before,
            evidence.Context,
            evidence.ActingSide,
            "commonwealth-element-a",
            "north-east");
        var command = CampaignMovementTestData.CommandFor(
            before,
            evidence.ActingSide,
            candidate);

        var decision = CampaignEngine.Decide(before, command, evidence.Context);

        Assert.True(decision.IsAccepted);
        var moved = Assert.IsType<ElementMoved>(Assert.Single(decision.Events));
        var successor = CampaignProjector.Apply(before, moved, evidence.Context);
        var element = successor.World.Elements.Single(value =>
            value.ElementId == command.ElementId);
        var representation = successor.World.Representations.Single(value =>
            value.RepresentationId == moved.RepresentationId);

        Assert.Equal(before.StateVersion + 1, successor.StateVersion);
        Assert.Equal(before.SequencePosition, successor.SequencePosition);
        Assert.Equal(command.DestinationLocationId, element.CurrentLocationId);
        Assert.Equal(command.DestinationLocationId, representation.CurrentLocationId);
        Assert.Equal(new CapabilityPointAmount(1, 2),
            element.OperationalState.CapabilityPointsExpended);
        Assert.Equal(0, element.OperationalState.CohesionLevel);
        Assert.Equal(before.RandomState, successor.RandomState);
        Assert.Equal(beforeBytes, CampaignSnapshotSerializer.Serialize(before));
        Assert.True(CampaignSnapshotValidator.IsValid(successor, evidence.Context));
    }

    [Fact]
    public void RepeatedMovesReplayToByteIdenticalAuthority()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var snapshot = evidence.Snapshot;
        var events = new List<CampaignEvent>(evidence.Events);

        Move("commonwealth-element-a", "north-east");
        Move("commonwealth-element-a", "east");
        Move("commonwealth-element-b", "south");

        var replayed = CampaignProjector.Replay(events, evidence.Context);

        Assert.Equal(3, events.OfType<ElementMoved>().Count());
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(snapshot),
            CampaignSnapshotSerializer.Serialize(replayed));
        Assert.Equal(new CapabilityPointAmount(1, 1), snapshot.World.Elements
            .Single(value => value.ElementId == "commonwealth-element-a")
            .OperationalState.CapabilityPointsExpended);
        Assert.Equal(new CapabilityPointAmount(1, 1), snapshot.World.Elements
            .Single(value => value.ElementId == "commonwealth-element-b")
            .OperationalState.CapabilityPointsExpended);

        void Move(string elementId, string destination)
        {
            var candidate = CampaignMovementTestData.FindMove(
                snapshot,
                evidence.Context,
                evidence.ActingSide,
                elementId,
                destination);
            var result = CampaignEngine.Decide(
                snapshot,
                CampaignMovementTestData.CommandFor(
                    snapshot,
                    evidence.ActingSide,
                    candidate),
                evidence.Context);
            var campaignEvent = Assert.IsType<ElementMoved>(Assert.Single(result.Events));
            snapshot = CampaignProjector.Apply(snapshot, campaignEvent, evidence.Context);
            events.Add(campaignEvent);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void StrictReplayIsDeterministicForZeroOneAndManyMoves(int moveCount)
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var snapshot = evidence.Snapshot;
        var events = new List<CampaignEvent>(evidence.Events);
        var destination = "north-east";

        for (var index = 0; index < moveCount; index++)
        {
            var candidate = CampaignMovementTestData.FindMove(
                snapshot,
                evidence.Context,
                evidence.ActingSide,
                "commonwealth-element-a",
                destination);
            var result = CampaignEngine.Decide(
                snapshot,
                CampaignMovementTestData.CommandFor(
                    snapshot,
                    evidence.ActingSide,
                    candidate),
                evidence.Context);
            var moved = Assert.IsType<ElementMoved>(Assert.Single(result.Events));
            snapshot = CampaignProjector.Apply(snapshot, moved, evidence.Context);
            events.Add(moved);
            destination = destination == "north-east" ? "east" : "north-east";
        }

        var firstReplay = CampaignProjector.Replay(events, evidence.Context);
        var secondReplay = CampaignProjector.Replay(
            events.Select(campaignEvent => CampaignEventSerializer.Deserialize(
                CampaignEventSerializer.Serialize(campaignEvent))),
            evidence.Context);

        Assert.Equal(moveCount, events.OfType<ElementMoved>().Count());
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(snapshot),
            CampaignSnapshotSerializer.Serialize(firstReplay));
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(firstReplay),
            CampaignSnapshotSerializer.Serialize(secondReplay));
    }

    [Fact]
    public void ResultingExpenditureAboveBaseCpaRejectsWithoutAnEvent()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var snapshot = evidence.Snapshot;
        const string elementId = "commonwealth-element-a";
        var allowance = evidence.Context.Artifact.Definition.Elements.Single(element =>
            element.ElementId == elementId).BaseCapabilityPointAllowance;
        var destination = "north-east";
        MovementActionCostBreakdown? lastCost = null;

        while (snapshot.World.Elements.Single(element => element.ElementId == elementId)
                   .OperationalState.CapabilityPointsExpended
               < new CapabilityPointAmount(allowance, 1))
        {
            var candidate = CampaignMovementTestData.FindMove(
                snapshot,
                evidence.Context,
                evidence.ActingSide,
                elementId,
                destination);
            lastCost = candidate.CostBreakdown;
            var accepted = CampaignEngine.Decide(
                snapshot,
                CampaignMovementTestData.CommandFor(
                    snapshot,
                    evidence.ActingSide,
                    candidate),
                evidence.Context);
            snapshot = CampaignProjector.Apply(
                snapshot,
                Assert.Single(accepted.Events),
                evidence.Context);
            destination = destination == "north-east" ? "east" : "north-east";
        }

        var element = snapshot.World.Elements.Single(value => value.ElementId == elementId);
        var forgedCandidate = new MoveElementAction(
            elementId,
            element.CurrentLocationId,
            destination,
            lastCost!);
        var before = CampaignSnapshotSerializer.Serialize(snapshot);
        var rejected = CampaignEngine.Decide(
            snapshot,
            CampaignMovementTestData.CommandFor(
                snapshot,
                evidence.ActingSide,
                forgedCandidate),
            evidence.Context);

        Assert.False(rejected.IsAccepted);
        Assert.Equal(
            CampaignCommandRejectionReason.UnsupportedTransition,
            rejected.RejectionReason);
        Assert.Empty(rejected.Events);
        Assert.Equal(before, CampaignSnapshotSerializer.Serialize(snapshot));
    }

    [Fact]
    public void DepletedCohesionRejectsAtAuthorityAdmissionWithoutAnEvent()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var candidate = CampaignMovementTestData.FindMove(
            evidence.Snapshot,
            evidence.Context,
            evidence.ActingSide,
            "commonwealth-element-a",
            "north-east");
        var command = CampaignMovementTestData.CommandFor(
            evidence.Snapshot,
            evidence.ActingSide,
            candidate);
        var depleted = ReplaceCohesion(
            evidence.Snapshot,
            command.ElementId,
            -26);
        var worldBefore = depleted.World;
        var elementsBefore = depleted.World.Elements.ToArray();

        var rejected = CampaignEngine.Decide(depleted, command, evidence.Context);

        Assert.False(rejected.IsAccepted);
        Assert.Equal(CampaignCommandRejectionReason.InvalidState, rejected.RejectionReason);
        Assert.Empty(rejected.Events);
        Assert.Same(worldBefore, depleted.World);
        Assert.Equal(elementsBefore, depleted.World.Elements);
    }

    [Fact]
    public void MoveIntoApparentEnemyOccupancyRejectsWithoutAnEvent()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var approach = CampaignMovementTestData.FindMove(
            evidence.Snapshot,
            evidence.Context,
            evidence.ActingSide,
            "commonwealth-element-a",
            "center");
        var approachDecision = CampaignEngine.Decide(
            evidence.Snapshot,
            CampaignMovementTestData.CommandFor(
                evidence.Snapshot,
                evidence.ActingSide,
                approach),
            evidence.Context);
        var atContactBoundary = CampaignProjector.Apply(
            evidence.Snapshot,
            Assert.Single(approachDecision.Events),
            evidence.Context);
        var command = new MoveElement(
            atContactBoundary.StateVersion,
            atContactBoundary.SequencePosition.PositionId,
            evidence.ActingSide,
            approach.ActionId,
            approach.ElementId,
            "center",
            "west");
        var before = CampaignSnapshotSerializer.Serialize(atContactBoundary);

        var rejected = CampaignEngine.Decide(
            atContactBoundary,
            command,
            evidence.Context);

        Assert.False(rejected.IsAccepted);
        Assert.Equal(
            CampaignCommandRejectionReason.UnsupportedTransition,
            rejected.RejectionReason);
        Assert.Empty(rejected.Events);
        Assert.Equal(before, CampaignSnapshotSerializer.Serialize(atContactBoundary));
    }

    [Fact]
    public void MoveBeyondAuthoritativeStackingLimitRejectsWithoutAnEvent()
    {
        var evidence = ReachMovementWithFiveFriendlyDestinationOccupants();
        var baseline = CampaignMovementTestData.ReachMovement();
        var candidate = CampaignMovementTestData.FindMove(
            baseline.Snapshot,
            baseline.Context,
            baseline.ActingSide,
            "commonwealth-element-a",
            "north-east");
        var command = new MoveElement(
            evidence.Snapshot.StateVersion,
            evidence.Snapshot.SequencePosition.PositionId,
            evidence.ActingSide,
            candidate.ActionId,
            "commonwealth-element-a",
            "east",
            "north-east");
        var before = CampaignSnapshotSerializer.Serialize(evidence.Snapshot);

        var rejected = CampaignEngine.Decide(
            evidence.Snapshot,
            command,
            evidence.Context);

        Assert.False(rejected.IsAccepted);
        Assert.Equal(
            CampaignCommandRejectionReason.UnsupportedTransition,
            rejected.RejectionReason);
        Assert.Empty(rejected.Events);
        Assert.Equal(before, CampaignSnapshotSerializer.Serialize(evidence.Snapshot));
    }

    [Fact]
    public void UnsupportedAuthoritativeStackingTableRejectsWithoutAnEvent()
    {
        var evidence = ReachMovementWithUnsupportedElementOrganization();
        var baseline = CampaignMovementTestData.ReachMovement();
        var candidate = CampaignMovementTestData.FindMove(
            baseline.Snapshot,
            baseline.Context,
            baseline.ActingSide,
            "commonwealth-element-a",
            "north-east");
        var command = new MoveElement(
            evidence.Snapshot.StateVersion,
            evidence.Snapshot.SequencePosition.PositionId,
            evidence.ActingSide,
            candidate.ActionId,
            candidate.ElementId,
            candidate.OriginLocationId,
            candidate.DestinationLocationId);
        var before = CampaignSnapshotSerializer.Serialize(evidence.Snapshot);

        var rejected = CampaignEngine.Decide(
            evidence.Snapshot,
            command,
            evidence.Context);

        Assert.False(rejected.IsAccepted);
        Assert.Equal(
            CampaignCommandRejectionReason.UnsupportedTransition,
            rejected.RejectionReason);
        Assert.Empty(rejected.Events);
        Assert.Equal(before, CampaignSnapshotSerializer.Serialize(evidence.Snapshot));
    }

    [Fact]
    public void InvalidStaleForgedUnsupportedAndOutOfBoundsMovesEmitNothing()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var snapshot = evidence.Snapshot;
        var candidate = CampaignMovementTestData.FindMove(
            snapshot,
            evidence.Context,
            evidence.ActingSide,
            "commonwealth-element-a",
            "north-east");
        var valid = CampaignMovementTestData.CommandFor(
            snapshot,
            evidence.ActingSide,
            candidate);
        var otherSide = evidence.ActingSide == LandSide.Axis
            ? LandSide.Commonwealth
            : LandSide.Axis;
        MoveElement[] rejected =
        [
            valid with { ContractVersion = 2 },
            valid with { ExpectedStateVersion = valid.ExpectedStateVersion - 1 },
            valid with { ExpectedPositionId = "land.position.forged" },
            valid with { ActingSide = otherSide },
            valid with { CandidateId = "sha256:0000000000000000000000000000000000000000000000000000000000000000" },
            valid with { ElementId = "axis-element-a" },
            valid with { OriginLocationId = "west" },
            valid with { DestinationLocationId = "missing-location" },
            valid with { DestinationLocationId = "south-west" },
        ];
        var before = CampaignSnapshotSerializer.Serialize(snapshot);

        foreach (var command in rejected)
        {
            var decision = CampaignEngine.Decide(snapshot, command, evidence.Context);

            Assert.False(decision.IsAccepted);
            Assert.Empty(decision.Events);
            Assert.Equal(before, CampaignSnapshotSerializer.Serialize(snapshot));
        }

        var reserveEvidence = CampaignMovementTestData.ReachMovement(reserveCount: 1);
        var reserveElement = reserveEvidence.Snapshot.World.Elements.Single(element =>
            element.ReserveStatus == CampaignElementReserveStatus.ReserveI);
        var representation = reserveEvidence.Snapshot.World.Representations.Single(value =>
            value.BoundElementIds.Contains(reserveElement.ElementId));
        var edge = reserveEvidence.Context.Artifact.Definition.Edges.First(value =>
            value.FirstLocationId == representation.CurrentLocationId
            || value.SecondLocationId == representation.CurrentLocationId);
        var destination = edge.FirstLocationId == representation.CurrentLocationId
            ? edge.SecondLocationId
            : edge.FirstLocationId;
        var forgedReserve = new MoveElement(
            reserveEvidence.Snapshot.StateVersion,
            reserveEvidence.Snapshot.SequencePosition.PositionId,
            reserveEvidence.ActingSide,
            valid.CandidateId,
            reserveElement.ElementId,
            reserveElement.CurrentLocationId,
            destination);
        var reserveDecision = CampaignEngine.Decide(
            reserveEvidence.Snapshot,
            forgedReserve,
            reserveEvidence.Context);

        Assert.False(reserveDecision.IsAccepted);
        Assert.Empty(reserveDecision.Events);
    }

    [Fact]
    public void ForgedEventCannotHalfApplyTheWorld()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var candidate = CampaignMovementTestData.FindMove(
            evidence.Snapshot,
            evidence.Context,
            evidence.ActingSide,
            "commonwealth-element-a",
            "north-east");
        var moved = CampaignMovementEventFactory.Create(
            evidence.Snapshot,
            evidence.Context,
            CampaignMovementTestData.CommandFor(
                evidence.Snapshot,
                evidence.ActingSide,
                candidate));
        var before = CampaignSnapshotSerializer.Serialize(evidence.Snapshot);

        Assert.Throws<InvalidCampaignHistoryException>(() =>
        {
            _ = CampaignProjector.Apply(
                evidence.Snapshot,
                Copy(moved, representationId: "map-representation.0004"),
                evidence.Context);
        });
        Assert.Throws<ArgumentException>(() =>
        {
            _ = Copy(
                moved,
                capabilityPointsExpendedAfter: new CapabilityPointAmount(2, 1));
        });
        Assert.Equal(before, CampaignSnapshotSerializer.Serialize(evidence.Snapshot));
    }

    [Fact]
    public void PublicMovementActionsRefreshFromTheMovedObservation()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var candidate = CampaignMovementTestData.FindMove(
            evidence.Snapshot,
            evidence.Context,
            evidence.ActingSide,
            "commonwealth-element-a",
            "north-east");
        var decision = CampaignEngine.Decide(
            evidence.Snapshot,
            CampaignMovementTestData.CommandFor(
                evidence.Snapshot,
                evidence.ActingSide,
                candidate),
            evidence.Context);
        var moved = CampaignProjector.Apply(
            evidence.Snapshot,
            Assert.Single(decision.Events),
            evidence.Context);

        var actingAudience = CampaignReserveActionTestData.ToAudience(evidence.ActingSide);
        var opponentAudience = actingAudience == CampaignActionAudience.Axis
            ? CampaignActionAudience.Commonwealth
            : CampaignActionAudience.Axis;
        var acting = CampaignLegalActions.Query(
            new CampaignAuthorityHandle(moved, evidence.Context),
            actingAudience);

        Assert.True(acting.IsSuccessful);
        Assert.Single(acting.ActionSet!.Candidates
            .OfType<CompleteMovementSegmentAction>());
        Assert.NotEmpty(acting.ActionSet.Candidates.OfType<MoveElementAction>());
        Assert.All(
            acting.ActionSet.Candidates.OfType<MoveElementAction>()
                .Where(move => move.ElementId == candidate.ElementId),
            move => Assert.Equal(candidate.DestinationLocationId,
                move.OriginLocationId));
        Assert.Empty(CampaignLegalActions.Query(
            new CampaignAuthorityHandle(moved, evidence.Context),
            opponentAudience).ActionSet!.Candidates);
        Assert.Empty(CampaignLegalActions.Query(
            new CampaignAuthorityHandle(moved, evidence.Context),
            CampaignActionAudience.System).ActionSet!.Candidates);
    }

    private static ElementMoved Copy(
        ElementMoved source,
        string? representationId = null,
        CapabilityPointAmount? capabilityPointsExpendedAfter = null) => new(
            source.CampaignId,
            source.StateVersion,
            source.PriorStateVersion,
            source.FromPositionId,
            source.GameTurn,
            source.OperationStage,
            source.ActingSide,
            source.ElementId,
            representationId ?? source.RepresentationId,
            source.OriginLocationId,
            source.DestinationLocationId,
            source.MobilityId,
            source.MobilitySources,
            source.Cost,
            source.CapabilityPointsExpendedBefore,
            capabilityPointsExpendedAfter ?? source.CapabilityPointsExpendedAfter,
            source.CohesionBefore,
            source.CohesionAfter,
            source.SequencePosition);

    private static CampaignSnapshot ReplaceCohesion(
        CampaignSnapshot snapshot,
        string elementId,
        int cohesionLevel)
    {
        var world = new CampaignWorldSnapshot(
            CampaignWorldSnapshot.CurrentContractVersion,
            snapshot.World.Elements.Select(element => element.ElementId == elementId
                ? new CampaignElementState(
                    element.ElementId,
                    element.CurrentLocationId,
                    element.ReserveStatus,
                    new CampaignElementOperationalState(
                        element.OperationalState.LedgerGameTurn,
                        element.OperationalState.LedgerOperationStage,
                        element.OperationalState.CapabilityPointsExpended,
                        cohesionLevel,
                        element.OperationalState.VehicleBreakdownState))
                : element).ToArray(),
            snapshot.World.Representations);
        return snapshot with { World = world };
    }

    private static CampaignMovementEvidence ReachMovementWithFiveFriendlyDestinationOccupants()
    {
        var baseline = Cna1979SyntheticContentCatalog.Artifact.Definition;
        var elementTemplate = baseline.Elements.Single(element =>
            element.ElementId == "commonwealth-element-b");
        var placementTemplate = baseline.Scenarios
            .Single(scenario => scenario.ScenarioId == "movement-contact-lab")
            .InitialPlacements.Single(placement =>
                placement.ElementId == elementTemplate.ElementId);
        var addedElements = Enumerable.Range(1, 5).Select(index => new ContentCombatElement(
            $"commonwealth-stack-{index}",
            elementTemplate.SideId,
            elementTemplate.ParentFormationId,
            elementTemplate.OrganizationId,
            elementTemplate.MobilityId,
            elementTemplate.BaseCapabilityPointAllowance,
            elementTemplate.PlacementMode,
            elementTemplate.Origin)).ToArray();
        var scenarios = baseline.Scenarios.Select(scenario => new ContentScenario(
            scenario.ScenarioId,
            scenario.Start,
            scenario.End,
            scenario.InitialPlacements.Concat(addedElements.Select(element =>
                new ContentInitialPlacement(
                    element.ElementId,
                    "north-east",
                    placementTemplate.Origin))),
            scenario.Origin)).ToArray();
        var definition = new ContentPackDefinition(
            baseline.SchemaVersion,
            baseline.FormatId,
            "movement-stacking-adjudication-test",
            baseline.RulesetId,
            baseline.Capabilities,
            baseline.SourceIndex,
            baseline.Locations,
            baseline.WeatherAreaAssignments,
            baseline.Edges,
            baseline.Formations,
            baseline.Elements.Concat(addedElements),
            scenarios);

        return ReachMovementWithContent(definition, "campaign-movement-stacking");
    }

    private static CampaignMovementEvidence ReachMovementWithUnsupportedElementOrganization()
    {
        var baseline = Cna1979SyntheticContentCatalog.Artifact.Definition;
        var definition = new ContentPackDefinition(
            baseline.SchemaVersion,
            baseline.FormatId,
            "movement-unsupported-stacking-adjudication-test",
            baseline.RulesetId,
            baseline.Capabilities,
            baseline.SourceIndex,
            baseline.Locations,
            baseline.WeatherAreaAssignments,
            baseline.Edges,
            baseline.Formations,
            baseline.Elements.Select(element => element.ElementId == "commonwealth-element-a"
                ? new ContentCombatElement(
                    element.ElementId,
                    element.SideId,
                    element.ParentFormationId,
                    "land.organization.regiment",
                    element.MobilityId,
                    element.BaseCapabilityPointAllowance,
                    element.PlacementMode,
                    element.Origin,
                    element.BreakdownVehicleCohort)
                : element),
            baseline.Scenarios);

        return ReachMovementWithContent(
            definition,
            "campaign-movement-unsupported-stacking");
    }

    private static CampaignMovementEvidence ReachMovementWithContent(
        ContentPackDefinition definition,
        string campaignId)
    {
        var artifact = ContentPackArtifact.Create(definition);
        var context = CampaignContentContext.Create(artifact, "movement-contact-lab");
        var catalogSetup = Cna1979SetupCatalog.Definitions[0];
        var setup = new CampaignSetupDefinition(
            catalogSetup.SchemaVersion,
            catalogSetup.SetupId,
            catalogSetup.DisplayName,
            catalogSetup.IsSynthetic,
            catalogSetup.InitialGameTurn,
            catalogSetup.InitialInitiative,
            catalogSetup.OpeningPreamble,
            catalogSetup.Weather,
            catalogSetup.StageEntry,
            context.Selection,
            catalogSetup.Sources);
        var created = new CampaignCreated(
            campaignId,
            1,
            Cna1979Ruleset.Manifest.Hash,
            CampaignSetupSnapshot.FromDefinition(setup),
            CampaignWorldFactory.CreateInitial(artifact, context.Scenario),
            SandtableRandom.Create(12345UL),
            Cna1979LandSequence.CreateTurn(setup.InitialGameTurn)[0]);
        var createdSnapshot = CampaignProjector.Apply(null, created, context);
        var stageEntry = StageEntryCampaignTestData.Advance(
            createdSnapshot,
            context,
            InitiativeOrderChoice.ActLast);
        var snapshot = stageEntry.Snapshot;
        var actingSide = FirstActingSideResolver.Resolve(snapshot);
        var completed = CampaignEngine.Decide(
            snapshot,
            new CompleteReserveDesignation(
                snapshot.StateVersion,
                snapshot.SequencePosition.PositionId,
                actingSide),
            context);
        var completion = Assert.Single(completed.Events);
        var movement = CampaignProjector.Apply(snapshot, completion, context);

        return new CampaignMovementEvidence(
            [created, .. stageEntry.Events, completion],
            movement,
            context,
            actingSide);
    }
}
