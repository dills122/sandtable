using System.Text.Json;

namespace Cna.Core.Rules;

internal static class BreakdownRulesArtifactCodec
{
    public static byte[] SerializeCanonical(BreakdownRulesArtifactDefinition definition)
    {
        Validate(definition);
        return SerializeUnchecked(definition);
    }

    public static BreakdownRulesArtifactDefinition Deserialize(ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            using var document = JsonDocument.Parse(
                utf8Json.ToArray(),
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 32,
                });
            var root = ReadObject(
                document.RootElement,
                "schemaVersion",
                "bands",
                "profiles",
                "vehicleTypes",
                "weatherShifts",
                "weatherInputTransformations",
                "terrain",
                "routes",
                "hexsides",
                "diceCoordinate",
                "sources");
            var definition = new BreakdownRulesArtifactDefinition(
                ReadInteger(root["schemaVersion"]),
                Copy(ReadArray(root["bands"]).Select(ParseBand)),
                Copy(ReadArray(root["profiles"]).Select(ParseProfile)),
                Copy(ReadArray(root["vehicleTypes"]).Select(ParseVehicleType)),
                Copy(ReadArray(root["weatherShifts"]).Select(ParseWeatherShift)),
                Copy(ReadArray(root["weatherInputTransformations"])
                    .Select(ParseWeatherInputTransformation)),
                Copy(ReadArray(root["terrain"]).Select(ParseTerrain)),
                Copy(ReadArray(root["routes"]).Select(ParseRoute)),
                Copy(ReadArray(root["hexsides"]).Select(ParseHexside)),
                ParseDiceCoordinate(root["diceCoordinate"]),
                Copy(ReadArray(root["sources"]).Select(ParseSource)));
            Validate(definition);

            if (!utf8Json.SequenceEqual(SerializeUnchecked(definition)))
            {
                throw new JsonException("The Breakdown rules artifact is not canonical JSON.");
            }

