using System.Text.Json;

namespace Cna.Core.Rules;

internal static class MovementRulesArtifactCodec
{
    public static byte[] SerializeCanonical(MovementRulesArtifactDefinition definition)
    {
        Validate(definition);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", definition.SchemaVersion);
            writer.WriteStartArray("mobility");
            foreach (var value in definition.Mobility)
            {
                writer.WriteStartObject();
                writer.WriteString("mobilityId", value.MobilityId);
                writer.WriteString("mobilityClass", FormatMobility(value.MobilityClass));
                WriteSources(writer, value.Sources);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("terrain");
            foreach (var value in definition.Terrain)
            {
                writer.WriteStartObject();
                writer.WriteString("terrainId", value.TerrainId);
                writer.WriteString("mobilityId", value.MobilityId);
                writer.WritePropertyName("cost");
                CapabilityPointAmountCodec.WriteCanonical(writer, value.Cost);
                writer.WriteNumber("stoppingStackingLimit", value.StoppingStackingLimit);
                WriteSources(writer, value.Sources);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("routes");
            foreach (var value in definition.Routes)
            {
                writer.WriteStartObject();
                writer.WriteString("routeId", value.RouteId);
                writer.WriteString("mobilityId", value.MobilityId);
                writer.WriteString("costKind", FormatRouteCostKind(value.CostKind));
                writer.WritePropertyName("amount");
                CapabilityPointAmountCodec.WriteCanonical(writer, value.Amount);
                writer.WriteNumber("traversalStackingLimit", value.TraversalStackingLimit);
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
                writer.WriteString("mobilityId", value.MobilityId);
                writer.WritePropertyName("addedCost");
                CapabilityPointAmountCodec.WriteCanonical(writer, value.AddedCost);
                WriteSources(writer, value.Sources);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("stacking");
            foreach (var value in definition.Stacking)
            {
                writer.WriteStartObject();
                writer.WriteString("organizationId", value.OrganizationId);
                writer.WriteNumber("stackingValue", value.StackingValue);
                WriteSources(writer, value.Sources);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            WriteSources(writer, definition.Sources);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static MovementRulesArtifactDefinition Deserialize(ReadOnlySpan<byte> utf8Json)
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
            var properties = ReadObject(
                document.RootElement,
                "schemaVersion",
                "mobility",
                "terrain",
                "routes",
                "hexsides",
                "stacking",
                "sources");
            var definition = new MovementRulesArtifactDefinition(
                ReadInteger(properties["schemaVersion"]),
                Copy(ReadArray(properties["mobility"]).Select(ParseMobility)),
                Copy(ReadArray(properties["terrain"]).Select(ParseTerrain)),
                Copy(ReadArray(properties["routes"]).Select(ParseRoute)),
                Copy(ReadArray(properties["hexsides"]).Select(ParseHexside)),
                Copy(ReadArray(properties["stacking"]).Select(ParseStacking)),
                Copy(ReadArray(properties["sources"]).Select(ParseSource)));
            Validate(definition);

            if (!utf8Json.SequenceEqual(SerializeCanonical(definition)))
            {
                throw new JsonException("The Movement rules artifact is not canonical JSON.");
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
            throw new JsonException("The Movement rules artifact is invalid.", exception);
        }
    }

    internal static void Validate(MovementRulesArtifactDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var authority = Cna1979Movement.Definition;

        Require(definition.SchemaVersion == Cna1979Movement.SchemaVersion);
        Require(SequenceEqual(definition.Mobility, authority.Mobility, MobilityEquals));
        Require(SequenceEqual(definition.Terrain, authority.Terrain, TerrainEquals));
        Require(SequenceEqual(definition.Routes, authority.Routes, RouteEquals));
        Require(SequenceEqual(definition.Hexsides, authority.Hexsides, HexsideEquals));
        Require(SequenceEqual(definition.Stacking, authority.Stacking, StackingEquals));
        Require(definition.Sources.SequenceEqual(authority.Sources));
    }

    private static MovementMobilityDefinition ParseMobility(JsonElement element)
    {
        var properties = ReadObject(element, "mobilityId", "mobilityClass", "sources");
        return new MovementMobilityDefinition(
            ReadString(properties["mobilityId"]),
            ParseMobility(ReadString(properties["mobilityClass"])),
            Copy(ReadArray(properties["sources"]).Select(ParseSource)));
    }

    private static MovementTerrainDefinition ParseTerrain(JsonElement element)
    {
        var properties = ReadObject(
            element,
            "terrainId",
            "mobilityId",
            "cost",
            "stoppingStackingLimit",
            "sources");
        return new MovementTerrainDefinition(
            ReadString(properties["terrainId"]),
            ReadString(properties["mobilityId"]),
            ParseAmount(properties["cost"]),
            ReadInteger(properties["stoppingStackingLimit"]),
            Copy(ReadArray(properties["sources"]).Select(ParseSource)));
    }

    private static MovementRouteDefinition ParseRoute(JsonElement element)
    {
        var properties = ReadObject(
            element,
            "routeId",
            "mobilityId",
            "costKind",
            "amount",
            "traversalStackingLimit",
            "sources");
        return new MovementRouteDefinition(
            ReadString(properties["routeId"]),
            ReadString(properties["mobilityId"]),
            ParseRouteCostKind(ReadString(properties["costKind"])),
            ParseAmount(properties["amount"]),
            ReadInteger(properties["traversalStackingLimit"]),
            Copy(ReadArray(properties["sources"]).Select(ParseSource)));
    }

    private static MovementHexsideDefinition ParseHexside(JsonElement element)
    {
        var properties = ReadObject(
            element,
            "hexsideId",
            "direction",
            "mobilityId",
            "addedCost",
            "sources");
        return new MovementHexsideDefinition(
            ReadString(properties["hexsideId"]),
            ParseDirection(ReadString(properties["direction"])),
            ReadString(properties["mobilityId"]),
            ParseAmount(properties["addedCost"]),
            Copy(ReadArray(properties["sources"]).Select(ParseSource)));
    }

    private static MovementStackingDefinition ParseStacking(JsonElement element)
    {
        var properties = ReadObject(
            element,
            "organizationId",
            "stackingValue",
            "sources");
        return new MovementStackingDefinition(
            ReadString(properties["organizationId"]),
            ReadInteger(properties["stackingValue"]),
            Copy(ReadArray(properties["sources"]).Select(ParseSource)));
    }

    private static CapabilityPointAmount ParseAmount(JsonElement element) =>
        CapabilityPointAmountCodec.Deserialize(JsonSerializer.SerializeToUtf8Bytes(element));

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
            throw new JsonException("Expected a Movement rules artifact object.");
        }

        var actual = element.EnumerateObject().ToArray();
        if (!actual.Select(value => value.Name).SequenceEqual(expectedProperties))
        {
            throw new JsonException(
                "Movement rules artifact properties are missing, extra, or reordered.");
        }

        return actual.ToDictionary(value => value.Name, value => value.Value, StringComparer.Ordinal);
    }

    private static JsonElement.ArrayEnumerator ReadArray(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Expected a Movement rules artifact array.");
        }

        return element.EnumerateArray();
    }

    private static string ReadString(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("Expected a Movement rules artifact string.");
        }

        return element.GetString()!;
    }

