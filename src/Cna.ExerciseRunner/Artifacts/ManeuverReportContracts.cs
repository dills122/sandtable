namespace Cna.ExerciseRunner.Artifacts;

public enum ManeuverReportStatus
{
    Succeeded,
    ExerciseFailed,
    AggregationFailed,
    Cancelled,
}

public enum ManeuverEntryStatus
{
    Succeeded,
    Failed,
    AggregationFailed,
    NotRun,
}

public enum ManeuverVariant
{
    Unpaired,
}

public enum ManeuverAggregationFailureCategory
{
    CompletedBundleMissing,
    BundleInvalid,
    BundleIdentityMismatch,
}

public enum ManeuverNotRunReason
{
    Cancelled,
    AggregationStopped,
}

public sealed record ManeuverReportCounts
{
    public ManeuverReportCounts(
        int requestedExerciseCount,
        int attemptedExerciseCount,
        int validatedExerciseCount,
        int succeededExerciseCount,
        int failedExerciseCount,
        int aggregationFailedExerciseCount,
        int notRunExerciseCount)
    {
        RequireNonnegative(requestedExerciseCount, nameof(requestedExerciseCount));
        RequireNonnegative(attemptedExerciseCount, nameof(attemptedExerciseCount));
        RequireNonnegative(validatedExerciseCount, nameof(validatedExerciseCount));
        RequireNonnegative(succeededExerciseCount, nameof(succeededExerciseCount));
        RequireNonnegative(failedExerciseCount, nameof(failedExerciseCount));
        RequireNonnegative(
            aggregationFailedExerciseCount,
            nameof(aggregationFailedExerciseCount));
        RequireNonnegative(notRunExerciseCount, nameof(notRunExerciseCount));
        RequestedExerciseCount = requestedExerciseCount;
        AttemptedExerciseCount = attemptedExerciseCount;
        ValidatedExerciseCount = validatedExerciseCount;
        SucceededExerciseCount = succeededExerciseCount;
        FailedExerciseCount = failedExerciseCount;
        AggregationFailedExerciseCount = aggregationFailedExerciseCount;
        NotRunExerciseCount = notRunExerciseCount;
    }

    public int RequestedExerciseCount { get; }
    public int AttemptedExerciseCount { get; }
    public int ValidatedExerciseCount { get; }
    public int SucceededExerciseCount { get; }
    public int FailedExerciseCount { get; }
    public int AggregationFailedExerciseCount { get; }
    public int NotRunExerciseCount { get; }

    private static void RequireNonnegative(int value, string parameterName) =>
        ArgumentOutOfRangeException.ThrowIfNegative(value, parameterName);
}

public sealed record ManeuverTerminalCount
{
    public ManeuverTerminalCount(ExerciseTerminalOutcome outcome, int count)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        if (outcome is not BoundaryReached)
            throw new ArgumentException(
                "Task 014 supports only boundary-reached terminal counts.",
                nameof(outcome));
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        Outcome = outcome;
        Count = count;
    }

    public ExerciseTerminalOutcome Outcome { get; }
    public int Count { get; }
}

public sealed record ManeuverFailureCount
{
    public ManeuverFailureCount(ExerciseFailureCategory category, int count)
    {
        if (!Enum.IsDefined(category)) throw new ArgumentOutOfRangeException(nameof(category));
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        Category = category;
        Count = count;
    }

    public ExerciseFailureCategory Category { get; }
    public int Count { get; }
}

public sealed record ManeuverAggregationFailureCount
{
    public ManeuverAggregationFailureCount(
        ManeuverAggregationFailureCategory category,
        int count)
    {
        if (!Enum.IsDefined(category)) throw new ArgumentOutOfRangeException(nameof(category));
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        Category = category;
        Count = count;
    }

    public ManeuverAggregationFailureCategory Category { get; }
    public int Count { get; }
}

