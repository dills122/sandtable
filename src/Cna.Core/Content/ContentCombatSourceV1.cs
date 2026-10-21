using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Content;

// Versioned combat facts supplement an existing Content 6 pack without changing its frozen bytes.
// The supplement is resolved by the pack's exact hash and rechecked on every cold replay.
internal sealed class ContentCombatSourceV1
{
    private readonly IReadOnlyDictionary<string, int> moraleByFormation;
    private readonly IReadOnlyDictionary<string, int> offensiveRatingByComponent;

    private ContentCombatSourceV1(string contentHash, string hash,
        IReadOnlyDictionary<string, int> moraleByFormation,
        IReadOnlyDictionary<string, int> offensiveRatingByComponent)
    {
        ContentHash = contentHash;
        Hash = hash;
        this.moraleByFormation = moraleByFormation;
        this.offensiveRatingByComponent = offensiveRatingByComponent;
    }

    internal string ContentHash { get; }
    internal string Hash { get; }

    internal int BasicMorale(string formationId) => moraleByFormation[formationId];
    internal int OffensiveRating(string componentId) => offensiveRatingByComponent[componentId];

    internal static ContentCombatSourceV1? Resolve(ContentPackV6Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        var resourceName = $"Cna.Core.Content.Fixtures.{artifact.Identity.PackId}.combat-source-v1.json";
        using var source = typeof(ContentCombatSourceV1).Assembly.GetManifestResourceStream(resourceName);
        if (source is null) return null;
        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        return Parse(buffer.ToArray(), artifact);
    }

    internal static ContentCombatSourceV1 Parse(ReadOnlySpan<byte> canonicalBytes,
        ContentPackV6Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        if (canonicalBytes.Length is 0 or > 65_536)
            throw new JsonException("Combat source size is invalid.");
        using var document = JsonDocument.Parse(canonicalBytes.ToArray(),
            new JsonDocumentOptions { MaxDepth = 16 });
        var root = document.RootElement;
        RequireProperties(root, "contractVersion", "contentPackId", "contentHash",
            "formationMorale", "componentRatings");
        if (root.GetProperty("contractVersion").GetInt32() != 1
            || root.GetProperty("contentPackId").GetString() != artifact.Identity.PackId
            || root.GetProperty("contentHash").GetString() != artifact.Identity.Hash)
            throw new JsonException("Combat source is not bound to this Content pack.");

        var sourceIds = artifact.Definition.LegacyDefinition.SourceIndex
            .Select(value => value.SourceId).ToHashSet(StringComparer.Ordinal);
        var morale = ReadFacts(root.GetProperty("formationMorale"), "formationId", "basicMorale",
            sourceIds, -3, 3);
        var ratings = ReadFacts(root.GetProperty("componentRatings"), "componentId",
            "offensiveCloseAssaultRating", sourceIds, 0, int.MaxValue);
        var expectedFormations = artifact.Definition.LegacyDefinition.Formations
            .Select(value => value.FormationId).Order(StringComparer.Ordinal);
        var expectedComponents = artifact.Definition.ElementCombatFacts
            .SelectMany(value => value.Components).Select(value => value.ComponentId)
            .Order(StringComparer.Ordinal);
        if (!morale.Keys.Order(StringComparer.Ordinal).SequenceEqual(expectedFormations)
            || !ratings.Keys.Order(StringComparer.Ordinal).SequenceEqual(expectedComponents))
            throw new JsonException("Combat source does not cover the pack's formations and components exactly.");

        var canonical = Serialize(artifact.Identity.PackId, artifact.Identity.Hash,
            root.GetProperty("formationMorale"), root.GetProperty("componentRatings"));
        if (!canonicalBytes.SequenceEqual(canonical))
            throw new JsonException("Combat source must use canonical bytes and ordering.");
        return new ContentCombatSourceV1(artifact.Identity.Hash,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(canonical)), morale, ratings);
    }

    private static Dictionary<string, int> ReadFacts(JsonElement array, string idName,
        string valueName, HashSet<string> sourceIds, int minimum, int maximum)
    {
        if (array.ValueKind != JsonValueKind.Array) throw new JsonException("Combat facts must be an array.");
        var values = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var fact in array.EnumerateArray())
        {
            RequireProperties(fact, idName, valueName, "sourceId", "locator");
            var id = ContentContractGuards.RequireStableId(fact.GetProperty(idName).GetString()!, idName);
            var value = fact.GetProperty(valueName).GetInt32();
            var sourceId = fact.GetProperty("sourceId").GetString();
            var locator = fact.GetProperty("locator").GetString();
            if (value < minimum || value > maximum || sourceId is null
                || !sourceIds.Contains(sourceId) || string.IsNullOrWhiteSpace(locator)
                || !values.TryAdd(id, value))
                throw new JsonException("Combat source contains an invalid or duplicate fact.");
            ContentContractGuards.RequireSourceAtom(locator, "locator");
        }
        return values;
    }

    private static byte[] Serialize(string packId, string contentHash, JsonElement formations,
        JsonElement components)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("contentPackId", packId);
            writer.WriteString("contentHash", contentHash);
            WriteFacts(writer, "formationMorale", formations, "formationId", "basicMorale");
            WriteFacts(writer, "componentRatings", components, "componentId",
                "offensiveCloseAssaultRating");
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static void WriteFacts(Utf8JsonWriter writer, string property, JsonElement facts,
        string idName, string valueName)
    {
        writer.WriteStartArray(property);
        foreach (var fact in facts.EnumerateArray().OrderBy(
            value => value.GetProperty(idName).GetString(), StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString(idName, fact.GetProperty(idName).GetString());
            writer.WriteNumber(valueName, fact.GetProperty(valueName).GetInt32());
            writer.WriteString("sourceId", fact.GetProperty("sourceId").GetString());
            writer.WriteString("locator", fact.GetProperty("locator").GetString());
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void RequireProperties(JsonElement value, params string[] names)
    {
        if (value.ValueKind != JsonValueKind.Object
            || !value.EnumerateObject().Select(property => property.Name)
                .SequenceEqual(names, StringComparer.Ordinal))
            throw new JsonException("Combat source has unexpected fields or field order.");
    }
}
