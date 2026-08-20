using System.Text.Json;
using Cna.Core.Exercises;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Commands;

public static class ExerciseRunCommand
{
    private const string Usage = "Usage: exercise run --manifest <repo-relative-path> --artifact-root <path>";

    public static ExerciseProcessExitCode Execute(
        string[] args,
        TextWriter standardOutput,
        TextWriter standardError,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(standardOutput);
        ArgumentNullException.ThrowIfNull(standardError);

        if (!TryParse(args, out var options))
        {
            standardError.WriteLine(Usage);
            return ExerciseProcessExitCode.ManifestInvalid;
        }

        var emptyChecks = new ExerciseCheckResults([]);
        ExerciseManifest manifest;
        byte[] normalizedManifest;
        string repositoryRoot;
        try
        {
            repositoryRoot = FindRepositoryRoot(Directory.GetCurrentDirectory());
            var manifestPath = ResolveManifestPath(repositoryRoot, options.ManifestPath);
            manifest = ExerciseManifestCodec.Deserialize(File.ReadAllBytes(manifestPath));
            normalizedManifest = ExerciseManifestCodec.Serialize(manifest);
        }
        catch (Exception exception) when (IsAdmissionFailure(exception))
        {
            var failure = ExerciseRunResult.Failed(
                ExerciseFailureCategory.ManifestInvalid,
                null);
            return WriteOrReport(
                options.ArtifactRoot,
                new ExerciseBundleWriteRequest(
                    ArtifactBundleProfile.FailedPreAdmission,
                    BasePayloads(failure, emptyChecks)),
                null,
                failure,
                standardOutput,
                standardError,
                $"Manifest admission failed: {exception.Message}");
        }

        var identityCapture = BuildIdentityCapture.Capture(new BuildIdentityCaptureRequest(
            manifest.BuildMode,
            repositoryRoot,
            normalizedManifest,
            manifest.RulesetHash,
            ExerciseConfigurationIdentity.ComputeHash(manifest),
            ExecutedArtifacts()));
        if (!identityCapture.IsCaptured)
        {
            var failure = ExerciseRunResult.Failed(
                ExerciseFailureCategory.BuildIdentityUnavailable,
                manifest.AssertFailureCategory);
            var payloads = BasePayloads(failure, emptyChecks);
            payloads.Add(ArtifactSchema.ExerciseManifestPath, normalizedManifest);
            return WriteOrReport(
                options.ArtifactRoot,
                new ExerciseBundleWriteRequest(ArtifactBundleProfile.FailedAdmitted, payloads),
                FailedWriteFallback(),
                failure,
                standardOutput,
                standardError,
                $"Build identity unavailable: {identityCapture.FailureReason}.");
        }

        var identity = identityCapture.Identity!;
        ExerciseExecutionResult execution;
        try
        {
            execution = ExerciseExecutor.Execute(manifest, cancellationToken);
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            var failure = ExerciseRunResult.Failed(
                ExerciseFailureCategory.UnexpectedFailure,
                manifest.AssertFailureCategory);
            var payloads = BasePayloads(failure, emptyChecks);
            payloads.Add(ArtifactSchema.ExerciseManifestPath, normalizedManifest);
            payloads.Add(ArtifactSchema.BuildIdentityPath, BuildIdentityCodec.Serialize(identity));
            return WriteOrReport(
                options.ArtifactRoot,
                new ExerciseBundleWriteRequest(ArtifactBundleProfile.FailedIdentified, payloads),
                FailedWriteFallback(),
                failure,
                standardOutput,
                standardError,
                $"Exercise execution failed unexpectedly: {exception.GetType().Name}.");
        }

        var runResult = execution.RunResult;
        var checks = execution.CheckResults;
        ReadjudicationProof? readjudication = null;
        if (execution.IsSucceeded)
        {
            readjudication = ReadjudicationVerifier.Verify(manifest, execution);
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
            identity,
            execution,
            runResult,
            checks,
            readjudication);
        return WriteOrReport(
            options.ArtifactRoot,
            primary,
            FailedWriteFallback(),
            runResult,
            standardOutput,
            standardError,
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
        ReadjudicationProof? readjudication)
    {
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
        payloads.Add(
            ArtifactSchema.DiagnosticsPath,
            ExerciseDiagnosticsWriter.Write(manifest, execution, runResult));

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
                ExerciseSummaryWriter.WriteMarkdown(manifest, execution, runResult, checks));
        }
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

