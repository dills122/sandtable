using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Execution;

public sealed record BuildArtifactInput
{
    public BuildArtifactInput(string name, string path, string? expectedSha256)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (expectedSha256 is not null)
            ReplayProofValidation.RequireSha256(expectedSha256, nameof(expectedSha256));
        Name = name;
        Path = path;
        ExpectedSha256 = expectedSha256;
    }

    public string Name { get; }
    public string Path { get; }
    public string? ExpectedSha256 { get; }
}

public sealed class BuildIdentityCaptureRequest
{
    private readonly byte[] normalizedManifest;

    public BuildIdentityCaptureRequest(
        ExerciseBuildMode buildMode,
        string repositoryRoot,
        byte[] normalizedManifest,
        string rulesetHash,
        string configurationHash,
        IEnumerable<BuildArtifactInput> artifacts)
    {
        if (!Enum.IsDefined(buildMode)) throw new ArgumentOutOfRangeException(nameof(buildMode));
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentNullException.ThrowIfNull(normalizedManifest);
        if (normalizedManifest.Length == 0)
            throw new ArgumentException("The normalized manifest cannot be empty.", nameof(normalizedManifest));
        if (!Cna1979Ruleset.IsCanonicalHash(rulesetHash))
            throw new ArgumentException("The ruleset hash is unsupported.", nameof(rulesetHash));
        ReplayProofValidation.RequireSha256(configurationHash, nameof(configurationHash));
        ArgumentNullException.ThrowIfNull(artifacts);
        var copy = artifacts.ToArray();
        if (copy.Length == 0 || copy.Any(value => value is null))
            throw new ArgumentException("At least one executed artifact is required.", nameof(artifacts));
        BuildMode = buildMode;
        RepositoryRoot = repositoryRoot;
        this.normalizedManifest = normalizedManifest.ToArray();
        RulesetHash = rulesetHash;
        ConfigurationHash = configurationHash;
        Artifacts = Array.AsReadOnly(copy);
    }

    public ExerciseBuildMode BuildMode { get; }
    public string RepositoryRoot { get; }
    public byte[] NormalizedManifest => normalizedManifest.ToArray();
    public string RulesetHash { get; }
    public string ConfigurationHash { get; }
    public IReadOnlyList<BuildArtifactInput> Artifacts { get; }
}

public enum BuildIdentityFailureReason
{
    None,
    GitUnavailable,
    HeadUnavailable,
    DirtyBaseline,
    ArtifactUnavailable,
    ArtifactMismatch,
}

public sealed class BuildIdentityCaptureResult
{
    private BuildIdentityCaptureResult(
        BuildIdentity? identity,
        BuildIdentityFailureReason failureReason)
    {
        Identity = identity;
        FailureReason = failureReason;
    }

    public bool IsCaptured => Identity is not null;
    public BuildIdentity? Identity { get; }
    public BuildIdentityFailureReason FailureReason { get; }

    internal static BuildIdentityCaptureResult Captured(BuildIdentity identity) =>
        new(identity, BuildIdentityFailureReason.None);

    internal static BuildIdentityCaptureResult Failed(BuildIdentityFailureReason reason)
    {
        if (reason == BuildIdentityFailureReason.None)
            throw new ArgumentOutOfRangeException(nameof(reason));
        return new BuildIdentityCaptureResult(null, reason);
    }
}

internal sealed record BuildIdentityProcessResult(
    bool IsStarted,
    int ExitCode,
    byte[] StandardOutput,
    byte[] StandardError);

internal interface IBuildIdentityEnvironment
{
    string FrameworkDescription { get; }
    string OsArchitecture { get; }
    string ProcessArchitecture { get; }
    BuildIdentityProcessResult RunGit(string repositoryRoot, IReadOnlyList<string> arguments);
    byte[] ReadFile(string path);
}

public static class BuildIdentityCapture
{
    public static BuildIdentityCaptureResult Capture(BuildIdentityCaptureRequest request) =>
        Capture(request, SystemBuildIdentityEnvironment.Instance);

