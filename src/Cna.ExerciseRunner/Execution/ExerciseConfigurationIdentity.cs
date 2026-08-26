using System.Security.Cryptography;
using System.Text.Json;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Execution;

internal static class ExerciseConfigurationIdentity
{
    internal const int CurrentContractVersion = 2;
    internal const string SchemeId = "sandtable.exercise-controller-configuration.v2";

    internal static string ComputeHash(ExerciseManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("schemeId", SchemeId);
            writer.WriteString("system", Format(manifest.Controllers.System));
            writer.WriteString("axis", Format(manifest.Controllers.Axis));
            writer.WriteString("commonwealth", Format(manifest.Controllers.Commonwealth));
            writer.WriteEndObject();
        }
        return $"sha256:{Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()))}";
    }

    private static string Format(ExerciseControllerPolicy policy) => policy switch
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
        _ => throw new ArgumentOutOfRangeException(nameof(policy)),
    };
}
