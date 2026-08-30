using Cna.Core.Actions;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Artifacts;

public enum PairedComparisonStatus
{
    Compared,
    Incomplete,
}

public enum PairedDivergenceKind
{
    None,
    AcceptedAction,
}

public sealed record PairedAcceptedActionIdentity
{
    public PairedAcceptedActionIdentity(
        int stepOrdinal,
        CampaignActionAudience audience,
        string actionId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(stepOrdinal);
        if (!Enum.IsDefined(audience)) throw new ArgumentOutOfRangeException(nameof(audience));
        ReplayProofValidation.RequireSha256(actionId, nameof(actionId));
        StepOrdinal = stepOrdinal;
        Audience = audience;
        ActionId = actionId;
    }

    public int StepOrdinal { get; }
    public CampaignActionAudience Audience { get; }
    public string ActionId { get; }
}

public sealed record PairedAcceptedActionDivergence
{
    public PairedAcceptedActionDivergence(
        PairedDivergenceKind kind,
        int? stepOrdinal,
        CampaignActionAudience? baselineAudience,
        string? baselineActionId,
        CampaignActionAudience? candidateAudience,
        string? candidateActionId)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if (stepOrdinal.HasValue) ArgumentOutOfRangeException.ThrowIfNegative(stepOrdinal.Value);
        RequireArm(baselineAudience, baselineActionId, nameof(baselineActionId));
        RequireArm(candidateAudience, candidateActionId, nameof(candidateActionId));
        var isEmpty = stepOrdinal is null
            && baselineAudience is null
            && baselineActionId is null
            && candidateAudience is null
            && candidateActionId is null;
        if (kind == PairedDivergenceKind.None && !isEmpty)
            throw new ArgumentException("A no-divergence record cannot identify an action.");
        if (kind == PairedDivergenceKind.AcceptedAction
            && (stepOrdinal is null
                || baselineAudience is null && candidateAudience is null))
            throw new ArgumentException(
                "An accepted-action divergence requires an ordinal and at least one observed arm action.");

        Kind = kind;
        StepOrdinal = stepOrdinal;
        BaselineAudience = baselineAudience;
        BaselineActionId = baselineActionId;
        CandidateAudience = candidateAudience;
        CandidateActionId = candidateActionId;
    }

    public PairedDivergenceKind Kind { get; }
    public int? StepOrdinal { get; }
    public CampaignActionAudience? BaselineAudience { get; }
    public string? BaselineActionId { get; }
    public CampaignActionAudience? CandidateAudience { get; }
    public string? CandidateActionId { get; }

    private static void RequireArm(
        CampaignActionAudience? audience,
        string? actionId,
        string parameterName)
    {
        if (audience.HasValue && !Enum.IsDefined(audience.Value))
            throw new ArgumentOutOfRangeException(nameof(audience));
        if (audience.HasValue != (actionId is not null))
            throw new ArgumentException(
                "A divergence arm must provide audience and action identity together.",
                parameterName);
        if (actionId is not null)
            ReplayProofValidation.RequireSha256(actionId, parameterName);
    }
}

