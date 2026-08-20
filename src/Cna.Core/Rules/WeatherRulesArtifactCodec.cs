using System.Text.Json;

namespace Cna.Core.Rules;

internal static class WeatherRulesArtifactCodec
{
    public static byte[] SerializeCanonical(WeatherRulesArtifactDefinition definition)
    {
        WeatherRulesArtifactValidator.Validate(definition);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", definition.SchemaVersion);
            writer.WriteStartObject("provenance");
            WriteSources(writer, "gameTurnRanges", definition.Provenance.GameTurnRanges);
            WriteSources(writer, "outcomes", definition.Provenance.Outcomes);
            WriteSources(
                writer,
                "foulWeatherLocations",
                definition.Provenance.FoulWeatherLocations);
            writer.WriteEndObject();
            writer.WriteStartArray("seasons");

            foreach (var season in definition.Seasons)
            {
                writer.WriteStartObject();
                writer.WriteString("season", FormatSeason(season.Season));
                writer.WriteStartArray("gameTurnRanges");
                foreach (var range in season.GameTurnRanges)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("first", range.First);
                    writer.WriteNumber("last", range.Last);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteStartArray("outcomes");
                foreach (var outcome in season.Outcomes)
                {
                    writer.WriteStartObject();
                    writer.WriteString("kind", FormatKind(outcome.Kind));
                    writer.WriteNumber("firstD66", outcome.FirstD66);
                    writer.WriteNumber("lastD66", outcome.LastD66);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteStartArray("foulWeatherLocations");
            foreach (var location in definition.FoulWeatherLocations)
            {
                writer.WriteStartObject();
                writer.WriteNumber("die", location.Die);
                writer.WriteStartArray("areas");
                foreach (var area in location.Areas)
                {
                    writer.WriteStringValue(FormatArea(area));
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("deferredRules");
            foreach (var deferredRule in definition.DeferredRules)
            {
                writer.WriteStartObject();
                writer.WriteString("ruleId", deferredRule.RuleId);
                writer.WriteString("weatherKind", FormatKind(deferredRule.WeatherKind));
                writer.WriteString("area", FormatArea(deferredRule.Area));
                writer.WriteString("status", deferredRule.Status);
                WriteSources(writer, "sources", deferredRule.Sources);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            WriteSources(writer, "sources", definition.Sources);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static WeatherRulesArtifactDefinition Deserialize(ReadOnlySpan<byte> utf8Json)
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
            var definition = ParseDefinition(document.RootElement);
            WeatherRulesArtifactValidator.Validate(definition);

            if (!utf8Json.SequenceEqual(SerializeCanonical(definition)))
            {
                throw new JsonException("The Weather artifact is not canonical JSON.");
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
            throw new JsonException("The Weather artifact is invalid.", exception);
        }
    }

    private static WeatherRulesArtifactDefinition ParseDefinition(JsonElement element)
    {
        var properties = ReadObject(
            element,
            "schemaVersion",
            "provenance",
            "seasons",
            "foulWeatherLocations",
            "deferredRules",
            "sources");
        return new WeatherRulesArtifactDefinition(
            ReadInteger(properties["schemaVersion"]),
            ParseProvenance(properties["provenance"]),
            ReadArray(properties["seasons"]).Select(ParseSeason),
            ReadArray(properties["foulWeatherLocations"]).Select(ParseFoulLocation),
            ReadArray(properties["deferredRules"]).Select(ParseDeferredRule),
            ReadArray(properties["sources"]).Select(ParseSource));
    }

    private static WeatherArtifactProvenance ParseProvenance(JsonElement element)
    {
        var properties = ReadObject(
            element,
            "gameTurnRanges",
            "outcomes",
            "foulWeatherLocations");
        return new WeatherArtifactProvenance(
            ReadArray(properties["gameTurnRanges"]).Select(ParseSource),
            ReadArray(properties["outcomes"]).Select(ParseSource),
            ReadArray(properties["foulWeatherLocations"]).Select(ParseSource));
    }

    private static WeatherTableDefinition ParseSeason(JsonElement element)
    {
        var properties = ReadObject(element, "season", "gameTurnRanges", "outcomes");
        return new WeatherTableDefinition(
            ParseSeasonToken(ReadString(properties["season"])),
            ReadArray(properties["gameTurnRanges"]).Select(ParseGameTurnRange),
            ReadArray(properties["outcomes"]).Select(ParseOutcome));
    }

    private static GameTurnRange ParseGameTurnRange(JsonElement element)
    {
        var properties = ReadObject(element, "first", "last");
        return new GameTurnRange(
            ReadInteger(properties["first"]),
            ReadInteger(properties["last"]));
    }

    private static WeatherD66OutcomeDefinition ParseOutcome(JsonElement element)
    {
        var properties = ReadObject(element, "kind", "firstD66", "lastD66");
        return new WeatherD66OutcomeDefinition(
            ParseKind(ReadString(properties["kind"])),
            ReadInteger(properties["firstD66"]),
            ReadInteger(properties["lastD66"]));
    }

    private static FoulWeatherLocationDefinition ParseFoulLocation(JsonElement element)
    {
        var properties = ReadObject(element, "die", "areas");
        return new FoulWeatherLocationDefinition(
            ReadInteger(properties["die"]),
            ReadArray(properties["areas"]).Select(value => ParseArea(ReadString(value))));
    }

    private static DeferredWeatherRuleDefinition ParseDeferredRule(JsonElement element)
    {
        var properties = ReadObject(
            element,
            "ruleId",
            "weatherKind",
            "area",
            "status",
            "sources");
        return new DeferredWeatherRuleDefinition(
            ReadString(properties["ruleId"]),
            ParseKind(ReadString(properties["weatherKind"])),
            ParseArea(ReadString(properties["area"])),
            ReadString(properties["status"]),
            ReadArray(properties["sources"]).Select(ParseSource));
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
            throw new JsonException("Expected a Weather artifact object.");
        }

        var actual = element.EnumerateObject().ToArray();
        if (!actual.Select(value => value.Name).SequenceEqual(expectedProperties))
        {
            throw new JsonException("Weather artifact properties are missing, extra, or reordered.");
        }

        return actual.ToDictionary(value => value.Name, value => value.Value, StringComparer.Ordinal);
    }

    private static JsonElement.ArrayEnumerator ReadArray(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Expected a Weather artifact array.");
        }

        return element.EnumerateArray();
    }

    private static string ReadString(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new JsonException("Expected a Weather artifact string.");
        }

        return element.GetString()!;
    }

    private static int ReadInteger(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out var value))
        {
            throw new JsonException("Expected a Weather artifact 32-bit integer.");
        }

        return value;
    }

