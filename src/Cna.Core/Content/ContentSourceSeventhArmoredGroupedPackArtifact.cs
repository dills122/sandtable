using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

internal sealed class ContentSourceSeventhArmoredGroupedPackArtifact
{
    internal const string FormatId = "sandtable.source-seventh-armored-grouped.v1";
    internal const string ContentHash = "sha256:a07b37a12dd890a8ee468119f64273cee75a062a234c46ad487902b784b034d9";
    private readonly byte[] bytes;
    private ContentSourceSeventhArmoredGroupedPackArtifact(ContentSourceCireneMarmaricaGroupedPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceCireneMarmaricaGroupedPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static ContentSourceSeventhArmoredGroupedPackArtifact Create(ContentSourceCireneMarmaricaGroupedPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceCireneMarmaricaGroupedPackArtifact.ContentHash)
            throw new InvalidOperationException("Seventh Armored grouped content requires its pinned predecessor.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash()); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("definitionHash", CampaignSourceSeventhArmoredGroupedDefinition.Hash());
            writer.WriteBase64String("definition", CampaignSourceSeventhArmoredGroupedDefinition.SerializeCanonical());
            writer.WriteEndObject();
        }
        var result = new ContentSourceSeventhArmoredGroupedPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash)
            throw new InvalidOperationException("Seventh Armored source content differs from its pinned profile: " + result.Hash());
        return result;
    }
    internal static ContentSourceSeventhArmoredGroupedPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceCireneMarmaricaGroupedPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(expected.bytes)) throw new InvalidOperationException("Seventh Armored grouped content is not canonical.");
        return expected;
    }
}
