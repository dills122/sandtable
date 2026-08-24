using System.Diagnostics;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Execution;

internal sealed class ExerciseRunCoordinatorRequest
{
    private readonly byte[] normalizedManifest;

    internal ExerciseRunCoordinatorRequest(
        ExerciseManifest manifest,
        byte[] normalizedManifest,
        ExerciseRunIdentity runIdentity,
        string repositoryRoot,
        string artifactRoot,
        ExerciseDiagnosticTelemetry telemetry,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(normalizedManifest);
        ArgumentNullException.ThrowIfNull(runIdentity);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);
        ArgumentNullException.ThrowIfNull(telemetry);
        if (!normalizedManifest.AsSpan().SequenceEqual(ExerciseManifestCodec.Serialize(manifest)))
            throw new ArgumentException(
                "The normalized bytes must match the admitted Exercise manifest.",
                nameof(normalizedManifest));
        if (runIdentity.RootSeed != manifest.RootSeed)
            throw new ArgumentException(
                "The run identity root seed must match the admitted Exercise manifest.",
                nameof(runIdentity));

        Manifest = manifest;
        this.normalizedManifest = normalizedManifest.ToArray();
        RunIdentity = runIdentity;
        RepositoryRoot = repositoryRoot;
        ArtifactRoot = artifactRoot;
        CancellationToken = cancellationToken;
        Telemetry = telemetry;
    }

    internal ExerciseManifest Manifest { get; }
    internal byte[] NormalizedManifest => normalizedManifest.ToArray();
    internal ExerciseRunIdentity RunIdentity { get; }
    internal string RepositoryRoot { get; }
    internal string ArtifactRoot { get; }
    internal CancellationToken CancellationToken { get; }
    internal ExerciseDiagnosticTelemetry Telemetry { get; }
}

internal sealed record ExerciseArtifactFinalizationTrace(
    bool PrimarySucceeded,
    int PayloadCount,
    long LogicalBytes,
    long ElapsedMicroseconds);

internal sealed record ExerciseRunCoordinatorResult(
    ExerciseProcessExitCode ExitCode,
    string? CompletedBundlePath,
    string? FailureMessage,
    ExerciseArtifactFinalizationTrace? ArtifactTrace);

internal static class ExerciseRunCoordinator
{
    internal static ExerciseRunCoordinatorResult Execute(ExerciseRunCoordinatorRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var manifest = request.Manifest;
        var normalizedManifest = request.NormalizedManifest;
        var emptyChecks = new ExerciseCheckResults([]);

        var identityStarted = Stopwatch.GetTimestamp();
        var identityCapture = BuildIdentityCapture.Capture(new BuildIdentityCaptureRequest(
            manifest.BuildMode,
            request.RepositoryRoot,
            normalizedManifest,
            manifest.RulesetHash,
            ExerciseConfigurationIdentity.ComputeHash(manifest),
            ExecutedArtifacts()));
        request.Telemetry.RecordPhase(
            "build-identity",
            ElapsedMicroseconds(identityStarted));
        if (!identityCapture.IsCaptured)
        {
            var failure = ExerciseRunResult.Failed(
                ExerciseFailureCategory.BuildIdentityUnavailable,
                manifest.AssertFailureCategory);
            var payloads = BasePayloads(failure, emptyChecks);
            payloads.Add(ArtifactSchema.ExerciseManifestPath, normalizedManifest);
            return FinalizeBundle(
                request,
                new ExerciseBundleWriteRequest(
                    ArtifactBundleProfile.FailedAdmitted,
                    payloads),
                FailedWriteFallback(),
                failure,
                $"Build identity unavailable: {identityCapture.FailureReason}.");
        }

        var buildIdentity = identityCapture.Identity!;
        ExerciseExecutionResult execution;
        try
        {
            var executionStarted = Stopwatch.GetTimestamp();
            execution = ExerciseExecutor.Execute(
                manifest,
                request.RunIdentity,
                request.CancellationToken);
            request.Telemetry.RecordPhase(
                "exercise-execution",
                ElapsedMicroseconds(executionStarted));
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            var failure = ExerciseRunResult.Failed(
                ExerciseFailureCategory.UnexpectedFailure,
                manifest.AssertFailureCategory);
            var payloads = BasePayloads(failure, emptyChecks);
            payloads.Add(ArtifactSchema.ExerciseManifestPath, normalizedManifest);
            payloads.Add(
                ArtifactSchema.BuildIdentityPath,
                BuildIdentityCodec.Serialize(buildIdentity));
            return FinalizeBundle(
                request,
                new ExerciseBundleWriteRequest(
                    ArtifactBundleProfile.FailedIdentified,
                    payloads),
                FailedWriteFallback(),
                failure,
                $"Exercise execution failed unexpectedly: {exception.GetType().Name}.");
        }

        var runResult = execution.RunResult;
        var checks = execution.CheckResults;
        ReadjudicationProof? readjudication = null;
        if (execution.IsSucceeded)
        {
            var readjudicationStarted = Stopwatch.GetTimestamp();
            readjudication = ReadjudicationVerifier.Verify(manifest, execution);
            request.Telemetry.RecordPhase(
                "readjudication",
                ElapsedMicroseconds(readjudicationStarted));
            checks = checks.WithReadjudication(readjudication);
            if (!readjudication.IsVerified)
                runResult = ExerciseRunResult.Failed(
                    ExerciseFailureCategory.ReadjudicationMismatch,
                    manifest.AssertFailureCategory);
        }

        var profile = ProfileFor(execution, readjudication);
        var primary = CreateCompletedRequest(
            profile,
            manifest,
            normalizedManifest,
            buildIdentity,
            execution,
            runResult,
            checks,
            readjudication,
            request.Telemetry);
        return FinalizeBundle(
            request,
            primary,
            FailedWriteFallback(),
            runResult,
            runResult.Completion is ExerciseFailed
                ? $"Exercise failed: {ExerciseExitCodeMapper.Map(runResult)}."
                : null);
    }

