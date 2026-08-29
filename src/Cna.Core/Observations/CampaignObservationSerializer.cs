using System.Text;
using System.Text.Json;
using Cna.Core.Rules;

namespace Cna.Core.Observations;

public static class CampaignObservationSerializer
{
    public static byte[] SerializeCanonical(CampaignObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteNumber("contractVersion", observation.ContractVersion);
            writer.WriteString("policyId", observation.PolicyId);
            writer.WriteString("campaignId", observation.CampaignId);
            writer.WriteNumber("stateVersion", observation.StateVersion);
            writer.WriteString("rulesetHash", observation.RulesetHash);
            writer.WriteString("scenarioId", observation.ScenarioId);
            writer.WriteString("observer", FormatSide(observation.Observer));
            WritePosition(writer, observation.Position);
            WriteWeather(writer, observation.Weather);
            WriteLocations(writer, observation.Locations);
            WriteEdges(writer, observation.Edges);
            WriteOwnElements(writer, observation.OwnElements);
            WriteApparentOpposingPresences(writer, observation.ApparentOpposingPresences);
            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static CampaignObservation DeserializeCanonical(ReadOnlySpan<byte> utf8Json)
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
            var root = document.RootElement;
            RequireProperties(
                root,
                "contractVersion",
                "policyId",
                "campaignId",
                "stateVersion",
                "rulesetHash",
                "scenarioId",
                "observer",
                "position",
                "weather",
                "locations",
                "edges",
                "ownElements",
                "apparentOpposingPresences");

            var observation = new CampaignObservation(
                root.GetProperty("contractVersion").GetInt32(),
                root.GetProperty("policyId").GetString()!,
                root.GetProperty("campaignId").GetString()!,
                root.GetProperty("stateVersion").GetInt64(),
                root.GetProperty("rulesetHash").GetString()!,
                root.GetProperty("scenarioId").GetString()!,
                ParseSide(root.GetProperty("observer").GetString()),
                ParsePosition(root.GetProperty("position")),
                ParseWeather(root.GetProperty("weather")),
                ParseLocations(root.GetProperty("locations")),
                ParseEdges(root.GetProperty("edges")),
                ParseOwnElements(root.GetProperty("ownElements")),
                ParseApparentOpposingPresences(
                    root.GetProperty("apparentOpposingPresences")));

            if (!utf8Json.SequenceEqual(SerializeCanonical(observation)))
            {
                throw new JsonException("The campaign observation is not canonical JSON.");
            }

            return observation;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or InvalidOperationException
            or FormatException
            or KeyNotFoundException
            or OverflowException)
        {
            throw new JsonException("The campaign observation JSON is invalid.", exception);
        }
    }

    private static void WriteWeather(Utf8JsonWriter writer, CampaignObservationWeather? weather)
    {
        if (weather is null) { writer.WriteNull("weather"); return; }
        writer.WriteStartObject("weather");
        writer.WriteNumber("contractVersion", weather.ContractVersion);
        writer.WriteNumber("gameTurn", weather.GameTurn);
        writer.WriteNumber("operationStage", weather.OperationStage);
        writer.WriteString("season", weather.Season.ToString().ToLowerInvariant());
        writer.WriteString("kind", weather.Kind.ToString().ToLowerInvariant());
        writer.WriteString("scope", weather.Scope switch
        {
            CampaignObservationWeatherScope.None => "none",
            CampaignObservationWeatherScope.Global => "global",
            CampaignObservationWeatherScope.ListedAreas => "listed-areas",
            _ => throw new ArgumentOutOfRangeException(nameof(weather)),
        });
        writer.WriteStartArray("affectedAreas");
        foreach (var area in weather.AffectedAreas)
            writer.WriteStringValue(area.ToString().ToLowerInvariant());
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WritePosition(
        Utf8JsonWriter writer,
        CampaignObservationPosition position)
    {
        writer.WriteStartObject("position");
        writer.WriteString("positionId", position.PositionId);
        writer.WriteNumber("gameTurn", position.GameTurn);
        writer.WriteNumber("operationStage", position.OperationStage);
        writer.WriteString("stageId", position.StageId);
        writer.WriteString("phaseId", position.PhaseId);
        WriteNullableString(writer, "segmentId", position.SegmentId);
        WriteNullableString(writer, "stepId", position.StepId);
        writer.WriteString("actorRole", FormatActorRole(position.ActorRole));
        WriteNullableSide(writer, "activeSide", position.ActiveSide);
        WriteNullableSide(writer, "initiativeHolder", position.InitiativeHolder);
        writer.WriteEndObject();
    }

    private static void WriteLocations(
        Utf8JsonWriter writer,
        IEnumerable<CampaignObservationLocation> locations)
    {
        writer.WriteStartArray("locations");

        foreach (var location in locations)
        {
            writer.WriteStartObject();
            writer.WriteString("locationId", location.LocationId);
            writer.WriteString("terrainId", location.TerrainId);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteEdges(
        Utf8JsonWriter writer,
        IEnumerable<CampaignObservationEdge> edges)
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
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteOwnElements(
        Utf8JsonWriter writer,
        IEnumerable<ObservedOwnElement> ownElements)
    {
        writer.WriteStartArray("ownElements");

        foreach (var element in ownElements)
        {
            writer.WriteStartObject();
            writer.WriteString("elementId", element.ElementId);
            writer.WriteString("parentFormationId", element.ParentFormationId);
            writer.WriteString("organizationId", element.OrganizationId);
            writer.WriteNumber(
                "baseCapabilityPointAllowance",
                element.BaseCapabilityPointAllowance);
            writer.WriteString("currentLocationId", element.CurrentLocationId);
            writer.WriteString("reserveStatus", FormatReserveStatus(element.ReserveStatus));
            writer.WriteString("mobilityId", element.MobilityId);
            writer.WriteNumber("ledgerGameTurn", element.LedgerGameTurn);
            writer.WriteNumber("ledgerOperationStage", element.LedgerOperationStage);
            writer.WritePropertyName("capabilityPointsExpended");
            CapabilityPointAmountCodec.WriteCanonical(
                writer,
                element.CapabilityPointsExpended);
            writer.WriteNumber("cohesionLevel", element.CohesionLevel);
            WriteVehicleBreakdownRisk(writer, element.VehicleBreakdownRisk);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteVehicleBreakdownRisk(
        Utf8JsonWriter writer,
        ObservedOwnVehicleBreakdownRisk? risk)
    {
        if (risk is null)
        {
            writer.WriteNull("vehicleBreakdownRisk");
            return;
        }

        writer.WriteStartObject("vehicleBreakdownRisk");
        writer.WriteString("cohortId", risk.CohortId);
        writer.WriteString("vehicleTypeId", risk.VehicleTypeId);
        writer.WriteString("profileId", risk.ProfileId);
        writer.WritePropertyName("cumulativeBreakdownPoints");
        BreakdownPointAmountCodec.WriteCanonical(writer, risk.CumulativeBreakdownPoints);
        writer.WritePropertyName("sandstormAttributedBreakdownPoints");
        BreakdownPointAmountCodec.WriteCanonical(
            writer,
            risk.SandstormAttributedBreakdownPoints);
        WriteNullableString(
            writer,
            "highestEffectiveCheckedBandId",
            risk.HighestEffectiveCheckedBandId);
        writer.WriteNumber("workingPointCount", risk.WorkingPointCount);
        writer.WriteNumber("brokenPointCount", risk.BrokenPointCount);
        writer.WriteEndObject();
    }

    private static void WriteApparentOpposingPresences(
        Utf8JsonWriter writer,
        IEnumerable<ObservedApparentPresence> apparentOpposingPresences)
    {
        writer.WriteStartArray("apparentOpposingPresences");
        foreach (var presence in apparentOpposingPresences)
        {
            writer.WriteStartObject();
            writer.WriteString("representationId", presence.RepresentationId);
            writer.WriteString("currentLocationId", presence.CurrentLocationId);
            writer.WriteBoolean("exertsZoc", presence.ExertsZoc);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static CampaignObservationPosition ParsePosition(JsonElement position)
    {
        RequireProperties(
            position,
            "positionId",
            "gameTurn",
            "operationStage",
            "stageId",
            "phaseId",
            "segmentId",
            "stepId",
            "actorRole",
            "activeSide",
            "initiativeHolder");
        return new CampaignObservationPosition(
            position.GetProperty("positionId").GetString()!,
            position.GetProperty("gameTurn").GetInt32(),
            position.GetProperty("operationStage").GetInt32(),
            position.GetProperty("stageId").GetString()!,
            position.GetProperty("phaseId").GetString()!,
            ParseNullableString(position.GetProperty("segmentId")),
            ParseNullableString(position.GetProperty("stepId")),
            ParseActorRole(position.GetProperty("actorRole").GetString()),
            ParseNullableSide(position.GetProperty("activeSide")),
            ParseNullableSide(position.GetProperty("initiativeHolder")));
    }

    private static CampaignObservationWeather? ParseWeather(JsonElement weather)
    {
        if (weather.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        RequireProperties(
            weather,
            "contractVersion",
            "gameTurn",
            "operationStage",
            "season",
            "kind",
            "scope",
            "affectedAreas");
        return new CampaignObservationWeather(
            weather.GetProperty("contractVersion").GetInt32(),
            weather.GetProperty("gameTurn").GetInt32(),
            weather.GetProperty("operationStage").GetInt32(),
            ParseSeason(weather.GetProperty("season").GetString()),
            ParseWeatherKind(weather.GetProperty("kind").GetString()),
            ParseWeatherScope(weather.GetProperty("scope").GetString()),
            weather.GetProperty("affectedAreas")
                .EnumerateArray()
                .Select(area => ParseWeatherArea(area.GetString()))
                .ToArray());
    }

    private static CampaignObservationLocation[] ParseLocations(JsonElement locations) =>
        locations.EnumerateArray().Select(location =>
        {
            RequireProperties(location, "locationId", "terrainId");
            return new CampaignObservationLocation(
                location.GetProperty("locationId").GetString()!,
                location.GetProperty("terrainId").GetString()!);
        }).ToArray();

    private static CampaignObservationEdge[] ParseEdges(JsonElement edges) =>
        edges.EnumerateArray().Select(edge =>
        {
            RequireProperties(edge, "firstLocationId", "secondLocationId", "features");
            return new CampaignObservationEdge(
                edge.GetProperty("firstLocationId").GetString()!,
                edge.GetProperty("secondLocationId").GetString()!,
                edge.GetProperty("features").EnumerateArray().Select(feature =>
                {
                    RequireProperties(feature, "featureId", "directionFromLocationId");
                    return new CampaignObservationEdgeFeature(
                        feature.GetProperty("featureId").GetString()!,
                        ParseNullableString(feature.GetProperty("directionFromLocationId")));
                }).ToArray());
        }).ToArray();

    private static ObservedOwnElement[] ParseOwnElements(JsonElement ownElements) =>
        ownElements.EnumerateArray().Select(element =>
        {
            RequireProperties(
                element,
                "elementId",
                "parentFormationId",
                "organizationId",
                "baseCapabilityPointAllowance",
                "currentLocationId",
                "reserveStatus",
                "mobilityId",
                "ledgerGameTurn",
                "ledgerOperationStage",
                "capabilityPointsExpended",
                "cohesionLevel",
                "vehicleBreakdownRisk");
            return new ObservedOwnElement(
                element.GetProperty("elementId").GetString()!,
                element.GetProperty("parentFormationId").GetString()!,
                element.GetProperty("organizationId").GetString()!,
                element.GetProperty("baseCapabilityPointAllowance").GetInt32(),
                element.GetProperty("currentLocationId").GetString()!,
                ParseReserveStatus(element.GetProperty("reserveStatus").GetString()),
                element.GetProperty("mobilityId").GetString()!,
                element.GetProperty("ledgerGameTurn").GetInt32(),
                element.GetProperty("ledgerOperationStage").GetInt32(),
                CapabilityPointAmountCodec.Deserialize(Encoding.UTF8.GetBytes(
                    element.GetProperty("capabilityPointsExpended").GetRawText())),
                element.GetProperty("cohesionLevel").GetInt32(),
                ParseVehicleBreakdownRisk(element.GetProperty("vehicleBreakdownRisk")));
        }).ToArray();

    private static ObservedOwnVehicleBreakdownRisk? ParseVehicleBreakdownRisk(JsonElement risk)
    {
        if (risk.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        RequireProperties(
            risk,
            "cohortId",
            "vehicleTypeId",
            "profileId",
            "cumulativeBreakdownPoints",
            "sandstormAttributedBreakdownPoints",
            "highestEffectiveCheckedBandId",
            "workingPointCount",
            "brokenPointCount");
        return new ObservedOwnVehicleBreakdownRisk(
            risk.GetProperty("cohortId").GetString()!,
            risk.GetProperty("vehicleTypeId").GetString()!,
            risk.GetProperty("profileId").GetString()!,
            BreakdownPointAmountCodec.Deserialize(Encoding.UTF8.GetBytes(
                risk.GetProperty("cumulativeBreakdownPoints").GetRawText())),
            BreakdownPointAmountCodec.Deserialize(Encoding.UTF8.GetBytes(
                risk.GetProperty("sandstormAttributedBreakdownPoints").GetRawText())),
            ParseNullableString(risk.GetProperty("highestEffectiveCheckedBandId")),
            risk.GetProperty("workingPointCount").GetInt32(),
            risk.GetProperty("brokenPointCount").GetInt32());
    }

    private static ObservedApparentPresence[] ParseApparentOpposingPresences(
        JsonElement apparentOpposingPresences) => apparentOpposingPresences
            .EnumerateArray()
            .Select(presence =>
            {
                RequireProperties(
                    presence,
                    "representationId",
                    "currentLocationId",
                    "exertsZoc");
                return new ObservedApparentPresence(
                    presence.GetProperty("representationId").GetString()!,
                    presence.GetProperty("currentLocationId").GetString()!,
                    presence.GetProperty("exertsZoc").GetBoolean());
            })
            .ToArray();

    private static void RequireProperties(JsonElement element, params string[] expected)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.EnumerateObject().Select(property => property.Name)
                .SequenceEqual(expected, StringComparer.Ordinal))
        {
            throw new JsonException(
                "The campaign observation property contract is invalid.");
        }
    }

    private static string? ParseNullableString(JsonElement element) =>
        element.ValueKind == JsonValueKind.Null ? null : element.GetString();

    private static LandSide? ParseNullableSide(JsonElement element) =>
        element.ValueKind == JsonValueKind.Null ? null : ParseSide(element.GetString());

    private static string FormatActorRole(LandActorRole role) => role switch
    {
        LandActorRole.None => "none",
        LandActorRole.Commonwealth => "commonwealth",
        LandActorRole.InitiativeHolder => "initiative-holder",
        LandActorRole.FirstActingSide => "first-acting-side",
        LandActorRole.SecondActingSide => "second-acting-side",
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };

    private static LandActorRole ParseActorRole(string? role) => role switch
    {
        "none" => LandActorRole.None,
        "commonwealth" => LandActorRole.Commonwealth,
        "initiative-holder" => LandActorRole.InitiativeHolder,
        "first-acting-side" => LandActorRole.FirstActingSide,
        "second-acting-side" => LandActorRole.SecondActingSide,
        _ => throw new JsonException($"Unknown Land actor role '{role}'."),
    };

    private static string FormatSide(LandSide side) => side switch
    {
        LandSide.Axis => "axis",
        LandSide.Commonwealth => "commonwealth",
        _ => throw new ArgumentOutOfRangeException(nameof(side)),
    };

    private static LandSide ParseSide(string? side) => side switch
    {
        "axis" => LandSide.Axis,
        "commonwealth" => LandSide.Commonwealth,
        _ => throw new JsonException($"Unknown Land side '{side}'."),
    };

    private static string FormatReserveStatus(
        CampaignObservationReserveStatus status) => status switch
        {
            CampaignObservationReserveStatus.None => "none",
            CampaignObservationReserveStatus.ReserveI => "reserve-i",
            CampaignObservationReserveStatus.ReserveII => "reserve-ii",
            _ => throw new ArgumentOutOfRangeException(nameof(status)),
        };

    private static CampaignObservationReserveStatus ParseReserveStatus(string? status) =>
        status switch
        {
            "none" => CampaignObservationReserveStatus.None,
            "reserve-i" => CampaignObservationReserveStatus.ReserveI,
            "reserve-ii" => CampaignObservationReserveStatus.ReserveII,
            _ => throw new JsonException($"Unknown observed Reserve status '{status}'."),
        };

    private static CampaignObservationWeatherSeason ParseSeason(string? season) => season switch
    {
        "fall" => CampaignObservationWeatherSeason.Fall,
        "winter" => CampaignObservationWeatherSeason.Winter,
        "spring" => CampaignObservationWeatherSeason.Spring,
        "summer" => CampaignObservationWeatherSeason.Summer,
        _ => throw new JsonException($"Unknown observed Weather season '{season}'."),
    };

    private static CampaignObservationWeatherKind ParseWeatherKind(string? kind) => kind switch
    {
        "normal" => CampaignObservationWeatherKind.Normal,
        "hot" => CampaignObservationWeatherKind.Hot,
        "sandstorm" => CampaignObservationWeatherKind.Sandstorm,
        "rainstorm" => CampaignObservationWeatherKind.Rainstorm,
        _ => throw new JsonException($"Unknown observed Weather kind '{kind}'."),
    };

    private static CampaignObservationWeatherScope ParseWeatherScope(string? scope) =>
        scope switch
        {
            "none" => CampaignObservationWeatherScope.None,
            "global" => CampaignObservationWeatherScope.Global,
            "listed-areas" => CampaignObservationWeatherScope.ListedAreas,
            _ => throw new JsonException($"Unknown observed Weather scope '{scope}'."),
        };

    private static CampaignObservationWeatherArea ParseWeatherArea(string? area) => area switch
    {
        "a" => CampaignObservationWeatherArea.A,
        "b" => CampaignObservationWeatherArea.B,
        "c" => CampaignObservationWeatherArea.C,
        "d" => CampaignObservationWeatherArea.D,
        "e" => CampaignObservationWeatherArea.E,
        _ => throw new JsonException($"Unknown observed Weather area '{area}'."),
    };

    private static void WriteNullableSide(
        Utf8JsonWriter writer,
        string propertyName,
        LandSide? side) => WriteNullableString(
            writer,
            propertyName,
            side is null ? null : FormatSide(side.Value));

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
}
