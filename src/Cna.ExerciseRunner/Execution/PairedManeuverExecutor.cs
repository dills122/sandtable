using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Campaigns;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Execution;

internal static class PairedManeuverExecutor
{
    internal static PairedManeuverReport Execute(
        PairedManeuverManifest manifest,
        string repositoryRoot,
        string artifactRoot,
        CancellationToken cancellationToken) =>
        Execute(
            manifest,
            repositoryRoot,
            artifactRoot,
            ManeuverExecutionDependencies.Default,
            cancellationToken);

    internal static PairedManeuverReport Execute(
        PairedManeuverManifest manifest,
        string repositoryRoot,
        string artifactRoot,
        ManeuverExecutionDependencies dependencies,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactRoot);
        ArgumentNullException.ThrowIfNull(dependencies);

        var arms = Flatten(manifest);
        var started = Stopwatch.GetTimestamp();
        var entries = new List<ManeuverReportEntry>(arms.Length);
        var diagnostics = new List<ManeuverDiagnosticEntry>(arms.Length);
        var trustedChildren = new Dictionary<int, ManeuverChildBundleView>();

        for (var ordinal = 0; ordinal < arms.Length; ordinal++)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                AppendNotRunTail(
                    arms,
                    ordinal,
                    ManeuverNotRunReason.Cancelled,
                    entries,
                    diagnostics);
                break;
            }

            var arm = arms[ordinal];
            var entryStarted = Stopwatch.GetTimestamp();
            var materialized = arm.Manifest.Materialize(manifest.RootSeed);
            var normalizedManifest = ExerciseManifestCodec.Serialize(materialized);
            var identity = new ExerciseRunIdentity(
                manifest.RootSeed,
                manifest.ManeuverId,
                arm.Pair.Repetition,
                arm.Pair.PairKey);
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
                    arms,
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
                child = dependencies.ReadChildBundle(result.CompletedBundlePath)
                    ?? throw new InvalidDataException(
                        "The completed Exercise bundle reader returned no evidence.");
            }
            catch (Exception exception) when (!IsFatal(exception))
            {
                AppendAggregationFailure(
                    arms,
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
                    arms,
                    ordinal,
                    ManeuverNotRunReason.Cancelled,
                    entries,
                    diagnostics);
                break;
            }

            if (!IsAggregateEligibleProfile(child.Profile)
                || !HasExpectedIdentity(
                    child,
                    materialized,
                    normalizedManifest,
                    identity))
            {
                AppendAggregationFailure(
                    arms,
                    ordinal,
                    ManeuverAggregationFailureCategory.BundleIdentityMismatch,
                    result.CompletedBundlePath,
                    child.ArtifactManifestSha256,
                    ElapsedMicroseconds(entryStarted),
                    entries,
                    diagnostics);
                break;
            }

            if (arm.Variant == ManeuverVariant.Candidate
                && (!trustedChildren.TryGetValue(ordinal - 1, out var baseline)
                    || !HasEqualPairEvidence(
                        manifest,
                        arm.Pair,
                        baseline,
                        child)))
            {
                AppendAggregationFailure(
                    arms,
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
                    arm,
                    ordinal,
                    child,
                    normalizedManifest,
                    ElapsedMicroseconds(entryStarted),
                    entries,
                    diagnostics,
                    out var cancelled))
            {
                AppendAggregationFailure(
                    arms,
                    ordinal,
                    ManeuverAggregationFailureCategory.BundleInvalid,
                    result.CompletedBundlePath,
                    child.ArtifactManifestSha256,
                    ElapsedMicroseconds(entryStarted),
                    entries,
                    diagnostics);
                break;
            }

            trustedChildren.Add(ordinal, child);
            if (cancelled)
            {
                AppendNotRunTail(
                    arms,
                    ordinal + 1,
                    ManeuverNotRunReason.Cancelled,
                    entries,
                    diagnostics);
                break;
            }
        }

        return BuildReport(
            manifest,
            entries,
            diagnostics,
            trustedChildren,
            ElapsedMicroseconds(started));
    }

    private static bool HasEqualPairEvidence(
        PairedManeuverManifest manifest,
        PairedManeuverPairManifest pair,
        ManeuverChildBundleView baseline,
        ManeuverChildBundleView candidate)
    {
        if (baseline.InitialSnapshotBytes is not { } baselineSnapshot
            || candidate.InitialSnapshotBytes is not { } candidateSnapshot
            || !baselineSnapshot.AsSpan().SequenceEqual(candidateSnapshot)
            || baseline.AcceptedActions.Count != baseline.AcceptedStepCount
            || candidate.AcceptedActions.Count != candidate.AcceptedStepCount
            || baseline.SeedLedger is null
            || candidate.SeedLedger is null
            || !SeedLedgerCodec.Serialize(baseline.SeedLedger).AsSpan()
                .SequenceEqual(SeedLedgerCodec.Serialize(candidate.SeedLedger)))
            return false;

        var identity = new ExerciseRunIdentity(
            manifest.RootSeed,
            manifest.ManeuverId,
            pair.Repetition,
            pair.PairKey);
        var baselineRequest = ExerciseExecutor.CreateRequest(
            pair.MaterializeBaseline(manifest.RootSeed),
            identity);
        var candidateRequest = ExerciseExecutor.CreateRequest(
            pair.MaterializeCandidate(manifest.RootSeed),
            identity);
        return SerializeCreationInputs(baselineRequest).AsSpan()
                .SequenceEqual(SerializeCreationInputs(candidateRequest))
            && HasEqualBuildCohort(baseline.BuildIdentity!, candidate.BuildIdentity!);
    }

    private static bool HasEqualBuildCohort(BuildIdentity baseline, BuildIdentity candidate) =>
        baseline.ContractVersion == candidate.ContractVersion
        && string.Equals(
            baseline.ContractSchemeId,
            candidate.ContractSchemeId,
            StringComparison.Ordinal)
        && baseline.BuildMode == candidate.BuildMode
        && string.Equals(baseline.HeadCommit, candidate.HeadCommit, StringComparison.Ordinal)
        && string.Equals(baseline.HeadTree, candidate.HeadTree, StringComparison.Ordinal)
        && baseline.Dirty == candidate.Dirty
        && string.Equals(
            baseline.PorcelainSha256,
            candidate.PorcelainSha256,
            StringComparison.Ordinal)
        && string.Equals(
            baseline.FrameworkDescription,
            candidate.FrameworkDescription,
            StringComparison.Ordinal)
        && string.Equals(
            baseline.OsArchitecture,
            candidate.OsArchitecture,
            StringComparison.Ordinal)
        && string.Equals(
            baseline.ProcessArchitecture,
            candidate.ProcessArchitecture,
            StringComparison.Ordinal)
        && string.Equals(baseline.RulesetHash, candidate.RulesetHash, StringComparison.Ordinal)
        && string.Equals(
            baseline.SeedSchemeId,
            candidate.SeedSchemeId,
            StringComparison.Ordinal)
        && baseline.BaselineEligible == candidate.BaselineEligible
        && baseline.Reproducible == candidate.Reproducible
        && baseline.Artifacts.SequenceEqual(candidate.Artifacts);

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
            && string.Equals(actual.PairKey, identity.PairKey, StringComparison.Ordinal);
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

    private static bool TryAppendValidatedEntry(
        PairedArm arm,
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
        var manifestHash = ReplayEvidenceHasher.HashBytes(normalizedManifest);
        var ledgerHash = ReplayEvidenceHasher.HashBytes(
            SeedLedgerCodec.Serialize(child.SeedLedger!));
        ManeuverReportEntry entry;
        switch (child.RunResult.Completion)
        {
            case ExerciseSucceeded { Outcome: BoundaryReached boundary }
                when child.Profile == ArtifactBundleProfile.Succeeded
                    && string.Equals(
                        boundary.PositionId,
                        arm.Manifest.TerminalBoundary,
                        StringComparison.Ordinal)
                    && failedChecks == 0:
                entry = new ManeuverReportEntry(
                    ordinal,
                    arm.Manifest.ExerciseId,
                    arm.Variant,
                    ManeuverEntryStatus.Succeeded,
                    boundary,
                    null,
                    null,
                    null,
                    child.AcceptedStepCount,
                    passedChecks,
                    failedChecks,
                    manifestHash,
                    ledgerHash);
                cancelled = false;
                break;
            case ExerciseFailed { Failure.Category: var category }
                when IsAggregateEligibleFailureProfile(child.Profile, category):
                entry = new ManeuverReportEntry(
                    ordinal,
                    arm.Manifest.ExerciseId,
                    arm.Variant,
                    ManeuverEntryStatus.Failed,
                    null,
                    category,
                    null,
                    null,
                    child.AcceptedStepCount,
                    passedChecks,
                    failedChecks,
                    manifestHash,
                    ledgerHash);
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
        PairedArm[] arms,
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
            arms[ordinal].Manifest.ExerciseId,
            arms[ordinal].Variant,
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
            arms,
            ordinal + 1,
            ManeuverNotRunReason.AggregationStopped,
            entries,
            diagnostics);
    }

    private static void AppendNotRunTail(
        PairedArm[] arms,
        int firstOrdinal,
        ManeuverNotRunReason reason,
        List<ManeuverReportEntry> entries,
        List<ManeuverDiagnosticEntry> diagnostics)
    {
        for (var ordinal = firstOrdinal; ordinal < arms.Length; ordinal++)
        {
            entries.Add(new ManeuverReportEntry(
                ordinal,
                arms[ordinal].Manifest.ExerciseId,
                arms[ordinal].Variant,
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

    private static PairedManeuverReport BuildReport(
        PairedManeuverManifest manifest,
        IReadOnlyList<ManeuverReportEntry> entries,
        IReadOnlyList<ManeuverDiagnosticEntry> diagnostics,
        IReadOnlyDictionary<int, ManeuverChildBundleView> trustedChildren,
        long elapsedMicroseconds)
    {
        var succeeded = entries.Count(value => value.Status == ManeuverEntryStatus.Succeeded);
        var failed = entries.Count(value => value.Status == ManeuverEntryStatus.Failed);
        var aggregationFailed = entries.Count(
            value => value.Status == ManeuverEntryStatus.AggregationFailed);
        var notRun = entries.Count(value => value.Status == ManeuverEntryStatus.NotRun);
        var validated = succeeded + failed;
        var deterministic = new PairedManeuverReportDeterministic(
            manifest,
            ExpectedStatus(entries),
            new ManeuverReportCounts(
                manifest.ExerciseCount,
                validated + aggregationFailed,
                validated,
                succeeded,
                failed,
                aggregationFailed,
                notRun),
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
            entries,
            manifest.Pairs.Select((pair, pairOrdinal) => BuildComparison(
                manifest,
                pair,
                pairOrdinal,
                entries,
                trustedChildren)));
        return new PairedManeuverReport(
            deterministic,
            new ManeuverReportDiagnostics(
                elapsedMicroseconds,
                new ManeuverThroughput(validated, elapsedMicroseconds),
                diagnostics));
    }

    private static PairedManeuverComparison BuildComparison(
        PairedManeuverManifest manifest,
        PairedManeuverPairManifest pair,
        int pairOrdinal,
        IReadOnlyList<ManeuverReportEntry> entries,
        IReadOnlyDictionary<int, ManeuverChildBundleView> trustedChildren)
    {
        var baselineOrdinal = checked(pairOrdinal * 2);
        var candidateOrdinal = baselineOrdinal + 1;
        trustedChildren.TryGetValue(baselineOrdinal, out var baseline);
        trustedChildren.TryGetValue(candidateOrdinal, out var candidate);
        var comparable = IsValidated(entries[baselineOrdinal])
            && IsValidated(entries[candidateOrdinal])
            && baseline is not null
            && candidate is not null;
        if (!comparable)
            return new PairedManeuverComparison(
                pair.PairKey,
                pair.Repetition,
                baselineOrdinal,
                candidateOrdinal,
                PairedComparisonStatus.Incomplete,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);

        var identity = new ExerciseRunIdentity(
            manifest.RootSeed,
            manifest.ManeuverId,
            pair.Repetition,
            pair.PairKey);
        var creationInputs = SerializeCreationInputs(ExerciseExecutor.CreateRequest(
            pair.MaterializeBaseline(manifest.RootSeed),
            identity));
        return new PairedManeuverComparison(
            pair.PairKey,
            pair.Repetition,
            baselineOrdinal,
            candidateOrdinal,
            PairedComparisonStatus.Compared,
            ReplayEvidenceHasher.HashBytes(creationInputs),
            ReplayEvidenceHasher.HashBytes(baseline!.InitialSnapshotBytes!),
            ReplayEvidenceHasher.HashBytes(SeedLedgerCodec.Serialize(baseline.SeedLedger!)),
            ExerciseConfigurationIdentity.ComputeHash(
                pair.MaterializeBaseline(manifest.RootSeed)),
            ExerciseConfigurationIdentity.ComputeHash(
                pair.MaterializeCandidate(manifest.RootSeed)),
            FindDivergence(baseline.AcceptedActions, candidate!.AcceptedActions),
            candidate.AcceptedStepCount - baseline.AcceptedStepCount,
            Equals(entries[baselineOrdinal].TerminalOutcome, entries[candidateOrdinal].TerminalOutcome),
            entries[baselineOrdinal].FailureCategory == entries[candidateOrdinal].FailureCategory);
    }

    private static PairedAcceptedActionDivergence FindDivergence(
        IReadOnlyList<ExerciseAcceptedActionRecord> baseline,
        IReadOnlyList<ExerciseAcceptedActionRecord> candidate)
    {
        var common = Math.Min(baseline.Count, candidate.Count);
        for (var ordinal = 0; ordinal < common; ordinal++)
        {
            if (baseline[ordinal].Audience != candidate[ordinal].Audience
                || !string.Equals(
                    baseline[ordinal].ActionId,
                    candidate[ordinal].ActionId,
                    StringComparison.Ordinal))
                return Divergence(ordinal, baseline[ordinal], candidate[ordinal]);
        }
        if (baseline.Count != candidate.Count)
            return Divergence(
                common,
                common < baseline.Count ? baseline[common] : null,
                common < candidate.Count ? candidate[common] : null);
        return new PairedAcceptedActionDivergence(
            PairedDivergenceKind.None,
            null,
            null,
            null,
            null,
            null);
    }

    private static PairedAcceptedActionDivergence Divergence(
        int ordinal,
        ExerciseAcceptedActionRecord? baseline,
        ExerciseAcceptedActionRecord? candidate) => new(
        PairedDivergenceKind.AcceptedAction,
        ordinal,
        baseline?.Audience,
        baseline?.ActionId,
        candidate?.Audience,
        candidate?.ActionId);

    private static byte[] SerializeCreationInputs(CampaignCreationRequest request)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("schemeId", "sandtable.exercise-pairing-inputs.v1");
            writer.WriteString("campaignId", request.CampaignId);
            writer.WriteString("rulesetHash", request.RulesetHash);
            writer.WriteNumber("seed", request.Seed);
            writer.WriteString("setupId", request.SetupId);
            writer.WriteString("setupHash", request.SetupHash);
            writer.WriteString("contentPackId", request.ContentPackId);
            writer.WriteString("contentHash", request.ContentHash);
            writer.WriteString("scenarioId", request.ScenarioId);
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static PairedArm[] Flatten(PairedManeuverManifest manifest) => manifest.Pairs
        .SelectMany(pair => new[]
        {
            new PairedArm(pair, pair.Baseline, ManeuverVariant.Baseline),
            new PairedArm(pair, pair.Candidate, ManeuverVariant.Candidate),
        })
        .ToArray();

    private static bool IsValidated(ManeuverReportEntry entry) => entry.Status is
        ManeuverEntryStatus.Succeeded or ManeuverEntryStatus.Failed;

    private static bool IsAggregateEligibleProfile(ArtifactBundleProfile profile) =>
        profile is ArtifactBundleProfile.Succeeded
            or ArtifactBundleProfile.FailedExecuted
            or ArtifactBundleProfile.FailedReconstructed
            or ArtifactBundleProfile.FailedReadjudicated;

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

    private static bool IsFatal(Exception exception) =>
        exception is OutOfMemoryException
            or StackOverflowException
            or AccessViolationException
            or AppDomainUnloadedException
            or BadImageFormatException;

    private static long ElapsedMicroseconds(long started) =>
        Math.Max(0, Stopwatch.GetElapsedTime(started).Ticks / 10);

    private sealed record PairedArm(
        PairedManeuverPairManifest Pair,
        ManeuverExerciseManifest Manifest,
        ManeuverVariant Variant);
}
