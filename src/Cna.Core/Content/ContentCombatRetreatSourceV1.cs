using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Content;

// Exact-pack retreat direction facts extend the normal Content 6 campaign
// without changing its frozen bytes or inferring direction from hidden World.
internal sealed class ContentCombatRetreatSourceV1
{
    private readonly IReadOnlyDictionary<string, string> anchors;

    private ContentCombatRetreatSourceV1(string contentHash, string hash,
        IReadOnlyDictionary<string, string> anchors)
    {
        ContentHash = contentHash;
        Hash = hash;
        this.anchors = anchors;
    }

    internal string ContentHash { get; }
    internal string Hash { get; }
    internal string Anchor(string sideId) => anchors[sideId];

    internal static ContentCombatRetreatSourceV1? Resolve(ContentPackV6Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        var resourceName = $"Cna.Core.Content.Fixtures.{artifact.Identity.PackId}.retreat-source-v1.json";
        using var source = typeof(ContentCombatRetreatSourceV1).Assembly
            .GetManifestResourceStream(resourceName);
        if (source is null) return null;
        using var buffer = new MemoryStream();
        source.CopyTo(buffer);
        return Parse(buffer.ToArray(), artifact);
    }

    internal static ContentCombatRetreatSourceV1 Parse(ReadOnlySpan<byte> canonicalBytes,
        ContentPackV6Artifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        if (canonicalBytes.Length is 0 or > 16_384)
            throw new JsonException("Retreat source size is invalid.");
        using var document = JsonDocument.Parse(canonicalBytes.ToArray(),
            new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        RequireProperties(root, "contractVersion", "contentPackId", "contentHash", "anchors");
        if (root.GetProperty("contractVersion").GetInt32() != 1
            || root.GetProperty("contentPackId").GetString() != artifact.Identity.PackId
            || root.GetProperty("contentHash").GetString() != artifact.Identity.Hash)
            throw new JsonException("Retreat source is not bound to this Content pack.");

        var sourceIds = artifact.Definition.LegacyDefinition.SourceIndex
            .Select(value => value.SourceId).ToHashSet(StringComparer.Ordinal);
        var locations = artifact.Definition.LegacyDefinition.Locations
            .Select(value => value.LocationId).ToHashSet(StringComparer.Ordinal);
        var sides = artifact.Definition.LegacyDefinition.Elements
            .Select(value => value.SideId).Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal).ToArray();
        var anchors = new Dictionary<string, string>(StringComparer.Ordinal);
        var rows = root.GetProperty("anchors").EnumerateArray().ToArray();
        foreach (var row in rows)
        {
            RequireProperties(row, "sideId", "locationId", "kind", "sourceId", "locator");
            var side = ContentContractGuards.RequireStableId(
                row.GetProperty("sideId").GetString()!, "sideId");
            var location = ContentContractGuards.RequireStableId(
                row.GetProperty("locationId").GetString()!, "locationId");
            var source = row.GetProperty("sourceId").GetString();
            var locator = row.GetProperty("locator").GetString();
            if (row.GetProperty("kind").GetString() != "friendly-supply-direction"
                || !locations.Contains(location) || source is null
                || !sourceIds.Contains(source) || string.IsNullOrWhiteSpace(locator)
                || !anchors.TryAdd(side, location))
                throw new JsonException("Retreat source contains an invalid anchor.");
            ContentContractGuards.RequireSourceAtom(locator, "locator");
        }
        if (!anchors.Keys.Order(StringComparer.Ordinal).SequenceEqual(sides)
            || !rows.Select(value => value.GetProperty("sideId").GetString())
                .SequenceEqual(sides))
            throw new JsonException("Retreat anchors must cover sides in canonical order.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("contentPackId", artifact.Identity.PackId);
            writer.WriteString("contentHash", artifact.Identity.Hash);
            writer.WriteStartArray("anchors");
            foreach (var row in rows)
            {
                writer.WriteStartObject();
                writer.WriteString("sideId", row.GetProperty("sideId").GetString());
                writer.WriteString("locationId", row.GetProperty("locationId").GetString());
                writer.WriteString("kind", "friendly-supply-direction");
                writer.WriteString("sourceId", row.GetProperty("sourceId").GetString());
                writer.WriteString("locator", row.GetProperty("locator").GetString());
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        var canonical = stream.ToArray();
        if (!canonicalBytes.SequenceEqual(canonical))
            throw new JsonException("Retreat source must use canonical bytes and ordering.");
        return new ContentCombatRetreatSourceV1(artifact.Identity.Hash,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(canonical)), anchors);
    }

    private static void RequireProperties(JsonElement value, params string[] names)
    {
        if (value.ValueKind != JsonValueKind.Object
            || !value.EnumerateObject().Select(property => property.Name)
                .SequenceEqual(names, StringComparer.Ordinal))
            throw new JsonException("Retreat source has unexpected fields or field order.");
    }
}
