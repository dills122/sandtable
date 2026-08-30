using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignReserveEndToEndTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(1, false)]
    public void BothSetupsDesignateAllAndReachReplayIdenticalMovement(
        int setupIndex,
        bool actFirst)
    {
        var evidence = CampaignReserveActionTestData.ExecuteToReserve(
            setupIndex,
            actFirst
                ? InitiativeOrderChoice.ActFirst
                : InitiativeOrderChoice.ActLast);
        var initialReserve = evidence.Snapshot;
        var initialBytes = CampaignSnapshotSerializer.Serialize(initialReserve);
        var snapshot = initialReserve;
        var firstSide = FirstActingSideResolver.Resolve(snapshot);
        var audience = CampaignReserveActionTestData.ToAudience(firstSide);
        var accepted = new List<CampaignEvent>();

        while (true)
        {
            var set = Query(snapshot, evidence.Context, audience);
            var designation = set.Candidates.OfType<DesignateReserveAction>()
                .OrderBy(value => value.ElementId, StringComparer.Ordinal)
                .FirstOrDefault();
            if (designation is null)
            {
                var completion = Assert.Single(
                    set.Candidates.OfType<CompleteReserveDesignationAction>());
                var completed = Execute(snapshot, evidence.Context, set, completion);
                accepted.Add(completed.AcceptedEvent!);
                snapshot = completed.SuccessorSnapshot!;
                break;
            }

            var designated = Execute(snapshot, evidence.Context, set, designation);
            accepted.Add(designated.AcceptedEvent!);
            snapshot = designated.SuccessorSnapshot!;
        }

        Assert.Equal(13, snapshot.StateVersion);
        Assert.Equal(LandPhaseIds.MovementAndCombat, snapshot.PhaseId);
        Assert.Equal(LandSegmentIds.Movement, snapshot.SegmentId);
        Assert.Equal(LandActorRole.FirstActingSide,
            snapshot.SequencePosition.ActorRole);
        Assert.Null(snapshot.ActiveSide);
        Assert.True(CampaignSnapshotValidator.IsValid(snapshot, evidence.Context));
        Assert.Equal(snapshot, CampaignSnapshotSerializer.Deserialize(
            CampaignSnapshotSerializer.Serialize(snapshot)));
        Assert.Equal(2, accepted.OfType<ReserveElementDesignated>().Count());
        Assert.Single(accepted.OfType<ReserveDesignationCompleted>());

        var firstSideId = FormatSide(firstSide);
        var elementsById = evidence.Context.Artifact.Definition.Elements
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        Assert.All(snapshot.World.Elements, element => Assert.Equal(
            string.Equals(
                elementsById[element.ElementId].SideId,
                firstSideId,
                StringComparison.Ordinal)
                ? CampaignElementReserveStatus.ReserveI
                : CampaignElementReserveStatus.None,
            element.ReserveStatus));

        var replayed = CampaignProjector.Replay(
            evidence.Events.Concat(accepted),
            evidence.Context);
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(snapshot),
            CampaignSnapshotSerializer.Serialize(replayed));
        Assert.Equal(initialBytes,
            CampaignSnapshotSerializer.Serialize(initialReserve));

        var movement = Query(snapshot, evidence.Context, audience);
        Assert.Single(movement.Candidates.OfType<CompleteMovementSegmentAction>());
        Assert.Empty(movement.Candidates.OfType<MoveElementAction>());
        Assert.Empty(Query(
            snapshot,
            evidence.Context,
            audience == CampaignActionAudience.Axis
                ? CampaignActionAudience.Commonwealth
                : CampaignActionAudience.Axis).Candidates);
        Assert.Empty(Query(
            snapshot,
            evidence.Context,
            CampaignActionAudience.System).Candidates);

        var generic = CampaignEngine.Decide(
            snapshot,
            new CompleteCurrentSequenceStep(
                snapshot.StateVersion,
                snapshot.SequencePosition.PositionId),
            evidence.Context);
        Assert.False(generic.IsAccepted);
        Assert.Equal(CampaignCommandRejectionReason.UnsupportedTransition,
            generic.RejectionReason);
        Assert.Empty(generic.Events);
    }

    private static CampaignActionExecutionResult Execute(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        CampaignLegalActionSet set,
        CampaignActionCandidate candidate)
    {
        var execution = CampaignActionExecution.Execute(
            snapshot,
            context,
            CampaignReserveActionTestData.Bind(set, candidate));
        Assert.True(execution.IsAccepted);
        Assert.NotNull(execution.AcceptedEvent);
        Assert.NotNull(execution.SuccessorSnapshot);
        Assert.NotNull(execution.Receipt);
        return execution;
    }

    private static CampaignLegalActionSet Query(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        CampaignActionAudience audience)
    {
        var result = CampaignLegalActions.Query(
            new CampaignAuthorityHandle(snapshot, context),
            audience);
        Assert.True(result.IsSuccessful);
        return result.ActionSet!;
    }

    private static string FormatSide(LandSide side) => side switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };
}
