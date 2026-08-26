using Cna.Core.Rules;
using Cna.Core.Setups;

namespace Cna.Core.Tests.Rules;

public sealed class RulesetManifestTests
{
    [Fact]
    public void CanonicalCna1979ManifestDerivesItsIdentityFromTheLandCatalog()
    {
        var manifest = Cna1979Ruleset.Manifest;
        var artifact = Assert.Single(
            manifest.Artifacts,
            value => value.ArtifactId == "cna-1979.1.land-sequence");

        Assert.Equal("cna-1979.1", manifest.RulesetId);
        Assert.Equal(6, manifest.ContractVersion);
        Assert.Equal(7, manifest.Artifacts.Count);
        Assert.Equal("cna-1979.1.land-sequence", artifact.ArtifactId);
        Assert.Equal(
            Cna1979Ruleset.CalculateLandSequenceContentHash(
                Cna1979LandSequence.CreateTurn(1)),
            artifact.ContentHash);
        Assert.Matches("^sha256:[0-9a-f]{64}$", artifact.ContentHash);
        Assert.Contains(
            artifact.Sources,
            source => source == new RuleReference("spi-1979-land-rules", "5.2"));
        Assert.Contains(
            artifact.Sources,
            source => source == new RuleReference("spi-1979-land-rules", "7.11"));
        Assert.Contains(
            artifact.Sources,
            source => source == new RuleReference("spi-1979-land-rules", "7.14"));
        var ruling = Assert.Single(
            manifest.Rulings,
            value => value.RulingId == Cna1979Ruleset.EmptyOpeningConvoyRulingId);
        Assert.Equal(4, manifest.Rulings.Count);
        Assert.Equal(
            "6a220a0aaf6ff20e453474384d5c96b1ec78344d53cc05fafdcffb0fae3aecac",
            manifest.Hash);
        Assert.Equal(Cna1979Ruleset.EmptyOpeningConvoyRulingId, ruling.RulingId);
        Assert.Equal(
            "cna-1979.1.conflict.empty-opening-convoy-phase",
            ruling.ConflictId);
        Assert.Equal(
            "resolve-explicitly-admitted-empty-opening-convoy",
            ruling.SelectedBehaviorId);
        Assert.Equal(
            [
                "reject-empty-opening-convoy-as-unsupported",
                "resolve-explicitly-admitted-empty-opening-convoy",
            ],
            ruling.AlternativeIds);
        Assert.Equal(["ACT-AC-002", "ACT-AC-003", "ACT-AC-016"], ruling.ProtectingTestIds);
        Assert.Equal(
            [
                new RuleReference("spi-1979-land-rules", "5.2"),
                new RuleReference("spi-1979-land-rules", "32.43"),
                new RuleReference("spi-1979-land-rules", "32.61"),
                Cna1979SetupCatalog.OpeningPreambleSourceReference,
            ],
            ruling.Sources);
        Assert.Contains(
            Cna1979SetupCatalog.OpeningPreambleSourceReference,
            ruling.Sources);
        Assert.Same(manifest, Cna1979Ruleset.Manifest);
        Assert.True(Cna1979Ruleset.IsCanonicalHash(manifest.Hash));
        Assert.False(Cna1979Ruleset.IsCanonicalHash(new string('0', 64)));
    }

    [Fact]
    public void LandCatalogHashChangesWhenNormalizedCatalogSemanticsChange()
    {
        var baseline = Cna1979LandSequence.CreateTurn(1);
        var changed = baseline.ToArray();
        var position = changed[0];
        changed[0] = new LandSequencePosition(
            position.ContractVersion,
            position.PositionId,
            position.GameTurn,
            position.OperationStage,
            position.StageId,
            "land.phase.changed-for-test",
            position.SegmentId,
            position.StepId,
            position.ActorRole,
            position.ActiveSide,
            position.Sources);

        var baselineHash = Cna1979Ruleset.CalculateLandSequenceContentHash(baseline);
        var changedHash = Cna1979Ruleset.CalculateLandSequenceContentHash(changed);

        Assert.NotEqual(baselineHash, changedHash);
    }

