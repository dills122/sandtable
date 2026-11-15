using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

/// <summary>
/// Commonwealth abstract supply counters and source setup choices. This is
/// a restricted source-backed overlay, not a complete map or campaign.
/// </summary>
internal sealed class CampaignSourceCommonwealthSupplyWorld
{
    internal const int ContractVersion = 1;
    internal const string PolicyId = "graziani.commonwealth-initial-supply.curated-sites.v1";
    private static readonly string[] AlexandriaSites = ["e3613", "e3714"];
    private static readonly string[] CairoSites = ["e1730", "e1829", "e1830", "e1930", "e1931"];
    private static readonly string[] DumpSites = ["d3611", "d3612"];
    private static readonly string[] PlacementIds =
        ["cw-dump-1", "cw-alexandria-1", "cw-alexandria-2", "cw-alexandria-3", "cw-cairo-1", "cw-cairo-2", "cw-cairo-3"];
    private readonly ReadOnlyCollection<CampaignSourceSupplyUnit> units;
    private readonly ReadOnlyCollection<CampaignSourceSupplyDump> placements;
    private readonly CampaignSourceDummyDecision? dummyDecision;

    private CampaignSourceCommonwealthSupplyWorld(IEnumerable<CampaignSourceSupplyUnit> units,
        IEnumerable<CampaignSourceSupplyDump> placements, CampaignSourceDummyDecision? dummyDecision)
    {
        this.units = Array.AsReadOnly(units.OrderBy(value => value.Id, StringComparer.Ordinal).ToArray());
        this.placements = Array.AsReadOnly(placements.OrderBy(value => value.DumpId, StringComparer.Ordinal).ToArray());
        this.dummyDecision = dummyDecision;
    }

    internal IReadOnlyList<CampaignSourceSupplyUnit> Units => units;
    internal IReadOnlyList<CampaignSourceSupplyDump> Placements => placements;
    internal CampaignSourceDummyDecision? DummyDecision => dummyDecision;
    internal IReadOnlyList<string> PendingPlacementIds => Array.AsReadOnly(PlacementIds
        .Where(id => placements.All(value => value.DumpId != id)).ToArray());

    internal static CampaignSourceCommonwealthSupplyWorld Create(CampaignPregameSourceState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Source.Session.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash)
            throw new InvalidOperationException("Commonwealth supply setup requires the pinned Graziani source.");
        var values = new List<CampaignSourceSupplyUnit>();
        for (var index = 1; index <= 5; index++)
            values.Add(new($"cw-matruh-{index}", "d3714", 40, 60));
        values.Add(new("cw-sidi-barrani-1", "c4131", 40, 60));
        return new CampaignSourceCommonwealthSupplyWorld(values, [], null);
    }

    internal IReadOnlyList<string> ValidDestinations(string placementId)
    {
        if (!PendingPlacementIds.Contains(placementId, StringComparer.Ordinal)) return [];
        // §60.41 deploys 7th Armoured Division in D3612; original Map D
        // places D3611 and D3612 west of Matruh. Other sites are not modeled.
        if (placementId == "cw-dump-1")
            return Array.AsReadOnly(DumpSites.Where(site => units.All(unit => unit.LocationId != site)).ToArray());
        var sites = placementId.StartsWith("cw-alexandria-", StringComparison.Ordinal)
            ? AlexandriaSites : CairoSites;
        // §32.14 permits five supply units in each major city hex. §60.92C
        // does not require a city's three units to share a hex.
        return Array.AsReadOnly(sites.Where(site => units.Count(unit => unit.LocationId == site) < 5).ToArray());
    }

    internal CampaignSourceCommonwealthSupplyWorld Place(string placementId, string locationId)
    {
        if (!ValidDestinations(placementId).Contains(locationId, StringComparer.Ordinal))
            throw new InvalidOperationException("Unavailable Commonwealth setup supply or unsupported destination.");
        var next = units.ToList();
        if (placementId == "cw-dump-1")
        {
            for (var index = 1; index <= 3; index++)
                next.Add(new($"{placementId}-{index}", locationId, 40, 30));
        }
        else next.Add(new(placementId, locationId, 40, 60));
        return new CampaignSourceCommonwealthSupplyWorld(next, placements.Append(new(placementId, locationId)), dummyDecision);
    }

    internal IReadOnlyList<string> ValidDummyDestinations()
    {
        if (dummyDecision is not null) return [];
        return Array.AsReadOnly(DumpSites.Where(site => units.All(unit => unit.LocationId != site)).ToArray());
    }

    // §60.92C supplies one optional group of three. A null location records a
    // decision to omit the whole group, rather than silently discarding it.
    internal CampaignSourceCommonwealthSupplyWorld DecideDummy(string? locationId)
    {
        if (dummyDecision is not null
            || locationId is not null && !ValidDummyDestinations().Contains(locationId, StringComparer.Ordinal))
            throw new InvalidOperationException("Unavailable Commonwealth dummy group or unsupported destination.");
        var next = units.ToList();
        if (locationId is not null)
            for (var index = 1; index <= 3; index++)
                next.Add(new CampaignSourceSupplyUnit($"cw-dummy-1-{index}", locationId, null, 0, true));
        return new CampaignSourceCommonwealthSupplyWorld(next, placements, new("cw-dummy-1", locationId));
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", ContractVersion);
            writer.WriteString("policyId", PolicyId);
            writer.WriteBoolean("siteCoverageComplete", false);
            writer.WriteString("otherMapHexCapacityPolicy", "provisional-three-land-units");
            writer.WriteString("mapSourceHash", "sha256:8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030");
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("landRulesSourceHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteStartArray("curatedDumpSites");
            foreach (var site in DumpSites) writer.WriteStringValue(site);
            writer.WriteEndArray();
            writer.WriteStartArray("alexandriaSites");
            foreach (var site in AlexandriaSites) writer.WriteStringValue(site);
            writer.WriteEndArray();
            writer.WriteStartArray("cairoSites");
            foreach (var site in CairoSites) writer.WriteStringValue(site);
            writer.WriteEndArray();
            writer.WriteNumber("scenarioDummyGroupsUnresolved", dummyDecision is null ? 1 : 0);
            writer.WriteString("generalDummyAllowanceStatus", "unresolved");
            writer.WriteString("dummyOmissionPolicy", "provisional-whole-group-optional");
            writer.WriteString("cairoE1931Status", "neighbor-and-summary-inference");
            writer.WriteStartArray("sources");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.41");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.92C");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":32.14");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.AirLogisticsSourceId + ":important-locations-p26");
            writer.WriteStringValue("spi-1979-map-d:source-hexes");
            writer.WriteEndArray();
            writer.WriteStartArray("units");
            foreach (var unit in units)
            {
                writer.WriteStartObject();
                writer.WriteString("id", unit.Id);
                writer.WriteString("locationId", unit.LocationId);
                writer.WriteBoolean("dummy", unit.IsDummy);
                if (unit.Ammo is { } ammo) writer.WriteNumber("ammo", ammo);
                else writer.WriteNull("ammo");
                writer.WriteNumber("fuel", unit.Fuel);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("placements");
            foreach (var placement in placements)
            {
                writer.WriteStartObject();
                writer.WriteString("id", placement.DumpId);
                writer.WriteString("locationId", placement.LocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartObject("dummyDecision");
            writer.WriteBoolean("decided", dummyDecision is not null);
            if (dummyDecision?.LocationId is { } dummyLocation) writer.WriteString("locationId", dummyLocation);
            else writer.WriteNull("locationId");
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}
