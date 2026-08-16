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

        RequireUniqueIds(Artifacts.Select(artifact => artifact.ArtifactId), nameof(artifacts));
        RequireUniqueIds(Rulings.Select(ruling => ruling.RulingId), nameof(rulings));

        Hash = CalculateHash();
    }

    public string RulesetId { get; }

    public int ContractVersion { get; }

    public IReadOnlyList<RulesetArtifact> Artifacts { get; }

    public IReadOnlyList<Ruling> Rulings { get; }

    public string Hash { get; }

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
                writer.WriteStartObject();
                writer.WriteString("rulingId", ruling.RulingId);
                writer.WriteString("decisionId", ruling.DecisionId);
                WriteSources(writer, ruling.Sources);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant();
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
