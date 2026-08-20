using System.Text.Json;

namespace Cna.ExerciseRunner.Artifacts;

public static class BuildIdentityCodec
{
    private static readonly string[] ArtifactProperties =
    [
        "name", "sizeBytes", "sha256",
    ];

    public static byte[] Serialize(BuildIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", identity.ContractVersion);
            writer.WriteString("schemeId", identity.ContractSchemeId);
            writer.WriteString("buildMode", FormatMode(identity.BuildMode));
            writer.WriteString("headCommit", identity.HeadCommit);
            writer.WriteString("headTree", identity.HeadTree);
            writer.WriteBoolean("dirty", identity.Dirty);
            writer.WriteString("porcelainSha256", identity.PorcelainSha256);
            writer.WriteString("frameworkDescription", identity.FrameworkDescription);
            writer.WriteString("osArchitecture", identity.OsArchitecture);
            writer.WriteString("processArchitecture", identity.ProcessArchitecture);
            writer.WriteString("rulesetHash", identity.RulesetHash);
            writer.WriteString("configurationHash", identity.ConfigurationHash);
            writer.WriteString("manifestHash", identity.ManifestHash);
            writer.WriteString("seedSchemeId", identity.SeedSchemeId);
            writer.WriteBoolean("baselineEligible", identity.BaselineEligible);
            writer.WriteBoolean("reproducible", identity.Reproducible);
            writer.WriteStartArray("artifacts");
            foreach (var artifact in identity.Artifacts)
            {
                writer.WriteStartObject();
                writer.WriteString("name", artifact.Name);
                writer.WriteNumber("sizeBytes", artifact.SizeBytes);
                writer.WriteString("sha256", artifact.Sha256);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    public static BuildIdentity Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            StrictJson.RequireExactProperties(
                root,
                [
                    "contractVersion", "schemeId", "buildMode", "headCommit", "headTree", "dirty",
                    "porcelainSha256", "frameworkDescription", "osArchitecture",
                    "processArchitecture", "rulesetHash", "configurationHash", "manifestHash",
                    "seedSchemeId", "baselineEligible", "reproducible", "artifacts",
                ]);
            if (root.GetProperty("contractVersion").GetInt32()
                != BuildIdentity.CurrentContractVersion)
                throw new JsonException("Unknown build-identity contract version.");
            if (!string.Equals(
                    root.GetProperty("schemeId").GetString(),
                    BuildIdentity.SchemeId,
                    StringComparison.Ordinal))
                throw new JsonException("Unknown build-identity scheme.");
            var artifactsElement = root.GetProperty("artifacts");
            if (artifactsElement.ValueKind != JsonValueKind.Array)
                throw new JsonException("Build artifacts must be an array.");
            var artifacts = artifactsElement.EnumerateArray().Select(element =>
            {
                StrictJson.RequireExactProperties(element, ArtifactProperties);
                return new BuildArtifactIdentity(
                    RequireString(element, "name"),
                    element.GetProperty("sizeBytes").GetInt64(),
                    RequireString(element, "sha256"));
            });
            var identity = new BuildIdentity(
                ParseMode(root.GetProperty("buildMode").GetString()),
                RequireString(root, "headCommit"),
                RequireString(root, "headTree"),
                root.GetProperty("dirty").GetBoolean(),
                RequireString(root, "porcelainSha256"),
                RequireString(root, "frameworkDescription"),
                RequireString(root, "osArchitecture"),
                RequireString(root, "processArchitecture"),
                RequireString(root, "rulesetHash"),
                RequireString(root, "configurationHash"),
                RequireString(root, "manifestHash"),
                RequireString(root, "seedSchemeId"),
                root.GetProperty("baselineEligible").GetBoolean(),
                root.GetProperty("reproducible").GetBoolean(),
                artifacts);
            if (!Serialize(identity).AsSpan().SequenceEqual(canonicalJson.Span))
                throw new JsonException("The build identity is not canonically encoded.");
            return identity;
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
            throw new JsonException("The build identity is invalid.", exception);
        }
    }

    private static string FormatMode(ExerciseBuildMode value) => value switch
    {
        ExerciseBuildMode.Baseline => "baseline",
        ExerciseBuildMode.Exploratory => "exploratory",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static ExerciseBuildMode ParseMode(string? value) => value switch
    {
        "baseline" => ExerciseBuildMode.Baseline,
        "exploratory" => ExerciseBuildMode.Exploratory,
        _ => throw new JsonException("Unknown build mode."),
    };

    private static string RequireString(JsonElement element, string propertyName) =>
        element.GetProperty(propertyName).GetString()
        ?? throw new JsonException($"{propertyName} must be a string.");
}
