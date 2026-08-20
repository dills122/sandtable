using Cna.Core.Actions;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Controllers;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class ExerciseControllerTests
{
    private const string ActionA =
        "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ActionB =
        "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void FixedAudienceOrderSelectsTheUniqueActiveAudienceAndFirstActionId()
    {
        var result = ExerciseController.Select(
            Controllers(),
            [
                new ExerciseControllerActionSet(CampaignActionAudience.System, []),
                new ExerciseControllerActionSet(
                    CampaignActionAudience.Axis,
                    [ActionB, ActionA]),
                new ExerciseControllerActionSet(CampaignActionAudience.Commonwealth, []),
            ]);

        Assert.True(result.IsSelected);
        Assert.Equal(CampaignActionAudience.Axis, result.Audience);
        Assert.Equal(ActionA, result.ActionId);
        Assert.Equal(ExerciseControllerSelectionFailure.None, result.FailureReason);
    }

    [Fact]
    public void ZeroAndMultipleActiveAudiencesFailClosed()
    {
        var zero = ExerciseController.Select(
            Controllers(),
            [
                new ExerciseControllerActionSet(CampaignActionAudience.System, []),
                new ExerciseControllerActionSet(CampaignActionAudience.Axis, []),
                new ExerciseControllerActionSet(CampaignActionAudience.Commonwealth, []),
            ]);
        var multiple = ExerciseController.Select(
            Controllers(),
            [
                new ExerciseControllerActionSet(CampaignActionAudience.System, [ActionA]),
                new ExerciseControllerActionSet(CampaignActionAudience.Axis, [ActionB]),
                new ExerciseControllerActionSet(CampaignActionAudience.Commonwealth, []),
            ]);

        Assert.False(zero.IsSelected);
        Assert.Equal(ExerciseControllerSelectionFailure.NoActiveAudience, zero.FailureReason);
        Assert.False(multiple.IsSelected);
        Assert.Equal(
            ExerciseControllerSelectionFailure.MultipleActiveAudiences,
            multiple.FailureReason);
    }

    private static ExerciseControllerManifest Controllers() => new(
        ExerciseControllerPolicy.FirstByActionId,
        ExerciseControllerPolicy.FirstByActionId,
        ExerciseControllerPolicy.FirstByActionId);
}