public sealed record PairedManeuverComparison
{
    public PairedManeuverComparison(
        string pairKey,
        int repetition,
        int baselineEntryOrdinal,
        int candidateEntryOrdinal,
        PairedComparisonStatus status,
        string? creationInputsSha256,
        string? baselineInitialSnapshotSha256,
        string? candidateInitialSnapshotSha256,
        string? seedLedgerSha256,
        string? baselineControllerConfigurationSha256,
        string? candidateControllerConfigurationSha256,
        IEnumerable<PairedAcceptedActionIdentity>? baselineAcceptedActions,
        IEnumerable<PairedAcceptedActionIdentity>? candidateAcceptedActions,
        PairedAcceptedActionDivergence? firstDivergence,
        int? acceptedStepCountDelta,
        bool? terminalOutcomeEqual,
        bool? failureCategoryEqual)
    {
        StableIdValidation.Require(pairKey, nameof(pairKey));
        ArgumentOutOfRangeException.ThrowIfNegative(repetition);
        ArgumentOutOfRangeException.ThrowIfNegative(baselineEntryOrdinal);
        ArgumentOutOfRangeException.ThrowIfNegative(candidateEntryOrdinal);
        ArgumentOutOfRangeException.ThrowIfNotEqual(
            candidateEntryOrdinal,
            checked(baselineEntryOrdinal + 1));
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        RequireHash(creationInputsSha256, nameof(creationInputsSha256));
        RequireHash(baselineInitialSnapshotSha256, nameof(baselineInitialSnapshotSha256));
        RequireHash(candidateInitialSnapshotSha256, nameof(candidateInitialSnapshotSha256));
        RequireHash(seedLedgerSha256, nameof(seedLedgerSha256));
        RequireHash(
            baselineControllerConfigurationSha256,
            nameof(baselineControllerConfigurationSha256));
        RequireHash(
            candidateControllerConfigurationSha256,
            nameof(candidateControllerConfigurationSha256));

        var baselineActions = CopyActions(baselineAcceptedActions, nameof(baselineAcceptedActions));
        var candidateActions = CopyActions(candidateAcceptedActions, nameof(candidateAcceptedActions));
        var complete = creationInputsSha256 is not null
            && baselineInitialSnapshotSha256 is not null
            && candidateInitialSnapshotSha256 is not null
            && seedLedgerSha256 is not null
            && baselineControllerConfigurationSha256 is not null
            && candidateControllerConfigurationSha256 is not null
            && baselineActions is not null
            && candidateActions is not null
            && firstDivergence is not null
            && acceptedStepCountDelta.HasValue
            && terminalOutcomeEqual.HasValue
            && failureCategoryEqual.HasValue;
        var empty = creationInputsSha256 is null
            && baselineInitialSnapshotSha256 is null
            && candidateInitialSnapshotSha256 is null
            && seedLedgerSha256 is null
            && baselineControllerConfigurationSha256 is null
            && candidateControllerConfigurationSha256 is null
            && baselineActions is null
            && candidateActions is null
            && firstDivergence is null
            && acceptedStepCountDelta is null
            && terminalOutcomeEqual is null
            && failureCategoryEqual is null;
        if (status == PairedComparisonStatus.Compared ? !complete : !empty)
            throw new ArgumentException(
                "Paired comparison status contradicts its evidence fields.");

        PairKey = pairKey;
        Repetition = repetition;
        BaselineEntryOrdinal = baselineEntryOrdinal;
        CandidateEntryOrdinal = candidateEntryOrdinal;
        Status = status;
        CreationInputsSha256 = creationInputsSha256;
        BaselineInitialSnapshotSha256 = baselineInitialSnapshotSha256;
        CandidateInitialSnapshotSha256 = candidateInitialSnapshotSha256;
        SeedLedgerSha256 = seedLedgerSha256;
        BaselineControllerConfigurationSha256 = baselineControllerConfigurationSha256;
        CandidateControllerConfigurationSha256 = candidateControllerConfigurationSha256;
        BaselineAcceptedActions = baselineActions is null ? null : Array.AsReadOnly(baselineActions);
        CandidateAcceptedActions = candidateActions is null ? null : Array.AsReadOnly(candidateActions);
        FirstDivergence = firstDivergence;
        AcceptedStepCountDelta = acceptedStepCountDelta;
        TerminalOutcomeEqual = terminalOutcomeEqual;
        FailureCategoryEqual = failureCategoryEqual;
    }

    public string PairKey { get; }
    public int Repetition { get; }
    public int BaselineEntryOrdinal { get; }
    public int CandidateEntryOrdinal { get; }
    public PairedComparisonStatus Status { get; }
    public string? CreationInputsSha256 { get; }
    public string? BaselineInitialSnapshotSha256 { get; }
    public string? CandidateInitialSnapshotSha256 { get; }
    public string? SeedLedgerSha256 { get; }
    public string? BaselineControllerConfigurationSha256 { get; }
    public string? CandidateControllerConfigurationSha256 { get; }
    public IReadOnlyList<PairedAcceptedActionIdentity>? BaselineAcceptedActions { get; }
    public IReadOnlyList<PairedAcceptedActionIdentity>? CandidateAcceptedActions { get; }
    public PairedAcceptedActionDivergence? FirstDivergence { get; }
    public int? AcceptedStepCountDelta { get; }
    public bool? TerminalOutcomeEqual { get; }
    public bool? FailureCategoryEqual { get; }

    private static void RequireHash(string? value, string parameterName)
    {
        if (value is not null) ReplayProofValidation.RequireSha256(value, parameterName);
    }

