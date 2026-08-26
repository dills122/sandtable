using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Content;

public sealed class ContentContractsTests
{
    public static TheoryData<string> InvalidStableIds => new()
    {
        { string.Empty },
        { "Axis" },
        { "axis value" },
        { "axis/value" },
        { "axis_formation" },
        { "éclair" },
    };

    [Theory]
    [MemberData(nameof(InvalidStableIds))]
    public void StableAuthoritativeIdsRejectUnsafeOrNoncanonicalValues(string value)
    {
        Assert.Throws<ArgumentException>(() => new ContentSourceIndexEntry(
            value,
            ContentSourceKind.RepositorySynthetic));
        Assert.Throws<ArgumentException>(() => new ContentHex(
            value,
            "land.terrain.clear",
            null,
            CreateOrigin("content.hex")));
    }

    [Fact]
    public void SourceAtomsAcceptTheBoundaryAndRejectUnsafeValues()
    {
        var maximum = $"a{new string('b', 127)}";

        var coordinate = new ContentSourceCoordinate(maximum, "A.1:west");

        Assert.Equal(maximum, coordinate.SectionId);
        Assert.Throws<ArgumentException>(() => new ContentSourceCoordinate(
            $"a{new string('b', 128)}",
            "label"));
        Assert.Throws<ArgumentException>(() => new ContentSourceCoordinate("section", "bad label"));
        Assert.Throws<ArgumentException>(() => new ContentSourceCoordinate("section", "bad\"label"));
        Assert.Throws<ArgumentException>(() => new ContentOrigin(
            ContentOriginKind.Synthetic,
            [new RuleReference("sandtable-rules-lab", "bad/locator")]));
    }

