using System.Text;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Tests.Rules;

public sealed class CapabilityPointAmountTests
{
    [Fact]
    public void EquivalentInputsNormalizeToOneExactValue()
    {
        var half = new CapabilityPointAmount(1, 2);
        var equivalent = new CapabilityPointAmount(2, 4);

        Assert.Equal(1, equivalent.Numerator);
        Assert.Equal(2, equivalent.Denominator);
        Assert.Equal(half, equivalent);
        Assert.Equal(half.GetHashCode(), equivalent.GetHashCode());
        Assert.Equal(0, half.CompareTo(equivalent));
        Assert.Equal(new CapabilityPointAmount(0, 1), CapabilityPointAmount.Zero);
    }

    [Fact]
    public void ArithmeticAndOrderingRemainExact()
    {
        var half = new CapabilityPointAmount(1, 2);
        var threeHalves = new CapabilityPointAmount(3, 2);

        Assert.Equal(new CapabilityPointAmount(2, 1), half + threeHalves);
        Assert.Equal(new CapabilityPointAmount(1, 1), threeHalves - half);
        Assert.True(half < threeHalves);
        Assert.True(threeHalves > half);
    }

    [Fact]
    public void InvalidOrUnrepresentableAmountsFailClosed()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CapabilityPointAmount(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CapabilityPointAmount(1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CapabilityPointAmount(1, -2));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CapabilityPointAmount.Zero - new CapabilityPointAmount(1, 2));
        Assert.Throws<OverflowException>(
            () => new CapabilityPointAmount(long.MaxValue, 1) +
                new CapabilityPointAmount(1, 1));
    }

    [Fact]
    public void CanonicalCodecUsesReducedFixedOrderJson()
    {
        var canonical = CapabilityPointAmountCodec.SerializeCanonical(
            new CapabilityPointAmount(2, 4));
        var parsed = CapabilityPointAmountCodec.Deserialize(canonical);

        Assert.Equal("{\"numerator\":1,\"denominator\":2}", Encoding.UTF8.GetString(canonical));
        Assert.Equal(new CapabilityPointAmount(1, 2), parsed);
        Assert.Equal(canonical, CapabilityPointAmountCodec.SerializeCanonical(parsed));
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
        Assert.Throws<JsonException>(() => CapabilityPointAmountCodec.Deserialize(
            Encoding.UTF8.GetBytes(json)));
    }
}
