using System.Text.Json;
using Cna.Core.Randomness;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignEventSerializer
{
    public static byte[] Serialize(CampaignEvent campaignEvent)
    {
        ArgumentNullException.ThrowIfNull(campaignEvent);
        using var stream = new MemoryStream();

        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();

            switch (campaignEvent)
            {
                case CampaignCreated created:
                    ValidateCreated(created);
                    WriteCreated(writer, created);
                    break;
                case InitiativeDetermined determined:
                    ValidateDetermined(determined);
                    WriteDetermined(writer, determined);
                    break;
                case NoObligationNavalConvoyScheduleResolved resolved:
                    ValidateAdvance(resolved, LandPhaseIds.TacticalShipping, 3);
                    WriteAdvance(writer, "no-obligation-naval-convoy-schedule-resolved", resolved);
                    break;
                case NoObligationTacticalShippingResolved resolved:
                    ValidateAdvance(resolved, LandPhaseIds.InitiativeDeclaration, 4);
                    WriteAdvance(writer, "no-obligation-tactical-shipping-resolved", resolved);
                    break;
                case InitiativeOrderDeclared declared:
                    ValidateDeclaration(declared);
                    WriteDeclaration(writer, declared);
                    break;
                case WeatherDetermined determined:
                    ValidateWeather(determined);
                    WriteWeather(writer, determined);
                    break;
                case NoObligationOrganizationResolved resolved:
                    ValidateStageEntry(resolved, 7);
                    WriteStageEntry(writer, "no-obligation-organization-resolved", resolved);
                    break;
                case NoObligationNavalConvoyArrivalResolved resolved:
                    ValidateStageEntry(resolved, 8);
                    WriteStageEntry(writer, "no-obligation-naval-convoy-arrival-resolved", resolved);
                    break;
                case NoObligationFleetAssignmentResolved resolved:
                    ValidateStageEntry(resolved, 9);
                    WriteStageEntry(writer, "no-obligation-fleet-assignment-resolved", resolved);
                    break;
                case NoObligationFleetRepairResolved resolved:
                    ValidateStageEntry(resolved, 10);
                    WriteStageEntry(writer, "no-obligation-fleet-repair-resolved", resolved);
                    break;
                case ReserveElementDesignated designated:
                    ValidateReserve(designated);
                    WriteReserveDesignation(writer, designated);
                    break;
                case ReserveDesignationCompleted completed:
                    ValidateReserve(completed);
                    WriteReserveCompletion(writer, completed);
                    break;
                case ElementMoved moved:
                    ValidateMovement(moved);
                    WriteMovement(writer, moved);
                    break;
                case MovementSegmentCompleted completed:
                    ValidateMovementCompletion(completed);
                    WriteMovementCompletion(writer, completed);
                    break;
                default:
                    throw new JsonException("The campaign event type is not serializable.");
            }

            writer.WriteEndObject();
        }

        return stream.ToArray();
    }

    public static CampaignEvent Deserialize(ReadOnlyMemory<byte> canonicalJson)
    {
        try
        {
            using var document = JsonDocument.Parse(canonicalJson);
            var root = document.RootElement;
            var eventType = root.GetProperty("eventType").GetString();

            CampaignEvent campaignEvent = eventType switch
            {
                "campaign-created" => ParseCreated(root),
                "initiative-determined" => ParseDetermined(root),
                "no-obligation-naval-convoy-schedule-resolved" => ParseSchedule(root),
                "no-obligation-tactical-shipping-resolved" => ParseTactical(root),
                "initiative-order-declared" => ParseDeclaration(root),
                "weather-determined" => ParseWeather(root),
                "no-obligation-organization-resolved" => ParseOrganization(root),
                "no-obligation-naval-convoy-arrival-resolved" => ParseArrival(root),
                "no-obligation-fleet-assignment-resolved" => ParseFleetAssignment(root),
                "no-obligation-fleet-repair-resolved" => ParseFleetRepair(root),
                "reserve-element-designated" => ParseReserveDesignation(root),
                "reserve-designation-completed" => ParseReserveCompletion(root),
                "element-moved" => ParseMovement(root),
                "movement-segment-completed" => ParseMovementCompletion(root),
                _ => throw new JsonException($"Unknown campaign event type '{eventType}'."),
            };

            if (!canonicalJson.Span.SequenceEqual(Serialize(campaignEvent)))
            {
                throw new JsonException("The campaign event is not canonical JSON.");
            }

            return campaignEvent;
        }
        catch (JsonException)
        {
            throw;
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or FormatException
            or InvalidOperationException
            or KeyNotFoundException)
        {
            throw new JsonException("The campaign event JSON is invalid.", exception);
        }
    }

    private static void WriteCreated(Utf8JsonWriter writer, CampaignCreated created)
    {
        writer.WriteNumber("contractVersion", created.ContractVersion);
        writer.WriteString("eventType", "campaign-created");
        writer.WriteString("campaignId", created.CampaignId);
        writer.WriteNumber("stateVersion", created.StateVersion);
        writer.WriteString("rulesetHash", created.RulesetHash);
        CampaignSnapshotSerializer.WriteSetup(writer, created.Setup);
        CampaignSnapshotSerializer.WriteWorld(writer, "initialWorld", created.InitialWorld);
        CampaignSnapshotSerializer.WriteRandomState(writer, created.RandomState);
        CampaignSnapshotSerializer.WritePosition(writer, created.SequencePosition);
    }

    private static CampaignCreated ParseCreated(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "rulesetHash",
            "setup",
            "initialWorld",
            "randomState",
            "sequencePosition");
        var created = new CampaignCreated(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("rulesetHash").GetString()!,
            CampaignSnapshotSerializer.ParseSetup(root.GetProperty("setup")),
            CampaignSnapshotSerializer.ParseWorld(root.GetProperty("initialWorld")),
            CampaignSnapshotSerializer.ParseRandomState(root.GetProperty("randomState")),
            CampaignSnapshotSerializer.ParsePosition(root.GetProperty("sequencePosition")));

        if (root.GetProperty("contractVersion").GetInt32() != created.ContractVersion)
        {
            throw new JsonException("The campaign creation contract version is invalid.");
        }

        ValidateCreated(created);
        return created;
    }

    private static void WriteDetermined(Utf8JsonWriter writer, InitiativeDetermined determined)
    {
        writer.WriteNumber("contractVersion", determined.ContractVersion);
        writer.WriteString("eventType", "initiative-determined");
        writer.WriteString("campaignId", determined.CampaignId);
        writer.WriteNumber("stateVersion", determined.StateVersion);
        writer.WriteString("fromPositionId", determined.FromPositionId);
        writer.WriteStartObject("outcome");
        WriteOutcome(writer, determined.Outcome);
        writer.WriteEndObject();
        writer.WriteString("randomAlgorithmId", determined.RandomAlgorithmId);
        writer.WriteNumber("randomCursorBefore", determined.RandomCursorBefore);
        writer.WriteNumber("randomCursorAfter", determined.RandomCursorAfter);
        CampaignSnapshotSerializer.WritePosition(writer, determined.SequencePosition);
        CampaignSnapshotSerializer.WriteSources(writer, determined.Sources);
    }

    private static void WriteWeather(Utf8JsonWriter writer, WeatherDetermined determined)
    {
        writer.WriteNumber("contractVersion", determined.ContractVersion);
        writer.WriteString("eventType", "weather-determined");
        writer.WriteString("campaignId", determined.CampaignId);
        writer.WriteNumber("stateVersion", determined.StateVersion);
        writer.WriteString("fromPositionId", determined.FromPositionId);
        writer.WriteNumber("gameTurn", determined.GameTurn);
        writer.WriteNumber("operationStage", determined.OperationStage);
        writer.WriteString("determiningSide", CampaignSnapshotSerializer.FormatSide(determined.DeterminingSide));
        writer.WriteString("season", CampaignOperationStageWeatherCodec.FormatSeason(determined.Season));
        writer.WriteNumber("firstDie", determined.FirstDie);
        writer.WriteNumber("secondDie", determined.SecondDie);
        writer.WriteString("kind", CampaignOperationStageWeatherCodec.FormatKind(determined.Kind));
        writer.WriteString("scope", CampaignOperationStageWeatherCodec.FormatScope(determined.Scope));
        if (determined.LocationDie.HasValue) writer.WriteNumber("locationDie", determined.LocationDie.Value);
        else writer.WriteNull("locationDie");
        writer.WriteStartArray("affectedAreas");
        foreach (var area in determined.AffectedAreas)
            writer.WriteStringValue(CampaignOperationStageWeatherCodec.FormatArea(area));
        writer.WriteEndArray();
        writer.WriteNumber("fuelWaterReductionSubjectCount", determined.FuelWaterReductionSubjectCount);
        writer.WriteNumber("restoredWellCount", determined.RestoredWellCount);
        writer.WriteNumber("damagedGroundedAircraftCount", determined.DamagedGroundedAircraftCount);
        writer.WriteNumber("randomCursorAfter", determined.RandomCursorAfter);
        CampaignSnapshotSerializer.WritePosition(writer, determined.SequencePosition);
        CampaignSnapshotSerializer.WriteSources(writer, determined.Sources);
    }

    private static void WriteStageEntry(
        Utf8JsonWriter writer,
        string eventType,
        StageEntryResolved resolved)
    {
        writer.WriteNumber("contractVersion", resolved.ContractVersion);
        writer.WriteString("eventType", eventType);
        writer.WriteString("campaignId", resolved.CampaignId);
        writer.WriteNumber("stateVersion", resolved.StateVersion);
        writer.WriteString("fromPositionId", resolved.FromPositionId);
        writer.WriteNumber("gameTurn", resolved.GameTurn);
        writer.WriteNumber("operationStage", resolved.OperationStage);
        CampaignSnapshotSerializer.WritePosition(writer, resolved.SequencePosition);
        CampaignSnapshotSerializer.WriteSources(writer, resolved.Sources);
    }

    private static void WriteReserveDesignation(
        Utf8JsonWriter writer,
        ReserveElementDesignated designated)
    {
        WriteReserveEnvelope(
            writer,
            "reserve-element-designated",
            designated);
        writer.WriteString("elementId", designated.ElementId);
        writer.WriteString("priorStatus", FormatReserveStatus(designated.PriorStatus));
        writer.WriteString(
            "resultingStatus",
            FormatReserveStatus(designated.ResultingStatus));
        CampaignSnapshotSerializer.WritePosition(
            writer,
            designated.SequencePosition);
        CampaignSnapshotSerializer.WriteSources(writer, designated.Sources);
    }

    private static void WriteReserveCompletion(
        Utf8JsonWriter writer,
        ReserveDesignationCompleted completed)
    {
        WriteReserveEnvelope(
            writer,
            "reserve-designation-completed",
            completed);
        CampaignSnapshotSerializer.WritePosition(writer, completed.SequencePosition);
        CampaignSnapshotSerializer.WriteSources(writer, completed.Sources);
    }

    private static void WriteMovement(Utf8JsonWriter writer, ElementMoved moved)
    {
        writer.WriteNumber("contractVersion", moved.ContractVersion);
        writer.WriteString("eventType", "element-moved");
        writer.WriteString("campaignId", moved.CampaignId);
        writer.WriteNumber("stateVersion", moved.StateVersion);
        writer.WriteNumber("priorStateVersion", moved.PriorStateVersion);
        writer.WriteString("fromPositionId", moved.FromPositionId);
        writer.WriteNumber("gameTurn", moved.GameTurn);
        writer.WriteNumber("operationStage", moved.OperationStage);
        writer.WriteString(
            "actingSide",
            CampaignSnapshotSerializer.FormatSide(moved.ActingSide));
        writer.WriteString("elementId", moved.ElementId);
        writer.WriteString("representationId", moved.RepresentationId);
        writer.WriteString("originLocationId", moved.OriginLocationId);
        writer.WriteString("destinationLocationId", moved.DestinationLocationId);
        writer.WriteString("mobilityId", moved.MobilityId);
        WriteSources(writer, "mobilitySources", moved.MobilitySources);
        WriteMovementCost(writer, moved.Cost);
        writer.WritePropertyName("capabilityPointsExpendedBefore");
        CapabilityPointAmountCodec.WriteCanonical(
            writer,
            moved.CapabilityPointsExpendedBefore);
        writer.WritePropertyName("capabilityPointsExpendedAfter");
        CapabilityPointAmountCodec.WriteCanonical(
            writer,
            moved.CapabilityPointsExpendedAfter);
        writer.WriteNumber("cohesionBefore", moved.CohesionBefore);
        writer.WriteNumber("cohesionAfter", moved.CohesionAfter);
        CampaignSnapshotSerializer.WritePosition(writer, moved.SequencePosition);
    }

    private static void WriteMovementCompletion(
        Utf8JsonWriter writer,
        MovementSegmentCompleted completed)
    {
        writer.WriteNumber("contractVersion", completed.ContractVersion);
        writer.WriteString("eventType", "movement-segment-completed");
        writer.WriteString("campaignId", completed.CampaignId);
        writer.WriteNumber("stateVersion", completed.StateVersion);
        writer.WriteNumber("priorStateVersion", completed.PriorStateVersion);
        writer.WriteString("fromPositionId", completed.FromPositionId);
        writer.WriteNumber("gameTurn", completed.GameTurn);
        writer.WriteNumber("operationStage", completed.OperationStage);
        writer.WriteString(
            "actingSide",
            CampaignSnapshotSerializer.FormatSide(completed.ActingSide));
        CampaignSnapshotSerializer.WritePosition(writer, completed.SequencePosition);
    }

    private static void WriteMovementCost(
        Utf8JsonWriter writer,
        CampaignMovementCost cost)
    {
        writer.WriteStartObject("cost");
        writer.WriteString("destinationTerrainId", cost.DestinationTerrainId);
        writer.WritePropertyName("destinationTerrainCost");
        CapabilityPointAmountCodec.WriteCanonical(writer, cost.DestinationTerrainCost);
        WriteSources(
            writer,
            "destinationTerrainSources",
            cost.DestinationTerrainSources);
        if (cost.RouteAdjustment is null)
        {
            writer.WriteNull("routeAdjustment");
        }
        else
        {
            writer.WriteStartObject("routeAdjustment");
            writer.WriteString("routeId", cost.RouteAdjustment.RouteId);
            writer.WriteString(
                "costKind",
                FormatRouteCostKind(cost.RouteAdjustment.CostKind));
            writer.WritePropertyName("amount");
            CapabilityPointAmountCodec.WriteCanonical(
                writer,
                cost.RouteAdjustment.Amount);
            CampaignSnapshotSerializer.WriteSources(
                writer,
                cost.RouteAdjustment.Sources);
            writer.WriteEndObject();
        }

        writer.WriteStartArray("crossedHexsideCosts");
        foreach (var crossed in cost.CrossedHexsideCosts)
        {
            writer.WriteStartObject();
            writer.WriteString("hexsideId", crossed.HexsideId);
            writer.WriteString("direction", FormatHexsideDirection(crossed.Direction));
            writer.WritePropertyName("addedCost");
            CapabilityPointAmountCodec.WriteCanonical(writer, crossed.AddedCost);
            CampaignSnapshotSerializer.WriteSources(writer, crossed.Sources);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WritePropertyName("totalCost");
        CapabilityPointAmountCodec.WriteCanonical(writer, cost.TotalCost);
        writer.WriteEndObject();
    }

    private static ElementMoved ParseMovement(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "priorStateVersion",
            "fromPositionId",
            "gameTurn",
            "operationStage",
            "actingSide",
            "elementId",
            "representationId",
            "originLocationId",
            "destinationLocationId",
            "mobilityId",
            "mobilitySources",
            "cost",
            "capabilityPointsExpendedBefore",
            "capabilityPointsExpendedAfter",
            "cohesionBefore",
            "cohesionAfter",
            "sequencePosition");
        var moved = new ElementMoved(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("priorStateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            root.GetProperty("gameTurn").GetInt32(),
            root.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParseSide(
                root.GetProperty("actingSide").GetString()),
            root.GetProperty("elementId").GetString()!,
            root.GetProperty("representationId").GetString()!,
            root.GetProperty("originLocationId").GetString()!,
            root.GetProperty("destinationLocationId").GetString()!,
            root.GetProperty("mobilityId").GetString()!,
            CampaignSnapshotSerializer.ParseSources(
                root.GetProperty("mobilitySources")),
            ParseMovementCost(root.GetProperty("cost")),
            ParseCapabilityPointAmount(
                root.GetProperty("capabilityPointsExpendedBefore")),
            ParseCapabilityPointAmount(
                root.GetProperty("capabilityPointsExpendedAfter")),
            root.GetProperty("cohesionBefore").GetInt32(),
            root.GetProperty("cohesionAfter").GetInt32(),
            CampaignSnapshotSerializer.ParsePosition(
                root.GetProperty("sequencePosition")));
        if (root.GetProperty("contractVersion").GetInt32() != moved.ContractVersion)
        {
            throw new JsonException("The ElementMoved event contract version is invalid.");
        }

        ValidateMovement(moved);
        return moved;
    }

    private static MovementSegmentCompleted ParseMovementCompletion(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "priorStateVersion",
            "fromPositionId",
            "gameTurn",
            "operationStage",
            "actingSide",
            "sequencePosition");
        if (root.GetProperty("contractVersion").GetInt32() != 1)
        {
            throw new JsonException(
                "The MovementSegmentCompleted event contract version is invalid.");
        }

        var completed = new MovementSegmentCompleted(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("priorStateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            root.GetProperty("gameTurn").GetInt32(),
            root.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParseSide(
                root.GetProperty("actingSide").GetString()),
            CampaignSnapshotSerializer.ParsePosition(
                root.GetProperty("sequencePosition")));
        ValidateMovementCompletion(completed);
        return completed;
    }

    private static CampaignMovementCost ParseMovementCost(JsonElement cost)
    {
        CampaignSnapshotSerializer.RequireProperties(
            cost,
            "destinationTerrainId",
            "destinationTerrainCost",
            "destinationTerrainSources",
            "routeAdjustment",
            "crossedHexsideCosts",
            "totalCost");
        var route = cost.GetProperty("routeAdjustment");
        CampaignMovementRouteAdjustment? routeAdjustment = null;
        if (route.ValueKind != JsonValueKind.Null)
        {
            CampaignSnapshotSerializer.RequireProperties(
                route,
                "routeId",
                "costKind",
                "amount",
                "sources");
            routeAdjustment = new CampaignMovementRouteAdjustment(
                route.GetProperty("routeId").GetString()!,
                ParseRouteCostKind(route.GetProperty("costKind").GetString()),
                ParseCapabilityPointAmount(route.GetProperty("amount")),
                CampaignSnapshotSerializer.ParseSources(
                    route.GetProperty("sources")));
        }

        var crossed = cost.GetProperty("crossedHexsideCosts")
            .EnumerateArray()
            .Select(value =>
            {
                CampaignSnapshotSerializer.RequireProperties(
                    value,
                    "hexsideId",
                    "direction",
                    "addedCost",
                    "sources");
                return new CampaignMovementHexsideCost(
                    value.GetProperty("hexsideId").GetString()!,
                    ParseHexsideDirection(value.GetProperty("direction").GetString()),
                    ParseCapabilityPointAmount(value.GetProperty("addedCost")),
                    CampaignSnapshotSerializer.ParseSources(
                        value.GetProperty("sources")));
            })
            .ToArray();
        return new CampaignMovementCost(
            cost.GetProperty("destinationTerrainId").GetString()!,
            ParseCapabilityPointAmount(cost.GetProperty("destinationTerrainCost")),
            CampaignSnapshotSerializer.ParseSources(
                cost.GetProperty("destinationTerrainSources")),
            routeAdjustment,
            crossed,
            ParseCapabilityPointAmount(cost.GetProperty("totalCost")));
    }

    private static CapabilityPointAmount ParseCapabilityPointAmount(JsonElement value) =>
        CapabilityPointAmountCodec.Deserialize(
            System.Text.Encoding.UTF8.GetBytes(value.GetRawText()));

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

    private static string FormatRouteCostKind(MovementRouteCostKind kind) => kind switch
    {
        MovementRouteCostKind.Override => "override",
        MovementRouteCostKind.ScaleUnderlying => "scale-underlying",
        _ => throw new JsonException("The Movement route cost kind is invalid."),
    };

    private static MovementRouteCostKind ParseRouteCostKind(string? kind) => kind switch
    {
        "override" => MovementRouteCostKind.Override,
        "scale-underlying" => MovementRouteCostKind.ScaleUnderlying,
        _ => throw new JsonException($"Unknown Movement route cost kind '{kind}'."),
    };

    private static string FormatHexsideDirection(MovementHexsideDirection direction) =>
        direction switch
        {
            MovementHexsideDirection.Either => "either",
            MovementHexsideDirection.Up => "up",
            MovementHexsideDirection.Down => "down",
            _ => throw new JsonException("The Movement hexside direction is invalid."),
        };

    private static MovementHexsideDirection ParseHexsideDirection(string? direction) =>
        direction switch
        {
            "either" => MovementHexsideDirection.Either,
            "up" => MovementHexsideDirection.Up,
            "down" => MovementHexsideDirection.Down,
            _ => throw new JsonException(
                $"Unknown Movement hexside direction '{direction}'."),
        };

    private static void ValidateMovement(ElementMoved moved)
    {
        try
        {
            moved.ValidateContract();
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidOperationException)
        {
            throw new JsonException("The ElementMoved event contract is invalid.", exception);
        }
    }

    private static void ValidateMovementCompletion(MovementSegmentCompleted completed)
    {
        try
        {
            completed.ValidateContract();
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidOperationException)
        {
            throw new JsonException(
                "The MovementSegmentCompleted event contract is invalid.",
                exception);
        }
    }

    private static void WriteReserveEnvelope(
        Utf8JsonWriter writer,
        string eventType,
        ReserveDesignationEvent campaignEvent)
    {
        writer.WriteNumber("contractVersion", campaignEvent.ContractVersion);
        writer.WriteString("eventType", eventType);
        writer.WriteString("campaignId", campaignEvent.CampaignId);
        writer.WriteNumber("stateVersion", campaignEvent.StateVersion);
        writer.WriteString("fromPositionId", campaignEvent.FromPositionId);
        writer.WriteNumber("gameTurn", campaignEvent.GameTurn);
        writer.WriteNumber("operationStage", campaignEvent.OperationStage);
        writer.WriteString(
            "actingSide",
            CampaignSnapshotSerializer.FormatSide(campaignEvent.ActingSide));
    }

    private static ReserveElementDesignated ParseReserveDesignation(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "fromPositionId",
            "gameTurn",
            "operationStage",
            "actingSide",
            "elementId",
            "priorStatus",
            "resultingStatus",
            "sequencePosition",
            "sources");
        RequireReserveContract(root);
        return new ReserveElementDesignated(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            root.GetProperty("gameTurn").GetInt32(),
            root.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParseSide(
                root.GetProperty("actingSide").GetString()),
            root.GetProperty("elementId").GetString()!,
            ParseReserveStatus(root.GetProperty("priorStatus").GetString()),
            ParseReserveStatus(root.GetProperty("resultingStatus").GetString()),
            CampaignSnapshotSerializer.ParsePosition(
                root.GetProperty("sequencePosition")),
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));
    }

    private static ReserveDesignationCompleted ParseReserveCompletion(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "fromPositionId",
            "gameTurn",
            "operationStage",
            "actingSide",
            "sequencePosition",
            "sources");
        RequireReserveContract(root);
        return new ReserveDesignationCompleted(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            root.GetProperty("gameTurn").GetInt32(),
            root.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParseSide(
                root.GetProperty("actingSide").GetString()),
            CampaignSnapshotSerializer.ParsePosition(
                root.GetProperty("sequencePosition")),
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));
    }

    private static void RequireReserveContract(JsonElement root)
    {
        if (root.GetProperty("contractVersion").GetInt32() != 1)
        {
            throw new JsonException(
                "The Reserve designation event contract version is invalid.");
        }
    }

    private static string FormatReserveStatus(
        CampaignElementReserveStatus status) => status switch
        {
            CampaignElementReserveStatus.None => "none",
            CampaignElementReserveStatus.ReserveI => "reserve-i",
            CampaignElementReserveStatus.ReserveII => "reserve-ii",
            _ => throw new JsonException("The Reserve status is invalid."),
        };

    private static CampaignElementReserveStatus ParseReserveStatus(string? status) =>
        status switch
        {
            "none" => CampaignElementReserveStatus.None,
            "reserve-i" => CampaignElementReserveStatus.ReserveI,
            "reserve-ii" => CampaignElementReserveStatus.ReserveII,
            _ => throw new JsonException($"Unknown Reserve status '{status}'."),
        };

    private static NoObligationOrganizationResolved ParseOrganization(JsonElement root)
    {
        RequireStageEntryProperties(root);
        return new NoObligationOrganizationResolved(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            root.GetProperty("gameTurn").GetInt32(),
            root.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParsePosition(root.GetProperty("sequencePosition")),
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));
    }

    private static NoObligationNavalConvoyArrivalResolved ParseArrival(JsonElement root)
    {
        RequireStageEntryProperties(root);
        return new NoObligationNavalConvoyArrivalResolved(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            root.GetProperty("gameTurn").GetInt32(),
            root.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParsePosition(root.GetProperty("sequencePosition")),
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));
    }

    private static NoObligationFleetAssignmentResolved ParseFleetAssignment(JsonElement root)
    {
        RequireStageEntryProperties(root);
        return new NoObligationFleetAssignmentResolved(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            root.GetProperty("gameTurn").GetInt32(),
            root.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParsePosition(root.GetProperty("sequencePosition")),
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));
    }

    private static NoObligationFleetRepairResolved ParseFleetRepair(JsonElement root)
    {
        RequireStageEntryProperties(root);
        return new NoObligationFleetRepairResolved(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            root.GetProperty("gameTurn").GetInt32(),
            root.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParsePosition(root.GetProperty("sequencePosition")),
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));
    }

    private static void RequireStageEntryProperties(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "fromPositionId",
            "gameTurn",
            "operationStage",
            "sequencePosition",
            "sources");
        if (root.GetProperty("contractVersion").GetInt32() != 1)
        {
            throw new JsonException("The Stage Entry event contract version is invalid.");
        }
    }

    private static WeatherDetermined ParseWeather(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType",
            "campaignId", "stateVersion", "fromPositionId", "gameTurn", "operationStage",
            "determiningSide", "season", "firstDie", "secondDie", "kind", "scope",
            "locationDie", "affectedAreas", "fuelWaterReductionSubjectCount", "restoredWellCount",
            "damagedGroundedAircraftCount", "randomCursorAfter", "sequencePosition", "sources");
        var location = root.GetProperty("locationDie");
        var determined = new WeatherDetermined(root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(), root.GetProperty("fromPositionId").GetString()!,
            root.GetProperty("gameTurn").GetInt32(), root.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParseSide(root.GetProperty("determiningSide").GetString()),
            CampaignOperationStageWeatherCodec.ParseSeason(root.GetProperty("season").GetString()),
            root.GetProperty("firstDie").GetInt32(), root.GetProperty("secondDie").GetInt32(),
            CampaignOperationStageWeatherCodec.ParseKind(root.GetProperty("kind").GetString()),
            CampaignOperationStageWeatherCodec.ParseScope(root.GetProperty("scope").GetString()),
            location.ValueKind == JsonValueKind.Null ? null : location.GetInt32(),
            root.GetProperty("affectedAreas").EnumerateArray()
                .Select(value => CampaignOperationStageWeatherCodec.ParseArea(value.GetString())).ToArray(),
            root.GetProperty("fuelWaterReductionSubjectCount").GetInt32(),
            root.GetProperty("restoredWellCount").GetInt32(),
            root.GetProperty("damagedGroundedAircraftCount").GetInt32(),
            root.GetProperty("randomCursorAfter").GetUInt64(),
            CampaignSnapshotSerializer.ParsePosition(root.GetProperty("sequencePosition")),
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));
        if (root.GetProperty("contractVersion").GetInt32() != determined.ContractVersion)
            throw new JsonException("The Weather event contract version is invalid.");
        ValidateWeather(determined);
        return determined;
    }

    private static void WriteAdvance(Utf8JsonWriter writer, string eventType,
        OpeningPreambleAdvanced resolved)
    {
        writer.WriteNumber("contractVersion", resolved.ContractVersion);
        writer.WriteString("eventType", eventType);
        writer.WriteString("campaignId", resolved.CampaignId);
        writer.WriteNumber("stateVersion", resolved.StateVersion);
        writer.WriteString("fromPositionId", resolved.FromPositionId);
        CampaignSnapshotSerializer.WritePosition(writer, resolved.SequencePosition);
        CampaignSnapshotSerializer.WriteSources(writer, resolved.Sources);
    }

    private static NoObligationNavalConvoyScheduleResolved ParseSchedule(JsonElement root)
    {
        RequireAdvanceProperties(root);
        var value = new NoObligationNavalConvoyScheduleResolved(
            root.GetProperty("campaignId").GetString()!, root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            CampaignSnapshotSerializer.ParsePosition(root.GetProperty("sequencePosition")),
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));
        if (root.GetProperty("contractVersion").GetInt32() != value.ContractVersion)
            throw new JsonException("The schedule event contract version is invalid.");
        ValidateAdvance(value, LandPhaseIds.TacticalShipping, 3);
        return value;
    }

    private static NoObligationTacticalShippingResolved ParseTactical(JsonElement root)
    {
        RequireAdvanceProperties(root);
        var value = new NoObligationTacticalShippingResolved(
            root.GetProperty("campaignId").GetString()!, root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            CampaignSnapshotSerializer.ParsePosition(root.GetProperty("sequencePosition")),
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));
        if (root.GetProperty("contractVersion").GetInt32() != value.ContractVersion)
            throw new JsonException("The tactical event contract version is invalid.");
        ValidateAdvance(value, LandPhaseIds.InitiativeDeclaration, 4);
        return value;
    }

    private static void RequireAdvanceProperties(JsonElement root) => CampaignSnapshotSerializer.RequireProperties(
        root, "contractVersion", "eventType", "campaignId", "stateVersion", "fromPositionId",
        "sequencePosition", "sources");

    private static void WriteDeclaration(Utf8JsonWriter writer, InitiativeOrderDeclared declared)
    {
        writer.WriteNumber("contractVersion", declared.ContractVersion);
        writer.WriteString("eventType", "initiative-order-declared");
        writer.WriteString("campaignId", declared.CampaignId);
        writer.WriteNumber("stateVersion", declared.StateVersion);
        writer.WriteString("fromPositionId", declared.FromPositionId);
        writer.WriteNumber("operationStage", declared.OperationStage);
        writer.WriteString("declaringHolder", CampaignSnapshotSerializer.FormatSide(declared.DeclaringHolder));
        writer.WriteString("firstSide", CampaignSnapshotSerializer.FormatSide(declared.FirstSide));
        writer.WriteString("secondSide", CampaignSnapshotSerializer.FormatSide(declared.SecondSide));
        CampaignSnapshotSerializer.WritePosition(writer, declared.SequencePosition);
        CampaignSnapshotSerializer.WriteSources(writer, declared.Sources);
    }

    private static InitiativeOrderDeclared ParseDeclaration(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(root, "contractVersion", "eventType", "campaignId",
            "stateVersion", "fromPositionId", "operationStage", "declaringHolder", "firstSide",
            "secondSide", "sequencePosition", "sources");
        var value = new InitiativeOrderDeclared(root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(), root.GetProperty("fromPositionId").GetString()!,
            CampaignSnapshotSerializer.ParsePosition(root.GetProperty("sequencePosition")),
            root.GetProperty("operationStage").GetInt32(),
            CampaignSnapshotSerializer.ParseSide(root.GetProperty("declaringHolder").GetString()),
            CampaignSnapshotSerializer.ParseSide(root.GetProperty("firstSide").GetString()),
            CampaignSnapshotSerializer.ParseSide(root.GetProperty("secondSide").GetString()),
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));
        if (root.GetProperty("contractVersion").GetInt32() != value.ContractVersion)
            throw new JsonException("The declaration event contract version is invalid.");
        ValidateDeclaration(value);
        return value;
    }

    private static InitiativeDetermined ParseDetermined(JsonElement root)
    {
        CampaignSnapshotSerializer.RequireProperties(
            root,
            "contractVersion",
            "eventType",
            "campaignId",
            "stateVersion",
            "fromPositionId",
            "outcome",
            "randomAlgorithmId",
            "randomCursorBefore",
            "randomCursorAfter",
            "sequencePosition",
            "sources");
        var determined = new InitiativeDetermined(
            root.GetProperty("campaignId").GetString()!,
            root.GetProperty("stateVersion").GetInt64(),
            root.GetProperty("fromPositionId").GetString()!,
            ParseOutcome(root.GetProperty("outcome")),
            root.GetProperty("randomAlgorithmId").GetString()!,
            root.GetProperty("randomCursorBefore").GetUInt64(),
            root.GetProperty("randomCursorAfter").GetUInt64(),
            CampaignSnapshotSerializer.ParsePosition(root.GetProperty("sequencePosition")),
            CampaignSnapshotSerializer.ParseSources(root.GetProperty("sources")));

        if (root.GetProperty("contractVersion").GetInt32() != determined.ContractVersion)
        {
            throw new JsonException("The Initiative event contract version is invalid.");
        }

        ValidateDetermined(determined);
        return determined;
    }

    private static void WriteOutcome(Utf8JsonWriter writer, InitiativeOutcome outcome)
    {
        switch (outcome)
        {
            case PredeterminedInitiativeOutcome predetermined:
                writer.WriteString("kind", "predetermined");
                writer.WriteString(
                    "holder",
                    CampaignSnapshotSerializer.FormatSide(predetermined.Holder));
                break;
            case ContestedInitiativeOutcome contested:
                writer.WriteString("kind", "contested");
                writer.WriteStartObject("axisFacts");
                writer.WriteString(
                    "rommelLocation",
                    CampaignSnapshotSerializer.FormatLocation(
                        contested.AxisFacts.RommelLocation));
                writer.WriteStartArray("germanLandCombatUnitLocations");

                foreach (var location in contested.AxisFacts.GermanLandCombatUnitLocations)
                {
                    writer.WriteStringValue(
                        CampaignSnapshotSerializer.FormatLocation(location));
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.WriteString("axisPresence", FormatPresence(contested.AxisPresence));
                writer.WriteStartArray("rounds");

                foreach (var round in contested.Rounds)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("round", round.Round);
                    writer.WriteNumber("axisDie", round.AxisDie);
                    writer.WriteNumber("axisRating", round.AxisRating);
                    writer.WriteNumber("axisTotal", round.AxisTotal);
                    writer.WriteNumber("commonwealthDie", round.CommonwealthDie);
                    writer.WriteNumber("commonwealthRating", round.CommonwealthRating);
                    writer.WriteNumber("commonwealthTotal", round.CommonwealthTotal);
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                writer.WriteString(
                    "holder",
                    CampaignSnapshotSerializer.FormatSide(contested.Holder));
                break;
            default:
                throw new JsonException("The Initiative outcome type is not serializable.");
        }
    }

    private static InitiativeOutcome ParseOutcome(JsonElement outcome)
    {
        var kind = outcome.GetProperty("kind").GetString();

        return kind switch
        {
            "predetermined" => ParsePredeterminedOutcome(outcome),
            "contested" => ParseContestedOutcome(outcome),
            _ => throw new JsonException($"Unknown Initiative outcome '{kind}'."),
        };
    }

    private static PredeterminedInitiativeOutcome ParsePredeterminedOutcome(JsonElement outcome)
    {
        CampaignSnapshotSerializer.RequireProperties(outcome, "kind", "holder");
        return new PredeterminedInitiativeOutcome(
            CampaignSnapshotSerializer.ParseSide(outcome.GetProperty("holder").GetString()));
    }

    private static ContestedInitiativeOutcome ParseContestedOutcome(JsonElement outcome)
    {
        CampaignSnapshotSerializer.RequireProperties(
            outcome,
            "kind",
            "axisFacts",
            "axisPresence",
            "rounds",
            "holder");
        var facts = outcome.GetProperty("axisFacts");
        CampaignSnapshotSerializer.RequireProperties(
            facts,
            "rommelLocation",
            "germanLandCombatUnitLocations");
        var axisFacts = new AxisInitiativeSourceFacts(
            CampaignSnapshotSerializer.ParseLocation(
                facts.GetProperty("rommelLocation").GetString()),
            facts.GetProperty("germanLandCombatUnitLocations")
                .EnumerateArray()
                .Select(location => CampaignSnapshotSerializer.ParseLocation(
                    location.GetString()))
                .ToArray());
        var rounds = outcome.GetProperty("rounds")
            .EnumerateArray()
            .Select(ParseRound)
            .ToArray();

        return new ContestedInitiativeOutcome(
            axisFacts,
            ParsePresence(outcome.GetProperty("axisPresence").GetString()),
            rounds,
            CampaignSnapshotSerializer.ParseSide(
                outcome.GetProperty("holder").GetString()));
    }

    private static InitiativeRollRound ParseRound(JsonElement round)
    {
        CampaignSnapshotSerializer.RequireProperties(
            round,
            "round",
            "axisDie",
            "axisRating",
            "axisTotal",
            "commonwealthDie",
            "commonwealthRating",
            "commonwealthTotal");

        return new InitiativeRollRound(
            round.GetProperty("round").GetInt32(),
            round.GetProperty("axisDie").GetInt32(),
            round.GetProperty("axisRating").GetInt32(),
            round.GetProperty("axisTotal").GetInt32(),
            round.GetProperty("commonwealthDie").GetInt32(),
            round.GetProperty("commonwealthRating").GetInt32(),
            round.GetProperty("commonwealthTotal").GetInt32());
    }

    private static string FormatPresence(AxisInitiativePresence presence) => presence switch
    {
        AxisInitiativePresence.RommelOnQualifyingGameMap =>
            "rommel-on-qualifying-game-map",
        AxisInitiativePresence.GermanLandCombatUnitOnQualifyingGameMap =>
            "german-land-combat-unit-on-qualifying-game-map",
        AxisInitiativePresence.NeitherOnQualifyingGameMap =>
            "neither-on-qualifying-game-map",
        _ => throw new ArgumentOutOfRangeException(nameof(presence)),
    };

    private static AxisInitiativePresence ParsePresence(string? presence) => presence switch
    {
        "rommel-on-qualifying-game-map" =>
            AxisInitiativePresence.RommelOnQualifyingGameMap,
        "german-land-combat-unit-on-qualifying-game-map" =>
            AxisInitiativePresence.GermanLandCombatUnitOnQualifyingGameMap,
        "neither-on-qualifying-game-map" =>
            AxisInitiativePresence.NeitherOnQualifyingGameMap,
        _ => throw new JsonException($"Unknown Axis Initiative presence '{presence}'."),
    };

    private static void ValidateCreated(CampaignCreated created)
    {
        if (created.Setup is null
            || created.InitialWorld is null
            || created.RandomState is null
            || created.SequencePosition is null)
        {
            throw new JsonException("The campaign creation event is invalid.");
        }

        var localSnapshot = new CampaignSnapshot(
            CampaignSnapshot.CurrentContractVersion,
            created.CampaignId,
            created.StateVersion,
            created.RulesetHash,
            created.Setup,
            created.InitialWorld,
            null,
            [],
            created.RandomState,
            created.SequencePosition);

        if (created.ContractVersion != 8
            || created.StateVersion != 1
            || created.RandomState.NextByteCursor != 0
            || !CampaignSnapshotValidator.IsLocallyValid(localSnapshot))
        {
            throw new JsonException("The campaign creation event is invalid.");
        }
    }

    private static void ValidateDetermined(InitiativeDetermined determined)
    {
        if (determined.ContractVersion != 2
            || determined.StateVersion < 2
            || !string.Equals(
                determined.RandomAlgorithmId,
                SandtableRandom.AlgorithmId,
                StringComparison.Ordinal)
            || determined.SequencePosition.ContractVersion != Cna1979LandSequence.ContractVersion
            || determined.SequencePosition.StageId != LandStageIds.NavalConvoy
            || determined.SequencePosition.PhaseId != LandPhaseIds.NavalConvoySchedule
            || determined.SequencePosition.ActorRole != LandActorRole.None
            || determined.SequencePosition.ActiveSide is not null)
        {
            throw new JsonException("The Initiative event contract is invalid.");
        }
    }

    private static void ValidateWeather(WeatherDetermined determined)
    {
        _ = determined.ToState();
        if (determined.ContractVersion != 1
            || determined.StateVersion < 6
            || determined.SequencePosition.GameTurn != determined.GameTurn
            || determined.SequencePosition.OperationStage != determined.OperationStage
            || determined.SequencePosition.PhaseId != LandPhaseIds.Organization
            || !determined.Sources.SequenceEqual(WeatherEventFactory.GetSources(determined.Kind)))
        {
            throw new JsonException("The Weather event contract is invalid.");
        }
    }

    private static void ValidateAdvance(
        OpeningPreambleAdvanced resolved,
        string expectedPhase,
        long expectedStateVersion)
    {
        if (resolved.ContractVersion != 1 || resolved.StateVersion != expectedStateVersion
            || resolved.SequencePosition.PhaseId != expectedPhase)
            throw new JsonException("The preamble event contract is invalid.");
    }

    private static void ValidateDeclaration(InitiativeOrderDeclared declared)
    {
        if (declared.ContractVersion != 1 || declared.StateVersion != 5 || declared.OperationStage != 1
            || declared.SequencePosition.PhaseId != LandPhaseIds.WeatherDetermination
            || declared.FirstSide == declared.SecondSide
            || (declared.FirstSide != declared.DeclaringHolder
                && declared.SecondSide != declared.DeclaringHolder))
            throw new JsonException("The declaration event contract is invalid.");
    }

    private static void ValidateStageEntry(StageEntryResolved resolved, long expectedStateVersion)
    {
        if (resolved.ContractVersion != 1
            || string.IsNullOrWhiteSpace(resolved.CampaignId)
            || resolved.StateVersion != expectedStateVersion)
        {
            throw new JsonException("The Stage Entry event contract is invalid.");
        }
    }

    private static void ValidateReserve(ReserveDesignationEvent campaignEvent)
    {
        try
        {
            campaignEvent.ValidateContract();
        }
        catch (Exception exception) when (exception is ArgumentException
            or ArithmeticException
            or InvalidOperationException)
        {
            throw new JsonException(
                "The Reserve designation event contract is invalid.",
                exception);
        }
    }
}
