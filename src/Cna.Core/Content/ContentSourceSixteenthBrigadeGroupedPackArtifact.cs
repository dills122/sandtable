using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

internal sealed class ContentSourceSixteenthBrigadeGroupedPackArtifact
{
    internal const string FormatId = "sandtable.source-sixteenth-brigade-grouped.v1";
    internal const string ContentHash = "sha256:b9cf1e19ad40bacfe4633df933fc4fbb79a281583ae9cfc277555190bfbba04a"; // RUL-GRA-28: derived from CampaignSourceAxisSupplementalForces.ContentHash.
    private readonly byte[] bytes;
    private ContentSourceSixteenthBrigadeGroupedPackArtifact(ContentSourceSeventhArmoredGroupedPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceSeventhArmoredGroupedPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static ContentSourceSixteenthBrigadeGroupedPackArtifact Create(ContentSourceSeventhArmoredGroupedPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceSeventhArmoredGroupedPackArtifact.ContentHash)
            throw new InvalidOperationException("Sixteenth Brigade grouped content requires its pinned predecessor.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash()); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("definitionHash", CampaignSourceSixteenthBrigadeGroupedDefinition.Hash());
            writer.WriteBase64String("definition", CampaignSourceSixteenthBrigadeGroupedDefinition.SerializeCanonical());
            writer.WriteEndObject();
        }
        var result = new ContentSourceSixteenthBrigadeGroupedPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash)
            throw new InvalidOperationException("Sixteenth Brigade source content differs from its pinned profile: " + result.Hash());
        return result;
    }
    internal static ContentSourceSixteenthBrigadeGroupedPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceSeventhArmoredGroupedPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(expected.bytes)) throw new InvalidOperationException("Sixteenth Brigade grouped content is not canonical.");
        return expected;
    }
}
