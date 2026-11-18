using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

// Scenario counters only; neither optional initial cargo nor vehicle breakdown is inferred.
internal sealed class ContentSourceCwFrontlineArtifact
{
    internal const string FormatId = "sandtable.source-cw-frontline.v1";
    internal const string ContentHash = "sha256:28dd214ff5b94535f4284cfce9ec2a030fe7b6e773051dc809085608830a792e"; // RUL-GRA-28: derived from CampaignSourceAxisSupplementalForces.ContentHash.
    private readonly byte[] bytes;
    private ContentSourceCwFrontlineArtifact(ContentSourceGiarabubMotorMachineGunArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceGiarabubMotorMachineGunArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal static IReadOnlyList<(string Id, string Location, int Toe, int CapabilityPoints, string PoolId, int PoolPoints)> Units { get; } =
        Array.AsReadOnly(new[] { ("1-rtr", "c3520", 7, 25, "cw-c3520-attached", 5),
            ("11-hussars", "c3020", 6, 45, "cw-c3020-attached", 3) });
    internal static ContentElementCombatFacts Facts(string id) => Cna1979GrazianiSourceFacts.CommonwealthInitialCombatFacts.Single(value => value.ElementId == id);
    internal static ContentOrigin Origin(string id) => Cna1979GrazianiSourceFacts
        .CreateCommonwealthInitialToeSeeds(Cna1979GrazianiCairoSourceExcerpt.ScenarioId)
        .Single(value => value.ElementId == id).InitialComponentToes.Single().Origin;
    internal static ContentSourceCwFrontlineArtifact Create(ContentSourceGiarabubMotorMachineGunArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        // The immutable predecessor can only be created or read through its
        // pinned canonical factory; untrusted bytes are still checked by Read.
        if (basis.Identity.Hash != ContentSourceGiarabubMotorMachineGunArtifact.ContentHash)
            throw new InvalidOperationException("CW frontline requires canonical predecessor.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Identity.Hash); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("sourceHash", CampaignSourceCommonwealthSupplementalForces.ContentHash);
            writer.WriteString("sources", "scenario:59.63a,59.66b,60.41;cw-charts:oa-7-armored-p6,4.46a,4.47;counter-section-7");
            writer.WriteStartArray("units");
            foreach (var unit in Units.OrderBy(value => value.Id, StringComparer.Ordinal))
            {
                var facts = Facts(unit.Id); var component = facts.Components.Single();
                writer.WriteStartObject(); writer.WriteString("elementId", unit.Id); writer.WriteString("sideId", "commonwealth");
                writer.WriteString("locationId", unit.Location); writer.WriteNumber("toe", unit.Toe);
                writer.WriteNumber("capabilityPoints", unit.CapabilityPoints); writer.WriteNumber("printedStacking", 1);
                if (unit.Id == "11-hussars") writer.WriteNumber("chartArmorRating", 1); else writer.WriteNull("chartArmorRating");
                writer.WriteString("existingAttachedPoolId", unit.PoolId); writer.WriteNumber("existingAttachedPoolPoints", unit.PoolPoints);
                writer.WriteNull("initialCargo"); writer.WriteNull("parentFormationId");
                writer.WriteString("combatClassificationId", facts.CombatClassificationId);
                writer.WriteString("componentId", component.ComponentId); writer.WriteString("componentClassId", component.ComponentClassId);
                writer.WriteNumber("maximumToe", component.MaximumToe); writer.WriteNumber("defensiveCloseAssault", component.DefensiveCloseAssaultRating);
                if (component.OffensiveCloseAssaultRating is int offensive) writer.WriteNumber("offensiveCloseAssault", offensive); else writer.WriteNull("offensiveCloseAssault");
                writer.WritePropertyName("weaponSourceProfile"); JsonSerializer.Serialize(writer, component.WeaponSourceProfile);
                writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        var result = new ContentSourceCwFrontlineArtifact(basis, stream.ToArray());
        if (result.Identity.Hash != ContentHash)
            throw new InvalidOperationException("CW frontline content differs from its pinned identity.");
        return result;
    }
    internal static ContentSourceCwFrontlineArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceGiarabubMotorMachineGunArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var result = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(result.bytes)) throw new InvalidOperationException("CW frontline content is not canonical.");
        return result;
    }
}
