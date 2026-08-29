using System.Text;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class BreakdownPointAmountTests
{
    [Fact]
    public void EquivalentInputsNormalizeToOneExactValue()
    {
        var half = new BreakdownPointAmount(1, 2);
        var equivalent = new BreakdownPointAmount(2, 4);

        Assert.Equal(1, equivalent.Numerator);
        Assert.Equal(2, equivalent.Denominator);
        Assert.Equal(half, equivalent);
        Assert.Equal(half.GetHashCode(), equivalent.GetHashCode());
        Assert.Equal(0, half.CompareTo(equivalent));
        Assert.Equal(new BreakdownPointAmount(0, 1), BreakdownPointAmount.Zero);
    }

    [Fact]
    public void ArithmeticOrderingAndCeilingRemainExact()
    {
        var half = new BreakdownPointAmount(1, 2);
        var twentyAndAHalf = new BreakdownPointAmount(41, 2);

        Assert.Equal(new BreakdownPointAmount(21, 1), half + twentyAndAHalf);
        Assert.Equal(new BreakdownPointAmount(20, 1), twentyAndAHalf - half);
        Assert.True(half < twentyAndAHalf);
        Assert.True(twentyAndAHalf > half);
        Assert.Equal(21, twentyAndAHalf.CeilingToWhole());
        Assert.Equal(20, new BreakdownPointAmount(20, 1).CeilingToWhole());
        Assert.Equal(0, BreakdownPointAmount.Zero.CeilingToWhole());
    }

    [Fact]
    public void InvalidOrUnrepresentableAmountsFailClosed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BreakdownPointAmount(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BreakdownPointAmount(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new BreakdownPointAmount(1, -2));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => BreakdownPointAmount.Zero - new BreakdownPointAmount(1, 2));
        Assert.Throws<OverflowException>(
            () => new BreakdownPointAmount(long.MaxValue, 1)
                + new BreakdownPointAmount(1, 1));
    }

    [Fact]
    public void CanonicalCodecUsesReducedFixedOrderJson()
    {
        var canonical = BreakdownPointAmountCodec.SerializeCanonical(
            new BreakdownPointAmount(2, 4));
        var parsed = BreakdownPointAmountCodec.Deserialize(canonical);

        Assert.Equal("{\"numerator\":1,\"denominator\":2}",
            Encoding.UTF8.GetString(canonical));
        Assert.Equal(new BreakdownPointAmount(1, 2), parsed);
        Assert.Equal(canonical, BreakdownPointAmountCodec.SerializeCanonical(parsed));
    }

    [Theory]
    [InlineData("{\"denominator\":2,\"numerator\":1}")]
    [InlineData("{\"numerator\":2,\"denominator\":4}")]
    [InlineData("{\"numerator\":0,\"denominator\":2}")]
    [InlineData("{\"numerator\":1,\"denominator\":0}")]
    [InlineData("{\"numerator\":-1,\"denominator\":2}")]
    [InlineData("{\"numerator\":1,\"denominator\":2}\n")]
    public void CanonicalCodecRejectsNoncanonicalOrInvalidJson(string json)
    {
        Assert.Throws<JsonException>(() => BreakdownPointAmountCodec.Deserialize(
            Encoding.UTF8.GetBytes(json)));
    }
}
