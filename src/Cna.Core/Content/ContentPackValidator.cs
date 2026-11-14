using Cna.Core.Rules;

namespace Cna.Core.Content;

public static class ContentPackValidator
{
    private static readonly HashSet<string> SupportedCapabilities = new(
        [
            "land.hex-topology",
            "land.formations",
            "land.element-mobility",
            "land.breakdown-cohorts",
            "land.initial-deployment",
            "land.weather-areas",
        ],
        StringComparer.Ordinal);

    public static ContentValidationResult Validate(ContentPackDefinition pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        var issues = new List<ContentValidationIssue>();

        ValidateDuplicateIds(pack, issues);
        ValidateOrigins(pack, issues);
        ValidateCapabilities(pack, issues);
        ValidateWeatherAreas(pack, issues);
        ValidateTopology(pack, issues);
        ValidateForces(pack, issues);
        ValidateScenarios(pack, issues);

        return new ContentValidationResult(issues);
    }

    private static void ValidateDuplicateIds(
        ContentPackDefinition pack,
        ICollection<ContentValidationIssue> issues)
    {
        AddDuplicateIssues(
            pack.SourceIndex.Select(value => value.SourceId),
            "/sourceIndex",
            issues);
        AddDuplicateIssues(
            pack.Locations.Select(value => value.LocationId),
            "/locations",
            issues);
        AddDuplicateIssues(
            pack.Formations.Select(value => value.FormationId),
            "/formations",
            issues);
        AddDuplicateIssues(
            pack.Elements.Select(value => value.ElementId),
            "/elements",
            issues);
        AddDuplicateIssues(
            pack.Elements
                .Select(value => value.BreakdownVehicleCohort)
                .Where(value => value is not null)
                .Select(value => value!.CohortId),
            "/elements/breakdownVehicleCohort",
            issues);
        AddDuplicateIssues(
            pack.Scenarios.Select(value => value.ScenarioId),
            "/scenarios",
            issues);
    }

    private static void AddDuplicateIssues(
        IEnumerable<string> ids,
        string collectionPath,
        ICollection<ContentValidationIssue> issues)
    {
        foreach (var group in ids.GroupBy(id => id, StringComparer.Ordinal))
        {
            if (group.Count() > 1)
            {
                Add(
                    issues,
                    "content.duplicate-id",
                    $"{collectionPath}/{group.Key}",
                    $"Identifier '{group.Key}' is duplicated in {collectionPath}.");
            }
        }
    }

    private static void ValidateOrigins(
        ContentPackDefinition pack,
        ICollection<ContentValidationIssue> issues)
    {
        var sources = pack.SourceIndex
            .GroupBy(entry => entry.SourceId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.OrderBy(entry => entry.Kind).First(),
                StringComparer.Ordinal);

        foreach (var (origin, path) in EnumerateOrigins(pack))
        {
            foreach (var reference in origin.References)
            {
                var referencePath = $"{path}/origin/references/{reference.SourceId}";

                if (!sources.TryGetValue(reference.SourceId, out var source))
                {
                    Add(
                        issues,
                        "content.unknown-reference",
                        referencePath,
                        $"Origin references unknown source '{reference.SourceId}'.");
                    continue;
                }

                var kindMatches = origin.Kind switch
                {
                    ContentOriginKind.Synthetic =>
                        source.Kind == ContentSourceKind.RepositorySynthetic,
                    ContentOriginKind.SourceDerived =>
                        source.Kind is ContentSourceKind.PublishedPrimary
                            or ContentSourceKind.AdoptedRuling,
                    _ => false,
                };

                if (!kindMatches)
                {
                    Add(
                        issues,
                        "content.invalid-origin",
                        referencePath,
                        $"Origin kind '{origin.Kind}' is incompatible with source kind '{source.Kind}'.");
                }
            }
        }
    }