public sealed record ManeuverReportEntry
{
    public ManeuverReportEntry(
        int ordinal,
        string exerciseId,
        ManeuverVariant variant,
        ManeuverEntryStatus status,
        ExerciseTerminalOutcome? terminalOutcome,
        ExerciseFailureCategory? failureCategory,
        ManeuverAggregationFailureCategory? aggregationFailureCategory,
        ManeuverNotRunReason? notRunReason,
        int? acceptedStepCount,
        int? passedCheckCount,
        int? failedCheckCount,
        string? normalizedManifestSha256,
        string? seedLedgerSha256)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ordinal);
        ArgumentException.ThrowIfNullOrWhiteSpace(exerciseId);
        if (!Enum.IsDefined(variant)) throw new ArgumentOutOfRangeException(nameof(variant));
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (failureCategory.HasValue && !Enum.IsDefined(failureCategory.Value))
            throw new ArgumentOutOfRangeException(nameof(failureCategory));
        if (aggregationFailureCategory.HasValue
            && !Enum.IsDefined(aggregationFailureCategory.Value))
            throw new ArgumentOutOfRangeException(nameof(aggregationFailureCategory));
        if (notRunReason.HasValue && !Enum.IsDefined(notRunReason.Value))
            throw new ArgumentOutOfRangeException(nameof(notRunReason));
        RequireNullableCount(acceptedStepCount, nameof(acceptedStepCount));
        RequireNullableCount(passedCheckCount, nameof(passedCheckCount));
        RequireNullableCount(failedCheckCount, nameof(failedCheckCount));
        if (normalizedManifestSha256 is not null)
            ReplayProofValidation.RequireSha256(
                normalizedManifestSha256,
                nameof(normalizedManifestSha256));
        if (seedLedgerSha256 is not null)
            ReplayProofValidation.RequireSha256(seedLedgerSha256, nameof(seedLedgerSha256));

        if (!IsValidState(
                status,
                terminalOutcome,
                failureCategory,
                aggregationFailureCategory,
                notRunReason,
                acceptedStepCount,
                passedCheckCount,
                failedCheckCount,
                normalizedManifestSha256,
                seedLedgerSha256))
            throw new ArgumentException("The Maneuver report entry state is contradictory.");

        Ordinal = ordinal;
        ExerciseId = exerciseId;
        Variant = variant;
        Status = status;
        TerminalOutcome = terminalOutcome;
        FailureCategory = failureCategory;
        AggregationFailureCategory = aggregationFailureCategory;
        NotRunReason = notRunReason;
        AcceptedStepCount = acceptedStepCount;
        PassedCheckCount = passedCheckCount;
        FailedCheckCount = failedCheckCount;
        NormalizedManifestSha256 = normalizedManifestSha256;
        SeedLedgerSha256 = seedLedgerSha256;
    }

    public int Ordinal { get; }
    public string ExerciseId { get; }
    public ManeuverVariant Variant { get; }
    public ManeuverEntryStatus Status { get; }
    public ExerciseTerminalOutcome? TerminalOutcome { get; }
    public ExerciseFailureCategory? FailureCategory { get; }
    public ManeuverAggregationFailureCategory? AggregationFailureCategory { get; }
    public ManeuverNotRunReason? NotRunReason { get; }
    public int? AcceptedStepCount { get; }
    public int? PassedCheckCount { get; }
    public int? FailedCheckCount { get; }
    public string? NormalizedManifestSha256 { get; }
    public string? SeedLedgerSha256 { get; }

    private static bool IsValidState(
        ManeuverEntryStatus status,
        ExerciseTerminalOutcome? terminalOutcome,
        ExerciseFailureCategory? failureCategory,
        ManeuverAggregationFailureCategory? aggregationFailureCategory,
        ManeuverNotRunReason? notRunReason,
        int? acceptedStepCount,
        int? passedCheckCount,
        int? failedCheckCount,
        string? normalizedManifestSha256,
        string? seedLedgerSha256) => status switch
        {
            ManeuverEntryStatus.Succeeded =>
                terminalOutcome is BoundaryReached
                && failureCategory is null
                && aggregationFailureCategory is null
                && notRunReason is null
                && acceptedStepCount.HasValue
                && passedCheckCount.HasValue
                && failedCheckCount == 0
                && normalizedManifestSha256 is not null
                && seedLedgerSha256 is not null,
            ManeuverEntryStatus.Failed =>
                terminalOutcome is null
                && IsAggregateEligibleFailure(failureCategory)
                && aggregationFailureCategory is null
                && notRunReason is null
                && acceptedStepCount.HasValue
                && passedCheckCount.HasValue
                && failedCheckCount.HasValue
                && normalizedManifestSha256 is not null
                && seedLedgerSha256 is not null,
            ManeuverEntryStatus.AggregationFailed =>
                terminalOutcome is null
                && failureCategory is null
                && aggregationFailureCategory.HasValue
                && notRunReason is null
                && acceptedStepCount is null
                && passedCheckCount is null
                && failedCheckCount is null
                && normalizedManifestSha256 is null
                && seedLedgerSha256 is null,
            ManeuverEntryStatus.NotRun =>
                terminalOutcome is null
                && failureCategory is null
                && aggregationFailureCategory is null
                && notRunReason.HasValue
                && acceptedStepCount is null
                && passedCheckCount is null
                && failedCheckCount is null
                && normalizedManifestSha256 is null
                && seedLedgerSha256 is null,
            _ => false,
        };

    private static bool IsAggregateEligibleFailure(ExerciseFailureCategory? category) =>
        category is ExerciseFailureCategory.ControllerFailed
            or ExerciseFailureCategory.NoUniqueLegalAction
            or ExerciseFailureCategory.IllegalAction
            or ExerciseFailureCategory.InvariantFailed
            or ExerciseFailureCategory.ReconstructionMismatch
            or ExerciseFailureCategory.ReadjudicationMismatch
            or ExerciseFailureCategory.StepLimitExceeded
            or ExerciseFailureCategory.Cancelled;

    private static void RequireNullableCount(int? value, string parameterName)
    {
        if (value.HasValue) ArgumentOutOfRangeException.ThrowIfNegative(value.Value, parameterName);
    }
}

