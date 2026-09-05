using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cna.Core.Content;
using Cna.Core.Rules;

namespace Cna.Core.Campaigns;

internal static class CampaignBreakdownCodec
{
    internal const string EmptyHash = "sha256:0000000000000000000000000000000000000000000000000000000000000000";

    public static string RouteId(string campaignId, string rulesetHash, CampaignBreakdownRoute route) =>
        Identity("route", campaignId, rulesetHash, writer => WriteRouteFields(writer, route, identity: true));
    public static string StopId(string campaignId, string rulesetHash, CampaignBreakdownStop stop) =>
        Identity("stop", campaignId, rulesetHash, writer => WriteStopFields(writer, stop));
    public static string CheckId(string campaignId, string rulesetHash, string stopId, string cohortId) =>
        Identity("check", campaignId, rulesetHash, writer =>
        {
            writer.WriteString("stopId", ContentContractGuards.RequireSha256(stopId, nameof(stopId)));
            writer.WriteString("cohortId", ContentContractGuards.RequireStableId(cohortId, nameof(cohortId)));
        });
    public static string LotId(string campaignId, string rulesetHash, CampaignBrokenVehicleLot lot) =>
        Identity("lot", campaignId, rulesetHash, writer => WriteLotFields(writer, lot));

    private static string Identity(string kind, string campaignId, string rulesetHash,
        Action<Utf8JsonWriter> fields)
    {
        ContentContractGuards.RequireStableId(campaignId, nameof(campaignId));
        if (!CampaignSnapshotValidator.IsRulesHash(rulesetHash)) throw new ArgumentException("Invalid ruleset hash.");
        var bytes = Bytes(writer =>
        {
            writer.WriteStartObject();
            writer.WriteString("domain", $"sandtable.breakdown.{kind}.v1");
            writer.WriteString("campaignId", campaignId);
            writer.WriteString("rulesetHash", rulesetHash);
            fields(writer);
            writer.WriteEndObject();
        });
        return $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";
    }

