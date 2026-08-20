using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;
using Cna.ExerciseRunner.Tests.Artifacts;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class ExerciseExecutorTests
{
    [Fact]
    public void CheckedInShapeRunsToTheExactBoundaryWithBothProofs()
    {
        var result = Execute(ExerciseManifestCodecTests.Create());

        Assert.True(result.IsSucceeded);
        Assert.Equal("land.position.operation-1.organization", result.BoundaryPositionId);
        Assert.Equal(5, result.Steps.Count);
        Assert.All(result.Steps, step => Assert.Single(step.EventRecords));
        Assert.NotEmpty(result.InitialSnapshot);
        Assert.NotEmpty(result.FinalSnapshot);
        Assert.True(result.Reconstruction!.IsVerified);

        var readjudication = ReadjudicationVerifier.Verify(
            ExerciseManifestCodecTests.Create(),
            result);
        Assert.True(readjudication.IsVerified);
        Assert.True(readjudication.TranscriptMatches);
        Assert.True(readjudication.EventsMatch);
        Assert.True(readjudication.FinalSnapshotMatches);
        Assert.Equal(ReadjudicationProof.CurrentContractVersion, readjudication.ContractVersion);
        Assert.Equal(ReadjudicationProof.SchemeId, readjudication.ContractSchemeId);
        Assert.Equal(
            readjudication,
            ReplayProofCodec.DeserializeReadjudication(
                ReplayProofCodec.Serialize(readjudication)));
        Assert.Equal(
            result.RunResult,
            ExerciseRunResultCodec.Deserialize(
                ExerciseRunResultCodec.Serialize(result.RunResult)));
    }

    [Fact]
    public void MaximumStepBoundFailsWithoutRelabelingFailureAsSuccess()
    {
        var result = Execute(ExerciseManifestCodecTests.Create(maximumSteps: 4));

        Assert.False(result.IsSucceeded);
        Assert.Equal(ExerciseFailureCategory.StepLimitExceeded, result.FailureCategory);
        Assert.Null(result.BoundaryPositionId);
        Assert.Equal(4, result.Steps.Count);
    }

    [Fact]
    public void ExactExpectedFailureIsReportedButRemainsAFailedRun()
    {
        var result = Execute(ExerciseManifestCodecTests.Create(
            maximumSteps: 4,
            assertFailureCategory: ExerciseFailureCategory.StepLimitExceeded));

        Assert.False(result.IsSucceeded);
        Assert.IsType<ExerciseFailed>(result.RunResult.Completion);
        Assert.True(result.RunResult.FailureAssertion!.Matches);
        Assert.NotEqual(
            ExerciseProcessExitCode.Succeeded,
            ExerciseExitCodeMapper.Map(result.RunResult));
    }

    [Fact]
    public void ZeroActiveAudiencesFailClosedWithOrderedCheckEvidence()
    {
        var result = Execute(ExerciseManifestCodecTests.Create(
            terminalBoundary: "land.position.unreachable"));

        Assert.False(result.IsSucceeded);
        Assert.Equal(ExerciseFailureCategory.NoUniqueLegalAction, result.FailureCategory);
        Assert.Equal(
            ExerciseCheckFailureCode.NoActiveAudience,
            result.CheckResults.Results[^2].FailureCode);
        Assert.Equal(ExerciseCheckId.TerminalBoundary, result.CheckResults.Results[^1].CheckId);
        Assert.False(result.CheckResults.Results[^1].IsPassed);
    }

    [Fact]
    public void CancellationBeforeExecutionFailsWithoutInventingChecks()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

#pragma warning disable xUnit1051 // An already-cancelled token is the behavior under test.
        var result = ExerciseExecutor.Execute(
            ExerciseManifestCodecTests.Create(),
            cancellation.Token);
#pragma warning restore xUnit1051

        Assert.False(result.IsSucceeded);
        Assert.Equal(ExerciseFailureCategory.Cancelled, result.FailureCategory);
        Assert.Empty(result.Steps);
        Assert.Empty(result.CheckResults.Results);
        Assert.Equal(
            ExerciseProcessExitCode.Cancelled,
            ExerciseExitCodeMapper.Map(result.RunResult));
    }

    [Fact]
    public void ReadjudicationRejectsATruncatedFailedExecution()
    {
        var manifest = ExerciseManifestCodecTests.Create(maximumSteps: 4);
        var failed = Execute(manifest);

        var proof = ReadjudicationVerifier.Verify(manifest, failed);

        Assert.False(proof.IsVerified);
        Assert.False(proof.TranscriptMatches);
        Assert.False(proof.EventsMatch);
        Assert.False(proof.FinalSnapshotMatches);
    }

    [Fact]
    public void ReadjudicationRejectsChangedActionOrder()
    {
        var manifest = ExerciseManifestCodecTests.Create();
        var expected = Execute(manifest);
        var changed = Copy(expected, expected.Steps.Reverse());

        var proof = ReadjudicationVerifier.Verify(manifest, changed);

        Assert.False(proof.IsVerified);
        Assert.False(proof.TranscriptMatches);
    }

    [Fact]
    public void ReadjudicationRejectsChangedEventBytes()
    {
        var manifest = ExerciseManifestCodecTests.Create();
        var expected = Execute(manifest);
        var steps = expected.Steps.ToArray();
        var original = steps[0];
        var records = original.EventRecords.Select(value => value.ToArray()).ToArray();
        records[0][0] ^= 0xff;
        steps[0] = new ExerciseAcceptedStep(
            original.Ordinal,
            original.Receipt,
            records,
            original.SnapshotCheckpoint);
        var changed = Copy(expected, steps);

        var proof = ReadjudicationVerifier.Verify(manifest, changed);

        Assert.False(proof.IsVerified);
        Assert.False(proof.EventsMatch);
    }

    [Fact]
    public void ReadjudicationRejectsChangedFinalSnapshotBytes()
    {
        var manifest = ExerciseManifestCodecTests.Create();
        var expected = Execute(manifest);
        var final = expected.FinalSnapshot;
        final[0] ^= 0xff;
        var changed = new ExerciseExecutionResult(
            expected.RunResult,
            expected.Steps,
            expected.InitialSnapshot,
            final,
            expected.Reconstruction,
            expected.CheckResults,
            expected.SeedLedger);

        var proof = ReadjudicationVerifier.Verify(manifest, changed);

        Assert.False(proof.IsVerified);
        Assert.False(proof.FinalSnapshotMatches);
    }

    [Fact]
    public void ReadjudicationReportsItsOwnProofIndependentlyOfReconstructionStatus()
    {
        var manifest = ExerciseManifestCodecTests.Create();
        var expected = Execute(manifest);
        var withoutReconstruction = new ExerciseExecutionResult(
            expected.RunResult,
            expected.Steps,
            expected.InitialSnapshot,
            expected.FinalSnapshot,
            null,
            expected.CheckResults,
            expected.SeedLedger);

        var proof = ReadjudicationVerifier.Verify(manifest, withoutReconstruction);

        Assert.True(proof.IsVerified);
    }

    private static ExerciseExecutionResult Copy(
        ExerciseExecutionResult source,
        IEnumerable<ExerciseAcceptedStep> steps) => new(
            source.RunResult,
            steps,
            source.InitialSnapshot,
            source.FinalSnapshot,
            source.Reconstruction,
            source.CheckResults,
            source.SeedLedger);

    private static ExerciseExecutionResult Execute(ExerciseManifest manifest) =>
        ExerciseExecutor.Execute(manifest, TestContext.Current.CancellationToken);
}
