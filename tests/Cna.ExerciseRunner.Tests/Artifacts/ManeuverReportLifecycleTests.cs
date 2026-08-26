using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class ManeuverReportLifecycleTests : IDisposable
{
    private const string HashA =
        "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string HashB =
        "sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string Boundary = "land.position.operation-1.organization";
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        $"sandtable-maneuver-report-{Guid.NewGuid():N}");

    [Fact]
    public void WriterCreatesWritesFlushesMovesAndStrictlyReadsBackOneReport()
    {
        var expectedBytes = ManeuverReportCodec.Serialize(Report(ManeuverReportStatus.Succeeded));
        var observed = new List<ManeuverReportWriterFailpoint>();

        var artifact = ManeuverReportWriter.Write(
            root,
            Report(ManeuverReportStatus.Succeeded),
            (point, stagingPath, finalPath) =>
            {
                observed.Add(point);
                if (point == ManeuverReportWriterFailpoint.AfterReportCreate)
                {
                    Assert.True(File.Exists(Path.Combine(stagingPath, "maneuver-report.json")));
                    Assert.False(Directory.Exists(finalPath));
                }
                if (point == ManeuverReportWriterFailpoint.AfterReportFlush)
                {
                    Assert.Equal(
                        expectedBytes,
                        File.ReadAllBytes(Path.Combine(stagingPath, "maneuver-report.json")));
                }
            },
            "fixed-run");

        Assert.Equal(
            Enum.GetValues<ManeuverReportWriterFailpoint>(),
            observed);
        Assert.Equal("fixed-run", Path.GetFileName(artifact.Path));
        Assert.Equal("succeeded", Directory.GetParent(artifact.Path)!.Name);
        Assert.Equal(expectedBytes, artifact.CanonicalBytes);
        Assert.Equal(
            ["maneuver-report.json"],
            Directory.EnumerateFileSystemEntries(artifact.Path).Select(Path.GetFileName));
        Assert.False(Directory.Exists(Path.Combine(
            root,
            "maneuvers",
            ".partial",
            "fixed-run")));
    }

    [Theory]
    [InlineData(ManeuverReportStatus.Succeeded, "succeeded")]
    [InlineData(ManeuverReportStatus.ExerciseFailed, "failed")]
    [InlineData(ManeuverReportStatus.AggregationFailed, "failed")]
    [InlineData(ManeuverReportStatus.Cancelled, "failed")]
    public void WriterDerivesFinalPlacementFromDeterministicStatus(
        ManeuverReportStatus status,
        string expectedParent)
    {
        var artifact = ManeuverReportWriter.Write(root, Report(status));

        Assert.Equal(expectedParent, Directory.GetParent(artifact.Path)!.Name);
        Assert.Equal(status, artifact.Report.Deterministic.Status);
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
    public void PreMoveFailpointsRetainOnlyUntrustedPartialEvidence(
        ManeuverReportWriterFailpoint failpoint)
    {
        var runId = $"partial-{failpoint}";

        Assert.Throws<InjectedFailure>(() => ManeuverReportWriter.Write(
            root,
            Report(ManeuverReportStatus.Succeeded),
            (point, _, _) =>
            {
                if (point == failpoint) throw new InjectedFailure();
            },
            runId));

        var partial = Path.Combine(root, "maneuvers", ".partial", runId);
        Assert.True(Directory.Exists(partial));
        Assert.False(Directory.Exists(Path.Combine(root, "maneuvers", "succeeded", runId)));
        Assert.Throws<InvalidDataException>(() => ManeuverReportReader.Read(partial));
    }

    [Theory]
    [InlineData(ManeuverReportWriterFailpoint.AfterMove)]
    [InlineData(ManeuverReportWriterFailpoint.BeforeReadback)]
    [InlineData(ManeuverReportWriterFailpoint.AfterReadback)]
    public void PostMoveFailpointsRetainValidFinalEvidenceButReturnNoCompletedArtifact(
        ManeuverReportWriterFailpoint failpoint)
    {
        var runId = $"moved-{failpoint}";

        Assert.Throws<InjectedFailure>(() => ManeuverReportWriter.Write(
            root,
            Report(ManeuverReportStatus.Succeeded),
            (point, _, _) =>
            {
                if (point == failpoint) throw new InjectedFailure();
            },
            runId));

        var finalPath = Path.Combine(root, "maneuvers", "succeeded", runId);
        Assert.Equal(
            ManeuverReportStatus.Succeeded,
            ManeuverReportReader.Read(finalPath).Report.Deterministic.Status);
    }

    [Fact]
    public void CorruptFinalEvidenceIsRetainedButNeverReturnedAsCompleted()
    {
        const string runId = "corrupt-after-move";

        Assert.Throws<InvalidDataException>(() => ManeuverReportWriter.Write(
            root,
            Report(ManeuverReportStatus.Succeeded),
            (point, _, finalPath) =>
            {
                if (point == ManeuverReportWriterFailpoint.BeforeReadback)
                    File.AppendAllText(Path.Combine(finalPath, "maneuver-report.json"), " ");
            },
            runId));

        var finalPath = Path.Combine(root, "maneuvers", "succeeded", runId);
        Assert.True(File.Exists(Path.Combine(finalPath, "maneuver-report.json")));
        Assert.Throws<InvalidDataException>(() => ManeuverReportReader.Read(finalPath));
    }

    [Fact]
    public void WriterNeverOverwritesAnExistingPartialOrFinalDestination()
    {
        var partial = Path.Combine(root, "maneuvers", ".partial", "partial-exists");
        var final = Path.Combine(root, "maneuvers", "succeeded", "final-exists");
        Directory.CreateDirectory(partial);
        Directory.CreateDirectory(final);
        File.WriteAllText(Path.Combine(partial, "marker.txt"), "partial");
        File.WriteAllText(Path.Combine(final, "marker.txt"), "final");

        Assert.Throws<IOException>(() => ManeuverReportWriter.Write(
            root,
            Report(ManeuverReportStatus.Succeeded),
            (_, _, _) => { },
            "partial-exists"));
        Assert.Throws<IOException>(() => ManeuverReportWriter.Write(
            root,
            Report(ManeuverReportStatus.Succeeded),
            (_, _, _) => { },
            "final-exists"));
        Assert.Equal("partial", File.ReadAllText(Path.Combine(partial, "marker.txt")));
        Assert.Equal("final", File.ReadAllText(Path.Combine(final, "marker.txt")));
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../escape")]
    [InlineData("nested/run")]
    [InlineData("nested\\run")]
    [InlineData("/rooted")]
    public void WriterRejectsUnsafeInjectedRunDirectoryIds(string runId)
    {
        Assert.Throws<ArgumentException>(() => ManeuverReportWriter.Write(
            root,
            Report(ManeuverReportStatus.Succeeded),
            (_, _, _) => { },
            runId));
    }

    [Fact]
    public void WriterRejectsSymlinkedRootAndArtifactTreeDirectories()
    {
        var actual = Path.Combine(root, "actual");
        var linkedRoot = Path.Combine(root, "linked-root");
        Directory.CreateDirectory(actual);
        Directory.CreateSymbolicLink(linkedRoot, actual);

        Assert.Throws<InvalidDataException>(() =>
            ManeuverReportWriter.Write(linkedRoot, Report(ManeuverReportStatus.Succeeded)));

        var regularRoot = Path.Combine(root, "regular-root");
        Directory.CreateDirectory(regularRoot);
        Directory.CreateSymbolicLink(Path.Combine(regularRoot, "maneuvers"), actual);
        Assert.Throws<InvalidDataException>(() =>
            ManeuverReportWriter.Write(regularRoot, Report(ManeuverReportStatus.Succeeded)));

        var partialLinkedRoot = Path.Combine(root, "partial-linked-root");
        Directory.CreateDirectory(Path.Combine(partialLinkedRoot, "maneuvers"));
        Directory.CreateSymbolicLink(
            Path.Combine(partialLinkedRoot, "maneuvers", ".partial"),
            actual);
        Assert.Throws<InvalidDataException>(() => ManeuverReportWriter.Write(
            partialLinkedRoot,
            Report(ManeuverReportStatus.Succeeded)));

        var statusLinkedRoot = Path.Combine(root, "status-linked-root");
        Directory.CreateDirectory(Path.Combine(statusLinkedRoot, "maneuvers", ".partial"));
        Directory.CreateSymbolicLink(
            Path.Combine(statusLinkedRoot, "maneuvers", "succeeded"),
            actual);
        Assert.Throws<InvalidDataException>(() => ManeuverReportWriter.Write(
            statusLinkedRoot,
            Report(ManeuverReportStatus.Succeeded)));
        Assert.Empty(Directory.EnumerateFileSystemEntries(actual));
    }

    [Theory]
    [InlineData(ManeuverReportStatus.Succeeded, "failed")]
    [InlineData(ManeuverReportStatus.ExerciseFailed, "succeeded")]
    [InlineData(ManeuverReportStatus.Succeeded, ".partial")]
    [InlineData(ManeuverReportStatus.Succeeded, "other")]
    public void ReaderRejectsStatusMismatchAndNonfinalPlacement(
        ManeuverReportStatus status,
        string parent)
    {
        var path = CreateReportDirectory(parent, $"placed-{status}-{parent}", Report(status));

        Assert.Throws<InvalidDataException>(() => ManeuverReportReader.Read(path));
    }

    [Fact]
    public void ReaderRejectsMissingExtraNestedAndLinkedEntries()
    {
        var missing = CreateReportDirectory("succeeded", "missing", Report(
            ManeuverReportStatus.Succeeded));
        File.Delete(Path.Combine(missing, "maneuver-report.json"));
        var extra = CreateReportDirectory("succeeded", "extra", Report(
            ManeuverReportStatus.Succeeded));
        File.WriteAllText(Path.Combine(extra, "extra.json"), "{}");
        var nested = CreateReportDirectory("succeeded", "nested", Report(
            ManeuverReportStatus.Succeeded));
        Directory.CreateDirectory(Path.Combine(nested, "nested"));
        var linked = CreateReportDirectory("succeeded", "linked", Report(
            ManeuverReportStatus.Succeeded));
        var outside = Path.Combine(root, "outside.json");
        File.WriteAllText(outside, "{}");
        File.CreateSymbolicLink(Path.Combine(linked, "linked.json"), outside);

        Assert.Throws<InvalidDataException>(() => ManeuverReportReader.Read(missing));
        Assert.Throws<InvalidDataException>(() => ManeuverReportReader.Read(extra));
        Assert.Throws<InvalidDataException>(() => ManeuverReportReader.Read(nested));
        Assert.Throws<InvalidDataException>(() => ManeuverReportReader.Read(linked));
    }

    [Fact]
    public void ReaderRejectsLinkedReportFileAndRunDirectory()
    {
        var fileLinked = CreateReportDirectory("succeeded", "file-link", Report(
            ManeuverReportStatus.Succeeded));
        var reportPath = Path.Combine(fileLinked, "maneuver-report.json");
        var outside = Path.Combine(root, "outside-report.json");
        File.Move(reportPath, outside);
        File.CreateSymbolicLink(reportPath, outside);

        var actualDirectory = CreateReportDirectory("succeeded", "actual-run", Report(
            ManeuverReportStatus.Succeeded));
        var linkedDirectory = Path.Combine(root, "maneuvers", "succeeded", "linked-run");
        Directory.CreateSymbolicLink(linkedDirectory, actualDirectory);

        Assert.Throws<InvalidDataException>(() => ManeuverReportReader.Read(fileLinked));
        Assert.Throws<InvalidDataException>(() => ManeuverReportReader.Read(linkedDirectory));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        GC.SuppressFinalize(this);
    }

    private string CreateReportDirectory(
        string parent,
        string runId,
        ManeuverReport report)
    {
        var path = Path.Combine(root, "maneuvers", parent, runId);
        Directory.CreateDirectory(path);
        File.WriteAllBytes(
            Path.Combine(path, "maneuver-report.json"),
            ManeuverReportCodec.Serialize(report));
        return path;
    }

    private static ManeuverReport Report(ManeuverReportStatus status)
    {
        var manifest = Manifest();
        ManeuverReportEntry entry = status switch
        {
            ManeuverReportStatus.Succeeded => new(
                0, "organization-boundary.first", ManeuverVariant.Unpaired,
                ManeuverEntryStatus.Succeeded, new BoundaryReached(Boundary), null, null, null,
                1, 8, 0, HashA, HashB),
            ManeuverReportStatus.ExerciseFailed => new(
                0, "organization-boundary.first", ManeuverVariant.Unpaired,
                ManeuverEntryStatus.Failed, null, ExerciseFailureCategory.IllegalAction, null, null,
                0, 3, 1, HashA, HashB),
            ManeuverReportStatus.AggregationFailed => new(
                0, "organization-boundary.first", ManeuverVariant.Unpaired,
                ManeuverEntryStatus.AggregationFailed, null, null,
                ManeuverAggregationFailureCategory.BundleInvalid, null,
                null, null, null, null, null),
            ManeuverReportStatus.Cancelled => new(
                0, "organization-boundary.first", ManeuverVariant.Unpaired,
                ManeuverEntryStatus.Failed, null, ExerciseFailureCategory.Cancelled, null, null,
                0, 3, 1, HashA, HashB),
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };
        var succeeded = status == ManeuverReportStatus.Succeeded ? 1 : 0;
        var failed = status is ManeuverReportStatus.ExerciseFailed or ManeuverReportStatus.Cancelled
            ? 1
            : 0;
        var aggregationFailed = status == ManeuverReportStatus.AggregationFailed ? 1 : 0;
        var deterministic = new ManeuverReportDeterministic(
            manifest,
            status,
            new ManeuverReportCounts(
                1,
                1,
                succeeded + failed,
                succeeded,
                failed,
                aggregationFailed,
                0),
            succeeded == 1
                ? [new ManeuverTerminalCount(new BoundaryReached(Boundary), 1)]
                : [],
            Enum.GetValues<ExerciseFailureCategory>()
                .Select(category => new ManeuverFailureCount(
                    category,
                    category == entry.FailureCategory ? 1 : 0)),
            Enum.GetValues<ManeuverAggregationFailureCategory>()
                .Select(category => new ManeuverAggregationFailureCount(
                    category,
                    category == entry.AggregationFailureCategory ? 1 : 0)),
            [entry]);
        return new ManeuverReport(
            deterministic,
            new ManeuverReportDiagnostics(
                100,
                new ManeuverThroughput(succeeded + failed, 100),
                [new ManeuverDiagnosticEntry(0, 50, null, null)]));
    }

    private static ManeuverManifest Manifest() => new(
        ManeuverManifest.CurrentContractVersion,
        ManeuverManifest.SchemeId,
        "rules-lab.serial",
        ManeuverMode.SerialUnpaired,
        0,
        new ManeuverReportOptions(ManeuverReportProfile.TrustedAuthority),
        [new ManeuverExerciseManifest(
            ExerciseManifest.CurrentContractVersion,
            "organization-boundary.first",
            "rules-lab.initiative.predetermined",
            "sha256:0e03d12e8b4a5aeb7b19b7eed3f4ed2dcb9d3db2d253bdeaf8867d6b57a099a2",
            "rules-lab.content.movement-contact.v1",
            "sha256:38687a168bf96018f61826b42ae0df7e34466c7055a111861be46d0c924dcd0d",
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
            null)]);

    private sealed class InjectedFailure : Exception;
}