    private static void WriteSources(
        Utf8JsonWriter writer,
        string propertyName,
        IEnumerable<RuleReference> sources)
    {
        writer.WriteStartArray(propertyName);
        foreach (var source in sources)
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", source.SourceId);
            writer.WriteString("locator", source.Locator);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static string FormatSeason(WeatherSeason season) => season switch
    {
        WeatherSeason.Fall => "fall",
        WeatherSeason.Winter => "winter",
        WeatherSeason.Spring => "spring",
        WeatherSeason.Summer => "summer",
        _ => throw new ArgumentOutOfRangeException(nameof(season)),
    };

    private static string FormatKind(WeatherKind kind) => kind switch
    {
        WeatherKind.Normal => "normal",
        WeatherKind.Hot => "hot",
        WeatherKind.Sandstorm => "sandstorm",
        WeatherKind.Rainstorm => "rainstorm",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private static string FormatArea(WeatherArea area) => area switch
    {
        WeatherArea.A => "a",
        WeatherArea.B => "b",
        WeatherArea.C => "c",
        WeatherArea.D => "d",
        WeatherArea.E => "e",
        _ => throw new ArgumentOutOfRangeException(nameof(area)),
    };

    private static WeatherSeason ParseSeasonToken(string value) => value switch
    {
        "fall" => WeatherSeason.Fall,
        "winter" => WeatherSeason.Winter,
        "spring" => WeatherSeason.Spring,
        "summer" => WeatherSeason.Summer,
        _ => throw new JsonException($"Unknown Weather season '{value}'."),
    };

    private static WeatherKind ParseKind(string value) => value switch
    {
        "normal" => WeatherKind.Normal,
        "hot" => WeatherKind.Hot,
        "sandstorm" => WeatherKind.Sandstorm,
        "rainstorm" => WeatherKind.Rainstorm,
        _ => throw new JsonException($"Unknown Weather kind '{value}'."),
    };

    private static WeatherArea ParseArea(string value) => value switch
    {
        "a" => WeatherArea.A,
        "b" => WeatherArea.B,
        "c" => WeatherArea.C,
        "d" => WeatherArea.D,
        "e" => WeatherArea.E,
        _ => throw new JsonException($"Unknown Weather area '{value}'."),
    };
}
