using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class RulesetManifestTests
{
    [Fact]
    public void HashIsStableAcrossInputOrdering()
    {
        var rulesReference = new RuleReference("spi-1979-rules", "land-sequence");
        var errataReference = new RuleReference("spi-1979-errata-09", "land-sequence");
        var artifacts = new[]
        {
            new RulesetArtifact("land-sequence", "sha256:sequence-v1", [rulesReference]),
            new RulesetArtifact("terrain-table", "sha256:terrain-v1", [rulesReference]),
        };
        var rulings = new[]
        {
            new Ruling("ruling-001", "errata-precedes-original", [rulesReference, errataReference]),
            new Ruling("ruling-002", "literal-original-rule", [rulesReference]),
        };

        var first = new RulesetManifest("cna-1979.1", 1, artifacts, rulings);
        var reordered = new RulesetManifest(
            "cna-1979.1",
            1,
            artifacts.Reverse(),
            rulings.Reverse());

        Assert.Equal(first.Hash, reordered.Hash);
        Assert.Matches("^[0-9a-f]{64}$", first.Hash);
    }

    [Fact]
    public void HashChangesWhenAuthoritativeArtifactOrRulingChanges()
    {
        var source = new RuleReference("spi-1979-rules", "land-sequence");
        var baseline = CreateManifest("sha256:sequence-v1", "literal-original-rule", source);
        var changedArtifact = CreateManifest("sha256:sequence-v2", "literal-original-rule", source);
        var changedRuling = CreateManifest("sha256:sequence-v1", "errata-precedes-original", source);

        Assert.NotEqual(baseline.Hash, changedArtifact.Hash);
        Assert.NotEqual(baseline.Hash, changedRuling.Hash);
    }

    [Fact]
    public void ConstructorCopiesAuthoritativeCollections()
    {
        var source = new RuleReference("spi-1979-rules", "land-sequence");
        var artifacts = new List<RulesetArtifact>
        {
            new("land-sequence", "sha256:sequence-v1", [source]),
        };
        var rulings = new List<Ruling>
        {
            new("ruling-001", "literal-original-rule", [source]),
        };
        var manifest = new RulesetManifest("cna-1979.1", 1, artifacts, rulings);
        var originalHash = manifest.Hash;

        artifacts.Clear();
        rulings.Clear();

        Assert.Single(manifest.Artifacts);
        Assert.Single(manifest.Rulings);
        Assert.Equal(originalHash, manifest.Hash);
    }

    private static RulesetManifest CreateManifest(
        string artifactHash,
        string rulingDecision,
        RuleReference source) => new(
            "cna-1979.1",
            1,
            [new RulesetArtifact("land-sequence", artifactHash, [source])],
            [new Ruling("ruling-001", rulingDecision, [source])]);
}
