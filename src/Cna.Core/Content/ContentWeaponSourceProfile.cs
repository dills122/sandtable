using System.Globalization;

namespace Cna.Core.Content;

public enum ContentPrintedRatingKind
{
    Unknown = 0,
    Number = 1,
    Parenthesized = 2,
    Dash = 3,
}

// The source uses a dash for "not applicable or zero". Do not collapse it to 0.
public sealed record ContentPrintedRating
{
    public ContentPrintedRating(ContentPrintedRatingKind kind, int? value)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        if ((kind is ContentPrintedRatingKind.Number or ContentPrintedRatingKind.Parenthesized) != value.HasValue)
            throw new ArgumentException("Numeric source markers require a value; unknown and dash do not.", nameof(value));
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
        Kind = kind;
        Value = value;
    }

    public ContentPrintedRatingKind Kind { get; }
    public int? Value { get; }

    public static ContentPrintedRating Number(int value) => new(ContentPrintedRatingKind.Number, value);
    public static ContentPrintedRating Parenthesized(int value) => new(ContentPrintedRatingKind.Parenthesized, value);
    public static ContentPrintedRating Dash { get; } = new(ContentPrintedRatingKind.Dash, null);
    public static ContentPrintedRating Unknown { get; } = new(ContentPrintedRatingKind.Unknown, null);
}

// Printed weapon rows, not a rules implementation or an admitted combat profile.
public sealed record ContentWeaponSourceProfile
{
    public ContentWeaponSourceProfile(ContentPrintedRating capabilityPoints, ContentPrintedRating antiAir,
        ContentPrintedRating antiArmor, ContentPrintedRating vulnerability, ContentPrintedRating armorProtection,
        ContentPrintedRating fuelRate, string breakdownMarker)
    {
        ArgumentNullException.ThrowIfNull(capabilityPoints);
        ArgumentNullException.ThrowIfNull(antiAir);
        ArgumentNullException.ThrowIfNull(antiArmor);
        ArgumentNullException.ThrowIfNull(vulnerability);
        ArgumentNullException.ThrowIfNull(armorProtection);
        ArgumentNullException.ThrowIfNull(fuelRate);
        if (!IsBreakdownMarker(breakdownMarker))
            throw new ArgumentException("Unsupported printed breakdown notation.", nameof(breakdownMarker));
        CapabilityPoints = capabilityPoints;
        AntiAir = antiAir;
        AntiArmor = antiArmor;
        Vulnerability = vulnerability;
        ArmorProtection = armorProtection;
        FuelRate = fuelRate;
        BreakdownMarker = breakdownMarker;
    }

    public ContentPrintedRating CapabilityPoints { get; }
    public ContentPrintedRating AntiAir { get; }
    public ContentPrintedRating AntiArmor { get; }
    public ContentPrintedRating Vulnerability { get; }
    public ContentPrintedRating ArmorProtection { get; }
    public ContentPrintedRating FuelRate { get; }
    public string BreakdownMarker { get; }

    private static bool IsBreakdownMarker(string? value)
    {
        if (value is "?" or "-" or "0") return true;
        return value is { Length: >= 2 and <= 11 }
            && value[0] is >= '1' and <= '9'
            && value[^1] is 'L' or 'R'
            && int.TryParse(value.AsSpan(0, value.Length - 1), NumberStyles.None, CultureInfo.InvariantCulture, out var shift)
            && shift > 0;
    }
}
