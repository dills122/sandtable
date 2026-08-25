using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Commands;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Commands;

public sealed class ManeuverRunCommandTests : IDisposable
{
    private const string FixturePath = "scenarios/maneuvers/rules-lab.serial.v1.json";
    private const string Boundary = "land.position.operation-1.organization";
    private readonly string temp = Path.Combine(
        Path.GetTempPath(),
        $"sandtable-maneuver-cli-{Guid.NewGuid():N}");
    private readonly string repositoryManifestDirectory = Path.Combine(
        ".planning",
        "maneuver-command-tests",
        Guid.NewGuid().ToString("N"));

    [Fact]
    public void CheckedFixtureRunsTwoUniqueExercisesAndPrintsOnlyValidatedPathsInOrder()
    {
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

        var exitCode = ManeuverRunCommand.Execute(
            Arguments(FixturePath, Path.Combine(temp, "success")),
            standardOutput,
            standardError,
            TestContext.Current.CancellationToken);

        Assert.Equal(ManeuverProcessExitCode.Succeeded, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        var output = ParseOutput(standardOutput.ToString(), expectedExerciseBundles: 2);
        var first = ExerciseBundleReader.Read(output.ExerciseBundlePaths[0]);
        var second = ExerciseBundleReader.Read(output.ExerciseBundlePaths[1]);
        Assert.Equal("organization-boundary.first", first.NormalizedManifest!.ExerciseId);
        Assert.Equal("organization-boundary.second", second.NormalizedManifest!.ExerciseId);
        var artifact = ManeuverReportReader.Read(output.ReportPath);
        Assert.Equal(output.ReportFingerprint, artifact.Report.ReportFingerprint);
        Assert.Equal(ManeuverReportStatus.Succeeded, artifact.Report.Deterministic.Status);
        Assert.Equal(2, artifact.Report.Deterministic.Counts.SucceededExerciseCount);
    }

    [Fact]
    public void MixedSuccessAndExerciseFailureRetainsBothBundlesAndReturnsThirteen()
    {
        var relativePath = WriteManifest(Manifest(
            Exercise("organization-boundary.first", 8, null),
            Exercise(
                "organization-boundary.second",
                1,
                ExerciseFailureCategory.StepLimitExceeded)));
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

        var exitCode = ManeuverRunCommand.Execute(
            Arguments(relativePath, Path.Combine(temp, "mixed")),
            standardOutput,
            standardError,
            TestContext.Current.CancellationToken);

        Assert.Equal(ManeuverProcessExitCode.ExerciseFailed, exitCode);
        Assert.Equal(
            "Maneuver completed with one or more Exercise failures.\n",
            NormalizeNewlines(standardError.ToString()));
        var output = ParseOutput(standardOutput.ToString(), expectedExerciseBundles: 2);
        var report = ManeuverReportReader.Read(output.ReportPath).Report;
        Assert.Equal(ManeuverReportStatus.ExerciseFailed, report.Deterministic.Status);
        Assert.Equal(1, report.Deterministic.Counts.SucceededExerciseCount);
        Assert.Equal(1, report.Deterministic.Counts.FailedExerciseCount);
        Assert.Equal(
            ExerciseFailureCategory.StepLimitExceeded,
            report.Deterministic.Entries[1].FailureCategory);
    }

    [Fact]
    public void CancellationBeforeFirstChildWritesAReportWithoutInventingBundlePaths()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

#pragma warning disable xUnit1051 // An already-cancelled token is the behavior under test.
        var exitCode = ManeuverRunCommand.Execute(
            Arguments(FixturePath, Path.Combine(temp, "cancelled")),
            standardOutput,
            standardError,
            cancellation.Token);
#pragma warning restore xUnit1051

        Assert.Equal(ManeuverProcessExitCode.Cancelled, exitCode);
        Assert.Equal("Maneuver cancelled.\n", NormalizeNewlines(standardError.ToString()));
        var output = ParseOutput(standardOutput.ToString(), expectedExerciseBundles: 0);
        var report = ManeuverReportReader.Read(output.ReportPath).Report;
        Assert.Equal(ManeuverReportStatus.Cancelled, report.Deterministic.Status);
        Assert.All(report.Deterministic.Entries, entry =>
            Assert.Equal(ManeuverNotRunReason.Cancelled, entry.NotRunReason));
    }

