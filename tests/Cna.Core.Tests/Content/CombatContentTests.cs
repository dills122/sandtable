using System.Text;
using System.Text.Json.Nodes;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Content;

public sealed class CombatContentTests
{
    [Fact]
    public void CertifiedScenarioLoadsExactCombatAndSupplyFacts()
    {
        var bytes = CombatContentFixture.Bytes();

        var parsed = ContentPackV7Serializer.Deserialize(bytes);

        Assert.True(parsed.IsSuccess, parsed.Message);
        var artifact = ContentPackV7Artifact.Create(parsed.Definition!);
        Assert.Equal(bytes, artifact.GetCanonicalBytes());
        Assert.Equal(10_339, artifact.CanonicalByteCount);
        Assert.Equal(
            "sha256:ee4fde9638ceb61ec08612fe32f9ac81db05572aac5ca20941e402d2fed25847",
            artifact.Identity.Hash);
        Assert.Equal("rules-lab.content.close-assault.v1", artifact.Identity.PackId);
        Assert.Equal("sandtable.capability.combat-cycle-infantry.v1", artifact.Definition.CapabilityProfileId);
        Assert.Equal(2, artifact.Definition.Elements.Count);
        Assert.All(artifact.Definition.Elements, element =>
        {
            var component = Assert.Single(element.Components);
            Assert.Equal(10, component.MaximumToe);
            Assert.Equal(1, component.OffensiveCloseAssaultRating);
            Assert.Equal(1, component.DefensiveCloseAssaultRating);
        });
        var scenario = Assert.Single(artifact.Definition.Scenarios);
        Assert.Equal("close-assault-positive-v1", scenario.ScenarioId);
        Assert.Equal(2, scenario.InitialPlacements.Count);
        Assert.All(scenario.InitialPlacements, placement =>
        {
            Assert.Equal(10, placement.InitialAmmunition.Points);
            Assert.Equal(10, Assert.Single(placement.InitialComponentToes).CurrentToe);
            Assert.Equal("distributed-for-stage", placement.InitialReadiness.WaterStatus);
            Assert.Equal("distributed-for-stage", placement.InitialReadiness.StoresStatus);
            Assert.False(placement.InitialReadiness.Pinned);
        });
        Assert.Equal(
            ["axis-supply", "commonwealth-supply"],
            scenario.RetreatSupplyAnchors.Select(anchor => anchor.LocationId));
    }

    [Fact]
    public void RejectsEveryFrozenNegativeVectorWithExactDiagnostic()
    {
        var canonical = CombatContentFixture.Bytes();
        var root = JsonNode.Parse(canonical)!;
        var vectors = JsonNode.Parse(CombatContentFixture.Vectors())!["negativeVectors"]!.AsArray();

        foreach (var vectorNode in vectors)
        {
            var vector = vectorNode!.AsObject();
            var candidate = ApplyVector(canonical, root, vector);

            var parsed = ContentPackV7Serializer.Deserialize(candidate);

            Assert.False(parsed.IsSuccess);
            Assert.Equal(vector["code"]!.GetValue<string>(), parsed.ErrorCode);
            Assert.Equal(vector["errorPath"]!.GetValue<string>(), parsed.ErrorPath);
        }
    }

    [Fact]
    public void CanonicalConstructionIsStableAndProvenanceChangesIdentity()
    {
        var definition = ContentPackV7Serializer.Deserialize(CombatContentFixture.Bytes()).Definition!;
        var reordered = definition.WithCollections(
            definition.SourceIndex.Reverse(),
            definition.Locations.Reverse(),
            definition.WeatherAreaAssignments.Reverse(),
            definition.Edges.Reverse(),
            definition.Formations.Reverse(),
            definition.Elements.Reverse(),
            definition.Scenarios.Reverse());

        Assert.Equal(definition, reordered);
        Assert.Equal(CombatContentFixture.Bytes(), ContentPackV7Serializer.SerializeCanonical(reordered));

        var formation = definition.Formations[0];
        var reference = Assert.Single(formation.BasicMoraleOrigin.References);
        var changedFormation = formation with
        {
            BasicMoraleOrigin = new ContentOrigin(
                formation.BasicMoraleOrigin.Kind,
                [new RuleReference(reference.SourceId, reference.Locator + ".v2")]),
        };
        var changed = definition.WithCollections(
            definition.SourceIndex,
            definition.Locations,
            definition.WeatherAreaAssignments,
            definition.Edges,
            [changedFormation, .. definition.Formations.Skip(1)],
            definition.Elements,
            definition.Scenarios);

        var changedArtifact = ContentPackV7Artifact.Create(changed);
        Assert.NotEqual(
            ContentPackV7Artifact.Create(definition).Identity.Hash,
            changedArtifact.Identity.Hash);
        Assert.True(ContentPackV7Serializer.Deserialize(changedArtifact.GetCanonicalBytes()).IsSuccess);
    }

