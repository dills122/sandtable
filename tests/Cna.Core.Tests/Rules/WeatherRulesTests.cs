using System.Text;
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
}