    [Fact]
    public void CorruptChildEvidenceProducesAggregateFailureWithoutClaimingAChildPath()
    {
        var dependencies = new ManeuverRunCommandDependencies(
            (manifest, repositoryRoot, artifactRoot, cancellationToken) =>
                ManeuverExecutor.Execute(
                    manifest,
                    repositoryRoot,
                    artifactRoot,
                    new ManeuverExecutionDependencies(
                        _ => new ExerciseRunCoordinatorResult(
                            ExerciseProcessExitCode.Succeeded,
                            "/corrupt/child",
                            null,
                            null),
                        _ => throw new InvalidDataException("injected corrupt bundle")),
                    cancellationToken),
            ManeuverReportWriter.Write);
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

        var exitCode = ManeuverRunCommand.Execute(
            Arguments(FixturePath, Path.Combine(temp, "aggregate-failed")),
            standardOutput,
            standardError,
            dependencies,
            TestContext.Current.CancellationToken);

        Assert.Equal(ManeuverProcessExitCode.AggregationFailed, exitCode);
        Assert.Equal(
            "Maneuver aggregation failed; trusted child evidence was unavailable.\n",
            NormalizeNewlines(standardError.ToString()));
        var output = ParseOutput(standardOutput.ToString(), expectedExerciseBundles: 0);
        var report = ManeuverReportReader.Read(output.ReportPath).Report;
        Assert.Equal(ManeuverReportStatus.AggregationFailed, report.Deterministic.Status);
        Assert.Equal(
            ManeuverAggregationFailureCategory.BundleInvalid,
            report.Deterministic.Entries[0].AggregationFailureCategory);
        Assert.Equal(
            ManeuverNotRunReason.AggregationStopped,
            report.Deterministic.Entries[1].NotRunReason);
    }

    [Fact]
    public void CorruptReportReadbackReturnsArtifactFailureAndPrintsNoPaths()
    {
        var dependencies = new ManeuverRunCommandDependencies(
            ManeuverExecutor.Execute,
            (artifactRoot, report) => ManeuverReportWriter.Write(
                artifactRoot,
                report,
                (point, _, finalPath) =>
                {
                    if (point == ManeuverReportWriterFailpoint.BeforeReadback)
                        File.AppendAllText(
                            Path.Combine(finalPath, ManeuverReportReader.FileName),
                            " ");
                },
                "corrupt-report"));
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

        var exitCode = ManeuverRunCommand.Execute(
            Arguments(FixturePath, Path.Combine(temp, "report-failed")),
            standardOutput,
            standardError,
            dependencies,
            TestContext.Current.CancellationToken);

        Assert.Equal(ManeuverProcessExitCode.ReportArtifactFailed, exitCode);
        Assert.Equal(string.Empty, standardOutput.ToString());
        Assert.Equal(
            "Maneuver report finalization failed; no completed report was returned: The Maneuver report is not trusted.\n",
            NormalizeNewlines(standardError.ToString()));
        Assert.True(File.Exists(Path.Combine(
            temp,
            "report-failed",
            "maneuvers",
            "succeeded",
            "corrupt-report",
            ManeuverReportReader.FileName)));
    }