    [Fact]
    public void LandCatalogHashIncludesCanonicalPerPositionSources()
    {
        var position = Cna1979LandSequence
            .CreateTurn(1)
            .First(value => value.PositionId.Contains(".first-player.", StringComparison.Ordinal));
        var reorderedSources = new LandSequencePosition(
            position.ContractVersion,
            position.PositionId,
            position.GameTurn,
            position.OperationStage,
            position.StageId,
            position.PhaseId,
            position.SegmentId,
            position.StepId,
            position.ActorRole,
            position.ActiveSide,
            position.Sources.Reverse().ToArray());
        var missingOrderSource = new LandSequencePosition(
            position.ContractVersion,
            position.PositionId,
            position.GameTurn,
            position.OperationStage,
            position.StageId,
            position.PhaseId,
            position.SegmentId,
            position.StepId,
            position.ActorRole,
            position.ActiveSide,
            [Cna1979LandSequence.SourceReference]);

        var baselineHash = Cna1979Ruleset.CalculateLandSequenceContentHash([position]);
        var reorderedHash = Cna1979Ruleset.CalculateLandSequenceContentHash([reorderedSources]);
        var missingSourceHash = Cna1979Ruleset.CalculateLandSequenceContentHash([missingOrderSource]);

        Assert.Equal(baselineHash, reorderedHash);
        Assert.NotEqual(baselineHash, missingSourceHash);
    }

    [Fact]
    public void LandCatalogHashIncludesActorRoleSemantics()
    {
        var position = Cna1979LandSequence
            .CreateTurn(1)
            .First(value => value.ActorRole == LandActorRole.FirstActingSide);
        var changedRole = new LandSequencePosition(
            position.ContractVersion,
            position.PositionId,
            position.GameTurn,
            position.OperationStage,
            position.StageId,
            position.PhaseId,
            position.SegmentId,
            position.StepId,
            LandActorRole.SecondActingSide,
            position.ActiveSide,
            position.Sources);

        Assert.NotEqual(
            Cna1979Ruleset.CalculateLandSequenceContentHash([position]),
            Cna1979Ruleset.CalculateLandSequenceContentHash([changedRole]));
    }

    [Fact]
    public void HashIsStableAcrossCanonicalCollectionOrdering()
    {
        var rulesReference = new RuleReference("spi-1979-rules", "land-sequence");
        var errataReference = new RuleReference("spi-1979-errata-09", "land-sequence");
        var artifacts = new[]
        {
            new RulesetArtifact("land-sequence", "sha256:sequence-v1", [errataReference, rulesReference]),
            new RulesetArtifact("terrain-table", "sha256:terrain-v1", [rulesReference]),
        };
        var rulings = new[]
        {
            new Ruling(
                "ruling-001",
                "conflict.sequence-authority",
                ["behavior.original", "behavior.errata"],
                "behavior.errata",
                ["RulesetManifestTests.ErrataPrecedence", "LandSequenceTests.ErrataPrecedence"],
                [rulesReference, errataReference]),
            new Ruling(
                "ruling-002",
                "conflict.literal-reading",
                ["behavior.literal", "behavior.inferred"],
                "behavior.literal",
                ["RulesetManifestTests.LiteralReading"],
                [rulesReference]),
        };

        var first = new RulesetManifest("test-ruleset", 1, artifacts, rulings);
        var reordered = new RulesetManifest(
            "test-ruleset",
            1,
            artifacts.Reverse(),
            rulings
                .Reverse()
                .Select(ruling => new Ruling(
                    ruling.RulingId,
                    ruling.ConflictId,
                    ruling.AlternativeIds.Reverse(),
                    ruling.SelectedBehaviorId,
                    ruling.ProtectingTestIds.Reverse(),
                    ruling.Sources.Reverse())));

        Assert.Equal(first.Hash, reordered.Hash);
        Assert.Matches("^[0-9a-f]{64}$", first.Hash);
    }

