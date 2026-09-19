namespace Cna.Core.Content;

internal static class ContentPackV7Validator
{
    public static ContentValidationResult Validate(ContentPackV7Definition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var issues = new List<ContentValidationIssue>();
        void Add(string code, string path, string message) => issues.Add(new(code, path, message));

        if (definition.PackId != ContentPackV7Definition.SupportedPackId)
            Add(CombatContentDiagnostics.Profile, "/packId", "Only the certified synthetic Combat pack is supported.");
        if (definition.RulesetId != ContentPackV7Definition.SupportedRulesetId
            || definition.CapabilityProfileId != ContentPackV7Definition.SupportedCapabilityProfileId
            || !definition.Capabilities.SequenceEqual(ContentPackV7Definition.RequiredCapabilities))
        {
            Add(CombatContentDiagnostics.Identity, "/capabilities", "Content7 identity is unsupported.");
        }

        var sources = Unique(definition.SourceIndex, value => value.SourceId, "/sourceIndex", "sourceId", issues);
        if (sources.Count != 1 || !sources.ContainsKey("sandtable-rules-lab"))
            Add(CombatContentDiagnostics.Provenance, "/sourceIndex", "Exactly one certified source is required.");
        for (var index = 0; index < definition.SourceIndex.Count; index++)
        {
            if (definition.SourceIndex[index].Kind != ContentSourceKind.RepositorySynthetic)
                Add(CombatContentDiagnostics.Provenance, $"/sourceIndex/{index}/kind", "Only repository-synthetic sources are supported.");
        }
        foreach (var (origin, path) in Origins(definition))
        {
            if (origin.Kind != ContentOriginKind.Synthetic)
                Add(CombatContentDiagnostics.Provenance, path + "/kind", "Only synthetic origins are supported.");
            if (origin.References.Count != 1)
                Add(CombatContentDiagnostics.Provenance, path + "/references", "Exactly one source reference is required.");
            else if (!sources.ContainsKey(origin.References[0].SourceId))
                Add(CombatContentDiagnostics.Provenance, path + "/references/0/sourceId", "Origin source is not indexed.");
        }

        var locations = Unique(definition.Locations, value => value.LocationId, "/locations", "locationId", issues);
        var weather = Unique(definition.WeatherAreaAssignments, value => value.LocationId,
            "/weatherAreaAssignments", "locationId", issues);
        if (!weather.Keys.Order(StringComparer.Ordinal).SequenceEqual(locations.Keys.Order(StringComparer.Ordinal)))
            Add(CombatContentDiagnostics.Reference, "/weatherAreaAssignments", "Every location requires one Weather assignment.");

        var edgeKeys = new HashSet<(string, string)>();
        for (var index = 0; index < definition.Edges.Count; index++)
        {
            var edge = definition.Edges[index];
            if (!locations.ContainsKey(edge.FirstLocationId))
                Add(CombatContentDiagnostics.Reference, $"/edges/{index}/firstLocationId", "Unknown edge endpoint.");
            if (!locations.ContainsKey(edge.SecondLocationId))
                Add(CombatContentDiagnostics.Reference, $"/edges/{index}/secondLocationId", "Unknown edge endpoint.");
            if (!edgeKeys.Add((edge.FirstLocationId, edge.SecondLocationId)))
                Add(CombatContentDiagnostics.Reference, $"/edges/{index}", "Duplicate edge.");
            if (edge.Features.Count != 0)
                Add(CombatContentDiagnostics.Profile, $"/edges/{index}/features", "Selected Combat edges are featureless.");
        }

        var formations = Unique(definition.Formations, value => value.FormationId, "/formations", "formationId", issues);
        var elements = Unique(definition.Elements, value => value.ElementId, "/elements", "elementId", issues);
        var parentIds = new HashSet<string>(StringComparer.Ordinal);
        var componentIds = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < definition.Formations.Count; index++)
        {
            if (definition.Formations[index].SideId is not ("axis" or "commonwealth"))
                Add(CombatContentDiagnostics.Reference, $"/formations/{index}/sideId", "Unsupported formation side.");
        }
        for (var index = 0; index < definition.Elements.Count; index++)
        {
            var element = definition.Elements[index];
            if (!formations.TryGetValue(element.ParentFormationId, out var parent)
                || parent.SideId != element.SideId
                || !parentIds.Add(element.ParentFormationId))
            {
                Add(CombatContentDiagnostics.Reference, $"/elements/{index}/parentFormationId", "Element parent is missing, reused, or belongs to another side.");
            }
            for (var componentIndex = 0; componentIndex < element.Components.Count; componentIndex++)
            {
                if (!componentIds.Add(element.Components[componentIndex].ComponentId))
                    Add(CombatContentDiagnostics.Reference, $"/elements/{index}/components/{componentIndex}/componentId", "Component ID must be unique pack-wide.");
            }
        }
        if (!parentIds.SetEquals(formations.Keys))
            Add(CombatContentDiagnostics.Reference, "/formations", "Every formation requires exactly one child element.");

