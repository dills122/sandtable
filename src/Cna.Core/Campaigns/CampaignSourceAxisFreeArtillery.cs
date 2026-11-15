using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Campaigns;

/// <summary>Restricted source-backed setup placements for the four printed 1AR battalions.
/// Other free Axis setup, source duplication, full map and total world remain unresolved.</summary>
internal sealed class CampaignSourceAxisFreeArtillery
{
    internal const string PolicyId = "graziani.axis-free-1ar-setup.v1";
    private static readonly IReadOnlyList<string> Sites = Array.AsReadOnly<string>(["c4407", "c4607"]);
    private static readonly IReadOnlyList<CampaignSourceFreeArtilleryUnit> UnitData = Array.AsReadOnly(new[]
    {
        new CampaignSourceFreeArtilleryUnit("axis-1-1ar-artillery", "1/1AR", 1, "75-27-gun", 3, 15),
        new CampaignSourceFreeArtilleryUnit("axis-2-1ar-artillery", "2/1AR", 1, "75-18-gun-howitzer", 3, 15),
        new CampaignSourceFreeArtilleryUnit("axis-3-1ar-artillery", "3/1AR", 1, "105-28-gun", 3, 15),
        new CampaignSourceFreeArtilleryUnit("axis-4-1ar-artillery", "4/1AR", 1, "100-17-howitzer", 3, 15),
    });
    private readonly ReadOnlyCollection<CampaignSourceFreeArtilleryPlacement> placements;
    private CampaignSourceAxisFreeArtillery(string battlefieldHash, IEnumerable<CampaignSourceFreeArtilleryPlacement> placements)
    {
        BattlefieldHash = battlefieldHash;
        this.placements = Array.AsReadOnly(placements.OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray());
    }
    internal static IReadOnlyList<CampaignSourceFreeArtilleryUnit> Units => UnitData;
    internal string BattlefieldHash { get; }
    internal IReadOnlyList<CampaignSourceFreeArtilleryPlacement> Placements => placements;
    internal IReadOnlyList<string> PendingIds => Array.AsReadOnly(UnitData.Select(unit => unit.ElementId)
        .Except(placements.Select(placement => placement.ElementId), StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());

    internal static CampaignSourceAxisFreeArtillery Create(CampaignPregameSourceState state, CampaignSourceBattlefield battlefield)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(battlefield);
        if (state.Source.Session.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash
            || UnitData.Any(unit => battlefield.Entries.Any(entry => entry.ElementId == unit.ElementId)
                || battlefield.Pending.Any(group => group.ElementIds.Contains(unit.ElementId, StringComparer.Ordinal))))
            throw new InvalidOperationException("Source 1AR setup conflicts with retained battlefield or content.");
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
                || (placed is null ? entry.AuthorityStatus != "source-1ar-unplaced"
                    : entry.AuthorityStatus is not ("source-1ar-placed" or "source-1ar-active"))
                || entry.LocationId != placed?.LocationId) return [];
        }
        return Array.AsReadOnly(Sites.Where(site =>
        {
            var occupants = battlefield.Entries.Where(entry => entry.LocationId == site).ToArray();
            return occupants.All(entry => entry.SideId == "axis" && entry.AuthorityStatus is "source-1ar-placed" or "source-1ar-active"
                && placements.Any(placement => placement.ElementId == entry.ElementId && placement.LocationId == site))
                && occupants.Length < 2;
        }).ToArray());
    }

    internal CampaignSourceAxisFreeArtillery Place(string elementId, string locationId, CampaignSourceBattlefield battlefield)
    {
        if (!ValidDestinations(elementId, battlefield).Contains(locationId, StringComparer.Ordinal))
            throw new InvalidOperationException("Source 1AR placement is unavailable.");
        return new(BattlefieldHash, placements.Append(new(elementId, locationId)));
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("initialBattlefieldHash", BattlefieldHash);
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("mapSourceHash", "sha256:8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030");
            writer.WriteString("source", "spi-1979-scenarios:60.31");
            writer.WriteString("counterSource", "spi-1979-air-logistics:counter-section-2-p40");
            writer.WriteString("unitSource", "spi-1979-axis-charts:oa-unassigned-guns-p16");
            writer.WriteString("organization", "independent-no-formal-headquarters");
            writer.WriteString("sourceDuplicateStatus", "4-1ar-and-xxi-corps-repeated-xxvi-relationship-unresolved");
            writer.WriteString("siteCoverage", "restricted-not-all-libya");
            writer.WriteBoolean("freeAxisRosterComplete", false);
            writer.WriteNumber("curatedCombatStackLimit", 2);
            writer.WriteStartArray("sites"); foreach (var site in Sites) writer.WriteStringValue(site); writer.WriteEndArray();
            writer.WriteStartArray("units");
            foreach (var unit in UnitData)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", unit.ElementId);
                writer.WriteString("sourceLabel", unit.SourceLabel);
                writer.WriteNumber("stacking", unit.Stacking);
                writer.WriteString("componentId", unit.ElementId + "." + unit.ComponentSuffix);
                writer.WriteNumber("toe", unit.Toe);
                writer.WriteNumber("capabilityPoints", unit.CapabilityPoints);
                var placed = placements.SingleOrDefault(value => value.ElementId == unit.ElementId);
                if (placed is null) writer.WriteNull("locationId"); else writer.WriteString("locationId", placed.LocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
    internal string InitialHash() => new CampaignSourceAxisFreeArtillery(BattlefieldHash, []).Hash();
}

internal sealed record CampaignSourceFreeArtilleryUnit(string ElementId, string SourceLabel, int Stacking,
    string ComponentSuffix, int Toe, int CapabilityPoints);
internal sealed record CampaignSourceFreeArtilleryPlacement(string ElementId, string LocationId);
