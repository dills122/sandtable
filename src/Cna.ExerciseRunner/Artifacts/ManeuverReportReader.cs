using System.Text.Json;

namespace Cna.ExerciseRunner.Artifacts;

public sealed class ManeuverReportArtifact
{
    private readonly byte[] canonicalBytes;

    internal ManeuverReportArtifact(string path, ManeuverReport report, byte[] canonicalBytes)
    {
        Path = path;
        Report = report;
        this.canonicalBytes = canonicalBytes.ToArray();
    }

    public string Path { get; }
    public ManeuverReport Report { get; }
    public byte[] CanonicalBytes => canonicalBytes.ToArray();
}

public static class ManeuverReportReader
{
    internal const string FileName = "maneuver-report.json";
    internal const string RootDirectoryName = "maneuvers";

    public static ManeuverReportArtifact Read(string reportDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportDirectory);
        try
        {
            var fullPath = Path.GetFullPath(reportDirectory);
            RequireRegularDirectory(fullPath, "The Maneuver report directory is invalid.");
            var statusDirectory = Directory.GetParent(fullPath)
                ?? throw new InvalidDataException(
                    "The Maneuver report has no status parent directory.");
            RequireRegularDirectory(
                statusDirectory.FullName,
                "The Maneuver status directory is invalid.");
            var maneuversDirectory = statusDirectory.Parent
                ?? throw new InvalidDataException(
                    "The Maneuver report is outside the Maneuver artifact tree.");
            RequireRegularDirectory(
                maneuversDirectory.FullName,
                "The Maneuver artifact directory is invalid.");
            if (!string.Equals(
                    maneuversDirectory.Name,
                    RootDirectoryName,
                    StringComparison.Ordinal))
                throw new InvalidDataException(
                    "The report is outside the Maneuver artifact directory.");
            var expectedStatus = statusDirectory.Name switch
            {
                "succeeded" => ManeuverReportStatus.Succeeded,
                "failed" => (ManeuverReportStatus?)null,
                _ => throw new InvalidDataException(
                    "The report is not in a final status directory."),
            };

            var entries = Directory.EnumerateFileSystemEntries(fullPath).ToArray();
            if (entries.Length != 1
                || !string.Equals(Path.GetFileName(entries[0]), FileName, StringComparison.Ordinal))
                throw new InvalidDataException(
                    "A completed Maneuver report directory must contain exactly one report file.");
            var reportPath = Path.Combine(fullPath, FileName);
            RequireRegularFile(reportPath);
            var canonicalBytes = File.ReadAllBytes(reportPath);
            var report = ManeuverReportCodec.Deserialize(canonicalBytes);
            var isSucceeded = report.Deterministic.Status == ManeuverReportStatus.Succeeded;
            if (expectedStatus.HasValue != isSucceeded
                || expectedStatus.HasValue && expectedStatus.Value != report.Deterministic.Status)
                throw new InvalidDataException(
                    "Maneuver report status does not match final placement.");
            return new ManeuverReportArtifact(fullPath, report, canonicalBytes);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException
            or UnauthorizedAccessException
            or JsonException
            or ArgumentException
            or ArithmeticException)
        {
            throw new InvalidDataException("The Maneuver report is not trusted.", exception);
        }
    }

    private static void RequireRegularDirectory(string path, string message)
    {
        if (!Directory.Exists(path)) throw new InvalidDataException(message);
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw new InvalidDataException(message);
    }

    private static void RequireRegularFile(string path)
    {
        if (!File.Exists(path))
            throw new InvalidDataException("The Maneuver report file is missing.");
        var attributes = File.GetAttributes(path);
        if ((attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) != 0)
            throw new InvalidDataException(
                "The Maneuver report must be a regular file, not a link or directory.");
    }
}
