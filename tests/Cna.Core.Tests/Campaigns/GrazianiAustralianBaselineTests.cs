using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Campaigns;

public sealed class GrazianiAustralianBaselineTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OwnersIndependentlyApproveAndMayWithdrawProvisionalBaseline(bool axisFirst)
    {
        var initial = CampaignSourceTrainingLedger.CreateSixthAustralian();
        var first = initial.SetBaselineVote(axisFirst, true);
        Assert.Null(first.CurrentMorale);
        Assert.Null(first.QualifyingStages);
        Assert.True(first.HasBaselineVote(axisFirst));
        Assert.False(first.HasBaselineVote(!axisFirst));
        Assert.Equal(initial.SerializeCanonical(), first.SetBaselineVote(axisFirst, false).SerializeCanonical());
        Assert.Throws<InvalidOperationException>(() => first.SetBaselineVote(axisFirst, true));

        var agreed = first.SetBaselineVote(!axisFirst, true);
        Assert.Equal(-3, agreed.CurrentMorale);
        Assert.Equal(0, agreed.QualifyingStages);
        Assert.Equal(CampaignSourceTrainingLedger.ProvisionalBaselineId, agreed.InitialRuling);
        var withdrawn = agreed.SetBaselineVote(axisFirst, false);
        Assert.Null(withdrawn.CurrentMorale);
        Assert.Null(withdrawn.QualifyingStages);
        Assert.True(withdrawn.HasBaselineVote(!axisFirst));
        var interrupted = agreed.Interrupt();
        Assert.True(interrupted.SetBaselineVote(axisFirst, false).Interrupted);

        var recorded = agreed.RecordStage(1, true, true, false, true, true, true, true);
        Assert.Equal(1, recorded.QualifyingStages);
        Assert.Throws<InvalidOperationException>(() => recorded.SetBaselineVote(axisFirst, false));
    }

    [Fact]
    public void BaselineVoteActionRoundTripsForEitherOwner()
    {
        var position = Cna1979LandSequenceV4.CreateTurn(1).Single(value => value.OperationStage == 1 && value.PhaseId == LandPhaseIds.Organization);
        foreach (var audience in new[] { CampaignActionAudience.Axis, CampaignActionAudience.Commonwealth })
        {
            var actions = new CampaignLegalActionSet("australian-vote", 17, Cna1979Ruleset.Manifest.Hash,
                position.PositionId, audience, [CampaignSourceTurnAction.ApproveSixthAustralianBaseline(17), CampaignSourceTurnAction.WithdrawSixthAustralianBaseline(17)], CampaignLegalActionSet.SourceTurnPolicyId);
            Assert.Equal(actions, CampaignLegalActionSerializer.DeserializeCanonical(CampaignLegalActionSerializer.Serialize(actions)));
        }
    }
}
