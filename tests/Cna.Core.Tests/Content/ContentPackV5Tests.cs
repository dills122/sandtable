using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Content;

public sealed class ContentPackV5Tests
{
    [Fact]
    public void SuccessorIdentityIsExplicitAndDormant()
    {
        var legacy = ContentTestData.CreateMinimalPack();
        var definition = CreateSuccessor(legacyDefinition: legacy);

        Assert.Equal(5, ContentPackV5Definition.SchemaVersion);
        Assert.Equal("sandtable.content-json.v4", ContentPackV5Definition.CanonicalFormatId);
        Assert.Equal("land.combat-components", ContentPackV5Definition.CombatCapabilityId);
        Assert.Equal(ContentPackV5Definition.SchemaVersion, definition.ContractSchemaVersion);
        Assert.Equal(ContentPackV5Definition.CanonicalFormatId, definition.FormatId);
        Assert.Contains(ContentPackV5Definition.CombatCapabilityId, definition.Capabilities);

        Assert.Equal(4, ContentPackDefinition.CurrentSchemaVersion);
        Assert.Equal("sandtable.content-json.v3", ContentPackDefinition.CanonicalFormatId);
        Assert.Equal(7, Cna1979Ruleset.ContractVersion);
        Assert.Same(legacy, definition.LegacyDefinition);
    }

