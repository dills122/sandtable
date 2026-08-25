using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Observations;

public sealed class CampaignObservationProjectionTests
{
    [Fact]
    public void AxisProjectionContainsPublicStateTopologyAndOnlyAxisElements()
    {
        var snapshot = CreateSnapshot();

        var result = CampaignObservationProjector.Project(
            snapshot,
            CampaignTestHarness.ContextFor(snapshot),
            LandSide.Axis);

        Assert.True(result.IsProjected);
        Assert.Equal(CampaignObservationRejectionReason.None, result.RejectionReason);
        var observation = Assert.IsType<CampaignObservation>(result.Observation);
        Assert.Equal(4, observation.ContractVersion);
        Assert.Equal("sandtable.observation.own-elements-only.v2", observation.PolicyId);
        Assert.Equal(snapshot.CampaignId, observation.CampaignId);
        Assert.Equal(snapshot.StateVersion, observation.StateVersion);
        Assert.Equal(Cna1979Ruleset.Manifest.Hash, observation.RulesetHash);
        Assert.Equal(snapshot.Setup.Content.ScenarioId, observation.ScenarioId);
        Assert.Equal(LandSide.Axis, observation.Observer);
        Assert.Equal(9, observation.Locations.Count);
        Assert.Equal(10, observation.Edges.Count);
        Assert.Equal(
            ["axis-element-a", "axis-element-b"],
            observation.OwnElements.Select(element => element.ElementId));
        Assert.Equal(["west", "north-west"], observation.OwnElements.Select(
            element => element.CurrentLocationId));
        Assert.All(observation.OwnElements, element =>
            Assert.Equal(CampaignObservationReserveStatus.None, element.ReserveStatus));
        Assert.DoesNotContain(
            observation.OwnElements,
            element => element.ElementId.StartsWith("commonwealth", StringComparison.Ordinal));
    }

    [Fact]
    public void CommonwealthProjectionContainsOnlyCommonwealthElements()
    {
        var snapshot = CreateSnapshot();

        var result = CampaignObservationProjector.Project(
            snapshot,
            CampaignTestHarness.ContextFor(snapshot),
            LandSide.Commonwealth);

        var observation = Assert.IsType<CampaignObservation>(result.Observation);
        Assert.Equal(
            ["commonwealth-element-a", "commonwealth-element-b"],
            observation.OwnElements.Select(element => element.ElementId));
        Assert.Equal(["east", "south-east"], observation.OwnElements.Select(
            element => element.CurrentLocationId));
    }

    [Fact]
    public void ResolvedInitiativeProjectionCopiesThePublicTurnCheckpoint()
    {
        var initial = CreateSnapshot(Cna1979SetupCatalog.Definitions[1], 7);
        var accepted = CampaignTestHarness.Decide(
            initial,
            new ResolveInitiative(initial.StateVersion, initial.SequencePosition.PositionId));
        var resolved = CampaignTestHarness.Apply(initial, Assert.Single(accepted.Events));

        var result = CampaignObservationProjector.Project(
            resolved,
            CampaignTestHarness.ContextFor(resolved),
            LandSide.Axis);

        var observation = Assert.IsType<CampaignObservation>(result.Observation);
        Assert.Equal(2, observation.StateVersion);
        Assert.Equal(LandSide.Commonwealth, observation.Position.InitiativeHolder);
        Assert.Equal(LandStageIds.NavalConvoy, observation.Position.StageId);
        Assert.Equal(LandPhaseIds.NavalConvoySchedule, observation.Position.PhaseId);
        Assert.Equal(0, observation.Position.OperationStage);
        Assert.Null(observation.Position.ActiveSide);
    }

    [Fact]
    public void InvalidObserverPrecedesInvalidAuthorityAndReturnsNoPartialObservation()
    {
        var snapshot = CreateSnapshot() with { RulesetHash = "invalid" };

        var result = CampaignObservationProjector.Project(
            snapshot,
            CampaignTestHarness.ContextFor(snapshot),
            (LandSide)99);

        Assert.False(result.IsProjected);
        Assert.Null(result.Observation);
        Assert.Equal(CampaignObservationRejectionReason.InvalidObserver, result.RejectionReason);
    }

    [Fact]
    public void MismatchedContextAndForgedCheckpointReturnInvalidState()
    {
        var snapshot = CreateSnapshot();
        var mismatched = CampaignContentContext.Create(
            Cna1979SyntheticContentCatalog.Artifact,
            "initiative-contested-lab");
        var forged = snapshot with { StateVersion = 99 };

        var mismatchResult = CampaignObservationProjector.Project(
            snapshot,
            mismatched,
            LandSide.Axis);
        var forgedResult = CampaignObservationProjector.Project(
            forged,
            CampaignTestHarness.ContextFor(snapshot),
            LandSide.Axis);

        Assert.Equal(CampaignObservationRejectionReason.InvalidState, mismatchResult.RejectionReason);
        Assert.Null(mismatchResult.Observation);
        Assert.Equal(CampaignObservationRejectionReason.InvalidState, forgedResult.RejectionReason);
        Assert.Null(forgedResult.Observation);
    }

    [Fact]
    public void ProjectionRejectsNullInputsAndDoesNotMutateAuthority()
    {
        var snapshot = CreateSnapshot();
        var context = CampaignTestHarness.ContextFor(snapshot);
        var before = CampaignSnapshotSerializer.Serialize(snapshot);
        var contentBefore = context.Artifact.GetCanonicalBytes();

        Assert.Throws<ArgumentNullException>(() => CampaignObservationProjector.Project(
            null!,
            context,
            LandSide.Axis));
        Assert.Throws<ArgumentNullException>(() => CampaignObservationProjector.Project(
            snapshot,
            null!,
            LandSide.Axis));

        var first = CampaignObservationProjector.Project(snapshot, context, LandSide.Axis);
        var second = CampaignObservationProjector.Project(snapshot, context, LandSide.Axis);

        Assert.Equal(first.Observation, second.Observation);
        Assert.Equal(before, CampaignSnapshotSerializer.Serialize(snapshot));
        Assert.Equal(contentBefore, context.Artifact.GetCanonicalBytes());
        Assert.Equal(0UL, snapshot.RandomState.NextByteCursor);
    }

    private static CampaignSnapshot CreateSnapshot() =>
        CreateSnapshot(Cna1979SetupCatalog.Definitions[0], 12345);

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
