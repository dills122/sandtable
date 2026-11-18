using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Campaigns;
using Cna.Core.Rules;

namespace Cna.Core.Content;

// Native content successor for the retained Tobruk/Bardia static-defense
// garrison counters: the 30th/31st/32nd GAF SDIC battalions and their HQs,
// the Marine Regiment and its battalions, 31 Libyan Coastal Defense, Tobruk's
// coastal/field artillery and light AA, and Bardia's coastal artillery and AA.
// Every element and TOE here already carries a citation in
// Cna1979GrazianiSourceFacts.cs (the per-element combat facts and TOE seed
// functions it already exposes for Tobruk/Bardia, reused verbatim) or in the
// retained oa-tobruk-p12/oa-bardia-p13/scenario 60.31 group-level citations
// already attached to TobrukInfantryChartSubset/TobrukArtilleryAndAaChartSubset/
// BardiaGarrisonChartCandidate; none of it is newly asserted here. Headquarters
// elements are named by those group citations but have no printed combat
// strength, so they are admitted with zero TOE components, matching the
// project's existing CreateScheduledHeadquarters convention.
internal sealed class ContentSourceTobrukBardiaGarrisonPackArtifact
{
    internal const string FormatId = "sandtable.source-tobruk-bardia-garrison.v1";
    internal const string ContentHash = "sha256:6b6fe5626e3e2ba8d7cb62b2b1bd5cca3efe297dc46bd7b514c8af61ef59c133"; // RUL-GRA-28: derived from CampaignSourceAxisSupplementalForces.ContentHash.
    private readonly byte[] bytes;

    private ContentSourceTobrukBardiaGarrisonPackArtifact(ContentSourceCwFreeGunsPackArtifact basis, byte[] bytes)
    {
        Basis = basis; this.bytes = bytes.ToArray();
        Identity = new(FormatId, basis.Identity.PackId, basis.Identity.RulesetId,
            "sha256:" + Convert.ToHexStringLower(SHA256.HashData(bytes)));
    }

    internal ContentSourceCwFreeGunsPackArtifact Basis { get; }
    internal ContentScheduledPackIdentity Identity { get; }
    internal byte[] CanonicalBytes => bytes.ToArray();

    internal const string TobrukLocationId = "c4807";
    internal const string BardiaLocationId = "c4321";

    // 31/32 GaF SDIC HQs + their three SDIC battalions each, the Marine
    // Regiment HQ + its three battalions, and 31 Libyan Coastal Defense.
    // Citation: TobrukInfantryChartSubset (scenario 60.31; oa-tobruk-p12).
    internal static IReadOnlyList<string> TobrukInfantryIds { get; } = Array.AsReadOnly(new[]
    {
        "31-gaf-sdic-hq", "1-31-gaf-sdic", "2-31-gaf-sdic", "3-31-gaf-sdic",
        "32-gaf-sdic-hq", "1-32-gaf-sdic", "2-32-gaf-sdic", "3-32-gaf-sdic",
        "marine-regiment-hq", "1-marine-battalion", "2-marine-battalion", "3-marine-battalion",
        "31-libyan-coastal-defense-battalion",
    });

    // Tobruk's artiglieria and coastal-defense artillery plus its light AA
    // battalion. Citation: TobrukArtilleryAndAaChartSubset (scenario 60.31;
    // oa-tobruk-p12).
    internal static IReadOnlyList<string> TobrukSupportIds { get; } = Array.AsReadOnly(new[]
    {
        "5-tobruk-artiglieria", "6-tobruk-artiglieria", "7-tobruk-artiglieria",
        "1-tobruk-coastal-defense-artillery", "2-tobruk-coastal-defense-artillery",
        "3-tobruk-coastal-defense-artillery", "4-tobruk-coastal-defense-artillery",
        "5-tobruk-coastal-defense-artillery", "tobruk-light-aa-battalion",
    });

    // 30 GaF SDIC HQ + its three SDIC battalions. Citation:
    // BardiaGarrisonChartCandidate (scenario 60.31; oa-bardia-p13; the
    // counter-sheet's 30-GaF-Bardia/32-GaF-Tobruk correction).
    internal static IReadOnlyList<string> BardiaInfantryIds { get; } = Array.AsReadOnly(new[]
    {
        "30-gaf-sdic-hq", "1-30-gaf-sdic", "2-30-gaf-sdic", "3-30-gaf-sdic",
    });

    // Bardia's coastal-defense artillery and its AA battalion. Citation:
    // BardiaGarrisonChartCandidate (scenario 60.31; oa-bardia-p13).
    internal static IReadOnlyList<string> BardiaSupportIds { get; } = Array.AsReadOnly(new[]
    {
        "5-bardia-coastal-defense-artillery", "17-bardia-antiair",
    });

    // The four headquarters named by the retained chart subsets but never
    // given a printed combat value anywhere in the source facts.
    internal static IReadOnlyList<string> HeadquartersIds { get; } = Array.AsReadOnly(new[]
    {
        "31-gaf-sdic-hq", "32-gaf-sdic-hq", "marine-regiment-hq", "30-gaf-sdic-hq",
    });

    internal static IReadOnlyList<string> TobrukIds { get; } =
        Array.AsReadOnly(TobrukInfantryIds.Concat(TobrukSupportIds).ToArray());

    internal static IReadOnlyList<string> BardiaIds { get; } =
        Array.AsReadOnly(BardiaInfantryIds.Concat(BardiaSupportIds).ToArray());

    internal static IReadOnlyList<string> AllIds { get; } =
        Array.AsReadOnly(TobrukIds.Concat(BardiaIds).ToArray());

