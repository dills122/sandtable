using Cna.Core.Actions;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class PairedManeuverReportCausalStopTests
{
    private const string Hash =
        "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Boundary =
        "land.position.operation-1.first-player.movement-and-combat.movement";

    [Fact]
    public void RejectsOrphanAggregationStoppedTail()
    {
        ManeuverReportEntry[] entries =
        [
            Succeeded(0),
            NotRun(1, ManeuverNotRunReason.AggregationStopped),
            NotRun(2, ManeuverNotRunReason.AggregationStopped),
            NotRun(3, ManeuverNotRunReason.AggregationStopped),
        ];

        Assert.Throws<ArgumentException>(() => Deterministic(
            ManeuverReportStatus.Succeeded,
            entries));
    }

    [Fact]
    public void RejectsMixedNotRunTailReasons()
    {
        ManeuverReportEntry[] entries =
        [
            AggregationFailed(0),
            NotRun(1, ManeuverNotRunReason.AggregationStopped),
            NotRun(2, ManeuverNotRunReason.Cancelled),
            NotRun(3, ManeuverNotRunReason.AggregationStopped),
        ];

        Assert.Throws<ArgumentException>(() => Deterministic(
            ManeuverReportStatus.AggregationFailed,
            entries));
    }

    [Fact]
    public void RejectsExecutionAfterCancellationStop()
    {
        ManeuverReportEntry[] entries =
        [
            Cancelled(0),
            AggregationFailed(1),
            NotRun(2, ManeuverNotRunReason.AggregationStopped),
            NotRun(3, ManeuverNotRunReason.AggregationStopped),
        ];

        Assert.Throws<ArgumentException>(() => Deterministic(
            ManeuverReportStatus.AggregationFailed,
            entries));
    }

    [Fact]
    public void RejectsSuccessfulStatusWithCancelledNotRunTail()
    {
        ManeuverReportEntry[] entries =
        [
            Succeeded(0),
            NotRun(1, ManeuverNotRunReason.Cancelled),
            NotRun(2, ManeuverNotRunReason.Cancelled),
            NotRun(3, ManeuverNotRunReason.Cancelled),
        ];

        Assert.Throws<ArgumentException>(() => Deterministic(
            ManeuverReportStatus.Succeeded,
            entries));
    }

    private static PairedManeuverReportDeterministic Deterministic(
        ManeuverReportStatus status,
        ManeuverReportEntry[] entries)
    {
        var succeeded = entries.Count(value => value.Status == ManeuverEntryStatus.Succeeded);
        var failed = entries.Count(value => value.Status == ManeuverEntryStatus.Failed);
        var aggregationFailed = entries.Count(
            value => value.Status == ManeuverEntryStatus.AggregationFailed);
        var notRun = entries.Count(value => value.Status == ManeuverEntryStatus.NotRun);
        return new PairedManeuverReportDeterministic(
            Manifest(),
            status,
            new ManeuverReportCounts(
                entries.Length,
                succeeded + failed + aggregationFailed,
                succeeded + failed,
                succeeded,
                failed,
                aggregationFailed,
                notRun),
            succeeded == 0
                ? []
                : [new ManeuverTerminalCount(new BoundaryReached(Boundary), succeeded)],
            Enum.GetValues<ExerciseFailureCategory>()
                .Select(category => new ManeuverFailureCount(
                    category,
                    entries.Count(value => value.FailureCategory == category))),
            Enum.GetValues<ManeuverAggregationFailureCategory>()
                .Select(category => new ManeuverAggregationFailureCount(
                    category,
                    entries.Count(value => value.AggregationFailureCategory == category))),
            entries,
            [Incomplete(0), Incomplete(1)]);
    }

    private static PairedManeuverComparison Incomplete(int pairOrdinal) => new(
        "pair",
        pairOrdinal,
        pairOrdinal * 2,
        pairOrdinal * 2 + 1,
        PairedComparisonStatus.Incomplete,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null,
        null);

    private static ManeuverReportEntry Succeeded(int ordinal) => new(
        ordinal,
        ExerciseId(ordinal),
        Variant(ordinal),
        ManeuverEntryStatus.Succeeded,
        new BoundaryReached(Boundary),
        null,
        null,
        null,
        1,
        3,
        0,
        Hash,
        Hash);

    private static ManeuverReportEntry Cancelled(int ordinal) => new(
        ordinal,
        ExerciseId(ordinal),
        Variant(ordinal),
        ManeuverEntryStatus.Failed,
        null,
        ExerciseFailureCategory.Cancelled,
        null,
        null,
        0,
        1,
        1,
        Hash,
        Hash);

    private static ManeuverReportEntry AggregationFailed(int ordinal) => new(
        ordinal,
        ExerciseId(ordinal),
        Variant(ordinal),
        ManeuverEntryStatus.AggregationFailed,
        null,
        null,
        ManeuverAggregationFailureCategory.BundleInvalid,
        null,
        null,
        null,
        null,
        null,
        null);

    private static ManeuverReportEntry NotRun(
        int ordinal,
        ManeuverNotRunReason reason) => new(
        ordinal,
        ExerciseId(ordinal),
        Variant(ordinal),
        ManeuverEntryStatus.NotRun,
        null,
        null,
        null,
        reason,
        null,
        null,
        null,
        null,
        null);

    private static string ExerciseId(int ordinal) => $"pair.{ordinal}";

    private static ManeuverVariant Variant(int ordinal) => ordinal % 2 == 0
        ? ManeuverVariant.Baseline
        : ManeuverVariant.Candidate;

    private static PairedManeuverManifest Manifest() => new(
        1,
        PairedManeuverManifest.SchemeId,
        "paired.causal-stop",
        PairedManeuverMode.SerialPaired,
        1,
        new ManeuverReportOptions(ManeuverReportProfile.TrustedAuthority),
        [Pair(0), Pair(1)]);

    private static PairedManeuverPairManifest Pair(int repetition) => new(
        1,
        "pair",
        repetition,
        Exercise(repetition * 2),
        Exercise(repetition * 2 + 1));

    private static ManeuverExerciseManifest Exercise(int ordinal) => new(
        2,
        ExerciseId(ordinal),
        "rules-lab.initiative.predetermined",
        "sha256:9e55e3de11338ba6432768ccb6740a6fed83b37503f69cc7ff8ecd58e205634f",
        "rules-lab.content.movement-contact.v1",
        "sha256:40f0e7a0a8876e4fefc4f06c1d752253cf338da614e587b9ff017e04541e7d79",
        "movement-contact-lab",
        Cna1979Ruleset.Manifest.Hash,
        Boundary,
        16,
        ExerciseBuildMode.Exploratory,
        ExerciseConfidentiality.TrustedAuthority,
        ExerciseDetail.Forensic,
        new ExerciseControllerManifest(
            ExerciseControllerPolicy.FirstByActionId,
            ExerciseControllerPolicy.FirstByActionId,
            ExerciseControllerPolicy.FirstByActionId),
        null);
}