public sealed class ManeuverReportDeterministic
{
    public ManeuverReportDeterministic(
        ManeuverManifest manifest,
        ManeuverReportStatus status,
        ManeuverReportCounts counts,
        IEnumerable<ManeuverTerminalCount> terminalCounts,
        IEnumerable<ManeuverFailureCount> failureCounts,
        IEnumerable<ManeuverAggregationFailureCount> aggregationFailureCounts,
        IEnumerable<ManeuverReportEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        ArgumentNullException.ThrowIfNull(counts);
        var terminalCopy = Copy(terminalCounts, nameof(terminalCounts));
        var failureCopy = Copy(failureCounts, nameof(failureCounts));
        var aggregationCopy = Copy(aggregationFailureCounts, nameof(aggregationFailureCounts));
        var entryCopy = Copy(entries, nameof(entries));

        RequireEntries(manifest, entryCopy);
        RequireCausalStop(status, entryCopy);
        RequireCounts(counts, entryCopy);
        RequireTerminalCounts(manifest, terminalCopy, entryCopy);
        RequireFailureCounts(failureCopy, entryCopy);
        RequireAggregationFailureCounts(aggregationCopy, entryCopy);
        if (status != ExpectedStatus(entryCopy))
            throw new ArgumentException("The Maneuver status contradicts its entries.", nameof(status));

        Manifest = manifest;
        Status = status;
        Counts = counts;
        TerminalCounts = Array.AsReadOnly(terminalCopy);
        FailureCounts = Array.AsReadOnly(failureCopy);
        AggregationFailureCounts = Array.AsReadOnly(aggregationCopy);
        Entries = Array.AsReadOnly(entryCopy);
    }

    public ManeuverManifest Manifest { get; }
    public ManeuverReportStatus Status { get; }
    public ManeuverReportCounts Counts { get; }
    public IReadOnlyList<ManeuverTerminalCount> TerminalCounts { get; }
    public IReadOnlyList<ManeuverFailureCount> FailureCounts { get; }
    public IReadOnlyList<ManeuverAggregationFailureCount> AggregationFailureCounts { get; }
    public IReadOnlyList<ManeuverReportEntry> Entries { get; }

