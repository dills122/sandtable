using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignReactionIdentityTests
{
    [Fact]
    public void MovementEndedAndReactingPositionBindExactSuspendedMovementIdentity()
    {
        var movement = MaterializedMovement();

        var ended = new CampaignMovementEndedState(movement);
        var reacting = new CampaignReactingPosition(movement);

        Assert.Equal(movement.ContractVersion, ended.SequenceContractVersion);
        Assert.Equal(movement.PositionId, ended.PositionId);
        Assert.Equal((movement.GameTurn, movement.OperationStage),
            (ended.GameTurn, ended.OperationStage));
        Assert.Equal(LandStageIds.Operation, ended.StageId);
        Assert.Equal(LandPhaseIds.MovementAndCombat, ended.PhaseId);
        Assert.Equal(LandSegmentIds.Movement, ended.SegmentId);
        Assert.Equal(LandSide.Axis, reacting.PhasingSide);
        Assert.Equal(LandSide.Commonwealth, reacting.ReactingSide);
        Assert.Equal(movement, reacting.SuspendedMovementPosition);
        Assert.DoesNotContain("ReactingSide", Enum.GetNames<LandActorRole>());
    }

    [Fact]
    public void MovementScopedValuesRejectUnmaterializedOrNonMovementPositions()
    {
        var movement = Cna1979LandSequence.CreateTurn(1).Single(position =>
            position.OperationStage == 1
            && position.ActorRole == LandActorRole.FirstActingSide
            && position.SegmentId == LandSegmentIds.Movement);
        var reserve = Cna1979LandSequence.CreateTurn(1).Single(position =>
            position.OperationStage == 1
            && position.ActorRole == LandActorRole.FirstActingSide
            && position.PhaseId == LandPhaseIds.ReserveDesignation);

        Assert.Throws<ArgumentException>(() => new CampaignReactingPosition(movement));
        Assert.Throws<ArgumentException>(() => new CampaignMovementEndedState(reserve));
    }

    [Fact]
    public void OperationalStateRejectsMovementEndedFromAnotherLedgerScope()
    {
        var ended = new CampaignMovementEndedState(MaterializedMovement(2, 2));

        Assert.Throws<ArgumentException>(() => new CampaignElementOperationalStateV5(
            1,
            1,
            CapabilityPointAmount.Zero,
            0,
            null,
            ended));
    }

    [Fact]
    public void WindowAndOpportunityIdsAreCanonicalDomainSeparatedAndSemantic()
    {
        var trigger = Representation(
            "map-representation.0001",
            "east",
            "axis-battalion-alpha");
        var equivalentTrigger = Representation(
            "map-representation.0001",
            "east",
            "axis-battalion-alpha");
        var reacting = Representation(
            "map-representation.0002",
            "east",
            "commonwealth-brigade-alpha");
        var baseline = CampaignReactionIdentity.CreateWindow(
            "campaign-1",
            Cna1979Ruleset.Manifest.Hash,
            2,
            42,
            trigger,
            "west",
            "east",
            LandSide.Commonwealth);
        var equivalent = CampaignReactionIdentity.CreateWindow(
            "campaign-1",
            Cna1979Ruleset.Manifest.Hash,
            2,
            42,
            equivalentTrigger,
            "west",
            "east",
            LandSide.Commonwealth);
        var opportunity = CampaignReactionIdentity.CreateOpportunity(baseline, reacting);

        Assert.Equal(baseline, equivalent);
        Assert.Equal(
            "sha256:a16e4c2448e0253bfcb8fe2540362300c1ac9b1a90993d5f5ac883105586d534",
            baseline.Value);
        Assert.Equal(
            "sha256:96cc532a66bbd01ba9b5277361753a65e28e020d8b59aff98a46217accb4a6c2",
            opportunity.Value);
        Assert.NotEqual(baseline.Value, opportunity.Value);
        Assert.NotEqual(baseline, CampaignReactionIdentity.CreateWindow(
            "campaign-2", Cna1979Ruleset.Manifest.Hash, 2, 42, trigger,
            "west", "east", LandSide.Commonwealth));
        Assert.NotEqual(baseline, CampaignReactionIdentity.CreateWindow(
            "campaign-1", Cna1979Ruleset.Manifest.Hash, 3, 42, trigger,
            "west", "east", LandSide.Commonwealth));
        Assert.NotEqual(baseline, CampaignReactionIdentity.CreateWindow(
            "campaign-1", Cna1979Ruleset.Manifest.Hash, 2, 43, trigger,
            "west", "east", LandSide.Commonwealth));
        Assert.NotEqual(baseline, CampaignReactionIdentity.CreateWindow(
            "campaign-1", Cna1979Ruleset.Manifest.Hash, 2, 42,
            Representation("map-representation.0003", "east", "axis-battalion-alpha"),
            "west", "east", LandSide.Commonwealth));
        Assert.NotEqual(baseline, CampaignReactionIdentity.CreateWindow(
            "campaign-1", Cna1979Ruleset.Manifest.Hash, 2, 42, trigger,
            "north-west", "east", LandSide.Commonwealth));
        Assert.NotEqual(baseline, CampaignReactionIdentity.CreateWindow(
            "campaign-1", Cna1979Ruleset.Manifest.Hash, 2, 42,
            Representation("map-representation.0001", "north-east",
                "axis-battalion-alpha"),
            "west", "north-east", LandSide.Commonwealth));
        Assert.NotEqual(baseline, CampaignReactionIdentity.CreateWindow(
            "campaign-1", Cna1979Ruleset.Manifest.Hash, 2, 42, trigger,
            "west", "east", LandSide.Axis));
        Assert.NotEqual(opportunity,
            CampaignReactionIdentity.CreateOpportunity(
                baseline,
                Representation("map-representation.0004", "east",
                    "commonwealth-brigade-alpha")));
    }

    private static LandSequencePosition MaterializedMovement(
        int gameTurn = 1,
        int operationStage = 1)
    {
        var position = Cna1979LandSequence.CreateTurn(gameTurn).Single(candidate =>
            candidate.OperationStage == operationStage
            && candidate.ActorRole == LandActorRole.FirstActingSide
            && candidate.SegmentId == LandSegmentIds.Movement);
        return new LandSequencePosition(
            position.ContractVersion,
            position.PositionId,
            position.GameTurn,
            position.OperationStage,
            position.StageId,
            position.PhaseId,
            position.SegmentId,
            position.StepId,
            position.ActorRole,
            LandSide.Axis,
            position.Sources);
    }

    private static CampaignMapRepresentationState Representation(
        string id,
        string location,
        params string[] elementIds) => new(
            id,
            location,
            CampaignMapRepresentationBindingKind.IndependentElement,
            elementIds);
}
