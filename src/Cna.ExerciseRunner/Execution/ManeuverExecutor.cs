using System.Diagnostics;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Execution;

internal static class ManeuverExecutor
{
    internal static ManeuverReport Execute(
        ManeuverManifest manifest,
        string repositoryRoot,
        string artifactRoot,
        CancellationToken cancellationToken) =>
        Execute(
            manifest,
            repositoryRoot,
            artifactRoot,
            ManeuverExecutionDependencies.Default,
            cancellationToken);

    internal static ManeuverReport Execute(
        ManeuverManifest manifest,
        string repositoryRoot,
        string artifactRoot,
        ManeuverExecutionDependencies dependencies,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);
        ArgumentNullException.ThrowIfNull(dependencies);

        var started = Stopwatch.GetTimestamp();
        var entries = new List<ManeuverReportEntry>(manifest.Exercises.Count);
        var diagnostics = new List<ManeuverDiagnosticEntry>(manifest.Exercises.Count);

        for (var ordinal = 0; ordinal < manifest.Exercises.Count; ordinal++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                AppendNotRunTail(
                    manifest,
                    ordinal,
                    ManeuverNotRunReason.Cancelled,
                    entries,
                    diagnostics);
                break;
            }

            var entryStarted = Stopwatch.GetTimestamp();
            var materialized = manifest.MaterializeExercise(ordinal);
            var normalizedManifest = ExerciseManifestCodec.Serialize(materialized);
            var identity = new ExerciseRunIdentity(
                manifest.RootSeed,
                manifest.ManeuverId,
                ordinal,
                null);
            var result = dependencies.RunChild(new ExerciseRunCoordinatorRequest(
                materialized,
                normalizedManifest,
                identity,
                repositoryRoot,
                artifactRoot,
                new ExerciseDiagnosticTelemetry(),
                cancellationToken));

            if (string.IsNullOrWhiteSpace(result.CompletedBundlePath))
            {
                AppendAggregationFailure(
                    manifest,
                    ordinal,
                    ManeuverAggregationFailureCategory.CompletedBundleMissing,
                    null,
                    null,
                    ElapsedMicroseconds(entryStarted),
                    entries,
                    diagnostics);
                break;
            }

            ManeuverChildBundleView child;
            try
            {
                child = dependencies.ReadChildBundle(result.CompletedBundlePath);
                if (child is null)
                    throw new InvalidDataException(
                        "The completed Exercise bundle reader returned no evidence.");
            }
            catch (Exception exception) when (!IsFatal(exception))
            {
                AppendAggregationFailure(
                    manifest,
                    ordinal,
                    ManeuverAggregationFailureCategory.BundleInvalid,
                    result.CompletedBundlePath,
                    null,
                    ElapsedMicroseconds(entryStarted),
                    entries,
                    diagnostics);
                break;
            }

            if (cancellationToken.IsCancellationRequested
                && IsPreExecutionCancellation(
                    result,
                    child,
                    materialized,
                    normalizedManifest))
            {
                AppendNotRunTail(
                    manifest,
                    ordinal,
                    ManeuverNotRunReason.Cancelled,
                    entries,
                    diagnostics);
                break;
            }

            if (!IsAggregateEligibleProfile(child.Profile))
            {
                AppendAggregationFailure(
                    manifest,
                    ordinal,
                    ManeuverAggregationFailureCategory.BundleIdentityMismatch,
                    result.CompletedBundlePath,
                    child.ArtifactManifestSha256,
                    ElapsedMicroseconds(entryStarted),
                    entries,
                    diagnostics);
                break;
            }

            if (!HasExpectedIdentity(child, materialized, normalizedManifest, identity))
            {
                AppendAggregationFailure(
                    manifest,
                    ordinal,
                    ManeuverAggregationFailureCategory.BundleIdentityMismatch,
                    result.CompletedBundlePath,
                    child.ArtifactManifestSha256,
                    ElapsedMicroseconds(entryStarted),
                    entries,
                    diagnostics);
                break;
            }

