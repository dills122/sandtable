using Cna.Core.Actions;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Controllers;
using Cna.ExerciseRunner.Tests.Artifacts;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class BreakdownStudyControllerTests
{
    [Theory]
    [InlineData(ExerciseControllerPolicy.ActFirstReserveAllMoveEachOnceByLowestCostThenComplete)]
    [InlineData(ExerciseControllerPolicy.ActFirstReserveAllRepeatHighestCostStopsThenComplete)]
    public void StudyPolicyRoundTripsAsAClosedNamedPolicy(ExerciseControllerPolicy policy)
    {
        var manifest = ExerciseManifestCodecTests.Create(controllerPolicy: policy);
        Assert.Equal(manifest, ExerciseManifestCodec.Deserialize(ExerciseManifestCodec.Serialize(manifest)));
    }

    [Theory]
    [InlineData(ExerciseControllerPolicy.ActFirstReserveAllMoveEachOnceByLowestCostThenComplete, "low")]
    [InlineData(ExerciseControllerPolicy.ActFirstReserveAllRepeatHighestCostStopsThenComplete, "high")]
    public void StudyPolicyUsesPublishedCostAndReservesAllCombat(ExerciseControllerPolicy policy, string expected)
    {
        var moves = Select(policy,
            [new(3, "high", "move-element", "truck", "west", "center", new CapabilityPointAmount(8, 1)),
             new(3, "low", "move-element", "truck", "west", "north", new CapabilityPointAmount(1, 2)),
             new(3, "finish", "complete-movement-segment", null)]);
        Assert.Equal(expected, moves.ActionId);
        var reserve = Select(policy,
            [new(3, "finish", "complete-reserve-designation", null),
             new(3, "reserve", "designate-reserve", "infantry")]);
        Assert.Equal("reserve", reserve.ActionId);
    }

    [Fact]
    public void RepeatPolicyCanMoveSurvivorsAfterPriorStopAndStillStopsEveryOpenRoute()
    {
        var policy = ExerciseControllerPolicy.ActFirstReserveAllRepeatHighestCostStopsThenComplete;
        var move = new ExerciseControllerCandidate(3, "move", "move-element", "truck", "center", "west", new(2, 1));
        Assert.Equal("move", Select(policy,
            [move, new(3, "finish", "complete-movement-segment", null)], ["truck"]).ActionId);
        Assert.Equal("stop", Select(policy,
            [move, new(3, "stop", "stop-element-movement", null)], ["truck"]).ActionId);
        Assert.Equal("finish", Select(policy,
            [new(3, "finish", "complete-movement-segment", null)], ["truck"]).ActionId);
    }

    private static ExerciseControllerSelection Select(ExerciseControllerPolicy policy,
        IEnumerable<ExerciseControllerCandidate> candidates, IEnumerable<string>? moved = null) =>
        ExerciseController.Select(new(policy, policy, policy),
        [
            new(CampaignActionAudience.System, []),
            new(CampaignActionAudience.Axis, candidates, priorMovedElementIds: moved),
            new(CampaignActionAudience.Commonwealth, []),
        ]);
}
