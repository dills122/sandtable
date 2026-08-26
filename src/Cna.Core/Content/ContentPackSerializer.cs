using System.Globalization;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Content;

public static class ContentPackSerializer
{
    public static byte[] SerializeCanonical(ContentPackDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var issues = ContentPackValidator.Validate(definition).Issues
            .Concat(Cna1979ContentCompatibilityValidator.Validate(definition).Issues)
            .ToArray();

        if (issues.Length > 0)
        {
            throw new InvalidContentPackException(issues);
        }

        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", definition.SchemaVersion);
            writer.WriteString("formatId", definition.FormatId);
            writer.WriteString("packId", definition.PackId);
            writer.WriteString("rulesetId", definition.RulesetId);
            writer.WriteStartArray("capabilities");

            foreach (var capability in definition.Capabilities)
            {
                writer.WriteStringValue(capability);
            }

            writer.WriteEndArray();
            WriteSourceIndex(writer, definition.SourceIndex);
            WriteLocations(writer, definition.Locations);
            WriteWeatherAreaAssignments(writer, definition.WeatherAreaAssignments);
            WriteEdges(writer, definition.Edges);
            WriteFormations(writer, definition.Formations);
            WriteElements(writer, definition.Elements);
            WriteScenarios(writer, definition.Scenarios);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static ContentPackParseResult Deserialize(ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            using var document = JsonDocument.Parse(
                utf8Json.ToArray(),
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 64,
                });

            return ContentPackParseResult.Success(ParseDefinition(document.RootElement));
        }
        catch (ContentPackParseException exception)
        {
            return ContentPackParseResult.Failure(exception.Code, exception.Message);
        }
        catch (JsonException exception)
        {
            return ContentPackParseResult.Failure("content.invalid-json", exception.Message);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException
                or FormatException
                or OverflowException)
        {
            return ContentPackParseResult.Failure("content.invalid-value", exception.Message);
        }
    }

    private static void WriteSourceIndex(
        Utf8JsonWriter writer,
        IEnumerable<ContentSourceIndexEntry> entries)
    {
        writer.WriteStartArray("sourceIndex");

        foreach (var entry in entries)
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", entry.SourceId);
            writer.WriteString("kind", FormatSourceKind(entry.Kind));
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteLocations(
        Utf8JsonWriter writer,
        IEnumerable<ContentHex> locations)
    {
        writer.WriteStartArray("locations");

        foreach (var location in locations)
        {
            writer.WriteStartObject();
            writer.WriteString("locationId", location.LocationId);
            writer.WriteString("kind", "hex");
            writer.WriteString("terrainId", location.TerrainId);

            if (location.SourceCoordinate is null)
            {
                writer.WriteNull("sourceCoordinate");
            }
            else
            {
                writer.WriteStartObject("sourceCoordinate");
                writer.WriteString("sectionId", location.SourceCoordinate.SectionId);
                writer.WriteString("label", location.SourceCoordinate.Label);
                writer.WriteEndObject();
            }

            WriteOrigin(writer, "origin", location.Origin);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteEdges(
        Utf8JsonWriter writer,
        IEnumerable<ContentHexEdge> edges)
    {
        writer.WriteStartArray("edges");

        foreach (var edge in edges)
        {
            writer.WriteStartObject();
            writer.WriteString("firstLocationId", edge.FirstLocationId);
            writer.WriteString("secondLocationId", edge.SecondLocationId);
            writer.WriteStartArray("features");

            foreach (var feature in edge.Features)
            {
                writer.WriteStartObject();
                writer.WriteString("featureId", feature.FeatureId);
                WriteNullableString(
                    writer,
                    "directionFromLocationId",
                    feature.DirectionFromLocationId);
                WriteOrigin(writer, "origin", feature.Origin);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            WriteOrigin(writer, "origin", edge.Origin);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteWeatherAreaAssignments(
        Utf8JsonWriter writer,
        IEnumerable<ContentWeatherAreaAssignment> assignments)
    {
        writer.WriteStartArray("weatherAreaAssignments");
        foreach (var assignment in assignments)
        {
            writer.WriteStartObject();
            writer.WriteString("locationId", assignment.LocationId);
            writer.WriteString("weatherArea", FormatWeatherArea(assignment.WeatherArea));
            WriteOrigin(writer, "origin", assignment.Origin);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteFormations(
        Utf8JsonWriter writer,
        IEnumerable<ContentFormation> formations)
    {
        writer.WriteStartArray("formations");

        foreach (var formation in formations)
        {
            writer.WriteStartObject();
            writer.WriteString("formationId", formation.FormationId);
            writer.WriteString("sideId", formation.SideId);
            WriteNullableString(writer, "parentFormationId", formation.ParentFormationId);
            writer.WriteString("organizationId", formation.OrganizationId);
            WriteOrigin(writer, "origin", formation.Origin);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteElements(
        Utf8JsonWriter writer,
        IEnumerable<ContentCombatElement> elements)
    {
        writer.WriteStartArray("elements");

        foreach (var element in elements)
        {
            writer.WriteStartObject();
            writer.WriteString("elementId", element.ElementId);
            writer.WriteString("sideId", element.SideId);
            writer.WriteString("parentFormationId", element.ParentFormationId);
            writer.WriteString("organizationId", element.OrganizationId);
            writer.WriteString("mobilityId", element.MobilityId);
            writer.WriteNumber(
                "baseCapabilityPointAllowance",
                element.BaseCapabilityPointAllowance);
            writer.WriteString("placementMode", FormatPlacementMode(element.PlacementMode));
            WriteOrigin(writer, "origin", element.Origin);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteScenarios(
        Utf8JsonWriter writer,
        IEnumerable<ContentScenario> scenarios)
    {
        writer.WriteStartArray("scenarios");

        foreach (var scenario in scenarios)
        {
            writer.WriteStartObject();
            writer.WriteString("scenarioId", scenario.ScenarioId);
            WriteBoundary(writer, "start", scenario.Start);
            WriteBoundary(writer, "end", scenario.End);
            writer.WriteStartArray("initialPlacements");

            foreach (var placement in scenario.InitialPlacements)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", placement.ElementId);
                writer.WriteString("locationId", placement.LocationId);
                WriteOrigin(writer, "origin", placement.Origin);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            WriteOrigin(writer, "origin", scenario.Origin);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteBoundary(
        Utf8JsonWriter writer,
        string propertyName,
        ContentScenarioBoundary boundary)
    {
        writer.WriteStartObject(propertyName);
        writer.WriteNumber("gameTurn", boundary.GameTurn);
        writer.WriteNumber("operationStage", boundary.OperationStage);
        writer.WriteEndObject();
    }

    private static void WriteOrigin(
        Utf8JsonWriter writer,
        string propertyName,
        ContentOrigin origin)
    {
        writer.WriteStartObject(propertyName);
        writer.WriteString("kind", FormatOriginKind(origin.Kind));
        writer.WriteStartArray("references");

        foreach (var reference in origin.References)
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", reference.SourceId);
            writer.WriteString("locator", reference.Locator);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static ContentPackDefinition ParseDefinition(JsonElement element)
    {
        var properties = ReadObject(
            element,
            "schemaVersion",
            "formatId",
            "packId",
            "rulesetId",
            "capabilities",
            "sourceIndex",
            "locations",
            "weatherAreaAssignments",
            "edges",
            "formations",
            "elements",
            "scenarios");
        var schemaVersion = ReadInteger(properties["schemaVersion"], "/schemaVersion");

        if (schemaVersion != ContentPackDefinition.CurrentSchemaVersion)
        {
            Fail("content.unknown-version", $"Unsupported schema version '{schemaVersion}'.");
        }

        var formatId = ReadString(properties["formatId"], "/formatId");

        if (!string.Equals(
            formatId,
            ContentPackDefinition.CanonicalFormatId,
            StringComparison.Ordinal))
        {
            Fail("content.unknown-format", $"Unsupported format ID '{formatId}'.");
        }

        return new ContentPackDefinition(
            schemaVersion,
            formatId,
            ReadString(properties["packId"], "/packId"),
            ReadString(properties["rulesetId"], "/rulesetId"),
            ReadArray(properties["capabilities"], "/capabilities")
                .Select((value, index) => ReadString(value, $"/capabilities/{index}")),
            ReadArray(properties["sourceIndex"], "/sourceIndex")
                .Select((value, index) => ParseSourceIndex(value, $"/sourceIndex/{index}")),
            ReadArray(properties["locations"], "/locations")
                .Select((value, index) => ParseLocation(value, $"/locations/{index}")),
            ReadArray(properties["weatherAreaAssignments"], "/weatherAreaAssignments")
                .Select((value, index) => ParseWeatherAreaAssignment(
                    value,
                    $"/weatherAreaAssignments/{index}")),
            ReadArray(properties["edges"], "/edges")
                .Select((value, index) => ParseEdge(value, $"/edges/{index}")),
            ReadArray(properties["formations"], "/formations")
                .Select((value, index) => ParseFormation(value, $"/formations/{index}")),
            ReadArray(properties["elements"], "/elements")
                .Select((value, index) => ParseElement(value, $"/elements/{index}")),
            ReadArray(properties["scenarios"], "/scenarios")
                .Select((value, index) => ParseScenario(value, $"/scenarios/{index}")));
    }

    private static ContentSourceIndexEntry ParseSourceIndex(JsonElement element, string path)
    {
        var properties = ReadObject(element, "sourceId", "kind");
        return new ContentSourceIndexEntry(
            ReadString(properties["sourceId"], $"{path}/sourceId"),
            ParseSourceKind(ReadString(properties["kind"], $"{path}/kind"), path));
    }

    private static ContentHex ParseLocation(JsonElement element, string path)
    {
        var properties = ReadObject(
            element,
            "locationId",
            "kind",
            "terrainId",
            "sourceCoordinate",
            "origin");
        var kind = ReadString(properties["kind"], $"{path}/kind");

        if (!string.Equals(kind, "hex", StringComparison.Ordinal))
        {
            Fail("content.invalid-discriminant", $"Unknown location kind '{kind}' at {path}/kind.");
        }

        return new ContentHex(
            ReadString(properties["locationId"], $"{path}/locationId"),
            ReadString(properties["terrainId"], $"{path}/terrainId"),
            ParseNullableCoordinate(properties["sourceCoordinate"], $"{path}/sourceCoordinate"),
            ParseOrigin(properties["origin"], $"{path}/origin"));
    }

    private static ContentSourceCoordinate? ParseNullableCoordinate(
        JsonElement element,
        string path)
    {
        if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        var properties = ReadObject(element, "sectionId", "label");
        return new ContentSourceCoordinate(
            ReadString(properties["sectionId"], $"{path}/sectionId"),
            ReadString(properties["label"], $"{path}/label"));
    }

    private static ContentWeatherAreaAssignment ParseWeatherAreaAssignment(
        JsonElement element,
        string path)
    {
        var properties = ReadObject(element, "locationId", "weatherArea", "origin");
        return new ContentWeatherAreaAssignment(
            ReadString(properties["locationId"], $"{path}/locationId"),
            ParseWeatherArea(ReadString(properties["weatherArea"], $"{path}/weatherArea"), path),
            ParseOrigin(properties["origin"], $"{path}/origin"));
    }

    private static ContentHexEdge ParseEdge(JsonElement element, string path)
    {
        var properties = ReadObject(
            element,
            "firstLocationId",
            "secondLocationId",
            "features",
            "origin");
        return new ContentHexEdge(
            ReadString(properties["firstLocationId"], $"{path}/firstLocationId"),
            ReadString(properties["secondLocationId"], $"{path}/secondLocationId"),
            ReadArray(properties["features"], $"{path}/features")
                .Select((value, index) => ParseFeature(value, $"{path}/features/{index}")),
            ParseOrigin(properties["origin"], $"{path}/origin"));
    }

    private static ContentEdgeFeature ParseFeature(JsonElement element, string path)
    {
        var properties = ReadObject(
            element,
            "featureId",
            "directionFromLocationId",
            "origin");
        return new ContentEdgeFeature(
            ReadString(properties["featureId"], $"{path}/featureId"),
            ReadNullableString(
                properties["directionFromLocationId"],
                $"{path}/directionFromLocationId"),
            ParseOrigin(properties["origin"], $"{path}/origin"));
    }

    private static ContentFormation ParseFormation(JsonElement element, string path)
    {
        var properties = ReadObject(
            element,
            "formationId",
            "sideId",
            "parentFormationId",
            "organizationId",
            "origin");
        return new ContentFormation(
            ReadString(properties["formationId"], $"{path}/formationId"),
            ReadString(properties["sideId"], $"{path}/sideId"),
            ReadNullableString(properties["parentFormationId"], $"{path}/parentFormationId"),
            ReadString(properties["organizationId"], $"{path}/organizationId"),
            ParseOrigin(properties["origin"], $"{path}/origin"));
    }

    private static ContentCombatElement ParseElement(JsonElement element, string path)
    {
        var properties = ReadObject(
            element,
            "elementId",
            "sideId",
            "parentFormationId",
            "organizationId",
            "mobilityId",
            "baseCapabilityPointAllowance",
            "placementMode",
            "origin");
        return new ContentCombatElement(
            ReadString(properties["elementId"], $"{path}/elementId"),
            ReadString(properties["sideId"], $"{path}/sideId"),
            ReadString(properties["parentFormationId"], $"{path}/parentFormationId"),
            ReadString(properties["organizationId"], $"{path}/organizationId"),
            ReadString(properties["mobilityId"], $"{path}/mobilityId"),
            ReadInteger(
                properties["baseCapabilityPointAllowance"],
                $"{path}/baseCapabilityPointAllowance"),
            ParsePlacementMode(
                ReadString(properties["placementMode"], $"{path}/placementMode"),
                path),
            ParseOrigin(properties["origin"], $"{path}/origin"));
    }

    private static ContentScenario ParseScenario(JsonElement element, string path)
    {
        var properties = ReadObject(
            element,
            "scenarioId",
            "start",
            "end",
            "initialPlacements",
            "origin");
        return new ContentScenario(
            ReadString(properties["scenarioId"], $"{path}/scenarioId"),
            ParseBoundary(properties["start"], $"{path}/start"),
            ParseBoundary(properties["end"], $"{path}/end"),
            ReadArray(properties["initialPlacements"], $"{path}/initialPlacements")
                .Select((value, index) => ParsePlacement(
                    value,
                    $"{path}/initialPlacements/{index}")),
            ParseOrigin(properties["origin"], $"{path}/origin"));
    }

    private static ContentScenarioBoundary ParseBoundary(JsonElement element, string path)
    {
        var properties = ReadObject(element, "gameTurn", "operationStage");
        return new ContentScenarioBoundary(
            ReadInteger(properties["gameTurn"], $"{path}/gameTurn"),
            ReadInteger(properties["operationStage"], $"{path}/operationStage"));
    }

    private static ContentInitialPlacement ParsePlacement(JsonElement element, string path)
    {
        var properties = ReadObject(element, "elementId", "locationId", "origin");
        return new ContentInitialPlacement(
            ReadString(properties["elementId"], $"{path}/elementId"),
            ReadString(properties["locationId"], $"{path}/locationId"),
            ParseOrigin(properties["origin"], $"{path}/origin"));
    }

    private static ContentOrigin ParseOrigin(JsonElement element, string path)
    {
        var properties = ReadObject(element, "kind", "references");
        return new ContentOrigin(
            ParseOriginKind(ReadString(properties["kind"], $"{path}/kind"), path),
            ReadArray(properties["references"], $"{path}/references")
                .Select((value, index) => ParseReference(
                    value,
                    $"{path}/references/{index}")));
    }

    private static RuleReference ParseReference(JsonElement element, string path)
    {
        var properties = ReadObject(element, "sourceId", "locator");
        return new RuleReference(
            ReadString(properties["sourceId"], $"{path}/sourceId"),
            ReadString(properties["locator"], $"{path}/locator"));
    }

    private static Dictionary<string, JsonElement> ReadObject(
        JsonElement element,
        params string[] expectedProperties)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            Fail("content.invalid-json-shape", "Expected a JSON object.");
        }

        var expected = expectedProperties.ToHashSet(StringComparer.Ordinal);
        var properties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        foreach (var property in element.EnumerateObject())
        {
            if (!expected.Contains(property.Name))
            {
                Fail("content.unknown-property", $"Unknown property '{property.Name}'.");
            }

            if (!properties.TryAdd(property.Name, property.Value))
            {
                Fail("content.duplicate-property", $"Duplicate property '{property.Name}'.");
            }
        }

        foreach (var propertyName in expectedProperties)
        {
            if (!properties.ContainsKey(propertyName))
            {
                Fail("content.missing-property", $"Missing required property '{propertyName}'.");
            }
        }

        return properties;
    }

    private static JsonElement.ArrayEnumerator ReadArray(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            Fail("content.invalid-json-shape", $"Expected a JSON array at {path}.");
        }

        return element.EnumerateArray();
    }

    private static string ReadString(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            Fail("content.invalid-json-shape", $"Expected a JSON string at {path}.");
        }

        return element.GetString()!;
    }

    private static string? ReadNullableString(JsonElement element, string path) =>
        element.ValueKind == JsonValueKind.Null ? null : ReadString(element, path);

    private static int ReadInteger(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.Number)
        {
            Fail("content.invalid-json-shape", $"Expected an integer at {path}.");
        }

        var raw = element.GetRawText();

        if (!int.TryParse(
                raw,
                NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out var value)
            || !string.Equals(
                raw,
                value.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal))
        {
            Fail("content.invalid-number", $"Expected a canonical 32-bit integer at {path}.");
        }

        return value;
    }

    private static ContentOriginKind ParseOriginKind(string value, string path) => value switch
    {
        "source-derived" => ContentOriginKind.SourceDerived,
        "synthetic" => ContentOriginKind.Synthetic,
        _ => throw new ContentPackParseException(
            "content.invalid-discriminant",
            $"Unknown origin kind '{value}' at {path}/kind."),
    };

    private static ContentSourceKind ParseSourceKind(string value, string path) => value switch
    {
        "published-primary" => ContentSourceKind.PublishedPrimary,
        "adopted-ruling" => ContentSourceKind.AdoptedRuling,
        "repository-synthetic" => ContentSourceKind.RepositorySynthetic,
        _ => throw new ContentPackParseException(
            "content.invalid-discriminant",
            $"Unknown source kind '{value}' at {path}/kind."),
    };

    private static ContentPlacementMode ParsePlacementMode(string value, string path) => value switch
    {
        "independent" => ContentPlacementMode.Independent,
        "attachment-only" => ContentPlacementMode.AttachmentOnly,
        _ => throw new ContentPackParseException(
            "content.invalid-discriminant",
            $"Unknown placement mode '{value}' at {path}/placementMode."),
    };

    private static ContentWeatherArea ParseWeatherArea(string value, string path) => value switch
    {
        "a" => ContentWeatherArea.A,
        "b" => ContentWeatherArea.B,
        "c" => ContentWeatherArea.C,
        "d" => ContentWeatherArea.D,
        "e" => ContentWeatherArea.E,
        _ => throw new ContentPackParseException(
            "content.invalid-discriminant",
            $"Unknown Weather area '{value}' at {path}/weatherArea."),
    };

    private static string FormatWeatherArea(ContentWeatherArea area) => area switch
    {
        ContentWeatherArea.A => "a",
        ContentWeatherArea.B => "b",
        ContentWeatherArea.C => "c",
        ContentWeatherArea.D => "d",
        ContentWeatherArea.E => "e",
        _ => throw new ArgumentOutOfRangeException(nameof(area)),
    };

    private static string FormatOriginKind(ContentOriginKind kind) => kind switch
    {
        ContentOriginKind.SourceDerived => "source-derived",
        ContentOriginKind.Synthetic => "synthetic",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static string FormatSourceKind(ContentSourceKind kind) => kind switch
    {
        ContentSourceKind.PublishedPrimary => "published-primary",
        ContentSourceKind.AdoptedRuling => "adopted-ruling",
        ContentSourceKind.RepositorySynthetic => "repository-synthetic",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static string FormatPlacementMode(ContentPlacementMode mode) => mode switch
    {
        ContentPlacementMode.Independent => "independent",
        ContentPlacementMode.AttachmentOnly => "attachment-only",
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    private static void WriteNullableString(
        Utf8JsonWriter writer,
        string propertyName,
        string? value)
    {
        if (value is null)
        {
            writer.WriteNull(propertyName);
        }
        else
        {
            writer.WriteString(propertyName, value);
        }
    }

    private static void Fail(string code, string message) =>
        throw new ContentPackParseException(code, message);

    private sealed class ContentPackParseException : Exception
    {
        public ContentPackParseException(string code, string message)
            : base(message)
        {
            Code = code;
        }

        public string Code { get; }
    }
}
