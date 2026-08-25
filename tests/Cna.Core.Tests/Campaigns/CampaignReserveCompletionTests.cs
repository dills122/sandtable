using System.Text;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;
using Cna.Core.Tests.Actions;

namespace Cna.Core.Tests.Campaigns;

public sealed class CampaignReserveCompletionTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void CompletionPreservesWorldAndAdvancesExactlyOnceToMovement(
        int selectedCount)
    {
        var evidence = ReachReserve();
        var snapshot = evidence.Snapshot;
        var actingSide = FirstActingSideResolver.Resolve(snapshot);
        var audience = CampaignReserveActionTestData.ToAudience(actingSide);
        var accepted = new List<CampaignEvent>();

        for (var index = 0; index < selectedCount; index++)
        {
            var designation = Query(snapshot, evidence.Context, audience).Candidates
                .OfType<DesignateReserveAction>()
                .OrderBy(value => value.ElementId, StringComparer.Ordinal)
                .First();
            var execution = Execute(
                snapshot,
                evidence.Context,
                audience,
                designation);
            accepted.Add(execution.AcceptedEvent!);
            snapshot = execution.SuccessorSnapshot!;
        }

        var beforeCompletion = snapshot;
        var beforeBytes = CampaignSnapshotSerializer.Serialize(beforeCompletion);
        var completion = Assert.Single(Query(snapshot, evidence.Context, audience)
            .Candidates.OfType<CompleteReserveDesignationAction>());

        var completed = Execute(
            snapshot,
            evidence.Context,
            audience,
            completion);

        var campaignEvent = Assert.IsType<ReserveDesignationCompleted>(
            completed.AcceptedEvent);
        var successor = completed.SuccessorSnapshot!;
        var expectedPosition = Cna1979LandSequence.GetNext(
            beforeCompletion.SequencePosition);
        var expected = beforeCompletion with
        {
            StateVersion = checked(beforeCompletion.StateVersion + 1),
            SequencePosition = expectedPosition,
        };
        Assert.Equal(11 + selectedCount, successor.StateVersion);
        Assert.Equal(expectedPosition, campaignEvent.SequencePosition);
        Assert.Equal(LandPhaseIds.MovementAndCombat, successor.PhaseId);
        Assert.Equal(LandSegmentIds.Movement, successor.SegmentId);
        Assert.Equal(LandActorRole.FirstActingSide,
            successor.SequencePosition.ActorRole);
        Assert.Null(successor.ActiveSide);
        Assert.Same(beforeCompletion.World, successor.World);
        Assert.Equal(beforeCompletion.World, successor.World);
        Assert.Equal(beforeCompletion.Setup, successor.Setup);
        Assert.Equal(beforeCompletion.RulesetHash, successor.RulesetHash);
        Assert.Equal(beforeCompletion.InitiativeHolder, successor.InitiativeHolder);
        Assert.Equal(beforeCompletion.OperationStageOrders,
            successor.OperationStageOrders);
        Assert.Equal(beforeCompletion.OperationStageWeather,
            successor.OperationStageWeather);
        Assert.Equal(beforeCompletion.RandomState, successor.RandomState);
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(expected),
            CampaignSnapshotSerializer.Serialize(successor));
        Assert.Equal(beforeBytes,
            CampaignSnapshotSerializer.Serialize(beforeCompletion));
        Assert.Equal(beforeCompletion.StateVersion,
            completed.Receipt!.PriorStateVersion);
        Assert.Equal(successor.StateVersion,
            completed.Receipt.CommittedStateVersion);

        accepted.Add(campaignEvent);
        var replayed = CampaignProjector.Replay(
            evidence.Events.Concat(accepted),
            evidence.Context);
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(successor),
            CampaignSnapshotSerializer.Serialize(replayed));

        foreach (var queryAudience in Enum.GetValues<CampaignActionAudience>())
        {
            Assert.Empty(Query(successor, evidence.Context, queryAudience).Candidates);
        }
    }

    [Fact]
    public void InvalidCompletionCommandsAndForgedHistoryFailClosed()
    {
        var evidence = ReachReserve();
        var reserve = evidence.Snapshot;
        var actingSide = FirstActingSideResolver.Resolve(reserve);
        var otherSide = actingSide == LandSide.Axis
            ? LandSide.Commonwealth
            : LandSide.Axis;
        var prior = CampaignProjector.Replay(
            evidence.Events.Take(evidence.Events.Count - 1),
            evidence.Context);
        (CampaignSnapshot Snapshot, CompleteReserveDesignation Command,
            CampaignCommandRejectionReason Reason)[] cases =
        [
            (reserve, new CompleteReserveDesignation(reserve.StateVersion,
                reserve.SequencePosition.PositionId, actingSide)
                with { ContractVersion = 2 },
                CampaignCommandRejectionReason.InvalidCommand),
            (reserve, new CompleteReserveDesignation(reserve.StateVersion - 1,
                reserve.SequencePosition.PositionId, actingSide),
                CampaignCommandRejectionReason.StaleState),
            (reserve, new CompleteReserveDesignation(reserve.StateVersion,
                prior.SequencePosition.PositionId, actingSide),
                CampaignCommandRejectionReason.UnexpectedSequenceStep),
            (reserve, new CompleteReserveDesignation(reserve.StateVersion,
                reserve.SequencePosition.PositionId, (LandSide)99),
                CampaignCommandRejectionReason.InvalidCommand),
            (reserve, new CompleteReserveDesignation(reserve.StateVersion,
                reserve.SequencePosition.PositionId, otherSide),
                CampaignCommandRejectionReason.UnsupportedTransition),
            (prior, new CompleteReserveDesignation(prior.StateVersion,
                prior.SequencePosition.PositionId, actingSide),
                CampaignCommandRejectionReason.UnsupportedTransition),
        ];
        var reserveBytes = CampaignSnapshotSerializer.Serialize(reserve);

        foreach (var (snapshot, command, reason) in cases)
        {
            var decision = CampaignEngine.Decide(snapshot, command, evidence.Context);

            Assert.False(decision.IsAccepted);
            Assert.Equal(reason, decision.RejectionReason);
            Assert.Empty(decision.Events);
        }

        Assert.Equal(reserveBytes, CampaignSnapshotSerializer.Serialize(reserve));

        var valid = CampaignReserveEventFactory.CreateCompletion(
            reserve,
            evidence.Context,
            new CompleteReserveDesignation(reserve.StateVersion,
                reserve.SequencePosition.PositionId, actingSide));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
        {
            _ = CampaignProjector.Apply(reserve,
                valid with { CampaignId = "campaign-forged" }, evidence.Context);
        });
        Assert.Throws<InvalidCampaignHistoryException>(() =>
        {
            _ = CampaignProjector.Apply(reserve,
                valid with { StateVersion = valid.StateVersion + 1 }, evidence.Context);
        });

        var canonical = Encoding.UTF8.GetString(CampaignEventSerializer.Serialize(valid));
        var forgedSide = canonical.Replace(
            $"\"actingSide\":\"{FormatSide(actingSide)}\"",
            $"\"actingSide\":\"{FormatSide(otherSide)}\"",
            StringComparison.Ordinal);
        var forged = CampaignEventSerializer.Deserialize(
            Encoding.UTF8.GetBytes(forgedSide));
        Assert.Throws<InvalidCampaignHistoryException>(() =>
        {
            _ = CampaignProjector.Apply(reserve, forged, evidence.Context);
        });

        var movement = CampaignProjector.Apply(reserve, valid, evidence.Context);
        var repeated = CampaignEngine.Decide(
            movement,
            new CompleteReserveDesignation(movement.StateVersion,
                movement.SequencePosition.PositionId, actingSide),
            evidence.Context);
        Assert.False(repeated.IsAccepted);
        Assert.Equal(CampaignCommandRejectionReason.UnsupportedTransition,
            repeated.RejectionReason);
        Assert.Empty(repeated.Events);
        Assert.Throws<InvalidCampaignHistoryException>(() =>
        {
            _ = CampaignProjector.Apply(movement, valid, evidence.Context);
        });
    }

    private static StageEntryCampaignEvidence ReachReserve() =>
        StageEntryCampaignTestData.Execute(
            Cna1979SetupCatalog.Definitions[0],
            12345,
            InitiativeOrderChoice.ActFirst);

    private static CampaignActionExecutionResult Execute(
        CampaignSnapshot snapshot,
        CampaignContentContext context,
        CampaignActionAudience audience,
        CampaignActionCandidate candidate)
    {
        var set = Query(snapshot, context, audience);
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
