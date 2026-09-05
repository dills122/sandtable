using Cna.Core.Rules;

namespace Cna.Core.Content;

public static class Cna1979ReactionContentCatalog
{
    private static readonly string[] RemoteLocations = ["remote-source", "remote-neighbor"];
    private static readonly string[] RemoteElementSuffixes = ["c", "d"];

    public static IReadOnlyList<ContentPackV5Artifact> Artifacts { get; } = Array.AsReadOnly(
        new[] { "adjacent", "positive-zoc", "low-defense", "remote-zoc", "headquarters", "noncombat", "recurrence" }
            .Select(Create).ToArray());

    private static ContentPackV5Artifact Create(string variant)
    {
        var seed = Cna1979SyntheticContentCatalog.ArtifactV5.Definition;
        var legacy = seed.LegacyDefinition;
        var packId = $"rules-lab.content.reaction.{variant}.v1";
        var scenarioId = $"reaction-{variant}";
        var elements = legacy.Elements.Concat(RemoteElementSuffixes.Select(suffix =>
            new ContentCombatElement($"commonwealth-element-{suffix}", "commonwealth",
                "commonwealth-lab-formation", "land.organization.battalion",
                Cna1979Movement.NonMotorizedMobilityId, 10, ContentPlacementMode.Independent,
                Origin($"element.{suffix}")))).ToArray();
        var locations = legacy.Locations.Concat(RemoteLocations
            .Select(id => new ContentHex(id, "land.terrain.clear", null, Origin($"location.{id}"))));
        var weather = legacy.WeatherAreaAssignments.Concat(RemoteLocations
            .Select(id => new ContentWeatherAreaAssignment(id, ContentWeatherArea.A, Origin($"weather.{id}"))));
        var edges = legacy.Edges.Concat(new[]
        {
            Edge("center", "north-east"), Edge("center", "south-east"),
            Edge("north-east", "south-east"), Edge("remote-neighbor", "remote-source"),
            Edge("remote-neighbor", "south"),
        });
        var stacked = variant is "positive-zoc" or "low-defense";
        var scenario = new ContentScenario(scenarioId, new(1, 1), new(1, 3),
            elements.Select(element => new ContentInitialPlacement(element.ElementId,
                element.ElementId switch
                {
                    "axis-element-a" or "axis-element-b" => "west",
                    "commonwealth-element-a" => "east",
                    "commonwealth-element-b" => stacked ? "east" : "north-east",
                    _ => "remote-source",
                }, Origin($"placement.{variant}.{element.ElementId}"))), Origin($"scenario.{variant}"));
        var definition = new ContentPackDefinition(legacy.SchemaVersion, legacy.FormatId, packId,
            legacy.RulesetId, legacy.Capabilities, legacy.SourceIndex, locations, weather, edges,
            legacy.Formations, elements, [scenario]);
        var combat = elements.Select(element => new ContentElementCombatFacts(element.ElementId,
            Classification(variant, element.ElementId),
            [new ContentCombatComponent($"{element.ElementId}.toe.infantry",
                Cna1979Combat.InfantryComponentClassId, 5, 1, Origin($"component.{element.ElementId}"))],
            Origin($"combat.{variant}.{element.ElementId}")));
        var placements = elements.Select(element => new ContentInitialPlacementCombatFacts(
            scenarioId, element.ElementId,
            [new ContentInitialComponentToe($"{element.ElementId}.toe.infantry",
                InitialToe(variant, element),
                Origin($"toe.{variant}.{element.ElementId}"))]));
        return ContentPackV5Artifact.Create(new(definition, combat, placements));
    }

    private static string Classification(string variant, string elementId) =>
        elementId is "commonwealth-element-a" or "commonwealth-element-b"
            ? variant switch
            {
                "headquarters" => Cna1979Combat.HeadquartersClassificationId,
                "noncombat" => Cna1979Combat.TruckConvoyClassificationId,
                _ => Cna1979Combat.CombatUnitClassificationId,
            }
            : Cna1979Combat.CombatUnitClassificationId;

    private static int InitialToe(string variant, ContentCombatElement element)
    {
        if (element.SideId == "axis" || variant == "recurrence") return 2;
        if (variant == "low-defense") return 4;
        if (element.ElementId is "commonwealth-element-c" or "commonwealth-element-d")
            return variant == "remote-zoc" ? 5 : 2;
        return 5;
    }

    private static ContentHexEdge Edge(string first, string second) => new(first, second,
        [new ContentEdgeFeature("land.edge.road", null, Origin($"road.{first}.{second}"))],
        Origin($"edge.{first}.{second}"));

    private static ContentOrigin Origin(string locator) => new(ContentOriginKind.Synthetic,
        [new RuleReference("sandtable-rules-lab", $"content.reaction.v1.{locator}")]);
}
