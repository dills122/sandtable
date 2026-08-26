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
            repositoryRoot = FindRepositoryRoot(Directory.GetCurrentDirectory());
            var manifestPath = ResolveManifestPath(repositoryRoot, options.ManifestPath);
            manifest = ExerciseManifestCodec.Deserialize(File.ReadAllBytes(manifestPath));
            normalizedManifest = ExerciseManifestCodec.Serialize(manifest);
            telemetry.RecordPhase(
                "manifest-admission",
                ElapsedMicroseconds(admissionStarted));
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
        or InvalidDataException
        or UnauthorizedAccessException
        or JsonException
        or ArgumentException;

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
