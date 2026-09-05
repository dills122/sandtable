using System.Security.Cryptography;

namespace Cna.Core.Rules;

/// <summary>Dormant Breakdown outcomes; not part of the current campaign ruleset.</summary>
internal static class Cna1979BreakdownAdjudication
{
    public const int SchemaVersion = 2;
    public const string ExactFractionsRulingId = "land.breakdown.ruling.exact-outcome-fractions";

    private static readonly RuleReference ChartSource = new("spi-1979-common-charts", "21.38");
    private static readonly RuleReference FractionSource = new("spi-1979-land-rules", "21.34");
    private static readonly RuleReference RoundingSource = new("spi-1979-land-rules", "21.35");

    public static BreakdownRulesV2ArtifactDefinition Definition { get; } = CreateDefinition();

    public static BreakdownOutcomeCell LookupOutcome(string bandId, int coordinate)
    {
        if (!Cna1979Breakdown.IsSupportedBandId(bandId))
        {
            throw new ArgumentException("Unsupported Breakdown outcome band.", nameof(bandId));
        }

        // Validate both faces; integers between legal coordinates are not dice outcomes.
        _ = Cna1979Breakdown.CreateSequentialDiceCoordinate(coordinate / 10, coordinate % 10);
        return Definition.OutcomeCells.Single(cell =>
            cell.BandId == bandId && cell.Coordinate == coordinate);
    }

    public static BreakdownLossResult CalculateLoss(int workingPointCount, int printedLabel)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(workingPointCount);
        var outcome = Definition.OutcomeFractions.SingleOrDefault(value =>
            value.PrintedLabel == printedLabel)
            ?? throw new ArgumentOutOfRangeException(nameof(printedLabel), printedLabel,
                "Unsupported Breakdown outcome label.");

        var product = checked((long)workingPointCount * outcome.Fraction.Numerator);
        var loss = checked((int)(product / outcome.Fraction.Denominator
            + (product % outcome.Fraction.Denominator == 0 ? 0 : 1)));
        if (workingPointCount == 1 && printedLabel == 10)
        {
            loss = 0;
        }

