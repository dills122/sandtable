using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Campaigns;

/// <summary>
/// Axis abstract-logistics supply counters at Graziani setup. Locations are
/// source-linked addresses, not a complete playable map or movement topology.
/// </summary>
internal sealed class CampaignSourceAxisSupplyWorld
{
    internal const int ContractVersion = 1;
    internal const string PolicyId = "graziani.axis-initial-supply.curated-sites.v1";
    private static readonly string[] DumpIds = ["axis-dump-1", "axis-dump-2"];
    private static readonly string[] DummyIds = ["axis-dummy-1", "axis-dummy-2"];

    // §60.31 places two Axis formations at C4507/C4707; Map C shows those
    // and C4407/C4607 as land hexes in Libya. These curated locations are
    // well beyond four hexes of all §60.41 Commonwealth setup locations,
    // including the unrestricted map-D/E placements. This is a deliberately
    // restricted subset of valid dump sites, not the entire legal map.
    private static readonly string[] CuratedSites = ["c4407", "c4507", "c4607", "c4707"];
    private static readonly string[] CommonwealthMapCPositions =
        ["c4131", "c3926", "c3922", "c3721", "c3520", "c3320", "c3020"];
    private readonly ReadOnlyCollection<CampaignSourceSupplyUnit> units;
    private readonly ReadOnlyCollection<CampaignSourceSupplyDump> dumps;
    private readonly ReadOnlyCollection<CampaignSourceDummyDecision> dummyDecisions;

    private CampaignSourceAxisSupplyWorld(IEnumerable<CampaignSourceSupplyUnit> units,
        IEnumerable<CampaignSourceSupplyDump> dumps, IEnumerable<CampaignSourceDummyDecision> dummyDecisions)
    {
        this.units = Array.AsReadOnly(units.OrderBy(value => value.Id, StringComparer.Ordinal).ToArray());
        this.dumps = Array.AsReadOnly(dumps.OrderBy(value => value.DumpId, StringComparer.Ordinal).ToArray());
        this.dummyDecisions = Array.AsReadOnly(dummyDecisions.OrderBy(value => value.Id, StringComparer.Ordinal).ToArray());
    }

    internal IReadOnlyList<CampaignSourceSupplyUnit> Units => units;
    internal IReadOnlyList<CampaignSourceSupplyDump> Dumps => dumps;
    internal IReadOnlyList<CampaignSourceDummyDecision> DummyDecisions => dummyDecisions;
    internal IReadOnlyList<string> PendingDummyIds => Array.AsReadOnly(DummyIds
        .Where(id => dummyDecisions.All(value => value.Id != id)).ToArray());
    internal IReadOnlyList<string> PendingDumpIds => Array.AsReadOnly(DumpIds
        .Where(id => dumps.All(value => value.DumpId != id)).ToArray());

