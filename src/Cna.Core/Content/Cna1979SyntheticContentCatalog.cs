using Cna.Core.Rules;

namespace Cna.Core.Content;

public enum ContentCatalogRejectionReason
{
    None,
    UnknownPackId,
    HashMismatch,
}

public sealed class ContentCatalogResolution
{
    private ContentCatalogResolution(
        ContentPackArtifact? artifact,
        ContentCatalogRejectionReason rejectionReason)
    {
        Artifact = artifact;
        RejectionReason = rejectionReason;
    }

    public bool IsResolved => Artifact is not null;

    public ContentPackArtifact? Artifact { get; }

    public ContentCatalogRejectionReason RejectionReason { get; }

    public static ContentCatalogResolution Resolved(ContentPackArtifact artifact) =>
        new(artifact, ContentCatalogRejectionReason.None);

    public static ContentCatalogResolution Rejected(ContentCatalogRejectionReason reason)
    {
        if (reason == ContentCatalogRejectionReason.None)
        {
            throw new ArgumentOutOfRangeException(nameof(reason));
        }

        return new ContentCatalogResolution(null, reason);
    }
}

public static class Cna1979SyntheticContentCatalog
{
    public const string PackId = "rules-lab.content.movement-contact.v1";

    private const string SourceId = "sandtable-rules-lab";
    private const string LocatorRoot = "content.movement-contact.v1";

    public static ContentPackArtifact Artifact { get; } = CreateArtifact();

    public static ContentPresentationCatalog Presentation { get; } = CreatePresentation();

    public static ContentCatalogResolution Resolve(string packId, string expectedHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(packId);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedHash);

        if (!string.Equals(packId, Artifact.Identity.PackId, StringComparison.Ordinal))
        {
            return ContentCatalogResolution.Rejected(
                ContentCatalogRejectionReason.UnknownPackId);
        }

