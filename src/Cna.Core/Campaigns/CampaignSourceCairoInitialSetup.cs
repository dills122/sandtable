using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

/// <summary>
/// Four real scenario-listed Cairo counters and source-bounded setup choices.
/// This is not a complete Cairo deployment or certified stack/control state.
/// </summary>
internal sealed class CampaignSourceCairoInitialSetup
{
    internal const string PolicyId = "graziani.commonwealth-cairo-initial.curated-placement.v1";
    internal const string InitialHash = "sha256:8ddb59d9f8be09ce4aff2ee7a1a88d71142bfa75760ecca274a1c4002a9902f3";
    private static readonly string[] CandidateSites = ["e1730", "e1830", "e1930"];
    private static readonly CampaignSourceCairoUnit[] SourceUnits =
    [
        new("1-cheshire-machine-gun", "u", 8, "1-cheshire-machine-gun.infantry", Cna1979Combat.InfantryComponentClassId, 3, 1),
        new("1-welch", "m", 10, "1-welch.infantry", Cna1979Combat.InfantryComponentClassId, 6, 1),
        new("2-kings-own-royal-regiment", "m", 10, "2-kings-own-royal-regiment.infantry", Cna1979Combat.InfantryComponentClassId, 6, 1),
        new("8-field-artillery", "x", 20, "8-field-artillery.25-pounder", Cna1979GrazianiSourceFacts.ArtilleryComponentClassId, 6, 1),
    ];
    private readonly ReadOnlyCollection<string> availableSites;
    private readonly ReadOnlyCollection<CampaignSourceCairoPlacement> placements;

    private CampaignSourceCairoInitialSetup(string battlefieldHash, IEnumerable<string> availableSites,
        IEnumerable<CampaignSourceCairoPlacement> placements)
    {
        BattlefieldHash = battlefieldHash;
        this.availableSites = Array.AsReadOnly(availableSites.Order(StringComparer.Ordinal).ToArray());
        this.placements = Array.AsReadOnly(placements.OrderBy(value => value.ElementId, StringComparer.Ordinal).ToArray());
    }

    internal string BattlefieldHash { get; }
    internal static IReadOnlyList<CampaignSourceCairoUnit> Units => Array.AsReadOnly(SourceUnits.ToArray());
    internal IReadOnlyList<CampaignSourceCairoPlacement> Placements => placements;
    internal IReadOnlyList<string> PendingIds => Array.AsReadOnly(SourceUnits.Select(value => value.ElementId)
        .Where(id => placements.All(value => value.ElementId != id)).ToArray());

    internal static CampaignSourceCairoInitialSetup Create(CampaignSourceBattlefield battlefield)
    {
        ArgumentNullException.ThrowIfNull(battlefield);
        var ids = SourceUnits.Select(value => value.ElementId).ToArray();
        if (ids.Intersect(battlefield.Entries.Select(value => value.ElementId), StringComparer.Ordinal).Any()
            || ids.Intersect(battlefield.Pending.SelectMany(value => value.ElementIds), StringComparer.Ordinal).Any())
            throw new InvalidOperationException("Cairo initial counters already exist in placed or deferred source state.");
        // Unknown third-party stack strengths cannot be treated as zero. E1829
        // already contains the 16th Brigade with unresolved HQ attachments;
        // E1931's label is inferred. Neither is offered in this bounded cut.
        var available = CandidateSites.Where(site => battlefield.Entries.All(entry => entry.LocationId != site)).ToArray();
        if (available.Length == 0)
            throw new InvalidOperationException("No bounded unoccupied Cairo setup site is available.");
        var initial = new CampaignSourceCairoInitialSetup(battlefield.Hash(), available, []);
        if (initial.Hash() != InitialHash)
            throw new InvalidOperationException("Cairo source setup differs from retained identity.");
        return initial;
    }

    internal IReadOnlyList<string> ValidDestinations(string elementId)
    {
        if (!PendingIds.Contains(elementId, StringComparer.Ordinal)) return [];
        // The four independently represented counters each print stack 1.
        // §8.37 permits eight stacking points in a major-city hex. The source
        // area remains incomplete, so initial unknown occupied sites are excluded.
        return Array.AsReadOnly(availableSites.Where(site => placements.Count(value => value.LocationId == site) < 8).ToArray());
    }