    [Fact]
    public void ArtifactAndDefinitionsDefensivelyCopyCallerOwnedData()
    {
        var definition = ContentPackV7Serializer.Deserialize(CombatContentFixture.Bytes()).Definition!;
        var callerOwned = definition.Elements.ToArray();
        var copy = definition.WithCollections(
            definition.SourceIndex,
            definition.Locations,
            definition.WeatherAreaAssignments,
            definition.Edges,
            definition.Formations,
            callerOwned,
            definition.Scenarios);
        var artifact = ContentPackV7Artifact.Create(copy);
        var bytes = artifact.GetCanonicalBytes();

        callerOwned[0] = callerOwned[1];
        bytes[0] = (byte)'[';

        Assert.Equal("axis-assault-battalion", copy.Elements[0].ElementId);
        Assert.Equal((byte)'{', artifact.GetCanonicalBytes()[0]);
    }

    [Fact]
    public void SerializerRejectsConstructedUnsupportedFactsInsteadOfRepairingThem()
    {
        var definition = ContentPackV7Serializer.Deserialize(CombatContentFixture.Bytes()).Definition!;
        var source = definition.SourceIndex[0];
        var location = definition.Locations[0];
        var weather = definition.WeatherAreaAssignments[0];
        var edge = definition.Edges[0];
        var formation = definition.Formations[0];
        var scenario = definition.Scenarios[0];
        var anchor = scenario.RetreatSupplyAnchors[0];

        Assert.Throws<InvalidContentPackException>(() => ContentPackV7Serializer.SerializeCanonical(
            definition.WithCollections(
                [new ContentSourceIndexEntry(source.SourceId, ContentSourceKind.PublishedPrimary)],
                definition.Locations,
                definition.WeatherAreaAssignments,
                definition.Edges,
                definition.Formations,
                definition.Elements,
                definition.Scenarios)));
        Assert.Throws<InvalidContentPackException>(() => ContentPackV7Serializer.SerializeCanonical(
            definition.WithCollections(
                definition.SourceIndex,
                [new ContentHex(location.LocationId, location.TerrainId, new ContentSourceCoordinate("1", "1"), location.Origin), .. definition.Locations.Skip(1)],
                definition.WeatherAreaAssignments,
                definition.Edges,
                definition.Formations,
                definition.Elements,
                definition.Scenarios)));
        Assert.Throws<InvalidContentPackException>(() => ContentPackV7Serializer.SerializeCanonical(
            definition.WithCollections(
                definition.SourceIndex,
                definition.Locations,
                [new ContentWeatherAreaAssignment(weather.LocationId, ContentWeatherArea.B, weather.Origin), .. definition.WeatherAreaAssignments.Skip(1)],
                definition.Edges,
                definition.Formations,
                definition.Elements,
                definition.Scenarios)));
        Assert.Throws<InvalidContentPackException>(() => ContentPackV7Serializer.SerializeCanonical(
            definition.WithCollections(
                definition.SourceIndex,
                definition.Locations,
                definition.WeatherAreaAssignments,
                [new ContentHexEdge(edge.FirstLocationId, edge.SecondLocationId,
                    [new ContentEdgeFeature("land.route.road", null, edge.Origin)], edge.Origin), .. definition.Edges.Skip(1)],
                definition.Formations,
                definition.Elements,
                definition.Scenarios)));
        Assert.Throws<InvalidContentPackException>(() => ContentPackV7Serializer.SerializeCanonical(
            definition.WithCollections(
                definition.SourceIndex,
                definition.Locations,
                definition.WeatherAreaAssignments,
                definition.Edges,
                [formation with { ParentFormationId = "axis-parent" }, .. definition.Formations.Skip(1)],
                definition.Elements,
                definition.Scenarios)));
        Assert.Throws<InvalidContentPackException>(() => ContentPackV7Serializer.SerializeCanonical(
            definition.WithCollections(
                definition.SourceIndex,
                definition.Locations,
                definition.WeatherAreaAssignments,
                definition.Edges,
                definition.Formations,
                definition.Elements,
                [new ContentCombatScenario(
                    scenario.ScenarioId,
                    scenario.Start,
                    scenario.End,
                    scenario.InitialPlacements,
                    [anchor with { Kind = "ammo-depot" }, .. scenario.RetreatSupplyAnchors.Skip(1)],
                    scenario.Origin)])));
    }

