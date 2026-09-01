using System.Globalization;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Content;

public static class ContentPackV5Serializer
{
    public static byte[] SerializeCanonical(ContentPackV5Definition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var issues = ContentPackV5Validator.Validate(definition).Issues
            .Concat(Cna1979ContentV5CompatibilityValidator.Validate(definition).Issues)
            .ToArray();
        if (issues.Length > 0)
        {
            throw new InvalidContentPackException(issues);
        }

        var legacyBytes = ContentPackSerializer.SerializeCanonical(definition.LegacyDefinition);
        using var legacyDocument = JsonDocument.Parse(legacyBytes);
        var legacy = legacyDocument.RootElement;
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", ContentPackV5Definition.SchemaVersion);
            writer.WriteString("formatId", ContentPackV5Definition.CanonicalFormatId);
            CopyProperty(writer, legacy, "packId");
            CopyProperty(writer, legacy, "rulesetId");
            writer.WriteStartArray("capabilities");
            foreach (var capability in definition.Capabilities)
            {
                writer.WriteStringValue(capability);
            }

            writer.WriteEndArray();
            CopyProperty(writer, legacy, "sourceIndex");
            CopyProperty(writer, legacy, "locations");
            CopyProperty(writer, legacy, "weatherAreaAssignments");
            CopyProperty(writer, legacy, "edges");
            CopyProperty(writer, legacy, "formations");
            WriteElements(writer, legacy.GetProperty("elements"), definition);
            WriteScenarios(writer, legacy.GetProperty("scenarios"), definition);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static ContentPackV5ParseResult Deserialize(ReadOnlySpan<byte> utf8Json)
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
            var definition = ParseDefinition(document.RootElement);
            if (!utf8Json.SequenceEqual(SerializeCanonical(definition)))
            {
                return ContentPackV5ParseResult.Failure(
                    "content.noncanonical-json",
                    "Content v5 input must be byte-identical canonical JSON.");
            }

            return ContentPackV5ParseResult.Success(definition);
        }
        catch (ContentPackV5ParseException exception)
        {
            return ContentPackV5ParseResult.Failure(exception.Code, exception.Message);
        }
        catch (JsonException exception)
        {
            return ContentPackV5ParseResult.Failure("content.invalid-json", exception.Message);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException
                or FormatException
                or OverflowException)
        {
            return ContentPackV5ParseResult.Failure("content.invalid-value", exception.Message);
        }
    }

    private static ContentPackV5Definition ParseDefinition(JsonElement element)
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
        if (schemaVersion != ContentPackV5Definition.SchemaVersion)
        {
            Fail("content.unknown-version", $"Unsupported schema version '{schemaVersion}'.");
        }

        var formatId = ReadString(properties["formatId"], "/formatId");
        if (!string.Equals(
            formatId,
            ContentPackV5Definition.CanonicalFormatId,
            StringComparison.Ordinal))
        {
            Fail("content.unknown-format", $"Unsupported format ID '{formatId}'.");
        }

        var capabilities = ReadArray(properties["capabilities"], "/capabilities")
            .Select((value, index) => ReadString(value, $"/capabilities/{index}"))
            .ToArray();
        var combatCapabilityCount = capabilities.Count(value => string.Equals(
            value,
            ContentPackV5Definition.CombatCapabilityId,
            StringComparison.Ordinal));
        if (combatCapabilityCount == 0)
        {
            Fail(
                "content.v5.missing-capability",
                $"Content v5 requires the '{ContentPackV5Definition.CombatCapabilityId}' capability.");
        }

        if (combatCapabilityCount > 1)
        {
            Fail(
                "content.v5.duplicate-capability",
                $"Content v5 permits exactly one '{ContentPackV5Definition.CombatCapabilityId}' capability.");
        }

        var elementFacts = new List<ContentElementCombatFacts>();
        var placementFacts = new List<ContentInitialPlacementCombatFacts>();
        using var legacyStream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(legacyStream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", ContentPackDefinition.CurrentSchemaVersion);
            writer.WriteString("formatId", ContentPackDefinition.CanonicalFormatId);
            CopyProperty(writer, properties, "packId");
            CopyProperty(writer, properties, "rulesetId");
            writer.WriteStartArray("capabilities");
            foreach (var capability in capabilities.Where(value => !string.Equals(
                value,
                ContentPackV5Definition.CombatCapabilityId,
                StringComparison.Ordinal)))
            {
                writer.WriteStringValue(capability);
            }

            writer.WriteEndArray();
            CopyProperty(writer, properties, "sourceIndex");
            CopyProperty(writer, properties, "locations");
            CopyProperty(writer, properties, "weatherAreaAssignments");
            CopyProperty(writer, properties, "edges");
            CopyProperty(writer, properties, "formations");
            WriteLegacyElements(writer, properties["elements"], elementFacts);
            WriteLegacyScenarios(writer, properties["scenarios"], placementFacts);
            writer.WriteEndObject();
        }

        var legacyResult = ContentPackSerializer.Deserialize(legacyStream.ToArray());
        if (!legacyResult.IsSuccess)
        {
            Fail(legacyResult.ErrorCode!, legacyResult.Message!);
        }

        var definition = new ContentPackV5Definition(
            legacyResult.Definition!,
            elementFacts,
            placementFacts);
        var issues = ContentPackV5Validator.Validate(definition).Issues
            .Concat(Cna1979ContentV5CompatibilityValidator.Validate(definition).Issues)
            .ToArray();
        if (issues.Length > 0)
        {
            var issue = issues[0];
            Fail(issue.Code, $"{issue.Path}: {issue.Message}");
        }

        return definition;
    }

    private static void WriteLegacyElements(
        Utf8JsonWriter writer,
        JsonElement element,
        List<ContentElementCombatFacts> facts)
    {
        writer.WriteStartArray("elements");
        var index = 0;
        foreach (var value in ReadArray(element, "/elements"))
        {
            var path = $"/elements/{index++}";
            var properties = ReadObject(
                value,
                "elementId",
                "sideId",
                "parentFormationId",
                "organizationId",
                "mobilityId",
                "baseCapabilityPointAllowance",
                "placementMode",
                "breakdownVehicleCohort",
                "combatClassificationId",
                "combatOrigin",
                "components",
                "origin");
            var elementId = ReadString(properties["elementId"], $"{path}/elementId");
            facts.Add(new ContentElementCombatFacts(
                elementId,
                ReadString(
                    properties["combatClassificationId"],
                    $"{path}/combatClassificationId"),
                ReadArray(properties["components"], $"{path}/components")
                    .Select((component, componentIndex) => ParseComponent(
                        component,
                        $"{path}/components/{componentIndex}")),
                ParseOrigin(properties["combatOrigin"], $"{path}/combatOrigin")));

            writer.WriteStartObject();
            foreach (var propertyName in new[]
            {
                "elementId",
                "sideId",
                "parentFormationId",
                "organizationId",
                "mobilityId",
                "baseCapabilityPointAllowance",
                "placementMode",
                "breakdownVehicleCohort",
                "origin",
            })
            {
                CopyProperty(writer, properties, propertyName);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static ContentCombatComponent ParseComponent(JsonElement element, string path)
    {
        var properties = ReadObject(
            element,
            "componentId",
            "componentClassId",
            "maximumToe",
            "defensiveCloseAssaultRating",
            "origin");
        return new ContentCombatComponent(
            ReadString(properties["componentId"], $"{path}/componentId"),
            ReadString(properties["componentClassId"], $"{path}/componentClassId"),
            ReadInteger(properties["maximumToe"], $"{path}/maximumToe"),
            ReadInteger(
                properties["defensiveCloseAssaultRating"],
                $"{path}/defensiveCloseAssaultRating"),
            ParseOrigin(properties["origin"], $"{path}/origin"));
    }

    private static void WriteLegacyScenarios(
        Utf8JsonWriter writer,
        JsonElement element,
        List<ContentInitialPlacementCombatFacts> facts)
    {
        writer.WriteStartArray("scenarios");
        var scenarioIndex = 0;
        foreach (var scenario in ReadArray(element, "/scenarios"))
        {
            var path = $"/scenarios/{scenarioIndex++}";
            var properties = ReadObject(
                scenario,
                "scenarioId",
                "start",
                "end",
                "initialPlacements",
                "origin");
            var scenarioId = ReadString(properties["scenarioId"], $"{path}/scenarioId");
            writer.WriteStartObject();
            CopyProperty(writer, properties, "scenarioId");
            CopyProperty(writer, properties, "start");
            CopyProperty(writer, properties, "end");
            writer.WriteStartArray("initialPlacements");
            var placementIndex = 0;
            foreach (var placement in ReadArray(
                properties["initialPlacements"],
                $"{path}/initialPlacements"))
            {
                var placementPath = $"{path}/initialPlacements/{placementIndex++}";
                var placementProperties = ReadObject(
                    placement,
                    "elementId",
                    "locationId",
                    "initialComponentToes",
                    "origin");
                var elementId = ReadString(
                    placementProperties["elementId"],
                    $"{placementPath}/elementId");
                facts.Add(new ContentInitialPlacementCombatFacts(
                    scenarioId,
                    elementId,
                    ReadArray(
                        placementProperties["initialComponentToes"],
                        $"{placementPath}/initialComponentToes")
                        .Select((seed, seedIndex) => ParseSeed(
                            seed,
                            $"{placementPath}/initialComponentToes/{seedIndex}"))));
                writer.WriteStartObject();
                CopyProperty(writer, placementProperties, "elementId");
                CopyProperty(writer, placementProperties, "locationId");
                CopyProperty(writer, placementProperties, "origin");
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            CopyProperty(writer, properties, "origin");
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static ContentInitialComponentToe ParseSeed(JsonElement element, string path)
    {
        var properties = ReadObject(element, "componentId", "currentToe", "origin");
        return new ContentInitialComponentToe(
            ReadString(properties["componentId"], $"{path}/componentId"),
            ReadInteger(properties["currentToe"], $"{path}/currentToe"),
            ParseOrigin(properties["origin"], $"{path}/origin"));
    }

    private static ContentOrigin ParseOrigin(JsonElement element, string path)
    {
        var properties = ReadObject(element, "kind", "references");
        var kind = ReadString(properties["kind"], $"{path}/kind") switch
        {
            "source-derived" => ContentOriginKind.SourceDerived,
            "synthetic" => ContentOriginKind.Synthetic,
            var value => throw new ContentPackV5ParseException(
                "content.invalid-discriminant",
                $"Unknown origin kind '{value}' at {path}/kind."),
        };
        return new ContentOrigin(
            kind,
            ReadArray(properties["references"], $"{path}/references")
                .Select((reference, index) => ParseReference(
                    reference,
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

    private static void CopyProperty(
        Utf8JsonWriter writer,
        Dictionary<string, JsonElement> properties,
        string propertyName)
    {
        writer.WritePropertyName(propertyName);
        properties[propertyName].WriteTo(writer);
    }

    private static void Fail(string code, string message) =>
        throw new ContentPackV5ParseException(code, message);

    private sealed class ContentPackV5ParseException : Exception
    {
        public ContentPackV5ParseException(string code, string message)
            : base(message)
        {
            Code = code;
        }

        public string Code { get; }
    }

    private static void WriteElements(
        Utf8JsonWriter writer,
        JsonElement legacyElements,
        ContentPackV5Definition definition)
    {
        var combatByElement = definition.ElementCombatFacts.ToDictionary(
            value => value.ElementId,
            StringComparer.Ordinal);
        writer.WriteStartArray("elements");

        foreach (var legacyElement in legacyElements.EnumerateArray())
        {
            var elementId = legacyElement.GetProperty("elementId").GetString()!;
            var combat = combatByElement[elementId];
            writer.WriteStartObject();
            foreach (var property in legacyElement.EnumerateObject())
            {
                if (property.NameEquals("breakdownVehicleCohort"))
                {
                    writer.WriteString(
                        "combatClassificationId",
                        combat.CombatClassificationId);
                    WriteOrigin(writer, "combatOrigin", combat.Origin);
                    WriteComponents(writer, combat.Components);
                }

                CopyProperty(writer, property);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteComponents(
        Utf8JsonWriter writer,
        IEnumerable<ContentCombatComponent> components)
    {
        writer.WriteStartArray("components");
        foreach (var component in components)
        {
            writer.WriteStartObject();
            writer.WriteString("componentId", component.ComponentId);
            writer.WriteString("componentClassId", component.ComponentClassId);
            writer.WriteNumber("maximumToe", component.MaximumToe);
            writer.WriteNumber(
                "defensiveCloseAssaultRating",
                component.DefensiveCloseAssaultRating);
            WriteOrigin(writer, "origin", component.Origin);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteScenarios(
        Utf8JsonWriter writer,
        JsonElement legacyScenarios,
        ContentPackV5Definition definition)
    {
        var factsByPlacement = definition.InitialPlacementCombatFacts.ToDictionary(
            value => (value.ScenarioId, value.ElementId));
        writer.WriteStartArray("scenarios");

        foreach (var legacyScenario in legacyScenarios.EnumerateArray())
        {
            var scenarioId = legacyScenario.GetProperty("scenarioId").GetString()!;
            writer.WriteStartObject();
            foreach (var property in legacyScenario.EnumerateObject())
            {
                if (!property.NameEquals("initialPlacements"))
                {
                    CopyProperty(writer, property);
                    continue;
                }

                writer.WriteStartArray("initialPlacements");
                foreach (var legacyPlacement in property.Value.EnumerateArray())
                {
                    var elementId = legacyPlacement.GetProperty("elementId").GetString()!;
                    var facts = factsByPlacement[(scenarioId, elementId)];
                    writer.WriteStartObject();
                    foreach (var placementProperty in legacyPlacement.EnumerateObject())
                    {
                        if (placementProperty.NameEquals("origin"))
                        {
                            WriteInitialComponentToes(writer, facts.InitialComponentToes);
                        }

                        CopyProperty(writer, placementProperty);
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteInitialComponentToes(
        Utf8JsonWriter writer,
        IEnumerable<ContentInitialComponentToe> initialComponentToes)
    {
        writer.WriteStartArray("initialComponentToes");
        foreach (var seed in initialComponentToes)
        {
            writer.WriteStartObject();
            writer.WriteString("componentId", seed.ComponentId);
            writer.WriteNumber("currentToe", seed.CurrentToe);
            WriteOrigin(writer, "origin", seed.Origin);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteOrigin(
        Utf8JsonWriter writer,
        string propertyName,
        ContentOrigin origin)
    {
        writer.WriteStartObject(propertyName);
        writer.WriteString("kind", origin.Kind switch
        {
            ContentOriginKind.SourceDerived => "source-derived",
            ContentOriginKind.Synthetic => "synthetic",
            _ => throw new InvalidOperationException($"Unsupported origin kind '{origin.Kind}'."),
        });
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

    private static void CopyProperty(
        Utf8JsonWriter writer,
        JsonElement parent,
        string propertyName)
    {
        writer.WritePropertyName(propertyName);
        parent.GetProperty(propertyName).WriteTo(writer);
    }

    private static void CopyProperty(Utf8JsonWriter writer, JsonProperty property)
    {
        writer.WritePropertyName(property.Name);
        property.Value.WriteTo(writer);
    }
}
