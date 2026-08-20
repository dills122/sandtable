using System.Security.Cryptography;
using Cna.ExerciseRunner.Artifacts;
using Cna.ExerciseRunner.Execution;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class ExerciseBundleReaderTests : IDisposable
{
    private readonly string root = Path.Combine(
        Path.GetTempPath(),
        $"sandtable-exercise-reader-{Guid.NewGuid():N}");

    [Fact]
    public void ReaderAcceptsACompleteHashValidFailedPreAdmissionBundle()
    {
        var bundlePath = CreateFailedBundle("failed");

        var bundle = ExerciseBundleReader.Read(bundlePath);

        Assert.Equal(ArtifactBundleStatus.Failed, bundle.Manifest.Status);
        Assert.Equal(ArtifactBundleProfile.FailedPreAdmission, bundle.Manifest.Profile);
        Assert.IsType<ExerciseFailed>(bundle.RunResult.Completion);
        Assert.Empty(bundle.CheckResults.Results);
    }

    [Theory]
    [InlineData("succeeded")]
    [InlineData(".partial")]
    [InlineData("other")]
    public void ReaderRejectsStatusAndNonfinalLocationMismatch(string parent)
    {
        var bundlePath = CreateFailedBundle(parent);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderRejectsChangedBytesMissingListedAndExtraUnlistedFiles()
    {
        var changed = CreateFailedBundle("failed", "changed");
        File.AppendAllText(Path.Combine(changed, ArtifactSchema.RunResultPath), " ");
        var missing = CreateFailedBundle("failed", "missing");
        File.Delete(Path.Combine(missing, ArtifactSchema.CheckResultsPath));
        var extra = CreateFailedBundle("failed", "extra");
        File.WriteAllText(Path.Combine(extra, "extra.json"), "{}");

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(changed));
        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(missing));
        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(extra));
    }

    [Fact]
    public void ReaderRejectsASymlinkedPayloadWithoutFollowingIt()
    {
        var bundlePath = CreateFailedBundle("failed");
        var payloadPath = Path.Combine(bundlePath, ArtifactSchema.RunResultPath);
        var outsidePath = Path.Combine(root, "outside.json");
        File.Move(payloadPath, outsidePath);
        File.CreateSymbolicLink(payloadPath, outsidePath);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderRejectsCorruptArtifactManifestBeforeTrustingPayloads()
    {
        var bundlePath = CreateFailedBundle("failed");
        var manifestPath = Path.Combine(bundlePath, ArtifactSchema.ArtifactManifestPath);
        var bytes = File.ReadAllBytes(manifestPath);
        bytes[0] = (byte)'[';
        File.WriteAllBytes(manifestPath, bytes);

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderRejectsMalformedPayloadEvenWhenItsManifestHashMatches()
    {
        var bundlePath = CreateFailedBundle("failed");
        var malformedChecks = "{}"u8.ToArray();
        var runResult = File.ReadAllBytes(Path.Combine(bundlePath, ArtifactSchema.RunResultPath));
        File.WriteAllBytes(
            Path.Combine(bundlePath, ArtifactSchema.CheckResultsPath),
            malformedChecks);
        var manifest = new ArtifactManifest(
            ArtifactBundleProfile.FailedPreAdmission,
            [
                Entry(
                    ArtifactSchema.CheckResultsPath,
                    ArtifactSchema.CheckResultsSchemaId,
                    malformedChecks),
                Entry(ArtifactSchema.RunResultPath, ArtifactSchema.RunResultSchemaId, runResult),
            ]);
        File.WriteAllBytes(
            Path.Combine(bundlePath, ArtifactSchema.ArtifactManifestPath),
            ArtifactManifestCodec.Serialize(manifest));

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    [Fact]
    public void ReaderRejectsANonObjectJsonLineEvenWhenItsManifestHashMatches()
    {
        var bundlePath = CreateFailedBundle("failed");
        var diagnostics = "\"not-an-object\"\n"u8.ToArray();
        var checks = File.ReadAllBytes(Path.Combine(
            bundlePath,
            ArtifactSchema.CheckResultsPath));
        var runResult = File.ReadAllBytes(Path.Combine(
            bundlePath,
            ArtifactSchema.RunResultPath));
        File.WriteAllBytes(
            Path.Combine(bundlePath, ArtifactSchema.DiagnosticsPath),
            diagnostics);
        var manifest = new ArtifactManifest(
            ArtifactBundleProfile.FailedPreAdmission,
            [
                Entry(ArtifactSchema.CheckResultsPath, ArtifactSchema.CheckResultsSchemaId, checks),
                Entry(ArtifactSchema.DiagnosticsPath, ArtifactSchema.DiagnosticsSchemaId, diagnostics),
                Entry(ArtifactSchema.RunResultPath, ArtifactSchema.RunResultSchemaId, runResult),
            ]);
        File.WriteAllBytes(
            Path.Combine(bundlePath, ArtifactSchema.ArtifactManifestPath),
            ArtifactManifestCodec.Serialize(manifest));

        Assert.Throws<InvalidDataException>(() => ExerciseBundleReader.Read(bundlePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        GC.SuppressFinalize(this);
    }

    private string CreateFailedBundle(string parent, string runId = "run-1")
    {
        var bundlePath = Path.Combine(root, parent, runId);
        Directory.CreateDirectory(bundlePath);
        var runResult = ExerciseRunResultCodec.Serialize(ExerciseRunResult.Failed(
            ExerciseFailureCategory.ManifestInvalid,
            null));
        var checks = ExerciseCheckResultsCodec.Serialize(new ExerciseCheckResults([]));
        File.WriteAllBytes(Path.Combine(bundlePath, ArtifactSchema.RunResultPath), runResult);
        File.WriteAllBytes(Path.Combine(bundlePath, ArtifactSchema.CheckResultsPath), checks);
        var manifest = new ArtifactManifest(
            ArtifactBundleProfile.FailedPreAdmission,
            [
                Entry(ArtifactSchema.CheckResultsPath, ArtifactSchema.CheckResultsSchemaId, checks),
                Entry(ArtifactSchema.RunResultPath, ArtifactSchema.RunResultSchemaId, runResult),
            ]);
        File.WriteAllBytes(
            Path.Combine(bundlePath, ArtifactSchema.ArtifactManifestPath),
            ArtifactManifestCodec.Serialize(manifest));
        return bundlePath;
    }

    private static ArtifactManifestEntry Entry(
        string path,
        string schemaId,
        byte[] payload) => new(
            path,
            schemaId,
            payload.LongLength,
            $"sha256:{Convert.ToHexStringLower(SHA256.HashData(payload))}");
}
