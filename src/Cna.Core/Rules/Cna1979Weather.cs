using System.Security.Cryptography;
using System.Text.Json;
using Cna.Core.Randomness;

namespace Cna.Core.Rules;

internal static class Cna1979Weather
{
    public const int SchemaVersion = 1;
    public const string ArtifactId = "cna-1979.1.weather-tables";
    public const string SeasonBoundaryRulingId =
        "cna-1979.1.ruling.weather-season-boundary";

    private static readonly RuleReference[] GameTurnRangeSources =
    [
        new(SeasonBoundaryRulingId, "selected-behavior"),
        new("spi-1979-common-charts", "29.61"),
        new("spi-1979-errata", "29.1"),
        new("spi-1979-errata", "29.61"),
        new("spi-1979-land-rules", "29.0-29.1"),
    ];

    private static readonly RuleReference[] OutcomeSources =
    [
        new("spi-1979-common-charts", "29.61"),
        new("spi-1979-errata", "29.61"),
    ];

    private static readonly RuleReference[] FoulLocationSources =
    [
        new("spi-1979-common-charts", "29.7"),
        new("spi-1979-land-rules", "29.1"),
    ];

    private static readonly RuleReference DeferredDeltaSource =
        new("spi-1979-land-rules", "29.41");

    private static readonly RuleReference[] AllSources =
        GameTurnRangeSources
            .Concat(OutcomeSources)
            .Concat(FoulLocationSources)
            .Append(DeferredDeltaSource)
            .Distinct()
            .OrderBy(value => value.SourceId, StringComparer.Ordinal)
            .ThenBy(value => value.Locator, StringComparer.Ordinal)
            .ToArray();

    private static readonly (WeatherSeason Season, int First, int Last)[] TurnRanges =
    [
        (WeatherSeason.Fall, 1, 12),
        (WeatherSeason.Fall, 49, 60),
        (WeatherSeason.Fall, 97, 108),
        (WeatherSeason.Winter, 13, 24),
        (WeatherSeason.Winter, 61, 72),
        (WeatherSeason.Winter, 109, 110),
        (WeatherSeason.Spring, 25, 36),
        (WeatherSeason.Spring, 73, 84),
        (WeatherSeason.Summer, 37, 48),
        (WeatherSeason.Summer, 85, 96),
    ];

    private static readonly (WeatherSeason Season, WeatherKind Kind, int First, int Last)[] Outcomes =
    [
        (WeatherSeason.Fall, WeatherKind.Normal, 11, 35),
        (WeatherSeason.Fall, WeatherKind.Hot, 36, 54),
        (WeatherSeason.Fall, WeatherKind.Sandstorm, 55, 61),
        (WeatherSeason.Fall, WeatherKind.Rainstorm, 62, 66),
        (WeatherSeason.Winter, WeatherKind.Normal, 11, 52),
        (WeatherSeason.Winter, WeatherKind.Rainstorm, 53, 66),
        (WeatherSeason.Spring, WeatherKind.Normal, 11, 42),
        (WeatherSeason.Spring, WeatherKind.Hot, 43, 55),
        (WeatherSeason.Spring, WeatherKind.Sandstorm, 56, 64),
        (WeatherSeason.Spring, WeatherKind.Rainstorm, 65, 66),
        (WeatherSeason.Summer, WeatherKind.Normal, 11, 23),
        (WeatherSeason.Summer, WeatherKind.Hot, 24, 55),
        (WeatherSeason.Summer, WeatherKind.Sandstorm, 56, 66),
    ];

    private static readonly Dictionary<int, IReadOnlyList<WeatherArea>> FoulAreas =
        new Dictionary<int, IReadOnlyList<WeatherArea>>
        {
            [1] = Array.AsReadOnly([WeatherArea.A, WeatherArea.B]),
            [2] = Array.AsReadOnly([WeatherArea.C, WeatherArea.D]),
            [3] = Array.AsReadOnly([WeatherArea.D, WeatherArea.E]),
            [4] = Array.AsReadOnly([WeatherArea.B, WeatherArea.C]),
            [5] = Array.AsReadOnly([WeatherArea.B, WeatherArea.D]),
            [6] = Array.AsReadOnly([WeatherArea.B, WeatherArea.C, WeatherArea.D]),
        };

    static Cna1979Weather()
    {
        for (var turn = 1; turn <= 110; turn++)
        {
            _ = GetSeason(turn);
        }

        foreach (var season in Enum.GetValues<WeatherSeason>())
        {
            for (var tens = 1; tens <= 6; tens++)
            {
                for (var ones = 1; ones <= 6; ones++)
                {
                    _ = GetKind(season, (tens * 10) + ones);
                }
            }
        }
    }

    public static WeatherSeason GetSeason(int gameTurn)
    {
        var matches = TurnRanges.Where(value =>
            gameTurn >= value.First && gameTurn <= value.Last).ToArray();

        return matches.Length == 1
            ? matches[0].Season
            : throw new ArgumentOutOfRangeException(
                nameof(gameTurn),
                gameTurn,
                "Weather supports Game Turns 1 through 110 only.");
    }

