using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Content;

/// <summary>Two source-read Map C neighborhoods. Geometry alone does not authorize movement.</summary>
internal sealed class ContentSourceLocalMapPackArtifact
{
    internal const string FormatId = "sandtable.source-local-map.v1";
    internal const string ContentHash = "sha256:d60465ab4a1fdf9799b8f49e8a22768b6ddb69e305e8e854f3c524d0295caf35";
    private static readonly IReadOnlyList<string> C3320Neighbors = Array.AsReadOnly<string>(
        ["c3219", "c3220", "c3319", "c3321", "c3419", "c3420"]);
    private static readonly IReadOnlyList<string> C3617Neighbors = Array.AsReadOnly<string>(
        ["c3517", "c3518", "c3616", "c3618", "c3717", "c3718"]);
    private readonly byte[] bytes;
    private ContentSourceLocalMapPackArtifact(ContentSourceIndependentPackArtifact basis, byte[] bytes)
    {
        Basis = basis;
        this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceIndependentPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static IReadOnlyList<string> Neighbors(string locationId) => locationId switch
    {
        "c3320" => C3320Neighbors,
        "c3617" => C3617Neighbors,
        _ => throw new InvalidOperationException("Source map neighborhood is unavailable."),
    };

    internal static ContentSourceLocalMapPackArtifact Create(ContentSourceIndependentPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceIndependentPackArtifact.ContentHash)
            throw new InvalidOperationException("Local map requires the pinned independent source catalog.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash());
            writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("mapSource", "spi-1979-map-c:pdf-p3");
            writer.WriteString("mapSourceHash", "sha256:8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030");
            writer.WriteString("status", "two-reader-limited-geometry-not-movement-legality");
            writer.WriteBoolean("mapCoverageComplete", false);
            writer.WriteStartArray("neighborhoods");
            WriteGeometry(writer, "c3320", Neighbors("c3320"));
            WriteGeometry(writer, "c3617", Neighbors("c3617"));
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        var result = new ContentSourceLocalMapPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash)
            throw new InvalidOperationException("Local source map differs from its pinned profile.");
        return result;
    }

    internal static ContentSourceLocalMapPackArtifact Read(byte[] canonicalBytes)
    {
        ArgumentNullException.ThrowIfNull(canonicalBytes);
        using var document = JsonDocument.Parse(canonicalBytes);
        var basis = ContentSourceIndependentPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!canonicalBytes.AsSpan().SequenceEqual(expected.bytes))
            throw new InvalidOperationException("Local source map content is not canonical.");
        return expected;
    }

    private static void WriteGeometry(Utf8JsonWriter writer, string id, IReadOnlyList<string> neighbors)
    {
        writer.WriteStartObject();
        writer.WriteString("locationId", id);
        writer.WriteNull("terrainId");
        writer.WriteNull("hexsideEffects");
        writer.WriteStartArray("adjacentLocationIds");
        foreach (var neighbor in neighbors) writer.WriteStringValue(neighbor);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
