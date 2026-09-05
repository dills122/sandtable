using System.Globalization;
using System.Text;
using System.Text.Json;
using Cna.Core.Rules;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Tests.Artifacts;

public sealed class PairedManeuverManifestCodecTests
{
    [Fact]
    public void ManifestHasExactCanonicalBytesAndMaterializesEqualPairInputs()
    {
        var manifest = Create();

        var bytes = PairedManeuverManifestCodec.Serialize(manifest);
        var admitted = PairedManeuverManifestCodec.Deserialize(bytes);

        Assert.Equal(bytes, PairedManeuverManifestCodec.Serialize(admitted));
        Assert.StartsWith(
            "{\"contractVersion\":1,\"schemeId\":\"sandtable.paired-maneuver-manifest.v1\",\"maneuverId\":\"rules-lab.paired\",\"mode\":\"serial-paired\",\"rootSeed\":1844,\"report\":{\"profile\":\"trusted-authority\"},\"pairs\":[{\"contractVersion\":1,\"pairKey\":\"reserve-policy\",\"repetition\":0,\"baseline\":",
            Encoding.UTF8.GetString(bytes),
            StringComparison.Ordinal);
        var pair = Assert.Single(admitted.Pairs);
        var baseline = pair.MaterializeBaseline(admitted.RootSeed);
        var candidate = pair.MaterializeCandidate(admitted.RootSeed);
        Assert.Equal(baseline.SetupId, candidate.SetupId);
        Assert.Equal(baseline.SetupHash, candidate.SetupHash);
        Assert.Equal(baseline.ContentPackId, candidate.ContentPackId);
        Assert.Equal(baseline.ContentHash, candidate.ContentHash);
        Assert.Equal(baseline.ScenarioId, candidate.ScenarioId);
        Assert.Equal(baseline.RulesetHash, candidate.RulesetHash);
        Assert.Equal(baseline.RootSeed, candidate.RootSeed);
        Assert.NotEqual(baseline.Controllers.Axis, candidate.Controllers.Axis);
    }

    [Fact]
    public void CanonicalBytesAreCultureIndependent()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var expected = PairedManeuverManifestCodec.Serialize(Create());
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("ar-SA");

            Assert.Equal(expected, PairedManeuverManifestCodec.Serialize(Create()));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void AdmissionRejectsUnequalDeclaredCreationInputsAndDuplicatePairIdentity()
    {
        var baseline = Exercise("baseline", ExerciseControllerPolicy.FirstByActionId);
        var unequal = Exercise(
            "candidate",
            ExerciseControllerPolicy.DesignateAllReservesThenFirstByActionId,
            scenarioId: "different-scenario");

        Assert.Throws<ArgumentException>(() => Pair(baseline, unequal));
        Assert.Throws<ArgumentException>(() => new PairedManeuverManifest(
            PairedManeuverManifest.CurrentContractVersion,
            PairedManeuverManifest.SchemeId,
            "rules-lab.paired",
            PairedManeuverMode.SerialPaired,
            1844,
            new ManeuverReportOptions(ManeuverReportProfile.TrustedAuthority),
            [Pair(baseline, Exercise("candidate", ExerciseControllerPolicy.FirstByActionId)),
             Pair(Exercise("baseline.2", ExerciseControllerPolicy.FirstByActionId),
                  Exercise("candidate.2", ExerciseControllerPolicy.FirstByActionId))]));
    }

    [Fact]
    public void ReaderRejectsMalformedAmbiguousAndNoncanonicalPairedArtifacts()
    {
        var canonical = Encoding.UTF8.GetString(
            PairedManeuverManifestCodec.Serialize(Create()));
        string[] invalid =
        [
            canonical + "\n",
            canonical.Replace("{\"contractVersion\":1,", "{\"extra\":true,\"contractVersion\":1,", StringComparison.Ordinal),
            canonical.Replace("\"serial-paired\"", "\"serial-unpaired\"", StringComparison.Ordinal),
            canonical.Replace("\"pairKey\":\"reserve-policy\"", "\"pairKey\":\"Invalid Pair\"", StringComparison.Ordinal),
            canonical.Replace("\"repetition\":0", "\"repetition\":-1", StringComparison.Ordinal),
            canonical.Replace("\"baseline\":{", "\"candidate\":{},\"baseline\":{", StringComparison.Ordinal),
            canonical.Replace("\"buildMode\":", "\"rootSeed\":1,\"buildMode\":", StringComparison.Ordinal),
        ];

        Assert.All(invalid, value => Assert.ThrowsAny<JsonException>(() =>
            PairedManeuverManifestCodec.Deserialize(Encoding.UTF8.GetBytes(value))));
        Assert.ThrowsAny<JsonException>(() => ManeuverManifestCodec.Deserialize(
            Encoding.UTF8.GetBytes(canonical)));
    }

    private static PairedManeuverManifest Create() => new(
        PairedManeuverManifest.CurrentContractVersion,
        PairedManeuverManifest.SchemeId,
        "rules-lab.paired",
        PairedManeuverMode.SerialPaired,
        1844,
        new ManeuverReportOptions(ManeuverReportProfile.TrustedAuthority),
        [Pair(
            Exercise("reserve-policy.baseline", ExerciseControllerPolicy.FirstByActionId),
            Exercise(
                "reserve-policy.candidate",
                ExerciseControllerPolicy.DesignateAllReservesThenFirstByActionId))]);

    private static PairedManeuverPairManifest Pair(
        ManeuverExerciseManifest baseline,
        ManeuverExerciseManifest candidate) => new(
        PairedManeuverPairManifest.CurrentContractVersion,
        "reserve-policy",
        0,
        baseline,
        candidate);

    private static ManeuverExerciseManifest Exercise(
        string exerciseId,
        ExerciseControllerPolicy controller,
        string scenarioId = "movement-contact-lab") => new(
        ExerciseManifest.CurrentContractVersion,
        exerciseId,
        "rules-lab.initiative.predetermined",
        "sha256:48ad98fd232f7c7c50d4f925dd83e3de97f2eb48cc6929a17aa1fb172cdbd394",
        "rules-lab.content.movement-contact.v1",
        "sha256:20cf54f25d752253105877c6139d8db86549759f9dbb80fad873686498f26f5f",
        scenarioId,
        Cna1979Ruleset.Manifest.Hash,
        "land.position.operation-1.first-player.movement-and-combat.movement",
        16,
        ExerciseBuildMode.Exploratory,
        ExerciseConfidentiality.TrustedAuthority,
        ExerciseDetail.Forensic,
        new ExerciseControllerManifest(
            controller,
            controller,
            controller),
        null);
}
