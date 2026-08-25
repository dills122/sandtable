using System.Text.Json;

namespace Cna.ExerciseRunner.Artifacts;

public static class ManeuverManifestCodec
{
    private static readonly string[] PropertyNames =
    [
        "contractVersion", "schemeId", "maneuverId", "mode", "rootSeed", "report",
        "exercises",
    ];

    private static readonly string[] ExercisePropertyNames =
    [
        "contractVersion", "exerciseId", "setupId", "setupHash", "contentPackId",
        "contentHash", "scenarioId", "rulesetHash", "terminalBoundary", "maximumSteps",
        "buildMode", "confidentiality", "detail", "controllers", "assertFailureCategory",
    ];

    public static byte[] Serialize(ManeuverManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", manifest.ContractVersion);
            writer.WriteString("schemeId", manifest.ContractSchemeId);
            writer.WriteString("maneuverId", manifest.ManeuverId);
            writer.WriteString("mode", Format(manifest.Mode));
            writer.WriteNumber("rootSeed", manifest.RootSeed);
            writer.WriteStartObject("report");
            writer.WriteString("profile", Format(manifest.Report.Profile));
            writer.WriteEndObject();
            writer.WriteStartArray("exercises");
            foreach (var exercise in manifest.Exercises) WriteExercise(writer, exercise);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static ManeuverManifest Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            StrictJson.RequireExactProperties(root, PropertyNames);
            var report = root.GetProperty("report");
            StrictJson.RequireExactProperties(report, ["profile"]);
            var exercisesElement = root.GetProperty("exercises");
            if (exercisesElement.ValueKind != JsonValueKind.Array)
                throw new JsonException("Maneuver exercises must be an array.");
            var exercises = exercisesElement.EnumerateArray().Select(ReadExercise).ToArray();
            var manifest = new ManeuverManifest(
                root.GetProperty("contractVersion").GetInt32(),
                RequiredString(root, "schemeId"),
                RequiredString(root, "maneuverId"),
                ParseMode(root.GetProperty("mode").GetString()),
                root.GetProperty("rootSeed").GetUInt64(),
                new ManeuverReportOptions(ParseProfile(report.GetProperty("profile").GetString())),
                exercises);
            if (!Serialize(manifest).AsSpan().SequenceEqual(canonicalJson.Span))
                throw new JsonException("The Maneuver manifest is not canonically encoded.");
            return manifest;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or OverflowException
            or FormatException)
        {
            throw new JsonException("The Maneuver manifest is invalid.", exception);
        }
    }

    private static void WriteExercise(Utf8JsonWriter writer, ManeuverExerciseManifest exercise)
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", exercise.ContractVersion);
        writer.WriteString("exerciseId", exercise.ExerciseId);
        writer.WriteString("setupId", exercise.SetupId);
        writer.WriteString("setupHash", exercise.SetupHash);
        writer.WriteString("contentPackId", exercise.ContentPackId);
        writer.WriteString("contentHash", exercise.ContentHash);
        writer.WriteString("scenarioId", exercise.ScenarioId);
        writer.WriteString("rulesetHash", exercise.RulesetHash);
        writer.WriteString("terminalBoundary", exercise.TerminalBoundary);
        writer.WriteNumber("maximumSteps", exercise.MaximumSteps);
        writer.WriteString("buildMode", Format(exercise.BuildMode));
        writer.WriteString("confidentiality", Format(exercise.Confidentiality));
        writer.WriteString("detail", Format(exercise.Detail));
        writer.WriteStartObject("controllers");
        writer.WriteString("system", Format(exercise.Controllers.System));
        writer.WriteString("axis", Format(exercise.Controllers.Axis));
        writer.WriteString("commonwealth", Format(exercise.Controllers.Commonwealth));
        writer.WriteEndObject();
        if (exercise.AssertFailureCategory is null) writer.WriteNull("assertFailureCategory");
        else writer.WriteString(
            "assertFailureCategory",
            ExerciseContractText.FormatFailure(exercise.AssertFailureCategory.Value));
        writer.WriteEndObject();
    }

    private static ManeuverExerciseManifest ReadExercise(JsonElement element)
    {
        StrictJson.RequireExactProperties(element, ExercisePropertyNames);
        var controllers = element.GetProperty("controllers");
        StrictJson.RequireExactProperties(controllers, ["system", "axis", "commonwealth"]);
        var failure = element.GetProperty("assertFailureCategory");
        return new ManeuverExerciseManifest(
            element.GetProperty("contractVersion").GetInt32(),
            RequiredString(element, "exerciseId"),
            RequiredString(element, "setupId"),
            RequiredString(element, "setupHash"),
            RequiredString(element, "contentPackId"),
            RequiredString(element, "contentHash"),
            RequiredString(element, "scenarioId"),
            RequiredString(element, "rulesetHash"),
            RequiredString(element, "terminalBoundary"),
            element.GetProperty("maximumSteps").GetInt32(),
            ParseBuildMode(element.GetProperty("buildMode").GetString()),
            ParseConfidentiality(element.GetProperty("confidentiality").GetString()),
            ParseDetail(element.GetProperty("detail").GetString()),
            new ExerciseControllerManifest(
                ParseController(controllers.GetProperty("system").GetString()),
                ParseController(controllers.GetProperty("axis").GetString()),
                ParseController(controllers.GetProperty("commonwealth").GetString())),
            failure.ValueKind == JsonValueKind.Null
                ? null
                : ExerciseContractText.ParseFailure(failure.GetString()));
    }

    private static string RequiredString(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).GetString()
            ?? throw new JsonException($"{propertyName} must be a string.");

    private static string Format(ManeuverMode value) => value switch
    {
        ManeuverMode.SerialUnpaired => "serial-unpaired",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static ManeuverMode ParseMode(string? value) => value switch
    {
        "serial-unpaired" => ManeuverMode.SerialUnpaired,
        _ => throw new JsonException("Unknown Maneuver mode."),
    };

    private static string Format(ManeuverReportProfile value) => value switch
    {
        ManeuverReportProfile.TrustedAuthority => "trusted-authority",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static ManeuverReportProfile ParseProfile(string? value) => value switch
    {
        "trusted-authority" => ManeuverReportProfile.TrustedAuthority,
        _ => throw new JsonException("Unknown Maneuver report profile."),
    };

    private static string Format(ExerciseBuildMode value) => value switch
    {
        ExerciseBuildMode.Baseline => "baseline",
        ExerciseBuildMode.Exploratory => "exploratory",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static ExerciseBuildMode ParseBuildMode(string? value) => value switch
    {
        "baseline" => ExerciseBuildMode.Baseline,
        "exploratory" => ExerciseBuildMode.Exploratory,
        _ => throw new JsonException("Unknown build mode."),
    };

    private static string Format(ExerciseConfidentiality value) => value switch
    {
        ExerciseConfidentiality.TrustedAuthority => "trusted-authority",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static ExerciseConfidentiality ParseConfidentiality(string? value) => value switch
    {
        "trusted-authority" => ExerciseConfidentiality.TrustedAuthority,
        _ => throw new JsonException("Unknown confidentiality."),
    };

    private static string Format(ExerciseDetail value) => value switch
    {
        ExerciseDetail.Compact => "compact",
        ExerciseDetail.Forensic => "forensic",
        ExerciseDetail.Debug => "debug",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static ExerciseDetail ParseDetail(string? value) => value switch
    {
        "compact" => ExerciseDetail.Compact,
        "forensic" => ExerciseDetail.Forensic,
        "debug" => ExerciseDetail.Debug,
        _ => throw new JsonException("Unknown detail."),
    };

    private static string Format(ExerciseControllerPolicy value) => value switch
    {
        ExerciseControllerPolicy.FirstByActionId => "first-by-action-id",
        ExerciseControllerPolicy.DesignateAllReservesThenFirstByActionId =>
            "designate-all-reserves-then-first-by-action-id",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static ExerciseControllerPolicy ParseController(string? value) => value switch
    {
        "first-by-action-id" => ExerciseControllerPolicy.FirstByActionId,
        "designate-all-reserves-then-first-by-action-id" =>
            ExerciseControllerPolicy.DesignateAllReservesThenFirstByActionId,
        _ => throw new JsonException("Unknown controller policy."),
    };
}
