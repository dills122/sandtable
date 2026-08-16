using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Campaigns;

public sealed class InitiativeCampaignTests
{
    [Theory]
    [InlineData(0, LandSide.Commonwealth, 2)]
    [InlineData(7, LandSide.Commonwealth, 5)]
    public void ResolveInitiativeEmitsOneEventAndStopsAtNavalConvoy(
        ulong seed,
        LandSide expectedHolder,
        ulong expectedCursor)
    {
        var snapshot = CreateSnapshot(Cna1979SetupCatalog.Definitions[1], seed);
        var command = new ResolveInitiative(
            snapshot.StateVersion,
            snapshot.SequencePosition.PositionId);

        var result = CampaignTestHarness.Decide(snapshot, command);

        Assert.True(result.IsAccepted);
        var determined = Assert.IsType<InitiativeDetermined>(Assert.Single(result.Events));
        Assert.Equal(2, determined.StateVersion);
        Assert.Equal(expectedCursor, determined.RandomCursorAfter);
        Assert.Equal(expectedHolder, determined.Outcome.Holder);

        var projected = CampaignTestHarness.Apply(snapshot, determined);
        Assert.Equal(2, projected.StateVersion);
        Assert.Equal(expectedHolder, projected.InitiativeHolder);
        Assert.Equal(expectedCursor, projected.RandomState.NextByteCursor);
        Assert.Equal(LandStageIds.NavalConvoy, projected.SequencePosition.StageId);
        Assert.Equal(LandPhaseIds.NavalConvoySchedule, projected.PhaseId);
        Assert.Equal(LandActorRole.None, projected.SequencePosition.ActorRole);
        Assert.Null(projected.ActiveSide);
        Assert.Same(snapshot.World, projected.World);
    }

    [Fact]
    public void PredeterminedResolutionHasNoRoundsAndLeavesCursorUnchanged()
    {
        var snapshot = CreateSnapshot(Cna1979SetupCatalog.Definitions[0], 12345);

        var result = CampaignTestHarness.Decide(
            snapshot,
            new ResolveInitiative(snapshot.StateVersion, snapshot.SequencePosition.PositionId));

        var determined = Assert.IsType<InitiativeDetermined>(Assert.Single(result.Events));
        var outcome = Assert.IsType<PredeterminedInitiativeOutcome>(determined.Outcome);
        Assert.Equal(LandSide.Axis, outcome.Holder);
        Assert.Equal(0UL, determined.RandomCursorBefore);
        Assert.Equal(0UL, determined.RandomCursorAfter);
    }

    [Theory]
    [InlineData(0, "land.position.initiative-determination", CampaignCommandRejectionReason.StaleState)]
    [InlineData(1, "land.position.wrong", CampaignCommandRejectionReason.UnexpectedSequenceStep)]
    public void ResolveRejectsStaleAndWrongPositionWithoutEvents(
        long expectedVersion,
        string expectedPosition,
        CampaignCommandRejectionReason expectedReason)
    {
        var snapshot = CreateSnapshot(Cna1979SetupCatalog.Definitions[0], 12345);

        var result = CampaignTestHarness.Decide(
            snapshot,
            new ResolveInitiative(expectedVersion, expectedPosition));

        Assert.False(result.IsAccepted);
        Assert.Equal(expectedReason, result.RejectionReason);
        Assert.Empty(result.Events);
        Assert.Equal(0UL, snapshot.RandomState.NextByteCursor);
    }

    [Fact]
    public void ResolveValidationKeepsNullCampaignAndMalformedCommandPrecedence()
    {
        var missing = CampaignTestHarness.Decide(null, new ResolveInitiative(99, " "));
        var snapshot = CreateSnapshot(Cna1979SetupCatalog.Definitions[0], 12345);
        var malformed = CampaignTestHarness.Decide(
            snapshot,
            new ResolveInitiative(99, " "));

        Assert.Equal(CampaignCommandRejectionReason.CampaignNotCreated, missing.RejectionReason);
        Assert.Equal(CampaignCommandRejectionReason.InvalidCommand, malformed.RejectionReason);
        Assert.Empty(missing.Events);
        Assert.Empty(malformed.Events);
    }

    [Fact]
    public void DuplicateAndGenericAdvanceAtNavalConvoyRemainUnsupported()
    {
        var initial = CreateSnapshot(Cna1979SetupCatalog.Definitions[0], 12345);
        var accepted = CampaignTestHarness.Decide(
            initial,
            new ResolveInitiative(initial.StateVersion, initial.SequencePosition.PositionId));
        var snapshot = CampaignTestHarness.Apply(initial, Assert.Single(accepted.Events));

        var duplicate = CampaignTestHarness.Decide(
            snapshot,
            new ResolveInitiative(snapshot.StateVersion, snapshot.SequencePosition.PositionId));
        var generic = CampaignTestHarness.Decide(
            snapshot,
            new CompleteCurrentSequenceStep(
                snapshot.StateVersion,
                snapshot.SequencePosition.PositionId));

        Assert.Equal(CampaignCommandRejectionReason.UnsupportedTransition, duplicate.RejectionReason);
        Assert.Empty(duplicate.Events);
        Assert.Equal(CampaignCommandRejectionReason.UnsupportedTransition, generic.RejectionReason);
        Assert.Empty(generic.Events);
    }

