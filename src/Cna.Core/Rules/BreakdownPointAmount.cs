using System.Globalization;
using System.Numerics;

namespace Cna.Core.Rules;

public sealed record BreakdownPointAmount : IComparable<BreakdownPointAmount>
{
    public BreakdownPointAmount(long numerator, int denominator)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(numerator);
        ArgumentOutOfRangeException.ThrowIfLessThan(denominator, 1);

        if (numerator == 0)
        {
            Numerator = 0;
            Denominator = 1;
            return;
        }

        var divisor = GreatestCommonDivisor(numerator, denominator);
        Numerator = numerator / divisor;
        Denominator = denominator / checked((int)divisor);
    }

    public static BreakdownPointAmount Zero { get; } = new(0, 1);

    public long Numerator { get; }

    public int Denominator { get; }

    public int CompareTo(BreakdownPointAmount? other)
    {
        var otherAmount = Require(other);
        return ((BigInteger)Numerator * otherAmount.Denominator)
            .CompareTo((BigInteger)otherAmount.Numerator * Denominator);
    }

    public long CeilingToWhole()
    {
        var whole = Numerator / Denominator;
        return Numerator % Denominator == 0 ? whole : checked(whole + 1);
    }

    public static BreakdownPointAmount operator +(
        BreakdownPointAmount left,
        BreakdownPointAmount right) => CreateChecked(
            ((BigInteger)Require(left).Numerator * Require(right).Denominator)
                + ((BigInteger)right.Numerator * left.Denominator),
            (BigInteger)left.Denominator * right.Denominator);

    public static BreakdownPointAmount operator -(
        BreakdownPointAmount left,
        BreakdownPointAmount right)
    {
        Require(left);
        Require(right);
        var numerator = ((BigInteger)left.Numerator * right.Denominator)
            - ((BigInteger)right.Numerator * left.Denominator);
        if (numerator.Sign < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(right),
                "Breakdown Point subtraction cannot produce a negative amount.");
        }

        return CreateChecked(numerator, (BigInteger)left.Denominator * right.Denominator);
    }

    public static bool operator <(
        BreakdownPointAmount left,
        BreakdownPointAmount right) => left.CompareTo(right) < 0;

    public static bool operator <=(
        BreakdownPointAmount left,
        BreakdownPointAmount right) => left.CompareTo(right) <= 0;

    public static bool operator >(
        BreakdownPointAmount left,
        BreakdownPointAmount right) => left.CompareTo(right) > 0;

    public static bool operator >=(
        BreakdownPointAmount left,
        BreakdownPointAmount right) => left.CompareTo(right) >= 0;

    public override string ToString() => string.Create(
        CultureInfo.InvariantCulture,
        $"{Numerator}/{Denominator}");

    private static BreakdownPointAmount CreateChecked(
        BigInteger numerator,
        BigInteger denominator)
    {
        if (numerator.IsZero)
        {
            return Zero;
        }

        var divisor = BigInteger.GreatestCommonDivisor(numerator, denominator);
        numerator /= divisor;
        denominator /= divisor;

        if (numerator > long.MaxValue || denominator > int.MaxValue)
        {
            throw new OverflowException(
                "The exact Breakdown Point result is outside the supported representation.");
        }

        return new BreakdownPointAmount((long)numerator, (int)denominator);
    }

    private static long GreatestCommonDivisor(long left, int right)
    {
        while (right != 0)
        {
            var remainder = left % right;
            left = right;
            right = checked((int)remainder);
        }

        return left;
    }

    private static BreakdownPointAmount Require(BreakdownPointAmount? amount) =>
        amount ?? throw new ArgumentNullException(nameof(amount));
}
