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
            Cna1979Ruleset.Manifest.Hash,
            12345,
            LandSide.Axis);

        var result = CampaignEngine.Decide(null, command);

        Assert.True(result.IsAccepted);
        var created = Assert.IsType<CampaignCreated>(Assert.Single(result.Events));
        Assert.Equal(1, created.StateVersion);

        var snapshot = CampaignProjector.Replay(result.Events);
        Assert.Equal("campaign-1", snapshot.CampaignId);
        Assert.Equal(Cna1979Ruleset.Manifest.Hash, snapshot.RulesetHash);
        Assert.Equal(12345UL, snapshot.Seed);
        Assert.Equal(1, snapshot.GameTurn);
        Assert.Equal(0, snapshot.OperationStage);
        Assert.Null(snapshot.ActiveSide);
        Assert.Equal(LandPhaseIds.InitiativeDetermination, snapshot.PhaseId);
    }

    [Fact]
    public void CreateCommandRejectsANonCanonicalRulesetHash()
    {
        var result = CampaignEngine.Decide(
            null,
            new CreateCampaign("campaign-1", "ruleset-hash", 12345, LandSide.Axis));

        Assert.False(result.IsAccepted);
        Assert.Equal(CampaignCommandRejectionReason.InvalidCommand, result.RejectionReason);
        Assert.Empty(result.Events);
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
    public void MandatoryUnimplementedInitiativeDeterminationRejectsWithoutAnEvent()
    {
        var snapshot = CreateSnapshot();

        var result = CampaignEngine.Decide(
            snapshot,
            new CompleteCurrentSequenceStep(
                snapshot.StateVersion,
                snapshot.SequencePosition.PositionId));

        Assert.False(result.IsAccepted);
        Assert.Equal(
            CampaignCommandRejectionReason.UnsupportedTransition,
            result.RejectionReason);
        Assert.Empty(result.Events);
        Assert.Equal(LandPhaseIds.InitiativeDetermination, snapshot.PhaseId);
        Assert.Equal(1, snapshot.StateVersion);
    }

    [Theory]
    [MemberData(nameof(InvalidSnapshots))]
    public void InvalidAuthoritativeSnapshotCannotProduceAnEvent(CampaignSnapshot snapshot)
    {
        var result = CampaignEngine.Decide(
            snapshot,
            new CompleteCurrentSequenceStep(
                snapshot.StateVersion,
                snapshot.SequencePosition?.PositionId ?? "land.position.initiative-determination"));

        Assert.False(result.IsAccepted);
        Assert.Equal(CampaignCommandRejectionReason.InvalidState, result.RejectionReason);
        Assert.Empty(result.Events);
    }

    public static TheoryData<CampaignSnapshot> InvalidSnapshots()
    {
        var valid = CreateSnapshot();
        var validPosition = valid.SequencePosition;
        var positionForOtherFirstPlayer = Cna1979LandSequence.CreateTurn(1, LandSide.Commonwealth)
            .First(position => position.ActiveSide == LandSide.Commonwealth);
        var wrongContractPosition = new LandSequencePosition(
            Cna1979LandSequence.ContractVersion + 1,
            validPosition.PositionId,
            validPosition.GameTurn,
            validPosition.OperationStage,
            validPosition.StageId,
            validPosition.PhaseId,
            validPosition.SegmentId,
            validPosition.StepId,
            validPosition.Source,
            validPosition.ActiveSide);

        return new TheoryData<CampaignSnapshot>
        {
            valid with { ContractVersion = 2 },
            valid with { CampaignId = " " },
            valid with { StateVersion = 0 },
            valid with { RulesetHash = " " },
            valid with { RulesetHash = "ruleset-hash" },
            valid with { FirstPlayer = (LandSide)999 },
            valid with { SequencePosition = null! },
            valid with { SequencePosition = wrongContractPosition },
            valid with { SequencePosition = positionForOtherFirstPlayer },
        };
    }

    private static CampaignSnapshot CreateSnapshot()
    {
        var result = CampaignEngine.Decide(
            null,
            new CreateCampaign(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                LandSide.Axis));

        return CampaignProjector.Replay(result.Events);
    }
}