    private static T[] Copy<T>(IEnumerable<T> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var copy = values.ToArray();
        if (copy.Any(value => value is null))
            throw new ArgumentException("Report collections cannot contain null.", parameterName);
        return copy;
    }

    private static void RequireEntries(
        ManeuverManifest manifest,
        ManeuverReportEntry[] entries)
    {
        if (entries.Length != manifest.Exercises.Count)
            throw new ArgumentException("The report must contain one entry per Exercise.");
        for (var ordinal = 0; ordinal < entries.Length; ordinal++)
        {
            var entry = entries[ordinal];
            if (entry.Ordinal != ordinal
                || entry.Variant != ManeuverVariant.Unpaired
                || !string.Equals(
                    entry.ExerciseId,
                    manifest.Exercises[ordinal].ExerciseId,
                    StringComparison.Ordinal))
                throw new ArgumentException("Report entries must match manifest order and identity.");
            if (entry.TerminalOutcome is BoundaryReached boundary
                && !string.Equals(
                    boundary.PositionId,
                    manifest.Exercises[ordinal].TerminalBoundary,
                    StringComparison.Ordinal))
                throw new ArgumentException(
                    "A successful entry must reach its admitted terminal boundary.");
        }
    }

    private static void RequireCounts(
        ManeuverReportCounts counts,
        ManeuverReportEntry[] entries)
    {
        var succeeded = entries.Count(value => value.Status == ManeuverEntryStatus.Succeeded);
        var failed = entries.Count(value => value.Status == ManeuverEntryStatus.Failed);
        var aggregationFailed = entries.Count(
            value => value.Status == ManeuverEntryStatus.AggregationFailed);
        var notRun = entries.Count(value => value.Status == ManeuverEntryStatus.NotRun);
        var validated = succeeded + failed;
        var attempted = validated + aggregationFailed;
        if (counts.RequestedExerciseCount != entries.Length
            || counts.AttemptedExerciseCount != attempted
            || counts.ValidatedExerciseCount != validated
            || counts.SucceededExerciseCount != succeeded
            || counts.FailedExerciseCount != failed
            || counts.AggregationFailedExerciseCount != aggregationFailed
            || counts.NotRunExerciseCount != notRun
            || (long)counts.AttemptedExerciseCount + counts.NotRunExerciseCount
                != counts.RequestedExerciseCount
            || (long)counts.SucceededExerciseCount + counts.FailedExerciseCount
                + counts.AggregationFailedExerciseCount + counts.NotRunExerciseCount
                != counts.RequestedExerciseCount)
            throw new ArgumentException("Maneuver report counts do not reconcile.", nameof(counts));
    }

