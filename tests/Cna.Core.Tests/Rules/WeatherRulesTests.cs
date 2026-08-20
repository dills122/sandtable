using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class WeatherRulesTests
{
    [Theory]
    [InlineData(1, (int)WeatherSeason.Fall)]
    [InlineData(12, (int)WeatherSeason.Fall)]
    [InlineData(13, (int)WeatherSeason.Winter)]
    [InlineData(25, (int)WeatherSeason.Spring)]
    [InlineData(37, (int)WeatherSeason.Summer)]
    [InlineData(49, (int)WeatherSeason.Fall)]
    [InlineData(61, (int)WeatherSeason.Winter)]
    [InlineData(73, (int)WeatherSeason.Spring)]
    [InlineData(85, (int)WeatherSeason.Summer)]
    [InlineData(97, (int)WeatherSeason.Fall)]
    [InlineData(109, (int)WeatherSeason.Winter)]
    [InlineData(110, (int)WeatherSeason.Winter)]
    public void SupportedGameTurnsMapToCorrectedSeason(
        int gameTurn,
        int expected)
    {
        Assert.Equal((WeatherSeason)expected, Cna1979Weather.GetSeason(gameTurn));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(111)]
    [InlineData(112)]
    public void UnsupportedGameTurnsRejectWithoutRandomness(int gameTurn)
    {
        var initial = SandtableRandom.Create(17);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => Cna1979Weather.Resolve(gameTurn, initial));
        Assert.Equal(0UL, initial.NextByteCursor);
    }

    [Theory]
    [InlineData((int)WeatherSeason.Fall, 35, (int)WeatherKind.Normal)]
    [InlineData((int)WeatherSeason.Fall, 36, (int)WeatherKind.Hot)]
    [InlineData((int)WeatherSeason.Fall, 55, (int)WeatherKind.Sandstorm)]
    [InlineData((int)WeatherSeason.Fall, 62, (int)WeatherKind.Rainstorm)]
    [InlineData((int)WeatherSeason.Winter, 52, (int)WeatherKind.Normal)]
    [InlineData((int)WeatherSeason.Winter, 53, (int)WeatherKind.Rainstorm)]
    [InlineData((int)WeatherSeason.Spring, 42, (int)WeatherKind.Normal)]
    [InlineData((int)WeatherSeason.Spring, 43, (int)WeatherKind.Hot)]
    [InlineData((int)WeatherSeason.Spring, 56, (int)WeatherKind.Sandstorm)]
    [InlineData((int)WeatherSeason.Spring, 65, (int)WeatherKind.Rainstorm)]
    [InlineData((int)WeatherSeason.Summer, 23, (int)WeatherKind.Normal)]
    [InlineData((int)WeatherSeason.Summer, 24, (int)WeatherKind.Hot)]
    [InlineData((int)WeatherSeason.Summer, 56, (int)WeatherKind.Sandstorm)]
    public void BoundaryD66ValuesSelectThePublishedOutcome(
        int season,
        int d66,
        int expected)
    {
        Assert.Equal(
            (WeatherKind)expected,
            Cna1979Weather.GetKind((WeatherSeason)season, d66));
    }

    [Theory]
    [InlineData(1, "A", "B")]
    [InlineData(2, "C", "D")]
    [InlineData(3, "D", "E")]
    [InlineData(4, "B", "C")]
    [InlineData(5, "B", "D")]
    [InlineData(6, "B", "C", "D")]
    public void FoulWeatherLocationIsCanonical(
        int die,
        params string[] expected)
    {
        Assert.Equal(expected, Cna1979Weather.GetAffectedAreas(die).Select(value => value.ToString()));
    }

    [Fact]
    public void ResolvePreservesAcceptedDiceAndRejectionSamplingCursor()
    {
        var initial = new RandomStreamState(
            SandtableRandom.ContractVersion,
            SandtableRandom.AlgorithmId,
            0,
            129);

        var result = Cna1979Weather.Resolve(1, initial);

        Assert.Equal(2, result.FirstDie);
        Assert.Equal(1, result.SecondDie);
        Assert.Equal(WeatherKind.Normal, result.Kind);
        Assert.Equal(WeatherScope.None, result.Scope);
        Assert.Null(result.LocationDie);
        Assert.Empty(result.AffectedAreas);
        Assert.Equal(132UL, result.RandomState.NextByteCursor);
    }

    [Fact]
    public void WeatherArtifactIsPresentInTheRulesetManifest()
    {
        var artifact = Assert.Single(
            Cna1979Ruleset.Manifest.Artifacts,
            value => value.ArtifactId == Cna1979Weather.ArtifactId);
        var ruling = Assert.Single(
            Cna1979Ruleset.Manifest.Rulings,
            value => value.RulingId == Cna1979Weather.SeasonBoundaryRulingId);

        var expectedArtifact = Cna1979Weather.CreateArtifact();
        Assert.Equal(expectedArtifact.ArtifactId, artifact.ArtifactId);
        Assert.Equal(expectedArtifact.ContentHash, artifact.ContentHash);
        Assert.Equal(expectedArtifact.Sources, artifact.Sources);
        Assert.Equal(
            "use-rule-29.1-boundaries-and-remap-chart-game-turns",
            ruling.SelectedBehaviorId);
        Assert.Equal(
            ["WTH-AC-001", "WTH-AC-002", "WTH-AC-004"],
            ruling.ProtectingTestIds);
    }

    [Fact]
    public void SeasonBoundaryRulingHasExactCanonicalBytesAndHashSensitiveFields()
    {
        var manifest = Cna1979Ruleset.Manifest;
        var ruling = Assert.Single(manifest.Rulings,
            value => value.RulingId == Cna1979Weather.SeasonBoundaryRulingId);
        var canonical = Encoding.UTF8.GetString(
            RulesetManifest.SerializeCanonicalRuling(ruling));
        var expected = "{\"rulingId\":\"cna-1979.1.ruling.weather-season-boundary\"," +
            "\"conflictId\":\"cna-1979.1.conflict.weather-season-boundary\"," +
            "\"alternativeIds\":[\"use-errata-29.61-parenthetical-and-derive-shifted-ranges\"," +
            "\"use-rule-29.1-boundaries-and-remap-chart-game-turns\"]," +
            "\"selectedBehaviorId\":\"use-rule-29.1-boundaries-and-remap-chart-game-turns\"," +
            "\"protectingTestIds\":[\"WTH-AC-001\",\"WTH-AC-002\",\"WTH-AC-004\"]," +
            "\"sources\":[{\"sourceId\":\"spi-1979-common-charts\",\"locator\":\"29.61\"}," +
            "{\"sourceId\":\"spi-1979-errata\",\"locator\":\"29.1\"}," +
            "{\"sourceId\":\"spi-1979-errata\",\"locator\":\"29.61\"}," +
            "{\"sourceId\":\"spi-1979-land-rules\",\"locator\":\"29.0-29.1\"}]}";

        Assert.Equal(expected, canonical);

        Ruling[] mutations =
        [
            new(ruling.RulingId, $"{ruling.ConflictId}.changed", ruling.AlternativeIds,
                ruling.SelectedBehaviorId, ruling.ProtectingTestIds, ruling.Sources),
            new(ruling.RulingId, ruling.ConflictId,
                [.. ruling.AlternativeIds, "use-weather-year-cycle"],
                ruling.SelectedBehaviorId, ruling.ProtectingTestIds, ruling.Sources),
            new(ruling.RulingId, ruling.ConflictId, ruling.AlternativeIds,
                ruling.AlternativeIds[0], ruling.ProtectingTestIds, ruling.Sources),
            new(ruling.RulingId, ruling.ConflictId, ruling.AlternativeIds,
                ruling.SelectedBehaviorId, [.. ruling.ProtectingTestIds, "WTH-AC-014"],
                ruling.Sources),
            new(ruling.RulingId, ruling.ConflictId, ruling.AlternativeIds,
                ruling.SelectedBehaviorId, ruling.ProtectingTestIds,
                [.. ruling.Sources, new RuleReference("spi-1979-land-rules", "29.2")]),
        ];

        foreach (var mutation in mutations)
        {
            var changed = new RulesetManifest(manifest.RulesetId, manifest.ContractVersion,
                manifest.Artifacts, manifest.Rulings.Select(value =>
                    value.RulingId == mutation.RulingId ? mutation : value));
            Assert.NotEqual(manifest.Hash, changed.Hash);
        }
    }

    [Fact]
    public void CanonicalWeatherArtifactMatchesGoldenAndStrictlyRoundTrips()
    {
        var canonical = WeatherRulesArtifactCodec.SerializeCanonical(
            Cna1979Weather.Definition);
        var goldenFile = File.ReadAllBytes(Path.Combine(
            AppContext.BaseDirectory,
            "Rules",
            "Fixtures",
            "cna-1979.1.weather-tables.v1.golden.json"));
        var golden = goldenFile.AsSpan(0, goldenFile.Length - 1).ToArray();

        var parsed = WeatherRulesArtifactCodec.Deserialize(canonical);

        Assert.Equal((byte)'\n', goldenFile[^1]);
        Assert.Equal(golden, canonical);
        Assert.Equal(Cna1979Weather.Definition, parsed);
        Assert.Equal(Cna1979Weather.Definition.GetHashCode(), parsed.GetHashCode());
        Assert.Equal(canonical, WeatherRulesArtifactCodec.SerializeCanonical(parsed));
        Assert.Equal(
            $"sha256:{Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant()}",
            Cna1979Weather.CreateArtifact().ContentHash);
        Assert.Equal(
            "sha256:10c92c736d61c6f88359b203d0a735df0d8a676b60e170b79b46053cfd223037",
            Cna1979Weather.CreateArtifact().ContentHash);
        Assert.Equal(Cna1979Weather.Definition.Sources, Cna1979Weather.CreateArtifact().Sources);
        Assert.Throws<JsonException>(() => WeatherRulesArtifactCodec.Deserialize(goldenFile));
    }

    [Fact]
    public void EachProvenanceGroupChangesTheArtifactAndRulesetManifestHashes()
    {
        var baselineDefinition = Cna1979Weather.Definition;
        var baselineArtifact = Cna1979Weather.CreateArtifact();
        var baselineManifest = Cna1979Ruleset.Manifest;

        WeatherArtifactProvenance[] provenanceMutations =
        [
            new(
                ReplaceLastLocator(baselineDefinition.Provenance.GameTurnRanges),
                baselineDefinition.Provenance.Outcomes,
                baselineDefinition.Provenance.FoulWeatherLocations),
            new(
                baselineDefinition.Provenance.GameTurnRanges,
                ReplaceLastLocator(baselineDefinition.Provenance.Outcomes),
                baselineDefinition.Provenance.FoulWeatherLocations),
            new(
                baselineDefinition.Provenance.GameTurnRanges,
                baselineDefinition.Provenance.Outcomes,
                ReplaceLastLocator(baselineDefinition.Provenance.FoulWeatherLocations)),
        ];

        foreach (var provenance in provenanceMutations)
        {
            var mutation = CopyDefinition(
                baselineDefinition,
                provenance,
                RecomputeSources(provenance, baselineDefinition.DeferredRules));
            var artifact = Cna1979Weather.CreateArtifact(mutation);
            var manifest = new RulesetManifest(
                baselineManifest.RulesetId,
                baselineManifest.ContractVersion,
                baselineManifest.Artifacts.Select(value =>
                    value.ArtifactId == artifact.ArtifactId ? artifact : value),
                baselineManifest.Rulings);

            Assert.NotEqual(baselineArtifact.ContentHash, artifact.ContentHash);
            Assert.NotEqual(baselineManifest.Hash, manifest.Hash);
        }
    }

    [Fact]
    public void ArtifactValidatorRejectsNoncanonicalProvenanceAndSourceUnionMutations()
    {
        var baseline = Cna1979Weather.Definition;
        WeatherRulesArtifactDefinition[] mutations =
        [
            CopyDefinition(baseline, new WeatherArtifactProvenance(
                baseline.Provenance.GameTurnRanges.Reverse(),
                baseline.Provenance.Outcomes,
                baseline.Provenance.FoulWeatherLocations)),
            CopyDefinition(baseline, new WeatherArtifactProvenance(
                baseline.Provenance.GameTurnRanges,
                [.. baseline.Provenance.Outcomes, baseline.Provenance.Outcomes[0]],
                baseline.Provenance.FoulWeatherLocations)),
            CopyDefinition(baseline, new WeatherArtifactProvenance(
                baseline.Provenance.GameTurnRanges,
                baseline.Provenance.Outcomes,
                baseline.Provenance.FoulWeatherLocations.Reverse())),
            CopyDefinition(baseline, sources: baseline.Sources.Skip(1)),
            CopyDefinition(baseline, sources:
                [.. baseline.Sources, new RuleReference("spi-1979-land-rules", "29.99")]),
            CopyDefinition(baseline, sources: baseline.Sources.Reverse()),
            CopyDefinition(baseline, sources: [.. baseline.Sources, baseline.Sources[0]]),
        ];

        Assert.All(mutations, mutation => Assert.Throws<JsonException>(
            () => WeatherRulesArtifactCodec.SerializeCanonical(mutation)));
    }

    [Fact]
    public void ArtifactParserIndependentlyRejectsAValidJsonSourceUnionMutation()
    {
        var canonical = Encoding.UTF8.GetString(
            WeatherRulesArtifactCodec.SerializeCanonical(Cna1979Weather.Definition));
        var sourcesProperty = canonical.LastIndexOf(",\"sources\":[", StringComparison.Ordinal);
        var firstSource = canonical.IndexOf('{', sourcesProperty);
        var firstSourceEnd = canonical.IndexOf("},", firstSource, StringComparison.Ordinal) + 2;
        var mutated = canonical.Remove(firstSource, firstSourceEnd - firstSource);

        Assert.Throws<JsonException>(() =>
            WeatherRulesArtifactCodec.Deserialize(Encoding.UTF8.GetBytes(mutated)));
    }

    private static WeatherRulesArtifactDefinition CopyDefinition(
        WeatherRulesArtifactDefinition definition,
        WeatherArtifactProvenance? provenance = null,
        IEnumerable<RuleReference>? sources = null) => new(
            definition.SchemaVersion,
            provenance ?? definition.Provenance,
            definition.Seasons,
            definition.FoulWeatherLocations,
            definition.DeferredRules,
            sources ?? definition.Sources);

    private static RuleReference[] ReplaceLastLocator(
        IReadOnlyList<RuleReference> sources) =>
        [
            .. sources.Take(sources.Count - 1),
            new RuleReference(sources[^1].SourceId, $"{sources[^1].Locator}.changed"),
        ];

    private static RuleReference[] RecomputeSources(
        WeatherArtifactProvenance provenance,
        IReadOnlyList<DeferredWeatherRuleDefinition> deferredRules) =>
        provenance.GameTurnRanges
            .Concat(provenance.Outcomes)
            .Concat(provenance.FoulWeatherLocations)
            .Concat(deferredRules.SelectMany(value => value.Sources))
            .Distinct()
            .OrderBy(value => value.SourceId, StringComparer.Ordinal)
            .ThenBy(value => value.Locator, StringComparer.Ordinal)
            .ToArray();
}
