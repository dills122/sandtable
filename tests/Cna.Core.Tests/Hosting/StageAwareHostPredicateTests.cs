using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.Core.Hosting;
using Cna.Core.Rules;
using Cna.Core.Tests.Exercises.Sequencing;

namespace Cna.Core.Tests.Hosting;

public sealed class StageAwareHostPredicateTests
{
    [Theory]
    [InlineData("op1.first", false)]
    [InlineData("op1.second", true)]
    [InlineData("op2.first", false)]
    [InlineData("op2.second", true)]
    [InlineData("op3.first", false)]
    [InlineData("op3.second", true)]
    public void AcceptedSelectionBelongsOnlyToCurrentOccurrence(string occurrence, bool second)
    {
        var session = Sample(occurrence, "combat-selection");
        Assert.Equal(second, CampaignHostAdapter.IsSecondSideCombatPending(session));
        Assert.Equal(second, CampaignHostAdapter.IsNativeSecondCombatVoluntaryFinish(session));
        Assert.Equal(!second, CampaignHostAdapter.IsNativeCombatVoluntaryFinish(session));
        Assert.False(CampaignHostAdapter.IsNativeCombatSelectedAssault(session));
        Assert.False(CampaignHostAdapter.IsNativeSecondCombatSelectedAssault(session));

        var completed = Sample(occurrence, "reserve-release");
        Assert.False(CampaignHostAdapter.IsSecondSideCombatPending(completed));
        Assert.False(CampaignHostAdapter.IsNativeSecondCombatVoluntaryFinish(completed));
        Assert.False(CampaignHostAdapter.IsNativeCombatVoluntaryFinish(completed));
    }

    [Theory]
    [InlineData("op1.second", true)]
    [InlineData("op2.first", false)]
    [InlineData("op2.second", true)]
    [InlineData("op3.first", false)]
    [InlineData("op3.second", true)]
    public void BreakdownEntryDoesNotReuseAnEarlierSelection(string occurrence, bool second)
    {
        var session = Sample(occurrence, "breakdown");
        Assert.Equal(second, CampaignHostAdapter.IsSecondSideCombatPending(session));
        Assert.False(CampaignHostAdapter.IsNativeSecondCombatVoluntaryFinish(session));
        Assert.False(CampaignHostAdapter.IsNativeCombatVoluntaryFinish(session));
        Assert.Equal(CampaignExercises.QueryCheckpoint(session).StateVersion,
            CampaignHostAdapter.QueryNativeCombatSelectionRoundBinding(session).StateVersion);
    }

    [Theory]
    [InlineData("op1.first")]
    [InlineData("op1.second")]
    [InlineData("op2.first")]
    [InlineData("op2.second")]
    [InlineData("op3.first")]
    [InlineData("op3.second")]
    public void CycleControlDraftAndSubmitUseCurrentRelease(string occurrence)
    {
        var session = Sample(occurrence, "reserve-release");
        var release = CampaignExercises.QueryPendingNativeCycleControl(session);
        var owner = release.ActingSide == LandSide.Axis
            ? CampaignActionAudience.Axis : CampaignActionAudience.Commonwealth;
        var other = owner == CampaignActionAudience.Axis
            ? CampaignActionAudience.Commonwealth : CampaignActionAudience.Axis;
        var draft = CampaignHostAdapter.QueryNativeCycleControlOwnerDraft(session, owner);
        var checkpoint = CampaignExercises.QueryCheckpoint(session);
        Assert.Equal(checkpoint.StateVersion, draft.AuthorityVersion);
        Assert.Throws<InvalidOperationException>(() =>
            CampaignHostAdapter.QueryNativeCycleControlOwnerDraft(session, other));

        var command = new CampaignActionSubmission(CampaignActionSubmission.CurrentContractVersion,
            checkpoint.CampaignId, checkpoint.StateVersion, checkpoint.PositionId, owner, draft.PrivateActionIds[^1]);
        var accepted = CampaignHostAdapter.SubmitNativeCycleControl(session, command);
        Assert.True(accepted.IsAccepted);
        var direct = CampaignExercises.SubmitNativeCycleControl(session, owner,
            CampaignNativeCycleControlChoiceV28.Finish, command.ActionId);
        Assert.True(direct.IsAccepted);
        Assert.Equal(direct.Evidence!.EventRecords.Single(), accepted.Evidence!.EventRecords.Single());
        var successor = accepted.SuccessorSession!;
        Assert.Equal(Sample(occurrence, "cycle-control-finish").CurrentContacts.AuthorityVersion,
            CampaignExercises.QueryCheckpoint(successor).StateVersion);
        Assert.Throws<InvalidOperationException>(() =>
            CampaignHostAdapter.QueryNativeCycleControlOwnerDraft(successor, owner));
        Assert.False(CampaignHostAdapter.SubmitNativeCycleControl(successor, command).IsAccepted);
    }

    private static ExerciseSession Sample(string occurrence, string cut) =>
        SequencingScenarios.All[0].Samples.Single(sample => sample.Label == $"{occurrence}.{cut}").Session;
}
