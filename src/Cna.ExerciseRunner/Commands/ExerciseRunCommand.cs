using System.Diagnostics;
using System.Text;
using System.Text.Json;
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
        var telemetry = new ExerciseDiagnosticTelemetry();
        ExerciseManifest manifest;
        byte[] normalizedManifest;
        string repositoryRoot;
        var admissionStarted = Stopwatch.GetTimestamp();
        try
        {
            repositoryRoot = CommandPathResolution.FindRepositoryRoot(Directory.GetCurrentDirectory());
            var manifestPath = CommandPathResolution.ResolveManifestPath(repositoryRoot, options.ManifestPath);
            manifest = ExerciseManifestCodec.Deserialize(File.ReadAllBytes(manifestPath));
            normalizedManifest = ExerciseManifestCodec.Serialize(manifest);
            telemetry.RecordPhase(
                "manifest-admission",
                ElapsedMicroseconds(admissionStarted));
        }
        catch (Exception exception) when (CommandPathResolution.IsAdmissionFailure(exception))
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

        var runIdentity = ExerciseRunIdentity.Standalone(
            manifest.ExerciseId,
            manifest.RootSeed);
        var result = ExerciseRunCoordinator.Execute(new ExerciseRunCoordinatorRequest(
            manifest,
            normalizedManifest,
            runIdentity,
            repositoryRoot,
            options.ArtifactRoot,
            telemetry,
            cancellationToken));
        if (result.CompletedBundlePath is not null)
            standardOutput.WriteLine($"bundle={result.CompletedBundlePath}");
        if (result.ArtifactTrace is not null)
            WriteArtifactTrace(standardOutput, manifest, result.ArtifactTrace);
        if (result.FailureMessage is not null)
            standardError.WriteLine(result.FailureMessage);
        return result.ExitCode;
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

    private static bool TryParse(string[] args, out CommandOptions options)
    {
        options = default;
        if (!CommandPathResolution.TryParseManifestAndArtifactRootOptions(
                args, "exercise", out var manifest, out var artifactRoot))
            return false;
        options = new CommandOptions(manifest, artifactRoot);
        return true;
    }

    private static bool IsFatal(Exception exception) => exception is OutOfMemoryException
        or StackOverflowException
        or AccessViolationException;

    private static void WriteArtifactTrace(
        TextWriter output,
        ExerciseManifest manifest,
        ExerciseArtifactFinalizationTrace trace)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("event", "exercise.artifact-finalized");
            writer.WriteString("exerciseId", manifest.ExerciseId);
            writer.WriteBoolean("primarySucceeded", trace.PrimarySucceeded);
            writer.WriteBoolean("readbackValidated", true);
            writer.WriteNumber("payloadCount", trace.PayloadCount);
            writer.WriteNumber("logicalBytes", trace.LogicalBytes);
            writer.WriteNumber("elapsedMicroseconds", trace.ElapsedMicroseconds);
            writer.WriteEndObject();
        }
        output.WriteLine($"trace={Encoding.UTF8.GetString(stream.ToArray())}");
    }

    private static long ElapsedMicroseconds(long started) =>
        Math.Max(0, Stopwatch.GetElapsedTime(started).Ticks / 10);

    private readonly record struct CommandOptions(string ManifestPath, string ArtifactRoot);
}
