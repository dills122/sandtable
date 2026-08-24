namespace Cna.ExerciseRunner.Artifacts;

public enum ManeuverReportWriterFailpoint
{
    StagingCreated,
    BeforeReportCreate,
    AfterReportCreate,
    BeforeReportWrite,
    AfterReportWrite,
    BeforeReportFlush,
    AfterReportFlush,
    BeforeMove,
    AfterMove,
    BeforeReadback,
    AfterReadback,
}

public static class ManeuverReportWriter
{
    public static ManeuverReportArtifact Write(string artifactRoot, ManeuverReport report) =>
        Write(
            artifactRoot,
            report,
            static (_, _, _) => { },
            Guid.NewGuid().ToString("N"));

    internal static ManeuverReportArtifact Write(
        string artifactRoot,
        ManeuverReport report,
        Action<ManeuverReportWriterFailpoint, string, string> observer,
        string runDirectoryId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(observer);
        RequireDirectoryId(runDirectoryId);

        var root = Path.GetFullPath(artifactRoot);
        RequireRoot(root);
        var maneuversRoot = CreateRegularDirectory(Path.Combine(
            root,
            ManeuverReportReader.RootDirectoryName));
        var partialRoot = CreateRegularDirectory(Path.Combine(maneuversRoot, ".partial"));
        var statusName = report.Deterministic.Status == ManeuverReportStatus.Succeeded
            ? "succeeded"
            : "failed";
        var statusRoot = CreateRegularDirectory(Path.Combine(maneuversRoot, statusName));
        var stagingPath = Path.Combine(partialRoot, runDirectoryId);
        var finalPath = Path.Combine(statusRoot, runDirectoryId);
        if (Directory.Exists(stagingPath) || File.Exists(stagingPath))
            throw new IOException("The unique Maneuver staging destination already exists.");
        if (Directory.Exists(finalPath) || File.Exists(finalPath))
            throw new IOException("The final Maneuver report destination already exists.");

        Directory.CreateDirectory(stagingPath);
        RequireRegularDirectory(stagingPath);
        observer(ManeuverReportWriterFailpoint.StagingCreated, stagingPath, finalPath);
        var reportPath = Path.Combine(stagingPath, ManeuverReportReader.FileName);
        var canonicalBytes = ManeuverReportCodec.Serialize(report);
        observer(ManeuverReportWriterFailpoint.BeforeReportCreate, stagingPath, finalPath);
        using (var stream = new FileStream(
            reportPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None))
        {
            observer(ManeuverReportWriterFailpoint.AfterReportCreate, stagingPath, finalPath);
            observer(ManeuverReportWriterFailpoint.BeforeReportWrite, stagingPath, finalPath);
            stream.Write(canonicalBytes);
            observer(ManeuverReportWriterFailpoint.AfterReportWrite, stagingPath, finalPath);
            observer(ManeuverReportWriterFailpoint.BeforeReportFlush, stagingPath, finalPath);
            stream.Flush(flushToDisk: true);
        }
        observer(ManeuverReportWriterFailpoint.AfterReportFlush, stagingPath, finalPath);

        observer(ManeuverReportWriterFailpoint.BeforeMove, stagingPath, finalPath);
        Directory.Move(stagingPath, finalPath);
        observer(ManeuverReportWriterFailpoint.AfterMove, stagingPath, finalPath);
        observer(ManeuverReportWriterFailpoint.BeforeReadback, stagingPath, finalPath);
        var artifact = ManeuverReportReader.Read(finalPath);
        observer(ManeuverReportWriterFailpoint.AfterReadback, stagingPath, finalPath);
        return artifact;
    }

    private static void RequireRoot(string root)
    {
        if (File.Exists(root))
            throw new InvalidDataException("The artifact root cannot be a file.");
        if (Directory.Exists(root)
            && (File.GetAttributes(root) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException(
                "The artifact root cannot be a symlink or reparse point.");
        Directory.CreateDirectory(root);
        RequireRegularDirectory(root);
    }

    private static string CreateRegularDirectory(string path)
    {
        if (File.Exists(path))
            throw new InvalidDataException("A Maneuver artifact directory cannot be a file.");
        Directory.CreateDirectory(path);
        RequireRegularDirectory(path);
        return path;
    }

    private static void RequireRegularDirectory(string path)
    {
        if (!Directory.Exists(path)
            || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException(
                "A Maneuver artifact directory cannot be a symlink or reparse point.");
    }

    private static void RequireDirectoryId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (Path.IsPathRooted(value)
            || value is "." or ".."
            || value.Contains('/')
            || value.Contains('\\')
            || !string.Equals(value, Path.GetFileName(value), StringComparison.Ordinal))
            throw new ArgumentException(
                "The run directory ID must be one local path segment.",
                nameof(value));
    }
}
