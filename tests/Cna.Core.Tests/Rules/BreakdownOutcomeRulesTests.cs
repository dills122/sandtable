using System.Numerics;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class BreakdownOutcomeRulesTests
{
    [Theory]
    [InlineData(30, 10, 3)]
    [InlineData(20, 33, 7)]
    [InlineData(100, 33, 34)]
    [InlineData(1, 10, 0)]
    [InlineData(2, 10, 1)]
    [InlineData(1, 25, 1)]
    [InlineData(1, 33, 1)]
    [InlineData(1, 50, 1)]
    [InlineData(1, 75, 1)]
    [InlineData(0, 75, 0)]
    [InlineData(int.MaxValue, 75, 1610612736)]
    [InlineData(int.MaxValue, 33, 715827883)]
    public void AcceptedFractionsUseWorkingCountCeilingAndSinglePointException(
        int working, int label, int expectedLoss)
    {
        var result = Cna1979BreakdownAdjudication.CalculateLoss(working, label);

        Assert.Equal(working, result.WorkingPointCount);
        Assert.Equal(label, result.Outcome.PrintedLabel);
        Assert.Equal(expectedLoss, result.LossCount);
    }

    [Fact]
    public void SequentialCoordinateAndPrintedOneThirdRemainDistinctFromSummedDice()
    {
        var cell = Cna1979BreakdownAdjudication.LookupOutcome("land.breakdown.band.71-plus", 33);

        Assert.Equal(25, cell.PrintedLabel);
        var loss = Cna1979BreakdownAdjudication.CalculateLoss(100, 33);
        Assert.Equal(new BreakdownPointAmount(1, 3), loss.Outcome.Fraction);
    }

    [Theory]
    [InlineData(0, 3, null, "NoWorkingPoints")]
    [InlineData(1, 3, null, "RawBpNotAboveThree")]
    [InlineData(1, 4, null, "BelowCheckSurface")]
    [InlineData(1, 21, null, "Eligible")]
    [InlineData(1, 21, "land.breakdown.band.4-10", "BandNotHigher")]
    public void EligibilityRequiresWorkingPointsRawThresholdAndHigherEffectiveBand(
        int working, int total, string? previous, string expected)
    {
        var result = Cna1979BreakdownAdjudication.EvaluateEligibility(
            working, new BreakdownPointAmount(total, 1), Cna1979Breakdown.ProfileTruckId,
            BreakdownWeatherKind.Normal, BreakdownPointAmount.Zero, previous);

        Assert.Equal(expected, result.Status.ToString());
    }

    [Fact]
    public void EveryPublishedCoordinateMatchesTheSourceLockedExpandedTable()
    {
        // Expanded BRK-RSH-002 transcription, rather than the production range algorithm.
        int[][] expected =
        [
            [ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 ],
            [ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 25, 33 ],
            [ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 25, 25, 33, 50 ],
            [ 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 25, 25, 25, 33, 33, 33, 50 ],
            [ 0, 0, 0, 0, 0, 0, 0, 0, 0, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 25, 25, 25, 25, 33, 33, 33, 50, 50 ],
            [ 0, 0, 0, 0, 0, 0, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 25, 25, 25, 25, 25, 25, 33, 33, 33, 50, 50, 75 ],
            [ 0, 0, 0, 0, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 25, 25, 25, 25, 25, 25, 25, 25, 33, 33, 33, 33, 33, 50, 50, 75 ],
            [ 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 33, 33, 33, 33, 33, 33, 50, 50, 75, 75 ],
            [ 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 25, 25, 25, 25, 25, 25, 25, 25, 25, 25, 33, 33, 33, 33, 33, 33, 33, 33, 50, 50, 50, 50, 75, 75, 75 ],
        ];
        string[] bands = ["0-3", "4-10", "11-20", "21-30", "31-40", "41-50", "51-60", "61-70", "71-plus"];
        var count = 0;
        for (var band = 0; band < bands.Length; band++)
        {
            for (var index = 0; index < 36; index++)
            {
                var coordinate = (index / 6 + 1) * 10 + index % 6 + 1;
                var cell = Cna1979BreakdownAdjudication.LookupOutcome("land.breakdown.band." + bands[band], coordinate);
                Assert.Equal(expected[band][index], cell.PrintedLabel);
                Assert.Equal(coordinate, cell.Coordinate);
                Assert.Equal("land.breakdown.band." + bands[band], cell.BandId);
                Assert.Equal(new RuleReference("spi-1979-common-charts", "21.38"), Assert.Single(cell.Sources));
                count++;
            }
        }
        Assert.Equal(324, count);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(-11)]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(10)]
    [InlineData(17)]
    [InlineData(20)]
    [InlineData(60)]
    [InlineData(67)]
    [InlineData(71)]
    [InlineData(111)]
    [InlineData(int.MaxValue)]
    public void NonDiceCoordinatesReject(int coordinate) => Assert.Throws<ArgumentOutOfRangeException>(() =>
        Cna1979BreakdownAdjudication.LookupOutcome("land.breakdown.band.4-10", coordinate));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("4-10")]
    [InlineData("land.breakdown.band.unknown")]
    public void UnknownBandsReject(string? band) => Assert.Throws<ArgumentException>(() =>
        Cna1979BreakdownAdjudication.LookupOutcome(band!, 11));

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    [InlineData(34)]
    [InlineData(100)]
    [InlineData(int.MaxValue)]
    public void UnprintedLabelsRejectEvenForZeroWorkingPoints(int label) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Cna1979BreakdownAdjudication.CalculateLoss(0, label));

    [Fact]
    public void NegativeWorkingCountsReject() => Assert.Throws<ArgumentOutOfRangeException>(() =>
        Cna1979BreakdownAdjudication.CalculateLoss(-1, 33));

    [Fact]
    public void AllFractionsHaveExactCeilingBoundsWithoutIntegerOverflow()
    {
        (int Label, int Numerator, int Denominator)[] fractions =
            [(0, 0, 1), (10, 1, 10), (25, 1, 4), (33, 1, 3), (50, 1, 2), (75, 3, 4)];
        var counts = Enumerable.Range(0, 101).Concat([int.MaxValue - 1, int.MaxValue]);
        foreach (var working in counts)
        {
            foreach (var (label, numerator, denominator) in fractions)
            {
                var result = Cna1979BreakdownAdjudication.CalculateLoss(working, label);
                Assert.Equal(new BreakdownPointAmount(numerator, denominator), result.Outcome.Fraction);
                Assert.InRange(result.LossCount, 0, working);
                if (working == 1 && label == 10)
                {
                    Assert.Equal(0, result.LossCount);
                    continue;
                }
                var exactNumerator = (BigInteger)working * numerator;
                Assert.True((BigInteger)result.LossCount * denominator >= exactNumerator);
                Assert.True((BigInteger)(result.LossCount - 1) * denominator < exactNumerator);
            }
        }
    }

    [Theory]
    [InlineData(BreakdownWeatherKind.Normal, 0, 1, "4-10")]
    [InlineData(BreakdownWeatherKind.Rainstorm, 0, 1, "4-10")]
    [InlineData(BreakdownWeatherKind.Hot, 0, 1, "11-20")]
    [InlineData(BreakdownWeatherKind.Sandstorm, 10499, 1000, "4-10")]
    [InlineData(BreakdownWeatherKind.Sandstorm, 21, 2, "11-20")]
    [InlineData(BreakdownWeatherKind.Sandstorm, 21, 1, "11-20")]
    public void EffectiveBandUsesAcceptedWeatherAndExactSandstormShare(
        BreakdownWeatherKind weather, int numerator, int denominator, string expected)
    {
        var result = Cna1979BreakdownAdjudication.EvaluateEligibility(
            5, new BreakdownPointAmount(21, 1), Cna1979Breakdown.ProfileTruckId, weather,
            new BreakdownPointAmount(numerator, denominator), null);

        Assert.Equal("land.breakdown.band.21-30", result.RawBand.BandId);
        Assert.Equal("land.breakdown.band." + expected, result.EffectiveBand?.BandId);
        Assert.Equal(BreakdownCheckEligibilityStatus.Eligible, result.Status);
    }

    [Fact]
    public void FractionalBandBoundaryUsesCeilingAndLaterCheckRequiresHigherEffectiveBand()
    {
        var previous = "land.breakdown.band.4-10";
        var atBoundary = Eligibility(new BreakdownPointAmount(30, 1), previous);
        var aboveBoundary = Eligibility(new BreakdownPointAmount(30001, 1000), previous);
        Assert.Equal(BreakdownCheckEligibilityStatus.BandNotHigher, atBoundary.Status);
        Assert.Equal(BreakdownCheckEligibilityStatus.Eligible, aboveBoundary.Status);
        Assert.Equal("land.breakdown.band.11-20", aboveBoundary.EffectiveBand?.BandId);
        Assert.Equal(BreakdownCheckEligibilityStatus.BandNotHigher,
            Eligibility(new BreakdownPointAmount(900, 1), "land.breakdown.band.71-plus").Status);
    }

    [Fact]
    public void RawThresholdIsExactAndNoWorkingPointsTakePrecedence()
    {
        var exact = Eligibility(new BreakdownPointAmount(3, 1), null);
        var fractional = Eligibility(new BreakdownPointAmount(30001, 10000), null);
        Assert.Equal(BreakdownCheckEligibilityStatus.RawBpNotAboveThree, exact.Status);
        Assert.Equal(BreakdownCheckEligibilityStatus.BelowCheckSurface, fractional.Status);
        Assert.Equal("land.breakdown.band.4-10", fractional.RawBand.BandId);
        Assert.Null(fractional.EffectiveBand);
        var noWorking = Cna1979BreakdownAdjudication.EvaluateEligibility(
            0, new BreakdownPointAmount(900, 1), Cna1979Breakdown.ProfileTruckId,
            BreakdownWeatherKind.Hot, BreakdownPointAmount.Zero, "land.breakdown.band.71-plus");
        Assert.Equal(BreakdownCheckEligibilityStatus.NoWorkingPoints, noWorking.Status);
    }

    [Fact]
    public void AccumulatingWithinCappedRawBandDoesNotCreateAnotherCheck()
    {
        var first = Eligibility(new BreakdownPointAmount(71, 1), null);
        var later = Eligibility(new BreakdownPointAmount(long.MaxValue, 1), first.EffectiveBand!.BandId);
        Assert.Equal("land.breakdown.band.51-60", first.EffectiveBand.BandId);
        Assert.Equal(first.RawBand, later.RawBand);
        Assert.Equal(first.EffectiveBand, later.EffectiveBand);
        Assert.Equal(BreakdownCheckEligibilityStatus.BandNotHigher, later.Status);
    }

    [Fact]
    public void InvalidInputsCannotBeHiddenByNoRollConditions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Cna1979BreakdownAdjudication.EvaluateEligibility(
            -1, BreakdownPointAmount.Zero, Cna1979Breakdown.ProfileTruckId,
            BreakdownWeatherKind.Normal, BreakdownPointAmount.Zero, null));
        Assert.Throws<ArgumentException>(() => Cna1979BreakdownAdjudication.EvaluateEligibility(
            0, BreakdownPointAmount.Zero, "unknown", BreakdownWeatherKind.Normal, BreakdownPointAmount.Zero, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cna1979BreakdownAdjudication.EvaluateEligibility(
            0, BreakdownPointAmount.Zero, Cna1979Breakdown.ProfileTruckId,
            (BreakdownWeatherKind)99, BreakdownPointAmount.Zero, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => Cna1979BreakdownAdjudication.EvaluateEligibility(
            0, BreakdownPointAmount.Zero, Cna1979Breakdown.ProfileTruckId,
            BreakdownWeatherKind.Rainstorm, new BreakdownPointAmount(1, 1), null));
        Assert.Throws<ArgumentException>(() => Eligibility(BreakdownPointAmount.Zero, "land.breakdown.band.0-3"));
        Assert.Throws<ArgumentException>(() => Eligibility(BreakdownPointAmount.Zero, "unknown"));
        Assert.Throws<ArgumentNullException>(() => Eligibility(null!, null));
        Assert.Throws<ArgumentNullException>(() => Cna1979BreakdownAdjudication.EvaluateEligibility(
            0, BreakdownPointAmount.Zero, Cna1979Breakdown.ProfileTruckId,
            BreakdownWeatherKind.Normal, null!, null));
    }

    private static BreakdownCheckEligibility Eligibility(BreakdownPointAmount total, string? previous) =>
        Cna1979BreakdownAdjudication.EvaluateEligibility(1, total, Cna1979Breakdown.ProfileTruckId,
            BreakdownWeatherKind.Normal, BreakdownPointAmount.Zero, previous);
}
