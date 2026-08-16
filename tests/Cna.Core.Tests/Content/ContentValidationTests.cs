using Cna.Core.Content;

namespace Cna.Core.Tests.Content;

public sealed class ContentValidationTests
{
    [Fact]
    public void MinimalPackPassesPackAndCna1979CompatibilityValidation()
    {
        var pack = ContentTestData.CreateMinimalPack();

        var packResult = ContentPackValidator.Validate(pack);
        var compatibilityResult = Cna1979ContentCompatibilityValidator.Validate(pack);

        Assert.True(packResult.IsValid);
        Assert.Empty(packResult.Issues);
        Assert.True(compatibilityResult.IsValid);
        Assert.Empty(compatibilityResult.Issues);
    }

    [Fact]
    public void ValidatorReturnsAllDiscoverableIssuesInCanonicalOrder()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var publishedSource = new ContentSourceIndexEntry(
            "published-source",
            ContentSourceKind.PublishedPrimary);
        var invalidOrigin = ContentTestData.Origin(
            "content.invalid-origin",
            publishedSource.SourceId);
        var unknownOrigin = ContentTestData.Origin(
            "content.unknown-origin",
            "missing-source");
        var west = baseline.Locations.Single(location => location.LocationId == "west");
        var locations = new ContentHex[]
        {
            west,
            west,
            new("isolated", "land.terrain.clear", null, invalidOrigin),
        };
        var edges = new ContentHexEdge[]
        {
            new("west", "west", [], unknownOrigin),
            new("west", "missing", [], ContentTestData.Origin("content.edge.missing")),
            new("west", "isolated", [], ContentTestData.Origin("content.edge.duplicate.a")),
            new("isolated", "west", [], ContentTestData.Origin("content.edge.duplicate.b")),
        };
        var formations = new ContentFormation[]
        {
            new(
                "formation-a",
                "axis",
                "formation-b",
                "land.organization.regiment",
                ContentTestData.Origin("content.formation.a")),
            new(
                "formation-b",
                "commonwealth",
                "formation-a",
                "land.organization.regiment",
                ContentTestData.Origin("content.formation.b")),
        };
        var elements = new ContentCombatElement[]
        {
            new(
                "element-invalid",
                "commonwealth",
                "formation-a",
                "land.organization.battalion",
                0,
                ContentPlacementMode.Independent,
                ContentTestData.Origin("content.element.invalid")),
            new(
                "element-attachment",
                "axis",
                "formation-a",
                "land.organization.battalion",
                10,
                ContentPlacementMode.AttachmentOnly,
                ContentTestData.Origin("content.element.attachment")),
            new(
                "element-unplaced",
                "axis",
                "formation-a",
                "land.organization.battalion",
                10,
                ContentPlacementMode.Independent,
                ContentTestData.Origin("content.element.unplaced")),
        };
        var scenario = new ContentScenario(
            "invalid-lab",
            new ContentScenarioBoundary(2, 1),
            new ContentScenarioBoundary(1, 3),
            [
                new ContentInitialPlacement(
                    "element-invalid",
                    "west",
                    ContentTestData.Origin("content.placement.invalid.a")),
                new ContentInitialPlacement(
                    "element-invalid",
                    "west",
                    ContentTestData.Origin("content.placement.invalid.b")),
                new ContentInitialPlacement(
                    "element-attachment",
                    "west",
                    ContentTestData.Origin("content.placement.attachment")),
                new ContentInitialPlacement(
                    "missing-element",
                    "missing-location",
                    ContentTestData.Origin("content.placement.missing")),
            ],
            ContentTestData.Origin("content.scenario.invalid"));
        var pack = ContentTestData.Copy(
            baseline,
            capabilities: [.. baseline.Capabilities, "land.air"],
            sourceIndex:
            [
                .. baseline.SourceIndex,
                baseline.SourceIndex[0],
                publishedSource,
            ],
            locations: locations,
            edges: edges,
            formations: formations,
            elements: elements,
            scenarios: [scenario]);

        var result = ContentPackValidator.Validate(pack);

