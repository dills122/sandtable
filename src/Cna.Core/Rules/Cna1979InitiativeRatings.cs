using System.Security.Cryptography;
using System.Text.Json;

namespace Cna.Core.Rules;

public static class Cna1979InitiativeRatings
{
    public const int SchemaVersion = 1;
    public const string ArtifactId = "cna-1979.1.initiative-ratings";

    public static RuleReference RatingConceptSourceReference { get; } = new(
        "spi-1979-land-rules",
        "7.13");

    public static RuleReference RatingChartSourceReference { get; } = new(
        "spi-1979-common-charts",
        "initiative-ratings");

    public static RuleReference HoldingBoxExclusionSourceReference { get; } = new(
        "spi-1979-common-charts",
        "initiative-ratings-note");

    public static IReadOnlyList<CommonwealthInitiativeRating> CommonwealthRows { get; } =
        Array.AsReadOnly<CommonwealthInitiativeRating>(
        [
            new(
                SchemaVersion,
                new GameTurnRange(1, 42),
                3,
                [RatingConceptSourceReference, RatingChartSourceReference]),
            new(
                SchemaVersion,
                new GameTurnRange(43, 90),
                4,
                [RatingConceptSourceReference, RatingChartSourceReference]),
            new(
                SchemaVersion,
                new GameTurnRange(91, 111),
                5,
                [RatingConceptSourceReference, RatingChartSourceReference]),
        ]);

    public static IReadOnlyList<AxisInitiativeRating> AxisRows { get; } =
        Array.AsReadOnly<AxisInitiativeRating>(
        [
            new(
                SchemaVersion,
                AxisInitiativePresence.RommelOnQualifyingGameMap,
                6,
                [
                    RatingConceptSourceReference,
                    RatingChartSourceReference,
                    HoldingBoxExclusionSourceReference,
                ]),
            new(
                SchemaVersion,
                AxisInitiativePresence.GermanLandCombatUnitOnQualifyingGameMap,
                3,
                [
                    RatingConceptSourceReference,
                    RatingChartSourceReference,
                    HoldingBoxExclusionSourceReference,
                ]),
            new(
                SchemaVersion,
                AxisInitiativePresence.NeitherOnQualifyingGameMap,
                1,
                [
                    RatingConceptSourceReference,
                    RatingChartSourceReference,
                    HoldingBoxExclusionSourceReference,
                ]),
        ]);

    public static CommonwealthInitiativeRating GetCommonwealth(int gameTurn)
    {
        if (gameTurn is < 1 or > 111)
        {
            throw new ArgumentOutOfRangeException(nameof(gameTurn));
        }

        return CommonwealthRows.Single(row => row.Turns.Contains(gameTurn));
    }

    public static AxisInitiativeRating GetAxis(AxisInitiativePresence presence)
    {
        if (!Enum.IsDefined(presence))
        {
            throw new ArgumentOutOfRangeException(nameof(presence));
        }

        return AxisRows.Single(row => row.Presence == presence);
    }

    public static AxisInitiativePresence ClassifyAxisPresence(
        AxisInitiativeSourceFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        if (facts.RommelLocation == AxisInitiativeLocation.QualifyingGameMap)
        {
            return AxisInitiativePresence.RommelOnQualifyingGameMap;
        }

        return facts.GermanLandCombatUnitLocations.Contains(
            AxisInitiativeLocation.QualifyingGameMap)
                ? AxisInitiativePresence.GermanLandCombatUnitOnQualifyingGameMap
                : AxisInitiativePresence.NeitherOnQualifyingGameMap;
    }

    public static RulesetArtifact CreateArtifact() => new(
        ArtifactId,
        CalculateContentHash(CommonwealthRows, AxisRows),
        [
            RatingChartSourceReference,
            HoldingBoxExclusionSourceReference,
            RatingConceptSourceReference,
        ]);

    public static string CalculateContentHash(
        IEnumerable<CommonwealthInitiativeRating> commonwealthRows,
        IEnumerable<AxisInitiativeRating> axisRows)
    {
        ArgumentNullException.ThrowIfNull(commonwealthRows);
        ArgumentNullException.ThrowIfNull(axisRows);

        var commonwealthCopy = commonwealthRows.ToArray();
        var axisCopy = axisRows.ToArray();

        if (commonwealthCopy.Length == 0 || commonwealthCopy.Any(row => row is null))
        {
            throw new ArgumentException(
                "At least one non-null Commonwealth rating row is required.",
                nameof(commonwealthRows));
        }

        if (axisCopy.Length == 0 || axisCopy.Any(row => row is null))
        {
            throw new ArgumentException(
                "At least one non-null Axis rating row is required.",
                nameof(axisRows));
        }

        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", SchemaVersion);
            writer.WriteStartArray("commonwealthRows");

            foreach (var row in commonwealthCopy.OrderBy(value => value.Turns.First))
            {
                writer.WriteStartObject();
                writer.WriteNumber("firstTurn", row.Turns.First);
                writer.WriteNumber("lastTurn", row.Turns.Last);
                writer.WriteNumber("rating", row.Rating);
                WriteSources(writer, row.Sources);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteStartArray("axisRows");

            foreach (var row in axisCopy.OrderBy(value => GetPresenceOrder(value.Presence)))
            {
                writer.WriteStartObject();
                writer.WriteString("presence", FormatPresence(row.Presence));
                writer.WriteNumber("rating", row.Rating);
                WriteSources(writer, row.Sources);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return $"sha256:{Convert.ToHexString(SHA256.HashData(stream.ToArray())).ToLowerInvariant()}";
    }

    private static void WriteSources(
        Utf8JsonWriter writer,
        IEnumerable<RuleReference> sources)
    {
        writer.WriteStartArray("sources");

        foreach (var source in sources
            .OrderBy(value => value.SourceId, StringComparer.Ordinal)
            .ThenBy(value => value.Locator, StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", source.SourceId);
            writer.WriteString("locator", source.Locator);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static int GetPresenceOrder(AxisInitiativePresence presence) => presence switch
    {
        AxisInitiativePresence.RommelOnQualifyingGameMap => 0,
        AxisInitiativePresence.GermanLandCombatUnitOnQualifyingGameMap => 1,
        AxisInitiativePresence.NeitherOnQualifyingGameMap => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(presence)),
    };

    private static string FormatPresence(AxisInitiativePresence presence) => presence switch
    {
        AxisInitiativePresence.RommelOnQualifyingGameMap =>
            "rommel-on-qualifying-game-map",
        AxisInitiativePresence.GermanLandCombatUnitOnQualifyingGameMap =>
            "german-land-combat-unit-on-qualifying-game-map",
        AxisInitiativePresence.NeitherOnQualifyingGameMap =>
            "neither-on-qualifying-game-map",
        _ => throw new ArgumentOutOfRangeException(nameof(presence)),
    };
}
