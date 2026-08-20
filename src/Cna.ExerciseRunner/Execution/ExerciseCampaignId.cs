using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.ExerciseRunner.Execution;

public static class ExerciseCampaignId
{
    public const int CurrentContractVersion = 1;
    public const string SchemeId = "sandtable.exercise-campaign-id.v1";

    public static string Derive(ExerciseRunIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", CurrentContractVersion);
            writer.WriteString("schemeId", SchemeId);
            writer.WriteString("maneuverId", identity.ManeuverId);
            writer.WriteNumber("exerciseOrdinal", identity.ExerciseOrdinal);
            if (identity.PairKey is null) writer.WriteNull("pairKey");
            else writer.WriteString("pairKey", identity.PairKey);
            writer.WriteEndObject();
        }
        return $"exercise-{Convert.ToHexStringLower(SHA256.HashData(stream.ToArray()))}";
    }
}