        Assert.False(result.IsValid);
        Assert.Equal(
            result.Issues
                .OrderBy(issue => issue.Path, StringComparer.Ordinal)
                .ThenBy(issue => issue.Code, StringComparer.Ordinal)
                .ThenBy(issue => issue.Message, StringComparer.Ordinal),
            result.Issues);
        Assert.Equal(result.Issues.Distinct().Count(), result.Issues.Count);
        AssertIssue(result, "content.duplicate-id", "/locations/west");
        AssertIssue(result, "content.duplicate-id", "/sourceIndex/sandtable-rules-lab");
        AssertIssue(result, "content.unsupported-capability", "/capabilities/land.air");
        AssertIssue(result, "content.invalid-origin", "/locations/isolated/origin/references/published-source");
        AssertIssue(result, "content.unknown-reference", "/edges/west|west/origin/references/missing-source");
        AssertIssue(result, "topology.self-edge", "/edges/west|west");
        AssertIssue(result, "topology.duplicate-edge", "/edges/isolated|west");
        AssertIssue(result, "content.unknown-reference", "/edges/missing|west/firstLocationId");
        AssertIssue(result, "formation.parent-cycle", "/formations/formation-a/parentFormationId");
        AssertIssue(result, "formation.side-mismatch", "/formations/formation-a/parentFormationId");
        AssertIssue(result, "formation.side-mismatch", "/elements/element-invalid/parentFormationId");
        AssertIssue(result, "element.invalid-base-cpa", "/elements/element-invalid/baseCapabilityPointAllowance");
        AssertIssue(result, "scenario.invalid-bounds", "/scenarios/invalid-lab/end");
        AssertIssue(result, "placement.duplicate-element", "/scenarios/invalid-lab/initialPlacements/element-invalid");
        AssertIssue(result, "placement.attachment-only", "/scenarios/invalid-lab/initialPlacements/element-attachment");
        AssertIssue(result, "placement.missing-element", "/scenarios/invalid-lab/initialPlacements/element-unplaced");
        AssertIssue(result, "content.unknown-reference", "/scenarios/invalid-lab/initialPlacements/missing-element/elementId");
        AssertIssue(result, "content.unknown-reference", "/scenarios/invalid-lab/initialPlacements/missing-element/locationId");
    }

    [Fact]
    public void TopologyValidationFindsSeventhNeighborAndDisconnectedLocations()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var center = new ContentHex(
            "center",
            "land.terrain.clear",
            null,
            ContentTestData.Origin("content.hex.center"));
        var neighbors = Enumerable.Range(1, 7)
            .Select(index => new ContentHex(
                $"neighbor-{index}",
                "land.terrain.clear",
                null,
                ContentTestData.Origin($"content.hex.neighbor-{index}")))
            .ToArray();
        var disconnected = new ContentHex(
            "disconnected",
            "land.terrain.clear",
            null,
            ContentTestData.Origin("content.hex.disconnected"));
        var edges = neighbors
            .Select(neighbor => new ContentHexEdge(
                center.LocationId,
                neighbor.LocationId,
                [],
                ContentTestData.Origin($"content.edge.{neighbor.LocationId}")))
            .ToArray();
        var pack = ContentTestData.Copy(
            baseline,
            locations: [center, .. neighbors, disconnected],
            edges: edges,
            formations: [],
            elements: [],
            scenarios: []);

        var result = ContentPackValidator.Validate(pack);

        AssertIssue(result, "topology.too-many-neighbors", "/locations/center");
        AssertIssue(result, "topology.disconnected", "/locations/disconnected");
    }

    [Fact]
    public void CompatibilityValidationRejectsUnknownVocabularyAndDirectionPolicies()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var locations = new ContentHex[]
        {
            new(
                "west",
                "land.terrain.unknown",
                null,
                ContentTestData.Origin("content.hex.west")),
            baseline.Locations[0],
        };
        var edges = new ContentHexEdge[]
        {
            new(
                "west",
                "east",
                [
                    new ContentEdgeFeature(
                        "land.edge.unknown",
                        null,
                        ContentTestData.Origin("content.edge.unknown")),
                    new ContentEdgeFeature(
                        "land.edge.slope",
                        null,
                        ContentTestData.Origin("content.edge.slope")),
                    new ContentEdgeFeature(
                        "land.edge.road",
                        "west",
                        ContentTestData.Origin("content.edge.road")),
                ],
                ContentTestData.Origin("content.edge.east-west")),
        };
        var formations = new ContentFormation[]
        {
            new(
                "unknown-formation",
                "unknown-side",
                null,
                "land.organization.unknown",
                ContentTestData.Origin("content.formation.unknown")),
        };
        var elements = new ContentCombatElement[]
        {
            new(
                "unknown-element",
                "unknown-side",
                "unknown-formation",
                "land.organization.unknown",
                10,
                ContentPlacementMode.Independent,
                ContentTestData.Origin("content.element.unknown")),
        };
        var scenario = new ContentScenario(
            "unknown-lab",
            new ContentScenarioBoundary(1, 1),
            new ContentScenarioBoundary(1, 1),
            [new ContentInitialPlacement(
                "unknown-element",
                "west",
                ContentTestData.Origin("content.placement.unknown"))],
            ContentTestData.Origin("content.scenario.unknown"));
        var pack = ContentTestData.Copy(
            baseline,
            locations: locations,
            edges: edges,
            formations: formations,
            elements: elements,
            scenarios: [scenario]);

        var result = Cna1979ContentCompatibilityValidator.Validate(pack);

        AssertIssue(result, "vocabulary.unknown-id", "/locations/west/terrainId");
        AssertIssue(result, "vocabulary.unknown-id", "/edges/east|west/features/land.edge.unknown/featureId");
        AssertIssue(result, "topology.invalid-direction", "/edges/east|west/features/land.edge.slope/directionFromLocationId");
        AssertIssue(result, "topology.invalid-direction", "/edges/east|west/features/land.edge.road/directionFromLocationId");
        AssertIssue(result, "vocabulary.unknown-id", "/formations/unknown-formation/sideId");
        AssertIssue(result, "vocabulary.unknown-id", "/formations/unknown-formation/organizationId");
        AssertIssue(result, "vocabulary.unknown-id", "/elements/unknown-element/sideId");
        AssertIssue(result, "vocabulary.unknown-id", "/elements/unknown-element/organizationId");
    }

    [Fact]
    public void CompatibilityValidationRequiresTheExactRulesetId()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var pack = ContentTestData.Copy(baseline, rulesetId: "cna-1979.2");

        var result = Cna1979ContentCompatibilityValidator.Validate(pack);

        AssertIssue(result, "vocabulary.unknown-id", "/rulesetId");
    }

    [Fact]
    public void InvalidDuplicateOrderingCannotChangeTheIssueLedger()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var repositorySource = baseline.SourceIndex[0];
        var conflictingSource = new ContentSourceIndexEntry(
            repositorySource.SourceId,
            ContentSourceKind.PublishedPrimary);
        var axisFormation = baseline.Formations[0];
        var conflictingFormation = new ContentFormation(
            axisFormation.FormationId,
            "commonwealth",
            axisFormation.ParentFormationId,
            axisFormation.OrganizationId,
            axisFormation.Origin);
        var independentElement = baseline.Elements[0];
        var conflictingElement = new ContentCombatElement(
            independentElement.ElementId,
            independentElement.SideId,
            independentElement.ParentFormationId,
            independentElement.OrganizationId,
            independentElement.BaseCapabilityPointAllowance,
            ContentPlacementMode.AttachmentOnly,
            independentElement.Origin);
        var first = ContentTestData.Copy(
            baseline,
            sourceIndex: [repositorySource, conflictingSource],
            formations: [axisFormation, conflictingFormation],
            elements: [independentElement, conflictingElement]);
        var reversed = ContentTestData.Copy(
            baseline,
            sourceIndex: [conflictingSource, repositorySource],
            formations: [conflictingFormation, axisFormation],
            elements: [conflictingElement, independentElement]);

        Assert.Equal(
            ContentPackValidator.Validate(first).Issues,
            ContentPackValidator.Validate(reversed).Issues);
    }

    private static void AssertIssue(
        ContentValidationResult result,
        string code,
        string path) => Assert.Contains(
            result.Issues,
            issue => issue.Code == code && issue.Path == path);
}
