using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignOperationStageWeatherCodec
{
    public static void Write(Utf8JsonWriter writer, IReadOnlyList<CampaignOperationStageWeather> values)
    {
        if (!IsStructurallyValid(values)) throw new JsonException("The Weather collection is invalid.");
        writer.WriteStartArray("operationStageWeather");
        foreach (var value in values) WriteValue(writer, value);
        writer.WriteEndArray();
    }

    public static void WriteValue(Utf8JsonWriter writer, CampaignOperationStageWeather value)
    {
        writer.WriteStartObject();
        writer.WriteNumber("contractVersion", value.ContractVersion);
        writer.WriteNumber("gameTurn", value.GameTurn);
        writer.WriteNumber("operationStage", value.OperationStage);
        writer.WriteString("determiningSide", CampaignSnapshotSerializer.FormatSide(value.DeterminingSide));
        writer.WriteString("season", FormatSeason(value.Season));
        writer.WriteNumber("firstDie", value.FirstDie);
        writer.WriteNumber("secondDie", value.SecondDie);
        writer.WriteString("kind", FormatKind(value.Kind));
        writer.WriteString("scope", FormatScope(value.Scope));
        if (value.LocationDie.HasValue) writer.WriteNumber("locationDie", value.LocationDie.Value);
        else writer.WriteNull("locationDie");
        writer.WriteStartArray("affectedAreas");
        foreach (var area in value.AffectedAreas) writer.WriteStringValue(FormatArea(area));
        writer.WriteEndArray();
        writer.WriteNumber("fuelWaterReductionSubjectCount", value.FuelWaterReductionSubjectCount);
        writer.WriteNumber("restoredWellCount", value.RestoredWellCount);
        writer.WriteNumber("damagedGroundedAircraftCount", value.DamagedGroundedAircraftCount);
        writer.WriteEndObject();
    }

    public static CampaignOperationStageWeather[] Parse(JsonElement values)
    {
        try
        {
            var parsed = values.EnumerateArray().Select(ParseValue).ToArray();
            return IsStructurallyValid(parsed) ? parsed : throw new JsonException("The Weather collection is invalid.");
        }
        catch (JsonException) { throw; }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        { throw new JsonException("The Weather collection is invalid.", exception); }
    }

    public static CampaignOperationStageWeather ParseValue(JsonElement value)
    {
        CampaignSnapshotSerializer.RequireProperties(value, "contractVersion", "gameTurn",
            "operationStage", "determiningSide", "season", "firstDie", "secondDie", "kind",
            "scope", "locationDie", "affectedAreas", "fuelWaterReductionSubjectCount",
            "restoredWellCount", "damagedGroundedAircraftCount");
        var location = value.GetProperty("locationDie");
        return new CampaignOperationStageWeather(
            value.GetProperty("contractVersion").GetInt32(), value.GetProperty("gameTurn").GetInt32(),
            value.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParseSide(value.GetProperty("determiningSide").GetString()),
            ParseSeason(value.GetProperty("season").GetString()), value.GetProperty("firstDie").GetInt32(),
            value.GetProperty("secondDie").GetInt32(), ParseKind(value.GetProperty("kind").GetString()),
            ParseScope(value.GetProperty("scope").GetString()),
            location.ValueKind == JsonValueKind.Null ? null : location.GetInt32(),
            value.GetProperty("affectedAreas").EnumerateArray().Select(item => ParseArea(item.GetString())).ToArray(),
            value.GetProperty("fuelWaterReductionSubjectCount").GetInt32(),
            value.GetProperty("restoredWellCount").GetInt32(),
            value.GetProperty("damagedGroundedAircraftCount").GetInt32());
    }

    public static bool IsStructurallyValid(IReadOnlyList<CampaignOperationStageWeather>? values) =>
        values is not null
        && !values.Any(value => value is null)
        && values.Select(value => (value.GameTurn, value.OperationStage)).Distinct().Count() == values.Count
        && values.SequenceEqual(values.OrderBy(value => value.GameTurn).ThenBy(value => value.OperationStage));

    internal static string FormatSeason(WeatherSeason value) => value.ToString().ToLowerInvariant();
    internal static string FormatKind(WeatherKind value) => value.ToString().ToLowerInvariant();
    internal static string FormatScope(WeatherScope value) => value switch
    {
        WeatherScope.None => "none",
        WeatherScope.Global => "global",
        WeatherScope.ListedAreas => "listed-areas",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };
    internal static string FormatArea(WeatherArea value) => value.ToString().ToLowerInvariant();
    internal static WeatherSeason ParseSeason(string? value) => Enum.TryParse<WeatherSeason>(value, true, out var parsed)
        && Enum.IsDefined(parsed) ? parsed : throw new JsonException($"Unknown Weather season '{value}'.");
    internal static WeatherKind ParseKind(string? value) => Enum.TryParse<WeatherKind>(value, true, out var parsed)
        && Enum.IsDefined(parsed) ? parsed : throw new JsonException($"Unknown Weather kind '{value}'.");
    internal static WeatherScope ParseScope(string? value) => value switch
    {
        "none" => WeatherScope.None,
        "global" => WeatherScope.Global,
        "listed-areas" => WeatherScope.ListedAreas,
        _ => throw new JsonException($"Unknown Weather scope '{value}'."),
    };
    internal static WeatherArea ParseArea(string? value) => Enum.TryParse<WeatherArea>(value, true, out var parsed)
        && Enum.IsDefined(parsed) ? parsed : throw new JsonException($"Unknown Weather area '{value}'.");
}
