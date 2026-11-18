using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Content;

/// <summary>Fixed source definitions for independently deployed 1AR battalions.</summary>
internal sealed class ContentSourceAxisFreeArtilleryPackArtifact
{
    internal const string FormatId = "sandtable.source-axis-free-artillery.v1";
    internal const string ContentHash = "sha256:aa31213766422d8a634ce09e92d1cdaaa97f3a3674785cc2b83e196294415852"; // RUL-GRA-28: derived from CampaignSourceAxisSupplementalForces.ContentHash.
    private readonly byte[] bytes;
    private ContentSourceAxisFreeArtilleryPackArtifact(ContentSourceExpandedIndependentPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceExpandedIndependentPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static ContentOrigin Origin => new(ContentOriginKind.SourceDerived,
        [new RuleReference(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.31"),
         new RuleReference(Cna1979GrazianiSourceFacts.AxisChartsSourceId, "oa-unassigned-guns-p16"),
         new RuleReference(Cna1979GrazianiSourceFacts.AxisChartsSourceId, "4.46b")]);

    internal static ContentSourceAxisFreeArtilleryPackArtifact Create(ContentSourceExpandedIndependentPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceExpandedIndependentPackArtifact.ContentHash)
            throw new InvalidOperationException("Axis free artillery requires pinned source content.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash());
            writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("axisChartsSourceHash", "sha256:9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca");
            writer.WriteString("organization", "independent-no-formal-headquarters");
            writer.WriteStartArray("units");
            foreach (var unit in CampaignSourceAxisFreeArtillery.Units)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", unit.ElementId);
                writer.WriteString("sourceLabel", unit.SourceLabel);
                writer.WriteString("sideId", "axis");
                writer.WriteNumber("capabilityPoints", unit.CapabilityPoints);
                writer.WriteNumber("stacking", unit.Stacking);
                writer.WriteString("componentId", unit.ElementId + "." + unit.ComponentSuffix);
                writer.WriteString("componentClassId", Cna1979GrazianiSourceFacts.ArtilleryComponentClassId);
                writer.WriteNumber("toe", unit.Toe);
                writer.WriteNull("parentFormationId");
                writer.WriteNull("mobilityId");
                writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        var result = new ContentSourceAxisFreeArtilleryPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash)
            throw new InvalidOperationException("Axis free artillery content differs from its pinned profile.");
        return result;
    }
    internal static ContentSourceAxisFreeArtilleryPackArtifact Read(byte[] canonicalBytes)
    {
        ArgumentNullException.ThrowIfNull(canonicalBytes);
        using var document = JsonDocument.Parse(canonicalBytes);
        var basis = ContentSourceExpandedIndependentPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!canonicalBytes.AsSpan().SequenceEqual(expected.bytes))
            throw new InvalidOperationException("Axis free artillery content is not canonical.");
        return expected;
    }
}