            return definition;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException
                or FormatException
                or OverflowException)
        {
            throw new JsonException("The Breakdown rules artifact is invalid.", exception);
        }
    }

    internal static void Validate(BreakdownRulesArtifactDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (!SerializeUnchecked(definition).AsSpan().SequenceEqual(
                SerializeUnchecked(Cna1979Breakdown.Definition)))
        {
            throw new JsonException("The Breakdown rules artifact authority is unsupported.");
        }
    }

    private static byte[] SerializeUnchecked(BreakdownRulesArtifactDefinition definition)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", definition.SchemaVersion);
            writer.WriteStartArray("bands");
            foreach (var value in definition.Bands)
            {
                writer.WriteStartObject();
                writer.WriteString("bandId", value.BandId);
                writer.WriteNumber("minimumWholePoints", value.MinimumWholePoints);
                if (value.MaximumWholePoints is int maximum)
                {
                    writer.WriteNumber("maximumWholePoints", maximum);
                }
                else
                {
                    writer.WriteNull("maximumWholePoints");
                }
                writer.WriteBoolean("isCheckEligible", value.IsCheckEligible);
                WriteSources(writer, value.Sources);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("profiles");
            foreach (var value in definition.Profiles)
            {
                writer.WriteStartObject();
                writer.WriteString("profileId", value.ProfileId);
                writer.WriteNumber("columnShift", value.ColumnShift);
                WriteSources(writer, value.Sources);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("vehicleTypes");
            foreach (var value in definition.VehicleTypes)
            {
                writer.WriteStartObject();
                writer.WriteString("vehicleTypeId", value.VehicleTypeId);
                writer.WriteString("profileId", value.ProfileId);
                WriteSources(writer, value.Sources);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("weatherShifts");
            foreach (var value in definition.WeatherShifts)
            {
                writer.WriteStartObject();
                writer.WriteString("weatherKind", FormatWeather(value.WeatherKind));
                writer.WriteNumber("columnShift", value.ColumnShift);
                writer.WriteString("condition", FormatCondition(value.Condition));
                WriteSources(writer, value.Sources);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("weatherInputTransformations");
            foreach (var value in definition.WeatherInputTransformations)
            {
                writer.WriteStartObject();
                writer.WriteString("weatherKind", FormatWeather(value.WeatherKind));
                writer.WriteString("inputRouteId", value.InputRouteId);
                writer.WriteString("treatedAsRouteId", value.TreatedAsRouteId);
                WriteSources(writer, value.Sources);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("terrain");
            foreach (var value in definition.Terrain)
            {
                writer.WriteStartObject();
                writer.WriteString("terrainId", value.TerrainId);
                writer.WritePropertyName("points");
                BreakdownPointAmountCodec.WriteCanonical(writer, value.Points);
                WriteSources(writer, value.Sources);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("routes");
            foreach (var value in definition.Routes)
            {
                writer.WriteStartObject();
                writer.WriteString("routeId", value.RouteId);
                writer.WriteString("operation", FormatOperation(value.Operation));
                writer.WritePropertyName("amount");
                BreakdownPointAmountCodec.WriteCanonical(writer, value.Amount);
                WriteSources(writer, value.Sources);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("hexsides");
            foreach (var value in definition.Hexsides)
            {
                writer.WriteStartObject();
                writer.WriteString("hexsideId", value.HexsideId);
                writer.WriteString("direction", FormatDirection(value.Direction));
                writer.WritePropertyName("addedPoints");
                BreakdownPointAmountCodec.WriteCanonical(writer, value.AddedPoints);
                WriteSources(writer, value.Sources);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartObject("diceCoordinate");
            writer.WriteString("formation", definition.DiceCoordinate.Formation);
            writer.WriteStartArray("coordinates");
            foreach (var coordinate in definition.DiceCoordinate.Coordinates)
            {
                writer.WriteNumberValue(coordinate);
            }
            writer.WriteEndArray();
            WriteSources(writer, definition.DiceCoordinate.Sources);
            writer.WriteEndObject();
            WriteSources(writer, definition.Sources);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    private static BreakdownBandDefinition ParseBand(JsonElement element)
    {
        var properties = ReadObject(element, "bandId", "minimumWholePoints",
            "maximumWholePoints", "isCheckEligible", "sources");
        return new BreakdownBandDefinition(
            ReadString(properties["bandId"]),
            ReadInteger(properties["minimumWholePoints"]),
            ReadNullableInteger(properties["maximumWholePoints"]),
            ReadBoolean(properties["isCheckEligible"]),
            ParseSources(properties["sources"]));
    }

    private static BreakdownProfileDefinition ParseProfile(JsonElement element)
    {
        var properties = ReadObject(element, "profileId", "columnShift", "sources");
        return new BreakdownProfileDefinition(
            ReadString(properties["profileId"]),
            ReadInteger(properties["columnShift"]),
            ParseSources(properties["sources"]));
    }

    private static BreakdownVehicleTypeDefinition ParseVehicleType(JsonElement element)
    {
        var properties = ReadObject(element, "vehicleTypeId", "profileId", "sources");
        return new BreakdownVehicleTypeDefinition(
            ReadString(properties["vehicleTypeId"]),
            ReadString(properties["profileId"]),
            ParseSources(properties["sources"]));
    }

    private static BreakdownWeatherShiftDefinition ParseWeatherShift(JsonElement element)
    {
        var properties = ReadObject(
            element, "weatherKind", "columnShift", "condition", "sources");
        return new BreakdownWeatherShiftDefinition(
            ParseWeather(ReadString(properties["weatherKind"])),
            ReadInteger(properties["columnShift"]),
            ParseCondition(ReadString(properties["condition"])),
            ParseSources(properties["sources"]));
    }

    private static BreakdownWeatherInputTransformationDefinition
        ParseWeatherInputTransformation(JsonElement element)
    {
        var properties = ReadObject(
            element, "weatherKind", "inputRouteId", "treatedAsRouteId", "sources");
        return new BreakdownWeatherInputTransformationDefinition(
            ParseWeather(ReadString(properties["weatherKind"])),
            ReadString(properties["inputRouteId"]),
            ReadString(properties["treatedAsRouteId"]),
            ParseSources(properties["sources"]));
    }

    private static BreakdownTerrainDefinition ParseTerrain(JsonElement element)
    {
        var properties = ReadObject(element, "terrainId", "points", "sources");
        return new BreakdownTerrainDefinition(
            ReadString(properties["terrainId"]),
            ParseAmount(properties["points"]),
            ParseSources(properties["sources"]));
    }

    private static BreakdownRouteDefinition ParseRoute(JsonElement element)
    {
        var properties = ReadObject(element, "routeId", "operation", "amount", "sources");
        return new BreakdownRouteDefinition(
            ReadString(properties["routeId"]),
            ParseOperation(ReadString(properties["operation"])),
            ParseAmount(properties["amount"]),
            ParseSources(properties["sources"]));
    }

    private static BreakdownHexsideDefinition ParseHexside(JsonElement element)
    {
        var properties = ReadObject(
            element, "hexsideId", "direction", "addedPoints", "sources");
        return new BreakdownHexsideDefinition(
            ReadString(properties["hexsideId"]),
            ParseDirection(ReadString(properties["direction"])),
            ParseAmount(properties["addedPoints"]),
            ParseSources(properties["sources"]));
    }

    private static BreakdownDiceCoordinateDefinition ParseDiceCoordinate(JsonElement element)
    {
        var properties = ReadObject(
            element, "formation", "coordinates", "sources");
        return new BreakdownDiceCoordinateDefinition(
            ReadString(properties["formation"]),
            Copy(ReadArray(properties["coordinates"]).Select(ReadInteger)),
            ParseSources(properties["sources"]));
    }

    private static BreakdownPointAmount ParseAmount(JsonElement element) =>
        BreakdownPointAmountCodec.Deserialize(JsonSerializer.SerializeToUtf8Bytes(element));

    private static System.Collections.ObjectModel.ReadOnlyCollection<RuleReference> ParseSources(
        JsonElement element) =>
        Copy(ReadArray(element).Select(ParseSource));

    private static RuleReference ParseSource(JsonElement element)
    {
        var properties = ReadObject(element, "sourceId", "locator");
        return new RuleReference(
            ReadString(properties["sourceId"]),
            ReadString(properties["locator"]));
    }

    private static Dictionary<string, JsonElement> ReadObject(
        JsonElement element,
        params string[] expectedProperties)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("Expected a Breakdown rules artifact object.");
        }

        var actual = element.EnumerateObject().ToArray();
        if (!actual.Select(value => value.Name).SequenceEqual(expectedProperties))
        {
            throw new JsonException(
                "Breakdown rules artifact properties are missing, extra, or reordered.");
        }

        return actual.ToDictionary(value => value.Name, value => value.Value, StringComparer.Ordinal);
    }

    private static JsonElement.ArrayEnumerator ReadArray(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Expected a Breakdown rules artifact array.");
        }

        return element.EnumerateArray();
    }

    private static string ReadString(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("Expected a Breakdown rules artifact string.");
        }

        return element.GetString()!;
    }

    private static int ReadInteger(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value))
        {
            throw new JsonException("Expected a Breakdown rules artifact 32-bit integer.");
        }

        return value;
    }

    private static int? ReadNullableInteger(JsonElement element) =>
        element.ValueKind == JsonValueKind.Null ? null : ReadInteger(element);

    private static bool ReadBoolean(JsonElement element)
    {
        if (element.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new JsonException("Expected a Breakdown rules artifact Boolean.");
        }

        return element.GetBoolean();
    }

    private static string FormatWeather(BreakdownWeatherKind value) => value switch
    {
        BreakdownWeatherKind.Normal => "normal",
        BreakdownWeatherKind.Hot => "hot",
        BreakdownWeatherKind.Sandstorm => "sandstorm",
        BreakdownWeatherKind.Rainstorm => "rainstorm",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static BreakdownWeatherKind ParseWeather(string value) => value switch
    {
        "normal" => BreakdownWeatherKind.Normal,
        "hot" => BreakdownWeatherKind.Hot,
        "sandstorm" => BreakdownWeatherKind.Sandstorm,
        "rainstorm" => BreakdownWeatherKind.Rainstorm,
        _ => throw new JsonException("Unsupported Breakdown weather kind."),
    };

    private static string FormatCondition(BreakdownWeatherShiftCondition value) => value switch
    {
        BreakdownWeatherShiftCondition.Always => "always",
        BreakdownWeatherShiftCondition.AtLeastHalfBreakdownPoints =>
            "at-least-half-breakdown-points",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static BreakdownWeatherShiftCondition ParseCondition(string value) => value switch
    {
        "always" => BreakdownWeatherShiftCondition.Always,
        "at-least-half-breakdown-points" =>
            BreakdownWeatherShiftCondition.AtLeastHalfBreakdownPoints,
        _ => throw new JsonException("Unsupported Breakdown weather shift condition."),
    };

    private static string FormatOperation(BreakdownInputOperation value) => value switch
    {
        BreakdownInputOperation.Override => "override",
        BreakdownInputOperation.ScaleUnderlying => "scale-underlying",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static BreakdownInputOperation ParseOperation(string value) => value switch
    {
        "override" => BreakdownInputOperation.Override,
        "scale-underlying" => BreakdownInputOperation.ScaleUnderlying,
        _ => throw new JsonException("Unsupported Breakdown input operation."),
    };

    private static string FormatDirection(BreakdownHexsideDirection value) => value switch
    {
        BreakdownHexsideDirection.Either => "either",
        BreakdownHexsideDirection.Up => "up",
        BreakdownHexsideDirection.Down => "down",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static BreakdownHexsideDirection ParseDirection(string value) => value switch
    {
        "either" => BreakdownHexsideDirection.Either,
        "up" => BreakdownHexsideDirection.Up,
        "down" => BreakdownHexsideDirection.Down,
        _ => throw new JsonException("Unsupported Breakdown hexside direction."),
    };

    private static void WriteSources(
        Utf8JsonWriter writer,
        IEnumerable<RuleReference> sources)
    {
        writer.WriteStartArray("sources");
        foreach (var source in sources)
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", source.SourceId);
            writer.WriteString("locator", source.Locator);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static System.Collections.ObjectModel.ReadOnlyCollection<T> Copy<T>(
        IEnumerable<T> values) => Array.AsReadOnly(values.ToArray());
}
