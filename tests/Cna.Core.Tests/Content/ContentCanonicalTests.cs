using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Content;

namespace Cna.Core.Tests.Content;

public sealed class ContentCanonicalTests
{
    [Fact]
    public void MinimalPackMatchesTheCompleteGoldenCanonicalVector()
    {
        var pack = ContentTestData.CreateMinimalPack();

        var bytes = ContentPackSerializer.SerializeCanonical(pack);

        Assert.Equal(ExpectedCanonicalJson, Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void ArtifactIdentityUsesIndependentSha256AndDefensiveByteCopies()
    {
        var pack = ContentTestData.CreateMinimalPack();
        var expectedBytes = Encoding.UTF8.GetBytes(ExpectedCanonicalJson);
        var expectedHash = $"sha256:{Convert.ToHexString(SHA256.HashData(expectedBytes)).ToLowerInvariant()}";

        var artifact = ContentPackArtifact.Create(pack);
        var firstCopy = artifact.GetCanonicalBytes();
        var destination = new byte[artifact.CanonicalByteCount];
        artifact.CopyCanonicalBytes(destination);

        firstCopy[0] = (byte)'[';

        Assert.Equal(pack, artifact.Definition);
        Assert.Equal(pack.SchemaVersion, artifact.Identity.SchemaVersion);
        Assert.Equal(pack.FormatId, artifact.Identity.FormatId);
        Assert.Equal(pack.PackId, artifact.Identity.PackId);
        Assert.Equal(pack.RulesetId, artifact.Identity.RulesetId);
        Assert.Equal(expectedHash, artifact.Identity.Hash);
        Assert.Equal(expectedBytes.Length, artifact.CanonicalByteCount);
        Assert.Equal(expectedBytes, destination);
        Assert.Equal(expectedBytes, artifact.GetCanonicalBytes());
        Assert.Throws<ArgumentException>(() => artifact.CopyCanonicalBytes(
            new byte[artifact.CanonicalByteCount - 1]));
    }

    [Fact]
    public void CanonicalRoundTripNormalizesInputPropertyAndCollectionOrder()
    {
        using var document = JsonDocument.Parse(ExpectedCanonicalJson);
        var root = document.RootElement;
        var reordered = string.Concat(
            "{",
            "\"packId\":", root.GetProperty("packId").GetRawText(), ",",
            "\"scenarios\":", ReverseArray(root.GetProperty("scenarios")), ",",
            "\"elements\":", ReverseArray(root.GetProperty("elements")), ",",
            "\"formations\":", ReverseArray(root.GetProperty("formations")), ",",
            "\"edges\":", ReverseArray(root.GetProperty("edges")), ",",
            "\"weatherAreaAssignments\":", ReverseArray(root.GetProperty("weatherAreaAssignments")), ",",
            "\"locations\":", ReverseArray(root.GetProperty("locations")), ",",
            "\"sourceIndex\":", ReverseArray(root.GetProperty("sourceIndex")), ",",
            "\"capabilities\":", ReverseArray(root.GetProperty("capabilities")), ",",
            "\"rulesetId\":", root.GetProperty("rulesetId").GetRawText(), ",",
            "\"formatId\":", root.GetProperty("formatId").GetRawText(), ",",
            "\"schemaVersion\":", root.GetProperty("schemaVersion").GetRawText(),
            "}");

        var result = ContentPackSerializer.Deserialize(Encoding.UTF8.GetBytes(reordered));

        Assert.True(result.IsSuccess);
        var definition = Assert.IsType<ContentPackDefinition>(result.Definition);
        Assert.Equal(ContentTestData.CreateMinimalPack(), definition);
        Assert.Equal(
            ExpectedCanonicalJson,
            Encoding.UTF8.GetString(ContentPackSerializer.SerializeCanonical(definition)));
    }

    [Fact]
    public void EverySelectedSemanticMutationChangesTheContentIdentity()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var changedLocations = baseline.Locations
            .Select(location => location.LocationId == "east"
                ? new ContentHex(
                    location.LocationId,
                    "land.terrain.desert",
                    location.SourceCoordinate,
                    location.Origin)
                : location)
            .ToArray();
        var changed = ContentTestData.Copy(baseline, locations: changedLocations);

        Assert.NotEqual(
            ContentPackArtifact.Create(baseline).Identity.Hash,
            ContentPackArtifact.Create(changed).Identity.Hash);
    }

    [Fact]
    public void InvalidPackCannotProduceCanonicalBytesOrIdentity()
    {
        var baseline = ContentTestData.CreateMinimalPack();
        var element = baseline.Elements[0];
        var invalid = ContentTestData.Copy(
            baseline,
            elements:
            [
                new ContentCombatElement(
                    element.ElementId,
                    "commonwealth",
                    element.ParentFormationId,
                    element.OrganizationId,
                    element.MobilityId,
                    element.BaseCapabilityPointAllowance,
                    element.PlacementMode,
                    element.Origin),
            ]);

        var artifactException = Assert.Throws<InvalidContentPackException>(
            () => ContentPackArtifact.Create(invalid));
        var serializerException = Assert.Throws<InvalidContentPackException>(
            () => ContentPackSerializer.SerializeCanonical(invalid));

        Assert.Contains(
            artifactException.Issues,
            issue => issue.Code == "formation.side-mismatch");
        Assert.Equal(artifactException.Issues, serializerException.Issues);
    }

    [Fact]
    public void ContentIdentityRejectsMalformedOrNoncanonicalValues()
    {
        const string validHash =
            "sha256:0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        Assert.Throws<ArgumentOutOfRangeException>(() => new ContentPackIdentity(
            0,
            ContentPackDefinition.CanonicalFormatId,
            "rules-lab.content.minimal.v1",
            "cna-1979.1",
            validHash));
        Assert.Throws<ArgumentException>(() => new ContentPackIdentity(
            ContentPackDefinition.CurrentSchemaVersion,
            "sandtable.content-json.v1",
            "rules-lab.content.minimal.v1",
            "cna-1979.1",
            validHash));
        Assert.Throws<ArgumentException>(() => new ContentPackIdentity(
            ContentPackDefinition.CurrentSchemaVersion,
            ContentPackDefinition.CanonicalFormatId,
            "Rules Lab",
            "cna-1979.1",
            validHash));
        Assert.Throws<ArgumentException>(() => new ContentPackIdentity(
            ContentPackDefinition.CurrentSchemaVersion,
            ContentPackDefinition.CanonicalFormatId,
            "rules-lab.content.minimal.v1",
            "cna-1979.1",
            validHash.ToUpperInvariant()));
    }

    [Fact]
    public void StrictReaderRejectsMalformedOrNoncanonicalSemanticInput()
    {
        var variants = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["duplicate top-level property"] = ExpectedCanonicalJson.Replace(
                "\"schemaVersion\":4,",
                "\"schemaVersion\":4,\"schemaVersion\":4,",
                StringComparison.Ordinal),
            ["unknown top-level property"] = ExpectedCanonicalJson.Replace(
                "\"schemaVersion\":4,",
                "\"schemaVersion\":4,\"unknown\":1,",
                StringComparison.Ordinal),
            ["missing top-level property"] = ExpectedCanonicalJson.Replace(
                "\"packId\":\"rules-lab.content.minimal.v1\",",
                string.Empty,
                StringComparison.Ordinal),
            ["trailing data"] = $"{ExpectedCanonicalJson}{{}}",
            ["unknown schema version"] = ExpectedCanonicalJson.Replace(
                "\"schemaVersion\":4",
                "\"schemaVersion\":5",
                StringComparison.Ordinal),
            ["unknown format"] = ExpectedCanonicalJson.Replace(
                "sandtable.content-json.v3",
                "sandtable.content-json.v2",
                StringComparison.Ordinal),
            ["invalid discriminant"] = ExpectedCanonicalJson.Replace(
                "repository-synthetic",
                "repository-unknown",
                StringComparison.Ordinal),
            ["noninteger number"] = ExpectedCanonicalJson.Replace(
                "\"gameTurn\":1",
                "\"gameTurn\":1.0",
                StringComparison.Ordinal),
            ["unknown nested property"] = ExpectedCanonicalJson.Replace(
                "\"locationId\":\"east\",",
                "\"locationId\":\"east\",\"unknown\":false,",
                StringComparison.Ordinal),
            ["missing nested property"] = ExpectedCanonicalJson.Replace(
                "\"terrainId\":\"land.terrain.clear\",",
                string.Empty,
                StringComparison.Ordinal),
            ["invalid stable ID"] = ExpectedCanonicalJson.Replace(
                "rules-lab.content.minimal.v1",
                "Rules Lab",
                StringComparison.Ordinal),
        };

        foreach (var (name, json) in variants)
        {
            var result = ContentPackSerializer.Deserialize(Encoding.UTF8.GetBytes(json));

            Assert.False(result.IsSuccess, name);
            Assert.Null(result.Definition);
            Assert.False(string.IsNullOrWhiteSpace(result.ErrorCode), name);
            Assert.False(string.IsNullOrWhiteSpace(result.Message), name);
        }
    }

    private static string ReverseArray(JsonElement array) => string.Concat(
        "[",
        string.Join(",", array.EnumerateArray().Reverse().Select(value => value.GetRawText())),
        "]");

    private static readonly string ExpectedCanonicalJson = string.Concat(
        "{\"schemaVersion\":4,",
        "\"formatId\":\"sandtable.content-json.v3\",",
        "\"packId\":\"rules-lab.content.minimal.v1\",",
        "\"rulesetId\":\"cna-1979.1\",",
        "\"capabilities\":[\"land.element-mobility\",\"land.formations\",\"land.hex-topology\",\"land.initial-deployment\"],",
        "\"sourceIndex\":[{\"sourceId\":\"sandtable-rules-lab\",\"kind\":\"repository-synthetic\"}],",
        "\"locations\":[",
        "{\"locationId\":\"east\",\"kind\":\"hex\",\"terrainId\":\"land.terrain.clear\",\"sourceCoordinate\":null,",
        "\"origin\":{\"kind\":\"synthetic\",\"references\":[{\"sourceId\":\"sandtable-rules-lab\",\"locator\":\"content.hex.east\"}]}},",
        "{\"locationId\":\"west\",\"kind\":\"hex\",\"terrainId\":\"land.terrain.clear\",\"sourceCoordinate\":null,",
        "\"origin\":{\"kind\":\"synthetic\",\"references\":[{\"sourceId\":\"sandtable-rules-lab\",\"locator\":\"content.hex.west\"}]}}],",
        "\"weatherAreaAssignments\":[],",
        "\"edges\":[{\"firstLocationId\":\"east\",\"secondLocationId\":\"west\",",
        "\"features\":[{\"featureId\":\"land.edge.road\",\"directionFromLocationId\":null,",
        "\"origin\":{\"kind\":\"synthetic\",\"references\":[{\"sourceId\":\"sandtable-rules-lab\",\"locator\":\"content.edge.road\"}]}}],",
        "\"origin\":{\"kind\":\"synthetic\",\"references\":[{\"sourceId\":\"sandtable-rules-lab\",\"locator\":\"content.edge.east-west\"}]}}],",
        "\"formations\":[{\"formationId\":\"axis-formation\",\"sideId\":\"axis\",\"parentFormationId\":null,",
        "\"organizationId\":\"land.organization.regiment\",",
        "\"origin\":{\"kind\":\"synthetic\",\"references\":[{\"sourceId\":\"sandtable-rules-lab\",\"locator\":\"content.formation.axis\"}]}}],",
        "\"elements\":[{\"elementId\":\"axis-element\",\"sideId\":\"axis\",\"parentFormationId\":\"axis-formation\",",
        "\"organizationId\":\"land.organization.battalion\",\"mobilityId\":\"land.mobility.motorized\",",
        "\"baseCapabilityPointAllowance\":20,\"placementMode\":\"independent\",\"breakdownVehicleCohort\":null,",
        "\"origin\":{\"kind\":\"synthetic\",\"references\":[{\"sourceId\":\"sandtable-rules-lab\",\"locator\":\"content.element.axis\"}]}}],",
        "\"scenarios\":[{\"scenarioId\":\"minimal-lab\",",
        "\"start\":{\"gameTurn\":1,\"operationStage\":1},\"end\":{\"gameTurn\":1,\"operationStage\":3},",
        "\"initialPlacements\":[{\"elementId\":\"axis-element\",\"locationId\":\"west\",",
        "\"origin\":{\"kind\":\"synthetic\",\"references\":[{\"sourceId\":\"sandtable-rules-lab\",\"locator\":\"content.placement.axis\"}]}}],",
        "\"origin\":{\"kind\":\"synthetic\",\"references\":[{\"sourceId\":\"sandtable-rules-lab\",\"locator\":\"content.scenario.minimal\"}]}}]}");
}
