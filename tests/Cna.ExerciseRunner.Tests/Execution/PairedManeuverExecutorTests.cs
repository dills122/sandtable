using Cna.Core.Actions;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Execution;

public sealed class PairedManeuverExecutorTests
{
    private const string Boundary =
        "land.position.operation-1.first-player.movement-and-combat.movement";
    private const string HashA =
        "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string HashB =
        "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void RunsIsolatedArmsSequentiallyWithEqualPairIdentitySeedsAndInitialSnapshot()
    {
        var manifest = Manifest();
        var events = new List<string>();
        var requests = new List<ExerciseRunCoordinatorRequest>();
        var dependencies = new ManeuverExecutionDependencies(
            request =>
            {
                requests.Add(request);
                var flatOrdinal = requests.Count - 1;
                events.Add($"run:{flatOrdinal}");
                return Completed(flatOrdinal);
            },
            path =>
            {
                var ordinal = ParseOrdinal(path);
                events.Add($"read:{ordinal}");
                return View(manifest, ordinal, [1, 2, 3]);
            });

        var report = PairedManeuverExecutor.Execute(
            manifest,
            "/repository",
            "/artifacts",
            dependencies,
            TestContext.Current.CancellationToken);

        Assert.Equal(["run:0", "read:0", "run:1", "read:1"], events);
        Assert.Equal(2, requests.Count);
        Assert.All(requests, request =>
        {
            Assert.Equal(manifest.RootSeed, request.RunIdentity.RootSeed);
            Assert.Equal(manifest.ManeuverId, request.RunIdentity.ManeuverId);
            Assert.Equal(0, request.RunIdentity.ExerciseOrdinal);
            Assert.Equal("reserve-policy", request.RunIdentity.PairKey);
        });
        Assert.NotSame(requests[0].Telemetry, requests[1].Telemetry);
        var comparison = Assert.Single(report.Deterministic.Comparisons);
        Assert.Equal(PairedComparisonStatus.Compared, comparison.Status);
        Assert.Equal(PairedDivergenceKind.AcceptedAction, comparison.FirstDivergence!.Kind);
        Assert.Equal(1, comparison.FirstDivergence.StepOrdinal);
        Assert.Equal(0, comparison.AcceptedStepCountDelta);
        Assert.Equal(
            report.Deterministic.Entries[0].SeedLedgerSha256,
            report.Deterministic.Entries[1].SeedLedgerSha256);
        Assert.NotEqual(
            comparison.BaselineControllerConfigurationSha256,
            comparison.CandidateControllerConfigurationSha256);
    }

    [Fact]
    public void PairEvidenceMismatchFailsAggregationAndDoesNotClaimAComparison()
    {
        var manifest = Manifest();
        var dependencies = new ManeuverExecutionDependencies(
            request => Completed(request.Manifest.ExerciseId.EndsWith(
                "baseline", StringComparison.Ordinal) ? 0 : 1),
            path => ParseOrdinal(path) == 0
                ? View(manifest, 0, [1, 2, 3])
                : View(manifest, 1, [9, 9, 9]));

        var report = PairedManeuverExecutor.Execute(
            manifest,
            "/repository",
            "/artifacts",
            dependencies,
            TestContext.Current.CancellationToken);

        Assert.Equal(ManeuverReportStatus.AggregationFailed, report.Deterministic.Status);
        Assert.Equal(ManeuverEntryStatus.Succeeded, report.Deterministic.Entries[0].Status);
        Assert.Equal(ManeuverEntryStatus.AggregationFailed, report.Deterministic.Entries[1].Status);
        var comparison = Assert.Single(report.Deterministic.Comparisons);
        Assert.Equal(PairedComparisonStatus.Incomplete, comparison.Status);
        Assert.Null(comparison.CreationInputsSha256);
        Assert.Null(comparison.FirstDivergence);
    }

    [Fact]
    public void DeterministicPairReportRepeatsDespiteDiagnosticTimingVariance()
    {
        var manifest = Manifest();
        PairedManeuverReport Run() => PairedManeuverExecutor.Execute(
            manifest,
            "/repository",
            "/artifacts",
            new ManeuverExecutionDependencies(
                request => Completed(request.Manifest.ExerciseId.EndsWith(
                    "baseline", StringComparison.Ordinal) ? 0 : 1),
                path => View(manifest, ParseOrdinal(path), [1, 2, 3])),
            TestContext.Current.CancellationToken);

        var first = Run();
        var second = Run();

        Assert.Equal(first.ReportFingerprint, second.ReportFingerprint);
        Assert.Equal(
            PairedManeuverReportCodec.SerializeDeterministic(first.Deterministic),
            PairedManeuverReportCodec.SerializeDeterministic(second.Deterministic));
    }

