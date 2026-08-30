using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignMovementCompletionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public void ZeroOneOrManyMovesCompleteToReplayIdenticalBreakdown(int moveCount)
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
            var decision = CampaignEngine.Decide(
                snapshot,
                CampaignMovementTestData.CommandFor(
                    snapshot,
                    evidence.ActingSide,
                    candidate),
                evidence.Context);
            var moved = Assert.IsType<ElementMoved>(Assert.Single(decision.Events));
            snapshot = CampaignProjector.Apply(snapshot, moved, evidence.Context);
            events.Add(moved);
            destination = destination == "north-east" ? "east" : "north-east";
        }

        var beforeCompletion = snapshot;
        var world = snapshot.World;
        var random = snapshot.RandomState;
        var completion = CampaignEngine.Decide(
            snapshot,
            new CompleteMovementSegment(
                snapshot.StateVersion,
                snapshot.SequencePosition.PositionId,
                evidence.ActingSide),
            evidence.Context);
        var completed = Assert.IsType<MovementSegmentCompleted>(
            Assert.Single(completion.Events));
        snapshot = CampaignProjector.Apply(snapshot, completed, evidence.Context);
        events.Add(completed);

        Assert.Equal(beforeCompletion.StateVersion + 1, snapshot.StateVersion);
        Assert.Equal(LandPhaseIds.MovementAndCombat, snapshot.PhaseId);
        Assert.Equal(LandSegmentIds.BreakdownDetermination, snapshot.SegmentId);
        Assert.Equal(
            "land.position.operation-1.first-player.movement-and-combat." +
            "breakdown-determination",
            snapshot.SequencePosition.PositionId);
        Assert.Same(world, snapshot.World);
        Assert.Same(random, snapshot.RandomState);
        Assert.True(CampaignSnapshotValidator.IsValid(snapshot, evidence.Context));

        var replayed = CampaignProjector.Replay(events, evidence.Context);
        var strictReplay = CampaignProjector.Replay(
            events.Select(campaignEvent => CampaignEventSerializer.Deserialize(
                CampaignEventSerializer.Serialize(campaignEvent))),
            evidence.Context);
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(snapshot),
            CampaignSnapshotSerializer.Serialize(replayed));
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(snapshot),
            CampaignSnapshotSerializer.Serialize(strictReplay));

        foreach (var audience in Enum.GetValues<CampaignActionAudience>())
        {
            var query = CampaignLegalActions.Query(
                new CampaignAuthorityHandle(snapshot, evidence.Context),
                audience);
            Assert.True(query.IsSuccessful);
            Assert.Empty(query.ActionSet!.Candidates);
        }
    }

    [Fact]
    public void InvalidRepeatedStaleForgedAndWrongSideCompletionEmitNothing()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var movement = evidence.Snapshot;
        var otherSide = evidence.ActingSide == LandSide.Axis
            ? LandSide.Commonwealth
            : LandSide.Axis;
        var validCommand = new CompleteMovementSegment(
            movement.StateVersion,
            movement.SequencePosition.PositionId,
            evidence.ActingSide);
        var valid = Assert.IsType<MovementSegmentCompleted>(Assert.Single(
            CampaignEngine.Decide(movement, validCommand, evidence.Context).Events));
        var breakdown = CampaignProjector.Apply(movement, valid, evidence.Context);
        var before = CampaignSnapshotSerializer.Serialize(movement);

        CompleteMovementSegment[] invalidCommands =
        [
            validCommand with { ContractVersion = 2 },
            validCommand with { ExpectedStateVersion = movement.StateVersion - 1 },
            validCommand with { ExpectedPositionId = "land.position.wrong" },
            validCommand with { ActingSide = otherSide },
            validCommand with { ActingSide = (LandSide)99 },
        ];
        foreach (var command in invalidCommands)
        {
            var rejected = CampaignEngine.Decide(movement, command, evidence.Context);
            Assert.False(rejected.IsAccepted);
            Assert.Empty(rejected.Events);
        }

        var repeated = CampaignEngine.Decide(
            breakdown,
            new CompleteMovementSegment(
                breakdown.StateVersion,
                breakdown.SequencePosition.PositionId,
                evidence.ActingSide),
            evidence.Context);
        Assert.False(repeated.IsAccepted);
        Assert.Empty(repeated.Events);
        Assert.Throws<InvalidCampaignHistoryException>(() => CampaignProjector.Apply(
            movement,
            valid with { CampaignId = "campaign-forged" },
            evidence.Context));
        Assert.Throws<InvalidCampaignHistoryException>(() => CampaignProjector.Apply(
            breakdown,
            valid,
            evidence.Context));
        Assert.Equal(before, CampaignSnapshotSerializer.Serialize(movement));
    }

    [Fact]
    public void CompletedCheckpointRejectsImpossibleZeroAndPostMoveShapes()
    {
        var evidence = CampaignMovementTestData.ReachMovement();
        var entry = evidence.Snapshot;
        var zeroEvent = Assert.IsType<MovementSegmentCompleted>(Assert.Single(
            CampaignEngine.Decide(
                entry,
                new CompleteMovementSegment(
                    entry.StateVersion,
                    entry.SequencePosition.PositionId,
                    evidence.ActingSide),
                evidence.Context).Events));
        var zeroCompleted = CampaignProjector.Apply(entry, zeroEvent, evidence.Context);

        Assert.True(CampaignSnapshotValidator.IsValid(zeroCompleted, evidence.Context));
        Assert.False(CampaignSnapshotValidator.IsValid(
            zeroCompleted with { StateVersion = entry.StateVersion },
            evidence.Context));
        Assert.False(CampaignSnapshotValidator.IsValid(
            zeroCompleted with { StateVersion = zeroCompleted.StateVersion + 1 },
            evidence.Context));

        var move = CampaignMovementTestData.FindMove(
            entry,
            evidence.Context,
            evidence.ActingSide,
            "commonwealth-element-a",
            "north-east");
        var movedEvent = Assert.IsType<ElementMoved>(Assert.Single(
            CampaignEngine.Decide(
                entry,
                CampaignMovementTestData.CommandFor(
                    entry,
                    evidence.ActingSide,
                    move),
                evidence.Context).Events));
        var moved = CampaignProjector.Apply(entry, movedEvent, evidence.Context);
        var movedCompletion = Assert.IsType<MovementSegmentCompleted>(Assert.Single(
            CampaignEngine.Decide(
                moved,
                new CompleteMovementSegment(
                    moved.StateVersion,
                    moved.SequencePosition.PositionId,
                    evidence.ActingSide),
                evidence.Context).Events));
        var postMoveCompleted = CampaignProjector.Apply(
            moved,
            movedCompletion,
            evidence.Context);

        Assert.True(CampaignSnapshotValidator.IsValid(
            postMoveCompleted,
            evidence.Context));
        Assert.False(CampaignSnapshotValidator.IsValid(
            postMoveCompleted with { World = entry.World },
            evidence.Context));
        Assert.False(CampaignSnapshotValidator.IsValid(
            postMoveCompleted with
            {
                SequencePosition = Cna1979LandSequence.GetNext(
                    postMoveCompleted.SequencePosition),
            },
            evidence.Context));
    }
}
