using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Observations;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Actions;

public sealed class CampaignMovementDormancyTests
{
    [Fact]
    public void PublicMovementQueryRemainsEmptyForEveryAudience()
    {
        var handle = ReachMovement();

        foreach (var audience in Enum.GetValues<CampaignActionAudience>())
        {
            var result = CampaignLegalActions.Query(handle, audience);

            Assert.True(result.IsSuccessful);
            Assert.Empty(result.ActionSet!.Candidates);
        }
    }

    [Fact]
    public void WellFormedDormantMovementIdsRejectWithoutEventReceiptOrMutation()
    {
        var handle = ReachMovement();
        var side = FirstActingSideResolver.Resolve(handle.Snapshot);
        var audience = CampaignReserveActionTestData.ToAudience(side);
        var projection = CampaignObservationProjector.Project(
            handle.Snapshot,
            handle.Context,
            side);
        var observation = Assert.IsType<CampaignObservation>(projection.Observation);
        var dormantCandidates = CampaignMovementActionDerivation.Derive(observation);
        var before = CampaignSnapshotSerializer.Serialize(handle.Snapshot);

        Assert.Contains(dormantCandidates, candidate => candidate is MoveElementAction);
        Assert.Contains(dormantCandidates,
            candidate => candidate is CompleteMovementSegmentAction);

        foreach (var candidate in dormantCandidates)
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

            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                execution.RejectionReason);
            Assert.Null(execution.AcceptedEvent);
            Assert.Null(execution.SuccessorSnapshot);
            Assert.Null(execution.Receipt);
            Assert.False(publicResult.IsAccepted);
            Assert.Equal(CampaignActionSubmissionRejectionReason.ActionNotLegal,
                publicResult.RejectionReason);
            Assert.Null(publicResult.SuccessorHandle);
            Assert.Null(publicResult.Receipt);
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
