using System.Text;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignBreakdownStepCodec
{
    public static byte[] Serialize(CampaignBreakdownStep step) => CampaignBreakdownCodec.Bytes(writer => Write(writer, step));
    public static CampaignBreakdownStep Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            var step = Parse(document.RootElement);
            if (!bytes.Span.SequenceEqual(Serialize(step))) throw new JsonException("Noncanonical Breakdown step.");
            return step;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException
            or KeyNotFoundException or FormatException or ArithmeticException)
        {
            throw new JsonException("Invalid Breakdown step.", error);
        }
    }
    internal static void Write(Utf8JsonWriter writer, CampaignBreakdownStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        writer.WriteStartObject();
        writer.WriteString("cohortId", step.CohortId);
        writer.WriteString("vehicleTypeId", step.VehicleTypeId);
        writer.WriteString("profileId", step.ProfileId);
        writer.WriteString("destinationTerrainId", step.DestinationTerrainId);
        writer.WriteString("inputRouteId", step.InputRouteId);
        writer.WriteString("effectiveRouteId", step.EffectiveRouteId);
        writer.WriteStartArray("hexsides");
        foreach (var hexside in step.Hexsides)
        {
            writer.WriteStartObject();
            writer.WriteString("hexsideId", hexside.HexsideId);
            writer.WriteString("direction", FormatDirection(hexside.Direction));
            WriteAmount(writer, "addedPoints", hexside.AddedPoints);
            CampaignSnapshotSerializer.WriteSources(writer, hexside.Sources);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteString("weatherKind", CampaignBreakdownCodec.FormatWeather(step.WeatherKind));
        WriteAmount(writer, "before", step.Before); WriteAmount(writer, "delta", step.Delta); WriteAmount(writer, "after", step.After);
        WriteAmount(writer, "sandstormBefore", step.SandstormBefore); WriteAmount(writer, "sandstormDelta", step.SandstormDelta);
        WriteAmount(writer, "sandstormAfter", step.SandstormAfter);
        CampaignSnapshotSerializer.WriteSources(writer, step.Sources);
        writer.WriteEndObject();
    }
    internal static CampaignBreakdownStep Parse(JsonElement value)
    {
        CampaignBreakdownCodec.Require(value, "cohortId", "vehicleTypeId", "profileId", "destinationTerrainId",
            "inputRouteId", "effectiveRouteId", "hexsides", "weatherKind", "before", "delta", "after",
            "sandstormBefore", "sandstormDelta", "sandstormAfter", "sources");
        return new(Text(value, "cohortId"), Text(value, "vehicleTypeId"), Text(value, "profileId"),
            Text(value, "destinationTerrainId"), value.GetProperty("inputRouteId").GetString(),
            value.GetProperty("effectiveRouteId").GetString(), value.GetProperty("hexsides").EnumerateArray().Select(ParseHexside),
            Text(value, "weatherKind") switch
            {
                "normal" => BreakdownWeatherKind.Normal,
                "hot" => BreakdownWeatherKind.Hot,
                "sandstorm" => BreakdownWeatherKind.Sandstorm,
                "rainstorm" => BreakdownWeatherKind.Rainstorm,
                _ => throw new JsonException("Invalid Breakdown weather."),
            }, ParseAmount(value, "before"), ParseAmount(value, "delta"), ParseAmount(value, "after"),
            ParseAmount(value, "sandstormBefore"), ParseAmount(value, "sandstormDelta"), ParseAmount(value, "sandstormAfter"),
            CampaignSnapshotSerializer.ParseSources(value.GetProperty("sources")));
    }
    private static CampaignBreakdownCrossedHexside ParseHexside(JsonElement value)
    {
        CampaignBreakdownCodec.Require(value, "hexsideId", "direction", "addedPoints", "sources");
        return new(Text(value, "hexsideId"), Text(value, "direction") switch
        {
            "either" => BreakdownHexsideDirection.Either,
            "up" => BreakdownHexsideDirection.Up,
            "down" => BreakdownHexsideDirection.Down,
            _ => throw new JsonException("Invalid Breakdown direction."),
        }, ParseAmount(value, "addedPoints"), CampaignSnapshotSerializer.ParseSources(value.GetProperty("sources")));
    }
    private static string Text(JsonElement value, string name) => value.GetProperty(name).GetString()!;
    private static BreakdownPointAmount ParseAmount(JsonElement value, string name) =>
        BreakdownPointAmountCodec.Deserialize(Encoding.UTF8.GetBytes(value.GetProperty(name).GetRawText()));
    private static void WriteAmount(Utf8JsonWriter writer, string name, BreakdownPointAmount amount)
    {
        writer.WritePropertyName(name); BreakdownPointAmountCodec.WriteCanonical(writer, amount);
    }
    internal static string FormatDirection(Cna.Core.Rules.BreakdownHexsideDirection direction) => direction switch
    {
        Cna.Core.Rules.BreakdownHexsideDirection.Either => "either",
        Cna.Core.Rules.BreakdownHexsideDirection.Up => "up",
        Cna.Core.Rules.BreakdownHexsideDirection.Down => "down",
        _ => throw new JsonException("Invalid Breakdown direction."),
    };
}