    [Fact]
    public void ComponentAndPlacementSeedValuesAreCanonicalAndDefensive()
    {
        var componentA = Component("axis-element.toe.infantry-a", maximumToe: 6);
        var componentB = Component("axis-element.toe.infantry-b", maximumToe: 4);
        var componentInput = new[] { componentB, componentA };
        var facts = new ContentElementCombatFacts(
            "axis-element",
            Cna1979Combat.CombatUnitClassificationId,
            componentInput,
            ContentTestData.Origin("content.combat.axis-element"));
        var seedA = Seed(componentA.ComponentId, currentToe: 6);
        var seedB = Seed(componentB.ComponentId, currentToe: 4);
        var seedInput = new[] { seedB, seedA };
        var placement = new ContentInitialPlacementCombatFacts(
            "minimal-lab",
            "axis-element",
            seedInput);

        componentInput[0] = Component("axis-element.toe.replaced", maximumToe: 1);
        seedInput[0] = Seed("axis-element.toe.replaced", currentToe: 1);

        Assert.Equal(
            [componentA.ComponentId, componentB.ComponentId],
            facts.Components.Select(value => value.ComponentId));
        Assert.Equal(
            [componentA.ComponentId, componentB.ComponentId],
            placement.InitialComponentToes.Select(value => value.ComponentId));
        Assert.Throws<ArgumentOutOfRangeException>(() => Component(
            "axis-element.toe.zero",
            maximumToe: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Component(
            "axis-element.toe.negative-rating",
            defensiveRating: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Seed(
            componentA.ComponentId,
            currentToe: -1));
        Assert.Throws<ArgumentException>(() => new ContentElementCombatFacts(
            "axis-element",
            Cna1979Combat.CombatUnitClassificationId,
            [],
            ContentTestData.Origin("content.combat.empty")));
    }

    [Fact]
    public void CompleteSuccessorPassesStructuralAndRulesCompatibilityValidation()
    {
        var definition = CreateSuccessor();

        var structural = ContentPackV5Validator.Validate(definition);
        var compatibility = Cna1979ContentV5CompatibilityValidator.Validate(definition);

        Assert.True(structural.IsValid);
        Assert.Empty(structural.Issues);
        Assert.True(compatibility.IsValid);
        Assert.Empty(compatibility.Issues);
    }

    public static TheoryData<string, Func<ContentPackV5Definition>> InvalidSeeds => new()
    {
        {
            "missing",
            () => CreateSuccessor(placementFacts:
            [
                new ContentInitialPlacementCombatFacts(
                    "minimal-lab",
                    "axis-element",
                    []),
            ])
        },
        {
            "duplicate",
            () => CreateSuccessor(placementFacts:
            [
                new ContentInitialPlacementCombatFacts(
                    "minimal-lab",
                    "axis-element",
                    [Seed(DefaultComponentId, 10), Seed(DefaultComponentId, 10)]),
            ])
        },
        {
            "unknown",
            () => CreateSuccessor(placementFacts:
            [
                new ContentInitialPlacementCombatFacts(
                    "minimal-lab",
                    "axis-element",
                    [Seed("axis-element.toe.unknown", 10)]),
            ])
        },
        {
            "over-maximum",
            () => CreateSuccessor(placementFacts:
            [
                new ContentInitialPlacementCombatFacts(
                    "minimal-lab",
                    "axis-element",
                    [Seed(DefaultComponentId, 11)]),
            ])
        },
    };

    [Theory]
    [MemberData(nameof(InvalidSeeds))]
    public void PlacementSeedsRejectEveryCompletenessAndBoundsFailure(
        string name,
        Func<ContentPackV5Definition> create)
    {
        var result = ContentPackV5Validator.Validate(create());

        Assert.False(result.IsValid, name);
        Assert.Contains(result.Issues, issue => issue.Code == name switch
        {
            "missing" => "content.initial-toe.missing-component",
            "duplicate" => "content.initial-toe.duplicate-component",
            "unknown" => "content.initial-toe.unknown-component",
            "over-maximum" => "content.initial-toe.over-maximum",
            _ => throw new InvalidOperationException(name),
        });
    }

    [Fact]
    public void PlacementSeedDeclarationsBindExactlyOneExistingPlacementAndAllowZeroToe()
    {
        var legacy = ContentTestData.CreateMinimalPack();
        var missing = new ContentPackV5Definition(
            legacy,
            [DefaultElementFacts()],
            []);
        var duplicate = new ContentPackV5Definition(
            legacy,
            [DefaultElementFacts()],
            [DefaultPlacementFacts(), DefaultPlacementFacts()]);
        var unknown = new ContentPackV5Definition(
            legacy,
            [DefaultElementFacts()],
            [
                DefaultPlacementFacts(),
                new ContentInitialPlacementCombatFacts(
                    "unknown-scenario",
                    "axis-element",
                    [Seed(DefaultComponentId, 10)]),
            ]);
        var zero = CreateSuccessor(placementFacts:
        [
            new ContentInitialPlacementCombatFacts(
                "minimal-lab",
                "axis-element",
                [Seed(DefaultComponentId, 0)]),
        ]);

        AssertIssue(
            ContentPackV5Validator.Validate(missing),
            "content.initial-toe.missing-placement");
        AssertIssue(
            ContentPackV5Validator.Validate(duplicate),
            "content.initial-toe.duplicate-placement");
        AssertIssue(
            ContentPackV5Validator.Validate(unknown),
            "content.initial-toe.unknown-placement");
        Assert.True(ContentPackV5Validator.Validate(zero).IsValid);
    }

    [Fact]
    public void CombatFactsRejectMissingDuplicateUnknownAndAmbiguousBindings()
    {
        var basePack = ContentTestData.CreateMinimalPack();
        var facts = DefaultElementFacts();
        var duplicateElement = new ContentPackV5Definition(
            basePack,
            [facts, facts],
            [DefaultPlacementFacts()]);
        var unknownElement = new ContentPackV5Definition(
            basePack,
            [new ContentElementCombatFacts(
                "unknown-element",
                Cna1979Combat.CombatUnitClassificationId,
                [Component("unknown-element.toe.infantry")],
                ContentTestData.Origin("content.combat.unknown-element"))],
            [DefaultPlacementFacts()]);
        var duplicateComponent = new ContentPackV5Definition(
            basePack,
            [new ContentElementCombatFacts(
                "axis-element",
                Cna1979Combat.CombatUnitClassificationId,
                [Component(DefaultComponentId), Component(DefaultComponentId)],
                ContentTestData.Origin("content.combat.axis-element"))],
            [DefaultPlacementFacts()]);
        var missing = new ContentPackV5Definition(
            basePack,
            [],
            [DefaultPlacementFacts()]);

        AssertIssue(
            ContentPackV5Validator.Validate(duplicateElement),
            "content.combat.duplicate-element");
        AssertIssue(
            ContentPackV5Validator.Validate(unknownElement),
            "content.combat.unknown-element");
        AssertIssue(
            ContentPackV5Validator.Validate(duplicateComponent),
            "content.combat.duplicate-component");
        AssertIssue(
            ContentPackV5Validator.Validate(missing),
            "content.combat.missing-element");
    }

    [Fact]
    public void RulesCompatibilityRejectsUnknownClassificationAndComponentClass()
    {
        var unknownClassification = CreateSuccessor(elementFacts:
        [
            new ContentElementCombatFacts(
                "axis-element",
                "land.combat-classification.unknown",
                [Component(DefaultComponentId)],
                ContentTestData.Origin("content.combat.axis-element")),
        ]);
        var unknownComponent = CreateSuccessor(elementFacts:
        [
            new ContentElementCombatFacts(
                "axis-element",
                Cna1979Combat.CombatUnitClassificationId,
                [new ContentCombatComponent(
                    DefaultComponentId,
                    "land.combat-component.unknown",
                    10,
                    1,
                    ContentTestData.Origin("content.component.axis-element"))],
                ContentTestData.Origin("content.combat.axis-element")),
        ]);

        AssertIssue(
            Cna1979ContentV5CompatibilityValidator.Validate(unknownClassification),
            "vocabulary.unknown-combat-classification");
        AssertIssue(
            Cna1979ContentV5CompatibilityValidator.Validate(unknownComponent),
            "vocabulary.unknown-combat-component");
    }

    [Fact]
    public void CombatAndSeedOriginsMustResolveAgainstTheSourceIndexAndKind()
    {
        var unknownOrigin = CreateSuccessor(elementFacts:
        [
            new ContentElementCombatFacts(
                "axis-element",
                Cna1979Combat.CombatUnitClassificationId,
                [new ContentCombatComponent(
                    DefaultComponentId,
                    Cna1979Combat.InfantryComponentClassId,
                    10,
                    1,
                    ContentTestData.Origin(
                        "content.component.axis-element",
                        sourceId: "missing-source"))],
                ContentTestData.Origin("content.combat.axis-element")),
        ]);
        var wrongKind = CreateSuccessor(placementFacts:
        [
            new ContentInitialPlacementCombatFacts(
                "minimal-lab",
                "axis-element",
                [new ContentInitialComponentToe(
                    DefaultComponentId,
                    10,
                    ContentTestData.Origin(
                        "content.seed.axis-element",
                        kind: ContentOriginKind.SourceDerived))]),
        ]);

        AssertIssue(
            ContentPackV5Validator.Validate(unknownOrigin),
            "content.unknown-reference");
        AssertIssue(
            ContentPackV5Validator.Validate(wrongKind),
            "content.invalid-origin");
    }

    [Fact]
    public void CanonicalBytesNormalizeOrderAndBindEverySuccessorFact()
    {
        var componentA = Component("axis-element.toe.infantry-a", maximumToe: 6);
        var componentB = Component("axis-element.toe.infantry-b", maximumToe: 4);
        var first = CreateSuccessor(
            elementFacts:
            [
                new ContentElementCombatFacts(
                    "axis-element",
                    Cna1979Combat.CombatUnitClassificationId,
                    [componentB, componentA],
                    ContentTestData.Origin("content.combat.axis-element")),
            ],
            placementFacts:
            [
                new ContentInitialPlacementCombatFacts(
                    "minimal-lab",
                    "axis-element",
                    [Seed(componentB.ComponentId, 4), Seed(componentA.ComponentId, 6)]),
            ]);
        var reordered = CreateSuccessor(
            elementFacts:
            [
                new ContentElementCombatFacts(
                    "axis-element",
                    Cna1979Combat.CombatUnitClassificationId,
                    [componentA, componentB],
                    ContentTestData.Origin("content.combat.axis-element")),
            ],
            placementFacts:
            [
                new ContentInitialPlacementCombatFacts(
                    "minimal-lab",
                    "axis-element",
                    [Seed(componentA.ComponentId, 6), Seed(componentB.ComponentId, 4)]),
            ]);

        var firstArtifact = ContentPackV5Artifact.Create(first);
        var reorderedArtifact = ContentPackV5Artifact.Create(reordered);
        var baseline = CreateSuccessor();
        var changedLegacy = ContentTestData.CreateMinimalPack();
        var changedLocations = changedLegacy.Locations.Select(location =>
            location.LocationId == "east"
                ? new ContentHex(
                    location.LocationId,
                    "land.terrain.desert",
                    location.SourceCoordinate,
                    location.Origin)
                : location);
        var changedComponentId = "axis-element.toe.infantry-renamed";
        var mutations = new[]
        {
            CreateSuccessor(legacyDefinition: ContentTestData.Copy(
                changedLegacy,
                locations: changedLocations)),
            CreateSuccessor(elementFacts:
            [
                DefaultElementFacts(
                    classificationId: Cna1979Combat.HeadquartersClassificationId),
            ]),
            CreateSuccessor(elementFacts: [DefaultElementFacts(maximumToe: 11)]),
            CreateSuccessor(elementFacts: [DefaultElementFacts(defensiveRating: 2)]),
            CreateSuccessor(
                elementFacts:
                [
                    new ContentElementCombatFacts(
                        "axis-element",
                        Cna1979Combat.CombatUnitClassificationId,
                        [Component(changedComponentId)],
                        ContentTestData.Origin("content.combat.axis-element")),
                ],
                placementFacts:
                [
                    new ContentInitialPlacementCombatFacts(
                        "minimal-lab",
                        "axis-element",
                        [Seed(changedComponentId, 10)]),
                ]),
            CreateSuccessor(elementFacts:
            [
                new ContentElementCombatFacts(
                    "axis-element",
                    Cna1979Combat.CombatUnitClassificationId,
                    [new ContentCombatComponent(
                        DefaultComponentId,
                        Cna1979Combat.InfantryComponentClassId,
                        10,
                        1,
                        ContentTestData.Origin("content.component.changed-origin"))],
                    ContentTestData.Origin("content.combat.changed-origin")),
            ]),
            CreateSuccessor(placementFacts:
            [
                new ContentInitialPlacementCombatFacts(
                    "minimal-lab",
                    "axis-element",
                    [Seed(DefaultComponentId, 9)]),
            ]),
            CreateSuccessor(placementFacts:
            [
                new ContentInitialPlacementCombatFacts(
                    "minimal-lab",
                    "axis-element",
                    [new ContentInitialComponentToe(
                        DefaultComponentId,
                        10,
                        ContentTestData.Origin("content.seed.changed-origin"))]),
            ]),
        };

        Assert.Equal(firstArtifact.GetCanonicalBytes(), reorderedArtifact.GetCanonicalBytes());
        Assert.Equal(firstArtifact.Identity.Hash, reorderedArtifact.Identity.Hash);
        Assert.All(mutations, mutation => Assert.NotEqual(
            ContentPackV5Artifact.Create(baseline).Identity.Hash,
            ContentPackV5Artifact.Create(mutation).Identity.Hash));
    }

    [Fact]
    public void CanonicalDocumentIsCompleteButRemainsRejectedByTheActiveReader()
    {
        var definition = CreateSuccessor();
        var artifact = ContentPackV5Artifact.Create(definition);
        var bytes = artifact.GetCanonicalBytes();
        using var document = JsonDocument.Parse(bytes);
        var root = document.RootElement;
        var element = root.GetProperty("elements")[0];
        var placement = root.GetProperty("scenarios")[0]
            .GetProperty("initialPlacements")[0];

        Assert.Equal(5, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("sandtable.content-json.v4", root.GetProperty("formatId").GetString());
        Assert.Equal(
            Cna1979Combat.CombatUnitClassificationId,
            element.GetProperty("combatClassificationId").GetString());
        Assert.Single(element.GetProperty("components").EnumerateArray());
        Assert.Single(placement.GetProperty("initialComponentToes").EnumerateArray());
        Assert.Equal(
            $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}",
            artifact.Identity.Hash);
        Assert.DoesNotContain("exertsZoc", Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);
        Assert.DoesNotContain("rawDefensiveCloseAssault", Encoding.UTF8.GetString(bytes), StringComparison.Ordinal);

        var activeRead = ContentPackSerializer.Deserialize(bytes);
        Assert.False(activeRead.IsSuccess);
        Assert.Equal("content.unknown-version", activeRead.ErrorCode);
    }

    [Fact]
    public void ArtifactAndIdentityDefendCanonicalBytesAndClosedVersionValues()
    {
        const string validHash =
            "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";
        var artifact = ContentPackV5Artifact.Create(CreateSuccessor());
        var firstCopy = artifact.GetCanonicalBytes();
        var destination = new byte[artifact.CanonicalByteCount];

        artifact.CopyCanonicalBytes(destination);
        firstCopy[0] = (byte)'[';

        Assert.Equal(destination, artifact.GetCanonicalBytes());
        Assert.Throws<ArgumentException>(() => artifact.CopyCanonicalBytes(
            new byte[artifact.CanonicalByteCount - 1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentPackV5Identity(
            4,
            ContentPackV5Definition.CanonicalFormatId,
            "rules-lab.content.minimal.v1",
            "cna-1979.1",
            validHash));
        Assert.Throws<ArgumentException>(() => new ContentPackV5Identity(
            ContentPackV5Definition.SchemaVersion,
            ContentPackDefinition.CanonicalFormatId,
            "rules-lab.content.minimal.v1",
            "cna-1979.1",
            validHash));
        Assert.Throws<ArgumentException>(() => new ContentPackV5Identity(
            ContentPackV5Definition.SchemaVersion,
            ContentPackV5Definition.CanonicalFormatId,
            "Rules Lab",
            "cna-1979.1",
            validHash));
        Assert.Throws<ArgumentException>(() => new ContentPackV5Identity(
            ContentPackV5Definition.SchemaVersion,
            ContentPackV5Definition.CanonicalFormatId,
            "rules-lab.content.minimal.v1",
            "cna-1979.1",
            validHash.ToUpperInvariant()));
    }

    [Fact]
    public void MixedLegacyCapabilityAndInvalidSuccessorCannotProduceIdentity()
    {
        var legacy = ContentTestData.CreateMinimalPack();
        var mixedLegacy = ContentTestData.Copy(
            legacy,
            capabilities: [.. legacy.Capabilities, ContentPackV5Definition.CombatCapabilityId]);
        var mixed = CreateSuccessor(legacyDefinition: mixedLegacy);
        var invalid = CreateSuccessor(placementFacts:
        [
            new ContentInitialPlacementCombatFacts(
                "minimal-lab",
                "axis-element",
                []),
        ]);

        AssertIssue(
            ContentPackV5Validator.Validate(mixed),
            "content.v5.mixed-legacy-capability");
        var exception = Assert.Throws<InvalidContentPackException>(() =>
            ContentPackV5Artifact.Create(invalid));
        Assert.Contains(
            exception.Issues,
            issue => issue.Code == "content.initial-toe.missing-component");
        Assert.Throws<InvalidContentPackException>(() =>
            ContentPackV5Serializer.SerializeCanonical(invalid));
    }

    private const string DefaultComponentId = "axis-element.toe.infantry";

    private static ContentPackV5Definition CreateSuccessor(
        ContentPackDefinition? legacyDefinition = null,
        IEnumerable<ContentElementCombatFacts>? elementFacts = null,
        IEnumerable<ContentInitialPlacementCombatFacts>? placementFacts = null) => new(
            legacyDefinition ?? ContentTestData.CreateMinimalPack(),
            elementFacts ?? [DefaultElementFacts()],
            placementFacts ?? [DefaultPlacementFacts()]);

    private static ContentElementCombatFacts DefaultElementFacts(
        int maximumToe = 10,
        int defensiveRating = 1,
        string? classificationId = null) => new(
            "axis-element",
            classificationId ?? Cna1979Combat.CombatUnitClassificationId,
            [Component(DefaultComponentId, maximumToe, defensiveRating)],
            ContentTestData.Origin("content.combat.axis-element"));

    private static ContentInitialPlacementCombatFacts DefaultPlacementFacts() => new(
        "minimal-lab",
        "axis-element",
        [Seed(DefaultComponentId, 10)]);

    private static ContentCombatComponent Component(
        string componentId,
        int maximumToe = 10,
        int defensiveRating = 1) => new(
            componentId,
            Cna1979Combat.InfantryComponentClassId,
            maximumToe,
            defensiveRating,
            ContentTestData.Origin($"content.component.{componentId}"));

    private static ContentInitialComponentToe Seed(
        string componentId,
        int currentToe) => new(
            componentId,
            currentToe,
            ContentTestData.Origin($"content.seed.{componentId}"));

    private static void AssertIssue(ContentValidationResult result, string code)
    {
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, issue => issue.Code == code);
    }
}