    private static IEnumerable<(ContentOrigin Origin, string Path)> EnumerateOrigins(
        ContentPackDefinition pack)
    {
        foreach (var location in pack.Locations)
        {
            yield return (location.Origin, $"/locations/{location.LocationId}");
        }

        foreach (var edge in pack.Edges)
        {
            var edgePath = EdgePath(edge);
            yield return (edge.Origin, edgePath);

            foreach (var feature in edge.Features)
            {
                yield return (feature.Origin, $"{edgePath}/features/{feature.FeatureId}");
            }
        }

        foreach (var assignment in pack.WeatherAreaAssignments)
        {
            yield return (
                assignment.Origin,
                $"/weatherAreaAssignments/{assignment.LocationId}");
        }

        foreach (var formation in pack.Formations)
        {
            yield return (formation.Origin, $"/formations/{formation.FormationId}");
        }

        foreach (var element in pack.Elements)
        {
            yield return (element.Origin, $"/elements/{element.ElementId}");

            if (element.BreakdownVehicleCohort is not null)
            {
                yield return (
                    element.BreakdownVehicleCohort.Origin,
                    $"/elements/{element.ElementId}/breakdownVehicleCohort");
            }
        }

        foreach (var scenario in pack.Scenarios)
        {
            var scenarioPath = $"/scenarios/{scenario.ScenarioId}";
            yield return (scenario.Origin, scenarioPath);

            foreach (var placement in scenario.InitialPlacements)
            {
                yield return (
                    placement.Origin,
                    $"{scenarioPath}/initialPlacements/{placement.ElementId}");
            }

            foreach (var arrival in scenario.ScheduledArrivals)
            {
                yield return (arrival.Origin, $"{scenarioPath}/scheduledArrivals/{arrival.GroupId}");
            }
            foreach (var area in scenario.EntryAreas)
            {
                yield return (area.Origin, $"{scenarioPath}/entryAreas/{area.AreaId}");
            }
            foreach (var group in scenario.InitialGroups)
            {
                yield return (group.Origin, $"{scenarioPath}/initialGroups/{group.GroupId}");
            }
        }
    }

    private static void ValidateCapabilities(
        ContentPackDefinition pack,
        ICollection<ContentValidationIssue> issues)
    {
        foreach (var capability in pack.Capabilities)
        {
            if (!SupportedCapabilities.Contains(capability))
            {
                Add(
                    issues,
                    "content.unsupported-capability",
                    $"/capabilities/{capability}",
                    $"Capability '{capability}' is not supported by the current content schema.");
            }
        }

        RequireCapability(
            pack,
            "land.hex-topology",
            pack.Locations.Count > 0 || pack.Edges.Count > 0,
            issues);
        RequireCapability(
            pack,
            "land.formations",
            pack.Formations.Count > 0 || pack.Elements.Count > 0,
            issues);
        RequireCapability(
            pack,
            "land.element-mobility",
            pack.Elements.Count > 0,
            issues);
        var hasBreakdownCohorts = pack.Elements.Any(
            element => element.BreakdownVehicleCohort is not null);
        RequireCapability(
            pack,
            "land.breakdown-cohorts",
            hasBreakdownCohorts,
            issues);

        if (!hasBreakdownCohorts
            && pack.Capabilities.Contains("land.breakdown-cohorts", StringComparer.Ordinal))
        {
            Add(
                issues,
                "content.breakdown-cohort.unexpected-capability",
                "/capabilities/land.breakdown-cohorts",
                "Capability 'land.breakdown-cohorts' requires at least one vehicle cohort.");
        }
        RequireCapability(
            pack,
            "land.initial-deployment",
            pack.Scenarios.Count > 0,
            issues);
    }

    private static void RequireCapability(
        ContentPackDefinition pack,
        string capability,
        bool required,
        ICollection<ContentValidationIssue> issues)
    {
        if (required && !pack.Capabilities.Contains(capability, StringComparer.Ordinal))
        {
            Add(
                issues,
                "content.missing-capability",
                $"/capabilities/{capability}",
                $"Content fields require capability '{capability}'.");
        }
    }

    private static void ValidateWeatherAreas(
        ContentPackDefinition pack,
        ICollection<ContentValidationIssue> issues)
    {
        var hasCapability = pack.Capabilities.Contains("land.weather-areas", StringComparer.Ordinal);
        if (!hasCapability && pack.WeatherAreaAssignments.Count > 0)
        {
            Add(issues, "content.weather-area.unexpected-without-capability", "/weatherAreaAssignments", "Weather areas require capability 'land.weather-areas'.");
            return;
        }

        if (!hasCapability)
        {
            return;
        }

        var locations = pack.Locations.Select(value => value.LocationId).ToHashSet(StringComparer.Ordinal);
        foreach (var group in pack.WeatherAreaAssignments.GroupBy(value => value.LocationId, StringComparer.Ordinal))
        {
            if (group.Count() > 1)
            {
                Add(issues, "content.weather-area.duplicate-location", $"/weatherAreaAssignments/{group.Key}", "A location may have only one Weather area.");
            }
            if (!locations.Contains(group.Key))
            {
                Add(issues, "content.weather-area.unknown-location", $"/weatherAreaAssignments/{group.Key}", "Weather area references an unknown location.");
            }
        }

        foreach (var missing in locations.Except(pack.WeatherAreaAssignments.Select(value => value.LocationId), StringComparer.Ordinal))
        {
            Add(issues, "content.weather-area.missing-location", $"/weatherAreaAssignments/{missing}", "Every location requires one Weather area.");
        }
    }

