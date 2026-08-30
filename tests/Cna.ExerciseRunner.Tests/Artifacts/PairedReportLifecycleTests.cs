using Cna.Core.Actions;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class PairedReportLifecycleTests
{
    private const string HashA =
        "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Boundary =
        "land.position.operation-1.first-player.movement-and-combat.movement";

    [Fact]
    public void WriterFinalizesAndStrictlyReopensOneCanonicalPairedReport()
    {
        using var temp = new TemporaryDirectory();
        var report = CreateReport();

        var artifact = PairedReportWriter.Write(
            temp.Path,
            report,
            static (_, _, _) => { },
            "paired-success");

        Assert.EndsWith(
            Path.Combine("maneuvers", "succeeded", "paired-success"),
            artifact.Path,
            StringComparison.Ordinal);
        Assert.Equal(
            PairedManeuverReportCodec.Serialize(report),
            artifact.CanonicalBytes);
        Assert.Equal(
            report.ReportFingerprint,
            PairedReportReader.Read(artifact.Path).Report.ReportFingerprint);
    }

    [Fact]
    public void ReaderRejectsExtraFiles()
    {
        using var temp = new TemporaryDirectory();
        var artifact = PairedReportWriter.Write(
            temp.Path,
            CreateReport(),
            static (_, _, _) => { },
            "paired-strict");
        File.WriteAllText(Path.Combine(artifact.Path, "extra.txt"), "extra");

        Assert.Throws<InvalidDataException>(() => PairedReportReader.Read(artifact.Path));
    }

    [Theory]
    [InlineData(ManeuverReportWriterFailpoint.StagingCreated)]
    [InlineData(ManeuverReportWriterFailpoint.BeforeReportCreate)]
    [InlineData(ManeuverReportWriterFailpoint.AfterReportCreate)]
    [InlineData(ManeuverReportWriterFailpoint.BeforeReportWrite)]
    [InlineData(ManeuverReportWriterFailpoint.AfterReportWrite)]
    [InlineData(ManeuverReportWriterFailpoint.BeforeReportFlush)]
    [InlineData(ManeuverReportWriterFailpoint.AfterReportFlush)]
    [InlineData(ManeuverReportWriterFailpoint.BeforeMove)]
    public void PreMoveInterruptionsRetainOnlyUntrustedPartialEvidence(
        ManeuverReportWriterFailpoint failpoint)
    {
        using var temp = new TemporaryDirectory();
        var runId = $"partial-{failpoint}";

        Assert.Throws<InjectedFailure>(() => PairedReportWriter.Write(
            temp.Path,
            CreateReport(),
            (point, _, _) =>
            {
                if (point == failpoint) throw new InjectedFailure();
            },
            runId));

        var partial = Path.Combine(temp.Path, "maneuvers", ".partial", runId);
        Assert.True(Directory.Exists(partial));
        Assert.False(Directory.Exists(Path.Combine(
            temp.Path,
            "maneuvers",
            "succeeded",
            runId)));
        Assert.Throws<InvalidDataException>(() => PairedReportReader.Read(partial));
    }

    [Theory]
    [InlineData(ManeuverReportWriterFailpoint.AfterMove)]
    [InlineData(ManeuverReportWriterFailpoint.BeforeReadback)]
    [InlineData(ManeuverReportWriterFailpoint.AfterReadback)]
    public void PostMoveInterruptionsRetainValidFinalEvidenceWithoutReturningIt(
        ManeuverReportWriterFailpoint failpoint)
    {
        using var temp = new TemporaryDirectory();
        var runId = $"moved-{failpoint}";

        Assert.Throws<InjectedFailure>(() => PairedReportWriter.Write(
            temp.Path,
            CreateReport(),
            (point, _, _) =>
            {
                if (point == failpoint) throw new InjectedFailure();
            },
            runId));

        var finalPath = Path.Combine(temp.Path, "maneuvers", "succeeded", runId);
        Assert.Equal(
            ManeuverReportStatus.Succeeded,
            PairedReportReader.Read(finalPath).Report.Deterministic.Status);
    }

    [Fact]
    public void ReaderRejectsLinkedReportFileAndRunDirectory()
    {
        using var temp = new TemporaryDirectory();
        var fileArtifact = PairedReportWriter.Write(
            temp.Path,
            CreateReport(),
            static (_, _, _) => { },
            "file-link");
        var reportPath = Path.Combine(fileArtifact.Path, PairedReportReader.FileName);
        var outside = Path.Combine(temp.Path, "outside.json");
        File.Move(reportPath, outside);
        File.CreateSymbolicLink(reportPath, outside);

        var actualArtifact = PairedReportWriter.Write(
            temp.Path,
            CreateReport(),
            static (_, _, _) => { },
            "actual-run");
        var linkedDirectory = Path.Combine(
            temp.Path,
            "maneuvers",
            "succeeded",
            "linked-run");
        Directory.CreateSymbolicLink(linkedDirectory, actualArtifact.Path);

        Assert.Throws<InvalidDataException>(() => PairedReportReader.Read(fileArtifact.Path));
        Assert.Throws<InvalidDataException>(() => PairedReportReader.Read(linkedDirectory));
    }

    [Theory]
    [InlineData("failed")]
    [InlineData(".partial")]
    [InlineData("other")]
    public void ReaderRejectsWrongOrNonfinalPlacement(string parent)
    {
        using var temp = new TemporaryDirectory();
        var path = Path.Combine(temp.Path, "maneuvers", parent, "misplaced");
        Directory.CreateDirectory(path);
        File.WriteAllBytes(
            Path.Combine(path, PairedReportReader.FileName),
            PairedManeuverReportCodec.Serialize(CreateReport()));

        Assert.Throws<InvalidDataException>(() => PairedReportReader.Read(path));
    }

    private static PairedManeuverReport CreateReport()
    {
        var manifest = Manifest();
        ManeuverReportEntry[] entries =
        [
            new(0, "baseline", ManeuverVariant.Baseline, ManeuverEntryStatus.Succeeded,
                new BoundaryReached(Boundary), null, null, null, 1, 3, 0, HashA, HashA),
            new(1, "candidate", ManeuverVariant.Candidate, ManeuverEntryStatus.Succeeded,
                new BoundaryReached(Boundary), null, null, null, 1, 3, 0, HashA, HashA),
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
            [new PairedManeuverComparison(
                "pair", 0, 0, 1, PairedComparisonStatus.Compared,
                PairedManeuverPairingEvidence.HashCreationInputs(manifest, manifest.Pairs[0]),
                HashA, HashA, HashA,
                ExerciseConfigurationIdentity.ComputeHash(
                    manifest.Pairs[0].MaterializeBaseline(manifest.RootSeed)),
                ExerciseConfigurationIdentity.ComputeHash(
                    manifest.Pairs[0].MaterializeCandidate(manifest.RootSeed)),
                [new PairedAcceptedActionIdentity(0, CampaignActionAudience.Axis, HashA)],
                [new PairedAcceptedActionIdentity(0, CampaignActionAudience.Axis, HashA)],
                new PairedAcceptedActionDivergence(
                    PairedDivergenceKind.None, null, null, null, null, null),
                0, true, true)]);
        return new PairedManeuverReport(
            deterministic,
            new ManeuverReportDiagnostics(
                1,
                new ManeuverThroughput(2, 1),
                [
                    new ManeuverDiagnosticEntry(0, 1, "/tmp/baseline", HashA),
                    new ManeuverDiagnosticEntry(1, 1, "/tmp/candidate", HashA),
                ]));
    }

    private static PairedManeuverManifest Manifest() => new(
        1,
        PairedManeuverManifest.SchemeId,
        "paired.lifecycle",
        PairedManeuverMode.SerialPaired,
        1,
        new ManeuverReportOptions(ManeuverReportProfile.TrustedAuthority),
        [new PairedManeuverPairManifest(
            1,
            "pair",
            0,
            Exercise("baseline"),
            Exercise("candidate"))]);

    private static ManeuverExerciseManifest Exercise(string id) => new(
        2, id,
        "rules-lab.initiative.predetermined",
        "sha256:9e55e3de11338ba6432768ccb6740a6fed83b37503f69cc7ff8ecd58e205634f",
        "rules-lab.content.movement-contact.v1",
        "sha256:40f0e7a0a8876e4fefc4f06c1d752253cf338da614e587b9ff017e04541e7d79",
        "movement-contact-lab", Cna1979Ruleset.Manifest.Hash, Boundary, 16,
        ExerciseBuildMode.Exploratory, ExerciseConfidentiality.TrustedAuthority,
        ExerciseDetail.Forensic,
        new ExerciseControllerManifest(
            ExerciseControllerPolicy.FirstByActionId,
            ExerciseControllerPolicy.FirstByActionId,
            ExerciseControllerPolicy.FirstByActionId),
        null);

    private sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "sandtable-paired-report-tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        internal string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }

    private sealed class InjectedFailure : Exception;
}
