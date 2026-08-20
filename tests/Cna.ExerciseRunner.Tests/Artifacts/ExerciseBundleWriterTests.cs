using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class ExerciseBundleWriterTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        $"sandtable-exercise-writer-{Guid.NewGuid():N}");

    [Fact]
    public void WriterFlushesRunResultThenManifestLastMovesAndReadsBack()
    {
        var observed = new List<ArtifactWriterFailpoint>();

        var bundle = ExerciseBundleWriter.Write(
            root,
            Request(),
            (point, stagingPath, _) =>
            {
                observed.Add(point);
                if (point == ArtifactWriterFailpoint.AfterRunResultFlush)
                {
                    Assert.True(File.Exists(Path.Combine(
                        stagingPath,
                        ArtifactSchema.RunResultPath)));
                    Assert.False(File.Exists(Path.Combine(
                        stagingPath,
                        ArtifactSchema.ArtifactManifestPath)));
                }
                if (point == ArtifactWriterFailpoint.AfterManifestFlush)
                {
                    Assert.True(File.Exists(Path.Combine(
                        stagingPath,
                        ArtifactSchema.ArtifactManifestPath)));
                }
            },
            "fixed-run");

        Assert.Equal("fixed-run", Path.GetFileName(bundle.Path));
        Assert.Equal("failed", Directory.GetParent(bundle.Path)!.Name);
        Assert.Equal(ArtifactBundleStatus.Failed, bundle.Manifest.Status);
        Assert.Contains(ArtifactWriterFailpoint.AfterReadback, observed);
        Assert.False(Directory.Exists(Path.Combine(root, ".partial", "fixed-run")));
    }

    [Theory]
    [InlineData(ArtifactWriterFailpoint.StagingCreated)]
    [InlineData(ArtifactWriterFailpoint.BeforePayloadFlush)]
    [InlineData(ArtifactWriterFailpoint.AfterPayloadFlush)]
    [InlineData(ArtifactWriterFailpoint.BeforeRunResultFlush)]
    [InlineData(ArtifactWriterFailpoint.AfterRunResultFlush)]
    [InlineData(ArtifactWriterFailpoint.BeforeManifestFlush)]
    [InlineData(ArtifactWriterFailpoint.AfterManifestFlush)]
    [InlineData(ArtifactWriterFailpoint.BeforeMove)]
    public void PreMoveFailpointsLeaveOnlyAnUntrustedPartialBundle(
        ArtifactWriterFailpoint failpoint)
    {
        var runId = $"partial-{failpoint}";

        Assert.Throws<InjectedFailure>(() => ExerciseBundleWriter.Write(
            root,
            Request(),
            (point, _, _) =>
            {
                if (point == failpoint) throw new InjectedFailure();
            },
            runId));

        Assert.False(Directory.Exists(Path.Combine(root, "failed", runId)));
        var partial = Path.Combine(root, ".partial", runId);
        Assert.True(Directory.Exists(partial));
        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(partial));
    }

    [Fact]
    public void FailureAfterMoveLeavesAReaderValidFinalBundle()
    {
        const string runId = "moved-run";

        Assert.Throws<InjectedFailure>(() => ExerciseBundleWriter.Write(
            root,
            Request(),
            (point, _, _) =>
            {
                if (point == ArtifactWriterFailpoint.AfterMove) throw new InjectedFailure();
            },
            runId));

        var finalPath = Path.Combine(root, "failed", runId);
        Assert.Equal(
            ArtifactBundleStatus.Failed,
            ExerciseBundleReader.Read(finalPath).Manifest.Status);
    }

    [Fact]
    public void WriterNeverOverwritesAnExistingDestination()
    {
        var destination = Path.Combine(root, "failed", "existing-run");
        Directory.CreateDirectory(destination);
        var marker = Path.Combine(destination, "marker.txt");
        File.WriteAllText(marker, "keep");

        Assert.Throws<IOException>(() => ExerciseBundleWriter.Write(
            root,
            Request(),
            (_, _, _) => { },
            "existing-run"));

        Assert.Equal("keep", File.ReadAllText(marker));
    }

    [Fact]
    public void WriterRejectsASymlinkedArtifactRoot()
    {
        var actual = Path.Combine(root, "actual");
        var linked = Path.Combine(root, "linked");
        Directory.CreateDirectory(actual);
        Directory.CreateSymbolicLink(linked, actual);

        Assert.Throws<InvalidDataException>(() =>
            ExerciseBundleWriter.Write(linked, Request()));
        Assert.Empty(Directory.EnumerateFileSystemEntries(actual));
    }

    [Fact]
    public void RecoverablePrimaryFaultCanFinalizeAReaderValidFailedFallback()
    {
        var primaryPayloads = Request().PayloadCopy();
        primaryPayloads[ArtifactSchema.ExerciseManifestPath] =
            ExerciseManifestCodec.Serialize(ExerciseManifestCodecTests.Create());
        var primary = new ExerciseBundleWriteRequest(
            ArtifactBundleProfile.FailedAdmitted,
            primaryPayloads);
        var fallback = Request(ExerciseFailureCategory.ArtifactFailed);

        var outcome = ExerciseBundleWriter.TryWrite(
            root,
            primary,
            fallback,
            (point, _, _) =>
            {
                if (point == ArtifactWriterFailpoint.BeforeManifestFlush)
                    throw new IOException("injected artifact fault");
            },
            "primary-run");

        Assert.False(outcome.IsPrimarySucceeded);
        Assert.NotNull(outcome.Failure);
        Assert.Contains("IOException", outcome.Failure.ExceptionType, StringComparison.Ordinal);
        Assert.NotNull(outcome.CompletedBundle);
        Assert.Equal(
            ArtifactBundleStatus.Failed,
            outcome.CompletedBundle.Manifest.Status);
        Assert.Equal("primary-run-failed", Path.GetFileName(outcome.CompletedBundle.Path));
        Assert.True(Directory.Exists(Path.Combine(root, ".partial", "primary-run")));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        GC.SuppressFinalize(this);
    }

    private static ExerciseBundleWriteRequest Request(
        ExerciseFailureCategory category = ExerciseFailureCategory.ManifestInvalid)
    {
        var runResult = ExerciseRunResultCodec.Serialize(ExerciseRunResult.Failed(
            category,
            null));
        var checks = ExerciseCheckResultsCodec.Serialize(new ExerciseCheckResults([]));
        return new ExerciseBundleWriteRequest(
            ArtifactBundleProfile.FailedPreAdmission,
            new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                [ArtifactSchema.RunResultPath] = runResult,
                [ArtifactSchema.CheckResultsPath] = checks,
            });
    }

    private sealed class InjectedFailure : Exception;
}
