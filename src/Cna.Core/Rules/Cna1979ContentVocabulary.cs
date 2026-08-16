using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Rules;

public enum ContentVocabularyKind
{
    Side,
    Terrain,
    EdgeFeature,
    Organization,
}

public enum ContentDirectionPolicy
{
    NotApplicable,
    Forbidden,
    Required,
}

public sealed record ContentVocabularyEntry
{
    public ContentVocabularyEntry(
        int schemaVersion,
        ContentVocabularyKind kind,
        string id,
        ContentDirectionPolicy directionPolicy,
        IEnumerable<RuleReference> sources)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(schemaVersion, 1);

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!Enum.IsDefined(directionPolicy))
        {
            throw new ArgumentOutOfRangeException(nameof(directionPolicy));
        }

        if (kind == ContentVocabularyKind.EdgeFeature
            && directionPolicy == ContentDirectionPolicy.NotApplicable)
        {
            throw new ArgumentException(
                "An edge feature must declare whether direction is required or forbidden.",
                nameof(directionPolicy));
        }

        if (kind != ContentVocabularyKind.EdgeFeature
            && directionPolicy != ContentDirectionPolicy.NotApplicable)
        {
            throw new ArgumentException(
                "Only edge features may declare a direction policy.",
                nameof(directionPolicy));
        }

        ArgumentNullException.ThrowIfNull(sources);
        var sourceCopy = sources.ToArray();

        if (sourceCopy.Length == 0 || sourceCopy.Any(source => source is null))
        {
            throw new ArgumentException(
                "At least one non-null source reference is required.",
                nameof(sources));
        }

        if (sourceCopy.Distinct().Count() != sourceCopy.Length)
        {
            throw new ArgumentException(
                "Duplicate source references are not allowed.",
                nameof(sources));
        }

        SchemaVersion = schemaVersion;
        Kind = kind;
        Id = id;
        DirectionPolicy = directionPolicy;
        Sources = Array.AsReadOnly(sourceCopy
            .OrderBy(source => source.SourceId, StringComparer.Ordinal)
            .ThenBy(source => source.Locator, StringComparer.Ordinal)
            .ToArray());
    }

    public int SchemaVersion { get; }

    public ContentVocabularyKind Kind { get; }

    public string Id { get; }

    public ContentDirectionPolicy DirectionPolicy { get; }

    public IReadOnlyList<RuleReference> Sources { get; }

    public bool Equals(ContentVocabularyEntry? other) =>
        ReferenceEquals(this, other)
        || (other is not null
            && SchemaVersion == other.SchemaVersion
            && Kind == other.Kind
            && string.Equals(Id, other.Id, StringComparison.Ordinal)
            && DirectionPolicy == other.DirectionPolicy
            && Sources.SequenceEqual(other.Sources));

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(SchemaVersion);
        hash.Add(Kind);
        hash.Add(Id, StringComparer.Ordinal);
        hash.Add(DirectionPolicy);

        foreach (var source in Sources)
        {
            hash.Add(source);
        }

        return hash.ToHashCode();
    }
}

public static class Cna1979ContentVocabulary
{
    public const int SchemaVersion = 1;
    public const string ArtifactId = "cna-1979.1.content-vocabulary";