    [Fact]
    public void ProjectorRejectsAFieldForgedInitiativeEvent()
    {
        var snapshot = CreateSnapshot(Cna1979SetupCatalog.Definitions[1], 0);
        var accepted = CampaignTestHarness.Decide(
            snapshot,
            new ResolveInitiative(snapshot.StateVersion, snapshot.SequencePosition.PositionId));
        var valid = Assert.IsType<InitiativeDetermined>(Assert.Single(accepted.Events));
        var forged = new InitiativeDetermined(
            valid.CampaignId,
            valid.StateVersion,
            valid.FromPositionId,
            valid.Outcome,
            valid.RandomAlgorithmId,
            valid.RandomCursorBefore,
            valid.RandomCursorAfter + 1,
            valid.SequencePosition,
            valid.Sources);

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(snapshot, forged));
    }

    [Fact]
    public void ProjectorRejectsInvalidPriorCheckpointBeforeApplyingInitiative()
    {
        var snapshot = CreateSnapshot(Cna1979SetupCatalog.Definitions[0], 12345);
        var accepted = CampaignTestHarness.Decide(
            snapshot,
            new ResolveInitiative(snapshot.StateVersion, snapshot.SequencePosition.PositionId));
        var determined = Assert.IsType<InitiativeDetermined>(Assert.Single(accepted.Events));
        var invalidCheckpoint = snapshot with { InitiativeHolder = LandSide.Axis };

        Assert.Throws<InvalidCampaignHistoryException>(() =>
            CampaignTestHarness.Apply(invalidCheckpoint, determined));
    }

    [Fact]
    public void ProjectorRejectsEveryAuthoritativeEventFieldForgery()
    {
        var snapshot = CreateSnapshot(Cna1979SetupCatalog.Definitions[1], 0);
        var accepted = CampaignTestHarness.Decide(
            snapshot,
            new ResolveInitiative(snapshot.StateVersion, snapshot.SequencePosition.PositionId));
        var valid = Assert.IsType<InitiativeDetermined>(Assert.Single(accepted.Events));
        var outcome = new ContestedInitiativeOutcome(
            Assert.IsType<ContestedInitiativeOutcome>(valid.Outcome).AxisFacts,
            AxisInitiativePresence.GermanLandCombatUnitOnQualifyingGameMap,
            [new InitiativeRollRound(1, 5, 3, 8, 6, 4, 10)],
            LandSide.Commonwealth);
        InitiativeDetermined[] forgedEvents =
        [
            Copy(valid, campaignId: "campaign-forged"),
            Copy(valid, stateVersion: 3),
            Copy(valid, fromPositionId: "land.position.forged"),
            Copy(valid, outcome: outcome),
            Copy(valid, randomAlgorithmId: "unknown"),
            Copy(valid, randomCursorBefore: 1),
            Copy(valid, randomCursorAfter: 3),
            Copy(valid, sequencePosition: Cna1979LandSequence.CreateTurn(43)[2]),
            Copy(valid, sources: [new RuleReference("forged", "source")]),
        ];

        Assert.All(forgedEvents, forged =>
            Assert.Throws<InvalidCampaignHistoryException>(() =>
                CampaignTestHarness.Apply(snapshot, forged)));
    }

    [Fact]
    public void ResolvedSnapshotRoundTripsCanonically()
    {
        var initial = CreateSnapshot(Cna1979SetupCatalog.Definitions[1], 7);
        var accepted = CampaignTestHarness.Decide(
            initial,
            new ResolveInitiative(initial.StateVersion, initial.SequencePosition.PositionId));
        var resolved = CampaignTestHarness.Apply(initial, Assert.Single(accepted.Events));

        var bytes = CampaignSnapshotSerializer.Serialize(resolved);
        var roundTrip = CampaignSnapshotSerializer.Deserialize(bytes);

        Assert.Equal(resolved, roundTrip);
        Assert.Equal(5UL, roundTrip.RandomState.NextByteCursor);
        Assert.Equal(LandSide.Commonwealth, roundTrip.InitiativeHolder);
    }

    private static InitiativeDetermined Copy(
        InitiativeDetermined value,
        string? campaignId = null,
        long? stateVersion = null,
        string? fromPositionId = null,
        InitiativeOutcome? outcome = null,
        string? randomAlgorithmId = null,
        ulong? randomCursorBefore = null,
        ulong? randomCursorAfter = null,
        LandSequencePosition? sequencePosition = null,
        IReadOnlyList<RuleReference>? sources = null) => new(
            campaignId ?? value.CampaignId,
            stateVersion ?? value.StateVersion,
            fromPositionId ?? value.FromPositionId,
            outcome ?? value.Outcome,
            randomAlgorithmId ?? value.RandomAlgorithmId,
            randomCursorBefore ?? value.RandomCursorBefore,
            randomCursorAfter ?? value.RandomCursorAfter,
            sequencePosition ?? value.SequencePosition,
            sources ?? value.Sources);

    private static CampaignSnapshot CreateSnapshot(CampaignSetupDefinition setup, ulong seed)
    {
        var result = CampaignTestHarness.Decide(
            null,
            CampaignTestHarness.Create(
                "campaign-1",
                Cna1979Ruleset.Manifest.Hash,
                seed,
                setup.SetupId,
                setup.Hash));

        return CampaignTestHarness.Replay(result.Events);
    }
}
