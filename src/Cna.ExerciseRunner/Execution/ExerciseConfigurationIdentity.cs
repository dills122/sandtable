using System.Security.Cryptography;
using System.Text.Json;
using Cna.ExerciseRunner.Artifacts;

namespace Cna.ExerciseRunner.Execution;

internal static class ExerciseConfigurationIdentity
{
    internal const int CurrentContractVersion = 1;
    internal const string SchemeId = "sandtable.exercise-controller-configuration.v1";

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
        _ => throw new ArgumentOutOfRangeException(nameof(policy)),
    };
}
