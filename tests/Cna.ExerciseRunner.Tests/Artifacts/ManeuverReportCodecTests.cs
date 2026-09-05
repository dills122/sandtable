using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class ManeuverReportCodecTests
{
    private const string HashA =
        "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string HashB =
        "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string HashC =
        "sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";
    private const string Boundary = "land.position.operation-1.organization";

    [Fact]
    public void ReportHasExactCanonicalVersionOneBytesAndDeterministicFingerprint()
    {
        var report = CreateSucceededReport();
        var manifestJson = Encoding.UTF8.GetString(
            ManeuverManifestCodec.Serialize(report.Deterministic.Manifest));
        var failureCounts =
            "[{\"category\":\"manifest-invalid\",\"count\":0},{\"category\":\"build-identity-unavailable\",\"count\":0},{\"category\":\"controller-failed\",\"count\":0},{\"category\":\"no-unique-legal-action\",\"count\":0},{\"category\":\"illegal-action\",\"count\":0},{\"category\":\"invariant-failed\",\"count\":0},{\"category\":\"reconstruction-mismatch\",\"count\":0},{\"category\":\"readjudication-mismatch\",\"count\":0},{\"category\":\"step-limit-exceeded\",\"count\":0},{\"category\":\"cancelled\",\"count\":0},{\"category\":\"artifact-failed\",\"count\":0},{\"category\":\"unexpected-failure\",\"count\":0}]";
        var aggregationCounts =
            "[{\"category\":\"completed-bundle-missing\",\"count\":0},{\"category\":\"bundle-invalid\",\"count\":0},{\"category\":\"bundle-identity-mismatch\",\"count\":0}]";
        var deterministicJson =
            $"{{\"manifest\":{manifestJson},\"status\":\"succeeded\",\"counts\":{{\"requestedExerciseCount\":1,\"attemptedExerciseCount\":1,\"validatedExerciseCount\":1,\"succeededExerciseCount\":1,\"failedExerciseCount\":0,\"aggregationFailedExerciseCount\":0,\"notRunExerciseCount\":0}},\"terminalCounts\":[{{\"kind\":\"boundary-reached\",\"positionId\":\"{Boundary}\",\"victor\":null,\"count\":1}}],\"failureCounts\":{failureCounts},\"aggregationFailureCounts\":{aggregationCounts},\"entries\":[{{\"ordinal\":0,\"exerciseId\":\"organization-boundary.first\",\"variant\":\"unpaired\",\"status\":\"succeeded\",\"terminalOutcome\":{{\"kind\":\"boundary-reached\",\"positionId\":\"{Boundary}\",\"victor\":null}},\"failureCategory\":null,\"aggregationFailureCategory\":null,\"notRunReason\":null,\"acceptedStepCount\":3,\"passedCheckCount\":8,\"failedCheckCount\":0,\"normalizedManifestSha256\":\"{HashA}\",\"seedLedgerSha256\":\"{HashB}\"}}]}}";
        var fingerprint = Hash(Encoding.UTF8.GetBytes(deterministicJson));
        var expected =
            $"{{\"contractVersion\":1,\"schemeId\":\"sandtable.maneuver-report.v1\",\"deterministic\":{deterministicJson},\"reportFingerprint\":\"{fingerprint}\",\"diagnostics\":{{\"elapsedMicroseconds\":1200,\"throughput\":{{\"validatedExerciseCount\":1,\"elapsedMicroseconds\":1200}},\"entries\":[{{\"ordinal\":0,\"elapsedMicroseconds\":900,\"observedBundlePath\":\"/tmp/exercise-0\",\"artifactManifestSha256\":\"{HashC}\"}}]}}}}";

        var bytes = ManeuverReportCodec.Serialize(report);

        Assert.Equal(expected, Encoding.UTF8.GetString(bytes));
        Assert.Equal(fingerprint, report.ReportFingerprint);
        Assert.Equal(bytes, ManeuverReportCodec.Serialize(ManeuverReportCodec.Deserialize(bytes)));
    }

    [Fact]
    public void DiagnosticsAreVisibleButExcludedFromTheFingerprint()
    {
        var first = CreateSucceededReport(1200, "/tmp/exercise-0", HashC);
        var second = CreateSucceededReport(9999, "/different/machine/path", HashA);

        Assert.Equal(first.ReportFingerprint, second.ReportFingerprint);
        Assert.NotEqual(
            Encoding.UTF8.GetString(ManeuverReportCodec.Serialize(first)),
            Encoding.UTF8.GetString(ManeuverReportCodec.Serialize(second)));
    }

    [Fact]
    public void CanonicalBytesAndFingerprintAreIndependentOfCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var baseline = ManeuverReportCodec.Serialize(CreateSucceededReport());
            var baselineFingerprint = CreateSucceededReport().ReportFingerprint;
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ar-SA");

            var localized = CreateSucceededReport();
            Assert.Equal(baseline, ManeuverReportCodec.Serialize(localized));
            Assert.Equal(baselineFingerprint, localized.ReportFingerprint);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void ReaderRejectsChangedFingerprintAndNoncanonicalTopLevelShapes()
    {
        var report = CreateSucceededReport();
        var json = Encoding.UTF8.GetString(ManeuverReportCodec.Serialize(report));
        string[] invalid =
        [
            json.Replace(report.ReportFingerprint, HashA, StringComparison.Ordinal),
            json.Replace("{\"contractVersion\":1,", "{\"extra\":true,\"contractVersion\":1,", StringComparison.Ordinal),
            json.Replace("\"schemeId\":\"sandtable.maneuver-report.v1\",", "", StringComparison.Ordinal),
            json.Replace("{\"contractVersion\":1,\"schemeId\":", "{\"schemeId\":\"wrong\",\"contractVersion\":1,\"schemeId\":", StringComparison.Ordinal),
            json + "\n",
        ];

        Assert.All(invalid, value => Assert.ThrowsAny<JsonException>(() =>
            ManeuverReportCodec.Deserialize(Encoding.UTF8.GetBytes(value))));
    }

    [Fact]
    public void DeterministicContractRejectsCountReconciliationFailures()
    {
        var source = CreateSucceededReport().Deterministic;
        ManeuverReportCounts[] invalid =
        [
            new(2, 1, 1, 1, 0, 0, 0),
            new(1, 0, 1, 1, 0, 0, 0),
            new(1, 1, 0, 1, 0, 0, 0),
            new(1, 1, 1, 0, 0, 0, 0),
        ];

        Assert.All(invalid, counts => Assert.Throws<ArgumentException>(() =>
            new ManeuverReportDeterministic(
                source.Manifest,
                source.Status,
                counts,
                source.TerminalCounts,
                source.FailureCounts,
                source.AggregationFailureCounts,
                source.Entries)));
    }

    [Fact]
    public void DeterministicContractRequiresCompleteOrderedFailureCatalogsIncludingZeros()
    {
        var source = CreateSucceededReport().Deterministic;
        var failureCounts = source.FailureCounts.Reverse().ToArray();
        var aggregationCounts = source.AggregationFailureCounts.Reverse().ToArray();

        Assert.Throws<ArgumentException>(() => new ManeuverReportDeterministic(
            source.Manifest,
            source.Status,
            source.Counts,
            source.TerminalCounts,
            failureCounts,
            source.AggregationFailureCounts,
            source.Entries));
        Assert.Throws<ArgumentException>(() => new ManeuverReportDeterministic(
            source.Manifest,
            source.Status,
            source.Counts,
            source.TerminalCounts,
            source.FailureCounts,
            aggregationCounts,
            source.Entries));
    }

    [Fact]
    public void EntryContractEnforcesTheCompleteStatusAndNullabilityMatrix()
    {
        Assert.Throws<ArgumentException>(() => Entry(
            ManeuverEntryStatus.Succeeded,
            terminalOutcome: null,
            acceptedStepCount: 1,
            passedCheckCount: 1,
            failedCheckCount: 0,
            normalizedManifestSha256: HashA,
            seedLedgerSha256: HashB));
        Assert.Throws<ArgumentException>(() => Entry(
            ManeuverEntryStatus.Succeeded,
            terminalOutcome: new BoundaryReached(Boundary),
            failureCategory: ExerciseFailureCategory.IllegalAction,
            acceptedStepCount: 1,
            passedCheckCount: 1,
            failedCheckCount: 0,
            normalizedManifestSha256: HashA,
            seedLedgerSha256: HashB));
        Assert.Throws<ArgumentException>(() => Entry(
            ManeuverEntryStatus.Failed,
            failureCategory: ExerciseFailureCategory.IllegalAction,
            acceptedStepCount: null,
            passedCheckCount: 1,
            failedCheckCount: 1,
            normalizedManifestSha256: HashA,
            seedLedgerSha256: HashB));
        Assert.Throws<ArgumentException>(() => Entry(
            ManeuverEntryStatus.AggregationFailed,
            aggregationFailureCategory: ManeuverAggregationFailureCategory.BundleInvalid,
            acceptedStepCount: 0));
        Assert.Throws<ArgumentException>(() => Entry(
            ManeuverEntryStatus.NotRun,
            notRunReason: ManeuverNotRunReason.Cancelled,
            normalizedManifestSha256: HashA));
    }

    [Fact]
    public void TaskFourteenRejectsVictoryAndWrongBoundaryTerminalFacts()
    {
        Assert.Throws<ArgumentException>(() => Entry(
            ManeuverEntryStatus.Succeeded,
            terminalOutcome: new VictoryReached("axis"),
            acceptedStepCount: 1,
            passedCheckCount: 1,
            failedCheckCount: 0,
            normalizedManifestSha256: HashA,
            seedLedgerSha256: HashB));

        var manifest = CreateManifest();
        var entry = Entry(
            ManeuverEntryStatus.Succeeded,
            terminalOutcome: new BoundaryReached("land.position.wrong"),
            acceptedStepCount: 1,
            passedCheckCount: 1,
            failedCheckCount: 0,
            normalizedManifestSha256: HashA,
            seedLedgerSha256: HashB);
        Assert.Throws<ArgumentException>(() => SuccessfulDeterministic(manifest, entry));
    }

    [Fact]
    public void OverallStatusAndAggregateCatalogCountsMustMatchEntries()
    {
        var source = CreateSucceededReport().Deterministic;
        var wrongStatus = ManeuverReportStatus.ExerciseFailed;
        var wrongFailureCounts = FailureCounts(ExerciseFailureCategory.IllegalAction);

        Assert.Throws<ArgumentException>(() => new ManeuverReportDeterministic(
            source.Manifest,
            wrongStatus,
            source.Counts,
            source.TerminalCounts,
            source.FailureCounts,
            source.AggregationFailureCounts,
            source.Entries));
        Assert.Throws<ArgumentException>(() => new ManeuverReportDeterministic(
            source.Manifest,
            source.Status,
            source.Counts,
            source.TerminalCounts,
            wrongFailureCounts,
            source.AggregationFailureCounts,
            source.Entries));
    }

    [Fact]
    public void CompleteEntryStateMatrixRoundTripsWithAggregationStatusPrecedence()
    {
        var manifest = CreateManifest(
            "organization-boundary.first",
            "organization-boundary.second",
            "organization-boundary.third",
            "organization-boundary.fourth");
        ManeuverReportEntry[] entries =
        [
            new(0, "organization-boundary.first", ManeuverVariant.Unpaired,
                ManeuverEntryStatus.Succeeded, new BoundaryReached(Boundary), null, null, null,
                3, 8, 0, HashA, HashB),
            new(1, "organization-boundary.second", ManeuverVariant.Unpaired,
                ManeuverEntryStatus.Failed, null, ExerciseFailureCategory.IllegalAction, null, null,
                1, 4, 1, HashA, HashB),
            new(2, "organization-boundary.third", ManeuverVariant.Unpaired,
                ManeuverEntryStatus.AggregationFailed, null, null,
                ManeuverAggregationFailureCategory.BundleInvalid, null,
                null, null, null, null, null),
            new(3, "organization-boundary.fourth", ManeuverVariant.Unpaired,
                ManeuverEntryStatus.NotRun, null, null, null,
                ManeuverNotRunReason.AggregationStopped,
                null, null, null, null, null),
        ];
        var deterministic = new ManeuverReportDeterministic(
            manifest,
            ManeuverReportStatus.AggregationFailed,
            new ManeuverReportCounts(4, 3, 2, 1, 1, 1, 1),
            [new ManeuverTerminalCount(new BoundaryReached(Boundary), 1)],
            FailureCounts(ExerciseFailureCategory.IllegalAction),
            AggregationFailureCounts(ManeuverAggregationFailureCategory.BundleInvalid),
            entries);
        var report = new ManeuverReport(
            deterministic,
            new ManeuverReportDiagnostics(
                4000,
                new ManeuverThroughput(2, 4000),
                [
                    new ManeuverDiagnosticEntry(0, 500, "/tmp/first", HashC),
                    new ManeuverDiagnosticEntry(1, 600, "/tmp/second", HashC),
                    new ManeuverDiagnosticEntry(2, null, "/tmp/third", null),
                    new ManeuverDiagnosticEntry(3, null, null, null),
                ]));

        var admitted = ManeuverReportCodec.Deserialize(ManeuverReportCodec.Serialize(report));

        Assert.Equal(ManeuverReportStatus.AggregationFailed, admitted.Deterministic.Status);
        Assert.Collection(
            admitted.Deterministic.Entries,
            value => Assert.Equal(ManeuverEntryStatus.Succeeded, value.Status),
            value => Assert.Equal(ManeuverEntryStatus.Failed, value.Status),
            value => Assert.Equal(ManeuverEntryStatus.AggregationFailed, value.Status),
            value => Assert.Equal(ManeuverEntryStatus.NotRun, value.Status));
    }

    [Fact]
    public void CancelledChildAndTailRequireCancelledOverallStatus()
    {
        var manifest = CreateManifest(
            "organization-boundary.first",
            "organization-boundary.second");
        ManeuverReportEntry[] entries =
        [
            new(0, "organization-boundary.first", ManeuverVariant.Unpaired,
                ManeuverEntryStatus.Failed, null, ExerciseFailureCategory.Cancelled, null, null,
                0, 3, 1, HashA, HashB),
            new(1, "organization-boundary.second", ManeuverVariant.Unpaired,
                ManeuverEntryStatus.NotRun, null, null, null, ManeuverNotRunReason.Cancelled,
                null, null, null, null, null),
        ];

        Assert.Throws<ArgumentException>(() => new ManeuverReportDeterministic(
            manifest,
            ManeuverReportStatus.ExerciseFailed,
            new ManeuverReportCounts(2, 1, 1, 0, 1, 0, 1),
            [],
            FailureCounts(ExerciseFailureCategory.Cancelled),
            AggregationFailureCounts(),
            entries));
        _ = new ManeuverReportDeterministic(
            manifest,
            ManeuverReportStatus.Cancelled,
            new ManeuverReportCounts(2, 1, 1, 0, 1, 0, 1),
            [],
            FailureCounts(ExerciseFailureCategory.Cancelled),
            AggregationFailureCounts(),
            entries);
    }

    [Fact]
    public void DeterministicContractRejectsMultipleOrNonterminalAggregationStopPoints()
    {
        var multipleManifest = CreateManifest(
            "organization-boundary.first",
            "organization-boundary.second",
            "organization-boundary.third");
        ManeuverReportEntry[] multiple =
        [
            EntryAt(0, "organization-boundary.first", ManeuverEntryStatus.AggregationFailed,
                aggregationFailureCategory: ManeuverAggregationFailureCategory.BundleInvalid),
            EntryAt(1, "organization-boundary.second", ManeuverEntryStatus.AggregationFailed,
                aggregationFailureCategory:
                    ManeuverAggregationFailureCategory.BundleIdentityMismatch),
            EntryAt(2, "organization-boundary.third", ManeuverEntryStatus.NotRun,
                notRunReason: ManeuverNotRunReason.AggregationStopped),
        ];
        var nonterminalManifest = CreateManifest(
            "organization-boundary.first",
            "organization-boundary.second",
            "organization-boundary.third");
        ManeuverReportEntry[] nonterminal =
        [
            EntryAt(0, "organization-boundary.first", ManeuverEntryStatus.AggregationFailed,
                aggregationFailureCategory: ManeuverAggregationFailureCategory.BundleInvalid),
            EntryAt(1, "organization-boundary.second", ManeuverEntryStatus.Succeeded,
                terminalOutcome: new BoundaryReached(Boundary), acceptedStepCount: 1,
                passedCheckCount: 8, failedCheckCount: 0,
                normalizedManifestSha256: HashA, seedLedgerSha256: HashB),
            EntryAt(2, "organization-boundary.third", ManeuverEntryStatus.NotRun,
                notRunReason: ManeuverNotRunReason.AggregationStopped),
        ];

        Assert.Throws<ArgumentException>(() => Deterministic(
            multipleManifest,
            ManeuverReportStatus.AggregationFailed,
            multiple));
        Assert.Throws<ArgumentException>(() => Deterministic(
            nonterminalManifest,
            ManeuverReportStatus.AggregationFailed,
            nonterminal));
    }

    [Fact]
    public void DeterministicContractRejectsAttemptedEntriesAfterANotRunTailBegins()
    {
        var manifest = CreateManifest(
            "organization-boundary.first",
            "organization-boundary.second");
        ManeuverReportEntry[] entries =
        [
            EntryAt(0, "organization-boundary.first", ManeuverEntryStatus.NotRun,
                notRunReason: ManeuverNotRunReason.Cancelled),
            EntryAt(1, "organization-boundary.second", ManeuverEntryStatus.Failed,
                failureCategory: ExerciseFailureCategory.Cancelled,
                acceptedStepCount: 0, passedCheckCount: 3, failedCheckCount: 1,
                normalizedManifestSha256: HashA, seedLedgerSha256: HashB),
        ];

        Assert.Throws<ArgumentException>(() => Deterministic(
            manifest,
            ManeuverReportStatus.Cancelled,
            entries));
    }

    [Fact]
    public void DeterministicContractRejectsUncausedAggregationAndHeterogeneousNotRunTails()
    {
        var uncausedAggregationManifest = CreateManifest(
            "organization-boundary.first",
            "organization-boundary.second");
        ManeuverReportEntry[] uncausedAggregation =
        [
            EntryAt(0, "organization-boundary.first", ManeuverEntryStatus.Failed,
                failureCategory: ExerciseFailureCategory.IllegalAction,
                acceptedStepCount: 0, passedCheckCount: 3, failedCheckCount: 1,
                normalizedManifestSha256: HashA, seedLedgerSha256: HashB),
            EntryAt(1, "organization-boundary.second", ManeuverEntryStatus.NotRun,
                notRunReason: ManeuverNotRunReason.AggregationStopped),
        ];
        var heterogeneousManifest = CreateManifest(
            "organization-boundary.first",
            "organization-boundary.second",
            "organization-boundary.third");
        ManeuverReportEntry[] heterogeneous =
        [
            EntryAt(0, "organization-boundary.first", ManeuverEntryStatus.Failed,
                failureCategory: ExerciseFailureCategory.Cancelled,
                acceptedStepCount: 0, passedCheckCount: 3, failedCheckCount: 1,
                normalizedManifestSha256: HashA, seedLedgerSha256: HashB),
            EntryAt(1, "organization-boundary.second", ManeuverEntryStatus.NotRun,
                notRunReason: ManeuverNotRunReason.Cancelled),
            EntryAt(2, "organization-boundary.third", ManeuverEntryStatus.NotRun,
                notRunReason: ManeuverNotRunReason.AggregationStopped),
        ];

        Assert.Throws<ArgumentException>(() => Deterministic(
            uncausedAggregationManifest,
            ManeuverReportStatus.AggregationFailed,
            uncausedAggregation));
        Assert.Throws<ArgumentException>(() => Deterministic(
            heterogeneousManifest,
            ManeuverReportStatus.Cancelled,
            heterogeneous));
    }

    [Fact]
    public void ValidCancellationAndAggregationStopTailsRemainAdmitted()
    {
        var preChildManifest = CreateManifest(
            "organization-boundary.first",
            "organization-boundary.second");
        ManeuverReportEntry[] preChild =
        [
            EntryAt(0, "organization-boundary.first", ManeuverEntryStatus.NotRun,
                notRunReason: ManeuverNotRunReason.Cancelled),
            EntryAt(1, "organization-boundary.second", ManeuverEntryStatus.NotRun,
                notRunReason: ManeuverNotRunReason.Cancelled),
        ];
        var cancelledChildManifest = CreateManifest(
            "organization-boundary.first",
            "organization-boundary.second");
        ManeuverReportEntry[] cancelledChild =
        [
            EntryAt(0, "organization-boundary.first", ManeuverEntryStatus.Failed,
                failureCategory: ExerciseFailureCategory.Cancelled,
                acceptedStepCount: 0, passedCheckCount: 3, failedCheckCount: 1,
                normalizedManifestSha256: HashA, seedLedgerSha256: HashB),
            EntryAt(1, "organization-boundary.second", ManeuverEntryStatus.NotRun,
                notRunReason: ManeuverNotRunReason.Cancelled),
        ];
        var externalCancellationManifest = CreateManifest(
            "organization-boundary.first",
            "organization-boundary.second");
        ManeuverReportEntry[] externalCancellation =
        [
            EntryAt(0, "organization-boundary.first", ManeuverEntryStatus.Failed,
                failureCategory: ExerciseFailureCategory.IllegalAction,
                acceptedStepCount: 0, passedCheckCount: 3, failedCheckCount: 1,
                normalizedManifestSha256: HashA, seedLedgerSha256: HashB),
            EntryAt(1, "organization-boundary.second", ManeuverEntryStatus.NotRun,
                notRunReason: ManeuverNotRunReason.Cancelled),
        ];
        var aggregationManifest = CreateManifest(
            "organization-boundary.first",
            "organization-boundary.second",
            "organization-boundary.third");
        ManeuverReportEntry[] aggregation =
        [
            EntryAt(0, "organization-boundary.first", ManeuverEntryStatus.Succeeded,
                terminalOutcome: new BoundaryReached(Boundary), acceptedStepCount: 1,
                passedCheckCount: 8, failedCheckCount: 0,
                normalizedManifestSha256: HashA, seedLedgerSha256: HashB),
            EntryAt(1, "organization-boundary.second", ManeuverEntryStatus.AggregationFailed,
                aggregationFailureCategory: ManeuverAggregationFailureCategory.BundleInvalid),
            EntryAt(2, "organization-boundary.third", ManeuverEntryStatus.NotRun,
                notRunReason: ManeuverNotRunReason.AggregationStopped),
        ];

        _ = Deterministic(preChildManifest, ManeuverReportStatus.Cancelled, preChild);
        _ = Deterministic(
            cancelledChildManifest,
            ManeuverReportStatus.Cancelled,
            cancelledChild);
        _ = Deterministic(
            externalCancellationManifest,
            ManeuverReportStatus.Cancelled,
            externalCancellation);
        _ = Deterministic(
            aggregationManifest,
            ManeuverReportStatus.AggregationFailed,
            aggregation);
    }

    [Fact]
    public void ReaderRejectsReFingerprintedTailReasonAndStatusMismatch()
    {
        var report = CancelledReport();
        var json = Encoding.UTF8.GetString(ManeuverReportCodec.Serialize(report));
        var altered = json.Replace(
            "\"notRunReason\":\"cancelled\"",
            "\"notRunReason\":\"aggregation-stopped\"",
            StringComparison.Ordinal);
        using var document = JsonDocument.Parse(altered);
        var deterministicBytes = Encoding.UTF8.GetBytes(
            document.RootElement.GetProperty("deterministic").GetRawText());
        altered = altered.Replace(
            report.ReportFingerprint,
            Hash(deterministicBytes),
            StringComparison.Ordinal);

        Assert.ThrowsAny<JsonException>(() =>
            ManeuverReportCodec.Deserialize(Encoding.UTF8.GetBytes(altered)));
    }

    [Fact]
    public void DiagnosticsRequireOneOrderedEntryAndReconciledThroughput()
    {
        var source = CreateSucceededReport();

        Assert.Throws<ArgumentException>(() => new ManeuverReport(
            source.Deterministic,
            new ManeuverReportDiagnostics(
                1200,
                new ManeuverThroughput(0, 1200),
                source.Diagnostics.Entries)));
        Assert.Throws<ArgumentException>(() => new ManeuverReport(
            source.Deterministic,
            new ManeuverReportDiagnostics(
                1200,
                new ManeuverThroughput(1, 1200),
                [])));
    }

    private static ManeuverReport CreateSucceededReport(
        long elapsedMicroseconds = 1200,
        string observedBundlePath = "/tmp/exercise-0",
        string artifactManifestSha256 = HashC)
    {
        var manifest = CreateManifest();
        var entry = Entry(
            ManeuverEntryStatus.Succeeded,
            terminalOutcome: new BoundaryReached(Boundary),
            acceptedStepCount: 3,
            passedCheckCount: 8,
            failedCheckCount: 0,
            normalizedManifestSha256: HashA,
            seedLedgerSha256: HashB);
        return new ManeuverReport(
            SuccessfulDeterministic(manifest, entry),
            new ManeuverReportDiagnostics(
                elapsedMicroseconds,
                new ManeuverThroughput(1, elapsedMicroseconds),
                [new ManeuverDiagnosticEntry(
                    0,
                    900,
                    observedBundlePath,
                    artifactManifestSha256)]));
    }

    private static ManeuverReport CancelledReport()
    {
        var manifest = CreateManifest(
            "organization-boundary.first",
            "organization-boundary.second");
        ManeuverReportEntry[] entries =
        [
            EntryAt(0, "organization-boundary.first", ManeuverEntryStatus.Failed,
                failureCategory: ExerciseFailureCategory.Cancelled,
                acceptedStepCount: 0, passedCheckCount: 3, failedCheckCount: 1,
                normalizedManifestSha256: HashA, seedLedgerSha256: HashB),
            EntryAt(1, "organization-boundary.second", ManeuverEntryStatus.NotRun,
                notRunReason: ManeuverNotRunReason.Cancelled),
        ];
        return new ManeuverReport(
            Deterministic(manifest, ManeuverReportStatus.Cancelled, entries),
            new ManeuverReportDiagnostics(
                100,
                new ManeuverThroughput(1, 100),
                [
                    new ManeuverDiagnosticEntry(0, 50, "/tmp/first", HashC),
                    new ManeuverDiagnosticEntry(1, null, null, null),
                ]));
    }

    private static ManeuverReportDeterministic Deterministic(
        ManeuverManifest manifest,
        ManeuverReportStatus status,
        ManeuverReportEntry[] entries)
    {
        var succeeded = entries.Count(value => value.Status == ManeuverEntryStatus.Succeeded);
        var failed = entries.Count(value => value.Status == ManeuverEntryStatus.Failed);
        var aggregationFailed = entries.Count(
            value => value.Status == ManeuverEntryStatus.AggregationFailed);
        var notRun = entries.Count(value => value.Status == ManeuverEntryStatus.NotRun);
        var validated = succeeded + failed;
        return new ManeuverReportDeterministic(
            manifest,
            status,
            new ManeuverReportCounts(
                entries.Length,
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
            entries);
    }

    private static ManeuverReportDeterministic SuccessfulDeterministic(
        ManeuverManifest manifest,
        ManeuverReportEntry entry) => new(
            manifest,
            ManeuverReportStatus.Succeeded,
            new ManeuverReportCounts(1, 1, 1, 1, 0, 0, 0),
            [new ManeuverTerminalCount(new BoundaryReached(Boundary), 1)],
            FailureCounts(),
            AggregationFailureCounts(),
            [entry]);

    private static ManeuverReportEntry Entry(
        ManeuverEntryStatus status,
        ExerciseTerminalOutcome? terminalOutcome = null,
        ExerciseFailureCategory? failureCategory = null,
        ManeuverAggregationFailureCategory? aggregationFailureCategory = null,
        ManeuverNotRunReason? notRunReason = null,
        int? acceptedStepCount = null,
        int? passedCheckCount = null,
        int? failedCheckCount = null,
        string? normalizedManifestSha256 = null,
        string? seedLedgerSha256 = null) => new(
            0,
            "organization-boundary.first",
            ManeuverVariant.Unpaired,
            status,
            terminalOutcome,
            failureCategory,
            aggregationFailureCategory,
            notRunReason,
            acceptedStepCount,
            passedCheckCount,
            failedCheckCount,
            normalizedManifestSha256,
            seedLedgerSha256);

    private static ManeuverReportEntry EntryAt(
        int ordinal,
        string exerciseId,
        ManeuverEntryStatus status,
        ExerciseTerminalOutcome? terminalOutcome = null,
        ExerciseFailureCategory? failureCategory = null,
        ManeuverAggregationFailureCategory? aggregationFailureCategory = null,
        ManeuverNotRunReason? notRunReason = null,
        int? acceptedStepCount = null,
        int? passedCheckCount = null,
        int? failedCheckCount = null,
        string? normalizedManifestSha256 = null,
        string? seedLedgerSha256 = null) => new(
            ordinal,
            exerciseId,
            ManeuverVariant.Unpaired,
            status,
            terminalOutcome,
            failureCategory,
            aggregationFailureCategory,
            notRunReason,
            acceptedStepCount,
            passedCheckCount,
            failedCheckCount,
            normalizedManifestSha256,
            seedLedgerSha256);

    private static ManeuverFailureCount[] FailureCounts(
        ExerciseFailureCategory? populated = null) =>
        Enum.GetValues<ExerciseFailureCategory>()
            .Select(category => new ManeuverFailureCount(
                category,
                category == populated ? 1 : 0))
            .ToArray();

    private static ManeuverAggregationFailureCount[] AggregationFailureCounts(
        ManeuverAggregationFailureCategory? populated = null) =>
    [
        new(ManeuverAggregationFailureCategory.CompletedBundleMissing,
            populated == ManeuverAggregationFailureCategory.CompletedBundleMissing ? 1 : 0),
        new(ManeuverAggregationFailureCategory.BundleInvalid,
            populated == ManeuverAggregationFailureCategory.BundleInvalid ? 1 : 0),
        new(ManeuverAggregationFailureCategory.BundleIdentityMismatch,
            populated == ManeuverAggregationFailureCategory.BundleIdentityMismatch ? 1 : 0),
    ];

    private static ManeuverManifest CreateManifest(params string[] exerciseIds)
    {
        if (exerciseIds.Length == 0) exerciseIds = ["organization-boundary.first"];
        var exercises = exerciseIds.Select(exerciseId => new ManeuverExerciseManifest(
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
            null)).ToArray();
        return new ManeuverManifest(
            ManeuverManifest.CurrentContractVersion,
            ManeuverManifest.SchemeId,
            "rules-lab.serial",
            ManeuverMode.SerialUnpaired,
            0,
            new ManeuverReportOptions(ManeuverReportProfile.TrustedAuthority),
            exercises);
    }

    private static string Hash(string value) => Hash(Encoding.UTF8.GetBytes(value));

    private static string Hash(byte[] value) =>
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(value))}";
}
