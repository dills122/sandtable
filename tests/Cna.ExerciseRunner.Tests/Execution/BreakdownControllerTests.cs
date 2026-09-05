using Cna.Core.Actions;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Controllers;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class BreakdownControllerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BoundedMovementStopsItsRouteBeforeAnotherElementOrSegment(bool remainingMove)
    {
        var candidates = new List<ExerciseControllerCandidate>
        {
            new(3, "stop", "stop-element-movement", null),
        };
        if (remainingMove)
            candidates.Add(new(3, "move", "move-element", "truck", "center", "west",
                new CapabilityPointAmount(1, 1)));
        var policy = ExerciseControllerPolicy.ActFirstReserveNoneMoveEachOnceThenComplete;
        var result = ExerciseController.Select(new ExerciseControllerManifest(policy, policy, policy),
        [
            new(CampaignActionAudience.System, []),
            new(CampaignActionAudience.Axis, candidates, priorMovedElementIds: ["truck"]),
            new(CampaignActionAudience.Commonwealth, []),
        ]);
        Assert.True(result.IsSelected);
        Assert.Equal("stop", result.ActionId);
    }

    [Theory]
    [InlineData("complete-movement-segment")]
    [InlineData("stop-element-movement")]
    public void BoundedMovementRejectsConflictingRouteTerminalCandidates(string kind)
    {
        var policy = ExerciseControllerPolicy.ActFirstReserveNoneMoveEachOnceThenComplete;
        var result = ExerciseController.Select(new ExerciseControllerManifest(policy, policy, policy),
        [
            new(CampaignActionAudience.System, []),
            new(CampaignActionAudience.Axis,
                [new(3, "stop", "stop-element-movement", null), new(3, "other", kind, null)]),
            new(CampaignActionAudience.Commonwealth, []),
        ]);
        Assert.False(result.IsSelected);
        Assert.Equal(ExerciseControllerSelectionFailure.PolicyFailed, result.FailureReason);
    }
}
