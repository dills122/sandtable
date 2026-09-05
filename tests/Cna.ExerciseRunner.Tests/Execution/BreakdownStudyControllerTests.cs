using Cna.Core.Actions;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Controllers;
using Cna.ExerciseRunner.Execution;
using Cna.ExerciseRunner.Tests.Artifacts;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class BreakdownStudyControllerTests
{
    [Theory]
    [InlineData(ExerciseControllerPolicy.ActFirstReserveAllMoveEachOnceByLowestCostThenComplete,
        "sha256:d9c25ee30963d4726e7ee23fdb46cac4d4b3f84698fb58585fe95fe0cc5fb926")]
    [InlineData(ExerciseControllerPolicy.ActFirstReserveAllRepeatHighestCostStopsThenComplete,
        "sha256:6d16851f8bbecd29e494afc405ccb446dcbc58d404a92b31fe5849b7630dceb0")]
    public void StudyPolicyRoundTripsAsAClosedNamedPolicy(ExerciseControllerPolicy policy, string expectedHash)
    {
        var manifest = ExerciseManifestCodecTests.Create(controllerPolicy: policy);
        Assert.Equal(manifest, ExerciseManifestCodec.Deserialize(ExerciseManifestCodec.Serialize(manifest)));
        Assert.Equal(expectedHash, ExerciseConfigurationIdentity.ComputeHash(manifest));
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