    private static ExerciseBundleWriteRequest CreateCompletedRequest(
        ArtifactBundleProfile profile,
        ExerciseManifest manifest,
        byte[] normalizedManifest,
        BuildIdentity identity,
        ExerciseExecutionResult execution,
        ExerciseRunResult runResult,
        ExerciseCheckResults checks,
        ReadjudicationProof? readjudication,
        ExerciseDiagnosticTelemetry telemetry)
    {
        var preparationStarted = Stopwatch.GetTimestamp();
        var payloads = BasePayloads(runResult, checks);
        payloads.Add(ArtifactSchema.ExerciseManifestPath, normalizedManifest);
        payloads.Add(ArtifactSchema.BuildIdentityPath, BuildIdentityCodec.Serialize(identity));

        if (profile is ArtifactBundleProfile.FailedIdentified) return new(profile, payloads);

        payloads.Add(ArtifactSchema.SeedLedgerPath, SeedLedgerCodec.Serialize(execution.SeedLedger));
        payloads.Add(
            ArtifactSchema.AcceptedActionsPath,
            ExerciseEvidenceWriter.WriteAcceptedActions(execution));
        payloads.Add(
            ArtifactSchema.CanonicalEventsPath,
            ExerciseEvidenceWriter.WriteCanonicalEvents(execution));
        payloads.Add(
            ArtifactSchema.StepEvidencePath,
            ExerciseEvidenceWriter.WriteStepEvidence(execution));
        payloads.Add(ArtifactSchema.InitialSnapshotPath, execution.InitialSnapshot);
        payloads.Add(ArtifactSchema.FinalSnapshotPath, execution.FinalSnapshot);
        if (execution.Reconstruction is not null)
            payloads.Add(
                ArtifactSchema.ReconstructionProofPath,
                ReplayProofCodec.Serialize(execution.Reconstruction));
        if (readjudication is not null)
            payloads.Add(
                ArtifactSchema.ReadjudicationProofPath,
                ReplayProofCodec.Serialize(readjudication));
        if (profile == ArtifactBundleProfile.Succeeded)
        {
            payloads.Add(
                ArtifactSchema.SummaryJsonPath,
                ExerciseSummaryWriter.WriteJson(
                    manifest,
                    execution,
                    runResult,
                    checks,
                    readjudication));
            payloads.Add(
                ArtifactSchema.SummaryMarkdownPath,
                ExerciseSummaryWriter.WriteMarkdown(
                    manifest,
                    identity,
                    execution,
                    runResult,
                    checks,
                    readjudication));
        }
        telemetry.RecordPhase(
            "artifact-preparation",
            ElapsedMicroseconds(preparationStarted));
        telemetry.RecordPreparedPayloads(
            payloads.Count,
            payloads.Values.Sum(value => value.LongLength));
        payloads.Add(
            ArtifactSchema.DiagnosticsPath,
            ExerciseDiagnosticsWriter.Write(
                manifest,
                execution,
                runResult,
                checks,
                readjudication,
                telemetry));
        return new ExerciseBundleWriteRequest(profile, payloads);
    }

