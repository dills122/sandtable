using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Content;

/// <summary>Fixed source successor for one owner-detached Maletti motor battalion.</summary>
internal sealed class ContentSourceCertifiedMalettiPackArtifact
{
    internal const string FormatId = "sandtable.source-certified-maletti.v1";
    internal const string ContentHash = "sha256:76a234112183267516f7eb71b973e3728e792c96c3baf048cb35f34376cc1f57"; // RUL-GRA-26: derived from CampaignSourceAxisSupplementalForces.ContentHash.
    private readonly byte[] bytes;
    private ContentSourceCertifiedMalettiPackArtifact(ContentSourceAxisCorpsArtilleryPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceAxisCorpsArtilleryPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static ContentOrigin Origin => new(ContentOriginKind.SourceDerived,
        [new RuleReference(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.31"),
         new RuleReference(Cna1979GrazianiSourceFacts.AxisChartsSourceId, "oa-maletti-p7"),
         new RuleReference(Cna1979GrazianiSourceFacts.AxisChartsSourceId, "4.46b")]);
    internal static ContentSourceCertifiedMalettiPackArtifact Create(ContentSourceAxisCorpsArtilleryPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceAxisCorpsArtilleryPackArtifact.ContentHash)
            throw new InvalidOperationException("Certified Maletti content requires pinned corps predecessor.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash()); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("sourceSupplementHash", CampaignSourceAxisSupplementalForces.ContentHash);
            writer.WriteString("hierarchyPolicy", CampaignSourceMalettiHierarchy.PolicyId);
            writer.WriteString("activation", "owner-selected-single-detachment-after-stack-certification");
            writer.WriteString("scenarioOverOaLxPolicy", "provisional-ii-m-only-maletti-lx-whole-bardia");
            writer.WriteStartArray("units");
            foreach (var id in CampaignSourceAxisPriorityMotorization.Targets.Order(StringComparer.Ordinal))
            {
                writer.WriteStartObject(); writer.WriteString("elementId", id); writer.WriteString("sideId", "axis");
                writer.WriteString("locationId", "c3617"); writer.WriteNumber("capabilityPoints", 10);
                writer.WriteString("printedCapabilityPoints", "10+"); writer.WriteString("componentId", id + ".infantry");
                writer.WriteString("componentClassId", Cna1979Combat.InfantryComponentClassId); writer.WriteNumber("toe", 5);
                writer.WriteNumber("stacking", 1); writer.WriteString("stackingSource", "spi-1979-air-logistics:counter-section-2-p40");
                writer.WriteString("historicalAssignedParentId", "1-libyan-motor-regiment-hq");
                writer.WriteNull("currentAttachedParentId"); writer.WriteNull("mobilityId"); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        var result = new ContentSourceCertifiedMalettiPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash)
            throw new InvalidOperationException("Certified Maletti content differs from its pinned profile.");
        return result;
    }
    internal static ContentSourceCertifiedMalettiPackArtifact Read(byte[] canonicalBytes)
    {
        ArgumentNullException.ThrowIfNull(canonicalBytes);
        using var document = JsonDocument.Parse(canonicalBytes);
        var basis = ContentSourceAxisCorpsArtilleryPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!canonicalBytes.AsSpan().SequenceEqual(expected.bytes))
            throw new InvalidOperationException("Certified Maletti content is not canonical.");
        return expected;
    }
}