    internal static byte[] Bytes(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) write(writer);
        return stream.ToArray();
    }

    public static byte[] Serialize(CampaignBreakdownFlow flow) => Bytes(writer => WriteFlow(writer, flow));

    public static CampaignBreakdownFlow Deserialize(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            var flow = ParseFlow(document.RootElement);
            if (!bytes.Span.SequenceEqual(Serialize(flow))) throw new JsonException("Noncanonical Breakdown flow.");
            return flow;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or KeyNotFoundException or FormatException or ArithmeticException)
        {
            throw new JsonException("Invalid Breakdown flow.", error);
        }
    }

    internal static void WriteFlow(Utf8JsonWriter writer, CampaignBreakdownFlow flow)
    {
        ArgumentNullException.ThrowIfNull(flow);
        writer.WriteStartObject();
        switch (flow)
        {
            case CampaignBreakdownFlow.Idle:
                writer.WriteString("kind", "idle");
                break;
            case CampaignBreakdownFlow.Moving moving:
                writer.WriteString("kind", "moving");
                writer.WritePropertyName("route"); WriteRoute(writer, moving.Route);
                break;
            case CampaignBreakdownFlow.Reacting reacting:
                writer.WriteString("kind", "reacting");
                WriteContinuation(writer, reacting.PhasingContinuation);
                writer.WritePropertyName("reactorRoute");
                if (reacting.ReactorRoute is null) writer.WriteNullValue();
                else WriteRoute(writer, reacting.ReactorRoute);
                break;
            case CampaignBreakdownFlow.ReactorStopOpen open:
                writer.WriteString("kind", "reactor-stop-open");
                WriteContinuation(writer, open.PhasingContinuation);
                writer.WritePropertyName("stop"); WriteStop(writer, open.Stop);
                break;
            case CampaignBreakdownFlow.ReactorStopClosed closed:
                writer.WriteString("kind", "reactor-stop-closed");
                WriteContinuation(writer, closed.PhasingContinuation);
                writer.WritePropertyName("stop"); WriteStop(writer, closed.Stop);
                break;
            case CampaignBreakdownFlow.PhasingStop stopped:
                writer.WriteString("kind", "phasing-stop");
                writer.WritePropertyName("stop"); WriteStop(writer, stopped.Stop);
                break;
            default: throw new JsonException("Unknown Breakdown flow.");
        }
        writer.WriteEndObject();
    }

    internal static CampaignBreakdownFlow ParseFlow(JsonElement value)
    {
        switch (value.GetProperty("kind").GetString())
        {
            case "idle":
                Require(value, "kind"); return new CampaignBreakdownFlow.Idle();
            case "moving":
                Require(value, "kind", "route"); return new CampaignBreakdownFlow.Moving(ParseRoute(value.GetProperty("route")));
            case "reacting":
                Require(value, "kind", "phasingContinuation", "reactorRoute");
                var route = value.GetProperty("reactorRoute");
                return new CampaignBreakdownFlow.Reacting(ParseContinuation(value.GetProperty("phasingContinuation")),
                    route.ValueKind == JsonValueKind.Null ? null : ParseRoute(route));
            case "reactor-stop-open":
                Require(value, "kind", "phasingContinuation", "stop");
                return new CampaignBreakdownFlow.ReactorStopOpen(ParseContinuation(value.GetProperty("phasingContinuation")), ParseStop(value.GetProperty("stop")));
            case "reactor-stop-closed":
                Require(value, "kind", "phasingContinuation", "stop");
                return new CampaignBreakdownFlow.ReactorStopClosed(ParseContinuation(value.GetProperty("phasingContinuation")), ParseStop(value.GetProperty("stop")));
            case "phasing-stop":
                Require(value, "kind", "stop"); return new CampaignBreakdownFlow.PhasingStop(ParseStop(value.GetProperty("stop")));
            default: throw new JsonException("Unknown Breakdown flow.");
        }
    }

    private static void WriteContinuation(Utf8JsonWriter writer, CampaignPhasingContinuation continuation)
    {
        ArgumentNullException.ThrowIfNull(continuation);
        writer.WriteStartObject("phasingContinuation");
        switch (continuation)
        {
            case CampaignPhasingContinuation.ResumeRoute resume:
                writer.WriteString("kind", "resume-route");
                writer.WritePropertyName("route"); WriteRoute(writer, resume.Route); break;
            case CampaignPhasingContinuation.ResolveStop resolve:
                writer.WriteString("kind", "resolve-stop");
                writer.WritePropertyName("stop"); WriteStop(writer, resolve.Stop); break;
            default: throw new JsonException("Unknown phasing continuation.");
        }
        writer.WriteEndObject();
    }

    private static CampaignPhasingContinuation ParseContinuation(JsonElement value)
    {
        switch (value.GetProperty("kind").GetString())
        {
            case "resume-route":
                Require(value, "kind", "route"); return new CampaignPhasingContinuation.ResumeRoute(ParseRoute(value.GetProperty("route")));
            case "resolve-stop":
                Require(value, "kind", "stop"); return new CampaignPhasingContinuation.ResolveStop(ParseStop(value.GetProperty("stop")));
            default: throw new JsonException("Unknown phasing continuation.");
        }
    }

    internal static void WriteRoute(Utf8JsonWriter writer, CampaignBreakdownRoute route)
    {
        ArgumentNullException.ThrowIfNull(route);
        writer.WriteStartObject(); writer.WriteString("routeId", route.RouteId);
        WriteRouteFields(writer, route, identity: false); writer.WriteEndObject();
    }

    private static void WriteRouteFields(Utf8JsonWriter writer, CampaignBreakdownRoute route, bool identity)
    {
        writer.WriteNumber("firstMoveStateVersion", route.FirstMoveStateVersion);
        writer.WriteString("elementId", route.ElementId);
        writer.WriteString("representationId", route.RepresentationId);
        writer.WriteString("owner", CampaignSnapshotSerializer.FormatSide(route.Owner));
        writer.WriteString("originLocationId", route.OriginLocationId);
        if (!identity) writer.WriteString("currentLocationId", route.CurrentLocationId);
        writer.WriteStartArray("cohortIds");
        foreach (var id in route.CohortIds) writer.WriteStringValue(id);
        writer.WriteEndArray();
    }

    internal static CampaignBreakdownRoute ParseRoute(JsonElement value)
    {
        Require(value, "routeId", "firstMoveStateVersion", "elementId", "representationId", "owner", "originLocationId", "currentLocationId", "cohortIds");
        return new CampaignBreakdownRoute(Text(value, "routeId"), value.GetProperty("firstMoveStateVersion").GetInt64(),
            Text(value, "elementId"), Text(value, "representationId"), CampaignSnapshotSerializer.ParseSide(Text(value, "owner")),
            Text(value, "originLocationId"), Text(value, "currentLocationId"),
            value.GetProperty("cohortIds").EnumerateArray().Select(id => id.GetString()!));
    }

    internal static void WriteStop(Utf8JsonWriter writer, CampaignBreakdownStop stop)
    {
        ArgumentNullException.ThrowIfNull(stop);
        writer.WriteStartObject(); writer.WriteString("stopId", stop.StopId);
        WriteStopFields(writer, stop); writer.WriteEndObject();
    }

    private static void WriteStopFields(Utf8JsonWriter writer, CampaignBreakdownStop stop)
    {
        writer.WriteNumber("recordedStateVersion", stop.RecordedStateVersion);
        writer.WritePropertyName("route"); WriteRoute(writer, stop.Route);
        writer.WriteString("reason", FormatReason(stop.Reason));
        writer.WriteString("weatherKind", FormatWeather(stop.WeatherKind));
        writer.WriteStartArray("cohortInputs");
        foreach (var input in stop.CohortInputs) WriteInput(writer, input);
        writer.WriteEndArray();
    }

    internal static CampaignBreakdownStop ParseStop(JsonElement value)
    {
        Require(value, "stopId", "recordedStateVersion", "route", "reason", "weatherKind", "cohortInputs");
        return new CampaignBreakdownStop(Text(value, "stopId"), value.GetProperty("recordedStateVersion").GetInt64(),
            ParseRoute(value.GetProperty("route")), ParseReason(Text(value, "reason")), ParseWeather(Text(value, "weatherKind")),
            value.GetProperty("cohortInputs").EnumerateArray().Select(ParseInput));
    }

    internal static void WriteInput(Utf8JsonWriter writer, CampaignBreakdownCheckInput input)
    {
        writer.WriteStartObject();
        writer.WriteString("cohortId", input.CohortId); writer.WriteString("vehicleTypeId", input.VehicleTypeId);
        writer.WriteString("profileId", input.ProfileId); writer.WriteNumber("workingPointCount", input.WorkingPointCount);
        writer.WritePropertyName("cumulativeBreakdownPoints"); BreakdownPointAmountCodec.WriteCanonical(writer, input.CumulativeBreakdownPoints);
        writer.WritePropertyName("sandstormAttributedBreakdownPoints"); BreakdownPointAmountCodec.WriteCanonical(writer, input.SandstormAttributedBreakdownPoints);
        writer.WriteString("highestEffectiveCheckedBandId", input.HighestEffectiveCheckedBandId); writer.WriteEndObject();
    }

    internal static CampaignBreakdownCheckInput ParseInput(JsonElement value)
    {
        Require(value, "cohortId", "vehicleTypeId", "profileId", "workingPointCount", "cumulativeBreakdownPoints", "sandstormAttributedBreakdownPoints", "highestEffectiveCheckedBandId");
        return new CampaignBreakdownCheckInput(Text(value, "cohortId"), Text(value, "vehicleTypeId"), Text(value, "profileId"),
            value.GetProperty("workingPointCount").GetInt32(),
            BreakdownPointAmountCodec.Deserialize(Encoding.UTF8.GetBytes(value.GetProperty("cumulativeBreakdownPoints").GetRawText())),
            BreakdownPointAmountCodec.Deserialize(Encoding.UTF8.GetBytes(value.GetProperty("sandstormAttributedBreakdownPoints").GetRawText())),
            value.GetProperty("highestEffectiveCheckedBandId").GetString());
    }

    internal static void WriteLot(Utf8JsonWriter writer, CampaignBrokenVehicleLot lot)
    {
        writer.WriteStartObject(); writer.WriteString("lotId", lot.LotId); WriteLotFields(writer, lot);
        CampaignSnapshotSerializer.WriteSources(writer, lot.Sources); writer.WriteEndObject();
    }

    private static void WriteLotFields(Utf8JsonWriter writer, CampaignBrokenVehicleLot lot)
    {
        writer.WriteString("stopId", lot.StopId); writer.WriteString("checkId", lot.CheckId);
        writer.WriteString("owner", CampaignSnapshotSerializer.FormatSide(lot.Owner));
        writer.WriteString("cohortId", lot.CohortId); writer.WriteString("vehicleTypeId", lot.VehicleTypeId);
        writer.WriteNumber("pointCount", lot.PointCount); writer.WriteString("locationId", lot.LocationId);
        writer.WriteNumber("createdStateVersion", lot.CreatedStateVersion);
    }

    internal static CampaignBrokenVehicleLot ParseLot(JsonElement value)
    {
        Require(value, "lotId", "stopId", "checkId", "owner", "cohortId", "vehicleTypeId", "pointCount", "locationId", "createdStateVersion", "sources");
        return new CampaignBrokenVehicleLot(Text(value, "lotId"), Text(value, "stopId"), Text(value, "checkId"),
            CampaignSnapshotSerializer.ParseSide(Text(value, "owner")), Text(value, "cohortId"), Text(value, "vehicleTypeId"),
            value.GetProperty("pointCount").GetInt32(), Text(value, "locationId"), value.GetProperty("createdStateVersion").GetInt64(),
            CampaignSnapshotSerializer.ParseSources(value.GetProperty("sources")));
    }

    private static string Text(JsonElement value, string property) => value.GetProperty(property).GetString()!;
    internal static void Require(JsonElement value, params string[] names) => CampaignSnapshotSerializer.RequireProperties(value, names);
    internal static string FormatWeather(BreakdownWeatherKind weather) => weather switch
    {
        BreakdownWeatherKind.Normal => "normal",
        BreakdownWeatherKind.Hot => "hot",
        BreakdownWeatherKind.Sandstorm => "sandstorm",
        BreakdownWeatherKind.Rainstorm => "rainstorm",
        _ => throw new JsonException("Invalid Breakdown weather."),
    };
    private static BreakdownWeatherKind ParseWeather(string weather) => weather switch
    {
        "normal" => BreakdownWeatherKind.Normal,
        "hot" => BreakdownWeatherKind.Hot,
        "sandstorm" => BreakdownWeatherKind.Sandstorm,
        "rainstorm" => BreakdownWeatherKind.Rainstorm,
        _ => throw new JsonException("Invalid Breakdown weather."),
    };
    internal static string FormatReason(CampaignBreakdownStopReason reason) => reason switch
    {
        CampaignBreakdownStopReason.Deliberate => "deliberate",
        CampaignBreakdownStopReason.MovementEnded => "movement-ended",
        CampaignBreakdownStopReason.CpExhausted => "cp-exhausted",
        CampaignBreakdownStopReason.ReactionCompleted => "reaction-completed",
        CampaignBreakdownStopReason.ReactionUnavailable => "reaction-unavailable",
        CampaignBreakdownStopReason.ReactionTimeout => "reaction-timeout",
        _ => throw new JsonException("Invalid stop reason."),
    };
    private static CampaignBreakdownStopReason ParseReason(string reason) => reason switch
    {
        "deliberate" => CampaignBreakdownStopReason.Deliberate,
        "movement-ended" => CampaignBreakdownStopReason.MovementEnded,
        "cp-exhausted" => CampaignBreakdownStopReason.CpExhausted,
        "reaction-completed" => CampaignBreakdownStopReason.ReactionCompleted,
        "reaction-unavailable" => CampaignBreakdownStopReason.ReactionUnavailable,
        "reaction-timeout" => CampaignBreakdownStopReason.ReactionTimeout,
        _ => throw new JsonException("Invalid stop reason."),
    };
}
