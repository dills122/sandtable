using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Content;

internal static class ContentTestData
{
    public static ContentOrigin Origin(
        string locator,
        string sourceId = "sandtable-rules-lab",
        ContentOriginKind kind = ContentOriginKind.Synthetic) => new(
            kind,
            [new RuleReference(sourceId, locator)]);

    public static ContentPackDefinition CreateMinimalPack()
    {
        var west = new ContentHex(
            "west",
            "land.terrain.clear",
            null,
            Origin("content.hex.west"));
        var east = new ContentHex(
            "east",
            "land.terrain.clear",
            null,
            Origin("content.hex.east"));
        var edge = new ContentHexEdge(
            "west",
            "east",
            [new ContentEdgeFeature(
                "land.edge.road",
                null,
                Origin("content.edge.road"))],
            Origin("content.edge.east-west"));
        var formation = new ContentFormation(
            "axis-formation",
            "axis",
            null,
            "land.organization.regiment",
            Origin("content.formation.axis"));
        var element = new ContentCombatElement(
            "axis-element",
            "axis",
            formation.FormationId,
            "land.organization.battalion",
            20,
            ContentPlacementMode.Independent,
            Origin("content.element.axis"));
        var scenario = new ContentScenario(
            "minimal-lab",
            new ContentScenarioBoundary(1, 1),
            new ContentScenarioBoundary(1, 3),
            [new ContentInitialPlacement(
                element.ElementId,
                west.LocationId,
                Origin("content.placement.axis"))],
            Origin("content.scenario.minimal"));

        return new ContentPackDefinition(
            ContentPackDefinition.CurrentSchemaVersion,
            "sandtable.content-json.v1",
            "rules-lab.content.minimal.v1",
            "cna-1979.1",
            ["land.hex-topology", "land.formations", "land.initial-deployment"],
            [new ContentSourceIndexEntry(
                "sandtable-rules-lab",
                ContentSourceKind.RepositorySynthetic)],
            [west, east],
            [edge],
            [formation],
            [element],
            [scenario]);
    }

    public static ContentPackDefinition Copy(
        ContentPackDefinition source,
        string? rulesetId = null,
        IEnumerable<string>? capabilities = null,
        IEnumerable<ContentSourceIndexEntry>? sourceIndex = null,
        IEnumerable<ContentHex>? locations = null,
        IEnumerable<ContentWeatherAreaAssignment>? weatherAreaAssignments = null,
        IEnumerable<ContentHexEdge>? edges = null,
        IEnumerable<ContentFormation>? formations = null,
        IEnumerable<ContentCombatElement>? elements = null,
        IEnumerable<ContentScenario>? scenarios = null) => new(
            source.SchemaVersion,
            source.FormatId,
            source.PackId,
            rulesetId ?? source.RulesetId,
            capabilities ?? source.Capabilities,
            sourceIndex ?? source.SourceIndex,
        locations ?? source.Locations,
        weatherAreaAssignments ?? source.WeatherAreaAssignments,
        edges ?? source.Edges,
            formations ?? source.Formations,
            elements ?? source.Elements,
            scenarios ?? source.Scenarios);
}
