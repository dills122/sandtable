using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

/// <summary>Pinned source successor for both Libyan initial formations.</summary>
internal sealed class ContentSourceLibyanGroupedPackArtifact
{
    internal const string FormatId = "sandtable.source-libyan-grouped.v1";
    internal const string ContentHash = "sha256:f2b55597a729b5505d3ab07011092853cd9333124df704bafc3e94df6ec7dff7"; // RUL-GRA-28: derived from CampaignSourceAxisSupplementalForces.ContentHash.
    private readonly byte[] bytes;
    private ContentSourceLibyanGroupedPackArtifact(ContentSourceMalettiGroupedPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceMalettiGroupedPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static ContentSourceLibyanGroupedPackArtifact Create(ContentSourceMalettiGroupedPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceMalettiGroupedPackArtifact.ContentHash)
            throw new InvalidOperationException("Libyan grouped content requires its pinned source predecessor.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash()); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("definitionHash", CampaignSourceLibyanGroupedDefinition.Hash());
            writer.WriteBase64String("definition", CampaignSourceLibyanGroupedDefinition.SerializeCanonical());
            writer.WriteEndObject();
        }
        var result = new ContentSourceLibyanGroupedPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash)
            throw new InvalidOperationException("Libyan grouped source content differs from its pinned profile.");
        return result;
    }
    internal static ContentSourceLibyanGroupedPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceMalettiGroupedPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(expected.bytes)) throw new InvalidOperationException("Libyan grouped source content is not canonical.");
        return expected;
    }
}
