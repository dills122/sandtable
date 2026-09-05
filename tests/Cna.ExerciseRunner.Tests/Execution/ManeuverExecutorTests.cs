using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class ManeuverExecutorTests
{
    private const string Boundary = "land.position.operation-1.organization";
    private static readonly string[] SerialEvents =
        ["run:0", "read:0", "run:1", "read:1", "run:2", "read:2"];
    private static readonly int[] FirstTwoOrdinals = [0, 1];
    private static readonly int[] FirstOrdinalOnly = [0];

    [Fact]
    public void RunsChildrenSeriallyInManifestOrderAndReopensEachCompletedPathExactlyOnce()
    {
        var manifest = CreateManifest("exercise.first", "exercise.second", "exercise.third");
        var events = new List<string>();
        var readCounts = new Dictionary<string, int>(StringComparer.Ordinal);
        var dependencies = new ManeuverExecutionDependencies(
            request =>
            {
                var ordinal = request.RunIdentity.ExerciseOrdinal;
                events.Add($"run:{ordinal}");
                AssertRunRequest(manifest, ordinal, request);
                return Completed(ordinal);
            },
            path =>
            {
                var ordinal = ParseOrdinal(path);
                events.Add($"read:{ordinal}");
                readCounts[path] = readCounts.GetValueOrDefault(path) + 1;
                return TrustedView(manifest, ordinal, ExerciseRunResult.Succeeded(
                    new BoundaryReached(Boundary)));
            });

        var report = ManeuverExecutor.Execute(
            manifest,
            "/repository",
            "/artifacts",
            dependencies,
            TestContext.Current.CancellationToken);

        Assert.Equal(SerialEvents, events);
        Assert.Equal(3, readCounts.Count);
        Assert.All(readCounts.Values, count => Assert.Equal(1, count));
        Assert.Equal(ManeuverReportStatus.Succeeded, report.Deterministic.Status);
        Assert.Equal(3, report.Deterministic.Counts.AttemptedExerciseCount);
        Assert.Equal(3, report.Deterministic.Counts.ValidatedExerciseCount);
        Assert.All(report.Deterministic.Entries, entry =>
            Assert.Equal(ManeuverEntryStatus.Succeeded, entry.Status));
        Assert.Equal(3, Assert.Single(report.Deterministic.TerminalCounts).Count);
        Assert.Equal(
            Enum.GetValues<ExerciseFailureCategory>(),
            report.Deterministic.FailureCounts.Select(value => value.Category));
        Assert.Equal(
            Enum.GetValues<ManeuverAggregationFailureCategory>(),
            report.Deterministic.AggregationFailureCounts.Select(value => value.Category));
    }

    [Fact]
    public void ContinuesAfterAnIdentityMatchedOrdinaryExerciseFailure()
    {
        var manifest = CreateManifest("exercise.first", "exercise.second");
        var runOrdinals = new List<int>();
        var dependencies = new ManeuverExecutionDependencies(
            request =>
            {
                runOrdinals.Add(request.RunIdentity.ExerciseOrdinal);
                return Completed(request.RunIdentity.ExerciseOrdinal);
            },
            path =>
            {
                var ordinal = ParseOrdinal(path);
                return TrustedView(
                    manifest,
                    ordinal,
                    ordinal == 0
                        ? ExerciseRunResult.Failed(
                            ExerciseFailureCategory.StepLimitExceeded,
                            null)
                        : ExerciseRunResult.Succeeded(new BoundaryReached(Boundary)));
            });

        var report = ManeuverExecutor.Execute(
            manifest,
            "/repository",
            "/artifacts",
            dependencies,
            TestContext.Current.CancellationToken);

        Assert.Equal(FirstTwoOrdinals, runOrdinals);
        Assert.Equal(ManeuverReportStatus.ExerciseFailed, report.Deterministic.Status);
        Assert.Collection(
            report.Deterministic.Entries,
            first =>
            {
                Assert.Equal(ManeuverEntryStatus.Failed, first.Status);
                Assert.Equal(ExerciseFailureCategory.StepLimitExceeded, first.FailureCategory);
            },
            second => Assert.Equal(ManeuverEntryStatus.Succeeded, second.Status));
        Assert.Equal(2, report.Deterministic.Counts.ValidatedExerciseCount);
        Assert.Equal(1, report.Deterministic.FailureCounts.Single(value =>
            value.Category == ExerciseFailureCategory.StepLimitExceeded).Count);
    }

    [Theory]
    [InlineData("missing", ManeuverAggregationFailureCategory.CompletedBundleMissing)]
    [InlineData("corrupt", ManeuverAggregationFailureCategory.BundleInvalid)]
    [InlineData("manifest-bytes", ManeuverAggregationFailureCategory.BundleIdentityMismatch)]
    [InlineData("build-configuration", ManeuverAggregationFailureCategory.BundleIdentityMismatch)]
    [InlineData("ledger-identity", ManeuverAggregationFailureCategory.BundleIdentityMismatch)]
    [InlineData("ledger-missing", ManeuverAggregationFailureCategory.BundleIdentityMismatch)]
    public void StopsOnUntrustedEvidenceAndAppendsAnExplicitAggregationStoppedTail(
        string mutation,
        ManeuverAggregationFailureCategory expectedCategory)
    {
        var manifest = CreateManifest("exercise.first", "exercise.second", "exercise.third");
        using var cancellation = new CancellationTokenSource();
        var runOrdinals = new List<int>();
        var readCount = 0;
        var dependencies = new ManeuverExecutionDependencies(
            request =>
            {
                runOrdinals.Add(request.RunIdentity.ExerciseOrdinal);
                return mutation == "missing"
                    ? new ExerciseRunCoordinatorResult(
                        ExerciseProcessExitCode.ArtifactFailed,
                        null,
                        "missing",
                        null)
                    : Completed(request.RunIdentity.ExerciseOrdinal);
            },
            path =>
            {
                readCount++;
                if (mutation == "corrupt")
                {
                    cancellation.Cancel();
                    throw new InvalidDataException("corrupt bundle");
                }

                var ordinal = ParseOrdinal(path);
                var materialized = manifest.MaterializeExercise(ordinal);
                var normalized = ExerciseManifestCodec.Serialize(materialized);
                var identity = Identity(manifest, ordinal);
                return TrustedView(
                    manifest,
                    ordinal,
                    ExerciseRunResult.Succeeded(new BoundaryReached(Boundary)),
                    normalizedManifest: mutation == "manifest-bytes" ? [0x00] : normalized,
                    buildIdentity: BuildIdentity(
                        materialized,
                        normalized,
                        mutation == "build-configuration" ? Hash('9') : null),
                    seedLedger: mutation switch
                    {
                        "ledger-identity" => ExerciseSeedLedger.Create(
                            new ExerciseRunIdentity(
                                manifest.RootSeed,
                                manifest.ManeuverId,
                                ordinal + 1,
                                null)),
                        "ledger-missing" => null,
                        _ => ExerciseSeedLedger.Create(identity),
                    },
                    missingSeedLedger: mutation == "ledger-missing");
            });

#pragma warning disable xUnit1051 // A test-owned token drives the cancellation/precedence behavior.
        var report = ManeuverExecutor.Execute(
            manifest,
            "/repository",
            "/artifacts",
            dependencies,
            cancellation.Token);
#pragma warning restore xUnit1051

        Assert.Equal(FirstOrdinalOnly, runOrdinals);
        Assert.Equal(mutation == "missing" ? 0 : 1, readCount);
        Assert.Equal(ManeuverReportStatus.AggregationFailed, report.Deterministic.Status);
        Assert.Collection(
            report.Deterministic.Entries,
            first =>
            {
                Assert.Equal(ManeuverEntryStatus.AggregationFailed, first.Status);
                Assert.Equal(expectedCategory, first.AggregationFailureCategory);
            },
            second =>
            {
                Assert.Equal(ManeuverEntryStatus.NotRun, second.Status);
                Assert.Equal(ManeuverNotRunReason.AggregationStopped, second.NotRunReason);
            },
            third =>
            {
                Assert.Equal(ManeuverEntryStatus.NotRun, third.Status);
                Assert.Equal(ManeuverNotRunReason.AggregationStopped, third.NotRunReason);
            });
        Assert.Equal(1, report.Deterministic.Counts.AttemptedExerciseCount);
        Assert.Equal(0, report.Deterministic.Counts.ValidatedExerciseCount);
        Assert.Equal(2, report.Deterministic.Counts.NotRunExerciseCount);
        Assert.Equal(1, report.Deterministic.AggregationFailureCounts.Single(value =>
            value.Category == expectedCategory).Count);
        if (mutation == "missing")
            Assert.Null(report.Diagnostics.Entries[0].ObservedBundlePath);
        else
            Assert.Equal("bundle-0", report.Diagnostics.Entries[0].ObservedBundlePath);
        Assert.All(report.Diagnostics.Entries.Skip(1), diagnostic =>
            Assert.Null(diagnostic.ObservedBundlePath));
    }

    [Fact]
    public void StopsBeforeTheNextChildWhenCancellationIsRequested()
    {
        var manifest = CreateManifest("exercise.first", "exercise.second", "exercise.third");
        using var cancellation = new CancellationTokenSource();
        var runOrdinals = new List<int>();
        var dependencies = new ManeuverExecutionDependencies(
            request =>
            {
                runOrdinals.Add(request.RunIdentity.ExerciseOrdinal);
                return Completed(request.RunIdentity.ExerciseOrdinal);
            },
            path =>
            {
                var ordinal = ParseOrdinal(path);
                var view = TrustedView(
                    manifest,
                    ordinal,
                    ExerciseRunResult.Succeeded(new BoundaryReached(Boundary)));
                cancellation.Cancel();
                return view;
            });

#pragma warning disable xUnit1051 // A test-owned token drives cancellation between child runs.
        var report = ManeuverExecutor.Execute(
            manifest,
            "/repository",
            "/artifacts",
            dependencies,
            cancellation.Token);
#pragma warning restore xUnit1051

        Assert.Equal(FirstOrdinalOnly, runOrdinals);
        Assert.Equal(ManeuverReportStatus.Cancelled, report.Deterministic.Status);
        Assert.Equal(ManeuverEntryStatus.Succeeded, report.Deterministic.Entries[0].Status);
        Assert.All(report.Deterministic.Entries.Skip(1), entry =>
        {
            Assert.Equal(ManeuverEntryStatus.NotRun, entry.Status);
            Assert.Equal(ManeuverNotRunReason.Cancelled, entry.NotRunReason);
        });
    }

    [Fact]
    public void AttributableCancelledChildStopsAndMarksTheRemainingTailCancelled()
    {
        var manifest = CreateManifest("exercise.first", "exercise.second");
        var runOrdinals = new List<int>();
        var dependencies = new ManeuverExecutionDependencies(
            request =>
            {
                runOrdinals.Add(request.RunIdentity.ExerciseOrdinal);
                return Completed(request.RunIdentity.ExerciseOrdinal);
            },
            path => TrustedView(
                manifest,
                ParseOrdinal(path),
                ExerciseRunResult.Failed(ExerciseFailureCategory.Cancelled, null)));

        var report = ManeuverExecutor.Execute(
            manifest,
            "/repository",
            "/artifacts",
            dependencies,
            TestContext.Current.CancellationToken);

        Assert.Equal(FirstOrdinalOnly, runOrdinals);
        Assert.Equal(ManeuverReportStatus.Cancelled, report.Deterministic.Status);
        Assert.Equal(
            ExerciseFailureCategory.Cancelled,
            report.Deterministic.Entries[0].FailureCategory);
        Assert.Equal(ManeuverNotRunReason.Cancelled, report.Deterministic.Entries[1].NotRunReason);
    }

    [Fact]
    public void CancellationAfterSchedulingButBeforeCoreBeginCreatesOnlyACancelledNotRunTail()
    {
        var manifest = CreateManifest("exercise.first", "exercise.second", "exercise.third");
        using var cancellation = new CancellationTokenSource();
        var readCount = 0;
        var dependencies = new ManeuverExecutionDependencies(
            request =>
            {
                Assert.False(request.CancellationToken.IsCancellationRequested);
                cancellation.Cancel();
                return new ExerciseRunCoordinatorResult(
                    ExerciseProcessExitCode.Cancelled,
                    "bundle-0",
                    "cancelled before Core begin",
                    null);
            },
            path =>
            {
                readCount++;
                return TrustedView(
                    manifest,
                    ParseOrdinal(path),
                    ExerciseRunResult.Failed(ExerciseFailureCategory.Cancelled, null),
                    profile: ArtifactBundleProfile.FailedIdentified,
                    missingSeedLedger: true,
                    acceptedStepCount: 0);
            });

#pragma warning disable xUnit1051 // A test-owned token drives the post-scheduling cancellation race.
        var report = ManeuverExecutor.Execute(
            manifest,
            "/repository",
            "/artifacts",
            dependencies,
            cancellation.Token);
#pragma warning restore xUnit1051

        Assert.Equal(1, readCount);
        Assert.Equal(ManeuverReportStatus.Cancelled, report.Deterministic.Status);
        Assert.Equal(0, report.Deterministic.Counts.AttemptedExerciseCount);
        Assert.Equal(0, report.Deterministic.Counts.ValidatedExerciseCount);
        Assert.All(report.Deterministic.Entries, entry =>
        {
            Assert.Equal(ManeuverEntryStatus.NotRun, entry.Status);
            Assert.Equal(ManeuverNotRunReason.Cancelled, entry.NotRunReason);
            Assert.Null(entry.FailureCategory);
            Assert.Null(entry.NormalizedManifestSha256);
            Assert.Null(entry.SeedLedgerSha256);
        });
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void PreExecutionCancellationRequiresAnEmptyExecutionFootprint(
        bool hasAcceptedStep,
        bool hasCheckResult)
    {
        var manifest = CreateManifest("exercise.first", "exercise.second");
        using var cancellation = new CancellationTokenSource();
        var dependencies = new ManeuverExecutionDependencies(
            _ =>
            {
                cancellation.Cancel();
                return new ExerciseRunCoordinatorResult(
                    ExerciseProcessExitCode.Cancelled,
                    "bundle-0",
                    "cancelled before Core begin",
                    null);
            },
            path => TrustedView(
                manifest,
                ParseOrdinal(path),
                ExerciseRunResult.Failed(ExerciseFailureCategory.Cancelled, null),
                profile: ArtifactBundleProfile.FailedIdentified,
                missingSeedLedger: true,
                checkResults: hasCheckResult
                    ? new ExerciseCheckResults(
                    [
                        ExerciseCheckResult.Passed(
                            ExerciseCheckId.TerminalBoundary,
                            null,
                            null),
                    ])
                    : null,
                acceptedStepCount: hasAcceptedStep ? 1 : 0));

#pragma warning disable xUnit1051 // A test-owned token drives the cancellation/evidence case.
        var report = ManeuverExecutor.Execute(
            manifest,
            "/repository",
            "/artifacts",
            dependencies,
            cancellation.Token);
#pragma warning restore xUnit1051

        Assert.Equal(ManeuverReportStatus.AggregationFailed, report.Deterministic.Status);
        Assert.Equal(
            ManeuverAggregationFailureCategory.BundleIdentityMismatch,
            report.Deterministic.Entries[0].AggregationFailureCategory);
        Assert.Equal(
            ManeuverNotRunReason.AggregationStopped,
            report.Deterministic.Entries[1].NotRunReason);
    }

    [Fact]
    public void ContradictoryBuildModePreventsPreExecutionCancellationAttribution()
    {
        var manifest = CreateManifest("exercise.first", "exercise.second");
        using var cancellation = new CancellationTokenSource();
        var dependencies = new ManeuverExecutionDependencies(
            _ =>
            {
                cancellation.Cancel();
                return new ExerciseRunCoordinatorResult(
                    ExerciseProcessExitCode.Cancelled,
                    "bundle-0",
                    "cancelled before Core begin",
                    null);
            },
            path =>
            {
                var ordinal = ParseOrdinal(path);
                var materialized = manifest.MaterializeExercise(ordinal);
                var normalized = ExerciseManifestCodec.Serialize(materialized);
                return TrustedView(
                    manifest,
                    ordinal,
                    ExerciseRunResult.Failed(ExerciseFailureCategory.Cancelled, null),
                    profile: ArtifactBundleProfile.FailedIdentified,
                    normalizedManifest: normalized,
                    buildIdentity: BuildIdentity(
                        materialized,
                        normalized,
                        buildMode: ExerciseBuildMode.Baseline),
                    missingSeedLedger: true,
                    acceptedStepCount: 0);
            });

#pragma warning disable xUnit1051 // A test-owned token drives the cancellation/evidence precedence case.
        var report = ManeuverExecutor.Execute(
            manifest,
            "/repository",
            "/artifacts",
            dependencies,
            cancellation.Token);
#pragma warning restore xUnit1051

        Assert.Equal(ManeuverReportStatus.AggregationFailed, report.Deterministic.Status);
        Assert.Equal(
            ManeuverAggregationFailureCategory.BundleIdentityMismatch,
            report.Deterministic.Entries[0].AggregationFailureCategory);
        Assert.Equal(
            ManeuverNotRunReason.AggregationStopped,
            report.Deterministic.Entries[1].NotRunReason);
    }

    [Fact]
    public void CorruptEvidenceStillTakesPrecedenceOverThePostSchedulingCancellationRace()
    {
        var manifest = CreateManifest("exercise.first", "exercise.second");
        using var cancellation = new CancellationTokenSource();
        var dependencies = new ManeuverExecutionDependencies(
            _ =>
            {
                cancellation.Cancel();
                return new ExerciseRunCoordinatorResult(
                    ExerciseProcessExitCode.Cancelled,
                    "bundle-0",
                    "cancelled before Core begin",
                    null);
            },
            _ => throw new InvalidDataException("corrupt cancellation evidence"));

#pragma warning disable xUnit1051 // A test-owned token drives the cancellation/evidence precedence case.
        var report = ManeuverExecutor.Execute(
            manifest,
            "/repository",
            "/artifacts",
            dependencies,
            cancellation.Token);
#pragma warning restore xUnit1051

        Assert.Equal(ManeuverReportStatus.AggregationFailed, report.Deterministic.Status);
        Assert.Equal(
            ManeuverAggregationFailureCategory.BundleInvalid,
            report.Deterministic.Entries[0].AggregationFailureCategory);
        Assert.Equal(
            ManeuverNotRunReason.AggregationStopped,
            report.Deterministic.Entries[1].NotRunReason);
    }

    [Theory]
    [InlineData(ArtifactBundleProfile.Succeeded, true)]
    [InlineData(ArtifactBundleProfile.FailedPreAdmission, false)]
    [InlineData(ArtifactBundleProfile.FailedAdmitted, false)]
    [InlineData(ArtifactBundleProfile.FailedIdentified, false)]
    [InlineData(ArtifactBundleProfile.FailedExecuted, true)]
    [InlineData(ArtifactBundleProfile.FailedReconstructed, true)]
    [InlineData(ArtifactBundleProfile.FailedReadjudicated, true)]
    [InlineData(ArtifactBundleProfile.FailedSummarized, false)]
    public void AttributesOutcomesOnlyFromTheClosedEligibleArtifactProfileSet(
        ArtifactBundleProfile profile,
        bool isEligible)
    {
        var manifest = CreateManifest("exercise.first", "exercise.second");
        var runResult = profile switch
        {
            ArtifactBundleProfile.Succeeded => ExerciseRunResult.Succeeded(
                new BoundaryReached(Boundary)),
            ArtifactBundleProfile.FailedReconstructed => ExerciseRunResult.Failed(
                ExerciseFailureCategory.ReconstructionMismatch,
                null),
            ArtifactBundleProfile.FailedReadjudicated => ExerciseRunResult.Failed(
                ExerciseFailureCategory.ReadjudicationMismatch,
                null),
            _ => ExerciseRunResult.Failed(ExerciseFailureCategory.StepLimitExceeded, null),
        };
        var runCount = 0;
        var dependencies = new ManeuverExecutionDependencies(
            request =>
            {
                runCount++;
                return new ExerciseRunCoordinatorResult(
                    ExerciseExitCodeMapper.Map(runResult),
                    $"bundle-{request.RunIdentity.ExerciseOrdinal}",
                    null,
                    null);
            },
            path => TrustedView(
                manifest,
                ParseOrdinal(path),
                runResult,
                profile: profile));

        var report = ManeuverExecutor.Execute(
            manifest,
            "/repository",
            "/artifacts",
            dependencies,
            TestContext.Current.CancellationToken);

        if (isEligible)
        {
            Assert.Equal(2, runCount);
            Assert.Equal(2, report.Deterministic.Counts.ValidatedExerciseCount);
            Assert.DoesNotContain(
                report.Deterministic.Entries,
                entry => entry.Status == ManeuverEntryStatus.AggregationFailed);
        }
        else
        {
            Assert.Equal(1, runCount);
            Assert.Equal(ManeuverReportStatus.AggregationFailed, report.Deterministic.Status);
            Assert.Equal(
                ManeuverEntryStatus.AggregationFailed,
                report.Deterministic.Entries[0].Status);
            Assert.Equal(
                ManeuverNotRunReason.AggregationStopped,
                report.Deterministic.Entries[1].NotRunReason);
        }
    }

    private static void AssertRunRequest(
        ManeuverManifest parent,
        int ordinal,
        ExerciseRunCoordinatorRequest request)
    {
        var materialized = parent.MaterializeExercise(ordinal);
        Assert.Equal(materialized, request.Manifest);
        Assert.Equal(ExerciseManifestCodec.Serialize(materialized), request.NormalizedManifest);
        Assert.Equal(parent.RootSeed, request.RunIdentity.RootSeed);
        Assert.Equal(parent.ManeuverId, request.RunIdentity.ManeuverId);
        Assert.Equal(ordinal, request.RunIdentity.ExerciseOrdinal);
        Assert.Null(request.RunIdentity.PairKey);
        Assert.Equal("/repository", request.RepositoryRoot);
        Assert.Equal("/artifacts", request.ArtifactRoot);
    }

    private static ExerciseRunCoordinatorResult Completed(int ordinal) => new(
        ExerciseProcessExitCode.Succeeded,
        $"bundle-{ordinal}",
        null,
        null);

    private static ManeuverChildBundleView TrustedView(
        ManeuverManifest parent,
        int ordinal,
        ExerciseRunResult runResult,
        ArtifactBundleProfile? profile = null,
        byte[]? normalizedManifest = null,
        BuildIdentity? buildIdentity = null,
        ExerciseSeedLedger? seedLedger = null,
        bool missingSeedLedger = false,
        ExerciseCheckResults? checkResults = null,
        int? acceptedStepCount = null)
    {
        var materialized = parent.MaterializeExercise(ordinal);
        normalizedManifest ??= ExerciseManifestCodec.Serialize(materialized);
        var identity = Identity(parent, ordinal);
        return new ManeuverChildBundleView(
            $"bundle-{ordinal}",
            profile ?? (runResult.Completion is ExerciseSucceeded
                ? ArtifactBundleProfile.Succeeded
                : ArtifactBundleProfile.FailedExecuted),
            Hash((char)('a' + ordinal)),
            normalizedManifest,
            buildIdentity ?? BuildIdentity(materialized, normalizedManifest),
            missingSeedLedger ? null : seedLedger ?? ExerciseSeedLedger.Create(identity),
            runResult,
            checkResults ?? new ExerciseCheckResults([]),
            acceptedStepCount ?? ordinal + 1);
    }

    private static BuildIdentity BuildIdentity(
        ExerciseManifest manifest,
        byte[] normalizedManifest,
        string? configurationHash = null,
        ExerciseBuildMode? buildMode = null)
    {
        var effectiveBuildMode = buildMode ?? ExerciseBuildMode.Exploratory;
        return new BuildIdentity(
            effectiveBuildMode,
            "1111111111111111111111111111111111111111",
            "2222222222222222222222222222222222222222",
            effectiveBuildMode == ExerciseBuildMode.Exploratory,
            Hash('0'),
            ".NET 10",
            "arm64",
            "arm64",
            manifest.RulesetHash,
            configurationHash ?? ExerciseConfigurationIdentity.ComputeHash(manifest),
            ReplayEvidenceHasher.HashBytes(normalizedManifest),
            ExerciseSeedLedger.SchemeId,
            effectiveBuildMode == ExerciseBuildMode.Baseline,
            effectiveBuildMode == ExerciseBuildMode.Baseline,
            [new BuildArtifactIdentity("runner.dll", 1, Hash('f'))]);
    }

    private static ExerciseRunIdentity Identity(ManeuverManifest parent, int ordinal) => new(
        parent.RootSeed,
        parent.ManeuverId,
        ordinal,
        null);

    private static int ParseOrdinal(string path) =>
        int.Parse(path.AsSpan("bundle-".Length), System.Globalization.CultureInfo.InvariantCulture);

    private static ManeuverManifest CreateManifest(params string[] exerciseIds) => new(
        ManeuverManifest.CurrentContractVersion,
        ManeuverManifest.SchemeId,
        "rules-lab.serial",
        ManeuverMode.SerialUnpaired,
        1844,
        new ManeuverReportOptions(ManeuverReportProfile.TrustedAuthority),
        exerciseIds.Select(exerciseId => new ManeuverExerciseManifest(
            ExerciseManifest.CurrentContractVersion,
            exerciseId,
            "rules-lab.initiative.predetermined",
            "sha256:48ad98fd232f7c7c50d4f925dd83e3de97f2eb48cc6929a17aa1fb172cdbd394",
            "rules-lab.content.movement-contact.v1",
            "sha256:20cf54f25d752253105877c6139d8db86549759f9dbb80fad873686498f26f5f",
            "movement-contact-lab",
            Cna1979Ruleset.Manifest.Hash,
            Boundary,
            8,
            ExerciseBuildMode.Exploratory,
            ExerciseConfidentiality.TrustedAuthority,
            ExerciseDetail.Forensic,
            new ExerciseControllerManifest(
                ExerciseControllerPolicy.FirstByActionId,
                ExerciseControllerPolicy.FirstByActionId,
                ExerciseControllerPolicy.FirstByActionId),
            null)));

    private static string Hash(char value) => $"sha256:{new string(value, 64)}";
}