        _ = Unique(definition.Scenarios, value => value.ScenarioId, "/scenarios", "scenarioId", issues);
        for (var scenarioIndex = 0; scenarioIndex < definition.Scenarios.Count; scenarioIndex++)
            ValidateScenarioReferences(definition.Scenarios[scenarioIndex], scenarioIndex, elements, locations, issues);

        if (issues.Count > 0)
            return new ContentValidationResult(issues);

        if (definition.Locations.Count != 6)
            Add(CombatContentDiagnostics.Profile, "/locations", "Selected profile requires six locations.");
        if (definition.Edges.Count != 5)
            Add(CombatContentDiagnostics.Profile, "/edges", "Selected profile requires five edges.");
        if (definition.Formations.Count != 2)
            Add(CombatContentDiagnostics.Profile, "/formations", "Selected profile requires two formations.");
        if (definition.Elements.Count != 2)
            Add(CombatContentDiagnostics.Profile, "/elements", "Selected profile requires two elements.");
        if (definition.Scenarios.Count != 1)
            Add(CombatContentDiagnostics.Profile, "/scenarios", "Selected profile requires one scenario.");
        if (!definition.Formations.Select(value => value.SideId).Order(StringComparer.Ordinal)
            .SequenceEqual(["axis", "commonwealth"]))
        {
            Add(CombatContentDiagnostics.Profile, "/formations", "Selected profile requires one formation per side.");
        }

        for (var index = 0; index < definition.Locations.Count; index++)
        {
            if (definition.Locations[index].SourceCoordinate is not null)
                Add(CombatContentDiagnostics.Profile, $"/locations/{index}/sourceCoordinate", "Synthetic locations cannot carry source coordinates.");
            if (definition.Locations[index].TerrainId != "land.terrain.clear")
                Add(CombatContentDiagnostics.Profile, $"/locations/{index}/terrainId", "Only Clear terrain is supported.");
        }
        for (var index = 0; index < definition.WeatherAreaAssignments.Count; index++)
        {
            if (definition.WeatherAreaAssignments[index].WeatherArea != ContentWeatherArea.A)
                Add(CombatContentDiagnostics.Profile, $"/weatherAreaAssignments/{index}/weatherArea", "Only Weather area a is supported.");
        }
        for (var index = 0; index < definition.Formations.Count; index++)
        {
            var formation = definition.Formations[index];
            if (formation.ParentFormationId is not null)
                Add(CombatContentDiagnostics.Profile, $"/formations/{index}/parentFormationId", "Selected formations must be roots.");
            if (formation.OrganizationId != "land.organization.battalion")
                Add(CombatContentDiagnostics.Profile, $"/formations/{index}/organizationId", "Only battalion formations are supported.");
            if (formation.BasicMorale != 0)
                Add(CombatContentDiagnostics.Profile, $"/formations/{index}/basicMorale", "Selected Basic Morale is zero.");
        }
        for (var index = 0; index < definition.Elements.Count; index++)
            ValidateElementProfile(definition.Elements[index], index, issues);

        if (issues.Count > 0 || definition.Scenarios.Count != 1)
            return new ContentValidationResult(issues);

