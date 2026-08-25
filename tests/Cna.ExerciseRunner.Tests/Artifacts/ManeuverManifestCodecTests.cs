using System.Globalization;
using System.Text;
using System.Text.Json;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class ManeuverManifestCodecTests
{
    private const string CanonicalManifest =
        "{\"contractVersion\":2,\"schemeId\":\"sandtable.maneuver-manifest.v2\",\"maneuverId\":\"rules-lab.serial\",\"mode\":\"serial-unpaired\",\"rootSeed\":0,\"report\":{\"profile\":\"trusted-authority\"},\"exercises\":[{\"contractVersion\":2,\"exerciseId\":\"organization-boundary.first\",\"setupId\":\"rules-lab.initiative.predetermined\",\"setupHash\":\"sha256:c1688f8869ca66182b87f487ec34edbef617ff1158f7d8b0d3101fe3993978ef\",\"contentPackId\":\"rules-lab.content.movement-contact.v1\",\"contentHash\":\"sha256:53d5b64f647251e3ac366c65f4ad05cae766afd7b70ee331d463e801496e2a99\",\"scenarioId\":\"movement-contact-lab\",\"rulesetHash\":\"beb66b242222f1ccc8bde4a34daacfcd561495b47e3d48391ede34e16830d6e6\",\"terminalBoundary\":\"land.position.operation-1.organization\",\"maximumSteps\":8,\"buildMode\":\"exploratory\",\"confidentiality\":\"trusted-authority\",\"detail\":\"forensic\",\"controllers\":{\"system\":\"first-by-action-id\",\"axis\":\"first-by-action-id\",\"commonwealth\":\"first-by-action-id\"},\"assertFailureCategory\":null}]}";

    [Fact]
    public void ManifestHasTheFrozenCanonicalVersionTwoBytes()
    {
        var bytes = ManeuverManifestCodec.Serialize(Create());

        Assert.Equal(CanonicalManifest, Encoding.UTF8.GetString(bytes));

        var admitted = ManeuverManifestCodec.Deserialize(bytes);
        Assert.Equal(bytes, ManeuverManifestCodec.Serialize(admitted));
        Assert.Equal("rules-lab.serial", admitted.ManeuverId);
        Assert.Equal(ManeuverMode.SerialUnpaired, admitted.Mode);
        Assert.Equal(ManeuverReportProfile.TrustedAuthority, admitted.Report.Profile);
        Assert.Single(admitted.Exercises);
    }

    [Fact]
    public void SemanticReservePolicyRoundTripsAndMaterializesExactExerciseV2()
    {
        var manifest = Create(ExerciseControllerPolicy
            .DesignateAllReservesThenFirstByActionId);

        var admitted = ManeuverManifestCodec.Deserialize(
            ManeuverManifestCodec.Serialize(manifest));
        var materialized = admitted.MaterializeExercise(0);

        Assert.Equal(ExerciseManifest.CurrentContractVersion,
            materialized.ContractVersion);
        Assert.Equal(
            ExerciseControllerPolicy.DesignateAllReservesThenFirstByActionId,
            materialized.Controllers.Axis);
        Assert.Contains(
            "designate-all-reserves-then-first-by-action-id",
            Encoding.UTF8.GetString(ManeuverManifestCodec.Serialize(admitted)),
            StringComparison.Ordinal);
    }

    [Fact]
    public void CanonicalBytesAreIndependentOfCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            foreach (var cultureName in new[] { "fr-FR", "tr-TR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

                Assert.Equal(
                    CanonicalManifest,
                    Encoding.UTF8.GetString(ManeuverManifestCodec.Serialize(Create())));
            }
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void ReaderRejectsNoncanonicalAndUnknownParentShapes()
    {
        string[] invalid =
        [
            CanonicalManifest.Replace("{\"contractVersion\":2,", "{\"extra\":true,\"contractVersion\":2,", StringComparison.Ordinal),
            CanonicalManifest.Replace("\"rootSeed\":0,", "", StringComparison.Ordinal),
            CanonicalManifest.Replace("{\"contractVersion\":2,\"schemeId\":", "{\"schemeId\":\"wrong\",\"contractVersion\":2,\"schemeId\":", StringComparison.Ordinal),
            CanonicalManifest.Replace("\"contractVersion\":2,\"schemeId\":", "\"schemeId\":\"wrong\",\"contractVersion\":2,", StringComparison.Ordinal),
            CanonicalManifest.Replace("\"contractVersion\":2,\"schemeId\":", "\"contractVersion\":1,\"schemeId\":", StringComparison.Ordinal),
            CanonicalManifest.Replace("sandtable.maneuver-manifest.v2", "sandtable.maneuver-manifest.v1", StringComparison.Ordinal),
            CanonicalManifest.Replace("serial-unpaired", "paired", StringComparison.Ordinal),
            CanonicalManifest.Replace("trusted-authority", "public", StringComparison.Ordinal),
            CanonicalManifest.Replace("rules-lab.serial", "standalone.rules-lab", StringComparison.Ordinal),
            CanonicalManifest.Replace("rules-lab.serial", "Rules Lab", StringComparison.Ordinal),
            CanonicalManifest + "\n",
        ];

        Assert.All(invalid, value => Assert.ThrowsAny<JsonException>(() =>
            ManeuverManifestCodec.Deserialize(Encoding.UTF8.GetBytes(value))));
    }

    [Fact]
    public void ReaderRejectsExtraMissingReorderedDuplicateAndUnknownChildShapes()
    {
        string[] invalid =
        [
            CanonicalManifest.Replace(
                "\"exercises\":[{\"contractVersion\":2,",
                "\"exercises\":[{\"extra\":true,\"contractVersion\":2,",
                StringComparison.Ordinal),
            CanonicalManifest.Replace("\"maximumSteps\":8,", "", StringComparison.Ordinal),
            CanonicalManifest.Replace(
                "{\"contractVersion\":2,\"exerciseId\":\"organization-boundary.first\",",
                "{\"exerciseId\":\"organization-boundary.first\",\"contractVersion\":2,",
                StringComparison.Ordinal),
            CanonicalManifest.Replace(
                "{\"contractVersion\":2,\"exerciseId\":",
                "{\"exerciseId\":\"wrong\",\"contractVersion\":2,\"exerciseId\":",
                StringComparison.Ordinal),
            CanonicalManifest.Replace(
                "\"exercises\":[{\"contractVersion\":2,",
                "\"exercises\":[{\"contractVersion\":1,",
                StringComparison.Ordinal),
            CanonicalManifest.Replace(
                CanonicalManifest[(CanonicalManifest.IndexOf("[{", StringComparison.Ordinal) + 1)..^2],
                "null",
                StringComparison.Ordinal),
        ];

        Assert.All(invalid, value => Assert.Throws<JsonException>(() =>
            ManeuverManifestCodec.Deserialize(Encoding.UTF8.GetBytes(value))));
    }

    [Fact]
    public void ReaderRejectsEmptyOrDuplicateExerciseEntries()
    {
        var duplicate = CanonicalManifest.Replace(
            "]}",
            $",{CanonicalManifest[(CanonicalManifest.IndexOf("[{", StringComparison.Ordinal) + 1)..^2]}]}}",
            StringComparison.Ordinal);
        string[] invalid =
        [
            CanonicalManifest.Replace(
                CanonicalManifest[(CanonicalManifest.IndexOf("[{", StringComparison.Ordinal) + 1)..^2],
                "",
                StringComparison.Ordinal),
            duplicate,
        ];

        Assert.All(invalid, value => Assert.Throws<JsonException>(() =>
            ManeuverManifestCodec.Deserialize(Encoding.UTF8.GetBytes(value))));
    }

    [Theory]
    [InlineData("rootSeed", "0")]
    [InlineData("campaignId", "\"forbidden\"")]
    [InlineData("pairKey", "\"forbidden\"")]
    [InlineData("variant", "\"axis\"")]
    [InlineData("repetitions", "2")]
    public void ReaderRejectsNestedRunOrPairIdentityFields(string property, string value)
    {
        var invalid = CanonicalManifest.Replace(
            "\"buildMode\":",
            $"\"{property}\":{value},\"buildMode\":",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() =>
            ManeuverManifestCodec.Deserialize(Encoding.UTF8.GetBytes(invalid)));
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
    public void ReaderRejectsEveryInvalidStandaloneIdentityShape(
        string property,
        string replacement)
    {
        using var document = JsonDocument.Parse(CanonicalManifest);
        var original = document.RootElement.GetProperty("exercises")[0]
            .GetProperty(property).GetString()!;
        var invalid = CanonicalManifest.Replace(
            $"\"{property}\":\"{original}\"",
            $"\"{property}\":\"{replacement}\"",
            StringComparison.Ordinal);

        Assert.Throws<JsonException>(() =>
            ManeuverManifestCodec.Deserialize(Encoding.UTF8.GetBytes(invalid)));
    }

    [Fact]
    public void ReaderRejectsInvalidStandaloneValuesAndChildShapes()
    {
        string[] invalid =
        [
            CanonicalManifest.Replace("\"maximumSteps\":8", "\"maximumSteps\":0", StringComparison.Ordinal),
            CanonicalManifest.Replace("\"buildMode\":\"exploratory\"", "\"buildMode\":\"unknown\"", StringComparison.Ordinal),
            CanonicalManifest.Replace("\"confidentiality\":\"trusted-authority\"", "\"confidentiality\":\"public\"", StringComparison.Ordinal),
            CanonicalManifest.Replace("\"detail\":\"forensic\"", "\"detail\":\"verbose\"", StringComparison.Ordinal),
            CanonicalManifest.Replace("\"system\":\"first-by-action-id\"", "\"system\":\"random\"", StringComparison.Ordinal),
            CanonicalManifest.Replace("\"assertFailureCategory\":null", "\"assertFailureCategory\":\"unknown\"", StringComparison.Ordinal),
            CanonicalManifest.Replace("\"controllers\":{", "\"controllers\":{\"extra\":true,", StringComparison.Ordinal),
        ];

        Assert.All(invalid, value => Assert.Throws<JsonException>(() =>
            ManeuverManifestCodec.Deserialize(Encoding.UTF8.GetBytes(value))));
    }

    [Fact]
    public void AdmissionMaterializesOrderedChildrenWithOnlyTheParentRootSeed()
    {
        var manifest = new ManeuverManifest(
            ManeuverManifest.CurrentContractVersion,
            ManeuverManifest.SchemeId,
            "rules-lab.serial",
            ManeuverMode.SerialUnpaired,
            1844,
            new ManeuverReportOptions(ManeuverReportProfile.TrustedAuthority),
            [CreateExercise("organization-boundary.first"), CreateExercise("organization-boundary.second")]);

        var admitted = ManeuverManifestCodec.Deserialize(ManeuverManifestCodec.Serialize(manifest));
        var exercises = admitted.MaterializeExercises();

        Assert.Collection(
            exercises,
            first =>
            {
                Assert.Equal("organization-boundary.first", first.ExerciseId);
                Assert.Equal(1844UL, first.RootSeed);
            },
            second =>
            {
                Assert.Equal("organization-boundary.second", second.ExerciseId);
                Assert.Equal(1844UL, second.RootSeed);
            });
    }

    [Fact]
    public void ReaderRejectsTheEntireManifestWhenALaterChildIsInvalid()
    {
        var first = CanonicalManifest[(CanonicalManifest.IndexOf("[{", StringComparison.Ordinal) + 1)..^2];
        var second = first
            .Replace("organization-boundary.first", "organization-boundary.second", StringComparison.Ordinal)
            .Replace("\"maximumSteps\":8", "\"maximumSteps\":0", StringComparison.Ordinal);
        var invalid = CanonicalManifest.Replace(first, $"{first},{second}", StringComparison.Ordinal);

        Assert.Throws<JsonException>(() =>
            ManeuverManifestCodec.Deserialize(Encoding.UTF8.GetBytes(invalid)));
    }

    private static ManeuverManifest Create(
        ExerciseControllerPolicy controllerPolicy =
            ExerciseControllerPolicy.FirstByActionId) => new(
        ManeuverManifest.CurrentContractVersion,
        ManeuverManifest.SchemeId,
        "rules-lab.serial",
        ManeuverMode.SerialUnpaired,
        0,
        new ManeuverReportOptions(ManeuverReportProfile.TrustedAuthority),
        [CreateExercise("organization-boundary.first", controllerPolicy)]);

    private static ManeuverExerciseManifest CreateExercise(
        string exerciseId,
        ExerciseControllerPolicy controllerPolicy =
            ExerciseControllerPolicy.FirstByActionId) => new(
        ExerciseManifest.CurrentContractVersion,
        exerciseId,
        "rules-lab.initiative.predetermined",
        "sha256:c1688f8869ca66182b87f487ec34edbef617ff1158f7d8b0d3101fe3993978ef",
        "rules-lab.content.movement-contact.v1",
        "sha256:53d5b64f647251e3ac366c65f4ad05cae766afd7b70ee331d463e801496e2a99",
        "movement-contact-lab",
        Cna1979Ruleset.Manifest.Hash,
        "land.position.operation-1.organization",
        8,
        ExerciseBuildMode.Exploratory,
        ExerciseConfidentiality.TrustedAuthority,
        ExerciseDetail.Forensic,
        new ExerciseControllerManifest(
            controllerPolicy,
            controllerPolicy,
            controllerPolicy),
        null);
}