    [Fact]
    public void InvalidUsageAndManifestFailBeforeExecutingOrWritingAReport()
    {
        var usageError = new StringWriter();
        var invalidManifestError = new StringWriter();

        var usageExit = ManeuverRunCommand.Execute(
            ["maneuver", "run", "--unknown", "value"],
            new StringWriter(),
            usageError,
            TestContext.Current.CancellationToken);
        var manifestExit = ManeuverRunCommand.Execute(
            Arguments("README.md", Path.Combine(temp, "invalid")),
            new StringWriter(),
            invalidManifestError,
            TestContext.Current.CancellationToken);

        Assert.Equal(ManeuverProcessExitCode.ManifestInvalid, usageExit);
        Assert.Equal(
            "Usage: maneuver run --manifest <repo-relative-path> --artifact-root <path>\n",
            NormalizeNewlines(usageError.ToString()));
        Assert.Equal(ManeuverProcessExitCode.ManifestInvalid, manifestExit);
        Assert.StartsWith(
            "Maneuver admission failed: ",
            NormalizeNewlines(invalidManifestError.ToString()),
            StringComparison.Ordinal);
        Assert.False(Directory.Exists(Path.Combine(temp, "invalid")));
    }

    [Fact]
    public void AbsoluteAndTraversingManifestPathsFailBeforeExecution()
    {
        var repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);
        var paths = new[]
        {
            Path.Combine(repositoryRoot, FixturePath),
            Path.Combine("..", "outside-maneuver.json"),
        };

