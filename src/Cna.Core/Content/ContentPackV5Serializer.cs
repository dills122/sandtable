using System.Text.Json;

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
