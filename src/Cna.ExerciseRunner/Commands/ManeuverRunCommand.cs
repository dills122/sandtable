using System.Text.Json;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Commands;

public enum ManeuverProcessExitCode
{
    Succeeded = 0,
    ManifestInvalid = 2,
    ReportArtifactFailed = 11,
    UnexpectedFailure = 12,
    ExerciseFailed = 13,
    AggregationFailed = 14,
    Cancelled = 130,
}

internal sealed class ManeuverRunCommandDependencies
{
    internal ManeuverRunCommandDependencies(
        Func<ManeuverManifest, string, string, CancellationToken, ManeuverReport> execute,
        Func<string, ManeuverReport, ManeuverReportArtifact> writeReport)
    {
        Execute = execute ?? throw new ArgumentNullException(nameof(execute));
        WriteReport = writeReport ?? throw new ArgumentNullException(nameof(writeReport));
    }

    internal Func<ManeuverManifest, string, string, CancellationToken, ManeuverReport> Execute
    {
        get;
    }

    internal Func<string, ManeuverReport, ManeuverReportArtifact> WriteReport { get; }

    internal static ManeuverRunCommandDependencies Default { get; } = new(
        ManeuverExecutor.Execute,
        ManeuverReportWriter.Write);
}

public static class ManeuverRunCommand
{
    private const string Usage =
        "Usage: maneuver run --manifest <repo-relative-path> --artifact-root <path>";

    public static ManeuverProcessExitCode Execute(
        string[] args,
        TextWriter standardOutput,
        TextWriter standardError,
        CancellationToken cancellationToken) =>
        Execute(
            args,
            standardOutput,
            standardError,
            ManeuverRunCommandDependencies.Default,
            cancellationToken);

    internal static ManeuverProcessExitCode Execute(
        string[] args,
        TextWriter standardOutput,
        TextWriter standardError,
        ManeuverRunCommandDependencies dependencies,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(standardOutput);
        ArgumentNullException.ThrowIfNull(standardError);
        ArgumentNullException.ThrowIfNull(dependencies);

        if (!TryParse(args, out var options))
        {
            standardError.WriteLine(Usage);
            return ManeuverProcessExitCode.ManifestInvalid;
        }

        ManeuverManifest manifest;
        string repositoryRoot;
        try
        {
            repositoryRoot = CommandPathResolution.FindRepositoryRoot(Directory.GetCurrentDirectory());
            var manifestPath = CommandPathResolution.ResolveManifestPath(repositoryRoot, options.ManifestPath);
            manifest = ManeuverManifestCodec.Deserialize(File.ReadAllBytes(manifestPath));
        }
        catch (Exception exception) when (CommandPathResolution.IsAdmissionFailure(exception))
        {
            standardError.WriteLine($"Maneuver admission failed: {exception.Message}");
            return ManeuverProcessExitCode.ManifestInvalid;
        }

        ManeuverReport report;
        try
        {
            report = dependencies.Execute(
                manifest,
                repositoryRoot,
                options.ArtifactRoot,
                cancellationToken);
            if (report is null)
                throw new InvalidOperationException("The Maneuver executor returned no report.");
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            standardError.WriteLine(
                $"Maneuver execution failed unexpectedly: {exception.Message}");
            return ManeuverProcessExitCode.UnexpectedFailure;
        }

        ManeuverReportArtifact artifact;
        try
        {
            artifact = dependencies.WriteReport(options.ArtifactRoot, report);
            if (artifact is null)
                throw new InvalidDataException("The Maneuver report writer returned no artifact.");
        }
        catch (Exception exception) when (!IsFatal(exception))
        {
            standardError.WriteLine(
                "Maneuver report finalization failed; no completed report was returned: "
                + exception.Message);
            return ManeuverProcessExitCode.ReportArtifactFailed;
        }

        WriteCompletedOutput(standardOutput, artifact);
        return WriteStatusAndMapExit(standardError, artifact.Report.Deterministic.Status);
    }

    private static void WriteCompletedOutput(
        TextWriter standardOutput,
        ManeuverReportArtifact artifact)
    {
        var deterministic = artifact.Report.Deterministic;
        for (var ordinal = 0; ordinal < deterministic.Entries.Count; ordinal++)
        {
            if (deterministic.Entries[ordinal].Status is not (
                    ManeuverEntryStatus.Succeeded or ManeuverEntryStatus.Failed))
                continue;
            var path = artifact.Report.Diagnostics.Entries[ordinal].ObservedBundlePath;
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidDataException(
                    "A validated Maneuver entry has no completed Exercise bundle path.");
            standardOutput.WriteLine($"exerciseBundle[{ordinal}]={path}");
        }
        standardOutput.WriteLine($"report={artifact.Path}");
        standardOutput.WriteLine($"reportFingerprint={artifact.Report.ReportFingerprint}");
    }

    private static ManeuverProcessExitCode WriteStatusAndMapExit(
        TextWriter standardError,
        ManeuverReportStatus status)
    {
        switch (status)
        {
            case ManeuverReportStatus.Succeeded:
                return ManeuverProcessExitCode.Succeeded;
            case ManeuverReportStatus.ExerciseFailed:
                standardError.WriteLine(
                    "Maneuver completed with one or more Exercise failures.");
                return ManeuverProcessExitCode.ExerciseFailed;
            case ManeuverReportStatus.AggregationFailed:
                standardError.WriteLine(
                    "Maneuver aggregation failed; trusted child evidence was unavailable.");
                return ManeuverProcessExitCode.AggregationFailed;
            case ManeuverReportStatus.Cancelled:
                standardError.WriteLine("Maneuver cancelled.");
                return ManeuverProcessExitCode.Cancelled;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
    }

    private static bool TryParse(string[] args, out CommandOptions options)
    {
        options = default;
        if (!CommandPathResolution.TryParseManifestAndArtifactRootOptions(
                args, "maneuver", out var manifest, out var artifactRoot))
            return false;
        options = new CommandOptions(manifest, artifactRoot);
        return true;
    }

    private static bool IsFatal(Exception exception) => exception is OutOfMemoryException
        or StackOverflowException
        or AccessViolationException
        or AppDomainUnloadedException
        or BadImageFormatException;

    private readonly record struct CommandOptions(string ManifestPath, string ArtifactRoot);
}
