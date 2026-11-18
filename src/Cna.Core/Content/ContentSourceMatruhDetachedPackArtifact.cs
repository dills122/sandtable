using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Content;

/// <summary>Detached Matruh battalion definitions nested in the actual retained source content.</summary>
internal sealed class ContentSourceMatruhDetachedPackArtifact
{
    internal const string FormatId = "sandtable.source-matruh-detached.v1";
    internal const string CorpsBasisHash = "sha256:396bc14c2e3fff24a69adf91d6e70af4b510dc66641cb95fb9b23f69c39da818"; // RUL-GRA-26: derived from CampaignSourceAxisSupplementalForces.ContentHash.
    internal const string MalettiBasisHash = "sha256:25a528d42b4eb1580ac75b72533b61b28352c8182e1765cad28649258776355e"; // RUL-GRA-26: derived from CampaignSourceAxisSupplementalForces.ContentHash.
    private readonly byte[] bytes;
    private ContentSourceMatruhDetachedPackArtifact(ContentScheduledPackIdentity basisIdentity, byte[] bytes)
    {
        BasisIdentity = basisIdentity; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basisIdentity.PackId, basisIdentity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentScheduledPackIdentity BasisIdentity { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static ContentOrigin Origin => new(ContentOriginKind.SourceDerived,
        [new RuleReference(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.41"),
         new RuleReference(Cna1979GrazianiSourceFacts.ErrataSourceId, "4.44b"),
         new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "oa-70-infantry-p9"),
         new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "oa-british-p11"),
         new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "4.46a")]);
    internal static ContentSourceMatruhDetachedPackArtifact Create(ContentSourceAxisCorpsArtilleryPackArtifact corps,
        ContentSourceCertifiedMalettiPackArtifact? maletti)
    {
        ArgumentNullException.ThrowIfNull(corps);
        if (corps.Hash() != ContentSourceAxisCorpsArtilleryPackArtifact.ContentHash
            || maletti is not null && (maletti.Hash() != ContentSourceCertifiedMalettiPackArtifact.ContentHash
                || maletti.Basis.Identity != corps.Identity))
            throw new InvalidOperationException("Matruh source predecessor content differs from its pinned profile.");
        var identity = maletti?.Identity ?? corps.Identity;
        var basisBytes = maletti?.CanonicalBytes ?? corps.CanonicalBytes;
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId); writer.WriteString("baseFormatId", identity.FormatId);
            writer.WriteString("baseHash", identity.Hash); writer.WriteBase64String("baseContent", basisBytes);
            writer.WriteString("formationPolicy", CampaignSourceMatruhFormation.PolicyId);
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("commonwealthChartsHash", "sha256:f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981");
            writer.WriteString("errataHash", "sha256:5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb");
            writer.WriteString("historicalAssignmentStatus", "buffs-hampshire-provisional-errata-essex-durham-conflict");
            writer.WriteStartArray("units");
            foreach (var member in CampaignSourceMatruhFormation.Members.OrderBy(value => value.ElementId, StringComparer.Ordinal))
            {
                writer.WriteStartObject(); writer.WriteString("elementId", member.ElementId); writer.WriteString("sideId", "commonwealth");
                writer.WriteString("sourceCode", member.SourceCode); writer.WriteString("assignmentStatus", member.AssignmentStatus);
                writer.WriteNumber("capabilityPoints", 10); writer.WriteNumber("stacking", 1);
                writer.WriteString("componentId", member.ElementId + ".infantry");
                writer.WriteString("componentClassId", Cna1979Combat.InfantryComponentClassId); writer.WriteNumber("toe", 6);
                writer.WriteNull("currentAttachedParentId"); writer.WriteNull("mobilityId"); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        var result = new ContentSourceMatruhDetachedPackArtifact(identity, stream.ToArray());
        if (result.Hash() != (maletti is null ? CorpsBasisHash : MalettiBasisHash))
            throw new InvalidOperationException("Matruh detached content differs from its pinned profile.");
        return result;
    }
    internal static ContentSourceMatruhDetachedPackArtifact Read(byte[] canonicalBytes)
    {
        ArgumentNullException.ThrowIfNull(canonicalBytes);
        using var document = JsonDocument.Parse(canonicalBytes);
        var root = document.RootElement;
        var baseBytes = root.GetProperty("baseContent").GetBytesFromBase64();
        ContentSourceMatruhDetachedPackArtifact expected;
        if (root.GetProperty("baseFormatId").GetString() == ContentSourceAxisCorpsArtilleryPackArtifact.FormatId)
            expected = Create(ContentSourceAxisCorpsArtilleryPackArtifact.Read(baseBytes), null);
        else if (root.GetProperty("baseFormatId").GetString() == ContentSourceCertifiedMalettiPackArtifact.FormatId)
        {
            var maletti = ContentSourceCertifiedMalettiPackArtifact.Read(baseBytes);
            expected = Create(maletti.Basis, maletti);
        }
        else throw new InvalidOperationException("Unsupported Matruh content predecessor.");
        if (!canonicalBytes.AsSpan().SequenceEqual(expected.bytes))
            throw new InvalidOperationException("Matruh content is not canonical.");
        return expected;
    }
}