    private static int ReadInteger(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value))
        {
            throw new JsonException("Expected a Movement rules artifact 32-bit integer.");
        }

        return value;
    }

    private static string FormatMobility(MovementMobilityClass value) => value switch
    {
        MovementMobilityClass.NonMotorized => "non-motorized",
        MovementMobilityClass.Motorized => "motorized",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static MovementMobilityClass ParseMobility(string value) => value switch
    {
        "non-motorized" => MovementMobilityClass.NonMotorized,
        "motorized" => MovementMobilityClass.Motorized,
        _ => throw new JsonException("Unsupported Movement mobility class."),
    };

    private static string FormatRouteCostKind(MovementRouteCostKind value) => value switch
    {
        MovementRouteCostKind.Override => "override",
        MovementRouteCostKind.ScaleUnderlying => "scale-underlying",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static MovementRouteCostKind ParseRouteCostKind(string value) => value switch
    {
        "override" => MovementRouteCostKind.Override,
        "scale-underlying" => MovementRouteCostKind.ScaleUnderlying,
        _ => throw new JsonException("Unsupported Movement route cost kind."),
    };

    private static string FormatDirection(MovementHexsideDirection value) => value switch
    {
        MovementHexsideDirection.Either => "either",
        MovementHexsideDirection.Up => "up",
        MovementHexsideDirection.Down => "down",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
    };

    private static MovementHexsideDirection ParseDirection(string value) => value switch
    {
        "either" => MovementHexsideDirection.Either,
        "up" => MovementHexsideDirection.Up,
        "down" => MovementHexsideDirection.Down,
        _ => throw new JsonException("Unsupported Movement hexside direction."),
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
        IEnumerable<T> values) =>
        Array.AsReadOnly(values.ToArray());

    private static bool SequenceEqual<T>(
        IReadOnlyList<T> left,
        IReadOnlyList<T> right,
        Func<T, T, bool> equals) =>
        left.Count == right.Count
        && left.Zip(right).All(pair => equals(pair.First, pair.Second));

    private static bool MobilityEquals(
        MovementMobilityDefinition left,
        MovementMobilityDefinition right) =>
        string.Equals(left.MobilityId, right.MobilityId, StringComparison.Ordinal)
        && left.MobilityClass == right.MobilityClass
        && left.Sources.SequenceEqual(right.Sources);

    private static bool TerrainEquals(
        MovementTerrainDefinition left,
        MovementTerrainDefinition right) =>
        string.Equals(left.TerrainId, right.TerrainId, StringComparison.Ordinal)
        && string.Equals(left.MobilityId, right.MobilityId, StringComparison.Ordinal)
        && left.Cost == right.Cost
        && left.StoppingStackingLimit == right.StoppingStackingLimit
        && left.Sources.SequenceEqual(right.Sources);

    private static bool RouteEquals(
        MovementRouteDefinition left,
        MovementRouteDefinition right) =>
        string.Equals(left.RouteId, right.RouteId, StringComparison.Ordinal)
        && string.Equals(left.MobilityId, right.MobilityId, StringComparison.Ordinal)
        && left.CostKind == right.CostKind
        && left.Amount == right.Amount
        && left.TraversalStackingLimit == right.TraversalStackingLimit
        && left.Sources.SequenceEqual(right.Sources);

    private static bool HexsideEquals(
        MovementHexsideDefinition left,
        MovementHexsideDefinition right) =>
        string.Equals(left.HexsideId, right.HexsideId, StringComparison.Ordinal)
        && left.Direction == right.Direction
        && string.Equals(left.MobilityId, right.MobilityId, StringComparison.Ordinal)
        && left.AddedCost == right.AddedCost
        && left.Sources.SequenceEqual(right.Sources);

    private static bool StackingEquals(
        MovementStackingDefinition left,
        MovementStackingDefinition right) =>
        string.Equals(left.OrganizationId, right.OrganizationId, StringComparison.Ordinal)
        && left.StackingValue == right.StackingValue
        && left.Sources.SequenceEqual(right.Sources);

    private static void Require(bool condition)
    {
        if (!condition)
        {
            throw new JsonException("The Movement rules artifact authority is unsupported.");
        }
    }
}
