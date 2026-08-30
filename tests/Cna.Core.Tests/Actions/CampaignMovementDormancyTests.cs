using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Actions;

public sealed class CampaignMovementAtomicPublicationTests
{
    [Fact]
    public void PublicMovementQueryPublishesTheCompleteVerticalOnlyToTheActingSide()
    {
        var handle = ReachMovement();
        var side = FirstActingSideResolver.Resolve(handle.Snapshot);
        var audience = CampaignReserveActionTestData.ToAudience(side);
        var opponent = audience == CampaignActionAudience.Axis
            ? CampaignActionAudience.Commonwealth
            : CampaignActionAudience.Axis;

        var acting = CampaignLegalActions.Query(handle, audience);

        Assert.True(acting.IsSuccessful);
        Assert.NotEmpty(acting.ActionSet!.Candidates.OfType<MoveElementAction>());
        Assert.Single(acting.ActionSet.Candidates
            .OfType<CompleteMovementSegmentAction>());
        Assert.Empty(CampaignLegalActions.Query(handle, opponent)
            .ActionSet!.Candidates);
        Assert.Empty(CampaignLegalActions.Query(handle, CampaignActionAudience.System)
            .ActionSet!.Candidates);
    }

    [Fact]
    public void EveryPublishedMoveAndCompletionIsExecutableFromThePublishedAuthority()
    {
        var handle = ReachMovement();
        var side = FirstActingSideResolver.Resolve(handle.Snapshot);
        var audience = CampaignReserveActionTestData.ToAudience(side);
        var query = CampaignLegalActions.Query(handle, audience);
        Assert.True(query.IsSuccessful);
        var publishedCandidates = query.ActionSet!.Candidates;
        var before = CampaignSnapshotSerializer.Serialize(handle.Snapshot);

        Assert.Contains(publishedCandidates, candidate => candidate is MoveElementAction);
        Assert.Contains(publishedCandidates,
            candidate => candidate is CompleteMovementSegmentAction);

        foreach (var candidate in publishedCandidates)
        {
            var submission = new CampaignActionSubmission(
                CampaignActionSubmission.CurrentContractVersion,
                handle.Snapshot.CampaignId,
                handle.Snapshot.StateVersion,
                handle.Snapshot.SequencePosition.PositionId,
                audience,
                candidate.ActionId);

            var execution = CampaignActionExecution.Execute(
                handle.Snapshot,
                handle.Context,
                submission);
            var publicResult = CampaignLegalActions.Submit(handle, submission);

            Assert.True(execution.IsAccepted);
            Assert.NotNull(execution.AcceptedEvent);
            Assert.NotNull(execution.SuccessorSnapshot);
            Assert.NotNull(execution.Receipt);
            Assert.True(publicResult.IsAccepted);
            Assert.NotNull(publicResult.SuccessorHandle);
            Assert.NotNull(publicResult.Receipt);
            Assert.Equal(before, CampaignSnapshotSerializer.Serialize(handle.Snapshot));
        }
    }

    private static CampaignAuthorityHandle ReachMovement()
    {
        var reserve = CampaignReserveActionTestData.ReachReserve(
            0,
            InitiativeOrderChoice.ActLast);
        var side = FirstActingSideResolver.Resolve(reserve.Snapshot);
        var audience = CampaignReserveActionTestData.ToAudience(side);
        var set = CampaignReserveActionTestData.Query(reserve, audience);
        var completion = Assert.Single(
            set.Candidates.OfType<CompleteReserveDesignationAction>());
        var result = CampaignLegalActions.Submit(
            reserve,
            CampaignReserveActionTestData.Bind(set, completion));

        Assert.True(result.IsAccepted);
        Assert.Equal(LandSegmentIds.Movement,
            result.SuccessorHandle!.Snapshot.SequencePosition.SegmentId);
        return result.SuccessorHandle;
    }
}
