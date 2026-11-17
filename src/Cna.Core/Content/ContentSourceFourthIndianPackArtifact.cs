using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

// Exact predecessor and source roster are part of the content identity.
internal sealed class ContentSourceFourthIndianPackArtifact
{
    internal const string FormatId = "sandtable.source-fourth-indian.v1";
    internal const string ContentHash = "sha256:b7a1ce7c8cbb933de7fdc65986f75ae1c3070bc10aab69fd06c880726fc75f22";
    private readonly byte[] bytes;
    private ContentSourceFourthIndianPackArtifact(ContentSourceCwFreeGunsPackArtifact basis, byte[] bytes)
    {
        Basis = basis;
        this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceCwFreeGunsPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static ContentSourceFourthIndianPackArtifact Create(ContentSourceCwFreeGunsPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceCwFreeGunsPackArtifact.ContentHash)
            throw new InvalidOperationException("Fourth Indian content requires the pinned source predecessor.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash()); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("definitionHash", CampaignSourceFourthIndianDefinition.Hash());
            writer.WriteBase64String("definition", CampaignSourceFourthIndianDefinition.SerializeCanonical());
            writer.WriteEndObject();
        }
        var result = new ContentSourceFourthIndianPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash) throw new InvalidOperationException("Fourth Indian source content differs from its pinned profile.");
        return result;
    }
    internal static ContentSourceFourthIndianPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceCwFreeGunsPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(expected.bytes))
            throw new InvalidOperationException("Fourth Indian source content is not canonical.");
        return expected;
    }
}