    private static void ValidateTopology(
        ContentPackDefinition pack,
        ICollection<ContentValidationIssue> issues)
    {
        var locationIds = pack.Locations
            .Select(location => location.LocationId)
            .ToHashSet(StringComparer.Ordinal);
        var adjacency = locationIds.ToDictionary(
            id => id,
            _ => new HashSet<string>(StringComparer.Ordinal),
            StringComparer.Ordinal);

        foreach (var group in pack.Edges.GroupBy(
            edge => (edge.FirstLocationId, edge.SecondLocationId)))
        {
            if (group.Count() > 1)
            {
                Add(
                    issues,
                    "topology.duplicate-edge",
                    EdgePath(group.First()),
                    "The unordered edge pair is declared more than once.");
            }
        }

        foreach (var edge in pack.Edges)
        {
            var path = EdgePath(edge);

            if (string.Equals(
                edge.FirstLocationId,
                edge.SecondLocationId,
                StringComparison.Ordinal))
            {
                Add(
                    issues,
                    "topology.self-edge",
                    path,
                    "An edge cannot connect a location to itself.");
            }

            var firstExists = locationIds.Contains(edge.FirstLocationId);
            var secondExists = locationIds.Contains(edge.SecondLocationId);

            if (!firstExists)
            {
                Add(
                    issues,
                    "content.unknown-reference",
                    $"{path}/firstLocationId",
                    $"Edge references unknown location '{edge.FirstLocationId}'.");
            }

            if (!secondExists)
            {
                Add(
                    issues,
                    "content.unknown-reference",
                    $"{path}/secondLocationId",
                    $"Edge references unknown location '{edge.SecondLocationId}'.");
            }

            if (firstExists
                && secondExists
                && !string.Equals(
                    edge.FirstLocationId,
                    edge.SecondLocationId,
                    StringComparison.Ordinal))
            {
                adjacency[edge.FirstLocationId].Add(edge.SecondLocationId);
                adjacency[edge.SecondLocationId].Add(edge.FirstLocationId);
            }

            foreach (var featureGroup in edge.Features.GroupBy(
                feature => (feature.FeatureId, feature.DirectionFromLocationId)))
            {
                if (featureGroup.Count() > 1)
                {
                    Add(
                        issues,
                        "content.duplicate-id",
                        $"{path}/features/{featureGroup.Key.FeatureId}",
                        "The edge feature and direction pair is duplicated.");
                }
            }

            foreach (var feature in edge.Features)
            {
                if (feature.DirectionFromLocationId is not null
                    && !string.Equals(
                        feature.DirectionFromLocationId,
                        edge.FirstLocationId,
                        StringComparison.Ordinal)
                    && !string.Equals(
                        feature.DirectionFromLocationId,
                        edge.SecondLocationId,
                        StringComparison.Ordinal))
                {
                    Add(
                        issues,
                        "topology.invalid-direction",
                        $"{path}/features/{feature.FeatureId}/directionFromLocationId",
                        "A directional feature must name one edge endpoint.");
                }
            }
        }

        foreach (var (locationId, neighbors) in adjacency)
        {
            if (neighbors.Count > 6)
            {
                Add(
                    issues,
                    "topology.too-many-neighbors",
                    $"/locations/{locationId}",
                    $"A hex cannot have {neighbors.Count} distinct neighbors.");
            }
        }

        if (pack.Capabilities.Contains("land.hex-topology", StringComparer.Ordinal)
            && locationIds.Count > 0)
        {
            var first = locationIds.Min(StringComparer.Ordinal)!;
            var visited = new HashSet<string>(StringComparer.Ordinal) { first };
            var pending = new Queue<string>();
            pending.Enqueue(first);

            while (pending.TryDequeue(out var current))
            {
                foreach (var neighbor in adjacency[current])
                {
                    if (visited.Add(neighbor))
                    {
                        pending.Enqueue(neighbor);
                    }
                }
            }

            foreach (var disconnected in locationIds.Except(visited, StringComparer.Ordinal))
            {
                Add(
                    issues,
                    "topology.disconnected",
                    $"/locations/{disconnected}",
                    "The location is disconnected from the canonical topology component.");
            }
        }
    }