    private static PairedAcceptedActionIdentity[]? CopyActions(
        IEnumerable<PairedAcceptedActionIdentity>? actions,
        string parameterName)
    {
        if (actions is null) return null;
        var copy = actions.ToArray();
        if (copy.Any(value => value is null))
            throw new ArgumentException("Accepted-action identities cannot contain null.", parameterName);
        for (var index = 0; index < copy.Length; index++)
        {
            if (copy[index].StepOrdinal != index)
                throw new ArgumentException(
                    "Accepted-action identities must be contiguous and zero-based.",
                    parameterName);
        }
        return copy;
    }
}

public sealed class PairedManeuverReportDeterministic
{
    public PairedManeuverReportDeterministic(
        PairedManeuverManifest manifest,
        ManeuverReportStatus status,
        ManeuverReportCounts counts,
        IEnumerable<ManeuverTerminalCount> terminalCounts,
        IEnumerable<ManeuverFailureCount> failureCounts,
        IEnumerable<ManeuverAggregationFailureCount> aggregationFailureCounts,
        IEnumerable<ManeuverReportEntry> entries,
        IEnumerable<PairedManeuverComparison> comparisons)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        ArgumentNullException.ThrowIfNull(counts);
        var terminalCopy = Copy(terminalCounts, nameof(terminalCounts));
        var failureCopy = Copy(failureCounts, nameof(failureCounts));
        var aggregationCopy = Copy(aggregationFailureCounts, nameof(aggregationFailureCounts));
        var entryCopy = Copy(entries, nameof(entries));
        var comparisonCopy = Copy(comparisons, nameof(comparisons));

        RequireEntries(manifest, entryCopy);
        RequireCounts(counts, entryCopy);
        RequireCatalogs(failureCopy, aggregationCopy, entryCopy);
        RequireTerminalCounts(terminalCopy, entryCopy);
        RequireCausalStop(status, entryCopy);
        if (status != ExpectedStatus(entryCopy))
            throw new ArgumentException(
                "The paired Maneuver status contradicts its entries.",
                nameof(status));
        RequireComparisons(manifest, entryCopy, comparisonCopy);

