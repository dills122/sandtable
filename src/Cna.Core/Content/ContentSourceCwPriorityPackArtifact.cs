using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Content;

/// <summary>Normalized fixed source definitions; activation remains authoritative World8 state.</summary>
internal sealed class ContentSourceCwPriorityPackArtifact
{
    internal const string FormatId = "sandtable.source-cw-priority-catalog.v1";
    internal const string ContentHash = "sha256:139c00802df6a18506f23c44e3f3e88818ad641b8e05c7bb089a7bc4c2c3a091"; // RUL-GRA-28: derived from CampaignSourceAxisSupplementalForces.ContentHash.
    private readonly byte[] bytes;
    private ContentSourceCwPriorityPackArtifact(ContentSourceAxisCorpsArtilleryPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceAxisCorpsArtilleryPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static ContentOrigin FrenchOrigin => new(ContentOriginKind.SourceDerived,
        [new RuleReference(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.41"),
         new RuleReference(Cna1979GrazianiSourceFacts.ErrataSourceId, "4.44b"),
         new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "oa-unassigned-allied-p21"),
         new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "4.46a")]);
    internal static ContentSourceCwPriorityPackArtifact Create(ContentSourceAxisCorpsArtilleryPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceAxisCorpsArtilleryPackArtifact.ContentHash)
            throw new InvalidOperationException("CW priority content requires pinned corps source.");
        // These are fixed definitions, not claims that optional units were activated.
        var malettiDefinitions = ContentSourceCertifiedMalettiPackArtifact.Create(basis);
        var matruhDefinitions = ContentSourceMatruhDetachedPackArtifact.Create(basis, malettiDefinitions);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash()); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("malettiDefinitionsHash", malettiDefinitions.Hash());
            writer.WriteBase64String("malettiDefinitions", malettiDefinitions.CanonicalBytes);
            writer.WriteString("matruhDefinitionsHash", matruhDefinitions.Hash());
            writer.WriteBase64String("matruhDefinitions", matruhDefinitions.CanonicalBytes);
            writer.WriteString("catalogPolicy", CampaignSourceCwPriorityCatalog.PolicyId);
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("cwChartsHash", "sha256:f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981");
            writer.WriteString("errataHash", "sha256:5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb");
            writer.WriteString("frenchCounterSource", "spi-1979-air-logistics:counter-section-5-p42");
            writer.WriteString("frenchArrival", "errata-4.44b-deployed-d");
            writer.WriteStartArray("targets");
            foreach (var target in CampaignSourceCwPriorityCatalog.Targets)
            {
                writer.WriteStartObject(); writer.WriteString("elementId", target.ElementId);
                writer.WriteString("locationId", target.LocationId); writer.WriteString("poolId", target.PoolId);
                writer.WriteString("sourceCode", target.SourceCode); writer.WriteNumber("toe", target.InfantryToe);
                writer.WriteString("componentId", target.ElementId + ".infantry");
                writer.WriteString("componentClassId", Cna1979Combat.InfantryComponentClassId);
                writer.WriteString("printedCapabilityPoints", "10+"); writer.WriteNumber("capabilityPoints", 10);
                writer.WriteNumber("stacking", target.SourceCode == "v" ? 0 : 1);
                writer.WriteNull("currentAttachedParentId"); writer.WriteNull("mobilityId"); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        var result = new ContentSourceCwPriorityPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash)
            throw new InvalidOperationException("CW priority content differs from its pinned profile.");
        return result;
    }
    internal static ContentSourceCwPriorityPackArtifact Read(byte[] canonicalBytes)
    {
        ArgumentNullException.ThrowIfNull(canonicalBytes);
        using var document = JsonDocument.Parse(canonicalBytes);
        var basis = ContentSourceAxisCorpsArtilleryPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!canonicalBytes.AsSpan().SequenceEqual(expected.bytes))
            throw new InvalidOperationException("CW priority source content is not canonical.");
        return expected;
    }
}
