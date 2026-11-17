using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

internal sealed class ContentSourceCireneMarmaricaGroupedPackArtifact
{
    internal const string FormatId = "sandtable.source-cirene-marmarica-grouped.v1";
    internal const string ContentHash = "sha256:10b016f0ecbea76f84aaf0433df15f9d26b6b9fc1ec8a9ce562bc19b5bfd1d21";
    private readonly byte[] bytes;
    private ContentSourceCireneMarmaricaGroupedPackArtifact(ContentSourceLibyanGroupedPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceLibyanGroupedPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static ContentSourceCireneMarmaricaGroupedPackArtifact Create(ContentSourceLibyanGroupedPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceLibyanGroupedPackArtifact.ContentHash)
            throw new InvalidOperationException("Italian grouped content requires its pinned source predecessor.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash()); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("definitionHash", CampaignSourceCireneMarmaricaGroupedDefinition.Hash());
            writer.WriteBase64String("definition", CampaignSourceCireneMarmaricaGroupedDefinition.SerializeCanonical());
            writer.WriteEndObject();
        }
        var result = new ContentSourceCireneMarmaricaGroupedPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash)
            throw new InvalidOperationException("Italian grouped source content differs from its pinned profile.");
        return result;
    }
    internal static ContentSourceCireneMarmaricaGroupedPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceLibyanGroupedPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(expected.bytes)) throw new InvalidOperationException("Italian grouped source content is not canonical.");
        return expected;
    }
}