    private static void RequireCausalStop(
        ManeuverReportStatus status,
        ManeuverReportEntry[] entries)
    {
        var aggregationStops = entries
            .Where(value => value.Status == ManeuverEntryStatus.AggregationFailed)
            .Select(value => value.Ordinal)
            .ToArray();
        var cancellationStops = entries
            .Where(value => value is
            {
                Status: ManeuverEntryStatus.Failed,
                FailureCategory: ExerciseFailureCategory.Cancelled,
            })
            .Select(value => value.Ordinal)
            .ToArray();
        if (aggregationStops.Length > 1 || cancellationStops.Length > 1)
            throw new ArgumentException("A Maneuver report can contain only one causal stop.");

        var firstNotRun = Array.FindIndex(
            entries,
            value => value.Status == ManeuverEntryStatus.NotRun);
        if (firstNotRun >= 0)
        {
            if (entries.Skip(firstNotRun).Any(
                    value => value.Status != ManeuverEntryStatus.NotRun))
                throw new ArgumentException("No attempted entry may follow a not-run entry.");
            var reason = entries[firstNotRun].NotRunReason!.Value;
            if (entries.Skip(firstNotRun).Any(value => value.NotRunReason != reason))
                throw new ArgumentException("A not-run tail must have one homogeneous reason.");

            switch (reason)
            {
                case ManeuverNotRunReason.AggregationStopped:
                    if (status != ManeuverReportStatus.AggregationFailed
                        || firstNotRun == 0
                        || entries[firstNotRun - 1].Status
                            != ManeuverEntryStatus.AggregationFailed)
                        throw new ArgumentException(
                            "An aggregation-stopped tail must immediately follow its stop.");
                    break;
                case ManeuverNotRunReason.Cancelled:
                    if (status != ManeuverReportStatus.Cancelled)
                        throw new ArgumentException(
                            "A cancelled tail requires cancelled report status.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(entries));
            }
        }

        if (aggregationStops.Length == 1)
        {
            var expectedStop = firstNotRun >= 0 ? firstNotRun - 1 : entries.Length - 1;
            if (aggregationStops[0] != expectedStop)
                throw new ArgumentException("Execution continued after an aggregation stop.");
        }
        if (cancellationStops.Length == 1)
        {
            var expectedStop = firstNotRun >= 0 ? firstNotRun - 1 : entries.Length - 1;
            if (cancellationStops[0] != expectedStop)
                throw new ArgumentException("Execution continued after cancellation.");
        }
    }

    private static void RequireTerminalCounts(
        ManeuverManifest manifest,
        ManeuverTerminalCount[] actual,
        ManeuverReportEntry[] entries)
    {
        var expected = entries
            .Where(value => value.Status == ManeuverEntryStatus.Succeeded)
            .Select(value => (BoundaryReached)value.TerminalOutcome!)
            .GroupBy(value => value.PositionId, StringComparer.Ordinal)
            .OrderBy(value => value.Key, StringComparer.Ordinal)
            .Select(value => new { PositionId = value.Key, Count = value.Count() })
            .ToArray();
        if (actual.Length != expected.Length) throw new ArgumentException("Terminal counts differ.");
        for (var index = 0; index < actual.Length; index++)
        {
            var boundary = (BoundaryReached)actual[index].Outcome;
            if (!string.Equals(boundary.PositionId, expected[index].PositionId, StringComparison.Ordinal)
                || actual[index].Count != expected[index].Count
                || !manifest.Exercises.Any(value => string.Equals(
                    value.TerminalBoundary,
                    boundary.PositionId,
                    StringComparison.Ordinal)))
                throw new ArgumentException("Terminal counts differ from successful entries.");
        }
    }

    private static void RequireFailureCounts(
        ManeuverFailureCount[] actual,
        ManeuverReportEntry[] entries)
    {
        var catalog = Enum.GetValues<ExerciseFailureCategory>();
        if (actual.Length != catalog.Length) throw new ArgumentException("Failure catalog is incomplete.");
        for (var index = 0; index < catalog.Length; index++)
        {
            var expectedCount = entries.Count(value => value.FailureCategory == catalog[index]);
            if (actual[index].Category != catalog[index] || actual[index].Count != expectedCount)
                throw new ArgumentException("Failure counts are incomplete, unordered, or incorrect.");
        }
    }

    private static void RequireAggregationFailureCounts(
        ManeuverAggregationFailureCount[] actual,
        ManeuverReportEntry[] entries)
    {
        var catalog = Enum.GetValues<ManeuverAggregationFailureCategory>();
        if (actual.Length != catalog.Length)
            throw new ArgumentException("Aggregation-failure catalog is incomplete.");
        for (var index = 0; index < catalog.Length; index++)
        {
            var expectedCount = entries.Count(
                value => value.AggregationFailureCategory == catalog[index]);
            if (actual[index].Category != catalog[index] || actual[index].Count != expectedCount)
                throw new ArgumentException(
                    "Aggregation-failure counts are incomplete, unordered, or incorrect.");
        }
    }

    private static ManeuverReportStatus ExpectedStatus(IReadOnlyList<ManeuverReportEntry> entries)
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
}

public sealed record ManeuverThroughput
{
    public ManeuverThroughput(int validatedExerciseCount, long elapsedMicroseconds)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(validatedExerciseCount);
        ArgumentOutOfRangeException.ThrowIfNegative(elapsedMicroseconds);
        ValidatedExerciseCount = validatedExerciseCount;
        ElapsedMicroseconds = elapsedMicroseconds;
    }