        foreach (var path in paths)
        {
            var artifactRoot = Path.Combine(temp, Guid.NewGuid().ToString("N"));
            var exitCode = ManeuverRunCommand.Execute(
                Arguments(path, artifactRoot),
                new StringWriter(),
                new StringWriter(),
                TestContext.Current.CancellationToken);

            Assert.Equal(ManeuverProcessExitCode.ManifestInvalid, exitCode);
            Assert.False(Directory.Exists(artifactRoot));
        }
    }

    [Fact]
    public void SymlinkedManifestPathFailsBeforeExecution()
    {
        var repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);
        var relativePath = Path.Combine(repositoryManifestDirectory, "linked-maneuver.json");
        var linkPath = Path.Combine(repositoryRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(linkPath)!);
        File.CreateSymbolicLink(linkPath, Path.Combine(repositoryRoot, FixturePath));
        var artifactRoot = Path.Combine(temp, "symlinked-manifest");

        var exitCode = ManeuverRunCommand.Execute(
            Arguments(relativePath, artifactRoot),
            new StringWriter(),
            new StringWriter(),
            TestContext.Current.CancellationToken);

        Assert.Equal(ManeuverProcessExitCode.ManifestInvalid, exitCode);
        Assert.False(Directory.Exists(artifactRoot));
    }

    [Fact]
    public void UnexpectedExecutionFailureReturnsTwelveWithoutWritingAReport()
    {
        var dependencies = new ManeuverRunCommandDependencies(
            (_, _, _, _) => throw new InvalidOperationException("injected execution failure"),
            ManeuverReportWriter.Write);
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

        var exitCode = ManeuverRunCommand.Execute(
            Arguments(FixturePath, Path.Combine(temp, "unexpected")),
            standardOutput,
            standardError,
            dependencies,
            TestContext.Current.CancellationToken);

        Assert.Equal(ManeuverProcessExitCode.UnexpectedFailure, exitCode);
        Assert.Equal(string.Empty, standardOutput.ToString());
        Assert.Equal(
            "Maneuver execution failed unexpectedly: injected execution failure\n",
            NormalizeNewlines(standardError.ToString()));
        Assert.False(Directory.Exists(Path.Combine(temp, "unexpected")));
    }

    [Fact]
    public void ProgramRoutingPreservesTheExistingExerciseRunCommand()
    {
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

        var exitCode = Cna.ExerciseRunner.Program.Execute(
            [
                "exercise",
                "run",
                "--manifest",
                "scenarios/exercises/rules-lab.organization.v1.json",
                "--artifact-root",
                Path.Combine(temp, "exercise-route"),
            ],
            standardOutput,
            standardError,
            TestContext.Current.CancellationToken);

        Assert.Equal((int)ExerciseProcessExitCode.Succeeded, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        Assert.Single(standardOutput.ToString().Split(
            '\n',
            StringSplitOptions.RemoveEmptyEntries));
        Assert.StartsWith("bundle=", standardOutput.ToString(), StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(temp)) Directory.Delete(temp, recursive: true);
        var repositoryDirectory = Path.Combine(
            FindRepositoryRoot(AppContext.BaseDirectory),
            repositoryManifestDirectory);
        if (Directory.Exists(repositoryDirectory))
            Directory.Delete(repositoryDirectory, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static string[] Arguments(string manifestPath, string artifactRoot) =>
    [
        "maneuver",
        "run",
        "--manifest",
        manifestPath,
        "--artifact-root",
        artifactRoot,
    ];

    private string WriteManifest(ManeuverManifest manifest)
    {
        var repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);
        var relativePath = Path.Combine(repositoryManifestDirectory, "maneuver.json");
        var path = Path.Combine(repositoryRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, ManeuverManifestCodec.Serialize(manifest));
        return relativePath;
    }

    private static ManeuverManifest Manifest(params ManeuverExerciseManifest[] exercises) => new(
        ManeuverManifest.CurrentContractVersion,
        ManeuverManifest.SchemeId,
        "rules-lab.serial",
        ManeuverMode.SerialUnpaired,
        0,
        new ManeuverReportOptions(ManeuverReportProfile.TrustedAuthority),
        exercises);

    private static ManeuverExerciseManifest Exercise(
        string exerciseId,
        int maximumSteps,
        ExerciseFailureCategory? expectedFailure) => new(
        ExerciseManifest.CurrentContractVersion,
        exerciseId,
        "rules-lab.initiative.predetermined",
        "sha256:c1688f8869ca66182b87f487ec34edbef617ff1158f7d8b0d3101fe3993978ef",
        "rules-lab.content.movement-contact.v1",
        "sha256:53d5b64f647251e3ac366c65f4ad05cae766afd7b70ee331d463e801496e2a99",
        "movement-contact-lab",
        Cna1979Ruleset.Manifest.Hash,
        Boundary,
        maximumSteps,
        ExerciseBuildMode.Exploratory,
        ExerciseConfidentiality.TrustedAuthority,
        ExerciseDetail.Forensic,
        new ExerciseControllerManifest(
            ExerciseControllerPolicy.FirstByActionId,
            ExerciseControllerPolicy.FirstByActionId,
            ExerciseControllerPolicy.FirstByActionId),
        expectedFailure);

    private static CommandOutput ParseOutput(string standardOutput, int expectedExerciseBundles)
    {
        var lines = NormalizeNewlines(standardOutput)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(expectedExerciseBundles + 2, lines.Length);
        var bundles = new string[expectedExerciseBundles];
        for (var ordinal = 0; ordinal < expectedExerciseBundles; ordinal++)
        {
            var prefix = $"exerciseBundle[{ordinal}]=";
            Assert.StartsWith(prefix, lines[ordinal], StringComparison.Ordinal);
            bundles[ordinal] = lines[ordinal][prefix.Length..];
        }
        var reportPrefix = "report=";
        var fingerprintPrefix = "reportFingerprint=";
        Assert.StartsWith(reportPrefix, lines[^2], StringComparison.Ordinal);
        Assert.StartsWith(fingerprintPrefix, lines[^1], StringComparison.Ordinal);
        return new CommandOutput(
            bundles,
            lines[^2][reportPrefix.Length..],
            lines[^1][fingerprintPrefix.Length..]);
    }

    private static string NormalizeNewlines(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal);

    private static string FindRepositoryRoot(string start)
    {
        for (var current = new DirectoryInfo(start); current is not null; current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "Sandtable.slnx")))
                return current.FullName;
        }
        throw new InvalidOperationException("The repository root was not found.");
    }

    private sealed record CommandOutput(
        string[] ExerciseBundlePaths,
        string ReportPath,
        string ReportFingerprint);
}
