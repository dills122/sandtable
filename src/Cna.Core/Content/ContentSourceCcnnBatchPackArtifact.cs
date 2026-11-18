using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

// Set-based successor: both player setup orders produce identical content.
internal sealed class ContentSourceCcnnBatchPackArtifact
{
    internal const string FormatId = "sandtable.source-grouped-extensions.v1";
    // All four derived from (and so changed when) CampaignSourceAxisSupplementalForces.ContentHash
    // changed - see RUL-GRA-28's note on that constant for the most recent reason.
    internal const string CcnnHash = "sha256:4e17559ae7a7358c9d13ff3bb29a7a2df02c507b31a8f8f2bb8a32d47f7d0203";
    internal const string CombinedHash = "sha256:b3d74b6204dd8821da271bb381847fc5939732e84f3bd991f3e5e04340e0efd6";
    internal const string CcnnArescaHash = "sha256:5d55910a8af9c03941b482dc6f215ebe2945a9dff63480a2eb537812d2b6f48c";
    internal const string CombinedArescaHash = "sha256:a43434610293aa4f1f4eb484765d8a694fd697952f48bbc7b4e7f0e0481f04e5";
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
