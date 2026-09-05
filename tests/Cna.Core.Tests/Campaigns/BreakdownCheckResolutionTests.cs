using Cna.Core.Campaigns;
using Cna.Core.Randomness;
using Cna.Core.Rules;
namespace Cna.Core.Tests.Campaigns;

public sealed class BreakdownCheckResolutionTests
{
    private const string Campaign = "check-test";
    private static readonly string Rules = Cna1979BreakdownRuleset.Manifest.Hash;
    [Fact]
    public void EligibleCheckUsesActualDiceCursorsAndCreatesPositiveLot()
    {
        var input = Input("a", 12, 71); var state = SandtableRandom.Create(1);
        var first = SandtableRandom.RollD6(state); var second = SandtableRandom.RollD6(first.State);
        var result = Resolve([input], state);
        var check = Assert.Single(result.Checks); var roll = Assert.IsType<CampaignBreakdownRoll>(check.Roll);
        Assert.Equal(CampaignBreakdownCheckStatus.Rolled, check.Status);
        Assert.Equal(first.Value, roll.FirstDie); Assert.Equal(second.Value, roll.SecondDie);
        Assert.Equal(second.State, result.RandomStateAfter);
        Assert.Equal(second.State.NextByteCursor, roll.RandomCursorAfter);
        Assert.Equal(input.WorkingPointCount, check.WorkingAfter + check.BrokenAfter);
        Assert.Equal(22, roll.Coordinate); Assert.Equal(10, roll.PrintedLabel); Assert.Equal(2, roll.LossCount);
        var lot = Assert.Single(result.CreatedLots); Assert.Equal(2, lot.PointCount);
        Assert.Equal("destination", lot.LocationId); Assert.Equal(new RuleReference("spi-1979-land-rules", "21.41"), Assert.Single(lot.Sources));
        lot.ValidateIdentity(Campaign, Rules);
    }
    [Theory]
    [InlineData(0, 0, null, (int)CampaignBreakdownCheckStatus.NoWorkingPoints)]
    [InlineData(12, 3, null, (int)CampaignBreakdownCheckStatus.RawBpNotAboveThree)]
    [InlineData(12, 4, null, (int)CampaignBreakdownCheckStatus.BelowCheckSurface)]
    [InlineData(12, 71, "land.breakdown.band.71-plus", (int)CampaignBreakdownCheckStatus.BandNotHigher)]
    public void NoRollPrecedenceRetainsRngAndMemory(int working, int bp, string? memory, int status)
    {
        var state = SandtableRandom.Create(9); var input = Input("a", working, bp, memory);
        var result = Resolve([input], state); var check = Assert.Single(result.Checks);
        Assert.Equal((CampaignBreakdownCheckStatus)status, check.Status); Assert.Null(check.Roll); Assert.Empty(result.CreatedLots);
        Assert.Equal(state, result.RandomStateAfter); Assert.Equal(memory, check.HighestEffectiveCheckedBandIdAfter);
    }
    [Fact]
    public void EmptyCohortsProduceEmptyUnchangedBatch()
    {
        var state = SandtableRandom.Create(3); var result = Resolve([], state);
        Assert.Empty(result.Checks); Assert.Empty(result.CreatedLots); Assert.Equal(state, result.RandomStateAfter);
    }
    [Fact]
    public void CohortsResolveInCanonicalOrderAndChainRng()
    {
        var result = Resolve([Input("z", 12, 71), Input("a", 12, 71)], SandtableRandom.Create(5));
        Assert.Equal(["a", "z"], result.Checks.Select(check => check.Input.CohortId));
        Assert.Equal(result.Checks[0].Roll!.RandomCursorAfter, result.Checks[1].Roll!.RandomCursorBefore);
        Assert.Equal(result.CreatedLots.OrderBy(lot => lot.LotId, StringComparer.Ordinal), result.CreatedLots);
    }
    [Fact]
    public void InvalidBatchRejectsWithoutMutatingCallerState()
    {
        var inputs = new[] { Input("a", 12, 71), new CampaignBreakdownCheckInput("z", "unknown", Cna1979Breakdown.ProfileTruckId,
            12, new(71, 1), BreakdownPointAmount.Zero, null) };
        var state = SandtableRandom.Create(1);
        Assert.Throws<ArgumentException>(() => Resolve(inputs, state)); Assert.Equal(0UL, state.NextByteCursor);
    }
    [Fact]
    public void RejectedRandomBytesAreIncludedInCursorEvidence()
    {
        var state = SandtableRandom.Create(1);
        while (SandtableRandom.NextByte(state).Value < 252) state = SandtableRandom.NextByte(state).State;
        var result = Resolve([Input("a", 12, 71)], state); var roll = Assert.Single(result.Checks).Roll!;
        Assert.True(roll.RandomCursorAfter - roll.RandomCursorBefore > 2);
        Assert.Equal(state.NextByteCursor, roll.RandomCursorBefore);
        Assert.Equal(SandtableRandom.RollD6(SandtableRandom.RollD6(state).State).State, result.RandomStateAfter);
    }
    [Fact]
    public void ZeroLossEligibleRollStillAdvancesMemory()
    {
        var input = Input("a", 12, 21);
        var state = SandtableRandom.Create(1);
        var result = Resolve([input], state); var check = Assert.Single(result.Checks);
        Assert.Equal(CampaignBreakdownCheckStatus.Rolled, check.Status); Assert.Equal(0, check.Roll!.LossCount);
        Assert.Equal("land.breakdown.band.4-10", check.HighestEffectiveCheckedBandIdAfter);
        Assert.NotEqual(state, result.RandomStateAfter); Assert.Empty(result.CreatedLots);
    }
    [Fact]
    public void SinglePointTenLabelCreatesNoLot()
    {
        var input = Input("a", 1, 21); var state = SandtableRandom.Create(2);
        var result = Resolve([input], state); var check = Assert.Single(result.Checks);
        Assert.Equal(10, check.Roll!.PrintedLabel); Assert.Equal(0, check.Roll.LossCount); Assert.Equal(1, check.WorkingAfter); Assert.Empty(result.CreatedLots);
    }
    [Theory]
    [InlineData(10, "land.breakdown.band.4-10")]
    [InlineData(11, "land.breakdown.band.11-20")]
    public void SandstormHalfBasisControlsEffectiveBand(int attributed, string effective)
    {
        var input = new CampaignBreakdownCheckInput("a", Cna1979Breakdown.VehicleTypeTruckId,
            Cna1979Breakdown.ProfileTruckId, 12, new(22, 1), new(attributed, 1), null);
        Assert.Equal(effective, Assert.Single(Resolve([input], SandtableRandom.Create(1), BreakdownWeatherKind.Sandstorm).Checks).EffectiveBandId);
    }
    [Fact]
    public void RainstormHasNeutralShiftAndExactFractionalRawThreshold()
    {
        var input = Input("a", 12, 21);
        Assert.Equal("land.breakdown.band.4-10", Assert.Single(Resolve([input], SandtableRandom.Create(1), BreakdownWeatherKind.Rainstorm).Checks).EffectiveBandId);
        var fractional = new CampaignBreakdownCheckInput("a", input.VehicleTypeId, input.ProfileId, 12, new(7, 2), BreakdownPointAmount.Zero, null);
        Assert.Equal(CampaignBreakdownCheckStatus.BelowCheckSurface, Assert.Single(Resolve([fractional], SandtableRandom.Create(1)).Checks).Status);
    }
    [Fact]
    public void UnsupportedRandomAlgorithmRejectsEvenWithoutDraws()
    {
        var state = new RandomStreamState(1, "unknown", 1, 0);
        Assert.Throws<ArgumentException>(() => Resolve([], state));
        Assert.Throws<ArgumentException>(() => Resolve([Input("a", 0, 0)], state));
    }
    [Fact]
    public void CheckCodecRoundTripsAndRejectsUnknownDuplicateAndChangedOutcome()
    {
        var check = Assert.Single(Resolve([Input("a", 12, 71)], SandtableRandom.Create(1)).Checks);
        var bytes = CampaignBreakdownCheckCodec.Serialize(check);
        Assert.Equal(check, CampaignBreakdownCheckCodec.Deserialize(bytes));
        var json = System.Text.Encoding.UTF8.GetString(bytes);
        foreach (var altered in new[] { json + " ", json.Insert(1, "\"extra\":1,"), json.Replace("\"status\":", "\"status\":\"rolled\",\"status\":"),
            json.Replace("\"workingAfter\":" + check.WorkingAfter, "\"workingAfter\":99") })
            Assert.Throws<System.Text.Json.JsonException>(() => CampaignBreakdownCheckCodec.Deserialize(System.Text.Encoding.UTF8.GetBytes(altered)));
    }
    private static CampaignBreakdownCheckInput Input(string id, int working, int bp, string? memory = null) =>
        new(id, Cna1979Breakdown.VehicleTypeTruckId, Cna1979Breakdown.ProfileTruckId, working, new(bp, 1), BreakdownPointAmount.Zero, memory);
    private static CampaignBreakdownCheckBatch Resolve(CampaignBreakdownCheckInput[] inputs, RandomStreamState state, BreakdownWeatherKind weather = BreakdownWeatherKind.Normal)
    {
        var route = CampaignBreakdownRoute.Create(Campaign, Rules, 2, "element", "representation", LandSide.Axis,
            "origin", "destination", inputs.Select(input => input.CohortId));
        var stop = CampaignBreakdownStop.Create(Campaign, Rules, 3, route, CampaignBreakdownStopReason.Deliberate, weather, inputs);
        return CampaignBreakdownCheckResolver.Resolve(Campaign, Rules, stop, inputs.ToDictionary(input => input.CohortId, _ => 0), state, 4);
    }
}