    internal static CampaignSourceAxisSupplyWorld Create(CampaignPregameSourceState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Source.Session.Content.Identity.Hash != CampaignPregameSourceCreation.SupportedContentHash)
            throw new InvalidOperationException("Axis supply setup requires the pinned Graziani source.");
        var values = new List<CampaignSourceSupplyUnit>();
        Add(values, "axis-tobruk", "c4807", 3, 15, 50);
        Add(values, "axis-bardia", "c4321", 2, 10, 40);
        // Scenario prints B4827; map/summary locate Benghazi at A4827.
        // Retain the literal row and flag the unresolved conflict in the checkpoint.
        Add(values, "axis-benghazi", "b4827", 1, 10, 10);
        // Source prints a dash for Derna ammo: preserve unknown rather than zero.
        Add(values, "axis-derna", "b5925", 1, null, 10);
        Add(values, "axis-tripoli", "box.tripoli", 8, 5, 60);
        Add(values, "axis-c0716", "c0716", 1, 20, 6);
        return new CampaignSourceAxisSupplyWorld(values, [], []);
    }

    internal IReadOnlyList<string> ValidDestinations(string dumpId)
    {
        if (!PendingDumpIds.Contains(dumpId, StringComparer.Ordinal)) return [];
        // Each dump contains three units. This provisional interpretation of
        // §32.14 applies its three-unit limit to other on-map hexes, so these
        // curated sites cannot hold two dumps.
        return Array.AsReadOnly(CuratedSites.Where(site => units.All(unit => unit.LocationId != site)
            && CommonwealthMapCPositions.All(enemy => HexDistanceLowerBound(site, enemy) > 4)).ToArray());
    }

    internal CampaignSourceAxisSupplyWorld Place(string dumpId, string locationId)
    {
        if (!ValidDestinations(dumpId).Contains(locationId, StringComparer.Ordinal))
            throw new InvalidOperationException("Unavailable Axis setup dump or unsupported destination.");
        var next = units.ToList();
        Add(next, dumpId, locationId, 3, 40, 50);
        return new CampaignSourceAxisSupplyWorld(next, dumps.Append(new(dumpId, locationId)), dummyDecisions);
    }

    internal IReadOnlyList<string> ValidDummyDestinations(string dummyId)
    {
        if (!PendingDummyIds.Contains(dummyId, StringComparer.Ordinal)) return [];
        return Array.AsReadOnly(CuratedSites.Where(site => units.All(unit => unit.LocationId != site)
            && CommonwealthMapCPositions.All(enemy => HexDistanceLowerBound(site, enemy) > 4)).ToArray());
    }

    // Null omits this optional source group; otherwise all three share one hex.
    internal CampaignSourceAxisSupplyWorld DecideDummy(string dummyId, string? locationId)
    {
        if (!PendingDummyIds.Contains(dummyId, StringComparer.Ordinal)
            || locationId is not null && !ValidDummyDestinations(dummyId).Contains(locationId, StringComparer.Ordinal))
            throw new InvalidOperationException("Unavailable Axis dummy group or unsupported destination.");
        var next = units.ToList();
        if (locationId is not null)
            for (var index = 1; index <= 3; index++)
                next.Add(new CampaignSourceSupplyUnit($"{dummyId}-{index}", locationId, null, 0, true));
        return new CampaignSourceAxisSupplyWorld(next, dumps, dummyDecisions.Append(new(dummyId, locationId)));
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
            foreach (var site in CuratedSites) writer.WriteStringValue(site);
            writer.WriteEndArray();
            writer.WriteStartArray("commonwealthMapCPositions");
            foreach (var position in CommonwealthMapCPositions) writer.WriteStringValue(position);
            writer.WriteEndArray();
            writer.WriteNumber("minimumMapCEnemyDistanceLowerBound", 5);
            writer.WriteString("benghaziCoordinateStatus", "scenario-b4827-map-a4827-unresolved");
            // Scenario specifically provides two groups of three optional
            // dummies; no choice is made and the general dummy rule remains open.
            writer.WriteNumber("scenarioDummyGroupsUnresolved", PendingDummyIds.Count);
            writer.WriteString("generalDummyAllowanceStatus", "unresolved");
            writer.WriteString("dummyOmissionPolicy", "provisional-whole-group-optional");
            writer.WriteStartArray("sources");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.31");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.41");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.ScenariosSourceId + ":60.92B");
            writer.WriteStringValue(Cna1979GrazianiSourceFacts.LandRulesSourceId + ":32.14");
            writer.WriteStringValue("spi-1979-map-c:source-hexes");
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
            writer.WriteStartArray("dumps");
            foreach (var dump in dumps)
            {
                writer.WriteStartObject();
                writer.WriteString("id", dump.DumpId);
                writer.WriteString("locationId", dump.LocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("dummyDecisions");
            foreach (var decision in dummyDecisions)
            {
                writer.WriteStartObject();
                writer.WriteString("id", decision.Id);
                if (decision.LocationId is null) writer.WriteNull("locationId");
                else writer.WriteString("locationId", decision.LocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));

    // Every hex step changes each printed grid component by at most one. This
    // lower bound cannot overstate separation, regardless of row staggering.
    private static int HexDistanceLowerBound(string first, string second)
    {
        var firstRow = int.Parse(first.AsSpan(1, 2), System.Globalization.CultureInfo.InvariantCulture);
        var firstColumn = int.Parse(first.AsSpan(3, 2), System.Globalization.CultureInfo.InvariantCulture);
        var secondRow = int.Parse(second.AsSpan(1, 2), System.Globalization.CultureInfo.InvariantCulture);
        var secondColumn = int.Parse(second.AsSpan(3, 2), System.Globalization.CultureInfo.InvariantCulture);
        return Math.Max(Math.Abs(firstRow - secondRow), Math.Abs(firstColumn - secondColumn));
    }

    private static void Add(List<CampaignSourceSupplyUnit> units, string prefix, string locationId,
        int count, int? ammo, int fuel)
    {
        for (var index = 1; index <= count; index++)
            units.Add(new CampaignSourceSupplyUnit($"{prefix}-{index}", locationId, ammo, fuel));
    }
}

internal sealed record CampaignSourceSupplyUnit(string Id, string LocationId, int? Ammo, int Fuel, bool IsDummy = false);
internal sealed record CampaignSourceSupplyDump(string DumpId, string LocationId);
internal sealed record CampaignSourceDummyDecision(string Id, string? LocationId);
