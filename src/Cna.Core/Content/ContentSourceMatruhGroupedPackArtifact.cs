using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

// The owner-selected Matruh disposition is state, not part of this fixed content.
internal sealed class ContentSourceMatruhGroupedPackArtifact
{
    internal const string FormatId = "sandtable.source-matruh-grouped.v1";
    internal const string ContentHash = "sha256:1964df64889750d230ca0f0337eb78b717e6964200904f63de2a1e252cfd5922";
    private readonly byte[] bytes;
    private ContentSourceMatruhGroupedPackArtifact(ContentSourceFourthIndianPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceFourthIndianPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static ContentSourceMatruhGroupedPackArtifact Create(ContentSourceFourthIndianPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceFourthIndianPackArtifact.ContentHash)
            throw new InvalidOperationException("Matruh group requires pinned source predecessor.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash()); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("cwChartsHash", "sha256:f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981");
            writer.WriteString("landRulesHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("errataHash", "sha256:5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb");
            writer.WriteString("sourcePolicy", CampaignSourceMatruhFormation.PolicyId);
            writer.WriteString("assignmentRuling", "provisional-errata-buffs-hampshire-assigned-oa-no-assigned-exception");
            writer.WriteString("colocationRuling", "provisional-owner-select-buffs-hampshire-with-matruh");
            writer.WriteString("essexDurhamAssignment", "scenario-23-70-vs-oa-note-f-unresolved");
            writer.WriteString("garrisonStack", "9.16-zero-at-matruh"); writer.WriteString("movementRestriction", "no-voluntary-move-until-selby-or-stage-attacked-close-assault");
            writer.WriteString("conversion", "gt12-operation-stage2-selby");
            writer.WriteString("headquartersId", CampaignSourceMatruhFormation.HeadquartersId); writer.WriteNumber("headquartersCpa", 30);
            writer.WriteString("locationId", "d3714"); writer.WriteString("poolId", "cw-matruh-attached"); writer.WriteNumber("poolPoints", 20);
            writer.WriteStartArray("members");
            foreach (var member in CampaignSourceMatruhFormation.Members.OrderBy(value => value.ElementId, StringComparer.Ordinal))
            {
                writer.WriteStartObject(); writer.WriteString("elementId", member.ElementId); writer.WriteString("sourceCode", member.SourceCode);
                writer.WriteString("assignmentStatus", member.AssignmentStatus); writer.WriteString("componentId", member.ElementId + ".infantry");
                writer.WriteNumber("toe", 6); writer.WriteNumber("cpa", 10); writer.WriteNumber("individualStack", 1); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        var result = new ContentSourceMatruhGroupedPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash) throw new InvalidOperationException("Matruh grouped source content differs from pinned profile.");
        return result;
    }
    internal static ContentSourceMatruhGroupedPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceFourthIndianPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(expected.bytes)) throw new InvalidOperationException("Matruh grouped source content is not canonical.");
        return expected;
    }
}
