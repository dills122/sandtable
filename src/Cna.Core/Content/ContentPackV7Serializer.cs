using System.Globalization;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Content;

public static class ContentPackV7Serializer
{
    private const int MaximumByteCount = 65_536;

    public static byte[] SerializeCanonical(ContentPackV7Definition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var issues = ContentPackV7Validator.Validate(definition).Issues.ToArray();
        if (issues.Length > 0)
            throw new InvalidContentPackException(issues);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schemaVersion", ContentPackV7Definition.SchemaVersion);
            writer.WriteString("formatId", ContentPackV7Definition.CanonicalFormatId);
            writer.WriteString("packId", definition.PackId);
            writer.WriteString("rulesetId", definition.RulesetId);
            writer.WriteStartArray("capabilities");
            foreach (var capability in definition.Capabilities)
                writer.WriteStringValue(capability);
            writer.WriteEndArray();
            writer.WriteString("capabilityProfileId", definition.CapabilityProfileId);
            WriteSources(writer, definition.SourceIndex);
            WriteLocations(writer, definition.Locations);
            WriteWeather(writer, definition.WeatherAreaAssignments);
            WriteEdges(writer, definition.Edges);
            WriteFormations(writer, definition.Formations);
            WriteElements(writer, definition.Elements);
            WriteScenarios(writer, definition.Scenarios);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static ContentPackV7ParseResult Deserialize(ReadOnlySpan<byte> utf8Json)
    {
        if (utf8Json.Length > MaximumByteCount
            || (utf8Json.Length >= 3
                && utf8Json[0] == 0xef
                && utf8Json[1] == 0xbb
                && utf8Json[2] == 0xbf))
        {
            return ContentPackV7ParseResult.Failure(
                CombatContentDiagnostics.Shape,
                string.Empty,
                "Content7 input exceeds bounds or contains a byte-order mark.");
        }

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
            var canonical = SerializeCanonical(definition);
            if (!utf8Json.SequenceEqual(canonical))
            {
                return ContentPackV7ParseResult.Failure(
                    CombatContentDiagnostics.Canonical,
                    string.Empty,
                    "Content7 input must be byte-identical canonical JSON.");
            }

            return ContentPackV7ParseResult.Success(definition);
        }
        catch (ContentPackV7ParseException exception)
        {
            return ContentPackV7ParseResult.Failure(
                exception.Code,
                exception.Path,
                exception.Message);
        }
        catch (JsonException exception)
        {
            return ContentPackV7ParseResult.Failure(
                CombatContentDiagnostics.Shape,
                string.Empty,
                exception.Message);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            return ContentPackV7ParseResult.Failure(
                CombatContentDiagnostics.Shape,
                string.Empty,
                exception.Message);
        }
    }