    [Fact]
    public void HistoricalContentReadersAndBytesRemainIndependent()
    {
        var v4Bytes = Cna1979SyntheticContentCatalog.Artifact.GetCanonicalBytes();
        var v5Bytes = Cna1979SyntheticContentCatalog.ArtifactV5.GetCanonicalBytes();
        var v6Bytes = BreakdownContentFixture.Bytes();
        var v7Bytes = CombatContentFixture.Bytes();

        Assert.Equal(v4Bytes, ContentPackSerializer.SerializeCanonical(
            ContentPackSerializer.Deserialize(v4Bytes).Definition!));
        Assert.Equal(v5Bytes, ContentPackV5Serializer.SerializeCanonical(
            ContentPackV5Serializer.Deserialize(v5Bytes).Definition!));
        Assert.Equal(v6Bytes, ContentPackV6Serializer.SerializeCanonical(
            ContentPackV6Serializer.Deserialize(v6Bytes).Definition!));
        Assert.False(ContentPackSerializer.Deserialize(v7Bytes).IsSuccess);
        Assert.False(ContentPackV5Serializer.Deserialize(v7Bytes).IsSuccess);
        Assert.False(ContentPackV6Serializer.Deserialize(v7Bytes).IsSuccess);
        Assert.False(ContentPackV7Serializer.Deserialize(v4Bytes).IsSuccess);
        Assert.False(ContentPackV7Serializer.Deserialize(v5Bytes).IsSuccess);
        Assert.False(ContentPackV7Serializer.Deserialize(v6Bytes).IsSuccess);
    }

    [Fact]
    public void InternalCatalogLoadsOnlyCertifiedCombatScenario()
    {
        Assert.Equal(CombatContentFixture.Bytes(), Cna1979CombatContentCatalog.Artifact.GetCanonicalBytes());
        Assert.Equal("close-assault-positive-v1", Assert.Single(
            Cna1979CombatContentCatalog.Artifact.Definition.Scenarios).ScenarioId);
    }

    private static byte[] ApplyVector(byte[] canonical, JsonNode root, JsonObject vector)
    {
        var operation = vector["op"]!.GetValue<string>();
        if (operation == "raw-replace")
        {
            var text = Encoding.UTF8.GetString(canonical);
            return Encoding.UTF8.GetBytes(text.Replace(
                vector["old"]!.GetValue<string>(),
                vector["new"]!.GetValue<string>(),
                StringComparison.Ordinal));
        }

        if (operation == "append" || operation == "prepend")
        {
            var addition = Encoding.UTF8.GetBytes(vector["value"]!.GetValue<string>());
            return operation == "append" ? [.. canonical, .. addition] : [.. addition, .. canonical];
        }

        var candidate = root.DeepClone();
        var parts = vector["path"]!.GetValue<string>()
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        JsonNode parent = candidate;
        foreach (var part in parts[..^1])
        {
            parent = parent is JsonArray array
                ? array[int.Parse(part, System.Globalization.CultureInfo.InvariantCulture)]!
                : parent[part]!;
        }

        var key = parts[^1];
        if (parent is JsonArray parentArray)
        {
            var index = int.Parse(key, System.Globalization.CultureInfo.InvariantCulture);
            if (operation == "remove")
                parentArray.RemoveAt(index);
            else
                parentArray[index] = vector["value"]!.DeepClone();
        }
        else if (operation == "remove")
        {
            parent.AsObject().Remove(key);
        }
        else
        {
            parent[key] = vector["value"]!.DeepClone();
        }

        return Encoding.UTF8.GetBytes(candidate.ToJsonString());
    }
}

internal static class CombatContentFixture
{
    internal static byte[] Bytes() => File.ReadAllBytes(Path.Combine(
        AppContext.BaseDirectory, "Content", "Fixtures", "combat-content-v7.canonical.json"));

    internal static byte[] Vectors() => File.ReadAllBytes(Path.Combine(
        AppContext.BaseDirectory, "Content", "Fixtures", "combat-content-v7.vectors.json"));
}
