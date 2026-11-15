using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

internal sealed record CampaignSourceCwFreeGunUnit(string ElementId, string ComponentSuffix, int Toe,
    int CapabilityPoints, string ComponentClassId, int Stacking);
internal sealed record CampaignSourceCwFreeGunPlacement(string ElementId, string LocationId);

/// <summary>Player placement of three scenario-deployed CW guns at a restricted Cairo subset.</summary>
internal sealed class CampaignSourceCwFreeGuns
{
    internal const string PolicyId = "graziani.cw-free-guns-cairo-setup.v1";
    private static readonly IReadOnlyList<string> Sites = Array.AsReadOnly<string>(["e1730", "e1830", "e1930"]);
    private static readonly IReadOnlyList<CampaignSourceCwFreeGunUnit> UnitData = Array.AsReadOnly(new[]
    {
        new CampaignSourceCwFreeGunUnit("cw-8-medium-artillery", "6-inch-howitzer", 6, 20, Cna1979GrazianiSourceFacts.ArtilleryComponentClassId, 1),
        new CampaignSourceCwFreeGunUnit("cw-65-antitank", "2-pounder", 6, 15, Cna1979GrazianiSourceFacts.AntiTankComponentClassId, 1),
        new CampaignSourceCwFreeGunUnit("cw-149-antitank", "2-pounder", 8, 15, Cna1979GrazianiSourceFacts.AntiTankComponentClassId, 1),
    });
    private readonly ReadOnlyCollection<CampaignSourceCwFreeGunPlacement> placements;
    private CampaignSourceCwFreeGuns(string battlefieldHash, IEnumerable<CampaignSourceCwFreeGunPlacement> placements)
    { BattlefieldHash = battlefieldHash; this.placements = Array.AsReadOnly(placements.OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray()); }
    internal static IReadOnlyList<CampaignSourceCwFreeGunUnit> Units => UnitData;
    internal string BattlefieldHash { get; }
    internal IReadOnlyList<CampaignSourceCwFreeGunPlacement> Placements => placements;
    internal IReadOnlyList<string> PendingIds => Array.AsReadOnly(UnitData.Select(value => value.ElementId)
        .Except(placements.Select(value => value.ElementId), StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray());
    internal static CampaignSourceCwFreeGuns Create(CampaignSourceAuthorityState state, CampaignSourceBattlefield battlefield)
    {
        ArgumentNullException.ThrowIfNull(state); ArgumentNullException.ThrowIfNull(battlefield);
        if (state.Pregame.Source.Session.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash
            || state.CairoSetup is null || state.CairoSetup.PendingIds.Count != 0 || state.CairoWorld is null
            || UnitData.Any(unit => battlefield.Entries.Any(entry => entry.ElementId == unit.ElementId)
                || battlefield.Pending.Any(group => group.ElementIds.Contains(unit.ElementId, StringComparer.Ordinal))))
            throw new InvalidOperationException("CW free-gun setup conflicts with retained source world.");
        return new(battlefield.Hash(), []);
    }
    internal IReadOnlyList<string> ValidDestinations(string elementId, CampaignSourceBattlefield battlefield)
    {
        ArgumentNullException.ThrowIfNull(battlefield);
        if (!IsConsistent(battlefield)) return [];
        if (!PendingIds.Contains(elementId, StringComparer.Ordinal)) return [];
        return Array.AsReadOnly(Sites.Where(site => SiteSafe(battlefield, site)
            && battlefield.Entries.Count(entry => entry.LocationId == site) < 8).ToArray());
    }
    internal void Validate(CampaignSourceAuthorityState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var battlefield = state.Battlefield ?? throw new InvalidOperationException("Source battlefield missing.");
        if (!IsConsistent(battlefield)) throw new InvalidOperationException("CW free-gun source occupancy is inconsistent.");
    }
    private bool IsConsistent(CampaignSourceBattlefield battlefield)
    {
        foreach (var unit in UnitData)
        {
            var entry = battlefield.Entries.SingleOrDefault(value => value.ElementId == unit.ElementId);
            var placed = placements.SingleOrDefault(value => value.ElementId == unit.ElementId);
            if (entry is null || entry.SideId != "commonwealth"
                || (placed is null ? entry.AuthorityStatus != "source-cw-free-gun-unplaced"
                    : entry.AuthorityStatus is not ("source-cw-free-gun-placed" or "source-cw-free-gun-active"))
                || entry.LocationId != placed?.LocationId) return false;
        }
        return placements.Select(value => value.LocationId).Distinct(StringComparer.Ordinal).All(site => SiteSafe(battlefield, site));
    }
    private bool SiteSafe(CampaignSourceBattlefield battlefield, string site)
    {
        var occupants = battlefield.Entries.Where(entry => entry.LocationId == site).ToArray();
        return occupants.Length <= 8 && occupants.All(entry => entry.SideId == "commonwealth" &&
            (CampaignSourceCairoInitialSetup.Units.Any(unit => unit.ElementId == entry.ElementId)
                && entry.AuthorityStatus is "source-setup-placed" or "world8-active"
             || UnitData.Any(unit => unit.ElementId == entry.ElementId)
                && (entry.AuthorityStatus is "source-cw-free-gun-placed" or "source-cw-free-gun-active")
                && placements.Any(value => value.ElementId == entry.ElementId && value.LocationId == site)));
    }
    internal CampaignSourceCwFreeGuns Place(string elementId, string locationId, CampaignSourceBattlefield battlefield)
    {
        if (!ValidDestinations(elementId, battlefield).Contains(locationId, StringComparer.Ordinal))
            throw new InvalidOperationException("CW free-gun placement is unavailable.");
        return new(BattlefieldHash, placements.Append(new(elementId, locationId)));
    }
    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("policyId", PolicyId); writer.WriteString("initialBattlefieldHash", BattlefieldHash);
            writer.WriteString("scenarioHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("cwChartsHash", "sha256:f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981");
            writer.WriteString("mapHash", "sha256:8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030");
            writer.WriteString("source", "spi-1979-scenarios:60.41;spi-1979-commonwealth-charts:oa-british-p10,p12;4.46a");
            writer.WriteString("siteCoverage", "restricted-three-source-cairo-hexes-not-all-map-d-e");
            writer.WriteString("futureAustralianPlacement", "unresolved-must-respect-reserved-stack");
            writer.WriteBoolean("worldCoverageComplete", false); writer.WriteNumber("cityStackLimit", 8);
            writer.WriteStartArray("sites"); foreach (var site in Sites) writer.WriteStringValue(site); writer.WriteEndArray();
            writer.WriteStartArray("units");
            foreach (var unit in UnitData)
            {
                writer.WriteStartObject(); writer.WriteString("elementId", unit.ElementId);
                writer.WriteString("componentId", unit.ElementId + "." + unit.ComponentSuffix);
                writer.WriteString("componentClassId", unit.ComponentClassId); writer.WriteNumber("toe", unit.Toe);
                writer.WriteNumber("capabilityPoints", unit.CapabilityPoints); writer.WriteNumber("stacking", unit.Stacking);
                var placed = placements.SingleOrDefault(value => value.ElementId == unit.ElementId);
                if (placed is null) writer.WriteNull("locationId"); else writer.WriteString("locationId", placed.LocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return stream.ToArray();
    }
    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
    internal string InitialHash() => new CampaignSourceCwFreeGuns(BattlefieldHash, []).Hash();
}
