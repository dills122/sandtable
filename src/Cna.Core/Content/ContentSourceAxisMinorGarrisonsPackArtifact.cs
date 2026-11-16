using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Content;

// Native content successor for independently represented source counters.
internal sealed class ContentSourceAxisMinorGarrisonsPackArtifact
{
    internal const string FormatId = "sandtable.source-axis-minor-garrisons.v1";
    private readonly byte[] bytes;
    private ContentSourceAxisMinorGarrisonsPackArtifact(ContentSourceCwFreeGunsPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }
    internal ContentSourceCwFreeGunsPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();
    internal static IReadOnlyList<(string Id, string Location, int Toe, int Stack)> Units { get; } =
        Array.AsReadOnly(new[] { ("scheferzen-garrison", "c3419", 1, 0), ("fort-maddalena-garrison", "c3019", 2, 1),
            ("el-grein-garrison", "c1715", 1, 0), ("mechili-garrison", "b4921", 2, 1) });
    // Land 9.16(a): G-marked counters stack freely only at their assigned site.
    internal static int StackAt(string id, string locationId)
    {
        var unit = Units.Single(value => value.Id == id);
        return locationId == unit.Location ? 0 : unit.Stack;
    }
    internal static ContentSourceAxisMinorGarrisonsPackArtifact Create(ContentSourceCwFreeGunsPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceCwFreeGunsPackArtifact.ContentHash)
            throw new InvalidOperationException("Minor garrisons require pinned native source content.");
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash()); writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("sourcePolicy", CampaignSourceAxisGarrisonForces.PolicyId);
            writer.WriteString("sourceHash", CampaignSourceAxisGarrisonForces.ContentHash);
            writer.WriteString("sources", "scenario:60.31;axis-charts:oa-minor-garrisons-p14,4.46b;air:counter-section-2-p40");
            writer.WriteStartArray("units");
            foreach (var unit in Units.OrderBy(value => value.Id, StringComparer.Ordinal))
            {
                var component = Cna1979GrazianiSourceFacts.AxisMinorGarrisonCombatFacts
                    .Single(value => value.ElementId == unit.Id).Components.Single();
                writer.WriteStartObject(); writer.WriteString("elementId", unit.Id); writer.WriteString("sideId", "axis");
                writer.WriteString("locationId", unit.Location); writer.WriteNumber("toe", unit.Toe);
                writer.WriteNumber("capabilityPoints", 15); writer.WriteNumber("printedStacking", unit.Stack);
                writer.WriteNumber("assignedLocationStacking", StackAt(unit.Id, unit.Location));
                writer.WriteString("stackingRule", "land:9.16a-assigned-city-or-village-only");
                writer.WriteString("componentId", component.ComponentId); writer.WriteString("componentClassId", component.ComponentClassId);
                writer.WriteNumber("maximumToe", component.MaximumToe);
                writer.WriteNumber("defensiveCloseAssault", component.DefensiveCloseAssaultRating);
                if (component.OffensiveCloseAssaultRating is int offensive) writer.WriteNumber("offensiveCloseAssault", offensive);
                else writer.WriteNull("offensiveCloseAssault");
                if (component.BarrageRating is int barrage) writer.WriteNumber("barrage", barrage);
                else writer.WriteNull("barrage");
                writer.WriteNull("parentFormationId");
                writer.WriteNull("mobilityId"); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return new(basis, stream.ToArray());
    }
    internal static ContentSourceAxisMinorGarrisonsPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceCwFreeGunsPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var result = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(result.bytes)) throw new InvalidOperationException("Minor garrison content is not canonical.");
        return result;
    }
    internal static ContentOrigin Origin(string id) => Cna1979GrazianiSourceFacts
        .CreateAxisMinorGarrisonToeSeeds(Cna1979GrazianiCairoSourceExcerpt.ScenarioId)
        .Single(value => value.ElementId == id).InitialComponentToes.Single().Origin;
}