        return string.Equals(expectedHash, Artifact.Identity.Hash, StringComparison.Ordinal)
            ? ContentCatalogResolution.Resolved(Artifact)
            : ContentCatalogResolution.Rejected(ContentCatalogRejectionReason.HashMismatch);
    }

    private static ContentPackArtifact CreateArtifact()
    {
        var locations = CreateLocations();
        var formations = CreateFormations();
        var elements = CreateElements();
        var definition = new ContentPackDefinition(
            ContentPackDefinition.CurrentSchemaVersion,
            ContentPackDefinition.CanonicalFormatId,
            PackId,
            Cna1979Ruleset.RulesetId,
            ["land.hex-topology", "land.formations", "land.initial-deployment"],
            [new ContentSourceIndexEntry(SourceId, ContentSourceKind.RepositorySynthetic)],
            locations,
            CreateEdges(),
            formations,
            elements,
            [
                CreateScenario(elements, "movement-contact-lab", 1),
                CreateScenario(elements, "initiative-contested-lab", 43),
            ]);

        return ContentPackArtifact.Create(definition);
    }

    private static IReadOnlyList<ContentHex> CreateLocations() =>
    [
        Location("north-west", "land.terrain.clear"),
        Location("north", "land.terrain.clear"),
        Location("north-east", "land.terrain.clear"),
        Location("west", "land.terrain.clear"),
        Location("center", "land.terrain.desert"),
        Location("east", "land.terrain.clear"),
        Location("south-west", "land.terrain.clear"),
        Location("south", "land.terrain.clear"),
        Location("south-east", "land.terrain.clear"),
    ];

    private static IReadOnlyList<ContentHexEdge> CreateEdges() =>
    [
        Edge("center", "east", "land.edge.ridge", null, "center-east"),
        Edge("center", "west", "land.edge.slope", "west", "center-west"),
        Edge("east", "north-east", "land.edge.road", null, "east-north-east"),
        Edge("east", "south-east", "land.edge.track", null, "east-south-east"),
        Edge("north", "north-east", "land.edge.road", null, "north-north-east"),
        Edge("north", "north-west", "land.edge.road", null, "north-north-west"),
        Edge("north-west", "west", "land.edge.road", null, "north-west-west"),
        Edge("south", "south-east", "land.edge.track", null, "south-south-east"),
        Edge("south", "south-west", "land.edge.track", null, "south-south-west"),
        Edge("south-west", "west", "land.edge.track", null, "south-west-west"),
    ];

    private static IReadOnlyList<ContentFormation> CreateFormations() =>
    [
        new(
            "axis-lab-formation",
            "axis",
            null,
            "land.organization.regiment",
            Origin("formation.axis-lab-formation")),
        new(
            "commonwealth-lab-formation",
            "commonwealth",
            null,
            "land.organization.regiment",
            Origin("formation.commonwealth-lab-formation")),
    ];

    private static IReadOnlyList<ContentCombatElement> CreateElements() =>
    [
        Element("axis-element-a", "axis", "axis-lab-formation", 20),
        Element("axis-element-b", "axis", "axis-lab-formation", 10),
        Element(
            "commonwealth-element-a",
            "commonwealth",
            "commonwealth-lab-formation",
            20),
        Element(
            "commonwealth-element-b",
            "commonwealth",
            "commonwealth-lab-formation",
            10),
    ];

    private static ContentScenario CreateScenario(
        IReadOnlyList<ContentCombatElement> elements,
        string scenarioId,
        int gameTurn) => new(
            scenarioId,
            new ContentScenarioBoundary(gameTurn, 1),
            new ContentScenarioBoundary(gameTurn, 3),
            [
                Placement(elements, scenarioId, "axis-element-a", "west"),
                Placement(elements, scenarioId, "axis-element-b", "north-west"),
                Placement(elements, scenarioId, "commonwealth-element-a", "east"),
                Placement(elements, scenarioId, "commonwealth-element-b", "south-east"),
            ],
            Origin($"scenario.{scenarioId}"));

    private static ContentHex Location(string locationId, string terrainId) => new(
        locationId,
        terrainId,
        null,
        Origin($"location.{locationId}"));

    private static ContentHexEdge Edge(
        string firstLocationId,
        string secondLocationId,
        string featureId,
        string? directionFromLocationId,
        string locatorId) => new(
            firstLocationId,
            secondLocationId,
            [new ContentEdgeFeature(
                featureId,
                directionFromLocationId,
                Origin($"edge-feature.{locatorId}.{FeatureLocator(featureId)}"))],
            Origin($"edge.{locatorId}"));

    private static ContentCombatElement Element(
        string elementId,
        string sideId,
        string parentFormationId,
        int baseCapabilityPointAllowance) => new(
            elementId,
            sideId,
            parentFormationId,
            "land.organization.battalion",
            baseCapabilityPointAllowance,
            ContentPlacementMode.Independent,
            Origin($"element.{elementId}"));

    private static ContentInitialPlacement Placement(
        IReadOnlyList<ContentCombatElement> elements,
        string scenarioId,
        string elementId,
        string locationId)
    {
        _ = elements.Single(element => element.ElementId == elementId);
        return new ContentInitialPlacement(
            elementId,
            locationId,
            Origin($"placement.{scenarioId}.{elementId}.{locationId}"));
    }

    private static ContentOrigin Origin(string locator) => new(
        ContentOriginKind.Synthetic,
        [new RuleReference(SourceId, $"{LocatorRoot}.{locator}")]);

    private static string FeatureLocator(string featureId) => featureId switch
    {
        "land.edge.road" => "road",
        "land.edge.track" => "track",
        "land.edge.slope" => "slope",
        "land.edge.ridge" => "ridge",
        _ => throw new ArgumentOutOfRangeException(nameof(featureId)),
    };

    private static ContentPresentationCatalog CreatePresentation()
    {
        var presentation = new ContentPresentationCatalog(
            PackId,
            "Amber Wadi Exercise",
            "Original synthetic rules laboratory; nonhistorical and not a CNA scenario.",
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["north-west"] = "Copper Northern Approach",
                ["north"] = "Northern Passage",
                ["north-east"] = "Azure Northern Approach",
                ["west"] = "Copper Approach",
                ["center"] = "Amber Wadi",
                ["east"] = "Azure Approach",
                ["south-west"] = "Copper Southern Approach",
                ["south"] = "Southern Passage",
                ["south-east"] = "Azure Southern Approach",
                ["axis-lab-formation"] = "Copper Group",
                ["axis-element-a"] = "Copper One",
                ["axis-element-b"] = "Copper Two",
                ["commonwealth-lab-formation"] = "Azure Group",
                ["commonwealth-element-a"] = "Azure One",
                ["commonwealth-element-b"] = "Azure Two",
                ["movement-contact-lab"] = "Amber Wadi Movement and Contact Lab",
                ["initiative-contested-lab"] = "Amber Wadi Initiative Contest Lab",
            });
        var knownIds = Artifact.Definition.Locations.Select(value => value.LocationId)
            .Concat(Artifact.Definition.Formations.Select(value => value.FormationId))
            .Concat(Artifact.Definition.Elements.Select(value => value.ElementId))
            .Concat(Artifact.Definition.Scenarios.Select(value => value.ScenarioId))
            .ToHashSet(StringComparer.Ordinal);

        if (presentation.Labels.Keys.Any(key => !knownIds.Contains(key)))
        {
            throw new InvalidOperationException(
                "Synthetic presentation contains a label for an unknown content ID.");
        }

        return presentation;
    }
}