    internal static BuildIdentityCaptureResult Capture(
        BuildIdentityCaptureRequest request,
        IBuildIdentityEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(environment);
        var status = environment.RunGit(
            request.RepositoryRoot,
            ["status", "--porcelain=v1", "-z", "--untracked-files=all"]);
        if (!status.IsStarted || status.ExitCode != 0)
            return BuildIdentityCaptureResult.Failed(BuildIdentityFailureReason.GitUnavailable);
        var dirty = status.StandardOutput.Length > 0;
        if (dirty && request.BuildMode == ExerciseBuildMode.Baseline)
            return BuildIdentityCaptureResult.Failed(BuildIdentityFailureReason.DirtyBaseline);

        var commit = ResolveGitObject(
            environment,
            request.RepositoryRoot,
            "HEAD^{commit}");
        var tree = ResolveGitObject(
            environment,
            request.RepositoryRoot,
            "HEAD^{tree}");
        if (commit is null || tree is null)
            return BuildIdentityCaptureResult.Failed(BuildIdentityFailureReason.HeadUnavailable);

        var artifacts = new List<BuildArtifactIdentity>();
        foreach (var input in request.Artifacts)
        {
            byte[] bytes;
            try
            {
                bytes = environment.ReadFile(input.Path);
            }
            catch (Exception exception) when (exception is IOException
                or UnauthorizedAccessException)
            {
                return BuildIdentityCaptureResult.Failed(
                    BuildIdentityFailureReason.ArtifactUnavailable);
            }
            var hash = Hash(bytes);
            if (input.ExpectedSha256 is not null
                && !string.Equals(input.ExpectedSha256, hash, StringComparison.Ordinal))
                return BuildIdentityCaptureResult.Failed(BuildIdentityFailureReason.ArtifactMismatch);
            artifacts.Add(new BuildArtifactIdentity(input.Name, bytes.LongLength, hash));
        }

        var identity = new BuildIdentity(
            request.BuildMode,
            commit,
            tree,
            dirty,
            Hash(status.StandardOutput),
            environment.FrameworkDescription,
            environment.OsArchitecture,
            environment.ProcessArchitecture,
            request.RulesetHash,
            request.ConfigurationHash,
            Hash(request.NormalizedManifest),
            ExerciseSeedLedger.SchemeId,
            request.BuildMode == ExerciseBuildMode.Baseline,
            !dirty,
            artifacts);
        return BuildIdentityCaptureResult.Captured(identity);
    }

    private static string? ResolveGitObject(
        IBuildIdentityEnvironment environment,
        string repositoryRoot,
        string revision)
    {
        var result = environment.RunGit(
            repositoryRoot,
            ["rev-parse", "--verify", revision]);
        if (!result.IsStarted || result.ExitCode != 0) return null;
        var value = Encoding.ASCII.GetString(result.StandardOutput).TrimEnd('\r', '\n');
        return value.Length is 40 or 64
            && value.All(character => character is >= '0' and <= '9'
                or >= 'a' and <= 'f')
            ? value
            : null;
    }

    private static string Hash(byte[] value) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(value))}";
}

internal sealed class SystemBuildIdentityEnvironment : IBuildIdentityEnvironment
{
    internal static SystemBuildIdentityEnvironment Instance { get; } = new();

    public string FrameworkDescription => RuntimeInformation.FrameworkDescription;
    public string OsArchitecture => RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
    public string ProcessArchitecture =>
        RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

    public BuildIdentityProcessResult RunGit(
        string repositoryRoot,
        IReadOnlyList<string> arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);
        try
        {
            using var process = Process.Start(start);
            if (process is null) return new BuildIdentityProcessResult(false, -1, [], []);
            using var standardOutput = new MemoryStream();
            using var standardError = new MemoryStream();
            var outputCopy = process.StandardOutput.BaseStream.CopyToAsync(standardOutput);
            var errorCopy = process.StandardError.BaseStream.CopyToAsync(standardError);
            Task.WhenAll(outputCopy, errorCopy).GetAwaiter().GetResult();
            process.WaitForExit();
            return new BuildIdentityProcessResult(
                true,
                process.ExitCode,
                standardOutput.ToArray(),
                standardError.ToArray());
        }
        catch (Exception exception) when (exception is InvalidOperationException
            or System.ComponentModel.Win32Exception
            or IOException
            or UnauthorizedAccessException)
        {
            return new BuildIdentityProcessResult(false, -1, [], []);
        }
    }

    public byte[] ReadFile(string path) => File.ReadAllBytes(path);
}
