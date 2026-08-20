using System.Text;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class ExerciseManifestCodecTests
{
    [Fact]
    public void ManifestHasAnExactStrictCanonicalVersionOneShape()
    {
        var manifest = Create();

        var bytes = ExerciseManifestCodec.Serialize(manifest);
        var json = Encoding.UTF8.GetString(bytes);

        Assert.Equal(
            $"{{\"contractVersion\":1,\"exerciseId\":\"organization-boundary\",\"setupId\":\"{manifest.SetupId}\",\"setupHash\":\"{manifest.SetupHash}\",\"contentPackId\":\"{manifest.ContentPackId}\",\"contentHash\":\"{manifest.ContentHash}\",\"scenarioId\":\"{manifest.ScenarioId}\",\"rulesetHash\":\"{manifest.RulesetHash}\",\"terminalBoundary\":\"land.position.operation-1.organization\",\"maximumSteps\":8,\"rootSeed\":0,\"buildMode\":\"exploratory\",\"confidentiality\":\"trusted-authority\",\"detail\":\"compact\",\"controllers\":{{\"system\":\"first-by-action-id\",\"axis\":\"first-by-action-id\",\"commonwealth\":\"first-by-action-id\"}},\"assertFailureCategory\":null}}",
            json);
        Assert.Equal(manifest, ExerciseManifestCodec.Deserialize(bytes));
    }

    [Fact]
    public void ReaderRejectsExtraMissingReorderedDuplicateAndUnknownValues()
    {
        var json = Encoding.UTF8.GetString(ExerciseManifestCodec.Serialize(Create()));
        string[] invalid =
        [
            json.Replace("{\"contractVersion\":1,", "{\"extra\":true,\"contractVersion\":1,", StringComparison.Ordinal),
            json.Replace("\"maximumSteps\":8,", "", StringComparison.Ordinal),
            json.Replace("{\"contractVersion\":1,\"exerciseId\":", "{\"exerciseId\":\"wrong\",\"contractVersion\":1,\"exerciseId\":", StringComparison.Ordinal),
            json.Replace("\"contractVersion\":1,\"exerciseId\":", "\"exerciseId\":\"wrong\",\"contractVersion\":1,", StringComparison.Ordinal),
            json.Replace("\"detail\":\"compact\"", "\"detail\":\"verbose\"", StringComparison.Ordinal),
        ];

        Assert.All(invalid, value => Assert.ThrowsAny<Exception>(() =>
            ExerciseManifestCodec.Deserialize(Encoding.UTF8.GetBytes(value))));
    }

    [Theory]
    [InlineData("exerciseId", "Invalid ID")]
    [InlineData("setupId", "invalid..setup")]
    [InlineData("setupHash", "sha256:not-a-hash")]
    [InlineData("contentPackId", "invalid_content")]
    [InlineData("contentHash", "SHA256:53d5b64f647251e3ac366c65f4ad05cae766afd7b70ee331d463e801496e2a99")]
    [InlineData("scenarioId", "invalid scenario")]
    [InlineData("rulesetHash", "0000000000000000000000000000000000000000000000000000000000000000")]
    [InlineData("terminalBoundary", "land.position.invalid..boundary")]
    public void ReaderRejectsSemanticallyInvalidIdentitiesBeforeExecution(
        string property,
        string replacement)
    {
        var json = Encoding.UTF8.GetString(ExerciseManifestCodec.Serialize(Create()));
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var original = document.RootElement.GetProperty(property).GetString()!;
        var invalid = json.Replace(
            $"\"{property}\":\"{original}\"",
            $"\"{property}\":\"{replacement}\"",
            StringComparison.Ordinal);

        Assert.Throws<System.Text.Json.JsonException>(() =>
            ExerciseManifestCodec.Deserialize(Encoding.UTF8.GetBytes(invalid)));
    }

    internal static ExerciseManifest Create(
        int maximumSteps = 8,
        ExerciseFailureCategory? assertFailureCategory = null,
        string terminalBoundary = "land.position.operation-1.organization",
        ExerciseDetail detail = ExerciseDetail.Compact,
        ExerciseBuildMode buildMode = ExerciseBuildMode.Exploratory)
    {
        return new ExerciseManifest(
            ExerciseManifest.CurrentContractVersion,
            "organization-boundary",
            "rules-lab.initiative.predetermined",
            "sha256:5ecf84d21a7ff95112b9b662915f6858926532d30be5a0eee3f1a45752fdc80a",
            "rules-lab.content.movement-contact.v1",
            "sha256:53d5b64f647251e3ac366c65f4ad05cae766afd7b70ee331d463e801496e2a99",
            "movement-contact-lab",
            Cna1979Ruleset.Manifest.Hash,
            terminalBoundary,
            maximumSteps,
            0,
            buildMode,
            ExerciseConfidentiality.TrustedAuthority,
            detail,
            new ExerciseControllerManifest(
                ExerciseControllerPolicy.FirstByActionId,
                ExerciseControllerPolicy.FirstByActionId,
                ExerciseControllerPolicy.FirstByActionId),
            assertFailureCategory);
    }
}