    private static ContentPackV7Definition ParseDefinition(JsonElement element)
    {
        var properties = ReadObject(element, string.Empty,
            "schemaVersion", "formatId", "packId", "rulesetId", "capabilities",
            "capabilityProfileId", "sourceIndex", "locations", "weatherAreaAssignments",
            "edges", "formations", "elements", "scenarios");
        var schemaVersion = ReadInt(properties["schemaVersion"], "/schemaVersion", int.MinValue, int.MaxValue);
        var formatId = ReadId(properties["formatId"], "/formatId");
        var packId = ReadId(properties["packId"], "/packId");
        var rulesetId = ReadId(properties["rulesetId"], "/rulesetId");
        var capabilities = ReadArray(properties["capabilities"], "/capabilities")
            .Select((value, index) => ReadId(value, $"/capabilities/{index}"))
            .ToArray();
        var profileId = ReadId(properties["capabilityProfileId"], "/capabilityProfileId");

        if (schemaVersion != ContentPackV7Definition.SchemaVersion)
            Fail(CombatContentDiagnostics.Identity, "/schemaVersion", "Unsupported Content schema.");
        if (formatId != ContentPackV7Definition.CanonicalFormatId)
            Fail(CombatContentDiagnostics.Identity, "/formatId", "Unsupported Content format.");
        if (rulesetId != ContentPackV7Definition.SupportedRulesetId)
            Fail(CombatContentDiagnostics.Identity, "/rulesetId", "Unsupported ruleset.");
        if (profileId != ContentPackV7Definition.SupportedCapabilityProfileId)
            Fail(CombatContentDiagnostics.Identity, "/capabilityProfileId", "Unsupported capability profile.");
        if (!capabilities.Order(StringComparer.Ordinal).SequenceEqual(ContentPackV7Definition.RequiredCapabilities))
            Fail(CombatContentDiagnostics.Identity, "/capabilities", "Exact Content7 capabilities are required.");

        var sourceValues = ReadArray(properties["sourceIndex"], "/sourceIndex");
        var locationValues = ReadArray(properties["locations"], "/locations");
        var weatherValues = ReadArray(properties["weatherAreaAssignments"], "/weatherAreaAssignments");
        var edgeValues = ReadArray(properties["edges"], "/edges");
        var formationValues = ReadArray(properties["formations"], "/formations");
        var elementValues = ReadArray(properties["elements"], "/elements");
        var scenarioValues = ReadArray(properties["scenarios"], "/scenarios");
        EnsureUniqueProperty(sourceValues, "sourceId", "/sourceIndex");
        EnsureUniqueProperty(locationValues, "locationId", "/locations");
        EnsureUniqueProperty(weatherValues, "locationId", "/weatherAreaAssignments");
        EnsureUniquePair(edgeValues, "firstLocationId", "secondLocationId", "/edges");
        EnsureUniqueProperty(formationValues, "formationId", "/formations");
        EnsureUniqueProperty(elementValues, "elementId", "/elements");
        EnsureUniqueProperty(scenarioValues, "scenarioId", "/scenarios");

        var definition = new ContentPackV7Definition(
            packId,
            rulesetId,
            capabilities,
            profileId,
            sourceValues
                .Select((value, index) => ParseSource(value, $"/sourceIndex/{index}")),
            locationValues
                .Select((value, index) => ParseLocation(value, $"/locations/{index}")),
            weatherValues
                .Select((value, index) => ParseWeather(value, $"/weatherAreaAssignments/{index}")),
            edgeValues
                .Select((value, index) => ParseEdge(value, $"/edges/{index}")),
            formationValues
                .Select((value, index) => ParseFormation(value, $"/formations/{index}")),
            elementValues
                .Select((value, index) => ParseElement(value, $"/elements/{index}")),
            scenarioValues
                .Select((value, index) => ParseScenario(value, $"/scenarios/{index}")));
        var validation = ContentPackV7Validator.Validate(definition);
        var issue = validation.Issues.Count == 0 ? null : validation.Issues[0];
        if (issue is not null)
            Fail(issue.Code, issue.Path, issue.Message);
        return definition;
    }