    internal CampaignSourceCairoInitialSetup Place(string elementId, string locationId)
    {
        if (!ValidDestinations(elementId).Contains(locationId, StringComparer.Ordinal))
            throw new InvalidOperationException("Unavailable Cairo source counter or unsupported destination.");
        return new(BattlefieldHash, availableSites, placements.Append(new(elementId, locationId)));
    }

    internal byte[] SerializeCanonical()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", 1);
            writer.WriteString("policyId", PolicyId);
            writer.WriteString("battlefieldHash", BattlefieldHash);
            writer.WriteString("citySelectionPolicy", "provisional-curated-city-hexes");
            writer.WriteBoolean("siteCoverageComplete", false);
            writer.WriteString("e1829Status", "excluded-unresolved-16th-attachment-stacking");
            writer.WriteString("e1931Status", "excluded-inferred-map-label");
            writer.WriteString("otherCairoForcesStatus", "incomplete");
            writer.WriteString("scenarioSourceHash", "sha256:fa12488e59552d7b0ff0d5abefa891d4b81af0492256bec2d25f8d61ef040a66");
            writer.WriteString("commonwealthChartsSourceHash", "sha256:f75d074727071025f3145b273921ec03882b8b57cb4a429485995d4cf5255981");
            writer.WriteString("airRulesSourceHash", "sha256:9e11678e26269525c25fd276a760e827a7cc738b987b39e89fe14295a7164cf4");
            writer.WriteString("landRulesSourceHash", "sha256:b362870368b9fb8abe6918195fdcf878d4a8041c557b835b9a1bdde3c8b76e99");
            writer.WriteString("mapSourceHash", "sha256:8830ec78df489607a2cc7e2cbc8ddcc9457ecbad3b11df071a60d825dadfd030");
            writer.WriteStartArray("sources");
            writer.WriteStringValue("spi-1979-scenarios:60.41");
            writer.WriteStringValue("spi-1979-commonwealth-charts:oa-british-p11");
            writer.WriteStringValue("spi-1979-commonwealth-charts:oa-artillery-p12");
            writer.WriteStringValue("spi-1979-commonwealth-charts:oa-indian-p16");
            writer.WriteStringValue("spi-1979-commonwealth-charts:4.46a");
            writer.WriteStringValue("spi-1979-air-logistics:counter-sections-7-8-p43");
            writer.WriteStringValue("spi-1979-land-rules:8.37-9.24");
            writer.WriteStringValue("spi-1979-map-e:cairo-city-hexes");
            writer.WriteEndArray();
            writer.WriteStartArray("availableSites");
            foreach (var site in availableSites) writer.WriteStringValue(site);
            writer.WriteEndArray();
            writer.WriteStartArray("units");
            foreach (var unit in SourceUnits)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", unit.ElementId);
                writer.WriteString("sideId", "commonwealth");
                writer.WriteString("characteristicsCode", unit.CharacteristicsCode);
                writer.WriteNumber("capabilityPoints", unit.CapabilityPoints);
                writer.WriteString("componentId", unit.ComponentId);
                writer.WriteString("componentClassId", unit.ComponentClassId);
                writer.WriteNumber("toe", unit.Toe);
                writer.WriteNumber("stacking", unit.Stacking);
                writer.WriteString("attachment", "unresolved");
                writer.WriteString("mobility", "unresolved");
                var placement = placements.SingleOrDefault(value => value.ElementId == unit.ElementId);
                if (placement is null) writer.WriteNull("locationId");
                else writer.WriteString("locationId", placement.LocationId);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    internal string Hash() => "sha256:" + Convert.ToHexStringLower(SHA256.HashData(SerializeCanonical()));
}

internal sealed record CampaignSourceCairoUnit(string ElementId, string CharacteristicsCode, int CapabilityPoints,
    string ComponentId, string ComponentClassId, int Toe, int Stacking);
internal sealed record CampaignSourceCairoPlacement(string ElementId, string LocationId);
