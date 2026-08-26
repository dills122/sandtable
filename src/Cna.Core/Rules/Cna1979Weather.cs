using Cna.Core.Randomness;

namespace Cna.Core.Rules;

internal static class Cna1979Weather
{
    public const int SchemaVersion = 1;
    public const string ArtifactId = "cna-1979.1.weather-tables";
    public const string SeasonBoundaryRulingId =
        "cna-1979.1.ruling.weather-season-boundary";

    internal const int MaxGameTurn = 110;

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

    public static WeatherRulesArtifactDefinition Definition { get; } = CreateDefinition();

    static Cna1979Weather()
    {
        WeatherRulesArtifactValidator.Validate(Definition);
    }

    public static WeatherSeason GetSeason(int gameTurn)
    {
        var matches = Definition.Seasons.Where(value =>
            value.GameTurnRanges.Any(range => range.Contains(gameTurn))).ToArray();

        return matches.Length == 1
            ? matches[0].Season
            : throw new ArgumentOutOfRangeException(
                nameof(gameTurn),
                gameTurn,
                $"Weather supports Game Turns 1 through {MaxGameTurn} only.");
    }

    public static WeatherKind GetKind(WeatherSeason season, int d66)
    {
        if (!Enum.IsDefined(season)
            || d66 / 10 is < 1 or > 6
            || d66 % 10 is < 1 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(d66));
        }

        var table = Definition.Seasons.SingleOrDefault(value => value.Season == season);
        var matches = table?.Outcomes.Where(value =>
            d66 >= value.FirstD66 && d66 <= value.LastD66).ToArray() ?? [];

        return matches.Length == 1
            ? matches[0].Kind
            : throw new InvalidOperationException(
                $"Weather table does not define exactly one outcome for {season} {d66}.");
    }

    public static IReadOnlyList<WeatherArea> GetAffectedAreas(int locationDie)
    {
        var definition = Definition.FoulWeatherLocations.SingleOrDefault(value =>
            value.Die == locationDie);
        return definition?.Areas
            ?? throw new ArgumentOutOfRangeException(nameof(locationDie));
    }

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

    public static RulesetArtifact CreateArtifact() => CreateArtifact(Definition);

    public static RulesetArtifact CreateArtifact(WeatherRulesArtifactDefinition definition) => new(
        ArtifactId,
        CalculateContentHash(definition),
        definition.Sources);

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

    private static string CalculateContentHash(WeatherRulesArtifactDefinition definition)
    {
        var bytes = WeatherRulesArtifactCodec.SerializeCanonical(definition);
        return $"sha256:{Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant()}";
    }

    private static WeatherRulesArtifactDefinition CreateDefinition()
    {
        var provenance = new WeatherArtifactProvenance(
            GameTurnRangeSources,
            OutcomeSources,
            FoulLocationSources);
        DeferredWeatherRuleDefinition[] deferredRules =
        [
            new(
                "nile-delta-sandstorm-exclusion",
                WeatherKind.Sandstorm,
                WeatherArea.E,
                "deferred",
                [DeferredDeltaSource]),
        ];
        var sources = GameTurnRangeSources
            .Concat(OutcomeSources)
            .Concat(FoulLocationSources)
            .Append(DeferredDeltaSource)
            .Distinct()
            .OrderBy(value => value.SourceId, StringComparer.Ordinal)
            .ThenBy(value => value.Locator, StringComparer.Ordinal);

        return new WeatherRulesArtifactDefinition(
            SchemaVersion,
            provenance,
            [
                new WeatherTableDefinition(
                    WeatherSeason.Fall,
                    [new(1, 12), new(49, 60), new(97, 108)],
                    [
                        new(WeatherKind.Normal, 11, 35),
                        new(WeatherKind.Hot, 36, 54),
                        new(WeatherKind.Sandstorm, 55, 61),
                        new(WeatherKind.Rainstorm, 62, 66),
                    ]),
                new WeatherTableDefinition(
                    WeatherSeason.Winter,
                    [new(13, 24), new(61, 72), new(109, 110)],
                    [
                        new(WeatherKind.Normal, 11, 52),
                        new(WeatherKind.Rainstorm, 53, 66),
                    ]),
                new WeatherTableDefinition(
                    WeatherSeason.Spring,
                    [new(25, 36), new(73, 84)],
                    [
                        new(WeatherKind.Normal, 11, 42),
                        new(WeatherKind.Hot, 43, 55),
                        new(WeatherKind.Sandstorm, 56, 64),
                        new(WeatherKind.Rainstorm, 65, 66),
                    ]),
                new WeatherTableDefinition(
                    WeatherSeason.Summer,
                    [new(37, 48), new(85, 96)],
                    [
                        new(WeatherKind.Normal, 11, 23),
                        new(WeatherKind.Hot, 24, 55),
                        new(WeatherKind.Sandstorm, 56, 66),
                    ]),
            ],
            [
                new(1, [WeatherArea.A, WeatherArea.B]),
                new(2, [WeatherArea.C, WeatherArea.D]),
                new(3, [WeatherArea.D, WeatherArea.E]),
                new(4, [WeatherArea.B, WeatherArea.C]),
                new(5, [WeatherArea.B, WeatherArea.D]),
                new(6, [WeatherArea.B, WeatherArea.C, WeatherArea.D]),
            ],
            deferredRules,
            sources);
    }
}
