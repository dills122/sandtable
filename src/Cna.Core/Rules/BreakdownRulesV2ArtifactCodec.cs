using System.Text.Json;

namespace Cna.Core.Rules;

/// <summary>Strict dormant schema 2; historical schema 1 remains unchanged.</summary>
internal static class BreakdownRulesV2ArtifactCodec
{
    public static byte[] SerializeCanonical(BreakdownRulesV2ArtifactDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var bytes = SerializeUnchecked(definition);
        RequireAuthority(bytes);
        return bytes;
    }

    public static BreakdownRulesV2ArtifactDefinition Deserialize(ReadOnlySpan<byte> utf8Json)
    {
        // This rules artifact admits one closed, source-qualified authority definition.
        // Exact canonical comparison also rejects duplicates, unknown fields, whitespace,
        // invalid scalar encodings and correctly rehashed but altered outcome semantics.
        RequireAuthority(utf8Json);
        return Cna1979BreakdownAdjudication.Definition;
    }

    private static void RequireAuthority(ReadOnlySpan<byte> bytes)
    {
        if (!bytes.SequenceEqual(SerializeUnchecked(Cna1979BreakdownAdjudication.Definition)))
        {
            throw new JsonException("The Breakdown schema 2 artifact is not canonical authority.");
        }
    }

    private static byte[] SerializeUnchecked(BreakdownRulesV2ArtifactDefinition definition)
    {
        using var predecessor = JsonDocument.Parse(
            BreakdownRulesArtifactCodec.SerializeCanonical(definition.Continuity));
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            foreach (var property in predecessor.RootElement.EnumerateObject())
            {
                if (property.NameEquals("schemaVersion"))
                {
                    writer.WriteNumber("schemaVersion", definition.SchemaVersion);
                }
                else if (property.NameEquals("sources"))
                {
                    WriteOutcomes(writer, definition);
                    WriteSources(writer, definition.Sources);
                }
                else
                {
                    property.WriteTo(writer);
                }
            }
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static void WriteOutcomes(Utf8JsonWriter writer, BreakdownRulesV2ArtifactDefinition definition)
    {
        writer.WriteStartArray("outcomeFractions");
        foreach (var value in definition.OutcomeFractions)
        {
            writer.WriteStartObject();
            writer.WriteNumber("printedLabel", value.PrintedLabel);
            writer.WritePropertyName("fraction");
            BreakdownPointAmountCodec.WriteCanonical(writer, value.Fraction);
            writer.WriteString("rulingId", value.RulingId);
            WriteSources(writer, value.Sources);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteStartArray("outcomeCells");
        foreach (var value in definition.OutcomeCells)
        {
            writer.WriteStartObject();
            writer.WriteString("bandId", value.BandId);
            writer.WriteNumber("coordinate", value.Coordinate);
            writer.WriteNumber("printedLabel", value.PrintedLabel);
            WriteSources(writer, value.Sources);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteSources(Utf8JsonWriter writer, IReadOnlyList<RuleReference> sources)
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
}
