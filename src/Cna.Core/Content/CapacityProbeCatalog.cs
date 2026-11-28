#if CAPACITY_PROBE
using Cna.Core.Campaigns;
using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Content;

// Invented, deterministic benchmark data. Never compiled into a normal Core build.
public static class CapacityProbeCatalog
{
    public static IReadOnlyList<int> Sizes { get; } = Array.AsReadOnly(new[] { 60, 250, 1000 });

    private static readonly Dictionary<int, Lazy<ContentPackV6Artifact>> Packs = Sizes.ToDictionary(
        size => size, size => new Lazy<ContentPackV6Artifact>(() => Generate(size)));

    public static string PackId(int size) => FormattableString.Invariant($"capacity-probe.content.{size}.v1");
    public static string SetupId(int size) => FormattableString.Invariant($"capacity-probe.setup.{size}.v1");
    public static ContentPackV6Artifact Get(int size) => Packs[size].Value;

    public static CampaignCreationRequest Request(int size, string campaignId, ulong seed)
    {
        var pack = Get(size);
        if (!Cna1979BreakdownSetupCatalog.TryGet(SetupId(size), out var setup))
            throw new InvalidOperationException("Capacity probe setup is not registered.");
        return new(1, campaignId, Cna1979Ruleset.Manifest.Hash, seed, setup.SetupId, setup.Hash,
            pack.Identity.PackId, pack.Identity.Hash, setup.Content.ScenarioId);
    }

    public static ContentPackV6Artifact Generate(int size)
    {
        if (!Sizes.Contains(size)) throw new ArgumentOutOfRangeException(nameof(size));
        var width = size == 60 ? 20 : 100;
        var height = size == 60 ? 20 : 70;
        const string organization = "land.organization.battalion";
        const string source = "capacity-probe";
        const string scenario = "capacity-probe";
        var origin = new ContentOrigin(ContentOriginKind.Synthetic, [new RuleReference(source, "invented-grid")]);
        static string Hex(int x, int y) => FormattableString.Invariant($"p-{x:D3}-{y:D3}");
        var locations = new List<ContentHex>();
        var weather = new List<ContentWeatherAreaAssignment>();
        var edges = new List<ContentHexEdge>();
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var id = Hex(x, y);
            locations.Add(new(id, (x + y) % 3 == 0 ? "land.terrain.desert" : "land.terrain.clear", null, origin));
            weather.Add(new(id, (ContentWeatherArea)(1 + (x + y) % 5), origin));
            if (x + 1 < width) edges.Add(new(id, Hex(x + 1, y), [], origin));
            if (y + 1 < height) edges.Add(new(id, Hex(x, y + 1), [], origin));
            if (x > 0 && y + 1 < height) edges.Add(new(id, Hex(x - 1, y + 1), [], origin));
        }
        var formations = new List<ContentFormation>();
        var elements = new List<ContentCombatElement>();
        var facts = new List<ContentElementCombatFacts>();
        var placements = new List<ContentInitialPlacement>();
        var toes = new List<ContentInitialPlacementCombatFacts>();
        // Clusters of occupied neighbouring cells separated by free cells. One element per cell
        // is the current v6 validator's maximum; do not bypass the stack limit.
        for (var i = 0; i < size; i++)
        {
            var id = FormattableString.Invariant($"piece-{i:D4}");
            var side = i % 2 == 0 ? "axis" : "commonwealth";
            var root = id + ".root";
            var component = id + ".component";
            formations.Add(new(root, side, null, organization, origin));
            elements.Add(new(id, side, root, organization, Cna1979Movement.NonMotorizedMobilityId,
                8, ContentPlacementMode.Independent, origin));
            facts.Add(new(id, Cna1979Combat.CombatUnitClassificationId,
                [new ContentCombatComponent(component, Cna1979Combat.InfantryComponentClassId, 3, 1, origin)], origin));
            // Contiguous pairs on each row, with a free row between clusters.
            placements.Add(new(id, Hex(i % width, i / width * 2), origin));
            toes.Add(new(scenario, id, [new ContentInitialComponentToe(component, 3, origin)]));
        }
        var legacy = new ContentPackDefinition(ContentPackDefinition.CurrentSchemaVersion,
            ContentPackDefinition.CanonicalFormatId, PackId(size), Cna1979Ruleset.RulesetId,
            ["land.element-mobility", "land.formations", "land.hex-topology", "land.initial-deployment", "land.weather-areas"],
            [new ContentSourceIndexEntry(source, ContentSourceKind.RepositorySynthetic)], locations, weather,
            edges, formations, elements, [new ContentScenario(scenario, new(1, 1), new(1, 3), placements, origin)]);
        return ContentPackV6Artifact.Create(new(legacy, facts, toes, ContentPackV6Definition.SupportedCapabilityProfileId));
    }
}
#endif