        Manifest = manifest;
        Status = status;
        Counts = counts;
        TerminalCounts = Array.AsReadOnly(terminalCopy);
        FailureCounts = Array.AsReadOnly(failureCopy);
        AggregationFailureCounts = Array.AsReadOnly(aggregationCopy);
        Entries = Array.AsReadOnly(entryCopy);
        Comparisons = Array.AsReadOnly(comparisonCopy);
    }

    public PairedManeuverManifest Manifest { get; }
    public ManeuverReportStatus Status { get; }
    public ManeuverReportCounts Counts { get; }
    public IReadOnlyList<ManeuverTerminalCount> TerminalCounts { get; }
    public IReadOnlyList<ManeuverFailureCount> FailureCounts { get; }
    public IReadOnlyList<ManeuverAggregationFailureCount> AggregationFailureCounts { get; }
    public IReadOnlyList<ManeuverReportEntry> Entries { get; }
    public IReadOnlyList<PairedManeuverComparison> Comparisons { get; }

    private static T[] Copy<T>(IEnumerable<T> values, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(values, parameterName);
        var copy = values.ToArray();
        if (copy.Any(value => value is null))
            throw new ArgumentException("Report collections cannot contain null.", parameterName);
        return copy;
    }

    private static void RequireEntries(
        PairedManeuverManifest manifest,
        ManeuverReportEntry[] entries)
    {
        if (entries.Length != manifest.ExerciseCount)
            throw new ArgumentException("The report must contain two entries per pair.");
        for (var pairOrdinal = 0; pairOrdinal < manifest.Pairs.Count; pairOrdinal++)
        {
            var pair = manifest.Pairs[pairOrdinal];
            var baselineOrdinal = checked(pairOrdinal * 2);
            var candidateOrdinal = baselineOrdinal + 1;
            RequireEntry(
                entries[baselineOrdinal],
                baselineOrdinal,
                pair.Baseline,
                ManeuverVariant.Baseline);
            RequireEntry(
                entries[candidateOrdinal],
                candidateOrdinal,
                pair.Candidate,
                ManeuverVariant.Candidate);
        }
    }

    private static void RequireEntry(
        ManeuverReportEntry entry,
        int ordinal,
        ManeuverExerciseManifest manifest,
        ManeuverVariant variant)
    {
        if (entry.Ordinal != ordinal
            || entry.Variant != variant
            || !string.Equals(entry.ExerciseId, manifest.ExerciseId, StringComparison.Ordinal))
            throw new ArgumentException("Paired report entries contradict manifest identity.");
        if (entry.TerminalOutcome is BoundaryReached boundary
            && !string.Equals(
                boundary.PositionId,
                manifest.TerminalBoundary,
                StringComparison.Ordinal))
            throw new ArgumentException(
                "A successful paired entry must reach its admitted terminal boundary.");
    }

    private static void RequireCounts(
        ManeuverReportCounts counts,
        ManeuverReportEntry[] entries)
    {
        var succeeded = entries.Count(value => value.Status == ManeuverEntryStatus.Succeeded);
        var failed = entries.Count(value => value.Status == ManeuverEntryStatus.Failed);
        var aggregateFailed = entries.Count(
            value => value.Status == ManeuverEntryStatus.AggregationFailed);
        var notRun = entries.Count(value => value.Status == ManeuverEntryStatus.NotRun);
        var validated = succeeded + failed;
        if (counts.RequestedExerciseCount != entries.Length
            || counts.AttemptedExerciseCount != validated + aggregateFailed
            || counts.ValidatedExerciseCount != validated
            || counts.SucceededExerciseCount != succeeded
            || counts.FailedExerciseCount != failed
            || counts.AggregationFailedExerciseCount != aggregateFailed
            || counts.NotRunExerciseCount != notRun
            || counts.AttemptedExerciseCount + counts.NotRunExerciseCount != entries.Length)
            throw new ArgumentException("Paired Maneuver report counts do not reconcile.");
    }

    private static void RequireCatalogs(
        ManeuverFailureCount[] failures,
        ManeuverAggregationFailureCount[] aggregationFailures,
        ManeuverReportEntry[] entries)
    {
        var failureCatalog = Enum.GetValues<ExerciseFailureCategory>();
        if (failures.Length != failureCatalog.Length)
            throw new ArgumentException("Failure catalog is incomplete.");
        for (var index = 0; index < failureCatalog.Length; index++)
        {
            if (failures[index].Category != failureCatalog[index]
                || failures[index].Count != entries.Count(
                    value => value.FailureCategory == failureCatalog[index]))
                throw new ArgumentException("Failure catalog is unordered or incorrect.");
        }
        var aggregationCatalog = Enum.GetValues<ManeuverAggregationFailureCategory>();
        if (aggregationFailures.Length != aggregationCatalog.Length)
            throw new ArgumentException("Aggregation-failure catalog is incomplete.");
        for (var index = 0; index < aggregationCatalog.Length; index++)
        {
            if (aggregationFailures[index].Category != aggregationCatalog[index]
                || aggregationFailures[index].Count != entries.Count(
                    value => value.AggregationFailureCategory == aggregationCatalog[index]))
                throw new ArgumentException(
                    "Aggregation-failure catalog is unordered or incorrect.");
        }
    }

    private static void RequireTerminalCounts(
        ManeuverTerminalCount[] actual,
        ManeuverReportEntry[] entries)
    {
        var expected = entries
            .Where(value => value.Status == ManeuverEntryStatus.Succeeded)
            .Select(value => (BoundaryReached)value.TerminalOutcome!)
            .GroupBy(value => value.PositionId, StringComparer.Ordinal)
            .OrderBy(value => value.Key, StringComparer.Ordinal)
            .Select(value => (value.Key, Count: value.Count()))
            .ToArray();
        if (actual.Length != expected.Length)
            throw new ArgumentException("Terminal counts differ.");
        for (var index = 0; index < actual.Length; index++)
        {
            var boundary = (BoundaryReached)actual[index].Outcome;
            if (!string.Equals(boundary.PositionId, expected[index].Key, StringComparison.Ordinal)
                || actual[index].Count != expected[index].Count)
                throw new ArgumentException("Terminal counts differ.");
        }
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
            throw new ArgumentException("A paired Maneuver report can contain only one causal stop.");

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

    private static void RequireComparisons(
        PairedManeuverManifest manifest,
        ManeuverReportEntry[] entries,
        PairedManeuverComparison[] comparisons)
    {
        if (comparisons.Length != manifest.Pairs.Count)
            throw new ArgumentException("The report must contain one comparison per pair.");
        for (var index = 0; index < comparisons.Length; index++)
        {
            var pair = manifest.Pairs[index];
            var comparison = comparisons[index];
            var baselineOrdinal = checked(index * 2);
            var candidateOrdinal = baselineOrdinal + 1;
            if (!string.Equals(comparison.PairKey, pair.PairKey, StringComparison.Ordinal)
                || comparison.Repetition != pair.Repetition
                || comparison.BaselineEntryOrdinal != baselineOrdinal
                || comparison.CandidateEntryOrdinal != candidateOrdinal)
                throw new ArgumentException("Pair comparisons contradict manifest identity.");

            var baseline = entries[baselineOrdinal];
            var candidate = entries[candidateOrdinal];
            var comparable = IsValidated(baseline) && IsValidated(candidate);
            if (comparable != (comparison.Status == PairedComparisonStatus.Compared))
                throw new ArgumentException("Pair comparison availability contradicts its entries.");
            if (!comparable) continue;
            var baselineActions = comparison.BaselineAcceptedActions!;
            var candidateActions = comparison.CandidateAcceptedActions!;
            if (!string.Equals(
                    comparison.CreationInputsSha256,
                    PairedManeuverPairingEvidence.HashCreationInputs(manifest, pair),
                    StringComparison.Ordinal)
                || !string.Equals(
                    comparison.BaselineInitialSnapshotSha256,
                    comparison.CandidateInitialSnapshotSha256,
                    StringComparison.Ordinal)
                || baselineActions.Count != baseline.AcceptedStepCount
                || candidateActions.Count != candidate.AcceptedStepCount
                || !Equals(
                    comparison.FirstDivergence,
                    PairedManeuverPairingEvidence.FindDivergence(
                        baselineActions,
                        candidateActions))
                || !string.Equals(
                    comparison.SeedLedgerSha256,
                    baseline.SeedLedgerSha256,
                    StringComparison.Ordinal)
                || !string.Equals(
                    comparison.SeedLedgerSha256,
                    candidate.SeedLedgerSha256,
                    StringComparison.Ordinal)
                || comparison.AcceptedStepCountDelta
                    != candidate.AcceptedStepCount - baseline.AcceptedStepCount
                || comparison.TerminalOutcomeEqual
                    != Equals(candidate.TerminalOutcome, baseline.TerminalOutcome)
                || comparison.FailureCategoryEqual
                    != (candidate.FailureCategory == baseline.FailureCategory)
                || !string.Equals(
                    comparison.BaselineControllerConfigurationSha256,
                    ExerciseConfigurationIdentity.ComputeHash(
                        pair.MaterializeBaseline(manifest.RootSeed)),
                    StringComparison.Ordinal)
                || !string.Equals(
                    comparison.CandidateControllerConfigurationSha256,
                    ExerciseConfigurationIdentity.ComputeHash(
                        pair.MaterializeCandidate(manifest.RootSeed)),
                    StringComparison.Ordinal))
                throw new ArgumentException("Paired comparison evidence contradicts its entries.");
        }
    }

    private static bool IsValidated(ManeuverReportEntry entry) => entry.Status is
        ManeuverEntryStatus.Succeeded or ManeuverEntryStatus.Failed;

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
}