    private static ExerciseProcessExitCode WriteOrReport(
        string artifactRoot,
        ExerciseBundleWriteRequest primary,
        ExerciseBundleWriteRequest? fallback,
        ExerciseRunResult runResult,
        TextWriter standardOutput,
        TextWriter standardError,
        string? failureMessage)
    {
        ExerciseBundleWriteOutcome outcome;
        try
        {
            outcome = ExerciseBundleWriter.TryWrite(artifactRoot, primary, fallback);
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            standardError.WriteLine($"Artifact finalization failed; no completed bundle exists: {exception.Message}");
            return ExerciseProcessExitCode.ArtifactFailed;
        }

        if (outcome.CompletedBundle is not null)
            standardOutput.WriteLine($"bundle={outcome.CompletedBundle.Path}");
        if (!outcome.IsPrimarySucceeded)
        {
            standardError.WriteLine(outcome.CompletedBundle is null
                ? "Artifact finalization failed; no completed bundle exists."
                : "Artifact finalization failed; a valid failed bundle was retained.");
            return ExerciseProcessExitCode.ArtifactFailed;
        }
        if (failureMessage is not null) standardError.WriteLine(failureMessage);
        return ExerciseExitCodeMapper.Map(runResult);
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
        var runnerAssembly = typeof(ExerciseRunCommand).Assembly.Location;
        var coreAssembly = typeof(CampaignExercises).Assembly.Location;
        yield return new BuildArtifactInput("Cna.Core.dll", coreAssembly, null);
        yield return new BuildArtifactInput("Cna.ExerciseRunner.dll", runnerAssembly, null);
        var dependencies = Path.ChangeExtension(runnerAssembly, ".deps.json");
        if (File.Exists(dependencies))
            yield return new BuildArtifactInput("Cna.ExerciseRunner.deps.json", dependencies, null);
    }

    private static bool TryParse(string[] args, out CommandOptions options)
    {
        options = default;
        if (args.Length != 6
            || !string.Equals(args[0], "exercise", StringComparison.Ordinal)
            || !string.Equals(args[1], "run", StringComparison.Ordinal))
            return false;

        string? manifest = null;
        string? artifactRoot = null;
        for (var index = 2; index < args.Length; index += 2)
        {
            if (string.IsNullOrWhiteSpace(args[index + 1])) return false;
            switch (args[index])
            {
                case "--manifest" when manifest is null:
                    manifest = args[index + 1];
                    break;
                case "--artifact-root" when artifactRoot is null:
                    artifactRoot = args[index + 1];
                    break;
                default:
                    return false;
            }
        }
        if (manifest is null || artifactRoot is null) return false;
        options = new CommandOptions(manifest, artifactRoot);
        return true;
    }

    private static string FindRepositoryRoot(string start)
    {
        for (var current = new DirectoryInfo(Path.GetFullPath(start)); current is not null;
             current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "Sandtable.slnx")))
                return current.FullName;
        }
        throw new InvalidDataException("The Sandtable repository root could not be found.");
    }

    private static string ResolveManifestPath(string repositoryRoot, string relativePath)
    {
        if (Path.IsPathRooted(relativePath))
            throw new InvalidDataException("The manifest path must be repository-relative.");
        var fullPath = Path.GetFullPath(relativePath, repositoryRoot);
        var relative = Path.GetRelativePath(repositoryRoot, fullPath);
        if (relative == ".."
            || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            throw new InvalidDataException("The manifest path escapes the repository.");
        RequireRegularPath(repositoryRoot, relative);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("The manifest does not exist.");
        if ((File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException("The manifest cannot be a symlink or reparse point.");
        return fullPath;
    }

    private static void RequireRegularPath(string repositoryRoot, string relativePath)
    {
        var current = repositoryRoot;
        foreach (var segment in relativePath.Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if ((File.Exists(current) || Directory.Exists(current))
                && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new InvalidDataException(
                    "The manifest path cannot traverse a symlink or reparse point.");
        }
    }

    private static bool IsAdmissionFailure(Exception exception) => exception is IOException
        or UnauthorizedAccessException
        or JsonException
        or ArgumentException;

    private static bool IsFatal(Exception exception) => exception is OutOfMemoryException
        or StackOverflowException
        or AccessViolationException;

    private readonly record struct CommandOptions(string ManifestPath, string ArtifactRoot);
}
