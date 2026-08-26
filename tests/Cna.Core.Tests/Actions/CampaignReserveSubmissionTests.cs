using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Tests.Campaigns;

namespace Cna.Core.Tests.Actions;

public sealed class CampaignReserveSubmissionTests
{
    [Fact]
    public void CurrentReserveSubmissionCommitsDesignationTransition()
    {
        var handle = CampaignReserveActionTestData.ReachReserve(
            0,
            InitiativeOrderChoice.ActFirst);
        var audience = CampaignReserveActionTestData.ToAudience(
            FirstActingSideResolver.Resolve(handle.Snapshot));
        var set = CampaignReserveActionTestData.Query(handle, audience);
        var candidate = set.Candidates.OfType<DesignateReserveAction>().First();
        var submission = CampaignReserveActionTestData.Bind(set, candidate);
        var before = CampaignSnapshotSerializer.Serialize(handle.Snapshot);

        var execution = CampaignActionExecution.Execute(
            handle.Snapshot,
            handle.Context,
            submission);
        var result = CampaignLegalActions.Submit(handle, submission);

        Assert.Equal(CampaignActionSubmissionRejectionReason.None,
            execution.RejectionReason);
        Assert.IsType<ReserveElementDesignated>(execution.AcceptedEvent);
        Assert.Equal(handle.Snapshot.StateVersion + 1,
            execution.SuccessorSnapshot!.StateVersion);
        Assert.Equal(handle.Snapshot.SequencePosition,
            execution.SuccessorSnapshot.SequencePosition);
        Assert.NotNull(execution.Receipt);
        Assert.True(result.IsAccepted);
        Assert.Equal(
            CampaignSnapshotSerializer.Serialize(execution.SuccessorSnapshot),
            CampaignSnapshotSerializer.Serialize(result.SuccessorHandle!.Snapshot));
        Assert.Equal(execution.Receipt, result.Receipt);
        Assert.Equal(before, CampaignSnapshotSerializer.Serialize(handle.Snapshot));
    }

    [Fact]
    public void StaleWrongAudienceUnknownAndForgedSubjectSubmissionsRejectWithoutEvents()
    {
        var handle = CampaignReserveActionTestData.ReachReserve(
            0,
            InitiativeOrderChoice.ActLast);
        var actingSide = FirstActingSideResolver.Resolve(handle.Snapshot);
        var audience = CampaignReserveActionTestData.ToAudience(actingSide);
        var opponentAudience = audience == CampaignActionAudience.Axis
            ? CampaignActionAudience.Commonwealth
            : CampaignActionAudience.Axis;
        var set = CampaignReserveActionTestData.Query(handle, audience);
        var candidate = set.Candidates.OfType<DesignateReserveAction>().First();
        var baseline = CampaignReserveActionTestData.Bind(set, candidate);
        var opponentElementId = actingSide == LandSide.Axis
            ? "commonwealth-element-a"
            : "axis-element-a";
        CampaignActionSubmission[] submissions =
        [
            baseline with { ExpectedStateVersion = baseline.ExpectedStateVersion - 1 },
            baseline with { Audience = opponentAudience },
            baseline with { ActionId = $"sha256:{new string('0', 64)}" },
            baseline with
            {
                ActionId = new DesignateReserveAction(opponentElementId).ActionId,
            },
        ];
        CampaignActionSubmissionRejectionReason[] expected =
        [
            CampaignActionSubmissionRejectionReason.StaleState,
            CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignActionSubmissionRejectionReason.ActionNotLegal,
            CampaignActionSubmissionRejectionReason.ActionNotLegal,
        ];
        var before = CampaignSnapshotSerializer.Serialize(handle.Snapshot);

        for (var index = 0; index < submissions.Length; index++)
        {
            var execution = CampaignActionExecution.Execute(
                handle.Snapshot,
                handle.Context,
                submissions[index]);

            Assert.Equal(expected[index], execution.RejectionReason);
            Assert.Null(execution.AcceptedEvent);
            Assert.Null(execution.SuccessorSnapshot);
            Assert.Null(execution.Receipt);
            Assert.Equal(before, CampaignSnapshotSerializer.Serialize(handle.Snapshot));
        }
    }
}
