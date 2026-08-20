using System.Text.Json;

namespace Cna.Core.Rules;

internal static class WeatherRulesArtifactValidator
{
    private static readonly WeatherSeason[] Seasons =
    [
        WeatherSeason.Fall,
        WeatherSeason.Winter,
        WeatherSeason.Spring,
        WeatherSeason.Summer,
    ];

    public static void Validate(WeatherRulesArtifactDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Require(definition.SchemaVersion == Cna1979Weather.SchemaVersion,
            "The Weather artifact schema version is unsupported.");
        ValidateSources(definition.Provenance.GameTurnRanges, "game-turn-range provenance");
        ValidateSources(definition.Provenance.Outcomes, "outcome provenance");
        ValidateSources(
            definition.Provenance.FoulWeatherLocations,
            "foul-location provenance");
        ValidateSeasons(definition.Seasons);
        ValidateFoulLocations(definition.FoulWeatherLocations);
        ValidateDeferredRules(definition.DeferredRules);
        ValidateSourceUnion(definition);
    }

    private static void ValidateSeasons(IReadOnlyList<WeatherTableDefinition> definitions)
    {
        Require(
            definitions.Count == Seasons.Length
                && !definitions.Any(value => value is null)
                && definitions.Select(value => value.Season).SequenceEqual(Seasons),
            "Weather seasons must be unique and ordered fall through summer.");

        foreach (var definition in definitions)
        {
            Require(
                definition.GameTurnRanges.Count > 0
                    && !definition.GameTurnRanges.Any(value => value is null)
                    && definition.GameTurnRanges.SequenceEqual(
                        definition.GameTurnRanges.OrderBy(value => value.First)),
                $"Game-Turn ranges for {definition.Season} are noncanonical.");
            Require(
                definition.GameTurnRanges.All(value => value.First >= 1 && value.Last <= 110),
                $"Game-Turn ranges for {definition.Season} are out of bounds.");

            Require(
                definition.Outcomes.Count > 0
                    && !definition.Outcomes.Any(value => value is null)
                    && definition.Outcomes.Select(value => value.Kind).Distinct().Count()
                        == definition.Outcomes.Count
                    && definition.Outcomes.SequenceEqual(
                        definition.Outcomes.OrderBy(value => value.Kind)),
                $"Weather outcomes for {definition.Season} are noncanonical.");

            foreach (var outcome in definition.Outcomes)
            {
                Require(
                    Enum.IsDefined(outcome.Kind)
                        && IsD66(outcome.FirstD66)
                        && IsD66(outcome.LastD66)
                        && outcome.FirstD66 <= outcome.LastD66,
                    $"Weather outcome {definition.Season}/{outcome.Kind} has an invalid range.");
            }

            foreach (var d66 in ValidD66Values())
            {
                Require(
                    definition.Outcomes.Count(value =>
                        d66 >= value.FirstD66 && d66 <= value.LastD66) == 1,
                    $"Weather outcome {definition.Season}/{d66} is not defined exactly once.");
            }
        }

        foreach (var gameTurn in Enumerable.Range(1, 110))
        {
            Require(
                definitions.Sum(value => value.GameTurnRanges.Count(range =>
                    range.Contains(gameTurn))) == 1,
                $"Game Turn {gameTurn} is not assigned to exactly one Weather season.");
        }
    }

    private static void ValidateFoulLocations(
        IReadOnlyList<FoulWeatherLocationDefinition> definitions)
    {
        Require(
            definitions.Count == 6
                && !definitions.Any(value => value is null)
                && definitions.Select(value => value.Die).SequenceEqual(Enumerable.Range(1, 6)),
            "Foul Weather locations must define dice 1 through 6 in order.");

        foreach (var definition in definitions)
        {
            Require(
                definition.Areas.Count > 0
                    && definition.Areas.All(Enum.IsDefined)
                    && definition.Areas.Distinct().Count() == definition.Areas.Count
                    && definition.Areas.SequenceEqual(definition.Areas.Order()),
                $"Foul Weather location {definition.Die} has noncanonical areas.");
        }
    }

    private static void ValidateDeferredRules(
        IReadOnlyList<DeferredWeatherRuleDefinition> definitions)
    {
        Require(
            definitions.Count > 0 && !definitions.Any(value => value is null),
            "At least one deferred Weather rule is required.");

        foreach (var definition in definitions)
        {
            Require(
                !string.IsNullOrWhiteSpace(definition.RuleId)
                    && Enum.IsDefined(definition.WeatherKind)
                    && Enum.IsDefined(definition.Area)
                    && string.Equals(definition.Status, "deferred", StringComparison.Ordinal),
                "A deferred Weather rule is invalid.");
            ValidateSources(definition.Sources, $"deferred rule '{definition.RuleId}'");
        }

        Require(
            definitions.Select(value => value.RuleId).Distinct(StringComparer.Ordinal).Count()
                == definitions.Count,
            "Deferred Weather rule identifiers must be unique.");
    }

    private static void ValidateSourceUnion(WeatherRulesArtifactDefinition definition)
    {
        ValidateSources(definition.Sources, "artifact source union");

        var expected = definition.Provenance.GameTurnRanges
            .Concat(definition.Provenance.Outcomes)
            .Concat(definition.Provenance.FoulWeatherLocations)
            .Concat(definition.DeferredRules.SelectMany(value => value.Sources))
            .Distinct()
            .OrderBy(value => value.SourceId, StringComparer.Ordinal)
            .ThenBy(value => value.Locator, StringComparer.Ordinal);

        Require(
            definition.Sources.SequenceEqual(expected),
            "The Weather artifact source union does not match its provenance.");
    }

    private static void ValidateSources(
        IReadOnlyList<RuleReference> sources,
        string description)
    {
        Require(
            sources.Count > 0
                && !sources.Any(value => value is null)
                && sources.Distinct().Count() == sources.Count
                && sources.SequenceEqual(sources
                    .OrderBy(value => value.SourceId, StringComparer.Ordinal)
                    .ThenBy(value => value.Locator, StringComparer.Ordinal)),
            $"The {description} sources are noncanonical.");
    }

    private static IEnumerable<int> ValidD66Values()
    {
        for (var tens = 1; tens <= 6; tens++)
        {
            for (var ones = 1; ones <= 6; ones++)
            {
                yield return (tens * 10) + ones;
            }
        }
    }

    private static bool IsD66(int value) =>
        value / 10 is >= 1 and <= 6 && value % 10 is >= 1 and <= 6;

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new JsonException(message);
        }
    }
}