    private static ArtifactBundleProfile ProfileFor(
        ExerciseExecutionResult execution,
        ReadjudicationProof? readjudication)
    {
        if (execution.InitialSnapshot.Length == 0)
            return ArtifactBundleProfile.FailedIdentified;
        if (execution.Reconstruction is null)
            return ArtifactBundleProfile.FailedExecuted;
        if (!execution.Reconstruction.IsVerified)
            return ArtifactBundleProfile.FailedReconstructed;
        if (readjudication is null || !readjudication.IsVerified)
            return ArtifactBundleProfile.FailedReadjudicated;
        return ArtifactBundleProfile.Succeeded;
    }

    private static ExerciseRunCoordinatorResult FinalizeBundle(
        ExerciseRunCoordinatorRequest request,
        ExerciseBundleWriteRequest primary,
        ExerciseBundleWriteRequest? fallback,
        ExerciseRunResult runResult,
        string? failureMessage)
    {
        ExerciseBundleWriteOutcome outcome;
        var artifactStarted = Stopwatch.GetTimestamp();
        try
        {
            outcome = ExerciseBundleWriter.TryWrite(request.ArtifactRoot, primary, fallback);
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            return new ExerciseRunCoordinatorResult(
                ExerciseProcessExitCode.ArtifactFailed,
                null,
                $"Artifact finalization failed; no completed bundle exists: {exception.Message}",
                null);
        }

        var completedBundle = outcome.CompletedBundle;
        var trace = request.Manifest.Detail == ExerciseDetail.Debug && completedBundle is not null
            ? new ExerciseArtifactFinalizationTrace(
                outcome.IsPrimarySucceeded,
                completedBundle.Manifest.Files.Count,
                completedBundle.Manifest.Files.Sum(file => file.SizeBytes),
                ElapsedMicroseconds(artifactStarted))
            : null;
        if (!outcome.IsPrimarySucceeded)
        {
            return new ExerciseRunCoordinatorResult(
                ExerciseProcessExitCode.ArtifactFailed,
                completedBundle?.Path,
                completedBundle is null
                    ? "Artifact finalization failed; no completed bundle exists."
                    : "Artifact finalization failed; a valid failed bundle was retained.",
                trace);
        }
        return new ExerciseRunCoordinatorResult(
            ExerciseExitCodeMapper.Map(runResult),
            completedBundle?.Path,
            failureMessage,
            trace);
    }

    private static Dictionary<string, byte[]> BasePayloads(
        ExerciseRunResult result,
        ExerciseCheckResults checks) => new(StringComparer.Ordinal)
        {
            [ArtifactSchema.RunResultPath] = ExerciseRunResultCodec.Serialize(result),
            [ArtifactSchema.CheckResultsPath] = ExerciseCheckResultsCodec.Serialize(checks),
        };

    private static ExerciseBundleWriteRequest FailedWriteFallback()
    {
        var result = ExerciseRunResult.Failed(ExerciseFailureCategory.ArtifactFailed, null);
        return new ExerciseBundleWriteRequest(
            ArtifactBundleProfile.FailedPreAdmission,
            BasePayloads(result, new ExerciseCheckResults([])));
    }

    private static IEnumerable<BuildArtifactInput> ExecutedArtifacts()
    {
        var runnerAssembly = typeof(ExerciseRunCoordinator).Assembly.Location;
        var coreAssembly = typeof(CampaignExercises).Assembly.Location;
        yield return new BuildArtifactInput("Cna.Core.dll", coreAssembly, null);
        yield return new BuildArtifactInput("Cna.ExerciseRunner.dll", runnerAssembly, null);
        var dependencies = Path.ChangeExtension(runnerAssembly, ".deps.json");
        if (File.Exists(dependencies))
            yield return new BuildArtifactInput(
                "Cna.ExerciseRunner.deps.json",
                dependencies,
                null);
    }

    private static bool IsFatal(Exception exception) => exception is OutOfMemoryException
        or StackOverflowException
        or AccessViolationException;

    private static long ElapsedMicroseconds(long started) =>
        Math.Max(0, Stopwatch.GetElapsedTime(started).Ticks / 10);
}
