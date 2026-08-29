using System.Security.Cryptography;
using System.Text;
using Cna.Core.Exercises;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;
using Cna.ExerciseRunner.Tests.Artifacts;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class BuildIdentityCaptureTests
{
    private static readonly string[] ExpectedGitInvocations =
    [
        "status --porcelain=v1 -z --untracked-files=all",
        "rev-parse --verify HEAD^{commit}",
        "rev-parse --verify 1111111111111111111111111111111111111111^{tree}",
    ];

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CleanAttachedAndDetachedHeadAreBothBaselineEligible(bool symbolicHeadExists)
    {
        var environment = FakeEnvironment.Clean();
        environment.SymbolicHeadExists = symbolicHeadExists;

        var result = BuildIdentityCapture.Capture(Request(ExerciseBuildMode.Baseline), environment);

        Assert.True(result.IsCaptured);
        Assert.True(result.Identity!.BaselineEligible);
        Assert.True(result.Identity.Reproducible);
        Assert.False(result.Identity.Dirty);
        Assert.Equal(Hash([]), result.Identity.PorcelainSha256);
        Assert.Equal(ExpectedGitInvocations, environment.GitInvocations);
    }

    [Fact]
    public void DirtyBaselineFailsBeforeProducingAnIdentity()
    {
        var environment = FakeEnvironment.Dirty();

        var result = BuildIdentityCapture.Capture(Request(ExerciseBuildMode.Baseline), environment);

        Assert.False(result.IsCaptured);
        Assert.Equal(BuildIdentityFailureReason.DirtyBaseline, result.FailureReason);
        Assert.Null(result.Identity);
    }

    [Fact]
    public void ExplicitDirtyExplorationHashesRawPorcelainWithoutRetainingNames()
    {
        var environment = FakeEnvironment.Dirty();

        var result = BuildIdentityCapture.Capture(
            Request(ExerciseBuildMode.Exploratory),
            environment);

        Assert.True(result.IsCaptured);
        Assert.True(result.Identity!.Dirty);
        Assert.False(result.Identity.BaselineEligible);
        Assert.False(result.Identity.Reproducible);
        Assert.Equal(Hash(environment.StatusBytes), result.Identity.PorcelainSha256);
        Assert.DoesNotContain(
            "secret.env",
            Encoding.UTF8.GetString(BuildIdentityCodec.Serialize(result.Identity)),
            StringComparison.Ordinal);
    }

    [Fact]
    public void MissingGitAndArtifactMismatchFailClosed()
    {
        var unavailable = FakeEnvironment.Clean();
        unavailable.GitAvailable = false;
        var mismatch = FakeEnvironment.Clean();

        var unavailableResult = BuildIdentityCapture.Capture(
            Request(ExerciseBuildMode.Baseline),
            unavailable);
        var mismatchResult = BuildIdentityCapture.Capture(
            Request(ExerciseBuildMode.Baseline, Hash('f')),
            mismatch);

        Assert.False(unavailableResult.IsCaptured);
        Assert.Equal(BuildIdentityFailureReason.GitUnavailable, unavailableResult.FailureReason);
        Assert.False(mismatchResult.IsCaptured);
        Assert.Equal(BuildIdentityFailureReason.ArtifactMismatch, mismatchResult.FailureReason);
    }

    [Fact]
    public void RealRepositoryExploratoryCaptureRecordsExecutedAssemblies()
    {
        var repositoryRoot = FindRepositoryRoot();
        var runner = typeof(ExerciseExecutor).Assembly.Location;
        var core = typeof(CampaignExercises).Assembly.Location;
        var request = new BuildIdentityCaptureRequest(
            ExerciseBuildMode.Exploratory,
            repositoryRoot,
            ExerciseManifestCodec.Serialize(ExerciseManifestCodecTests.Create()),
            Cna1979Ruleset.Manifest.Hash,
            Hash('c'),
            [
                new BuildArtifactInput("Cna.Core.dll", core, null),
                new BuildArtifactInput("Cna.ExerciseRunner.dll", runner, null),
            ]);

        var result = BuildIdentityCapture.Capture(request);

        Assert.True(result.IsCaptured);
        Assert.Equal(2, result.Identity!.Artifacts.Count);
        Assert.All(result.Identity.Artifacts, artifact => Assert.StartsWith(
            "sha256:",
            artifact.Sha256,
            StringComparison.Ordinal));
    }

    private static BuildIdentityCaptureRequest Request(
        ExerciseBuildMode mode,
        string? expectedArtifactHash = null) => new(
            mode,
            "/repo",
            ExerciseManifestCodec.Serialize(ExerciseManifestCodecTests.Create()),
            Cna1979Ruleset.Manifest.Hash,
            Hash('c'),
            [new BuildArtifactInput("runner.dll", "/bin/runner.dll", expectedArtifactHash)]);

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            directory is not null;
            directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Sandtable.slnx")))
                return directory.FullName;
        }
        throw new DirectoryNotFoundException("Could not locate the Sandtable repository root.");
    }

    private static string Hash(byte[] value) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(value))}";

    private static string Hash(char value) => $"sha256:{new string(value, 64)}";

    private sealed class FakeEnvironment : IBuildIdentityEnvironment
    {
        private static readonly byte[] RunnerBytes = "runner-binary"u8.ToArray();

        internal bool GitAvailable { get; set; } = true;
        internal bool SymbolicHeadExists { get; set; }
        internal byte[] StatusBytes { get; private init; } = [];
        internal List<string> GitInvocations { get; } = [];

        public string FrameworkDescription => ".NET 10.0.11";
        public string OsArchitecture => "arm64";
        public string ProcessArchitecture => "arm64";

        public BuildIdentityProcessResult RunGit(string repositoryRoot, IReadOnlyList<string> arguments)
        {
            _ = repositoryRoot;
            var command = string.Join(' ', arguments);
            GitInvocations.Add(command);
            if (!GitAvailable) return new BuildIdentityProcessResult(false, 127, [], []);
            return command switch
            {
                "status --porcelain=v1 -z --untracked-files=all" =>
                    new BuildIdentityProcessResult(true, 0, StatusBytes, []),
                "rev-parse --verify HEAD^{commit}" =>
                    new BuildIdentityProcessResult(true, 0, Encoding.ASCII.GetBytes(
                        "1111111111111111111111111111111111111111\n"), []),
                "rev-parse --verify 1111111111111111111111111111111111111111^{tree}" =>
                    new BuildIdentityProcessResult(true, 0, Encoding.ASCII.GetBytes(
                        "2222222222222222222222222222222222222222\n"), []),
                _ => throw new InvalidOperationException($"Unexpected Git command: {command}"),
            };
        }

        public byte[] ReadFile(string path) => path == "/bin/runner.dll"
            ? RunnerBytes.ToArray()
            : throw new FileNotFoundException("Unknown fake file.", path);

        internal static FakeEnvironment Clean() => new();

        internal static FakeEnvironment Dirty() => new()
        {
            StatusBytes = "?? secret.env\0"u8.ToArray(),
        };
    }
}