    private static void ValidateForces(
        ContentPackDefinition pack,
        ICollection<ContentValidationIssue> issues)
    {
        var hasBreakdownCapability = pack.Capabilities.Contains(
            "land.breakdown-cohorts",
            StringComparer.Ordinal);
        var formations = pack.Formations
            .GroupBy(formation => formation.FormationId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(formation => formation.SideId, StringComparer.Ordinal)
                    .ThenBy(formation => formation.ParentFormationId, StringComparer.Ordinal)
                    .ThenBy(formation => formation.OrganizationId, StringComparer.Ordinal)
                    .First(),
                StringComparer.Ordinal);

        foreach (var formation in pack.Formations)
        {
            if (formation.ParentFormationId is null)
            {
                continue;
            }

            var path = $"/formations/{formation.FormationId}/parentFormationId";

            if (!formations.TryGetValue(formation.ParentFormationId, out var parent))
            {
                Add(
                    issues,
                    "content.unknown-reference",
                    path,
                    $"Formation references unknown parent '{formation.ParentFormationId}'.");
            }
            else if (!string.Equals(formation.SideId, parent.SideId, StringComparison.Ordinal))
            {
                Add(
                    issues,
                    "formation.side-mismatch",
                    path,
                    "A formation and its parent must belong to the same side.");
            }
        }

        ValidateFormationCycles(formations, issues);

        foreach (var element in pack.Elements)
        {
            var parentPath = $"/elements/{element.ElementId}/parentFormationId";

            if (!formations.TryGetValue(element.ParentFormationId, out var parent))
            {
                Add(
                    issues,
                    "content.unknown-reference",
                    parentPath,
                    $"Element references unknown formation '{element.ParentFormationId}'.");
            }
            else if (!string.Equals(element.SideId, parent.SideId, StringComparison.Ordinal))
            {
                Add(
                    issues,
                    "formation.side-mismatch",
                    parentPath,
                    "An element and its parent formation must belong to the same side.");
            }

            if (element.BaseCapabilityPointAllowance <= 0)
            {
                Add(
                    issues,
                    "element.invalid-base-cpa",
                    $"/elements/{element.ElementId}/baseCapabilityPointAllowance",
                    "Base Capability Point Allowance must be positive.");
            }

            if (element.BreakdownVehicleCohort is not null
                && !string.Equals(
                    element.MobilityId,
                    Cna1979Movement.MotorizedMobilityId,
                    StringComparison.Ordinal))
            {
                Add(
                    issues,
                    "content.breakdown-cohort.nonmotorized-element",
                    $"/elements/{element.ElementId}/breakdownVehicleCohort",
                    "Only motorized elements may declare a vehicle breakdown cohort.");
            }

            if (hasBreakdownCapability
                && element.BreakdownVehicleCohort is null
                && string.Equals(
                    element.MobilityId,
                    Cna1979Movement.MotorizedMobilityId,
                    StringComparison.Ordinal))
            {
                Add(
                    issues,
                    "content.breakdown-cohort.missing-motorized-element",
                    $"/elements/{element.ElementId}/breakdownVehicleCohort",
                    "Every motorized element requires a vehicle breakdown cohort when the capability is declared.");
            }
        }
    }

    private static void ValidateFormationCycles(
        IReadOnlyDictionary<string, ContentFormation> formations,
        ICollection<ContentValidationIssue> issues)
    {
        var cycleMembers = new HashSet<string>(StringComparer.Ordinal);

        foreach (var formationId in formations.Keys)
        {
            var traversal = new List<string>();
            var positions = new Dictionary<string, int>(StringComparer.Ordinal);
            var currentId = formationId;

            while (formations.TryGetValue(currentId, out var current))
            {
                if (positions.TryGetValue(currentId, out var cycleStart))
                {
                    foreach (var member in traversal.Skip(cycleStart))
                    {
                        cycleMembers.Add(member);
                    }

                    break;
                }

                positions.Add(currentId, traversal.Count);
                traversal.Add(currentId);

                if (current.ParentFormationId is null)
                {
                    break;
                }

                currentId = current.ParentFormationId;
            }
        }

        foreach (var member in cycleMembers)
        {
            Add(
                issues,
                "formation.parent-cycle",
                $"/formations/{member}/parentFormationId",
                "Formation parent relationships must be acyclic.");
        }
    }

