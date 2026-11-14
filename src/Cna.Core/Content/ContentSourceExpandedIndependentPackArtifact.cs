using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Content;

/// <summary>Source catalog successor adding three scenario-independent British counters.</summary>
internal sealed class ContentSourceExpandedIndependentPackArtifact
{
    internal const string FormatId = "sandtable.source-independent-expanded.v1";
    internal const string ContentHash = "sha256:526b73bfc5933689b7bb90b5362ccb482b33e23dc7e85ad52047ea05e706f714";
    private static readonly IReadOnlyList<ContentSourceIndependentUnit> Added = Array.AsReadOnly(new[]
    {
        Unit("1-royal-northumberland-fusiliers", "c3721", 8, 3),
        Unit("2-scots-guards", "c4131", 10, 6),
        Unit("3-coldstream-guards", "c3922", 10, 6),
    });
    private readonly byte[] bytes;
    private ContentSourceExpandedIndependentPackArtifact(ContentSourceLocalMapPackArtifact basis, byte[] bytes)
    {
        Basis = basis;
        this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceLocalMapPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static IReadOnlyList<ContentSourceIndependentUnit> AddedUnits => Added;

    internal static ContentSourceExpandedIndependentPackArtifact Create(ContentSourceLocalMapPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceLocalMapPackArtifact.ContentHash)
            throw new InvalidOperationException("Expanded source catalog requires pinned local map content.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash());
            writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("activation", "scenario-explicit-independent-60.41");
            writer.WriteString("stackingSource", "spi-1979-air-logistics:counter-section-7-p43");
            writer.WriteStartArray("units");
            foreach (var unit in Added)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", unit.ElementId);
                writer.WriteString("sideId", unit.SideId);
                writer.WriteString("locationId", unit.LocationId);
                writer.WriteNumber("capabilityPoints", unit.CapabilityPoints);
                writer.WriteString("componentId", unit.ElementId + ".infantry");
                writer.WriteString("componentClassId", Cna1979Combat.InfantryComponentClassId);
                writer.WriteNumber("toe", unit.Toe);
                writer.WriteNumber("stacking", 1);
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
        var result = new ContentSourceExpandedIndependentPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash)
            throw new InvalidOperationException("Expanded source catalog differs from its pinned profile.");
        return result;
    }

    internal static ContentSourceExpandedIndependentPackArtifact Read(byte[] canonicalBytes)
    {
        ArgumentNullException.ThrowIfNull(canonicalBytes);
        using var document = JsonDocument.Parse(canonicalBytes);
        var basis = ContentSourceLocalMapPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!canonicalBytes.AsSpan().SequenceEqual(expected.bytes))
            throw new InvalidOperationException("Expanded source catalog is not canonical.");
        return expected;
    }

    private static ContentSourceIndependentUnit Unit(string id, string location, int capability, int toe) =>
        new(id, "commonwealth", location, capability, toe, "60.41", new(ContentOriginKind.SourceDerived,
            [new RuleReference(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.41"),
             new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "oa-british-p11"),
             new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "4.46a")]));
}