            if (!TryAppendValidatedEntry(
                    manifest,
                    ordinal,
                    child,
                    normalizedManifest,
                    ElapsedMicroseconds(entryStarted),
                    entries,
                    diagnostics,
                    out var cancelled))
            {
                AppendAggregationFailure(
                    manifest,
                    ordinal,
                    ManeuverAggregationFailureCategory.BundleInvalid,
                    result.CompletedBundlePath,
                    child.ArtifactManifestSha256,
                    ElapsedMicroseconds(entryStarted),
                    entries,
                    diagnostics);
                break;
            }

            if (cancelled)
            {
                AppendNotRunTail(
                    manifest,
                    ordinal + 1,
                    ManeuverNotRunReason.Cancelled,
                    entries,
                    diagnostics);
                break;
            }
        }

        var elapsed = ElapsedMicroseconds(started);
        return BuildReport(manifest, entries, diagnostics, elapsed);
    }

    private static bool HasExpectedIdentity(
        ManeuverChildBundleView child,
        ExerciseManifest materialized,
        byte[] normalizedManifest,
        ExerciseRunIdentity identity)
    {
        var ledger = child.SeedLedger;
        if (!HasExpectedManifestAndBuildIdentity(child, materialized, normalizedManifest)
            || ledger is null)
            return false;

        var actual = ledger.Identity;
        return actual.RootSeed == identity.RootSeed
            && string.Equals(actual.ManeuverId, identity.ManeuverId, StringComparison.Ordinal)
            && actual.ExerciseOrdinal == identity.ExerciseOrdinal
            && actual.PairKey is null;
    }

    private static bool HasExpectedManifestAndBuildIdentity(
        ManeuverChildBundleView child,
        ExerciseManifest materialized,
        byte[] normalizedManifest)
    {
        var childManifest = child.NormalizedManifestBytes;
        var buildIdentity = child.BuildIdentity;
        return childManifest is not null
            && childManifest.AsSpan().SequenceEqual(normalizedManifest)
            && buildIdentity is not null
            && buildIdentity.BuildMode == materialized.BuildMode
            && string.Equals(
                buildIdentity.ManifestHash,
                ReplayEvidenceHasher.HashBytes(normalizedManifest),
                StringComparison.Ordinal)
            && string.Equals(
                buildIdentity.RulesetHash,
                materialized.RulesetHash,
                StringComparison.Ordinal)
            && string.Equals(
                buildIdentity.ConfigurationHash,
                ExerciseConfigurationIdentity.ComputeHash(materialized),
                StringComparison.Ordinal);
    }

    private static bool IsPreExecutionCancellation(
        ExerciseRunCoordinatorResult coordinatorResult,
        ManeuverChildBundleView child,
        ExerciseManifest materialized,
        byte[] normalizedManifest) =>
        coordinatorResult.ExitCode == ExerciseProcessExitCode.Cancelled
        && child.Profile == ArtifactBundleProfile.FailedIdentified
        && child.SeedLedger is null
        && child.AcceptedStepCount == 0
        && child.CheckResults.Results.Count == 0
        && child.RunResult.Completion is ExerciseFailed
        {
            Failure.Category: ExerciseFailureCategory.Cancelled,
        }
        && HasExpectedManifestAndBuildIdentity(child, materialized, normalizedManifest);

    private static bool IsAggregateEligibleProfile(ArtifactBundleProfile profile) =>
        profile is ArtifactBundleProfile.Succeeded
            or ArtifactBundleProfile.FailedExecuted
            or ArtifactBundleProfile.FailedReconstructed
            or ArtifactBundleProfile.FailedReadjudicated;

    private static bool TryAppendValidatedEntry(
        ManeuverManifest manifest,
        int ordinal,
        ManeuverChildBundleView child,
        byte[] normalizedManifest,
        long elapsedMicroseconds,
        List<ManeuverReportEntry> entries,
        List<ManeuverDiagnosticEntry> diagnostics,
        out bool cancelled)
    {
        var passedChecks = child.CheckResults.Results.Count(value => value.IsPassed);
        var failedChecks = child.CheckResults.Results.Count(value => !value.IsPassed);
        var normalizedManifestSha256 = ReplayEvidenceHasher.HashBytes(normalizedManifest);
        var seedLedgerSha256 = ReplayEvidenceHasher.HashBytes(
            SeedLedgerCodec.Serialize(child.SeedLedger!));

        ManeuverReportEntry entry;
        switch (child.RunResult.Completion)
        {
            case ExerciseSucceeded { Outcome: BoundaryReached boundary }
                when child.Profile == ArtifactBundleProfile.Succeeded
                    && string.Equals(
                        boundary.PositionId,
                        manifest.Exercises[ordinal].TerminalBoundary,
                        StringComparison.Ordinal)
                    && failedChecks == 0:
                entry = new ManeuverReportEntry(
                    ordinal,
                    manifest.Exercises[ordinal].ExerciseId,
                    ManeuverVariant.Unpaired,
                    ManeuverEntryStatus.Succeeded,
                    boundary,
                    null,
                    null,
                    null,
                    child.AcceptedStepCount,
                    passedChecks,
                    failedChecks,
                    normalizedManifestSha256,
                    seedLedgerSha256);
                cancelled = false;
                break;
            case ExerciseFailed { Failure.Category: var category }
                when IsAggregateEligibleFailureProfile(child.Profile, category):
                entry = new ManeuverReportEntry(
                    ordinal,
                    manifest.Exercises[ordinal].ExerciseId,
                    ManeuverVariant.Unpaired,
                    ManeuverEntryStatus.Failed,
                    null,
                    category,
                    null,
                    null,
                    child.AcceptedStepCount,
                    passedChecks,
                    failedChecks,
                    normalizedManifestSha256,
                    seedLedgerSha256);
                cancelled = category == ExerciseFailureCategory.Cancelled;
                break;
            default:
                cancelled = false;
                return false;
        }

        entries.Add(entry);
        diagnostics.Add(new ManeuverDiagnosticEntry(
            ordinal,
            elapsedMicroseconds,
            child.Path,
            child.ArtifactManifestSha256));
        return true;
    }

    private static void AppendAggregationFailure(
        ManeuverManifest manifest,
        int ordinal,
        ManeuverAggregationFailureCategory category,
        string? observedBundlePath,
        string? artifactManifestSha256,
        long elapsedMicroseconds,
        List<ManeuverReportEntry> entries,
        List<ManeuverDiagnosticEntry> diagnostics)
    {
        entries.Add(new ManeuverReportEntry(
            ordinal,
            manifest.Exercises[ordinal].ExerciseId,
            ManeuverVariant.Unpaired,
            ManeuverEntryStatus.AggregationFailed,
            null,
            null,
            category,
            null,
            null,
            null,
            null,
            null,
            null));
        diagnostics.Add(new ManeuverDiagnosticEntry(
            ordinal,
            elapsedMicroseconds,
            observedBundlePath,
            artifactManifestSha256));
        AppendNotRunTail(
            manifest,
            ordinal + 1,
            ManeuverNotRunReason.AggregationStopped,
            entries,
            diagnostics);
    }

    private static void AppendNotRunTail(
        ManeuverManifest manifest,
        int firstOrdinal,
        ManeuverNotRunReason reason,
        List<ManeuverReportEntry> entries,
        List<ManeuverDiagnosticEntry> diagnostics)
    {
        for (var ordinal = firstOrdinal; ordinal < manifest.Exercises.Count; ordinal++)
        {
            entries.Add(new ManeuverReportEntry(
                ordinal,
                manifest.Exercises[ordinal].ExerciseId,
                ManeuverVariant.Unpaired,
                ManeuverEntryStatus.NotRun,
                null,
                null,
                null,
                reason,
                null,
                null,
                null,
                null,
                null));
            diagnostics.Add(new ManeuverDiagnosticEntry(ordinal, null, null, null));
        }
    }

    private static ManeuverReport BuildReport(
        ManeuverManifest manifest,
        IReadOnlyList<ManeuverReportEntry> entries,
        IReadOnlyList<ManeuverDiagnosticEntry> diagnostics,
        long elapsedMicroseconds)
    {
        var succeeded = entries.Count(value => value.Status == ManeuverEntryStatus.Succeeded);
        var failed = entries.Count(value => value.Status == ManeuverEntryStatus.Failed);
        var aggregationFailed = entries.Count(
            value => value.Status == ManeuverEntryStatus.AggregationFailed);
        var notRun = entries.Count(value => value.Status == ManeuverEntryStatus.NotRun);
        var validated = succeeded + failed;
        var counts = new ManeuverReportCounts(
            manifest.Exercises.Count,
            validated + aggregationFailed,
            validated,
            succeeded,
            failed,
            aggregationFailed,
            notRun);
        var deterministic = new ManeuverReportDeterministic(
            manifest,
            ExpectedStatus(entries),
            counts,
            entries
                .Where(value => value.Status == ManeuverEntryStatus.Succeeded)
                .Select(value => (BoundaryReached)value.TerminalOutcome!)
                .GroupBy(value => value.PositionId, StringComparer.Ordinal)
                .OrderBy(value => value.Key, StringComparer.Ordinal)
                .Select(value => new ManeuverTerminalCount(
                    new BoundaryReached(value.Key),
                    value.Count())),
            Enum.GetValues<ExerciseFailureCategory>()
                .Select(category => new ManeuverFailureCount(
                    category,
                    entries.Count(value => value.FailureCategory == category))),
            Enum.GetValues<ManeuverAggregationFailureCategory>()
                .Select(category => new ManeuverAggregationFailureCount(
                    category,
                    entries.Count(value => value.AggregationFailureCategory == category))),
            entries);
        return new ManeuverReport(
            deterministic,
            new ManeuverReportDiagnostics(
                elapsedMicroseconds,
                new ManeuverThroughput(validated, elapsedMicroseconds),
                diagnostics));
    }

    private static ManeuverReportStatus ExpectedStatus(
        IReadOnlyList<ManeuverReportEntry> entries)
    {
        if (entries.Any(value => value.Status == ManeuverEntryStatus.AggregationFailed))
            return ManeuverReportStatus.AggregationFailed;
        if (entries.Any(value => value.FailureCategory == ExerciseFailureCategory.Cancelled
            || value.NotRunReason == ManeuverNotRunReason.Cancelled))
            return ManeuverReportStatus.Cancelled;
        if (entries.Any(value => value.Status == ManeuverEntryStatus.Failed))
            return ManeuverReportStatus.ExerciseFailed;
        return ManeuverReportStatus.Succeeded;
    }

    private static bool IsAggregateEligibleFailureProfile(
        ArtifactBundleProfile profile,
        ExerciseFailureCategory category) => profile switch
        {
            ArtifactBundleProfile.FailedExecuted =>
                category is ExerciseFailureCategory.ControllerFailed
                    or ExerciseFailureCategory.NoUniqueLegalAction
                    or ExerciseFailureCategory.IllegalAction
                    or ExerciseFailureCategory.InvariantFailed
                    or ExerciseFailureCategory.StepLimitExceeded
                    or ExerciseFailureCategory.Cancelled,
            ArtifactBundleProfile.FailedReconstructed =>
                category == ExerciseFailureCategory.ReconstructionMismatch,
            ArtifactBundleProfile.FailedReadjudicated =>
                category == ExerciseFailureCategory.ReadjudicationMismatch,
            _ => false,
        };

    private static bool IsFatal(Exception exception) =>
        exception is OutOfMemoryException
            or StackOverflowException
            or AccessViolationException
            or AppDomainUnloadedException
            or BadImageFormatException;

    private static long ElapsedMicroseconds(long started) =>
        Math.Max(0, Stopwatch.GetElapsedTime(started).Ticks / 10);
}
