using System.Text;
using System.Text.Json;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class ExerciseRunResultCodecTests
{
    [Fact]
    public void SuccessHasAnExactClosedCanonicalVersionOneShape()
    {
        var result = ExerciseRunResult.Succeeded(
            new BoundaryReached("land.position.operation-1.organization"));

        var bytes = ExerciseRunResultCodec.Serialize(result);

        Assert.Equal(
            "{\"contractVersion\":1,\"schemeId\":\"sandtable.exercise-result.v1\",\"status\":\"succeeded\",\"outcome\":{\"kind\":\"boundary-reached\",\"positionId\":\"land.position.operation-1.organization\",\"victor\":null},\"failure\":null,\"failureAssertion\":null}",
            Encoding.UTF8.GetString(bytes));
        var roundTrip = ExerciseRunResultCodec.Deserialize(bytes);
        var success = Assert.IsType<ExerciseSucceeded>(roundTrip.Completion);
        Assert.Equal(result, roundTrip);
        Assert.IsType<BoundaryReached>(success.Outcome);
        Assert.Equal(ExerciseProcessExitCode.Succeeded, ExerciseExitCodeMapper.Map(roundTrip));
    }

    [Fact]
    public void ExpectedFailureAssertionNeverRelabelsFailureAsSuccess()
    {
        var result = ExerciseRunResult.Failed(
            ExerciseFailureCategory.StepLimitExceeded,
            ExerciseFailureCategory.StepLimitExceeded);

        var bytes = ExerciseRunResultCodec.Serialize(result);

        Assert.Equal(
            "{\"contractVersion\":1,\"schemeId\":\"sandtable.exercise-result.v1\",\"status\":\"failed\",\"outcome\":null,\"failure\":{\"category\":\"step-limit-exceeded\"},\"failureAssertion\":{\"expectedCategory\":\"step-limit-exceeded\",\"matches\":true}}",
            Encoding.UTF8.GetString(bytes));
        var roundTrip = ExerciseRunResultCodec.Deserialize(bytes);
        Assert.IsType<ExerciseFailed>(roundTrip.Completion);
        Assert.True(roundTrip.FailureAssertion!.Matches);
        Assert.Equal(
            ExerciseProcessExitCode.StepLimitExceeded,
            ExerciseExitCodeMapper.Map(roundTrip));
    }

    [Theory]
    [InlineData("land position.operation-1.organization")]
    [InlineData("land/position.operation-1.organization")]
    [InlineData("Land.position.operation-1.organization")]
    [InlineData("land..position.operation-1.organization")]
    public void ReaderRejectsNoncanonicalBoundaryPositionIds(string positionId)
    {
        var canonical = Encoding.UTF8.GetString(ExerciseRunResultCodec.Serialize(
            ExerciseRunResult.Succeeded(
                new BoundaryReached("land.position.operation-1.organization"))));
        var invalid = canonical.Replace(
            "land.position.operation-1.organization",
            positionId,
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() => ExerciseRunResultCodec.Deserialize(
            Encoding.UTF8.GetBytes(invalid)));
    }

    [Fact]
    public void ReaderRejectsUnknownReorderedAndContradictoryResultShapes()
    {
        var success = Encoding.UTF8.GetString(ExerciseRunResultCodec.Serialize(
            ExerciseRunResult.Succeeded(
                new BoundaryReached("land.position.operation-1.organization"))));
        string[] invalid =
        [
            success.Replace("{\"contractVersion\":1,", "{\"extra\":true,\"contractVersion\":1,", StringComparison.Ordinal),
            success.Replace("\"contractVersion\":1,\"schemeId\"", "\"schemeId\":\"duplicate\",\"contractVersion\":1,\"schemeId\"", StringComparison.Ordinal),
            success.Replace("\"status\":\"succeeded\"", "\"status\":\"unknown\"", StringComparison.Ordinal),
            success.Replace("\"contractVersion\":1,\"schemeId\":\"sandtable.exercise-result.v1\"", "\"schemeId\":\"sandtable.exercise-result.v1\",\"contractVersion\":1", StringComparison.Ordinal),
            success.Replace("\"failure\":null", "\"failure\":{\"category\":\"step-limit-exceeded\"}", StringComparison.Ordinal),
            success.Replace("\"outcome\":{\"kind\":\"boundary-reached\",\"positionId\":\"land.position.operation-1.organization\",\"victor\":null}", "\"outcome\":null", StringComparison.Ordinal),
        ];

        Assert.All(invalid, value => Assert.Throws<JsonException>(() =>
            ExerciseRunResultCodec.Deserialize(Encoding.UTF8.GetBytes(value))));
    }

    [Fact]
    public void ExitMappingIsExhaustiveAndStableForEveryFailureCategory()
    {
        var expected = new Dictionary<ExerciseFailureCategory, ExerciseProcessExitCode>
        {
            [ExerciseFailureCategory.ManifestInvalid] = ExerciseProcessExitCode.ManifestInvalid,
            [ExerciseFailureCategory.BuildIdentityUnavailable] = ExerciseProcessExitCode.BuildIdentityUnavailable,
            [ExerciseFailureCategory.ControllerFailed] = ExerciseProcessExitCode.ControllerFailed,
            [ExerciseFailureCategory.NoUniqueLegalAction] = ExerciseProcessExitCode.NoUniqueLegalAction,
            [ExerciseFailureCategory.IllegalAction] = ExerciseProcessExitCode.IllegalAction,
            [ExerciseFailureCategory.InvariantFailed] = ExerciseProcessExitCode.InvariantFailed,
            [ExerciseFailureCategory.ReconstructionMismatch] = ExerciseProcessExitCode.ReconstructionMismatch,
            [ExerciseFailureCategory.ReadjudicationMismatch] = ExerciseProcessExitCode.ReadjudicationMismatch,
            [ExerciseFailureCategory.StepLimitExceeded] = ExerciseProcessExitCode.StepLimitExceeded,
            [ExerciseFailureCategory.Cancelled] = ExerciseProcessExitCode.Cancelled,
            [ExerciseFailureCategory.ArtifactFailed] = ExerciseProcessExitCode.ArtifactFailed,
            [ExerciseFailureCategory.UnexpectedFailure] = ExerciseProcessExitCode.UnexpectedFailure,
        };

        Assert.Equal(Enum.GetValues<ExerciseFailureCategory>().Length, expected.Count);
        Assert.All(expected, pair => Assert.Equal(
            pair.Value,
            ExerciseExitCodeMapper.Map(ExerciseRunResult.Failed(pair.Key, null))));
    }
}
