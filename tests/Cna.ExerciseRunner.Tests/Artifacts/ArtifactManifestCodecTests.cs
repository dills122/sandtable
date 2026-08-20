using System.Text;
using System.Text.Json;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class ArtifactManifestCodecTests
{
    [Fact]
    public void FailedPreAdmissionManifestHasExactCanonicalVersionOneBytes()
    {
        var manifest = new ArtifactManifest(
            ArtifactBundleProfile.FailedPreAdmission,
            [
                new ArtifactManifestEntry(
                    ArtifactSchema.CheckResultsPath,
                    ArtifactSchema.CheckResultsSchemaId,
                    12,
                    Hash('a')),
                new ArtifactManifestEntry(
                    ArtifactSchema.RunResultPath,
                    ArtifactSchema.RunResultSchemaId,
                    34,
                    Hash('b')),
            ]);

        var bytes = ArtifactManifestCodec.Serialize(manifest);

        Assert.Equal(
            $"{{\"contractVersion\":1,\"schemeId\":\"sandtable.exercise-artifacts.v1\",\"profile\":\"failed-pre-admission\",\"status\":\"failed\",\"confidentiality\":\"trusted-authority\",\"files\":[{{\"path\":\"check-results.json\",\"schemaId\":\"sandtable.exercise-checks.v1\",\"sizeBytes\":12,\"sha256\":\"{Hash('a')}\"}},{{\"path\":\"run-result.json\",\"schemaId\":\"sandtable.exercise-result.v1\",\"sizeBytes\":34,\"sha256\":\"{Hash('b')}\"}}]}}",
            Encoding.UTF8.GetString(bytes));
        Assert.Equal(
            bytes,
            ArtifactManifestCodec.Serialize(ArtifactManifestCodec.Deserialize(bytes)));
    }

    [Fact]
    public void ReaderRejectsUnknownReorderedExtraDuplicateAndInvalidProfileValues()
    {
        var json = Encoding.UTF8.GetString(ArtifactManifestCodec.Serialize(new ArtifactManifest(
            ArtifactBundleProfile.FailedPreAdmission,
            [
                new ArtifactManifestEntry(
                    ArtifactSchema.CheckResultsPath,
                    ArtifactSchema.CheckResultsSchemaId,
                    0,
                    Hash('a')),
                new ArtifactManifestEntry(
                    ArtifactSchema.RunResultPath,
                    ArtifactSchema.RunResultSchemaId,
                    0,
                    Hash('b')),
            ])));
        string[] invalid =
        [
            json.Replace("{\"contractVersion\":1,", "{\"extra\":true,\"contractVersion\":1,", StringComparison.Ordinal),
            json.Replace("\"contractVersion\":1,\"schemeId\"", "\"schemeId\":\"duplicate\",\"contractVersion\":1,\"schemeId\"", StringComparison.Ordinal),
            json.Replace("\"contractVersion\":1,\"schemeId\":\"sandtable.exercise-artifacts.v1\"", "\"schemeId\":\"sandtable.exercise-artifacts.v1\",\"contractVersion\":1", StringComparison.Ordinal),
            json.Replace("\"profile\":\"failed-pre-admission\"", "\"profile\":\"unknown\"", StringComparison.Ordinal),
            json.Replace("\"status\":\"failed\"", "\"status\":\"succeeded\"", StringComparison.Ordinal),
            json.Replace("\"schemaId\":\"sandtable.exercise-checks.v1\"", "\"schemaId\":\"unknown\"", StringComparison.Ordinal),
            json.Replace("\"contractVersion\":1", "\"contractVersion\":99", StringComparison.Ordinal),
        ];

        Assert.All(invalid, value => Assert.Throws<JsonException>(() =>
            ArtifactManifestCodec.Deserialize(Encoding.UTF8.GetBytes(value))));
    }

    [Fact]
    public void ManifestRejectsDuplicateAndProfileInconsistentPayloadPaths()
    {
        var checks = new ArtifactManifestEntry(
            ArtifactSchema.CheckResultsPath,
            ArtifactSchema.CheckResultsSchemaId,
            0,
            Hash('a'));
        var result = new ArtifactManifestEntry(
            ArtifactSchema.RunResultPath,
            ArtifactSchema.RunResultSchemaId,
            0,
            Hash('b'));
        var admitted = new ArtifactManifestEntry(
            ArtifactSchema.ExerciseManifestPath,
            ArtifactSchema.ExerciseManifestSchemaId,
            0,
            Hash('c'));

        Assert.Throws<ArgumentException>(() => new ArtifactManifest(
            ArtifactBundleProfile.FailedPreAdmission,
            [checks, checks, result]));
        Assert.Throws<ArgumentException>(() => new ArtifactManifest(
            ArtifactBundleProfile.FailedPreAdmission,
            [checks, admitted, result]));
    }

    [Theory]
    [InlineData("../outside.json")]
    [InlineData("/absolute.json")]
    [InlineData("sub\\windows.json")]
    [InlineData("./run-result.json")]
    [InlineData("sub/../run-result.json")]
    [InlineData("RUN-RESULT.JSON")]
    [InlineData("unknown.json")]
    public void ArtifactSchemaRejectsNoncanonicalOrUnknownPaths(string path)
    {
        Assert.Throws<ArgumentException>(() => ArtifactSchema.RequireKnownPath(path));
    }

    private static string Hash(char value) => $"sha256:{new string(value, 64)}";
}
