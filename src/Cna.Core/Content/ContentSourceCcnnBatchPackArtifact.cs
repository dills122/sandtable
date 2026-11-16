using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

// Set-based successor: both player setup orders produce identical content.
internal sealed class ContentSourceCcnnBatchPackArtifact
{
    internal const string FormatId = "sandtable.source-grouped-extensions.v1";
    internal const string CcnnHash = "sha256:24a0eb68bfe9d3fe77af383822b80c12e6d602d958df1400c0a1f62a3bd5efe8";
    internal const string CombinedHash = "sha256:842044d8d54f0ef5cf87a40f13de1d5268f4aa21dda45e9dd734afd49ef0149d";
    internal const string CcnnArescaHash = "sha256:9f7d8c4112bafebc88013be990cfbf53c8b02a5f77c5380aea738d1454721110";
    internal const string CombinedArescaHash = "sha256:11ab37b81dcd3d9749b34889f95da0b4e585d4f2aa264a52ee0d34e80b4e0f34";
    private readonly byte[] bytes;
    private ContentSourceCcnnBatchPackArtifact(byte[] bytes) => this.bytes = bytes;
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes));
    internal static string HashFor(bool australian, bool aresca) => aresca
        ? (australian ? CombinedArescaHash : CcnnArescaHash) : (australian ? CombinedHash : CcnnHash);
    internal static ContentSourceCcnnBatchPackArtifact Create(ContentSourceCatanzaroGroupedPackArtifact basis, bool australian, bool aresca = false)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceCatanzaroGroupedPackArtifact.ContentHash)
            throw new InvalidOperationException("Grouped extensions require pinned Catanzaro predecessor.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash()); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteStartArray("extensions");
            if (aresca)
            {
                writer.WriteStartObject(); writer.WriteString("id", "aresca");
                writer.WriteString("definitionHash", CampaignSourceArescaGroupedDefinition.Hash());
                writer.WriteBase64String("definition", CampaignSourceArescaGroupedDefinition.SerializeCanonical()); writer.WriteEndObject();
            }
            writer.WriteStartObject(); writer.WriteString("id", "ccnn-batch");
            writer.WriteString("definitionHash", CampaignSourceCcnnBatch.Hash());
            writer.WriteBase64String("definition", CampaignSourceCcnnBatch.SerializeCanonical()); writer.WriteEndObject();
            if (australian)
            {
                writer.WriteStartObject(); writer.WriteString("id", "sixth-australian");
                writer.WriteString("definitionHash", CampaignSourceSixthAustralianGroupedDefinition.Hash());
                writer.WriteBase64String("definition", CampaignSourceSixthAustralianGroupedDefinition.SerializeCanonical()); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        var result = new ContentSourceCcnnBatchPackArtifact(stream.ToArray());
        if (result.Hash() != HashFor(australian, aresca))
            throw new InvalidOperationException("Grouped extension content differs from pinned profile: " + result.Hash());
        return result;
    }
    internal static ContentSourceCcnnBatchPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        var basis = ContentSourceCatanzaroGroupedPackArtifact.Read(root.GetProperty("baseContent").GetBytesFromBase64());
        var ids = root.GetProperty("extensions").EnumerateArray().Select(value => value.GetProperty("id").GetString()).ToArray();
        if (!ids.SequenceEqual(["ccnn-batch"]) && !ids.SequenceEqual(["ccnn-batch", "sixth-australian"])
            && !ids.SequenceEqual(["aresca", "ccnn-batch"]) && !ids.SequenceEqual(["aresca", "ccnn-batch", "sixth-australian"]))
            throw new InvalidOperationException("Grouped extension set is invalid.");
        var expected = Create(basis, ids.Contains("sixth-australian", StringComparer.Ordinal), ids.Contains("aresca", StringComparer.Ordinal));
        if (!bytes.AsSpan().SequenceEqual(expected.bytes)) throw new InvalidOperationException("Grouped extension content is not canonical.");
        return expected;
    }
}
