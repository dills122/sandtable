using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;

namespace Cna.Core.Content;

// Six independently represented source counters. Two AA units are removed
// by the Land-only abstraction; the 8+ motor MG remains unresolved.
internal sealed class ContentSourceAxisGarrisonRemainderPackArtifact
{
    internal const string FormatId = "sandtable.source-axis-garrison-remainder.v1";
    internal const string ContentHash = "sha256:538aa3b9e67877cc786680bfd6477e75a0ed8ebb5c05bd97c443d07dbeb98661"; // RUL-GRA-26: derived from CampaignSourceAxisSupplementalForces.ContentHash.
    private readonly byte[] bytes;
    private ContentSourceAxisGarrisonRemainderPackArtifact(ContentSourceAxisMinorGarrisonsPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceAxisMinorGarrisonsPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal static IReadOnlyList<(string Id, string Location, int Toe, int Stack, int CapabilityPoints)> Units { get; } =
        Array.AsReadOnly(new[]
        {
            ("giarabub-infantry", "c1014", 1, 0, 10),
            ("giarabub-machine-gun", "c1014", 4, 1, 8),
            ("giarabub-antitank", "c1014", 2, 0, 15),
            ("giarabub-artillery", "c1014", 1, 0, 15),
            ("soluch-antitank-company", "a4130", 2, 0, 15),
            ("soluch-artillery-regiment", "a4130", 12, 1, 15),
        });
    internal static IReadOnlyList<string> RemovedAntiAirIds { get; } =
        Array.AsReadOnly(new[] { "benina-aa-battery", "giarabub-light-aa" });
    internal static int StackAt(string id, string locationId)
    {
        var unit = Units.Single(value => value.Id == id);
        return unit.Location == locationId ? 0 : unit.Stack;
    }
    internal static ContentElementCombatFacts Facts(string id) =>
        Cna1979GrazianiSourceFacts.GiarabubCombatFacts.Concat(Cna1979GrazianiSourceFacts.AxisMinorGarrisonCombatFacts)
            .Single(value => value.ElementId == id);
    internal static ContentOrigin Origin(string id) => Cna1979GrazianiSourceFacts
        .CreateGiarabubToeSeeds(Cna1979GrazianiCairoSourceExcerpt.ScenarioId)
        .Concat(Cna1979GrazianiSourceFacts.CreateAxisMinorGarrisonToeSeeds(Cna1979GrazianiCairoSourceExcerpt.ScenarioId))
        .Single(value => value.ElementId == id).InitialComponentToes.Single().Origin;
    internal static ContentSourceAxisGarrisonRemainderPackArtifact Create(ContentSourceAxisMinorGarrisonsPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (ContentSourceAxisMinorGarrisonsPackArtifact.Read(basis.CanonicalBytes).Identity != basis.Identity)
            throw new InvalidOperationException("Garrison successor requires canonical minor-garrison content.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Identity.Hash); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("sourcePolicy", CampaignSourceAxisGarrisonForces.PolicyId);
            writer.WriteString("sourceHash", CampaignSourceAxisGarrisonForces.ContentHash);
            writer.WriteString("sources", "scenario:60.31;axis-charts:oa-giarabub-p14,oa-minor-garrisons-p14,4.46b;air:counter-section-2-p40;land:9.16a");
            writer.WriteString("antiAirTreatment", "land:32.83(1)-italian-uu-removed-from-play");
            writer.WriteStartArray("removedAntiAirUnits");
            foreach (var id in RemovedAntiAirIds.Order(StringComparer.Ordinal)) writer.WriteStringValue(id);
            writer.WriteEndArray();
            writer.WriteStartArray("units");
            foreach (var unit in Units.OrderBy(value => value.Id, StringComparer.Ordinal))
            {
                var facts = Facts(unit.Id); var component = facts.Components.Single();
                writer.WriteStartObject(); writer.WriteString("elementId", unit.Id); writer.WriteString("sideId", "axis");
                writer.WriteString("combatClassificationId", facts.CombatClassificationId);
                writer.WriteString("locationId", unit.Location); writer.WriteNumber("toe", unit.Toe);
                writer.WriteNumber("capabilityPoints", unit.CapabilityPoints); writer.WriteNumber("printedStacking", unit.Stack);
                writer.WriteNumber("assignedLocationStacking", StackAt(unit.Id, unit.Location));
                writer.WriteString("stackingRule", "land:9.16a-assigned-city-or-village-only");
                writer.WriteString("componentId", component.ComponentId); writer.WriteString("componentClassId", component.ComponentClassId);
                writer.WriteNumber("maximumToe", component.MaximumToe); writer.WriteNumber("defensiveCloseAssault", component.DefensiveCloseAssaultRating);
                writer.WriteBoolean("defensiveRatingParenthesized", component.DefensiveRatingParenthesized);
                if (component.OffensiveCloseAssaultRating is int offensive) writer.WriteNumber("offensiveCloseAssault", offensive); else writer.WriteNull("offensiveCloseAssault");
                writer.WriteBoolean("offensiveRatingParenthesized", component.OffensiveRatingParenthesized);
                if (component.BarrageRating is int barrage) writer.WriteNumber("barrage", barrage); else writer.WriteNull("barrage");
                writer.WriteBoolean("barrageRatingParenthesized", component.BarrageRatingParenthesized);
                writer.WritePropertyName("weaponSourceProfile"); JsonSerializer.Serialize(writer, component.WeaponSourceProfile);
                writer.WriteNull("parentFormationId"); writer.WriteNull("mobilityId"); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        var result = new ContentSourceAxisGarrisonRemainderPackArtifact(basis, stream.ToArray());
        if (result.Identity.Hash != ContentHash)
            throw new InvalidOperationException("Garrison remainder content differs from its pinned identity.");
        return result;
    }
    internal static ContentSourceAxisGarrisonRemainderPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceAxisMinorGarrisonsPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(expected.bytes)) throw new InvalidOperationException("Garrison successor is not canonical.");
        return expected;
    }
}
