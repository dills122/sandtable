using System.Text;
using System.Text.Json;
using Cna.Core.Rules;
namespace Cna.Core.Campaigns;

internal static class CampaignBreakdownCheckCodec
{
    public static byte[] Serialize(CampaignBreakdownCheck check) => CampaignBreakdownCodec.Bytes(writer => Write(writer, check));
    public static CampaignBreakdownCheck Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes); var check = Parse(document.RootElement);
            if (!bytes.Span.SequenceEqual(Serialize(check))) throw new JsonException("Noncanonical Breakdown check.");
            return check;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or ArithmeticException)
        { throw new JsonException("Invalid Breakdown check.", error); }
    }
    internal static void Write(Utf8JsonWriter writer, CampaignBreakdownCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        writer.WriteStartObject(); writer.WriteString("checkId", check.CheckId);
        writer.WritePropertyName("input"); CampaignBreakdownCodec.WriteInput(writer, check.Input);
        writer.WriteString("rawBandId", check.RawBandId); writer.WriteString("effectiveBandId", check.EffectiveBandId);
        writer.WriteString("status", FormatStatus(check.Status)); writer.WritePropertyName("roll");
        if (check.Roll is null) writer.WriteNullValue(); else WriteRoll(writer, check.Roll);
        writer.WriteNumber("workingAfter", check.WorkingAfter); writer.WriteNumber("brokenBefore", check.BrokenBefore);
        writer.WriteNumber("brokenAfter", check.BrokenAfter);
        writer.WriteString("highestEffectiveCheckedBandIdAfter", check.HighestEffectiveCheckedBandIdAfter);
        CampaignSnapshotSerializer.WriteSources(writer, check.Sources); writer.WriteEndObject();
    }
    internal static CampaignBreakdownCheck Parse(JsonElement value)
    {
        CampaignBreakdownCodec.Require(value, "checkId", "input", "rawBandId", "effectiveBandId", "status", "roll",
            "workingAfter", "brokenBefore", "brokenAfter", "highestEffectiveCheckedBandIdAfter", "sources");
        return new(Text(value, "checkId"), CampaignBreakdownCodec.ParseInput(value.GetProperty("input")), Text(value, "rawBandId"),
            value.GetProperty("effectiveBandId").GetString(), ParseStatus(Text(value, "status")),
            value.GetProperty("roll").ValueKind == JsonValueKind.Null ? null : ParseRoll(value.GetProperty("roll")),
            value.GetProperty("workingAfter").GetInt32(), value.GetProperty("brokenBefore").GetInt32(),
            value.GetProperty("brokenAfter").GetInt32(), value.GetProperty("highestEffectiveCheckedBandIdAfter").GetString(),
            CampaignSnapshotSerializer.ParseSources(value.GetProperty("sources")));
    }
    private static void WriteRoll(Utf8JsonWriter writer, CampaignBreakdownRoll roll)
    {
        writer.WriteStartObject(); writer.WriteNumber("firstDie", roll.FirstDie); writer.WriteNumber("secondDie", roll.SecondDie);
        writer.WriteNumber("coordinate", roll.Coordinate); writer.WriteNumber("randomCursorBefore", roll.RandomCursorBefore);
        writer.WriteNumber("randomCursorAfter", roll.RandomCursorAfter); writer.WriteNumber("printedLabel", roll.PrintedLabel);
        writer.WritePropertyName("fraction"); BreakdownPointAmountCodec.WriteCanonical(writer, roll.Fraction);
        writer.WriteString("rulingId", roll.RulingId); writer.WriteNumber("lossCount", roll.LossCount); writer.WriteEndObject();
    }
    private static CampaignBreakdownRoll ParseRoll(JsonElement value)
    {
        CampaignBreakdownCodec.Require(value, "firstDie", "secondDie", "coordinate", "randomCursorBefore", "randomCursorAfter",
            "printedLabel", "fraction", "rulingId", "lossCount");
        return new(value.GetProperty("firstDie").GetInt32(), value.GetProperty("secondDie").GetInt32(),
            value.GetProperty("coordinate").GetInt32(), value.GetProperty("randomCursorBefore").GetUInt64(),
            value.GetProperty("randomCursorAfter").GetUInt64(), value.GetProperty("printedLabel").GetInt32(),
            BreakdownPointAmountCodec.Deserialize(Encoding.UTF8.GetBytes(value.GetProperty("fraction").GetRawText())),
            Text(value, "rulingId"), value.GetProperty("lossCount").GetInt32());
    }
    private static string Text(JsonElement value, string name) => value.GetProperty(name).GetString()!;
    private static string FormatStatus(CampaignBreakdownCheckStatus status) => status switch
    {
        CampaignBreakdownCheckStatus.NoWorkingPoints => "no-working-points",
        CampaignBreakdownCheckStatus.RawBpNotAboveThree => "raw-bp-not-above-three",
        CampaignBreakdownCheckStatus.BelowCheckSurface => "below-check-surface",
        CampaignBreakdownCheckStatus.BandNotHigher => "band-not-higher",
        CampaignBreakdownCheckStatus.Rolled => "rolled",
        _ => throw new JsonException("Invalid Breakdown check status."),
    };
    private static CampaignBreakdownCheckStatus ParseStatus(string status) => status switch
    {
        "no-working-points" => CampaignBreakdownCheckStatus.NoWorkingPoints,
        "raw-bp-not-above-three" => CampaignBreakdownCheckStatus.RawBpNotAboveThree,
        "below-check-surface" => CampaignBreakdownCheckStatus.BelowCheckSurface,
        "band-not-higher" => CampaignBreakdownCheckStatus.BandNotHigher,
        "rolled" => CampaignBreakdownCheckStatus.Rolled,
        _ => throw new JsonException("Invalid Breakdown check status."),
    };
}
