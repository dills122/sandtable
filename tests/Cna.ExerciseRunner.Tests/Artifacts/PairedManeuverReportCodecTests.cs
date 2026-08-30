using System.Text;
using System.Text.Json;
using Cna.Core.Actions;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class PairedManeuverReportCodecTests
{
    private const string HashA =
        "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string HashB =
        "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string HashC =
        "sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";
    private const string Boundary =
        "land.position.operation-1.first-player.movement-and-combat.movement";

    [Fact]
    public void ReportRoundTripsCanonicalPairingInputsDivergenceAndHonestInterpretation()
    {
        var report = CreateReport();

        var bytes = PairedManeuverReportCodec.Serialize(report);
        var json = Encoding.UTF8.GetString(bytes);
        var admitted = PairedManeuverReportCodec.Deserialize(bytes);

        Assert.Equal(bytes, PairedManeuverReportCodec.Serialize(admitted));
        Assert.Contains(
            "\"pairKey\":\"reserve-policy\",\"repetition\":0,\"baselineEntryOrdinal\":0,\"candidateEntryOrdinal\":1,\"status\":\"compared\",\"creationInputsSha256\":\"sha256:aaaaaaaa",
            json,
            StringComparison.Ordinal);
        Assert.Contains(
            "\"firstDivergence\":{\"kind\":\"accepted-action\",\"stepOrdinal\":2,\"baselineAudience\":\"axis\",\"baselineActionId\":\"sha256:aaaaaaaa",
            json,
            StringComparison.Ordinal);
        Assert.Contains(PairedManeuverReport.Interpretation, json, StringComparison.Ordinal);
        Assert.Equal(
            "sha256:a8ea2417ed0057761dbd6463d3715fcd28d3cbbb0b5cf5edd0698f270cb6ab48",
            report.ReportFingerprint);
        Assert.Equal(report.ReportFingerprint, admitted.ReportFingerprint);
    }

    [Fact]
    public void DiagnosticsDoNotChangeTheDeterministicFingerprint()
    {
        var first = CreateReport(1000, "/tmp/first");
        var second = CreateReport(9000, "/different/path");

        Assert.Equal(first.ReportFingerprint, second.ReportFingerprint);
        Assert.NotEqual(
            PairedManeuverReportCodec.Serialize(first),
            PairedManeuverReportCodec.Serialize(second));
    }

    [Fact]
    public void ComparisonRejectsUnequalPairProofsAndContradictoryDivergence()
    {
        Assert.Throws<ArgumentException>(() => CreateReport(comparisonSeedHash: HashB));
        Assert.Throws<ArgumentException>(() => new PairedAcceptedActionDivergence(
            PairedDivergenceKind.None,
            0,
            CampaignActionAudience.Axis,
            HashA,
            CampaignActionAudience.Axis,
            HashB));
        Assert.Throws<ArgumentException>(() => new PairedAcceptedActionDivergence(
            PairedDivergenceKind.AcceptedAction,
            0,
            null,
            null,
            null,
            null));
    }

    [Fact]
    public void ReaderRejectsMalformedAmbiguousAndNoncanonicalReports()
    {
        var canonical = Encoding.UTF8.GetString(
            PairedManeuverReportCodec.Serialize(CreateReport()));
        string[] invalid =
        [
            canonical + "\n",
            canonical.Replace("{\"contractVersion\":1,", "{\"extra\":true,\"contractVersion\":1,", StringComparison.Ordinal),
            canonical.Replace(PairedManeuverReport.SchemeId, "sandtable.maneuver-report.v1", StringComparison.Ordinal),
            canonical.Replace("\"status\":\"compared\"", "\"status\":\"unknown\"", StringComparison.Ordinal),
            canonical.Replace("\"reportFingerprint\":\"sha256:", "\"reportFingerprint\":\"sha256:0", StringComparison.Ordinal),
            canonical.Replace("\"baselineEntryOrdinal\":0,\"candidateEntryOrdinal\":1", "\"candidateEntryOrdinal\":1,\"baselineEntryOrdinal\":0", StringComparison.Ordinal),
        ];

        Assert.All(invalid, value => Assert.ThrowsAny<JsonException>(() =>
            PairedManeuverReportCodec.Deserialize(Encoding.UTF8.GetBytes(value))));
    }

    private static PairedManeuverReport CreateReport(
        long elapsedMicroseconds = 1000,
        string baselinePath = "/tmp/baseline",
        string comparisonSeedHash = HashC)
    {
        var manifest = CreateManifest();
        ManeuverReportEntry[] entries =
        [
            new(0, "reserve-policy.baseline", ManeuverVariant.Baseline,
                ManeuverEntryStatus.Succeeded, new BoundaryReached(Boundary), null, null, null,
                10, 20, 0, HashA, HashC),
            new(1, "reserve-policy.candidate", ManeuverVariant.Candidate,
                ManeuverEntryStatus.Succeeded, new BoundaryReached(Boundary), null, null, null,
                12, 24, 0, HashB, HashC),
        ];
        var deterministic = new PairedManeuverReportDeterministic(
            manifest,
            ManeuverReportStatus.Succeeded,
            new ManeuverReportCounts(2, 2, 2, 2, 0, 0, 0),
            [new ManeuverTerminalCount(new BoundaryReached(Boundary), 2)],
            Enum.GetValues<ExerciseFailureCategory>()
                .Select(value => new ManeuverFailureCount(value, 0)),
            Enum.GetValues<ManeuverAggregationFailureCategory>()
                .Select(value => new ManeuverAggregationFailureCount(value, 0)),
            entries,
            [Compared(manifest, comparisonSeedHash)]);
        return new PairedManeuverReport(
            deterministic,
            new ManeuverReportDiagnostics(
                elapsedMicroseconds,
                new ManeuverThroughput(2, elapsedMicroseconds),
                [
                    new ManeuverDiagnosticEntry(0, 400, baselinePath, HashA),
                    new ManeuverDiagnosticEntry(1, 500, "/tmp/candidate", HashB),
                ]));
    }

    private static PairedManeuverComparison Compared(
        PairedManeuverManifest manifest,
        string seedLedgerSha256) => new(
        "reserve-policy",
        0,
        0,
        1,
        PairedComparisonStatus.Compared,
        HashA,
        HashB,
        seedLedgerSha256,
        ExerciseConfigurationIdentity.ComputeHash(
            manifest.Pairs[0].MaterializeBaseline(manifest.RootSeed)),
        ExerciseConfigurationIdentity.ComputeHash(
            manifest.Pairs[0].MaterializeCandidate(manifest.RootSeed)),
        new PairedAcceptedActionDivergence(
            PairedDivergenceKind.AcceptedAction,
            2,
            CampaignActionAudience.Axis,
            HashA,
            CampaignActionAudience.Axis,
            HashB),
        2,
        true,
        true);

    private static PairedManeuverManifest CreateManifest() => new(
        PairedManeuverManifest.CurrentContractVersion,
        PairedManeuverManifest.SchemeId,
        "rules-lab.paired",
        PairedManeuverMode.SerialPaired,
        1844,
        new ManeuverReportOptions(ManeuverReportProfile.TrustedAuthority),
        [new PairedManeuverPairManifest(
            PairedManeuverPairManifest.CurrentContractVersion,
            "reserve-policy",
            0,
            Exercise("reserve-policy.baseline", ExerciseControllerPolicy.FirstByActionId),
            Exercise(
                "reserve-policy.candidate",
                ExerciseControllerPolicy.DesignateAllReservesThenFirstByActionId))]);

    private static ManeuverExerciseManifest Exercise(
        string exerciseId,
        ExerciseControllerPolicy controller) => new(
        ExerciseManifest.CurrentContractVersion,
        exerciseId,
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
        new ExerciseControllerManifest(controller, controller, controller),
        null);
}