public sealed class PairedManeuverReport
{
    public const int CurrentContractVersion = 1;
    public const string SchemeId = "sandtable.paired-maneuver-report.v1";
    public const string Interpretation =
        "Paired by identical declared initial conditions and initial RNG streams. "
        + "Action trajectories and subsequent random consumption may diverge after the first "
        + "differing choice. This report is descriptive only and makes no causal, "
        + "statistical-significance, gameplay-balance, or synchronized-post-divergence claim.";

    public PairedManeuverReport(
        PairedManeuverReportDeterministic deterministic,
        ManeuverReportDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(deterministic);
        ArgumentNullException.ThrowIfNull(diagnostics);
        if (diagnostics.ElapsedMicroseconds != diagnostics.Throughput.ElapsedMicroseconds
            || diagnostics.Throughput.ValidatedExerciseCount
                != deterministic.Counts.ValidatedExerciseCount
            || diagnostics.Entries.Count != deterministic.Entries.Count)
            throw new ArgumentException(
                "Paired Maneuver diagnostics do not reconcile.",
                nameof(diagnostics));
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
        ReportFingerprint = PairedManeuverReportCodec.Fingerprint(deterministic);
        Diagnostics = diagnostics;
    }

    public int ContractVersion { get; }
    public string ContractSchemeId { get; }
    public PairedManeuverReportDeterministic Deterministic { get; }
    public string ReportFingerprint { get; }
    public ManeuverReportDiagnostics Diagnostics { get; }
}
