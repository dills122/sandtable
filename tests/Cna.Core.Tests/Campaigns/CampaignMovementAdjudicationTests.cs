using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

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
    public void PublicMovementActionsRemainDormantAfterInternalMoves()
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

        foreach (var audience in Enum.GetValues<CampaignActionAudience>())
        {
            var result = CampaignLegalActions.Query(
                new CampaignAuthorityHandle(moved, evidence.Context),
                audience);

            Assert.True(result.IsSuccessful);
            Assert.Empty(result.ActionSet!.Candidates);
        }
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
}
