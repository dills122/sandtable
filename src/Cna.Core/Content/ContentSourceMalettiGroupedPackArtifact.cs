using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

internal sealed class ContentSourceMalettiGroupedPackArtifact
{
    internal const string FormatId = "sandtable.source-maletti-grouped.v1";
    internal const string ContentHash = "sha256:4b813f546d07188da8d9e9e0dd41ff5c6bd4bc02e681b7e46c23a001d68d4bbc"; // RUL-GRA-28: derived from CampaignSourceAxisSupplementalForces.ContentHash.
    private readonly byte[] bytes;
    private ContentSourceMalettiGroupedPackArtifact(ContentSourceMatruhGroupedPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceMatruhGroupedPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static ContentSourceMalettiGroupedPackArtifact Create(ContentSourceMatruhGroupedPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceMatruhGroupedPackArtifact.ContentHash)
            throw new InvalidOperationException("Maletti grouped content requires pinned source predecessor.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash()); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("definitionHash", CampaignSourceMalettiGroupedDefinition.Hash());
            writer.WriteBase64String("definition", CampaignSourceMalettiGroupedDefinition.SerializeCanonical());
            writer.WriteEndObject();
        }
        var result = new ContentSourceMalettiGroupedPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash) throw new InvalidOperationException("Maletti grouped source content differs from pinned profile.");
        return result;
    }
    internal static ContentSourceMalettiGroupedPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceMatruhGroupedPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(expected.bytes)) throw new InvalidOperationException("Maletti grouped source content is not canonical.");
        return expected;
    }
}
