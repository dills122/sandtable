using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Content;

internal sealed record ContentSourceIndependentUnit(string ElementId, string SideId, string LocationId,
    int CapabilityPoints, int Toe, string SourceLocationReference, ContentOrigin Origin);

/// <summary>Versioned source catalog, not a complete map or movement definition.</summary>
internal sealed class ContentSourceIndependentPackArtifact
{
    internal const string FormatId = "sandtable.source-independent-setup.v1";
    // Derived from (and so changes whenever) CampaignSourceAxisSupplementalForces.ContentHash
    // changes - see RUL-GRA-26's note on that constant for the most recent reason.
    internal const string ContentHash = "sha256:3a8832098cac32c35d7ec7076273c0903e093a04d4529da973a52b8c3255c4aa";
    private static readonly ReadOnlyCollection<ContentSourceIndependentUnit> Catalog = Array.AsReadOnly(
        CampaignSourceAxisPriorityMotorization.Targets.Select(id => new ContentSourceIndependentUnit(id, "axis", "c3617", 10, 5,
            "60.31", ContentSourceMalettiPackArtifact.Origin)).Concat(new[]
        {
            Commonwealth("1-kings-royal-rifle-corps", "c3922"),
            Commonwealth("2-rifle-brigade", "c3320"),
        }).OrderBy(unit => unit.ElementId, StringComparer.Ordinal).ToArray());
    private readonly byte[] bytes;
    private ContentSourceIndependentPackArtifact(ContentSourceSetupPackArtifact basis, byte[] bytes)
    {
        Basis = basis;
        this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceSetupPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal static IReadOnlyList<ContentSourceIndependentUnit> Units => Catalog;
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;

    internal static ContentSourceIndependentPackArtifact Create(ContentSourceSetupPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != CampaignSourceCairoActiveWorld.SourceContentHash)
            throw new InvalidOperationException("Independent source catalog requires pinned Cairo content.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash());
            writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("axisSupplementHash", CampaignSourceAxisSupplementalForces.ContentHash);
            writer.WriteString("commonwealthSupplementHash", CampaignSourceCommonwealthSupplementalForces.ContentHash);
            writer.WriteString("mapTerrainAndTopology", "unresolved");
            writer.WriteStartArray("units");
            foreach (var unit in Catalog)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", unit.ElementId);
                writer.WriteString("sideId", unit.SideId);
                writer.WriteString("locationId", unit.LocationId);
                writer.WriteString("sourceLocationReference", unit.SourceLocationReference);
                writer.WriteNumber("capabilityPoints", unit.CapabilityPoints);
                writer.WriteString("printedCapabilityPoints", "10+");
                writer.WriteString("componentId", unit.ElementId + ".infantry");
                writer.WriteString("componentClassId", Cna1979Combat.InfantryComponentClassId);
                writer.WriteNumber("toe", unit.Toe);
                if (unit.SideId == "axis")
                {
                    writer.WriteNumber("stacking", 1);
                    writer.WriteString("stackingSource", "spi-1979-air-logistics:counter-section-2-p40");
                    writer.WriteString("activation", "owner-established-detachment");
                }
                else
                {
                    writer.WriteNull("stacking");
                    writer.WriteNull("stackingSource");
                    writer.WriteString("activation", "scenario-explicit-detachment-60.41");
                }
                writer.WriteNull("currentParentId");
                writer.WriteNull("mobilityId");
                writer.WriteStartArray("origin");
                foreach (var reference in unit.Origin.References)
                {
                    writer.WriteStartObject();
                    writer.WriteString("sourceId", reference.SourceId);
                    writer.WriteString("locator", reference.Locator);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        var result = new ContentSourceIndependentPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash)
            throw new InvalidOperationException("Independent source catalog differs from its pinned profile.");
        return result;
    }

    internal static ContentSourceIndependentPackArtifact Read(byte[] canonicalBytes)
    {
        ArgumentNullException.ThrowIfNull(canonicalBytes);
        using var document = JsonDocument.Parse(canonicalBytes);
        var basis = ContentSourceSetupPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!canonicalBytes.AsSpan().SequenceEqual(expected.bytes))
            throw new InvalidOperationException("Independent source catalog is not canonical.");
        return expected;
    }

    private static ContentSourceIndependentUnit Commonwealth(string id, string location) => new(id, "commonwealth", location, 10, 6,
        "60.41", new(ContentOriginKind.SourceDerived,
            [new RuleReference(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.41"),
             new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "oa-7-armoured-p6"),
             new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "4.46a")]));

}
