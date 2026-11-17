using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

// Set-based successor: both player setup orders produce identical content.
internal sealed class ContentSourceCcnnBatchPackArtifact
{
    internal const string FormatId = "sandtable.source-grouped-extensions.v1";
    internal const string CcnnHash = "sha256:7ba942011641992303537e680d68fe0f2fbb1fa6b431d9525100705941a1077a";
    internal const string CombinedHash = "sha256:eafd1ad9b045123f73a1770975a9b73bff966814294ae377a4a8cf56792b8156";
    internal const string CcnnArescaHash = "sha256:862ab1eece703a94a428dd7b0e23259741b351c7e6616cbcc2dadebf4cba5e74";
    internal const string CombinedArescaHash = "sha256:eb9be2f8b4648b8a47dab8c7bdc2a62b2de8029ce8b58f67c06f61e645cb9639";
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