    private static ManeuverChildBundleView View(
        PairedManeuverManifest manifest,
        int flatOrdinal,
        byte[] initialSnapshot)
    {
        var pair = manifest.Pairs[0];
        var materialized = flatOrdinal == 0
            ? pair.MaterializeBaseline(manifest.RootSeed)
            : pair.MaterializeCandidate(manifest.RootSeed);
        var normalized = ExerciseManifestCodec.Serialize(materialized);
        var identity = new ExerciseRunIdentity(
            manifest.RootSeed,
            manifest.ManeuverId,
            pair.Repetition,
            pair.PairKey);
        var campaignId = ExerciseCampaignId.Derive(identity);
        var actions = flatOrdinal == 0
            ? new[]
            {
                Action(0, campaignId, HashA),
                Action(1, campaignId, HashA),
            }
            : new[]
            {
                Action(0, campaignId, HashA),
                Action(1, campaignId, HashB),
            };
        return new ManeuverChildBundleView(
            $"bundle-{flatOrdinal}",
            ArtifactBundleProfile.Succeeded,
            HashA,
            normalized,
            BuildIdentity(materialized, normalized),
            ExerciseSeedLedger.Create(identity),
            ExerciseRunResult.Succeeded(new BoundaryReached(Boundary)),
            new ExerciseCheckResults([]),
            actions.Length,
            actions,
            initialSnapshot);
    }

    private static ExerciseAcceptedActionRecord Action(
        int ordinal,
        string campaignId,
        string actionId) => new(
        ordinal,
        campaignId,
        ordinal + 1,
        ordinal + 2,
        Boundary,
        CampaignActionAudience.Axis,
        actionId);

    private static BuildIdentity BuildIdentity(
        ExerciseManifest manifest,
        byte[] normalized) => new(
        ExerciseBuildMode.Exploratory,
        "1111111111111111111111111111111111111111",
        "2222222222222222222222222222222222222222",
        true,
        HashA,
        ".NET 10",
        "arm64",
        "arm64",
        manifest.RulesetHash,
        ExerciseConfigurationIdentity.ComputeHash(manifest),
        ReplayEvidenceHasher.HashBytes(normalized),
        ExerciseSeedLedger.SchemeId,
        false,
        false,
        [new BuildArtifactIdentity("runner.dll", 1, HashA)]);

    private static ExerciseRunCoordinatorResult Completed(int ordinal) => new(
        ExerciseProcessExitCode.Succeeded,
        $"bundle-{ordinal}",
        null,
        null);

    private static int ParseOrdinal(string path) =>
        int.Parse(path.AsSpan("bundle-".Length), System.Globalization.CultureInfo.InvariantCulture);

    private static PairedManeuverManifest Manifest() => new(
        1,
        PairedManeuverManifest.SchemeId,
        "rules-lab.paired",
        PairedManeuverMode.SerialPaired,
        1844,
        new ManeuverReportOptions(ManeuverReportProfile.TrustedAuthority),
        [new PairedManeuverPairManifest(
            1,
            "reserve-policy",
            0,
            Exercise("reserve-policy.baseline", ExerciseControllerPolicy.FirstByActionId),
            Exercise(
                "reserve-policy.candidate",
                ExerciseControllerPolicy.DesignateAllReservesThenFirstByActionId))]);

    private static ManeuverExerciseManifest Exercise(
        string id,
        ExerciseControllerPolicy policy) => new(
        2, id,
        "rules-lab.initiative.predetermined",
        "sha256:9e55e3de11338ba6432768ccb6740a6fed83b37503f69cc7ff8ecd58e205634f",
        "rules-lab.content.movement-contact.v1",
        "sha256:40f0e7a0a8876e4fefc4f06c1d752253cf338da614e587b9ff017e04541e7d79",
        "movement-contact-lab", Cna1979Ruleset.Manifest.Hash, Boundary, 16,
        ExerciseBuildMode.Exploratory, ExerciseConfidentiality.TrustedAuthority,
        ExerciseDetail.Forensic,
        new ExerciseControllerManifest(policy, policy, policy),
        null);
}