        return new BreakdownLossResult(workingPointCount, loss, outcome);
    }

    public static BreakdownCheckEligibility EvaluateEligibility(
        int workingPointCount,
        BreakdownPointAmount totalPoints,
        string profileId,
        BreakdownWeatherKind weatherKind,
        BreakdownPointAmount sandstormAttributedPoints,
        string? highestEffectiveCheckedBandId)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(workingPointCount);
        ArgumentNullException.ThrowIfNull(totalPoints);
        ArgumentNullException.ThrowIfNull(sandstormAttributedPoints);
        if (highestEffectiveCheckedBandId is not null
            && !Cna1979Breakdown.IsCheckEligibleBandId(highestEffectiveCheckedBandId))
        {
            throw new ArgumentException("Checked memory must name an eligible Breakdown band.",
                nameof(highestEffectiveCheckedBandId));
        }

        var rawBand = Cna1979Breakdown.LookupAccumulatedBand(totalPoints);
        // Rainstorm transforms route inputs during BP accumulation. At a stop its column
        // contribution is neutral; do not call the predecessor's forbidden Rainstorm shift.
        var effectiveBand = Cna1979Breakdown.SelectEffectiveCheckBand(
            totalPoints, profileId,
            weatherKind == BreakdownWeatherKind.Rainstorm ? BreakdownWeatherKind.Normal : weatherKind,
            sandstormAttributedPoints);
        var status = workingPointCount switch
        {
            0 => BreakdownCheckEligibilityStatus.NoWorkingPoints,
            _ when totalPoints <= new BreakdownPointAmount(3, 1) =>
                BreakdownCheckEligibilityStatus.RawBpNotAboveThree,
            _ when effectiveBand is null => BreakdownCheckEligibilityStatus.BelowCheckSurface,
            _ when highestEffectiveCheckedBandId is not null
                && BandIndex(effectiveBand.BandId) <= BandIndex(highestEffectiveCheckedBandId) =>
                BreakdownCheckEligibilityStatus.BandNotHigher,
            _ => BreakdownCheckEligibilityStatus.Eligible,
        };
        return new BreakdownCheckEligibility(rawBand, effectiveBand, status);
    }

    public static RulesetArtifact CreateArtifact() => new(
        Cna1979Breakdown.ArtifactId,
        $"sha256:{Convert.ToHexStringLower(SHA256.HashData(
            BreakdownRulesV2ArtifactCodec.SerializeCanonical(Definition)))}",
        Definition.Sources);

    // Returned rulings are prepared for the coupled successor manifest, not registered
    // with current Ruleset 8. Sequence 4 and campaign activation belong to later tasks.
    public static IReadOnlyList<Ruling> CreateRulings() => Array.AsReadOnly<Ruling>(
    [
        new(ExactFractionsRulingId, "BRK-DEC-004",
            ["exact-fractions-ceiling-single-point-exception", "literal-thirty-three-percent"],
            "exact-fractions-ceiling-single-point-exception", ["BRK-AC-001", "BRK-AC-004"],
            [FractionSource, RoundingSource]),
        new("land.breakdown.ruling.stop-reaction-precedence", "BRK-DEC-005",
            ["reaction-before-phasing-stop", "breakdown-before-reaction"],
            "reaction-before-phasing-stop", ["BRK-AC-006"],
            [new("spi-1979-land-rules", "21.24-21.26")]),
        new("land.breakdown.ruling.standalone-truck-profile", "BRK-DEC-006",
            ["certified-unladen-truck-battalion", "general-motorized-infantry"],
            "certified-unladen-truck-battalion", ["BRK-AC-007", "BRK-AC-008"],
            [new("spi-1979-land-rules", "21.27-21.29"), new("spi-1979-land-rules", "21.36"),
                new("spi-1979-land-rules", "21.42-21.45")]),
        new("land.breakdown.ruling.persistent-stop-lots", "BRK-DEC-007",
            ["persistent-stop-location-lots", "scalar-broken-count"],
            "persistent-stop-location-lots", ["BRK-AC-007", "BRK-AC-008"],
            [new("spi-1979-land-rules", "21.41-21.45")]),
    ]);

    private static int BandIndex(string bandId) => Cna1979Breakdown.Definition.Bands
        .Select((band, index) => (band.BandId, Index: index))
        .Single(value => value.BandId == bandId).Index;

    private static BreakdownRulesV2ArtifactDefinition CreateDefinition()
    {
        var fractions = Array.AsReadOnly<BreakdownOutcomeFraction>(
        [
            Fraction(0, 0, 1), Fraction(10, 1, 10), Fraction(25, 1, 4),
            Fraction(33, 1, 3), Fraction(50, 1, 2), Fraction(75, 3, 4),
        ]);
        // Common Charts 21.38, source-locked by BRK-RSH-002: inclusive upper bounds
        // over sequential coordinates only. Zero denotes an empty percentage column.
        int[][] bounds =
        [
            [66],
            [42, 64, 65, 66],
            [32, 62, 64, 65, 66],
            [26, 55, 62, 65, 66],
            [23, 53, 61, 64, 66],
            [16, 46, 56, 63, 65, 66],
            [14, 42, 54, 63, 65, 66],
            [0, 33, 52, 62, 64, 66],
            [0, 25, 43, 55, 63, 66],
        ];
        var continuity = Cna1979Breakdown.Definition;
        var cells = Array.AsReadOnly(continuity.Bands.SelectMany((band, index) =>
            continuity.DiceCoordinate.Coordinates.Select(coordinate => new BreakdownOutcomeCell(
                band.BandId, coordinate,
                fractions[Array.FindIndex(bounds[index], upper => coordinate <= upper)].PrintedLabel,
                Array.AsReadOnly<RuleReference>([ChartSource])))).ToArray());
        var sources = Array.AsReadOnly(continuity.Sources
            .Concat(fractions.SelectMany(value => value.Sources))
            .Concat(cells.SelectMany(value => value.Sources))
            .Distinct().OrderBy(value => value.SourceId, StringComparer.Ordinal)
            .ThenBy(value => value.Locator, StringComparer.Ordinal).ToArray());
        return new BreakdownRulesV2ArtifactDefinition(SchemaVersion, continuity, fractions, cells, sources);
    }

    private static BreakdownOutcomeFraction Fraction(int label, int numerator, int denominator) => new(
        label, new BreakdownPointAmount(numerator, denominator), ExactFractionsRulingId,
        Array.AsReadOnly<RuleReference>([ChartSource, FractionSource, RoundingSource]));
}