        var scenario = definition.Scenarios[0];
        for (var index = 0; index < scenario.RetreatSupplyAnchors.Count; index++)
        {
            if (scenario.RetreatSupplyAnchors[index].Kind != "friendly-supply-direction")
                Add(CombatContentDiagnostics.Profile, $"/scenarios/0/retreatSupplyAnchors/{index}/kind", "Unsupported retreat-supply anchor kind.");
        }
        if (scenario.Start != scenario.End)
            Add(CombatContentDiagnostics.Seed, "/scenarios/0/end", "Scenario must be stage-bounded.");
        for (var index = 0; index < scenario.InitialPlacements.Count; index++)
        {
            var placement = scenario.InitialPlacements[index];
            var path = $"/scenarios/0/initialPlacements/{index}";
            if (placement.InitialComponentToes[0].CurrentToe != 10)
                Add(CombatContentDiagnostics.Seed, path + "/initialComponentToes/0/currentToe", "Initial TOE must equal ten.");
            if (placement.InitialAmmunition.Points != 10)
                Add(CombatContentDiagnostics.Seed, path + "/initialAmmunition/points", "Initial Ammo must equal ten.");
            var readiness = placement.InitialReadiness;
            if (readiness.GameTurn != scenario.Start.GameTurn)
                Add(CombatContentDiagnostics.Seed, path + "/initialReadiness/gameTurn", "Readiness turn must match scenario start.");
            if (readiness.OperationStage != scenario.Start.OperationStage)
                Add(CombatContentDiagnostics.Seed, path + "/initialReadiness/operationStage", "Readiness stage must match scenario start.");
            if (readiness.WaterStatus != "distributed-for-stage")
                Add(CombatContentDiagnostics.Seed, path + "/initialReadiness/waterStatus", "Water must be distributed for this stage.");
            if (readiness.StoresStatus != "distributed-for-stage")
                Add(CombatContentDiagnostics.Seed, path + "/initialReadiness/storesStatus", "Stores must be distributed for this stage.");
            if (readiness.Pinned)
                Add(CombatContentDiagnostics.Seed, path + "/initialReadiness/pinned", "Initial force cannot be pinned.");
        }
        if (issues.Count == 0)
            ValidateGeometry(definition, issues);
        return new ContentValidationResult(issues);
    }

    private static void ValidateScenarioReferences(
        ContentCombatScenario scenario,
        int scenarioIndex,
        Dictionary<string, ContentElementCombatFactsV2> elements,
        Dictionary<string, ContentHex> locations,
        List<ContentValidationIssue> issues)
    {
        var path = $"/scenarios/{scenarioIndex}";
        var placements = Unique(scenario.InitialPlacements, value => value.ElementId,
            path + "/initialPlacements", "elementId", issues);
        if (!placements.Keys.Order(StringComparer.Ordinal).SequenceEqual(elements.Keys.Order(StringComparer.Ordinal)))
            issues.Add(new(CombatContentDiagnostics.Reference, path + "/initialPlacements", "Scenario must place complete inventory."));
        for (var index = 0; index < scenario.InitialPlacements.Count; index++)
        {
            var placement = scenario.InitialPlacements[index];
            var placementPath = $"{path}/initialPlacements/{index}";
            if (!locations.ContainsKey(placement.LocationId))
                issues.Add(new(CombatContentDiagnostics.Reference, placementPath + "/locationId", "Unknown placement location."));
            var toes = Unique(placement.InitialComponentToes, value => value.ComponentId,
                placementPath + "/initialComponentToes", "componentId", issues);
            if (elements.TryGetValue(placement.ElementId, out var element)
                && !toes.Keys.Order(StringComparer.Ordinal).SequenceEqual(
                    element.Components.Select(value => value.ComponentId).Order(StringComparer.Ordinal)))
            {
                issues.Add(new(CombatContentDiagnostics.Reference, placementPath + "/initialComponentToes", "TOE seeds must match owned components."));
            }
        }
        var anchors = Unique(scenario.RetreatSupplyAnchors, value => value.SideId,
            path + "/retreatSupplyAnchors", "sideId", issues);
        if (!anchors.Keys.Order(StringComparer.Ordinal).SequenceEqual(["axis", "commonwealth"]))
            issues.Add(new(CombatContentDiagnostics.Reference, path + "/retreatSupplyAnchors", "One anchor per supported side is required."));
        for (var index = 0; index < scenario.RetreatSupplyAnchors.Count; index++)
        {
            if (!locations.ContainsKey(scenario.RetreatSupplyAnchors[index].LocationId))
                issues.Add(new(CombatContentDiagnostics.Reference, $"{path}/retreatSupplyAnchors/{index}/locationId", "Unknown anchor location."));
        }
    }

    private static void ValidateElementProfile(
        ContentElementCombatFactsV2 element,
        int index,
        List<ContentValidationIssue> issues)
    {
        var path = $"/elements/{index}";
        void Add(string suffix, string message) => issues.Add(new(CombatContentDiagnostics.Profile, path + suffix, message));
        if (element.OrganizationId != "land.organization.battalion") Add("/organizationId", "Only battalion elements are supported.");
        if (element.MobilityId != "land.mobility.non-motorized") Add("/mobilityId", "Only non-motorized infantry is supported.");
        if (element.BaseCapabilityPointAllowance != 10) Add("/baseCapabilityPointAllowance", "Selected CPA is ten.");
        if (element.PlacementMode != ContentPlacementMode.Independent) Add("/placementMode", "Only independent placement is supported.");
        if (element.CombatClassificationId != "land.combat-classification.combat-unit") Add("/combatClassificationId", "Only combat-unit classification is supported.");
        if (element.Components.Count != 1) Add("/components", "Exactly one infantry component is required.");
        for (var componentIndex = 0; componentIndex < element.Components.Count; componentIndex++)
        {
            var component = element.Components[componentIndex];
            var componentPath = $"{path}/components/{componentIndex}";
            if (component.ComponentClassId != "land.combat-component.infantry")
                issues.Add(new(CombatContentDiagnostics.Profile, componentPath + "/componentClassId", "Only infantry components are supported."));
            if (component.MaximumToe != 10)
                issues.Add(new(CombatContentDiagnostics.Profile, componentPath + "/maximumToe", "Selected maximum TOE is ten."));
            if (component.OffensiveCloseAssaultRating != 1)
                issues.Add(new(CombatContentDiagnostics.Profile, componentPath + "/offensiveCloseAssaultRating", "Selected offensive rating is one."));
            if (component.DefensiveCloseAssaultRating != 1)
                issues.Add(new(CombatContentDiagnostics.Profile, componentPath + "/defensiveCloseAssaultRating", "Selected defensive rating is one."));
        }
    }

    private static void ValidateGeometry(ContentPackV7Definition definition, List<ContentValidationIssue> issues)
    {
        var graph = definition.Locations.ToDictionary(
            value => value.LocationId,
            _ => new HashSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal);
        foreach (var edge in definition.Edges)
        {
            graph[edge.FirstLocationId].Add(edge.SecondLocationId);
            graph[edge.SecondLocationId].Add(edge.FirstLocationId);
        }
        if (!graph.Values.Select(value => value.Count).Order().SequenceEqual([1, 1, 2, 2, 2, 2]))
        {
            issues.Add(new(CombatContentDiagnostics.Profile, "/edges", "Selected geometry must be an unbranched line."));
            return;
        }
        var scenario = definition.Scenarios[0];
        var sides = definition.Elements.ToDictionary(value => value.ElementId, value => value.SideId, StringComparer.Ordinal);
        var initial = scenario.InitialPlacements.ToDictionary(value => sides[value.ElementId], value => value.LocationId, StringComparer.Ordinal);
        var anchors = scenario.RetreatSupplyAnchors.ToDictionary(value => value.SideId, value => value.LocationId, StringComparer.Ordinal);
        for (var index = 0; index < scenario.RetreatSupplyAnchors.Count; index++)
        {
            var anchor = scenario.RetreatSupplyAnchors[index];
            if (graph[anchor.LocationId].Count != 1 || Route(graph, initial[anchor.SideId], anchor.LocationId).Count != 3)
            {
                issues.Add(new(CombatContentDiagnostics.Profile,
                    $"/scenarios/0/retreatSupplyAnchors/{index}/locationId",
                    "Anchor must be endpoint two edges from its force."));
                return;
            }
        }
        if (Route(graph, initial["axis"], initial["commonwealth"]).Count != 2)
            issues.Add(new(CombatContentDiagnostics.Profile, "/scenarios/0/initialPlacements", "Initial forces must be adjacent."));
    }

    private static IReadOnlyList<string> Route(
        Dictionary<string, HashSet<string>> graph,
        string start,
        string end)
    {
        var pending = new Queue<IReadOnlyList<string>>();
        pending.Enqueue([start]);
        var visited = new HashSet<string>(StringComparer.Ordinal) { start };
        while (pending.TryDequeue(out var path))
        {
            if (path[^1] == end) return path;
            foreach (var neighbor in graph[path[^1]].Order(StringComparer.Ordinal))
            {
                if (visited.Add(neighbor)) pending.Enqueue([.. path, neighbor]);
            }
        }
        return [];
    }

    private static Dictionary<string, T> Unique<T>(
        IReadOnlyList<T> values,
        Func<T, string> key,
        string path,
        string property,
        List<ContentValidationIssue> issues)
    {
        var result = new Dictionary<string, T>(StringComparer.Ordinal);
        for (var index = 0; index < values.Count; index++)
        {
            if (!result.TryAdd(key(values[index]), values[index]))
                issues.Add(new(CombatContentDiagnostics.Reference, $"{path}/{index}/{property}", "Duplicate identity."));
        }
        return result;
    }

    private static IEnumerable<(ContentOrigin Origin, string Path)> Origins(ContentPackV7Definition definition)
    {
        for (var index = 0; index < definition.Locations.Count; index++) yield return (definition.Locations[index].Origin, $"/locations/{index}/origin");
        for (var index = 0; index < definition.WeatherAreaAssignments.Count; index++) yield return (definition.WeatherAreaAssignments[index].Origin, $"/weatherAreaAssignments/{index}/origin");
        for (var index = 0; index < definition.Edges.Count; index++) yield return (definition.Edges[index].Origin, $"/edges/{index}/origin");
        for (var index = 0; index < definition.Formations.Count; index++)
        {
            yield return (definition.Formations[index].BasicMoraleOrigin, $"/formations/{index}/basicMoraleOrigin");
            yield return (definition.Formations[index].Origin, $"/formations/{index}/origin");
        }
        for (var index = 0; index < definition.Elements.Count; index++)
        {
            var element = definition.Elements[index];
            yield return (element.CombatOrigin, $"/elements/{index}/combatOrigin");
            for (var component = 0; component < element.Components.Count; component++)
                yield return (element.Components[component].Origin, $"/elements/{index}/components/{component}/origin");
            yield return (element.Origin, $"/elements/{index}/origin");
        }
        for (var scenarioIndex = 0; scenarioIndex < definition.Scenarios.Count; scenarioIndex++)
        {
            var scenario = definition.Scenarios[scenarioIndex];
            for (var placementIndex = 0; placementIndex < scenario.InitialPlacements.Count; placementIndex++)
            {
                var placement = scenario.InitialPlacements[placementIndex];
                for (var toe = 0; toe < placement.InitialComponentToes.Count; toe++)
                    yield return (placement.InitialComponentToes[toe].Origin, $"/scenarios/{scenarioIndex}/initialPlacements/{placementIndex}/initialComponentToes/{toe}/origin");
                yield return (placement.InitialAmmunition.Origin, $"/scenarios/{scenarioIndex}/initialPlacements/{placementIndex}/initialAmmunition/origin");
                yield return (placement.InitialReadiness.Origin, $"/scenarios/{scenarioIndex}/initialPlacements/{placementIndex}/initialReadiness/origin");
                yield return (placement.Origin, $"/scenarios/{scenarioIndex}/initialPlacements/{placementIndex}/origin");
            }
            for (var anchor = 0; anchor < scenario.RetreatSupplyAnchors.Count; anchor++)
                yield return (scenario.RetreatSupplyAnchors[anchor].Origin, $"/scenarios/{scenarioIndex}/retreatSupplyAnchors/{anchor}/origin");
            yield return (scenario.Origin, $"/scenarios/{scenarioIndex}/origin");
        }
    }
}