    [Fact]
    public void OriginDefensivelyCopiesCanonicalReferencesAndComparesStructurally()
    {
        var references = new List<RuleReference>
        {
            new("sandtable-rules-lab", "content.hex.west"),
            new("sandtable-rules-lab", "content.hex.east"),
        };
        var origin = new ContentOrigin(ContentOriginKind.Synthetic, references);
        var equivalent = new ContentOrigin(
            ContentOriginKind.Synthetic,
            references.AsEnumerable().Reverse().ToArray());

        references.Clear();

        Assert.Equal(
            [
                new RuleReference("sandtable-rules-lab", "content.hex.east"),
                new RuleReference("sandtable-rules-lab", "content.hex.west"),
            ],
            origin.References);
        Assert.Equal(origin, equivalent);
        Assert.Equal(origin.GetHashCode(), equivalent.GetHashCode());
        Assert.Throws<ArgumentException>(() => new ContentOrigin(ContentOriginKind.Synthetic, []));
        Assert.Throws<ArgumentException>(() => new ContentOrigin(
            ContentOriginKind.Synthetic,
            [
                new RuleReference("sandtable-rules-lab", "content.hex"),
                new RuleReference("sandtable-rules-lab", "content.hex"),
            ]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentOrigin(
            (ContentOriginKind)99,
            [new RuleReference("sandtable-rules-lab", "content.hex")]));
    }

    [Fact]
    public void HexEdgeNormalizesEndpointsAndCopiesFeatures()
    {
        var origin = CreateOrigin("content.edge.east-west");
        var features = new List<ContentEdgeFeature>
        {
            new("land.edge.slope", "west", CreateOrigin("content.edge.slope")),
            new("land.edge.road", null, CreateOrigin("content.edge.road")),
        };
        var edge = new ContentHexEdge("west", "east", features, origin);
        var equivalent = new ContentHexEdge(
            "east",
            "west",
            features.AsEnumerable().Reverse().ToArray(),
            origin);

        features.Clear();

        Assert.Equal("east", edge.FirstLocationId);
        Assert.Equal("west", edge.SecondLocationId);
        Assert.Equal(["land.edge.road", "land.edge.slope"], edge.Features.Select(value => value.FeatureId));
        Assert.Equal(edge, equivalent);
        Assert.Equal(edge.GetHashCode(), equivalent.GetHashCode());
    }

    [Fact]
    public void PackDefensivelyCopiesAndCanonicalizesEveryKeyedCollection()
    {
        var capabilities = new List<string>
        {
            "land.initial-deployment",
            "land.formations",
            "land.element-mobility",
            "land.hex-topology",
        };
        var sources = new List<ContentSourceIndexEntry>
        {
            new("sandtable-rules-lab", ContentSourceKind.RepositorySynthetic),
        };
        var locations = CreateLocations().Reverse().ToList();
        var edges = CreateEdges().ToList();
        var formations = CreateFormations().ToList();
        var elements = CreateElements().ToList();
        var scenarios = CreateScenarios().ToList();
        var pack = new ContentPackDefinition(
            ContentPackDefinition.CurrentSchemaVersion,
            ContentPackDefinition.CanonicalFormatId,
            "rules-lab.content.contracts.v1",
            "cna-1979.1",
            capabilities,
            sources,
            locations,
            edges,
            formations,
            elements,
            scenarios);
        var equivalent = new ContentPackDefinition(
            ContentPackDefinition.CurrentSchemaVersion,
            ContentPackDefinition.CanonicalFormatId,
            "rules-lab.content.contracts.v1",
            "cna-1979.1",
            capabilities.AsEnumerable().Reverse().ToArray(),
            sources.AsEnumerable().Reverse().ToArray(),
            locations.AsEnumerable().Reverse().ToArray(),
            edges.AsEnumerable().Reverse().ToArray(),
            formations.AsEnumerable().Reverse().ToArray(),
            elements.AsEnumerable().Reverse().ToArray(),
            scenarios.AsEnumerable().Reverse().ToArray());

        capabilities.Clear();
        sources.Clear();
        locations.Clear();
        edges.Clear();
        formations.Clear();
        elements.Clear();
        scenarios.Clear();

        Assert.Equal(
            [
                "land.element-mobility",
                "land.formations",
                "land.hex-topology",
                "land.initial-deployment",
            ],
            pack.Capabilities);
        Assert.Equal(["east", "west"], pack.Locations.Select(value => value.LocationId));
        Assert.Equal(pack, equivalent);
        Assert.Equal(pack.GetHashCode(), equivalent.GetHashCode());
    }

    [Fact]
    public void PresentationCatalogIsCopiedAndRemainsOutsideAuthoritativeEquality()
    {
        var pack = CreatePack();
        var labels = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["west"] = "Copper Approach",
            ["east"] = "Azure Approach",
        };
        var first = new ContentPresentationCatalog(
            pack.PackId,
            "Amber Wadi Exercise",
            "Original synthetic rules laboratory; nonhistorical and not a CNA scenario.",
            labels);

        labels["west"] = "Changed after construction";
        var changedPresentation = new ContentPresentationCatalog(
            pack.PackId,
            "Changed display name",
            "Changed presentation notice",
            new Dictionary<string, string>());

        Assert.Equal("Copper Approach", first.Labels["west"]);
        Assert.NotEqual(first, changedPresentation);
        Assert.Equal(pack, CreatePack());
        Assert.DoesNotContain("Amber Wadi Exercise", pack.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void LocalVersionsEnumsAndScenarioBoundsAreStrict()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentSourceIndexEntry(
            "sandtable-rules-lab",
            (ContentSourceKind)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentScenarioBoundary(0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentScenarioBoundary(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentScenarioBoundary(1, 4));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentPackDefinition(
            ContentPackDefinition.CurrentSchemaVersion + 1,
            ContentPackDefinition.CanonicalFormatId,
            "rules-lab.content.contracts.v1",
            "cna-1979.1",
            [],
            [],
            [],
            [],
            [],
            [],
            []));
        Assert.Throws<ArgumentException>(() => new ContentPackDefinition(
            ContentPackDefinition.CurrentSchemaVersion,
            "sandtable.content-json.v1",
            "rules-lab.content.contracts.v1",
            "cna-1979.1",
            [],
            [],
            [],
            [],
            [],
            [],
            []));
    }

    private static ContentPackDefinition CreatePack() => new(
        ContentPackDefinition.CurrentSchemaVersion,
        ContentPackDefinition.CanonicalFormatId,
        "rules-lab.content.contracts.v1",
        "cna-1979.1",
        [
            "land.hex-topology",
            "land.formations",
            "land.element-mobility",
            "land.initial-deployment",
        ],
        [new ContentSourceIndexEntry("sandtable-rules-lab", ContentSourceKind.RepositorySynthetic)],
        CreateLocations(),
        CreateEdges(),
        CreateFormations(),
        CreateElements(),
        CreateScenarios());

    private static ContentOrigin CreateOrigin(string locator) => new(
        ContentOriginKind.Synthetic,
        [new RuleReference("sandtable-rules-lab", locator)]);

    private static IReadOnlyList<ContentHex> CreateLocations() =>
    [
        new("west", "land.terrain.clear", null, CreateOrigin("content.hex.west")),
        new("east", "land.terrain.clear", null, CreateOrigin("content.hex.east")),
    ];

    private static IReadOnlyList<ContentHexEdge> CreateEdges() =>
    [
        new(
            "west",
            "east",
            [new ContentEdgeFeature("land.edge.road", null, CreateOrigin("content.edge.road"))],
            CreateOrigin("content.edge.east-west")),
    ];

    private static IReadOnlyList<ContentFormation> CreateFormations() =>
    [
        new(
            "axis-formation",
            "axis",
            null,
            "land.organization.regiment",
            CreateOrigin("content.formation.axis")),
    ];

    private static IReadOnlyList<ContentCombatElement> CreateElements() =>
    [
        new(
            "axis-element",
            "axis",
            "axis-formation",
            "land.organization.battalion",
            Cna1979Movement.MotorizedMobilityId,
            20,
            ContentPlacementMode.Independent,
            CreateOrigin("content.element.axis")),
    ];

    private static IReadOnlyList<ContentScenario> CreateScenarios() =>
    [
        new(
            "contracts-lab",
            new ContentScenarioBoundary(1, 1),
            new ContentScenarioBoundary(1, 3),
            [new ContentInitialPlacement(
                "axis-element",
                "west",
                CreateOrigin("content.placement.axis"))],
            CreateOrigin("content.scenario.contracts")),
    ];
}
