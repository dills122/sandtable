using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Commands;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Commands;

public sealed class ManeuverRunCommandTests : IDisposable
{
    private const string FixturePath = "scenarios/maneuvers/rules-lab.serial.v2.json";
    private const string StageEntryFixturePath =
        "scenarios/maneuvers/rules-lab.stage-entry.serial.v2.json";
    private const string ReserveDesignationFixturePath =
        "scenarios/maneuvers/rules-lab.reserve-designation.serial.v2.json";
    private const string ControllerMatrixFixturePath =
        "scenarios/maneuvers/rules-lab.controller-matrix.serial.v2.json";
    private const string MovementFixturePath =
        "scenarios/maneuvers/rules-lab.movement.serial.v2.json";
    private const string PairedFixturePath =
        "scenarios/maneuvers/rules-lab.reserve-policy.paired.v1.json";
    private const string Boundary = "land.position.operation-1.organization";
    private const string ReserveBoundary =
        "land.position.operation-1.first-player.reserve-designation";
    private const string MovementBoundary =
        "land.position.operation-1.first-player.movement-and-combat.movement";
    private const string BreakdownBoundary =
        "land.position.operation-1.first-player.movement-and-combat.breakdown-determination";
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
    public void CheckedStageEntryFixtureRunsBothSetupsToReserveAndAggregatesValidatedReport()
    {
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

        var exitCode = ManeuverRunCommand.Execute(
            Arguments(StageEntryFixturePath, Path.Combine(temp, "stage-entry")),
            standardOutput,
            standardError,
            TestContext.Current.CancellationToken);

        Assert.Equal(ManeuverProcessExitCode.Succeeded, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        var output = ParseOutput(standardOutput.ToString(), expectedExerciseBundles: 2);
        var bundles = output.ExerciseBundlePaths.Select(ExerciseBundleReader.Read).ToArray();
        Assert.Equal(
            ["reserve-boundary.predetermined", "reserve-boundary.contested"],
            bundles.Select(value => value.NormalizedManifest!.ExerciseId));
        Assert.Equal(
            ["rules-lab.initiative.predetermined", "rules-lab.initiative.contested"],
            bundles.Select(value => value.NormalizedManifest!.SetupId));
        Assert.All(bundles, bundle =>
        {
            Assert.Equal(9, bundle.AcceptedActions.Count);
            var completion = Assert.IsType<ExerciseSucceeded>(bundle.RunResult.Completion);
            Assert.Equal(
                ReserveBoundary,
                Assert.IsType<BoundaryReached>(completion.Outcome).PositionId);
        });

        var artifact = ManeuverReportReader.Read(output.ReportPath);
        Assert.Equal(output.ReportFingerprint, artifact.Report.ReportFingerprint);
        Assert.Equal(ManeuverReportStatus.Succeeded, artifact.Report.Deterministic.Status);
        Assert.Equal(2, artifact.Report.Deterministic.Counts.SucceededExerciseCount);
        var terminal = Assert.Single(artifact.Report.Deterministic.TerminalCounts);
        Assert.Equal(2, terminal.Count);
        Assert.Equal(
            ReserveBoundary,
            Assert.IsType<BoundaryReached>(terminal.Outcome).PositionId);
    }

    [Fact]
    public void CheckedReserveDesignationFixtureRunsBothSetupsToMovement()
    {
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

        var exitCode = ManeuverRunCommand.Execute(
            Arguments(
                ReserveDesignationFixturePath,
                Path.Combine(temp, "reserve-designation")),
            standardOutput,
            standardError,
            TestContext.Current.CancellationToken);

        Assert.Equal(ManeuverProcessExitCode.Succeeded, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        var output = ParseOutput(standardOutput.ToString(), expectedExerciseBundles: 2);
        var bundles = output.ExerciseBundlePaths.Select(ExerciseBundleReader.Read).ToArray();
        Assert.Equal(
            ["reserve-designation-movement.predetermined",
                "reserve-designation-movement.contested"],
            bundles.Select(value => value.NormalizedManifest!.ExerciseId));
        Assert.Equal(
            ["rules-lab.initiative.predetermined", "rules-lab.initiative.contested"],
            bundles.Select(value => value.NormalizedManifest!.SetupId));
        Assert.All(bundles, bundle =>
        {
            Assert.Equal(12, bundle.AcceptedActions.Count);
            Assert.Equal(2, bundle.CanonicalEvents.Count(value =>
                System.Text.Encoding.UTF8.GetString(value).Contains(
                    "\"eventType\":\"reserve-element-designated\"",
                    StringComparison.Ordinal)));
            Assert.Single(bundle.CanonicalEvents, value =>
                System.Text.Encoding.UTF8.GetString(value).Contains(
                    "\"eventType\":\"reserve-designation-completed\"",
                    StringComparison.Ordinal));
            var completion = Assert.IsType<ExerciseSucceeded>(bundle.RunResult.Completion);
            Assert.Equal(
                MovementBoundary,
                Assert.IsType<BoundaryReached>(completion.Outcome).PositionId);
            Assert.True(bundle.ReconstructionProof!.IsVerified);
            Assert.True(bundle.ReadjudicationProof!.IsVerified);
        });

        var artifact = ManeuverReportReader.Read(output.ReportPath);
        Assert.Equal(output.ReportFingerprint, artifact.Report.ReportFingerprint);
        Assert.Equal(ManeuverReportStatus.Succeeded, artifact.Report.Deterministic.Status);
        Assert.Equal(2, artifact.Report.Deterministic.Counts.SucceededExerciseCount);
        var terminal = Assert.Single(artifact.Report.Deterministic.TerminalCounts);
        Assert.Equal(2, terminal.Count);
        Assert.Equal(MovementBoundary,
            Assert.IsType<BoundaryReached>(terminal.Outcome).PositionId);
        Assert.Equal(
            "sha256:9621ee95f7b944f3cea226a9f00f63d782cc417f094543e34f8c36c683f68e1e",
            artifact.Report.ReportFingerprint);
    }

    [Fact]
    public void CheckedControllerMatrixFixtureRunsAllSixPoliciesToMovement()
    {
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

        var exitCode = ManeuverRunCommand.Execute(
            Arguments(
                ControllerMatrixFixturePath,
                Path.Combine(temp, "controller-matrix")),
            standardOutput,
            standardError,
            TestContext.Current.CancellationToken);

        Assert.Equal(ManeuverProcessExitCode.Succeeded, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        var output = ParseOutput(standardOutput.ToString(), expectedExerciseBundles: 6);
        var bundles = output.ExerciseBundlePaths.Select(ExerciseBundleReader.Read).ToArray();
        Assert.Equal(
            [
                "movement-entry.act-first.reserve-none",
                "movement-entry.act-first.reserve-one",
                "movement-entry.act-first.reserve-all",
                "movement-entry.act-last.reserve-none",
                "movement-entry.act-last.reserve-one",
                "movement-entry.act-last.reserve-all",
            ],
            bundles.Select(value => value.NormalizedManifest!.ExerciseId));
        Assert.Equal([10, 11, 12, 10, 11, 12],
            bundles.Select(value => value.AcceptedActions.Count));
        Assert.Equal([0, 1, 2, 0, 1, 2], bundles.Select(bundle =>
            bundle.CanonicalEvents.Count(value =>
                System.Text.Encoding.UTF8.GetString(value).Contains(
                    "\"eventType\":\"reserve-element-designated\"",
                    StringComparison.Ordinal))));
        for (var index = 0; index < bundles.Length; index++)
        {
            var bundle = bundles[index];
            var completion = Assert.IsType<ExerciseSucceeded>(bundle.RunResult.Completion);
            Assert.Equal(
                MovementBoundary,
                Assert.IsType<BoundaryReached>(completion.Outcome).PositionId);
            Assert.True(bundle.ReconstructionProof!.IsVerified);
            Assert.True(bundle.ReadjudicationProof!.IsVerified);
            using var final = System.Text.Json.JsonDocument.Parse(bundle.FinalSnapshotBytes!);
            var holder = final.RootElement.GetProperty("initiativeHolder").GetString();
            var firstSide = final.RootElement.GetProperty("operationStageOrders")[0]
                .GetProperty("firstSide").GetString();
            Assert.Equal(index < 3, string.Equals(holder, firstSide, StringComparison.Ordinal));
        }

        var artifact = ManeuverReportReader.Read(output.ReportPath);
        Assert.Equal(output.ReportFingerprint, artifact.Report.ReportFingerprint);
        Assert.Equal(ManeuverReportStatus.Succeeded, artifact.Report.Deterministic.Status);
        Assert.Equal(6, artifact.Report.Deterministic.Counts.SucceededExerciseCount);
        var terminal = Assert.Single(artifact.Report.Deterministic.TerminalCounts);
        Assert.Equal(6, terminal.Count);
        Assert.Equal(MovementBoundary,
            Assert.IsType<BoundaryReached>(terminal.Outcome).PositionId);
        Assert.Equal(
            "sha256:cab825d30b128ab1f1e2032879ca0ac3f793abc054a2c710dbdf22e93f49e71c",
            artifact.Report.ReportFingerprint);
    }

    [Fact]
    public void CheckedMovementFixtureRunsAllSixPoliciesToBreakdownWithExactEvidence()
    {
        var standardOutput = new StringWriter();
        var standardError = new StringWriter();

        var exitCode = ManeuverRunCommand.Execute(
            Arguments(MovementFixturePath, Path.Combine(temp, "movement")),
            standardOutput,
            standardError,
            TestContext.Current.CancellationToken);

        Assert.Equal(ManeuverProcessExitCode.Succeeded, exitCode);
        Assert.Equal(string.Empty, standardError.ToString());
        var output = ParseOutput(standardOutput.ToString(), expectedExerciseBundles: 6);
        var bundles = output.ExerciseBundlePaths.Select(ExerciseBundleReader.Read).ToArray();
        Assert.Equal(
            [
                "movement-execution.act-first.reserve-none",
                "movement-execution.act-first.reserve-one",
                "movement-execution.act-first.reserve-all",
                "movement-execution.act-last.reserve-none",
                "movement-execution.act-last.reserve-one",
                "movement-execution.act-last.reserve-all",
            ],
            bundles.Select(value => value.NormalizedManifest!.ExerciseId));
        Assert.All(bundles, bundle =>
        {
            Assert.Equal(13, bundle.AcceptedActions.Count);
            Assert.Equal(13, bundle.CanonicalEvents.Count);
            Assert.Equal(13, bundle.StepEvidence.Count);
            Assert.Equal(94, bundle.CheckResults.Results.Count);
            Assert.All(bundle.CheckResults.Results, result => Assert.True(result.IsPassed));
            Assert.True(bundle.ReconstructionProof!.IsVerified);
            Assert.True(bundle.ReadjudicationProof!.IsVerified);
            Assert.Single(bundle.CanonicalEvents, value => EventType(value) ==
                "movement-segment-completed");
            var completion = Assert.IsType<ExerciseSucceeded>(bundle.RunResult.Completion);
            Assert.Equal(
                BreakdownBoundary,
                Assert.IsType<BoundaryReached>(completion.Outcome).PositionId);
        });
        Assert.Equal([0, 1, 2, 0, 1, 2], bundles.Select(bundle =>
            bundle.CanonicalEvents.Count(value => EventType(value) ==
                "reserve-element-designated")));
        Assert.Equal([2, 1, 0, 2, 1, 0], bundles.Select(bundle =>
            bundle.CanonicalEvents.Count(value => EventType(value) == "element-moved")));

        AssertMovement(
            bundles[0],
            ("axis-element-a", "west", "center", 0, 8),
            ("axis-element-b", "north-west", "north", 0, 1));
        AssertMovement(
            bundles[1],
            ("axis-element-b", "north-west", "north", 0, 1));
        AssertMovement(bundles[2]);
        AssertMovement(
            bundles[3],
            ("commonwealth-element-a", "east", "center", 0, 8),
            ("commonwealth-element-b", "south-east", "east", 0, 1));
        AssertMovement(
            bundles[4],
            ("commonwealth-element-b", "south-east", "east", 0, 1));
        AssertMovement(bundles[5]);

        var artifact = ManeuverReportReader.Read(output.ReportPath);
        Assert.Equal(output.ReportFingerprint, artifact.Report.ReportFingerprint);
        Assert.Equal(ManeuverReportStatus.Succeeded, artifact.Report.Deterministic.Status);
        Assert.Equal(6, artifact.Report.Deterministic.Counts.SucceededExerciseCount);
        var terminal = Assert.Single(artifact.Report.Deterministic.TerminalCounts);
        Assert.Equal(6, terminal.Count);
        Assert.Equal(
            BreakdownBoundary,
            Assert.IsType<BoundaryReached>(terminal.Outcome).PositionId);
        Assert.Equal(
            "sha256:c1c20270dcd3402886931c28851bea7f23cd1e0778b45f94c43d85ed01d41c4b",
            artifact.Report.ReportFingerprint);
    }

    [Fact]
    public void CheckedPairedFixtureProvesEqualInitialEvidenceAndRepeatableHonestDivergence()
    {
        CommandOutput Run(string directory)
        {
            var standardOutput = new StringWriter();
            var standardError = new StringWriter();
            var exitCode = ManeuverRunCommand.Execute(
                Arguments(PairedFixturePath, Path.Combine(temp, directory)),
                standardOutput,
                standardError,
                TestContext.Current.CancellationToken);
            Assert.Equal(ManeuverProcessExitCode.Succeeded, exitCode);
            Assert.Equal(string.Empty, standardError.ToString());
            return ParseOutput(standardOutput.ToString(), expectedExerciseBundles: 2);
        }

        var firstOutput = Run("paired-first");
        var secondOutput = Run("paired-second");
        var bundles = firstOutput.ExerciseBundlePaths.Select(ExerciseBundleReader.Read).ToArray();
        Assert.Equal(["reserve-policy.baseline", "reserve-policy.candidate"],
            bundles.Select(value => value.NormalizedManifest!.ExerciseId));
        Assert.Equal([10, 12], bundles.Select(value => value.AcceptedActions.Count));
        Assert.Equal(bundles[0].InitialSnapshotBytes, bundles[1].InitialSnapshotBytes);
        Assert.Equal(
            SeedLedgerCodec.Serialize(bundles[0].SeedLedger!),
            SeedLedgerCodec.Serialize(bundles[1].SeedLedger!));
        Assert.Equal(
            bundles[0].SeedLedger!.Entries.Select(value => value.DerivedSeed),
            bundles[1].SeedLedger!.Entries.Select(value => value.DerivedSeed));
        Assert.All(firstOutput.ExerciseBundlePaths, path =>
        {
            var summary = File.ReadAllText(Path.Combine(path, ArtifactSchema.SummaryJsonPath));
            var diagnostics = File.ReadAllText(Path.Combine(path, ArtifactSchema.DiagnosticsPath));
            Assert.Contains("\"variant\":\"paired\"", summary, StringComparison.Ordinal);
            Assert.Contains("\"variant\":\"paired\"", diagnostics, StringComparison.Ordinal);
            Assert.DoesNotContain("\"variant\":\"baseline\"", summary, StringComparison.Ordinal);
            Assert.DoesNotContain("\"variant\":\"candidate\"", summary, StringComparison.Ordinal);
        });

        var report = PairedReportReader.Read(firstOutput.ReportPath).Report;
        Assert.Equal(
            [ManeuverVariant.Baseline, ManeuverVariant.Candidate],
            report.Deterministic.Entries.Select(value => value.Variant));
        var comparison = Assert.Single(report.Deterministic.Comparisons);
        Assert.Equal(PairedComparisonStatus.Compared, comparison.Status);
        Assert.Equal(PairedDivergenceKind.AcceptedAction, comparison.FirstDivergence!.Kind);
        Assert.Equal(2, comparison.AcceptedStepCountDelta);
        Assert.Contains(
            "may diverge after the first differing choice",
            PairedManeuverReport.Interpretation,
            StringComparison.Ordinal);
        Assert.Contains(
            "makes no causal, statistical-significance, gameplay-balance, or synchronized-post-divergence claim",
            PairedManeuverReport.Interpretation,
            StringComparison.Ordinal);
        Assert.Equal(firstOutput.ReportFingerprint, secondOutput.ReportFingerprint);
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
    public void NonStringSchemeDiscriminatorReturnsManifestInvalidWithoutWritingArtifacts()
    {
        var repositoryRoot = FindRepositoryRoot(AppContext.BaseDirectory);
        var relativePath = Path.Combine(repositoryManifestDirectory, "numeric-scheme.json");
        var path = Path.Combine(repositoryRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{\"schemeId\":123}");
        var artifactRoot = Path.Combine(temp, "numeric-scheme");
        var standardError = new StringWriter();

        var exitCode = ManeuverRunCommand.Execute(
            Arguments(relativePath, artifactRoot),
            new StringWriter(),
            standardError,
            TestContext.Current.CancellationToken);

        Assert.Equal(ManeuverProcessExitCode.ManifestInvalid, exitCode);
        Assert.StartsWith(
            "Maneuver admission failed: ",
            NormalizeNewlines(standardError.ToString()),
            StringComparison.Ordinal);
        Assert.False(Directory.Exists(artifactRoot));
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
                "scenarios/exercises/rules-lab.organization.v2.json",
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

    private static string EventType(byte[] canonicalEvent)
    {
        using var document = System.Text.Json.JsonDocument.Parse(canonicalEvent);
        return document.RootElement.GetProperty("eventType").GetString()!;
    }

    private static void AssertMovement(
        ExerciseBundle bundle,
        params (string ElementId, string Origin, string Destination, int Before, int After)[]
            expected)
    {
        var movement = bundle.CanonicalEvents
            .Where(value => EventType(value) == "element-moved")
            .Select(value => System.Text.Json.JsonDocument.Parse(value))
            .ToArray();
        try
        {
            Assert.Equal(expected.Length, movement.Length);
            Assert.Equal(expected.Length, expected.Select(value => value.ElementId)
                .Distinct(StringComparer.Ordinal).Count());
            using var final = System.Text.Json.JsonDocument.Parse(bundle.FinalSnapshotBytes!);
            for (var index = 0; index < expected.Length; index++)
            {
                var root = movement[index].RootElement;
                var facts = expected[index];
                Assert.Equal(facts.ElementId, root.GetProperty("elementId").GetString());
                Assert.Equal(facts.Origin, root.GetProperty("originLocationId").GetString());
                Assert.Equal(
                    facts.Destination,
                    root.GetProperty("destinationLocationId").GetString());
                AssertExactAmount(
                    root.GetProperty("capabilityPointsExpendedBefore"),
                    facts.Before);
                AssertExactAmount(
                    root.GetProperty("capabilityPointsExpendedAfter"),
                    facts.After);
                AssertExactAmount(root.GetProperty("cost").GetProperty("totalCost"),
                    facts.After - facts.Before);
                Assert.Equal(0, root.GetProperty("cohesionBefore").GetInt32());
                Assert.Equal(0, root.GetProperty("cohesionAfter").GetInt32());

                var element = final.RootElement.GetProperty("world").GetProperty("elements")
                    .EnumerateArray().Single(value => string.Equals(
                        value.GetProperty("elementId").GetString(),
                        facts.ElementId,
                        StringComparison.Ordinal));
                Assert.Equal(
                    facts.Destination,
                    element.GetProperty("currentLocationId").GetString());
                Assert.Equal("none", element.GetProperty("reserveStatus").GetString());
                var operational = element.GetProperty("operationalState");
                AssertExactAmount(
                    operational.GetProperty("capabilityPointsExpended"),
                    facts.After);
                Assert.Equal(0, operational.GetProperty("cohesionLevel").GetInt32());
            }
        }
        finally
        {
            foreach (var document in movement) document.Dispose();
        }
    }

    private static void AssertExactAmount(System.Text.Json.JsonElement amount, int numerator)
    {
        Assert.Equal(numerator, amount.GetProperty("numerator").GetInt32());
        Assert.Equal(1, amount.GetProperty("denominator").GetInt32());
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
        "sha256:9e55e3de11338ba6432768ccb6740a6fed83b37503f69cc7ff8ecd58e205634f",
        "rules-lab.content.movement-contact.v1",
        "sha256:40f0e7a0a8876e4fefc4f06c1d752253cf338da614e587b9ff017e04541e7d79",
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