    public int ValidatedExerciseCount { get; }
    public long ElapsedMicroseconds { get; }
}

public sealed record ManeuverDiagnosticEntry
{
    public ManeuverDiagnosticEntry(
        int ordinal,
        long? elapsedMicroseconds,
        string? observedBundlePath,
        string? artifactManifestSha256)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ordinal);
        if (elapsedMicroseconds.HasValue)
            ArgumentOutOfRangeException.ThrowIfNegative(elapsedMicroseconds.Value);
        if (observedBundlePath is not null)
            ArgumentException.ThrowIfNullOrWhiteSpace(observedBundlePath);
        if (artifactManifestSha256 is not null)
            ReplayProofValidation.RequireSha256(
                artifactManifestSha256,
                nameof(artifactManifestSha256));
        Ordinal = ordinal;
        ElapsedMicroseconds = elapsedMicroseconds;
        ObservedBundlePath = observedBundlePath;
        ArtifactManifestSha256 = artifactManifestSha256;
    }

    public int Ordinal { get; }
    public long? ElapsedMicroseconds { get; }
    public string? ObservedBundlePath { get; }
    public string? ArtifactManifestSha256 { get; }
}

public sealed record ManeuverReportDiagnostics
{
    public ManeuverReportDiagnostics(
        long elapsedMicroseconds,
        ManeuverThroughput throughput,
        IEnumerable<ManeuverDiagnosticEntry> entries)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(elapsedMicroseconds);
        ArgumentNullException.ThrowIfNull(throughput);
        ArgumentNullException.ThrowIfNull(entries);
        var copy = entries.ToArray();
        if (copy.Any(value => value is null))
            throw new ArgumentException("Diagnostic entries cannot contain null.", nameof(entries));
        ElapsedMicroseconds = elapsedMicroseconds;
        Throughput = throughput;
        Entries = Array.AsReadOnly(copy);
    }

    public long ElapsedMicroseconds { get; }
    public ManeuverThroughput Throughput { get; }
    public IReadOnlyList<ManeuverDiagnosticEntry> Entries { get; }
}

public sealed class ManeuverReport
{
    public const int CurrentContractVersion = 1;
    public const string SchemeId = "sandtable.maneuver-report.v1";

    public ManeuverReport(
        ManeuverReportDeterministic deterministic,
        ManeuverReportDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(deterministic);
        ArgumentNullException.ThrowIfNull(diagnostics);
        if (diagnostics.ElapsedMicroseconds != diagnostics.Throughput.ElapsedMicroseconds
            || diagnostics.Throughput.ValidatedExerciseCount
                != deterministic.Counts.ValidatedExerciseCount
            || diagnostics.Entries.Count != deterministic.Entries.Count)
            throw new ArgumentException("Maneuver diagnostics do not reconcile.", nameof(diagnostics));
        for (var ordinal = 0; ordinal < diagnostics.Entries.Count; ordinal++)
        {
            if (diagnostics.Entries[ordinal].Ordinal != ordinal)
                throw new ArgumentException(
                    "Diagnostic entries must be complete and ordered.",
                    nameof(diagnostics));
        }

        ContractVersion = CurrentContractVersion;
        ContractSchemeId = SchemeId;
        Deterministic = deterministic;
        ReportFingerprint = ManeuverReportCodec.Fingerprint(deterministic);
        Diagnostics = diagnostics;
    }

    public int ContractVersion { get; }
    public string ContractSchemeId { get; }
    public ManeuverReportDeterministic Deterministic { get; }
    public string ReportFingerprint { get; }
    public ManeuverReportDiagnostics Diagnostics { get; }
}
