using System.Text.Json;
using System.Text.Json.Nodes;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Commands;
using Cna.ExerciseRunner.Tests.Artifacts;

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
    private readonly string repositoryManifestDirectory = Path.Combine(
        ".planning",
        "exercise-command-tests",
        Guid.NewGuid().ToString("N"));

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
            "sha256:38ed28be6562e5d5967d838b0d264c3b52bcae77a5e61d122a282b7b91c16f0b",
            buildIdentity.ConfigurationHash);
        Assert.NotEqual(buildIdentity.ManifestHash, buildIdentity.ConfigurationHash);
        Assert.All(SimulationEvidencePaths, path => Assert.True(File.Exists(
            Path.Combine(bundle.Path, path))));
    }

    [Theory]
    [InlineData(
        "rules-lab.organization.baseline.v2.json",
        "rules-lab.organization.v2.json",
        "organization-boundary",
        "land.position.operation-1.organization",
        8)]
    [InlineData(
        "rules-lab.reserve.baseline.v2.json",
        "rules-lab.reserve.v2.json",
        "reserve-boundary",
        "land.position.operation-1.first-player.reserve-designation",
        9)]
    [InlineData(
        "rules-lab.reserve-designation.baseline.v2.json",
        "rules-lab.reserve-designation.v2.json",
        "reserve-designation-movement",
        "land.position.operation-1.first-player.movement-and-combat.movement",
        12)]
    public void CheckedBaselineFixturesAreExactBuildPolicyTwinsOfExploratoryFixtures(
        string baselineFileName,
        string exploratoryFileName,
        string exerciseId,
        string terminalBoundary,
        int maximumSteps)
    {
        var repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);
        var baselineBytes = File.ReadAllBytes(Path.Combine(
            repositoryRoot,
            "scenarios/exercises",
            baselineFileName));
        var exploratoryBytes = File.ReadAllBytes(Path.Combine(
            repositoryRoot,
            "scenarios/exercises",
            exploratoryFileName));

        var manifest = ExerciseManifestCodec.Deserialize(baselineBytes);
        var baseline = JsonNode.Parse(baselineBytes)!.AsObject();
        var exploratory = JsonNode.Parse(exploratoryBytes)!.AsObject();
        baseline["buildMode"] = "exploratory";

        Assert.Equal(exerciseId, manifest.ExerciseId);
        Assert.Equal(terminalBoundary, manifest.TerminalBoundary);
        Assert.Equal(maximumSteps, manifest.MaximumSteps);
        Assert.Equal(ExerciseBuildMode.Baseline, manifest.BuildMode);
        Assert.Equal(ExerciseDetail.Compact, manifest.Detail);
        Assert.NotEmpty(ExerciseManifestCodec.Serialize(manifest));
        Assert.True(JsonNode.DeepEquals(exploratory, baseline));
    }

    [Fact]
    public void CheckedReserveFixtureRunsThroughStageEntryWithBothProofs()
    {
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

        var exitCode = ExerciseRunCommand.Execute(
            [
                "exercise",
                "run",
                "--manifest",
                "scenarios/exercises/rules-lab.reserve.v2.json",
                "--artifact-root",
                Path.Combine(temp, "reserve"),
            ],
            standardOutput,
            standardError,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExerciseProcessExitCode.Succeeded, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        var bundle = ExerciseBundleReader.Read(ParseBundlePath(standardOutput.ToString()));
        Assert.Equal("reserve-boundary", bundle.NormalizedManifest!.ExerciseId);
        Assert.Equal(
            "land.position.operation-1.first-player.reserve-designation",
            bundle.NormalizedManifest.TerminalBoundary);
        var completion = Assert.IsType<ExerciseSucceeded>(bundle.RunResult.Completion);
        Assert.Equal(
            bundle.NormalizedManifest.TerminalBoundary,
            Assert.IsType<BoundaryReached>(completion.Outcome).PositionId);
        Assert.Equal(9, bundle.AcceptedActions.Count);
        Assert.Equal(9, bundle.CanonicalEvents.Count);
        Assert.Equal(9, bundle.StepEvidence.Count);
        Assert.True(bundle.ReconstructionProof!.IsVerified);
        Assert.True(bundle.ReadjudicationProof!.IsVerified);
    }

    [Fact]
    public void CheckedReserveDesignationFixtureReachesMovementWithExactEvidence()
    {
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

        var exitCode = ExerciseRunCommand.Execute(
            [
                "exercise",
                "run",
                "--manifest",
                "scenarios/exercises/rules-lab.reserve-designation.v2.json",
                "--artifact-root",
                Path.Combine(temp, "reserve-designation"),
            ],
            standardOutput,
            standardError,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExerciseProcessExitCode.Succeeded, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        var bundle = ExerciseBundleReader.Read(ParseBundlePath(standardOutput.ToString()));
        Assert.Equal("reserve-designation-movement",
            bundle.NormalizedManifest!.ExerciseId);
        Assert.Equal(12, bundle.AcceptedActions.Count);
        Assert.Equal(12, bundle.CanonicalEvents.Count);
        Assert.Equal(12, bundle.StepEvidence.Count);
        var canonicalEvents = bundle.CanonicalEvents.Select(value =>
            System.Text.Encoding.UTF8.GetString(value)).ToArray();
        Assert.Equal(2, canonicalEvents.Count(value => value.Contains(
            "\"eventType\":\"reserve-element-designated\"",
            StringComparison.Ordinal)));
        Assert.Single(canonicalEvents, value => value.Contains(
            "\"eventType\":\"reserve-designation-completed\"",
            StringComparison.Ordinal));

        using var finalSnapshot = JsonDocument.Parse(bundle.FinalSnapshotBytes!);
        var statuses = finalSnapshot.RootElement.GetProperty("world")
            .GetProperty("elements")
            .EnumerateArray()
            .Select(element => element.GetProperty("reserveStatus").GetString())
            .ToArray();
        Assert.Equal(2, statuses.Count(value => value == "reserve-i"));
        Assert.Equal(2, statuses.Count(value => value == "none"));
        Assert.True(bundle.ReconstructionProof!.IsVerified);
        Assert.True(bundle.ReadjudicationProof!.IsVerified);
    }

    [Fact]
    public void CommandBoundaryPreservesEvidenceAcrossTiersAndEmitsOnlyDebugTrace()
    {
        var compact = RunDetail(ExerciseDetail.Compact);
        var forensic = RunDetail(ExerciseDetail.Forensic);
        var debug = RunDetail(ExerciseDetail.Debug);

        Assert.All(SimulationEvidencePaths, path =>
        {
            var expected = File.ReadAllBytes(Path.Combine(compact.BundlePath, path));
            Assert.Equal(expected, File.ReadAllBytes(Path.Combine(forensic.BundlePath, path)));
            Assert.Equal(expected, File.ReadAllBytes(Path.Combine(debug.BundlePath, path)));
        });
        Assert.DoesNotContain("trace=", compact.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain("trace=", forensic.StandardOutput, StringComparison.Ordinal);
        var traceLine = debug.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Single(line => line.StartsWith("trace=", StringComparison.Ordinal));
        using var trace = JsonDocument.Parse(traceLine["trace=".Length..]);
        Assert.Equal("exercise.artifact-finalized", trace.RootElement.GetProperty("event").GetString());
        Assert.True(trace.RootElement.GetProperty("readbackValidated").GetBoolean());
        Assert.Equal(15, trace.RootElement.GetProperty("payloadCount").GetInt32());
        Assert.True(trace.RootElement.GetProperty("elapsedMicroseconds").GetInt64() >= 0);
        Assert.True(CountDiagnosticRecords(compact.BundlePath)
            < CountDiagnosticRecords(forensic.BundlePath));
        Assert.True(CountDiagnosticRecords(forensic.BundlePath)
            < CountDiagnosticRecords(debug.BundlePath));
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
        var repositoryDirectory = Path.Combine(
            FindRepositoryRoot(AppContext.BaseDirectory),
            repositoryManifestDirectory);
        if (Directory.Exists(repositoryDirectory)) Directory.Delete(repositoryDirectory, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static string[] Arguments(string artifactRoot) =>
    [
        "exercise",
        "run",
        "--manifest",
        "scenarios/exercises/rules-lab.organization.v2.json",
        "--artifact-root",
        artifactRoot,
    ];

    private DetailCommandRun RunDetail(ExerciseDetail detail)
    {
        var repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);
        var manifestRelativePath = Path.Combine(
            repositoryManifestDirectory,
            $"{detail.ToString().ToLowerInvariant()}.json");
        var manifestPath = Path.Combine(repositoryRoot, manifestRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        File.WriteAllBytes(
            manifestPath,
            ExerciseManifestCodec.Serialize(ExerciseManifestCodecTests.Create(detail: detail)));
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = ExerciseRunCommand.Execute(
            [
                "exercise",
                "run",
                "--manifest",
                manifestRelativePath,
                "--artifact-root",
                Path.Combine(temp, detail.ToString().ToLowerInvariant()),
            ],
            output,
            error,
            TestContext.Current.CancellationToken);

        Assert.Equal(ExerciseProcessExitCode.Succeeded, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        return new DetailCommandRun(ParseBundlePath(output.ToString()), output.ToString());
    }

    private static int CountDiagnosticRecords(string bundlePath) =>
        File.ReadAllBytes(Path.Combine(bundlePath, ArtifactSchema.DiagnosticsPath))
            .Count(value => value == (byte)'\n');

    private static string ParseBundlePath(string standardOutput)
    {
        var line = standardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Single(value => value.StartsWith("bundle=", StringComparison.Ordinal));
        return line["bundle=".Length..];
    }

    private static string FindRepositoryRoot(string start)
    {
        for (var current = new DirectoryInfo(start); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "Sandtable.slnx")))
                return current.FullName;
        }
        throw new InvalidOperationException("The repository root was not found.");
    }

    private sealed record DetailCommandRun(string BundlePath, string StandardOutput);
}
