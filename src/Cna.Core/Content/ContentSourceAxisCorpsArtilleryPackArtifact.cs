using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Content;

internal sealed class ContentSourceAxisCorpsArtilleryPackArtifact
{
    internal const string FormatId = "sandtable.source-axis-corps-artillery.v1";
    internal const string ContentHash = "sha256:05aafa2092adebf7d50d7d3f02d1d1dd68ccf75098390ba03e68a0e81edf3fb7"; // RUL-GRA-26: derived from CampaignSourceAxisSupplementalForces.ContentHash.
    private readonly byte[] bytes;
    private ContentSourceAxisCorpsArtilleryPackArtifact(ContentSourceAxisFreeArtilleryPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceAxisFreeArtilleryPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static ContentOrigin Origin => new(ContentOriginKind.SourceDerived,
        [new RuleReference(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.31"),
         new RuleReference(Cna1979GrazianiSourceFacts.ErrataSourceId, "60.31"),
         new RuleReference(Cna1979GrazianiSourceFacts.AxisChartsSourceId, "oa-unassigned-guns-p16"),
         new RuleReference(Cna1979GrazianiSourceFacts.LandRulesSourceId, "32.83")]);
    internal static ContentSourceAxisCorpsArtilleryPackArtifact Create(ContentSourceAxisFreeArtilleryPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceAxisFreeArtilleryPackArtifact.ContentHash)
            throw new InvalidOperationException("Corps artillery requires pinned predecessor content.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash()); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("axisChartsSourceHash", "sha256:9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca");
            writer.WriteString("errataSourceHash", "sha256:5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb");
            writer.WriteString("landSourceHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("xxviIdentity", "provisional-unique-counter-inference");
            writer.WriteString("landOnlyPolicy", "kk-light-aa-retained-heavy-aa-removed");
            writer.WriteStartArray("units");
            foreach (var unit in CampaignSourceAxisCorpsArtillery.Units)
            {
                writer.WriteStartObject(); writer.WriteString("elementId", unit.ElementId);
                writer.WriteString("sideId", "axis"); writer.WriteString("sourceLabel", unit.SourceLabel);
                writer.WriteNumber("capabilityPoints", 15); writer.WriteNumber("stacking", 1);
                writer.WriteNull("parentFormationId"); writer.WriteNull("mobilityId"); writer.WriteStartArray("components");
                foreach (var component in unit.Components)
                {
                    writer.WriteStartObject(); writer.WriteString("componentId", unit.ElementId + "." + component.Suffix);
                    writer.WriteString("componentClassId", component.Suffix == "light-aa"
                        ? Cna1979GrazianiSourceFacts.AntiAirComponentClassId : Cna1979GrazianiSourceFacts.ArtilleryComponentClassId);
                    writer.WriteNumber("toe", component.Toe); writer.WriteEndObject();
                }
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        var result = new ContentSourceAxisCorpsArtilleryPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash)
            throw new InvalidOperationException("Source corps artillery content differs from its pinned profile.");
        return result;
    }
    internal static ContentSourceAxisCorpsArtilleryPackArtifact Read(byte[] canonicalBytes)
    {
        ArgumentNullException.ThrowIfNull(canonicalBytes);
        using var document = JsonDocument.Parse(canonicalBytes);
        var basis = ContentSourceAxisFreeArtilleryPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!canonicalBytes.AsSpan().SequenceEqual(expected.bytes))
            throw new InvalidOperationException("Source corps artillery content is not canonical.");
        return expected;
    }
}
