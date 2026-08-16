using System.Text;
using Cna.Core.Content;

namespace Cna.Core.Tests.Content;

public sealed class SyntheticContentCatalogTests
{
    [Fact]
    public void CatalogLoadsTheCompleteOriginalRulesLaboratory()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;
        var pack = artifact.Definition;
        var scenario = Assert.Single(pack.Scenarios);

        Assert.Equal("rules-lab.content.movement-contact.v1", pack.PackId);
        Assert.Equal("cna-1979.1", pack.RulesetId);
        Assert.Equal(
            ["land.formations", "land.hex-topology", "land.initial-deployment"],
            pack.Capabilities);
        Assert.Equal(9, pack.Locations.Count);
        Assert.Equal(10, pack.Edges.Count);
        Assert.Equal(2, pack.Formations.Count);
        Assert.Equal(4, pack.Elements.Count);
        Assert.Equal(4, scenario.InitialPlacements.Count);
        Assert.Equal(new ContentScenarioBoundary(1, 1), scenario.Start);
        Assert.Equal(new ContentScenarioBoundary(1, 3), scenario.End);
        Assert.True(ContentPackValidator.Validate(pack).IsValid);
        Assert.True(Cna1979ContentCompatibilityValidator.Validate(pack).IsValid);
    }

    [Fact]
    public void CatalogResolutionRequiresExactPackIdAndHash()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;

        var resolved = Cna1979SyntheticContentCatalog.Resolve(
            artifact.Identity.PackId,
            artifact.Identity.Hash);
        var unknown = Cna1979SyntheticContentCatalog.Resolve(
            "rules-lab.content.unknown.v1",
            artifact.Identity.Hash);
        var mismatched = Cna1979SyntheticContentCatalog.Resolve(
            artifact.Identity.PackId,
            $"sha256:{new string('0', 64)}");

        Assert.True(resolved.IsResolved);
        Assert.Same(artifact, resolved.Artifact);
        Assert.Equal(ContentCatalogRejectionReason.None, resolved.RejectionReason);
        Assert.False(unknown.IsResolved);
        Assert.Null(unknown.Artifact);
        Assert.Equal(ContentCatalogRejectionReason.UnknownPackId, unknown.RejectionReason);
        Assert.False(mismatched.IsResolved);
        Assert.Null(mismatched.Artifact);
        Assert.Equal(ContentCatalogRejectionReason.HashMismatch, mismatched.RejectionReason);
    }

    [Fact]
    public void FixtureCanonicalArtifactRoundTripsWithAFrozenIdentity()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;

        var parsed = ContentPackSerializer.Deserialize(artifact.GetCanonicalBytes());

        Assert.True(parsed.IsSuccess);
        var definition = Assert.IsType<ContentPackDefinition>(parsed.Definition);
        Assert.Equal(artifact.Definition, definition);
        Assert.Equal(
            artifact.GetCanonicalBytes(),
            ContentPackSerializer.SerializeCanonical(definition));
        Assert.Equal(
            "sha256:0cf3b3ff21f7a8a8fbcd2667a6b4b3db83b4dab00495add723e5c2f355cf2800",
            artifact.Identity.Hash);
        Assert.Equal(9_897, artifact.CanonicalByteCount);
        var goldenFile = File.ReadAllBytes(Path.Combine(
            AppContext.BaseDirectory,
            "Content",
            "Fixtures",
            "rules-lab.content.movement-contact.v1.golden.json"));

        Assert.Equal((byte)'\n', goldenFile[^1]);
        Assert.Equal(
            artifact.GetCanonicalBytes(),
            goldenFile.AsSpan(0, goldenFile.Length - 1).ToArray());
    }

    [Fact]
    public void EveryFixtureDatumIsSyntheticAndUsesTheRepositorySource()
    {
        var pack = Cna1979SyntheticContentCatalog.Artifact.Definition;
        var origins = EnumerateOrigins(pack).ToArray();

        Assert.Equal(
            [new ContentSourceIndexEntry(
                "sandtable-rules-lab",
                ContentSourceKind.RepositorySynthetic)],
            pack.SourceIndex);
        Assert.NotEmpty(origins);
        Assert.All(origins, origin =>
        {
            Assert.Equal(ContentOriginKind.Synthetic, origin.Kind);
            Assert.NotEmpty(origin.References);
            Assert.All(
                origin.References,
                reference => Assert.Equal("sandtable-rules-lab", reference.SourceId));
            Assert.All(
                origin.References,
                reference => Assert.StartsWith(
                    "content.movement-contact.v1",
                    reference.Locator,
                    StringComparison.Ordinal));
        });
        Assert.Equal(
            origins.SelectMany(origin => origin.References).Count(),
            origins.SelectMany(origin => origin.References).Distinct().Count());
    }

    [Fact]
    public void PresentationIsOriginalNonhistoricalAndOutsideAuthoritativeIdentity()
    {
        var artifact = Cna1979SyntheticContentCatalog.Artifact;
        var presentation = Cna1979SyntheticContentCatalog.Presentation;
        var changedPresentation = new ContentPresentationCatalog(
            artifact.Identity.PackId,
            "Changed exercise name",
            "Changed presentation only",
            new Dictionary<string, string>());

        Assert.Equal(artifact.Identity.PackId, presentation.PackId);
        Assert.Equal("Amber Wadi Exercise", presentation.DisplayName);
        Assert.Contains("Original synthetic", presentation.Notice, StringComparison.Ordinal);
        Assert.Contains("nonhistorical", presentation.Notice, StringComparison.Ordinal);
        Assert.Contains("not a CNA scenario", presentation.Notice, StringComparison.Ordinal);
        Assert.Contains("Copper Group", presentation.Labels.Values);
        Assert.Contains("Azure Group", presentation.Labels.Values);
        Assert.NotEqual(presentation, changedPresentation);
        Assert.Equal(
            artifact.Identity.Hash,
            ContentPackArtifact.Create(artifact.Definition).Identity.Hash);
        Assert.DoesNotContain(
            presentation.DisplayName,
            Encoding.UTF8.GetString(artifact.GetCanonicalBytes()),
            StringComparison.Ordinal);
    }

    private static IEnumerable<ContentOrigin> EnumerateOrigins(ContentPackDefinition pack)
    {
        foreach (var location in pack.Locations)
        {
            yield return location.Origin;
        }

        foreach (var edge in pack.Edges)
        {
            yield return edge.Origin;

            foreach (var feature in edge.Features)
            {
                yield return feature.Origin;
            }
        }

        foreach (var formation in pack.Formations)
        {
            yield return formation.Origin;
        }

        foreach (var element in pack.Elements)
        {
            yield return element.Origin;
        }

        foreach (var scenario in pack.Scenarios)
        {
            yield return scenario.Origin;

            foreach (var placement in scenario.InitialPlacements)
            {
                yield return placement.Origin;
            }
        }
    }
}
