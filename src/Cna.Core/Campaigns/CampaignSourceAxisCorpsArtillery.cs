using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

internal sealed record CampaignSourceCorpsGun(string Suffix, int Toe);
internal sealed record CampaignSourceCorpsArtilleryUnit(string ElementId, string SourceLabel,
    IReadOnlyList<CampaignSourceCorpsGun> Components);
internal sealed record CampaignSourceCorpsArtilleryPlacement(string ElementId, string LocationId);

/// <summary>Source-backed, restricted XXI/XXVI artillery setup; not a complete Axis roster.</summary>
internal sealed class CampaignSourceAxisCorpsArtillery
{
    internal const string PolicyId = "graziani.axis-corps-artillery-setup.v1";
    private static readonly IReadOnlyList<string> Sites = Array.AsReadOnly<string>(["c4307", "c4308"]);
    private static readonly IReadOnlyList<CampaignSourceCorpsArtilleryUnit> UnitData = Array.AsReadOnly(new[]
    {
        new CampaignSourceCorpsArtilleryUnit("axis-xxi-corps-artillery", "XXI Cp", Array.AsReadOnly(new[]
        { new CampaignSourceCorpsGun("75-27-gun", 3), new CampaignSourceCorpsGun("100-17-howitzer", 6), new CampaignSourceCorpsGun("light-aa", 3) })),
        new CampaignSourceCorpsArtilleryUnit("axis-xxvi-corps-artillery", "XXVI Cp", Array.AsReadOnly(new[]
        { new CampaignSourceCorpsGun("75-27-gun", 9), new CampaignSourceCorpsGun("light-aa", 3) })),
    });
    private readonly ReadOnlyCollection<CampaignSourceCorpsArtilleryPlacement> placements;
    private CampaignSourceAxisCorpsArtillery(string battlefieldHash, IEnumerable<CampaignSourceCorpsArtilleryPlacement> placements)
    { BattlefieldHash = battlefieldHash; this.placements = Array.AsReadOnly(placements.OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray()); }
    internal static IReadOnlyList<CampaignSourceCorpsArtilleryUnit> Units => UnitData;
    internal string BattlefieldHash { get; }
    internal IReadOnlyList<CampaignSourceCorpsArtilleryPlacement> Placements => placements;
    internal IReadOnlyList<string> PendingIds => Array.AsReadOnly(UnitData.Select(value => value.ElementId)
        .Except(placements.Select(value => value.ElementId), StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
    internal static CampaignSourceAxisCorpsArtillery Create(CampaignPregameSourceState state, CampaignSourceBattlefield battlefield)
    {
        ArgumentNullException.ThrowIfNull(state); ArgumentNullException.ThrowIfNull(battlefield);
        if (state.Source.Session.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash
            || UnitData.Any(unit => battlefield.Entries.Any(entry => entry.ElementId == unit.ElementId)
                || battlefield.Pending.Any(group => group.ElementIds.Contains(unit.ElementId, StringComparer.Ordinal))))
            throw new InvalidOperationException("Source corps setup conflicts with retained battlefield or content.");
        return new(battlefield.Hash(), []);
    }
    internal IReadOnlyList<string> ValidDestinations(string elementId, CampaignSourceBattlefield battlefield)
    {
        ArgumentNullException.ThrowIfNull(battlefield);
        if (!PendingIds.Contains(elementId, StringComparer.Ordinal)) return [];
        foreach (var unit in UnitData)
        {
            var entry = battlefield.Entries.SingleOrDefault(value => value.ElementId == unit.ElementId);
            var placed = placements.SingleOrDefault(value => value.ElementId == unit.ElementId);
            if (entry is null || entry.SideId != "axis"
                || (placed is null ? entry.AuthorityStatus != "source-corps-artillery-unplaced"
                    : entry.AuthorityStatus is not ("source-corps-artillery-placed" or "source-corps-artillery-active"))
                || entry.LocationId != placed?.LocationId) return [];
        }
        // One counter per restricted, source-verified clear hex; all other
        // known combat occupants block. Supply counters have zero combat stack.
        return Array.AsReadOnly(Sites.Where(site => battlefield.Entries.All(entry => entry.LocationId != site)).ToArray());
    }
    internal CampaignSourceAxisCorpsArtillery Place(string elementId, string locationId, CampaignSourceBattlefield battlefield)
    {
        if (!ValidDestinations(elementId, battlefield).Contains(locationId, StringComparer.Ordinal))
            throw new InvalidOperationException("Source corps artillery placement is unavailable.");
        return new(BattlefieldHash, placements.Append(new(elementId, locationId)));
    }
    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", PolicyId); writer.WriteString("initialBattlefieldHash", BattlefieldHash);
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("axisChartsSourceHash", "sha256:9bbd997f5a50bc38b2c745d4cd951b14fcd7cf4467c7cf2d599f59695a6085ca");
            writer.WriteString("airSourceHash", "sha256:9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4");
            writer.WriteString("landSourceHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("errataSourceHash", "sha256:5db539e1fac4aea3ef152370fb732f1938307fe4b2d47ad93eb605db0a09f0fb");
            writer.WriteString("mapSourceHash", "sha256:8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030");
            writer.WriteString("xxiIdentity", "errata-60.31-one-xxi-corps-artillery");
            writer.WriteString("xxviIdentity", "provisional-unique-xxvi-corps-artillery-counter");
            writer.WriteString("sourceRepeated4-1ar", "unresolved");
            writer.WriteString("landOnlyPolicy", "32.83-kk-removes-heavy-aa-only-light-aa-retained");
            writer.WriteBoolean("freeAxisRosterComplete", false);
            writer.WriteString("siteCoverage", "restricted-not-all-libya");
            writer.WriteStartArray("sites"); foreach (var site in Sites) writer.WriteStringValue(site); writer.WriteEndArray();
            writer.WriteStartArray("units");
            foreach (var unit in UnitData)
            {
                writer.WriteStartObject(); writer.WriteString("elementId", unit.ElementId); writer.WriteString("sourceLabel", unit.SourceLabel);
                writer.WriteString("unitCode", "kk"); writer.WriteNumber("capabilityPoints", 15); writer.WriteNumber("stacking", 1);
                var placed = placements.SingleOrDefault(value => value.ElementId == unit.ElementId);
                if (placed is null) writer.WriteNull("locationId"); else writer.WriteString("locationId", placed.LocationId);
                writer.WriteStartArray("components");
                foreach (var component in unit.Components)
                { writer.WriteStartObject(); writer.WriteString("componentId", unit.ElementId + "." + component.Suffix); writer.WriteNumber("toe", component.Toe); writer.WriteEndObject(); }
                writer.WriteEndArray(); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
    internal string InitialHash() => new CampaignSourceAxisCorpsArtillery(BattlefieldHash, []).Hash();
}
