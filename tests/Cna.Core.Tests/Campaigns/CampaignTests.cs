using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignTests
{
    [Fact]
    public void CreateCommandEmitsTheInitialAuthoritativeEvent()
    {
        var command = new CreateCampaign(
            "campaign-1",
            "ruleset-hash",
            12345,
            LandSide.Axis);

        var result = CampaignEngine.Decide(null, command);

        Assert.True(result.IsAccepted);
        var created = Assert.IsType<CampaignCreated>(Assert.Single(result.Events));
        Assert.Equal(1, created.StateVersion);

        var snapshot = CampaignProjector.Replay(result.Events);
        Assert.Equal("campaign-1", snapshot.CampaignId);
        Assert.Equal("ruleset-hash", snapshot.RulesetHash);
        Assert.Equal(12345UL, snapshot.Seed);
        Assert.Equal(1, snapshot.GameTurn);
        Assert.Equal(0, snapshot.OperationStage);
        Assert.Null(snapshot.ActiveSide);
        Assert.Equal(LandPhaseIds.InitiativeDetermination, snapshot.PhaseId);
    }

    [Theory]
    [InlineData(0, "land.position.initiative-determination", CampaignCommandRejectionReason.StaleState)]
    [InlineData(1, "land.position.wrong", CampaignCommandRejectionReason.UnexpectedSequenceStep)]
    public void IllegalAdvanceIsRejectedWithoutChangingTheSnapshot(
        long expectedStateVersion,
        string expectedPositionId,
        CampaignCommandRejectionReason expectedReason)
    {
        var snapshot = CreateSnapshot();
        var command = new CompleteCurrentSequenceStep(expectedStateVersion, expectedPositionId);

        var result = CampaignEngine.Decide(snapshot, command);

        Assert.False(result.IsAccepted);
        Assert.Equal(expectedReason, result.RejectionReason);
        Assert.Empty(result.Events);
        Assert.Equal(1, snapshot.StateVersion);
        Assert.Equal(LandPhaseIds.InitiativeDetermination, snapshot.PhaseId);
    }

    [Fact]
    public void LegalCommandsReachTheFirstPlayersMovementSegment()
    {
        var history = new List<CampaignEvent>();
        var createResult = CampaignEngine.Decide(
            null,
            new CreateCampaign("campaign-1", "ruleset-hash", 12345, LandSide.Axis));
        history.AddRange(createResult.Events);
        var snapshot = CampaignProjector.Replay(history);

        while (snapshot.PhaseId != LandPhaseIds.MovementAndCombat
            || snapshot.SegmentId != LandSegmentIds.Movement)
        {
            var command = new CompleteCurrentSequenceStep(
                snapshot.StateVersion,
                snapshot.SequencePosition.PositionId);
            var result = CampaignEngine.Decide(snapshot, command);

            Assert.True(result.IsAccepted);
            history.AddRange(result.Events);
            snapshot = CampaignProjector.Replay(history);
        }

        Assert.Equal(1, snapshot.GameTurn);
        Assert.Equal(1, snapshot.OperationStage);
        Assert.Equal(LandSide.Axis, snapshot.ActiveSide);
        Assert.Equal(history.Count, snapshot.StateVersion);
    }

    [Fact]
    public void AdvancingBeyondTheImplementedMovementBoundaryIsRejected()
    {
        var history = new List<CampaignEvent>();
        var createResult = CampaignEngine.Decide(
            null,
            new CreateCampaign("campaign-1", "ruleset-hash", 12345, LandSide.Axis));
        history.AddRange(createResult.Events);
        var snapshot = CampaignProjector.Replay(history);

        while (snapshot.SegmentId != LandSegmentIds.Movement)
        {
            var result = CampaignEngine.Decide(
                snapshot,
                new CompleteCurrentSequenceStep(
                    snapshot.StateVersion,
                    snapshot.SequencePosition.PositionId));
            Assert.True(result.IsAccepted);
            history.AddRange(result.Events);
            snapshot = CampaignProjector.Replay(history);
        }

        var unsupported = CampaignEngine.Decide(
            snapshot,
            new CompleteCurrentSequenceStep(
                snapshot.StateVersion,
                snapshot.SequencePosition.PositionId));

        Assert.False(unsupported.IsAccepted);
        Assert.Equal(
            CampaignCommandRejectionReason.UnsupportedTransition,
            unsupported.RejectionReason);
        Assert.Empty(unsupported.Events);
    }

    private static CampaignSnapshot CreateSnapshot()
    {
        var result = CampaignEngine.Decide(
            null,
            new CreateCampaign("campaign-1", "ruleset-hash", 12345, LandSide.Axis));

        return CampaignProjector.Replay(result.Events);
    }
}
