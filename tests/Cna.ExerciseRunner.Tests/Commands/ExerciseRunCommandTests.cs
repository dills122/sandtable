using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Commands;

namespace Cna.ExerciseRunner.Tests.Commands;

public sealed class ExerciseRunCommandTests : IDisposable
{
    private static readonly string[] SimulationEvidencePaths =
    [
        ArtifactSchema.AcceptedActionsPath,
        ArtifactSchema.CanonicalEventsPath,
        ArtifactSchema.StepEvidencePath,
        ArtifactSchema.InitialSnapshotPath,
        ArtifactSchema.FinalSnapshotPath,
        ArtifactSchema.SeedLedgerPath,
        ArtifactSchema.CheckResultsPath,
        ArtifactSchema.ReconstructionProofPath,
        ArtifactSchema.ReadjudicationProofPath,
    ];

    private readonly string temp = Path.Combine(
        Path.GetTempPath(),
        $"sandtable-exercise-cli-{Guid.NewGuid():N}");

    [Fact]
    public void DocumentedCommandCreatesAReaderValidatedSuccessBundle()
    {
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

        var exitCode = ExerciseRunCommand.Execute(
            Arguments(Path.Combine(temp, "artifacts")),
            standardOutput,
            standardError,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExerciseProcessExitCode.Succeeded, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        var bundlePath = ParseBundlePath(standardOutput.ToString());
        var bundle = ExerciseBundleReader.Read(bundlePath);
        Assert.Equal(ArtifactBundleStatus.Succeeded, bundle.Manifest.Status);
        Assert.Equal(ArtifactBundleProfile.Succeeded, bundle.Manifest.Profile);
        Assert.Equal(15, bundle.Manifest.Files.Count);
        Assert.IsType<ExerciseSucceeded>(bundle.RunResult.Completion);
        var buildIdentity = BuildIdentityCodec.Deserialize(File.ReadAllBytes(
            Path.Combine(bundle.Path, ArtifactSchema.BuildIdentityPath)));
        Assert.Equal(
            "sha256:1a5b64805ccc6531434c3a37d3346c6e7797f2da132c020fd7f61e03870ee769",
            buildIdentity.ConfigurationHash);
        Assert.NotEqual(buildIdentity.ManifestHash, buildIdentity.ConfigurationHash);
        Assert.All(SimulationEvidencePaths, path => Assert.True(File.Exists(
            Path.Combine(bundle.Path, path))));
    }

    [Fact]
    public void TwoRunsHaveByteIdenticalSimulationEvidence()
    {
        var firstOutput = new StringWriter();
        var secondOutput = new StringWriter();

        var firstExit = ExerciseRunCommand.Execute(
            Arguments(Path.Combine(temp, "first")),
            firstOutput,
            new StringWriter(),
            TestContext.Current.CancellationToken);
        var secondExit = ExerciseRunCommand.Execute(
            Arguments(Path.Combine(temp, "second")),
            secondOutput,
            new StringWriter(),
            TestContext.Current.CancellationToken);

        Assert.Equal(ExerciseProcessExitCode.Succeeded, firstExit);
        Assert.Equal(ExerciseProcessExitCode.Succeeded, secondExit);
        var first = ParseBundlePath(firstOutput.ToString());
        var second = ParseBundlePath(secondOutput.ToString());
        Assert.All(SimulationEvidencePaths, path => Assert.Equal(
            File.ReadAllBytes(Path.Combine(first, path)),
            File.ReadAllBytes(Path.Combine(second, path))));
    }

    [Fact]
    public void UnknownOrMissingCommandArgumentsFailBeforeCreatingArtifacts()
    {
        var standardError = new StringWriter();

        var exitCode = ExerciseRunCommand.Execute(
            ["exercise", "run", "--unknown", "value"],
            new StringWriter(),
            standardError,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExerciseProcessExitCode.ManifestInvalid, exitCode);
        Assert.Contains("Usage:", standardError.ToString(), StringComparison.Ordinal);
        Assert.False(Directory.Exists(temp));
    }

    [Fact]
    public void CancellationRetainsAValidatedIdentifiedFailureBundle()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

#pragma warning disable xUnit1051 // An already-cancelled token is the behavior under test.
        var exitCode = ExerciseRunCommand.Execute(
            Arguments(Path.Combine(temp, "cancelled")),
            standardOutput,
            standardError,
            cancellation.Token);
#pragma warning restore xUnit1051

        Assert.Equal(ExerciseProcessExitCode.Cancelled, exitCode);
        var bundle = ExerciseBundleReader.Read(ParseBundlePath(standardOutput.ToString()));
        Assert.Equal(ArtifactBundleStatus.Failed, bundle.Manifest.Status);
        Assert.Equal(ArtifactBundleProfile.FailedIdentified, bundle.Manifest.Profile);
        var failure = Assert.IsType<ExerciseFailed>(bundle.RunResult.Completion);
        Assert.Equal(ExerciseFailureCategory.Cancelled, failure.Failure.Category);
        Assert.Contains("Exercise failed", standardError.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void InvalidManifestRetainsOnlyAPreAdmissionFailureBundle()
    {
        var standardOutput = new StringWriter();

        var exitCode = ExerciseRunCommand.Execute(
            [
                "exercise",
                "run",
                "--manifest",
                "README.md",
                "--artifact-root",
                Path.Combine(temp, "invalid"),
            ],
            standardOutput,
            new StringWriter(),
            TestContext.Current.CancellationToken);

        Assert.Equal(ExerciseProcessExitCode.ManifestInvalid, exitCode);
        var bundle = ExerciseBundleReader.Read(ParseBundlePath(standardOutput.ToString()));
        Assert.Equal(ArtifactBundleProfile.FailedPreAdmission, bundle.Manifest.Profile);
        Assert.Equal(2, bundle.Manifest.Files.Count);
        Assert.IsType<ExerciseFailed>(bundle.RunResult.Completion);
    }

    [Fact]
    public void ArtifactFailureMakesNoCompletedBundleClaimWhenNothingCanBeFinalized()
    {
        Directory.CreateDirectory(temp);
        var blockedRoot = Path.Combine(temp, "blocked-root");
        File.WriteAllText(blockedRoot, "not a directory");
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

        var exitCode = ExerciseRunCommand.Execute(
            Arguments(blockedRoot),
            standardOutput,
            standardError,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExerciseProcessExitCode.ArtifactFailed, exitCode);
        Assert.DoesNotContain("bundle=", standardOutput.ToString(), StringComparison.Ordinal);
        Assert.Contains(
            "no completed bundle exists",
            standardError.ToString(),
            StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static string[] Arguments(string artifactRoot) =>
    [
        "exercise",
        "run",
        "--manifest",
        "scenarios/exercises/rules-lab.organization.v1.json",
        "--artifact-root",
        artifactRoot,
    ];

    private static string ParseBundlePath(string standardOutput)
    {
        var line = standardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Single(value => value.StartsWith("bundle=", StringComparison.Ordinal));
        return line["bundle=".Length..];
    }
}