    [Fact]
    public void HashChangesWhenAnyAuthoritativeRulingSemanticChanges()
    {
        var baseline = CreateManifest(
            "conflict.literal-reading",
            ["behavior.literal", "behavior.inferred"],
            "behavior.literal",
            ["RulesetManifestTests.LiteralReading"]);
        var changedConflict = CreateManifest(
            "conflict.errata-precedence",
            ["behavior.literal", "behavior.inferred"],
            "behavior.literal",
            ["RulesetManifestTests.LiteralReading"]);
        var changedAlternatives = CreateManifest(
            "conflict.literal-reading",
            ["behavior.literal", "behavior.community"],
            "behavior.literal",
            ["RulesetManifestTests.LiteralReading"]);
        var changedSelection = CreateManifest(
            "conflict.literal-reading",
            ["behavior.literal", "behavior.inferred"],
            "behavior.inferred",
            ["RulesetManifestTests.LiteralReading"]);
        var changedProtectingTest = CreateManifest(
            "conflict.literal-reading",
            ["behavior.literal", "behavior.inferred"],
            "behavior.literal",
            ["RulesetManifestTests.InferredReading"]);

        Assert.NotEqual(baseline.Hash, changedConflict.Hash);
        Assert.NotEqual(baseline.Hash, changedAlternatives.Hash);
        Assert.NotEqual(baseline.Hash, changedSelection.Hash);
        Assert.NotEqual(baseline.Hash, changedProtectingTest.Hash);
    }

    [Fact]
    public void RulingCopiesAndExposesTheCompleteSourcePolicyLedger()
    {
        var alternatives = new List<string> { "behavior.literal", "behavior.errata" };
        var protectingTests = new List<string> { "RulesetManifestTests.ErrataPrecedence" };
        var sources = new List<RuleReference>
        {
            new("spi-1979-rules", "8.37"),
            new("spi-1979-errata-09", "8.37"),
        };

        var ruling = new Ruling(
            "ruling-001",
            "conflict.errata-precedence",
            alternatives,
            "behavior.errata",
            protectingTests,
            sources);

        alternatives.Clear();
        protectingTests.Clear();
        sources.Clear();

        Assert.Equal("conflict.errata-precedence", ruling.ConflictId);
        Assert.Equal(["behavior.literal", "behavior.errata"], ruling.AlternativeIds);
        Assert.Equal("behavior.errata", ruling.SelectedBehaviorId);
        Assert.Equal(["RulesetManifestTests.ErrataPrecedence"], ruling.ProtectingTestIds);
        Assert.Equal(2, ruling.Sources.Count);
    }

    [Fact]
    public void RulingRequiresTheSelectedBehaviorToBeAConsideredAlternative()
    {
        var exception = Assert.Throws<ArgumentException>(() => new Ruling(
            "ruling-001",
            "conflict.errata-precedence",
            ["behavior.literal", "behavior.errata"],
            "behavior.unconsidered",
            ["RulesetManifestTests.ErrataPrecedence"],
            [new RuleReference("spi-1979-rules", "8.37")]));

        Assert.Equal("selectedBehaviorId", exception.ParamName);
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
            new(
                "ruling-001",
                "conflict.literal-reading",
                ["behavior.literal", "behavior.inferred"],
                "behavior.literal",
                ["RulesetManifestTests.LiteralReading"],
                [source]),
        };
        var manifest = new RulesetManifest("test-ruleset", 1, artifacts, rulings);
        var originalHash = manifest.Hash;

        artifacts.Clear();
        rulings.Clear();

        Assert.Single(manifest.Artifacts);
        Assert.Single(manifest.Rulings);
        Assert.Equal(originalHash, manifest.Hash);
    }

    private static RulesetManifest CreateManifest(
        string conflictId,
        IEnumerable<string> alternativeIds,
        string selectedBehaviorId,
        IEnumerable<string> protectingTestIds)
    {
        var source = new RuleReference("spi-1979-rules", "land-sequence");

        return new RulesetManifest(
            "test-ruleset",
            1,
            [new RulesetArtifact("land-sequence", "sha256:sequence-v1", [source])],
            [new Ruling(
                "ruling-001",
                conflictId,
                alternativeIds,
                selectedBehaviorId,
                protectingTestIds,
                [source])]);
    }
}