    private static ContentSourceIndexEntry ParseSource(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "sourceId", "kind");
        var sourceId = ReadId(properties["sourceId"], path + "/sourceId");
        var kind = ReadString(properties["kind"], path + "/kind");
        if (kind != "repository-synthetic")
            Fail(CombatContentDiagnostics.Provenance, path + "/kind", "Unsupported source kind.");
        return new ContentSourceIndexEntry(sourceId, ContentSourceKind.RepositorySynthetic);
    }

    private static ContentHex ParseLocation(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "locationId", "kind", "terrainId", "sourceCoordinate", "origin");
        RequireNull(properties["sourceCoordinate"], path + "/sourceCoordinate");
        if (ReadString(properties["kind"], path + "/kind") != "hex")
            Fail(CombatContentDiagnostics.Profile, path + "/kind", "Only hex locations are supported.");
        return new ContentHex(
            ReadId(properties["locationId"], path + "/locationId"),
            ReadId(properties["terrainId"], path + "/terrainId"),
            null,
            ParseOrigin(properties["origin"], path + "/origin"));
    }

    private static ContentWeatherAreaAssignment ParseWeather(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "locationId", "weatherArea", "origin");
        var area = ReadString(properties["weatherArea"], path + "/weatherArea");
        if (area != "a")
            Fail(CombatContentDiagnostics.Profile, path + "/weatherArea", "Only Weather area a is supported.");
        return new ContentWeatherAreaAssignment(
            ReadId(properties["locationId"], path + "/locationId"),
            ContentWeatherArea.A,
            ParseOrigin(properties["origin"], path + "/origin"));
    }

    private static ContentHexEdge ParseEdge(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "firstLocationId", "secondLocationId", "features", "origin");
        var features = ReadArray(properties["features"], path + "/features");
        if (features.Length != 0)
            Fail(CombatContentDiagnostics.Profile, path + "/features", "Selected Combat edges are featureless.");
        var first = ReadId(properties["firstLocationId"], path + "/firstLocationId");
        var second = ReadId(properties["secondLocationId"], path + "/secondLocationId");
        if (StringComparer.Ordinal.Compare(first, second) >= 0)
            Fail(CombatContentDiagnostics.Profile, path, "Edge endpoints must be distinct and canonical.");
        return new ContentHexEdge(first, second, [], ParseOrigin(properties["origin"], path + "/origin"));
    }

    private static ContentFormationMorale ParseFormation(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "formationId", "sideId", "parentFormationId",
            "organizationId", "basicMorale", "basicMoraleOrigin", "origin");
        RequireNull(properties["parentFormationId"], path + "/parentFormationId");
        return new ContentFormationMorale(
            ReadId(properties["formationId"], path + "/formationId"),
            ReadId(properties["sideId"], path + "/sideId"),
            null,
            ReadId(properties["organizationId"], path + "/organizationId"),
            ReadInt(properties["basicMorale"], path + "/basicMorale", -3, 3),
            ParseOrigin(properties["basicMoraleOrigin"], path + "/basicMoraleOrigin"),
            ParseOrigin(properties["origin"], path + "/origin"));
    }

    private static ContentElementCombatFactsV2 ParseElement(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "elementId", "sideId", "parentFormationId",
            "organizationId", "mobilityId", "baseCapabilityPointAllowance", "placementMode",
            "combatClassificationId", "combatOrigin", "components", "breakdownVehicleCohort", "origin");
        RequireNull(properties["breakdownVehicleCohort"], path + "/breakdownVehicleCohort");
        var mode = ReadString(properties["placementMode"], path + "/placementMode");
        if (mode != "independent")
            Fail(CombatContentDiagnostics.Profile, path + "/placementMode", "Only independent elements are supported.");
        var componentValues = ReadArray(properties["components"], path + "/components");
        EnsureUniqueProperty(componentValues, "componentId", path + "/components");
        return new ContentElementCombatFactsV2(
            ReadId(properties["elementId"], path + "/elementId"),
            ReadId(properties["sideId"], path + "/sideId"),
            ReadId(properties["parentFormationId"], path + "/parentFormationId"),
            ReadId(properties["organizationId"], path + "/organizationId"),
            ReadId(properties["mobilityId"], path + "/mobilityId"),
            ReadInt(properties["baseCapabilityPointAllowance"], path + "/baseCapabilityPointAllowance", 1, int.MaxValue),
            ContentPlacementMode.Independent,
            ReadId(properties["combatClassificationId"], path + "/combatClassificationId"),
            ParseOrigin(properties["combatOrigin"], path + "/combatOrigin"),
            componentValues
                .Select((value, index) => ParseComponent(value, $"{path}/components/{index}")),
            ParseOrigin(properties["origin"], path + "/origin"));
    }

    private static ContentCombatComponentV2 ParseComponent(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "componentId", "componentClassId", "maximumToe",
            "offensiveCloseAssaultRating", "defensiveCloseAssaultRating", "origin");
        return new ContentCombatComponentV2(
            ReadId(properties["componentId"], path + "/componentId"),
            ReadId(properties["componentClassId"], path + "/componentClassId"),
            ReadInt(properties["maximumToe"], path + "/maximumToe", 1, int.MaxValue),
            ReadInt(properties["offensiveCloseAssaultRating"], path + "/offensiveCloseAssaultRating", 0, int.MaxValue),
            ReadInt(properties["defensiveCloseAssaultRating"], path + "/defensiveCloseAssaultRating", 0, int.MaxValue),
            ParseOrigin(properties["origin"], path + "/origin"));
    }

    private static ContentCombatScenario ParseScenario(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "scenarioId", "start", "end", "initialPlacements",
            "retreatSupplyAnchors", "origin");
        var placementValues = ReadArray(properties["initialPlacements"], path + "/initialPlacements");
        var anchorValues = ReadArray(properties["retreatSupplyAnchors"], path + "/retreatSupplyAnchors");
        EnsureUniqueProperty(placementValues, "elementId", path + "/initialPlacements");
        EnsureUniqueProperty(anchorValues, "sideId", path + "/retreatSupplyAnchors");
        return new ContentCombatScenario(
            ReadId(properties["scenarioId"], path + "/scenarioId"),
            ParseBoundary(properties["start"], path + "/start"),
            ParseBoundary(properties["end"], path + "/end"),
            placementValues
                .Select((value, index) => ParsePlacement(value, $"{path}/initialPlacements/{index}")),
            anchorValues
                .Select((value, index) => ParseAnchor(value, $"{path}/retreatSupplyAnchors/{index}")),
            ParseOrigin(properties["origin"], path + "/origin"));
    }

    private static ContentScenarioBoundary ParseBoundary(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "gameTurn", "operationStage");
        return new ContentScenarioBoundary(
            ReadInt(properties["gameTurn"], path + "/gameTurn", 1, 111),
            ReadInt(properties["operationStage"], path + "/operationStage", 1, 3));
    }

    private static ContentInitialPlacementCombatFactsV2 ParsePlacement(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "elementId", "locationId", "initialComponentToes",
            "initialAmmunition", "initialReadiness", "origin");
        var toeValues = ReadArray(properties["initialComponentToes"], path + "/initialComponentToes");
        EnsureUniqueProperty(toeValues, "componentId", path + "/initialComponentToes");
        return new ContentInitialPlacementCombatFactsV2(
            ReadId(properties["elementId"], path + "/elementId"),
            ReadId(properties["locationId"], path + "/locationId"),
            toeValues
                .Select((value, index) => ParseToe(value, $"{path}/initialComponentToes/{index}")),
            ParseAmmunition(properties["initialAmmunition"], path + "/initialAmmunition"),
            ParseReadiness(properties["initialReadiness"], path + "/initialReadiness"),
            ParseOrigin(properties["origin"], path + "/origin"));
    }

    private static ContentInitialComponentToe ParseToe(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "componentId", "currentToe", "origin");
        return new ContentInitialComponentToe(
            ReadId(properties["componentId"], path + "/componentId"),
            ReadInt(properties["currentToe"], path + "/currentToe", 0, int.MaxValue),
            ParseOrigin(properties["origin"], path + "/origin"));
    }

    private static ContentInitialAmmunition ParseAmmunition(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "points", "origin");
        return new ContentInitialAmmunition(
            ReadInt(properties["points"], path + "/points", 0, int.MaxValue),
            ParseOrigin(properties["origin"], path + "/origin"));
    }

    private static ContentInitialCombatReadiness ParseReadiness(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "gameTurn", "operationStage", "waterStatus",
            "storesStatus", "pinned", "origin");
        return new ContentInitialCombatReadiness(
            ReadInt(properties["gameTurn"], path + "/gameTurn", 1, 111),
            ReadInt(properties["operationStage"], path + "/operationStage", 1, 3),
            ReadString(properties["waterStatus"], path + "/waterStatus"),
            ReadString(properties["storesStatus"], path + "/storesStatus"),
            ReadBoolean(properties["pinned"], path + "/pinned"),
            ParseOrigin(properties["origin"], path + "/origin"));
    }

    private static ContentRetreatSupplyAnchor ParseAnchor(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "sideId", "locationId", "kind", "origin");
        return new ContentRetreatSupplyAnchor(
            ReadId(properties["sideId"], path + "/sideId"),
            ReadId(properties["locationId"], path + "/locationId"),
            ReadString(properties["kind"], path + "/kind"),
            ParseOrigin(properties["origin"], path + "/origin"));
    }

    private static ContentOrigin ParseOrigin(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "kind", "references");
        if (ReadString(properties["kind"], path + "/kind") != "synthetic")
            Fail(CombatContentDiagnostics.Provenance, path + "/kind", "Only synthetic origins are supported.");
        var references = ReadArray(properties["references"], path + "/references")
            .Select((value, index) => ParseReference(value, $"{path}/references/{index}"))
            .ToArray();
        if (references.Length != 1)
            Fail(CombatContentDiagnostics.Provenance, path + "/references", "Exactly one source reference is required.");
        return new ContentOrigin(ContentOriginKind.Synthetic, references);
    }

    private static RuleReference ParseReference(JsonElement element, string path)
    {
        var properties = ReadObject(element, path, "sourceId", "locator");
        return new RuleReference(
            ReadId(properties["sourceId"], path + "/sourceId"),
            ReadLocator(properties["locator"], path + "/locator"));
    }

    private static Dictionary<string, JsonElement> ReadObject(JsonElement element, string path, params string[] required)
    {
        if (element.ValueKind != JsonValueKind.Object)
            Fail(CombatContentDiagnostics.Shape, path, "Expected an object.");
        var result = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!result.TryAdd(property.Name, property.Value))
                Fail(CombatContentDiagnostics.Shape, string.Empty, "Duplicate JSON properties are not allowed.");
        }
        foreach (var name in required)
        {
            if (!result.ContainsKey(name))
                Fail(CombatContentDiagnostics.Shape, path + "/" + name, "Required property is missing.");
        }
        if (result.Count != required.Length || result.Keys.Any(name => !required.Contains(name, StringComparer.Ordinal)))
            Fail(CombatContentDiagnostics.Shape, path, "Unknown properties are not allowed.");
        return result;
    }

    private static JsonElement[] ReadArray(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.Array)
            Fail(CombatContentDiagnostics.Shape, path, "Expected an array.");
        return element.EnumerateArray().ToArray();
    }

    private static void EnsureUniqueProperty(JsonElement[] values, string propertyName, string path)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < values.Length; index++)
        {
            if (values[index].ValueKind != JsonValueKind.Object)
                continue;
            var properties = values[index].EnumerateObject().ToArray();
            var matching = properties.Where(value => value.NameEquals(propertyName)).ToArray();
            if (matching.Length != 1 || matching[0].Value.ValueKind != JsonValueKind.String)
                continue;
            if (!seen.Add(matching[0].Value.GetString()!))
                Fail(CombatContentDiagnostics.Reference, $"{path}/{index}/{propertyName}", "Duplicate identity.");
        }
    }

    private static void EnsureUniquePair(
        JsonElement[] values,
        string firstProperty,
        string secondProperty,
        string path)
    {
        var seen = new HashSet<(string, string)>();
        for (var index = 0; index < values.Length; index++)
        {
            if (values[index].ValueKind != JsonValueKind.Object
                || !values[index].TryGetProperty(firstProperty, out var first)
                || !values[index].TryGetProperty(secondProperty, out var second)
                || first.ValueKind != JsonValueKind.String
                || second.ValueKind != JsonValueKind.String)
            {
                continue;
            }
            var left = first.GetString()!;
            var right = second.GetString()!;
            var pair = StringComparer.Ordinal.Compare(left, right) <= 0 ? (left, right) : (right, left);
            if (!seen.Add(pair))
                Fail(CombatContentDiagnostics.Reference, $"{path}/{index}", "Duplicate edge.");
        }
    }

    private static string ReadString(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.String)
            Fail(CombatContentDiagnostics.Shape, path, "Expected a string.");
        return element.GetString()!;
    }

    private static string ReadId(JsonElement element, string path)
    {
        var value = ReadString(element, path);
        if (value.Length is < 1 or > 128)
            Fail(CombatContentDiagnostics.Bounds, path, "Stable ID is outside bounds.");
        try
        {
            ContentContractGuards.RequireStableId(value, path);
        }
        catch (ArgumentException)
        {
            Fail(CombatContentDiagnostics.Bounds, path, "Invalid stable ID.");
        }
        return value;
    }

    private static string ReadLocator(JsonElement element, string path)
    {
        var value = ReadString(element, path);
        try
        {
            ContentContractGuards.RequireSourceAtom(value, path);
        }
        catch (ArgumentException)
        {
            Fail(CombatContentDiagnostics.Bounds, path, "Invalid source locator.");
        }
        return value;
    }

    private static int ReadInt(JsonElement element, string path, int minimum, int maximum)
    {
        if (element.ValueKind != JsonValueKind.Number)
            Fail(CombatContentDiagnostics.Shape, path, "Expected an integer.");
        var raw = element.GetRawText();
        if (raw.ContainsAny('.', 'e', 'E'))
            Fail(CombatContentDiagnostics.Shape, path, "Integer must use plain decimal notation.");
        if (!long.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var value)
            || value < minimum
            || value > maximum)
        {
            Fail(CombatContentDiagnostics.Bounds, path, "Integer is outside supported bounds.");
        }
        return (int)value;
    }

    private static bool ReadBoolean(JsonElement element, string path)
    {
        if (element.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            Fail(CombatContentDiagnostics.Shape, path, "Expected a boolean.");
        return element.GetBoolean();
    }

    private static void RequireNull(JsonElement element, string path)
    {
        if (element.ValueKind != JsonValueKind.Null)
            Fail(CombatContentDiagnostics.Shape, path, "Expected null.");
    }

    private static void WriteSources(Utf8JsonWriter writer, IReadOnlyList<ContentSourceIndexEntry> values)
    {
        writer.WriteStartArray("sourceIndex");
        foreach (var value in values)
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", value.SourceId);
            writer.WriteString("kind", "repository-synthetic");
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteLocations(Utf8JsonWriter writer, IReadOnlyList<ContentHex> values)
    {
        writer.WriteStartArray("locations");
        foreach (var value in values)
        {
            writer.WriteStartObject();
            writer.WriteString("locationId", value.LocationId);
            writer.WriteString("kind", "hex");
            writer.WriteString("terrainId", value.TerrainId);
            writer.WriteNull("sourceCoordinate");
            WriteOrigin(writer, "origin", value.Origin);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteWeather(Utf8JsonWriter writer, IReadOnlyList<ContentWeatherAreaAssignment> values)
    {
        writer.WriteStartArray("weatherAreaAssignments");
        foreach (var value in values)
        {
            writer.WriteStartObject();
            writer.WriteString("locationId", value.LocationId);
            writer.WriteString("weatherArea", value.WeatherArea.ToString().ToLowerInvariant());
            WriteOrigin(writer, "origin", value.Origin);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteEdges(Utf8JsonWriter writer, IReadOnlyList<ContentHexEdge> values)
    {
        writer.WriteStartArray("edges");
        foreach (var value in values)
        {
            writer.WriteStartObject();
            writer.WriteString("firstLocationId", value.FirstLocationId);
            writer.WriteString("secondLocationId", value.SecondLocationId);
            writer.WriteStartArray("features");
            writer.WriteEndArray();
            WriteOrigin(writer, "origin", value.Origin);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteFormations(Utf8JsonWriter writer, IReadOnlyList<ContentFormationMorale> values)
    {
        writer.WriteStartArray("formations");
        foreach (var value in values)
        {
            writer.WriteStartObject();
            writer.WriteString("formationId", value.FormationId);
            writer.WriteString("sideId", value.SideId);
            writer.WriteNull("parentFormationId");
            writer.WriteString("organizationId", value.OrganizationId);
            writer.WriteNumber("basicMorale", value.BasicMorale);
            WriteOrigin(writer, "basicMoraleOrigin", value.BasicMoraleOrigin);
            WriteOrigin(writer, "origin", value.Origin);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteElements(Utf8JsonWriter writer, IReadOnlyList<ContentElementCombatFactsV2> values)
    {
        writer.WriteStartArray("elements");
        foreach (var value in values)
        {
            writer.WriteStartObject();
            writer.WriteString("elementId", value.ElementId);
            writer.WriteString("sideId", value.SideId);
            writer.WriteString("parentFormationId", value.ParentFormationId);
            writer.WriteString("organizationId", value.OrganizationId);
            writer.WriteString("mobilityId", value.MobilityId);
            writer.WriteNumber("baseCapabilityPointAllowance", value.BaseCapabilityPointAllowance);
            writer.WriteString("placementMode", "independent");
            writer.WriteString("combatClassificationId", value.CombatClassificationId);
            WriteOrigin(writer, "combatOrigin", value.CombatOrigin);
            writer.WriteStartArray("components");
            foreach (var component in value.Components)
            {
                writer.WriteStartObject();
                writer.WriteString("componentId", component.ComponentId);
                writer.WriteString("componentClassId", component.ComponentClassId);
                writer.WriteNumber("maximumToe", component.MaximumToe);
                writer.WriteNumber("offensiveCloseAssaultRating", component.OffensiveCloseAssaultRating);
                writer.WriteNumber("defensiveCloseAssaultRating", component.DefensiveCloseAssaultRating);
                WriteOrigin(writer, "origin", component.Origin);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteNull("breakdownVehicleCohort");
            WriteOrigin(writer, "origin", value.Origin);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteScenarios(Utf8JsonWriter writer, IReadOnlyList<ContentCombatScenario> values)
    {
        writer.WriteStartArray("scenarios");
        foreach (var value in values)
        {
            writer.WriteStartObject();
            writer.WriteString("scenarioId", value.ScenarioId);
            WriteBoundary(writer, "start", value.Start);
            WriteBoundary(writer, "end", value.End);
            writer.WriteStartArray("initialPlacements");
            foreach (var placement in value.InitialPlacements)
            {
                writer.WriteStartObject();
                writer.WriteString("elementId", placement.ElementId);
                writer.WriteString("locationId", placement.LocationId);
                writer.WriteStartArray("initialComponentToes");
                foreach (var toe in placement.InitialComponentToes)
                {
                    writer.WriteStartObject();
                    writer.WriteString("componentId", toe.ComponentId);
                    writer.WriteNumber("currentToe", toe.CurrentToe);
                    WriteOrigin(writer, "origin", toe.Origin);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteStartObject("initialAmmunition");
                writer.WriteNumber("points", placement.InitialAmmunition.Points);
                WriteOrigin(writer, "origin", placement.InitialAmmunition.Origin);
                writer.WriteEndObject();
                writer.WriteStartObject("initialReadiness");
                writer.WriteNumber("gameTurn", placement.InitialReadiness.GameTurn);
                writer.WriteNumber("operationStage", placement.InitialReadiness.OperationStage);
                writer.WriteString("waterStatus", placement.InitialReadiness.WaterStatus);
                writer.WriteString("storesStatus", placement.InitialReadiness.StoresStatus);
                writer.WriteBoolean("pinned", placement.InitialReadiness.Pinned);
                WriteOrigin(writer, "origin", placement.InitialReadiness.Origin);
                writer.WriteEndObject();
                WriteOrigin(writer, "origin", placement.Origin);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteStartArray("retreatSupplyAnchors");
            foreach (var anchor in value.RetreatSupplyAnchors)
            {
                writer.WriteStartObject();
                writer.WriteString("sideId", anchor.SideId);
                writer.WriteString("locationId", anchor.LocationId);
                writer.WriteString("kind", anchor.Kind);
                WriteOrigin(writer, "origin", anchor.Origin);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            WriteOrigin(writer, "origin", value.Origin);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
    }

    private static void WriteBoundary(Utf8JsonWriter writer, string propertyName, ContentScenarioBoundary value)
    {
        writer.WriteStartObject(propertyName);
        writer.WriteNumber("gameTurn", value.GameTurn);
        writer.WriteNumber("operationStage", value.OperationStage);
        writer.WriteEndObject();
    }

    private static void WriteOrigin(Utf8JsonWriter writer, string propertyName, ContentOrigin value)
    {
        writer.WriteStartObject(propertyName);
        writer.WriteString("kind", "synthetic");
        writer.WriteStartArray("references");
        foreach (var reference in value.References)
        {
            writer.WriteStartObject();
            writer.WriteString("sourceId", reference.SourceId);
            writer.WriteString("locator", reference.Locator);
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void Fail(string code, string path, string message) =>
        throw new ContentPackV7ParseException(code, path, message);
}

internal static class CombatContentDiagnostics
{
    public const string Shape = "CMB-CNT-001";
    public const string Identity = "CMB-CNT-002";
    public const string Bounds = "CMB-CNT-003";
    public const string Profile = "CMB-CNT-004";
    public const string Reference = "CMB-CNT-005";
    public const string Provenance = "CMB-CNT-006";
    public const string Seed = "CMB-CNT-007";
    public const string Canonical = "CMB-CNT-008";
}

internal sealed class ContentPackV7ParseException(string code, string path, string message) : Exception(message)
{
    public string Code { get; } = code;
    public string Path { get; } = path;
}