    public static IReadOnlyList<ContentVocabularyEntry> Entries { get; } =
        Array.AsReadOnly<ContentVocabularyEntry>(
        [
            Create(
                ContentVocabularyKind.Side,
                "axis",
                ContentDirectionPolicy.NotApplicable,
                new RuleReference("spi-1979-common-charts", "7.2.initiative-ratings")),
            Create(
                ContentVocabularyKind.Side,
                "commonwealth",
                ContentDirectionPolicy.NotApplicable,
                new RuleReference("spi-1979-common-charts", "7.2.initiative-ratings")),
            Create(
                ContentVocabularyKind.Terrain,
                "land.terrain.clear",
                ContentDirectionPolicy.NotApplicable,
                new RuleReference("spi-1979-land-rules", "8.45.clear-hex")),
            Create(
                ContentVocabularyKind.Terrain,
                "land.terrain.desert",
                ContentDirectionPolicy.NotApplicable,
                new RuleReference("spi-1979-land-rules", "8.45.desert-hex")),
            Create(
                ContentVocabularyKind.EdgeFeature,
                "land.edge.road",
                ContentDirectionPolicy.Forbidden,
                new RuleReference("spi-1979-land-rules", "8.33"),
                new RuleReference("spi-1979-land-rules", "8.47")),
            Create(
                ContentVocabularyKind.EdgeFeature,
                "land.edge.track",
                ContentDirectionPolicy.Forbidden,
                new RuleReference("spi-1979-land-rules", "8.33"),
                new RuleReference("spi-1979-land-rules", "8.46")),
            Create(
                ContentVocabularyKind.EdgeFeature,
                "land.edge.slope",
                ContentDirectionPolicy.Required,
                new RuleReference("spi-1979-land-rules", "8.35"),
                new RuleReference("spi-1979-land-rules", "8.43")),
            Create(
                ContentVocabularyKind.EdgeFeature,
                "land.edge.ridge",
                ContentDirectionPolicy.Forbidden,
                new RuleReference("spi-1979-land-rules", "8.35"),
                new RuleReference("spi-1979-land-rules", "8.43")),
            Create(
                ContentVocabularyKind.Organization,
                "land.organization.regiment",
                ContentDirectionPolicy.NotApplicable,
                new RuleReference("spi-1979-land-rules", "4.23.organization-size-key")),
            Create(
                ContentVocabularyKind.Organization,
                "land.organization.battalion",
                ContentDirectionPolicy.NotApplicable,
                new RuleReference("spi-1979-land-rules", "4.23.organization-size-key"),
                new RuleReference("spi-1979-common-charts", "9.4.stacking-point-values")),
        ]);

    public static bool Contains(ContentVocabularyKind kind, string id) =>
        Entries.Any(entry => entry.Kind == kind
            && string.Equals(entry.Id, id, StringComparison.Ordinal));

    public static ContentVocabularyEntry Get(ContentVocabularyKind kind, string id) =>
        Entries.Single(entry => entry.Kind == kind
            && string.Equals(entry.Id, id, StringComparison.Ordinal));

    public static RulesetArtifact CreateArtifact() => new(
        ArtifactId,
        CalculateContentHash(Entries),
        Entries
            .SelectMany(entry => entry.Sources)
            .Distinct()
            .OrderBy(source => source.SourceId, StringComparer.Ordinal)
            .ThenBy(source => source.Locator, StringComparer.Ordinal));

    public static string CalculateContentHash(IEnumerable<ContentVocabularyEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var entryCopy = entries.ToArray();

        if (entryCopy.Length == 0 || entryCopy.Any(entry => entry is null))
        {
            throw new ArgumentException(
                "At least one non-null vocabulary entry is required.",
                nameof(entries));
        }

        var duplicate = entryCopy
            .GroupBy(entry => (entry.Kind, entry.Id))
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new ArgumentException(
                $"Duplicate vocabulary entry '{duplicate.Key.Kind}:{duplicate.Key.Id}'.",
                nameof(entries));
        }

        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", SchemaVersion);
            writer.WriteStartArray("entries");

            foreach (var entry in entryCopy
                .OrderBy(value => FormatKind(value.Kind), StringComparer.Ordinal)
                .ThenBy(value => value.Id, StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteNumber("schemaVersion", entry.SchemaVersion);
                writer.WriteString("kind", FormatKind(entry.Kind));
                writer.WriteString("id", entry.Id);
                writer.WriteString(
                    "directionPolicy",
                    FormatDirectionPolicy(entry.DirectionPolicy));
                writer.WriteStartArray("sources");

                foreach (var source in entry.Sources)
                {
                    writer.WriteStartObject();
                    writer.WriteString("sourceId", source.SourceId);
                    writer.WriteString("locator", source.Locator);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return $"sha256:{Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant()}";
    }

    private static ContentVocabularyEntry Create(
        ContentVocabularyKind kind,
        string id,
        ContentDirectionPolicy directionPolicy,
        params RuleReference[] sources) => new(
            SchemaVersion,
            kind,
            id,
            directionPolicy,
            sources);

    private static string FormatKind(ContentVocabularyKind kind) => kind switch
    {
        ContentVocabularyKind.Side => "side",
        ContentVocabularyKind.Terrain => "terrain",
        ContentVocabularyKind.EdgeFeature => "edge-feature",
        ContentVocabularyKind.Organization => "organization",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static string FormatDirectionPolicy(ContentDirectionPolicy policy) => policy switch
    {
        ContentDirectionPolicy.NotApplicable => "not-applicable",
        ContentDirectionPolicy.Forbidden => "forbidden",
        ContentDirectionPolicy.Required => "required",
        _ => throw new ArgumentOutOfRangeException(nameof(policy)),
    };
}
