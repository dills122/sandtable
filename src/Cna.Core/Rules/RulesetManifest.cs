using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Rules;

public sealed class RulesetManifest
{
    public RulesetManifest(
        string rulesetId,
        int contractVersion,
        IEnumerable<RulesetArtifact> artifacts,
        IEnumerable<Ruling> rulings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rulesetId);
        ArgumentOutOfRangeException.ThrowIfLessThan(contractVersion, 1);
        ArgumentNullException.ThrowIfNull(artifacts);
        ArgumentNullException.ThrowIfNull(rulings);

        RulesetId = rulesetId;
        ContractVersion = contractVersion;
        Artifacts = Array.AsReadOnly(artifacts.ToArray());
        Rulings = Array.AsReadOnly(rulings.ToArray());

        if (Artifacts.Any(artifact => artifact is null))
        {
            throw new ArgumentException("Null artifacts are not allowed.", nameof(artifacts));
        }

        if (Rulings.Any(ruling => ruling is null))
        {
            throw new ArgumentException("Null rulings are not allowed.", nameof(rulings));
        }

        RequireUniqueIds(Artifacts.Select(artifact => artifact.ArtifactId), nameof(artifacts));
        RequireUniqueIds(Rulings.Select(ruling => ruling.RulingId), nameof(rulings));

        Hash = CalculateHash();
    }

    public string RulesetId { get; }

    public int ContractVersion { get; }

    public IReadOnlyList<RulesetArtifact> Artifacts { get; }

    public IReadOnlyList<Ruling> Rulings { get; }

    public string Hash { get; }

    internal static byte[] SerializeCanonicalRuling(Ruling ruling)
    {
        ArgumentNullException.ThrowIfNull(ruling);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteRuling(writer, ruling);
        }
        return stream.ToArray();
    }

    private static void RequireUniqueIds(IEnumerable<string> ids, string parameterName)
    {
        var knownIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var id in ids)
        {
            if (!knownIds.Add(id))
            {
                throw new ArgumentException($"Duplicate identifier '{id}'.", parameterName);
            }
        }
    }

    private string CalculateHash()
    {
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("rulesetId", RulesetId);
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteStartArray("artifacts");

            foreach (var artifact in Artifacts.OrderBy(value => value.ArtifactId, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("artifactId", artifact.ArtifactId);
                writer.WriteString("contentHash", artifact.ContentHash);
                WriteSources(writer, artifact.Sources);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteStartArray("rulings");

            foreach (var ruling in Rulings.OrderBy(value => value.RulingId, StringComparer.Ordinal))
            {
                WriteRuling(writer, ruling);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
    }

    private static void WriteRuling(Utf8JsonWriter writer, Ruling ruling)
    {
        writer.WriteStartObject();
        writer.WriteString("rulingId", ruling.RulingId);
        writer.WriteString("conflictId", ruling.ConflictId);
        WriteSortedValues(writer, "alternativeIds", ruling.AlternativeIds);
        writer.WriteString("selectedBehaviorId", ruling.SelectedBehaviorId);
        WriteSortedValues(writer, "protectingTestIds", ruling.ProtectingTestIds);
        WriteSources(writer, ruling.Sources);
        writer.WriteEndObject();
    }

    private static void WriteSortedValues(
        Utf8JsonWriter writer,
        string propertyName,
        IEnumerable<string> values)
    {
        writer.WriteStartArray(propertyName);

        foreach (var value in values.Order(StringComparer.Ordinal))
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }

    private static void WriteSources(
        Utf8JsonWriter writer,
        IEnumerable<RuleReference> sources)
    {
        writer.WriteStartArray("sources");

        foreach (var source in sources
            .OrderBy(value => value.SourceId, StringComparer.Ordinal)
            .ThenBy(value => value.Locator, StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", source.SourceId);
            writer.WriteString("locator", source.Locator);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }
}
