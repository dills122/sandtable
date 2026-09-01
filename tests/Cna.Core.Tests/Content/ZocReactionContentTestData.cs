using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Content;

internal static class ZocReactionContentTestData
{
    public const string PackId = "rules-lab.content.zoc-reaction.v1";
    public const string ScenarioId = "zoc-reaction-lab";
    public const string FirstElementId = "axis-battalion-alpha";
    public const string SecondElementId = "axis-battalion-bravo";

    public static ContentPackV5Definition CreatePositiveFixture()
    {
        var seed = ContentTestData.CreateMinimalPack();
        var formation = seed.Formations.Single();
        var first = Element(FirstElementId, formation.FormationId);
        var second = Element(SecondElementId, formation.FormationId);
        var scenario = new ContentScenario(
            ScenarioId,
            seed.Scenarios.Single().Start,
            seed.Scenarios.Single().End,
            [Placement(first.ElementId), Placement(second.ElementId)],
            ContentTestData.Origin("content.scenario.zoc-reaction"));
        var legacy = new ContentPackDefinition(
            seed.SchemaVersion,
            seed.FormatId,
            PackId,
            seed.RulesetId,
            seed.Capabilities,
            seed.SourceIndex,
            seed.Locations,
            seed.WeatherAreaAssignments,
            seed.Edges,
            seed.Formations,
            [first, second],
            [scenario]);

        return new ContentPackV5Definition(
            legacy,
            [CombatFacts(first.ElementId), CombatFacts(second.ElementId)],
            [PlacementFacts(first.ElementId), PlacementFacts(second.ElementId)]);
    }

    private static ContentCombatElement Element(string elementId, string formationId) => new(
        elementId,
        "axis",
        formationId,
        "land.organization.battalion",
        Cna1979Movement.MotorizedMobilityId,
        20,
        ContentPlacementMode.Independent,
        ContentTestData.Origin($"content.element.{elementId}"));

    private static ContentInitialPlacement Placement(string elementId) => new(
        elementId,
        "west",
        ContentTestData.Origin($"content.placement.{elementId}"));

    private static ContentElementCombatFacts CombatFacts(string elementId)
    {
        var componentId = $"{elementId}.toe.infantry";
        return new ContentElementCombatFacts(
            elementId,
            Cna1979Combat.CombatUnitClassificationId,
            [new ContentCombatComponent(
                componentId,
                Cna1979Combat.InfantryComponentClassId,
                5,
                1,
                ContentTestData.Origin($"content.component.{componentId}"))],
            ContentTestData.Origin($"content.combat.{elementId}"));
    }

    private static ContentInitialPlacementCombatFacts PlacementFacts(string elementId)
    {
        var componentId = $"{elementId}.toe.infantry";
        return new ContentInitialPlacementCombatFacts(
            ScenarioId,
            elementId,
            [new ContentInitialComponentToe(
                componentId,
                5,
                ContentTestData.Origin($"content.seed.{componentId}"))]);
    }
}