    internal static string LocationOf(string elementId) => elementId switch
    {
        _ when TobrukIds.Contains(elementId, StringComparer.Ordinal) => TobrukLocationId,
        _ when BardiaIds.Contains(elementId, StringComparer.Ordinal) => BardiaLocationId,
        _ => throw new ArgumentException("Unknown Tobruk/Bardia garrison element.", nameof(elementId)),
    };

    private static ContentOrigin HeadquartersOrigin(string elementId)
    {
        var oaLocator = elementId == "30-gaf-sdic-hq" ? "oa-bardia-p13" : "oa-tobruk-p12";
        return new ContentOrigin(ContentOriginKind.SourceDerived,
        [
            new RuleReference(Cna1979GrazianiSourceFacts.ScenariosSourceId, "60.31"),
            new RuleReference(Cna1979GrazianiSourceFacts.AxisChartsSourceId, oaLocator),
        ]);
    }

    // Every headquarters' TOE is zero: the retained chart subsets name these
    // four HQs but print no combat value for any of them (TobrukInfantryCombatFacts'
    // own comment notes "Conditional headquarters are intentionally absent").
    // This is not an invented number; it is the project's existing
    // zero-component headquarters convention (ContentElementCombatFacts.
    // CreateScheduledHeadquarters) applied to the already-cited group entry.
    internal static ContentElementCombatFacts HeadquartersFacts(string elementId) =>
        ContentElementCombatFacts.CreateScheduledHeadquarters(elementId, HeadquartersOrigin(elementId));

    // Unions the already-published Tobruk/Bardia TOE seed functions (filtered
    // to this pack's 24 non-headquarters elements) with the four zero-TOE
    // headquarters seeds. No new TOE value is introduced anywhere here.
    internal static IReadOnlyList<ContentInitialPlacementCombatFacts> CreateToeSeeds(string scenarioId)
    {
        var bardiaSupportIds = BardiaSupportIds;
        var bardiaInfantryIds = BardiaInfantryIds.Where(id => id != "30-gaf-sdic-hq").ToArray();
        var headquarters = HeadquartersIds.Select(id =>
            new ContentInitialPlacementCombatFacts(scenarioId, id, []));
        var seeds = Cna1979GrazianiSourceFacts.CreateTobrukInfantryToeSeeds(scenarioId)
            .Concat(Cna1979GrazianiSourceFacts.CreateTobrukSupportToeSeeds(scenarioId))
            .Concat(Cna1979GrazianiSourceFacts.CreateBardiaSupportToeSeeds(scenarioId)
                .Where(value => bardiaSupportIds.Contains(value.ElementId, StringComparer.Ordinal)))
            .Concat(Cna1979GrazianiSourceFacts.CreateBardiaInfantryToeSeeds(scenarioId)
                .Where(value => bardiaInfantryIds.Contains(value.ElementId, StringComparer.Ordinal)))
            .Concat(headquarters)
            .ToArray();
        if (seeds.Length != AllIds.Count
            || seeds.Select(value => value.ElementId).Distinct(StringComparer.Ordinal).Count() != AllIds.Count
            || seeds.Any(value => !AllIds.Contains(value.ElementId, StringComparer.Ordinal)))
            throw new InvalidOperationException("Tobruk/Bardia garrison TOE seeds differ from its retained element list.");
        return Array.AsReadOnly(seeds);
    }

    internal static ContentSourceTobrukBardiaGarrisonPackArtifact Create(ContentSourceCwFreeGunsPackArtifact basis)
    {
        ArgumentNullException.ThrowIfNull(basis);
        if (basis.Hash() != ContentSourceCwFreeGunsPackArtifact.ContentHash)
            throw new InvalidOperationException("Tobruk/Bardia garrison requires pinned native source content.");
        var seeds = CreateToeSeeds(Cna1979GrazianiCairoSourceExcerpt.ScenarioId)
            .ToDictionary(value => value.ElementId, StringComparer.Ordinal);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("formatId", FormatId);
            writer.WriteString("baseHash", basis.Hash());
            writer.WriteBase64String("baseContent", basis.CanonicalBytes);
            writer.WriteString("sources", "scenario:60.31;axis-charts:oa-tobruk-p12,oa-bardia-p13;land:4.45");
            writer.WriteStartArray("units");
            foreach (var id in AllIds.Order(StringComparer.Ordinal))
            {
                var toe = seeds[id];
                writer.WriteStartObject();
                writer.WriteString("elementId", id);
                writer.WriteString("sideId", "axis");
                writer.WriteString("locationId", LocationOf(id));
                writer.WriteStartArray("components");
                foreach (var component in toe.InitialComponentToes)
                {
                    writer.WriteStartObject();
                    writer.WriteString("componentId", component.ComponentId);
                    writer.WriteNumber("toe", component.CurrentToe);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        var result = new ContentSourceTobrukBardiaGarrisonPackArtifact(basis, stream.ToArray());
        if (result.Identity.Hash != ContentHash)
            throw new InvalidOperationException("Tobruk/Bardia garrison content differs from its pinned identity.");
        return result;
    }

    internal static ContentSourceTobrukBardiaGarrisonPackArtifact Read(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        using var document = JsonDocument.Parse(bytes);
        var basis = ContentSourceCwFreeGunsPackArtifact.Read(document.RootElement.GetProperty("baseContent").GetBytesFromBase64());
        var expected = Create(basis);
        if (!bytes.AsSpan().SequenceEqual(expected.bytes))
            throw new InvalidOperationException("Tobruk/Bardia garrison content is not canonical.");
        return expected;
    }
}
