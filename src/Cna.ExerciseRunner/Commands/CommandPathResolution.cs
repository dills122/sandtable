using System.Text.Json;

namespace Cna.ExerciseRunner.Commands;

/// <summary>
/// Repository-root resolution, manifest-path validation, and command-line option parsing
/// shared by the run commands (<c>exercise run</c>, <c>maneuver run</c>). Extracted so the
/// admission logic — including which exceptions count as admission failures — cannot drift
/// between the two commands the way it previously did.
/// </summary>
internal static class CommandPathResolution
{
    internal static string FindRepositoryRoot(string start)
    {
        for (var current = new DirectoryInfo(Path.GetFullPath(start)); current is not null;
             current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "Sandtable.slnx")))
                return current.FullName;
        }
        throw new InvalidDataException("The Sandtable repository root could not be found.");
    }

    internal static string ResolveManifestPath(string repositoryRoot, string relativePath)
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

    /// <summary>
    /// Parses the <c>&lt;verb&gt; run --manifest &lt;path&gt; --artifact-root &lt;path&gt;</c>
    /// option grammar shared by both run commands. <paramref name="verb"/> is the one token
    /// that differs between callers (e.g. "exercise" vs. "maneuver").
    /// </summary>
    internal static bool TryParseManifestAndArtifactRootOptions(
        string[] args,
        string verb,
        out string manifestPath,
        out string artifactRoot)
    {
        manifestPath = string.Empty;
        artifactRoot = string.Empty;
        if (args.Length != 6
            || !string.Equals(args[0], verb, StringComparison.Ordinal)
            || !string.Equals(args[1], "run", StringComparison.Ordinal))
            return false;

        string? manifest = null;
        string? root = null;
        for (var index = 2; index < args.Length; index += 2)
        {
            if (string.IsNullOrWhiteSpace(args[index + 1])) return false;
            switch (args[index])
            {
                case "--manifest" when manifest is null:
                    manifest = args[index + 1];
                    break;
                case "--artifact-root" when root is null:
                    root = args[index + 1];
                    break;
                default:
                    return false;
            }
        }
        if (manifest is null || root is null) return false;
        manifestPath = manifest;
        artifactRoot = root;
        return true;
    }

    internal static bool IsAdmissionFailure(Exception exception) => exception is IOException
        or InvalidDataException
        or UnauthorizedAccessException
        or JsonException
        or ArgumentException;
}