    public static WeatherKind GetKind(WeatherSeason season, int d66)
    {
        if (!Enum.IsDefined(season)
            || d66 / 10 is < 1 or > 6
            || d66 % 10 is < 1 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(d66));
        }

        var matches = Outcomes.Where(value =>
            value.Season == season && d66 >= value.First && d66 <= value.Last).ToArray();

        return matches.Length == 1
            ? matches[0].Kind
            : throw new InvalidOperationException(
                $"Weather table does not define exactly one outcome for {season} {d66}.");
    }

    public static IReadOnlyList<WeatherArea> GetAffectedAreas(int locationDie) =>
        FoulAreas.TryGetValue(locationDie, out var areas)
            ? areas
            : throw new ArgumentOutOfRangeException(nameof(locationDie));

    public static WeatherResolution Resolve(int gameTurn, RandomStreamState initialState)
    {
        var season = GetSeason(gameTurn);
        ArgumentNullException.ThrowIfNull(initialState);
        var first = SandtableRandom.RollD6(initialState);
        var second = SandtableRandom.RollD6(first.State);
        var kind = GetKind(season, (first.Value * 10) + second.Value);

        if (kind is WeatherKind.Sandstorm or WeatherKind.Rainstorm)
        {
            var location = SandtableRandom.RollD6(second.State);
            return new WeatherResolution(
                season,
                first.Value,
                second.Value,
                kind,
                WeatherScope.ListedAreas,
                location.Value,
                GetAffectedAreas(location.Value),
                location.State);
        }

        return new WeatherResolution(
            season,
            first.Value,
            second.Value,
            kind,
            kind == WeatherKind.Hot ? WeatherScope.Global : WeatherScope.None,
            null,
            Array.Empty<WeatherArea>(),
            second.State);
    }

    public static RulesetArtifact CreateArtifact() => new(
        ArtifactId,
        CalculateContentHash(),
        AllSources);

    public static Ruling CreateSeasonBoundaryRuling() => new(
        SeasonBoundaryRulingId,
        "cna-1979.1.conflict.weather-season-boundary",
        [
            "use-errata-29.61-parenthetical-and-derive-shifted-ranges",
            "use-rule-29.1-boundaries-and-remap-chart-game-turns",
        ],
        "use-rule-29.1-boundaries-and-remap-chart-game-turns",
        ["WTH-AC-001", "WTH-AC-002", "WTH-AC-004"],
        [
            new RuleReference("spi-1979-common-charts", "29.61"),
            new RuleReference("spi-1979-errata", "29.1"),
            new RuleReference("spi-1979-errata", "29.61"),
            new RuleReference("spi-1979-land-rules", "29.0-29.1"),
        ]);

    private static string CalculateContentHash()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", SchemaVersion);
            writer.WriteStartObject("provenance");
            WriteSources(writer, "gameTurnRanges", GameTurnRangeSources);
            WriteSources(writer, "outcomes", OutcomeSources);
            WriteSources(writer, "foulWeatherLocations", FoulLocationSources);
            writer.WriteEndObject();
            writer.WriteStartArray("seasons");

            foreach (var season in Enum.GetValues<WeatherSeason>())
            {
                writer.WriteStartObject();
                writer.WriteString("season", FormatSeason(season));
                writer.WriteStartArray("gameTurnRanges");
                foreach (var range in TurnRanges.Where(value => value.Season == season).OrderBy(value => value.First))
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("first", range.First);
                    writer.WriteNumber("last", range.Last);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteStartArray("outcomes");
                foreach (var outcome in Outcomes.Where(value => value.Season == season).OrderBy(value => value.Kind))
                {
                    writer.WriteStartObject();
                    writer.WriteString("kind", FormatKind(outcome.Kind));
                    writer.WriteNumber("firstD66", outcome.First);
                    writer.WriteNumber("lastD66", outcome.Last);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteStartArray("foulWeatherLocations");
            foreach (var row in FoulAreas.OrderBy(value => value.Key))
            {
                writer.WriteStartObject();
                writer.WriteNumber("die", row.Key);
                writer.WriteStartArray("areas");
                foreach (var area in row.Value)
                {
                    writer.WriteStringValue(FormatArea(area));
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("deferredRules");
            writer.WriteStartObject();
            writer.WriteString("ruleId", "nile-delta-sandstorm-exclusion");
            writer.WriteString("weatherKind", "sandstorm");
            writer.WriteString("area", "e");
            writer.WriteString("status", "deferred");
            WriteSources(writer, "sources", [DeferredDeltaSource]);
            writer.WriteEndObject();
            writer.WriteEndArray();
            WriteSources(writer, "sources", AllSources);
            writer.WriteEndObject();
        }

        return $"sha256:{Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant()}";
    }

    private static void WriteSources(Utf8JsonWriter writer, string propertyName, IEnumerable<RuleReference> sources)
    {
        writer.WriteStartArray(propertyName);
        foreach (var source in sources)
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", source.SourceId);
            writer.WriteString("locator", source.Locator);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static string FormatSeason(WeatherSeason season) => season.ToString().ToLowerInvariant();
    private static string FormatKind(WeatherKind kind) => kind.ToString().ToLowerInvariant();
    private static string FormatArea(WeatherArea area) => area.ToString().ToLowerInvariant();
}
