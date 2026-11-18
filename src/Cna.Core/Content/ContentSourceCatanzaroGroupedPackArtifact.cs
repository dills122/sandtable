using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

internal sealed class ContentSourceCatanzaroGroupedPackArtifact
{
    internal const string FormatId = "sandtable.source-catanzaro-grouped.v1";
    internal const string ContentHash = "sha256:baa7904c633b673bdc148ac8026e40d702ded8995a64daa8115db7cd9ef0590e"; // RUL-GRA-28: derived from CampaignSourceAxisSupplementalForces.ContentHash.
    private readonly byte[] bytes;
    private ContentSourceCatanzaroGroupedPackArtifact(ContentSourceSixteenthBrigadeGroupedPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceSixteenthBrigadeGroupedPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static ContentSourceCatanzaroGroupedPackArtifact Create(ContentSourceSixteenthBrigadeGroupedPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceSixteenthBrigadeGroupedPackArtifact.ContentHash)
            throw new InvalidOperationException("Catanzaro content requires its pinned predecessor.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash()); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("definitionHash", CampaignSourceCatanzaroGroupedDefinition.Hash());
            writer.WriteBase64String("definition", CampaignSourceCatanzaroGroupedDefinition.SerializeCanonical());
            writer.WriteEndObject();
        }
        var result = new ContentSourceCatanzaroGroupedPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash)
            throw new InvalidOperationException("Catanzaro source content differs from its pinned profile: " + result.Hash());
        return result;
    }
    internal static ContentSourceCatanzaroGroupedPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceSixteenthBrigadeGroupedPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(expected.bytes)) throw new InvalidOperationException("Catanzaro grouped content is not canonical.");
        return expected;
    }
}
