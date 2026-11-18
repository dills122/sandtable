using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Content;

/// <summary>Fixed source definitions for three freely placed CW gun counters.</summary>
internal sealed class ContentSourceCwFreeGunsPackArtifact
{
    internal const string FormatId = "sandtable.source-cw-free-guns.v1";
    internal const string ContentHash = "sha256:7a54581b0af8b521d5194ffbbd41b0009e93a3cd4c39085bf8e54ad82c8f09c9"; // RUL-GRA-28: derived from CampaignSourceAxisSupplementalForces.ContentHash.
    private readonly byte[] bytes;
    private ContentSourceCwFreeGunsPackArtifact(ContentSourceCwPriorityPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceCwPriorityPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal string Hash() => Identity.Hash;
    internal static ContentOrigin Origin(string id) => new(ContentOriginKind.SourceDerived,
        [new RuleReference(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.41"),
         new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId,
             id == "cw-8-medium-artillery" ? "oa-british-artillery-p12" : "oa-unassigned-antitank-p10"),
         new RuleReference(Cna1979GrazianiSourceFacts.CommonwealthChartsSourceId, "4.46a")]);
    internal static ContentSourceCwFreeGunsPackArtifact Create(ContentSourceCwPriorityPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceCwPriorityPackArtifact.ContentHash)
            throw new InvalidOperationException("CW free guns require pinned normalized source content.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash()); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("cwChartsHash", "sha256:f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981");
            writer.WriteString("sitePolicy", CampaignSourceCwFreeGuns.PolicyId);
            writer.WriteString("futureAustralianPlacement", "unresolved-must-respect-reserved-stack");
            writer.WriteStartArray("units");
            foreach (var unit in CampaignSourceCwFreeGuns.Units)
            {
                writer.WriteStartObject(); writer.WriteString("elementId", unit.ElementId); writer.WriteString("sideId", "commonwealth");
                writer.WriteNumber("capabilityPoints", unit.CapabilityPoints); writer.WriteNumber("stacking", unit.Stacking);
                writer.WriteString("componentId", unit.ElementId + "." + unit.ComponentSuffix);
                writer.WriteString("componentClassId", unit.ComponentClassId); writer.WriteNumber("toe", unit.Toe);
                writer.WriteNull("parentFormationId"); writer.WriteNull("mobilityId"); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        var result = new ContentSourceCwFreeGunsPackArtifact(basis, stream.ToArray());
        if (result.Hash() != ContentHash)
            throw new InvalidOperationException("CW free-gun content differs from its pinned profile.");
        return result;
    }
    internal static ContentSourceCwFreeGunsPackArtifact Read(byte[] canonicalBytes)
    {
        ArgumentNullException.ThrowIfNull(canonicalBytes);
        using var document = JsonDocument.Parse(canonicalBytes);
        var basis = ContentSourceCwPriorityPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!canonicalBytes.AsSpan().SequenceEqual(expected.bytes))
            throw new InvalidOperationException("CW free-gun source content is not canonical.");
        return expected;
    }
}
