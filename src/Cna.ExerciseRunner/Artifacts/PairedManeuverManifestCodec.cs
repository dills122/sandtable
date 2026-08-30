using System.Text.Json;

namespace Cna.ExerciseRunner.Artifacts;

public static class PairedManeuverManifestCodec
{
    private static readonly string[] PropertyNames =
    [
        "contractVersion", "schemeId", "maneuverId", "mode", "rootSeed", "report", "pairs",
    ];

    private static readonly string[] PairPropertyNames =
    [
        "contractVersion", "pairKey", "repetition", "baseline", "candidate",
    ];

    public static byte[] Serialize(PairedManeuverManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", manifest.ContractVersion);
            writer.WriteString("schemeId", manifest.ContractSchemeId);
            writer.WriteString("maneuverId", manifest.ManeuverId);
            writer.WriteString("mode", "serial-paired");
            writer.WriteNumber("rootSeed", manifest.RootSeed);
            writer.WriteStartObject("report");
            writer.WriteString("profile", "trusted-authority");
            writer.WriteEndObject();
            writer.WriteStartArray("pairs");
            foreach (var pair in manifest.Pairs)
            {
                writer.WriteStartObject();
                writer.WriteNumber("contractVersion", pair.ContractVersion);
                writer.WriteString("pairKey", pair.PairKey);
                writer.WriteNumber("repetition", pair.Repetition);
                writer.WritePropertyName("baseline");
                ManeuverManifestCodec.WriteExercise(writer, pair.Baseline);
                writer.WritePropertyName("candidate");
                ManeuverManifestCodec.WriteExercise(writer, pair.Candidate);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static PairedManeuverManifest Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            StrictJson.RequireExactProperties(root, PropertyNames);
            var report = root.GetProperty("report");
            StrictJson.RequireExactProperties(report, ["profile"]);
            if (!string.Equals(
                    report.GetProperty("profile").GetString(),
                    "trusted-authority",
                    StringComparison.Ordinal))
                throw new JsonException("Unknown paired Maneuver report profile.");
            var pairsElement = root.GetProperty("pairs");
            if (pairsElement.ValueKind != JsonValueKind.Array)
                throw new JsonException("Paired Maneuver pairs must be an array.");

            var manifest = new PairedManeuverManifest(
                root.GetProperty("contractVersion").GetInt32(),
                RequiredString(root, "schemeId"),
                RequiredString(root, "maneuverId"),
                ParseMode(root.GetProperty("mode").GetString()),
                root.GetProperty("rootSeed").GetUInt64(),
                new ManeuverReportOptions(ManeuverReportProfile.TrustedAuthority),
                pairsElement.EnumerateArray().Select(ReadPair));
            if (!Serialize(manifest).AsSpan().SequenceEqual(canonicalJson.Span))
                throw new JsonException(
                    "The paired Maneuver manifest is not canonically encoded.");
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
            throw new JsonException("The paired Maneuver manifest is invalid.", exception);
        }
    }

    internal static bool HasPairedScheme(ReadOnlyMemory<byte> json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("schemeId", out var scheme)
                && scheme.ValueKind == JsonValueKind.String
                && string.Equals(scheme.GetString(), PairedManeuverManifest.SchemeId,
                    StringComparison.Ordinal);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static PairedManeuverPairManifest ReadPair(JsonElement element)
    {
        StrictJson.RequireExactProperties(element, PairPropertyNames);
        return new PairedManeuverPairManifest(
            element.GetProperty("contractVersion").GetInt32(),
            RequiredString(element, "pairKey"),
            element.GetProperty("repetition").GetInt32(),
            ManeuverManifestCodec.ReadExercise(element.GetProperty("baseline")),
            ManeuverManifestCodec.ReadExercise(element.GetProperty("candidate")));
    }

    private static PairedManeuverMode ParseMode(string? value) => value switch
    {
        "serial-paired" => PairedManeuverMode.SerialPaired,
        _ => throw new JsonException("Unknown paired Maneuver mode."),
    };

    private static string RequiredString(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).GetString()
            ?? throw new JsonException($"{propertyName} must be a string.");
}
