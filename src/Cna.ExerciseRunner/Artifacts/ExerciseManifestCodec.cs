using System.Text.Json;

namespace Cna.ExerciseRunner.Artifacts;

public static class ExerciseManifestCodec
{
    private static readonly string[] PropertyNames =
    [
        "contractVersion", "exerciseId", "setupId", "setupHash", "contentPackId",
        "contentHash", "scenarioId", "rulesetHash", "terminalBoundary", "maximumSteps",
        "rootSeed", "buildMode", "confidentiality", "detail", "controllers",
        "assertFailureCategory",
    ];

    public static byte[] Serialize(ExerciseManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", manifest.ContractVersion);
            writer.WriteString("exerciseId", manifest.ExerciseId);
            writer.WriteString("setupId", manifest.SetupId);
            writer.WriteString("setupHash", manifest.SetupHash);
            writer.WriteString("contentPackId", manifest.ContentPackId);
            writer.WriteString("contentHash", manifest.ContentHash);
            writer.WriteString("scenarioId", manifest.ScenarioId);
            writer.WriteString("rulesetHash", manifest.RulesetHash);
            writer.WriteString("terminalBoundary", manifest.TerminalBoundary);
            writer.WriteNumber("maximumSteps", manifest.MaximumSteps);
            writer.WriteNumber("rootSeed", manifest.RootSeed);
            writer.WriteString("buildMode", Format(manifest.BuildMode));
            writer.WriteString("confidentiality", Format(manifest.Confidentiality));
            writer.WriteString("detail", Format(manifest.Detail));
            writer.WriteStartObject("controllers");
            writer.WriteString("system", Format(manifest.Controllers.System));
            writer.WriteString("axis", Format(manifest.Controllers.Axis));
            writer.WriteString("commonwealth", Format(manifest.Controllers.Commonwealth));
            writer.WriteEndObject();
            if (manifest.AssertFailureCategory is null) writer.WriteNull("assertFailureCategory");
            else writer.WriteString(
                "assertFailureCategory",
                Format(manifest.AssertFailureCategory.Value));
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static ExerciseManifest Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            StrictJson.RequireExactProperties(root, PropertyNames);
            var controllers = root.GetProperty("controllers");
            StrictJson.RequireExactProperties(controllers, ["system", "axis", "commonwealth"]);
            var failure = root.GetProperty("assertFailureCategory");
            return new ExerciseManifest(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("exerciseId").GetString()!,
                root.GetProperty("setupId").GetString()!,
                root.GetProperty("setupHash").GetString()!,
                root.GetProperty("contentPackId").GetString()!,
                root.GetProperty("contentHash").GetString()!,
                root.GetProperty("scenarioId").GetString()!,
                root.GetProperty("rulesetHash").GetString()!,
                root.GetProperty("terminalBoundary").GetString()!,
                root.GetProperty("maximumSteps").GetInt32(),
                root.GetProperty("rootSeed").GetUInt64(),
                ParseBuildMode(root.GetProperty("buildMode").GetString()),
                ParseConfidentiality(root.GetProperty("confidentiality").GetString()),
                ParseDetail(root.GetProperty("detail").GetString()),
                new ExerciseControllerManifest(
                    ParseController(controllers.GetProperty("system").GetString()),
                    ParseController(controllers.GetProperty("axis").GetString()),
                    ParseController(controllers.GetProperty("commonwealth").GetString())),
                failure.ValueKind == JsonValueKind.Null
                    ? null
                    : ExerciseContractText.ParseFailure(failure.GetString()));
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
            throw new JsonException("The Exercise manifest is invalid.", exception);
        }
    }

    private static string Format(ExerciseBuildMode value) => value switch
    {
        ExerciseBuildMode.Baseline => "baseline",
        ExerciseBuildMode.Exploratory => "exploratory",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static string Format(ExerciseConfidentiality value) => value switch
    {
        ExerciseConfidentiality.TrustedAuthority => "trusted-authority",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static string Format(ExerciseDetail value) => value switch
    {
        ExerciseDetail.Compact => "compact",
        ExerciseDetail.Forensic => "forensic",
        ExerciseDetail.Debug => "debug",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static string Format(ExerciseControllerPolicy value) => value switch
    {
        ExerciseControllerPolicy.FirstByActionId => "first-by-action-id",
        ExerciseControllerPolicy.DesignateAllReservesThenFirstByActionId =>
            "designate-all-reserves-then-first-by-action-id",
        ExerciseControllerPolicy.ActFirstReserveNoneThenFirstByActionId =>
            "act-first-reserve-none-then-first-by-action-id",
        ExerciseControllerPolicy.ActFirstReserveOneThenFirstByActionId =>
            "act-first-reserve-one-then-first-by-action-id",
        ExerciseControllerPolicy.ActFirstReserveAllThenFirstByActionId =>
            "act-first-reserve-all-then-first-by-action-id",
        ExerciseControllerPolicy.ActLastReserveNoneThenFirstByActionId =>
            "act-last-reserve-none-then-first-by-action-id",
        ExerciseControllerPolicy.ActLastReserveOneThenFirstByActionId =>
            "act-last-reserve-one-then-first-by-action-id",
        ExerciseControllerPolicy.ActLastReserveAllThenFirstByActionId =>
            "act-last-reserve-all-then-first-by-action-id",
        ExerciseControllerPolicy.ActFirstReserveNoneMoveEachOnceThenComplete =>
            "act-first-reserve-none-move-each-once-then-complete",
        ExerciseControllerPolicy.ActFirstReserveOneMoveEachOnceThenComplete =>
            "act-first-reserve-one-move-each-once-then-complete",
        ExerciseControllerPolicy.ActFirstReserveAllMoveEachOnceThenComplete =>
            "act-first-reserve-all-move-each-once-then-complete",
        ExerciseControllerPolicy.ActLastReserveNoneMoveEachOnceThenComplete =>
            "act-last-reserve-none-move-each-once-then-complete",
        ExerciseControllerPolicy.ActLastReserveOneMoveEachOnceThenComplete =>
            "act-last-reserve-one-move-each-once-then-complete",
        ExerciseControllerPolicy.ActLastReserveAllMoveEachOnceThenComplete =>
            "act-last-reserve-all-move-each-once-then-complete",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static string Format(ExerciseFailureCategory value) =>
        ExerciseContractText.FormatFailure(value);

    private static ExerciseBuildMode ParseBuildMode(string? value) => value switch
    {
        "baseline" => ExerciseBuildMode.Baseline,
        "exploratory" => ExerciseBuildMode.Exploratory,
        _ => throw new JsonException("Unknown build mode."),
    };

    private static ExerciseConfidentiality ParseConfidentiality(string? value) => value switch
    {
        "trusted-authority" => ExerciseConfidentiality.TrustedAuthority,
        _ => throw new JsonException("Unknown confidentiality."),
    };

    private static ExerciseDetail ParseDetail(string? value) => value switch
    {
        "compact" => ExerciseDetail.Compact,
        "forensic" => ExerciseDetail.Forensic,
        "debug" => ExerciseDetail.Debug,
        _ => throw new JsonException("Unknown detail."),
    };

    private static ExerciseControllerPolicy ParseController(string? value) => value switch
    {
        "first-by-action-id" => ExerciseControllerPolicy.FirstByActionId,
        "designate-all-reserves-then-first-by-action-id" =>
            ExerciseControllerPolicy.DesignateAllReservesThenFirstByActionId,
        "act-first-reserve-none-then-first-by-action-id" =>
            ExerciseControllerPolicy.ActFirstReserveNoneThenFirstByActionId,
        "act-first-reserve-one-then-first-by-action-id" =>
            ExerciseControllerPolicy.ActFirstReserveOneThenFirstByActionId,
        "act-first-reserve-all-then-first-by-action-id" =>
            ExerciseControllerPolicy.ActFirstReserveAllThenFirstByActionId,
        "act-last-reserve-none-then-first-by-action-id" =>
            ExerciseControllerPolicy.ActLastReserveNoneThenFirstByActionId,
        "act-last-reserve-one-then-first-by-action-id" =>
            ExerciseControllerPolicy.ActLastReserveOneThenFirstByActionId,
        "act-last-reserve-all-then-first-by-action-id" =>
            ExerciseControllerPolicy.ActLastReserveAllThenFirstByActionId,
        "act-first-reserve-none-move-each-once-then-complete" =>
            ExerciseControllerPolicy.ActFirstReserveNoneMoveEachOnceThenComplete,
        "act-first-reserve-one-move-each-once-then-complete" =>
            ExerciseControllerPolicy.ActFirstReserveOneMoveEachOnceThenComplete,
        "act-first-reserve-all-move-each-once-then-complete" =>
            ExerciseControllerPolicy.ActFirstReserveAllMoveEachOnceThenComplete,
        "act-last-reserve-none-move-each-once-then-complete" =>
            ExerciseControllerPolicy.ActLastReserveNoneMoveEachOnceThenComplete,
        "act-last-reserve-one-move-each-once-then-complete" =>
            ExerciseControllerPolicy.ActLastReserveOneMoveEachOnceThenComplete,
        "act-last-reserve-all-move-each-once-then-complete" =>
            ExerciseControllerPolicy.ActLastReserveAllMoveEachOnceThenComplete,
        _ => throw new JsonException("Unknown controller policy."),
    };

}
