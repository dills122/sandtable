using System.Text.Json;

namespace Cna.Core.Rules;

internal static class ReserveRulesArtifactCodec
{
    public static byte[] SerializeCanonical(ReserveRulesArtifactDefinition definition)
    {
        Validate(definition);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", definition.SchemaVersion);
            writer.WriteString("eligibleOwner", definition.EligibleOwner);
            writer.WriteString("assignmentTiming", definition.AssignmentTiming);
            writer.WriteString("assignmentResult", definition.AssignmentResult);
            writer.WriteNumber("capabilityPointCost", definition.CapabilityPointCost);
            writer.WriteStartObject("supportedTransition");
            writer.WriteString("from", definition.SupportedTransition.From);
            writer.WriteString("to", definition.SupportedTransition.To);
            writer.WriteEndObject();
            writer.WriteStartArray("sources");
            foreach (var source in definition.Sources)
            {
                writer.WriteStartObject();
                writer.WriteString("sourceId", source.SourceId);
                writer.WriteString("locator", source.Locator);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static ReserveRulesArtifactDefinition Deserialize(ReadOnlySpan<byte> utf8Json)
    {
        try
        {
            using var document = JsonDocument.Parse(
                utf8Json.ToArray(),
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = false,
                    CommentHandling = JsonCommentHandling.Disallow,
                    MaxDepth = 16,
                });
            var properties = ReadObject(
                document.RootElement,
                "schemaVersion",
                "eligibleOwner",
                "assignmentTiming",
                "assignmentResult",
                "capabilityPointCost",
                "supportedTransition",
                "sources");
            var transition = ReadObject(
                properties["supportedTransition"],
                "from",
                "to");
            var definition = new ReserveRulesArtifactDefinition(
                ReadInteger(properties["schemaVersion"]),
                ReadString(properties["eligibleOwner"]),
                ReadString(properties["assignmentTiming"]),
                ReadString(properties["assignmentResult"]),
                ReadInteger(properties["capabilityPointCost"]),
                new ReserveStatusTransitionDefinition(
                    ReadString(transition["from"]),
                    ReadString(transition["to"])),
                ReadArray(properties["sources"]).Select(ParseSource));
            Validate(definition);

            if (!utf8Json.SequenceEqual(SerializeCanonical(definition)))
            {
                throw new JsonException("The Reserve Designation artifact is not canonical JSON.");
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
            throw new JsonException("The Reserve Designation artifact is invalid.", exception);
        }
    }

    internal static void Validate(ReserveRulesArtifactDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        Require(definition.SchemaVersion == Cna1979Reserve.SchemaVersion);
        Require(string.Equals(
            definition.EligibleOwner,
            Cna1979Reserve.EligibleOwner,
            StringComparison.Ordinal));
        Require(string.Equals(
            definition.AssignmentTiming,
            Cna1979Reserve.AssignmentTiming,
            StringComparison.Ordinal));
        Require(string.Equals(
            definition.AssignmentResult,
            Cna1979Reserve.AssignmentResult,
            StringComparison.Ordinal));
        Require(definition.CapabilityPointCost == Cna1979Reserve.CapabilityPointCost);
        Require(string.Equals(
            definition.SupportedTransition.From,
            Cna1979Reserve.TransitionFrom,
            StringComparison.Ordinal));
        Require(string.Equals(
            definition.SupportedTransition.To,
            Cna1979Reserve.TransitionTo,
            StringComparison.Ordinal));
        Require(definition.Sources.SequenceEqual(Cna1979Reserve.SourceReferences));
    }

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
            throw new JsonException("Expected a Reserve Designation artifact object.");
        }

        var actual = element.EnumerateObject().ToArray();
        if (!actual.Select(value => value.Name).SequenceEqual(expectedProperties))
        {
            throw new JsonException(
                "Reserve Designation artifact properties are missing, extra, or reordered.");
        }

        return actual.ToDictionary(value => value.Name, value => value.Value, StringComparer.Ordinal);
    }

    private static JsonElement.ArrayEnumerator ReadArray(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Expected a Reserve Designation artifact array.");
        }

        return element.EnumerateArray();
    }

    private static string ReadString(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("Expected a Reserve Designation artifact string.");
        }

        return element.GetString()!;
    }

    private static int ReadInteger(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value))
        {
            throw new JsonException("Expected a Reserve Designation artifact 32-bit integer.");
        }

        return value;
    }

    private static void Require(bool condition)
    {
        if (!condition)
        {
            throw new JsonException("The Reserve Designation artifact authority is unsupported.");
        }
    }
}
