using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Controllers;
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
    public void QueryFailureRetainsThePartialDecisionAndAvailableTimings()
    {
        var runtime = new FaultingRuntime
        {
            QueryOverride = (audience, result) => audience == CampaignActionAudience.Axis
                ? ExerciseRuntimeQueryResult.Rejected()
                : result,
        };
        var manifest = ExerciseManifestCodecTests.Create(detail: ExerciseDetail.Debug);

        var result = ExerciseExecutor.Execute(
            manifest,
            runtime,
            TestContext.Current.CancellationToken);

        var decision = Assert.Single(result.FailedDecisions);
        Assert.Equal(ExerciseDecisionFailureStage.AuthorityQuery, decision.FailureStage);
        Assert.Equal(ExerciseCheckFailureCode.AuthorityQueryRejected, decision.FailureCode);
        Assert.Equal(2, decision.Queries.Count);
        Assert.NotNull(result.BeginElapsedMicroseconds);
        Assert.All(decision.Queries, query => Assert.True(query.ElapsedMicroseconds >= 0));
        AssertCorrelatedFailedDecisionDiagnostics(manifest, result, "authority-query");
    }

    [Fact]
    public void ControllerFailureRetainsQueriesAndSelectionFailure()
    {
        var runtime = new FaultingRuntime
        {
            SelectionOverride = (_, _) => ExerciseControllerSelection.Failed(
                ExerciseControllerSelectionFailure.PolicyFailed),
        };
        var manifest = ExerciseManifestCodecTests.Create(detail: ExerciseDetail.Forensic);

        var result = ExerciseExecutor.Execute(
            manifest,
            runtime,
            TestContext.Current.CancellationToken);

        var decision = Assert.Single(result.FailedDecisions);
        Assert.Equal(ExerciseDecisionFailureStage.ControllerSelection, decision.FailureStage);
        Assert.Equal(ExerciseControllerSelectionFailure.PolicyFailed, decision.SelectionFailure);
        Assert.Equal(3, decision.Queries.Count);
        Assert.NotNull(decision.ControllerElapsedMicroseconds);
        AssertCorrelatedFailedDecisionDiagnostics(manifest, result, "controller-selection");
    }

    [Fact]
    public void SelectedActionMembershipFailureRetainsTheAttemptedAction()
    {
        var invalidActionId = Sha('f');
        var runtime = new FaultingRuntime
        {
            SelectionOverride = (_, actionSets) => ExerciseControllerSelection.Selected(
                actionSets.Single(set => set.ActionIds.Count > 0).Audience,
                invalidActionId),
        };
        var manifest = ExerciseManifestCodecTests.Create(detail: ExerciseDetail.Forensic);

        var result = ExerciseExecutor.Execute(
            manifest,
            runtime,
            TestContext.Current.CancellationToken);

        var decision = Assert.Single(result.FailedDecisions);
        Assert.Equal(
            ExerciseDecisionFailureStage.SelectedActionMembership,
            decision.FailureStage);
        Assert.Equal(invalidActionId, decision.SelectedActionId);
        Assert.False(decision.SubmissionAttempted);
        AssertCorrelatedFailedDecisionDiagnostics(manifest, result, "selected-action-membership");
    }

    [Fact]
    public void SubmissionRejectionRetainsSelectedActionAndSubmissionResult()
    {
        var runtime = new FaultingRuntime
        {
            SubmissionOverride = (_, _, _) => ExerciseRuntimeStepResult.Rejected(
                CampaignActionSubmissionRejectionReason.InvalidSubmission),
        };
        var manifest = ExerciseManifestCodecTests.Create(detail: ExerciseDetail.Forensic);

        var result = ExerciseExecutor.Execute(
            manifest,
            runtime,
            TestContext.Current.CancellationToken);

        var decision = Assert.Single(result.FailedDecisions);
        Assert.Equal(ExerciseDecisionFailureStage.ActionSubmission, decision.FailureStage);
        Assert.NotNull(decision.SelectedActionId);
        Assert.True(decision.SubmissionAttempted);
        Assert.False(decision.SubmissionAccepted);
        Assert.Equal(
            CampaignActionSubmissionRejectionReason.InvalidSubmission,
            decision.SubmissionRejectionReason);
        AssertCorrelatedFailedDecisionDiagnostics(manifest, result, "action-submission");
    }

    [Fact]
    public void EventCardinalityFailureRetainsAcceptedSubmissionEvidenceCount()
    {
        var runtime = new FaultingRuntime
        {
            SubmissionOverride = (_, _, result) => result with { EventRecords = [] },
        };
        var manifest = ExerciseManifestCodecTests.Create(detail: ExerciseDetail.Forensic);

        var result = ExerciseExecutor.Execute(
            manifest,
            runtime,
            TestContext.Current.CancellationToken);

        var decision = Assert.Single(result.FailedDecisions);
        Assert.Equal(ExerciseDecisionFailureStage.EventCardinality, decision.FailureStage);
        Assert.True(decision.SubmissionAccepted);
        Assert.Equal(0, decision.SubmittedEventCount);
        AssertCorrelatedFailedDecisionDiagnostics(manifest, result, "event-cardinality");
    }

    [Fact]
    public void ContinuityFailureRetainsTheCompleteAttemptedDecision()
    {
        ExerciseCheckpoint? initialCheckpoint = null;
        var runtime = new FaultingRuntime
        {
            CheckpointOverride = (call, checkpoint) => call switch
            {
                1 => initialCheckpoint = checkpoint,
                2 => initialCheckpoint!,
                _ => checkpoint,
            },
        };
        var manifest = ExerciseManifestCodecTests.Create(detail: ExerciseDetail.Forensic);

        var result = ExerciseExecutor.Execute(
            manifest,
            runtime,
            TestContext.Current.CancellationToken);

        var decision = Assert.Single(result.FailedDecisions);
        Assert.Equal(ExerciseDecisionFailureStage.CheckpointContinuity, decision.FailureStage);
        Assert.True(decision.SubmissionAccepted);
        Assert.Equal(1, decision.SubmittedEventCount);
        AssertCorrelatedFailedDecisionDiagnostics(manifest, result, "checkpoint-continuity");
    }

    [Fact]
    public void DebugFailuresPreserveEveryTimingMeasuredBeforeFailure()
    {
        var stepLimitManifest = ExerciseManifestCodecTests.Create(
            maximumSteps: 4,
            detail: ExerciseDetail.Debug);
        var noActiveManifest = ExerciseManifestCodecTests.Create(
            terminalBoundary: "land.position.unreachable",
            detail: ExerciseDetail.Debug);
        var mismatchRuntime = new FaultingRuntime
        {
            ReconstructionOverride = (_, _) => new ReconstructionProof(
                ExerciseReconstructionFailureReason.SnapshotMismatch,
                Sha('1'),
                Sha('2'),
                Sha('3')),
        };
        var mismatchManifest = ExerciseManifestCodecTests.Create(detail: ExerciseDetail.Debug);

        var stepLimit = Execute(stepLimitManifest);
        var noActive = Execute(noActiveManifest);
        var mismatch = ExerciseExecutor.Execute(
            mismatchManifest,
            mismatchRuntime,
            TestContext.Current.CancellationToken);

        Assert.NotNull(stepLimit.BeginElapsedMicroseconds);
        Assert.NotNull(noActive.BeginElapsedMicroseconds);
        Assert.NotNull(mismatch.BeginElapsedMicroseconds);
        Assert.NotNull(mismatch.ReconstructionElapsedMicroseconds);
        Assert.Contains(
            "\"operation\":\"core-begin\"",
            Diagnostics(stepLimitManifest, stepLimit),
            StringComparison.Ordinal);
        Assert.Contains(
            "\"operation\":\"controller-selection\"",
            Diagnostics(noActiveManifest, noActive),
            StringComparison.Ordinal);
        Assert.Contains(
            "\"operation\":\"history-reconstruction\"",
            Diagnostics(mismatchManifest, mismatch),
            StringComparison.Ordinal);
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

    private static void AssertCorrelatedFailedDecisionDiagnostics(
        ExerciseManifest manifest,
        ExerciseExecutionResult result,
        string failureStage)
    {
        var diagnostics = Diagnostics(manifest, result);
        Assert.Contains(
            $"\"event\":\"exercise.decision-failed\"",
            diagnostics,
            StringComparison.Ordinal);
        Assert.Contains($"\"failureStage\":\"{failureStage}\"", diagnostics, StringComparison.Ordinal);
        Assert.Contains("\"campaignId\":\"", diagnostics, StringComparison.Ordinal);
        Assert.Contains(
            diagnostics.Split('\n', StringSplitOptions.RemoveEmptyEntries),
            line => line.Contains("\"event\":\"exercise.check-evaluated\"", StringComparison.Ordinal)
                && line.Contains("\"stepOrdinal\":0", StringComparison.Ordinal)
                && line.Contains("\"campaignId\":\"", StringComparison.Ordinal)
                && line.Contains("\"status\":\"failed\"", StringComparison.Ordinal));
    }

    private static string Diagnostics(ExerciseManifest manifest, ExerciseExecutionResult result) =>
        System.Text.Encoding.UTF8.GetString(ExerciseDiagnosticsWriter.Write(
            manifest,
            result,
            result.RunResult,
            result.CheckResults,
            null));

    private static string Sha(char value) => $"sha256:{new string(value, 64)}";

    private sealed class FaultingRuntime : IExerciseExecutionRuntime
    {
        private readonly CoreExerciseExecutionRuntime inner = CoreExerciseExecutionRuntime.Instance;
        private int checkpointCalls;

        internal Func<CampaignActionAudience, ExerciseRuntimeQueryResult, ExerciseRuntimeQueryResult>?
            QueryOverride
        { get; init; }
        internal Func<ExerciseControllerManifest, IReadOnlyList<ExerciseControllerActionSet>, ExerciseControllerSelection>?
            SelectionOverride
        { get; init; }
        internal Func<ExerciseSession, CampaignActionSubmission, ExerciseRuntimeStepResult, ExerciseRuntimeStepResult>?
            SubmissionOverride
        { get; init; }
        internal Func<int, ExerciseCheckpoint, ExerciseCheckpoint>? CheckpointOverride { get; init; }
        internal Func<ExerciseSession, ReconstructionProof, ReconstructionProof>?
            ReconstructionOverride
        { get; init; }

        public ExerciseStartResult Begin(CampaignCreationRequest request) => inner.Begin(request);

        public ExerciseCheckpoint QueryCheckpoint(ExerciseSession session)
        {
            var checkpoint = inner.QueryCheckpoint(session);
            return CheckpointOverride?.Invoke(++checkpointCalls, checkpoint) ?? checkpoint;
        }

        public ExerciseRuntimeQueryResult Query(
            ExerciseSession session,
            CampaignActionAudience audience)
        {
            var result = inner.Query(session, audience);
            return QueryOverride?.Invoke(audience, result) ?? result;
        }

        public ExerciseControllerSelection Select(
            ExerciseControllerManifest policies,
            IReadOnlyList<ExerciseControllerActionSet> actionSets) =>
            SelectionOverride?.Invoke(policies, actionSets) ?? inner.Select(policies, actionSets);

        public ExerciseRuntimeStepResult Submit(
            ExerciseSession session,
            CampaignActionSubmission submission)
        {
            var result = inner.Submit(session, submission);
            return SubmissionOverride?.Invoke(session, submission, result) ?? result;
        }

        public ReconstructionProof Reconstruct(ExerciseSession session)
        {
            var proof = inner.Reconstruct(session);
            return ReconstructionOverride?.Invoke(session, proof) ?? proof;
        }
    }
}
