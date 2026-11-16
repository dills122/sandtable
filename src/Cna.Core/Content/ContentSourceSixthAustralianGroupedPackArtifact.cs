using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

internal sealed class ContentSourceSixthAustralianGroupedPackArtifact
{
    internal const string FormatId = "sandtable.source-sixth-australian-grouped.v1";
    internal const string ContentHash = "sha256:896b00d665b19b696011757e91232d1beafc484f463908d3ddb1dbe74aaeb655";
    private readonly byte[] bytes;
    private ContentSourceSixthAustralianGroupedPackArtifact(ContentSourceCatanzaroGroupedPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceCatanzaroGroupedPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static ContentSourceSixthAustralianGroupedPackArtifact Create(ContentSourceCatanzaroGroupedPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceCatanzaroGroupedPackArtifact.ContentHash)
            throw new InvalidOperationException("Sixth Australian content requires its pinned predecessor.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash()); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("definitionHash", CampaignSourceSixthAustralianGroupedDefinition.Hash());
            writer.WriteBase64String("definition", CampaignSourceSixthAustralianGroupedDefinition.SerializeCanonical());
            writer.WriteEndObject();
        }
        var result = new ContentSourceSixthAustralianGroupedPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash)
            throw new InvalidOperationException("Sixth Australian source content differs from its pinned profile: " + result.Hash());
        return result;
    }
    internal static ContentSourceSixthAustralianGroupedPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceCatanzaroGroupedPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(expected.bytes)) throw new InvalidOperationException("Sixth Australian grouped content is not canonical.");
        return expected;
    }
}