    private static void ValidateScenarios(
        ContentPackDefinition pack,
        ICollection<ContentValidationIssue> issues)
    {
        var elements = pack.Elements
            .GroupBy(element => element.ElementId, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(element => element.SideId, StringComparer.Ordinal)
                    .ThenBy(element => element.ParentFormationId, StringComparer.Ordinal)
                    .ThenBy(element => element.OrganizationId, StringComparer.Ordinal)
                    .ThenBy(element => element.BaseCapabilityPointAllowance)
                    .ThenBy(element => element.PlacementMode)
                    .First(),
                StringComparer.Ordinal);
        var locationIds = pack.Locations
            .Select(location => location.LocationId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var scenario in pack.Scenarios)
        {
            var scenarioPath = $"/scenarios/{scenario.ScenarioId}";

            if (scenario.ScheduledArrivals.Count != 0)
            {
                Add(issues, "scenario.scheduled-arrivals-unsupported", $"{scenarioPath}/scheduledArrivals",
                    "This content format cannot encode scheduled arrivals; a successor format is required.");
            }
            if (scenario.EntryAreas.Count != 0)
            {
                Add(issues, "scenario.entry-areas-unsupported", $"{scenarioPath}/entryAreas",
                    "This content format cannot encode entry areas; a successor format is required.");
            }
            if (scenario.InitialGroups.Count != 0)
            {
                Add(issues, "scenario.initial-groups-unsupported", $"{scenarioPath}/initialGroups",
                    "This content format cannot encode grouped initial elements; a successor format is required.");
            }

            if (Compare(scenario.Start, scenario.End) > 0)
            {
                Add(
                    issues,
                    "scenario.invalid-bounds",
                    $"{scenarioPath}/end",
                    "Scenario end must not precede its start.");
            }

            var placementsByElement = scenario.InitialPlacements
                .GroupBy(placement => placement.ElementId, StringComparer.Ordinal)
                .ToArray();

            foreach (var group in placementsByElement)
            {
                if (group.Count() > 1)
                {
                    Add(
                        issues,
                        "placement.duplicate-element",
                        $"{scenarioPath}/initialPlacements/{group.Key}",
                        "An independently placed element may appear only once.");
                }
            }

            foreach (var placement in scenario.InitialPlacements)
            {
                var placementPath = $"{scenarioPath}/initialPlacements/{placement.ElementId}";

                if (!elements.TryGetValue(placement.ElementId, out var element))
                {
                    Add(
                        issues,
                        "content.unknown-reference",
                        $"{placementPath}/elementId",
                        $"Placement references unknown element '{placement.ElementId}'.");
                }
                else if (element.PlacementMode == ContentPlacementMode.AttachmentOnly)
                {
                    Add(
                        issues,
                        "placement.attachment-only",
                        placementPath,
                        "An attachment-only element cannot be placed independently.");
                }

                if (!locationIds.Contains(placement.LocationId))
                {
                    Add(
                        issues,
                        "content.unknown-reference",
                        $"{placementPath}/locationId",
                        $"Placement references unknown location '{placement.LocationId}'.");
                }
            }

            var placedIds = scenario.InitialPlacements
                .Select(placement => placement.ElementId)
                .ToHashSet(StringComparer.Ordinal);

            var initialGroupIds = new HashSet<string>(StringComparer.Ordinal);
            var groupedIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in scenario.InitialGroups)
            {
                var path = $"{scenarioPath}/initialGroups/{group.GroupId}";
                if (!initialGroupIds.Add(group.GroupId))
                    Add(issues, "initial-group.duplicate-group", path, "Initial group identifiers must be unique.");
                if (group.SideId is not ("axis" or "commonwealth"))
                    Add(issues, "initial-group.invalid-side", path, "The group must belong to a supported side.");
                if (!locationIds.Contains(group.LocationId))
                    Add(issues, "content.unknown-reference", $"{path}/locationId", "The group references an unknown location.");
                foreach (var elementId in group.ElementIds)
                {
                    if (!groupedIds.Add(elementId))
                        Add(issues, "initial-group.duplicate-element", path, "An element may occur in only one initial group.");
                    if (placedIds.Contains(elementId))
                        Add(issues, "initial-group.initially-placed", path, "A grouped element cannot have an independent placement.");
                    if (!elements.TryGetValue(elementId, out var element))
                        Add(issues, "content.unknown-reference", $"{path}/elementIds/{elementId}", "The group references an unknown element.");
                    else if (element.SideId != group.SideId)
                        Add(issues, "initial-group.element-owner", $"{path}/elementIds/{elementId}", "A grouped element must be owned by its group.");
                }
            }

            var scheduledIds = new HashSet<string>(StringComparer.Ordinal);
            var groupIds = new HashSet<string>(StringComparer.Ordinal);
            var areaIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var area in scenario.EntryAreas)
            {
                var path = $"{scenarioPath}/entryAreas/{area.AreaId}";
                if (!areaIds.Add(area.AreaId))
                    Add(issues, "arrival.duplicate-entry-area", path, "Entry area identifiers must be unique.");
                foreach (var locationId in area.LocationIds)
                    if (!locationIds.Contains(locationId))
                        Add(issues, "content.unknown-reference", $"{path}/locationIds/{locationId}",
                            "The entry area references an unknown location.");
            }
            foreach (var arrival in scenario.ScheduledArrivals)
            {
                var path = $"{scenarioPath}/scheduledArrivals/{arrival.GroupId}";
                if (!groupIds.Add(arrival.GroupId))
                    Add(issues, "arrival.duplicate-group", path, "Arrival group identifiers must be unique.");
                if (arrival.SideId is not ("axis" or "commonwealth"))
                    Add(issues, "arrival.invalid-side", path, "The arrival must belong to a supported side.");
                if (arrival.EntryAreaId is not null && !areaIds.Contains(arrival.EntryAreaId))
                    Add(issues, "content.unknown-reference", $"{path}/entryAreaId", "The arrival references an unknown entry area.");
                if (Compare(arrival.Arrival, scenario.Start) < 0 || Compare(arrival.Arrival, scenario.End) > 0)
                    Add(issues, "arrival.outside-scenario", path, "The arrival must occur within the scenario.");
                foreach (var elementId in arrival.ElementIds)
                {
                    if (!scheduledIds.Add(elementId))
                        Add(issues, "arrival.duplicate-element", path, "An element may be scheduled only once.");
                    if (placedIds.Contains(elementId))
                        Add(issues, "arrival.initially-placed", path, "An arriving element cannot already be placed.");
                    if (groupedIds.Contains(elementId))
                        Add(issues, "arrival.initially-grouped", path, "An arriving element cannot already be in an initial group.");
                    if (!elements.TryGetValue(elementId, out var element))
                        Add(issues, "content.unknown-reference", $"{path}/elementIds/{elementId}", "The arrival references an unknown element.");
                    else if (element.SideId != arrival.SideId || element.PlacementMode != ContentPlacementMode.Independent)
                        Add(issues, "arrival.element-owner-or-placement", $"{path}/elementIds/{elementId}",
                            "An arriving element must be independent and owned by its group.");
                }
            }

            foreach (var element in pack.Elements.Where(
                element => element.PlacementMode == ContentPlacementMode.Independent))
            {
                if (!placedIds.Contains(element.ElementId))
                {
                    Add(
                        issues,
                        "placement.missing-element",
                        $"{scenarioPath}/initialPlacements/{element.ElementId}",
                        "An independently placed element requires one initial placement.");
                }
            }
        }
    }

    private static int Compare(ContentScenarioBoundary first, ContentScenarioBoundary second)
    {
        var turnComparison = first.GameTurn.CompareTo(second.GameTurn);
        return turnComparison != 0
            ? turnComparison
            : first.OperationStage.CompareTo(second.OperationStage);
    }

    private static string EdgePath(ContentHexEdge edge) =>
        $"/edges/{edge.FirstLocationId}|{edge.SecondLocationId}";

    private static void Add(
        ICollection<ContentValidationIssue> issues,
        string code,
        string path,
        string message) => issues.Add(new ContentValidationIssue(code, path, message));
}
