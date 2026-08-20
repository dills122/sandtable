using System.Text.Json;

namespace Cna.ExerciseRunner.Artifacts;

public static class ArtifactManifestCodec
{
    private static readonly string[] EntryProperties =
    [
        "path", "schemaId", "sizeBytes", "sha256",
    ];

    public static byte[] Serialize(ArtifactManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", manifest.ContractVersion);
            writer.WriteString("schemeId", manifest.ContractSchemeId);
            writer.WriteString("profile", FormatProfile(manifest.Profile));
            writer.WriteString("status", FormatStatus(manifest.Status));
            writer.WriteString("confidentiality", "trusted-authority");
            writer.WriteStartArray("files");
            foreach (var file in manifest.Files)
            {
                writer.WriteStartObject();
                writer.WriteString("path", file.Path);
                writer.WriteString("schemaId", file.SchemaId);
                writer.WriteNumber("sizeBytes", file.SizeBytes);
                writer.WriteString("sha256", file.Sha256);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static ArtifactManifest Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            StrictJson.RequireExactProperties(
                root,
                ["contractVersion", "schemeId", "profile", "status", "confidentiality", "files"]);
            if (root.GetProperty("contractVersion").GetInt32()
                != ArtifactManifest.CurrentContractVersion)
                throw new JsonException("Unknown artifact-manifest contract version.");
            if (!string.Equals(
                    root.GetProperty("schemeId").GetString(),
                    ArtifactManifest.SchemeId,
                    StringComparison.Ordinal))
                throw new JsonException("Unknown artifact-manifest scheme.");
            if (!string.Equals(
                    root.GetProperty("confidentiality").GetString(),
                    "trusted-authority",
                    StringComparison.Ordinal))
                throw new JsonException("Unknown artifact confidentiality.");
            var profile = ParseProfile(root.GetProperty("profile").GetString());
            var filesElement = root.GetProperty("files");
            if (filesElement.ValueKind != JsonValueKind.Array)
                throw new JsonException("Artifact files must be an array.");
            var files = filesElement.EnumerateArray().Select(ReadEntry).ToArray();
            var manifest = new ArtifactManifest(profile, files);
            if (!string.Equals(
                    root.GetProperty("status").GetString(),
                    FormatStatus(manifest.Status),
                    StringComparison.Ordinal))
                throw new JsonException("Artifact profile and status are contradictory.");
            if (!Serialize(manifest).AsSpan().SequenceEqual(canonicalJson.Span))
                throw new JsonException("The artifact manifest is not canonically encoded.");
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
            throw new JsonException("The artifact manifest is invalid.", exception);
        }
    }

    private static ArtifactManifestEntry ReadEntry(JsonElement element)
    {
        StrictJson.RequireExactProperties(element, EntryProperties);
        return new ArtifactManifestEntry(
            element.GetProperty("path").GetString()
                ?? throw new JsonException("Artifact path must be a string."),
            element.GetProperty("schemaId").GetString()
                ?? throw new JsonException("Artifact schema must be a string."),
            element.GetProperty("sizeBytes").GetInt64(),
            element.GetProperty("sha256").GetString()
                ?? throw new JsonException("Artifact hash must be a string."));
    }

    private static string FormatProfile(ArtifactBundleProfile value) => value switch
    {
        ArtifactBundleProfile.Succeeded => "succeeded",
        ArtifactBundleProfile.FailedPreAdmission => "failed-pre-admission",
        ArtifactBundleProfile.FailedAdmitted => "failed-admitted",
        ArtifactBundleProfile.FailedIdentified => "failed-identified",
        ArtifactBundleProfile.FailedExecuted => "failed-executed",
        ArtifactBundleProfile.FailedReconstructed => "failed-reconstructed",
        ArtifactBundleProfile.FailedReadjudicated => "failed-readjudicated",
        ArtifactBundleProfile.FailedSummarized => "failed-summarized",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static ArtifactBundleProfile ParseProfile(string? value)
    {
        foreach (var candidate in Enum.GetValues<ArtifactBundleProfile>())
        {
            if (string.Equals(FormatProfile(candidate), value, StringComparison.Ordinal))
                return candidate;
        }
        throw new JsonException("Unknown artifact profile.");
    }

    private static string FormatStatus(ArtifactBundleStatus value) => value switch
    {
        ArtifactBundleStatus.Succeeded => "succeeded",
        ArtifactBundleStatus.Failed => "failed",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };
}
