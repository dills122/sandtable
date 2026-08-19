using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class ContentVocabularyTests
{
    [Fact]
    public void CanonicalVocabularyExposesTheClosedSourceCitedRows()
    {
        var entries = Cna1979ContentVocabulary.Entries;

        Assert.Equal(10, entries.Count);
        Assert.Equal(2, entries.Count(value => value.Kind == ContentVocabularyKind.Side));
        Assert.Equal(2, entries.Count(value => value.Kind == ContentVocabularyKind.Terrain));
        Assert.Equal(4, entries.Count(value => value.Kind == ContentVocabularyKind.EdgeFeature));
        Assert.Equal(2, entries.Count(value => value.Kind == ContentVocabularyKind.Organization));
        Assert.All(entries, value =>
        {
            Assert.Equal(Cna1979ContentVocabulary.SchemaVersion, value.SchemaVersion);
            Assert.NotEmpty(value.Sources);
        });

        Assert.True(Cna1979ContentVocabulary.Contains(
            ContentVocabularyKind.Terrain,
            "land.terrain.clear"));
        Assert.False(Cna1979ContentVocabulary.Contains(
            ContentVocabularyKind.Terrain,
            "land.terrain.unknown"));

        Assert.Equal(
            ContentDirectionPolicy.Required,
            Cna1979ContentVocabulary.Get(
                ContentVocabularyKind.EdgeFeature,
                "land.edge.slope").DirectionPolicy);
        Assert.Equal(
            ContentDirectionPolicy.Forbidden,
            Cna1979ContentVocabulary.Get(
                ContentVocabularyKind.EdgeFeature,
                "land.edge.road").DirectionPolicy);
        Assert.Equal(
            ContentDirectionPolicy.NotApplicable,
            Cna1979ContentVocabulary.Get(
                ContentVocabularyKind.Organization,
                "land.organization.battalion").DirectionPolicy);
    }

    [Fact]
    public void VocabularyEntryDefensivelyCopiesAndCanonicalizesSources()
    {
        var sources = new List<RuleReference>
        {
            new("spi-1979-land-rules", "8.47"),
            new("spi-1979-land-rules", "8.33"),
        };
        var entry = new ContentVocabularyEntry(
            1,
            ContentVocabularyKind.EdgeFeature,
            "land.edge.road",
            ContentDirectionPolicy.Forbidden,
            sources);
        var equivalent = new ContentVocabularyEntry(
            1,
            ContentVocabularyKind.EdgeFeature,
            "land.edge.road",
            ContentDirectionPolicy.Forbidden,
            sources.AsEnumerable().Reverse().ToArray());

        sources.Clear();

        Assert.Equal(
            [
                new RuleReference("spi-1979-land-rules", "8.33"),
                new RuleReference("spi-1979-land-rules", "8.47"),
            ],
            entry.Sources);
        Assert.Equal(entry, equivalent);
        Assert.Equal(entry.GetHashCode(), equivalent.GetHashCode());
    }

    [Fact]
    public void VocabularyHashIsOrderIndependentAndSensitiveToEveryRowSemantic()
    {
        var baselineEntries = Cna1979ContentVocabulary.Entries;
        var baseline = Cna1979ContentVocabulary.CalculateContentHash(baselineEntries);
        var reordered = Cna1979ContentVocabulary.CalculateContentHash(
            baselineEntries.Reverse());
        var original = baselineEntries[0];
        var edgeIndex = baselineEntries
            .Select((entry, index) => (entry, index))
            .Single(value => value.entry.Id == "land.edge.slope")
            .index;
        var edge = baselineEntries[edgeIndex];

        var mutations = new (int Index, ContentVocabularyEntry Entry)[]
        {
            (0, new(
                original.SchemaVersion + 1,
                original.Kind,
                original.Id,
                original.DirectionPolicy,
                original.Sources)),
            (0, new(
                original.SchemaVersion,
                ContentVocabularyKind.Terrain,
                original.Id,
                original.DirectionPolicy,
                original.Sources)),
            (0, new(
                original.SchemaVersion,
                original.Kind,
                "axis-changed-for-test",
                original.DirectionPolicy,
                original.Sources)),
            (edgeIndex, new(
                edge.SchemaVersion,
                edge.Kind,
                edge.Id,
                ContentDirectionPolicy.Forbidden,
                edge.Sources)),
            (0, new(
                original.SchemaVersion,
                original.Kind,
                original.Id,
                original.DirectionPolicy,
                [new RuleReference("spi-1979-common-charts", "changed-for-test")])),
        };

        Assert.Equal(baseline, reordered);
        Assert.Matches("^sha256:[0-9a-f]{64}$", baseline);

        foreach (var mutation in mutations)
        {
            var changedEntries = baselineEntries.ToArray();
            changedEntries[mutation.Index] = mutation.Entry;

            Assert.NotEqual(
                baseline,
                Cna1979ContentVocabulary.CalculateContentHash(changedEntries));
        }
    }

    [Fact]
    public void CanonicalRulesetManifestIncludesTheVocabularyArtifact()
    {
        var manifest = Cna1979Ruleset.Manifest;
        var artifact = Assert.Single(
            manifest.Artifacts,
            value => value.ArtifactId == Cna1979ContentVocabulary.ArtifactId);

        Assert.Equal(5, manifest.Artifacts.Count);
        Assert.Equal(
            Cna1979ContentVocabulary.CalculateContentHash(
                Cna1979ContentVocabulary.Entries),
            artifact.ContentHash);
        Assert.Equal(
            Cna1979ContentVocabulary.CreateArtifact().Sources,
            artifact.Sources);
        Assert.Contains(
            artifact.Sources,
            source => source == new RuleReference(
                "spi-1979-land-rules",
                "8.45.clear-hex"));
        Assert.Contains(
            artifact.Sources,
            source => source == new RuleReference(
                "spi-1979-common-charts",
                "9.4.stacking-point-values"));
    }
}
