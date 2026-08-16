using Cna.Core.Campaigns;
using Cna.Core.Randomness;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignTests
{
    [Fact]
    public void CreateCommandEmitsTheInitialAuthoritativeEvent()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var command = CampaignTestHarness.Create(
            "campaign-1",
            Cna1979Ruleset.Manifest.Hash,
            12345,
            setup.SetupId,
            setup.Hash);

        var result = CampaignTestHarness.Decide(null, command);

        Assert.True(result.IsAccepted);
        var created = Assert.IsType<CampaignCreated>(Assert.Single(result.Events));
        Assert.Equal(1, created.StateVersion);

        var snapshot = CampaignTestHarness.Replay(result.Events);
        Assert.Equal("campaign-1", snapshot.CampaignId);
        Assert.Equal(Cna1979Ruleset.Manifest.Hash, snapshot.RulesetHash);
        Assert.Equal(12345UL, snapshot.RandomState.Seed);
        Assert.Equal(setup.SetupId, snapshot.Setup.SetupId);
        Assert.Null(snapshot.InitiativeHolder);
        Assert.Equal(1, snapshot.GameTurn);
        Assert.Equal(0, snapshot.OperationStage);
        Assert.Null(snapshot.ActiveSide);
        Assert.Equal(LandPhaseIds.InitiativeDetermination, snapshot.PhaseId);
    }

    [Fact]
    public void CreateCommandRejectsANonCanonicalRulesetHash()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var result = CampaignTestHarness.Decide(
            null,
            CampaignTestHarness.Create(
                "campaign-1",
                "ruleset-hash",
                12345,
                setup.SetupId,
                setup.Hash));

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

        var result = CampaignTestHarness.Decide(snapshot, command);

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

        var result = CampaignTestHarness.Decide(
            snapshot,
            new CompleteCurrentSequenceStep(
                snapshot.StateVersion,
                snapshot.SequencePosition.PositionId));

        Assert.False(result.IsAccepted);
        Assert.Equal(CampaignCommandRejectionReason.UnsupportedTransition, result.RejectionReason);
        Assert.Empty(result.Events);
        Assert.Equal(LandPhaseIds.InitiativeDetermination, snapshot.PhaseId);
        Assert.Equal(1, snapshot.StateVersion);
    }

    [Theory]
    [MemberData(nameof(InvalidSnapshots))]
    public void InvalidAuthoritativeSnapshotCannotProduceAnEvent(CampaignSnapshot snapshot)
    {
        var result = CampaignTestHarness.Decide(
            snapshot,
            new CompleteCurrentSequenceStep(
                snapshot.StateVersion,
                snapshot.SequencePosition?.PositionId
                    ?? "land.position.initiative-determination"));

        Assert.False(result.IsAccepted);
        Assert.Equal(CampaignCommandRejectionReason.InvalidState, result.RejectionReason);
        Assert.Empty(result.Events);
    }

    public static TheoryData<CampaignSnapshot> InvalidSnapshots()
    {
        var valid = CreateSnapshot();
        var position = valid.SequencePosition;
        var wrongContractPosition = new LandSequencePosition(
            Cna1979LandSequence.ContractVersion + 1,
            position.PositionId,
            position.GameTurn,
            position.OperationStage,
            position.StageId,
            position.PhaseId,
            position.SegmentId,
            position.StepId,
            position.ActorRole,
            position.ActiveSide,
            position.Sources);
        var wrongSetup = new CampaignSetupSnapshot(
            valid.Setup.SchemaVersion,
            valid.Setup.SetupId,
            "sha256:wrong",
            valid.Setup.IsSynthetic,
            valid.Setup.InitialGameTurn,
            valid.Setup.InitialInitiative,
            valid.Setup.Content,
            valid.Setup.Sources);

        return new TheoryData<CampaignSnapshot>
        {
            valid with { ContractVersion = 1 },
            valid with { CampaignId = " " },
            valid with { StateVersion = 0 },
            valid with { RulesetHash = " " },
            valid with { RulesetHash = "ruleset-hash" },
            valid with { Setup = wrongSetup },
            valid with { InitiativeHolder = LandSide.Axis },
            valid with { RandomState = new RandomStreamState(2, SandtableRandom.AlgorithmId, 12345, 0) },
            valid with { RandomState = new RandomStreamState(1, "unknown", 12345, 0) },
            valid with { RandomState = new RandomStreamState(1, SandtableRandom.AlgorithmId, 12345, 1) },
            valid with { SequencePosition = null! },
            valid with { SequencePosition = wrongContractPosition },
            valid with { SequencePosition = Cna1979LandSequence.GetNext(position) },
        };
    }

    private static CampaignSnapshot CreateSnapshot()
    {
        var setup = Cna1979SetupCatalog.Definitions[0];
        var result = CampaignTestHarness.Decide(
            null,
            CampaignTestHarness.Create(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                12345,
                setup.SetupId,
                setup.Hash));

        return CampaignTestHarness.Replay(result.Events);
    }
}
